using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// World 6 «Ошибка 404 / Глюк-мир» set dressing after the concept art: a blocky pixel canyon under the route,
    /// giant neon «404 / ERROR / NOT FOUND / CRITICAL FAILURE / glitch» words, floating blue error dialogs,
    /// walls of green binary code, translucent neon voxel cubes, a swirling spiral portal in the sky, red glitch sparks.
    /// Plus a playable side area west of the glitch island: four voxel islands with 4 more caged friends and coins.
    /// All positions below are local to the world origin World6.O.
    /// </summary>
    public static class World6Scenery
    {
        static Transform root;
        static Material voxDark, voxTop, edgePink, edgeCyan, gridBlue, glassPurple, glassCyan, glassPink;
        static Material dlgBlue, dlgBar, dlgRed, dlgWhite, dlgPaper, binBack;
        static Material redSpark, glitchPink, glitchCyan, glitchRed, canyonCol, canyonTop, canyonDeep;

        static Vector3 W(Vector3 local) => World6.O + local;
        static Vector3 W(float x, float y, float z) => World6.O + new Vector3(x, y, z);

        public static void Build()
        {
            if (World6.Root == null) return;
            Parts.Layer = Layers.World;
            root = Node("W6Scenery", World6.Root, Vector3.zero);
            root.position = World6.O;

            // materials: created once
            voxDark = M("17113F", 0.5f); voxTop = M("241E66", 0.55f);
            edgePink = G("E040FF", 2f); edgeCyan = G("35E0FF", 2f); gridBlue = G("3C7BFF", 1.2f);
            glassPurple = Glass("8A3CFF", 0.3f); glassCyan = Glass("35C8FF", 0.24f); glassPink = Glass("FF4FD8", 0.26f);
            dlgBlue = G("1E46E0", 0.55f); dlgBar = G("0A1C8A", 0.6f); dlgRed = G("E53935", 1.3f); dlgWhite = G("EAF2FF", 0.7f);
            dlgPaper = M("DDE4F0", 0.4f); binBack = Glass("020A14", 0.82f);
            redSpark = Add("FF2A2A", 0.9f); glitchPink = G("FF3FD8", 2f); glitchCyan = G("2AF0FF", 2f); glitchRed = G("FF2A3A", 2f);
            canyonCol = M("1B1452", 0.5f); canyonTop = G("3A6BFF", 0.9f); canyonDeep = G("B03CFF", 1.1f);

            Canyon();
            SpiralPortal(new Vector3(0, 52, 175));
            GiantWords();
            Dialogs();
            BinaryWalls();
            VoxelClusters();
            Sparks();
            SideArea();
            ClearDecorFromIslands();
            Parts.Layer = Layers.World;
        }

        // ---------------- blocky pixel canyon (far below the route, no colliders) ----------------

        static void Canyon()
        {
            var r = new Rng(6404);
            var t = Node("PixelCanyon", root, Vector3.zero);
            // two banks of voxel columns along the route, the gap between them is the glowing canyon
            for (int side = -1; side <= 1; side += 2)
                for (int ix = 0; ix < 5; ix++)
                    for (int iz = 0; iz < 22; iz++)
                    {
                        float x = side * (16 + ix * 7f), z = -25 + iz * 7.5f;
                        float top = -16 + ix * 2.2f + r.F(-3f, 3f);
                        if (side < 0 && x < -22 && z > 18 && z < 70) top -= 9;   // lower under the side islands
                        float h = 14 + r.F(0, 6);
                        B(t, new Vector3(x, top - h / 2, z), new Vector3(6.6f, h, 7.1f), canyonCol);
                        if (r.Chance(0.45f)) B(t, new Vector3(x, top + 0.05f, z), new Vector3(6.7f, 0.12f, 7.2f), canyonTop);
                        else if (r.Chance(0.3f)) B(t, new Vector3(x + r.F(-1.5f, 1.5f), top + 0.7f, z + r.F(-1.5f, 1.5f)), new Vector3(2.2f, 1.4f, 2.2f), canyonCol);
                    }
            // the deep glowing floor of the canyon and a grid of light lines
            B(t, new Vector3(0, -34, 55), new Vector3(30, 0.5f, 170), canyonDeep);
            for (int k = 0; k < 8; k++) B(t, new Vector3(-10.5f + k * 3f, -33.6f, 55), new Vector3(0.18f, 0.2f, 170), gridBlue);
        }

        // ---------------- the swirling spiral portal in the sky ----------------

        static void SpiralPortal(Vector3 c)
        {
            var t = Node("SpiralPortal", root, c, new Vector3(90, 0, 0));
            Material[] mats = { edgeCyan, G("6A5CFF", 1.6f), dlgWhite, G("2A7BFF", 1.4f) };
            for (int k = 0; k < 9; k++)
            {
                var ring = Ring(t, new Vector3(0, -k * 0.6f, 0), 6 + k * 4.2f, 0.5f + k * 0.08f, mats[k % mats.Length]);
                var s = ring.AddComponent<Spin>(); s.axis = Vector3.up; s.speed = (k % 2 == 0 ? 1 : -1) * (14 + k * 3);
            }
            // pixel spiral arm made of glowing squares
            var arm = Node("Arm", t, new Vector3(0, 0.6f, 0));
            for (int k = 0; k < 46; k++)
            {
                float a = k * 0.42f, rr = 3 + k * 0.85f;
                B(arm, new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr), Vector3.one * (0.8f + k * 0.04f), k % 3 == 0 ? dlgWhite : edgeCyan);
            }
            var sa = arm.gameObject.AddComponent<Spin>(); sa.axis = Vector3.up; sa.speed = 25;
            S(t, new Vector3(0, -2, 0), new Vector3(6, 0.5f, 6), G("FFFFFF", 2.2f)).AddComponent<Pulse>().amount = 0.12f;
        }

        // ---------------- giant neon words with a chromatic glitch shadow ----------------

        static void GiantWords()
        {
            // TMP size 10 ≈ 1 m line height
            Word("404", new Vector3(-45, 22, 20), 180, "FFFFFF");
            Word("ERROR", new Vector3(42, 16, 30), 120, "FFFFFF");
            Word("CRITICAL\nFAILURE", new Vector3(-55, 22, 58), 80, "E8F4FF");
            Word("NOT FOUND", new Vector3(45, 14, 66), 80, "FFFFFF");
            Word("glitch", new Vector3(40, 24, 105), 130, "F4E8FF");
            Word("NOT FOUND", new Vector3(-38, 28, 112), 70, "FFFFFF");
            Word("404", new Vector3(16, 36, 150), 160, "FFFFFF");
            Word("ОШИБКА?", new Vector3(-40, 12, 86), 70, "FFD9FF");
        }

        static void Word(string text, Vector3 p, float size, string hex)
        {
            // face the player who walks along +z near x = 0
            var look = Quaternion.LookRotation(W(p) - W(0, 3, p.z - 45));
            var n = Node("Word " + text, root, p);
            n.rotation = look;
            var pink = Prim.Text(n, text, new Vector3(-0.012f * size, 0.006f * size, 0.3f), size, Hex("FF2BD6"));
            var cyan = Prim.Text(n, text, new Vector3(0.012f * size, -0.006f * size, 0.15f), size, Hex("20E8FF"));
            Prim.Text(n, text, Vector3.zero, size, Hex(hex));
            pink.gameObject.AddComponent<GlitchJitter>();
            cyan.gameObject.AddComponent<GlitchJitter>();
            // a few torn glitch bars across the word
            for (int k = 0; k < 3; k++)
            {
                float w = size * 0.05f;
                var bar = B(n, new Vector3((k - 1) * w * 0.4f, (k - 1) * size * 0.022f, -0.2f), new Vector3(w * (0.6f + k * 0.3f), size * 0.004f, 0.1f), k == 1 ? glitchRed : k == 0 ? glitchPink : glitchCyan);
                bar.AddComponent<GlitchJitter>();
            }
        }

        // ---------------- floating blue error dialogs ----------------

        static readonly string[] dlgTitles = { "ERROR!", "ОШИБКА 404", "Не найдено", "Загрузка…", "ГЛЮК.exe", "Внимание!" };
        static readonly string[] dlgLines = { "Мир не найден.\nПопробовать ещё раз?", "Чубрик слишком\nхорош для этого мира", "Прыжок выполнен\nс ошибкой (ура!)", "Загрузка 99%…\nещё чуть-чуть", "Банан.dll\nне отвечает", "Нажми ОК,\nничего не будет" };

        static void Dialogs()
        {
            var r = new Rng(4040);
            Vector3[] spots =
            {
                new Vector3(-22, 13, 26), new Vector3(24, 9, 38), new Vector3(-18, 16, 70), new Vector3(22, 18, 82),
                new Vector3(-6, 22, 128), new Vector3(10, 14, 120), new Vector3(-22, 4, -6), new Vector3(24, 6, 6),
                new Vector3(-48, 15, 40), new Vector3(-54, 11, 18),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var p = spots[i];
                var face = Quaternion.LookRotation(W(p) - W(0, 3, p.z - 30)) * Quaternion.Euler(r.F(-8, 8), 0, r.F(-12, 12));
                Dialog(p, face, r.F(1.4f, 2.4f), i, i % 2 == 0);
            }
            // a cascade of white windows (like the art's bottom right)
            var cas = Node("WindowCascade", root, new Vector3(24, 1, 48));
            cas.rotation = Quaternion.LookRotation(W(24, 1, 48) - W(0, 3, 20));
            for (int k = 0; k < 8; k++)
            {
                var o = new Vector3(k * 0.7f, k * 0.55f, -k * 0.25f);
                B(cas, o, new Vector3(5.6f, 3.6f, 0.15f), dlgPaper);
                B(cas, o + new Vector3(0, 1.55f, -0.09f), new Vector3(5.6f, 0.5f, 0.05f), dlgBar);
            }
            cas.gameObject.AddComponent<Bob>().amp = 0.3f;
        }

        static void Dialog(Vector3 p, Quaternion rot, float s, int i, bool bob)
        {
            var d = Node("ErrorDialog", root, p);
            d.rotation = rot;
            d.localScale = Vector3.one * s;
            B(d, Vector3.zero, new Vector3(6, 3.8f, 0.2f), dlgBlue);
            B(d, new Vector3(0, 1.62f, -0.12f), new Vector3(6, 0.56f, 0.06f), dlgBar);
            B(d, new Vector3(2.65f, 1.62f, -0.16f), new Vector3(0.48f, 0.4f, 0.06f), dlgRed);
            Cy(d, new Vector3(-2.3f, 0.45f, -0.13f), new Vector3(0.8f, 0.03f, 0.8f), dlgRed, new Vector3(90, 0, 0));
            var title = Prim.Text(d, dlgTitles[i % dlgTitles.Length], new Vector3(-0.9f, 1.62f, -0.17f), 3.4f, Color.white, false);
            title.alignment = TMPro.TextAlignmentOptions.Left;
            title.rectTransform.sizeDelta = new Vector2(3.8f, 0.6f);
            Prim.Text(d, dlgLines[i % dlgLines.Length], new Vector3(0.5f, 0.35f, -0.13f), 3f, Color.white, false);
            Prim.Text(d, "X", new Vector3(-2.3f, 0.45f, -0.18f), 5, Color.white, false);
            if (i % 3 == 1)
            {   // a loading bar that never finishes
                B(d, new Vector3(0, -1.1f, -0.12f), new Vector3(4.6f, 0.5f, 0.05f), dlgPaper);
                B(d, new Vector3(-0.4f, -1.1f, -0.16f), new Vector3(3.6f, 0.36f, 0.05f), edgeCyan);
            }
            else
            {   // OK / Cancel
                B(d, new Vector3(-1.1f, -1.2f, -0.12f), new Vector3(1.6f, 0.55f, 0.05f), dlgPaper);
                B(d, new Vector3(1.1f, -1.2f, -0.12f), new Vector3(1.6f, 0.55f, 0.05f), dlgPaper);
                Prim.Text(d, "OK", new Vector3(-1.1f, -1.2f, -0.16f), 3, Hex("0A1C8A"), false);
                Prim.Text(d, "НЕТ", new Vector3(1.1f, -1.2f, -0.16f), 3, Hex("0A1C8A"), false);
            }
            if (bob) d.gameObject.AddComponent<Bob>().amp = 0.5f;
            else d.gameObject.AddComponent<Wobble>();
        }

        // ---------------- walls of green binary code ----------------

        static void BinaryWalls()
        {
            var r = new Rng(1010);
            var sb = new System.Text.StringBuilder();
            Vector3[] spots =
            {
                new Vector3(-26, 8, 0), new Vector3(26, 10, 14), new Vector3(-24, 7, 92), new Vector3(28, 6, 92),
                new Vector3(-56, 12, 46), new Vector3(0, 20, 150),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var p = spots[i];
                var n = Node("BinaryWall", root, p);
                n.rotation = Quaternion.LookRotation(W(p) - W(0, 3, p.z - 40));
                B(n, Vector3.zero, new Vector3(14, 10, 0.3f), binBack);
                B(n, new Vector3(0, 5.05f, 0), new Vector3(14.2f, 0.16f, 0.36f), i % 2 == 0 ? edgeCyan : edgePink);
                B(n, new Vector3(0, -5.05f, 0), new Vector3(14.2f, 0.16f, 0.36f), i % 2 == 0 ? edgeCyan : edgePink);
                sb.Clear();
                for (int line = 0; line < 13; line++)
                {
                    for (int c = 0; c < 26; c++) sb.Append(r.Chance(0.5f) ? '1' : '0');
                    if (line < 12) sb.Append('\n');
                }
                var t = Prim.Text(n, sb.ToString(), new Vector3(0, 0, -0.2f), 6.4f, Hex(i % 3 == 2 ? "35E0FF" : "3DFF6E"), false);
                t.lineSpacing = -18;
                t.gameObject.AddComponent<GlitchJitter>();
            }
        }

        // ---------------- translucent neon voxel cubes ----------------

        static void VoxelClusters()
        {
            var r = new Rng(808);
            Vector3[] spots =
            {
                new Vector3(-22, 2, 8), new Vector3(24, 4, 28), new Vector3(26, 2, 54), new Vector3(-24, 10, 78),
                new Vector3(26, 8, 104), new Vector3(-24, 12, 116), new Vector3(-50, 2, 62), new Vector3(-48, 0, 14),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var n = Node("VoxelCluster", root, spots[i], new Vector3(0, r.F(0, 90), 0));
                int count = 4 + r.I(3);
                for (int k = 0; k < count; k++)
                {
                    float s = r.F(2.2f, 4.5f);
                    var o = new Vector3(r.I(3) * 3.2f - 3.2f, r.I(3) * 3.2f, r.I(2) * 3.2f);
                    VoxCube(n, o, s, k % 3 == 0 ? glassCyan : k % 3 == 1 ? glassPurple : glassPink, k % 2 == 0 ? edgePink : edgeCyan);
                }
                if (i % 2 == 0) n.gameObject.AddComponent<Bob>().amp = 0.6f;
            }
        }

        /// <summary>A see-through neon cube with glowing vertical and top edges.</summary>
        static void VoxCube(Transform p, Vector3 c, float s, Material glass, Material edge)
        {
            B(p, c, Vector3.one * s, glass);
            float h = s / 2, e = Mathf.Max(0.08f, s * 0.035f);
            for (int k = 0; k < 4; k++)
            {
                float sx = k % 2 == 0 ? -h : h, sz = k < 2 ? -h : h;
                B(p, c + new Vector3(sx, 0, sz), new Vector3(e, s, e), edge);
            }
            B(p, c + new Vector3(0, h, -h), new Vector3(s, e, e), edge);
            B(p, c + new Vector3(0, h, h), new Vector3(s, e, e), edge);
            B(p, c + new Vector3(-h, h, 0), new Vector3(e, e, s), edge);
            B(p, c + new Vector3(h, h, 0), new Vector3(e, e, s), edge);
        }

        // ---------------- red glitch sparks and torn colour bars ----------------

        static void Sparks()
        {
            var r = new Rng(13);
            var star = Meshes.Star(6, 0.3f, 0.1f);
            for (int i = 0; i < 18; i++)
            {
                var p = new Vector3((i % 2 == 0 ? -1 : 1) * r.F(13, 34), r.F(-2, 20), r.F(-10, 130));
                var sp = Msh(star, root, p, Vector3.one * r.F(1.2f, 2.6f), redSpark, new Vector3(0, 0, r.F(0, 90)));
                sp.AddComponent<Billboard>();
                var pu = sp.AddComponent<Pulse>(); pu.amount = 0.3f; pu.speed = r.F(8, 14);
                S(root, p, 0.5f, dlgWhite).AddComponent<Pulse>().amount = 0.25f;
            }
            Material[] bars = { glitchPink, glitchCyan, glitchRed, dlgWhite };
            for (int i = 0; i < 34; i++)
            {
                var p = new Vector3((i % 2 == 0 ? -1 : 1) * r.F(12, 40), r.F(-4, 28), r.F(-15, 140));
                B(root, p, new Vector3(r.F(2, 9), r.F(0.1f, 0.35f), 0.1f), bars[i % bars.Length], new Vector3(0, r.F(-30, 30), 0)).AddComponent<GlitchJitter>();
            }
        }

        // ---------------- the playable side area: voxel islands west of the glitch island ----------------

        // island centres (top surface) — the glitch island's west edge is at x = -11 (top y = 0, z 34..56)
        static readonly Vector3 isl1 = new Vector3(-30, 2.6f, 45), isl2 = new Vector3(-30, 4.4f, 61), isl3 = new Vector3(-30, 4.2f, 29), isl4 = new Vector3(-42, 7.2f, 29);

        static void SideArea()
        {
            // stepping stones from the glitch island: gaps ~3 m, small steps up
            Stone(new Vector3(-15.5f, 0.8f, 45), 2.6f, edgeCyan);
            Stone(new Vector3(-21, 1.8f, 45), 2.6f, edgePink);
            // island 1 «404»: the hub of the side area
            Island(isl1, 10, 10, "404", edgeCyan);
            // island 2 «NOT FOUND»: north, via a floating loading-bar stone
            Stone(new Vector3(-30, 3.5f, 52.8f), 2.2f, edgeCyan);
            Island(isl2, 12, 8, "NOT FOUND", edgePink);
            // island 3 «ERROR»: south, via a floating voxel
            Stone(new Vector3(-30, 3.4f, 37.2f), 2.2f, edgePink);
            Island(isl3, 10, 8, "ERROR", edgeCyan);
            // island 4: a high «CRITICAL FAILURE» voxel tower west of island 3
            Stone(new Vector3(-36.8f, 5.7f, 29), 1.8f, edgePink);
            Island(isl4, 6, 6, "CRITICAL\nFAILURE", edgePink);

            // big error dialog lying flat on island 2 as its floor decoration
            var flat = Node("FlatDialog", root, isl2 + new Vector3(1.5f, 0.02f, 0), new Vector3(90, 0, 0));
            B(flat, Vector3.zero, new Vector3(6.5f, 4.2f, 0.03f), dlgBlue);
            B(flat, new Vector3(0, 1.8f, -0.01f), new Vector3(6.5f, 0.6f, 0.03f), dlgBar);
            var ft = Prim.Text(flat, "ДРУГ НЕ НАЙДЕН\n(А ВОТ И НАЙДЕН!)", new Vector3(0, -0.2f, -0.03f), 4.4f, Color.white, false);
            ft.transform.localRotation = Quaternion.identity;

            // decor on the islands (no colliders, kept to the edges)
            VoxCube(root, isl1 + new Vector3(-4, 0.6f, -4), 1.2f, glassPurple, edgeCyan);
            VoxCube(root, isl1 + new Vector3(4, 0.5f, 4), 1f, glassCyan, edgePink);
            VoxCube(root, isl3 + new Vector3(-4, 0.5f, -3), 1f, glassPink, edgeCyan);
            var sign = Prim.Text(root, "ТУТ ТОЖЕ ДРУЗЬЯ!", isl1 + new Vector3(0, 3.6f, 0), 6, Hex("FFE36A"));
            sign.gameObject.AddComponent<Billboard>();
            sign.gameObject.AddComponent<Bob>().amp = 0.2f;

            // friends in cages + their keys
            Rescue.SpawnCage("w6c8", W(isl1 + new Vector3(-3, 0, 3)), 90, World6.Root);
            Rescue.SpawnCage("w6c9", W(isl2 + new Vector3(-4, 0, 1.5f)), 60, World6.Root);
            Rescue.SpawnCage("w6c10", W(isl3 + new Vector3(-2.5f, 0, -2)), 120, World6.Root);
            Rescue.SpawnCage("w6c11", W(isl4 + new Vector3(0, 0, 1)), 90, World6.Root);
            Pickups.I.Key("w6k8", W(isl2 + new Vector3(4, 1.1f, 2)));        // keys are found on a neighbouring island
            Pickups.I.Key("w6k9", W(isl3 + new Vector3(3, 1.1f, -1.5f)));
            Pickups.I.Key("w6k10", W(isl4 + new Vector3(-1.5f, 1.1f, -1.5f)));
            Pickups.I.Key("w6k11", W(isl1 + new Vector3(3, 1.1f, -3)));

            // coins along the path and on the islands
            Coin(new Vector3(-15.5f, 1.8f, 45)); Coin(new Vector3(-18.2f, 2.6f, 45)); Coin(new Vector3(-21, 2.8f, 45));
            for (int k = 0; k < 4; k++) Coin(isl1 + new Vector3(-1.5f + k, 1, -1.5f + k));
            Coin(new Vector3(-30, 4.5f, 52.8f)); Coin(new Vector3(-30, 4.4f, 37.2f));
            for (int k = 0; k < 5; k++) Coin(isl2 + new Vector3(-0.5f + k * 1.2f, 1, -2.5f));
            for (int k = 0; k < 4; k++) Coin(isl3 + new Vector3(0.5f + k * 1.1f, 1, 2));
            Coin(new Vector3(-36.8f, 6.7f, 29));
            for (int k = 0; k < 3; k++) Coin(isl4 + new Vector3(1.5f, 1 + k * 0.6f, -1.5f + k * 1.5f));
            Parts.Layer = Layers.World;   // Coin / Key switch the layer to Pickup
        }

        static void Coin(Vector3 local) { Pickups.I.Coin(W(local)); Parts.Layer = Layers.World; }

        /// <summary>A walkable voxel stone with a glowing top outline.</summary>
        static void Stone(Vector3 top, float s, Material edge)
        {
            Lobby.Solid(PrimitiveType.Cube, top - Vector3.up * s / 2, Vector3.one * s, voxTop, default, root);
            Outline(top, s, s, edge);
            B(root, top - Vector3.up * (s + 0.6f), Vector3.one * (s * 0.55f), voxDark).AddComponent<GlitchJitter>();
        }

        /// <summary>A walkable pixel island: flat top slab with a neon grid, voxel chunks hanging below, a floating word.</summary>
        static void Island(Vector3 top, float sx, float sz, string word, Material edge)
        {
            Lobby.Solid(PrimitiveType.Cube, top - Vector3.up * 0.75f, new Vector3(sx, 1.5f, sz), voxTop, default, root);
            Outline(top, sx, sz, edge);
            // faint pixel grid on the top
            for (int k = 1; k < 4; k++)
            {
                B(root, top + new Vector3(-sx / 2 + k * sx / 4, 0.01f, 0), new Vector3(0.06f, 0.02f, sz - 0.2f), gridBlue);
                B(root, top + new Vector3(0, 0.01f, -sz / 2 + k * sz / 4), new Vector3(sx - 0.2f, 0.02f, 0.06f), gridBlue);
            }
            // blocky rock hanging below, getting smaller towards the bottom
            var r = new Rng((int)(top.x * 31 + top.z * 7));
            for (int k = 0; k < 9; k++)
            {
                float f = 1 - k / 10f, s = Mathf.Min(sx, sz) * 0.35f * f + 0.8f;
                var o = new Vector3(r.F(-sx / 2 + s / 2, sx / 2 - s / 2) * f, -1.5f - s / 2 - k * 0.9f, r.F(-sz / 2 + s / 2, sz / 2 - s / 2) * f);
                B(root, top + o, Vector3.one * s, k % 3 == 0 ? voxTop : voxDark);
                if (k % 4 == 1) B(root, top + o + new Vector3(0, s / 2 + 0.03f, 0), new Vector3(s * 1.02f, 0.06f, s * 1.02f), edge);
            }
            // the island's neon name floating above its back edge
            var t = Prim.Text(root, word, top + new Vector3(0, 4.5f, sz / 2 + 0.5f), word.Length > 6 ? 14 : 20, Color.white);
            t.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            t.gameObject.AddComponent<Bob>().amp = 0.25f;
        }

        static void Outline(Vector3 top, float sx, float sz, Material edge)
        {
            float e = 0.12f;
            B(root, top + new Vector3(0, 0.02f, -sz / 2), new Vector3(sx + e, e, e), edge);
            B(root, top + new Vector3(0, 0.02f, sz / 2), new Vector3(sx + e, e, e), edge);
            B(root, top + new Vector3(-sx / 2, 0.02f, 0), new Vector3(e, e, sz), edge);
            B(root, top + new Vector3(sx / 2, 0.02f, 0), new Vector3(e, e, sz), edge);
        }

        /// <summary>World6's random floating menus / loading rings must not hang inside the new islands.</summary>
        static void ClearDecorFromIslands()
        {
            Vector3[] spots = { isl1, isl2, isl3, isl4, new Vector3(-18, 1.3f, 45) };
            foreach (Transform ch in World6.Root)
            {
                if (ch.name != "UpsideDownMenu" && ch.name != "Loading") continue;
                var lp = ch.position - World6.O;
                foreach (var s in spots)
                    if (Mathf.Abs(lp.x - s.x) < 10 && Mathf.Abs(lp.z - s.z) < 10 && Mathf.Abs(lp.y - s.y) < 8)
                    { ch.position += Vector3.up * 14; break; }
            }
        }
    }
}
