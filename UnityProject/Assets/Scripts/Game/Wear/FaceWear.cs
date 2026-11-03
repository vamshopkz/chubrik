using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Glasses / eyes, masks, scarves and neck items. Each item is assembled from 100+ small parts
    /// (screws, hinges, stitches, knit loops, chain links, set gems) that the model bakes afterwards.</summary>
    public partial class ChubrikModel
    {
        // ---------------- small kit for face / neck items (Fw = FaceWear) ----------------
        static readonly int[] FwSd = { -1, 1 };
        const float FwRad = Mathf.Deg2Rad;

        /// <summary>Moves a part built in a local frame (origin c, rotation q) into its parent's space.</summary>
        static GameObject FwAt(GameObject go, Vector3 c, Quaternion q)
        {
            var t = go.transform;
            t.localPosition = c + q * t.localPosition;
            t.localRotation = q * t.localRotation;
            return go;
        }

        /// <summary>Low-poly torus (rims, links, bands): ~100 verts instead of 275.</summary>
        static GameObject FwRing(Transform p, Vector3 pos, float R, float r, Material m, Vector3 euler = default, Vector3? sc = null, int seg = 14)
            => Msh(Meshes.Torus(R, r, seg, seg < 12 ? 5 : 6), p, pos, sc ?? Vector3.one, m, euler);

        /// <summary>Closed loop sampled n times without repeating the first point.</summary>
        static System.Func<float, Vector3> FwLoop(System.Func<float, Vector3> path, int n) => t => path(t * (n - 1f) / n);

        /// <summary>Wire of n rods along a path.</summary>
        static void FwWire(Transform p, System.Func<float, Vector3> path, int n, float d, Material m)
        {
            var a = path(0);
            for (int k = 1; k <= n; k++)
            {
                var b = path(k / (float)n);
                var e = (b - a).normalized * d * 0.3f;
                LRod(p, a - e, b + e, d, m);
                a = b;
            }
        }

        /// <summary>Flat bar from a to b with its face turned towards <paramref name="face"/>.</summary>
        static GameObject FwBar(Transform p, Vector3 a, Vector3 b, float w, float th, Material m, Vector3 face)
        {
            var d = b - a;
            var go = B(p, (a + b) / 2, new Vector3(w, d.magnitude, th), m);
            var f = Vector3.ProjectOnPlane(face, d);
            if (f.sqrMagnitude > 1e-8f && d.sqrMagnitude > 1e-8f) go.transform.localRotation = Quaternion.LookRotation(f, d);
            return go;
        }

        /// <summary>Strap / ribbon of n flat segments along a path, faces pointing away from <paramref name="centre"/>.</summary>
        static void FwBand(Transform p, System.Func<float, Vector3> path, int n, float w, float th, Material m, Vector3 centre)
        {
            var a = path(0);
            for (int k = 1; k <= n; k++)
            {
                var b = path(k / (float)n);
                var e = (b - a) * 0.06f;
                FwBar(p, a - e, b + e, w, th, m, (a + b) / 2 - centre);
                a = b;
            }
        }

        /// <summary>Point pushed onto the default head surface (HeadC space).</summary>
        static Vector3 FwOnHead(Vector3 v, float grow = 1.04f)
        {
            var r = Dt.HeadR;
            float k = Mathf.Sqrt(v.x * v.x / (r.x * r.x) + v.y * v.y / (r.y * r.y) + v.z * v.z / (r.z * r.z));
            return v / Mathf.Max(k, 1e-4f) * grow;
        }

        /// <summary>Outward normal of the default head at a point near its surface.</summary>
        static Vector3 FwHeadN(Vector3 v) { var r = Dt.HeadR; return new Vector3(v.x / (r.x * r.x), v.y / (r.y * r.y), v.z / (r.z * r.z)).normalized; }

        /// <summary>Path over the head surface through the given waypoints.</summary>
        static System.Func<float, Vector3> FwHeadPath(float grow, params Vector3[] pts) => t =>
        {
            float s = Mathf.Clamp01(t) * (pts.Length - 1);
            int i = Mathf.Min((int)s, pts.Length - 2);
            return FwOnHead(Vector3.Slerp(pts[i], pts[i + 1], s - i), grow);
        };

        /// <summary>Depth of the default head's front surface at (x, y) in HeadC space.</summary>
        static float FwFaceZ(float x, float y)
        {
            var r = Dt.HeadR;
            return r.z * Mathf.Sqrt(Mathf.Max(0, 1 - x * x / (r.x * r.x) - y * y / (r.y * r.y)));
        }

        /// <summary>Radii (x, z) of the body (torso or head bottom) at height y of the Neck node.</summary>
        static Vector2 FwNeckR(float y)
        {
            float ty = (y + 0.25f) / 0.3f, hy = (y - 0.33f) / 0.39f;
            float ft = Mathf.Sqrt(Mathf.Max(0, 1 - ty * ty)), fh = Mathf.Sqrt(Mathf.Max(0, 1 - hy * hy));
            return new Vector2(Mathf.Max(0.33f * ft, 0.4f * fh), Mathf.Max(0.29f * ft, 0.38f * fh));
        }

        /// <summary>Four-point twinkle: two crossed thin bars (2 parts).</summary>
        static void FwGlint(Transform p, Vector3 pos, float s, Material m, float rotZ = 0)
        {
            B(p, pos, new Vector3(s * 0.16f, s, s * 0.08f), m, new Vector3(0, 0, rotZ));
            B(p, pos, new Vector3(s, s * 0.16f, s * 0.08f), m, new Vector3(0, 0, rotZ));
        }

        /// <summary>Slotted screw head (2 parts).</summary>
        static void FwScrew(Transform p, Vector3 pos, float d, Material m, Material slot, Vector3 nrm)
        {
            var q = Quaternion.FromToRotation(Vector3.up, nrm.normalized);
            Lc(p, pos, new Vector3(d, d * 0.2f, d), m).transform.localRotation = q;
            B(p, pos + q * Vector3.up * d * 0.2f, new Vector3(d * 0.8f, d * 0.12f, d * 0.16f), slot).transform.localRotation = q * Quaternion.Euler(0, 45, 0);
        }

        /// <summary>Light cut gem: faceted crown, collar and 4 claw prongs (6 parts, ~190 verts).</summary>
        static void FwGem(Transform p, Vector3 pos, float d, Material gem, Material setting, Vector3 nrm)
        {
            var q = Quaternion.FromToRotation(Vector3.up, nrm.normalized);
            Msh(Meshes.Cone(8), p, pos + q * Vector3.up * d * 0.12f, new Vector3(d, d * 0.42f, d), gem).transform.localRotation = q;
            Lc(p, pos, new Vector3(d * 1.15f, d * 0.12f, d * 1.15f), setting).transform.localRotation = q;
            for (int k = 0; k < 4; k++)
            {
                var r = Quaternion.Euler(0, k * 90 + 45, 0);
                B(p, pos + q * (r * new Vector3(d * 0.5f, d * 0.2f, 0)), new Vector3(d * 0.16f, d * 0.4f, d * 0.16f), setting).transform.localRotation = q * r * Quaternion.Euler(0, 0, 20);
            }
        }

        /// <summary>Chain of n links along a path: flat and edge-on links in turn (face = which way "flat" looks).</summary>
        static void FwChain(Transform p, System.Func<float, Vector3> path, int n, float R, float r, Material m, System.Func<Vector3, Vector3> face)
        {
            Dt.Path(path, n, (pos, dir, i) =>
            {
                var f = face(pos);
                var up = i % 2 == 0 ? f : Vector3.Cross(dir, f);
                var go = Msh(Meshes.Torus(R, r, 10, 5), p, pos, new Vector3(1, 1, 1.5f), m);
                if (up.sqrMagnitude > 1e-8f) go.transform.localRotation = Quaternion.LookRotation(dir, up);
            });
        }

        /// <summary>Knit loop "V": two slanted strokes on a surface (2 parts).</summary>
        static void FwKnitV(Transform p, Vector3 pos, Vector3 nrm, Vector3 up, float s, Material m)
        {
            var q = Quaternion.LookRotation(nrm, up);
            foreach (var sd in FwSd)
                B(p, pos + q * new Vector3(sd * s * 0.22f, 0, 0), new Vector3(s * 0.4f, s, s * 0.25f), m).transform.localRotation = q * Quaternion.Euler(0, 0, -sd * 28);
        }

        /// <summary>Fur strand: thin 6-sided cone from a to b (~36 verts).</summary>
        static void FwTuft(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Msh(Meshes.Cone(6), p, a, new Vector3(d, (b - a).magnitude, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        }

        // ================= eyes =================

        void Glasses(int i, int set)
        {
            var g = Node("Glasses", HeadC, new Vector3(0, eyeY, faceZ + 0.01f));
            var gold = M("FFC83A", 0.85f, 0.8f);
            var shine = Add("FFFFFF", 0.6f);
            // temple arm: hinge → over the ear → tip behind the ear (6 parts)
            Vector3 TA(int sd) => new Vector3(sd * 0.25f, 0.03f, -0.02f);
            Vector3 TB(int sd) => new Vector3(sd * 0.37f, 0.045f, -0.2f);
            void Temple(int sd, float d, Material m, Material tip)
            {
                var c = new Vector3(sd * 0.41f, 0.05f, -0.4f);
                var e = new Vector3(sd * 0.39f, -0.03f, -0.5f);
                LRod(g, TA(sd), TB(sd), d, m); LRod(g, TB(sd), c, d, m); LRod(g, c, e, d * 1.3f, tip);
                Ls(g, TB(sd), d * 1.1f, m); Ls(g, c, d * 1.3f, tip); Ls(g, e, d * 1.4f, tip);
            }
            // hinge: end piece, three knuckles, screw head (5 parts)
            void Hinge(Vector3 at, int sd, Material m)
            {
                B(g, at - new Vector3(sd * 0.012f, 0, 0), new Vector3(0.036f, 0.034f, 0.022f), m);
                for (int k = 0; k < 3; k++) Lc(g, at + new Vector3(sd * 0.008f, -0.013f + k * 0.013f, -0.014f), new Vector3(0.02f, 0.0055f, 0.02f), m);
                Ls(g, at + new Vector3(sd * 0.008f, 0.024f, -0.014f), 0.014f, m);
            }
            // lens reflections: two slanted strips and a dot (3 parts), s = lens radius
            void Glints(Vector3 c, float s, Material m)
            {
                B(g, c + new Vector3(-0.3f, 0.35f, 0) * s, new Vector3(0.12f * s, 0.6f * s, 0.004f), m, new Vector3(0, 0, 40));
                B(g, c + new Vector3(-0.05f, 0.3f, 0) * s, new Vector3(0.06f * s, 0.4f * s, 0.004f), m, new Vector3(0, 0, 40));
                Ls(g, c + new Vector3(0.45f, -0.4f, 0) * s, new Vector3(0.12f * s, 0.12f * s, 0.004f), m);
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:   // aviators: teardrop lenses in a wire frame, double bridge, pads, hinges
                {
                    var dark = M("15181E", 0.95f, 0.3f); var tip = M("2A2A2A", 0.3f); var pad = Glass("FFFFFF", 0.5f);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * eyeX, -0.01f, 0);
                        S(g, c + new Vector3(sd * 0.006f, -0.006f, 0), new Vector3(0.21f, 0.18f, 0.04f), dark);
                        FwWire(g, t =>
                        {
                            float a = t * Mathf.PI * 2, s = Mathf.Sin(a);
                            return c + new Vector3(Mathf.Cos(a) * 0.107f + (s < 0 ? -s * sd * 0.012f : 0), s * (s < 0 ? 0.1f : 0.087f), 0.008f);
                        }, 24, 0.011f, gold);
                        Glints(c + new Vector3(0, 0, 0.021f), 0.1f, shine);
                        Ls(g, c + new Vector3(-0.06f, -0.02f, 0.018f), new Vector3(0.008f, 0.008f, 0.004f), shine);
                        var pa = new Vector3(sd * 0.05f, -0.035f, 0.004f); var pp = new Vector3(sd * 0.042f, -0.065f, -0.03f);
                        LRod(g, pa, pp, 0.007f, gold);
                        Ls(g, pp, new Vector3(0.02f, 0.032f, 0.012f), pad);
                        Ls(g, pa, 0.012f, gold);
                        Hinge(new Vector3(sd * 0.245f, 0.035f, -0.004f), sd, gold);
                        Temple(sd, 0.014f, gold, tip);
                        var td = (TB(sd) - TA(sd)).normalized * 0.005f;
                        for (int k = 0; k < 4; k++) { var p0 = Vector3.Lerp(TA(sd), TB(sd), 0.25f + k * 0.16f); LRod(g, p0 - td, p0 + td, 0.021f, gold); }
                        B(g, c + new Vector3(sd * 0.07f, 0.06f, 0.016f), new Vector3(0.022f, 0.007f, 0.004f), gold);
                        Ls(g, new Vector3(sd * 0.064f, 0.046f, 0.01f), 0.017f, gold);
                    }
                    LRod(g, new Vector3(-0.064f, 0.046f, 0.01f), new Vector3(0.064f, 0.046f, 0.01f), 0.014f, gold);
                    FwWire(g, t => new Vector3(Mathf.Lerp(-0.042f, 0.042f, t), 0.016f + Mathf.Sin(t * Mathf.PI) * 0.014f, 0.01f), 6, 0.011f, gold);
                    for (int k = 0; k < 3; k++) Ls(g, new Vector3(-0.03f + k * 0.03f, 0.046f, 0.017f), 0.011f, gold);
                    break;
                }
                case 1:   // horn-rims: tortoiseshell speckles, browline, silver pins, keyhole bridge
                {
                    var brown = M("5A3418", 0.6f); var amber = M("9A5E2A", 0.6f); var spot = M("2E1A0C", 0.6f); var silver = M("D9DEE6", 0.9f, 0.9f);
                    var rng = new Rng(11);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * eyeX, 0, 0);
                        Ring(g, c, 0.085f, 0.02f, brown, new Vector3(90, 0, 0));
                        S(g, c + new Vector3(0, 0, -0.005f), new Vector3(0.16f, 0.16f, 0.01f), Glass("DDEEFF", 0.25f));
                        FwWire(g, t => { float a = Mathf.Lerp(15, 165, t) * FwRad; return c + new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.09f + 0.008f, 0.006f); }, 8, 0.034f, brown);
                        for (int k = 0; k < 20; k++)
                        {
                            float a = (k + rng.F(-0.3f, 0.3f)) / 20f * Mathf.PI * 2, rr = 0.085f + rng.F(-0.008f, 0.008f);
                            Ls(g, c + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0.019f), new Vector3(rng.F(0.014f, 0.024f), rng.F(0.008f, 0.014f), 0.008f),
                                k % 3 == 0 ? amber : spot, new Vector3(0, 0, a * Mathf.Rad2Deg + 90));
                        }
                        FwRing(g, c + new Vector3(0, 0, 0.002f), 0.066f, 0.004f, silver, new Vector3(90, 0, 0));
                        Glints(c + new Vector3(0, 0, 0.003f), 0.075f, shine);
                        var pin = c + new Vector3(sd * 0.078f, 0.05f, 0.021f);
                        Ls(g, pin, new Vector3(0.016f, 0.012f, 0.008f), silver);
                        B(g, pin + new Vector3(sd * 0.016f, 0, 0), new Vector3(0.022f, 0.005f, 0.006f), silver);
                        Ls(g, pin + new Vector3(sd * 0.03f, 0, 0), 0.01f, silver);
                        Hinge(new Vector3(sd * 0.25f, 0.03f, -0.006f), sd, silver);
                        Temple(sd, 0.026f, brown, brown);
                        for (int k = 0; k < 3; k++) Ls(g, Vector3.Lerp(TA(sd), TB(sd), 0.3f + k * 0.22f) + new Vector3(sd * 0.012f, 0.004f, 0), 0.011f, silver);
                        Ls(g, new Vector3(sd * 0.052f, -0.03f, -0.006f), new Vector3(0.02f, 0.042f, 0.022f), brown);
                    }
                    LRod(g, new Vector3(-0.062f, 0.014f, 0.004f), new Vector3(0.062f, 0.014f, 0.004f), 0.028f, brown);
                    foreach (var sd in FwSd) Ls(g, new Vector3(sd * 0.03f, 0.014f, 0.018f), 0.011f, silver);
                    break;
                }
                case 2:   // cyber visor: scan lines, HUD, vented top bar, ear modules, strap round the head
                {
                    var frame = M("2A3140", 0.8f, 0.5f); var cyan = G("35E0FF", 2f); var line = Add("9FF6FF", 0.9f); var steel = M("8C96A3", 0.8f, 0.7f);
                    B(g, Vector3.zero, new Vector3(0.56f, 0.12f, 0.05f), Add("35E0FF", 0.7f));
                    B(g, new Vector3(0, 0.07f, -0.01f), new Vector3(0.58f, 0.025f, 0.06f), frame);
                    B(g, new Vector3(0, -0.068f, -0.01f), new Vector3(0.54f, 0.016f, 0.055f), frame);
                    for (int k = 0; k < 7; k++) foreach (var sd in FwSd) B(g, new Vector3(sd * 0.145f, 0.045f - k * 0.015f, 0.027f), new Vector3(0.21f, 0.003f, 0.002f), line);
                    // HUD: reticle (right), bar graph (left), data dots
                    var rc = new Vector3(eyeX, 0, 0.028f);
                    FwRing(g, rc, 0.03f, 0.003f, cyan, new Vector3(90, 0, 0));
                    for (int k = 0; k < 4; k++) B(g, rc + Quaternion.Euler(0, 0, k * 90) * new Vector3(0.042f, 0, 0), new Vector3(0.016f, 0.004f, 0.002f), cyan, new Vector3(0, 0, k * 90));
                    for (int k = 0; k < 5; k++) { float h = 0.012f + (k * 7 % 5) * 0.008f; B(g, new Vector3(-eyeX - 0.036f + k * 0.018f, -0.035f + h / 2, 0.028f), new Vector3(0.01f, h, 0.002f), cyan); }
                    for (int k = 0; k < 6; k++) Ls(g, new Vector3(-0.05f + k * 0.02f, -0.045f, 0.028f), 0.007f, cyan);
                    // vents and sensors on the top bar
                    for (int k = 0; k < 12; k++) B(g, new Vector3(-0.22f + k * 0.04f, 0.084f, -0.01f), new Vector3(0.022f, 0.006f, 0.045f), steel);
                    for (int k = 0; k < 6; k++) Ls(g, new Vector3(-0.1f + k * 0.04f, 0.072f, 0.021f), 0.009f, cyan);
                    foreach (var sd in FwSd) B(g, new Vector3(sd * 0.2f, 0.064f, 0.021f), new Vector3(0.14f, 0.004f, 0.003f), cyan);
                    // nose notch with pads
                    B(g, new Vector3(0, -0.055f, 0), new Vector3(0.05f, 0.04f, 0.056f), frame);
                    foreach (var sd in FwSd) Ls(g, new Vector3(sd * 0.03f, -0.07f, -0.02f), new Vector3(0.018f, 0.03f, 0.014f), steel);
                    foreach (var sd in FwSd)
                    {
                        B(g, new Vector3(sd * 0.3f, 0, -0.08f), new Vector3(0.03f, 0.1f, 0.16f), frame);
                        var ear = new Vector3(sd * 0.32f, 0, -0.1f);
                        Lc(g, ear, new Vector3(0.09f, 0.012f, 0.09f), steel, new Vector3(0, 0, 90));
                        FwRing(g, ear + new Vector3(sd * 0.013f, 0, 0), 0.045f, 0.006f, frame, new Vector3(0, 0, 90));
                        for (int k = 0; k < 5; k++) { float y = -0.024f + k * 0.012f; B(g, ear + new Vector3(sd * 0.014f, y, 0), new Vector3(0.004f, 0.005f, 1.6f * Mathf.Sqrt(0.035f * 0.035f - y * y)), frame); }
                        Ls(g, ear + new Vector3(sd * 0.014f, 0.03f, 0.025f), 0.013f, G("FF3B6B", 2.2f));
                        foreach (var up in FwSd) FwScrew(g, new Vector3(sd * 0.3f, up * 0.032f, 0.001f), 0.016f, steel, frame, Vector3.forward);
                        for (int k = 0; k < 3; k++) B(g, new Vector3(sd * (0.215f + k * 0.02f), -0.012f, 0.026f), new Vector3(0.007f, 0.05f, 0.002f), frame);
                    }
                    LRod(g, new Vector3(0.33f, 0.04f, -0.12f), new Vector3(0.36f, 0.2f, -0.16f), 0.01f, steel);
                    Ls(g, new Vector3(0.36f, 0.2f, -0.16f), 0.025f, cyan);
                    // strap round the back of the head (HeadC space) and its buckle
                    var strap = M("1E232C", 0.4f);
                    float sy = eyeY;
                    FwBand(HeadC, FwHeadPath(1.05f, new Vector3(0.37f, sy, 0.18f), new Vector3(0.41f, sy, -0.05f), new Vector3(0.2f, sy + 0.02f, -0.36f), new Vector3(0, sy + 0.03f, -0.4f),
                        new Vector3(-0.2f, sy + 0.02f, -0.36f), new Vector3(-0.41f, sy, -0.05f), new Vector3(-0.37f, sy, 0.18f)), 16, 0.04f, 0.012f, strap, Vector3.zero);
                    var bp = FwOnHead(new Vector3(0, sy + 0.03f, -0.4f), 1.075f);
                    B(HeadC, bp, new Vector3(0.06f, 0.055f, 0.01f), steel);
                    B(HeadC, bp + new Vector3(0, 0, -0.006f), new Vector3(0.04f, 0.035f, 0.006f), strap);
                    Ls(HeadC, bp + new Vector3(0, 0, -0.01f), 0.012f, cyan);
                    break;
                }
                case 3:   // monocle: knurled rim, gallery beads, spurs, chain looping to the ear with a charm
                {
                    var c = new Vector3(eyeX, 0, 0);
                    Ring(g, c + new Vector3(0, 0, 0.005f), 0.09f, 0.015f, gold, new Vector3(90, 0, 0));
                    S(g, c, new Vector3(0.17f, 0.17f, 0.01f), Glass("FFFFFF", 0.2f));
                    FwRing(g, c + new Vector3(0, 0, 0.016f), 0.077f, 0.005f, gold, new Vector3(90, 0, 0));
                    for (int k = 0; k < 32; k++) { float a = k * 360f / 32; B(g, c + Quaternion.Euler(0, 0, a) * new Vector3(0.105f, 0, 0.005f), new Vector3(0.012f, 0.007f, 0.022f), gold, new Vector3(0, 0, a)); }
                    for (int k = 0; k < 14; k++) { float a = (k + 0.5f) / 14f * Mathf.PI * 2; Ls(g, c + new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.09f, 0.021f), 0.01f, gold); }
                    Glints(c + new Vector3(0, 0, 0.006f), 0.08f, shine);
                    foreach (var sd in FwSd)
                    {
                        var top = c + new Vector3(0, sd * 0.125f, -0.02f);
                        LRod(g, c + new Vector3(-0.035f, sd * 0.1f, 0), top, 0.008f, gold);
                        LRod(g, top, c + new Vector3(0.035f, sd * 0.1f, 0), 0.008f, gold);
                    }
                    // chain (HeadC space): from an eyelet on the rim, sagging over the cheek, up to a clip at the ear
                    var e0 = new Vector3(eyeX + 0.07f, eyeY - 0.075f, faceZ + 0.016f);
                    var e2 = FwOnHead(new Vector3(0.41f, -0.02f, -0.02f), 1.03f);
                    var cc = new Vector3(0.36f, -0.42f, 0.3f);
                    FwRing(HeadC, e0, 0.012f, 0.004f, gold, new Vector3(0, 0, 90));
                    System.Func<float, Vector3> cp = t =>
                    {
                        var v = (1 - t) * (1 - t) * e0 + 2 * t * (1 - t) * cc + t * t * e2;
                        var r = Dt.HeadR;
                        float k = Mathf.Sqrt(v.x * v.x / (r.x * r.x) + v.y * v.y / (r.y * r.y) + v.z * v.z / (r.z * r.z));
                        return k < 1.05f ? v / k * 1.05f : v;
                    };
                    FwChain(HeadC, cp, 44, 0.007f, 0.0022f, gold, q => FwHeadN(q));
                    FwRing(HeadC, e2, 0.013f, 0.004f, gold, new Vector3(0, 0, 90));
                    B(HeadC, e2 + new Vector3(0.006f, 0.02f, 0), new Vector3(0.012f, 0.04f, 0.02f), gold);
                    Ls(HeadC, e2 + new Vector3(0.008f, 0.042f, 0), 0.014f, gold);
                    var low = cp(0.5f);
                    FwRing(HeadC, low + new Vector3(0, -0.012f, 0), 0.008f, 0.0025f, gold, new Vector3(0, 0, 90));
                    Msh(Meshes.Star(), HeadC, low + new Vector3(0, -0.04f, 0), new Vector3(0.045f, 0.045f, 0.012f), gold, new Vector3(0, 40, 0));
                    Ls(HeadC, low + new Vector3(0.004f, -0.04f, 0.005f), 0.012f, G("3FA9FF", 1.6f));
                    break;
                }
                case 4:   // pirate patch: stitched leather, skull & crossbones, strap with a brass buckle
                {
                    var black = M("111111", 0.3f); var lip = M("2B2B2B", 0.3f); var thread = M("BDBDBD", 0.2f); var bone = M("F2EEDF", 0.3f);
                    var brass = M("C9A23A", 0.8f, 0.8f); var strap = M("1C1C1C", 0.35f);
                    var pc = new Vector3(-eyeX, 0, 0.005f);
                    S(g, pc, new Vector3(0.18f, 0.16f, 0.04f), black);
                    FwWire(g, t => pc + new Vector3(Mathf.Cos(t * Mathf.PI * 2) * 0.09f, Mathf.Sin(t * Mathf.PI * 2) * 0.08f, 0), 14, 0.014f, lip);
                    Dt.Stitches(g, FwLoop(t => pc + new Vector3(Mathf.Cos(t * Mathf.PI * 2) * 0.072f, Mathf.Sin(t * Mathf.PI * 2) * 0.063f, 0.012f), 22), 22, 0.013f, 0.006f, thread);
                    var sk = pc + new Vector3(0, 0.014f, 0.02f);
                    Ls(g, sk, new Vector3(0.048f, 0.042f, 0.012f), bone);
                    B(g, sk + new Vector3(0, -0.025f, -0.002f), new Vector3(0.028f, 0.016f, 0.008f), bone);
                    foreach (var sd in FwSd) Ls(g, sk + new Vector3(sd * 0.011f, 0.002f, 0.005f), new Vector3(0.013f, 0.015f, 0.006f), black);
                    for (int k = 0; k < 3; k++) B(g, sk + new Vector3(-0.008f + k * 0.008f, -0.028f, 0.002f), new Vector3(0.0025f, 0.01f, 0.008f), black);
                    var xb = sk + new Vector3(0, -0.032f, -0.003f);
                    foreach (var sd in FwSd)
                    {
                        Vector3 b0 = xb + new Vector3(-0.036f, -sd * 0.018f, 0), b1 = xb + new Vector3(0.036f, sd * 0.018f, 0);
                        LRod(g, b0, b1, 0.008f, bone);
                        Ls(g, b0, 0.012f, bone); Ls(g, b1, 0.012f, bone);
                    }
                    // strap over the forehead (above the other eye) and round the head (HeadC space)
                    System.Func<float, Vector3> sp = t => { float a = t * Mathf.PI * 2, x = Mathf.Cos(a) * 0.4f; return FwOnHead(new Vector3(x, 0.1f + 0.55f * x, Mathf.Sin(a) * 0.38f), 1.035f); };
                    FwBand(HeadC, sp, 30, 0.034f, 0.01f, strap, Vector3.zero);
                    Dt.Stitches(HeadC, FwLoop(t => sp(t) * 1.012f, 24), 24, 0.016f, 0.005f, thread);
                    float tb = 1 - 25f / 360;
                    var bp = sp(tb); var bq = Quaternion.LookRotation(FwHeadN(bp), sp(tb + 0.01f) - sp(tb - 0.01f));
                    foreach (var sd in FwSd)
                    {
                        FwAt(B(HeadC, new Vector3(sd * 0.026f, 0, 0.008f), new Vector3(0.006f, 0.045f, 0.006f), brass), bp, bq);
                        FwAt(B(HeadC, new Vector3(0, sd * 0.02f, 0.008f), new Vector3(0.05f, 0.006f, 0.006f), brass), bp, bq);
                    }
                    FwAt(B(HeadC, new Vector3(0, 0, 0.011f), new Vector3(0.004f, 0.04f, 0.004f), brass), bp, bq);
                    FwAt(B(HeadC, new Vector3(0, -0.036f, 0.008f), new Vector3(0.044f, 0.008f, 0.006f), strap), bp, bq);
                    for (int k = 0; k < 3; k++) FwAt(Ls(HeadC, new Vector3(0, 0.04f + k * 0.016f, 0.005f), new Vector3(0.008f, 0.008f, 0.004f), thread), bp, bq);
                    // rivets where the strap meets the patch
                    foreach (var rp in new[] { pc + new Vector3(0.07f, 0.04f, 0.008f), pc + new Vector3(-0.085f, -0.01f, 0) })
                    {
                        FwRing(g, rp, 0.011f, 0.003f, brass, new Vector3(90, 0, 0));
                        Ls(g, rp + new Vector3(0, 0, 0.003f), 0.013f, brass);
                    }
                    break;
                }
                case 5:   // stars in the eyes: spinning outlined stars, halos, twinkles, orbiting mini stars
                {
                    foreach (var pu in Pupils) pu.SetActive(false);
                    var yel = G("FFD23A", 2f); var core = G("FFF6C0", 2.4f); var edge = G("FF9A1F", 1.8f); var ray = Add("FFF3A0", 0.85f);
                    var white = G("FFFFFF", 1.5f); var halo = Add("FFD23A", 0.3f);
                    var star = Meshes.Star();
                    Vector3 V(int j) { float a = Mathf.PI / 2 + j * Mathf.PI / 5, r = j % 2 == 0 ? 0.07f : 0.0294f; return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.019f); }
                    var rng = new Rng(5);
                    foreach (var sd in FwSd)
                    {
                        var ec = new Vector3(sd * eyeX, -0.01f, -0.02f);
                        var sn = Node(sd < 0 ? "StarL" : "StarR", g, ec);
                        Add<Spin>(sn.gameObject).axis = Vector3.forward;
                        Msh(star, sn, Vector3.zero, Vector3.one * 0.14f, yel);
                        Msh(star, sn, new Vector3(0, 0, 0.02f), Vector3.one * 0.07f, core);
                        for (int k = 0; k < 10; k++) { LRod(sn, V(k), V(k + 1), 0.008f, edge); Ls(sn, V(k) + new Vector3(0, 0, 0.003f), k % 2 == 0 ? 0.018f : 0.012f, core); }
                        for (int k = 0; k < 5; k++) { float a = 126 + k * 72; B(sn, Quaternion.Euler(0, 0, a) * new Vector3(0.05f, 0, 0.018f), new Vector3(0.04f, 0.006f, 0.004f), ray, new Vector3(0, 0, a)); }
                        Ls(sn, new Vector3(0, 0, 0.03f), 0.02f, white);
                        // halo and twinkles hugging the face
                        FwRing(g, ec + new Vector3(0, 0, 0.005f), 0.1f, 0.016f, halo, new Vector3(90, 0, 0));
                        for (int k = 0; k < 14; k++)
                        {
                            float a = (k * (k < 8 ? 45 : 60) + (k < 8 ? 20 : 45) + rng.F(-10, 10)) * FwRad, r = k < 8 ? rng.F(0.12f, 0.15f) : rng.F(0.16f, 0.18f);
                            float x = ec.x + Mathf.Cos(a) * r, y = ec.y + Mathf.Sin(a) * r;
                            var p = new Vector3(x, y, FwFaceZ(x, y + eyeY) - faceZ + 0.015f);
                            if (k < 8) FwGlint(g, p, rng.F(0.03f, 0.05f), ray, rng.F(0, 45));
                            else Ls(g, p, 0.012f, core);
                        }
                        var orb = Node("Orbit", g, ec);
                        var sp = Add<Spin>(orb.gameObject); sp.axis = Vector3.forward; sp.speed = -50;
                        for (int k = 0; k < 3; k++) Msh(star, orb, Quaternion.Euler(0, 0, k * 120 + sd * 30) * new Vector3(0.125f, 0, 0.02f), Vector3.one * 0.035f, core);
                    }
                    break;
                }
                case 6:   // neon party glasses: LED rims, glow halos, equalizer lenses, lit temples
                {
                    var pink = G("FF3FA4", 2.4f); var pink2 = G("FF3FA4", 2f); var led = G("FFD6EC", 3f); var cyan = G("3FF2FF", 2.2f);
                    var dark = M("101018", 0.95f); var halo = Add("FF3FA4", 0.35f); var black = M("1A1A22", 0.5f); var pad = Glass("FFFFFF", 0.5f);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * eyeX, 0, 0);
                        Ring(g, c, 0.09f, 0.022f, pink, new Vector3(90, 0, 0));
                        S(g, c + new Vector3(0, 0, -0.005f), new Vector3(0.17f, 0.17f, 0.01f), dark);
                        FwRing(g, c + new Vector3(0, 0, -0.004f), 0.09f, 0.038f, halo, new Vector3(90, 0, 0));
                        for (int k = 0; k < 18; k++) { float a = k * Mathf.PI * 2 / 18; Ls(g, c + new Vector3(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.09f, 0.02f), 0.014f, led); }
                        FwWire(g, t => { float a = Mathf.Lerp(25, 155, t) * FwRad; return c + new Vector3(Mathf.Cos(a) * 0.12f, Mathf.Sin(a) * 0.12f, 0.006f); }, 7, 0.012f, cyan);
                        for (int k = 0; k < 5; k++)
                        {
                            float h = 0.02f + ((k * 3 + (sd > 0 ? 1 : 0)) % 5) * 0.012f;
                            B(g, c + new Vector3(-0.048f + k * 0.024f, -0.045f + h / 2, 0.002f), new Vector3(0.014f, h, 0.003f), k % 2 == 0 ? cyan : pink2);
                        }
                        B(g, c + new Vector3(-0.03f, 0.042f, 0.004f), new Vector3(0.01f, 0.035f, 0.002f), shine, new Vector3(0, 0, 35));
                        Ls(g, c + new Vector3(-0.05f, 0.02f, 0.004f), new Vector3(0.01f, 0.01f, 0.003f), shine);
                        Hinge(new Vector3(sd * 0.25f, 0.02f, -0.01f), sd, black);
                        Temple(sd, 0.016f, pink2, black);
                        for (int k = 0; k < 4; k++) Ls(g, Vector3.Lerp(TA(sd), TB(sd), 0.2f + k * 0.2f) + new Vector3(sd * 0.008f, 0.004f, 0), 0.012f, led);
                        Ls(g, new Vector3(sd * 0.05f, -0.035f, -0.01f), new Vector3(0.018f, 0.03f, 0.014f), pad);
                    }
                    Rod(g, new Vector3(-0.05f, 0.01f, 0), new Vector3(0.05f, 0.01f, 0), 0.02f, pink2);
                    for (int k = 0; k < 3; k++) Ls(g, new Vector3(-0.025f + k * 0.025f, 0.01f, 0.01f), 0.012f, led);
                    break;
                }
                case 7:   // lenticular stickers: rainbow discs with grooves, stripes, printed eye, halftone, peeled corner
                {
                    var white = M("FFFFFF", 0.6f); var ink = M("161616", 0.9f); var holo = Add("FFFFFF", 0.35f); var starM = G("FFF0A0", 1.6f); var rimM = M("F4F4F8", 0.8f, 0.3f);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * eyeX, 0, 0.01f);
                        var d = Cy(g, c, new Vector3(0.17f, 0.006f, 0.17f), M("FFFFFF", 0.95f, 0.4f), new Vector3(90, 0, 0));
                        var hu = Add<Hue>(d); hu.speed = 0.5f; hu.offset = sd * 0.25f;
                        FwRing(g, c, 0.087f, 0.007f, rimM, new Vector3(90, 0, 0));
                        for (int k = 0; k < 5; k++) FwRing(g, c + new Vector3(0, 0, 0.007f), 0.022f + k * 0.013f, 0.003f, holo, new Vector3(90, 0, 0));
                        for (int k = 0; k < 8; k++) { float x = -0.063f + k * 0.018f; B(g, c + new Vector3(x, 0, 0.0075f), new Vector3(0.004f, 2 * Mathf.Sqrt(0.078f * 0.078f - x * x), 0.001f), holo); }
                        Ls(g, c + new Vector3(0, -0.005f, 0.009f), new Vector3(0.07f, 0.08f, 0.006f), ink);
                        Ls(g, c + new Vector3(0.016f, 0.016f, 0.012f), new Vector3(0.022f, 0.022f, 0.004f), white);
                        Ls(g, c + new Vector3(-0.012f, -0.022f, 0.012f), new Vector3(0.01f, 0.01f, 0.004f), white);
                        for (int k = 0; k < 18; k++) { float a = k * Mathf.PI * 2 / 18; Ls(g, c + new Vector3(Mathf.Cos(a) * 0.066f, Mathf.Sin(a) * 0.066f, 0.008f), new Vector3(0.009f, 0.009f, 0.003f), holo); }
                        for (int k = 0; k < 3; k++) Msh(Meshes.Star(), g, c + Quaternion.Euler(0, 0, 30 + k * 120) * new Vector3(0.05f, 0, 0) + new Vector3(0, 0, 0.009f), new Vector3(0.022f, 0.022f, 0.01f), starM);
                        for (int k = 0; k < 3; k++) FwGlint(g, c + new Vector3(-0.045f + k * 0.04f, 0.055f - k * 0.03f, 0.011f), 0.025f - k * 0.004f, holo, 15);
                        for (int k = 0; k < 8; k++) { float a = (k + 0.5f) * Mathf.PI / 4; Ls(g, c + new Vector3(Mathf.Cos(a) * 0.087f, Mathf.Sin(a) * 0.087f, 0.006f), 0.01f, rimM); }
                        var pe = c + new Vector3(sd * 0.055f, -0.055f, 0.012f);
                        B(g, pe, new Vector3(0.03f, 0.022f, 0.003f), white, new Vector3(-20, 0, sd * 45));
                        Ls(g, pe + new Vector3(-sd * 0.012f, 0.012f, 0.002f), new Vector3(0.03f, 0.008f, 0.008f), white, new Vector3(0, 0, sd * 45));
                    }
                    break;
                }
                case 105:   // diamond glasses: star diamonds, claw-set pavé on gold rims, facets, diamond temples
                {
                    var ice = G("BFF3FF", 1.5f); var dia = G("E8FDFF", 2.2f); var white = G("FFFFFF", 2.5f); var sparkle = Add("FFFFFF", 0.8f);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * eyeX, 0, 0);
                        Msh(Meshes.Star(), g, c, new Vector3(0.24f, 0.24f, 0.05f), ice);
                        Ring(g, c + new Vector3(0, 0, -0.01f), 0.1f, 0.012f, gold, new Vector3(90, 0, 0));
                        for (int k = 0; k < 8; k++) { float a = (k * 45 + 22.5f) * FwRad; FwGem(g, c + new Vector3(Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f, 0.004f), 0.026f, dia, gold, Vector3.forward); }
                        for (int k = 0; k < 10; k++)
                        {
                            float a = Mathf.PI / 2 + k * Mathf.PI / 5, r = k % 2 == 0 ? 0.115f : 0.05f;
                            Ls(g, c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.008f), k % 2 == 0 ? 0.02f : 0.014f, white);
                        }
                        Msh(Meshes.Star(), g, c + new Vector3(0, 0, 0.008f), new Vector3(0.1f, 0.1f, 0.03f), dia);
                        for (int k = 0; k < 3; k++) FwGlint(g, c + new Vector3(-0.07f + k * 0.07f, 0.09f - (k % 2) * 0.17f, 0.02f), 0.05f - k * 0.008f, sparkle, 10);
                        Temple(sd, 0.014f, gold, gold);
                        for (int k = 0; k < 5; k++) Ls(g, Vector3.Lerp(TA(sd), TB(sd), 0.15f + k * 0.17f) + new Vector3(sd * 0.009f, 0.006f, 0), 0.012f, dia);
                        Ls(g, TA(sd) + new Vector3(-sd * 0.012f, 0, 0.01f), 0.024f, gold);
                        B(g, TA(sd) + new Vector3(-sd * 0.03f, 0, 0.005f), new Vector3(0.03f, 0.016f, 0.012f), gold);
                    }
                    LRod(g, new Vector3(-0.045f, 0.02f, -0.008f), new Vector3(0.045f, 0.02f, -0.008f), 0.014f, gold);
                    for (int k = 0; k < 3; k++) Ls(g, new Vector3(-0.02f + k * 0.02f, 0.02f, 0.0f), 0.013f, dia);
                    break;
                }
            }
        }

        // ================= masks =================

        void Mask(int i, int set)
        {
            var m = Node("Mask", HeadC, new Vector3(0, -0.12f, faceZ - 0.02f));
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:   // respirator: rubber seal, exhale valve, ribbed filters with holes, two head straps
                {
                    var grey = M("8A939E", 0.5f, 0.2f); var dark = M("3A4048"); var rubber = M("222222", 0.15f); var steel = M("C9D0D8", 0.8f, 0.8f); var band = M("FFC83A", 0.4f);
                    S(m, Vector3.zero, new Vector3(0.26f, 0.2f, 0.14f), grey);
                    float mz = faceZ - 0.02f;
                    FwWire(m, t => { float a = t * Mathf.PI * 2, x = Mathf.Cos(a) * 0.128f, y = Mathf.Sin(a) * 0.098f; return new Vector3(x, y, FwFaceZ(x, y - 0.12f) - mz + 0.006f); }, 20, 0.02f, rubber);
                    // exhale valve
                    var v = new Vector3(0, -0.025f, 0.064f);
                    Lc(m, v, new Vector3(0.07f, 0.012f, 0.07f), dark, new Vector3(90, 0, 0));
                    FwRing(m, v + new Vector3(0, 0, 0.012f), 0.035f, 0.006f, steel, new Vector3(90, 0, 0));
                    Lc(m, v + new Vector3(0, 0, 0.016f), new Vector3(0.055f, 0.006f, 0.055f), grey, new Vector3(90, 0, 0));
                    for (int k = 0; k < 6; k++) B(m, v + new Vector3(0, -0.02f + k * 0.008f, 0.023f), new Vector3(0.04f - Mathf.Abs(k - 2.5f) * 0.006f, 0.003f, 0.004f), rubber);
                    for (int k = 0; k < 4; k++) Ls(m, v + Quaternion.Euler(0, 0, 45 + k * 90) * new Vector3(0.045f, 0, 0) + new Vector3(0, 0, 0.004f), 0.01f, steel);
                    // filter cartridges (local +Y = outwards)
                    foreach (var sd in FwSd)
                    {
                        var fc = new Vector3(sd * 0.13f, -0.03f, 0.02f);
                        var q = Quaternion.LookRotation(new Vector3(sd * 0.85f, -0.1f, 0.5f)) * Quaternion.Euler(90, 0, 0);
                        FwAt(Cy(m, Vector3.zero, new Vector3(0.11f, 0.035f, 0.11f), dark), fc, q);
                        FwAt(Lc(m, new Vector3(0, 0.038f, 0), new Vector3(0.1f, 0.006f, 0.1f), grey), fc, q);
                        for (int k = 0; k < 3; k++) FwAt(FwRing(m, new Vector3(0, -0.02f + k * 0.02f, 0), 0.056f, 0.005f, dark), fc, q);
                        FwAt(FwRing(m, new Vector3(0, -0.03f, 0), 0.058f, 0.006f, band), fc, q);
                        for (int k = 0; k < 7; k++) { var hp = k == 0 ? Vector3.zero : Quaternion.Euler(0, k * 60, 0) * new Vector3(0.028f, 0, 0); FwAt(Lc(m, hp + new Vector3(0, 0.043f, 0), new Vector3(0.016f, 0.003f, 0.016f), rubber), fc, q); }
                        FwAt(Lc(m, new Vector3(0, -0.044f, 0), new Vector3(0.08f, 0.012f, 0.08f), steel), fc, q);
                        FwAt(FwRing(m, new Vector3(0, -0.044f, 0), 0.04f, 0.005f, steel), fc, q);
                    }
                    // two straps round the head (HeadC space) with clips at the mask
                    var strap = M("2A2A2A", 0.3f);
                    FwBand(HeadC, FwHeadPath(1.04f, new Vector3(0.12f, -0.06f, 0.36f), new Vector3(0.38f, -0.02f, 0.1f), new Vector3(0.25f, 0.04f, -0.3f), new Vector3(0, 0.06f, -0.4f),
                        new Vector3(-0.25f, 0.04f, -0.3f), new Vector3(-0.38f, -0.02f, 0.1f), new Vector3(-0.12f, -0.06f, 0.36f)), 20, 0.03f, 0.008f, strap, Vector3.zero);
                    FwBand(HeadC, FwHeadPath(1.04f, new Vector3(0.12f, -0.19f, 0.33f), new Vector3(0.33f, -0.22f, 0.1f), new Vector3(0, -0.26f, -0.3f),
                        new Vector3(-0.33f, -0.22f, 0.1f), new Vector3(-0.12f, -0.19f, 0.33f)), 16, 0.03f, 0.008f, strap, Vector3.zero);
                    foreach (var sd in FwSd) foreach (var y in new[] { -0.06f, -0.19f })
                    {
                        var cp = FwOnHead(new Vector3(sd * 0.15f, y, 0.34f), 1.06f);
                        B(HeadC, cp, new Vector3(0.026f, 0.036f, 0.012f), steel).transform.localRotation = Quaternion.LookRotation(FwHeadN(cp));
                        Ls(HeadC, cp + FwHeadN(cp) * 0.007f, 0.012f, dark);
                    }
                    B(m, new Vector3(0, 0.085f, 0.03f), new Vector3(0.08f, 0.012f, 0.03f), steel, new Vector3(-25, 0, 0));
                    foreach (var sd in FwSd) Ls(m, new Vector3(sd * 0.03f, 0.088f, 0.04f), 0.01f, dark);
                    foreach (var p in new[] { new Vector3(-0.07f, 0.05f, 0.05f), new Vector3(0.07f, 0.05f, 0.05f), new Vector3(-0.06f, -0.075f, 0.045f), new Vector3(0.06f, -0.075f, 0.045f) })
                        Ls(m, p, 0.012f, steel);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 1:   // ninja mask: wrap folds, seams, creases, breathing mesh, shuriken emblem, knot with fringed tails
                case 103: // + shadow version: violet glowing seams, eye slits and emblem
                {
                    bool sh = set == 3;
                    var c = sh ? M("2A1E40", 0.3f) : M("16161A", 0.3f);
                    var fold = sh ? M("3A2A58", 0.3f) : M("2A2A32", 0.3f);
                    var thread = sh ? G("B57CFF", 2.2f) : M("4A4A54", 0.3f);
                    var metal = sh ? G("B57CFF", 2.6f) : M("9AA3AE", 0.8f, 0.8f);
                    var mc = new Vector3(0, -0.12f, 0.01f); var mr = new Vector3(0.42f, 0.22f, 0.4f);
                    S(HeadC, mc, mr * 2, c);
                    for (int k = 0; k < 4; k++)
                    {
                        float th = (75 + k * 20) * FwRad, f = Mathf.Sin(th);
                        FwRing(HeadC, new Vector3(0, mc.y + Mathf.Cos(th) * mr.y, mc.z), 1f, 0.03f, fold, default, new Vector3(mr.x * f * 1.01f, 0.45f, mr.z * f * 1.01f), 28);
                    }
                    Dt.Stitches(HeadC, FwLoop(t => Dt.Ell(mc, mr, 74, t * 360, 1.015f), 32), 32, 0.02f, 0.006f, thread);
                    Dt.Stitches(HeadC, FwLoop(t => Dt.Ell(mc, mr, 138, t * 360, 1.025f), 22), 22, 0.02f, 0.006f, thread);
                    Dt.Stitches(HeadC, t => Dt.Ell(mc, mr, Mathf.Lerp(82, 132, t), 90, 1.015f), 7, 0.02f, 0.006f, thread);
                    foreach (var sd in FwSd)
                        for (int k = 0; k < 6; k++)
                        {
                            float th = 86 + k * 7, ph = 90 - sd * (42 + (k % 2) * 10);
                            var go = B(HeadC, Dt.Ell(mc, mr, th, ph, 1.008f), new Vector3(0.009f, 0.05f, 0.01f), fold);
                            go.transform.localRotation = Quaternion.LookRotation(Dt.EllN(mr, th, ph), Vector3.up) * Quaternion.Euler(0, 0, sd * 25);
                        }
                    for (int r = 0; r < 3; r++)
                        for (int k = 0; k < 4; k++)
                            Ls(HeadC, Dt.Ell(mc, mr, 92 + r * 7, 90 + (k - 1.5f) * 7, 1.01f), new Vector3(0.016f, 0.016f, 0.008f), fold);
                    // shuriken emblem on the left cheek
                    float eth = 104, eph = 128;
                    var ep = Dt.Ell(mc, mr, eth, eph, 1.02f); var eq = Quaternion.LookRotation(Dt.EllN(mr, eth, eph), Vector3.up);
                    for (int k = 0; k < 4; k++) Spike(HeadC, ep, ep + eq * (Quaternion.Euler(0, 0, 45 + k * 90) * new Vector3(0.045f, 0, 0)), 0.03f, metal);
                    Ls(HeadC, ep + eq * new Vector3(0, 0, 0.005f), 0.022f, sh ? thread : fold);
                    // knot at the back with fringed tails
                    var knot = Node("Knot", HeadC, new Vector3(0, -0.05f, -0.38f));
                    Ls(knot, new Vector3(0, 0, -0.02f), new Vector3(0.09f, 0.07f, 0.06f), c);
                    foreach (var sd in FwSd)
                    {
                        FwRing(knot, new Vector3(sd * 0.06f, 0.012f, -0.02f), 0.032f, 0.013f, c, new Vector3(90, 0, sd * 25));
                        Vector3 end = default;
                        for (int j = 0; j < 3; j++)
                        {
                            end = new Vector3(sd * (0.05f + j * 0.022f), -0.07f - j * 0.09f, -0.03f - j * 0.014f);
                            B(knot, end, new Vector3(0.065f, 0.1f, 0.02f), c, new Vector3(10, 0, sd * (15 + j * 6)));
                        }
                        for (int j = 0; j < 4; j++) B(knot, end + new Vector3(sd * 0.012f + (j - 1.5f) * 0.015f, -0.065f, 0), new Vector3(0.011f, 0.04f, 0.012f), j % 2 == 0 ? c : fold, new Vector3(10, 0, sd * 20 + (j - 1.5f) * 6));
                    }
                    if (sh)
                        foreach (var sd in FwSd)
                        {
                            var sl = new Vector3(sd * eyeX, eyeY, faceZ);
                            B(HeadC, sl, new Vector3(0.14f, 0.025f, 0.02f), G("B57CFF", 3f));
                            Spike(HeadC, sl + new Vector3(sd * 0.065f, 0, 0), sl + new Vector3(sd * 0.12f, 0.03f, -0.03f), 0.022f, thread);
                            Spike(HeadC, sl + new Vector3(sd * 0.06f, -0.008f, 0), sl + new Vector3(sd * 0.11f, -0.02f, -0.035f), 0.016f, thread);
                        }
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 2:   // gas mask: bolted eyepieces, ribbed filter with grille, cheek valves, chin cup, spider harness
                {
                    var olive = M("4F5B3A", 0.3f); var dark = M("2A2E26"); var steel = M("9AA3AE", 0.7f, 0.7f); var rubber = M("1E201C", 0.15f); var shine = Add("FFFFFF", 0.5f);
                    var hc = new Vector3(0, 0.12f, -0.08f); var hr = new Vector3(0.29f, 0.26f, 0.17f);
                    S(m, hc, hr * 2, olive);
                    foreach (var sd in FwSd)
                    {
                        var e = new Vector3(sd * eyeX, eyeY, faceZ + 0.04f);
                        Cy(HeadC, e, new Vector3(0.17f, 0.03f, 0.17f), Glass("1A2A1A", 0.75f), new Vector3(90, 0, 0));
                        FwRing(HeadC, e + new Vector3(0, 0, 0.03f), 0.088f, 0.014f, steel, new Vector3(90, 0, 0));
                        FwRing(HeadC, e + new Vector3(0, 0, 0.005f), 0.1f, 0.018f, rubber, new Vector3(90, 0, 0));
                        for (int k = 0; k < 8; k++) { float a = (k * 45 + 22.5f) * FwRad; Ls(HeadC, e + new Vector3(Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f, 0.024f), 0.014f, steel); }
                        B(HeadC, e + new Vector3(-0.025f, 0.03f, 0.032f), new Vector3(0.01f, 0.05f, 0.002f), shine, new Vector3(0, 0, 35));
                        Ls(HeadC, e + new Vector3(0.035f, -0.03f, 0.032f), new Vector3(0.012f, 0.012f, 0.003f), shine);
                        // speech / exhale valve on the cheek
                        float th = 112, ph = 90 - sd * 50;
                        var vp = Dt.Ell(hc, hr, th, ph); var vq = Quaternion.FromToRotation(Vector3.up, Dt.EllN(hr, th, ph));
                        FwAt(Lc(m, new Vector3(0, 0.008f, 0), new Vector3(0.06f, 0.012f, 0.06f), dark), vp, vq);
                        FwAt(FwRing(m, new Vector3(0, 0.02f, 0), 0.03f, 0.006f, steel), vp, vq);
                        for (int k = 0; k < 4; k++) FwAt(B(m, new Vector3(0, 0.022f, -0.012f + k * 0.008f), new Vector3(0.04f, 0.004f, 0.004f), rubber), vp, vq);
                    }
                    // filter canister (local +Y = forward-down)
                    var fc = new Vector3(0, -0.04f, 0.1f); var fq = Quaternion.Euler(110, 0, 0);
                    FwAt(Cy(m, Vector3.zero, new Vector3(0.16f, 0.08f, 0.16f), dark), fc, fq);
                    FwAt(Lc(m, new Vector3(0, 0.085f, 0), new Vector3(0.15f, 0.008f, 0.15f), steel), fc, fq);
                    for (int k = 0; k < 9; k++) { var hp = k == 0 ? Vector3.zero : Quaternion.Euler(0, k * 45, 0) * new Vector3(0.045f, 0, 0); FwAt(Lc(m, hp + new Vector3(0, 0.092f, 0), new Vector3(0.018f, 0.004f, 0.018f), rubber), fc, fq); }
                    for (int k = 0; k < 3; k++) FwAt(FwRing(m, new Vector3(0, -0.05f + k * 0.035f, 0), 0.081f, 0.006f, steel), fc, fq);
                    for (int k = 0; k < 8; k++) { var r = Quaternion.Euler(0, k * 45 + 22.5f, 0); FwAt(B(m, r * new Vector3(0.082f, 0, 0), new Vector3(0.012f, 0.1f, 0.012f), dark), fc, fq * r); }
                    FwAt(Lc(m, new Vector3(0, -0.095f, 0), new Vector3(0.12f, 0.02f, 0.12f), steel), fc, fq);
                    // chin cup
                    var chin = Dt.Ell(hc, hr, 150, 90, 0.97f);
                    Ls(m, chin, new Vector3(0.15f, 0.07f, 0.1f), olive);
                    for (int k = 0; k < 3; k++) B(m, chin + new Vector3(0, -0.02f + k * 0.02f, 0.048f - Mathf.Abs(k - 1) * 0.006f), new Vector3(0.11f - Mathf.Abs(k - 1) * 0.03f, 0.008f, 0.01f), dark);
                    // spider harness over the head (HeadC space)
                    var strap = M("2A2A24", 0.3f);
                    FwBand(HeadC, FwHeadPath(1.04f, new Vector3(0, 0.24f, 0.3f), new Vector3(0, 0.39f, 0.05f), new Vector3(0, 0.3f, -0.25f), new Vector3(0, 0.12f, -0.37f)), 8, 0.035f, 0.01f, strap, Vector3.zero);
                    var starts = new List<Vector3> { new Vector3(0, 0.24f, 0.3f) };
                    foreach (var sd in FwSd)
                    {
                        FwBand(HeadC, FwHeadPath(1.04f, new Vector3(sd * 0.27f, 0.13f, 0.27f), new Vector3(sd * 0.36f, 0.17f, -0.05f), new Vector3(sd * 0.1f, 0.13f, -0.37f)), 8, 0.035f, 0.01f, strap, Vector3.zero);
                        FwBand(HeadC, FwHeadPath(1.04f, new Vector3(sd * 0.27f, -0.13f, 0.26f), new Vector3(sd * 0.38f, -0.08f, -0.05f), new Vector3(sd * 0.1f, 0.1f, -0.37f)), 8, 0.035f, 0.01f, strap, Vector3.zero);
                        starts.Add(new Vector3(sd * 0.27f, 0.13f, 0.27f)); starts.Add(new Vector3(sd * 0.27f, -0.13f, 0.26f));
                    }
                    foreach (var s0 in starts)
                    {
                        var bp = FwOnHead(s0, 1.06f); var bn = FwHeadN(bp);
                        B(HeadC, bp, new Vector3(0.04f, 0.03f, 0.008f), steel).transform.localRotation = Quaternion.LookRotation(bn);
                        Ls(HeadC, bp + bn * 0.006f, 0.012f, dark);
                    }
                    var pad = FwOnHead(new Vector3(0, 0.12f, -0.37f), 1.06f);
                    Ls(HeadC, pad, new Vector3(0.11f, 0.11f, 0.035f), rubber);
                    foreach (var sx in FwSd) foreach (var sy in FwSd) Ls(HeadC, pad + new Vector3(sx * 0.03f, sy * 0.03f, -0.014f), 0.012f, steel);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 3:   // carnival mask: gold trim, rhinestones, ringed eye holes, filigree, feathers, brooch, ribbons
                {
                    var c = Node("Carnival", HeadC, new Vector3(0, eyeY, faceZ + 0.0f));
                    var purple = M("8E24AA", 0.7f, 0.3f); var goldM = M("FFC83A", 0.85f, 0.8f); var pinkG = G("FF5FC8", 1.6f); var tealG = G("3FF2E0", 1.6f);
                    var black = M("000000"); var ribbon = M("E91E63", 0.5f);
                    var poly = new[] { new Vector2(-0.3f, 0.02f), new Vector2(-0.22f, 0.1f), new Vector2(-0.05f, 0.07f), new Vector2(0, 0.03f), new Vector2(0.05f, 0.07f), new Vector2(0.22f, 0.1f), new Vector2(0.3f, 0.02f), new Vector2(0.2f, -0.08f), new Vector2(0.05f, -0.05f), new Vector2(0, -0.08f), new Vector2(-0.05f, -0.05f), new Vector2(-0.2f, -0.08f) };
                    Msh(Meshes.Fan("carn", poly, 0.03f), c, Vector3.zero, Vector3.one, purple);
                    for (int k = 0; k < poly.Length; k++)
                    {
                        Vector3 a = (Vector3)poly[k] + new Vector3(0, 0, 0.012f), b = (Vector3)poly[(k + 1) % poly.Length] + new Vector3(0, 0, 0.012f), mid = (a + b) / 2;
                        var e = (b - a) * 0.04f;
                        LRod(c, a - e, mid, 0.012f, goldM); LRod(c, mid, b + e, 0.012f, goldM);
                        Ls(c, (Vector3)(poly[k] * 0.82f) + new Vector3(0, 0, 0.016f), 0.016f, k % 2 == 0 ? pinkG : tealG);
                    }
                    foreach (var sd in FwSd)
                    {
                        var eh = new Vector3(sd * eyeX, 0, 0.012f);
                        Ls(c, eh, new Vector3(0.1f, 0.06f, 0.01f), black);
                        FwRing(c, eh + new Vector3(0, 0, 0.002f), 1f, 0.12f, goldM, new Vector3(90, 0, 0), new Vector3(0.055f, 0.1f, 0.034f), 20);
                        for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; Ls(c, eh + new Vector3(Mathf.Cos(a) * 0.064f, Mathf.Sin(a) * 0.043f, 0.004f), 0.011f, goldM); }
                        for (int k = 0; k < 8; k++) { float a = k * 0.85f, r = 0.026f * (1 - k / 9f); Ls(c, new Vector3(sd * (0.25f + Mathf.Cos(a) * r), 0.02f + Mathf.Sin(a) * r, 0.017f), 0.012f * (1 - k * 0.06f), goldM); }
                        for (int k = 0; k < 6; k++) { float t = k / 5f; Ls(c, new Vector3(sd * Mathf.Lerp(0.19f, 0.07f, t), 0.056f + Mathf.Sin(t * Mathf.PI) * 0.01f, 0.017f), 0.011f, goldM); }
                        // ribbon to the side of the head and a hanging tail
                        var head = new Vector3(0, -eyeY, -faceZ);
                        FwBand(c, t => t < 0.34f ? Vector3.Lerp(new Vector3(sd * 0.3f, 0.02f, -0.005f), new Vector3(sd * 0.37f, 0.025f, -0.12f), t / 0.34f)
                            : t < 0.67f ? Vector3.Lerp(new Vector3(sd * 0.37f, 0.025f, -0.12f), new Vector3(sd * 0.42f, 0.03f, -0.26f), (t - 0.34f) / 0.33f)
                            : Vector3.Lerp(new Vector3(sd * 0.42f, 0.03f, -0.26f), new Vector3(sd * 0.43f, 0.03f, -0.38f), (t - 0.67f) / 0.33f), 4, 0.025f, 0.006f, ribbon, head);
                        for (int k = 0; k < 2; k++) B(c, new Vector3(sd * (0.43f + k * 0.012f), -0.04f - k * 0.02f, -0.37f - k * 0.02f), new Vector3(0.008f, 0.12f, 0.025f), ribbon, new Vector3(0, 0, sd * (8 + k * 12)));
                    }
                    var fb = new Vector3(0.25f, 0.08f, -0.01f);
                    string[] vane = { "E91E63", "FFC83A", "26C6DA" };
                    for (int k = 0; k < 3; k++)
                    {
                        var dir = Quaternion.Euler(0, 0, -12 - k * 22) * Vector3.up;
                        Dt.Feather(c, fb, dir, Vector3.Cross(Vector3.forward, dir), 0.22f - k * 0.02f, M(vane[k], 0.4f), goldM);
                    }
                    Dt.Gem(c, fb + new Vector3(0, 0, 0.02f), 0.04f, tealG, goldM, Vector3.forward);
                    break;
                }
                case 4:   // cyber faceplate: bevels, segmented glow lines, vents, screws, cheek modules, pistons, cables
                {
                    var ch = M("D9DEE6", 0.95f, 0.9f); var dark = M("2A3140", 0.8f, 0.5f); var cyan = G("35E0FF", 2f); var red = G("FF3B6B", 2.2f); var steel = M("8C96A3", 0.8f, 0.7f);
                    B(m, new Vector3(0, -0.01f, 0), new Vector3(0.52f, 0.2f, 0.1f), ch);
                    foreach (var y in new[] { 0.094f, -0.114f }) B(m, new Vector3(0, y, 0), new Vector3(0.54f, 0.014f, 0.105f), dark);
                    for (int r = 0; r < 3; r++)
                    {
                        int n = 6 - r; float w = 0.4f - r * 0.08f, dw = w / n;
                        for (int k = 0; k < n; k++) B(m, new Vector3(-w / 2 + dw * (k + 0.5f), 0.05f - r * 0.05f, 0.052f), new Vector3(dw * 0.75f, 0.012f, 0.01f), cyan);
                    }
                    foreach (var sd in FwSd)
                    {
                        for (int k = 0; k < 4; k++) B(m, new Vector3(sd * 0.232f, 0.055f - k * 0.035f, 0.051f), new Vector3(0.032f, 0.008f, 0.006f), dark);
                        B(m, new Vector3(sd * 0.208f, -0.01f, 0.051f), new Vector3(0.004f, 0.18f, 0.004f), dark);
                        // angled cheek guard with hex dots, module, piston and cable
                        var gq = Quaternion.Euler(0, -sd * 25, 0); var gp = new Vector3(sd * 0.285f, -0.01f, -0.045f);
                        FwAt(B(m, Vector3.zero, new Vector3(0.03f, 0.17f, 0.11f), ch), gp, gq);
                        for (int k = 0; k < 5; k++) FwAt(Ls(m, new Vector3(sd * 0.016f, -0.05f + k * 0.025f, (k % 2) * 0.02f - 0.01f), new Vector3(0.006f, 0.016f, 0.016f), dark), gp, gq);
                        var mp = new Vector3(sd * 0.31f, 0.03f, -0.07f);
                        Lc(m, mp, new Vector3(0.06f, 0.02f, 0.06f), steel, new Vector3(0, 0, 90));
                        FwRing(m, mp + new Vector3(sd * 0.02f, 0, 0), 0.028f, 0.005f, dark, new Vector3(0, 0, 90));
                        Ls(m, mp + new Vector3(sd * 0.024f, 0, 0), 0.016f, red);
                        for (int k = 0; k < 3; k++) B(m, mp + new Vector3(sd * 0.01f, -0.04f - k * 0.012f, 0), new Vector3(0.03f, 0.006f, 0.05f), dark);
                        Vector3 p0 = new Vector3(sd * 0.3f, -0.09f, -0.08f), p1 = new Vector3(sd * 0.36f, -0.05f, -0.2f), p2 = new Vector3(sd * 0.39f, -0.02f, -0.3f);
                        LRod(m, p0, p1, 0.026f, steel); LRod(m, p1, p2, 0.014f, ch); Ls(m, p0, 0.03f, dark); Ls(m, p2, 0.026f, dark);
                        var ce = new Vector3(sd * 0.41f, 0.06f, -0.33f);
                        LRod(m, mp, ce, 0.016f, dark);
                        for (int k = 0; k < 3; k++) Lc(m, Vector3.Lerp(mp, ce, 0.25f + k * 0.25f), new Vector3(0.024f, 0.006f, 0.024f), steel).transform.localRotation = Quaternion.FromToRotation(Vector3.up, ce - mp);
                    }
                    B(m, new Vector3(0, -0.08f, 0.051f), new Vector3(0.4f, 0.004f, 0.004f), dark);
                    foreach (var p in new[] { new Vector3(-0.235f, 0.075f, 0), new Vector3(0.235f, 0.075f, 0), new Vector3(-0.235f, -0.095f, 0), new Vector3(0.235f, -0.095f, 0), new Vector3(0, 0.075f, 0), new Vector3(0, -0.095f, 0) })
                        FwScrew(m, p + new Vector3(0, 0, 0.051f), 0.016f, steel, dark, Vector3.forward);
                    B(m, new Vector3(0, -0.135f, -0.01f), new Vector3(0.36f, 0.03f, 0.09f), ch, new Vector3(25, 0, 0));
                    for (int k = 0; k < 5; k++) B(m, new Vector3(-0.12f + k * 0.06f, -0.142f, 0.03f), new Vector3(0.012f, 0.02f, 0.03f), dark, new Vector3(25, 0, 0));
                    for (int k = 0; k < 5; k++) Ls(m, new Vector3(-0.08f + k * 0.04f, 0.094f, 0.053f), 0.011f, k == 2 ? red : cyan);
                    for (int k = 0; k < 8; k++) Ls(m, new Vector3(-0.245f + k * 0.07f, 0.094f, 0.052f), 0.008f, steel);
                    for (int k = 0; k < 6; k++) Ls(m, new Vector3(-0.2f + k * 0.08f, -0.114f, 0.052f), 0.008f, steel);
                    LRod(m, new Vector3(0.33f, 0.06f, -0.08f), new Vector3(0.35f, 0.2f, -0.12f), 0.01f, steel);
                    Ls(m, new Vector3(0.35f, 0.2f, -0.12f), 0.022f, red);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 5:   // scarf over the nose: knit loops in three rows (cream middle row), ribbed edges, fringed end
                {
                    var red = M("D32F2F", 0.15f); var red2 = M("A61F1F", 0.15f); var cream = M("F5EEDC", 0.15f);
                    var rc = new Vector3(0, -0.09f, 0); var rq = Quaternion.Euler(-8, 0, 0); var rs = new Vector3(1, 1, 1.06f);
                    Ring(HeadC, rc, 0.37f, 0.08f, red, new Vector3(-8, 0, 0), rs);
                    for (int row = 0; row < 3; row++)
                        for (int k = 0; k < 22; k++)
                        {
                            float a = (k + row * 0.5f) * Mathf.PI * 2 / 22, b = (row - 1) * 45 * FwRad;
                            float rr = 0.37f + 0.08f * Mathf.Cos(b);
                            var lp = Vector3.Scale(new Vector3(Mathf.Cos(a) * rr, 0.08f * Mathf.Sin(b), Mathf.Sin(a) * rr), rs);
                            var nrm = rq * new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                            var up = rq * new Vector3(-Mathf.Cos(a) * Mathf.Sin(b), Mathf.Cos(b), -Mathf.Sin(a) * Mathf.Sin(b));
                            FwKnitV(HeadC, rc + rq * lp + nrm * 0.004f, nrm, up, 0.05f, row == 1 ? cream : red2);
                        }
                    foreach (var sd in FwSd) FwRing(HeadC, rc + rq * new Vector3(0, sd * 0.069f, 0), 0.41f, 0.014f, red2, new Vector3(-8, 0, 0), rs, 28);
                    // hanging end with knit rows and fringe
                    var ep = new Vector3(0.18f, -0.28f, 0.3f); var eq = Quaternion.Euler(10, 0, -12);
                    B(HeadC, ep, new Vector3(0.1f, 0.24f, 0.04f), red, new Vector3(10, 0, -12));
                    for (int r = 0; r < 4; r++) foreach (var sd in FwSd)
                        FwKnitV(HeadC, ep + eq * new Vector3(sd * 0.024f, 0.08f - r * 0.05f, 0.022f), eq * Vector3.forward, eq * Vector3.up, 0.04f, r == 1 || r == 2 ? cream : red2);
                    for (int k = 0; k < 5; k++)
                    {
                        var f0 = ep + eq * new Vector3(-0.04f + k * 0.02f, -0.12f, 0); var f1 = ep + eq * new Vector3(-0.042f + k * 0.021f, -0.17f, 0.004f);
                        LRod(HeadC, f0, f1, 0.009f, red2); Ls(HeadC, f1, 0.014f, red);
                    }
                    Ls(HeadC, new Vector3(0.2f, -0.16f, 0.33f), new Vector3(0.12f, 0.1f, 0.08f), red);
                    foreach (var sd in FwSd) B(HeadC, new Vector3(0.2f + sd * 0.03f, -0.16f, 0.37f), new Vector3(0.012f, 0.09f, 0.012f), red2, new Vector3(0, 0, sd * 20));
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 6:   // smiley mask: ridged rim, shiny eyes, brows, smile with dimples and tongue, cheeks, elastic cords
                {
                    var s = Node("Smiley", HeadC, new Vector3(0, 0, faceZ + 0.015f));
                    var yel = M("FFD600", 0.4f); var rimM = M("FFB300", 0.4f); var ink = M("1A1A1A"); var pink = M("FF8FA8", 0.2f); var white = G("FFFFFF", 1f);
                    var shine = Add("FFFFFF", 0.45f); var cord = M("222222", 0.2f); var tongue = M("FF5C7A", 0.3f); var freck = M("E89A2C", 0.3f);
                    Cy(s, Vector3.zero, new Vector3(0.62f, 0.01f, 0.62f), yel, new Vector3(90, 0, 0));
                    FwRing(s, Vector3.zero, 0.31f, 0.018f, rimM, new Vector3(90, 0, 0), null, 32);
                    for (int k = 0; k < 28; k++) { float a = k * 360f / 28; B(s, Quaternion.Euler(0, 0, a) * new Vector3(0.31f, 0, 0.014f), new Vector3(0.026f, 0.01f, 0.012f), rimM, new Vector3(0, 0, a)); }
                    foreach (var sd in FwSd)
                    {
                        S(s, new Vector3(sd * 0.1f, 0.08f, 0.015f), new Vector3(0.06f, 0.1f, 0.01f), ink);
                        Ls(s, new Vector3(sd * 0.1f + 0.01f, 0.105f, 0.021f), new Vector3(0.018f, 0.022f, 0.004f), white);
                        Ls(s, new Vector3(sd * 0.1f - 0.008f, 0.06f, 0.021f), new Vector3(0.009f, 0.009f, 0.004f), white);
                        FwWire(s, t => { float a = Mathf.Lerp(35, 145, t) * FwRad; return new Vector3(sd * 0.1f + Mathf.Cos(a) * 0.05f, 0.13f + Mathf.Sin(a) * 0.03f, 0.013f); }, 4, 0.012f, ink);
                        var ck = new Vector3(sd * 0.19f, -0.04f, 0.011f);
                        Ls(s, ck, new Vector3(0.08f, 0.05f, 0.004f), pink);
                        for (int k = 0; k < 3; k++) B(s, ck + new Vector3(-0.018f + k * 0.018f, 0, 0.003f), new Vector3(0.006f, 0.026f, 0.002f), M("FF6B8B", 0.2f), new Vector3(0, 0, 25));
                        Ls(s, new Vector3(sd * 0.132f, -0.04f, 0.013f), 0.022f, ink);
                    }
                    FwWire(s, t => { float a = Mathf.Lerp(200, 340, t) * FwRad; return new Vector3(Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.12f, 0.013f); }, 14, 0.02f, ink);
                    Ls(s, new Vector3(0.035f, -0.128f, 0.011f), new Vector3(0.05f, 0.04f, 0.008f), tongue);
                    B(s, new Vector3(0.035f, -0.13f, 0.015f), new Vector3(0.003f, 0.022f, 0.002f), M("C2185B", 0.3f));
                    for (int k = 0; k < 4; k++) { float a = (130 - k * 14) * FwRad; B(s, new Vector3(Mathf.Cos(a) * 0.24f, Mathf.Sin(a) * 0.24f, 0.012f), new Vector3(0.012f, 0.04f, 0.002f), shine, new Vector3(0, 0, 130 - k * 14 - 90)); }
                    Ls(s, new Vector3(-0.12f, 0.22f, 0.012f), new Vector3(0.016f, 0.016f, 0.003f), shine);
                    for (int k = 0; k < 6; k++) Ls(s, new Vector3((k % 3 - 1) * 0.022f + (k < 3 ? -0.19f : 0.19f), -0.005f + (k % 2) * 0.012f, 0.012f), new Vector3(0.01f, 0.01f, 0.003f), freck);
                    // elastic cords round the head (HeadC space)
                    foreach (var y in new[] { 0.07f, -0.12f })
                    {
                        float ex = Mathf.Sqrt(0.31f * 0.31f - y * y) - 0.015f;
                        var a0 = FwOnHead(new Vector3(0.4f, y, 0.12f), 1.03f); var a1 = FwOnHead(new Vector3(-0.4f, y, 0.12f), 1.03f);
                        FwWire(HeadC, FwHeadPath(1.03f, a0, new Vector3(0.3f, y + 0.01f, -0.28f), new Vector3(0, y + 0.02f, -0.4f), new Vector3(-0.3f, y + 0.01f, -0.28f), a1), y > 0 ? 14 : 12, 0.012f, cord);
                        foreach (var sd in FwSd)
                        {
                            var ee = new Vector3(sd * ex, y, faceZ + 0.015f);
                            LRod(HeadC, ee + new Vector3(0, 0, -0.008f), sd > 0 ? a0 : a1, 0.012f, cord);
                            FwRing(HeadC, ee + new Vector3(0, 0, 0.007f), 0.011f, 0.003f, M("C9D0D8", 0.8f, 0.8f), new Vector3(90, 0, 0));
                            Ls(HeadC, ee + new Vector3(0, 0, -0.012f), 0.018f, cord);
                        }
                    }
                    foreach (var e in new[] { EyeL, EyeR }) if (e) e.gameObject.SetActive(false);
                    Mouth.gameObject.SetActive(false);
                    break;
                }
                case 7:   // robot mask: framed plate, bolted LED eyes, speaker grille, vents, ear gears, rivets
                {
                    var grey = M("8C96A3", 0.7f, 0.6f); var dark = M("2A2E36"); var chrome = M("D9DEE6", 0.95f, 0.9f); var red = G("FF2E2E", 2.5f); var red2 = G("FF9A9A", 3f); var cyan = G("35E0FF", 2f);
                    float z0 = faceZ - 0.01f, zf = z0 + 0.04f;
                    B(HeadC, new Vector3(0, 0, z0), new Vector3(0.6f, 0.56f, 0.08f), grey);
                    foreach (var sd in FwSd)
                    {
                        B(HeadC, new Vector3(0, sd * 0.28f, z0), new Vector3(0.62f, 0.03f, 0.09f), chrome);
                        B(HeadC, new Vector3(sd * 0.3f, 0, z0), new Vector3(0.03f, 0.56f, 0.09f), chrome);
                        foreach (var sy in FwSd) FwScrew(HeadC, new Vector3(sd * 0.25f, sy * 0.23f, zf + 0.001f), 0.024f, chrome, dark, Vector3.forward);
                        var e = new Vector3(sd * 0.13f, 0.06f, zf);
                        Cy(HeadC, new Vector3(sd * 0.13f, 0.06f, faceZ + 0.035f), new Vector3(0.1f, 0.01f, 0.1f), red, new Vector3(90, 0, 0));
                        FwRing(HeadC, e + new Vector3(0, 0, 0.004f), 0.058f, 0.01f, dark, new Vector3(90, 0, 0));
                        FwRing(HeadC, e + new Vector3(0, 0, 0.004f), 0.074f, 0.008f, chrome, new Vector3(90, 0, 0));
                        for (int k = 0; k < 6; k++) { float a = (k * 60 + 30) * FwRad; Ls(HeadC, e + new Vector3(Mathf.Cos(a) * 0.074f, Mathf.Sin(a) * 0.074f, 0.01f), 0.012f, dark); }
                        B(HeadC, e + new Vector3(0, 0, 0.007f), new Vector3(0.08f, 0.004f, 0.002f), red2);
                        B(HeadC, e + new Vector3(0, 0, 0.007f), new Vector3(0.004f, 0.08f, 0.002f), red2);
                        B(HeadC, e + new Vector3(0, 0.022f, 0.007f), new Vector3(0.06f, 0.006f, 0.002f), red2);
                        // cheek panel lines and dots
                        B(HeadC, new Vector3(sd * 0.2f, -0.1f, zf + 0.001f), new Vector3(0.004f, 0.12f, 0.003f), dark);
                        B(HeadC, new Vector3(sd * 0.235f, -0.04f, zf + 0.001f), new Vector3(0.07f, 0.004f, 0.003f), dark);
                        for (int k = 0; k < 3; k++) Ls(HeadC, new Vector3(sd * 0.24f, -0.08f - k * 0.03f, zf), 0.012f, k == 0 ? cyan : dark);
                        // ear gear
                        var ear = new Vector3(sd * 0.32f, 0, z0);
                        Lc(HeadC, ear, new Vector3(0.1f, 0.012f, 0.1f), dark, new Vector3(0, 0, 90));
                        FwRing(HeadC, ear + new Vector3(sd * 0.012f, 0, 0), 0.032f, 0.007f, chrome, new Vector3(0, 0, 90));
                        for (int k = 0; k < 6; k++) B(HeadC, ear, new Vector3(0.022f, 0.12f, 0.014f), dark, new Vector3(k * 30, 0, 0));
                        B(HeadC, new Vector3(sd * 0.2f, 0.22f, zf + 0.002f), new Vector3(0.025f, 0.025f, 0.006f), chrome);
                    }
                    B(HeadC, new Vector3(0, -0.14f, zf + 0.001f), new Vector3(0.26f, 0.012f, 0.012f), chrome);
                    B(HeadC, new Vector3(0, -0.14f, zf + 0.001f), new Vector3(0.012f, 0.11f, 0.012f), chrome);
                    for (int k = 0; k < 7; k++) B(HeadC, new Vector3(-0.09f + k * 0.03f, -0.14f, zf + 0.002f), new Vector3(0.018f, 0.08f, 0.01f), dark);
                    foreach (var sy in FwSd) B(HeadC, new Vector3(0, -0.14f + sy * 0.055f, zf + 0.002f), new Vector3(0.24f, 0.01f, 0.012f), chrome);
                    for (int r = 0; r < 3; r++) foreach (var sd in FwSd) B(HeadC, new Vector3(sd * 0.07f, 0.24f - r * 0.022f, zf + 0.001f), new Vector3(0.07f, 0.008f, 0.006f), dark);
                    for (int k = 0; k < 5; k++) B(HeadC, new Vector3(-0.05f + k * 0.025f, 0.165f, zf + 0.002f), new Vector3(0.016f, 0.01f, 0.004f), cyan);
                    for (int k = 0; k < 3; k++) B(HeadC, new Vector3(0, -0.215f - k * 0.016f, zf + 0.001f), new Vector3(0.16f - k * 0.03f, 0.007f, 0.006f), dark);
                    foreach (var sy in FwSd) for (int k = 0; k < 8; k++) Ls(HeadC, new Vector3(-0.245f + k * 0.07f, sy * 0.28f, z0 + 0.046f), 0.012f, dark);
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
            // knitted scarf: loops in three rows round the wrap, ribbed edges, knot and two fringed ends
            void Scarf(Material m, Material knitA, Material knitB, Material fringe)
            {
                Ring(n, Vector3.zero, 0.25f, 0.075f, m, default, new Vector3(1, 1.2f, 1));
                for (int row = 0; row < 3; row++)
                    for (int k = 0; k < 16; k++)
                    {
                        float a = (k + row * 0.5f) * Mathf.PI * 2 / 16, b = (row - 1) * 48 * FwRad;
                        float rr = 0.25f + 0.077f * Mathf.Cos(b);
                        var pos = new Vector3(Mathf.Cos(a) * rr, 0.077f * Mathf.Sin(b) * 1.2f, Mathf.Sin(a) * rr);
                        var nrm = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b) / 1.2f, Mathf.Sin(a) * Mathf.Cos(b));
                        var up = new Vector3(-Mathf.Cos(a) * Mathf.Sin(b), Mathf.Cos(b) * 1.2f, -Mathf.Sin(a) * Mathf.Sin(b));
                        FwKnitV(n, pos, nrm, up, 0.05f, row == 1 ? knitB : knitA);
                    }
                foreach (var sd in FwSd) FwRing(n, new Vector3(0, sd * 0.085f, 0), 0.277f, 0.012f, knitA, default, null, 28);
                Ls(n, new Vector3(0.09f, -0.02f, 0.3f), new Vector3(0.13f, 0.12f, 0.09f), m);
                foreach (var sd in FwSd) B(n, new Vector3(0.09f + sd * 0.035f, -0.02f, 0.34f), new Vector3(0.012f, 0.1f, 0.014f), knitA, new Vector3(0, 0, sd * 18));
                // two ends hanging over the chest (sway when running)
                for (int e = 0; e < 2; e++)
                {
                    float len = e == 0 ? 0.28f : 0.22f;
                    var end = Node(e == 0 ? "End" : "End2", n, e == 0 ? new Vector3(0.12f, -0.02f, 0.24f) : new Vector3(0.04f, -0.03f, 0.235f), e == 0 ? new Vector3(-22, 0, 8) : new Vector3(-18, 0, -6));
                    B(end, new Vector3(0, -len / 2, 0), new Vector3(0.12f, len, 0.05f), m);
                    int rows = e == 0 ? 4 : 3;
                    for (int r = 0; r < rows; r++) foreach (var sd in FwSd)
                        FwKnitV(end, new Vector3(sd * 0.028f, -0.045f - r * 0.06f, 0.027f), Vector3.forward, Vector3.up, 0.045f, r % 2 == 1 ? knitB : knitA);
                    for (int k = 0; k < 5; k++)
                    {
                        float x = -0.048f + k * 0.024f;
                        LRod(end, new Vector3(x, -len + 0.005f, 0), new Vector3(x * 1.1f, -len - 0.05f, 0.004f), 0.01f, fringe);
                        Ls(end, new Vector3(x * 1.1f, -len - 0.05f, 0.004f), 0.016f, fringe);
                    }
                    var sw = Add<Sway>(end.gameObject); sw.model = this; sw.amp = 6; sw.lift = -20; sw.axisB = Vector3.right; sw.phase = e * 1.3f;
                }
            }
            switch (set >= 0 ? 100 + set : i)
            {
                case 0: Scarf(M("FFD23A", 0.15f), M("E8B820", 0.15f), M("FFF0A0", 0.15f), M("FFD23A", 0.15f)); break;
                case 1: Scarf(Mats.Tex(Patterns.Stripes("E53935", "FFFFFF", 10), Color.white, 0.15f), M("E53935", 0.15f), M("FFFFFF", 0.15f), M("E53935", 0.15f)); break;
                case 2: Scarf(M("1F1F24", 0.1f), M("2E2E35", 0.1f), M("4A4A55", 0.1f), M("2E2E35", 0.1f)); break;
                case 3:   // bandit bandana: paisley print, white dot border, hems, creases, printed band, knot at the back
                {
                    var red = M("C62828", 0.2f); var red2 = M("9E1F1F", 0.2f); var white = M("FFFFFF", 0.2f); var black = M("1A1A1A", 0.2f);
                    Ring(n, Vector3.zero, 0.24f, 0.03f, red);
                    var tc = new Vector3(0, -0.02f, 0.24f); var tq = Quaternion.Euler(-20, 0, 0);
                    Msh(Meshes.Fan("band", new[] { new Vector2(-0.18f, 0), new Vector2(0.18f, 0), new Vector2(0, -0.2f) }, 0.02f), n, tc, Vector3.one, red, new Vector3(-20, 0, 0));
                    Vector3 T(float x, float y, float dz = 0.011f) => tc + tq * new Vector3(x, y, dz);
                    foreach (var sd in FwSd)
                    {
                        Dt.Stitches(n, t => T(sd * Mathf.Lerp(0.15f, 0.005f, t), Mathf.Lerp(-0.015f, -0.17f, t), 0.012f), 10, 0.016f, 0.005f, white);
                        for (int k = 0; k < 12; k++) { float t = k / 11f; Ls(n, T(sd * Mathf.Lerp(0.12f, 0.008f, t), Mathf.Lerp(-0.02f, -0.145f, t)), new Vector3(0.011f, 0.011f, 0.004f), white); }
                    }
                    foreach (var pc in new[] { new Vector2(-0.06f, -0.05f), new Vector2(0.06f, -0.05f), new Vector2(0, -0.11f) })
                    {
                        Ls(n, T(pc.x, pc.y), new Vector3(0.04f, 0.028f, 0.006f), white, new Vector3(-20, 0, 30));
                        Ls(n, T(pc.x + 0.006f, pc.y + 0.002f, 0.014f), new Vector3(0.014f, 0.012f, 0.004f), black);
                        Spike(n, T(pc.x - 0.014f, pc.y - 0.008f), T(pc.x - 0.03f, pc.y + 0.012f), 0.014f, white);
                    }
                    foreach (var fc in new[] { new Vector2(0, -0.035f), new Vector2(-0.035f, -0.09f) })
                    {
                        Ls(n, T(fc.x, fc.y, 0.013f), 0.01f, black);
                        for (int k = 0; k < 4; k++) Ls(n, T(fc.x + Mathf.Cos(k * Mathf.PI / 2) * 0.01f, fc.y + Mathf.Sin(k * Mathf.PI / 2) * 0.01f), new Vector3(0.01f, 0.01f, 0.004f), white);
                    }
                    for (int k = 0; k < 3; k++) FwBar(n, T(-0.05f + k * 0.05f, -0.01f, 0.0115f), T(-0.02f + k * 0.02f, -0.06f - (k % 2) * 0.03f, 0.0115f), 0.004f, 0.002f, red2, tq * Vector3.forward);
                    // printed band and folds
                    for (int k = 0; k < 20; k++) { float a = (k + 0.5f) * Mathf.PI * 2 / 20; Ls(n, new Vector3(Mathf.Cos(a) * 0.268f, 0, Mathf.Sin(a) * 0.268f), new Vector3(0.012f, 0.012f, 0.012f), white); }
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * Mathf.PI * 2 / 10; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        B(n, d * 0.265f, new Vector3(0.008f, 0.05f, 0.008f), red2).transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 0, 30);
                    }
                    // knot at the back of the neck
                    var kb = new Vector3(0, 0, -0.27f);
                    Ls(n, kb, new Vector3(0.07f, 0.06f, 0.05f), red);
                    foreach (var sd in FwSd)
                    {
                        Ls(n, kb + new Vector3(sd * 0.05f, 0.015f, -0.01f), new Vector3(0.06f, 0.035f, 0.025f), red, new Vector3(0, 0, sd * 20));
                        var tp = kb + new Vector3(sd * 0.025f, -0.07f, -0.02f);
                        B(n, tp, new Vector3(0.04f, 0.1f, 0.012f), red, new Vector3(8, 0, sd * 12));
                        foreach (var dy in new[] { 0.02f, -0.02f }) Ls(n, tp + new Vector3(sd * 0.004f * (dy > 0 ? -1 : 1), dy, -0.008f), new Vector3(0.01f, 0.01f, 0.004f), white);
                        foreach (var dy in new[] { 0.035f, -0.035f }) B(n, tp + new Vector3(-sd * 0.017f, dy, -0.007f), new Vector3(0.003f, 0.016f, 0.003f), white, new Vector3(8, 0, sd * 12));
                    }
                    break;
                }
                case 4:   // bow tie: pleated polka-dot wings with stitched rims, knot, white shirt collar
                {
                    var red = M("D81B60", 0.5f); var red2 = M("AD1457", 0.5f); var dot = M("FFFFFF", 0.4f); var shirt = M("FFFFFF", 0.3f); var thread = M("F8BBD0", 0.3f); var seam = M("D0D0D0", 0.3f);
                    foreach (var sd in FwSd)
                    {
                        var c = new Vector3(sd * 0.11f, -0.04f, 0.25f); var q = Quaternion.Euler(0, 0, sd * 90);
                        Cone(n, c, new Vector3(0.14f, 0.11f, 0.06f), red, new Vector3(0, 0, sd * 90));
                        FwAt(Cone(n, new Vector3(0, 0.035f, 0.01f), new Vector3(0.1f, 0.075f, 0.045f), red2), c, q);
                        Vector3 W(float h, float ph, float grow = 1.04f) { float f = 1 - h / 0.11f; return new Vector3(0.07f * f * Mathf.Cos(ph * FwRad) * grow, h, 0.03f * f * Mathf.Sin(ph * FwRad) * grow); }
                        foreach (var ph in new[] { 0f, 180f }) FwAt(LRod(n, W(0.003f, ph), W(0.1f, ph), 0.012f, red2), c, q);
                        for (int k = 0; k < 6; k++) { float ph = 35 + k * 22; FwAt(LRod(n, W(0.006f, ph, 1.08f), W(0.08f, ph, 1.15f), 0.007f, red2), c, q); }
                        FwAt(FwRing(n, Vector3.zero, 1f, 0.15f, red2, default, new Vector3(0.07f, 0.08f, 0.03f), 20), c, q);
                        for (int r = 0; r < 4; r++) for (int k = 0; k < 3; k++) FwAt(Ls(n, W(0.016f + r * 0.02f, 60 + k * 30 + (r % 2) * 15, 1.06f), new Vector3(0.014f, 0.014f, 0.005f), dot), c, q);
                        Dt.Stitches(n, t => c + q * (W(0.012f, Mathf.Lerp(10, 170, t), 1.08f)), 12, 0.012f, 0.004f, thread);
                        // collar point (white shirt) peeking behind the wing
                        Msh(Meshes.Fan("fwcollar", new[] { new Vector2(0, 0), new Vector2(0.1f, -0.03f), new Vector2(0.03f, -0.1f) }, 0.012f), n, new Vector3(sd * 0.02f, 0.02f, 0.232f), new Vector3(sd, 1, 1), shirt, new Vector3(-30, 0, 0));
                        var cq = Quaternion.Euler(-30, 0, 0); var c0 = new Vector3(sd * 0.02f, 0.02f, 0.232f);
                        Dt.Stitches(n, t => c0 + cq * new Vector3(sd * Mathf.Lerp(0.012f, 0.088f, t), Mathf.Lerp(-0.005f, -0.03f, t), 0.007f), 5, 0.012f, 0.003f, seam);
                        Ls(n, c0 + cq * new Vector3(sd * 0.035f, -0.085f, 0.008f), new Vector3(0.012f, 0.012f, 0.006f), seam);
                    }
                    S(n, new Vector3(0, -0.04f, 0.26f), 0.06f, red);
                    for (int k = 0; k < 3; k++) LRod(n, new Vector3(-0.015f + k * 0.015f, -0.01f, 0.282f), new Vector3(-0.012f + k * 0.012f, -0.07f, 0.282f), 0.006f, red2);
                    Dt.Stitches(n, t => new Vector3(Mathf.Lerp(-0.022f, 0.022f, t), -0.012f, 0.284f), 4, 0.008f, 0.003f, thread);
                    FwRing(n, new Vector3(0, 0.012f, 0), 0.235f, 0.022f, shirt, default, new Vector3(1, 1.6f, 1), 28);
                    Dt.Stitches(n, t => Dt.Circle(new Vector3(0, -0.004f, 0), 0.258f, 0.258f, 20, 160)(t), 12, 0.014f, 0.004f, seam);
                    FwRing(n, new Vector3(0, -0.04f, 0), 0.24f, 0.01f, red, default, null, 28);
                    B(n, new Vector3(0, -0.04f, -0.25f), new Vector3(0.05f, 0.025f, 0.012f), M("C9D0D8", 0.8f, 0.8f));
                    foreach (var sd in FwSd) Ls(n, new Vector3(sd * 0.03f, -0.04f, -0.252f), 0.014f, M("C9D0D8", 0.8f, 0.8f));
                    break;
                }
                case 5:   // knight cape: studded gold collar, gem clasps with chain, trimmed cape with crest and tassels
                {
                    var goldM = M("FFC83A", 0.8f, 0.8f); var red = M("B71C1C", 0.2f); var red2 = M("8E1515", 0.2f); var gem = G("3FA9FF", 1.8f); var white = M("F4F4F4", 0.3f);
                    Ring(n, Vector3.zero, 0.24f, 0.04f, goldM);
                    for (int k = 0; k < 16; k++) { float a = (k + 0.5f) * Mathf.PI * 2 / 16; Ls(n, new Vector3(Mathf.Cos(a) * 0.24f, 0.036f, Mathf.Sin(a) * 0.24f), 0.018f, goldM); }
                    var cps = new Vector3[2];
                    foreach (var sd in FwSd)
                    {
                        var cp = new Vector3(sd * 0.175f, -0.015f, 0.19f); var cn = new Vector3(sd * 0.6f, 0.15f, 0.8f).normalized; var cq = Quaternion.FromToRotation(Vector3.up, cn);
                        cps[(sd + 1) / 2] = cp;
                        Lc(n, cp, new Vector3(0.07f, 0.008f, 0.07f), goldM).transform.localRotation = cq;
                        FwRing(n, cp + cn * 0.006f, 0.034f, 0.006f, goldM).transform.localRotation = cq;
                        FwGem(n, cp + cn * 0.008f, 0.032f, gem, goldM, cn);
                    }
                    FwChain(n, t => Vector3.Lerp(cps[0], cps[1], t) + new Vector3(0, -Mathf.Sin(t * Mathf.PI) * 0.07f, Mathf.Sin(t * Mathf.PI) * 0.075f + 0.012f), 11, 0.012f, 0.004f, goldM, q => Vector3.forward);
                    var cape = Node("Cape", n, new Vector3(0, 0.0f, -0.255f), new Vector3(16, 0, 0));
                    B(cape, new Vector3(0, -0.36f, 0), new Vector3(0.62f, 0.72f, 0.025f), red);
                    foreach (var sd in FwSd) B(cape, new Vector3(sd * 0.3f, -0.36f, -0.006f), new Vector3(0.03f, 0.72f, 0.03f), goldM);
                    B(cape, new Vector3(0, -0.71f, -0.006f), new Vector3(0.62f, 0.03f, 0.03f), goldM);
                    Lc(cape, new Vector3(0, -0.02f, -0.012f), new Vector3(0.05f, 0.31f, 0.05f), red2, new Vector3(0, 0, 90));
                    for (int k = 0; k < 8; k++) Ls(cape, new Vector3(-0.245f + k * 0.07f, -0.02f, -0.036f), 0.016f, goldM);
                    for (int k = 0; k < 10; k++) Ls(cape, new Vector3(-0.27f + k * 0.06f, -0.675f, -0.016f), 0.018f, goldM);
                    foreach (var sd in FwSd) Dt.Stitches(cape, Dt.Line(new Vector3(sd * 0.265f, -0.07f, -0.014f), new Vector3(sd * 0.265f, -0.64f, -0.014f)), 10, 0.025f, 0.006f, goldM);
                    for (int k = 0; k < 5; k++) B(cape, new Vector3(-0.2f + k * 0.1f, -0.42f, -0.016f), new Vector3(0.02f, 0.5f, 0.012f), red2);
                    var crest = new Vector3(0, -0.28f, -0.026f);
                    Msh(Meshes.Fan("fwcrest", new[] { new Vector2(-0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(0.08f, -0.02f), new Vector2(0, -0.1f), new Vector2(-0.08f, -0.02f) }, 0.012f), cape, crest, Vector3.one, goldM);
                    B(cape, crest + new Vector3(0, 0, -0.008f), new Vector3(0.025f, 0.14f, 0.006f), white);
                    B(cape, crest + new Vector3(0, 0.025f, -0.008f), new Vector3(0.12f, 0.025f, 0.006f), white);
                    foreach (var sx in FwSd) foreach (var sy in FwSd) Ls(cape, crest + new Vector3(sx * 0.042f, 0.025f + sy * 0.035f, -0.008f), 0.016f, gem);
                    for (int k = 0; k < 6; k++)
                    {
                        float x = -0.25f + k * 0.1f;
                        Spike(cape, new Vector3(x, -0.72f, -0.006f), new Vector3(x, -0.79f, -0.006f), 0.07f, goldM);
                        Ls(cape, new Vector3(x, -0.8f, -0.006f), 0.024f, goldM);
                    }
                    var sw = Add<Sway>(cape.gameObject); sw.model = this; sw.amp = 3; sw.lift = 35; sw.axisA = Vector3.forward; sw.axisB = Vector3.right;
                    break;
                }
                case 6:   // spiked collar: stitched leather, chrome spikes on washers, studs, buckle at the back, D-ring tag
                {
                    var leather = M("151515", 0.5f); var chrome = M("D9DEE6", 0.9f, 0.9f); var thread = M("8A8A8A", 0.2f); var goldM = M("FFC83A", 0.85f, 0.8f); var edge = M("2A2A2A", 0.4f);
                    Ring(n, new Vector3(0, -0.01f, 0), 0.25f, 0.04f, leather);
                    FwRing(n, new Vector3(0, 0.023f, 0), 0.273f, 0.008f, edge, default, null, 28);
                    FwRing(n, new Vector3(0, -0.043f, 0), 0.273f, 0.008f, edge, default, null, 28);
                    foreach (var sy in FwSd) Dt.Stitches(n, FwLoop(Dt.Circle(new Vector3(0, -0.01f + sy * 0.023f, 0), 0.287f, 0.287f), 28), 28, 0.018f, 0.005f, thread);
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * Mathf.PI * 2 / 10;
                        var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        Lc(n, d * 0.288f, new Vector3(0.05f, 0.006f, 0.05f), chrome).transform.localRotation = Quaternion.FromToRotation(Vector3.up, d);
                        Spike(n, d * 0.27f, d * 0.36f, 0.06f, chrome);
                        var d2 = new Vector3(Mathf.Cos(a + Mathf.PI / 10), 0, Mathf.Sin(a + Mathf.PI / 10));
                        Ls(n, d2 * 0.289f + new Vector3(0, 0.008f, 0), 0.014f, chrome);
                        Ls(n, d2 * 0.289f + new Vector3(0, -0.028f, 0), 0.014f, chrome);
                    }
                    var bc = new Vector3(0, -0.01f, -0.292f); var bq = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    foreach (var sd in FwSd)
                    {
                        FwAt(B(n, new Vector3(0, sd * 0.035f, 0), new Vector3(0.07f, 0.01f, 0.01f), chrome), bc, bq);
                        FwAt(B(n, new Vector3(sd * 0.035f, 0, 0), new Vector3(0.01f, 0.07f, 0.01f), chrome), bc, bq);
                    }
                    FwAt(B(n, new Vector3(0.012f, 0, 0.008f), new Vector3(0.04f, 0.006f, 0.006f), chrome), bc, bq);
                    FwAt(B(n, new Vector3(-0.055f, 0, 0.002f), new Vector3(0.012f, 0.085f, 0.012f), leather), bc, bq);
                    for (int k = 0; k < 3; k++) FwAt(Ls(n, new Vector3(0.06f + k * 0.025f, 0, 0.002f), new Vector3(0.012f, 0.012f, 0.006f), edge), bc, bq);
                    FwAt(B(n, new Vector3(-0.08f, 0, 0.006f), new Vector3(0.04f, 0.06f, 0.008f), leather), bc, bq);
                    FwRing(n, new Vector3(0, -0.07f, 0.292f), 0.022f, 0.005f, goldM, new Vector3(90, 0, 0));
                    Lc(n, new Vector3(0, -0.118f, 0.29f), new Vector3(0.05f, 0.004f, 0.05f), goldM, new Vector3(90, 0, 0));
                    Msh(Meshes.Star(), n, new Vector3(0, -0.118f, 0.295f), new Vector3(0.03f, 0.03f, 0.01f), leather);
                    break;
                }
                case 7:   // fur collar: fluffy puffs with fur strands and white fluff, gold clasp with a little chain
                {
                    var fur = M("F5EEDC", 0.05f); var fur2 = M("E6D5B8", 0.05f); var fur3 = M("FFFFFF", 0.05f); var goldM = M("FFC83A", 0.85f, 0.8f);
                    var ball = Meshes.Ball(14, 9);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k * Mathf.PI * 2 / 14;
                        Msh(ball, n, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.26f, Vector3.one * 0.15f, fur);
                    }
                    var rng = new Rng(77);
                    Vector3 On(float a, float b, float grow) => new Vector3(Mathf.Cos(a) * (0.26f + 0.072f * grow * Mathf.Cos(b)), 0.072f * grow * Mathf.Sin(b), Mathf.Sin(a) * (0.26f + 0.072f * grow * Mathf.Cos(b)));
                    for (int k = 0; k < 84; k++)
                    {
                        float a = rng.F(0, Mathf.PI * 2), b = rng.F(-60, 85) * FwRad;
                        var d = (new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b)) + new Vector3(rng.F(-0.3f, 0.3f), rng.F(-0.1f, 0.3f), rng.F(-0.3f, 0.3f))).normalized;
                        var p0 = On(a, b, 0.9f);
                        FwTuft(n, p0, p0 + d * rng.F(0.04f, 0.065f), 0.024f, k % 3 == 0 ? fur2 : fur);
                    }
                    for (int k = 0; k < 20; k++) { float a = rng.F(0, Mathf.PI * 2), b = rng.F(-30, 70) * FwRad; Ls(n, On(a, b, 1.0f), rng.F(0.045f, 0.065f), fur3); }
                    var cf = new Vector3(0, -0.01f, 0.335f);
                    foreach (var sd in FwSd)
                    {
                        Lc(n, cf + new Vector3(sd * 0.035f, 0, 0), new Vector3(0.04f, 0.006f, 0.04f), goldM, new Vector3(90, 0, 0));
                        FwRing(n, cf + new Vector3(sd * 0.035f, 0, 0.007f), 0.016f, 0.004f, goldM, new Vector3(90, 0, 0));
                    }
                    FwChain(n, t => cf + new Vector3(Mathf.Lerp(-0.022f, 0.022f, t), -Mathf.Sin(t * Mathf.PI) * 0.012f, 0.01f), 3, 0.008f, 0.0025f, goldM, q => Vector3.forward);
                    Ls(n, cf + new Vector3(0, -0.012f, 0.016f), 0.014f, G("FF5C7A", 1.6f));
                    break;
                }
                case 105:   // diamond chain: Cuban links with pavé, fine second strand, framed spinning star pendant, clasp, sparkles
                {
                    var gm = M("FFC83A", 0.9f, 0.9f); var dia = G("E8FDFF", 2.2f); var ice = G("BFF3FF", 2f); var sparkle = Add("FFFFFF", 0.8f);
                    System.Func<float, Vector3> Neckline(float drop, float extra) => t =>
                    {
                        float a = -Mathf.PI / 2 + t * Mathf.PI * 2, s = Mathf.Sin(a);
                        float y = -0.05f - Mathf.Pow(Mathf.Max(0, s), 1.5f) * drop;
                        var r = FwNeckR(y);
                        return new Vector3(Mathf.Cos(a) * (r.x + extra), y, s * (r.y + extra));
                    };
                    System.Func<Vector3, Vector3> face = q => (new Vector3(q.x, 0, q.z).normalized + Vector3.up * 0.4f).normalized;
                    var main = FwLoop(Neckline(0.12f, 0.03f), 30);
                    FwChain(n, main, 30, 0.016f, 0.006f, gm, face);
                    Dt.Path(main, 30, (pos, dir, k) => { if (k % 2 == 0) Ls(n, pos + face(pos) * 0.008f, 0.014f, dia); });
                    FwChain(n, FwLoop(Neckline(0.2f, 0.024f), 36), 36, 0.011f, 0.0035f, gm, face);
                    // pendant: bail, frame of stones and a spinning star diamond
                    var top = Neckline(0.12f, 0.03f)(0.5f);
                    var pc = top + new Vector3(0, -0.105f, 0.01f);
                    FwRing(n, top + new Vector3(0, -0.022f, 0.004f), 0.016f, 0.005f, gm, new Vector3(0, 0, 90));
                    B(n, top + new Vector3(0, -0.032f, 0.006f), new Vector3(0.016f, 0.02f, 0.01f), gm);
                    Ls(n, top + new Vector3(0, -0.032f, 0.013f), 0.012f, dia);
                    FwRing(n, pc, 0.085f, 0.008f, gm, new Vector3(90, 0, 0));
                    for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; Ls(n, pc + new Vector3(Mathf.Cos(a) * 0.085f, Mathf.Sin(a) * 0.085f, 0.008f), 0.014f, dia); }
                    var pn = Node("Pendant", n, pc);
                    Add<Spin>(pn.gameObject).axis = Vector3.forward;
                    var star = Meshes.Star(8, 0.6f);
                    Msh(star, pn, Vector3.zero, Vector3.one * 0.13f, ice);
                    Msh(star, pn, new Vector3(0, 0, 0.014f), new Vector3(0.065f, 0.065f, 0.08f), dia);
                    for (int k = 0; k < 16; k++) { float a = Mathf.PI / 2 + k * Mathf.PI / 8, r = k % 2 == 0 ? 0.062f : 0.036f; Ls(pn, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.017f), k % 2 == 0 ? 0.016f : 0.011f, dia); }
                    Ls(pn, new Vector3(0, 0, 0.024f), 0.022f, G("FFFFFF", 2.5f));
                    // lobster clasp at the back
                    var back = Neckline(0.12f, 0.03f)(0);
                    Ls(n, back + new Vector3(0.02f, 0, -0.004f), new Vector3(0.034f, 0.022f, 0.016f), gm);
                    B(n, back + new Vector3(0.042f, 0, -0.004f), new Vector3(0.012f, 0.008f, 0.008f), gm);
                    FwRing(n, back + new Vector3(-0.012f, 0, -0.004f), 0.01f, 0.003f, gm, new Vector3(90, 0, 0));
                    for (int k = 0; k < 5; k++)
                    {
                        var sp = main(0.3f + k * 0.1f) + face(main(0.3f + k * 0.1f)) * 0.02f;
                        var gq = Quaternion.LookRotation(face(sp));
                        FwAt(B(n, Vector3.zero, new Vector3(0.006f, 0.04f, 0.003f), sparkle), sp, gq);
                        FwAt(B(n, Vector3.zero, new Vector3(0.04f, 0.006f, 0.003f), sparkle), sp, gq);
                    }
                    break;
                }
            }
        }
    }
}
