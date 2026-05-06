window.neurithmMicrophone = {
    stream: null,
    audioContext: null,
    analyser: null,
    source: null,
    timer: null,

    requestPermission: async function () {
        if (!window.isSecureContext) {
            const message = "Microphone requires HTTPS secure context. Open https://neurithm.net or https://www.neurithm.net.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { status: "blocked", errorMessage: message };
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            const message = "Browser does not support mediaDevices.getUserMedia.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { status: "blocked", errorMessage: message };
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream.getTracks().forEach(track => track.stop());
            window.neurithmClientLogs?.write("Microphone", "Microphone permission granted");
            return { status: "granted", errorMessage: null };
        } catch (error) {
            const message = error && error.message ? error.message : "Unknown microphone permission error";
            const denied = error && (error.name === "NotAllowedError" || error.name === "SecurityError");
            window.neurithmClientLogs?.write("Microphone", `Microphone permission failed: ${message}`);
            return { status: denied ? "denied" : "error", errorMessage: message };
        }
    },

    startCapture: async function (dotNetRef) {
        if (!window.isSecureContext) {
            const message = "Microphone capture blocked: secure context required. Use HTTPS on neurithm.net domain.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { started: false, blocked: true, errorMessage: message };
        }

        try {
            if (this.timer) {
                this.stopCapture();
            }

            this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 2048;
            this.source = this.audioContext.createMediaStreamSource(this.stream);
            this.source.connect(this.analyser);

            const buffer = new Float32Array(this.analyser.fftSize);
            const sampleRate = this.audioContext.sampleRate;

            this.timer = window.setInterval(() => {
                this.analyser.getFloatTimeDomainData(buffer);
                dotNetRef.invokeMethodAsync("OnAudioFrame", {
                    samples: Array.from(buffer),
                    sampleRate: sampleRate
                });
            }, 40);

            window.neurithmClientLogs?.write("Microphone", "Audio capture started");
            return { started: true, blocked: false, errorMessage: null };
        } catch (error) {
            const message = error && error.message ? error.message : "Unknown start capture error";
            const blocked = !!(error && error.name === "SecurityError");
            window.neurithmClientLogs?.write("Microphone", `Failed to start capture: ${message}`);
            return { started: false, blocked: blocked, errorMessage: message };
        }
    },

    stopCapture: function () {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }

        if (this.source) {
            try { this.source.disconnect(); } catch { }
            this.source = null;
        }

        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }

        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }

        this.analyser = null;
        window.neurithmClientLogs?.write("Microphone", "Audio capture stopped");
    }
};

window.neurithmClientLogs = {
    storageKey: "neurithm_client_logs",

    write: function (category, message) {
        const now = new Date().toISOString();
        const line = `[${now}] [${category}] ${message}`;

        let current = [];
        try {
            const raw = localStorage.getItem(this.storageKey);
            current = raw ? JSON.parse(raw) : [];
        } catch {
            current = [];
        }

        current.push(line);
        if (current.length > 500) {
            current = current.slice(current.length - 500);
        }

        localStorage.setItem(this.storageKey, JSON.stringify(current));
        console.log(line);
    },

    readRecent: function (count) {
        try {
            const raw = localStorage.getItem(this.storageKey);
            const logs = raw ? JSON.parse(raw) : [];
            return logs.slice(Math.max(0, logs.length - count));
        } catch {
            return [];
        }
    }
};
