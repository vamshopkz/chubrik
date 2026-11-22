using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 17.11: little birds hop about pecking the ground; come close and they flutter up into the sky.
    /// A while later they land back where they were.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        public static readonly List<Bird> All = new List<Bird>();
        public bool Flying { get; private set; }
        public int Flights { get; private set; }
        Vector3 home, vel;
        Transform wingL, wingR, head;
        float t, hopT, backAt, phase;
        Rng rng;

        static readonly HashSet<int> worlds = new HashSet<int>();
        /// <summary>22.11 soft restart: the flocks are made again with the lobby.</summary>
        internal static void ResetStatics() { worlds.Clear(); All.Clear(); }

        /// <summary>Flocks in the lobby and the green worlds (once per world).</summary>
        public static void ForWorld(int w)
        {
            if (!worlds.Add(w)) return;
            switch (w)
            {
                case 1:
                    foreach (var (c, k) in new[] { (new Vector3(4, 0, -3), 1), (new Vector3(2, 0, 30), 2), (new Vector3(20, 0, -12), 3), (new Vector3(-30, 0, -12), 4), (new Vector3(25, 0, 28), 5), (new Vector3(-2, 0, -14), 6) })
                        Flock(Lobby.root, c, 5, 1.6f, k);
                    break;
                case 2: Flock(World2.Root, World2.Spawn + new Vector3(2, 0, 6), 5, 1.5f, 21); break;
                case 4: Flock(World4.Root, World4.Spawn + new Vector3(-2, 0, 7), 4, 1.5f, 41); break;
                case 5: Flock(World5.Root, World5.Spawn + new Vector3(2, 0, 7), 6, 1.6f, 51); break;
            }
        }

        /// <summary>A small flock round a spot (each bird stands on the ground found under it).</summary>
        public static void Flock(Transform parent, Vector3 c, int n, float r, int seed)
        {
            var rg = new Rng(seed);
            string[] cols = { "8D6E63", "FF7043", "42A5F5", "FFCA28", "78909C", "EF5350" };
            Physics.SyncTransforms();
            for (int i = 0; i < n; i++)
            {
                var p = c + new Vector3(rg.F(-r, r), 3f, rg.F(-r, r));
                if (!Physics.Raycast(p, Vector3.down, out var hit, 8f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                var go = new GameObject("Bird") { layer = Layers.World };
                go.transform.SetParent(parent, true);
                go.transform.position = hit.point;
                go.transform.rotation = Quaternion.Euler(0, rg.F(0, 360), 0);
                var b = go.AddComponent<Bird>();
                b.Build(cols[rg.I(cols.Length)], seed * 31 + i);
            }
        }

        void Build(string hex, int seed)
        {
            rng = new Rng(seed);
            home = transform.position;
            phase = rng.F(0, 10);
            Parts.Layer = Layers.World;
            var body = M(hex, 0.3f); var belly = M("F5EBDC", 0.25f); var beak = M("FFB300", 0.4f); var eye = M("111111", 0.9f);
            var k = Node("Body", transform, new Vector3(0, 0.12f, 0));
            S(k, Vector3.zero, new Vector3(0.18f, 0.16f, 0.24f), body);
            S(k, new Vector3(0, -0.03f, 0.04f), new Vector3(0.14f, 0.11f, 0.16f), belly);
            head = Node("Head", k, new Vector3(0, 0.08f, 0.09f));
            S(head, Vector3.zero, 0.13f, body);
            Cone(head, new Vector3(0, 0, 0.06f), new Vector3(0.04f, 0.06f, 0.04f), beak, new Vector3(90, 0, 0));
            foreach (var sd in new[] { -1f, 1f }) S(head, new Vector3(sd * 0.045f, 0.02f, 0.04f), 0.025f, eye);
            B(k, new Vector3(0, 0.02f, -0.15f), new Vector3(0.08f, 0.02f, 0.1f), body, new Vector3(-20, 0, 0));      // tail
            wingL = Node("WingL", k, new Vector3(-0.08f, 0.03f, 0));
            wingR = Node("WingR", k, new Vector3(0.08f, 0.03f, 0));
            S(wingL, new Vector3(-0.03f, 0, -0.02f), new Vector3(0.05f, 0.1f, 0.16f), body);
            S(wingR, new Vector3(0.03f, 0, -0.02f), new Vector3(0.05f, 0.1f, 0.16f), body);
            foreach (var sd in new[] { -1f, 1f }) Rod(transform, new Vector3(sd * 0.03f, 0.06f, 0), new Vector3(sd * 0.03f, 0, 0.01f), 0.012f, beak);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            All.Add(this);
        }

        void OnDestroy() => All.Remove(this);

        /// <summary>Scare the bird (the player came close): up into the sky.</summary>
        public void FlyAway(Vector3 from)
        {
            if (Flying) return;
            Flying = true; Flights++;
            var away = transform.position - from; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            vel = away.normalized * rng.F(2.5f, 4f) + Vector3.up * rng.F(4f, 6f);
            transform.rotation = Quaternion.LookRotation(new Vector3(vel.x, 0, vel.z));
            if (rng.Chance(0.5f)) AudioFx.Play("flutter", 0.5f);
            backAt = Time.time + rng.F(20f, 35f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            var pl = PlayerCtl.I;
            if (Flying)
            {
                // flap hard, climb and drift away; land back home after a while
                float f = Mathf.Sin(t * 40) * 70;
                wingL.localRotation = Quaternion.Euler(0, 0, f); wingR.localRotation = Quaternion.Euler(0, 0, -f);
                vel += Vector3.up * 2f * dt;
                transform.position += vel * dt;
                if (transform.position.y > home.y + 45) { foreach (Transform c in transform) c.gameObject.SetActive(false); }
                if (Time.time > backAt && (pl == null || (pl.transform.position - home).sqrMagnitude > 64))
                {
                    Flying = false;
                    transform.position = home;
                    foreach (Transform c in transform) c.gameObject.SetActive(true);
                }
                return;
            }
            if (pl != null && (pl.transform.position - transform.position).sqrMagnitude < 4.5f * 4.5f)
            {
                // the whole flock goes when one is scared
                foreach (var b in All) if (b != null && (b.home - home).sqrMagnitude < 36) b.FlyAway(pl.transform.position);
                return;
            }
            // pecking and little hops
            float peck = Mathf.Max(0, Mathf.Sin(t * 6 + phase));
            head.localRotation = Quaternion.Euler(peck * 50, 0, 0);
            wingL.localRotation = wingR.localRotation = Quaternion.identity;
            if ((hopT -= dt) < 0)
            {
                hopT = rng.F(1.2f, 3f);
                transform.Rotate(0, rng.F(-90, 90), 0);
                var next = transform.position + transform.forward * 0.25f;
                if ((next - home).sqrMagnitude < 1.5f) transform.position = next;
            }
        }
    }
}
