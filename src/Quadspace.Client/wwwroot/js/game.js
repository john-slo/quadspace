// quadspace canvas render loop. Owns rendering, input (WASD move + arrow fire), the animated parallax
// starfield, and a procedural Web Audio background beat. All gameplay state is authoritative in C#:
// each frame we call the .NET `Tick` (movement) and draw what it returns; arrows call `Fire`; `M`
// toggles sound; game over calls `EndGame`.

const MOVE_KEYS = {
    KeyW: [0, -1],
    KeyS: [0, 1],
    KeyA: [-1, 0],
    KeyD: [1, 0],
};

const FIRE_KEYS = {
    ArrowUp: [0, -1],
    ArrowDown: [0, 1],
    ArrowLeft: [-1, 0],
    ArrowRight: [1, 0],
};

const clampAxis = (v) => (v < -1 ? -1 : v > 1 ? 1 : v);

// ---------------------------------------------------------------------------
// Procedural background beat (Web Audio). Lazily created and resumed on the
// first user gesture; mute state persists in localStorage.
// ---------------------------------------------------------------------------
function createBeat() {
    const MASTER_VOLUME = 0.22;
    const TEMPO = 128;
    const stepDuration = 60 / TEMPO / 2; // eighth notes
    const bass = [55.0, 55.0, 65.41, 55.0, 73.42, 55.0, 82.41, 65.41];
    const arp = [220.0, 261.63, 329.63, 440.0];

    let ctx = null;
    let master = null;
    let timer = null;
    let step = 0;
    let nextTime = 0;
    let muted = localStorage.getItem('quadspace-muted') === '1';

    const ensureContext = () => {
        if (ctx) {
            return;
        }
        const Ctor = window.AudioContext || window.webkitAudioContext;
        ctx = new Ctor();
        master = ctx.createGain();
        master.gain.value = muted ? 0 : MASTER_VOLUME;
        master.connect(ctx.destination);
    };

    const tone = (freq, at, dur, type, volume) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(freq, at);
        gain.gain.setValueAtTime(0.0001, at);
        gain.gain.exponentialRampToValueAtTime(volume, at + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, at + dur);
        osc.connect(gain).connect(master);
        osc.start(at);
        osc.stop(at + dur + 0.02);
    };

    const kick = (at) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.frequency.setValueAtTime(140, at);
        osc.frequency.exponentialRampToValueAtTime(50, at + 0.12);
        gain.gain.setValueAtTime(0.9, at);
        gain.gain.exponentialRampToValueAtTime(0.001, at + 0.14);
        osc.connect(gain).connect(master);
        osc.start(at);
        osc.stop(at + 0.15);
    };

    const schedule = () => {
        while (nextTime < ctx.currentTime + 0.1) {
            const s = step % 8;
            if (s % 4 === 0) {
                kick(nextTime);
            }
            tone(bass[s], nextTime, stepDuration * 0.9, 'sawtooth', 0.18);
            tone(arp[Math.floor(step / 2) % arp.length] * 2, nextTime, stepDuration * 0.5, 'square', 0.05);
            nextTime += stepDuration;
            step++;
        }
    };

    const reflectState = () => {
        const indicator = document.getElementById('sound-state');
        if (indicator) {
            indicator.textContent = muted ? 'OFF' : 'ON';
        }
    };

    return {
        start() {
            ensureContext();
            if (ctx.state === 'suspended') {
                ctx.resume();
            }
            if (!timer) {
                nextTime = ctx.currentTime + 0.05;
                timer = setInterval(schedule, 25);
            }
            reflectState();
        },
        toggleMuted() {
            muted = !muted;
            localStorage.setItem('quadspace-muted', muted ? '1' : '0');
            if (master) {
                master.gain.value = muted ? 0 : MASTER_VOLUME;
            }
            reflectState();
        },
        reflectState,
        stop() {
            if (timer) {
                clearInterval(timer);
                timer = null;
            }
            if (ctx) {
                ctx.close();
                ctx = null;
                master = null;
            }
        },
    };
}

// ---------------------------------------------------------------------------
// Starfield
// ---------------------------------------------------------------------------
function buildStarfield(arena, starfield) {
    const layers = [];
    for (let l = 0; l < starfield.layers; l++) {
        const list = [];
        for (let i = 0; i < starfield.starsPerLayer; i++) {
            list.push({ x: Math.random() * arena.width, y: Math.random() * arena.height });
        }
        layers.push({
            list,
            speed: 18 + l * 34,
            size: 0.6 + l * 0.7,
            color: `rgba(200, 230, 255, ${0.25 + l * 0.22})`,
        });
    }
    return layers;
}

