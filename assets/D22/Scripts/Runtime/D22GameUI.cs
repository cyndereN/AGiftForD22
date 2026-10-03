using System;
using System.Collections.Generic;
using UnityEngine;

namespace D22
{
    // A resolution-independent prototype UI. All story text and art come from project assets.
    public sealed class D22GameUI : MonoBehaviour
    {
        enum ScreenMode { Menu, Puzzle, Dialogue, Choices, Drink, Pause, Ending, HUD, Loading, Ability, Minigame }
        ScreenMode mode = ScreenMode.Menu;
        D22GameFlow flow;
        Font font;
        Texture2D poster, cover;
        GUIStyle title, body, small, button, mark, keyStyle;
        Texture2D buttonNormal, buttonHover;
        D22Line[] lines;
        D22Question question;
        Action done, back, pourAction;
        Action<int> choose;
        string heading, hint, note;
        int line, placed, sips, drag = -1;
        float pour, cooldown, typeT;
        bool hasDrink, pouring, typed;
        string typeZh = "", typeEn = "";
        readonly Rect[] pieces = new Rect[6];
        readonly bool[] locked = new bool[6];
        Vector2 dragOffset;
        bool abilityHeld;
        string flyingId;
        float flyT = -1;
        Vector2 flyFrom, flyTo;
        readonly Queue<string> flyQueue = new();
        const float Width = 1440, Height = 900;
        const float Slot = 72, SlotGap = 10;
        public string CurrentScreen => mode.ToString();
        public int PlacedPieces => placed;
        public bool BlocksLook { get; private set; }

        public void Initialize(D22GameFlow f, Font uiFont, Texture2D p, Texture2D c)
        { flow = f; font = uiFont; poster = p; cover = c; }

        void OnDestroy()
        {
            if (buttonNormal) Destroy(buttonNormal);
            if (buttonHover) Destroy(buttonHover);
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 48, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            body = new GUIStyle(title) { fontSize = 30 };
            small = new GUIStyle(title) { fontSize = 19 };
            mark = new GUIStyle(title) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            keyStyle = new GUIStyle(small) { fontSize = 16, alignment = TextAnchor.UpperLeft };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 22, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            buttonNormal = new Texture2D(1, 1); buttonNormal.SetPixel(0, 0, new Color(.28f, .1f, .06f)); buttonNormal.Apply();
            buttonHover = new Texture2D(1, 1); buttonHover.SetPixel(0, 0, new Color(.43f, .16f, .09f)); buttonHover.Apply();
            button.normal.background = buttonNormal; button.hover.background = buttonHover; button.active.background = buttonHover;
            button.normal.textColor = button.hover.textColor = button.active.textColor = Color.white;
            button.border = new RectOffset();
            title.normal.textColor = body.normal.textColor = small.normal.textColor = mark.normal.textColor = keyStyle.normal.textColor = new Color(.95f, .91f, .83f);
        }

