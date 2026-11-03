using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Hoodies and jackets (with sleeves), gloves and paws, wrist items, belts.</summary>
    public partial class ChubrikModel
    {
        void Sleeves(Material m, float len, float fat = 1f)
        {
            foreach (var a in new[] { ArmL, ArmR })
                Ca(a, new Vector3(0, -0.04f - len * 0.5f, 0), new Vector3(0.19f * fat, Mathf.Max(0.1f, len * 0.5f + 0.06f), 0.19f * fat), m);
        }

        void Top(int i, int set)
        {
            var t = Node("Top", Torso, Vector3.zero);
            void Shell(Material m, float s = 1f) => S(t, new Vector3(0, 0.27f, 0), new Vector3(0.7f * s, 0.5f * s, 0.62f * s), m);
            void Fur(Material m, string col)
            {
                S(t, new Vector3(0, 0.23f, 0), new Vector3(0.78f, 0.62f, 0.7f), m);
                var r = new Rng(col.GetHashCode());
                for (int k = 0; k < 22; k++)
                {
                    float a = r.F(0, Mathf.PI * 2), y = r.F(-0.05f, 0.42f);
                    float rad = 0.36f * Mathf.Sqrt(Mathf.Max(0.1f, 1 - Mathf.Pow((y - 0.23f) / 0.33f, 2)));
                    S(t, new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad * 0.9f), r.F(0.1f, 0.15f), m);
                }
                Ring(t, new Vector3(0, 0.46f, 0), 0.24f, 0.07f, m);
                Sleeves(m, 0.24f, 1.25f);
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    var m = M("7E57C2", 0.15f);
                    S(t, new Vector3(0, 0.24f, 0), new Vector3(0.74f, 0.58f, 0.66f), m);
                    S(t, new Vector3(0, 0.5f, -0.22f), new Vector3(0.48f, 0.3f, 0.28f), m);
                    B(t, new Vector3(0, 0.1f, 0.31f), new Vector3(0.3f, 0.1f, 0.03f), M("6A45B0", 0.15f), new Vector3(-12, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) Rod(t, new Vector3(sd * 0.06f, 0.44f, 0.25f), new Vector3(sd * 0.07f, 0.32f, 0.31f), 0.015f, M("FFFFFF"));
                    Sleeves(m, 0.26f, 1.05f);
                    break;
                }
                case 1:
                    Shell(M("FAFAFA", 0.2f));
                    Msh(Meshes.Star(), t, new Vector3(0, 0.28f, 0.31f), new Vector3(0.2f, 0.2f, 0.05f), M("FFC400", 0.4f), new Vector3(-12, 0, 0));
                    Sleeves(M("FAFAFA", 0.2f), 0.08f);
                    break;
                case 2:
                {
                    var m = M("4E5A32", 0.3f);
                    Shell(m, 1.02f);
                    for (int k = 0; k < 3; k++) B(t, new Vector3(-0.13f + k * 0.13f, 0.18f, 0.3f), new Vector3(0.1f, 0.12f, 0.06f), M("3E4826", 0.3f), new Vector3(-10, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 0.18f, 0.44f, 0), new Vector3(0.1f, 0.03f, 0.5f), m);
                    break;
                }
                case 3:
                {
                    var m = Mats.Tex(Patterns.Sweater("C62828"), Color.white, 0.1f);
                    Shell(m);
                    Sleeves(M("C62828", 0.1f), 0.26f);
                    break;
                }
                case 4:
                    Shell(M("1E88E5", 0.3f), 0.99f);
                    B(t, new Vector3(0, 0.27f, 0.305f), new Vector3(0.08f, 0.36f, 0.02f), M("FFFFFF"), new Vector3(-10, 0, 0));
                    S(t, new Vector3(0, 0.3f, 0.31f), new Vector3(0.12f, 0.12f, 0.02f), M("FFFFFF"));
                    break;
                case 5:
                {
                    var m = M("1C1C20", 0.75f, 0.1f);
                    Shell(m, 1.02f);
                    B(t, new Vector3(0.02f, 0.27f, 0.31f), new Vector3(0.015f, 0.4f, 0.02f), M("D9DEE6", 0.9f, 0.9f), new Vector3(-10, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 0.1f, 0.42f, 0.24f), new Vector3(0.12f, 0.16f, 0.03f), m, new Vector3(-25, sd * 25, sd * 20));
                    Sleeves(m, 0.26f);
                    break;
                }
                case 6: Fur(M("FFC400", 0.05f), "y"); break;
                case 7: Fur(M("18181C", 0.05f), "b"); break;
                case 101: Fur(Mats.Tex(Patterns.Skin(1, Hex("F4F7FA")), Color.white, 0.05f), "l"); break;
                case 103:
                {
                    var m = M("1A1F3A", 0.2f);
                    Shell(m, 1.04f);
                    Ring(t, new Vector3(0, 0.1f, 0), 0.31f, 0.05f, M("7B4BFF", 0.3f), default, new Vector3(1, 1, 0.9f));
                    foreach (var sd in new[] { -1, 1 }) B(t, new Vector3(sd * 0.06f, 0.33f, 0.3f), new Vector3(0.03f, 0.3f, 0.02f), M("E8E8F0"), new Vector3(-12, 0, sd * 22));
                    Sleeves(m, 0.24f, 1.25f);
                    break;
                }
                case 107:
                {
                    var red = M("9C0D2E", 0.25f);
                    Shell(red, 1.03f);
                    var cape = Node("Mantle", t, new Vector3(0, 0.46f, -0.24f), new Vector3(10, 0, 0));
                    B(cape, new Vector3(0, -0.4f, 0), new Vector3(0.8f, 0.82f, 0.03f), red);
                    var sw = Add<Sway>(cape.gameObject); sw.model = this; sw.amp = 2; sw.lift = 30;
                    var erm = M("FFFFFF", 0.05f);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k * Mathf.PI * 2 / 14;
                        var p = new Vector3(Mathf.Cos(a) * 0.27f, 0.47f, Mathf.Sin(a) * 0.27f);
                        S(t, p, 0.14f, erm);
                        if (k % 3 == 0) S(t, p + p.normalized * 0.05f + Vector3.up * 0.02f, 0.04f, M("111111"));
                    }
                    Sleeves(red, 0.26f, 1.1f);
                    break;
                }
            }
        }

        void Hands(int i, int set)
        {
            bool bare = i < 0 && set < 0;
            foreach (var h in new[] { HandL, HandR })
            {
                if (bare) { S(h, Vector3.zero, 0.17f, Skin); continue; }
                switch (set >= 0 ? 100 + set : i)
                {
                    case 0: S(h, new Vector3(0, -0.02f, 0), 0.25f, M("E53935", 0.6f)); Cy(h, new Vector3(0, 0.08f, 0), new Vector3(0.17f, 0.04f, 0.17f), M("FFFFFF")); break;
                    case 1:
                        S(h, Vector3.zero, 0.19f, M("FFF0F4", 0.1f));
                        for (int k = -1; k <= 1; k++) S(h, new Vector3(k * 0.04f, -0.06f, 0.07f), 0.05f, M("FF8FB0"));
                        break;
                    case 2:
                        S(h, Vector3.zero, 0.18f, M("26262B", 0.4f));
                        for (int k = -1; k <= 1; k++) S(h, new Vector3(k * 0.04f, -0.08f, 0.04f), 0.05f, Skin);
                        break;
                    case 3:
                    {
                        var metal = M("9AA5B4", 0.8f, 0.7f);
                        B(h, Vector3.zero, new Vector3(0.15f, 0.13f, 0.15f), metal);
                        foreach (var sd in new[] { -1, 1 }) B(h, new Vector3(sd * 0.05f, -0.1f, 0.02f), new Vector3(0.03f, 0.1f, 0.04f), metal, new Vector3(0, 0, sd * 12));
                        S(h, new Vector3(0, 0, 0.08f), 0.03f, G("35E0FF", 2f));
                        break;
                    }
                    case 4:
                        S(h, Vector3.zero, 0.18f, M("6D4425", 0.4f));
                        Cy(h, new Vector3(0, 0.09f, 0), new Vector3(0.2f, 0.06f, 0.2f), M("5A3418", 0.4f));
                        break;
                    case 5:
                        S(h, new Vector3(0, -0.01f, 0), new Vector3(0.21f, 0.23f, 0.19f), M("1E88E5", 0.05f));
                        S(h, new Vector3(h == HandL ? 0.08f : -0.08f, 0, 0.04f), 0.08f, M("1E88E5", 0.05f));
                        Ring(h, new Vector3(0, 0.08f, 0), 0.08f, 0.03f, M("FFFFFF", 0.05f));
                        break;
                    case 6:
                        S(h, Vector3.zero, 0.19f, M("3A2A20", 0.3f));
                        for (int k = -1; k <= 1; k++) Spike(h, new Vector3(k * 0.045f, -0.06f, 0.05f), new Vector3(k * 0.05f, -0.16f, 0.1f), 0.035f, M("F5F5F0", 0.7f));
                        break;
                    case 7:
                    {
                        var g = S(h, Vector3.zero, 0.19f, G("35E0FF", 2f));
                        var hu = Add<Hue>(g); hu.glow = 2f; hu.sat = 0.6f;
                        break;
                    }
                    case 104:
                        S(h, Vector3.zero, 0.19f, M("15171F", 0.7f, 0.4f));
                        Ring(h, new Vector3(0, 0.02f, 0), 0.095f, 0.012f, G("26F0FF", 2.5f));
                        Ring(h, new Vector3(0, 0.07f, 0), 0.085f, 0.012f, G("26F0FF", 2.5f));
                        break;
                    default: S(h, Vector3.zero, 0.17f, Skin); break;
                }
            }
        }

        void Wrist(int i, int set)
        {
            var w = Node("Wrist", ArmL, new Vector3(0, -0.22f, 0));
            switch (i)
            {
                case 0: Ring(w, Vector3.zero, 0.085f, 0.02f, M("1A1A1A")); B(w, new Vector3(0, 0, 0.09f), new Vector3(0.09f, 0.08f, 0.02f), G("35A8FF", 1.6f)); break;
                case 1: Ring(w, Vector3.zero, 0.088f, 0.035f, G("FF3FA4", 2f)); break;
                case 2: Ring(w, Vector3.zero, 0.085f, 0.022f, M("FFC83A", 0.9f, 0.9f)); Ring(w, new Vector3(0, -0.04f, 0), 0.085f, 0.015f, M("FFC83A", 0.9f, 0.9f)); break;
                case 3: Ring(w, Vector3.zero, 0.085f, 0.03f, M("7A4A24", 0.4f), default, new Vector3(1, 1.6f, 1)); break;
                case 4:
                    Ring(w, Vector3.zero, 0.088f, 0.028f, M("FF7A1E", 0.1f));
                    for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; S(w, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.1f, 0.03f, M("2E7D32")); }
                    break;
                case 5:
                {
                    Ring(w, Vector3.zero, 0.085f, 0.02f, M("2A3140", 0.7f, 0.5f));
                    var holo = Cy(w, new Vector3(0, 0, 0.18f), new Vector3(0.16f, 0.004f, 0.16f), Add("35E0FF", 0.7f), new Vector3(90, 0, 0));
                    Add<Spin>(holo).speed = 120;
                    break;
                }
                case 6:
                    Ring(w, Vector3.zero, 0.088f, 0.03f, M("151515", 0.4f));
                    for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); Spike(w, d * 0.1f, d * 0.16f, 0.04f, M("D9DEE6", 0.9f, 0.9f)); }
                    break;
                default:
                {
                    Ring(w, Vector3.zero, 0.085f, 0.018f, M("FFC83A", 0.8f, 0.8f));
                    var gem = S(w, new Vector3(0, 0, 0.1f), new Vector3(0.07f, 0.09f, 0.05f), G("B57CFF", 2.2f));
                    Add<Pulse>(gem);
                    break;
                }
            }
        }

        void Belt(int i, int set)
        {
            var b = Node("Belt", Torso, new Vector3(0, 0.07f, 0));
            Vector3 front = new Vector3(0, 0, 0.27f);
            void Strap(Material m, float r = 0.03f) => Ring(b, Vector3.zero, 0.3f, r, m, default, new Vector3(1, 1, 0.88f));
            switch (i)
            {
                case 0: Strap(M("6D4425", 0.4f)); Msh(Meshes.Star(), b, front + new Vector3(0, 0, 0.02f), new Vector3(0.12f, 0.12f, 0.04f), M("FFC83A", 0.9f, 0.9f)); break;
                case 1: Strap(M("4E5A32")); for (int k = -1; k <= 1; k++) B(b, new Vector3(k * 0.16f, -0.03f, 0.25f - Mathf.Abs(k) * 0.06f), new Vector3(0.09f, 0.09f, 0.06f), M("3E4826"), new Vector3(0, k * 30, 0)); break;
                case 2:
                    Strap(M("151515", 0.4f));
                    for (int k = 0; k < 10; k++) { float a = k * Mathf.PI / 5; var d = new Vector3(Mathf.Cos(a) * 0.31f, 0, Mathf.Sin(a) * 0.27f); Spike(b, d, d * 1.18f, 0.04f, M("D9DEE6", 0.9f, 0.9f)); }
                    break;
                case 3:
                    Strap(M("1A1A1A", 0.1f), 0.045f);
                    foreach (var sd in new[] { -1, 1 }) B(b, new Vector3(0.24f + sd * 0.03f, -0.12f, 0.14f), new Vector3(0.05f, 0.2f, 0.02f), M("1A1A1A", 0.1f), new Vector3(0, -50, sd * 10));
                    break;
                case 4:
                    Strap(M("1F4FD1", 0.5f));
                    Ring(Torso, new Vector3(0, 0.28f, 0), 0.33f, 0.022f, M("1F4FD1", 0.5f), new Vector3(0, 0, 40), new Vector3(1, 1, 0.9f));
                    S(b, front, new Vector3(0.08f, 0.08f, 0.03f), M("D9DEE6", 0.9f, 0.9f));
                    break;
                case 5:
                    Strap(M("7A4A24", 0.4f));
                    B(b, new Vector3(0.2f, -0.1f, 0.18f), new Vector3(0.03f, 0.16f, 0.03f), M("8D6E63"));
                    B(b, new Vector3(0.2f, -0.02f, 0.18f), new Vector3(0.1f, 0.04f, 0.04f), M("9AA5B4", 0.8f, 0.7f));
                    B(b, new Vector3(-0.2f, -0.1f, 0.18f), new Vector3(0.025f, 0.16f, 0.025f), M("9AA5B4", 0.8f, 0.7f), new Vector3(0, 0, 10));
                    break;
                case 6:
                    for (int k = 0; k < 18; k++)
                    {
                        float a = k * Mathf.PI * 2 / 18;
                        Ring(b, new Vector3(Mathf.Cos(a) * 0.3f, 0, Mathf.Sin(a) * 0.265f), 0.03f, 0.01f, M("FFC83A", 0.9f, 0.9f), new Vector3(k % 2 * 90, -a * Mathf.Rad2Deg, 90));
                    }
                    break;
                default:
                    Strap(M("00897B", 0.1f), 0.045f);
                    foreach (var sd in new[] { -1, 1 }) S(b, new Vector3(0.22f + sd * 0.05f, 0, 0.18f), new Vector3(0.09f, 0.06f, 0.04f), M("00897B", 0.1f));
                    break;
            }
        }
    }
}
