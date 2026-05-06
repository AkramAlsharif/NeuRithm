window.neurithmPianoAudio = {
    audioContext: null,
    activeVoices: {},

    ensureContext: function () {
        if (!this.audioContext) {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        if (this.audioContext.state === "suspended") {
            this.audioContext.resume();
        }
    },

    noteToFrequency: function (note) {
        if (!note || note.length < 2) {
            return 440;
        }

        const map = {
            "C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5,
            "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11
        };

        const pitch = note.length >= 3 && note[1] === '#' ? note.substring(0, 2) : note.substring(0, 1);
        const octaveText = note.substring(pitch.length);
        const octave = parseInt(octaveText, 10);

        if (!map.hasOwnProperty(pitch) || Number.isNaN(octave)) {
            return 440;
        }

        const midi = ((octave + 1) * 12) + map[pitch];
        return 440 * Math.pow(2, (midi - 69) / 12);
    },

    noteOn: function (note) {
        if (!note) {
            return;
        }

        this.ensureContext();

        if (this.activeVoices[note]) {
            return;
        }

        const frequency = this.noteToFrequency(note);
        const oscillator = this.audioContext.createOscillator();
        oscillator.type = "triangle";
        oscillator.frequency.value = frequency;

        const gain = this.audioContext.createGain();
        gain.gain.value = 0.0001;
        gain.gain.linearRampToValueAtTime(0.07, this.audioContext.currentTime + 0.015);

        oscillator.connect(gain);
        gain.connect(this.audioContext.destination);

        oscillator.start();

        this.activeVoices[note] = {
            oscillator: oscillator,
            gain: gain
        };
    },

    noteOff: function (note) {
        const voice = this.activeVoices[note];
        if (!voice || !this.audioContext) {
            return;
        }

        const stopAt = this.audioContext.currentTime + 0.08;
        voice.gain.gain.cancelScheduledValues(this.audioContext.currentTime);
        voice.gain.gain.setValueAtTime(voice.gain.gain.value, this.audioContext.currentTime);
        voice.gain.gain.linearRampToValueAtTime(0.0001, stopAt);
        voice.oscillator.stop(stopAt + 0.01);

        delete this.activeVoices[note];
    }
};
