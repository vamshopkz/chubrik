using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Coins, big coins and hidden tokens; coins come back after a while so a world never runs dry.</summary>
    public class Pickups : MonoBehaviour
    {
        public static Pickups I;
        class P { public GameObject go; public Vector3 pos; public int value; public string token; public bool key, crystal; public float back; public bool active = true; }
        readonly List<P> items = new List<P>();
        GameObject arrow;
        public int CoinsAlive { get { int n = 0; foreach (var p in items) if (p.active && p.token == null) n++; return n; } }
        public int CoinsTotal { get { int n = 0; foreach (var p in items) if (p.token == null) n++; return n; } }
        public Vector3? CoinNear(Vector3 c) { Vector3? b = null; float bd = float.MaxValue; foreach (var p in items) if (p.token == null && p.active && Vector3.Distance(p.pos, c) < bd) { bd = Vector3.Distance(p.pos, c); b = p.pos; } return b; }
        public int CoinsWithin(Vector3 c, float r) { int n = 0; foreach (var p in items) if (p.token == null && Vector3.Distance(p.pos, c) < r) n++; return n; }
        public float NearestCoinDist { get; private set; }
        public Vector3? NearestCoin { get; private set; }

        public static void Create(Transform parent)
        {
            I = new GameObject("Pickups").AddComponent<Pickups>();
            I.transform.SetParent(parent, false);
        }

        public void Coin(Vector3 pos, int value = 1)
        {
            Parts.Layer = Layers.Pickup;
            var go = new GameObject(value > 1 ? "BigCoin" : "Coin") { layer = Layers.Pickup };
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            float d = value > 1 ? 0.75f : 0.45f;
            var face = Parts.Cy(go.transform, Vector3.zero, new Vector3(d, 0.04f, d), Parts.M("FFC83A", 0.9f, 0.85f), new Vector3(90, 0, 0));
            Parts.Cy(go.transform, Vector3.zero, new Vector3(d * 0.7f, 0.05f, d * 0.7f), Parts.M("FFB300", 0.9f, 0.85f), new Vector3(90, 0, 0));
            if (value > 1) Parts.Msh(Meshes.Star(), go.transform, Vector3.zero, new Vector3(0.35f, 0.35f, 0.14f), Parts.M("FFF3C4", 0.9f, 0.6f));
            go.AddComponent<Spin>().speed = 150;
            items.Add(new P { go = go, pos = pos, value = value });
        }

        /// <summary>Hidden token: picked up once forever (id saved in the profile).</summary>
        public void Token(string id, Vector3 pos)
        {
            if (Save.P.found.Contains(id)) return;
            Parts.Layer = Layers.Pickup;
            var go = new GameObject("Token " + id) { layer = Layers.Pickup };
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var star = Parts.Msh(Meshes.Star(), go.transform, Vector3.zero, new Vector3(0.8f, 0.8f, 0.25f), Parts.G("B57CFF", 1.6f));
            Parts.Ring(go.transform, Vector3.zero, 0.5f, 0.04f, Parts.G("FFFFFF", 1.5f), new Vector3(90, 0, 0));
            Parts.S(go.transform, Vector3.zero, 1.3f, Parts.Add("B57CFF", 0.15f));
            go.AddComponent<Spin>().speed = 90;
            go.AddComponent<Bob>().amp = 0.15f;
            items.Add(new P { go = go, pos = pos, token = id });
        }

        /// <summary>Golden key: opens one cage with a captured friend (taken once forever).</summary>
        public void Key(string id, Vector3 pos)
        {
            if (Save.P.found.Contains(id)) return;
            Parts.Layer = Layers.Pickup;
            var go = new GameObject("Key " + id) { layer = Layers.Pickup };
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            KeyModel.Build(go.transform);
            go.AddComponent<Spin>().speed = 120;
            go.AddComponent<Bob>().amp = 0.15f;
            items.Add(new P { go = go, pos = pos, token = id, key = true });
        }

        /// <summary>Rare crystal of World 3 (taken once forever): powers the Guardian's gate.</summary>
        public void Crystal(string id, Vector3 pos)
        {
            if (Save.P.found.Contains(id)) return;
            Parts.Layer = Layers.Pickup;
            var go = new GameObject("Crystal " + id) { layer = Layers.Pickup };
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var m = Parts.G(id.GetHashCode() % 2 == 0 ? "6FA8FF" : "B57CFF", 1.8f);
            Parts.Msh(Meshes.Cone(6), go.transform, Vector3.zero, new Vector3(0.36f, 0.42f, 0.36f), m);
            Parts.Msh(Meshes.Cone(6), go.transform, Vector3.zero, new Vector3(0.36f, 0.3f, 0.36f), m, new Vector3(180, 0, 0));
            Parts.Ls(go.transform, Vector3.zero, 0.9f, Parts.Add("BFE8FF", 0.15f));
            go.AddComponent<Spin>().speed = 100;
            go.AddComponent<Bob>().amp = 0.12f;
            items.Add(new P { go = go, pos = pos, token = id, crystal = true });
        }

        public int KeysLeft { get { int n = 0; foreach (var p in items) if (p.key && p.active) n++; return n; } }
        public int TokensLeft { get { int n = 0; foreach (var p in items) if (p.token != null && !p.key && !p.crystal && p.active) n++; return n; } }
        public Vector3? CrystalPos() { foreach (var p in items) if (p.crystal && p.active) return p.pos; return null; }
        public int KeysLeftIn(string prefix) { int n = 0; foreach (var p in items) if (p.key && p.active && p.token.StartsWith(prefix)) n++; return n; }
        public int CrystalsLeft { get { int n = 0; foreach (var p in items) if (p.crystal && p.active) n++; return n; } }
        public IEnumerable<Vector3> TokenSpots() { foreach (var p in items) if (p.token != null && p.active) yield return p.pos; }

        void Update()
        {
            var pl = PlayerCtl.I;
            if (pl == null || pl.MenuOrbit) { if (arrow) arrow.SetActive(false); return; }
            var c = pl.transform.position + Vector3.up * 0.7f * pl.Model.SizeScale;
            float reach = 1.0f + 0.4f * pl.Model.SizeScale;
            float best = float.MaxValue; Vector3? bestPos = null;
            foreach (var p in items)
            {
                if (!p.active)
                {
                    if (p.token == null && Time.time > p.back) { p.active = true; p.go.SetActive(true); Effects.Emit("gold", p.pos, 4, 0.2f); }
                    continue;
                }
                float d = Vector3.Distance(c, p.pos);
                if (d < reach) { Collect(p); continue; }
                if (p.token == null && d < best) { best = d; bestPos = p.pos; }
            }
            // the navigator also leads to whole crates (coins, tokens, secret loot boxes inside)
            foreach (var b in Breakable.All)
            {
                if (b == null || !b.Intact || !b.HasLoot) continue;
                float d = Vector3.Distance(c, b.transform.position);
                if (d < best) { best = d; bestPos = b.transform.position + Vector3.up * 0.6f; }
            }
            NearestCoin = bestPos; NearestCoinDist = best;

            // coin navigator
            bool nav = Save.P.navigator && Save.P.navigatorOn && bestPos.HasValue;
            if (nav && arrow == null) MakeArrow();
            if (arrow)
            {
                arrow.SetActive(nav);
                if (nav)
                {
                    arrow.transform.position = pl.transform.position + Vector3.up * (2.1f * pl.Model.SizeScale + 0.2f);
                    var dir = bestPos.Value - arrow.transform.position;
                    if (dir.sqrMagnitude > 0.01f) arrow.transform.rotation = Quaternion.Slerp(arrow.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8);
                }
            }
        }

        void MakeArrow()
        {
            Parts.Layer = Layers.Pickup;
            arrow = new GameObject("Navigator");
            var m = Parts.G("3DFF6E", 1.5f);
            Parts.Cy(arrow.transform, new Vector3(0, 0, -0.1f), new Vector3(0.12f, 0.25f, 0.12f), m, new Vector3(90, 0, 0));
            Parts.Cone(arrow.transform, new Vector3(0, 0, 0.15f), new Vector3(0.32f, 0.35f, 0.32f), m, new Vector3(90, 0, 0));
        }

        void Collect(P p)
        {
            p.active = false;
            p.go.SetActive(false);
            if (p.key)
            {
                Save.P.found.Add(p.token);
                Save.P.keys++;
                Save.Write();
                AudioFx.Play("token");
                Effects.Emit("gold", p.pos, 30, 0.5f);
                Hud.Toast($"Золотой ключ! Открой им клетку с другом (ключей: {Save.P.keys})", UIKit.Gold);
                return;
            }
            if (p.crystal)
            {
                Save.P.found.Add(p.token);
                Save.P.coins += 2;
                Save.Write();
                AudioFx.Play("note" + (World3.Crystals() % 8));
                Effects.Emit("magic", p.pos, 24, 0.4f);
                int n = World3.Crystals();
                Hud.Toast(n < World3.GateNeed ? $"Кристалл! {n} из {World3.GateNeed} для Врат Стража" : $"Кристаллов: {n} из {World3.CrystalsTotal}", UIKit.Cyan);
                return;
            }
            if (p.token != null)
            {
                Save.P.found.Add(p.token);
                Save.P.tokens++;
                Save.Write();
                AudioFx.Play("token");
                Effects.Emit("magic", p.pos, 30, 0.5f);
                PlayerCtl.I.PlayVictory();
                Hud.Toast($"Жетон найден! Ещё спрятано: {TokensLeft}", UIKit.Violet);
                return;
            }
            p.back = Time.time + (p.value > 1 ? 90f : 45f);
            Save.P.coins += p.value;
            Save.Write();
            AudioFx.Play("coin", p.value > 1 ? 1f : 0.7f);
            Effects.Emit("gold", p.pos, p.value > 1 ? 14 : 5, 0.2f);
            if (p.value > 1) Effects.Float("+" + p.value, p.pos + Vector3.up * 0.4f, UIKit.Gold);
        }

        /// <summary>Test helper: collect the nearest coin regardless of distance.</summary>
        public bool CollectNearestForTest()
        {
            foreach (var p in items) if (p.active && p.token == null) { Collect(p); return true; }
            return false;
        }
    }

    /// <summary>Places you can enter (shop, wardrobe, premium kiosk): the HUD shows a button while you stand there.</summary>
    public static class Zones
    {
        public class Z { public Vector3 pos; public float r; public string label; public Action act; public Func<bool> active; }
        public static readonly List<Z> All = new List<Z>();
        public static void Add(Vector3 pos, float r, string label, Action act, Func<bool> active = null) => All.Add(new Z { pos = pos, r = r, label = label, act = act, active = active });

        public static Z Near()
        {
            var p = PlayerCtl.I;
            if (p == null || p.MenuOrbit) return null;
            foreach (var z in All)
            {
                if (z.active != null && !z.active()) continue;
                var d = p.transform.position - z.pos; d.y = 0;
                if (d.magnitude < z.r && Mathf.Abs(p.transform.position.y - z.pos.y) < 3) return z;
            }
            return null;
        }

        public static bool Interact()
        {
            var z = Near();
            if (z == null) return false;
            z.act();
            return true;
        }
    }

    /// <summary>Trampoline mushroom / spring pad.</summary>
    public class BouncePad : MonoBehaviour
    {
        public float power = 19f;
        public Transform top;
        float squash;

        public void Bounce(PlayerCtl p)
        {
            p.Launch(power);
            squash = 1;
            AudioFx.Play("boing");
            Effects.Emit("stardust", transform.position + Vector3.up * 0.6f, 8, 0.6f);
        }

        void Update()
        {
            if (top == null) return;
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 3);
            float k = 1 - Mathf.Sin(squash * Mathf.PI) * 0.35f;
            top.localScale = new Vector3(2 - k, k, 2 - k);
        }
    }

    /// <summary>Musical floor tile: plays its note when stepped on.</summary>
    public class NoteTile : MonoBehaviour
    {
        public int note;
        public Renderer rend;
        static readonly string[] Names = { "ДО", "РЕ", "МИ", "ФА", "СОЛЬ", "ЛЯ", "СИ", "ДО!" };
        public Color baseColor;
        float lit, cd;

        float lastTouch = -1;

        public void Press()
        {
            bool fresh = Time.time - lastTouch > 0.25f;   // a new step, not standing still on it
            lastTouch = Time.time;
            if (!fresh || cd > 0) return;
            cd = 0.35f;
            lit = 1;
            AudioFx.Note(note);
            Effects.Float(Names[note % Names.Length], transform.position + Vector3.up * 1.2f, baseColor, 4);
        }

        void Update()
        {
            cd -= Time.deltaTime;
            lit = Mathf.MoveTowards(lit, 0, Time.deltaTime * 2);
            if (rend) rend.material.SetColor("_EmissionColor", baseColor * lit * 2.5f);
            transform.localPosition = new Vector3(transform.localPosition.x, -lit * 0.06f + 0.1f, transform.localPosition.z);
        }
    }

    /// <summary>Training cactus: bumps you back and shows your damage effect.</summary>
    public class Hurter : MonoBehaviour
    {
        float cd;
        public void Touch(PlayerCtl p)
        {
            if (Time.time < cd) return;
            cd = Time.time + 0.8f;
            p.Hurt(transform.position);
        }
    }

    /// <summary>Moving platform: back and forth between two points, or turning around.</summary>
    public class Mover : MonoBehaviour
    {
        public Vector3 a, b;
        public float period = 4f, spin, phase;
        void Update()
        {
            if (a != b)
            {
                float t = (Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2) + 1) / 2;
                transform.position = Vector3.Lerp(a, b, t);
            }
            if (spin != 0) transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        }
    }

    /// <summary>Crate / target dummy: breaks from any weapon hit, drops coins, comes back later.</summary>
    public class Breakable : MonoBehaviour
    {
        public int coins = 3;
        public bool dummy;
        public string token;       // purple token inside (once)
        public string lootbox;     // golden crate: a free loot box (once, id saved)
        float back = -1;
        Renderer[] rs; Collider[] cs;
        public static readonly List<Breakable> All = new List<Breakable>();
        /// <summary>Whole crate standing there (the coin navigator also leads to these).</summary>
        public bool Intact => back < 0 && !dummy && isActiveAndEnabled;
        /// <summary>Something inside worth the navigator's attention (lobby training crates are empty).</summary>
        public bool HasLoot => coins > 0 || (!string.IsNullOrEmpty(token) && !Save.P.found.Contains(token)) || (!string.IsNullOrEmpty(lootbox) && !Save.P.found.Contains(lootbox));

        void Awake() { rs = GetComponentsInChildren<Renderer>(); cs = GetComponentsInChildren<Collider>(); All.Add(this); }
        void OnDestroy() { All.Remove(this); }

        public void Break()
        {
            if (back > 0) return;
            if (!string.IsNullOrEmpty(token) && !Save.P.found.Contains(token)) Pickups.I.Token(token, transform.position + Vector3.up * 1.2f);
            if (!string.IsNullOrEmpty(lootbox) && !Save.P.found.Contains(lootbox))
            {
                Save.P.found.Add(lootbox);
                ShopUI.FreeBox();
            }
            AudioFx.Play(dummy ? "bonk" : "crate");
            Effects.Emit(dummy ? "star" : "dust", transform.position + Vector3.up * 0.5f, 14, 0.4f);
            if (coins > 0)
            {
                Effects.Emit("coin", transform.position + Vector3.up * 0.6f, coins * 2, 0.3f, Vector3.up * 2);
                Save.AddCoins(coins);
                Effects.Float("+" + coins, transform.position + Vector3.up * 1.4f, UIKit.Gold);
            }
            if (dummy) { StartCoroutine(Wobble()); back = Time.time + 1f; return; }
            foreach (var r in rs) r.enabled = false;
            foreach (var c in cs) c.enabled = false;
            back = Time.time + 25f;
        }

        System.Collections.IEnumerator Wobble()
        {
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 30) * 25 * (1 - t / 0.8f), 0, 0);
                yield return null;
            }
            transform.localRotation = Quaternion.identity;
        }

        void Update()
        {
            if (back > 0 && Time.time > back)
            {
                back = -1;
                foreach (var r in rs) r.enabled = true;
                foreach (var c in cs) c.enabled = true;
            }
        }
    }

    /// <summary>Shots of ranged tools: blaster bolt, magic orb, slingshot pellet, boomerang (comes back).</summary>
    public class Projectile : MonoBehaviour
    {
        int kind; Vector3 dir; Transform owner; float t; Vector3 start;

        public static void Fire(int kind, Vector3 pos, Vector3 dir, Transform owner)
        {
            Parts.Layer = 0;
            var go = new GameObject("Shot");
            go.transform.position = pos;
            var p = go.AddComponent<Projectile>();
            p.kind = kind; p.dir = dir.normalized; p.owner = owner; p.start = pos;
            switch (kind)
            {
                case 0: Parts.Cy(go.transform, Vector3.zero, new Vector3(0.12f, 0.3f, 0.12f), Parts.G("35E0FF", 3f), new Vector3(90, 0, 0)); break;
                case 4: Parts.B(go.transform, Vector3.zero, Vector3.one * 0.14f, Parts.M("FF3FA4")); break;
                case 5:
                {
                    var m = PlayerCtl.I.Model.WeaponMat("FF7A1E", 0.4f, 0);
                    var b = Parts.Msh(Meshes.Fan("boom", new[] { new Vector2(-0.3f, 0.05f), new Vector2(0, 0.2f), new Vector2(0.3f, 0.05f), new Vector2(0.28f, -0.03f), new Vector2(0, 0.1f), new Vector2(-0.28f, -0.03f) }, 0.04f), go.transform, Vector3.zero, Vector3.one, m, new Vector3(90, 0, 0));
                    var sp = go.AddComponent<Spin>(); sp.speed = 900;
                    if (PlayerCtl.I.Model.Weapon) PlayerCtl.I.Model.Weapon.gameObject.SetActive(false);
                    break;
                }
                case 101: Parts.Msh(Meshes.Star(6), go.transform, Vector3.zero, Vector3.one * 0.35f, Parts.G("9FE8FF", 2.5f)); go.AddComponent<Spin>().speed = 400; break;
                default: { var o = Parts.S(go.transform, Vector3.zero, 0.3f, Parts.G("B57CFF", 3f)); o.AddComponent<Pulse>().amount = 0.2f; break; }
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            float speed = kind == 5 ? 14 : 22;
            if (kind == 5)
            {
                // out and back
                float half = 0.55f;
                var target = t < half ? start + dir * 9 : (owner ? owner.position + Vector3.up * 0.9f : start);
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                if (t > half && Vector3.Distance(transform.position, target) < 0.4f) { Done(); return; }
            }
            else transform.position += dir * speed * Time.deltaTime;
            string trail = kind == 0 ? "elec" : kind == 101 ? "ice" : kind == 4 ? "pixel" : kind == 5 ? "dust" : "magic";
            Effects.Emit(trail, transform.position, 1, 0.05f);
            if (PlayerCtl.Hit(transform.position, 0.5f) && kind != 5) { Effects.Emit("star", transform.position, 8, 0.2f); Done(); return; }
            if (kind != 5 && Physics.CheckSphere(transform.position, 0.12f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { Effects.Emit("dust", transform.position, 6, 0.1f); Done(); return; }
            if (t > 1.6f) Done();
        }

        void Done()
        {
            if (kind == 5 && PlayerCtl.I && PlayerCtl.I.Model.Weapon) PlayerCtl.I.Model.Weapon.gameObject.SetActive(true);
            Destroy(gameObject);
        }
    }

    /// <summary>Teleport pad: stand on it to jump to the paired spot with your victory effect.</summary>
    public class Teleporter : MonoBehaviour
    {
        public Vector3 target;
        public float faceYaw;
        float cd;
        void Update()
        {
            var p = PlayerCtl.I;
            if (p == null || p.MenuOrbit || Time.time < cd) return;
            var d = p.transform.position - transform.position;
            if (Mathf.Abs(d.y) < 1.2f && new Vector2(d.x, d.z).magnitude < 0.9f)
            {
                cd = Time.time + 3f;
                AudioFx.Play("portal");
                p.Frozen = true;
                p.PlayVictory(() => { p.Teleport(target, faceYaw); p.Frozen = UIRoot.Open != null; });
            }
        }
    }

    /// <summary>Scrolls the water texture.</summary>
    public class WaterScroll : MonoBehaviour
    {
        Material m;
        void Start() { m = GetComponent<Renderer>().material; }
        void Update() { if (m) m.mainTextureOffset = new Vector2(Time.time * 0.01f, Time.time * 0.006f); }
    }
}
