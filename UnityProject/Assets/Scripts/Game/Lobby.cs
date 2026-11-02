using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 1 — the lobby: a sunny safe island without enemies. Plaza with a fountain, the gadget shop, the
    /// premium kiosk, the wardrobe, closed portals to future worlds, a jump course with a tower, bounce mushrooms,
    /// floating islands, crates, target dummies, a training cactus, a musical path, coins and 10 hidden tokens.
    /// </summary>
    public static class Lobby
    {
        static Transform root;
        public static readonly Vector3 Spawn = new Vector3(0, 0.3f, -4);
        public static readonly List<Vector3> TokenSpots = new List<Vector3>();

        public static Transform Build()
        {
            root = new GameObject("Lobby").transform;
            Parts.Layer = Layers.World;
            Sky();
            Ground();
            Plaza();
            Shop(new Vector3(22, 0, 0));
            Premium(new Vector3(16, 0, 17));
            Wardrobe(new Vector3(-22, 0, 0));
            Portals();
            Course();
            Training();
            Music(new Vector3(-24, 0, -18));
            House(new Vector3(-24, 0, 24));
            TreeHouse(new Vector3(26, 0, -26));
            Nature();
            Coins();
            Tokens();
            return root;
        }

        // ---------------- helpers ----------------

        static GameObject Solid(PrimitiveType t, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default, Transform parent = null)
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

        static GameObject Box(Vector3 pos, Vector3 size, Material m, float yaw = 0) => Solid(PrimitiveType.Cube, pos, size, m, new Vector3(0, yaw, 0));
        static GameObject Disc(Vector3 top, float r, float h, Material m) => Solid(PrimitiveType.Cylinder, top - Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), m);

        static Material Tiled(Texture2D t, float tile, float smooth = 0.15f)
        {
            var m = Mats.Tex(t, Color.white, smooth);
            m.mainTextureScale = new Vector2(tile, tile);
            return m;
        }

        static void Sign(Vector3 pos, float yaw, string text, string col = "FFC83A", float w = 4f)
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

        static ChubrikModel Npc(Vector3 pos, float yaw, params string[] items)
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

        // ---------------- sky, light, ground ----------------

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

        static void Ground()
        {
            Disc(Vector3.zero, 58, 2, Tiled(Tex.Grass(), 36));
            Disc(new Vector3(0, -0.3f, 0), 66, 2.2f, Tiled(Tex.Sand(), 40));
            var water = Prim.Make(PrimitiveType.Plane, root, new Vector3(0, PlayerCtl.I != null ? PlayerCtl.I.WaterY : -0.9f, 0), new Vector3(60, 1, 60), null, "Water", 0);
            var wm = Mats.Tex(Tex.Water(), new Color(0.75f, 0.9f, 1f), 0.9f);
            wm.mainTextureScale = new Vector2(60, 60);
            water.GetComponent<Renderer>().sharedMaterial = wm;
            water.AddComponent<WaterScroll>();
            // paths (light paving)
            var path = Tiled(Tex.Sandstone(), 4);
            void Path(Vector3 a, Vector3 b, float w)
            {
                var d = b - a; d.y = 0;
                var go = Prim.Make(PrimitiveType.Cube, root, (a + b) / 2 + Vector3.up * 0.015f, new Vector3(w, 0.03f, d.magnitude), path, "Path", Layers.World);
                go.transform.rotation = Quaternion.LookRotation(d);
            }
            Path(new Vector3(7, 0, 0), new Vector3(18, 0, 0), 3);
            Path(new Vector3(-7, 0, 0), new Vector3(-18, 0, 0), 3);
            Path(new Vector3(0, 0, 8), new Vector3(0, 0, 30), 3);
            Path(new Vector3(0, 0, -8), new Vector3(0, 0, -16), 3);
            Path(new Vector3(5, 0, 5), new Vector3(14, 0, 14), 2.4f);
        }

        // ---------------- plaza + fountain ----------------

        static void Plaza()
        {
            var stone = Tiled(Tex.Sandstone(), 6);
            Disc(new Vector3(0, 0.05f, 0), 8, 0.4f, stone);
            var rim = M("E8DCC8", 0.3f);
            // fountain
            var f = new Vector3(0, 0, 6);
            Disc(f + Vector3.up * 0.7f, 2.4f, 0.7f, rim);
            var w = Prim.Make(PrimitiveType.Cylinder, root, f + Vector3.up * 0.66f, new Vector3(4.2f, 0.02f, 4.2f), Glass("4FC3F7", 0.75f), "FountainWater", Layers.World);
            Solid(PrimitiveType.Cylinder, f + Vector3.up * 1.2f, new Vector3(0.6f, 0.6f, 0.6f), rim);
            Disc(f + Vector3.up * 2f, 1.2f, 0.25f, rim);
            var spray = new GameObject("Spray").AddComponent<FountainFx>();
            spray.transform.position = f + Vector3.up * 2.1f;
            Sign(new Vector3(-3.5f, 0, 9.2f), 0, "ЛОББИ ЧУБРИКОВ", "3D8BFF", 5f);
            // benches + lamps
            for (int k = 0; k < 4; k++)
            {
                float a = 45 + k * 90;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Bench(d * 9.5f, a + 180);
                Lamp(Quaternion.Euler(0, a + 45, 0) * Vector3.forward * 8.6f);
            }
            Sign(new Vector3(4.5f, 0, -6.5f), 200, "МАГАЗИН — ВПРАВО", "FF9A2E", 4.4f);
            Sign(new Vector3(-4.5f, 0, -6.5f), 160, "ГАРДЕРОБ — ВЛЕВО", "FF5FA2", 4.4f);
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

        // ---------------- buildings ----------------

        static void Shop(Vector3 c)
        {
            var wall = M("FFE3B3", 0.2f); var trim = M("FF8A1E", 0.3f); var wood = Tiled(Tex.Planks("A0642F"), 2);
            var b = Node("Shop", root, c, new Vector3(0, -90, 0));   // front faces -X (towards the plaza)
            Solid(PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(9, 0.2f, 8), wood, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 2.3f, -3.8f), new Vector3(9, 4.4f, 0.4f), wall, default, b);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cube, new Vector3(sd * 4.3f, 2.3f, 0), new Vector3(0.4f, 4.4f, 8), wall, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 4.7f, 0), new Vector3(9.6f, 0.4f, 8.6f), trim, default, b);
            Msh(Meshes.Cone(4), b, new Vector3(0, 4.9f, 0), new Vector3(6f, 2.4f, 6f), M("E65100", 0.3f), new Vector3(0, 45, 0));
            // striped awning + counter
            var awn = Mats.Tex(Patterns.Stripes("FF8A1E", "FFFFFF", 8, true), Color.white, 0.2f);
            B(b, new Vector3(0, 3.9f, 4.4f), new Vector3(9.2f, 0.12f, 1.6f), awn, new Vector3(-18, 0, 0));
            Solid(PrimitiveType.Cube, new Vector3(0, 0.75f, 1.2f), new Vector3(6, 1.1f, 0.9f), M("8D5524", 0.3f), default, b);
            B(b, new Vector3(0, 1.33f, 1.2f), new Vector3(6.2f, 0.08f, 1.1f), M("FFFFFF", 0.4f));
            // goods on shelves
            for (int k = 0; k < 5; k++)
            {
                B(b, new Vector3(-3 + k * 1.5f, 2.2f, -3.4f), new Vector3(1.2f, 0.06f, 0.5f), M("8D5524"));
                var gift = B(b, new Vector3(-3 + k * 1.5f, 2.5f, -3.4f), new Vector3(0.5f, 0.5f, 0.5f), M(new[] { "E53935", "1E88E5", "43A047", "8E24AA", "FFC400" }[k], 0.4f));
                gift.AddComponent<Bob>().amp = 0.05f;
            }
            S(b, new Vector3(-2, 1.6f, 1.2f), 0.5f, M("FFC83A", 0.9f, 0.85f));   // a big coin on the counter
            var sign = Prim.Text(b, "МАГАЗИН ПРИМОЧЕК", new Vector3(0, 5.3f, 4.35f), 6, Hex("FFF3C4"));
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Npc(b.TransformPoint(new Vector3(0, 0.2f, -0.6f)), b.eulerAngles.y + 180, "hat_0", "top_4", "color_1", "emote_6", "eyes_1");
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 3.2f)), 3.4f, "МАГАЗИН ПРИМОЧЕК", () => ShopUI.Open());
            // crates as steps up to the roof (a token waits there)
            var crate = Tiled(Tex.Planks("C08040"), 1);
            Box(c + new Vector3(2, 0.6f, 5.6f), new Vector3(1.4f, 1.2f, 1.4f), crate);
            Box(c + new Vector3(3.6f, 1.2f, 5.4f), new Vector3(1.4f, 2.4f, 1.4f), crate);
            Box(c + new Vector3(4.8f, 1.8f, 4.2f), new Vector3(1.4f, 3.6f, 1.4f), crate);
            TokenSpots.Add(c + new Vector3(3.6f, 5.7f, 3.4f));
        }

        static void Premium(Vector3 c)
        {
            var gold = M("FFC83A", 0.9f, 0.85f); var white = M("FFF8E7", 0.4f);
            Disc(c + Vector3.up * 0.4f, 4.2f, 0.4f, white);
            Ring(root, c + Vector3.up * 0.4f, 4.2f, 0.12f, gold);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                Solid(PrimitiveType.Cylinder, c + new Vector3(Mathf.Cos(a) * 3.6f, 2.4f, Mathf.Sin(a) * 3.6f), new Vector3(0.35f, 2f, 0.35f), gold);
            }
            Disc(c + Vector3.up * 4.7f, 4.4f, 0.3f, gold);
            S(root, c + Vector3.up * 4.7f, new Vector3(7.6f, 4.4f, 7.6f), M("FFF1C2", 0.6f, 0.3f));
            var star = Msh(Meshes.Star(), root, c + Vector3.up * 7.6f, Vector3.one * 1.3f, G("FFE14D", 1.8f));
            star.AddComponent<Spin>();
            // 8 premium gift boxes on a turning stand
            var stand = Node("Stand", root, c + Vector3.up * 0.4f);
            stand.gameObject.AddComponent<Spin>().speed = 12;
            for (int k = 0; k < Catalog.Sets.Count; k++)
            {
                float a = k * Mathf.PI * 2 / 8;
                var p = new Vector3(Mathf.Cos(a) * 2.2f, 0.45f, Mathf.Sin(a) * 2.2f);
                var col = Catalog.Sets[k].color;
                B(stand, p, Vector3.one * 0.8f, Mats.Lit(col, 0.5f, 0.2f));
                B(stand, p, new Vector3(0.84f, 0.84f, 0.16f), gold);
                B(stand, p, new Vector3(0.16f, 0.84f, 0.84f), gold);
                S(stand, p + Vector3.up * 0.48f, new Vector3(0.3f, 0.2f, 0.3f), gold);
            }
            var t = Prim.Text(root, "ПРЕМИУМ-НАБОРЫ", c + new Vector3(0, 5.9f, 0), 6, Hex("FFE14D"));
            t.gameObject.AddComponent<Billboard>();
            Zones.Add(c, 4.6f, "ПРЕМИУМ-НАБОРЫ", () => PremiumUI.Open());
        }

        static void Wardrobe(Vector3 c)
        {
            var wall = M("FFD1E8", 0.2f); var trim = M("FF5FA2", 0.3f); var floor = Tiled(Tex.Planks("C49A6C"), 2);
            var b = Node("Wardrobe", root, c, new Vector3(0, 90, 0));   // front faces +X
            Solid(PrimitiveType.Cube, new Vector3(0, 0.1f, 0), new Vector3(9, 0.2f, 8), floor, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 2.3f, -3.8f), new Vector3(9, 4.4f, 0.4f), wall, default, b);
            foreach (var sd in new[] { -1, 1 }) Solid(PrimitiveType.Cube, new Vector3(sd * 4.3f, 2.3f, 0), new Vector3(0.4f, 4.4f, 8), wall, default, b);
            Solid(PrimitiveType.Cube, new Vector3(0, 4.7f, 0), new Vector3(9.6f, 0.4f, 8.6f), trim, default, b);
            S(b, new Vector3(0, 4.9f, 0), new Vector3(9f, 3.4f, 8f), M("D81B60", 0.4f));
            // big mirror and clothes racks
            B(b, new Vector3(0, 2.2f, -3.55f), new Vector3(3, 3.2f, 0.1f), M("E8EEF5", 0.98f, 1f));
            Ring(b, new Vector3(0, 2.2f, -3.5f), 1.7f, 0.12f, M("FFC83A", 0.85f, 0.8f), new Vector3(90, 0, 0), new Vector3(0.9f, 1, 1.05f));
            foreach (var sd in new[] { -1, 1 })
            {
                Rod(b, new Vector3(sd * 3.4f, 2.6f, -2.5f), new Vector3(sd * 3.4f, 2.6f, 2), 0.06f, M("B0BAC6", 0.8f, 0.8f));
                for (int k = 0; k < 6; k++)
                    B(b, new Vector3(sd * 3.4f, 1.9f, -2.2f + k * 0.75f), new Vector3(0.12f, 1.2f, 0.6f), M(new[] { "7E57C2", "FFC400", "1E88E5", "E53935", "43A047", "18181C" }[k], 0.2f));
            }
            var sign = Prim.Text(b, "ГАРДЕРОБ", new Vector3(0, 5.2f, 4.35f), 7, Hex("FFFFFF"));
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Npc(b.TransformPoint(new Vector3(-1.6f, 0.2f, -1.6f)), b.eulerAngles.y + 160, "hair_3", "eyes_6", "top_0", "color_6", "pet_2", "emote_7", "neck_4");
            // mannequins in the window
            var m1 = Npc(b.TransformPoint(new Vector3(2.4f, 0.2f, 2.6f)), b.eulerAngles.y + 200, "hat_4", "neck_5", "top_5", "color_3", "emote_5", "shoes_4");
            var m2 = Npc(b.TransformPoint(new Vector3(-2.4f, 0.2f, 2.6f)), b.eulerAngles.y + 160, "hat_6", "wings_0", "color_7", "tail_4", "emote_1", "weapon_2");
            Zones.Add(b.TransformPoint(new Vector3(0, 0, 3.2f)), 3.4f, "ГАРДЕРОБ", () => WardrobeUI.Open());
            TokenSpots.Add(c + new Vector3(-6.5f, 0.9f, 2.5f));
            Bush(c + new Vector3(-6.5f, 0, 2.5f), 1.6f);
            Bush(c + new Vector3(-6.2f, 0, -1.5f), 1.3f);
        }

        static void Portals()
        {
            foreach (var (x, n, col) in new[] { (-8f, 2, "B57CFF"), (8f, 3, "35E0FF") })
            {
                var c = new Vector3(x, 0, 34);
                var stone = M("9AA3AE", 0.2f);
                foreach (var sd in new[] { -1, 1 }) Box(c + new Vector3(sd * 3, 3, 0), new Vector3(1.2f, 6, 1.4f), stone);
                Box(c + new Vector3(0, 6.5f, 0), new Vector3(7.6f, 1, 1.6f), stone);
                var surf = Cy(root, c + new Vector3(0, 3, 0), new Vector3(4.8f, 0.02f, 5.8f), Add(col, 0.55f), new Vector3(90, 0, 0));
                surf.AddComponent<Spin>().axis = Vector3.up;
                var s2 = Cy(root, c + new Vector3(0, 3, 0.05f), new Vector3(3.4f, 0.02f, 4f), Add("FFFFFF", 0.25f), new Vector3(90, 0, 0));
                s2.AddComponent<Pulse>();
                var t = Prim.Text(root, $"МИР {n}", c + new Vector3(0, 6.5f, -0.85f), 8, Color.white);
                t.transform.rotation = Quaternion.identity;
                var t2 = Prim.Text(root, "СКОРО ОТКРОЕТСЯ", c + new Vector3(0, 0.6f, -1.2f), 4, Hex("FFE14D"));
                t2.transform.rotation = Quaternion.identity;
                int world = n;
                Zones.Add(c + new Vector3(0, 0, -1.2f), 2.6f, $"МИР {n}", () => { AudioFx.Play("error"); Hud.Toast($"Мир {world} ещё строится — скоро откроется!", UIKit.Violet); });
            }
            Sign(new Vector3(0, 0, 27), 0, "ПОРТАЛЫ В МИРЫ", "9B6BFF", 4.4f);
            Mushroom(new Vector3(14, 0, 31), 19.5f);
            TokenSpots.Add(new Vector3(8, 7.8f, 34));
        }

        // ---------------- jump course ----------------

        static readonly string[] candy = { "FF5252", "FF9A2E", "FFD23A", "6EDC3C", "3D8BFF", "9B6BFF", "FF5FA2" };

        static GameObject Plat(Vector3 top, Vector3 size, int col)
        {
            var go = Box(top - Vector3.up * size.y / 2, size, M(candy[col % candy.Length], 0.35f));
            B(go.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.1f, 0.8f), M("FFFFFF", 0.4f));
            return go;
        }

        static void Course()
        {
            Sign(new Vector3(2.5f, 0, -12), 180, "ПОЛОСА ПРЫЖКОВ", "6EDC3C", 4.4f);
            Plat(new Vector3(0, 1.2f, -18), new Vector3(2.6f, 0.6f, 2.6f), 0);
            Plat(new Vector3(3, 2.4f, -21), new Vector3(2.4f, 0.6f, 2.4f), 1);
            Plat(new Vector3(6, 3.6f, -24), new Vector3(2.4f, 0.6f, 2.4f), 2);
            Plat(new Vector3(4, 4.8f, -27.5f), new Vector3(2.4f, 0.6f, 2.4f), 3);
            Plat(new Vector3(0, 6f, -30.5f), new Vector3(4.5f, 0.8f, 4.5f), 4);
            foreach (var p in new[] { new Vector3(0, 0, -18), new Vector3(3, 0, -21), new Vector3(6, 0, -24), new Vector3(4, 0, -27.5f), new Vector3(0, 0, -30.5f) })
                Solid(PrimitiveType.Cylinder, p + Vector3.up * (p.z < -30 ? 2.8f : 0.5f), new Vector3(0.3f, p.z < -30 ? 2.8f : 0.5f, 0.3f), M("B0BAC6", 0.5f, 0.4f));
            // moving platform to the tower
            var mv = Plat(new Vector3(0, 6, -34.5f), new Vector3(2.6f, 0.5f, 2.6f), 5);
            var m = mv.AddComponent<Mover>(); m.a = mv.transform.position; m.b = mv.transform.position + new Vector3(0, 0, -6.5f); m.period = 6;
            // tower: base, column, spiral, top
            var tc = new Vector3(0, 0, -45);
            Disc(tc + Vector3.up * 6, 3, 0.8f, M("FF9A2E", 0.35f));
            Solid(PrimitiveType.Cylinder, tc + Vector3.up * 2.8f, new Vector3(1.6f, 2.8f, 1.6f), M("E8DCC8", 0.3f));
            Solid(PrimitiveType.Cylinder, tc + Vector3.up * 10.5f, new Vector3(1.2f, 4.5f, 1.2f), M("E8DCC8", 0.3f));
            for (int k = 0; k < 7; k++)
            {
                float a = k * 60 * Mathf.Deg2Rad;
                Plat(tc + new Vector3(Mathf.Cos(a) * 2.6f, 7.3f + k * 1.15f, Mathf.Sin(a) * 2.3f), new Vector3(1.8f, 0.4f, 1.8f), k);
            }
            Disc(tc + Vector3.up * 15.4f, 1.5f, 0.5f, M("FFC83A", 0.6f, 0.4f));
            var flag = Node("Flag", root, tc + Vector3.up * 15.4f);
            Rod(flag, new Vector3(1.2f, 0, 0), new Vector3(1.2f, 3, 0), 0.08f, M("FFFFFF"));
            B(flag, new Vector3(1.9f, 2.6f, 0), new Vector3(1.3f, 0.8f, 0.04f), M("FF5252", 0.3f)).AddComponent<Pulse>().amount = 0.03f;
            TokenSpots.Add(tc + Vector3.up * 16.6f);
            Pad(tc + new Vector3(-1.8f, 6.05f, 1.6f), Spawn + new Vector3(3, 0, 0), 0, "6EDC3C");
            Pad(new Vector3(-5, 0.07f, -7), new Vector3(0, 6.3f, -30), 180, "35E0FF");

            // spinning disc with coins
            var spin = Disc(new Vector3(9, 4.2f, -32), 2.6f, 0.5f, M("9B6BFF", 0.4f));
            spin.AddComponent<Mover>().spin = 35;
            Solid(PrimitiveType.Cylinder, new Vector3(9, 2, -32), new Vector3(0.4f, 2f, 0.4f), M("B0BAC6", 0.5f, 0.4f));
            Mushroom(new Vector3(9, 0, -27.5f), 15f);

            // bounce mushrooms to floating islands
            Mushroom(new Vector3(-6, 0, -18), 18.5f);
            Island(new Vector3(-10, 7, -23), 3.2f);
            TokenSpots.Add(new Vector3(-10, 8.1f, -23));
            Mushroom(new Vector3(-14, 0, -28), 20.5f);
            Island(new Vector3(-17, 9.5f, -34), 3.6f);
        }

        static void Pad(Vector3 pos, Vector3 to, float yaw, string col)
        {
            Disc(pos + Vector3.up * 0.05f, 0.9f, 0.12f, M("2E3440", 0.6f, 0.4f));
            var ring = Ring(root, pos + Vector3.up * 0.1f, 0.75f, 0.07f, G(col, 2.2f));
            ring.AddComponent<Pulse>();
            var glow = Cy(root, pos + Vector3.up * 0.9f, new Vector3(1.4f, 0.8f, 1.4f), Add(col, 0.18f));
            glow.AddComponent<Pulse>().amount = 0.05f;
            var t = new GameObject("Teleporter").AddComponent<Teleporter>();
            t.transform.position = pos; t.target = to; t.faceYaw = yaw;
        }

        static void Mushroom(Vector3 p, float power)
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

        // ---------------- training, music, houses ----------------

        static void Training()
        {
            var crate = Tiled(Tex.Planks("C08040"), 1);
            var rng = new Rng(4);
            for (int k = 0; k < 7; k++)
            {
                var p = new Vector3(11 + (k % 4) * 1.8f, 0.55f + (k / 4) * 1.1f, -12 - (k % 2) * 1.8f);
                var b = Box(p, Vector3.one * 1.1f, crate, rng.F(-10, 10));
                B(b.transform, Vector3.zero, new Vector3(1.04f, 0.15f, 1.04f), M("6D4425"));
                b.AddComponent<Breakable>().coins = 3;
            }
            for (int k = 0; k < 3; k++)
            {
                var d = Node("Dummy", root, new Vector3(17 + k * 2.4f, 0, -16));
                Solid(PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(0.2f, 0.6f, 0.2f), M("8D5524"), default, d);
                var body = Solid(PrimitiveType.Sphere, new Vector3(0, 1.5f, 0), new Vector3(0.9f, 1.1f, 0.9f), M("F2D7A0", 0.1f), default, d);
                S(d, new Vector3(0, 2.25f, 0), 0.6f, M("F2D7A0", 0.1f));
                Cy(d, new Vector3(0, 1.6f, 0.45f), new Vector3(0.5f, 0.01f, 0.5f), Mats.UnlitTex(Tex.Target(), Color.white), new Vector3(90, 0, 0));
                d.gameObject.AddComponent<Breakable>().dummy = true;
                d.GetComponent<Breakable>().coins = 1;
            }
            Sign(new Vector3(14, 0, -8.5f), 180, "ЯЩИКИ И МИШЕНИ: БЕЙ (F / УДАР)", "FF5252", 5.4f);

            // training cactus (bumps you back and shows the damage effect, no hearts lost here)
            var cac = Node("Cactus", root, new Vector3(-11, 0, -10));
            var green = M("43A047", 0.3f);
            Solid(PrimitiveType.Capsule, new Vector3(0, 1.3f, 0), new Vector3(1f, 1.3f, 1f), green, default, cac);
            foreach (var sd in new[] { -1, 1 })
            {
                Ca(cac, new Vector3(sd * 0.7f, 1.4f, 0), new Vector3(0.45f, 0.3f, 0.45f), green, new Vector3(0, 0, 90));
                Ca(cac, new Vector3(sd * 0.95f, 1.85f, 0), new Vector3(0.42f, 0.4f, 0.42f), green);
            }
            for (int k = 0; k < 16; k++) { float a = k * 1.3f; Spike(cac, new Vector3(Mathf.Cos(a) * 0.48f, 0.5f + k * 0.13f, Mathf.Sin(a) * 0.48f), new Vector3(Mathf.Cos(a) * 0.68f, 0.5f + k * 0.13f, Mathf.Sin(a) * 0.68f), 0.06f, M("FFF8E1")); }
            S(cac, new Vector3(0, 2.75f, 0), 0.4f, M("FF5FA2", 0.3f));
            foreach (var sd in new[] { -1, 1 }) { S(cac, new Vector3(sd * 0.18f, 1.9f, 0.45f), 0.18f, M("FFFFFF")); S(cac, new Vector3(sd * 0.18f, 1.9f, 0.52f), 0.09f, M("111111", 0.9f)); }
            cac.gameObject.AddComponent<Hurter>();
            Sign(new Vector3(-11, 0, -6.5f), 180, "КАКТУС-ТРЕНЕР: НЕ БОЛЬНО", "43A047", 4.6f);
        }

        static void Music(Vector3 c)
        {
            Sign(c + new Vector3(0, 0, 3), 180, "МУЗЫКАЛЬНАЯ ДОРОЖКА", "9B6BFF", 4.8f);
            string[] cols = { "FF5252", "FF9A2E", "FFD23A", "6EDC3C", "35E0FF", "3D8BFF", "9B6BFF", "FF5FA2" };
            for (int k = 0; k < 8; k++)
            {
                var p = c + new Vector3(-5.25f + k * 1.5f, 0, 0);
                var holder = Node("Tile", root, p);
                var col = Hex(cols[k]);
                var m = Mats.Emissive(null, col, Color.black);
                var tile = Prim.Make(PrimitiveType.Cube, holder, new Vector3(0, 0.1f, 0), new Vector3(1.35f, 0.2f, 3f), m, "Tile", Layers.World);
                tile.AddComponent<BoxCollider>();
                var nt = tile.AddComponent<NoteTile>();
                nt.note = k; nt.rend = tile.GetComponent<Renderer>(); nt.baseColor = col;
            }
            // rock steps to a hidden token behind the path
            var rock = M("8F949B", 0.2f);
            Solid(PrimitiveType.Sphere, c + new Vector3(-8.5f, 0.6f, -2.5f), new Vector3(2.2f, 1.6f, 2.2f), rock);
            Solid(PrimitiveType.Sphere, c + new Vector3(-10f, 1.6f, -4.2f), new Vector3(2f, 3.4f, 2f), rock);
            TokenSpots.Add(c + new Vector3(-10f, 4.2f, -4.2f));
        }

        static void House(Vector3 c)
        {
            var wall = M("FFF3D6", 0.25f);
            Solid(PrimitiveType.Cylinder, c + Vector3.up * 2.2f, new Vector3(6, 2.2f, 6), wall);
            Cone(root, c + Vector3.up * 4.4f, new Vector3(7.4f, 3.6f, 7.4f), M("3D8BFF", 0.4f));
            B(root, c + new Vector3(0, 1.1f, -2.98f), new Vector3(1.2f, 2.2f, 0.1f), M("8D5524"));
            S(root, c + new Vector3(0.4f, 1.1f, -3.05f), 0.12f, M("FFC83A", 0.9f, 0.9f));
            foreach (var sd in new[] { -1, 1 })
            {
                var w = c + new Vector3(sd * 2f, 2.6f, -2.2f);
                Cy(root, w, new Vector3(0.9f, 0.05f, 0.9f), M("BFE6FF", 0.9f), new Vector3(90, sd * 40, 0));
            }
            Box(c + new Vector3(1.6f, 6.2f, -0.8f), new Vector3(0.8f, 2.4f, 0.8f), M("C0504D", 0.2f));
            TokenSpots.Add(c + new Vector3(1.6f, 8.2f, -0.8f));
            Mushroom(c + new Vector3(5.5f, 0, 1.5f), 20f);
            Fence(c + new Vector3(-5, 0, 5), c + new Vector3(5, 0, 5));
            var t = Prim.Text(root, "ДОМИК ЧУБРИКА", c + new Vector3(0, 3.6f, -3.25f), 3.4f, Hex("3D8BFF"));
            t.transform.rotation = Quaternion.identity;
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

        static void Fence(Vector3 a, Vector3 b)
        {
            var wood = M("FFFFFF", 0.3f);
            int n = Mathf.CeilToInt(Vector3.Distance(a, b) / 1.2f);
            for (int i = 0; i <= n; i++) B(root, Vector3.Lerp(a, b, (float)i / n) + Vector3.up * 0.5f, new Vector3(0.12f, 1, 0.12f), wood);
            foreach (var h in new[] { 0.35f, 0.75f }) Rod(root, a + Vector3.up * h, b + Vector3.up * h, 0.07f, wood);
        }

        // ---------------- nature ----------------

        static void Tree(Vector3 p, float s, int seed)
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

        static void Pine(Vector3 p, float s)
        {
            Solid(PrimitiveType.Cylinder, p + Vector3.up * 0.8f * s, new Vector3(0.4f * s, 0.8f * s, 0.4f * s), M("6D4C41"));
            for (int k = 0; k < 3; k++) Cone(root, p + Vector3.up * (1.3f + k * 1.2f) * s, new Vector3((3 - k * 0.7f) * s, 2.2f * s, (3 - k * 0.7f) * s), M("2E7D32", 0.15f));
        }

        static void Bush(Vector3 p, float s)
        {
            for (int k = 0; k < 3; k++) S(root, p + new Vector3((k - 1) * 0.5f * s, 0.5f * s, (k % 2) * 0.3f * s), 1.2f * s, M("388E3C", 0.15f));
        }

        static void Flowers(Vector3 c, float r, int n, int seed)
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

        static void Nature()
        {
            var keep = new[] { new Vector3(0, 0, 0), new Vector3(22, 0, 0), new Vector3(16, 0, 17), new Vector3(-22, 0, 0), new Vector3(0, 0, 34), new Vector3(0, 0, -30), new Vector3(-24, 0, 24), new Vector3(26, 0, -26), new Vector3(-24, 0, -18), new Vector3(14, 0, -14), new Vector3(-12, 0, -26) };
            float[] keepR = { 12, 9, 7, 9, 12, 18, 8, 6, 9, 8, 12 };
            bool Free(Vector3 p)
            {
                for (int i = 0; i < keep.Length; i++) if (Vector3.Distance(p, keep[i]) < keepR[i]) return false;
                return true;
            }
            var r = new Rng(21);
            int trees = 0;
            for (int i = 0; i < 400 && trees < 46; i++)
            {
                float a = r.F(0, Mathf.PI * 2), d = r.F(20, 54);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (!Free(p)) continue;
                if (r.Chance(0.3f)) Pine(p, r.F(0.8f, 1.3f)); else Tree(p, r.F(0.8f, 1.3f), i);
                trees++;
            }
            for (int i = 0; i < 18; i++)
            {
                float a = r.F(0, Mathf.PI * 2), d = r.F(14, 56);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (!Free(p)) continue;
                Solid(PrimitiveType.Sphere, p + Vector3.up * 0.2f, new Vector3(r.F(1, 2.4f), r.F(0.8f, 1.6f), r.F(1, 2.2f)), M("9AA0A8", 0.2f));
            }
            for (int i = 0; i < 20; i++)
            {
                float a = r.F(0, Mathf.PI * 2), d = r.F(10, 54);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (Free(p)) Bush(p, r.F(0.7f, 1.2f));
            }
            Flowers(Vector3.zero, 55, 260, 5);
            // beach rocks, a token behind one
            Solid(PrimitiveType.Sphere, new Vector3(0, 0.3f, -60), new Vector3(3.2f, 2.6f, 2.4f), M("9AA0A8", 0.2f));
            TokenSpots.Add(new Vector3(0, 0.5f, -62.2f));
            // clouds drifting in the sky
            var white = M("FFFFFF", 0.05f);
            for (int i = 0; i < 12; i++)
            {
                var c = Node("Cloud", root, new Vector3(r.F(-120, 120), r.F(38, 60), r.F(-120, 120)));
                for (int k = 0; k < 5; k++) S(c, new Vector3((k - 2) * 2.4f, r.F(-0.5f, 1f), r.F(-1, 1)), r.F(3, 5.5f), white);
                var mv = c.gameObject.AddComponent<Mover>(); mv.a = c.position; mv.b = c.position + new Vector3(r.F(15, 30), 0, r.F(-5, 5)); mv.period = r.F(60, 120); mv.phase = r.F();
            }
        }

        // ---------------- coins & tokens ----------------

        static void Coins()
        {
            var p = Pickups.I;
            for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; p.Coin(new Vector3(Mathf.Cos(a) * 11.5f, 0.8f, Mathf.Sin(a) * 11.5f)); }
            for (int k = 0; k < 6; k++) { p.Coin(new Vector3(8 + k * 1.6f, 0.8f, 0)); p.Coin(new Vector3(-8 - k * 1.6f, 0.8f, 0)); p.Coin(new Vector3(0, 0.8f, 10 + k * 2.6f)); }
            foreach (var t in new[] { new Vector3(0, 1.2f, -18), new Vector3(3, 2.4f, -21), new Vector3(6, 3.6f, -24), new Vector3(4, 4.8f, -27.5f) })
                p.Coin(t + Vector3.up * 0.8f);
            for (int k = 0; k < 7; k++) { float a = k * 60 * Mathf.Deg2Rad; p.Coin(new Vector3(Mathf.Cos(a) * 2.6f, 8.1f + k * 1.15f, -45 + Mathf.Sin(a) * 2.3f)); }
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; p.Coin(new Vector3(9 + Mathf.Cos(a) * 1.8f, 5f, -32 + Mathf.Sin(a) * 1.8f)); }
            for (int k = 0; k < 5; k++) p.Coin(new Vector3(-6, 3 + k * 1.2f, -18));     // over the first mushroom
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; p.Coin(new Vector3(-17 + Mathf.Cos(a) * 2.2f, 10.4f, -34 + Mathf.Sin(a) * 2.2f), 10); }
            for (int k = 0; k < 10; k++) p.Coin(new Vector3(-18 + k * 4, 0.5f, -61));      // beach
            for (int k = 0; k < 8; k++) p.Coin(new Vector3(-24 + k * 1.5f - 5.25f, 0.9f, -18));
            for (int k = 0; k < 6; k++) p.Coin(new Vector3(14, 3 + k * 1.3f, 31));        // over the portal mushroom
            for (int k = 0; k < 5; k++) p.Coin(new Vector3(5 + k * 1.8f, 0.8f, 5 + k * 1.8f));
            p.Coin(new Vector3(0, 6.9f, -30.5f), 10);
            p.Coin(new Vector3(0, 2.6f, 6), 10);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; p.Coin(new Vector3(-24 + Mathf.Cos(a) * 6.5f, 0.8f, 24 + Mathf.Sin(a) * 6.5f)); }
            for (int k = 0; k < 14; k++) { float a = k * Mathf.PI / 7; p.Coin(new Vector3(16 + Mathf.Cos(a) * 6f, 0.8f, 17 + Mathf.Sin(a) * 6f)); }     // around the premium kiosk
            for (int k = 0; k < 8; k++) p.Coin(new Vector3(-10 - k * 1.2f, 0.8f, 0.0f + Mathf.Sin(k) * 0.5f + 8));
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; p.Coin(new Vector3(26 + Mathf.Cos(a) * 1.4f, 5.4f, -26 + Mathf.Sin(a) * 1.4f)); }   // treehouse
            for (int k = 0; k < 6; k++) p.Coin(new Vector3(-30 + k * 2.5f, 0.8f, -6));
        }

        static void Tokens()
        {
            TokenSpots.Add(new Vector3(0, 3.6f, 6));   // above the fountain bowl
            for (int i = 0; i < TokenSpots.Count; i++) Pickups.I.Token("t" + i, TokenSpots[i]);
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
}
