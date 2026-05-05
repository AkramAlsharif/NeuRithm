window.neurithmMicrophone = {
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
