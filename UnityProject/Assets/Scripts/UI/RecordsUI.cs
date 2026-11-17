using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// Menu → «Таблица рекордов»: everyone who has ever played (their line is kept on the online relay), sorted by
    /// coins, rescued friends, finished worlds, tokens or golden eggs. Our own line is always there and marked.
    /// </summary>
    public static class RecordsUI
    {
        public enum By { Coins, Friends, Worlds, Tokens, Eggs }
        static By by = By.Coins;
        public static List<Online.Record> Last = new List<Online.Record>();

        public static List<Online.Record> Sorted(By b)
        {
            var list = new List<Online.Record>();
            if (Online.I != null) list.AddRange(Online.I.Records.Values);
            var mine = Online.Mine();
            list.RemoveAll(r => r.pid == mine.pid);
            list.Add(mine);
            long V(Online.Record r) => b switch { By.Friends => r.friends, By.Worlds => r.worlds, By.Tokens => r.tokens, By.Eggs => r.eggs, _ => r.coins };
            list.Sort((x, y) => x.old != y.old ? (x.old ? 1 : -1) : V(y).CompareTo(V(x)));
            return list;
        }

        static string Value(Online.Record r) => r.old ? "старая версия" : by switch
        {
            By.Friends => r.friends + " друзей", By.Worlds => r.worlds + " миров", By.Tokens => UIKit.Num(r.tokens) + " жет.", By.Eggs => r.eggs + " яиц", _ => UIKit.Num(r.coins) + " мон.",
        };

        public static void Open()
        {
            var win = Window.Show("ТАБЛИЦА РЕКОРДОВ", Fill, 1100, 800);
            win.gameObject.AddComponent<Refresher>();
        }

        static void Fill(Window w)
        {
            w.Chips("Кто лучше всех", new (string, Color, bool, System.Action)[]
            {
                ("МОНЕТЫ", UIKit.Gold, by == By.Coins, () => { by = By.Coins; Window.Refresh(); }),
                ("ДРУЗЬЯ", UIKit.Green, by == By.Friends, () => { by = By.Friends; Window.Refresh(); }),
                ("МИРЫ", UIKit.Blue, by == By.Worlds, () => { by = By.Worlds; Window.Refresh(); }),
                ("ЖЕТОНЫ", UIKit.Violet, by == By.Tokens, () => { by = By.Tokens; Window.Refresh(); }),
                ("ЯЙЦА", UIKit.Orange, by == By.Eggs, () => { by = By.Eggs; Window.Refresh(); }),
            });
            var list = Last = Sorted(by);
            if (Online.I == null || !Online.I.Connected) w.Text("Нет интернета — видно только тебя. Таблица загрузится, когда появится связь.", 24, UIKit.Muted);
            string me = Save.P.id;
            int shown = 0;
            for (int i = 0; i < list.Count && shown < 100; i++, shown++)
            {
                var r = list[i];
                bool mine = r.pid == me;
                var row = UIKit.Img(w.Content, UIKit.Glass, mine ? new Color(1f, 0.85f, 0.3f, 0.9f) : Color.white, "Record");
                row.gameObject.AddComponent<LayoutElement>().minHeight = 96;
                var num = UIKit.Txt(row.transform, (i + 1).ToString(), 40, i < 3 ? UIKit.Gold : Color.white, TextAlignmentOptions.Center, true);
                UIKit.Place(num.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(70, 80));
                if (i < 12 && Preview.I != null && r.look != null)
                {
                    var raw = new GameObject("Skin", typeof(RectTransform)).AddComponent<RawImage>();
                    raw.transform.SetParent(row.transform, false); raw.raycastTarget = false;
                    UIKit.Place(raw.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(88, 88));
                    raw.texture = Preview.I.Portrait(r.look, r.lookStr);
                }
                var name = UIKit.Txt(row.transform, (mine ? "Ты — " : "") + r.nick, 30, mine ? UIKit.Ink : Color.white, TextAlignmentOptions.MidlineLeft, false);
                UIKit.Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(196, 0), new Vector2(480, 80));
                var val = UIKit.Txt(row.transform, Value(r), 30, mine ? UIKit.Ink : UIKit.Gold, TextAlignmentOptions.MidlineRight, false);
                UIKit.Place(val.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(380, 80));
                val.enableAutoSizing = true; val.fontSizeMin = 16; val.fontSizeMax = 30;
            }
            int myPlace = list.FindIndex(r => r.pid == me) + 1;
            w.Footer.text = $"Все, кто когда-либо играл: {list.Count} · твоё место: {myPlace} · ник меняется в настройках";
        }

        class Refresher : MonoBehaviour
        {
            int last; float t; Window win;
            void Start() { win = GetComponent<Window>(); last = Count(); }
            static int Count() => Online.I != null ? Online.I.Records.Count : 0;
            void Update()
            {
                if ((t += Time.unscaledDeltaTime) < 1.5f) return;
                t = 0;
                if (UIRoot.Open != win) return;
                int c = Count();
                if (c != last) { last = c; Window.Refresh(); }
            }
        }
    }
}
