using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Wings and shoulder pads, backpacks, tails. Every item is assembled from 100+ detail parts.</summary>
    public partial class ChubrikModel
    {
        // ================= back-wear kit (Bw) =================

        /// <summary>Smooth medium ball (204 verts) for main shapes smaller than the torso.</summary>
        static GameObject BwBall(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Ball(16, 11), p, pos, sc, m, euler);
        /// <summary>Tiny ball (48 verts): beads, fluff, sparks.</summary>
        static GameObject BwDot(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(7, 5), p, pos, Vector3.one * d, m);
        /// <summary>Rivet / stud head: tiny ball flattened along the surface normal n.</summary>
        static GameObject BwRivet(Transform p, Vector3 pos, float d, Material m, Vector3 n)
        {
            var go = Msh(Meshes.Ball(7, 5), p, pos, new Vector3(d, d * 0.55f, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
            return go;
        }
        /// <summary>Low-poly torus (15×6) in the XZ plane.</summary>
        static GameObject BwRing(Transform p, Vector3 pos, float R, float r, Material m, Vector3 euler = default, Vector3? sc = null) => Msh(Meshes.Torus(R, r, 15, 6), p, pos, sc ?? Vector3.one, m, euler);
        /// <summary>Low-poly torus around the axis.</summary>
        static GameObject BwRingN(Transform p, Vector3 pos, float R, float r, Material m, Vector3 axis)
        {
            var go = BwRing(p, pos, R, r, m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            return go;
        }
        /// <summary>Rotation with local +Y along up and local +Z towards face.</summary>
        static Quaternion BwRot(Vector3 up, Vector3 face)
        {
            up = up.normalized;
            var f = face - up * Vector3.Dot(face, up);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.Cross(up, Mathf.Abs(up.x) < 0.9f ? Vector3.right : Vector3.forward);
            return Quaternion.LookRotation(f.normalized, up);
        }
        /// <summary>Box from a to b (width w, thickness th towards face).</summary>
        static GameObject BwBar(Transform p, Vector3 a, Vector3 b, float w, float th, Material m, Vector3 face)
        {
            var go = B(p, (a + b) / 2, new Vector3(w, (b - a).magnitude, th), m);
            go.transform.localRotation = BwRot(b - a, face);
            return go;
        }
        /// <summary>Low-poly cone (8 sides) from base a to tip b.</summary>
        static GameObject BwCone(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Msh(Meshes.Cone(8), p, a, new Vector3(d, (b - a).magnitude, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }
        /// <summary>Fluff balls scattered over an ellipsoid (theta from +Y, th0..th1).</summary>
        static void BwFur(Transform p, Vector3 c, Vector3 r, int n, float d, Material m, int seed, float th0 = 0, float th1 = 180)
        {
            var g = new Rng(seed);
            float c0 = Mathf.Cos(th0 * Mathf.Deg2Rad), c1 = Mathf.Cos(th1 * Mathf.Deg2Rad);
            for (int k = 0; k < n; k++)
                BwDot(p, Dt.Ell(c, r, Mathf.Acos(Mathf.Lerp(c0, c1, g.F())) * Mathf.Rad2Deg, g.F(0, 360)), d * g.F(0.75f, 1.2f), m);
        }
        /// <summary>Pointed fur tufts standing out of an ellipsoid (bent towards +Y by bias).</summary>
        static void BwTufts(Transform p, Vector3 c, Vector3 r, int n, float len, float d, Material m, int seed, float th0 = 0, float th1 = 180, float bias = 0.6f)
        {
            var g = new Rng(seed);
            float c0 = Mathf.Cos(th0 * Mathf.Deg2Rad), c1 = Mathf.Cos(th1 * Mathf.Deg2Rad);
            for (int k = 0; k < n; k++)
            {
                float th = Mathf.Acos(Mathf.Lerp(c0, c1, g.F())) * Mathf.Rad2Deg, ph = g.F(0, 360);
                var a = Dt.Ell(c, r, th, ph, 0.92f);
                BwCone(p, a, a + (Dt.EllN(r, th, ph) + Vector3.up * bias).normalized * len * g.F(0.8f, 1.2f), d, m);
            }
        }
        /// <summary>Feather lying in the XY plane of p from a to tip: rounded vane, shaft and barb stripes on the bz side.</summary>
        static void BwFeather(Transform p, Vector2 a, Vector2 tip, float wd, float z, float bz, Material vane, Material shaft, Material barb, int barbs)
        {
            var A = new Vector3(a.x, a.y, z); var d = new Vector3(tip.x - a.x, tip.y - a.y, 0);
            float len = d.magnitude; var q = Quaternion.LookRotation(Vector3.forward, d);
            Ls(p, A + d * 0.5f, new Vector3(wd, len, 0.014f), vane).transform.localRotation = q;
            B(p, A + d * 0.45f + new Vector3(0, 0, bz * 0.0075f), new Vector3(0.007f, len * 0.9f, 0.005f), shaft).transform.localRotation = q;
            for (int k = 0; k < barbs; k++)
            {
                var c = A + d * (barbs == 1 ? 0.55f : 0.35f + 0.4f * k / (barbs - 1)) + new Vector3(0, 0, bz * 0.0078f);
                foreach (var sd in new[] { -1f, 1f })
                {
                    var side = q * Vector3.right * sd;
                    B(p, c + side * wd * 0.2f, new Vector3(0.005f, wd * 0.3f, 0.004f), barb).transform.localRotation = Quaternion.LookRotation(Vector3.forward, side + d.normalized * 0.8f);
                }
            }
        }
        /// <summary>Moves the parts of a helper frame node into its parent (in place) so they bake together with the parent's parts.</summary>
        static void BwFlat(Transform t)
        {
            var p = t.parent;
            for (int k = t.childCount - 1; k >= 0; k--)
            {
                var c = t.GetChild(k);
                var lp = t.localPosition + t.localRotation * c.localPosition; var lr = t.localRotation * c.localRotation;
                c.SetParent(p, false); c.localPosition = lp; c.localRotation = lr;
            }
            t.gameObject.SetActive(false); Destroy(t.gameObject);
        }
        /// <summary>Point on the torso surface in direction d from its centre (Torso space) and the outward normal there.</summary>
        static Vector3 BwSurf(Vector3 d, float grow, out Vector3 n)
        {
            var r = Dt.TorsoR;
            var p = d / Mathf.Sqrt(d.x * d.x / (r.x * r.x) + d.y * d.y / (r.y * r.y) + d.z * d.z / (r.z * r.z));
            n = new Vector3(p.x / (r.x * r.x), p.y / (r.y * r.y), p.z / (r.z * r.z)).normalized;
            return Dt.TorsoC + p * grow;
        }
        /// <summary>Webbing band lying on the torso along dir(t) (directions from the torso centre): n segments + stitched edges.</summary>
        void BwBand(System.Func<float, Vector3> dir, int n, float w, float th, Material m, Material stitch, float grow = 1.06f)
        {
            for (int k = 0; k < n; k++)
            {
                var a = BwSurf(dir(k / (float)n), grow, out var na); var b = BwSurf(dir((k + 1) / (float)n), grow, out var nb);
                var q = BwRot(b - a, na + nb); var c = (a + b) / 2; float len = (b - a).magnitude;
                B(Torso, c, new Vector3(w, len * 1.1f, th), m).transform.localRotation = q;
                if (stitch == null) continue;
                foreach (var sd in new[] { -1f, 1f })
                    B(Torso, c + q * new Vector3(sd * w * 0.36f, 0, th * 0.5f), new Vector3(0.005f, len * 0.55f, 0.004f), stitch).transform.localRotation = q;
            }
        }
        /// <summary>Shoulder strap path: from the upper back over the shoulder down the chest.</summary>
        static Vector3 BwStrapDir(float sd, float t) { float a = Mathf.Lerp(-70, 112, t) * Mathf.Deg2Rad; return new Vector3(sd, Mathf.Cos(a), Mathf.Sin(a)); }
        /// <summary>Backpack shoulder straps with adjuster buckles and a sternum strap (58 parts with stitches).</summary>
        void BwStraps(Material strap, Material stitch, Material buckle, bool sternum = true)
        {
            foreach (var sd in new[] { -1f, 1f })
            {
                BwBand(t => BwStrapDir(sd, t), 8, 0.06f, 0.014f, strap, stitch);
                var p = BwSurf(BwStrapDir(sd, 0.84f), 1.1f, out var n); var q = BwRot(BwSurf(BwStrapDir(sd, 0.9f), 1.1f, out _) - p, n);
                B(Torso, p, new Vector3(0.075f, 0.032f, 0.012f), buckle).transform.localRotation = q;
                B(Torso, p + q * new Vector3(0, -0.004f, 0.007f), new Vector3(0.062f, 0.008f, 0.006f), buckle).transform.localRotation = q;
            }
            if (!sternum) return;
            var l = BwStrapDir(-1, 0.72f); var r = BwStrapDir(1, 0.72f);
            BwBand(t => Vector3.Lerp(l, r, t), 4, 0.028f, 0.01f, strap, null, 1.09f);
            var c = BwSurf(Vector3.Lerp(l, r, 0.5f), 1.12f, out var cn); var cq = BwRot(Vector3.up, cn);
            B(Torso, c, new Vector3(0.05f, 0.036f, 0.014f), buckle).transform.localRotation = cq;
            B(Torso, c + cq * new Vector3(0, 0, 0.008f), new Vector3(0.018f, 0.018f, 0.008f), buckle).transform.localRotation = cq;
        }

        // ================= wings & shoulders =================

        void Wings(int i, int set)
        {
            var w = Node("Wings", Torso, new Vector3(0, 0.36f, -0.25f));
            Vector2[] angel = { new Vector2(0, 0), new Vector2(0.15f, 0.18f), new Vector2(0.4f, 0.32f), new Vector2(0.62f, 0.36f), new Vector2(0.72f, 0.26f), new Vector2(0.62f, 0.13f), new Vector2(0.67f, 0.02f), new Vector2(0.55f, -0.05f), new Vector2(0.57f, -0.16f), new Vector2(0.43f, -0.19f), new Vector2(0.41f, -0.3f), new Vector2(0.26f, -0.27f), new Vector2(0.1f, -0.12f) };
            Vector2[] dragon = { new Vector2(0, 0), new Vector2(0.2f, 0.26f), new Vector2(0.5f, 0.44f), new Vector2(0.78f, 0.46f), new Vector2(0.7f, 0.22f), new Vector2(0.64f, 0.04f), new Vector2(0.52f, 0.14f), new Vector2(0.43f, -0.1f), new Vector2(0.31f, 0.02f), new Vector2(0.2f, -0.2f), new Vector2(0.08f, -0.05f) };
            // feather layout of the angel outline: primaries fan from the wrist, secondaries hang from the arm
            Vector2[] P0 = { new Vector2(0.5f, 0.31f), new Vector2(0.48f, 0.28f), new Vector2(0.45f, 0.25f), new Vector2(0.41f, 0.21f), new Vector2(0.37f, 0.17f) };
            Vector2[] P1 = { new Vector2(0.84f, 0.27f), new Vector2(0.82f, 0.12f), new Vector2(0.76f, -0.02f), new Vector2(0.67f, -0.15f), new Vector2(0.56f, -0.26f) };
            Vector2[] S0 = { new Vector2(0.33f, 0.14f), new Vector2(0.27f, 0.12f), new Vector2(0.21f, 0.1f), new Vector2(0.15f, 0.07f), new Vector2(0.09f, 0.04f) };
            Vector2[] S1 = { new Vector2(0.45f, -0.33f), new Vector2(0.35f, -0.34f), new Vector2(0.25f, -0.3f), new Vector2(0.16f, -0.22f), new Vector2(0.08f, -0.13f) };

            // flapping pivot + holder (holder: x = outwards, wing plane XY; the side seen from behind is z * -sd)
            Transform Wing(float sd, float x, float yaw, float idle = 6)
            {
                var piv = Node("Wing", w, new Vector3(sd * x, 0, 0));
                var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.baseYaw = yaw; f.idleAmp = idle;
                return Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
            }
            void Covert(Transform h, Vector2 c, Vector2 size, float z, Material m)
                => Ls(h, new Vector3(c.x, c.y, z), new Vector3(size.x, size.y, 0.016f), m, new Vector3(0, 0, 20));
            // harness plate between the wing roots (seen from behind)
            void Mount(Material plate, Material metal, Material gem)
            {
                BwBall(w, new Vector3(0, 0, -0.012f), new Vector3(0.2f, 0.14f, 0.05f), plate);
                foreach (var sd in new[] { -1f, 1f })
                {
                    Lc(w, new Vector3(sd * 0.08f, 0, -0.02f), new Vector3(0.04f, 0.045f, 0.04f), metal);
                    BwDot(w, new Vector3(sd * 0.08f, 0.05f, -0.02f), 0.03f, metal);
                }
                for (int k = 0; k < 6; k++) { float a = (k + 0.5f) * 60 * Mathf.Deg2Rad; BwRivet(w, new Vector3(Mathf.Cos(a) * 0.085f, Mathf.Sin(a) * 0.058f, -0.03f), 0.016f, metal, Vector3.back); }
                Dt.Gem(w, new Vector3(0, 0, -0.038f), 0.045f, gem, metal, Vector3.back);
            }
            // feathered wing pair: membrane + inner layer, leading edge, 5 primaries, 5 secondaries, 3 rows of coverts (~72 parts a wing)
            void Feathered(float s, Material mem, Material inner, Material pri, Material sec, Material cov, Material shaft, Material barb, System.Action<Transform, float, float> extra = null, Material[] priBarb = null)
            {
                foreach (var sd in new[] { -1f, 1f })
                {
                    var h = Wing(sd, 0.08f, 18); float bz = -sd;
                    Msh(Meshes.Wing("angel", angel), h, Vector3.zero, new Vector3(s, s, 1), mem);
                    Msh(Meshes.Wing("angel", angel), h, new Vector3(0.04f, 0.03f, 0.02f * bz), new Vector3(s * 0.7f, s * 0.7f, 1), inner);
                    var e0 = new Vector3(0.02f, 0.02f, 0) * s; var e1 = new Vector3(0.3f, 0.3f, 0) * s; var e2 = new Vector3(0.62f, 0.355f, 0) * s;
                    LRod(h, e0, e1, 0.04f * s, cov); LRod(h, e1, e2, 0.032f * s, cov);
                    Ls(h, e1, 0.048f * s, cov); Ls(h, e2, 0.04f * s, cov);
                    for (int k = 0; k < 5; k++) BwFeather(h, P0[k] * s, P1[k] * s, 0.09f * s, 0.04f * bz, bz, pri, shaft, priBarb != null ? priBarb[k] : barb, 2);
                    for (int k = 0; k < 5; k++) BwFeather(h, S0[k] * s, S1[k] * s, 0.085f * s, 0.047f * bz, bz, sec, shaft, barb, 1);
                    for (int k = 0; k < 6; k++) Covert(h, Vector2.Lerp(new Vector2(0.1f, 0.03f), new Vector2(0.56f, 0.27f), k / 5f) * s, new Vector2(0.09f, 0.13f) * s, 0.06f * bz, cov);
                    for (int k = 0; k < 5; k++) Covert(h, Vector2.Lerp(new Vector2(0.12f, -0.04f), new Vector2(0.46f, 0.16f), k / 4f) * s, new Vector2(0.1f, 0.15f) * s, 0.053f * bz, cov);
                    for (int k = 0; k < 5; k++) Covert(h, Vector2.Lerp(new Vector2(0.1f, 0.02f), new Vector2(0.5f, 0.24f), k / 4f) * s, new Vector2(0.1f, 0.14f) * s, -0.025f * bz, cov);
                    extra?.Invoke(h, bz, s);
                }
                CanGlide = true;
            }
            // shoulder pad dome tilted outwards (frame node, merged into "Shoulder" afterwards)
            Transform Pad(float sd, out Transform sh)
            {
                sh = Node("Shoulder", Torso, new Vector3(sd * 0.3f, 0.42f, 0));
                return Node("Pad", sh, Vector3.zero, new Vector3(0, 0, -sd * 28));
            }

            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    var white = M("FFFFFF", 0.2f); var gold = M("E8C25A", 0.75f, 0.6f);
                    Feathered(1f, white, M("EEF4FF", 0.2f), white, M("F7FAFF", 0.2f), white, gold, M("D6E2F2", 0.2f));
                    Mount(gold, gold, G("8FD8FF", 1.6f));
                    break;
                }
                case 1:
                {
                    var mem = M("8E1B1B", 0.4f); var bone = M("3A1010", 0.4f); var vein = M("5E1010", 0.3f); var claw = M("F2E6C8", 0.6f); var scl = M("B8342A", 0.5f);
                    Vector2 sh0 = new Vector2(0.02f, 0.02f), el = new Vector2(0.21f, 0.27f), wr = new Vector2(0.49f, 0.43f);
                    Vector2[] tips = { new Vector2(0.78f, 0.46f), new Vector2(0.64f, 0.04f), new Vector2(0.43f, -0.1f), new Vector2(0.2f, -0.2f) };
                    Vector2[] valleys = { new Vector2(0.7f, 0.22f), new Vector2(0.52f, 0.14f), new Vector2(0.31f, 0.02f), new Vector2(0.08f, -0.05f) };
                    Vector3 V(Vector2 v, float z = 0) => new Vector3(v.x, v.y, z);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var h = Wing(sd, 0.08f, 18); float bz = -sd;
                        Msh(Meshes.Wing("dragon", dragon), h, Vector3.zero, Vector3.one, mem);
                        // arm: shoulder → elbow → wrist
                        LRod(h, V(sh0), V(el), 0.05f, bone); LRod(h, V(el), V(wr), 0.045f, bone);
                        Ls(h, V(sh0), 0.065f, bone); Ls(h, V(el), 0.058f, bone); Ls(h, V(wr), 0.055f, bone);
                        // four bowed fingers of 3 bones with knuckles and a claw
                        for (int f = 0; f < 4; f++)
                        {
                            var a = f == 3 ? Vector2.Lerp(el, wr, 0.3f) : wr; var tp = tips[f];
                            var bow = new Vector2(-(tp - a).y, (tp - a).x).normalized * 0.025f;
                            var prev = V(a);
                            for (int k = 1; k <= 3; k++)
                            {
                                float t = k / 3f; var cur = V(Vector2.Lerp(a, tp, t) + bow * Mathf.Sin(t * Mathf.PI));
                                LRod(h, prev, cur, 0.044f - k * 0.005f, bone);
                                if (k < 3) Ls(h, cur, 0.046f - k * 0.004f, bone);
                                prev = cur;
                            }
                            var dir = (tp - Vector2.Lerp(a, tp, 0.8f)).normalized;
                            BwCone(h, V(tp - dir * 0.01f), V(tp + dir * 0.07f), 0.03f, claw);
                        }
                        // thumb claw + elbow spur
                        Ls(h, V(wr + new Vector2(-0.01f, 0.03f)), 0.04f, bone);
                        BwCone(h, V(wr + new Vector2(-0.01f, 0.04f)), V(wr + new Vector2(-0.06f, 0.13f)), 0.038f, claw);
                        BwCone(h, V(el + new Vector2(0, 0.02f)), V(el + new Vector2(-0.07f, 0.1f)), 0.032f, claw);
                        // forked veins towards the trailing edge on both faces
                        for (int k = 0; k < 4; k++)
                        {
                            var o = k == 3 ? el : wr; var v = valleys[k] - o;
                            var m0 = o + v * 0.15f; var m1 = o + v * 0.72f;
                            foreach (var z in new[] { 0.017f * bz, -0.017f * bz })
                            {
                                BwBar(h, V(m0, z), V(m1, z), 0.009f, 0.005f, vein, Vector3.forward);
                                foreach (var ang in new[] { -28f, 28f })
                                    BwBar(h, V(m1, z), V(m1 + (Vector2)(Quaternion.Euler(0, 0, ang) * (Vector3)v) * 0.22f, z), 0.007f, 0.005f, vein, Vector3.forward);
                            }
                        }
                        // scales along the arm (back side)
                        for (int k = 0; k < 8; k++)
                        {
                            float t = (k + 0.5f) / 8f; var p = t < 0.45f ? Vector2.Lerp(sh0, el, t / 0.45f) : Vector2.Lerp(el, wr, (t - 0.45f) / 0.55f);
                            Ls(h, V(p + new Vector2(0.01f, (k % 2 == 0 ? 1 : -1) * 0.012f), 0.024f * bz), new Vector3(0.045f, 0.034f, 0.014f), scl, new Vector3(0, 0, 40));
                        }
                    }
                    CanGlide = true;
                    // spine hub with dorsal spikes
                    BwBall(w, new Vector3(0, 0, -0.012f), new Vector3(0.17f, 0.13f, 0.06f), bone);
                    for (int k = 0; k < 3; k++) BwCone(w, new Vector3(0, -0.05f + k * 0.05f, -0.035f), new Vector3(0, -0.02f + k * 0.05f, -0.1f), 0.035f, claw);
                    for (int k = 0; k < 4; k++) Ls(w, new Vector3((k < 2 ? -1 : 1) * 0.05f, (k % 2 == 0 ? 0.03f : -0.03f), -0.032f), new Vector3(0.045f, 0.035f, 0.014f), scl, new Vector3(90, 0, 0));
                    break;
                }
                case 2:
                {
                    var metal = M("A8B3C2", 0.8f, 0.7f); var gold = M("FFC83A", 0.8f, 0.8f); var leather = M("6B3E1E", 0.3f); var thread = M("E8C890", 0.2f); var gem = G("35A8FF", 1.6f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var f = Pad(sd, out var sh);
                        var dr = new Vector3(0.13f, 0.075f, 0.135f);
                        BwBall(f, Vector3.zero, dr * 2, metal);
                        BwRing(f, new Vector3(0, -0.004f, 0), 0.128f, 0.011f, gold, default, new Vector3(1, 1, 1.04f));
                        BwRing(f, Vector3.zero, 0.135f, 0.012f, gold, new Vector3(0, 0, 90), new Vector3(0.56f, 1, 1));   // crest
                        for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; BwRivet(f, new Vector3(Mathf.Cos(a) * 0.128f, 0.008f, Mathf.Sin(a) * 0.133f), 0.016f, gold, Vector3.up); }
                        // three lames stepping down the arm, gold edges, rivets on the outer half
                        for (int k = 0; k < 3; k++)
                        {
                            var c = new Vector3(sd * (0.015f + k * 0.015f), -0.04f - k * 0.032f, 0); var sz = new Vector3(0.27f + k * 0.012f, 0.055f, 0.275f + k * 0.008f);
                            BwBall(f, c, sz, metal);
                            BwRing(f, c, sz.x * 0.5f, 0.007f, gold, default, new Vector3(1, 1, sz.z / sz.x));
                            for (int j = 0; j < 6; j++) { float a = ((sd > 0 ? -60 : 120) + j * 24) * Mathf.Deg2Rad; BwRivet(f, c + new Vector3(Mathf.Cos(a) * sz.x * 0.5f, 0.007f, Mathf.Sin(a) * sz.z * 0.5f), 0.013f, gold, Vector3.up); }
                        }
                        // emblem: star with a gem on the outer face of the dome
                        float ph = sd > 0 ? 0 : 180;
                        var ep = Dt.Ell(Vector3.zero, dr, 55, ph, 1.02f); var en = Dt.EllN(dr, 55, ph);
                        Msh(Meshes.Star(), f, ep, new Vector3(0.07f, 0.07f, 0.04f), gold).transform.localRotation = Quaternion.LookRotation(en, Vector3.up);
                        BwDot(f, ep + en * 0.008f, 0.022f, gem);
                        BwFlat(f);
                    }
                    // leather straps joining the pads across the upper back and the chest
                    BwBand(t => Dt.OnTorso(55, Mathf.Lerp(205, 335, t)) - Dt.TorsoC, 6, 0.045f, 0.012f, leather, thread);
                    BwBand(t => Dt.OnTorso(60, Mathf.Lerp(25, 155, t)) - Dt.TorsoC, 6, 0.045f, 0.012f, leather, thread);
                    foreach (var ph in new[] { 250f, 270f, 290f }) { var p = BwSurf(Dt.OnTorso(55, ph) - Dt.TorsoC, 1.1f, out var n); BwRivet(Torso, p, 0.02f, gold, n); }
                    var bp = BwSurf(Dt.OnTorso(60, 90) - Dt.TorsoC, 1.1f, out var bn);
                    BwRingN(Torso, bp, 0.03f, 0.008f, gold, bn);
                    BwBar(Torso, bp + Vector3.down * 0.026f, bp + Vector3.up * 0.026f, 0.006f, 0.006f, gold, bn);
                    break;
                }
                case 3:
                {
                    var dark = M("3A3F47", 0.6f, 0.6f); var metal = M("A8B3C2", 0.8f, 0.7f); var leather = M("2A2A2E", 0.3f); var chain = M("8C949E", 0.7f, 0.8f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var f = Pad(sd, out var sh);
                        BwBall(f, Vector3.zero, new Vector3(0.26f, 0.15f, 0.27f), dark);
                        BwRing(f, new Vector3(0, -0.004f, 0), 0.128f, 0.01f, metal, default, new Vector3(1, 1, 1.04f));
                        for (int k = 0; k < 2; k++)
                        {
                            var c = new Vector3(sd * (0.015f + k * 0.018f), -0.042f - k * 0.034f, 0); var sz = new Vector3(0.27f + k * 0.014f, 0.055f, 0.275f + k * 0.01f);
                            BwBall(f, c, sz, dark);
                            for (int j = 0; j < 5; j++) { float a = ((sd > 0 ? -60 : 120) + j * 30) * Mathf.Deg2Rad; BwRivet(f, c + new Vector3(Mathf.Cos(a) * sz.x * 0.5f, 0.006f, Mathf.Sin(a) * sz.z * 0.5f), 0.014f, metal, Vector3.up); }
                        }
                        // three big spikes along the top with collars and washers
                        for (int k = 0; k < 3; k++)
                        {
                            float z = -0.08f + k * 0.08f; var a = new Vector3(0, 0.075f * Mathf.Sqrt(1 - z * z / 0.0182f) - 0.012f, z);
                            Spike(f, a, a + new Vector3(sd * 0.03f, 0.17f - Mathf.Abs(k - 1) * 0.05f, 0), 0.07f, metal);
                            BwRing(f, a + Vector3.up * 0.012f, 0.034f, 0.008f, chain);
                            Lc(f, a + Vector3.up * 0.004f, new Vector3(0.085f, 0.004f, 0.085f), dark);
                        }
                        // small spikes round the rim with rivets between them
                        for (int j = 0; j < 7; j++)
                        {
                            float a = ((sd > 0 ? -75 : 105) + j * 25) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            var p = new Vector3(d.x * 0.13f, -0.002f, d.z * 0.135f);
                            BwCone(f, p, p + (d + Vector3.down * 0.15f).normalized * 0.06f, 0.03f, metal);
                            if (j < 6) { float b = a + 12.5f * Mathf.Deg2Rad; BwRivet(f, new Vector3(Mathf.Cos(b) * 0.128f, 0.008f, Mathf.Sin(b) * 0.133f), 0.014f, metal, Vector3.up); }
                        }
                        BwFlat(f);
                    }
                    // crossed leather harness on the back: spiked studs and an O-ring
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var dA = Dt.OnTorso(45, sd > 0 ? 215 : 325) - Dt.TorsoC; var dB = Dt.OnTorso(118, sd > 0 ? 322 : 218) - Dt.TorsoC;
                        BwBand(t => Vector3.Lerp(dA, dB, t), 5, 0.05f, 0.012f, leather, null);
                        foreach (var t in new[] { 0.14f, 0.3f, 0.7f, 0.86f }) { var p = BwSurf(Vector3.Lerp(dA, dB, t), 1.08f, out var n); BwCone(Torso, p, p + n * 0.032f, 0.026f, metal); }
                    }
                    {
                        var p = BwSurf(Vector3.Lerp(Dt.OnTorso(45, 215), Dt.OnTorso(118, 322), 0.5f) - Dt.TorsoC, 1.1f, out var n);
                        BwRingN(Torso, p, 0.035f, 0.009f, chain, n);
                        BwRivet(Torso, p, 0.04f, dark, n);
                    }
                    // chain hanging across the chest between the pads
                    var c0 = new Vector3(-0.25f, 0.43f, 0.1f); var c1 = new Vector3(0, 0.29f, 0.37f); var c2 = new Vector3(0.25f, 0.43f, 0.1f);
                    for (int k = 0; k < 11; k++)
                    {
                        float t = (k + 0.5f) / 11f; var p = (1 - t) * (1 - t) * c0 + 2 * t * (1 - t) * c1 + t * t * c2;
                        var tg = 2 * (1 - t) * (c1 - c0) + 2 * t * (c2 - c1);
                        var ax = k % 2 == 0 ? Vector3.forward : Vector3.Cross(tg, Vector3.forward);
                        BwRing(Torso, p, 0.018f, 0.005f, chain, default, new Vector3(1, 1, 1.5f)).transform.localRotation = Quaternion.LookRotation(tg, ax);
                    }
                    break;
                }
                case 4:
                {
                    var glow = Add("35E0FF", 0.55f); var vio = Add("B57CFF", 0.6f); var node = G("E8FBFF", 3f);
                    Feathered(1.1f, Add("35E0FF", 0.75f), vio, glow, glow, Add("B57CFF", 0.5f), G("FFFFFF", 2.5f), Add("B57CFF", 0.7f), (h, bz, s) =>
                    {
                        // energy nodes along the leading edge
                        for (int k = 0; k < 3; k++) BwDot(h, new Vector3(0.02f + k * 0.3f, 0.02f + k * 0.17f, 0.03f * bz) * s, 0.04f * s, node);
                    });
                    Mount(M("2A2E35", 0.6f, 0.6f), M("8C949E", 0.8f, 0.8f), G("35E0FF", 2.5f));
                    break;
                }
                case 5:
                {
                    var pink = M("FFB3D9", 0.05f); var light = M("FFD6EA", 0.05f); var white = M("FFFFFF", 0.1f); var heart = M("FF5FA2", 0.3f); var bow = M("FF7AB8", 0.4f); var pearl = M("FFF6FB", 0.8f, 0.3f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var h = Wing(sd, 0.1f, 15); float bz = -sd;
                        for (int k = 0; k < 3; k++)
                        {
                            var c = new Vector3(0.1f + k * 0.08f, 0.05f - k * 0.04f, 0); float d = 0.18f - k * 0.03f;
                            BwBall(h, c, Vector3.one * d, pink);
                            BwFur(h, c, Vector3.one * d * 0.48f, 9, 0.06f - k * 0.008f, light, 10 + k + (sd > 0 ? 5 : 0));
                            BwFur(h, c, Vector3.one * d * 0.5f, 5, 0.05f - k * 0.006f, white, 30 + k + (sd > 0 ? 5 : 0));
                        }
                        // little down feathers curling under the puffs
                        for (int k = 0; k < 4; k++) Ls(h, new Vector3(0.07f + k * 0.07f, -0.04f - k * 0.035f, 0), new Vector3(0.04f, 0.1f, 0.025f), white, new Vector3(0, 0, 25 + k * 8));
                        Msh(Meshes.Heart(), h, new Vector3(0.18f, 0.02f, 0.085f * bz), new Vector3(0.07f, 0.07f, 0.04f), heart);
                    }
                    CanGlide = true;
                    // satin bow on a pearl band between the wings
                    B(w, new Vector3(0, 0.0f, -0.022f), new Vector3(0.2f, 0.03f, 0.012f), bow);
                    for (int k = 0; k < 6; k++) BwDot(w, new Vector3(-0.09f + k * 0.036f, -0.022f, -0.025f), 0.016f, pearl);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        Ls(w, new Vector3(sd * 0.06f, 0.02f, -0.04f), new Vector3(0.11f, 0.07f, 0.04f), bow, new Vector3(0, 0, sd * 20));
                        Ls(w, new Vector3(sd * 0.065f, 0.02f, -0.058f), new Vector3(0.05f, 0.03f, 0.01f), heart, new Vector3(0, 0, sd * 20));
                        B(w, new Vector3(sd * 0.03f, -0.05f, -0.045f), new Vector3(0.03f, 0.1f, 0.01f), bow, new Vector3(0, 0, sd * 15));
                    }
                    Ls(w, new Vector3(0, 0.02f, -0.05f), 0.045f, bow);
                    break;
                }
                case 6:
                {
                    var metal = M("C9D1DB", 0.85f, 0.8f); var dark = M("555F6B", 0.7f, 0.6f); var black = M("2A2E35", 0.4f, 0.4f); var red = M("E53935", 0.5f); var yel = M("FFC400", 0.4f);
                    var fire = G("FF8A1E", 2.4f); var core = G("FFF2A0", 3f); var hot = G("FF6A00", 1.6f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        // blade wing: plate, round leading edge, flap, panel lines, rivets, stripe, winglet with a nav light
                        var f = Node("JetWing", w, new Vector3(sd * 0.32f, 0.02f, -0.02f), new Vector3(0, 0, sd * 18));
                        B(f, Vector3.zero, new Vector3(0.52f, 0.035f, 0.2f), metal);
                        LRod(f, new Vector3(-0.26f, 0, 0.1f), new Vector3(0.26f, 0, 0.1f), 0.04f, metal);
                        B(f, new Vector3(sd * 0.02f, -0.006f, -0.12f), new Vector3(0.4f, 0.02f, 0.05f), dark, new Vector3(-12, 0, 0));
                        for (int k = 0; k < 3; k++) B(f, new Vector3(sd * (-0.1f + k * 0.12f), 0.018f, 0), new Vector3(0.005f, 0.004f, 0.18f), dark);
                        for (int k = 0; k < 6; k++) foreach (var z in new[] { -0.07f, 0.07f }) BwRivet(f, new Vector3(sd * (-0.21f + k * 0.075f), 0.018f, z), 0.012f, dark, Vector3.up);
                        B(f, new Vector3(sd * 0.18f, 0, 0), new Vector3(0.05f, 0.037f, 0.201f), red);
                        B(f, new Vector3(sd * 0.265f, 0.045f, -0.01f), new Vector3(0.016f, 0.1f, 0.15f), metal);
                        Lc(f, new Vector3(sd * 0.265f, 0.1f, 0), new Vector3(0.03f, 0.012f, 0.03f), black);
                        BwDot(f, new Vector3(sd * 0.265f, 0.115f, 0), 0.026f, sd < 0 ? G("FF3030", 2.5f) : G("3CFF6A", 2.5f));
                        BwFlat(f);
                        // engine: nacelle, intake fan, ribs, nozzle bell, fins, feed pipe, two flames
                        var e = new Vector3(sd * 0.16f, -0.1f, -0.06f);
                        Cy(w, e, new Vector3(0.09f, 0.12f, 0.09f), dark);
                        Lc(w, e + new Vector3(0, 0.121f, 0), new Vector3(0.07f, 0.004f, 0.07f), black);
                        BwRing(w, e + new Vector3(0, 0.12f, 0), 0.045f, 0.009f, metal);
                        for (int k = 0; k < 4; k++) B(w, e + new Vector3(0, 0.126f, 0), new Vector3(0.07f, 0.004f, 0.012f), metal, new Vector3(0, k * 45, 0));
                        for (int k = 0; k < 3; k++) BwRing(w, e + new Vector3(0, -0.07f + k * 0.05f, 0), 0.047f, 0.005f, metal);
                        Cone(w, e + new Vector3(0, -0.17f, 0), new Vector3(0.1f, 0.07f, 0.1f), black);
                        BwRing(w, e + new Vector3(0, -0.17f, 0), 0.05f, 0.006f, metal);
                        Lc(w, e + new Vector3(0, -0.168f, 0), new Vector3(0.085f, 0.003f, 0.085f), hot);
                        B(w, e + new Vector3(sd * 0.06f, -0.08f, 0), new Vector3(0.035f, 0.08f, 0.006f), red);
                        B(w, e + new Vector3(0, -0.08f, -0.06f), new Vector3(0.006f, 0.08f, 0.035f), red);
                        LRod(w, new Vector3(sd * 0.06f, -0.02f, -0.07f), e + new Vector3(-sd * 0.045f, 0.03f, 0), 0.022f, metal);
                        BwDot(w, e + new Vector3(-sd * 0.045f, 0.03f, 0), 0.03f, dark);
                        var fl = Cone(w, e + new Vector3(0, -0.17f, 0), new Vector3(0.08f, 0.22f, 0.08f), fire, new Vector3(180, 0, 0));
                        Add<Pulse>(fl).amount = 0.2f;
                        fl.SetActive(false); JetFlames.Add(fl);
                        var fc = Cone(w, e + new Vector3(0, -0.17f, 0), new Vector3(0.045f, 0.13f, 0.045f), core, new Vector3(180, 0, 0));
                        Add<Pulse>(fc).amount = 0.25f;
                        fc.SetActive(false); JetFlames.Add(fc);
                    }
                    // central pack: vents, pressure gauge, hazard stripes, rivets, carry handle, status lights
                    var pk = new Vector3(0, -0.02f, -0.07f);
                    B(w, pk, new Vector3(0.15f, 0.22f, 0.09f), metal);
                    B(w, pk + new Vector3(0, 0.115f, 0), new Vector3(0.13f, 0.015f, 0.08f), dark);
                    for (int k = 0; k < 5; k++) B(w, pk + new Vector3(0, -0.07f + k * 0.022f, -0.047f), new Vector3(0.1f, 0.008f, 0.006f), black);
                    Lc(w, pk + new Vector3(0, 0.06f, -0.047f), new Vector3(0.06f, 0.004f, 0.06f), M("F2F4F7", 0.5f), new Vector3(90, 0, 0));
                    BwRing(w, pk + new Vector3(0, 0.06f, -0.048f), 0.031f, 0.005f, dark, new Vector3(90, 0, 0));
                    B(w, pk + new Vector3(0.006f, 0.066f, -0.052f), new Vector3(0.004f, 0.024f, 0.003f), red, new Vector3(0, 0, -35));
                    BwDot(w, pk + new Vector3(0, 0.06f, -0.052f), 0.01f, black);
                    for (int k = 0; k < 4; k++) B(w, pk + new Vector3(-0.045f + k * 0.03f, -0.097f, -0.047f), new Vector3(0.014f, 0.022f, 0.006f), k % 2 == 0 ? yel : black, new Vector3(0, 0, 30));
                    foreach (var x in new[] { -0.062f, 0.062f }) foreach (var y in new[] { -0.095f, 0.095f }) BwRivet(w, pk + new Vector3(x, y, -0.046f), 0.012f, dark, Vector3.back);
                    var h0 = pk + new Vector3(-0.04f, 0.12f, -0.02f); var h1 = pk + new Vector3(0.04f, 0.12f, -0.02f); var up = new Vector3(0, 0.04f, 0);
                    LRod(w, h0, h0 + up, 0.012f, black); LRod(w, h0 + up, h1 + up, 0.012f, black); LRod(w, h1, h1 + up, 0.012f, black);
                    BwDot(w, pk + new Vector3(-0.03f, 0.1f, -0.047f), 0.014f, G("3CFF6A", 2.5f));
                    BwDot(w, pk + new Vector3(0.03f, 0.1f, -0.047f), 0.014f, G("FF3030", 2.5f));
                    BwStraps(black, null, metal);
                    CanGlide = true; HasJet = true;
                    break;
                }
                case 7:
                {
                    var housing = M("333840", 0.6f, 0.5f); var metal = M("A8B3C2", 0.8f, 0.7f); var lens = G("FFF2A0", 3f); var led = G("FFFFFF", 4f);
                    var pad = M("4A3A2A", 0.3f); var thread = M("D9B48A", 0.2f); var black = M("1E2126", 0.3f); var green = G("3CFF6A", 2.5f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var sh = Node("Lamp", Torso, new Vector3(sd * 0.27f, 0.47f, 0.02f));
                        // stitched leather pad on the shoulder + bracket base
                        BwBall(sh, new Vector3(0, -0.055f, -0.01f), new Vector3(0.16f, 0.05f, 0.18f), pad);
                        Dt.Studs(sh, Dt.Circle(new Vector3(0, -0.04f, -0.01f), 0.066f, 0.076f, 0, 315), 8, 0.009f, thread, 0.5f);
                        B(sh, new Vector3(0, -0.03f, 0), new Vector3(0.13f, 0.012f, 0.06f), metal);
                        // the lamp in its own frame (axis = beam): housing, bezel with screws, lens, LED, cooling fins, back cap, U-bracket, switch, faint beam
                        var f = Node("LampF", sh, Vector3.zero, new Vector3(70, 0, 0));
                        Cy(f, Vector3.zero, new Vector3(0.1f, 0.06f, 0.1f), housing);
                        BwRing(f, new Vector3(0, 0.06f, 0), 0.05f, 0.008f, metal);
                        Lc(f, new Vector3(0, 0.062f, 0), new Vector3(0.084f, 0.005f, 0.084f), lens);
                        BwDot(f, new Vector3(0, 0.068f, 0), 0.025f, led);
                        for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; BwDot(f, new Vector3(Mathf.Cos(a) * 0.05f, 0.067f, Mathf.Sin(a) * 0.05f), 0.009f, black); }
                        for (int k = 0; k < 3; k++) BwRing(f, new Vector3(0, -0.015f - k * 0.017f, 0), 0.052f, 0.005f, metal);
                        BwBall(f, new Vector3(0, -0.06f, 0), new Vector3(0.09f, 0.03f, 0.09f), housing);
                        foreach (var x in new[] { -1f, 1f })
                        {
                            B(f, new Vector3(x * 0.058f, 0, 0.02f), new Vector3(0.008f, 0.05f, 0.06f), metal);
                            Lc(f, new Vector3(x * 0.064f, 0, 0), new Vector3(0.03f, 0.005f, 0.03f), black, new Vector3(0, 0, 90));
                        }
                        B(f, new Vector3(0, 0, -0.052f), new Vector3(0.015f, 0.02f, 0.01f), black);
                        LRod(f, new Vector3(0, 0, -0.056f), new Vector3(0, 0.012f, -0.07f), 0.005f, metal);
                        BwCone(f, new Vector3(0, 0.42f, 0), new Vector3(0, 0.07f, 0), 0.24f, Add("FFF2A0", 0.12f));
                        BwFlat(f);
                        // cable over the shoulder to the battery pack, with clips
                        Vector3[] cp = { new Vector3(sd * 0.27f, 0.45f, -0.04f), new Vector3(sd * 0.25f, 0.42f, -0.16f), new Vector3(sd * 0.17f, 0.37f, -0.26f), new Vector3(sd * 0.07f, 0.3f, -0.31f) };
                        for (int k = 0; k < 3; k++) LRod(Torso, cp[k], cp[k + 1], 0.012f, black);
                        BwDot(Torso, cp[1], 0.02f, metal); BwDot(Torso, cp[2], 0.02f, metal);
                    }
                    // battery pack on the back: lid, charge LEDs, vents, screws, label
                    var bp = new Vector3(0, 0.26f, -0.31f);
                    B(Torso, bp, new Vector3(0.16f, 0.12f, 0.05f), black);
                    B(Torso, bp + new Vector3(0, 0.065f, 0), new Vector3(0.165f, 0.02f, 0.055f), metal);
                    for (int k = 0; k < 3; k++) BwDot(Torso, bp + new Vector3(-0.03f + k * 0.03f, 0.025f, -0.026f), 0.014f, green);
                    for (int k = 0; k < 3; k++) B(Torso, bp + new Vector3(0, -0.01f - k * 0.014f, -0.026f), new Vector3(0.1f, 0.006f, 0.004f), housing);
                    foreach (var x in new[] { -0.068f, 0.068f }) foreach (var y in new[] { -0.048f, 0.04f }) BwRivet(Torso, bp + new Vector3(x, y, -0.026f), 0.011f, metal, Vector3.back);
                    B(Torso, bp + new Vector3(0, -0.05f, -0.026f), new Vector3(0.12f, 0.012f, 0.004f), M("FFC400", 0.4f));
                    BwStraps(pad, thread, metal, false);
                    break;
                }
                case 100:
                {
                    var flame = G("FFD23A", 2.4f); var ember = G("FFF2A0", 3f); var gold = M("FFC83A", 0.8f, 0.8f);
                    Feathered(1.15f, G("FF6A00", 1.6f), G("FFD23A", 2f), G("FF3B1F", 1.8f), G("FF8A1E", 1.8f), G("FFD23A", 2f), G("FFF2A0", 2.5f), G("FFE14D", 2.2f), (h, bz, s) =>
                    {
                        // flame licks off the primaries + embers
                        for (int k = 0; k < 5; k++)
                        {
                            var dd = (P1[k] - P0[k]).normalized; var t = P1[k] * s;
                            BwCone(h, new Vector3(t.x - dd.x * 0.04f, t.y - dd.y * 0.04f, 0.04f * bz), new Vector3(t.x + dd.x * 0.1f, t.y + dd.y * 0.1f, 0.04f * bz), 0.05f * s, flame);
                        }
                        for (int k = 0; k < 3; k++) BwDot(h, new Vector3(0.3f + k * 0.2f, -0.38f + k * 0.08f, 0.03f * bz) * s, 0.03f, ember);
                    });
                    Mount(gold, gold, G("FF3B1F", 2f));
                    break;
                }
                case 104:
                {
                    var blade = G("26F0FF", 2.5f); var edge = G("E8FFFF", 3f); var halo = Add("26F0FF", 0.3f); var dark = M("1E2430", 0.5f, 0.6f); var metal = M("8C949E", 0.8f, 0.8f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", w, new Vector3(sd * 0.08f, 0, 0));
                        var fp = Add<Flap>(piv.gameObject); fp.model = this; fp.side = sd; fp.baseYaw = 20; fp.idleAmp = 3;
                        for (int k = 0; k < 3; k++)
                        {
                            // blade: glowing core, white cutting edge, dark spine, diamond tip, segment notches, emitter hilt, soft halo
                            var f = Node("Blade", piv, new Vector3(sd * (0.28f + k * 0.03f), 0.08f + k * 0.08f, 0), new Vector3(0, 0, sd * (15 + k * 18)));
                            B(f, Vector3.zero, new Vector3(0.58f, 0.04f, 0.02f), blade);
                            B(f, new Vector3(sd * 0.01f, 0.022f, 0), new Vector3(0.56f, 0.008f, 0.024f), edge);
                            B(f, new Vector3(-sd * 0.02f, -0.024f, 0), new Vector3(0.5f, 0.01f, 0.03f), dark);
                            B(f, new Vector3(sd * 0.29f, 0, 0), new Vector3(0.04f, 0.04f, 0.02f), edge, new Vector3(0, 0, 45));
                            for (int j = 0; j < 4; j++) B(f, new Vector3(sd * (-0.15f + j * 0.1f), -0.004f, 0), new Vector3(0.006f, 0.028f, 0.022f), dark);
                            Lc(f, new Vector3(-sd * 0.3f, 0, 0), new Vector3(0.05f, 0.03f, 0.05f), metal, new Vector3(0, 0, 90));
                            foreach (var x in new[] { -0.32f, -0.28f }) BwRing(f, new Vector3(sd * x, 0, 0), 0.027f, 0.005f, blade, new Vector3(0, 0, 90));
                            B(f, new Vector3(-sd * 0.3f, 0.026f, 0), new Vector3(0.03f, 0.004f, 0.02f), dark);
                            B(f, Vector3.zero, new Vector3(0.6f, 0.07f, 0.04f), halo);
                            BwFlat(f);
                        }
                        // spine the blades plug into + sparks
                        BwBar(piv, new Vector3(sd * 0.0f, 0.04f, 0), new Vector3(sd * 0.04f, 0.3f, 0), 0.03f, 0.03f, dark, Vector3.forward);
                        for (int k = 0; k < 3; k++) BwDot(piv, new Vector3(sd * (0.006f + k * 0.017f), 0.08f + k * 0.08f, 0), 0.032f, metal);
                        BwRivet(piv, new Vector3(sd * 0.02f, 0.17f, -0.016f), 0.014f, metal, Vector3.back);
                        for (int k = 0; k < 4; k++) BwDot(piv, new Vector3(sd * (0.35f + k * 0.08f), 0.28f + (k % 2) * 0.06f, -0.02f), 0.018f, edge);
                    }
                    CanGlide = true;
                    // cyber hub between the wing roots
                    Lc(w, new Vector3(0, 0, -0.02f), new Vector3(0.14f, 0.02f, 0.14f), dark, new Vector3(90, 0, 0));
                    BwDot(w, new Vector3(0, 0, -0.045f), 0.06f, blade);
                    BwRing(w, new Vector3(0, 0, -0.042f), 0.05f, 0.006f, edge, new Vector3(90, 0, 0));
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; BwRivet(w, new Vector3(Mathf.Cos(a) * 0.062f, Mathf.Sin(a) * 0.062f, -0.041f), 0.011f, metal, Vector3.back); }
                    for (int k = 0; k < 4; k++) B(w, new Vector3(0, 0, -0.042f), new Vector3(0.005f, 0.13f, 0.004f), blade, new Vector3(0, 0, 22.5f + k * 45));
                    break;
                }
                case 106:
                {
                    var white = M("FFFFFF", 0.3f); var gold = M("FFD86B", 0.8f, 0.7f);
                    string[] pastel = { "FF9AA2", "FFD59A", "FFF59A", "A8F0B0", "A0D8FF" };
                    var pb = new Material[5];
                    for (int k = 0; k < 5; k++) pb[k] = M(pastel[k], 0.3f);
                    Feathered(1.1f, white, Add("FF9AE6", 0.5f), white, white, M("F4EEFF", 0.25f), gold, M("E6D4FF", 0.25f), (h, bz, s) =>
                    {
                        Msh(Meshes.Star(), h, new Vector3(0.3f, 0.12f, 0.07f * bz) * s, new Vector3(0.07f, 0.07f, 0.04f), gold);
                        for (int k = 0; k < 2; k++) BwDot(h, new Vector3(0.62f + k * 0.12f, 0.36f - k * 0.2f, 0.05f * bz) * s, 0.025f, G("FFFFFF", 2.5f));
                    }, pb);
                    Mount(white, gold, G("FF9AE6", 2f));
                    break;
                }
            }
        }

        // ================= backpacks =================

        void Back(int i, int set)
        {
            var b = Node("Back", Torso, new Vector3(0, 0.28f, -0.28f));
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    var blue = M("1E63D9", 0.3f); var dark = M("173F8A"); var yel = M("FFC400", 0.3f); var metal = M("C9D1DB", 0.85f, 0.8f); var thread = M("FFFFFF", 0.2f); var black = M("222428", 0.3f); var red = M("E53935", 0.4f);
                    // body with a rounded top and a reinforced bottom
                    B(b, new Vector3(0, -0.03f, -0.08f), new Vector3(0.4f, 0.36f, 0.18f), blue);
                    Cy(b, new Vector3(0, 0.15f, -0.08f), new Vector3(0.18f, 0.2f, 0.18f), blue, new Vector3(0, 0, 90));
                    B(b, new Vector3(0, -0.205f, -0.08f), new Vector3(0.41f, 0.04f, 0.19f), dark);
                    foreach (var x in new[] { -0.2f, 0.2f }) LRod(b, new Vector3(x, -0.2f, -0.17f), new Vector3(x, 0.15f, -0.17f), 0.014f, dark);
                    // front pocket: zipper, stitching, keychain star
                    B(b, new Vector3(0, -0.08f, -0.19f), new Vector3(0.3f, 0.17f, 0.05f), yel);
                    Dt.Zipper(b, new Vector3(-0.13f, -0.01f, -0.219f), new Vector3(0.13f, -0.01f, -0.219f), 14, metal, dark, Vector3.back);
                    Dt.Stitches(b, Dt.Line(new Vector3(-0.135f, -0.155f, -0.217f), new Vector3(0.135f, -0.155f, -0.217f)), 9, 0.018f, 0.004f, dark);
                    foreach (var x in new[] { -0.138f, 0.138f }) Dt.Stitches(b, Dt.Line(new Vector3(x, -0.14f, -0.217f), new Vector3(x, -0.03f, -0.217f)), 4, 0.018f, 0.004f, dark);
                    for (int k = 0; k < 3; k++) BwDot(b, new Vector3(0.108f, -0.03f - k * 0.012f, -0.232f), 0.01f, metal);
                    Msh(Meshes.Star(), b, new Vector3(0.108f, -0.075f, -0.234f), new Vector3(0.05f, 0.05f, 0.03f), red);
                    // main zipper over the top + carry handle
                    Dt.Zipper(b, new Vector3(-0.18f, 0.243f, -0.08f), new Vector3(0.18f, 0.243f, -0.08f), 16, metal, dark, Vector3.up);
                    foreach (var x in new[] { -0.05f, 0.05f }) B(b, new Vector3(x, 0.242f, -0.04f), new Vector3(0.03f, 0.01f, 0.03f), dark);
                    var hp = new[] { new Vector3(-0.05f, 0.245f, -0.04f), new Vector3(-0.035f, 0.29f, -0.04f), new Vector3(0.035f, 0.29f, -0.04f), new Vector3(0.05f, 0.245f, -0.04f) };
                    for (int k = 0; k < 3; k++) LRod(b, hp[k], hp[k + 1], 0.016f, dark);
                    // side pockets: pencil on the right, ruler with ticks on the left
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        B(b, new Vector3(sd * 0.215f, -0.1f, -0.08f), new Vector3(0.04f, 0.16f, 0.15f), M("2B78F0", 0.2f));
                        B(b, new Vector3(sd * 0.215f, -0.025f, -0.08f), new Vector3(0.046f, 0.02f, 0.155f), dark);
                    }
                    Lc(b, new Vector3(0.215f, 0.02f, -0.04f), new Vector3(0.03f, 0.09f, 0.03f), M("FFD23A", 0.4f));
                    Cone(b, new Vector3(0.215f, 0.11f, -0.04f), new Vector3(0.03f, 0.035f, 0.03f), M("F2C893", 0.3f));
                    BwDot(b, new Vector3(0.215f, 0.142f, -0.04f), 0.01f, black);
                    B(b, new Vector3(-0.215f, 0.0f, -0.12f), new Vector3(0.012f, 0.2f, 0.04f), M("8FE07A", 0.5f));
                    for (int k = 0; k < 5; k++) B(b, new Vector3(-0.222f, -0.02f + k * 0.025f, -0.13f), new Vector3(0.003f, 0.003f, k % 2 == 0 ? 0.02f : 0.012f), black);
                    // patches: red star + stitched smiley badge
                    Msh(Meshes.Star(), b, new Vector3(-0.09f, 0.08f, -0.172f), new Vector3(0.08f, 0.08f, 0.02f), red);
                    Lc(b, new Vector3(0.09f, 0.08f, -0.171f), new Vector3(0.075f, 0.003f, 0.075f), thread, new Vector3(90, 0, 0));
                    Lc(b, new Vector3(0.09f, 0.08f, -0.173f), new Vector3(0.062f, 0.003f, 0.062f), yel, new Vector3(90, 0, 0));
                    foreach (var x in new[] { -0.012f, 0.012f }) BwDot(b, new Vector3(0.09f + x, 0.088f, -0.176f), 0.009f, black);
                    BwRing(b, new Vector3(0.09f, 0.078f, -0.176f), 0.014f, 0.003f, black, new Vector3(90, 0, 0), new Vector3(1, 1, 0.6f));
                    Dt.Studs(b, t => new Vector3(0.09f + Mathf.Cos(t * 5.5f) * 0.034f, 0.08f + Mathf.Sin(t * 5.5f) * 0.034f, -0.175f), 8, 0.006f, dark);
                    BwStraps(dark, M("5C8DF0", 0.2f), black);
                    break;
                }
                case 1:
                {
                    var metal = M("C9D1DB", 0.85f, 0.8f); var red = M("E53935", 0.6f); var dark = M("555F6B", 0.7f, 0.6f); var black = M("2A2E35", 0.4f, 0.4f); var yel = M("FFC400", 0.4f); var white = M("F2F4F7", 0.5f);
                    var fire = G("FF8A1E", 2.4f); var core = G("FFF2A0", 3f);
                    B(b, new Vector3(0, 0, -0.02f), new Vector3(0.36f, 0.4f, 0.03f), dark);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var x = sd * 0.11f; const float z = -0.08f;
                        // tank with bands and clamp bolts, red dome with a valve wheel
                        Cy(b, new Vector3(x, 0, z), new Vector3(0.16f, 0.2f, 0.16f), metal);
                        BwBall(b, new Vector3(x, 0.2f, z), Vector3.one * 0.16f, red);
                        for (int k = 0; k < 3; k++)
                        {
                            float y = -0.13f + k * 0.13f;
                            BwRing(b, new Vector3(x, y, z), 0.082f, 0.008f, dark);
                            BwRivet(b, new Vector3(x, y, z - 0.09f), 0.016f, black, Vector3.back);
                            BwRivet(b, new Vector3(x + sd * 0.09f, y, z), 0.016f, black, Vector3.right * sd);
                        }
                        Lc(b, new Vector3(x, 0.29f, z), new Vector3(0.03f, 0.02f, 0.03f), dark);
                        BwRing(b, new Vector3(x, 0.315f, z), 0.025f, 0.005f, red);
                        foreach (var a in new[] { 0f, 90f }) B(b, new Vector3(x, 0.315f, z), new Vector3(0.05f, 0.005f, 0.006f), dark, new Vector3(0, a, 0));
                        // fins
                        B(b, new Vector3(x + sd * 0.085f, -0.14f, z), new Vector3(0.006f, 0.12f, 0.06f), red);
                        B(b, new Vector3(x, -0.14f, z - 0.085f), new Vector3(0.06f, 0.12f, 0.006f), red);
                        // nozzle: throat, bell, lip, hot glow
                        Lc(b, new Vector3(x, -0.2f, z), new Vector3(0.07f, 0.015f, 0.07f), black);
                        Cone(b, new Vector3(x, -0.27f, z), new Vector3(0.12f, 0.08f, 0.12f), dark);
                        BwRing(b, new Vector3(x, -0.27f, z), 0.06f, 0.007f, metal);
                        Lc(b, new Vector3(x, -0.268f, z), new Vector3(0.1f, 0.003f, 0.1f), G("FF6A00", 1.5f));
                        // label on the tank
                        B(b, new Vector3(x, 0.06f, z - 0.081f), new Vector3(0.07f, 0.05f, 0.004f), white);
                        for (int k = 0; k < 2; k++) B(b, new Vector3(x, 0.07f - k * 0.02f, z - 0.084f), new Vector3(0.05f, 0.006f, 0.003f), red);
                        // pipe from the valve to the spine
                        LRod(b, new Vector3(x, 0.28f, z), new Vector3(sd * 0.03f, 0.21f, -0.1f), 0.018f, metal);
                        BwDot(b, new Vector3(sd * 0.03f, 0.21f, -0.1f), 0.026f, dark);
                        var fl = Cone(b, new Vector3(x, -0.27f, z), new Vector3(0.11f, 0.32f, 0.11f), fire, new Vector3(180, 0, 0));
                        Add<Pulse>(fl).amount = 0.2f;
                        fl.SetActive(false); JetFlames.Add(fl);
                        var fc = Cone(b, new Vector3(x, -0.27f, z), new Vector3(0.06f, 0.2f, 0.06f), core, new Vector3(180, 0, 0));
                        Add<Pulse>(fc).amount = 0.25f;
                        fc.SetActive(false); JetFlames.Add(fc);
                    }
                    // spine between the tanks: vents, pressure gauge, hazard stripes
                    B(b, new Vector3(0, 0.01f, -0.07f), new Vector3(0.07f, 0.36f, 0.07f), black);
                    BwBall(b, new Vector3(0, 0.19f, -0.07f), new Vector3(0.07f, 0.04f, 0.07f), dark);
                    for (int k = 0; k < 5; k++) B(b, new Vector3(0, -0.08f + k * 0.03f, -0.107f), new Vector3(0.05f, 0.008f, 0.006f), metal);
                    Lc(b, new Vector3(0, 0.11f, -0.108f), new Vector3(0.06f, 0.005f, 0.06f), white, new Vector3(90, 0, 0));
                    BwRing(b, new Vector3(0, 0.11f, -0.11f), 0.031f, 0.005f, metal, new Vector3(90, 0, 0));
                    B(b, new Vector3(0.006f, 0.116f, -0.114f), new Vector3(0.004f, 0.022f, 0.003f), red, new Vector3(0, 0, -40));
                    BwDot(b, new Vector3(0, 0.11f, -0.114f), 0.01f, black);
                    for (int k = 0; k < 4; k++) B(b, new Vector3(0, -0.16f + k * 0.018f, -0.107f), new Vector3(0.068f, 0.009f, 0.004f), k % 2 == 0 ? yel : black, new Vector3(0, 0, 20));
                    BwStraps(black, M("3D434D", 0.3f), metal);
                    HasJet = true;
                    break;
                }
                case 2:
                {
                    var wood = M("8D5524", 0.3f); var groove = M("5A3414", 0.2f); var iron = M("B0BAC6", 0.8f, 0.8f); var gold = M("FFC83A", 0.8f, 0.8f); var red = M("C62828", 0.4f); var leather = M("6B3E1E", 0.3f);
                    var sh = Node("Shield", b, new Vector3(0, -0.02f, -0.04f), new Vector3(90, 0, 0));
                    // planks: wooden disc with seams and a few battle scratches (outer face = −Y here)
                    Cy(sh, Vector3.zero, new Vector3(0.52f, 0.025f, 0.52f), wood);
                    foreach (var x in new[] { -0.156f, -0.052f, 0.052f, 0.156f }) B(sh, new Vector3(x, -0.0135f, 0), new Vector3(0.006f, 0.003f, 2 * Mathf.Sqrt(0.0676f - x * x) * 0.94f), groove);
                    for (int k = 0; k < 3; k++) B(sh, new Vector3(-0.12f + k * 0.11f, -0.014f, 0.13f - k * 0.05f), new Vector3(0.004f, 0.002f, 0.06f), M("C08A55", 0.3f), new Vector3(0, 30 + k * 40, 0));
                    // iron rim with rivets, two iron bands with nails
                    Ring(sh, Vector3.zero, 0.25f, 0.025f, iron);
                    for (int k = 0; k < 16; k++) { float a = k * 22.5f * Mathf.Deg2Rad; BwRivet(sh, new Vector3(Mathf.Cos(a) * 0.228f, -0.016f, Mathf.Sin(a) * 0.228f), 0.016f, iron, Vector3.down); }
                    foreach (var z in new[] { -0.11f, 0.11f })
                    {
                        B(sh, new Vector3(0, -0.015f, z), new Vector3(2 * Mathf.Sqrt(0.0676f - z * z) * 0.95f, 0.006f, 0.04f), iron);
                        for (int k = 0; k < 5; k++) BwRivet(sh, new Vector3(-0.16f + k * 0.08f, -0.019f, z), 0.013f, iron, Vector3.down);
                    }
                    // painted ring, boss with studs, gold star
                    BwRing(sh, new Vector3(0, -0.013f, 0), 0.17f, 0.014f, red, default, new Vector3(1, 0.25f, 1));
                    BwBall(sh, new Vector3(0, -0.012f, 0), new Vector3(0.16f, 0.08f, 0.16f), iron);
                    BwRing(sh, new Vector3(0, -0.014f, 0), 0.08f, 0.009f, iron);
                    for (int k = 0; k < 8; k++) { float a = (k * 45 + 22.5f) * Mathf.Deg2Rad; BwRivet(sh, new Vector3(Mathf.Cos(a) * 0.105f, -0.017f, Mathf.Sin(a) * 0.105f), 0.018f, gold, Vector3.down); }
                    Msh(Meshes.Star(), sh, new Vector3(0, -0.05f, 0), new Vector3(0.12f, 0.12f, 0.06f), gold, new Vector3(90, 0, 0));
                    // leather wraps where the straps hold it
                    foreach (var x in new[] { -0.15f, 0.15f }) B(sh, new Vector3(x, 0, -0.19f), new Vector3(0.05f, 0.04f, 0.04f), leather);
                    BwStraps(leather, M("D9B48A", 0.2f), iron);
                    break;
                }
                case 3:
                {
                    var g = Node("Guitar", b, new Vector3(0, -0.05f, -0.06f), new Vector3(0, 0, 35));
                    var red = M("D32F2F", 0.8f, 0.1f); var neck = M("4A2A12", 0.4f); var black = M("1A1A1A"); var chrome = M("D9DEE5", 0.9f, 0.9f); var white = M("F5F5F5", 0.5f); var board = M("2A1A0E", 0.3f); var gold = M("FFC83A", 0.8f, 0.8f);
                    float Fz(float x, float y) { float u = x / 0.17f, v = (y + 0.12f) / 0.16f; return -0.05f * Mathf.Sqrt(Mathf.Max(0.05f, 1 - u * u - v * v)); }
                    // body, neck, headstock, pickguard
                    BwBall(g, new Vector3(0, -0.12f, 0), new Vector3(0.34f, 0.32f, 0.1f), red);
                    BwBall(g, new Vector3(0, 0.06f, 0), new Vector3(0.26f, 0.24f, 0.1f), red);
                    B(g, new Vector3(0, 0.36f, 0), new Vector3(0.06f, 0.42f, 0.04f), neck);
                    B(g, new Vector3(0, 0.6f, 0), new Vector3(0.1f, 0.1f, 0.04f), black);
                    Ls(g, new Vector3(0.03f, -0.1f, -0.049f), new Vector3(0.16f, 0.2f, 0.01f), white);
                    // fretboard, frets, dots, nut
                    B(g, new Vector3(0, 0.36f, -0.023f), new Vector3(0.05f, 0.4f, 0.008f), board);
                    float fy = 0.555f;
                    for (int k = 0; k < 10; k++)
                    {
                        float step = 0.05f * Mathf.Pow(0.94f, k); float ny = fy - step;
                        B(g, new Vector3(0, ny, -0.028f), new Vector3(0.052f, 0.004f, 0.004f), chrome);
                        if (k == 2 || k == 4 || k == 6 || k == 8) BwDot(g, new Vector3(0, (fy + ny) / 2, -0.0275f), 0.01f, white);
                        fy = ny;
                    }
                    B(g, new Vector3(0, 0.562f, -0.026f), new Vector3(0.052f, 0.008f, 0.008f), white);
                    // tuning pegs (posts on the face + keys at the sides) and a logo
                    for (int k = 0; k < 3; k++)
                        foreach (var sd in new[] { -1f, 1f })
                        {
                            float y = 0.57f + k * 0.03f;
                            Lc(g, new Vector3(sd * 0.03f, y, -0.026f), new Vector3(0.012f, 0.008f, 0.012f), chrome, new Vector3(90, 0, 0));
                            B(g, new Vector3(sd * 0.065f, y, 0), new Vector3(0.03f, 0.016f, 0.008f), chrome);
                        }
                    B(g, new Vector3(0, 0.63f, -0.021f), new Vector3(0.04f, 0.01f, 0.003f), gold);
                    // strings from the nut to the bridge
                    for (int k = 0; k < 6; k++) BwBar(g, new Vector3(-0.02f + k * 0.008f, 0.565f, -0.031f), new Vector3(-0.025f + k * 0.01f, -0.16f, -0.064f), 0.003f, 0.003f, chrome, Vector3.back);
                    // two pickups with pole pieces
                    foreach (var y in new[] { -0.03f, -0.09f })
                    {
                        B(g, new Vector3(0, y, -0.05f), new Vector3(0.07f, 0.022f, 0.01f), black);
                        for (int k = 0; k < 6; k++) BwDot(g, new Vector3(-0.025f + k * 0.01f, y, -0.0555f), 0.006f, chrome);
                    }
                    // bridge with saddles, tremolo arm
                    B(g, new Vector3(0, -0.165f, -0.05f), new Vector3(0.075f, 0.02f, 0.012f), chrome);
                    for (int k = 0; k < 6; k++) B(g, new Vector3(-0.025f + k * 0.01f, -0.16f, -0.058f), new Vector3(0.006f, 0.008f, 0.006f), chrome);
                    LRod(g, new Vector3(0.04f, -0.175f, -0.056f), new Vector3(0.1f, -0.25f, -0.075f), 0.006f, chrome);
                    BwDot(g, new Vector3(0.1f, -0.25f, -0.075f), 0.014f, white);
                    // knobs, pickup switch, strap buttons
                    foreach (var kp in new[] { new Vector2(0.09f, -0.18f), new Vector2(0.12f, -0.12f), new Vector2(0.06f, -0.235f) })
                    {
                        float z = Fz(kp.x, kp.y);
                        Lc(g, new Vector3(kp.x, kp.y, z - 0.004f), new Vector3(0.025f, 0.008f, 0.025f), black, new Vector3(90, 0, 0));
                        BwDot(g, new Vector3(kp.x, kp.y, z - 0.012f), 0.01f, chrome);
                    }
                    LRod(g, new Vector3(-0.08f, 0.08f, -0.035f), new Vector3(-0.09f, 0.09f, -0.06f), 0.006f, chrome);
                    BwDot(g, new Vector3(-0.09f, 0.09f, -0.062f), 0.014f, white);
                    BwDot(g, new Vector3(0, -0.28f, 0), 0.02f, chrome); BwDot(g, new Vector3(-0.1f, 0.15f, 0), 0.02f, chrome);
                    // stickers: lightning bolt and a star
                    Msh(Meshes.Bolt(), g, new Vector3(-0.09f, -0.16f, Fz(-0.09f, -0.16f) - 0.002f), new Vector3(0.1f, 0.1f, 0.03f), M("FFE14D", 0.4f));
                    Msh(Meshes.Star(), g, new Vector3(-0.06f, 0.06f, -0.046f), new Vector3(0.05f, 0.05f, 0.02f), white);
                    // guitar strap round the body (over the left shoulder) with white stars
                    var tilt = Quaternion.Euler(0, 0, -35);
                    BwBand(t => tilt * new Vector3(Mathf.Cos(t * Mathf.PI * 2), 0, Mathf.Sin(t * Mathf.PI * 2)), 14, 0.05f, 0.012f, black, M("E53935", 0.3f));
                    for (int k = 0; k < 5; k++)
                    {
                        var p = BwSurf(tilt * new Vector3(Mathf.Cos((0.12f + k * 0.07f) * Mathf.PI * 2), 0, Mathf.Sin((0.12f + k * 0.07f) * Mathf.PI * 2)), 1.085f, out var n);
                        Msh(Meshes.Star(), Torso, p, new Vector3(0.03f, 0.03f, 0.02f), white).transform.localRotation = Quaternion.LookRotation(n, Vector3.up);
                    }
                    break;
                }
                case 4:
                {
                    var gel = Glass("6EDC3C", 0.6f); var bub = Glass("D8FFB8", 0.5f); var glow = G("C6FF8A", 0.8f); var white = M("FFFFFF", 0.6f); var black = M("102010", 0.6f);
                    var slime = Node("Slime", b, new Vector3(0, 0, -0.12f));
                    Add<Pulse>(slime.gameObject).amount = 0.05f;
                    var r = new Vector3(0.21f, 0.2f, 0.15f);
                    S(slime, Vector3.zero, r * 2, gel);
                    BwBall(slime, new Vector3(0.02f, -0.02f, 0), new Vector3(0.16f, 0.16f, 0.12f), glow);
                    // bubbles floating inside
                    var rng = new Rng(404);
                    for (int k = 0; k < 12; k++) BwDot(slime, Vector3.Scale(new Vector3(rng.F(-1, 1), rng.F(-1, 1), rng.F(-1, 1)).normalized * rng.F(0.3f, 0.75f), r), rng.F(0.02f, 0.05f), bub);
                    // swallowed things: a coin, a bone, a rubber duck
                    var gold = M("FFC83A", 0.8f, 0.8f);
                    Lc(slime, new Vector3(-0.09f, 0.07f, -0.03f), new Vector3(0.06f, 0.005f, 0.06f), gold, new Vector3(70, 20, 0));
                    BwRing(slime, new Vector3(-0.09f, 0.07f, -0.03f), 0.024f, 0.003f, gold, new Vector3(70, 20, 0));
                    var bone = M("F2EAD8", 0.3f);
                    var ba = new Vector3(-0.03f, -0.1f, 0.02f); var bb = new Vector3(0.08f, -0.05f, 0.02f);
                    LRod(slime, ba, bb, 0.018f, bone);
                    foreach (var e in new[] { ba, bb }) foreach (var o in new[] { -1f, 1f }) BwDot(slime, e + new Vector3(-0.008f, 0.012f, 0) * o, 0.022f, bone);
                    var duck = M("FFD23A", 0.4f);
                    Ls(slime, new Vector3(0.08f, 0.07f, -0.04f), new Vector3(0.06f, 0.045f, 0.05f), duck);
                    Ls(slime, new Vector3(0.1f, 0.105f, -0.04f), 0.035f, duck);
                    BwCone(slime, new Vector3(0.112f, 0.1f, -0.04f), new Vector3(0.135f, 0.098f, -0.04f), 0.016f, M("FF8A1E", 0.4f));
                    BwDot(slime, new Vector3(0.11f, 0.112f, -0.056f), 0.007f, black);
                    // cute face on the back: eyes with pupils and highlights, smile, tongue
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        Ls(slime, new Vector3(sd * 0.06f, 0.05f, -0.142f), new Vector3(0.06f, 0.07f, 0.03f), white);
                        Ls(slime, new Vector3(sd * 0.058f, 0.045f, -0.156f), new Vector3(0.032f, 0.036f, 0.01f), black);
                        BwDot(slime, new Vector3(sd * 0.05f + 0.006f, 0.056f, -0.162f), 0.011f, white);
                    }
                    Ls(slime, new Vector3(0, -0.03f, -0.146f), new Vector3(0.07f, 0.03f, 0.02f), black);
                    Ls(slime, new Vector3(0.008f, -0.038f, -0.153f), new Vector3(0.03f, 0.016f, 0.01f), M("FF7AB8", 0.4f));
                    // drips hanging from the bottom + popping bubbles on top
                    foreach (var x in new[] { -0.13f, -0.06f, 0.02f, 0.09f, 0.14f })
                    {
                        float yb = -r.y * Mathf.Sqrt(Mathf.Max(0.1f, 1 - x * x / (r.x * r.x)));
                        Ls(slime, new Vector3(x, yb - 0.01f, -0.04f), new Vector3(0.04f, 0.09f, 0.04f), gel);
                        BwDot(slime, new Vector3(x, yb - 0.07f, -0.04f), 0.032f, gel);
                    }
                    for (int k = 0; k < 4; k++) BwDot(slime, new Vector3(-0.1f + k * 0.065f, 0.19f - Mathf.Abs(k - 1.5f) * 0.02f, -0.03f), 0.03f + (k % 2) * 0.01f, bub);
                    // straps with goo blobs on the shoulders
                    BwStraps(M("2E6B1F", 0.3f), M("C6FF8A", 0.3f), M("2A2E35", 0.4f, 0.4f));
                    foreach (var sd in new[] { -1f, 1f }) foreach (var t in new[] { 0.3f, 0.42f }) { var p = BwSurf(BwStrapDir(sd, t), 1.1f, out _); BwDot(Torso, p, 0.04f, gel); }
                    break;
                }
                case 5:
                {
                    var org = M("F57C00", 0.3f); var dark = M("C25E00", 0.3f); var light = M("FF9A3C", 0.3f); var metal = M("C9D1DB", 0.85f, 0.8f); var white = M("FFFFFF", 0.2f); var black = M("222428", 0.3f);
                    // body with a rounded top, front pocket with a flap and snaps, stitching, top zipper, name tag
                    B(b, new Vector3(0, -0.03f, -0.08f), new Vector3(0.34f, 0.3f, 0.16f), org);
                    Cy(b, new Vector3(0, 0.12f, -0.08f), new Vector3(0.16f, 0.17f, 0.16f), org, new Vector3(0, 0, 90));
                    B(b, new Vector3(0, -0.175f, -0.08f), new Vector3(0.35f, 0.035f, 0.17f), dark);
                    B(b, new Vector3(0, -0.09f, -0.175f), new Vector3(0.24f, 0.12f, 0.04f), light);
                    B(b, new Vector3(0, -0.035f, -0.18f), new Vector3(0.25f, 0.05f, 0.048f), dark);
                    foreach (var x in new[] { -0.07f, 0.07f }) BwRivet(b, new Vector3(x, -0.045f, -0.205f), 0.016f, metal, Vector3.back);
                    Dt.Stitches(b, Dt.Line(new Vector3(-0.11f, -0.145f, -0.196f), new Vector3(0.11f, -0.145f, -0.196f)), 8, 0.016f, 0.004f, white);
                    foreach (var x in new[] { -0.112f, 0.112f }) Dt.Stitches(b, Dt.Line(new Vector3(x, -0.135f, -0.196f), new Vector3(x, -0.07f, -0.196f)), 3, 0.016f, 0.004f, white);
                    Dt.Zipper(b, new Vector3(-0.15f, 0.203f, -0.08f), new Vector3(0.15f, 0.203f, -0.08f), 14, metal, dark, Vector3.up);
                    B(b, new Vector3(0.09f, 0.07f, -0.162f), new Vector3(0.07f, 0.04f, 0.004f), white);
                    for (int k = 0; k < 2; k++) B(b, new Vector3(0.09f, 0.078f - k * 0.016f, -0.165f), new Vector3(0.05f, 0.005f, 0.003f), black);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        // feathered mini wing on a bolted hinge
                        var piv = Node("MiniWing", b, new Vector3(sd * 0.16f, 0.05f, -0.1f));
                        var f = Add<Flap>(piv.gameObject); f.model = this; f.side = sd; f.idleAmp = 15;
                        var h = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0)); float bz = -sd;
                        Lc(piv, Vector3.zero, new Vector3(0.035f, 0.035f, 0.035f), metal);
                        foreach (var y in new[] { -0.03f, 0.03f }) BwDot(piv, new Vector3(0, y, 0), 0.022f, black);
                        Ls(h, new Vector3(0.11f, 0.04f, 0), new Vector3(0.22f, 0.12f, 0.03f), white);
                        for (int k = 0; k < 5; k++)
                        {
                            float a = (25 - k * 20) * Mathf.Deg2Rad; float len = new[] { 0.2f, 0.23f, 0.22f, 0.19f, 0.15f }[k];
                            var a0 = new Vector2(0.05f, 0.04f); BwFeather(h, a0, a0 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len, 0.06f, 0.02f * bz, bz, white, light, M("E8EEF5", 0.2f), 1);
                        }
                        for (int k = 0; k < 3; k++) Ls(h, new Vector3(0.04f + k * 0.035f, 0.06f - k * 0.005f, 0.032f * bz), new Vector3(0.05f, 0.07f, 0.014f), white, new Vector3(0, 0, 20));
                    }
                    BwStraps(dark, white, black);
                    break;
                }
                case 6:
                {
                    var white = M("F2F4F7", 0.7f, 0.2f); var red = M("E53935"); var metal = M("8C949E", 0.8f, 0.8f); var dark = M("555F6B", 0.6f); var blue = M("1E63D9", 0.4f); var black = M("222428", 0.3f);
                    // oxygen tank: body, domes, bands with bolts, back plate
                    B(b, new Vector3(0, 0, -0.005f), new Vector3(0.2f, 0.36f, 0.025f), dark);
                    Cy(b, new Vector3(0, 0, -0.1f), new Vector3(0.24f, 0.2f, 0.24f), white);
                    BwBall(b, new Vector3(0, 0.2f, -0.1f), Vector3.one * 0.24f, white);
                    BwBall(b, new Vector3(0, -0.2f, -0.1f), new Vector3(0.24f, 0.16f, 0.24f), white);
                    Ring(b, new Vector3(0, 0, -0.1f), 0.125f, 0.015f, red);
                    foreach (var y in new[] { -0.13f, 0.13f })
                    {
                        BwRing(b, new Vector3(0, y, -0.1f), 0.122f, 0.008f, metal);
                        for (int k = 0; k < 4; k++) { float a = (200 + k * 47) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); BwRivet(b, new Vector3(0, y, -0.1f) + d * 0.13f, 0.016f, dark, d); }
                    }
                    // valve with a hand wheel and a pressure gauge
                    Lc(b, new Vector3(0, 0.33f, -0.1f), new Vector3(0.04f, 0.02f, 0.04f), metal);
                    B(b, new Vector3(0, 0.365f, -0.1f), new Vector3(0.05f, 0.04f, 0.04f), metal);
                    BwRing(b, new Vector3(0, 0.395f, -0.1f), 0.03f, 0.006f, red);
                    for (int k = 0; k < 3; k++) B(b, new Vector3(0, 0.395f, -0.1f), new Vector3(0.058f, 0.005f, 0.006f), red, new Vector3(0, k * 60, 0));
                    BwDot(b, new Vector3(0, 0.398f, -0.1f), 0.016f, metal);
                    Lc(b, new Vector3(0, 0.36f, -0.124f), new Vector3(0.045f, 0.004f, 0.045f), white, new Vector3(90, 0, 0));
                    BwRing(b, new Vector3(0, 0.36f, -0.126f), 0.023f, 0.004f, metal, new Vector3(90, 0, 0));
                    B(b, new Vector3(0.004f, 0.364f, -0.129f), new Vector3(0.003f, 0.016f, 0.002f), red, new Vector3(0, 0, -50));
                    // corrugated hose over the shoulder to the helmet
                    Vector3 H(float t) { float u = 1 - t; return u * u * u * new Vector3(0.04f, 0.37f, -0.1f) + 3 * u * u * t * new Vector3(0.25f, 0.43f, -0.13f) + 3 * u * t * t * new Vector3(0.42f, 0.45f, 0.08f) + t * t * t * new Vector3(0.33f, 0.44f, 0.24f); }
                    for (int k = 0; k < 4; k++) LRod(b, H(k / 4f), H((k + 1) / 4f), 0.026f, dark);
                    for (int k = 0; k < 14; k++) { float t = (k + 0.5f) / 14f; BwRingN(b, H(t), 0.016f, 0.006f, dark, H(t + 0.01f) - H(t - 0.01f)); }
                    Lc(b, H(0), new Vector3(0.04f, 0.015f, 0.04f), metal).transform.localRotation = Quaternion.FromToRotation(Vector3.up, H(0.02f) - H(0));
                    Lc(b, H(0.9f), new Vector3(0.042f, 0.018f, 0.042f), metal).transform.localRotation = Quaternion.FromToRotation(Vector3.up, H(0.92f) - H(0.88f));
                    // "O2" label
                    B(b, new Vector3(0, 0.06f, -0.222f), new Vector3(0.1f, 0.06f, 0.004f), blue);
                    BwRing(b, new Vector3(-0.02f, 0.06f, -0.225f), 0.014f, 0.004f, white, new Vector3(90, 0, 0), new Vector3(0.8f, 1, 1));
                    B(b, new Vector3(0.016f, 0.052f, -0.225f), new Vector3(0.016f, 0.005f, 0.003f), white);
                    B(b, new Vector3(0.016f, 0.06f, -0.225f), new Vector3(0.005f, 0.018f, 0.003f), white, new Vector3(0, 0, -40));
                    B(b, new Vector3(0.016f, 0.07f, -0.225f), new Vector3(0.012f, 0.005f, 0.003f), white);
                    BwStraps(M("8A939E", 0.4f), white, black);
                    break;
                }
                case 7:
                {
                    var belt = M("222222"); var stitch = M("777777"); var yel = M("FFC400", 0.4f); var dyel = M("F2A900", 0.4f); var metal = M("C9D1DB", 0.85f, 0.8f); var plastic = M("333333", 0.5f);
                    // waist belt with a side-release buckle and an adjuster (front part hidden by the pouch is skipped)
                    BwBand(t => Dt.OnTorso(108, Mathf.Lerp(125, 410, t)) - Dt.TorsoC, 14, 0.045f, 0.012f, belt, stitch, 1.05f);
                    var bp = BwSurf(Dt.OnTorso(108, 200) - Dt.TorsoC, 1.09f, out var bn); var bq = BwRot(Vector3.up, bn);
                    B(Torso, bp, new Vector3(0.06f, 0.05f, 0.016f), plastic).transform.localRotation = bq;
                    B(Torso, bp + bq * new Vector3(0.04f, 0, 0), new Vector3(0.03f, 0.04f, 0.014f), plastic).transform.localRotation = bq;
                    B(Torso, bp + bq * new Vector3(-0.01f, 0, 0.009f), new Vector3(0.02f, 0.02f, 0.004f), metal).transform.localRotation = bq;
                    var ap = BwSurf(Dt.OnTorso(108, 330) - Dt.TorsoC, 1.09f, out var an);
                    B(Torso, ap, new Vector3(0.025f, 0.055f, 0.014f), plastic).transform.localRotation = BwRot(Vector3.up, an);
                    // pouch with zipper along the top arc, stitched seam, front pocket with its own zipper
                    var c = new Vector3(0.05f, 0.12f, 0.285f); var pr = new Vector3(0.15f, 0.07f, 0.055f);
                    BwBall(Torso, c, pr * 2, yel);
                    System.Func<float, Vector3> zp = t => Dt.Ell(c, pr, 52, Mathf.Lerp(18, 162, t), 1.03f);
                    for (int k = 0; k < 4; k++) BwBar(Torso, zp(k / 4f), zp((k + 1) / 4f), 0.02f, 0.004f, belt, Dt.EllN(pr, 52, Mathf.Lerp(18, 162, (k + 0.5f) / 4f)));
                    Dt.Path(zp, 16, (p, d, k) => BwBar(Torso, p - d * 0.005f, p + d * 0.005f, 0.008f, 0.007f, metal, p - c));
                    var zt = zp(0.82f); var zn = (zt - c).normalized;
                    B(Torso, zt + zn * 0.006f, new Vector3(0.018f, 0.014f, 0.012f), metal).transform.localRotation = BwRot(Vector3.down, zn);
                    LRod(Torso, zt + zn * 0.01f, new Vector3(zt.x, zt.y - 0.04f, c.z + pr.z + 0.014f), 0.006f, belt);
                    Dt.Path(t => c + new Vector3(Mathf.Cos(t * Mathf.PI * 2) * pr.x * 1.01f, Mathf.Sin(t * Mathf.PI * 2) * pr.y * 1.01f, 0.012f), 14, (p, d, k) => BwBar(Torso, p - d * 0.007f, p + d * 0.007f, 0.004f, 0.004f, stitch, Vector3.forward));
                    var pp = Dt.Ell(c, pr, 112, 90, 0.98f);
                    BwBall(Torso, pp, new Vector3(0.2f, 0.06f, 0.03f), dyel);
                    Dt.Zipper(Torso, pp + new Vector3(-0.08f, 0.024f, 0.012f), pp + new Vector3(0.08f, 0.024f, 0.012f), 8, metal, belt, Vector3.forward);
                    // banana charm hanging from the pull + side webbing loops
                    var ch = new Vector3(zt.x, zt.y - 0.045f, c.z + pr.z + 0.014f);
                    BwRing(Torso, ch, 0.008f, 0.0025f, metal, new Vector3(90, 0, 0));
                    var ban = M("FFE14D", 0.4f);
                    for (int k = 0; k < 4; k++) { float a = (-40 + k * 27) * Mathf.Deg2Rad; Ls(Torso, ch + new Vector3(Mathf.Sin(a) * 0.035f, -0.03f - Mathf.Cos(a) * 0.012f, 0.005f), new Vector3(0.022f, 0.026f, 0.02f), ban); }
                    foreach (var x in new[] { -0.03f, 0.03f }) BwDot(Torso, ch + new Vector3(x * 1.2f, -0.018f, 0.005f), 0.01f, M("6B4A1E", 0.3f));
                    foreach (var sd in new[] { -1f, 1f }) B(Torso, c + new Vector3(sd * 0.148f, 0, -0.01f), new Vector3(0.01f, 0.045f, 0.03f), belt);
                    break;
                }
                case 102:
                {
                    var white = M("F2F4F7", 0.7f, 0.2f); var red = M("E53935", 0.6f); var metal = M("8C949E", 0.8f, 0.8f); var dark = M("3D434D", 0.5f, 0.5f); var glass = G("8FE6FF", 1.4f);
                    B(b, new Vector3(0, 0, -0.012f), new Vector3(0.26f, 0.42f, 0.025f), dark);
                    var r = Node("Rocket", b, new Vector3(0, 0, -0.14f));
                    // body, nose with a tip and band, panel rings, rivet lines
                    Cy(r, Vector3.zero, new Vector3(0.22f, 0.24f, 0.22f), white);
                    Cone(r, new Vector3(0, 0.24f, 0), new Vector3(0.22f, 0.24f, 0.22f), red);
                    BwDot(r, new Vector3(0, 0.47f, 0), 0.03f, metal);
                    BwRing(r, new Vector3(0, 0.237f, 0), 0.112f, 0.008f, red);
                    foreach (var y in new[] { -0.06f, 0.06f }) BwRing(r, new Vector3(0, y, 0), 0.111f, 0.006f, metal);
                    foreach (var a in new[] { 210f, 330f })
                    {
                        var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                        for (int k = 0; k < 7; k++) BwRivet(r, d * 0.111f + Vector3.up * (-0.2f + k * 0.065f), 0.011f, metal, d);
                    }
                    // portholes on the back
                    foreach (var p in new[] { new Vector2(0.11f, 0.035f), new Vector2(-0.1f, 0.022f) })
                    {
                        var pos = new Vector3(0, p.x, -0.112f);
                        BwRing(r, pos, p.y, 0.008f, metal, new Vector3(90, 0, 0));
                        Lc(r, pos, new Vector3(p.y * 1.8f, 0.004f, p.y * 1.8f), glass, new Vector3(90, 0, 0));
                        BwDot(r, pos + new Vector3(-p.y * 0.35f, p.y * 0.35f, -0.005f), p.y * 0.4f, white);
                    }
                    // fins with white edges and rivets
                    for (int k = 0; k < 3; k++)
                    {
                        float a = 60 + k * 120; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        B(r, d * 0.12f + Vector3.down * 0.18f, new Vector3(0.02f, 0.14f, 0.1f), red, new Vector3(0, a, 0));
                        LRod(r, d * 0.17f + Vector3.down * 0.25f, d * 0.17f + Vector3.down * 0.12f, 0.022f, white);
                        foreach (var y in new[] { -0.21f, -0.15f }) BwDot(r, d * 0.12f + Vector3.up * y, 0.024f, metal);
                    }
                    // nozzle bell + two flames
                    Lc(r, new Vector3(0, -0.245f, 0), new Vector3(0.12f, 0.01f, 0.12f), dark);
                    Cone(r, new Vector3(0, -0.31f, 0), new Vector3(0.16f, 0.08f, 0.16f), dark);
                    BwRing(r, new Vector3(0, -0.31f, 0), 0.08f, 0.008f, metal);
                    var fl = Cone(r, new Vector3(0, -0.31f, 0), new Vector3(0.16f, 0.4f, 0.16f), G("FF8A1E", 2.4f), new Vector3(180, 0, 0));
                    Add<Pulse>(fl).amount = 0.2f;
                    fl.SetActive(false); JetFlames.Add(fl);
                    var fc = Cone(r, new Vector3(0, -0.31f, 0), new Vector3(0.09f, 0.24f, 0.09f), G("FFF2A0", 3f), new Vector3(180, 0, 0));
                    Add<Pulse>(fc).amount = 0.25f;
                    fc.SetActive(false); JetFlames.Add(fc);
                    // red star on the side, clamps holding the rocket to the back plate
                    Msh(Meshes.Star(), r, new Vector3(0.112f, 0.0f, 0), new Vector3(0.07f, 0.07f, 0.03f), red, new Vector3(0, 90, 0));
                    Msh(Meshes.Star(), r, new Vector3(-0.112f, 0.0f, 0), new Vector3(0.07f, 0.07f, 0.03f), red, new Vector3(0, 90, 0));
                    foreach (var y in new[] { -0.17f, 0.17f })
                    {
                        BwRing(r, new Vector3(0, y, 0), 0.116f, 0.01f, metal);
                        foreach (var x in new[] { -0.07f, 0.07f }) B(r, new Vector3(x, y, 0.1f), new Vector3(0.02f, 0.025f, 0.06f), metal);
                        BwRivet(r, new Vector3(0, y, -0.126f), 0.018f, dark, Vector3.back);
                    }
                    BwStraps(dark, M("E53935", 0.3f), metal);
                    HasJet = true;
                    break;
                }
            }
        }

        // ================= tails =================

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
            // segment space: +Y along the tail, +Z = upper (dorsal) side
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:
                {
                    // fluffy cat tail: furry segments, pointed tip tufts, ribbon bow with a bell at the base
                    Chain(6, 0.08f, 12, (t, k) =>
                    {
                        float d = 0.14f - k * 0.008f; var c = new Vector3(0, 0.04f, 0);
                        BwBall(t, c, Vector3.one * d, Skin);
                        BwFur(t, c, Vector3.one * d * 0.46f, 16, 0.052f - k * 0.002f, Skin, 100 + k);
                        if (k == 5) BwTufts(t, c, Vector3.one * d * 0.5f, 6, 0.06f, 0.035f, Skin, 120, 0, 60, 1f);
                        if (k == 0)
                        {
                            var pink = M("FF6FA8", 0.4f); var gold = M("FFC83A", 0.85f, 0.8f);
                            BwRing(t, new Vector3(0, 0.05f, 0), 0.072f, 0.012f, pink);
                            foreach (var sd in new[] { -1f, 1f }) Ls(t, new Vector3(sd * 0.04f, 0.05f, 0.08f), new Vector3(0.06f, 0.04f, 0.03f), pink, new Vector3(0, 0, sd * 25));
                            Ls(t, new Vector3(0, 0.05f, 0.085f), 0.03f, pink);
                            BwDot(t, new Vector3(0.075f, 0.04f, 0.03f), 0.045f, gold);
                            B(t, new Vector3(0.096f, 0.035f, 0.035f), new Vector3(0.004f, 0.022f, 0.006f), M("3A2A10"));
                            BwRing(t, new Vector3(0.07f, 0.065f, 0.03f), 0.008f, 0.0025f, gold, new Vector3(0, 0, 90));
                        }
                    });
                    break;
                }
                case 1:
                {
                    var yel = G("FFE14D", 2.4f); var white = G("FFFFFF", 3f); var arc = G("E8FBFF", 3f); var copper = M("D9822B", 0.7f, 0.8f); var dark = M("2A2E35", 0.5f, 0.5f); var metal = M("C9D1DB", 0.85f, 0.8f); var ins = M("F2F4F7", 0.5f);
                    // mount on the back
                    Lc(root, Vector3.zero, new Vector3(0.12f, 0.012f, 0.12f), dark);
                    BwRing(root, new Vector3(0, 0.01f, 0), 0.055f, 0.006f, metal);
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; BwRivet(root, new Vector3(Mathf.Cos(a) * 0.045f, 0.012f, Mathf.Sin(a) * 0.045f), 0.011f, metal, Vector3.up); }
                    // the bolt sticks up behind the back and wags
                    var bn = Node("Bolt", root, Vector3.zero, new Vector3(65, 0, 0));
                    var sw = Add<Sway>(bn.gameObject); sw.model = this; sw.amp = 8;
                    // tesla coil at its base: core, insulators, copper windings, ball
                    Lc(bn, new Vector3(0, 0.035f, 0), new Vector3(0.032f, 0.035f, 0.032f), dark);
                    foreach (var y in new[] { 0.004f, 0.07f }) Lc(bn, new Vector3(0, y, 0), new Vector3(0.06f, 0.004f, 0.06f), ins);
                    for (int k = 0; k < 10; k++) BwRing(bn, new Vector3(0, 0.01f + k * 0.0062f, 0), 0.022f, 0.0033f, copper);
                    Ls(bn, new Vector3(0, 0.085f, 0), 0.045f, metal);
                    // main bolt (colour cycling), white core, soft halo, energy rings
                    var bolt = Msh(Meshes.Bolt(), bn, new Vector3(0.013f, 0.38f, 0), Vector3.one * 0.55f, yel, new Vector3(0, 0, 19.3f));
                    Add<Hue>(bolt).glow = 2f;
                    Msh(Meshes.Bolt(), bn, new Vector3(0.013f, 0.38f, 0), new Vector3(0.36f, 0.4f, 1f), white, new Vector3(0, 0, 19.3f));
                    Ls(bn, new Vector3(0, 0.38f, 0), new Vector3(0.3f, 0.66f, 0.1f), Add("FFF6B0", 0.16f));
                    foreach (var y in new[] { 0.2f, 0.38f, 0.56f }) BwRing(bn, new Vector3(0, y, 0), 0.11f, 0.004f, arc, new Vector3(12, 0, 8));
                    // branch bolts, crawling arcs, sparks
                    var rng = new Rng(77);
                    for (int k = 0; k < 6; k++)
                        Msh(Meshes.Bolt(), bn, new Vector3((k % 2 == 0 ? -1 : 1) * rng.F(0.08f, 0.14f), 0.14f + k * 0.08f, rng.F(-0.02f, 0.02f)), new Vector3(0.13f, 0.13f, 0.5f), yel, new Vector3(0, 0, rng.F(-40, 40)));
                    for (int a = 0; a < 6; a++)
                    {
                        var prev = new Vector3(rng.F(-0.03f, 0.03f), 0.09f, rng.F(-0.02f, 0.02f));
                        for (int k = 1; k <= 7; k++)
                        {
                            var cur = new Vector3(rng.F(-0.12f, 0.12f), 0.09f + k * 0.08f, rng.F(-0.05f, 0.05f));
                            BwBar(bn, prev, cur, 0.006f, 0.006f, arc, Vector3.forward);
                            prev = cur;
                        }
                    }
                    for (int k = 0; k < 30; k++) BwDot(bn, new Vector3(rng.F(-0.16f, 0.16f), rng.F(0.06f, 0.72f), rng.F(-0.07f, 0.07f)), rng.F(0.012f, 0.028f), k % 3 == 0 ? yel : white);
                    break;
                }
                case 2:
                {
                    var metal = M("9AA5B4", 0.8f, 0.7f); var dark = M("555F6B", 0.6f, 0.6f); var chrome = M("D9DEE5", 0.9f, 0.9f); var cable = M("E53935", 0.4f);
                    // socket on the back
                    Lc(root, new Vector3(0, -0.005f, 0), new Vector3(0.12f, 0.015f, 0.12f), dark);
                    BwRing(root, new Vector3(0, 0.008f, 0), 0.05f, 0.007f, metal);
                    for (int k = 0; k < 4; k++) { float a = (k * 90 + 45) * Mathf.Deg2Rad; BwRivet(root, new Vector3(Mathf.Cos(a) * 0.05f, 0.01f, Mathf.Sin(a) * 0.05f), 0.013f, chrome, Vector3.up); }
                    // segments: housing, end rings, ball joint, bolts, side pistons, dorsal armour plate, cable
                    var end = Chain(6, 0.08f, 10, (t, k) =>
                    {
                        float s = 1 - k * 0.07f, R = 0.045f * s;
                        Cy(t, new Vector3(0, 0.04f, 0), new Vector3(0.09f * s, 0.035f, 0.09f * s), metal);
                        foreach (var y in new[] { 0.006f, 0.074f }) BwRing(t, new Vector3(0, y, 0), R, 0.006f, dark);
                        Ls(t, Vector3.zero, 0.06f * s, dark);
                        for (int j = 0; j < 4; j++) { float a = (j * 90 + 45) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); BwRivet(t, new Vector3(0, 0.04f, 0) + d * (R + 0.002f), 0.012f, chrome, d); }
                        foreach (var sd in new[] { -1f, 1f })
                        {
                            LRod(t, new Vector3(sd * (R + 0.012f), -0.01f, 0), new Vector3(sd * (R + 0.012f), 0.05f, 0), 0.017f, dark);
                            LRod(t, new Vector3(sd * (R + 0.012f), 0.05f, 0), new Vector3(sd * (R + 0.012f), 0.09f, 0), 0.008f, chrome);
                        }
                        B(t, new Vector3(0, 0.04f, R + 0.006f), new Vector3(0.05f * s, 0.06f, 0.012f), metal);
                        foreach (var y in new[] { 0.018f, 0.062f }) BwRivet(t, new Vector3(0, y, R + 0.013f), 0.01f, chrome, Vector3.forward);
                        LRod(t, new Vector3(-R * 0.75f, 0, -R * 0.75f), new Vector3(-R * 0.75f, 0.08f, -R * 0.75f), 0.01f, cable);
                    }, 6);
                    // claw: base, two fingers with tips, hinge pins, red sensor eye
                    Lc(end, new Vector3(0, 0.09f, 0), new Vector3(0.06f, 0.012f, 0.06f), dark);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        B(end, new Vector3(sd * 0.03f, 0.13f, 0), new Vector3(0.02f, 0.08f, 0.03f), metal, new Vector3(0, 0, sd * 20));
                        BwCone(end, new Vector3(sd * 0.044f, 0.165f, 0), new Vector3(sd * 0.03f, 0.2f, 0), 0.022f, chrome);
                        BwDot(end, new Vector3(sd * 0.022f, 0.1f, 0.016f), 0.012f, chrome);
                    }
                    BwDot(end, new Vector3(0, 0.1f, 0.028f), 0.016f, G("FF3030", 2.5f));
                    break;
                }
                case 3:
                case 100:
                {
                    bool lava = set == 0;
                    var m = lava ? G("FF5A1F", 1.4f) : M("2E7D32", 0.5f);
                    var spike = lava ? G("FFD23A", 2f) : M("C6FF8A", 0.4f);
                    var scl = lava ? M("3A2420", 0.15f) : M("43A047", 0.55f);
                    var ridge = lava ? G("FFB020", 2.2f) : M("1B5E20", 0.4f);
                    var belly = M("D8E86A", 0.4f); var drip = G("FF8A1E", 2f);
                    // scales round the base
                    for (int k = 0; k < 5; k++) { float a = (k * 72 + 18) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); Ls(root, d * 0.075f + Vector3.up * 0.01f, new Vector3(0.06f, 0.025f, 0.06f), scl).transform.localRotation = Quaternion.FromToRotation(Vector3.up, d + Vector3.up); }
                    // segments: core, overlapping scales (lava: dark crust all round), belly plate / lava drip, dorsal spike + fin, side spikes
                    var end = Chain(6, 0.085f, 8, (t, k) =>
                    {
                        float d = 0.15f - k * 0.017f; var c = new Vector3(0, 0.04f, 0); var rr = new Vector3(d / 2, 0.06f, d / 2);
                        float p0 = lava ? 0 : 5, p1 = lava ? 360 : 175;
                        BwBall(t, c, new Vector3(d, 0.12f, d), m);
                        Dt.Scales(t, (u, v) => Dt.Ell(c, rr, Mathf.Lerp(62, 112, v), Mathf.Lerp(p0, p1, u), 1.02f), (u, v) => Dt.EllN(rr, Mathf.Lerp(62, 112, v), Mathf.Lerp(p0, p1, u)), 2, 6, d * (lava ? 0.48f : 0.38f), scl);
                        if (lava) BwDot(t, new Vector3(0, 0.03f, -d * 0.5f - 0.012f), 0.028f - k * 0.002f, drip);
                        else B(t, new Vector3(0, 0.04f, -d * 0.47f), new Vector3(d * 0.5f, 0.075f, 0.014f), belly);
                        BwCone(t, new Vector3(0, 0.03f, d * 0.42f), new Vector3(0, 0.09f, d * 0.42f + 0.07f), 0.06f - k * 0.004f, spike);
                        B(t, new Vector3(0, 0.075f, d * 0.44f + 0.012f), new Vector3(0.008f, 0.05f, 0.035f), ridge);
                        foreach (var sd in new[] { -1f, 1f }) BwCone(t, new Vector3(sd * d * 0.44f, 0.04f, d * 0.12f), new Vector3(sd * (d * 0.44f + 0.045f), 0.075f, d * 0.12f + 0.01f), 0.028f, spike);
                    });
                    // spade tip with barbs
                    Cone(end, new Vector3(0, 0.08f, 0), new Vector3(0.1f, 0.14f, 0.05f), spike);
                    foreach (var sd in new[] { -1f, 1f }) BwCone(end, new Vector3(sd * 0.03f, 0.1f, 0), new Vector3(sd * 0.085f, 0.05f, 0), 0.04f, spike);
                    break;
                }
                case 4:
                {
                    var org = M("FF7A1E", 0.1f); var wht = M("FFFFFF", 0.1f); var drk = M("E0560A", 0.1f);
                    var t = Node("Fox", root, Vector3.zero, new Vector3(15, 0, 0));
                    var c = new Vector3(0, 0.22f, 0); var r = new Vector3(0.11f, 0.21f, 0.11f); var tc = new Vector3(0, 0.44f, 0);
                    S(t, c, r * 2, org);
                    S(t, tc, Vector3.one * 0.16f, wht);
                    // fluffy fur, pointed tufts and darker streaks along the tail
                    BwFur(t, c, r * 0.97f, 42, 0.065f, org, 41, 20, 165);
                    BwTufts(t, c, r, 22, 0.06f, 0.04f, org, 43, 25, 160);
                    var g = new Rng(44);
                    for (int k = 0; k < 8; k++)
                    {
                        float th = g.F(50, 130), ph = k * 45 + g.F(-15, 15);
                        Ls(t, Dt.Ell(c, r, th, ph, 1.01f), new Vector3(0.03f, 0.11f, 0.02f), drk).transform.localRotation = BwRot(Vector3.up, Dt.EllN(r, th, ph));
                    }
                    // white tip: fluff + pointed brush
                    BwFur(t, tc, Vector3.one * 0.08f, 18, 0.05f, wht, 45, 0, 120);
                    BwTufts(t, tc, Vector3.one * 0.08f, 7, 0.07f, 0.04f, wht, 46, 0, 50, 1f);
                    // fluff ring at the root
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; BwDot(t, new Vector3(Mathf.Cos(a) * 0.065f, 0.04f, Mathf.Sin(a) * 0.065f), 0.06f, org); }
                    var sw = Add<Sway>(t.gameObject); sw.model = this; sw.amp = 14;
                    break;
                }
                case 5:
                {
                    var pink = G("FF3FA4", 2.2f); var halo = Add("FF7AC8", 0.35f); var spark = G("FFE0F4", 3f); var metal = M("555F6B", 0.7f, 0.6f); var chrome = M("C9D1DB", 0.85f, 0.8f);
                    // emitter socket: base, rings, bolts, fins
                    Lc(root, Vector3.zero, new Vector3(0.11f, 0.02f, 0.11f), metal);
                    for (int k = 0; k < 3; k++) BwRing(root, new Vector3(0, 0.02f + k * 0.018f, 0), 0.04f - k * 0.006f, 0.007f, k == 1 ? pink : chrome);
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; BwRivet(root, new Vector3(Mathf.Cos(a) * 0.045f, 0.02f, Mathf.Sin(a) * 0.045f), 0.012f, chrome, Vector3.up); }
                    for (int k = 0; k < 4; k++) B(root, new Vector3(0, 0.03f, 0), new Vector3(0.11f, 0.02f, 0.006f), metal, new Vector3(0, k * 45 + 22.5f, 0));
                    // energy lash: core, halo, orbit ring, sparks, little arcs
                    var rng = new Rng(55);
                    var end = Chain(10, 0.07f, 8, (t, k) =>
                    {
                        float d = 0.07f - k * 0.004f; var c = new Vector3(0, 0.03f, 0);
                        Ls(t, c, d, pink);
                        Ls(t, c, d * 1.6f, halo);
                        BwRing(t, c, d * 0.7f, 0.004f, spark, new Vector3(25 * ((k % 3) - 1), k * 37, 0));
                        for (int j = 0; j < 3; j++) BwDot(t, c + new Vector3(rng.F(-1, 1), rng.F(-1, 1), rng.F(-1, 1)).normalized * d * 0.95f, 0.012f, spark);
                        for (int j = 0; j < 3; j++)
                        {
                            var o = new Vector3(rng.F(-1, 1), 0, rng.F(-1, 1)).normalized * d * 0.6f;
                            BwBar(t, c + o, c + o * 1.9f + Vector3.up * rng.F(-0.02f, 0.02f), 0.004f, 0.004f, spark, Vector3.forward);
                        }
                    }, 14);
                    // cracker tip: flaring flames, star, sparks
                    for (int k = 0; k < 3; k++) BwCone(end, new Vector3(0, 0.05f, 0), new Vector3((k - 1) * 0.03f, 0.13f - Mathf.Abs(k - 1) * 0.02f, 0), 0.04f, k == 1 ? spark : pink);
                    Msh(Meshes.Star(), end, new Vector3(0, 0.08f, 0), new Vector3(0.06f, 0.06f, 0.03f), spark);
                    for (int k = 0; k < 4; k++) BwDot(end, new Vector3(Mathf.Cos(k * 1.57f) * 0.05f, 0.1f + (k % 2) * 0.03f, Mathf.Sin(k * 1.57f) * 0.05f), 0.014f, spark);
                    break;
                }
                case 6:
                {
                    var metal = M("B9C3CF", 0.85f, 0.8f); var white = M("F2F4F7", 0.5f); var dark = M("2A2E35", 0.5f, 0.5f); var cyan = G("35E0FF", 2.5f);
                    // mount plate with bolts and a glowing ring
                    B(root, Vector3.zero, new Vector3(0.14f, 0.02f, 0.14f), dark);
                    foreach (var x in new[] { -0.05f, 0.05f }) foreach (var z in new[] { -0.05f, 0.05f }) BwRivet(root, new Vector3(x, 0.012f, z), 0.014f, metal, Vector3.up);
                    BwRing(root, new Vector3(0, 0.012f, 0), 0.035f, 0.005f, cyan);
                    BwDot(root, new Vector3(0, 0.012f, 0), 0.02f, metal);
                    // segments: block, white dorsal plate with bolts, side vents, hinge with glowing caps, LED, edge light strips
                    var end = Chain(5, 0.09f, 10, (t, k) =>
                    {
                        float wd = 0.1f - k * 0.01f;
                        B(t, new Vector3(0, 0.045f, 0), new Vector3(wd, 0.08f, wd), metal);
                        B(t, new Vector3(0, 0.045f, wd / 2 + 0.005f), new Vector3(wd * 0.8f, 0.065f, 0.01f), white);
                        foreach (var sd in new[] { -1f, 1f })
                        {
                            for (int j = 0; j < 3; j++) B(t, new Vector3(sd * (wd / 2 + 0.003f), 0.025f + j * 0.02f, 0), new Vector3(0.006f, 0.008f, wd * 0.6f), dark);
                            BwDot(t, new Vector3(sd * wd * 0.56f, 0, 0), 0.022f, cyan);
                            B(t, new Vector3(sd * wd / 2, 0.045f, wd / 2), new Vector3(0.006f, 0.06f, 0.006f), cyan);
                            foreach (var y in new[] { 0.02f, 0.07f }) BwRivet(t, new Vector3(sd * wd * 0.32f, y, wd / 2 + 0.011f), 0.01f, metal, Vector3.forward);
                        }
                        Lc(t, Vector3.zero, new Vector3(0.05f, wd * 0.55f, 0.05f), dark, new Vector3(0, 0, 90));
                        B(t, new Vector3(0, 0.045f, -wd / 2 - 0.004f), new Vector3(wd * 0.7f, 0.06f, 0.008f), dark);
                        BwDot(t, new Vector3(0, 0.045f, wd / 2 + 0.012f), 0.018f, cyan);
                    }, 7);
                    // pulsing LED tip in a ring with three fins
                    var led = S(end, new Vector3(0, 0.12f, 0), 0.07f, cyan); Add<Pulse>(led);
                    BwRing(end, new Vector3(0, 0.1f, 0), 0.035f, 0.007f, metal);
                    for (int k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120, 0) * Vector3.forward; B(end, d * 0.04f + Vector3.up * 0.1f, new Vector3(0.006f, 0.05f, 0.03f), white, new Vector3(0, k * 120, 0)); }
                    break;
                }
                case 7:
                {
                    // bunny pom-pom: soft core in two fluff layers and pointed wisps; gently wiggles
                    var puff = Node("Puff", root, Vector3.zero);
                    var sw = Add<Sway>(puff.gameObject); sw.model = this; sw.amp = 6; sw.speed = 4;
                    var c = new Vector3(0, 0.05f, 0);
                    BwBall(puff, c, Vector3.one * 0.15f, Skin);
                    BwFur(puff, c, Vector3.one * 0.075f, 40, 0.055f, Skin, 71, 0, 150);
                    BwFur(puff, c, Vector3.one * 0.09f, 46, 0.045f, Skin, 72, 0, 140);
                    BwTufts(puff, c, Vector3.one * 0.085f, 22, 0.04f, 0.03f, Skin, 73, 0, 130, 0.2f);
                    break;
                }
                case 106:
                {
                    string[] rb = { "FF3B3B", "FF9A2E", "FFE14D", "6EDC3C", "35A8FF", "5B5BFF", "B57CFF" };
                    var cloud = M("FFFFFF", 0.2f); var star = G("FFFFFF", 3f); var gold = G("FFE14D", 2.4f);
                    // little cloud round the root
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; Ls(root, new Vector3(Mathf.Cos(a) * 0.07f, 0.02f + (k % 2) * 0.015f, Mathf.Sin(a) * 0.07f), 0.07f + (k % 3) * 0.012f, cloud); }
                    // rainbow segments: glowing core, same-colour fluff, a white sparkle star on top
                    var end = Chain(7, 0.075f, 10, (t, k) =>
                    {
                        float d = 0.14f - k * 0.01f; var c = new Vector3(0, 0.04f, 0); var m = G(rb[k], 1.2f);
                        BwBall(t, c, Vector3.one * d, m);
                        BwFur(t, c, Vector3.one * d * 0.48f, 12, 0.05f - k * 0.003f, m, 90 + k);
                        Msh(Meshes.Star(), t, c + new Vector3(0, 0, d * 0.5f + 0.014f), new Vector3(0.035f, 0.035f, 0.02f), star);
                    });
                    // spinning gold star at the tip with sparkles
                    var st = Msh(Meshes.Star(), end, new Vector3(0, 0.13f, 0), new Vector3(0.13f, 0.13f, 0.06f), gold);
                    Add<Spin>(st).speed = 60;
                    for (int k = 0; k < 4; k++) BwDot(end, new Vector3(Mathf.Cos(k * 1.57f + 0.8f) * 0.09f, 0.13f + Mathf.Sin(k * 1.57f + 0.8f) * 0.09f, 0), 0.018f, star);
                    break;
                }
            }
        }
    }
}
