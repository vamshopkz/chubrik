using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 18.11: World 2 «Парящие руины» dressed like its concept art: sunny sky, big grassy floating islands with rocky
    /// undersides and hanging roots, broken arches and columns, round ruined towers, waterfalls off the edges,
    /// twisted trees, treasure chests and a castle with a glowing golden gate and an orange flag.
    /// Plus a new playable side area west of island 1: rope bridge → tower island → stairs → chest island
    /// (+ a little waterfall island) → stairs → castle island. 4 more friends (w2c8..w2c11) and their keys.
    /// </summary>
    public static class World2Scenery
    {
        static Transform Root;
        static Material stone, stoneDark, stoneLight, cobble, grass, bark, wood, plank, rope, moss, rootM;
        static Material leafA, leafB, leafC, water, waterCore, foam, mist, gold, goldGlow, flag, dark, cloud, sun, chestWood;

        static Vector3 V(float x, float y, float z) => World2.O + new Vector3(x, y, z);

        public static void Build()
        {
            if (World2.Root == null) return;
            Root = World2.Root;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;

            // materials: created once
            stone = Lobby.Tiled(Tex.Sandstone(), 2); stoneDark = M("6E665A", 0.1f); stoneLight = M("C9BFA8", 0.15f);
            cobble = M("A9A291", 0.12f); grass = Lobby.Tiled(Tex.Grass(), 3); bark = Mats.Tex(Tex.Bark(), Color.white, 0.1f);
            wood = Lobby.Tiled(Tex.Planks("8A5A2E"), 1); plank = M("A8743F", 0.25f); rope = M("D9C08F", 0.1f);
            moss = M("5E9E3A", 0.1f); rootM = M("6B4A2B", 0.1f);
            leafA = M("5DAA3A", 0.15f); leafB = M("7CBF45", 0.15f); leafC = M("3F8A33", 0.15f);
            water = Glass("8FD8FF", 0.65f); waterCore = Add("E8F8FF", 0.35f); foam = M("F2FBFF", 0.3f); mist = Add("FFFFFF", 0.18f);
            gold = M("FFC83A", 0.85f, 0.7f); goldGlow = G("FFD54A", 2.2f); flag = M("FF8A1E", 0.3f); dark = M("3A3530", 0.05f);
            cloud = M("FFFFFF", 0.05f); sun = Add("FFF3C4", 0.07f); chestWood = Lobby.Tiled(Tex.Planks("B0702F"), 1);

            SideArea();
            RouteDressing();
            Backdrop();

            Lobby.root = keep;
        }

        // ================= the new playable side area (west of island 1) =================

        static void SideArea()
        {
            // rope bridge from island 1 (west edge x=-6) to the tower island A
            RopeBridge(V(-6, 0, 20.5f), V(-12, 0, 20.5f));
            for (int i = 0; i < 4; i++) Pickups.I.Coin(V(-6.8f - i * 1.5f, 0.8f, 20.5f));

            // A: tower island (top y=0), x -22..-12, z 15..25
            Isle(V(-17, 0, 20), 10, 10, 1.8f, true, 81);
            CobblePath(V(-12.5f, 0, 20.5f), V(-19.5f, 0, 24.5f), 2.2f);
            Tower(V(-19.8f, 0, 16.9f), 1.5f, 6.5f, true, true);
            Arch(V(-14.6f, 0, 20.5f), 90, 1f, true, true);
            Rescue.SpawnCage("w2c8", V(-16.8f, 0, 23.4f), -150, Root);
            Pickups.I.Key("w2k9", V(-16.6f, 1.1f, 17.4f));
            Stream(V(-13.2f, 0, 17.2f), V(-13.2f, 0, 15.1f), 1.0f);
            Waterfall(V(-13.2f, 0, 15.0f), Vector3.back, 1.1f, 16f);
            TwistedTree(V(-21f, 0, 21f), 0.75f, true, 3);
            Bushes(V(-12.9f, 0, 16.2f), 2, 11);
            Coins(V(-17.5f, 0.8f, 20.5f), 1.6f, 5);

            // stone stairs A → B (3 steps of 0.4 m)
            for (int k = 1; k <= 3; k++) Step(V(-20, 0.4f * k, 25.1f + (k - 0.5f) * 1.12f), 3, 1.12f, 0.4f * k + 1f);
            Pickups.I.Coin(V(-20, 1.5f, 26.8f));

            // B: chest island (top y=1.6), x -24.5..-15.5, z 28.5..37.5
            Isle(V(-20, 1.6f, 33), 9, 9, 1.8f, true, 82);
            CobblePath(V(-20, 1.6f, 29), V(-20, 1.6f, 37.2f), 2.4f);
            Rescue.SpawnCage("w2c9", V(-23f, 1.6f, 35.6f), 90, Root);
            Pickups.I.Key("w2k10", V(-16.8f, 2.7f, 35.6f));
            Chest(V(-17f, 1.6f, 30.3f), -30);
            TwistedTree(V(-23.2f, 1.6f, 30.2f), 0.7f, true, 5);
            Column(V(-16.2f, 1.6f, 33.2f), 2.6f, true, true);
            Coins(V(-20, 2.4f, 33), 1.8f, 6);

            // D: little waterfall island west of B (gap 3 m, +1 m), top y=2.6, x -32.5..-27.5
            Isle(V(-30, 2.6f, 33), 5, 5, 1.6f, true, 83);
            Pickups.I.Key("w2k8", V(-30.4f, 3.7f, 33.4f));
            Pickups.I.Coin(V(-26, 3.6f, 33));
            Pond(V(-31.2f, 2.6f, 31.6f), 0.9f);
            Waterfall(V(-32.6f, 2.6f, 31.6f), Vector3.left, 1.0f, 15f);
            Column(V(-31.4f, 2.6f, 34.7f), 1.6f, true, true);

            // stone stairs B → C (6 steps of 0.4 m, then the castle island 0.4 m higher)
            for (int k = 1; k <= 6; k++) Step(V(-20, 1.6f + 0.4f * k, 37.5f + (k - 0.5f) * 1.08f), 3, 1.08f, 0.4f * k + 2.2f);
            for (int k = 0; k < 4; k++) Pickups.I.Coin(V(-20, 2.9f + k * 0.6f, 38.6f + k * 1.5f));
            foreach (var sd in new[] { -1, 1 }) Column(V(-20 + sd * 2.1f, 1.6f, 37.0f), 1.4f, false, false);

            // C: castle island (top y=4.4), x -26..-14, z 44..56
            Isle(V(-20, 4.4f, 50), 12, 12, 2.2f, true, 84);
            CobblePath(V(-20, 4.4f, 44.3f), V(-20, 4.4f, 53.4f), 2.6f);
            Castle(V(-20, 4.4f, 50));
            Rescue.SpawnCage("w2c10", V(-24, 4.4f, 46.4f), 40, Root);
            Rescue.SpawnCage("w2c11", V(-16.2f, 4.4f, 46.0f), -40, Root);
            Pickups.I.Key("w2k11", V(-20, 9.5f, 54.6f));   // on the castle wall above the golden gate
            Chest(V(-23.6f, 4.4f, 52.0f), 60);
            Waterfall(V(-14.0f, 4.4f, 48.6f), Vector3.right, 1.2f, 20f);
            Stream(V(-15.4f, 4.4f, 48.6f), V(-14.1f, 4.4f, 48.6f), 1.1f);
            Coins(V(-20, 5.2f, 49), 2.2f, 7);
        }

        /// <summary>Castle wall at the back of island C with a glowing golden gate, two round towers and an orange flag.
        /// Three stone blocks lead up onto the wall (key on top).</summary>
        static void Castle(Vector3 c)
        {
            float z = 4.6f, th = 1.6f, h = 4f;
            Lobby.Box(c + new Vector3(-3.5f, h / 2, z), new Vector3(3f, h, th), stone);       // x -5..-2
            Lobby.Box(c + new Vector3(3.5f, h / 2, z), new Vector3(3f, h, th), stone);        // x 2..5
            Lobby.Box(c + new Vector3(0, 3.5f, z), new Vector3(4f, 1f, th), stone);           // lintel over the gate
            // the golden glowing gate (closed)
            Lobby.Box(c + new Vector3(0, 1.5f, z + 0.3f), new Vector3(3.6f, 3f, 0.3f), goldGlow);
            B(Root, c + new Vector3(0, 1.5f, z - 0.05f), new Vector3(3.9f, 3.2f, 0.5f), Add("FFE27A", 0.2f)).AddComponent<Pulse>().amount = 0.03f;
            for (int k = -1; k <= 1; k++) B(Root, c + new Vector3(k * 1.2f, 1.5f, z + 0.1f), new Vector3(0.12f, 3f, 0.1f), gold);
            S(Root, c + new Vector3(0, 3.5f, z - th / 2 - 0.05f), new Vector3(0.7f, 0.7f, 0.15f), goldGlow);   // keystone gem
            // crenellations on the wall (decor, low)
            for (int k = 0; k < 6; k++)
            {
                float x = -4.6f + k * 1.84f;
                if (k == 2 || k == 3) continue;   // keep the top above the gate free for the key
                B(Root, c + new Vector3(x, h + 0.3f, z + 0.5f), new Vector3(0.7f, 0.6f, 0.5f), stone);
            }
            // round towers on both ends (solid)
            Tower(c + new Vector3(-4.9f, 0, z), 1.3f, 7.5f, true, false);
            Tower(c + new Vector3(4.9f, 0, z), 1.3f, 8.5f, true, false);
            var top = c + new Vector3(4.9f, 8.5f + 0.5f, z);
            Rod(Root, top, top + Vector3.up * 3f, 0.08f, dark);
            B(Root, top + new Vector3(0.75f, 2.5f, 0), new Vector3(1.5f, 0.85f, 0.05f), flag, new Vector3(0, -10, 0));
            Spike(Root, top + new Vector3(1.5f, 2.5f, -0.1f), top + new Vector3(2.1f, 2.5f, -0.2f), 0.85f, flag);
            // steps up onto the wall (1 m each)
            for (int k = 1; k <= 3; k++) Lobby.Box(c + new Vector3(2.9f, k * 0.5f, z - th / 2 - 0.15f - (4 - k) * 1.4f + 0.7f), new Vector3(1.6f, k, 1.4f), stoneLight);
            // ruined side wall (low, broken)
            Lobby.Box(c + new Vector3(-5.6f, 0.9f, 0.4f), new Vector3(0.8f, 1.8f, 3.4f), stone, 2);
            B(Root, c + new Vector3(-5.5f, 0.25f, -2.2f), new Vector3(0.9f, 0.5f, 1.1f), stone, new Vector3(0, 20, 8));
            // ivy on the wall
            for (int k = 0; k < 5; k++) if (k != 2) Ca(Root, c + new Vector3(-4.8f + k * 2.3f, 2.4f + (k % 2) * 0.6f, z - th / 2 - 0.04f), new Vector3(0.12f, 0.8f, 0.12f), moss);
        }

        // ================= dressing on the existing route (decor only, no colliders) =================

        static void RouteDressing()
        {
            // waterfalls pouring off the edges of the old islands
            Waterfall(V(-7.05f, 0, -2.5f), Vector3.left, 1.2f, 17f);       // start island, west edge
            Waterfall(V(9.05f, 3.6f, 74f), Vector3.right, 1.4f, 19f);       // ruins plaza, east edge
            Waterfall(V(7.05f, 5.4f, 92.5f), Vector3.right, 1.1f, 18f);     // island before the stump
            Waterfall(V(-10.05f, 6.6f, 141.5f), Vector3.left, 1.5f, 20f);   // temple island, west edge

            // hanging roots under the old islands
            HangRoots(V(0, -1.6f, 0), 7, 7, 101);
            HangRoots(V(0, -1.6f, 22), 6, 6, 102);
            HangRoots(V(0, 2f, 70), 9, 8, 103);
            HangRoots(V(0, 3.8f, 95), 7, 6, 104);
            HangRoots(V(0, 5f, 137), 10, 9, 105);
        }

        // ================= distant set dressing =================

        static void Backdrop()
        {
            // far ahead: the big castle on its own island (golden gate, orange flag)
            var cc = V(0, 26, 192);
            RoundIsle(cc, 18, 4, 201, false);
            BigCastle(cc);
            Waterfall(cc + new Vector3(-17.9f, 0, -4), Vector3.left, 2.4f, 34f);
            Waterfall(cc + new Vector3(17.9f, 0, 3), Vector3.right, 2.0f, 30f);
            TwistedTree(cc + new Vector3(-12, 0, -10), 1.6f, false, 21);
            TwistedTree(cc + new Vector3(13, 0, -9), 1.3f, false, 22);
            // a huge stone stair from below up to the castle island
            for (int k = 0; k < 8; k++) B(Root, cc + new Vector3(0, -3.2f - k * 1.1f, -18.5f - k * 1.6f), new Vector3(6, 1.1f, 1.7f), stoneLight);
            RoundIsle(cc + new Vector3(0, -12f, -32), 4, 2f, 202, false);

            // west: big island with a round ruined tower and arches
            var w = V(-74, 6, 70);
            RoundIsle(w, 16, 4, 203, false);
            Tower(w + new Vector3(-4, 0, 2), 3.2f, 14f, false, true);
            Arch(w + new Vector3(6, 0, -5), 20, 1.6f, false, false);
            Arch(w + new Vector3(8, 0, 6), 70, 1.4f, false, true);
            TwistedTree(w + new Vector3(4, 0, 9), 1.5f, false, 23);
            Waterfall(w + new Vector3(15.9f, 0, -2), Vector3.right, 2f, 30f);
            CobblePath(w + new Vector3(-1, 0, -12), w + new Vector3(4, 0, 1), 3f);

            // east: big island with a colonnade, broken arches and a waterfall
            var e = V(72, 9, 64);
            RoundIsle(e, 15, 4, 204, false);
            for (int k = 0; k < 5; k++) Column(e + new Vector3(-6 + k * 3, 0, 4), 6f, k == 1 || k == 4, false);
            Arch(e + new Vector3(-2, 0, -6), 0, 1.7f, false, true);
            TwistedTree(e + new Vector3(8, 0, -6), 1.4f, false, 24);
            Waterfall(e + new Vector3(-14.9f, 0, -3), Vector3.left, 2.2f, 32f);
            Chest(e + new Vector3(2, 0, -2), 30, false);

            // smaller islands with ruins all around
            var r = new Rng(2077);
            Vector3[] spots =
            {
                V(-36, 16, -32), V(34, 4, -36), V(-62, -3, 128), V(78, 15, 148), V(-48, 18, 18), V(56, 20, 108),
                V(-28, -6, 168), V(40, 12, 184), V(-90, 14, 30), V(96, 4, 22),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                float rad = r.F(4.5f, 8f);
                RoundIsle(spots[i], rad, 2.2f, 300 + i, false);
                int kind = i % 4;
                if (kind == 0) TwistedTree(spots[i] + new Vector3(r.F(-1, 1), 0, r.F(-1, 1)), rad * 0.18f, false, 40 + i);
                else if (kind == 1) Arch(spots[i], r.F(0, 180), rad * 0.22f, false, r.Chance(0.5f));
                else if (kind == 2) Tower(spots[i] + new Vector3(r.F(-1, 1), 0, r.F(-1, 1)), rad * 0.3f, rad * 1.1f, false, true);
                else { Column(spots[i] + new Vector3(-1.5f, 0, 0), rad * 0.5f, false, false); Column(spots[i] + new Vector3(1.5f, 0, 0.5f), rad * 0.35f, true, false); }
                if (i % 3 == 0) Waterfall(spots[i] + new Vector3(rad - 0.1f, 0, 0), Vector3.right, rad * 0.18f, rad * 3f);
            }

            // tiny rocks drifting in the air
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var p = V(side * r.F(42, 66), r.F(-4, 24), r.F(-20, 180));
                var t = Node("SkyRock", Root, p);
                float s = r.F(0.8f, 2.2f);
                Cy(t, Vector3.zero, new Vector3(s * 2, 0.25f, s * 2), grass);
                Msh(Meshes.Cone(6), t, -Vector3.up * 0.2f, new Vector3(s * 2.1f, s * 1.8f, s * 2.1f), stoneDark, new Vector3(180, r.F(0, 60), 0));
                if (r.Chance(0.4f)) Ls(t, new Vector3(s * 0.3f, 0.4f, 0), s * 0.8f, leafA);
                var b = t.gameObject.AddComponent<Bob>(); b.amp = 0.4f; b.speed = r.F(0.4f, 0.9f);
            }

            // puffy clouds at island height (the art's sky), plus sun rays from the upper left
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var p = V(side * r.F(56, 100), r.F(-8, 30), r.F(-40, 220));
                if (i >= 13) p = V(r.F(-50, 50), r.F(10, 40), r.F(240, 280));   // a few far ahead
                var t = Node("Cloud", Root, p);
                float s = r.F(4, 8);
                for (int k = 0; k < 5; k++)
                    Ls(t, new Vector3((k - 2) * s * 0.55f, (k == 2 ? 0.5f : 0) * s + r.F(-0.2f, 0.3f) * s, r.F(-0.4f, 0.4f) * s), s * (k == 2 ? 1.5f : r.F(0.8f, 1.2f)), cloud);
            }
            for (int i = 0; i < 4; i++)
            {
                var a = V(-130 + i * 14, 120, 40 + i * 25);
                var b = V(-60 + i * 8, -10, 60 + i * 25);
                var ray = B(Root, (a + b) / 2, new Vector3(6 + i * 2, (b - a).magnitude, 0.2f), sun);
                ray.transform.rotation = Quaternion.LookRotation(Vector3.Cross(b - a, Vector3.up).normalized, (b - a).normalized);
            }
        }

        static void BigCastle(Vector3 c)
        {
            float z = 6f;
            // front walls with the big golden gate
            B(Root, c + new Vector3(-6.5f, 4, z - 6), new Vector3(7, 8, 2), stone);
            B(Root, c + new Vector3(6.5f, 4, z - 6), new Vector3(7, 8, 2), stone);
            B(Root, c + new Vector3(0, 7, z - 6), new Vector3(6, 2, 2), stone);
            B(Root, c + new Vector3(0, 3f, z - 6.6f), new Vector3(5.4f, 6f, 0.4f), goldGlow);
            B(Root, c + new Vector3(0, 3f, z - 7.1f), new Vector3(6.2f, 6.6f, 0.6f), Add("FFE27A", 0.22f)).AddComponent<Pulse>().amount = 0.03f;
            for (int k = 0; k < 8; k++) B(Root, c + new Vector3(-9 + k * 2.57f, 8.6f, z - 6), new Vector3(1.1f, 1.2f, 2.1f), stone);
            // keep behind
            B(Root, c + new Vector3(0, 7, z + 1), new Vector3(10, 14, 8), stoneLight);
            for (int k = 0; k < 5; k++) B(Root, c + new Vector3(-4 + k * 2, 14.6f, z - 3.1f), new Vector3(1.1f, 1.2f, 1.2f), stoneLight);
            for (int k = 0; k < 3; k++) B(Root, c + new Vector3(-3 + k * 3, 9.5f, z - 3.05f), new Vector3(0.9f, 1.8f, 0.2f), dark);
            // four round towers
            Tower(c + new Vector3(-10, 0, z - 6), 2.6f, 13f, false, false);
            Tower(c + new Vector3(10, 0, z - 6), 2.6f, 11f, false, false);
            Tower(c + new Vector3(-7, 0, z + 5), 2.2f, 17f, false, true);
            Tower(c + new Vector3(8, 0, z + 5), 2.4f, 19f, false, false);
            // orange flag on the highest tower
            var top = c + new Vector3(8, 19.6f, z + 5);
            Rod(Root, top, top + Vector3.up * 6, 0.2f, dark);
            B(Root, top + new Vector3(1.8f, 5f, 0), new Vector3(3.6f, 2f, 0.1f), flag, new Vector3(0, -12, 0));
            Spike(Root, top + new Vector3(3.6f, 5f, -0.4f), top + new Vector3(5f, 5f, -0.7f), 2f, flag);
        }

        // ================= pieces =================

        /// <summary>Grassy floating island (rectangular): stone slab (solid when walkable), grass top, rocky underside, roots.</summary>
        static void Isle(Vector3 c, float sx, float sz, float thick, bool solid, int seed)
        {
            var r = new Rng(seed);
            var size = new Vector3(sx, thick, sz);
            if (solid) Lobby.Box(c - Vector3.up * thick / 2, size, stone);
            else B(Root, c - Vector3.up * thick / 2, size, stone);
            B(Root, c + Vector3.up * 0.03f, new Vector3(sx - 0.3f, 0.06f, sz - 0.3f), grass);
            // grassy lip hanging over the edges
            B(Root, c - Vector3.up * 0.25f, new Vector3(sx + 0.2f, 0.45f, sz + 0.2f), moss);
            float big = Mathf.Max(sx, sz);
            Msh(Meshes.Cone(7), Root, c - Vector3.up * thick, new Vector3(sx * 1.08f, big * 0.75f, sz * 1.08f), stoneDark, new Vector3(180, r.F(0, 60), 0));
            for (int k = 0; k < 2; k++)
                Msh(Meshes.Cone(6), Root, c - Vector3.up * thick + new Vector3(r.F(-0.3f, 0.3f) * sx, 0, r.F(-0.3f, 0.3f) * sz),
                    new Vector3(sx * 0.4f, big * r.F(0.6f, 1f), sz * 0.4f), stone, new Vector3(180, r.F(0, 60), 0));
            HangRoots(c - Vector3.up * thick, sx / 2, sz / 2, seed);
        }

        /// <summary>Round floating island for the backdrop (no colliders).</summary>
        static void RoundIsle(Vector3 c, float rad, float thick, int seed, bool solid)
        {
            var r = new Rng(seed);
            if (solid) Lobby.Disc(c, rad, thick, stone);
            else Cy(Root, c - Vector3.up * thick / 2, new Vector3(rad * 2, thick / 2, rad * 2), stone);
            Cy(Root, c + Vector3.up * 0.03f, new Vector3(rad * 2 + 0.3f, 0.12f, rad * 2 + 0.3f), grass);
            Msh(Meshes.Cone(8), Root, c - Vector3.up * thick, new Vector3(rad * 2.1f, rad * 1.5f, rad * 2.1f), stoneDark, new Vector3(180, r.F(0, 60), 0));
            for (int k = 0; k < 2; k++)
            {
                float a = r.F(0, Mathf.PI * 2);
                Msh(Meshes.Cone(6), Root, c - Vector3.up * thick + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rad * 0.45f,
                    new Vector3(rad * 0.8f, rad * r.F(1f, 1.6f), rad * 0.8f), stone, new Vector3(180, r.F(0, 60), 0));
            }
            HangRoots(c - Vector3.up * thick, rad * 0.75f, rad * 0.75f, seed);
            for (int k = 0; k < 2; k++)
            {
                float a = r.F(0, Mathf.PI * 2);
                Ls(Root, c + new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * rad * 0.75f, new Vector3(1.6f, 1f, 1.6f) * rad * 0.18f, k == 0 ? leafA : leafC);
            }
        }

        /// <summary>Long roots and vines hanging from under an island.</summary>
        static void HangRoots(Vector3 bottom, float hx, float hz, int seed)
        {
            var r = new Rng(seed * 7 + 3);
            for (int k = 0; k < 5; k++)
            {
                float a = k * Mathf.PI * 2 / 5 + r.F(-0.3f, 0.3f);
                var p = bottom + new Vector3(Mathf.Cos(a) * hx * 0.85f, 0.2f, Mathf.Sin(a) * hz * 0.85f);
                float len = r.F(2.5f, 5.5f);
                var mid = p + new Vector3(r.F(-0.5f, 0.5f), -len * 0.55f, r.F(-0.5f, 0.5f));
                var end = mid + new Vector3(r.F(-0.6f, 0.6f), -len * 0.45f, r.F(-0.6f, 0.6f));
                var m = k % 2 == 0 ? rootM : moss;
                LRod(Root, p, mid, 0.14f, m);
                LRod(Root, mid, end, 0.09f, m);
            }
        }

        /// <summary>Stone stair block: top at p.y, goes down <paramref name="h"/> metres.</summary>
        static void Step(Vector3 top, float w, float d, float h)
        {
            Lobby.Box(top - Vector3.up * h / 2, new Vector3(w, h, d), stoneLight);
            B(Root, top + new Vector3(0, -0.06f, d / 2 - 0.05f), new Vector3(w + 0.04f, 0.12f, 0.14f), stoneDark);   // worn edge
        }

        /// <summary>Cobblestone path: flat slabs on the grass (decor).</summary>
        static void CobblePath(Vector3 a, Vector3 b, float w)
        {
            var d = b - a; d.y = 0;
            int n = Mathf.Max(2, Mathf.RoundToInt(d.magnitude / 1.15f));
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(a, b, (i + 0.5f) / n) + Vector3.up * 0.07f;
                B(Root, p, new Vector3(w * (i % 2 == 0 ? 1f : 0.85f), 0.07f, 0.95f), i % 3 == 0 ? stoneLight : cobble, new Vector3(0, yaw + (i % 4 - 1.5f) * 4, 0));
            }
        }

        /// <summary>Rope bridge from a to b (top surfaces): plank deck, posts, ropes, invisible side rails.</summary>
        static void RopeBridge(Vector3 a, Vector3 b, float w = 2.6f)
        {
            var d = b - a;
            float len = d.magnitude + 1.2f;
            var go = new GameObject("RopeBridge") { layer = Layers.World };
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation((a + b) / 2, Quaternion.LookRotation(d.normalized));
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.15f, 0); col.size = new Vector3(w, 0.3f, len);
            foreach (var sd in new[] { -1, 1 })
            {
                var rail = go.AddComponent<BoxCollider>();
                rail.center = new Vector3(sd * (w / 2 + 0.1f), 0.7f, 0); rail.size = new Vector3(0.2f, 1.4f, len - 0.6f);
            }
            var t = go.transform;
            int n = Mathf.CeilToInt(len / 0.6f);
            for (int i = 0; i < n; i++)
            {
                float z = -len / 2 + (i + 0.5f) * len / n;
                B(t, new Vector3(0, -0.08f - Mathf.Sin((i + 0.5f) / n * Mathf.PI) * 0.05f, z), new Vector3(w * (i % 3 == 0 ? 1f : 0.94f), 0.12f, len / n - 0.08f), i % 2 == 0 ? plank : wood, new Vector3(0, (i % 4 - 1.5f) * 1.5f, 0));
            }
            foreach (var sd in new[] { -1, 1 })
            {
                var p0 = new Vector3(sd * (w / 2 + 0.05f), 0.6f, -len / 2 + 0.25f);
                var p1 = new Vector3(sd * (w / 2 + 0.05f), 0.6f, len / 2 - 0.25f);
                Cy(t, p0, new Vector3(0.18f, 0.65f, 0.18f), wood);
                Cy(t, p1, new Vector3(0.18f, 0.65f, 0.18f), wood);
                // hand rope sagging in the middle, low rope along the deck
                var mid = new Vector3(sd * (w / 2 + 0.05f), 0.85f, 0);
                LRod(t, p0 + Vector3.up * 0.55f, mid, 0.05f, rope);
                LRod(t, mid, p1 + Vector3.up * 0.55f, 0.05f, rope);
                LRod(t, new Vector3(sd * (w / 2), -0.05f, -len / 2), new Vector3(sd * (w / 2), -0.05f, len / 2), 0.06f, rope);
            }
        }

        /// <summary>Round ruined tower: body, base ring, crenellations (some missing when ruined), dark windows, ivy.</summary>
        static void Tower(Vector3 b, float r, float h, bool solid, bool ruined)
        {
            var rng = new Rng((int)(b.x * 31 + b.z * 17));
            if (solid) Lobby.Solid(PrimitiveType.Cylinder, b + Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), stone);
            else Cy(Root, b + Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), stone);
            Cy(Root, b + Vector3.up * 0.4f, new Vector3(r * 2.3f, 0.4f, r * 2.3f), stoneLight);
            Cy(Root, b + Vector3.up * (h - 0.3f), new Vector3(r * 2.25f, 0.3f, r * 2.25f), stoneLight);
            int n = 8;
            for (int k = 0; k < n; k++)
            {
                if (ruined && (k == 2 || k == 3 || k == 6)) continue;
                float a = k * Mathf.PI * 2 / n;
                float hh = ruined ? rng.F(0.4f, 1f) : 0.8f;
                B(Root, b + new Vector3(Mathf.Cos(a) * r * 0.95f, h + hh / 2, Mathf.Sin(a) * r * 0.95f), new Vector3(r * 0.55f, hh, r * 0.4f), stone, new Vector3(0, -a * Mathf.Rad2Deg + 90, 0));
            }
            for (int k = 0; k < 3; k++)
            {
                float a = rng.F(0, Mathf.PI * 2), y = h * (0.35f + k * 0.2f);
                B(Root, b + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), new Vector3(r * 0.35f, r * 0.6f, 0.2f), dark, new Vector3(0, -a * Mathf.Rad2Deg + 90, 0));
            }
            for (int k = 0; k < 3; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ca(Root, b + new Vector3(Mathf.Cos(a) * r * 1.01f, h * rng.F(0.3f, 0.75f), Mathf.Sin(a) * r * 1.01f), new Vector3(0.25f, h * 0.18f, 0.25f) * Mathf.Max(1, r * 0.5f), moss);
            }
        }

        /// <summary>Stone arch (two pillars + lintel); broken: one pillar shorter and half the lintel gone.</summary>
        static void Arch(Vector3 c, float yaw, float s, bool solid, bool broken)
        {
            var t = Node("RuinArch", Root, c, new Vector3(0, yaw, 0));
            float ph = 4.4f * s, ph2 = broken ? ph * 0.6f : ph;
            void Block(Vector3 p, Vector3 sc)
            {
                if (solid) Lobby.Solid(PrimitiveType.Cube, p, sc, stone, default, t);
                else B(t, p, sc, stone);
            }
            Block(new Vector3(-2.2f * s, ph / 2, 0), new Vector3(1f * s, ph, 1f * s));
            Block(new Vector3(2.2f * s, ph2 / 2, 0), new Vector3(1f * s, ph2, 1f * s));
            if (!broken) Block(new Vector3(0, ph + 0.45f * s, 0), new Vector3(5.6f * s, 0.9f * s, 1.1f * s));
            else
            {
                Block(new Vector3(-1.2f * s, ph + 0.45f * s, 0), new Vector3(3.2f * s, 0.9f * s, 1.1f * s));
                B(t, new Vector3(3.4f * s, 0.35f * s, 0.8f * s), new Vector3(1.4f * s, 0.7f * s, 0.9f * s), stone, new Vector3(0, 25, 12));   // fallen block
            }
            B(t, new Vector3(-2.2f * s, ph * 0.5f, 0.52f * s), new Vector3(0.3f * s, ph * 0.6f, 0.05f), moss);
            Ca(t, new Vector3(-1.4f * s, ph - 0.2f * s, 0.56f * s), new Vector3(0.1f, 0.6f, 0.1f) * s, moss);
        }

        static void Column(Vector3 b, float h, bool broken, bool solid)
        {
            float w = solid ? 1 : Mathf.Max(1, h / 4);
            if (solid) Lobby.Solid(PrimitiveType.Cylinder, b + Vector3.up * h / 2, new Vector3(0.8f, h / 2, 0.8f), stone);
            else Cy(Root, b + Vector3.up * h / 2, new Vector3(0.8f * w, h / 2, 0.8f * w), stone);
            B(Root, b + Vector3.up * 0.15f * w, new Vector3(1.2f, 0.3f, 1.2f) * w, stoneLight);
            if (!broken) B(Root, b + Vector3.up * (h + 0.15f * w), new Vector3(1.2f, 0.3f, 1.2f) * w, stoneLight);
            else Cone(Root, b + Vector3.up * h, new Vector3(0.8f, 0.4f, 0.8f) * w, stone, new Vector3(0, 0, 20));
            Ca(Root, b + new Vector3(0.4f * w, h * 0.45f, 0), new Vector3(0.12f, h * 0.2f, 0.12f) * w, moss);
        }

        /// <summary>Twisted tree: bent trunk, a couple of branches, flat round leaf clumps, roots.</summary>
        static void TwistedTree(Vector3 p, float s, bool solid, int seed)
        {
            var r = new Rng(seed);
            var p1 = p + new Vector3(r.F(-0.6f, 0.6f), 2.2f, r.F(-0.6f, 0.6f)) * s;
            var p2 = p1 + new Vector3(r.F(-1.2f, 1.2f), 2f, r.F(-1.2f, 1.2f)) * s;
            var p3 = p2 + new Vector3(r.F(-0.8f, 0.8f), 1.4f, r.F(-0.8f, 0.8f)) * s;
            if (solid) Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.1f * s, new Vector3(0.75f * s, 1.1f * s, 0.75f * s), bark);
            else Rod(Root, p, p1, 0.8f * s, bark);
            Rod(Root, p1, p2, 0.6f * s, bark);
            Rod(Root, p2, p3, 0.45f * s, bark);
            var br = p2 + new Vector3(r.F(1.5f, 2.2f) * (r.Chance(0.5f) ? 1 : -1), 0.9f, r.F(-1, 1)) * s;
            Rod(Root, p2 - Vector3.up * 0.4f * s, br, 0.3f * s, bark);
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f + r.F(0, 1);
                Spike(Root, p + Vector3.up * 0.5f * s, p + new Vector3(Mathf.Cos(a) * 1.3f, -0.1f, Mathf.Sin(a) * 1.3f) * s, 0.4f * s, bark);
            }
            var leaves = new[] { leafA, leafB, leafC };
            Ls(Root, p3 + Vector3.up * 0.6f * s, new Vector3(3.6f, 1.6f, 3.4f) * s, leaves[r.I(3)]);
            Ls(Root, br + Vector3.up * 0.4f * s, new Vector3(2.6f, 1.2f, 2.4f) * s, leaves[r.I(3)]);
            for (int k = 0; k < 3; k++)
                Ls(Root, p3 + new Vector3(r.F(-1.6f, 1.6f), r.F(-0.4f, 0.8f), r.F(-1.6f, 1.6f)) * s, new Vector3(2f, 1.1f, 2f) * s * r.F(0.8f, 1.1f), leaves[r.I(3)]);
        }

        /// <summary>Treasure chest: wooden box, round lid, gold bands, glowing lock.</summary>
        static void Chest(Vector3 p, float yaw, bool solid = true)
        {
            var t = Node("Chest", Root, p, new Vector3(0, yaw, 0));
            if (solid) Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 0.38f, 0), new Vector3(1.3f, 0.76f, 0.85f), chestWood, default, t);
            else B(t, new Vector3(0, 0.38f, 0), new Vector3(1.3f, 0.76f, 0.85f), chestWood);
            Cy(t, new Vector3(0, 0.76f, 0), new Vector3(0.85f, 0.65f, 0.85f), chestWood, new Vector3(0, 0, 90));
            foreach (var x in new[] { -0.45f, 0.45f })
            {
                B(t, new Vector3(x, 0.38f, 0), new Vector3(0.12f, 0.8f, 0.9f), gold);
                Cy(t, new Vector3(x, 0.76f, 0), new Vector3(0.9f, 0.06f, 0.9f), gold, new Vector3(0, 0, 90));
            }
            B(t, new Vector3(0, 0.72f, -0.45f), new Vector3(0.22f, 0.28f, 0.08f), goldGlow);
            Ls(t, new Vector3(0, 0.9f, 0), 1.9f, Add("FFE14D", 0.12f)).AddComponent<Pulse>().amount = 0.05f;
        }

        /// <summary>Waterfall pouring off an island edge at <paramref name="lip"/> in direction <paramref name="dir"/>.</summary>
        static void Waterfall(Vector3 lip, Vector3 dir, float w, float drop)
        {
            var t = Node("Waterfall", Root, lip, new Vector3(0, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0));
            B(t, new Vector3(0, -0.15f, 0.25f), new Vector3(w * 1.1f, 0.35f, 0.6f), foam, new Vector3(-20, 0, 0));
            B(t, new Vector3(0, -drop / 2, 0.45f), new Vector3(w, drop, 0.3f), water);
            B(t, new Vector3(0, -drop / 2, 0.5f), new Vector3(w * 0.5f, drop, 0.32f), waterCore);
            Ls(t, new Vector3(0, -drop, 0.5f), new Vector3(w * 3.5f, w * 2f, w * 3.5f), mist).AddComponent<Pulse>().amount = 0.06f;
        }

        /// <summary>Flat stream on the grass running to a waterfall (decor).</summary>
        static void Stream(Vector3 a, Vector3 b, float w)
        {
            var d = b - a;
            var s = B(Root, (a + b) / 2 + Vector3.up * 0.08f, new Vector3(w, 0.05f, d.magnitude), water);
            s.transform.rotation = Quaternion.LookRotation(d.normalized);
            S(Root, a + Vector3.up * 0.06f, new Vector3(w * 1.6f, 0.1f, w * 1.6f), water);   // little spring pool
        }

        static void Pond(Vector3 c, float r)
        {
            Cy(Root, c + Vector3.up * 0.07f, new Vector3(r * 2, 0.03f, r * 2), water);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                Ls(Root, c + new Vector3(Mathf.Cos(a) * r, 0.1f, Mathf.Sin(a) * r), new Vector3(0.5f, 0.3f, 0.5f), k % 2 == 0 ? stoneLight : stone);
            }
        }

        static void Bushes(Vector3 c, int n, int seed)
        {
            var r = new Rng(seed);
            for (int k = 0; k < n; k++)
                Ls(Root, c + new Vector3(r.F(-0.8f, 0.8f), 0.35f, r.F(-0.8f, 0.8f)), new Vector3(1.2f, 0.9f, 1.2f) * r.F(0.8f, 1.2f), k % 2 == 0 ? leafA : leafC);
        }

        static void Coins(Vector3 c, float rad, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                Pickups.I.Coin(c + new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad));
            }
        }
    }
}
