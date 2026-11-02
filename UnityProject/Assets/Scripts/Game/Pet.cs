using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Pet companion: floats behind the chubrik's shoulder, bobs, looks where the owner goes.</summary>
    public class Pet : MonoBehaviour
    {
        public Transform owner;
        public float scale = 1f;
        Transform body;
        Vector3 vel;
        string built = null;
        float t;

        public void Set(string id, int layer)
        {
            if (built == id) return;
            built = id;
            if (body) Destroy(body.gameObject);
            if (string.IsNullOrEmpty(id)) return;
            Parts.Layer = layer;
            body = Node("PetBody", transform, Vector3.zero);
            int set = Catalog.SetOf(id);
            Build(set >= 0 ? 100 + set : Catalog.Index(id), body);
            foreach (var tr in body.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = layer;
            foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (owner) transform.position = owner.position + Vector3.up * 1.4f;
        }

        public static void Build(int kind, Transform b)
        {
            var white = M("FFFFFF", 0.4f);
            var black = M("141414", 0.9f);
            void Eyes(Transform p, float x, float y, float z, float s = 0.08f)
            {
                foreach (var sd in new[] { -1, 1 }) { S(p, new Vector3(sd * x, y, z), s, white); S(p, new Vector3(sd * x, y, z + s * 0.35f), s * 0.55f, black); }
            }
            switch (kind)
            {
                case 0: // robo ball
                {
                    var metal = M("C9D1DB", 0.9f, 0.7f);
                    S(b, Vector3.zero, 0.42f, metal);
                    Ring(b, Vector3.zero, 0.21f, 0.02f, M("2E7DFF", 0.6f));
                    var eye = S(b, new Vector3(0, 0.03f, 0.18f), new Vector3(0.16f, 0.1f, 0.06f), G("35E0FF", 2.5f));
                    Add<Pulse>(eye);
                    Rod(b, new Vector3(0, 0.2f, 0), new Vector3(0, 0.34f, 0), 0.02f, metal);
                    S(b, new Vector3(0, 0.36f, 0), 0.06f, G("FF3B3B", 2f));
                    break;
                }
                case 1: // mini bear cub
                {
                    var fur = M("9C6B3F", 0.1f);
                    S(b, new Vector3(0, -0.08f, 0), new Vector3(0.34f, 0.3f, 0.3f), fur);
                    S(b, new Vector3(0, 0.16f, 0), 0.32f, fur);
                    foreach (var sd in new[] { -1, 1 }) S(b, new Vector3(sd * 0.12f, 0.3f, 0), 0.12f, fur);
                    S(b, new Vector3(0, 0.12f, 0.14f), new Vector3(0.14f, 0.1f, 0.08f), M("E8C9A0"));
                    S(b, new Vector3(0, 0.14f, 0.18f), 0.05f, black);
                    Eyes(b, 0.07f, 0.2f, 0.13f, 0.06f);
                    break;
                }
                case 2: // ghost kitten
                {
                    var gh = Glass("E8F0FF", 0.55f);
                    S(b, Vector3.zero, new Vector3(0.36f, 0.4f, 0.34f), gh);
                    foreach (var sd in new[] { -1, 1 }) Cone(b, new Vector3(sd * 0.1f, 0.15f, 0), new Vector3(0.12f, 0.14f, 0.06f), gh, new Vector3(0, 0, -sd * 20));
                    for (int k = -1; k <= 1; k++) S(b, new Vector3(k * 0.1f, -0.2f, 0), 0.12f, gh);
                    foreach (var sd in new[] { -1, 1 }) S(b, new Vector3(sd * 0.07f, 0.04f, 0.15f), new Vector3(0.06f, 0.08f, 0.03f), G("7FD8FF", 2f));
                    break;
                }
                case 3: // energy spark
                {
                    var core = S(b, Vector3.zero, 0.22f, G("FFF2A0", 3f)); Add<Pulse>(core).amount = 0.15f;
                    S(b, Vector3.zero, 0.4f, Add("FFC83A", 0.35f));
                    var ring = Ring(b, Vector3.zero, 0.22f, 0.012f, G("FFD24A", 2.5f), new Vector3(70, 0, 0)); Add<Spin>(ring).axis = Vector3.forward;
                    break;
                }
                case 4: // floating pixel
                {
                    var px = B(b, Vector3.zero, Vector3.one * 0.28f, G("FF3FA4", 1.4f), new Vector3(20, 30, 10));
                    Add<Hue>(px).glow = 1.5f;
                    Add<Spin>(px).axis = new Vector3(1, 1, 0);
                    break;
                }
                case 5: // little dragon
                {
                    var g = M("43A047", 0.5f);
                    S(b, new Vector3(0, -0.04f, -0.05f), new Vector3(0.26f, 0.24f, 0.32f), g);
                    S(b, new Vector3(0, 0.14f, 0.12f), 0.24f, g);
                    S(b, new Vector3(0, 0.1f, 0.24f), new Vector3(0.14f, 0.1f, 0.12f), g);
                    foreach (var sd in new[] { -1, 1 }) Cone(b, new Vector3(sd * 0.06f, 0.24f, 0.1f), new Vector3(0.05f, 0.1f, 0.05f), M("FFE14D"), new Vector3(-20, 0, -sd * 15));
                    Eyes(b, 0.07f, 0.18f, 0.22f, 0.06f);
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", b, new Vector3(sd * 0.1f, 0.05f, -0.05f));
                        var f = Add<Flap>(piv.gameObject); f.side = sd; f.idleAmp = 30;
                        var h = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
                        Msh(Meshes.Wing("petdragon", new[] { new Vector2(0, 0), new Vector2(0.18f, 0.14f), new Vector2(0.26f, 0.0f), new Vector2(0.14f, 0.04f), new Vector2(0.08f, -0.06f) }), h, Vector3.zero, Vector3.one, M("2E7D32", 0.4f));
                    }
                    Cone(b, new Vector3(0, -0.06f, -0.2f), new Vector3(0.1f, 0.22f, 0.1f), g, new Vector3(-100, 0, 0));
                    break;
                }
                case 6: // flying fish
                {
                    var f = M("FF8A3D", 0.7f, 0.2f);
                    S(b, Vector3.zero, new Vector3(0.2f, 0.24f, 0.4f), f);
                    Msh(Meshes.Fan("fishtail", new[] { new Vector2(0, 0), new Vector2(0.15f, 0.12f), new Vector2(0.15f, -0.12f) }, 0.03f), b, new Vector3(0, 0, -0.2f), Vector3.one, M("FFC83A", 0.6f), new Vector3(0, 90, 0));
                    foreach (var sd in new[] { -1, 1 }) Cone(b, new Vector3(sd * 0.1f, 0, 0.02f), new Vector3(0.12f, 0.14f, 0.02f), M("FFC83A", 0.6f), new Vector3(0, 0, -sd * 100));
                    Eyes(b, 0.08f, 0.04f, 0.12f, 0.06f);
                    break;
                }
                case 7: // blob
                {
                    var bl = S(b, Vector3.zero, new Vector3(0.38f, 0.32f, 0.36f), Glass("B57CFF", 0.75f));
                    Add<Pulse>(bl).amount = 0.12f;
                    Eyes(b, 0.07f, 0.04f, 0.15f, 0.07f);
                    break;
                }
                case 101: // snow owlet
                {
                    var w = M("F4F7FA", 0.1f);
                    S(b, Vector3.zero, new Vector3(0.32f, 0.36f, 0.3f), w);
                    foreach (var sd in new[] { -1, 1 }) Cone(b, new Vector3(sd * 0.09f, 0.14f, 0), new Vector3(0.07f, 0.1f, 0.05f), w, new Vector3(0, 0, -sd * 15));
                    foreach (var sd in new[] { -1, 1 }) { S(b, new Vector3(sd * 0.07f, 0.06f, 0.13f), 0.1f, M("FFC83A")); S(b, new Vector3(sd * 0.07f, 0.06f, 0.17f), 0.05f, black); }
                    Cone(b, new Vector3(0, 0.0f, 0.15f), new Vector3(0.04f, 0.06f, 0.04f), M("FF9A2E"), new Vector3(100, 0, 0));
                    for (int k = 0; k < 5; k++) S(b, new Vector3(Mathf.Sin(k * 2.1f) * 0.08f, -0.06f - k * 0.02f, 0.14f), 0.025f, M("9FB3CC"));
                    break;
                }
                case 105: // golden piggy bank
                {
                    var g = M("FFC83A", 0.95f, 0.9f);
                    S(b, Vector3.zero, new Vector3(0.38f, 0.32f, 0.42f), g);
                    Cy(b, new Vector3(0, 0, 0.21f), new Vector3(0.14f, 0.03f, 0.14f), g, new Vector3(90, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) { Cone(b, new Vector3(sd * 0.1f, 0.14f, 0.1f), new Vector3(0.08f, 0.1f, 0.05f), g); S(b, new Vector3(sd * 0.03f, 0.0f, 0.23f), 0.03f, black); }
                    B(b, new Vector3(0, 0.16f, -0.02f), new Vector3(0.12f, 0.02f, 0.02f), black);
                    Eyes(b, 0.08f, 0.08f, 0.16f, 0.06f);
                    foreach (var p in new[] { new Vector3(-0.1f, -0.15f, 0.1f), new Vector3(0.1f, -0.15f, 0.1f), new Vector3(-0.1f, -0.15f, -0.1f), new Vector3(0.1f, -0.15f, -0.1f) }) Cy(b, p, new Vector3(0.07f, 0.04f, 0.07f), g);
                    break;
                }
            }
        }

        void LateUpdate()
        {
            if (owner == null || body == null) return;
            t += Time.deltaTime;
            var target = owner.position + owner.right * 0.75f * scale - owner.forward * 0.45f * scale + Vector3.up * (1.45f * scale + Mathf.Sin(t * 2.4f) * 0.12f);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref vel, 0.25f);
            var look = owner.forward + vel * 0.3f; look.y = 0;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6);
            transform.localScale = Vector3.one * scale;
        }
    }
}
