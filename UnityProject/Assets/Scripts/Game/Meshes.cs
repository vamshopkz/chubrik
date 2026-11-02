using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Extra procedural meshes (cone, torus, star, heart, flat polygon), cached by parameters.</summary>
    public static class Meshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Mesh C(string key, System.Func<Mesh> make)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = make(); m.name = key; cache[key] = m;
            return m;
        }

        /// <summary>Cone of radius 0.5 and height 1, base at y = 0 (like Unity's unit primitives).</summary>
        public static Mesh Cone(int seg = 14) => C("cone" + seg, () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                var d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 0.5f; var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 0.5f;
                var tip = Vector3.up;
                int s = v.Count;
                v.Add(d0); v.Add(tip); v.Add(d1);
                var n0 = (d0.normalized * 1f + Vector3.up * 0.5f).normalized; var n1 = (d1.normalized + Vector3.up * 0.5f).normalized;
                n.Add(n0); n.Add(((n0 + n1) * 0.5f).normalized); n.Add(n1);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
                s = v.Count;
                v.Add(Vector3.zero); v.Add(d1); v.Add(d0);
                n.Add(Vector3.down); n.Add(Vector3.down); n.Add(Vector3.down);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            m.SetUVs(0, new List<Vector2>(new Vector2[v.Count]));
            m.RecalculateBounds();
            return m;
        });

        /// <summary>Torus in the XZ plane: ring radius R, tube radius r.</summary>
        public static Mesh Torus(float R = 0.5f, float r = 0.1f, int seg = 24, int side = 10) => C($"torus{R}_{r}_{seg}", () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg;
                var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int j = 0; j <= side; j++)
                {
                    float b = j * Mathf.PI * 2 / side;
                    var dir = c * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    v.Add(c * R + dir * r); n.Add(dir); uv.Add(new Vector2((float)i / seg * 4, (float)j / side));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    t.Add(a); t.Add(a + 1); t.Add(b); t.Add(b); t.Add(a + 1); t.Add(b + 1);
                }
            var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        });

        /// <summary>Flat polygon in XY (any simple outline) extruded along Z by <paramref name="depth"/>.</summary>
        public static Mesh Fan(string key, Vector2[] poly, float depth)
        {
            return C("fan" + key + depth, () =>
            {
                var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
                int k = poly.Length;
                var tris = EarClip(poly);
                float sgn = 0;
                for (int i = 0; i < k; i++) { var p0 = poly[i]; var p1 = poly[(i + 1) % k]; sgn += p0.x * p1.y - p1.x * p0.y; }
                sgn = sgn >= 0 ? 1 : -1;
                for (int f = 0; f < 2; f++)
                {
                    float z = f == 0 ? depth / 2 : -depth / 2;
                    var nn = f == 0 ? Vector3.forward : Vector3.back;
                    int c = v.Count;
                    foreach (var p in poly) { v.Add(new Vector3(p.x, p.y, z)); n.Add(nn); }
                    for (int i = 0; i < tris.Count; i += 3)
                    {
                        int a = c + tris[i], b = c + tris[i + 1], d = c + tris[i + 2];
                        if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[d] - v[a]), nn) >= 0) { t.Add(a); t.Add(b); t.Add(d); }
                        else { t.Add(a); t.Add(d); t.Add(b); }
                    }
                }
                for (int i = 0; i < k; i++)
                {
                    var a = poly[i]; var b = poly[(i + 1) % k];
                    var e = (b - a).normalized; var nn = new Vector3(e.y, -e.x, 0) * sgn;
                    int s = v.Count;
                    v.Add(new Vector3(a.x, a.y, depth / 2)); v.Add(new Vector3(b.x, b.y, depth / 2));
                    v.Add(new Vector3(b.x, b.y, -depth / 2)); v.Add(new Vector3(a.x, a.y, -depth / 2));
                    n.Add(nn); n.Add(nn); n.Add(nn); n.Add(nn);
                    if (Vector3.Dot(Vector3.Cross(v[s + 1] - v[s], v[s + 2] - v[s]), nn) > 0) { t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3); }
                    else { t.Add(s); t.Add(s + 2); t.Add(s + 1); t.Add(s); t.Add(s + 3); t.Add(s + 2); }
                }
                var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
                m.SetUVs(0, new List<Vector2>(new Vector2[v.Count]));
                m.RecalculateBounds();
                return m;
            });
        }

        static List<int> EarClip(Vector2[] poly)
        {
            float area = 0;
            for (int i = 0; i < poly.Length; i++) { var a = poly[i]; var b = poly[(i + 1) % poly.Length]; area += a.x * b.y - b.x * a.y; }
            bool ccw = area > 0;
            var idx = new List<int>();
            for (int i = 0; i < poly.Length; i++) idx.Add(i);
            var res = new List<int>();
            int guard = 0;
            while (idx.Count > 3 && guard++ < 2000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int i0 = idx[(i + idx.Count - 1) % idx.Count], i1 = idx[i], i2 = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[i0], b = poly[i1], c = poly[i2];
                    float cr = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (ccw ? cr <= 0 : cr >= 0) continue;
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == i0 || j == i1 || j == i2) continue;
                        if (InTri(poly[j], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    res.Add(i0); res.Add(i1); res.Add(i2);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) { res.Add(idx[0]); res.Add(idx[1]); res.Add(idx[2]); }
            return res;
        }

        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        public static Mesh Star(int points = 5, float inner = 0.42f, float depth = 0.25f)
        {
            var p = new Vector2[points * 2];
            for (int i = 0; i < p.Length; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / points;
                float r = i % 2 == 0 ? 0.5f : 0.5f * inner;
                p[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return Fan("star" + points + inner, p, depth);
        }

        public static Mesh Heart(float depth = 0.3f)
        {
            var p = new Vector2[40];
            for (int i = 0; i < 40; i++)
            {
                float t = i * Mathf.PI * 2 / 40;
                float x = 16 * Mathf.Pow(Mathf.Sin(t), 3);
                float y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
                p[i] = new Vector2(x, y + 2) / 34f;
            }
            return Fan("heart", p, depth);
        }

        /// <summary>Wing membrane: points along the top edge and scalloped bottom (in XY, x outwards).</summary>
        public static Mesh Wing(string key, Vector2[] outline, float depth = 0.03f) => Fan("wing" + key, outline, depth);

        /// <summary>Lightning zig-zag polygon.</summary>
        public static Mesh Bolt(float depth = 0.08f) => Fan("bolt", new[]
        {
            new Vector2(0.05f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.05f, 0.08f), new Vector2(0.22f, 0.08f),
            new Vector2(-0.2f, -0.5f), new Vector2(-0.02f, -0.05f), new Vector2(-0.2f, -0.05f),
        }, depth);
    }
}
