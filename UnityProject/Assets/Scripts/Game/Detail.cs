using System;
using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// Detail kits for wardrobe items (like the weapon kits of Neon Arena): stitches, studs, knit ribs, fur, zippers,
    /// laces, gems with prongs, feathers, scales. Every call adds many small low-poly parts; the model bakes them
    /// into one mesh per material afterwards, so detail is cheap to draw.
    /// </summary>
    public static class Dt
    {
        // ---- the chubrik's body in local space of its nodes ----
        /// <summary>Torso ellipsoid in Torso space.</summary>
        public static readonly Vector3 TorsoC = new Vector3(0, 0.22f, 0), TorsoR = new Vector3(0.33f, 0.3f, 0.29f);
        /// <summary>Head ellipsoid (default head) in HeadC space, centre at zero.</summary>
        public static readonly Vector3 HeadR = new Vector3(0.4f, 0.39f, 0.38f);

        /// <summary>Point on an ellipsoid: theta from +Y down (0 = top, 90 = equator), phi around Y (0 = +X, 90 = +Z front).</summary>
        public static Vector3 Ell(Vector3 c, Vector3 r, float theta, float phi, float grow = 1f)
        {
            float t = theta * Mathf.Deg2Rad, p = phi * Mathf.Deg2Rad;
            return c + new Vector3(Mathf.Sin(t) * Mathf.Cos(p) * r.x, Mathf.Cos(t) * r.y, Mathf.Sin(t) * Mathf.Sin(p) * r.z) * grow;
        }

        /// <summary>Outward normal of the ellipsoid at (theta, phi).</summary>
        public static Vector3 EllN(Vector3 r, float theta, float phi)
        {
            float t = theta * Mathf.Deg2Rad, p = phi * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(t) * Mathf.Cos(p) / r.x, Mathf.Cos(t) / r.y, Mathf.Sin(t) * Mathf.Sin(p) / r.z).normalized;
        }

        /// <summary>Point on the torso surface (Torso space), slightly outside.</summary>
        public static Vector3 OnTorso(float theta, float phi, float grow = 1.03f) => Ell(TorsoC, TorsoR, theta, phi, grow);
        /// <summary>Point on the head surface (HeadC space), slightly outside.</summary>
        public static Vector3 OnHead(float theta, float phi, float grow = 1.03f) => Ell(Vector3.zero, HeadR, theta, phi, grow);

        /// <summary>n samples along a path t∈[0,1]: position, tangent, index.</summary>
        public static void Path(Func<float, Vector3> path, int n, Action<Vector3, Vector3, int> f)
        {
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                var p = path(t);
                var d = path(Mathf.Min(1, t + 0.01f)) - path(Mathf.Max(0, t - 0.01f));
                f(p, d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward, i);
            }
        }

        public static Func<float, Vector3> Line(Vector3 a, Vector3 b) => t => Vector3.Lerp(a, b, t);

        /// <summary>Ellipse around Y at height y (angles in degrees, 90 = front).</summary>
        public static Func<float, Vector3> Circle(Vector3 c, float rx, float rz, float a0 = 0, float a1 = 360) => t =>
        {
            float a = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad;
            return c + new Vector3(Mathf.Cos(a) * rx, 0, Mathf.Sin(a) * rz);
        };

        /// <summary>A line of latitude on the torso (theta) from phi0 to phi1.</summary>
        public static Func<float, Vector3> TorsoRing(float theta, float phi0 = 0, float phi1 = 360, float grow = 1.04f) => t => OnTorso(theta, Mathf.Lerp(phi0, phi1, t), grow);
        /// <summary>A meridian on the torso at phi from theta0 to theta1.</summary>
        public static Func<float, Vector3> TorsoLine(float phi, float theta0, float theta1, float grow = 1.04f) => t => OnTorso(Mathf.Lerp(theta0, theta1, t), phi, grow);
        public static Func<float, Vector3> HeadRing(float theta, float phi0 = 0, float phi1 = 360, float grow = 1.04f) => t => OnHead(theta, Mathf.Lerp(phi0, phi1, t), grow);

        /// <summary>Turns a part so its local +Y points along <paramref name="up"/>.</summary>
        public static GameObject Orient(GameObject go, Vector3 up)
        {
            if (up.sqrMagnitude > 1e-8f) go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, up.normalized);
            return go;
        }

        // ---------------- kits ----------------

        /// <summary>Dashed stitches along a path (small boxes lying along the tangent).</summary>
        public static void Stitches(Transform p, Func<float, Vector3> path, int n, float len, float th, Material m)
        {
            Path(path, n, (pos, dir, i) => Orient(B(p, pos, new Vector3(th, len, th), m), dir));
        }

        /// <summary>Round studs / rivets / beads / buttons along a path.</summary>
        public static void Studs(Transform p, Func<float, Vector3> path, int n, float d, Material m, float flat = 1f)
        {
            Path(path, n, (pos, dir, i) => Ls(p, pos, new Vector3(d, d * flat, d), m));
        }

        /// <summary>Spikes along a path pointing away from <paramref name="centre"/>.</summary>
        public static void Spikes(Transform p, Func<float, Vector3> path, int n, Vector3 centre, float h, float d, Material m)
        {
            Path(path, n, (pos, dir, i) => Spike(p, pos, pos + (pos - centre).normalized * h, d, m));
        }

        /// <summary>Knit ribbing / bands: stacked thin rings around Y.</summary>
        public static void Ribs(Transform p, Vector3 c, float R, float r, int rows, float step, Material m, Vector3 euler = default, Vector3? scale = null)
        {
            var rot = Quaternion.Euler(euler);
            for (int k = 0; k < rows; k++) Ring(p, c + rot * Vector3.up * (k * step), R, r, m, euler, scale);
        }

        /// <summary>Fur / fluff: small balls scattered over an ellipsoid cap (theta up to thetaMax).</summary>
        public static void Fur(Transform p, Vector3 c, Vector3 radii, int n, float d, Material m, int seed, float thetaMax = 180, float phi0 = 0, float phi1 = 360)
        {
            var r = new Rng(seed);
            for (int k = 0; k < n; k++)
            {
                float th = Mathf.Acos(1 - 2 * r.F(0, (1 - Mathf.Cos(thetaMax * Mathf.Deg2Rad)) / 2)) * Mathf.Rad2Deg;
                float ph = r.F(phi0, phi1);
                Ls(p, Ell(c, radii, th, ph), d * r.F(0.7f, 1.2f), m);
            }
        }

        /// <summary>Zipper: tape, alternating teeth and a pull tab at the top (a = bottom, b = top).</summary>
        public static void Zipper(Transform p, Vector3 a, Vector3 b, int teeth, Material metal, Material tape = null, Vector3? outward = null)
        {
            var dir = (b - a).normalized;
            var o = outward ?? Vector3.forward;
            var side = Vector3.Cross(dir, o).normalized;
            if (tape != null) Orient(B(p, (a + b) / 2 - o * 0.004f, new Vector3(0.03f, (b - a).magnitude, 0.006f), tape), dir);
            for (int k = 0; k < teeth; k++)
            {
                var pos = Vector3.Lerp(a, b, (k + 0.5f) / teeth) + side * ((k % 2 == 0 ? 1 : -1) * 0.004f);
                Orient(B(p, pos, new Vector3(0.012f, 0.006f, 0.008f), metal), dir);
            }
            Orient(B(p, b + o * 0.006f, new Vector3(0.018f, 0.02f, 0.01f), metal), dir);
            Orient(B(p, b - dir * 0.022f + o * 0.01f, new Vector3(0.012f, 0.03f, 0.005f), metal), dir);
        }

        /// <summary>Criss-cross laces between two rows of eyelets (a→b is the lacing direction, halfW across).</summary>
        public static void Laces(Transform p, Vector3 a, Vector3 b, Vector3 across, int crosses, Material lace, Material eyelet = null)
        {
            var w = across;
            for (int k = 0; k <= crosses; k++)
            {
                var c = Vector3.Lerp(a, b, k / (float)crosses);
                if (eyelet != null) { Ring(p, c + w, 0.008f, 0.003f, eyelet, new Vector3(90, 0, 0)); Ring(p, c - w, 0.008f, 0.003f, eyelet, new Vector3(90, 0, 0)); }
                if (k < crosses)
                {
                    var n = Vector3.Lerp(a, b, (k + 1) / (float)crosses);
                    LRod(p, c + w, n - w, 0.007f, lace);
                    LRod(p, c - w, n + w, 0.007f, lace);
                }
            }
            var top = b;
            LRod(p, top + w, top + w * 0.2f + Vector3.down * 0.05f, 0.006f, lace);
            LRod(p, top - w, top - w * 0.2f + Vector3.down * 0.05f, 0.006f, lace);
        }

        /// <summary>Gem in a setting: the stone, a collar and 4 prongs (normal = facing direction).</summary>
        public static void Gem(Transform p, Vector3 pos, float d, Material gem, Material setting, Vector3 normal)
        {
            var q = Quaternion.FromToRotation(Vector3.up, normal.normalized);
            Ls(p, pos + q * Vector3.up * d * 0.15f, new Vector3(d, d * 0.6f, d), gem).transform.localRotation = q;
            Lc(p, pos, new Vector3(d * 1.15f, d * 0.08f, d * 1.15f), setting).transform.localRotation = q;
            for (int k = 0; k < 4; k++)
            {
                var off = q * (Quaternion.Euler(0, k * 90 + 45, 0) * Vector3.right * d * 0.5f);
                Ls(p, pos + off + q * Vector3.up * d * 0.25f, d * 0.18f, setting);
            }
        }

        /// <summary>A feather: shaft + vane + barbs on both sides (dir = along the shaft, side = flat direction).</summary>
        public static void Feather(Transform p, Vector3 basePos, Vector3 dir, Vector3 side, float len, Material vane, Material shaft, int barbs = 5)
        {
            dir = dir.normalized; side = side.normalized;
            var tip = basePos + dir * len;
            LRod(p, basePos, tip, len * 0.05f, shaft);
            var n = Vector3.Cross(dir, side).normalized;
            var v = B(p, (basePos + tip) / 2 + dir * len * 0.05f, new Vector3(len * 0.3f, len * 0.85f, len * 0.025f), vane);
            v.transform.localRotation = Quaternion.LookRotation(n, dir);
            for (int k = 0; k < barbs; k++)
            {
                var c = Vector3.Lerp(basePos, tip, 0.25f + k * 0.65f / barbs);
                foreach (var sd in new[] { -1f, 1f })
                    Orient(B(p, c + side * sd * len * 0.17f, new Vector3(len * 0.03f, len * 0.16f, len * 0.02f), vane), side * sd + dir * 0.6f);
            }
        }

        /// <summary>Overlapping scales / sequins on a surface given as surf(u,v), u,v ∈ [0,1].</summary>
        public static void Scales(Transform p, Func<float, float, Vector3> surf, Func<float, float, Vector3> normal, int rows, int cols, float d, Material m)
        {
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    float u = (i + (j % 2) * 0.5f) / cols, v = rows == 1 ? 0.5f : j / (float)(rows - 1);
                    var go = Ls(p, surf(u, v), new Vector3(d, d * 0.35f, d), m);
                    Orient(go, normal(u, v));
                }
        }

        /// <summary>Scales over a torso band (theta0..theta1, phi0..phi1).</summary>
        public static void TorsoScales(Transform p, float theta0, float theta1, float phi0, float phi1, int rows, int cols, float d, Material m, float grow = 1.05f)
            => Scales(p, (u, v) => OnTorso(Mathf.Lerp(theta0, theta1, v), Mathf.Lerp(phi0, phi1, u), grow), (u, v) => EllN(TorsoR, Mathf.Lerp(theta0, theta1, v), Mathf.Lerp(phi0, phi1, u)), rows, cols, d, m);

        /// <summary>Flat patch lying on the torso surface (pocket, badge, plate) at (theta, phi).</summary>
        public static GameObject TorsoPatch(Transform p, float theta, float phi, Vector3 size, Material m, float grow = 1.04f)
        {
            var go = B(p, OnTorso(theta, phi, grow), size, m);
            var n = EllN(TorsoR, theta, phi);
            go.transform.localRotation = Quaternion.LookRotation(n, Vector3.up);
            return go;
        }
    }

    /// <summary>
    /// Merges the small static parts of a built model: leaf renderers with nothing else on them are combined
    /// per (parent, material) into one mesh. Animated nodes, effects and anything with scripts stay as they are.
    /// </summary>
    public static class Baker
    {
        static readonly Dictionary<Mesh, Mesh> flipped = new Dictionary<Mesh, Mesh>();

        public static void Bake(Transform root, ICollection<GameObject> keep, List<Mesh> made)
        {
            var groups = new Dictionary<(Transform, Material), List<MeshFilter>>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(false))
            {
                var go = mf.gameObject;
                if (go.transform.childCount > 0 || go.transform == root || keep.Contains(go)) continue;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled || mr.sharedMaterials.Length != 1 || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                if (go.GetComponents<Component>().Length != 3) continue;   // Transform + MeshFilter + MeshRenderer only
                var key = (go.transform.parent, mr.sharedMaterial);
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<MeshFilter>();
                list.Add(mf);
            }
            foreach (var kv in groups)
            {
                var list = kv.Value;
                if (list.Count < 2) continue;
                var parent = kv.Key.Item1;
                var toLocal = parent.worldToLocalMatrix;
                var ci = new List<CombineInstance>(list.Count);
                long verts = 0;
                foreach (var mf in list)
                {
                    var mtx = toLocal * mf.transform.localToWorldMatrix;
                    var mesh = mf.sharedMesh;
                    if (mtx.determinant < 0) mesh = Flipped(mesh);
                    ci.Add(new CombineInstance { mesh = mesh, transform = mtx });
                    verts += mesh.vertexCount;
                }
                var m = new Mesh { name = "Baked" };
                if (verts > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.CombineMeshes(ci.ToArray(), true, true);
                made.Add(m);
                var go = new GameObject("Baked") { layer = list[0].gameObject.layer };
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key.Item2;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (var mf in list)
                {
                    mf.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(mf.gameObject);
                }
            }
        }

        /// <summary>Mirrored parts (negative scale) need reversed triangles once baked.</summary>
        static Mesh Flipped(Mesh src)
        {
            if (flipped.TryGetValue(src, out var f) && f != null) return f;
            f = UnityEngine.Object.Instantiate(src);
            f.name = src.name + "_flip";
            for (int s = 0; s < f.subMeshCount; s++)
            {
                var t = f.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3) { var x = t[i]; t[i] = t[i + 1]; t[i + 1] = x; }
                f.SetTriangles(t, s);
            }
            flipped[src] = f;
            return f;
        }
    }
}
