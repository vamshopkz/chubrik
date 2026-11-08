using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 6 — «Глюк-мир», the prank world: you start on a crooked «ОШИБКА 404» window, trees wear the purple-black
    /// «Texture Missing» checkerboard, platforms are stretched pieces of interface, giant «ВЫХОД» and «ПАУЗА» buttons
    /// and upside-down settings. In the middle stands a wall you can't see (with a silly enemy bumping into it),
    /// giant «?» marks hang instead of stars, banana peels make you slip, code and loading icons float around.
    /// The boss — the Gorilla King in a golden mask — is far too small. Lives here are endless (the HUD shows 100).
    /// </summary>
    public static class World6
    {
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 5000);
        public static Vector3 Spawn => O + new Vector3(0, 0.6f, 1.5f);
        public static float FallY => O.y - 14f;
        public static float PausedUntil;
        public static readonly List<Banana> Bananas = new List<Banana>();
        public static readonly List<QuestionMark> Marks = new List<QuestionMark>();
        public static TinyKing Boss;
        public static GlitchButton Pause, Exit;
        public static Vector3 ArenaC, WallC;
        static Material ui, uiBar, uiDark, missing, code;

        public static void Build()
        {
            Root = new GameObject("World6").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            Bananas.Clear(); Marks.Clear();
            ui = M("F4F7FA", 0.4f); uiBar = M("3D8BFF", 0.4f); uiDark = M("2A3140", 0.4f);
            missing = Mats.Tex(MissingTex(), Color.white, 0.2f);
            missing.mainTextureScale = new Vector2(2, 2);

            // ---- start: the crooked «ОШИБКА 404» window ----
            ErrorWindow(V(0, 0, 0));
            Portal(V(-4.5f, 0.4f, -3.2f));
            Rescue.SpawnCage("w6c0", V(4.2f, 0.4f, -2.4f), -30, Root);

            // ---- a stretched slider bar bridge, giant ПАУЗА / ВЫХОД buttons ----
            Slider(V(0, 0, 6), V(0, 0, 19));
            Pause = GlitchButton.Spawn(V(-4, 0.6f, 24), "ПАУЗА", "FFC83A", Root, false);
            Exit = GlitchButton.Spawn(V(4, 1.2f, 28), "ВЫХОД", "E53935", Root, true);
            Pickups.I.Key("w6k0", V(4, 3.4f, 28));
            Mark(V(-4, 2.6f, 24));
            Bar(V(0, 0.6f, 31.5f), new Vector3(9, 0.5f, 3));

            // ---- the glitchy island with the wall you can't see ----
            Checker(V(0, 0, 45), 22);
            WallC = V(0, 0, 45);
            InvisibleWall(WallC);
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3 + 0.4f; MissingTree(V(Mathf.Cos(a) * 9, 0, 45 + Mathf.Sin(a) * 9), k); }
            for (int k = 0; k < 5; k++) Bananas.Add(Banana.Spawn(V(-7 + k * 3.5f, 0.05f, 40 + (k % 2) * 6), Root));
            Mark(V(-6, 1.6f, 50)); Mark(V(6, 1.6f, 39)); Mark(V(7, 1.6f, 51));
            Rescue.SpawnCage("w6c1", V(-8, 0, 38), 60, Root);
            Rescue.SpawnCage("w6c2", V(8.5f, 0, 47), -90, Root);
            Pickups.I.Key("w6k1", V(-8.5f, 1, 52));
            Pickups.I.Key("w6k2", V(9, 1, 40));
            Pickups.I.Token("w6t0", V(0, 4.5f, 45));   // right on top of the invisible wall

            // ---- upside-down settings: toggles as steps up to the panel ----
            for (int k = 0; k < 5; k++) Toggle(V(-3 + k * 1.5f, 0.6f + k * 0.8f, 57 + k * 1.2f), k % 2 == 0);
            SettingsPanel(V(0, 4.6f, 66));
            Rescue.SpawnCage("w6c3", V(-4, 4.6f, 66), 90, Root);
            Pickups.I.Key("w6k3", V(4.5f, 5.6f, 64));
            Mark(V(3, 6.2f, 68));

            // ---- spinning loading-circles over the void to the arena ----
            Spinner(V(0, 4.6f, 74.5f), 2.6f, 40);
            Spinner(V(3, 5.2f, 80), 2.4f, -55);
            Spinner(V(-1, 5.8f, 85), 2.6f, 70);
            Pickups.I.Key("w6k4", V(3, 6.6f, 80));
            Rescue.SpawnCage("w6c4", V(-1.2f, 5.8f, 85.4f), 0, Root);

            // ---- the arena of the boss who is too small ----
            ArenaC = V(0, 6, 99);
            Checker(ArenaC, 17);   // wide enough to land on from the last loading circle
            Lobby.Sign(ArenaC + new Vector3(4, 0, 5), 0, "БОСС СЛИШКОМ МАЛЕНЬКИЙ", "FF5FA2", 6);
            Boss = new GameObject("TinyKing").AddComponent<TinyKing>();
            Boss.Build(ArenaC + new Vector3(2, 0, 2), Root);
            Rescue.SpawnCage("w6c5", ArenaC + new Vector3(-6.5f, 0, -3), 70, Root);
            Rescue.SpawnCage("w6c6", ArenaC + new Vector3(6.5f, 0, -3), -70, Root);
            Rescue.SpawnCage("w6c7", ArenaC + new Vector3(-5, 0, 5.5f), 140, Root);
            Pickups.I.Key("w6k5", ArenaC + new Vector3(-7.5f, 1, 2));
            Pickups.I.Key("w6k6", ArenaC + new Vector3(7.5f, 1, 2));
            Pickups.I.Key("w6k7", ArenaC + new Vector3(0, 1, -8));
            Mark(ArenaC + new Vector3(-3, 1.6f, -6));

            Decor();
            Lobby.root = keep;
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- textures ----------------

        static Texture2D missingTex;
        /// <summary>The classic purple-and-black checkerboard of a missing texture.</summary>
        public static Texture2D MissingTex()
        {
            if (missingTex) return missingTex;
            var cv = new Canvas2(64, 64, Color.black);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) if (((x / 16) + (y / 16)) % 2 == 0) cv.px[y * 64 + x] = Hex("FF00DC");
            missingTex = cv.Tex(false, true, FilterMode.Point);
            return missingTex;
        }

        // ---------------- pieces ----------------

        /// <summary>The start: a giant tilted «ОШИБКА 404» pop-up window with a smiley.</summary>
        static void ErrorWindow(Vector3 c)
        {
            var t = Node("Error404", Root, c, new Vector3(0, 0, -5));
            var win = Lobby.Solid(PrimitiveType.Cube, new Vector3(0, -0.2f, 0), new Vector3(14, 0.8f, 10), ui, default, t);
            B(t, new Vector3(0, 0.22f, 4.2f), new Vector3(14, 0.06f, 1.6f), uiBar);
            B(t, new Vector3(6.2f, 0.27f, 4.2f), new Vector3(1.2f, 0.06f, 1.2f), M("E53935", 0.4f));
            var x = Prim.Text(t, "X", new Vector3(6.2f, 0.32f, 4.2f), 5, Color.white); x.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var title = Prim.Text(t, "ОШИБКА 404", new Vector3(-2, 0.32f, 4.2f), 7, Color.white); title.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var sub = Prim.Text(t, "ЧУБРИК НЕ НАЙДЕН (НО ТЫ ЗДЕСЬ)", new Vector3(0, 0.25f, -3.8f), 3.2f, Hex("2A3140"), false); sub.transform.localRotation = Quaternion.Euler(90, 0, 0);
            // the smiley painted on the window
            var face = Node("Smiley", t, new Vector3(0, 0.22f, 0.4f));
            Cy(face, Vector3.zero, new Vector3(4, 0.03f, 4), M("FFD23A", 0.3f));
            foreach (var sd in new[] { -1, 1 }) Cy(face, new Vector3(sd * 0.7f, 0.03f, 0.6f), new Vector3(0.5f, 0.03f, 0.7f), uiDark);
            for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 2.2f; Cy(face, new Vector3(Mathf.Sin(a) * 1.2f, 0.03f, -0.4f - Mathf.Cos(a) * 0.55f), new Vector3(0.3f, 0.03f, 0.3f), uiDark); }
            // OK / Cancel buttons on the window that do nothing
            B(t, new Vector3(-2.4f, 0.26f, -2.4f), new Vector3(2.6f, 0.1f, 1f), uiBar);
            B(t, new Vector3(2.4f, 0.26f, -2.4f), new Vector3(2.6f, 0.1f, 1f), M("B0BAC6", 0.4f));
            var ok = Prim.Text(t, "ОК?", new Vector3(-2.4f, 0.33f, -2.4f), 3, Color.white); ok.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var no = Prim.Text(t, "НЕТ?", new Vector3(2.4f, 0.33f, -2.4f), 3, Color.white); no.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        static void Slider(Vector3 a, Vector3 b)
        {
            var d = b - a;
            var go = Lobby.Box((a + b) / 2 - Vector3.up * 0.25f, new Vector3(2.4f, 0.5f, d.magnitude), uiBar, 0);
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
            B(go.transform, new Vector3(0, 0.51f, -0.1f), new Vector3(0.6f, 0.02f, 0.75f), ui);
            var knob = Cy(Root, (a + b) / 2 + Vector3.up * 0.1f, new Vector3(1.6f, 0.15f, 1.6f), M("FFFFFF", 0.6f));
            knob.AddComponent<Bob>().amp = 0.05f;
            var t = Prim.Text(Root, "ГРОМКОСТЬ: 300%", a + new Vector3(0, 1.6f, 1), 3, Color.white);
            t.gameObject.AddComponent<Billboard>();
        }

        static void Bar(Vector3 c, Vector3 size)
        {
            var go = Lobby.Box(c, size, ui, 0);
            B(go.transform, new Vector3(0, 0.51f, 0), new Vector3(0.96f, 0.02f, 0.4f), uiBar);
            go.AddComponent<Wobble>();
        }

        static void Checker(Vector3 c, float size)
        {
            var go = Lobby.Box(c - Vector3.up * 0.4f, new Vector3(size, 0.8f, size), missing, 0);
            go.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(size / 4, size / 4);
            for (int k = 0; k < 4; k++)
            {
                var corner = c + new Vector3((k % 2 == 0 ? -1 : 1) * size / 2, -1.2f, (k < 2 ? -1 : 1) * size / 2);
                B(Root, corner, new Vector3(1, 1.6f, 1), M("00FF66", 0.3f)).AddComponent<GlitchJitter>();   // stray polygons
            }
        }

        /// <summary>The wall you can't see: a collider with a joke sign, and a little enemy that keeps bumping into it.</summary>
        static void InvisibleWall(Vector3 c)
        {
            var go = new GameObject("InvisibleWall") { layer = Layers.World };
            go.transform.SetParent(Root, false);
            go.transform.position = c + Vector3.up * 2;
            go.AddComponent<BoxCollider>().size = new Vector3(8, 4, 0.6f);
            var t = Prim.Text(Root, "СТЕНА, КОТОРУЮ ТЫ НЕ ВИДИШЬ\n(ХА-ХА!)", c + new Vector3(0, 3, -0.4f), 4, new Color(1, 1, 1, 0.45f), false);
            t.transform.rotation = Quaternion.identity;
            var b = new GameObject("WallBumper").AddComponent<WallBumper>();
            b.Build(c + new Vector3(1.5f, 0, -0.35f), Root);
        }

        static void MissingTree(Vector3 p, int seed)
        {
            Lobby.Solid(PrimitiveType.Cube, p + Vector3.up * 1.6f, new Vector3(0.7f, 3.2f, 0.7f), missing);
            var crown = B(Root, p + Vector3.up * 4f, new Vector3(3, 2.6f, 3), missing);
            crown.transform.rotation = Quaternion.Euler(0, seed * 25, 0);
            crown.AddComponent<GlitchJitter>();
            var t = Prim.Text(Root, "Texture Missing", p + new Vector3(0, 5.8f, 0), 2.2f, Hex("FF00DC"));
            t.gameObject.AddComponent<Billboard>();
        }

        static void Toggle(Vector3 c, bool on)
        {
            var go = Lobby.Box(c, new Vector3(2.2f, 0.5f, 1.2f), M(on ? "4CE35A" : "B0BAC6", 0.5f), 0);
            go.transform.rotation = Quaternion.Euler(0, 0, 180);   // upside down (it still works as a step)
            S(go.transform, new Vector3(on ? 0.25f : -0.25f, -0.6f, 0), new Vector3(0.45f, 0.6f, 0.9f), M("FFFFFF", 0.6f));
        }

        static void SettingsPanel(Vector3 c)
        {
            var go = Lobby.Box(c - Vector3.up * 0.3f, new Vector3(12, 0.6f, 8), ui, 0);
            // upside-down labels and sliders on the panel
            string[] rows = { "ГРАФИКА: КАРТОШКА", "ЯЗЫК: БАНАНОВЫЙ", "ГРАВИТАЦИЯ: ИНОГДА" };
            for (int k = 0; k < 3; k++)
            {
                var t = Prim.Text(Root, rows[k], c + new Vector3(-1, 0.05f, 2.4f - k * 2.2f), 3.2f, Hex("2A3140"), false);
                t.transform.rotation = Quaternion.Euler(90, 180, 0);
                B(Root, c + new Vector3(3.6f, 0.04f, 2.4f - k * 2.2f), new Vector3(3, 0.04f, 0.3f), uiBar);
                Cy(Root, c + new Vector3(2.6f + k, 0.08f, 2.4f - k * 2.2f), new Vector3(0.6f, 0.06f, 0.6f), M("FFFFFF", 0.6f));
            }
        }

        static void Spinner(Vector3 c, float r, float speed)
        {
            var go = Lobby.Disc(c, r, 0.4f, M("2A3140", 0.4f));
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4;
                B(go.transform, new Vector3(Mathf.Cos(a) * 0.36f, 0.6f, Mathf.Sin(a) * 0.36f), new Vector3(0.12f, 0.1f, 0.06f), G(k == 0 ? "FFFFFF" : "3D8BFF", 1.5f), new Vector3(0, -a * Mathf.Rad2Deg, 0));
            }
            go.AddComponent<Mover>().spin = speed;
        }

        static void Mark(Vector3 p) => Marks.Add(QuestionMark.Spawn(p, Root));

        static void Portal(Vector3 c)
        {
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 1.8f, 1.8f, 0), new Vector3(0.7f, 3.6f, 0.8f), missing);
            Lobby.Box(c + new Vector3(0, 3.9f, 0), new Vector3(4.4f, 0.6f, 1f), missing);
            var surf = Cy(Root, c + new Vector3(0, 1.8f, 0), new Vector3(2.8f, 0.02f, 3.4f), Add("FFC83A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ (ЧЕСТНО)", c + new Vector3(0, 4.6f, 0), 3, Color.white);
            t.gameObject.AddComponent<Billboard>();
            Zones.Add(c + new Vector3(0, 0, 1.2f), 2.2f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1), () => Worlds.Current == 6);
        }

        /// <summary>Floating bits of code, loading icons and upside-down menus all around.</summary>
        static void Decor()
        {
            code = Mats.Emissive(null, Hex("4CE35A"), Hex("4CE35A") * 0.6f);
            string[] lines = { "if (bug) смеяться();", "while(true) прыгать++;", "чубрик.размер = 99999;", "// TODO: починить мир", "босс.scale = 0.1f; // упс", "NullBananaException", "return 404;" };
            var r = new Rng(606);
            for (int i = 0; i < 26; i++)
            {
                var p = O + new Vector3(r.F(-30, 30), r.F(-6, 18), r.F(-10, 115));
                if (Mathf.Abs(p.x - O.x) < 9) p.x += Mathf.Sign(p.x - O.x + 0.01f) * 10;
                var t = Prim.Text(Root, lines[i % lines.Length], p, r.F(3, 6), Hex(i % 3 == 0 ? "4CE35A" : i % 3 == 1 ? "35E0FF" : "FF5FA2"));
                t.gameObject.AddComponent<Billboard>();
                t.gameObject.AddComponent<Bob>().amp = 0.4f;
            }
            for (int i = 0; i < 12; i++)
            {
                var p = O + new Vector3((i % 2 == 0 ? -1 : 1) * r.F(14, 30), r.F(0, 14), r.F(0, 110));
                var ring = Node("Loading", Root, p, new Vector3(90, 0, 0));
                for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Ls(ring, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.6f, 0.5f + k * 0.04f, G("FFFFFF", 0.5f + k * 0.15f)); }
                ring.gameObject.AddComponent<Spin>().axis = Vector3.up;
                ring.GetComponent<Spin>().speed = 200;
            }
            for (int i = 0; i < 10; i++)
            {
                var p = O + new Vector3((i % 2 == 0 ? -1 : 1) * r.F(12, 26), r.F(-4, 10), r.F(0, 110));
                var menu = Node("UpsideDownMenu", Root, p, new Vector3(r.F(0, 40), r.F(0, 360), 180));
                B(menu, Vector3.zero, new Vector3(5, 3.4f, 0.2f), ui);
                B(menu, new Vector3(0, 1.4f, -0.12f), new Vector3(5, 0.6f, 0.05f), uiBar);
                for (int k = 0; k < 3; k++) B(menu, new Vector3(0, 0.4f - k * 0.9f, -0.12f), new Vector3(3.6f, 0.6f, 0.05f), M(k == 0 ? "4CE35A" : "B0BAC6", 0.4f));
                menu.gameObject.AddComponent<Wobble>();
            }
        }

        public static void Win()
        {
            bool first = !Save.P.done.Contains(6);
            if (first) { Save.P.done.Add(6); Save.P.coins += 999; Save.P.tokens += 4; }
            else Save.P.coins += 50;
            Save.Write();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            void Done() => Window.Show("МИР 6 ПРОЙДЕН!", w =>
            {
                w.Text(first ? "Глюк-грань твоя! Награда: 999 монет и 4 жетона (настоящие, без глюков)." : "Глюк-грань снова твоя! +50 монет.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться в глюк-мире", "Спасти друзей и собрать «?»", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
            if (!(first && Story.Show("w6end", Done))) Done();
        }
    }

    /// <summary>A giant UI button you can stand on: ПАУЗА freezes the bosses for a moment, ВЫХОД is a joke.</summary>
    public class GlitchButton : MonoBehaviour
    {
        bool exit, wasOn; Transform cap; float pressT;
        public int Presses { get; private set; }

        public static GlitchButton Spawn(Vector3 top, string label, string hex, Transform parent, bool exit)
        {
            var b = new GameObject("Button " + label).AddComponent<GlitchButton>();
            b.transform.SetParent(parent, false);
            b.transform.position = top;
            b.exit = exit;
            Parts.Layer = Layers.World;
            var keep = Lobby.root; Lobby.root = b.transform;
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, -0.6f, 0), new Vector3(4.4f, 0.6f, 4.4f), M("2A3140", 0.4f));
            Lobby.root = keep;
            b.cap = Node("Cap", b.transform, Vector3.zero);
            Cy(b.cap, new Vector3(0, 0.1f, 0), new Vector3(3.6f, 0.12f, 3.6f), M(hex, 0.5f));
            var t = Prim.Text(b.cap, label, new Vector3(0, 0.25f, 0), 5, Color.white);
            t.transform.localRotation = Quaternion.Euler(90, 0, 0);
            return b;
        }

        void Update()
        {
            pressT = Mathf.MoveTowards(pressT, 0, Time.deltaTime * 3);
            cap.localPosition = Vector3.down * pressT * 0.15f;
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 6) return;
            var d = p.transform.position - transform.position;
            bool on = p.Grounded && new Vector2(d.x, d.z).magnitude < 2.1f && d.y > -0.2f && d.y < 0.8f;
            if (on && !wasOn) Press();
            wasOn = on;
        }

        public void Press()
        {
            Presses++;
            pressT = 1;
            AudioFx.Play("click"); AudioFx.Play(exit ? "error" : "open");
            Hud.Glitch();
            if (exit) Hud.Toast(Presses < 3 ? "Выход не найден. Попробуй ещё раз… (шутка!)" : "Ладно-ладно, выход — это портал. Но тут же весело!", UIKit.Red);
            else { World6.PausedUntil = Time.time + 3; Hud.Toast("ПАУЗА… для всех, кроме тебя! Босс застыл на 3 секунды", UIKit.Gold); }
        }
    }

    /// <summary>A banana peel spinning on the floor: step on it and you slip.</summary>
    public class Banana : MonoBehaviour
    {
        float cd;
        public int Slips { get; private set; }

        public static Banana Spawn(Vector3 p, Transform parent)
        {
            var b = new GameObject("Banana").AddComponent<Banana>();
            b.transform.SetParent(parent, false);
            b.transform.position = p;
            Parts.Layer = Layers.World;
            var yellow = M("FFD23A", 0.5f);
            S(b.transform, new Vector3(0, 0.08f, 0), new Vector3(0.5f, 0.16f, 0.5f), yellow);
            for (int k = 0; k < 4; k++)
            {
                var a = k * 90f;
                var peel = S(b.transform, Quaternion.Euler(0, a, 0) * new Vector3(0, 0.05f, 0.4f), new Vector3(0.24f, 0.07f, 0.6f), yellow);
                peel.transform.localRotation = Quaternion.Euler(0, a, 0);
            }
            S(b.transform, new Vector3(0, 0.2f, 0), new Vector3(0.12f, 0.12f, 0.12f), M("6B4A2A"));
            b.gameObject.AddComponent<Spin>().speed = 120;
            return b;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 6 || Time.time < cd || !p.Grounded) return;
            var d = p.transform.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude < 0.7f && Mathf.Abs(d.y) < 0.6f) Slip(p);
        }

        public void Slip(PlayerCtl p)
        {
            cd = Time.time + 1.5f;
            Slips++;
            p.Hurt(p.transform.position - p.transform.forward);   // whoosh: off you go (no heart lost)
            p.Anim.Flip();
            AudioFx.Play("boing");
            Hud.Toast("Банан! Скольз-скольз…", UIKit.Gold);
        }
    }

    /// <summary>A giant «?» instead of a star: something random — coins, a token, or just a bug.</summary>
    public class QuestionMark : MonoBehaviour
    {
        float back = -1; Transform view;

        public static QuestionMark Spawn(Vector3 p, Transform parent)
        {
            var q = new GameObject("QuestionMark").AddComponent<QuestionMark>();
            q.transform.SetParent(parent, false);
            q.transform.position = p;
            q.view = Node("View", q.transform, Vector3.zero);
            var t = Prim.Text(q.view, "?", Vector3.zero, 14, Hex("FFE14D"));
            t.gameObject.AddComponent<Billboard>();
            Parts.Layer = Layers.World;
            S(q.view, Vector3.zero, 1.4f, Add("FFE14D", 0.15f)).AddComponent<Pulse>();
            q.view.gameObject.AddComponent<Bob>().amp = 0.25f;
            return q;
        }

        public bool Taken => back > 0;

        void Update()
        {
            if (back > 0) { if (Time.time > back) { back = -1; view.gameObject.SetActive(true); } return; }
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 6) return;
            if (Vector3.Distance(p.transform.position + Vector3.up * 0.7f, transform.position) < 1.3f) Take();
        }

        public void Take()
        {
            back = Time.time + 40;
            view.gameObject.SetActive(false);
            AudioFx.Play("magic");
            Effects.Emit("pixel", transform.position, 30, 0.5f);
            int roll = Random.Range(0, 4);
            if (roll == 0) { Save.P.tokens++; Save.Write(); Hud.Toast("«?» → жетон! (повезло)", UIKit.Violet); }
            else if (roll == 1) { Save.AddCoins(40); Hud.Toast("«?» → 40 монет!", UIKit.Gold); }
            else if (roll == 2) { Save.AddCoins(10); Hud.Toast("«?» → 10 монет и немножко глюков", UIKit.Gold); Hud.Glitch(); }
            else { Hud.Toast("«?» → ничего! Это был баг", UIKit.Pink); Hud.Glitch(); }
        }
    }

    /// <summary>A silly enemy that keeps running into the invisible wall.</summary>
    public class WallBumper : MonoBehaviour
    {
        Transform body; Vector3 home; float t;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p; home = p;
            Parts.Layer = Layers.World;
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 0.9f, 1f), Mats.Tex(World6.MissingTex(), Color.white, 0.3f));
            foreach (var sd in new[] { -1, 1 })
            {
                S(body, new Vector3(sd * 0.2f, 0.7f, -0.45f), 0.25f, M("FFFFFF", 0.6f));
                S(body, new Vector3(sd * 0.2f + 0.04f * sd, 0.72f, -0.55f), 0.11f, M("1A1A1A"));
            }
            B(body, new Vector3(0, 0.38f, -0.48f), new Vector3(0.3f, 0.06f, 0.03f), M("1A1A1A"));
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        void Update()
        {
            if (Worlds.Current != 6) return;
            t += Time.deltaTime;
            // run at the wall (+Z) … BONK … stagger back … again
            float c = Mathf.Repeat(t, 2.2f);
            float z = c < 1.2f ? Mathf.Lerp(-2.2f, 0, c / 1.2f) : Mathf.Lerp(0, -2.2f, (c - 1.2f) / 1f);
            transform.position = home + new Vector3(0, 0, z) + Vector3.up * (c < 1.2f ? Mathf.Abs(Mathf.Sin(c * 14)) * 0.1f : 0);
            body.localRotation = Quaternion.Euler(c > 1.2f && c < 1.6f ? -25 : 0, 0, c > 1.2f ? Mathf.Sin(c * 30) * 12 : 0);
            if (c > 1.2f && c - Time.deltaTime <= 1.2f)
            {
                var p = PlayerCtl.I;
                if (p != null && Vector3.Distance(p.transform.position, transform.position) < 18) AudioFx.Play("bonk", 0.5f);
                Effects.Emit("star", transform.position + Vector3.up * 1.2f, 5, 0.3f);
            }
        }
    }

    /// <summary>
    /// Король Горилл в золотой маске — but far too small. He runs around the arena in a panic; every jump on his head
    /// makes him bigger (tiny → small → normal), the third one pops him into confetti and drops the Glitch facet.
    /// </summary>
    public class TinyKing : MonoBehaviour
    {
        Vector3 home; Transform body; TMPro.TextMeshPro sign; Vector3 target; float retarget, bounceCd;
        static readonly float[] Sizes = { 0.22f, 0.5f, 1.1f };
        public int Stomps { get; private set; }
        public bool Defeated { get; private set; }

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p; home = World6.ArenaC;
            Parts.Layer = Layers.World;
            var fur = M("3A3036", 0.3f); var face = M("8A6A5A", 0.3f); var gold = M("FFC83A", 0.9f, 0.85f);
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 1.3f, 0), new Vector3(1.8f, 1.7f, 1.4f), fur);
            S(body, new Vector3(0, 2.4f, 0.15f), new Vector3(1.1f, 1f, 1f), fur);
            S(body, new Vector3(0, 2.3f, 0.55f), new Vector3(0.7f, 0.55f, 0.35f), face);
            B(body, new Vector3(0, 2.55f, 0.6f), new Vector3(0.95f, 0.3f, 0.1f), gold);                 // the golden mask
            foreach (var sd in new[] { -1, 1 })
            {
                S(body, new Vector3(sd * 0.22f, 2.57f, 0.67f), 0.13f, M("FFFFFF", 0.6f));
                Ca(body, new Vector3(sd * 0.95f, 1f, 0.2f), new Vector3(0.45f, 0.75f, 0.45f), fur);
                Ca(body, new Vector3(sd * 0.4f, 0.35f, 0), new Vector3(0.45f, 0.4f, 0.45f), fur);
            }
            for (int k = 0; k < 5; k++) Cone(body, new Vector3(-0.3f + k * 0.15f, 2.85f, 0.1f), new Vector3(0.12f, 0.3f, 0.12f), gold);   // tiny crown
            sign = Prim.Text(transform, "?!", new Vector3(0, 3.6f, 0), 4, Color.white);
            sign.gameObject.AddComponent<Billboard>();
            Apply();
            if (Save.P.done.Contains(6)) { Defeated = true; body.gameObject.SetActive(false); sign.gameObject.SetActive(false); SpawnFacet(); }
        }

        void Apply() { transform.localScale = Vector3.one * Sizes[Mathf.Min(Stomps, 2)]; }

        void Update()
        {
            if (Defeated || Worlds.Current != 6) return;
            var p = PlayerCtl.I;
            float s = Sizes[Mathf.Min(Stomps, 2)];
            if (Time.time >= World6.PausedUntil)
            {
                retarget -= Time.deltaTime;
                if (retarget <= 0 || (target - transform.position).sqrMagnitude < 0.3f)
                {
                    retarget = Random.Range(0.8f, 2f);
                    var r = Random.insideUnitCircle * 6.5f;
                    target = home + new Vector3(r.x, 0, r.y);
                }
                var to = target - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 8);
                    transform.position += to.normalized * (3.2f - Stomps * 0.8f) * Time.deltaTime;
                }
                body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 14)) * 0.15f;
            }
            if (p == null) return;
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz < 1.2f * s + 0.5f && d.y > 2.2f * s && d.y < 3.2f * s + 1.4f && p.Velocity.y < 1 && Time.time > bounceCd) { bounceCd = Time.time + 0.6f; p.Launch(11f); Stomp(); }
        }

        public void Stomp()
        {
            if (Defeated) return;
            Stomps++;
            AudioFx.Play("boing"); AudioFx.Play("hurt", 0.5f);
            Effects.Emit("star", transform.position + Vector3.up * 2, 20, 0.6f);
            if (Stomps >= 3) { Die(); return; }
            Apply();
            sign.text = Stomps == 1 ? "АЙ! ТЕПЕРЬ ПОБОЛЬШЕ" : "ВОТ ТЕПЕРЬ НОРМАЛЬНЫЙ!";
            Hud.Toast(Stomps == 1 ? "Король Горилл подрос! Ещё 2 прыжка" : "Теперь он нормального размера! Последний прыжок!", UIKit.Lime);
        }

        void Die()
        {
            Defeated = true;
            AudioFx.Play("win5");
            Effects.Emit("confetti", transform.position + Vector3.up * 2, 160, 1f, Vector3.up * 5);
            Effects.Emit("pixel", transform.position + Vector3.up * 2, 80, 1f, Vector3.up * 4);
            body.gameObject.SetActive(false);
            sign.gameObject.SetActive(false);
            Hud.Glitch();
            Hud.Toast("Король Горилл лопнул как баг! Возьми Глюк-грань", UIKit.Gold);
            SpawnFacet();
        }

        void SpawnFacet() => new GameObject("GlitchFacet").AddComponent<GlitchFacet>().Build(World6.ArenaC + Vector3.up * 1.2f, transform.parent);
    }

    /// <summary>The Glitch facet of the Heart: take it to finish World 6.</summary>
    public class GlitchFacet : MonoBehaviour
    {
        float cd;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            var gem = Node("Gem", transform, Vector3.zero);
            var mat = Mats.Tex(World6.MissingTex(), Color.white, 0.6f);
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f), mat);
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 0.8f, 1.1f), mat, new Vector3(180, 0, 0));
            S(gem, Vector3.zero, 2.6f, Add("FF5FD2", 0.15f)).AddComponent<Pulse>();
            gem.gameObject.AddComponent<Spin>().speed = 90;
            gem.gameObject.AddComponent<GlitchJitter>();
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Time.time < cd || Worlds.Current != 6) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 2.4f && new Vector2(d.x, d.z).magnitude < 1.8f) { cd = Time.time + 6; World6.Win(); }
        }
    }

    /// <summary>Space distortion: a slow wobble of scale and tilt.</summary>
    public class Wobble : MonoBehaviour
    {
        Vector3 s0; Quaternion r0; float ph;
        void Start() { s0 = transform.localScale; r0 = transform.localRotation; ph = Random.value * 10; }
        void Update()
        {
            if (Worlds.Current != 6) return;
            float t = Time.time + ph;
            transform.localScale = new Vector3(s0.x * (1 + Mathf.Sin(t * 1.3f) * 0.04f), s0.y, s0.z * (1 + Mathf.Cos(t * 1.1f) * 0.04f));
            transform.localRotation = r0 * Quaternion.Euler(Mathf.Sin(t * 0.7f) * 1.5f, 0, Mathf.Cos(t * 0.9f) * 1.5f);
        }
    }

    /// <summary>Glitch: now and then the thing jumps sideways for a frame or two.</summary>
    public class GlitchJitter : MonoBehaviour
    {
        Vector3 p0; float next, until; bool moved;
        void Start() { p0 = transform.localPosition; next = Time.time + Random.Range(0.5f, 3f); }
        void Update()
        {
            if (Worlds.Current != 6) return;
            if (!moved && Time.time > next) { moved = true; until = Time.time + 0.08f; transform.localPosition = p0 + Random.insideUnitSphere * 0.4f; }
            if (moved && Time.time > until) { moved = false; transform.localPosition = p0; next = Time.time + Random.Range(0.8f, 3.5f); }
        }
    }
}
