using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// Rescue missions: golden keys are hidden around the levels; a key opens a cage with a captured chubrik
    /// in its own random skin. Rescued friends move to the lobby and walk around the plaza.
    /// </summary>
    public static class Rescue
    {
        static readonly string[] Names = { "Пончик", "Бублик", "Зефирка", "Кексик", "Пухлик", "Шнурок", "Чипсик", "Тыковка",
                                           "Вафелька", "Ириска", "Мармелад", "Плюшка", "Сырник", "Леденец", "Пряник", "Карамелька", "Ватрушка", "Крендель" };
        public static readonly List<Cage> Cages = new List<Cage>();
        static readonly List<GameObject> lobbyFriends = new List<GameObject>();

        /// <summary>Cage "w2c5" → its own name: 8 different friends per world.</summary>
        /// <summary>17.11: special friends in the richest outfits, locked deep in the backrooms.</summary>
        public static readonly string[] Vips = { "vipArtemiy", "vipArtem", "vipAli" };

        public static string NameOf(string id)
        {
            if (id == "vipArtemiy") return "Артемий";
            if (id == "vipArtem") return "Артём";
            if (id == "vipAli") return "Али";
            int c = id.IndexOf('c');
            if (id.Length > 1 && id[0] == 'w' && c > 1 && int.TryParse(id.Substring(1, c - 1), out var w) && int.TryParse(id.Substring(c + 1), out var n))
                return Names[((w - 2) * 8 + n) % Names.Length];
            return Names[Mathf.Abs(id.GetHashCode()) % Names.Length];
        }

        /// <summary>A random but stable look for the friend of cage <paramref name="id"/>.</summary>
        public static Look LookOf(string id)
        {
            if (id.StartsWith("vip"))
            {
                var v = Look.Default();
                if (id == "vipArtem") { v["suit"] = Catalog.RichArtem; v["aura"] = "aura_4"; v["pet"] = "pet_p5"; v["emote"] = "emote_5"; }
                else if (id == "vipAli") { v["suit"] = Catalog.RichAli; v["aura"] = "aura_4"; v["emote"] = "emote_4"; }
                else { v["suit"] = Catalog.RichArtemiy; v["aura"] = "aura_p7"; v["pet"] = "pet_p5"; v["color"] = "color_7"; v["emote"] = "emote_5"; }   // 17.11 the coolest outfit
                return v;
            }
            var r = new Rng(id.GetHashCode());
            var l = Look.Default();
            void Pick(string dept, float chance)
            {
                if (!r.Chance(chance)) return;
                var d = Catalog.Dept(dept);
                var regular = d.items.FindAll(i => !i.Premium && !i.tokens && !i.Reward);
                l[dept] = regular[r.I(regular.Count)].id;
            }
            Pick("head", 1); Pick("hair", 0.9f); Pick("ears", 0.9f); Pick("hat", 0.5f); Pick("eyes", 0.35f);
            Pick("color", 1); Pick("skin", 0.3f); Pick("top", 0.6f); Pick("neck", 0.3f); Pick("tail", 0.35f);
            Pick("pants", 0.6f); Pick("shoes", 0.7f); Pick("hands", 0.3f); Pick("back", 0.2f);
            l["emote"] = "emote_" + r.I(8);
            return l;
        }

        /// <summary>18.11: keys found in this visit (each key opens its own cage). Gone in the lobby.</summary>
        public static readonly HashSet<string> Held = new HashSet<string>();

        /// <summary>The key of a cage: «wNcI» ↔ «wNkI», the backrooms VIPs ↔ «brk0..2»; the titan's cage needs none.</summary>
        public static string KeyFor(string cageId)
        {
            if (cageId == "vipArtemiy") return "brk0";
            if (cageId == "vipArtem") return "brk1";
            if (cageId == "vipAli") return "brk2";
            int c = cageId.IndexOf('c');
            if (cageId.Length > 2 && cageId[0] == 'w' && c > 1) return cageId.Substring(0, c) + "k" + cageId.Substring(c + 1);
            return null;
        }

        /// <summary>A key for this cage in the pocket: its own key, or a spare one (a reward key that fits any cage).</summary>
        public static bool HasKeyFor(string cageId) { var k = KeyFor(cageId); return (k != null && Held.Contains(k)) || Save.P.keys > Held.Count; }

        static readonly Dictionary<Material, Material> ghosts = new Dictionary<Material, Material>();
        /// <summary>Makes a thing see-through (cages and keys you already took once).</summary>
        public static void Ghost(Transform t, float alpha = 0.4f)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r.GetComponent<TMPro.TMP_Text>()) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (!ghosts.TryGetValue(m, out var g))
                    {
                        var c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                        g = Parts.Glass(ColorUtility.ToHtmlStringRGB(c), alpha);
                        ghosts[m] = g;
                    }
                    mats[i] = g;
                }
                r.sharedMaterials = mats;
            }
        }

        /// <summary>18.11: coming back into a world — every cage is shut again (see-through if that friend was saved
        /// before) and every key is back (see-through if taken before).</summary>
        public static void ResetWorld(string cagePrefix, string keyPrefix)
        {
            foreach (var c in Cages) if (c != null && c.id.StartsWith(cagePrefix)) c.ResetGhost();
            Pickups.I?.ResetKeys(keyPrefix);
        }

        public static void SpawnCage(string id, Vector3 pos, float yaw, Transform parent, bool ice = false) => new GameObject("Cage " + id).AddComponent<Cage>().Build(id, pos, yaw, parent, ice);

        /// <summary>The player hit something: a closed cage within reach opens (if there's a key).</summary>
        public static bool HitNear(Vector3 pos)
        {
            foreach (var c in Cages)
            {
                if (c.Open || !c.isActiveAndEnabled) continue;
                var d = c.transform.position - pos; d.y = 0;
                if (d.magnitude < Cage.Reach && Mathf.Abs(c.transform.position.y - pos.y) < 2.5f) { c.TryOpen(); return true; }
            }
            return false;
        }

        public static int Rescued(string prefix) { int n = 0; foreach (var c in Cages) if (c.id.StartsWith(prefix) && c.Open) n++; return n; }
        public static int Total(string prefix) { int n = 0; foreach (var c in Cages) if (c.id.StartsWith(prefix)) n++; return n; }

        /// <summary>Rescued friends wander around the lobby plaza.</summary>
        static readonly Vector2[] Homes =
        {
            new Vector2(-7, 10), new Vector2(-30, 13), new Vector2(-3, -28), new Vector2(2, 30), new Vector2(20, -12), new Vector2(-38, 6),
            new Vector2(20, -28), new Vector2(15, 30), new Vector2(32, -12), new Vector2(-30, -12), new Vector2(6, 12), new Vector2(-26, 4),
            new Vector2(2, -45), new Vector2(25, 28), new Vector2(22, 4), new Vector2(-44, -3), new Vector2(18, -47), new Vector2(36, 12),
            new Vector2(45, -10), new Vector2(-36, -20), new Vector2(-8, -2), new Vector2(-18, -2), new Vector2(-4, -36), new Vector2(42, 20),
            new Vector2(14, 19), new Vector2(-34, -4), new Vector2(22, -40), new Vector2(5, 41), new Vector2(40, -20), new Vector2(-26, -20),
            new Vector2(-2, -14), new Vector2(-24, 10), new Vector2(10, -32), new Vector2(-2, 45), new Vector2(0, 20.5f), new Vector2(16, -6),
            new Vector2(-46, 6), new Vector2(30, 32), new Vector2(-14, 14), new Vector2(8, -50),
        };

        public static void SpawnLobbyFriends()
        {
            foreach (var id in Save.P.rescued) AddLobbyFriend(id);
        }

        public static void AddLobbyFriend(string id)
        {
            int i = lobbyFriends.Count;
            // 15.11: each friend has a home spot of its own all over the lobby (no crowd in the middle)
            var h = Homes[i % Homes.Length];
            var pos = new Vector3(h.x + (i / Homes.Length) * 1.5f, 0.05f, h.y);
            var m = ChubrikModel.Create("Friend " + id, Lobby.root, LookOf(id), Layers.World);
            m.transform.position = pos;
            var col = m.gameObject.AddComponent<CapsuleCollider>();   // you can't walk through friends either
            col.center = new Vector3(0, 0.75f, 0); col.height = 1.5f; col.radius = 0.4f;
            var f = m.gameObject.AddComponent<Friend>();
            f.Init(m, id);
            lobbyFriends.Add(m.gameObject);
        }
    }

    /// <summary>A cage with a captured friend. Stand next to it with a key to open it.</summary>
    public class Cage : MonoBehaviour
    {
        public string id;
        /// <summary>World 4: a giant ice cube instead of a cage — with a snowy key, a jump attack breaks it.</summary>
        public bool ice;
        public bool Open { get; private set; }
        ChubrikModel friend;
        ChubrikAnim anim;
        Transform bars;
        float openT = -1;

        public void Build(string cageId, Vector3 pos, float yaw, Transform parent, bool iceCube = false)
        {
            id = cageId; ice = iceCube;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            Open = false;   // 18.11: always shut — a friend saved before waits in a see-through cage (you can save him again)
            Parts.Layer = Layers.World;
            var metal = M("5B6370", 0.7f, 0.7f);
            var gold = M("FFC83A", 0.9f, 0.85f);
            Cy(transform, new Vector3(0, 0.1f, 0), new Vector3(1.9f, 0.1f, 1.9f), metal);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.1f, 0); col.radius = 0.95f; col.height = 2.2f;
            bars = Node("Bars", transform, Vector3.zero);
            if (ice)
            {
                // a giant ice cube with frosty edges and a snow cap
                var iceM = Glass("BFEAFF", 0.45f); var frost = M("F4FBFF", 0.6f);
                B(bars, new Vector3(0, 1.15f, 0), new Vector3(1.9f, 2.1f, 1.9f), iceM);
                for (int k = 0; k < 4; k++)
                {
                    float x = k % 2 == 0 ? -0.95f : 0.95f, z = k < 2 ? -0.95f : 0.95f;
                    B(bars, new Vector3(x, 1.15f, z), new Vector3(0.12f, 2.1f, 0.12f), frost);
                    B(bars, new Vector3(x * (k < 2 ? 1 : 0), 2.2f, z * (k < 2 ? 0 : 1)), k < 2 ? new Vector3(0.12f, 0.12f, 1.9f) : new Vector3(1.9f, 0.12f, 0.12f), frost);
                }
                S(bars, new Vector3(0, 2.25f, 0), new Vector3(2f, 0.4f, 2f), M("FFFFFF", 0.15f));
                for (int k = 0; k < 6; k++) Spike(bars, new Vector3(-0.8f + k * 0.32f, 0.12f, 0.96f), new Vector3(-0.8f + k * 0.32f, 0.6f, 0.98f), 0.12f, frost);
            }
            else
            for (int k = 0; k < 14; k++)
            {
                float a = k * Mathf.PI * 2 / 14;
                Cy(bars, new Vector3(Mathf.Cos(a) * 0.9f, 1.1f, Mathf.Sin(a) * 0.9f), new Vector3(0.06f, 1f, 0.06f), metal);
            }
            if (!ice)
            {
                Ring(bars, new Vector3(0, 1.1f, 0), 0.9f, 0.04f, metal);
                Cy(bars, new Vector3(0, 2.15f, 0), new Vector3(1.95f, 0.06f, 1.95f), metal);
                Cone(bars, new Vector3(0, 2.2f, 0), new Vector3(1.9f, 0.5f, 1.9f), M("C62828", 0.4f));
                Ring(bars, new Vector3(0, 2.75f, 0), 0.1f, 0.03f, metal);
                // padlock on the front
                var lockT = Node("Lock", bars, new Vector3(0, 1.0f, 0.95f));
                B(lockT, Vector3.zero, new Vector3(0.32f, 0.28f, 0.1f), gold);
                Ring(lockT, new Vector3(0, 0.17f, 0), 0.1f, 0.03f, gold, new Vector3(90, 0, 0));
                S(lockT, new Vector3(0, -0.02f, 0.05f), new Vector3(0.06f, 0.08f, 0.02f), M("1A1A1A"));
            }
            var tag = Prim.Text(transform, "ПОМОГИ!", new Vector3(0, 3.2f, 0), 3, Parts.Hex("FFE14D"));
            tag.gameObject.AddComponent<Billboard>();
            tag.gameObject.name = "Help";

            friend = ChubrikModel.Create("Captive", transform, Rescue.LookOf(id), Layers.World);
            friend.transform.localPosition = new Vector3(0, 0.2f, 0);
            friend.transform.localRotation = Quaternion.identity;
            anim = friend.gameObject.AddComponent<ChubrikAnim>();
            anim.model = friend; anim.Preview = true; anim.emote = 6; anim.PlayEmote();   // shrugging, waiting for help
            Rescue.Cages.Add(this);
            if (Save.P.rescued.Contains(id)) { Rescue.Ghost(bars); ghosted = true; }
        }

        public const float Reach = 2.6f;
        float hintT;
        bool ghosted;
        public bool Ghosted => ghosted;

        /// <summary>Back in the world: shut again, see-through (the friend was saved before).</summary>
        public void ResetGhost()
        {
            if (!Open && (ghosted || !Save.P.rescued.Contains(id))) return;
            Open = false; openT = -1;
            bars.localPosition = Vector3.zero; bars.gameObject.SetActive(true);
            if (!ghosted && Save.P.rescued.Contains(id)) { Rescue.Ghost(bars); ghosted = true; }
            friend.gameObject.SetActive(true);
            friend.transform.localPosition = new Vector3(0, 0.2f, 0); friend.transform.localRotation = Quaternion.identity;
            anim.emote = 6; anim.PlayEmote();
            GetComponent<Collider>().enabled = true;
            transform.Find("Help")?.gameObject.SetActive(true);
        }

        /// <summary>18.11: opened by beating the titan — no key needed.</summary>
        public void Free()
        {
            if (Open) return;
            bool wasIce = ice; ice = false;
            Save.P.keys++;
            TryOpen();
            ice = wasIce;
        }

        public void TryOpen()
        {
            if (Open) return;
            if (ice)
            {
                // 15.11: the ice needs a snowy key too — then a jump attack breaks it
                if (!Rescue.HasKeyFor(id)) { AudioFx.Play("error"); Hud.Toast("Нужен снежный ключ от этого льда! У каждого друга свой ключ", UIKit.Cyan); return; }
                var pl = PlayerCtl.I;
                if (pl != null && pl.Grounded) { AudioFx.Play("bonk"); Hud.Toast("Лёд крепкий! Прыгни и ударь в прыжке", UIKit.Cyan); return; }
            }
            else if (!Rescue.HasKeyFor(id))
            {
                AudioFx.Play("error");
                Hud.Toast("Нужен ключ от этой клетки! У каждого друга свой ключ — ищи его", UIKit.Orange);
                return;
            }
            var kid = Rescue.KeyFor(id);
            if (kid == null || !Rescue.Held.Remove(kid)) { }   // a spare key
            Save.P.keys = Mathf.Max(0, Save.P.keys - 1);
            if (ice) { AudioFx.Play("crate"); Effects.Emit("ice", transform.position + Vector3.up * 1.2f, 60, 1f, Vector3.up * 3); }
            bool again = Save.P.rescued.Contains(id);
            if (!again) { Save.P.rescued.Add(id); Save.P.coins += 50; } else Save.P.coins += 10;
            Save.Write();
            Open = true;
            openT = 0;
            GetComponent<Collider>().enabled = false;
            transform.Find("Help")?.gameObject.SetActive(false);
            AudioFx.Play("open");
            AudioFx.Play("token");
            var nm = Rescue.NameOf(id);
            Hud.Toast(again ? $"Ты снова спас друга {nm}! +10 монет" : $"Ты спас друга {nm}! +50 монет. Он ждёт тебя в лобби", UIKit.Lime);
            Effects.Emit("confetti", transform.position + Vector3.up * 2.5f, 90, 0.4f, Vector3.up * 4);
            if (!again) Rescue.AddLobbyFriend(id);
        }

        void Update()
        {
            if (!Open && PlayerCtl.I != null)
            {
                // standing next to the cage: tell how to open it
                var d = transform.position - PlayerCtl.I.transform.position; d.y = 0;
                hintT -= Time.deltaTime;
                if (d.magnitude < Reach + 0.6f && hintT <= 0)
                {
                    bool has = Rescue.HasKeyFor(id);
                    if (ice) Hud.Toast(has ? "Друг вмёрз в лёд! Прыгни и ударь в прыжке — лёд разобьётся" : "Друг вмёрз в лёд! Нужен его снежный ключ", UIKit.Cyan);
                    else Hud.Toast(has ? (Hud.Touch ? "Нажми «УДАР» — клетка откроется!" : "Ударь (F) — клетка откроется!") : "Нужен ключ от этой клетки! У каждого друга свой ключ", has ? UIKit.Lime : UIKit.Orange);
                    hintT = 6;
                }
                else if (d.magnitude > Reach + 2) hintT = 0;
            }
            if (openT < 0) return;
            openT += Time.deltaTime;
            bars.localPosition = Vector3.up * Mathf.Min(openT * 4, 4);
            if (openT < 1.4f)
            {
                // the friend jumps for joy
                friend.transform.localPosition = new Vector3(0, 0.2f + Mathf.Abs(Mathf.Sin(openT * 7)) * 0.8f, Mathf.Min(openT, 1) * 1.2f);
                friend.transform.Rotate(0, Time.deltaTime * 360, 0);
            }
            else if (friend.gameObject.activeSelf)
            {
                // off to the lobby
                Effects.Victory(0, friend);
                friend.gameObject.SetActive(false);
            }
            if (openT > 1.2f) bars.gameObject.SetActive(false);
            if (openT > 3) openT = -1;
        }
    }

    /// <summary>Golden key pickup (handled by Pickups like tokens: taken once forever).</summary>
    public static class KeyModel
    {
        /// <summary>15.11: the snowy key of the Snowy Valley — icy blue with a snow cap and a snowflake.</summary>
        public static void BuildSnow(Transform t)
        {
            var ice = G("8FD8FF", 1.1f); var snow = M("FFFFFF", 0.15f);
            Ring(t, new Vector3(0, 0.22f, 0), 0.16f, 0.05f, ice, new Vector3(90, 0, 0));
            S(t, new Vector3(0, 0.37f, 0), new Vector3(0.3f, 0.12f, 0.12f), snow);   // a snow cap on the ring
            B(t, new Vector3(0, -0.12f, 0), new Vector3(0.08f, 0.5f, 0.08f), ice);
            B(t, new Vector3(0.09f, -0.3f, 0), new Vector3(0.14f, 0.06f, 0.06f), ice);
            B(t, new Vector3(0.07f, -0.18f, 0), new Vector3(0.1f, 0.06f, 0.06f), ice);
            for (int k = 0; k < 3; k++) B(t, new Vector3(0, 0.22f, 0), new Vector3(0.22f, 0.025f, 0.025f), snow, new Vector3(0, 0, k * 60));   // snowflake
            S(t, Vector3.zero, 1.1f, Add("BFEFFF", 0.15f));
        }

        public static void Build(Transform t)
        {
            var gold = G("FFC83A", 0.6f);
            Ring(t, new Vector3(0, 0.22f, 0), 0.16f, 0.05f, gold, new Vector3(90, 0, 0));
            B(t, new Vector3(0, -0.12f, 0), new Vector3(0.08f, 0.5f, 0.08f), gold);
            B(t, new Vector3(0.09f, -0.3f, 0), new Vector3(0.14f, 0.06f, 0.06f), gold);
            B(t, new Vector3(0.07f, -0.18f, 0), new Vector3(0.1f, 0.06f, 0.06f), gold);
            S(t, Vector3.zero, 1.1f, Add("FFE14D", 0.15f));
        }
    }

    /// <summary>A rescued friend in the lobby: walks between random spots near the plaza and dances.</summary>
    public class Friend : MonoBehaviour
    {
        ChubrikModel model;
        ChubrikAnim anim;
        Vector3 target;
        float wait;
        Rng rng;

        public void Init(ChubrikModel m, string id)
        {
            model = m;
            anim = gameObject.AddComponent<ChubrikAnim>();
            anim.model = m;
            anim.emote = Mathf.Max(0, m.look.Idx("emote"));
            anim.walkStyle = 0;
            rng = new Rng(id.GetHashCode());
            var tag = Prim.Text(transform, Rescue.NameOf(id), new Vector3(0, 2.1f, 0), 2.4f, Color.white);
            tag.gameObject.layer = Layers.World;
            tag.gameObject.AddComponent<Billboard>();
            target = home = transform.position;
        }

        Vector3 home;
        /// <summary>Where this friend lives (walks around).</summary>
        public Vector3 Home => home;

        /// <summary>Is the straight way clear (no walls or things)? Friends never walk through them.</summary>
        static bool Clear(Vector3 from, Vector3 to)
        {
            var d = to - from; d.y = 0;
            return !Physics.SphereCast(from + Vector3.up * 0.8f, 0.35f, d.normalized, out _, d.magnitude + 0.4f, Layers.SolidMask, QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            var to = target - transform.position; to.y = 0;
            if (to.magnitude < 0.2f)
            {
                anim.speed01 = 0;
                wait -= Time.deltaTime;
                if (wait <= 0)
                {
                    // a short walk round its own home spot, only where nothing is in the way
                    for (int k = 0; k < 6; k++)
                    {
                        float a = rng.F(0, Mathf.PI * 2), d = rng.F(1.5f, 4f);
                        var t = new Vector3(home.x + Mathf.Cos(a) * d, transform.position.y, home.z + Mathf.Sin(a) * d);
                        if (Clear(transform.position, t)) { target = t; break; }
                    }
                    wait = rng.F(3, 8);
                    if (rng.Chance(0.5f)) anim.PlayEmote();
                }
                return;
            }
            if (anim.emoting) { anim.speed01 = 0; return; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 6);
            var step = to.normalized * 2.2f * Time.deltaTime;
            if (!Clear(transform.position, transform.position + step * 4)) { target = transform.position; wait = rng.F(1, 3); anim.speed01 = 0; return; }   // something in the way: stop
            transform.position += step;
            anim.speed01 = 0.4f;
        }
    }
}
