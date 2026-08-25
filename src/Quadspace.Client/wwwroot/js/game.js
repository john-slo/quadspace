// quadspace canvas render loop. Owns rendering, input (WASD move + arrow fire), the animated parallax
// starfield, a procedural Web Audio background beat (two layers), and sound effects. All gameplay
// state is authoritative in C#: each frame we call the .NET `Tick`; arrows call `Fire`; the audio beat
// calls `OnBeat` (beat-quantized spawns); `M` toggles music; `N` toggles SFX; game over calls `EndGame`.

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
// Procedural background beat (Web Audio): kick + bass + arp, plus an optional
// melodic second layer. Fires `onBeat` on each quarter note (for beat-synced
// spawning and the visual pulse). Mute state persists in localStorage.
// ---------------------------------------------------------------------------
function createBeat(options) {
    const MASTER_VOLUME = 0.22;
    const tempo = options.tempo || 128;
    const secondLayer = options.secondLayer !== false;
    const onBeat = options.onBeat || (() => { });
    const stepDuration = 60 / tempo / 2; // eighth notes
    const bass = [55.0, 55.0, 65.41, 55.0, 73.42, 55.0, 82.41, 65.41];
    const arp = [220.0, 261.63, 329.63, 440.0];
    const lead = [659.25, 0, 783.99, 880.0, 0, 783.99, 659.25, 0, 587.33, 0, 659.25, 0, 523.25, 0, 587.33, 0];

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
                setTimeout(onBeat, Math.max(0, (nextTime - ctx.currentTime) * 1000));
            }
            tone(bass[s], nextTime, stepDuration * 0.9, 'sawtooth', 0.18);
            tone(arp[Math.floor(step / 2) % arp.length] * 2, nextTime, stepDuration * 0.5, 'square', 0.05);
            if (secondLayer) {
                const note = lead[step % lead.length];
                if (note) {
                    tone(note, nextTime, stepDuration * 1.4, 'triangle', 0.05);
                }
            }
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
        setMuted(shouldBeMuted) {
            if (muted === shouldBeMuted) {
                return; // Already in desired state
            }
            this.toggleMuted(); // Use existing toggle logic if state differs
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
// Sound effects (fire / hit), toggled independently from the music.
// ---------------------------------------------------------------------------
function createSfx() {
    let ctx = null;
    let off = localStorage.getItem('quadspace-sfx') === '1';

    const ensure = () => {
        if (!ctx) {
            const Ctor = window.AudioContext || window.webkitAudioContext;
            ctx = new Ctor();
        }
        if (ctx.state === 'suspended') {
            ctx.resume();
        }
    };

    const blip = (from, to, dur, type, volume) => {
        if (off) {
            return;
        }
        ensure();
        const t = ctx.currentTime;
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(from, t);
        osc.frequency.exponentialRampToValueAtTime(Math.max(1, to), t + dur);
        gain.gain.setValueAtTime(volume, t);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + dur);
        osc.connect(gain).connect(ctx.destination);
        osc.start(t);
        osc.stop(t + dur + 0.02);
    };

    const reflectState = () => {
        const indicator = document.getElementById('sfx-state');
        if (indicator) {
            indicator.textContent = off ? 'OFF' : 'ON';
        }
    };

    return {
        fire() {
            blip(520, 180, 0.06, 'triangle', 0.05);
        },
        hit() {
            blip(150, 42, 0.18, 'sine', 0.16);
        },
        shipHit() {
            blip(320, 55, 0.35, 'sawtooth', 0.22);
        },
        toggle() {
            off = !off;
            localStorage.setItem('quadspace-sfx', off ? '1' : '0');
            reflectState();
        },
        reflectState,
        stop() {
            if (ctx) {
                ctx.close();
                ctx = null;
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

function drawSphere(ctx, s, scale) {
    const r = s.radius * scale;
    if (r <= 0.2) {
        return;
    }
    ctx.save();
    const body = ctx.createRadialGradient(
        s.x - r * 0.4, s.y - r * 0.4, r * 0.1,
        s.x, s.y, r);
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
    ctx.arc(s.x, s.y, r, 0, Math.PI * 2);
    ctx.fill();

    // Rim light.
    ctx.shadowBlur = 0;
    ctx.lineWidth = Math.max(1, r * 0.08);
    ctx.strokeStyle = s.isLife ? 'rgba(180, 255, 200, 0.5)' : 'rgba(200, 225, 255, 0.45)';
    ctx.beginPath();
    ctx.arc(s.x, s.y, r * 0.94, 0, Math.PI * 2);
    ctx.stroke();

    // Specular highlight.
    ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
    ctx.beginPath();
    ctx.arc(s.x - r * 0.35, s.y - r * 0.35, Math.max(1, r * 0.16), 0, Math.PI * 2);
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

function draw(ctx, canvas, starfield, dt, model, now, pulse) {
    ctx.fillStyle = '#05010f';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    drawStarfield(ctx, canvas, starfield, dt);
    for (const s of model.spheres) {
        drawSphere(ctx, s, pulse);
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
export function start(canvas, arena, starfield, options, dotNetRef) {
    if (!arena || arena.width <= 0 || arena.height <= 0) {
        throw new Error('Invalid arena dimensions: width and height must be greater than 0');
    }
    canvas.width = arena.width;
    canvas.height = arena.height;
    console.debug(`[Game] Canvas initialized: ${arena.width}x${arena.height}`);

    const ctx = canvas.getContext('2d');
    if (!ctx) {
        throw new Error('Failed to get 2D context from canvas');
    }
    const layers = buildStarfield(arena, starfield);
    const beatPulse = options.beatPulse || 0;
    console.debug(`[Game] Game start: beatPulse=${beatPulse}, layers=${layers.length}`);

    let running = true;
    let lastBeatAt = performance.now();
    const onBeat = () => {
        if (!running) {
            return;
        }
        lastBeatAt = performance.now();
        try {
            dotNetRef.invokeMethod('OnBeat');
        } catch {
            // Component was disposed between scheduling and firing the beat.
        }
    };

    const beat = createBeat({ tempo: options.beatsPerMinute, secondLayer: options.secondLayer, onBeat });
    const sfx = createSfx();
    beat.reflectState();
    sfx.reflectState();

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
                sfx.fire();
            }
            e.preventDefault();
        } else if (e.code === 'KeyM') {
            if (!e.repeat) {
                beat.toggleMuted();
            }
            e.preventDefault();
        } else if (e.code === 'KeyN') {
            if (!e.repeat) {
                sfx.toggle();
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

    const stop = () => {
        running = false;
        window.removeEventListener('keydown', onKeyDown);
        window.removeEventListener('keyup', onKeyUp);
        beat.stop();
        sfx.stop();
    };

    let last = null; // Initialize on first frame to avoid negative elapsed time at startup
    let prevScore = 0;
    let prevLives = null;
    let frameCount = 0;
    let lastErrorTime = 0;
    const MIN_DT = 0.001; // Minimum delta time to prevent division by zero in C#

    const frame = (now) => {
        if (!running) {
            return;
        }

        try {
            frameCount++;

            // Initialize last on first frame to avoid negative elapsed time at startup.
            // Bug history: Initializing last = performance.now() before first callback could
            // cause negative elapsed time if browser timer resolution or async timing shifts.
            if (last === null) {
                last = now;
                requestAnimationFrame(frame);
                return;
            }

            // Validate input timestamp
            if (typeof now !== 'number' || !isFinite(now)) {
                console.error(`[Frame ${frameCount}] Invalid timestamp: now=${now}`);
                throw new Error(`Invalid animation frame timestamp: ${now}`);
            }

            const elapsed = now - last;

            // Skip this frame if time went backwards (can happen with tab suspension or system clock adjustments)
            if (elapsed < 0) {
                last = now;
                requestAnimationFrame(frame);
                return;
            }

            // Calculate dt: clamp to reasonable range, ensure minimum to prevent division by zero in C#.
            // MIN_DT (0.001s) guards against edge cases where dt is so small it could cause divide-by-zero
            // in C# physics calculations or config-driven logic (e.g., modulo operations).
            const rawDt = elapsed / 1000;
            const clampedDt = Math.min(0.05, rawDt);
            const dt = Math.max(MIN_DT, clampedDt);

            // Validate dt value
            if (!isFinite(dt) || dt < 0) {
                throw new Error(`Invalid delta time: ${dt}`);
            }

            last = now;

            // Invoke game tick
            let model;
            try {
                model = dotNetRef.invokeMethod('Tick', dt, move.x, move.y);
            } catch (e) {
                console.error(`Tick invocation failed:`, e);
                throw new Error(`Tick failed: ${e.message}`);
            }

            // Validate model object
            if (!model || typeof model !== 'object') {
                throw new Error(`Invalid model returned from Tick`);
            }

            if (model.score > prevScore) {
                sfx.hit();
            }
            prevScore = model.score;

            if (prevLives !== null && model.lives < prevLives) {
                sfx.shipHit();
            }
            prevLives = model.lives;

            // Calculate pulse
            const timeSinceBeat = now - lastBeatAt;
            const pulse = 1 + beatPulse * Math.exp(-6 * timeSinceBeat / 1000);

            if (!isFinite(pulse)) {
                throw new Error(`Invalid pulse calculated`);
            }

            // Draw with error context
            try {
                draw(ctx, canvas, layers, dt, model, now, pulse);
            } catch (e) {
                console.error(`[Frame ${frameCount}] Draw failed:`, e);
                throw new Error(`Draw failed: ${e.message}`);
            }

            if (model.isGameOver) {
                stop();
                dotNetRef.invokeMethodAsync('EndGame');
                return;
            }

            requestAnimationFrame(frame);
        } catch (error) {
            // Log error and continue animation loop (graceful degradation)
            if (performance.now() - lastErrorTime > 1000) {
                console.error(`Game loop error:`, error);
                lastErrorTime = performance.now();
            }
            requestAnimationFrame(frame);
        }
    };
    requestAnimationFrame(frame);

    return { stop };
}

// ---------------------------------------------------------------------------
// Home / attract screen: plays the same background beat and pulses the
// PRESS START element (id="press-start") in sync with each beat. Audio starts
// on the first user gesture (browser autoplay policy).
// ---------------------------------------------------------------------------
export function startAttract(options) {
    let resetTimer = null;
    const pulse = () => {
        const el = document.getElementById('press-start');
        if (!el) {
            return;
        }
        el.style.transform = 'scale(1.22)';
        if (resetTimer) {
            clearTimeout(resetTimer);
        }
        resetTimer = setTimeout(() => {
            const still = document.getElementById('press-start');
            if (still) {
                still.style.transform = 'scale(1)';
            }
        }, 140);
    };

    const beat = createBeat({ tempo: options.beatsPerMinute, secondLayer: options.secondLayer, onBeat: pulse });
    beat.reflectState();

    const startOnce = () => beat.start();
    window.addEventListener('pointerdown', startOnce, { once: true });
    window.addEventListener('keydown', startOnce, { once: true });

    return {
        play() {
            // Ensure audio is unmuted and playing
            beat.setMuted(false);
            beat.start();
        },
        pause() {
            // Ensure audio is muted (paused)
            beat.setMuted(true);
        },
        stop() {
            window.removeEventListener('pointerdown', startOnce);
            window.removeEventListener('keydown', startOnce);
            if (resetTimer) {
                clearTimeout(resetTimer);
            }
            beat.stop();
        },
    };
}
