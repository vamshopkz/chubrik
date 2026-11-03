using System;
using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Hoodies and jackets (with sleeves), gloves and paws, wrist items, belts. Every item is built from 100+ detail parts.</summary>
    public partial class ChubrikModel
    {
        /// <summary>Sleeves on both arms; optional knit cuffs and seams (shoulder seam + outer seam).</summary>
        void Sleeves(Material m, float len, float fat = 1f, Material cuff = null, Material stitch = null, int ribs = 10)
        {
            float r = 0.095f * fat, h = Mathf.Max(0.1f, len * 0.5f + 0.06f), cy = -0.04f - len * 0.5f, yc = cy - h * 0.5f;
            foreach (var a in new[] { ArmL, ArmR })
            {
                Ca(a, new Vector3(0, cy, 0), new Vector3(0.19f * fat, h, 0.19f * fat), m);
                float sd = a == ArmL ? -1 : 1;
                if (cuff != null) BodyKit.Knit(a, new Vector3(0, yc - 0.01f, 0), r * 1.04f, r * 1.04f, 0.05f, ribs, cuff, cuff);
                if (stitch == null) continue;
                float a0 = sd < 0 ? 70 : -110;
                Dt.Stitches(a, Dt.Circle(new Vector3(0, cy + h * 0.45f, 0), r * 1.03f, r * 1.03f, a0, a0 + 220), 9, 0.018f, 0.006f, stitch);
                Dt.Stitches(a, Dt.Line(new Vector3(sd * r * 1.03f, cy + h * 0.35f, 0), new Vector3(sd * r * 1.03f, yc + 0.02f, 0)), Mathf.Max(3, Mathf.RoundToInt(len * 28)), 0.018f, 0.006f, stitch);
            }
        }

        void Top(int i, int set)
        {
            var t = Node("Top", Torso, Vector3.zero);
            Vector3 C = new Vector3(0, 0.27f, 0), R = new Vector3(0.35f, 0.25f, 0.31f);
            void Shell(Material m, float s = 1f) { R = new Vector3(0.35f, 0.25f, 0.31f) * s; S(t, C, R * 2, m); }
            Vector3 P(float th, float ph, float g = 1.02f) => BodyKit.E(C, R, th, ph, g);
            Vector3 N(float th, float ph) => Dt.EllN(R, th, ph);
            GameObject F(float th, float ph, Vector3 size, Material m, float g = 1.02f, float roll = 0) => BodyKit.Flat(t, C, R, th, ph, size, m, g, roll);
            GameObject On(GameObject go, float th, float ph, float roll = 0) => BodyKit.Face(go, N(th, ph), roll);
            void Sm(float th0, float th1, float ph0, float ph1, int n, Material m, float g = 1.025f) => Dt.Stitches(t, BodyKit.SP(C, R, th0, th1, ph0, ph1, g), n, 0.018f, 0.006f, m);
            void Pn(float th0, float th1, float ph0, float ph1, int nu, int nv, float depth, Material m, float g = 1.02f) => BodyKit.Panel(t, C, R, th0, th1, ph0, ph1, nu, nv, depth, m, g);
            void Hem(float th, int n, Material m, float g = 1.025f) => Sm(th, th, 0, 360f * (n - 1) / n, n, m, g);
            void Strip(float th0, float th1, float ph0, float ph1, int n, float w, float depth, Material m, float g = 1.03f) => BodyKit.Band(t, BodyKit.SP(C, R, th0, th1, ph0, ph1, g), BodyKit.EN(C, R), n, w, depth, m);
            void Star(float th, float ph, float d, Material m, float g = 1.03f) => On(Msh(Meshes.Star(), t, P(th, ph, g), new Vector3(d, d, d * 0.3f), m), th, ph);
            void Crest(float th, float ph, Material ring, Material moon, Material cut)    // round crest with a crescent moon
            {
                On(BodyKit.Tor(t, P(th, ph, 1.03f), 0.03f, 0.005f, ring, default, null, 10), th, ph).transform.localRotation *= Quaternion.Euler(90, 0, 0);
                BodyKit.Disc(t, C, R, th, ph, 0.042f, 0.004f, moon, 1.035f);
                BodyKit.Disc(t, C, R, th - 3, ph + 3, 0.036f, 0.004f, cut, 1.045f);
            }
            void Zip(Func<float, Vector3> path, int teeth, Material metal, Material tape)    // zipper following a curved path
            {
                var nrm = BodyKit.EN(C, R);
                BodyKit.Band(t, path, nrm, Mathf.Max(2, teeth / 5), 0.024f, 0.006f, tape);
                Dt.Path(path, teeth, (p, d, k) =>
                {
                    var n = nrm(p); var sd = Vector3.Cross(d, n).normalized;
                    var go = B(t, p + n * 0.004f + sd * (k % 2 == 0 ? 0.004f : -0.004f), new Vector3(0.012f, 0.006f, 0.007f), metal);
                    go.transform.localRotation = Quaternion.LookRotation(n, d);
                });
                Vector3 e = path(1), e2 = path(0.97f), dir = (e - e2).normalized, ne = nrm(e);
                B(t, e + ne * 0.009f, new Vector3(0.018f, 0.022f, 0.01f), metal).transform.localRotation = Quaternion.LookRotation(ne, dir);
                B(t, e - dir * 0.022f + ne * 0.014f, new Vector3(0.012f, 0.03f, 0.005f), metal).transform.localRotation = Quaternion.LookRotation(ne, dir);
            }
            void FurCoat(Material m, Material shade, Material clasp, Material loop, int seed, bool leopard)
            {
                C = new Vector3(0, 0.23f, 0); R = new Vector3(0.39f, 0.31f, 0.35f);
                S(t, C, R * 2, m);
                BodyKit.Tufts(t, C, R, leopard ? 4 : 5, 10, 28, 150, 0.11f, m, seed);              // fluffy tufts
                BodyKit.Tufts(t, C, R, 2, 6, 60, 130, 0.075f, shade, seed + 1, 1.05f);             // darker clumps
                var rg = new Rng(seed + 2);
                for (int k = 0; k < 14; k++)                                                        // fur strands
                {
                    float th = rg.F(35, 140), ph = rg.F(0, 360);
                    var a = P(th, ph, 1.04f);
                    BodyKit.Cn(t, a, a + N(th, ph) * 0.06f + Vector3.down * 0.02f, 0.035f, shade);
                }
                Ring(t, new Vector3(0, 0.46f, 0), 0.24f, 0.07f, m);                                 // big collar
                Dt.Path(BodyKit.Loop(new Vector3(0, 0.48f, 0), 0.26f, 0.24f, 16), 16, (p, d, k) => BodyKit.Ball(t, p, k % 2 == 0 ? 0.1f : 0.08f, k % 3 == 0 ? shade : m));
                Dt.Path(BodyKit.SP(C, R, 42, 128, 90, 90, 1.08f), 7, (p, d, k) => BodyKit.Ball(t, p, 0.05f, shade));   // front edge
                for (int k = 0; k < 3; k++)                                                         // toggles / ice clasps with loops
                {
                    var c = P(68 + k * 20, 90, 1.17f);
                    if (leopard) BodyKit.Face(BodyKit.Ball(t, c, new Vector3(0.035f, 0.05f, 0.025f), clasp), N(68 + k * 20, 90), 45);
                    else BodyKit.Pin(t, c, Vector3.right, 0.026f, 0.075f, clasp);
                    foreach (var sd in new[] { -1f, 1f }) BodyKit.Tor(t, c + new Vector3(sd * 0.045f, 0, -0.005f), 0.016f, 0.004f, loop, new Vector3(90, 0, 0), null, 10);
                }
                if (leopard)                                                                       // snow leopard rosettes
                    for (int k = 0; k < 7; k++)
                    {
                        float th = 50 + (k % 3) * 30 + rg.F(-6, 6), ph = 20 + k * 47 + rg.F(-8, 8);
                        BodyKit.Ball(t, P(th, ph, 1.08f), 0.045f, M("D9D2C0", 0.1f));
                        for (int s = 0; s < 5; s++) { float a = s * 72 * Mathf.Deg2Rad; BodyKit.Dot(t, P(th + Mathf.Cos(a) * 7, ph + Mathf.Sin(a) * 8, 1.08f), 0.04f, M("3A3F48", 0.1f)); }
                    }
                Sleeves(m, 0.24f, 1.25f);
                foreach (var arm in new[] { ArmL, ArmR })                                          // fur cuffs + sleeve tufts
                {
                    Dt.Path(BodyKit.Loop(new Vector3(0, -0.26f, 0), 0.12f, 0.12f, 10), 10, (p, d, k) => BodyKit.Ball(arm, p, 0.065f, k % 3 == 0 ? shade : m));
                    for (int k = 0; k < 3; k++) BodyKit.Ball(arm, new Vector3((arm == ArmL ? -1 : 1) * 0.11f, -0.06f - k * 0.06f, (k - 1) * 0.05f), 0.06f, m);
                }
            }

            switch (set >= 0 ? 100 + set : i)
            {
                case 0:     // oversize hoodie: hood, drawstrings, kangaroo pocket, knit ribs
                {
                    var m = M("7E57C2", 0.15f); var dk = M("6A45B0", 0.15f); var st = M("4E2F96", 0.2f); var wh = M("FFFFFF"); var met = M("D9DEE6", 0.9f, 0.9f);
                    C = new Vector3(0, 0.24f, 0); R = new Vector3(0.37f, 0.29f, 0.33f);
                    S(t, C, R * 2, m);
                    var hc = new Vector3(0, 0.5f, -0.22f); var hr = new Vector3(0.24f, 0.15f, 0.14f);
                    S(t, hc, hr * 2, m);                                                            // hood (down)
                    Dt.Stitches(t, BodyKit.SP(hc, hr, 25, 155, 270, 270, 1.03f), 8, 0.018f, 0.006f, st);
                    BodyKit.Tor(t, new Vector3(0, 0.47f, 0), 0.25f, 0.03f, dk, default, new Vector3(1, 1, 0.9f), 24);   // hood rim / drawstring channel
                    foreach (var sd in new[] { -1f, 1f })                                           // drawstrings with eyelets, knots and aglets
                    {
                        var pts = new Vector3[4];
                        for (int k = 0; k < 4; k++) pts[k] = BodyKit.Front(C, R, sd * (0.06f + k * 0.004f), 0.45f - k * 0.06f, 1.05f);
                        BodyKit.Tor(t, pts[0], 0.012f, 0.004f, met, new Vector3(90, 0, 0), null, 10);
                        for (int k = 0; k < 3; k++) LRod(t, pts[k], pts[k + 1], 0.014f, wh);
                        BodyKit.Dot(t, pts[3], 0.022f, wh);
                        Lc(t, pts[3] + Vector3.down * 0.022f, new Vector3(0.014f, 0.014f, 0.014f), met);
                    }
                    Pn(92, 116, 58, 122, 6, 1, 0.025f, dk, 1.02f);                                  // kangaroo pocket
                    foreach (var sd in new[] { -1f, 1f }) Strip(92, 116, 90 - sd * 26, 90 - sd * 33, 3, 0.02f, 0.03f, st, 1.04f);
                    Sm(91, 91, 66, 114, 9, st, 1.06f);
                    Sm(117, 117, 58, 122, 11, st, 1.06f);
                    BodyKit.Disc(t, C, R, 68, 66, 0.06f, 0.006f, wh, 1.03f);                         // chest logo
                    Star(68, 66, 0.045f, dk, 1.045f);
                    BodyKit.Knit(t, new Vector3(0, 0.065f, 0), 0.305f, 0.272f, 0.06f, 26, dk, st);    // waistband
                    Sm(50, 112, 0, 0, 8, st); Sm(50, 112, 180, 180, 8, st);                         // side seams
                    Sm(35, 80, 62, 15, 6, st); Sm(35, 80, 118, 165, 6, st);                         // raglan seams
                    Sleeves(m, 0.26f, 1.05f, dk, st, 10);
                    break;
                }
                case 1:     // T-shirt with a sequin-edged star
                {
                    var m = M("FAFAFA", 0.2f); var yel = M("FFC400", 0.4f); var org = M("FF8F00", 0.7f, 0.3f); var st = M("D6D6D6", 0.2f); var rib = M("E6E6E6", 0.2f);
                    Shell(m);
                    var sc = new Vector3(0, 0.28f, 0.31f); var sq = Quaternion.Euler(-12, 0, 0);
                    Msh(Meshes.Star(), t, sc, new Vector3(0.2f, 0.2f, 0.05f), yel, new Vector3(-12, 0, 0));
                    for (int e = 0; e < 10; e++)                                                    // sequins along the star outline
                    {
                        float a0 = Mathf.PI / 2 + e * Mathf.PI / 5, a1 = a0 + Mathf.PI / 5, r0 = e % 2 == 0 ? 0.1f : 0.042f, r1 = e % 2 == 0 ? 0.042f : 0.1f;
                        var p0 = new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0.027f); var p1 = new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0.027f);
                        for (int k = 0; k < 3; k++) BodyKit.Dot(t, sc + sq * Vector3.Lerp(p0, p1, 0.17f + k * 0.33f), 0.013f, org);
                    }
                    Star(60, 64, 0.05f, yel); Star(62, 116, 0.04f, yel); Star(95, 70, 0.035f, yel); Star(96, 112, 0.045f, yel);
                    BodyKit.Knit(t, new Vector3(0, 0.46f, 0), 0.228f, 0.2f, 0.03f, 18, m, rib);      // crew collar
                    Sleeves(m, 0.08f, 1f, rib, st, 8);
                    BodyKit.Tor(t, new Vector3(0, 0.184f, 0), 0.33f, 0.012f, m, default, new Vector3(1, 1, 0.88f), 24);   // hem
                    Hem(106, 18, st);
                    Sm(45, 108, 0, 0, 7, st); Sm(45, 108, 180, 180, 7, st);
                    Sm(38, 70, 30, 10, 5, st); Sm(38, 70, 150, 170, 5, st);                         // shoulder seams
                    F(36, 270, new Vector3(0.04f, 0.025f, 0.004f), st, 1.03f);                      // neck label
                    break;
                }
                case 2:     // tactical vest: plates, MOLLE webbing, mag pouches, radio
                {
                    var m = M("4E5A32", 0.3f); var dk = M("3E4826", 0.3f); var web = M("5A6640", 0.25f); var st = M("2F3620", 0.2f); var met = M("8A8F96", 0.7f, 0.7f); var blk = M("1E1E1E", 0.3f);
                    Shell(m, 1.02f);
                    Pn(42, 100, 58, 122, 4, 3, 0.03f, dk, 1.03f);                                   // front plate
                    Pn(42, 100, 238, 302, 4, 3, 0.03f, dk, 1.03f);                                  // back plate
                    foreach (var th in new[] { 58f, 72f, 86f })                                      // MOLLE rows with bar-tacks
                    {
                        Pn(th - 3, th + 3, 60, 120, 4, 1, 0.012f, web, 1.075f);
                        for (int k = 0; k < 5; k++) F(th, 60 + k * 15, new Vector3(0.008f, 0.03f, 0.01f), st, 1.09f);
                    }
                    foreach (var ph in new[] { 72f, 90f, 108f })                                     // magazine pouches
                    {
                        F(94, ph, new Vector3(0.075f, 0.1f, 0.05f), dk, 1.12f);
                        F(86, ph, new Vector3(0.08f, 0.035f, 0.055f), m, 1.14f);
                        BodyKit.Disc(t, C, R, 87, ph, 0.016f, 0.006f, met, 1.23f);
                        foreach (var sd in new[] { -1f, 1f }) Dt.Stitches(t, BodyKit.SP(C, R, 89, 101, ph + sd * 6, ph + sd * 6, 1.2f), 2, 0.016f, 0.005f, st);
                    }
                    foreach (var sd in new[] { -1f, 1f })                                           // shoulder straps with buckles
                    {
                        float k = Mathf.Sqrt(1 - Mathf.Pow(0.17f / R.x, 2));
                        Func<float, Vector3> sp = tt => { float u = Mathf.Lerp(15, 165, tt) * Mathf.Deg2Rad; return new Vector3(sd * 0.17f, C.y + R.y * k * Mathf.Sin(u) * 1.06f, R.z * k * Mathf.Cos(u) * 1.06f); };
                        BodyKit.Band(t, sp, BodyKit.EN(C, R), 7, 0.08f, 0.025f, dk);
                        Dt.Stitches(t, tt => { var q = sp(Mathf.Lerp(0.05f, 0.95f, tt)); return q + BodyKit.EN(C, R)(q) * 0.014f; }, 5, 0.018f, 0.006f, st);
                        var bp = sp(0.1f);
                        B(t, bp + Vector3.forward * 0.02f, new Vector3(0.07f, 0.025f, 0.012f), blk);
                        F(85, sd > 0 ? 0 : 180, new Vector3(0.06f, 0.03f, 0.015f), dk, 1.05f);       // side cummerbund strap + buckle
                        F(85, sd > 0 ? 8 : 172, new Vector3(0.025f, 0.035f, 0.02f), blk, 1.08f);
                    }
                    F(52, 64, new Vector3(0.05f, 0.075f, 0.04f), dk, 1.12f);                         // radio pouch + radio
                    F(45, 64, new Vector3(0.055f, 0.025f, 0.045f), m, 1.13f);
                    var ant = P(43, 60, 1.15f);
                    LRod(t, ant, ant + new Vector3(0, 0.12f, -0.01f), 0.01f, blk);
                    BodyKit.Dot(t, ant + new Vector3(0, 0.125f, -0.01f), 0.016f, blk);
                    foreach (var ph in new[] { 62f, 118f, 242f, 298f })                              // plate bolts
                        foreach (var th in new[] { 46f, 96f }) BodyKit.Disc(t, C, R, th, ph, 0.018f, 0.008f, met, 1.075f);
                    F(50, 104, new Vector3(0.07f, 0.045f, 0.008f), M("C2B280", 0.2f), 1.085f);       // name patch with a star
                    Star(50, 104, 0.035f, M("FFC400", 0.4f), 1.1f);
                    var hb = P(40, 270, 1.08f);                                                     // drag handle
                    LRod(t, hb + new Vector3(-0.05f, 0, 0), hb + new Vector3(-0.04f, 0.035f, -0.02f), 0.016f, blk);
                    LRod(t, hb + new Vector3(-0.04f, 0.035f, -0.02f), hb + new Vector3(0.04f, 0.035f, -0.02f), 0.016f, blk);
                    LRod(t, hb + new Vector3(0.04f, 0.035f, -0.02f), hb + new Vector3(0.05f, 0, 0), 0.016f, blk);
                    Hem(105, 20, st);
                    break;
                }
                case 3:     // deer sweater: 3D reindeer applique, snowflakes, knit ribs
                {
                    var m = Mats.Tex(Patterns.Sweater("C62828"), Color.white, 0.1f);
                    var red = M("C62828", 0.1f); var dr = M("9E1B1B", 0.1f); var wh = M("FFFFFF", 0.1f);
                    var brown = M("8D5524", 0.2f); var tan = M("D7B98A", 0.2f); var nose = M("FF1F1F", 0.8f); var blk = M("111111", 0.6f);
                    Shell(m);
                    On(BodyKit.Mid(t, P(68, 90, 1.03f), new Vector3(0.11f, 0.12f, 0.05f), brown), 68, 90);   // reindeer head
                    On(BodyKit.Ball(t, P(77, 90, 1.08f), new Vector3(0.07f, 0.05f, 0.03f), tan), 77, 90);
                    BodyKit.Ball(t, P(78, 90, 1.14f), 0.035f, nose);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        BodyKit.Dot(t, P(64, 90 + sd * 6, 1.1f), 0.016f, blk);
                        On(BodyKit.Ball(t, P(62, 90 + sd * 13, 1.06f), new Vector3(0.045f, 0.025f, 0.015f), brown), 62, 90 + sd * 13, sd * 30);
                        Vector3 a0 = P(58, 90 + sd * 7, 1.05f), a1 = P(44, 90 + sd * 17, 1.05f);
                        LRod(t, a0, a1, 0.012f, tan);                                               // antlers
                        LRod(t, Vector3.Lerp(a0, a1, 0.45f), P(48, 90 + sd * 7, 1.05f), 0.01f, tan);
                        LRod(t, Vector3.Lerp(a0, a1, 0.8f), P(40, 90 + sd * 12, 1.04f), 0.009f, tan);
                    }
                    foreach (var sf in new[] { new Vector2(55, 62), new Vector2(55, 118), new Vector2(92, 68), new Vector2(92, 112) })   // snowflakes
                    {
                        for (int k = 0; k < 3; k++) F(sf.x, sf.y, new Vector3(0.006f, 0.05f, 0.006f), wh, 1.03f, k * 60);
                        BodyKit.Dot(t, P(sf.x, sf.y, 1.04f), 0.014f, wh);
                    }
                    BodyKit.Knit(t, new Vector3(0, 0.185f, 0), 0.338f, 0.3f, 0.05f, 28, red, dr);     // ribbed hem
                    BodyKit.Knit(t, new Vector3(0, 0.455f, 0), 0.235f, 0.21f, 0.04f, 16, red, dr);    // ribbed collar
                    Sleeves(red, 0.26f, 1f, dr, dr, 10);
                    break;
                }
                case 4:     // sports tank top: numbers, piping, mesh panels
                {
                    var m = M("1E88E5", 0.3f); var wh = M("FFFFFF"); var dk = M("1565C0", 0.3f);
                    Shell(m, 0.99f);
                    BodyKit.Digit(t, C, R, 74, 85, 2, 0.1f, wh); BodyKit.Digit(t, C, R, 74, 96, 2, 0.1f, wh);           // "22" front
                    BodyKit.Digit(t, C, R, 66, 263, 2, 0.13f, wh); BodyKit.Digit(t, C, R, 66, 277, 2, 0.13f, wh);       // "22" back
                    foreach (var sd in new[] { -1f, 1f })                                           // arm-hole piping
                        BodyKit.Tor(t, new Vector3(sd * 0.32f, 0.32f, 0), 0.095f, 0.014f, wh).transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(sd, -0.5f, 0));
                    Strip(38, 66, 50, 90, 5, 0.025f, 0.01f, wh); Strip(38, 66, 130, 90, 5, 0.025f, 0.01f, wh);           // V-neck
                    Sm(40, 66, 52, 86, 6, dk, 1.05f); Sm(40, 66, 128, 94, 6, dk, 1.05f);
                    Pn(45, 108, -6, 6, 1, 6, 0.01f, wh); Pn(45, 108, 174, 186, 1, 6, 0.01f, wh);    // side stripes
                    foreach (var ph0 in new[] { 18f, 142f })                                         // ventilation mesh
                        for (int r = 0; r < 5; r++) for (int c = 0; c < 3; c++) BodyKit.Dot(t, P(52 + r * 11, ph0 + c * 10, 1.0f), 0.018f, dk);
                    BodyKit.Tor(t, new Vector3(0, 0.185f, 0), 0.327f, 0.012f, wh, default, new Vector3(1, 1, 0.88f), 24);   // hem piping
                    Hem(106, 20, dk);
                    F(55, 68, new Vector3(0.05f, 0.012f, 0.006f), wh, 1.03f, 20); F(57, 66, new Vector3(0.02f, 0.01f, 0.006f), wh, 1.03f, -40);   // logo
                    Sm(28, 50, 40, 20, 5, dk); Sm(28, 50, 140, 160, 5, dk);                         // strap seams
                    break;
                }
                case 5:     // biker leather jacket: asymmetric zip, lapels, studded epaulettes, zip pockets, belt
                {
                    var m = M("1C1C20", 0.75f, 0.1f); var chr = M("D9DEE6", 0.9f, 0.9f); var st = M("3A3A42", 0.3f); var tape = M("2A2A30", 0.3f);
                    Shell(m, 1.02f);
                    Zip(BodyKit.SP(C, R, 106, 40, 84, 104, 1.03f), 22, chr, tape);                  // main zipper
                    foreach (var sd in new[] { -1, 1 })
                    {
                        B(t, new Vector3(sd * 0.1f, 0.42f, 0.24f), new Vector3(0.12f, 0.16f, 0.03f), m, new Vector3(-25, sd * 25, sd * 20));   // lapels
                        BodyKit.Pin(t, new Vector3(sd * 0.14f, 0.37f, 0.272f), new Vector3(sd * 0.3f, 0.2f, 1), 0.016f, 0.006f, chr);
                        float ph = sd > 0 ? 0 : 180;                                                // epaulettes with snap + studs
                        F(48, ph, new Vector3(0.06f, 0.12f, 0.015f), m, 1.04f);
                        BodyKit.Disc(t, C, R, 38, ph, 0.02f, 0.008f, chr, 1.08f);
                        for (int k = 0; k < 4; k++) BodyKit.Dot(t, P(44 + k * 5, ph + (k % 2 == 0 ? 5 : -5), 1.075f), 0.012f, chr);
                        Zip(BodyKit.SP(C, R, 98, 88, 90 - sd * 30, 90 - sd * 18, 1.03f), 8, chr, tape);   // pocket zips
                        Dt.Zipper(sd < 0 ? ArmL : ArmR, new Vector3(sd * 0.1f, -0.26f, 0.02f), new Vector3(sd * 0.1f, -0.17f, 0.02f), 6, chr, null, new Vector3(sd, 0, 0));   // cuff zips
                        BodyKit.Tor(sd < 0 ? ArmL : ArmR, new Vector3(0, -0.26f, 0), 0.099f, 0.01f, m);
                    }
                    BodyKit.Tor(t, new Vector3(0, 0.47f, 0), 0.235f, 0.028f, m, default, new Vector3(1, 1, 0.9f), 24);   // collar band
                    Zip(BodyKit.SP(C, R, 58, 58, 105, 122, 1.03f), 6, chr, tape);                   // chest pocket
                    BodyKit.Band(t, BodyKit.SP(C, R, 104, 104, 0, 360, 1.05f), BodyKit.EN(C, R), 20, 0.05f, 0.015f, m);      // waist belt
                    var bc = P(104, 70, 1.12f);
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        B(t, bc + new Vector3(sx * 0.025f, 0, 0), new Vector3(0.008f, 0.055f, 0.01f), chr);
                        B(t, bc + new Vector3(0, sx * 0.024f, 0), new Vector3(0.058f, 0.008f, 0.01f), chr);
                    }
                    B(t, bc + new Vector3(-0.008f, 0, 0.004f), new Vector3(0.035f, 0.005f, 0.005f), chr);
                    Sm(50, 50, 200, 340, 12, st);                                                   // back yoke seam
                    Sleeves(m, 0.26f, 1f, null, st);
                    break;
                }
                case 6: FurCoat(M("FFC400", 0.05f), M("E6A800", 0.05f), M("6D4425", 0.4f), M("5A3418", 0.4f), 61, false); break;
                case 7: FurCoat(M("18181C", 0.05f), M("2C2C33", 0.05f), M("FFC83A", 0.9f, 0.9f), M("FFC83A", 0.9f, 0.9f), 71, false); break;
                case 101: FurCoat(Mats.Tex(Patterns.Skin(1, Hex("F4F7FA")), Color.white, 0.05f), M("DDE3EA", 0.05f), G("9FE8FF", 1.4f), M("D9DEE6", 0.9f, 0.9f), 101, true); break;
                case 103:   // night kimono: crossed collar, obi with cord and bow, moon crests, stars
                {
                    var m = M("1A1F3A", 0.2f); var wh = M("E8E8F0"); var pur = M("7B4BFF", 0.3f); var gold = M("FFC83A", 0.85f, 0.85f); var sil = M("C9D2E6", 0.85f, 0.7f);
                    Shell(m, 1.04f);
                    Strip(30, 80, 50, 100, 6, 0.05f, 0.015f, wh, 1.03f);                            // under collar
                    Strip(30, 104, 130, 62, 8, 0.05f, 0.015f, wh, 1.045f);                          // top collar (left over right)
                    Dt.Stitches(t, BodyKit.SP(C, R, 32, 102, 129, 63, 1.075f), 9, 0.016f, 0.005f, sil);
                    BodyKit.Band(t, Dt.Circle(new Vector3(0, 0.1f, 0), 0.32f, 0.29f), p => new Vector3(p.x / 0.1024f, 0, p.z / 0.0841f).normalized, 18, 0.11f, 0.03f, pur);   // obi
                    BodyKit.Tor(t, new Vector3(0, 0.1f, 0), 0.337f, 0.009f, gold, default, new Vector3(1, 1, 0.905f), 24);   // obi-jime cord
                    var kn = new Vector3(0, 0.1f, 0.312f);
                    BodyKit.Ball(t, kn, 0.035f, gold);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        LRod(t, kn, kn + new Vector3(sd * 0.03f, -0.08f, 0.01f), 0.008f, gold);
                        BodyKit.Cn(t, kn + new Vector3(sd * 0.034f, -0.11f, 0.012f), kn + new Vector3(sd * 0.03f, -0.075f, 0.01f), 0.02f, gold);   // tassels
                    }
                    B(t, new Vector3(0, 0.14f, -0.33f), new Vector3(0.22f, 0.14f, 0.08f), pur);       // taiko bow at the back
                    foreach (var sd in new[] { -1f, 1f }) BodyKit.Ball(t, new Vector3(sd * 0.1f, 0.16f, -0.34f), new Vector3(0.1f, 0.09f, 0.05f), pur);
                    Crest(56, 52, wh, wh, m); Crest(56, 128, wh, wh, m); Crest(45, 270, wh, wh, m);
                    var rg = new Rng(103);
                    for (int k = 0; k < 8; k++) Star(70 + rg.F(0, 30), 225 + k * 12, 0.035f, sil);  // embroidered stars
                    for (int k = 0; k < 6; k++) Star(95 + rg.F(0, 14), k < 3 ? 30 + k * 12 : 126 + (k - 3) * 12, 0.03f, sil);
                    Sleeves(m, 0.24f, 1.25f, null, sil);
                    foreach (var arm in new[] { ArmL, ArmR })                                      // sleeve hems and hanging sleeves
                    {
                        float sd = arm == ArmL ? -1 : 1;
                        BodyKit.Tor(arm, new Vector3(0, -0.25f, 0), 0.121f, 0.01f, wh);
                        B(arm, new Vector3(0, -0.29f, -0.03f), new Vector3(0.03f, 0.2f, 0.2f), m);
                        B(arm, new Vector3(0, -0.392f, -0.03f), new Vector3(0.035f, 0.012f, 0.2f), wh);
                        var cp = new Vector3(sd * 0.12f, -0.12f, 0);
                        BodyKit.Tor(arm, cp, 0.026f, 0.004f, wh, new Vector3(0, 0, 90), null, 10);
                        BodyKit.Pin(arm, cp + new Vector3(sd * 0.002f, 0, 0), new Vector3(sd, 0, 0), 0.036f, 0.004f, wh);
                        BodyKit.Pin(arm, cp + new Vector3(sd * 0.005f, 0.005f, 0.005f), new Vector3(sd, 0, 0), 0.03f, 0.004f, m);
                    }
                    break;
                }
                case 107:   // royal mantle: swaying cape with ermine, gold clasps and chain
                {
                    var red = M("9C0D2E", 0.25f); var erm = M("FFFFFF", 0.05f); var blk = M("111111"); var gold = M("FFC83A", 0.85f, 0.85f); var gem = G("FF3355", 1.2f);
                    Shell(red, 1.03f);
                    var cape = Node("Mantle", t, new Vector3(0, 0.46f, -0.24f), new Vector3(10, 0, 0));
                    B(cape, new Vector3(0, -0.4f, 0), new Vector3(0.8f, 0.82f, 0.03f), red);
                    var sw = Add<Sway>(cape.gameObject); sw.model = this; sw.amp = 2; sw.lift = 30;
                    foreach (var sx in new[] { -1f, 1f }) B(cape, new Vector3(sx * 0.4f, -0.4f, 0), new Vector3(0.03f, 0.82f, 0.036f), gold);
                    for (int k = 0; k < 10; k++)                                                    // ermine border with tails
                    {
                        float x = -0.36f + k * 0.08f;
                        BodyKit.Ball(cape, new Vector3(x, -0.81f, 0), 0.09f, erm);
                        if (k % 2 == 1) BodyKit.Ball(cape, new Vector3(x, -0.8f, -0.045f), new Vector3(0.025f, 0.05f, 0.02f), blk);
                    }
                    for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++)                       // gold stars on the back
                            Msh(Meshes.Star(), cape, new Vector3(-0.22f + c * 0.22f, -0.18f - r * 0.2f, -0.02f), new Vector3(0.07f, 0.07f, 0.02f), gold);
                    for (int k = 0; k < 16; k++)                                                    // ermine collar
                    {
                        float a = k * Mathf.PI * 2 / 16;
                        var p = new Vector3(Mathf.Cos(a) * 0.27f, 0.47f, Mathf.Sin(a) * 0.27f);
                        BodyKit.Mid(t, p, Vector3.one * 0.14f, erm);
                        if (k % 3 == 0) BodyKit.Ball(t, p + p.normalized * 0.06f + Vector3.up * 0.02f, new Vector3(0.03f, 0.045f, 0.03f), blk);
                    }
                    Pn(42, 108, 82, 98, 1, 6, 0.03f, erm);                                          // ermine front placket
                    for (int k = 0; k < 4; k++) On(BodyKit.Ball(t, P(55 + k * 15, 90, 1.09f), new Vector3(0.022f, 0.04f, 0.015f), blk), 55 + k * 15, 90);
                    Vector3 c0 = P(50, 62, 1.06f), c1 = P(50, 118, 1.06f);
                    foreach (var c in new[] { c0, c1 })                                             // gem clasps
                    {
                        var n = (c - C).normalized;
                        BodyKit.Pin(t, c, n, 0.05f, 0.012f, gold);
                        Dt.Gem(t, c + n * 0.008f, 0.03f, gem, gold, n);
                    }
                    BodyKit.Chain(t, tt => Vector3.Lerp(c0, c1, tt) + new Vector3(0, -0.05f * Mathf.Sin(tt * Mathf.PI), 0.02f * Mathf.Sin(tt * Mathf.PI)), 7, 0.014f, 0.004f, gold);
                    BodyKit.Tor(t, new Vector3(0, C.y + R.y * Mathf.Cos(106 * Mathf.Deg2Rad), 0), R.x * Mathf.Sin(106 * Mathf.Deg2Rad) * 1.02f, 0.012f, gold, default, new Vector3(1, 1, R.z / R.x), 24);
                    for (int k = 0; k < 16; k++) BodyKit.Dot(t, P(101, k * 22.5f, 1.03f), 0.016f, gold);   // gold embroidery
                    Sleeves(red, 0.26f, 1.1f, null, gold);
                    foreach (var arm in new[] { ArmL, ArmR })                                      // ermine cuffs
                    {
                        Dt.Path(BodyKit.Loop(new Vector3(0, -0.27f, 0), 0.105f, 0.105f, 9), 9, (p, d, k) => BodyKit.Ball(arm, p, 0.06f, erm));
                        foreach (var a in new[] { 60f, 120f }) BodyKit.Ball(arm, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.13f, -0.27f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.13f), new Vector3(0.02f, 0.035f, 0.02f), blk);
                        BodyKit.Tor(arm, new Vector3(0, -0.225f, 0), 0.106f, 0.008f, gold);
                    }
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
                float si = h == HandL ? 1 : -1;     // towards the body (thumb side)
                Vector3 Fs(float x, float y, float r, float cy = 0, float g = 1.02f)     // point on the front of a hand sphere
                {
                    var rel = new Vector3(x, y - cy, Mathf.Sqrt(Mathf.Max(0.0001f, r * r - x * x - (y - cy) * (y - cy))));
                    return new Vector3(0, cy, 0) + rel * g;
                }
                GameObject Fl(Vector3 pos, Vector3 size, Material m, float cy = 0, float roll = 0) => BodyKit.Face(B(h, pos, size, m), pos - new Vector3(0, cy, 0), roll);
                void Tufts(float r, int rows, int cols, float th0, float th1, float d, Material m, int seed) => BodyKit.Tufts(h, Vector3.zero, Vector3.one * r, rows, cols, th0, th1, d, m, seed);
                void Cuff(float y, float r, int n, float d, Material a, Material b) => Dt.Path(BodyKit.Loop(new Vector3(0, y, 0), r, r, n), n, (p, dd, k) => BodyKit.Ball(h, p, d, k % 2 == 0 ? a : b));
                switch (set >= 0 ? 100 + set : i)
                {
                    case 0:     // boxing gloves: knuckle pads, seams, laced cuff
                    {
                        var red = M("E53935", 0.6f); var wh = M("FFFFFF"); var sm = M("B71C1C", 0.3f); var lace = M("F5F5F5", 0.2f); var met = M("C9CED6", 0.85f, 0.8f);
                        S(h, new Vector3(0, -0.02f, 0), 0.25f, red);
                        BodyKit.Mid(h, new Vector3(si * 0.1f, 0, 0.05f), new Vector3(0.09f, 0.13f, 0.09f), red).transform.localRotation = Quaternion.Euler(0, 0, -si * 20);
                        for (int k = 0; k < 4; k++) BodyKit.Ball(h, Fs(-0.054f + k * 0.036f, -0.075f, 0.125f, -0.02f, 0.94f), new Vector3(0.05f, 0.045f, 0.05f), red);
                        Dt.Stitches(h, tt => { float a = Mathf.Lerp(45, 200, tt) * Mathf.Deg2Rad; return new Vector3(0, -0.02f + Mathf.Cos(a) * 0.128f, Mathf.Sin(a) * 0.128f); }, 12, 0.018f, 0.006f, sm);
                        for (int row = 0; row < 2; row++)
                            for (int k = 0; k < 6; k++)
                            {
                                float x = (k < 3 ? -1 : 1) * (0.022f + (k % 3) * 0.024f);
                                Dt.Orient(B(h, Fs(x, 0.025f - row * 0.04f, 0.125f, -0.02f, 1.02f), new Vector3(0.006f, 0.016f, 0.006f), sm), Vector3.right);
                            }
                        Dt.Stitches(h, BodyKit.Loop(new Vector3(0, 0.045f, 0), 0.109f, 0.109f, 10), 10, 0.018f, 0.006f, sm);
                        Cy(h, new Vector3(0, 0.08f, 0), new Vector3(0.17f, 0.04f, 0.17f), wh);
                        foreach (var y in new[] { 0.042f, 0.118f }) BodyKit.Tor(h, new Vector3(0, y, 0), 0.086f, 0.007f, wh);
                        for (int k = 0; k < 4; k++)                                                 // laces
                        {
                            float y = 0.055f + k * 0.019f;
                            foreach (var sx in new[] { -1f, 1f }) BodyKit.Dot(h, new Vector3(sx * 0.022f, y, 0.087f), 0.01f, met);
                            if (k < 3) foreach (var sx in new[] { -1f, 1f }) LRod(h, new Vector3(sx * 0.022f, y, 0.09f), new Vector3(-sx * 0.022f, y + 0.019f, 0.09f), 0.007f, lace);
                        }
                        foreach (var sx in new[] { -1f, 1f })
                        {
                            BodyKit.Tor(h, new Vector3(sx * 0.015f, 0.125f, 0.092f), 0.012f, 0.003f, lace, new Vector3(90, 0, sx * 30), null, 10);
                            LRod(h, new Vector3(0, 0.122f, 0.092f), new Vector3(sx * 0.02f, 0.085f, 0.096f), 0.006f, lace);
                        }
                        BodyKit.Pin(h, new Vector3(-si * 0.124f, -0.02f, 0.02f), new Vector3(-si, 0, 0), 0.05f, 0.006f, wh);   // logo
                        Msh(Meshes.Star(), h, new Vector3(-si * 0.128f, -0.02f, 0.02f), new Vector3(0.035f, 0.035f, 0.01f), red).transform.localRotation = Quaternion.LookRotation(new Vector3(-si, 0, 0));
                        break;
                    }
                    case 1:     // cat paws: toe beans, fluffy fur, bow with a bell
                    {
                        var fur = M("FFF0F4", 0.1f); var fur2 = M("FFE2EA", 0.1f); var pink = M("FF8FB0", 0.3f); var claw = M("FFFFFF", 0.6f);
                        var groove = M("F3C6D3", 0.1f); var rib = M("FF4F8B", 0.3f); var gold = M("FFC83A", 0.9f, 0.9f);
                        S(h, Vector3.zero, 0.19f, fur);
                        BodyKit.Ball(h, new Vector3(0, -0.07f, 0.055f), new Vector3(0.07f, 0.05f, 0.03f), pink).transform.localRotation = Quaternion.Euler(40, 0, 0);
                        for (int k = 0; k < 4; k++)
                        {
                            float x = -0.045f + k * 0.03f;
                            BodyKit.Ball(h, Fs(x, -0.035f - Mathf.Abs(x) * 0.3f, 0.095f, 0, 1.0f), 0.032f, pink);
                            BodyKit.Cn(h, Fs(x, -0.05f - Mathf.Abs(x) * 0.3f, 0.095f, 0, 0.98f), Fs(x, -0.06f - Mathf.Abs(x) * 0.3f, 0.095f, 0, 1.22f), 0.012f, claw);
                        }
                        for (int k = -1; k <= 1; k++) LRod(h, Fs(k * 0.03f, 0.01f, 0.095f, 0, 1.01f), Fs(k * 0.03f, -0.025f, 0.095f, 0, 1.01f), 0.006f, groove);
                        Tufts(0.095f, 4, 7, 15, 110, 0.035f, fur2, 11);
                        Cuff(0.07f, 0.066f, 12, 0.045f, fur, fur2);
                        foreach (var sx in new[] { -1f, 1f }) BodyKit.Ball(h, new Vector3(sx * 0.022f, 0.078f, 0.07f), new Vector3(0.04f, 0.028f, 0.014f), rib).transform.localRotation = Quaternion.Euler(0, 0, sx * 15);
                        BodyKit.Dot(h, new Vector3(0, 0.078f, 0.074f), 0.016f, rib);
                        BodyKit.Ball(h, new Vector3(0, 0.055f, 0.08f), 0.03f, gold);
                        B(h, new Vector3(0, 0.05f, 0.094f), new Vector3(0.016f, 0.003f, 0.004f), M("8A6A10", 0.5f));
                        break;
                    }
                    case 2:     // fingerless gloves: finger holes, perforation, knuckle pad, velcro strap
                    {
                        var blk = M("26262B", 0.4f); var grey = M("4A4A52", 0.3f); var st = M("8A8A94", 0.2f); var met = M("C9CED6", 0.85f, 0.8f);
                        S(h, Vector3.zero, 0.18f, blk);
                        for (int k = 0; k < 4; k++)
                        {
                            float x = -0.045f + k * 0.03f;
                            BodyKit.Ball(h, new Vector3(x, -0.095f, 0.03f), new Vector3(0.034f, 0.05f, 0.034f), Skin);
                            Lc(h, new Vector3(x, -0.072f, 0.028f), new Vector3(0.04f, 0.006f, 0.04f), blk);
                        }
                        BodyKit.Ball(h, new Vector3(si * 0.085f, -0.04f, 0.05f), new Vector3(0.035f, 0.05f, 0.035f), Skin).transform.localRotation = Quaternion.Euler(0, 0, si * 30);
                        Lc(h, new Vector3(si * 0.07f, -0.02f, 0.045f), new Vector3(0.042f, 0.006f, 0.042f), blk, new Vector3(0, 0, si * 30));
                        for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) BodyKit.Dot(h, Fs(-0.036f + c * 0.024f, 0.045f - r * 0.022f, 0.09f), 0.012f, grey);
                        for (int k = 0; k < 4; k++) Fl(Fs(-0.039f + k * 0.026f, -0.04f, 0.09f, 0, 1.04f), new Vector3(0.024f, 0.022f, 0.014f), grey);
                        var sp = Fs(-si * 0.01f, 0.055f, 0.09f, 0, 1.06f);                          // velcro strap
                        Fl(sp, new Vector3(0.075f, 0.03f, 0.012f), grey);
                        foreach (var sy in new[] { -1f, 1f }) Dt.Stitches(h, Dt.Line(sp + new Vector3(-0.03f, sy * 0.011f, 0.007f), sp + new Vector3(0.03f, sy * 0.011f, 0.007f)), 3, 0.014f, 0.004f, st);
                        BodyKit.Tor(h, new Vector3(-si * 0.07f, 0.055f, 0.05f), 0.012f, 0.003f, met, new Vector3(0, 0, 90), null, 10);
                        Dt.Stitches(h, tt => Fs(Mathf.Lerp(-0.06f, 0.06f, tt), -0.058f, 0.09f, 0, 1.03f), 8, 0.014f, 0.005f, st);
                        Dt.Stitches(h, tt => { float y = Mathf.Lerp(0.06f, -0.06f, tt); return new Vector3(-si * Mathf.Sqrt(0.0081f - y * y) * 1.03f, y, 0); }, 6, 0.016f, 0.005f, st);
                        for (int r = 0; r < 2; r++) for (int c = 0; c < 4; c++)                   // palm grip dots
                            {
                                float y = 0.02f - c * 0.025f, z = -0.015f + r * 0.03f;
                                BodyKit.Dot(h, new Vector3(si * Mathf.Sqrt(Mathf.Max(0.0001f, 0.0081f - y * y - z * z)) * 1.02f, y, z), 0.014f, grey);
                            }
                        BodyKit.Tor(h, new Vector3(0, 0.07f, 0), 0.058f, 0.01f, blk);
                        break;
                    }
                    case 3:     // robo manipulators: armoured palm, jointed claws, glowing core, pistons
                    {
                        var metal = M("9AA5B4", 0.8f, 0.7f); var dark = M("3A4250", 0.6f, 0.5f); var glow = G("35E0FF", 2f); var jn = M("D9DEE6", 0.9f, 0.9f); var rub = M("1E1E1E", 0.2f);
                        B(h, Vector3.zero, new Vector3(0.15f, 0.13f, 0.15f), metal);
                        foreach (var sz in new[] { 1f, -1f })                                        // front / back plates with bolts
                        {
                            B(h, new Vector3(0, 0.01f, sz * 0.08f), new Vector3(0.11f, 0.08f, 0.012f), dark);
                            for (int k = 0; k < 4; k++) BodyKit.Dot(h, new Vector3((k % 2 == 0 ? -1 : 1) * 0.045f, 0.01f + (k < 2 ? -1 : 1) * 0.03f, sz * 0.087f), 0.01f, jn);
                        }
                        for (int k = 0; k < 5; k++) B(h, new Vector3(-si * 0.076f, -0.04f + k * 0.02f, 0), new Vector3(0.006f, 0.008f, 0.08f), dark);   // vents
                        BodyKit.Ball(h, new Vector3(0, 0.01f, 0.09f), 0.03f, glow);
                        BodyKit.Tor(h, new Vector3(0, 0.01f, 0.088f), 0.022f, 0.004f, jn, new Vector3(90, 0, 0), null, 10);
                        for (int k = 0; k < 3; k++) BodyKit.Dot(h, new Vector3(-0.03f + k * 0.03f, 0.058f, 0.077f), 0.01f, glow);
                        Cy(h, new Vector3(0, 0.075f, 0), new Vector3(0.12f, 0.02f, 0.12f), jn);
                        for (int k = 0; k < 4; k++) { float a = (45 + k * 90) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); LRod(h, d * 0.05f + Vector3.up * 0.065f, d * 0.045f + Vector3.up * 0.13f, 0.012f, jn); }
                        LRod(h, new Vector3(-si * 0.06f, 0.06f, -0.04f), new Vector3(-si * 0.075f, -0.02f, -0.06f), 0.01f, glow);
                        LRod(h, new Vector3(-si * 0.075f, -0.02f, -0.06f), new Vector3(-si * 0.06f, -0.06f, -0.07f), 0.01f, glow);
                        void Finger(Vector3 s, Vector3 d)                                           // knuckle pin, 2 segments, joint, claw tip, rubber pad
                        {
                            d = d.normalized; var bend = (d + Vector3.forward * 0.4f).normalized;
                            BodyKit.Pin(h, s, Vector3.right, 0.024f, 0.036f, jn);
                            Dt.Orient(B(h, s + d * 0.028f, new Vector3(0.03f, 0.05f, 0.03f), metal), d);
                            var j = s + d * 0.056f;
                            BodyKit.Pin(h, j, Vector3.right, 0.02f, 0.034f, jn);
                            Dt.Orient(B(h, j + bend * 0.024f, new Vector3(0.026f, 0.045f, 0.026f), metal), bend);
                            BodyKit.Cn(h, j + bend * 0.046f, j + bend * 0.075f, 0.026f, dark);
                            BodyKit.Dot(h, j + bend * 0.03f + Vector3.forward * 0.013f, 0.014f, rub);
                        }
                        Finger(new Vector3(-0.045f, -0.065f, 0.03f), new Vector3(0, -1, 0.15f));
                        Finger(new Vector3(0.045f, -0.065f, 0.03f), new Vector3(0, -1, 0.15f));
                        Finger(new Vector3(si * 0.075f, -0.03f, 0.04f), new Vector3(si * 0.4f, -1, 0.2f));
                        for (int k = 0; k < 8; k++) BodyKit.Dot(h, new Vector3((k % 2 == 0 ? -1 : 1) * 0.076f, 0.055f - (k / 2) * 0.035f, (k % 4 < 2 ? 1 : -1) * 0.06f), 0.01f, jn);   // edge rivets
                        BodyKit.Pin(h, new Vector3(si * 0.077f, 0.01f, 0), Vector3.right * si, 0.05f, 0.006f, rub);   // palm suction pad
                        BodyKit.Tor(h, new Vector3(si * 0.08f, 0.01f, 0), 0.02f, 0.003f, dark, new Vector3(0, 0, 90), null, 10);
                        break;
                    }
                    case 4:     // leather gauntlets: flared cuff with fringe and concho, stitched points, rivets
                    {
                        var lth = M("6D4425", 0.4f); var cuf = M("5A3418", 0.4f); var dark = M("3E2412", 0.3f); var brass = M("C9A040", 0.8f, 0.8f); var st = M("E8D2A8", 0.2f);
                        S(h, Vector3.zero, 0.18f, lth);
                        Cy(h, new Vector3(0, 0.06f, 0), new Vector3(0.2f, 0.03f, 0.2f), cuf);
                        Cy(h, new Vector3(0, 0.12f, 0), new Vector3(0.24f, 0.03f, 0.24f), cuf);
                        BodyKit.Tor(h, new Vector3(0, 0.15f, 0), 0.12f, 0.01f, dark);
                        Dt.Stitches(h, BodyKit.Loop(new Vector3(0, 0.045f, 0), 0.102f, 0.102f, 12), 12, 0.016f, 0.005f, st);
                        Dt.Stitches(h, BodyKit.Loop(new Vector3(0, 0.14f, 0), 0.122f, 0.122f, 12), 12, 0.016f, 0.005f, st);
                        for (int k = 0; k < 8; k++)                                                 // fringe on the outer side
                        {
                            float a = (si > 0 ? 180 : 0) + (k - 3.5f) * 11;
                            var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                            B(h, d * 0.124f + Vector3.up * 0.095f, new Vector3(0.012f, 0.07f, 0.004f), cuf).transform.localRotation = Quaternion.LookRotation(d, Vector3.up);
                        }
                        BodyKit.Pin(h, new Vector3(0, 0.11f, 0.118f), Vector3.forward, 0.045f, 0.006f, brass);   // concho
                        Msh(Meshes.Star(), h, new Vector3(0, 0.11f, 0.124f), new Vector3(0.035f, 0.035f, 0.01f), brass);
                        for (int k = 0; k < 4; k++) BodyKit.Ball(h, new Vector3(-0.045f + k * 0.03f, -0.085f, 0.035f), new Vector3(0.035f, 0.06f, 0.035f), lth);
                        for (int k = -1; k <= 1; k++) LRod(h, Fs(k * 0.03f, -0.03f, 0.09f, 0, 1.01f), Fs(k * 0.03f, -0.075f, 0.09f, 0, 1.0f), 0.005f, dark);
                        BodyKit.Ball(h, new Vector3(si * 0.08f, -0.03f, 0.04f), new Vector3(0.04f, 0.065f, 0.04f), lth).transform.localRotation = Quaternion.Euler(0, 0, si * 25);
                        for (int k = 0; k < 4; k++) BodyKit.Dot(h, Fs(-0.039f + k * 0.026f, -0.035f, 0.09f, 0, 1.02f), 0.011f, brass);
                        for (int k = -1; k <= 1; k++) Dt.Stitches(h, tt => Fs(k * 0.028f, Mathf.Lerp(0.03f, -0.015f, tt), 0.09f, 0, 1.02f), 4, 0.01f, 0.004f, st);
                        BodyKit.Tor(h, new Vector3(0, 0.032f, 0), 0.087f, 0.009f, dark);
                        B(h, new Vector3(0, 0.032f, 0.095f), new Vector3(0.03f, 0.024f, 0.006f), brass);
                        B(h, new Vector3(-0.004f, 0.032f, 0.099f), new Vector3(0.018f, 0.004f, 0.004f), brass);
                        break;
                    }
                    case 5:     // winter mittens: knit cuff, snowflake, zigzag knit, pompom tie
                    {
                        var blue = M("1E88E5", 0.05f); var wh = M("FFFFFF", 0.05f); var shade = M("1565C0", 0.05f); var rib = M("E3EEF7", 0.05f);
                        var mc = new Vector3(0, -0.01f, 0); var mr = new Vector3(0.105f, 0.115f, 0.095f);
                        S(h, mc, mr * 2, blue);
                        BodyKit.Mid(h, new Vector3(si * 0.08f, 0, 0.04f), Vector3.one * 0.08f, blue);
                        BodyKit.Knit(h, new Vector3(0, 0.085f, 0), 0.083f, 0.083f, 0.05f, 14, wh, rib, default, 0.01f);
                        var fc = BodyKit.Front(mc, mr, 0, -0.025f, 1.02f); var fn = BodyKit.EN(mc, mr)(fc);
                        for (int k = 0; k < 3; k++) BodyKit.Face(B(h, fc, new Vector3(0.006f, 0.06f, 0.004f), wh), fn, k * 60);   // snowflake
                        for (int k = 0; k < 6; k++)
                        {
                            float a = (90 + k * 60) * Mathf.Deg2Rad;
                            BodyKit.Dot(h, BodyKit.Front(mc, mr, Mathf.Cos(a) * 0.03f, -0.025f + Mathf.Sin(a) * 0.03f, 1.03f), 0.01f, wh);
                        }
                        BodyKit.Dot(h, fc + fn * 0.003f, 0.012f, wh);
                        for (int k = 0; k < 12; k++)                                                // zigzag knit band
                        {
                            float ph = (20 + k * 140f / 11) * Mathf.Deg2Rad;
                            BodyKit.Dot(h, mc + new Vector3(Mathf.Cos(ph) * 0.095f, 0.04f + (k % 2 == 0 ? 0.012f : -0.004f), Mathf.Sin(ph) * 0.087f), 0.012f, wh);
                        }
                        var rg = new Rng(55);
                        for (int k = 0; k < 6; k++) BodyKit.Dot(h, BodyKit.Front(mc, mr, rg.F(-0.06f, 0.06f), rg.F(-0.1f, -0.06f), 1.02f), 0.01f, wh);   // snow dots
                        var pp = new Vector3(-si * 0.1f, -0.005f, 0.03f);                           // pompom on a string
                        LRod(h, new Vector3(-si * 0.07f, 0.08f, 0.02f), pp, 0.006f, wh);
                        BodyKit.Ball(h, pp, 0.045f, wh);
                        for (int k = 0; k < 5; k++) { float a = k * 72 * Mathf.Deg2Rad; BodyKit.Dot(h, pp + new Vector3(0, Mathf.Cos(a), Mathf.Sin(a)) * 0.02f + Vector3.right * -si * 0.008f, 0.022f, wh); }
                        Dt.Stitches(h, tt => { float a = Mathf.Lerp(-60, 120, tt) * Mathf.Deg2Rad; return new Vector3(si * 0.06f, 0.035f * Mathf.Cos(a), 0.04f + 0.04f * Mathf.Sin(a)); }, 6, 0.012f, 0.004f, shade);
                        break;
                    }
                    case 6:     // claw paws: shaggy fur, toes with sheathed claws, paw pads
                    {
                        var fur = M("3A2A20", 0.3f); var fur2 = M("4E3A2C", 0.3f); var fur3 = M("2A1E16", 0.3f); var claw = M("F5F5F0", 0.7f); var pad = M("5A3A3A", 0.4f);
                        S(h, Vector3.zero, 0.19f, fur);
                        Tufts(0.095f, 3, 8, 15, 120, 0.04f, fur2, 66);
                        var rg = new Rng(67);
                        for (int k = 0; k < 10; k++)
                        {
                            float th = rg.F(20, 110), ph = rg.F(0, 360);
                            var p = Dt.Ell(Vector3.zero, Vector3.one * 0.095f, th, ph);
                            BodyKit.Cn(h, p, p * 1.4f + Vector3.up * 0.01f, 0.025f, fur3);
                        }
                        for (int k = -1; k <= 1; k++)
                        {
                            var toe = new Vector3(k * 0.04f, -0.06f, 0.06f);
                            BodyKit.Ball(h, toe, new Vector3(0.045f, 0.05f, 0.045f), fur2);
                            BodyKit.Dot(h, new Vector3(k * 0.045f, -0.08f, 0.082f), 0.026f, fur3);
                            BodyKit.Cn(h, new Vector3(k * 0.045f, -0.08f, 0.085f), new Vector3(k * 0.05f, -0.16f, 0.12f), 0.03f, claw);
                        }
                        BodyKit.Ball(h, new Vector3(si * 0.075f, -0.03f, 0.045f), 0.04f, fur2);         // dewclaw
                        BodyKit.Cn(h, new Vector3(si * 0.085f, -0.045f, 0.06f), new Vector3(si * 0.1f, -0.1f, 0.085f), 0.022f, claw);
                        BodyKit.Ball(h, new Vector3(0, -0.085f, 0.0f), new Vector3(0.06f, 0.02f, 0.05f), pad);   // pads underneath
                        for (int k = -1; k <= 1; k++) BodyKit.Ball(h, new Vector3(k * 0.035f, -0.075f, 0.045f), new Vector3(0.025f, 0.015f, 0.025f), pad);
                        Cuff(0.07f, 0.066f, 12, 0.048f, fur, fur2);
                        break;
                    }
                    case 7:     // light-up gloves: rainbow glow, LED fingertips, circuit traces, battery pack
                    {
                        var g = S(h, Vector3.zero, 0.19f, G("35E0FF", 2f));
                        var hu = Add<Hue>(g); hu.glow = 2f; hu.sat = 0.6f;
                        var dark = M("1A1C24", 0.5f); var led = G("FFFFFF", 2.5f);
                        for (int k = 0; k < 4; k++)
                        {
                            float x = -0.045f + k * 0.03f;
                            BodyKit.Ball(h, new Vector3(x, -0.09f, 0.035f), new Vector3(0.035f, 0.055f, 0.035f), dark);
                            BodyKit.Dot(h, new Vector3(x, -0.115f, 0.045f), 0.016f, led);
                        }
                        BodyKit.Ball(h, new Vector3(si * 0.085f, -0.04f, 0.05f), new Vector3(0.035f, 0.05f, 0.035f), dark).transform.localRotation = Quaternion.Euler(0, 0, si * 30);
                        BodyKit.Dot(h, new Vector3(si * 0.1f, -0.065f, 0.06f), 0.015f, led);
                        for (int k = 0; k < 4; k++) Fl(Fs(-0.039f + k * 0.026f, -0.04f, 0.095f, 0, 1.03f), new Vector3(0.024f, 0.02f, 0.012f), dark);
                        for (int k = 0; k < 7; k++)                                                 // circuit traces with LED nodes
                        {
                            float x = -0.045f + k * 0.015f;
                            Fl(Fs(x, 0.015f, 0.095f, 0, 1.01f), new Vector3(0.003f, 0.04f, 0.004f), dark);
                            if (k % 2 == 0) Fl(Fs(x + 0.007f, 0.035f - k * 0.004f, 0.095f, 0, 1.01f), new Vector3(0.016f, 0.003f, 0.004f), dark);
                            BodyKit.Dot(h, Fs(x, 0.036f, 0.095f, 0, 1.02f), 0.008f, led);
                        }
                        for (int k = 0; k < 3; k++) Fl(Fs(-0.02f + k * 0.02f, -0.012f, 0.095f, 0, 1.01f), new Vector3(0.012f, 0.003f, 0.004f), dark);
                        Cy(h, new Vector3(0, 0.08f, 0), new Vector3(0.17f, 0.03f, 0.17f), dark);
                        foreach (var y in new[] { 0.065f, 0.095f }) BodyKit.Tor(h, new Vector3(0, y, 0), 0.087f, 0.005f, led);
                        var bp = new Vector3(-si * 0.095f, 0.02f, 0);                               // battery pack
                        B(h, bp, new Vector3(0.016f, 0.04f, 0.03f), dark);
                        BodyKit.Dot(h, bp + new Vector3(-si * 0.009f, 0.01f, 0.006f), 0.008f, led);
                        BodyKit.Dot(h, bp + new Vector3(-si * 0.009f, 0.01f, -0.006f), 0.008f, G("FF3355", 2.5f));
                        B(h, bp + new Vector3(-si * 0.009f, -0.01f, 0), new Vector3(0.004f, 0.012f, 0.008f), M("D9DEE6", 0.9f, 0.9f));
                        for (int r = 0; r < 2; r++) for (int c = 0; c < 4; c++)                   // palm grip dots
                            {
                                float y = 0.02f - c * 0.025f, z = -0.015f + r * 0.03f;
                                BodyKit.Dot(h, new Vector3(si * Mathf.Sqrt(Mathf.Max(0.0001f, 0.009f - y * y - z * z)) * 1.02f, y, z), 0.014f, dark);
                            }
                        Dt.Stitches(h, tt => { float y = Mathf.Lerp(0.06f, -0.06f, tt); return new Vector3(-si * Mathf.Sqrt(0.009f - y * y) * 1.03f, y, -0.02f); }, 6, 0.016f, 0.005f, dark);
                        break;
                    }
                    case 104:   // cyber gloves: armoured finger plates with glowing joints, knuckle guard, wrist module
                    {
                        var bs = M("15171F", 0.7f, 0.4f); var plate = M("2A2E3A", 0.75f, 0.6f); var glow = G("26F0FF", 2.5f); var bolt = M("AEB6C4", 0.9f, 0.9f);
                        S(h, Vector3.zero, 0.19f, bs);
                        Ring(h, new Vector3(0, 0.02f, 0), 0.095f, 0.012f, glow);
                        Ring(h, new Vector3(0, 0.07f, 0), 0.085f, 0.012f, glow);
                        void Curl(float x, float z0, int n, float a0, float a1)                    // plates following the curl of a finger
                        {
                            float r1 = Mathf.Sqrt(0.0095f - x * x) * 1.06f;
                            for (int k = 0; k < n * 2 - 1; k++)
                            {
                                float a = Mathf.Lerp(a0, a1, k / (2f * n - 2)) * Mathf.Deg2Rad;
                                var nrm = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a));
                                var p = new Vector3(x, r1 * Mathf.Sin(a), z0 + r1 * Mathf.Cos(a));
                                var go = k % 2 == 0 ? B(h, p, new Vector3(0.026f, 0.022f, 0.012f), plate) : B(h, p + nrm * 0.004f, new Vector3(0.022f, 0.004f, 0.012f), glow);
                                go.transform.localRotation = Quaternion.LookRotation(nrm, new Vector3(0, Mathf.Cos(a), -Mathf.Sin(a)));
                            }
                        }
                        for (int k = 0; k < 4; k++) Curl(-0.045f + k * 0.03f, 0, 3, -25, -80);
                        foreach (var sz in new[] { 0.03f, 0.055f }) Dt.Orient(B(h, new Vector3(si * 0.09f, -0.02f - sz * 0.6f, sz), new Vector3(0.022f, 0.026f, 0.014f), plate), new Vector3(0, 1, 0));
                        B(h, new Vector3(si * 0.094f, -0.032f, 0.042f), new Vector3(0.012f, 0.004f, 0.02f), glow);
                        for (int k = 0; k < 4; k++)                                                 // knuckle guard + bolts
                        {
                            var p = Fs(-0.039f + k * 0.026f, -0.01f, 0.095f, 0, 1.06f);
                            Fl(p, new Vector3(0.024f, 0.026f, 0.012f), plate);
                            BodyKit.Dot(h, p + p.normalized * 0.007f, 0.008f, bolt);
                        }
                        for (int k = 0; k < 6; k++) Fl(Fs(-0.04f + k * 0.016f, 0.035f, 0.095f, 0, 1.02f), new Vector3(0.003f, 0.02f + (k % 3) * 0.006f, 0.004f), glow);   // circuit lines
                        var wm = new Vector3(-si * 0.095f, 0.03f, 0);                               // wrist module
                        B(h, wm, new Vector3(0.016f, 0.045f, 0.04f), plate);
                        B(h, wm + new Vector3(-si * 0.009f, 0.006f, 0), new Vector3(0.003f, 0.022f, 0.03f), glow);
                        for (int k = 0; k < 3; k++) BodyKit.Dot(h, wm + new Vector3(-si * 0.009f, -0.014f, -0.01f + k * 0.01f), 0.007f, bolt);
                        for (int k = 0; k < 6; k++) BodyKit.Dot(h, new Vector3(si * 0.092f, 0.03f - (k / 2) * 0.025f, (k % 2 == 0 ? -0.012f : 0.012f)), 0.012f, plate);   // hex vents
                        BodyKit.Ball(h, new Vector3(0, -0.09f, -0.01f), new Vector3(0.07f, 0.02f, 0.06f), plate);   // palm plate
                        BodyKit.Tor(h, new Vector3(0, 0.095f, 0), 0.08f, 0.01f, plate);
                        for (int k = 0; k < 4; k++) { float a = (45 + k * 90) * Mathf.Deg2Rad; BodyKit.Dot(h, new Vector3(Mathf.Cos(a) * 0.088f, 0.095f, Mathf.Sin(a) * 0.088f), 0.01f, bolt); }
                        break;
                    }
                    default: S(h, Vector3.zero, 0.17f, Skin); break;
                }
            }
        }

        void Wrist(int i, int set)
        {
            var w = Node("Wrist", ArmL, new Vector3(0, -0.22f, 0));
            Vector3 Rd(float deg, float r, float y = 0) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
            Vector3 Dir(float deg) => Rd(deg, 1);
            GameObject Bx(float deg, float r, float y, Vector3 size, Material m, float roll = 0) => BodyKit.Face(B(w, Rd(deg, r, y), size, m), Dir(deg), roll);
            void Stitch(float y, float r, int n, Material m, float a0 = 0) => Dt.Stitches(w, BodyKit.Loop(new Vector3(0, y, 0), r, r, n, a0), n, 0.012f, 0.004f, m);
            switch (i)
            {
                case 0:     // smart watch: ribbed strap, buckle, case with crown and a tiny watch face UI
                {
                    var strap = M("1A1A1A", 0.3f); var cs = M("2B2F36", 0.8f, 0.6f); var rim = M("8A93A0", 0.85f, 0.85f); var scr = G("35A8FF", 1.6f);
                    var ui = M("FFFFFF", 0.6f); var dark = M("333333", 0.3f); var grn = G("4DFF88", 1.6f);
                    Ring(w, Vector3.zero, 0.085f, 0.02f, strap);
                    for (int k = 0; k < 18; k++) Bx(125 + k * 7, 0.104f, 0, new Vector3(0.004f, 0.03f, 0.01f), dark);
                    for (int k = 0; k < 10; k++) Bx(362 + k * 5.5f, 0.104f, 0, new Vector3(0.004f, 0.03f, 0.01f), dark);
                    for (int k = 0; k < 6; k++) BodyKit.Dot(w, Rd(298 + k * 10, 0.104f), 0.008f, dark);
                    foreach (var a in new[] { 261f, 279f }) Bx(a, 0.108f, 0, new Vector3(0.006f, 0.05f, 0.006f), rim);   // buckle
                    foreach (var y in new[] { -0.024f, 0.024f }) Bx(270, 0.108f, y, new Vector3(0.034f, 0.006f, 0.006f), rim);
                    Bx(270, 0.111f, 0, new Vector3(0.026f, 0.004f, 0.004f), rim, 90);
                    Bx(289, 0.106f, 0, new Vector3(0.008f, 0.046f, 0.012f), strap);
                    Bx(250, 0.106f, 0, new Vector3(0.008f, 0.046f, 0.012f), strap);
                    B(w, new Vector3(0, 0, 0.09f), new Vector3(0.1f, 0.09f, 0.026f), cs);         // case
                    foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) BodyKit.Pin(w, new Vector3(sx * 0.045f, sy * 0.04f, 0.09f), Vector3.forward, 0.02f, 0.026f, cs);
                    B(w, new Vector3(0, 0, 0.104f), new Vector3(0.084f, 0.074f, 0.004f), scr);
                    foreach (var sy in new[] { -1f, 1f }) B(w, new Vector3(0, sy * 0.04f, 0.104f), new Vector3(0.092f, 0.006f, 0.006f), rim);
                    foreach (var sx in new[] { -1f, 1f }) B(w, new Vector3(sx * 0.045f, 0, 0.104f), new Vector3(0.006f, 0.08f, 0.006f), rim);
                    foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f })    // lugs + pins
                        {
                            B(w, new Vector3(sx * 0.054f, sy * 0.025f, 0.084f), new Vector3(0.015f, 0.012f, 0.02f), cs);
                            BodyKit.Dot(w, new Vector3(sx * 0.054f, sy * 0.032f, 0.084f), 0.006f, rim);
                        }
                    var cr = new Vector3(0, 0.052f, 0.09f);                                         // crown with ridges + button
                    BodyKit.Pin(w, cr, Vector3.up, 0.02f, 0.012f, rim);
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; B(w, cr + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.01f, new Vector3(0.003f, 0.011f, 0.003f), rim); }
                    B(w, new Vector3(0.02f, -0.047f, 0.09f), new Vector3(0.02f, 0.006f, 0.01f), rim);
                    for (int k = 0; k < 4; k++) BodyKit.Dot(w, new Vector3(-0.03f + k * 0.008f, -0.045f, 0.09f), 0.004f, dark);   // speaker holes
                    var fc = new Vector3(0, 0, 0.107f);
                    for (int k = 0; k < 12; k++)                                                    // ticks
                    {
                        float a = k * 30 * Mathf.Deg2Rad; bool big = k % 3 == 0;
                        B(w, fc + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.028f, big ? new Vector3(0.003f, 0.01f, 0.002f) : new Vector3(0.002f, 0.006f, 0.002f), ui, new Vector3(0, 0, k * 30 - 90));
                    }
                    B(w, fc + new Vector3(0.004f, 0.007f, 0), new Vector3(0.003f, 0.016f, 0.002f), ui, new Vector3(0, 0, -30));   // hands
                    B(w, fc + new Vector3(-0.01f, 0.006f, 0.001f), new Vector3(0.002f, 0.024f, 0.002f), ui, new Vector3(0, 0, 60));
                    BodyKit.Dot(w, fc + new Vector3(0, 0, 0.002f), 0.005f, ui);
                    Msh(Meshes.Heart(), w, fc + new Vector3(-0.03f, -0.028f, 0), new Vector3(0.012f, 0.012f, 0.003f), G("FF3355", 1.8f));
                    for (int k = 0; k < 3; k++) B(w, fc + new Vector3(0.018f + k * 0.006f, -0.03f + k * 0.002f, 0), new Vector3(0.004f, 0.006f + k * 0.004f, 0.002f), grn);
                    B(w, fc + new Vector3(0.028f, 0.03f, 0), new Vector3(0.014f, 0.007f, 0.001f), ui);
                    B(w, fc + new Vector3(0.026f, 0.03f, 0.001f), new Vector3(0.008f, 0.004f, 0.002f), grn);
                    for (int k = 0; k < 3; k++) B(w, fc + new Vector3(-0.024f, 0.012f - k * 0.006f, 0), new Vector3(0.014f - k * 0.003f, 0.003f, 0.002f), G("BFE6FF", 1.4f));
                    var sun = fc + new Vector3(-0.028f, 0.028f, 0);
                    BodyKit.Dot(w, sun, 0.007f, G("FFD24A", 1.8f));
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; B(w, sun + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.007f, new Vector3(0.001f, 0.003f, 0.001f), G("FFD24A", 1.8f), new Vector3(0, 0, k * 60 - 90)); }
                    for (int k = 0; k < 4; k++) BodyKit.Dot(w, fc + new Vector3(-0.008f + k * 0.006f, -0.018f, 0), 0.004f, k % 2 == 0 ? grn : G("FF9A3C", 1.6f));   // app dots
                    break;
                }
                case 1:     // neon sweatband: terry loops, piping, logo
                {
                    var pink = G("FF3FA4", 2f); var pink2 = G("FF7AC4", 1.6f); var wh = G("FFFFFF", 1.6f); var st = M("FFFFFF", 0.3f); var yel = G("FFE14D", 2f);
                    Cy(w, Vector3.zero, new Vector3(0.18f, 0.035f, 0.18f), pink);
                    foreach (var y in new[] { -0.03f, 0.03f }) BodyKit.Tor(w, new Vector3(0, y, 0), 0.092f, 0.006f, wh);
                    for (int r = 0; r < 3; r++)
                        for (int k = 0; k < 22; k++) BodyKit.Dot(w, Rd(110 + (k + (r % 2) * 0.5f) * 320f / 22, 0.092f, -0.016f + r * 0.016f), 0.014f, pink2);
                    foreach (var y in new[] { -0.024f, 0.024f }) Stitch(y, 0.093f, 16, st);
                    Bx(90, 0.094f, 0, new Vector3(0.042f, 0.032f, 0.004f), wh);
                    BodyKit.Face(Msh(Meshes.Bolt(), w, Rd(90, 0.098f), new Vector3(0.03f, 0.03f, 0.01f), yel), Vector3.forward);
                    foreach (var sx in new[] { -1f, 1f }) Msh(Meshes.Star(), w, new Vector3(sx * 0.014f, 0.008f, 0.098f), new Vector3(0.008f, 0.008f, 0.003f), pink);
                    break;
                }
                case 2:     // gold bracelet: double chain, spacer beads, ID plate with gem, charms, clasp
                {
                    var gold = M("FFC83A", 0.9f, 0.9f); var dia = M("E8F6FF", 0.95f, 0.3f); var ruby = M("E0115F", 0.9f, 0.2f);
                    BodyKit.Chain(w, BodyKit.Loop(new Vector3(0, 0.013f, 0), 0.088f, 0.088f, 26), 26, 0.011f, 0.003f, gold);
                    BodyKit.Chain(w, BodyKit.Loop(new Vector3(0, -0.015f, 0), 0.088f, 0.088f, 26, 360f / 52), 26, 0.011f, 0.003f, gold);
                    Dt.Path(BodyKit.Loop(new Vector3(0, -0.001f, 0), 0.09f, 0.09f, 26, 360f / 104), 26, (p, d, k) => BodyKit.Dot(w, p, 0.009f, gold));
                    B(w, new Vector3(0, 0, 0.103f), new Vector3(0.06f, 0.035f, 0.006f), gold);    // ID plate
                    for (int k = 0; k < 8; k++)
                    {
                        float x = k < 4 ? -0.022f + k * 0.0147f : -0.022f + (k - 4) * 0.0147f, y = k < 4 ? 0.013f : -0.013f;
                        BodyKit.Dot(w, new Vector3(x, y, 0.107f), 0.007f, dia);
                    }
                    Dt.Gem(w, new Vector3(0, 0, 0.107f), 0.016f, ruby, gold, Vector3.forward);
                    foreach (var sx in new[] { -1f, 1f }) BodyKit.Dot(w, new Vector3(sx * 0.026f, 0, 0.107f), 0.006f, gold);
                    BodyKit.Tor(w, new Vector3(0, 0, -0.1f), 0.01f, 0.003f, gold, new Vector3(90, 0, 0), null, 10);   // clasp
                    B(w, new Vector3(0.016f, 0, -0.1f), new Vector3(0.02f, 0.012f, 0.008f), gold);
                    BodyKit.Dot(w, new Vector3(0.028f, 0, -0.1f), 0.007f, gold);
                    int c = 0;
                    foreach (var a in new[] { 40f, 140f, 200f })                                    // charms
                    {
                        var p = Rd(a, 0.095f, -0.03f);
                        BodyKit.Tor(w, p, 0.006f, 0.0015f, gold, new Vector3(90, -a, 0), null, 10);
                        var cp = p + Vector3.down * 0.018f;
                        if (c == 0) BodyKit.Face(Msh(Meshes.Heart(), w, cp, new Vector3(0.022f, 0.022f, 0.006f), ruby), Dir(a));
                        else if (c == 1) BodyKit.Face(Msh(Meshes.Star(), w, cp, new Vector3(0.024f, 0.024f, 0.006f), gold), Dir(a));
                        else BodyKit.Ball(w, cp, 0.018f, dia);
                        c++;
                    }
                    break;
                }
                case 3:     // leather strap: edge stitches, studs, tooled dots, buckle, plate, braided cord
                {
                    var lth = M("7A4A24", 0.4f); var st = M("F0DDB8", 0.2f); var brass = M("C9A040", 0.8f, 0.8f); var dark = M("3A2414", 0.3f); var cord = M("5A3418", 0.3f);
                    Ring(w, Vector3.zero, 0.085f, 0.03f, lth, default, new Vector3(1, 1.6f, 1));
                    foreach (var y in new[] { -0.036f, 0.036f }) Stitch(y, 0.106f, 22, st);
                    for (int k = 0; k < 6; k++) BodyKit.Dot(w, Rd(125 + k * 24, 0.116f), 0.014f, brass);
                    for (int k = 0; k < 5; k++) BodyKit.Dot(w, Rd(137 + k * 24, 0.115f), 0.008f, dark);
                    for (int k = 0; k < 4; k++) BodyKit.Dot(w, Rd(345 + k * 23.3f, 0.116f), 0.014f, brass);
                    for (int k = 0; k < 3; k++) BodyKit.Dot(w, Rd(356.6f + k * 23.3f, 0.115f), 0.008f, dark);
                    foreach (var a in new[] { 262f, 278f }) Bx(a, 0.118f, 0, new Vector3(0.006f, 0.06f, 0.006f), brass);   // buckle
                    foreach (var y in new[] { -0.03f, 0.03f }) Bx(270, 0.118f, y, new Vector3(0.034f, 0.006f, 0.006f), brass);
                    Bx(270, 0.121f, 0, new Vector3(0.026f, 0.004f, 0.004f), brass, 90);
                    Bx(287, 0.116f, 0, new Vector3(0.008f, 0.06f, 0.012f), lth);
                    for (int k = 0; k < 5; k++) BodyKit.Dot(w, Rd(298 + k * 10, 0.115f), 0.009f, dark);
                    B(w, new Vector3(0, 0, 0.118f), new Vector3(0.05f, 0.04f, 0.006f), brass);    // plate + screws
                    foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) BodyKit.Dot(w, new Vector3(sx * 0.02f, sy * 0.015f, 0.121f), 0.005f, dark);
                    for (int k = 0; k < 24; k++)                                                    // braided cord
                    {
                        float a = k * 15;
                        BodyKit.Face(BodyKit.Ball(w, Rd(a, 0.088f, 0.058f + (k % 2 == 0 ? 0.004f : -0.004f)), new Vector3(0.018f, 0.011f, 0.012f), cord), Dir(a), k % 2 == 0 ? 35 : -35);
                    }
                    foreach (var a in new[] { 262f, 278f }) BodyKit.Dot(w, Rd(a, 0.094f, 0.058f), 0.014f, brass);
                    break;
                }
                case 4:     // survival paracord: cobra knots, edge loops, beads, buckle with compass, whistle, fire starter
                {
                    var org = M("FF7A1E", 0.1f); var org2 = M("E8620C", 0.1f); var grn = M("2E7D32", 0.3f); var blk = M("1E1E1E", 0.3f); var steel = M("C9CED6", 0.85f, 0.8f);
                    BodyKit.Tor(w, Vector3.zero, 0.088f, 0.02f, org, default, null, 24);
                    for (int k = 0; k < 21; k++)
                    {
                        float a = 140 + k * 260f / 20, off = k % 2 == 0 ? 3 : -3;
                        BodyKit.Face(BodyKit.Ball(w, Rd(a + off, 0.094f, 0.009f), new Vector3(0.026f, 0.022f, 0.02f), org), Dir(a), 20);
                        BodyKit.Face(BodyKit.Ball(w, Rd(a - off, 0.094f, -0.009f), new Vector3(0.026f, 0.022f, 0.02f), org2), Dir(a), -20);
                        BodyKit.Dot(w, Rd(a, 0.092f, 0.027f), 0.012f, org2);
                        BodyKit.Dot(w, Rd(a, 0.092f, -0.027f), 0.012f, org);
                    }
                    for (int k = 0; k < 5; k++) BodyKit.Ball(w, Rd(54 + k * 18, 0.1f), 0.03f, grn);
                    for (int k = 0; k < 4; k++) BodyKit.Dot(w, Rd(63 + k * 18, 0.1f), 0.014f, blk);
                    Bx(262, 0.117f, 0, new Vector3(0.03f, 0.04f, 0.02f), blk);                     // side-release buckle
                    Bx(282, 0.117f, 0, new Vector3(0.026f, 0.035f, 0.018f), blk);
                    foreach (var y in new[] { -0.012f, 0.012f }) Bx(273, 0.119f, y, new Vector3(0.012f, 0.008f, 0.014f), blk);
                    Bx(262, 0.128f, 0, new Vector3(0.016f, 0.004f, 0.004f), M("555555", 0.3f));
                    var cp = Rd(262, 0.13f);                                                        // compass
                    BodyKit.Pin(w, cp, Dir(262), 0.022f, 0.006f, steel);
                    BodyKit.Pin(w, cp + Dir(262) * 0.003f, Dir(262), 0.018f, 0.002f, Glass("BFE6FF", 0.5f));
                    BodyKit.Face(B(w, cp + Dir(262) * 0.002f, new Vector3(0.002f, 0.008f, 0.001f), M("E53935", 0.5f)), Dir(262)).transform.localPosition += Vector3.up * 0.004f;
                    BodyKit.Face(B(w, cp + Dir(262) * 0.002f, new Vector3(0.002f, 0.008f, 0.001f), M("FFFFFF", 0.5f)), Dir(262)).transform.localPosition += Vector3.down * 0.004f;
                    var wp = Rd(220, 0.11f, -0.035f);                                               // whistle
                    Lc(w, wp, new Vector3(0.016f, 0.016f, 0.016f), org2, new Vector3(0, -220, 90));
                    B(w, wp + new Vector3(0, 0.007f, 0), new Vector3(0.008f, 0.004f, 0.006f), blk);
                    LRod(w, Rd(310, 0.112f, -0.04f), Rd(335, 0.112f, -0.04f), 0.007f, M("4A4A4A", 0.6f, 0.6f));   // fire starter rod
                    Lc(w, Rd(306, 0.112f, -0.04f), new Vector3(0.012f, 0.008f, 0.012f), blk, new Vector3(0, -306, 90));
                    break;
                }
                case 5:     // holo watch: link bracelet, emitter, light beam and a spinning holo radar
                {
                    var metal = M("2A3140", 0.7f, 0.5f); var light = M("8A96AA", 0.8f, 0.8f); var holoA = Add("35E0FF", 0.7f); var glow = G("35E0FF", 2f); var red = G("FF3355", 2f);
                    Ring(w, Vector3.zero, 0.085f, 0.02f, metal);
                    for (int k = 0; k < 20; k++)
                    {
                        float a = 122 + k * 296f / 19;
                        Bx(a, 0.102f, 0, new Vector3(0.016f, 0.034f, 0.012f), light);
                        BodyKit.Dot(w, Rd(a + 7.8f, 0.1f), 0.008f, metal);
                    }
                    B(w, new Vector3(0, 0, 0.095f), new Vector3(0.09f, 0.08f, 0.03f), metal);
                    BodyKit.Pin(w, new Vector3(0, 0, 0.112f), Vector3.forward, 0.036f, 0.006f, glow);   // emitter lens
                    BodyKit.Tor(w, new Vector3(0, 0, 0.112f), 0.022f, 0.004f, light, new Vector3(90, 0, 0), null, 10);
                    foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) BodyKit.Dot(w, new Vector3(sx * 0.037f, sy * 0.032f, 0.111f), 0.007f, light);
                    for (int k = 0; k < 3; k++) B(w, new Vector3(-0.015f + k * 0.015f, 0.042f, 0.095f), new Vector3(0.01f, 0.005f, 0.012f), light);
                    for (int k = 0; k < 6; k++) BodyKit.Dot(w, new Vector3(-0.03f + k * 0.012f, -0.03f, 0.111f), 0.006f, k % 2 == 0 ? glow : red);
                    for (int k = 0; k < 8; k++) B(w, new Vector3(-0.03f + k * 0.0086f, -0.041f, 0.095f), new Vector3(0.003f, 0.002f, 0.018f), M("10141C", 0.3f));   // vents
                    B(w, new Vector3(0, 0.026f, 0.111f), new Vector3(0.05f, 0.012f, 0.002f), glow);
                    for (int k = 0; k < 3; k++) B(w, new Vector3(-0.015f + k * 0.015f, 0.026f, 0.1125f), new Vector3(0.008f, 0.003f, 0.002f), metal);
                    BodyKit.Cn(w, new Vector3(0, 0, 0.18f), new Vector3(0, 0, 0.115f), 0.16f, Add("35E0FF", 0.25f));   // light beam
                    var hn = Node("Holo", w, new Vector3(0, 0, 0.18f), new Vector3(90, 0, 0));
                    Add<Spin>(hn.gameObject).speed = 120;
                    Cy(hn, Vector3.zero, new Vector3(0.16f, 0.004f, 0.16f), holoA);
                    foreach (var r in new[] { 0.055f, 0.035f }) BodyKit.Tor(hn, new Vector3(0, 0.003f, 0), r, 0.002f, holoA);
                    for (int k = 0; k < 6; k++) B(hn, new Vector3(0, 0.003f, 0), new Vector3(0.002f, 0.002f, 0.14f), holoA, new Vector3(0, k * 30, 0));
                    for (int k = 0; k < 12; k++) BodyKit.Dot(hn, Rd(k * 30 + (k % 3) * 7, 0.065f, 0.004f), 0.008f, holoA);
                    BodyKit.Dot(hn, new Vector3(0, 0.004f, 0), 0.012f, holoA);
                    BodyKit.Tor(hn, new Vector3(0, 0.004f, 0), 0.078f, 0.0015f, holoA);
                    for (int k = 0; k < 8; k++) BodyKit.Dot(hn, Rd(k * 45 + 20, 0.078f, 0.005f), 0.006f, holoA);
                    B(w, new Vector3(0, 0, -0.106f), new Vector3(0.03f, 0.03f, 0.01f), light);    // clasp
                    foreach (var sx in new[] { -1f, 1f }) BodyKit.Dot(w, new Vector3(sx * 0.01f, 0, -0.112f), 0.006f, metal);
                    break;
                }
                case 6:     // spiked cuff: two rows of spikes on washers, studs, stitched edges, snap
                {
                    var blk = M("151515", 0.4f); var sil = M("D9DEE6", 0.9f, 0.9f); var st = M("6A6A6A", 0.2f);
                    Cy(w, Vector3.zero, new Vector3(0.18f, 0.035f, 0.18f), blk);
                    foreach (var y in new[] { -0.035f, 0.035f }) BodyKit.Tor(w, new Vector3(0, y, 0), 0.09f, 0.005f, sil);
                    for (int r = 0; r < 2; r++)
                        for (int k = 0; k < 10; k++)
                        {
                            float a = k * 36 + r * 18, y = r == 0 ? 0.016f : -0.016f;
                            BodyKit.Cn(w, Rd(a, 0.09f, y), Rd(a, 0.135f, y), 0.028f, sil);
                            BodyKit.Face(BodyKit.Ball(w, Rd(a, 0.091f, y), new Vector3(0.034f, 0.034f, 0.006f), sil), Dir(a));
                            if (r == 0) BodyKit.Dot(w, Rd(a + 18, 0.092f, 0.016f), 0.012f, sil);
                        }
                    foreach (var y in new[] { -0.029f, 0.029f }) Stitch(y, 0.092f, 24, st);
                    BodyKit.Pin(w, Rd(279, 0.093f, 0), Dir(279), 0.02f, 0.006f, sil);             // snap
                    BodyKit.Dot(w, Rd(279, 0.097f, 0), 0.01f, sil);
                    break;
                }
                default:    // magic amulet: runes, pulsing gem in a setting, orbiting crystals, filigree, charms, beads
                {
                    var gold = M("FFC83A", 0.8f, 0.8f); var rune = M("3A1C6E", 0.4f); var runeG = G("D9A8FF", 2f); var bead = M("7B3FE4", 0.9f, 0.2f); var gemG = G("B57CFF", 2.2f);
                    Ring(w, Vector3.zero, 0.085f, 0.018f, gold);
                    foreach (var y in new[] { -0.022f, 0.022f }) BodyKit.Tor(w, new Vector3(0, y, 0), 0.086f, 0.004f, gold);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = 125 + k * 290f / 13;
                        Bx(a, 0.103f, 0, new Vector3(0.024f, 0.03f, 0.006f), rune);
                        Bx(a, 0.107f, 0.003f, new Vector3(0.003f, 0.016f, 0.002f), runeG, 25 + k * 40);
                        Bx(a, 0.107f, -0.004f, new Vector3(0.003f, 0.012f, 0.002f), runeG, -40 + k * 25);
                    }
                    var gem = S(w, new Vector3(0, 0, 0.1f), new Vector3(0.07f, 0.09f, 0.05f), gemG);
                    Add<Pulse>(gem);
                    BodyKit.Tor(w, new Vector3(0, 0, 0.096f), 0.04f, 0.006f, gold, new Vector3(90, 0, 0), new Vector3(1, 1, 1.25f));
                    for (int k = 0; k < 6; k++) { float a = (k * 60 + 30) * Mathf.Deg2Rad; BodyKit.Dot(w, new Vector3(Mathf.Cos(a) * 0.036f, Mathf.Sin(a) * 0.046f, 0.108f), 0.008f, gold); }
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; BodyKit.Dot(w, new Vector3(Mathf.Cos(a) * 0.07f, Mathf.Sin(a) * 0.07f, 0.096f), 0.01f, runeG); }
                    foreach (var ph in new[] { 58f, 122f })                                         // filigree curls
                    {
                        var c = Rd(ph, 0.105f); var tg = new Vector3(-Mathf.Sin(ph * Mathf.Deg2Rad), 0, Mathf.Cos(ph * Mathf.Deg2Rad));
                        for (int k = 0; k < 10; k++)
                        {
                            float s = k * 54 * Mathf.Deg2Rad, rr = 0.02f - k * 0.0016f;
                            BodyKit.Dot(w, c + tg * (rr * Mathf.Cos(s)) + Vector3.up * (rr * Mathf.Sin(s)), 0.007f, gold);
                        }
                    }
                    foreach (var a in new[] { 75f, 105f })                                          // moon + star charms
                    {
                        var p = Rd(a, 0.1f, -0.04f);
                        BodyKit.Tor(w, p, 0.006f, 0.0015f, gold, new Vector3(90, -a, 0), null, 10);
                        var cp = p + Vector3.down * 0.02f;
                        if (a < 90)
                        {
                            BodyKit.Pin(w, cp, Dir(a), 0.022f, 0.004f, gold);
                            BodyKit.Pin(w, cp + Dir(a) * 0.002f + new Vector3(0.005f, 0.004f, 0), Dir(a), 0.018f, 0.004f, rune);
                        }
                        else BodyKit.Face(Msh(Meshes.Star(), w, cp, new Vector3(0.024f, 0.024f, 0.006f), gold), Dir(a));
                    }
                    for (int k = 0; k < 22; k++) BodyKit.Dot(w, Rd(k * 360f / 22, 0.09f, -0.034f), 0.012f, k % 2 == 0 ? bead : gold);
                    break;
                }
            }
        }

        void Belt(int i, int set)
        {
            var b = Node("Belt", Torso, new Vector3(0, 0.07f, 0));
            if (!string.IsNullOrEmpty(look.Of("pants"))) b.localScale = new Vector3(1.15f, 1, 1.15f);   // sits over trousers
            const float rx = 0.3f, rz = 0.264f;
            var circ = Dt.Circle(Vector3.zero, rx, rz);
            Func<Vector3, Vector3> rad = p => new Vector3(p.x / (rx * rx), 0, p.z / (rz * rz)).normalized;
            Vector3 At(float deg, float y = 0, float g = 1f) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * rx * g, y, Mathf.Sin(a) * rz * g); }
            GameObject On(GameObject go, float deg, float roll = 0) => BodyKit.Face(go, rad(At(deg)), roll);
            GameObject Bx(float deg, float y, float g, Vector3 size, Material m, float roll = 0) => On(B(b, At(deg, y, g), size, m), deg, roll);
            void Strap(Material m, float h = 0.06f, int n = 24, float depth = 0.018f) => BodyKit.Band(b, circ, rad, n, h, depth, m);
            void Edges(float h, int n, Material m, float g = 1.035f) { foreach (var sy in new[] { -1f, 1f }) Dt.Stitches(b, BodyKit.Loop(new Vector3(0, sy * h * 0.36f, 0), rx * g, rz * g, n, 90f / n), n, 0.016f, 0.005f, m); }
            void Buckle(Material m, float w = 0.07f, float hh = 0.08f)                              // frame, prong, keeper
            {
                var c = At(90) + new Vector3(0, 0, 0.016f);
                foreach (var sx in new[] { -1f, 1f })
                {
                    B(b, c + new Vector3(sx * w / 2, 0, 0), new Vector3(0.01f, hh, 0.012f), m);
                    B(b, c + new Vector3(0, sx * hh / 2, 0), new Vector3(w + 0.01f, 0.01f, 0.012f), m);
                }
                B(b, c + new Vector3(-w * 0.2f, 0, 0.006f), new Vector3(w * 0.6f, 0.007f, 0.007f), m);
                Bx(110, 0, 1.04f, new Vector3(0.014f, hh * 0.9f, 0.02f), m);
            }
            switch (i)
            {
                case 0:     // star buckle belt: stitched edges, holes, plate with rope border, conchos
                {
                    var lth = M("6D4425", 0.4f); var st = M("E8C9A0", 0.2f); var gold = M("FFC83A", 0.9f, 0.9f); var dark = M("2E1A0E", 0.3f);
                    Strap(lth, 0.06f, 26); Edges(0.06f, 26, st);
                    for (int k = 0; k < 5; k++) BodyKit.Dot(b, At(115 + k * 5, 0, 1.035f), 0.01f, dark);
                    Bx(142, 0, 1.04f, new Vector3(0.03f, 0.05f, 0.006f), gold);
                    Bx(108, 0, 1.04f, new Vector3(0.012f, 0.07f, 0.03f), lth);
                    var pc = At(90) + new Vector3(0, 0, 0.019f);
                    B(b, pc, new Vector3(0.13f, 0.1f, 0.012f), dark);
                    Msh(Meshes.Star(), b, pc + new Vector3(0, 0, 0.012f), new Vector3(0.12f, 0.12f, 0.04f), gold);
                    for (int k = 0; k < 5; k++) { float a = Mathf.PI / 2 + k * Mathf.PI * 2 / 5; BodyKit.Dot(b, pc + new Vector3(Mathf.Cos(a) * 0.052f, Mathf.Sin(a) * 0.052f, 0.034f), 0.012f, M("FFE9A0", 0.9f, 0.6f)); }
                    for (int k = 0; k < 14; k++)                                                    // rope border
                    {
                        float a = k * Mathf.PI * 2 / 14;
                        BodyKit.Dot(b, pc + new Vector3(Mathf.Cos(a) * 0.062f, Mathf.Sin(a) * 0.047f, 0.008f), 0.011f, gold);
                    }
                    foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) BodyKit.Dot(b, pc + new Vector3(sx * 0.058f, sy * 0.043f, 0.008f), 0.008f, gold);
                    foreach (var a in new[] { 20f, 160f, 215f, 250f, 290f, 325f })                  // conchos
                    {
                        On(Lc(b, At(a, 0, 1.04f), new Vector3(0.035f, 0.003f, 0.035f), gold), a).transform.localRotation *= Quaternion.Euler(90, 0, 0);
                        BodyKit.Dot(b, At(a, 0, 1.065f), 0.012f, gold);
                    }
                    break;
                }
                case 1:     // webbing belt with pouches, side-release buckle, carabiner, canteen
                {
                    var olv = M("4E5A32", 0.25f); var dk = M("3E4826", 0.25f); var st = M("2F3620", 0.2f); var blk = M("1E1E1E", 0.3f); var met = M("9AA5B4", 0.8f, 0.7f);
                    Strap(olv, 0.065f); Edges(0.065f, 24, st);
                    foreach (var a in new[] { 30f, 150f, -20f, 200f })                              // pouches
                    {
                        Bx(a, -0.035f, 1.14f, new Vector3(0.09f, 0.1f, 0.06f), dk);
                        Bx(a, 0.012f, 1.17f, new Vector3(0.095f, 0.035f, 0.065f), olv);
                        On(BodyKit.Pin(b, At(a, 0.0f, 1.29f), Vector3.up, 0.016f, 0.006f, met), a).transform.localRotation *= Quaternion.Euler(90, 0, 0);
                        foreach (var sd in new[] { -1f, 1f }) Dt.Stitches(b, Dt.Line(At(a + sd * 8.5f, -0.07f, 1.26f), At(a + sd * 8.5f, -0.01f, 1.26f)), 2, 0.016f, 0.005f, st);
                    }
                    var bc = At(90) + new Vector3(0, 0, 0.02f);                                     // side-release buckle
                    B(b, bc + new Vector3(-0.02f, 0, 0), new Vector3(0.05f, 0.055f, 0.02f), blk);
                    B(b, bc + new Vector3(0.03f, 0, 0), new Vector3(0.04f, 0.045f, 0.018f), blk);
                    foreach (var sy in new[] { -1f, 1f }) B(b, bc + new Vector3(0.008f, sy * 0.016f, 0.003f), new Vector3(0.016f, 0.01f, 0.016f), blk);
                    B(b, bc + new Vector3(-0.02f, 0, 0.011f), new Vector3(0.026f, 0.006f, 0.004f), M("4A4A4A", 0.3f));
                    var dr = At(60, -0.04f, 1.06f);                                                 // carabiner
                    BodyKit.Tor(b, dr, 0.02f, 0.004f, met, new Vector3(0, -60, 90), new Vector3(1, 1, 1.4f), 10);
                    LRod(b, dr + new Vector3(0, 0.022f, 0), dr + new Vector3(0, -0.022f, 0.006f), 0.004f, met);
                    BodyKit.Dot(b, dr + new Vector3(0, 0.026f, 0), 0.008f, met);
                    var cn = At(300, -0.05f, 1.2f);                                                 // canteen
                    Cy(b, cn, new Vector3(0.09f, 0.05f, 0.07f), olv);
                    Lc(b, cn + new Vector3(0, 0.06f, 0), new Vector3(0.03f, 0.012f, 0.03f), blk);
                    Bx(300, 0.0f, 1.07f, new Vector3(0.025f, 0.07f, 0.012f), dk);
                    break;
                }
                case 2:     // spiked belt: spikes on washers, studs, stitched edges, buckle
                {
                    var lth = M("151515", 0.4f); var sil = M("D9DEE6", 0.9f, 0.9f); var st = M("5A5A5A", 0.2f);
                    Strap(lth); Edges(0.06f, 24, st);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = 115 + k * 310f / 13;
                        var bs = At(a, 0, 1.03f);
                        BodyKit.Cn(b, bs, bs + rad(bs) * 0.06f, 0.035f, sil);
                        On(BodyKit.Ball(b, bs, new Vector3(0.04f, 0.04f, 0.008f), sil), a);
                        BodyKit.Dot(b, At(a + 11.9f, 0, 1.035f), 0.016f, sil);
                    }
                    Buckle(sil);
                    break;
                }
                case 3:     // ninja belt: two cloth wraps, creases, knot with fringed tails, shuriken, kunai, pouch
                {
                    var cl = M("1A1A1A", 0.1f); var cl2 = M("2A2A2E", 0.1f); var cr = M("0E0E10", 0.1f); var steel = M("C9CED6", 0.85f, 0.8f); var red = M("C62828", 0.3f);
                    BodyKit.Band(b, Dt.Circle(new Vector3(0, 0.022f, 0), rx, rz), rad, 24, 0.05f, 0.02f, cl);
                    BodyKit.Band(b, Dt.Circle(new Vector3(0, -0.022f, 0), rx * 1.01f, rz * 1.01f), rad, 24, 0.05f, 0.02f, cl2);
                    Dt.Stitches(b, BodyKit.Loop(Vector3.zero, rx * 1.04f, rz * 1.04f, 24), 24, 0.03f, 0.004f, cr);
                    var kn = At(35, 0, 1.08f);                                                      // knot
                    BodyKit.Ball(b, kn, new Vector3(0.06f, 0.05f, 0.04f), cl);
                    foreach (var sd in new[] { -1f, 1f }) BodyKit.Ball(b, kn + new Vector3(sd * 0.03f, 0.01f, -0.005f), new Vector3(0.04f, 0.03f, 0.03f), cl2);
                    foreach (var sd in new[] { -1f, 1f })                                           // tails with fringe
                    {
                        var p = kn + new Vector3(sd * 0.03f, -0.02f, 0.005f);
                        for (int j = 0; j < 3; j++)
                        {
                            var q = Quaternion.Euler(0, -55, sd * (8 + j * 6));
                            var d = q * Vector3.down;
                            B(b, p + d * 0.036f, new Vector3(0.05f, 0.075f, 0.02f), j % 2 == 0 ? cl : cl2).transform.localRotation = q;
                            p += d * 0.072f;
                        }
                        var fq = Quaternion.Euler(0, -55, sd * 20);
                        for (int f = 0; f < 4; f++) { var fp = p + fq * new Vector3(-0.018f + f * 0.012f, 0, 0); LRod(b, fp, fp + fq * Vector3.down * 0.03f, 0.005f, cl); }
                    }
                    var sh = At(140, 0.005f, 1.07f);                                                // shuriken
                    On(Msh(Meshes.Star(4, 0.3f), b, sh, new Vector3(0.07f, 0.07f, 0.01f), steel), 140, 15);
                    BodyKit.Dot(b, sh + rad(sh) * 0.006f, 0.014f, cl);
                    var kb = At(210, 0.01f, 1.08f);                                                 // kunai
                    BodyKit.Cn(b, kb + Vector3.up * 0.02f, kb + Vector3.up * 0.1f, 0.026f, steel);
                    LRod(b, kb + Vector3.up * 0.02f, kb + Vector3.down * 0.05f, 0.012f, cl2);
                    for (int k = 0; k < 3; k++) Lc(b, kb + Vector3.down * (0.005f + k * 0.018f), new Vector3(0.016f, 0.004f, 0.016f), red);
                    BodyKit.Tor(b, kb + Vector3.down * 0.062f, 0.012f, 0.003f, steel, new Vector3(0, -210, 90), null, 10);
                    Bx(320, -0.01f, 1.11f, new Vector3(0.06f, 0.06f, 0.04f), cl2);                 // pouch
                    Bx(320, 0.018f, 1.13f, new Vector3(0.064f, 0.02f, 0.045f), cl);
                    BodyKit.Dot(b, At(320, 0.015f, 1.22f), 0.01f, red);
                    for (int k = 0; k < 8; k++) Bx(60 + k * 10, -0.022f, 1.06f, new Vector3(0.012f, 0.012f, 0.004f), red, 45);   // red diamonds
                    break;
                }
                case 4:     // cobalt Sam Browne belt: waist belt + cross strap, stitches, chrome buckle, D-ring
                {
                    var blue = M("1F4FD1", 0.5f); var st = M("BFD0FF", 0.2f); var chr = M("D9DEE6", 0.9f, 0.9f);
                    Strap(blue); Edges(0.06f, 24, st);
                    var q = Quaternion.Euler(0, 0, 40); var cc = new Vector3(0, 0.21f, 0);
                    Func<float, Vector3> cross = tt => { float a = tt * Mathf.PI * 2; return cc + q * new Vector3(Mathf.Cos(a) * 0.33f, 0, Mathf.Sin(a) * 0.297f); };
                    Func<Vector3, Vector3> cn = p => { var l = Quaternion.Inverse(q) * (p - cc); return q * new Vector3(l.x / 0.1089f, 0, l.z / 0.0882f).normalized; };
                    BodyKit.Band(b, cross, cn, 22, 0.045f, 0.016f, blue);
                    foreach (var sy in new[] { -1f, 1f }) Dt.Stitches(b, tt => { var p = cross(tt * 15f / 16); return p + q * Vector3.up * (sy * 0.016f) + cn(p) * 0.009f; }, 16, 0.016f, 0.005f, st);
                    Buckle(chr);
                    var dp = At(180, 0.02f, 1.06f);                                                 // D-ring where the strap meets the belt
                    BodyKit.Tor(b, dp, 0.022f, 0.005f, chr, new Vector3(0, 0, 90), null, 10);
                    foreach (var sy in new[] { -1f, 1f }) BodyKit.Dot(b, dp + new Vector3(-0.012f, sy * 0.02f, 0), 0.01f, chr);
                    Bx(140, 0, 1.04f, new Vector3(0.03f, 0.05f, 0.006f), chr);                     // strap tip
                    break;
                }
                case 5:     // tool belt: pouches with rivets, hammer, wrench, screwdriver, pliers, tape measure, pencil
                {
                    var lth = M("7A4A24", 0.4f); var dk = M("5A3418", 0.4f); var st = M("F0DDB8", 0.2f); var wood = M("8D6E63", 0.4f); var met = M("9AA5B4", 0.8f, 0.7f);
                    var steel = M("C9CED6", 0.85f, 0.8f); var yel = M("FFC400", 0.3f); var red = M("E53935", 0.3f); var blk = M("1E1E1E", 0.3f);
                    Strap(lth, 0.065f); Edges(0.065f, 24, st); Buckle(met, 0.06f, 0.075f);
                    foreach (var a in new[] { 40f, 140f })                                          // pouches
                    {
                        Bx(a, -0.05f, 1.13f, new Vector3(0.11f, 0.12f, 0.05f), lth);
                        Bx(a, -0.075f, 1.22f, new Vector3(0.1f, 0.06f, 0.012f), dk);
                        foreach (var sx in new[] { -1f, 1f }) foreach (var sy in new[] { -1f, 1f }) BodyKit.Dot(b, At(a + sx * 9, -0.075f + sy * 0.022f, 1.25f), 0.01f, met);
                        Dt.Stitches(b, Dt.Line(At(a - 8, -0.043f, 1.245f), At(a + 8, -0.043f, 1.245f)), 3, 0.016f, 0.005f, st);
                    }
                    var hp = At(165, 0, 1.12f);                                                     // hammer in a loop
                    LRod(b, hp + Vector3.up * 0.03f, hp + Vector3.down * 0.15f, 0.02f, wood);
                    B(b, hp + Vector3.up * 0.04f, new Vector3(0.025f, 0.025f, 0.08f), met, new Vector3(0, -165, 0));
                    foreach (var sd in new[] { -1f, 1f }) BodyKit.Cn(b, hp + Vector3.up * 0.04f + Quaternion.Euler(0, -165, 0) * new Vector3(0, 0, -0.035f), hp + Vector3.up * (0.03f + sd * 0.01f) + Quaternion.Euler(0, -165, 0) * new Vector3(0, sd * 0.012f, -0.075f), 0.016f, met);
                    Bx(165, 0, 1.05f, new Vector3(0.03f, 0.07f, 0.02f), dk);
                    var wp = At(15, -0.08f, 1.1f);                                                  // wrench
                    On(B(b, wp, new Vector3(0.016f, 0.13f, 0.006f), met), 15);
                    foreach (var sx in new[] { -1f, 1f }) On(B(b, wp + Vector3.down * 0.07f + Quaternion.Euler(0, -15, 0) * new Vector3(0, 0, sx * 0.012f), new Vector3(0.008f, 0.025f, 0.006f), met), 15);
                    On(BodyKit.Tor(b, wp + Vector3.up * 0.068f, 0.012f, 0.004f, met, default, null, 10), 15).transform.localRotation *= Quaternion.Euler(90, 0, 0);
                    var sp = At(48, 0.03f, 1.15f);                                                  // screwdriver
                    Lc(b, sp, new Vector3(0.02f, 0.025f, 0.02f), yel);
                    for (int k = 0; k < 3; k++) Lc(b, sp + Vector3.up * (-0.015f + k * 0.015f), new Vector3(0.022f, 0.003f, 0.022f), blk);
                    LRod(b, sp + Vector3.up * 0.025f, sp + Vector3.up * 0.06f, 0.006f, steel);
                    B(b, sp + Vector3.up * 0.063f, new Vector3(0.008f, 0.006f, 0.002f), steel);
                    var pp = At(132, 0.03f, 1.15f);                                                 // pliers
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        LRod(b, pp + new Vector3(sd * 0.006f, -0.02f, 0), pp + new Vector3(sd * 0.012f, 0.03f, 0), 0.01f, red);
                        BodyKit.Cn(b, pp + new Vector3(sd * 0.004f, 0.035f, 0), pp + new Vector3(sd * 0.002f, 0.065f, 0), 0.012f, steel);
                    }
                    BodyKit.Dot(b, pp + new Vector3(0, 0.035f, 0), 0.012f, steel);
                    var tp = At(0, -0.02f, 1.13f);                                                  // tape measure
                    BodyKit.Pin(b, tp, Vector3.right, 0.06f, 0.03f, yel);
                    BodyKit.Pin(b, tp + Vector3.right * 0.016f, Vector3.right, 0.025f, 0.004f, blk);
                    B(b, tp + new Vector3(0, -0.03f, 0.02f), new Vector3(0.012f, 0.008f, 0.015f), steel);
                    B(b, tp + new Vector3(-0.02f, 0.01f, 0), new Vector3(0.006f, 0.05f, 0.02f), steel);
                    var pn = At(30, 0.03f, 1.16f);                                                  // pencil
                    Lc(b, pn, new Vector3(0.01f, 0.03f, 0.01f), yel);
                    BodyKit.Cn(b, pn + Vector3.up * 0.03f, pn + Vector3.up * 0.045f, 0.01f, wood);
                    Lc(b, pn + Vector3.down * 0.033f, new Vector3(0.01f, 0.004f, 0.01f), M("FF8FB0", 0.3f));
                    break;
                }
                case 6:     // gold chain belt: links, front swag with beads, medallion, clasp, dangling tail
                {
                    var gold = M("FFC83A", 0.9f, 0.9f); var ruby = M("E0115F", 0.9f, 0.2f);
                    BodyKit.Chain(b, BodyKit.Loop(Vector3.zero, rx * 1.02f, rz * 1.02f, 48), 48, 0.016f, 0.005f, gold);
                    Vector3 Sw(float tt) => At(Mathf.Lerp(50, 130, tt), -0.08f * Mathf.Sin(tt * Mathf.PI), 1.06f + 0.06f * Mathf.Sin(tt * Mathf.PI));
                    BodyKit.Chain(b, Sw, 26, 0.013f, 0.004f, gold);
                    for (int k = 0; k < 8; k++) BodyKit.Dot(b, Sw(0.1f + k * 0.114f) + Vector3.down * 0.008f, 0.012f, gold);
                    var md = Sw(0.5f) + new Vector3(0, -0.05f, 0.005f);                             // medallion
                    BodyKit.Tor(b, md + Vector3.up * 0.035f, 0.01f, 0.003f, gold, new Vector3(0, 0, 90), null, 10);
                    BodyKit.Pin(b, md, Vector3.forward, 0.06f, 0.008f, gold);
                    Msh(Meshes.Star(), b, md + new Vector3(0, 0, 0.006f), new Vector3(0.04f, 0.04f, 0.008f), gold);
                    BodyKit.Ball(b, md + new Vector3(0, 0, 0.011f), new Vector3(0.016f, 0.016f, 0.01f), ruby);
                    for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; BodyKit.Dot(b, md + new Vector3(Mathf.Cos(a) * 0.03f, Mathf.Sin(a) * 0.03f, 0.003f), 0.008f, gold); }
                    var cl = At(200, 0, 1.06f);                                                     // clasp + dangling tail
                    BodyKit.Tor(b, cl, 0.012f, 0.004f, gold, new Vector3(0, 0, 90), null, 10);
                    B(b, cl + new Vector3(0, 0, 0.0f) + rad(cl) * 0.006f, new Vector3(0.014f, 0.024f, 0.01f), gold);
                    BodyKit.Dot(b, cl + Vector3.down * 0.016f, 0.01f, gold);
                    BodyKit.Chain(b, tt => cl + new Vector3(0, -0.02f - tt * 0.08f, 0) + rad(cl) * 0.01f, 6, 0.01f, 0.003f, gold);
                    break;
                }
                default:    // cloth sash: fold lines, knot with loops, fringed ends, gold embroidery
                {
                    var tl = M("00897B", 0.1f); var tl2 = M("00695C", 0.1f); var gold = M("FFC83A", 0.8f, 0.8f); var fr = M("4DB6AC", 0.1f);
                    Strap(tl, 0.085f, 24, 0.02f);
                    foreach (var y in new[] { -0.015f, 0.015f }) Dt.Stitches(b, BodyKit.Loop(new Vector3(0, y, 0), rx * 1.035f, rz * 1.035f, 24, y > 0 ? 0 : 7.5f), 24, 0.03f, 0.004f, tl2);
                    var kn = At(50, 0, 1.08f);
                    BodyKit.Mid(b, kn, new Vector3(0.06f, 0.055f, 0.045f), tl);
                    foreach (var sd in new[] { -1f, 1f }) BodyKit.Ball(b, kn + new Vector3(sd * 0.04f, 0.015f, 0), new Vector3(0.05f, 0.04f, 0.03f), tl).transform.localRotation = Quaternion.Euler(0, -50, sd * 25);
                    foreach (var sd in new[] { -1f, 1f })                                           // fringed ends
                    {
                        var p = kn + new Vector3(sd * 0.025f, -0.02f, 0.01f);
                        for (int j = 0; j < 3; j++)
                        {
                            var q = Quaternion.Euler(0, -50, sd * (6 + j * 4));
                            var d = q * Vector3.down;
                            B(b, p + d * 0.03f, new Vector3(0.06f, 0.062f, 0.015f), j == 1 ? tl2 : tl).transform.localRotation = q;
                            p += d * 0.06f;
                        }
                        var fq = Quaternion.Euler(0, -50, sd * 14);
                        for (int f = 0; f < 5; f++) { var fp = p + fq * new Vector3(-0.024f + f * 0.012f, 0, 0); LRod(b, fp, fp + fq * Vector3.down * 0.035f, 0.005f, fr); }
                    }
                    for (int k = 0; k < 16; k++) { float a = k < 8 ? 75 + k * 11 : 205 + (k - 8) * 17; BodyKit.Dot(b, At(a, (k % 2 == 0 ? 0.012f : -0.012f), 1.045f), 0.012f, gold); }
                    break;
                }
            }
        }
    }

    /// <summary>Detail helpers for body wear: points on ellipsoids, curved panels, bands, knit ribs, chains, digits, low-poly balls.</summary>
    static class BodyKit
    {
        public static Vector3 E(Vector3 c, Vector3 r, float th, float ph, float g = 1.02f) => Dt.Ell(c, r, th, ph, g);
        /// <summary>Outward normal of an ellipsoid at a point near its surface.</summary>
        public static Func<Vector3, Vector3> EN(Vector3 c, Vector3 r) => p => new Vector3((p.x - c.x) / (r.x * r.x), (p.y - c.y) / (r.y * r.y), (p.z - c.z) / (r.z * r.z)).normalized;
        /// <summary>Path over an ellipsoid between two (theta, phi) points.</summary>
        public static Func<float, Vector3> SP(Vector3 c, Vector3 r, float th0, float th1, float ph0, float ph1, float g = 1.03f) => t => E(c, r, Mathf.Lerp(th0, th1, t), Mathf.Lerp(ph0, ph1, t), g);
        /// <summary>Closed ellipse around Y sampled n times without repeating the start.</summary>
        public static Func<float, Vector3> Loop(Vector3 c, float rx, float rz, int n, float a0 = 0) => Dt.Circle(c, rx, rz, a0, a0 + 360f * (n - 1) / n);
        /// <summary>Point on the front (+Z) of an ellipsoid at (x, y).</summary>
        public static Vector3 Front(Vector3 c, Vector3 r, float x, float y, float g = 1.03f)
        {
            float k = 1 - Mathf.Pow((x - c.x) / r.x, 2) - Mathf.Pow((y - c.y) / r.y, 2);
            return new Vector3(x, y, c.z + r.z * Mathf.Sqrt(Mathf.Max(0.0001f, k)) * g);
        }

        public static GameObject Ball(Transform p, Vector3 pos, Vector3 sc, Material m) => Msh(Meshes.Ball(8, 6), p, pos, sc, m);
        public static GameObject Ball(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(8, 6), p, pos, Vector3.one * d, m);
        public static GameObject Mid(Transform p, Vector3 pos, Vector3 sc, Material m) => Msh(Meshes.Ball(12, 8), p, pos, sc, m);
        public static GameObject Dot(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(6, 4), p, pos, Vector3.one * d, m);
        public static GameObject Tor(Transform p, Vector3 pos, float R, float r, Material m, Vector3 euler = default, Vector3? sc = null, int seg = 16)
            => Msh(Meshes.Torus(R, r, seg, seg < 16 ? 5 : 6), p, pos, sc ?? Vector3.one, m, euler);
        /// <summary>Low-poly cone from a (base) to b (tip).</summary>
        public static GameObject Cn(Transform p, Vector3 a, Vector3 b, float d, Material m) => Dt.Orient(Msh(Meshes.Cone(8), p, a, new Vector3(d, (b - a).magnitude, d), m), b - a);
        /// <summary>Short cylinder (button, rivet, snap, lens) of diameter d and height h along axis.</summary>
        public static GameObject Pin(Transform p, Vector3 pos, Vector3 axis, float d, float h, Material m) => Dt.Orient(Lc(p, pos, new Vector3(d, h / 2, d), m), axis);
        /// <summary>Turns a part so its +Z looks along n (Y stays up), then rolls it.</summary>
        public static GameObject Face(GameObject go, Vector3 n, float roll = 0) { go.transform.localRotation = Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(0, 0, roll); return go; }
        /// <summary>Box lying on an ellipsoid (x across, y up the surface, z = thickness).</summary>
        public static GameObject Flat(Transform p, Vector3 c, Vector3 r, float th, float ph, Vector3 size, Material m, float g = 1.02f, float roll = 0)
            => Face(B(p, E(c, r, th, ph, g), size, m), Dt.EllN(r, th, ph), roll);
        public static GameObject Disc(Transform p, Vector3 c, Vector3 r, float th, float ph, float d, float h, Material m, float g = 1.03f)
            => Pin(p, E(c, r, th, ph, g), Dt.EllN(r, th, ph), d, h, m);

        /// <summary>Curved panel (pocket, plate, stripe): nu × nv boxes following the ellipsoid.</summary>
        public static void Panel(Transform p, Vector3 c, Vector3 r, float th0, float th1, float ph0, float ph1, int nu, int nv, float depth, Material m, float g = 1.02f)
        {
            float rv = (r.y + (r.x + r.z) * 0.5f) * 0.5f;
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    float th = Mathf.Lerp(th0, th1, (j + 0.5f) / nv), ph = Mathf.Lerp(ph0, ph1, (i + 0.5f) / nu);
                    float hr = Mathf.Sin(th * Mathf.Deg2Rad) * Mathf.Lerp(r.x, r.z, Mathf.Abs(Mathf.Sin(ph * Mathf.Deg2Rad)));
                    float w = hr * Mathf.Abs(ph1 - ph0) * Mathf.Deg2Rad / nu * g * 1.1f, h = rv * Mathf.Abs(th1 - th0) * Mathf.Deg2Rad / nv * g * 1.1f;
                    Flat(p, c, r, th, ph, new Vector3(w, h, depth), m, g);
                }
        }

        /// <summary>Strap / binding: n boxes along a path (w across, depth thick), facing nrm.</summary>
        public static void Band(Transform p, Func<float, Vector3> path, Func<Vector3, Vector3> nrm, int n, float w, float depth, Material m, float over = 1.12f)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 a = path(i / (float)n), b = path((i + 1) / (float)n), d = b - a, c = (a + b) / 2;
                B(p, c, new Vector3(w, d.magnitude * over, depth), m).transform.localRotation = Quaternion.LookRotation(nrm(c), d);
            }
        }

        /// <summary>Knit cuff / waistband: a short cylinder with n vertical ribs.</summary>
        public static void Knit(Transform p, Vector3 pos, float rx, float rz, float h, int n, Material m, Material rib, Vector3 euler = default, float w = 0.013f)
        {
            var q = Quaternion.Euler(euler);
            Cy(p, pos, new Vector3(rx * 2, h / 2, rz * 2), m, euler);
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2 / n;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                B(p, pos + q * new Vector3(d.x * rx, 0, d.z * rz), new Vector3(w, h * 0.96f, w * 0.8f), rib).transform.localRotation = Quaternion.LookRotation(q * d, q * Vector3.up);
            }
        }

        /// <summary>Fur tufts spread evenly (jittered rows) over an ellipsoid band.</summary>
        public static void Tufts(Transform p, Vector3 c, Vector3 r, int rows, int cols, float th0, float th1, float d, Material m, int seed, float g = 1f, float ph0 = 0, float ph1 = 360)
        {
            var rg = new Rng(seed);
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    float th = Mathf.Lerp(th0, th1, (j + rg.F(0.25f, 0.75f)) / rows);
                    float ph = Mathf.Lerp(ph0, ph1, (i + (j % 2) * 0.5f + rg.F(-0.2f, 0.2f)) / cols);
                    Ball(p, E(c, r, th, ph, g), d * rg.F(0.8f, 1.2f), m);
                }
        }

        /// <summary>Chain of interlocking links along a path (every other link turned 90°).</summary>
        public static void Chain(Transform p, Func<float, Vector3> path, int n, float R, float r, Material m, float stretch = 1.5f)
        {
            Dt.Path(path, n, (pos, dir, i) =>
                Msh(Meshes.Torus(R, r, 10, 5), p, pos, new Vector3(stretch, 1, 1), m).transform.localRotation = Quaternion.FromToRotation(Vector3.right, dir) * Quaternion.Euler(i % 2 * 90, 0, 0));
        }

        static readonly int[] Seg7 = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };   // bits a..g

        /// <summary>Seven-segment digit lying on an ellipsoid (H = height); +phi is the viewer's right.</summary>
        public static void Digit(Transform p, Vector3 c, Vector3 r, float th, float ph, int digit, float H, Material m, float g = 1.03f)
        {
            float W = H * 0.55f, T = H * 0.17f;
            float[] u = { 0, 0.5f, 0.5f, 0, -0.5f, -0.5f, 0 }, v = { 0.5f, 0.25f, -0.25f, -0.5f, -0.25f, 0.25f, 0 };
            float hr = Mathf.Sin(th * Mathf.Deg2Rad) * Mathf.Lerp(r.x, r.z, Mathf.Abs(Mathf.Sin(ph * Mathf.Deg2Rad)));
            for (int s = 0; s < 7; s++)
            {
                if ((Seg7[digit] >> s & 1) == 0) continue;
                bool hz = s == 0 || s == 3 || s == 6;
                Flat(p, c, r, th - v[s] * H / r.y * Mathf.Rad2Deg, ph + u[s] * W / hr * Mathf.Rad2Deg, hz ? new Vector3(W, T, 0.008f) : new Vector3(T, H * 0.5f, 0.008f), m, g);
            }
        }
    }
}
