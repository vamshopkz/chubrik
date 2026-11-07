using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Hair, ears and hats (incl. premium hats): every item is built from 100+ parts (locks, rims, stitches, gems).</summary>
    public partial class ChubrikModel
    {
        // ---- low-poly detail parts of this file (cached meshes, few vertices) ----
        static GameObject HwBall(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Ball(8, 5), p, pos, sc, m, euler);   // 54 verts
        static GameObject HwBall(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(8, 5), p, pos, Vector3.one * d, m);
        static GameObject HwDot(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(6, 4), p, pos, Vector3.one * d, m);   // 35 verts
        static GameObject HwDot(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Ball(6, 4), p, pos, sc, m, euler);

        /// <summary>Thin cone from a (base) to b (tip): 48 verts.</summary>
        static GameObject HwSpike(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Msh(Meshes.Cone(8), p, a, new Vector3(d, (b - a).magnitude, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        /// <summary>Thin rod a→b: 60 verts.</summary>
        static GameObject HwRod(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Msh(Meshes.Tube(6), p, (a + b) / 2, new Vector3(d, (b - a).magnitude / 2, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        /// <summary>Elongated ball lying along a→b (lock of hair, horn segment), flattened along <paramref name="n"/>.</summary>
        static GameObject HwSeg(Transform p, Vector3 a, Vector3 b, float d, Material m, Vector3 n = default, float flat = 1f, bool smooth = false)
        {
            var dir = b - a; float len = dir.magnitude; dir /= Mathf.Max(len, 1e-5f);
            var go = Msh(smooth ? Meshes.Ball() : Meshes.Ball(8, 5), p, (a + b) / 2, new Vector3(d, Mathf.Max(d, len * 1.3f), d * flat), m);
            var np = n - dir * Vector3.Dot(n, dir);
            go.transform.localRotation = np.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(np.normalized, dir) : Quaternion.FromToRotation(Vector3.up, dir);
            return go;
        }

        /// <summary>Low-poly torus (147 verts).</summary>
        static GameObject HwRing(Transform p, Vector3 pos, float R, float r, Material m, Vector3 euler = default, Vector3? scale = null)
            => Msh(Meshes.Torus(R, r, 20, 6), p, pos, scale ?? Vector3.one, m, euler);

        /// <summary>Flat part (stitch, plate, scale) lying on a surface with normal n, long side along t.</summary>
        static GameObject HwFlat(Transform p, Vector3 pos, Vector3 size, Material m, Vector3 n, Vector3 t, bool ball = false)
        {
            var go = ball ? HwDot(p, pos, size, m) : B(p, pos, size, m);
            var tp = t - n * Vector3.Dot(t, n);
            go.transform.localRotation = Quaternion.LookRotation(n.normalized, tp.sqrMagnitude > 1e-6f ? tp.normalized : Vector3.up);
            return go;
        }

        static Material HwShade(string hex, float k, float smooth = 0.35f, float metal = 0f)
        {
            var c = Hex(hex) * k; c.a = 1;
            return Mats.Lit(c, smooth, metal);
        }

        /// <summary>Gem in a low-poly setting: stone, collar, 4 prongs and a sparkle (7 parts, ~310 verts).</summary>
        static void HwGem(Transform p, Vector3 pos, float d, Material gem, Material setting, Vector3 normal)
        {
            var q = Quaternion.FromToRotation(Vector3.up, normal.normalized);
            HwBall(p, pos + q * Vector3.up * d * 0.15f, new Vector3(d, d * 0.6f, d), gem).transform.localRotation = q;
            Lc(p, pos, new Vector3(d * 1.15f, d * 0.08f, d * 1.15f), setting).transform.localRotation = q;
            for (int k = 0; k < 4; k++)
                HwDot(p, pos + q * (Quaternion.Euler(0, k * 90 + 45, 0) * Vector3.right * d * 0.5f + Vector3.up * d * 0.25f), d * 0.2f, setting);
            HwDot(p, pos + q * new Vector3(-0.15f * d, d * 0.42f, 0.15f * d), d * 0.18f, G("FFFFFF", 2f));
        }

        /// <summary>Points over the top of the head in rows (theta from the crown), skipping the face.</summary>
        static void HwCapPoints(float thMax, float step, float spacing, float faceTh, float faceGap, System.Action<float, float, int> f)
        {
            int idx = 0;
            for (float th = 0; th <= thMax + 0.01f; th += step)
            {
                int n = th < 1 ? 1 : Mathf.Max(1, Mathf.RoundToInt(360 * Mathf.Sin(th * Mathf.Deg2Rad) / spacing));
                for (int k = 0; k < n; k++)
                {
                    float ph = k * 360f / n + th * 1.7f;
                    if (th >= faceTh && Mathf.Abs(Mathf.DeltaAngle(ph, 90)) < faceGap) continue;
                    f(th, ph, idx++);
                }
            }
        }

        void Hair(int i, int set)
        {
            if (hairHidden) return;
            var root = Node("Hair", HeadC, Vector3.zero);
            switch (i)
            {
                case 0: // Ёжик: two-tone spikes in rows, frosted tips, a cowlick and sideburns
                {
                    var m = M("6B3E1E", 0.3f); var dk = HwShade("6B3E1E", 0.72f, 0.3f); var tip = M("B9844E", 0.35f);
                    HwCapPoints(72, 12, 22, 40, 48, (th, ph, k) =>
                    {
                        var p = HP(th, ph, 0.93f);
                        var dir = (Dt.EllN(Dt.HeadR, th, ph) + Vector3.back * 0.3f + Vector3.up * 0.1f).normalized;
                        float len = 0.15f + 0.035f * Mathf.Sin(k * 2.3f);
                        HwSpike(root, p, p + dir * len, 0.1f, k % 3 == 0 ? dk : m);
                        HwSpike(root, p + dir * len * 0.55f, p + dir * (len + 0.02f), 0.052f, tip);
                    });
                    for (int k = 0; k < 6; k++)   // cowlick swirl on the crown
                    {
                        var p = HP(16 + k * 1.5f, 270 + k * 60, 0.98f);
                        var d = Quaternion.Euler(0, -k * 60 - 50, 0) * Vector3.right;
                        HwSpike(root, p, p + (d * 0.07f + Vector3.up * 0.09f), 0.05f, k % 2 == 0 ? m : tip);
                    }
                    foreach (var sd in new[] { -1, 1 })   // sideburns
                        for (int k = 0; k < 2; k++)
                        {
                            var p = HP(74 + k * 9, 90 - sd * (55 - k * 6), 0.95f);
                            HwSpike(root, p, p + Vector3.down * 0.08f + Vector3.right * sd * 0.02f, 0.06f, dk);
                            HwSpike(root, p + Vector3.down * 0.04f, p + Vector3.down * 0.095f, 0.03f, tip);
                        }
                    break;
                }
                case 1: // Ирокез: fan blades along the midline, gel strands, root strip and buzzed sides
                {
                    var m = M("FF3FA4", 0.3f); var lt = M("FF9AD3", 0.35f); var dk = HwShade("FF3FA4", 0.65f, 0.3f);
                    Vector3 Mid(float a, float r) => a < 0 ? HP(-a, 90, r) : HP(a, 270, r);
                    for (int k = 0; k < 15; k++) HwBall(root, Mid(-45 + k * 10, 0.97f), 0.085f, dk);
                    for (int k = 0; k < 9; k++)
                    {
                        float a = -40 + k * 16;
                        var p = Mid(a, 0.95f);
                        var dir = (p.normalized + Vector3.back * 0.2f).normalized;
                        float len = 0.28f - Mathf.Abs(k - 3.5f) * 0.022f;
                        HwSpike(root, p, p + dir * len, 0.12f, m);
                        foreach (var sd in new[] { -1, 1 })
                            HwSpike(root, p + Vector3.right * sd * 0.03f, p + dir * len * 0.8f + Vector3.right * sd * 0.05f, 0.08f, m);
                        HwSpike(root, p + dir * len * 0.6f, p + dir * (len + 0.02f), 0.06f, lt);
                        if (k < 8)
                        {
                            var q = Mid(a + 8, 0.95f);
                            HwSpike(root, q, q + (q.normalized + Vector3.back * 0.3f).normalized * len * 0.7f, 0.05f, lt);
                        }
                    }
                    for (int k = 0; k < 10; k++)   // short fringe on both sides of the strip
                        foreach (var sd in new[] { -1, 1 })
                        {
                            var p = Mid(-36 + k * 13, 0.96f) + Vector3.right * sd * 0.065f;
                            HwSpike(root, p, p + (p.normalized + Vector3.right * sd * 0.4f).normalized * 0.08f, 0.06f, m);
                        }
                    foreach (var side in new[] { 0f, 180f })   // buzzed fade on the sides
                        for (int r = 0; r < 2; r++)
                            for (int k = 0; k < 7; k++)
                            {
                                float th = 40 + r * 18, ph = side - 40 + k * 13.3f + r * 6;
                                var p = HP(th, ph, 0.99f);
                                HwSpike(root, p, p + Dt.EllN(Dt.HeadR, th, ph) * 0.035f, 0.04f, dk);
                            }
                    var f0 = Mid(-48, 0.96f); var f1 = f0 + new Vector3(0, 0.08f, 0.12f);   // forelock curling forward
                    HwSpike(root, f0, f1, 0.08f, m);
                    HwSpike(root, f1 - new Vector3(0, 0, 0.03f), f1 + new Vector3(0, -0.05f, 0.03f), 0.05f, lt);
                    HwDot(root, f1, 0.05f, m);
                    break;
                }
                case 2: // Кудряшки: curl clusters with highlights and shade, ringlets on the sides and back
                {
                    var m = M("4A2A12", 0.2f); var lt = M("8A5630", 0.3f); var dk = HwShade("4A2A12", 0.6f, 0.2f);
                    HwCapPoints(84, 14, 30, 38, 46, (th, ph, k) =>
                    {
                        var p = HP(th, ph, 0.98f); var n = Dt.EllN(Dt.HeadR, th, ph);
                        Ls(root, p, 0.13f, m);
                        var side = Vector3.Cross(n, Vector3.up).normalized;
                        if (side.sqrMagnitude < 0.1f) side = Vector3.right;
                        HwDot(root, p + n * 0.045f + Vector3.up * 0.02f + side * 0.015f, 0.05f, lt);
                        HwDot(root, p + n * 0.03f - side * 0.04f, new Vector3(0.045f, 0.03f, 0.045f), dk);
                    });
                    foreach (var ph in new[] { 15f, 165f, 230f, 310f })
                    {
                        var c = HP(86, ph, 1.0f);
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 100 * Mathf.Deg2Rad;
                            HwBall(root, c + new Vector3(Mathf.Cos(a) * 0.03f, -0.03f - k * 0.05f, Mathf.Sin(a) * 0.03f), 0.08f - k * 0.004f, k % 2 == 0 ? m : lt);
                        }
                    }
                    break;
                }
                case 3: // Длинные пряди: cap, parting, long locks with highlights, side-swept bangs, star clip
                {
                    var m = M("F2C14E", 0.4f); var lt = M("FFE59A", 0.5f); var dk = HwShade("F2C14E", 0.72f, 0.35f);
                    var c = new Vector3(0, 0.09f, -0.02f); var r = new Vector3(0.43f, 0.33f, 0.41f);
                    S(root, c, r * 2, m);
                    for (int k = 0; k < 6; k++)   // parting
                    {
                        float a0 = -38 + k * 13, a1 = a0 + 13;
                        Vector3 P(float a) => Dt.Ell(c, r, Mathf.Abs(a), a < 0 ? 90 : 270, 1.005f);
                        HwRod(root, P(a0), P(a1), 0.014f, dk);
                    }
                    for (int k = 0; k < 14; k++)   // locks: sides and back
                    {
                        float ph = 145 + k * 18;
                        var out0 = new Vector3(Mathf.Cos(ph * Mathf.Deg2Rad), 0, Mathf.Sin(ph * Mathf.Deg2Rad));
                        var start = Dt.Ell(c, r, 75, ph, 0.98f);
                        float back = Mathf.Clamp01(-out0.z), len = 0.36f + back * 0.12f;
                        Vector3 L(float t) => start + out0 * (0.05f * Mathf.Sin(Mathf.PI * t * 0.8f) + 0.05f * t * t) + Vector3.down * len * t;
                        for (int s = 0; s < 6; s++)
                        {
                            float t0 = s / 6f, t1 = (s + 1) / 6f;
                            HwSeg(root, L(t0), L(t1), 0.13f * (1 - 0.45f * t0), s == 5 ? dk : m, out0, 0.55f);
                        }
                        var sideV = Vector3.Cross(Vector3.up, out0) * 0.025f;
                        HwRod(root, L(0.1f) + out0 * 0.03f + sideV, L(0.45f) + out0 * 0.035f + sideV, 0.014f, lt);
                        HwRod(root, L(0.45f) + out0 * 0.035f - sideV, L(0.75f) + out0 * 0.035f - sideV, 0.012f, lt);
                    }
                    for (int k = 0; k < 7; k++)   // bangs swept to the right
                    {
                        float ph = 55 + k * 11.7f;
                        var a = Dt.Ell(c, r, 28, ph, 1.0f); var b = Dt.Ell(c, r, 50, ph - 14, 1.04f); var e = Dt.Ell(c, r, 60, ph - 26, 1.06f);
                        var n = Dt.EllN(r, 45, ph);
                        HwSeg(root, a, b, 0.09f, k % 3 == 1 ? lt : m, n, 0.5f);
                        HwSeg(root, b, e, 0.06f, m, n, 0.5f);
                    }
                    var clip = Dt.Ell(c, r, 62, 28, 1.03f);   // hair clip with a little star
                    Msh(Meshes.Star(), root, clip + Vector3.right * 0.01f, Vector3.one * 0.09f, M("FF5FA2", 0.5f), new Vector3(0, 70, 0));
                    B(root, clip - new Vector3(0.0f, 0.0f, 0.04f), new Vector3(0.015f, 0.02f, 0.1f), M("FFFFFF", 0.6f), new Vector3(0, 20, 0));
                    HwDot(root, clip + Vector3.right * 0.03f, 0.03f, G("FFE14D", 1.4f));
                    break;
                }
                case 4: // Лысина с хвостом: gathered strands, scrunchie, ponytail of strands, horseshoe fringe, shine
                {
                    var m = M("2A2A2A", 0.4f); var hi = M("5A5A5A", 0.5f); var dk = M("1A1A1A", 0.3f); var red = M("E53935");
                    var tieP = new Vector3(0, 0.22f, -0.33f);
                    Ring(root, tieP, 0.05f, 0.025f, red, new Vector3(70, 0, 0));
                    for (int k = 0; k < 8; k++)   // scrunchie ruffles
                    {
                        float a = k * 45 * Mathf.Deg2Rad;
                        HwDot(root, tieP + Quaternion.Euler(70, 0, 0) * new Vector3(Mathf.Cos(a) * 0.058f, 0, Mathf.Sin(a) * 0.058f), 0.042f, red);
                    }
                    foreach (var sd in new[] { -1, 1 }) HwDot(root, tieP + new Vector3(sd * 0.03f, 0.05f, -0.03f), 0.03f, M("FFC83A", 0.8f, 0.8f));
                    var tail = Node("Pony", root, new Vector3(0, 0.22f, -0.36f), new Vector3(-150, 0, 0));
                    Ca(tail, new Vector3(0, 0.16f, 0), new Vector3(0.12f, 0.17f, 0.12f), m);
                    Ls(tail, new Vector3(0, 0.32f, 0), 0.13f, m);
                    for (int k = 0; k < 10; k++)   // strands of the ponytail
                    {
                        float a = k * 36 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        Vector3 p0 = d * 0.035f + Vector3.up * 0.03f, p1 = d * 0.055f + Vector3.up * 0.18f, p2 = d * 0.05f + Vector3.up * 0.34f, p3 = d * 0.075f + Vector3.up * 0.45f;
                        HwSeg(tail, p0, p1, 0.04f, m); HwSeg(tail, p1, p2, 0.04f, k % 3 == 0 ? hi : m); HwSeg(tail, p2, p3 - d * 0.01f, 0.035f, m);
                        HwSpike(tail, p2, p3 + d * 0.02f + Vector3.up * 0.04f, 0.035f, dk);
                    }
                    for (int k = 0; k < 4; k++) { float a = (k * 90 + 20) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); HwRod(tail, d * 0.062f + Vector3.up * 0.06f, d * 0.068f + Vector3.up * 0.3f, 0.012f, hi); }
                    var sw = Add<Sway>(tail.gameObject); sw.model = this; sw.amp = 8;
                    for (int k = 0; k < 8; k++)   // hair gathered from the fringe to the tie
                    {
                        float ph = 205 + k * 18.5f;
                        for (int s = 0; s < 3; s++)
                        {
                            float t0 = s / 3f, t1 = (s + 1) / 3f;
                            Vector3 P(float t) => HP(Mathf.Lerp(85, 60, t), ph + Mathf.DeltaAngle(ph, 270) * t, 1.01f);
                            HwRod(root, P(t0), P(t1), 0.016f, s == 2 ? m : dk);
                        }
                    }
                    for (int r = 0; r < 2; r++)   // horseshoe fringe
                        for (int k = 0; k < 12; k++)
                        {
                            float th = 82 + r * 10, ph = 192 + k * 14.5f + r * 7;
                            var p = HP(th, ph, 0.99f);
                            HwSpike(root, p, p + (Dt.EllN(Dt.HeadR, th, ph) * 0.4f + Vector3.down).normalized * 0.055f, 0.05f, r == 0 ? m : dk);
                        }
                    var sh = Glass("FFFFFF", 0.35f);   // shine on the bald top
                    HwFlat(root, HP(28, 120, 1.0f), new Vector3(0.12f, 0.05f, 0.012f), sh, Dt.EllN(Dt.HeadR, 28, 120), Vector3.right, true);
                    HwFlat(root, HP(38, 105, 1.0f), new Vector3(0.04f, 0.03f, 0.01f), sh, Dt.EllN(Dt.HeadR, 38, 105), Vector3.right, true);
                    break;
                }
                case 5: // Космо-косички: parted cap, combed strands, plaited braids with glowing ties, stars
                {
                    var m = M("7B4BFF", 0.4f); var lt = M("A98BFF", 0.45f); var dk = HwShade("7B4BFF", 0.65f, 0.35f);
                    var cyan = G("35E0FF", 2.4f); var star = G("FFE14D", 2f);
                    var c = new Vector3(0, 0.12f, -0.03f); var r = new Vector3(0.42f, 0.3f, 0.4f);
                    S(root, c, r * 2, m);
                    Vector3 Mid(float a) => Dt.Ell(c, r, Mathf.Abs(a), a < 0 ? 90 : 270, 1.01f);
                    for (int k = 0; k < 5; k++) HwRod(root, Mid(-35 + k * 18), Mid(-17 + k * 18), 0.014f, dk);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        float phE = sd > 0 ? -15 : 195;
                        for (int j = 0; j < 6; j++)   // strands combed from the parting to the braid
                        {
                            float a = -30 + j * 17, th0 = Mathf.Abs(a) + 4, ph0 = a < 0 ? 90 - sd * 8 : 270 + sd * 8;
                            Vector3 P(float t) => Dt.Ell(c, r, Mathf.Lerp(th0, 100, t), ph0 + Mathf.DeltaAngle(ph0, phE) * t, 1.02f);
                            for (int s = 0; s < 3; s++) HwRod(root, P(s / 3f), P((s + 1) / 3f), 0.02f, j % 2 == 0 ? lt : dk);
                        }
                        var b = Node("Braid", root, new Vector3(sd * 0.36f, 0.02f, -0.12f), new Vector3(0, 0, sd * 12));
                        for (int k = 0; k < 9; k++)   // plait: alternating lobes
                        {
                            float y = -0.04f - k * 0.045f, s = k % 2 == 0 ? 1 : -1, d = 0.085f - k * 0.004f;
                            HwBall(b, new Vector3(s * 0.018f, y, 0.005f), new Vector3(d, d * 0.8f, d * 0.9f), k % 2 == 0 ? m : lt, new Vector3(0, 0, s * 25));
                            HwBall(b, new Vector3(-s * 0.016f, y - 0.022f, -0.006f), new Vector3(d * 0.8f, d * 0.65f, d * 0.8f), dk, new Vector3(0, 0, -s * 25));
                        }
                        HwRing(b, new Vector3(0, -0.02f, 0), 0.045f, 0.012f, cyan);
                        HwRing(b, new Vector3(0, -0.42f, 0), 0.035f, 0.011f, cyan);
                        var tip = Ls(b, new Vector3(0, -0.47f, 0), 0.12f, cyan); Add<Pulse>(tip);
                        for (int k = 0; k < 3; k++)
                        {
                            Msh(Meshes.Star(), b, new Vector3(HwAlt(k) * 0.045f, -0.1f - k * 0.11f, 0.03f), Vector3.one * 0.045f, star, new Vector3(0, 0, k * 25));
                            float a = k * 120 * Mathf.Deg2Rad;
                            HwDot(b, new Vector3(Mathf.Cos(a) * 0.08f, -0.47f, Mathf.Sin(a) * 0.08f), 0.022f, cyan);
                        }
                        var sw = Add<Sway>(b.gameObject); sw.model = this; sw.amp = 6; sw.phase = sd;
                    }
                    var rng = new Rng(5);
                    for (int k = 0; k < 12; k++)   // tiny stars in the hair
                    {
                        float th = rng.F(15, 85), ph = rng.F(0, 360);
                        if (th > 45 && Mathf.Abs(Mathf.DeltaAngle(ph, 90)) < 50) ph += 180;
                        Msh(Meshes.Star(), root, Dt.Ell(c, r, th, ph, 1.02f), Vector3.one * 0.04f, star).transform.localRotation = Quaternion.LookRotation(Dt.EllN(r, th, ph));
                    }
                    for (int k = 0; k < 6; k++)   // bangs
                    {
                        float ph = 60 + k * 12;
                        HwSeg(root, Dt.Ell(c, r, 35, ph, 1.0f), Dt.Ell(c, r, 62, ph + (k < 3 ? -10 : 10), 1.05f), 0.08f, k % 2 == 0 ? m : lt, Dt.EllN(r, 50, ph), 0.5f);
                    }
                    break;
                }
                case 6: // Огненный чуб: layered flame tongues over the scalp, glowing forelock, embers
                {
                    var o = G("FF6A00", 2f); var y = G("FFC400", 2.2f); var core = G("FFF3B0", 2.5f); var red = M("8A2A00", 0.3f);
                    var c = new Vector3(0, 0.15f, -0.04f); var r = new Vector3(0.41f, 0.275f, 0.39f);
                    S(root, c, r * 2, M("B33A00", 0.3f));
                    for (int k = 0; k < 4; k++)
                    {
                        var fl = Node("Flame", root, new Vector3(-0.12f + k * 0.08f, 0.33f, 0.16f - k * 0.02f), new Vector3(-35 + k * 5, 0, (k - 1.5f) * 12));
                        var sc = new Vector3(0.16f, 0.36f - Mathf.Abs(k - 1.5f) * 0.06f, 0.12f);
                        var cn = Cone(fl, Vector3.zero, sc, k % 2 == 0 ? o : y);
                        Add<Pulse>(cn).amount = 0.12f;
                        Cone(fl, new Vector3(0, 0.005f, 0.01f), Vector3.Scale(sc, new Vector3(0.62f, 0.66f, 0.62f)), k % 2 == 0 ? y : o);
                        Cone(fl, new Vector3(0, 0.01f, 0.015f), Vector3.Scale(sc, new Vector3(0.32f, 0.4f, 0.32f)), core);
                    }
                    HwCapPoints(75, 15, 26, 40, 50, (th, ph, k) =>   // flame tongues licking backwards
                    {
                        var p = Dt.Ell(c, r, th, ph, 0.97f);
                        var dir = (Dt.EllN(r, th, ph) + Vector3.back * 0.6f + Vector3.up * 0.3f).normalized;
                        HwSpike(root, p, p + dir * (0.1f + (k % 3) * 0.02f), 0.08f, o);
                        HwSpike(root, p + dir * 0.02f, p + dir * 0.08f, 0.045f, y);
                    });
                    var rng = new Rng(11);
                    for (int k = 0; k < 14; k++)   // embers
                        HwDot(root, new Vector3(rng.F(-0.22f, 0.22f), rng.F(0.45f, 0.72f), rng.F(-0.15f, 0.2f)), rng.F(0.018f, 0.032f), k % 2 == 0 ? y : o);
                    foreach (var sd in new[] { -1, 1 })   // sideburn flames
                        for (int k = 0; k < 4; k++)
                        {
                            var p = HP(70 + k * 5, 90 - sd * (52 + k * 7), 0.98f);
                            HwSpike(root, p, p + new Vector3(sd * 0.02f, 0.08f - k * 0.01f, -0.03f), 0.045f, k % 2 == 0 ? o : y);
                        }
                    for (int k = 0; k < 10; k++)   // dark roots along the hairline
                        HwBall(root, Dt.Ell(c, r, 62, 40 + k * 11.1f, 1.0f), new Vector3(0.07f, 0.05f, 0.07f), red);
                    break;
                }
                default: // Взрыв на макушке: puffs with fluff, burst rays, singed bits, sparkles, rising hair
                {
                    var m = M("FF8A00", 0.25f); var lt = M("FFB347", 0.3f); var dk = M("C0560A", 0.25f); var spark = G("FFE14D", 2f);
                    var r = new Rng(7);
                    var puffs = new List<(Vector3, float)>();
                    for (int k = 0; k < 16; k++)
                    {
                        float th = r.F(0, 50), ph = r.F(0, 360);
                        var p = HP(th, ph, 1.05f) + Vector3.up * r.F(0.05f, 0.18f); float d = r.F(0.16f, 0.26f);
                        Ls(root, p, d, m); puffs.Add((p, d));
                        for (int s = 0; s < 3; s++)
                        {
                            var dir = new Vector3(r.F(-1, 1), r.F(0.2f, 1), r.F(-1, 1)).normalized;
                            HwBall(root, p + dir * d * 0.42f, d * 0.45f, s == 0 ? lt : m);
                        }
                    }
                    var cen = new Vector3(0, 0.45f, -0.02f);
                    for (int k = 0; k < 20; k++)   // burst rays
                    {
                        var dir = Quaternion.Euler(r.F(-70, 70), r.F(0, 360), 0) * Vector3.up;
                        if (dir.y < 0.1f) dir.y = -dir.y + 0.2f;
                        dir.Normalize();
                        HwSpike(root, cen + dir * 0.24f, cen + dir * 0.37f, 0.035f, spark);
                    }
                    for (int k = 0; k < 12; k++)   // singed bits
                    {
                        var (p, d) = puffs[k];
                        HwDot(root, p + new Vector3(r.F(-1, 1), r.F(0, 1), r.F(-1, 1)).normalized * d * 0.48f, 0.035f, dk);
                    }
                    for (int k = 0; k < 6; k++)
                    {
                        var a = k * 60 * Mathf.Deg2Rad;
                        Msh(Meshes.Star(4), root, cen + new Vector3(Mathf.Cos(a) * 0.36f, 0.12f + (k % 2) * 0.1f, Mathf.Sin(a) * 0.3f), Vector3.one * 0.06f, spark, new Vector3(0, -k * 60 + 90, 0));
                    }
                    for (int k = 0; k < 12; k++)   // hair rising from the hairline into the blast
                    {
                        float ph = 120 + k * 27;
                        var p = HP(62, ph, 0.98f);
                        HwSpike(root, p, Vector3.Lerp(p, cen, 0.45f) + Vector3.up * 0.05f, 0.06f, k % 2 == 0 ? m : dk);
                    }
                    break;
                }
            }
        }

        static float HwAlt(int k) => k % 2 == 0 ? 1 : -1;

        void Ears(int i, int set)
        {
            if (earsHidden) return;
            var pink = M("FF9FB8", 0.3f); var pinkD = M("F2708F", 0.3f); var cream = M("FFF4E6", 0.3f);
            foreach (var sd in new[] { -1, 1 })
            {
                var e = Node("Ear", HeadC, Vector3.zero);
                switch (i)
                {
                    case 0: // Котёнка: rounded rim, inner fur, lynx tuft, tabby stripes
                    {
                        var f = Node("EarF", e, new Vector3(sd * 0.24f, 0.26f, -0.02f), new Vector3(0, 0, -sd * 24));
                        Cone(f, Vector3.zero, new Vector3(0.22f, 0.26f, 0.1f), Skin);
                        Cone(f, new Vector3(0, 0.012f, 0.035f), new Vector3(0.12f, 0.17f, 0.04f), pink);
                        Cone(f, new Vector3(0, 0.02f, 0.046f), new Vector3(0.07f, 0.11f, 0.03f), pinkD);
                        foreach (var s in new[] { -1f, 1f })   // rim along both edges
                            for (int k = 0; k < 8; k++)
                            {
                                Vector3 R(float t) => new Vector3(s * 0.108f * (1 - t), 0.26f * t, 0);
                                HwSeg(f, R(0.04f + k * 0.11f), R(0.15f + k * 0.11f), 0.03f, Skin);
                            }
                        for (int k = 0; k < 18; k++)   // inner fur tufts
                        {
                            float x = -0.05f + (k % 6) * 0.02f, y = 0.012f + (k / 6) * 0.022f, z = 0.05f * (1 - y / 0.26f) + 0.018f;
                            HwSpike(f, new Vector3(x, y, z), new Vector3(x * 1.4f, y + 0.08f + (k % 3) * 0.02f, z + 0.012f), 0.016f, cream);
                        }
                        for (int k = 0; k < 3; k++) HwSpike(f, new Vector3((k - 1) * 0.006f, 0.235f, 0), new Vector3((k - 1) * 0.022f, 0.31f + (k == 1 ? 0.02f : 0), 0), 0.02f, Skin);
                        for (int k = 0; k < 8; k++)   // fluff where the ear meets the head
                            HwDot(f, new Vector3(-0.1f + k * 0.0285f, 0.0f, (k % 2 == 0 ? 0.02f : -0.025f)), 0.05f, Skin);
                        var stripe = Glass("3A2410", 0.25f);
                        for (int k = 0; k < 4; k++)   // tabby stripes on the back
                        {
                            float y = 0.05f + k * 0.04f, w = 0.11f * (1 - y / 0.26f) * 1.5f;
                            B(f, new Vector3(0, y, -0.05f * (1 - y / 0.26f) - 0.003f), new Vector3(w, 0.014f, 0.004f), stripe, new Vector3(-11, 0, 0));
                        }
                        break;
                    }
                    case 1: // Медвежьи: fluffy rim, stitched inner patch like a teddy bear
                    {
                        var f = Node("EarF", e, new Vector3(sd * 0.28f, 0.28f, -0.03f));
                        S(f, Vector3.zero, new Vector3(0.19f, 0.19f, 0.08f), Skin);
                        Ls(f, new Vector3(0, 0, 0.03f), new Vector3(0.11f, 0.11f, 0.04f), pink);
                        HwDot(f, new Vector3(0, 0, 0.05f), new Vector3(0.05f, 0.05f, 0.015f), pinkD);
                        for (int k = 0; k < 16; k++)   // fluffy rim
                        {
                            float a = (-30 + k * 16) * Mathf.Deg2Rad;
                            HwDot(f, new Vector3(Mathf.Cos(a) * 0.092f, Mathf.Sin(a) * 0.092f, 0), 0.042f, Skin);
                        }
                        for (int k = 0; k < 12; k++)   // fur around the inner patch
                        {
                            float a = k * 30 * Mathf.Deg2Rad;
                            HwDot(f, new Vector3(Mathf.Cos(a) * 0.06f, Mathf.Sin(a) * 0.06f, 0.042f), 0.022f, cream);
                        }
                        var thread = M("5A3A22", 0.3f);
                        for (int k = 0; k < 14; k++)   // stitches around the patch
                        {
                            float a = k * 360f / 14 * Mathf.Deg2Rad;
                            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            HwFlat(f, dir * 0.07f + Vector3.forward * 0.047f, new Vector3(0.006f, 0.016f, 0.004f), thread, Vector3.forward, new Vector3(-dir.y, dir.x, 0));
                        }
                        var rng = new Rng(20 + sd);
                        for (int k = 0; k < 10; k++)   // fur on the back
                            HwDot(f, new Vector3(rng.F(-0.07f, 0.07f), rng.F(-0.04f, 0.08f), -0.035f), 0.03f, Skin);
                        for (int k = 0; k < 6; k++) HwDot(f, new Vector3(-0.06f + k * 0.024f, -0.07f, (k % 2) * 0.02f - 0.01f), 0.045f, Skin);
                        break;
                    }
                    case 2: // Заячьи длинные: rim, fur along the pink, veins, fluffy base and tip
                    {
                        var f = Node("EarF", e, new Vector3(sd * 0.13f, 0.58f, -0.04f), new Vector3(0, 0, -sd * 10));
                        Ca(f, Vector3.zero, new Vector3(0.13f, 0.26f, 0.07f), Skin);
                        Ca(f, new Vector3(0, 0, 0.035f), new Vector3(0.07f, 0.2f, 0.03f), pink);
                        float W(float y, float h, float w) => w * Mathf.Sqrt(Mathf.Max(0, 1 - y * y / (h * h)));
                        foreach (var s in new[] { -1f, 1f })
                        {
                            for (int k = 0; k < 8; k++)   // rim
                            {
                                float y0 = -0.2f + k * 0.055f, y1 = y0 + 0.055f;
                                HwSeg(f, new Vector3(s * W(y0, 0.27f, 0.064f), y0, 0.008f), new Vector3(s * W(y1, 0.27f, 0.064f), y1, 0.008f), 0.024f, Skin);
                            }
                            for (int k = 0; k < 10; k++)   // fur along the pink edge
                            {
                                float y = -0.16f + k * 0.034f;
                                HwDot(f, new Vector3(s * (W(y, 0.21f, 0.036f) + 0.008f), y, 0.048f), 0.018f, cream);
                            }
                        }
                        for (int v = -1; v <= 1; v++)   // veins
                        {
                            var a = new Vector3(v * 0.004f, -0.1f, 0.051f); var b = new Vector3(v * 0.01f, 0.01f, 0.051f); var c = new Vector3(v * 0.016f, 0.12f, 0.049f);
                            HwRod(f, a, b, 0.005f, pinkD); HwRod(f, b, c, 0.004f, pinkD);
                        }
                        for (int k = 0; k < 8; k++)
                        {
                            float a = k * 45 * Mathf.Deg2Rad;
                            HwDot(f, new Vector3(Mathf.Cos(a) * 0.06f, -0.22f, Mathf.Sin(a) * 0.035f), 0.045f, Skin);
                        }
                        for (int k = 0; k < 3; k++) HwDot(f, new Vector3((k - 1) * 0.018f, 0.245f + (k == 1 ? 0.012f : 0), 0.01f), 0.03f, cream);
                        break;
                    }
                    case 3: // Эльфийские: rims, inner ridge, gold cuffs with gems and filigree, chain with a leaf
                    {
                        var rot = new Vector3(-15, 0, -sd * 75);
                        var f = Node("EarF", e, new Vector3(sd * 0.36f, 0.06f, -0.04f), rot);
                        var gold = M("FFC83A", 0.85f, 0.8f); var emer = G("2EE59D", 1.2f);
                        Cone(f, Vector3.zero, new Vector3(0.13f, 0.34f, 0.06f), Skin);
                        Cone(f, new Vector3(0, 0.02f, 0.02f), new Vector3(0.07f, 0.24f, 0.025f), pink);
                        float u = -sd;
                        Vector3 Edge(float s, float t) => new Vector3(s * 0.064f * (1 - t), 0.34f * t, 0);
                        for (int k = 0; k < 7; k++) HwSeg(f, Edge(u, 0.03f + k * 0.125f), Edge(u, 0.14f + k * 0.125f), 0.022f, Skin);
                        for (int k = 0; k < 5; k++) HwSeg(f, Edge(-u, 0.03f + k * 0.14f), Edge(-u, 0.15f + k * 0.14f), 0.018f, Skin);
                        for (int k = 0; k < 8; k++) { float y = 0.03f + k * 0.03f; HwDot(f, new Vector3(0, y, 0.03f * (1 - y / 0.34f) + 0.006f), new Vector3(0.012f, 0.03f, 0.012f), Skin); }
                        foreach (var y in new[] { 0.08f, 0.15f })   // cuffs
                        {
                            float rx = 0.065f * (1 - y / 0.34f) + 0.006f, rz = 0.03f * (1 - y / 0.34f) + 0.006f;
                            HwRing(f, new Vector3(0, y, 0), rx, 0.008f, gold, default, new Vector3(1, 1, rz / rx));
                            HwGem(f, new Vector3(0, y, rz + 0.006f), 0.026f, emer, gold, Vector3.forward);
                            for (int k = 0; k < 4; k++) HwDot(f, new Vector3((k < 2 ? -1 : 1) * rx * 0.75f, y + (k % 2 == 0 ? 0.012f : -0.012f), rz * 0.7f), 0.01f, gold);
                        }
                        HwDot(f, new Vector3(0, 0.33f, 0), 0.018f, pinkD);
                        var lobe = new Vector3(-u * 0.045f, 0.012f, 0.0f);
                        HwBall(f, lobe, 0.042f, Skin);
                        var down = Quaternion.Inverse(Quaternion.Euler(rot)) * Vector3.down;
                        for (int k = 0; k < 5; k++) HwDot(f, lobe + down * (0.03f + k * 0.018f), 0.011f, gold);
                        var leafP = lobe + down * 0.13f;
                        HwFlat(f, leafP, new Vector3(0.03f, 0.05f, 0.01f), emer, Vector3.forward, down, true);
                        HwDot(f, leafP - down * 0.025f, 0.014f, gold);
                        break;
                    }
                    case 4: // Локаторы: dish with rim, ribs, struts, rivets, feed arms, bracket, cable and lights
                    {
                        var metal = M("B9C3CF", 0.8f, 0.6f); var dark = M("5A6470", 0.6f, 0.5f);
                        var f = Node("Dish", e, new Vector3(sd * 0.42f, 0.05f, 0), new Vector3(0, 0, -sd * 90));
                        Cy(f, Vector3.zero, new Vector3(0.28f, 0.015f, 0.28f), metal);
                        Cone(f, Vector3.zero, new Vector3(0.07f, 0.14f, 0.07f), metal);
                        Ls(f, new Vector3(0, 0.15f, 0), 0.04f, G("FF3B3B"));
                        HwRing(f, new Vector3(0, 0.012f, 0), 0.14f, 0.012f, metal);
                        HwRing(f, new Vector3(0, 0.017f, 0), 0.1f, 0.005f, dark);
                        HwRing(f, new Vector3(0, 0.017f, 0), 0.06f, 0.005f, dark);
                        for (int k = 0; k < 8; k++)
                        {
                            float a = k * 45;
                            var d = Quaternion.Euler(0, a, 0) * Vector3.right;
                            B(f, d * 0.08f + Vector3.up * 0.018f, new Vector3(0.1f, 0.004f, 0.006f), dark, new Vector3(0, -a, 0));
                        }
                        for (int k = 0; k < 12; k++) { float a = k * 30 * Mathf.Deg2Rad; HwDot(f, new Vector3(Mathf.Cos(a) * 0.127f, 0.02f, Mathf.Sin(a) * 0.127f), 0.012f, dark); }
                        for (int k = 0; k < 3; k++) { float a = (k * 120 + 30) * Mathf.Deg2Rad; HwRod(f, new Vector3(Mathf.Cos(a) * 0.13f, 0.015f, Mathf.Sin(a) * 0.13f), new Vector3(0, 0.12f, 0), 0.008f, metal); }
                        Lc(f, new Vector3(0, -0.03f, 0), new Vector3(0.06f, 0.02f, 0.06f), dark);   // bracket
                        foreach (var s in new[] { -1f, 1f })
                        {
                            B(f, new Vector3(s * 0.03f, -0.035f, 0), new Vector3(0.012f, 0.04f, 0.05f), metal);
                            HwDot(f, new Vector3(s * 0.038f, -0.04f, 0.015f), 0.01f, dark);
                            HwDot(f, new Vector3(s * 0.038f, -0.04f, -0.015f), 0.01f, dark);
                            HwDot(f, new Vector3(s * 0.04f, -0.05f, 0), 0.016f, metal);   // hinge caps
                        }
                        Lc(f, new Vector3(0, -0.05f, 0), new Vector3(0.025f, 0.04f, 0.025f), dark, new Vector3(0, 0, 90));
                        Vector3[] cab = { new Vector3(0.0f, -0.04f, -0.03f), new Vector3(0.03f, -0.03f, -0.08f), new Vector3(0.06f, -0.04f, -0.12f), new Vector3(0.07f, -0.07f, -0.15f), new Vector3(0.06f, -0.1f, -0.17f) };
                        for (int k = 0; k < 4; k++) HwRod(f, cab[k], cab[k + 1], 0.012f, M("2A2E36", 0.3f));
                        for (int k = 0; k < 3; k++) { float a = (k * 120 + 90) * Mathf.Deg2Rad; HwDot(f, new Vector3(Mathf.Cos(a) * 0.145f, 0.02f, Mathf.Sin(a) * 0.145f), 0.016f, G(k == 1 ? "FF3B3B" : "3BFF7A", 2f)); }
                        for (int k = 0; k < 6; k++) B(f, new Vector3(0, -0.017f, -0.09f + k * 0.036f), new Vector3(0.2f - Mathf.Abs(k - 2.5f) * 0.04f, 0.003f, 0.006f), dark);
                        Lc(f, new Vector3(0, -0.02f, 0), new Vector3(0.09f, 0.006f, 0.09f), metal);
                        for (int k = 0; k < 3; k++) B(f, new Vector3(-0.02f + k * 0.02f, -0.026f, 0.03f), new Vector3(0.008f, 0.004f, 0.025f), dark);
                        break;
                    }
                    case 5: // Рожки демона: curved ridged horn, highlight, dark tip, spiky base, embers and glowing cracks
                    {
                        var red = M("C62828", 0.6f); var dk = M("7A1010", 0.5f); var hi = M("FF6B6B", 0.7f); var tipM = M("3A0808", 0.6f);
                        var ember = G("FF6A00", 2.2f);
                        var h = Node("Horn", e, new Vector3(sd * 0.16f, 0.33f, 0.06f), new Vector3(-10, 0, -sd * 22));
                        var pts = new Vector3[9]; var dirs = new Vector3[9];
                        pts[0] = Vector3.zero;
                        for (int k = 0; k < 9; k++)
                        {
                            dirs[k] = Quaternion.Euler(0, 0, -sd * 48 * k / 8f) * Vector3.up;
                            if (k < 8) pts[k + 1] = pts[k] + dirs[k] * 0.034f;
                        }
                        for (int k = 0; k < 8; k++) HwSeg(h, pts[k], pts[k + 1], 0.1f * (1 - k / 9f), red, default, 1, true);
                        for (int k = 1; k < 7; k++)
                        {
                            float d = 0.1f * (1 - k / 9f) * 1.06f;
                            Ls(h, pts[k], new Vector3(d, d * 0.22f, d), dk).transform.localRotation = Quaternion.FromToRotation(Vector3.up, dirs[k]);
                        }
                        for (int k = 0; k < 3; k++) HwRod(h, pts[k * 2 + 1] + Vector3.forward * 0.036f * (1 - k * 0.25f), pts[k * 2 + 3] + Vector3.forward * 0.03f * (1 - k * 0.25f), 0.01f, hi);
                        HwSpike(h, pts[8], pts[8] + dirs[8] * 0.05f, 0.03f, tipM);
                        HwRing(h, new Vector3(0, -0.005f, 0), 0.055f, 0.012f, dk);
                        for (int r = 0; r < 2; r++)
                            for (int k = 0; k < 8; k++)
                            {
                                float a = (k * 45 + r * 22.5f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                                var p = d * (0.058f + r * 0.012f) + Vector3.down * (0.01f + r * 0.012f);
                                HwSpike(h, p, p + d * 0.03f + Vector3.up * (0.035f - r * 0.012f), 0.022f, r == 0 ? dk : tipM);
                            }
                        var rng = new Rng(30 + sd);
                        for (int k = 0; k < 10; k++)   // rough texture
                        {
                            int s = 1 + rng.I(6); float a = rng.F(0, 360) * Mathf.Deg2Rad, d = 0.1f * (1 - s / 9f) * 0.48f;
                            var side = Vector3.Cross(dirs[s], Vector3.forward).normalized;
                            HwDot(h, pts[s] + (side * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)) * d, 0.012f, dk);
                        }
                        for (int k = 0; k < 4; k++) HwDot(h, pts[2 + k] + new Vector3(sd * 0.06f, 0.02f, 0.03f * (k % 2 == 0 ? 1 : -1)), 0.016f, ember);
                        HwRod(h, pts[1] + Vector3.forward * 0.044f + Vector3.right * 0.01f, pts[3] + Vector3.forward * 0.038f, 0.006f, ember);
                        HwRod(h, pts[3] + Vector3.forward * 0.038f, pts[4] + Vector3.forward * 0.034f - Vector3.right * 0.01f, 0.005f, ember);
                        break;
                    }
                    case 6: // Мягкие висячие: fluffy fringe, curly fur, a darker patch with spots, a bow on the left ear
                    {
                        var f = Node("EarF", e, new Vector3(sd * 0.39f, -0.04f, -0.03f), new Vector3(0, 0, sd * 14));
                        var er = new Vector3(0.07f, 0.2f, 0.035f);
                        Ca(f, Vector3.zero, new Vector3(0.14f, 0.2f, 0.07f), Skin);
                        for (int k = 0; k < 12; k++)   // fringe at the bottom
                        {
                            float ph = k * 30;
                            HwDot(f, Dt.Ell(new Vector3(0, -0.15f, 0), new Vector3(0.07f, 0.06f, 0.035f), 120, ph), 0.04f, Skin);
                        }
                        for (int k = 0; k < 10; k++) HwDot(f, Dt.Ell(new Vector3(0, -0.11f, 0), new Vector3(0.075f, 0.05f, 0.04f), 95, k * 36 + 18), 0.035f, Skin);
                        var rng = new Rng(40 + sd);
                        float phC = sd > 0 ? 0 : 180;
                        for (int k = 0; k < 20; k++) HwDot(f, Dt.Ell(Vector3.zero, er, rng.F(35, 140), phC + rng.F(-70, 70), 1.02f), 0.035f, Skin);
                        for (int k = 0; k < 6; k++) HwDot(f, new Vector3(-0.05f + k * 0.02f, 0.17f, (k % 2 == 0 ? 0.015f : -0.015f)), 0.04f, Skin);
                        var spot = Glass("5A3A22", 0.3f);
                        var pn = Dt.EllN(er, 95, phC + sd * 10);
                        HwFlat(f, Dt.Ell(Vector3.zero, er, 95, phC + sd * 10, 1.03f), new Vector3(0.09f, 0.12f, 0.012f), spot, pn, Vector3.up, true);
                        for (int k = 0; k < 4; k++)
                        {
                            float th = 50 + k * 22, ph = phC - sd * (30 - k * 12);
                            HwFlat(f, Dt.Ell(Vector3.zero, er, th, ph, 1.04f), new Vector3(0.025f, 0.025f, 0.008f), spot, Dt.EllN(er, th, ph), Vector3.up, true);
                        }
                        if (sd < 0)   // bow
                        {
                            var bow = M("FF5FA2", 0.4f); var bp = new Vector3(-0.02f, 0.15f, 0.04f);
                            Cone(f, bp, new Vector3(0.06f, 0.06f, 0.03f), bow, new Vector3(0, 0, 90));
                            Cone(f, bp, new Vector3(0.06f, 0.06f, 0.03f), bow, new Vector3(0, 0, -90));
                            HwDot(f, bp, 0.03f, bow);
                            B(f, bp + new Vector3(-0.012f, -0.03f, 0), new Vector3(0.012f, 0.045f, 0.006f), bow, new Vector3(0, 0, -15));
                            B(f, bp + new Vector3(0.012f, -0.03f, 0), new Vector3(0.012f, 0.045f, 0.006f), bow, new Vector3(0, 0, 15));
                            HwDot(f, bp + new Vector3(-0.035f, 0.004f, 0.012f), 0.01f, cream);
                            HwDot(f, bp + new Vector3(0.035f, 0.004f, 0.012f), 0.01f, cream);
                        }
                        break;
                    }
                    default: // Неоновые антенны: telescopic rods, spring, LEDs, orbit ring, halo, sparks, headband
                    {
                        var metal = M("333333", 0.6f, 0.5f); var chrome = M("C9D2DC", 0.85f, 0.8f);
                        string col = sd < 0 ? "FF3FA4" : "35E0FF";
                        var neon = G(col, 2.4f);
                        Vector3 a = new Vector3(sd * 0.12f, 0.3f, 0), b = new Vector3(sd * 0.18f, 0.6f, 0), ab = b - a;
                        for (int k = 0; k < 3; k++) HwRod(e, a + ab * (k / 3f), a + ab * ((k + 1) / 3f), 0.03f - k * 0.006f, metal);
                        for (int k = 1; k < 3; k++) Lc(e, a + ab * (k / 3f), new Vector3(0.034f - k * 0.004f, 0.008f, 0.034f - k * 0.004f), chrome).transform.localRotation = Quaternion.FromToRotation(Vector3.up, ab);
                        var q = Quaternion.FromToRotation(Vector3.up, ab.normalized);
                        for (int k = 0; k < 14; k++)   // spring at the base
                        {
                            float t = k * 50 * Mathf.Deg2Rad;
                            HwDot(e, a + ab.normalized * (0.02f + k * 0.005f) + q * new Vector3(Mathf.Cos(t) * 0.022f, 0, Mathf.Sin(t) * 0.022f), 0.012f, chrome);
                        }
                        Ls(e, a, new Vector3(0.07f, 0.035f, 0.07f), metal);
                        for (int k = 0; k < 4; k++) { float t = (k * 90 + 45) * Mathf.Deg2Rad; HwDot(e, a + new Vector3(Mathf.Cos(t) * 0.028f, 0.012f, Mathf.Sin(t) * 0.028f), 0.01f, chrome); }
                        for (int k = 0; k < 6; k++)
                            B(e, a + ab * (0.18f + k * 0.13f) + Vector3.forward * 0.012f, new Vector3(0.012f, 0.02f, 0.006f), k % 2 == 0 ? neon : G("FFFFFF", 1.5f)).transform.localRotation = q;
                        Lc(e, b - ab.normalized * 0.04f, new Vector3(0.03f, 0.01f, 0.03f), chrome).transform.localRotation = q;
                        HwRing(e, b + ab.normalized * 0.02f, 0.075f, 0.006f, neon, new Vector3(70, sd * 30, 0));
                        Ls(e, b + ab.normalized * 0.02f, 0.15f, Add(col, 0.25f));
                        var ball = S(e, b + ab.normalized * 0.02f, 0.09f, neon);
                        Add<Pulse>(ball);
                        for (int k = 0; k < 4; k++)
                        {
                            float t = (k * 90 + 20) * Mathf.Deg2Rad;
                            Msh(Meshes.Star(4), e, b + ab.normalized * 0.02f + new Vector3(Mathf.Cos(t) * 0.1f, Mathf.Sin(t) * 0.1f, 0.02f), Vector3.one * 0.03f, neon);
                        }
                        Vector3 Band(float th) => (th >= 0 ? HP(th, 0, 1.05f) : HP(-th, 180, 1.05f)) + Vector3.back * 0.02f;
                        for (int k = 0; k < 3; k++) HwRod(e, a + Vector3.down * 0.01f + Vector3.back * 0.01f * k, Band(sd * (24 + k * 8)), 0.006f, k == 1 ? neon : metal);   // wires
                        if (sd > 0)   // headband over the head
                        {
                            var band = M("2A2E36", 0.5f, 0.3f);
                            for (int k = 0; k < 14; k++)
                            {
                                float t0 = -84 + k * 12f, t1 = t0 + 12;
                                var p0 = Band(t0); var p1 = Band(t1);
                                B(e, (p0 + p1) / 2, new Vector3(0.035f, (p1 - p0).magnitude * 1.05f, 0.025f), band).transform.localRotation = Quaternion.FromToRotation(Vector3.up, p1 - p0);
                            }
                            for (int k = 0; k < 8; k++) HwDot(e, Band(-70 + k * 20) * 1.025f, 0.014f, k % 2 == 0 ? G("FF3FA4", 2.4f) : G("35E0FF", 2.4f));
                            HwBall(e, Band(-86), 0.05f, band); HwBall(e, Band(86), 0.05f, band);
                        }
                        break;
                    }
                }
            }
        }

        void Hat(int i, int set)
        {
            if (i >= 200) { RewardHat(i); return; }
            var h = Node("Hat", HeadC, new Vector3(0, hatY, 0));
            // Cap: 6 panels with stitched seams, eyelets, button, stitched brim, back strap with snaps
            void Cap(string col, bool logo)
            {
                var m = M(col, 0.35f); var dk = HwShade(col, 0.72f, 0.3f);
                var thr = logo ? M("8A8F99", 0.3f) : dk;
                var c = new Vector3(0, -0.08f, 0); var r = new Vector3(0.42f, 0.25f, 0.41f);
                S(h, c, r * 2, m);
                var br = Node("Brim", h, new Vector3(0, -0.13f, 0.32f), new Vector3(-8, 0, 0));
                Cy(br, Vector3.zero, new Vector3(0.5f, 0.012f, 0.42f), m);
                Cy(br, new Vector3(0, -0.004f, 0), new Vector3(0.49f, 0.011f, 0.41f), dk);
                foreach (var s in new[] { 0.9f, 0.76f, 0.62f })
                    Dt.Stitches(br, Dt.Circle(new Vector3(0, 0.013f, 0), 0.25f * s, 0.21f * s, 15, 165), 12, 0.02f, 0.006f, thr);
                Ls(h, new Vector3(0, 0.17f, 0), new Vector3(0.06f, 0.045f, 0.06f), m);
                for (int k = 0; k < 6; k++)
                {
                    float ph = 30 + k * 60;
                    Dt.Stitches(h, t => Dt.Ell(c, r, Mathf.Lerp(8, 88, t), ph, 1.01f), 9, 0.022f, 0.007f, thr);
                    var n = Dt.EllN(r, 42, ph - 30);   // eyelet on each panel
                    Lc(h, Dt.Ell(c, r, 42, ph - 30, 1.0f), new Vector3(0.025f, 0.004f, 0.025f), dk).transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
                }
                HwRing(h, c, 0.415f, 0.012f, dk, default, new Vector3(1, 1, 0.41f / 0.415f));
                var bn = Dt.EllN(r, 98, 270); var bp = Dt.Ell(c, r, 98, 270, 1.02f);   // back strap
                HwFlat(h, bp, new Vector3(0.03f, 0.13f, 0.012f), dk, bn, Vector3.right);
                for (int k = 0; k < 5; k++) HwDot(h, bp + new Vector3(-0.05f + k * 0.025f, 0, -0.008f), 0.014f, thr);
                HwFlat(h, bp + new Vector3(0.075f, 0, 0), new Vector3(0.035f, 0.03f, 0.016f), M("C9D2DC", 0.8f, 0.8f), bn, Vector3.up);
                if (logo)
                {
                    Msh(Meshes.Star(), h, new Vector3(0, 0.0f, 0.36f), Vector3.one * 0.13f, M("FFFFFF"), new Vector3(-25, 0, 0));
                    Msh(Meshes.Star(), h, new Vector3(0, 0.0f, 0.352f), Vector3.one * 0.16f, M("D4D8E0", 0.3f), new Vector3(-25, 0, 0));
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 36 * Mathf.Deg2Rad, th = 66 + 13 * Mathf.Sin(a), ph = 90 + 15 * Mathf.Cos(a);
                        HwFlat(h, Dt.Ell(c, r, th, ph, 1.012f), new Vector3(0.005f, 0.018f, 0.004f), thr, Dt.EllN(r, th, ph), new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0));
                    }
                }
                hairHidden = true;
            }
            // Crown: band with rims, spikes with filigree, gems in prong settings; style 0 royal, 1 dragon, 2 king
            void Crown(string gold, string gem, int spikes, float size, Material spikeMat = null, int style = 0)
            {
                var g = M(gold, 0.85f, 0.8f); var gm = G(gem, 1.2f); var pearl = M("FFF8EE", 0.9f);
                float R = 0.28f * size;
                Cy(h, new Vector3(0, -0.02f, 0), new Vector3(0.56f * size, 0.06f, 0.56f * size), g);
                var rimM = style == 1 ? M("FFC83A", 0.85f, 0.8f) : g;
                HwRing(h, new Vector3(0, -0.08f, 0), R, 0.014f, rimM); HwRing(h, new Vector3(0, 0.04f, 0), R, 0.012f, rimM);
                if (style != 1)
                {
                    int n = style == 0 ? 20 : 16;
                    foreach (var y in new[] { -0.08f, 0.04f })
                        for (int k = 0; k < n; k++) { float a = (k + (y > 0 ? 0.5f : 0)) * Mathf.PI * 2 / n; HwDot(h, new Vector3(Mathf.Cos(a) * (R + 0.012f), y, Mathf.Sin(a) * (R + 0.012f)), 0.022f, pearl); }
                }
                else
                {
                    var sc = HwShade(gold, 0.65f, 0.6f, 0.3f);
                    for (int row = 0; row < 2; row++)   // dragon scales on the band
                        for (int k = 0; k < 18; k++)
                        {
                            float a = (k + row * 0.5f) * Mathf.PI * 2 / 18; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            HwFlat(h, d * (R + 0.004f) + Vector3.up * (-0.045f + row * 0.04f), new Vector3(0.05f, 0.045f, 0.014f), sc, d + Vector3.up * 0.3f, Vector3.up, true);
                        }
                }
                var core = G("FFE14D", 2.2f); var ivory = M("FFF4DC", 0.6f);
                for (int k = 0; k < spikes; k++)
                {
                    float a = k * Mathf.PI * 2 / spikes, hgt = 0.2f * size;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var side = new Vector3(-d.z, 0, d.x);
                    var p = d * 0.26f * size;
                    Cone(h, p + Vector3.up * 0.03f, new Vector3(0.12f, hgt, 0.12f), spikeMat ?? g);
                    if (style == 1)
                    {
                        Cone(h, p + Vector3.up * 0.032f + d * 0.012f, new Vector3(0.06f, hgt * 0.7f, 0.06f), core);
                        Lc(h, p + Vector3.up * 0.035f, new Vector3(0.13f, 0.012f, 0.13f), rimM);
                        foreach (var s in new[] { -1f, 1f }) HwSpike(h, p + side * s * 0.05f + Vector3.up * 0.04f, p + side * s * 0.075f + Vector3.up * 0.1f + d * 0.01f, 0.025f, ivory);
                    }
                    else
                    {
                        HwBall(h, p + Vector3.up * (0.04f + hgt), 0.045f, pearl);
                        for (int j = 0; j < 3; j++)
                        {
                            float y = 0.04f + j * hgt * 0.22f;
                            HwDot(h, p + Vector3.up * (0.03f + y) + d * (0.06f * (1 - y / hgt) + 0.004f), 0.016f, gm);
                        }
                        foreach (var s in new[] { -1f, 1f }) HwDot(h, p + Vector3.up * 0.055f + side * s * 0.045f + d * 0.035f, 0.022f, g);
                    }
                    HwGem(h, d * (R + 0.004f) + Vector3.up * -0.02f, 0.06f, gm, rimM, d);
                    float a2 = a + Mathf.PI / spikes; var d2 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
                    if (style == 0) HwGem(h, d2 * (R + 0.004f) + Vector3.up * -0.02f, 0.04f, G("E8F6FF", 1.4f), g, d2);
                    if (style == 1) HwSpike(h, d2 * (R - 0.01f) + Vector3.up * 0.04f, d2 * (R - 0.005f) + Vector3.up * 0.09f, 0.03f, ivory);
                }
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Cap("FF8A1E", false); break;
                case 1: Cap("FFD23A", false); break;
                case 2: Cap("222428", true); break;
                case 3: // Шапка с помпоном: knit V-stitches, ribbed cuff, fluffy pom-pom, label
                {
                    var m = M("E53935", 0.2f); var knit = HwShade("E53935", 0.8f, 0.2f); var w = M("FFFFFF", 0.2f); var wg = M("E6E6EA", 0.2f);
                    var c = new Vector3(0, -0.06f, 0); var r = new Vector3(0.43f, 0.33f, 0.42f);
                    S(h, c, r * 2, m);
                    for (int j = 0; j < 5; j++)
                    {
                        float th = 22 + j * 16; int n = Mathf.RoundToInt(16 * Mathf.Sin(th * Mathf.Deg2Rad));
                        for (int k = 0; k < n; k++)
                        {
                            float ph = k * 360f / n + j * 7;
                            var p = Dt.Ell(c, r, th, ph, 1.0f); var nn = Dt.EllN(r, th, ph);
                            var tm = (Dt.Ell(c, r, th + 2, ph) - Dt.Ell(c, r, th - 2, ph)).normalized; var ts = Vector3.Cross(nn, tm).normalized;
                            foreach (var s in new[] { -1f, 1f })
                                HwFlat(h, p + ts * s * 0.014f, new Vector3(0.026f, 0.06f, 0.016f), knit, nn, tm * 0.93f + ts * s * 0.37f, true);
                        }
                    }
                    Ring(h, new Vector3(0, -0.14f, 0), 0.4f, 0.07f, w, default, new Vector3(1, 1, 0.97f));
                    for (int k = 0; k < 20; k++)   // cuff ribbing
                    {
                        float a = k * 18 * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a) * 0.97f);
                        HwFlat(h, new Vector3(0, -0.14f, 0) + o * 0.466f, new Vector3(0.025f, 0.1f, 0.02f), wg, o, Vector3.up);
                    }
                    var pom = new Vector3(0, 0.33f, 0);
                    S(h, pom, 0.22f, M("FFFFFF", 0.1f));
                    var rng = new Rng(3);
                    for (int k = 0; k < 24; k++)   // pom-pom fluff
                    {
                        var d = new Vector3(rng.F(-1, 1), rng.F(-0.4f, 1), rng.F(-1, 1)).normalized;
                        HwBall(h, pom + d * 0.1f, rng.F(0.06f, 0.08f), k % 3 == 0 ? wg : w);
                    }
                    var lp = new Vector3(0, -0.13f, -0.475f);   // woven label at the back
                    B(h, lp, new Vector3(0.07f, 0.045f, 0.008f), M("333A8C", 0.3f));
                    B(h, lp + new Vector3(0, 0.012f, -0.005f), new Vector3(0.05f, 0.006f, 0.003f), w);
                    B(h, lp + new Vector3(0, -0.008f, -0.005f), new Vector3(0.04f, 0.006f, 0.003f), w);
                    hairHidden = true;
                    break;
                }
                case 4: Crown("FFC83A", "E53935", 5, 1f); break;
                case 5: // Пиратская треуголка: flaps with gold braid and stitches, skull and crossbones, feathers
                {
                    var m = M("1E1E22", 0.3f); var gold = M("FFC83A", 0.8f, 0.8f); var thr = M("4A4A55", 0.3f); var bone = M("FFFFFF", 0.4f); var blk = M("111111", 0.2f);
                    Cy(h, new Vector3(0, -0.05f, 0), new Vector3(1.0f, 0.02f, 0.9f), m);
                    S(h, new Vector3(0, 0.02f, 0), new Vector3(0.62f, 0.36f, 0.58f), m);
                    for (int k = 0; k < 3; k++)
                    {
                        float a = 90 + k * 120;
                        var d = Quaternion.Euler(0, -a, 0) * Vector3.right;
                        var fl = Node("Flap", h, d * 0.38f + Vector3.up * 0.04f, new Vector3(-10, -a + 90, 0));
                        B(fl, Vector3.zero, new Vector3(0.5f, 0.16f, 0.03f), m);
                        for (int j = 0; j < 12; j++) HwDot(fl, new Vector3(-0.24f + j * 0.0436f, 0.08f, 0.012f), 0.024f, gold);
                        for (int j = 0; j < 8; j++) B(fl, new Vector3(-0.21f + j * 0.06f, 0.052f, 0.016f), new Vector3(0.026f, 0.005f, 0.004f), thr);
                        foreach (var s in new[] { -1f, 1f }) HwDot(fl, new Vector3(s * 0.215f, 0.0f, 0.017f), 0.026f, gold);
                        HwRod(fl, new Vector3(0.25f, 0.06f, 0), new Vector3(0.26f, -0.04f, 0.01f), 0.008f, gold);
                        HwBall(fl, new Vector3(0.26f, -0.06f, 0.01f), new Vector3(0.03f, 0.045f, 0.03f), gold);
                        if (k == 0)   // skull and crossbones
                        {
                            foreach (var s in new[] { -1f, 1f })
                            {
                                HwRod(fl, new Vector3(-0.065f * s, -0.055f, 0.022f), new Vector3(0.065f * s, 0.045f, 0.022f), 0.014f, bone);
                                HwDot(fl, new Vector3(-0.07f * s, -0.06f, 0.022f), 0.024f, bone);
                                HwDot(fl, new Vector3(0.07f * s, 0.05f, 0.022f), 0.024f, bone);
                                HwDot(fl, new Vector3(s * 0.018f, 0.008f, 0.052f), 0.022f, blk);
                            }
                            Ls(fl, new Vector3(0, 0.0f, 0.032f), new Vector3(0.08f, 0.075f, 0.04f), bone);
                            B(fl, new Vector3(0, -0.038f, 0.032f), new Vector3(0.045f, 0.025f, 0.03f), bone);
                            HwDot(fl, new Vector3(0, -0.016f, 0.052f), 0.012f, blk);
                        }
                    }
                    Ring(h, new Vector3(0, -0.02f, 0), 0.47f, 0.015f, gold, default, new Vector3(1, 1, 0.9f));
                    B(h, new Vector3(0, 0.195f, -0.02f), new Vector3(0.012f, 0.012f, 0.24f), HwShade("1E1E22", 0.5f), new Vector3(-6, 0, 0));   // crease
                    for (int k = 0; k < 3; k++)   // stitched seams over the crown
                    {
                        float ph = 30 + k * 120;
                        Dt.Stitches(h, t => Dt.Ell(new Vector3(0, 0.02f, 0), new Vector3(0.31f, 0.18f, 0.29f), Mathf.Lerp(20, 70, t), ph, 1.02f), 5, 0.022f, 0.006f, thr);
                    }
                    var fd = new Vector3(0.45f, 1f, -0.55f).normalized;
                    Dt.Feather(h, new Vector3(0.18f, 0.12f, -0.12f), fd, Vector3.Cross(fd, Vector3.up), 0.36f, M("E53935", 0.3f), bone);
                    var fd2 = new Vector3(0.7f, 0.8f, -0.3f).normalized;
                    Dt.Feather(h, new Vector3(0.2f, 0.1f, -0.08f), fd2, Vector3.Cross(fd2, Vector3.up), 0.26f, bone, gold);
                    hairHidden = true;
                    break;
                }
                case 6: // Шляпа волшебника: piped brim, stitches, glowing spiral, stars and sparkles, buckled band
                {
                    var m = M("2E4BD9", 0.3f); var gold = M("FFC83A", 0.8f, 0.7f); var starM = G("FFE14D", 1.8f); var dot = G("BFE8FF", 1.6f);
                    hairHidden = true;
                    Cy(h, new Vector3(0, -0.06f, 0), new Vector3(0.98f, 0.012f, 0.98f), m);
                    HwRing(h, new Vector3(0, -0.06f, 0), 0.49f, 0.012f, gold);
                    Dt.Stitches(h, Dt.Circle(new Vector3(0, -0.046f, 0), 0.42f, 0.42f), 24, 0.026f, 0.006f, M("8FA8FF", 0.3f));
                    var cone = Node("Cone", h, new Vector3(0, -0.06f, 0), new Vector3(-12, 0, 0));
                    Cone(cone, Vector3.zero, new Vector3(0.6f, 0.8f, 0.6f), m);
                    Ring(cone, new Vector3(0, 0.05f, 0), 0.28f, 0.03f, gold);
                    for (int k = 0; k < 4; k++)
                        Msh(Meshes.Star(), cone, new Vector3(Mathf.Sin(k * 1.7f) * 0.13f, 0.18f + k * 0.12f, 0.22f - k * 0.05f), Vector3.one * (0.1f - k * 0.012f), starM, new Vector3(-15, 0, k * 20));
                    Vector3 On(float y, float a, float grow) { float rr = 0.3f * (1 - y / 0.8f) + grow; return new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr); }
                    Quaternion Face(float a) => Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0.375f, Mathf.Sin(a)).normalized);
                    for (int k = 0; k < 30; k++) { float t = k / 30f; HwDot(cone, On(0.12f + t * 0.6f, t * Mathf.PI * 7 - 1.2f, 0.008f), 0.02f, dot); }
                    for (int k = 0; k < 8; k++)
                    {
                        float a = (k * 45 + 200) * Mathf.Deg2Rad, y = 0.14f + (k % 4) * 0.12f;
                        Msh(Meshes.Star(), cone, On(y, a, 0.006f), Vector3.one * 0.06f, starM).transform.localRotation = Face(a);
                        float a2 = (k * 45 + 20) * Mathf.Deg2Rad;
                        Msh(Meshes.Star(4), cone, On(0.1f + k * 0.07f, a2, 0.008f), Vector3.one * 0.04f, G("FFFFFF", 2f)).transform.localRotation = Face(a2);
                    }
                    var bf = On(0.05f, Mathf.PI / 2, 0.035f);   // buckle on the band
                    B(cone, bf + new Vector3(0, 0.03f, 0), new Vector3(0.08f, 0.012f, 0.01f), gold);
                    B(cone, bf - new Vector3(0, 0.03f, 0), new Vector3(0.08f, 0.012f, 0.01f), gold);
                    B(cone, bf + new Vector3(0.035f, 0, 0), new Vector3(0.012f, 0.07f, 0.01f), gold);
                    B(cone, bf - new Vector3(0.035f, 0, 0), new Vector3(0.012f, 0.07f, 0.01f), gold);
                    B(cone, bf + new Vector3(0, 0, 0.004f), new Vector3(0.008f, 0.06f, 0.008f), M("C9D2DC", 0.8f, 0.8f));
                    for (int k = 0; k < 12; k++) { float a = (k * 30 + 105) * Mathf.Deg2Rad; HwDot(cone, On(0.05f, a, 0.03f), 0.018f, gold); }
                    foreach (var y in new[] { 0.0f, 0.1f })
                        for (int k = 0; k < 8; k++)
                        {
                            float a = (k * 45 + 22) * Mathf.Deg2Rad;
                            var go = B(cone, On(y, a, 0.004f), new Vector3(0.026f, 0.005f, 0.004f), gold);
                            go.transform.localRotation = Face(a);
                        }
                    Msh(Meshes.Star(), cone, new Vector3(0, 0.82f, 0), Vector3.one * 0.08f, starM);
                    HwDot(cone, new Vector3(0, 0.79f, 0), 0.03f, gold);
                    break;
                }
                case 7: // Каска строителя: curved ridges with grips, vents, badge, headlamp, reflective band, rivets
                {
                    var m = M("FFC400", 0.75f); var dk = HwShade("FFC400", 0.75f, 0.6f); var dark = M("2A2E36", 0.4f);
                    var c = new Vector3(0, -0.1f, 0); var r = new Vector3(0.45f, 0.31f, 0.44f);
                    S(h, c, r * 2, m);
                    Cy(h, new Vector3(0, -0.15f, 0.04f), new Vector3(1.0f, 0.015f, 1.04f), m);
                    HwRing(h, new Vector3(0, -0.15f, 0.04f), 0.5f, 0.012f, m, default, new Vector3(1, 1, 1.04f));
                    Vector3 Mid(float a, float g) => Dt.Ell(c, r, Mathf.Abs(a), a < 0 ? 90 : 270, g);
                    Vector3 MidN(float a) => Dt.EllN(r, Mathf.Abs(a), a < 0 ? 90 : 270);
                    for (int k = 0; k < 9; k++)   // centre ridge
                    {
                        float a0 = -66 + k * 14.7f, a1 = a0 + 14.7f, am = (a0 + a1) / 2;
                        var p0 = Mid(a0, 1.04f); var p1 = Mid(a1, 1.04f);
                        HwFlat(h, (p0 + p1) / 2, new Vector3(0.07f, (p1 - p0).magnitude * 1.1f, 0.05f), m, MidN(am), p1 - p0);
                    }
                    for (int k = 0; k < 10; k++) { float a = -60 + k * 13.3f; HwFlat(h, Mid(a, 1.125f), new Vector3(0.08f, 0.012f, 0.012f), dk, MidN(a), Vector3.Cross(MidN(a), Vector3.right)); }
                    foreach (var s in new[] { -1f, 1f })   // side ridges
                        for (int k = 0; k < 7; k++)
                        {
                            Vector3 P(float a) { float x = 0.13f * s, z = 0.4f * Mathf.Sin(a * Mathf.Deg2Rad); float q = 1 - x * x / (r.x * r.x) - z * z / (r.z * r.z); return new Vector3(x, c.y + r.y * Mathf.Sqrt(Mathf.Max(0, q)) + 0.012f, z); }
                            var p0 = P(-60 + k * 17.1f); var p1 = P(-60 + (k + 1) * 17.1f);
                            var n = Vector3.Cross(p1 - p0, Vector3.right * s).normalized * s;
                            HwFlat(h, (p0 + p1) / 2, new Vector3(0.035f, (p1 - p0).magnitude * 1.1f, 0.03f), m, n.y < 0 ? -n : n, p1 - p0);
                        }
                    foreach (var s in new[] { -1f, 1f })   // vents
                        for (int k = 0; k < 3; k++)
                        {
                            float th = 30 + k * 9, ph = 270 + s * 55;
                            HwFlat(h, Dt.Ell(c, r, th, ph, 1.005f), new Vector3(0.05f, 0.014f, 0.01f), dark, Dt.EllN(r, th, ph), Vector3.Cross(Dt.EllN(r, th, ph), Vector3.up));
                        }
                    var bn = Dt.EllN(r, 64, 90); var bp = Dt.Ell(c, r, 64, 90, 1.01f);   // badge with hazard stripes
                    HwFlat(h, bp, new Vector3(0.16f, 0.09f, 0.012f), M("FFFFFF", 0.5f), bn, Vector3.up);
                    for (int k = 0; k < 3; k++)
                    {
                        var go = HwFlat(h, bp + bn * 0.007f + Vector3.right * (-0.045f + k * 0.045f), new Vector3(0.018f, 0.075f, 0.004f), dark, bn, Vector3.up);
                        go.transform.localRotation *= Quaternion.Euler(0, 0, 35);
                    }
                    var ln = Dt.EllN(r, 40, 90); var lp = Dt.Ell(c, r, 40, 90, 1.0f);   // headlamp
                    var lq = Quaternion.FromToRotation(Vector3.up, ln);
                    Lc(h, lp + ln * 0.02f, new Vector3(0.1f, 0.025f, 0.1f), dark).transform.localRotation = lq;
                    Ls(h, lp + ln * 0.045f, new Vector3(0.08f, 0.03f, 0.08f), G("FFF6C8", 2f)).transform.localRotation = lq;
                    HwRing(h, lp + ln * 0.045f, 0.045f, 0.008f, M("C9D2DC", 0.85f, 0.8f)).transform.localRotation = lq;
                    for (int k = 0; k < 4; k++) HwDot(h, lp + ln * 0.035f + lq * (Quaternion.Euler(0, k * 90 + 45, 0) * Vector3.right * 0.055f), 0.012f, M("C9D2DC", 0.85f, 0.8f));
                    for (int k = 0; k < 20; k++)   // reflective band
                    {
                        float ph = k * 18; var n = Dt.EllN(r, 80, ph);
                        HwFlat(h, Dt.Ell(c, r, 80, ph, 1.006f), new Vector3(0.035f, 0.115f, 0.008f), k % 2 == 0 ? M("DDE3EA", 0.9f, 0.6f) : M("FF6A00", 0.5f), n, new Vector3(-Mathf.Sin(ph * Mathf.Deg2Rad), 0, Mathf.Cos(ph * Mathf.Deg2Rad)));
                    }
                    for (int k = 0; k < 26; k++) HwDot(h, Dt.Ell(c, r, 95, k * 360f / 26, 1.0f), 0.022f, dk);
                    foreach (var s in new[] { -1f, 1f })   // accessory clips
                    {
                        var cp = Dt.Ell(c, r, 88, 90 - s * 90, 1.01f);
                        B(h, cp, new Vector3(0.02f, 0.05f, 0.06f), dark);
                        HwDot(h, cp + new Vector3(s * 0.012f, 0, 0.018f), 0.014f, dk);
                        HwDot(h, cp + new Vector3(s * 0.012f, 0, -0.018f), 0.014f, dk);
                    }
                    var xp = Dt.Ell(c, r, 55, 160, 1.008f); var xn = Dt.EllN(r, 55, 160);   // green safety cross
                    HwFlat(h, xp, new Vector3(0.06f, 0.02f, 0.006f), M("2EBF4F", 0.4f), xn, Vector3.up);
                    HwFlat(h, xp, new Vector3(0.02f, 0.06f, 0.006f), M("2EBF4F", 0.4f), xn, Vector3.up);
                    var wp = Dt.Ell(c, r, 62, 270, 1.008f); var wn = Dt.EllN(r, 62, 270);   // warning plate on the back
                    HwFlat(h, wp, new Vector3(0.14f, 0.05f, 0.006f), m, wn, Vector3.up);
                    for (int k = 0; k < 3; k++) HwFlat(h, wp + wn * 0.005f + Vector3.right * (-0.045f + k * 0.045f), new Vector3(0.016f, 0.05f, 0.004f), dark, wn, new Vector3(0.6f, 1, 0));
                    hairHidden = true;
                    break;
                }
                // ---- premium ----
                case 100: // Драконья корона: scaled band, flame spikes with cores and claws, dragon eye, side fins
                {
                    Crown("C62828", "FFC400", 6, 1.05f, G("FF6A00", 2f), 1);
                    float R = 0.28f * 1.05f; var ivory = M("FFF4DC", 0.6f);
                    HwGem(h, new Vector3(0, -0.02f, R + 0.01f), 0.07f, G("FFB300", 1.6f), M("FFC83A", 0.85f, 0.8f), Vector3.forward);
                    B(h, new Vector3(0, -0.012f, R + 0.044f), new Vector3(0.012f, 0.045f, 0.006f), M("111111", 0.6f));
                    foreach (var s in new[] { -1f, 1f })
                    {
                        HwSpike(h, new Vector3(s * 0.035f, 0.02f, R + 0.01f), new Vector3(s * 0.07f, 0.055f, R + 0.025f), 0.025f, ivory);
                        var fin = Node("Fin", h, new Vector3(s * (R + 0.01f), 0.0f, -0.06f), new Vector3(0, 0, -s * 25));
                        B(fin, new Vector3(s * 0.01f, 0.05f, 0), new Vector3(0.012f, 0.1f, 0.14f), HwShade("C62828", 0.7f, 0.5f));
                        for (int k = 0; k < 3; k++) HwRod(fin, new Vector3(s * 0.012f, 0.0f, -0.05f + k * 0.05f), new Vector3(s * 0.016f, 0.11f - Mathf.Abs(k - 1) * 0.02f, -0.06f + k * 0.06f), 0.012f, ivory);
                    }
                    break;
                }
                case 101: // Ледяная корона: frosted rims, ice spikes with glowing cores and shards, icicles, snowflakes
                {
                    var ice = Glass("9FE8FF", 0.6f); var frost = M("DDF6FF", 0.9f, 0.3f); var white = G("FFFFFF", 1.5f); var coreG = G("BFF3FF", 2f); var tile = Glass("E6FBFF", 0.5f);
                    Cy(h, new Vector3(0, -0.02f, 0), new Vector3(0.58f, 0.05f, 0.58f), frost);
                    HwRing(h, new Vector3(0, -0.07f, 0), 0.29f, 0.012f, frost); HwRing(h, new Vector3(0, 0.03f, 0), 0.29f, 0.012f, frost);
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k * Mathf.PI * 2 / 7, hgt = 0.18f + (k % 2) * 0.12f;
                        var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var side = new Vector3(-d.z, 0, d.x);
                        var p = d * 0.26f;
                        Cone(h, p, new Vector3(0.1f, hgt, 0.1f), ice);
                        Cone(h, p + Vector3.up * 0.005f, new Vector3(0.04f, hgt * 0.75f, 0.04f), coreG);
                        foreach (var s in new[] { -1f, 1f }) HwSpike(h, p + side * s * 0.04f + Vector3.up * 0.01f, p + side * s * 0.08f + Vector3.up * hgt * 0.5f + d * 0.01f, 0.04f, ice);
                        Msh(Meshes.Star(4), h, p + Vector3.up * (hgt + 0.02f), Vector3.one * 0.045f, white).transform.localRotation = Quaternion.LookRotation(d);
                        float a2 = a + Mathf.PI / 7; var d2 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
                        Msh(Meshes.Star(6), h, d2 * 0.296f + Vector3.up * -0.02f, Vector3.one * 0.05f, white).transform.localRotation = Quaternion.LookRotation(d2);
                    }
                    var rng = new Rng(101);
                    for (int k = 0; k < 14; k++)   // frost crystals and ice tiles on the band
                    {
                        float a = rng.F(0, Mathf.PI * 2), y = rng.F(-0.06f, 0.02f);
                        HwDot(h, new Vector3(Mathf.Cos(a) * 0.293f, y, Mathf.Sin(a) * 0.293f), 0.015f, white);
                        float b = (k + 0.5f) * Mathf.PI * 2 / 14; var db = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                        HwFlat(h, db * 0.292f + Vector3.up * -0.02f, new Vector3(0.06f, 0.04f, 0.008f), tile, db, Vector3.up);
                    }
                    for (int k = 0; k < 26; k++)   // icicles
                    {
                        float a = k * Mathf.PI * 2 / (k < 16 ? 16 : 10) + (k < 16 ? 0 : 0.2f); var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        float len = k < 16 ? 0.04f + (k % 3) * 0.025f : 0.03f;
                        var top = d * (k < 16 ? 0.285f : 0.27f) + Vector3.up * -0.072f;
                        HwSpike(h, top, top + Vector3.down * len, k < 16 ? 0.03f : 0.02f, ice);
                    }
                    var gem = Msh(Meshes.Star(6), h, new Vector3(0, 0.08f, 0.3f), Vector3.one * 0.12f, G("BFF3FF", 2f)); Add<Spin>(gem).axis = Vector3.forward;
                    HwRing(h, new Vector3(0, 0.08f, 0.29f), 0.07f, 0.01f, frost, new Vector3(90, 0, 0));
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; HwDot(h, new Vector3(Mathf.Cos(a) * 0.07f, 0.08f + Mathf.Sin(a) * 0.07f, 0.3f), 0.016f, white); }
                    break;
                }
                case 102: // Шлем космонавта: bubble, sealed neck ring with bolts and latches, frame band, comm pods, lamps, antenna
                {
                    var shell = M("F2F4F7", 0.6f); var metal = M("B9C3CF", 0.8f, 0.6f); var dark = M("2A2E36", 0.4f);
                    S(HeadC, Vector3.zero, Vector3.one * 1.08f, Glass("CFEFFF", 0.22f));
                    Ring(HeadC, new Vector3(0, -0.42f, 0), 0.34f, 0.08f, shell);
                    HwRing(HeadC, new Vector3(0, -0.345f, 0), 0.33f, 0.02f, M("2E7DFF", 0.5f));
                    for (int k = 0; k < 16; k++) { float a = k * 22.5f * Mathf.Deg2Rad; HwDot(HeadC, new Vector3(Mathf.Cos(a) * 0.4f, -0.37f, Mathf.Sin(a) * 0.4f), 0.022f, metal); }
                    foreach (var s in new[] { -1f, 1f })
                    {
                        B(HeadC, new Vector3(s * 0.425f, -0.42f, 0), new Vector3(0.03f, 0.07f, 0.05f), metal);   // latch
                        HwDot(HeadC, new Vector3(s * 0.442f, -0.4f, 0), 0.018f, dark);
                        B(HeadC, new Vector3(s * 0.445f, -0.44f, 0.0f), new Vector3(0.012f, 0.05f, 0.025f), dark, new Vector3(0, 0, s * 12));
                        var hp = new Vector3(s * 0.3f, -0.45f, -0.28f); var ho = new Vector3(s * 0.7f, 0, -0.7f);   // hose connector
                        var hq = Quaternion.FromToRotation(Vector3.up, ho);
                        Lc(HeadC, hp, new Vector3(0.07f, 0.03f, 0.07f), metal).transform.localRotation = hq;
                        HwRing(HeadC, hp + ho * 0.03f, 0.035f, 0.01f, dark).transform.localRotation = hq;
                        B(HeadC, hp + ho * 0.02f + Vector3.up * 0.04f, new Vector3(0.02f, 0.02f, 0.02f), dark);
                        B(HeadC, hp + ho * 0.02f + Vector3.down * 0.04f, new Vector3(0.02f, 0.02f, 0.02f), dark);
                        var pod = new Vector3(s * 0.56f, 0, 0);   // comm pod
                        Ls(HeadC, pod, new Vector3(0.08f, 0.16f, 0.16f), shell);
                        HwRing(HeadC, pod, 0.075f, 0.01f, metal, new Vector3(0, 0, 90));
                        for (int k = 0; k < 3; k++) B(HeadC, pod + new Vector3(s * 0.04f, -0.035f + k * 0.035f, 0), new Vector3(0.006f, 0.012f, 0.08f), dark);
                        var lp = new Vector3(Mathf.Sin(s * 40 * Mathf.Deg2Rad) * 0.565f, Mathf.Cos(40 * Mathf.Deg2Rad) * 0.565f, 0.05f);   // lamp
                        Lc(HeadC, lp, new Vector3(0.07f, 0.03f, 0.07f), dark, new Vector3(90, 0, 0));
                        Ls(HeadC, lp + Vector3.forward * 0.032f, new Vector3(0.055f, 0.055f, 0.02f), G("FFF6C8", 2f));
                        HwRing(HeadC, lp + Vector3.forward * 0.03f, 0.035f, 0.007f, metal, new Vector3(90, 0, 0));
                        for (int k = 0; k < 3; k++) { float a = (k * 120 + 90) * Mathf.Deg2Rad; HwDot(HeadC, lp + new Vector3(Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, 0.0f), 0.01f, metal); }
                    }
                    for (int k = 0; k < 16; k++)   // frame band over the top
                    {
                        float a0 = (-84 + k * 10.5f) * Mathf.Deg2Rad, a1 = a0 + 10.5f * Mathf.Deg2Rad;
                        var p0 = new Vector3(Mathf.Sin(a0), Mathf.Cos(a0), 0) * 0.56f; var p1 = new Vector3(Mathf.Sin(a1), Mathf.Cos(a1), 0) * 0.56f;
                        var n = ((p0 + p1) / 2).normalized;
                        HwFlat(HeadC, (p0 + p1) / 2, new Vector3(0.07f, (p1 - p0).magnitude * 1.08f, 0.025f), shell, n, p1 - p0);
                        HwDot(HeadC, (p0 + p1) / 2 + n * 0.015f, 0.014f, metal);
                    }
                    HwRod(HeadC, new Vector3(-0.58f, 0.07f, 0), new Vector3(-0.62f, 0.3f, -0.03f), 0.012f, metal);   // antenna
                    HwDot(HeadC, new Vector3(-0.62f, 0.31f, -0.03f), 0.03f, G("FF3B3B", 2f));
                    Lc(HeadC, new Vector3(-0.585f, 0.075f, 0), new Vector3(0.03f, 0.01f, 0.03f), dark);
                    var cq = Quaternion.Euler(0, 40, 0); var cp = new Vector3(0.28f, -0.36f, 0.2f);   // chest-side control box
                    B(HeadC, cp, new Vector3(0.12f, 0.08f, 0.04f), G("35E0FF", 1.5f), new Vector3(0, 40, 0));
                    B(HeadC, cp + cq * new Vector3(0, -0.02f, -0.015f), new Vector3(0.18f, 0.14f, 0.03f), shell, new Vector3(0, 40, 0));
                    string[] bc = { "FF3B3B", "FFD23A", "3BFF7A", "35E0FF", "FF3FA4", "FFFFFF" };
                    for (int k = 0; k < 6; k++) HwDot(HeadC, cp + cq * new Vector3(-0.065f + k * 0.026f, -0.065f, 0.005f), 0.018f, G(bc[k], 1.6f));
                    for (int k = 0; k < 8; k++)   // vents on the neck ring
                    {
                        float a = (k < 4 ? 112 + k * 14 : 250 + (k - 4) * 14) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        HwFlat(HeadC, o * 0.42f + Vector3.up * -0.42f, new Vector3(0.012f, 0.05f, 0.008f), dark, o, Vector3.up);
                    }
                    var refl = Add("FFFFFF", 0.5f);   // reflections on the glass
                    for (int k = 0; k < 3; k++)
                    {
                        float th = 32 + k * 9, ph = 118 + k * 4;
                        HwFlat(HeadC, Dt.Ell(Vector3.zero, Vector3.one * 0.545f, th, ph), new Vector3(0.012f, 0.11f - k * 0.03f, 0.004f), refl, Dt.EllN(Vector3.one, th, ph), Vector3.up);
                    }
                    HwRod(HeadC, new Vector3(0.48f, -0.06f, 0.1f), new Vector3(0.14f, -0.16f, 0.38f), 0.01f, dark);   // mic boom
                    HwDot(HeadC, new Vector3(0.12f, -0.165f, 0.39f), 0.03f, dark);
                    break;
                }
                case 104: // Кибер-кабуто: ribbed dome, neon kuwagata, layered shikoro with glowing edges, fukigaeshi, emblem
                {
                    var dark = M("1E2230", 0.8f, 0.6f); var plate = M("2A3044", 0.7f, 0.5f); var neon = G("26F0FF", 2.2f); var red = G("FF2E63", 2f);
                    var c = new Vector3(0, -0.08f, 0); var r = new Vector3(0.44f, 0.28f, 0.43f);
                    S(h, c, r * 2, dark);
                    Cy(h, new Vector3(0, -0.17f, 0), new Vector3(0.98f, 0.02f, 0.98f), dark);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var hn = Node("Kuwagata", h, new Vector3(sd * 0.16f, 0.2f, 0.28f), new Vector3(-20, 0, -sd * 30));
                        B(hn, Vector3.zero, new Vector3(0.05f, 0.34f, 0.03f), neon);
                        for (int k = 0; k < 4; k++) B(hn, new Vector3(0, -0.12f + k * 0.08f, 0), new Vector3(0.06f, 0.012f, 0.036f), dark);
                        Ls(hn, new Vector3(0, -0.17f, 0), new Vector3(0.07f, 0.04f, 0.05f), plate);
                    }
                    Msh(Meshes.Star(6, 0.85f), h, new Vector3(0, 0.0f, 0.42f), Vector3.one * 0.1f, neon, new Vector3(-15, 0, 0));
                    HwRing(h, new Vector3(0, 0.0f, 0.415f), 0.06f, 0.01f, plate, new Vector3(75, 0, 0));
                    HwDot(h, new Vector3(0, 0.0f, 0.45f), 0.03f, red);
                    for (int m = 0; m < 10; m++)   // dome ribs
                    {
                        float ph = m * 36 + 18;
                        for (int k = 0; k < 5; k++)
                        {
                            float t0 = 10 + k * 15, t1 = t0 + 15, tm = (t0 + t1) / 2;
                            var p0 = Dt.Ell(c, r, t0, ph, 1.02f); var p1 = Dt.Ell(c, r, t1, ph, 1.02f);
                            HwFlat(h, (p0 + p1) / 2, new Vector3(0.02f, (p1 - p0).magnitude * 1.05f, 0.018f), plate, Dt.EllN(r, tm, ph), p1 - p0);
                        }
                    }
                    Lc(h, new Vector3(0, 0.2f, 0), new Vector3(0.08f, 0.015f, 0.08f), plate);
                    HwDot(h, new Vector3(0, 0.215f, 0), 0.035f, neon);
                    for (int j = 0; j < 3; j++)   // shikoro: neck guard tiers
                        for (int k = 0; k < 9; k++)
                        {
                            float ph = (160 + k * 24.4f) * Mathf.Deg2Rad, rad = 0.47f + j * 0.03f;
                            var o = new Vector3(Mathf.Cos(ph), 0, Mathf.Sin(ph)); var n = (o + Vector3.up * 0.5f).normalized;
                            var p = o * rad + Vector3.up * (-0.2f - j * 0.06f);
                            HwFlat(h, p, new Vector3(rad * 0.45f, 0.065f, 0.015f), plate, n, Vector3.up);
                            HwFlat(h, p - Vector3.up * 0.03f + o * 0.012f, new Vector3(rad * 0.42f, 0.008f, 0.01f), neon, n, Vector3.up);
                        }
                    foreach (var s in new[] { -1f, 1f })   // fukigaeshi: side flaps turned outward
                    {
                        float ph = (90 - s * 62) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(ph), 0, Mathf.Sin(ph)); var n = (o + Vector3.up * 0.3f + Vector3.forward * 0.4f).normalized;
                        var p = o * 0.5f + Vector3.up * -0.15f;
                        HwFlat(h, p, new Vector3(0.12f, 0.1f, 0.015f), plate, n, Vector3.up);
                        HwFlat(h, p + Vector3.up * 0.05f + n * 0.008f, new Vector3(0.12f, 0.01f, 0.01f), neon, n, Vector3.up);
                        for (int k = 0; k < 3; k++) HwDot(h, p + n * 0.01f + Vector3.Cross(Vector3.up, n).normalized * (-0.04f + k * 0.04f), 0.014f, red);
                    }
                    B(h, new Vector3(0, -0.17f, 0.49f), new Vector3(0.3f, 0.012f, 0.012f), neon);
                    foreach (var s in new[] { -1f, 1f }) B(h, new Vector3(s * 0.2f, -0.155f, 0.42f), new Vector3(0.06f, 0.006f, 0.02f), plate, new Vector3(0, s * 20, 0));
                    hairHidden = true;
                    break;
                }
                case 105: // Золотой цилиндр: rolled brim, fluted crown, diamond band with a bow and buckle, coins, beads
                {
                    var g = M("FFC83A", 0.9f, 0.85f); var gl = M("FFE08A", 0.9f, 0.85f); var dg = HwShade("FFC83A", 0.7f, 0.8f, 0.85f);
                    var black = M("1A1A1A"); var diamond = G("E8F6FF", 1.4f);
                    Cy(h, new Vector3(0, -0.04f, 0), new Vector3(0.78f, 0.012f, 0.78f), g);
                    Cy(h, new Vector3(0, 0.16f, 0), new Vector3(0.48f, 0.2f, 0.48f), g);
                    Cy(h, new Vector3(0, 0.0f, 0), new Vector3(0.49f, 0.04f, 0.49f), black);
                    HwRing(h, new Vector3(0, -0.04f, 0), 0.39f, 0.014f, g);
                    Cy(h, new Vector3(0, -0.05f, 0), new Vector3(0.77f, 0.008f, 0.77f), dg);
                    HwRing(h, new Vector3(0, 0.36f, 0), 0.24f, 0.01f, gl);
                    Msh(Meshes.Star(4), h, new Vector3(0, 0.362f, 0), Vector3.one * 0.12f, diamond, new Vector3(90, 0, 0));
                    for (int k = 0; k < 16; k++)   // fluted crown
                    {
                        float a = k * 22.5f * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        HwFlat(h, o * 0.242f + Vector3.up * 0.2f, new Vector3(0.012f, 0.3f, 0.006f), gl, o, Vector3.up);
                    }
                    var bw = new Vector3(0.255f, 0.0f, 0);   // bow on the right side
                    Ls(h, bw + new Vector3(0, 0.01f, 0.04f), new Vector3(0.025f, 0.05f, 0.07f), black);
                    Ls(h, bw + new Vector3(0, 0.01f, -0.04f), new Vector3(0.025f, 0.05f, 0.07f), black);
                    HwDot(h, bw + new Vector3(0.01f, 0.01f, 0), 0.035f, black);
                    B(h, bw + new Vector3(0.005f, -0.04f, 0.02f), new Vector3(0.01f, 0.06f, 0.02f), black, new Vector3(15, 0, 0));
                    B(h, bw + new Vector3(0.005f, -0.04f, -0.02f), new Vector3(0.01f, 0.06f, 0.02f), black, new Vector3(-15, 0, 0));
                    for (int k = 0; k < 14; k++)   // diamonds on the band
                    {
                        float ad = k * 360f / 14 + 12;
                        if (Mathf.Abs(Mathf.DeltaAngle(ad, 90)) < 20 || Mathf.Abs(Mathf.DeltaAngle(ad, 0)) < 20) continue;
                        var o = new Vector3(Mathf.Cos(ad * Mathf.Deg2Rad), 0, Mathf.Sin(ad * Mathf.Deg2Rad));
                        HwBall(h, o * 0.252f, new Vector3(0.025f, 0.018f, 0.025f), diamond).transform.localRotation = Quaternion.FromToRotation(Vector3.up, o);
                        HwDot(h, o * 0.247f, 0.03f, g);
                    }
                    HwGem(h, new Vector3(0, 0, 0.25f), 0.06f, diamond, g, Vector3.forward);   // buckle
                    B(h, new Vector3(0, 0.038f, 0.25f), new Vector3(0.1f, 0.012f, 0.012f), g);
                    B(h, new Vector3(0, -0.038f, 0.25f), new Vector3(0.1f, 0.012f, 0.012f), g);
                    B(h, new Vector3(0.05f, 0, 0.25f), new Vector3(0.012f, 0.088f, 0.012f), g);
                    B(h, new Vector3(-0.05f, 0, 0.25f), new Vector3(0.012f, 0.088f, 0.012f), g);
                    for (int k = 0; k < 3; k++)   // coins tucked in the band
                    {
                        float a = (200 + k * 40) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        Lc(h, o * 0.26f + Vector3.up * 0.05f, new Vector3(0.07f, 0.006f, 0.07f), gl).transform.localRotation = Quaternion.FromToRotation(Vector3.up, o);
                        HwFlat(h, o * 0.268f + Vector3.up * 0.05f, new Vector3(0.008f, 0.04f, 0.004f), dg, o, Vector3.up);
                    }
                    for (int k = 0; k < 20; k++)   // bead rows at the top and above the band
                    {
                        float a = k * 18 * Mathf.Deg2Rad;
                        HwDot(h, new Vector3(Mathf.Cos(a) * 0.245f, 0.35f, Mathf.Sin(a) * 0.245f), 0.016f, gl);
                        HwDot(h, new Vector3(Mathf.Cos(a + 0.157f) * 0.248f, 0.05f, Mathf.Sin(a + 0.157f) * 0.248f), 0.016f, gl);
                    }
                    hairHidden = true;
                    break;
                }
                case 106: // Рог единорога: rainbow spiral, flower wreath with leaves, mane tuft, gold rings, sparkles
                {
                    var hn = Node("UHorn", h, new Vector3(0, -0.04f, 0.22f), new Vector3(28, 0, 0));
                    var horn = Cone(hn, Vector3.zero, new Vector3(0.13f, 0.42f, 0.13f), M("FFF4FB", 0.8f));
                    Add<Hue>(horn).sat = 0.3f;
                    var gold = M("FFC83A", 0.8f, 0.8f);
                    Ring(hn, new Vector3(0, 0.02f, 0), 0.07f, 0.02f, gold);
                    HwRing(hn, new Vector3(0, 0.07f, 0), 0.056f, 0.01f, gold);
                    string[] rb = { "FF7AA8", "FFB36B", "FFE66B", "7BEA9B", "6BC8FF", "B58CFF" };
                    var rm = new Material[6]; for (int k = 0; k < 6; k++) rm[k] = M(rb[k], 0.6f);
                    for (int s = 0; s < 2; s++)   // spiral grooves
                        for (int k = 0; k < 16; k++)
                        {
                            float t = k / 16f, y = 0.09f + t * 0.3f, rr = 0.065f * (1 - y / 0.42f) + 0.004f, a = t * Mathf.PI * 4 + s * Mathf.PI;
                            HwDot(hn, new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr), new Vector3(0.022f, 0.012f, 0.022f) * (1 - t * 0.5f), rm[(k + s * 3) % 6]);
                        }
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; HwDot(hn, new Vector3(Mathf.Cos(a) * 0.078f, 0.02f, Mathf.Sin(a) * 0.078f) + Vector3.up * 0.018f, 0.014f, gold); }
                    var yel = M("FFE14D", 0.5f); var leaf = M("5BCB6B", 0.4f);
                    for (int f = 0; f < 6; f++)   // flower wreath
                    {
                        float a = f * 60 * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var fp = o * 0.11f + Vector3.up * 0.01f;
                        for (int p = 0; p < 5; p++)
                        {
                            float b = p * 72 * Mathf.Deg2Rad;
                            var pd = Quaternion.FromToRotation(Vector3.up, (o * 0.4f + Vector3.up).normalized) * new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                            HwDot(hn, fp + pd * 0.02f, new Vector3(0.025f, 0.012f, 0.025f), rm[f]);
                        }
                        HwDot(hn, fp + Vector3.up * 0.006f, 0.016f, yel);
                        float a2 = a + 30 * Mathf.Deg2Rad; var o2 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2));
                        HwFlat(hn, o2 * 0.115f, new Vector3(0.022f, 0.045f, 0.008f), leaf, Vector3.up + o2 * 0.3f, o2, true);
                        HwDot(hn, o2 * 0.09f + Vector3.up * 0.02f, 0.01f, G("FFF3B0", 1.6f));
                    }
                    for (int k = 0; k < 6; k++)
                    {
                        float a = (k * 60 + 30) * Mathf.Deg2Rad;
                        Msh(Meshes.Star(4), hn, new Vector3(Mathf.Cos(a) * 0.13f, 0.1f + k * 0.055f, Mathf.Sin(a) * 0.13f), Vector3.one * 0.035f, G("FFFFFF", 2f));
                    }
                    Msh(Meshes.Star(4), hn, new Vector3(0, 0.45f, 0), Vector3.one * 0.05f, G("FFFFFF", 2.2f));
                    for (int k = 0; k < 8; k++)   // mane tuft swept back over the head
                    {
                        float x = -0.07f + k * 0.02f;
                        HwSeg(h, new Vector3(x, -0.02f, 0.12f), new Vector3(x * 1.4f, 0.0f, -0.06f), 0.06f, rm[k % 6], Vector3.up, 0.45f);
                    }
                    break;
                }
                case 107: // Корона Короля Чубриков: velvet cap, king crown, ermine with spots, arches, orb and spinning star
                {
                    S(h, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.32f, 0.5f), M("9C0D2E", 0.2f));
                    Crown("FFD700", "2E7DFF", 8, 1.15f, null, 2);
                    Ring(h, new Vector3(0, -0.11f, 0), 0.33f, 0.05f, M("FFFFFF", 0.1f));
                    var blk = M("111111", 0.3f);
                    for (int k = 0; k < 12; k++)
                    {
                        float a = (k * 30 + 15) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        HwFlat(h, o * 0.381f + Vector3.up * -0.11f, new Vector3(0.014f, 0.032f, 0.01f), blk, o, Vector3.up);
                    }
                    var g = M("FFD700", 0.85f, 0.8f); float R = 0.28f * 1.15f;
                    for (int k = 0; k < 4; k++)   // arches
                    {
                        float a = (22.5f + k * 90) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        for (int j = 0; j < 5; j++)
                        {
                            float t = (0.1f + j * 0.2f) * Mathf.PI / 2;
                            HwBall(h, o * R * 0.95f * Mathf.Cos(t) + Vector3.up * (0.04f + 0.22f * Mathf.Sin(t)), 0.045f, j % 2 == 0 ? g : M("FFF8EE", 0.9f));
                        }
                    }
                    Ls(h, new Vector3(0, 0.28f, 0), 0.07f, g);
                    HwRing(h, new Vector3(0, 0.28f, 0), 0.036f, 0.007f, g, new Vector3(90, 0, 0));
                    HwDot(h, new Vector3(0, 0.28f, 0.036f), 0.022f, G("2E7DFF", 1.6f));
                    var top = Msh(Meshes.Star(), h, new Vector3(0, 0.37f, 0), Vector3.one * 0.14f, G("FFE14D", 2f)); Add<Spin>(top);
                    break;
                }
            }
            if (set == 2) { hairHidden = true; earsHidden = true; }
        }
    }
}
