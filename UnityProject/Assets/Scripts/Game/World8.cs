using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// «Мир 8: Самый мемный мир» (world id 8). Bright neon floating islands over a purple cloud sea, glowing bridges,
    /// pink trampolines up to higher islands, golden smileys, silly patrolling mobs and giant billboards with our own
    /// meme parodies (a singing cat, a «sigma» stone face, a cat with dumplings, a laughing smiley, a dancing banana,
    /// a duck in sunglasses). Main route ~228 m along +z with three side branches (west A, east B, west C).
    /// 12 friends in cages, 12 keys, 4 checkpoints, a golden portal on the farthest island → <see cref="Win"/>.
    /// The robot «Пыжик» flies next to the player and gives hints. All positions are local to <see cref="O"/>.
    /// </summary>
    public static class World8
    {
        public const int Id = 8;
        public static readonly Vector3 O = new Vector3(0, 40, 8000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 2.5f);
        public static float FallY => O.y - 20f;
        public static Transform Root;

        // hint data for Пыжик
        public static readonly List<(string id, Vector3 pos)> KeySpots = new List<(string, Vector3)>();
        public static readonly List<Vector3> PadSpots = new List<Vector3>();
        public static readonly List<MemeMob> Mobs = new List<MemeMob>();
        public static Vector3 PortalPos;

        static readonly string[] TopHex = { "FF5FB8", "35E6FF", "9BFF3A", "FFE135", "B36BFF" };
        static Material[] tops, rims;
        static Material rock, bridgeM, white, black, gold, goldGlow, pinkGlow, trampBase, moverM, cloudA, cloudB, cloudC;

        // islands for the decor pass, spots to keep free of decor
        static readonly List<(Vector3 c, float r, int col)> isles = new List<(Vector3, float, int)>();
        static readonly List<Vector3> clear = new List<Vector3>();

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        public static void Build()
        {
            Root = new GameObject("World8").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            KeySpots.Clear(); PadSpots.Clear(); Mobs.Clear(); isles.Clear(); clear.Clear();

            // materials: created once
            tops = new Material[TopHex.Length]; rims = new Material[TopHex.Length];
            for (int i = 0; i < TopHex.Length; i++) { tops[i] = M(TopHex[i], 0.45f); rims[i] = G(TopHex[i], 2.2f); }
            rock = M("5B3A9E", 0.2f); bridgeM = M("2B1B5E", 0.5f); white = M("FFFFFF", 0.4f); black = M("15121F", 0.8f);
            gold = M("FFC83A", 0.9f, 0.8f); goldGlow = G("FFD23A", 1.8f); pinkGlow = G("FF4FB0", 1.8f);
            trampBase = M("6A2DB8", 0.5f); moverM = M("FF8AD0", 0.5f);
            cloudA = M("8E5BE8", 0.05f); cloudB = M("B98CFF", 0.05f); cloudC = M("6A3FC9", 0.05f);
            clear.Add(Spawn); clear.Add(Spawn + new Vector3(3.5f, 0, 1));   // the boss pad spot stays free

            Route();
            BranchA(); BranchB(); BranchC();
            Billboards();
            Backdrop();
            IslandDecor();
            new GameObject("Pyzhik").AddComponent<Pyzhik>().Build(Root);
            Lobby.root = keep;
            Parts.Layer = Layers.World;
        }

        // ---------------- the main route ----------------

        static void Route()
        {
            // start island (16 m), welcome arch
            Island(0, 0, 4, 8, 1);
            Arch(V(0, 0, 10.6f));
            Cage("w8c0", V(-5, 0, 8), 150);
            CoinLine(V(0, 0.9f, 7.5f), V(0, 0.9f, 10.5f), 3);

            Bridge(new Vector3(0, 0, 11.7f), new Vector3(0, 0.5f, 20.3f), 0);
            CoinLine(V(0, 1.3f, 13), V(0, 1.6f, 19), 5);

            // island 1 (pink): a jelly mob, key 0
            Island(0, 0.5f, 26, 6, 0);
            Mob(0, V(-3, 0.5f, 27), V(3, 0.5f, 27));
            Key("w8k0", V(-3.5f, 1.6f, 29.5f));
            CoinCircle(V(0, 1.4f, 26), 2.5f, 6);

            // stepping stones up
            Stone(1.5f, 1.1f, 34.5f, 1.6f, 3); Stone(-1.5f, 1.7f, 39.5f, 1.6f, 4); Stone(1.5f, 2.2f, 44.5f, 1.6f, 1);
            Coin(V(1.5f, 2, 34.5f)); Coin(V(-1.5f, 2.6f, 39.5f)); Coin(V(1.5f, 3.1f, 44.5f));

            // island 2 (lime): checkpoint 1, trampoline up to a little high island with key 1
            Island(0, 2.5f, 53, 7, 2);
            Checkpoint(V(-3.5f, 2.5f, 49));
            Cage("w8c1", V(-4.5f, 2.5f, 56), 120);
            Trampoline(4, 2.5f, 55, 17f);
            CoinColumn(V(4, 4.5f, 55), 4);
            Island(6, 9, 62, 3, 0, 0.8f);
            Key("w8k1", V(6, 10.1f, 62.5f));
            CoinLine(V(5, 9.9f, 60.5f), V(7, 9.9f, 61), 2);

            Bridge(new Vector3(0, 2.5f, 59.8f), new Vector3(0, 3, 69.2f), 2);
            CoinLine(V(0, 3.4f, 61), V(0, 3.9f, 68), 5);

            // island 3 (violet): an egg mob, key 6
            Island(0, 3, 75, 6, 4);
            Mob(2, V(-3, 3, 76), V(3, 3, 74));
            Cage("w8c3", V(4, 3, 78.5f), -130);
            Key("w8k6", V(-4, 4.1f, 72));
            CoinLine(V(-2, 3.9f, 79), V(2, 3.9f, 79), 3);

            // a sliding pink platform over the gap
            MovingPlat(V(0, 3, 84.5f), V(0, 3, 92.5f), 4.5f);
            Coin(V(0, 4.4f, 88.5f));

            // island 4 (pink): checkpoint 2, trampoline to a high island with key 3, branch B goes east
            Island(0, 3.5f, 101, 7, 0);
            Checkpoint(V(-3.5f, 3.5f, 96.5f));
            Mob(0, V(-2, 3.5f, 100), V(3, 3.5f, 100));
            Cage("w8c4", V(-5, 3.5f, 104), 120);
            Trampoline(4, 3.5f, 104, 17f);
            CoinColumn(V(4, 5.5f, 104), 3);
            Island(6, 10, 110, 3, 1, 0.8f);
            Key("w8k3", V(6, 11.1f, 110.5f));
            CoinCircle(V(6, 10.9f, 110), 1.6f, 3);

            Bridge(new Vector3(0, 3.5f, 107.8f), new Vector3(0, 5, 117.2f), 1);
            CoinLine(V(0, 4.4f, 109), V(0, 5.8f, 116), 5);

            // island 5 (cyan): trampoline up to island 6
            Island(0, 5, 123, 6, 1);
            Cage("w8c6", V(4, 5, 121), -120);
            Trampoline(0, 5, 126.5f, 15f);
            CoinColumn(V(0, 7, 126.5f), 3);
            CoinLine(V(-3, 5.9f, 120), V(-1, 5.9f, 120), 2);

            // island 6 (lime): checkpoint 3, branch C goes west
            Island(0, 9.5f, 136, 6, 2);
            Checkpoint(V(-3.5f, 9.5f, 133));
            Cage("w8c7", V(3.5f, 9.5f, 139), -140);
            CoinLine(V(-1.5f, 10.4f, 138), V(1.5f, 10.4f, 138), 3);

            // floating stones with key 9 in the middle
            Stone(1.5f, 10, 145.5f, 1.6f, 0); Stone(-1.5f, 10.5f, 150.5f, 1.6f, 3); Stone(1.5f, 11, 155.5f, 1.6f, 4);
            Key("w8k9", V(-1.5f, 11.6f, 150.5f));
            Coin(V(1.5f, 10.9f, 145.5f)); Coin(V(1.5f, 11.9f, 155.5f));

            // island 7 (yellow): two mobs, key 8
            Island(0, 11.5f, 164, 7, 3);
            Mob(0, V(-4, 11.5f, 164), V(4, 11.5f, 164));
            Mob(1, V(3, 11.5f, 159.5f), V(3, 11.5f, 168.5f));
            Key("w8k8", V(-4, 12.6f, 167.5f));
            CoinCircle(V(0, 12.4f, 164), 2.2f, 6);

            Bridge(new Vector3(0, 11.5f, 170.8f), new Vector3(0, 12, 179.2f), 3);
            CoinLine(V(0, 12.4f, 172), V(0, 12.9f, 178), 5);

            // island 8 (violet)
            Island(0, 12, 185, 6, 4);
            Cage("w8c10", V(4, 12, 187), -130);
            Key("w8k11", V(-4, 13.1f, 183));
            CoinLine(V(-1.5f, 12.9f, 188), V(1.5f, 12.9f, 188), 3);

            Bridge(new Vector3(0, 12, 190.8f), new Vector3(0, 13, 199.2f), 4);
            CoinLine(V(0, 12.9f, 192), V(0, 13.9f, 198), 5);

            // island 9 (pink): checkpoint 4, two trampolines up to the summit
            Island(0, 13, 205, 6, 0);
            Checkpoint(V(-3.5f, 13, 201));
            Mob(2, V(-3, 13, 205), V(3, 13, 205));
            Key("w8k10", V(3.5f, 14.1f, 201.5f));
            Trampoline(-2.5f, 13, 208.5f, 18f);
            Trampoline(2.5f, 13, 208.5f, 18f);
            CoinColumn(V(-2.5f, 15, 208.5f), 3); CoinColumn(V(2.5f, 15, 208.5f), 3);

            // the summit (yellow): the last friend and the golden portal
            Island(0, 19, 222, 8, 3);
            Cage("w8c11", V(-5, 19, 218), 60);
            CoinCircle(V(0, 19.9f, 220), 3.2f, 10);
            Pickups.I.Coin(V(0, 20.4f, 220), 5); Parts.Layer = Layers.World;
            Portal(V(0, 19, 227.5f));
        }

        // west branch from island 2: a yellow island + a far violet one with cage 2
        static void BranchA()
        {
            Bridge(new Vector3(-6.8f, 2.5f, 53), new Vector3(-15.2f, 2.5f, 53), 3);
            CoinLine(V(-8, 3.4f, 53), V(-14, 3.4f, 53), 4);
            Island(-21, 2.5f, 53, 6, 3);
            Mob(1, V(-24, 2.5f, 51), V(-18, 2.5f, 55));
            Key("w8k2", V(-21, 3.6f, 49.5f));
            CoinLine(V(-23, 3.4f, 56), V(-19, 3.4f, 56), 3);
            Stone(-29.5f, 3, 55, 1.6f, 1);
            Coin(V(-29.5f, 3.9f, 55));
            Island(-37, 3.5f, 57, 5, 4);
            Cage("w8c2", V(-38, 3.5f, 58), 90);
            CoinCircle(V(-36, 4.4f, 56), 2f, 5);
            Pickups.I.Coin(V(-36, 4.8f, 56), 5); Parts.Layer = Layers.World;
        }

        // east branch from island 4: lime island, a far island with cage 5 and a trampoline to key 5
        static void BranchB()
        {
            Bridge(new Vector3(6.8f, 3.5f, 101), new Vector3(15.2f, 4, 101), 2);
            CoinLine(V(8, 4.4f, 101), V(14, 4.8f, 101), 4);
            Island(21, 4, 101, 6, 2);
            Mob(1, V(18, 4, 99), V(24, 4, 99));
            Key("w8k4", V(21, 5.1f, 104.5f));
            Stone(29.5f, 4.5f, 104, 1.6f, 0);
            Coin(V(29.5f, 5.4f, 104));
            Island(37, 5, 108, 5, 1);
            Cage("w8c5", V(40, 5, 106), -60);
            Trampoline(37, 5, 110, 17f);
            CoinColumn(V(37, 7, 110), 3);
            Island(38, 11.5f, 117, 3.2f, 3, 0.8f);
            Key("w8k5", V(38, 12.6f, 117.5f));
            CoinCircle(V(38, 12.4f, 117), 1.8f, 4);
            Pickups.I.Coin(V(38, 12.9f, 117), 5); Parts.Layer = Layers.World;
        }

        // west branch from island 6: pink island (cage 9, key 7), far violet island with cage 8
        static void BranchC()
        {
            Bridge(new Vector3(-5.8f, 9.5f, 136), new Vector3(-15.2f, 9.5f, 136), 0);
            CoinLine(V(-7, 10.4f, 136), V(-14, 10.4f, 136), 4);
            Island(-21, 9.5f, 136, 6, 0);
            Mob(2, V(-24, 9.5f, 134), V(-18, 9.5f, 138));
            Cage("w8c9", V(-23, 9.5f, 139.5f), 100);
            Key("w8k7", V(-21, 10.6f, 132));
            Stone(-29.5f, 10, 138, 1.6f, 2);
            Coin(V(-29.5f, 10.9f, 138));
            Island(-37, 10.5f, 142, 5, 4);
            Cage("w8c8", V(-38, 10.5f, 143), 90);
            CoinCircle(V(-36, 11.4f, 141), 2f, 5);
            Pickups.I.Coin(V(-36, 11.8f, 141), 5); Parts.Layer = Layers.World;
        }

        // ---------------- pieces ----------------

        /// <summary>A round neon island: coloured top (walkable), a glowing rim, a rock cone underneath.</summary>
        static void Island(float x, float y, float z, float r, int col, float coneK = 1.3f)
        {
            var c = V(x, y, z);
            var t = Node("Island", Root, c);
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, -0.6f, 0), new Vector3(r * 2, 0.6f, r * 2), tops[col], default, t);
            Spike(t, new Vector3(0, -1.2f, 0), new Vector3(0, -1.2f - r * coneK, 0), r * 1.9f, rock);
            Ring(t, new Vector3(0, 0.02f, 0), r - 0.05f, 0.13f, rims[col]);
            Ring(t, new Vector3(0, -1.2f, 0), r * 0.95f, 0.1f, rims[(col + 2) % rims.Length]);
            isles.Add((c, r, col));
        }

        /// <summary>A small floating stepping stone.</summary>
        static void Stone(float x, float y, float z, float r, int col)
        {
            var t = Node("Stone", Root, V(x, y, z));
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, -0.4f, 0), new Vector3(r * 2, 0.4f, r * 2), tops[col], default, t);
            Spike(t, new Vector3(0, -0.8f, 0), new Vector3(0, -2.4f, 0), r * 1.8f, rock);
            Ring(t, new Vector3(0, 0.02f, 0), r - 0.05f, 0.1f, rims[col]);
        }

        /// <summary>A glowing bridge between two deck-top points (local).</summary>
        static void Bridge(Vector3 la, Vector3 lb, int col)
        {
            Vector3 a = O + la, b = O + lb, d = b - a;
            float L = d.magnitude;
            var n = Node("Bridge", Root, (a + b) / 2, Quaternion.LookRotation(d.normalized).eulerAngles);
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, -0.2f, 0), new Vector3(2.6f, 0.4f, L + 1f), bridgeM, default, n);
            foreach (var sd in new[] { -1, 1 })
            {
                B(n, new Vector3(sd * 1.25f, 0.01f, 0), new Vector3(0.14f, 0.04f, L + 1f), rims[col]);   // glowing deck edge
                B(n, new Vector3(sd * 1.3f, 0.8f, 0), new Vector3(0.1f, 0.1f, L), rims[col]);           // rail
                for (int k = 0; k <= 2; k++)
                {
                    float z = -L / 2 + k * L / 2;
                    Cy(n, new Vector3(sd * 1.3f, 0.4f, z), new Vector3(0.1f, 0.4f, 0.1f), white);
                    S(n, new Vector3(sd * 1.3f, 0.9f, z), 0.24f, rims[(col + k + 1) % rims.Length]);
                }
            }
        }

        static void MovingPlat(Vector3 a, Vector3 b, float period)
        {
            var go = Lobby.Box(a - Vector3.up * 0.3f, new Vector3(3.4f, 0.6f, 3.4f), moverM);
            B(go.transform, new Vector3(0, 0.5f, 0.5f), new Vector3(1.01f, 0.08f, 0.03f), goldGlow);
            B(go.transform, new Vector3(0, 0.5f, -0.5f), new Vector3(1.01f, 0.08f, 0.03f), goldGlow);
            var m = go.AddComponent<Mover>();
            m.a = go.transform.position; m.b = go.transform.position + (b - a); m.period = period;
        }

        /// <summary>A pink trampoline (BouncePad) standing on <paramref name="y"/>.</summary>
        static void Trampoline(float x, float y, float z, float power)
        {
            var top = V(x, y, z);
            var t = Node("PinkTrampoline", Root, top);
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(2.4f, 0.15f, 2.4f), trampBase, default, t);
            Ring(t, new Vector3(0, 0.32f, 0), 1.12f, 0.1f, goldGlow);
            var skin = Node("Skin", t, new Vector3(0, 0.32f, 0));
            Cy(skin, Vector3.zero, new Vector3(2.1f, 0.05f, 2.1f), pinkGlow);
            Cy(skin, new Vector3(0, 0.02f, 0), new Vector3(0.8f, 0.05f, 0.8f), white);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.35f, 0); col.size = new Vector3(2, 0.3f, 2);
            var pad = t.gameObject.AddComponent<BouncePad>();
            pad.power = power; pad.top = skin;
            PadSpots.Add(top); clear.Add(top);
        }

        static void Checkpoint(Vector3 p)
        {
            new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);
            clear.Add(p);
        }

        static void Cage(string id, Vector3 p, float yaw)
        {
            Rescue.SpawnCage(id, p, yaw, Root);
            Parts.Layer = Layers.World;
            clear.Add(p);
        }

        static void Key(string id, Vector3 p)
        {
            Pickups.I.Key(id, p);
            Parts.Layer = Layers.World;
            KeySpots.Add((id, p));
            clear.Add(p - Vector3.up * 1.1f);
        }

        static void Coin(Vector3 p) { Pickups.I.Coin(p); Parts.Layer = Layers.World; }
        static void CoinLine(Vector3 a, Vector3 b, int n) { for (int i = 0; i < n; i++) Coin(Vector3.Lerp(a, b, n == 1 ? 0.5f : (float)i / (n - 1))); }
        static void CoinColumn(Vector3 bottom, int n) { for (int i = 0; i < n; i++) Coin(bottom + Vector3.up * i * 1.2f); }
        static void CoinCircle(Vector3 c, float r, int n)
        {
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; Coin(c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); }
        }

        static void Mob(int kind, Vector3 a, Vector3 b)
        {
            Mobs.Add(MemeMob.Spawn(kind, a, b, Root));
            Parts.Layer = Layers.World;
            clear.Add(a); clear.Add(b);
        }

        /// <summary>The welcome arch over the first bridge.</summary>
        static void Arch(Vector3 c)
        {
            var t = Node("WelcomeArch", Root, c);
            foreach (var sd in new[] { -1, 1 })
            {
                Lobby.Solid(PrimitiveType.Cylinder, new Vector3(sd * 2.6f, 2.5f, 0), new Vector3(0.6f, 2.5f, 0.6f), tops[0], default, t);
                S(t, new Vector3(sd * 2.6f, 5.3f, 0), 0.9f, goldGlow).AddComponent<Pulse>().amount = 0.12f;
            }
            B(t, new Vector3(0, 5.3f, 0), new Vector3(6, 0.9f, 0.5f), rims[4]);
            B(t, new Vector3(0, 6.6f, 0), new Vector3(9.5f, 1.8f, 0.3f), M("2B1B5E", 0.5f));
            B(t, new Vector3(0, 6.6f, 0.02f), new Vector3(9.8f, 2.1f, 0.2f), rims[1]);
            var tx = Prim.Text(t, "МИР 8: САМЫЙ МЕМНЫЙ МИР", new Vector3(0, 6.6f, -0.2f), 6, Hex("FFE135"));
            tx.rectTransform.sizeDelta = new Vector2(9.2f, 1.6f);
            tx.enableAutoSizing = true; tx.fontSizeMin = 1; tx.fontSizeMax = 6;
        }

        /// <summary>The golden portal at the end: stepping in finishes the world.</summary>
        static void Portal(Vector3 c)
        {
            PortalPos = c;
            var t = Node("GoldenPortal", Root, c);
            Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(6.4f, 0.12f, 6.4f), gold, default, t);
            Ring(t, new Vector3(0, 0.26f, 0), 3.15f, 0.12f, goldGlow);
            foreach (var sd in new[] { -1, 1 })
            {
                Lobby.Solid(PrimitiveType.Cylinder, new Vector3(sd * 3.9f, 1.9f, 0), new Vector3(0.7f, 1.9f, 0.7f), gold, default, t);
                S(t, new Vector3(sd * 3.9f, 4.1f, 0), 0.9f, pinkGlow).AddComponent<Pulse>();
            }
            var ring = Node("Ring", t, new Vector3(0, 3.5f, 0));
            Ring(ring, Vector3.zero, 2.9f, 0.38f, goldGlow, new Vector3(90, 0, 0));
            Ring(ring, Vector3.zero, 3.45f, 0.12f, pinkGlow, new Vector3(90, 0, 0));
            var surf = Cy(ring, Vector3.zero, new Vector3(5.4f, 0.02f, 5.4f), Add("FFD54A", 0.45f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().speed = 60;
            var swirl = Node("Swirl", ring, Vector3.zero);
            swirl.gameObject.AddComponent<Spin>().axis = Vector3.forward;
            swirl.GetComponent<Spin>().speed = -90;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4;
                Msh(Meshes.Star(), swirl, new Vector3(Mathf.Cos(a) * 3.9f, Mathf.Sin(a) * 3.9f, 0), new Vector3(0.6f, 0.6f, 0.2f), k % 2 == 0 ? goldGlow : pinkGlow);
            }
            SmileyFace(ring, new Vector3(0, 0, -0.05f), 2.2f);
            var tx = Prim.Text(t, "ЗОЛОТОЙ ПОРТАЛ — ФИНИШ!", new Vector3(0, 8.4f, 0), 6, Hex("FFE135"));
            tx.gameObject.AddComponent<Billboard>();
            var lg = new GameObject("PortalLight").AddComponent<Light>();
            lg.transform.SetParent(t, false); lg.transform.localPosition = new Vector3(0, 3.5f, -1.5f);
            lg.type = LightType.Point; lg.color = Hex("FFD23A"); lg.range = 16; lg.intensity = 3;
            Zones.Add(c + Vector3.up * 0.25f, 2.3f, "ЗОЛОТОЙ ПОРТАЛ", Win, () => Worlds.Current == Id, Zones.Kind.Step);
            clear.Add(c);
        }

        /// <summary>A golden smiley face (facing −z) under <paramref name="p"/>; no colliders.</summary>
        static void SmileyFace(Transform p, Vector3 pos, float d)
        {
            var f = Node("SmileyFace", p, pos);
            Cy(f, Vector3.zero, new Vector3(d, 0.04f * d, d), goldGlow, new Vector3(90, 0, 0));
            foreach (var sd in new[] { -1, 1 }) S(f, new Vector3(sd * 0.17f * d, 0.12f * d, -0.04f * d), new Vector3(0.1f * d, 0.17f * d, 0.04f * d), black);
            for (int k = 0; k < 5; k++)
            {
                float a = Mathf.Lerp(205, 335, k / 4f) * Mathf.Deg2Rad;
                S(f, new Vector3(Mathf.Cos(a) * 0.28f * d, Mathf.Sin(a) * 0.28f * d - 0.02f * d, -0.04f * d), 0.08f * d, black);
            }
        }

        /// <summary>A big floating golden smiley disc that always faces the camera and rocks a little.</summary>
        static void SmileyDisc(Vector3 p, float d)
        {
            var t = Node("SmileyDisc", Root, p);
            t.gameObject.AddComponent<Billboard>();
            var face = Node("Face", t, Vector3.zero);
            SmileyFace(face, Vector3.zero, d);
            Ring(face, Vector3.zero, 0.5f * d, 0.04f * d, gold, new Vector3(90, 0, 0));
            var w = face.gameObject.AddComponent<W8Wiggle>();
            w.rotAmp = new Vector3(0, 0, 14); w.posAmp = new Vector3(0, 0.4f, 0); w.speed = 1.6f; w.phase = p.z * 0.1f;
        }

        // ---------------- billboards with meme parodies ----------------

        static void Billboards()
        {
            Board(V(-14, 0, 28), -40, 0, 1);
            Board(V(15, 2, 60), 45, 1, 1);
            Board(V(-14, 3, 84), -40, 2, 1);
            Board(V(16, 8, 142), 45, 3, 1);
            Board(V(-15, 11, 172), -40, 4, 1);
            Board(V(14, 12, 195), 40, 5, 1);
            // giant far ones
            Board(V(55, 2, 70), 55, 3, 2.2f);
            Board(V(-56, 4, 120), -55, 0, 2.2f);
            Board(V(0, 26, 262), 0, 5, 2.5f);
        }

        static readonly string[] Captions = { "ЧИПИ-ЧИПИ ЧАПА-ЧАПА", "СИГМА", "КОТ И ПЕЛЬМЕНИ", "ХА-ХА-ХА", "БАНАНЧИК", "КРЯ" };
        static readonly string[] BoardBg = { "7FE3FF", "2B2350", "FFB6E1", "B36BFF", "35E6FF", "FF8A3D" };

        /// <summary>A billboard on two long posts: a glowing frame, a picture made of shapes, a caption.</summary>
        static void Board(Vector3 basePos, float yaw, int kind, float scale)
        {
            var t = Node("Billboard", Root, basePos, new Vector3(0, yaw, 0));
            t.localScale = Vector3.one * scale;
            var post = M("3A2A6E", 0.5f, 0.3f);
            foreach (var sd in new[] { -1, 1 }) Cy(t, new Vector3(sd * 3.2f, -1, 0.3f), new Vector3(0.35f, 4.5f, 0.35f), post);
            B(t, new Vector3(0, 5.8f, 0), new Vector3(8.4f, 5.6f, 0.3f), M("FFFFFF", 0.3f));
            B(t, new Vector3(0, 5.8f, -0.17f), new Vector3(7.6f, 4.8f, 0.04f), M(BoardBg[kind], 0.3f));
            var fr = rims[kind % rims.Length];
            B(t, new Vector3(0, 8.6f, -0.1f), new Vector3(8.6f, 0.2f, 0.2f), fr);
            B(t, new Vector3(0, 3.0f, -0.1f), new Vector3(8.6f, 0.2f, 0.2f), fr);
            foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 4.3f, 5.8f, -0.1f), new Vector3(0.2f, 5.8f, 0.2f), fr);
            for (int k = 0; k < 5; k++) S(t, new Vector3(-3.4f + k * 1.7f, 8.85f, -0.15f), 0.3f, k % 2 == 0 ? goldGlow : pinkGlow);
            // caption strip
            B(t, new Vector3(0, 2.1f, 0), new Vector3(8.2f, 1.4f, 0.3f), M("1E1240", 0.5f));
            var tx = Prim.Text(t, Captions[kind], new Vector3(0, 2.1f, -0.2f), 8, Hex("FFE135"));
            tx.rectTransform.sizeDelta = new Vector2(7.8f, 1.2f);
            tx.enableAutoSizing = true; tx.fontSizeMin = 1; tx.fontSizeMax = 8;
            var pic = Node("Picture", t, new Vector3(0, 5.8f, -0.3f));
            switch (kind)
            {
                case 0: SingingCat(pic); break;
                case 1: StoneFace(pic); break;
                case 2: DumplingCat(pic); break;
                case 3: LaughSmiley(pic); break;
                case 4: Banana(pic); break;
                default: CoolDuck(pic); break;
            }
        }

        static W8Wiggle Wig(Transform t, Vector3 rot, Vector3 pos, float speed)
        {
            var w = t.gameObject.AddComponent<W8Wiggle>();
            w.rotAmp = rot; w.posAmp = pos; w.speed = speed;
            return w;
        }

        /// <summary>An orange cat singing with its mouth wide open, music notes around.</summary>
        static void SingingCat(Transform p)
        {
            var orange = M("FF9A2E", 0.3f); var pinkIn = M("FF9EC4", 0.3f);
            var head = Node("Head", p, new Vector3(0, -0.4f, 0));
            Wig(head, new Vector3(0, 0, 10), new Vector3(0, 0.15f, 0), 5);
            S(head, Vector3.zero, new Vector3(2.8f, 2.3f, 0.8f), orange);
            foreach (var sd in new[] { -1, 1 })
            {
                Spike(head, new Vector3(sd * 0.8f, 0.7f, 0), new Vector3(sd * 1.15f, 1.75f, 0), 0.9f, orange);
                Spike(head, new Vector3(sd * 0.8f, 0.8f, -0.2f), new Vector3(sd * 1.08f, 1.5f, -0.2f), 0.45f, pinkIn);
                S(head, new Vector3(sd * 0.55f, 0.3f, -0.36f), new Vector3(0.28f, 0.38f, 0.1f), black);   // eyes
                S(head, new Vector3(sd * 0.95f, -0.25f, -0.32f), new Vector3(0.4f, 0.25f, 0.1f), pinkIn);   // cheeks
                for (int k = 0; k < 2; k++) Rod(head, new Vector3(sd * 0.6f, -0.2f - k * 0.2f, -0.4f), new Vector3(sd * 1.6f, -0.05f - k * 0.35f, -0.4f), 0.04f, white);
            }
            S(head, new Vector3(0, -0.5f, -0.36f), new Vector3(0.95f, 0.85f, 0.15f), M("7A1020", 0.4f));   // singing mouth
            S(head, new Vector3(0, -0.75f, -0.44f), new Vector3(0.5f, 0.25f, 0.06f), pinkIn);
            S(head, new Vector3(0, -0.02f, -0.42f), new Vector3(0.22f, 0.15f, 0.08f), pinkIn);             // nose
            // music notes
            for (int k = 0; k < 3; k++)
            {
                var n = Node("Note", p, new Vector3(k == 1 ? -2.6f : 2.5f, 0.6f + k * 0.5f - (k == 2 ? 1.6f : 0), 0));
                Wig(n, Vector3.zero, new Vector3(0, 0.25f, 0), 3 + k);
                S(n, Vector3.zero, new Vector3(0.45f, 0.35f, 0.1f), black, new Vector3(0, 0, 25));
                Rod(n, new Vector3(0.2f, 0, 0), new Vector3(0.2f, 0.9f, 0), 0.07f, black);
                B(n, new Vector3(0.38f, 0.82f, 0), new Vector3(0.38f, 0.12f, 0.05f), black, new Vector3(0, 0, -20));
            }
        }

        /// <summary>A grey stone face statue with one raised eyebrow, glow rays behind.</summary>
        static void StoneFace(Transform p)
        {
            var stone = M("8E9299", 0.15f); var dark = M("4E525A", 0.2f);
            for (int k = 0; k < 6; k++) B(p, new Vector3(0, 0, 0.05f), new Vector3(7.2f, 0.18f, 0.02f), G("B36BFF", 1.2f), new Vector3(0, 0, k * 30));
            B(p, new Vector3(0, -0.1f, -0.1f), new Vector3(2.2f, 3.8f, 0.8f), stone);
            B(p, new Vector3(0, 1.85f, -0.1f), new Vector3(2.4f, 0.35f, 0.9f), dark);            // flat top
            B(p, new Vector3(-0.55f, 0.7f, -0.55f), new Vector3(0.9f, 0.22f, 0.3f), dark);        // flat brow
            var brow = Node("Brow", p, new Vector3(0.55f, 1.0f, -0.55f));
            B(brow, Vector3.zero, new Vector3(0.9f, 0.22f, 0.3f), dark, new Vector3(0, 0, 18));   // raised brow
            Wig(brow, new Vector3(0, 0, 6), new Vector3(0, 0.12f, 0), 2.2f);
            foreach (var sd in new[] { -1, 1 }) B(p, new Vector3(sd * 0.55f, 0.4f, -0.52f), new Vector3(0.6f, 0.22f, 0.1f), black);
            B(p, new Vector3(0, -0.05f, -0.65f), new Vector3(0.5f, 1.2f, 0.5f), stone);          // nose
            B(p, new Vector3(0, -1.05f, -0.52f), new Vector3(1.1f, 0.12f, 0.1f), dark);          // smug mouth line
            B(p, new Vector3(0, -1.8f, -0.3f), new Vector3(1.6f, 0.5f, 0.6f), stone);            // chin
            B(p, new Vector3(2.6f, 1.2f, -0.2f), new Vector3(0.12f, 0.12f, 0.1f), goldGlow, new Vector3(0, 0, 45)).AddComponent<Pulse>();
        }

        /// <summary>A big grey round-eared cat holding a plate of dumplings.</summary>
        static void DumplingCat(Transform p)
        {
            var grey = M("9AA0A8", 0.25f); var pinkIn = M("FFB0C8", 0.3f); var dough = M("F3E3C0", 0.35f);
            var cat = Node("Cat", p, new Vector3(0, 0.4f, 0));
            Wig(cat, new Vector3(0, 0, 4), new Vector3(0, 0.1f, 0), 2);
            S(cat, Vector3.zero, new Vector3(2.5f, 2.1f, 0.8f), grey);
            foreach (var sd in new[] { -1, 1 })
            {
                S(cat, new Vector3(sd * 0.95f, 1.0f, 0.1f), new Vector3(0.95f, 0.95f, 0.4f), grey);
                S(cat, new Vector3(sd * 0.95f, 1.0f, -0.05f), new Vector3(0.55f, 0.55f, 0.2f), pinkIn);
                S(cat, new Vector3(sd * 0.5f, 0.2f, -0.35f), new Vector3(0.6f, 0.6f, 0.1f), white);
                S(cat, new Vector3(sd * 0.48f, 0.15f, -0.4f), new Vector3(0.32f, 0.36f, 0.08f), black);
                S(cat, new Vector3(sd * 1.3f, -1.7f, -0.55f), new Vector3(0.6f, 0.45f, 0.3f), grey);   // paws holding the plate
            }
            S(cat, new Vector3(0, -0.25f, -0.4f), new Vector3(0.22f, 0.16f, 0.08f), pinkIn);
            S(cat, new Vector3(0, -0.5f, -0.38f), new Vector3(0.4f, 0.12f, 0.06f), black);
            // the plate with dumplings
            S(p, new Vector3(0, -1.55f, -0.6f), new Vector3(3.0f, 0.35f, 1.0f), white);
            for (int k = 0; k < 5; k++) S(p, new Vector3(-1 + k * 0.5f, -1.3f + (k % 2) * 0.12f, -0.75f), new Vector3(0.55f, 0.38f, 0.4f), dough);
            for (int k = 0; k < 3; k++)
            {
                var st = Rod(p, new Vector3(-0.6f + k * 0.6f, -0.9f, -0.8f), new Vector3(-0.5f + k * 0.6f, -0.1f, -0.8f), 0.08f, Glass("FFFFFF", 0.5f));
                st.AddComponent<Bob>().amp = 0.15f;
            }
        }

        /// <summary>A round smiley laughing so hard it cries.</summary>
        static void LaughSmiley(Transform p)
        {
            var face = Node("Face", p, Vector3.zero);
            Wig(face, new Vector3(0, 0, 9), new Vector3(0, 0.12f, 0), 7);
            Cy(face, Vector3.zero, new Vector3(4, 0.08f, 4), G("FFD23A", 0.9f), new Vector3(90, 0, 0));
            foreach (var sd in new[] { -1, 1 })
            {
                // «> <» squeezed eyes
                B(face, new Vector3(sd * 0.7f, 0.65f, -0.1f), new Vector3(0.6f, 0.14f, 0.05f), black, new Vector3(0, 0, sd * 25));
                B(face, new Vector3(sd * 0.7f, 0.42f, -0.1f), new Vector3(0.6f, 0.14f, 0.05f), black, new Vector3(0, 0, -sd * 25));
                var tear = S(face, new Vector3(sd * 1.55f, 0.15f, -0.12f), new Vector3(0.38f, 0.55f, 0.08f), G("5FD3FF", 1.2f));
                tear.AddComponent<Bob>().amp = 0.2f;
            }
            S(face, new Vector3(0, -0.55f, -0.1f), new Vector3(2.2f, 1.3f, 0.06f), M("6B1E1E", 0.4f));   // big laughing mouth
            B(face, new Vector3(0, -0.05f, -0.15f), new Vector3(1.9f, 0.3f, 0.05f), white);               // teeth
            S(face, new Vector3(0, -0.95f, -0.14f), new Vector3(0.9f, 0.4f, 0.05f), M("FF6F91", 0.4f));   // tongue
            var ha = Prim.Text(p, "ХА!", new Vector3(2.7f, 1.6f, -0.1f), 7, Hex("FF4FB0"));
            Wig(ha.transform, new Vector3(0, 0, 15), Vector3.zero, 6);
        }

        /// <summary>A dancing banana with gloves and sneakers.</summary>
        static void Banana(Transform p)
        {
            var yel = M("FFE135", 0.35f); var brown = M("6B4A2A", 0.3f);
            var b = Node("Banana", p, new Vector3(0, -0.2f, 0));
            Wig(b, new Vector3(0, 0, 16), new Vector3(0.25f, 0, 0), 4);
            // a crescent of capsules (centre of the curve at x = 1.6)
            for (int k = 0; k < 5; k++)
            {
                float a = Mathf.Lerp(-55, 55, k / 4f);
                float ar = a * Mathf.Deg2Rad;
                Ca(b, new Vector3(1.6f - 2f * Mathf.Cos(ar), 2f * Mathf.Sin(ar), 0), new Vector3(0.9f, 0.65f, 0.7f), yel, new Vector3(0, 0, -a));
            }
            S(b, new Vector3(0.66f, 1.77f, 0), 0.3f, brown);
            S(b, new Vector3(0.66f, -1.77f, 0), 0.25f, brown);
            foreach (var sd in new[] { -1, 1 })
            {
                S(b, new Vector3(-0.55f + sd * 0.25f, 0.4f, -0.35f), new Vector3(0.32f, 0.4f, 0.1f), white);
                S(b, new Vector3(-0.55f + sd * 0.25f, 0.36f, -0.4f), new Vector3(0.15f, 0.2f, 0.06f), black);
                var arm = Node("Arm", b, new Vector3(-0.2f, -0.2f, -0.2f));
                Wig(arm, new Vector3(0, 0, sd * 30), Vector3.zero, 8);
                Rod(arm, Vector3.zero, new Vector3(sd * 1.1f, 0.7f, 0), 0.08f, black);
                S(arm, new Vector3(sd * 1.15f, 0.75f, 0), 0.32f, white);
                Rod(b, new Vector3(0.1f + sd * 0.2f, -1.3f, -0.1f), new Vector3(0.1f + sd * 0.45f, -2.1f, -0.1f), 0.08f, black);
                S(b, new Vector3(0.1f + sd * 0.5f, -2.15f, -0.2f), new Vector3(0.45f, 0.22f, 0.3f), M("E53935", 0.4f));
            }
            for (int k = 0; k < 3; k++) S(b, new Vector3(-0.75f + k * 0.2f, -0.05f - Mathf.Sin(k / 2f * Mathf.PI) * 0.12f, -0.38f), 0.1f, black);
        }

        /// <summary>A cool duck in sunglasses.</summary>
        static void CoolDuck(Transform p)
        {
            var yel = M("FFD43B", 0.35f); var orange = M("FF8A1E", 0.4f);
            var d = Node("Duck", p, Vector3.zero);
            Wig(d, new Vector3(0, 0, 6), new Vector3(0, 0.1f, 0), 3);
            S(d, new Vector3(-0.2f, -1.0f, 0), new Vector3(2.8f, 1.8f, 0.8f), yel);
            S(d, new Vector3(-0.8f, -0.95f, -0.45f), new Vector3(1.3f, 0.8f, 0.2f), M("F2B92C", 0.35f));   // wing
            var head = Node("Head", d, new Vector3(0.5f, 0.6f, -0.1f));
            Wig(head, new Vector3(0, 0, 8), Vector3.zero, 5);
            S(head, Vector3.zero, new Vector3(1.7f, 1.6f, 0.8f), yel);
            S(head, new Vector3(0.95f, -0.2f, -0.2f), new Vector3(1.1f, 0.38f, 0.5f), orange);
            B(head, new Vector3(-0.1f, 0.2f, -0.45f), new Vector3(0.62f, 0.42f, 0.08f), black);
            B(head, new Vector3(0.6f, 0.2f, -0.4f), new Vector3(0.62f, 0.42f, 0.08f), black);
            B(head, new Vector3(0.25f, 0.3f, -0.45f), new Vector3(0.3f, 0.08f, 0.06f), black);
            B(head, new Vector3(-0.2f, 0.3f, -0.5f), new Vector3(0.18f, 0.06f, 0.02f), white, new Vector3(0, 0, 30));
            B(head, new Vector3(0.5f, 0.3f, -0.45f), new Vector3(0.18f, 0.06f, 0.02f), white, new Vector3(0, 0, 30));
            S(head, new Vector3(-0.1f, 0.85f, 0), new Vector3(0.3f, 0.45f, 0.2f), yel, new Vector3(0, 0, 20));
            foreach (var sd in new[] { -1, 1 }) S(d, new Vector3(-0.2f + sd * 0.6f, -1.95f, -0.2f), new Vector3(0.6f, 0.18f, 0.4f), orange);
        }

        // ---------------- backdrop ----------------

        static void Backdrop()
        {
            var r = new Rng(8888);
            // the purple cloud sea far below
            for (int i = 0; i < 38; i++)
            {
                var p = V(r.F(-95, 95), r.F(-18.5f, -14.5f), r.F(-40, 285));
                S(Root, p, new Vector3(r.F(18, 34), r.F(4, 7), r.F(14, 26)), i % 3 == 0 ? cloudB : i % 3 == 1 ? cloudA : cloudC);
            }
            // distant floating islands on both sides
            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var p = V(side * r.F(50, 90), r.F(-8, 26), r.F(-30, 270));
                int col = r.I(TopHex.Length);
                float rr = r.F(3, 7);
                var t = Node("FarIsland", Root, p);
                Cy(t, new Vector3(0, -0.5f, 0), new Vector3(rr * 2, 0.5f, rr * 2), tops[col]);
                Spike(t, new Vector3(0, -1, 0), new Vector3(0, -1 - rr * 1.4f, 0), rr * 1.9f, rock);
                Ring(t, Vector3.zero, rr, 0.18f, rims[col]);
                if (i % 2 == 0) Lollipop(t, new Vector3(r.F(-1, 1), 0, r.F(-1, 1)) * rr * 0.5f, (col + 1) % rims.Length, 1.6f);
                else Mushroom(t, new Vector3(r.F(-1, 1), 0, r.F(-1, 1)) * rr * 0.5f, (col + 2) % rims.Length, 1.6f);
                var bob = t.gameObject.AddComponent<Bob>(); bob.amp = 0.8f; bob.speed = 0.5f;
            }
            // big golden smiley discs along the route
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float z = 18 + i * 15.5f;
                float y = Mathf.Lerp(0, 19, z / 225f);
                SmileyDisc(V(side * r.F(10, 13), y + r.F(5, 9), z), r.F(2.2f, 3.4f));
            }
            // big glowing meme words in the sky
            Word("МЕМ", V(-32, 24, 45), 70, "FF4FB0");
            Word("ЛОЛ", V(34, 22, 95), 70, "35E6FF");
            Word("ОГО", V(-36, 30, 160), 70, "9BFF3A");
            Word("ВАУ", V(30, 32, 215), 60, "FFE135");
            Word("ИМБА", V(-28, 38, 245), 55, "B36BFF");
            Word("ХА-ХА", V(40, 16, 30), 45, "FFE135");
        }

        static void Word(string s, Vector3 p, float size, string hex)
        {
            var t = Prim.Text(Root, s, p, size, Hex(hex));
            t.fontStyle = FontStyles.Bold;
            t.rectTransform.sizeDelta = new Vector2(40, 10);
            t.gameObject.AddComponent<Billboard>();
            var b = t.gameObject.AddComponent<Bob>(); b.amp = 1.2f; b.speed = 0.7f;
        }

        static void Lollipop(Transform p, Vector3 pos, int col, float s = 1)
        {
            Rod(p, pos, pos + Vector3.up * 1.9f * s, 0.1f * s, white);
            var head = S(p, pos + Vector3.up * 2.1f * s, new Vector3(0.9f, 0.9f, 0.3f) * s, rims[col]);
            head.AddComponent<Spin>().speed = 50;
        }

        static void Mushroom(Transform p, Vector3 pos, int col, float s = 1)
        {
            Cy(p, pos + Vector3.up * 0.35f * s, new Vector3(0.3f, 0.35f, 0.3f) * s, white);
            S(p, pos + Vector3.up * 0.75f * s, new Vector3(1.1f, 0.55f, 1.1f) * s, rims[col]);
        }

        /// <summary>Candy lollipops and glowing mushrooms near the island edges, away from cages/keys/pads.</summary>
        static void IslandDecor()
        {
            var r = new Rng(808);
            foreach (var (c, rad, col) in isles)
            {
                int n = rad >= 7.5f ? 5 : rad >= 5.5f ? 3 : rad >= 4.5f ? 2 : 1;
                float a0 = r.F(0, Mathf.PI * 2);
                for (int k = 0; k < n; k++)
                {
                    float a = a0 + k * Mathf.PI * 2 / n;
                    var pos = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rad * 0.8f;
                    if (!Free(pos) || OnPath(pos - O)) continue;
                    if (k % 2 == 0) Lollipop(Root, pos, (col + 1 + k) % rims.Length);
                    else Mushroom(Root, pos, (col + 3) % rims.Length);
                }
            }
        }

        static bool Free(Vector3 p)
        {
            foreach (var q in clear) { var d = p - q; d.y = 0; if (d.magnitude < 2.6f && Mathf.Abs(p.y - q.y) < 4) return false; }
            return true;
        }

        /// <summary>Keep the decor off the bridge ends (the main line x≈0 and the branch lines).</summary>
        static bool OnPath(Vector3 l) => Mathf.Abs(l.x) < 2.2f
            || (Mathf.Abs(l.z - 53) < 2.2f && l.x < 0 && l.y < 6) || (Mathf.Abs(l.z - 101) < 2.2f && l.x > 0 && l.y < 6)
            || (Mathf.Abs(l.z - 136) < 2.2f && l.x < 0 && l.y > 8 && l.y < 11);

        // ---------------- the end ----------------

        /// <summary>The golden portal was touched: the world is done.</summary>
        public static void Win()
        {
            bool first = !Save.P.done.Contains(Id);
            if (first) { Save.P.done.Add(Id); Save.P.coins += 800; Save.P.tokens += 8; }
            else Save.P.coins += 80;
            Save.Write();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            Effects.Emit("confetti", PortalPos + Vector3.up * 4, 60, 1f, Vector3.up * 4);
            Window.Show("МИР 8 ПРОЙДЕН!", w =>
            {
                w.Text(first ? "Ты прошёл самый мемный мир! Награда: 800 монет и 8 жетонов." : "Мир 8 снова пройден! +80 монет.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться и поиграть", "Пыжик ещё полетает с тобой", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
        }
    }

    /// <summary>Oscillates the local position / rotation around the start pose (billboard pictures, notes, brows).</summary>
    public class W8Wiggle : MonoBehaviour
    {
        public Vector3 rotAmp, posAmp;
        public float speed = 3, phase;
        Vector3 p0; Quaternion r0;
        void Start() { p0 = transform.localPosition; r0 = transform.localRotation; }
        void Update()
        {
            float s = Mathf.Sin(Time.time * speed + phase);
            transform.localPosition = p0 + posAmp * s;
            transform.localRotation = r0 * Quaternion.Euler(rotAmp * s);
        }
    }

    /// <summary>A silly patrolling mob of World 8 (jelly blob / cringe cube / screaming egg): hops between two points;
    /// jump on its head to beat it, touching it from the side costs a heart.</summary>
    public class MemeMob : MonoBehaviour
    {
        public int kind;
        Vector3 a, b;
        float t, dir = 1, back, deadT, hitCd, ph;
        bool dead;
        Transform body;
        const float H = 1.1f, R = 0.6f;

        public static MemeMob Spawn(int kind, Vector3 a, Vector3 b, Transform parent)
        {
            var go = new GameObject("MemeMob" + kind) { layer = Layers.World };
            go.transform.SetParent(parent, false);
            go.transform.position = a;
            var m = go.AddComponent<MemeMob>();
            m.kind = kind; m.a = a; m.b = b; m.ph = Random.value * 10; m.t = Random.value;
            m.Build();
            return m;
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            body = Node("Body", transform, Vector3.zero);
            var eyeW = M("FFFFFF", 0.5f); var eyeB = M("15121F", 0.8f);
            void Eyes(float x, float y, float z, float s)
            {
                foreach (var sd in new[] { -1, 1 })
                {
                    S(body, new Vector3(sd * x, y, z), s * (sd < 0 ? 1f : 1.25f), eyeW);   // googly: one eye bigger
                    S(body, new Vector3(sd * x + sd * 0.03f, y - 0.03f, z + s * 0.4f), s * 0.45f, eyeB);
                }
            }
            switch (kind)
            {
                case 0:   // pink jelly blob with the tongue out
                    S(body, new Vector3(0, 0.5f, 0), new Vector3(1.25f, 1.0f, 1.25f), G("FF5FB8", 0.5f));
                    Eyes(0.22f, 0.72f, 0.48f, 0.34f);
                    S(body, new Vector3(0.1f, 0.35f, 0.6f), new Vector3(0.25f, 0.3f, 0.12f), M("FF2A6A", 0.4f));
                    break;
                case 1:   // grumpy «cringe» cube
                    B(body, new Vector3(0, 0.55f, 0), new Vector3(1.0f, 0.95f, 1.0f), M("35C8FF", 0.4f));
                    Eyes(0.22f, 0.7f, 0.5f, 0.3f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        B(body, new Vector3(sd * 0.22f, 0.92f, 0.52f), new Vector3(0.32f, 0.07f, 0.05f), eyeB, new Vector3(0, 0, sd * -20));
                        B(body, new Vector3(sd * 0.25f, 0.05f, 0), new Vector3(0.25f, 0.12f, 0.4f), M("1E1240", 0.5f));
                    }
                    B(body, new Vector3(0, 0.33f, 0.51f), new Vector3(0.4f, 0.06f, 0.04f), eyeB, new Vector3(0, 0, 8));
                    break;
                default:  // screaming egg
                    S(body, new Vector3(0, 0.62f, 0), new Vector3(0.9f, 1.25f, 0.9f), M("FFF6E0", 0.4f));
                    Eyes(0.17f, 0.85f, 0.36f, 0.26f);
                    S(body, new Vector3(0, 0.45f, 0.4f), new Vector3(0.28f, 0.36f, 0.1f), eyeB);
                    foreach (var sd in new[] { -1, 1 }) S(body, new Vector3(sd * 0.2f, 0.04f, 0.05f), new Vector3(0.28f, 0.1f, 0.4f), M("FF8A1E", 0.4f));
                    break;
            }
            Place();
        }

        void Place()
        {
            transform.position = Vector3.Lerp(a, b, t);
            var fwd = (b - a) * dir; fwd.y = 0;
            if (fwd.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(fwd);
        }

        void Update()
        {
            if (Worlds.Current != World8.Id) return;
            if (dead)
            {
                deadT += Time.deltaTime;
                body.localScale = new Vector3(1 + deadT * 2, Mathf.Max(0.05f, 1 - deadT * 5), 1 + deadT * 2);
                if (deadT > 0.35f && body.gameObject.activeSelf) body.gameObject.SetActive(false);
                if (Time.time > back) { dead = false; deadT = 0; body.gameObject.SetActive(true); body.localScale = Vector3.one; Effects.Emit("smoke", transform.position + Vector3.up * 0.5f, 10, 0.5f); }
                return;
            }
            var p = PlayerCtl.I;
            float near = 1;
            if (p != null) { var dd = p.transform.position - transform.position; if (Mathf.Abs(dd.y) < 2 && new Vector2(dd.x, dd.z).magnitude < 5) near = 1.8f; }
            float len = Mathf.Max(0.5f, Vector3.Distance(a, b));
            t += dir * 1.6f * near * Time.deltaTime / len;
            if (t > 1) { t = 1; dir = -1; } else if (t < 0) { t = 0; dir = 1; }
            Place();
            float h = Mathf.Abs(Mathf.Sin(Time.time * 6 + ph));
            body.localPosition = Vector3.up * h * 0.3f;
            float sq = 1 - h;
            body.localScale = new Vector3(1 + sq * 0.15f, 1 - sq * 0.18f, 1 + sq * 0.15f);

            if (p == null || p.Frozen) return;
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            float pr = 0.38f * p.Model.SizeScale;
            if (horiz < R + pr + 0.25f && d.y > H * 0.45f && d.y < H + 0.8f && p.Velocity.y < 1f)
            {
                p.Launch(12f);
                AudioFx.Play("boing");
                Die();
            }
            else if (horiz < R + pr && d.y > -0.6f && d.y < H * 0.45f && Time.time > hitCd)
            {
                hitCd = Time.time + 1f;
                p.HurtBy(transform.position);
            }
        }

        void Die()
        {
            dead = true; deadT = 0; back = Time.time + 25f;
            AudioFx.Play("bonk");
            var c = transform.position + Vector3.up * H * 0.6f;
            Effects.Emit("star", c, 16, 0.4f);
            Effects.Emit("confetti", c, 14, 0.5f);
            Effects.Emit("coin", c, 8, 0.3f, Vector3.up * 2);
            Save.AddCoins(5);
            Effects.Float("+5", c + Vector3.up, UIKit.Gold);
        }
    }

    /// <summary>«Пыжик» — a little light-blue cube robot with a red blinking antenna, two eye screens and thrusters.
    /// Flies next to the player in World 8 only (smooth lag, bobbing, looks where the player goes) and now and then
    /// says a short funny hint (trampoline nearby, where a key is, how to beat a mob…).</summary>
    public class Pyzhik : MonoBehaviour
    {
        Transform vis, body, ball, eyeL, eyeR;
        Renderer ballR;
        Material ballOn, ballOff;
        Vector3 vel, pointAt;
        float nextHint, pointUntil, blinkAt, greetAt = -1;
        bool greeted, ballLit;
        int jokeIdx;

        static readonly string[] Jokes =
        {
            "Пыжик: я не робот-пылесос, я робот-помогатор!",
            "Пыжик: этот мир такой мемный, что у меня антенна смеётся!",
            "Пыжик: собирай смайлики-монетки!",
            "Пыжик: кря! Ой, это не я, это утка на плакате.",
            "Пыжик: жми прыжок два раза — будет двойной прыжок!",
            "Пыжик: флажки восстанавливают сердечки!",
            "Пыжик: друзья в клетках ждут ключи. Спасём всех!",
            "Пыжик: в конце нас ждёт золотой портал!",
        };

        public void Build(Transform parent)
        {
            Parts.Layer = Layers.World;
            transform.SetParent(parent, false);
            transform.position = World8.Spawn + new Vector3(1.2f, 2, -1);
            vis = Node("Vis", transform, Vector3.zero);
            body = Node("Body", vis, Vector3.zero);
            var blue = M("8FD8FF", 0.6f, 0.1f); var dark = M("1B2A44", 0.8f); var metal = M("C9D1DB", 0.8f, 0.7f);
            var screen = G("5CFFE0", 2.2f);
            B(body, Vector3.zero, new Vector3(0.55f, 0.5f, 0.5f), blue);
            B(body, new Vector3(0, 0.27f, 0), new Vector3(0.45f, 0.06f, 0.42f), M("D8F1FF", 0.6f));   // lid
            foreach (var sd in new[] { -1, 1 })
            {
                B(body, new Vector3(sd * 0.12f, 0.05f, 0.255f), new Vector3(0.18f, 0.18f, 0.03f), dark);   // eye screens
                var e = B(body, new Vector3(sd * 0.12f, 0.05f, 0.275f), new Vector3(0.08f, 0.11f, 0.02f), screen).transform;
                if (sd < 0) eyeL = e; else eyeR = e;
                Cy(body, new Vector3(sd * 0.29f, 0, 0), new Vector3(0.12f, 0.03f, 0.12f), metal, new Vector3(0, 0, 90));   // side bolts
                S(body, new Vector3(sd * 0.36f, -0.1f, 0.05f), 0.13f, blue);                                        // little hands
                Cy(body, new Vector3(sd * 0.15f, -0.3f, 0), new Vector3(0.13f, 0.06f, 0.13f), metal);              // thrusters
                var fl = Spike(body, new Vector3(sd * 0.15f, -0.36f, 0), new Vector3(sd * 0.15f, -0.62f, 0), 0.12f, Add("FF9A2E", 0.85f));
                fl.AddComponent<Pulse>().amount = 0.25f;
            }
            B(body, new Vector3(0, -0.09f, 0.27f), new Vector3(0.13f, 0.03f, 0.02f), screen);                    // smile
            Rod(body, new Vector3(0, 0.27f, 0), new Vector3(0, 0.52f, 0), 0.035f, metal);
            ballOn = G("FF2A2A", 2.6f); ballOff = M("7A1010", 0.5f);
            ball = S(body, new Vector3(0, 0.56f, 0), 0.12f, ballOn).transform;
            ballR = ball.GetComponent<Renderer>(); ballLit = true;
            nextHint = Time.time + 18;
        }

        void LateUpdate()
        {
            var p = PlayerCtl.I;
            bool here = p != null && Worlds.Current == World8.Id;
            if (vis.gameObject.activeSelf != here) vis.gameObject.SetActive(here);
            if (!here) { greeted = false; greetAt = -1; return; }
            var pt = p.transform;

            // follow: a bit to the right of and above the player, with a smooth lag
            var fwd = pt.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            var want = pt.position + right * 1.3f + Vector3.up * 2.1f - fwd * 0.5f;
            bool pointing = Time.time < pointUntil;
            if (pointing)
            {
                var to = pointAt - pt.position;
                want = pt.position + Vector3.ClampMagnitude(to, 3.5f) + Vector3.up * 2.4f;
            }
            if ((transform.position - want).sqrMagnitude > 30 * 30) { transform.position = want; vel = Vector3.zero; }
            transform.position = Vector3.SmoothDamp(transform.position, want, ref vel, 0.35f);
            vis.localPosition = Vector3.up * Mathf.Sin(Time.time * 3.1f) * 0.12f;

            // look where the player goes (or at the hint target), lean with the speed
            var look = pointing ? pointAt - transform.position : new Vector3(p.Velocity.x, 0, p.Velocity.z);
            look.y = 0;
            if (look.sqrMagnitude < 0.25f) look = fwd;
            var lean = Quaternion.Euler(Mathf.Clamp(vel.magnitude * 3, 0, 18), 0, Mathf.Sin(Time.time * 2) * 5);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized) * lean, Time.deltaTime * 5);

            // antenna blink, eye blink
            bool lit = Mathf.Repeat(Time.time, 1.2f) < 0.6f;
            if (lit != ballLit) { ballLit = lit; ballR.sharedMaterial = lit ? ballOn : ballOff; }
            float eyeY = Mathf.Repeat(Time.time, 3.7f) < 0.12f ? 0.15f : 1f;
            eyeL.localScale = eyeR.localScale = new Vector3(0.08f, 0.11f * eyeY, 0.02f);

            // hints
            if (!greeted)
            {
                if (greetAt < 0) greetAt = Time.time + 2.5f;
                if (Time.time > greetAt) { greeted = true; Say("Пыжик: привет! Я робот Пыжик, полечу с тобой!"); nextHint = Time.time + 15; }
                return;
            }
            if (Time.time > nextHint && UIRoot.Open == null) { nextHint = Time.time + Random.Range(20f, 30f); Hint(pt.position); }
        }

        void Hint(Vector3 me)
        {
            foreach (var pad in World8.PadSpots)
                if ((pad - me).magnitude < 8 && Mathf.Abs(pad.y - me.y) < 2) { Point(pad); Say("Пыжик: прыгай на розовый батут!"); return; }
            foreach (var m in World8.Mobs)
                if (m != null && (m.transform.position - me).magnitude < 9) { Point(m.transform.position); Say("Пыжик: прыгни ему на голову — бум!"); return; }
            float best = 32; Vector3? key = null;
            foreach (var (id, pos) in World8.KeySpots)
                if (!Save.P.found.Contains(id) && (pos - me).magnitude < best) { best = (pos - me).magnitude; key = pos; }
            if (key.HasValue)
            {
                Point(key.Value);
                Say(key.Value.y > me.y + 3 ? "Пыжик: ключ наверху! Ищи батут!" : "Пыжик: ключ на том острове! Я покажу.");
                return;
            }
            if ((World8.PortalPos - me).magnitude < 25) { Point(World8.PortalPos); Say("Пыжик: вон золотой портал! Беги туда!"); return; }
            Say(Jokes[jokeIdx++ % Jokes.Length]);
        }

        void Point(Vector3 at) { pointAt = at; pointUntil = Time.time + 3f; }

        static void Say(string s) => Hud.Toast(s, UIKit.Cyan);
    }
}
