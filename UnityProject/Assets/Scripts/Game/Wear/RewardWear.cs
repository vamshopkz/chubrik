using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Reward items (index 200+): the Shipozavr crown, the winter things from the Frosty lucky box.</summary>
    public partial class ChubrikModel
    {
        /// <summary>200 = Корона Шипозавра, 201 = Шапка-ушанка.</summary>
        void RewardHat(int i)
        {
            var h = Node("Hat", HeadC, new Vector3(0, hatY, 0));
            if (i == 200)
            {
                var gold = M("E8B730", 0.85f, 0.8f); var bone = M("F3EBD2", 0.45f); var green = G("4CE35A", 1.4f); var scale = M("3E8A3A", 0.5f, 0.2f);
                Cy(h, new Vector3(0, -0.02f, 0), new Vector3(0.6f, 0.07f, 0.6f), gold);
                Ring(h, new Vector3(0, 0.05f, 0), 0.3f, 0.02f, gold);
                Ring(h, new Vector3(0, -0.09f, 0), 0.3f, 0.02f, gold);
                // green dino scales round the band
                Dt.Scales(h, (u, v) => new Vector3(Mathf.Cos(u * Mathf.PI * 2) * 0.305f, -0.065f + v * 0.1f, Mathf.Sin(u * Mathf.PI * 2) * 0.305f),
                    (u, v) => new Vector3(Mathf.Cos(u * Mathf.PI * 2), 0.2f, Mathf.Sin(u * Mathf.PI * 2)), 3, 16, 0.06f, scale);
                // studs top and bottom
                for (int k = 0; k < 32; k++)
                {
                    float a = k * Mathf.PI * 2 / 32;
                    Ls(h, new Vector3(Mathf.Cos(a) * 0.31f, k % 2 == 0 ? 0.05f : -0.09f, Mathf.Sin(a) * 0.31f), 0.022f, gold);
                }
                // ten bone spikes, each on a glowing green socket
                for (int k = 0; k < 10; k++)
                {
                    float a = k * Mathf.PI * 2 / 10;
                    var b = new Vector3(Mathf.Cos(a) * 0.28f, 0.05f, Mathf.Sin(a) * 0.28f);
                    float hgt = k % 2 == 0 ? 0.26f : 0.17f;
                    Spike(h, b, b + new Vector3(Mathf.Cos(a) * 0.06f, hgt, Mathf.Sin(a) * 0.06f), 0.09f, bone);
                    Ls(h, b, 0.06f, green);
                }
                Dt.Gem(h, new Vector3(0, -0.02f, 0.31f), 0.09f, G("E53935", 1.6f), gold, Vector3.forward);
                return;
            }
            // Шапка-ушанка: fur crown, folded front flap with a red star, ear flaps with ties and pom-poms
            var fur = M("8A5A36", 0.15f); var furLight = M("C9A47E", 0.1f); var cloth = M("3B4A5C", 0.3f);
            S(h, new Vector3(0, 0.0f, 0), new Vector3(0.82f, 0.46f, 0.8f), cloth);
            Cy(h, new Vector3(0, -0.12f, 0), new Vector3(0.88f, 0.06f, 0.86f), fur);
            Dt.Fur(h, new Vector3(0, -0.12f, 0), new Vector3(0.44f, 0.07f, 0.43f), 46, 0.07f, furLight, 41, 120);
            var flap = Node("Flap", h, new Vector3(0, -0.02f, 0.36f), new Vector3(-20, 0, 0));
            B(flap, Vector3.zero, new Vector3(0.5f, 0.22f, 0.06f), fur);
            Dt.Fur(flap, Vector3.zero, new Vector3(0.25f, 0.11f, 0.04f), 22, 0.05f, furLight, 42);
            Msh(Meshes.Star(), flap, new Vector3(0, 0.01f, 0.045f), new Vector3(0.12f, 0.12f, 0.04f), M("E53935", 0.4f, 0.3f));
            Ring(flap, new Vector3(0, 0.01f, 0.042f), 0.075f, 0.008f, M("FFC83A", 0.8f, 0.7f), new Vector3(90, 0, 0));
            foreach (var sd in new[] { -1f, 1f })
            {
                var ear = Node(sd < 0 ? "EarFlapL" : "EarFlapR", h, new Vector3(sd * 0.4f, -0.2f, 0.02f), new Vector3(0, 0, sd * 8));
                S(ear, new Vector3(0, -0.06f, 0), new Vector3(0.1f, 0.3f, 0.24f), fur);
                Dt.Fur(ear, new Vector3(sd * 0.02f, -0.06f, 0), new Vector3(0.05f, 0.15f, 0.12f), 14, 0.045f, furLight, sd < 0 ? 43 : 44);
                LRod(ear, new Vector3(0, -0.2f, 0.02f), new Vector3(0, -0.36f, 0.06f), 0.012f, M("2A2A30"));
                Ls(ear, new Vector3(0, -0.38f, 0.06f), 0.06f, furLight);
            }
            Dt.Stitches(h, Dt.Circle(new Vector3(0, 0.12f, 0), 0.36f, 0.35f), 16, 0.03f, 0.008f, M("2A3646"));
            hairHidden = true;
        }

        /// <summary>200 = Красный шарф: knitted, two long ends that flutter when you run.</summary>
        void RewardNeck(int i)
        {
            var n = Node("Neck", Torso, new Vector3(0, 0.47f, 0));
            var red = M("D62828", 0.2f); var redDk = M("A31C1C", 0.2f); var white = M("F4F4F4", 0.2f);
            Ring(n, Vector3.zero, 0.25f, 0.075f, red, default, new Vector3(1, 1.2f, 1));
            for (int row = 0; row < 4; row++)
                for (int k = 0; k < 18; k++)
                {
                    float a = (k + row * 0.5f) * Mathf.PI * 2 / 18;
                    Ls(n, new Vector3(Mathf.Cos(a) * 0.3f, (row - 1.5f) * 0.045f, Mathf.Sin(a) * 0.3f), new Vector3(0.05f, 0.04f, 0.05f), row == 1 || row == 2 ? white : redDk);
                }
            for (int e = 0; e < 2; e++)
            {
                var end = Node(e == 0 ? "End" : "End2", n, new Vector3(0.12f + e * 0.08f, -0.02f, 0.22f), new Vector3(-12, 0, 10 + e * 14));
                var sw = end.gameObject.AddComponent<Sway>(); sw.model = this; sw.amp = 16; sw.speed = 4.5f + e; sw.phase = e * 1.3f;
                B(end, new Vector3(0, -0.18f, 0), new Vector3(0.11f, 0.36f, 0.03f), red);
                for (int k = 0; k < 5; k++) B(end, new Vector3(0, -0.04f - k * 0.07f, 0.017f), new Vector3(0.112f, 0.02f, 0.004f), white);
                for (int k = 0; k < 12; k++) LRod(end, new Vector3(-0.05f + k * 0.009f, -0.36f, 0), new Vector3(-0.05f + k * 0.009f, -0.43f, 0.01f), 0.007f, red);
            }
            Ls(n, new Vector3(0.14f, -0.01f, 0.24f), new Vector3(0.1f, 0.08f, 0.07f), red);   // the knot
        }

        /// <summary>200 = Ледяные коньки: white boots, laces, a steel blade on posts, frost crystals.</summary>
        void RewardShoes(int i)
        {
            var boot = M("F4F7FA", 0.4f); var sole = M("3B4A5C", 0.4f); var steel = M("D8E2EC", 0.95f, 0.95f); var lace = M("5FD3FF", 0.3f); var frost = G("BFE8FF", 1.2f);
            foreach (var f in new[] { FootL, FootR })
            {
                S(f, new Vector3(0, 0.03f, 0.02f), new Vector3(0.21f, 0.15f, 0.29f), boot);
                Cy(f, new Vector3(0, 0.1f, -0.01f), new Vector3(0.17f, 0.06f, 0.17f), boot);
                B(f, new Vector3(0, -0.04f, 0.02f), new Vector3(0.19f, 0.03f, 0.28f), sole);
                Dt.Laces(f, new Vector3(0, 0.07f, 0.13f), new Vector3(0, 0.14f, 0.02f), new Vector3(0.035f, 0, 0), 5, lace, steel);
                // blade with three posts and a curled toe
                B(f, new Vector3(0, -0.13f, 0.03f), new Vector3(0.02f, 0.03f, 0.36f), steel);
                Ls(f, new Vector3(0, -0.11f, 0.21f), new Vector3(0.02f, 0.06f, 0.05f), steel);
                for (int k = 0; k < 3; k++) B(f, new Vector3(0, -0.085f, -0.09f + k * 0.11f), new Vector3(0.035f, 0.06f, 0.03f), sole);
                Dt.Stitches(f, Dt.Circle(new Vector3(0, -0.015f, 0.02f), 0.1f, 0.145f), 14, 0.02f, 0.006f, M("9FB4C8"));
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 2 / 8;
                    Spike(f, new Vector3(Mathf.Cos(a) * 0.09f, 0.11f, Mathf.Sin(a) * 0.09f - 0.01f), new Vector3(Mathf.Cos(a) * 0.12f, 0.15f, Mathf.Sin(a) * 0.12f - 0.01f), 0.02f, frost);
                }
            }
        }
    }

    /// <summary>Алмазный Ледяной Чубрик: the skin shimmers in shades of icy blue.</summary>
    public class IceShimmer : MonoBehaviour
    {
        public Material skin;
        void Update()
        {
            if (skin == null) return;
            float t = Time.time;
            var c = Color.HSVToRGB(0.53f + Mathf.Sin(t * 0.8f) * 0.04f, 0.55f, 1f) * (0.35f + Mathf.Sin(t * 2.3f) * 0.12f);
            skin.SetColor("_EmissionColor", c);
        }
    }
}
