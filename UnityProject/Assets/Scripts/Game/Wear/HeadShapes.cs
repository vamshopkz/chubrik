using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Head shapes and faces (eyes, mouth, cheeks).</summary>
    public partial class ChubrikModel
    {
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
    }
}
