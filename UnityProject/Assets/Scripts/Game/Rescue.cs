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
        static readonly string[] Names = { "Пухлик", "Бублик", "Шнурок", "Кексик", "Пончик", "Чипсик", "Зефирка", "Тыковка", "Вафелька", "Ириска", "Мармелад", "Плюшка" };
        public static readonly List<Cage> Cages = new List<Cage>();
        static readonly List<GameObject> lobbyFriends = new List<GameObject>();

        public static string NameOf(string id) => Names[Mathf.Abs(id.GetHashCode()) % Names.Length];

        /// <summary>A random but stable look for the friend of cage <paramref name="id"/>.</summary>
        public static Look LookOf(string id)
        {
            var r = new Rng(id.GetHashCode());
            var l = Look.Default();
            void Pick(string dept, float chance)
            {
                if (!r.Chance(chance)) return;
                var d = Catalog.Dept(dept);
                var regular = d.items.FindAll(i => !i.Premium && !i.tokens);
                l[dept] = regular[r.I(regular.Count)].id;
            }
            Pick("head", 1); Pick("hair", 0.9f); Pick("ears", 0.9f); Pick("hat", 0.5f); Pick("eyes", 0.35f);
            Pick("color", 1); Pick("skin", 0.3f); Pick("top", 0.6f); Pick("neck", 0.3f); Pick("tail", 0.35f);
            Pick("pants", 0.6f); Pick("shoes", 0.7f); Pick("hands", 0.3f); Pick("back", 0.2f);
            l["emote"] = "emote_" + r.I(8);
            return l;
        }

        public static void SpawnCage(string id, Vector3 pos, float yaw, Transform parent) => new GameObject("Cage " + id).AddComponent<Cage>().Build(id, pos, yaw, parent);

        public static int Rescued(string prefix) { int n = 0; foreach (var c in Cages) if (c.id.StartsWith(prefix) && c.Open) n++; return n; }
        public static int Total(string prefix) { int n = 0; foreach (var c in Cages) if (c.id.StartsWith(prefix)) n++; return n; }

        /// <summary>Rescued friends wander around the lobby plaza.</summary>
        public static void SpawnLobbyFriends()
        {
            foreach (var id in Save.P.rescued) AddLobbyFriend(id);
        }

        public static void AddLobbyFriend(string id)
        {
            int i = lobbyFriends.Count;
            float a = i * 2.4f;
            var pos = new Vector3(Mathf.Cos(a) * 6f, 0.05f, Mathf.Sin(a) * 6f - 1);
            var m = ChubrikModel.Create("Friend " + id, Lobby.root, LookOf(id), Layers.World);
            m.transform.position = pos;
            var f = m.gameObject.AddComponent<Friend>();
            f.Init(m, id);
            lobbyFriends.Add(m.gameObject);
        }
    }

    /// <summary>A cage with a captured friend. Stand next to it with a key to open it.</summary>
    public class Cage : MonoBehaviour
    {
        public string id;
        public bool Open { get; private set; }
        ChubrikModel friend;
        ChubrikAnim anim;
        Transform bars;
        float openT = -1;

        public void Build(string cageId, Vector3 pos, float yaw, Transform parent)
        {
            id = cageId;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            Open = Save.P.rescued.Contains(id);
            Parts.Layer = Layers.World;
            var metal = M("5B6370", 0.7f, 0.7f);
            var gold = M("FFC83A", 0.9f, 0.85f);
            Cy(transform, new Vector3(0, 0.1f, 0), new Vector3(1.9f, 0.1f, 1.9f), metal);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.1f, 0); col.radius = 0.95f; col.height = 2.2f;
            bars = Node("Bars", transform, Vector3.zero);
            for (int k = 0; k < 14; k++)
            {
                float a = k * Mathf.PI * 2 / 14;
                Cy(bars, new Vector3(Mathf.Cos(a) * 0.9f, 1.1f, Mathf.Sin(a) * 0.9f), new Vector3(0.06f, 1f, 0.06f), metal);
            }
            Ring(bars, new Vector3(0, 1.1f, 0), 0.9f, 0.04f, metal);
            Cy(bars, new Vector3(0, 2.15f, 0), new Vector3(1.95f, 0.06f, 1.95f), metal);
            Cone(bars, new Vector3(0, 2.2f, 0), new Vector3(1.9f, 0.5f, 1.9f), M("C62828", 0.4f));
            Ring(bars, new Vector3(0, 2.75f, 0), 0.1f, 0.03f, metal);
            // padlock on the front
            var lockT = Node("Lock", bars, new Vector3(0, 1.0f, 0.95f));
            B(lockT, Vector3.zero, new Vector3(0.32f, 0.28f, 0.1f), gold);
            Ring(lockT, new Vector3(0, 0.17f, 0), 0.1f, 0.03f, gold, new Vector3(90, 0, 0));
            S(lockT, new Vector3(0, -0.02f, 0.05f), new Vector3(0.06f, 0.08f, 0.02f), M("1A1A1A"));
            var tag = Prim.Text(transform, "ПОМОГИ!", new Vector3(0, 3.2f, 0), 3, Parts.Hex("FFE14D"));
            tag.gameObject.AddComponent<Billboard>();
            tag.gameObject.name = "Help";

            friend = ChubrikModel.Create("Captive", transform, Rescue.LookOf(id), Layers.World);
            friend.transform.localPosition = new Vector3(0, 0.2f, 0);
            friend.transform.localRotation = Quaternion.identity;
            anim = friend.gameObject.AddComponent<ChubrikAnim>();
            anim.model = friend; anim.Preview = true; anim.emote = 6; anim.PlayEmote();   // shrugging, waiting for help
            Rescue.Cages.Add(this);
            if (Open) { bars.gameObject.SetActive(false); friend.gameObject.SetActive(false); tag.gameObject.SetActive(false); col.enabled = false; }
            else Zones.Add(transform.position, 2.4f, "ОТКРЫТЬ КЛЕТКУ", TryOpen, () => !Open);
        }

        void TryOpen()
        {
            if (Open) return;
            if (Save.P.keys <= 0)
            {
                AudioFx.Play("error");
                Hud.Toast("Нужен золотой ключ! Ищи ключи на островах", UIKit.Orange);
                return;
            }
            Save.P.keys--;
            Save.P.rescued.Add(id);
            Save.P.coins += 50;
            Save.Write();
            Open = true;
            openT = 0;
            GetComponent<Collider>().enabled = false;
            transform.Find("Help")?.gameObject.SetActive(false);
            AudioFx.Play("open");
            AudioFx.Play("token");
            var nm = Rescue.NameOf(id);
            Hud.Toast($"Ты спас друга {nm}! +50 монет. Он ждёт тебя в лобби", UIKit.Lime);
            Effects.Emit("confetti", transform.position + Vector3.up * 2.5f, 90, 0.4f, Vector3.up * 4);
            Rescue.AddLobbyFriend(id);
        }

        void Update()
        {
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
            target = transform.position;
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
                    float a = rng.F(0, Mathf.PI * 2), d = rng.F(4, 11);
                    target = new Vector3(Mathf.Cos(a) * d, transform.position.y, Mathf.Sin(a) * d);
                    if (Vector3.Distance(target, new Vector3(0, 0, 6)) < 3.2f) target += new Vector3(4, 0, 0);   // not into the fountain
                    wait = rng.F(3, 8);
                    if (rng.Chance(0.5f)) anim.PlayEmote();
                }
                return;
            }
            if (anim.emoting) { anim.speed01 = 0; return; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 6);
            transform.position += to.normalized * 2.2f * Time.deltaTime;
            anim.speed01 = 0.4f;
        }
    }
}
