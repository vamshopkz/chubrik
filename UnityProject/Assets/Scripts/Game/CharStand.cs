using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// 22.11: the player's other chubriks (the ones not played right now) stand at the sides of the lobby start
    /// with their name tag. Walk up and press «ВОЙТИ» — you play as that one, and the one you were stands there instead.
    /// </summary>
    public static class CharStand
    {
        static readonly Vector3[] Offsets = { new Vector3(-6, 0, -3), new Vector3(6, 0, -3) };
        static Vector3[] spots;
        static readonly CharSlot[] atSpot = new CharSlot[2];
        static readonly List<StandingChar> stands = new List<StandingChar>();
        static readonly List<Zones.Z> zones = new List<Zones.Z>();
        static string sig;
        static bool hooked, dirty;

        /// <summary>The chubriks standing in the lobby now (autotest).</summary>
        public static IReadOnlyList<StandingChar> Stands => stands;
        public static Vector3 SpotOf(int s) => spots != null && s >= 0 && s < spots.Length ? spots[s] : Lobby.Spawn;

        /// <summary>Called once after the lobby is built (Boot), then again whenever the chubriks change.</summary>
        public static void Build()
        {
            if (Lobby.root == null) return;
            if (!hooked)
            {
                hooked = true;
                Save.Changed += () => { if (Signature() != sig) dirty = true; };
                new GameObject("CharStands").AddComponent<CharStandRunner>();
            }
            if (spots == null) spots = FindSpots();
            Clear();
            var P = Save.P;
            var others = new List<CharSlot>();
            for (int i = 0; i < P.chars.Count; i++) if (i != P.activeChar) others.Add(P.chars[i]);
            for (int s = 0; s < atSpot.Length; s++) if (atSpot[s] != null && !others.Contains(atSpot[s])) atSpot[s] = null;
            foreach (var c in others)
            {
                if (System.Array.IndexOf(atSpot, c) >= 0) continue;
                int free = System.Array.IndexOf(atSpot, (CharSlot)null);
                if (free >= 0) atSpot[free] = c;
            }
            for (int s = 0; s < atSpot.Length; s++)
                if (atSpot[s] != null) Spawn(s, P.chars.IndexOf(atSpot[s]));
            sig = Signature();
            dirty = false;
        }

        internal static void Tick() { if (dirty) Build(); }

        /// <summary>Who is active + every other chubrik's name and outfit: a change means the stands are rebuilt.</summary>
        static string Signature()
        {
            var P = Save.P;
            var sb = new System.Text.StringBuilder();
            sb.Append(P.activeChar).Append('#').Append(P.chars.Count);
            for (int i = 0; i < P.chars.Count; i++)
            {
                if (i == P.activeChar) continue;
                sb.Append('|').Append(Save.CharName(i)).Append(':');
                foreach (var e in P.chars[i].look) sb.Append(e).Append(',');
            }
            return sb.ToString();
        }

        static void Clear()
        {
            foreach (var st in stands) if (st) { st.gameObject.SetActive(false); Object.Destroy(st.gameObject); }
            stands.Clear();
            foreach (var z in zones) Zones.All.Remove(z);
            zones.Clear();
        }

        /// <summary>Free flat ground next to the start, once (before any stand exists).</summary>
        static Vector3[] FindSpots()
        {
            Physics.SyncTransforms();
            var res = new Vector3[Offsets.Length];
            for (int s = 0; s < Offsets.Length; s++)
            {
                var off = Offsets[s];
                var tries = new[] { off, off + new Vector3(0, 0, 2), off + new Vector3(0, 0, -2), off * 1.3f, off * 0.75f, off + new Vector3(Mathf.Sign(off.x) * 2, 0, 2), off + new Vector3(0, 0, 4) };
                res[s] = Lobby.Spawn + off + Vector3.down * 0.25f;
                foreach (var t in tries)
                {
                    var c = Lobby.Spawn + t;
                    if (!Physics.Raycast(c + Vector3.up * 12, Vector3.down, out var hit, 30, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < 0.8f || Mathf.Abs(hit.point.y - Lobby.Spawn.y) > 2.5f) continue;
                    var g = hit.point;
                    if (Physics.CheckCapsule(g + Vector3.up * 0.55f, g + Vector3.up * 1.5f, 0.5f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    res[s] = g + Vector3.up * 0.02f;
                    break;
                }
            }
            return res;
        }

        static void Spawn(int s, int idx)
        {
            if (idx < 0) return;
            var slot = Save.P.chars[idx];
            string name = Save.CharName(idx);
            var pos = spots[s];
            var m = ChubrikModel.Create("CharStand " + name, Lobby.root, Save.CharLook(idx), Layers.World);
            m.transform.position = pos;
            var face = Lobby.Spawn + new Vector3(0, 0, 3) - pos; face.y = 0;   // looking at the start of the plaza
            if (face.sqrMagnitude > 0.01f) m.transform.rotation = Quaternion.LookRotation(face);
            var col = m.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.75f, 0); col.height = 1.5f; col.radius = 0.4f;
            var st = m.gameObject.AddComponent<StandingChar>();
            st.Init(m, slot, name, s);
            stands.Add(st);
            var z = new Zones.Z
            {
                pos = pos, r = 2.4f, label = "ИГРАТЬ ЗА: " + name, kind = Zones.Kind.Button,
                act = () => PlayAs(Save.P.chars.IndexOf(slot)),
                active = () => Worlds.Current == 1 && !Worlds.Level,
            };
            Zones.All.Add(z);
            zones.Add(z);
        }

        /// <summary>Plays as chubrik <paramref name="idx"/> right here: the one played before takes its place.</summary>
        public static void PlayAs(int idx)
        {
            var P = Save.P;
            if (idx < 0 || idx >= P.chars.Count || idx == P.activeChar) return;
            var slot = P.chars[idx];
            int s = System.Array.IndexOf(atSpot, slot);
            string prev = Save.CharName(P.activeChar);
            if (s >= 0) atSpot[s] = Save.ActiveChar;
            Save.SwitchChar(idx);
            PlayerCtl.I?.ApplyLook(Save.CurrentLook());
            Build();
            AudioFx.Play("magic");
            Hud.Toast($"Теперь ты играешь за: {Save.CharName(P.activeChar)}! {prev} подождёт тебя здесь", UIKit.Lime);
        }
    }

    class CharStandRunner : MonoBehaviour
    {
        void Update() => CharStand.Tick();
    }

    /// <summary>One of your chubriks waiting in the lobby: stands still, looks at you when you come close, does its emote now and then.</summary>
    public class StandingChar : MonoBehaviour
    {
        public CharSlot Slot;
        public string Name;
        public int Spot;
        public ChubrikModel Model;
        ChubrikAnim anim;
        Quaternion baseRot;
        float wait;

        public void Init(ChubrikModel m, CharSlot slot, string name, int spot)
        {
            Model = m; Slot = slot; Name = name; Spot = spot;
            anim = gameObject.AddComponent<ChubrikAnim>();
            anim.model = m;
            anim.emote = Mathf.Max(0, m.look.Idx("emote"));
            anim.walkStyle = 0;
            var tag = Prim.Text(transform, name, new Vector3(0, 2.15f, 0), 2.4f, Color.white);
            tag.gameObject.layer = Layers.World;
            tag.gameObject.AddComponent<Billboard>();
            var sub = Prim.Text(transform, "твой чубрик", new Vector3(0, 1.82f, 0), 1.5f, UIKit.Gold);
            sub.gameObject.layer = Layers.World;
            sub.gameObject.AddComponent<Billboard>();
            baseRot = transform.rotation;
            wait = Random.Range(2f, 5f);
        }

        void Update()
        {
            var want = baseRot;
            var p = PlayerCtl.I;
            if (p != null && !p.MenuOrbit)
            {
                var d = p.transform.position - transform.position; d.y = 0;
                if (d.magnitude < 6 && d.magnitude > 0.3f) want = Quaternion.LookRotation(d);   // turns to you when you come
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * 4);
            anim.speed01 = 0;
            wait -= Time.deltaTime;
            if (wait <= 0)
            {
                wait = Random.Range(5f, 10f);
                if (!anim.emoting) anim.PlayEmote();
            }
        }
    }
}
