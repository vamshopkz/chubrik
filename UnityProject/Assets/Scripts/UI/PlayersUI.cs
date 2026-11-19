using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// Menu → «Игроки онлайн»: everyone playing right now, each shown as a picture of their skin (no names).
    /// 18.11: nobody comes to you by themselves — under every picture there is «Пригласить»; the other player
    /// accepts or declines. «Выйти из комнаты» ends playing together.
    /// </summary>
    public static class PlayersUI
    {
        public static void Open()
        {
            var win = Window.Show("ИГРОКИ ОНЛАЙН", Fill, 1100, 780);
            win.gameObject.AddComponent<Refresher>();
        }

        static void Fill(Window w)
        {
            var on = Online.I;
            if (on == null || !on.Connected)
            {
                w.Text("Подключаюсь к интернету…", 30, UIKit.Muted);
                w.Footer.text = "Нужен интернет";
                return;
            }
            var room = on.RoomPeer;
            if (on.RoomWith != null)
                w.Row(room != null && room.nick != "" ? "Ты в комнате с игроком «" + room.nick + "»" : "Ты в комнате с другом", room != null ? "Он сейчас: " + Online.WorldName(room.world) : null, "ВЫЙТИ ИЗ КОМНАТЫ", UIKit.Red, () => on.LeaveRoom());
            else
                w.Row("Ты играешь один", "Без твоего приглашения к тебе никто не подключится", "ВЫЙТИ ИЗ КОМНАТЫ", UIKit.Red, null, false);
            if (Online.Locked)
                w.Row($"Комната заблокирована до {Online.LockTime(Save.P.roomLockUntil)}", "Никто не может прислать тебе приглашение", "ОТКРЫТЬ КОМНАТУ", UIKit.Green, () => { Online.LockRoom(0); Hud.Toast("Комната снова открыта", UIKit.Lime); Window.Refresh(); }, true, UIKit.Red);
            else
                w.Row("Заблокировать комнату", "Выбери, на сколько: пока закрыто, тебе не придут приглашения", "ЗАБЛОКИРОВАТЬ", UIKit.Red, LockUI.Open, true, UIKit.Red);
            var list = new List<Online.Peer>(on.Peers.Values);
            list.Sort((a, b) => (b.SameVersion ? 1 : 0) - (a.SameVersion ? 1 : 0));
            if (list.Count == 0)
            {
                w.Text("Сейчас больше никто не играет.", 32);
                w.Text("Позови друга: когда он зайдёт в игру, его чубрик появится здесь.", 26, UIKit.Muted);
                w.Footer.text = "Список обновляется сам";
                return;
            }
            w.Text("Нажми «Пригласить» — если игрок согласится, вы будете играть вместе", 26, UIKit.Gold);
            var grid = UIKit.Rect("Players", w.Content);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(232, 350); gl.spacing = new Vector2(14, 14); gl.padding = new RectOffset(4, 4, 4, 4);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            foreach (var peer in list)
            {
                var pr = peer;
                bool ok = pr.SameVersion;
                var tile = UIKit.Img(grid, UIKit.Btn, ok ? Color.white : new Color(1, 1, 1, 0.45f), "Player", true);
                bool inRoom = on.RoomWith == pr.id, waiting = on.Invitation(pr.id);
                System.Action act = !ok ? () => { AudioFx.Play("error"); Hud.Toast("У этого игрока другая версия игры — " + pr.ver, UIKit.Orange); }
                    : inRoom ? () => Hud.Toast("Вы уже играете вместе", UIKit.Lime)
                    : (System.Action)(() => { Online.I.Invite(pr); Window.Refresh(); });
                bool closed = pr.Locked && !inRoom;
                var inv = UIKit.Button(tile.transform, inRoom ? "ВМЕСТЕ" : closed ? "ЗАКРЫТО до " + Online.LockTime(pr.lockUntil) : waiting ? "ЖДЁМ…" : "ПРИГЛАСИТЬ", inRoom ? UIKit.Lime : closed ? new Color(0.45f, 0.4f, 0.5f) : ok ? UIKit.Green : new Color(0.4f, 0.4f, 0.5f), act, closed ? 17 : 22, "Invite");
                UIKit.Place(inv.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(216, 64));
                var raw = new GameObject("Skin", typeof(RectTransform)).AddComponent<RawImage>();
                raw.transform.SetParent(tile.transform, false);
                raw.raycastTarget = false;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(214, 214));
                if (pr.look != null && Preview.I != null) raw.texture = Preview.I.Portrait(pr.look, pr.lookStr);
                if (!ok) raw.color = new Color(1, 1, 1, 0.5f);
                var where = UIKit.Txt(tile.transform, ok ? Online.WorldName(pr.world) : "другая версия", 22, ok ? UIKit.Ink : UIKit.Muted, TextAlignmentOptions.Center, false);
                UIKit.Place(where.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(220, 46));
                where.enableAutoSizing = true; where.fontSizeMin = 14; where.fontSizeMax = 22;
            }
            w.Footer.text = $"Сейчас играют: {list.Count}";
        }

        /// <summary>Redraws the list when someone comes or goes (or moves to another world).</summary>
        class Refresher : MonoBehaviour
        {
            string last;
            float t;
            Window win;
            void Start() { win = GetComponent<Window>(); last = Signature(); }
            void Update()
            {
                if ((t += Time.unscaledDeltaTime) < 1f) return;
                t = 0;
                if (UIRoot.Open != win) return;
                var s = Signature();
                if (s != last) { last = s; Window.Refresh(); }
            }
            static string Signature()
            {
                var on = Online.I;
                if (on == null) return "";
                var sb = new StringBuilder(on.Connected ? "c" : "n");
                var keys = new List<string>(on.Peers.Keys); keys.Sort();
                foreach (var k in keys) { var p = on.Peers[k]; sb.Append(k).Append(p.world).Append(p.lookStr.GetHashCode()); }
                sb.Append(on.RoomWith);
                foreach (var k in keys) { if (on.Invitation(k)) sb.Append('i').Append(k); if (on.Peers[k].Locked) sb.Append('L').Append(k); }
                sb.Append(Online.Locked);
                return sb.ToString();
            }
        }
    }
}

