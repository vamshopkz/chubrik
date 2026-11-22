using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 19.11: the stadium's fans are the friends you rescued (some seats stay free), they jump at every goal.
    /// «Следить за кем-то»: watch another player's football from the stands — a TV camera follows them and their ball.
    /// </summary>
    public static class FootballFans
    {
        public const int MaxFans = 24;
        public static readonly List<Vector3> Seats = new List<Vector3>();
        public static readonly List<Transform> Fans = new List<Transform>();
        public static int FreeSeats => Seats.Count - Fans.Count;
        public static string Watching;            // the peer we watch (null = nobody)
        public static Vector3 WatchBall; public static int WatchMe, WatchThem; public static float WatchSeen = -99;
        static Transform root;
        static TextMeshProUGUI tv;
        static GameObject stopBtn;
        static float sendT, watchStart;
        internal static void ResetStatics() { Seats.Clear(); Fans.Clear(); Watching = null; WatchSeen = -99; }

        /// <summary>Seats on the stands (rows of blue chairs), built with the stadium.</summary>
        public static void Build(Transform stadium, float bx, float bz)
        {
            root = stadium;
            Seats.Clear();
            var chair = M("1E5BD8", 0.4f); var leg = M("8C96A3", 0.5f, 0.4f);
            foreach (var sd in new[] { -1f, 1f })
                for (int row = 0; row < 4; row++)
                {
                    float z = sd * (bz + 2 + row * 1.6f), y = 1 + row * 1.8f;
                    for (int k = 0; k < 18; k++)
                    {
                        var p = new Vector3(-bx + 1.5f + k * (bx * 2 - 3) / 17f, y, z);
                        B(stadium, p + new Vector3(0, 0.2f, 0), new Vector3(0.7f, 0.12f, 0.6f), chair);
                        B(stadium, p + new Vector3(0, 0.55f, sd * 0.3f), new Vector3(0.7f, 0.6f, 0.1f), chair);
                        Seats.Add(Football.O + p + Vector3.up * 0.26f);
                    }
                }
            root.gameObject.AddComponent<Runner>();
        }

        /// <summary>Puts the rescued friends on random seats (some seats stay free).</summary>
        public static void Refresh()
        {
            foreach (var f in Fans) if (f) Object.Destroy(f.gameObject);
            Fans.Clear();
            if (root == null) return;
            var ids = new List<string>(Save.P.rescued);
            if (ids.Count == 0) return;
            var rng = new Rng(ids.Count * 7 + 3);
            var free = new List<int>(); for (int i = 0; i < Seats.Count; i++) free.Add(i);
            int n = Mathf.Min(ids.Count, MaxFans, Seats.Count - 8);   // always some free seats
            for (int i = 0; i < n; i++)
            {
                int pick = rng.I(free.Count); int s = free[pick]; free.RemoveAt(pick);
                var id = ids[ids.Count - 1 - i];
                var m = ChubrikModel.Create("Fan " + id, root, Rescue.LookOf(id), Layers.World);
                var seat = Seats[s];
                m.transform.position = seat;
                var look = Football.O - seat; look.y = 0; look.x = 0;
                m.transform.rotation = Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward);
                var an = m.gameObject.AddComponent<ChubrikAnim>();
                an.model = m; an.Preview = true; an.emote = (i % 3 == 0) ? 1 : 5;
                m.gameObject.AddComponent<Fan>().anim = an;
                Fans.Add(m.transform);
            }
        }

        /// <summary>Everybody jumps and cheers.</summary>
        public static void Cheer()
        {
            foreach (var f in Fans) if (f) f.GetComponent<Fan>()?.Jump();
            AudioFx.Play("token", 0.5f);
        }

        class Fan : MonoBehaviour
        {
            public ChubrikAnim anim;
            float jumpT = -1, phase;
            Vector3 home;
            void Start() { home = transform.position; phase = Random.value * 10; }
            public void Jump() { jumpT = 0; if (anim) anim.PlayEmote(); }
            void Update()
            {
                if (jumpT < 0) { transform.position = home + Vector3.up * Mathf.Max(0, Mathf.Sin(Time.time * 2 + phase)) * 0.05f; return; }
                jumpT += Time.deltaTime;
                transform.position = home + Vector3.up * Mathf.Abs(Mathf.Sin(jumpT * 9)) * 0.6f;
                if (jumpT > 1.6f) { jumpT = -1; transform.position = home; }
            }
        }

        // ---------------- sitting and watching ----------------

        public static void SitDown()
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            Football.EnsureBuilt();
            Window.Close();
            int s = Seats.FindIndex(p => !Fans.Exists(f => f && (f.position - p).sqrMagnitude < 0.1f));
            var seat = s >= 0 ? Seats[s] : Football.O + new Vector3(0, 2, -16);
            pl.PlayVictory(() =>
            {
                var look = Football.O - seat; look.y = 0; look.x = 0;
                pl.Teleport(seat + Vector3.up * 0.1f, Quaternion.LookRotation(look).eulerAngles.y);
                pl.yaw = pl.transform.eulerAngles.y; pl.pitch = 18;
                pl.WaterY = Football.O.y - 10; pl.SafePos = pl.Checkpoint = pl.transform.position;
            });
            Hud.Toast("Ты на трибуне! Рядом болеют спасённые друзья", UIKit.Lime);
        }

        /// <summary>Players playing football right now (they are in the stadium).</summary>
        public static List<Online.Peer> Players()
        {
            var l = new List<Online.Peer>();
            var on = Online.I;
            if (on == null) return l;
            foreach (var p in on.Peers.Values)
                if (p.SameVersion && p.world == 1 && Time.unscaledTime - p.stateSeen < 4f && Football.Inside(p.pos)) l.Add(p);
            return l;
        }

        public static void PickTarget()
        {
            Window.Show("СЛЕДИТЬ ЗА КЕМ-ТО", w =>
            {
                var list = Players();
                if (list.Count == 0)
                {
                    w.Text("Сейчас никто не играет в футбол. Загляни попозже — или позови друга!", 28, UIKit.Muted);
                    w.Row("Пока посидеть на трибуне", "Рядом с твоими друзьями-болельщиками", "НА ТРИБУНУ", UIKit.Green, SitDown);
                }
                foreach (var p in list)
                {
                    var pp = p;
                    w.Row(string.IsNullOrEmpty(p.nick) ? "Игрок" : p.nick, "Играет в футбол прямо сейчас", "СЛЕДИТЬ", UIKit.Violet, () => Watch(pp), true, UIKit.Violet);
                }
                w.Footer.text = "Камера будет следить за игроком и его мячом";
            }, 1000, 640);
        }

        public static void Watch(Online.Peer p)
        {
            if (p == null) return;
            Watching = p.id;
            WatchSeen = -99;
            watchStart = Time.time;
            SitDown();
            ShowTv(true);
            Hud.Toast($"Следишь за «{(string.IsNullOrEmpty(p.nick) ? "игроком" : p.nick)}»", UIKit.Violet);
        }

        public static void StopWatching()
        {
            Watching = null;
            ShowTv(false);
            if (Football.Ball && Football.Mode == 0) { Football.Ball.isKinematic = false; Football.ResetBall(); }
        }

        static void ShowTv(bool on)
        {
            if (!on) { if (tv) Object.Destroy(tv.gameObject); if (stopBtn) Object.Destroy(stopBtn); tv = null; stopBtn = null; return; }
            if (tv) return;
            tv = UIKit.Txt(UIRoot.HudLayer, "", 34, Color.white);
            UIKit.Place(tv.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78 - UIRoot.Safe().w), new Vector2(1000, 56));
            var b = UIKit.Button(UIRoot.HudLayer, "ХВАТИТ СМОТРЕТЬ", UIKit.Red, StopWatching, 24, "StopWatching");
            UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140 - UIRoot.Safe().w), new Vector2(320, 64));
            stopBtn = b.Bg.gameObject;
        }

        /// <summary>The TV camera along the side of the pitch, following the watched player.</summary>
        public static bool CameraUpdate(Camera cam)
        {
            if (Watching == null || Online.I == null) return false;
            if (!Online.I.Peers.TryGetValue(Watching, out var p)) { StopWatching(); Hud.Toast("Игрок ушёл", UIKit.Orange); return false; }
            var target = p.pos + Vector3.up * 1f;
            var pos = new Vector3(target.x, Football.O.y + 9f, Football.O.z - Football.HalfW - 9f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, pos, 1 - Mathf.Exp(-4 * Time.deltaTime));
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(target - cam.transform.position), 1 - Mathf.Exp(-6 * Time.deltaTime));
            if (tv) tv.text = $"Смотришь: {(string.IsNullOrEmpty(p.nick) ? "игрок" : p.nick)}" + (Time.unscaledTime - WatchSeen < 3 ? $"   ·   {WatchMe} : {WatchThem}" : "");
            return true;
        }

        /// <summary>«FW|id|x|y|z|me|them» — the ball of someone playing (for those who watch).</summary>
        public static void OnWatch(string[] f, string from)
        {
            if (from != Watching || f.Length < 7) return;
            var I = CultureInfo.InvariantCulture;
            WatchBall = Football.O + new Vector3(float.Parse(f[2], I), float.Parse(f[3], I), float.Parse(f[4], I));
            int.TryParse(f[5], out WatchMe); int.TryParse(f[6], out WatchThem);
            WatchSeen = Time.unscaledTime;
        }

        class Runner : MonoBehaviour
        {
            void Update()
            {
                var on = Online.I; var pl = PlayerCtl.I;
                if (on == null || pl == null) return;
                // our ball for those who might watch us
                if (Football.Mode != 0 && Football.Ball && Football.Inside(pl.transform.position) && on.AnyoneInLobby() && (sendT -= Time.deltaTime) <= 0)
                {
                    sendT = 0.2f;
                    var b = Football.Ball.position - Football.O;
                    on.Send(string.Format(CultureInfo.InvariantCulture, "FW|{0}|{1:0.00}|{2:0.00}|{3:0.00}|{4}|{5}", on.Id, b.x, b.y, b.z, Football.Me, Football.Them));
                }
                // the watched player's ball
                if (Watching != null && Football.Ball && Football.Mode == 0 && Time.unscaledTime - WatchSeen < 3)
                {
                    Football.Ball.isKinematic = true;
                    var np = Vector3.Lerp(Football.Ball.position, WatchBall, 1 - Mathf.Exp(-10 * Time.deltaTime));
                    Football.Ball.position = np; Football.Ball.transform.position = np;
                }
                if (Watching != null && !Football.Inside(pl.transform.position) && Time.time - watchStart > 4f) StopWatching();   // left the stadium (after the trip to the stands)
            }
        }
    }
}