function drawStarfield(ctx, canvas, starfield, dt) {
    for (const layer of starfield) {
        ctx.fillStyle = layer.color;
        for (const s of layer.list) {
            s.y += layer.speed * dt;
            if (s.y > canvas.height) {
                s.y = 0;
                s.x = Math.random() * canvas.width;
            }
            ctx.beginPath();
            ctx.arc(s.x, s.y, layer.size, 0, Math.PI * 2);
            ctx.fill();
        }
    }
}

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------
function drawShip(ctx, x, y, r) {
    ctx.save();
    ctx.translate(x, y);

    // Thruster flame (flickers).
    const flicker = 0.6 + Math.random() * 0.4;
    ctx.save();
    ctx.shadowColor = '#ff8a3d';
    ctx.shadowBlur = 14;
    ctx.fillStyle = `rgba(255, 138, 61, ${0.7 * flicker})`;
    ctx.beginPath();
    ctx.moveTo(-r * 0.4, r * 0.9);
    ctx.lineTo(0, r * (1.2 + 0.5 * flicker));
    ctx.lineTo(r * 0.4, r * 0.9);
    ctx.closePath();
    ctx.fill();
    ctx.restore();

    // Hull.
    ctx.shadowColor = '#16f2f2';
    ctx.shadowBlur = 16;
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#16f2f2';
    const hull = ctx.createLinearGradient(0, -r, 0, r);
    hull.addColorStop(0, 'rgba(22, 242, 242, 0.55)');
    hull.addColorStop(1, 'rgba(138, 43, 226, 0.35)');
    ctx.fillStyle = hull;
    ctx.beginPath();
    ctx.moveTo(0, -r);
    ctx.lineTo(r, r);
    ctx.lineTo(0, r * 0.45);
    ctx.lineTo(-r, r);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Cockpit.
    ctx.shadowBlur = 8;
    ctx.fillStyle = '#eaffff';
    ctx.beginPath();
    ctx.arc(0, -r * 0.1, r * 0.18, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
}

function drawSphere(ctx, s) {
    if (s.radius <= 0.2) {
        return;
    }
    ctx.save();
    const body = ctx.createRadialGradient(
        s.x - s.radius * 0.4, s.y - s.radius * 0.4, s.radius * 0.1,
        s.x, s.y, s.radius);
    if (s.isLife) {
        body.addColorStop(0, '#eaffea');
        body.addColorStop(0.5, '#57e08a');
        body.addColorStop(1, '#0b3a22');
        ctx.shadowColor = 'rgba(87, 224, 138, 0.8)';
    } else {
        body.addColorStop(0, '#ffffff');
        body.addColorStop(0.4, '#aebccd');
        body.addColorStop(1, '#20283f');
        ctx.shadowColor = 'rgba(160, 200, 255, 0.6)';
    }
    ctx.fillStyle = body;
    ctx.shadowBlur = 10;
    ctx.beginPath();
    ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
    ctx.fill();

    // Rim light.
    ctx.shadowBlur = 0;
    ctx.lineWidth = Math.max(1, s.radius * 0.08);
    ctx.strokeStyle = s.isLife ? 'rgba(180, 255, 200, 0.5)' : 'rgba(200, 225, 255, 0.45)';
    ctx.beginPath();
    ctx.arc(s.x, s.y, s.radius * 0.94, 0, Math.PI * 2);
    ctx.stroke();

    // Specular highlight.
    ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
    ctx.beginPath();
    ctx.arc(s.x - s.radius * 0.35, s.y - s.radius * 0.35, Math.max(1, s.radius * 0.16), 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
}

function drawProjectile(ctx, p) {
    ctx.save();
    ctx.shadowColor = '#ff2fd0';
    ctx.shadowBlur = 12;

    // Motion tail (opposite travel direction).
    const tail = p.radius * 6;
    const tx = p.x - p.dirX * tail;
    const ty = p.y - p.dirY * tail;
    const trail = ctx.createLinearGradient(p.x, p.y, tx, ty);
    trail.addColorStop(0, 'rgba(255, 47, 208, 0.9)');
    trail.addColorStop(1, 'rgba(255, 47, 208, 0)');
    ctx.strokeStyle = trail;
    ctx.lineWidth = p.radius * 1.6;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(p.x, p.y);
    ctx.lineTo(tx, ty);
    ctx.stroke();

    // Bright core.
    ctx.shadowBlur = 14;
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.radius * 1.1, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
}

function drawHud(ctx, model) {
    ctx.save();
    ctx.font = '20px Consolas, monospace';
    ctx.textBaseline = 'top';
    ctx.fillStyle = '#eaf6ff';
    ctx.shadowColor = '#16f2f2';
    ctx.shadowBlur = 8;
    ctx.fillText(`SCORE ${model.score}`, 20, 16);
    ctx.textAlign = 'center';
    ctx.fillText(`LEVEL ${model.level}`, ctx.canvas.width / 2, 16);
    ctx.textAlign = 'right';
    ctx.fillStyle = '#ff2fd0';
    ctx.shadowColor = '#ff2fd0';
    ctx.fillText(`LIVES ${model.lives}`, ctx.canvas.width - 20, 16);
    ctx.restore();
}

function drawLevelIntro(ctx, level) {
    ctx.save();
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.font = 'bold 72px Consolas, monospace';
    ctx.fillStyle = '#16f2f2';
    ctx.shadowColor = '#16f2f2';
    ctx.shadowBlur = 24;
    ctx.fillText(`LEVEL ${level}`, ctx.canvas.width / 2, ctx.canvas.height / 2);
    ctx.restore();
}

function draw(ctx, canvas, starfield, dt, model, now) {
    ctx.fillStyle = '#05010f';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    drawStarfield(ctx, canvas, starfield, dt);
    for (const s of model.spheres) {
        drawSphere(ctx, s);
    }
    for (const p of model.projectiles) {
        drawProjectile(ctx, p);
    }
    const hideShip = model.shipInvulnerable && Math.floor(now / 120) % 2 === 1;
    if (!hideShip) {
        drawShip(ctx, model.shipX, model.shipY, model.shipRadius);
    }
    drawHud(ctx, model);
    if (model.isLevelIntro) {
        drawLevelIntro(ctx, model.level);
    }
}

// ---------------------------------------------------------------------------
// Loop
// ---------------------------------------------------------------------------
export function start(canvas, arena, starfield, dotNetRef) {
    canvas.width = arena.width;
    canvas.height = arena.height;
    const ctx = canvas.getContext('2d');
    const layers = buildStarfield(arena, starfield);
    const beat = createBeat();
    beat.reflectState();

    const pressed = new Set();
    const move = { x: 0, y: 0 };
    const recompute = () => {
        let x = 0;
        let y = 0;
        for (const code of pressed) {
            const delta = MOVE_KEYS[code];
            if (delta) {
                x += delta[0];
                y += delta[1];
            }
        }
        move.x = clampAxis(x);
        move.y = clampAxis(y);
    };

    const onKeyDown = (e) => {
        beat.start(); // first gesture resumes/starts audio (respects mute)
        if (MOVE_KEYS[e.code]) {
            pressed.add(e.code);
            recompute();
            e.preventDefault();
        } else if (FIRE_KEYS[e.code]) {
            if (!e.repeat) {
                const [dx, dy] = FIRE_KEYS[e.code];
                dotNetRef.invokeMethod('Fire', dx, dy);
            }
            e.preventDefault();
        } else if (e.code === 'KeyM') {
            if (!e.repeat) {
                beat.toggleMuted();
            }
            e.preventDefault();
        }
    };
    const onKeyUp = (e) => {
        if (pressed.delete(e.code)) {
            recompute();
        }
    };
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);

    let running = true;
    const stop = () => {
        running = false;
        window.removeEventListener('keydown', onKeyDown);
        window.removeEventListener('keyup', onKeyUp);
        beat.stop();
    };

    let last = performance.now();
    const frame = (now) => {
        if (!running) {
            return;
        }
        const dt = Math.min(0.05, (now - last) / 1000);
        last = now;
        const model = dotNetRef.invokeMethod('Tick', dt, move.x, move.y);
        draw(ctx, canvas, layers, dt, model, now);
        if (model.isGameOver) {
            stop();
            dotNetRef.invokeMethodAsync('EndGame');
            return;
        }
        requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);

    return { stop };
}
