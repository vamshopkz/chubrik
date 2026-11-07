using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Anything a punch, a weapon or a shot can hurt (bosses, snowmen…). Returns true when it took the hit.</summary>
    public interface IHittable { bool OnHit(Vector3 from); }

    /// <summary>Spikes in a bridge: they pop up and hide again. The Shipozavr crown makes you immune.</summary>
    public class SpikeStrip : MonoBehaviour
    {
        Transform spikes; float phase, cd;
        const float Period = 2.6f;
        public bool Up { get; private set; }

        public void Build(Vector3 p, float yaw, float width, Transform parent, float ph)
        {
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            phase = ph;
            Parts.Layer = Layers.World;
            B(transform, new Vector3(0, 0.01f, 0), new Vector3(width, 0.04f, 1.1f), M("4A3B30", 0.3f));
            spikes = Node("Spikes", transform, Vector3.zero);
            var steel = M("C9D1DB", 0.85f, 0.8f);
            for (int k = 0; k < (int)(width / 0.35f); k++)
                for (int row = 0; row < 2; row++)
                    Spike(spikes, new Vector3(-width / 2 + 0.2f + k * 0.35f, 0, -0.25f + row * 0.5f), new Vector3(-width / 2 + 0.2f + k * 0.35f, 0.5f, -0.25f + row * 0.5f), 0.16f, steel);
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.time / Period + phase, 1);
            bool up = t > 0.55f;
            if (up != Up && Worlds.Current == 2)
            {
                var pl = PlayerCtl.I;
                if (up && pl != null && Vector3.Distance(pl.transform.position, transform.position) < 12) AudioFx.Play("blaster", 0.3f);
            }
            Up = up;
            spikes.localPosition = Vector3.up * Mathf.MoveTowards(spikes.localPosition.y, up ? 0 : -0.55f, Time.deltaTime * 6);
            var p = PlayerCtl.I;
            if (!Up || p == null || Worlds.Current != 2 || p.Frozen || Time.time < cd) return;
            var d = transform.InverseTransformPoint(p.transform.position);
            if (Mathf.Abs(d.z) < 0.7f && Mathf.Abs(d.x) < 1.8f && d.y > -0.3f && d.y < 0.6f)
            {
                cd = Time.time + 1f;
                if (p.SpikeProof) { Effects.Emit("star", p.transform.position + Vector3.up * 0.3f, 6, 0.2f); return; }
                p.HurtBy(transform.position - transform.forward);
            }
        }
    }

    /// <summary>
    /// Шипозавр — the mega boss of the Spike Arena (World 2): 2222 lives. Walks, charges in a straight line, gets
    /// dizzy after a charge (that's when a jump on his head does 222), shoots back spikes all around every third
    /// round. Punches and weapons take 74 any time. At zero he shrinks, runs away and drops the mega treasure.
    /// </summary>
    public class Shipozavr : MonoBehaviour, IHittable
    {
        public static Shipozavr I;
        public const int MaxHp = 2222, Stomp = 222, Punch = 74;
        public int Hp { get; private set; } = MaxHp;
        public bool Defeated { get; private set; }
        public static bool Beaten => Save.P.found.Contains("shipozavr");
        Vector3 home; Transform body, head, jaw, tail; Transform bar; TMPro.TextMeshPro label;
        enum St { Idle, Walk, Aim, Charge, Dizzy, Volley, Flee }
        St st; float t, hurtCd, bounceCd, hitFlash; int rounds; Vector3 chargeDir;
        const float ArenaR = 10f, Height = 2.6f, Radius = 1.4f;
        readonly List<(Transform t, Vector3 dir, float life)> shots = new List<(Transform, Vector3, float)>();

        /// <summary>The arena island with a spiky bridge from the temple, and the boss (once beaten: just a sign).</summary>
        public static void BuildArena(Vector3 c, Transform root)
        {
            var go = new GameObject("Shipozavr");
            go.transform.SetParent(root, false);
            I = go.AddComponent<Shipozavr>();
            I.home = c;
            go.transform.position = c;
            if (Beaten) { I.Defeated = true; I.Hp = 0; Lobby.Sign(c + new Vector3(0, 0, 4), 180, "ШИПОЗАВР ПОБЕЖДЁН!", "4CE35A", 5); return; }
            I.Build();
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            var green = M("4E9A3A", 0.35f); var belly = M("D9D08A", 0.3f); var bone = M("F3EBD2", 0.45f); var dark = M("1E2A18", 0.4f);
            body = Node("Body", transform, Vector3.zero);
            S(body, new Vector3(0, 1.4f, 0), new Vector3(2.2f, 1.7f, 3f), green);
            S(body, new Vector3(0, 1.1f, 0.5f), new Vector3(1.6f, 1.1f, 2f), belly);
            foreach (var sd in new[] { -1, 1 })
            {
                Ca(body, new Vector3(sd * 0.75f, 0.45f, 0.6f), new Vector3(0.6f, 0.5f, 0.6f), green);
                Ca(body, new Vector3(sd * 0.75f, 0.45f, -0.7f), new Vector3(0.6f, 0.5f, 0.6f), green);
                for (int k = 0; k < 3; k++) Spike(body, new Vector3(sd * 0.75f + (k - 1) * 0.12f, 0.05f, 0.9f), new Vector3(sd * 0.75f + (k - 1) * 0.14f, 0.0f, 1.12f), 0.1f, bone);
            }
            // back spikes along the spine (they hurt unless you wear the crown)
            for (int k = 0; k < 9; k++)
            {
                float z = -1.3f + k * 0.32f, y = 2.15f + Mathf.Sin(k / 8f * Mathf.PI) * 0.2f;
                Spike(body, new Vector3(0, y, z), new Vector3(0, y + 0.55f + (k % 2) * 0.2f, z - 0.15f), 0.35f, bone);
            }
            head = Node("Head", body, new Vector3(0, 2.0f, 1.55f));
            S(head, Vector3.zero, new Vector3(1.1f, 0.9f, 1.2f), green);
            jaw = Node("Jaw", head, new Vector3(0, -0.25f, 0.2f));
            S(jaw, new Vector3(0, -0.1f, 0.25f), new Vector3(0.9f, 0.35f, 0.9f), belly);
            for (int k = 0; k < 6; k++) Spike(head, new Vector3(-0.3f + k * 0.12f, -0.2f, 0.62f), new Vector3(-0.3f + k * 0.12f, -0.36f, 0.62f), 0.07f, bone);
            foreach (var sd in new[] { -1, 1 })
            {
                S(head, new Vector3(sd * 0.3f, 0.2f, 0.45f), 0.28f, M("FFFFFF", 0.6f));
                S(head, new Vector3(sd * 0.3f, 0.2f, 0.57f), 0.14f, dark);
                Spike(head, new Vector3(sd * 0.3f, 0.4f, 0.1f), new Vector3(sd * 0.55f, 0.85f, -0.1f), 0.18f, bone);
            }
            tail = Node("Tail", body, new Vector3(0, 1.2f, -1.4f));
            for (int k = 0; k < 5; k++)
            {
                S(tail, new Vector3(0, -k * 0.12f, -k * 0.45f), Vector3.one * (0.9f - k * 0.15f), green);
                Spike(tail, new Vector3(0, 0.3f - k * 0.13f, -k * 0.45f), new Vector3(0, 0.65f - k * 0.15f, -k * 0.45f - 0.1f), 0.22f, bone);
            }
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.center = new Vector3(0, 1.4f, 0); col.radius = 1.4f; col.height = 3.2f;
            // lives bar over the head
            var barRoot = Node("Bar", transform, new Vector3(0, 4.1f, 0));
            barRoot.gameObject.AddComponent<Billboard>();
            B(barRoot, Vector3.zero, new Vector3(3.2f, 0.32f, 0.05f), M("1A1A1A"));
            bar = Node("Fill", barRoot, new Vector3(-1.5f, 0, -0.03f));
            B(bar, new Vector3(0.5f, 0, 0), new Vector3(1f, 0.24f, 0.04f), G("E53935", 1.6f));
            bar.localScale = new Vector3(3f, 1, 1);
            label = Prim.Text(barRoot, "", new Vector3(0, 0.5f, 0), 3.4f, Hex("FFE14D"));
            foreach (var r in GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        bool PlayerIn(out PlayerCtl p)
        {
            p = PlayerCtl.I;
            if (p == null || Worlds.Current != 2 || p.Frozen) return false;
            var d = p.transform.position - home; d.y = 0;
            return d.magnitude < ArenaR + 0.5f && Mathf.Abs(p.transform.position.y - home.y) < 6;
        }

        void Update()
        {
            UpdateShots();
            if (body == null) return;
            if (label) label.text = $"ШИПОЗАВР  {Hp} / {MaxHp}";
            if (bar) bar.localScale = new Vector3(3f * Hp / MaxHp, 1, 1);
            hitFlash = Mathf.MoveTowards(hitFlash, 0, Time.deltaTime * 4);
            body.localScale = Vector3.one * (1 + hitFlash * 0.08f) * (st == St.Flee ? Mathf.Max(0.12f, 1 - t * 2) : 1);
            if (Defeated && st != St.Flee) return;
            float dt = Time.deltaTime;
            t += dt;
            bool active = PlayerIn(out var p);
            tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 3) * 18, 0);
            switch (st)
            {
                case St.Idle:
                    if (active) { st = St.Walk; t = 0; AudioFx.Play("hurt", 0.6f); Hud.Toast("ШИПОЗАВР! Когда он врежется и закружится — прыгай ему на голову!", UIKit.Orange); }
                    break;
                case St.Walk:
                {
                    if (!active) { st = St.Idle; break; }
                    Face(p.transform.position, 3);
                    Step(1.6f);
                    body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 6)) * 0.08f;
                    if (t > 2f) { rounds++; st = rounds % 3 == 0 ? St.Volley : St.Aim; t = 0; }
                    break;
                }
                case St.Aim:
                    Face(p.transform.position, 8);
                    jaw.localRotation = Quaternion.Euler(25, 0, 0);
                    body.localRotation = Quaternion.Euler(-5, 0, Mathf.Sin(Time.time * 40) * 2);
                    if (t > 0.7f) { st = St.Charge; t = 0; chargeDir = transform.forward; jaw.localRotation = Quaternion.identity; body.localRotation = Quaternion.identity; AudioFx.Play("jump7"); }
                    break;
                case St.Charge:
                {
                    var np = transform.position + chargeDir * 10 * dt;
                    var off = np - home; off.y = 0;
                    if (off.magnitude > ArenaR - 1.6f || t > 1.4f) { st = St.Dizzy; t = 0; AudioFx.Play("bonk"); Effects.Emit("dust", transform.position + chargeDir * 1.5f, 30, 0.6f); break; }
                    transform.position = np;
                    if (Time.frameCount % 3 == 0) Effects.Emit("dust", transform.position, 3, 0.4f);
                    break;
                }
                case St.Dizzy:
                    head.localRotation = Quaternion.Euler(30, Mathf.Sin(Time.time * 8) * 15, 0);
                    body.localPosition = Vector3.down * 0.35f;
                    if (Time.frameCount % 10 == 0) Effects.Emit("star", head.position + Vector3.up * 0.8f, 2, 0.3f);
                    if (t > 2.8f) { st = St.Walk; t = 0; head.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
                    break;
                case St.Volley:
                    body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30) * 3);
                    if (t > 0.6f)
                    {
                        Volley();
                        st = St.Walk; t = 0; body.localRotation = Quaternion.identity;
                    }
                    break;
                case St.Flee:
                    transform.position += transform.forward * 8 * dt;
                    if (t > 1.6f) { body.gameObject.SetActive(false); st = St.Idle; enabled = shots.Count > 0; }
                    return;
            }
            if (!active || p == null) return;
            // contact: head stomp while dizzy, back spikes otherwise, side touch hurts
            var d = p.transform.position - transform.position;
            float horiz = new Vector2(d.x, d.z).magnitude;
            if (horiz < Radius + 0.5f && d.y > Height * 0.7f && d.y < Height + 2f && p.Velocity.y < 1f && Time.time > bounceCd)
            {
                bounceCd = Time.time + 0.5f;
                if (st == St.Dizzy) { p.Launch(13f); Damage(Stomp); }
                else if (p.SpikeProof) { p.Launch(10f); Damage(50); }
                else { p.Launch(9f); p.HurtBy(transform.position); Hud.Toast("Ой, шипы на спине! Прыгай, когда он кружится после удара", UIKit.Orange); }
            }
            else if (horiz < Radius + 0.4f && d.y < Height * 0.7f && Time.time > hurtCd && st != St.Dizzy)
            {
                hurtCd = Time.time + 1.1f;
                p.HurtBy(transform.position);
            }
        }

        void Face(Vector3 at, float speed)
        {
            var to = at - transform.position; to.y = 0;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * speed);
        }

        void Step(float speed)
        {
            var np = transform.position + transform.forward * speed * Time.deltaTime;
            var off = np - home; off.y = 0;
            if (off.magnitude < ArenaR - 2) transform.position = np;
        }

        void Volley()
        {
            AudioFx.Play("zap");
            Parts.Layer = Layers.World;
            for (int k = 0; k < 10; k++)
            {
                float a = k * Mathf.PI * 2 / 10 + rounds;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var s = Spike(transform.parent, transform.position + Vector3.up * 0.6f + dir * 1.6f, transform.position + Vector3.up * 0.6f + dir * 2.4f, 0.25f, M("F3EBD2", 0.45f));
                shots.Add((s.transform, dir, 2.2f));
            }
        }

        void UpdateShots()
        {
            var p = PlayerCtl.I;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var (tr, dir, life) = shots[i];
                life -= Time.deltaTime;
                tr.position += dir * 9 * Time.deltaTime;
                shots[i] = (tr, dir, life);
                bool hit = p != null && Worlds.Current == 2 && !p.Frozen && Vector3.Distance(p.transform.position + Vector3.up * 0.5f, tr.position) < 0.7f;
                if (hit && !p.SpikeProof) p.HurtBy(tr.position);
                if (life <= 0 || hit) { Destroy(tr.gameObject); shots.RemoveAt(i); }
            }
        }

        public bool OnHit(Vector3 from)
        {
            if (Defeated || body == null || st == St.Charge) return false;
            Damage(Punch);
            return true;
        }

        public void Damage(int n)
        {
            if (Defeated) return;
            Hp = Mathf.Max(0, Hp - n);
            hitFlash = 1;
            AudioFx.Play("crate");
            Effects.Emit("star", transform.position + Vector3.up * 2.5f, 10, 0.4f);
            Effects.Float("-" + n, transform.position + Vector3.up * 3.6f, UIKit.Red);
            if (Hp <= 0) Die();
        }

        /// <summary>Tests: knock off this many lives.</summary>
        public void TestDamage(int n) => Damage(n);

        void Die()
        {
            Defeated = true;
            st = St.Flee; t = 0;
            AudioFx.Play("hurt"); AudioFx.Play("win5");
            transform.rotation = Quaternion.LookRotation((transform.position - home).sqrMagnitude > 1 ? (transform.position - home).normalized : Vector3.right);
            transform.Find("Bar")?.gameObject.SetActive(false);
            MegaReward(home + Vector3.up * 0.4f);
        }

        /// <summary>The treasure: 2222 coins in a fountain, 22 tokens, a golden key and the Shipozavr crown.</summary>
        static void MegaReward(Vector3 c)
        {
            if (!Save.P.found.Contains("shipozavr")) Save.P.found.Add("shipozavr");
            Save.P.coins += 2222; Save.P.tokens += 22; Save.P.keys += 1;
            Save.Give(Catalog.ShipoCrown);
            Save.Write();
            Parts.Layer = Layers.World;
            var chest = Lobby.Box(c + Vector3.up * 0.5f, new Vector3(1.6f, 1f, 1.1f), M("C9A14A", 0.6f, 0.5f), 0);
            B(chest.transform, new Vector3(0, 0.55f, 0), new Vector3(1.05f, 0.18f, 1.05f), M("FFC83A", 0.9f, 0.85f));
            chest.transform.SetParent(I ? I.transform.parent : null, true);
            for (int k = 0; k < 6; k++) Effects.Emit("coin", c + Vector3.up * (1 + k * 0.4f), 60, 1.2f, Vector3.up * (6 + k));
            Effects.Emit("magic", c + Vector3.up * 2, 80, 1f, Vector3.up * 5);
            Effects.Emit("confetti", c + Vector3.up * 3, 160, 0.8f, Vector3.up * 6);
            AudioFx.Play("token"); AudioFx.Play("buy");
            Window.Show("МЕГА-НАГРАДА!", w =>
            {
                w.Text("Шипозавр громко рычит, уменьшается до крошечного размера и убегает! На платформу падает сокровище:", 28);
                w.Text("2222 золотые монеты · 22 фиолетовых жетона · Золотой Ключ · Корона Шипозавра", 30, UIKit.Gold);
                w.Text("Корона Шипозавра защищает от обычных шипов на мостиках — надень её в гардеробе.", 24, UIKit.Muted);
                w.Row("Ура!", null, "ЗАБРАТЬ", UIKit.Green, Window.Close);
            }, 1060, 600);
        }
    }
}
