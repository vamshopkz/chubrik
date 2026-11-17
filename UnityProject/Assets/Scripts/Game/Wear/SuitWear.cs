using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 17.11 «Костюмы»: whole-body outfits worn over the chubrik — the chicken (promo code «Курица»), the rich
    /// outfits of Артём and Али, football kits, sportsmen, transforming car robots, phones and computers.
    /// A costume takes the place of the clothes (hats, tops, trousers, shoes…), the face stays.
    /// </summary>
    public partial class ChubrikModel
    {
        /// <summary>Clothes a costume replaces (they are not built while a costume is worn).</summary>
        public static readonly HashSet<string> SuitHides = new HashSet<string> { "hat", "hair", "top", "pants", "neck", "wings", "back", "belt", "shoes", "hands", "wrist", "mask", "tail" };
        /// <summary>The costume index (−1 none); 0 and 200 are the chickens.</summary>
        public static bool IsChicken(int suit) => suit == 0 || suit == 200;

        void Suit(int i, int set)
        {
            var t = Node("Suit", Torso, Vector3.zero);
            Vector3 C = Dt.TorsoC, R = Dt.TorsoR * 1.07f;
            switch (i)
            {
                case 0: Chicken(t, false); break;
                case 200: Chicken(t, true); break;
                case 1: Rich(t, false); break;
                case 2: Rich(t, true); break;
                case 3: Football(t, "E53935", "FFFFFF", "FFFFFF", 7, false); break;
                case 4: Football(t, "F4F7FA", "1E5BD8", "1E5BD8", 10, false); break;
                case 5: Football(t, "FFD31A", "1F9E4A", "1F9E4A", 9, false); break;
                case 6: Football(t, "1A1A1A", "FFFFFF", "1A1A1A", 11, true); break;
                case 7: Keeper(t); break;
                case 8: Basketball(t); break;
                case 9: Hockey(t); break;
                case 10: Boxer(t); break;
                case 11: Tennis(t); break;
                case 12: Swimmer(t); break;
                case 13: Skier(t); break;
                case 14: Karate(t); break;
                case 15: Lifter(t); break;
                case 16: CarRobot(t, "D7262B", "2A2F3A", false, false); break;
                case 17: CarRobot(t, "1E5BD8", "C9D1DB", true, false); break;
                case 18: CarRobot(t, "FFD21A", "1A1A1A", false, true); break;
                case 19: PlaneRobot(t); break;
                case 20: Phone(t, false); break;
                case 21: Phone(t, true); break;
                case 22: Laptop(t); break;
                case 23: MonitorHead(t); break;
                case 24: GamingPC(t); break;
                case 25: Tablet(t); break;
            }
        }

        // ================= shared pieces =================

        static readonly Vector3 SC = Dt.TorsoC, SR = Dt.TorsoR * 1.07f;

        void Shirt(Transform t, Material m, Material trim, bool sleeves = true, bool collar = true)
        {
            S(t, SC, SR * 2, m);
            Dt.Stitches(t, BodyKit.SP(SC, SR, 150, 150, 0, 350), 24, 0.02f, 0.006f, trim);                // the hem
            if (collar) Ring(t, new Vector3(0, 0.49f, 0), 0.16f, 0.025f, trim);
            if (!sleeves) return;
            foreach (var a in new[] { ArmL, ArmR })
            {
                Ca(a, new Vector3(0, -0.08f, 0), new Vector3(0.2f, 0.1f, 0.2f), m);
                Ring(a, new Vector3(0, -0.15f, 0), 0.085f, 0.014f, trim);
            }
        }

        void Shorts(Material m, Material trim)
        {
            S(Torso, new Vector3(0, 0.03f, 0), new Vector3(0.66f, 0.22f, 0.58f), m);
            foreach (var g in new[] { LegL, LegR })
            {
                Cy(g, new Vector3(0, -0.07f, 0), new Vector3(0.21f, 0.07f, 0.21f), m);
                Ring(g, new Vector3(0, -0.135f, 0), 0.1f, 0.012f, trim);
            }
        }

        void Socks(Material m, Material stripe)
        {
            foreach (var g in new[] { LegL, LegR })
            {
                Cy(g, new Vector3(0, -0.21f, 0), new Vector3(0.18f, 0.07f, 0.18f), m);
                for (int k = 0; k < 2; k++) Ring(g, new Vector3(0, -0.17f - k * 0.03f, 0), 0.088f, 0.008f, stripe);
            }
        }

        void Boots(Material m, Material sole, Material stud, int studs = 6)
        {
            foreach (var f in new[] { FootL, FootR })
            {
                S(f, new Vector3(0, 0.04f, 0.05f), new Vector3(0.2f, 0.13f, 0.3f), m);
                B(f, new Vector3(0, -0.02f, 0.05f), new Vector3(0.18f, 0.03f, 0.3f), sole);
                for (int k = 0; k < studs; k++) Cy(f, new Vector3((k % 2 - 0.5f) * 0.09f, -0.045f, -0.06f + (k / 2) * 0.08f), new Vector3(0.03f, 0.012f, 0.03f), stud);
                Dt.Laces(f, new Vector3(0, 0.1f, 0.12f), new Vector3(0, 0.08f, 0.0f), Vector3.right, 4, M("FFFFFF", 0.3f));
            }
        }

        /// <summary>Seams round both sleeves and both trouser legs.</summary>
        void Seams(Material m)
        {
            foreach (var a in new[] { ArmL, ArmR }) Dt.Stitches(a, Dt.Circle(new Vector3(0, -0.2f, 0), 0.09f, 0.09f), 10, 0.018f, 0.005f, m);
            foreach (var g in new[] { LegL, LegR }) Dt.Stitches(g, Dt.Circle(new Vector3(0, -0.16f, 0), 0.105f, 0.105f), 10, 0.018f, 0.005f, m);
        }

        /// <summary>Rows of rivets on the chest plate (robots).</summary>
        void Rivets(Transform t, Material m, float z = 0.335f)
        {
            for (int k = 0; k < 12; k++) Ls(t, new Vector3(-0.33f + (k % 6) * 0.132f, k < 6 ? 0.52f : -0.04f, z), 0.025f, m);
            for (int k = 0; k < 8; k++) Ls(t, new Vector3(k < 4 ? -0.355f : 0.355f, 0.45f - (k % 4) * 0.13f, z - 0.04f), 0.025f, m);
        }

        void Number(Transform t, int n, Material m, float th = 70, float H = 0.13f)
        {
            foreach (var ph in new[] { 90f, 270f })
            {
                if (n < 10) BodyKit.Digit(t, SC, SR, th, ph, n, H, m, 1.035f);
                else
                {
                    float d = ph == 90 ? 9 : -9;
                    BodyKit.Digit(t, SC, SR, th, ph + d, n / 10, H, m, 1.035f);
                    BodyKit.Digit(t, SC, SR, th, ph - d, n % 10, H, m, 1.035f);
                }
            }
        }

        void Hands(Material m, float d = 0.17f)
        {
            foreach (var h in new[] { HandL, HandR }) S(h, Vector3.zero, Vector3.one * d, m);
        }

        GameObject Held(Vector3 pos, Vector3 euler)
        {
            var w = Node("Held", HandR, pos, euler);
            return w.gameObject;
        }

        void Ball(Transform p, Vector3 pos, float d, Material a, Material b)
        {
            S(p, pos, d, a);
            for (int k = 0; k < 12; k++)
            {
                float th = 30 + (k / 4) * 60, ph = (k % 4) * 90 + (k / 4) * 45;
                var q = Dt.Ell(pos, Vector3.one * d / 2, th, ph, 1.0f);
                Msh(Meshes.Ball(6, 4), p, q, new Vector3(d * 0.28f, d * 0.28f, d * 0.12f), b).transform.localRotation = Quaternion.LookRotation(q - pos);
            }
        }

        // ================= the chicken =================

        void Chicken(Transform t, bool golden)
        {
            var white = golden ? Mats.Emissive(null, Hex("FFD24A"), Hex("FF9A00") * 0.35f, null, 0.9f) : M("FAFAF5", 0.25f);
            var shade = golden ? M("F0B020", 0.8f, 0.6f) : M("E6E2D6", 0.2f);
            var red = M("E53030", 0.35f);
            var orange = M("FF9A1E", 0.4f);
            // fluffy body feathers
            S(t, SC + new Vector3(0, -0.02f, 0.02f), SR * 2.08f, white);
            BodyKit.Tufts(t, SC, SR * 1.04f, 4, 12, 20, 160, 0.11f, white, 31);
            BodyKit.Tufts(t, SC, SR * 1.06f, 2, 8, 60, 140, 0.08f, shade, 32);
            // wings on the arms: fans of feathers
            foreach (var (a, sd) in new[] { (ArmL, -1f), (ArmR, 1f) })
            {
                Ca(a, new Vector3(0, -0.12f, 0), new Vector3(0.2f, 0.16f, 0.2f), white);
                for (int k = 0; k < 6; k++)
                    Dt.Feather(a, new Vector3(sd * 0.04f, -0.05f - k * 0.04f, -0.02f), new Vector3(sd * 0.25f, -0.9f, -0.2f).normalized, Vector3.forward, 0.2f + k * 0.015f, k % 2 == 0 ? white : shade, shade, 4);
            }
            // the tail: a fan of feathers up and back
            for (int k = 0; k < 7; k++)
            {
                float a = (k - 3) * 16f;
                var dir = Quaternion.Euler(0, a, 0) * new Vector3(0, 0.75f, -0.66f);
                Dt.Feather(t, new Vector3(0, 0.12f, -0.27f), dir.normalized, Vector3.right, 0.32f + (3 - Mathf.Abs(k - 3)) * 0.04f, k % 2 == 0 ? white : shade, shade, 5);
            }
            // head: comb on top, beak and wattle in front
            for (int k = 0; k < 5; k++)
            {
                float z = -0.14f + k * 0.07f;
                S(HeadC, new Vector3(0, hatY + 0.02f + Mathf.Sin(k / 4f * Mathf.PI) * 0.06f, z), new Vector3(0.07f, 0.14f + (k % 2) * 0.03f, 0.08f), red);
            }
            Cone(HeadC, new Vector3(0, -0.04f, faceZ - 0.02f), new Vector3(0.16f, 0.2f, 0.12f), orange, new Vector3(90, 0, 0));
            Cone(HeadC, new Vector3(0, -0.075f, faceZ - 0.02f), new Vector3(0.13f, 0.14f, 0.09f), M("E8851A", 0.4f), new Vector3(100, 0, 0));
            S(HeadC, new Vector3(0, -0.17f, faceZ + 0.03f), new Vector3(0.07f, 0.12f, 0.06f), red);
            S(HeadC, new Vector3(0.04f, -0.16f, faceZ + 0.02f), new Vector3(0.06f, 0.1f, 0.05f), red);
            BodyKit.Tufts(HeadC, Vector3.zero, Dt.HeadR * 1.01f, 2, 9, 10, 60, 0.07f, white, 33);
            // yellow legs with three toes and a spur
            foreach (var (g, f) in new[] { (LegL, FootL), (LegR, FootR) })
            {
                Cy(g, new Vector3(0, -0.15f, 0), new Vector3(0.08f, 0.15f, 0.08f), orange);
                for (int k = 0; k < 4; k++) Ring(g, new Vector3(0, -0.06f - k * 0.05f, 0), 0.045f, 0.008f, M("E8851A", 0.4f));
                for (int k = -1; k <= 1; k++) Rod(f, new Vector3(0, 0.01f, 0), new Vector3(k * 0.08f, 0.0f, 0.16f), 0.045f, orange);
                Rod(f, new Vector3(0, 0.01f, 0), new Vector3(0, 0.02f, -0.09f), 0.035f, orange);
            }
            if (golden)
            {
                var halo = Ring(HeadC, new Vector3(0, hatY + 0.28f, 0), 0.2f, 0.02f, G("FFE14D", 2.5f));
                Add<Spin>(halo).speed = 70;
            }
        }

        // ================= the rich =================

        void Rich(Transform t, bool white)
        {
            var cloth = white ? M("F6F4EE", 0.35f) : M("1A1A22", 0.45f);
            var lapel = white ? M("E6E0D2", 0.5f) : M("2E2E3A", 0.7f);
            var gold = M("FFC83A", 0.95f, 0.9f);
            var diamond = G("D8F4FF", 1.6f);
            var shirt = M(white ? "1E5BD8" : "FFFFFF", 0.3f);
            S(t, SC, SR * 2, cloth);
            // shirt front, tie / bow, lapels, gold buttons
            BodyKit.Panel(t, SC, SR, 40, 110, 70, 110, 3, 4, 0.006f, shirt, 1.01f);
            if (white) for (int k = 0; k < 4; k++) BodyKit.Flat(t, SC, SR, 50 + k * 14, 90, new Vector3(0.05f, 0.05f, 0.012f), M("C62828", 0.4f), 1.03f, 45);
            else { S(t, Dt.Ell(SC, SR, 38, 90, 1.05f), new Vector3(0.14f, 0.06f, 0.05f), M("1A1A1A", 0.4f)); S(t, Dt.Ell(SC, SR, 38, 90, 1.08f), 0.04f, gold); }
            foreach (var sd in new[] { -1f, 1f })
            {
                BodyKit.Band(t, BodyKit.SP(SC, SR, 30, 100, 90 + sd * 14, 90 + sd * 26, 1.03f), BodyKit.EN(SC, SR), 6, 0.05f, 0.01f, lapel);
                BodyKit.Flat(t, SC, SR, 105, 90 + sd * 45, new Vector3(0.1f, 0.03f, 0.01f), lapel, 1.03f);   // pocket flaps
            }
            for (int k = 0; k < 3; k++) BodyKit.Disc(t, SC, SR, 80 + k * 18, 104, 0.035f, 0.012f, gold, 1.04f);
            Dt.Stitches(t, BodyKit.SP(SC, SR, 150, 150, 0, 350), 24, 0.02f, 0.006f, lapel);
            // a thick gold chain with a «$» medallion
            BodyKit.Chain(t, Dt.Circle(new Vector3(0, 0.44f, 0.03f), 0.2f, 0.19f, 200, 340), 18, 0.018f, 0.006f, gold);
            var med = Msh(Meshes.Star(), t, Dt.Ell(SC, SR, 55, 90, 1.12f), new Vector3(0.12f, 0.12f, 0.04f), gold);
            S(med.transform, Vector3.zero, 0.35f, diamond);
            // sleeves with cuffs and gold watch / diamond rings
            foreach (var a in new[] { ArmL, ArmR })
            {
                Ca(a, new Vector3(0, -0.13f, 0), new Vector3(0.18f, 0.15f, 0.18f), cloth);
                Ring(a, new Vector3(0, -0.25f, 0), 0.075f, 0.016f, shirt);
            }
            Cy(ArmL, new Vector3(0, -0.22f, 0), new Vector3(0.16f, 0.025f, 0.16f), gold);
            BodyKit.Pin(ArmL, new Vector3(0, -0.22f, 0.08f), Vector3.forward, 0.07f, 0.02f, diamond);
            foreach (var h in new[] { HandL, HandR }) for (int k = 0; k < 2; k++) { Ring(h, new Vector3(-0.03f + k * 0.06f, -0.06f, 0.05f), 0.025f, 0.008f, gold); Ls(h, new Vector3(-0.03f + k * 0.06f, -0.04f, 0.07f), 0.03f, diamond); }
            // trousers and shiny shoes
            foreach (var g in new[] { LegL, LegR }) { Cy(g, new Vector3(0, -0.13f, 0), new Vector3(0.18f, 0.13f, 0.18f), cloth); B(g, new Vector3(0, -0.13f, 0.09f), new Vector3(0.01f, 0.24f, 0.01f), lapel); }
            foreach (var f in new[] { FootL, FootR }) { S(f, new Vector3(0, 0.03f, 0.05f), new Vector3(0.19f, 0.11f, 0.3f), M(white ? "8D5524" : "0E0E10", 0.95f)); B(f, new Vector3(0, -0.02f, 0.05f), new Vector3(0.18f, 0.025f, 0.29f), M("3A2A1A", 0.4f)); }
            if (white)
            {
                // Али: gold sunglasses and a briefcase full of money
                var shades = M("111111", 0.95f);
                foreach (var sd in new[] { -1f, 1f }) { S(HeadC, new Vector3(sd * eyeX, eyeY, faceZ + 0.04f), new Vector3(0.16f, 0.11f, 0.04f), shades); Ring(HeadC, new Vector3(sd * eyeX, eyeY, faceZ + 0.045f), 0.075f, 0.012f, gold, new Vector3(90, 0, 0), new Vector3(1, 1, 0.75f)); }
                B(HeadC, new Vector3(0, eyeY + 0.01f, faceZ + 0.05f), new Vector3(0.06f, 0.015f, 0.015f), gold);
                var bag = Node("Briefcase", HandL, new Vector3(0, -0.2f, 0.02f));
                B(bag, Vector3.zero, new Vector3(0.38f, 0.28f, 0.1f), M("2A1A10", 0.6f));
                for (int k = 0; k < 4; k++) B(bag, new Vector3(-0.15f + k * 0.1f, 0.15f, 0), new Vector3(0.07f, 0.05f, 0.08f), M("4CAF50", 0.2f));   // banknotes sticking out
                foreach (var sd in new[] { -1f, 1f }) B(bag, new Vector3(sd * 0.12f, 0.0f, 0.055f), new Vector3(0.05f, 0.04f, 0.01f), gold);
                Rod(bag, new Vector3(-0.07f, 0.14f, 0), new Vector3(0.07f, 0.14f, 0), 0.025f, gold);
            }
            else
            {
                // Артём: a tall top hat with a gold band, a monocle and a cane with a diamond knob
                Cy(HeadC, new Vector3(0, hatY + 0.02f, 0), new Vector3(0.52f, 0.015f, 0.52f), cloth);
                Cy(HeadC, new Vector3(0, hatY + 0.2f, 0), new Vector3(0.32f, 0.18f, 0.32f), cloth);
                Cy(HeadC, new Vector3(0, hatY + 0.07f, 0), new Vector3(0.33f, 0.035f, 0.33f), gold);
                Ring(HeadC, new Vector3(eyeX, eyeY, faceZ + 0.04f), 0.07f, 0.01f, gold, new Vector3(90, 0, 0));
                S(HeadC, new Vector3(eyeX, eyeY, faceZ + 0.04f), new Vector3(0.12f, 0.12f, 0.01f), Glass("DFF2FF", 0.3f));
                Rod(HeadC, new Vector3(eyeX + 0.07f, eyeY, faceZ + 0.04f), new Vector3(0.22f, -0.25f, faceZ - 0.05f), 0.006f, gold);
                Rod(HandR, new Vector3(0, 0, 0.02f), new Vector3(0, -0.55f, 0.12f), 0.035f, M("1A1A1A", 0.9f));
                S(HandR, new Vector3(0, 0.03f, 0.02f), 0.07f, diamond);
                // a bag of money on the back
                var sack = S(Torso, new Vector3(0, 0.25f, -0.36f), new Vector3(0.36f, 0.4f, 0.3f), M("C9A66B", 0.2f));
                Prim.Text(sack.transform, "$", new Vector3(0, 0, -0.52f), 5, Hex("2E7D32")).transform.localRotation = Quaternion.Euler(0, 180, 0);
                Ring(Torso, new Vector3(0, 0.43f, -0.36f), 0.07f, 0.015f, M("8D5524", 0.3f));
            }
        }

        // ================= sport =================

        void Football(Transform t, string body, string trim, string num, int n, bool stripes)
        {
            var m = M(body, 0.3f); var tr = M(trim, 0.3f); var nm = M(num, 0.35f);
            Shirt(t, m, tr);
            if (stripes) for (int k = 0; k < 6; k++) BodyKit.Band(t, BodyKit.SP(SC, SR, 25, 150, 60 + k * 12, 60 + k * 12, 1.02f), BodyKit.EN(SC, SR), 6, 0.04f, 0.006f, tr);
            Number(t, n, stripes ? M("E53935", 0.3f) : nm, 105, 0.12f);
            BodyKit.Disc(t, SC, SR, 55, 115, 0.07f, 0.01f, tr, 1.04f);                                        // badge
            Msh(Meshes.Star(), t, Dt.Ell(SC, SR, 50, 115, 1.06f), new Vector3(0.05f, 0.05f, 0.02f), M("FFC83A", 0.9f, 0.8f));
            Shorts(stripes ? M("1A1A1A", 0.3f) : tr, m);
            Socks(m, tr);
            Boots(M("1A1A1A", 0.6f), M("FFFFFF", 0.3f), M("C9D1DB", 0.6f, 0.4f));
            Ball(Body, new Vector3(0.32f, 0.12f, 0.35f), 0.24f, M("FFFFFF", 0.4f), M("1A1A1A", 0.4f));
        }

        void Keeper(Transform t)
        {
            var m = M("2FBF4A", 0.3f); var tr = M("1A1A1A", 0.3f);
            Shirt(t, m, tr);
            foreach (var a in new[] { ArmL, ArmR }) Ca(a, new Vector3(0, -0.15f, 0), new Vector3(0.17f, 0.15f, 0.17f), m);   // long sleeves
            for (int k = 0; k < 8; k++) BodyKit.Flat(t, SC, SR, 50 + (k / 4) * 30, 60 + (k % 4) * 20, new Vector3(0.07f, 0.07f, 0.01f), M("27A040", 0.3f), 1.03f, 45);
            Number(t, 1, M("FFFFFF", 0.3f), 105, 0.13f);
            Shorts(tr, m); Socks(m, tr);
            Boots(M("FF6A00", 0.6f), M("1A1A1A", 0.3f), M("C9D1DB", 0.6f, 0.4f));
            foreach (var h in new[] { HandL, HandR })
            {
                S(h, Vector3.zero, new Vector3(0.24f, 0.24f, 0.16f), M("FFFFFF", 0.35f));
                for (int k = 0; k < 4; k++) Ca(h, new Vector3(-0.075f + k * 0.05f, -0.12f, 0), new Vector3(0.05f, 0.06f, 0.05f), M("FFFFFF", 0.35f));
                Cy(h, new Vector3(0, 0.07f, 0), new Vector3(0.18f, 0.04f, 0.18f), M("2FBF4A", 0.3f));
            }
        }

        void Basketball(Transform t)
        {
            var m = M("FF7A1A", 0.3f); var tr = M("6A1B9A", 0.3f);
            Shirt(t, m, tr, false);
            foreach (var sd in new[] { -1f, 1f }) BodyKit.Band(t, BodyKit.SP(SC, SR, 5, 40, 90 + sd * 50, 270 - sd * 50, 1.02f), BodyKit.EN(SC, SR), 8, 0.05f, 0.01f, tr);
            Number(t, 23, M("FFFFFF", 0.3f), 85, 0.14f);
            Shorts(tr, m);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.12f, 0), new Vector3(0.22f, 0.11f, 0.22f), tr);   // long baggy shorts
            Socks(M("FFFFFF", 0.3f), m);
            Boots(M("FFFFFF", 0.5f), m, m, 0);
            Cy(HeadC, new Vector3(0, 0.2f, 0), new Vector3(0.8f, 0.03f, 0.78f), m);                                    // headband
            var ball = S(HandL, new Vector3(0, -0.12f, 0.12f), 0.3f, M("E86A1A", 0.3f));
            for (int k = 0; k < 4; k++) Ring(ball.transform, Vector3.zero, 0.5f, 0.02f, M("1A1A1A", 0.3f), new Vector3(k * 45, 0, k % 2 * 90));
        }

        void Hockey(Transform t)
        {
            var m = M("C62828", 0.3f); var tr = M("FFFFFF", 0.3f); var black = M("1A1A1A", 0.5f);
            S(t, SC, SR * 2.15f, m);
            for (int k = 0; k < 2; k++) BodyKit.Band(t, BodyKit.SP(SC, SR * 1.07f, 120 + k * 12, 120 + k * 12, 0, 350), BodyKit.EN(SC, SR), 18, 0.04f, 0.008f, tr);
            Number(t, 99, tr, 75, 0.13f);
            foreach (var a in new[] { ArmL, ArmR }) { S(a, new Vector3(0, 0.0f, 0), new Vector3(0.26f, 0.22f, 0.26f), m); Ca(a, new Vector3(0, -0.15f, 0), new Vector3(0.19f, 0.15f, 0.19f), m); }
            foreach (var h in new[] { HandL, HandR }) { S(h, Vector3.zero, new Vector3(0.2f, 0.18f, 0.2f), black); Cy(h, new Vector3(0, 0.07f, 0), new Vector3(0.2f, 0.05f, 0.2f), m); }
            Shorts(black, m); Socks(m, tr);
            foreach (var f in new[] { FootL, FootR })
            {
                S(f, new Vector3(0, 0.05f, 0.04f), new Vector3(0.19f, 0.14f, 0.28f), black);
                B(f, new Vector3(0, -0.06f, 0.04f), new Vector3(0.02f, 0.06f, 0.36f), M("C9D1DB", 0.9f, 0.8f));   // skate blade
                for (int k = 0; k < 3; k++) Rod(f, new Vector3(0, -0.04f, -0.08f + k * 0.08f), new Vector3(0, 0.0f, -0.08f + k * 0.08f), 0.02f, M("C9D1DB", 0.9f, 0.8f));
            }
            // helmet with a cage visor
            S(HeadC, new Vector3(0, 0.06f, -0.02f), Dt.HeadR * 2.12f, M("F4F7FA", 0.5f));
            for (int k = 0; k < 5; k++) Rod(HeadC, new Vector3(-0.2f + k * 0.1f, 0.12f, faceZ + 0.06f), new Vector3(-0.2f + k * 0.1f, -0.25f, faceZ + 0.06f), 0.012f, M("C9D1DB", 0.8f, 0.6f));
            for (int k = 0; k < 3; k++) Rod(HeadC, new Vector3(-0.24f, 0.1f - k * 0.15f, faceZ + 0.06f), new Vector3(0.24f, 0.1f - k * 0.15f, faceZ + 0.06f), 0.012f, M("C9D1DB", 0.8f, 0.6f));
            // the stick
            Rod(HandR, Vector3.zero, new Vector3(0.05f, -0.75f, 0.2f), 0.04f, M("C08040", 0.3f));
            B(HandR, new Vector3(0.1f, -0.78f, 0.28f), new Vector3(0.22f, 0.07f, 0.03f), black, new Vector3(0, -20, 0));
        }

        void Boxer(Transform t)
        {
            var red = M("D32F2F", 0.5f); var gold = M("FFC83A", 0.95f, 0.9f); var white = M("FFFFFF", 0.3f);
            // champion belt + satin shorts, big gloves
            S(Torso, new Vector3(0, 0.03f, 0), new Vector3(0.7f, 0.24f, 0.62f), M("1E5BD8", 0.85f));
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.07f, 0), new Vector3(0.23f, 0.08f, 0.23f), M("1E5BD8", 0.85f));
            Cy(Torso, new Vector3(0, 0.1f, 0), new Vector3(0.68f, 0.05f, 0.6f), gold);
            var plate = B(Torso, new Vector3(0, 0.1f, 0.3f), new Vector3(0.24f, 0.16f, 0.04f), gold);
            Msh(Meshes.Star(), plate.transform, new Vector3(0, 0, 0.7f), new Vector3(0.5f, 0.5f, 0.4f), red);
            for (int k = 0; k < 8; k++) Ls(Torso, Dt.Circle(new Vector3(0, 0.1f, 0.33f), 0.14f, 0.02f)(k / 8f), 0.03f, G("D8F4FF", 1.4f));
            foreach (var h in new[] { HandL, HandR })
            {
                S(h, new Vector3(0, -0.02f, 0.03f), new Vector3(0.26f, 0.28f, 0.26f), red);
                S(h, new Vector3(0.09f, 0.0f, 0.1f), new Vector3(0.1f, 0.12f, 0.1f), red);
                Cy(h, new Vector3(0, 0.12f, 0), new Vector3(0.2f, 0.06f, 0.2f), white);
                Dt.Laces(h, new Vector3(0, 0.14f, 0.1f), new Vector3(0, 0.06f, 0.13f), Vector3.right, 3, white);
            }
            foreach (var f in new[] { FootL, FootR }) { S(f, new Vector3(0, 0.05f, 0.04f), new Vector3(0.19f, 0.16f, 0.27f), white); Dt.Laces(f, new Vector3(0, 0.12f, 0.1f), new Vector3(0, 0.04f, 0.15f), Vector3.right, 4, M("1A1A1A", 0.3f)); }
            Cy(HeadC, new Vector3(0, 0.22f, 0), new Vector3(0.82f, 0.03f, 0.8f), red);
            Dt.Stitches(Torso, Dt.Circle(new Vector3(0, -0.02f, 0), 0.34f, 0.3f), 24, 0.02f, 0.006f, white);
        }

        void Tennis(Transform t)
        {
            var white = M("FFFFFF", 0.3f); var green = M("2FBF4A", 0.3f);
            Shirt(t, white, green);
            BodyKit.Band(t, BodyKit.SP(SC, SR, 50, 50, 20, 160, 1.02f), BodyKit.EN(SC, SR), 10, 0.035f, 0.008f, green);
            Shorts(white, green); Socks(white, green); Boots(white, M("C9D1DB", 0.3f), white, 0);
            Cy(HeadC, new Vector3(0, 0.2f, 0), new Vector3(0.82f, 0.035f, 0.8f), green);
            foreach (var a in new[] { ArmL, ArmR }) Cy(a, new Vector3(0, -0.24f, 0), new Vector3(0.14f, 0.035f, 0.14f), green);   // wristbands
            // racket with strings
            var r = Node("Racket", HandR, new Vector3(0, -0.05f, 0.05f), new Vector3(-30, 0, 0));
            Rod(r, Vector3.zero, new Vector3(0, -0.3f, 0), 0.04f, M("1A1A1A", 0.4f));
            Ring(r, new Vector3(0, -0.48f, 0), 0.16f, 0.018f, M("D32F2F", 0.5f), new Vector3(90, 0, 0), new Vector3(1, 1, 1.35f));
            for (int k = -3; k <= 3; k++) { Rod(r, new Vector3(k * 0.04f, -0.3f, 0), new Vector3(k * 0.04f, -0.69f, 0), 0.005f, white); Rod(r, new Vector3(-0.15f, -0.48f + k * 0.05f, 0), new Vector3(0.15f, -0.48f + k * 0.05f, 0), 0.005f, white); }
            S(HandL, new Vector3(0, -0.06f, 0.06f), 0.1f, M("D7F52A", 0.3f));
        }

        void Swimmer(Transform t)
        {
            var blue = M("1E88E5", 0.8f); var cap = M("FFD31A", 0.8f);
            S(Torso, new Vector3(0, 0.03f, 0), new Vector3(0.66f, 0.2f, 0.58f), blue);
            for (int k = 0; k < 6; k++) Ls(Torso, Dt.Circle(new Vector3(0, 0.12f, 0), 0.33f, 0.29f)(k / 6f), 0.04f, M("FFFFFF", 0.3f));
            S(HeadC, new Vector3(0, 0.08f, -0.02f), Dt.HeadR * 2.08f, cap);
            Prim.Text(HeadC, "67", new Vector3(0, 0.12f, -0.42f), 3, Hex("1E88E5")).transform.localRotation = Quaternion.Euler(0, 180, 0);
            foreach (var sd in new[] { -1f, 1f })
            {
                S(HeadC, new Vector3(sd * eyeX, eyeY, faceZ + 0.04f), new Vector3(0.14f, 0.11f, 0.05f), Glass("35E0FF", 0.55f));
                Ring(HeadC, new Vector3(sd * eyeX, eyeY, faceZ + 0.04f), 0.065f, 0.014f, M("1A1A1A", 0.6f), new Vector3(90, 0, 0));
            }
            BodyKit.Band(HeadC, Dt.Circle(new Vector3(0, eyeY, 0), 0.42f, 0.41f, -60, 240), p => new Vector3(p.x, 0, p.z).normalized, 14, 0.02f, 0.01f, M("1A1A1A", 0.6f));
            foreach (var f in new[] { FootL, FootR })
            {
                S(f, new Vector3(0, 0.02f, 0.12f), new Vector3(0.22f, 0.05f, 0.45f), M("1E88E5", 0.5f));   // flippers
                for (int k = 0; k < 3; k++) B(f, new Vector3((k - 1) * 0.06f, 0.035f, 0.18f), new Vector3(0.012f, 0.012f, 0.25f), M("1565C0", 0.5f));
            }
            Seams(M("0D47A1", 0.4f));
            Dt.Stitches(Torso, Dt.Circle(new Vector3(0, 0.12f, 0), 0.335f, 0.295f), 20, 0.02f, 0.006f, M("FFFFFF", 0.3f));
            var ring = Ring(Body, new Vector3(0, 0.42f, 0), 0.38f, 0.1f, M("FF5252", 0.4f));
            for (int k = 0; k < 4; k++) Ring(Body, new Vector3(0, 0.42f, 0), 0.38f, 0.102f, M("FFFFFF", 0.4f), new Vector3(0, k * 90, 0), new Vector3(0.2f, 1, 1));
        }

        void Skier(Transform t)
        {
            var jacket = M("FF4081", 0.3f); var dark = M("263238", 0.3f); var white = M("FFFFFF", 0.3f);
            S(t, SC, SR * 2.1f, jacket);
            Dt.Zipper(t, Dt.OnTorso(30, 90, 1.12f), Dt.OnTorso(150, 90, 1.12f), 16, M("C9D1DB", 0.8f, 0.6f));
            BodyKit.Band(t, BodyKit.SP(SC, SR * 1.05f, 80, 80, 0, 350), BodyKit.EN(SC, SR), 16, 0.05f, 0.008f, white);
            foreach (var a in new[] { ArmL, ArmR }) { Ca(a, new Vector3(0, -0.14f, 0), new Vector3(0.2f, 0.16f, 0.2f), jacket); Ring(a, new Vector3(0, -0.1f, 0), 0.1f, 0.015f, white); }
            Hands(dark, 0.19f);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.12f, 0), new Vector3(0.21f, 0.13f, 0.21f), dark);
            foreach (var f in new[] { FootL, FootR })
            {
                S(f, new Vector3(0, 0.06f, 0.04f), new Vector3(0.2f, 0.18f, 0.28f), white);
                B(f, new Vector3(0, -0.04f, 0.15f), new Vector3(0.12f, 0.02f, 0.9f), M("FFC83A", 0.5f));   // skis
                B(f, new Vector3(0, -0.01f, 0.58f), new Vector3(0.12f, 0.02f, 0.1f), M("FFC83A", 0.5f), new Vector3(-30, 0, 0));
                for (int k = 0; k < 3; k++) B(f, new Vector3(0, 0.0f, -0.02f + k * 0.06f), new Vector3(0.14f, 0.03f, 0.02f), dark);
            }
            Knit(HeadC, jacket, white);
            S(HeadC, new Vector3(0, 0.08f, faceZ + 0.06f), new Vector3(0.5f, 0.16f, 0.08f), Glass("FF9A1E", 0.6f));
            Rod(HandR, Vector3.zero, new Vector3(0, -0.9f, 0.1f), 0.025f, M("C9D1DB", 0.7f, 0.5f));
            Ring(HandR, new Vector3(0, -0.8f, 0.09f), 0.06f, 0.01f, dark, new Vector3(0, 0, 0));
        }

        void Knit(Transform head, Material m, Material pom)
        {
            S(head, new Vector3(0, 0.14f, -0.01f), new Vector3(0.84f, 0.62f, 0.8f), m);
            BodyKit.Knit(head, new Vector3(0, 0.1f, 0), 0.42f, 0.41f, 0.1f, 22, m, pom);
            S(head, new Vector3(0, hatY + 0.08f, 0), 0.16f, pom);
        }

        void Karate(Transform t)
        {
            var white = M("FAFAFA", 0.25f); var black = M("1A1A1A", 0.4f);
            S(t, SC, SR * 2.05f, white);
            foreach (var sd in new[] { -1f, 1f }) BodyKit.Band(t, BodyKit.SP(SC, SR, 20, 110, 90 + sd * 40, 90 - sd * 10, 1.04f), BodyKit.EN(SC, SR), 8, 0.06f, 0.01f, M("EDEDED", 0.25f));   // crossed lapels
            Cy(t, new Vector3(0, 0.05f, 0), new Vector3(0.7f, 0.04f, 0.62f), black);
            S(t, new Vector3(0.05f, 0.05f, 0.31f), new Vector3(0.09f, 0.07f, 0.05f), black);
            foreach (var sd in new[] { -1f, 1f }) Rod(t, new Vector3(0.05f, 0.05f, 0.32f), new Vector3(0.05f + sd * 0.07f, -0.17f, 0.33f), 0.035f, black);
            foreach (var a in new[] { ArmL, ArmR }) Cy(a, new Vector3(0, -0.13f, 0), new Vector3(0.22f, 0.13f, 0.22f), white);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.14f, 0), new Vector3(0.22f, 0.14f, 0.22f), white);
            Dt.Stitches(t, BodyKit.SP(SC, SR, 150, 150, 0, 350), 24, 0.02f, 0.006f, M("D0D0D0", 0.3f));
            Seams(M("D0D0D0", 0.3f));
            Cy(HeadC, new Vector3(0, 0.2f, 0), new Vector3(0.82f, 0.035f, 0.8f), M("D32F2F", 0.3f));
            foreach (var sd in new[] { -1f, 1f }) Rod(HeadC, new Vector3(sd * 0.03f, 0.2f, -0.4f), new Vector3(sd * 0.12f, 0.0f, -0.5f), 0.03f, M("D32F2F", 0.3f));
            // a board to break
            var board = B(HandL, new Vector3(0, -0.06f, 0.15f), new Vector3(0.3f, 0.04f, 0.2f), Lobby.Tiled(Tex.Planks("C08040"), 1));
            B(board.transform, new Vector3(0, 0.6f, 0), new Vector3(0.05f, 0.4f, 1.0f), M("8D5524", 0.3f));
        }

        void Lifter(Transform t)
        {
            var red = M("D32F2F", 0.3f); var metal = M("C9D1DB", 0.85f, 0.8f); var black = M("1A1A1A", 0.5f);
            S(t, SC, SR * 2.02f, red);
            foreach (var sd in new[] { -1f, 1f }) BodyKit.Band(t, BodyKit.SP(SC, SR, 5, 45, 90 + sd * 30, 270 - sd * 30, 1.02f), BodyKit.EN(SC, SR), 8, 0.05f, 0.01f, red);
            Cy(t, new Vector3(0, 0.07f, 0), new Vector3(0.7f, 0.07f, 0.62f), M("5D4037", 0.5f));   // lifting belt
            B(t, new Vector3(0, 0.07f, 0.31f), new Vector3(0.1f, 0.08f, 0.03f), metal);
            Shorts(red, black);
            Seams(M("B71C1C", 0.3f));
            Boots(black, M("5D4037", 0.4f), black, 0);
            Hands(M("FFFFFF", 0.3f), 0.18f);
            // the barbell over the shoulders
            Rod(Torso, new Vector3(-0.85f, 0.48f, -0.1f), new Vector3(0.85f, 0.48f, -0.1f), 0.04f, metal);
            foreach (var sd in new[] { -1f, 1f })
                for (int k = 0; k < 3; k++)
                {
                    float x = sd * (0.6f + k * 0.07f);
                    Cy(Torso, new Vector3(x, 0.48f, -0.1f), new Vector3(0.5f - k * 0.1f, 0.03f, 0.5f - k * 0.1f), k == 0 ? red : black, new Vector3(0, 0, 90));
                }
        }

        // ================= robots that turn into cars =================

        void CarRobot(Transform t, string body, string trim, bool truck, bool sport)
        {
            var paint = M(body, 0.85f, 0.4f); var tr = M(trim, 0.6f, 0.5f);
            var glass = Glass("9ED8FF", 0.55f); var tyre = M("1A1A1A", 0.3f); var rim = M("C9D1DB", 0.9f, 0.8f);
            var lamp = G("FFF6C2", 2f); var red = G("FF2A2A", 1.6f);
            // chest = the bonnet with a windscreen and headlights
            B(t, new Vector3(0, 0.24f, 0.02f), new Vector3(0.72f, 0.6f, 0.62f), paint);
            B(t, new Vector3(0, 0.34f, 0.31f), new Vector3(0.6f, 0.26f, 0.04f), glass, new Vector3(-12, 0, 0));
            foreach (var sd in new[] { -1f, 1f })
            {
                Cy(t, new Vector3(sd * 0.24f, 0.08f, 0.33f), new Vector3(0.12f, 0.02f, 0.12f), lamp, new Vector3(90, 0, 0));
                Ring(t, new Vector3(sd * 0.24f, 0.08f, 0.335f), 0.06f, 0.012f, rim, new Vector3(90, 0, 0));
                Cy(t, new Vector3(sd * 0.27f, 0.08f, -0.29f), new Vector3(0.1f, 0.02f, 0.06f), red, new Vector3(90, 0, 0));
                // wheels on the shoulders
                Cy(ArmL.parent, new Vector3(sd * 0.42f, 0.42f, -0.02f), new Vector3(0.26f, 0.06f, 0.26f), tyre, new Vector3(0, 0, 90));
                Cy(ArmL.parent, new Vector3(sd * 0.47f, 0.42f, -0.02f), new Vector3(0.14f, 0.02f, 0.14f), rim, new Vector3(0, 0, 90));
                for (int k = 0; k < 5; k++) B(ArmL.parent, new Vector3(sd * 0.48f, 0.42f, -0.02f), new Vector3(0.01f, 0.12f, 0.02f), rim, new Vector3(k * 36, 0, 0));
            }
            for (int k = 0; k < 6; k++) B(t, new Vector3(-0.15f + k * 0.06f, 0.0f, 0.33f), new Vector3(0.03f, 0.1f, 0.02f), tr);   // grille
            B(t, new Vector3(0, -0.08f, 0.32f), new Vector3(0.7f, 0.06f, 0.06f), tr);                                         // bumper
            if (truck) { foreach (var sd in new[] { -1f, 1f }) { Rod(t, new Vector3(sd * 0.3f, 0.2f, -0.3f), new Vector3(sd * 0.3f, 0.75f, -0.3f), 0.06f, rim); Cy(t, new Vector3(sd * 0.3f, 0.78f, -0.3f), new Vector3(0.08f, 0.01f, 0.08f), tyre); } }
            if (sport) { B(t, new Vector3(0, 0.62f, -0.32f), new Vector3(0.8f, 0.03f, 0.16f), tr); foreach (var sd in new[] { -1f, 1f }) B(t, new Vector3(sd * 0.3f, 0.57f, -0.32f), new Vector3(0.03f, 0.1f, 0.08f), tr); BodyKit.Band(t, Dt.Line(new Vector3(-0.08f, 0.55f, 0.3f), new Vector3(-0.08f, -0.05f, 0.33f)), p => Vector3.forward, 6, 0.05f, 0.005f, tr); BodyKit.Band(t, Dt.Line(new Vector3(0.08f, 0.55f, 0.3f), new Vector3(0.08f, -0.05f, 0.33f)), p => Vector3.forward, 6, 0.05f, 0.005f, tr); }
            // arms and legs: armour plates
            foreach (var a in new[] { ArmL, ArmR }) { B(a, new Vector3(0, -0.14f, 0), new Vector3(0.2f, 0.28f, 0.2f), tr); B(a, new Vector3(0, -0.1f, 0.1f), new Vector3(0.16f, 0.12f, 0.03f), paint); }
            foreach (var h in new[] { HandL, HandR }) B(h, Vector3.zero, new Vector3(0.17f, 0.15f, 0.17f), M("5B6370", 0.7f, 0.6f));
            foreach (var g in new[] { LegL, LegR })
            {
                B(g, new Vector3(0, -0.14f, 0), new Vector3(0.22f, 0.3f, 0.22f), paint);
                Cy(g, new Vector3(0, -0.2f, -0.12f), new Vector3(0.2f, 0.04f, 0.2f), tyre, new Vector3(0, 0, 90));
                Cy(g, new Vector3(0, -0.2f, -0.12f), new Vector3(0.11f, 0.045f, 0.11f), rim, new Vector3(0, 0, 90));
            }
            foreach (var f in new[] { FootL, FootR }) B(f, new Vector3(0, 0.03f, 0.05f), new Vector3(0.22f, 0.1f, 0.3f), tr);
            // helmet with a crest, ear fins and a visor
            S(HeadC, new Vector3(0, 0.08f, -0.03f), new Vector3(0.86f, 0.74f, 0.8f), paint);
            B(HeadC, new Vector3(0, hatY + 0.03f, 0.05f), new Vector3(0.06f, 0.12f, 0.5f), tr);
            foreach (var sd in new[] { -1f, 1f }) { B(HeadC, new Vector3(sd * 0.43f, 0.08f, 0), new Vector3(0.05f, 0.24f, 0.12f), tr); Cy(HeadC, new Vector3(sd * 0.45f, 0.18f, 0), new Vector3(0.03f, 0.08f, 0.03f), rim); }
            B(HeadC, new Vector3(0, -0.2f, faceZ + 0.0f), new Vector3(0.3f, 0.1f, 0.06f), tr);   // mouth plate
            Rivets(t, rim);
            Msh(Meshes.Star(), Torso, new Vector3(0, 0.3f, 0.34f), new Vector3(0.14f, 0.14f, 0.03f), G(body == "D7262B" ? "FF5252" : "64B5F6", 1.4f));
        }

        void PlaneRobot(Transform t)
        {
            var white = M("E8EEF5", 0.85f, 0.5f); var blue = M("1E5BD8", 0.6f, 0.4f); var glass = Glass("35E0FF", 0.6f);
            B(t, new Vector3(0, 0.24f, 0), new Vector3(0.66f, 0.6f, 0.6f), white);
            S(t, new Vector3(0, 0.36f, 0.3f), new Vector3(0.4f, 0.2f, 0.3f), glass);                                           // cockpit
            Cone(t, new Vector3(0, 0.2f, 0.32f), new Vector3(0.3f, 0.3f, 0.3f), white, new Vector3(90, 0, 0));                 // nose
            foreach (var sd in new[] { -1f, 1f })
            {
                // wings on the back with engines
                var wing = B(t, new Vector3(sd * 0.6f, 0.25f, -0.25f), new Vector3(0.75f, 0.04f, 0.3f), white, new Vector3(0, sd * -15, sd * 8));
                B(wing.transform, new Vector3(0.45f * sd / 0.75f, 0, 0), new Vector3(0.1f, 2f, 1f), blue);
                Cy(t, new Vector3(sd * 0.55f, 0.15f, -0.2f), new Vector3(0.12f, 0.12f, 0.12f), blue, new Vector3(90, 0, 0));
                Cy(t, new Vector3(sd * 0.55f, 0.15f, -0.33f), new Vector3(0.09f, 0.01f, 0.09f), G("FF9A1E", 2f), new Vector3(90, 0, 0));
                for (int k = 0; k < 6; k++) B(t, new Vector3(sd * 0.55f, 0.15f, -0.08f), new Vector3(0.01f, 0.1f, 0.01f), M("5B6370", 0.6f), new Vector3(0, 0, k * 30));
            }
            B(t, new Vector3(0, 0.6f, -0.3f), new Vector3(0.04f, 0.3f, 0.22f), blue);                                           // tail fin
            BodyKit.Band(t, Dt.Line(new Vector3(-0.34f, 0.1f, 0.31f), new Vector3(0.34f, 0.1f, 0.31f)), p => Vector3.forward, 8, 0.06f, 0.006f, blue);
            foreach (var a in new[] { ArmL, ArmR }) B(a, new Vector3(0, -0.14f, 0), new Vector3(0.19f, 0.28f, 0.19f), blue);
            foreach (var g in new[] { LegL, LegR }) { B(g, new Vector3(0, -0.14f, 0), new Vector3(0.22f, 0.3f, 0.22f), white); Cy(g, new Vector3(0, -0.25f, 0.1f), new Vector3(0.12f, 0.03f, 0.12f), M("1A1A1A", 0.3f), new Vector3(0, 0, 90)); }
            Hands(M("5B6370", 0.7f, 0.6f), 0.17f);
            S(HeadC, new Vector3(0, 0.08f, -0.03f), new Vector3(0.86f, 0.74f, 0.8f), white);
            B(HeadC, new Vector3(0, hatY + 0.03f, 0.02f), new Vector3(0.05f, 0.14f, 0.45f), blue);
            for (int k = 0; k < 10; k++) Ls(Torso, new Vector3(-0.3f + (k % 5) * 0.15f, 0.48f, -0.1f + (k / 5) * 0.2f), 0.025f, M("5B6370", 0.6f));   // rivets
            Rivets(t, M("5B6370", 0.6f), 0.305f);
        }

        // ================= phones and computers =================

        void Phone(Transform t, bool keypad)
        {
            var body = keypad ? M("37474F", 0.5f) : M("15151A", 0.9f, 0.3f);
            var frame = M("C9D1DB", 0.9f, 0.8f);
            var screen = keypad ? G("9CCC65", 0.6f) : Mats.Emissive(null, Hex("101828"), Hex("2040A0") * 0.6f, null, 0.95f);
            B(t, new Vector3(0, 0.26f, 0), new Vector3(0.74f, 0.86f, 0.62f), body);
            foreach (var sd in new[] { -1f, 1f }) foreach (var z in new[] { -1f, 1f }) Cy(t, new Vector3(sd * 0.37f, 0.26f, z * 0.31f), new Vector3(0.05f, 0.43f, 0.05f), frame);
            if (!keypad)
            {
                B(t, new Vector3(0, 0.27f, 0.315f), new Vector3(0.66f, 0.78f, 0.01f), screen);
                string[] cols = { "FF5252", "FFC83A", "4CAF50", "35E0FF", "B57CFF", "FF8FC4" };
                for (int k = 0; k < 16; k++) B(t, new Vector3(-0.21f + (k % 4) * 0.14f, 0.52f - (k / 4) * 0.14f, 0.322f), new Vector3(0.1f, 0.1f, 0.01f), G(cols[k % cols.Length], 1.1f));
                B(t, new Vector3(0, -0.1f, 0.322f), new Vector3(0.2f, 0.02f, 0.01f), G("FFFFFF", 1f));
                // the camera block on the back
                B(t, new Vector3(-0.17f, 0.52f, -0.32f), new Vector3(0.26f, 0.26f, 0.03f), M("2A2A30", 0.8f));
                for (int k = 0; k < 3; k++) { Cy(t, new Vector3(-0.23f + (k % 2) * 0.12f, 0.58f - (k / 2) * 0.12f, -0.34f), new Vector3(0.09f, 0.015f, 0.09f), M("0A0A0A", 0.95f), new Vector3(90, 0, 0)); Ring(t, new Vector3(-0.23f + (k % 2) * 0.12f, 0.58f - (k / 2) * 0.12f, -0.345f), 0.045f, 0.008f, frame, new Vector3(90, 0, 0)); }
            }
            else
            {
                B(t, new Vector3(0, 0.5f, 0.315f), new Vector3(0.5f, 0.3f, 0.01f), screen);
                Prim.Text(t, "12:00", new Vector3(0, 0.5f, 0.33f), 2.6f, Hex("1B3A0F"));
                for (int k = 0; k < 12; k++)
                {
                    var p = new Vector3(-0.16f + (k % 3) * 0.16f, 0.24f - (k / 3) * 0.1f, 0.32f);
                    Cy(t, p, new Vector3(0.11f, 0.012f, 0.07f), M("CFD8DC", 0.4f), new Vector3(90, 0, 0));
                    Prim.Text(t, k < 9 ? (k + 1).ToString() : k == 9 ? "*" : k == 10 ? "0" : "#", p + new Vector3(0, 0, 0.012f), 1.6f, Hex("263238"));
                }
                Rod(t, new Vector3(0.28f, 0.68f, -0.15f), new Vector3(0.28f, 0.92f, -0.15f), 0.05f, body);   // antenna
                for (int k = 0; k < 15; k++) Cy(t, new Vector3(-0.08f + (k % 5) * 0.04f, 0.66f + (k / 5) * 0.03f, 0.316f), new Vector3(0.018f, 0.004f, 0.018f), M("1A1A1A", 0.5f), new Vector3(90, 0, 0));   // speaker holes
                S(t, new Vector3(0.28f, 0.94f, -0.15f), 0.07f, G("FF5252", 1.4f));
            }
            foreach (var sd in new[] { -1f, 1f }) B(t, new Vector3(0.375f, 0.4f + sd * 0.08f, 0), new Vector3(0.02f, 0.1f, 0.06f), frame);   // side buttons
            foreach (var a in new[] { ArmL, ArmR }) Ca(a, new Vector3(0, -0.14f, 0), new Vector3(0.16f, 0.15f, 0.16f), body);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.14f, 0), new Vector3(0.18f, 0.14f, 0.18f), body);
            Boots(body, frame, frame, 0);
            Cy(HeadC, new Vector3(0, hatY + 0.03f, 0), new Vector3(0.12f, 0.02f, 0.12f), G("FF5252", 1.4f));   // a notification dot
        }

        void Laptop(Transform t)
        {
            var alu = M("B0BEC5", 0.85f, 0.7f); var key = M("263238", 0.4f);
            var screen = Mats.Emissive(null, Hex("0F2A4A"), Hex("1E6FD0") * 0.7f, null, 0.95f);
            // the open lid on the chest, the keyboard as a tray at the waist
            B(t, new Vector3(0, 0.36f, 0.27f), new Vector3(0.72f, 0.5f, 0.04f), alu, new Vector3(-8, 0, 0));
            B(t, new Vector3(0, 0.37f, 0.295f), new Vector3(0.64f, 0.42f, 0.01f), screen, new Vector3(-8, 0, 0));
            for (int k = 0; k < 6; k++) B(t, new Vector3(-0.2f, 0.52f - k * 0.06f, 0.305f + k * 0.008f), new Vector3(0.16f + (k % 3) * 0.08f, 0.02f, 0.01f), G(k % 2 == 0 ? "7DFF9A" : "FFC83A", 1.0f), new Vector3(-8, 0, 0));   // code
            var tray = B(t, new Vector3(0, 0.06f, 0.45f), new Vector3(0.72f, 0.04f, 0.4f), alu);
            for (int k = 0; k < 40; k++) B(t, new Vector3(-0.3f + (k % 10) * 0.066f, 0.085f, 0.33f + (k / 10) * 0.055f), new Vector3(0.05f, 0.012f, 0.045f), key);
            B(t, new Vector3(0, 0.085f, 0.6f), new Vector3(0.2f, 0.005f, 0.08f), M("90A4AE", 0.8f, 0.6f));
            S(t, SC, SR * 2, M("ECEFF1", 0.3f));                                                                   // a light hoodie under it
            Msh(Meshes.Heart(), t, new Vector3(0, 0.38f, -0.32f), Vector3.one * 0.12f, M("FFFFFF", 0.6f));
            foreach (var sd in new[] { -1f, 1f }) Rod(t, new Vector3(sd * 0.34f, 0.06f, 0.3f), new Vector3(sd * 0.3f, 0.5f, 0.0f), 0.025f, M("455A64", 0.4f));   // straps
            Hands(M("ECEFF1", 0.3f), 0.17f);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.13f, 0), new Vector3(0.18f, 0.13f, 0.18f), M("37474F", 0.3f));
            Boots(M("FFFFFF", 0.4f), M("90A4AE", 0.3f), M("90A4AE", 0.3f), 0);
        }

        void MonitorHead(Transform t)
        {
            var case_ = M("212121", 0.7f); var stand = M("9E9E9E", 0.85f, 0.7f);
            // the head sits inside a monitor frame: bezel round the face, back casing, a stand down the back
            var fr = Node("Monitor", HeadC, new Vector3(0, 0.02f, 0));
            B(fr, new Vector3(0, 0.44f, 0.05f), new Vector3(1.02f, 0.08f, 0.6f), case_);
            B(fr, new Vector3(0, -0.42f, 0.05f), new Vector3(1.02f, 0.1f, 0.6f), case_);
            foreach (var sd in new[] { -1f, 1f }) B(fr, new Vector3(sd * 0.47f, 0.01f, 0.05f), new Vector3(0.08f, 0.94f, 0.6f), case_);
            B(fr, new Vector3(0, 0.01f, -0.3f), new Vector3(0.9f, 0.82f, 0.12f), case_);
            S(fr, new Vector3(0, 0.0f, 0.36f), new Vector3(0.88f, 0.8f, 0.02f), Glass("BFE6FF", 0.18f));
            Prim.Text(fr, "CHUBRIK", new Vector3(0, -0.42f, 0.36f), 1.6f, Hex("FFFFFF"));
            Cy(fr, new Vector3(0.38f, -0.42f, 0.355f), new Vector3(0.03f, 0.01f, 0.03f), G("7DFF9A", 2f), new Vector3(90, 0, 0));
            for (int k = 0; k < 8; k++) B(fr, new Vector3(-0.28f + k * 0.08f, 0.2f, -0.37f), new Vector3(0.03f, 0.3f, 0.02f), M("424242", 0.5f));   // vents
            Rod(t, new Vector3(0, 0.75f, -0.35f), new Vector3(0, 0.2f, -0.35f), 0.06f, stand);
            // a T-shirt with a cursor, and a mouse in the hand
            S(t, SC, SR * 2, M("1E88E5", 0.3f));
            Msh(Meshes.Star(), t, Dt.Ell(SC, SR, 70, 90, 1.05f), new Vector3(0.14f, 0.14f, 0.03f), G("FFFFFF", 1.2f));
            Dt.Stitches(t, BodyKit.SP(SC, SR, 150, 150, 0, 350), 24, 0.02f, 0.006f, M("1565C0", 0.3f));
            var mouse = S(HandR, new Vector3(0, -0.07f, 0.06f), new Vector3(0.12f, 0.07f, 0.17f), M("ECEFF1", 0.6f));
            B(mouse.transform, new Vector3(0, 0.45f, 0.25f), new Vector3(0.05f, 0.1f, 0.4f), M("90A4AE", 0.5f));
            Rod(HandR, new Vector3(0, -0.07f, 0.15f), new Vector3(0.05f, -0.4f, 0.1f), 0.012f, M("90A4AE", 0.5f));
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.13f, 0), new Vector3(0.18f, 0.13f, 0.18f), M("37474F", 0.3f));
            Boots(M("1E88E5", 0.4f), M("FFFFFF", 0.3f), M("FFFFFF", 0.3f), 0);
        }

        void GamingPC(Transform t)
        {
            var case_ = M("101014", 0.8f, 0.3f); var glass = Glass("9AA5B1", 0.25f);
            B(t, new Vector3(0, 0.26f, 0), new Vector3(0.7f, 0.86f, 0.64f), case_);
            B(t, new Vector3(0, 0.26f, 0.325f), new Vector3(0.62f, 0.78f, 0.01f), glass);
            // three RGB fans behind the glass + glowing strips
            for (int k = 0; k < 3; k++)
            {
                var c = new Vector3(0, 0.52f - k * 0.24f, 0.29f);
                var ring = Ring(t, c, 0.09f, 0.015f, G("FF3DF2", 2f), new Vector3(90, 0, 0));
                var hue = Add<Hue>(ring); hue.offset = k * 0.33f; hue.glow = 2f;
                var fan = Node("Fan", t, c, new Vector3(90, 0, 0));
                for (int b = 0; b < 7; b++) B(fan, Vector3.zero, new Vector3(0.025f, 0.01f, 0.16f), M("2A2A30", 0.5f), new Vector3(0, b * 360f / 7, 15));
                fan.gameObject.AddComponent<Spin>().speed = 600;
            }
            foreach (var sd in new[] { -1f, 1f }) { var s = B(t, new Vector3(sd * 0.33f, 0.26f, 0.32f), new Vector3(0.015f, 0.8f, 0.015f), G("35E0FF", 2f)); Add<Hue>(s).glow = 2f; }
            for (int k = 0; k < 10; k++) B(t, new Vector3(0.355f, 0.62f - k * 0.07f, -0.05f), new Vector3(0.01f, 0.03f, 0.4f), M("2A2A30", 0.6f));   // vents
            Cy(t, new Vector3(0.2f, 0.66f, 0.33f), new Vector3(0.06f, 0.01f, 0.06f), G("7DFF9A", 2f), new Vector3(90, 0, 0));
            // a headset and a keyboard in the hands
            Ring(HeadC, new Vector3(0, 0.08f, 0), 0.42f, 0.03f, M("1A1A1A", 0.6f), new Vector3(0, 0, 90));
            foreach (var sd in new[] { -1f, 1f }) { Cy(HeadC, new Vector3(sd * 0.42f, 0.0f, 0), new Vector3(0.2f, 0.05f, 0.2f), M("1A1A1A", 0.6f), new Vector3(0, 0, 90)); var g = Cy(HeadC, new Vector3(sd * 0.46f, 0.0f, 0), new Vector3(0.12f, 0.01f, 0.12f), G("FF3DF2", 2f), new Vector3(0, 0, 90)); Add<Hue>(g).glow = 2f; }
            Rod(HeadC, new Vector3(-0.44f, -0.05f, 0.05f), new Vector3(-0.15f, -0.22f, faceZ), 0.02f, M("1A1A1A", 0.6f));
            foreach (var a in new[] { ArmL, ArmR }) Ca(a, new Vector3(0, -0.14f, 0), new Vector3(0.16f, 0.15f, 0.16f), case_);
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.14f, 0), new Vector3(0.18f, 0.14f, 0.18f), case_);
            Boots(M("1A1A1A", 0.6f), G("35E0FF", 1.5f), case_, 0);
        }

        void Tablet(Transform t)
        {
            var frame = M("ECEFF1", 0.8f, 0.4f);
            var screen = Mats.Emissive(null, Hex("FFFFFF"), Hex("FFFFFF") * 0.2f, null, 0.9f);
            S(t, SC, SR * 2, M("FFB74D", 0.3f));
            Dt.Stitches(t, BodyKit.SP(SC, SR, 150, 150, 0, 350), 24, 0.02f, 0.006f, M("F57C00", 0.3f));
            // a big tablet held in front with a drawing on it, and a stylus in the right hand
            var tab = Node("Tablet", HandL, new Vector3(0.18f, -0.05f, 0.18f), new Vector3(-20, 30, 0));
            B(tab, Vector3.zero, new Vector3(0.5f, 0.38f, 0.03f), frame);
            B(tab, new Vector3(0, 0, 0.016f), new Vector3(0.44f, 0.32f, 0.005f), screen);
            string[] cols = { "FF5252", "FFC83A", "4CAF50", "35E0FF", "B57CFF" };
            for (int k = 0; k < 14; k++) Ls(tab, new Vector3(-0.15f + (k % 7) * 0.05f, -0.06f + Mathf.Sin(k) * 0.06f + (k / 7) * 0.08f, 0.02f), 0.035f, M(cols[k % cols.Length], 0.3f));
            Msh(Meshes.Heart(), tab, new Vector3(0.12f, 0.05f, 0.022f), Vector3.one * 0.08f, M("FF5252", 0.4f));
            Rod(HandR, new Vector3(0, -0.02f, 0.05f), new Vector3(0, -0.22f, 0.12f), 0.02f, M("FFFFFF", 0.6f));
            foreach (var g in new[] { LegL, LegR }) Cy(g, new Vector3(0, -0.13f, 0), new Vector3(0.18f, 0.13f, 0.18f), M("5C6BC0", 0.3f));
            Boots(M("FFFFFF", 0.4f), M("FFB74D", 0.3f), M("FFB74D", 0.3f), 0);
        }
    }
}
