using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 1 — the lobby: a sunny safe island without enemies, laid out after the player's own drawn map:
    /// hub with a river, the winking-face wardrobe on the floor, the red shop and the blue premium shop, portals to all worlds
    /// behind blue gates that open with rescued friends, a park, snow, jungle, glitch and crystal areas, 10 hidden tokens.
    /// </summary>
    public static class Lobby
    {
        internal static Transform root;
        // =====================================================================================================
        // The lobby follows the player's own drawn map (09.11): a green hub with a river, a park with a pond on
        // the left, the glitch checkerboard below it, the jungle at the bottom, the snowy valley on top with a lake
        // and a water cave, the crystal cave on the right. Thick stone walls round everything; blue gates open
        // once enough friends are rescued (ruins 0, snow 4, jungle 10, crystal 20, glitch 25).
        // =====================================================================================================

        public static readonly Vector3 Spawn = new Vector3(0, 0.3f, 4);
        public static readonly List<Vector3> TokenSpots = new List<Vector3>();
        public static readonly List<LobbyGate> Gates = new List<LobbyGate>();
        /// <summary>Spots the autotest uses.</summary>
        public static readonly Vector3 TestMushroom = new Vector3(3, 0, -4), PadFrom = new Vector3(19, 0.07f, -5), PadTo = new Vector3(-20, 4.9f, 12), CactusPos = new Vector3(-22, 0, 0);
        /// <summary>Rescued friends needed to open the way to each world.</summary>
        public static int NeedFor(int world) => world switch { 3 => 20, 4 => 4, 5 => 10, 6 => 25, _ => 0 };
        public static bool Open(int world) => Save.P.rescued.Count >= NeedFor(world);

        static Material wallM, grass, snowM, checker, waterM;

        public static Transform Build()
        {
            root = new GameObject("Lobby").transform;
            Parts.Layer = Layers.World;
            TokenSpots.Clear(); Gates.Clear();
            wallM = Tiled(Tex.Concrete(), 2, 0.2f); wallM.color = Hex("B8BEC8");
            grass = Tiled(Tex.Grass(), 10); snowM = Tiled(Tex.Snow(), 8);
            checker = Mats.Tex(World6.MissingTex(), Color.white, 0.2f); checker.mainTextureScale = new Vector2(6, 4);
            waterM = Mats.Emissive(Tex.Water(), new Color(0.6f, 0.85f, 1f), Hex("1E6FB0") * 0.25f, null, 0.9f);
            Sky();
            Sea();
            Regions();
            Rivers();
            Walls();
            Hub();
            Wardrobe(new Vector3(-5, 0, 18.2f));
            Shop(new Vector3(-6.5f, 0, -9.4f));
            Premium(new Vector3(-6.5f, 0, -17.2f));
            PortalArch(new Vector3(18, 0, 19.5f), 0, 2, "ПАРЯЩИЕ РУИНЫ", "B57CFF", M("9AA3AE", 0.2f), "ruins");
            Park();
            Glitch();
            Jungle();
            Snow();
            CrystalCave();
            Gates.Add(LobbyGate.Spawn(new Vector3(6.15f, 0, 16.3f), 0, 7.7f, 4, "СНЕЖНАЯ ДОЛИНА", root));     // between the wardrobe and ruins corners
            Gates.Add(LobbyGate.Spawn(new Vector3(26.6f, 0, 10.4f), 90, 4.4f, 4, "СНЕЖНАЯ ДОЛИНА", root));    // where the river comes in
            Gates.Add(LobbyGate.Spawn(new Vector3(19.15f, 0, -21.5f), 0, 11.1f, 10, "ДЖУНГЛИ", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(26.6f, 0, 1.5f), 90, 7f, 20, "КРИСТАЛЬНАЯ ПЕЩЕРА", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(-39.05f, 0, -6.9f), 0, 8.5f, 25, "ГЛЮК-МИР 404", root));
            Clouds();
            Tokens();
            return root;
        }

        // ---------------- ground, sea, regions ----------------
        // Map coordinates follow the drawing: x to the right, z up the sheet. A point is P(x, z, cornerRadius).

        static Vector3 P(float x, float z, float r = 0) => new Vector3(x, r, z);

        /// <summary>Turns corner points into a path with rounded corners (quadratic arcs).</summary>
        static List<Vector3> Round(Vector3[] p, bool closed)
        {
            var o = new List<Vector3>();
            int n = p.Length;
            for (int i = 0; i < n; i++)
            {
                var b = new Vector3(p[i].x, 0, p[i].z);
                float r = p[i].y;
                if ((!closed && (i == 0 || i == n - 1)) || r <= 0.01f) { o.Add(b); continue; }
                var a = new Vector3(p[(i + n - 1) % n].x, 0, p[(i + n - 1) % n].z);
                var c = new Vector3(p[(i + 1) % n].x, 0, p[(i + 1) % n].z);
                float d = Mathf.Min(r, (a - b).magnitude / 2, (c - b).magnitude / 2);
                var s = b + (a - b).normalized * d; var e = b + (c - b).normalized * d;
                int seg = Mathf.Clamp((int)(d * 1.3f), 3, 9);
                for (int k = 0; k <= seg; k++) { float t = k / (float)seg; o.Add((1 - t) * (1 - t) * s + 2 * t * (1 - t) * b + t * t * e); }
            }
            return o;
        }

        static void Sea()
        {
            var water = Prim.Make(PrimitiveType.Plane, root, new Vector3(0, PlayerCtl.I != null ? PlayerCtl.I.WaterY : -0.9f, 0), new Vector3(30, 1, 30), null, "Water", 0);
            var wm = Mats.Tex(Tex.Water(), new Color(0.75f, 0.9f, 1f), 0.9f);
            wm.mainTextureScale = new Vector2(40, 40);
            water.GetComponent<Renderer>().sharedMaterial = wm;
            water.AddComponent<WaterScroll>();
        }

        /// <summary>A flat piece of ground in the shape of a (rounded) outline: top at y 0, 2 m thick, with a collider.</summary>
        static GameObject Ground(string name, Material m, float tile, params Vector3[] pts)
        {
            var q = Round(pts, true);
            float area = 0;
            for (int i = 0; i < q.Count; i++) { var a = q[i]; var b = q[(i + 1) % q.Count]; area += a.x * b.z - b.x * a.z; }
            if (area < 0) q.Reverse();   // counter-clockwise seen from above
            int n = q.Count;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            foreach (var p in q) { v.Add(p); uv.Add(new Vector2(p.x, p.z) / tile); }
            // ear clipping
            var idx = new List<int>(); for (int i = 0; i < n; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool cut = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 A = new Vector2(q[ia].x, q[ia].z), Bp = new Vector2(q[ib].x, q[ib].z), C = new Vector2(q[ic].x, q[ic].z);
                    if (Cross(Bp - A, C - Bp) <= 1e-5f) continue;   // reflex corner
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        var P2 = new Vector2(q[j].x, q[j].z);
                        if (Cross(Bp - A, P2 - A) > 1e-4f && Cross(C - Bp, P2 - Bp) > 1e-4f && Cross(A - C, P2 - C) > 1e-4f) { inside = true; break; }
                    }
                    if (inside) continue;
                    tr.Add(ia); tr.Add(ic); tr.Add(ib);   // clockwise from above = facing up
                    idx.RemoveAt(i); cut = true; break;
                }
                if (!cut) break;
            }
            if (idx.Count == 3) { tr.Add(idx[0]); tr.Add(idx[2]); tr.Add(idx[1]); }
            // the sides, 2 m down
            for (int i = 0; i < n; i++)
            {
                var a = q[i]; var b = q[(i + 1) % n];
                int s = v.Count;
                float len = (b - a).magnitude;
                v.Add(a); v.Add(b); v.Add(a + Vector3.down * 2); v.Add(b + Vector3.down * 2);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(len / tile, 0)); uv.Add(new Vector2(0, -2 / tile)); uv.Add(new Vector2(len / tile, -2 / tile));
                // outward normal of a counter-clockwise outline is to the right of the edge
                var outN = new Vector3(b.z - a.z, 0, -(b.x - a.x));
                var nrm = Vector3.Cross(v[s + 1] - v[s], v[s + 2] - v[s]);
                if (Vector3.Dot(nrm, outN) >= 0) { tr.Add(s); tr.Add(s + 1); tr.Add(s + 2); tr.Add(s + 1); tr.Add(s + 3); tr.Add(s + 2); }
                else { tr.Add(s); tr.Add(s + 2); tr.Add(s + 1); tr.Add(s + 1); tr.Add(s + 2); tr.Add(s + 3); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.layer = Layers.World;
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>The magenta/black «missing texture» checker, repeating (for the glitch floor).</summary>
        static Texture2D CheckerTex()
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) px[y * 64 + x] = ((x / 16) + (y / 16)) % 2 == 0 ? new Color32(255, 0, 220, 255) : new Color32(20, 20, 24, 255);
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static Material GroundMat(Texture2D t, float smooth = 0.15f) => Mats.Tex(t, Color.white, smooth);

        /// <summary>The areas of the drawing, each in its own shape.</summary>
        static void Regions()
        {
            var g = GroundMat(Tex.Grass());
            // the hub with the wardrobe corner (top left), the ruins corner (top right) and the shop corner (bottom left)
            Ground("Hub", g, 4, P(-11, 23, 3), P(2.3f, 23), P(2.3f, 16.3f), P(10, 16.3f), P(10, 23.7f), P(26.6f, 23.7f), P(26.6f, -21.5f), P(-11, -21.5f, 3));
            // the park with the pond corner and the notch on its left side
            Ground("Park", g, 4, P(-50.7f, 17.5f, 3), P(-11, 17.5f), P(-11, -6.9f), P(-50.7f, -6.9f), P(-50.7f, 0.6f, 1.5f), P(-43.7f, 0.6f, 1.5f), P(-43.7f, 10.4f, 1.5f), P(-50.7f, 10.4f, 1.5f));
            // the glitch checkerboard with the portal nook on its left
            Ground("Glitch", GroundMat(CheckerTex(), 0.2f), 7, P(-50.7f, -6.9f), P(-22.6f, -6.9f), P(-22.6f, -24, 2), P(-44, -24, 2), P(-44, -16.3f, 1), P(-50.7f, -16.3f, 2));
            Ground("Jungle", g, 3.5f, P(-8.5f, -21.5f), P(26.6f, -21.5f), P(26.6f, -53, 3), P(-8.5f, -53, 3));
            // the snowy valley round the top of the hub, down the right side to the crystal cave
            Ground("Snow", GroundMat(Tex.Snow()), 7, P(-5.9f, 23), P(-5.9f, 49.8f, 4), P(47.5f, 49.8f, 4), P(47.5f, 6.8f), P(26.6f, 6.8f), P(26.6f, 23.7f), P(10, 23.7f), P(10, 16.3f), P(2.3f, 16.3f), P(2.3f, 23));
            // the crystal cave: a round grey cave right of the hub
            var cave = GroundMat(Tex.Concrete(), 0.3f); cave.color = Hex("6A6480");
            Ground("CrystalCave", cave, 4, P(26.6f, 6.8f), P(50.7f, 6.8f, 6), P(50.7f, -24, 9), P(26.6f, -24));
        }

        static void Water(Vector3 a, Vector3 b, float w)
        {
            var d = b - a; d.y = 0;
            var go = Prim.Make(PrimitiveType.Cube, root, (a + b) / 2 + Vector3.up * 0.03f, new Vector3(w, 0.04f, d.magnitude + w * 0.6f), waterM, "River", Layers.World);
            go.transform.rotation = Quaternion.LookRotation(d);
        }

        /// <summary>The river from the snowy lake down the right side, through the hub and into the jungle pool (shallow: you can wade it).</summary>
        static void Rivers()
        {
            Vector3[] p = { new Vector3(39.6f, 0, 42), new Vector3(39.6f, 0, 32), new Vector3(39, 0, 25), new Vector3(35, 0, 17), new Vector3(29, 0, 10.5f), new Vector3(22, 0, 9),
                            new Vector3(16, 0, 5), new Vector3(12, 0, -2.5f), new Vector3(10, 0, -12), new Vector3(9, 0, -21), new Vector3(10, 0, -28) };
            for (int i = 0; i < p.Length - 1; i++) Water(p[i], p[i + 1], 4.4f);
            // the lake in the snowy valley: along the top and down the right
            Prim.Make(PrimitiveType.Cube, root, new Vector3(31.8f, 0.03f, 46.6f), new Vector3(31, 0.04f, 6.4f), waterM, "Lake", Layers.World).AddComponent<WaterScroll>();
            Prim.Make(PrimitiveType.Cube, root, new Vector3(42.6f, 0.03f, 42.8f), new Vector3(9.8f, 0.04f, 10.4f), waterM, "Lake", Layers.World);
            // the jungle pool where the river ends
            Prim.Make(PrimitiveType.Cylinder, root, new Vector3(13, 0.035f, -29.5f), new Vector3(16, 0.02f, 13.5f), waterM, "Pool", Layers.World);
            // stepping stones over the river by the hub
            for (int k = 0; k < 4; k++) Solid(PrimitiveType.Cylinder, new Vector3(9.6f + k * 1.3f, 0.1f, -2.5f + k * 0.7f), new Vector3(0.9f, 0.1f, 0.9f), M("9AA0A8", 0.3f));
        }

        // ---------------- walls ----------------

        // The four lobby walls from the player's drawing: dirt with grass on top (hub, park, ruins, jungle),
        // dirt with snow on top (snowy valley), the purple/teal checker (glitch world), dark stone with pink crystals (crystal cave).
        enum WallStyle { Grass, Snow, Glitch, Crystal }
        static WallStyle style;
        static readonly Dictionary<(WallStyle, int), Material> wallMats = new Dictionary<(WallStyle, int), Material>();
        static readonly Dictionary<WallStyle, Texture2D> wallTex = new Dictionary<WallStyle, Texture2D>();
        static float crystalRun;

        static Texture2D WallTex(WallStyle st)
        {
            if (wallTex.TryGetValue(st, out var have)) return have;
            const int N = 128;
            var px = new Color32[N * N];
            var rng = new System.Random(7 + (int)st);
            Color32 outline = new Color32(47, 84, 92, 255);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float v = y / (float)N;   // 0 = bottom, 1 = top
                    Color c;
                    switch (st)
                    {
                        case WallStyle.Glitch:
                            c = ((x * 2 / N) + (y * 2 / N)) % 2 == 0 ? new Color(0.42f, 0.25f, 0.78f) : new Color(0.24f, 0.37f, 0.42f);
                            break;
                        case WallStyle.Crystal:
                            c = new Color(0.30f, 0.42f, 0.46f);
                            break;
                        default:
                        {
                            // orange dirt with marker strokes, a wavy band of grass or snow along the top
                            c = new Color(0.93f, 0.62f, 0.38f);
                            float wave = 0.80f - 0.035f * Mathf.Sin(x / (float)N * Mathf.PI * 2 * 5);
                            if (v > wave + 0.012f) c = st == WallStyle.Grass ? new Color(0.13f, 0.62f, 0.25f) : new Color(0.97f, 0.98f, 1f);
                            else if (v > wave - 0.012f) c = outline;
                            break;
                        }
                    }
                    // marker strokes: slight vertical streaks
                    float streak = ((x * 37) % 11) / 11f * 0.06f - 0.03f + (float)rng.NextDouble() * 0.04f;
                    c = new Color(Mathf.Clamp01(c.r + streak), Mathf.Clamp01(c.g + streak), Mathf.Clamp01(c.b + streak));
                    // the dark outline round each block
                    if (x < 3 || x >= N - 3 || y < 3 || y >= N - 3) c = outline;
                    if (st == WallStyle.Glitch && (Mathf.Abs(x - N / 2) < 2 || Mathf.Abs(y - N / 2) < 2)) c = outline;
                    px[y * N + x] = c;
                }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            t.SetPixels32(px); t.Apply(true, true);
            return wallTex[st] = t;
        }

        /// <summary>The wall material, its texture repeated once per ~4.5 m of wall so the blocks stay square.</summary>
        static Material WallMat(WallStyle st, float len, float h)
        {
            int rep = Mathf.Max(1, Mathf.RoundToInt(len / h * 1.25f));
            if (wallMats.TryGetValue((st, rep), out var m)) return m;
            m = Mats.Tex(WallTex(st), Color.white, st == WallStyle.Crystal ? 0.35f : 0.12f);
            m.mainTextureScale = new Vector2(rep, 1);
            return wallMats[(st, rep)] = m;
        }

        static void Wall(Vector3 a, Vector3 b, float h = 5.5f)
        {
            var d = b - a; d.y = 0;
            if (d.magnitude < 0.1f) return;
            float len = d.magnitude + 1;
            var go = Box((a + b) / 2 + Vector3.up * h / 2, new Vector3(1, h, len), WallMat(style, len, h), Quaternion.LookRotation(d).eulerAngles.y);
            string cap = style switch { WallStyle.Grass => "2E9E3E", WallStyle.Snow => "FFFFFF", WallStyle.Glitch => "6A3FC0", _ => "3E5560" };
            B(go.transform, new Vector3(0, 0.5f, 0), new Vector3(1.15f, 0.06f, 1.02f), M(cap, 0.3f));   // the top
            if (style == WallStyle.Crystal)
            {
                // pink crystal clusters sticking out of the top, like on the drawing
                crystalRun += d.magnitude;
                if (crystalRun > 5.5f)
                {
                    crystalRun = 0;
                    var p = (a + b) / 2 + Vector3.up * h;
                    var pink = G("FF8FC8", 1.1f);
                    Spike(root, p, p + new Vector3(0, 1.8f, 0), 0.55f, pink);
                    Spike(root, p + new Vector3(0.15f, 0, 0.3f), p + new Vector3(0.5f, 1.3f, 0.9f), 0.4f, pink);
                    Spike(root, p + new Vector3(-0.15f, 0, -0.3f), p + new Vector3(-0.5f, 1.3f, -0.9f), 0.4f, pink);
                }
            }
        }

        /// <summary>A wall along the drawn line, with rounded corners.</summary>
        static void WallPath(float h, params Vector3[] p)
        {
            var q = Round(p, false);
            for (int i = 0; i < q.Count - 1; i++) Wall(q[i], q[i + 1], h);
        }

        /// <summary>Every grey line of the drawing; the breaks are the blue gates and the ways through.</summary>
        static void Walls()
        {
            // the hub: left side with the shop corner, the wardrobe corner, the dip with the snow gate, the ruins corner
            style = WallStyle.Grass;
            WallPath(5.5f, P(13.6f, -21.5f), P(-11, -21.5f, 3), P(-11, -4.6f));                       // jungle line left of the gate, shop corner
            WallPath(5.5f, P(-11, -0.8f), P(-11, 23, 3), P(2.3f, 23, 2), P(2.3f, 16.3f));              // (a way into the park at the bottom), wardrobe corner
            WallPath(5.5f, P(10, 16.3f), P(10, 23.7f, 2), P(26.6f, 23.7f, 3), P(26.6f, 12.6f));        // snow gate between the corners, ruins corner
            WallPath(5.5f, P(26.6f, 8.2f), P(26.6f, 5));                                                // river gate above, crystal gate below
            WallPath(5.5f, P(26.6f, -2), P(26.6f, -53, 3), P(-8.5f, -53, 3), P(-8.5f, -21.5f));        // hub right side, the jungle
            WallPath(5.5f, P(26.6f, -21.5f), P(24.7f, -21.5f));                                         // right of the jungle gate
            WallPath(5.5f, P(-11, -5.3f), P(4, -5.3f));                                                 // the line above the shops (in by the river)
            WallPath(5.5f, P(-8.5f, -40.3f), P(2.6f, -40.3f));                                          // the jungle portal nook
            // the park, the glitch checkerboard below it
            WallPath(5.5f, P(-11, 17.5f), P(-50.7f, 17.5f, 3), P(-50.7f, 10.4f, 1.5f), P(-43.7f, 10.4f, 1.5f), P(-43.7f, 0.6f, 1.5f), P(-50.7f, 0.6f, 1.5f), P(-50.7f, -6.9f));
            style = WallStyle.Glitch;
            WallPath(5.5f, P(-50.7f, -6.9f),
                     P(-50.7f, -16.3f, 2), P(-44, -16.3f, 1), P(-44, -24, 2), P(-22.6f, -24, 2), P(-22.6f, -6.9f));
            WallPath(5.5f, P(-50.7f, -6.9f), P(-43.3f, -6.9f));                                         // glitch gate in this line
            WallPath(5.5f, P(-34.8f, -6.9f), P(-11, -6.9f));
            // the snowy valley: the big outline, the line round the portal hollow, the lake corner
            style = WallStyle.Snow;
            WallPath(5.5f, P(-5.9f, 23), P(-5.9f, 49.8f, 4), P(47.5f, 49.8f, 4), P(47.5f, 5.5f));
            WallPath(5.5f, P(-5.9f, 28.5f), P(-4, 35.5f, 3), P(3, 37, 3), P(35.5f, 37));               // the hollow is reached through the water cave
            WallPath(5.5f, P(16.5f, 49.8f), P(15.8f, 43.5f));
            // the crystal cave
            style = WallStyle.Crystal; crystalRun = 3;
            WallPath(7, P(26.6f, 6.8f), P(50.7f, 6.8f, 6), P(50.7f, -24, 9), P(26.6f, -24));
        }

        // ---------------- the hub ----------------

        static void Hub()
        {
            var stone = Tiled(Tex.Sandstone(), 4);
            Disc(new Vector3(0, 0.05f, 4), 5.5f, 0.1f, stone);
            Sign(new Vector3(-3.6f, 0, 8.8f), 0, "ЛОББИ ЧУБРИКОВ", "3D8BFF", 5f);
            Bench(new Vector3(4.5f, 0, 8), 200); Bench(new Vector3(-5, 0, 0), 100);
            Lamp(new Vector3(4, 0, 0)); Lamp(new Vector3(-4, 0, 8));
            foreach (var p in new[] { new Vector3(-9, 0, 7), new Vector3(3, 0, 13.5f), new Vector3(-8, 0, 11), new Vector3(21, 0, -10), new Vector3(22, 0, 3), new Vector3(1, 0, -16), new Vector3(15, 0, -17), new Vector3(-9.5f, 0, 14) })
                Tree(p, 0.95f, (int)(p.x * 7 + p.z));
            Flowers(new Vector3(6, 0, 2), 14, 70, 5);
            // a mushroom up to the shop roof and a teleport pad up to the tree house in the park
            Mushroom(TestMushroom, 18.5f);
            TokenSpots.Add(TestMushroom + Vector3.up * 6.2f);                   // high over the mushroom
            TokenSpots.Add(new Vector3(-9.8f, 1, 22));                           // hidden in the wardrobe corner
            Pad(PadFrom, PadTo, 0, "35E0FF");
            // training corner: crates and target dummies
            var crate = Tiled(Tex.Planks("C08040"), 1);
            var rng = new Rng(4);
            for (int k = 0; k < 5; k++)
            {
                var p = new Vector3(19.5f + (k % 3) * 1.8f, 0.55f + (k / 3) * 1.1f, -14 - (k % 2) * 1.2f);
                var b = Box(p, Vector3.one * 1.1f, crate, rng.F(-10, 10));
                B(b.transform, Vector3.zero, new Vector3(1.04f, 0.15f, 1.04f), M("6D4425"));
                b.AddComponent<Breakable>().coins = 0;
            }
            Sign(new Vector3(12, 0, 10), 200, "ПАРЯЩИЕ РУИНЫ — ВВЕРХ ПО РЕКЕ", "B57CFF", 5f);
        }

        // ---------------- buildings ----------------

        /// <summary>The wardrobe from the drawing: a flat grey square right on the floor with a face on it —
        /// a smile, one open eye and one winking eye. Step on it to open the wardrobe.</summary>
        static void Wardrobe(Vector3 c)
        {
            var grey = M("A8AEB8", 0.3f); var frame = M("7E8590", 0.3f); var dark = M("2A2E36", 0.5f); var pink = M("FF8FA8", 0.3f);
            const float W = 7.5f, D = 6f, top = 0.12f;
            Box(c + Vector3.up * top / 2, new Vector3(W, top, D), grey);
            foreach (var sd in new[] { -1, 1 })
            {
                B(root, c + new Vector3(sd * (W / 2 - 0.2f), top + 0.01f, 0), new Vector3(0.4f, 0.02f, D), frame);
                B(root, c + new Vector3(0, top + 0.01f, sd * (D / 2 - 0.2f)), new Vector3(W, 0.02f, 0.4f), frame);
            }
            float y = top + 0.012f;
            // the open eye (top left, as on the drawing)
            var eye = c + new Vector3(-1.7f, y, 1.2f);
            Cy(root, eye, new Vector3(1.8f, 0.01f, 1.8f), M("FFFFFF", 0.4f));
            Cy(root, eye + new Vector3(0.1f, 0.006f, -0.1f), new Vector3(0.9f, 0.01f, 0.9f), dark);
            Cy(root, eye + new Vector3(0.3f, 0.012f, 0.12f), new Vector3(0.28f, 0.01f, 0.28f), M("FFFFFF"));
            // the winking eye: a happy closed arc
            for (int k = 0; k < 9; k++) { float a = (k / 8f - 0.5f) * 2.4f; Cy(root, c + new Vector3(1.7f + Mathf.Sin(a) * 0.9f, y, 1.0f + Mathf.Cos(a) * 0.45f), new Vector3(0.36f, 0.01f, 0.36f), dark); }
            // the smile
            for (int k = 0; k < 15; k++) { float a = (k / 14f - 0.5f) * 2.6f; Cy(root, c + new Vector3(Mathf.Sin(a) * 2.1f, y, -0.9f - Mathf.Cos(a) * 0.75f), new Vector3(0.42f, 0.01f, 0.42f), dark); }
            foreach (var sd in new[] { -1, 1 }) Cy(root, c + new Vector3(sd * 2.85f, y, -0.3f), new Vector3(1.1f, 0.01f, 0.65f), pink);
            Sign(c + new Vector3(0, 0, D / 2 + 0.8f), 0, "ГАРДЕРОБ", "FF5FA2", 5f);
            Npc(new Vector3(c.x - W / 2 - 1.1f, 0.05f, c.z - 1.2f), 160, "hat_4", "neck_5", "top_5", "color_3", "emote_5", "shoes_4");
            Npc(new Vector3(c.x + W / 2 + 1.1f, 0.05f, c.z - 1.2f), 200, "hat_6", "wings_0", "color_7", "tail_4", "emote_1", "weapon_2");
            Zones.Add(c, 4.2f, "ГАРДЕРОБ", () => WardrobeUI.Open(), () => Worlds.Current == 1);
        }

        /// <summary>The red shop: 100 coins, extra hearts, the coin pointer.</summary>
        static void Shop(Vector3 c)
        {
            var wall = M("FFD6D6", 0.2f); var trim = M("E53935", 0.3f); var wood = Tiled(Tex.Planks("A0642F"), 2);
            var b = Node("Shop", root, c, new Vector3(0, 90, 0));   // front faces +X (towards the hub)
            Solid(PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(7, 0.2f, 5.6f), wood, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 2.1f, -2.6f), new Vector3(7, 4f, 0.4f), wall, default, b);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cube, new Vector3(sd * 3.3f, 2.1f, 0), new Vector3(0.4f, 4f, 5.6f), wall, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 4.3f, 0), new Vector3(7.6f, 0.4f, 6.2f), trim, default, b);
            var awn = Mats.Tex(Patterns.Stripes("E53935", "FFFFFF", 8, true), Color.white, 0.2f);
            B(b, new Vector3(0, 3.6f, 3.2f), new Vector3(7.2f, 0.12f, 1.4f), awn, new Vector3(-18, 0, 0));
            Solid(PrimitiveType.Cube, new Vector3(0, 0.75f, 0.8f), new Vector3(5, 1.1f, 0.8f), M("8D5524", 0.3f), default, b);
            S(b, new Vector3(-1.6f, 1.6f, 0.8f), 0.5f, M("FFC83A", 0.9f, 0.85f));
            for (int k = 0; k < 6; k++) Msh(Meshes.Heart(), b, new Vector3(-2.2f + k * 0.9f, 2.6f, -2.3f), Vector3.one * 0.5f, M("E53935", 0.4f));
            var arrow = Cone(b, new Vector3(1.6f, 1.6f, 0.8f), new Vector3(0.5f, 0.8f, 0.5f), G("6EDC3C", 1.4f));
            arrow.AddComponent<Spin>();
            var sign = Prim.Text(b, "МАГАЗИН", new Vector3(0, 4.9f, 3.15f), 6, Hex("FFFFFF"));
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Npc(b.TransformPoint(new Vector3(0, 0.2f, -0.6f)), b.eulerAngles.y + 180, "hat_0", "top_4", "color_1", "emote_6", "eyes_1");
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 2.6f)), 3.2f, "МАГАЗИН", () => ShopUI.Open(), () => Worlds.Current == 1);
            var crate = Tiled(Tex.Planks("C08040"), 1);
            Box(c + new Vector3(3.4f, 0.6f, -3.0f), new Vector3(1.3f, 1.2f, 1.3f), crate);
            TokenSpots.Add(c + new Vector3(0, 5.4f, 0));
        }

        /// <summary>The blue premium shop.</summary>
        static void Premium(Vector3 c)
        {
            var wall = M("CFE3FF", 0.3f); var trim = M("2E6BFF", 0.4f, 0.2f); var gold = M("FFC83A", 0.9f, 0.85f);
            var b = Node("PremiumShop", root, c, new Vector3(0, 90, 0));
            Solid(PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(7, 0.2f, 5.6f), M("FFF8E7", 0.4f), default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 2.1f, -2.6f), new Vector3(7, 4f, 0.4f), wall, default, b);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cube, new Vector3(sd * 3.3f, 2.1f, 0), new Vector3(0.4f, 4f, 5.6f), wall, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 4.3f, 0), new Vector3(7.6f, 0.4f, 6.2f), trim, default, b);
            var star = Msh(Meshes.Star(), b, new Vector3(0, 5.6f, 0), Vector3.one * 1.1f, G("FFE14D", 1.8f));
            star.AddComponent<Spin>();
            var stand = Node("Stand", b, new Vector3(0, 0.2f, -0.6f));
            stand.gameObject.AddComponent<Spin>().speed = 12;
            for (int k = 0; k < Catalog.Sets.Count; k++)
            {
                float a = k * Mathf.PI * 2 / 8;
                var p = new Vector3(Mathf.Cos(a) * 1.7f, 0.45f, Mathf.Sin(a) * 1.4f);
                B(stand, p, Vector3.one * 0.7f, Mats.Lit(Catalog.Sets[k].color, 0.5f, 0.2f));
                B(stand, p, new Vector3(0.74f, 0.74f, 0.14f), gold);
            }
            var sign = Prim.Text(b, "ПРЕМИУМ", new Vector3(0, 4.9f, 3.15f), 6, Hex("FFE14D"));
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 2.6f)), 3.2f, "ПРЕМИУМ-НАБОРЫ", () => PremiumUI.Open(), () => Worlds.Current == 1);
        }

        // ---------------- portals ----------------

        /// <summary>A portal arch; front (the side you walk in from) is local −Z.</summary>
        static void PortalArch(Vector3 c, float yaw, int world, string title, string col, Material pillar, string decor)
        {
            var t = Node("Portal" + world, root, c, new Vector3(0, yaw, 0));
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cube, new Vector3(sd * 3, 3, 0), new Vector3(1.2f, 6, 1.4f), pillar, default, t);
            Solid(PrimitiveType.Cube, new Vector3(0, 6.5f, 0), new Vector3(7.6f, 1, 1.6f), pillar, default, t);
            var surf = Cy(t, new Vector3(0, 3, 0), new Vector3(4.8f, 0.02f, 5.8f), Add(col, 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var s2 = Cy(t, new Vector3(0, 3, 0.05f), new Vector3(3.4f, 0.02f, 4f), Add("FFFFFF", 0.25f), new Vector3(90, 0, 0));
            s2.AddComponent<Pulse>();
            var tx = Prim.Text(t, $"МИР {world}", new Vector3(0, 6.5f, -0.85f), 8, Color.white);
            tx.transform.localRotation = Quaternion.identity;
            var t2 = Prim.Text(t, title, new Vector3(0, 0.6f, -1.2f), 4, Hex(col));
            t2.transform.localRotation = Quaternion.identity;
            switch (decor)
            {
                case "snow":
                    for (int k = 0; k < 9; k++) Spike(t, new Vector3(-3.4f + k * 0.85f, 6, -0.7f), new Vector3(-3.4f + k * 0.85f, 4.8f - (k % 3) * 0.4f, -0.72f), 0.3f, M("E8F6FF", 0.8f));
                    S(t, new Vector3(0, 7.1f, 0), new Vector3(7.8f, 0.6f, 1.8f), M("FFFFFF", 0.1f));
                    new GameObject("PortalSnow").AddComponent<PortalSnow>().transform.position = c + Vector3.up * 3;
                    break;
                case "jungle":
                    for (int k = 0; k < 8; k++) Ca(t, new Vector3(-3.4f + k * 0.97f, 5.2f, -0.82f), new Vector3(0.1f, 1f, 0.1f), M("2E9E3E", 0.2f));
                    for (int k = 0; k < 5; k++) S(t, new Vector3(-3.6f + k * 1.8f, 7.1f, 0), new Vector3(1.8f, 0.8f, 1.8f), M(k % 2 == 0 ? "2E9E3E" : "4CBF4A", 0.2f));
                    break;
                case "crystal":
                    foreach (var sd in new[] { -1, 1 }) Spike(t, new Vector3(sd * 3, 6, 0), new Vector3(sd * 3.6f, 8.4f, 0), 0.7f, G("A66BFF", 1.6f));
                    break;
            }
            Zones.Add(t.TransformPoint(new Vector3(0, 0, -1.2f)), 2.6f, $"МИР {world}: {title}", () => Worlds.Enter(world), () => Worlds.Current == 1);
        }

        // ---------------- areas ----------------

        static void Park()
        {
            // the pond in the top left corner
            var pc = new Vector3(-46.2f, 0, 14.4f);
            foreach (var o in new[] { new Vector3(-1.3f, 0, 0.9f), new Vector3(1.3f, 0, 0.9f) }) Prim.Make(PrimitiveType.Cylinder, root, pc + o + Vector3.up * 0.03f, new Vector3(3.4f, 0.02f, 3.4f), waterM, "Pond", Layers.World);
            var tip = Prim.Make(PrimitiveType.Cube, root, pc + new Vector3(0, 0.03f, -0.7f), new Vector3(3.4f, 0.04f, 3.4f), waterM, "Pond", Layers.World);
            tip.transform.rotation = Quaternion.Euler(0, 45, 0);
            Solid(PrimitiveType.Sphere, pc + new Vector3(0, 0.3f, 0.4f), new Vector3(1.4f, 0.8f, 1.4f), M("9AA0A8", 0.2f));
            TokenSpots.Add(pc + new Vector3(0, 1.3f, 0.4f));
            foreach (var p in new[] { new Vector3(-30, 0, 13), new Vector3(-37, 0, 7.8f), new Vector3(-30, 0, 3.3f), new Vector3(-40, 0, -2), new Vector3(-16, 0, 6.5f), new Vector3(-36, 0, 14) })
                Tree(p, 1f, (int)(p.x * 3 - p.z));
            Flowers(new Vector3(-30, 0, 6), 13, 90, 9);
            Bench(new Vector3(-36, 0, 3), 0); Bench(new Vector3(-26, 0, 15.5f), 180);
            TreeHouse(new Vector3(-20, 0, 12));
            // the training cactus (bumps you back, no hearts lost here)
            var cac = Node("Cactus", root, CactusPos);
            var green = M("43A047", 0.3f);
            Solid(PrimitiveType.Capsule, new Vector3(0, 1.3f, 0), new Vector3(1f, 1.3f, 1f), green, default, cac);
            foreach (var sd in new[] { -1, 1 })
            {
                Ca(cac, new Vector3(sd * 0.7f, 1.4f, 0), new Vector3(0.45f, 0.3f, 0.45f), green, new Vector3(0, 0, 90));
                Ca(cac, new Vector3(sd * 0.95f, 1.85f, 0), new Vector3(0.42f, 0.4f, 0.42f), green);
            }
            S(cac, new Vector3(0, 2.75f, 0), 0.4f, M("FF5FA2", 0.3f));
            foreach (var sd in new[] { -1, 1 }) { S(cac, new Vector3(sd * 0.18f, 1.9f, 0.45f), 0.18f, M("FFFFFF")); S(cac, new Vector3(sd * 0.18f, 1.9f, 0.52f), 0.09f, M("111111", 0.9f)); }
            cac.gameObject.AddComponent<Hurter>();
            Sign(CactusPos + new Vector3(0, 0, 2.5f), 180, "КАКТУС-ТРЕНЕР: НЕ БОЛЬНО", "43A047", 4.6f);
            for (int k = 0; k < 3; k++)
            {
                var d = Node("Dummy", root, new Vector3(-29 + k * 2.4f, 0, -3.5f));
                Solid(PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(0.2f, 0.6f, 0.2f), M("8D5524"), default, d);
                Solid(PrimitiveType.Sphere, new Vector3(0, 1.5f, 0), new Vector3(0.9f, 1.1f, 0.9f), M("F2D7A0", 0.1f), default, d);
                Cy(d, new Vector3(0, 1.6f, 0.45f), new Vector3(0.5f, 0.01f, 0.5f), Mats.UnlitTex(Tex.Target(), Color.white), new Vector3(90, 0, 0));
                d.gameObject.AddComponent<Breakable>().dummy = true;
                d.GetComponent<Breakable>().coins = 0;
            }
        }

        static void Glitch()
        {
            var r = new Rng(66);
            for (int k = 0; k < 6; k++)
                Box(new Vector3(r.F(-40, -26), 0.8f + (k % 2) * 0.8f, r.F(-21, -11)), new Vector3(1.6f, 1.6f + (k % 2) * 1.6f, 1.6f), checker, r.F(0, 45));
            TokenSpots.Add(new Vector3(-30, 5, -14));
            Mushroom(new Vector3(-30, 0, -19), 17f);
            // the portal in the little nook on the left, facing the checkerboard
            PortalArch(new Vector3(-47.6f, 0, -11.6f), 270, 6, "ГЛЮК-МИР 404", "FF5FD2", checker, "glitch");
        }

        static void Jungle()
        {
            foreach (var p in new[] { new Vector3(-4.5f, 0, -26), new Vector3(24, 0, -27.5f), new Vector3(14, 0, -43), new Vector3(-4.5f, 0, -34), new Vector3(23, 0, -48), new Vector3(1, 0, -29) })
                Tree(p, 1.15f, (int)(p.z * 5));
            // the striped posts from the drawing
            foreach (var p in new[] { new Vector3(0.5f, 0, -36), new Vector3(21, 0, -35) })
            {
                var stripes = Mats.Tex(Patterns.Stripes("D9822B", "6B4A2A", 6, true), Color.white, 0.3f);
                Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.6f, new Vector3(0.8f, 1.6f, 0.8f), stripes);
                Disc(p + Vector3.up * 3.3f, 0.7f, 0.2f, M("6B4A2A", 0.3f));
            }
            TokenSpots.Add(new Vector3(21, 4.2f, -35));
            Mushroom(new Vector3(18, 0, -40), 15f);
            Flowers(new Vector3(10, 0, -47), 7, 60, 13);
            // the portal in its nook at the bottom left, facing the jungle
            PortalArch(new Vector3(-4.2f, 0, -46.6f), 270, 5, "ДЖУНГЛИ", "6EDC3C", M("8A8A6A", 0.2f), "jungle");
        }

        static void Snow()
        {
            foreach (var p in new[] { new Vector3(1.3f, 0, 28), new Vector3(10.4f, 0, 27.3f), new Vector3(20.8f, 0, 28.6f), new Vector3(30, 0, 33), new Vector3(40.5f, 0, 10),
                                      new Vector3(-1.5f, 0, 41), new Vector3(12.5f, 0, 40) })
            {
                Pine(p, 1.2f);
                S(root, p + Vector3.up * 3.2f, new Vector3(1.6f, 0.4f, 1.6f), M("FFFFFF", 0.1f));
            }
            var rng = new Rng(51);
            for (int k = 0; k < 10; k++) S(root, new Vector3(rng.F(-3, 32), 0, rng.F(25, 34)), new Vector3(rng.F(1.2f, 2.6f), rng.F(0.5f, 1f), rng.F(1.2f, 2.6f)), M("F4FAFF", 0.15f));
            // a snowman by the portal
            var sm = new Vector3(11, 0, 44.5f);
            S(root, sm + Vector3.up * 0.6f, 1.2f, M("FFFFFF", 0.15f)); S(root, sm + Vector3.up * 1.45f, 0.85f, M("FFFFFF", 0.15f)); S(root, sm + Vector3.up * 2.1f, 0.6f, M("FFFFFF", 0.15f));
            Cone(root, sm + new Vector3(0, 2.1f, -0.3f), new Vector3(0.1f, 0.3f, 0.1f), M("FF8A1E"), new Vector3(-90, 0, 0));
            // the portal in the hollow at the top: you get there through the water cave
            PortalArch(new Vector3(5.2f, 0, 44.2f), 0, 4, "СНЕЖНАЯ ДОЛИНА", "8FD8FF", Glass("A8DCFF", 0.85f), "snow");
            TokenSpots.Add(new Vector3(-3, 1, 47.5f));
            // the water cave: a rocky tunnel along the south shore of the lake, from the river to the hollow
            var rock = M("8A93A0", 0.2f);
            for (float x = 17; x <= 36; x += 3.8f)
            {
                B(root, new Vector3(x, 5.9f, 40.2f), new Vector3(2.4f, 1.2f, 7.6f), rock);
                S(root, new Vector3(x + 1.9f, 6.3f, 38.6f), new Vector3(4.2f, 2f, 3.6f), rock);
                Spike(root, new Vector3(x + 1.2f, 5.4f, 41.5f), new Vector3(x + 1.2f, 4.2f, 41.5f), 0.3f, M("BFE8FF", 0.8f));
                S(root, new Vector3(x, 0.4f, 43.3f), new Vector3(1.6f, 0.9f, 1.3f), rock);
            }
            for (int k = 0; k < 4; k++) S(root, new Vector3(19 + k * 5.5f, 3.6f, 37.9f), 0.5f, G("7FE3FF", 1.6f));
            // the rock on the far side of the river
            Box(new Vector3(44.6f, 2.5f, 32.4f), new Vector3(5.8f, 5, 10.6f), rock);
            for (int k = 0; k < 3; k++) S(root, new Vector3(44.6f, 5f, 28.6f + k * 3.6f), new Vector3(6.4f, 2.4f, 4f), rock);
            TokenSpots.Add(new Vector3(26, 1, 40.5f));
            Sign(new Vector3(33.4f, 0, 35.6f), 0, "ПЕЩЕРА С ВОДОЙ", "35E0FF", 4);
        }

        static void CrystalCave()
        {
            var c = new Vector3(38.6f, 0, -8.6f);
            // a partly open rocky roof
            var rock = M("6E6488", 0.2f);
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; S(root, c + new Vector3(Mathf.Cos(a) * 7.5f, 7.4f, Mathf.Sin(a) * 9), new Vector3(7, 2.2f, 7), rock); }
            // the big red crystals from the drawing
            var red = G("E5395A", 1.4f);
            foreach (var p in new[] { new Vector3(47, 0, -3), new Vector3(46.5f, 0, -15), new Vector3(36, 0, -20.5f), new Vector3(30.5f, 0, -17) })
                for (int k = 0; k < 4; k++)
                {
                    var b = p + new Vector3((k % 2 - 0.5f) * 1.2f, 0, (k / 2 - 0.5f) * 1.2f);
                    Spike(root, b, b + new Vector3((k % 2 - 0.5f), 2.6f + k * 0.4f, (k / 2 - 0.5f)), 1.1f, red);
                }
            TokenSpots.Add(new Vector3(42, 4.5f, -17));
            Mushroom(new Vector3(43, 0, -11), 15f);
            // the portal faces the hub (west), just inside the gate
            PortalArch(new Vector3(36, 0, -7), 90, 3, "КРИСТАЛЬНАЯ ПЕЩЕРА", "35E0FF", rock, "crystal");
        }

        static void Clouds()
        {
            var r = new Rng(21);
            var white = M("FFFFFF", 0.05f);
            for (int i = 0; i < 12; i++)
            {
                var c = Node("Cloud", root, new Vector3(r.F(-120, 120), r.F(38, 60), r.F(-120, 120)));
                for (int k = 0; k < 5; k++) S(c, new Vector3((k - 2) * 2.4f, r.F(-0.5f, 1f), r.F(-1, 1)), r.F(3, 5.5f), white);
                var mv = c.gameObject.AddComponent<Mover>(); mv.a = c.position; mv.b = c.position + new Vector3(r.F(15, 30), 0, r.F(-5, 5)); mv.period = r.F(60, 120); mv.phase = r.F();
            }
        }

        static void TreeHouse(Vector3 c)
        {
            Solid(PrimitiveType.Cylinder, c + Vector3.up * 4f, new Vector3(0.9f, 4f, 0.9f), Mats.Tex(Tex.Bark(), Color.white, 0.1f));
            foreach (var o in new[] { new Vector3(0, 9.2f, 0), new Vector3(1.6f, 8.4f, 0.6f), new Vector3(-1.4f, 8.6f, -0.8f), new Vector3(0.2f, 8.3f, -1.7f) })
                S(root, c + o, 3.6f, M("43A047", 0.15f));
            var wood = Tiled(Tex.Planks("A0642F"), 1);
            Disc(c + Vector3.up * 4.6f, 2.6f, 0.3f, wood);
            for (int k = 0; k < 5; k++)
            {
                float a = (k * 50 + 200) * Mathf.Deg2Rad;
                Box(c + new Vector3(Mathf.Cos(a) * 3.4f, 0.5f + k * 0.85f, Mathf.Sin(a) * 3.4f), new Vector3(1.2f, 0.25f, 1.2f), wood, -k * 50);
            }
            TokenSpots.Add(c + new Vector3(1.4f, 5.6f, 1.2f));
        }

        static void Tokens()
        {
            while (TokenSpots.Count > 10) TokenSpots.RemoveAt(TokenSpots.Count - 1);
            for (int i = 0; i < TokenSpots.Count; i++) Pickups.I.Token("t" + i, TokenSpots[i]);
        }

        // ---------------- helpers ----------------

        internal static GameObject Solid(PrimitiveType t, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default, Transform parent = null)
        {
            Parts.Layer = Layers.World;
            var go = Prim.Make(t, parent ?? root, pos, scale, m, t.ToString(), Layers.World);
            go.transform.localEulerAngles = euler;
            switch (t)
            {
                case PrimitiveType.Cube: go.AddComponent<BoxCollider>(); break;
                case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                case PrimitiveType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                default: go.AddComponent<MeshCollider>().sharedMesh = Prim.MeshOf(t); break;
            }
            return go;
        }

        internal static GameObject Box(Vector3 pos, Vector3 size, Material m, float yaw = 0) => Solid(PrimitiveType.Cube, pos, size, m, new Vector3(0, yaw, 0));
        internal static GameObject Disc(Vector3 top, float r, float h, Material m) => Solid(PrimitiveType.Cylinder, top - Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), m);

        internal static Material Tiled(Texture2D t, float tile, float smooth = 0.15f)
        {
            var m = Mats.Tex(t, Color.white, smooth);
            m.mainTextureScale = new Vector2(tile, tile);
            return m;
        }

        internal static void Sign(Vector3 pos, float yaw, string text, string col = "FFC83A", float w = 4f)
        {
            var s = Node("Sign", root, pos, new Vector3(0, yaw, 0));
            var wood = M("8D5524", 0.2f);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cylinder, new Vector3(sd * (w / 2 - 0.2f), 0.9f, 0), new Vector3(0.18f, 0.9f, 0.18f), wood, default, s);
            B(s, new Vector3(0, 1.9f, 0), new Vector3(w, 0.9f, 0.14f), wood);
            B(s, new Vector3(0, 1.9f, -0.075f), new Vector3(w - 0.2f, 0.72f, 0.02f), M(col, 0.3f));
            var t = Prim.Text(s, text, new Vector3(0, 1.9f, -0.1f), 4.2f, Color.white);
            t.rectTransform.sizeDelta = new Vector2(w - 0.3f, 0.8f);
            t.enableAutoSizing = true; t.fontSizeMin = 1; t.fontSizeMax = 4.2f;
            t.transform.localRotation = Quaternion.identity;
        }

        internal static ChubrikModel Npc(Vector3 pos, float yaw, params string[] items)
        {
            var look = Look.Default();
            foreach (var id in items) { var it = Catalog.Get(id); if (it != null) look[it.dept] = id; }
            var m = ChubrikModel.Create("NPC", root, look, Layers.World);
            m.transform.position = pos;
            m.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var a = m.gameObject.AddComponent<ChubrikAnim>();
            a.model = m; a.emote = Mathf.Max(0, look.Idx("emote")); a.Preview = true; a.PlayEmote();
            var col = m.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.75f, 0); col.height = 1.5f; col.radius = 0.4f;
            Parts.Layer = Layers.World;
            return m;
        }

        // ---------------- sky, light ----------------

        static void Sky()
        {
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null)
            {
                sky = new Material(sky);
                sky.SetColor("_SkyTint", Hex("6EB9FF"));
                sky.SetColor("_GroundColor", Hex("A9D8FF"));
                sky.SetFloat("_AtmosphereThickness", 0.75f);
                sky.SetFloat("_Exposure", 1.35f);
                sky.SetFloat("_SunSize", 0.05f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("BFE3FF");
            RenderSettings.ambientEquatorColor = Hex("CFE8D0");
            RenderSettings.ambientGroundColor = Hex("7A8F5A");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("CDEBFF");
            RenderSettings.fogStartDistance = 90; RenderSettings.fogEndDistance = 260;
            var lg = new GameObject("Sun");
            lg.transform.SetParent(root, false);
            lg.transform.rotation = Quaternion.Euler(52, -35, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = Hex("FFF4DE"); l.intensity = 1.25f;
            l.shadows = LightShadows.Soft; l.shadowStrength = 0.6f;
            RenderSettings.sun = l;
        }

        static void Bench(Vector3 p, float yaw)
        {
            var wood = M("A0642F", 0.2f); var iron = M("3A3F48", 0.6f, 0.5f);
            var b = Node("Bench", root, p, new Vector3(0, yaw, 0));
            Solid(PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(2, 0.1f, 0.6f), wood, default, b);
            B(b, new Vector3(0, 0.8f, -0.28f), new Vector3(2, 0.35f, 0.08f), wood);
            foreach (var sd in new[] { -1, 1 }) B(b, new Vector3(sd * 0.85f, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.5f), iron);
        }

        static void Lamp(Vector3 p)
        {
            var iron = M("2E3440", 0.6f, 0.5f);
            Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.6f, new Vector3(0.14f, 1.6f, 0.14f), iron);
            S(root, p + Vector3.up * 3.35f, 0.5f, G("FFF2C0", 1.4f));
            Cone(root, p + Vector3.up * 3.55f, new Vector3(0.6f, 0.3f, 0.6f), iron);
        }

        internal static void Pad(Vector3 pos, Vector3 to, float yaw, string col)
        {
            Disc(pos + Vector3.up * 0.05f, 0.9f, 0.12f, M("2E3440", 0.6f, 0.4f));
            var ring = Ring(root, pos + Vector3.up * 0.1f, 0.75f, 0.07f, G(col, 2.2f));
            ring.AddComponent<Pulse>();
            var glow = Cy(root, pos + Vector3.up * 0.9f, new Vector3(1.4f, 0.8f, 1.4f), Add(col, 0.18f));
            glow.AddComponent<Pulse>().amount = 0.05f;
            var t = new GameObject("Teleporter").AddComponent<Teleporter>();
            t.transform.position = pos; t.target = to; t.faceYaw = yaw;
        }

        internal static void Mushroom(Vector3 p, float power)
        {
            var stem = Solid(PrimitiveType.Cylinder, p + Vector3.up * 0.5f, new Vector3(0.7f, 0.5f, 0.7f), M("FFF3D6", 0.3f));
            var capRoot = Node("Cap", root, p + Vector3.up * 1.0f);
            var cap = Prim.Make(PrimitiveType.Sphere, capRoot, new Vector3(0, 0.1f, 0), new Vector3(2.4f, 1.1f, 2.4f), M("E53935", 0.45f), "Cap", Layers.World);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                S(cap.transform, new Vector3(Mathf.Cos(a) * 0.32f, 0.3f, Mathf.Sin(a) * 0.32f), 0.18f, M("FFFFFF"));
            }
            S(cap.transform, new Vector3(0, 0.48f, 0), 0.2f, M("FFFFFF"));
            var col = capRoot.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.2f, 0); col.size = new Vector3(2.2f, 0.6f, 2.2f);
            var pad = capRoot.gameObject.AddComponent<BouncePad>();
            pad.power = power; pad.top = cap.transform;
        }

        static void Island(Vector3 top, float r)
        {
            Disc(top, r, 0.6f, Tiled(Tex.Grass(), 3));
            Cone(root, top + Vector3.down * 0.6f, new Vector3(r * 2, r * 1.4f, r * 2), M("8D6E63", 0.2f), new Vector3(180, 0, 0));
            Flowers(top, r * 0.8f, 12, 77 + (int)top.x);
            Tree(top + new Vector3(r * 0.5f, 0, -r * 0.3f), 0.7f, 3);
        }

        static void Fence(Vector3 a, Vector3 b)
        {
            var wood = M("FFFFFF", 0.3f);
            int n = Mathf.CeilToInt(Vector3.Distance(a, b) / 1.2f);
            for (int i = 0; i <= n; i++) B(root, Vector3.Lerp(a, b, (float)i / n) + Vector3.up * 0.5f, new Vector3(0.12f, 1, 0.12f), wood);
            foreach (var h in new[] { 0.35f, 0.75f }) Rod(root, a + Vector3.up * h, b + Vector3.up * h, 0.07f, wood);
        }

        // ---------------- nature ----------------

        internal static void Tree(Vector3 p, float s, int seed)
        {
            var r = new Rng(seed);
            Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.6f * s, new Vector3(0.5f * s, 1.6f * s, 0.5f * s), Mats.Tex(Tex.Bark(), Color.white, 0.1f));
            string[] greens = { "4CAF50", "66BB6A", "43A047", "7CB342" };
            for (int k = 0; k < 4; k++)
            {
                var o = new Vector3(r.F(-0.8f, 0.8f), 3.4f + r.F(0, 1.4f), r.F(-0.8f, 0.8f)) * s;
                S(root, p + o, r.F(2f, 2.8f) * s, M(greens[r.I(greens.Length)], 0.15f));
            }
            if (r.Chance(0.4f)) for (int k = 0; k < 4; k++) S(root, p + new Vector3(r.F(-1.2f, 1.2f), r.F(3.6f, 4.8f), r.F(-1.2f, 1.2f)) * s, 0.25f * s, M("FF5252", 0.5f));
        }

        internal static void Pine(Vector3 p, float s)
        {
            Solid(PrimitiveType.Cylinder, p + Vector3.up * 0.8f * s, new Vector3(0.4f * s, 0.8f * s, 0.4f * s), M("6D4C41"));
            for (int k = 0; k < 3; k++) Cone(root, p + Vector3.up * (1.3f + k * 1.2f) * s, new Vector3((3 - k * 0.7f) * s, 2.2f * s, (3 - k * 0.7f) * s), M("2E7D32", 0.15f));
        }

        internal static void Bush(Vector3 p, float s)
        {
            for (int k = 0; k < 3; k++) S(root, p + new Vector3((k - 1) * 0.5f * s, 0.5f * s, (k % 2) * 0.3f * s), 1.2f * s, M("388E3C", 0.15f));
        }

        internal static void Flowers(Vector3 c, float r, int n, int seed)
        {
            var rng = new Rng(seed);
            var g = new Geo();
            string[] cols = { "FF5252", "FFD23A", "FFFFFF", "FF5FA2", "9B6BFF", "35A8FF" };
            var stem = M("2E7D32");
            var sphere = Prim.MeshOf(PrimitiveType.Sphere);
            for (int i = 0; i < n; i++)
            {
                float a = rng.F(0, Mathf.PI * 2), d = Mathf.Sqrt(rng.F()) * r;
                var p = c + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                g.Cyl(stem, p, 0.03f, 0.35f, 5);
                var m = M(cols[rng.I(cols.Length)], 0.3f);
                for (int k = 0; k < 5; k++)
                {
                    float b = k * Mathf.PI * 2 / 5;
                    g.Msh(m, sphere, Matrix4x4.TRS(p + new Vector3(Mathf.Cos(b) * 0.09f, 0.38f, Mathf.Sin(b) * 0.09f), Quaternion.identity, Vector3.one * 0.13f));
                }
                g.Msh(M("FFC400"), sphere, Matrix4x4.TRS(p + Vector3.up * 0.39f, Quaternion.identity, Vector3.one * 0.09f));
            }
            g.Build("Flowers", root, false, Layers.World);
        }

    }

    /// <summary>
    /// A blue gate in the lobby wall: closed until enough friends are rescued (counted over all worlds).
    /// </summary>
    public class LobbyGate : MonoBehaviour
    {
        public int need;
        public bool IsOpen { get; private set; }
        Collider col; Transform panel; TMPro.TextMeshPro front, back; string title; int shown = -1; float openT = -1, hintT;

        public static LobbyGate Spawn(Vector3 p, float yaw, float width, int need, string title, Transform parent)
        {
            var g = new GameObject("Gate " + title).AddComponent<LobbyGate>();
            g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            g.need = need; g.title = title;
            Parts.Layer = Layers.World;
            var bc = g.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 2.75f, 0); bc.size = new Vector3(width, 5.5f, 0.6f);
            g.col = bc;
            g.panel = Parts.Node("Panel", g.transform, Vector3.zero);
            Parts.B(g.panel, new Vector3(0, 2.75f, 0), new Vector3(width, 5.5f, 0.3f), Parts.Glass("2E6BFF", 0.55f));
            Parts.B(g.panel, new Vector3(0, 5.45f, 0), new Vector3(width + 0.2f, 0.2f, 0.4f), Parts.G("7DB8FF", 1.4f));
            for (int k = 0; k < (int)(width / 1.2f); k++) Parts.B(g.panel, new Vector3(-width / 2 + 0.6f + k * 1.2f, 2.75f, 0), new Vector3(0.08f, 5.4f, 0.34f), Parts.G("9FD0FF", 0.9f));
            g.front = Prim.Text(g.panel, "", new Vector3(0, 3.2f, -0.2f), 3.4f, Color.white);
            g.front.transform.localRotation = Quaternion.identity;
            g.back = Prim.Text(g.panel, "", new Vector3(0, 3.2f, 0.2f), 3.4f, Color.white);
            g.back.transform.localRotation = Quaternion.Euler(0, 180, 0);
            g.Refresh(true);
            return g;
        }

        void Refresh(bool instant)
        {
            int n = Save.P.rescued.Count;
            if (n == shown) return;
            shown = n;
            string txt = $"{title}\nСПАСИ {need} ДРУЗЕЙ\n{Mathf.Min(n, need)} / {need}";
            front.text = back.text = txt;
            if (n >= need && !IsOpen)
            {
                IsOpen = true; col.enabled = false;
                if (instant) panel.gameObject.SetActive(false);
                else { openT = 0; AudioFx.Play("portal"); Effects.Emit("magic", transform.position + Vector3.up * 3, 60, 1.2f, Vector3.up * 2); Hud.Toast($"Открылся путь: {title}!", UIKit.Lime); }
            }
            else if (n < need && IsOpen) { IsOpen = false; col.enabled = true; panel.gameObject.SetActive(true); panel.localPosition = Vector3.zero; }
        }

        void Update()
        {
            Refresh(false);
            if (openT >= 0)
            {
                openT += Time.deltaTime;
                panel.localPosition = Vector3.down * openT * 4;
                if (openT > 1.5f) { panel.gameObject.SetActive(false); openT = -1; }
            }
            var p = PlayerCtl.I;
            if (IsOpen || p == null || Worlds.Current != 1) return;
            hintT -= Time.deltaTime;
            var l = transform.InverseTransformPoint(p.transform.position);
            if (Mathf.Abs(l.z) < 2.2f && Mathf.Abs(l.x) < 4 && hintT <= 0)
            {
                hintT = 5;
                Hud.Toast($"Закрыто! Спаси ещё {need - Save.P.rescued.Count} друзей — и проход в «{title}» откроется", UIKit.Blue);
            }
        }
    }

    /// <summary>Fountain water spray.</summary>
    public class FountainFx : MonoBehaviour
    {
        float acc;
        void Update()
        {
            acc += Time.deltaTime * 30;
            int n = (int)acc; acc -= n;
            if (n > 0) Effects.Emit("splash", transform.position, n, 0.15f, Vector3.up * 3.5f);
        }
    }

    /// <summary>Snowflakes swirling round the ice portal in the lobby.</summary>
    public class PortalSnow : MonoBehaviour
    {
        float acc;
        void Update()
        {
            if (Worlds.Current != 1) return;
            acc += Time.deltaTime * 14;
            int n = (int)acc; acc -= n;
            for (int i = 0; i < n; i++)
            {
                float a = Time.time * 2 + i * 1.7f;
                Effects.Emit("snow", transform.position + new Vector3(Mathf.Cos(a) * 3.2f, Random.Range(-2.5f, 3f), Mathf.Sin(a) * 1.2f), 1, 0.2f, new Vector3(-Mathf.Sin(a), -0.4f, Mathf.Cos(a)) * 1.2f);
            }
        }
    }
}
