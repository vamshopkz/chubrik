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
        bool hairHidden, earsHidden;

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

            var bodyCol = BodyColor(look.Idx("color"));
            int skinIdx = look.Idx("skin");
            Skin = skinIdx >= 0 ? Mats.Tex(Patterns.Skin(skinIdx, bodyCol), Color.white, skinIdx == 7 ? 0.75f : 0.35f) : Mats.Lit(bodyCol, 0.35f);
            if (skinIdx == 6) { Skin.SetFloat("_Smoothness", 0.85f); }
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

            int made = Parts.Made;
            HeadShape(look.Idx("head"));
            PartCount["head"] = Parts.Made - made;
            Do("hat", Hat);
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

        delegate void Builder(int idx, int set);

        /// <summary>Calls the builder of a department; "always" builders also run with nothing worn (bare hands / feet).</summary>
        void Do(string dept, Builder b, bool always = false)
        {
            var id = look.Of(dept);
            if (string.IsNullOrEmpty(id) && !always) return;
            int made = Parts.Made;
            b(Catalog.Index(id), Catalog.SetOf(id));
            if (!string.IsNullOrEmpty(id)) PartCount[dept] = Parts.Made - made;
        }

        public static Color BodyColor(int i)
        {
            string[] c = { "FFC93C", "5BB8FF", "F2403C", "2A2A33", "8BE03A", "A050FF", "FF8FC4", "F4F7FA" };
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
