// quadspace canvas render loop. Owns rendering, WASD input, and the animated parallax starfield.
// All gameplay state is authoritative in C#: each frame we call the .NET `Tick` and draw what it returns.

const MOVE_KEYS = {
    KeyW: [0, -1],
    KeyS: [0, 1],
    KeyA: [-1, 0],
    KeyD: [1, 0],
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

function draw(ctx, canvas, starfield, dt, model) {
    ctx.fillStyle = '#05010f';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

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

    drawShip(ctx, model.shipX, model.shipY, model.shipRadius);
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
    let last = performance.now();
    const frame = (now) => {
        if (!running) {
            return;
        }
        const dt = Math.min(0.05, (now - last) / 1000);
        last = now;
        const model = dotNetRef.invokeMethod('Tick', dt, move.x, move.y);
        draw(ctx, canvas, layers, dt, model);
        requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);

    return {
        stop() {
            running = false;
            window.removeEventListener('keydown', onKeyDown);
            window.removeEventListener('keyup', onKeyUp);
        },
    };
}
