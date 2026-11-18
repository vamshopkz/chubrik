using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 5 «Джунгли» dressed like its concept art: a ring of tall moss-covered limestone pillars with trees on top,
    /// rope bridges between them, big waterfalls into blue pools, a natural stone arch, floating rocks on ropes,
    /// rusty bronze gears and pipes in the cliffs, wooden scaffolds — plus a playable side area east of the jungle bank:
    /// five pillars joined by rope bridges and floating stepping stones, 4 more friends in cages and their keys.
    /// </summary>
    public static class World5Scenery
    {
        static Transform R;
        static Material lime, limeDark, grassTop, moss, leaf, leaf2, bark, plank, rope, wood, bronze, bronzeDark, glowTeal, fall, foam, streak;

        static Vector3 V(float x, float y, float z) => World5.O + new Vector3(x, y, z);

        public static void Build()
        {
            R = World5.Root;
            if (R == null) return;
            Parts.Layer = Layers.World;

            // materials (each made once)
            lime = Mats.Tex(Tex.Sandstone(), Hex("D9D6C6"), 0.15f);
            limeDark = M("9C9884", 0.15f);
            grassTop = Lobby.Tiled(Tex.Grass(), 2);
            moss = M("5DA83A", 0.15f);
            leaf = M("2E9E3E", 0.2f);
            leaf2 = M("6CC24A", 0.2f);
            bark = Mats.Tex(Tex.Bark(), Color.white, 0.1f);
            plank = Lobby.Tiled(Tex.Planks("9A6A3A"), 1);
            rope = M("8A6A42", 0.1f);
            wood = M("7A5230", 0.15f);
            bronze = M("B0682E", 0.45f, 0.6f);
            bronzeDark = M("7A4520", 0.35f, 0.5f);
            glowTeal = G("7FF5E8", 1.8f);
            fall = Glass("BFE9FF", 0.6f);
            foam = M("F4FBFF", 0.3f);
            streak = G("E8F8FF", 0.6f);

            Water();
            Backdrop();
            SideArea();
        }

        // ---------------- blue pools instead of the purple swamp outside the route ----------------

        static void Water()
        {
            Pool(V(52, -2.85f, 70), 78, 220);    // east
            Pool(V(-55, -2.85f, 70), 70, 220);   // west
            Pool(V(-3.5f, -2.85f, 149), 33, 62);    // north, behind the pyramid
            Pool(V(-3.5f, -2.85f, -26), 33, 28);    // south, behind the start
        }

        static void Pool(Vector3 c, float w, float l)
        {
            var m = Mats.Tex(Tex.Water(), new Color(0.45f, 0.8f, 1f), 0.95f);
            m.mainTextureScale = new Vector2(w / 6f, l / 6f);
            var p = Prim.Make(PrimitiveType.Plane, R, c, new Vector3(w / 10f, 1, l / 10f), m, "Pool", Layers.World);
            p.AddComponent<WaterScroll>();
        }

        // ---------------- far backdrop (no colliders) ----------------

        static void Backdrop()
        {
            const int N = 24;
            var rng = new Rng(5505);
            var tops = new Vector3[N];
            var rad = new float[N];
            var centre = V(0, 0, 50);
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2 / N + rng.F(-0.08f, 0.08f);
                float push = rng.F(0, 14);
                var b = centre + new Vector3(Mathf.Cos(a) * (60 + push), -4, Mathf.Sin(a) * (102 + push));
                float r = rng.F(4.5f, 8f), h = rng.F(26, 48);
                if (i == 7 || i == 9) r = Mathf.Min(r, 5.5f);   // room for the arch between them
                tops[i] = b + Vector3.up * h; rad[i] = r;
                var inward = (centre - b); inward.y = 0; inward.Normalize();
                if (i == 8) { Arch(b + Vector3.up * 1, inward); continue; }   // the natural stone arch instead of a pillar
                BigPillar(b, r, h, i);
                // every 4th pillar: a rusty gear with a glowing eye and a pipe down to the water
                if (i % 4 == 1)
                {
                    var gc = b + inward * (r + 0.3f) + Vector3.up * h * 0.45f;
                    Gear(gc, Mathf.Min(r * 0.7f, 4.5f), inward, i % 8 == 1 ? 12 : -9, true);
                    Pipe(new[] { gc + inward * 0.4f + Vector3.down * r * 0.6f, b + inward * (r + 0.6f) + Vector3.up * h * 0.25f, b + inward * (r + 0.6f) + Vector3.up * 0.5f }, 0.7f);
                }
                // every 4th (other phase): a big waterfall into the pool
                if (i % 4 == 3) Waterfall(b + inward * (r * 1.1f + 0.2f) + Vector3.up * (h - 1.5f), inward, 4.5f);
                // wooden scaffold at the foot of some pillars
                if (i % 6 == 0) Scaffold(b + inward * (r + 1.6f) + Vector3.up * 1.2f, inward, rng.F(5, 9));
            }

            // rope bridges between neighbouring pillar tops (not all of them)
            for (int i = 0; i < N; i++)
            {
                if (i % 3 == 2 || i == 7) continue;
                int j = (i + 1) % N;
                var dir = tops[j] - tops[i]; dir.y = 0; dir.Normalize();
                float y = Mathf.Min(tops[i].y, tops[j].y) - 2.5f;
                var a = tops[i] + dir * (rad[i] - 0.5f); a.y = y;
                var c = tops[j] - dir * (rad[j] - 0.5f); c.y = y;
                Bridge(a, c, 1.8f, false, 4f);
            }

            // floating rocks between pillars, tied with ropes
            for (int i = 0; i < N; i += 3)
            {
                int j = (i + 1) % N;
                var p = (tops[i] + tops[j]) * 0.5f + Vector3.up * rng.F(5, 10) + (centre - tops[i]).normalized * 4;
                p.y = Mathf.Max(p.y, World5.O.y + 30);
                float s = rng.F(3, 6);
                FloatRock(p, s, false);
                Rod(R, p + Vector3.up * 0.2f * s, tops[i] + Vector3.down * 0.5f, 0.12f, rope);
                Rod(R, p + Vector3.up * 0.2f * s, tops[j] + Vector3.down * 0.5f, 0.12f, rope);
                for (int k = 0; k < 2; k++)   // little pebbles drifting around it
                    FloatRock(p + new Vector3(rng.F(-6, 6), rng.F(-4, 3), rng.F(-6, 6)), rng.F(0.8f, 1.6f), true);
            }

            // soft sun beams through the haze
            var beam = Add("FFF2B8", 0.07f);
            for (int k = 0; k < 3; k++)
                Spike(R, V(-20 + k * 22, -3, 150 + k * 6), V(-10 + k * 14, 120, 240), 18, beam);
        }

        /// <summary>A natural stone arch (half sunk in the water) facing <paramref name="inward"/>, mossy on top.</summary>
        static void Arch(Vector3 c, Vector3 inward)
        {
            const float Rr = 9f, r = 2.2f;
            float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            var q = Quaternion.Euler(0, yaw, 0);
            Ring(R, c, Rr, r, lime, new Vector3(90, yaw, 0));
            for (int k = 0; k < 7; k++)
            {
                float t = Mathf.PI * (0.15f + k * 0.117f);
                Ls(R, c + q * new Vector3(Mathf.Cos(t) * Rr, Mathf.Sin(t) * Rr + r * 0.8f, 0), new Vector3(4, 1.8f, 4), k % 2 == 0 ? moss : leaf2);
            }
            Tree(c + q * new Vector3(-2.5f, Rr + r - 0.3f, 0), 3, 2.2f);
            Tree(c + q * new Vector3(3, Rr + r - 0.5f, 0), 2.6f, 1.9f);
        }

        /// <summary>A tall limestone pillar: stacked rough drums, a grassy top with trees, moss and vines.</summary>
        static void BigPillar(Vector3 b, float r, float h, int seed)
        {
            var rng = new Rng(seed * 13 + 7);
            float y = 0;
            for (int k = 0; k < 3; k++)
            {
                float hh = h * (k == 0 ? 0.42f : k == 1 ? 0.33f : 0.25f);
                float rr = r * (1.08f - k * 0.1f);
                var off = new Vector3(rng.F(-0.5f, 0.5f), 0, rng.F(-0.5f, 0.5f));
                Cy(R, b + off + Vector3.up * (y + hh / 2), new Vector3(rr * 2, hh / 2, rr * 2), k == 1 ? limeDark : lime, new Vector3(0, rng.F(0, 360), 0));
                y += hh;
            }
            var top = b + Vector3.up * h;
            Cy(R, top + Vector3.up * 0.1f, new Vector3(r * 1.9f, 0.25f, r * 1.9f), moss);
            for (int k = 0; k < 3; k++)   // moss spilling over the edge
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ls(R, top + new Vector3(Mathf.Cos(a) * r * 0.85f, -0.8f, Mathf.Sin(a) * r * 0.85f), new Vector3(r * 0.7f, 2.4f, r * 0.7f), k == 0 ? leaf2 : moss);
            }
            Ls(R, b + new Vector3(rng.F(-1, 1) * r, h * 0.3f, rng.F(-1, 1) * r), new Vector3(r * 0.9f, r * 1.4f, r * 0.9f), lime);   // a bulge
            Tree(top + new Vector3(rng.F(-0.3f, 0.3f) * r, 0, rng.F(-0.3f, 0.3f) * r), rng.F(3, 5), rng.F(2.4f, 3.4f));
            for (int k = 0; k < 2; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ca(R, top + new Vector3(Mathf.Cos(a) * r * 0.97f, -3.5f, Mathf.Sin(a) * r * 0.97f), new Vector3(0.18f, 3.5f, 0.18f), leaf);   // vines
            }
        }

        /// <summary>A round-crowned tree (decoration, no collider).</summary>
        static void Tree(Vector3 p, float h, float crown)
        {
            Cy(R, p + Vector3.up * h / 2, new Vector3(0.45f, h / 2, 0.45f), bark);
            S(R, p + new Vector3(0, h + crown * 0.2f, 0), crown * 1.3f, leaf);
            S(R, p + new Vector3(crown * 0.4f, h - crown * 0.1f, crown * 0.25f), crown, leaf2);
        }

        /// <summary>A rope bridge from a to b (deck tops). Solid = walkable deck with a collider.</summary>
        static void Bridge(Vector3 a, Vector3 b, float w, bool solid, float hangerStep = 1.3f)
        {
            var d = b - a;
            float len = d.magnitude;
            var rot = Quaternion.LookRotation(d / len);
            var side = rot * Vector3.right;
            var mid = (a + b) / 2 - (rot * Vector3.up) * 0.12f;
            var size = new Vector3(w, 0.24f, len + 0.6f);
            if (solid) Lobby.Solid(PrimitiveType.Cube, mid, size, plank, rot.eulerAngles, R);
            else B(R, mid, size, plank, rot.eulerAngles);
            foreach (var sd in new[] { -1f, 1f })
            {
                var o = side * (w / 2 + 0.05f) * sd;
                Cy(R, a + o + Vector3.up * 0.55f, new Vector3(0.16f, 0.6f, 0.16f), wood);
                Cy(R, b + o + Vector3.up * 0.55f, new Vector3(0.16f, 0.6f, 0.16f), wood);
                // the sagging hand rope
                const int seg = 4;
                Vector3 prev = a + o + Vector3.up * 1.05f;
                for (int k = 1; k <= seg; k++)
                {
                    float t = k / (float)seg;
                    var p = Vector3.Lerp(a, b, t) + o + Vector3.up * (1.05f - 0.45f * 4 * t * (1 - t));
                    Rod(R, prev, p, 0.06f, rope);
                    prev = p;
                }
                int n = Mathf.FloorToInt(len / hangerStep);
                for (int k = 1; k < n; k++)
                {
                    float t = k / (float)n;
                    var p = Vector3.Lerp(a, b, t) + o;
                    Rod(R, p, p + Vector3.up * (1.05f - 0.45f * 4 * t * (1 - t)), 0.035f, rope);
                }
            }
        }

        /// <summary>A rusty bronze gear facing <paramref name="normal"/>, slowly turning.</summary>
        static void Gear(Vector3 c, float rad, Vector3 normal, float speed, bool eye)
        {
            var g = Node("Gear", R, c, Quaternion.LookRotation(normal).eulerAngles);
            var t = Node("Turn", g, Vector3.zero);
            var sp = t.gameObject.AddComponent<Spin>(); sp.axis = Vector3.forward; sp.speed = speed;
            Ring(t, Vector3.zero, rad, rad * 0.12f, bronze, new Vector3(90, 0, 0));
            for (int k = 0; k < 10; k++)
            {
                float a = k * 36f;
                var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                B(t, dir * rad * 1.12f, new Vector3(rad * 0.24f, rad * 0.22f, rad * 0.2f), bronzeDark, new Vector3(0, 0, a));
            }
            for (int k = 0; k < 4; k++) B(t, Vector3.zero, new Vector3(rad * 0.14f, rad * 1.9f, rad * 0.1f), bronzeDark, new Vector3(0, 0, k * 45f));
            Cy(t, Vector3.zero, new Vector3(rad * 0.45f, rad * 0.1f, rad * 0.45f), bronze, new Vector3(90, 0, 0));
            if (eye)
            {
                Cy(g, Vector3.forward * rad * 0.12f, new Vector3(rad * 0.28f, 0.05f, rad * 0.28f), glowTeal, new Vector3(90, 0, 0)).AddComponent<Pulse>().amount = 0.05f;
            }
        }

        /// <summary>A rusty pipe through the given points with round joints.</summary>
        static void Pipe(Vector3[] pts, float d)
        {
            for (int k = 0; k < pts.Length - 1; k++) Rod(R, pts[k], pts[k + 1], d, bronze);
            for (int k = 0; k < pts.Length; k++) S(R, pts[k], d * 1.35f, bronzeDark);
        }

        /// <summary>A waterfall pouring down from <paramref name="top"/>, its face looking along <paramref name="outward"/>.</summary>
        static void Waterfall(Vector3 top, Vector3 outward, float w)
        {
            outward.y = 0; outward.Normalize();
            var rot = Quaternion.LookRotation(outward);
            var e = rot.eulerAngles;
            var side = rot * Vector3.right;
            float bottom = World5.O.y - 2.8f;
            float hh = top.y - bottom;
            var c = new Vector3(top.x, (top.y + bottom) / 2, top.z) + outward * 0.6f;
            B(R, c, new Vector3(w, hh, 0.4f), fall, e);
            for (int k = 0; k < 3; k++)   // bright streaks
                B(R, c + side * ((k - 1) * w * 0.3f) + outward * 0.22f, new Vector3(w * 0.08f, hh, 0.05f), streak, e);
            Ls(R, top + outward * 0.5f, new Vector3(w * 1.1f, 0.6f, 1.4f), fall);   // the lip
            for (int k = 0; k < 4; k++)
            {
                var f = Ls(R, new Vector3(top.x, bottom + 0.1f, top.z) + outward * (1.2f + (k % 2) * 0.8f) + side * ((k - 1.5f) * w * 0.32f), new Vector3(w * 0.45f, 1, w * 0.45f), foam);
                f.AddComponent<Pulse>().amount = 0.12f;
            }
        }

        /// <summary>A wooden scaffold leaning on a cliff: posts, cross braces and a plank deck (decoration).</summary>
        static void Scaffold(Vector3 b, Vector3 outward, float h)
        {
            var rot = Quaternion.LookRotation(outward);
            var side = rot * Vector3.right;
            for (int k = 0; k < 4; k++)
            {
                var p = b + side * (k < 2 ? -1.4f : 1.4f) + outward * (k % 2 == 0 ? -0.8f : 0.8f);
                Cy(R, p + Vector3.up * h / 2, new Vector3(0.22f, h / 2, 0.22f), wood);
            }
            Rod(R, b + side * -1.4f + outward * 0.8f, b + side * 1.4f + outward * 0.8f + Vector3.up * h * 0.9f, 0.12f, wood);
            Rod(R, b + side * 1.4f + outward * 0.8f, b + side * -1.4f + outward * 0.8f + Vector3.up * h * 0.9f, 0.12f, wood);
            B(R, b + Vector3.up * h, new Vector3(3.4f, 0.2f, 2.2f), plank, rot.eulerAngles);
            B(R, b + Vector3.up * h * 0.5f, new Vector3(3.4f, 0.2f, 2.2f), plank, rot.eulerAngles);
            Rod(R, b + Vector3.up * h, b + Vector3.up * (h + 6) + outward * 1.5f, 0.08f, rope);   // a hoist rope
        }

        /// <summary>A floating rock: an upside-down cone of stone with a grassy top.</summary>
        static void FloatRock(Vector3 p, float s, bool bob)
        {
            var t = Node("FloatRock", R, p);
            Cone(t, Vector3.zero, new Vector3(s * 1.6f, s * 1.5f, s * 1.6f), lime, new Vector3(180, 0, 0));
            Cy(t, Vector3.up * 0.05f, new Vector3(s * 1.65f, 0.12f * s, s * 1.65f), moss);
            if (!bob) { S(t, new Vector3(s * 0.2f, s * 0.35f, 0), s * 0.7f, leaf2); Ls(t, new Vector3(-s * 0.4f, -s * 0.5f, s * 0.2f), s * 0.6f, limeDark); }
            else { var bb = t.gameObject.AddComponent<Bob>(); bb.amp = 0.4f; bb.speed = 0.8f; }
        }

        // ---------------- the playable side area: «Висячие скалы» ----------------

        static void SideArea()
        {
            var S0 = V(8.6f, 0, 35.0f);                // the jungle bank's east edge
            var A = V(16.5f, 0.8f, 36f); const float rA = 3.2f;
            var Bp = V(26f, 2.4f, 38f); const float rB = 3f;
            var C = V(30f, 4.2f, 48.5f); const float rC = 3.2f;
            var D = V(23.5f, 6f, 58f); const float rD = 4f;
            var T1 = V(27.6f, 6.9f, 63.2f);            // floating stepping stones
            var T2 = V(30.2f, 7.6f, 66.3f);
            var E = V(33.5f, 8.4f, 70.5f); const float rE = 3f;

            Rock(A, rA, 0); Rock(Bp, rB, 1); Rock(C, rC, 2); Rock(D, rD, 3); Rock(E, rE, 4);
            Stone(T1, 1.1f); Stone(T2, 1.1f);

            Bridge(S0, Rim(A, rA, S0), 1.8f, true);
            Link(A, rA, Bp, rB);
            Link(Bp, rB, C, rC);
            Link(C, rC, D, rD);

            // a wooden lookout tower on D (solid crate block with braces)
            var tw = D + new Vector3(-2.2f, 0, -1.2f);
            Lobby.Solid(PrimitiveType.Cube, tw + Vector3.up * 0.8f, new Vector3(1.8f, 1.6f, 1.8f), plank, default, R);
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Cy(R, tw + new Vector3(sx * 0.95f, 0.85f, sz * 0.95f), new Vector3(0.2f, 0.85f, 0.2f), wood);
            Rod(R, tw + new Vector3(-0.95f, 0.1f, -0.97f), tw + new Vector3(0.95f, 1.5f, -0.97f), 0.1f, wood);
            Rod(R, tw + new Vector3(-0.97f, 0.1f, 0.95f), tw + new Vector3(-0.97f, 1.5f, -0.95f), 0.1f, wood);
            Rod(R, tw + new Vector3(0, 1.7f, 0), tw + new Vector3(0, 4.2f, 0), 0.1f, wood);                 // a little flag
            B(R, tw + new Vector3(0.45f, 3.9f, 0), new Vector3(0.8f, 0.5f, 0.04f), M("FFD23A", 0.3f));

            // a big rusty gear in D's west face (looks at the route), a pipe down to the water
            var gc = D + new Vector3(-rD + 0.2f, -2.8f, 0);
            Gear(gc, 2.2f, Vector3.left, 14, true);
            Pipe(new[] { gc + new Vector3(-0.3f, -2.4f, 1.2f), D + new Vector3(-rD - 0.4f, -5, 1.8f), D + new Vector3(-rD - 0.4f, -8.8f, 1.8f) }, 0.5f);
            // waterfalls off D and E into the pool
            var wn = new Vector3(-0.45f, 0, 0.89f).normalized;
            Waterfall(D + wn * (rD - 0.3f) + Vector3.down * 0.3f, wn, 2.4f);
            Waterfall(E + new Vector3(rE - 0.3f, -0.3f, 0), Vector3.right, 2.2f);
            new GameObject("SideMist").AddComponent<Mist>().transform.position = V(36.5f, -2.4f, 70.5f);

            // greenery on the pillar tops (decoration, at the rims so nothing blocks the way)
            Tree(A + new Vector3(-1.8f, 0, -2f), 3f, 2f);
            Tree(C + new Vector3(2f, 0, 2f), 3.4f, 2.2f);
            Tree(E + new Vector3(1.2f, 0, 2f), 3.8f, 2.4f);
            Tree(D + new Vector3(2.6f, 0, -2.3f), 3.6f, 2.4f);

            // friends in cages
            Rescue.SpawnCage("w5c8", A + new Vector3(-0.6f, 0, 1.9f), -100, R);
            Rescue.SpawnCage("w5c9", Bp + new Vector3(1.5f, 0, -1.4f), -60, R);
            Rescue.SpawnCage("w5c10", C + new Vector3(1.9f, 0, 0.3f), -90, R);
            Rescue.SpawnCage("w5c11", D + new Vector3(-1.8f, 0, 2.2f), 140, R);

            // their keys
            var abA = Rim(A, rA, Bp); var abB = Rim(Bp, rB, A);
            Pickups.I.Key("w5k8", (abA + abB) / 2 + Vector3.up * 1.1f);         // on the A–B bridge
            Pickups.I.Key("w5k9", T2 + Vector3.up * 1.1f);                      // on the high stepping stone
            Pickups.I.Key("w5k10", tw + Vector3.up * 2.7f);                     // on top of the lookout tower
            Pickups.I.Key("w5k11", E + new Vector3(0.3f, 1.1f, 0.8f));          // the summit rock

            // coins
            CoinLine(S0, Rim(A, rA, S0), 3);
            CoinLine(Rim(Bp, rB, C), Rim(C, rC, Bp), 3);
            CoinLine(Rim(C, rC, D), Rim(D, rD, C), 3);
            Pickups.I.Coin(T1 + Vector3.up * 1.1f);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                Pickups.I.Coin(E + new Vector3(Mathf.Cos(a) * 1.9f, 1.1f, Mathf.Sin(a) * 1.9f));
            }
            for (int k = 0; k < 3; k++) Pickups.I.Coin(D + new Vector3(1.2f, 1.1f, -1 + k * 1.2f));
        }

        /// <summary>A walkable limestone pillar standing in the pool, with a grassy top.</summary>
        static void Rock(Vector3 top, float r, int seed)
        {
            float h = top.y - (World5.O.y - 6);
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), lime, default, R);
            Cy(R, top + Vector3.up * 0.02f, new Vector3(r * 2 + 0.2f, 0.03f, r * 2 + 0.2f), grassTop);
            var rng = new Rng(seed * 17 + 3);
            for (int k = 0; k < 5; k++)   // moss tufts round the rim, hanging over the edge
            {
                float a = k * 1.256f + rng.F(-0.3f, 0.3f);
                Ls(R, top + new Vector3(Mathf.Cos(a) * (r + 0.05f), -0.45f, Mathf.Sin(a) * (r + 0.05f)), new Vector3(1.3f, 1f, 1.3f), k % 2 == 0 ? moss : leaf2);
            }
            for (int k = 0; k < 3; k++)   // rough bulges on the sides
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ls(R, top + new Vector3(Mathf.Cos(a) * r * 0.9f, -rng.F(2.5f, 6), Mathf.Sin(a) * r * 0.9f), new Vector3(r * 0.9f, rng.F(2, 3.5f), r * 0.9f), k == 0 ? limeDark : lime);
            }
            for (int k = 0; k < 3; k++)   // vines
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ca(R, top + new Vector3(Mathf.Cos(a) * (r + 0.05f), -1.5f, Mathf.Sin(a) * (r + 0.05f)), new Vector3(0.1f, 1.4f, 0.1f), leaf);
            }
        }

        /// <summary>A small floating stepping stone (walkable top).</summary>
        static void Stone(Vector3 top, float r)
        {
            Lobby.Solid(PrimitiveType.Cylinder, top - Vector3.up * 0.4f, new Vector3(r * 2, 0.4f, r * 2), lime, default, R);
            Cy(R, top + Vector3.up * 0.02f, new Vector3(r * 2 + 0.15f, 0.03f, r * 2 + 0.15f), grassTop);
            Cone(R, top - Vector3.up * 0.75f, new Vector3(r * 2, 2.2f, r * 2), lime, new Vector3(180, 0, 0));
            Ca(R, top + new Vector3(r * 0.6f, -1.8f, 0), new Vector3(0.08f, 1f, 0.08f), leaf);
        }

        /// <summary>The floor point on <paramref name="c"/>'s top facing <paramref name="to"/>, just inside the rim.</summary>
        static Vector3 Rim(Vector3 c, float r, Vector3 to)
        {
            var d = to - c; d.y = 0; d.Normalize();
            return c + d * (r - 0.4f);
        }

        static void Link(Vector3 a, float ra, Vector3 b, float rb) => Bridge(Rim(a, ra, b), Rim(b, rb, a), 1.8f, true);

        static void CoinLine(Vector3 a, Vector3 b, int n)
        {
            for (int k = 1; k <= n; k++) Pickups.I.Coin(Vector3.Lerp(a, b, k / (n + 1f)) + Vector3.up * 1f);
        }
    }
}
