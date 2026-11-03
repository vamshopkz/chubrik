using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Glasses / eyes, masks, scarves and neck items.</summary>
    public partial class ChubrikModel
    {
        void Glasses(int i, int set)
        {
            var g = Node("Glasses", HeadC, new Vector3(0, eyeY, faceZ + 0.01f));
            var gold = M("FFC83A", 0.85f, 0.8f);
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                    foreach (var sd in new[] { -1, 1 }) S(g, new Vector3(sd * eyeX, -0.01f, 0), new Vector3(0.21f, 0.17f, 0.04f), M("15181E", 0.95f, 0.3f));
                    Rod(g, new Vector3(-0.05f, 0.04f, 0.01f), new Vector3(0.05f, 0.04f, 0.01f), 0.015f, gold);
                    foreach (var sd in new[] { -1, 1 }) Rod(g, new Vector3(sd * 0.24f, 0.03f, -0.02f), new Vector3(sd * 0.36f, 0.04f, -0.2f), 0.015f, gold);
                    break;
                case 1:
                {
                    var brown = M("5A3418", 0.6f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Ring(g, new Vector3(sd * eyeX, 0, 0), 0.085f, 0.02f, brown, new Vector3(90, 0, 0));
                        S(g, new Vector3(sd * eyeX, 0, -0.005f), new Vector3(0.16f, 0.16f, 0.01f), Glass("DDEEFF", 0.25f));
                    }
                    Rod(g, new Vector3(-0.055f, 0.01f, 0), new Vector3(0.055f, 0.01f, 0), 0.025f, brown);
                    break;
                }
                case 2:
                    B(g, new Vector3(0, 0, 0), new Vector3(0.56f, 0.12f, 0.05f), Add("35E0FF", 0.7f));
                    B(g, new Vector3(0, 0.07f, -0.01f), new Vector3(0.58f, 0.025f, 0.06f), M("2A3140", 0.8f, 0.5f));
                    foreach (var sd in new[] { -1, 1 }) B(g, new Vector3(sd * 0.3f, 0, -0.08f), new Vector3(0.03f, 0.1f, 0.16f), M("2A3140", 0.8f, 0.5f));
                    break;
                case 3:
                    Ring(g, new Vector3(eyeX, 0, 0.005f), 0.09f, 0.015f, gold, new Vector3(90, 0, 0));
                    S(g, new Vector3(eyeX, 0, 0), new Vector3(0.17f, 0.17f, 0.01f), Glass("FFFFFF", 0.2f));
                    Rod(g, new Vector3(eyeX + 0.06f, -0.07f, 0), new Vector3(eyeX + 0.1f, -0.3f, -0.05f), 0.008f, gold);
                    break;
                case 4:
                    S(g, new Vector3(-eyeX, 0, 0.005f), new Vector3(0.18f, 0.16f, 0.04f), M("111111", 0.3f));
                    Ring(HeadC, new Vector3(0, eyeY + 0.05f, 0), 0.41f, 0.012f, M("111111", 0.3f), new Vector3(0, 0, 18), new Vector3(1, 1, 1.02f));
                    break;
                case 5:
                    foreach (var p in Pupils) p.SetActive(false);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var st = Msh(Meshes.Star(), g, new Vector3(sd * eyeX, -0.01f, -0.02f), Vector3.one * 0.14f, G("FFD23A", 2f));
                        Add<Spin>(st).axis = Vector3.forward;
                    }
                    break;
                case 6:
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Ring(g, new Vector3(sd * eyeX, 0, 0), 0.09f, 0.022f, G("FF3FA4", 2.4f), new Vector3(90, 0, 0));
                        S(g, new Vector3(sd * eyeX, 0, -0.005f), new Vector3(0.17f, 0.17f, 0.01f), M("101018", 0.95f));
                    }
                    Rod(g, new Vector3(-0.05f, 0.01f, 0), new Vector3(0.05f, 0.01f, 0), 0.02f, G("FF3FA4", 2f));
                    break;
                case 7:
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var d = Cy(g, new Vector3(sd * eyeX, 0, 0.01f), new Vector3(0.17f, 0.006f, 0.17f), M("FFFFFF", 0.95f, 0.4f), new Vector3(90, 0, 0));
                        var hu = Add<Hue>(d); hu.speed = 0.5f; hu.offset = sd * 0.25f;
                    }
                    break;
                case 105:
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Msh(Meshes.Star(), g, new Vector3(sd * eyeX, 0, 0), new Vector3(0.24f, 0.24f, 0.05f), G("BFF3FF", 1.5f));
                        Ring(g, new Vector3(sd * eyeX, 0, -0.01f), 0.1f, 0.012f, gold, new Vector3(90, 0, 0));
                    }
                    break;
            }
        }

        void Mask(int i, int set)
        {
            var m = Node("Mask", HeadC, new Vector3(0, -0.12f, faceZ - 0.02f));
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    var grey = M("8A939E", 0.5f, 0.2f);
                    S(m, new Vector3(0, 0, 0), new Vector3(0.26f, 0.2f, 0.14f), grey);
                    foreach (var sd in new[] { -1, 1 }) Cy(m, new Vector3(sd * 0.13f, -0.03f, 0.02f), new Vector3(0.1f, 0.04f, 0.1f), M("3A4048"), new Vector3(0, sd * 30, 90));
                    Ring(HeadC, new Vector3(0, -0.06f, 0), 0.4f, 0.01f, M("222222"), default, new Vector3(1, 1, 1.02f));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 1:
                case 103:
                {
                    var c = set == 3 ? M("2A1E40", 0.3f) : M("16161A", 0.3f);
                    S(HeadC, new Vector3(0, -0.12f, 0.01f), new Vector3(0.84f, 0.44f, 0.8f), c);
                    var knot = Node("Knot", HeadC, new Vector3(0, -0.05f, -0.38f));
                    foreach (var sd in new[] { -1, 1 }) B(knot, new Vector3(sd * 0.05f, -0.12f, -0.02f), new Vector3(0.06f, 0.24f, 0.02f), c, new Vector3(10, 0, sd * 15));
                    if (set == 3) foreach (var sd in new[] { -1, 1 }) B(HeadC, new Vector3(sd * eyeX, eyeY + 0.0f, faceZ + 0.0f), new Vector3(0.14f, 0.025f, 0.02f), G("B57CFF", 3f));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 2:
                {
                    var olive = M("4F5B3A", 0.3f);
                    S(m, new Vector3(0, 0.12f, -0.08f), new Vector3(0.58f, 0.52f, 0.34f), olive);
                    foreach (var sd in new[] { -1, 1 })
                        Cy(HeadC, new Vector3(sd * eyeX, eyeY, faceZ + 0.04f), new Vector3(0.17f, 0.03f, 0.17f), Glass("1A2A1A", 0.75f), new Vector3(90, 0, 0));
                    Cy(m, new Vector3(0, -0.04f, 0.1f), new Vector3(0.16f, 0.08f, 0.16f), M("2A2E26"), new Vector3(70, 0, 0));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 3:
                {
                    var c = Node("Carnival", HeadC, new Vector3(0, eyeY, faceZ + 0.0f));
                    var poly = new[] { new Vector2(-0.3f, 0.02f), new Vector2(-0.22f, 0.1f), new Vector2(-0.05f, 0.07f), new Vector2(0, 0.03f), new Vector2(0.05f, 0.07f), new Vector2(0.22f, 0.1f), new Vector2(0.3f, 0.02f), new Vector2(0.2f, -0.08f), new Vector2(0.05f, -0.05f), new Vector2(0, -0.08f), new Vector2(-0.05f, -0.05f), new Vector2(-0.2f, -0.08f) };
                    Msh(Meshes.Fan("carn", poly, 0.03f), c, Vector3.zero, Vector3.one, M("8E24AA", 0.7f, 0.3f));
                    foreach (var sd in new[] { -1, 1 }) S(c, new Vector3(sd * eyeX, 0, 0.012f), new Vector3(0.1f, 0.06f, 0.01f), M("000000"));
                    for (int k = 0; k < 3; k++) Ca(c, new Vector3(0.28f + k * 0.03f, 0.12f + k * 0.04f, -0.02f), new Vector3(0.04f, 0.12f, 0.02f), M(k == 1 ? "FFC83A" : "E91E63"), new Vector3(0, 0, -20 - k * 15));
                    break;
                }
                case 4:
                {
                    var ch = M("D9DEE6", 0.95f, 0.9f);
                    B(m, new Vector3(0, -0.01f, 0), new Vector3(0.52f, 0.2f, 0.1f), ch);
                    for (int k = 0; k < 3; k++) B(m, new Vector3(0, 0.05f - k * 0.05f, 0.052f), new Vector3(0.4f - k * 0.08f, 0.012f, 0.01f), G("35E0FF", 2f));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 5:
                {
                    var red = M("D32F2F", 0.15f);
                    Ring(HeadC, new Vector3(0, -0.09f, 0.0f), 0.37f, 0.08f, red, new Vector3(-8, 0, 0), new Vector3(1, 1, 1.06f));
                    B(HeadC, new Vector3(0.18f, -0.28f, 0.3f), new Vector3(0.1f, 0.24f, 0.04f), red, new Vector3(10, 0, -12));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 6:
                {
                    var s = Node("Smiley", HeadC, new Vector3(0, 0, faceZ + 0.015f));
                    Cy(s, Vector3.zero, new Vector3(0.62f, 0.01f, 0.62f), M("FFD600", 0.4f), new Vector3(90, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) S(s, new Vector3(sd * 0.1f, 0.08f, 0.015f), new Vector3(0.06f, 0.1f, 0.01f), M("1A1A1A"));
                    for (int k = -3; k <= 3; k++) S(s, new Vector3(k * 0.04f, -0.12f + k * k * 0.008f, 0.015f), new Vector3(0.045f, 0.035f, 0.01f), M("1A1A1A"));
                    foreach (var e in new[] { EyeL, EyeR }) if (e) e.gameObject.SetActive(false);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 7:
                {
                    var grey = M("8C96A3", 0.7f, 0.6f);
                    B(HeadC, new Vector3(0, 0, faceZ - 0.01f), new Vector3(0.6f, 0.56f, 0.08f), grey);
                    foreach (var sd in new[] { -1, 1 }) Cy(HeadC, new Vector3(sd * 0.13f, 0.06f, faceZ + 0.035f), new Vector3(0.1f, 0.01f, 0.1f), G("FF2E2E", 2.5f), new Vector3(90, 0, 0));
                    for (int k = 0; k < 4; k++) B(HeadC, new Vector3(-0.09f + k * 0.06f, -0.14f, faceZ + 0.035f), new Vector3(0.03f, 0.08f, 0.01f), M("2A2E36"));
                    foreach (var e in new[] { EyeL, EyeR }) if (e) e.gameObject.SetActive(false);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
            }
        }

        // ================= body =================

        void Neck(int i, int set)
        {
            var n = Node("Neck", Torso, new Vector3(0, 0.47f, 0));
            void Scarf(Material m)
            {
                Ring(n, Vector3.zero, 0.25f, 0.075f, m, default, new Vector3(1, 1.2f, 1));
                var end = Node("End", n, new Vector3(0.12f, -0.02f, 0.22f), new Vector3(10, 0, 8));
                B(end, new Vector3(0, -0.14f, 0), new Vector3(0.12f, 0.28f, 0.05f), m);
                var sw = Add<Sway>(end.gameObject); sw.model = this; sw.amp = 6; sw.lift = -20; sw.axisB = Vector3.right;
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Scarf(M("FFD23A", 0.15f)); break;
                case 1: Scarf(Mats.Tex(Patterns.Stripes("E53935", "FFFFFF", 10), Color.white, 0.15f)); break;
                case 2: Scarf(M("1F1F24", 0.1f)); break;
                case 3:
                {
                    var red = M("C62828", 0.2f);
                    Ring(n, Vector3.zero, 0.24f, 0.03f, red);
                    Msh(Meshes.Fan("band", new[] { new Vector2(-0.18f, 0), new Vector2(0.18f, 0), new Vector2(0, -0.2f) }, 0.02f), n, new Vector3(0, -0.02f, 0.24f), Vector3.one, red, new Vector3(-20, 0, 0));
                    break;
                }
                case 4:
                {
                    var red = M("D81B60", 0.5f);
                    foreach (var sd in new[] { -1, 1 }) Cone(n, new Vector3(sd * 0.11f, -0.04f, 0.25f), new Vector3(0.14f, 0.11f, 0.06f), red, new Vector3(0, 0, sd * 90));
                    S(n, new Vector3(0, -0.04f, 0.26f), 0.06f, red);
                    break;
                }
                case 5:
                {
                    Ring(n, Vector3.zero, 0.24f, 0.04f, M("FFC83A", 0.8f, 0.8f));
                    var cape = Node("Cape", n, new Vector3(0, 0.0f, -0.24f), new Vector3(8, 0, 0));
                    B(cape, new Vector3(0, -0.36f, 0), new Vector3(0.62f, 0.72f, 0.025f), M("B71C1C", 0.2f));
                    var sw = Add<Sway>(cape.gameObject); sw.model = this; sw.amp = 3; sw.lift = 35; sw.axisA = Vector3.forward; sw.axisB = Vector3.right;
                    break;
                }
                case 6:
                {
                    Ring(n, new Vector3(0, -0.01f, 0), 0.25f, 0.04f, M("151515", 0.5f));
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * Mathf.PI * 2 / 10;
                        var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        Spike(n, d * 0.27f, d * 0.36f, 0.06f, M("D9DEE6", 0.9f, 0.9f));
                    }
                    break;
                }
                case 7:
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k * Mathf.PI * 2 / 14;
                        S(n, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.26f, 0.15f, M("F5EEDC", 0.05f));
                    }
                    break;
                case 105:
                {
                    var g = M("FFC83A", 0.9f, 0.9f);
                    for (int k = 0; k < 16; k++)
                    {
                        float a = k * Mathf.PI * 2 / 16;
                        var p = new Vector3(Mathf.Cos(a) * 0.25f, -0.06f - Mathf.Max(0, Mathf.Sin(a)) * 0.12f, Mathf.Sin(a) * 0.27f);
                        Ring(n, p, 0.03f, 0.01f, g, new Vector3(k % 2 * 90, a * Mathf.Rad2Deg, 0));
                    }
                    var gem = Msh(Meshes.Star(8, 0.6f), n, new Vector3(0, -0.24f, 0.3f), Vector3.one * 0.13f, G("BFF3FF", 2f));
                    Add<Spin>(gem).axis = Vector3.forward;
                    break;
                }
            }
        }
    }
}
