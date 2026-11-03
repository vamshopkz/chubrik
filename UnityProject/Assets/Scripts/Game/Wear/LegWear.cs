using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Shorts and trousers, shoes.</summary>
    public partial class ChubrikModel
    {
        void Pants(int i, int set)
        {
            var p = Node("Pants", Torso, Vector3.zero);
            void Wear(Material m, bool shorts, float fat = 1f)
            {
                S(p, new Vector3(0, 0.1f, 0), new Vector3(0.69f, 0.36f, 0.6f), m);
                foreach (var g in new[] { LegL, LegR })
                {
                    if (shorts) Ca(g, new Vector3(0, -0.06f, 0), new Vector3(0.21f * fat, 0.09f, 0.21f * fat), m);
                    else Ca(g, new Vector3(0, -0.13f, 0), new Vector3(0.2f * fat, 0.15f, 0.2f * fat), m);
                }
            }
            switch (i)
            {
                case 0: Wear(M("4FC3F7", 0.2f), true); break;
                case 1:
                    Wear(M("8C939C", 0.1f), false);
                    foreach (var g in new[] { LegL, LegR }) B(g, new Vector3(g == LegL ? -0.1f : 0.1f, -0.13f, 0), new Vector3(0.012f, 0.26f, 0.05f), M("FFFFFF"));
                    break;
                case 2: Wear(Mats.Tex(Patterns.Denim(), Color.white, 0.15f), false); break;
                case 3:
                    Wear(M("A1885A", 0.15f), false, 1.05f);
                    foreach (var g in new[] { LegL, LegR }) B(g, new Vector3(g == LegL ? -0.1f : 0.1f, -0.12f, 0), new Vector3(0.05f, 0.09f, 0.1f), M("8C744A", 0.15f));
                    break;
                case 4: Wear(Mats.Tex(Patterns.Floral(), Color.white, 0.2f), true, 1.1f); break;
                case 5:
                {
                    var plaid = Mats.Tex(Patterns.Plaid("C62828", "1A1A1A", "FFD23A"), Color.white, 0.2f);
                    S(p, new Vector3(0, 0.1f, 0), new Vector3(0.69f, 0.36f, 0.6f), plaid);
                    Cy(p, new Vector3(0, 0.02f, 0), new Vector3(0.74f, 0.1f, 0.66f), plaid);
                    break;
                }
                case 6:
                    Wear(M("15151C", 0.6f), false);
                    foreach (var g in new[] { LegL, LegR })
                        for (int k = 0; k < 2; k++) Ring(g, new Vector3(0, -0.06f - k * 0.1f, 0), 0.1f, 0.012f, G(k == 0 ? "FF3FA4" : "26F0FF", 2.5f));
                    break;
                default: Wear(M("3C4656", 0.2f), false); break;
            }
        }

        void Shoes(int i, int set)
        {
            foreach (var f in new[] { FootL, FootR })
            {
                switch (set >= 0 ? 100 + set : i)
                {
                    case 0:
                        S(f, Vector3.zero, new Vector3(0.21f, 0.13f, 0.28f), M("FAFAFA", 0.3f));
                        B(f, new Vector3(0, -0.05f, 0), new Vector3(0.2f, 0.035f, 0.27f), M("E53935"));
                        B(f, new Vector3(f == FootL ? -0.1f : 0.1f, 0.01f, 0), new Vector3(0.01f, 0.03f, 0.14f), M("1E88E5"));
                        break;
                    case 1:
                        S(f, Vector3.zero, new Vector3(0.2f, 0.13f, 0.27f), M("FF3FA4", 0.3f));
                        S(f, new Vector3(0, -0.01f, 0.08f), new Vector3(0.18f, 0.1f, 0.12f), M("FFFFFF"));
                        B(f, new Vector3(0, -0.05f, 0), new Vector3(0.19f, 0.03f, 0.27f), M("FFFFFF"));
                        break;
                    case 2:
                        Cy(f, new Vector3(0, 0.07f, -0.03f), new Vector3(0.19f, 0.07f, 0.19f), M("4A3020", 0.4f));
                        S(f, Vector3.zero, new Vector3(0.23f, 0.15f, 0.3f), M("4A3020", 0.4f));
                        B(f, new Vector3(0, -0.06f, 0), new Vector3(0.23f, 0.05f, 0.3f), M("151515"));
                        break;
                    case 3:
                        Cy(f, new Vector3(0, 0.07f, -0.03f), new Vector3(0.17f, 0.07f, 0.17f), M("FFFFFF", 0.05f));
                        S(f, new Vector3(0, -0.01f, 0), new Vector3(0.17f, 0.1f, 0.24f), M("FFFFFF", 0.05f));
                        B(f, new Vector3(0, -0.06f, 0.01f), new Vector3(0.2f, 0.025f, 0.29f), M("8D5524", 0.3f));
                        B(f, new Vector3(0, -0.02f, 0.06f), new Vector3(0.2f, 0.02f, 0.04f), M("8D5524", 0.3f));
                        break;
                    case 4:
                        S(f, Vector3.zero, new Vector3(0.25f, 0.17f, 0.31f), M("C9D1DB", 0.85f, 0.8f));
                        Ring(f, new Vector3(0, 0.05f, -0.03f), 0.1f, 0.02f, M("E53935"));
                        B(f, new Vector3(0, -0.07f, 0), new Vector3(0.22f, 0.025f, 0.28f), G("35A8FF", 2f));
                        break;
                    case 5:
                    {
                        S(f, Vector3.zero, new Vector3(0.21f, 0.13f, 0.28f), M("FAFAFA", 0.3f));
                        var sole = B(f, new Vector3(0, -0.05f, 0), new Vector3(0.2f, 0.035f, 0.27f), G("FF3FA4", 2f));
                        var hu = Add<Hue>(sole); hu.glow = 2.2f; hu.speed = 0.6f; hu.offset = f == FootL ? 0 : 0.5f;
                        break;
                    }
                    case 6:
                        Cy(f, new Vector3(0, 0.09f, -0.03f), new Vector3(0.2f, 0.1f, 0.2f), M("8A8F98", 0.05f));
                        S(f, Vector3.zero, new Vector3(0.22f, 0.14f, 0.29f), M("8A8F98", 0.05f));
                        Ring(f, new Vector3(0, 0.14f, -0.03f), 0.1f, 0.012f, G("26F0FF", 2.5f));
                        Ring(f, new Vector3(0, 0.03f, -0.03f), 0.11f, 0.012f, G("26F0FF", 2.5f));
                        break;
                    case 7:
                        S(f, Vector3.zero, new Vector3(0.25f, 0.15f, 0.31f), M("8D6E63", 0.05f));
                        foreach (var sd in new[] { -1, 1 }) S(f, new Vector3(sd * 0.06f, 0.07f, 0.08f), 0.07f, M("8D6E63", 0.05f));
                        foreach (var sd in new[] { -1, 1 }) S(f, new Vector3(sd * 0.04f, 0.04f, 0.14f), 0.035f, M("111111", 0.9f));
                        break;
                    case 102:
                        S(f, new Vector3(0, 0.02f, 0), new Vector3(0.28f, 0.2f, 0.33f), M("F2F4F7", 0.4f));
                        B(f, new Vector3(0, -0.07f, 0), new Vector3(0.27f, 0.05f, 0.33f), M("8A939E", 0.6f));
                        Ring(f, new Vector3(0, 0.08f, -0.02f), 0.11f, 0.025f, M("1E63D9"));
                        break;
                    default:
                        S(f, Vector3.zero, new Vector3(0.2f, 0.12f, 0.26f), Skin);
                        break;
                }
            }
        }
    }
}
