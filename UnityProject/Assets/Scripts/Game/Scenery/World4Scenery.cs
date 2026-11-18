using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 4 «Снежная долина» dressed like its concept art: blue icy canyon cliffs all round, an ice castle on a cliff
    /// joined by a rope bridge to a wooden watchtower, an ice cave with glowing icicles, log cabins, igloos, snowy pines,
    /// ice slides and a big wooden sign. Plus a new side area west of the frozen lake: three snowy islands (watchtower
    /// ledge → igloo village → ice fort with a treasure grotto) linked by a rope bridge, ice steps and an ice slide,
    /// with 4 more friends frozen in ice cubes and their snowy keys.
    /// </summary>
    public static class World4Scenery
    {
        static Transform R;
        static Material snowT, snowW, igloo, iceRock, iceDeep, iceLight, iceGlass, iceGlow, castleM, blueRoof, dark,
            woodDark, plankA, plankB, logs, roofM, stone, gold, goldGlow, warm, pine, trunk;

        static Vector3 V(float x, float y, float z) => World4.O + new Vector3(x, y, z);
        static float WY => World4.WaterY;

        public static void Build()
        {
            if (World4.Root == null) return;
            R = World4.Root;
            var keep = Lobby.root;
            Lobby.root = R;
            Parts.Layer = Layers.World;

            // materials (created once)
            snowT = Lobby.Tiled(Tex.Snow(), 4);
            snowW = M("FFFFFF", 0.12f); igloo = M("EAF4FF", 0.3f);
            iceRock = M("7FB6E3", 0.55f); iceDeep = M("4D86C2", 0.5f); iceLight = M("BFE4FA", 0.7f);
            iceGlass = Glass("A8DCFF", 0.82f); iceGlow = G("8FD0FF", 0.9f);
            castleM = M("CDEBFF", 0.7f); blueRoof = M("4FA8E8", 0.6f); dark = M("1E3A5F", 0.2f);
            woodDark = M("5A3A22", 0.3f); plankA = M("8A5A36", 0.3f); plankB = M("7A4C2C", 0.3f);
            logs = Lobby.Tiled(Tex.Logs(), 1); roofM = M("6B4226", 0.3f); stone = M("8A93A0", 0.2f);
            gold = M("FFC83A", 0.7f, 0.6f); goldGlow = G("FFD95A", 1.3f); warm = G("FFD27A", 1.3f);
            pine = M("2F6B45", 0.15f); trunk = M("6D4C41", 0.2f);

            Backdrop();
            SideArea();
            BigSign(V(-15, 0, 18), -45);

            Lobby.root = keep;
        }

        // =============================== the new playable side area ===============================

        static void SideArea()
        {
            // --- island A: the watchtower ledge, a rope bridge from the west shore of the frozen lake ---
            Island(V(-26, 0, 34), 5.5f, 1);
            RopeBridge(V(-13f, 0, 34), V(-21f, 0, 34), 2.4f, true);
            Watchtower(V(-28.5f, 0, 31.5f), 3.6f, true);
            Chest(V(-29.5f, 0, 35.8f), 70, true);
            Pine(V(-30.8f, 0, 33.2f), 1.1f, true);
            Pine(V(-22.2f, 0, 37.8f), 0.9f, true);
            Rescue.SpawnCage("w4c8", V(-24f, 0, 30.2f), 90, R, true);
            for (int k = 0; k < 5; k++) Pickups.I.Coin(V(-16f - k * 1.3f, 0.8f, 34));

            // --- ice steps up to island B ---
            float[] sz = { 41.6f, 44.0f, 46.2f }, sy = { 0.7f, 1.5f, 2.1f };
            for (int i = 0; i < 3; i++)
            {
                float h = sy[i] - (WY - World4.O.y - 0.2f);   // pillar from under the water up to the step
                Lobby.Solid(PrimitiveType.Cube, V(-27, sy[i] - h / 2, sz[i]), new Vector3(1.8f, h, 1.8f), iceRock, new Vector3(0, i * 7, 0), R);
                B(R, V(-27, sy[i] + 0.02f, sz[i]), new Vector3(1.9f, 0.08f, 1.9f), snowW, new Vector3(0, i * 7, 0));
                Pickups.I.Coin(V(-27, sy[i] + 0.8f, sz[i]));
            }
            Pickups.I.Key("w4k8", V(-27, 1.5f + 1.1f, 44.0f), true);

            // --- island B: the igloo village with a little frozen pond ---
            Island(V(-29, 2.6f, 54), 7, 2);
            Igloo(V(-24f, 2.6f, 52.5f), 1.7f, -100, true);
            Igloo(V(-32f, 2.6f, 50f), 1.4f, 60, true);
            Cabin(V(-34f, 2.6f, 54.5f), 90, true);
            Pine(V(-23.5f, 2.6f, 56.3f), 1f, true);
            Pine(V(-33.8f, 2.6f, 58f), 1.2f, true);
            var pond = Lobby.Solid(PrimitiveType.Cylinder, V(-28.5f, 2.63f, 54.5f), new Vector3(3.6f, 0.03f, 3.6f), iceGlass, default, R);
            pond.AddComponent<Ice>();
            Ring(R, V(-28.5f, 2.62f, 54.5f), 1.85f, 0.18f, snowW);
            for (int k = 0; k < 4; k++)
                B(R, V(-28.5f, 2.67f, 54.5f), new Vector3(0.04f, 0.01f, 2.6f - k * 0.4f), snowW, new Vector3(0, k * 47 + 10, 0));
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Pickups.I.Coin(V(-28.5f + Mathf.Cos(a) * 2.6f, 3.4f, 54.5f + Mathf.Sin(a) * 2.6f)); }
            Rescue.SpawnCage("w4c9", V(-25f, 2.6f, 57.5f), 180, R, true);
            Pickups.I.Key("w4k9", V(-28.5f, 3.75f, 54.5f), true);

            // --- rope bridge up to island C ---
            var b0 = V(-27, 2.6f, 60.2f); var b1 = V(-25.4f, 5, 70.3f);
            RopeBridge(b0, b1, 2.4f, true);
            for (int k = 1; k <= 3; k++) Pickups.I.Coin(Vector3.Lerp(b0, b1, k / 4f) + Vector3.up * 0.8f);
            Pickups.I.Key("w4k10", Vector3.Lerp(b0, b1, 0.5f) + Vector3.up * 1.1f, true);

            // --- island C: the little ice fort with a treasure grotto ---
            var C = V(-25, 5, 76);
            Island(C, 6.5f, 3);
            IceTower(V(-30.2f, 5, 74.5f), 1f, 6f, true);
            IceTower(V(-19.8f, 5, 74.5f), 1f, 6f, true);
            Grotto(V(-25, 5, 79.6f));
            Rescue.SpawnCage("w4c10", V(-25, 5, 79.6f), 180, R, true);
            Rescue.SpawnCage("w4c11", V(-22f, 5, 71.8f), -135, R, true);
            Pickups.I.Key("w4k11", V(-25, 6.1f, 75), true);
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Pickups.I.Coin(C + new Vector3(Mathf.Cos(a) * 2f, 0.8f, -1 + Mathf.Sin(a) * 2f)); }

            // --- the ice slide back down from C to the igloo village (slippery) ---
            var s0 = V(-29.5f, 5, 72.2f); var s1 = V(-30.8f, 2.6f, 59.5f);
            IceSlide(s0, s1, 2.4f, true);
            for (int k = 1; k <= 3; k++) Pickups.I.Coin(Vector3.Lerp(s0, s1, k / 4f) + Vector3.up * 0.8f);
        }

        /// <summary>A snowy island: thick snow disc, blue ice cliff down to the water, icicles under the rim.</summary>
        static void Island(Vector3 top, float r, int seed)
        {
            var rng = new Rng(4400 + seed);
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * 1f, new Vector3(r * 2, 1f, r * 2), snowT, default, R);
            float colH = top.y - 1.8f - WY;
            if (colH > 0.2f)
            {
                Cy(R, new Vector3(top.x, WY + colH / 2, top.z), new Vector3(r * 1.85f, colH / 2, r * 1.85f), iceRock);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * 90 + rng.F(0, 60);
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    B(R, new Vector3(top.x, WY + colH / 2, top.z) + d * r * 0.86f, new Vector3(r * 0.5f, colH, 0.4f), iceDeep, new Vector3(0, a, 0));
                }
            }
            Cy(R, new Vector3(top.x, WY + 0.12f, top.z), new Vector3(r * 2.5f, 0.12f, r * 2.5f), iceLight);   // ice shelf on the water
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4 + rng.F(0, 0.5f);
                var p = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - 0.2f) - Vector3.up * 1.95f;
                Spike(R, p, p - Vector3.up * rng.F(0.8f, 1.6f), 0.4f, iceGlow);
            }
            for (int k = 0; k < 6; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ls(R, top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - 0.4f), new Vector3(rng.F(1, 1.8f), 0.5f, rng.F(1, 1.8f)), snowW);
            }
        }

        /// <summary>Plank bridge with sagging ropes; solid = walkable (deck collider), lamps on the end posts.</summary>
        static void RopeBridge(Vector3 a, Vector3 b, float w, bool solid)
        {
            var d = b - a; float len = d.magnitude;
            var rot = Quaternion.LookRotation(d / len);
            var up = rot * Vector3.up; var side = rot * Vector3.right;
            var e = rot.eulerAngles;
            var mid = (a + b) / 2 - up * 0.15f;
            if (solid) Lobby.Solid(PrimitiveType.Cube, mid, new Vector3(w, 0.3f, len), woodDark, e, R);
            else B(R, mid, new Vector3(w, 0.3f, len), woodDark, e);
            int n = Mathf.Max(3, Mathf.RoundToInt(len / 0.75f));
            for (int i = 0; i < n; i++)
                B(R, Vector3.Lerp(a, b, (i + 0.5f) / n) + up * 0.02f, new Vector3(w + 0.25f, 0.07f, len / n * 0.78f), i % 2 == 0 ? plankA : plankB, e + new Vector3(0, (i % 3 - 1) * 2, 0));
            foreach (var sd in new[] { -1f, 1f })
            {
                var o = side * sd * (w / 2 + 0.1f);
                foreach (var end in new[] { a, b })
                {
                    Cy(R, end + o + Vector3.up * 0.65f, new Vector3(0.2f, 0.75f, 0.2f), woodDark);
                    if (solid) S(R, end + o + Vector3.up * 1.5f, 0.3f, warm);
                }
                // sagging rope rail + hangers down to the deck
                Vector3 prev = a + o + Vector3.up * 1.2f;
                for (int k = 1; k <= 4; k++)
                {
                    float t = k / 4f;
                    var p = Vector3.Lerp(a, b, t) + o + Vector3.up * (1.2f - Mathf.Sin(t * Mathf.PI) * 0.35f);
                    Rod(R, prev, p, 0.05f, woodDark);
                    if (k < 4) Rod(R, p, Vector3.Lerp(a, b, t) + o, 0.03f, woodDark);
                    prev = p;
                }
            }
        }

        /// <summary>Slippery glass-ice slide from a (top) to b (bottom) with low ice rails.</summary>
        static void IceSlide(Vector3 a, Vector3 b, float w, bool solid)
        {
            var d = b - a; float len = d.magnitude;
            var rot = Quaternion.LookRotation(d / len);
            var up = rot * Vector3.up; var side = rot * Vector3.right;
            var e = rot.eulerAngles;
            var mid = (a + b) / 2 - up * 0.2f;
            if (solid)
            {
                Lobby.Solid(PrimitiveType.Cube, mid, new Vector3(w, 0.4f, len), iceGlass, e, R).AddComponent<Ice>();
                foreach (var sd in new[] { -1f, 1f })
                    Lobby.Solid(PrimitiveType.Cube, mid + side * sd * (w / 2 + 0.1f) + up * 0.4f, new Vector3(0.2f, 0.8f, len), iceLight, e, R);
            }
            else
            {
                B(R, mid, new Vector3(w, 0.4f, len), iceGlass, e);
                foreach (var sd in new[] { -1f, 1f }) B(R, mid + side * sd * (w / 2 + 0.1f) + up * 0.4f, new Vector3(0.2f, 0.8f, len), iceLight, e);
            }
            // two ice legs down to the water
            for (int k = 1; k <= 2; k++)
            {
                var p = Vector3.Lerp(a, b, k / 3f) - up * 0.4f;
                if (p.y - WY > 0.5f) Cy(R, new Vector3(p.x, (p.y + WY) / 2, p.z), new Vector3(0.6f, (p.y - WY) / 2, 0.6f), iceLight);
            }
        }

        /// <summary>The treasure grotto on island C: an ice arch with glowing icicles and two chests.</summary>
        static void Grotto(Vector3 c)
        {
            foreach (var sd in new[] { -1f, 1f })
                Lobby.Solid(PrimitiveType.Cube, c + new Vector3(sd * 3.9f, 1.3f, 0), new Vector3(0.8f, 6.6f, 4.2f), iceLight, default, R);
            Lobby.Solid(PrimitiveType.Cube, c + new Vector3(0, 1.3f, 2.3f), new Vector3(8.6f, 6.6f, 0.8f), iceRock, default, R);
            Lobby.Solid(PrimitiveType.Cube, c + new Vector3(0, 5.05f, 0.1f), new Vector3(9f, 0.9f, 5.2f), iceLight, default, R);
            S(R, c + new Vector3(0, 5.5f, 0.1f), new Vector3(9.4f, 1.4f, 5.6f), snowW);
            for (int k = 0; k < 7; k++)   // icicles on the front edge
            {
                var p = c + new Vector3(-3.6f + k * 1.2f, 4.6f, -2.3f);
                Spike(R, p, p - Vector3.up * (0.9f + (k % 3) * 0.35f), 0.35f, iceGlow);
            }
            for (int k = 0; k < 4; k++)   // and inside
            {
                var p = c + new Vector3(-2.7f + k * 1.8f, 4.6f, 0.6f);
                Spike(R, p, p - Vector3.up * 0.8f, 0.3f, iceGlow);
            }
            foreach (var sd in new[] { -1f, 1f })   // glowing crystals in the back corners
            {
                Spike(R, c + new Vector3(sd * 3.1f, 0, 1.5f), c + new Vector3(sd * 3.0f, 1.6f, 1.5f), 0.6f, iceGlow);
                Spike(R, c + new Vector3(sd * 2.6f, 0, 1.7f), c + new Vector3(sd * 2.3f, 1.0f, 1.7f), 0.4f, iceGlow);
                Chest(c + new Vector3(sd * 2.3f, 0, 1.0f), 180, true);
            }
        }

        // =============================== props ===============================

        static void Igloo(Vector3 p, float r, float yaw, bool solid)
        {
            var t = Node("Igloo", R, p, new Vector3(0, yaw, 0));
            var dome = new Vector3(r * 2, r * 1.7f, r * 2);
            if (solid) Lobby.Solid(PrimitiveType.Sphere, Vector3.zero, dome, igloo, default, t); else S(t, Vector3.zero, dome, igloo);
            for (int k = 1; k <= 2; k++)   // rows of ice blocks
            {
                float y = r * 0.85f * k * 0.33f, rad = r * Mathf.Sqrt(1 - (k * 0.33f) * (k * 0.33f));
                Ring(t, new Vector3(0, y, 0), 0.5f, 0.012f, iceLight, default, new Vector3(rad * 2 + 0.04f, 1, rad * 2 + 0.04f));
            }
            Cy(t, new Vector3(0, r * 0.32f, r * 0.95f), new Vector3(r * 0.85f, r * 0.42f, r * 0.85f), igloo, new Vector3(90, 0, 0));
            Cy(t, new Vector3(0, r * 0.3f, r * 1.38f), new Vector3(r * 0.6f, 0.02f, r * 0.6f), dark, new Vector3(90, 0, 0));
        }

        static void Cabin(Vector3 p, float yaw, bool solid)
        {
            var t = Node("Cabin", R, p, new Vector3(0, yaw, 0));
            if (solid) Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(3f, 2.4f, 2.6f), logs, default, t);
            else B(t, new Vector3(0, 1.2f, 0), new Vector3(3f, 2.4f, 2.6f), logs);
            foreach (var sd in new[] { -1f, 1f })
            {
                var e = new Vector3(0, 0, -sd * 35);
                var roof = solid ? Lobby.Solid(PrimitiveType.Cube, new Vector3(sd * 0.85f, 2.85f, 0), new Vector3(2.0f, 0.22f, 3.2f), roofM, e, t)
                                 : B(t, new Vector3(sd * 0.85f, 2.85f, 0), new Vector3(2.0f, 0.22f, 3.2f), roofM, e);
                B(roof.transform, new Vector3(0, 0.9f, 0), new Vector3(1.04f, 0.8f, 1.02f), snowW);
            }
            S(t, new Vector3(0, 3.45f, 0), new Vector3(0.7f, 0.35f, 3.3f), snowW);
            B(t, new Vector3(-0.6f, 0.85f, 1.31f), new Vector3(0.8f, 1.6f, 0.05f), woodDark);
            B(t, new Vector3(0.75f, 1.4f, 1.31f), new Vector3(0.6f, 0.55f, 0.05f), warm);
            B(t, new Vector3(1.51f, 1.4f, 0), new Vector3(0.05f, 0.55f, 0.6f), warm);
            B(t, new Vector3(-0.8f, 3.4f, -0.6f), new Vector3(0.4f, 1.0f, 0.4f), stone);
            S(t, new Vector3(-0.8f, 3.92f, -0.6f), new Vector3(0.55f, 0.2f, 0.55f), snowW);
        }

        static void Watchtower(Vector3 p, float h, bool solid)
        {
            var t = Node("Watchtower", R, p);
            float top = h + 2.2f;
            for (int k = 0; k < 4; k++)
            {
                float x = k % 2 == 0 ? -1 : 1, z = k < 2 ? -1 : 1;
                var pos = new Vector3(x, top / 2, z); var sc = new Vector3(0.25f, top / 2, 0.25f);
                if (solid) Lobby.Solid(PrimitiveType.Cylinder, pos, sc, woodDark, default, t); else Cy(t, pos, sc, woodDark);
            }
            Rod(t, new Vector3(-1, 0.3f, 1), new Vector3(1, h * 0.8f, 1), 0.1f, plankA);
            Rod(t, new Vector3(1, 0.3f, -1), new Vector3(-1, h * 0.8f, -1), 0.1f, plankA);
            Rod(t, new Vector3(-1, 0.3f, -1), new Vector3(-1, h * 0.8f, 1), 0.1f, plankA);
            Rod(t, new Vector3(1, 0.3f, 1), new Vector3(1, h * 0.8f, -1), 0.1f, plankA);
            B(t, new Vector3(0, h, 0), new Vector3(2.8f, 0.25f, 2.8f), plankA);
            for (int k = 0; k < 4; k++)
                B(t, Quaternion.Euler(0, k * 90, 0) * new Vector3(0, h + 0.9f, 1.05f), new Vector3(2.3f, 0.12f, 0.1f), plankB, new Vector3(0, k * 90, 0));
            Msh(Meshes.Cone(4), t, new Vector3(0, top, 0), new Vector3(3.8f, 1.4f, 3.8f), roofM, new Vector3(0, 45, 0));
            Msh(Meshes.Cone(4), t, new Vector3(0, top + 0.25f, 0), new Vector3(3.2f, 1.2f, 3.2f), snowW, new Vector3(0, 45, 0));
            S(t, new Vector3(0, top - 0.35f, 0), 0.4f, warm);
        }

        static void IceTower(Vector3 p, float r, float h, bool solid)
        {
            var t = Node("IceTower", R, p);
            if (solid) Lobby.Solid(PrimitiveType.Cylinder, new Vector3(0, h / 2, 0), new Vector3(2 * r, h / 2, 2 * r), castleM, default, t);
            else Cy(t, new Vector3(0, h / 2, 0), new Vector3(2 * r, h / 2, 2 * r), castleM);
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60;
                B(t, Quaternion.Euler(0, a, 0) * new Vector3(0, h + 0.3f, r * 0.85f), new Vector3(r * 0.55f, 0.6f, r * 0.35f), castleM, new Vector3(0, a, 0));
            }
            Cone(t, new Vector3(0, h, 0), new Vector3(r * 1.9f, r * 2.4f, r * 1.9f), blueRoof);
            Cone(t, new Vector3(0, h + r * 1.5f, 0), new Vector3(r * 0.7f, r * 0.9f, r * 0.7f), snowW);
            B(t, new Vector3(0, h * 0.62f, -r * 0.98f), new Vector3(r * 0.5f, r * 0.8f, 0.06f), iceGlow);
            Rod(t, new Vector3(0, h + r * 2.3f, 0), new Vector3(0, h + r * 2.3f + 1.4f, 0), 0.06f, woodDark);
            B(t, new Vector3(0.45f, h + r * 2.3f + 1.15f, 0), new Vector3(0.8f, 0.45f, 0.04f), G("35E0FF", 1.2f));
        }

        static void Chest(Vector3 p, float yaw, bool solid)
        {
            var t = Node("Chest", R, p, new Vector3(0, yaw, 0));
            if (solid) Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.6f, 0.7f), plankA, default, t);
            else B(t, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.6f, 0.7f), plankA);
            Cy(t, new Vector3(0, 0.6f, 0), new Vector3(0.7f, 0.55f, 0.7f), plankB, new Vector3(0, 0, 90));
            B(t, new Vector3(0, 0.6f, 0), new Vector3(1.12f, 0.05f, 0.72f), goldGlow);
            foreach (var sd in new[] { -1f, 1f }) B(t, new Vector3(sd * 0.38f, 0.45f, 0), new Vector3(0.09f, 0.62f, 0.74f), gold);
            B(t, new Vector3(0, 0.5f, 0.36f), new Vector3(0.18f, 0.22f, 0.05f), gold);
            S(t, new Vector3(0, 0.95f, 0), new Vector3(0.9f, 0.15f, 0.5f), snowW);
        }

        static void Pine(Vector3 p, float s, bool solid)
        {
            if (solid) Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * 0.6f * s, new Vector3(0.35f * s, 0.6f * s, 0.35f * s), trunk, default, R);
            else Cy(R, p + Vector3.up * 0.6f * s, new Vector3(0.35f * s, 0.6f * s, 0.35f * s), trunk);
            for (int k = 0; k < 3; k++)
                Cone(R, p + Vector3.up * (0.9f + k * 1.0f) * s, new Vector3((2.6f - k * 0.65f) * s, 1.9f * s, (2.6f - k * 0.65f) * s), pine);
            Cone(R, p + Vector3.up * 3.15f * s, new Vector3(0.75f * s, 0.75f * s, 0.75f * s), snowW);
            Cone(R, p + Vector3.up * 2.25f * s, new Vector3(1.3f * s, 0.55f * s, 1.3f * s), snowW);
        }

        // =============================== backdrop (no colliders) ===============================

        /// <summary>A blue ice cliff rising from the water: faceted body, a light streak, snow on top, icicles.</summary>
        static void Cliff(Vector3 b, float w, float h, float yaw, Rng r)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            var c = b + Vector3.up * h / 2;
            B(R, c, new Vector3(w, h, w * 0.8f), iceRock, new Vector3(0, yaw, 0));
            B(R, c + q * new Vector3(r.F(-1, 1), -h * 0.04f, r.F(-1, 1)), new Vector3(w * 0.75f, h * 0.92f, w * 0.9f), iceDeep, new Vector3(0, yaw + 40, 0));
            B(R, c + q * new Vector3(r.F(-w * 0.2f, w * 0.2f), -h * 0.1f, 0), new Vector3(w * 0.3f, h * 0.75f, w * 0.84f), iceLight, new Vector3(0, yaw, 0));
            B(R, b + Vector3.up * (h + 0.15f), new Vector3(w * 1.03f, 0.5f, w * 0.83f), snowW, new Vector3(0, yaw, 0));
            S(R, b + Vector3.up * (h + 0.35f), new Vector3(w * 0.95f, 1.2f, w * 0.75f), snowW, new Vector3(0, yaw, 0));
            for (int k = 0; k < 2; k++)
            {
                var e = b + q * new Vector3(r.F(-w * 0.4f, w * 0.4f), h - 0.1f, (k == 0 ? -1 : 1) * w * 0.41f);
                Spike(R, e, e - Vector3.up * r.F(1.5f, 3f), 0.6f, iceLight);
            }
        }

        static void Ridge(Rng r, int n, System.Func<int, Vector3> at, float hMin, float hMax, float wMin, float wMax)
        {
            for (int i = 0; i < n; i++)
            {
                var b = at(i);
                float h = r.F(hMin, hMax), w = r.F(wMin, wMax);
                Cliff(b, w, h, r.F(0, 90), r);
                if (r.Chance(0.5f)) Pine(b + new Vector3(r.F(-w * 0.25f, w * 0.25f), h + 0.3f, r.F(-w * 0.2f, w * 0.2f)), r.F(1.1f, 1.6f), false);
            }
        }

        static void Backdrop()
        {
            var r = new Rng(4477);
            float wy = WY;
            // the west canyon wall behind the new islands (gaps where the special cliffs stand)
            float[] wz = { 6, 14, 22, 32, 52, 58, 80, 88, 96, 104 };
            Ridge(r, wz.Length, i => new Vector3(World4.O.x - 46 - r.F(0, 6), wy, World4.O.z + wz[i]), 7, 17, 6, 9);
            // the east canyon wall opposite (the castle cliff stands in its middle)
            float[] ez = { -4, 6, 16, 26, 36, 46, 78, 104 };
            Ridge(r, ez.Length, i => new Vector3(World4.O.x + 37 + r.F(0, 6), wy, World4.O.z + ez[i]), 7, 16, 6, 9);
            // the cliffs behind the start
            Ridge(r, 11, i => new Vector3(World4.O.x - 40 + i * 8, wy, World4.O.z - 32 - r.F(0, 6)), 8, 18, 7, 10);

            // --- the Ice Castle on its cliff (east) with a rope bridge to the watchtower cliff ---
            Cliff(V(44, wy - World4.O.y, 62), 18, 12 - (wy - World4.O.y), 0, r);
            IceCastle(V(44, 12, 62));
            Cliff(V(43, wy - World4.O.y, 92), 12, 14 - (wy - World4.O.y), 10, r);
            Watchtower(V(43, 14, 93), 4, false);
            Igloo(V(39.5f, 14, 95.5f), 1.3f, -90, false);
            Igloo(V(46.5f, 14, 88.6f), 1.2f, -120, false);
            for (int k = 0; k < 5; k++) Cy(R, V(38.5f + k * 1.2f, 14.6f, 88.2f), new Vector3(0.15f, 0.6f, 0.15f), woodDark);
            foreach (var y in new[] { 14.5f, 14.95f }) B(R, V(40.9f, y, 88.2f), new Vector3(5, 0.1f, 0.08f), plankA);
            RopeBridge(V(42, 12, 69.6f), V(42, 14, 87.5f), 2f, false);
            IceSlide(V(36.5f, 12, 57), V(30.5f, 0.2f, 47.5f), 2.2f, false);
            IceSlide(V(37.8f, 14, 91), V(31.5f, 1, 101), 2.2f, false);

            // --- the west watchtower cliff and the cabin cliff ---
            Cliff(V(-47, wy - World4.O.y, 42), 11, 11 - (wy - World4.O.y), 20, r);
            Watchtower(V(-47, 11, 42), 3.5f, false);
            Cliff(V(-46, wy - World4.O.y, 24), 13, 8 - (wy - World4.O.y), -10, r);
            Cabin(V(-46.5f, 8, 23.5f), 80, false);
            Pine(V(-43.5f, 8, 27.5f), 1.3f, false);
            Pine(V(-49.5f, 8, 20.5f), 1.5f, false);
            IceSlide(V(-40.5f, 8, 21), V(-33.5f, 0, 14), 2.2f, false);

            // --- the ice cave in the west wall: a dark mouth with glowing icicles ---
            Cliff(V(-47, wy - World4.O.y, 68), 14, 13 - (wy - World4.O.y), 0, r);
            var m = V(-39.85f, 0, 68);
            B(R, m + Vector3.up * 1.2f, new Vector3(0.3f, 5.6f, 6.2f), dark);
            foreach (var sd in new[] { -1f, 1f }) B(R, m + new Vector3(0.3f, 1.2f, sd * 3.4f), new Vector3(0.9f, 6f, 1f), iceLight);
            B(R, m + new Vector3(0.3f, 4.2f, 0), new Vector3(1f, 0.8f, 7.8f), iceLight);
            for (int k = 0; k < 8; k++)
            {
                var p = m + new Vector3(0.4f, 3.8f, -2.6f + k * 0.75f);
                Spike(R, p, p - Vector3.up * (0.8f + (k % 3) * 0.5f), 0.35f, iceGlow);
            }
            for (int k = 0; k < 3; k++) Spike(R, m + new Vector3(0.3f, -1.6f, -1.5f + k * 1.5f), m + new Vector3(0.6f, 0.2f + (k % 2) * 0.7f, -1.5f + k * 1.5f), 0.6f, iceGlow);
            Cy(R, m + new Vector3(2.5f, -1.5f, 0), new Vector3(6, 0.1f, 7), iceLight);
        }

        /// <summary>The big ice castle from the picture: crenellated walls, four towers, a tall keep, a wooden gate.</summary>
        static void IceCastle(Vector3 c)
        {
            for (int s = 0; s < 4; s++)
            {
                var q = Quaternion.Euler(0, s * 90, 0);
                B(R, c + q * new Vector3(0, 1.8f, 5.5f), new Vector3(11, 3.6f, 0.8f), castleM, new Vector3(0, s * 90, 0));
                for (int k = 0; k < 5; k++)
                    B(R, c + q * new Vector3(-4 + k * 2, 3.9f, 5.5f), new Vector3(0.9f, 0.6f, 0.95f), castleM, new Vector3(0, s * 90, 0));
            }
            foreach (var x in new[] { -5.5f, 5.5f })
                foreach (var z in new[] { -5.5f, 5.5f })
                    IceTower(c + new Vector3(x, 0, z), 1.5f, 6.5f, false);
            IceTower(c + new Vector3(0, 0, 1.5f), 2.6f, 9.5f, false);
            for (int k = 0; k < 3; k++) B(R, c + new Vector3(-2.62f, 3 + k * 2.2f, 1.5f), new Vector3(0.06f, 0.9f, 0.6f), warm);
            // the gate faces the valley (west)
            B(R, c + new Vector3(-5.95f, 1.2f, 0), new Vector3(0.2f, 2.4f, 2f), woodDark);
            Ring(R, c + new Vector3(-6f, 2.4f, 0), 1.05f, 0.14f, iceLight, new Vector3(0, 0, 90));
            foreach (var sd in new[] { -1f, 1f }) B(R, c + new Vector3(-6.05f, 1.2f, sd * 0.45f), new Vector3(0.05f, 2.3f, 0.06f), gold);
        }

        // =============================== the big wooden sign ===============================

        static void BigSign(Vector3 p, float yaw)
        {
            var t = Node("SignSnowValley", R, p, new Vector3(0, yaw, 0));
            Cy(t, new Vector3(0, -0.7f, 0), new Vector3(3.4f, 0.9f, 2.4f), stone);
            S(t, new Vector3(0, 0.2f, 0), new Vector3(3.6f, 0.6f, 2.6f), snowW);
            foreach (var sd in new[] { -1f, 1f }) Cy(t, new Vector3(sd * 2.3f, 1.8f, 0.05f), new Vector3(0.32f, 1.9f, 0.32f), woodDark);
            B(t, new Vector3(0, 2.65f, 0), new Vector3(6.2f, 3f, 0.3f), Lobby.Tiled(Tex.Planks("8A5A36"), 1));
            B(t, new Vector3(0, 2.65f, 0.02f), new Vector3(6.5f, 3.3f, 0.26f), woodDark);
            S(t, new Vector3(0, 4.2f, 0), new Vector3(6.6f, 0.5f, 0.6f), snowW);
            for (int k = 0; k < 6; k++)
            {
                var a = new Vector3(-2.8f + k * 1.1f, 1.05f, -0.1f);
                Spike(t, a, a - Vector3.up * (0.4f + (k % 2) * 0.3f), 0.18f, iceLight);
            }
            Prim.Text(t, "СНЕЖНАЯ", new Vector3(0, 3.25f, -0.2f), 9, Color.white);
            Prim.Text(t, "ДОЛИНА", new Vector3(0, 2.05f, -0.2f), 9, Color.white);
        }
    }
}
