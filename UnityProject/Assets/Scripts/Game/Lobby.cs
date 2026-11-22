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
        /// <summary>21.11: World 67 and World 8 stand in their own area, the «Neon quarter» west of the park.</summary>
        public static readonly Vector3 Portal67 = new Vector3(-76, 0, 7);
        /// <summary>The entrance to the neon quarter: an opening in the park's wall with a neon arch over it.</summary>
        public static readonly Vector3 NeonGate = new Vector3(-43.7f, 0, 5.5f);
        /// <summary>Where the corridor from the entrance opens into the neon square (both portals face it).</summary>
        static readonly Vector3 NeonDoor = new Vector3(-50.7f, 0, 5.5f);
        /// <summary>The football pad looks for free ground round this spot (in the neon quarter, near World 67).</summary>
        public static readonly Vector3 FootballNear = new Vector3(-64, 0, 13);
        public static int NeedFor(int world) => world switch { 3 => 20, 4 => 4, 5 => 10, 6 => 25, 8 => 30, _ => 0 };
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
            posts.Clear();
            building = true;
            Sky();
            PlanRiver();               // 22.11 the river is a real channel: planned first, the ground and the sea leave room for it
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
            NeonQuarter();             // 21.11 World 67 and World 8 in their own area
            Jungle();
            Snow();
            CrystalCave();
            Gates.Add(LobbyGate.Spawn(new Vector3(6.15f, 0, 16.3f), 0, 7.7f, 4, "СНЕЖНАЯ ДОЛИНА", root));     // between the wardrobe and ruins corners
            var riverGate = LobbyGate.Spawn(new Vector3(26.6f, 0, 10.4f), 90, 4.4f, 4, "СНЕЖНАЯ ДОЛИНА", root);    // where the river comes in
            riverGate.Grate(RiverDepth + 0.5f);   // 22.11 a grate under it: you can't swim under a closed gate
            Gates.Add(riverGate);
            Gates.Add(LobbyGate.Spawn(new Vector3(19.15f, 0, -21.5f), 0, 11.1f, 10, "ДЖУНГЛИ", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(26.6f, 0, 1.5f), 90, 7f, 20, "КРИСТАЛЬНАЯ ПЕЩЕРА", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(-39.05f, 0, -6.9f), 0, 8.5f, 25, "ГЛЮК-МИР 404", root));
            // 22.11 the river goes on through the hub–jungle wall into the jungle pool: a jungle gate with a grate down
            // to the riverbed shuts the passage until the jungle is open (it opens together with the jungle gate)
            JungleRiverGate = LobbyGate.Spawn(JunglePass, 0, JunglePassW, NeedFor(5), "ДЖУНГЛИ", root);
            JungleRiverGate.Grate(RiverDepth + 0.5f);
            Gates.Add(JungleRiverGate);
            Clouds();
            Tokens();
            MakeSolid();
            Football.BuildPad(root);   // 18.11 (21.11: in the neon quarter)
            Portal8();                 // 19.11 the portal to World 8 (21.11: at its spot in the neon quarter)
            Eggs.SpawnLobby();
            Bird.ForWorld(1);
            building = false;
            AdminPanel.Restore();   // 18.11 the admin panel's copies
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
            // 22.11: the sea (300 × 300 m) has a hole under the river: the channel is deeper than the sea level,
            // and the island's ground covers the hole everywhere else
            // (22.11 later: the river reaches the snowy lake and the jungle pool, so its bounding box would stick out of
            // the island — the hole follows the channel itself in 1 m cells under the island instead)
            float y = PlayerCtl.I != null ? PlayerCtl.I.WaterY : -0.9f;
            const float H = 150;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            void Quad(float x0, float z0, float x1, float z1)
            {
                if (x1 - x0 < 0.01f || z1 - z0 < 0.01f) return;
                int b = v.Count;
                foreach (var (x, z) in new[] { (x0, z0), (x0, z1), (x1, z1), (x1, z0) }) { v.Add(new Vector3(x, 0, z)); uv.Add(new Vector2((x + H) / (2 * H), (z + H) / (2 * H))); }
                tr.Add(b); tr.Add(b + 1); tr.Add(b + 2); tr.Add(b); tr.Add(b + 2); tr.Add(b + 3);
            }
            if (river == null) Quad(-H, -H, H, H);
            else
            {
                const float X0 = -86, X1 = 54, Z0 = -56, Z1 = 52, C = 1;   // the island with a margin, cut in 1 m cells
                Quad(-H, -H, H, Z0); Quad(-H, Z1, H, H);
                Quad(-H, Z0, X0, Z1); Quad(X1, Z0, H, Z1);
                int nx = Mathf.RoundToInt((X1 - X0) / C), nz = Mathf.RoundToInt((Z1 - Z0) / C);
                for (int j = 0; j < nz; j++)
                {
                    float z = Z0 + j * C; int run = -1;
                    for (int i = 0; i <= nx; i++)
                    {
                        // a cell stays sea when it lies wholly outside the channel; rows are merged into strips
                        bool sea = i < nx && river.Edge(new Vector3(X0 + (i + 0.5f) * C, 0, z + C / 2)) > C * 0.75f;
                        if (sea && run < 0) run = i;
                        else if (!sea && run >= 0) { Quad(X0 + run * C, z, X0 + i * C, z + C); run = -1; }
                    }
                }
            }
            var mesh = new Mesh { name = "Sea" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var wm = Mats.Tex(Tex.Water(), new Color(0.75f, 0.9f, 1f), 0.9f);
            wm.mainTextureScale = new Vector2(40, 40);
            var water = Prim.MeshGO(mesh, root, wm, "Water");
            water.transform.localPosition = new Vector3(0, y, 0);
            water.AddComponent<WaterScroll>();
        }

        /// <summary>A flat piece of ground in the shape of a (rounded) outline: top at y 0, 2 m thick, with a collider.
        /// 22.11: where the river channel runs, the ground has no floor (the outline is cut along the channel's rim).</summary>
        static GameObject Ground(string name, Material m, float tile, params Vector3[] pts)
        {
            var q = Round(pts, true);
            if (Area2(q) < 0) q.Reverse();   // counter-clockwise seen from above
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            foreach (var piece in Carve(q, out bool carved)) Fill(piece, tile, carved, v, uv, tr);
            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
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

        static float Area2(List<Vector3> q)
        {
            float area = 0;
            for (int i = 0; i < q.Count; i++) { var a = q[i]; var b = q[(i + 1) % q.Count]; area += a.x * b.z - b.x * a.z; }
            return area;
        }

        /// <summary>One counter-clockwise outline: its top (ear clipping) and its sides 2 m down (not along the river's rim).</summary>
        static void Fill(List<Vector3> q, float tile, bool carved, List<Vector3> v, List<Vector2> uv, List<int> tr)
        {
            int n = q.Count, b0 = v.Count;
            foreach (var p in q) { v.Add(p); uv.Add(new Vector2(p.x, p.z) / tile); }
            // ear clipping
            var idx = new List<int>(); for (int i = 0; i < n; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 20000)
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
                    tr.Add(b0 + ia); tr.Add(b0 + ic); tr.Add(b0 + ib);   // clockwise from above = facing up
                    idx.RemoveAt(i); cut = true; break;
                }
                if (!cut) break;
            }
            if (idx.Count == 3) { tr.Add(b0 + idx[0]); tr.Add(b0 + idx[2]); tr.Add(b0 + idx[1]); }
            else if (idx.Count > 3) Debug.LogWarning($"[Lobby] ground piece not fully filled ({idx.Count} corners left)");
            // the sides, 2 m down
            for (int i = 0; i < n; i++)
            {
                var a = q[i]; var b = q[(i + 1) % n];
                if (carved && OnRim((a + b) / 2)) continue;   // along the river: the riverbed lines it
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
        static readonly List<(WallStyle st, List<Vector3> poly)> areas = new List<(WallStyle, List<Vector3>)>();
        static void Area(WallStyle st, params Vector3[] pts) => areas.Add((st, Round(pts, true)));

        /// <summary>Which area a point is in (its wall style); null outside the map.</summary>
        static WallStyle? StyleAt(Vector3 p)
        {
            foreach (var (st, poly) in areas)
            {
                bool inside = false;
                for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                    if ((poly[i].z > p.z) != (poly[j].z > p.z) && p.x < (poly[j].x - poly[i].x) * (p.z - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x) inside = !inside;
                if (inside) return st;
            }
            return null;
        }

        static void Regions()
        {
            areas.Clear();
            var g = GroundMat(Tex.Grass());
            // the hub with the wardrobe corner (top left), the ruins corner (top right) and the shop corner (bottom left)
            Ground("Hub", g, 4, P(-11, 23, 3), P(2.3f, 23), P(2.3f, 16.3f), P(10, 16.3f), P(10, 23.7f), P(26.6f, 23.7f), P(26.6f, -21.5f), P(-11, -21.5f, 3));
            // the park with the pond corner and the notch on its left side
            // (21.11: its top-left corner is square now and the notch is the way into the neon quarter next to it)
            Ground("Park", g, 4, P(-50.7f, 17.5f), P(-11, 17.5f), P(-11, -6.9f), P(-50.7f, -6.9f), P(-50.7f, 0.6f, 1.5f), P(-43.7f, 0.6f), P(-43.7f, 10.4f), P(-50.7f, 10.4f, 1.5f));
            // the glitch checkerboard with the portal nook on its left (21.11: square bottom-left corner, the neon quarter is next to it)
            Ground("Glitch", GroundMat(CheckerTex(), 0.2f), 7, P(-50.7f, -6.9f), P(-22.6f, -6.9f), P(-22.6f, -24, 2), P(-44, -24, 2), P(-44, -16.3f, 1), P(-50.7f, -16.3f));
            // 21.11 the neon quarter west of the park and the glitch area: dark violet with a glowing grid; the park's notch is its entrance hall
            var neon = Mats.Emissive(Tex.Grid("1C0A36", "D24CFF", 4), Color.white, Color.white * 0.75f, null, 0.35f);
            Ground("Neon", neon, 6, NeonOutline);
            Ground("Jungle", g, 3.5f, P(-8.5f, -21.5f), P(26.6f, -21.5f), P(26.6f, -53, 3), P(-8.5f, -53, 3));
            // the snowy valley round the top of the hub, down the right side to the crystal cave
            Ground("Snow", GroundMat(Tex.Snow()), 7, P(-5.9f, 23), P(-5.9f, 49.8f, 4), P(47.5f, 49.8f, 4), P(47.5f, 6.8f), P(26.6f, 6.8f), P(26.6f, 23.7f), P(10, 23.7f), P(10, 16.3f), P(2.3f, 16.3f), P(2.3f, 23));
            // the crystal cave: a round grey cave right of the hub
            var cave = GroundMat(Tex.Concrete(), 0.3f); cave.color = Hex("6A6480");
            Ground("CrystalCave", cave, 4, P(26.6f, 6.8f), P(50.7f, 6.8f, 6), P(50.7f, -24, 9), P(26.6f, -24));
            // each area's walls are its own: grass-and-earth in the lobby, park and jungle, snow, glitch, crystal
            Area(WallStyle.Neon, NeonOutline);   // first: the park's rectangle below still covers the notch
            Area(WallStyle.Crystal, P(26.6f, 6.8f), P(50.7f, 6.8f, 6), P(50.7f, -24, 9), P(26.6f, -24));
            Area(WallStyle.Glitch, P(-50.7f, -6.9f), P(-22.6f, -6.9f), P(-22.6f, -24, 2), P(-44, -24, 2), P(-44, -16.3f, 1), P(-50.7f, -16.3f));
            Area(WallStyle.Snow, P(-5.9f, 23), P(-5.9f, 49.8f, 4), P(47.5f, 49.8f, 4), P(47.5f, 6.8f), P(26.6f, 6.8f), P(26.6f, 23.7f), P(10, 23.7f), P(10, 16.3f), P(2.3f, 16.3f), P(2.3f, 23));
            Area(WallStyle.Grass, P(-11, 23, 3), P(2.3f, 23), P(2.3f, 16.3f), P(10, 16.3f), P(10, 23.7f), P(26.6f, 23.7f), P(26.6f, -21.5f), P(-11, -21.5f, 3));
            Area(WallStyle.Grass, P(-50.7f, 17.5f), P(-11, 17.5f), P(-11, -6.9f), P(-50.7f, -6.9f));
            Area(WallStyle.Grass, P(-8.5f, -21.5f), P(26.6f, -21.5f), P(26.6f, -53, 3), P(-8.5f, -53, 3));
        }

        /// <summary>21.11 the neon quarter: x −84..−50.7, z −24..17.5, plus the park's old notch (x −50.7..−43.7, z 0.6..10.4)
        /// as the entrance hall. The corners shared with the park keep exactly the park's rounding.</summary>
        static readonly Vector3[] NeonOutline =
        {
            P(-84, 17.5f, 4), P(-50.7f, 17.5f), P(-50.7f, 10.4f, 1.5f), P(-43.7f, 10.4f), P(-43.7f, 0.6f), P(-50.7f, 0.6f, 1.5f), P(-50.7f, -24, 2), P(-84, -24, 4),
        };

        // ---------------- the river (22.11: a real channel you can swim in) ----------------
        // «Like in Super Bear Adventure»: the river from the snowy lake through the hub to the jungle pool is a half-pipe
        // dug into the ground (a semi-ellipse across, 5.1 m wide, 1.9 m deep) lined with sand, with see-through blue
        // water a bit below the grass. The ground has no floor over it (Ground() cuts its outline along the rim).
        // Swimming is in Water.cs; there is no current. Two wooden bridges and the stepping stones cross it in the hub.

        public const float RiverHalfW = 2.55f, RiverDepth = 1.9f, RiverSurface = -0.32f;
        internal static Water.Channel river;
        /// <summary>The river's rim seen from above (counter-clockwise): the ground skips everything inside it.</summary>
        static List<Vector2> corridor;
        /// <summary>Autotest: a deep spot in the river in the hub and the way it flows there; a spot by its west bank, facing it.</summary>
        public static Vector3 SwimSpot, SwimBank;
        public static float SwimYaw, SwimBankYaw;
        const int RiverCapN = 6;

        static Vector3 Left(Vector3 t) => new Vector3(-t.z, 0, t.x);

        // 22.11 (later the same day, the kid asked): the snowy lake by the water cave and the jungle pool are the river's
        // own water now — the same carved half-pipe, only wider and a bit deeper there, one channel from the lake's
        // far end over the cave through the hub and the hub–jungle wall (a gated passage) into the jungle pool.
        public const float LakeHalfW = 2.8f, LakeDepth = 2.2f, PoolHalfW = 5.6f, PoolDepth = 2.3f;
        /// <summary>Autotest: a deep spot in the snowy lake (over the water cave) and in the jungle pool.</summary>
        public static Vector3 LakeSpot, PoolSpot;
        /// <summary>Where the river crosses the hub–jungle wall (z −21.5) and the width of the gated passage there.</summary>
        public static Vector3 JunglePass;
        public static float JunglePassW;
        /// <summary>The gate with a grate over the river's passage into the jungle (opens with the jungle).</summary>
        public static LobbyGate JungleRiverGate;

        /// <summary>The channel's half width and depth along its line: the lake (north of the river's old start, z &gt; 36.5),
        /// the river, the jungle pool (south of the wall, z &lt; −22.5).</summary>
        static void Profile(Vector3 p, out float w, out float d)
        {
            w = RiverHalfW; d = RiverDepth;
            if (p.z > 36.5f) { float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(36.5f, 40.5f, p.z)); w = Mathf.Lerp(RiverHalfW, LakeHalfW, t); d = Mathf.Lerp(RiverDepth, LakeDepth, t); }
            else if (p.z < -22.5f) { float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-22.5f, -27.5f, p.z)); w = Mathf.Lerp(RiverHalfW, PoolHalfW, t); d = Mathf.Lerp(RiverDepth, PoolDepth, t); }
        }

        /// <summary>The river's centre line (22.11: from the far end of the snowy lake — along the top over the water cave,
        /// round the corner, down past the cave's way in — through the hub, under the hub–jungle wall, to the end of the
        /// jungle pool), samples every 0.9 m with rounded ends, registered as swimmable water.</summary>
        static void PlanRiver()
        {
            var line = Round(new[] { P(20.6f, 46.35f), P(42f, 46.35f, 5), P(42f, 35.5f, 4), P(39f, 25f, 4), P(35f, 17f, 4), P(29f, 10.5f, 4), P(22f, 9f, 4), P(16f, 5f, 4), P(12f, -2.5f, 4), P(10f, -12f, 4), P(9f, -21f, 3), P(13.4f, -31.5f) }, false);
            var pts = new List<Vector3> { line[0] };
            float acc = 0; const float step = 0.9f;
            for (int i = 0; i < line.Count - 1; i++)
            {
                var a = line[i]; var b = line[i + 1]; float len = (b - a).magnitude;
                if (len < 1e-4f) continue;
                float t = step - acc;
                while (t < len) { pts.Add(a + (b - a) * (t / len)); t += step; }
                acc = len - (t - step);
            }
            if ((pts[pts.Count - 1] - line[line.Count - 1]).magnitude > 0.35f) pts.Add(line[line.Count - 1]); else pts[pts.Count - 1] = line[line.Count - 1];
            var tan = new List<Vector3>();
            for (int i = 0; i < pts.Count; i++) { var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]; d.y = 0; tan.Add(d.normalized); }
            // the width and depth at every sample (the lake and the pool are wider); in a bend the width never gets
            // bigger than the bend's radius (the inner bank would fold over itself)
            var ws = new List<float>(); var ds = new List<float>();
            for (int i = 0; i < pts.Count; i++)
            {
                Profile(pts[i], out float w0, out float d0);
                if (i > 0 && i < pts.Count - 1)
                {
                    float ang = Vector3.Angle(tan[i - 1], tan[i + 1]) * Mathf.Deg2Rad;
                    if (ang > 1e-3f) w0 = Mathf.Min(w0, 0.85f * (pts[i + 1] - pts[i - 1]).magnitude / ang);
                }
                ws.Add(w0); ds.Add(d0);
            }
            river = new Water.Channel { owner = root, surface = RiverSurface };
            // the ends are round (a quarter of a ball): width and depth go down to 0
            void Cap(Vector3 from, Vector3 outward, float u, Vector3 t, float w0, float d0) { float k = Mathf.Sqrt(Mathf.Max(0, 1 - u * u)); river.Add(from + outward * u * w0, Left(t), w0 * k, d0 * k); }
            int L = pts.Count - 1;
            int capA = Mathf.Max(RiverCapN, Mathf.CeilToInt(ws[0] / 0.45f)), capB = Mathf.Max(RiverCapN, Mathf.CeilToInt(ws[L] / 0.45f));   // the wide pool's end gets more rings
            for (int j = capA; j >= 1; j--) Cap(pts[0], -tan[0], j / (float)capA, tan[0], ws[0], ds[0]);
            for (int i = 0; i < pts.Count; i++) river.Add(pts[i], Left(tan[i]), ws[i], ds[i]);
            for (int j = 1; j <= capB; j++) Cap(pts[L], tan[L], j / (float)capB, tan[L], ws[L], ds[L]);
            river.Seal();
            // v0.31: in the snowy valley the river is frozen (ice you walk and slide on); from the snow gate on it flows
            river.speed = 1.7f;
            river.iceTo = 0;
            for (int i = 1; i < river.Count; i++) if (river.c[i].x < 26.6f && river.c[i].z < 23.7f) { river.iceTo = i; break; }
            Water.Add(river);
            // the rim outline: along the left bank, round the end, back along the right bank (the tips are shared)
            int N = river.Count;
            corridor = new List<Vector2>();
            for (int i = 0; i < N; i++) { var p = river.c[i] + river.n[i] * river.w[i]; corridor.Add(new Vector2(p.x, p.z)); }
            for (int i = N - 2; i >= 1; i--) { var p = river.c[i] - river.n[i] * river.w[i]; corridor.Add(new Vector2(p.x, p.z)); }
            float ar = 0;
            for (int i = 0; i < corridor.Count; i++) ar += Cross(corridor[i], corridor[(i + 1) % corridor.Count]);
            if (ar < 0) corridor.Reverse();
            // the autotest's spots: mid-river between the stepping stones and the south bridge
            int best = RiverCapN; float bd = float.MaxValue;
            for (int i = RiverCapN; i < N - RiverCapN - 1; i++) { float dd = (river.c[i] - new Vector3(11, 0, -8)).sqrMagnitude; if (dd < bd) { bd = dd; best = i; } }
            var flow = river.c[best + 1] - river.c[best]; flow.y = 0;
            SwimSpot = river.c[best]; SwimYaw = Quaternion.LookRotation(flow).eulerAngles.y;
            var west = -river.n[best];
            SwimBank = river.c[best] + west * (river.w[best] - 1.3f); SwimBankYaw = Quaternion.LookRotation(west).eulerAngles.y;
            // 22.11 the lake over the water cave, the middle of the jungle pool, the passage through the hub–jungle wall
            LakeSpot = river.c[Nearest(new Vector3(29, 0, 46.35f))];
            PoolSpot = river.c[Nearest(new Vector3(12.6f, 0, -29.6f))];
            JunglePass = new Vector3(9, 0, -21.5f); JunglePassW = RiverHalfW * 2 + 2;
            for (int i = 0; i < N - 1; i++)
                if (river.c[i].z >= -21.5f && river.c[i + 1].z < -21.5f)
                {
                    float t = Mathf.InverseLerp(river.c[i].z, river.c[i + 1].z, -21.5f);
                    JunglePass = new Vector3(Mathf.Lerp(river.c[i].x, river.c[i + 1].x, t), 0, -21.5f);
                    JunglePassW = Mathf.Lerp(river.w[i], river.w[i + 1], t) * 2 + 2;
                    break;
                }
        }

        /// <summary>The river sample nearest to a point.</summary>
        public static int Nearest(Vector3 p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < river.Count; i++) { float dd = (river.c[i].x - p.x) * (river.c[i].x - p.x) + (river.c[i].z - p.z) * (river.c[i].z - p.z); if (dd < bd) { bd = dd; best = i; } }
            return best;
        }

        /// <summary>
        /// Autotest: swimming along the river's middle from one spot to another, half a metre under the surface —
        /// is it water all the way (and deep), and what stands in the way? Gates (their grates) are listed apart,
        /// the stepping stones don't count (you swim round them).
        /// </summary>
        public static bool RiverPath(Vector3 from, Vector3 to, out List<LobbyGate> gates, out string blocks)
        {
            gates = new List<LobbyGate>(); blocks = "";
            if (river == null) { blocks = "no river"; return false; }
            Physics.SyncTransforms();
            int a = Nearest(from), b = Nearest(to), step = b >= a ? 1 : -1;
            bool wet = true;
            for (int i = a; i != b; i += step)
            {
                var p = river.c[i]; var q = river.c[i + step];
                if (!Water.At(new Vector3(p.x, RiverSurface - 0.5f, p.z), out _, out float bot) || bot > RiverSurface - 1.2f) { wet = false; blocks += $" dry@{p.x:0.0},{p.z:0.0}"; }
                var o = new Vector3(p.x, RiverSurface - 0.6f, p.z); var dir = new Vector3(q.x, o.y, q.z) - o;
                foreach (var h in Physics.RaycastAll(o, dir.normalized, dir.magnitude, ~(1 << Layers.Player), QueryTriggerInteraction.Ignore))
                {
                    var g = h.collider.GetComponentInParent<LobbyGate>();
                    if (g != null) { if (!gates.Contains(g)) gates.Add(g); continue; }
                    if (h.collider.name == "SteppingStone" || h.collider.GetComponentInParent<PlayerCtl>() != null) continue;
                    blocks += $" {h.collider.name}@{h.point.x:0.0},{h.point.z:0.0}";
                }
            }
            return wet && blocks.Length == 0;
        }

        /// <summary>Is a point on the river's rim (an edge of a carved ground piece along the river)?</summary>
        static bool OnRim(Vector3 p)
        {
            if (corridor == null) return false;
            var q = new Vector2(p.x, p.z);
            if (!river.area.Contains(q)) return false;
            for (int i = 0; i < corridor.Count; i++)
            {
                var a = corridor[i]; var ab = corridor[(i + 1) % corridor.Count] - a;
                float l2 = ab.sqrMagnitude;
                float t = l2 < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(q - a, ab) / l2);
                if ((a + ab * t - q).sqrMagnitude < 0.02f * 0.02f) return true;
            }
            return false;
        }

        /// <summary>A ground outline minus the river: the pieces left (the hub falls in two, the snow and the jungle get a bay).</summary>
        static List<List<Vector3>> Carve(List<Vector3> q, out bool carved)
        {
            carved = false;
            var one = new List<List<Vector3>> { q };
            if (corridor == null) return one;
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var p in q) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); }
            if (!river.area.Overlaps(Rect.MinMaxRect(x0, z0, x1, z1))) return one;
            var A = new List<Vector2>(); foreach (var p in q) A.Add(new Vector2(p.x, p.z));
            var res = Subtract(A, corridor, out bool touched);
            if (res == null) { Debug.LogWarning("[Lobby] could not cut the river out of the ground — left whole"); return one; }
            if (!touched) return one;
            carved = true;
            var outL = new List<List<Vector3>>();
            foreach (var r in res)
            {
                var l = new List<Vector3>(); foreach (var p in r) l.Add(new Vector3(p.x, 0, p.y));
                CleanOutline(l);
                if (l.Count < 3) continue;
                if (Area2(l) < 0) l.Reverse();
                outL.Add(l);
            }
            return outL;
        }

        /// <summary>Drops doubled and straight-through corners (the ear clipping can't cut at a straight corner).</summary>
        static void CleanOutline(List<Vector3> l)
        {
            bool changed = true;
            while (changed && l.Count > 3)
            {
                changed = false;
                for (int i = 0; i < l.Count && l.Count > 3; i++)
                {
                    int n = l.Count;
                    var a = l[(i + n - 1) % n]; var b = l[i]; var c = l[(i + 1) % n];
                    Vector3 ab = b - a, bc = c - b;
                    if (ab.sqrMagnitude < 1e-6f) { l.RemoveAt(i); changed = true; i--; continue; }
                    if (bc.sqrMagnitude < 1e-6f) continue;
                    float cr = (ab.x * bc.z - ab.z * bc.x) / (ab.magnitude * bc.magnitude);
                    if (Mathf.Abs(cr) < 2e-3f && Vector3.Dot(ab, bc) > 0) { l.RemoveAt(i); changed = true; i--; }
                }
            }
        }

        class Cut { public Vector2 p; public int ea, eb, ia, ib; public float ta, tb; public bool aEnters, used; }

        /// <summary>
        /// Polygon A minus polygon B (both counter-clockwise, simple), Greiner–Hormann style: walk A where it is outside B,
        /// switch to B (backwards) where A goes into B. Returns null if something odd happens (then the ground stays whole).
        /// touched = false when B doesn't cut A at all.
        /// </summary>
        static List<List<Vector2>> Subtract(List<Vector2> A, List<Vector2> B, out bool touched)
        {
            touched = false;
            int na = A.Count, nb = B.Count;
            var cuts = new List<Cut>();
            for (int i = 0; i < na; i++)
            {
                Vector2 a0 = A[i], a1 = A[(i + 1) % na];
                float ax0 = Mathf.Min(a0.x, a1.x), ax1 = Mathf.Max(a0.x, a1.x), ay0 = Mathf.Min(a0.y, a1.y), ay1 = Mathf.Max(a0.y, a1.y);
                for (int j = 0; j < nb; j++)
                {
                    Vector2 b0 = B[j], b1 = B[(j + 1) % nb];
                    if (Mathf.Max(b0.x, b1.x) < ax0 || Mathf.Min(b0.x, b1.x) > ax1 || Mathf.Max(b0.y, b1.y) < ay0 || Mathf.Min(b0.y, b1.y) > ay1) continue;
                    var r = a1 - a0; var sd = b1 - b0; float den = Cross(r, sd);
                    if (Mathf.Abs(den) < 1e-9f) continue;
                    var qp = b0 - a0; float t = Cross(qp, sd) / den, u = Cross(qp, r) / den;
                    if (t <= 1e-6f || t >= 1 - 1e-6f || u <= 1e-6f || u >= 1 - 1e-6f) continue;
                    cuts.Add(new Cut { p = a0 + r * t, ea = i, eb = j, ta = t, tb = u });
                }
            }
            if (cuts.Count == 0)
            {
                if (InPoly(A[0], B)) { touched = true; return new List<List<Vector2>>(); }   // all of it under the river
                return new List<List<Vector2>> { A };   // (a river wholly inside a piece of ground would be a hole: never needed here)
            }
            if (cuts.Count % 2 != 0) return null;
            var seqA = new List<(Vector2 p, Cut c)>(); var seqB = new List<(Vector2 p, Cut c)>();
            for (int i = 0; i < na; i++)
            {
                seqA.Add((A[i], null));
                var on = cuts.FindAll(c => c.ea == i); on.Sort((x, y) => x.ta.CompareTo(y.ta));
                foreach (var c in on) { c.ia = seqA.Count; seqA.Add((c.p, c)); }
            }
            for (int j = 0; j < nb; j++)
            {
                seqB.Add((B[j], null));
                var on = cuts.FindAll(c => c.eb == j); on.Sort((x, y) => x.tb.CompareTo(y.tb));
                foreach (var c in on) { c.ib = seqB.Count; seqB.Add((c.p, c)); }
            }
            int NA = seqA.Count, NB = seqB.Count;
            foreach (var c in cuts) c.aEnters = InPoly((c.p + seqA[(c.ia + 1) % NA].p) * 0.5f, B);
            var res = new List<List<Vector2>>();
            int guard = 0;
            foreach (var start in cuts)
            {
                if (start.used || start.aEnters) continue;
                var poly = new List<Vector2>();
                var cur = start;
                while (true)
                {
                    if (++guard > 200000) return null;
                    cur.used = true; poly.Add(cur.p);
                    int k = (cur.ia + 1) % NA;
                    while (seqA[k].c == null) { poly.Add(seqA[k].p); k = (k + 1) % NA; if (++guard > 200000) return null; }
                    var into = seqA[k].c;
                    if (!into.aEnters) return null;
                    into.used = true; poly.Add(into.p);
                    k = (into.ib - 1 + NB) % NB;
                    while (seqB[k].c == null) { poly.Add(seqB[k].p); k = (k - 1 + NB) % NB; if (++guard > 200000) return null; }
                    var outOf = seqB[k].c;
                    if (outOf == start) break;
                    if (outOf.aEnters || outOf.used) return null;
                    cur = outOf;
                }
                res.Add(poly);
            }
            touched = true;
            return res;
        }

        static bool InPoly(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            return inside;
        }

        /// <summary>Is the wall line a–b over the river (within <paramref name="margin"/> of its rim)?</summary>
        static bool NearRiver(Vector3 a, Vector3 b, float margin)
        {
            if (river == null) return false;
            float len = Vector3.Distance(a, b);
            for (float t = 0; t <= len + 0.01f; t += 0.3f) if (river.Edge(Vector3.Lerp(a, b, len < 0.01f ? 0 : Mathf.Min(1, t / len))) < margin) return true;
            return false;
        }

        /// <summary>The riverbed (a sandy half-pipe with a collider), the see-through flowing water, the lakes, the pool,
        /// the stepping stones and the bridges.</summary>
        static void Rivers()
        {
            var c = river; int N = c.Count; const int K = 12;
            // the riverbed: a semi-ellipse across at every sample; its rim is exactly the ground's cut edge
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            float along = 0;
            for (int i = 0; i < N; i++)
            {
                if (i > 0) along += Vector3.Distance(c.c[i], c.c[i - 1]);
                float w = c.w[i], d = c.d[i]; var n = c.n[i];
                float arc = Mathf.PI * Mathf.Sqrt((w * w + d * d) / 2);   // about the length across
                for (int k = 0; k <= K; k++)
                {
                    float th = Mathf.PI * k / K;
                    var p = k == 0 ? c.c[i] + n * w : k == K ? c.c[i] - n * w : c.c[i] + n * (w * Mathf.Cos(th)) + Vector3.down * (d * Mathf.Sin(th));
                    v.Add(p); uv.Add(new Vector2(along / 2.5f, arc * k / K / 2.5f));
                }
            }
            for (int i = 0; i < N - 1; i++)
                for (int k = 0; k < K; k++)
                {
                    int a = i * (K + 1) + k, b = a + 1, a2 = a + K + 1, b2 = a2 + 1;
                    tr.Add(a); tr.Add(a2); tr.Add(b); tr.Add(b); tr.Add(a2); tr.Add(b2);   // facing in (up)
                }
            var bedMesh = new Mesh { name = "RiverBed" };
            bedMesh.SetVertices(v); bedMesh.SetUVs(0, uv); bedMesh.SetTriangles(tr, 0);
            bedMesh.RecalculateNormals(); bedMesh.RecalculateBounds();
            var bedM = Tiled(Tex.Sand(), 1, 0.1f); bedM.color = Hex("D9C9A3");
            var bed = Prim.MeshGO(bedMesh, root, bedM, "RiverBed");
            bed.layer = Layers.World;
            bed.AddComponent<MeshCollider>().sharedMesh = bedMesh;
            // the water: a see-through sheet a bit below the grass, its edges just inside the riverbed; no collider
            v = new List<Vector3>(); uv = new List<Vector2>(); tr = new List<int>();
            float h = -RiverSurface; along = 0;
            for (int i = 0; i < N; i++)
            {
                if (i > 0) along += Vector3.Distance(c.c[i], c.c[i - 1]);
                float w = c.w[i], d = c.d[i];
                float ww = d > h ? w * Mathf.Sqrt(1 - (h / d) * (h / d)) + 0.05f : 0;
                var mid = new Vector3(c.c[i].x, RiverSurface, c.c[i].z);
                v.Add(mid + c.n[i] * ww); v.Add(mid - c.n[i] * ww);
                uv.Add(new Vector2(along / 4f, ww / 4f)); uv.Add(new Vector2(along / 4f, -ww / 4f));
            }
            for (int i = Mathf.Max(0, c.iceTo - 1); i < N - 1; i++)
            {
                int l0 = i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                tr.Add(l0); tr.Add(l1); tr.Add(r0); tr.Add(r0); tr.Add(l1); tr.Add(r1);
            }
            if (c.iceTo > 0) FrozenRiver(c);
            var wMesh = new Mesh { name = "RiverWater" };
            wMesh.SetVertices(v); wMesh.SetUVs(0, uv); wMesh.SetTriangles(tr, 0);
            wMesh.RecalculateNormals(); wMesh.RecalculateBounds();
            var water = Prim.MeshGO(wMesh, root, Water.SurfaceMat(0.55f), "River");
            water.layer = Layers.World;
            var wr = water.GetComponent<Renderer>(); wr.shadowCastingMode = ShadowCastingMode.Off;
            water.AddComponent<WaterFlow>();   // the texture slides from the snowy lake towards the jungle
            // (22.11: the snowy lake and the jungle pool used to be flat blue sheets on the ground; they are the
            // channel's own wide ends now — see PlanRiver)
            // stepping stones over the river by the hub (where they were): stone pillars standing on the riverbed,
            // now straight across from bank to bank (the old row ran half along the river and ended mid-stream)
            // v0.31: one medium stone in the middle instead of the row of little ones
            var stoneM = M("9AA0A8", 0.3f);
            c.Locate(new Vector3(11.55f, 0, -1.45f), out int sseg, out float st, out _, out _, out _, out _);
            var sc = Vector3.Lerp(c.c[sseg], c.c[sseg + 1], st);
            {
                float bottom = c.Over(sc, out float bot) ? bot - 0.25f : -0.05f, top = 0.22f;
                Solid(PrimitiveType.Cylinder, new Vector3(sc.x, (top + bottom) / 2, sc.z), new Vector3(1.6f, (top - bottom) / 2, 1.6f), stoneM).name = "SteppingStone";
                S(root, new Vector3(sc.x, top, sc.z), new Vector3(1.55f, 0.3f, 1.55f), stoneM);
            }
            StoneSpot = new Vector3(sc.x, top0, sc.z);
            // two wooden bridges where the main paths cross: spawn → the crystal gate, spawn → the jungle gate
            Bridge(new Vector3(14.75f, 0, 2.65f));
            Bridge(new Vector3(9.6f, 0, -15f));
        }

        /// <summary>v0.31: the top of the one stone in the river (autotest).</summary>
        public static Vector3 StoneSpot;
        const float top0 = 0.22f;

        /// <summary>v0.31: the river in the snowy valley is frozen: a light blue ice sheet just under the grass, from bank
        /// to bank, solid and slippery (like World 4's ice), a few white cracks and snow along its edges.</summary>
        static void FrozenRiver(Water.Channel c)
        {
            int n = c.iceTo + 1;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            const float y = -0.06f;
            float along = 0;
            for (int i = 0; i < n; i++)
            {
                if (i > 0) along += Vector3.Distance(c.c[i], c.c[i - 1]);
                var mid = new Vector3(c.c[i].x, y, c.c[i].z);
                float w = c.w[i] + 0.05f;
                v.Add(mid + c.n[i] * w); v.Add(mid - c.n[i] * w);
                uv.Add(new Vector2(along / 3f, w / 3f)); uv.Add(new Vector2(along / 3f, -w / 3f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int l0 = i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                tr.Add(l0); tr.Add(l1); tr.Add(r0); tr.Add(r0); tr.Add(l1); tr.Add(r1);
            }
            var mesh = new Mesh { name = "RiverIce" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var iceM = Mats.Tex(Tex.RiverIce(), new Color(0.82f, 0.94f, 1f), 0.95f);
            var go = Prim.MeshGO(mesh, root, iceM, "RiverIce");
            go.layer = Layers.World;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<Ice>();
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // white cracks and snow lumps by the banks
            var white = M("F4FAFF", 0.2f);
            var rr = new Rng(41);
            for (int i = 2; i < n - 1; i += 3)
            {
                var p = new Vector3(c.c[i].x, y + 0.012f, c.c[i].z) + c.n[i] * rr.F(-c.w[i] * 0.6f, c.w[i] * 0.6f);
                B(root, p, new Vector3(0.05f, 0.01f, rr.F(0.6f, 1.4f)), white, new Vector3(0, rr.F(0, 180), 0)).name = "IceCrack";
                foreach (var sd in new[] { -1f, 1f })
                    if (rr.Chance(0.6f)) S(root, new Vector3(c.c[i].x, y, c.c[i].z) + c.n[i] * sd * (c.w[i] - 0.15f), new Vector3(rr.F(0.5f, 0.9f), 0.22f, rr.F(0.5f, 0.9f)), white).name = "IceSnow";
            }
            IceSpot = new Vector3(c.c[n / 2].x, y, c.c[n / 2].z);
        }

        /// <summary>v0.31: a spot on the frozen river (autotest).</summary>
        public static Vector3 IceSpot;

        /// <summary>A simple wooden bridge straight across the river with railings.</summary>
        static void Bridge(Vector3 near)
        {
            river.Locate(near, out int seg, out float t, out _, out _, out _, out _);
            int j = Mathf.Min(seg + 1, river.Count - 1);
            var cp = Vector3.Lerp(river.c[seg], river.c[j], t); var n = Vector3.Lerp(river.n[seg], river.n[j], t); n.y = 0; n.Normalize();
            float L = RiverHalfW * 2 + 2.6f;
            var b = Node("Bridge", root, new Vector3(cp.x, 0, cp.z), new Vector3(0, Quaternion.LookRotation(n).eulerAngles.y, 0));   // local z = across the river
            var wood = Tiled(Tex.Planks("A0642F"), 1); var dark = M("6D4425", 0.25f);
            Solid(PrimitiveType.Cube, new Vector3(0, 0.12f, 0), new Vector3(2.3f, 0.24f, L), wood, default, b).name = "BridgeDeck";
            for (int k = 0; k < 7; k++) B(b, new Vector3(0, 0.245f, -L / 2 + 0.4f + k * (L - 0.8f) / 6), new Vector3(2.32f, 0.02f, 0.08f), dark);   // plank seams
            foreach (var sd in new[] { -1f, 1f })
            {
                B(b, new Vector3(sd * 0.85f, -0.05f, 0), new Vector3(0.22f, 0.22f, L), dark);   // beams under the deck
                foreach (var z in new[] { -L / 2 + 0.25f, 0f, L / 2 - 0.25f })
                    Solid(PrimitiveType.Cylinder, new Vector3(sd * 1.05f, 0.24f + 0.45f, z), new Vector3(0.14f, 0.45f, 0.14f), dark, default, b);
                Solid(PrimitiveType.Cube, new Vector3(sd * 1.05f, 1.08f, 0), new Vector3(0.1f, 0.1f, L - 0.3f), wood, default, b);
            }
        }

        // ---------------- walls ----------------

        // The four lobby walls from the player's drawing: dirt with grass on top (hub, park, ruins, jungle),
        // dirt with snow on top (snowy valley), the purple/teal checker (glitch world), dark stone with pink crystals (crystal cave).
        enum WallStyle { Grass, Snow, Glitch, Crystal, Neon }
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
                        case WallStyle.Neon:
                            // 21.11 dark violet panels with a pink neon band on top and a cyan line low down
                            c = new Color(0.13f, 0.06f, 0.26f);
                            if (x % (N / 2) < 2) c = new Color(0.22f, 0.12f, 0.42f);
                            if (v > 0.86f && v < 0.93f) c = new Color(1f, 0.24f, 0.94f);
                            else if (v > 0.12f && v < 0.15f) c = new Color(0.21f, 0.88f, 1f);
                            break;
                        default:
                        {
                            // orange dirt with marker strokes, a wavy band of grass or snow along the top
                            c = new Color(0.93f, 0.62f, 0.38f);
                            float wave = 0.80f - 0.035f * Mathf.Sin(x / (float)N * Mathf.PI * 2 * 5);
                            if (v > wave) c = st == WallStyle.Grass ? new Color(0.13f, 0.62f, 0.25f) : new Color(0.97f, 0.98f, 1f);
                            break;
                        }
                    }
                    // marker strokes: slight vertical streaks
                    float streak = (Mathf.PerlinNoise(x * 0.09f, y * 0.02f) - 0.5f) * 0.08f + (float)rng.NextDouble() * 0.02f;
                    c = new Color(Mathf.Clamp01(c.r + streak), Mathf.Clamp01(c.g + streak), Mathf.Clamp01(c.b + streak));
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
            m = st == WallStyle.Neon ? Mats.Emissive(WallTex(st), Color.white, Color.white * 0.8f, null, 0.3f)   // the neon bands glow
                                     : Mats.Tex(WallTex(st), Color.white, st == WallStyle.Crystal ? 0.35f : 0.12f);
            m.mainTextureScale = new Vector2(rep, 1);
            return wallMats[(st, rep)] = m;
        }

        static bool building;
        static readonly HashSet<Vector2Int> posts = new HashSet<Vector2Int>();
        public static int Posts => posts.Count;

        /// <summary>A round post at a wall joint (one per joint, never two in the same place).</summary>
        static void Post(Vector3 p, float h)
        {
            if (!posts.Add(new Vector2Int(Mathf.RoundToInt(p.x * 10), Mathf.RoundToInt(p.z * 10)))) return;
            var st = StyleAt(p + new Vector3(0.9f, 0, 0.9f)) ?? StyleAt(p - new Vector3(0.9f, 0, 0.9f)) ?? style;
            var go = Solid(PrimitiveType.Cylinder, new Vector3(p.x, h / 2, p.z), new Vector3(1.04f, h / 2, 1.04f), WallMat(st, 3.2f, h));
            go.name = "WallPost";
            Cy(root, new Vector3(p.x, h + 0.02f, p.z), new Vector3(1.16f, 0.17f, 1.16f), CapMat(st));   // a little higher than the wall caps: no flicker
            // 22.11 over the river channel the post goes on down into the water like a pier
            if (building && river != null && river.Edge(p) < 0.65f) Solid(PrimitiveType.Cylinder, new Vector3(p.x, -1.2f, p.z), new Vector3(1.04f, 1.2f, 1.04f), PlugMat()).name = "WallPlug";
        }

        static Material plugM;
        static Material PlugMat() => plugM != null ? plugM : plugM = Tiled(Tex.Concrete(), 1, 0.2f);

        /// <summary>The top of a wall or a post: grass, snow, purple, stone — and glowing pink in the neon quarter.</summary>
        static Material CapMat(WallStyle st) => st switch
        {
            WallStyle.Grass => M("2E9E3E", 0.3f), WallStyle.Snow => M("FFFFFF", 0.3f), WallStyle.Glitch => M("6A3FC0", 0.3f),
            WallStyle.Neon => G("FF3CF0", 1.5f), _ => M("3E5560", 0.3f),
        };

        static Material barrierM;
        public const float BarrierH = 30f;
        public static int Barriers;

        static void Wall(Vector3 a, Vector3 b, float h = 5.5f)
        {
            var d = b - a; d.y = 0;
            if (d.magnitude < 0.1f) return;
            // 18.11: wall pieces end exactly at their corners and a round post covers each joint — overlapping
            // pieces at the rounded corners used to flicker (two faces in the same place)
            float len = d.magnitude;
            var right = new Vector3(d.z, 0, -d.x).normalized;
            var mid = (a + b) / 2;
            // each face of a wall looks like the area it faces (outside the map: like the inside)
            var sr = StyleAt(mid + right * 1.2f); var sl = StyleAt(mid - right * 1.2f);
            var R = sr ?? sl ?? style; var L = sl ?? sr ?? style;
            float yaw = Quaternion.LookRotation(d).eulerAngles.y;
            if (R == L) Half(mid, 1f, R);
            else { Half(mid + right * 0.25f, 0.5f, R); Half(mid - right * 0.25f, 0.5f, L); }
            // 17.11: the wall goes on up as an invisible barrier — no jumping over walls
            if (barrierM == null) barrierM = Glass("D8F4FF", 0.07f);
            Post(a, h); Post(b, h);
            // 22.11 a wall over the river channel goes on down to its bed (no gap under it, no swimming through)
            if (building && NearRiver(a, b, 0.6f)) Box(mid + Vector3.down * 1.2f, new Vector3(1f, 2.4f, len), PlugMat(), yaw).name = "WallPlug";
            var bar = Prim.Make(PrimitiveType.Cube, root, mid + Vector3.up * (h + BarrierH / 2), new Vector3(1.0f, BarrierH, len + 1), barrierM, "Barrier", Layers.Barrier);
            bar.transform.localEulerAngles = new Vector3(0, yaw, 0);
            bar.AddComponent<BoxCollider>();
            bar.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            bar.GetComponent<Renderer>().enabled = false;   // 18.11: invisible — it stops you, but you don't see it
            Barriers++;

            void Half(Vector3 c, float thick, WallStyle st)
            {
                var go = Box(c + Vector3.up * h / 2, new Vector3(thick, h, len), WallMat(st, len, h), yaw);
                B(go.transform, new Vector3(0, 0.5f, 0), new Vector3(1.12f, 0.06f, 1.02f), CapMat(st));   // the top
                if (st != WallStyle.Crystal) return;
                // pink crystal clusters sticking out of the top, like on the drawing
                crystalRun += d.magnitude;
                if (crystalRun < 5.5f) return;
                crystalRun = 0;
                var p = c + Vector3.up * h;
                var pink = G("FF8FC8", 1.1f);
                Spike(root, p, p + new Vector3(0, 1.8f, 0), 0.55f, pink);
                Spike(root, p + new Vector3(0.15f, 0, 0.3f), p + new Vector3(0.5f, 1.3f, 0.9f), 0.4f, pink);
                Spike(root, p + new Vector3(-0.15f, 0, -0.3f), p + new Vector3(-0.5f, 1.3f, -0.9f), 0.4f, pink);
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
            // jungle line left of the gate, shop corner; 22.11 with a gap where the river goes on into the jungle
            // (its own gate with a grate stands in it), so no wall and no stone plugs over the channel there
            float px0 = JunglePass.x - JunglePassW / 2, px1 = JunglePass.x + JunglePassW / 2;
            if (px1 < 13.4f) WallPath(5.5f, P(13.6f, -21.5f), P(px1, -21.5f));
            WallPath(5.5f, P(px0, -21.5f), P(-11, -21.5f, 3), P(-11, -4.6f));
            WallPath(5.5f, P(-11, -0.8f), P(-11, 23, 3), P(2.3f, 23, 2), P(2.3f, 16.3f));              // (a way into the park at the bottom), wardrobe corner
            WallPath(5.5f, P(10, 16.3f), P(10, 23.7f, 2), P(26.6f, 23.7f, 3), P(26.6f, 12.6f));        // snow gate between the corners, ruins corner
            WallPath(5.5f, P(26.6f, 8.2f), P(26.6f, 5));                                                // river gate above, crystal gate below
            WallPath(5.5f, P(26.6f, -2), P(26.6f, -53, 3), P(-8.5f, -53, 3), P(-8.5f, -21.5f));        // hub right side, the jungle
            WallPath(5.5f, P(26.6f, -21.5f), P(24.7f, -21.5f));                                         // right of the jungle gate
            WallPath(5.5f, P(-11, -5.3f), P(4, -5.3f));                                                 // the line above the shops (in by the river)
            WallPath(5.5f, P(-8.5f, -40.3f), P(2.6f, -40.3f));                                          // the jungle portal nook
            // the park, the glitch checkerboard below it
            // (21.11: the old notch on the left is the entrance hall of the neon quarter: a 6 m opening under the neon arch)
            WallPath(5.5f, P(-11, 17.5f), P(-50.7f, 17.5f), P(-50.7f, 10.4f, 1.5f), P(-43.7f, 10.4f), P(-43.7f, NeonGate.z + 3.9f));
            WallPath(5.5f, P(-43.7f, NeonGate.z - 3.9f), P(-43.7f, 0.6f), P(-50.7f, 0.6f, 1.5f), P(-50.7f, -6.9f));
            style = WallStyle.Glitch;
            WallPath(5.5f, P(-50.7f, -6.9f),
                     P(-50.7f, -16.3f), P(-44, -16.3f, 1), P(-44, -24, 2), P(-22.6f, -24, 2), P(-22.6f, -6.9f));
            WallPath(5.5f, P(-50.7f, -6.9f), P(-43.3f, -6.9f));                                         // glitch gate in this line
            WallPath(5.5f, P(-34.8f, -6.9f), P(-11, -6.9f));
            // 21.11 the neon quarter: the rest of its outline (the sides by the park and the glitch area are theirs)
            style = WallStyle.Neon;
            WallPath(5.5f, P(-50.7f, 17.5f), P(-84, 17.5f, 4), P(-84, -24, 4), P(-50.7f, -24, 2), P(-50.7f, -16.3f));
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
            Zones.Add(c, 2.9f, "ГАРДЕРОБ", () => WardrobeUI.Open(), () => Worlds.Current == 1, Zones.Kind.Step);   // step onto the face
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
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 1.6f)), 4.4f, "МАГАЗИН", () => ShopUI.Open(), () => Worlds.Current == 1, Zones.Kind.Hit);   // hit it to go in
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
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 1.6f)), 4.4f, "ПРЕМИУМ-НАБОРЫ", () => PremiumUI.Open(), () => Worlds.Current == 1, Zones.Kind.Hit);
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
            var tx = Prim.Text(t, $"МИР {(world == World67.Id ? 67 : world)}", new Vector3(0, 6.5f, -0.85f), 8, Color.white);
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
                case "meme":
                    // 19.11: a laughing smiley, neon «LOL» and spinning golden smiley coins on top
                    var smile = Node("Smiley", t, new Vector3(0, 8.6f, 0));
                    S(smile, Vector3.zero, new Vector3(2.2f, 2.2f, 0.6f), G("FFE14D", 1.2f));
                    foreach (var sd in new[] { -1, 1 }) S(smile, new Vector3(sd * 0.4f, 0.3f, -0.3f), new Vector3(0.3f, 0.45f, 0.1f), M("1A1A1A", 0.5f));
                    for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 2.2f; S(smile, new Vector3(Mathf.Sin(a) * 0.62f, -0.25f - Mathf.Cos(a) * 0.3f, -0.3f), 0.16f, M("1A1A1A", 0.5f)); }
                    smile.gameObject.AddComponent<Bob>().amp = 0.2f;
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var coin = Node("SmileyCoin", t, new Vector3(sd * 3, 7.6f, 0));
                        Cy(coin, Vector3.zero, new Vector3(1.1f, 0.06f, 1.1f), M("FFC83A", 0.9f, 0.85f), new Vector3(90, 0, 0));
                        coin.gameObject.AddComponent<Spin>().speed = 120;
                    }
                    break;
            }
            Zones.Add(t.TransformPoint(new Vector3(0, 0, -1.2f)), 2.6f, $"МИР {(world == World67.Id ? 67 : world)}: {title}", () =>
            {
                // 19.11: a portal of a world that needs more saved friends says so (World 8 stands in the open hub)
                if (!Open(world)) { AudioFx.Play("error"); Hud.Toast($"Чтобы войти, спаси ещё друзей: нужно {NeedFor(world)}, спасено {Save.P.rescued.Count}", UIKit.Orange); return; }
                Worlds.Enter(world);
            }, () => Worlds.Current == 1);
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

        public static Vector3 Portal8Pos;

        /// <summary>19.11: the portal to World 8 «Самый мемный мир». 21.11: at its own spot in the neon quarter,
        /// facing the entrance; it still lets you in only with enough rescued friends (NeedFor(8)).</summary>
        static void Portal8()
        {
            Portal8Pos = new Vector3(-76, 0, -8);
            PortalArch(Portal8Pos, YawFacing(Portal8Pos, NeonDoor), World8.Id, "САМЫЙ МЕМНЫЙ МИР", "FF7AD9", M("7A3CFF", 0.3f), "meme");
        }

        /// <summary>The yaw that turns a portal's front (local −Z) towards a point.</summary>
        static float YawFacing(Vector3 from, Vector3 to) => Mathf.Repeat(Mathf.Atan2(-(to.x - from.x), -(to.z - from.z)) * Mathf.Rad2Deg, 360);

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

        // ---------------- the neon quarter (21.11) ----------------

        /// <summary>
        /// 21.11 the player asked for World 67 and World 8 to get a place of their own: the neon quarter west of the park.
        /// In through the park's old notch under a neon arch; inside, a dark violet square with a glowing grid: the World 67
        /// portal with a big neon «67» on the wall and green/purple glitch blocks, the World 8 portal (built in Portal8())
        /// with meme smiley billboards, neon lamps, a few trees and benches; the football pad finds its spot here too.
        /// </summary>
        static void NeonQuarter()
        {
            NeonArch();
            PortalArch(Portal67, YawFacing(Portal67, NeonDoor), World67.Id, "ГЛЮЧНЫЙ СИКС-СЕВЕН", "39FF6A", M("3A1C78", 0.3f), "glitch");
            Neon67(new Vector3(-82.9f, 0, Portal67.z));
            // glitch blocks round World 67: standing on the ground (solid) and hanging in the air (spinning)
            var green = G("39FF6A", 1.2f); var purple = G("A040FF", 1.3f);
            foreach (var (p, sz, yaw, gr) in new[] { (new Vector3(-81.2f, 0, 13.8f), 1.8f, 20f, true), (new Vector3(-81.2f, 1.8f, 13.8f), 1.1f, 50f, false), (new Vector3(-80.8f, 0, 0.6f), 1.4f, 35f, false),
                                                   (new Vector3(-71, 0, 15.3f), 1.6f, 10f, false), (new Vector3(-69.4f, 0, 13.8f), 1.1f, 40f, true), (new Vector3(-53.6f, 0, -2.4f), 1.2f, 15f, true) })
                Box(p + Vector3.up * sz / 2, Vector3.one * sz, gr ? green : purple, yaw);
            var air = new[] { new Vector3(-79.5f, 4.6f, 12.4f), new Vector3(-79.8f, 3.8f, 1.6f), new Vector3(-72, 6.4f, 10.5f), new Vector3(-67, 4.2f, -0.5f), new Vector3(-58, 5.2f, 12) };
            for (int k = 0; k < air.Length; k++)
            {
                var c = Node("GlitchCube", root, air[k]);
                c.gameObject.AddComponent<Bob>().amp = 0.35f;
                var cube = B(c, Vector3.zero, Vector3.one * (0.8f + (k % 3) * 0.3f), k % 2 == 0 ? G("39FF6A", 1.5f) : G("A040FF", 1.5f), new Vector3(k * 17, k * 40, k * 9));
                var sp = cube.AddComponent<Spin>(); sp.axis = new Vector3(0.4f, 1, 0.2f).normalized; sp.speed = 35 + k * 12;
            }
            // glowing tiles from the entrance to World 67, and a branch to World 8
            var pink = G("FF3CF0", 1.3f); var cyan = G("35E0FF", 1.3f);
            int n = 0;
            for (float x = NeonGate.x - 1.5f; x > Portal67.x + 4.2f; x -= 2.4f) B(root, new Vector3(x, 0.02f, NeonGate.z), new Vector3(1.1f, 0.04f, 1.1f), n++ % 2 == 0 ? pink : cyan);
            var p8 = new Vector3(-76, 0, -8);   // = Portal8Pos (set in Portal8())
            Vector3 from = new Vector3(-62, 0, NeonGate.z), to = p8 + (NeonDoor - p8).normalized * 2.6f;
            float ty = YawFacing(from, to);
            for (int k = 1; k <= 7; k++) B(root, Vector3.Lerp(from, to, k / 7f) + Vector3.up * 0.02f, new Vector3(1.1f, 0.04f, 1.1f), k % 2 == 0 ? pink : cyan, new Vector3(0, ty, 0));
            // meme billboards round World 8
            MemeBoard(new Vector3(-80.5f, 0, -19), "LOL", 0, "2B1060");
            MemeBoard(new Vector3(-69, 0, -21), "ХА-ХА", 1, "0E3A5C");
            MemeBoard(new Vector3(-57.5f, 0, -19.5f), "ЖИЗА", 2, "4A0E44");
            // neon lamps, trees, benches
            foreach (var (p, col) in new[] { (new Vector3(-53, 0, 12.2f), "FF3CF0"), (new Vector3(-53, 0, 0), "35E0FF"), (new Vector3(-66, 0, 9.3f), "39FF6A"),
                                             (new Vector3(-66, 0, -3), "FF3CF0"), (new Vector3(-82, 0, -1.5f), "A040FF"), (new Vector3(-62, 0, -12), "35E0FF") })
                NeonLamp(p, col);
            foreach (var p in new[] { new Vector3(-55, 0, 14.5f), new Vector3(-53.5f, 0, -13), new Vector3(-80.8f, 0, -12) })
                Tree(p, 0.95f, (int)(p.x * 5 + p.z));
            Bench(new Vector3(-59.5f, 0, 16.2f), 180); Bench(new Vector3(-63, 0, -22.4f), 0);
        }

        /// <summary>The neon arch over the entrance in the park's wall: «НЕОНОВЫЙ КВАРТАЛ · МИР 67 · МИР 8» on both sides.</summary>
        static void NeonArch()
        {
            var t = Node("NeonGate", root, NeonGate, new Vector3(0, 270, 0));   // front (local −Z) faces the park, local x = world +z
            var dark = M("1C0A36", 0.4f);
            var pink = G("FF3CF0", 1.8f); var cyan = G("35E0FF", 1.8f);
            foreach (var sd in new[] { -1, 1 })
            {
                // the pillars stand over the ends of the wall (the opening between them is 6.2 m)
                Solid(PrimitiveType.Cube, new Vector3(sd * 3.9f, 4.25f, 0), new Vector3(1.6f, 8.5f, 1.6f), dark, default, t);
                foreach (var fz in new[] { -0.82f, 0.82f }) B(t, new Vector3(sd * 3.9f, 3.6f, fz), new Vector3(0.3f, 7f, 0.05f), sd < 0 ? pink : cyan);
            }
            Solid(PrimitiveType.Cube, new Vector3(0, 8.3f, 0), new Vector3(9.6f, 2.6f, 1.4f), dark, default, t);
            foreach (var fz in new[] { -0.72f, 0.72f })
            {
                B(t, new Vector3(0, 9.5f, fz), new Vector3(9.4f, 0.12f, 0.06f), pink);
                B(t, new Vector3(0, 7.1f, fz), new Vector3(9.4f, 0.12f, 0.06f), cyan);
            }
            foreach (var back in new[] { false, true })
            {
                float z = back ? 0.77f : -0.77f;
                var a = Prim.Text(t, "НЕОНОВЫЙ КВАРТАЛ", new Vector3(0, 8.75f, z), 6.5f, Hex("FF7AF0"));
                a.transform.localRotation = Quaternion.Euler(0, back ? 180 : 0, 0);
                var b = Prim.Text(t, "МИР 67 · МИР 8", new Vector3(0, 7.75f, z), 5.5f, Hex("39FF6A"));
                b.transform.localRotation = Quaternion.Euler(0, back ? 180 : 0, 0);
            }
        }

        /// <summary>A big neon «67» on a dark board on the wall behind the World 67 portal.</summary>
        static void Neon67(Vector3 c)
        {
            var t = Node("Neon67", root, c, new Vector3(0, 270, 0));   // faces east (local −Z), local x = world +z, local +z = towards the wall
            B(t, new Vector3(0, 6.4f, 0.3f), new Vector3(8.8f, 7.2f, 0.15f), M("12061F", 0.5f));
            var frame = G("FF3CF0", 1.6f);
            foreach (var sd in new[] { -1, 1 })
            {
                B(t, new Vector3(0, 6.4f + sd * 3.6f, 0.15f), new Vector3(8.9f, 0.14f, 0.14f), frame);
                B(t, new Vector3(sd * 4.4f, 6.4f, 0.15f), new Vector3(0.14f, 7.2f, 0.14f), frame);
            }
            Digit(t, new Vector3(-2, 6.4f, 0), '6', G("39FF6A", 1.8f));
            Digit(t, new Vector3(2, 6.4f, 0), '7', G("B04CFF", 1.8f));
        }

        /// <summary>A seven-segment neon digit, 3 × 6 m, in t's local x/y plane.</summary>
        static void Digit(Transform t, Vector3 c, char d, Material m)
        {
            const float W = 3, H = 6, th = 0.6f;
            string on = d switch { '6' => "acdefg", '7' => "abc", '8' => "abcdefg", _ => "g" };
            foreach (char s in on)
            {
                var (p, sz) = s switch
                {
                    'a' => (new Vector2(0, H / 2 - th / 2), new Vector2(W, th)),
                    'b' => (new Vector2(W / 2 - th / 2, H / 4), new Vector2(th, H / 2)),
                    'c' => (new Vector2(W / 2 - th / 2, -H / 4), new Vector2(th, H / 2)),
                    'd' => (new Vector2(0, -H / 2 + th / 2), new Vector2(W, th)),
                    'e' => (new Vector2(-W / 2 + th / 2, -H / 4), new Vector2(th, H / 2)),
                    'f' => (new Vector2(-W / 2 + th / 2, H / 4), new Vector2(th, H / 2)),
                    _ => (Vector2.zero, new Vector2(W, th)),
                };
                B(t, c + new Vector3(p.x, p.y, 0), new Vector3(sz.x, sz.y, 0.3f), m);
            }
        }

        /// <summary>A meme billboard on two posts: a smiley (0 laughing, 1 winking, 2 in sunglasses) and a caption.</summary>
        static void MemeBoard(Vector3 p, string caption, int face, string bg)
        {
            var t = Node("MemeBoard", root, p, new Vector3(0, YawFacing(p, new Vector3(-64, 0, -6)), 0));   // front (local −Z) to the square
            var iron = M("2A2E36", 0.5f, 0.4f);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cylinder, new Vector3(sd * 1.7f, 1.4f, 0.25f), new Vector3(0.24f, 1.4f, 0.24f), iron, default, t);
            B(t, new Vector3(0, 4.3f, 0), new Vector3(4.4f, 3.4f, 0.2f), M(bg, 0.3f));
            var glow = G("FF3CF0", 1.6f);
            foreach (var sd in new[] { -1, 1 })
            {
                B(t, new Vector3(0, 4.3f + sd * 1.76f, 0), new Vector3(4.64f, 0.12f, 0.26f), glow);
                B(t, new Vector3(sd * 2.26f, 4.3f, 0), new Vector3(0.12f, 3.64f, 0.26f), glow);
            }
            var sm = Node("Smiley", t, new Vector3(-1, 4.3f, -0.12f));
            Cy(sm, Vector3.zero, new Vector3(2.2f, 0.03f, 2.2f), G("FFE14D", 0.9f), new Vector3(90, 0, 0));
            var dark = M("1A1A1A", 0.5f);
            if (face == 2) B(sm, new Vector3(0, 0.28f, -0.06f), new Vector3(1.6f, 0.36f, 0.06f), dark);   // sunglasses
            else
            {
                S(sm, new Vector3(-0.38f, 0.3f, -0.05f), new Vector3(0.26f, 0.42f, 0.08f), dark);
                if (face == 1) B(sm, new Vector3(0.38f, 0.3f, -0.05f), new Vector3(0.42f, 0.09f, 0.06f), dark);   // the wink
                else S(sm, new Vector3(0.38f, 0.3f, -0.05f), new Vector3(0.26f, 0.42f, 0.08f), dark);
            }
            if (face == 0)
            {
                S(sm, new Vector3(0, -0.38f, -0.05f), new Vector3(1.0f, 0.55f, 0.08f), dark);   // laughing mouth with a tongue
                S(sm, new Vector3(0, -0.52f, -0.08f), new Vector3(0.45f, 0.2f, 0.05f), M("FF5F7A", 0.4f));
            }
            else for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 2.2f; S(sm, new Vector3(Mathf.Sin(a) * 0.6f + (face == 2 ? 0.1f : 0), -0.25f - Mathf.Cos(a) * 0.28f, -0.05f), 0.15f, dark); }
            sm.gameObject.AddComponent<Bob>().amp = 0.08f;
            var tx = Prim.Text(t, caption, new Vector3(1.15f, 4.3f, -0.14f), 5.5f, Color.white);
            tx.transform.localRotation = Quaternion.identity;
        }

        static void NeonLamp(Vector3 p, string col)
        {
            Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.7f, new Vector3(0.16f, 1.7f, 0.16f), M("2A1E3A", 0.6f, 0.5f));
            Cy(root, p + Vector3.up * 2.2f, new Vector3(0.22f, 1.1f, 0.22f), G(col, 1.6f));   // a glowing tube round the post
            S(root, p + Vector3.up * 3.65f, 0.6f, G(col, 1.9f));
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
            // the water cave (15.11): a real cave under the lake shore — rock walls, a rocky snow-capped roof,
            // a little underground pool, glowing ice crystals and icicles; in from the river, out into the hollow
            var rock = M("8A93A0", 0.2f); var rockD = M("6E7682", 0.2f);
            Box(new Vector3(26.4f, 1.45f, 44.1f), new Vector3(21.4f, 8.1f, 1.4f), rock);                     // the lake side (22.11: down to under the lake's bed — a cliff into the water)
            Box(new Vector3(25.6f, 2.75f, 37.65f), new Vector3(19.6f, 5.5f, 0.3f), rockD);                   // lining of the valley wall
            var rr = new Rng(77);
            for (float x = 16.5f; x <= 37; x += 3.2f)
            {
                S(root, new Vector3(x, 6f + rr.F(-0.3f, 0.4f), 40.4f), new Vector3(rr.F(5.5f, 7f), rr.F(2.4f, 3.2f), 8.8f), rock);   // the roof
                S(root, new Vector3(x + 1.2f, 7.3f, 40.2f + rr.F(-1, 1)), new Vector3(rr.F(3.5f, 5f), 1.2f, rr.F(4, 6)), M("F4FAFF", 0.15f));   // snow on top
                S(root, new Vector3(x, 1.2f, 43.2f), new Vector3(rr.F(1.6f, 2.6f), rr.F(1.8f, 2.8f), 1.4f), rockD);   // boulders along the walls
                float ix = x + 1.6f;
                for (int k = 0; k < 3; k++) Spike(root, new Vector3(ix + k * 0.5f, 5.2f, 39.6f + k * 0.9f), new Vector3(ix + k * 0.5f, 4.1f - (k % 2) * 0.5f, 39.6f + k * 0.9f), 0.32f, M("BFE8FF", 0.8f));   // icicles
            }
            // the entrances framed by rocks
            foreach (var (x, z) in new[] { (16.2f, 37.9f), (16.2f, 43.2f), (36.6f, 37.9f), (36.6f, 43.2f) })
                S(root, new Vector3(x, 2.4f, z), new Vector3(2.2f, 5.2f, 1.8f), rock);
            // the underground pool and glowing ice crystals
            Prim.Make(PrimitiveType.Cylinder, root, new Vector3(25.5f, 0.04f, 41.4f), new Vector3(6.5f, 0.02f, 3.2f), waterM, "Pool", Layers.World);
            foreach (var c in new[] { new Vector3(19.5f, 0, 38.4f), new Vector3(28.5f, 0, 43f), new Vector3(33.5f, 0, 38.4f), new Vector3(22.5f, 0, 43f) })
                for (int k = 0; k < 3; k++)
                {
                    var b = c + new Vector3((k - 1) * 0.45f, 0, 0);
                    Spike(root, b, b + new Vector3((k - 1) * 0.35f, 1.3f + (k % 2) * 0.6f, 0), 0.45f, G("7FE3FF", 1.6f));
                }
            foreach (var x in new[] { 21f, 31f })
            {
                var lg = new GameObject("CaveLight"); lg.transform.SetParent(root, false); lg.transform.position = new Vector3(x, 3f, 40.6f);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Hex("8FE6FF"); l.range = 10; l.intensity = 2.2f; l.shadows = LightShadows.None;
            }
            // the rock on the far side of the river
            // (22.11: narrower — the river runs further east by the cave now, leaving a path to the cave's way in)
            Box(new Vector3(46.15f, 2.5f, 32.4f), new Vector3(2.7f, 5, 10.6f), rock);
            for (int k = 0; k < 3; k++) S(root, new Vector3(46.15f, 5f, 28.6f + k * 3.6f), new Vector3(3.2f, 2.4f, 4f), rock);
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
            Disc(c + Vector3.up * 4.6f, 2.6f, 0.3f, wood).AddComponent<TreeTop>();
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

        /// <summary>
        /// 15.11: you can't walk through things. Every standing decoration in the lobby (rocks, bushes, pine branches,
        /// snowmen, crystals, bench backs…) gets a box; flat decals, water, things overhead, moving or glowing bits,
        /// text and characters are left as they are.
        /// </summary>
        static void MakeSolid()
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = r.gameObject;
                if (go.GetComponent<Collider>() || go.GetComponent<TMPro.TMP_Text>()) continue;
                bool skip = false;
                for (var t = go.transform.parent; t != null && t != root; t = t.parent)
                    if (t.GetComponent<Collider>() || t.GetComponent<ChubrikModel>() || t.GetComponent<LobbyGate>()) { skip = true; break; }
                if (skip || go.GetComponentInParent<Spin>() || go.GetComponentInParent<Pulse>() || go.GetComponentInParent<Mover>() || go.GetComponent<WaterScroll>()) continue;
                if (go.name is "River" or "Lake" or "Pool" or "Pond" or "Water") continue;
                var b = r.bounds;
                if (b.size.x > 9 || b.size.z > 9 || b.min.y > 2.2f || b.size.y < 0.15f || Mathf.Min(b.size.x, b.size.z) < 0.06f) continue;
                go.AddComponent<BoxCollider>();
            }
        }

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

        /// <summary>Signs are gone (15.11, the player asked): the call sites stay, nothing is built.</summary>
        internal static void Sign(Vector3 pos, float yaw, string text, string col = "FFC83A", float w = 4f) { }

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
            if (!building) return;   // only the lobby trees (other worlds borrow this tree)
            // 18.11: you can climb every tree: branches round the trunk like steps, a leafy nest on top.
            // Jump off the top with two jumps — and you fall into the backrooms
            var bark = Mats.Tex(Tex.Bark(), Color.white, 0.1f);
            float a0 = r.F(0, 360);
            for (int k = 0; k < 6; k++)
            {
                float a = (a0 + k * 68) * Mathf.Deg2Rad, y = (0.75f + k * 0.85f) * s;
                var br = Solid(PrimitiveType.Cube, p + new Vector3(Mathf.Cos(a) * 0.85f * s, y, Mathf.Sin(a) * 0.85f * s), new Vector3(1.1f * s, 0.2f, 0.55f), bark, new Vector3(0, -(a0 + k * 68), 0));
                br.name = "TreeBranch";
                S(root, br.transform.position + new Vector3(Mathf.Cos(a) * 0.55f * s, 0.15f, Mathf.Sin(a) * 0.55f * s), 0.45f * s, M(greens[k % greens.Length], 0.15f));
            }
            var top = Solid(PrimitiveType.Cylinder, p + Vector3.up * 5.95f * s, new Vector3(2.4f * s, 0.12f, 2.4f * s), M("43A047", 0.15f));
            top.name = "TreeTop";
            top.AddComponent<TreeTop>();
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
                if (building && river != null && river.Edge(p) < 1.6f) continue;   // 22.11 none over the river or under its bridges
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
        /// <summary>The invisible wall over the gate (autotest).</summary>
        public Collider Barrier => barrier;
        Collider col, barrier; Transform panel; TMPro.TextMeshPro front, back; string title; int shown = -1; float openT = -1, hintT, width;

        public static LobbyGate Spawn(Vector3 p, float yaw, float width, int need, string title, Transform parent)
        {
            var g = new GameObject("Gate " + title).AddComponent<LobbyGate>();
            g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            g.need = need; g.title = title; g.width = width;
            Parts.Layer = Layers.World;
            var bc = g.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 2.75f, 0); bc.size = new Vector3(width, 5.5f, 0.6f);
            g.col = bc;
            // v0.31: over a closed gate an invisible wall goes on up (like over the walls) — no climbing over it
            var bar = new GameObject("GateBarrier") { layer = Layers.Barrier };
            bar.transform.SetParent(g.transform, false);
            var bb = bar.AddComponent<BoxCollider>();
            bb.center = new Vector3(0, 5.5f + Lobby.BarrierH / 2, 0); bb.size = new Vector3(width + 1.2f, Lobby.BarrierH, 1.0f);
            g.barrier = bb;
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

        /// <summary>22.11 a gate over the river: iron bars from it down to the riverbed (part of the gate — they go when it opens).</summary>
        public void Grate(float depth)
        {
            Parts.Layer = Layers.World;
            var gr = Parts.Node("Grate", panel, Vector3.zero);
            var bc = gr.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, -depth / 2, 0); bc.size = new Vector3(width, depth, 0.5f);
            var iron = Parts.M("3A4A5E", 0.5f, 0.6f);
            int n = Mathf.Max(2, Mathf.RoundToInt(width / 0.42f));
            for (int k = 0; k <= n; k++) Parts.B(gr, new Vector3(-width / 2 + k * width / n, -depth / 2, 0), new Vector3(0.1f, depth, 0.1f), iron);
            foreach (var y in new[] { -0.55f, -depth + 0.4f }) Parts.B(gr, new Vector3(0, y, 0), new Vector3(width, 0.12f, 0.14f), iron);
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
                IsOpen = true; col.enabled = false; barrier.enabled = false;
                if (instant) panel.gameObject.SetActive(false);
                else { openT = 0; AudioFx.Play("portal"); Effects.Emit("magic", transform.position + Vector3.up * 3, 60, 1.2f, Vector3.up * 2); Hud.Toast($"Открылся путь: {title}!", UIKit.Lime); }
            }
            else if (n < need && IsOpen) { IsOpen = false; openT = -1; col.enabled = true; barrier.enabled = true; panel.gameObject.SetActive(true); panel.localPosition = Vector3.zero; }   // also stops a running open animation
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
