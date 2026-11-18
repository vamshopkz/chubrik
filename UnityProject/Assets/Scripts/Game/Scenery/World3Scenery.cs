using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 3 «Кристальная пещера» dressed like its concept art: floating dark-stone islands with purple / cyan crystals,
    /// giant crystals, wooden mine scaffolds and cart rails, rope bridges and ladders, chains from the ceiling, stalactites,
    /// glowing waterfalls and lava falls. Plus a playable side area west of island 1 («Старая шахта»): a rope bridge,
    /// a mine-camp island, stepping stones, a crystal grove island and an icy crystal bridge to the last island,
    /// with 4 more caged friends (w3c8..w3c11) and their keys (w3k8..w3k11).
    /// </summary>
    public static class World3Scenery
    {
        static Transform R;
        static Material rock, rockTop, rockDark, wood, plank, beam, rope, iron, rust;
        static Material cryA, cryB, cryC, cryPink, halo, haloC, lavaGlow, lavaFall, lavaCore, water, waterCore, foam, ice, iceGlow, lamp;
        static Texture2D fallTex;

        static Vector3 V(float x, float y, float z) => World3.O + new Vector3(x, y, z);
        static float LavaY => World3.LavaY;

        public static void Build()
        {
            if (World3.Root == null) return;
            R = World3.Root;
            Parts.Layer = Layers.World;
            int made0 = Parts.Made;

            // materials: created once
            rock = Mats.Tex(Tex.Concrete(), Hex("5E5478"), 0.25f);
            rockTop = Mats.Tex(Tex.Concrete(), Hex("8A80A6"), 0.3f);
            rockDark = M("2A2338", 0.2f);
            wood = Lobby.Tiled(Tex.Planks("7A4A24"), 1);
            plank = M("8A5A30", 0.2f);
            beam = M("5A3A22", 0.2f);
            rope = M("C8A26A", 0.1f);
            iron = M("4A4A58", 0.55f, 0.7f);
            rust = M("6A4A3A", 0.35f, 0.4f);
            cryA = G("6FA8FF", 1.6f); cryB = G("A66BFF", 1.7f); cryC = G("5FF2FF", 1.8f); cryPink = G("FF7AE0", 1.6f);
            halo = Add("A66BFF", 0.07f); haloC = Add("5FF2FF", 0.07f);
            lavaGlow = G("FF7A1E", 2.4f); lavaCore = G("FFB040", 2.2f);
            fallTex = FallTex();
            lavaFall = Flowing(Mats.Additive(Hex("FF6A10"), fallTex), 0.9f, 4);
            water = Flowing(Mats.Additive(Hex("6FE8FF"), fallTex), 0.7f, 5);
            waterCore = Add("CFFBFF", 0.35f);
            foam = Add("BFF6FF", 0.3f);
            ice = Glass("9FEFFF", 0.55f); iceGlow = G("8FF4FF", 1.4f);
            lamp = G("FFC86A", 2.2f);

            SideArea();
            Backdrop();

            Debug.Log($"[World3Scenery] parts: {Parts.Made - made0}");
        }

        // =====================================================================================
        // the playable side area «Старая шахта» west of island 1
        // =====================================================================================

        static void SideArea()
        {
            // island A: the mine camp
            var a = V(-19, 0.6f, 30);
            Isle(a, 5, 1);
            RopeBridge(V(-5.7f, 0, 28), V(-14.5f, 0.6f, 29.3f), 2.4f, true);
            Lantern(V(-14.2f, 0.6f, 27.6f)); Lantern(V(-14.2f, 0.6f, 31f));
            // scaffold with a deck (key on top) and a crate step
            Scaffold(V(-21.2f, 2.3f, 27.4f), 2.4f);
            var crate = Lobby.Solid(PrimitiveType.Cube, V(-19.2f, 1.05f, 26.8f), new Vector3(1, 0.9f, 1), wood, new Vector3(0, 12, 0), R);
            B(crate.transform, Vector3.zero, new Vector3(1.04f, 0.14f, 1.04f), beam);
            Pickups.I.Key("w3k9", V(-21.2f, 3.4f, 27.4f));
            // cart rails across the island with a cart full of crystals
            Rails(V(-22.8f, 0.6f, 33), V(-15.2f, 0.6f, 33), 0.8f);
            Cart(V(-21, 0.6f, 33), 90, true);
            // a wooden mine shed with logs on the west rim
            Shed(V(-23.2f, 0.6f, 30.5f), 90);
            Cluster(V(-16, 0.6f, 26.4f), 4, 0.8f, 11, true);
            Rescue.SpawnCage("w3c8", V(-17.2f, 0.6f, 30.6f), 90, R);

            // stepping stones up to the crystal grove
            Stone(V(-21, 1.4f, 37), 1.3f, 2);
            Stone(V(-23.5f, 2.2f, 40.5f), 1.3f, 3);
            Pickups.I.Key("w3k8", V(-23.5f, 3.3f, 40.5f));

            // island B: the crystal grove
            var b = V(-28, 3, 47);
            Isle(b, 5, 4);
            GiantCluster(V(-28.6f, 3, 46.6f), 0.3f, cryB, 41);
            Lobby.Solid(PrimitiveType.Cylinder, V(-28.6f, 4, 46.6f), new Vector3(2f, 1, 2f), rockDark, default, R);
            // a rock pillar with a key
            Lobby.Solid(PrimitiveType.Cylinder, V(-30.6f, 3.75f, 49.6f), new Vector3(1.6f, 0.75f, 1.6f), rock, default, R);
            Cy(R, V(-30.6f, 4.52f, 49.6f), new Vector3(1.3f, 0.02f, 1.3f), rockTop);
            Spike(R, V(-31.3f, 4.2f, 49.9f), V(-31.9f, 5.4f, 50.3f), 0.35f, cryC);
            Pickups.I.Key("w3k10", V(-30.6f, 5.6f, 49.6f));
            Cluster(V(-31.8f, 3, 47), 4, 0.8f, 12, true);
            Cluster(V(-24.2f, 3, 45.6f), 3, 0.7f, 13, false);
            Rescue.SpawnCage("w3c9", V(-30.6f, 3, 44.8f), 45, R);
            Rescue.SpawnCage("w3c10", V(-25f, 3, 49f), -120, R);

            // the icy crystal bridge to island C
            IceBridge(V(-27.2f, 3, 51.6f), V(-25.7f, 3, 59.0f), 2.4f);

            // island C: the old mine entrance
            var c = V(-25, 3, 63);
            Isle(c, 4.5f, 5);
            MineEntrance(V(-23.2f, 3, 67.0f), 180);
            Lobby.Solid(PrimitiveType.Cube, V(-27.6f, 3.75f, 63.6f), new Vector3(1.6f, 1.5f, 1.6f), wood, default, R);
            Lobby.Solid(PrimitiveType.Cube, V(-26.2f, 4.5f, 65.9f), new Vector3(1.8f, 3f, 1.8f), rock, new Vector3(0, 8, 0), R);
            Cy(R, V(-26.2f, 6.02f, 65.9f), new Vector3(1.5f, 0.02f, 1.5f), rockTop, new Vector3(0, 8, 0));
            Spike(R, V(-25.3f, 5.4f, 66.6f), V(-24.7f, 6.9f, 67.2f), 0.4f, cryB);
            Pickups.I.Key("w3k11", V(-26.2f, 7.1f, 65.9f));
            Cluster(V(-21.6f, 3, 64.6f), 4, 0.8f, 14, true);
            Lantern(V(-26.4f, 3, 59.4f)); Lantern(V(-23.6f, 3, 59.7f));
            Rescue.SpawnCage("w3c11", V(-22.4f, 3, 62.2f), -90, R);

            // coins
            for (int i = 1; i <= 4; i++) Pickups.I.Coin(Vector3.Lerp(V(-5.7f, 0, 28), V(-14.5f, 0.6f, 29.3f), i * 0.2f) + Vector3.up * 0.9f);
            Pickups.I.Coin(V(-18.5f, 1.5f, 28.4f)); Pickups.I.Coin(V(-20.2f, 1.5f, 31.4f)); Pickups.I.Coin(V(-15.8f, 1.5f, 29));
            Pickups.I.Coin(V(-21, 2.3f, 37));
            Pickups.I.Coin(V(-27.6f, 3.9f, 43.6f)); Pickups.I.Coin(V(-25.8f, 3.9f, 46.4f)); Pickups.I.Coin(V(-26.2f, 3.9f, 44.4f)); Pickups.I.Coin(V(-27, 3.9f, 50.2f));
            Pickups.I.Coin(Vector3.Lerp(V(-27.2f, 3, 51.6f), V(-25.7f, 3, 59), 0.35f) + Vector3.up * 0.9f);
            Pickups.I.Coin(Vector3.Lerp(V(-27.2f, 3, 51.6f), V(-25.7f, 3, 59), 0.7f) + Vector3.up * 0.9f);
            Pickups.I.Coin(V(-24, 3.9f, 61)); Pickups.I.Coin(V(-23.4f, 3.9f, 64.6f)); Pickups.I.Coin(V(-27.6f, 5.4f, 63.6f), 5);

            // soft cyan light over the side area
            PointLight(V(-25, 9, 47), "6FD8FF", 28, 1.6f);
        }

        /// <summary>Walkable floating island: stone disc with a collider, rocky cone underneath, crystals on the rim.</summary>
        static void Isle(Vector3 top, float r, int seed)
        {
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * 0.7f, new Vector3(r * 2, 0.7f, r * 2), rock, default, R);
            Cy(R, top + Vector3.up * 0.02f, new Vector3(r * 2 - 0.8f, 0.02f, r * 2 - 0.8f), rockTop);
            Msh(Meshes.Cone(8), R, top - Vector3.up * 1.4f, new Vector3(r * 2.1f, r * 1.6f + 3, r * 2.1f), rockDark, new Vector3(180, seed * 17, 0));
            var rng = new Rng(seed * 13 + 5);
            // hanging rock teeth and crystals under the island
            for (int k = 0; k < 3; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                var p = top + new Vector3(Mathf.Cos(a) * r * 0.55f, -1.6f, Mathf.Sin(a) * r * 0.55f);
                Msh(Meshes.Cone(5), R, p, new Vector3(1.6f, rng.F(2.5f, 4.5f), 1.6f), rockDark, new Vector3(180, 0, 0));
                Spike(R, p + new Vector3(0, -0.6f, 0), p + new Vector3(Mathf.Cos(a) * 1.4f, -2.2f, Mathf.Sin(a) * 1.4f), 0.5f, k == 1 ? cryC : cryB);
            }
            // small crystals sticking out of the rim (outside the walkable disc)
            for (int k = 0; k < 4; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Spike(R, top + d * (r - 0.1f) - Vector3.up * 0.5f, top + d * (r + 0.9f) + Vector3.up * 0.4f, 0.4f, k % 2 == 0 ? cryA : cryPink);
            }
        }

        static void Stone(Vector3 top, float r, int seed)
        {
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * 0.5f, new Vector3(r * 2, 0.5f, r * 2), rock, default, R);
            Cy(R, top + Vector3.up * 0.02f, new Vector3(r * 2 - 0.5f, 0.02f, r * 2 - 0.5f), rockTop);
            Msh(Meshes.Cone(6), R, top - Vector3.up * 1f, new Vector3(r * 2.1f, 3.2f, r * 2.1f), rockDark, new Vector3(180, seed * 23, 0));
            Spike(R, top + new Vector3(r - 0.2f, -0.6f, 0), top + new Vector3(r + 0.6f, -2f, 0.3f), 0.4f, seed % 2 == 0 ? cryC : cryB);
        }

        /// <summary>Rope bridge with planks (walkable when solid): a box collider deck + side rails like the stone walkways.</summary>
        static void RopeBridge(Vector3 a, Vector3 b, float w, bool solid)
        {
            var d = b - a;
            float len = d.magnitude;
            var go = new GameObject("RopeBridge") { layer = Layers.World };
            go.transform.SetParent(R, false);
            go.transform.SetPositionAndRotation((a + b) / 2, Quaternion.LookRotation(d.normalized));
            var t = go.transform;
            if (solid)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0, -0.15f, 0); col.size = new Vector3(w, 0.3f, len + 0.6f);
                foreach (var sd in new[] { -1, 1 })
                {
                    var rail = go.AddComponent<BoxCollider>();
                    rail.center = new Vector3(sd * (w / 2 + 0.1f), 0.6f, 0); rail.size = new Vector3(0.2f, 1.2f, len - 1f);
                }
            }
            int n = Mathf.CeilToInt(len / 0.55f);
            for (int i = 0; i < n; i++)
            {
                float z = -len / 2 + (i + 0.5f) * len / n;
                B(t, new Vector3(0, -0.07f, z), new Vector3(w, 0.1f, len / n - 0.1f), plank, new Vector3(0, (i % 3 - 1) * 2.5f, 0));
            }
            foreach (var sd in new[] { -1, 1 })
            {
                float x = sd * (w / 2 + 0.05f);
                // posts, hand rope, rope under the planks
                Cy(t, new Vector3(x, 0.5f, -len / 2), new Vector3(0.16f, 0.6f, 0.16f), beam);
                Cy(t, new Vector3(x, 0.5f, len / 2), new Vector3(0.16f, 0.6f, 0.16f), beam);
                Rod(t, new Vector3(x, 0.95f, -len / 2), new Vector3(x, 0.95f, len / 2), 0.06f, rope);
                Rod(t, new Vector3(x, -0.13f, -len / 2), new Vector3(x, -0.13f, len / 2), 0.06f, rope);
                int h = Mathf.Max(2, Mathf.RoundToInt(len / 1.6f));
                for (int i = 1; i < h; i++)
                {
                    float z = -len / 2 + i * len / h;
                    LRod(t, new Vector3(x, -0.1f, z), new Vector3(x, 0.95f, z), 0.04f, rope);
                }
            }
        }

        /// <summary>Icy crystal bridge: a frosted glass deck with glowing edges and icicles hanging below.</summary>
        static void IceBridge(Vector3 a, Vector3 b, float w)
        {
            var d = b - a;
            float len = d.magnitude;
            var go = new GameObject("IceBridge") { layer = Layers.World };
            go.transform.SetParent(R, false);
            go.transform.SetPositionAndRotation((a + b) / 2, Quaternion.LookRotation(d.normalized));
            var t = go.transform;
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.2f, 0); col.size = new Vector3(w, 0.4f, len + 0.6f);
            foreach (var sd in new[] { -1, 1 })
            {
                var rail = go.AddComponent<BoxCollider>();
                rail.center = new Vector3(sd * (w / 2 + 0.1f), 0.6f, 0); rail.size = new Vector3(0.2f, 1.2f, len - 1f);
            }
            B(t, new Vector3(0, -0.2f, 0), new Vector3(w, 0.4f, len + 0.4f), ice);
            B(t, new Vector3(0, -0.42f, 0), new Vector3(w * 0.6f, 0.06f, len), iceGlow);
            var rng = new Rng(909);
            foreach (var sd in new[] { -1, 1 })
            {
                B(t, new Vector3(sd * (w / 2 - 0.05f), 0.02f, 0), new Vector3(0.1f, 0.06f, len + 0.4f), iceGlow);
                for (int i = 0; i < 4; i++)
                {
                    float z = -len / 2 + (i + 0.5f) * len / 4;
                    Spike(t, new Vector3(sd * (w / 2), 0, z), new Vector3(sd * (w / 2 + 0.25f), rng.F(0.6f, 1.1f), z + 0.15f), 0.28f, i % 2 == 0 ? cryC : cryA);
                }
            }
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(rng.F(-w / 2, w / 2), -0.4f, -len / 2 + (i + 0.5f) * len / 9);
                Spike(t, p, p + Vector3.down * rng.F(0.5f, 1.6f), 0.3f, ice);
            }
        }

        /// <summary>Wooden mine scaffold: a walkable deck on four legs with cross braces and a little ladder.</summary>
        static void Scaffold(Vector3 top, float s)
        {
            Lobby.Solid(PrimitiveType.Cube, top - Vector3.up * 0.13f, new Vector3(s, 0.26f, s), wood, default, R);
            float h = 1.7f, e = s / 2 - 0.15f;
            for (int k = 0; k < 4; k++)
            {
                var p = top + new Vector3(k < 2 ? -e : e, 0, k % 2 == 0 ? -e : e);
                B(R, p - Vector3.up * (h / 2 + 0.1f), new Vector3(0.22f, h + 0.2f, 0.22f), beam);
            }
            Rod(R, top + new Vector3(-e, -0.3f, -e), top + new Vector3(e, -h, -e), 0.1f, beam);
            Rod(R, top + new Vector3(-e, -h, e), top + new Vector3(-e, -0.3f, -e), 0.1f, beam);
            // railing on the back sides and a lantern
            B(R, top + new Vector3(-e, 0.55f, 0), new Vector3(0.1f, 0.1f, s), beam);
            B(R, top + new Vector3(-e, 0.3f, -e), new Vector3(0.12f, 0.6f, 0.12f), beam);
            B(R, top + new Vector3(-e, 0.3f, e), new Vector3(0.12f, 0.6f, 0.12f), beam);
            Ls(R, top + new Vector3(-e, 0.75f, 0), 0.3f, lamp);
        }

        static void Lantern(Vector3 floor)
        {
            Cy(R, floor + Vector3.up * 0.8f, new Vector3(0.12f, 0.8f, 0.12f), beam);
            B(R, floor + Vector3.up * 1.62f, new Vector3(0.4f, 0.06f, 0.06f), beam);
            Ls(R, floor + new Vector3(0.18f, 1.38f, 0), 0.3f, lamp);
            Cone(R, floor + new Vector3(0.18f, 1.52f, 0), new Vector3(0.32f, 0.16f, 0.32f), iron);
        }

        /// <summary>Two rails with ties (decoration, lies on the ground).</summary>
        static void Rails(Vector3 a, Vector3 b, float gauge)
        {
            var d = b - a; float len = d.magnitude;
            var t = Node("Rails", R, (a + b) / 2, Quaternion.LookRotation(d.normalized).eulerAngles);
            foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * gauge / 2, 0.11f, 0), new Vector3(0.08f, 0.08f, len), iron);
            int n = Mathf.CeilToInt(len / 0.9f);
            for (int i = 0; i < n; i++) B(t, new Vector3(0, 0.04f, -len / 2 + (i + 0.5f) * len / n), new Vector3(gauge + 0.4f, 0.07f, 0.22f), beam);
        }

        /// <summary>Mine cart with glowing crystals inside.</summary>
        static void Cart(Vector3 floor, float yaw, bool solid)
        {
            var t = Node("Cart", R, floor, new Vector3(0, yaw, 0));
            if (solid) Lobby.Solid(PrimitiveType.Cube, floor + Vector3.up * 0.65f, new Vector3(0.95f, 0.7f, 1.4f), rust, new Vector3(0, yaw, 0), R);
            else B(t, new Vector3(0, 0.65f, 0), new Vector3(0.95f, 0.7f, 1.4f), rust);
            B(t, new Vector3(0, 1.02f, 0), new Vector3(1.05f, 0.08f, 1.5f), iron);
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    Cy(t, new Vector3(sx * 0.42f, 0.22f, sz * 0.45f), new Vector3(0.38f, 0.04f, 0.38f), iron, new Vector3(0, 0, 90));
            Spike(t, new Vector3(-0.15f, 0.9f, -0.2f), new Vector3(-0.3f, 1.6f, -0.35f), 0.35f, cryB);
            Spike(t, new Vector3(0.15f, 0.9f, 0.15f), new Vector3(0.25f, 1.5f, 0.35f), 0.3f, cryC);
            Spike(t, new Vector3(0, 0.9f, 0.4f), new Vector3(-0.1f, 1.35f, 0.55f), 0.25f, cryPink);
        }

        /// <summary>Wooden mine shed: two posts, a roof beam, braces and a pile of logs (decoration).</summary>
        static void Shed(Vector3 floor, float yaw)
        {
            var t = Node("Shed", R, floor, new Vector3(0, yaw, 0));
            foreach (var sd in new[] { -1, 1 })
            {
                B(t, new Vector3(sd * 1.3f, 1.3f, 0), new Vector3(0.25f, 2.6f, 0.25f), beam);
                Rod(t, new Vector3(sd * 1.3f, 1.8f, 0), new Vector3(sd * 0.5f, 2.55f, 0), 0.12f, beam);
            }
            B(t, new Vector3(0, 2.7f, 0), new Vector3(3.2f, 0.3f, 0.4f), beam);
            B(t, new Vector3(0, 2.95f, -0.4f), new Vector3(3.4f, 0.12f, 1.4f), plank, new Vector3(-15, 0, 0));
            for (int i = 0; i < 3; i++)
                Cy(t, new Vector3(0, 0.22f + (i == 2 ? 0.36f : 0), -0.6f + (i == 1 ? 0.42f : 0) + (i == 2 ? 0.2f : 0)), new Vector3(0.4f, 1, 0.4f), beam, new Vector3(0, 0, 90));
        }

        /// <summary>Old mine entrance: a timber frame against a dark rock face with a cart on the rails.</summary>
        static void MineEntrance(Vector3 floor, float yaw)
        {
            var t = Node("MineEntrance", R, floor, new Vector3(0, yaw, 0));
            Msh(Meshes.Cone(6), t, new Vector3(0, -0.5f, -1.5f), new Vector3(6, 6, 3.5f), rockDark, new Vector3(0, 30, 0));
            B(t, new Vector3(0, 1.3f, -0.55f), new Vector3(2.2f, 2.6f, 0.1f), M("0E0A16", 0.1f));
            foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 1.2f, 1.4f, -0.4f), new Vector3(0.28f, 2.8f, 0.28f), beam);
            B(t, new Vector3(0, 2.85f, -0.4f), new Vector3(3, 0.32f, 0.36f), beam);
            Ls(t, new Vector3(0, 2.55f, -0.1f), 0.32f, lamp);
            Rails(floor + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -0.5f), floor + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 2.4f), 0.8f);
        }

        /// <summary>A cluster of glowing crystals: tilted spikes (optionally with a soft glow halo).</summary>
        static void Cluster(Vector3 c, int n, float size, int seed, bool glow)
        {
            var r = new Rng(seed * 31 + 3);
            for (int k = 0; k < n; k++)
            {
                var d = new Vector3(r.F(-1, 1), 0, r.F(-1, 1)) * size * 0.6f;
                float h = size * r.F(0.9f, 2.2f);
                var tilt = Quaternion.Euler(r.F(-25, 25), r.F(0, 360), r.F(-25, 25));
                var m = r.I(4) switch { 0 => cryA, 1 => cryB, 2 => cryPink, _ => cryC };
                Spike(R, c + d - Vector3.up * 0.1f, c + d + tilt * Vector3.up * h, size * r.F(0.3f, 0.5f), m);
            }
            if (glow) Ls(R, c + Vector3.up * size * 0.7f, size * 2.4f, seed % 2 == 0 ? halo : haloC);
        }

        /// <summary>Big crystal bouquet: a tall centre crystal and a ring of tilted ones (scale k: 1 = giant backdrop).</summary>
        static void GiantCluster(Vector3 c, float k, Material main, int seed)
        {
            var r = new Rng(seed);
            Spike(R, c, c + new Vector3(r.F(-0.1f, 0.1f), 1, r.F(-0.1f, 0.1f)) * 30 * k, 8 * k, main);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2 / 7 + r.F(-0.3f, 0.3f);
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var b = c + d * 3.5f * k;
                var m = i % 3 == 0 ? cryC : i % 3 == 1 ? main : cryA;
                Spike(R, b, b + (d * r.F(0.4f, 0.7f) + Vector3.up).normalized * r.F(12, 22) * k, r.F(3.5f, 5.5f) * k, m);
            }
            Ls(R, c + Vector3.up * 12 * k, 34 * k, main == cryC ? haloC : halo).AddComponent<Pulse>().amount = 0.03f;
        }

        // =====================================================================================
        // backdrop: everything far from the route, no colliders
        // =====================================================================================

        static void Backdrop()
        {
            // three giant crystals: the big purple one behind the Guardian, a pink one east, a cyan one west
            GiantCluster(new Vector3(World3.O.x, LavaY, World3.O.z + 158), 1.2f, cryB, 1);
            GiantCluster(V(42, LavaY - World3.O.y, 52), 0.8f, cryPink, 2);
            GiantCluster(V(-60, LavaY - World3.O.y, 76), 0.9f, cryC, 3);
            PointLight(V(0, 12, 148), "B47CFF", 48, 2.6f);

            // floating dark-stone islands with crystals
            FloatIsle(V(-55, 8, 22), 4, 1); FloatIsle(V(-57, 9, 40), 3.5f, 2); FloatIsle(V(-52, 6, 100), 5, 3); FloatIsle(V(-58, 12, 120), 3.5f, 4);
            FloatIsle(V(36, 6, 8), 3.5f, 5); FloatIsle(V(42, 12, 30), 4, 6); FloatIsle(V(38, 7, 82), 4.5f, 7); FloatIsle(V(42, 9, 99), 3, 8);
            FloatIsle(V(-24, 9, 148), 4, 9); FloatIsle(V(26, 6, 150), 3.5f, 10); FloatIsle(V(-16, 14, -24), 4, 11); FloatIsle(V(20, 11, -20), 3, 12);
            // plank bridges and rope ladders between / under them
            RopeBridge(V(-55, 8, 26), V(-57, 9, 36.5f), 1.8f, false);
            RopeBridge(V(38.8f, 7, 86.4f), V(41.6f, 9, 96), 1.8f, false);
            RopeLadder(V(-57, 9, 40) + new Vector3(2.6f, 0, 0), 7);
            RopeLadder(V(42, 12, 30) + new Vector3(-3, 0, 0), 8);
            RopeLadder(V(-24, 9, 148) + new Vector3(3, 0, 0), 6);
            RopeLadder(V(26, 6, 150) + new Vector3(-2.6f, 0, 0), 5);
            // mine scaffolds on two of the islands
            Frame(V(-55, 8, 20), 0); Frame(V(38, 7, 80), 90); Frame(V(-52, 6, 98), 30);
            Cart(V(-51, 6, 102), 20, false);

            // a long mine-cart trestle along the west wall
            Trestle(V(-44, 5, 0), V(-44, 9, 112), 10);

            // chains from the ceiling
            Chain(V(-12, 28, 40), 13); Chain(V(-34, 28, 36), 14); Chain(V(-36, 27, 58), 10); Chain(V(14, 28, 74), 12);
            Chain(V(-16, 28, 108), 12); Chain(V(18, 27, 112), 10); Chain(V(24, 28, 18), 12); Chain(V(-22, 27, 82), 11);

            // big stalactites with crystal tips
            var r = new Rng(3303);
            for (int i = 0; i < 26; i++)
            {
                var top = V(r.F(-62, 50), r.F(24, 30), r.F(-20, 160));
                float h = r.F(5, 12);
                Msh(Meshes.Cone(6), R, top, new Vector3(r.F(2.5f, 5), h, r.F(2.5f, 5)), rockDark, new Vector3(180, r.F(0, 60), 0));
                if (i % 2 == 0) Spike(R, top + Vector3.down * (h - 0.5f), top + Vector3.down * (h + 1.6f), 0.9f, i % 4 == 0 ? cryC : cryB);
            }
            // crystal clusters hanging from the ceiling
            for (int i = 0; i < 6; i++)
            {
                var top = V(r.F(-55, 45), 26, r.F(0, 150));
                for (int k = 0; k < 4; k++)
                    Spike(R, top, top + new Vector3(r.F(-2, 2), -r.F(3, 6), r.F(-2, 2)), r.F(0.8f, 1.6f), k % 2 == 0 ? cryC : cryB);
            }

            // glowing waterfalls and lava falls
            Waterfall(V(-50, 20, 46), 90, 4); Waterfall(V(-40, 16, 84), 70, 3); Waterfall(V(44, 20, 44), -90, 4);
            Waterfall(V(36, 16, 104), -110, 3); Waterfall(V(-26, 18, -18), 30, 3.5f); Waterfall(V(-38, 13, 54), 90, 2.4f);
            LavaFall(V(34, 12, 72), -80, 2.4f); LavaFall(V(-36, 10, 108), 80, 2.6f); LavaFall(V(28, 10, 142), -60, 2.2f);
            LavaFall(V(26, 14, 14), -90, 2); LavaFall(V(-14, 12, 154), 20, 2.4f);
        }

        /// <summary>Decorative floating island: a stone slab on an upside-down rock cone with crystals.</summary>
        static void FloatIsle(Vector3 top, float r, int seed)
        {
            var rng = new Rng(seed * 97 + 11);
            Cy(R, top - Vector3.up * 0.4f, new Vector3(r * 2, 0.4f, r * 2), rock);
            Msh(Meshes.Cone(7), R, top - Vector3.up * 0.8f, new Vector3(r * 2.1f, r * 1.8f + 2, r * 2.1f), rockDark, new Vector3(180, rng.F(0, 60), 0));
            Msh(Meshes.Cone(5), R, top + new Vector3(r * 0.4f, -r * 1.2f, 0), new Vector3(r, r * 1.4f, r), rockDark, new Vector3(180, 0, 0));
            int n = 2 + rng.I(3);
            for (int k = 0; k < n; k++)
            {
                var p = top + new Vector3(rng.F(-r, r) * 0.6f, 0, rng.F(-r, r) * 0.6f);
                var tilt = Quaternion.Euler(rng.F(-20, 20), 0, rng.F(-20, 20));
                var m = rng.I(3) switch { 0 => cryB, 1 => cryC, _ => cryPink };
                Spike(R, p, p + tilt * Vector3.up * rng.F(1.5f, 3.5f), rng.F(0.6f, 1.1f), m);
            }
            Spike(R, top + new Vector3(-r * 0.4f, -1.4f, r * 0.3f), top + new Vector3(-r * 0.7f, -3.2f, r * 0.5f), 0.7f, cryC);
        }

        static void RopeLadder(Vector3 top, float len)
        {
            foreach (var sd in new[] { -1, 1 }) LRod(R, top + new Vector3(0, 0, sd * 0.35f), top + new Vector3(0, -len, sd * 0.35f), 0.05f, rope);
            int n = Mathf.RoundToInt(len / 0.9f);
            for (int i = 1; i <= n; i++) B(R, top + new Vector3(0, -i * 0.9f + 0.4f, 0), new Vector3(0.12f, 0.06f, 0.8f), plank);
        }

        /// <summary>Wooden mine frame standing on an island (two posts, a beam, braces).</summary>
        static void Frame(Vector3 floor, float yaw)
        {
            var t = Node("Frame", R, floor, new Vector3(0, yaw, 0));
            foreach (var sd in new[] { -1, 1 })
            {
                B(t, new Vector3(sd * 1.5f, 1.6f, 0), new Vector3(0.3f, 3.2f, 0.3f), beam);
                Rod(t, new Vector3(sd * 1.5f, 2.2f, 0), new Vector3(sd * 0.6f, 3.1f, 0), 0.14f, beam);
            }
            B(t, new Vector3(0, 3.3f, 0), new Vector3(3.8f, 0.34f, 0.4f), beam);
            Ls(t, new Vector3(0.6f, 2.8f, 0.25f), 0.35f, lamp);
        }

        /// <summary>Long cart rail line on wooden trestle legs that stand in the lava, with a cart on it.</summary>
        static void Trestle(Vector3 a, Vector3 b, int bents)
        {
            var d = b - a; float len = d.magnitude;
            var t = Node("Trestle", R, (a + b) / 2, Quaternion.LookRotation(d.normalized).eulerAngles);
            foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 0.55f, 0.12f, 0), new Vector3(0.12f, 0.12f, len), iron);
            B(t, new Vector3(0, -0.05f, 0), new Vector3(1.8f, 0.14f, len), beam);
            int n = Mathf.CeilToInt(len / 2.4f);
            for (int i = 0; i < n; i++) B(t, new Vector3(0, 0.04f, -len / 2 + (i + 0.5f) * len / n), new Vector3(1.7f, 0.08f, 0.3f), plank);
            for (int i = 0; i <= bents; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)bents);
                float h = p.y - LavaY;
                foreach (var sd in new[] { -1, 1 }) Rod(R, p + new Vector3(sd * 0.8f, -0.1f, 0), new Vector3(p.x + sd * 1.8f, LavaY, p.z), 0.28f, beam);
                B(R, p + Vector3.down * (h * 0.45f), new Vector3(2.6f, 0.22f, 0.22f), beam);
                Rod(R, p + new Vector3(-1.1f, -h * 0.45f, 0), p + new Vector3(0.9f, -0.2f, 0), 0.14f, beam);
            }
            Cart(Vector3.Lerp(a, b, 0.35f), 0, false);
            Cart(Vector3.Lerp(a, b, 0.72f), 0, false);
        }

        static void Chain(Vector3 top, int links)
        {
            for (int i = 0; i < links; i++)
                Ring(R, top + Vector3.down * (i * 0.42f), 0.2f, 0.05f, iron, new Vector3(90, i % 2 == 0 ? 0 : 90, 0), new Vector3(1, 1, 1.5f));
            var end = top + Vector3.down * (links * 0.42f + 0.3f);
            Ls(R, end, new Vector3(0.9f, 0.7f, 0.9f), rockDark);
            Spike(R, end + Vector3.down * 0.2f, end + Vector3.down * 1.1f, 0.5f, links % 2 == 0 ? cryB : cryC);
        }

        /// <summary>Glowing waterfall: a rock lip, two scrolling sheets and foam on the lava (water meets lava → steam).</summary>
        static void Waterfall(Vector3 top, float yaw, float w)
        {
            float h = top.y - LavaY;
            var rot = Quaternion.Euler(0, yaw, 0);
            Ls(R, top + Vector3.up * 0.2f, new Vector3(w * 1.6f, 1.2f, 2.2f), rockDark, new Vector3(0, yaw, 0));
            B(R, top + Vector3.down * (h / 2), new Vector3(w, h, 0.12f), water, new Vector3(0, yaw, 0));
            B(R, top + Vector3.down * (h / 2) + rot * Vector3.forward * 0.08f, new Vector3(w * 0.45f, h, 0.06f), waterCore, new Vector3(0, yaw, 0));
            var foot = new Vector3(top.x, LavaY + 0.4f, top.z);
            Ls(R, foot, new Vector3(w * 1.5f, 1.2f, w * 1.2f), foam).AddComponent<Pulse>().amount = 0.12f;
            Ls(R, foot + Vector3.up * 1.2f, w * 2.2f, Add("D8F4FF", 0.08f)).AddComponent<Pulse>().amount = 0.08f;
        }

        static void LavaFall(Vector3 top, float yaw, float w)
        {
            float h = top.y - LavaY;
            Ls(R, top + Vector3.up * 0.2f, new Vector3(w * 1.6f, 1.2f, 2), rockDark, new Vector3(0, yaw, 0));
            B(R, top + Vector3.down * (h / 2), new Vector3(w, h, 0.16f), lavaFall, new Vector3(0, yaw, 0));
            B(R, top + Vector3.down * (h / 2), new Vector3(w * 0.4f, h, 0.2f), lavaCore, new Vector3(0, yaw, 0));
            Ls(R, new Vector3(top.x, LavaY + 0.2f, top.z), new Vector3(w * 2, 0.8f, w * 2), lavaGlow).AddComponent<Pulse>().amount = 0.1f;
        }

        static void PointLight(Vector3 p, string col, float range, float intensity)
        {
            var go = new GameObject("CaveLight");
            go.transform.SetParent(R, false);
            go.transform.position = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = Hex(col); l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        }

        /// <summary>Material with a vertical streak texture that scrolls down (one scroller per material).</summary>
        static Material Flowing(Material m, float speed, float tiles)
        {
            m.mainTextureScale = new Vector2(1, tiles);
            var go = new GameObject("FlowScroll");
            go.transform.SetParent(R, false);
            var f = go.AddComponent<W3Flow>();
            f.m = m; f.speed = speed;
            return m;
        }

        /// <summary>Vertical streaks for falling water / lava (white, tinted by the material).</summary>
        static Texture2D FallTex()
        {
            if (fallTex) return fallTex;
            const int W = 32, H = 64;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            var rng = new Rng(77);
            var col = new float[W];
            for (int x = 0; x < W; x++) col[x] = rng.F(0.35f, 1f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = col[x] * (0.7f + 0.3f * Mathf.Sin(y * 0.39f + x * 1.7f));
                    px[y * W + x] = new Color(v, v, v, v);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    /// <summary>Scrolls a shared material's texture downwards (waterfalls, lava falls).</summary>
    public class W3Flow : MonoBehaviour
    {
        public Material m;
        public float speed = 0.7f;
        void Update() { if (m) m.mainTextureOffset = new Vector2(0, Time.time * speed); }
    }
}
