using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Wardrobe stage far below the world (y = −500, layer Preview): a mannequin chubrik with its own light and
    /// camera rendering into a texture. Also renders item thumbnails (cached).
    /// </summary>
    public class Preview : MonoBehaviour
    {
        public static Preview I;
        public RenderTexture RT;
        public ChubrikModel Model;
        public ChubrikAnim Anim;
        Camera cam;
        Pet pet;
        Aura aura;
        BlobShadow shadow;
        public float Yaw = 20f;
        public bool Walk;
        Transform stage;
        static readonly Vector3 Origin = new Vector3(0, -500, 0);
        readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();
        RenderTexture thumbRT;
        public string View = "body";

        public static void Create()
        {
            var go = new GameObject("Preview");
            I = go.AddComponent<Preview>();
            I.Build();
        }

        void Build()
        {
            stage = new GameObject("Stage") { layer = Layers.Preview }.transform;
            stage.position = Origin;
            Parts.Layer = Layers.Preview;
            var floor = Parts.Cy(stage, new Vector3(0, -0.05f, 0), new Vector3(3.2f, 0.05f, 3.2f), Parts.M("BFE3FF", 0.3f));
            floor.AddComponent<MeshCollider>().sharedMesh = Prim.MeshOf(PrimitiveType.Cylinder);
            Parts.Ring(stage, new Vector3(0, 0.0f, 0), 1.6f, 0.05f, Parts.M("FFFFFF", 0.5f));
            RT = new RenderTexture(560, 700, 24) { name = "PreviewRT", antiAliasing = Application.isMobilePlatform ? 1 : 4 };
            thumbRT = new RenderTexture(192, 192, 24) { name = "ThumbRT", antiAliasing = 2 };
            var cg = new GameObject("PreviewCam") { layer = Layers.Preview };
            cam = cg.AddComponent<Camera>();
            cam.cullingMask = (1 << Layers.Preview) | 1;      // + default layer: particle effects
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.68f, 0.98f, 1);
            cam.fieldOfView = 30; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30;
            cam.targetTexture = RT;
            cam.enabled = false;
            var lg = new GameObject("PreviewLight");
            lg.transform.rotation = Quaternion.Euler(35, 150, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Directional; l.intensity = 1.1f; l.color = new Color(1, 0.97f, 0.92f);
            l.cullingMask = 1 << Layers.Preview;
            l.shadows = LightShadows.None;
            Model = ChubrikModel.Create("Mannequin", stage, Save.CurrentLook(), Layers.Preview);
            Anim = Model.gameObject.AddComponent<ChubrikAnim>();
            Anim.model = Model; Anim.Preview = true;
            Anim.OnStep = left =>
            {
                int k = Effects.StepKind(Model.look);
                if (k >= 0) Effects.Step(k, (left ? Model.FootL : Model.FootR).position);
            };
            pet = new GameObject("PreviewPet") { layer = Layers.Preview }.AddComponent<Pet>();
            pet.owner = Model.transform;
            aura = Model.gameObject.AddComponent<Aura>();
            shadow = Model.gameObject.AddComponent<BlobShadow>();
            shadow.target = Model.transform;
            shadow.mask = 1 << Layers.Preview;
            gameObject.SetActive(false);
        }

        public void Show(bool on)
        {
            gameObject.SetActive(on);
            cam.enabled = on;
            stage.gameObject.SetActive(on);
            if (pet) pet.gameObject.SetActive(on);
        }

        public void SetLook(Look l)
        {
            Model.Build(l);
            Anim.model = Model;
            Anim.walkStyle = Mathf.Max(0, l.Idx("walk"));
            Anim.emote = Mathf.Max(0, l.Idx("emote"));
            pet.Set(l.Of("pet"), Layers.Preview);
            pet.scale = Mathf.Clamp(Model.SizeScale, 0.7f, 1.4f);
            aura.model = Model;
            aura.Set(Aura.KindOf(l));
            shadow.Set(Mathf.Max(0, l.Idx("shadow")));
            shadow.size = Model.SizeScale;
            foreach (var t in pet.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Preview;
        }

        void LateUpdate()
        {
            if (Model == null) return;
            Model.transform.localRotation = Quaternion.Euler(0, Yaw, 0);
            Anim.speed01 = Walk ? 0.8f : 0;
            Frame(cam, "body", Model.SizeScale, false);
        }

        void Frame(Camera c, string view, float s, bool thumb)
        {
            Vector3 target; float dist;
            switch (view)
            {
                case "head": target = new Vector3(0, 1.2f * s, 0); dist = 2.4f * s; break;
                case "feet": target = new Vector3(0, 0.4f * s, 0); dist = 2.8f * s; break;
                case "back": target = new Vector3(0, 0.85f * s, 0); dist = 3.6f * s; break;
                default: target = new Vector3(0, 0.8f * s, 0); dist = 4.3f * s; break;
            }
            if (!thumb) dist *= 1.35f;
            var dir = Quaternion.Euler(10, 0, 0) * Vector3.forward;
            c.transform.position = Origin + target + dir * dist;
            c.transform.rotation = Quaternion.LookRotation(-dir);
        }

        /// <summary>Thumbnails of items (the current look wearing each one), cached; the mannequin is restored after.</summary>
        public void Thumbs(IList<Item> items, System.Action<Item, Texture2D> done)
        {
            var todo = new List<Item>();
            foreach (var it in items)
            {
                if (thumbs.TryGetValue(it.id, out var t) && t) done(it, t);
                else todo.Add(it);
            }
            if (todo.Count == 0) return;
            bool wasActive = gameObject.activeSelf;
            Show(true);
            var keep = Model.look.Copy();
            var basis = Save.CurrentLook();
            cam.targetTexture = thumbRT;
            foreach (var it in todo)
            {
                if (it.dept == "pet")
                {
                    // the pet alone, close up
                    Model.gameObject.SetActive(false);
                    pet.gameObject.SetActive(false);
                    Parts.Layer = Layers.Preview;
                    var holder = Parts.Node("PetThumb", stage, new Vector3(0, 0.9f, 0), new Vector3(0, 200, 0));
                    Pet.Build(it.Premium ? 100 + it.set : Catalog.Index(it.id), holder);
                    foreach (var tr in holder.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = Layers.Preview;
                    cam.transform.position = Origin + new Vector3(0, 1.0f, 1.25f);
                    cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, -0.1f, -1.25f));
                    var tx = Grab();
                    holder.gameObject.SetActive(false);
                    Destroy(holder.gameObject);
                    Model.gameObject.SetActive(true);
                    pet.gameObject.SetActive(true);
                    thumbs[it.id] = tx;
                    done(it, tx);
                    continue;
                }
                var l = basis.Copy();
                l[it.dept] = it.id;
                Model.Build(l);
                string view = it.D.view;
                Model.transform.localRotation = Quaternion.Euler(0, view == "back" ? 205 : 20, 0);
                Frame(cam, view == "fx" ? "body" : view, Model.SizeScale, true);
                var t = Grab();
                thumbs[it.id] = t;
                done(it, t);
            }
            cam.targetTexture = RT;
            SetLook(keep);
            if (!wasActive) Show(false);
        }

        Texture2D Grab()
        {
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = thumbRT;
            var t = new Texture2D(thumbRT.width, thumbRT.height, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, thumbRT.width, thumbRT.height), 0, 0);
            t.Apply(false, true);
            RenderTexture.active = prev;
            return t;
        }
    }
}
