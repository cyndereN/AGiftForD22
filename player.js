// Free camera for the SuperSplat scenes.
//
// PlayCanvas is Y-up. WASD moves along the look direction, so looking up or
// down moves vertically too. Nothing pins the camera to the ground. The only
// limit is the scene box: the player cannot leave the splat bounds.

const WALK = {
    // scene_live: { minX: -4, maxX: 4, minY: 0, maxY: 3, minZ: -6, maxZ: 3 },
    // scene_hutong: { minX: -3, maxX: 3, minY: -1, maxY: 4, minZ: -8, maxZ: 2 },
    // scene_performance: { minX: -5, maxX: 5, minY: 0, maxY: 3, minZ: -5, maxZ: 5 },
    // scene_recordshop: { minX: -3, maxX: 3, minY: 0, maxY: 3, minZ: -4, maxZ: 4 }
};

const TITLES = {
    scene_live: "Live House",
    scene_hutong: "胡同",
    scene_performance: "演出",
    scene_recordshop: "唱片店"
};

// Later skills fill the same boxes. Empty slots stay blank until learned.
const SKILLS = {
    walk: { mark: "👣", title: "行走" },
    drink: { mark: "🍺", title: "喝酒" },
    guitar: { mark: "🎸", title: "吉他" },
    drums: { mark: "🥁", title: "鼓" },
    bass: { mark: "🎵", title: "贝斯" }
};

const SLOT_ORDER = ["walk", "drink", "guitar", "drums", "bass"];

const BRIEFS = {
    scene_recordshop: [
        {
            zh: "唱片店是翻音乐的房间。黑胶和 CD 立在格子里，抽出来，就能听。",
            en: "A record shop is a room for flipping through music. Vinyl and CDs stand in the bins. Pull one out, and you can listen."
        },
        {
            zh: "北京还有这样的店。独音唱片 2011 年开在鼓楼东大街，自己也做厂牌。痛仰、旅行团的片子从这里出去过。",
            en: "Beijing still has rooms like this. Duyin Records opened on Gulou East Street in 2011, and became a label. Miserable Faith and The Life Journey passed through here."
        },
        {
            zh: "福声更早。2002 年开在平安里，2018 年搬到冰窖口胡同。架子上是崔健、唐朝、黑豹，还有窦唯。",
            en: "Fusheng is older. It opened near Ping'anli in 2002 and moved to Bingjiaokou Hutong in 2018. The shelves keep Cui Jian, Tang Dynasty, Black Panther, and Dou Wei."
        },
        {
            zh: "你脚下这间，也是这种店。走两步看看。",
            en: "The room under your feet is that kind of shop. Take a couple of steps."
        }
    ]
};

