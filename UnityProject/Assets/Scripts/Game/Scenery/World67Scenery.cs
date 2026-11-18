using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// «Мир 67» set dressing from the concept art: a night sky of floating rock islands with ruined stone towers,
    /// spiral stairs, rope bridges and stone aqueducts, brass gears and pipes with green neon, cyan crystals,
    /// teal portal arches, lanterns, waterfalls and a giant glowing «67» outline in the sky.
    /// Plus a walkable side area west of plaza B: rope bridge → tower island with a spiral stair → aqueduct →
    /// gear island → rope bridge → portal island → stepping stones back down (a loop full of coins).
    /// All coordinates here are relative to World67.O (the node sits at O).
    /// </summary>
    public static class World67Scenery
    {
        static Transform R;
        static Material stone, stoneDark, rock, moss, grass, brass, wood, rope, crystal, crystalPurple,
            neonGreen, teal, warm, neon67, halo67, water, waterNeon, portalFill, pathGlow, window, roof, hub;

        public static void Build()
        {
            if (World67.Root == null) return;
            Parts.Layer = Layers.World;
            R = Node("World67Scenery", World67.Root, World67.O);

            // materials: created once
            stone = Mats.Tex(Tex.Concrete(), Hex("8C93C4"), 0.15f); stone.mainTextureScale = new Vector2(3, 3);
            grass = Mats.Tex(Tex.Grass(), Hex("5FB7A0"), 0.1f); grass.mainTextureScale = new Vector2(3, 3);
            stoneDark = M("3A3E62", 0.2f); rock = M("2A2442", 0.15f); moss = M("2F7A66", 0.1f);
            brass = M("B8873A", 0.6f, 0.8f); wood = M("6E4A2E", 0.2f); rope = M("C9A66B", 0.1f);
            crystal = G("5FF2FF", 2.2f); crystalPurple = G("C66BFF", 2f); neonGreen = G("39FF6A", 1.8f);
            teal = G("2EF5C8", 1.8f); warm = G("FFC45A", 2.2f); neon67 = G("FFF0FA", 2.6f); window = G("FFB347", 1.6f);
            halo67 = Add("FF6AD5", 0.18f); water = Add("8FEFFF", 0.32f); portalFill = Add("2EF5C8", 0.45f);
            pathGlow = G("FFD36A", 1.8f); waterNeon = Add("39FF6A", 0.3f); roof = M("33295A", 0.2f); hub = M("6B4A22", 0.5f, 0.7f);

            SideArea();
            Backdrop();
            Giant67(new Vector3(0, 30, 290));
        }

        static Vector3 W(Vector3 local) => World67.O + local;

        // =====================================================================
        // the walkable side area (west of plaza B: x −8, top y 4, z 92..106)
        // =====================================================================

        static void SideArea()
        {
            // 1) rope bridge from the west edge of plaza B to island A
            WalkBridge(new Vector3(-8.2f, 4, 99), new Vector3(-15.8f, 4, 99));
            for (int k = 0; k < 5; k++) Pickups.I.Coin(W(new Vector3(-9.5f - k * 1.4f, 4.9f, 99)));

            // 2) island A with a ruined tower and a spiral stair round it
            var a = new Vector3(-21, 4, 99);
            WalkIsland(a, 5.5f, 7);
            Lobby.Solid(PrimitiveType.Cylinder, a + new Vector3(0, 8, 0), new Vector3(3.6f, 8, 3.6f), stone, default, R);
            TowerTop(a + new Vector3(0, 16, 0), 1.8f, true);
            B(R, a + new Vector3(0, 7, -1.82f), new Vector3(0.6f, 1.1f, 0.05f), window);
            B(R, a + new Vector3(1.82f, 13, 0), new Vector3(0.05f, 1.1f, 0.6f), window);
            // 15 steps, 26° and 0.42 m each: one full turn from y 4.4 to y 10.3, ending on the north side
            for (int i = 0; i < 15; i++)
            {
                float ang = 86 + i * 26, top = a.y + 0.42f * (i + 1);
                var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0, Mathf.Sin(ang * Mathf.Deg2Rad));
                var c = a + dir * 3.2f + Vector3.up * (top - 0.18f);
                Lobby.Solid(PrimitiveType.Cube, c, new Vector3(1.9f, 0.36f, 1.45f), stoneDark, new Vector3(0, -ang, 0), R);
                if (i % 2 == 1) Pickups.I.Coin(W(a + dir * 3.3f + Vector3.up * (top + 0.9f)));
                if (i % 5 == 2) Lantern(a + dir * 4.05f + Vector3.up * top, 1.1f);
            }
            Crystals(a + new Vector3(3.6f, 0, -3.4f), 1.2f, crystal, 11);
            Crystals(a + new Vector3(-4.2f, 0, -2.2f), 0.9f, crystalPurple, 12);
            Waterfall(a + new Vector3(-5.2f, -0.8f, 1.5f), 14, 1.6f, 90);
            for (int k = 0; k < 4; k++) Pickups.I.Coin(W(a + new Vector3(4.2f, 0.9f, -1.5f + k * 1.2f)));

            // 3) a stone aqueduct north from the top of the stair to island B (y 10.3)
            Aqueduct(new Vector3(-21, 10.3f, 102.6f), new Vector3(-21, 10.3f, 111.2f), 2.6f, true);
            for (int k = 0; k < 4; k++) Pickups.I.Coin(W(new Vector3(-21, 11.2f, 104 + k * 2)));

            // 4) island B: the gear island
            var b = new Vector3(-21, 10.3f, 115);
            WalkIsland(b, 4.5f, 8);
            for (int k = 0; k < 8; k++)
            {
                float t = k * 45 * Mathf.Deg2Rad;
                Pickups.I.Coin(W(b + new Vector3(Mathf.Cos(t) * 2.6f, 0.9f, Mathf.Sin(t) * 2.6f)));
            }
            Gear(b + new Vector3(2.6f, 3.6f, 5.4f), 2.4f, new Vector3(0, 0, 0), 25);   // behind the island, off the floor
            Gear(b + new Vector3(-1.2f, 2.4f, 5.6f), 1.5f, new Vector3(0, 0, 0), -40);
            Pipe(new[] { b + new Vector3(4.6f, -0.4f, 3.5f), b + new Vector3(5.4f, 2.5f, 4.2f), b + new Vector3(5.4f, 6, 4.2f), b + new Vector3(2.6f, 6.4f, 5.4f) }, 0.45f);
            Lantern(b + new Vector3(3.3f, 0, -2.6f), 1.6f);
            Lantern(b + new Vector3(-3.3f, 0, 2.6f), 1.6f);
            Crystals(b + new Vector3(2.8f, 0, 2.4f), 1f, crystal, 13);
            var lightGo = new GameObject("Glow67Side") { layer = Layers.World };
            lightGo.transform.SetParent(R, false);
            lightGo.transform.localPosition = b + new Vector3(0, 3, 0);
            var li = lightGo.AddComponent<Light>();
            li.type = LightType.Point; li.color = Hex("3FF5D2"); li.range = 16; li.intensity = 2.2f;

            // 5) a slightly sloping rope bridge west to island C (portal island)
            WalkBridge(new Vector3(-25.3f, 10.3f, 115), new Vector3(-29.8f, 9.5f, 115));
            for (int k = 0; k < 3; k++) Pickups.I.Coin(W(new Vector3(-26.4f - k * 1.2f, 11 - k * 0.2f, 115)));
            var c2 = new Vector3(-33.5f, 9.5f, 115);
            WalkIsland(c2, 4f, 9);
            PortalArch(c2 + new Vector3(-2, 0, 0.5f), 90, 1.4f);
            Crystals(c2 + new Vector3(1.6f, 0, 2.6f), 1.1f, crystal, 14);
            Crystals(c2 + new Vector3(-1.4f, 0, -2.8f), 0.8f, crystalPurple, 15);
            Lantern(c2 + new Vector3(2.6f, 0, -2.2f), 1.6f);
            Waterfall(c2 + new Vector3(0, -0.8f, 4.2f), 16, 1.4f, 0, true);
            for (int k = 0; k < 6; k++) Pickups.I.Coin(W(c2 + new Vector3(-0.6f + (k % 3) * 1.3f, 0.9f, -1 + (k / 3) * 1.6f)));

            // 6) stepping stones back down to island A (gaps ~1–2 m, steps 1.2–1.4 m down)
            var stones = new[] { new Vector3(-32.5f, 8.2f, 107.6f), new Vector3(-30.5f, 6.8f, 104.4f), new Vector3(-28.4f, 5.4f, 101.4f) };
            foreach (var s in stones)
            {
                Lobby.Solid(PrimitiveType.Cylinder, s + Vector3.down * 0.3f, new Vector3(2.6f, 0.3f, 2.6f), stone, default, R);
                Cone(R, s + Vector3.down * 0.55f, new Vector3(2.3f, 2.2f, 2.3f), rock, new Vector3(180, 0, 0));
                Ring(R, s + Vector3.down * 0.05f, 1.3f, 0.05f, teal);
                Pickups.I.Coin(W(s + Vector3.up * 0.9f));
            }
        }

        /// <summary>Walkable floating island: grassy top (top at c.y), mossy rim, rocky underside with hanging rocks.</summary>
        static void WalkIsland(Vector3 c, float r, int seed)
        {
            Lobby.Solid(PrimitiveType.Cylinder, c + Vector3.down * 0.5f, new Vector3(r * 2, 0.5f, r * 2), grass, default, R);
            IslandBelow(c, r, seed);
            Ring(R, c + Vector3.up * 0.03f, r - 0.15f, 0.06f, pathGlow);   // the glowing path rim
        }

        static void IslandBelow(Vector3 c, float r, int seed)
        {
            var rng = new Rng(seed);
            Cy(R, c + Vector3.down * 1.05f, new Vector3(r * 2.04f, 0.1f, r * 2.04f), moss);
            Cone(R, c + Vector3.down * 1f, new Vector3(r * 1.95f, r * 1.7f, r * 1.95f), rock, new Vector3(180, rng.F(0, 360), 0));
            for (int k = 0; k < 2; k++)
            {
                float t = rng.F(0, Mathf.PI * 2);
                var p = c + new Vector3(Mathf.Cos(t) * r * 0.55f, -1, Mathf.Sin(t) * r * 0.55f);
                Cone(R, p, new Vector3(r * 0.6f, r * rng.F(0.7f, 1.1f), r * 0.6f), rock, new Vector3(180, 0, rng.F(-10, 10)));
            }
        }

        /// <summary>Walkable rope bridge from a to b (deck-top points): planks with colliders, posts, sagging rope rails.</summary>
        static void WalkBridge(Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude;
            var rot = Quaternion.LookRotation(d.normalized).eulerAngles;
            var side = Vector3.Cross(Vector3.up, d.normalized).normalized;
            int n = Mathf.CeilToInt(len / 0.56f);
            for (int i = 0; i < n; i++)
            {
                var p = Vector3.Lerp(a, b, (i + 0.5f) / n) + Vector3.down * 0.09f;
                Lobby.Solid(PrimitiveType.Cube, p, new Vector3(2.2f, 0.18f, len / n - 0.06f), wood, rot, R);
            }
            foreach (var s in new[] { -1f, 1f })
            {
                var pa = a + side * s * 1.1f; var pb = b + side * s * 1.1f;
                Cy(R, pa + Vector3.up * 0.6f, new Vector3(0.16f, 0.65f, 0.16f), wood);
                Cy(R, pb + Vector3.up * 0.6f, new Vector3(0.16f, 0.65f, 0.16f), wood);
                Ls(R, pa + Vector3.up * 1.3f, 0.22f, warm);
                Ls(R, pb + Vector3.up * 1.3f, 0.22f, warm);
                SagRope(pa + Vector3.up * 1.1f, pb + Vector3.up * 1.1f, 0.35f, 5, 0.05f, rope);
            }
        }

        static void SagRope(Vector3 a, Vector3 b, float sag, int n, float d, Material m)
        {
            var prev = a;
            for (int i = 1; i <= n; i++)
            {
                float t = (float)i / n;
                var p = Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4 * t * (1 - t));
                LRod(R, prev, p, d, m);
                prev = p;
            }
        }

        /// <summary>Stone aqueduct from a to b (deck-top points) with arches and pillars below; walkable when asked.</summary>
        static void Aqueduct(Vector3 a, Vector3 b, float width, bool walk, float pillarH = 9)
        {
            var d = b - a; float len = d.magnitude;
            var yaw = Quaternion.LookRotation(d.normalized).eulerAngles.y;
            var mid = (a + b) / 2 + Vector3.down * 0.35f;
            if (walk) Lobby.Solid(PrimitiveType.Cube, mid, new Vector3(width, 0.7f, len), stone, new Vector3(0, yaw, 0), R);
            else B(R, mid, new Vector3(width, 0.7f, len), stoneDark, new Vector3(0, yaw, 0));
            // glowing gold path line on top
            B(R, (a + b) / 2 + Vector3.up * 0.02f, new Vector3(0.12f, 0.03f, len * 0.92f), pathGlow, new Vector3(0, yaw, 0));
            int spans = Mathf.Max(1, Mathf.RoundToInt(len / 6f));
            for (int i = 0; i <= spans; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / spans);
                if (walk && (i == 0 || i == spans)) continue;   // ends rest on the islands
                B(R, p + Vector3.down * (0.7f + pillarH / 2), new Vector3(width * 0.8f, pillarH, 1.1f), stoneDark, new Vector3(0, yaw, 0));
            }
            for (int i = 0; i < spans; i++)
            {
                // a half ring under each span = the arch
                float ar = len / spans / 2 - 0.4f;
                var p = Vector3.Lerp(a, b, (i + 0.5f) / spans) + Vector3.down * (0.7f + ar * 0.55f + 0.3f);
                Ring(R, p, ar, 0.35f, stoneDark, new Vector3(90, yaw + 90, 0), new Vector3(1, 1, 0.55f));
            }
        }

        /// <summary>Crenellated broken top of a tower (decor).</summary>
        static void TowerTop(Vector3 top, float r, bool ruined)
        {
            Cy(R, top + Vector3.up * 0.2f, new Vector3(r * 2.3f, 0.2f, r * 2.3f), stoneDark);
            for (int k = 0; k < 6; k++)
            {
                if (ruined && (k == 2 || k == 3)) continue;
                float t = k * 60 * Mathf.Deg2Rad;
                B(R, top + new Vector3(Mathf.Cos(t) * r * 1.05f, 0.85f, Mathf.Sin(t) * r * 1.05f), new Vector3(0.7f, 1.1f, 0.7f), stoneDark, new Vector3(0, -k * 60, 0));
            }
            if (!ruined) Cone(R, top + Vector3.up * 0.4f, new Vector3(r * 2.4f, r * 2.2f, r * 2.4f), roof);
        }

        static void Crystals(Vector3 at, float s, Material m, int seed)
        {
            var rng = new Rng(seed);
            for (int k = 0; k < 4; k++)
            {
                var dir = new Vector3(rng.F(-0.5f, 0.5f), 1, rng.F(-0.5f, 0.5f)).normalized;
                var basePos = at + new Vector3(rng.F(-0.4f, 0.4f), 0, rng.F(-0.4f, 0.4f)) * s;
                Spike(R, basePos, basePos + dir * s * rng.F(1.2f, 2.4f), s * rng.F(0.35f, 0.55f), m);
            }
        }

        static void Lantern(Vector3 floor, float h)
        {
            Cy(R, floor + Vector3.up * h / 2, new Vector3(0.1f, h / 2, 0.1f), stoneDark);
            var bulb = Ls(R, floor + Vector3.up * (h + 0.18f), new Vector3(0.3f, 0.36f, 0.3f), warm);
            bulb.AddComponent<Pulse>().amount = 0.05f;
            Cone(R, floor + Vector3.up * (h + 0.34f), new Vector3(0.42f, 0.25f, 0.42f), stoneDark);
        }

        /// <summary>A thin sheet of water falling from an island edge into the void, with mist at the bottom.</summary>
        static void Waterfall(Vector3 top, float h, float w, float yaw, bool neon = false)
        {
            B(R, top + Vector3.down * h / 2, new Vector3(w, h, 0.12f), neon ? waterNeon : water, new Vector3(0, yaw, 0));
            B(R, top + Vector3.down * h * 0.4f, new Vector3(w * 0.4f, h * 0.8f, 0.14f), water, new Vector3(0, yaw, 0));
            var mist = Ls(R, top + Vector3.down * h, new Vector3(w * 2.2f, 1.2f, w * 2.2f), water);
            mist.AddComponent<Pulse>().amount = 0.12f;
        }

        /// <summary>Brass gear standing upright (axis along local z after euler), slowly turning.</summary>
        static void Gear(Vector3 c, float r, Vector3 euler, float speed)
        {
            var g = Node("Gear67", R, c, euler + new Vector3(90, 0, 0));
            Cy(g, Vector3.zero, new Vector3(r * 2, 0.18f, r * 2), brass);
            Cy(g, Vector3.zero, new Vector3(r * 0.5f, 0.3f, r * 0.5f), hub);
            Ring(g, Vector3.zero, r * 0.72f, 0.05f, neonGreen);
            int teeth = Mathf.Clamp(Mathf.RoundToInt(r * 5), 8, 20);
            for (int k = 0; k < teeth; k++)
            {
                float t = k * 360f / teeth;
                var dir = Quaternion.Euler(0, t, 0) * Vector3.forward;
                B(g, dir * (r + 0.15f), new Vector3(r * 0.35f, 0.34f, 0.45f), brass, new Vector3(0, t, 0));
            }
            var sp = g.gameObject.AddComponent<Spin>(); sp.axis = Vector3.up; sp.speed = speed;
        }

        /// <summary>Brass pipe along points with a thin green neon line next to it.</summary>
        static void Pipe(Vector3[] pts, float d)
        {
            for (int i = 1; i < pts.Length; i++)
            {
                LRod(R, pts[i - 1], pts[i], d, brass);
                LRod(R, pts[i - 1] + Vector3.forward * (d * 0.55f), pts[i] + Vector3.forward * (d * 0.55f), d * 0.2f, neonGreen);
                Ls(R, pts[i], d * 1.25f, brass);
            }
        }

        /// <summary>Teal neon portal arch (just for looks).</summary>
        static void PortalArch(Vector3 floor, float yaw, float s)
        {
            var p = Node("PortalArch67", R, floor, new Vector3(0, yaw, 0));
            foreach (var sd in new[] { -1f, 1f }) B(p, new Vector3(sd * 1.5f * s, 1.3f * s, 0), new Vector3(0.6f * s, 2.6f * s, 0.6f * s), stoneDark);
            Ring(p, new Vector3(0, 2.6f * s, 0), 1.5f * s, 0.3f * s, stoneDark, new Vector3(90, 0, 0));
            Ring(p, new Vector3(0, 2.6f * s, -0.05f), 1.22f * s, 0.08f * s, teal, new Vector3(90, 0, 0));
            B(p, new Vector3(0, 1.3f * s, 0), new Vector3(2.3f * s, 2.6f * s, 0.06f), portalFill);
            var disc = Cy(p, new Vector3(0, 2.6f * s, 0), new Vector3(2.3f * s, 0.02f, 2.3f * s), portalFill, new Vector3(90, 0, 0));
            disc.AddComponent<Pulse>().amount = 0.06f;
        }

        // =====================================================================
        // distant backdrop (no colliders)
        // =====================================================================

        static void Backdrop()
        {
            // floating islands round the route (all ≥ 15 m from walkways and from the side area)
            var isl = new[]
            {
                new Vector4(40, 0, 20, 6), new Vector4(54, 6, 40, 5), new Vector4(50, 10, 95, 8), new Vector4(38, -4, 122, 5),
                new Vector4(56, 4, 172, 6), new Vector4(40, 12, 196, 5), new Vector4(-48, 4, 30, 6), new Vector4(-38, 13, 52, 4.5f),
                new Vector4(-60, 8, 112, 7), new Vector4(-52, -2, 152, 6), new Vector4(-42, 10, 192, 5), new Vector4(-16, 2, -42, 6),
                new Vector4(18, 8, -36, 5), new Vector4(0, -2, 268, 9),
            };
            for (int i = 0; i < isl.Length; i++)
            {
                var c = new Vector3(isl[i].x, isl[i].y, isl[i].z);
                DecorIsland(c, isl[i].w, 100 + i, i != 2 && i != 13);
            }
            // bridges between neighbours
            DecorRopeBridge(new Vector3(40, 0, 20), new Vector3(54, 6, 40), 6, 5);
            DecorRopeBridge(new Vector3(-48, 4, 30), new Vector3(-38, 13, 52), 6, 4.5f);
            DecorRopeBridge(new Vector3(56, 4, 172), new Vector3(40, 12, 196), 6, 5);
            DecorRopeBridge(new Vector3(-16, 2, -42), new Vector3(18, 8, -36), 6, 5);
            AqueductBetween(new Vector3(50, 10, 95), 8, new Vector3(38, -4, 122), 5);
            AqueductBetween(new Vector3(-60, 8, 112), 7, new Vector3(-52, -2, 152), 6);
            // a long low aqueduct crossing far below the route
            Aqueduct(new Vector3(-46, -12, 132), new Vector3(46, -12, 132), 3f, false, 14);

            // the big machine island: two gears, pipes, neon garlands
            var m = new Vector3(50, 10, 95);
            Gear(m + new Vector3(-3, 8, 2), 5f, new Vector3(0, -70, 0), 12);
            Gear(m + new Vector3(-2, 4.5f, -5.5f), 3.2f, new Vector3(0, -70, 0), -19);
            B(R, m + new Vector3(1.5f, 5, 0), new Vector3(4, 10, 7), stoneDark);   // machine house
            B(R, m + new Vector3(-0.6f, 3, 0), new Vector3(0.1f, 2, 1.3f), window);
            Pipe(new[] { m + new Vector3(-6, 0.2f, 5), m + new Vector3(-6, 6, 5), m + new Vector3(-1, 13, 5), m + new Vector3(3, 13, 3), m + new Vector3(3, 18, 3) }, 0.9f);
            Pipe(new[] { m + new Vector3(-6, 0.2f, -2), m + new Vector3(-8, 4, -3), m + new Vector3(-8, 9, -7) }, 0.7f);
            for (int k = 0; k < 3; k++)
                SagRope(m + new Vector3(3, 12 - k * 1.5f, 3), new Vector3(40, 12, 196) + new Vector3(0, 9 - k * 1.2f, 0), 4 + k, 8, 0.12f, neonGreen);
            SagRope(m + new Vector3(2, 10, -3), new Vector3(54, 6, 40) + new Vector3(0, 8, 0), 6, 8, 0.12f, neonGreen);

            // spiral stair towers standing on the low aqueduct
            SpiralTower(new Vector3(-38, -12, 132), 2.2f, 22, 3);
            SpiralTower(new Vector3(38, -12, 132), 2f, 18, 4);
        }

        /// <summary>A floating rock island with a ruined tower, crystals, a lantern and maybe a waterfall.</summary>
        static void DecorIsland(Vector3 c, float r, int seed, bool tower)
        {
            var rng = new Rng(seed);
            Cy(R, c + Vector3.down * 0.5f, new Vector3(r * 2, 0.5f, r * 2), grass);
            IslandBelow(c, r, seed);
            if (tower && r >= 5)
            {
                float tr = r * 0.32f, th = r * rng.F(2f, 3.2f);
                var tc = c + new Vector3(rng.F(-1, 1), 0, rng.F(-1, 1)) * r * 0.25f;
                Cy(R, tc + Vector3.up * th / 2, new Vector3(tr * 2, th / 2, tr * 2), stoneDark);
                TowerTop(tc + Vector3.up * th, tr, rng.Chance(0.5f));
                B(R, tc + new Vector3(0, th * 0.6f, -tr), new Vector3(tr * 0.5f, tr * 0.8f, 0.1f), window);
                // spiral stair round the tower
                int n = 10;
                for (int k = 0; k < n; k++)
                {
                    float ang = k * 34 + seed * 17;
                    var dir = Quaternion.Euler(0, -ang, 0) * Vector3.right;
                    B(R, tc + dir * (tr + 0.6f) + Vector3.up * (th * 0.75f * k / n + 0.3f), new Vector3(1.2f, 0.22f, 0.8f), stone, new Vector3(0, -ang, 0));
                }
            }
            else if (tower) Lantern(c + new Vector3(r * 0.4f, 0, 0), 1.8f);
            Crystals(c + new Vector3(-r * 0.55f, 0, r * 0.3f), r * 0.35f, rng.Chance(0.75f) ? crystal : crystalPurple, seed + 1);
            if (r >= 5) Crystals(c + new Vector3(r * 0.5f, -0.6f, -r * 0.7f), r * 0.25f, crystal, seed + 2);
            if (rng.Chance(0.6f)) Waterfall(c + new Vector3(0, -0.9f, r * 0.98f), r * 3.2f, r * 0.35f, 0);
        }

        static void DecorRopeBridge(Vector3 ia, Vector3 ib, float ra, float rb)
        {
            var dir = ib - ia; dir.y = 0; dir.Normalize();
            var a = ia + dir * (ra - 0.4f); var b = ib - dir * (rb - 0.4f);
            var side = Vector3.Cross(Vector3.up, dir).normalized;
            // deck: a chain of short sagging plank pieces
            int n = 8; var prev = a;
            for (int i = 1; i <= n; i++)
            {
                float t = (float)i / n;
                var p = Vector3.Lerp(a, b, t) + Vector3.down * (1.2f * 4 * t * (1 - t));
                var go = B(R, (prev + p) / 2, new Vector3(2f, 0.15f, (p - prev).magnitude), wood);
                go.transform.localRotation = Quaternion.LookRotation(p - prev);
                prev = p;
            }
            foreach (var s in new[] { -1f, 1f })
            {
                SagRope(a + side * s + Vector3.up * 1.1f, b + side * s + Vector3.up * 1.1f, 1.2f, 6, 0.06f, rope);
                Ls(R, a + side * s + Vector3.up * 1.2f, 0.25f, warm);
            }
        }

        static void AqueductBetween(Vector3 ia, float ra, Vector3 ib, float rb)
        {
            var dir = ib - ia; dir.y = 0; dir.Normalize();
            var a = ia + dir * (ra - 0.5f); var b = ib - dir * (rb - 0.5f);
            Aqueduct(a, b, 2.4f, false, 10);
        }

        static void SpiralTower(Vector3 floor, float r, float h, int seed)
        {
            Cy(R, floor + Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), stoneDark);
            TowerTop(floor + Vector3.up * h, r, seed % 2 == 0);
            int n = Mathf.RoundToInt(h / 0.9f);
            for (int k = 0; k < n; k++)
            {
                float ang = k * 28;
                var dir = Quaternion.Euler(0, -ang, 0) * Vector3.right;
                B(R, floor + dir * (r + 0.7f) + Vector3.up * (k * 0.9f + 0.4f), new Vector3(1.4f, 0.25f, 0.9f), stone, new Vector3(0, -ang, 0));
                if (k % 6 == 3) Ls(R, floor + dir * (r + 1.5f) + Vector3.up * (k * 0.9f + 1.4f), 0.3f, warm);
            }
            B(R, floor + new Vector3(0, h * 0.7f, -r), new Vector3(r * 0.4f, r * 0.7f, 0.1f), window);
        }

        // =====================================================================
        // the giant neon «67» outline in the sky
        // =====================================================================

        static void Giant67(Vector3 o)
        {
            var g = Node("Giant67", R, o);
            // «6»: a loop at the bottom and a curved stem up to the top right
            NeonRing(g, new Vector3(-12, 8.5f, 0), 8.5f);
            NeonLine(g, new[] { new Vector3(-20.5f, 8.5f, 0), new Vector3(-20.3f, 16, 0), new Vector3(-19, 22, 0), new Vector3(-16.5f, 27, 0),
                new Vector3(-13, 30.5f, 0), new Vector3(-9, 32, 0), new Vector3(-5, 31, 0) });
            // «7»: the top bar and the slanted leg
            NeonLine(g, new[] { new Vector3(4, 32, 0), new Vector3(21, 32, 0), new Vector3(9, 0, 0) });
            g.gameObject.AddComponent<Bob>().amp = 0.8f;
        }

        static void NeonRing(Transform g, Vector3 c, float r)
        {
            Ring(g, c, r, 0.45f, neon67, new Vector3(90, 0, 0));
            Ring(g, c, r, 1.3f, halo67, new Vector3(90, 0, 0));
        }

        static void NeonLine(Transform g, Vector3[] pts)
        {
            for (int i = 1; i < pts.Length; i++)
            {
                LRod(g, pts[i - 1], pts[i], 0.9f, neon67);
                LRod(g, pts[i - 1], pts[i], 2.6f, halo67);
                Ls(g, pts[i], 0.9f, neon67);
            }
            Ls(g, pts[0], 0.9f, neon67);
        }
    }
}
