using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Which world the chubrik is in (1 = lobby, 2 = floating ruins) and travelling between them.</summary>
    public static class Worlds
    {
        public static int Current = 1;

        public static void Enter(int w)
        {
            var p = PlayerCtl.I;
            if (w == 2 && World2.Root == null) World2.Build();
            Window.Close();
            AudioFx.Play("portal");
            p.Frozen = true;
            p.PlayVictory(() =>
            {
                Current = w;
                if (w == 2)
                {
                    p.WaterY = World2.O.y - 18f;
                    p.Checkpoint = World2.Spawn;
                    p.Teleport(World2.Spawn, 0);
                    p.Hearts = Save.P.maxHearts;
                    Hud.Toast("МИР 2 — ПАРЯЩИЕ РУИНЫ. Прыгай мобам на голову и дойди до кубка!", UIKit.Lime);
                }
                else
                {
                    p.WaterY = -0.9f;
                    p.Checkpoint = Lobby.Spawn;
                    p.Teleport(new Vector3(0, 0.3f, 30), 180);
                    p.Hearts = Save.P.maxHearts;
                    Hud.Toast("С возвращением в лобби!", UIKit.Gold);
                }
                p.SafePos = p.transform.position + Vector3.up * 0.2f;
                p.Frozen = UIRoot.Open != null;
            });
        }
    }

    /// <summary>
    /// World 2 — «Парящие руины», the easiest level: wide floating stone islands, wooden bridges with safety rails,
    /// giant stumps, broken columns and arches, crates with coins / purple tokens / a secret loot box, three kinds of
    /// slow enemies you beat with a jump on the head, checkpoint flags and a golden cup at the end.
    /// </summary>
    public static class World2
    {
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 1000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 2.5f);
        public static readonly List<Enemy> Enemies = new List<Enemy>();
        public static Vector3 GoalPos;
        static Material stone, stoneDark, grass, wood, plank, bark, moss;

        public static void Build()
        {
            Root = new GameObject("World2").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            stone = Lobby.Tiled(Tex.Sandstone(), 3); stoneDark = M("8C8577", 0.15f);
            grass = Lobby.Tiled(Tex.Grass(), 4); wood = Lobby.Tiled(Tex.Planks("A0642F"), 1); plank = M("B07A45", 0.25f);
            bark = Mats.Tex(Tex.Bark(), Color.white, 0.1f); moss = M("5E9E3A", 0.1f);

            // start island + portal home
            Isle(V(0, 0, 0), new Vector2(14, 14));
            Portal(V(0, 0, -6f));
            Lobby.Sign(V(-4.5f, 0, 3), 0, "МИР 2 · ПАРЯЩИЕ РУИНЫ", "6EDC3C", 5.4f);
            Lobby.Sign(V(4.5f, 0, 3), 0, "ПРЫГАЙ МОБАМ НА ГОЛОВУ!", "FF9A2E", 5.4f);
            Column(V(-5.5f, 0, -5.5f), 4, false); Column(V(5.5f, 0, -5.5f), 2.2f, true);
            Crate(V(4, 0.55f, -2)); Crate(V(5.2f, 0.55f, -2.3f));
            Line(V(0, 0.8f, 2), V(0, 0.8f, 6), 3);

            // bridge → island 1 (slime)
            Bridge(V(0, 0, 7), V(0, 0, 16));
            Line(V(0, 0.8f, 8), V(0, 0.8f, 15), 5);
            Isle(V(0, 0, 22), new Vector2(12, 12));
            Ruins(V(0, 0, 22));
            Enemy.Spawn(EnemyKind.Slime, V(0, 0, 22), 3.2f);
            Crate(V(3.5f, 0.55f, 25)); Crate(V(3.3f, 1.65f, 25)); Crate(V(4, 0.55f, 18), 3, "w2t0");
            CoinRing(V(0, 0.8f, 22), 4.6f, 10);

            // wide stepping stones up to the giant stump
            Isle(V(6, 1.2f, 32), new Vector2(6, 6), 1.2f);
            Isle(V(10, 2.4f, 40), new Vector2(6, 6), 1.2f);
            Coin(V(6, 2, 32)); Coin(V(10, 3.2f, 40));
            Stump(V(10, 3.6f, 50), 6.5f, 7);
            Enemy.Spawn(EnemyKind.Bee, V(10, 3.6f, 50), 3.4f);
            Crate(V(13, 4.15f, 52)); Crate(V(7, 4.15f, 53));
            CoinRing(V(10, 4.4f, 50), 2.2f, 8);
            Checkpoint(V(6.5f, 3.6f, 47));

            // bridge → ruins plaza
            Bridge(V(8, 3.6f, 56.2f), V(1, 3.6f, 62.5f));
            Line(V(7.3f, 4.4f, 56.9f), V(1.7f, 4.4f, 61.8f), 4);
            Isle(V(0, 3.6f, 70), new Vector2(18, 16));
            Plaza(V(0, 3.6f, 70));
            Enemy.Spawn(EnemyKind.Golem, V(-3.5f, 3.6f, 70), 2.8f);
            Enemy.Spawn(EnemyKind.Golem, V(4, 3.6f, 72), 2.6f, 0.5f);
            Crate(V(-7, 4.15f, 64)); Crate(V(-7, 4.15f, 65.2f)); Crate(V(-5.8f, 4.15f, 64.5f));
            GoldCrate(V(7, 4.15f, 76), "w2box0");
            CoinRing(V(0, 4.4f, 70), 6, 14);

            // side path: round island with a bounce mushroom up to a secret grove (purple token)
            Bridge(V(-9, 3.6f, 70), V(-15, 3.6f, 72.5f));
            RoundIsle(V(-20, 3.6f, 75), 5f);
            Lobby.Mushroom(V(-20, 3.6f, 75), 18f);
            Isle(V(-20, 10.2f, 86), new Vector2(7, 7));
            Lobby.Tree(V(-22, 10.2f, 88), 0.8f, 33);
            Crate(V(-18.5f, 10.75f, 86), 5, "w2t1");
            for (int k = 0; k < 5; k++) Coin(V(-20, 6 + k * 1.2f, 77 + k * 1.6f));
            Bridge(V(-17, 10.2f, 89.6f), V(-6.5f, 5.4f, 94));   // gentle way back down to the main path

            // main path: ramp bridge up → second stump → stones → temple
            Bridge(V(0, 3.6f, 78.2f), V(0, 5.4f, 89.3f));
            Line(V(0, 4.6f, 79), V(0, 6.2f, 87), 5);
            Isle(V(0, 5.4f, 95), new Vector2(14, 12));
            Ruins(V(0, 5.4f, 95));
            Enemy.Spawn(EnemyKind.Slime, V(-2, 5.4f, 95), 2.6f);
            Enemy.Spawn(EnemyKind.Bee, V(3, 5.4f, 96), 2.4f, 0.4f);
            Crate(V(5.5f, 5.95f, 91)); Crate(V(-3, 5.95f, 100), 3, "w2t2");
            Checkpoint(V(-1.8f, 5.4f, 90.6f));

            Bridge(V(4.5f, 5.4f, 101), V(10, 6.6f, 107));
            Stump(V(12, 6.6f, 112), 5.5f, 9);
            Enemy.Spawn(EnemyKind.Slime, V(12, 6.6f, 112), 2.6f);
            CoinRing(V(12, 7.4f, 112), 2.6f, 8);
            Isle(V(6, 6.6f, 120.5f), new Vector2(6, 6), 1.2f);
            Coin(V(6, 7.4f, 120.5f));
            Bridge(V(4, 6.6f, 123.6f), V(1.5f, 6.6f, 127.5f));

            // the temple with the golden cup
            Isle(V(0, 6.6f, 137), new Vector2(20, 18));
            Temple(V(0, 6.6f, 137));
            Enemy.Spawn(EnemyKind.Golem, V(-5, 6.6f, 134), 2.4f);
            Enemy.Spawn(EnemyKind.Bee, V(5, 6.6f, 134), 2.4f, 0.3f);
            Crate(V(-8, 7.15f, 131)); Crate(V(8, 7.15f, 131)); Crate(V(-8, 7.15f, 132.2f)); Crate(V(8, 7.15f, 140), 3, "w2t3");
            Checkpoint(V(-6, 6.6f, 130));
            Line(V(0, 7.4f, 129), V(0, 7.4f, 138), 7);
            GoalPos = V(0, 8.3f, 142.5f);
            Goal(GoalPos);

            Sky();
            Lobby.root = keep;
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- pieces ----------------

        /// <summary>Floating stone island: top at c.y, grass on top, rocky cone underneath, little ruin blocks on the corners.</summary>
        static void Isle(Vector3 c, Vector2 size, float thick = 1.6f)
        {
            Lobby.Box(c - Vector3.up * thick / 2, new Vector3(size.x, thick, size.y), stone);
            B(Root, c + Vector3.up * 0.03f, new Vector3(size.x - 0.6f, 0.06f, size.y - 0.6f), grass);
            var r = new Rng((int)(c.x * 13 + c.z * 7));
            Msh(Meshes.Cone(7), Root, c - Vector3.up * thick, new Vector3(size.x * 1.05f, Mathf.Max(size.x, size.y) * 0.45f, size.y * 1.05f), stoneDark, new Vector3(180, r.F(0, 60), 0));
            for (int k = 0; k < 4; k++)
            {
                var corner = new Vector3((k % 2 == 0 ? -1 : 1) * (size.x / 2 - 0.5f), 0, (k < 2 ? -1 : 1) * (size.y / 2 - 0.5f));
                if (r.Chance(0.6f)) Lobby.Box(c + corner + Vector3.up * 0.3f, new Vector3(0.8f, 0.6f, 0.8f), stone, r.F(0, 40));
                // hanging vines
                Ca(Root, c + corner * 1.02f - Vector3.up * (thick + 0.6f), new Vector3(0.06f, 0.7f, 0.06f), moss);
            }
            Lobby.Flowers(c, Mathf.Min(size.x, size.y) * 0.4f, (int)(size.x * size.y / 8), r.I(1000));
        }

        static void RoundIsle(Vector3 c, float r)
        {
            Lobby.Disc(c, r, 1.6f, stone);
            Cy(Root, c + Vector3.up * 0.03f, new Vector3(r * 2 - 0.5f, 0.03f, r * 2 - 0.5f), grass);
            Msh(Meshes.Cone(8), Root, c - Vector3.up * 1.6f, new Vector3(r * 2.1f, r * 1.2f, r * 2.1f), stoneDark, new Vector3(180, 0, 0));
        }

        /// <summary>Giant tree stump: bark side, rings on top, roots around.</summary>
        static void Stump(Vector3 top, float r, int seed)
        {
            float h = 5f;
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), bark);
            Cy(Root, top + Vector3.up * 0.02f, new Vector3(r * 2 - 0.3f, 0.02f, r * 2 - 0.3f), Mats.Tex(RingsTex(), Color.white, 0.2f));
            var rng = new Rng(seed);
            for (int k = 0; k < 7; k++)
            {
                float a = k * Mathf.PI * 2 / 7 + rng.F(-0.2f, 0.2f);
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Spike(Root, top - Vector3.up * (h - 0.8f) + d * (r - 0.3f), top - Vector3.up * (h + 0.4f) + d * (r + 1.6f), 1.1f, bark);
            }
            Msh(Meshes.Cone(7), Root, top - Vector3.up * h, new Vector3(r * 2.2f, r * 1.1f, r * 2.2f), stoneDark, new Vector3(180, 0, 0));
            // little mushrooms on the side
            for (int k = 0; k < 3; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                var p = top + new Vector3(Mathf.Cos(a) * (r + 0.1f), -rng.F(1, 3.5f), Mathf.Sin(a) * (r + 0.1f));
                S(Root, p, new Vector3(0.8f, 0.3f, 0.8f), M("FF8A3D", 0.4f));
            }
        }

        static Texture2D ringsTex;
        static Texture2D RingsTex()
        {
            if (ringsTex) return ringsTex;
            var cv = new Canvas2(256, 256, Hex("D9A76A"));
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float d = Mathf.Sqrt((x - 128f) * (x - 128f) + (y - 128f) * (y - 128f));
                    float w = Mathf.Sin(d * 0.55f + Tex.Fbm(x / 30f, y / 30f, 8, 3) * 3);
                    if (w > 0.7f) cv.px[y * 256 + x] = Hex("B07A45");
                    if (d > 122) cv.px[y * 256 + x] = Hex("6D4425");
                }
            ringsTex = cv.Tex(false);
            return ringsTex;
        }

        /// <summary>Wooden bridge from a to b (top surfaces): planks, posts, ropes and invisible safety rails.</summary>
        static void Bridge(Vector3 a, Vector3 b, float w = 3.2f)
        {
            var d = b - a;
            float len = d.magnitude + 1.2f;
            var rot = Quaternion.LookRotation(d.normalized);
            var mid = (a + b) / 2;
            var go = new GameObject("Bridge") { layer = Layers.World };
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation(mid, rot);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.15f, 0); col.size = new Vector3(w, 0.3f, len);
            foreach (var sd in new[] { -1, 1 })
            {
                var rail = go.AddComponent<BoxCollider>();
                rail.center = new Vector3(sd * (w / 2 + 0.1f), 0.7f, 0); rail.size = new Vector3(0.2f, 1.4f, len - 0.6f);
            }
            int n = Mathf.CeilToInt(len / 0.62f);
            for (int i = 0; i < n; i++)
            {
                float z = -len / 2 + (i + 0.5f) * len / n;
                B(go.transform, new Vector3(0, -0.08f, z), new Vector3(w * (i % 3 == 0 ? 1f : 0.96f), 0.14f, len / n - 0.07f), i % 2 == 0 ? plank : wood, new Vector3(0, (i % 4 - 1.5f) * 1.2f, 0));
            }
            foreach (var sd in new[] { -1, 1 })
            {
                B(go.transform, new Vector3(sd * (w / 2 - 0.1f), -0.2f, 0), new Vector3(0.18f, 0.18f, len), M("7A4A24"));
                int posts = Mathf.Max(2, Mathf.CeilToInt(len / 2.6f));
                Vector3 prev = default;
                for (int i = 0; i <= posts; i++)
                {
                    float z = -len / 2 + 0.3f + i * (len - 0.6f) / posts;
                    var p = new Vector3(sd * (w / 2 + 0.05f), 0.55f, z);
                    Cy(go.transform, p, new Vector3(0.14f, 0.6f, 0.14f), M("7A4A24", 0.2f));
                    var top = p + Vector3.up * 0.5f;
                    if (i > 0) Rod(go.transform, prev, top, 0.05f, M("D9C08F", 0.1f));
                    prev = top;
                }
            }
        }

        static void Column(Vector3 b, float h, bool broken)
        {
            Lobby.Box(b + Vector3.up * 0.2f, new Vector3(1.3f, 0.4f, 1.3f), stone);
            Lobby.Solid(PrimitiveType.Cylinder, b + Vector3.up * (0.4f + h / 2), new Vector3(0.9f, h / 2, 0.9f), stone);
            if (!broken) Lobby.Box(b + Vector3.up * (h + 0.6f), new Vector3(1.3f, 0.4f, 1.3f), stone);
            else
            {
                Cone(Root, b + Vector3.up * (h + 0.4f), new Vector3(0.9f, 0.5f, 0.9f), stone, new Vector3(0, 0, 25));
                Lobby.Solid(PrimitiveType.Cylinder, b + new Vector3(1.6f, 0.45f, 0.6f), new Vector3(0.9f, 0.9f, 0.9f), stone, new Vector3(0, 30, 90));
            }
            for (int k = 0; k < 3; k++) Ca(Root, b + new Vector3(0.46f, 0.6f + k * h / 3, 0.1f * k), new Vector3(0.12f, 0.35f, 0.12f), moss, new Vector3(0, 0, 10));
        }

        static void Arch(Vector3 c, float yaw)
        {
            var t = Node("Arch", Root, c, new Vector3(0, yaw, 0));
            foreach (var sd in new[] { -1, 1 }) Lobby.Solid(PrimitiveType.Cube, new Vector3(sd * 2.2f, 2.2f, 0), new Vector3(1, 4.4f, 1), stone, default, t);
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 4.8f, 0), new Vector3(5.6f, 0.9f, 1.1f), stone, default, t);
            B(t, new Vector3(0, 5.35f, 0), new Vector3(1, 0.4f, 1.15f), M("C9B99A", 0.2f));
            for (int k = 0; k < 4; k++) Ca(t, new Vector3(-2f + k * 1.3f, 4.1f, 0.55f), new Vector3(0.08f, 0.4f, 0.08f), moss);
        }

        static void Ruins(Vector3 c)
        {
            Column(c + new Vector3(-4.5f, 0, -3.5f), 3.2f, true);
            Column(c + new Vector3(4.5f, 0, 3.5f), 2.4f, true);
            Lobby.Box(c + new Vector3(-4.8f, 0.5f, 3.5f), new Vector3(2.2f, 1, 0.8f), stone, 12);
        }

        static void Plaza(Vector3 c)
        {
            Arch(c + new Vector3(0, 0, -7.2f), 0);
            Arch(c + new Vector3(0, 0, 7.2f), 0);
            for (int k = 0; k < 4; k++) Column(c + new Vector3(k < 2 ? -7.5f : 7.5f, 0, k % 2 == 0 ? -3.5f : 3.5f), 3.5f, k == 1 || k == 2);
            Cy(Root, c + Vector3.up * 0.05f, new Vector3(5, 0.04f, 5), M("C9B99A", 0.2f));
            var rune = Msh(Meshes.Star(8, 0.55f), Root, c + Vector3.up * 0.1f, new Vector3(3, 3, 0.05f), G("7DFF9A", 0.8f), new Vector3(90, 0, 0));
            rune.AddComponent<Spin>().axis = Vector3.forward;
        }

        static void Temple(Vector3 c)
        {
            Lobby.Box(c + new Vector3(0, 0.3f, 5.5f), new Vector3(9, 0.6f, 6), stone);
            Lobby.Box(c + new Vector3(0, 0.75f, 5.8f), new Vector3(7, 0.3f, 4.6f), M("C9B99A", 0.2f));
            for (int k = 0; k < 6; k++) Column(c + new Vector3(-6.5f + k * 2.6f, 0, 9), 4.5f, k == 0 || k == 4);
            Lobby.Box(c + new Vector3(0, 5.8f, 9), new Vector3(16, 0.8f, 1.6f), stone);
            Cone(Root, c + new Vector3(0, 6.2f, 9), new Vector3(16, 2.2f, 1.6f), M("C9B99A", 0.2f));
            Arch(c + new Vector3(0, 0, -8.2f), 0);
        }

        static void Portal(Vector3 c)
        {
            var stoneM = M("9AA3AE", 0.2f);
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 2.4f, 2.4f, 0), new Vector3(1, 4.8f, 1.2f), stoneM);
            Lobby.Box(c + new Vector3(0, 5.2f, 0), new Vector3(6, 0.9f, 1.4f), stoneM);
            var surf = Cy(Root, c + new Vector3(0, 2.4f, 0), new Vector3(3.8f, 0.02f, 4.6f), Add("FFC83A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ", c + new Vector3(0, 5.2f, 0.75f), 5, Color.white);
            t.transform.rotation = Quaternion.Euler(0, 180, 0);
            Zones.Add(c + new Vector3(0, 0, 1.4f), 2.4f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1));
        }

        static void Checkpoint(Vector3 p) => new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);

        static void Goal(Vector3 p)
        {
            Lobby.Disc(p, 1.3f, 0.8f, M("C9B99A", 0.2f));
            var g = new GameObject("Goal").AddComponent<GoalCup>();
            g.transform.SetParent(Root, false);
            g.transform.position = p;
            g.Build();
        }

        static void Crate(Vector3 p, int coins = 3, string token = null)
        {
            Parts.Layer = Layers.World;
            var crate = Lobby.Tiled(Tex.Planks(token != null ? "8E5BD9" : "C08040"), 1);
            var b = Lobby.Box(p, Vector3.one * 1.1f, crate, (p.x * 37 + p.z * 11) % 20 - 10);
            B(b.transform, Vector3.zero, new Vector3(1.04f, 0.15f, 1.04f), M(token != null ? "4A2A8C" : "6D4425"));
            B(b.transform, Vector3.zero, new Vector3(0.15f, 1.04f, 1.04f), M(token != null ? "4A2A8C" : "6D4425"));
            if (token != null) Msh(Meshes.Star(), b.transform, new Vector3(0, 0, 0.53f), new Vector3(0.45f, 0.45f, 0.05f), G("B57CFF", 1.5f));
            var br = b.AddComponent<Breakable>();
            br.coins = coins; br.token = token;
        }

        static void GoldCrate(Vector3 p, string id)
        {
            var b = Lobby.Box(p, Vector3.one * 1.1f, M("FFC83A", 0.85f, 0.7f), 15);
            B(b.transform, Vector3.zero, new Vector3(1.05f, 0.18f, 1.05f), M("E53935", 0.4f));
            B(b.transform, Vector3.zero, new Vector3(0.18f, 1.05f, 1.05f), M("E53935", 0.4f));
            var q = Prim.Text(b.transform, "?", new Vector3(0, 0.1f, -0.56f), 6, Color.white);
            q.transform.localRotation = Quaternion.identity;
            S(b.transform, Vector3.zero, 1.6f, Add("FFE14D", 0.15f)).AddComponent<Pulse>();
            var br = b.AddComponent<Breakable>();
            br.coins = 10; br.lootbox = id;
        }

        static void Coin(Vector3 p) => Pickups.I.Coin(p);
        static void Line(Vector3 a, Vector3 b, int n) { for (int i = 0; i < n; i++) Coin(Vector3.Lerp(a, b, n == 1 ? 0 : (float)i / (n - 1))); }
        static void CoinRing(Vector3 c, float r, int n) { for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; Coin(c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); } }

        /// <summary>Sea of clouds below and distant floating rocks around.</summary>
        static void Sky()
        {
            var r = new Rng(77);
            var white = M("FFFFFF", 0.05f);
            for (int i = 0; i < 26; i++)
            {
                var c = O + new Vector3(r.F(-60, 60), -16 - r.F(0, 4), r.F(-20, 160));
                var cl = Node("Cloud", Root, c);
                for (int k = 0; k < 4; k++) S(cl, new Vector3((k - 1.5f) * 4, r.F(-1, 1), r.F(-2, 2)), r.F(5, 9), white);
            }
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var c = O + new Vector3(side * r.F(30, 55), r.F(-6, 14), r.F(0, 150));
                float s = r.F(2, 5);
                Cy(Root, c, new Vector3(s * 2, 0.5f, s * 2), grass);
                Msh(Meshes.Cone(6), Root, c - Vector3.up * 0.5f, new Vector3(s * 2, s * 1.6f, s * 2), stoneDark, new Vector3(180, 0, 0));
                if (r.Chance(0.6f)) Lobby.Tree(c + Vector3.up * 0.5f, s * 0.3f, i);
            }
        }
    }

    public enum EnemyKind { Golem, Bee, Slime }

    /// <summary>
    /// Easy enemy: walks slowly in a circle. Jump on its head to beat it (you bounce up and get coins);
    /// touching it from the side costs a heart. Weapons beat it too. Comes back after a while.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyKind kind;
        Vector3 center; float radius, angle, speed = 0.32f;
        Transform body, armL, armR, key, wingL, wingR;
        public bool Dead { get; private set; }
        float deadT, back, hitCd;
        public float Height => kind == EnemyKind.Slime ? 1.0f : kind == EnemyKind.Bee ? 1.6f : 1.5f;
        float R => kind == EnemyKind.Slime ? 0.7f : 0.6f;

        public static Enemy Spawn(EnemyKind k, Vector3 center, float radius, float phase = 0)
        {
            var go = new GameObject("Enemy " + k) { layer = Layers.World };
            go.transform.SetParent(World2.Root, false);
            var e = go.AddComponent<Enemy>();
            e.kind = k; e.center = center; e.radius = radius; e.angle = phase * Mathf.PI * 2;
            e.Build();
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.center = new Vector3(0, e.Height * 0.5f, 0); col.radius = 0.7f;
            World2.Enemies.Add(e);
            e.Place();
            return e;
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            body = Node("Body", transform, Vector3.zero);
            var black = M("141414", 0.9f);
            void Eyes(Transform p, float x, float y, float z, float s, Material glow = null)
            {
                foreach (var sd in new[] { -1, 1 })
                {
                    S(p, new Vector3(sd * x, y, z), s, glow ?? M("FFFFFF", 0.5f));
                    if (glow == null) S(p, new Vector3(sd * x, y - s * 0.1f, z + s * 0.35f), s * 0.5f, black);
                }
            }
            switch (kind)
            {
                case EnemyKind.Golem:   // Деревянный Робо-Голем
                {
                    var w = Lobby.Tiled(Tex.Planks("A0642F"), 1);
                    var metal = M("8C96A3", 0.7f, 0.6f);
                    B(body, new Vector3(0, 0.75f, 0), new Vector3(0.9f, 0.8f, 0.7f), w);
                    B(body, new Vector3(0, 1.3f, 0), new Vector3(0.6f, 0.45f, 0.55f), w);
                    B(body, new Vector3(0, 0.75f, 0.36f), new Vector3(0.5f, 0.4f, 0.04f), metal);
                    Eyes(body, 0.13f, 1.33f, 0.28f, 0.11f, G("FFB300", 2.5f));
                    B(body, new Vector3(0, 1.2f, 0.28f), new Vector3(0.3f, 0.05f, 0.02f), black);
                    Rod(body, new Vector3(0, 1.52f, 0), new Vector3(0, 1.75f, 0), 0.04f, metal);
                    S(body, new Vector3(0, 1.78f, 0), 0.1f, G("FF3B3B", 2f)).AddComponent<Pulse>();
                    armL = Node("ArmL", body, new Vector3(-0.55f, 1.0f, 0)); armR = Node("ArmR", body, new Vector3(0.55f, 1.0f, 0));
                    foreach (var a in new[] { armL, armR }) { B(a, new Vector3(0, -0.25f, 0), new Vector3(0.2f, 0.5f, 0.2f), w); S(a, new Vector3(0, -0.52f, 0), 0.22f, metal); }
                    foreach (var sd in new[] { -1, 1 }) B(body, new Vector3(sd * 0.22f, 0.18f, 0), new Vector3(0.25f, 0.36f, 0.3f), w);
                    break;
                }
                case EnemyKind.Bee:     // Заводной Пчелоид
                {
                    var stripes = Mats.Tex(Patterns.Stripes("FFC400", "1A1A1A", 6), Color.white, 0.5f);
                    S(body, new Vector3(0, 0.9f, 0), new Vector3(0.75f, 0.7f, 0.9f), stripes, new Vector3(90, 0, 0));
                    S(body, new Vector3(0, 1.0f, 0.45f), 0.5f, M("FFC400", 0.5f));
                    Eyes(body, 0.12f, 1.07f, 0.64f, 0.16f);
                    Spike(body, new Vector3(0, 0.88f, -0.4f), new Vector3(0, 0.82f, -0.7f), 0.14f, black);
                    foreach (var sd in new[] { -1, 1 }) Rod(body, new Vector3(sd * 0.08f, 1.2f, 0.55f), new Vector3(sd * 0.18f, 1.45f, 0.7f), 0.025f, black);
                    key = Node("Key", body, new Vector3(0, 1.3f, -0.05f));
                    var keyM = M("C9D1DB", 0.85f, 0.8f);
                    Cy(key, new Vector3(0, 0.08f, 0), new Vector3(0.06f, 0.08f, 0.06f), keyM);
                    foreach (var sd in new[] { -1, 1 }) Ring(key, new Vector3(sd * 0.13f, 0.2f, 0), 0.1f, 0.03f, keyM, new Vector3(0, 0, 90));
                    wingL = Node("WingL", body, new Vector3(-0.3f, 1.2f, 0)); wingR = Node("WingR", body, new Vector3(0.3f, 1.2f, 0));
                    foreach (var (wg, sd) in new[] { (wingL, -1), (wingR, 1) }) S(wg, new Vector3(sd * 0.3f, 0.1f, 0), new Vector3(0.55f, 0.08f, 0.35f), Glass("E8F6FF", 0.55f));
                    break;
                }
                default:                 // Медленный Каменный Слайм
                {
                    var rock = M("8F949B", 0.35f);
                    S(body, new Vector3(0, 0.45f, 0), new Vector3(1.3f, 0.9f, 1.2f), rock);
                    for (int k = 0; k < 5; k++) S(body, new Vector3(Mathf.Sin(k * 2.3f) * 0.4f, 0.6f + (k % 2) * 0.2f, Mathf.Cos(k * 1.7f) * 0.35f), 0.22f, M("6C7177", 0.3f));
                    S(body, new Vector3(0, 0.95f, -0.1f), new Vector3(0.4f, 0.2f, 0.4f), M("5E9E3A", 0.1f));
                    Eyes(body, 0.18f, 0.6f, 0.52f, 0.2f);
                    B(body, new Vector3(0, 0.35f, 0.58f), new Vector3(0.3f, 0.05f, 0.03f), black);
                    break;
                }
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        void Place()
        {
            var p = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            transform.position = p;
            var tangent = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        void Update()
        {
            float t = Time.time;
            if (Dead)
            {
                deadT += Time.deltaTime;
                body.localScale = new Vector3(1 + deadT * 2, Mathf.Max(0.05f, 1 - deadT * 5), 1 + deadT * 2);
                if (deadT > 0.35f && body.gameObject.activeSelf) body.gameObject.SetActive(false);
                if (Time.time > back) { Dead = false; deadT = 0; body.gameObject.SetActive(true); body.localScale = Vector3.one; Effects.Emit("smoke", transform.position + Vector3.up * 0.5f, 10, 0.5f); }
                return;
            }
            angle += speed / Mathf.Max(1, radius) * Time.deltaTime;
            Place();
            switch (kind)
            {
                case EnemyKind.Golem:
                    body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(t * 3)) * 0.06f;
                    armL.localRotation = Quaternion.Euler(Mathf.Sin(t * 3) * 30, 0, 0);
                    armR.localRotation = Quaternion.Euler(-Mathf.Sin(t * 3) * 30, 0, 0);
                    body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3) * 4);
                    break;
                case EnemyKind.Bee:
                    body.localPosition = Vector3.up * (0.1f + Mathf.Sin(t * 2.5f) * 0.12f);
                    key.localRotation = Quaternion.Euler(0, t * 120, 0);
                    wingL.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 40) * 30);
                    wingR.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(t * 40) * 30);
                    break;
                default:
                    float s = Mathf.Sin(t * 2.2f);
                    body.localScale = new Vector3(1 - s * 0.08f, 1 + s * 0.12f, 1 - s * 0.08f);
                    break;
            }

            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 2 || p.Frozen) return;
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            float pr = 0.38f * p.Model.SizeScale;
            if (horiz < R + pr + 0.25f && d.y > Height * 0.45f && d.y < Height + 0.8f && p.Velocity.y < 1f)
            {
                Stomp(p);
            }
            else if (horiz < R + pr && d.y > -0.6f && d.y < Height * 0.45f && Time.time > hitCd)
            {
                hitCd = Time.time + 1f;
                p.HurtBy(transform.position);
            }
        }

        void Stomp(PlayerCtl p)
        {
            p.Launch(12f);
            AudioFx.Play("boing");
            Die();
        }

        public void Die()
        {
            if (Dead) return;
            Dead = true; deadT = 0; back = Time.time + 45f;
            AudioFx.Play("bonk");
            var c = transform.position + Vector3.up * Height * 0.6f;
            Effects.Emit("star", c, 16, 0.4f);
            Effects.Emit(kind == EnemyKind.Golem ? "dust" : kind == EnemyKind.Bee ? "stardust" : "smoke", c, 14, 0.5f);
            Effects.Emit("coin", c, 8, 0.3f, Vector3.up * 2);
            Save.AddCoins(5);
            Effects.Float("+5", c + Vector3.up, UIKit.Gold);
        }
    }

    /// <summary>Checkpoint flag: touch it and you come back here when you run out of hearts.</summary>
    public class Checkpoint : MonoBehaviour
    {
        Transform cloth;
        bool on;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            Lobby.Disc(p + Vector3.up * 0.1f, 0.6f, 0.2f, M("9AA3AE", 0.2f));
            Rod(transform, new Vector3(0, 0, 0), new Vector3(0, 3, 0), 0.08f, M("FFFFFF", 0.4f));
            cloth = Node("Cloth", transform, new Vector3(0, 2.2f, 0));
            B(cloth, new Vector3(0.5f, 0.3f, 0), new Vector3(1f, 0.6f, 0.04f), M("E53935", 0.3f));
        }

        void Update()
        {
            cloth.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 3) * 10, 0);
            var p = PlayerCtl.I;
            if (on || p == null) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 2 && new Vector2(d.x, d.z).magnitude < 1.6f)
            {
                on = true;
                p.Checkpoint = transform.position + new Vector3(1.2f, 0.3f, 0);
                p.Hearts = Save.P.maxHearts;
                cloth.localPosition = new Vector3(0, 2.6f, 0);
                cloth.GetComponentInChildren<Renderer>().sharedMaterial = M("6EDC3C", 0.3f);
                AudioFx.Play("token");
                Effects.Emit("confetti", transform.position + Vector3.up * 3, 40, 0.3f, Vector3.up * 3);
                Hud.Toast("Флажок! Сердечки восстановлены", UIKit.Lime);
            }
        }
    }

    /// <summary>The golden cup at the end of World 2.</summary>
    public class GoalCup : MonoBehaviour
    {
        Transform cup;
        float cd;

        public void Build()
        {
            Parts.Layer = Layers.World;
            cup = Node("Cup", transform, Vector3.zero);
            var gold = M("FFC83A", 0.95f, 0.9f);
            Cy(cup, new Vector3(0, 0.05f, 0), new Vector3(0.7f, 0.05f, 0.7f), gold);
            Cy(cup, new Vector3(0, 0.35f, 0), new Vector3(0.18f, 0.3f, 0.18f), gold);
            S(cup, new Vector3(0, 1.0f, 0), new Vector3(0.9f, 0.8f, 0.9f), gold);
            Cy(cup, new Vector3(0, 1.35f, 0), new Vector3(0.95f, 0.05f, 0.95f), gold);
            foreach (var sd in new[] { -1, 1 }) Ring(cup, new Vector3(sd * 0.52f, 1.05f, 0), 0.2f, 0.05f, gold, new Vector3(90, 0, 0));
            Msh(Meshes.Star(), cup, new Vector3(0, 1.0f, 0.46f), new Vector3(0.35f, 0.35f, 0.05f), G("FFFFFF", 1.2f));
            S(cup, new Vector3(0, 0.9f, 0), 2.6f, Add("FFE14D", 0.15f)).AddComponent<Pulse>();
            cup.gameObject.AddComponent<Spin>().speed = 50;
            cup.gameObject.AddComponent<Bob>().amp = 0.15f;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Time.time < cd) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 2.2f && new Vector2(d.x, d.z).magnitude < 1.8f)
            {
                cd = Time.time + 6f;
                bool first = !Save.P.done.Contains(2);
                if (first) { Save.P.done.Add(2); Save.P.coins += 300; Save.P.tokens += 1; }
                else Save.P.coins += 50;
                Save.Write();
                AudioFx.Play("token");
                Effects.Emit("confetti", transform.position + Vector3.up * 2, 160, 0.5f, Vector3.up * 5);
                p.PlayVictory();
                Window.Show("МИР 2 ПРОЙДЕН!", w =>
                {
                    w.Text(first ? "Ура! Ты дошёл до золотого кубка. Награда: 300 монет и 1 жетон!" : "Кубок снова твой! +50 монет.", 32, UIKit.Gold);
                    w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                    w.Row("Остаться в мире", "Собрать монеты и найти жетоны", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
                }, 960, 520);
            }
        }
    }
}
