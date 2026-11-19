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
        /// <summary>17.11 the records table: each profile keeps one retained message «R|…» at StatsTopic/profileId.</summary>
        public const string StatsTopic = "chubrik/v1/stats";

        public class Record
        {
            public string pid, nick = "", lookStr = "";
            public long coins, tokens;
            public int friends, worlds, eggs;
            public Look look;
            public string raw;     // the «R|…» line as it came (kept in the local book of everyone)
            public bool old;       // an older game version without records: seen online, numbers unknown
        }
        /// <summary>17.11: everyone ever seen is kept on the device too (the public relay may forget retained lines);
        /// lines the relay has lost are given back to it, so the table keeps everyone who has ever played.</summary>
        readonly HashSet<string> fromRelay = new HashSet<string>();
        bool bookDirty; float bookT = 30f, reshareAt = -1; bool reshared;
        static string BookPath => System.IO.Path.Combine(Application.persistentDataPath, "records_book.txt");
        public readonly Dictionary<string, Record> Records = new Dictionary<string, Record>();
        /// <summary>The player we are «in a room» with (we came to them or they came to us).</summary>
        public string RoomWith;
        public Peer RoomPeer => RoomWith != null && Peers.TryGetValue(RoomWith, out var p) ? p : null;
        /// <summary>18.11: players are together only by invitation. Who we invited (and when), and who invites us.</summary>
        public readonly Dictionary<string, float> Invited = new Dictionary<string, float>();
        public string InviteFrom { get; private set; }
        public bool Invitation(string id) => Invited.TryGetValue(id, out var t) && Time.unscaledTime - t < 60f;
        float recordT = 4f;
        string lastRecord = "";
        static readonly string[] Brokers = { "broker.emqx.io", "broker.hivemq.com" };

        public class Peer
        {
            public string id, ver = "", lookStr = "", nick = "";
            public long lockUntil;   // 19.11: their room is locked until (unix seconds)
            public bool Locked => lockUntil > NowUnix;
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
            foreach (var b in Brokers) relays.Add(new Relay { broker = b, link = MqttLink.Create(b, Subs, Topic) });
            LoadBook();
        }

        void LoadBook()
        {
            try
            {
                if (!System.IO.File.Exists(BookPath)) return;
                foreach (var line in System.IO.File.ReadAllLines(BookPath))
                {
                    var r = ParseRecord(line);
                    if (r != null && r.pid != Save.P?.id && !Records.ContainsKey(r.pid)) Records[r.pid] = r;
                }
            }
            catch (Exception e) { Debug.Log("[Online] records book: " + e.Message); }
        }

        void SaveBook()
        {
            bookDirty = false;
            try
            {
                var lines = new List<string>();
                foreach (var r in Records.Values) if (!string.IsNullOrEmpty(r.raw) && !r.old) lines.Add(r.raw);
                System.IO.File.WriteAllLines(BookPath, lines);
            }
            catch (Exception e) { Debug.Log("[Online] records book: " + e.Message); }
        }

        public static Record ParseRecord(string line)
        {
            var f = (line ?? "").Split('|');
            if (f.Length < 9 || f[0] != "R" || string.IsNullOrEmpty(f[1])) return null;
            var r = new Record { pid = f[1], nick = f[2], raw = line };
            long.TryParse(f[3], NumberStyles.Integer, Inv, out r.coins);
            int.TryParse(f[4], out r.friends); int.TryParse(f[5], out r.worlds);
            long.TryParse(f[6], NumberStyles.Integer, Inv, out r.tokens);
            int.TryParse(f[7], out r.eggs);
            r.lookStr = f[8]; r.look = ParseLook(r.lookStr);
            return r;
        }

        /// <summary>v0.27: voice pieces go to the friend's own topic (nobody else gets them).</summary>
        public const string VoiceTopic = "chubrik/v1/voice";
        string Subs => Topic + "\n" + StatsTopic + "/#" + "\n" + VoiceTopic + "/" + Id;

        public void SendTo(string topic, string m)
        {
            foreach (var r in relays) if (r.link != null && r.link.Ready) r.link.Publish(topic, m, false);
        }

        public static long NowUnix => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        /// <summary>19.11 «Заблокировать комнату»: nobody can send us invitations until this time.</summary>
        public static bool Locked => Save.P.roomLockUntil > NowUnix;
        public static void LockRoom(int minutes)
        {
            Save.P.roomLockUntil = minutes > 0 ? NowUnix + minutes * 60L : 0;
            Save.Write();
            if (I != null) I.helloT = 0;   // tell the others now
        }
        public static string LockTime(long until) => System.DateTimeOffset.FromUnixTimeSeconds(until).ToLocalTime().ToString("HH:mm");

        public static string Clean(string s) => (s ?? "").Replace("|", "").Replace(";", "").Replace("\n", "").Trim();

        /// <summary>Our own line for the records table.</summary>
        public static Record Mine()
        {
            var P = Save.P;
            return new Record { pid = P.id, nick = Nick, coins = P.coins, tokens = P.tokens, friends = P.rescued.Count, worlds = P.done.Count, eggs = Eggs.Found, lookStr = LookString(Save.CurrentLook()), look = Save.CurrentLook() };
        }

        /// <summary>The player's nickname (Settings); a default one until they choose.</summary>
        public static string Nick => string.IsNullOrEmpty(Save.P.nick) ? "Чубрик-" + ((Save.P.id ?? "") + "0000").Substring(0, 4).ToUpperInvariant() : Save.P.nick;

        void PublishRecord(bool force)
        {
            var r = Mine();
            var msg = string.Format(Inv, "R|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}", r.pid, Clean(r.nick), r.coins, r.friends, r.worlds, r.tokens, r.eggs, r.lookStr);
            if (!force && msg == lastRecord) return;
            lastRecord = msg;
            foreach (var rl in relays) if (rl.link != null && rl.link.Ready) rl.link.Publish(StatsTopic + "/" + r.pid, msg, true);
        }

        /// <summary>Leave the room: say goodbye to the friend and go back to the lobby.</summary>
        public void LeaveRoom()
        {
            if (RoomWith != null) Send($"L|{Id}|{RoomWith}");
            RoomWith = null;
            Window.Close();
            if (Worlds.Current != 1) Worlds.Enter(1);
            else PlayerCtl.I?.PlayVictory(() => PlayerCtl.I.Teleport(Lobby.Spawn, 0));
            Hud.Toast("Ты вышел из комнаты", UIKit.Cyan);
        }

        public static string WorldName(int w) => w switch
        {
            2 => "Парящие руины", 3 => "Кристальные пещеры", 4 => "Снежная долина", 5 => "Джунгли", 6 => "Глюк-мир", 7 => "Мир 67", 8 => "Мемный мир", _ => "Лобби",
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
                r.link = MqttLink.Create(r.broker, Subs, Topic);
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
                Send($"H|{Id}|{Application.version}|{Worlds.Current}|{lookStr}|{Clean(Nick)}|{(Locked ? Save.P.roomLockUntil : 0)}");
            }
            // our line in the records table (kept on the relay), when something changed
            if (now >= recordT && Connected && p != null)
            {
                recordT = now + 20f;
                PublishRecord(false);
                var mine = Mine();
                Records[mine.pid] = mine;
                if (reshareAt < 0) reshareAt = now + 15f;
            }
            // give the relay back the lines it has lost (players from the local book it no longer remembers)
            if (!reshared && reshareAt > 0 && now >= reshareAt && Connected)
            {
                reshared = true;
                int n = 0;
                foreach (var r in Records.Values)
                    if (!r.old && !string.IsNullOrEmpty(r.raw) && !fromRelay.Contains(r.pid) && r.pid != Save.P.id && n++ < 300)
                        foreach (var rl in relays) if (rl.link != null && rl.link.Ready) rl.link.Publish(StatsTopic + "/" + r.pid, r.raw, true);
                if (n > 0) Debug.Log("[Online] gave back " + n + " record lines");
            }
            if (bookDirty && now >= bookT) { bookT = now + 30f; SaveBook(); }
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
                bool show = playing && peer.SameVersion && (peer.id == RoomWith || peer.id == FootballFans.Watching) && peer.world == Worlds.Current && now - peer.stateSeen < 3f;
                if (show && peer.avatar == null) peer.avatar = RemoteChubrik.Spawn(peer, avatarsRoot);
                else if (!show && peer.avatar != null) { Destroy(peer.avatar.gameObject); peer.avatar = null; }
            }
            foreach (var g in gone) Remove(g);
            if (RoomWith != null && !Peers.ContainsKey(RoomWith)) { RoomWith = null; Hud.Toast("Друг вышел из игры — комната закрыта", UIKit.Orange); }
        }

        bool AnyoneHere()
        {
            var rp = RoomPeer;   // our position goes only to the friend we are together with
            if (rp != null && rp.SameVersion && rp.world == Worlds.Current) return true;
            // 19.11: playing football — others may watch from the stands
            if (PlayerCtl.I != null && Football.Mode != 0 && Football.Inside(PlayerCtl.I.transform.position) && AnyoneInLobby()) return true;
            return false;
        }

        public bool AnyoneInLobby()
        {
            foreach (var peer in Peers.Values) if (peer.SameVersion && peer.world == 1) return true;
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
            if (id == Id && f[0] != "R") return;   // our own message back from the relay
            float now = Time.unscaledTime;
            switch (f[0])
            {
                case "H" when f.Length >= 5:
                {
                    if (!Peers.TryGetValue(id, out var peer)) Peers[id] = peer = new Peer { id = id };
                    peer.seen = now;
                    peer.ver = f[2];
                    int.TryParse(f[3], out peer.world);
                    if (f.Length >= 6) peer.nick = f[5];
                    peer.lockUntil = f.Length >= 7 && long.TryParse(f[6], out var lu) ? lu : 0;
                    // an older game (before the records): still put them in the table while they are online
                    if (string.Compare(peer.ver, "0.17", StringComparison.Ordinal) < 0 && peer.ver.Length <= 4 && !Records.ContainsKey("s:" + id))
                        Records["s:" + id] = new Record { pid = "s:" + id, nick = string.IsNullOrEmpty(peer.nick) ? "Игрок (старая версия)" : peer.nick, old = true, lookStr = f[4], look = ParseLook(f[4]) };
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
                    // 18.11: only someone we invited (or who accepted our invitation) may come
                    if (f[2] == Id && Peers.ContainsKey(id) && (Invitation(id) || RoomWith == id)) { RoomWith = id; Invited.Remove(id); Hud.Toast("К тебе пришёл друг! Он рядом с тобой", UIKit.Lime); }
                    break;
                case "I" when f.Length >= 3:
                    // an invitation for us
                    if (f[2] == Id && Peers.ContainsKey(id) && RoomWith != id && Locked) { Send($"N|{Id}|{id}"); break; }   // 19.11 our room is locked
                    if (f[2] == Id && Peers.ContainsKey(id) && RoomWith != id)
                    {
                        InviteFrom = id;
                        var nick = f.Length >= 4 && f[3] != "" ? f[3] : (Peers[id].nick != "" ? Peers[id].nick : "Игрок");
                        InviteUI.Show(id, nick);
                    }
                    break;
                case "A" when f.Length >= 3:
                    if (f[2] == Id && Invitation(id)) { RoomWith = id; Hud.Toast($"«{NickOf(id)}» принял приглашение — идёт к тебе!", UIKit.Lime); }
                    break;
                case "N" when f.Length >= 3:
                    if (f[2] == Id && Invited.Remove(id)) Hud.Toast($"«{NickOf(id)}» сейчас не может играть вместе", UIKit.Orange);
                    break;
                case "V":
                    Voice.I?.OnVoice(f);
                    break;
                case "C":
                    RoomChat.I?.OnChat(f);
                    break;
                case "FW":
                    FootballFans.OnWatch(f, id);
                    break;
                case "FS": case "FB": case "FK": case "FE":
                    Football.OnNet(f, id);
                    break;
                case "L" when f.Length >= 3:
                    if (f[2] == Id && RoomWith == id) { RoomWith = null; Hud.Toast("Друг вышел из комнаты", UIKit.Orange); }
                    break;
                case "R" when f.Length >= 9:
                {
                    if (f[1] == Save.P.id) break;   // our own line comes back from the relay
                    var r = ParseRecord(string.Join("|", f));
                    if (r == null) break;
                    if (injecting) { r.raw = null; Records[r.pid] = r; break; }
                    fromRelay.Add(r.pid);
                    if (!Records.TryGetValue(r.pid, out var had) || had.raw != r.raw) bookDirty = true;
                    Records[r.pid] = r;
                    break;
                }
                case "B":
                    Remove(id);
                    break;
            }
        }

        string NickOf(string id) => Peers.TryGetValue(id, out var p) && p.nick != "" ? p.nick : "Игрок";

        /// <summary>18.11: invite a player to play together; they choose «Принять» or «Отказаться».</summary>
        public void Invite(Peer peer)
        {
            if (peer == null) return;
            if (peer.Locked) { AudioFx.Play("error"); Hud.Toast($"Комната «{NickOf(peer.id)}» закрыта до {LockTime(peer.lockUntil)}", UIKit.Orange); return; }
            Invited[peer.id] = Time.unscaledTime;
            Send($"I|{Id}|{peer.id}|{Clean(Nick)}");
            AudioFx.Play("click");
            Hud.Toast($"Приглашение отправлено «{NickOf(peer.id)}» — ждём ответа", UIKit.Cyan);
        }

        /// <summary>We accept an invitation: we leave our old room (if any) and go to the one who invited us.</summary>
        public void Accept(string id)
        {
            InviteFrom = null;
            if (!Peers.TryGetValue(id, out var peer)) { Hud.Toast("Игрок уже вышел из игры", UIKit.Orange); return; }
            if (RoomWith != null && RoomWith != id) Send($"L|{Id}|{RoomWith}");
            Send($"A|{Id}|{id}");
            Join(peer);
        }

        public void Decline(string id)
        {
            InviteFrom = null;
            Send($"N|{Id}|{id}");
        }

        /// <summary>Go to that player: into their world (even if it is still closed for us) and next to them.</summary>
        public void Join(Peer peer)
        {
            var p = PlayerCtl.I;
            if (p == null || peer == null) return;
            Window.Close();
            RoomWith = peer.id;
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
        bool injecting;
        /// <summary>Tests: a message as if it came from the relay (never written to the book nor given back to the relay).</summary>
        public void Inject(string text) { injecting = true; try { Handle(text); } finally { injecting = false; } }
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
