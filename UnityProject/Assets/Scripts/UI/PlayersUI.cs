using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// Menu → «Игроки онлайн»: everyone playing right now, each shown as a picture of their skin (no names).
    /// Tap a picture to go to that player.
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
            var list = new List<Online.Peer>(on.Peers.Values);
            list.Sort((a, b) => (b.SameVersion ? 1 : 0) - (a.SameVersion ? 1 : 0));
            if (list.Count == 0)
            {
                w.Text("Сейчас больше никто не играет.", 32);
                w.Text("Позови друга: когда он зайдёт в игру, его чубрик появится здесь.", 26, UIKit.Muted);
                w.Footer.text = "Список обновляется сам";
                return;
            }
            w.Text("Нажми на чубрика — и ты окажешься рядом с ним!", 28, UIKit.Gold);
            var grid = UIKit.Rect("Players", w.Content);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(232, 290); gl.spacing = new Vector2(14, 14); gl.padding = new RectOffset(4, 4, 4, 4);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            foreach (var peer in list)
            {
                var pr = peer;
                bool ok = pr.SameVersion;
                var tile = UIKit.Img(grid, UIKit.Btn, ok ? Color.white : new Color(1, 1, 1, 0.45f), "Player", true);
                var btn = tile.gameObject.AddComponent<UIButton>();
                btn.Bg = tile;
                btn.OnClick = ok ? () => Online.I.Join(pr) : (System.Action)(() => { AudioFx.Play("error"); Hud.Toast("У этого игрока другая версия игры — " + pr.ver, UIKit.Orange); });
                var raw = new GameObject("Skin", typeof(RectTransform)).AddComponent<RawImage>();
                raw.transform.SetParent(tile.transform, false);
                raw.raycastTarget = false;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(214, 214));
                if (pr.look != null && Preview.I != null) raw.texture = Preview.I.Portrait(pr.look, pr.lookStr);
                if (!ok) raw.color = new Color(1, 1, 1, 0.5f);
                var where = UIKit.Txt(tile.transform, ok ? Online.WorldName(pr.world) : "другая версия", 22, ok ? UIKit.Ink : UIKit.Muted, TextAlignmentOptions.Center, false);
                UIKit.Place(where.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(220, 52));
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
                return sb.ToString();
            }
        }
    }
}
