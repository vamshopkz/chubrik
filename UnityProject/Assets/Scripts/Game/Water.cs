using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// 22.11 water you can swim in («like in Super Bear Adventure»): a registry of water volumes. A world adds its rivers,
    /// lakes and pools here (a channel along a centre line, or a plain box); the player asks <see cref="At"/> whether he is
    /// in water and swims (see the PlayerCtl part below). v0.31: a river channel has a current (<see cref="FlowAt"/>).
    /// Volumes belong to a world's root — a destroyed or hidden root switches its water off by itself.
    /// </summary>
    public static class Water
    {
        public abstract class Volume
        {
            public Transform owner;
            /// <summary>The water surface (world y).</summary>
            public float surface;
            /// <summary>The footprint seen from above (only x and z are used).</summary>
            public Rect area;
            public bool Live => owner != null && owner.gameObject.activeInHierarchy;
            /// <summary>v0.31: the current here (m/s, world direction); zero for still water.</summary>
            public virtual Vector3 Flow(Vector3 p) => Vector3.zero;
            /// <summary>Is this point (x, z) over the water? <paramref name="bottom"/> = the floor under it.</summary>
            public abstract bool Over(Vector3 p, out float bottom);
        }

        /// <summary>A plain pool: an axis-aligned box, surface on top, flat floor.</summary>
        public class BoxVolume : Volume
        {
            public float floor;
            public override bool Over(Vector3 p, out float bottom) { bottom = floor; return area.Contains(new Vector2(p.x, p.z)); }
        }

        /// <summary>
        /// A river channel: a half-pipe along a centre line. Each sample has its centre (at the rim's height), the
        /// direction to its left bank, the half width and the depth (a semi-ellipse across); ends taper to 0.
        /// </summary>
        public class Channel : Volume
        {
            public readonly List<Vector3> c = new List<Vector3>(), n = new List<Vector3>();
            public readonly List<float> w = new List<float>(), d = new List<float>();
            public int Count => c.Count;
            /// <summary>v0.31: samples before this one are frozen (ice you walk on, no water).</summary>
            public int iceTo;
            /// <summary>v0.31: how fast the river flows along its line (from the first sample to the last), m/s.</summary>
            public float speed;

            public override Vector3 Flow(Vector3 p)
            {
                if (speed <= 0 || !area.Contains(new Vector2(p.x, p.z))) return Vector3.zero;
                Locate(p, out int seg, out _, out float dist, out _, out float hw, out _);
                if (dist >= hw || seg < iceTo) return Vector3.zero;
                var d = c[Mathf.Min(seg + 1, c.Count - 1)] - c[seg]; d.y = 0;
                float k = 1 - Mathf.Pow(dist / Mathf.Max(0.1f, hw), 2);   // fastest in the middle, slow by the banks
                // the wide pool at the end is calm
                return d.normalized * speed * Mathf.Clamp01(k + 0.2f) * Mathf.Clamp01(hw < 3.2f ? 1 : 3.2f / hw);
            }

            public void Add(Vector3 centre, Vector3 left, float halfW, float depth) { c.Add(centre); n.Add(left); w.Add(halfW); d.Add(depth); }

            /// <summary>Call once every sample is in: works out the footprint.</summary>
            public void Seal()
            {
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                for (int i = 0; i < c.Count; i++)
                {
                    float r = w[i] + 0.1f;
                    x0 = Mathf.Min(x0, c[i].x - r); x1 = Mathf.Max(x1, c[i].x + r);
                    z0 = Mathf.Min(z0, c[i].z - r); z1 = Mathf.Max(z1, c[i].z + r);
                }
                area = Rect.MinMaxRect(x0, z0, x1, z1);
            }

            /// <summary>The nearest point of the centre line: its sample index and fraction, the distance to it,
            /// the side (+ = left), the half width and the depth there.</summary>
            public void Locate(Vector3 p, out int seg, out float t, out float dist, out float side, out float halfW, out float depth)
            {
                seg = 0; t = 0; dist = float.MaxValue;
                var q = new Vector2(p.x, p.z);
                for (int i = 0; i < c.Count - 1; i++)
                {
                    var a = new Vector2(c[i].x, c[i].z); var b = new Vector2(c[i + 1].x, c[i + 1].z);
                    var ab = b - a; float l2 = ab.sqrMagnitude;
                    float u = l2 < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(q - a, ab) / l2);
                    float dd = (a + ab * u - q).sqrMagnitude;
                    if (dd < dist) { dist = dd; seg = i; t = u; }
                }
                dist = Mathf.Sqrt(dist);
                int j = Mathf.Min(seg + 1, c.Count - 1);
                halfW = Mathf.Lerp(w[seg], w[j], t); depth = Mathf.Lerp(d[seg], d[j], t);
                var on = Vector3.Lerp(c[seg], c[j], t); var nn = Vector3.Lerp(n[seg], n[j], t);
                side = Mathf.Sign(Vector3.Dot(new Vector3(p.x - on.x, 0, p.z - on.z), nn));
            }

            /// <summary>How far a point is outside the rim (negative: over the water).</summary>
            public float Edge(Vector3 p)
            {
                if (!area.Contains(new Vector2(p.x, p.z)))
                {
                    // far away: a quick, rough answer
                    var cl = new Vector2(Mathf.Clamp(p.x, area.xMin, area.xMax), Mathf.Clamp(p.z, area.yMin, area.yMax));
                    float far = (cl - new Vector2(p.x, p.z)).magnitude;
                    if (far > 0.5f) return far;
                }
                Locate(p, out _, out _, out float dist, out _, out float hw, out _);
                return dist - hw;
            }

            public override bool Over(Vector3 p, out float bottom)
            {
                bottom = 0;
                if (!area.Contains(new Vector2(p.x, p.z))) return false;
                Locate(p, out int seg, out _, out float dist, out _, out float hw, out float dp);
                if (dist >= hw || hw < 0.05f || seg < iceTo) return false;
                float k = dist / hw;
                bottom = c[seg].y - dp * Mathf.Sqrt(Mathf.Max(0, 1 - k * k));
                return true;
            }
        }

        static readonly List<Volume> vols = new List<Volume>();

        public static void Add(Volume v) { if (!vols.Contains(v)) vols.Add(v); }
        public static void Remove(Volume v) => vols.Remove(v);

        /// <summary>A box of water (a pool): <paramref name="centre"/> on the surface.</summary>
        public static BoxVolume AddBox(Transform owner, Vector3 centre, Vector2 size, float depth)
        {
            var v = new BoxVolume { owner = owner, surface = centre.y, floor = centre.y - depth, area = new Rect(centre.x - size.x / 2, centre.z - size.y / 2, size.x, size.y) };
            Add(v);
            return v;
        }

        /// <summary>
        /// Is this point in (or right over) water? surfaceY = the water surface, bottomY = the floor under it.
        /// True when the point is over a water volume, not higher than 1 m over its surface and not deep under its floor.
        /// </summary>
        public static bool At(Vector3 p, out float surfaceY, out float bottomY)
        {
            surfaceY = bottomY = 0;
            for (int i = vols.Count - 1; i >= 0; i--)
            {
                var v = vols[i];
                if (v.owner == null) { vols.RemoveAt(i); continue; }
                if (!v.Live || p.y > v.surface + 1f || !v.Over(p, out float b) || p.y < b - 1.5f) continue;
                surfaceY = v.surface; bottomY = b;
                return true;
            }
            return false;
        }

        /// <summary>v0.31: the current at this point (the river carries a swimmer along it).</summary>
        public static Vector3 FlowAt(Vector3 p)
        {
            foreach (var v in vols)
                if (v.owner != null && v.Live && v.Over(p, out _)) return v.Flow(p);
            return Vector3.zero;
        }

        // ---------------- looks ----------------

        static Texture2D flowTex, ringTex;

        /// <summary>Light blue water with soft streaks, tiling (for the see-through river surface).</summary>
        public static Texture2D FlowTex()
        {
            if (flowTex != null) return flowTex;
            const int N = 128;
            var px = new Color32[N * N];
            Color deep = new Color(0.36f, 0.70f, 0.95f), light = new Color(0.78f, 0.93f, 1f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float nz = Tex.Fbm(x / 32f, y / 32f, 4, 57);
                    float s = Mathf.Sin((y / (float)N) * Mathf.PI * 2 * 3 + nz * 7f) * 0.5f + 0.5f;
                    float hi = Mathf.Pow(s, 6) * 0.8f + nz * 0.25f;
                    px[y * N + x] = Color.Lerp(deep, light, Mathf.Clamp01(hi));
                }
            flowTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 2 };
            flowTex.SetPixels32(px); flowTex.Apply(true, true);
            return flowTex;
        }

        /// <summary>The see-through water material: blue, glassy, both sides (you see the surface from under water too).</summary>
        public static Material SurfaceMat(float alpha = 0.55f)
        {
            var m = Mats.Transparent(new Color(1f, 1f, 1f, alpha));
            m.mainTexture = FlowTex();
            m.SetFloat("_Smoothness", 0.92f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
            return m;
        }

        static Texture2D RingTex()
        {
            if (ringTex != null) return ringTex;
            const int N = 64;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float a = Mathf.Clamp01(1 - Mathf.Abs(d - 0.82f) / 0.12f);
                    px[y * N + x] = new Color(1, 1, 1, a * a);
                }
            ringTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            ringTex.SetPixels32(px); ringTex.Apply(false, true);
            return ringTex;
        }

        // ---------------- ripples on the surface ----------------

        class Ripples : MonoBehaviour
        {
            struct R { public Transform t; public Renderer r; public float age, size; }
            readonly R[] rs = new R[8];
            int next;
            MaterialPropertyBlock mpb;
            static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Color0 = Shader.PropertyToID("_Color");

            void Awake()
            {
                mpb = new MaterialPropertyBlock();
                var m = Mats.UnlitTransparent(Color.white, RingTex());
                for (int i = 0; i < rs.Length; i++)
                {
                    var go = Prim.Make(PrimitiveType.Quad, transform, Vector3.zero, Vector3.one, m, "Ripple", 0);
                    go.transform.rotation = Quaternion.Euler(90, 0, 0);
                    var r = go.GetComponent<Renderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    go.SetActive(false);
                    rs[i] = new R { t = go.transform, r = r, age = 99 };
                }
            }

            public void Spawn(Vector3 p, float size)
            {
                ref var r = ref rs[next]; next = (next + 1) % rs.Length;
                r.age = 0; r.size = size;
                r.t.position = p; r.t.gameObject.SetActive(true);
            }

            void Update()
            {
                for (int i = 0; i < rs.Length; i++)
                {
                    ref var r = ref rs[i];
                    if (r.age > 1.4f) continue;
                    r.age += Time.deltaTime;
                    if (r.age > 1.4f) { r.t.gameObject.SetActive(false); continue; }
                    float k = r.age / 1.4f;
                    float s = Mathf.Lerp(0.5f, 2.4f, 1 - (1 - k) * (1 - k)) * r.size;
                    r.t.localScale = new Vector3(s, s, 1);
                    var col = new Color(0.9f, 0.97f, 1f, 0.6f * (1 - k));
                    mpb.SetColor(BaseColor, col); mpb.SetColor(Color0, col);
                    r.r.SetPropertyBlock(mpb);
                }
            }
        }
        static Ripples ripples;

        /// <summary>A little ring wave spreading on the water surface.</summary>
        public static void Ripple(Vector3 p, float size = 1f)
        {
            if (ripples == null) ripples = new GameObject("WaterRipples").AddComponent<Ripples>();
            ripples.Spawn(p + Vector3.up * 0.015f, size);
        }

        // ---------------- the camera under water ----------------

        static GameObject overlay;
        static bool camUnder, fogWas;
        static FogMode fogModeWas;
        static Color fogColorWas;
        static float fogStartWas, fogEndWas, fogDensityWas;
        static readonly Color UnderFog = new Color(0.40f, 0.74f, 0.93f);
        public static bool CameraUnder => camUnder;
        /// <summary>22.11 soft restart: no water and the camera is dry (the new lobby registers its river again).</summary>
        internal static void ResetStatics() { vols.Clear(); camUnder = false; }

        /// <summary>Every frame after the camera moved: under the surface the screen turns light blue (fog + a see-through
        /// blue sheet in front of the lens); out of the water everything is back as it was.</summary>
        public static void CameraFx(Camera cam)
        {
            var p = cam.transform.position;
            bool now = At(p, out float s, out _) && p.y < s - 0.03f;
            if (now != camUnder)
            {
                camUnder = now;
                if (now)
                {
                    fogWas = RenderSettings.fog; fogModeWas = RenderSettings.fogMode; fogColorWas = RenderSettings.fogColor;
                    fogStartWas = RenderSettings.fogStartDistance; fogEndWas = RenderSettings.fogEndDistance; fogDensityWas = RenderSettings.fogDensity;
                    RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = UnderFog;
                    RenderSettings.fogStartDistance = 0.5f; RenderSettings.fogEndDistance = 24f;
                }
                else if (RenderSettings.fogColor == UnderFog)   // nobody else changed the fog meanwhile (a new world sets its own)
                {
                    RenderSettings.fog = fogWas; RenderSettings.fogMode = fogModeWas; RenderSettings.fogColor = fogColorWas;
                    RenderSettings.fogStartDistance = fogStartWas; RenderSettings.fogEndDistance = fogEndWas; RenderSettings.fogDensity = fogDensityWas;
                }
            }
            if (!now) { if (overlay) overlay.SetActive(false); return; }
            if (overlay == null || overlay.transform.parent != cam.transform)
            {
                if (overlay) Object.Destroy(overlay);
                overlay = Prim.Make(PrimitiveType.Quad, cam.transform, Vector3.zero, Vector3.one, Mats.UnlitTransparent(new Color(0.32f, 0.72f, 1f, 0.3f)), "UnderWaterTint", 0);
                var r = overlay.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.sortingOrder = 100;
            }
            overlay.SetActive(true);
            float z = cam.nearClipPlane + 0.03f;
            float h = 2 * z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.25f;
            overlay.transform.localPosition = new Vector3(0, 0, z);
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = new Vector3(h * Mathf.Max(1f, cam.aspect), h, 1);
        }
    }

    /// <summary>Slides the water texture along the river: it looks like flowing water (nothing is pushed, it is only a look).</summary>
    public class WaterFlow : MonoBehaviour
    {
        public Vector2 speed = new Vector2(-0.09f, 0f);
        Material m;
        void Start() { m = GetComponent<Renderer>().material; }
        void Update() { if (m) m.mainTextureOffset = speed * Time.time; }
    }

    /// <summary>
    /// 22.11 swimming: when the chubrik's chest goes under the water surface he swims — floats with his head and back
    /// out, slower (≈60 %), bobbing; A hops up out of the water (by the bank he climbs out), holding B dives: under water
    /// he swims in 3D (stick = direction, looking down goes deeper, B deeper, A up). No drowning, no hearts lost, no current.
    /// </summary>
    public partial class PlayerCtl
    {
        int swim;            // 0 = no, 1 = on the surface, 2 = under water
        bool swimRise;       // A under water: going up to the surface
        float swimT, rippleT, bubbleT, underIdle, camUnderK;
        public bool Swimming => swim != 0;
        public bool UnderWater => swim == 2;
        public int Swims { get; private set; }
        public int Climbs { get; private set; }
        /// <summary>The surface of the water we swim in.</summary>
        public float SwimSurface { get; private set; }

        /// <summary>How deep the feet hang under the surface while floating (a giant chubrik still fits a 1.9 m river).</summary>
        public float FloatDepth(float move01 = 0) => Mathf.Min(Mathf.Lerp(0.78f, 0.64f, move01) * Model.SizeScale, 1.25f);

        /// <summary>The normal capsule for this size.</summary>
        void BaseCapsule()
        {
            float s = Model.SizeScale;
            CC.radius = 0.38f * Mathf.Clamp(s, 0.6f, 1.6f); CC.height = 1.45f * s; CC.center = new Vector3(0, 0.74f * s, 0);
        }

        /// <summary>Swimming the chubrik lies in the water: a low capsule fits under the surface.</summary>
        void SwimCapsule()
        {
            float h = Mathf.Max(0.8f * Model.SizeScale, CC.radius * 2 + 0.02f);
            CC.height = h; CC.center = new Vector3(0, h / 2 + 0.03f, 0);
        }

        void StartSwim(float surf)
        {
            swim = 1; swimRise = false; swimT = 0; underIdle = 0; Swims++;
            SetDive(0); EndLeap();
            plunge = 0; Anim.plunge = false; attackDownT = -1; superBounce = false;
            if (Anim.emoting) Anim.StopEmote();
            SwimCapsule();
            var p = transform.position;
            float fall = Mathf.Max(0, -vel.y);
            AudioFx.Play("splash", Mathf.Clamp(0.35f + fall / 14f, 0.35f, 1f));
            Effects.Emit("splash", new Vector3(p.x, surf, p.z), fall > 6 ? 34 : 14, 0.45f, Vector3.up * Mathf.Clamp(fall * 0.4f, 1.8f, 5f));
            Water.Ripple(new Vector3(p.x, surf, p.z), 1.5f * Model.SizeScale);
            Anim.swim = 1;
        }

        void EndSwim()
        {
            if (swim == 0) return;
            swim = 0; swimRise = false;
            BaseCapsule();
            if (Anim) { Anim.swim = 0; Anim.swimMove = 0; }
        }

        /// <summary>Called every frame from Update: true when the chubrik is in the water (and this frame is handled here).</summary>
        bool SwimUpdate(float dt, Vector2 mv, bool wantJump)
        {
            if (Cheats.Flying || Riding || Snowmobile) { EndSwim(); return false; }
            float s = Model.SizeScale;
            var pos = transform.position;
            float surf, bottom;
            if (swim == 0)
            {
                if (!Water.At(pos, out surf, out bottom) || pos.y + Mathf.Min(0.85f * s, 1.15f) >= surf) return false;   // the chest under the surface
                StartSwim(surf);
            }
            else if (!Water.At(pos, out surf, out bottom) || pos.y + Mathf.Min(0.3f * s, 0.45f) >= surf) { EndSwim(); return false; }

            SwimSurface = surf;
            swimT += dt;
            bool can = !Frozen && !Menus.AnyOpen;
            bool diveHeld = AttackHeld && can;
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var wish = fwd * mv.y + right * mv.x;
            if (wish.sqrMagnitude > 1) wish.Normalize();
            float speed = RunSpeed * 0.6f * (swim == 2 ? 0.9f : 1f) * Mathf.Lerp(1f, 1.25f, Mathf.InverseLerp(1f, 1.8f, s));
            var hv = new Vector3(vel.x, 0, vel.z);
            var cur = Water.FlowAt(pos);   // v0.31: the river carries you the way it flows (you can still swim against it)
            hv = Vector3.MoveTowards(hv, wish * speed + cur, (wish.sqrMagnitude > 0.01f ? 9f : 6f) * dt);
            if (wish.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish), dt * 8);
                idle = 0;
            }
            float move01 = Mathf.Clamp01(hv.magnitude / Mathf.Max(0.1f, speed));
            // floating: head and back out of the water, a gentle bob
            float floatY = surf - FloatDepth(move01) + Mathf.Sin(swimT * 2.4f) * 0.045f;

            if (swim == 1)
            {
                if (diveHeld)
                {
                    swim = 2; swimRise = false; underIdle = 0;
                    AudioFx.Play("swing", 0.35f);
                    Effects.Emit("splash", new Vector3(pos.x, surf, pos.z), 10, 0.3f, Vector3.up * 2f);
                    Water.Ripple(new Vector3(pos.x, surf, pos.z), 1.2f * s);
                }
                else if (wantJump && can)
                {
                    if (ClimbOut(surf)) return true;
                    // a hop up out of the water — a second jump can follow in the air
                    EndSwim();
                    vel = new Vector3(hv.x, 8.5f, hv.z);
                    Grounded = false; coyote = 0; airJumps = Model.HasJet ? 2 : 1; airJumped = false;
                    Jumps++;
                    AudioFx.Jump(Mathf.Max(0, look.Idx("jsnd")), false, Catalog.PitchOf(look.Of("jsnd")));
                    AudioFx.Play("splash", 0.4f);
                    Effects.Emit("splash", new Vector3(pos.x, surf, pos.z), 16, 0.35f, Vector3.up * 3f);
                    Water.Ripple(new Vector3(pos.x, surf, pos.z), 1.3f * s);
                    CC.Move(vel * dt);
                    Anim.grounded = false; Anim.vy = vel.y;
                    return true;
                }
            }
            if (swim == 1) vel.y = Mathf.MoveTowards(vel.y, Mathf.Clamp((floatY - pos.y) * 5f, -2.5f, 2.5f), 28f * dt);
            else
            {
                if (wantJump && can) swimRise = true;
                bool input = diveHeld || (JumpHeld && can) || wish.sqrMagnitude > 0.01f;
                underIdle = input ? 0 : underIdle + dt;
                if (underIdle > 5f) swimRise = true;   // left alone for a while: back up by himself
                float vy = 0;
                if (diveHeld) { vy = -2.6f; swimRise = false; }
                else if (swimRise || (JumpHeld && can)) vy = 3.2f;
                vy -= Mathf.Clamp((pitch - 12f) / 35f, -1f, 1f) * move01 * speed * 0.7f;   // look down and swim: deeper
                vel.y = Mathf.MoveTowards(vel.y, vy, 14f * dt);
                if (pos.y + vel.y * dt > floatY && !diveHeld)
                {
                    // back at the surface
                    swim = 1; swimRise = false;
                    vel.y = Mathf.Min(vel.y, 1f);
                    AudioFx.Play("splash", 0.3f);
                    Effects.Emit("splash", new Vector3(pos.x, surf, pos.z), 10, 0.3f, Vector3.up * 2.5f);
                    Water.Ripple(new Vector3(pos.x, surf, pos.z), 1.3f * s);
                }
            }

            vel.x = hv.x; vel.z = hv.z;
            ground = null;
            var flags = CC.Move(vel * dt);
            Grounded = false; OnIce = false; coyote = 0;
            if ((flags & CollisionFlags.Below) != 0 && vel.y < 0) vel.y = 0;
            if ((flags & CollisionFlags.Above) != 0 && vel.y > 0) vel.y = 0;

            // ripples round him on the surface, bubbles under water, a little splashing while swimming
            var p2 = transform.position;
            rippleT -= dt;
            if (swim == 1 && rippleT <= 0) { rippleT = move01 > 0.2f ? 0.35f : 0.9f; Water.Ripple(new Vector3(p2.x, surf, p2.z), Mathf.Lerp(0.8f, 1.2f, move01) * s); }
            bubbleT -= dt;
            if (swim == 2 && bubbleT <= 0)
            {
                bubbleT = Random.Range(0.15f, 0.4f);
                var head = p2 + Vector3.up * 0.5f * s + transform.forward * 0.45f * s;
                if (head.y < surf - 0.1f) Effects.Emit("bubble", head, 1, 0.12f, Vector3.up * 0.7f);
            }
            if (swim == 1 && move01 > 0.3f && Random.value < dt * 6) Effects.Emit("splash", new Vector3(p2.x, surf, p2.z) + transform.forward * 0.5f * s, 1, 0.2f, Vector3.up * 1.5f);

            TickInvuln(dt);
            attackCd -= dt;
            Anim.grounded = true; Anim.vy = 0; Anim.speed01 = 0; Anim.gliding = false;
            Anim.swim = swim; Anim.swimMove = move01;
            return true;
        }

        /// <summary>A by the bank: pull up out of the water onto the ground (uses the ledge climb).</summary>
        bool ClimbOut(float surf)
        {
            float s = Model.SizeScale;
            var pos = transform.position;
            var f = transform.forward; f.y = 0;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            f.Normalize();
            var steer = Steer.sqrMagnitude > 0.01f ? (Quaternion.Euler(0, yaw, 0) * new Vector3(Steer.x, 0, Steer.y)).normalized : f;
            float bestScore = float.MaxValue; Vector3 best = default, bestDir = default;
            float r = CC.radius * 0.85f, h = 1.45f * s;
            for (int k = -3; k <= 3; k++)   // up to 90° either side of where he swims
            {
                var dir = Quaternion.Euler(0, k * 30f, 0) * steer;
                foreach (float reach in new[] { 1.2f, 1.6f, 2.0f })
                {
                    float rr = reach * Mathf.Max(1f, s);
                    var from = new Vector3(pos.x, surf + 2.6f * s, pos.z) + dir * rr;
                    if (!Physics.Raycast(from, Vector3.down, out var top, 3.4f * s + 1, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    if (top.normal.y < 0.75f || top.point.y < surf + 0.15f || top.point.y - pos.y > 2.8f * s) continue;
                    if (top.collider.GetComponentInParent<Hurter>() != null || top.collider.GetComponentInParent<BouncePad>() != null) continue;
                    var stand = top.point + Vector3.up * 0.06f;
                    if (Physics.Raycast(pos + Vector3.up * 0.9f * s, Vector3.up, Mathf.Max(0.1f, stand.y + h - pos.y - 0.9f * s), Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;   // under a bridge: no room to pull up
                    if (Physics.Raycast(new Vector3(pos.x, stand.y + 0.5f * s, pos.z), dir, rr, Layers.BlockMask, QueryTriggerInteraction.Ignore)) continue;   // a wall or a gate
                    if (Physics.CheckCapsule(stand + Vector3.up * (r + 0.08f), stand + Vector3.up * (h - r), r, Layers.BlockMask, QueryTriggerInteraction.Ignore)) continue;
                    float score = Mathf.Abs(k) + reach * 0.3f;
                    if (score < bestScore) { bestScore = score; best = stand; bestDir = dir; }
                    break;
                }
            }
            if (bestScore == float.MaxValue) return false;
            EndSwim();
            hang = 1; hangT = 0.12f; Climbs++;
            hangPos = pos; hangTop = best;
            CC.enabled = false;
            transform.rotation = Quaternion.LookRotation(bestDir);
            vel = Vector3.zero;
            EndLeap();
            Anim.hang = true; Anim.grounded = false; Anim.speed01 = 0; Anim.vy = 0;
            AudioFx.Play("jump4", 0.6f); AudioFx.Play("splash", 0.4f);
            Effects.Emit("splash", new Vector3(pos.x, surf, pos.z), 14, 0.35f, Vector3.up * 2.5f);
            Water.Ripple(new Vector3(pos.x, surf, pos.z), 1.4f * s);
            return true;
        }
    }
}
