using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Small building helpers shared by the chubrik, pets and props: primitives without colliders + cached materials.</summary>
    public static class Parts
    {
        public static int Layer = 0;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        public static Material M(string hex, float smooth = 0.35f, float metal = 0f) => Mats.Lit(Hex(hex), smooth, metal);

        /// <summary>Glowing material (emission = colour × k).</summary>
        public static Material G(string hex, float k = 1.6f)
        {
            string key = "g" + hex + k;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            var c = Hex(hex);
            m = Mats.Emissive(null, c, c * k);
            mats[key] = m;
            return m;
        }

        public static Material Glass(string hex, float a = 0.35f)
        {
            string key = "t" + hex + a;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            var c = Hex(hex); c.a = a;
            m = Mats.Transparent(c);
            m.SetFloat("_Smoothness", 0.9f);
            mats[key] = m;
            return m;
        }

        public static Material Add(string hex, float a = 0.8f)
        {
            string key = "a" + hex + a;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            var c = Hex(hex); c.a = a;
            m = Mats.Additive(c);
            mats[key] = m;
            return m;
        }

        public static Material TexM(string key, Texture2D t, float smooth = 0.3f, float metal = 0f)
        {
            key = "x" + key;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            m = Mats.Tex(t, Color.white, smooth, metal);
            mats[key] = m;
            return m;
        }

        public static Transform Node(string name, Transform parent, Vector3 pos, Vector3 euler = default)
        {
            var t = new GameObject(name) { layer = Layer }.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = euler;
            return t;
        }

        /// <summary>Parts made so far (every primitive / mesh call adds one): the wardrobe counts the parts of each item.</summary>
        public static int Made;

        static GameObject P(PrimitiveType type, Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler)
        {
            Made++;
            var go = Prim.Make(type, p, pos, sc, m, type.ToString(), Layer);
            go.transform.localEulerAngles = euler;
            return go;
        }

        public static GameObject S(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => P(PrimitiveType.Sphere, p, pos, sc, m, euler);
        public static GameObject S(Transform p, Vector3 pos, float d, Material m) => P(PrimitiveType.Sphere, p, pos, Vector3.one * d, m, default);
        public static GameObject B(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => P(PrimitiveType.Cube, p, pos, sc, m, euler);
        /// <summary>Unity cylinder: height = 2 × sc.y.</summary>
        public static GameObject Cy(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => P(PrimitiveType.Cylinder, p, pos, sc, m, euler);
        public static GameObject Ca(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => P(PrimitiveType.Capsule, p, pos, sc, m, euler);

        public static GameObject Msh(Mesh mesh, Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default)
        {
            Made++;
            var go = Prim.MeshGO(mesh, p, m, mesh.name);
            go.layer = Layer;
            go.transform.localPosition = pos;
            go.transform.localScale = sc;
            go.transform.localEulerAngles = euler;
            return go;
        }

        // low-poly versions for small details (rivets, stitches, fur, beads): same sizes as S / Cy / Rod
        public static GameObject Ls(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Ball(), p, pos, sc, m, euler);
        public static GameObject Ls(Transform p, Vector3 pos, float d, Material m) => Msh(Meshes.Ball(), p, pos, Vector3.one * d, m);
        public static GameObject Lc(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Tube(), p, pos, sc, m, euler);
        public static GameObject LRod(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Lc(p, (a + b) / 2, new Vector3(d, (b - a).magnitude / 2, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        /// <summary>Cone (base radius = sc.x/2, height = sc.y) standing on <paramref name="pos"/>.</summary>
        public static GameObject Cone(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 euler = default) => Msh(Meshes.Cone(), p, pos, sc, m, euler);

        /// <summary>Cone from <paramref name="a"/> (base) to <paramref name="b"/> (tip).</summary>
        public static GameObject Spike(Transform p, Vector3 a, Vector3 b, float baseD, Material m)
        {
            var go = Msh(Meshes.Cone(), p, a, new Vector3(baseD, (b - a).magnitude, baseD), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        /// <summary>Cylinder rod between two local points.</summary>
        public static GameObject Rod(Transform p, Vector3 a, Vector3 b, float d, Material m)
        {
            var go = Cy(p, (a + b) / 2, new Vector3(d, (b - a).magnitude / 2, d), m);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        public static GameObject Ring(Transform p, Vector3 pos, float R, float r, Material m, Vector3 euler = default, Vector3? scale = null)
            => Msh(Meshes.Torus(R, r), p, pos, scale ?? Vector3.one, m, euler);

        public static T Add<T>(GameObject go) where T : Component => go.AddComponent<T>();
    }

    // ---------------- tiny animation components ----------------

    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float speed = 90f;
        void Update() { transform.Rotate(axis * speed * Time.deltaTime, Space.Self); }
    }

    /// <summary>Cycles the colour of the renderer through the rainbow.</summary>
    public class Hue : MonoBehaviour
    {
        public float speed = 0.25f, sat = 0.75f, val = 1f, offset, glow = 0f;
        Material m;
        void Start() { var r = GetComponent<Renderer>(); if (r) m = r.material; }
        void Update()
        {
            if (m == null) return;
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * speed + offset, 1f), sat, val);
            m.color = c;
            if (glow > 0) m.SetColor("_EmissionColor", c * glow);
        }
    }

    /// <summary>Gentle pulsing scale (flames, slime, glowing orbs).</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.08f, speed = 6f;
        Vector3 baseScale; float phase;
        void Start() { baseScale = transform.localScale; phase = Random.value * 10; }
        void Update() { float k = 1 + Mathf.Sin(Time.time * speed + phase) * amount; transform.localScale = new Vector3(baseScale.x * (2 - k), baseScale.y * k, baseScale.z * (2 - k)); }
    }

    /// <summary>Bobs up and down.</summary>
    public class Bob : MonoBehaviour
    {
        public float amp = 0.1f, speed = 2f;
        Vector3 basePos; float phase;
        void Start() { basePos = transform.localPosition; phase = Random.value * 10; }
        void Update() { transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amp; }
    }

    /// <summary>Wings: flap faster while the chubrik jumps / glides (model.flap 0..1).</summary>
    public class Flap : MonoBehaviour
    {
        public ChubrikModel model;
        public float side = 1, idleAmp = 6, flyAmp = 38, baseYaw = 0;
        float t;
        void Update()
        {
            float f = model != null ? model.flap : 0;
            t += Time.deltaTime * Mathf.Lerp(2.2f, 13f, f);
            float a = Mathf.Sin(t) * Mathf.Lerp(idleAmp, flyAmp, f);
            transform.localRotation = Quaternion.Euler(0, side * (baseYaw + a), side * a * 0.4f);
        }
    }

    /// <summary>Tail / cape segment: swings with time and lags behind movement.</summary>
    public class Sway : MonoBehaviour
    {
        public ChubrikModel model;
        public float amp = 12, speed = 3, phase, lift = 0;
        public Vector3 axisA = Vector3.forward, axisB = Vector3.right;
        Quaternion base0;
        void Start() { base0 = transform.localRotation; }
        void Update()
        {
            float mv = model != null ? model.speed01 : 0;
            float a = Mathf.Sin(Time.time * (speed + mv * 4) + phase) * amp * (1 + mv);
            float b = mv * lift + Mathf.Sin(Time.time * speed * 0.7f + phase) * amp * 0.3f;
            transform.localRotation = base0 * Quaternion.AngleAxis(a, axisA) * Quaternion.AngleAxis(b, axisB);
        }
    }
}
