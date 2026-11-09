using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 1 — the lobby: a sunny safe island without enemies, laid out after the player's own drawn map:
    /// hub with a river, the winking-cube wardrobe, the red shop and the blue premium shop, portals to all worlds
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
        public static readonly Vector3 TestMushroom = new Vector3(3, 0, -4), PadFrom = new Vector3(6, 0.07f, -12), PadTo = new Vector3(-6, 7.6f, 17.5f), CactusPos = new Vector3(-22, 0, 0);
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
            Wardrobe(new Vector3(-6, 0, 17.5f));
            Shop(new Vector3(-7.5f, 0, -8.5f));
            Premium(new Vector3(-7.5f, 0, -17));
            PortalArch(new Vector3(18, 0, 19.5f), 0, 2, "ПАРЯЩИЕ РУИНЫ", "B57CFF", M("9AA3AE", 0.2f), "ruins");
            Park();
            Glitch();
            Jungle();
            Snow();
            CrystalCave();
            Gates.Add(LobbyGate.Spawn(new Vector3(-3.25f, 0, 23.5f), 0, 4.6f, 4, "СНЕЖНАЯ ДОЛИНА", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(27, 0, 16.5f), 90, 7.2f, 4, "СНЕЖНАЯ ДОЛИНА", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(5, 0, -21.5f), 0, 22.2f, 10, "ДЖУНГЛИ", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(27.6f, 0, -3.5f), 90, 5.2f, 20, "КРИСТАЛЬНАЯ ПЕЩЕРА", root));
            Gates.Add(LobbyGate.Spawn(new Vector3(-33, 0, -6), 0, 6.2f, 25, "ГЛЮК-МИР 404", root));
            Clouds();
            Tokens();
            return root;
        }

        // ---------------- ground, sea, regions ----------------

        static void Sea()
        {
            var water = Prim.Make(PrimitiveType.Plane, root, new Vector3(0, PlayerCtl.I != null ? PlayerCtl.I.WaterY : -0.9f, 0), new Vector3(30, 1, 30), null, "Water", 0);
            var wm = Mats.Tex(Tex.Water(), new Color(0.75f, 0.9f, 1f), 0.9f);
            wm.mainTextureScale = new Vector2(40, 40);
            water.GetComponent<Renderer>().sharedMaterial = wm;
            water.AddComponent<WaterScroll>();
        }

        static GameObject Slab(float x0, float x1, float z0, float z1, Material m)
        {
            var go = Box(new Vector3((x0 + x1) / 2, -1, (z0 + z1) / 2), new Vector3(x1 - x0, 2, z1 - z0), m);
            B(root, new Vector3((x0 + x1) / 2, -1.6f, (z0 + z1) / 2), new Vector3(x1 - x0 + 0.6f, 1.4f, z1 - z0 + 0.6f), Tiled(Tex.Sand(), 6));   // a sandy rim at the waterline
            return go;
        }

        static void Regions()
        {
            Slab(-12, 27, -21.5f, 23.5f, grass);                 // the hub
            Slab(-49, -12, -6, 17, grass);                        // the park
            var g = Slab(-45, -22, -22, -6, checker);             // the glitch area
            g.GetComponent<Renderer>().sharedMaterial = checker;
            Slab(-8, 26, -51, -21.5f, Tiled(Tex.Grass(), 8));     // the jungle
            Slab(-6, 49, 23.5f, 50, snowM);                       // the snowy valley
            Slab(27, 49, 7, 23.5f, snowM);
            Slab(27, 29, -6, -1, Tiled(Tex.Concrete(), 2));       // the corridor to the crystal cave
            Disc(new Vector3(39, 0, -8.5f), 11, 2, Tiled(Tex.Concrete(), 6));
        }

        static void Water(Vector3 a, Vector3 b, float w)
        {
            var d = b - a; d.y = 0;
            var go = Prim.Make(PrimitiveType.Cube, root, (a + b) / 2 + Vector3.up * 0.03f, new Vector3(w, 0.04f, d.magnitude + w * 0.6f), waterM, "River", Layers.World);
            go.transform.rotation = Quaternion.LookRotation(d);
        }

        /// <summary>The river from the snowy lake down through the hub into the jungle (shallow: you can wade it).</summary>
        static void Rivers()
        {
            Vector3[] p = { new Vector3(36, 0, 31), new Vector3(34, 0, 24), new Vector3(29, 0, 17.5f), new Vector3(23, 0, 12), new Vector3(17, 0, 6), new Vector3(12, 0, 0),
                            new Vector3(9, 0, -8), new Vector3(8, 0, -16), new Vector3(9, 0, -25), new Vector3(10, 0, -36), new Vector3(9, 0, -48) };
            for (int i = 0; i < p.Length - 1; i++) Water(p[i], p[i + 1], 4.4f);
            // the lake in the snowy valley
            Prim.Make(PrimitiveType.Cube, root, new Vector3(39, 0.03f, 40), new Vector3(18, 0.04f, 18), waterM, "Lake", Layers.World).AddComponent<WaterScroll>();
            // stepping stones over the river by the hub
            for (int k = 0; k < 4; k++) Solid(PrimitiveType.Cylinder, new Vector3(9.6f + k * 1.3f, 0.1f, -2.5f + k * 0.7f), new Vector3(0.9f, 0.1f, 0.9f), M("9AA0A8", 0.3f));
        }

        // ---------------- walls ----------------

        static void Wall(Vector3 a, Vector3 b, float h = 5.5f)
        {
            var d = b - a; d.y = 0;
            if (d.magnitude < 0.1f) return;
            var go = Box((a + b) / 2 + Vector3.up * h / 2, new Vector3(1, h, d.magnitude + 1), wallM, Quaternion.LookRotation(d).eulerAngles.y);
            B(go.transform, new Vector3(0, 0.5f, 0), new Vector3(1.15f, 0.06f, 1.02f), M("8A919C", 0.3f));   // a stone cap
        }

        static void WallsX(float z, float x0, float x1, params float[] gaps)
        {
            float a = x0;
            for (int i = 0; i + 1 < gaps.Length; i += 2) { Wall(new Vector3(a, 0, z), new Vector3(gaps[i], 0, z)); a = gaps[i + 1]; }
            Wall(new Vector3(a, 0, z), new Vector3(x1, 0, z));
        }

        static void WallsZ(float x, float z0, float z1, params float[] gaps)
        {
            float a = z0;
            for (int i = 0; i + 1 < gaps.Length; i += 2) { Wall(new Vector3(x, 0, a), new Vector3(x, 0, gaps[i])); a = gaps[i + 1]; }
            Wall(new Vector3(x, 0, a), new Vector3(x, 0, z1));
        }

        static void Walls()
        {
            // hub
            WallsX(23.5f, -12, 27, -5.6f, -0.9f);                // wardrobe gate to the snow
            WallsZ(27, -21.5f, 23.5f, -6.2f, -0.8f, 12.9f, 20.1f); // crystal gate, river gate
            WallsX(-21.5f, -12, 27, -6.1f, 16.1f);               // jungle gate
            WallsZ(-12, -21.5f, -6); WallsZ(-12, 17, 23.5f);
            // park
            WallsX(17, -49, -12); WallsZ(-49, -6, 17);
            WallsX(-6, -49, -12, -36.1f, -29.9f);                // glitch gate
            // glitch area
            WallsZ(-45, -22, -6); WallsX(-22, -45, -22); WallsZ(-22, -22, -6);
            // jungle
            WallsZ(-8, -51, -21.5f); WallsX(-51, -8, 26); WallsZ(26, -51, -21.5f);
            // snowy valley
            WallsX(50, -6, 49); WallsZ(-6, 23.5f, 50); WallsZ(49, 7, 50); WallsX(7, 27, 49);
            // the crystal cave: a round wall with an opening to the west
            var c = new Vector3(39, 0, -8.5f);
            for (int k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI * 2 / 16, a1 = (k + 1) * Mathf.PI * 2 / 16;
                float mid = (a0 + a1) / 2;
                if (Mathf.Abs(Mathf.DeltaAngle(mid * Mathf.Rad2Deg, 180)) < 18) continue;   // the way in
                Wall(c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 11, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 11, 7);
            }
            Wall(new Vector3(27, 0, -6.2f), new Vector3(28.6f, 0, -6.2f)); Wall(new Vector3(27, 0, -0.8f), new Vector3(28.6f, 0, -0.8f));
        }

        // ---------------- the hub ----------------

        static void Hub()
        {
            var stone = Tiled(Tex.Sandstone(), 4);
            Disc(new Vector3(0, 0.05f, 4), 5.5f, 0.1f, stone);
            Sign(new Vector3(-3.6f, 0, 8.8f), 0, "ЛОББИ ЧУБРИКОВ", "3D8BFF", 5f);
            Bench(new Vector3(4.5f, 0, 8), 200); Bench(new Vector3(-5, 0, 0), 100);
            Lamp(new Vector3(4, 0, 0)); Lamp(new Vector3(-4, 0, 8));
            foreach (var p in new[] { new Vector3(-9, 0, 7), new Vector3(3, 0, 15), new Vector3(-9, 0, -2), new Vector3(20, 0, -10), new Vector3(22, 0, 3), new Vector3(1, 0, -16), new Vector3(15, 0, -17), new Vector3(-2, 0, 20.5f) })
                Tree(p, 0.95f, (int)(p.x * 7 + p.z));
            Flowers(new Vector3(6, 0, 2), 14, 70, 5);
            // a mushroom up to the shop roof and a teleport pad up to the wardrobe roof
            Mushroom(TestMushroom, 18.5f);
            TokenSpots.Add(TestMushroom + Vector3.up * 6.2f);                   // high over the mushroom
            TokenSpots.Add(new Vector3(-10.5f, 1, 22.2f));                       // hidden behind the wardrobe
            Pad(PadFrom, PadTo, 0, "35E0FF");
            // training corner: crates and target dummies
            var crate = Tiled(Tex.Planks("C08040"), 1);
            var rng = new Rng(4);
            for (int k = 0; k < 5; k++)
            {
                var p = new Vector3(19 + (k % 3) * 1.8f, 0.55f + (k / 3) * 1.1f, 16 - (k % 2) * 1.2f);
                var b = Box(p, Vector3.one * 1.1f, crate, rng.F(-10, 10));
                B(b.transform, Vector3.zero, new Vector3(1.04f, 0.15f, 1.04f), M("6D4425"));
                b.AddComponent<Breakable>().coins = 0;
            }
            Sign(new Vector3(12, 0, 10), 200, "ПАРЯЩИЕ РУИНЫ — ВВЕРХ ПО РЕКЕ", "B57CFF", 5f);
        }

        // ---------------- buildings ----------------

        /// <summary>The wardrobe: a big grey cube with a smiling, winking face.</summary>
        static void Wardrobe(Vector3 c)
        {
            var grey = M("A8AEB8", 0.3f); var dark = M("2A2E36", 0.5f); var pink = M("FF8FA8", 0.3f);
            Box(c + Vector3.up * 3.5f, new Vector3(9, 7, 7), grey);
            B(root, c + new Vector3(0, 7.05f, 0), new Vector3(9.3f, 0.12f, 7.3f), M("8A919C", 0.3f));
            float f = c.z - 3.52f;   // the front face (towards the hub)
            // the open eye
            Cy(root, new Vector3(c.x - 2, 4.6f, f), new Vector3(1.6f, 0.05f, 1.9f), M("FFFFFF", 0.4f), new Vector3(90, 0, 0));
            Cy(root, new Vector3(c.x - 2, 4.4f, f - 0.03f), new Vector3(0.8f, 0.05f, 1f), dark, new Vector3(90, 0, 0));
            Cy(root, new Vector3(c.x - 1.8f, 4.7f, f - 0.06f), new Vector3(0.25f, 0.05f, 0.25f), M("FFFFFF"), new Vector3(90, 0, 0));
            // the winking eye: a happy closed arc
            for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 2.4f; B(root, new Vector3(c.x + 2 + Mathf.Sin(a) * 0.9f, 4.5f + Mathf.Cos(a) * 0.35f, f - 0.03f), new Vector3(0.3f, 0.18f, 0.05f), dark, new Vector3(0, 0, -a * 30)); }
            // the smile and the cheeks
            for (int k = 0; k < 11; k++) { float a = (k / 10f - 0.5f) * 2.6f; Cy(root, new Vector3(c.x + Mathf.Sin(a) * 2.2f, 2.4f - Mathf.Cos(a) * 0.9f + 0.9f, f - 0.03f), new Vector3(0.45f, 0.05f, 0.45f), dark, new Vector3(90, 0, 0)); }
            foreach (var sd in new[] { -1, 1 }) Cy(root, new Vector3(c.x + sd * 3.3f, 3f, f - 0.02f), new Vector3(1.2f, 0.05f, 0.7f), pink, new Vector3(90, 0, 0));
            var t = Prim.Text(root, "ГАРДЕРОБ", new Vector3(c.x, 6.4f, f - 0.05f), 6, Hex("FFFFFF"));
            t.transform.rotation = Quaternion.identity;
            Npc(new Vector3(c.x - 4, 0.05f, f - 1.5f), 160, "hat_4", "neck_5", "top_5", "color_3", "emote_5", "shoes_4");
            Npc(new Vector3(c.x + 4, 0.05f, f - 1.5f), 200, "hat_6", "wings_0", "color_7", "tail_4", "emote_1", "weapon_2");
            Zones.Add(new Vector3(c.x, 0, f - 1.6f), 3.4f, "ГАРДЕРОБ", () => WardrobeUI.Open(), () => Worlds.Current == 1);
            TokenSpots.Add(c + new Vector3(2.5f, 8, 0.5f));                    // on the roof (the teleport pad brings you up)
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
            Box(c + new Vector3(3.2f, 0.6f, 3.6f), new Vector3(1.3f, 1.2f, 1.3f), crate);
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
            // the heart-shaped pond
            var pc = new Vector3(-41, 0, 11);
            foreach (var o in new[] { new Vector3(-1.6f, 0, 1.2f), new Vector3(1.6f, 0, 1.2f) }) Prim.Make(PrimitiveType.Cylinder, root, pc + o + Vector3.up * 0.03f, new Vector3(4.2f, 0.02f, 4.2f), waterM, "Pond", Layers.World);
            var tip = Prim.Make(PrimitiveType.Cube, root, pc + new Vector3(0, 0.03f, -0.9f), new Vector3(4.2f, 0.04f, 4.2f), waterM, "Pond", Layers.World);
            tip.transform.rotation = Quaternion.Euler(0, 45, 0);
            Solid(PrimitiveType.Sphere, pc + new Vector3(0, 0.3f, 0.5f), new Vector3(1.6f, 0.9f, 1.6f), M("9AA0A8", 0.2f));
            TokenSpots.Add(pc + new Vector3(0, 1.4f, 0.5f));
            foreach (var p in new[] { new Vector3(-32, 0, 12), new Vector3(-26, 0, 4), new Vector3(-33, 0, 3), new Vector3(-22, 0, 12), new Vector3(-17, 0, 5), new Vector3(-44, 0, 1), new Vector3(-38, 0, -2) })
                Tree(p, 1f, (int)(p.x * 3 - p.z));
            Flowers(new Vector3(-30, 0, 6), 14, 90, 9);
            Bench(new Vector3(-36, 0, 7), 0); Bench(new Vector3(-28, 0, 14.5f), 180);
            TreeHouse(new Vector3(-20, 0, 14));
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
                var d = Node("Dummy", root, new Vector3(-26 + k * 2.4f, 0, -3));
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
                Box(new Vector3(r.F(-42, -26), 0.8f + (k % 2) * 0.8f, r.F(-20, -10)), new Vector3(1.6f, 1.6f + (k % 2) * 1.6f, 1.6f), checker, r.F(0, 45));
            TokenSpots.Add(new Vector3(-30, 5, -14));
            Mushroom(new Vector3(-30, 0, -18), 17f);
            PortalArch(new Vector3(-42, 0, -14), 90, 6, "ГЛЮК-МИР 404", "FF5FD2", checker, "glitch");
        }

        static void Jungle()
        {
            foreach (var p in new[] { new Vector3(-4, 0, -27), new Vector3(20, 0, -27), new Vector3(16, 0, -44), new Vector3(-5, 0, -36), new Vector3(22, 0, -38), new Vector3(3, 0, -30) })
                Tree(p, 1.15f, (int)(p.z * 5));
            // the striped posts from the drawing
            foreach (var p in new[] { new Vector3(-1, 0, -33), new Vector3(18, 0, -32) })
            {
                var stripes = Mats.Tex(Patterns.Stripes("D9822B", "6B4A2A", 6, true), Color.white, 0.3f);
                Solid(PrimitiveType.Cylinder, p + Vector3.up * 1.6f, new Vector3(0.8f, 1.6f, 0.8f), stripes);
                Disc(p + Vector3.up * 3.3f, 0.7f, 0.2f, M("6B4A2A", 0.3f));
            }
            TokenSpots.Add(new Vector3(18, 4.2f, -32));
            Mushroom(new Vector3(16, 0, -30.5f), 15f);
            Flowers(new Vector3(5, 0, -38), 10, 60, 13);
            PortalArch(new Vector3(-2, 0, -45), 180, 5, "ДЖУНГЛИ", "6EDC3C", M("8A8A6A", 0.2f), "jungle");
        }

        static void Snow()
        {
            foreach (var p in new[] { new Vector3(-2, 0, 30), new Vector3(10, 0, 33), new Vector3(15, 0, 46), new Vector3(22, 0, 29), new Vector3(-3, 0, 46), new Vector3(30, 0, 13), new Vector3(42, 0, 10), new Vector3(25, 0, 40) })
            {
                Pine(p, 1.2f);
                S(root, p + Vector3.up * 3.2f, new Vector3(1.6f, 0.4f, 1.6f), M("FFFFFF", 0.1f));
            }
            var rng = new Rng(51);
            for (int k = 0; k < 14; k++) S(root, new Vector3(rng.F(-4, 28), 0, rng.F(26, 48)), new Vector3(rng.F(1.2f, 2.6f), rng.F(0.5f, 1f), rng.F(1.2f, 2.6f)), M("F4FAFF", 0.15f));
            // a snowman by the portal
            S(root, new Vector3(12, 0.6f, 42), 1.2f, M("FFFFFF", 0.15f)); S(root, new Vector3(12, 1.45f, 42), 0.85f, M("FFFFFF", 0.15f)); S(root, new Vector3(12, 2.1f, 42), 0.6f, M("FFFFFF", 0.15f));
            Cone(root, new Vector3(12, 2.1f, 41.7f), new Vector3(0.1f, 0.3f, 0.1f), M("FF8A1E"), new Vector3(-90, 0, 0));
            PortalArch(new Vector3(5, 0, 44), 0, 4, "СНЕЖНАЯ ДОЛИНА", "8FD8FF", Glass("A8DCFF", 0.85f), "snow");
            // the water cave by the lake: a rocky dome with water inside and a token
            var rock = M("8A93A0", 0.2f);
            var cc = new Vector3(41, 0, 44);
            for (int k = 0; k < 9; k++)
            {
                float a = k * Mathf.PI / 8;   // the back half (north): the cave opens towards the lake
                Solid(PrimitiveType.Cube, cc + new Vector3(Mathf.Cos(a) * 5, 2.5f, Mathf.Sin(a) * 5), new Vector3(2.4f, 5, 1.2f), rock, new Vector3(0, -a * Mathf.Rad2Deg + 90, 0));
            }
            Solid(PrimitiveType.Sphere, cc + new Vector3(0, 4.6f, 1.5f), new Vector3(11, 4, 8), rock);
            for (int k = 0; k < 6; k++) Spike(root, cc + new Vector3(-3 + k * 1.2f, 2.8f, 0), cc + new Vector3(-3 + k * 1.2f, 1.8f, 0), 0.3f, M("BFE8FF", 0.8f));
            TokenSpots.Add(cc + new Vector3(0, 1, -1));
            Sign(new Vector3(36, 0, 31.5f), 0, "ПЕЩЕРА С ВОДОЙ", "35E0FF", 4);
        }

        static void CrystalCave()
        {
            var c = new Vector3(39, 0, -8.5f);
            // a partly open rocky roof
            var rock = M("6E6488", 0.2f);
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; S(root, c + new Vector3(Mathf.Cos(a) * 7, 7.4f, Mathf.Sin(a) * 7), new Vector3(7, 2.2f, 7), rock); }
            // the big red crystals from the drawing
            var red = G("E5395A", 1.4f);
            foreach (var p in new[] { new Vector3(45, 0, -3), new Vector3(46, 0, -14), new Vector3(36, 0, -17), new Vector3(33, 0, 0) })
                for (int k = 0; k < 4; k++)
                {
                    var b = p + new Vector3((k % 2 - 0.5f) * 1.2f, 0, (k / 2 - 0.5f) * 1.2f);
                    Spike(root, b, b + new Vector3((k % 2 - 0.5f), 2.6f + k * 0.4f, (k / 2 - 0.5f)), 1.1f, red);
                }
            Disc(c + Vector3.up * 0.02f, 9.5f, 0.04f, M("3B3052", 0.3f));
            TokenSpots.Add(new Vector3(46, 4, -14));
            Mushroom(new Vector3(43, 0, -12), 15f);
            PortalArch(new Vector3(45, 0, -8.5f), 90 + 180, 3, "КРИСТАЛЬНАЯ ПЕЩЕРА", "35E0FF", rock, "crystal");
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
