using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Wings and shoulder pads, backpacks, tails.</summary>
    public partial class ChubrikModel
    {
        void Wings(int i, int set)
        {
            var w = Node("Wings", Torso, new Vector3(0, 0.36f, -0.25f));
            Vector2[] angel = { new Vector2(0, 0), new Vector2(0.15f, 0.18f), new Vector2(0.4f, 0.32f), new Vector2(0.62f, 0.36f), new Vector2(0.72f, 0.26f), new Vector2(0.62f, 0.13f), new Vector2(0.67f, 0.02f), new Vector2(0.55f, -0.05f), new Vector2(0.57f, -0.16f), new Vector2(0.43f, -0.19f), new Vector2(0.41f, -0.3f), new Vector2(0.26f, -0.27f), new Vector2(0.1f, -0.12f) };
            Vector2[] dragon = { new Vector2(0, 0), new Vector2(0.2f, 0.26f), new Vector2(0.5f, 0.44f), new Vector2(0.78f, 0.46f), new Vector2(0.7f, 0.22f), new Vector2(0.64f, 0.04f), new Vector2(0.52f, 0.14f), new Vector2(0.43f, -0.1f), new Vector2(0.31f, 0.02f), new Vector2(0.2f, -0.2f), new Vector2(0.08f, -0.05f) };
            void Pair(Vector2[] shape, string key, Material m, float scale = 1f, Material inner = null, bool rods = false)
            {
                foreach (var sd in new[] { -1f, 1f })
                {
                    var piv = Node("Wing", w, new Vector3(sd * 0.08f, 0, 0));
                    var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.baseYaw = 18;
                    var holder = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
                    Msh(Meshes.Wing(key, shape), holder, Vector3.zero, new Vector3(scale, scale, 1), m);
                    if (inner != null) Msh(Meshes.Wing(key, shape), holder, new Vector3(0.04f, 0.03f, -0.02f * sd), new Vector3(scale * 0.7f, scale * 0.7f, 1), inner);
                    if (rods)
                    {
                        var bone = M("3A1010", 0.4f);
                        Rod(holder, Vector3.zero, new Vector3(0.78f, 0.46f, 0) * scale, 0.03f, bone);
                        Rod(holder, new Vector3(0.5f, 0.44f, 0) * scale, new Vector3(0.43f, -0.1f, 0) * scale, 0.02f, bone);
                        Rod(holder, new Vector3(0.3f, 0.36f, 0) * scale, new Vector3(0.2f, -0.2f, 0) * scale, 0.02f, bone);
                    }
                }
                CanGlide = true;
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Pair(angel, "angel", M("FFFFFF", 0.2f), 1f, M("EEF4FF", 0.2f)); break;
                case 1: Pair(dragon, "dragon", M("8E1B1B", 0.4f), 1f, null, true); break;
                case 2:
                case 3:
                {
                    var metal = M("A8B3C2", 0.8f, 0.7f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var sh = Node("Shoulder", Torso, new Vector3(sd * 0.3f, 0.42f, 0));
                        if (i == 2) { S(sh, Vector3.zero, new Vector3(0.26f, 0.16f, 0.26f), metal); Ring(sh, new Vector3(0, -0.02f, 0), 0.12f, 0.02f, M("FFC83A", 0.8f, 0.8f)); }
                        else for (int k = 0; k < 3; k++) Spike(sh, new Vector3(sd * 0.02f, 0, -0.08f + k * 0.08f), new Vector3(sd * 0.08f, 0.2f - Mathf.Abs(k - 1) * 0.05f, -0.08f + k * 0.08f), 0.08f, metal);
                    }
                    break;
                }
                case 4: Pair(angel, "angel", Add("35E0FF", 0.75f), 1.1f, Add("B57CFF", 0.6f)); break;
                case 5:
                {
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", w, new Vector3(sd * 0.1f, 0, 0));
                        var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.baseYaw = 15;
                        var holder = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
                        for (int k = 0; k < 3; k++) S(holder, new Vector3(0.1f + k * 0.08f, 0.05f - k * 0.04f, 0), 0.18f - k * 0.03f, M("FFB3D9", 0.05f));
                    }
                    CanGlide = true;
                    break;
                }
                case 6:
                {
                    var metal = M("C9D1DB", 0.85f, 0.8f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        B(w, new Vector3(sd * 0.32f, 0.02f, -0.02f), new Vector3(0.52f, 0.04f, 0.2f), metal, new Vector3(0, 0, sd * 18));
                        Cy(w, new Vector3(sd * 0.16f, -0.1f, -0.06f), new Vector3(0.09f, 0.12f, 0.09f), M("555F6B", 0.7f, 0.6f));
                        var fl = Cone(w, new Vector3(sd * 0.16f, -0.22f, -0.06f), new Vector3(0.08f, 0.22f, 0.08f), G("FF8A1E", 2.4f), new Vector3(180, 0, 0));
                        Add<Pulse>(fl).amount = 0.2f;
                        fl.SetActive(false); JetFlames.Add(fl);
                    }
                    CanGlide = true; HasJet = true;
                    break;
                }
                case 7:
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var sh = Node("Lamp", Torso, new Vector3(sd * 0.27f, 0.47f, 0.02f));
                        Cy(sh, Vector3.zero, new Vector3(0.1f, 0.06f, 0.1f), M("333840", 0.6f, 0.5f), new Vector3(70, 0, 0));
                        Cy(sh, new Vector3(0, 0.02f, 0.06f), new Vector3(0.08f, 0.01f, 0.08f), G("FFF2A0", 3f), new Vector3(70, 0, 0));
                    }
                    break;
                case 100: Pair(angel, "angel", G("FF6A00", 1.6f), 1.15f, G("FFD23A", 2f)); break;
                case 104:
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", w, new Vector3(sd * 0.08f, 0, 0));
                        var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.baseYaw = 20; f.idleAmp = 3;
                        for (int k = 0; k < 3; k++)
                            B(piv, new Vector3(sd * (0.28f + k * 0.03f), 0.08f + k * 0.08f, 0), new Vector3(0.58f, 0.04f, 0.02f), G("26F0FF", 2.5f), new Vector3(0, 0, sd * (15 + k * 18)));
                    }
                    CanGlide = true;
                    break;
                case 106: Pair(angel, "angel", M("FFFFFF", 0.3f), 1.1f, Add("FF9AE6", 0.5f)); break;
            }
        }

        void Back(int i, int set)
        {
            var b = Node("Back", Torso, new Vector3(0, 0.28f, -0.28f));
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    var blue = M("1E63D9", 0.3f);
                    B(b, new Vector3(0, 0, -0.08f), new Vector3(0.4f, 0.42f, 0.18f), blue);
                    B(b, new Vector3(0, -0.08f, -0.18f), new Vector3(0.3f, 0.16f, 0.06f), M("FFC400", 0.3f));
                    foreach (var sd in new[] { -1, 1 }) B(Torso, new Vector3(sd * 0.15f, 0.4f, 0.12f), new Vector3(0.05f, 0.04f, 0.5f), M("173F8A"), new Vector3(-30, 0, 0));
                    break;
                }
                case 1:
                {
                    var metal = M("C9D1DB", 0.85f, 0.8f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Cy(b, new Vector3(sd * 0.11f, 0, -0.08f), new Vector3(0.16f, 0.2f, 0.16f), metal);
                        S(b, new Vector3(sd * 0.11f, 0.2f, -0.08f), 0.16f, M("E53935", 0.6f));
                        Cone(b, new Vector3(sd * 0.11f, -0.2f, -0.08f), new Vector3(0.12f, 0.08f, 0.12f), M("555F6B", 0.7f, 0.6f), new Vector3(180, 0, 0));
                        var fl = Cone(b, new Vector3(sd * 0.11f, -0.27f, -0.08f), new Vector3(0.11f, 0.32f, 0.11f), G("FF8A1E", 2.4f), new Vector3(180, 0, 0));
                        Add<Pulse>(fl).amount = 0.2f;
                        fl.SetActive(false); JetFlames.Add(fl);
                    }
                    HasJet = true;
                    break;
                }
                case 2:
                {
                    var sh = Node("Shield", b, new Vector3(0, -0.02f, -0.04f), new Vector3(90, 0, 0));
                    Cy(sh, Vector3.zero, new Vector3(0.52f, 0.025f, 0.52f), M("8D5524", 0.3f));
                    Ring(sh, Vector3.zero, 0.25f, 0.025f, M("B0BAC6", 0.8f, 0.8f));
                    Msh(Meshes.Star(), sh, new Vector3(0, -0.03f, 0), new Vector3(0.22f, 0.22f, 0.04f), M("FFC83A", 0.8f, 0.8f), new Vector3(90, 0, 0));
                    break;
                }
                case 3:
                {
                    var g = Node("Guitar", b, new Vector3(0, -0.05f, -0.06f), new Vector3(0, 0, 35));
                    var red = M("D32F2F", 0.8f, 0.1f);
                    S(g, new Vector3(0, -0.12f, 0), new Vector3(0.34f, 0.32f, 0.1f), red);
                    S(g, new Vector3(0, 0.06f, 0), new Vector3(0.26f, 0.24f, 0.1f), red);
                    B(g, new Vector3(0, 0.36f, 0), new Vector3(0.06f, 0.42f, 0.04f), M("4A2A12", 0.4f));
                    B(g, new Vector3(0, 0.6f, 0), new Vector3(0.1f, 0.1f, 0.04f), M("1A1A1A"));
                    S(g, new Vector3(0, -0.06f, -0.05f), new Vector3(0.08f, 0.08f, 0.02f), M("1A1A1A"));
                    break;
                }
                case 4:
                {
                    var slime = S(b, new Vector3(0, 0, -0.1f), new Vector3(0.42f, 0.42f, 0.3f), Glass("6EDC3C", 0.6f));
                    Add<Pulse>(slime).amount = 0.06f;
                    for (int k = 0; k < 4; k++) S(slime.transform, new Vector3(Mathf.Sin(k * 2.1f) * 0.2f, Mathf.Cos(k * 1.3f) * 0.2f, -0.2f), 0.12f, G("C6FF8A", 0.8f));
                    break;
                }
                case 5:
                {
                    B(b, new Vector3(0, 0, -0.08f), new Vector3(0.34f, 0.36f, 0.16f), M("F57C00", 0.3f));
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("MiniWing", b, new Vector3(sd * 0.16f, 0.05f, -0.1f));
                        var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.idleAmp = 15;
                        var holder = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
                        S(holder, new Vector3(0.14f, 0.04f, 0), new Vector3(0.28f, 0.14f, 0.03f), M("FFFFFF", 0.2f));
                    }
                    break;
                }
                case 6:
                {
                    Cy(b, new Vector3(0, 0, -0.1f), new Vector3(0.24f, 0.2f, 0.24f), M("F2F4F7", 0.7f, 0.2f));
                    S(b, new Vector3(0, 0.2f, -0.1f), 0.24f, M("F2F4F7", 0.7f, 0.2f));
                    Ring(b, new Vector3(0, 0, -0.1f), 0.125f, 0.015f, M("E53935"));
                    Rod(b, new Vector3(0.1f, 0.25f, -0.05f), new Vector3(0.12f, 0.45f, 0.15f), 0.03f, M("555F6B", 0.6f));
                    break;
                }
                case 7:
                    Ring(Torso, new Vector3(0, 0.1f, 0), 0.3f, 0.02f, M("222222"), default, new Vector3(1, 1, 0.88f));
                    S(Torso, new Vector3(0.05f, 0.1f, 0.27f), new Vector3(0.3f, 0.13f, 0.1f), M("FFC400", 0.4f));
                    B(Torso, new Vector3(0.05f, 0.12f, 0.32f), new Vector3(0.2f, 0.01f, 0.01f), M("333333"));
                    break;
                case 102:
                {
                    var r = Node("Rocket", b, new Vector3(0, 0, -0.14f));
                    Cy(r, new Vector3(0, 0, 0), new Vector3(0.22f, 0.24f, 0.22f), M("F2F4F7", 0.7f, 0.2f));
                    Cone(r, new Vector3(0, 0.24f, 0), new Vector3(0.22f, 0.24f, 0.22f), M("E53935", 0.6f));
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 120;
                        var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        B(r, d * 0.12f + Vector3.down * 0.18f, new Vector3(0.02f, 0.14f, 0.1f), M("E53935"), new Vector3(0, a, 0));
                    }
                    var fl = Cone(r, new Vector3(0, -0.24f, 0), new Vector3(0.16f, 0.4f, 0.16f), G("FF8A1E", 2.4f), new Vector3(180, 0, 0));
                    Add<Pulse>(fl).amount = 0.2f;
                    fl.SetActive(false); JetFlames.Add(fl);
                    HasJet = true;
                    break;
                }
            }
        }

        void Tail(int i, int set)
        {
            var root = Node("Tail", Torso, new Vector3(0, 0.08f, -0.27f), new Vector3(-120, 0, 0));
            Transform Chain(int n, float step, float bend, System.Action<Transform, int> seg, float amp = 10)
            {
                var cur = root;
                for (int k = 0; k < n; k++)
                {
                    var nd = Node("Seg" + k, cur, k == 0 ? Vector3.zero : new Vector3(0, step, 0), new Vector3(bend, 0, 0));
                    seg(nd, k);
                    var sw = Add<Sway>(nd.gameObject); sw.model = this; sw.amp = amp; sw.phase = k * 0.6f; sw.lift = -6;
                    cur = nd;
                }
                return cur;
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Chain(6, 0.08f, 12, (t, k) => S(t, new Vector3(0, 0.04f, 0), 0.14f - k * 0.008f, Skin)); break;
                case 1:
                {
                    var b = Msh(Meshes.Bolt(), root, new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.6f, 0.6f), G("FFE14D", 2.4f), new Vector3(0, 90, 0));
                    var sw = Add<Sway>(root.gameObject); sw.model = this; sw.amp = 8;
                    Add<Hue>(b).glow = 2f;
                    break;
                }
                case 2:
                {
                    var metal = M("9AA5B4", 0.8f, 0.7f);
                    var end = Chain(6, 0.08f, 10, (t, k) => { Cy(t, new Vector3(0, 0.04f, 0), new Vector3(0.09f - k * 0.006f, 0.035f, 0.09f - k * 0.006f), metal); S(t, Vector3.zero, 0.06f, M("555F6B", 0.6f, 0.6f)); }, 6);
                    foreach (var sd in new[] { -1, 1 }) B(end, new Vector3(sd * 0.03f, 0.12f, 0), new Vector3(0.02f, 0.1f, 0.03f), metal, new Vector3(0, 0, sd * 20));
                    break;
                }
                case 3:
                case 100:
                {
                    var m = set == 0 ? G("FF5A1F", 1.4f) : M("2E7D32", 0.5f);
                    var spike = set == 0 ? G("FFD23A", 2f) : M("C6FF8A", 0.4f);
                    var end = Chain(6, 0.085f, 8, (t, k) =>
                    {
                        float d = 0.15f - k * 0.017f;
                        S(t, new Vector3(0, 0.04f, 0), new Vector3(d, 0.12f, d), m);
                        Spike(t, new Vector3(0, 0.04f, -d * 0.4f), new Vector3(0, 0.06f, -d * 0.4f - 0.08f), 0.06f, spike);
                    });
                    Cone(end, new Vector3(0, 0.08f, 0), new Vector3(0.1f, 0.14f, 0.05f), spike);
                    break;
                }
                case 4:
                {
                    var t = Node("Fox", root, Vector3.zero, new Vector3(15, 0, 0));
                    S(t, new Vector3(0, 0.22f, 0), new Vector3(0.22f, 0.42f, 0.22f), M("FF7A1E", 0.1f));
                    S(t, new Vector3(0, 0.44f, 0), new Vector3(0.16f, 0.16f, 0.16f), M("FFFFFF", 0.1f));
                    var sw = Add<Sway>(t.gameObject); sw.model = this; sw.amp = 14;
                    break;
                }
                case 5: Chain(10, 0.07f, 8, (t, k) => S(t, new Vector3(0, 0.03f, 0), 0.07f - k * 0.004f, G("FF3FA4", 2.2f)), 14); break;
                case 6:
                {
                    var metal = M("B9C3CF", 0.85f, 0.8f);
                    var end = Chain(5, 0.09f, 10, (t, k) => B(t, new Vector3(0, 0.045f, 0), new Vector3(0.1f - k * 0.01f, 0.08f, 0.1f - k * 0.01f), metal), 7);
                    var led = S(end, new Vector3(0, 0.12f, 0), 0.07f, G("35E0FF", 2.5f)); Add<Pulse>(led);
                    break;
                }
                case 7: S(root, new Vector3(0, 0.03f, 0), 0.16f, Skin); break;
                case 106:
                {
                    string[] rb = { "FF3B3B", "FF9A2E", "FFE14D", "6EDC3C", "35A8FF", "5B5BFF", "B57CFF" };
                    Chain(7, 0.075f, 10, (t, k) => S(t, new Vector3(0, 0.04f, 0), 0.14f - k * 0.01f, G(rb[k], 1.2f)));
                    break;
                }
            }
        }
    }
}
