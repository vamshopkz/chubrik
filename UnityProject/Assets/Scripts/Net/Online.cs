using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Online play: every running game says «I'm here» through a free public MQTT relay (two brokers at once, the
    /// same ones the browser and the phone can both reach). The menu lists everyone playing — as a picture of their
    /// skin, no names — and a tap takes you to them: into their world, right next to them. Players in the same world
    /// see each other run, jump and flip. Coins, enemies and progress stay each player's own.
    /// Messages (text, '|' separated, one topic):
    ///   H|id|version|world|look            hello, every 3 s (look = dept=item;dept=item…)
    ///   S|id|seq|world|x|y|z|yaw|speed|vy|flags   position, 10 times a second while someone shares the world
    ///   J|id|toId                          «I came to you»
    ///   B|id                               leaving
    /// </summary>
    public class Online : MonoBehaviour
    {
        public static Online I;
        public const string Topic = "chubrik/v1/all";
        static readonly string[] Brokers = { "broker.emqx.io", "broker.hivemq.com" };

        public class Peer
        {
            public string id, ver = "", lookStr = "";
            public Look look;
            public int world = 1, seq = -1;
            public float seen, stateSeen = -99;
            public Vector3 pos;
            public float yaw, speed, vy;
            public int flags;
            public RemoteChubrik avatar;
            public bool SameVersion => ver == Application.version;
        }

        class Relay { public string broker; public ILink link; public float retryAt; }

        public readonly Dictionary<string, Peer> Peers = new Dictionary<string, Peer>();
        public string Id { get; private set; }
        public bool Connected { get { foreach (var r in relays) if (r.link != null && r.link.Ready) return true; return false; } }

        readonly List<Relay> relays = new List<Relay>();
        readonly List<NetIncoming> inbox = new List<NetIncoming>();
        float helloT, stateT;
        int seq;
        string lookStr = "";
        Transform avatarsRoot;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Create()
        {
            if (I != null) return;
            I = new GameObject("Online").AddComponent<Online>();
            DontDestroyOnLoad(I.gameObject);
        }

        void Awake()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 10);
            avatarsRoot = new GameObject("OnlinePlayers").transform;
            DontDestroyOnLoad(avatarsRoot.gameObject);
            foreach (var b in Brokers) relays.Add(new Relay { broker = b, link = MqttLink.Create(b, Topic, Topic) });
        }

        public static string WorldName(int w) => w switch
        {
            2 => "Парящие руины", 3 => "Кристальные пещеры", 4 => "Снежная долина", 5 => "Джунгли", 6 => "Глюк-мир", 7 => "Мир 67", _ => "Лобби",
        };

        public static string LookString(Look l)
        {
            var sb = new StringBuilder();
            foreach (var kv in l) { if (sb.Length > 0) sb.Append(';'); sb.Append(kv.Key).Append('=').Append(kv.Value); }
            return sb.ToString();
        }

        static Look ParseLook(string s)
        {
            var l = Look.Default();
            foreach (var part in s.Split(';'))
            {
                int e = part.IndexOf('=');
                if (e <= 0) continue;
                var dept = part.Substring(0, e); var id = part.Substring(e + 1);
                if (Catalog.Get(id) != null) l[dept] = id;   // unknown items (a newer version) stay default
            }
            return l;
        }

        public void Send(string m)
        {
            foreach (var r in relays) if (r.link != null && r.link.Ready) r.link.Send(m);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            // keep both relays alive: the free brokers sometimes drop a connection
            foreach (var r in relays)
            {
                if (r.link != null && !r.link.Failed) continue;
                if (r.retryAt <= 0) { r.retryAt = now + 10; continue; }
                if (now < r.retryAt) continue;
                r.link?.Close();
                r.link = MqttLink.Create(r.broker, Topic, Topic);
                r.retryAt = 0;
            }
            inbox.Clear();
            foreach (var r in relays) r.link?.Poll(inbox);
            foreach (var m in inbox) { try { Handle(m.text); } catch (Exception e) { Debug.LogWarning("[Online] bad message: " + e.Message); } }

            var p = PlayerCtl.I;
            bool playing = p != null && !p.MenuOrbit;
            if (now >= helloT && playing)
            {
                helloT = now + 3f;
                lookStr = LookString(Save.CurrentLook());
                Send($"H|{Id}|{Application.version}|{Worlds.Current}|{lookStr}");
            }
            // our position, only while someone else is in the same world
            if (playing && now >= stateT && AnyoneHere())
            {
                stateT = now + 0.1f;
                var pos = p.transform.position;
                int flags = (p.Anim.grounded ? 1 : 0) | (p.Anim.Flipping ? 2 : 0) | (p.Anim.Attacking ? 4 : 0) | (p.Anim.belly ? 8 : 0) | (p.Anim.gliding ? 16 : 0) | (p.Anim.emoting ? 32 : 0);
                Send(string.Format(Inv, "S|{0}|{1}|{2}|{3:0.00}|{4:0.00}|{5:0.00}|{6:0}|{7:0.00}|{8:0.0}|{9}",
                    Id, ++seq, Worlds.Current, pos.x, pos.y, pos.z, p.transform.eulerAngles.y, p.Anim.speed01, p.Anim.vy, flags));
            }
            // forget players who went quiet; show the ones in our world
            var gone = new List<string>();
            foreach (var kv in Peers)
            {
                var peer = kv.Value;
                if (now - peer.seen > 10f) { gone.Add(kv.Key); continue; }
                bool show = playing && peer.SameVersion && peer.world == Worlds.Current && now - peer.stateSeen < 3f;
                if (show && peer.avatar == null) peer.avatar = RemoteChubrik.Spawn(peer, avatarsRoot);
                else if (!show && peer.avatar != null) { Destroy(peer.avatar.gameObject); peer.avatar = null; }
            }
            foreach (var g in gone) Remove(g);
        }

        bool AnyoneHere()
        {
            foreach (var peer in Peers.Values) if (peer.SameVersion && peer.world == Worlds.Current) return true;
            return false;
        }

        void Remove(string id)
        {
            if (!Peers.TryGetValue(id, out var peer)) return;
            if (peer.avatar != null) Destroy(peer.avatar.gameObject);
            Peers.Remove(id);
        }

        void Handle(string text)
        {
            var f = text.Split('|');
            if (f.Length < 2) return;
            string id = f[1];
            if (id == Id) return;   // our own message back from the relay
            float now = Time.unscaledTime;
            switch (f[0])
            {
                case "H" when f.Length >= 5:
                {
                    if (!Peers.TryGetValue(id, out var peer)) Peers[id] = peer = new Peer { id = id };
                    peer.seen = now;
                    peer.ver = f[2];
                    int.TryParse(f[3], out peer.world);
                    var ls = f[4];
                    if (ls != peer.lookStr)
                    {
                        peer.lookStr = ls; peer.look = ParseLook(ls);
                        if (peer.avatar != null) peer.avatar.SetLook(peer.look);
                    }
                    break;
                }
                case "S" when f.Length >= 11:
                {
                    if (!Peers.TryGetValue(id, out var peer)) return;   // wait for their hello (we need the look)
                    int s = int.Parse(f[2], Inv);
                    if (s <= peer.seq && peer.seq - s < 1000) return;   // the copy from the second relay, or late
                    peer.seq = s;
                    peer.seen = peer.stateSeen = now;
                    peer.world = int.Parse(f[3], Inv);
                    peer.pos = new Vector3(float.Parse(f[4], Inv), float.Parse(f[5], Inv), float.Parse(f[6], Inv));
                    peer.yaw = float.Parse(f[7], Inv);
                    peer.speed = float.Parse(f[8], Inv);
                    peer.vy = float.Parse(f[9], Inv);
                    peer.flags = int.Parse(f[10], Inv);
                    break;
                }
                case "J" when f.Length >= 3:
                    if (f[2] == Id && Peers.ContainsKey(id)) Hud.Toast("К тебе пришёл друг! Он рядом с тобой", UIKit.Lime);
                    break;
                case "B":
                    Remove(id);
                    break;
            }
        }

        /// <summary>Go to that player: into their world (even if it is still closed for us) and next to them.</summary>
        public void Join(Peer peer)
        {
            var p = PlayerCtl.I;
            if (p == null || peer == null) return;
            Window.Close();
            Send($"J|{Id}|{peer.id}");
            StartCoroutine(JoinRoutine(peer));
        }

        System.Collections.IEnumerator JoinRoutine(Peer peer)
        {
            var p = PlayerCtl.I;
            if (Worlds.Current != peer.world)
            {
                Worlds.Enter(peer.world);
                float t = 0;
                while ((Worlds.Current != peer.world || p.Frozen) && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSeconds(0.2f);
                Teleport(peer);
            }
            else p.PlayVictory(() => Teleport(peer));
        }

        void Teleport(Peer peer)
        {
            var p = PlayerCtl.I;
            var side = Quaternion.Euler(0, peer.yaw + 90, 0) * Vector3.forward * 1.6f;
            p.Teleport(peer.pos + side + Vector3.up * 0.3f, peer.yaw);
            p.SafePos = p.transform.position;
            Hud.Toast("Ты рядом с другом!", UIKit.Lime);
        }

        void OnApplicationQuit() => Send($"B|{Id}");

        /// <summary>Tests: feed a message in as if it came from the relay.</summary>
        public void Inject(string text) => Handle(text);
    }

    /// <summary>Another player's chubrik in our world: follows their position smoothly and plays their moves.</summary>
    public class RemoteChubrik : MonoBehaviour
    {
        Online.Peer peer;
        ChubrikModel model;
        ChubrikAnim anim;
        Pet pet;
        int lastFlags;

        public static RemoteChubrik Spawn(Online.Peer peer, Transform parent)
        {
            var go = new GameObject("Online " + peer.id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(peer.pos, Quaternion.Euler(0, peer.yaw, 0));
            var r = go.AddComponent<RemoteChubrik>();
            r.peer = peer;
            r.SetLook(peer.look ?? Look.Default());
            return r;
        }

        public void SetLook(Look l)
        {
            if (model != null) Destroy(model.gameObject);
            model = ChubrikModel.Create("Model", transform, l, Layers.Player);
            anim = model.gameObject.AddComponent<ChubrikAnim>();
            anim.model = model;
            anim.walkStyle = Mathf.Max(0, l.Idx("walk"));
            anim.emote = Mathf.Max(0, l.Idx("emote"));
            if (pet == null) { pet = new GameObject("Pet").AddComponent<Pet>(); pet.transform.SetParent(transform.parent, false); pet.owner = transform; }
            pet.Set(l.Of("pet"), Layers.Player);
        }

        void Update()
        {
            if (peer == null) return;
            float dt = Time.deltaTime;
            var target = peer.pos;
            if ((transform.position - target).sqrMagnitude > 36) transform.position = target;   // far jump (teleport): snap
            else transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-14 * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, peer.yaw, 0), 1 - Mathf.Exp(-14 * dt));
            anim.speed01 = peer.speed;
            anim.vy = peer.vy;
            anim.grounded = (peer.flags & 1) != 0;
            anim.belly = (peer.flags & 8) != 0;
            anim.gliding = (peer.flags & 16) != 0;
            if ((peer.flags & 2) != 0 && (lastFlags & 2) == 0) anim.Flip();
            if ((peer.flags & 4) != 0 && (lastFlags & 4) == 0) anim.Attack();
            if ((peer.flags & 32) != 0 && (lastFlags & 32) == 0) anim.PlayEmote();
            if ((peer.flags & 32) == 0 && (lastFlags & 32) != 0) anim.StopEmote();
            lastFlags = peer.flags;
        }

        void OnDestroy() { if (pet != null) Destroy(pet.gameObject); }
    }
}
