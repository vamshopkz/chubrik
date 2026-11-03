using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Hair, ears and hats (incl. premium hats).</summary>
    public partial class ChubrikModel
    {
        void Hair(int i, int set)
        {
            if (hairHidden) return;
            var root = Node("Hair", HeadC, Vector3.zero);
            switch (i)
            {
                case 0: // Ёжик
                {
                    var m = M("6B3E1E", 0.3f);
                    foreach (var (th, n) in new[] { (0f, 1), (22f, 6), (42f, 9), (62f, 7) })
                        for (int k = 0; k < n; k++)
                        {
                            float ph = k * 360f / n + th;
                            if (th >= 42 && Mathf.DeltaAngle(ph, 90) is var dd && Mathf.Abs(dd) < 50) continue;   // keep the face clear
                            var p = HP(th, ph, 0.92f);
                            Spike(root, p, p + p.normalized * 0.17f, 0.11f, m);
                        }
                    break;
                }
                case 1: // Ирокез
                {
                    var m = M("FF3FA4", 0.3f);
                    for (int k = 0; k < 7; k++)
                    {
                        float th = -40 + k * 20;
                        var p = th < 0 ? HP(-th, 90, 0.95f) : HP(th, 270, 0.95f);
                        Spike(root, p, p + p.normalized * (0.26f - Mathf.Abs(k - 3) * 0.03f), 0.12f, m);
                    }
                    break;
                }
                case 2: // Кудряшки
                {
                    var m = M("4A2A12", 0.2f);
                    for (int th = 0; th <= 80; th += 20)
                        for (int k = 0, n = Mathf.Max(1, th / 7); k < n; k++)
                        {
                            float ph = k * 360f / n + th;
                            if (th >= 40 && Mathf.Abs(Mathf.DeltaAngle(ph, 90)) < 45) continue;
                            S(root, HP(th, ph, 0.98f), 0.15f, m);
                        }
                    break;
                }
                case 3: // Длинные пряди
                {
                    var m = M("F2C14E", 0.4f);
                    S(root, new Vector3(0, 0.09f, -0.02f), new Vector3(0.86f, 0.66f, 0.82f), m);
                    for (int k = 0; k < 7; k++)
                    {
                        float ph = 160 + k * 36;   // sides and back
                        if (Mathf.Abs(Mathf.DeltaAngle(ph, 90)) < 55) continue;
                        var top = HP(80, ph, 1.02f);
                        Ca(root, top + Vector3.down * 0.2f, new Vector3(0.12f, 0.24f, 0.1f), m);
                    }
                    break;
                }
                case 4: // Лысина с хвостом
                {
                    var m = M("2A2A2A", 0.4f);
                    Ring(root, new Vector3(0, 0.22f, -0.33f), 0.05f, 0.025f, M("E53935"), new Vector3(70, 0, 0));
                    var tail = Node("Pony", root, new Vector3(0, 0.22f, -0.36f), new Vector3(-150, 0, 0));
                    Ca(tail, new Vector3(0, 0.16f, 0), new Vector3(0.12f, 0.17f, 0.12f), m);
                    S(tail, new Vector3(0, 0.32f, 0), 0.13f, m);
                    var sw = Add<Sway>(tail.gameObject); sw.model = this; sw.amp = 8;
                    break;
                }
                case 5: // Космо-косички
                {
                    var m = M("7B4BFF", 0.4f);
                    S(root, new Vector3(0, 0.12f, -0.03f), new Vector3(0.84f, 0.6f, 0.8f), m);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var b = Node("Braid", root, new Vector3(sd * 0.36f, 0.02f, -0.12f), new Vector3(0, 0, sd * 12));
                        for (int k = 0; k < 5; k++) S(b, new Vector3(0, -0.06f - k * 0.075f, 0), 0.1f - k * 0.008f, m);
                        var tip = S(b, new Vector3(0, -0.47f, 0), 0.12f, G("35E0FF", 2.4f)); Add<Pulse>(tip);
                        var sw = Add<Sway>(b.gameObject); sw.model = this; sw.amp = 6; sw.phase = sd;
                    }
                    break;
                }
                case 6: // Огненный чуб
                {
                    var o = G("FF6A00", 2f); var y = G("FFC400", 2.2f);
                    S(root, new Vector3(0, 0.15f, -0.04f), new Vector3(0.82f, 0.55f, 0.78f), M("B33A00", 0.3f));
                    for (int k = 0; k < 4; k++)
                    {
                        var fl = Node("Flame", root, new Vector3(-0.12f + k * 0.08f, 0.33f, 0.16f - k * 0.02f), new Vector3(-35 + k * 5, 0, (k - 1.5f) * 12));
                        var c = Cone(fl, Vector3.zero, new Vector3(0.16f, 0.36f - Mathf.Abs(k - 1.5f) * 0.06f, 0.12f), k % 2 == 0 ? o : y);
                        Add<Pulse>(c).amount = 0.12f;
                    }
                    break;
                }
                default: // Взрыв на макушке
                {
                    var m = M("FF8A00", 0.25f);
                    var r = new Rng(7);
                    for (int k = 0; k < 16; k++)
                    {
                        float th = r.F(0, 50), ph = r.F(0, 360);
                        S(root, HP(th, ph, 1.05f) + Vector3.up * r.F(0.05f, 0.18f), r.F(0.16f, 0.26f), m);
                    }
                    break;
                }
            }
        }

        void Ears(int i, int set)
        {
            if (earsHidden) return;
            var pink = M("FF9FB8", 0.3f);
            foreach (var sd in new[] { -1, 1 })
            {
                var e = Node("Ear", HeadC, Vector3.zero);
                switch (i)
                {
                    case 0:
                        Cone(e, new Vector3(sd * 0.24f, 0.26f, -0.02f), new Vector3(0.22f, 0.26f, 0.1f), Skin, new Vector3(0, 0, -sd * 24));
                        Cone(e, new Vector3(sd * 0.235f, 0.27f, 0.02f), new Vector3(0.12f, 0.17f, 0.04f), pink, new Vector3(0, 0, -sd * 24));
                        break;
                    case 1:
                        S(e, new Vector3(sd * 0.28f, 0.28f, -0.03f), new Vector3(0.19f, 0.19f, 0.08f), Skin);
                        S(e, new Vector3(sd * 0.28f, 0.28f, 0.0f), new Vector3(0.11f, 0.11f, 0.04f), pink);
                        break;
                    case 2:
                        Ca(e, new Vector3(sd * 0.13f, 0.58f, -0.04f), new Vector3(0.13f, 0.26f, 0.07f), Skin, new Vector3(0, 0, -sd * 10));
                        Ca(e, new Vector3(sd * 0.13f, 0.58f, -0.005f), new Vector3(0.07f, 0.2f, 0.03f), pink, new Vector3(0, 0, -sd * 10));
                        break;
                    case 3:
                        Cone(e, new Vector3(sd * 0.36f, 0.06f, -0.04f), new Vector3(0.13f, 0.34f, 0.06f), Skin, new Vector3(-15, 0, -sd * 75));
                        break;
                    case 4:
                    {
                        var metal = M("B9C3CF", 0.8f, 0.6f);
                        Cy(e, new Vector3(sd * 0.42f, 0.05f, 0), new Vector3(0.28f, 0.015f, 0.28f), metal, new Vector3(0, 0, 90));
                        Cone(e, new Vector3(sd * 0.43f, 0.05f, 0), new Vector3(0.07f, 0.14f, 0.07f), metal, new Vector3(0, 0, -sd * 90));
                        S(e, new Vector3(sd * 0.57f, 0.05f, 0), 0.04f, G("FF3B3B"));
                        break;
                    }
                    case 5:
                    {
                        var red = M("C62828", 0.6f);
                        var h = Node("Horn", e, new Vector3(sd * 0.16f, 0.33f, 0.06f), new Vector3(-10, 0, -sd * 22));
                        Cy(h, new Vector3(0, 0.05f, 0), new Vector3(0.1f, 0.05f, 0.1f), red);
                        Cone(h, new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.18f, 0.1f), red, new Vector3(0, 0, -sd * 25));
                        break;
                    }
                    case 6:
                        Ca(e, new Vector3(sd * 0.39f, -0.04f, -0.03f), new Vector3(0.14f, 0.2f, 0.07f), Skin, new Vector3(0, 0, sd * 14));
                        break;
                    default:
                        Rod(e, new Vector3(sd * 0.12f, 0.3f, 0), new Vector3(sd * 0.18f, 0.6f, 0), 0.025f, M("333333", 0.6f, 0.5f));
                        var ball = S(e, new Vector3(sd * 0.18f, 0.62f, 0), 0.09f, G(sd < 0 ? "FF3FA4" : "35E0FF", 2.4f));
                        Add<Pulse>(ball);
                        break;
                }
            }
        }

        void Hat(int i, int set)
        {
            var h = Node("Hat", HeadC, new Vector3(0, hatY, 0));
            void Cap(string col, bool logo)
            {
                var m = M(col, 0.35f);
                S(h, new Vector3(0, -0.08f, 0), new Vector3(0.84f, 0.5f, 0.82f), m);
                Cy(h, new Vector3(0, -0.13f, 0.32f), new Vector3(0.5f, 0.012f, 0.42f), m, new Vector3(-8, 0, 0));
                S(h, new Vector3(0, 0.17f, 0), 0.06f, m);
                if (logo) Msh(Meshes.Star(), h, new Vector3(0, 0.0f, 0.36f), Vector3.one * 0.13f, M("FFFFFF"), new Vector3(-25, 0, 0));
                hairHidden = true;
            }
            void Crown(string gold, string gem, int spikes, float size, Material spikeMat = null)
            {
                var g = M(gold, 0.85f, 0.8f);
                Cy(h, new Vector3(0, -0.02f, 0), new Vector3(0.56f * size, 0.06f, 0.56f * size), g);
                for (int k = 0; k < spikes; k++)
                {
                    float a = k * Mathf.PI * 2 / spikes;
                    var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.26f * size;
                    Cone(h, p + Vector3.up * 0.03f, new Vector3(0.12f, 0.2f * size, 0.12f), spikeMat ?? g);
                    S(h, p * 1.06f + Vector3.up * 0.0f, 0.06f, G(gem, 1.2f));
                }
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Cap("FF8A1E", false); break;
                case 1: Cap("FFD23A", false); break;
                case 2: Cap("222428", true); break;
                case 3:
                {
                    var m = M("E53935", 0.2f);
                    S(h, new Vector3(0, -0.06f, 0), new Vector3(0.86f, 0.66f, 0.84f), m);
                    Ring(h, new Vector3(0, -0.14f, 0), 0.4f, 0.07f, M("FFFFFF", 0.2f), default, new Vector3(1, 1, 0.97f));
                    S(h, new Vector3(0, 0.33f, 0), 0.22f, M("FFFFFF", 0.1f));
                    hairHidden = true;
                    break;
                }
                case 4: Crown("FFC83A", "E53935", 5, 1f); break;
                case 5:
                {
                    var m = M("1E1E22", 0.3f);
                    Cy(h, new Vector3(0, -0.05f, 0), new Vector3(1.0f, 0.02f, 0.9f), m);
                    S(h, new Vector3(0, 0.02f, 0), new Vector3(0.62f, 0.36f, 0.58f), m);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = 90 + k * 120;
                        var d = Quaternion.Euler(0, -a, 0) * Vector3.right;
                        B(h, d * 0.38f + Vector3.up * 0.04f, new Vector3(0.5f, 0.16f, 0.03f), m, new Vector3(-10, -a + 90, 0));
                    }
                    S(h, new Vector3(0, 0.06f, 0.36f), 0.1f, M("FFFFFF"));
                    Ring(h, new Vector3(0, -0.02f, 0), 0.47f, 0.015f, M("FFC83A", 0.8f, 0.8f), default, new Vector3(1, 1, 0.9f));
                    hairHidden = true;
                    break;
                }
                case 6:
                {
                    var m = M("2E4BD9", 0.3f);
                    hairHidden = true;
                    Cy(h, new Vector3(0, -0.06f, 0), new Vector3(0.98f, 0.012f, 0.98f), m);
                    var cone = Node("Cone", h, new Vector3(0, -0.06f, 0), new Vector3(-12, 0, 0));
                    Cone(cone, Vector3.zero, new Vector3(0.6f, 0.8f, 0.6f), m);
                    Ring(cone, new Vector3(0, 0.05f, 0), 0.28f, 0.03f, M("FFC83A", 0.8f, 0.7f));
                    for (int k = 0; k < 4; k++)
                        Msh(Meshes.Star(), cone, new Vector3(Mathf.Sin(k * 1.7f) * 0.13f, 0.18f + k * 0.12f, 0.22f - k * 0.05f), Vector3.one * (0.1f - k * 0.012f), G("FFE14D", 1.8f), new Vector3(-15, 0, k * 20));
                    break;
                }
                case 7:
                {
                    var m = M("FFC400", 0.75f);
                    S(h, new Vector3(0, -0.1f, 0), new Vector3(0.9f, 0.62f, 0.88f), m);
                    Cy(h, new Vector3(0, -0.15f, 0.04f), new Vector3(1.0f, 0.015f, 1.04f), m);
                    B(h, new Vector3(0, 0.18f, 0), new Vector3(0.07f, 0.07f, 0.66f), m, new Vector3(0, 0, 0));
                    hairHidden = true;
                    break;
                }
                // ---- premium ----
                case 100: Crown("C62828", "FFC400", 6, 1.05f, G("FF6A00", 2f)); break;
                case 101:
                {
                    var ice = Glass("9FE8FF", 0.6f);
                    Cy(h, new Vector3(0, -0.02f, 0), new Vector3(0.58f, 0.05f, 0.58f), M("DDF6FF", 0.9f, 0.3f));
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k * Mathf.PI * 2 / 7;
                        var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.26f;
                        Cone(h, p, new Vector3(0.1f, 0.18f + (k % 2) * 0.12f, 0.1f), ice);
                    }
                    var gem = Msh(Meshes.Star(6), h, new Vector3(0, 0.08f, 0.3f), Vector3.one * 0.12f, G("BFF3FF", 2f)); Add<Spin>(gem).axis = Vector3.forward;
                    break;
                }
                case 102:
                {
                    S(HeadC, Vector3.zero, Vector3.one * 1.08f, Glass("CFEFFF", 0.22f));
                    Ring(HeadC, new Vector3(0, -0.42f, 0), 0.34f, 0.08f, M("F2F4F7", 0.6f));
                    B(HeadC, new Vector3(0.28f, -0.36f, 0.2f), new Vector3(0.12f, 0.08f, 0.04f), G("35E0FF", 1.5f), new Vector3(0, 40, 0));
                    break;
                }
                case 104:
                {
                    var dark = M("1E2230", 0.8f, 0.6f);
                    S(h, new Vector3(0, -0.08f, 0), new Vector3(0.88f, 0.56f, 0.86f), dark);
                    Cy(h, new Vector3(0, -0.17f, 0), new Vector3(0.98f, 0.02f, 0.98f), dark);
                    foreach (var sd in new[] { -1, 1 })
                        B(h, new Vector3(sd * 0.16f, 0.2f, 0.28f), new Vector3(0.05f, 0.34f, 0.03f), G("26F0FF", 2.2f), new Vector3(-20, 0, -sd * 30));
                    hairHidden = true;
                    break;
                }
                case 105:
                {
                    var g = M("FFC83A", 0.9f, 0.85f);
                    Cy(h, new Vector3(0, -0.04f, 0), new Vector3(0.78f, 0.012f, 0.78f), g);
                    Cy(h, new Vector3(0, 0.16f, 0), new Vector3(0.48f, 0.2f, 0.48f), g);
                    Cy(h, new Vector3(0, 0.0f, 0), new Vector3(0.49f, 0.04f, 0.49f), M("1A1A1A"));
                    hairHidden = true;
                    break;
                }
                case 106:
                {
                    var horn = Cone(h, new Vector3(0, -0.04f, 0.22f), new Vector3(0.13f, 0.42f, 0.13f), M("FFF4FB", 0.8f), new Vector3(28, 0, 0));
                    Add<Hue>(horn).sat = 0.3f;
                    Ring(h, new Vector3(0, -0.02f, 0.22f), 0.07f, 0.02f, M("FFC83A", 0.8f, 0.8f), new Vector3(28, 0, 0));
                    break;
                }
                case 107:
                {
                    S(h, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.32f, 0.5f), M("9C0D2E", 0.2f));
                    Crown("FFD700", "2E7DFF", 8, 1.15f);
                    Ring(h, new Vector3(0, -0.04f, 0), 0.33f, 0.05f, M("FFFFFF", 0.1f));
                    var top = Msh(Meshes.Star(), h, new Vector3(0, 0.32f, 0), Vector3.one * 0.14f, G("FFE14D", 2f)); Add<Spin>(top);
                    break;
                }
            }
            if (set == 2) { hairHidden = true; earsHidden = true; }
        }
    }
}
