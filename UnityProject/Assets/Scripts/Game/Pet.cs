using System.Collections.Generic;
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
            if (body) { PetMeshes.Free(body); Destroy(body.gameObject); }
            if (string.IsNullOrEmpty(id)) return;
            Parts.Layer = layer;
            body = Node("PetBody", transform, Vector3.zero);
            int set = Catalog.SetOf(id);
            Build(set >= 0 ? 100 + set : Catalog.Index(id), body);
            foreach (var tr in body.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = layer;
            foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (owner) transform.position = owner.position + Vector3.up * 1.4f;
        }

        void OnDestroy() { if (body) PetMeshes.Free(body); }

        public static void Build(int kind, Transform b)
        {
            var white = M("FFFFFF", 0.4f);
            var black = M("141414", 0.9f);
            var shine = G("FFFFFF", 1.2f);
            var pink = M("FF8FA8", 0.2f);
            Mesh mid = Meshes.Ball(14, 10), sm = Meshes.Ball(8, 5), tiny = Meshes.Ball(6, 4), cone8 = Meshes.Cone(8);

            // ---- local helpers ----
            GameObject Mb(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 e = default) => Msh(mid, p, pos, sc, m, e);
            GameObject Sm(Transform p, Vector3 pos, Vector3 sc, Material m, Vector3 e = default) => Msh(sm, p, pos, sc, m, e);
            GameObject T(Transform p, Vector3 pos, float d, Material m) => Msh(tiny, p, pos, Vector3.one * d, m);
            GameObject Sp(Transform p, Vector3 a, Vector3 c, float d, Material m)
            {
                var go = Msh(cone8, p, a, new Vector3(d, (c - a).magnitude, d), m);
                go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (c - a).normalized);
                return go;
            }
            Vector3 Up(Vector3 n) => Mathf.Abs(n.normalized.y) > 0.95f ? Vector3.forward : Vector3.up;
            // part lying on a surface: local z along the normal
            GameObject OnN(GameObject go, Vector3 n) { go.transform.localRotation = Quaternion.LookRotation(n, Up(n)); return go; }
            // thin box from a to c, flat side facing n (seams, stitches, mouths)
            GameObject Dash(Transform p, Vector3 a, Vector3 c, float th, Material m, Vector3 n)
            {
                var go = B(p, (a + c) / 2, new Vector3(th, (c - a).magnitude * 1.1f, th * 0.7f), m);
                go.transform.localRotation = Quaternion.LookRotation(n, c - a);
                return go;
            }
            void Bolt(Transform p, Vector3 pos, Vector3 n, float d, Material head, Material slot)
            {
                Lc(p, pos, new Vector3(d, d * 0.2f, d), head).transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
                OnN(B(p, pos + n * d * 0.2f, new Vector3(d * 0.75f, d * 0.15f, d * 0.12f), slot), n);
            }
            // point on the front (+z) of an ellipsoid at (x, y), and its normal
            Vector3 Fz(Vector3 c, Vector3 r, float x, float y, float off)
            {
                float u = (x - c.x) / r.x, v = (y - c.y) / r.y;
                return new Vector3(x, y, c.z + r.z * Mathf.Sqrt(Mathf.Max(0.02f, 1 - u * u - v * v)) + off);
            }
            Vector3 Nr(Vector3 c, Vector3 r, Vector3 p) => new Vector3((p.x - c.x) / (r.x * r.x), (p.y - c.y) / (r.y * r.y), (p.z - c.z) / (r.z * r.z)).normalized;
            // cartoon eye: white, iris, pupil, two sparkles (5 parts)
            void Eye(Transform p, Vector3 pos, Vector3 n, float s, Material iris)
            {
                var q = Quaternion.LookRotation(n, Vector3.up);
                Mb(p, pos, new Vector3(s, s, s * 0.8f), white).transform.localRotation = q;
                Mb(p, pos + n * s * 0.32f, new Vector3(s * 0.62f, s * 0.62f, s * 0.25f), iris).transform.localRotation = q;
                Mb(p, pos + n * s * 0.42f, new Vector3(s * 0.4f, s * 0.4f, s * 0.2f), black).transform.localRotation = q;
                T(p, pos + n * s * 0.5f + q * new Vector3(s * 0.12f, s * 0.14f, 0), s * 0.18f, shine);
                T(p, pos + n * s * 0.47f + q * new Vector3(-s * 0.1f, -s * 0.12f, 0), s * 0.09f, shine);
            }

            switch (kind)
            {
                case 0: // robo ball: panelled metal shell, bolts, LED visor, side hubs, antenna, thruster, back hatch
                {
                    var metal = M("C9D1DB", 0.9f, 0.7f);
                    var steel = M("8C96A3", 0.8f, 0.7f);
                    var blue = M("2E7DFF", 0.6f);
                    var dk = M("2A3140", 0.8f, 0.4f);
                    var cyan = G("35E0FF", 2.5f);
                    var rr = Vector3.one * 0.21f;
                    S(b, Vector3.zero, 0.42f, metal);
                    Ring(b, Vector3.zero, 0.21f, 0.02f, blue);
                    // panel seams: two latitude lines + four diagonal meridians, bolts where they cross
                    foreach (var th in new[] { 45f, 135f })
                        for (int k = 0; k < 12; k++)
                        {
                            var a = Dt.Ell(Vector3.zero, rr, th, k * 30, 1.02f); var c = Dt.Ell(Vector3.zero, rr, th, k * 30 + 30, 1.02f);
                            Dash(b, a, c, 0.008f, steel, a + c);
                        }
                    foreach (var ph in new[] { 45f, 135f, 225f, 315f })
                    {
                        foreach (var sg in new[] { new Vector2(45, 64), new Vector2(64, 83), new Vector2(97, 116), new Vector2(116, 135) })
                        {
                            var a = Dt.Ell(Vector3.zero, rr, sg.x, ph, 1.02f); var c = Dt.Ell(Vector3.zero, rr, sg.y, ph, 1.02f);
                            Dash(b, a, c, 0.008f, steel, a + c);
                        }
                        foreach (var th in new[] { 45f, 135f }) { var p = Dt.Ell(Vector3.zero, rr, th, ph, 1.03f); Bolt(b, p, p.normalized, 0.026f, steel, dk); }
                    }
                    // rivets on the band (not under the visor / hubs)
                    for (int k = 0; k < 16; k++)
                    {
                        float a = k * 22.5f; if ((a > 60 && a < 120) || a == 0 || a == 180) continue;
                        T(b, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)) * 0.229f, 0.016f, steel);
                    }
                    // LED visor: bezel, pulsing eye with pupil + scanlines, bezel bolts
                    Mb(b, new Vector3(0, 0.03f, 0.19f), new Vector3(0.2f, 0.14f, 0.06f), dk);
                    var eye = Mb(b, new Vector3(0, 0.03f, 0.2f), new Vector3(0.16f, 0.1f, 0.06f), cyan);
                    Add<Pulse>(eye);
                    var et = eye.transform;
                    Mb(et, new Vector3(0, 0, 0.4f), new Vector3(0.36f, 0.44f, 0.4f), M("0B3B5C", 0.9f));
                    T(et, new Vector3(0.08f, 0.1f, 0.58f), 0.12f, shine);
                    foreach (var y in new[] { -0.32f, 0.32f }) B(et, new Vector3(0, y, 0.39f), new Vector3(0.6f, 0.05f, 0.1f), G("BFF6FF", 2f));
                    for (int k = 0; k < 6; k++) { float a = (k * 60 + 30) * Mathf.Deg2Rad; T(b, new Vector3(Mathf.Cos(a) * 0.088f, 0.03f + Mathf.Sin(a) * 0.058f, 0.21f), 0.014f, steel); }
                    // speaker grill under the visor
                    for (int k = -2; k <= 2; k++)
                    {
                        var p = new Vector3(k * 0.022f, -0.075f, 0); p.z = Mathf.Sqrt(0.212f * 0.212f - p.x * p.x - p.y * p.y);
                        Dash(b, p + Vector3.up * 0.012f, p - Vector3.up * 0.012f, 0.008f, dk, p);
                    }
                    // side hubs
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var o = new Vector3(sd, 0, 0);
                        Lc(b, o * 0.22f, new Vector3(0.12f, 0.012f, 0.12f), steel, new Vector3(0, 0, 90));
                        Lc(b, o * 0.234f, new Vector3(0.08f, 0.003f, 0.08f), blue, new Vector3(0, 0, 90));
                        T(b, o * 0.238f, 0.026f, cyan);
                        for (int k = 0; k < 4; k++) { float a = (k * 90 + 45) * Mathf.Deg2Rad; T(b, o * 0.233f + new Vector3(0, Mathf.Cos(a), Mathf.Sin(a)) * 0.05f, 0.012f, dk); }
                    }
                    // antenna: collar, rod, coils, glowing tip + halo
                    Lc(b, new Vector3(0, 0.205f, 0), new Vector3(0.06f, 0.012f, 0.06f), steel);
                    LRod(b, new Vector3(0, 0.2f, 0), new Vector3(0, 0.34f, 0), 0.02f, metal);
                    for (int k = 0; k < 3; k++) Lc(b, new Vector3(0, 0.25f + k * 0.03f, 0), new Vector3(0.034f, 0.004f, 0.034f), steel);
                    Mb(b, new Vector3(0, 0.36f, 0), Vector3.one * 0.06f, G("FF3B3B", 2f));
                    Sm(b, new Vector3(0, 0.36f, 0), Vector3.one * 0.1f, Add("FF6B6B", 0.3f));
                    // thruster underneath
                    Lc(b, new Vector3(0, -0.21f, 0), new Vector3(0.08f, 0.015f, 0.08f), steel);
                    var fl = Mb(b, new Vector3(0, -0.235f, 0), new Vector3(0.055f, 0.04f, 0.055f), cyan); Add<Pulse>(fl).amount = 0.2f;
                    for (int k = 0; k < 4; k++) B(b, Dt.Ell(Vector3.zero, rr, 150, k * 90 + 45, 1f), new Vector3(0.05f, 0.035f, 0.008f), steel, new Vector3(0, -(k * 90 + 45), 0));
                    // back hatch with screws and vents
                    var hn = new Vector3(0, 0.25f, -1).normalized; var hp = hn * 0.208f; var hq = Quaternion.LookRotation(hn, Vector3.up);
                    OnN(B(b, hp, new Vector3(0.12f, 0.1f, 0.01f), steel), hn);
                    foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) T(b, hp + hq * new Vector3(sx * 0.05f, sy * 0.038f, 0.006f), 0.014f, metal);
                    for (int k = -1; k <= 1; k++) Dash(b, hp + hq * new Vector3(-0.035f, k * 0.02f, 0.007f), hp + hq * new Vector3(0.035f, k * 0.02f, 0.007f), 0.008f, dk, hn);
                    break;
                }
                case 1: // mini bear cub: plush toy with seams, muzzle, paw pads, ribbon bow, fur tufts
                {
                    var fur = M("9C6B3F", 0.1f);
                    var furD = M("85592F", 0.1f);
                    var light = M("E8C9A0", 0.15f);
                    var thread = M("5A3A20", 0.2f);
                    var red = M("E53935", 0.4f);
                    Vector3 bc = new Vector3(0, -0.08f, 0), br = new Vector3(0.17f, 0.15f, 0.15f), hc = new Vector3(0, 0.16f, 0), hr = Vector3.one * 0.16f;
                    S(b, bc, br * 2, fur);
                    S(b, hc, 0.32f, fur);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Mb(b, new Vector3(sd * 0.12f, 0.3f, 0), Vector3.one * 0.12f, fur);
                        Mb(b, new Vector3(sd * 0.12f, 0.3f, 0.05f), new Vector3(0.07f, 0.07f, 0.03f), light);
                    }
                    // plush seam over the head
                    Dt.Path(u => { float a = Mathf.Lerp(40, 190, u) * Mathf.Deg2Rad; return hc + new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)) * 0.163f; }, 12,
                        (p, d, i) => Dash(b, p - d * 0.008f, p + d * 0.008f, 0.006f, thread, p - hc));
                    // belly patch with stitching + button
                    Mb(b, new Vector3(0, -0.1f, 0.125f), new Vector3(0.2f, 0.18f, 0.06f), light);
                    Dt.Path(u => { float a = u * Mathf.PI * 2 * 11 / 12; return Fz(bc, br, Mathf.Cos(a) * 0.106f, -0.1f + Mathf.Sin(a) * 0.096f, 0.003f); }, 12,
                        (p, d, i) => Dash(b, p - d * 0.008f, p + d * 0.008f, 0.005f, thread, Nr(bc, br, p)));
                    T(b, new Vector3(0, -0.12f, 0.153f), 0.016f, thread);
                    // muzzle, nose, mouth, tongue
                    Vector3 mc = new Vector3(0, 0.12f, 0.14f), mr = new Vector3(0.07f, 0.05f, 0.04f);
                    Mb(b, mc, mr * 2, light);
                    Mb(b, new Vector3(0, 0.14f, 0.175f), new Vector3(0.06f, 0.04f, 0.035f), black);
                    T(b, new Vector3(-0.012f, 0.15f, 0.19f), 0.014f, shine);
                    Dash(b, Fz(mc, mr, 0, 0.12f, 0.002f), Fz(mc, mr, 0, 0.1f, 0.002f), 0.006f, black, Vector3.forward);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Dash(b, Fz(mc, mr, 0, 0.1f, 0.002f), Fz(mc, mr, sd * 0.02f, 0.093f, 0.002f), 0.006f, black, Vector3.forward);
                        Dash(b, Fz(mc, mr, sd * 0.02f, 0.093f, 0.002f), Fz(mc, mr, sd * 0.036f, 0.102f, 0.002f), 0.006f, black, Vector3.forward);
                    }
                    Mb(b, Fz(mc, mr, 0, 0.092f, -0.002f), new Vector3(0.024f, 0.014f, 0.01f), M("E57373", 0.3f));
                    // eyes, brows, cheeks
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = Fz(hc, hr, sd * 0.07f, 0.2f, -0.012f);
                        Eye(b, ep, Nr(hc, hr, ep), 0.062f, M("3B2A1A", 0.8f));
                        Dash(b, Fz(hc, hr, sd * 0.045f, 0.252f, 0.002f), Fz(hc, hr, sd * 0.095f, 0.244f, 0.002f), 0.012f, thread, Nr(hc, hr, Fz(hc, hr, sd * 0.07f, 0.248f, 0)));
                        var cp = Fz(hc, hr, sd * 0.105f, 0.135f, -0.002f);
                        OnN(Mb(b, cp, new Vector3(0.05f, 0.03f, 0.015f), pink), Nr(hc, hr, cp));
                    }
                    // cowlick on top
                    var top = hc + new Vector3(0, 0.155f, 0.035f);
                    Sp(b, top, top + new Vector3(0, 0.05f, 0.025f), 0.035f, fur);
                    Sp(b, top + new Vector3(-0.025f, -0.004f, -0.01f), top + new Vector3(-0.045f, 0.04f, 0), 0.03f, fur);
                    Sp(b, top + new Vector3(0.025f, -0.004f, -0.01f), top + new Vector3(0.045f, 0.035f, 0.01f), 0.028f, fur);
                    T(b, top + new Vector3(0.012f, 0, 0.02f), 0.03f, furD);
                    T(b, top + new Vector3(-0.014f, 0.004f, 0.012f), 0.026f, furD);
                    // ribbon with a bow
                    Msh(Meshes.Torus(0.12f, 0.016f, 20, 6), b, new Vector3(0, 0.03f, 0), Vector3.one, red);
                    var kn = new Vector3(0, 0.03f, 0.128f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Mb(b, kn + new Vector3(sd * 0.035f, 0.004f, -0.004f), new Vector3(0.055f, 0.038f, 0.022f), red, new Vector3(0, 0, sd * 18));
                        Dash(b, kn + new Vector3(sd * 0.008f, -0.01f, 0.004f), kn + new Vector3(sd * 0.03f, -0.05f, 0.022f), 0.016f, red, Vector3.forward);
                    }
                    Mb(b, kn + new Vector3(0, 0, 0.005f), new Vector3(0.026f, 0.026f, 0.022f), M("C62828", 0.4f));
                    // arms and legs with paw pads + toe beans
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Vector3 ac = new Vector3(sd * 0.15f, -0.06f, 0.06f), ar = new Vector3(0.045f, 0.065f, 0.045f);
                        Mb(b, ac, ar * 2, fur);
                        Mb(b, Fz(ac, ar, sd * 0.155f, -0.095f, -0.004f), new Vector3(0.05f, 0.045f, 0.016f), light);
                        for (int k = -1; k <= 1; k++) T(b, Fz(ac, ar, sd * 0.155f + k * 0.017f, -0.068f, -0.002f), 0.016f, light);
                        Vector3 lc = new Vector3(sd * 0.09f, -0.2f, 0.07f), lr = new Vector3(0.05f, 0.045f, 0.065f);
                        Mb(b, lc, lr * 2, fur);
                        Mb(b, new Vector3(sd * 0.09f, -0.205f, 0.133f), new Vector3(0.062f, 0.055f, 0.014f), light);
                        for (int k = -1; k <= 1; k++) T(b, Fz(lc, lr, sd * 0.09f + k * 0.021f, -0.17f, -0.002f), 0.018f, light);
                    }
                    // tail
                    Mb(b, new Vector3(0, -0.15f, -0.15f), Vector3.one * 0.07f, fur);
                    foreach (var sd in new[] { -1, 1 }) Sm(b, new Vector3(sd * 0.015f, -0.14f, -0.18f), Vector3.one * 0.03f, furD);
                    // fur tufts on the back half
                    var rng = new Rng(11);
                    for (int k = 0; k < 26; k++)
                    {
                        bool hd = k < 12;
                        Sm(b, Dt.Ell(hd ? hc : bc, hd ? hr : br, rng.F(hd ? 25 : 35, hd ? 115 : 145), rng.F(200, 340), 0.99f), Vector3.one * rng.F(0.028f, 0.04f), furD);
                    }
                    // side seams + plush tag sewn into the right one
                    foreach (var ph in new[] { -28f, 208f })
                        Dt.Path(u => Dt.Ell(bc, br, Mathf.Lerp(35, 145, u), ph, 1.015f), 8, (p, d, i) => Dash(b, p - d * 0.008f, p + d * 0.008f, 0.005f, thread, p - bc));
                    var tn = Dt.EllN(br, 110, -32); var tp = Dt.Ell(bc, br, 110, -32, 1.04f);
                    OnN(B(b, tp, new Vector3(0.035f, 0.045f, 0.004f), white), tn);
                    OnN(B(b, tp + tn * 0.003f, new Vector3(0.036f, 0.012f, 0.003f), red), tn);
                    break;
                }
                case 2: // ghost kitten: see-through body, swaying wisp skirt, glowing slit eyes, whiskers, bell collar, spirit motes
                {
                    var gh = Glass("E8F0FF", 0.55f);
                    var ghD = Glass("B8C8F0", 0.6f);
                    var pk = Glass("FFB3D9", 0.75f);
                    var ink = M("3A4A6A", 0.4f);
                    var eyeG = G("7FD8FF", 2f);
                    Vector3 c0 = Vector3.zero, r0 = new Vector3(0.18f, 0.2f, 0.17f);
                    S(b, Vector3.zero, r0 * 2, gh);
                    Mb(b, new Vector3(0, 0.02f, 0), new Vector3(0.2f, 0.22f, 0.18f), Add("BFE6FF", 0.25f));   // inner glow
                    // ears: outer, pink inner, tip tufts
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Cone(b, new Vector3(sd * 0.1f, 0.15f, 0), new Vector3(0.12f, 0.14f, 0.06f), gh, new Vector3(0, 0, -sd * 20));
                        Cone(b, new Vector3(sd * 0.1f, 0.155f, 0.02f), new Vector3(0.07f, 0.09f, 0.025f), pk, new Vector3(0, 0, -sd * 20));
                        var dir = new Vector3(sd * 0.34f, 0.94f, 0); var tip = new Vector3(sd * 0.1f, 0.15f, 0) + dir * 0.13f;
                        Sp(b, tip, tip + new Vector3(sd * 0.03f, 0.035f, 0), 0.015f, gh);
                        Sp(b, tip - dir * 0.02f, tip + new Vector3(sd * 0.04f, 0, 0.005f), 0.012f, gh);
                    }
                    // ghostly skirt: 8 curling wisps that sway together
                    var wisps = Node("Wisps", b, new Vector3(0, -0.13f, 0));
                    var ws = Add<Sway>(wisps.gameObject); ws.amp = 8; ws.speed = 2.2f;
                    for (int k = 0; k < 8; k++)
                        for (int j = 0; j < 3; j++)
                        {
                            float a = k * 45 * Mathf.Deg2Rad + j * 0.25f, rad = 0.1f + j * 0.012f;
                            var p = new Vector3(Mathf.Cos(a) * rad, -j * 0.05f, Mathf.Sin(a) * rad);
                            var sc = Vector3.one * (0.1f - j * 0.028f);
                            if (j == 0) Mb(wisps, p, sc, gh); else Sm(wisps, p, sc, gh);
                        }
                    // glowing eyes with cat slits, halo, sparkle
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = new Vector3(sd * 0.07f, 0.04f, 0.15f);
                        Mb(b, ep - Vector3.forward * 0.003f, new Vector3(0.09f, 0.11f, 0.02f), Add("7FD8FF", 0.3f));
                        Mb(b, ep, new Vector3(0.06f, 0.08f, 0.03f), eyeG);
                        B(b, ep + new Vector3(0, 0, 0.014f), new Vector3(0.012f, 0.05f, 0.008f), M("1A2A4A", 0.6f));
                        T(b, ep + new Vector3(sd * 0.012f, 0.02f, 0.016f), 0.016f, shine);
                    }
                    // nose, "w" mouth, whiskers + whisker dots, cheeks
                    Cone(b, new Vector3(0, 0.005f, 0.167f), new Vector3(0.028f, 0.022f, 0.014f), pk, new Vector3(180, 0, 0));
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Dash(b, Fz(c0, r0, 0, -0.02f, 0.002f), Fz(c0, r0, sd * 0.014f, -0.034f, 0.002f), 0.005f, ink, Vector3.forward);
                        Dash(b, Fz(c0, r0, sd * 0.014f, -0.034f, 0.002f), Fz(c0, r0, sd * 0.03f, -0.025f, 0.002f), 0.005f, ink, Vector3.forward);
                        for (int k = -1; k <= 1; k++)
                        {
                            var root = Fz(c0, r0, sd * 0.06f, -0.012f + k * 0.012f, 0);
                            LRod(b, root, root + new Vector3(sd * 0.12f, k * 0.025f + 0.01f, -0.03f), 0.005f, ink);
                            T(b, Fz(c0, r0, sd * 0.045f, -0.014f + k * 0.011f, 0.001f), 0.01f, ink);
                        }
                        var cp = Fz(c0, r0, sd * 0.11f, -0.01f, -0.001f);
                        OnN(Mb(b, cp, new Vector3(0.05f, 0.028f, 0.012f), pk), Nr(c0, r0, cp));
                    }
                    // front paws with toe lines
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var pp = new Vector3(sd * 0.06f, -0.12f, 0.13f);
                        Mb(b, pp, new Vector3(0.07f, 0.05f, 0.06f), gh);
                        for (int k = -1; k <= 1; k += 2) Dash(b, pp + new Vector3(k * 0.012f, 0, 0.028f), pp + new Vector3(k * 0.012f, -0.018f, 0.026f), 0.005f, pk, Vector3.forward);
                    }
                    // question-mark tail with a glowing tip
                    var tail = Node("Tail", b, new Vector3(0, -0.06f, -0.15f));
                    var ts = Add<Sway>(tail.gameObject); ts.amp = 14; ts.speed = 2.6f;
                    for (int k = 0; k < 8; k++)
                    {
                        float u = k / 7f;
                        Sm(tail, new Vector3(Mathf.Sin(u * 5f) * 0.03f, u * 0.24f, -Mathf.Sin(u * 2.6f) * 0.09f), Vector3.one * Mathf.Lerp(0.065f, 0.035f, u), gh);
                    }
                    Mb(tail, new Vector3(Mathf.Sin(5f) * 0.03f, 0.27f, -Mathf.Sin(2.6f) * 0.09f + 0.01f), Vector3.one * 0.04f, eyeG);
                    // orbiting spirit motes
                    var motes = Node("Motes", b, Vector3.zero); Add<Spin>(motes.gameObject).speed = 45;
                    for (int k = 0; k < 12; k++) { float a = k * 30 * Mathf.Deg2Rad; T(motes, new Vector3(Mathf.Cos(a) * 0.27f, Mathf.Sin(k * 1.7f) * 0.1f, Mathf.Sin(a) * 0.27f), 0.02f + (k % 3) * 0.006f, G("BFF6FF", 2f)); }
                    // pearl collar with a little bell
                    Dt.Studs(b, u => Dt.Ell(c0, r0, 107, u * 324, 1.03f), 10, 0.022f, M("FFFFFF", 0.9f));
                    var bell = Dt.Ell(c0, r0, 112, 90, 1.12f); var gold = M("FFC83A", 0.9f, 0.8f);
                    Mb(b, bell, Vector3.one * 0.038f, gold);
                    Dash(b, bell + new Vector3(-0.01f, -0.008f, 0.018f), bell + new Vector3(0.01f, -0.008f, 0.018f), 0.005f, ink, Vector3.forward);
                    T(b, bell + new Vector3(0, 0.02f, 0), 0.016f, gold);
                    // faint tabby stripes on the forehead
                    var fn = Nr(c0, r0, Fz(c0, r0, 0, 0.12f, 0));
                    Dash(b, Fz(c0, r0, 0, 0.14f, 0.002f), Fz(c0, r0, 0, 0.1f, 0.002f), 0.012f, ghD, fn);
                    foreach (var sd in new[] { -1, 1 }) Dash(b, Fz(c0, r0, sd * 0.03f, 0.13f, 0.002f), Fz(c0, r0, sd * 0.022f, 0.095f, 0.002f), 0.01f, ghD, fn);
                    // little wisps rising from the head
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var wn = Node("Wisp", b, new Vector3(sd * 0.04f, 0.2f, -0.04f)); var bo = Add<Bob>(wn.gameObject); bo.amp = 0.02f; bo.speed = 1.6f;
                        for (int k = 0; k < 3; k++) Sm(wn, new Vector3(sd * Mathf.Sin(k * 1.4f) * 0.025f, k * 0.035f, 0), Vector3.one * (0.04f - k * 0.01f), gh);
                    }
                    break;
                }
                case 3: // energy spark: pulsing core with a face, bead rings, orbiting sparks, lightning arcs, rays, flame crown, trail
                {
                    var core = S(b, Vector3.zero, 0.22f, G("FFF2A0", 3f)); Add<Pulse>(core).amount = 0.15f;
                    S(b, Vector3.zero, 0.4f, Add("FFC83A", 0.35f));
                    Mb(b, Vector3.zero, Vector3.one * 0.3f, Add("FFF7CC", 0.3f));
                    // face on the core (pulses with it)
                    var ct = core.transform;
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Mb(ct, new Vector3(sd * 0.17f, 0.08f, 0.44f), new Vector3(0.14f, 0.2f, 0.08f), black);
                        T(ct, new Vector3(sd * 0.17f + 0.03f, 0.13f, 0.485f), 0.06f, shine);
                        Mb(ct, new Vector3(sd * 0.3f, -0.04f, 0.38f), new Vector3(0.12f, 0.07f, 0.05f), M("FF8A65", 0.3f));
                    }
                    for (int k = -1; k <= 1; k++) T(ct, new Vector3(k * 0.08f, -0.15f + Mathf.Abs(k) * 0.03f, 0.465f), 0.06f, black);
                    // three spinning rings with beads
                    var bead = G("FFFFFF", 2f);
                    var ring = Ring(b, Vector3.zero, 0.22f, 0.012f, G("FFD24A", 2.5f), new Vector3(70, 0, 0)); Add<Spin>(ring).axis = Vector3.forward;
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; T(ring.transform, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.22f, 0.035f, bead); }
                    var r2 = Ring(b, Vector3.zero, 0.26f, 0.008f, G("FFB000", 2.2f), new Vector3(-35, 0, 40)); var s2 = Add<Spin>(r2); s2.axis = Vector3.up; s2.speed = -120;
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; T(r2.transform, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.26f, 0.028f, bead); }
                    var r3 = Ring(b, Vector3.zero, 0.18f, 0.008f, G("FFF2A0", 2.4f), new Vector3(20, 0, -60)); var s3 = Add<Spin>(r3); s3.axis = Vector3.up; s3.speed = 160;
                    for (int k = 0; k < 5; k++) { float a = k * 72 * Mathf.Deg2Rad; T(r3.transform, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.18f, 0.024f, bead); }
                    // orbiting sparks on three tilted shells
                    var sparkM = G("FFF2A0", 2.5f);
                    for (int s = 0; s < 3; s++)
                    {
                        var sh = Node("Orbit", b, Vector3.zero, new Vector3(s * 55, s * 40, s * 25));
                        Add<Spin>(sh.gameObject).speed = 70 + s * 35;
                        for (int k = 0; k < 10; k++)
                        {
                            float a = (k * 36 + s * 12) * Mathf.Deg2Rad, rad = 0.29f + 0.03f * Mathf.Sin(k * 2.3f);
                            T(sh, new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(k * 1.9f + s) * 0.04f, Mathf.Sin(a) * rad), 0.018f + 0.008f * ((k + s) % 3), sparkM);
                        }
                    }
                    // zig-zag lightning arcs
                    var arcs = Node("Arcs", b, Vector3.zero); Add<Spin>(arcs.gameObject).speed = -40; Add<Pulse>(arcs.gameObject).amount = 0.1f;
                    var boltM = G("FFFDE7", 2.6f);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 60 * Mathf.Deg2Rad;
                        var d = new Vector3(Mathf.Cos(a), k % 2 == 0 ? 0.35f : -0.35f, Mathf.Sin(a)).normalized;
                        var side = Vector3.Cross(d, Vector3.up).normalized;
                        Vector3 p0 = d * 0.14f, p1 = d * 0.19f + side * 0.025f, p2 = d * 0.24f - side * 0.02f, p3 = d * 0.3f + side * 0.01f;
                        LRod(arcs, p0, p1, 0.008f, boltM); LRod(arcs, p1, p2, 0.008f, boltM); LRod(arcs, p2, p3, 0.007f, boltM);
                    }
                    // soft rays (not over the face)
                    var rayM = Add("FFD24A", 0.5f);
                    for (int k = 0; k < 16; k++)
                    {
                        float y = 1 - (k + 0.5f) / 8f, rad = Mathf.Sqrt(Mathf.Max(0, 1 - y * y)), a = k * 2.39996f;
                        var d = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                        if (d.z > 0.55f) continue;
                        Sp(b, d * 0.1f, d * 0.25f, 0.03f, rayM);
                    }
                    // flame crown
                    var crown = Node("Crown", b, new Vector3(0, 0.1f, 0)); Add<Pulse>(crown.gameObject).amount = 0.12f;
                    Cone(crown, Vector3.zero, new Vector3(0.06f, 0.17f, 0.06f), G("FFF2A0", 2.6f));
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 72 * Mathf.Deg2Rad;
                        Cone(crown, new Vector3(Mathf.Cos(a) * 0.035f, 0, Mathf.Sin(a) * 0.035f), new Vector3(0.05f, 0.12f - (k % 2) * 0.03f, 0.05f),
                            k % 2 == 0 ? G("FF9A2E", 2.2f) : G("FFD24A", 2.4f), new Vector3(Mathf.Sin(a) * 25, 0, -Mathf.Cos(a) * 25));
                    }
                    // trail behind
                    for (int k = 0; k < 6; k++) Sm(b, new Vector3(0, -0.015f * k, -0.2f - 0.055f * k), Vector3.one * (0.09f - k * 0.012f), k % 2 == 0 ? Add("FFC83A", 0.5f) : Add("FFF2A0", 0.4f));
                    // twinkling 4-point stars
                    var tw = Node("Twinkle", b, Vector3.zero); var tsp = Add<Spin>(tw.gameObject); tsp.speed = 25; tsp.axis = new Vector3(0.2f, 1, 0);
                    var star = Meshes.Star(4, 0.35f, 0.2f);
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; Msh(star, tw, new Vector3(Mathf.Cos(a) * 0.36f, Mathf.Sin(k * 2.1f) * 0.12f, Mathf.Sin(a) * 0.36f), Vector3.one * 0.05f, bead, new Vector3(0, 90 - k * 60, 0)); }
                    break;
                }
                case 4: // floating pixel: rainbow cube covered in raised pixel-art tiles, corner caps, orbiting RGB sub-pixels
                {
                    var px = B(b, Vector3.zero, Vector3.one * 0.28f, G("FF3FA4", 1.4f), new Vector3(20, 30, 10));
                    Add<Hue>(px).glow = 1.5f;
                    Add<Spin>(px).axis = new Vector3(1, 1, 0);
                    var pt = px.transform;
                    var pal = new Dictionary<char, Material> { { 'k', M("141414", 0.6f) }, { 'w', G("FFFFFF", 1.2f) }, { 'r', G("FF3B3B", 1.6f) }, { 'y', G("FFE14D", 1.6f) }, { 'c', G("35E0FF", 1.6f) }, { 'g', G("8BE03A", 1.6f) } };
                    Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
                    // one 5×5 pixel picture per face (cube space: faces at ±0.5)
                    void Face(Vector3 n, Vector3 u, Vector3 v, params string[] rows)
                    {
                        for (int r = 0; r < 5; r++)
                            for (int c = 0; c < 5; c++)
                            {
                                char ch = rows[r][c]; if (ch == '.') continue;
                                B(pt, n * 0.52f + u * ((c - 2) * 0.18f) + v * ((2 - r) * 0.18f), Abs(u) * 0.16f + Abs(v) * 0.16f + Abs(n) * 0.07f, pal[ch]);
                            }
                    }
                    Face(Vector3.forward, Vector3.right, Vector3.up, ".....", ".k.k.", ".....", "k...k", ".kkk.");          // smiley
                    Face(Vector3.back, Vector3.left, Vector3.up, ".r.r.", "rrrrr", "rrrrr", ".rrr.", "..r..");             // heart
                    Face(Vector3.up, Vector3.right, Vector3.back, "..y..", ".yyy.", "yyyyy", ".yyy.", "..y..");            // gem
                    Face(Vector3.down, Vector3.right, Vector3.forward, ".ccc.", "c...c", "c.w.c", "c...c", ".ccc.");        // coin
                    Face(Vector3.right, Vector3.back, Vector3.up, "..g..", "...g.", "ggggg", "...g.", "..g..");            // arrow
                    Face(Vector3.left, Vector3.forward, Vector3.up, "...w.", "..w..", ".www.", "..w..", ".w...");          // bolt
                    foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
                        B(pt, new Vector3(sx, sy, sz) * 0.5f, Vector3.one * 0.16f, pal['w']);
                    // RGB sub-pixels with trails
                    var orb = Node("SubPixels", b, Vector3.zero); Add<Spin>(orb.gameObject).speed = 80;
                    var rgb = new[] { G("FF3B3B", 1.8f), G("8BE03A", 1.8f), G("2E7DFF", 1.8f) };
                    for (int k = 0; k < 3; k++)
                        for (int j = 0; j < 4; j++)
                        {
                            float a = k * 120 * Mathf.Deg2Rad + j * 0.22f;
                            B(orb, new Vector3(Mathf.Cos(a) * 0.3f, 0.04f * Mathf.Sin(k * 2.1f), Mathf.Sin(a) * 0.3f), Vector3.one * (0.06f - j * 0.012f), rgb[k], new Vector3(0, -a * Mathf.Rad2Deg, 0));
                        }
                    // glints: tiny pixels and plus-shaped sparkles
                    var glint = Node("Glints", b, Vector3.zero, new Vector3(30, 0, 20)); Add<Spin>(glint.gameObject).speed = -55;
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; B(glint, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.36f, Vector3.one * 0.025f, pal['w']); }
                    for (int k = 0; k < 6; k++)
                    {
                        float a = (k * 60 + 22) * Mathf.Deg2Rad; var p = new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(k * 1.3f) * 0.12f, Mathf.Sin(a) * 0.42f);
                        B(glint, p, new Vector3(0.05f, 0.012f, 0.012f), pal['y'], new Vector3(0, -k * 60 - 22, 0));
                        B(glint, p, new Vector3(0.012f, 0.05f, 0.012f), pal['y']);
                    }
                    // pixels dropping out underneath
                    var drop = Node("Drops", b, Vector3.zero); var db = Add<Bob>(drop.gameObject); db.amp = 0.025f; db.speed = 3;
                    for (int k = 0; k < 5; k++) B(drop, new Vector3(Mathf.Sin(k * 2.2f) * 0.05f, -0.24f - k * 0.05f, -0.03f * k), Vector3.one * (0.045f - k * 0.007f), k % 2 == 0 ? pal['c'] : pal['y']);
                    break;
                }
                case 5: // little dragon: scaled hide, belly plates, back spikes, ridged horns, boned wings, claws, swaying spade tail
                {
                    var g = M("43A047", 0.5f);
                    var dg = M("2E7D32", 0.4f);
                    var belly = M("C5E1A5", 0.4f);
                    var spk = M("FFB300", 0.5f);
                    var claw = M("FFF8E1", 0.6f);
                    Vector3 bc = new Vector3(0, -0.04f, -0.05f), br = new Vector3(0.13f, 0.12f, 0.16f), hc = new Vector3(0, 0.14f, 0.12f), hr = Vector3.one * 0.12f;
                    Vector3 sc = new Vector3(0, 0.1f, 0.24f), sr = new Vector3(0.07f, 0.05f, 0.06f);
                    S(b, bc, br * 2, g);
                    S(b, hc, 0.24f, g);
                    Mb(b, sc, sr * 2, g);
                    // belly plates
                    for (int k = 0; k < 5; k++)
                    {
                        float th = 78 + k * 16;
                        OnN(Sm(b, Dt.Ell(bc, br, th, 90, 0.99f), new Vector3(0.12f - k * 0.012f, 0.04f, 0.022f), belly), Dt.EllN(br, th, 90));
                    }
                    // spikes along the head and back
                    foreach (var th in new[] { 45f, 70f }) { var p = Dt.Ell(hc, hr, th, 270, 0.97f); Sp(b, p, p + Dt.EllN(hr, th, 270) * 0.05f, 0.04f, spk); }
                    for (int k = 0; k < 5; k++) { float th = 15 + k * 17; var p = Dt.Ell(bc, br, th, 270, 0.97f); Sp(b, p, p + Dt.EllN(br, th, 270) * (0.06f - k * 0.006f), 0.045f - k * 0.004f, spk); }
                    // scales over the back and sides
                    for (int j = 0; j < 4; j++)
                        for (int i = 0; i < 8; i++)
                        {
                            float th = 40 + j * 22, ph = 195 + (i + (j % 2) * 0.5f) * 19;
                            OnN(Sm(b, Dt.Ell(bc, br, th, ph, 1f), new Vector3(0.042f, 0.042f, 0.012f), dg), Dt.EllN(br, th, ph));
                        }
                    // horns with growth rings
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var hb = new Vector3(sd * 0.06f, 0.24f, 0.1f);
                        var hg = Cone(b, hb, new Vector3(0.05f, 0.1f, 0.05f), M("FFE14D"), new Vector3(-20, 0, -sd * 15));
                        var q = hg.transform.localRotation; var up = q * Vector3.up;
                        for (int k = 1; k <= 2; k++) { float d = 0.05f * (1 - 0.25f * k) + 0.006f; Lc(b, hb + up * (0.025f * k), new Vector3(d, 0.004f, d), M("E0B800", 0.5f)).transform.localRotation = q; }
                    }
                    // face: eyes, brow ridges, cheeks, nostrils with smoke puffs, smile with fangs
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = Fz(hc, hr, sd * 0.07f, 0.18f, -0.012f);
                        Eye(b, ep, Nr(hc, hr, ep), 0.065f, M("8D5A00", 0.8f));
                        Dash(b, Fz(hc, hr, sd * 0.04f, 0.235f, 0.003f), Fz(hc, hr, sd * 0.1f, 0.225f, 0.003f), 0.014f, dg, Nr(hc, hr, Fz(hc, hr, sd * 0.07f, 0.23f, 0)));
                        var cp = Fz(hc, hr, sd * 0.085f, 0.125f, -0.002f);
                        OnN(Mb(b, cp, new Vector3(0.045f, 0.026f, 0.012f), pink), Nr(hc, hr, cp));
                        T(b, Fz(sc, sr, sd * 0.025f, 0.115f, -0.002f), 0.014f, black);
                        var puff = Sm(b, new Vector3(sd * 0.03f, 0.14f + (sd > 0 ? 0.02f : 0), 0.32f), Vector3.one * (sd > 0 ? 0.022f : 0.03f), Add("E0E0E0", 0.35f));
                        var pb = Add<Bob>(puff); pb.amp = 0.015f; pb.speed = 3;
                        Sp(b, Fz(sc, sr, sd * 0.02f, 0.081f, -0.002f), Fz(sc, sr, sd * 0.02f, 0.081f, -0.002f) + new Vector3(0, -0.016f, 0.002f), 0.012f, claw);
                    }
                    Vector3 m0 = Fz(sc, sr, -0.045f, 0.088f, 0.002f), m1 = Fz(sc, sr, -0.015f, 0.08f, 0.002f), m2 = Fz(sc, sr, 0.015f, 0.08f, 0.002f), m3 = Fz(sc, sr, 0.045f, 0.088f, 0.002f);
                    Dash(b, m0, m1, 0.006f, black, Vector3.forward); Dash(b, m1, m2, 0.006f, black, Vector3.forward); Dash(b, m2, m3, 0.006f, black, Vector3.forward);
                    // wings: membrane + arm bone, three fingers with knobs, wrist claw
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", b, new Vector3(sd * 0.1f, 0.05f, -0.05f));
                        var f = Add<Flap>(piv.gameObject); f.side = sd; f.idleAmp = 30;
                        var h = Node("W", piv, Vector3.zero, new Vector3(0, sd < 0 ? 180 : 0, 0));
                        Msh(Meshes.Wing("petdragon", new[] { new Vector2(0, 0), new Vector2(0.18f, 0.14f), new Vector2(0.26f, 0.0f), new Vector2(0.14f, 0.04f), new Vector2(0.08f, -0.06f) }), h, Vector3.zero, Vector3.one, dg);
                        var wr = new Vector3(0.18f, 0.14f, 0);
                        LRod(h, Vector3.zero, wr, 0.04f, g);
                        foreach (var tip in new[] { new Vector3(0.26f, 0, 0), new Vector3(0.14f, 0.04f, 0), new Vector3(0.08f, -0.06f, 0) }) { LRod(h, wr, tip, 0.034f, g); T(h, tip, 0.03f, g); }
                        T(h, wr, 0.045f, g);
                        Sp(h, wr, wr + new Vector3(0.02f, 0.05f, 0), 0.025f, claw);
                    }
                    // clawed arms and feet
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ap = new Vector3(sd * 0.08f, -0.06f, 0.08f);
                        Mb(b, ap, new Vector3(0.055f, 0.08f, 0.055f), g);
                        for (int k = -1; k <= 1; k++) Sp(b, ap + new Vector3(k * 0.014f, -0.035f, 0.018f), ap + new Vector3(k * 0.018f, -0.05f, 0.035f), 0.012f, claw);
                        var lp = new Vector3(sd * 0.08f, -0.15f, 0);
                        Mb(b, lp, new Vector3(0.09f, 0.08f, 0.11f), g);
                        Mb(b, lp + new Vector3(0, -0.03f, 0.04f), new Vector3(0.08f, 0.04f, 0.1f), g);
                        for (int k = -1; k <= 1; k++) Sp(b, lp + new Vector3(k * 0.022f, -0.035f, 0.085f), lp + new Vector3(k * 0.026f, -0.04f, 0.11f), 0.016f, claw);
                    }
                    // tail: tapering segments, spikes, spade tip
                    var tail = Node("Tail", b, new Vector3(0, -0.07f, -0.19f));
                    var tsw = Add<Sway>(tail.gameObject); tsw.amp = 14; tsw.speed = 2.5f; tsw.axisA = Vector3.up;
                    Vector3 TailP(float u) => new Vector3(0, -0.03f * u + 0.09f * u * u, -0.22f * u);
                    for (int k = 0; k < 5; k++)
                    {
                        float d = 0.1f - k * 0.013f;
                        if (k < 3) Mb(tail, TailP(k / 5f), Vector3.one * d, g); else Sm(tail, TailP(k / 5f), Vector3.one * d, g);
                    }
                    for (int k = 0; k < 4; k++) { var p = TailP(0.1f + k * 0.2f) + Vector3.up * (0.04f - k * 0.006f); Sp(tail, p, p + new Vector3(0, 0.035f, -0.01f), 0.03f, spk); }
                    Cone(tail, TailP(0.92f), new Vector3(0.09f, 0.09f, 0.025f), spk, new Vector3(-80, 0, 0));
                    Cone(tail, TailP(0.92f), new Vector3(0.09f, 0.03f, 0.025f), spk, new Vector3(100, 0, 0));
                    break;
                }
                case 6: // flying fish: scaled body, rayed tail / dorsal / anal fins, flapping wing-fins, gills, lateral line, bubbles
                {
                    var f = M("FF8A3D", 0.7f, 0.2f);
                    var fin = M("FFC83A", 0.6f);
                    var ray = M("E65100", 0.5f);
                    var scl = M("FFB070", 0.7f, 0.2f);
                    Vector3 c0 = Vector3.zero, r0 = new Vector3(0.1f, 0.12f, 0.2f);
                    S(b, Vector3.zero, r0 * 2, f);
                    // forked tail with rays (wags side to side)
                    var tail = Node("Tail", b, new Vector3(0, 0, -0.19f), new Vector3(0, 90, 0));
                    var tsw = Add<Sway>(tail.gameObject); tsw.axisA = Vector3.up; tsw.amp = 18; tsw.speed = 4;
                    Msh(Meshes.Fan("petfishtail", new[] { new Vector2(0, 0.02f), new Vector2(0.12f, 0.13f), new Vector2(0.16f, 0.12f), new Vector2(0.11f, 0), new Vector2(0.16f, -0.12f), new Vector2(0.12f, -0.13f), new Vector2(0, -0.02f) }, 0.03f), tail, Vector3.zero, Vector3.one, fin);
                    foreach (var e in new[] { new Vector2(0.14f, 0.115f), new Vector2(0.13f, 0.06f), new Vector2(0.11f, 0f), new Vector2(0.13f, -0.06f), new Vector2(0.14f, -0.115f) })
                        LRod(tail, new Vector3(0.01f, 0, 0), new Vector3(e.x, e.y, 0), 0.04f, ray);
                    Mb(tail, Vector3.zero, new Vector3(0.07f, 0.08f, 0.06f), f);
                    // dorsal fin
                    var dor = Node("Dorsal", b, new Vector3(0, 0.1f, 0.06f), new Vector3(0, 90, 0));
                    Msh(Meshes.Fan("petfishdorsal", new[] { new Vector2(0, -0.02f), new Vector2(0.03f, 0.07f), new Vector2(0.09f, 0.09f), new Vector2(0.17f, 0.04f), new Vector2(0.2f, -0.03f) }, 0.02f), dor, Vector3.zero, Vector3.one, fin);
                    foreach (var e in new[] { new Vector2(0.035f, 0.065f), new Vector2(0.08f, 0.08f), new Vector2(0.125f, 0.06f), new Vector2(0.17f, 0.03f) })
                        LRod(dor, new Vector3(e.x - 0.02f, -0.01f, 0), new Vector3(e.x, e.y, 0), 0.028f, ray);
                    // big wing-fins (mirrored on the left), each with five rays
                    var wingPts = new[] { new Vector2(0, 0.02f), new Vector2(0.1f, 0.06f), new Vector2(0.22f, 0.05f), new Vector2(0.27f, 0), new Vector2(0.2f, -0.03f), new Vector2(0.1f, -0.03f), new Vector2(0, -0.02f) };
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Fin", b, new Vector3(sd * 0.085f, -0.02f, -0.01f));
                        var fl = Add<Flap>(piv.gameObject); fl.side = sd; fl.idleAmp = 22;
                        var h = Node("F", piv, Vector3.zero, new Vector3(70, 0, 0));
                        if (sd < 0) h.localScale = new Vector3(-1, 1, 1);
                        Msh(Meshes.Fan("petfishwing", wingPts, 0.02f), h, Vector3.zero, Vector3.one, fin);
                        foreach (var e in new[] { new Vector2(0.1f, 0.05f), new Vector2(0.18f, 0.045f), new Vector2(0.25f, 0.01f), new Vector2(0.2f, -0.02f), new Vector2(0.1f, -0.025f) })
                            LRod(h, new Vector3(0.005f, 0, 0), new Vector3(e.x, e.y, 0), 0.028f, ray);
                    }
                    // pelvic fins + anal fin
                    foreach (var sd in new[] { -1, 1 }) Cone(b, new Vector3(sd * 0.04f, -0.1f, -0.02f), new Vector3(0.05f, 0.07f, 0.012f), fin, new Vector3(30, 0, -sd * 130));
                    var an = Node("Anal", b, new Vector3(0, -0.095f, -0.06f), new Vector3(0, 90, 0));
                    Msh(Meshes.Fan("petfishanal", new[] { new Vector2(0, 0.02f), new Vector2(0.03f, -0.05f), new Vector2(0.08f, -0.04f), new Vector2(0.1f, 0.03f) }, 0.02f), an, Vector3.zero, Vector3.one, fin);
                    foreach (var x in new[] { 0.03f, 0.055f, 0.078f }) LRod(an, new Vector3(x - 0.01f, 0.01f, 0), new Vector3(x, -0.04f, 0), 0.026f, ray);
                    // side details: gill arc + slits, lateral line, scales
                    Vector3 Sx(int sd, float y, float z, float off)
                    {
                        float v = y / r0.y, w = z / r0.z;
                        return new Vector3(sd * (r0.x * Mathf.Sqrt(Mathf.Max(0.02f, 1 - v * v - w * w)) + off), y, z);
                    }
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var gp = new[] { Sx(sd, 0.07f, 0.03f, 0.002f), Sx(sd, 0.035f, 0.05f, 0.002f), Sx(sd, -0.01f, 0.05f, 0.002f), Sx(sd, -0.05f, 0.03f, 0.002f) };
                        for (int k = 0; k < 3; k++) Dash(b, gp[k], gp[k + 1], 0.008f, ray, Nr(c0, r0, gp[k]));
                        foreach (var z in new[] { 0.015f, 0f }) { var a = Sx(sd, 0.065f, z, 0.002f); var c = Sx(sd, 0.03f, z, 0.002f); Dash(b, a, c, 0.006f, ray, Nr(c0, r0, a)); }
                        for (int k = 0; k < 6; k++) T(b, Sx(sd, 0.028f, -0.03f - k * 0.027f, 0.004f), 0.011f, ray);
                        for (int j = 0; j < 4; j++)
                            for (int i = 0; i < 6; i++)
                            {
                                float th = 52 + j * 25, ph = -12 - (i + (j % 2) * 0.5f) * 8.5f;
                                if (sd < 0) ph = 180 - ph;
                                OnN(Sm(b, Dt.Ell(c0, r0, th, ph, 1f), new Vector3(0.038f, 0.038f, 0.01f), scl), Dt.EllN(r0, th, ph));
                            }
                    }
                    // eyes with rims
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = Fz(c0, r0, sd * 0.072f, 0.04f, -0.012f);
                        var n = (Nr(c0, r0, ep) + Vector3.forward * 0.8f).normalized;
                        Lc(b, ep - n * 0.004f, new Vector3(0.078f, 0.004f, 0.078f), ray).transform.localRotation = Quaternion.FromToRotation(Vector3.up, n);
                        Eye(b, ep, n, 0.062f, M("2B2B6B", 0.8f));
                    }
                    // lips
                    var lip = M("FF6E40", 0.6f);
                    Mb(b, new Vector3(0, 0.012f, 0.195f), new Vector3(0.05f, 0.022f, 0.03f), lip);
                    Mb(b, new Vector3(0, -0.012f, 0.193f), new Vector3(0.045f, 0.02f, 0.028f), lip);
                    Mb(b, new Vector3(0, 0, 0.203f), new Vector3(0.03f, 0.014f, 0.01f), M("3A1A10", 0.5f));
                    // bubbles rising in front
                    var bub = Glass("CFF3FF", 0.4f);
                    for (int k = 0; k < 4; k++)
                    {
                        var bb = Sm(b, new Vector3(0.02f * Mathf.Sin(k * 2f), 0.06f + k * 0.055f, 0.24f + k * 0.015f), Vector3.one * (0.03f + k * 0.008f), bub);
                        var bo = Add<Bob>(bb); bo.amp = 0.02f; bo.speed = 1.5f + k * 0.3f;
                        T(bb.transform, new Vector3(-0.2f, 0.22f, 0.3f), 0.25f, shine);
                    }
                    break;
                }
                case 7: // blob: wobbly jelly with lumps and drips, swirling inner bubbles, glowing nucleus, floating bubbles, pseudopods
                {
                    var jelly = Glass("B57CFF", 0.75f);
                    var bubM = Glass("E6D4FF", 0.5f);
                    Vector3 c0 = Vector3.zero, r0 = new Vector3(0.19f, 0.16f, 0.18f);
                    var bl = S(b, Vector3.zero, r0 * 2, jelly);
                    Add<Pulse>(bl).amount = 0.12f;
                    // lumps, drips, top drop and gloss wobble with the body (children, unit-sphere space)
                    var lt = bl.transform; var u5 = Vector3.one * 0.5f;
                    var rng = new Rng(7);
                    for (int k = 0; k < 10; k++)
                    {
                        float th = rng.F(20, 120), ph = rng.F(0, 360);
                        if (ph > 50 && ph < 130 && th > 45) ph += 120;   // keep the face clear
                        Sm(lt, Dt.Ell(Vector3.zero, u5, th, ph, 0.9f), Vector3.one * rng.F(0.18f, 0.26f), jelly);
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        var p = Dt.Ell(Vector3.zero, u5, 128 + (k % 3) * 8, k * 45 + 20, 0.92f);
                        Sm(lt, p + Vector3.down * 0.05f, new Vector3(0.14f, 0.2f, 0.14f), jelly);
                        T(lt, p + Vector3.down * 0.17f, 0.09f, jelly);
                    }
                    Cone(lt, new Vector3(0, 0.44f, 0), new Vector3(0.16f, 0.2f, 0.16f), jelly);
                    T(lt, new Vector3(0, 0.66f, 0), 0.1f, jelly);
                    var glossA = Add("FFFFFF", 0.35f);
                    OnN(Mb(lt, Dt.Ell(Vector3.zero, u5, 25, 45, 1f), new Vector3(0.22f, 0.12f, 0.06f), glossA), Dt.EllN(u5, 25, 45));
                    OnN(Mb(lt, Dt.Ell(Vector3.zero, u5, 40, 20, 1f), new Vector3(0.08f, 0.06f, 0.04f), glossA), Dt.EllN(u5, 40, 20));
                    foreach (var a in new[] { new Vector2(30, 150), new Vector2(50, 170), new Vector2(35, 10) }) T(lt, Dt.Ell(Vector3.zero, u5, a.x, a.y, 1.01f), 0.04f, shine);
                    // swirling inner bubbles with highlights
                    var inner = Node("Inner", b, Vector3.zero); Add<Spin>(inner.gameObject).speed = 25;
                    for (int k = 0; k < 14; k++)
                    {
                        var p = Dt.Ell(Vector3.zero, new Vector3(0.12f, 0.1f, 0.11f), rng.F(20, 160), rng.F(0, 360), rng.F(0.3f, 0.95f));
                        float d = rng.F(0.02f, 0.05f);
                        Sm(inner, p, Vector3.one * d, bubM);
                        T(inner, p + new Vector3(-0.3f, 0.3f, 0.3f) * d, d * 0.3f, shine);
                    }
                    // glowing nucleus with orbiting organelles
                    var nuc = Mb(b, new Vector3(0, -0.03f, -0.04f), Vector3.one * 0.08f, G("E040FB", 1.6f)); Add<Pulse>(nuc).amount = 0.1f;
                    var org = Node("Organelles", b, new Vector3(0, -0.03f, -0.04f)); var osp = Add<Spin>(org.gameObject); osp.speed = 70; osp.axis = new Vector3(0.3f, 1, 0);
                    for (int k = 0; k < 6; k++) { float a = k * 60 * Mathf.Deg2Rad; T(org, new Vector3(Mathf.Cos(a) * 0.065f, Mathf.Sin(k * 2f) * 0.02f, Mathf.Sin(a) * 0.065f), 0.022f, G("FFD24A", 1.5f)); }
                    // face: eyes, open mouth with tongue, cheeks, sparkle freckles
                    var mouthM = M("2A0F3A", 0.6f);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = Fz(c0, r0, sd * 0.07f, 0.04f, -0.012f);
                        Eye(b, ep, Nr(c0, r0, ep), 0.07f, M("4A148C", 0.8f));
                        T(b, Fz(c0, r0, sd * 0.03f, -0.035f, 0), 0.012f, mouthM);
                        var cp = Fz(c0, r0, sd * 0.115f, -0.01f, -0.002f);
                        OnN(Mb(b, cp, new Vector3(0.05f, 0.028f, 0.012f), pink), Nr(c0, r0, cp));
                        for (int k = 0; k < 3; k++) T(b, Fz(c0, r0, sd * (0.11f + k * 0.017f), 0.025f + (k == 1 ? 0.008f : 0), 0), 0.011f, M("F3E5FF", 0.6f));
                    }
                    var mp = Fz(c0, r0, 0, -0.04f, -0.004f);
                    Mb(b, mp, new Vector3(0.05f, 0.035f, 0.02f), mouthM);
                    Mb(b, mp + new Vector3(0, -0.008f, 0.006f), new Vector3(0.028f, 0.014f, 0.01f), M("FF7FA8", 0.4f));
                    // bubbles floating around (each bobs with its highlight)
                    foreach (var deg in new[] { -10f, 35f, 145f, 190f, 235f, 305f })
                    {
                        float a = deg * Mathf.Deg2Rad; int k = (int)(deg + 10) / 45;
                        var fb = Sm(b, new Vector3(Mathf.Cos(a) * 0.25f, 0.05f + (k % 3) * 0.06f, Mathf.Sin(a) * 0.22f - 0.04f), Vector3.one * (0.035f + (k % 2) * 0.015f), Glass("D9C2FF", 0.4f));
                        var bo = Add<Bob>(fb); bo.amp = 0.03f; bo.speed = 1.2f + k * 0.2f;
                        T(fb.transform, new Vector3(-0.22f, 0.22f, 0.3f), 0.25f, shine);
                    }
                    // little pseudopod arms
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var arm = Node("Pod", b, new Vector3(sd * 0.17f, -0.06f, 0.03f));
                        var s = Add<Sway>(arm.gameObject); s.amp = 18; s.speed = 3; s.phase = sd;
                        for (int k = 0; k < 3; k++) Sm(arm, new Vector3(sd * 0.03f * k, -0.012f * k, 0.01f * k), Vector3.one * (0.07f - k * 0.015f), jelly);
                    }
                    break;
                }
                case 101: // snow owlet: facial discs, feathered wings & tail (Dt.Feather), chevron chest, ice crown, talons, frost motes
                {
                    var w = M("F4F7FA", 0.1f);
                    var soft = M("FFFFFF", 0.2f);
                    var frost = M("9FB3CC", 0.3f);
                    var vane = M("DCE6F0", 0.15f);
                    var ice = Glass("BFE6FF", 0.65f);
                    Vector3 c0 = Vector3.zero, r0 = new Vector3(0.16f, 0.18f, 0.15f);
                    S(b, Vector3.zero, r0 * 2, w);
                    // facial discs with big amber eyes (rim, iris, pupil, two sparkles) and frosty V-brows
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var dp = Fz(c0, r0, sd * 0.062f, 0.06f, -0.016f);
                        var dn = Nr(c0, r0, dp);
                        OnN(Mb(b, dp, new Vector3(0.13f, 0.13f, 0.04f), soft), dn);
                        var ep = dp + dn * 0.02f; var q = Quaternion.LookRotation(dn, Vector3.up);
                        Lc(b, ep, new Vector3(0.1f, 0.005f, 0.1f), M("E0A800", 0.5f)).transform.localRotation = q * Quaternion.Euler(90, 0, 0);
                        Mb(b, ep + dn * 0.004f, new Vector3(0.088f, 0.088f, 0.03f), M("FFC83A")).transform.localRotation = q;
                        Mb(b, ep + dn * 0.013f, new Vector3(0.048f, 0.048f, 0.02f), black).transform.localRotation = q;
                        T(b, ep + dn * 0.024f + q * new Vector3(0.01f, 0.012f, 0), 0.014f, shine);
                        T(b, ep + dn * 0.022f + q * new Vector3(-0.01f, -0.01f, 0), 0.007f, shine);
                        var bm = Fz(c0, r0, sd * 0.075f, 0.14f, 0.003f);
                        Dash(b, Fz(c0, r0, sd * 0.025f, 0.12f, 0.003f), bm, 0.014f, frost, Nr(c0, r0, bm));
                        Dash(b, bm, Fz(c0, r0, sd * 0.11f, 0.13f, 0.003f), 0.011f, frost, Nr(c0, r0, bm));
                    }
                    // beak (upper, lower, nostrils)
                    Cone(b, new Vector3(0, 0.01f, 0.14f), new Vector3(0.04f, 0.06f, 0.04f), M("FF9A2E"), new Vector3(100, 0, 0));
                    Cone(b, new Vector3(0, -0.002f, 0.138f), new Vector3(0.028f, 0.035f, 0.025f), M("E07B1A", 0.4f), new Vector3(125, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) T(b, new Vector3(sd * 0.008f, 0.024f, 0.147f), 0.008f, M("8A4A10"));
                    // ear tufts
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Cone(b, new Vector3(sd * 0.09f, 0.14f, 0), new Vector3(0.07f, 0.1f, 0.05f), w, new Vector3(0, 0, -sd * 15));
                        Sp(b, new Vector3(sd * 0.075f, 0.15f, 0.01f), new Vector3(sd * 0.1f, 0.24f, 0), 0.025f, frost);
                        Sp(b, new Vector3(sd * 0.11f, 0.13f, -0.005f), new Vector3(sd * 0.16f, 0.21f, -0.01f), 0.025f, w);
                    }
                    // chevron markings on the chest
                    foreach (var cv in new[] { new Vector2(-0.04f, -0.06f), new Vector2(0.04f, -0.065f), new Vector2(-0.065f, -0.11f), new Vector2(0f, -0.105f), new Vector2(0.065f, -0.11f) })
                    {
                        var a = Fz(c0, r0, cv.x - 0.014f, cv.y + 0.012f, 0.002f); var m = Fz(c0, r0, cv.x, cv.y, 0.002f); var c = Fz(c0, r0, cv.x + 0.014f, cv.y + 0.012f, 0.002f);
                        Dash(b, a, m, 0.01f, frost, Nr(c0, r0, m)); Dash(b, m, c, 0.01f, frost, Nr(c0, r0, m));
                    }
                    // wings: covert + three flight feathers each
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var piv = Node("Wing", b, new Vector3(sd * 0.15f, 0.03f, -0.01f));
                        var fl = Add<Flap>(piv.gameObject); fl.side = sd; fl.idleAmp = 12;
                        Mb(piv, new Vector3(sd * 0.005f, -0.04f, 0), new Vector3(0.05f, 0.18f, 0.14f), w);
                        for (int k = 0; k < 3; k++)
                            Dt.Feather(piv, new Vector3(sd * 0.02f, -0.07f, -0.04f + k * 0.035f), new Vector3(sd * 0.25f, -1, -0.35f), Vector3.forward, 0.1f - k * 0.01f, vane, frost, 3);
                    }
                    // feet: toes + talons
                    foreach (var sd in new[] { -1, 1 })
                        for (int k = -1; k <= 1; k++)
                        {
                            var tp = new Vector3(sd * 0.05f + k * 0.018f, -0.172f, 0.07f + (k == 0 ? 0.01f : 0));
                            Sm(b, tp, new Vector3(0.022f, 0.02f, 0.045f), M("FFB74D", 0.4f), new Vector3(0, k * 18, 0));
                            Sp(b, tp + new Vector3(k * 0.005f, 0, 0.02f), tp + new Vector3(k * 0.008f, -0.01f, 0.035f), 0.01f, M("5A6B80", 0.5f));
                        }
                    // tail fan
                    for (int k = -1; k <= 1; k++) Dt.Feather(b, new Vector3(k * 0.03f, -0.1f, -0.12f), new Vector3(k * 0.35f, -0.45f, -1), Vector3.right, 0.12f, vane, frost, 2);
                    // ice crown: three crystals with glowing gems
                    for (int k = -1; k <= 1; k++)
                    {
                        var cb = new Vector3(k * 0.035f, 0.175f - Mathf.Abs(k) * 0.01f, 0.02f);
                        Sp(b, cb, new Vector3(k * 0.05f, 0.25f - Mathf.Abs(k) * 0.025f, 0.02f), 0.035f - Mathf.Abs(k) * 0.008f, ice);
                        T(b, cb + new Vector3(0, 0.004f, 0.012f), 0.018f, G("BFF3FF", 1.8f));
                    }
                    // frost motes + fluffy head
                    var motes = Node("Frost", b, Vector3.zero); Add<Spin>(motes.gameObject).speed = 35;
                    for (int k = 0; k < 8; k++) { float a = k * 45 * Mathf.Deg2Rad; T(motes, new Vector3(Mathf.Cos(a) * 0.25f, Mathf.Sin(k * 1.9f) * 0.08f, Mathf.Sin(a) * 0.25f), 0.018f, G("E8F6FF", 1.4f)); }
                    var rng = new Rng(101);
                    for (int k = 0; k < 8; k++) Sm(b, Dt.Ell(c0, r0, rng.F(15, 60), rng.F(200, 340), 0.98f), Vector3.one * 0.04f, w);
                    break;
                }
                case 105: // golden piggy bank: coin slot with a coin, riveted belt with gems, hooves, curly tail, orbiting coins
                {
                    var g = M("FFC83A", 0.95f, 0.9f);
                    var dg = M("D4A017", 0.9f, 0.9f);
                    var coin = M("FFE14D", 0.95f, 0.8f);
                    Vector3 c0 = Vector3.zero, r0 = new Vector3(0.19f, 0.16f, 0.21f);
                    S(b, Vector3.zero, r0 * 2, g);
                    // snout: disc, rim, nostrils, bead trim at the base
                    Cy(b, new Vector3(0, 0, 0.21f), new Vector3(0.14f, 0.03f, 0.14f), g, new Vector3(90, 0, 0));
                    Msh(Meshes.Torus(0.07f, 0.008f, 20, 6), b, new Vector3(0, 0, 0.238f), Vector3.one, dg, new Vector3(90, 0, 0));
                    foreach (var sd in new[] { -1, 1 }) Mb(b, new Vector3(sd * 0.03f, 0, 0.24f), new Vector3(0.03f, 0.04f, 0.02f), black);
                    for (int k = 0; k < 10; k++) { float a = k * 36 * Mathf.Deg2Rad; T(b, new Vector3(Mathf.Cos(a) * 0.072f, Mathf.Sin(a) * 0.072f, 0.196f), 0.018f, dg); }
                    // coin slot with a framed edge and a ₸ coin half inside
                    var st = new Vector3(0, 0.16f, -0.02f);
                    B(b, st, new Vector3(0.12f, 0.02f, 0.02f), black);
                    foreach (var sd in new[] { -1, 1 })
                    {
                        B(b, st + new Vector3(0, 0.002f, sd * 0.016f), new Vector3(0.14f, 0.012f, 0.012f), dg);
                        B(b, st + new Vector3(sd * 0.068f, 0.002f, 0), new Vector3(0.012f, 0.012f, 0.044f), dg);
                    }
                    Lc(b, st + new Vector3(0, 0.022f, 0), new Vector3(0.07f, 0.004f, 0.07f), coin, new Vector3(90, 0, 0));
                    B(b, st + new Vector3(0, 0.024f, 0.005f), new Vector3(0.005f, 0.026f, 0.003f), dg);
                    B(b, st + new Vector3(0, 0.04f, 0.005f), new Vector3(0.024f, 0.005f, 0.003f), dg);
                    B(b, st + new Vector3(0, 0.033f, 0.005f), new Vector3(0.024f, 0.005f, 0.003f), dg);
                    // belt with rivets and three gems (sides + back)
                    Msh(Meshes.Torus(0.195f, 0.012f, 32, 6), b, Vector3.zero, new Vector3(1, 1, 1.1f), dg);
                    for (int k = 0; k < 16; k++)
                    {
                        float a = k * 22.5f; if ((a > 60 && a < 120) || a == 0 || a == 180 || a == 270) continue;
                        float ar = a * Mathf.Deg2Rad; T(b, new Vector3(Mathf.Cos(ar) * 0.207f, 0, Mathf.Sin(ar) * 0.228f), 0.016f, g);
                    }
                    var gems = new[] { G("FF2E6E", 1.3f), G("2ECC71", 1.3f), G("2E7DFF", 1.3f) };
                    var ga = new[] { 0f, 180f, 270f };
                    for (int k = 0; k < 3; k++) { float ar = ga[k] * Mathf.Deg2Rad; var n = new Vector3(Mathf.Cos(ar), 0, Mathf.Sin(ar)); Dt.Gem(b, new Vector3(n.x * 0.205f, 0, n.z * 0.226f), 0.035f, gems[k], g, n); }
                    // rivets down the back seam
                    foreach (var th in new[] { 30f, 45f, 60f, 75f, 105f, 120f, 135f, 150f }) T(b, Dt.Ell(c0, r0, th, 270, 1.01f), 0.014f, dg);
                    // ears with pink insides
                    foreach (var sd in new[] { -1, 1 })
                    {
                        Cone(b, new Vector3(sd * 0.1f, 0.14f, 0.1f), new Vector3(0.08f, 0.1f, 0.05f), g);
                        Cone(b, new Vector3(sd * 0.1f, 0.142f, 0.118f), new Vector3(0.05f, 0.07f, 0.02f), M("FF9E80", 0.5f));
                    }
                    // eyes with lashes, cheeks
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var ep = Fz(c0, r0, sd * 0.08f, 0.08f, -0.012f);
                        var en = Nr(c0, r0, ep); var q = Quaternion.LookRotation(en, Vector3.up);
                        Eye(b, ep, en, 0.06f, M("5A3418", 0.8f));
                        for (int k = 0; k < 3; k++)
                        {
                            float a = (40 + k * 22) * Mathf.Deg2Rad;
                            var p0 = ep + en * 0.01f + q * new Vector3(sd * Mathf.Cos(a) * 0.028f, Mathf.Sin(a) * 0.028f, 0);
                            Sp(b, p0, p0 + q * new Vector3(sd * Mathf.Cos(a), Mathf.Sin(a), 0.2f) * 0.022f, 0.009f, black);
                        }
                        var cp = Fz(c0, r0, sd * 0.115f, -0.005f, -0.002f);
                        OnN(Mb(b, cp, new Vector3(0.05f, 0.03f, 0.014f), pink), Nr(c0, r0, cp));
                    }
                    // smile under the snout
                    Vector3 s0 = Fz(c0, r0, -0.035f, -0.085f, 0.002f), s1 = Fz(c0, r0, -0.012f, -0.095f, 0.002f), s2 = Fz(c0, r0, 0.012f, -0.095f, 0.002f), s3 = Fz(c0, r0, 0.035f, -0.085f, 0.002f);
                    Dash(b, s0, s1, 0.007f, black, Nr(c0, r0, s1)); Dash(b, s1, s2, 0.007f, black, Nr(c0, r0, s1)); Dash(b, s2, s3, 0.007f, black, Nr(c0, r0, s2));
                    // legs with hooves
                    foreach (var p in new[] { new Vector3(-0.1f, -0.15f, 0.1f), new Vector3(0.1f, -0.15f, 0.1f), new Vector3(-0.1f, -0.15f, -0.1f), new Vector3(0.1f, -0.15f, -0.1f) })
                    {
                        Cy(b, p, new Vector3(0.07f, 0.04f, 0.07f), g);
                        Lc(b, p + new Vector3(0, -0.036f, 0), new Vector3(0.076f, 0.01f, 0.076f), dg);
                        B(b, p + new Vector3(0, -0.036f, 0.03f), new Vector3(0.006f, 0.022f, 0.016f), black);
                    }
                    // curly tail
                    for (int k = 0; k < 9; k++)
                    {
                        float u = k / 8f, a = u * Mathf.PI * 3, rad = 0.022f * (1 - u * 0.4f);
                        Sm(b, new Vector3(Mathf.Cos(a) * rad, 0.03f + Mathf.Sin(a) * rad, -0.215f - u * 0.045f), Vector3.one * (0.026f - u * 0.008f), g);
                    }
                    // money plug underneath
                    Lc(b, new Vector3(0, -0.158f, 0), new Vector3(0.08f, 0.008f, 0.08f), dg);
                    Mb(b, new Vector3(0, -0.168f, 0), new Vector3(0.04f, 0.02f, 0.04f), dg);
                    // orbiting coins + sparkles
                    var orb = Node("Coins", b, new Vector3(0, 0.05f, 0)); Add<Spin>(orb.gameObject).speed = 60;
                    var coinG = G("FFE14D", 1.1f);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 60 * Mathf.Deg2Rad; var n = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        var p = n * 0.33f + Vector3.up * (Mathf.Sin(k * 2.2f) * 0.06f); var q = Quaternion.LookRotation(n, Vector3.up);
                        Lc(orb, p, new Vector3(0.06f, 0.005f, 0.06f), coinG).transform.localRotation = q * Quaternion.Euler(90, 0, 0);
                        B(orb, p + n * 0.006f, new Vector3(0.006f, 0.03f, 0.004f), dg).transform.localRotation = q;
                    }
                    var star = Meshes.Star(4, 0.35f, 0.2f);
                    for (int k = 0; k < 4; k++) { float a = (k * 90 + 30) * Mathf.Deg2Rad; Msh(star, orb, new Vector3(Mathf.Cos(a) * 0.29f, 0.12f - (k % 2) * 0.2f, Mathf.Sin(a) * 0.29f), Vector3.one * 0.05f, shine, new Vector3(0, 60 - k * 90, 0)); }
                    break;
                }
            }

            // merge static leaf parts per (parent, material); the meshes are freed with the pet body
            var pm = b.GetComponent<PetMeshes>();
            if (!pm) pm = b.gameObject.AddComponent<PetMeshes>();
            if (ChubrikModel.BakeParts) Baker.Bake(b, new HashSet<GameObject>(), pm.meshes);
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

    /// <summary>Owns the meshes baked for one pet body and destroys them together with it.</summary>
    public class PetMeshes : MonoBehaviour
    {
        public readonly List<Mesh> meshes = new List<Mesh>();
        public static void Free(Transform body) { var pm = body ? body.GetComponent<PetMeshes>() : null; if (pm) pm.FreeAll(); }
        void FreeAll() { foreach (var m in meshes) if (m) Destroy(m); meshes.Clear(); }
        void OnDestroy() => FreeAll();
    }
}
