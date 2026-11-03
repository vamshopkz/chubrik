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
            var browM = M("4A2C1A", 0.45f);
            var shine = G("FFFFFF", 1f);
            var blush = M("FF8FA8", 0.2f);
            var freck = M("B86A3C", 0.3f);
            var line = Glass("2B1A1A", 0.35f);      // soft creases
            var metalL = M("C9D1DB", 0.8f, 0.6f);
            Mesh mid = Meshes.Ball(14, 10), tiny = Meshes.Ball(6, 4), cone6 = Meshes.Cone(6);
            string mouth = "smile", eyes = "normal";
            string iris = "6B4A2E", irisD = "2A1A10";
            bool box = false, face = true;          // face: nose, freckles, forehead gloss (human-like heads)
            Vector3 hc = Vector3.zero, hr = Dt.HeadR;

            // ---- local helpers (head surface in HeadC space) ----
            Vector3 Sf(float x, float y, float off = 0.003f)
            {
                if (box) return new Vector3(x, y, faceZ + off);
                float u = (x - hc.x) / hr.x, v = (y - hc.y) / hr.y;
                return new Vector3(x, y, hc.z + hr.z * Mathf.Sqrt(Mathf.Max(0.02f, 1 - u * u - v * v)) + off);
            }
            Vector3 Nm(Vector3 p) => box ? Vector3.forward : new Vector3((p.x - hc.x) / (hr.x * hr.x), (p.y - hc.y) / (hr.y * hr.y), (p.z - hc.z) / (hr.z * hr.z)).normalized;
            GameObject Mb(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 e = default) => Msh(mid, p, pos, sc, m, e);
            GameObject T(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 e = default) => Msh(tiny, p, pos, sc, m, e);
            GameObject Flat(GameObject go, Vector3 n) { go.transform.localRotation = Quaternion.LookRotation(n, Vector3.up); return go; }
            GameObject Dash(Transform p, Vector3 a, Vector3 c, float th, Material m, Vector3 n)
            {
                var go = B(p, (a + c) / 2, new Vector3(th, (c - a).magnitude * 1.1f, th * 0.6f), m);
                go.transform.localRotation = Quaternion.LookRotation(n, c - a);
                return go;
            }
            GameObject Sp(Transform p, Vector3 a, Vector3 c, float d, Material m)
            {
                var go = Msh(cone6, p, a, new Vector3(d, (c - a).magnitude, d), m);
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (c - a).normalized);
                return go;
            }

            switch (i)
            {
                case 1:
                {
                    B(HeadC, Vector3.zero, new Vector3(0.74f, 0.7f, 0.7f), Skin); faceZ = 0.35f; hatY = 0.35f;
                    box = true; iris = "2F6FD8"; irisD = "123060";
                    // rounded edges: piping along the 12 edges + corner balls
                    var hs = new Vector3(0.37f, 0.35f, 0.35f);
                    foreach (var s1 in new[] { -1, 1 })
                        foreach (var s2 in new[] { -1, 1 })
                        {
                            LRod(HeadC, new Vector3(s1 * hs.x, s2 * hs.y, -hs.z + 0.02f), new Vector3(s1 * hs.x, s2 * hs.y, hs.z - 0.02f), 0.05f, Skin);
                            LRod(HeadC, new Vector3(s1 * hs.x, -hs.y + 0.02f, s2 * hs.z), new Vector3(s1 * hs.x, hs.y - 0.02f, s2 * hs.z), 0.05f, Skin);
                            LRod(HeadC, new Vector3(-hs.x + 0.02f, s1 * hs.y, s2 * hs.z), new Vector3(hs.x - 0.02f, s1 * hs.y, s2 * hs.z), 0.05f, Skin);
                        }
                    foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                        Ls(HeadC, new Vector3(sx * hs.x, sy * hs.y, sz * hs.z), 0.05f, Skin);
                    break;
                }
                case 2:
                {
                    S(HeadC, Vector3.zero, new Vector3(0.94f, 0.74f, 0.82f), Skin); faceZ = 0.41f; hatY = 0.37f; eyeX = 0.17f;
                    hr = new Vector3(0.47f, 0.37f, 0.41f); iris = "3E7D3A"; irisD = "17331A";
                    // chubby cheeks + soft chin
                    foreach (var sd in new[] { -1, 1 }) Mb(HeadC, Sf(sd * 0.3f, -0.17f, -0.045f), new Vector3(0.2f, 0.15f, 0.14f), Skin);
                    Mb(HeadC, Sf(0, -0.28f, -0.04f), new Vector3(0.2f, 0.09f, 0.1f), Skin);
                    break;
                }
                case 3:
                {
                    S(HeadC, Vector3.zero, new Vector3(0.74f, 0.84f, 0.74f), Skin);
                    Cone(HeadC, new Vector3(0, 0.3f, 0), new Vector3(0.36f, 0.32f, 0.36f), Skin);
                    faceZ = 0.37f; hatY = 0.44f; eyeX = 0.13f;
                    hr = new Vector3(0.37f, 0.42f, 0.37f); iris = "8A5A20"; irisD = "3A2208";
                    // pointed ears with pink insides and tip tufts
                    var pinkIn = M("FF9AB0", 0.3f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Vector3 eb = new Vector3(sd * 0.33f, 0.04f, -0.02f), et = new Vector3(sd * 0.55f, 0.22f, -0.07f);
                        Spike(HeadC, eb, et, 0.13f, Skin);
                        Spike(HeadC, eb + new Vector3(sd * 0.01f, 0.01f, 0.035f), Vector3.Lerp(eb, et, 0.8f) + new Vector3(0, 0, 0.03f), 0.07f, pinkIn);
                        Sp(HeadC, et - (et - eb).normalized * 0.015f, et + new Vector3(sd * 0.02f, 0.04f, 0), 0.02f, Skin);
                    }
                    // curl at the top of the point
                    Sp(HeadC, new Vector3(0, 0.58f, 0), new Vector3(0.03f, 0.68f, 0.02f), 0.04f, Skin);
                    Sp(HeadC, new Vector3(0, 0.58f, 0), new Vector3(-0.035f, 0.66f, -0.01f), 0.035f, Skin);
                    Sp(HeadC, new Vector3(0, 0.57f, 0), new Vector3(0, 0.64f, -0.04f), 0.03f, Skin);
                    break;
                }
                case 4:
                {
                    S(HeadC, Vector3.zero, new Vector3(0.8f, 0.78f, 0.76f), Skin); mouth = "grin"; iris = "2F8F5A"; irisD = "103A22";
                    // happy crinkles at the outer eye corners
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var a = Sf(sd * (eyeX + 0.105f), eyeY + 0.02f); var c = Sf(sd * (eyeX + 0.14f), eyeY + 0.04f);
                        Dash(HeadC, a, c, 0.01f, line, Nm(a));
                        a = Sf(sd * (eyeX + 0.105f), eyeY - 0.01f); c = Sf(sd * (eyeX + 0.14f), eyeY - 0.025f);
                        Dash(HeadC, a, c, 0.01f, line, Nm(a));
                    }
                    break;
                }
                case 5:
                {
                    S(HeadC, Vector3.zero, new Vector3(0.8f, 0.78f, 0.76f), Skin); mouth = "flat"; eyes = "serious"; iris = "4A5A6A"; irisD = "1C242C";
                    // frown furrows, under-eye lines, chin cleft
                    foreach (var sd in new[] { -1, 1 }) Dash(HeadC, Sf(sd * 0.02f, 0.15f), Sf(sd * 0.024f, 0.19f), 0.008f, line, Nm(Sf(0, 0.17f)));
                    Dash(HeadC, Sf(-0.05f, 0.25f), Sf(0.05f, 0.25f), 0.008f, line, Nm(Sf(0, 0.25f)));
                    foreach (var sd in new[] { -1, 1 }) Dash(HeadC, Sf(sd * (eyeX - 0.04f), eyeY - 0.12f), Sf(sd * (eyeX + 0.04f), eyeY - 0.115f), 0.008f, line, Nm(Sf(sd * eyeX, eyeY - 0.12f)));
                    Dash(HeadC, Sf(0, -0.225f), Sf(0, -0.25f), 0.008f, line, Nm(Sf(0, -0.24f)));
                    // band-aid on the jaw
                    var bp = Sf(0.17f, -0.2f, 0.002f); var bq = Quaternion.LookRotation(Nm(bp), Vector3.up) * Quaternion.Euler(0, 0, 30);
                    var tape = M("E8B888", 0.3f);
                    B(HeadC, bp, new Vector3(0.11f, 0.035f, 0.006f), tape).transform.localRotation = bq;
                    B(HeadC, bp + bq * new Vector3(0, 0, 0.002f), new Vector3(0.036f, 0.03f, 0.008f), M("F5DEC0", 0.3f)).transform.localRotation = bq;
                    foreach (var sd in new[] { -1, 1 })
                    {
                        T(HeadC, bp + bq * new Vector3(sd * 0.055f, 0, 0), new Vector3(0.035f, 0.035f, 0.006f), tape).transform.localRotation = bq;
                        foreach (var x in new[] { 0.028f, 0.042f }) T(HeadC, bp + bq * new Vector3(sd * x, 0, 0.004f), new Vector3(0.006f, 0.006f, 0.003f), M("C8955E", 0.3f));
                    }
                    break;
                }
                case 6:
                {
                    S(HeadC, new Vector3(0, 0.06f, 0), new Vector3(0.78f, 0.98f, 0.76f), Skin); mouth = "tiny"; eyes = "alien"; hatY = 0.55f;
                    hc = new Vector3(0, 0.06f, 0); hr = new Vector3(0.39f, 0.49f, 0.38f); face = false;
                    var glow = G("5BFFB0", 1.2f);
                    // third-eye gem with a row of glowing dots on each side
                    var gp = Sf(0, 0.25f, -0.004f);
                    Dt.Gem(HeadC, gp, 0.05f, G("5BFFB0", 2f), metalL, Nm(gp));
                    foreach (var sd in new[] { -1, 1 })
                        for (int k = 0; k < 4; k++) { var p = Sf(sd * (0.07f + k * 0.045f), 0.34f - k * 0.018f, 0.001f); Flat(T(HeadC, p, new Vector3(0.022f, 0.022f, 0.008f), glow), Nm(p)); }
                    // brow ridges, nostril slits, cheek spots, glow lines under the eyes
                    var spot = Glass("1A1A2E", 0.35f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var rp = Sf(sd * eyeX, eyeY + 0.2f, -0.015f);
                        Flat(Mb(HeadC, rp, new Vector3(0.16f, 0.05f, 0.05f), Skin), Nm(rp)).transform.localRotation *= Quaternion.Euler(0, 0, -sd * 15);
                        Dash(HeadC, Sf(sd * 0.016f, -0.04f), Sf(sd * 0.016f, -0.062f), 0.008f, dark, Vector3.forward);
                        for (int k = 0; k < 3; k++) { var p = Sf(sd * (0.2f + k * 0.025f), -0.07f + (k == 1 ? 0.02f : 0), 0.001f); Flat(T(HeadC, p, new Vector3(0.02f, 0.02f, 0.006f), spot), Nm(p)); }
                        var lp = Sf(sd * eyeX, eyeY - 0.13f, 0.002f);
                        Flat(B(HeadC, lp, new Vector3(0.08f, 0.008f, 0.006f), glow), Nm(lp)).transform.localRotation *= Quaternion.Euler(0, 0, sd * 12);
                    }
                    // brain lobes on top, gill slits on the sides, small antenna nubs
                    foreach (var ph in new[] { -20f, 200f })
                        foreach (var th in new[] { 20f, 38f, 56f }) Ls(HeadC, Dt.Ell(hc, hr, th, ph, 0.92f), 0.15f, Skin);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        float ph = sd > 0 ? 15 : 165;
                        for (int k = 0; k < 4; k++)
                        {
                            float th = 95 + k * 10;
                            Flat(B(HeadC, Dt.Ell(hc, hr, th, ph, 1.005f), new Vector3(0.008f, 0.05f, 0.012f), dark), Dt.EllN(hr, th, ph));
                        }
                        var nb = Sf(sd * 0.12f, 0.42f, -0.01f); var nt = nb + (Nm(nb) + Vector3.up).normalized * 0.06f;
                        LRod(HeadC, nb, nt, 0.025f, Skin);
                        Ls(HeadC, nt, 0.03f, glow);
                    }
                    break;
                }
                case 7:
                {
                    var metal = M("A8B3C2", 0.7f, 0.6f);
                    var slot = M("2A3140", 0.8f);
                    B(HeadC, Vector3.zero, new Vector3(0.76f, 0.68f, 0.7f), metal);
                    B(HeadC, new Vector3(0, 0, 0.352f), new Vector3(0.62f, 0.5f, 0.01f), slot);
                    Rod(HeadC, new Vector3(0, 0.34f, 0), new Vector3(0, 0.55f, 0), 0.03f, metal);
                    var ball = S(HeadC, new Vector3(0, 0.57f, 0), 0.08f, G("FF3B3B", 2f)); Add<Pulse>(ball);
                    foreach (var sd in new[] { -1, 1 }) Cy(HeadC, new Vector3(sd * 0.39f, 0, 0), new Vector3(0.14f, 0.03f, 0.14f), metal, new Vector3(0, 0, 90));
                    faceZ = 0.355f; hatY = 0.34f; mouth = "grill"; eyes = "led"; box = true; face = false;
                    // screen bezel with corner bolts
                    foreach (var sd in new[] { -1, 1 })
                    {
                        B(HeadC, new Vector3(0, sd * 0.262f, 0.355f), new Vector3(0.66f, 0.025f, 0.016f), metalL);
                        B(HeadC, new Vector3(sd * 0.322f, 0, 0.355f), new Vector3(0.025f, 0.55f, 0.016f), metalL);
                        foreach (var sy in new[] { -1, 1 })
                        {
                            var bp = new Vector3(sd * 0.322f, sy * 0.262f, 0.364f);
                            Lc(HeadC, bp, new Vector3(0.035f, 0.004f, 0.035f), metal, new Vector3(90, 0, 0));
                            B(HeadC, bp + new Vector3(0, 0, 0.005f), new Vector3(0.026f, 0.005f, 0.004f), slot, new Vector3(0, 0, 45));
                        }
                    }
                    // panel seams (top cross, sides, back) and top vents
                    B(HeadC, new Vector3(0, 0.341f, 0), new Vector3(0.74f, 0.006f, 0.008f), slot);
                    B(HeadC, new Vector3(0, 0.341f, 0), new Vector3(0.008f, 0.006f, 0.68f), slot);
                    foreach (var sd in new[] { -1, 1 }) B(HeadC, new Vector3(sd * 0.381f, 0, -0.22f), new Vector3(0.006f, 0.66f, 0.008f), slot);
                    B(HeadC, new Vector3(0, 0.15f, -0.351f), new Vector3(0.74f, 0.008f, 0.006f), slot);
                    for (int k = 0; k < 4; k++) B(HeadC, new Vector3(0.2f, 0.342f, -0.22f + k * 0.05f), new Vector3(0.12f, 0.006f, 0.022f), slot);
                    // antenna collar and coils
                    Lc(HeadC, new Vector3(0, 0.345f, 0), new Vector3(0.09f, 0.012f, 0.09f), metalL);
                    for (int k = 0; k < 3; k++) Lc(HeadC, new Vector3(0, 0.4f + k * 0.04f, 0), new Vector3(0.05f, 0.004f, 0.05f), metalL);
                    // ear caps: vent slits, status LED, rivets around
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Lc(HeadC, new Vector3(sd * 0.425f, 0, 0), new Vector3(0.1f, 0.008f, 0.1f), M("6B7685", 0.7f, 0.5f), new Vector3(0, 0, 90));
                        for (int k = -1; k <= 1; k++) B(HeadC, new Vector3(sd * 0.434f, 0.012f, k * 0.028f), new Vector3(0.004f, 0.03f, 0.008f), slot);
                        T(HeadC, new Vector3(sd * 0.434f, -0.032f, 0), new Vector3(0.008f, 0.016f, 0.016f), G("8BE03A", 2f));
                        foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) T(HeadC, new Vector3(sd * 0.381f, sy * 0.11f, sz * 0.11f), Vector3.one * 0.022f, metalL);
                    }
                    // LED brows, cheek lights, battery bars
                    foreach (var sd in new[] { -1, 1 }) B(HeadC, new Vector3(sd * eyeX, 0.13f, 0.361f), new Vector3(0.1f, 0.014f, 0.008f), G("35E0FF", 1.6f), new Vector3(0, 0, -sd * 8));
                    T(HeadC, new Vector3(-0.22f, -0.1f, 0.358f), new Vector3(0.03f, 0.03f, 0.01f), G("8BE03A", 2f));
                    T(HeadC, new Vector3(0.22f, -0.1f, 0.358f), new Vector3(0.03f, 0.03f, 0.01f), G("FF9A2E", 2f));
                    for (int k = 0; k < 3; k++) B(HeadC, new Vector3(-0.25f + k * 0.018f, -0.18f, 0.359f), new Vector3(0.012f, 0.03f, 0.006f), G("8BE03A", 1.4f));
                    // chin plate, back hatch, rivet row above the screen
                    B(HeadC, new Vector3(0, -0.3f, 0.355f), new Vector3(0.3f, 0.05f, 0.012f), metalL);
                    foreach (var sd in new[] { -1, 1 }) T(HeadC, new Vector3(sd * 0.12f, -0.3f, 0.362f), Vector3.one * 0.018f, metal);
                    B(HeadC, new Vector3(0, -0.05f, -0.355f), new Vector3(0.3f, 0.22f, 0.012f), metalL);
                    foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) T(HeadC, new Vector3(sx * 0.13f, -0.05f + sy * 0.09f, -0.362f), Vector3.one * 0.018f, metal);
                    for (int k = 0; k < 6; k++) T(HeadC, new Vector3(-0.25f + k * 0.1f, 0.305f, 0.352f), Vector3.one * 0.02f, metalL);
                    break;
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
                    {
                        var rot = new Vector3(0, 0, sd * 25);
                        Mb(e, new Vector3(0, 0.03f, 0.005f), new Vector3(0.226f, 0.296f, 0.08f), M("1F3A2A", 0.8f), rot);   // rim
                        S(e, new Vector3(0, 0.03f, 0.02f), new Vector3(0.2f, 0.27f, 0.1f), M("101010", 0.95f), rot);
                        Mb(e, new Vector3(-sd * 0.02f, -0.04f, 0.058f), new Vector3(0.09f, 0.05f, 0.02f), Add("7A4CFF", 0.35f), rot);   // sheen
                        Mb(e, new Vector3(sd * 0.03f, 0.08f, 0.07f), Vector3.one * 0.04f, shine);
                        T(e, new Vector3(sd * 0.055f, 0.03f, 0.068f), Vector3.one * 0.018f, shine);
                        T(e, new Vector3(-sd * 0.03f, -0.05f, 0.06f), Vector3.one * 0.011f, shine);
                        Msh(Meshes.Star(4, 0.3f, 0.2f), e, new Vector3(0, 0.13f, 0.06f), Vector3.one * 0.035f, shine);
                        break;
                    }
                    case "led":
                        B(e, new Vector3(0, 0, 0.062f), new Vector3(0.17f, 0.085f, 0.012f), M("12324A", 0.8f));   // housing
                        B(e, new Vector3(0, 0, 0.07f), new Vector3(0.14f, 0.06f, 0.02f), G("35E0FF", 2.2f));
                        for (int k = -1; k <= 1; k += 2) B(e, new Vector3(0, k * 0.012f, 0.081f), new Vector3(0.12f, 0.004f, 0.003f), G("0E8FB0", 1.4f));
                        B(e, new Vector3(sd * 0.04f, 0.015f, 0.082f), new Vector3(0.025f, 0.015f, 0.004f), G("E8FDFF", 2.5f));
                        break;
                    default:
                    {
                        S(e, Vector3.zero, new Vector3(0.17f, 0.21f, 0.1f), M("FFFFFF", 0.6f));
                        var lim = Mb(e, new Vector3(0, -0.01f, 0.033f), new Vector3(0.135f, 0.165f, 0.03f), M(irisD, 0.8f)); Pupils.Add(lim);
                        var ir = Mb(e, new Vector3(0, -0.01f, 0.043f), new Vector3(0.118f, 0.146f, 0.03f), M(iris, 0.85f)); Pupils.Add(ir);
                        var pu = Mb(e, new Vector3(0, -0.012f, 0.057f), new Vector3(0.078f, 0.098f, 0.03f), M("161616", 0.9f)); Pupils.Add(pu);
                        var hl = Ls(pu.transform, new Vector3(0.25f, 0.3f, 0.5f), new Vector3(0.44f, 0.38f, 0.3f), shine); Pupils.Add(hl);
                        T(pu.transform, new Vector3(-0.3f, -0.32f, 0.5f), new Vector3(0.2f, 0.18f, 0.2f), shine);
                        T(pu.transform, new Vector3(0.42f, -0.1f, 0.45f), Vector3.one * 0.12f, shine);
                        // lower lash at the outer corner
                        { float a = -28 * Mathf.Deg2Rad; var p0 = new Vector3(sd * Mathf.Cos(a) * 0.082f, Mathf.Sin(a) * 0.1f, 0.012f); Sp(e, p0, p0 + new Vector3(sd * 1.2f, -0.6f, 0.2f).normalized * 0.026f, 0.012f, dark); }
                        if (eyes == "serious")
                        {
                            S(e, new Vector3(0, 0.07f, 0.01f), new Vector3(0.19f, 0.11f, 0.11f), Skin);   // half-closed lid
                            var lidP = new[] { new Vector3(-0.075f, 0.04f, 0.03f), new Vector3(-0.025f, 0.033f, 0.055f), new Vector3(0.025f, 0.033f, 0.055f), new Vector3(0.075f, 0.04f, 0.03f) };
                            for (int k = 0; k < 3; k++) Dash(e, lidP[k], lidP[k + 1], 0.012f, dark, Vector3.forward);
                            // thick brow: heavy inner part + thinner outer part
                            B(e, new Vector3(-sd * 0.03f, 0.1575f, 0.03f), new Vector3(0.09f, 0.036f, 0.03f), dark, new Vector3(0, 0, -sd * 14));
                            B(e, new Vector3(sd * 0.04f, 0.14f, 0.028f), new Vector3(0.08f, 0.026f, 0.028f), dark, new Vector3(0, 0, -sd * 14));
                        }
                        else
                        {
                            // upper lashes + eyelid line
                            for (int k = 0; k < 3; k++)
                            {
                                float a = (32 + k * 20) * Mathf.Deg2Rad;
                                var p0 = new Vector3(sd * Mathf.Cos(a) * 0.08f, Mathf.Sin(a) * 0.1f, 0.016f);
                                Sp(e, p0, p0 + new Vector3(sd * Mathf.Cos(a) * 1.3f, Mathf.Sin(a), 0.4f).normalized * (0.042f - k * 0.006f), 0.016f, dark);
                            }
                            for (int k = 0; k < 4; k++)
                            {
                                float a0 = (40 + k * 25) * Mathf.Deg2Rad, a1 = (65 + k * 25) * Mathf.Deg2Rad;
                                Dash(e, new Vector3(sd * Mathf.Cos(a0) * 0.083f, Mathf.Sin(a0) * 0.102f, 0.02f), new Vector3(sd * Mathf.Cos(a1) * 0.083f, Mathf.Sin(a1) * 0.102f, 0.02f), 0.011f, dark, Vector3.forward);
                            }
                        }
                        break;
                    }
                }
            }

            // eyebrows (the serious eyes carry their own)
            if (eyes == "normal")
                foreach (var sd in new[] { -1, 1 })
                {
                    System.Func<float, Vector3> bp = u => Sf(sd * (eyeX + (u - 0.5f) * 0.12f), eyeY + 0.135f + Mathf.Sin(u * Mathf.PI) * 0.022f);
                    for (int k = 0; k < 4; k++) { var a = bp(k / 4f); var c = bp((k + 1) / 4f); Dash(HeadC, a, c, 0.022f - Mathf.Abs(k - 1.5f) * 0.003f, browM, Nm((a + c) / 2)); }
                }

            // nose + forehead gloss
            if (face)
            {
                var np = Sf(0, -0.045f, -0.008f);
                Mb(HeadC, np, new Vector3(0.065f, 0.05f, 0.045f), Skin);
                foreach (var sd in new[] { -1, 1 }) T(HeadC, np + new Vector3(sd * 0.013f, -0.013f, 0.014f), new Vector3(0.014f, 0.009f, 0.008f), dark);
                T(HeadC, np + new Vector3(-0.01f, 0.01f, 0.019f), Vector3.one * 0.012f, Add("FFFFFF", 0.5f));
                var gp = Sf(-0.15f, 0.22f, -0.008f);
                Flat(Mb(HeadC, gp, new Vector3(0.11f, 0.06f, 0.03f), Add("FFFFFF", 0.18f)), Nm(gp)).transform.localRotation *= Quaternion.Euler(0, 0, 25);
            }

            Mouth = Node("Mouth", HeadC, new Vector3(0, -0.13f, faceZ - 0.02f));
            switch (mouth)
            {
                case "grin":
                    Mb(Mouth, new Vector3(0, -0.01f, 0), new Vector3(0.22f, 0.12f, 0.06f), dark);
                    for (int k = 0; k < 6; k++) B(Mouth, new Vector3(-0.055f + k * 0.022f, 0.029f - Mathf.Abs(k - 2.5f) * 0.002f, 0.02f), new Vector3(0.02f, 0.026f, 0.02f), M("FFFFFF"));
                    Mb(Mouth, new Vector3(0, -0.04f, 0.02f), new Vector3(0.1f, 0.04f, 0.03f), M("FF6B8B"));
                    B(Mouth, new Vector3(0, -0.036f, 0.034f), new Vector3(0.005f, 0.022f, 0.006f), M("E0466A", 0.4f));
                    foreach (var sd in new[] { -1, 1 })
                    {
                        T(Mouth, new Vector3(sd * 0.112f, 0.012f, -0.004f), Vector3.one * 0.026f, dark);
                        T(Mouth, new Vector3(sd * 0.15f, 0.035f, -0.022f), new Vector3(0.018f, 0.018f, 0.01f), dark);   // dimples
                    }
                    break;
                case "flat":
                    B(Mouth, Vector3.zero, new Vector3(0.12f, 0.022f, 0.02f), dark);
                    foreach (var sd in new[] { -1, 1 }) B(Mouth, new Vector3(sd * 0.066f, -0.008f, -0.002f), new Vector3(0.03f, 0.018f, 0.018f), dark, new Vector3(0, 0, -sd * 32));
                    break;
                case "tiny":
                    Mb(Mouth, Vector3.zero, new Vector3(0.05f, 0.03f, 0.02f), dark);
                    foreach (var sd in new[] { -1, 1 }) T(Mouth, new Vector3(sd * 0.032f, 0.006f, -0.002f), Vector3.one * 0.012f, dark);
                    break;
                case "grill":
                    B(Mouth, new Vector3(0, 0, 0.024f), new Vector3(0.23f, 0.09f, 0.004f), M("1A1F2A", 0.8f));   // grill frame
                    for (int k = 0; k < 4; k++) B(Mouth, new Vector3(-0.075f + k * 0.05f, 0, 0.03f), new Vector3(0.025f, 0.06f, 0.01f), G("35E0FF", 1.2f));
                    foreach (var sd in new[] { -1, 1 }) T(Mouth, new Vector3(sd * 0.1f, 0, 0.03f), Vector3.one * 0.016f, metalL);
                    break;
                default:
                    for (int k = -3; k <= 3; k++) { float x = k * 0.0213f; Mb(Mouth, new Vector3(x, x * x * 5.86f, 0), new Vector3(0.034f, 0.028f, 0.03f), dark); }
                    foreach (var sd in new[] { -1, 1 }) T(Mouth, new Vector3(sd * 0.084f, 0.036f, -0.003f), new Vector3(0.016f, 0.024f, 0.016f), dark, new Vector3(0, 0, sd * 30));
                    break;
            }

            // cheeks: blush with a shine dot + freckles between nose and blush
            if (i != 7 && i != 6)
                foreach (var sd in new[] { -1, 1 })
                {
                    var cp = Sf(sd * (eyeX + 0.09f), -0.07f, -0.006f);
                    var n = Nm(cp);
                    Flat(Mb(HeadC, cp, new Vector3(0.1f, 0.055f, 0.03f), blush), n);
                    T(HeadC, cp + n * 0.014f + new Vector3(-sd * 0.018f, 0.012f, 0), new Vector3(0.018f, 0.012f, 0.008f), Add("FFFFFF", 0.55f));
                    for (int k = 0; k < 3; k++)
                    {
                        var fp = Sf(sd * (eyeX - 0.03f + k * 0.026f), k == 1 ? -0.075f : -0.087f, 0.001f);
                        Flat(T(HeadC, fp, new Vector3(0.016f, 0.016f, 0.006f), freck), Nm(fp));
                    }
                }
        }
    }
}