        void OnGUI()
        {
            Styles();
            GUI.color = Color.white;
            if (mode == ScreenMode.Menu)
            {
                GUI.matrix = Matrix4x4.identity;
                if (cover) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), cover, ScaleMode.ScaleAndCrop);
                else Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, .38f));
            }
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            if (mode != ScreenMode.HUD && mode != ScreenMode.Ability && mode != ScreenMode.Minigame && mode != ScreenMode.Menu)
                Fill(new Rect(-Width, -Height, Width * 3, Height * 3), new Color(.035f, .03f, .025f, .97f));
            switch (mode)
            {
                case ScreenMode.Menu: Menu(); break;
                case ScreenMode.Puzzle: Puzzle(); break;
                case ScreenMode.Dialogue: Dialogue(); break;
                case ScreenMode.Choices: Choices(); break;
                case ScreenMode.Drink: Drink(); break;
                case ScreenMode.Pause: Pause(); break;
                case ScreenMode.Ending: Ending(); break;
                case ScreenMode.HUD: HUD(); break;
                case ScreenMode.Ability: Ability(); break;
                case ScreenMode.Minigame: Minigame(); break;
                default: GUI.Label(new Rect(100, 360, 1200, 120), "…", title); break;
            }
            DrawFly();
            BlocksLook = PointerBlocks(GuiMouse());
            GUI.matrix = Matrix4x4.identity;
        }

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        bool Button(Rect rect, string text) => GUI.Button(rect, text, button);

        void Menu()
        {
            GUI.Label(new Rect(90, 160, 650, 180), "A Gift\nfor D-22", new GUIStyle(title) { fontSize = 72 });
            Fill(new Rect(95, 360, 70, 4), new Color(.75f, .18f, .1f));
            if (Button(new Rect(95, 520, 360, 64), "开始")) flow.Begin();
            if (Button(new Rect(95, 604, 360, 52), "漫游")) ShowGallery();
        }

        void ShowGallery()
        {
            question = new D22Question { zh = "去哪儿", en = "", choices = new[] { new D22Choice { zh = "唱片店", en = "" }, new D22Choice { zh = "胡同", en = "" }, new D22Choice { zh = "D-22", en = "" }, new D22Choice { zh = "现场扫描", en = "" }, new D22Choice { zh = "演出", en = "" } } };
            choose = i => flow.LoadSpace(new[] { "D22_RecordShop", "D22_Hutong", "D22_Bootstrap", "D22_LiveScan", "D22_Performance" }[i]);
            ShowChoices(question, choose);
        }

        void Dialogue()
        {
            GUI.Label(new Rect(100, 250, 560, 360), Shown(lines[line].zh), body);
            GUI.Label(new Rect(780, 250, 560, 360), Shown(lines[line].en), new GUIStyle(body) { fontSize = 24 });
            Fill(new Rect(720, 260, 1, 280), new Color(.6f, .5f, .4f, .3f));
            if (Button(new Rect(690, 745, 290, 65), "跳过")) done?.Invoke();
            if (typed && Button(new Rect(1030, 745, 310, 65), "继续")) AdvanceDialogue();
        }

        public void AdvanceDialogue()
        {
            if (mode != ScreenMode.Dialogue || !typed) return;
            line++;
            if (line >= lines.Length) { line = lines.Length - 1; done?.Invoke(); }
            else BeginType(lines[line].zh, lines[line].en);
        }

        Rect Target(int i) => new Rect(510 + (i % 2) * 210, 155 + (i / 2) * 195, 210, 195);

        void Puzzle()
        {
            GUI.Label(new Rect(80, 40, 1260, 70), "拼回去。", body);
            for (int i = 0; i < 6; i++) Fill(Target(i), new Color(.15f, .13f, .11f));
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0)
                for (int i = 5; i >= 0; i--) if (!locked[i] && pieces[i].Contains(ev.mousePosition)) { drag = i; dragOffset = ev.mousePosition - pieces[i].position; ev.Use(); break; }
            if (drag >= 0 && ev.type == EventType.MouseDrag) { pieces[drag].position = ev.mousePosition - dragOffset; ev.Use(); }
            if (drag >= 0 && ev.type == EventType.MouseUp)
            { int i = drag; drag = -1; if (Vector2.Distance(pieces[i].center, Target(i).center) < 75) PlacePiece(i); ev.Use(); }
            for (int i = 0; i < 6; i++)
            {
                GUI.DrawTextureWithTexCoords(pieces[i], poster, new Rect((i % 2) * .5f, 1 - (i / 2 + 1) / 3f, .5f, 1f / 3));
                if (!locked[i]) GUI.Label(new Rect(pieces[i].x + 8, pieces[i].y + 8, 40, 28), (i + 1).ToString(), small);
            }
            GUI.Label(new Rect(80, 790, 500, 65), $"{placed} / 6", small);
            if (Button(new Rect(720, 780, 240, 65), "跳过")) done?.Invoke();
            if (placed == 6 && Button(new Rect(990, 780, 300, 65), "继续")) done?.Invoke();
        }

        public void PlacePiece(int i)
        {
            if (mode != ScreenMode.Puzzle || i < 0 || i >= 6 || locked[i]) return;
            pieces[i] = Target(i);
            locked[i] = true;
            placed++;
        }

        void Choices()
        {
            GUI.Label(new Rect(100, 65, 1220, 100), Shown(question.zh), body);
            if (!string.IsNullOrEmpty(question.en))
                GUI.Label(new Rect(100, 160, 1220, 90), Shown(question.en), small);
            if (!typed)
            {
                if (Button(new Rect(1030, 745, 310, 65), "跳过")) typed = true;
                return;
            }
            for (int i = 0; i < question.choices.Length; i++)
                if (Button(new Rect(180, 280 + i * 92, 1080, 72), question.choices[i].zh)) Choose(i);
        }

        public void Choose(int i)
        {
            if (mode == ScreenMode.Choices && typed && i >= 0 && i < question.choices.Length) choose?.Invoke(i);
        }

        void Drink()
        {
            Ability();
        }

        void Pause()
        {
            GUI.Label(new Rect(120, 90, 1200, 80), "暂停", title);
            flow.ReducedMotion = GUI.Toggle(new Rect(120, 280, 800, 50), flow.ReducedMotion, " 减少晃动", button);
            GUI.Label(new Rect(120, 380, 900, 40), "音量", small);
            flow.Volume = GUI.HorizontalSlider(new Rect(120, 440, 800, 35), flow.Volume, 0, 1);
            if (Button(new Rect(120, 560, 420, 70), "继续")) done?.Invoke();
            if (Button(new Rect(580, 560, 420, 70), "菜单")) flow.LoadSpace("D22_Menu");
        }

        void Ending()
        {
            GUI.Label(new Rect(130, 220, 1180, 230), Shown(note), title);
            if (Button(new Rect(520, 660, 240, 70), "跳过")) done?.Invoke();
            if (typed && Button(new Rect(130, 660, 360, 70), "菜单")) done?.Invoke();
        }

        void HUD()
        {
            DrawChrome();
            DrawSlots();
            DrawWorldMarks();
            Fill(new Rect(718, 448, 4, 4), new Color(1, 1, 1, .6f));
        }

        void Ability()
        {
            Fill(new Rect(-Width, -Height, Width * 3, Height * 3), new Color(0, 0, 0, .46f));
            DrawChrome();
            DrawSlots();
            var book = flow.Abilities;
            var info = D22Abilities.Info(book?.OpenId);
            Rect frame = AbilityFrame();
            Fill(frame, new Color(.08f, .075f, .07f, .98f));
            FrameBorder(frame);
            if (info != null)
            {
                GUI.Label(new Rect(frame.x + 18, frame.y + 12, 280, 28), info.title, small);
                GUI.Label(new Rect(frame.x, frame.yMax - 42, frame.width, 28), info.caption, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }
            if (Button(new Rect(frame.xMax - 44, frame.y + 8, 36, 36), "×")) { flow.CloseAbility(); return; }
            Rect inner = new Rect(frame.x + 28, frame.y + 52, frame.width - 56, frame.height - 108);
            DrawAbilityBody(book, inner);
            HandleAbilityPointer(book, frame, inner);
        }

        void DrawAbilityBody(D22Abilities book, Rect inner)
        {
            if (book == null) return;
            switch (book.OpenId)
            {
                case "drink": DrawDrink(book, inner); break;
                case "cricket": DrawCricket(book, inner); break;
                case "pigeon": DrawPigeon(book, inner); break;
                case "scissors": DrawScissors(book, inner); break;
                case "guitar": DrawStrings(book, inner, 6, new Color(.82f, .62f, .38f)); break;
                case "bass": DrawStrings(book, inner, 4, new Color(.55f, .62f, .78f)); break;
                case "drums": DrawDrums(book, inner); break;
            }
        }

        void DrawDrink(D22Abilities book, Rect inner)
        {
            if (book.HasBottlePreview && book.BottleView)
            {
                var bottle = new Rect(inner.x + inner.width * .22f, inner.y + 8, inner.width * .56f, inner.height - 16);
                GUI.DrawTexture(bottle, book.BottleView, ScaleMode.ScaleToFit);
            }
            else DrawFallbackBottle(inner, book.BottleTilt);
        }

        void DrawFallbackBottle(Rect inner, float tilt)
        {
            Vector2 pivot = new Vector2(inner.center.x, inner.yMax - 18);
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(tilt, pivot);
            Fill(new Rect(pivot.x - 28, pivot.y - 168, 56, 130), new Color(.42f, .14f, .18f));
            Fill(new Rect(pivot.x - 12, pivot.y - 220, 24, 54), new Color(.46f, .18f, .2f, .8f));
            GUI.matrix = matrix;
        }

        void DrawCricket(D22Abilities book, Rect inner)
        {
            Fill(inner, new Color(.05f, .07f, .05f));
            FrameBorder(inner);
            float pulse = .7f + book.CricketPulse * .5f;
            var cage = new Rect(inner.center.x - 70 * pulse, inner.center.y - 46 * pulse, 140 * pulse, 92 * pulse);
            FrameBorder(cage);
            Fill(new Rect(inner.center.x - 8, inner.center.y - 8 + (1 - book.CricketPulse) * 10, 16, 16), new Color(.7f + book.CricketPulse * .3f, .85f, .45f));
        }

        void DrawPigeon(D22Abilities book, Rect inner)
        {
            Fill(inner, new Color(.07f, .08f, .1f));
            float r = Mathf.Min(inner.width, inner.height) * .32f;
            Vector2 c = inner.center;
            FrameBorder(new Rect(c.x - r, c.y - r, r * 2, r * 2));
            float rad = book.PigeonAngle * Mathf.Deg2Rad;
            Vector2 p = c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
            Fill(new Rect(p.x - 8, p.y - 8, 16, 16), new Color(.9f, .86f, .7f, .4f + book.PigeonSpin));
        }

        void DrawScissors(D22Abilities book, Rect inner)
        {
            Fill(inner, new Color(.1f, .08f, .06f));
            Fill(new Rect(inner.x + 20, inner.center.y - 8, inner.width - 40, 16), new Color(.35f, .28f, .18f));
            float x = inner.x + 20 + (inner.width - 56) * book.ScissorsX;
            Fill(new Rect(x, inner.center.y - 28, 36, 56), new Color(.7f, .7f, .68f));
            if (book.ScissorsSpark > .05f)
                Fill(new Rect(x + 30, inner.center.y - 40, 10, 10), new Color(1f, .85f, .4f, book.ScissorsSpark));
        }

        void DrawStrings(D22Abilities book, Rect inner, int count, Color color)
        {
            Fill(inner, new Color(.08f, .07f, .06f));
            for (int i = 0; i < count; i++)
            {
                float x = inner.x + inner.width * (i + 1) / (count + 1f);
                bool lit = book.LitString == i;
                Fill(new Rect(x - (lit ? 3 : 1.5f), inner.y + 10, lit ? 6 : 3, inner.height - 20), lit ? Color.white : color);
            }
        }

        void DrawDrums(D22Abilities book, Rect inner)
        {
            string[] names = { "底", "军", "镲" };
            for (int i = 0; i < 3; i++)
            {
                var pad = new Rect(inner.x + i * (inner.width / 3f) + 10, inner.y + 24, inner.width / 3f - 20, inner.height - 48);
                Fill(pad, book.LitPad == i ? new Color(.55f, .2f, .12f) : new Color(.16f, .12f, .1f));
                FrameBorder(pad);
                GUI.Label(pad, names[i], mark);
            }
        }

        void HandleAbilityPointer(D22Abilities book, Rect frame, Rect inner)
        {
            if (book == null) return;
            var ev = Event.current;
            Vector2 m = ev.mousePosition;
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                if (new Rect(frame.xMax - 44, frame.y + 8, 36, 36).Contains(m)) return;
                if (!frame.Contains(m)) { flow.CloseAbility(); ev.Use(); return; }
                abilityHeld = true;
                book.PointerDown(Norm(inner, m));
                ev.Use();
            }
            else if (abilityHeld && ev.type == EventType.MouseDrag)
            {
                book.PointerDrag(Norm(inner, m));
                ev.Use();
            }
            else if (abilityHeld && ev.type == EventType.MouseUp)
            {
                book.PointerUp();
                abilityHeld = false;
                ev.Use();
            }
        }

        static Vector2 Norm(Rect inner, Vector2 m)
        {
            return new Vector2(
                Mathf.Clamp01((m.x - inner.x) / Mathf.Max(1, inner.width)),
                Mathf.Clamp01((m.y - inner.y) / Mathf.Max(1, inner.height)));
        }

        void DrawChrome()
        {
            if (Button(new Rect(35, 28, 110, 44), "菜单")) flow.Pause();
            if (!string.IsNullOrEmpty(hint))
            {
                bool tally = hint.Contains("鸽哨");
                float w = tally ? 920 : 500;
                var pill = new Rect((Width - w) / 2, tally ? 730 : 748, w, tally ? 56 : 42);
                Fill(pill, new Color(0, 0, 0, tally ? .72f : .5f));
                GUI.Label(pill, hint, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = tally ? 22 : 19 });
            }
            DrawWalk();
        }

        void DrawSlots()
        {
            var book = flow.Abilities;
            for (int i = 0; i < D22Abilities.SlotCount; i++)
            {
                var info = D22Abilities.Slots[i];
                Rect box = SlotRect(i);
                bool got = book != null && book.Learned(info.id);
                bool open = (book != null && book.OpenId == info.id)
                    || (flow.Hunt != null && flow.Hunt.Open && D22Hunt.AbilityId(flow.Hunt.Game) == info.id);
                Fill(box, open ? new Color(.42f, .36f, .28f, .9f) : got ? new Color(.07f, .08f, .07f, .82f) : new Color(.04f, .04f, .04f, .55f));
                FrameBorder(box, got || open ? new Color(.96f, .94f, .9f, open ? 1 : .85f) : new Color(.96f, .94f, .9f, .35f));
                if (got) GUI.Label(box, info.mark, mark);
                float cdLeft = info.id == "drink" ? cooldown : (flow.Hunt != null ? flow.Hunt.CooldownLeft(info.id) : 0);
                if (got && cdLeft > 0)
                {
                    Fill(box, new Color(0, 0, 0, .62f));
                    GUI.Label(box, Mathf.CeilToInt(cdLeft).ToString(), mark);
                }
                GUI.Label(new Rect(box.x, box.yMax + 2, box.width, 20), info.key, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
                var ev = Event.current;
                if (got && ev.type == EventType.MouseDown && ev.button == 0 && box.Contains(ev.mousePosition))
                { flow.ToggleAbility(info.id); ev.Use(); }
            }
        }

        void DrawWalk()
        {
            var book = flow.Abilities;
            if (book == null || !book.HasWalk) return;
            Rect box = WalkRect();
            Fill(box, book.WalkHot ? new Color(.42f, .36f, .28f, .9f) : new Color(.07f, .08f, .07f, .82f));
            FrameBorder(box, book.WalkHot ? Color.white : new Color(.96f, .94f, .9f, .85f));
            GUI.Label(box, "走", mark);
        }

        void DrawWorldMarks()
        {
            var hunt = flow.Hunt;
            var cam = Camera.main;
            if (hunt == null || !hunt.Spawned || cam == null) return;
            foreach (var spot in hunt.Marks)
            {
                if (!spot) continue;
                if (spot.kind == D22MarkKind.Pigeons)
                {
                    float t = Time.unscaledTime;
                    for (int i = 0; i < 5; i++)
                    {
                        float a = t * .85f + i * 1.25f;
                        Vector3 p = spot.transform.position + new Vector3(Mathf.Sin(a) * .95f, Mathf.Sin(a * 1.6f) * .28f, Mathf.Cos(a) * .7f);
                        DrawWorldMark(cam, p, "鸽", new Color(.12f, .16f, .2f, .7f));
                    }
                }
                else
                {
                    string label = spot.kind == D22MarkKind.Cricket ? "蛐" : "磨";
                    var tint = spot.kind == D22MarkKind.Cricket ? new Color(.12f, .2f, .08f, .72f) : new Color(.2f, .14f, .08f, .72f);
                    DrawWorldMark(cam, spot.transform.position, label, tint);
                }
            }
        }

        void DrawWorldMark(Camera cam, Vector3 world, string label, Color tint)
        {
            Vector3 vp = cam.WorldToViewportPoint(world);
            if (vp.z < .15f || vp.x < -.08f || vp.x > 1.08f || vp.y < -.08f || vp.y > 1.08f) return;
            var box = new Rect(vp.x * Width - 32, (1 - vp.y) * Height - 32, 64, 64);
            Fill(box, tint);
            FrameBorder(box);
            GUI.Label(box, label, mark);
        }

        void Minigame()
        {
            var hunt = flow.Hunt;
            if (hunt == null || !hunt.Open) { ShowHUD(); return; }
            if (hunt.Unlocked) hint = hunt.Quota(flow.Sips);
            Fill(new Rect(-Width, -Height, Width * 3, Height * 3), new Color(0, 0, 0, .46f));
            DrawChrome();
            DrawSlots();
            Rect frame = AbilityFrame();
            Fill(frame, new Color(.08f, .075f, .07f, .98f));
            FrameBorder(frame);
            string titleText = hunt.Game == "cricket" ? "蛐蛐" : hunt.Game == "pigeon" ? "鸽哨" : "磨刀";
            string caption = hunt.Game == "cricket" ? "点下去，框落下"
                : hunt.Game == "pigeon" ? "点鸽子出音符"
                : "拖刀过石";
            GUI.Label(new Rect(frame.x + 18, frame.y + 12, 280, 28), titleText, small);
            GUI.Label(new Rect(frame.x, frame.yMax - 42, frame.width, 28), caption, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            if (Button(new Rect(frame.xMax - 44, frame.y + 8, 36, 36), "×")) { flow.CloseHunt(); return; }
            Rect inner = new Rect(frame.x + 28, frame.y + 52, frame.width - 56, frame.height - 108);
            if (hunt.Game == "cricket") DrawCatch(hunt, inner);
            else if (hunt.Game == "pigeon") DrawPigeonGame(hunt, inner);
            else DrawGrindGame(hunt, inner);
            HandleHuntPointer(hunt, frame, inner);
        }

        void DrawCatch(D22Hunt hunt, Rect inner)
        {
            Fill(inner, new Color(.06f, .08f, .05f));
            Vector2 m = Event.current.mousePosition;
            Vector2 n = Norm(inner, m);
            if (!hunt.Dropping && inner.Contains(m)) hunt.AimTrap(n);
            Vector2 c = hunt.Dropping || !inner.Contains(m) ? hunt.Trap : n;
            float fall = hunt.Dropping ? Mathf.Pow(Mathf.Clamp01(hunt.DropLeft / D22Hunt.DropTime), 2) * 78 : 0;
            float w = D22Hunt.TrapSize * inner.width;
            float h = D22Hunt.TrapSize * inner.height;
            var box = new Rect(inner.x + c.x * inner.width - w * .5f, inner.y + c.y * inner.height - h * .5f - fall, w, h);
            Fill(box, hunt.Dropping ? new Color(.9f, .85f, .4f, .16f) : new Color(1, 1, 1, .05f));
            FrameBorder(box, hunt.Dropping ? new Color(1f, .92f, .45f) : new Color(.9f, .85f, .5f, .85f));
            Vector2 bug = new Vector2(inner.x + hunt.Bug.x * inner.width, inner.y + hunt.Bug.y * inner.height);
            Fill(new Rect(bug.x - 10, bug.y - 10, 20, 20), new Color(.75f, .9f, .35f));
        }

        void DrawPigeonGame(D22Hunt hunt, Rect inner)
        {
            Fill(inner, new Color(.07f, .09f, .12f));
            foreach (var b in hunt.Birds)
            {
                var p = new Rect(inner.x + b.pos.x * inner.width - 28, inner.y + b.pos.y * inner.height - 16, 56, 32);
                Fill(p, new Color(.9f, .88f, .8f));
            }
            foreach (var n in hunt.Notes)
            {
                var p = new Rect(inner.x + n.pos.x * inner.width - 16, inner.y + n.pos.y * inner.height - 18, 32, 36);
                GUI.color = new Color(1, 1, .7f, Mathf.Clamp01(n.life));
                GUI.Label(p, "♪", mark);
                GUI.color = Color.white;
            }
        }

        void DrawGrindGame(D22Hunt hunt, Rect inner)
        {
            Fill(inner, new Color(.1f, .08f, .06f));
            var stone = new Rect(inner.x + inner.width * .08f, inner.y + inner.height * .28f, inner.width * .84f, inner.height * .5f);
            Fill(stone, new Color(.35f, .32f, .26f));
            FrameBorder(stone);
            Vector2 k = new Vector2(inner.x + hunt.Knife.x * inner.width, inner.y + hunt.Knife.y * inner.height);
            Fill(new Rect(k.x - 84, k.y - 16, 168, 32), new Color(.75f, .75f, .72f));
        }

        void HandleHuntPointer(D22Hunt hunt, Rect frame, Rect inner)
        {
            var ev = Event.current;
            Vector2 n = Norm(inner, ev.mousePosition);
            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                if (new Rect(frame.xMax - 44, frame.y + 8, 36, 36).Contains(ev.mousePosition)) return;
                if (!frame.Contains(ev.mousePosition)) { flow.CloseHunt(); ev.Use(); return; }
                hunt.Pointer(n, true, false, false);
                ev.Use();
            }
            else if (ev.type == EventType.MouseDrag)
            {
                hunt.Pointer(n, false, true, false);
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp)
            {
                hunt.Pointer(n, false, false, true);
                ev.Use();
            }
        }

        void DrawFly()
        {
            if (flyT < 0 || string.IsNullOrEmpty(flyingId)) return;
            var info = D22Abilities.Info(flyingId);
            if (info == null) return;
            float pop = Mathf.Clamp01(flyT / .46f);
            float travel = Mathf.Clamp01((flyT - .68f) / .62f);
            Vector2 pos = travel <= 0 ? flyFrom : Vector2.Lerp(flyFrom, flyTo, travel);
            float scale = travel <= 0 ? Mathf.Lerp(.15f, 1.75f, pop) : Mathf.Lerp(1.75f, .78f, travel);
            var rect = new Rect(pos.x - 36 * scale, pos.y - 36 * scale, 72 * scale, 72 * scale);
            GUI.Label(rect, info.mark, new GUIStyle(mark) { fontSize = Mathf.RoundToInt(34 * scale) });
        }

        public void PlayLearnFly(string id)
        {
            if (D22Abilities.Info(id) == null) return;
            flyQueue.Enqueue(id);
            if (flyT < 0) NextFly();
        }

        void NextFly()
        {
            if (flyQueue.Count == 0) { flyingId = null; flyT = -1; return; }
            flyingId = flyQueue.Dequeue();
            flyT = 0;
            flyFrom = new Vector2(Width * .5f, Height * .5f);
            flyTo = flyingId == "walk" ? WalkRect().center : SlotRect(IndexOf(flyingId)).center;
            if (flow.ReducedMotion) flyT = 2;
        }

        void Update()
        {
            TickType();
            if (flyT < 0) return;
            flyT += Time.unscaledDeltaTime;
            if (flyT > 1.4f) NextFly();
        }

        void BeginType(string zh, string en)
        {
            typeZh = zh ?? "";
            typeEn = en ?? "";
            typeT = 0;
            typed = typeZh.Length == 0 && typeEn.Length == 0;
        }

        string Shown(string full)
        {
            if (typed || string.IsNullOrEmpty(full)) return full ?? "";
            int steps = Mathf.Max(typeZh.Length, typeEn.Length, 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(typeT / .026f), 0, steps);
            int n = Mathf.CeilToInt(full.Length * i / (float)steps);
            return full.Substring(0, Mathf.Clamp(n, 0, full.Length));
        }

        void TickType()
        {
            if (typed) return;
            if (mode != ScreenMode.Dialogue && mode != ScreenMode.Choices && mode != ScreenMode.Ending) return;
            typeT += Time.unscaledDeltaTime;
            int steps = Mathf.Max(typeZh.Length, typeEn.Length);
            if (steps <= 0 || typeT / .026f >= steps) typed = true;
        }

        static int IndexOf(string id)
        {
            for (int i = 0; i < D22Abilities.Slots.Length; i++) if (D22Abilities.Slots[i].id == id) return i;
            return 0;
        }

        static Rect SlotRect(int i) => new Rect(36 + i * (Slot + SlotGap), 808, Slot, Slot);
        static Rect WalkRect() => new Rect(1330, 800, 80, 80);
        static Rect AbilityFrame() => new Rect((Width - 440) / 2, Height * .46f - 220, 440, 440);
        static Rect MenuRect() => new Rect(35, 28, 110, 44);
        static Rect HintRect() => new Rect(470, 748, 500, 42);
        static Rect InteractRect() => new Rect(500, 690, 440, 50);

        Vector2 GuiMouse()
        {
            var ev = Event.current;
            return ev != null ? ev.mousePosition : Vector2.zero;
        }

        bool PointerBlocks(Vector2 m)
        {
            if (mode == ScreenMode.Ability && AbilityFrame().Contains(m)) return true;
            if (MenuRect().Contains(m)) return true;
            if (WalkRect().Contains(m)) return true;
            for (int i = 0; i < D22Abilities.SlotCount; i++) if (SlotRect(i).Contains(m)) return true;
            if (mode == ScreenMode.Minigame && AbilityFrame().Contains(m)) return true;
            return false;
        }

        static void FrameBorder(Rect rect) => FrameBorder(rect, new Color(.96f, .94f, .9f, .88f));
        static void FrameBorder(Rect rect, Color color)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1), color);
            Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            Fill(new Rect(rect.x, rect.y, 1, rect.height), color);
            Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

        public void ShowMenu() => mode = ScreenMode.Menu;
        public void ShowLoading() => mode = ScreenMode.Loading;
        public void ShowHUD() { mode = ScreenMode.HUD; abilityHeld = false; }
        public void UpdateHUD(string h, bool has, int s, float cd) { hint = h; hasDrink = has; sips = s; cooldown = cd; }
        public void ShowDialogue(string titleText, D22Line[] l, Action a)
        {
            heading = titleText; lines = l; line = 0; done = a; mode = ScreenMode.Dialogue;
            if (l == null || l.Length == 0) { typed = true; a(); return; }
            BeginType(l[0].zh, l[0].en);
        }
        public void ShowPuzzle(Action a)
        {
            done = a; placed = 0; drag = -1; mode = ScreenMode.Puzzle;
            for (int i = 0; i < 6; i++) { locked[i] = false; pieces[i] = new Rect(i % 2 == 0 ? 110 : 1110, 165 + (i / 2) * 200, 210, 195); }
        }
        public void ShowChoices(D22Question q, Action<int> a)
        {
            question = q; choose = a; mode = ScreenMode.Choices;
            BeginType(q?.zh, q?.en);
        }
        public void ShowDrink(Action a, Action b) { pourAction = a; back = b; pour = 0; pouring = false; mode = ScreenMode.Drink; }
        public void ShowAbility() { mode = ScreenMode.Ability; abilityHeld = false; }
        public void ShowMinigame() { mode = ScreenMode.Minigame; abilityHeld = false; }
        public void DisableDrink() => pouring = true;
        public void SetPour(float p) => pour = p;
        public void ShowPause(Action a) { done = a; mode = ScreenMode.Pause; }
        public void ShowEnding(string n, Action a) { note = n; done = a; mode = ScreenMode.Ending; BeginType(n, ""); }
    }
}
