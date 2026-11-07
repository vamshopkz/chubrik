using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 5 — «Джунгли»: green grass, tall tropical trees and palms, a blue river with floating logs that drift
    /// left and right, a waterfall with a hidden cave behind it, pink trampoline mushrooms, a poisonous purple swamp
    /// (falling in costs a heart and sends you back to the start), crumbling stone slabs in front of a big stepped
    /// pyramid, star tokens on trees and steps, 8 friends in cages, and the Ancient Stone Golem on the pyramid top
    /// (3 lives: jump on his head three times) — then a golden key and the victory portal appear.
    /// </summary>
    public static class World5
    {
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 4000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 2.5f);
        public static float FallY => O.y - 2.2f;
        public static readonly List<Mover> Logs = new List<Mover>();
        public static readonly List<FallingSlab> Slabs = new List<FallingSlab>();
        public static StoneGolem Boss;
        public static Vector3 TopC, TreeTop, CaveC;
        static Material grass, dirt, stone, stoneDark, moss, bark, leaf, leaf2, palmLeaf, pink, water, swamp;

        public static void Build()
        {
            Root = new GameObject("World5").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            Logs.Clear(); Slabs.Clear();
            grass = Lobby.Tiled(Tex.Grass(), 5); dirt = M("7A5A3A", 0.15f);
            stone = Mats.Tex(Tex.Sandstone(), Hex("B8B09A"), 0.2f); stoneDark = M("6E6A5A", 0.2f); moss = M("4E8A3A", 0.15f);
            bark = Mats.Tex(Tex.Bark(), Color.white, 0.1f); leaf = M("2E9E3E", 0.2f); leaf2 = M("4CBF4A", 0.2f); palmLeaf = M("3FAE3A", 0.3f);
            pink = M("FF6FC8", 0.45f);

            Swamp();
            // ---- start meadow ----
            Field(V(0, 0, 0), 11);
            Portal(V(-5, 0, -6));
            Lobby.Sign(V(-4, 0, 4.5f), 0, "МИР 5 · ДЖУНГЛИ", "4CE35A", 5.4f);
            Lobby.Sign(V(4.5f, 0, 4.5f), 0, "ВПЕРЕДИ — ПИРАМИДА!", "FF9A2E", 5.4f);
            Palm(V(-8, 0, 2), 0); Palm(V(7.5f, 0, -4), 1); Palm(V(-7, 0, -6), 2);
            // a giant tree with a tree-house: a pink mushroom throws you up (a friend waits up there)
            TreeTop = V(8, 9, 5);
            GiantTree(V(8, 0, 5), 9);
            Mushroom(V(4.5f, 0, 6.5f), 24f);
            Rescue.SpawnCage("w5c1", TreeTop + new Vector3(-1.2f, 0, 0.6f), -70, Root);   // the high tree
            Pickups.I.Token("w5t0", TreeTop + new Vector3(1.6f, 1, -1));
            Rescue.SpawnCage("w5c3", V(-3, 0, -3.5f), 30, Root);
            Pickups.I.Key("w5k0", V(-8, 4.6f, 2.6f));   // in the palm crown
            Lobby.Box(V(-6.6f, 0.6f, 3.4f), new Vector3(1.2f, 1.2f, 1.2f), stone, 15);
            Lobby.Box(V(-7.4f, 1.9f, 2.6f), new Vector3(1f, 1.4f, 1f), stone, -10);

            // ---- the river with floating logs, the waterfall upstream ----
            River(V(0, 0, 22), 36, 16);
            for (int k = 0; k < 3; k++) FloatLog(V(0, 0, 16.5f + k * 4.5f), k * 0.33f);
            Waterfall(V(-15, 0, 22));
            CaveC = V(-15.5f, 0, 22);
            Rescue.SpawnCage("w5c0", CaveC, 90, Root);                                     // behind the waterfall
            Pickups.I.Key("w5k1", CaveC + new Vector3(1.2f, 1, 2.2f));
            Pickups.I.Token("w5t1", CaveC + new Vector3(1.2f, 1, -2.2f));

            // ---- jungle bank ----
            Field(V(0, 0, 39), 10);
            Jungle(V(0, 0, 39), 9, 5);
            Rescue.SpawnCage("w5c4", V(5, 0, 42), -100, Root);
            Mushroom(V(-4, 0, 37), 20f);
            Lobby.Box(V(-7.5f, 6.4f, 33.5f), new Vector3(3, 0.6f, 3), Lobby.Tiled(Tex.Planks("8A5A36"), 1));   // a branch platform
            Pickups.I.Token("w5t2", V(-7.5f, 7.6f, 33.5f));
            Pickups.I.Key("w5k2", V(-7.5f, 7.6f, 35));
            Checkpoint(V(-2, 0, 32));

            // ---- over the poisonous swamp: stone pillars and pink mushrooms ----
            Pillar(V(2, 0.4f, 52), 2.2f);
            Pillar(V(-2, 1.2f, 57), 2f);
            Pillar(V(3, 2f, 62), 2.4f);
            Mushroom(V(3, 2f, 62), 17f, 1.5f);
            Pillar(V(-1, 6, 67.5f), 2.4f);                                                 // high one: the mushroom reaches it
            Rescue.SpawnCage("w5c5", V(-1.2f, 6, 67.2f), 0, Root);
            Pickups.I.Key("w5k3", V(-2, 2.6f, 57));
            Pickups.I.Token("w5t3", V(3, 6.5f, 62));
            Pillar(V(0, 1.6f, 70), 2.6f);
            Checkpoint(V(1.2f, 1.6f, 70.6f));

            // ---- crumbling stone slabs in front of the pyramid ----
            for (int k = 0; k < 6; k++)
            {
                var s = new GameObject("FallingSlab").AddComponent<FallingSlab>();
                s.Build(V((k % 2 == 0 ? -0.8f : 0.8f), 1.6f, 74 + k * 2.6f), Root);
                Slabs.Add(s);
            }
            Pickups.I.Key("w5k4", V(0.8f, 3.2f, 79.2f));

            // ---- the stepped pyramid ----
            Pyramid(V(0, 0, 100));
            Rescue.SpawnCage("w5c2", V(-8, 1.6f, 87.5f), 40, Root);                        // near the pyramid
            Rescue.SpawnCage("w5c6", V(9, 4.6f, 95), -90, Root);                           // on a pyramid ledge
            Pickups.I.Key("w5k5", V(-9.5f, 5.4f, 98));
            Pickups.I.Key("w5k6", V(9.5f, 8.4f, 104));
            Pickups.I.Key("w5k7", V(0, 3.2f, 87.5f));
            Rescue.SpawnCage("w5c7", V(8, 1.6f, 87.5f), -40, Root);

            Sky();
            Lobby.root = keep;
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- pieces ----------------

        static void Field(Vector3 c, float r)
        {
            Lobby.Disc(c, r, 2.4f, grass);
            Msh(Meshes.Cone(9), Root, c - Vector3.up * 2.4f, new Vector3(r * 2, r, r * 2), dirt, new Vector3(180, 0, 0));
            Lobby.Flowers(c, r * 0.8f, (int)(r * 3), (int)(c.z * 7));
            var rng = new Rng((int)(c.z * 3 + 7));
            for (int k = 0; k < (int)r; k++)
            {
                float a = rng.F(0, Mathf.PI * 2), d = rng.F(r * 0.6f, r - 0.6f);
                var p = c + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                for (int b = 0; b < 4; b++) Spike(Root, p, p + new Vector3(Mathf.Cos(b * 1.6f) * 0.5f, 0.9f, Mathf.Sin(b * 1.6f) * 0.5f), 0.25f, leaf2);   // ferns
            }
        }

        static void Pillar(Vector3 top, float r)
        {
            Lobby.Disc(top, r, top.y - (O.y - 6), stone);
            Cy(Root, top + Vector3.up * 0.02f, new Vector3(r * 2 - 0.4f, 0.02f, r * 2 - 0.4f), moss);
            for (int k = 0; k < 4; k++) Ca(Root, top + new Vector3(Mathf.Cos(k * 1.6f) * r, -1.2f - k * 0.4f, Mathf.Sin(k * 1.6f) * r), new Vector3(0.08f, 0.9f, 0.08f), leaf);   // hanging vines
        }

        static void Swamp()
        {
            var tex = SwampTex();
            swamp = Mats.Emissive(tex, Color.white, Hex("6A2A9A") * 0.5f, tex);
            swamp.mainTextureScale = new Vector2(16, 16);
            var p = Prim.Make(PrimitiveType.Plane, Root, new Vector3(O.x, O.y - 3, O.z + 50), new Vector3(16, 1, 16), swamp, "Swamp", Layers.World);
            p.AddComponent<WaterScroll>();
            new GameObject("SwampBubbles").AddComponent<SwampBubbles>().transform.SetParent(Root, false);
        }

        static Texture2D swampTex;
        static Texture2D SwampTex()
        {
            if (swampTex) return swampTex;
            var cv = new Canvas2(128, 128, Hex("5A2A86"));
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float n = Tex.Fbm(x / 24f, y / 24f, 5, 55);
                    cv.px[y * 128 + x] = Color.Lerp(Hex("3E1A66"), Hex("A04AD9"), n);
                }
            swampTex = cv.Tex();
            return swampTex;
        }

        static void River(Vector3 c, float width, float length)
        {
            water = Mats.Tex(Tex.Water(), new Color(0.6f, 0.85f, 1f), 0.95f);
            water.mainTextureScale = new Vector2(6, 3);
            var w = Prim.Make(PrimitiveType.Plane, Root, c + Vector3.down * 1.2f, new Vector3(width / 10f, 1, length / 10f), water, "River", Layers.World);
            w.AddComponent<WaterScroll>();
            // muddy banks
            foreach (var sd in new[] { -1, 1 }) B(Root, c + new Vector3(0, -1.6f, sd * (length / 2)), new Vector3(width, 1.2f, 0.6f), dirt);
            for (int k = 0; k < 12; k++) Ls(Root, c + new Vector3(-width / 2 + k * 3, -1f, (k % 2 == 0 ? 1 : -1) * (length / 2 - 0.2f)), new Vector3(1, 0.5f, 1), stoneDark);
        }

        static void FloatLog(Vector3 c, float phase)
        {
            var go = Lobby.Box(c - Vector3.up * 0.4f, new Vector3(5f, 0.8f, 1.6f), bark, 0);
            for (int k = 0; k < 2; k++) Cy(go.transform, new Vector3(k == 0 ? -0.5f : 0.5f, 0, 0), new Vector3(0.16f, 0.5f, 0.5f), M("C9A06A", 0.2f), new Vector3(0, 0, 90));
            Ls(go.transform, new Vector3(0.2f, 0.55f, 0.1f), new Vector3(0.2f, 0.25f, 0.4f), leaf2);
            var mv = go.AddComponent<Mover>();
            mv.a = c - Vector3.up * 0.4f + Vector3.left * 4; mv.b = c - Vector3.up * 0.4f + Vector3.right * 4; mv.period = 6; mv.phase = phase;
            Logs.Add(mv);
        }

        static void Waterfall(Vector3 c)
        {
            Lobby.Box(c + new Vector3(-3, 5, 0), new Vector3(3, 14, 18), stoneDark);   // the cliff
            // the falling water curtain (no collider: you walk through it into the cave)
            var fall = Mats.Transparent(new Color(0.6f, 0.85f, 1f, 0.55f));
            for (int k = 0; k < 9; k++) Ca(Root, c + new Vector3(0.6f, 4.5f, -4 + k), new Vector3(0.9f, 5f, 0.5f), fall);
            var foam = M("F4FBFF", 0.3f);
            for (int k = 0; k < 10; k++) Ls(Root, c + new Vector3(1.4f, -0.9f, -4.5f + k), new Vector3(1.2f, 0.5f, 1), foam);
            new GameObject("WaterfallMist").AddComponent<Mist>().transform.position = c + new Vector3(1.4f, -0.6f, 0);
            // a dry ledge behind the curtain: the hidden cave
            Lobby.Box(c + new Vector3(-0.6f, -0.4f, 0), new Vector3(2.8f, 0.8f, 7), stone);
            Lobby.Box(c + new Vector3(2.5f, -0.4f, -6.5f), new Vector3(4, 0.8f, 2), stone);   // a stone path from the start bank
            Lobby.Box(c + new Vector3(5.5f, -0.4f, -7.5f), new Vector3(3, 0.8f, 2), stone);
            for (int k = 0; k < 5; k++) Spike(Root, c + new Vector3(-1.4f, 0, -2.5f + k * 1.2f), c + new Vector3(-1.5f, 1.4f, -2.5f + k * 1.2f), 0.4f, G("6FD8FF", 0.9f));
        }

        static void Jungle(Vector3 c, float r, int seed)
        {
            var rng = new Rng(seed * 31);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4 + rng.F(-0.2f, 0.2f);
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - 1.2f);
                if (k % 2 == 0) TropicalTree(p, rng.F(6, 9), k); else Palm(p, k);
            }
        }

        static void TropicalTree(Vector3 p, float h, int seed)
        {
            Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * h / 2, new Vector3(0.6f, h / 2, 0.6f), bark);
            var rng = new Rng(seed * 7 + 3);
            for (int k = 0; k < 4; k++) S(Root, p + new Vector3(rng.F(-1.4f, 1.4f), h + rng.F(-0.6f, 0.8f), rng.F(-1.4f, 1.4f)), rng.F(2.6f, 3.6f), k % 2 == 0 ? leaf : leaf2);
            for (int k = 0; k < 3; k++) Ca(Root, p + new Vector3(rng.F(-1.5f, 1.5f), h - 1.6f, rng.F(-1.5f, 1.5f)), new Vector3(0.07f, 1.4f, 0.07f), leaf);
            for (int k = 0; k < 4; k++) { float a = k * 1.57f; Spike(Root, p + Vector3.up * 0.6f, p + new Vector3(Mathf.Cos(a) * 1.1f, 0, Mathf.Sin(a) * 1.1f), 0.35f, bark); }   // roots
        }

        static void GiantTree(Vector3 p, float h)
        {
            Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * h / 2, new Vector3(1.6f, h / 2, 1.6f), bark);
            var plank = Lobby.Tiled(Tex.Planks("A0642F"), 1);
            Lobby.Disc(p + Vector3.up * h, 3.2f, 0.4f, plank);
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Rod(Root, p + new Vector3(Mathf.Cos(a) * 3.1f, h, Mathf.Sin(a) * 3.1f), p + new Vector3(Mathf.Cos(a) * 3.1f, h + 0.9f, Mathf.Sin(a) * 3.1f), 0.1f, M("7A4A24")); }
            for (int k = 0; k < 5; k++) S(Root, p + new Vector3(Mathf.Cos(k * 1.25f) * 2, h + 3.4f + (k % 2) * 0.6f, Mathf.Sin(k * 1.25f) * 2), 4f, k % 2 == 0 ? leaf : leaf2);
            for (int k = 0; k < 5; k++) Ca(Root, p + new Vector3(Mathf.Cos(k * 1.3f) * 2.6f, h - 1.2f, Mathf.Sin(k * 1.3f) * 2.6f), new Vector3(0.08f, 1.6f, 0.08f), leaf);
        }

        static void Palm(Vector3 p, int seed)
        {
            var rng = new Rng(seed * 11 + 5);
            float lean = rng.F(-12, 12);
            var t = Node("Palm", Root, p, new Vector3(lean, rng.F(0, 360), 0));
            for (int k = 0; k < 7; k++) Cy(t, new Vector3(Mathf.Sin(k * 0.25f) * 0.4f, 0.35f + k * 0.65f, 0), new Vector3(0.42f - k * 0.025f, 0.34f, 0.42f - k * 0.025f), bark, new Vector3(0, 0, -k * 2f));
            var top = new Vector3(0.9f, 4.7f, 0);
            for (int k = 0; k < 7; k++)
            {
                float a = k * Mathf.PI * 2 / 7;
                var lf = B(t, top + new Vector3(Mathf.Cos(a) * 1.1f, -0.2f, Mathf.Sin(a) * 1.1f), new Vector3(0.7f, 0.06f, 2.4f), palmLeaf);
                lf.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), -0.35f, Mathf.Sin(a)));
            }
            for (int k = 0; k < 3; k++) S(t, top + new Vector3(Mathf.Cos(k * 2.1f) * 0.25f, -0.35f, Mathf.Sin(k * 2.1f) * 0.25f), 0.32f, M("6B4A2A", 0.3f));   // coconuts
        }

        /// <summary>A springy pink mushroom trampoline.</summary>
        static void Mushroom(Vector3 p, float power, float size = 1f)
        {
            var go = new GameObject("PinkMushroom") { layer = Layers.World };
            go.transform.SetParent(Root, false);
            go.transform.position = p;
            Cy(go.transform, new Vector3(0, 0.5f * size, 0), new Vector3(0.5f * size, 0.5f * size, 0.5f * size), M("FFF4E6", 0.3f));
            var top = Node("Cap", go.transform, new Vector3(0, 1.05f * size, 0));
            S(top, Vector3.zero, new Vector3(2.2f * size, 0.9f * size, 2.2f * size), pink);
            for (int k = 0; k < 7; k++) S(top, new Vector3(Mathf.Cos(k * 0.9f) * 0.7f * size, 0.32f * size, Mathf.Sin(k * 0.9f) * 0.7f * size), 0.28f * size, M("FFFFFF", 0.3f));
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f * size, 0); col.radius = 1.05f * size; col.height = 1f * size; col.direction = 0;
            var pad = go.AddComponent<BouncePad>();
            pad.power = power; pad.top = top;
        }

        static void Pyramid(Vector3 c)
        {
            float[] size = { 26, 21, 16, 12 };
            float y = 0;
            for (int t = 0; t < size.Length; t++)
            {
                float h = t == 0 ? 3f : 3f;
                Lobby.Box(c + Vector3.up * (y + h / 2), new Vector3(size[t], h, size[t]), stone);
                // carved band and moss on each tier
                B(Root, c + new Vector3(0, y + h - 0.25f, -size[t] / 2 - 0.02f), new Vector3(size[t] * 0.9f, 0.3f, 0.05f), stoneDark);
                for (int k = 0; k < 6; k++) S(Root, c + new Vector3(-size[t] / 2 + 1 + k * (size[t] - 2) / 5, y + h, -size[t] / 2 + 0.3f), new Vector3(1.4f, 0.4f, 0.8f), moss);
                y += h;
            }
            TopC = c + Vector3.up * y;
            // the big staircase up the front face
            for (int k = 0; k < 20; k++)
                Lobby.Box(c + new Vector3(0, 0.3f + k * 0.6f, -13.6f + k * 0.62f), new Vector3(4, 0.6f, 1.2f), stone);
            for (int k = 0; k < 10; k++) Pickups.I.Coin(c + new Vector3(0, 1.2f + k * 1.2f, -13.2f + k * 1.24f));
            // star tokens on the steps of the tiers
            Pickups.I.Token("w5t4", c + new Vector3(-9.5f, 4, -6));
            Pickups.I.Token("w5t5", c + new Vector3(7, 7, -7));
            Pickups.I.Token("w5t6", c + new Vector3(-5.5f, 10, 5));
            // torches at the top corners
            for (int k = 0; k < 4; k++)
            {
                var p = TopC + new Vector3(k < 2 ? -5.4f : 5.4f, 0, k % 2 == 0 ? -5.4f : 5.4f);
                Cy(Root, p + Vector3.up * 0.8f, new Vector3(0.3f, 0.8f, 0.3f), stoneDark);
                Cone(Root, p + Vector3.up * 1.6f, new Vector3(0.4f, 0.7f, 0.4f), Add("FF8A1E", 0.8f)).AddComponent<Pulse>();
            }
            Lobby.Sign(c + new Vector3(4.5f, 0, -15), 0, "ПИРАМИДА ДРЕВНЕГО ГОЛЕМА", "FF9A2E", 5);
            Boss = new GameObject("StoneGolem").AddComponent<StoneGolem>();
            Boss.Build(TopC, Root);
        }

        static void Checkpoint(Vector3 p) => new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);

        static void Portal(Vector3 c)
        {
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 2.4f, 2.4f, 0), new Vector3(1, 4.8f, 1.2f), stone);
            Lobby.Box(c + new Vector3(0, 5.2f, 0), new Vector3(6, 0.9f, 1.4f), stone);
            for (int k = 0; k < 6; k++) Ca(Root, c + new Vector3(-2.4f + k * 0.95f, 4.2f, -0.72f), new Vector3(0.08f, 0.8f, 0.08f), leaf);
            var surf = Cy(Root, c + new Vector3(0, 2.4f, 0), new Vector3(3.8f, 0.02f, 4.6f), Add("FFC83A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ", c + new Vector3(0, 5.2f, 0.75f), 5, Color.white);
            t.transform.rotation = Quaternion.Euler(0, 180, 0);
            Zones.Add(c + new Vector3(0, 0, 1.4f), 2.4f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1), () => Worlds.Current == 5);
        }

        static void Sky()
        {
            var r = new Rng(505);
            for (int i = 0; i < 30; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var p = new Vector3(O.x + side * r.F(20, 45), O.y - 3, O.z + r.F(-15, 130));
                if (r.Chance(0.5f)) TropicalTree(p, r.F(10, 16), i); else Palm(p, i);
            }
        }

        /// <summary>Victory portal after the Golem: finishes World 5.</summary>
        public static void VictoryPortal(Vector3 c)
        {
            Parts.Layer = Layers.World;
            var t = Node("VictoryPortal", Root, c);
            foreach (var sd in new[] { -1, 1 }) Cy(t, new Vector3(sd * 1.8f, 1.8f, 0), new Vector3(0.5f, 1.8f, 0.5f), M("FFC83A", 0.9f, 0.85f));
            Ring(t, new Vector3(0, 2.2f, 0), 1.9f, 0.18f, M("FFC83A", 0.9f, 0.85f), new Vector3(90, 0, 0));
            var surf = Cy(t, new Vector3(0, 2.2f, 0), new Vector3(3.6f, 0.02f, 3.6f), Add("FFE14D", 0.6f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var tx = Prim.Text(t, "ПОРТАЛ ПОБЕДЫ", new Vector3(0, 4.6f, 0), 3.4f, Hex("FFE14D"));
            tx.gameObject.AddComponent<Billboard>();
            Zones.Add(c, 2.6f, "ПОРТАЛ ПОБЕДЫ", Win, () => Worlds.Current == 5);
        }

        public static void Win()
        {
            bool first = !Save.P.done.Contains(5);
            if (first) { Save.P.done.Add(5); Save.P.coins += 900; Save.P.tokens += 3; }
            else Save.P.coins += 50;
            Save.Write();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            void Done() => Window.Show("МИР 5 ПРОЙДЕН!", w =>
            {
                w.Text(first ? "Древний Голем повержен! Награда: 900 монет и 3 жетона." : "Портал победы снова твой! +50 монет.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться в джунглях", "Спасти друзей и найти звёздочки", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
            if (!(first && Story.Show("w5end", Done))) Done();
        }
    }

    /// <summary>A stone slab before the pyramid: shakes when you step on it and falls 2 seconds later.</summary>
    public class FallingSlab : MonoBehaviour
    {
        Vector3 home; float t = -1, fallV, back; Collider col; Transform view;
        public bool Falling => t >= 2f || back > 0;

        public void Build(Vector3 top, Transform parent)
        {
            transform.SetParent(parent, false);
            home = top - Vector3.up * 0.3f;
            transform.position = home;
            Parts.Layer = Layers.World;
            view = Node("View", transform, Vector3.zero);
            B(view, Vector3.zero, new Vector3(2.4f, 0.6f, 2.4f), Mats.Tex(Tex.Sandstone(), Hex("B8B09A"), 0.2f));
            for (int k = 0; k < 4; k++) B(view, new Vector3(-0.9f + k * 0.6f, 0.31f, 0), new Vector3(0.05f, 0.02f, 2.2f), M("6E6A5A"));
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.4f, 0.6f, 2.4f);
            col = bc;
            gameObject.layer = Layers.World;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (back > 0)
            {
                if (Time.time > back) { back = 0; t = -1; transform.position = home; view.localPosition = Vector3.zero; col.enabled = true; view.gameObject.SetActive(true); }
                return;
            }
            if (t < 0 && p != null && Worlds.Current == 5 && p.Grounded)
            {
                var d = p.transform.position - transform.position;
                if (Mathf.Abs(d.x) < 1.3f && Mathf.Abs(d.z) < 1.3f && d.y > 0 && d.y < 0.7f) { t = 0; AudioFx.Play("crate", 0.4f); }
            }
            if (t < 0) return;
            t += Time.deltaTime;
            if (t < 2f) view.localPosition = new Vector3(Mathf.Sin(Time.time * 50) * 0.05f * t, 0, 0);   // shaking harder and harder
            else
            {
                fallV += 20 * Time.deltaTime;
                transform.position += Vector3.down * fallV * Time.deltaTime;
                if (transform.position.y < home.y - 1) col.enabled = false;
                if (transform.position.y < home.y - 12) { view.gameObject.SetActive(false); back = Time.time + 4; fallV = 0; }
            }
        }

        /// <summary>Tests: as if stepped on.</summary>
        public void TestStep() { if (t < 0) t = 0; }
    }

    /// <summary>
    /// Древний Каменный Голем on the pyramid top: 3 lives. He jumps in place and his landing sends a round shock wave
    /// over the floor (jump over it). Jump on his head three times — a golden key and the victory portal appear.
    /// </summary>
    public class StoneGolem : MonoBehaviour
    {
        Vector3 home; Transform body, armL, armR; Transform[] hearts = new Transform[3];
        enum St { Idle, Wait, Jump, Dizzy }
        St st; float t, vy, h, hitCd, bounceCd;
        public int Lives { get; private set; } = 3;
        public bool Defeated { get; private set; }
        const float ArenaR = 6f, Height = 3.2f, Radius = 1.2f;
        readonly List<(Transform t, float r)> waves = new List<(Transform, float)>();

        public void Build(Vector3 top, Transform parent)
        {
            home = top;
            transform.SetParent(parent, false);
            transform.position = top;
            transform.rotation = Quaternion.Euler(0, 180, 0);
            Parts.Layer = Layers.World;
            var stone = Mats.Tex(Tex.Sandstone(), Hex("9A927C"), 0.2f); var dark = M("4E4A3E", 0.2f); var moss = M("4E8A3A", 0.15f); var glow = G("4CE35A", 2f);
            body = Node("Body", transform, Vector3.zero);
            B(body, new Vector3(0, 1.6f, 0), new Vector3(2.2f, 1.9f, 1.6f), stone);
            B(body, new Vector3(0, 3f, 0.1f), new Vector3(1.4f, 1.1f, 1.2f), stone);
            foreach (var sd in new[] { -1, 1 })
            {
                B(body, new Vector3(sd * 0.32f, 3.1f, 0.71f), new Vector3(0.3f, 0.18f, 0.05f), glow);
                B(body, new Vector3(sd * 0.55f, 0.4f, 0), new Vector3(0.7f, 0.8f, 0.8f), dark);
                S(body, new Vector3(sd * 0.7f, 2.6f, -0.3f), new Vector3(0.6f, 0.3f, 0.6f), moss);
            }
            B(body, new Vector3(0, 2.7f, 0.71f), new Vector3(0.6f, 0.1f, 0.05f), dark);
            for (int k = 0; k < 5; k++) B(body, new Vector3(-0.6f + k * 0.3f, 1.8f, 0.81f), new Vector3(0.18f, 0.18f, 0.03f), k % 2 == 0 ? glow : dark);   // glyphs
            armL = Node("ArmL", body, new Vector3(-1.4f, 2.3f, 0)); armR = Node("ArmR", body, new Vector3(1.4f, 2.3f, 0));
            foreach (var a in new[] { armL, armR }) { B(a, new Vector3(0, -0.8f, 0), new Vector3(0.6f, 1.5f, 0.6f), stone); B(a, new Vector3(0, -1.7f, 0), new Vector3(0.8f, 0.6f, 0.8f), dark); }
            var lbl = Node("Label", transform, new Vector3(0, 4.6f, 0));
            lbl.gameObject.AddComponent<Billboard>();
            Prim.Text(lbl, "ДРЕВНИЙ КАМЕННЫЙ ГОЛЕМ", new Vector3(0, 0.5f, 0), 3f, Hex("FFE14D"));
            for (int k = 0; k < 3; k++)
            {
                hearts[k] = Msh(Meshes.Heart(), lbl, new Vector3(-0.7f + k * 0.7f, 0, 0), Vector3.one * 0.45f, G("FF5252", 1.6f)).transform;
            }
            if (Save.P.done.Contains(5)) { Defeated = true; Lives = 0; body.gameObject.SetActive(false); lbl.gameObject.SetActive(false); World5.VictoryPortal(home); }
        }

        bool PlayerIn(out PlayerCtl p)
        {
            p = PlayerCtl.I;
            if (p == null || Worlds.Current != 5 || p.Frozen) return false;
            var d = p.transform.position - home; d.y = 0;
            return d.magnitude < ArenaR + 0.5f && p.transform.position.y > home.y - 1 && p.transform.position.y < home.y + 8;
        }

        void Update()
        {
            UpdateWaves();
            if (Defeated) return;
            float dt = Time.deltaTime; t += dt;
            bool active = PlayerIn(out var p);
            switch (st)
            {
                case St.Idle:
                    if (active) { st = St.Wait; t = 0; AudioFx.Play("bonk"); Hud.Toast("ДРЕВНИЙ ГОЛЕМ! Прыгай через ударные волны и прыгай ему на голову — 3 раза", UIKit.Orange); }
                    break;
                case St.Wait:
                    if (!active) { st = St.Idle; break; }
                    if (p != null) { var to = p.transform.position - transform.position; to.y = 0; if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 3); }
                    body.localScale = new Vector3(1 + Mathf.Sin(t * 10) * 0.03f, 1 - Mathf.Sin(t * 10) * 0.05f, 1);
                    if (t > 1.6f) { st = St.Jump; t = 0; vy = 11; h = 0; body.localScale = Vector3.one; AudioFx.Play("jump7"); }
                    break;
                case St.Jump:
                    vy -= 25 * dt; h += vy * dt;
                    armL.localRotation = armR.localRotation = Quaternion.Euler(-150, 0, 0);
                    if (h <= 0)
                    {
                        h = 0; armL.localRotation = armR.localRotation = Quaternion.identity;
                        AudioFx.Play("bonk"); AudioFx.Play("crate");
                        Effects.Emit("dust", transform.position + Vector3.up * 0.3f, 40, 1f);
                        Parts.Layer = Layers.World;
                        var ring = Ring(transform.parent, transform.position + Vector3.up * 0.25f, 1f, 0.15f, Mats.Additive(new Color(1f, 0.85f, 0.4f, 0.9f)));
                        waves.Add((ring.transform, 1.2f));
                        st = St.Dizzy; t = 0;
                    }
                    break;
                case St.Dizzy:
                    if (Time.frameCount % 12 == 0) Effects.Emit("star", transform.position + Vector3.up * 3.8f, 2, 0.4f);
                    if (t > 1.6f) { st = St.Wait; t = 0; }
                    break;
            }
            body.localPosition = Vector3.up * h;
            if (!active || p == null) return;
            var d = p.transform.position - (transform.position + Vector3.up * h);
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz < Radius + 0.4f && d.y > Height * 0.75f && d.y < Height + 1.6f && p.Velocity.y < 1 && Time.time > bounceCd && st != St.Jump)
            {
                bounceCd = Time.time + 0.6f;
                p.Launch(13f);
                Hit();
            }
            else if (horiz < Radius + 0.4f && d.y > -0.5f && d.y < Height * 0.75f && Time.time > hitCd)
            {
                hitCd = Time.time + 1.2f;
                p.HurtBy(transform.position);
            }
        }

        void UpdateWaves()
        {
            var p = PlayerCtl.I;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var (ring, r) = waves[i];
                r += Time.deltaTime * 5.5f;
                waves[i] = (ring, r);
                ring.localScale = new Vector3(r, 1.5f, r);
                if (r > ArenaR) { Destroy(ring.gameObject); waves.RemoveAt(i); continue; }
                if (p != null && Worlds.Current == 5 && !p.Frozen)
                {
                    var d = p.transform.position - ring.position;
                    if (Mathf.Abs(new Vector2(d.x, d.z).magnitude - r) < 0.45f && d.y < 0.55f && d.y > -0.6f) p.HurtBy(ring.position);
                }
            }
        }

        void Hit()
        {
            if (Defeated) return;
            Lives--;
            if (Lives >= 0 && Lives < 3) hearts[Lives].gameObject.SetActive(false);
            AudioFx.Play("crate"); AudioFx.Play("boing");
            Effects.Emit("dust", transform.position + Vector3.up * 3, 30, 0.8f);
            Effects.Emit("star", transform.position + Vector3.up * 3.6f, 16, 0.5f);
            st = St.Wait; t = 0;
            if (Lives <= 0) Die();
            else Hud.Toast($"Бум! У Голема осталось жизней: {Lives}", UIKit.Lime);
        }

        /// <summary>Tests: a stomp on the head.</summary>
        public void TestHit() => Hit();

        void Die()
        {
            Defeated = true;
            AudioFx.Play("win3");
            Effects.Emit("dust", transform.position + Vector3.up * 2, 100, 1.4f, Vector3.up * 3);
            Effects.Emit("confetti", transform.position + Vector3.up * 4, 120, 0.8f, Vector3.up * 5);
            body.gameObject.SetActive(false);
            transform.Find("Label")?.gameObject.SetActive(false);
            Pickups.I.Key("w5kboss", home + new Vector3(0, 1.2f, -2.5f));
            World5.VictoryPortal(home + new Vector3(0, 0, 2.5f));
            Hud.Toast("Голем рассыпался! Бери золотой ключ и прыгай в портал победы", UIKit.Gold);
        }
    }

    /// <summary>Poison bubbles popping on the swamp around the player.</summary>
    public class SwampBubbles : MonoBehaviour
    {
        float acc;
        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 5) return;
            acc += Time.deltaTime * 6;
            int n = (int)acc; acc -= n;
            for (int i = 0; i < n; i++)
                Effects.Emit("bubble", new Vector3(p.transform.position.x + Random.Range(-14f, 14f), World5.O.y - 2.9f, p.transform.position.z + Random.Range(-8f, 18f)), 1, 0.2f, Vector3.up * 0.5f, new Color(0.8f, 0.5f, 1f, 0.8f));
        }
    }

    /// <summary>Mist at the foot of the waterfall.</summary>
    public class Mist : MonoBehaviour
    {
        float acc;
        void Update()
        {
            if (Worlds.Current != 5) return;
            acc += Time.deltaTime * 10;
            int n = (int)acc; acc -= n;
            if (n > 0) Effects.Emit("puff", transform.position + new Vector3(0, 0, Random.Range(-4f, 4f)), n, 0.4f, Vector3.up * 1.5f);
        }
    }
}
