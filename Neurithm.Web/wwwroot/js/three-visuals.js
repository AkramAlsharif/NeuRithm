window.neurithmThreeVisuals = {
    scene: null,
    camera: null,
    renderer: null,
    sparkles: [],
    animFrame: null,
    host: null,
    notesProvider: null,

    ensureHost: function (containerId) {
        const host = document.getElementById(containerId);
        if (!host) {
            return null;
        }

        host.style.position = "relative";
        host.style.overflow = "hidden";
        host.style.background = "#020104";
        return host;
    },

    start: async function (containerId) {
        if (this.animFrame) {
            return;
        }

        const host = this.ensureHost(containerId);
        if (!host) {
            return;
        }

        this.host = host;

        const THREE = await import('/lib/threejs/three.module.min.js');

        this.scene = new THREE.Scene();
        this.camera = new THREE.PerspectiveCamera(60, host.clientWidth / host.clientHeight, 0.1, 100);
        this.camera.position.z = 6;

        this.renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
        this.renderer.setSize(host.clientWidth, host.clientHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));

        host.innerHTML = '';
        host.appendChild(this.renderer.domElement);

        const lightA = new THREE.PointLight(0xbf7bff, 1.2, 30);
        lightA.position.set(0, 3, 4);
        this.scene.add(lightA);

        const lightB = new THREE.PointLight(0x7c3aed, 1.0, 30);
        lightB.position.set(0, -3, 4);
        this.scene.add(lightB);

        const sparkleGeometry = new THREE.SphereGeometry(0.02, 8, 8);
        for (let i = 0; i < 240; i++) {
            const material = new THREE.MeshBasicMaterial({ color: i % 2 === 0 ? 0xb388ff : 0x9333ea });
            const sparkle = new THREE.Mesh(sparkleGeometry, material);
            sparkle.position.set((Math.random() * 10) - 5, (Math.random() * 8) - 2, (Math.random() * 2) - 1);
            sparkle.userData.speed = 0.01 + Math.random() * 0.03;
            this.sparkles.push(sparkle);
            this.scene.add(sparkle);
        }

        const resize = () => {
            if (!this.host || !this.renderer || !this.camera) {
                return;
            }

            const width = this.host.clientWidth;
            const height = this.host.clientHeight || 400;
            this.camera.aspect = width / height;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(width, height);
        };

        window.addEventListener('resize', resize);
        this._onResize = resize;

        const animate = () => {
            this.animFrame = requestAnimationFrame(animate);

            for (let i = 0; i < this.sparkles.length; i++) {
                const s = this.sparkles[i];
                s.position.y -= s.userData.speed;
                if (s.position.y < -3.5) {
                    s.position.y = 4.2;
                    s.position.x = (Math.random() * 10) - 5;
                }
            }

            if (this.renderer && this.scene && this.camera) {
                this.renderer.render(this.scene, this.camera);
            }
        };

        animate();
    },

    stop: function () {
        if (this.animFrame) {
            cancelAnimationFrame(this.animFrame);
            this.animFrame = null;
        }

        if (this._onResize) {
            window.removeEventListener('resize', this._onResize);
            this._onResize = null;
        }

        if (this.renderer) {
            this.renderer.dispose();
            this.renderer = null;
        }

        this.scene = null;
        this.camera = null;
        this.sparkles = [];
    }
};
