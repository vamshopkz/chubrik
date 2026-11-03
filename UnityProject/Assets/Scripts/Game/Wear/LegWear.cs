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
            var legs = new[] { LegL, LegR };
            var PR = new Vector3(0.345f, 0.18f, 0.3f);
            float lr = 0.1f, ly = -0.13f, lh = 0.075f;   // trouser leg: radius, centre, half of the straight part
            // hips ellipsoid (Torso space): point / patch / stitched line
            Vector3 PE(float th, float ph, float gr = 1.02f) => Dt.Ell(new Vector3(0, 0.1f, 0), PR, th, ph, gr);
            Vector3 PN(float th, float ph) => Dt.EllN(PR, th, ph);
            GameObject HipPatch(float th, float ph, Vector3 size, Material m, float gr = 1.02f)
            { var go = B(p, PE(th, ph, gr), size, m); go.transform.localRotation = Quaternion.LookRotation(PN(th, ph), Vector3.up); return go; }
            void HipLine(float th0, float ph0, float th1, float ph1, int n, Material m, float gr = 1.03f, float len = 0.014f)
                => Dt.Stitches(p, t => PE(Mathf.Lerp(th0, th1, t), Mathf.Lerp(ph0, ph1, t), gr), n, len, 0.007f, m);
            // scoop curve: goes down first, then sideways
            void HipCurve(float th0, float ph0, float th1, float ph1, int n, Material m, float gr = 1.03f)
                => Dt.Stitches(p, t => PE(Mathf.Lerp(th0, th1, Mathf.Sqrt(t)), Mathf.Lerp(ph0, ph1, t * t), gr), n, 0.014f, 0.007f, m);
            // trouser leg (Leg space): a = angle (0 = outer side, 90 = front, 180 = inner side)
            float LR(float y) { float d = Mathf.Abs(y - ly) - lh; return d <= 0 ? lr : lr * Mathf.Sqrt(Mathf.Max(0, 1 - d * d / (lh * lh))); }
            Vector3 LD(Transform lg, float a) => new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * (lg == LegL ? -1 : 1), 0, Mathf.Sin(a * Mathf.Deg2Rad));
            Vector3 LP(Transform lg, float a, float y, float gr = 1.03f) => LD(lg, a) * LR(y) * gr + Vector3.up * y;
            Quaternion LQ(Transform lg, float a) => Quaternion.LookRotation(LD(lg, a), Vector3.up);
            GameObject LegPatch(Transform lg, float a, float y, Vector3 size, Material m, float gr = 1.03f)
            { var go = B(lg, LP(lg, a, y, gr), size, m); go.transform.localRotation = LQ(lg, a); return go; }
            void LegLine(Transform lg, float a, float y0, float y1, int n, Material m, float gr = 1.04f)
                => Dt.Stitches(lg, t => LP(lg, a, Mathf.Lerp(y0, y1, t), gr), n, 0.014f, 0.007f, m);
            void LegRing(Transform lg, float y, int n, Material m, float gr = 1.04f)
                => Dt.Stitches(lg, t => LP(lg, t * 360f * (n - 1) / n, y, gr), n, 0.014f, 0.007f, m);
            // box / ball placed in a local frame (o, q) — pockets, plates, buckles
            GameObject Bq(Transform par, Vector3 o, Quaternion q, Vector3 lp, Vector3 size, Material m, Vector3 euler = default)
            { var go = B(par, o + q * lp, size, m); go.transform.localRotation = q * Quaternion.Euler(euler); return go; }
            GameObject Lsq(Transform par, Vector3 o, Quaternion q, Vector3 lp, Vector3 size, Material m)
            { var go = Ls(par, o + q * lp, size, m); go.transform.localRotation = q; return go; }
            float hemY = -0.195f, topY = -0.03f;

            // base: hips + legs, waistband with stitching, belt loops, back seam, out/inseams, hem cuffs
            void Wear(Material m, bool shorts, Material thread, float fat = 1f, Material hemM = null, bool hem = true)
            {
                S(p, new Vector3(0, 0.1f, 0), new Vector3(0.69f, 0.36f, 0.6f), m);
                float hy = shorts ? 0.09f : 0.15f;
                lr = (shorts ? 0.105f : 0.1f) * fat; ly = shorts ? -0.06f : -0.13f; lh = hy / 2;
                hemY = shorts ? -0.112f : -0.195f;
                foreach (var lg in legs) Ca(lg, new Vector3(0, ly, 0), new Vector3(lr * 2, hy, lr * 2), m);
                Cy(p, new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.028f, 0.61f), m);
                Dt.Stitches(p, Dt.Circle(new Vector3(0, 0.128f, 0), 0.354f, 0.309f, 0, 342), 20, 0.016f, 0.007f, thread);
                foreach (var ph in new[] { 15f, 60f, 120f, 165f, 240f, 300f })
                {
                    float a = ph * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    B(p, new Vector3(d.x * 0.357f, 0.15f, d.z * 0.312f), new Vector3(0.024f, 0.064f, 0.012f), m).transform.localRotation = Quaternion.LookRotation(d, Vector3.up);
                }
                HipLine(80, 270, 122, 270, 8, thread);
                foreach (var lg in legs)
                {
                    LegLine(lg, 0, topY, hemY + 0.01f, shorts ? 4 : 8, thread);
                    LegLine(lg, 180, shorts ? -0.07f : -0.06f, hemY + 0.01f, shorts ? 3 : 7, thread);
                    if (!hem) continue;
                    Lc(lg, new Vector3(0, hemY, 0), new Vector3(lr * 2.14f, 0.016f, lr * 2.14f), hemM ?? m);
                    LegRing(lg, hemY + 0.012f, 14, thread, 1.08f);
                }
            }
            // drawstring through two grommets with a knot and tipped cords
            void Drawstring(Material cord, Material tip, Material grom)
            {
                foreach (var s in new[] { -1, 1 })
                {
                    var top = new Vector3(s * 0.03f, 0.145f, 0.309f);
                    Lc(p, top, new Vector3(0.02f, 0.003f, 0.02f), grom, new Vector3(90, 0, 0));
                    var end = top + new Vector3(s * 0.012f, -0.085f, 0.012f);
                    LRod(p, top + Vector3.forward * 0.004f, end, 0.008f, cord);
                    Dt.Orient(Lc(p, end, new Vector3(0.012f, 0.011f, 0.012f), tip), end - top);
                }
                Ls(p, new Vector3(0, 0.138f, 0.318f), new Vector3(0.026f, 0.02f, 0.016f), cord);
            }
            // a pair of slanted side-pocket openings
            void SidePockets(Material thread, int n = 6)
            {
                foreach (var s in new[] { -1, 1 }) HipLine(74, 90 + s * 32, 108, 90 + s * 58, n, thread);
            }

            switch (i)
            {
                case 0:   // summer shorts: drawstring, slant pockets, back patch pockets, navy cuffs, side tape, anchor patch
                {
                    var m = M("4FC3F7", 0.2f); var wt = M("FFFFFF"); var navy = M("1E3A6E", 0.3f); var yel = M("FFD23A", 0.3f);
                    Wear(m, true, wt, 1f, navy);
                    Drawstring(wt, yel, M("D8DEE6", 0.7f, 0.8f));
                    SidePockets(wt, 7);
                    foreach (var s in new[] { -1, 1 })
                    {
                        float ph = 270 + s * 32;
                        HipPatch(100, ph, new Vector3(0.085f, 0.075f, 0.012f), m, 1.025f);
                        HipLine(90, ph - 12, 90, ph + 12, 5, wt, 1.05f);
                        HipLine(111, ph - 12, 111, ph + 12, 5, wt, 1.05f);
                    }
                    foreach (var lg in legs) LegPatch(lg, 0, -0.06f, new Vector3(0.022f, 0.085f, 0.006f), yel, 1.02f);
                    {
                        var o = LP(LegL, 70, -0.065f, 1.04f); var q = LQ(LegL, 70);
                        Lsq(LegL, o, q, new Vector3(0, 0.003f, -0.002f), new Vector3(0.06f, 0.075f, 0.006f), wt);
                        Bq(LegL, o, q, new Vector3(0, 0, 0.002f), new Vector3(0.008f, 0.05f, 0.004f), navy);
                        Bq(LegL, o, q, new Vector3(0, 0.015f, 0.002f), new Vector3(0.028f, 0.007f, 0.004f), navy);
                        Lsq(LegL, o, q, new Vector3(0, 0.03f, 0.002f), new Vector3(0.014f, 0.014f, 0.005f), navy);
                        foreach (var s in new[] { -1, 1 }) Bq(LegL, o, q, new Vector3(s * 0.01f, -0.021f, 0.002f), new Vector3(0.017f, 0.006f, 0.004f), navy, new Vector3(0, 0, -s * 35));
                    }
                    break;
                }
                case 1:   // track pants: double stripes, zip pockets, ribbed ankle cuffs, drawstring, logo
                {
                    var m = M("8C939C", 0.1f); var wt = M("FFFFFF"); var th = M("A9B0B8", 0.1f); var cuff = M("6E757E", 0.1f);
                    var metal = M("D8DEE6", 0.7f, 0.8f); var dark = M("2A2E36", 0.3f);
                    Wear(m, false, th, 1f, null, false);
                    Drawstring(wt, dark, metal);
                    foreach (var ph0 in new[] { 0f, 180f })
                        foreach (var s in new[] { -4f, 4f })
                        {
                            float ph = ph0 + s;
                            Dt.Path(t => PE(Mathf.Lerp(76, 124, t), ph, 1.022f), 4, (pos, dir, k) =>
                                B(p, pos, new Vector3(0.014f, 0.04f, 0.005f), wt).transform.localRotation = Quaternion.LookRotation(PN(Mathf.Lerp(76, 124, k / 3f), ph), dir));
                        }
                    foreach (var lg in legs)
                    {
                        foreach (var s in new[] { -9f, 9f })
                        {
                            LegPatch(lg, s, -0.13f, new Vector3(0.014f, 0.15f, 0.005f), wt, 1.02f);
                            var a0 = LP(lg, s, -0.055f, 1.025f); var a1 = LP(lg, s, -0.015f, 1.025f);
                            B(lg, (a0 + a1) / 2, new Vector3(0.014f, (a1 - a0).magnitude, 0.005f), wt).transform.localRotation = Quaternion.LookRotation(LD(lg, s), a1 - a0);
                        }
                        Lc(lg, new Vector3(0, -0.2f, 0), new Vector3(0.215f, 0.025f, 0.215f), cuff);
                        for (int k = 0; k < 14; k++) LegPatch(lg, k * 360f / 14, -0.2f, new Vector3(0.008f, 0.044f, 0.006f), cuff, 1.085f);
                    }
                    foreach (var s in new[] { -1, 1 })
                        Dt.Zipper(p, PE(106, 90 + s * 56, 1.035f), PE(77, 90 + s * 34, 1.035f), 9, metal, dark, PN(92, 90 + s * 45));
                    {
                        var o = LP(LegL, 62, -0.075f, 1.04f); var q = LQ(LegL, 62);
                        Lsq(LegL, o, q, Vector3.zero, new Vector3(0.045f, 0.045f, 0.006f), wt);
                        for (int k = 0; k < 3; k++) Bq(LegL, o, q, new Vector3(-0.011f + k * 0.011f, 0, 0.003f), new Vector3(0.006f, 0.024f - Mathf.Abs(k - 1) * 0.008f, 0.004f), M("1E88E5"));
                    }
                    break;
                }
                case 2:   // worn jeans: double-stitched scoop pockets, coin pocket, copper rivets, J-fly, back pockets, yoke, ripped knees
                {
                    var m = Mats.Tex(Patterns.Denim(), Color.white, 0.15f);
                    var th = M("F2A33A", 0.3f); var cu = M("C87533", 0.7f, 0.8f);
                    var hole = M("1E2C44", 0.1f); var fray = M("F4F1E8", 0.1f); var fade = M("8FB3D9", 0.1f);
                    Wear(m, false, th);
                    void Rivet(float rt, float rp) => Dt.Orient(Ls(p, PE(rt, rp, 1.035f), new Vector3(0.018f, 0.008f, 0.018f), cu), PN(rt, rp));
                    foreach (var s in new[] { -1, 1 })
                    {
                        HipCurve(74, 90 + s * 24, 106, 90 + s * 56, 7, th);
                        HipCurve(74, 90 + s * 28, 104, 90 + s * 60, 7, th);
                        Rivet(75, 90 + s * 24); Rivet(106, 90 + s * 56);
                        float ph = 270 + s * 32;
                        HipPatch(100, ph, new Vector3(0.09f, 0.08f, 0.012f), m, 1.025f);
                        Dt.Stitches(p, t => PE(97 + 5 * Mathf.Sin(t * Mathf.PI), ph - 13 + 26 * t, 1.05f), 6, 0.014f, 0.007f, th);
                        Dt.Stitches(p, t => PE(102 + 5 * Mathf.Sin(t * Mathf.PI), ph - 13 + 26 * t, 1.05f), 6, 0.014f, 0.007f, th);
                        Rivet(89, ph - 14); Rivet(89, ph + 14);
                    }
                    HipPatch(84, 54, new Vector3(0.05f, 0.045f, 0.008f), m, 1.03f);
                    HipLine(78, 47, 78, 61, 4, th, 1.05f);
                    Rivet(79, 45); Rivet(79, 63);
                    Lc(p, new Vector3(0, 0.15f, 0.315f), new Vector3(0.03f, 0.006f, 0.03f), cu, new Vector3(90, 0, 0));
                    Dt.Stitches(p, t => PE(Mathf.Lerp(78, 118, t), 81 + 9 * Mathf.SmoothStep(0, 1, (t - 0.7f) / 0.3f), 1.03f), 8, 0.014f, 0.007f, th);
                    HipLine(88, 228, 88, 312, 9, th);
                    foreach (var lg in legs)
                    {
                        Lsq(lg, LP(lg, 80, -0.075f, 0.985f), LQ(lg, 80), Vector3.zero, new Vector3(0.09f, 0.075f, 0.03f), fade);
                        LegPatch(lg, 95, -0.13f, new Vector3(0.07f, 0.032f, 0.006f), hole, 1.02f);
                        for (int k = 0; k < 4; k++) LegPatch(lg, 95, -0.141f + k * 0.0073f, new Vector3(0.074f, 0.0035f, 0.004f), fray, 1.045f);
                    }
                    break;
                }
                case 3:   // cargo: big thigh pockets with flaps and snaps, ankle velcro tabs, back flap pockets, knee darts
                {
                    var m = M("A1885A", 0.15f); var pk = M("8C744A", 0.15f); var th = M("6E5A36", 0.2f);
                    var btn = M("3A2E1A", 0.5f); var vel = M("2E2A22", 0.05f);
                    Wear(m, false, th, 1.05f);
                    SidePockets(th, 6);
                    foreach (var lg in legs)
                    {
                        var o = LP(lg, 22, -0.125f, 1.06f); var q = LQ(lg, 22);
                        Bq(lg, o, q, Vector3.zero, new Vector3(0.1f, 0.095f, 0.04f), pk);
                        Bq(lg, o, q, new Vector3(0, -0.005f, 0.021f), new Vector3(0.006f, 0.08f, 0.004f), th);
                        Bq(lg, o, q, new Vector3(0, 0.056f, 0.008f), new Vector3(0.108f, 0.034f, 0.044f), pk);
                        foreach (var s in new[] { -1, 1 }) Lsq(lg, o, q, new Vector3(s * 0.03f, 0.05f, 0.031f), new Vector3(0.014f, 0.014f, 0.008f), btn);
                        for (int k = 0; k < 5; k++)
                        {
                            Bq(lg, o, q, new Vector3(-0.04f + k * 0.02f, -0.04f, 0.0215f), new Vector3(0.013f, 0.005f, 0.004f), th);
                            Bq(lg, o, q, new Vector3(-0.04f + k * 0.02f, 0.043f, 0.0315f), new Vector3(0.013f, 0.005f, 0.004f), th);
                        }
                        for (int k = 0; k < 3; k++) foreach (var s in new[] { -1, 1 })
                            Bq(lg, o, q, new Vector3(s * 0.044f, -0.03f + k * 0.022f, 0.0215f), new Vector3(0.005f, 0.013f, 0.004f), th);
                        LegPatch(lg, 40, -0.183f, new Vector3(0.06f, 0.022f, 0.01f), pk, 1.1f);
                        LegPatch(lg, 40, -0.183f, new Vector3(0.022f, 0.018f, 0.006f), vel, 1.16f);
                        foreach (var s in new[] { -1, 1 }) LegPatch(lg, 40 + s * 13, -0.183f, new Vector3(0.004f, 0.016f, 0.004f), th, 1.16f);
                        foreach (var y in new[] { -0.115f, -0.13f })
                            Dt.Stitches(lg, t => LP(lg, Mathf.Lerp(70, 120, t), y, 1.04f), 4, 0.014f, 0.007f, th);
                    }
                    foreach (var s in new[] { -1, 1 })
                    {
                        float ph = 270 + s * 32;
                        HipPatch(101, ph, new Vector3(0.085f, 0.07f, 0.012f), pk, 1.025f);
                        HipPatch(91, ph, new Vector3(0.09f, 0.026f, 0.02f), pk, 1.035f);
                        Ls(p, PE(92, ph, 1.07f), 0.014f, btn);
                    }
                    break;
                }
                case 4:   // board shorts: lace-up fly with grommets, teal cuffs, velcro side pocket, hibiscus flowers
                {
                    var m = Mats.Tex(Patterns.Floral(), Color.white, 0.2f); var wt = M("FFFFFF"); var teal = M("00BFA5", 0.3f);
                    var pink = M("FF5C8A", 0.3f); var yel = M("FFD23A", 0.4f); var grom = M("D8DEE6", 0.7f, 0.8f);
                    Wear(m, true, wt, 1.1f, teal);
                    HipPatch(91, 90, new Vector3(0.055f, 0.1f, 0.008f), teal, 1.025f);
                    Dt.Laces(p, PE(103, 90, 1.065f), PE(79, 90, 1.065f), new Vector3(0.018f, 0, 0), 3, wt);
                    for (int k = 0; k <= 3; k++) foreach (var s in new[] { -1, 1 })
                    {
                        float et = Mathf.Lerp(103, 79, k / 3f);
                        Dt.Orient(Lc(p, PE(et, 90 - s * 3.2f, 1.05f), new Vector3(0.016f, 0.003f, 0.016f), grom), PN(et, 90));
                    }
                    {
                        var o = LP(LegR, 15, -0.06f, 1.08f); var q = LQ(LegR, 15);
                        Bq(LegR, o, q, Vector3.zero, new Vector3(0.07f, 0.06f, 0.02f), m);
                        Bq(LegR, o, q, new Vector3(0, 0.03f, 0.006f), new Vector3(0.074f, 0.022f, 0.026f), teal);
                        Bq(LegR, o, q, new Vector3(0, 0.03f, 0.02f), new Vector3(0.03f, 0.01f, 0.004f), wt);
                        for (int k = 0; k < 3; k++) Bq(LegR, o, q, new Vector3(-0.022f + k * 0.022f, -0.026f, 0.011f), new Vector3(0.013f, 0.005f, 0.004f), wt);
                    }
                    void Flower(Transform par, Vector3 o, Quaternion q, float sz)
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            var qk = q * Quaternion.Euler(0, 0, k * 72);
                            Lsq(par, o, qk, new Vector3(0, sz * 0.32f, 0), new Vector3(sz * 0.42f, sz * 0.55f, sz * 0.12f), pink);
                        }
                        Lsq(par, o, q, new Vector3(0, 0, sz * 0.05f), new Vector3(sz * 0.22f, sz * 0.22f, sz * 0.15f), yel);
                        for (int k = 0; k < 3; k++) Lsq(par, o, q, new Vector3((k - 1) * sz * 0.08f, sz * 0.1f, sz * 0.12f), Vector3.one * sz * 0.08f, yel);
                    }
                    Flower(LegL, LP(LegL, 60, -0.07f, 1.04f), LQ(LegL, 60), 0.075f);
                    Flower(p, PE(96, 40, 1.03f), Quaternion.LookRotation(PN(96, 40), Vector3.up), 0.07f);
                    break;
                }
                case 5:   // plaid kilt skirt: pleats with folds and hem stitching, waistband, kilt pin with charms, side buckle, fringe
                {
                    var plaid = Mats.Tex(Patterns.Plaid("C62828", "1A1A1A", "FFD23A"), Color.white, 0.2f);
                    var fold = M("1A1A1A", 0.2f); var yel = M("FFD23A", 0.3f); var band = M("8E1B1B", 0.25f);
                    var gold = M("FFC83A", 0.8f, 0.8f); var lea = M("4A3020", 0.3f);
                    S(p, new Vector3(0, 0.1f, 0), new Vector3(0.69f, 0.36f, 0.6f), plaid);
                    Cy(p, new Vector3(0, 0.02f, 0), new Vector3(0.74f, 0.1f, 0.66f), plaid);
                    Cy(p, new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.03f, 0.61f), band);
                    Dt.Stitches(p, Dt.Circle(new Vector3(0, 0.15f, 0), 0.356f, 0.31f, 0, 342), 20, 0.016f, 0.007f, yel);
                    const int N = 26;
                    for (int k = 0; k < N; k++)
                    {
                        float a = (k + 0.5f) / N * Mathf.PI * 2, a2 = k / (float)N * Mathf.PI * 2;
                        var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var d2 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
                        var q = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(-7, 0, 0);
                        var c = new Vector3(d.x * 0.376f, -0.005f, d.z * 0.338f);
                        B(p, c, new Vector3(0.082f, 0.19f, 0.016f), plaid).transform.localRotation = q;
                        B(p, c + q * new Vector3(0, -0.082f, 0.009f), new Vector3(0.03f, 0.006f, 0.004f), yel).transform.localRotation = q;
                        B(p, new Vector3(d2.x * 0.383f, -0.01f, d2.z * 0.345f), new Vector3(0.006f, 0.18f, 0.012f), fold).transform.localRotation = Quaternion.LookRotation(d2, Vector3.up) * Quaternion.Euler(-7, 0, 0);
                    }
                    {
                        float a = 65 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        var o = new Vector3(d.x * 0.392f, -0.04f, d.z * 0.352f); var q = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(-7, 0, 0);
                        LRod(p, o + q * new Vector3(0, -0.035f, 0.012f), o + q * new Vector3(0, 0.035f, 0.012f), 0.007f, gold);
                        Lsq(p, o, q, new Vector3(0, 0.04f, 0.012f), Vector3.one * 0.015f, gold);
                        Bq(p, o, q, new Vector3(0, -0.038f, 0.012f), new Vector3(0.012f, 0.014f, 0.012f), gold);
                        for (int k = 0; k < 3; k++)
                        {
                            var top = o + q * new Vector3(0, 0.02f - k * 0.015f, 0.016f); var bot = top + q * new Vector3(0.012f, -0.012f, 0.004f);
                            LRod(p, top, bot, 0.003f, gold);
                            Ls(p, bot, 0.013f, k == 1 ? band : yel);
                        }
                    }
                    {
                        HipPatch(84, 10, new Vector3(0.06f, 0.022f, 0.008f), lea, 1.03f);
                        HipPatch(84, -10, new Vector3(0.05f, 0.022f, 0.008f), lea, 1.03f);
                        var o = PE(84, 0, 1.05f); var q = Quaternion.LookRotation(PN(84, 0), Vector3.up);
                        foreach (var s in new[] { -1, 1 })
                        {
                            Bq(p, o, q, new Vector3(0, s * 0.014f, 0), new Vector3(0.028f, 0.004f, 0.005f), gold);
                            Bq(p, o, q, new Vector3(s * 0.013f, 0, 0), new Vector3(0.004f, 0.03f, 0.005f), gold);
                        }
                        Bq(p, o, q, new Vector3(0, 0, 0.002f), new Vector3(0.024f, 0.003f, 0.004f), gold);
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        float a = (50 + k * 2f) * Mathf.Deg2Rad;
                        B(p, new Vector3(Mathf.Cos(a) * 0.398f, -0.112f, Mathf.Sin(a) * 0.358f), new Vector3(0.005f, 0.03f, 0.005f), k % 2 == 0 ? yel : band);
                    }
                    break;
                }
                case 6:   // cyberpunk: neon rings and seams, armoured knees with bolts, circuit traces, thigh straps, LED waistband, mag pockets
                {
                    var m = M("15151C", 0.6f); var cy = G("26F0FF", 2.5f); var pk = G("FF3FA4", 2.5f);
                    var plate = M("3A3F4A", 0.7f, 0.6f); var bolt = M("9AA3AE", 0.8f, 0.8f);
                    Wear(m, false, cy);
                    foreach (var lg in legs)
                    {
                        for (int k = 0; k < 2; k++) Ring(lg, new Vector3(0, -0.06f - k * 0.1f, 0), 0.1f, 0.012f, k == 0 ? pk : cy);
                        var o = LP(lg, 92, -0.135f, 1.12f); var q = LQ(lg, 92);
                        Bq(lg, o, q, Vector3.zero, new Vector3(0.085f, 0.065f, 0.022f), plate);
                        Bq(lg, o, q, new Vector3(0, 0.045f, -0.004f), new Vector3(0.07f, 0.025f, 0.018f), plate, new Vector3(-20, 0, 0));
                        Bq(lg, o, q, new Vector3(0, 0, 0.012f), new Vector3(0.05f, 0.008f, 0.004f), pk);
                        foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 })
                            Lsq(lg, o, q, new Vector3(sx * 0.033f, sy * 0.024f, 0.012f), new Vector3(0.01f, 0.01f, 0.006f), bolt);
                        var o2 = LP(lg, 35, -0.1f, 1.06f); var q2 = LQ(lg, 35);
                        for (int t = 0; t < 3; t++)
                        {
                            float x = -0.025f + t * 0.025f;
                            Bq(lg, o2, q2, new Vector3(x, -0.01f, 0), new Vector3(0.005f, 0.05f, 0.004f), cy);
                            Bq(lg, o2, q2, new Vector3(x + (t - 1) * 0.006f, 0.025f, 0), new Vector3(0.005f, 0.025f, 0.004f), cy, new Vector3(0, 0, (t - 1) * -35));
                            Lsq(lg, o2, q2, new Vector3(x, -0.037f, 0), Vector3.one * 0.011f, pk);
                        }
                        for (int k = 0; k < 6; k++) LegPatch(lg, k * 36, -0.03f, new Vector3(0.04f, 0.016f, 0.008f), plate, 1.05f);
                        LegPatch(lg, 60, -0.03f, new Vector3(0.022f, 0.024f, 0.012f), bolt, 1.09f);
                        Ls(lg, LP(lg, 60, -0.03f, 1.2f), 0.009f, pk);
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        float a = (55 + k * 10) * Mathf.Deg2Rad;
                        Ls(p, new Vector3(Mathf.Cos(a) * 0.358f, 0.15f, Mathf.Sin(a) * 0.313f), 0.012f, k % 2 == 0 ? cy : pk);
                    }
                    foreach (var s in new[] { -1, 1 })
                    {
                        float ph = 90 + s * 62;
                        HipPatch(98, ph, new Vector3(0.06f, 0.07f, 0.025f), plate, 1.04f);
                        HipPatch(92, ph, new Vector3(0.03f, 0.008f, 0.004f), cy, 1.075f);
                        foreach (var d in new[] { -5, 5 }) Ls(p, PE(104, ph + d, 1.08f), 0.01f, bolt);
                    }
                    break;
                }
                default:  // classic trousers: pinstripes, pressed creases, slant and welt pockets, fly, waist tab
                {
                    var m = M("3C4656", 0.2f); var th = M("56627A", 0.2f); var pin = M("8C97AB", 0.2f);
                    var crease = M("4D5970", 0.25f); var btn = M("1E232B", 0.6f);
                    Wear(m, false, th);
                    foreach (var lg in legs)
                    {
                        LegPatch(lg, 90, -0.13f, new Vector3(0.006f, 0.15f, 0.004f), crease, 1.02f);
                        var a0 = LP(lg, 90, -0.055f, 1.02f); var a1 = LP(lg, 90, -0.015f, 1.02f);
                        B(lg, (a0 + a1) / 2, new Vector3(0.006f, (a1 - a0).magnitude, 0.004f), crease).transform.localRotation = Quaternion.LookRotation(LD(lg, 90), a1 - a0);
                        for (int k = 0; k < 10; k++) LegPatch(lg, k * 36 + 9, -0.13f, new Vector3(0.003f, 0.15f, 0.003f), pin, 1.015f);
                    }
                    for (int ph = 30; ph <= 150; ph += 15) if (ph != 90) HipLine(76, ph, 120, ph, 3, pin, 1.022f, 0.05f);
                    HipLine(78, 82, 112, 84, 6, th);
                    SidePockets(th, 6);
                    foreach (var s in new[] { -1, 1 })
                    {
                        float ph = 270 + s * 32;
                        HipPatch(96, ph, new Vector3(0.08f, 0.012f, 0.01f), crease, 1.025f);
                        Ls(p, PE(100, ph, 1.04f), 0.014f, btn);
                        foreach (var d in new[] { -12, 12 }) HipPatch(96, ph + d, new Vector3(0.004f, 0.016f, 0.006f), th, 1.04f);
                    }
                    B(p, new Vector3(-0.03f, 0.15f, 0.318f), new Vector3(0.07f, 0.04f, 0.008f), m);
                    Lc(p, new Vector3(0, 0.15f, 0.324f), new Vector3(0.022f, 0.004f, 0.022f), btn, new Vector3(90, 0, 0));
                    B(p, new Vector3(-0.06f, 0.15f, 0.324f), new Vector3(0.012f, 0.008f, 0.004f), M("C9D1DB", 0.8f, 0.8f));
                    break;
                }
            }
        }

        void Shoes(int i, int set)
        {
            int kind = set >= 0 ? 100 + set : i;
            Transform f = null;
            float sd = 1;                                        // +1 right foot, −1 left (outer side = +sd·X)
            var UC = Vector3.zero; var UR = new Vector3(0.105f, 0.065f, 0.14f);   // upper ellipsoid (Foot space, toes +Z)
            Vector3 UP(float th, float ph, float gr = 1.02f) => Dt.Ell(UC, UR, th, ph, gr);
            Vector3 UN(float th, float ph) => Dt.EllN(UR, th, ph);
            // angle on the outer / inner side, off > 0 goes towards the toe
            float Out(float off) => sd > 0 ? off : 180 - off;
            float In(float off) => sd > 0 ? 180 - off : off;
            GameObject UPatch(float th, float ph, Vector3 size, Material m, float gr = 1.02f)
            {
                var n = UN(th, ph);
                var go = B(f, UP(th, ph, gr), size, m);
                go.transform.localRotation = Quaternion.LookRotation(n, Mathf.Abs(n.y) > 0.8f ? Vector3.forward : Vector3.up);
                return go;
            }
            // strip of flat boxes lying on the upper along (th, ph)(t): stitches, stripes, panel seams
            void Strip(System.Func<float, Vector2> tp, int n, float w, float len, Material m, float gr = 1.025f)
            {
                Dt.Path(t => UP(tp(t).x, tp(t).y, gr), n, (pos, dir, k) =>
                {
                    var a = tp(n == 1 ? 0.5f : k / (float)(n - 1));
                    B(f, pos, new Vector3(w, len, 0.006f), m).transform.localRotation = Quaternion.LookRotation(UN(a.x, a.y), dir);
                });
            }
            void ULine(float th0, float ph0, float th1, float ph1, int n, Material m, float gr = 1.025f)
                => Strip(t => new Vector2(Mathf.Lerp(th0, th1, t), Mathf.Lerp(ph0, ph1, t)), n, 0.007f, 0.014f, m, gr);
            void SoleRing(float y, float rx, float rz, int n, Material m)
                => Dt.Stitches(f, Dt.Circle(new Vector3(0, y, 0), rx, rz, 0, 360f * (n - 1) / n), n, 0.016f, 0.007f, m);
            // blocks around an ellipse at height y facing outwards: tread lugs, sidewall ribs, LEDs
            void Ring2(float y, float rx, float rz, int n, Vector3 size, Material m, float a0 = 0, float a1 = 360, bool ball = false)
            {
                for (int k = 0; k < n; k++)
                {
                    float a = Mathf.Lerp(a0, a1, (k + 0.5f) / n) * Mathf.Deg2Rad;
                    var pos = new Vector3(Mathf.Cos(a) * rx, y, Mathf.Sin(a) * rz);
                    var go = ball ? Ls(f, pos, size, m) : B(f, pos, size, m);
                    go.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a) / rx, 0, Mathf.Sin(a) / rz), Vector3.up);
                }
            }
            // laces with own low-poly eyelets (or speed hooks on the upper half)
            void Lace(Vector3 a, Vector3 b, float half, int crosses, Material lace, Material eye, bool hooks = false)
            {
                Dt.Laces(f, a, b, new Vector3(half, 0, 0), crosses, lace);
                var n = Vector3.Cross(Vector3.right, b - a).normalized;
                for (int k = 0; k <= crosses; k++)
                    foreach (var s in new[] { -1, 1 })
                    {
                        var pos = Vector3.Lerp(a, b, k / (float)crosses) + Vector3.right * s * half - n * 0.003f;
                        if (hooks && k > crosses / 2) Ls(f, pos + n * 0.004f, 0.013f, eye);
                        else Dt.Orient(Lc(f, pos, new Vector3(0.017f, 0.003f, 0.017f), eye), n);
                    }
            }
            GameObject Bq(Vector3 o, Quaternion q, Vector3 lp, Vector3 size, Material m, Vector3 euler = default)
            { var go = B(f, o + q * lp, size, m); go.transform.localRotation = q * Quaternion.Euler(euler); return go; }
            GameObject Lsq(Vector3 o, Quaternion q, Vector3 lp, Vector3 size, Material m)
            { var go = Ls(f, o + q * lp, size, m); go.transform.localRotation = q; return go; }
            // small buckle frame (4 bars + prong) facing q's +Z
            void Buckle(Vector3 o, Quaternion q, float w, float h, Material m)
            {
                foreach (var s in new[] { -1, 1 })
                {
                    Bq(o, q, new Vector3(0, s * h / 2, 0), new Vector3(w, 0.004f, 0.005f), m);
                    Bq(o, q, new Vector3(s * w / 2, 0, 0), new Vector3(0.004f, h, 0.005f), m);
                }
                Bq(o, q, new Vector3(0, 0, 0.003f), new Vector3(w * 0.9f, 0.003f, 0.004f), m);
            }

            if (kind < 0 || kind > 7 && kind != 102)
            {
                foreach (var ft in new[] { FootL, FootR }) S(ft, Vector3.zero, new Vector3(0.2f, 0.12f, 0.26f), Skin);
                return;
            }
            // materials once per item
            var wt = M("FAFAFA", 0.3f); var red = M("E53935"); var blue = M("1E88E5"); var dk = M("2A2E36", 0.3f);
            var steel = M("C9D1DB", 0.8f, 0.8f); var lace = M("FFFFFF");
            foreach (var ft in new[] { FootL, FootR })
            {
                f = ft; sd = ft == FootR ? 1 : -1;
                switch (kind)
                {
                    case 0:   // running shoes: two-layer sole with lugs, swoosh stripes, laces, tongue, heel counter, mesh toe
                    {
                        S(f, Vector3.zero, new Vector3(0.21f, 0.13f, 0.28f), wt);
                        Cy(f, new Vector3(0, -0.035f, 0), new Vector3(0.205f, 0.014f, 0.275f), wt);
                        Cy(f, new Vector3(0, -0.056f, 0), new Vector3(0.2f, 0.01f, 0.27f), red);
                        Ring2(-0.058f, 0.1f, 0.134f, 14, new Vector3(0.022f, 0.016f, 0.014f), red);
                        SoleRing(-0.022f, 0.104f, 0.139f, 16, M("9AA3AE", 0.3f));
                        foreach (var side in new[] { true, false })
                            Strip(t => new Vector2(102 - 30 * t * t, side ? Out(-45 + 95 * t) : In(-45 + 95 * t)), 6, 0.016f, 0.036f, blue);
                        Lace(new Vector3(0, 0.062f, 0.075f), new Vector3(0, 0.072f, -0.02f), 0.026f, 4, lace, steel);
                        B(f, new Vector3(0, 0.078f, -0.04f), new Vector3(0.05f, 0.012f, 0.06f), wt, new Vector3(-25, 0, 0));
                        B(f, new Vector3(0, 0.094f, -0.062f), new Vector3(0.026f, 0.014f, 0.006f), red, new Vector3(-60, 0, 0));
                        Ls(f, new Vector3(0, 0.0f, -0.118f), new Vector3(0.19f, 0.1f, 0.07f), red);
                        B(f, new Vector3(0, 0.06f, -0.14f), new Vector3(0.012f, 0.036f, 0.006f), blue, new Vector3(-20, 0, 0));
                        B(f, new Vector3(0, 0.022f, -0.156f), new Vector3(0.04f, 0.008f, 0.005f), steel);
                        for (int r = 0; r < 2; r++) for (int c = 0; c < 4; c++)
                            Dt.Orient(Ls(f, UP(42 + r * 14, 72 + c * 12, 1.0f), new Vector3(0.012f, 0.005f, 0.012f), M("8A939E")), UN(42 + r * 14, 72 + c * 12));
                        break;
                    }
                    case 1:   // canvas sneakers: rubber toe cap with bumper lines, foxing stripes, ribbed sidewall, star patch
                    {
                        var pink = M("FF3FA4", 0.3f); var w = M("FFFFFF");
                        S(f, Vector3.zero, new Vector3(0.2f, 0.13f, 0.27f), pink);
                        S(f, new Vector3(0, -0.01f, 0.08f), new Vector3(0.18f, 0.1f, 0.12f), w);
                        Cy(f, new Vector3(0, -0.047f, 0), new Vector3(0.21f, 0.02f, 0.28f), w);
                        Cy(f, new Vector3(0, -0.035f, 0), new Vector3(0.213f, 0.004f, 0.283f), blue);
                        Cy(f, new Vector3(0, -0.053f, 0), new Vector3(0.213f, 0.003f, 0.283f), pink);
                        Ring2(-0.047f, 0.106f, 0.141f, 18, new Vector3(0.004f, 0.016f, 0.004f), M("E0E3E8"));
                        for (int k = 0; k < 4; k++) B(f, new Vector3(0, -0.03f + k * 0.009f, 0.138f - k * 0.004f), new Vector3(0.07f - k * 0.008f, 0.003f, 0.004f), M("D5D9DE"), new Vector3(-k * 12, 0, 0));
                        ULine(60, 15, 60, 165, 10, dk);
                        Lace(new Vector3(0, 0.062f, 0.065f), new Vector3(0, 0.072f, -0.03f), 0.028f, 5, lace, steel);
                        foreach (var dy in new[] { -0.012f, 0.012f }) Dt.Orient(Lc(f, UP(75, In(-30), 1.0f) + Vector3.up * dy, new Vector3(0.012f, 0.003f, 0.012f), steel), UN(75, In(-30)));
                        {
                            var o = UP(80, Out(-55), 1.02f); var q = Quaternion.LookRotation(UN(80, Out(-55)), Vector3.up);
                            Lsq(o, q, Vector3.zero, new Vector3(0.045f, 0.045f, 0.006f), w);
                            Msh(Meshes.Star(), f, o + q * new Vector3(0, 0, 0.004f), new Vector3(0.032f, 0.032f, 0.006f), pink).transform.localRotation = q;
                        }
                        B(f, new Vector3(0, 0.035f, -0.136f), new Vector3(0.04f, 0.03f, 0.006f), w);
                        B(f, new Vector3(0, 0.035f, -0.14f), new Vector3(0.03f, 0.008f, 0.004f), red);
                        B(f, new Vector3(0, 0.08f, -0.04f), new Vector3(0.05f, 0.012f, 0.06f), pink, new Vector3(-25, 0, 0));
                        B(f, new Vector3(0, 0.088f, -0.044f), new Vector3(0.03f, 0.004f, 0.02f), w, new Vector3(-25, 0, 0));
                        ULine(30, 270, 85, 270, 5, dk);
                        break;
                    }
                    case 2:   // heavy boots: lug sole, yellow welt stitching, speed hooks up the shaft, padded collar, pull tab
                    {
                        var br = M("4A3020", 0.4f); var blk = M("151515"); var yel = M("FFD23A", 0.3f);
                        UR = new Vector3(0.115f, 0.075f, 0.15f);
                        Cy(f, new Vector3(0, 0.07f, -0.03f), new Vector3(0.19f, 0.07f, 0.19f), br);
                        S(f, Vector3.zero, new Vector3(0.23f, 0.15f, 0.3f), br);
                        Cy(f, new Vector3(0, -0.058f, 0), new Vector3(0.235f, 0.025f, 0.305f), blk);
                        B(f, new Vector3(0, -0.078f, -0.1f), new Vector3(0.2f, 0.03f, 0.09f), blk);
                        Cy(f, new Vector3(0, -0.036f, 0), new Vector3(0.24f, 0.008f, 0.31f), M("2A1C12", 0.3f));
                        SoleRing(-0.03f, 0.12f, 0.155f, 22, yel);
                        Ring2(-0.07f, 0.112f, 0.148f, 14, new Vector3(0.03f, 0.022f, 0.02f), blk);
                        Lace(new Vector3(0, 0.066f, 0.088f), new Vector3(0, 0.13f, 0.074f), 0.03f, 5, M("C8A060", 0.2f), steel, true);
                        B(f, new Vector3(0, 0.15f, -0.126f), new Vector3(0.03f, 0.045f, 0.008f), yel);
                        foreach (var s in new[] { -1, 1 }) B(f, new Vector3(s * 0.01f, 0.15f, -0.131f), new Vector3(0.003f, 0.035f, 0.003f), blk);
                        Ring(f, new Vector3(0, 0.14f, -0.03f), 0.095f, 0.014f, M("6B4A32", 0.3f));
                        ULine(55, 10, 55, 170, 10, yel);
                        Dt.Stitches(f, Dt.Line(new Vector3(0, 0.02f, -0.127f), new Vector3(0, 0.12f, -0.127f)), 5, 0.014f, 0.007f, yel);
                        break;
                    }
                    case 3:   // sandals over tube socks: ribbed sock with stripes, footbed, toe strap, ankle strap with buckle, rivets
                    {
                        var sock = M("FFFFFF", 0.05f); var rib = M("E6E9ED", 0.05f); var lea = M("8D5524", 0.3f); var lea2 = M("6B3F1A", 0.3f);
                        Cy(f, new Vector3(0, 0.07f, -0.03f), new Vector3(0.17f, 0.07f, 0.17f), sock);
                        S(f, new Vector3(0, -0.01f, 0), new Vector3(0.17f, 0.1f, 0.24f), sock);
                        Lc(f, new Vector3(0, 0.11f, -0.03f), new Vector3(0.176f, 0.005f, 0.176f), red);
                        Lc(f, new Vector3(0, 0.127f, -0.03f), new Vector3(0.176f, 0.005f, 0.176f), blue);
                        for (int k = 0; k < 14; k++)
                        {
                            float a = k * Mathf.PI * 2 / 14; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            B(f, new Vector3(0, 0.075f, -0.03f) + d * 0.086f, new Vector3(0.006f, 0.1f, 0.004f), rib).transform.localRotation = Quaternion.LookRotation(d, Vector3.up);
                        }
                        Cy(f, new Vector3(0, -0.062f, 0.01f), new Vector3(0.2f, 0.0125f, 0.29f), lea);
                        Cy(f, new Vector3(0, -0.048f, 0.01f), new Vector3(0.19f, 0.003f, 0.28f), M("D7B98E", 0.2f));
                        Ring2(-0.068f, 0.1f, 0.145f, 14, new Vector3(0.02f, 0.012f, 0.014f), lea2);
                        for (int k = 0; k < 5; k++)
                        {
                            float a = Mathf.Lerp(-10, 190, k / 4f) * Mathf.Deg2Rad;
                            var pos = new Vector3(Mathf.Cos(a) * 0.078f, -0.012f + Mathf.Sin(a) * 0.046f, 0.06f);
                            B(f, pos, new Vector3(0.03f, 0.009f, 0.04f), lea).transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90);
                        }
                        for (int k = 0; k < 6; k++) B(f, new Vector3(-0.05f + k * 0.02f, 0.038f, 0.06f), new Vector3(0.012f, 0.004f, 0.004f), M("F2E3C6"));
                        for (int k = 0; k < 6; k++)
                        {
                            float a = Mathf.Lerp(-20, 200, k / 5f) * Mathf.Deg2Rad;
                            var d = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a));
                            B(f, new Vector3(0, 0.03f, -0.03f) + d * 0.09f, new Vector3(0.05f, 0.022f, 0.008f), lea).transform.localRotation = Quaternion.LookRotation(d, Vector3.up);
                        }
                        foreach (var s in new[] { -1, 1 })
                        {
                            B(f, new Vector3(s * 0.088f, -0.015f, -0.03f), new Vector3(0.008f, 0.07f, 0.022f), lea);
                            Ls(f, new Vector3(s * 0.094f, -0.04f, -0.03f), new Vector3(0.006f, 0.01f, 0.01f), steel);
                            Ls(f, new Vector3(s * 0.082f, -0.035f, 0.06f), new Vector3(0.006f, 0.01f, 0.01f), steel);
                        }
                        Buckle(new Vector3(sd * 0.095f, 0.03f, -0.03f), Quaternion.LookRotation(Vector3.right * sd, Vector3.up), 0.024f, 0.028f, steel);
                        break;
                    }
                    case 4:   // space boots: accordion shaft, armoured toe, LED sole, side clamps, heel thruster with fins, badge
                    {
                        var sil = M("C9D1DB", 0.85f, 0.8f); var gl = G("35A8FF", 2f); var gry = M("5A6270", 0.5f, 0.6f); var cy = G("26F0FF", 2.5f);
                        UR = new Vector3(0.125f, 0.085f, 0.155f);
                        S(f, Vector3.zero, new Vector3(0.25f, 0.17f, 0.31f), sil);
                        Cy(f, new Vector3(0, 0.1f, -0.03f), new Vector3(0.19f, 0.06f, 0.19f), sil);
                        Ring(f, new Vector3(0, 0.05f, -0.03f), 0.1f, 0.02f, red);
                        for (int k = 0; k < 4; k++) Lc(f, new Vector3(0, 0.085f + k * 0.022f, -0.03f), new Vector3(0.2f, 0.006f, 0.2f), gry);
                        Cy(f, new Vector3(0, -0.072f, 0), new Vector3(0.22f, 0.0125f, 0.28f), gl);
                        Ring2(-0.084f, 0.1f, 0.13f, 12, new Vector3(0.026f, 0.012f, 0.018f), gry);
                        Ring2(-0.072f, 0.112f, 0.142f, 10, Vector3.one * 0.012f, cy, 0, 360, true);
                        var po = new Vector3(0, 0.066f, 0.088f); var pq = Quaternion.Euler(30, 0, 0);
                        Bq(po, pq, Vector3.zero, new Vector3(0.12f, 0.012f, 0.08f), wt);
                        foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                            Lsq(po, pq, new Vector3(sx * 0.05f, 0.007f, sz * 0.03f), Vector3.one * 0.01f, steel);
                        foreach (var off in new[] { -20f, 25f })
                        {
                            var o = UP(95, Out(off), 1.02f); var q = Quaternion.LookRotation(UN(95, Out(off)), Vector3.up);
                            Bq(o, q, Vector3.zero, new Vector3(0.03f, 0.03f, 0.01f), gry);
                            Bq(o, q, new Vector3(0, 0.006f, 0.009f), new Vector3(0.008f, 0.034f, 0.006f), red, new Vector3(0, 0, 15));
                            Lsq(o, q, new Vector3(0, -0.01f, 0.01f), Vector3.one * 0.01f, steel);
                        }
                        Spike(f, new Vector3(0, 0.01f, -0.205f), new Vector3(0, 0.01f, -0.15f), 0.07f, gry);
                        Ls(f, new Vector3(0, 0.01f, -0.2f), new Vector3(0.05f, 0.05f, 0.01f), cy);
                        foreach (var s in new[] { -1, 1 }) B(f, new Vector3(s * 0.035f, 0.04f, -0.16f), new Vector3(0.006f, 0.04f, 0.04f), red, new Vector3(0, 0, s * 20));
                        foreach (var off in new[] { -60f, 50f }) ULine(60, Out(off), 115, Out(off), 6, gry);
                        {
                            var o = UP(85, Out(-5), 1.02f); var q = Quaternion.LookRotation(UN(85, Out(-5)), Vector3.up);
                            Lsq(o, q, Vector3.zero, new Vector3(0.04f, 0.04f, 0.006f), wt);
                            Msh(Meshes.Star(), f, o + q * new Vector3(0, 0, 0.004f), new Vector3(0.03f, 0.03f, 0.006f), red).transform.localRotation = q;
                        }
                        break;
                    }
                    case 5:   // LED sneakers: colour-cycling sole + rainbow LED dots, heel window, light bars, glowing tongue button
                    {
                        var lgr = M("E3E8EE", 0.3f); var cy = G("26F0FF", 2.5f);
                        S(f, Vector3.zero, new Vector3(0.21f, 0.13f, 0.28f), wt);
                        var sole = Cy(f, new Vector3(0, -0.05f, 0), new Vector3(0.205f, 0.0175f, 0.275f), G("FF3FA4", 2f));
                        var hu = Add<Hue>(sole); hu.glow = 2.2f; hu.speed = 0.6f; hu.offset = f == FootL ? 0 : 0.5f;
                        string[] rb = { "FF3B3B", "FF9A1E", "FFE14D", "3DFF6E", "26C6FF", "B57CFF" };
                        for (int k = 0; k < 16; k++)
                        {
                            float a = (k + 0.5f) * Mathf.PI * 2 / 16;
                            Ls(f, new Vector3(Mathf.Cos(a) * 0.104f, -0.05f, Mathf.Sin(a) * 0.139f), 0.011f, G(rb[k % 6], 2.4f));
                        }
                        B(f, new Vector3(0, -0.045f, -0.135f), new Vector3(0.05f, 0.022f, 0.012f), Glass("BFEFFF", 0.5f));
                        B(f, new Vector3(0, -0.045f, -0.13f), new Vector3(0.03f, 0.012f, 0.008f), dk);
                        Lace(new Vector3(0, 0.062f, 0.075f), new Vector3(0, 0.072f, -0.02f), 0.026f, 4, M("DDE3EA"), steel);
                        B(f, new Vector3(0, 0.078f, -0.04f), new Vector3(0.05f, 0.012f, 0.06f), wt, new Vector3(-25, 0, 0));
                        Ls(f, new Vector3(0, 0.087f, -0.05f), new Vector3(0.016f, 0.008f, 0.016f), cy);
                        B(f, new Vector3(0, 0.094f, -0.062f), new Vector3(0.026f, 0.014f, 0.006f), lgr, new Vector3(-60, 0, 0));
                        foreach (var side in new[] { true, false })
                        {
                            float Sx(float off) => side ? Out(off) : In(off);
                            UPatch(82, Sx(-35), new Vector3(0.07f, 0.04f, 0.008f), lgr);
                            Strip(t => new Vector2(66, Sx(-62 + 54 * t)), 6, 0.007f, 0.014f, dk);
                            Strip(t => new Vector2(96, Sx(-20 + 60 * t)), 3, 0.008f, 0.03f, cy);
                        }
                        Ls(f, new Vector3(0, 0, -0.118f), new Vector3(0.19f, 0.1f, 0.07f), lgr);
                        B(f, new Vector3(0, 0.02f, -0.156f), new Vector3(0.016f, 0.006f, 0.004f), dk);
                        B(f, new Vector3(0, 0.06f, -0.14f), new Vector3(0.012f, 0.036f, 0.006f), lgr, new Vector3(-20, 0, 0));
                        break;
                    }
                    case 6:   // cyber valenki: felt with fold-over cuff and fuzz, rubber galosh, neon rings, circuits, LED panel, cable
                    {
                        var felt = M("8A8F98", 0.05f); var felt2 = M("767B84", 0.05f); var rub = M("1C1F24", 0.85f); var cy = G("26F0FF", 2.5f);
                        UR = new Vector3(0.11f, 0.07f, 0.145f);
                        Cy(f, new Vector3(0, 0.09f, -0.03f), new Vector3(0.2f, 0.1f, 0.2f), felt);
                        S(f, Vector3.zero, new Vector3(0.22f, 0.14f, 0.29f), felt);
                        S(f, new Vector3(0, -0.03f, 0), new Vector3(0.235f, 0.09f, 0.305f), rub);
                        Ring(f, new Vector3(0, 0.14f, -0.03f), 0.1f, 0.012f, cy);
                        Ring(f, new Vector3(0, 0.03f, -0.03f), 0.11f, 0.012f, cy);
                        Cy(f, new Vector3(0, 0.18f, -0.03f), new Vector3(0.218f, 0.025f, 0.218f), felt2);
                        Dt.Stitches(f, Dt.Circle(new Vector3(0, 0.158f, -0.03f), 0.111f, 0.111f, 0, 360f * 13 / 14), 14, 0.016f, 0.007f, felt);
                        Ring2(-0.03f, 0.119f, 0.154f, 10, new Vector3(0.006f, 0.03f, 0.006f), rub, 45, 135);
                        var po = new Vector3(0, 0.058f, 0.085f); var pq = Quaternion.Euler(30, 0, 0);
                        Bq(po, pq, Vector3.zero, new Vector3(0.1f, 0.01f, 0.06f), steel);
                        foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                            Lsq(po, pq, new Vector3(sx * 0.04f, 0.006f, sz * 0.022f), Vector3.one * 0.009f, dk);
                        for (int k = 0; k < 4; k++)
                        {
                            float a = (40 + k * 33) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            float h = k % 2 == 0 ? 0.07f : 0.05f;
                            B(f, new Vector3(0, 0.055f + h / 2, -0.03f) + d * 0.102f, new Vector3(0.005f, h, 0.004f), cy).transform.localRotation = Quaternion.LookRotation(d, Vector3.up);
                            Ls(f, new Vector3(0, 0.055f, -0.03f) + d * 0.104f, 0.011f, cy);
                        }
                        B(f, new Vector3(sd * 0.1f, 0.09f, -0.06f), new Vector3(0.008f, 0.042f, 0.03f), dk);
                        string[] led = { "3DFF6E", "FFE14D", "FF3B3B" };
                        for (int k = 0; k < 3; k++) Ls(f, new Vector3(sd * 0.105f, 0.077f + k * 0.013f, -0.06f), 0.009f, G(led[k], 2.4f));
                        var c0 = new Vector3(0, 0.0f, -0.15f); var c1 = new Vector3(0, 0.06f, -0.14f); var c2 = new Vector3(0, 0.12f, -0.138f); var c3 = new Vector3(0, 0.165f, -0.138f);
                        LRod(f, c0, c1, 0.012f, dk); LRod(f, c1, c2, 0.012f, dk); LRod(f, c2, c3, 0.012f, dk);
                        Ls(f, c1, 0.018f, steel); Ls(f, c2, 0.018f, steel);
                        Dt.Fur(f, Vector3.zero, new Vector3(0.112f, 0.071f, 0.148f), 14, 0.014f, felt2, f == FootR ? 7 : 8, 70);
                        break;
                    }
                    case 7:   // paw slippers: plush with fur tufts, four toes with claws and pink beans, sole pads, fluffy collar
                    {
                        var fur = M("8D6E63", 0.05f); var fur2 = M("A1887F", 0.05f); var pad = M("FF8FB1", 0.3f);
                        var claw = M("FFF3D6", 0.5f); var cream = M("F5E6D3", 0.05f);
                        UR = new Vector3(0.125f, 0.075f, 0.155f);
                        S(f, Vector3.zero, new Vector3(0.25f, 0.15f, 0.31f), fur);
                        Cy(f, new Vector3(0, -0.066f, 0), new Vector3(0.23f, 0.01f, 0.29f), M("5D4037", 0.1f));
                        for (int k = 0; k < 4; k++)
                        {
                            float x = -0.054f + k * 0.036f;
                            var pos = new Vector3(x, 0.032f - Mathf.Abs(x) * 0.2f, 0.115f - Mathf.Abs(x) * 0.3f);
                            Ls(f, pos, new Vector3(0.05f, 0.045f, 0.055f), fur);
                            Spike(f, pos + new Vector3(0, -0.006f, 0.022f), pos + new Vector3(0, -0.016f, 0.05f), 0.016f, claw);
                            Ls(f, pos + new Vector3(0, 0.02f, 0.004f), new Vector3(0.022f, 0.006f, 0.022f), pad);
                            Ls(f, new Vector3(x * 1.1f, -0.072f, 0.09f), new Vector3(0.03f, 0.006f, 0.03f), pad);
                        }
                        Ls(f, new Vector3(0, 0.072f, 0.03f), new Vector3(0.06f, 0.008f, 0.05f), pad);
                        Ls(f, new Vector3(0, -0.072f, -0.02f), new Vector3(0.09f, 0.006f, 0.08f), pad);
                        Dt.Path(Dt.Circle(new Vector3(0, 0.064f, -0.05f), 0.075f, 0.06f, 0, 330), 12, (pos, dir, k) => Ls(f, pos, 0.03f, cream));
                        Dt.Fur(f, Vector3.zero, new Vector3(0.127f, 0.077f, 0.157f), 12, 0.026f, fur2, f == FootR ? 3 : 4, 80);
                        SoleRing(-0.045f, 0.103f, 0.127f, 14, M("4E342E", 0.1f));
                        break;
                    }
                    case 102:   // moon boots: puffy quilted upper and shaft rolls, deep lug sole, blue strap clamps, mission patch
                    {
                        var mw = M("F2F4F7", 0.4f); var mg = M("8A939E", 0.6f); var mb = M("1E63D9"); var seam = M("C5CCD6", 0.3f);
                        UR = new Vector3(0.14f, 0.1f, 0.165f); UC = new Vector3(0, 0.02f, 0);
                        S(f, new Vector3(0, 0.02f, 0), new Vector3(0.28f, 0.2f, 0.33f), mw);
                        Cy(f, new Vector3(0, -0.075f, 0), new Vector3(0.28f, 0.025f, 0.34f), mg);
                        Ring(f, new Vector3(0, 0.08f, -0.02f), 0.11f, 0.025f, mb);
                        Cy(f, new Vector3(0, 0.15f, -0.02f), new Vector3(0.22f, 0.07f, 0.22f), mw);
                        for (int k = 0; k < 3; k++) Ls(f, new Vector3(0, 0.12f + k * 0.04f, -0.02f), new Vector3(0.245f, 0.035f, 0.245f), mw);
                        for (int k = 0; k < 5; k++) ULine(25, 30 + k * 30, 85, 30 + k * 30, 4, seam);
                        Ring2(-0.09f, 0.125f, 0.155f, 14, new Vector3(0.03f, 0.02f, 0.025f), mg);
                        foreach (var sx in new[] { -1, 1 }) for (int k = 0; k < 3; k++)
                            B(f, new Vector3(sx * 0.04f, -0.1f, -0.08f + k * 0.08f), new Vector3(0.05f, 0.012f, 0.05f), dk);
                        foreach (var z in new[] { 0.1f, 0.13f })
                        {
                            float kz = Mathf.Sqrt(1 - z * z / (0.165f * 0.165f)), rx = 0.14f * kz * 1.04f, ry = 0.1f * kz * 1.04f;
                            for (int k = 0; k < 4; k++)
                            {
                                float a = Mathf.Lerp(25, 155, k / 3f);
                                float r = a * Mathf.Deg2Rad;
                                B(f, new Vector3(Mathf.Cos(r) * rx, 0.02f + Mathf.Sin(r) * ry, z), new Vector3(0.04f, 0.008f, 0.022f), mb, new Vector3(0, 0, a - 90));
                            }
                            float ac = sd > 0 ? 15 : 165, rc = ac * Mathf.Deg2Rad;
                            var cp = new Vector3(Mathf.Cos(rc) * rx * 1.04f, 0.02f + Mathf.Sin(rc) * ry * 1.04f, z);
                            B(f, cp, new Vector3(0.022f, 0.03f, 0.028f), mb, new Vector3(0, 0, ac - 90));
                            B(f, cp + new Vector3(Mathf.Cos(rc), Mathf.Sin(rc), 0) * 0.014f, new Vector3(0.01f, 0.028f, 0.008f), steel, new Vector3(0, 0, ac - 90));
                        }
                        {
                            var o = UP(80, Out(-20), 1.02f); var q = Quaternion.LookRotation(UN(80, Out(-20)), Vector3.up);
                            Lsq(o, q, Vector3.zero, new Vector3(0.05f, 0.05f, 0.006f), mb);
                            Msh(Meshes.Star(), f, o + q * new Vector3(0, 0, 0.004f), new Vector3(0.03f, 0.03f, 0.006f), wt).transform.localRotation = q;
                            Ring(f, o + q * new Vector3(0, 0, 0.005f), 0.021f, 0.003f, red, (q * Quaternion.Euler(90, 0, 0)).eulerAngles);
                        }
                        B(f, new Vector3(0, 0.03f, -0.168f), new Vector3(0.06f, 0.01f, 0.006f), steel);
                        B(f, new Vector3(0, 0.2f, -0.135f), new Vector3(0.03f, 0.04f, 0.008f), mb);
                        break;
                    }
                    default:
                        S(f, Vector3.zero, new Vector3(0.2f, 0.12f, 0.26f), Skin);
                        break;
                }
            }
        }
    }
}
