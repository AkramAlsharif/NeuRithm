window.neurithmMicrophone = {
    stream: null,
    audioContext: null,
    analyser: null,
    source: null,
    pullGain: null,
    timer: null,

    requestPermission: async function () {
        if (!window.isSecureContext) {
            const message = "Microphone requires HTTPS secure context. Open https://neurithm.net or https://www.neurithm.net and trust the local certificate in your browser/OS.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { status: "blocked", errorMessage: message };
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            const message = "Browser does not support mediaDevices.getUserMedia.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { status: "blocked", errorMessage: message };
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: false,
                    noiseSuppression: false,
                    autoGainControl: false
                }
            });
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
            const message = "Microphone capture blocked: secure context required. Use HTTPS on neurithm.net and trust the local certificate.";
            window.neurithmClientLogs?.write("Microphone", message);
            return { started: false, blocked: true, errorMessage: message };
        }

        try {
            if (this.timer) {
                this.stopCapture();
            }

            this.stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: false,
                    noiseSuppression: false,
                    autoGainControl: false
                }
            });

            this.audioContext = new (window.AudioContext || window.webkitAudioContext)({ latencyHint: "interactive" });
            if (this.audioContext.state === "suspended") {
                await this.audioContext.resume();
            }

            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 2048;
            this.analyser.smoothingTimeConstant = 0.0;

            this.source = this.audioContext.createMediaStreamSource(this.stream);
            this.pullGain = this.audioContext.createGain();
            this.pullGain.gain.value = 0.0;

            this.source.connect(this.analyser);
            this.analyser.connect(this.pullGain);
            this.pullGain.connect(this.audioContext.destination);

            const buffer = new Float32Array(this.analyser.fftSize);
            const sampleRate = this.audioContext.sampleRate;

            this.timer = window.setInterval(() => {
                try {
                    this.analyser.getFloatTimeDomainData(buffer);
                    dotNetRef.invokeMethodAsync("OnAudioFrame", {
                        samples: Array.from(buffer),
                        sampleRate: sampleRate
                    }).catch(err => {
                        const message = err && err.message ? err.message : String(err);
                        window.neurithmClientLogs?.write("Microphone", `OnAudioFrame invoke failed: ${message}`);
                    });
                } catch (error) {
                    const message = error && error.message ? error.message : "Unknown analyser read error";
                    window.neurithmClientLogs?.write("Microphone", `Analyser read failed: ${message}`);
                }
            }, 25);

            window.neurithmClientLogs?.write("Microphone", `Audio capture started (sampleRate=${sampleRate})`);
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

        if (this.pullGain) {
            try { this.pullGain.disconnect(); } catch { }
            this.pullGain = null;
        }

        if (this.source) {
            try { this.source.disconnect(); } catch { }
            this.source = null;
        }

        if (this.analyser) {
            try { this.analyser.disconnect(); } catch { }
            this.analyser = null;
        }

        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }

        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }

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
