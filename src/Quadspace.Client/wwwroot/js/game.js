// quadspace canvas render loop. Owns rendering, input (WASD move + arrow fire), and the animated
// parallax starfield. All gameplay state is authoritative in C#: each frame we call the .NET `Tick`
// (movement) and draw what it returns; arrow presses call `Fire` directly; game over calls `EndGame`.

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

function drawShip(ctx, x, y, r) {
    ctx.save();
    ctx.translate(x, y);
    ctx.shadowColor = '#16f2f2';
    ctx.shadowBlur = 16;
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#16f2f2';
    ctx.fillStyle = 'rgba(22, 242, 242, 0.25)';
    ctx.beginPath();
    ctx.moveTo(0, -r);
    ctx.lineTo(r, r);
    ctx.lineTo(0, r * 0.45);
    ctx.lineTo(-r, r);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.restore();
}

function drawSphere(ctx, s) {
    if (s.radius <= 0.2) {
        return;
    }
    ctx.save();
    const gradient = ctx.createRadialGradient(
        s.x - s.radius * 0.35, s.y - s.radius * 0.35, s.radius * 0.1,
        s.x, s.y, s.radius);
    if (s.isLife) {
        gradient.addColorStop(0, '#eaffea');
        gradient.addColorStop(0.45, '#57e08a');
        gradient.addColorStop(1, '#0b3a22');
        ctx.shadowColor = 'rgba(87, 224, 138, 0.8)';
    } else {
        gradient.addColorStop(0, '#f4f8ff');
        gradient.addColorStop(0.45, '#9fb4c8');
        gradient.addColorStop(1, '#2b3550');
        ctx.shadowColor = 'rgba(160, 200, 255, 0.6)';
    }
    ctx.fillStyle = gradient;
    ctx.shadowBlur = 8;
    ctx.beginPath();
    ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
}

function drawProjectile(ctx, p) {
    ctx.save();
    ctx.shadowColor = '#ff2fd0';
    ctx.shadowBlur = 10;
    ctx.fillStyle = '#ff2fd0';
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
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

export function start(canvas, arena, starfield, dotNetRef) {
    canvas.width = arena.width;
    canvas.height = arena.height;
    const ctx = canvas.getContext('2d');
    const layers = buildStarfield(arena, starfield);

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
