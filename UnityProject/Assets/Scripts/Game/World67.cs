using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// «Мир 67: Глючный Сикс-Севен» (world id 7). A slightly broken neon world built round the «67» (six-seven) meme:
    /// «67» everywhere, giant 6 and 7 that swap places with a modem screech, the sky that blinks into a grid of 67,
    /// flickering platforms, a trail of 6–7 copies when you jump, fake error pop-ups that shatter, music that
    /// stutters every 6–7 seconds, choppy 7-fps enemies. The quest says 67 friends and 67 keys… but only 8 exist —
    /// at the gate with 67 locks the «67» falls off and a taped paper «8» is under it: the level counts as 100%.
    /// </summary>
    public static class World67
    {
        public const int Id = 7;
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 7000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 1.5f);
        public static float FallY => O.y - 14f;
        public static Gate67 Gate;
        public static readonly List<Bot67> Bots = new List<Bot67>();
        public static readonly List<FlickerPlatform> Flickers = new List<FlickerPlatform>();
        public static readonly List<Transform> FakeWalls = new List<Transform>();
        public static BouncePad FirstTrampoline;
        static Material floor, floorDark, neon, neonPink, neonCyan;

        public static void Build()
        {
            Root = new GameObject("World67").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            Bots.Clear(); Flickers.Clear(); FakeWalls.Clear();
            floor = Mats.Tex(Tex67(false), Color.white, 0.25f);
            floorDark = Mats.Tex(Tex67(true), Color.white, 0.25f);
            neon = G("39FF6A", 1.6f); neonPink = G("FF3DF2", 1.6f); neonCyan = G("35E0FF", 1.6f);

            // ---- start: the plaza, the big pompous sign, the way back ----
            Plat(V(0, 0, 0), new Vector3(18, 1, 16));
            Portal(V(-6.5f, 0, -5));
            StartSign(V(0, 0, 7.4f));
            Rescue.SpawnCage("w7c0", V(6.5f, 0, -4), -40, Root);
            Pickups.I.Key("w7k0", V(-6.5f, 1.2f, 4));
            for (int k = 0; k < 6; k++) Pickups.I.Coin(V(-3 + k * 1.2f, 0.9f, 3));

            // ---- section 1: flickering stepping stones ----
            for (int k = 0; k < 6; k++) Flicker(V((k % 2 == 0 ? -2.5f : 2.5f), 0.4f * k, 12 + k * 5), new Vector3(3.6f, 0.6f, 3.6f));
            Popup3D.Spawn(V(5, 3.2f, 20), "Ошибка 67:\nслишком много чилла", Root);
            Popup3D.Spawn(V(-5.5f, 4, 31), "Загрузка 67%…", Root);

            // ---- plaza A: walking digits, a trampoline up to a ledge, a fake wall with a secret nook ----
            Plat(V(0, 2.5f, 49), new Vector3(14, 1, 14));
            Checkpoint(V(-4, 2.5f, 44));
            Bots.Add(Bot67.Spawn(1, V(-4, 2.5f, 50), V(4, 2.5f, 50), Root));
            Bots.Add(Bot67.Spawn(2, V(3, 2.5f, 54), V(3, 2.5f, 45), Root));
            Rescue.SpawnCage("w7c1", V(-5, 2.5f, 54), 120, Root);
            FirstTrampoline = Trampoline(V(5, 2.5f, 53), 17f);
            Plat(V(5, 9, 60), new Vector3(4, 0.6f, 4));
            Pickups.I.Key("w7k1", V(5, 10.3f, 60));
            Pickups.I.Token("w7t0", V(5, 10.4f, 61.4f));
            FakeWall(V(-7.3f, 2.5f, 49), 90, 6);   // walk through it!
            Plat(V(-11, 2.5f, 49), new Vector3(6, 1, 6));
            Pickups.I.Key("w7k2", V(-11.5f, 3.7f, 49));
            for (int k = 0; k < 4; k++) Pickups.I.Coin(V(-10 + (k % 2) * 1.6f, 3.4f, 47.5f + (k / 2) * 3));

            // ---- section 2: moving platforms and a glitchy loading bar ----
            Moving(V(-3, 2.5f, 60), V(3, 2.5f, 60), 3.5f);
            Moving(V(3, 3, 66), V(-3, 3, 66), 3f);
            Moving(V(0, 3.5f, 72), V(0, 3.5f, 76), 2.6f);
            LoadingBar(V(0, 4, 79), V(0, 4, 91));
            Popup3D.Spawn(V(-4, 6, 85), "Six-Seven detected", Root);

            // ---- plaza B: glitch bots, a trampoline tower ----
            Plat(V(0, 4, 99), new Vector3(16, 1, 14));
            Checkpoint(V(-5, 4, 94));
            Bots.Add(Bot67.Spawn(0, V(-5, 4, 101), V(5, 4, 101), Root));
            Bots.Add(Bot67.Spawn(0, V(5, 4, 96), V(-2, 4, 104), Root));
            Rescue.SpawnCage("w7c2", V(-6.5f, 4, 104), 110, Root);
            Rescue.SpawnCage("w7c3", V(6.5f, 4, 104), -110, Root);
            Trampoline(V(6, 4, 95), 15f);
            Plat(V(6, 10, 92), new Vector3(3.6f, 0.6f, 3.6f));
            Trampoline(V(6, 10, 92), 15f);
            Plat(V(1, 16, 90), new Vector3(4, 0.6f, 4));
            Pickups.I.Key("w7k3", V(1, 17.3f, 90));
            for (int k = 0; k < 5; k++) Pickups.I.Coin(V(6, 11.5f + k * 1.1f, 92));

            // ---- section 3: giant 6 and 7 lying flat as platforms ----
            Digit(6, V(-3, 5, 116), 1f);
            Digit(7, V(3.5f, 6, 132), 1f);
            Pickups.I.Key("w7k4", V(5.5f, 7.3f, 137));
            Rescue.SpawnCage("w7c4", V(-3, 5, 116), 0, Root);
            Bots.Add(Bot67.Spawn(1, V(-5, 5, 112), V(-1, 5, 112), Root));

            // ---- plaza C: another fake wall, more bots ----
            Plat(V(0, 6, 152), new Vector3(14, 1, 14));
            Checkpoint(V(0, 6, 147));
            Bots.Add(Bot67.Spawn(2, V(-4, 6, 154), V(4, 6, 154), Root));
            Rescue.SpawnCage("w7c5", V(5, 6, 157), -150, Root);
            FakeWall(V(7.3f, 6, 152), 90, 6);
            Plat(V(11, 6, 152), new Vector3(6, 1, 6));
            Pickups.I.Key("w7k5", V(11.5f, 7.3f, 152));
            Pickups.I.Token("w7t1", V(12, 7.3f, 154));
            Popup3D.Spawn(V(-5, 8.5f, 156), "Ошибка 67:\nслишком много чилла", Root);

            // ---- section 4: a big gap — fast flickers and glitchy trampolines ----
            Trampoline(V(0, 6, 158), 14f);
            for (int k = 0; k < 4; k++) Flicker(V((k % 2 == 0 ? 2 : -2), 6 + k * 0.3f, 165 + k * 5), new Vector3(3.2f, 0.6f, 3.2f), true);
            Pickups.I.Key("w7k6", V(0, 10, 172));   // in the air over the gap
            Plat(V(0, 7.5f, 186), new Vector3(6, 0.6f, 5));
            Trampoline(V(0, 7.5f, 186), 13f);

            // ---- the final plaza: the gate with 67 locks ----
            Plat(V(0, 6, 202), new Vector3(20, 1, 18));
            Checkpoint(V(0, 6, 195));
            Rescue.SpawnCage("w7c6", V(-7.5f, 6, 197), 60, Root);
            Rescue.SpawnCage("w7c7", V(7.5f, 6, 197), -60, Root);
            Pickups.I.Key("w7k7", V(-8, 7.2f, 205));
            Gate = new GameObject("Gate67").AddComponent<Gate67>();
            Gate.Build(V(0, 6, 209), Root);

            Decor();
            var fx = new GameObject("World67Fx").AddComponent<World67Fx>();
            fx.transform.SetParent(Root, false);
            Lobby.root = keep;
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- textures ----------------

        static readonly string[] Six = { "###", "#..", "###", "#.#", "###" }, Seven = { "###", "..#", ".#.", ".#.", ".#." };

        /// <summary>Draws a pixel «6» or «7» (3×5 cells) into the canvas.</summary>
        public static void PixelDigit(Canvas2 cv, int x, int y, int d, int cell, Color c)
        {
            var rows = d == 6 ? Six : Seven;
            for (int r = 0; r < 5; r++)
                for (int k = 0; k < 3; k++)
                    if (rows[r][k] == '#') cv.Rect(x + k * cell, y + (4 - r) * cell, cell, cell, c);
        }

        static readonly Dictionary<bool, Texture2D> texCache = new Dictionary<bool, Texture2D>();
        /// <summary>Glitchy neon «grass» with 67 written all over it.</summary>
        public static Texture2D Tex67(bool dark)
        {
            if (texCache.TryGetValue(dark, out var have) && have) return have;
            var r = new Rng(dark ? 67 : 76);
            var cv = new Canvas2(128, 128, dark ? Hex("1E0F3C") : Hex("3A1C78"));
            for (int i = 0; i < 300; i++) cv.Rect(r.I(128), r.I(128), 2 + r.I(4), 1 + r.I(2), dark ? Hex("2A1658") : Hex("4A2894"));
            for (int i = 0; i < 6; i++) cv.Rect(0, r.I(128), 128, 1, Hex(i % 2 == 0 ? "39FF6A" : "FF3DF2") * new Color(1, 1, 1, 0.5f));   // scan lines
            for (int i = 0; i < 7; i++)
            {
                int x = r.I(110), y = r.I(110);
                var c = Hex(i % 3 == 0 ? "39FF6A" : i % 3 == 1 ? "35E0FF" : "FF3DF2");
                PixelDigit(cv, x, y, 6, 2, c); PixelDigit(cv, x + 8, y, 7, 2, c);
            }
            return texCache[dark] = cv.Tex(true, true, FilterMode.Point);
        }

        // ---------------- pieces ----------------

        static GameObject Plat(Vector3 top, Vector3 size, bool dark = false)
        {
            var go = Lobby.Box(top - Vector3.up * size.y / 2, size, dark ? floorDark : floor, 0);
            go.GetComponent<Renderer>().sharedMaterial = dark ? floorDark : floor;
            // neon rim
            B(go.transform, new Vector3(0, 0.5f, 0.5f), new Vector3(1.01f, 0.06f, 0.02f), neon);
            B(go.transform, new Vector3(0, 0.5f, -0.5f), new Vector3(1.01f, 0.06f, 0.02f), neonPink);
            return go;
        }

        static void Flicker(Vector3 top, Vector3 size, bool fast = false)
        {
            var go = Plat(top, size, true);
            var f = go.AddComponent<FlickerPlatform>();
            f.fast = fast;
            Flickers.Add(f);
        }

        static BouncePad Trampoline(Vector3 top, float power)
        {
            var t = Node("Trampoline67", Root, top);
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(2.4f, 0.15f, 2.4f), M("2A1658", 0.4f), default, t);
            var skin = Node("Skin", t, new Vector3(0, 0.32f, 0));
            Cy(skin, Vector3.zero, new Vector3(2.1f, 0.04f, 2.1f), neon);
            var txt = Prim.Text(skin, "67", new Vector3(0, 0.06f, 0), 5, Hex("1E0F3C"));
            txt.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.35f, 0); col.size = new Vector3(2, 0.3f, 2);
            var pad = t.gameObject.AddComponent<BouncePad>();
            pad.power = power; pad.top = skin; pad.glitch = true;
            return pad;
        }

        static void Moving(Vector3 a, Vector3 b, float period)
        {
            var go = Plat(a, new Vector3(3.4f, 0.6f, 3.4f), true);
            var m = go.AddComponent<Mover>();
            m.a = go.transform.position; m.b = go.transform.position + (b - a); m.period = period;
        }

        /// <summary>A long thin «loading bar» bridge (it jitters a little, but holds).</summary>
        static void LoadingBar(Vector3 a, Vector3 b)
        {
            var d = b - a;
            var go = Lobby.Box((a + b) / 2 - Vector3.up * 0.3f, new Vector3(2.2f, 0.6f, d.magnitude), M("1E0F3C", 0.4f), 0);
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
            B(go.transform, new Vector3(0, 0.51f, -0.17f), new Vector3(0.8f, 0.02f, 0.66f), neon);   // 67% filled
            var t = Prim.Text(Root, "ЗАГРУЗКА МОСТА: 67%", a + new Vector3(0, 2, 1.5f), 3, Hex("39FF6A"));
            t.gameObject.AddComponent<Billboard>();
            go.AddComponent<GlitchJitter>();
        }

        /// <summary>A wall that looks solid — but it's a glitch: you walk right through it into a secret nook.</summary>
        static void FakeWall(Vector3 baseC, float yaw, float width)
        {
            var w = B(Root, baseC + Vector3.up * 2.5f, new Vector3(width, 5, 0.8f), floorDark, new Vector3(0, yaw, 0));
            w.name = "FakeWall";
            w.AddComponent<GlitchJitter>();
            FakeWalls.Add(w.transform);
            var t = Prim.Text(w.transform, "67", new Vector3(0, 0.1f, -0.6f), 6, Hex("39FF6A"));
            t.transform.localRotation = Quaternion.identity;
        }

        /// <summary>A giant digit lying flat to walk on, made of 7-segment bars.</summary>
        static void Digit(int d, Vector3 c, float s)
        {
            // segments: a top, b top-right, c bottom-right, d bottom, e bottom-left, f top-left, g middle
            bool[] on = d == 6 ? new[] { true, false, true, true, true, true, true } : new[] { true, true, true, false, false, false, false };
            float W = 7 * s, H = 12 * s, T = 2.6f * s;
            var segs = new (Vector3 p, Vector3 size)[]
            {
                (new Vector3(0, 0, H / 2), new Vector3(W, 1, T)), (new Vector3(W / 2, 0, H / 4), new Vector3(T, 1, H / 2 + T / 2)),
                (new Vector3(W / 2, 0, -H / 4), new Vector3(T, 1, H / 2 + T / 2)), (new Vector3(0, 0, -H / 2), new Vector3(W, 1, T)),
                (new Vector3(-W / 2, 0, -H / 4), new Vector3(T, 1, H / 2 + T / 2)), (new Vector3(-W / 2, 0, H / 4), new Vector3(T, 1, H / 2 + T / 2)),
                (new Vector3(0, 0, 0), new Vector3(W, 1, T)),
            };
            for (int i = 0; i < 7; i++) if (on[i]) Plat(c + segs[i].p, segs[i].size, d == 7);
        }

        static void Checkpoint(Vector3 p) => new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);

        static void Portal(Vector3 c)
        {
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 1.8f, 1.8f, 0), new Vector3(0.7f, 3.6f, 0.8f), floorDark);
            Lobby.Box(c + new Vector3(0, 3.9f, 0), new Vector3(4.4f, 0.6f, 1f), floorDark);
            var surf = Cy(Root, c + new Vector3(0, 1.8f, 0), new Vector3(2.8f, 0.02f, 3.4f), Add("39FF6A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ (67% ЧЕСТНО)", c + new Vector3(0, 4.7f, 0), 3, Color.white);
            t.gameObject.AddComponent<Billboard>();
            Zones.Add(c + new Vector3(0, 0, 1.2f), 2.2f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1), () => Worlds.Current == Id);
        }

        /// <summary>The big pompous sign at the start with the quest.</summary>
        static void StartSign(Vector3 c)
        {
            var s = Node("StartSign", Root, c);
            foreach (var sd in new[] { -1, 1 }) Lobby.Solid(PrimitiveType.Cylinder, new Vector3(sd * 5.5f, 2.5f, 0), new Vector3(0.5f, 2.5f, 0.5f), M("8A919C", 0.6f, 0.5f), default, s);
            B(s, new Vector3(0, 5.6f, 0), new Vector3(12.5f, 3.6f, 0.4f), M("1E0F3C", 0.4f));
            B(s, new Vector3(0, 5.6f, -0.21f), new Vector3(12.9f, 4, 0.02f), neon);
            B(s, new Vector3(0, 5.6f, -0.23f), new Vector3(12.3f, 3.4f, 0.02f), M("1E0F3C", 0.4f));
            var t = Prim.Text(s, "ВНИМАНИЕ, ЧУБРИК!\nСПАСИ 67 ДРУЗЕЙ · НАЙДИ 67 КЛЮЧЕЙ", new Vector3(0, 5.6f, -0.3f), 4.6f, Hex("39FF6A"));
            t.transform.localRotation = Quaternion.identity;
            t.rectTransform.sizeDelta = new Vector2(12, 3.2f);
            t.enableAutoSizing = true; t.fontSizeMin = 1; t.fontSizeMax = 4.6f;
            s.gameObject.AddComponent<GlitchJitter>();
        }

        /// <summary>«67» on clouds and rocks, floating «SIX SEVEN» words, the two giant digits.</summary>
        static void Decor()
        {
            var r = new Rng(6767);
            for (int i = 0; i < 18; i++)
            {
                var p = O + new Vector3((i % 2 == 0 ? -1 : 1) * r.F(16, 36), r.F(-4, 20), r.F(-10, 215));
                var cloud = Node("Cloud67", Root, p);
                for (int k = 0; k < 4; k++) S(cloud, new Vector3((k - 1.5f) * 2.2f, r.F(-0.4f, 0.6f), 0), r.F(2.6f, 4f), M(i % 3 == 0 ? "D8C8FF" : "F4F0FF", 0.05f));
                var t = Prim.Text(cloud, i % 2 == 0 ? "67" : "SIX SEVEN", new Vector3(0, 0.2f, -2), i % 2 == 0 ? 6 : 3, Hex(i % 3 == 0 ? "FF3DF2" : "39FF6A"));
                t.gameObject.AddComponent<Billboard>();
                cloud.gameObject.AddComponent<Bob>().amp = 0.6f;
            }
            for (int i = 0; i < 14; i++)
            {
                var p = O + new Vector3((i % 2 == 0 ? -1 : 1) * r.F(12, 24), r.F(-8, 4), r.F(0, 210));
                var rock = Lobby.Box(p, Vector3.one * r.F(2, 4), floorDark, r.F(0, 90));
                rock.transform.rotation = Quaternion.Euler(r.F(0, 40), r.F(0, 360), r.F(0, 40));
                rock.AddComponent<Bob>().amp = 0.3f;
            }
            World67Fx.BigSix = BigDigit("6", O + new Vector3(-26, 22, 110));
            World67Fx.BigSeven = BigDigit("7", O + new Vector3(26, 22, 110));
        }

        static Transform BigDigit(string d, Vector3 p)
        {
            var t = Prim.Text(Root, d, p, 60, Hex(d == "6" ? "39FF6A" : "FF3DF2"));
            t.fontStyle = FontStyles.Bold;
            t.gameObject.AddComponent<Billboard>();
            return t.transform;
        }

        /// <summary>The level is done (the gate with 67 locks opened): 100%.</summary>
        public static void Win()
        {
            bool first = !Save.P.done.Contains(Id);
            if (first) { Save.P.done.Add(Id); Save.P.coins += 667; Save.P.tokens += 6; }
            else Save.P.coins += 67;
            Save.Write();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            void Done() => Window.Show("МИР 67 ПРОЙДЕН НА 100%!", w =>
            {
                w.Text(first ? "Ты спас всех 8 из 67 друзей (то есть всех!). Награда: 667 монет и 6 жетонов." : "Мир 67 снова пройден! +67 монет.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться и потанцевать", "Друзья празднуют у ворот", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
            if (!(first && Story.Show("w7end", Done))) Done();
        }
    }

    /// <summary>A platform that flickers now and then: see-through for a moment with red/cyan ghost copies, or a twitch
    /// to the side — but it always stays solid.</summary>
    public class FlickerPlatform : MonoBehaviour
    {
        public bool fast;
        Renderer rend; Material solid, ghost; Transform redC, cyanC;
        Vector3 basePos; float next, until; bool shifted;
        public int Flickers { get; private set; }

        void Start()
        {
            rend = GetComponent<Renderer>();
            solid = rend.sharedMaterial;
            ghost = Parts.Glass("8A5CFF", 0.35f);
            basePos = transform.localPosition;
            redC = Ghost("FF2A5A", new Vector3(0.06f, 0, 0)); cyanC = Ghost("2AE8FF", new Vector3(-0.06f, 0, 0));
            next = Time.time + Random.Range(0.5f, 3f);
        }

        Transform Ghost(string hex, Vector3 off)
        {
            var g = Parts.B(transform, off, Vector3.one * 1.02f, Parts.Add(hex, 0.35f));
            var gc = g.GetComponent<Collider>(); if (gc) Destroy(gc);
            g.SetActive(false);
            return g.transform;
        }

        public void Flick()
        {
            Flickers++;
            until = Time.time + 0.22f;
            if (Random.value < 0.6f) { rend.sharedMaterial = ghost; redC.gameObject.SetActive(true); cyanC.gameObject.SetActive(true); }
            else { shifted = true; transform.localPosition = basePos + transform.right * Random.Range(-0.06f, 0.06f); }
        }

        void Update()
        {
            if (Worlds.Current != World67.Id) return;
            if (until > 0 && Time.time > until)
            {
                until = 0;
                rend.sharedMaterial = solid; redC.gameObject.SetActive(false); cyanC.gameObject.SetActive(false);
                if (shifted) { shifted = false; transform.localPosition = basePos; }
            }
            if (Time.time > next) { next = Time.time + (fast ? Random.Range(0.6f, 1.6f) : Random.Range(1.6f, 4f)); Flick(); }
        }
    }

    /// <summary>A fake error window floating in the level: touch it and it shatters into pixels.</summary>
    public class Popup3D : MonoBehaviour
    {
        public bool Broken { get; private set; }
        public static readonly List<Popup3D> All = new List<Popup3D>();

        public static Popup3D Spawn(Vector3 p, string text, Transform parent)
        {
            var go = new GameObject("ErrWindow3D") { layer = Layers.World };
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            var pop = go.AddComponent<Popup3D>();
            Parts.B(go.transform, Vector3.zero, new Vector3(3.6f, 2.2f, 0.12f), Parts.M("D4D0C8", 0.3f));
            Parts.B(go.transform, new Vector3(0, 0.9f, -0.07f), new Vector3(3.5f, 0.36f, 0.02f), Parts.M("0A246A", 0.3f));
            Parts.B(go.transform, new Vector3(1.55f, 0.9f, -0.09f), new Vector3(0.3f, 0.28f, 0.02f), Parts.M("D4D0C8", 0.3f));
            var title = Prim.Text(go.transform, "Chubrik.exe", new Vector3(-0.6f, 0.9f, -0.1f), 1.6f, Color.white);
            title.transform.localRotation = Quaternion.identity;
            var t = Prim.Text(go.transform, text, new Vector3(0, -0.15f, -0.1f), 2.2f, Hex("1A1A1A"), false);
            t.transform.localRotation = Quaternion.identity;
            t.rectTransform.sizeDelta = new Vector2(3.3f, 1.5f);
            t.enableAutoSizing = true; t.fontSizeMin = 0.5f; t.fontSizeMax = 2.2f;
            go.AddComponent<Billboard>();
            go.AddComponent<Bob>().amp = 0.2f;
            All.Add(pop);
            return pop;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (Broken || p == null || Worlds.Current != World67.Id) return;
            if (Vector3.Distance(p.transform.position + Vector3.up * 0.8f, transform.position) < 2.2f) Break();
        }

        public void Break()
        {
            if (Broken) return;
            Broken = true;
            AudioFx.Play("crate");
            for (int i = 0; i < 40; i++)
            {
                var px = Parts.B(null, transform.position + new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(-1f, 1f), 0), Vector3.one * Random.Range(0.12f, 0.28f),
                                 Parts.M(i % 3 == 0 ? "0A246A" : i % 3 == 1 ? "D4D0C8" : "39FF6A", 0.3f));
                px.AddComponent<PixelBit>().vel = new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 5f), Random.Range(-3f, 3f));
            }
            gameObject.SetActive(false);
        }
    }

    /// <summary>A flying pixel of a broken window (or a digit of the glitch fireworks): falls, spins and vanishes.</summary>
    public class PixelBit : MonoBehaviour
    {
        public Vector3 vel; public float life = 1.2f, gravity = 9f;
        float t;
        void Update()
        {
            t += Time.deltaTime;
            vel.y -= gravity * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.Rotate(200 * Time.deltaTime, 160 * Time.deltaTime, 0);
            transform.localScale *= 1 - Time.deltaTime * 0.8f;
            if (t > life) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Enemies of World 67: a Glitch-bot (0) and walking «6» (1) and «7» (2). They move with choppy 7 frames a second
    /// (Chubrik stays smooth). Jump on them or hit them; touching them from the side bumps you back.
    /// </summary>
    public class Bot67 : MonoBehaviour, IHittable
    {
        public int kind;
        public bool Dead { get; private set; }
        Vector3 a, b, logical; float t, frameT, hitCd;
        Transform body, legL, legR;
        int frame;

        public static Bot67 Spawn(int kind, Vector3 a, Vector3 b, Transform parent)
        {
            var go = new GameObject("Bot67 " + kind) { layer = Layers.World };
            go.transform.SetParent(parent, false);
            go.transform.position = a;
            var e = go.AddComponent<Bot67>();
            e.kind = kind; e.a = a; e.b = b; e.logical = a; e.t = Random.value;
            e.Build();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.center = new Vector3(0, 0.9f, 0); col.radius = 0.8f;
            return e;
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            body = Parts.Node("Body", transform, Vector3.zero);
            var dark = Parts.M("1E0F3C", 0.4f);
            if (kind == 0)
            {
                // the glitch-bot: a boxy robot with a screen showing «67»
                Parts.B(body, new Vector3(0, 0.8f, 0), new Vector3(1.1f, 1f, 0.8f), Parts.M("8A93A0", 0.6f, 0.4f));
                Parts.B(body, new Vector3(0, 0.85f, 0.41f), new Vector3(0.8f, 0.6f, 0.02f), dark);
                var t = Prim.Text(body, "67", new Vector3(0, 0.85f, 0.43f), 2.4f, Hex("39FF6A"));
                t.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Parts.Rod(body, new Vector3(0, 1.3f, 0), new Vector3(0, 1.7f, 0), 0.04f, dark);
                Parts.S(body, new Vector3(0, 1.75f, 0), 0.16f, Parts.G("FF3DF2", 2f)).AddComponent<Pulse>();
                Parts.B(body, new Vector3(0.72f, 0.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), Parts.G("39FF6A", 1.4f)).AddComponent<GlitchJitter>();   // a stray polygon
            }
            else
            {
                // a walking «6» or «7» with googly eyes
                var col = kind == 1 ? "39FF6A" : "FF3DF2";
                var t = Prim.Text(body, kind == 1 ? "6" : "7", new Vector3(0, 1.05f, 0), 9, Hex(col));
                t.fontStyle = FontStyles.Bold;
                t.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var back = Prim.Text(body, kind == 1 ? "6" : "7", new Vector3(0, 1.05f, -0.02f), 9, Hex(col));
                back.fontStyle = FontStyles.Bold;
                foreach (var sd in new[] { -1, 1 })
                {
                    Parts.S(body, new Vector3(sd * 0.18f, 1.5f, 0.12f), 0.24f, Parts.M("FFFFFF", 0.5f));
                    Parts.S(body, new Vector3(sd * 0.18f, 1.48f, 0.22f), 0.11f, Parts.M("111111", 0.9f));
                }
            }
            legL = Parts.Node("LegL", body, new Vector3(-0.22f, 0.3f, 0)); legR = Parts.Node("LegR", body, new Vector3(0.22f, 0.3f, 0));
            foreach (var l in new[] { legL, legR })
            {
                Parts.Cy(l, new Vector3(0, -0.15f, 0), new Vector3(0.12f, 0.15f, 0.12f), dark);
                Parts.S(l, new Vector3(0, -0.3f, 0.06f), new Vector3(0.24f, 0.12f, 0.32f), dark);
            }
        }

        public bool OnHit(Vector3 from) { Die(); return true; }

        public void Die()
        {
            if (Dead) return;
            Dead = true;
            AudioFx.Play("crate"); AudioFx.Play("zap", 0.5f);
            for (int i = 0; i < 12; i++)
            {
                var d = Prim.Text(null, i % 2 == 0 ? "6" : "7", transform.position + Vector3.up * 1f, 3, Hex(i % 3 == 0 ? "39FF6A" : "FF3DF2"));
                d.gameObject.AddComponent<Billboard>();
                var pb = d.gameObject.AddComponent<PixelBit>(); pb.vel = new Vector3(Random.Range(-3f, 3f), Random.Range(3f, 7f), Random.Range(-3f, 3f));
            }
            Destroy(gameObject);
        }

        void Update()
        {
            if (Dead || Worlds.Current != World67.Id) return;
            // the real movement is smooth…
            t += Time.deltaTime * 0.18f;
            float k = Mathf.PingPong(t, 1);
            var prev = logical;
            logical = Vector3.Lerp(a, b, k);
            // …but the picture updates only 7 times a second
            frameT += Time.deltaTime;
            if (frameT >= 1f / 7f)
            {
                frameT = 0; frame++;
                transform.position = logical + (Random.value < 0.15f ? new Vector3(Random.Range(-0.15f, 0.15f), 0, 0) : Vector3.zero);
                var dir = logical - prev; dir.y = 0;
                if (dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(dir);
                float sw = frame % 2 == 0 ? 25 : -25;
                legL.localRotation = Quaternion.Euler(sw, 0, 0); legR.localRotation = Quaternion.Euler(-sw, 0, 0);
                body.localRotation = Quaternion.Euler(0, 0, frame % 2 == 0 ? 6 : -6);
            }
            var p = PlayerCtl.I;
            if (p == null) return;
            hitCd -= Time.deltaTime;
            var d = p.transform.position - logical; float dy = d.y; d.y = 0;
            if (d.magnitude < 1.1f && dy > 1.0f && dy < 2.6f && p.Velocity.y < 0.5f) { Die(); p.Launch(9); return; }   // jumped on its head
            if (d.magnitude < 0.9f && dy > -0.5f && dy < 1.0f && hitCd <= 0) { hitCd = 1f; p.HurtBy(logical); }
        }
    }

    /// <summary>The gate with 67 locks at the end — and the joke about the 8 keys.</summary>
    public class Gate67 : MonoBehaviour
    {
        public bool Opened { get; private set; }
        public bool Started { get; private set; }
        Transform doorL, doorR, six, seven, paper;
        float hintT;
        readonly List<GameObject> dancers = new List<GameObject>();

        public void Build(Vector3 c, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = c;
            Parts.Layer = Layers.World;
            var keep = Lobby.root; Lobby.root = transform;
            var stone = Parts.M("2A1658", 0.4f);
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(new Vector3(sd * 6.2f, 5, 0), new Vector3(2, 10, 2), stone);
            Lobby.Box(new Vector3(0, 10.5f, 0), new Vector3(14.4f, 1.6f, 2.2f), stone);
            Lobby.root = keep;
            doorL = Door(new Vector3(-5.2f, 0, -0.2f), 1); doorR = Door(new Vector3(5.2f, 0, -0.2f), -1);
            // the sign over the gate: «67» … and a taped paper «8» hidden behind it
            var board = Parts.B(transform, new Vector3(0, 13.2f, -0.2f), new Vector3(7, 3.4f, 0.4f), Parts.M("1E0F3C", 0.4f));
            Parts.B(board.transform, new Vector3(0, 0, -0.52f), new Vector3(1.04f, 1.06f, 0.05f), Parts.G("39FF6A", 1.2f));
            paper = Parts.Node("Paper8", transform, new Vector3(0, 13.2f, -0.45f));
            Parts.B(paper, Vector3.zero, new Vector3(2.6f, 2.9f, 0.04f), Parts.M("FFFDF4", 0.1f), new Vector3(0, 0, -4));
            var eight = Prim.Text(paper, "8", new Vector3(0, -0.1f, -0.05f), 22, Hex("1A1A1A"), false);
            eight.transform.localRotation = Quaternion.Euler(0, 0, -4);
            foreach (var sd in new[] { -1, 1 }) Parts.B(paper, new Vector3(sd * 1.05f, 1.35f, -0.04f), new Vector3(0.9f, 0.3f, 0.02f), Parts.Glass("F2E6B0", 0.6f), new Vector3(0, 0, sd * 30));   // tape
            paper.gameObject.SetActive(false);
            six = Prim.Text(transform, "6", new Vector3(-1.4f, 13.2f, -0.5f), 26, Hex("39FF6A")).transform;
            seven = Prim.Text(transform, "7", new Vector3(1.4f, 13.2f, -0.5f), 26, Hex("FF3DF2")).transform;
            six.localRotation = seven.localRotation = Quaternion.identity;
        }

        Transform Door(Vector3 hinge, int side)
        {
            var h = Parts.Node("Door", transform, hinge);
            var d = Lobby.Solid(PrimitiveType.Cube, new Vector3(side * 2.6f, 4.6f, 0), new Vector3(5.2f, 9.2f, 0.5f), Parts.M("3A1C78", 0.4f), default, h);
            // 67 padlocks (34 on one door, 33 on the other)
            int n = side > 0 ? 34 : 33;
            var gold = Parts.M("FFC83A", 0.8f, 0.7f);
            for (int i = 0; i < n; i++)
            {
                float x = side * (0.5f + (i % 6) * 0.8f), y = 1 + (i / 6) * 1.3f;
                Parts.B(h, new Vector3(x, y, -0.32f), new Vector3(0.4f, 0.34f, 0.12f), gold);
                Parts.Ring(h, new Vector3(x, y + 0.24f, -0.32f), 0.12f, 0.035f, Parts.M("C9D1DB", 0.8f, 0.6f));
            }
            return h;
        }

        void Update()
        {
            if (Started || Worlds.Current != World67.Id) return;
            var p = PlayerCtl.I;
            if (p == null) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.magnitude > 9) return;
            int saved = Rescue.Rescued("w7");
            if (saved >= 8) { Begin(); return; }
            hintT -= Time.deltaTime;
            if (hintT <= 0) { hintT = 6; Hud.Toast($"На воротах 67 замков! Спасено друзей: {saved} / 67. Ищи ещё!", UIKit.Pink); }
        }

        /// <summary>The glitch: the «67» cracks and falls off, the paper «8» is under it, the gate opens — 100%.</summary>
        public void Begin()
        {
            if (Started) return;
            Started = true;
            StartCoroutine(Sequence());
        }

        IEnumerator Sequence()
        {
            var p = PlayerCtl.I;
            for (int k = 0; k < 6; k++)
            {
                Effects.Emit("elec", transform.position + new Vector3(Random.Range(-3f, 3f), 13, -0.6f), 12, 0.6f);
                AudioFx.Play("zap", 0.7f); Hud.Glitch();
                six.localPosition += new Vector3(Random.Range(-0.1f, 0.1f), 0, 0); seven.localPosition += new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);
                yield return new WaitForSeconds(0.2f);
            }
            paper.gameObject.SetActive(true);
            AudioFx.Play("crate");
            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                six.localPosition += new Vector3(-1.5f, -9f * t, -0.5f) * Time.deltaTime; six.Rotate(0, 0, 200 * Time.deltaTime);
                seven.localPosition += new Vector3(1.5f, -9f * t, -0.5f) * Time.deltaTime; seven.Rotate(0, 0, -260 * Time.deltaTime);
                yield return null;
            }
            AudioFx.Play("bonk");
            six.gameObject.SetActive(false); seven.gameObject.SetActive(false);
            bool closed = false;
            var w = Window.Show("СИСТЕМНОЕ СООБЩЕНИЕ", win =>
            {
                win.Text("Критическая нехватка ключей! На складе было только 8 штук.", 32, UIKit.Gold);
                win.Text("Ну и ладно, 8 — это почти 67, если сильно прищуриться. Уровень пройден!", 30);
                win.Row("Прищуриться", null, "ОК", UIKit.Green, () => { closed = true; Window.Close(); });
            }, 960, 520);
            w.OnClose = () => closed = true;
            for (float t = 0; t < 20 && !closed && !Boot.Testing; t += Time.unscaledDeltaTime) yield return null;
            Window.Close();
            AudioFx.Play("error", 0.6f); AudioFx.Play("open");
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0, 105, t / 2f);
                doorL.localRotation = Quaternion.Euler(0, -a, 0); doorR.localRotation = Quaternion.Euler(0, a, 0);
                yield return null;
            }
            Opened = true;
            Dance();
            World67.Win();
            if (p != null) p.Frozen = UIRoot.Open != null;
        }

        /// <summary>The 8 rescued friends dance by the gate under glitchy fireworks of 6 and 7.</summary>
        void Dance()
        {
            for (int i = 0; i < 8; i++)
            {
                var m = ChubrikModel.Create("Dancer w7c" + i, transform, Rescue.LookOf("w7c" + i), Layers.World);
                m.transform.position = transform.position + new Vector3(-5.25f + i * 1.5f, 0, -5.5f);
                m.transform.rotation = Quaternion.Euler(0, 180, 0);
                var a = m.gameObject.AddComponent<ChubrikAnim>();
                a.model = m; a.emote = i % 8; a.Preview = true; a.PlayEmote();
                dancers.Add(m.gameObject);
            }
            StartCoroutine(Fireworks());
        }

        IEnumerator Fireworks()
        {
            for (int k = 0; k < 16; k++)
            {
                var c = transform.position + new Vector3(Random.Range(-8f, 8f), Random.Range(10f, 16f), Random.Range(-6f, 2f));
                AudioFx.Play("zap", 0.4f);
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI / 5;
                    var d = Prim.Text(null, i % 2 == 0 ? "6" : "7", c, 3, Hex(i % 3 == 0 ? "39FF6A" : i % 3 == 1 ? "FF3DF2" : "35E0FF"));
                    d.gameObject.AddComponent<Billboard>();
                    var pb = d.gameObject.AddComponent<PixelBit>(); pb.vel = new Vector3(Mathf.Cos(a) * 5, Mathf.Sin(a) * 5, 0); pb.gravity = 3; pb.life = 1.6f;
                }
                yield return new WaitForSeconds(0.4f);
            }
        }

        public int Dancers => dancers.Count;
    }

    /// <summary>
    /// Everything glitchy that happens over time in World 67: the sky blinks into a grid of 67, the giant 6 and 7
    /// swap places with a modem screech, a trail of 6–7 copies sometimes follows a jump, fake error windows pop up on
    /// the screen (a tap shatters them), the music stutters every 6–7 seconds.
    /// </summary>
    public class World67Fx : MonoBehaviour
    {
        public static Transform BigSix, BigSeven;
        public static World67Fx I;
        public int Trails { get; private set; }
        public int Popups { get; private set; }
        GameObject sky;
        float skyT, skyOff, swapT, stutterT, popupT, trailCd;
        bool wasGrounded = true; float lastVy;
        GameObject popup;

        void Awake()
        {
            I = this;
            sky = new GameObject("Sky67") { layer = Layers.World };
            sky.transform.SetParent(transform, false);
            var mesh = Object.Instantiate(Meshes.Ball(24, 16));
            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3) { var t = tris[i]; tris[i] = tris[i + 1]; tris[i + 1] = t; }   // seen from inside
            mesh.triangles = tris;
            sky.AddComponent<MeshFilter>().sharedMesh = mesh;
            var cv = new Canvas2(256, 128, Hex("140A2A"));
            for (int y = 0; y < 8; y++) for (int x = 0; x < 16; x++)
                {
                    var c = Hex((x + y) % 3 == 0 ? "39FF6A" : (x + y) % 3 == 1 ? "FF3DF2" : "35E0FF");
                    World67.PixelDigit(cv, x * 16 + 2, y * 16 + 3, 6, 2, c); World67.PixelDigit(cv, x * 16 + 9, y * 16 + 3, 7, 2, c);
                }
            var tex = cv.Tex(true, false, FilterMode.Point);
            var mat = Mats.UnlitTex(tex, Color.white);
            mat.mainTextureScale = new Vector2(4, 3);
            sky.AddComponent<MeshRenderer>().sharedMaterial = mat;
            sky.transform.localScale = Vector3.one * 600;
            sky.SetActive(false);
            skyT = Time.time + 8; swapT = Time.time + 10; stutterT = Time.time + 6.5f; popupT = Time.time + 9;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            bool here = Worlds.Current == World67.Id && p != null && !p.MenuOrbit;
            if (!here) { if (sky.activeSelf) sky.SetActive(false); if (popup) Destroy(popup); return; }
            float now = Time.time;
            // the sky blinks into a grid of 67 for a moment
            if (now > skyT) { sky.SetActive(true); sky.transform.position = p.transform.position; skyT = now + Random.Range(9f, 15f); skyOff = now + 0.3f; AudioFx.Play("zap", 0.3f); }
            if (sky.activeSelf && now > skyOff) sky.SetActive(false);
            // the giant digits swap places
            if (now > swapT && BigSix && BigSeven) { swapT = now + Random.Range(11f, 16f); StartCoroutine(Swap()); }
            // the music stutters every 6–7 seconds
            if (now > stutterT) { stutterT = now + Random.Range(6f, 7f); AudioFx.Stutter(); }
            // a fake error window on the screen
            if (now > popupT && popup == null && UIRoot.Open == null) { popupT = now + Random.Range(9f, 14f); ShowPopup(); }
            // a trail of 6–7 copies on some jumps
            trailCd -= Time.deltaTime;
            float vy = p.Velocity.y;
            bool jumped = (wasGrounded && !p.Grounded && vy > 3) || (!p.Grounded && vy - lastVy > 3);
            if (jumped && trailCd <= 0 && Random.value < 0.4f) { trailCd = 1.2f; StartCoroutine(Trail()); }
            wasGrounded = p.Grounded; lastVy = vy;
        }

        IEnumerator Swap()
        {
            AudioFx.Play("modem", 0.5f);
            Vector3 a = BigSix.position, b = BigSeven.position;
            for (float t = 0; t < 1.3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 1.3f);
                var lift = Vector3.up * Mathf.Sin(k * Mathf.PI) * 8;
                BigSix.position = Vector3.Lerp(a, b, k) + lift; BigSeven.position = Vector3.Lerp(b, a, k) - lift;
                yield return null;
            }
            BigSix.position = b; BigSeven.position = a;
        }

        /// <summary>6 or 7 colored copies of Chubrik left behind during a jump, gone in half a second.</summary>
        public IEnumerator Trail()
        {
            var p = PlayerCtl.I;
            if (p == null || p.Model == null) yield break;
            Trails++;
            int n = Random.value < 0.5f ? 6 : 7;
            string[] cols = { "FF3DF2", "39FF6A", "35E0FF", "FFC83A", "FF6B3D", "8A5CFF", "FFFFFF" };
            for (int i = 0; i < n; i++)
            {
                var g = Object.Instantiate(p.Model.gameObject, p.Model.transform.position, p.Model.transform.rotation);
                g.transform.localScale = p.Model.transform.lossyScale;
                foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
                foreach (var c in g.GetComponentsInChildren<Collider>(true)) Destroy(c);
                var m = Parts.Add(cols[i], 0.35f);
                foreach (var r in g.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = m;
                Destroy(g, 0.5f);
                yield return new WaitForSeconds(0.045f);
            }
        }

        /// <summary>An old-OS style error window somewhere on the screen; a tap breaks it into pixels.</summary>
        public void ShowPopup()
        {
            Popups++;
            string[] texts = { "Ошибка 67: слишком много чилла", "Загрузка 67%…", "Six-Seven detected", "Найдено 67 багов. Исправить? (нет)", "Внимание: Чубрик слишком крутой" };
            var root = UIRoot.HudLayer;
            var win = UIKit.Img(root, UIKit.White, Hex("D4D0C8"), "Popup67", true);
            popup = win.gameObject;
            var rt = win.rectTransform;
            UIKit.Place(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(Random.Range(-320f, 320f), -Random.Range(150f, 300f)), new Vector2(560, 170));
            var bar = UIKit.Img(win.transform, UIKit.White, Hex("0A246A"), "Bar");
            UIKit.Place(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(552, 40));
            var title = UIKit.Txt(bar.transform, "  Chubrik.exe", 24, Color.white, TextAlignmentOptions.MidlineLeft, false);
            UIKit.Fill(title.rectTransform);
            var x = UIKit.Img(bar.transform, UIKit.White, Hex("D4D0C8"), "X");
            UIKit.Place(x.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(34, 30));
            var xt = UIKit.Txt(x.transform, "x", 22, Hex("1A1A1A"), TextAlignmentOptions.Center, false); UIKit.Fill(xt.rectTransform);
            var t = UIKit.Txt(win.transform, texts[Random.Range(0, texts.Length)], 30, Hex("1A1A1A"), TextAlignmentOptions.Center, false);
            UIKit.Fill(t.rectTransform, 16, 8, 16, 48);
            var b = win.gameObject.AddComponent<UIButton>();
            b.Bg = win; b.OnClick = () => Shatter(win.rectTransform);
            AudioFx.Play("error", 0.6f);
            StartCoroutine(AutoClose(popup));
        }

        IEnumerator AutoClose(GameObject go)
        {
            yield return new WaitForSeconds(5f);
            if (go && go == popup) Shatter(go.GetComponent<RectTransform>());
        }

        /// <summary>The window breaks into pixels that fly apart and fade.</summary>
        public void Shatter(RectTransform rt)
        {
            if (rt == null) return;
            AudioFx.Play("crate", 0.7f);
            var parent = rt.parent;
            for (int i = 0; i < 36; i++)
            {
                var px = UIKit.Img(parent, UIKit.White, Hex(i % 3 == 0 ? "0A246A" : i % 3 == 1 ? "D4D0C8" : "39FF6A"), "Pixel");
                var p = px.rectTransform;
                p.anchorMin = p.anchorMax = rt.anchorMin; p.pivot = new Vector2(0.5f, 0.5f);
                p.anchoredPosition = rt.anchoredPosition + new Vector2(Random.Range(-260f, 260f), -85 + Random.Range(-70f, 70f));
                p.sizeDelta = Vector2.one * Random.Range(14f, 30f);
                px.gameObject.AddComponent<UiPixel>().vel = new Vector2(Random.Range(-400f, 400f), Random.Range(100f, 500f));
            }
            Destroy(rt.gameObject);
            if (popup == rt.gameObject) popup = null;
        }
    }

    /// <summary>A pixel of a shattered on-screen window.</summary>
    public class UiPixel : MonoBehaviour
    {
        public Vector2 vel; float t;
        Image img; RectTransform rt;
        void Start() { img = GetComponent<Image>(); rt = (RectTransform)transform; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            vel.y -= 1400 * Time.unscaledDeltaTime;
            rt.anchoredPosition += vel * Time.unscaledDeltaTime;
            rt.Rotate(0, 0, 400 * Time.unscaledDeltaTime);
            var c = img.color; c.a = Mathf.Clamp01(1 - t / 0.8f); img.color = c;
            if (t > 0.8f) Destroy(gameObject);
        }
    }
}
