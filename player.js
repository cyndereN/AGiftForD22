// Free camera for the SuperSplat scenes.
//
// PlayCanvas is Y-up. WASD moves along the look direction, so looking up or
// down moves vertically too. Nothing pins the camera to the ground. The only
// limit is the scene box: the player cannot leave the splat bounds.
//
// Walk sits at the bottom right. Interactive abilities sit at the bottom left
// and open with the number keys 1-7.

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

const SKILLS = {
    walk: { mark: "👣", title: "行走" },
    drink: { mark: "🍺", title: "喝酒" },
    guitar: { mark: "🎸", title: "吉他" },
    drums: { mark: "🥁", title: "鼓" },
    bass: { mark: "🎵", title: "贝斯" }
};

// Bottom-left boxes. Empty ids stay blank until a later ability is added.
const INTERACTIVE = [
    { id: "drink", key: "1" },
    { id: "guitar", key: "2" },
    { id: "drums", key: "3" },
    { id: "bass", key: "4" },
    { id: "slot5", key: "5" },
    { id: "slot6", key: "6" },
    { id: "slot7", key: "7" }
];

const HOTKEYS = {
    Digit1: "drink",
    Digit2: "guitar",
    Digit3: "drums",
    Digit4: "bass",
    Digit5: "slot5",
    Digit6: "slot6",
    Digit7: "slot7"
};

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
    ],
    scene_hutong: [
        {
            zh: "酒还在嗓子里。胡同到了。先走到那扇门前。",
            en: "The drink is still in the throat. The hutong is here. Walk to the door first."
        }
    ]
};

const BOSS = [
    {
        zh: "吃了么您。着急去看演出？",
        en: "Eaten yet? Rushing off to the show?"
    },
    {
        zh: "门口那帮人，演完没有不喝的。你先别走。",
        en: "The ones at the door, none of them leave a show sober. Don't go yet."
    },
    {
        zh: "身后。刚给你搁上的。走之前拿一下。",
        en: "Behind you. I just set it out. Take it before you leave."
    }
];

const WINE = [
    {
        zh: "酒并不为了解渴。台上声音一大，人就需要一点东西把白天关掉。",
        en: "The drink is not for thirst. When the sound onstage gets big, people need something to switch the day off."
    },
    {
        zh: "尼采管这叫酒神。不是神话，是那一下边界松了，嗓子和身子一起往前冲。",
        en: "Nietzsche called it the Dionysian. Not the myth. The moment the edges loosen, and the voice and the body rush forward together."
    },
    {
        zh: "所以演出必喝酒。音量把情感放大，酒把放大按实，让它停在这间屋子里。",
        en: "So a show means drinking. Not for the crowd. The volume turns the feeling up. The drink holds it down, keeps it in the room."
    },
    {
        zh: "D-22 也是这样。演完坐穿馆，喝到天亮。酒是那场噪声还没散的时候，留下的那一口。",
        en: "D-22 was the same. After the set they sat the place through, and drank until morning. The drink is the mouthful left while the noise is still in the room."
    },
    {
        zh: "拿上。酒杯。这是你会的第二件事。",
        en: "Take the glass. This is the second thing you know."
    }
];

const ABILITY_INTRO = {
    drink: [
        {
            zh: "情感放大器。莫贪杯哦。",
            en: "Emotion amplifier. Try not to overdose."
        }
    ]
};