namespace Chubrik
{
    /// <summary>18.11: «X зовёт тебя играть вместе» — accept or decline (it goes away by itself after 30 s).</summary>
    public static class InviteUI
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        static string from;

        public static void Show(string id, string nick)
        {
            Close();
            from = id;
            var panel = UIKit.Img(UIRoot.TopLayer, UIKit.Panel, Color.white, "Invite", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(820, 250));
            root = panel.gameObject;
            var t = UIKit.Txt(panel.transform, $"«{nick}» зовёт тебя играть вместе!", 34, UIKit.Gold);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(780, 60));
            t.enableAutoSizing = true; t.fontSizeMin = 20; t.fontSizeMax = 34;
            var yes = UIKit.Button(panel.transform, "ПРИНЯТЬ", UIKit.Green, () => { var f = from; Close(); Window.Close(); Online.I?.Accept(f); }, 30, "Accept");
            UIKit.Place(yes.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-190, 26), new Vector2(340, 90));
            var no = UIKit.Button(panel.transform, "ОТКАЗАТЬСЯ", UIKit.Red, () => { var f = from; Close(); Online.I?.Decline(f); }, 30, "Decline");
            UIKit.Place(no.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(190, 26), new Vector2(340, 90));
            root.AddComponent<Timeout>();
            AudioFx.Play("token");
            Menus.Unlock();
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null;
        }

        class Timeout : MonoBehaviour
        {
            float t;
            void Update() { if ((t += Time.unscaledDeltaTime) > 30f) { var f = from; Close(); Online.I?.Decline(f); } }
        }
    }
}

namespace Chubrik
{
    /// <summary>19.11 «Заблокировать комнату»: ready times or an exact one (hours and minutes) — shows until when.</summary>
    public static class LockUI
    {
        static int hours, minutes = 30;

        public static void Open()
        {
            Window.Show("ЗАБЛОКИРОВАТЬ КОМНАТУ", w =>
            {
                w.Text("Пока комната закрыта, никто не сможет прислать тебе приглашение.", 26, UIKit.Muted);
                var presets = new (string, int)[] { ("5 мин", 5), ("15 мин", 15), ("30 мин", 30), ("1 час", 60), ("2 часа", 120), ("6 часов", 360), ("1 день", 1440) };
                var chips = new System.Collections.Generic.List<(string, Color, bool, System.Action)>();
                foreach (var (label, m) in presets) { int mm = m; chips.Add((label, UIKit.Blue, hours * 60 + minutes == mm, () => { hours = mm / 60; minutes = mm % 60; Window.Refresh(); })); }
                w.Chips("Быстро выбрать", chips);
                w.Chips($"Часы: {hours}", new (string, Color, bool, System.Action)[]
                {
                    ("−1 ч", UIKit.Orange, false, () => { hours = Mathf.Max(0, hours - 1); Window.Refresh(); }), ("+1 ч", UIKit.Green, false, () => { hours = Mathf.Min(48, hours + 1); Window.Refresh(); }),
                    ("−5 ч", UIKit.Orange, false, () => { hours = Mathf.Max(0, hours - 5); Window.Refresh(); }), ("+5 ч", UIKit.Green, false, () => { hours = Mathf.Min(48, hours + 5); Window.Refresh(); }),
                });
                w.Chips($"Минуты: {minutes}", new (string, Color, bool, System.Action)[]
                {
                    ("−1", UIKit.Orange, false, () => { minutes = Mathf.Max(0, minutes - 1); Window.Refresh(); }), ("+1", UIKit.Green, false, () => { minutes = Mathf.Min(59, minutes + 1); Window.Refresh(); }),
                    ("−10", UIKit.Orange, false, () => { minutes = Mathf.Max(0, minutes - 10); Window.Refresh(); }), ("+10", UIKit.Green, false, () => { minutes = Mathf.Min(59, minutes + 10); Window.Refresh(); }),
                });
                int total = hours * 60 + minutes;
                string until = total > 0 ? Online.LockTime(Online.NowUnix + total * 60L) : "—";
                w.Row(total > 0 ? $"Закрыть на {(hours > 0 ? hours + " ч " : "")}{minutes} мин — до {until}" : "Выбери время", null, "ЗАБЛОКИРОВАТЬ", UIKit.Red, () =>
                {
                    Online.LockRoom(total);
                    AudioFx.Play("click");
                    Hud.Toast($"Комната заблокирована до {until}", UIKit.Orange);
                    PlayersUI.Open();
                }, total > 0, UIKit.Red);
                w.Row("Назад", null, "НАЗАД", UIKit.Blue, PlayersUI.Open);
            }, 1100, 760);
        }

        /// <summary>Tests: set the chosen time.</summary>
        public static void Set(int h, int m) { hours = h; minutes = m; }
    }
}
