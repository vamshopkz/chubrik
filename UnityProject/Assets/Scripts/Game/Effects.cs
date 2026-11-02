using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Particle effects: one persistent world-space particle system per kind (Emit at any point),
    /// step effects, damage effects, victory / teleport effects, little floating texts.
    /// </summary>
    public class Effects : MonoBehaviour
    {
        public static Effects I;
        readonly Dictionary<string, ParticleSystem> systems = new Dictionary<string, ParticleSystem>();

        public static void Create()
        {
            I = new GameObject("Effects").AddComponent<Effects>();
        }

        class Spec { public string tex = "soft"; public bool add; public float life = 1, size = 0.2f, grav, speed = 1; public Color col = Color.white; public bool rainbow, grow; }

        static Spec SpecOf(string k)
        {
            switch (k)
            {
                case "fire": return new Spec { add = true, life = 0.6f, size = 0.28f, grav = -0.4f, speed = 1.2f, col = Parts.Hex("FF7A1E") };
                case "bubble": return new Spec { tex = "ring", life = 1.6f, size = 0.22f, grav = -0.08f, speed = 0.5f, col = new Color(0.8f, 0.95f, 1f, 0.8f) };
                case "slime": return new Spec { tex = "disc", life = 2.2f, size = 0.18f, grav = 1.2f, speed = 1f, col = Parts.Hex("6EDC3C") };
                case "pixel": return new Spec { tex = "square", life = 0.9f, size = 0.12f, grav = 0.6f, speed = 1.4f, rainbow = true };
                case "stardust": return new Spec { tex = "spark", add = true, life = 1f, size = 0.22f, grav = -0.05f, speed = 0.6f, col = Parts.Hex("FFF2A0") };
                case "bolt": return new Spec { tex = "streak", add = true, life = 0.25f, size = 0.45f, speed = 2.5f, col = Parts.Hex("7FD8FF") };
                case "ice": return new Spec { tex = "star", life = 1.2f, size = 0.16f, grav = 0.4f, speed = 1f, col = Parts.Hex("BFEFFF") };
                case "coin": return new Spec { tex = "disc", life = 1f, size = 0.14f, grav = 1.6f, speed = 2.2f, col = Parts.Hex("FFC83A") };
                case "comet": return new Spec { add = true, life = 0.9f, size = 0.3f, grav = -0.1f, speed = 0.8f, col = Parts.Hex("9FD0FF") };
                case "rainbow": return new Spec { add = true, life = 1.1f, size = 0.26f, grav = -0.1f, speed = 0.6f, rainbow = true };
                case "heart": return new Spec { tex = "heart", life = 1.6f, size = 0.22f, grav = -0.35f, speed = 0.5f, col = Parts.Hex("FF6FA8") };
                case "smoke": return new Spec { life = 1.6f, size = 0.7f, grav = -0.15f, speed = 0.8f, col = new Color(0.75f, 0.75f, 0.8f, 0.7f), grow = true };
                case "confetti": return new Spec { tex = "square", life = 2.6f, size = 0.12f, grav = 0.8f, speed = 6f, rainbow = true };
                case "glyph": return new Spec { tex = "glyph", add = true, life = 1.3f, size = 0.3f, grav = 0.7f, speed = 0.2f, col = Parts.Hex("3DFF6E") };
                case "star": return new Spec { tex = "star", life = 0.9f, size = 0.26f, grav = 0.2f, speed = 3f, col = Parts.Hex("FFE14D") };
                case "splash": return new Spec { tex = "disc", life = 0.9f, size = 0.16f, grav = 2.2f, speed = 4f, col = Parts.Hex("7FD0FF") };
                case "dark": return new Spec { life = 1.4f, size = 0.4f, grav = -0.2f, speed = 0.4f, col = new Color(0.25f, 0.05f, 0.4f, 0.75f), grow = true };
                case "gold": return new Spec { tex = "spark", add = true, life = 1f, size = 0.24f, grav = -0.1f, speed = 0.7f, col = Parts.Hex("FFD24A") };
                case "glitch": return new Spec { tex = "square", add = true, life = 0.3f, size = 0.18f, speed = 1.5f, rainbow = true };
                case "elec": return new Spec { tex = "streak", add = true, life = 0.18f, size = 0.35f, speed = 1.5f, col = Parts.Hex("9FE8FF") };
                case "dust": return new Spec { life = 0.7f, size = 0.35f, grav = -0.05f, speed = 1.2f, col = new Color(0.95f, 0.9f, 0.8f, 0.6f), grow = true };
                case "magic": return new Spec { tex = "spark", add = true, life = 1.2f, size = 0.28f, grav = -0.3f, speed = 0.5f, col = Parts.Hex("D9A0FF") };
                default: return new Spec();
            }
        }

        ParticleSystem Sys(string kind)
        {
            if (systems.TryGetValue(kind, out var ps) && ps) return ps;
            var sp = SpecOf(kind);
            var go = new GameObject("FX " + kind);
            go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(sp.life * 0.7f, sp.life);
            main.startSize = new ParticleSystem.MinMaxCurve(sp.size * 0.7f, sp.size * 1.2f);
            main.startSpeed = 0;
            main.gravityModifier = sp.grav;
            main.maxParticles = 1500;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1, sp.grow ? AnimationCurve.Linear(0, 0.6f, 1, 1.8f) : AnimationCurve.Linear(0, 1, 1, 0.5f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            var tex = Patterns.Shape(sp.tex);
            r.sharedMaterial = sp.add ? Mats.Additive(Color.white, tex) : Mats.Particles(tex);
            r.renderMode = sp.tex == "streak" ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (sp.tex == "streak") { r.velocityScale = 0.05f; r.lengthScale = 2f; }
            ps.Play();
            systems[kind] = ps;
            specs[kind] = sp;
            return ps;
        }
        readonly Dictionary<string, Spec> specs = new Dictionary<string, Spec>();

        /// <summary>Emit <paramref name="n"/> particles of a kind around <paramref name="pos"/>.</summary>
        public static void Emit(string kind, Vector3 pos, int n, float radius = 0.2f, Vector3 vel = default, Color? color = null, float speedMul = 1f)
        {
            if (I == null) return;
            var ps = I.Sys(kind);
            var sp = I.specs[kind];
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                var rnd = UnityEngine.Random.insideUnitSphere;
                ep.position = pos + rnd * radius;
                ep.velocity = vel + rnd.normalized * sp.speed * speedMul * UnityEngine.Random.Range(0.4f, 1f);
                Color c = color ?? sp.col;
                if (sp.rainbow && color == null) c = Color.HSVToRGB(UnityEngine.Random.value, 0.8f, 1f);
                ep.startColor = c;
                ps.Emit(ep, 1);
            }
        }

        // ---------------- steps ----------------

        public static void Step(int kind, Vector3 foot)
        {
            switch (kind)
            {
                case 0: Emit("fire", foot, 4, 0.08f, Vector3.up * 0.8f); break;
                case 1: Emit("bubble", foot + Vector3.up * 0.1f, 2, 0.1f, Vector3.up * 0.4f); break;
                case 2: Emit("slime", foot, 3, 0.06f, Vector3.up * 0.6f); break;
                case 3: Emit("pixel", foot, 4, 0.08f, Vector3.up * 1.2f); break;
                case 4: Emit("stardust", foot + Vector3.up * 0.1f, 4, 0.12f); break;
                case 5: Emit("bolt", foot + Vector3.up * 0.1f, 3, 0.1f); break;
                case 6: Emit("ice", foot, 4, 0.1f, Vector3.up * 0.8f); break;
                case 7: Emit("coin", foot, 2, 0.06f, Vector3.up * 1.5f); break;
                case 102: Emit("comet", foot + Vector3.up * 0.1f, 5, 0.1f); break;
                case 106: Emit("rainbow", foot + Vector3.up * 0.1f, 5, 0.12f); break;
                default: Emit("dust", foot, 1, 0.05f, Vector3.up * 0.3f); break;
            }
        }

        public static int StepKind(Look l)
        {
            var id = l.Of("steps");
            if (string.IsNullOrEmpty(id)) return -1;
            int s = Catalog.SetOf(id);
            return s >= 0 ? 100 + s : Catalog.Index(id);
        }

        // ---------------- floating text (Zzz, +1) ----------------

        public static void Float(string text, Vector3 pos, Color c, float size = 3f)
        {
            if (I == null) return;
            var t = Prim.Text(I.transform, text, pos, size, c);
            t.transform.position = pos;
            t.gameObject.AddComponent<Floater>();
        }

        public static void Zzz(Vector3 pos) => Float("Z", pos, new Color(0.8f, 0.9f, 1f), 3f);

        // ---------------- damage ----------------

        public static void Damage(int kind, ChubrikModel m)
        {
            if (I == null || m == null) return;
            AudioFx.Play("hurt");
            var head = m.HeadC ? m.HeadC.position : m.transform.position + Vector3.up;
            switch (kind)
            {
                case 1: Hud.Crack(); break;
                case 2: Emit("star", head + Vector3.up * 0.3f, 14, 0.2f); break;
                case 3: Hud.Glitch(); break;
                case 4: Emit("pixel", m.transform.position + Vector3.up * 0.7f, 30, 0.45f, Vector3.up, ChubrikModel.BodyColor(m.look.Idx("color"))); I.StartCoroutine(I.Flicker(m, 0.7f)); break;
                case 5: I.StartCoroutine(I.Lightning(head)); break;
                case 6: Emit("ice", m.transform.position + Vector3.up * 0.7f, 24, 0.4f, Vector3.up * 2); I.StartCoroutine(I.Tint(m, new Color(0.6f, 0.85f, 1f), 0.8f)); break;
                case 7: Emit("splash", m.transform.position + Vector3.up * 0.5f, 30, 0.3f, Vector3.up * 3); break;
                default: Hud.Flash(new Color(1, 0.1f, 0.1f, 0.45f)); break;
            }
        }

        IEnumerator Flicker(ChubrikModel m, float time)
        {
            var rs = m.GetComponentsInChildren<Renderer>();
            for (float t = 0; t < time; t += 0.08f)
            {
                bool on = ((int)(t / 0.08f)) % 2 == 0;
                foreach (var r in rs) if (r) r.enabled = on;
                yield return new WaitForSeconds(0.08f);
            }
            foreach (var r in rs) if (r) r.enabled = true;
        }

        IEnumerator Tint(ChubrikModel m, Color c, float time)
        {
            var rs = m.GetComponentsInChildren<Renderer>();
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c);
            foreach (var r in rs) if (r) r.SetPropertyBlock(mpb);
            yield return new WaitForSeconds(time);
            foreach (var r in rs) if (r) r.SetPropertyBlock(null);
        }

        IEnumerator Lightning(Vector3 target)
        {
            Hud.Flash(new Color(1, 1, 0.85f, 0.5f));
            AudioFx.Play("zap");
            var root = new GameObject("Bolt").transform;
            var mat = Parts.G("FFF59D", 4f);
            var p = target + Vector3.up * 9;
            var r = new Rng(Environment.TickCount);
            Parts.Layer = 0;
            while (p.y > target.y + 0.2f)
            {
                var q = new Vector3(target.x + r.F(-0.5f, 0.5f), p.y - r.F(0.8f, 1.6f), target.z + r.F(-0.5f, 0.5f));
                if (q.y < target.y) q = target;
                Parts.Rod(root, p, q, 0.09f, mat);
                p = q;
            }
            Emit("elec", target, 20, 0.3f);
            yield return new WaitForSeconds(0.25f);
            Destroy(root.gameObject);
        }

        // ---------------- victory / teleport ----------------

        /// <summary>Plays a victory effect; <paramref name="mid"/> runs when the chubrik is hidden (teleport moment).</summary>
        public static void Victory(int kind, ChubrikModel m, Action mid = null)
        {
            if (I == null || m == null) { mid?.Invoke(); return; }
            I.StartCoroutine(I.VictoryCo(kind, m, mid));
        }

        static void Show(ChubrikModel m, bool on)
        {
            foreach (var r in m.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        }

        IEnumerator VictoryCo(int kind, ChubrikModel m, Action mid)
        {
            Vector3 C() => m.transform.position + Vector3.up * 0.7f * m.SizeScale;
            AudioFx.Play("win" + Mathf.Clamp(kind, 0, 7));
            switch (kind)
            {
                case 0: // pixels
                    for (int i = 0; i < 6; i++) { Emit("pixel", C(), 12, 0.4f, Vector3.up * 3); yield return new WaitForSeconds(0.05f); }
                    Show(m, false); yield return new WaitForSeconds(0.15f);
                    mid?.Invoke();
                    Emit("pixel", C(), 40, 0.5f, Vector3.down * 1);
                    yield return new WaitForSeconds(0.15f); Show(m, true);
                    break;
                case 1: // confetti
                    Emit("confetti", C() + Vector3.up, 120, 0.3f, Vector3.up * 4);
                    yield return new WaitForSeconds(0.3f);
                    mid?.Invoke();
                    Emit("confetti", C() + Vector3.up, 80, 0.3f, Vector3.up * 4);
                    break;
                case 2: // portal burst
                {
                    var ring = Parts.Ring(I.transform, m.transform.position + Vector3.up * 0.05f, 0.5f, 0.06f, Parts.Add("B57CFF", 0.9f));
                    ring.transform.position = m.transform.position + Vector3.up * 0.05f;
                    for (float t = 0; t < 0.4f; t += Time.deltaTime) { ring.transform.localScale = Vector3.one * (1 + t * 6); yield return null; }
                    Show(m, false); Emit("magic", C(), 40, 0.5f);
                    mid?.Invoke();
                    ring.transform.position = m.transform.position + Vector3.up * 0.05f;
                    for (float t = 0.4f; t > 0; t -= Time.deltaTime) { ring.transform.localScale = Vector3.one * (1 + t * 6); yield return null; }
                    Destroy(ring); Show(m, true); Emit("magic", C(), 40, 0.5f);
                    break;
                }
                case 3: // smoke
                    Emit("smoke", C(), 30, 0.5f);
                    yield return new WaitForSeconds(0.2f); Show(m, false);
                    yield return new WaitForSeconds(0.3f);
                    mid?.Invoke();
                    Emit("smoke", C(), 30, 0.5f);
                    yield return new WaitForSeconds(0.2f); Show(m, true);
                    break;
                case 4: // gold rain
                    for (int i = 0; i < 20; i++) { Emit("coin", C() + Vector3.up * 3, 6, 1.6f, Vector3.down * 2); Emit("gold", C(), 2, 0.5f); if (i == 10) mid?.Invoke(); yield return new WaitForSeconds(0.06f); }
                    break;
                case 5: // rocket launch
                {
                    Parts.Layer = m.gameObject.layer;
                    var rocket = new GameObject("Rocket").transform;
                    rocket.SetParent(m.transform, false);
                    Parts.Cy(rocket, new Vector3(0, -0.05f, 0), new Vector3(0.5f, 0.05f, 0.5f), Parts.M("E53935"));
                    var fl = Parts.Cone(rocket, new Vector3(0, -0.1f, 0), new Vector3(0.4f, 0.8f, 0.4f), Parts.G("FF8A1E", 2.5f), new Vector3(180, 0, 0));
                    var body = m.Body;
                    for (float t = 0; t < 0.7f; t += Time.deltaTime)
                    {
                        float h = t * t * 30;
                        if (body) body.localPosition = Vector3.up * h;
                        rocket.localPosition = Vector3.up * h;
                        Emit("smoke", m.transform.position + Vector3.up * h, 2, 0.2f);
                        yield return null;
                    }
                    Show(m, false);
                    mid?.Invoke();
                    yield return new WaitForSeconds(0.2f);
                    Destroy(rocket.gameObject);
                    Show(m, true);
                    Emit("star", C(), 16, 0.4f);
                    break;
                }
                case 6: // matrix code
                    for (int i = 0; i < 18; i++)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * Mathf.PI / 3 + i * 0.3f;
                            Emit("glyph", C() + new Vector3(Mathf.Cos(a), 1.2f, Mathf.Sin(a)) * 0.9f, 1, 0.05f, Vector3.down * 2);
                        }
                        if (i == 9) { Show(m, false); mid?.Invoke(); }
                        if (i == 12) Show(m, true);
                        yield return new WaitForSeconds(0.05f);
                    }
                    break;
                default: // magic shine
                    for (int i = 0; i < 24; i++)
                    {
                        float a = i * 0.6f;
                        Emit("magic", C() + new Vector3(Mathf.Cos(a) * 0.7f, -0.6f + i * 0.08f, Mathf.Sin(a) * 0.7f), 3, 0.05f);
                        if (i == 12) mid?.Invoke();
                        yield return new WaitForSeconds(0.04f);
                    }
                    Emit("gold", C(), 20, 0.5f);
                    break;
            }
        }
    }

    /// <summary>World text that drifts up and fades out.</summary>
    public class Floater : MonoBehaviour
    {
        float t;
        TMPro.TextMeshPro txt;
        void Start() { txt = GetComponent<TMPro.TextMeshPro>(); }
        void Update()
        {
            t += Time.deltaTime;
            transform.position += new Vector3(Mathf.Sin(t * 3) * 0.01f, Time.deltaTime * 0.8f, 0);
            var c = Camera.main;
            if (c) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position);
            if (txt) { var col = txt.color; col.a = Mathf.Clamp01(2 - t); txt.color = col; }
            if (t > 2) Destroy(gameObject);
        }
    }

    /// <summary>Continuous aura around the chubrik (department "Ауры").</summary>
    public class Aura : MonoBehaviour
    {
        public ChubrikModel model;
        public int kind = -1;   // 0..7 regular, 100+set premium
        GameObject extra;
        float acc;

        public void Set(int k)
        {
            kind = k;
            if (extra) Destroy(extra);
            if (model == null) return;
            Parts.Layer = model.gameObject.layer;
            if (k == 6)
            {
                extra = Parts.Ring(model.HeadC, new Vector3(0, 0.62f, 0), 0.3f, 0.04f, Parts.G("FFFFFF", 2f));
                extra.AddComponent<Hue>().glow = 2.5f;
                extra.AddComponent<Spin>().speed = 50;
            }
            else if (k == 1)
            {
                extra = Parts.S(model.Torso, new Vector3(0, 0.45f, 0), new Vector3(1.4f, 1.6f, 1.4f), Parts.Add("7FD8FF", 0.18f));
                extra.AddComponent<Pulse>().amount = 0.04f;
            }
            else if (k == 107)
            {
                extra = Parts.S(model.Torso, new Vector3(0, 0.45f, 0), new Vector3(1.5f, 1.7f, 1.5f), Parts.Add("FFD24A", 0.2f));
                extra.AddComponent<Pulse>().amount = 0.05f;
            }
        }

        void Update()
        {
            if (kind < 0 || model == null || model.Torso == null) return;
            if (!model.gameObject.activeInHierarchy) return;
            acc += Time.deltaTime * 22;
            int n = (int)acc;
            if (n == 0) return;
            acc -= n;
            var c = model.Torso.position + Vector3.up * 0.4f * model.SizeScale;
            float r = 0.55f * model.SizeScale;
            switch (kind)
            {
                case 0: Effects.Emit("fire", c + Vector3.down * 0.4f, n, r, Vector3.up * 1.2f); break;
                case 1: Effects.Emit("ice", c, n / 2 + 1, r, Vector3.up * 0.2f); break;
                case 2: Effects.Emit("elec", c, n, r); break;
                case 3: Effects.Emit("glitch", c, n, r); break;
                case 4: Effects.Emit("gold", c, n, r, Vector3.up * 0.4f); break;
                case 5: Effects.Emit("dark", c, n, r, Vector3.up * 0.2f); break;
                case 6: Effects.Emit("rainbow", c + Vector3.up * 0.6f, 1, 0.3f); break;
                case 7: Effects.Emit("heart", c, Mathf.Max(1, n / 3), r, Vector3.up * 0.5f); break;
                case 100: Effects.Emit("fire", c + Vector3.down * 0.4f, n * 2, r * 1.2f, Vector3.up * 2f); break;
                case 103: Effects.Emit("smoke", c + Vector3.down * 0.5f, n, r, Vector3.up * 0.2f, new Color(0.15f, 0.1f, 0.25f, 0.7f)); break;
                case 107: Effects.Emit("gold", c, n, r * 1.1f, Vector3.up * 0.6f); break;
            }
        }

        public static int KindOf(Look l)
        {
            var id = l.Of("aura");
            if (string.IsNullOrEmpty(id)) return -1;
            int s = Catalog.SetOf(id);
            return s >= 0 ? 100 + s : Catalog.Index(id);
        }
    }

    /// <summary>Blob shadow under the chubrik in the chosen style (department "Тень").</summary>
    public class BlobShadow : MonoBehaviour
    {
        public Transform target;
        public int style;
        public float size = 1f;
        public int mask = Layers.SolidMask;
        GameObject quad;
        Material mat;

        public void Set(int s)
        {
            style = s;
            if (quad == null)
            {
                quad = new GameObject("Shadow") { layer = target ? target.gameObject.layer : 0 };
                quad.AddComponent<MeshFilter>().sharedMesh = Prim.MeshOf(PrimitiveType.Quad);
                quad.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var hue = quad.GetComponent<Hue>();
            if (hue) Destroy(hue);
            string tex = s switch { 1 => "puddle", 2 => "square", 6 => "star", _ => "soft" };
            Color c = s switch { 3 => new Color(0.2f, 0.9f, 1f, 0.9f), 4 => new Color(0.75f, 0.85f, 1f, 0.45f), 5 => new Color(1, 0.3f, 0.6f, 0.7f), _ => new Color(0, 0, 0, 0.45f) };
            mat = s == 3 ? Mats.Additive(c, Patterns.Shape(tex)) : Mats.UnlitTransparent(c, Patterns.Shape(tex));
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (s == 5) { var h = quad.AddComponent<Hue>(); h.speed = 0.4f; }
            quad.SetActive(s != 7);
        }

        void LateUpdate()
        {
            if (quad == null || target == null || !quad.activeSelf) return;
            if (Physics.Raycast(target.position + Vector3.up * 0.5f, Vector3.down, out var hit, 30f, mask, QueryTriggerInteraction.Ignore))
            {
                float h = hit.distance - 0.5f;
                float k = Mathf.Clamp(1.1f - h * 0.08f, 0.35f, 1.1f) * size;
                quad.transform.position = hit.point + hit.normal * 0.02f;
                quad.transform.rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0, 0, Time.time * (style == 6 ? 40 : 0));
                quad.transform.localScale = Vector3.one * k;
                quad.SetActive(true);
            }
        }

        void OnDestroy() { if (quad) Destroy(quad); }
        void OnDisable() { if (quad) quad.SetActive(false); }
        void OnEnable() { if (quad) quad.SetActive(style != 7); }
    }
}