(() => {
    const style = document.createElement("style");
    style.textContent = `
        #controlsWrap,
        #settingsPanel,
        #infoPanel,
        #viewerBranding,
        #joystickBase,
        #annotationNav,
        #xrModal,
        #tooltip,
        #walkHint {
            display: none !important;
        }
        #moveHint {
            position: fixed;
            left: 50%;
            bottom: max(24px, env(safe-area-inset-bottom));
            transform: translateX(-50%);
            color: #f4f0ea;
            font: 13px/1.4 "Segoe UI", "PingFang SC", sans-serif;
            letter-spacing: 0.06em;
            background: rgba(0, 0, 0, 0.5);
            padding: 8px 16px;
            border-radius: 999px;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.45s ease;
            z-index: 4;
        }
        #moveHint.show {
            opacity: 1;
        }
        #menuBack {
            position: fixed;
            top: max(16px, env(safe-area-inset-top));
            left: max(16px, env(safe-area-inset-left));
            z-index: 12;
            color: #f4efe6;
            text-decoration: none;
            font: 13px/1 "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
            letter-spacing: 0.16em;
            background: rgba(0, 0, 0, 0.55);
            padding: 10px 14px;
        }
        #hud {
            position: fixed;
            left: max(16px, env(safe-area-inset-left));
            bottom: max(16px, env(safe-area-inset-bottom));
            z-index: 7;
            display: flex;
            gap: 8px;
        }
        #hud button {
            width: 64px;
            height: 64px;
            padding: 0;
            border: 1px solid rgba(244, 239, 230, 0.35);
            background: rgba(7, 8, 7, 0.55);
            color: #f4efe6;
            font-size: 28px;
            line-height: 1;
            cursor: default;
            opacity: 1;
        }
        #hud button.got {
            border-color: #f4efe6;
            background: rgba(7, 8, 7, 0.82);
        }
        #hud button.hot {
            border-color: #fff;
            background: rgba(244, 239, 230, 0.28);
            box-shadow: 0 0 14px rgba(244, 239, 230, 0.9);
        }
        .skillFly {
            position: fixed;
            z-index: 11;
            width: 72px;
            height: 72px;
            margin-left: -36px;
            margin-top: -36px;
            display: grid;
            place-items: center;
            font-size: 40px;
            pointer-events: none;
            transform: scale(0.15);
        }
        #brief {
            position: fixed;
            inset: 0;
            z-index: 8;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 24px;
            background: rgba(0, 0, 0, 0.62);
            color: #f4efe6;
            font-family: "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
        }
        #brief .cols {
            width: min(920px, 100%);
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 28px 48px;
        }
        #brief p {
            font-size: clamp(18px, 2.1vw, 28px);
            line-height: 1.45;
            min-height: 4.4em;
        }
        #brief .en {
            font-family: Georgia, "Times New Roman", serif;
            font-style: italic;
            color: rgba(244, 239, 230, 0.88);
        }
        #briefSkip {
            position: absolute;
            right: max(20px, env(safe-area-inset-right));
            bottom: max(18px, env(safe-area-inset-bottom));
            background: none;
            border: 0;
            color: rgba(244, 239, 230, 0.62);
            font: inherit;
            letter-spacing: 0.16em;
            font-size: 12px;
            cursor: pointer;
        }
        @media (max-width: 720px) {
            #brief .cols { grid-template-columns: 1fr; gap: 12px; }
            #brief p { min-height: 3em; font-size: 18px; }
            #hud button { width: 52px; height: 52px; }
        }
        @media (prefers-reduced-motion: reduce) {
            .skillFly { transition: none !important; }
        }
    `;
    document.documentElement.appendChild(style);

    const keys = new Set();
    const look = { dragging: false };
    const MOVE_KEYS = { KeyW: "w", KeyA: "a", KeyS: "s", KeyD: "d" };
    const learned = new Set();

    let enabled = false;
    let briefing = false;
    let hud = null;
    let bounds = null;
    let x = 0;
    let y = 0;
    let z = 0;
    let yaw = 180;
    let pitch = 0;
    let speed = 1.2;
    let hint = null;
    let lastWall = 0;

    const sceneKey = () => {
        const file = (location.pathname.split("/").pop() || "").replace(/\.html?$/i, "");
        return file || "scene";
    };

    const boundsFromSplat = (sceneBound) => {
        const center = sceneBound.center;
        const half = sceneBound.halfExtents;
        const spanX = half.x * 2;
        const spanY = half.y * 2;
        const spanZ = half.z * 2;
        if (spanX < 0.05 || spanZ < 0.05 || spanY < 0.05) {
            return null;
        }
        // The exported camera often sits just outside the splat shell.
        const margin = 1.5;
        return {
            minX: center.x - half.x - margin,
            maxX: center.x + half.x + margin,
            minY: center.y - half.y - margin,
            maxY: center.y + half.y + margin,
            minZ: center.z - half.z - margin,
            maxZ: center.z + half.z + margin,
            spanX,
            spanY,
            spanZ
        };
    };

    const resolveBounds = (sceneBound) => {
        const manual = WALK[sceneKey()];
        if (manual) {
            return {
                minX: manual.minX,
                maxX: manual.maxX,
                minY: manual.minY,
                maxY: manual.maxY,
                minZ: manual.minZ,
                maxZ: manual.maxZ,
                spanX: manual.maxX - manual.minX,
                spanY: manual.maxY - manual.minY,
                spanZ: manual.maxZ - manual.minZ
            };
        }
        return boundsFromSplat(sceneBound);
    };

    const hideHint = () => {
        if (hint) hint.classList.remove("show");
    };

    const mountHint = () => {
        if (!document.body || hint || briefing) return;
        hint = document.createElement("div");
        hint.id = "moveHint";
        hint.textContent = learned.has("walk")
            ? "点击画面转向 · WASD 自由移动"
            : "现在可以用 WASD 移动 · 点击画面转向";
        document.body.appendChild(hint);
        requestAnimationFrame(() => hint.classList.add("show"));
    };

    const loadLearned = () => {
        try {
            const saved = JSON.parse(sessionStorage.getItem("d22-skillbook") || "[]");
            if (Array.isArray(saved)) saved.forEach((id) => learned.add(id));
        } catch (err) {}
    };

    const saveLearned = () => {
        try {
            sessionStorage.setItem("d22-skillbook", JSON.stringify([...learned]));
        } catch (err) {}
    };

    const fillSlot = (id) => {
        const skill = SKILLS[id];
        const slot = hud?.querySelector(`[data-skill="${id}"]`);
        if (!skill || !slot) return;
        slot.classList.add("got");
        slot.textContent = skill.mark;
        slot.disabled = false;
        slot.title = skill.title;
        slot.setAttribute("aria-label", skill.title);
    };

    const learnSkill = (id) => {
        const skill = SKILLS[id];
        const slot = hud?.querySelector(`[data-skill="${id}"]`);
        if (!skill || !slot || learned.has(id)) return;
        learned.add(id);
        saveLearned();
        if (matchMedia("(prefers-reduced-motion: reduce)").matches) {
            fillSlot(id);
            return;
        }
        const fly = document.createElement("div");
        fly.className = "skillFly";
        fly.textContent = skill.mark;
        const cx = window.innerWidth / 2;
        const cy = window.innerHeight / 2;
        fly.style.transition = "none";
        fly.style.left = `${cx}px`;
        fly.style.top = `${cy}px`;
        document.body.appendChild(fly);
        requestAnimationFrame(() => {
            fly.style.transition = "transform 0.46s cubic-bezier(.2, 1.45, .35, 1)";
            fly.style.transform = "scale(1.75)";
            setTimeout(() => {
                const box = slot.getBoundingClientRect();
                const land = (event) => {
                    if (event.propertyName !== "left") return;
                    fly.removeEventListener("transitionend", land);
                    fly.remove();
                    fillSlot(id);
                };
                fly.addEventListener("transitionend", land);
                fly.style.transition = "left 0.62s cubic-bezier(.5, .02, .25, 1), top 0.62s cubic-bezier(.5, .02, .25, 1), transform 0.62s ease";
                fly.style.left = `${box.left + box.width / 2}px`;
                fly.style.top = `${box.top + box.height / 2}px`;
                fly.style.transform = "scale(0.78)";
            }, 680);
        });
    };

    const mountHud = () => {
        if (!document.body || hud) return;
        hud = document.createElement("div");
        hud.id = "hud";
        for (const id of SLOT_ORDER) {
            const slot = document.createElement("button");
            slot.type = "button";
            slot.dataset.skill = id;
            slot.disabled = true;
            slot.setAttribute("aria-label", "空位");
            hud.appendChild(slot);
            if (learned.has(id)) fillSlot(id);
        }
        document.body.appendChild(hud);
    };

    const mountMenu = () => {
        if (!document.body || document.getElementById("menuBack")) return;
        const link = document.createElement("a");
        link.id = "menuBack";
        link.href = "index.html";
        link.textContent = "主菜单";
        link.addEventListener("click", () => document.exitPointerLock?.());
        document.body.appendChild(link);
    };

    const playBrief = async () => {
        const beats = BRIEFS[sceneKey()];
        if (!beats || !document.body) return;
        briefing = true;
        const root = document.createElement("div");
        root.id = "brief";
        root.innerHTML = `<div class="cols"><p class="zh"></p><p class="en"></p></div><button id="briefSkip" type="button">跳过</button>`;
        document.body.appendChild(root);
        const zhEl = root.querySelector(".zh");
        const enEl = root.querySelector(".en");
        let skipped = false;
        root.querySelector("#briefSkip").addEventListener("click", () => {
            skipped = true;
        });
        const reduce = matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reduce) {
            zhEl.textContent = beats.map((beat) => beat.zh).join("\n\n");
            enEl.textContent = beats.map((beat) => beat.en).join("\n\n");
            await new Promise((resolve) => {
                root.querySelector("#briefSkip").addEventListener("click", () => resolve(), { once: true });
            });
        } else {
            for (const beat of beats) {
                if (skipped) break;
                const steps = Math.max(beat.zh.length, beat.en.length);
                for (let i = 1; i <= steps; i++) {
                    if (skipped) break;
                    zhEl.textContent = beat.zh.slice(0, Math.ceil(beat.zh.length * i / steps));
                    enEl.textContent = beat.en.slice(0, Math.ceil(beat.en.length * i / steps));
                    await new Promise((resolve) => setTimeout(resolve, 26));
                }
                if (!skipped) await new Promise((resolve) => setTimeout(resolve, 520));
            }
        }
        root.remove();
        keys.clear();
        briefing = false;
        mountHint();
    };

    const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

    const onKeyDown = (event) => {
        const key = MOVE_KEYS[event.code];
        if (!key) return;
        keys.add(key);
        hideHint();
        event.preventDefault();
    };

    const onKeyUp = (event) => {
        const key = MOVE_KEYS[event.code];
        if (key) keys.delete(key);
    };

    const onPointerDown = (event) => {
        if (!enabled || event.button !== 0) return;
        look.dragging = true;
        event.currentTarget.setPointerCapture?.(event.pointerId);
        event.currentTarget.requestPointerLock?.();
        hideHint();
    };

    const onPointerUp = () => {
        look.dragging = false;
    };

    const onPointerMove = (event) => {
        const locked = document.pointerLockElement != null;
        if (!enabled || (!look.dragging && !locked)) return;
        yaw -= event.movementX * 0.15;
        pitch = Math.max(-75, Math.min(75, pitch - event.movementY * 0.12));
    };

    const attachInput = (canvas) => {
        window.addEventListener("keydown", onKeyDown);
        window.addEventListener("keyup", onKeyUp);
        canvas.addEventListener("pointerdown", onPointerDown);
        window.addEventListener("pointerup", onPointerUp);
        window.addEventListener("pointermove", onPointerMove);
        document.addEventListener("pointerlockchange", () => {
            if (!document.pointerLockElement) look.dragging = false;
        });
    };

    const start = (scene) => {
        const next = resolveBounds(scene.sceneBound);
        if (!next) return;
        bounds = next;
        const manual = WALK[sceneKey()];
        if (manual) {
            x = (manual.minX + manual.maxX) / 2;
            y = (manual.minY + manual.maxY) / 2;
            z = (manual.minZ + manual.maxZ) / 2;
        } else {
            // SuperSplat exports the standing camera at the origin, looking +Z.
            x = clamp(0, bounds.minX, bounds.maxX);
            y = clamp(0, bounds.minY, bounds.maxY);
            z = clamp(0, bounds.minZ, bounds.maxZ);
        }
        const span = Math.max(bounds.spanX, bounds.spanY, bounds.spanZ);
        speed = Math.min(1.4, Math.max(0.45, span / 50));
        yaw = 180;
        pitch = 0;
        enabled = true;
        const title = TITLES[sceneKey()];
        if (title) document.title = title;
        mountHint();
        const canvas = document.getElementById("application-canvas");
        if (canvas) attachInput(canvas);
    };

    window.__player = {
        get enabled() {
            return enabled;
        },
        update(dt) {
            const now = performance.now();
            const wallDt = lastWall ? (now - lastWall) / 1000 : dt;
            lastWall = now;
            const stepDt = Math.min(Math.max(wallDt, 0), 0.05);
            const yawRad = yaw * Math.PI / 180;
            const pitchRad = pitch * Math.PI / 180;
            const cosPitch = Math.cos(pitchRad);
            const forwardX = -Math.sin(yawRad) * cosPitch;
            const forwardY = Math.sin(pitchRad);
            const forwardZ = -Math.cos(yawRad) * cosPitch;
            const rightX = Math.cos(yawRad);
            const rightZ = -Math.sin(yawRad);
            let moveX = 0;
            let moveY = 0;
            let moveZ = 0;
            if (keys.has("w")) {
                moveX += forwardX;
                moveY += forwardY;
                moveZ += forwardZ;
            }
            if (keys.has("s")) {
                moveX -= forwardX;
                moveY -= forwardY;
                moveZ -= forwardZ;
            }
            if (keys.has("d")) {
                moveX += rightX;
                moveZ += rightZ;
            }
            if (keys.has("a")) {
                moveX -= rightX;
                moveZ -= rightZ;
            }
            const length = Math.hypot(moveX, moveY, moveZ);
            const walkSlot = hud?.querySelector('[data-skill="walk"]');
            if (walkSlot?.classList.contains("got")) {
                walkSlot.classList.toggle("hot", length > 0 && !briefing);
            }
            if (length > 0 && !briefing) {
                if (!learned.has("walk")) learnSkill("walk");
                const scale = (speed * stepDt) / length;
                x = clamp(x + moveX * scale, bounds.minX, bounds.maxX);
                y = clamp(y + moveY * scale, bounds.minY, bounds.maxY);
                z = clamp(z + moveZ * scale, bounds.minZ, bounds.maxZ);
            }
            return { x, y, z, pitch, yaw, fov: 60 };
        }
    };

    const wait = () => {
        if (window.__scene && !enabled) start(window.__scene);
        if (!enabled) requestAnimationFrame(wait);
    };

    const boot = () => {
        const key = sceneKey();
        if (TITLES[key]) document.title = TITLES[key];
        loadLearned();
        mountMenu();
        mountHud();
        window.__skills = { learn: learnSkill };
        playBrief();
        wait();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
