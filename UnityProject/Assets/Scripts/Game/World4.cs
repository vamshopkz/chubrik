using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Marks a slippery surface (frozen lake, ice bridges): the chubrik slides unless he wears skates.</summary>
    public class Ice : MonoBehaviour { }

    /// <summary>
    /// World 4 — «Снежная Долина»: an old cable-car station (three stomps on the chimney → a fountain of tokens),
    /// slippery ice bridges and a frozen lake over icy water, snowballs that grow when rolled, cracked ice walls,
    /// falling icicles in the ice cave, warm steam geysers, a snowboard run with coin rings, a village with a frozen
    /// mailbox (the developer code brings a jet snowmobile), 8 friends frozen in ice cubes, angry snowmen and ice
    /// boars, a secret cave behind the frozen waterfall with the Frosty lucky box, and King Yeti on the Ice Castle.
    /// </summary>
    public static class World4
    {
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 3000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 3f);
        public static float WaterY => O.y - 1.6f;
        public static readonly List<Snowman> Snowmen = new List<Snowman>();
        public static readonly List<IceBoar> Boars = new List<IceBoar>();
        public static readonly List<Snowball> Balls = new List<Snowball>();
        public static readonly List<CrackedWall> Walls = new List<CrackedWall>();
        public static Yeti Boss;
        public static Snowboard Board;
        public static Chimney Chim;
        public static Vector3 GoalPos, ArenaC, CaveC, MailboxPos, BoardTop;
        static Material snow, snowDark, ice, wood, woodDark, roof, rock, water;

        public static void Build()
        {
            Root = new GameObject("World4").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            Snowmen.Clear(); Boars.Clear(); Balls.Clear(); Walls.Clear();
            snow = Lobby.Tiled(Tex.Snow(), 6); snowDark = M("D6E2EE", 0.2f);
            ice = Glass("A8DCFF", 0.82f); ice.SetFloat("_Smoothness", 0.95f);
            wood = Lobby.Tiled(Tex.Planks("8A5A36"), 1); woodDark = M("5A3A22", 0.3f); roof = M("B33A3A", 0.3f);
            rock = M("8A93A0", 0.2f);

            Water();
            // ---- start: the old cable-car station ----
            Field(V(0, 0, 0), 13);
            Portal(V(-6, 0, -6));
            Lobby.Sign(V(-4.5f, 0, 5.5f), 0, "МИР 4 · СНЕЖНАЯ ДОЛИНА", "35E0FF", 5.4f);
            Lobby.Sign(V(4.5f, 0, 5.5f), 0, "СПАСИ 8 ДРУЗЕЙ ИЗО ЛЬДА!", "4CE35A", 5.4f);
            Station(V(5, 0, -4));
            Pines(V(0, 0, 0), 11, 8, 3);
            for (int k = 0; k < 5; k++) Pickups.I.Coin(V(-3 + k * 1.5f, 0.8f, 8));
            Rescue.SpawnCage("w4c0", V(-8.5f, 0, 3), 60, Root, true);
            Snowman.Spawn(V(8, 0, 7), Root, "w4t0");

            // ---- ice bridge over the icy water → the frozen lake ----
            IceBridge(V(0, 0, 12.5f), V(0, 0, 24));
            IceDisc(V(0, 0, 38), 14);
            for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Pickups.I.Coin(V(Mathf.Cos(a) * 7, 0.8f, 38 + Mathf.Sin(a) * 7)); }
            IceBoar.Spawn(V(-5, 0, 36), Root); IceBoar.Spawn(V(6, 0, 42), Root);
            Rescue.SpawnCage("w4c1", V(8, 0, 32), -60, Root, true);
            // ice slides with tokens frozen into them
            IceSlide(V(-9, 0, 44), 30, "w4t1");

            // ---- the snowball meadow: roll a snowball, break the cracked wall ----
            Field(V(0, 0, 63), 11);
            Pines(V(0, 0, 63), 9.5f, 6, 4);
            Checkpoint(V(-3, 0, 54.5f));
            Snowman.Spawn(V(6, 0, 66), Root, null); Snowman.Spawn(V(-7, 0, 68), Root, "w4t2");
            Snowball.Spawn(V(-3, 0, 60), Root); Snowball.Spawn(V(3, 0, 58.5f), Root);
            Rescue.SpawnCage("w4c2", V(-8, 0, 59), 90, Root, true);
            // a high ledge with a token on a tall pine: roll a big snowball under it to use as a step
            Lobby.Box(V(7.5f, 1.4f, 60), new Vector3(3, 2.8f, 3), rock);
            Lobby.Pine(V(7.5f, 2.8f, 60), 1.4f);
            Pickups.I.Token("w4t3", V(7.5f, 4.2f, 61.4f));
            // the cracked ice wall closes the short way to the frozen waterfall
            Walls.Add(CrackedWall.Spawn(V(0, 0, 73.5f), 0, 6f, Root, "Wall"));

            // ---- the ice cave: falling icicles, a steam geyser up to the slope ----
            Field(V(0, 0, 84), 9);
            IceCave(V(0, 0, 84));
            Rescue.SpawnCage("w4c3", V(5.5f, 0, 88), -120, Root, true);
            Checkpoint(V(-5, 0, 80));
            Geyser(V(-3, 0, 92), 26f);                                       // up to the snowboard rock
            Geyser(V(7.5f, 0, 80), 20f);
            Lobby.Box(V(8, 5.2f, 74), new Vector3(3, 0.6f, 3), rock);       // a snowy rock the second geyser reaches
            Pickups.I.Token("w4t4", V(8, 6.2f, 74));

            // ---- the mountain slope with the snowboard (the first geyser lifts you up there) ----
            BoardTop = V(-4, 9.5f, 98);
            Lobby.Box(BoardTop - Vector3.up * 0.5f, new Vector3(6, 1, 6), snow);
            Lobby.Box(BoardTop - Vector3.up * 5, new Vector3(5, 9, 5), rock);
            Board = new GameObject("Snowboard").AddComponent<Snowboard>();
            Board.Build(BoardTop + new Vector3(0, 0.05f, 1.2f), Root, SnowPath());
            Rescue.SpawnCage("w4c4", BoardTop + new Vector3(-1.8f, 0, -1.6f), 30, Root, true);

            // ---- the village at the bottom of the run, the frozen mailbox ----
            Field(V(18, 0, 140), 14);
            Pines(V(18, 0, 140), 12.5f, 10, 7);
            Hut(V(12, 0, 146), 20); Hut(V(24, 0, 147), -15); Hut(V(28, 0, 136), -70);
            MailboxPos = V(14.5f, 0, 142);
            Mailbox(MailboxPos);
            Snowman.Spawn(V(20, 0, 133), Root, null); Snowman.Spawn(V(10, 0, 136), Root, null);
            IceBoar.Spawn(V(22, 0, 141), Root);
            Rescue.SpawnCage("w4c5", V(27, 0, 143), -90, Root, true);
            Checkpoint(V(16, 0, 131));

            // ---- the frozen waterfall with a crack; behind it the secret cave with the Frosty lucky box ----
            Waterfall(V(4, 0, 152));
            CaveC = V(4, 0, 161);
            SecretCave(CaveC);
            Walls.Add(CrackedWall.Spawn(V(4, 0, 155.2f), 0, 4.2f, Root, "WaterfallCrack"));
            Snowball.Spawn(V(8, 0, 148), Root);
            Pickups.I.Token("w4t5", V(4, 0.9f, 162.5f));
            Rescue.SpawnCage("w4c6", CaveC + new Vector3(-3, 0, 1), 70, Root, true);

            // ---- the road up to the Ice Castle and King Yeti on top ----
            IceBridge(V(18, 0, 154.2f), V(18, 0, 166));
            Field(V(18, 0, 172), 7);
            Rescue.SpawnCage("w4c7", V(21.5f, 0, 174), -110, Root, true);
            Ramp(V(18, 0, 176), V(18, 6, 186), 5);
            ArenaC = V(18, 6, 196);
            Castle(ArenaC);
            for (int k = 0; k < 3; k++) Snowball.Spawn(ArenaC + new Vector3(-6 + k * 6, 0, -6), Root, true);
            GoalPos = ArenaC + Vector3.up * 1.2f;
            Boss = new GameObject("Yeti").AddComponent<Yeti>();
            Boss.Build(ArenaC + new Vector3(0, 0, 4), Root);

            Mountains();
            new GameObject("SnowFall").AddComponent<SnowFall>().transform.SetParent(Root, false);
            Lobby.root = keep;
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- pieces ----------------

        /// <summary>A round snowy field: thick snow cake with soft drifts on the rim.</summary>
        static void Field(Vector3 c, float r)
        {
            Lobby.Disc(c, r, 2.4f, snow);
            var rng = new Rng((int)(c.x * 7 + c.z * 3));
            for (int k = 0; k < (int)(r * 1.6f); k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - rng.F(0.3f, 1.4f));
                Ls(Root, p, new Vector3(rng.F(1.2f, 2.6f), rng.F(0.4f, 0.9f), rng.F(1.2f, 2.6f)), snowDark);
            }
        }

        static void Water()
        {
            water = Mats.Tex(Tex.Water(), new Color(0.65f, 0.85f, 1f), 0.95f);
            water.mainTextureScale = new Vector2(30, 30);
            var w = Prim.Make(PrimitiveType.Plane, Root, new Vector3(O.x, WaterY, O.z + 100), new Vector3(30, 1, 30), water, "IcyWater", Layers.World);
            w.AddComponent<WaterScroll>();
            // ice floes drifting on the water
            var r = new Rng(404);
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector3(O.x + r.F(-60, 60), WaterY + 0.05f, O.z + r.F(-20, 220));
                Cy(Root, p, new Vector3(r.F(1.5f, 4), 0.06f, r.F(1.5f, 4)), M("F2FAFF", 0.7f));
            }
        }

        static void IceDisc(Vector3 c, float r)
        {
            var go = Lobby.Disc(c, r, 0.6f, ice);
            go.AddComponent<Ice>();
            Cy(Root, c + Vector3.up * 0.01f, new Vector3(r * 2 - 1, 0.01f, r * 2 - 1), M("E8F6FF", 0.9f));
            var rng = new Rng(77);
            for (int k = 0; k < 18; k++)
            {
                float a = rng.F(0, Mathf.PI * 2), d = rng.F(1, r - 1);
                var cr = B(Root, c + new Vector3(Mathf.Cos(a) * d, 0.025f, Mathf.Sin(a) * d), new Vector3(0.05f, 0.01f, rng.F(0.8f, 2.4f)), M("FFFFFF", 0.9f));
                cr.transform.rotation = Quaternion.Euler(0, rng.F(0, 180), 0);
            }
        }

        /// <summary>Slippery ice bridge without rails (careful in the turns!).</summary>
        static void IceBridge(Vector3 a, Vector3 b, float w = 3.6f)
        {
            var d = b - a;
            var go = Lobby.Box((a + b) / 2 - Vector3.up * 0.3f, new Vector3(w, 0.6f, d.magnitude + 1), ice, Quaternion.LookRotation(d.normalized).eulerAngles.y);
            go.AddComponent<Ice>();
            int n = Mathf.CeilToInt(d.magnitude / 2f);
            for (int i = 0; i <= n; i++)
                foreach (var sd in new[] { -1, 1 })
                {
                    var p = Vector3.Lerp(a, b, i / (float)n) + Quaternion.LookRotation(d.normalized) * new Vector3(sd * (w / 2 + 0.1f), -0.4f, 0);
                    Spike(Root, p, p + Vector3.down * 1.4f, 0.25f, M("CFEFFF", 0.8f));
                }
        }

        static void IceSlide(Vector3 c, float yaw, string token)
        {
            var t = Node("IceSlide", Root, c, new Vector3(0, yaw, 0));
            var ramp = Lobby.Solid(PrimitiveType.Cube, Vector3.zero, new Vector3(2.4f, 0.4f, 7), ice, new Vector3(-22, 0, 0), t);
            ramp.transform.localPosition = new Vector3(0, 1.3f, 0);
            ramp.AddComponent<Ice>();
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 1.4f, 3.6f), new Vector3(2.4f, 2.8f, 0.8f), snow, default, t);
            Pickups.I.Token(token, t.TransformPoint(new Vector3(0, 2.3f, 0)));
        }

        static void Pines(Vector3 c, float r, int n, int seed)
        {
            var rng = new Rng(seed * 13);
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2 / n + rng.F(-0.2f, 0.2f);
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - 0.8f);
                Lobby.Pine(p, rng.F(0.9f, 1.4f));
                S(Root, p + Vector3.up * rng.F(2.6f, 3.4f), new Vector3(1.4f, 0.4f, 1.4f), M("FFFFFF", 0.1f));   // snow on the branches
            }
        }

        static void Station(Vector3 c)
        {
            Lobby.Box(c + Vector3.up * 1.6f, new Vector3(5, 3.2f, 4), wood, 0);
            var r = Lobby.Box(c + Vector3.up * 3.4f, new Vector3(5.6f, 0.4f, 4.6f), roof, 0);
            S(Root, c + Vector3.up * 3.7f, new Vector3(5.4f, 0.5f, 4.4f), M("FFFFFF", 0.1f));
            B(Root, c + new Vector3(0, 1.2f, 2.02f), new Vector3(1.2f, 2.2f, 0.06f), woodDark);
            foreach (var sd in new[] { -1, 1 }) B(Root, c + new Vector3(sd * 1.6f, 1.9f, 2.02f), new Vector3(0.9f, 0.8f, 0.05f), Glass("BFE6FF", 0.7f));
            Lobby.Sign(c + new Vector3(0, 0, 2.6f), 0, "КАНАТНАЯ ДОРОГА", "8A5A36", 3.6f);
            // steps up to the roof: crates
            Lobby.Box(c + new Vector3(-3.4f, 0.6f, 1), new Vector3(1.2f, 1.2f, 1.2f), wood, 10);
            Lobby.Box(c + new Vector3(-3.3f, 1.8f, -0.4f), new Vector3(1.2f, 1.2f, 1.2f), wood, -8);
            Lobby.Box(c + new Vector3(-3.2f, 1.8f, -0.4f) + Vector3.up * 0.6f, new Vector3(0.8f, 0.01f, 0.8f), snow, 0);
            Chim = new GameObject("Chimney").AddComponent<Chimney>();
            Chim.Build(c + new Vector3(1.5f, 3.6f, -1), Root);
            // the cable-car pylon and a stuck gondola
            var pyl = c + new Vector3(-1, 0, -6);
            Lobby.Box(pyl + Vector3.up * 4, new Vector3(0.6f, 8, 0.6f), M("6E7884", 0.6f, 0.6f));
            B(Root, pyl + Vector3.up * 8, new Vector3(3, 0.3f, 0.3f), M("6E7884", 0.6f, 0.6f));
            Rod(Root, pyl + new Vector3(1.4f, 8, 0), pyl + new Vector3(1.4f, 14, 40), 0.06f, M("2A2A30"));
            var g = pyl + new Vector3(1.4f, 6.4f, 4);
            B(Root, g, new Vector3(1.6f, 1.4f, 1.6f), M("E53935", 0.5f));
            B(Root, g + Vector3.up * 0.2f, new Vector3(1.62f, 0.6f, 1.3f), Glass("BFE6FF", 0.6f));
            Rod(Root, g + Vector3.up * 0.7f, g + Vector3.up * 1.9f, 0.06f, M("2A2A30"));
        }

        static void Hut(Vector3 c, float yaw)
        {
            var t = Node("Hut", Root, c, new Vector3(0, yaw, 0));
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 1.4f, 0), new Vector3(4, 2.8f, 3.4f), Lobby.Tiled(Tex.Logs(), 1), default, t);
            foreach (var sd in new[] { -1, 1 })
                Lobby.Solid(PrimitiveType.Cube, new Vector3(sd * 1.05f, 3.4f, 0), new Vector3(2.6f, 0.25f, 4f), roof, new Vector3(0, 0, -sd * 38), t);
            S(t, new Vector3(0, 4.1f, 0), new Vector3(1.4f, 0.4f, 4f), M("FFFFFF", 0.1f));
            B(t, new Vector3(0, 1.1f, 1.72f), new Vector3(0.9f, 2f, 0.06f), woodDark);
            B(t, new Vector3(1.2f, 1.7f, 1.72f), new Vector3(0.8f, 0.7f, 0.05f), G("FFD27A", 1.2f));   // warm window
            Lobby.Solid(PrimitiveType.Cube, new Vector3(-1.2f, 3.9f, -0.6f), new Vector3(0.5f, 1.2f, 0.5f), M("8A4A3A", 0.2f), default, t);
        }

        static void Mailbox(Vector3 p)
        {
            Lobby.Box(p + Vector3.up * 0.6f, new Vector3(0.15f, 1.2f, 0.15f), woodDark);
            var box = Lobby.Box(p + Vector3.up * 1.35f, new Vector3(0.6f, 0.45f, 0.8f), M("2E5BD9", 0.4f, 0.3f));
            S(box.transform, new Vector3(0, 0.6f, 0), new Vector3(1.1f, 0.4f, 1.1f), M("FFFFFF", 0.1f));
            for (int k = 0; k < 5; k++) Spike(Root, p + new Vector3(-0.25f + k * 0.12f, 1.12f, 0.41f), p + new Vector3(-0.25f + k * 0.12f, 0.85f, 0.42f), 0.05f, M("CFEFFF", 0.8f));
            var t = Prim.Text(Root, "ПОЧТА", p + new Vector3(0, 2.1f, 0), 2.6f, Color.white);
            t.gameObject.AddComponent<Billboard>();
            Zones.Add(p, 2.4f, "ЗАМЁРЗШИЙ ПОЧТОВЫЙ ЯЩИК", MailWindow, () => Worlds.Current == 4);
        }

        static void MailWindow()
        {
            Menus.Unlock();
            Window.Show("ЗАМЁРЗШИЙ ПОЧТОВЫЙ ЯЩИК", w =>
            {
                w.Text("В ящике записка: «Знаешь секретный код? Шепни его — и из леса прилетит подарок!»", 28);
                var holder = UIKit.Rect("CodeRow", w.Content);
                holder.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 96;
                var input = UIKit.Input(holder, "Секретный код", 32, 34);
                UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(600, 86));
                var go = UIKit.Button(holder, "ШЕПНУТЬ", UIKit.Green, () =>
                {
                    if (Promo.Norm(input.text) != Promo.Norm(Promo.DevCode)) { AudioFx.Play("error"); Hud.Toast("Ничего не случилось… Это не тот код", UIKit.Red); return; }
                    Window.Close();
                    CallSnowmobile();
                }, 30);
                UIKit.Place(go.Bg.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(300, 86));
            }, 1000, 480);
        }

        static GameObject sled;

        /// <summary>The jet snowmobile: rides up steep slopes and knocks snowmen over (until you leave World 4).</summary>
        public static void CallSnowmobile()
        {
            var p = PlayerCtl.I;
            if (p == null) return;
            p.Snowmobile = true;
            if (sled) Object.Destroy(sled);
            Parts.Layer = Layers.Player;
            sled = new GameObject("Snowmobile");
            sled.transform.SetParent(p.transform, false);
            var red = M("E53935", 0.6f, 0.4f); var black = M("1E1E22", 0.4f); var steel = M("C9D1DB", 0.85f, 0.8f);
            B(sled.transform, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.35f, 1.8f), red);
            B(sled.transform, new Vector3(0, 0.45f, -0.3f), new Vector3(0.7f, 0.2f, 0.9f), black);
            B(sled.transform, new Vector3(0, 0.6f, 0.65f), new Vector3(0.7f, 0.12f, 0.12f), steel);
            B(sled.transform, new Vector3(0, 0.62f, 0.75f), new Vector3(0.6f, 0.4f, 0.05f), Glass("BFE6FF", 0.5f));
            foreach (var sd in new[] { -1, 1 }) B(sled.transform, new Vector3(sd * 0.45f, 0.04f, 0.1f), new Vector3(0.1f, 0.06f, 2.1f), steel);
            foreach (var sd in new[] { -1, 1 })
            {
                Cy(sled.transform, new Vector3(sd * 0.3f, 0.35f, -1.0f), new Vector3(0.22f, 0.15f, 0.22f), steel, new Vector3(90, 0, 0));
                var fl = Cone(sled.transform, new Vector3(sd * 0.3f, 0.35f, -1.1f), new Vector3(0.2f, 0.5f, 0.2f), Add("FF8A1E", 0.8f), new Vector3(-90, 0, 0));
                fl.AddComponent<Pulse>();
            }
            AudioFx.Play("portal"); AudioFx.Play("jump5");
            Effects.Emit("puff", p.transform.position, 40, 1.2f);
            Hud.Toast("РЕАКТИВНЫЙ СНЕГОХОД! Ездит даже по горам и сбивает снеговиков", UIKit.Gold);
        }

        public static void DropSnowmobile()
        {
            if (PlayerCtl.I) PlayerCtl.I.Snowmobile = false;
            if (sled) Object.Destroy(sled);
        }

        static void IceCave(Vector3 c)
        {
            // an arch of ice with icicles hanging from it
            foreach (var sd in new[] { -1, 1 })
                Lobby.Box(c + new Vector3(sd * 5.4f, 2.6f, 0), new Vector3(1.4f, 5.2f, 12), Glass("9FD8FF", 0.75f));
            Lobby.Box(c + new Vector3(0, 5.6f, 0), new Vector3(12, 1, 12), Glass("9FD8FF", 0.75f));
            S(Root, c + new Vector3(0, 6.3f, 0), new Vector3(12, 1.4f, 12.5f), M("FFFFFF", 0.1f));
            for (int k = 0; k < 7; k++)
            {
                var p = c + new Vector3(((k * 37) % 7 - 3) * 1.2f, 5.1f, -4.5f + k * 1.5f);
                new GameObject("Icicle").AddComponent<Icicle>().Build(p, Root);
            }
            for (int k = 0; k < 10; k++)
            {
                float a = k * 0.63f;
                Spike(Root, c + new Vector3(Mathf.Sin(a) * 4.6f, 0, -5 + k), c + new Vector3(Mathf.Sin(a) * 4.8f, 1 + (k % 3) * 0.5f, -5 + k), 0.4f, G("8FD0FF", 0.9f));
            }
        }

        static void Geyser(Vector3 p, float power)
        {
            var go = new GameObject("SteamGeyser");
            go.transform.SetParent(Root, false);
            go.transform.position = p;
            Parts.Layer = Layers.World;
            var top = Node("Top", go.transform, Vector3.zero);
            var pad = Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * 0.06f, new Vector3(1.6f, 0.06f, 1.6f), M("6E7884", 0.3f));
            pad.transform.SetParent(go.transform, true);
            Cy(go.transform, new Vector3(0, 0.13f, 0), new Vector3(1f, 0.02f, 1f), G("BFEFFF", 0.8f));
            for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Ls(go.transform, new Vector3(Mathf.Cos(a) * 0.9f, 0.15f, Mathf.Sin(a) * 0.9f), new Vector3(0.4f, 0.25f, 0.4f), rock); }
            var bp = pad.AddComponent<BouncePad>();
            bp.power = power;
            go.AddComponent<SteamPuffs>();
        }

        static void Waterfall(Vector3 c)
        {
            Lobby.Box(c + new Vector3(0, 6, 4), new Vector3(16, 12, 3), rock);
            for (int k = 0; k < 9; k++) Lobby.Solid(PrimitiveType.Capsule, c + new Vector3(-4 + k, 4.5f, 2.3f), new Vector3(0.9f, 4.5f, 0.6f), Glass("BFEAFF", 0.8f));
            for (int k = 0; k < 12; k++) Spike(Root, c + new Vector3(-5 + k * 0.9f, 9.2f, 2.2f), c + new Vector3(-5 + k * 0.9f, 7.2f - (k % 3) * 0.6f, 2.2f), 0.4f, M("E8F6FF", 0.8f));
            Lobby.Sign(c + new Vector3(-5.5f, 0, -1), 0, "ЗАМЁРЗШИЙ ВОДОПАД", "35E0FF", 4.4f);
        }

        static void SecretCave(Vector3 c)
        {
            Lobby.Disc(c, 6, 1, M("7A6A5A", 0.2f));
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 6, 3, 0), new Vector3(1, 6, 12), rock);
            Lobby.Box(c + new Vector3(0, 6.2f, 0), new Vector3(13, 0.6f, 12), rock);
            Lobby.Box(c + new Vector3(0, 3, 6), new Vector3(13, 6, 1), rock);
            // the hot spring
            Cy(Root, c + new Vector3(2.5f, 0.05f, 2), new Vector3(3.4f, 0.05f, 3.4f), Mats.Emissive(null, Hex("6FD8E8"), Hex("2A8899") * 0.6f));
            for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; Ls(Root, c + new Vector3(2.5f + Mathf.Cos(a) * 1.8f, 0.15f, 2 + Mathf.Sin(a) * 1.8f), new Vector3(0.5f, 0.3f, 0.5f), rock); }
            new GameObject("Steam").AddComponent<SteamPuffs>().transform.position = c + new Vector3(2.5f, 0.2f, 2);
            for (int k = 0; k < 6; k++) Spike(Root, c + new Vector3(-4 + k * 1.6f, 0, 4.5f), c + new Vector3(-4 + k * 1.6f, 1.5f, 4.6f), 0.5f, G("8FD0FF", 1.1f));
            var box = new GameObject("FrostBox").AddComponent<FrostBox>();
            box.Build(c + new Vector3(-1.5f, 0, 3.5f), Root);
        }

        static void Ramp(Vector3 a, Vector3 b, float w)
        {
            var d = b - a;
            var go = Lobby.Box((a + b) / 2 - Vector3.up * 0.3f, new Vector3(w, 0.6f, d.magnitude + 0.5f), snow, 0);
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
        }

        static void Castle(Vector3 c)
        {
            var iceWall = Glass("A8DCFF", 0.85f);
            Lobby.Disc(c, 11, 6.5f, snow);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4 + Mathf.PI / 8;
                var p = c + new Vector3(Mathf.Cos(a) * 10.6f, 0, Mathf.Sin(a) * 10.6f);
                if (Mathf.Abs(Mathf.Sin(a) + 1) < 0.5f) continue;   // the gate side (towards the ramp) stays open
                Lobby.Solid(PrimitiveType.Cylinder, p + Vector3.up * 3.5f, new Vector3(2.2f, 3.5f, 2.2f), iceWall);
                Cone(Root, p + Vector3.up * 7, new Vector3(2.8f, 3, 2.8f), M("4FA8E8", 0.6f));
            }
            Lobby.Sign(c + new Vector3(-4, -6, -24), 0, "ЛЕДЯНОЙ ЗАМОК КОРОЛЯ ЙЕТИ", "35E0FF", 6);   // at the foot of the road up
            // the castle tower far behind
            Lobby.Solid(PrimitiveType.Cylinder, c + new Vector3(0, 9, 16), new Vector3(6, 9, 6), iceWall);
            Cone(Root, c + new Vector3(0, 18, 16), new Vector3(8, 9, 8), M("4FA8E8", 0.6f));
        }

        static void Mountains()
        {
            var r = new Rng(909);
            for (int i = 0; i < 22; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var b = new Vector3(O.x + side * r.F(55, 110), WaterY, O.z + r.F(-30, 230));
                float h = r.F(30, 70), w = r.F(30, 60);
                Msh(Meshes.Cone(8), Root, b, new Vector3(w, h, w), M("8A96A6", 0.15f), new Vector3(0, r.F(0, 60), 0));
                Msh(Meshes.Cone(8), Root, b + Vector3.up * h * 0.55f, new Vector3(w * 0.46f, h * 0.46f, w * 0.46f), M("F6FAFF", 0.2f), new Vector3(0, r.F(0, 60), 0));
            }
        }

        static void Portal(Vector3 c)
        {
            var iceM = Glass("A8DCFF", 0.85f);
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 2.4f, 2.4f, 0), new Vector3(1, 4.8f, 1.2f), iceM);
            Lobby.Box(c + new Vector3(0, 5.2f, 0), new Vector3(6, 0.9f, 1.4f), iceM);
            var surf = Cy(Root, c + new Vector3(0, 2.4f, 0), new Vector3(3.8f, 0.02f, 4.6f), Add("FFC83A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ", c + new Vector3(0, 5.2f, 0.75f), 5, Color.white);
            t.transform.rotation = Quaternion.Euler(0, 180, 0);
            Zones.Add(c + new Vector3(0, 0, 1.4f), 2.4f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1), () => Worlds.Current == 4);
        }

        static void Checkpoint(Vector3 p) => new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);

        /// <summary>The snowboard run: from the rock top down the mountain, over two jumps, to the village.</summary>
        static List<Vector3> SnowPath()
        {
            var pts = new List<Vector3>();
            var a = BoardTop + new Vector3(0, 0.1f, 1.5f);
            Vector3[] keys = { a, V(-2, 8, 106), V(4, 6.5f, 114), V(10, 5, 120), V(12, 3.8f, 126), V(15, 2.2f, 130), V(17, 0.6f, 134), V(18, 0.25f, 137) };
            for (int i = 0; i < keys.Length - 1; i++)
                for (int k = 0; k < 10; k++) pts.Add(Vector3.Lerp(keys[i], keys[i + 1], k / 10f));
            pts.Add(keys[keys.Length - 1]);
            // the slope you see under the run
            for (int i = 1; i < keys.Length; i++)
            {
                var m = (keys[i - 1] + keys[i]) / 2; var d = keys[i] - keys[i - 1];
                var s = B(Root, m - Vector3.up * 0.35f, new Vector3(5.5f, 0.5f, d.magnitude + 0.6f), snow);
                s.transform.rotation = Quaternion.LookRotation(d.normalized);
                var rockUnder = B(Root, m - Vector3.up * 3, new Vector3(5, 5, d.magnitude), rock);
                rockUnder.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(d).eulerAngles.y, 0);
            }
            // coin rings along the run
            for (int i = 8; i < pts.Count - 5; i += 9)
                for (int k = 0; k < 8; k++)
                {
                    float an = k * Mathf.PI / 4;
                    var dir = (pts[i + 1] - pts[i]).normalized;
                    var side = Vector3.Cross(Vector3.up, dir).normalized;
                    Pickups.I.Coin(pts[i] + Vector3.up * 1.4f + side * Mathf.Cos(an) * 1.1f + Vector3.up * Mathf.Sin(an) * 1.1f);
                }
            Pickups.I.Token("w4t6", pts[pts.Count / 2] + Vector3.up * 3.2f);   // in the air over the big jump
            return pts;
        }

        // ---------------- footprints ----------------

        static readonly Queue<GameObject> prints = new Queue<GameObject>();
        static Material printM;

        /// <summary>Paw prints in the snow (only where it is snowy, not on ice).</summary>
        public static void Footprint(Vector3 p, Quaternion rot)
        {
            var pl = PlayerCtl.I;
            if (Root == null || pl == null || pl.OnIce || !pl.Grounded) return;
            printM ??= Mats.Transparent(new Color(0.55f, 0.65f, 0.8f, 0.45f));
            GameObject go;
            if (prints.Count >= 40) go = prints.Dequeue();
            else { Parts.Layer = Layers.World; go = Cy(Root, Vector3.zero, new Vector3(0.18f, 0.005f, 0.24f), printM); }
            go.transform.SetPositionAndRotation(new Vector3(p.x, pl.transform.position.y + 0.02f, p.z), rot);
            prints.Enqueue(go);
        }

        /// <summary>A punch next to a snowball kicks it forward.</summary>
        public static void HitNear(Vector3 pos, bool inAir)
        {
            if (Root == null || Worlds.Current != 4) return;
            foreach (var b in Balls)
            {
                if (b == null) continue;
                var d = b.transform.position - pos; d.y = 0;
                if (d.magnitude < b.R + 1.2f) { b.Kick(PlayerCtl.I.transform.forward); return; }
            }
        }
    }

    // =====================================================================================================

    /// <summary>Злой Снеговик: stands still and throws snowballs (a hit covers you in snow for 2 s — slower). A punch
    /// in the belly beats him: his head flies off into a snowdrift. Some keep a purple token inside.</summary>
    public class Snowman : MonoBehaviour, IHittable
    {
        Transform head, armR; float cd; string token;
        public bool Dead { get; private set; }
        float deadT, back;
        readonly List<(Transform t, Vector3 v, float life)> balls = new List<(Transform, Vector3, float)>();

        public static Snowman Spawn(Vector3 p, Transform parent, string token)
        {
            var s = new GameObject("Snowman").AddComponent<Snowman>();
            s.transform.SetParent(parent, false);
            s.transform.position = p;
            s.token = token;
            s.Build();
            World4.Snowmen.Add(s);
            return s;
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            var white = M("FFFFFF", 0.15f); var coal = M("1A1A1A", 0.4f);
            S(transform, new Vector3(0, 0.6f, 0), 1.2f, white);
            S(transform, new Vector3(0, 1.45f, 0), 0.85f, white);
            for (int k = 0; k < 3; k++) S(transform, new Vector3(0, 1.25f + k * 0.2f, 0.42f - Mathf.Abs(k - 1) * 0.02f), 0.09f, coal);
            head = Node("Head", transform, new Vector3(0, 2.15f, 0));
            S(head, Vector3.zero, 0.62f, white);
            foreach (var sd in new[] { -1, 1 })
            {
                S(head, new Vector3(sd * 0.11f, 0.06f, 0.27f), 0.08f, coal);
                B(head, new Vector3(sd * 0.11f, 0.17f, 0.26f), new Vector3(0.12f, 0.03f, 0.03f), coal, new Vector3(0, 0, sd * -25));   // angry brows
            }
            Cone(head, new Vector3(0, 0, 0.28f), new Vector3(0.1f, 0.3f, 0.1f), M("FF8A1E", 0.3f), new Vector3(90, 0, 0));
            for (int k = 0; k < 5; k++) S(head, new Vector3(-0.12f + k * 0.06f, -0.13f + Mathf.Abs(k - 2) * 0.02f, 0.26f), 0.045f, coal);
            Cy(head, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 0.03f, 0.5f), coal);
            Cy(head, new Vector3(0, 0.45f, 0), new Vector3(0.34f, 0.15f, 0.34f), coal);
            Ring(transform, new Vector3(0, 1.82f, 0), 0.3f, 0.06f, M("E53935", 0.2f));
            Rod(transform, new Vector3(-0.4f, 1.5f, 0), new Vector3(-0.9f, 1.9f, 0), 0.05f, M("5A3A22"));
            armR = Node("ArmR", transform, new Vector3(0.4f, 1.5f, 0));
            Rod(armR, Vector3.zero, new Vector3(0.5f, 0.4f, 0), 0.05f, M("5A3A22"));
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.center = new Vector3(0, 1.2f, 0); col.radius = 0.6f; col.height = 2.6f;
        }

        void Update()
        {
            UpdateBalls();
            var p = PlayerCtl.I;
            if (Dead)
            {
                deadT += Time.deltaTime;
                if (head.gameObject.activeSelf) { head.position += new Vector3(0, 4 - deadT * 12, 3) * Time.deltaTime; head.Rotate(400 * Time.deltaTime, 0, 0); }
                if (deadT > 1.2f) head.gameObject.SetActive(false);
                if (Time.time > back) Revive();
                return;
            }
            if (p == null || Worlds.Current != 4 || p.Frozen) return;
            var d = p.transform.position - transform.position; d.y = 0;
            if (d.magnitude < 14) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 3);
            armR.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3) * 10);
            if (p.Snowmobile && d.magnitude < 1.6f) { Die(); return; }   // knocked over by the snowmobile
            // a stomp on the head also works
            var rel = p.transform.position - transform.position;
            if (new Vector2(rel.x, rel.z).magnitude < 0.8f && rel.y > 2f && rel.y < 3.2f && p.Velocity.y < 1) { p.Launch(11f); Die(); return; }
            cd -= Time.deltaTime;
            if (d.magnitude < 13 && cd <= 0)
            {
                cd = 2.6f;
                var lead = p.Velocity; lead.y = 0;   // a standing chubrik always "falls" a little: aim with the run only
                Throw(p.transform.position + Vector3.up * 0.8f + lead * 0.4f);
            }
        }

        void Throw(Vector3 target)
        {
            armR.localRotation = Quaternion.Euler(0, 0, 70);
            Parts.Layer = Layers.World;
            var from = transform.position + Vector3.up * 2f + transform.right * 0.5f;
            var b = Ls(transform.parent, from, 0.3f, M("FFFFFF", 0.1f));
            float time = Mathf.Clamp(Vector3.Distance(from, target) / 11f, 0.4f, 1.3f);
            var v = (target - from) / time + Vector3.up * 0.5f * 18 * time;
            balls.Add((b.transform, v, 2.5f));
            AudioFx.Play("swing", 0.5f);
        }

        void UpdateBalls()
        {
            var p = PlayerCtl.I;
            for (int i = balls.Count - 1; i >= 0; i--)
            {
                var (t, v, life) = balls[i];
                v += Vector3.down * 18 * Time.deltaTime;
                t.position += v * Time.deltaTime;
                life -= Time.deltaTime;
                balls[i] = (t, v, life);
                bool hit = p != null && Worlds.Current == 4 && Vector3.Distance(p.transform.position + Vector3.up * 0.7f, t.position) < 0.65f;
                if (hit)
                {
                    p.SlowUntil = Time.time + 2f;
                    AudioFx.Play("splash", 0.6f);
                    Effects.Emit("puff", t.position, 20, 0.4f);
                    Hud.Toast("Бух! Тебя засыпало снегом — 2 секунды бежишь медленнее", UIKit.Cyan);
                }
                bool ground = Physics.CheckSphere(t.position, 0.15f, Layers.SolidMask, QueryTriggerInteraction.Ignore);
                if (hit || ground || life <= 0)
                {
                    if (ground) Effects.Emit("puff", t.position, 8, 0.2f);
                    Destroy(t.gameObject); balls.RemoveAt(i);
                }
            }
        }

        public bool OnHit(Vector3 from) { if (Dead) return false; Die(); return true; }

        public void Die()
        {
            if (Dead) return;
            Dead = true; deadT = 0; back = Time.time + 60;
            AudioFx.Play("bonk"); AudioFx.Play("splash", 0.5f);
            Effects.Emit("puff", transform.position + Vector3.up, 40, 0.8f, Vector3.up * 2);
            Save.AddCoins(5);
            Effects.Float("+5", transform.position + Vector3.up * 2.4f, UIKit.Gold);
            if (token != null && !Save.P.found.Contains(token)) Pickups.I.Token(token, transform.position + Vector3.up * 1.2f);
            foreach (var r in GetComponentsInChildren<Renderer>()) if (!r.transform.IsChildOf(head)) r.enabled = false;
        }

        void Revive()
        {
            Dead = false;
            head.gameObject.SetActive(true);
            head.localPosition = new Vector3(0, 2.15f, 0); head.localRotation = Quaternion.identity;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            token = null;
            Effects.Emit("puff", transform.position + Vector3.up, 20, 0.6f);
        }
    }

    /// <summary>Ледяной Кабанчик: wanders, then charges across the ice. Jump aside — he slides into a snowdrift and
    /// gets stuck horns up: that's the moment to stomp or punch him.</summary>
    public class IceBoar : MonoBehaviour, IHittable
    {
        enum St { Wander, Aim, Charge, Stuck, Dead }
        St st; float t, hitCd; Vector3 home, dir; Transform body;

        public static IceBoar Spawn(Vector3 p, Transform parent)
        {
            var b = new GameObject("IceBoar").AddComponent<IceBoar>();
            b.transform.SetParent(parent, false);
            b.transform.position = p;
            b.home = p;
            b.Build();
            World4.Boars.Add(b);
            return b;
        }

        public bool Stuck => st == St.Stuck;
        public bool Dead => st == St.Dead;

        void Build()
        {
            Parts.Layer = Layers.World;
            var fur = M("8FB4D9", 0.35f); var furDark = M("5E7FA6", 0.3f); var tusk = M("F4F7FA", 0.6f);
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 0.6f, 0), new Vector3(1f, 0.8f, 1.5f), fur);
            S(body, new Vector3(0, 0.65f, 0.75f), new Vector3(0.7f, 0.6f, 0.6f), fur);
            Cy(body, new Vector3(0, 0.6f, 1.05f), new Vector3(0.3f, 0.06f, 0.24f), furDark, new Vector3(90, 0, 0));
            foreach (var sd in new[] { -1, 1 })
            {
                S(body, new Vector3(sd * 0.16f, 0.82f, 0.98f), 0.1f, M("1A1A1A"));
                Spike(body, new Vector3(sd * 0.14f, 0.5f, 1.02f), new Vector3(sd * 0.26f, 0.85f, 1.15f), 0.1f, tusk);
                Cone(body, new Vector3(sd * 0.22f, 1.0f, 0.7f), new Vector3(0.16f, 0.25f, 0.16f), furDark, new Vector3(0, 0, -sd * 20));
                for (int k = 0; k < 2; k++) Ca(body, new Vector3(sd * 0.3f, 0.2f, -0.45f + k * 0.85f), new Vector3(0.2f, 0.22f, 0.2f), furDark);
            }
            for (int k = 0; k < 6; k++) Spike(body, new Vector3(0, 0.98f, 0.5f - k * 0.2f), new Vector3(0, 1.3f, 0.45f - k * 0.2f), 0.14f, M("CFEFFF", 0.8f));
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true; col.center = new Vector3(0, 0.6f, 0); col.radius = 0.8f;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            float dt = Time.deltaTime; t += dt;
            bool here = p != null && Worlds.Current == 4 && !p.Frozen;
            switch (st)
            {
                case St.Wander:
                {
                    float a = Time.time * 0.4f + home.x;
                    var target = home + new Vector3(Mathf.Cos(a) * 2.5f, 0, Mathf.Sin(a) * 2.5f);
                    var to = target - transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.01f) { transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 3); transform.position += to.normalized * 1.2f * dt; }
                    if (here && Vector3.Distance(p.transform.position, transform.position) < 10 && t > 2) { st = St.Aim; t = 0; }
                    break;
                }
                case St.Aim:
                {
                    var to = p != null ? p.transform.position - transform.position : transform.forward; to.y = 0;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 6);
                    body.localPosition = new Vector3(0, 0, Mathf.Sin(Time.time * 30) * 0.03f);
                    if (t > 0.9f) { st = St.Charge; t = 0; dir = transform.forward; AudioFx.Play("jump7", 0.6f); }
                    break;
                }
                case St.Charge:
                    transform.position += dir * 9 * dt;
                    if (Time.frameCount % 3 == 0) Effects.Emit("puff", transform.position, 2, 0.3f);
                    if (t > 1.2f || Vector3.Distance(transform.position, home) > 12) { st = St.Stuck; t = 0; AudioFx.Play("bonk"); Effects.Emit("puff", transform.position + dir, 30, 0.8f); }
                    break;
                case St.Stuck:
                    body.localRotation = Quaternion.Euler(70, 0, Mathf.Sin(Time.time * 20) * 6);   // horns up in the snowdrift
                    if (t > 3.2f) { st = St.Wander; t = 0; body.localRotation = Quaternion.identity; transform.position = Vector3.MoveTowards(transform.position, home, 3); }
                    break;
                case St.Dead:
                    body.localScale = Vector3.one * Mathf.Max(0.01f, 1 - t * 3);
                    if (t > 40) { st = St.Wander; t = 0; body.localScale = Vector3.one; transform.position = home; }
                    return;
            }
            if (!here) return;
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz < 1.1f && d.y > 0.7f && d.y < 2f && p.Velocity.y < 1 && (st == St.Stuck || st == St.Wander)) { p.Launch(11f); Die(); return; }
            if (p.Snowmobile && horiz < 1.4f) { Die(); return; }
            if (horiz < 1.1f && d.y < 0.9f && st == St.Charge && Time.time > hitCd) { hitCd = Time.time + 1; p.HurtBy(transform.position); }
        }

        public bool OnHit(Vector3 from) { if (st == St.Dead || st == St.Charge) return false; Die(); return true; }

        public void Die()
        {
            if (st == St.Dead) return;
            st = St.Dead; t = 0;
            body.localRotation = Quaternion.identity;
            AudioFx.Play("bonk");
            Effects.Emit("ice", transform.position + Vector3.up * 0.6f, 30, 0.6f);
            Save.AddCoins(5);
            Effects.Float("+5", transform.position + Vector3.up * 1.8f, UIKit.Gold);
        }
    }

    /// <summary>Сосулька: shakes when you run under it, then falls. Comes back after a while.</summary>
    public class Icicle : MonoBehaviour
    {
        Vector3 home; float shake = -1, fallV; bool falling; float back;
        Transform cone;
        public bool Fallen => back > 0 || falling;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            home = p;
            Parts.Layer = Layers.World;
            cone = Node("Cone", transform, Vector3.zero);
            Msh(Meshes.Cone(7), cone, Vector3.zero, new Vector3(0.45f, 1.4f, 0.45f), Glass("D8F2FF", 0.85f), new Vector3(180, 0, 0));
            Msh(Meshes.Cone(6), cone, new Vector3(0, -0.2f, 0), new Vector3(0.2f, 0.9f, 0.2f), G("BFEFFF", 1f), new Vector3(180, 0, 0));
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (back > 0) { if (Time.time > back) { back = 0; transform.position = home; cone.gameObject.SetActive(true); } return; }
            if (!falling && shake < 0 && p != null && Worlds.Current == 4)
            {
                var d = p.transform.position - home;
                if (new Vector2(d.x, d.z).magnitude < 1.6f && d.y < 0 && d.y > -7) { shake = 0; AudioFx.Play("zap", 0.3f); }
            }
            if (shake >= 0 && !falling)
            {
                shake += Time.deltaTime;
                transform.position = home + new Vector3(Mathf.Sin(Time.time * 60) * 0.05f, 0, 0);
                if (shake > 0.6f) { falling = true; fallV = 0; }
            }
            if (falling)
            {
                fallV += 25 * Time.deltaTime;
                transform.position += Vector3.down * fallV * Time.deltaTime;
                if (p != null && Worlds.Current == 4 && Vector3.Distance(p.transform.position + Vector3.up * 0.8f, transform.position + Vector3.down * 0.7f) < 0.8f) p.HurtBy(transform.position);
                if (Physics.Raycast(transform.position, Vector3.down, 1.45f, Layers.SolidMask, QueryTriggerInteraction.Ignore) || transform.position.y < home.y - 8)
                {
                    AudioFx.Play("crate", 0.6f);
                    Effects.Emit("ice", transform.position + Vector3.down, 30, 0.5f, Vector3.up * 2);
                    falling = false; shake = -1; back = Time.time + 6;
                    cone.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>A snowball: walk into it to roll it — on snow it grows. Big ones break cracked ice walls, hurt King Yeti
    /// when he is tired, and stand still as a step to jump on.</summary>
    public class Snowball : MonoBehaviour
    {
        public float R { get; private set; } = 0.35f;
        public const float Big = 1.05f;
        Vector3 home; float startR; Transform ball; SphereCollider col; Vector3 kick; bool arena;

        public static Snowball Spawn(Vector3 groundPos, Transform parent, bool arena = false)
        {
            var s = new GameObject("Snowball").AddComponent<Snowball>();
            s.transform.SetParent(parent, false);
            s.home = groundPos; s.arena = arena;
            s.startR = arena ? 0.6f : 0.35f;
            s.Build();
            World4.Balls.Add(s);
            return s;
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            ball = S(transform, Vector3.zero, 1f, Mats.Tex(Tex.Snow(), Color.white, 0.1f)).transform;
            col = gameObject.AddComponent<SphereCollider>();
            gameObject.layer = Layers.World;
            Reset();
        }

        public void Reset()
        {
            R = startR; kick = Vector3.zero;
            transform.position = home + Vector3.up * R;
            Apply();
        }

        void Apply()
        {
            ball.localScale = Vector3.one * R * 2;
            col.radius = R;
            transform.position = new Vector3(transform.position.x, home.y + R, transform.position.z);
        }

        public void Kick(Vector3 dir) { dir.y = 0; kick = dir.normalized * 7; AudioFx.Play("punch", 0.6f); }

        /// <summary>Tests: a big snowball in front of a wall, kicked into it.</summary>
        public void TestRollInto(CrackedWall w)
        {
            R = Big + 0.1f;
            var front = w.transform.position - w.transform.forward * (R + 1.2f);
            transform.position = new Vector3(front.x, transform.position.y, front.z);
            home = new Vector3(home.x, w.transform.position.y, home.z);
            Apply();
            Kick(w.transform.forward);
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 4) return;
            Vector3 move = Vector3.zero;
            var d = transform.position - p.transform.position; d.y = 0;
            var pv = p.Velocity; pv.y = 0;
            bool touching = d.magnitude < R + 0.55f && p.transform.position.y < transform.position.y + R * 0.5f;
            if (touching && pv.magnitude > 1f && Vector3.Dot(pv.normalized, d.normalized) > 0.3f) move = pv.normalized * pv.magnitude * 0.95f;
            if (kick.sqrMagnitude > 0.01f) { move += kick; kick = Vector3.MoveTowards(kick, Vector3.zero, 9 * Time.deltaTime); }
            if (move.sqrMagnitude < 0.01f) return;
            var step = move * Time.deltaTime;
            var np = transform.position + step;
            // stay on the ground of its own area
            if (!Physics.Raycast(np + Vector3.up * 0.2f, Vector3.down, R + 1f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { kick = Vector3.zero; return; }
            transform.position = np;
            ball.Rotate(Vector3.Cross(Vector3.up, step.normalized), step.magnitude / R * Mathf.Rad2Deg, Space.World);
            R = Mathf.Min(arena ? 1.4f : 1.6f, R + step.magnitude * 0.07f);
            Apply();
            if (Time.frameCount % 4 == 0) Effects.Emit("puff", transform.position - Vector3.up * R, 1, 0.2f);
            if (R >= Big)
            {
                foreach (var w in World4.Walls) if (w != null && !w.Broken && w.Touches(transform.position, R)) { w.Smash(); break; }
                var y = World4.Boss;
                if (y != null && y.Tired && Vector3.Distance(y.transform.position, transform.position) < R + 1.8f) { y.HitBySnowball(); Shatter(); }
            }
        }

        public void Shatter()
        {
            AudioFx.Play("splash");
            Effects.Emit("puff", transform.position, 50, R);
            Reset();
        }
    }

    /// <summary>A wall of cracked ice: a big rolling snowball smashes it.</summary>
    public class CrackedWall : MonoBehaviour
    {
        public bool Broken { get; private set; }
        float width; string id;

        public static CrackedWall Spawn(Vector3 p, float yaw, float width, Transform parent, string id)
        {
            var w = new GameObject("CrackedWall").AddComponent<CrackedWall>();
            w.transform.SetParent(parent, false);
            w.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            w.width = width; w.id = "w4wall_" + id;
            w.Build();
            return w;
        }

        void Build()
        {
            if (Save.P.found.Contains(id)) { Broken = true; return; }
            Parts.Layer = Layers.World;
            var keep = Lobby.root; Lobby.root = transform;
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 1.6f, 0), new Vector3(width, 3.2f, 0.8f), Glass("BFEAFF", 0.8f));
            Lobby.root = keep;
            var white = M("FFFFFF", 0.9f);
            var r = new Rng(width.GetHashCode());
            for (int k = 0; k < 14; k++)
            {
                var c = B(transform, new Vector3(r.F(-width / 2 + 0.3f, width / 2 - 0.3f), r.F(0.3f, 3f), -0.42f), new Vector3(0.05f, r.F(0.4f, 1.2f), 0.02f), white);
                c.transform.localRotation = Quaternion.Euler(0, 0, r.F(-60, 60));
            }
            var t = Prim.Text(transform, "ТРЕЩИНА", new Vector3(0, 3.6f, -0.5f), 2.6f, Hex("35E0FF"));
            t.transform.localRotation = Quaternion.identity;
        }

        public bool Touches(Vector3 p, float r)
        {
            var l = transform.InverseTransformPoint(p);
            return Mathf.Abs(l.x) < width / 2 + r * 0.5f && Mathf.Abs(l.z) < r + 0.5f;
        }

        public void Smash()
        {
            if (Broken) return;
            Broken = true;
            if (!Save.P.found.Contains(id)) { Save.P.found.Add(id); Save.Write(); }
            AudioFx.Play("crate"); AudioFx.Play("splash");
            Effects.Emit("ice", transform.position + Vector3.up * 1.5f, 120, width / 2, Vector3.up * 3);
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            Hud.Toast("Ледяная стена разбита!", UIKit.Cyan);
        }
    }

    /// <summary>The blue snowboard on the mountain top: step on it for a fast ride with jumps and coin rings.</summary>
    public class Snowboard : MonoBehaviour
    {
        List<Vector3> path; bool riding; float s, side, cd; Transform board, ridingBoard;
        public bool Riding => riding;

        public void Build(Vector3 p, Transform parent, List<Vector3> pts)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            path = pts;
            Parts.Layer = Layers.World;
            board = Node("Board", transform, Vector3.zero);
            MakeBoard(board);
            var t = Prim.Text(transform, "СНОУБОРД", new Vector3(0, 1.4f, 0), 2.6f, Hex("35E0FF"));
            t.gameObject.AddComponent<Billboard>();
        }

        static void MakeBoard(Transform b)
        {
            var blue = M("2E7DFF", 0.6f, 0.2f);
            B(b, new Vector3(0, 0.05f, 0), new Vector3(0.45f, 0.05f, 1.6f), blue);
            foreach (var z in new[] { -0.8f, 0.8f }) Cy(b, new Vector3(0, 0.05f, z), new Vector3(0.45f, 0.025f, 0.2f), blue);
            foreach (var z in new[] { -0.3f, 0.3f }) B(b, new Vector3(0, 0.12f, z), new Vector3(0.36f, 0.08f, 0.18f), M("1A1A1A"));
            B(b, new Vector3(0, 0.08f, 0), new Vector3(0.1f, 0.01f, 1.4f), M("FFFFFF"));
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 4) return;
            if (!riding)
            {
                cd -= Time.deltaTime;
                if (cd <= 0 && Vector3.Distance(p.transform.position, transform.position) < 1.1f && !p.Frozen) StartRide();
                return;
            }
            s += Time.deltaTime * 14f;
            int i = Mathf.Min(path.Count - 2, (int)(s / 1.4f));
            float f = Mathf.Clamp01(s / 1.4f - i);
            if (i >= path.Count - 2 && f >= 1)
            {
                // finish exactly at the bottom of the run, whatever the frame step was
                p.CC.enabled = false; p.transform.position = path[path.Count - 1] + Vector3.up * 0.3f; p.CC.enabled = true;
                EndRide(); return;
            }
            var a = path[i]; var b = path[i + 1];
            var pos = Vector3.Lerp(a, b, f);
            var fwd = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, fwd).normalized;
            side = Mathf.Clamp(side + p.Steer.x * 6 * Time.deltaTime, -2.2f, 2.2f);
            // two jumps along the run
            float u = s / (1.4f * (path.Count - 1));
            float hop = Jump(u, 0.32f) + Jump(u, 0.62f);
            p.CC.enabled = false;
            p.transform.position = pos + right * side + Vector3.up * (0.15f + hop);
            p.CC.enabled = true;
            var look = fwd; look.y = 0;
            if (look.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.Slerp(p.transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 8);
            if (Time.frameCount % 2 == 0) Effects.Emit("puff", p.transform.position, 2, 0.3f);
        }

        static float Jump(float u, float at) { float k = (u - at) / 0.06f; return Mathf.Abs(k) < 1 ? (1 - k * k) * 2.6f : 0; }

        void StartRide()
        {
            var p = PlayerCtl.I;
            riding = true; s = 0; side = 0;
            p.Riding = true;
            board.gameObject.SetActive(false);
            Parts.Layer = Layers.Player;
            ridingBoard = Node("RideBoard", p.transform, Vector3.zero);
            MakeBoard(ridingBoard);
            AudioFx.Play("jump5");
            Hud.Toast("Вжух! Держись — наклоняйся влево-вправо и собирай кольца из монет", UIKit.Cyan);
        }

        public void EndRide()
        {
            var p = PlayerCtl.I;
            riding = false; cd = 4;
            if (p != null) { p.Riding = false; p.SafePos = p.transform.position + Vector3.up * 0.2f; }
            if (ridingBoard) Destroy(ridingBoard.gameObject);
            board.gameObject.SetActive(true);
            AudioFx.Play("land0");
            Hud.Toast("Классный спуск! Добро пожаловать в деревню", UIKit.Lime);
        }

        /// <summary>Tests: ride to the end at once.</summary>
        public void TestRide() { if (!riding) StartRide(); s = 1.4f * (path.Count - 1) - 0.5f; }
    }

    /// <summary>The station chimney: land on it three times and a fountain of tokens flies out.</summary>
    public class Chimney : MonoBehaviour
    {
        int stomps; bool wasOn; Vector3 top;
        public bool Done => Save.P.found.Contains("w4chimney");
        public int Stomps => stomps;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            top = p + Vector3.up * 1.2f;
            var keep = Lobby.root; Lobby.root = transform;
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(0.9f, 1.2f, 0.9f), M("8A4A3A", 0.2f));
            Lobby.root = keep;
            Parts.Layer = Layers.World;
            B(transform, new Vector3(0, 1.22f, 0), new Vector3(1.05f, 0.08f, 1.05f), M("5A2E22", 0.2f));
            S(transform, new Vector3(0, 1.3f, 0), new Vector3(0.9f, 0.18f, 0.9f), M("FFFFFF", 0.1f));
            gameObject.AddComponent<SteamPuffs>().smoke = true;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 4 || Done) return;
            var d = p.transform.position - top;
            bool on = p.Grounded && new Vector2(d.x, d.z).magnitude < 0.6f && Mathf.Abs(d.y) < 0.5f;
            if (on && !wasOn) Stomp();
            wasOn = on;
        }

        public void Stomp()
        {
            if (Done) return;
            stomps++;
            AudioFx.Play("bonk");
            Effects.Emit("smoke", top, 10, 0.3f, Vector3.up * 2);
            if (stomps < 3) { Hud.Toast($"Труба гудит… ещё {3 - stomps}!", UIKit.Cyan); return; }
            Save.P.found.Add("w4chimney"); Save.Write();
            AudioFx.Play("token");
            Effects.Emit("magic", top, 60, 0.4f, Vector3.up * 6);
            for (int k = 0; k < 3; k++)
            {
                float a = k * Mathf.PI * 2 / 3;
                Pickups.I.Token("w4ch" + k, top + new Vector3(Mathf.Cos(a) * 1.6f, 0.6f + k * 0.3f, Mathf.Sin(a) * 1.6f));
            }
            Hud.Toast("Из трубы вылетел фонтан жетонов!", UIKit.Violet);
        }
    }

    /// <summary>Warm steam / chimney smoke puffs.</summary>
    public class SteamPuffs : MonoBehaviour
    {
        public bool smoke; float acc;
        void Update()
        {
            if (Worlds.Current != 4) return;
            acc += Time.deltaTime * (smoke ? 4 : 10);
            int n = (int)acc; acc -= n;
            if (n > 0) Effects.Emit(smoke ? "smoke" : "puff", transform.position + Vector3.up * (smoke ? 1.4f : 0.2f), n, 0.3f, Vector3.up * (smoke ? 1.5f : 3));
        }
    }

    /// <summary>Snowflakes falling around the player.</summary>
    public class SnowFall : MonoBehaviour
    {
        float acc;
        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 4) return;
            acc += Time.deltaTime * 30;
            int n = (int)acc; acc -= n;
            for (int i = 0; i < n; i++)
                Effects.Emit("snow", p.transform.position + new Vector3(Random.Range(-14f, 14f), Random.Range(5f, 9f), Random.Range(-10f, 16f)), 1, 0.1f, new Vector3(Random.Range(-0.3f, 0.3f), -1.2f, 0));
        }
    }

    /// <summary>The Frosty lucky box in the secret cave: a winter thing you don't have yet (then coins).</summary>
    public class FrostBox : MonoBehaviour
    {
        float cd;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            var keep = Lobby.root; Lobby.root = transform;
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 0.55f, 0), new Vector3(1.3f, 1.1f, 1.1f), M("5FD3FF", 0.8f, 0.5f));
            Lobby.root = keep;
            var frost = M("F4FBFF", 0.7f);
            B(transform, new Vector3(0, 0.55f, 0), new Vector3(1.35f, 0.2f, 1.15f), frost);
            B(transform, new Vector3(0, 0.55f, 0), new Vector3(0.2f, 1.15f, 1.15f), frost);
            Msh(Meshes.Star(6, 0.5f), transform, new Vector3(0, 0.7f, 0.57f), new Vector3(0.4f, 0.4f, 0.05f), G("FFFFFF", 1.5f));
            S(transform, new Vector3(0, 0.6f, 0), 2f, Add("BFEFFF", 0.15f)).AddComponent<Pulse>();
            var t = Prim.Text(transform, "МОРОЗНЫЙ ЛАКИ-БОКС", new Vector3(0, 1.9f, 0), 2.6f, Hex("BFEFFF"));
            t.gameObject.AddComponent<Billboard>();
            Zones.Add(p + new Vector3(0, 0, -0.5f), 2.2f, "ОТКРЫТЬ МОРОЗНЫЙ ЛАКИ-БОКС", Open, () => Worlds.Current == 4);
        }

        public void Open()
        {
            if (Time.time < cd) return;
            cd = Time.time + 2;
            var left = new List<string>();
            foreach (var id in Catalog.WinterItems) if (!Save.Owns(id)) left.Add(id);
            Effects.Emit("ice", transform.position + Vector3.up, 60, 0.8f, Vector3.up * 3);
            if (left.Count == 0) { Save.AddCoins(300); AudioFx.Play("coin"); Hud.Toast("Все зимние вещи уже твои! +300 монет", UIKit.Gold); return; }
            var it = Catalog.Get(left[Random.Range(0, left.Count)]);
            Save.Give(it.id);
            Save.Write();
            ShopUI.ShowBox(it);
        }
    }

    /// <summary>
    /// Король Йети on the castle top: walks, pounds the ground (a snow wave — jump over it), throws ice blocks,
    /// then gets tired and pants. Roll a big snowball into him while he pants — three times.
    /// </summary>
    public class Yeti : MonoBehaviour
    {
        Vector3 home; Transform body, armL, armR, crown;
        enum St { Sit, Walk, Pound, Throw, Tired, Gone }
        St st; float t, hurtCd; int throws;
        public int Hits { get; private set; }
        public bool Defeated { get; private set; }
        public bool Tired => st == St.Tired;
        const float ArenaR = 10f;
        readonly List<(Transform t, float r)> waves = new List<(Transform, float)>();
        readonly List<(Transform t, Vector3 v)> blocks = new List<(Transform, Vector3)>();

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p; home = World4.ArenaC;
            transform.rotation = Quaternion.Euler(0, 180, 0);
            Parts.Layer = Layers.World;
            var fur = M("F2F6FA", 0.1f); var face = M("9FB4C8", 0.3f); var dark = M("2A3646", 0.4f);
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 2f, 0), new Vector3(2.6f, 2.8f, 2.2f), fur);
            Dt.Fur(body, new Vector3(0, 2f, 0), new Vector3(1.35f, 1.45f, 1.15f), 40, 0.35f, fur, 99);
            S(body, new Vector3(0, 3.7f, 0.2f), new Vector3(1.5f, 1.3f, 1.3f), fur);
            S(body, new Vector3(0, 3.6f, 0.75f), new Vector3(0.9f, 0.75f, 0.5f), face);
            foreach (var sd in new[] { -1, 1 })
            {
                S(body, new Vector3(sd * 0.22f, 3.8f, 0.98f), 0.18f, G("7DFFEA", 1.6f));
                B(body, new Vector3(sd * 0.22f, 4f, 0.95f), new Vector3(0.28f, 0.07f, 0.06f), dark, new Vector3(0, 0, sd * -20));
                Spike(body, new Vector3(sd * 0.25f, 3.35f, 0.95f), new Vector3(sd * 0.27f, 3.18f, 1f), 0.09f, M("FFFFFF"));
                Ca(body, new Vector3(sd * 0.7f, 0.5f, 0), new Vector3(0.9f, 0.6f, 0.9f), fur);
            }
            crown = Node("Crown", body, new Vector3(0, 4.35f, 0.2f));
            Cy(crown, Vector3.zero, new Vector3(1.1f, 0.12f, 1.1f), Glass("A8DCFF", 0.9f));
            for (int k = 0; k < 7; k++) { float a = k * Mathf.PI * 2 / 7; Spike(crown, new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f), new Vector3(Mathf.Cos(a) * 0.6f, 0.7f + (k % 2) * 0.3f, Mathf.Sin(a) * 0.6f), 0.25f, G("BFEFFF", 1.2f)); }
            armL = Node("ArmL", body, new Vector3(-1.5f, 3f, 0)); armR = Node("ArmR", body, new Vector3(1.5f, 3f, 0));
            foreach (var a in new[] { armL, armR }) { Ca(a, new Vector3(0, -0.9f, 0), new Vector3(0.75f, 1f, 0.75f), fur); S(a, new Vector3(0, -2f, 0), new Vector3(1f, 0.8f, 1f), face); }
            var label = Prim.Text(transform, "КОРОЛЬ ЙЕТИ", new Vector3(0, 6.2f, 0), 3.6f, Hex("BFEFFF"));
            label.gameObject.AddComponent<Billboard>();
            label.gameObject.name = "Label";
            if (Save.P.done.Contains(4)) { Defeated = true; st = St.Gone; body.gameObject.SetActive(false); label.gameObject.SetActive(false); SpawnFacet(); }
        }

        bool PlayerIn(out PlayerCtl p)
        {
            p = PlayerCtl.I;
            if (p == null || Worlds.Current != 4 || p.Frozen) return false;
            var d = p.transform.position - home; d.y = 0;
            return d.magnitude < ArenaR + 0.5f && Mathf.Abs(p.transform.position.y - home.y) < 6;
        }

        void Update()
        {
            UpdateWaves();
            if (Defeated) return;
            float dt = Time.deltaTime; t += dt;
            bool active = PlayerIn(out var p);
            switch (st)
            {
                case St.Sit:
                    body.localPosition = Vector3.down * 0.5f;
                    if (active) { st = St.Walk; t = 0; body.localPosition = Vector3.zero; AudioFx.Play("hurt", 0.7f); Hud.Toast("КОРОЛЬ ЙЕТИ! Прыгай через снежные волны, а когда он устанет — закати в него большой снежный ком!", UIKit.Cyan); }
                    break;
                case St.Walk:
                {
                    if (!active) { st = St.Sit; break; }
                    var to = p.transform.position - transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 2.5f);
                    if (to.magnitude > 3.5f) { var np = transform.position + to.normalized * 1.3f * dt; var off = np - home; off.y = 0; if (off.magnitude < ArenaR - 3) transform.position = np; }
                    float s = Mathf.Sin(Time.time * 4);
                    armL.localRotation = Quaternion.Euler(s * 20, 0, 0); armR.localRotation = Quaternion.Euler(-s * 20, 0, 0);
                    if (t > 2.4f) { st = St.Pound; t = 0; AudioFx.Play("swing"); }
                    break;
                }
                case St.Pound:
                {
                    float k = Mathf.Clamp01(t / 0.7f);
                    armL.localRotation = armR.localRotation = Quaternion.Euler(-170 * k, 0, 0);
                    if (t > 0.7f)
                    {
                        armL.localRotation = armR.localRotation = Quaternion.identity;
                        AudioFx.Play("bonk"); AudioFx.Play("splash", 0.7f);
                        Effects.Emit("puff", transform.position + transform.forward * 1.5f, 50, 1f, Vector3.up * 3);
                        Parts.Layer = Layers.World;
                        var ring = Ring(transform.parent, transform.position + Vector3.up * 0.3f, 1f, 0.18f, Mats.Transparent(new Color(1, 1, 1, 0.85f)));
                        waves.Add((ring.transform, 1.5f));
                        st = St.Throw; t = 0; throws = 0;
                    }
                    break;
                }
                case St.Throw:
                    if (t > 0.9f)
                    {
                        t = 0; throws++;
                        if (p != null) ThrowBlock(p.transform.position);
                        if (throws >= 2) { st = St.Tired; t = 0; Hud.Toast("Король Йети устал! Быстрее — закати в него большой снежный ком!", UIKit.Lime); }
                    }
                    break;
                case St.Tired:
                    body.localRotation = Quaternion.Euler(12 + Mathf.Sin(Time.time * 6) * 4, 0, 0);
                    if (Time.frameCount % 8 == 0) Effects.Emit("puff", body.position + Vector3.up * 3.6f + transform.forward, 2, 0.2f, transform.forward * 2);
                    if (t > 5f) { st = St.Walk; t = 0; body.localRotation = Quaternion.identity; }
                    break;
            }
            if (!active || p == null) return;
            var d = p.transform.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude < 1.6f && d.y < 3 && Time.time > hurtCd && st != St.Tired) { hurtCd = Time.time + 1.2f; p.HurtBy(transform.position); }
        }

        void ThrowBlock(Vector3 target)
        {
            Parts.Layer = Layers.World;
            var from = transform.position + Vector3.up * 4.5f;
            var b = B(transform.parent, from, Vector3.one * 0.9f, Glass("BFEAFF", 0.9f));
            float time = 1.1f;
            var v = (target - from) / time + Vector3.up * 0.5f * 18 * time;
            blocks.Add((b.transform, v));
            AudioFx.Play("swing");
        }

        void UpdateWaves()
        {
            var p = PlayerCtl.I;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var (ring, r) = waves[i];
                r += Time.deltaTime * 6.5f;
                waves[i] = (ring, r);
                ring.localScale = new Vector3(r, 2, r);
                if (r > ArenaR) { Destroy(ring.gameObject); waves.RemoveAt(i); continue; }
                if (p != null && Worlds.Current == 4 && !p.Frozen)
                {
                    var d = p.transform.position - ring.position;
                    if (Mathf.Abs(new Vector2(d.x, d.z).magnitude - r) < 0.5f && d.y < 0.6f && d.y > -0.6f) p.HurtBy(ring.position);
                }
            }
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var (bt, v) = blocks[i];
                v += Vector3.down * 18 * Time.deltaTime;
                bt.position += v * Time.deltaTime;
                bt.Rotate(200 * Time.deltaTime, 120 * Time.deltaTime, 0);
                blocks[i] = (bt, v);
                bool hitP = p != null && Worlds.Current == 4 && Vector3.Distance(p.transform.position + Vector3.up * 0.7f, bt.position) < 1f;
                if (hitP) p.HurtBy(bt.position);
                if (hitP || bt.position.y < home.y + 0.3f)
                {
                    AudioFx.Play("crate", 0.7f);
                    Effects.Emit("ice", bt.position, 30, 0.6f, Vector3.up * 2);
                    Destroy(bt.gameObject); blocks.RemoveAt(i);
                }
            }
        }

        public void HitBySnowball()
        {
            if (Defeated || st != St.Tired) return;
            Hits++;
            AudioFx.Play("crate"); AudioFx.Play("boing");
            Effects.Emit("puff", body.position + Vector3.up * 2, 60, 1.2f, Vector3.up * 3);
            st = St.Walk; t = 0; body.localRotation = Quaternion.identity;
            if (Hits >= 3) Die();
            else Hud.Toast($"В яблочко! Ещё {3 - Hits}", UIKit.Lime);
        }

        /// <summary>Tests: a big snowball hits a tired Yeti.</summary>
        public void TestHit() { if (Defeated) return; st = St.Tired; HitBySnowball(); }

        void Die()
        {
            Defeated = true; st = St.Gone;
            AudioFx.Play("win3");
            Effects.Emit("puff", transform.position + Vector3.up * 2, 120, 1.6f, Vector3.up * 3);
            Effects.Emit("confetti", transform.position + Vector3.up * 4, 120, 0.8f, Vector3.up * 5);
            body.gameObject.SetActive(false);
            transform.Find("Label")?.gameObject.SetActive(false);
            Hud.Toast("Король Йети растаял от стыда! Возьми Ледяную грань Сердца", UIKit.Gold);
            SpawnFacet();
        }

        void SpawnFacet() => new GameObject("IceFacet").AddComponent<IceFacet>().Build(World4.GoalPos, transform.parent);
    }

    /// <summary>The Ice facet of the Heart: take it to finish World 4.</summary>
    public class IceFacet : MonoBehaviour
    {
        float cd;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            var gem = Node("Gem", transform, Vector3.zero);
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f), G("CFF4FF", 2f));
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 0.8f, 1.1f), G("8FD0FF", 2f), new Vector3(180, 0, 0));
            S(gem, Vector3.zero, 2.6f, Add("E8FAFF", 0.15f)).AddComponent<Pulse>();
            gem.gameObject.AddComponent<Spin>().speed = 60;
            gem.gameObject.AddComponent<Bob>().amp = 0.2f;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Time.time < cd || Worlds.Current != 4) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 2.4f && new Vector2(d.x, d.z).magnitude < 1.8f)
            {
                cd = Time.time + 6f;
                bool first = !Save.P.done.Contains(4);
                if (first) { Save.P.done.Add(4); Save.P.coins += 700; Save.P.tokens += 3; }
                else Save.P.coins += 50;
                Save.Write();
                AudioFx.Play("token");
                Effects.Emit("confetti", transform.position + Vector3.up * 2, 160, 0.5f, Vector3.up * 5);
                p.PlayVictory();
                if (!(first && Story.Show("w4end", () => Finished(false)))) Finished(!first);
            }
        }

        static void Finished(bool again)
        {
            Window.Show("МИР 4 ПРОЙДЕН!", w =>
            {
                w.Text(again ? "Ледяная грань снова сияет! +50 монет." : "Ледяная грань Сердца твоя! Награда: 700 монет и 3 жетона.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться в долине", "Спасти друзей, найти жетоны и тайную пещеру", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
        }
    }
}
