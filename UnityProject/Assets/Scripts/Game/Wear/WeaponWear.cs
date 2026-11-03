using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Weapons and tools held in the right hand, weapon paint.</summary>
    public partial class ChubrikModel
    {
        // ================= weapon =================

        public Material WeaponMat(string fallbackHex, float smooth = 0.6f, float metal = 0.5f)
        {
            int c = look.Idx("wcolor");
            switch (c)
            {
                case 0: return Mats.Tex(Patterns.Skin(3, Hex("3050FF")), Color.white, 0.6f);
                case 1: return M("FFC83A", 0.95f, 0.95f);
                case 2: return G("2E8BFF", 1.4f);
                case 3: return M("8A4B22", 0.1f, 0.3f);
                case 4: return Tex.SkinMat("flames");
                case 5: return Mats.Tex(Patterns.Skin(7, Color.black), Color.white, 0.8f, 0.3f);
                case 6: return Tex.SkinMat("rainbow");
                case 7: return Glass("BFEFFF", 0.7f);
                default: return M(fallbackHex, smooth, metal);
            }
        }

        void WeaponBuild(int i, int set)
        {
            WeaponId = look.Of("weapon");
            Weapon = Node("Weapon", HandR, new Vector3(0, -0.04f, 0.02f), new Vector3(90, 0, 0));
            var w = Weapon;
            var main = WeaponMat("6B7685");
            var dark = M("2A2E36", 0.5f, 0.3f);
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                    B(w, new Vector3(0, 0, 0.12f), new Vector3(0.1f, 0.13f, 0.3f), main);
                    Cy(w, new Vector3(0, 0.02f, 0.32f), new Vector3(0.05f, 0.08f, 0.05f), dark, new Vector3(90, 0, 0));
                    S(w, new Vector3(0, 0.02f, 0.41f), 0.06f, G("35E0FF", 2.5f));
                    B(w, new Vector3(0, -0.09f, 0.02f), new Vector3(0.07f, 0.14f, 0.07f), dark, new Vector3(-15, 0, 0));
                    break;
                case 1:
                    B(w, new Vector3(0, 0, 0.18f), new Vector3(0.05f, 0.03f, 0.4f), main);
                    Ring(w, new Vector3(0, 0, 0.42f), 0.06f, 0.025f, main, new Vector3(0, 0, 90));
                    break;
                case 2:
                case 101:
                {
                    var stick = set == 1 ? M("BFEFFF", 0.8f) : WeaponMat("6D4425", 0.3f, 0);
                    Rod(w, new Vector3(0, 0, -0.25f), new Vector3(0, 0, 0.65f), 0.05f, stick);
                    if (set == 1) for (int k = 0; k < 5; k++) Spike(w, new Vector3(0, 0, 0.62f), new Vector3(Mathf.Cos(k * 1.26f) * 0.12f, Mathf.Sin(k * 1.26f) * 0.12f, 0.8f), 0.05f, G("9FE8FF", 2f));
                    else { var orb = S(w, new Vector3(0, 0, 0.7f), 0.14f, G("B57CFF", 2.5f)); Add<Pulse>(orb); Ring(w, new Vector3(0, 0, 0.66f), 0.07f, 0.015f, M("FFC83A", 0.8f, 0.8f), new Vector3(90, 0, 0)); }
                    break;
                }
                case 3:
                case 103:
                case 104:
                {
                    Cy(w, new Vector3(0, 0, 0.0f), new Vector3(0.05f, 0.08f, 0.05f), dark, new Vector3(90, 0, 0));
                    string col = set == 3 ? "6B2BFF" : set == 4 ? "FF2EB4" : "35E0FF";
                    if (set == 3 || set == 4)
                    {
                        Cy(w, new Vector3(0, 0, 0.09f), new Vector3(0.12f, 0.01f, 0.12f), M("FFC83A", 0.8f, 0.8f), new Vector3(90, 0, 0));
                        B(w, new Vector3(0, 0.01f, 0.5f), new Vector3(0.02f, 0.06f, 0.8f), set == 3 ? M("1A1426", 0.95f, 0.8f) : G(col, 2f));
                        B(w, new Vector3(0, 0.045f, 0.5f), new Vector3(0.021f, 0.012f, 0.8f), G(set == 3 ? col : "26F0FF", 3f));
                    }
                    else
                    {
                        Cy(w, new Vector3(0, 0, 0.48f), new Vector3(0.045f, 0.4f, 0.045f), G(col, 2.6f), new Vector3(90, 0, 0));
                        Cy(w, new Vector3(0, 0, 0.48f), new Vector3(0.08f, 0.41f, 0.08f), Add(col, 0.35f), new Vector3(90, 0, 0));
                    }
                    break;
                }
                case 4:
                {
                    var wood = WeaponMat("8D5524", 0.2f, 0);
                    B(w, new Vector3(0, 0, 0.05f), new Vector3(0.05f, 0.05f, 0.18f), wood);
                    foreach (var sd in new[] { -1, 1 }) B(w, new Vector3(sd * 0.06f, 0, 0.2f), new Vector3(0.05f, 0.05f, 0.16f), wood, new Vector3(0, sd * 20, 0));
                    Rod(w, new Vector3(-0.09f, 0, 0.28f), new Vector3(0.09f, 0, 0.28f), 0.012f, M("E53935"));
                    B(w, new Vector3(0, 0, 0.27f), new Vector3(0.05f, 0.05f, 0.05f), M("FF3FA4"));
                    break;
                }
                case 5:
                {
                    var bm = WeaponMat("FF7A1E", 0.4f, 0);
                    Msh(Meshes.Fan("boom", new[] { new Vector2(-0.3f, 0.05f), new Vector2(0, 0.2f), new Vector2(0.3f, 0.05f), new Vector2(0.28f, -0.03f), new Vector2(0, 0.1f), new Vector2(-0.28f, -0.03f) }, 0.04f), w, new Vector3(0, 0, 0.18f), Vector3.one, bm, new Vector3(90, 0, 0));
                    break;
                }
                case 6:
                    Rod(w, new Vector3(0, 0, -0.08f), new Vector3(0, 0, 0.4f), 0.03f, M("FFFFFF"));
                    Cy(w, new Vector3(0, 0, 0.55f), new Vector3(0.32f, 0.03f, 0.32f), Mats.Tex(Patterns.Swirl(), look.Idx("wcolor") >= 0 ? (Color)WeaponMat("FF4FA0").color : Color.white, 0.6f), new Vector3(0, 90, 90));
                    break;
                case 7:
                    B(w, new Vector3(0, 0, 0.0f), new Vector3(0.05f, 0.06f, 0.14f), dark);
                    B(w, new Vector3(0, 0.01f, 0.17f), new Vector3(0.015f, 0.06f, 0.22f), main);
                    B(w, new Vector3(0, 0.045f, 0.17f), new Vector3(0.016f, 0.01f, 0.22f), G("FF3FA4", 2.5f));
                    break;
                case 107:
                {
                    var gold = WeaponMat("FFD700", 0.95f, 0.95f);
                    Rod(w, new Vector3(0, 0, -0.2f), new Vector3(0, 0, 0.55f), 0.045f, gold);
                    S(w, new Vector3(0, 0, 0.62f), 0.15f, G("E53935", 1.6f));
                    for (int k = 0; k < 5; k++) Spike(w, new Vector3(Mathf.Cos(k * 1.26f) * 0.06f, Mathf.Sin(k * 1.26f) * 0.06f, 0.66f), new Vector3(Mathf.Cos(k * 1.26f) * 0.08f, Mathf.Sin(k * 1.26f) * 0.08f, 0.8f), 0.04f, gold);
                    break;
                }
            }
        }
    }
}
