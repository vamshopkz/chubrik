using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// The chubrik: a round cartoon hero built from primitives. Build(look) rebuilds every worn item of the
    /// 31 departments. Rig: Body (squash) → Torso → Head/HeadC, arms, legs; feet at the root origin, +Z forward.
    /// </summary>
    public class ChubrikModel : MonoBehaviour
    {
        public Look look = Look.Default();
        public Transform Body, Torso, Head, HeadC, ArmL, ArmR, HandL, HandR, LegL, LegR, FootL, FootR, EyeL, EyeR, Weapon, Mouth;
        public readonly List<GameObject> Pupils = new List<GameObject>();
        public readonly List<GameObject> JetFlames = new List<GameObject>();
        public bool CanGlide, HasJet;
        public float SizeScale = 1f;       // secret modifiers (giant / mini)
        public float flap, speed01;         // set by the animator (wings / tails react)
        public string WeaponId = "";
        public Material Skin;
        int layer;
        float faceZ = 0.38f, hatY = 0.39f, eyeY = 0.04f, eyeX = 0.14f;
        bool hairHidden, earsHidden;

        public static ChubrikModel Create(string name, Transform parent, Look look, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<ChubrikModel>();
            m.layer = layer;
            m.Build(look);
            return m;
        }

        public void Build(Look l)
        {
            look = l.Copy();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
            Pupils.Clear(); JetFlames.Clear();
            CanGlide = HasJet = false; SizeScale = 1f; hairHidden = earsHidden = false;
            Parts.Layer = layer;

            var bodyCol = BodyColor(look.Idx("color"));
            int skinIdx = look.Idx("skin");
            Skin = skinIdx >= 0 ? Mats.Tex(Patterns.Skin(skinIdx, bodyCol), Color.white, skinIdx == 7 ? 0.75f : 0.35f) : Mats.Lit(bodyCol, 0.35f);
            if (skinIdx == 6) { Skin.SetFloat("_Smoothness", 0.85f); }

            Body = Node("Body", transform, Vector3.zero);
            Torso = Node("Torso", Body, new Vector3(0, 0.36f, 0));
            S(Torso, new Vector3(0, 0.22f, 0), new Vector3(0.66f, 0.6f, 0.58f), Skin);
            Head = Node("Head", Torso, new Vector3(0, 0.5f, 0));
            HeadC = Node("HeadC", Head, new Vector3(0, 0.3f, 0));
            ArmL = Node("ArmL", Torso, new Vector3(-0.3f, 0.36f, 0), new Vector3(0, 0, -18));
            ArmR = Node("ArmR", Torso, new Vector3(0.3f, 0.36f, 0), new Vector3(0, 0, 18));
            foreach (var a in new[] { ArmL, ArmR })
                Ca(a, new Vector3(0, -0.14f, 0), new Vector3(0.15f, 0.14f, 0.15f), Skin);
            HandL = Node("HandL", ArmL, new Vector3(0, -0.3f, 0));
            HandR = Node("HandR", ArmR, new Vector3(0, -0.3f, 0));
            LegL = Node("LegL", Body, new Vector3(-0.14f, 0.36f, 0));
            LegR = Node("LegR", Body, new Vector3(0.14f, 0.36f, 0));
            foreach (var g in new[] { LegL, LegR })
                Ca(g, new Vector3(0, -0.14f, 0), new Vector3(0.17f, 0.13f, 0.17f), Skin);
            FootL = Node("FootL", LegL, new Vector3(0, -0.3f, 0.04f));
            FootR = Node("FootR", LegR, new Vector3(0, -0.3f, 0.04f));

            HeadShape(look.Idx("head"));
            Do("hat", Hat);
            Do("hair", Hair);
            Do("ears", Ears);
            Do("eyes", Glasses);
            Do("mask", Mask);
            Do("neck", Neck);
            Do("top", Top);
            Do("wings", Wings);
            Do("hands", Hands, true);
            Do("wrist", Wrist);
            Do("back", Back);
            Do("tail", Tail);
            Do("pants", Pants);
            Do("belt", Belt);
            Do("shoes", Shoes, true);
            Do("weapon", WeaponBuild);
            Secret(look.Of("secret"));
            if (layer != 0) foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        delegate void Builder(int idx, int set);

        /// <summary>Calls the builder of a department; "always" builders also run with nothing worn (bare hands / feet).</summary>
        void Do(string dept, Builder b, bool always = false)
        {
            var id = look.Of(dept);
            if (string.IsNullOrEmpty(id) && !always) return;
            b(Catalog.Index(id), Catalog.SetOf(id));
        }

        public static Color BodyColor(int i)
        {
            string[] c = { "FFC93C", "5BB8FF", "F2403C", "2A2A33", "8BE03A", "A050FF", "FF8FC4", "F4F7FA" };
            return Hex(c[Mathf.Clamp(i, 0, 7)]);
        }

        Vector3 HP(float theta, float phi, float r = 1f, float rx = 0.4f, float ry = 0.39f, float rz = 0.38f)
        {
            float t = theta * Mathf.Deg2Rad, p = phi * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(t) * Mathf.Cos(p) * rx * r, Mathf.Cos(t) * ry * r, Mathf.Sin(t) * Mathf.Sin(p) * rz * r);
        }

        // ================= head & face =================

        void HeadShape(int i)
        {
            var dark = M("2B1A1A", 0.5f);
            string mouth = "smile", eyes = "normal";
            switch (i)
            {
                case 1: B(HeadC, Vector3.zero, new Vector3(0.74f, 0.7f, 0.7f), Skin); faceZ = 0.35f; hatY = 0.35f; break;
                case 2: S(HeadC, Vector3.zero, new Vector3(0.94f, 0.74f, 0.82f), Skin); faceZ = 0.41f; hatY = 0.37f; eyeX = 0.17f; break;
                case 3:
                    S(HeadC, Vector3.zero, new Vector3(0.74f, 0.84f, 0.74f), Skin);
                    Cone(HeadC, new Vector3(0, 0.3f, 0), new Vector3(0.36f, 0.32f, 0.36f), Skin);
                    faceZ = 0.37f; hatY = 0.44f; eyeX = 0.13f; break;
                case 4: S(HeadC, Vector3.zero, new Vector3(0.8f, 0.78f, 0.76f), Skin); mouth = "grin"; break;
                case 5: S(HeadC, Vector3.zero, new Vector3(0.8f, 0.78f, 0.76f), Skin); mouth = "flat"; eyes = "serious"; break;
                case 6: S(HeadC, new Vector3(0, 0.06f, 0), new Vector3(0.78f, 0.98f, 0.76f), Skin); mouth = "tiny"; eyes = "alien"; hatY = 0.55f; break;
                case 7:
                {
                    var metal = M("A8B3C2", 0.7f, 0.6f);
                    B(HeadC, Vector3.zero, new Vector3(0.76f, 0.68f, 0.7f), metal);
                    B(HeadC, new Vector3(0, 0, 0.352f), new Vector3(0.62f, 0.5f, 0.01f), M("2A3140", 0.8f));
                    Rod(HeadC, new Vector3(0, 0.34f, 0), new Vector3(0, 0.55f, 0), 0.03f, metal);
                    var ball = S(HeadC, new Vector3(0, 0.57f, 0), 0.08f, G("FF3B3B", 2f)); Add<Pulse>(ball);
                    foreach (var sd in new[] { -1, 1 }) Cy(HeadC, new Vector3(sd * 0.39f, 0, 0), new Vector3(0.14f, 0.03f, 0.14f), metal, new Vector3(0, 0, 90));
                    faceZ = 0.355f; hatY = 0.34f; mouth = "grill"; eyes = "led"; break;
                }
                default: S(HeadC, Vector3.zero, new Vector3(0.8f, 0.78f, 0.76f), Skin); break;
            }

            // eyes
            foreach (var sd in new[] { -1, 1 })
            {
                var e = Node(sd < 0 ? "EyeL" : "EyeR", HeadC, new Vector3(sd * eyeX, eyeY, faceZ - 0.06f));
                if (sd < 0) EyeL = e; else EyeR = e;
                switch (eyes)
                {
                    case "alien":
                        S(e, new Vector3(0, 0.03f, 0.02f), new Vector3(0.2f, 0.27f, 0.1f), M("101010", 0.95f), new Vector3(0, 0, sd * 25));
                        S(e, new Vector3(sd * 0.03f, 0.08f, 0.07f), 0.04f, G("FFFFFF", 1f));
                        break;
                    case "led":
                        B(e, new Vector3(0, 0, 0.07f), new Vector3(0.14f, 0.06f, 0.02f), G("35E0FF", 2.2f));
                        break;
                    default:
                        S(e, Vector3.zero, new Vector3(0.17f, 0.21f, 0.1f), M("FFFFFF", 0.6f));
                        var pu = S(e, new Vector3(0, -0.01f, 0.04f), new Vector3(0.1f, 0.13f, 0.06f), M("161616", 0.9f));
                        Pupils.Add(pu);
                        var hl = S(pu.transform, new Vector3(0.25f, 0.3f, 0.5f), new Vector3(0.38f, 0.32f, 0.3f), G("FFFFFF", 1f));
                        Pupils.Add(hl);
                        if (eyes == "serious")
                        {
                            S(e, new Vector3(0, 0.07f, 0.01f), new Vector3(0.19f, 0.11f, 0.11f), Skin);   // half-closed lid
                            B(e, new Vector3(0, 0.15f, 0.03f), new Vector3(0.16f, 0.03f, 0.03f), dark, new Vector3(0, 0, -sd * 14));
                        }
                        break;
                }
            }

            Mouth = Node("Mouth", HeadC, new Vector3(0, -0.13f, faceZ - 0.02f));
            switch (mouth)
            {
                case "grin":
                    S(Mouth, new Vector3(0, -0.01f, 0), new Vector3(0.22f, 0.12f, 0.06f), dark);
                    B(Mouth, new Vector3(0, 0.035f, 0.02f), new Vector3(0.15f, 0.03f, 0.03f), M("FFFFFF"));
                    S(Mouth, new Vector3(0, -0.04f, 0.02f), new Vector3(0.1f, 0.04f, 0.03f), M("FF6B8B"));
                    break;
                case "flat": B(Mouth, Vector3.zero, new Vector3(0.12f, 0.022f, 0.02f), dark); break;
                case "tiny": S(Mouth, Vector3.zero, new Vector3(0.05f, 0.03f, 0.02f), dark); break;
                case "grill":
                    for (int k = 0; k < 4; k++) B(Mouth, new Vector3(-0.075f + k * 0.05f, 0, 0.02f), new Vector3(0.025f, 0.06f, 0.01f), G("35E0FF", 1.2f));
                    break;
                default:
                    for (int k = -2; k <= 2; k++) S(Mouth, new Vector3(k * 0.032f, k * k * 0.006f, 0), new Vector3(0.036f, 0.03f, 0.03f), dark);
                    break;
            }
            if (i != 7 && i != 6)
                foreach (var sd in new[] { -1, 1 })
                    S(HeadC, new Vector3(sd * (eyeX + 0.09f), -0.07f, faceZ - 0.08f), new Vector3(0.1f, 0.055f, 0.03f), M("FF8FA8", 0.2f));
        }

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

        // ================= secret modifiers =================

        void Secret(string id)
        {
            int i = Catalog.Index(id);
            if (i < 0) return;
            var rs = GetComponentsInChildren<Renderer>(true);
            switch (i)
            {
                case 0: SizeScale = 1.8f; break;
                case 1: SizeScale = 0.55f; break;
                case 2: Body.localScale = new Vector3(0.8f, 1.5f, 0.8f); break;
                case 3: Body.localScale = new Vector3(1.1f, 1f, 0.22f); break;
                case 4: { var m = Mats.Glow(new Color(0.03f, 0.03f, 0.04f)); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 5: { var m = Glass("BFE6FF", 0.3f); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 6: { var m = M("E8EEF5", 0.98f, 1f); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 7:
                {
                    SizeScale = 1.2f;
                    var m = Mats.Emissive(null, Hex("FFD24A"), Hex("FF9A00") * 0.9f, null, 0.9f);
                    m.SetFloat("_Metallic", 0.8f);
                    foreach (var r in rs) if (!r.sharedMaterial || r.sharedMaterial.color.grayscale > 0.15f) r.sharedMaterial = m;
                    var halo = Ring(HeadC, new Vector3(0, hatY + 0.2f, 0), 0.22f, 0.025f, G("FFE14D", 3f));
                    Add<Spin>(halo).speed = 60;
                    var t = Prim.Text(HeadC, "АРТЁМ 22 X", new Vector3(0, hatY + 0.4f, 0), 2.2f, Hex("FFD24A"));
                    t.gameObject.layer = layer;
                    t.gameObject.AddComponent<Billboard>();
                    break;
                }
            }
            transform.localScale = Vector3.one * SizeScale;
        }
    }

    /// <summary>Keeps a world label facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main;
            if (c) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position);
        }
    }
}
