using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 3 — «Кристальные пещеры»: dark stone paths over a lava lake, glowing blue-violet crystals, lava cracks
    /// and geysers, moving platforms, the ancient song riddle of the Crystal Masters, 30 rare crystals that power the
    /// Guardian's gate, 4 cages with friends and the Crystal Guardian boss who keeps the Crystal facet of the Heart.
    /// </summary>
    public static class World3
    {
        public static Transform Root;
        public static readonly Vector3 O = new Vector3(0, 40, 2000);
        public static Vector3 Spawn => O + new Vector3(0, 0.3f, 2.5f);
        public static float LavaY => O.y - 7f;
        public static readonly List<Enemy> Enemies = new List<Enemy>();
        public const int CrystalsTotal = 30, GateNeed = 15;
        public static Vector3 GoalPos, ArenaC;
        public static Guardian Boss;
        public static CrystalGate Gate;
        static Material rock, rockDark, path, crystalA, crystalB, crystalC, lavaGlow;
        static int crystalN;

        public static int Crystals() { int n = 0; foreach (var f in Save.P.found) if (f.StartsWith("w3cr")) n++; return n; }

        public static void Build()
        {
            Root = new GameObject("World3").transform;
            var keep = Lobby.root;
            Lobby.root = Root;
            Parts.Layer = Layers.World;
            crystalN = 0;
            rock = Mats.Tex(Tex.Concrete(), Hex("6E6488"), 0.25f);
            rockDark = M("2A2338", 0.2f);
            path = Mats.Tex(Tex.Concrete(), Hex("8A80A6"), 0.3f);
            crystalA = G("6FA8FF", 1.6f); crystalB = G("A66BFF", 1.6f); crystalC = G("5FF2FF", 1.8f);
            lavaGlow = G("FF7A1E", 2.4f);

            // ---- start: portal home, signs ----
            Isle(V(0, 0, 0), 7);
            Portal(V(0, 0, -5));
            Lobby.Sign(V(-4, 0, 3.5f), 0, "МИР 3 · КРИСТАЛЬНЫЕ ПЕЩЕРЫ", "8F6BFF", 5.4f);
            Lobby.Sign(V(4, 0, 3.5f), 0, $"СОБЕРИ {GateNeed} КРИСТАЛЛОВ — ОТКРОЙ ВРАТА", "35E0FF", 5.4f);
            Cluster(V(-5, 0, -2), 7, 1.4f, 1); Cluster(V(5, 0, -1), 6, 1.2f, 2);
            Crys(V(3, 1, 1.5f)); Crys(V(-3, 1, 1.5f)); Crys(V(0, 1, 5));

            // ---- walkway → island 1 ----
            Walk(V(0, 0, 6.5f), V(4, 0, 14));
            Walk(V(4, 0, 14), V(0, 0, 21));
            Crys(V(2, 1, 10)); Crys(V(2.2f, 1, 17.5f));
            Vent(V(3.5f, 0, 14), 0f);
            Isle(V(0, 0, 27), 6);
            Cluster(V(-4, 0, 30), 6, 1.2f, 3);
            Enemy.Spawn(EnemyKind.Beetle, V(0, 0, 27), 3f, 0, 3);
            Crys(V(-3, 1, 25)); Crys(V(3.5f, 1, 29));
            Checkpoint(V(-3.8f, 0, 23.5f));
            Rescue.SpawnCage("w3c0", V(3.6f, 0, 31), -40, Root);

            // ---- stone pillars over the lava, a crystal mushroom up to a hidden key ----
            Pillar(V(5, 0.8f, 34), 1.8f); Crys(V(5, 1.8f, 34));
            Pillar(V(9, 1.6f, 38.5f), 2f); Crys(V(9, 2.6f, 38.5f));
            Lobby.Mushroom(V(9.6f, 1.6f, 39.4f), 17f);
            Pillar(V(14, 5.8f, 41), 1.6f);
            Pickups.I.Key("w3k0", V(14, 6.7f, 41));
            Pillar(V(9, 2.4f, 44), 2.2f); Crys(V(9, 3.4f, 44));

            // ---- moving platforms over the lava ----
            Moving(V(7, 2.4f, 48.5f), V(3, 2.4f, 48.5f), 5f, 0);
            Moving(V(-1, 2.4f, 52.5f), V(3, 2.4f, 52.5f), 5f, 0.5f);
            Crys(V(1, 3.4f, 50.5f));

            // ---- the song hall: the Crystal Masters' riddle ----
            Isle(V(0, 2.4f, 61), 8);
            SongHall(V(0, 2.4f, 61));
            Enemy.Spawn(EnemyKind.Magma, V(-1, 2.4f, 63), 2.6f, 0, 3);
            Enemy.Spawn(EnemyKind.Bat, V(2, 2.4f, 58), 3.2f, 0.4f, 3);
            Crys(V(-5, 3.4f, 57)); Crys(V(5, 3.4f, 57)); Crys(V(0, 3.4f, 66)); Crys(V(5, 3.4f, 64.5f));
            Checkpoint(V(-5.5f, 2.4f, 63.5f));
            Rescue.SpawnCage("w3c1", V(-4.5f, 2.4f, 66.8f), 160, Root);

            // ---- geyser walkway → island 3 ----
            Walk(V(-1, 2.4f, 68.5f), V(-5, 3.2f, 76));
            Vent(V(-2.4f, 2.7f, 71), 0.2f); Vent(V(-4.2f, 3.0f, 74), 0.6f);
            Crys(V(-2, 3.5f, 70)); Crys(V(-4.5f, 4f, 75.5f));
            Enemy.Spawn(EnemyKind.Bat, V(-3, 3.2f, 72), 1.6f, 0.1f, 3);
            Isle(V(-6, 3.2f, 82), 7);
            Cluster(V(-11, 3.2f, 82), 8, 1.6f, 4);
            Enemy.Spawn(EnemyKind.Beetle, V(-6, 3.2f, 82), 3.2f, 0.5f, 3);
            Enemy.Spawn(EnemyKind.Bat, V(-4, 3.2f, 84), 2.4f, 0.8f, 3);
            Crys(V(-9, 4.2f, 79)); Crys(V(-2.5f, 4.2f, 85)); Crys(V(-6, 4.2f, 87.5f)); Crys(V(-10, 4.2f, 82));
            // crystal mound with steps: a key on top
            Lobby.Box(V(-9.5f, 3.8f, 85.5f), new Vector3(2, 1.2f, 2), rock, 15);
            Lobby.Box(V(-10.5f, 4.6f, 87.5f), new Vector3(1.8f, 2.8f, 1.8f), rock, -10);
            Pickups.I.Key("w3k1", V(-10.5f, 6.9f, 87.5f));
            Rescue.SpawnCage("w3c2", V(-1.6f, 3.2f, 79.5f), -110, Root);

            // ---- to the gate ----
            Walk(V(-1.5f, 3.2f, 87.5f), V(0, 3.2f, 93));
            Crys(V(-0.7f, 4.2f, 90));
            Lobby.Box(V(0, 2.7f, 99), new Vector3(12, 1, 12), path);
            Cluster(V(-5, 3.2f, 95), 5, 1f, 5); Cluster(V(5, 3.2f, 103.5f), 5, 1f, 6);
            Enemy.Spawn(EnemyKind.Magma, V(1.5f, 3.2f, 98), 2.4f, 0.3f, 3);
            Checkpoint(V(-4.5f, 3.2f, 94.5f));
            Rescue.SpawnCage("w3c3", V(4.5f, 3.2f, 95.5f), -60, Root);
            Crys(V(3, 4.2f, 99)); Crys(V(-3, 4.2f, 101));
            // side ledges over the lava: the last key
            Pillar(V(8.5f, 4f, 98), 1.5f); Crys(V(8.5f, 5f, 98));
            Pillar(V(12, 5f, 101), 1.4f);
            Pickups.I.Key("w3k2", V(12, 6f, 101));
            Pillar(V(-8.5f, 4.2f, 101), 1.5f);
            Pickups.I.Key("w3k3", V(-8.5f, 5.2f, 101));

            Gate = new GameObject("CrystalGate").AddComponent<CrystalGate>();
            Gate.Build(V(0, 3.2f, 105), Root);

            // ---- the Guardian's island in the lava lake ----
            Walk(V(0, 3.2f, 106), V(0, 3.2f, 111));
            ArenaC = V(0, 3.2f, 122);
            Isle(ArenaC, 11);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4;
                var p = ArenaC + new Vector3(Mathf.Cos(a) * 10.2f, 0, Mathf.Sin(a) * 10.2f);
                Cluster(p, 3, 0.9f, 10 + k);
            }
            Crys(V(-7, 4.2f, 118)); Crys(V(7, 4.2f, 118)); Crys(V(0, 4.2f, 108.5f)); Crys(V(0, 4.2f, 129));
            GoalPos = ArenaC + Vector3.up * 1.2f;
            Boss = new GameObject("Guardian").AddComponent<Guardian>();
            Boss.Build(ArenaC, Root);

            Cave();
            Lobby.root = keep;
            Debug.Log($"[World3] crystals placed: {crystalN}");
        }

        static Vector3 V(float x, float y, float z) => O + new Vector3(x, y, z);

        // ---------------- environment (cave light, back to the sunny lobby) ----------------

        static bool saved;
        static Material skyS; static Color ambS, ambE, ambG, fogS, sunCol, camBg; static float fogA, fogB, sunI; static CameraClearFlags camClear; static AmbientMode ambMode;

        public static void Env(bool cave)
        {
            var cam = PlayerCtl.I ? PlayerCtl.I.Cam : null;
            var sun = RenderSettings.sun;
            if (!saved)
            {
                if (!cave) return;
                saved = true;
                skyS = RenderSettings.skybox; ambMode = RenderSettings.ambientMode;
                ambS = RenderSettings.ambientSkyColor; ambE = RenderSettings.ambientEquatorColor; ambG = RenderSettings.ambientGroundColor;
                fogS = RenderSettings.fogColor; fogA = RenderSettings.fogStartDistance; fogB = RenderSettings.fogEndDistance;
                if (sun) { sunCol = sun.color; sunI = sun.intensity; }
                if (cam) { camClear = cam.clearFlags; camBg = cam.backgroundColor; }
            }
            if (cave)
            {
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = Hex("5A4C8C"); RenderSettings.ambientEquatorColor = Hex("6A4A8A"); RenderSettings.ambientGroundColor = Hex("B0603A");
                RenderSettings.fogColor = Hex("241838"); RenderSettings.fogStartDistance = 30; RenderSettings.fogEndDistance = 130;
                if (sun) { sun.color = Hex("FFD2B0"); sun.intensity = 0.75f; }
                if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("241838"); }
            }
            else
            {
                RenderSettings.skybox = skyS; RenderSettings.ambientMode = ambMode;
                RenderSettings.ambientSkyColor = ambS; RenderSettings.ambientEquatorColor = ambE; RenderSettings.ambientGroundColor = ambG;
                RenderSettings.fogColor = fogS; RenderSettings.fogStartDistance = fogA; RenderSettings.fogEndDistance = fogB;
                if (sun) { sun.color = sunCol; sun.intensity = sunI; }
                if (cam) { cam.clearFlags = camClear; cam.backgroundColor = camBg; }
            }
        }

        // ---------------- pieces ----------------

        /// <summary>Round dark stone island with a rocky cone underneath and glowing lava cracks on its rim.</summary>
        static void Isle(Vector3 c, float r)
        {
            Lobby.Disc(c, r, 1.4f, path);
            Cy(Root, c + Vector3.up * 0.02f, new Vector3(r * 2 - 0.8f, 0.02f, r * 2 - 0.8f), rock);
            Msh(Meshes.Cone(9), Root, c - Vector3.up * 1.4f, new Vector3(r * 2.1f, r * 1.4f + 4, r * 2.1f), rockDark, new Vector3(180, 0, 0));
            var rng = new Rng((int)(c.x * 17 + c.z * 5));
            int cracks = (int)(r * 1.6f);
            for (int k = 0; k < cracks; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = c + d * (r - rng.F(0.3f, 1.4f)) + Vector3.up * 0.045f;
                var cr = B(Root, p, new Vector3(0.12f, 0.02f, rng.F(0.8f, 1.8f)), lavaGlow);
                cr.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0, rng.F(-40, 40), 0) * d);
            }
            // glowing drips under the rim
            for (int k = 0; k < 4; k++)
            {
                float a = rng.F(0, Mathf.PI * 2);
                Ls(Root, c + new Vector3(Mathf.Cos(a) * r * 0.8f, -1.6f - rng.F(0, 1.5f), Mathf.Sin(a) * r * 0.8f), 0.3f, lavaGlow);
            }
        }

        static void Pillar(Vector3 top, float r)
        {
            Lobby.Disc(top, r, top.y - LavaY + 1, path);
            Cy(Root, top + Vector3.up * 0.02f, new Vector3(r * 2 - 0.4f, 0.02f, r * 2 - 0.4f), rock);
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f + top.z;
                Spike(Root, top + new Vector3(Mathf.Cos(a) * r, -0.2f, Mathf.Sin(a) * r), top + new Vector3(Mathf.Cos(a) * (r + 0.6f), 0.6f, Mathf.Sin(a) * (r + 0.6f)), 0.35f, k % 2 == 0 ? crystalA : crystalB);
            }
        }

        /// <summary>Stone walkway between two top points with low crystal-studded edges (invisible rails keep you on).</summary>
        static GameObject Walk(Vector3 a, Vector3 b, float w = 3.6f)
        {
            var d = b - a;
            float len = d.magnitude + 1f;
            var go = new GameObject("Walk") { layer = Layers.World };
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation((a + b) / 2, Quaternion.LookRotation(d.normalized));
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.4f, 0); col.size = new Vector3(w, 0.8f, len);
            foreach (var sd in new[] { -1, 1 })
            {
                var rail = go.AddComponent<BoxCollider>();
                rail.center = new Vector3(sd * (w / 2 + 0.15f), 0.6f, 0); rail.size = new Vector3(0.3f, 1.2f, len - 0.4f);
            }
            B(go.transform, new Vector3(0, -0.4f, 0), new Vector3(w, 0.8f, len), path);
            B(go.transform, new Vector3(0, 0.005f, 0), new Vector3(w - 0.5f, 0.02f, len - 0.2f), rock);
            int n = Mathf.CeilToInt(len / 1.4f);
            for (int i = 0; i < n; i++)
            {
                float z = -len / 2 + (i + 0.5f) * len / n;
                foreach (var sd in new[] { -1, 1 })
                {
                    B(go.transform, new Vector3(sd * (w / 2 - 0.1f), 0.15f, z), new Vector3(0.35f, 0.3f, len / n - 0.15f), rockDark);
                    if ((i + (sd > 0 ? 1 : 0)) % 2 == 0)
                        Spike(go.transform, new Vector3(sd * (w / 2 - 0.1f), 0.25f, z), new Vector3(sd * (w / 2 + 0.05f), 0.85f, z + 0.1f), 0.2f, i % 3 == 0 ? crystalC : crystalA);
                }
            }
            Msh(Meshes.Cone(6), go.transform, new Vector3(0, -0.8f, 0), new Vector3(w * 0.9f, 2.5f, len * 0.7f), rockDark, new Vector3(180, 0, 0));
            return go;
        }

        static void Moving(Vector3 a, Vector3 b, float period, float phase)
        {
            var go = Lobby.Box(a - Vector3.up * 0.25f, new Vector3(3.2f, 0.5f, 3.2f), path);
            B(go.transform, new Vector3(0, 0.51f, 0), new Vector3(0.8f, 0.02f, 0.8f), crystalC);
            for (int k = 0; k < 4; k++) Spike(go.transform, new Vector3(k < 2 ? -0.45f : 0.45f, -0.5f, k % 2 == 0 ? -0.45f : 0.45f), new Vector3(k < 2 ? -0.2f : 0.2f, -1.6f, k % 2 == 0 ? -0.2f : 0.2f), 0.3f, crystalB);
            var mv = go.AddComponent<Mover>();
            mv.a = a - Vector3.up * 0.25f; mv.b = b - Vector3.up * 0.25f; mv.period = period; mv.phase = phase;
        }

        /// <summary>A cluster of glowing crystals: tilted six-sided spikes.</summary>
        static void Cluster(Vector3 c, int n, float size, int seed)
        {
            var r = new Rng(seed * 31 + 7);
            for (int k = 0; k < n; k++)
            {
                var d = new Vector3(r.F(-1, 1), 0, r.F(-1, 1)) * size * 0.6f;
                float h = size * r.F(0.8f, 2.2f);
                var tilt = Quaternion.Euler(r.F(-25, 25), r.F(0, 360), r.F(-25, 25));
                var tip = c + d + tilt * Vector3.up * h;
                var m = r.I(3) switch { 0 => crystalA, 1 => crystalB, _ => crystalC };
                Spike(Root, c + d, tip, size * r.F(0.25f, 0.45f), m);
            }
            Ls(Root, c + Vector3.up * size * 0.6f, size * 2.2f, Add("8F6BFF", 0.08f));
        }

        /// <summary>A rare crystal (picked up once, saved): powers the Guardian's gate.</summary>
        static void Crys(Vector3 p) => Pickups.I.Crystal("w3cr" + crystalN++, p);

        static void Vent(Vector3 p, float phase)
        {
            var v = new GameObject("LavaVent").AddComponent<LavaVent>();
            v.Build(p, Root, phase);
        }

        static void Checkpoint(Vector3 p) => new GameObject("Checkpoint").AddComponent<Checkpoint>().Build(p, Root);

        static void Portal(Vector3 c)
        {
            foreach (var sd in new[] { -1, 1 }) Lobby.Box(c + new Vector3(sd * 2.4f, 2.4f, 0), new Vector3(1, 4.8f, 1.2f), rock);
            Lobby.Box(c + new Vector3(0, 5.2f, 0), new Vector3(6, 0.9f, 1.4f), rock);
            foreach (var sd in new[] { -1, 1 }) Spike(Root, c + new Vector3(sd * 2.4f, 4.6f, 0), c + new Vector3(sd * 2.9f, 7f, 0), 0.6f, crystalB);
            var surf = Cy(Root, c + new Vector3(0, 2.4f, 0), new Vector3(3.8f, 0.02f, 4.6f), Add("FFC83A", 0.55f), new Vector3(90, 0, 0));
            surf.AddComponent<Spin>().axis = Vector3.up;
            var t = Prim.Text(Root, "В ЛОББИ", c + new Vector3(0, 5.2f, 0.75f), 5, Color.white);
            t.transform.rotation = Quaternion.Euler(0, 180, 0);
            Zones.Add(c + new Vector3(0, 0, 1.4f), 2.4f, "ПОРТАЛ В ЛОББИ", () => Worlds.Enter(1));
        }

        // ---------------- the song riddle ----------------

        static readonly List<SingingCrystal> singers = new List<SingingCrystal>();
        static readonly int[] songOrder = { 2, 0, 3, 1 };
        static int songStep;
        public static bool SongSolved => Save.P.found.Contains("w3song");
        static GameObject secretBridge;
        static readonly string[] songCols = { "FF5252", "FFD23A", "5FF2FF", "B57CFF" };

        static void SongHall(Vector3 c)
        {
            singers.Clear(); songStep = 0;
            // the Masters' mural: four glyphs show the order
            var wall = Lobby.Box(c + new Vector3(0, 2.2f, 7.2f), new Vector3(7, 4.4f, 0.8f), rock);
            B(Root, c + new Vector3(0, 2.4f, 6.78f), new Vector3(6.2f, 3.4f, 0.04f), M("3B3052", 0.3f));
            var title = Prim.Text(Root, "ПЕСНЯ МАСТЕРОВ", c + new Vector3(0, 3.7f, 6.7f), 3.4f, Hex("FFE14D"));
            title.transform.rotation = Quaternion.identity;
            for (int k = 0; k < 4; k++)
            {
                var gp = c + new Vector3(-2.1f + k * 1.4f, 2.3f, 6.72f);   // read left to right from the hall
                Msh(Meshes.Star(6, 0.5f), Root, gp, new Vector3(0.9f, 0.9f, 0.05f), G(songCols[songOrder[k]], 1.8f), new Vector3(0, 0, 0));
                var num = Prim.Text(Root, (k + 1).ToString(), gp + new Vector3(0, -0.85f, -0.05f), 3, Color.white);
                num.transform.rotation = Quaternion.identity;
            }
            var hint = Prim.Text(Root, "УДАРЬ КРИСТАЛЛЫ ПО ПОРЯДКУ", c + new Vector3(0, 0.9f, 6.7f), 2.4f, Color.white);
            hint.transform.rotation = Quaternion.identity;
            // four singing crystals
            for (int k = 0; k < 4; k++)
            {
                float a = (45 + k * 90) * Mathf.Deg2Rad;
                var s = new GameObject("Singer" + k).AddComponent<SingingCrystal>();
                s.Build(k, c + new Vector3(Mathf.Cos(a) * 4f, 0, Mathf.Sin(a) * 3.4f), songCols[k], Root);
                singers.Add(s);
            }
            // the secret bridge to a treasure ledge (appears when the song is right)
            secretBridge = Walk(c + new Vector3(8, 0, 0), c + new Vector3(15, 0, 2.5f), 3f);
            secretBridge.name = "SecretBridge";
            secretBridge.SetActive(SongSolved);
            Isle(c + new Vector3(18.5f, 0, 3.5f), 3.6f);
            GoldCrate(c + new Vector3(19.5f, 0.55f, 4.5f), "w3box0");
            Crate(c + new Vector3(17.2f, 0.55f, 2.2f), "w3t0");
            Crys(c + new Vector3(18.5f, 1, 1.5f));
        }

        /// <summary>The player hit something: a singing crystal within reach sings.</summary>
        public static void HitNear(Vector3 pos)
        {
            if (Root == null || Worlds.Current != 3) return;
            foreach (var s in singers)
            {
                var d = s.transform.position - pos; d.y = 0;
                if (d.magnitude < 2.3f && Mathf.Abs(s.transform.position.y - pos.y) < 2f) { Sing(s.index); return; }
            }
        }

        public static void Sing(int idx)
        {
            singers[idx].Ring();
            if (SongSolved) return;
            if (songOrder[songStep] == idx)
            {
                songStep++;
                if (songStep < songOrder.Length) return;
                Save.P.found.Add("w3song"); Save.Write();
                secretBridge.SetActive(true);
                AudioFx.Play("win3");
                Effects.Emit("magic", secretBridge.transform.position + Vector3.up, 80, 0.6f, Vector3.up * 2);
                Hud.Toast("Песня Мастеров звучит! Открылся тайный мост к сокровищу", UIKit.Lime);
            }
            else
            {
                songStep = songOrder[0] == idx ? 1 : 0;
                AudioFx.Play("error");
                Hud.Toast("Не тот порядок. Посмотри на стену Мастеров", UIKit.Orange);
            }
        }

        static void Crate(Vector3 p, string token)
        {
            var crate = Lobby.Tiled(Tex.Planks("8E5BD9"), 1);
            var b = Lobby.Box(p, Vector3.one * 1.1f, crate, 12);
            B(b.transform, Vector3.zero, new Vector3(1.04f, 0.15f, 1.04f), M("4A2A8C"));
            Msh(Meshes.Star(), b.transform, new Vector3(0, 0, 0.53f), new Vector3(0.45f, 0.45f, 0.05f), G("B57CFF", 1.5f));
            var br = b.AddComponent<Breakable>();
            br.coins = 3; br.token = token;
        }

        static void GoldCrate(Vector3 p, string id)
        {
            var b = Lobby.Box(p, Vector3.one * 1.1f, M("FFC83A", 0.85f, 0.7f), 15);
            B(b.transform, Vector3.zero, new Vector3(1.05f, 0.18f, 1.05f), M("8F6BFF", 0.4f));
            B(b.transform, Vector3.zero, new Vector3(0.18f, 1.05f, 1.05f), M("8F6BFF", 0.4f));
            S(b.transform, Vector3.zero, 1.6f, Add("FFE14D", 0.15f)).AddComponent<Pulse>();
            var br = b.AddComponent<Breakable>();
            br.coins = 10; br.lootbox = id;
        }

        /// <summary>The lava lake, cave walls with crystals, stalactites and two mysterious portals far away.</summary>
        static void Cave()
        {
            var lavaTex = LavaTex();
            var lava = Prim.Make(PrimitiveType.Plane, Root, new Vector3(O.x, LavaY, O.z + 60), new Vector3(30, 1, 30), null, "Lava", Layers.World);
            var lm = Mats.Emissive(lavaTex, Color.white, Hex("FF5A10") * 1.6f, lavaTex);
            lm.mainTextureScale = new Vector2(14, 14);
            lava.GetComponent<Renderer>().sharedMaterial = lm;
            lava.AddComponent<WaterScroll>();
            var r = new Rng(303);
            // rock spires rising from the lava with crystals
            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var b = new Vector3(O.x + side * r.F(20, 48), LavaY, O.z + r.F(-20, 150));
                float h = r.F(8, 30);
                Msh(Meshes.Cone(7), Root, b, new Vector3(r.F(5, 11), h, r.F(5, 11)), rockDark, new Vector3(0, r.F(0, 60), 0));
                if (r.Chance(0.7f)) Cluster(b + Vector3.up * h * r.F(0.3f, 0.6f) + new Vector3(-side * 2, 0, 0), 4, r.F(1, 2.2f), 100 + i);
            }
            // cave walls far around and the ceiling with stalactites
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI * 2 / 18;
                var b = new Vector3(O.x + Mathf.Cos(a) * 95, LavaY, O.z + 60 + Mathf.Sin(a) * 120);
                Msh(Meshes.Cone(6), Root, b, new Vector3(50, 80, 50), rockDark, new Vector3(0, i * 20, 0));
            }
            for (int i = 0; i < 40; i++)
            {
                var top = new Vector3(O.x + r.F(-50, 50), O.y + r.F(22, 30), O.z + r.F(-15, 150));
                Msh(Meshes.Cone(6), Root, top, new Vector3(r.F(1.5f, 4), r.F(4, 10), r.F(1.5f, 4)), r.Chance(0.25f) ? crystalB : rockDark, new Vector3(180, r.F(0, 60), 0));
            }
            // two mysterious portals glowing in the depths
            foreach (var p in new[] { new Vector3(O.x - 40, O.y + 6, O.z + 135), new Vector3(O.x + 42, O.y + 9, O.z + 70) })
            {
                var ring = Ring(Root, p, 5, 0.6f, crystalB, new Vector3(90, 0, 0), Vector3.one);
                ring.transform.LookAt(new Vector3(O.x, p.y, O.z + 60));
                ring.transform.Rotate(90, 0, 0, Space.Self);
                var surf = Cy(Root, p, new Vector3(9, 0.02f, 9), Add("8F6BFF", 0.35f));
                surf.transform.rotation = ring.transform.rotation;
                surf.AddComponent<Pulse>();
            }
            // embers rising from the lava
            new GameObject("Embers").AddComponent<Embers>().transform.SetParent(Root, false);
        }

        static Texture2D lavaTex;
        static Texture2D LavaTex()
        {
            if (lavaTex) return lavaTex;
            var cv = new Canvas2(256, 256, Hex("C83A0A"));
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float n = Tex.Fbm(x / 40f, y / 40f, 6, 404);
                    float w = Mathf.Abs(Mathf.Sin(n * 9 + x * 0.02f));
                    var c = Color.Lerp(Hex("FFD24A"), Hex("FF5A10"), Mathf.SmoothStep(0, 1, w));
                    if (n > 0.62f) c = Color.Lerp(c, Hex("3A1A10"), Mathf.Clamp01((n - 0.62f) * 6));
                    cv.px[y * 256 + x] = c;
                }
            lavaTex = cv.Tex();
            return lavaTex;
        }
    }

    /// <summary>Lava geyser: bubbles, then shoots fire for a moment. Touching the fire costs a heart.</summary>
    public class LavaVent : MonoBehaviour
    {
        float phase; Transform jet; Renderer jetR;
        const float Period = 3.4f, Burst = 1.1f;
        public bool Erupting { get; private set; }

        public void Build(Vector3 p, Transform parent, float ph)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            phase = ph;
            Parts.Layer = Layers.World;
            Cy(transform, new Vector3(0, 0.04f, 0), new Vector3(1.4f, 0.04f, 1.4f), M("2A2338", 0.2f));
            Cy(transform, new Vector3(0, 0.07f, 0), new Vector3(0.9f, 0.03f, 0.9f), G("FF7A1E", 2.6f));
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                Ls(transform, new Vector3(Mathf.Cos(a) * 0.7f, 0.12f, Mathf.Sin(a) * 0.7f), new Vector3(0.35f, 0.2f, 0.35f), M("3A2A26", 0.2f));
            }
            jet = Node("Jet", transform, Vector3.zero);
            jetR = Cy(jet, new Vector3(0, 1.5f, 0), new Vector3(0.9f, 1.5f, 0.9f), Add("FF8A1E", 0.6f)).GetComponent<Renderer>();
            Cy(jet, new Vector3(0, 1.5f, 0), new Vector3(0.45f, 1.5f, 0.45f), Add("FFE14D", 0.8f));
            jet.gameObject.SetActive(false);
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.time / Period + phase, 1) * Period;
            bool on = t > Period - Burst;
            if (on != Erupting) { Erupting = on; jet.gameObject.SetActive(on); if (on) AudioFx.Play("splash", 0.4f); }
            if (!on && t > Period - Burst - 0.8f && Time.frameCount % 4 == 0) Effects.Emit("fire", transform.position + Vector3.up * 0.2f, 2, 0.2f, Vector3.up * 2);
            if (on)
            {
                jet.localScale = new Vector3(1 + Mathf.Sin(Time.time * 30) * 0.08f, 1, 1 + Mathf.Cos(Time.time * 27) * 0.08f);
                if (Time.frameCount % 2 == 0) Effects.Emit("fire", transform.position + Vector3.up * Random.Range(0.5f, 3f), 3, 0.35f, Vector3.up * 3);
                var p = PlayerCtl.I;
                if (p != null && Worlds.Current == 3 && !p.Frozen)
                {
                    var d = p.transform.position - transform.position;
                    if (new Vector2(d.x, d.z).magnitude < 0.9f && d.y > -0.5f && d.y < 3.2f) p.HurtBy(transform.position + Vector3.down);
                }
            }
        }
    }

    /// <summary>One of the four singing crystals of the Masters' riddle: rings with its own note when hit.</summary>
    public class SingingCrystal : MonoBehaviour
    {
        public int index;
        Transform top; float glow;

        public void Build(int k, Vector3 p, string col, Transform parent)
        {
            index = k;
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            Lobby.Disc(p + Vector3.up * 0.25f, 0.8f, 0.5f, M("3B3052", 0.3f));
            top = Node("Top", transform, new Vector3(0, 0.5f, 0));
            Msh(Meshes.Cone(6), top, Vector3.zero, new Vector3(0.9f, 2.2f, 0.9f), G(col, 1.6f));
            for (int i = 0; i < 3; i++) Spike(top, new Vector3(Mathf.Cos(i * 2.1f) * 0.35f, 0, Mathf.Sin(i * 2.1f) * 0.35f), new Vector3(Mathf.Cos(i * 2.1f) * 0.7f, 1f, Mathf.Sin(i * 2.1f) * 0.7f), 0.3f, G(col, 1.2f));
            Ls(top, new Vector3(0, 1, 0), 2.2f, Add(col, 0.12f));
        }

        public void Ring()
        {
            glow = 1;
            AudioFx.Play("note" + Mathf.Clamp(index * 2, 0, 7));
            Effects.Emit("magic", transform.position + Vector3.up * 2, 20, 0.4f, Vector3.up * 2);
        }

        void Update()
        {
            glow = Mathf.MoveTowards(glow, 0, Time.deltaTime * 1.5f);
            top.localScale = Vector3.one * (1 + glow * 0.25f);
            top.localRotation = Quaternion.Euler(0, Time.time * 20 + glow * 90, 0);
        }
    }

    /// <summary>The Guardian's gate: a wall of crystal that melts away once you carry enough rare crystals.</summary>
    public class CrystalGate : MonoBehaviour
    {
        Transform door; TMPro.TextMeshPro label; Collider col;
        public bool Open { get; private set; }
        float openT = -1, hintT;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            var rock = M("2A2338", 0.2f);
            var keep = Lobby.root; Lobby.root = transform;
            foreach (var sd in new[] { -1, 1 }) Lobby.Solid(PrimitiveType.Cube, new Vector3(sd * 4.2f, 3, 0), new Vector3(2, 6, 1.6f), rock);
            Lobby.Solid(PrimitiveType.Cube, new Vector3(0, 6.4f, 0), new Vector3(10.4f, 1, 1.8f), rock);
            Lobby.root = keep;
            foreach (var sd in new[] { -1, 1 })
                for (int k = 0; k < 3; k++) Spike(transform, new Vector3(sd * 4.2f, 6 + k * 0.2f, -0.3f + k * 0.3f), new Vector3(sd * (4.6f + k * 0.4f), 8 + k * 0.6f, k * 0.2f), 0.7f, G(k == 1 ? "5FF2FF" : "A66BFF", 1.6f));
            door = Node("Door", transform, Vector3.zero);
            var doorGo = B(door, new Vector3(0, 2.9f, 0), new Vector3(6.4f, 5.8f, 0.6f), Glass("A66BFF", 0.55f));
            col = doorGo.AddComponent<BoxCollider>();
            for (int k = 0; k < 9; k++) Spike(door, new Vector3(-2.8f + k * 0.7f, 0.1f, 0.2f), new Vector3(-2.8f + k * 0.7f + Mathf.Sin(k) * 0.3f, 2.4f + (k % 3) * 1.1f, 0.3f), 0.5f, G(k % 2 == 0 ? "6FA8FF" : "B57CFF", 1.4f));
            label = Prim.Text(transform, "", new Vector3(0, 4.6f, -0.5f), 4, Color.white);
            label.transform.rotation = Quaternion.Euler(0, 0, 0);
            if (World3.Crystals() >= World3.GateNeed) { Open = true; door.gameObject.SetActive(false); }
        }

        public void ForceCheck() { if (!Open && World3.Crystals() >= World3.GateNeed) OpenUp(); }

        void OpenUp()
        {
            Open = true; openT = 0;
            col.enabled = false;
            AudioFx.Play("portal");
            Effects.Emit("magic", transform.position + Vector3.up * 3, 120, 0.8f, Vector3.up * 3);
            Hud.Toast("Врата Стража открыты! Впереди — Страж Кристаллов", UIKit.Violet);
        }

        void Update()
        {
            int n = World3.Crystals();
            label.text = Open ? "" : $"КРИСТАЛЛЫ {Mathf.Min(n, World3.GateNeed)} / {World3.GateNeed}";
            if (!Open && n >= World3.GateNeed) OpenUp();
            if (openT >= 0)
            {
                openT += Time.deltaTime;
                door.localPosition = Vector3.down * openT * 4;
                if (openT > 1.6f) { door.gameObject.SetActive(false); openT = -1; }
            }
            var p = PlayerCtl.I;
            if (!Open && p != null && Worlds.Current == 3)
            {
                hintT -= Time.deltaTime;
                if (Vector3.Distance(p.transform.position, transform.position) < 5 && hintT <= 0)
                {
                    Hud.Toast($"Врата питаются кристаллами: нужно {World3.GateNeed}, у тебя {n}", UIKit.Cyan);
                    hintT = 6;
                }
            }
        }
    }

    /// <summary>
    /// Страж Кристаллов: a stone giant with a crystal crown. Walks towards you, slams the ground — a fire wave runs
    /// out (jump over it) — then freezes for a moment: jump on its head. Three hits and it crumbles, leaving the
    /// Crystal facet of the Heart.
    /// </summary>
    public class Guardian : MonoBehaviour
    {
        Vector3 home; Transform body, armL, armR, crown; Renderer crownGlow;
        public int Hits { get; private set; }
        public bool Defeated { get; private set; }
        enum St { Idle, Walk, Windup, Stunned }
        St st; float t, hitCd, bounceCd;
        readonly List<(Transform ring, float r)> waves = new List<(Transform, float)>();
        const float Height = 3.4f, Radius = 1.3f, ArenaR = 10f;
        Heart heart;

        public void Build(Vector3 c, Transform parent)
        {
            home = c;
            transform.SetParent(parent, false);
            transform.position = c;
            Parts.Layer = Layers.World;
            var stone = Mats.Tex(Tex.Concrete(), Hex("7A7090"), 0.25f);
            var dark = M("2A2338", 0.2f);
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 1.8f, 0), new Vector3(2.4f, 2.2f, 1.9f), stone);
            S(body, new Vector3(0, 3.0f, 0.1f), new Vector3(1.5f, 1.2f, 1.3f), stone);
            foreach (var sd in new[] { -1, 1 })
            {
                S(body, new Vector3(sd * 0.32f, 3.1f, 0.68f), 0.32f, G("FF9A2E", 2.6f));
                Ca(body, new Vector3(sd * 0.6f, 0.45f, 0), new Vector3(0.75f, 0.55f, 0.75f), dark);
                for (int k = 0; k < 4; k++) B(body, new Vector3(sd * (0.6f + k * 0.15f), 1.2f + k * 0.4f, 0.9f), new Vector3(0.3f, 0.06f, 0.05f), G("FF7A1E", 2f), new Vector3(0, 0, sd * 30));
            }
            B(body, new Vector3(0, 2.7f, 0.74f), new Vector3(0.6f, 0.12f, 0.06f), dark);
            for (int k = 0; k < 6; k++) Ls(body, new Vector3(Mathf.Sin(k * 1.9f) * 0.9f, 1.4f + (k % 3) * 0.5f, Mathf.Cos(k * 1.3f) * 0.75f), 0.5f, dark);
            crown = Node("Crown", body, new Vector3(0, 3.55f, 0));
            for (int k = 0; k < 7; k++)
            {
                float a = k * Mathf.PI * 2 / 7;
                Spike(crown, new Vector3(Mathf.Cos(a) * 0.4f, 0, Mathf.Sin(a) * 0.35f), new Vector3(Mathf.Cos(a) * 0.7f, 0.9f + (k % 2) * 0.4f, Mathf.Sin(a) * 0.6f), 0.4f, G(k % 2 == 0 ? "A66BFF" : "5FF2FF", 1.6f));
            }
            crownGlow = S(crown, new Vector3(0, 0.5f, 0), 2.2f, Add("FFE14D", 0.0f)).GetComponent<Renderer>();
            crownGlow.material = Mats.Additive(new Color(1f, 0.9f, 0.4f, 0));
            armL = Node("ArmL", body, new Vector3(-1.4f, 2.4f, 0)); armR = Node("ArmR", body, new Vector3(1.4f, 2.4f, 0));
            foreach (var a in new[] { armL, armR })
            {
                Ca(a, new Vector3(0, -0.8f, 0), new Vector3(0.7f, 0.9f, 0.7f), stone);
                S(a, new Vector3(0, -1.8f, 0), new Vector3(0.9f, 0.8f, 0.9f), dark);
                Spike(a, new Vector3(0, -0.4f, -0.3f), new Vector3(0, 0.3f, -0.8f), 0.3f, G("6FA8FF", 1.4f));
            }
            var label = Prim.Text(transform, "СТРАЖ КРИСТАЛЛОВ", new Vector3(0, 5.4f, 0), 3.5f, Hex("FFE14D"));
            label.gameObject.AddComponent<Billboard>();
            label.gameObject.name = "Label";
            if (Save.P.done.Contains(3)) { Defeated = true; body.gameObject.SetActive(false); label.gameObject.SetActive(false); SpawnHeart(); }
        }

        bool PlayerIn(out PlayerCtl p)
        {
            p = PlayerCtl.I;
            if (p == null || Worlds.Current != 3 || p.Frozen) return false;
            var d = p.transform.position - home; d.y = 0;
            return d.magnitude < ArenaR + 0.5f && Mathf.Abs(p.transform.position.y - home.y) < 6;
        }

        void Update()
        {
            UpdateWaves();
            if (Defeated) return;
            float dt = Time.deltaTime;
            t += dt;
            bool active = PlayerIn(out var p);
            var c = crownGlow.material.color;
            c.a = Mathf.MoveTowards(c.a, st == St.Stunned ? 0.35f + Mathf.Sin(Time.time * 10) * 0.1f : 0, dt * 2);
            crownGlow.material.color = c;
            switch (st)
            {
                case St.Idle:
                    body.localPosition = Vector3.up * Mathf.Sin(Time.time * 1.5f) * 0.05f;
                    if (active) { st = St.Walk; t = 0; Hud.Toast("Страж проснулся! Перепрыгивай огненные волны и прыгай ему на голову, когда он застынет", UIKit.Orange); }
                    break;
                case St.Walk:
                {
                    if (!active) { st = St.Idle; break; }
                    var to = p.transform.position - transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 2.5f);
                    if (to.magnitude > 2.6f)
                    {
                        var np = transform.position + to.normalized * 1.4f * dt;
                        var off = np - home; off.y = 0;
                        if (off.magnitude < ArenaR - 2) transform.position = np;
                    }
                    float s = Mathf.Sin(Time.time * 4);
                    body.localRotation = Quaternion.Euler(0, 0, s * 4);
                    armL.localRotation = Quaternion.Euler(s * 25, 0, 0); armR.localRotation = Quaternion.Euler(-s * 25, 0, 0);
                    if (t > 2.6f) { st = St.Windup; t = 0; AudioFx.Play("swing"); }
                    break;
                }
                case St.Windup:
                {
                    float k = Mathf.Clamp01(t / 0.8f);
                    armL.localRotation = Quaternion.Euler(-160 * k, 0, 0); armR.localRotation = Quaternion.Euler(-160 * k, 0, 0);
                    body.localPosition = Vector3.up * k * 0.3f;
                    if (t > 0.8f) Slam();
                    break;
                }
                case St.Stunned:
                    armL.localRotation = Quaternion.Euler(20, 0, 30); armR.localRotation = Quaternion.Euler(20, 0, -30);
                    body.localPosition = Vector3.down * 0.25f;
                    body.localRotation = Quaternion.Euler(8, 0, Mathf.Sin(Time.time * 6) * 3);
                    if (t > 2.6f) { st = St.Walk; t = 0; }
                    break;
            }
            if (!active || p == null) return;
            // stomp on the head / touch from the side
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz < Radius + 0.4f && d.y > Height * 0.75f && d.y < Height + 1.4f && p.Velocity.y < 1f)
            {
                if (st == St.Stunned) Hit(p);
                else if (Time.time > bounceCd)
                {
                    bounceCd = Time.time + 0.6f;
                    p.Launch(11f);
                    AudioFx.Play("boing");
                    Hud.Toast("Он не застыл! Жди, пока Страж ударит по земле", UIKit.Orange);
                }
            }
            else if (horiz < Radius + 0.45f && d.y > -0.5f && d.y < Height * 0.75f && Time.time > hitCd)
            {
                hitCd = Time.time + 1.2f;
                p.HurtBy(transform.position);
            }
        }

        void Slam()
        {
            st = St.Stunned; t = 0;
            armL.localRotation = Quaternion.identity; armR.localRotation = Quaternion.identity;
            AudioFx.Play("bonk"); AudioFx.Play("splash", 0.6f);
            Effects.Emit("dust", transform.position + Vector3.up * 0.3f, 40, 0.8f);
            Effects.Emit("fire", transform.position + Vector3.up * 0.3f, 30, 0.6f, Vector3.up * 2);
            Parts.Layer = Layers.World;
            var ring = Ring(transform.parent, transform.position + Vector3.up * 0.25f, 1f, 0.12f, Mats.Additive(new Color(1f, 0.5f, 0.1f, 0.9f)));
            waves.Add((ring.transform, 1.2f));
        }

        void UpdateWaves()
        {
            var p = PlayerCtl.I;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var (ring, r) = waves[i];
                r += Time.deltaTime * 6f;
                waves[i] = (ring, r);
                ring.localScale = new Vector3(r, 1.5f, r);
                if (r > ArenaR) { Destroy(ring.gameObject); waves.RemoveAt(i); continue; }
                if (p != null && Worlds.Current == 3 && !p.Frozen)
                {
                    var d = p.transform.position - ring.position;
                    float hd = new Vector2(d.x, d.z).magnitude;
                    if (Mathf.Abs(hd - r) < 0.45f && d.y < 0.55f && d.y > -0.6f) p.HurtBy(ring.position);
                }
            }
        }

        void Hit(PlayerCtl p)
        {
            Hits++;
            p.Launch(14f);
            AudioFx.Play("crate"); AudioFx.Play("boing");
            Effects.Emit("magic", crown.position, 40, 0.6f, Vector3.up * 3);
            Effects.Emit("star", crown.position, 20, 0.4f);
            st = St.Walk; t = 0;
            if (Hits >= 3) Die();
            else Hud.Toast($"Есть! Ещё {3 - Hits}", UIKit.Lime);
        }

        /// <summary>Test helper: the next stomp lands on a frozen Guardian.</summary>
        public void TestHit() { if (Defeated) return; st = St.Stunned; t = 0; Hit(PlayerCtl.I); }

        void Die()
        {
            Defeated = true;
            AudioFx.Play("win3");
            var c = transform.position + Vector3.up * 2;
            Effects.Emit("dust", c, 80, 1.2f, Vector3.up * 2);
            Effects.Emit("magic", c, 100, 1f, Vector3.up * 4);
            Effects.Emit("confetti", c + Vector3.up * 2, 120, 0.6f, Vector3.up * 5);
            body.gameObject.SetActive(false);
            transform.Find("Label")?.gameObject.SetActive(false);
            Hud.Toast("Страж повержен! Возьми Кристальную грань Сердца", UIKit.Gold);
            SpawnHeart();
        }

        void SpawnHeart()
        {
            heart = new GameObject("CrystalFacet").AddComponent<Heart>();
            heart.Build(World3.GoalPos, transform.parent);
        }
    }

    /// <summary>The Crystal facet of the Heart of the Valley: take it to finish World 3.</summary>
    public class Heart : MonoBehaviour
    {
        float cd;

        public void Build(Vector3 p, Transform parent)
        {
            transform.SetParent(parent, false);
            transform.position = p;
            Parts.Layer = Layers.World;
            var gem = Node("Gem", transform, Vector3.zero);
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f), G("8FD0FF", 2f));
            Msh(Meshes.Cone(6), gem, Vector3.zero, new Vector3(1.1f, 0.8f, 1.1f), G("A66BFF", 2f), new Vector3(180, 0, 0));
            S(gem, Vector3.zero, 2.6f, Add("BFE8FF", 0.15f)).AddComponent<Pulse>();
            gem.gameObject.AddComponent<Spin>().speed = 60;
            gem.gameObject.AddComponent<Bob>().amp = 0.2f;
        }

        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Time.time < cd || Worlds.Current != 3) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 2.4f && new Vector2(d.x, d.z).magnitude < 1.8f)
            {
                cd = Time.time + 6f;
                bool first = !Save.P.done.Contains(3);
                if (first) { Save.P.done.Add(3); Save.P.coins += 500; Save.P.tokens += 2; }
                else Save.P.coins += 50;
                Save.Write();
                AudioFx.Play("token");
                Effects.Emit("confetti", transform.position + Vector3.up * 2, 160, 0.5f, Vector3.up * 5);
                p.PlayVictory();
                if (!(first && Story.Show("w3end", () => Finished(false)))) Finished(!first);
            }
        }

        static void Finished(bool again)
        {
            Window.Show("МИР 3 ПРОЙДЕН!", w =>
            {
                w.Text(again ? "Грань снова сияет у тебя в руках! +50 монет." : "Кристальная грань Сердца твоя! Награда: 500 монет и 2 жетона.", 32, UIKit.Gold);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                w.Row("Остаться в пещерах", "Собрать кристаллы, ключи и спасти друзей", "ОСТАТЬСЯ", UIKit.Blue, Window.Close);
            }, 960, 520);
        }
    }

    /// <summary>Embers floating up from the lava around the player.</summary>
    public class Embers : MonoBehaviour
    {
        float acc;
        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || Worlds.Current != 3) return;
            acc += Time.deltaTime * 12;
            int n = (int)acc; acc -= n;
            for (int i = 0; i < n; i++)
            {
                var at = p.transform.position + new Vector3(Random.Range(-14f, 14f), Random.Range(-6f, 2f), Random.Range(-10f, 18f));
                Effects.Emit("fire", at, 1, 0.12f, Vector3.up * 1.2f);
            }
        }
    }
}
