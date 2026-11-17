using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// The chubrik: a round cartoon hero built from primitives. Build(look) rebuilds every worn item of the
    /// 31 departments. Rig: Body (squash) → Torso → Head/HeadC, arms, legs; feet at the root origin, +Z forward.
    /// </summary>
    public partial class ChubrikModel : MonoBehaviour
    {
        public Look look = Look.Default();
        public Transform Body, Torso, Head, HeadC, ArmL, ArmR, HandL, HandR, LegL, LegR, FootL, FootR, EyeL, EyeR, Weapon, Mouth;
        public readonly List<GameObject> Pupils = new List<GameObject>();
        public readonly List<GameObject> JetFlames = new List<GameObject>();
        public bool CanGlide, HasJet;
        public float SizeScale = 1f;       // secret modifiers (giant / mini)
        public float flap, speed01;         // set by the animator (wings / tails react)
        public string WeaponId = "";
        public Material Skin;
        /// <summary>Parts each department's item was built from (before baking): the wardrobe shows it, the autotest checks it.</summary>
        public readonly Dictionary<string, int> PartCount = new Dictionary<string, int>();
        /// <summary>Off for tests that count separate renderers.</summary>
        public static bool BakeParts = true;
        readonly List<Mesh> baked = new List<Mesh>();
        int layer;
        float faceZ = 0.38f, hatY = 0.39f, eyeY = 0.04f, eyeX = 0.14f;
        /// <summary>17.11: how the hat is fitted to this head (measured from the skull): its top, half width and the hat scale.</summary>
        public float SkullTop { get; private set; }
        public float SkullHalfW { get; private set; }
        public float HatScale { get; private set; } = 1f;
        public float HatScaleY { get; private set; } = 1f;
        public bool BoxHead { get; private set; }
        bool hairHidden, earsHidden, suitOn, chickenOn;

        public static ChubrikModel Create(string name, Transform parent, Look look, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<ChubrikModel>();
            m.layer = layer;
            m.Build(look);
            return m;
        }

        public void Build(Look l)
        {
            look = l.Copy();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
            Pupils.Clear(); JetFlames.Clear(); PartCount.Clear();
            foreach (var m in baked) if (m) Destroy(m);
            baked.Clear();
            CanGlide = HasJet = false; SizeScale = 1f; hairHidden = earsHidden = false;
            Parts.Layer = layer;

            int suit = look.Idx("suit");
            bool chicken = IsChicken(suit);
            var bodyCol = BodyColor(look.Idx("color"));
            int skinIdx = look.Idx("skin");
            Skin = skinIdx >= 0 ? Mats.Tex(Patterns.Skin(skinIdx, bodyCol), Color.white, skinIdx == 7 ? 0.75f : 0.35f) : Mats.Lit(bodyCol, 0.35f);
            if (skinIdx == 6) { Skin.SetFloat("_Smoothness", 0.85f); }
            if (chicken) Skin = suit == 200 ? Mats.Emissive(null, Hex("FFD24A"), Hex("FF9A00") * 0.35f, null, 0.9f) : M("FAFAF5", 0.25f);
            if (skinIdx == 200)
            {
                // Алмазный Ледяной Чубрик: icy facets that glow and shimmer
                var tex = Patterns.Skin(200, bodyCol);
                Skin = Mats.Emissive(tex, Color.white, Hex("6FD8FF") * 0.35f, tex, 0.9f);
                var sh = GetComponent<IceShimmer>() ?? gameObject.AddComponent<IceShimmer>();
                sh.skin = Skin;
            }

            Body = Node("Body", transform, Vector3.zero);
            Torso = Node("Torso", Body, new Vector3(0, 0.36f, 0));
            S(Torso, new Vector3(0, 0.22f, 0), new Vector3(0.66f, 0.6f, 0.58f), Skin);
            Head = Node("Head", Torso, new Vector3(0, 0.5f, 0));
            HeadC = Node("HeadC", Head, new Vector3(0, 0.3f, 0));
            ArmL = Node("ArmL", Torso, new Vector3(-0.3f, 0.36f, 0), new Vector3(0, 0, -18));
            ArmR = Node("ArmR", Torso, new Vector3(0.3f, 0.36f, 0), new Vector3(0, 0, 18));
            foreach (var a in new[] { ArmL, ArmR })
                Ca(a, new Vector3(0, -0.14f, 0), new Vector3(0.15f, 0.14f, 0.15f), Skin);
            HandL = Node("HandL", ArmL, new Vector3(0, -0.3f, 0));
            HandR = Node("HandR", ArmR, new Vector3(0, -0.3f, 0));
            LegL = Node("LegL", Body, new Vector3(-0.14f, 0.36f, 0));
            LegR = Node("LegR", Body, new Vector3(0.14f, 0.36f, 0));
            foreach (var g in new[] { LegL, LegR })
                Ca(g, new Vector3(0, -0.14f, 0), new Vector3(0.17f, 0.13f, 0.17f), Skin);
            FootL = Node("FootL", LegL, new Vector3(0, -0.3f, 0.04f));
            FootR = Node("FootR", LegR, new Vector3(0, -0.3f, 0.04f));

            if (suit < 0) WolfBody();
            int made = Parts.Made;
            HeadShape(chicken ? 2 : look.Idx("head"));
            PartCount["head"] = Parts.Made - made;
            FitHat();
            suitOn = suit >= 0; chickenOn = chicken;
            Do("hat", Hat);
            SeatHatOnBox();
            Do("hair", Hair);
            Do("ears", Ears);
            Do("eyes", Glasses);
            Do("mask", Mask);
            Do("neck", Neck);
            Do("top", Top);
            Do("wings", Wings);
            Do("hands", Hands, true);
            Do("wrist", Wrist);
            Do("back", Back);
            Do("tail", Tail);
            Do("pants", Pants);
            Do("belt", Belt);
            Do("shoes", Shoes, true);
            Do("weapon", WeaponBuild);
            Do("suit", Suit);
            Secret(look.Of("secret"));
            if (BakeParts)
            {
                var keep = new HashSet<GameObject>(Pupils);
                foreach (var f in JetFlames) if (f) keep.Add(f);
                Baker.Bake(transform, keep, baked);
            }
            if (layer != 0) foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// 17.11: hats sit right on top of every head shape. The biggest part of the head is the skull; its top in the
        /// head's space is where a hat's base goes (a square head ends flat at its top, the alien's is tall, the
        /// round one is low) and the hat grows or shrinks with the skull's width.
        /// </summary>
        void FitHat()
        {
            Renderer best = null; float bestV = 0; Bounds bb = default; int verts = 0;
            var inv = HeadC.worldToLocalMatrix;
            foreach (var r in HeadC.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var b = Xf(inv * r.transform.localToWorldMatrix, mf.sharedMesh.bounds);
                float v = b.size.x * b.size.y * b.size.z;
                if (v > bestV) { bestV = v; best = r; bb = b; verts = mf.sharedMesh.vertexCount; }
            }
            HatScale = HatScaleY = 1; BoxHead = false;
            if (best == null) { SkullTop = hatY; SkullHalfW = 0.41f; return; }
            BoxHead = verts <= 24;                                     // a cube: the square and the robot heads
            SkullTop = bb.max.y;
            // a box has corners: the hat has to reach round them, but it stays as tall (it sits on the flat top)
            SkullHalfW = BoxHead ? Mathf.Sqrt(bb.extents.x * bb.extents.x + bb.extents.z * bb.extents.z) * 0.98f : Mathf.Max(bb.extents.x, bb.extents.z * 1.05f);
            hatY = SkullTop + 0.01f;               // the wolf head: top 0.38 → 0.39 as the hats were made for
            HatScale = Mathf.Clamp(SkullHalfW / 0.41f, 0.82f, 1.32f);
            HatScaleY = HatScale;
            if (BoxHead) { HatScale = Mathf.Clamp(Mathf.Max(bb.extents.x, bb.extents.z) / 0.41f * 1.04f, 0.82f, 1.2f); HatScaleY = HatScale; }
        }

        /// <summary>A square head is flat on top: the hat's lowest point goes right onto it (not sunk into the box).</summary>
        void SeatHatOnBox()
        {
            if (!BoxHead || HeadC == null) return;
            var h = HeadC.Find("Hat");
            if (h == null) return;
            float min = float.MaxValue;
            var inv = HeadC.worldToLocalMatrix;
            foreach (var mf in h.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = Xf(inv * mf.transform.localToWorldMatrix, mf.sharedMesh.bounds);
                // only what is above the face counts (ear flaps, straps and ribbons may hang down the sides)
                if (Mathf.Abs(b.center.x) < SkullHalfW * 0.6f && b.max.y > SkullTop - 0.15f) min = Mathf.Min(min, b.min.y);
            }
            if (min == float.MaxValue) return;
            h.localPosition += Vector3.up * (SkullTop - 0.03f - min);
        }

        static Bounds Xf(Matrix4x4 m, Bounds b)
        {
            var c = b.center; var e = b.extents;
            var r = new Bounds(m.MultiplyPoint3x4(c), Vector3.zero);
            for (int k = 0; k < 8; k++)
                r.Encapsulate(m.MultiplyPoint3x4(c + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z)));
            return r;
        }

        /// <summary>15.11 the hero is a wolf: a light belly and a bushy tail (unless a tail or a shirt is worn).</summary>
        void WolfBody()
        {
            if (look.Idx("head") != 0) return;
            var cream = M("F2ECE2", 0.3f);
            if (string.IsNullOrEmpty(look.Of("top"))) S(Torso, new Vector3(0, 0.18f, 0.19f), new Vector3(0.42f, 0.42f, 0.2f), cream);
            if (!string.IsNullOrEmpty(look.Of("tail"))) return;
            var t = Node("WolfTail", Torso, new Vector3(0, 0.08f, -0.27f), new Vector3(-35, 0, 0));
            float z = 0;
            for (int k = 0; k < 5; k++)
            {
                float w = 0.13f + Mathf.Sin((k + 0.5f) / 5f * Mathf.PI) * 0.09f;
                S(t, new Vector3(0, 0, -z), new Vector3(w, w, w * 1.35f), k == 4 ? cream : Skin);
                z += w * 0.62f;
            }
            Cone(t, new Vector3(0, 0, -z - 0.02f), new Vector3(0.12f, 0.12f, 0.12f), cream, new Vector3(-90, 0, 0));
        }

        delegate void Builder(int idx, int set);

        /// <summary>Calls the builder of a department; "always" builders also run with nothing worn (bare hands / feet).</summary>
        void Do(string dept, Builder b, bool always = false)
        {
            var id = look.Of(dept);
            if (string.IsNullOrEmpty(id) && !always) return;
            if (suitOn && SuitHides.Contains(dept))
            {
                // a costume replaces the clothes; bare hands and feet stay under it
                if (always) b(-1, -1);
                return;
            }
            if (chickenOn && dept == "ears") return;
            int made = Parts.Made;
            b(Catalog.Index(id), Catalog.SetOf(id));
            if (!string.IsNullOrEmpty(id)) PartCount[dept] = Parts.Made - made;
        }

        public static Color BodyColor(int i)
        {
            string[] c = { "8E99A8", "5BB8FF", "F2403C", "2A2A33", "8BE03A", "A050FF", "FF8FC4", "F4F7FA" };
            return Hex(c[Mathf.Clamp(i, 0, 7)]);
        }

        Vector3 HP(float theta, float phi, float r = 1f, float rx = 0.4f, float ry = 0.39f, float rz = 0.38f)
        {
            float t = theta * Mathf.Deg2Rad, p = phi * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(t) * Mathf.Cos(p) * rx * r, Mathf.Cos(t) * ry * r, Mathf.Sin(t) * Mathf.Sin(p) * rz * r);
        }

        // ================= secret modifiers =================

        void Secret(string id)
        {
            int i = Catalog.Index(id);
            if (i < 0) return;
            var rs = GetComponentsInChildren<Renderer>(true);
            switch (i)
            {
                case 0: SizeScale = 1.8f; break;
                case 1: SizeScale = 0.55f; break;
                case 2: Body.localScale = new Vector3(0.8f, 1.5f, 0.8f); break;
                case 3: Body.localScale = new Vector3(1.1f, 1f, 0.22f); break;
                case 4: { var m = Mats.Glow(new Color(0.03f, 0.03f, 0.04f)); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 5: { var m = Glass("BFE6FF", 0.3f); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 6: { var m = M("E8EEF5", 0.98f, 1f); foreach (var r in rs) r.sharedMaterial = m; break; }
                case 7:
                {
                    SizeScale = 1.2f;
                    var m = Mats.Emissive(null, Hex("FFD24A"), Hex("FF9A00") * 0.9f, null, 0.9f);
                    m.SetFloat("_Metallic", 0.8f);
                    foreach (var r in rs) if (!r.sharedMaterial || r.sharedMaterial.color.grayscale > 0.15f) r.sharedMaterial = m;
                    var halo = Ring(HeadC, new Vector3(0, hatY + 0.2f, 0), 0.22f, 0.025f, G("FFE14D", 3f));
                    Add<Spin>(halo).speed = 60;
                    var t = Prim.Text(HeadC, "АРТЁМ 22 X", new Vector3(0, hatY + 0.4f, 0), 2.2f, Hex("FFD24A"));
                    t.gameObject.layer = layer;
                    t.gameObject.AddComponent<Billboard>();
                    break;
                }
            }
            transform.localScale = Vector3.one * SizeScale;
        }
    }

    /// <summary>Keeps a world label facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main;
            if (c) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position);
        }
    }
}
