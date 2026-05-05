window.neurithmMicrophone = {
    stream: null,
    audioContext: null,
    analyser: null,
    source: null,
    timer: null,

    requestPermission: async function () {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            window.neurithmClientLogs?.write("Microphone", "Browser does not support mediaDevices.getUserMedia");
            return false;
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream.getTracks().forEach(track => track.stop());
            window.neurithmClientLogs?.write("Microphone", "Microphone permission granted");
            return true;
        } catch (error) {
            const message = error && error.message ? error.message : "Unknown microphone error";
            window.neurithmClientLogs?.write("Microphone", `Microphone permission denied: ${message}`);
            return false;
        }
    },

    startCapture: async function (dotNetRef) {
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
            }, 60);

            window.neurithmClientLogs?.write("Microphone", "Audio capture started");
            return true;
        } catch (error) {
            const message = error && error.message ? error.message : "Unknown start capture error";
            window.neurithmClientLogs?.write("Microphone", `Failed to start capture: ${message}`);
            return false;
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