const PREVIEW = { x: 0, y: -120, z: 0 };

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
        #moveHint,
        #useHint {
            position: fixed;
            left: 50%;
            bottom: 108px;
            transform: translateX(-50%);
            color: #f4f0ea;
            font: 13px/1.4 "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
            letter-spacing: 0.06em;
            background: rgba(0, 0, 0, 0.5);
            padding: 8px 16px;
            border-radius: 999px;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.45s ease;
            z-index: 6;
        }
        #moveHint.show,
        #useHint.show {
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
        #hudWalk {
            position: fixed;
            right: max(16px, env(safe-area-inset-right));
            bottom: max(16px, env(safe-area-inset-bottom));
            z-index: 7;
        }
        #hud button,
        #hudWalk button {
            position: relative;
            width: 56px;
            height: 56px;
            padding: 0;
            border: 1px solid rgba(244, 239, 230, 0.35);
            background: rgba(7, 8, 7, 0.55);
            color: #f4efe6;
            font-size: 26px;
            line-height: 1;
            cursor: default;
        }
        #hudWalk button {
            width: 64px;
            height: 64px;
        }
        #hud button .key {
            position: absolute;
            left: 5px;
            top: 4px;
            font: 11px/1 "Segoe UI", "PingFang SC", sans-serif;
            letter-spacing: 0;
            opacity: 0.72;
            pointer-events: none;
        }
        #hud button .cd {
            position: absolute;
            inset: 0;
            display: grid;
            place-items: center;
            font: 18px/1 "Segoe UI", "PingFang SC", sans-serif;
            background: rgba(0, 0, 0, 0.62);
            pointer-events: none;
        }
        #hud button:disabled {
            opacity: 1;
            color: #f4efe6;
        }
        #hud button.got,
        #hudWalk button.got {
            border-color: #f4efe6;
            background: rgba(7, 8, 7, 0.82);
        }
        #hud button.got {
            cursor: pointer;
        }
        #hud button.hot,
        #hudWalk button.hot {
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
            z-index: 10;
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
        #ability {
            position: fixed;
            inset: 0;
            z-index: 9;
            display: none;
        }
        #ability.on {
            display: block;
        }
        #abilityFrame {
            position: absolute;
            left: 50%;
            top: 46%;
            width: min(440px, 78vmin);
            height: min(440px, 78vmin);
            transform: translate(-50%, -50%);
            border: 1px solid rgba(244, 239, 230, 0.88);
            background: #141311;
            box-shadow: 0 0 0 100vmax rgba(0, 0, 0, 0.46);
            color: #f4efe6;
            font-family: "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
        }
        #abilityFrame.live {
            background: transparent;
        }
        #abilityName {
            position: absolute;
            top: 14px;
            left: 16px;
            margin: 0;
            letter-spacing: 0.18em;
            font-size: 14px;
            pointer-events: none;
        }
        #abilityCaption {
            position: absolute;
            left: 0;
            right: 0;
            bottom: 16px;
            margin: 0;
            text-align: center;
            letter-spacing: 0.14em;
            font-size: 13px;
            pointer-events: none;
        }
        #abilityClose {
            position: absolute;
            top: 8px;
            right: 8px;
            width: 36px;
            height: 36px;
            border: 0;
            background: transparent;
            color: rgba(244, 239, 230, 0.8);
            font-size: 22px;
            cursor: pointer;
        }
        #bottleFallback {
            position: absolute;
            left: 50%;
            bottom: 22%;
            width: 86px;
            height: 210px;
            margin-left: -43px;
            transform-origin: 50% 100%;
            pointer-events: none;
        }
        #abilityFrame.live #bottleFallback {
            visibility: hidden;
        }
        #bottleFallback .body {
            position: absolute;
            left: 6px;
            right: 6px;
            bottom: 0;
            height: 138px;
            border: 2px solid rgba(244, 239, 230, 0.82);
            border-radius: 16px 16px 10px 10px;
            background: #6a2430;
        }
        #bottleFallback .neck {
            position: absolute;
            left: 28px;
            right: 28px;
            bottom: 136px;
            height: 52px;
            border: 2px solid rgba(244, 239, 230, 0.82);
            border-bottom: 0;
            background: rgba(106, 36, 48, 0.45);
        }
        @media (max-width: 720px) {
            #brief .cols { grid-template-columns: 1fr; gap: 12px; }
            #brief p { min-height: 3em; font-size: 18px; }
            #hud button,
            #hudWalk button { width: 48px; height: 48px; }
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
    const heard = new Set();

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
    let useHint = null;
    let lastWall = 0;
    let bossMet = false;
    let openId = null;
    let briefChain = Promise.resolve();

    const wine = {
        world: null,
        preview: null,
        camera: null,
        spot: null,
        taken: false,
        tilt: 0,
        pouring: false,
        pourT: 0,
        cdUntil: 0,
        sips: 0
    };

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

    const mountUseHint = () => {
        if (!document.body || useHint) return;
        useHint = document.createElement("div");
        useHint.id = "useHint";
        useHint.textContent = "回头。红椅子上。";
        document.body.appendChild(useHint);
    };

    const loadLearned = () => {
        try {
            const saved = JSON.parse(sessionStorage.getItem("d22-skillbook") || "[]");
            if (Array.isArray(saved)) saved.forEach((id) => learned.add(id));
            const seen = JSON.parse(sessionStorage.getItem("d22-ability-heard") || "[]");
            if (Array.isArray(seen)) seen.forEach((id) => heard.add(id));
        } catch (err) {}
    };

    const saveLearned = () => {
        try {
            sessionStorage.setItem("d22-skillbook", JSON.stringify([...learned]));
        } catch (err) {}
    };

    const saveHeard = () => {
        try {
            sessionStorage.setItem("d22-ability-heard", JSON.stringify([...heard]));
        } catch (err) {}
    };

    const slotFor = (id) => document.querySelector(`[data-skill="${id}"]`);

    const paintSlot = (slot, mark) => {
        const key = slot.dataset.key;
        slot.replaceChildren();
        if (mark) {
            const icon = document.createElement("span");
            icon.className = "mark";
            icon.textContent = mark;
            slot.appendChild(icon);
        }
        if (key) {
            const badge = document.createElement("span");
            badge.className = "key";
            badge.textContent = key;
            slot.appendChild(badge);
        }
    };

    const fillSlot = (id) => {
        const skill = SKILLS[id];
        const slot = slotFor(id);
        if (!skill || !slot) return;
        slot.classList.add("got");
        slot.disabled = false;
        slot.title = skill.title;
        slot.setAttribute("aria-label", skill.title);
        paintSlot(slot, skill.mark);
    };

    const learnSkill = (id) => {
        const skill = SKILLS[id];
        const slot = slotFor(id);
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
        for (const item of INTERACTIVE) {
            const slot = document.createElement("button");
            slot.type = "button";
            slot.dataset.skill = item.id;
            slot.dataset.key = item.key;
            slot.disabled = true;
            slot.setAttribute("aria-label", `能力 ${item.key}`);
            paintSlot(slot, "");
            slot.addEventListener("click", () => toggleAbility(item.id));
            hud.appendChild(slot);
        }
        document.body.appendChild(hud);
        for (const item of INTERACTIVE) {
            if (learned.has(item.id)) fillSlot(item.id);
        }

        const walkDock = document.createElement("div");
        walkDock.id = "hudWalk";
        const walk = document.createElement("button");
        walk.type = "button";
        walk.dataset.skill = "walk";
        walk.disabled = true;
        walk.setAttribute("aria-label", "行走");
        walkDock.appendChild(walk);
        document.body.appendChild(walkDock);
        if (learned.has("walk")) fillSlot("walk");
    };

    const syncWineViewport = () => {
        const frame = document.getElementById("abilityFrame");
        const canvas = document.getElementById("application-canvas");
        const cam = wine.camera?.camera;
        if (!frame || !canvas || !cam || !frame.classList.contains("live")) return;
        const c = canvas.getBoundingClientRect();
        const f = frame.getBoundingClientRect();
        if (c.width < 1 || c.height < 1 || f.width < 1) return;
        const rect = cam.rect;
        rect.x = (f.left - c.left) / c.width;
        rect.y = (c.bottom - f.bottom) / c.height;
        rect.z = f.width / c.width;
        rect.w = f.height / c.height;
        cam.rect = rect;
    };

    const setWinePose = () => {
        if (!wine.preview) return;
        wine.preview.setLocalEulerAngles(wine.tilt, 28, 0);
        const fallback = document.getElementById("bottleFallback");
        if (fallback) fallback.style.transform = `rotate(${wine.tilt}deg)`;
    };

    const closeAbility = () => {
        if (wine.pouring) {
            wine.pouring = false;
            wine.pourT = 0;
            wine.tilt = 0;
        }
        openId = null;
        const root = document.getElementById("ability");
        if (root) root.classList.remove("on");
        if (wine.camera) wine.camera.enabled = false;
    };

    const drinkCooling = () => performance.now() < wine.cdUntil;

    const finishDrink = () => {
        wine.pouring = false;
        wine.pourT = 0;
        wine.tilt = 0;
        openId = null;
        const root = document.getElementById("ability");
        if (root) root.classList.remove("on");
        if (wine.camera) wine.camera.enabled = false;
        wine.cdUntil = performance.now() + 3000;
        if (sceneKey() === "scene_recordshop") {
            location.href = "scene_hutong.html";
            return;
        }
        wine.sips += 1;
        if (wine.sips >= 5) location.reload();
    };

    const toggleAbility = (id) => {
        const skill = SKILLS[id];
        if (!skill || !learned.has(id) || briefing) return;
        if (id === "drink" && drinkCooling()) return;
        if (openId === id) {
            closeAbility();
            return;
        }
        openId = id;
        const root = document.getElementById("ability");
        const frame = document.getElementById("abilityFrame");
        const name = document.getElementById("abilityName");
        const caption = document.getElementById("abilityCaption");
        if (!root || !frame) return;
        name.textContent = skill.title;
        caption.textContent = id === "drink" ? "点击倾倒" : "";
        frame.classList.toggle("live", id === "drink" && !!wine.preview);
        root.classList.add("on");
        document.exitPointerLock?.();
        look.dragging = false;
        if (id === "drink" && wine.camera) {
            wine.camera.enabled = true;
            syncWineViewport();
        } else if (wine.camera) {
            wine.camera.enabled = false;
        }
        if (ABILITY_INTRO[id] && !heard.has(id)) {
            heard.add(id);
            saveHeard();
            playBeats(ABILITY_INTRO[id]);
        }
    };

    const mountAbility = () => {
        if (!document.body || document.getElementById("ability")) return;
        const root = document.createElement("div");
        root.id = "ability";
        root.innerHTML = `
            <div id="abilityFrame">
                <p id="abilityName"></p>
                <button id="abilityClose" type="button" aria-label="关闭">×</button>
                <div id="bottleFallback" aria-hidden="true"><i class="neck"></i><i class="body"></i></div>
                <p id="abilityCaption"></p>
            </div>`;
        document.body.appendChild(root);
        const frame = root.querySelector("#abilityFrame");
        frame.addEventListener("pointerdown", (event) => {
            event.stopPropagation();
            if (briefing || wine.pouring || drinkCooling() || openId !== "drink" || event.target.id === "abilityClose") return;
            wine.pouring = true;
            wine.pourT = 0;
        });
        root.addEventListener("pointerdown", () => {
            if (!briefing) closeAbility();
        });
        root.querySelector("#abilityClose").addEventListener("pointerdown", (event) => {
            event.stopPropagation();
            if (!briefing) closeAbility();
        });
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

    const runBeats = async (beats, onBeat) => {
        if (!beats?.length || !document.body) return;
        briefing = true;
        look.dragging = false;
        document.exitPointerLock?.();
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
            beats.forEach((_, index) => onBeat?.(index));
            zhEl.textContent = beats.map((beat) => beat.zh).join("\n\n");
            enEl.textContent = beats.map((beat) => beat.en).join("\n\n");
            await new Promise((resolve) => {
                if (skipped) resolve();
                else root.querySelector("#briefSkip").addEventListener("click", () => resolve(), { once: true });
            });
        } else {
            for (let index = 0; index < beats.length; index++) {
                if (skipped) break;
                onBeat?.(index);
                const beat = beats[index];
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
    };

    const playBeats = (beats, onBeat) => {
        const job = briefChain.then(() => runBeats(beats, onBeat));
        briefChain = job.catch(() => {});
        return job;
    };

    const playBrief = () => {
        const beats = BRIEFS[sceneKey()];
        if (!beats) {
            mountHint();
            return;
        }
        playBeats(beats).then(() => {
            keys.clear();
            mountHint();
        });
    };

    const eachRender = (entity, visit) => {
        if (!entity) return;
        if (entity.render) visit(entity);
        const children = entity.children || [];
        for (let i = 0; i < children.length; i++) eachRender(children[i], visit);
    };

    const sitWine = () => {
        if (!wine.world || !wine.spot) return;
        wine.world.enabled = true;
        wine.world.setLocalScale(0.36, 0.36, 0.36);
        wine.world.setPosition(wine.spot.x, wine.spot.y, wine.spot.z);
        wine.world.setEulerAngles(0, wine.spot.spin, 0);
    };

    const placeWine = () => {
        wine.spot = { x: -0.36, y: -0.68, z: 1.72, spin: 200 };
        wine.taken = false;
        sitWine();
    };

    const loadWine = (app) => {
        if (wine.camera || !app?.assets?.loadFromUrl) return;
        app.assets.loadFromUrl("Model/Wine.glb", "container", (err, asset) => {
            if (err || !asset?.resource?.instantiateRenderEntity) {
                console.warn("Wine.glb failed to load", err);
                return;
            }
            try {
                const layerHost = app.scene.layers.layerList[0];
                const Layer = layerHost.constructor;
                const Entity = app.root.constructor;
                const Color = app.scene.ambientLight.constructor;
                const layer = new Layer({ name: "WinePreview" });
                app.scene.layers.push(layer);

                wine.world = asset.resource.instantiateRenderEntity();
                wine.world.enabled = false;
                wine.world.name = "wine-world";
                app.root.addChild(wine.world);

                wine.preview = asset.resource.instantiateRenderEntity();
                wine.preview.name = "wine-preview";
                wine.preview.setLocalScale(1, 1, 1);
                wine.preview.setPosition(PREVIEW.x, PREVIEW.y, PREVIEW.z);
                eachRender(wine.preview, (entity) => {
                    entity.render.layers = [layer.id];
                });
                app.root.addChild(wine.preview);

                const light = new Entity("wineLight");
                light.addComponent("light", {
                    type: "omni",
                    intensity: 2.6,
                    range: 8,
                    layers: [layer.id]
                });
                light.setPosition(PREVIEW.x + 0.55, PREVIEW.y + 1.15, PREVIEW.z + 1.05);
                app.root.addChild(light);

                wine.camera = new Entity("wineCamera");
                wine.camera.addComponent("camera", {
                    clearColor: new Color(0.07, 0.065, 0.06, 1),
                    priority: 10,
                    fov: 26,
                    nearClip: 0.02,
                    farClip: 12,
                    layers: [layer.id]
                });
                wine.camera.setPosition(PREVIEW.x, PREVIEW.y + 0.5, PREVIEW.z + 2.45);
                wine.camera.lookAt(PREVIEW.x, PREVIEW.y + 0.5, PREVIEW.z);
                wine.camera.enabled = false;
                app.root.addChild(wine.camera);
                sitWine();
                setWinePose();
            } catch (setupErr) {
                console.warn("Wine preview failed", setupErr);
            }
        });
    };

    const meetBoss = async () => {
        if (bossMet) return;
        bossMet = true;
        await playBeats(BOSS);
        placeWine();
    };

    const takeWine = async () => {
        if (wine.taken || briefing || !wine.spot) return;
        wine.taken = true;
        if (useHint) useHint.classList.remove("show");
        if (wine.world) wine.world.enabled = false;
        await playBeats(WINE);
        learnSkill("drink");
    };

    const aimAtWine = () => {
        if (!wine.world?.enabled || wine.taken || briefing || openId) return 0;
        const pos = wine.world.getPosition();
        const tx = pos.x - x;
        const ty = pos.y + 0.16 - y;
        const tz = pos.z - z;
        const len = Math.hypot(tx, ty, tz);
        if (len < 0.05 || len > 1.7) return 0;
        const yawRad = yaw * Math.PI / 180;
        const pitchRad = pitch * Math.PI / 180;
        const fx = -Math.sin(yawRad) * Math.cos(pitchRad);
        const fy = Math.sin(pitchRad);
        const fz = -Math.cos(yawRad) * Math.cos(pitchRad);
        return (tx * fx + ty * fy + tz * fz) / len;
    };

    const atBoss = () => {
        const bound = window.__scene?.sceneBound;
        if (!bound || sceneKey() !== "scene_recordshop") return false;
        // The red door into the back room. Dialogue starts here, not beside the boss.
        return z > 5 && Math.abs(x - bound.center.x) < bound.halfExtents.x * 0.92;
    };

    const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

    const onKeyDown = (event) => {
        const hotkey = HOTKEYS[event.code];
        if (hotkey && !event.repeat) {
            toggleAbility(hotkey);
            event.preventDefault();
            return;
        }
        if (event.code === "KeyE" && !event.repeat && aimAtWine() > 0.88) {
            takeWine();
            event.preventDefault();
            return;
        }
        if (event.code === "Escape" && openId && !briefing) {
            closeAbility();
            return;
        }
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
        if (!enabled || event.button !== 0 || openId) return;
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
        if (!enabled || openId || (!look.dragging && !locked)) return;
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
        loadWine(scene.app);
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
            const walkSlot = slotFor("walk");
            if (walkSlot?.classList.contains("got")) {
                walkSlot.classList.toggle("hot", length > 0 && !briefing && !openId);
            }
            const frozen = briefing || openId;
            if (length > 0 && !frozen) {
                hideHint();
                if (!learned.has("walk")) learnSkill("walk");
                const scale = (speed * stepDt) / length;
                x = clamp(x + moveX * scale, bounds.minX, bounds.maxX);
                y = clamp(y + moveY * scale, bounds.minY, bounds.maxY);
                z = clamp(z + moveZ * scale, bounds.minZ, bounds.maxZ);
            }
            if (wine.spot && !wine.taken) sitWine();
            if (!bossMet && !frozen && atBoss()) meetBoss();
            if (useHint) {
                const aimed = aimAtWine() > 0.88;
                useHint.textContent = aimed ? "对准酒瓶 · 按 E" : "回头。红椅子上。";
                useHint.classList.toggle("show", !briefing && !wine.taken && !!wine.spot && (aimed || !openId));
            }
            if (wine.pouring) {
                wine.pourT += stepDt;
                const t = Math.min(1, wine.pourT / 1.35);
                const lift = t < 0.4 ? t / 0.4 : Math.max(0, 1 - (t - 0.4) / 0.6);
                wine.tilt = lift * 112;
                if (t >= 1) finishDrink();
            }
            const drinkSlot = slotFor("drink");
            if (drinkSlot?.classList.contains("got")) {
                const left = wine.cdUntil - performance.now();
                let badge = drinkSlot.querySelector(".cd");
                if (left > 0) {
                    if (!badge) {
                        badge = document.createElement("span");
                        badge.className = "cd";
                        drinkSlot.appendChild(badge);
                    }
                    badge.textContent = String(Math.ceil(left / 1000));
                } else if (badge) {
                    badge.remove();
                }
            }
            setWinePose();
            if (openId === "drink" && wine.camera) {
                wine.camera.enabled = true;
                syncWineViewport();
            } else if (wine.camera) {
                wine.camera.enabled = false;
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
        mountAbility();
        mountUseHint();
        window.__skills = { learn: learnSkill, open: toggleAbility };
        playBrief();
        wait();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }
})();
