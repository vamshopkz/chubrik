using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// 22.11: «ИГРАТЬ» on the title screen → pick a chubrik. Three big squares: your chubriks on see-through squares,
    /// free places with a big «+» (a new chubrik). Each chubrik has its own progress (coins, things, friends,
    /// worlds); picking another one rebuilds the game for it (<see cref="Boot.Restart"/>). This is the only place
    /// to choose whom to play — the other chubriks don't stand anywhere in the game.
    /// </summary>
    public static class CharSlots
    {
        static GameObject root;
        static GameObject confirm;
        public static bool IsOpen => root != null;
        static RectTransform slotsRow;
        /// <summary>How many squares the picker shows (autotest).</summary>
        public static int Squares => slotsRow ? slotsRow.childCount : 0;

        public static void Open()
        {
            Close();
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.05f, 0.12f, 0.3f, 0.96f), "CharSlots", true);
            UIKit.Fill(dim.rectTransform);
            root = dim.gameObject;
            var safe = UIRoot.Safe();

            var title = UIKit.Txt(dim.transform, "ВЫБЕРИ ЧУБРИКА", 76, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24 - safe.w), new Vector2(1100, 100));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 76;

            var back = UIKit.Button(dim.transform, "НАЗАД", UIKit.Blue, Close, 34, "Back");
            UIKit.Place(back.Bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24 + safe.x, -26 - safe.w), new Vector2(230, 92));

            // three squares side by side; narrower screens (tablets 4:3) get smaller squares
            float width = UIRoot.Root.rect.width > 10 ? UIRoot.Root.rect.width : 1600;
            float gap = 50, size = Mathf.Clamp((width - safe.x - safe.z - 80 - gap * 2) / 3f, 300, 430);
            var row = UIKit.Rect("Slots", dim.transform);
            UIKit.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(size * 3 + gap * 2, size));
            slotsRow = row;
            for (int i = 0; i < Save.MaxChars; i++)
            {
                var cell = UIKit.Rect("Slot " + (i + 1), row);
                UIKit.Place(cell, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * (size + gap), 0), new Vector2(size, size));
                if (i < Save.CharCount) Filled(cell, i, size);
                else Empty(cell, size);
            }

            var tip = UIKit.Txt(dim.transform, "У каждого чубрика всё своё: монеты, вещи, друзья и пройденные миры", 28, Color.white);
            UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30 + safe.y), new Vector2(Mathf.Min(1300, width - 80), 80));
        }

        /// <summary>The frame of a square: see-through fill + white rim (gold for the one played now).</summary>
        static Image Square(RectTransform cell, float alpha, Color tint, Color rim)
        {
            var fill = UIKit.Img(cell, UIKit.Btn, new Color(tint.r, tint.g, tint.b, alpha), "Square", true);
            UIKit.Fill(fill.rectTransform);
            var frame = UIKit.Img(fill.transform, UIKit.BtnFrame, rim, "Rim");
            UIKit.Fill(frame.rectTransform, -3, -3, -3, -3);
            return fill;
        }

        static void Filled(RectTransform cell, int i, float size)
        {
            bool active = i == Save.ActiveIndex;
            var sq = Square(cell, 0.25f, Color.white, active ? UIKit.Gold : new Color(1, 1, 1, 0.85f));
            var b = sq.gameObject.AddComponent<UIButton>();
            b.Bg = sq;
            int idx = i;
            b.OnClick = () => Choose(idx);

            // the picture with rounded corners
            var mask = UIKit.Img(sq.transform, UIKit.Btn, Color.white, "Picture");
            UIKit.Place(mask.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(size - 106, size - 106));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var raw = new GameObject("Skin", typeof(RectTransform)).AddComponent<RawImage>();
            raw.transform.SetParent(mask.transform, false);
            raw.raycastTarget = false;
            UIKit.Fill(raw.rectTransform);   // the whole chubrik, head to feet
            if (Preview.I != null)
            {
                var look = Save.CharLook(i);
                raw.texture = Preview.I.Portrait(look, "char|" + Online.LookString(look), 384);
            }

            var name = UIKit.Txt(sq.transform, Save.CharName(i), 40, Color.white);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(size - 30, 70));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true; name.fontSizeMin = 22; name.fontSizeMax = 40;

            if (active)
            {
                var now = UIKit.Img(sq.transform, UIKit.Pill != null ? UIKit.Pill : UIKit.Btn, UIKit.Gold, "Now");
                UIKit.Place(now.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(170, 50));
                var nt = UIKit.Txt(now.transform, "СЕЙЧАС", 26, Color.white);
                UIKit.Fill(nt.rectTransform, 6, 2, 6, 2);
            }
            // v0.31: no «×» on the chubrik any more — a tap opens the frame «Играть / Удалить / Ник» with the progress line
            Bar(sq.transform, Save.CharProgress(i, out _), new Vector2(0, 4), size - 60, 12);
        }

        /// <summary>A progress line: a dark track with a green-to-gold fill.</summary>
        static void Bar(Transform parent, float k, Vector2 pos, float w, float h)
        {
            var track = UIKit.Img(parent, UIKit.Pill != null ? UIKit.Pill : UIKit.Btn, new Color(0.05f, 0.1f, 0.25f, 0.7f), "Progress");
            UIKit.Place(track.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, new Vector2(w, h));
            var fill = UIKit.Img(track.transform, UIKit.Pill != null ? UIKit.Pill : UIKit.Btn, Color.Lerp(UIKit.Green, UIKit.Gold, k), "Fill");
            var rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(Mathf.Max(0.04f, k), 1);
            rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
        }

        /// <summary>The frame over a tapped chubrik: its picture, name, progress line, «ИГРАТЬ», «УДАЛИТЬ» and a new nickname.</summary>
        public static void Choose(int i)
        {
            if (root == null || i < 0 || i >= Save.CharCount) return;
            if (confirm) Object.Destroy(confirm);
            AudioFx.Play("click");
            var dim = UIKit.Img(root.transform, UIKit.White, new Color(0.02f, 0.06f, 0.18f, 0.6f), "Choice", true);
            UIKit.Fill(dim.rectTransform);
            confirm = dim.gameObject;
            var close = dim.gameObject.AddComponent<UIButton>(); close.Bg = dim; close.OnClick = CloseChoice;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, 640));
            var P = panel.transform;
            var x = UIKit.Button(P, "X", UIKit.Red, CloseChoice, 32, "Close");
            UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(80, 68));
            // the picture
            var mask = UIKit.Img(P, UIKit.Btn, Color.white, "Picture");
            UIKit.Place(mask.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(300, 300));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var raw = new GameObject("Skin", typeof(RectTransform)).AddComponent<RawImage>();
            raw.transform.SetParent(mask.transform, false); raw.raycastTarget = false;
            UIKit.Fill(raw.rectTransform);
            if (Preview.I != null) { var look = Save.CharLook(i); raw.texture = Preview.I.Portrait(look, "char|" + Online.LookString(look), 384); }
            var name = UIKit.Txt(P, Save.CharName(i) + (i == Save.ActiveIndex ? "  (сейчас)" : ""), 48, UIKit.Gold, TMPro.TextAlignmentOptions.MidlineLeft);
            UIKit.Place(name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -36), new Vector2(560, 70));
            name.enableAutoSizing = true; name.fontSizeMin = 26; name.fontSizeMax = 48;
            float k = Save.CharProgress(i, out var about);
            var pt = UIKit.Txt(P, $"Прогресс: {Mathf.RoundToInt(k * 100)}%", 30, Color.white, TMPro.TextAlignmentOptions.MidlineLeft);
            UIKit.Place(pt.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -116), new Vector2(640, 46));
            var track = UIKit.Rect("BarHolder", P);
            UIKit.Place(track, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -170), new Vector2(660, 30));
            Bar(track, k, new Vector2(0, 0), 660, 30);
            var ab = UIKit.Txt(P, about, 26, UIKit.Muted, TMPro.TextAlignmentOptions.MidlineLeft);
            UIKit.Place(ab.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -214), new Vector2(660, 44));
            ab.enableAutoSizing = true; ab.fontSizeMin = 18; ab.fontSizeMax = 26;
            // a new nickname for this chubrik (each one has its own)
            var input = UIKit.Input(P, "Ник этого чубрика", 16, 30);
            input.text = i == Save.ActiveIndex ? Save.P.nick : "";
            UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -270), new Vector2(420, 80));
            int idx = i;
            var ok = UIKit.Button(P, "СМЕНИТЬ НИК", UIKit.Blue, () =>
            {
                var n = Online.Clean(input.text);
                if (n.Length > 16) n = n.Substring(0, 16);
                Save.SetCharNick(idx, n);
                AudioFx.Play("buy");
                Hud.Toast(n.Length > 0 ? "Новый ник: " + n : "Ник сброшен", UIKit.Green);
                Open(); Choose(idx);
            }, 26, "SetNick");
            UIKit.Place(ok.Bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(796, -270), new Vector2(230, 80));
            var play = UIKit.Button(P, "ИГРАТЬ", UIKit.Green, () => Pick(idx), 44, "Play");
            UIKit.Place(play.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-230, 40), new Vector2(420, 120));
            var del = UIKit.Button(P, "УДАЛИТЬ", UIKit.Red, () => AskDelete(idx), 40, "Delete");
            UIKit.Place(del.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(230, 40), new Vector2(420, 120));
        }

        public static bool ChoiceOpen => confirm != null;
        static void CloseChoice() { if (confirm) Object.Destroy(confirm); confirm = null; }

        static void Empty(RectTransform cell, float size)
        {
            var sq = Square(cell, 0.5f, new Color(0.75f, 0.88f, 1f), new Color(1, 1, 1, 0.9f));
            var b = sq.gameObject.AddComponent<UIButton>();
            b.Bg = sq;
            b.OnClick = New;
            var plus = UIKit.Txt(sq.transform, "+", size * 0.6f, Color.white);
            UIKit.Place(plus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(size, size * 0.7f));
            plus.overflowMode = TextOverflowModes.Overflow;
            var t = UIKit.Txt(sq.transform, "НОВЫЙ ЧУБРИК", 34, Color.white);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(size - 30, 64));
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true; t.fontSizeMin = 20; t.fontSizeMax = 34;
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null; confirm = null;
        }

        /// <summary>Play as chubrik <paramref name="i"/>: the same one — just «Играть»; another one — its own
        /// progress is loaded and the game is built again for it, then «Играть».</summary>
        public static void Pick(int i)
        {
            if (i < 0 || i >= Save.CharCount) return;
            Close();
            if (i == Save.ActiveIndex) { Menus.Play(); return; }
            Boot.Restart(() => Save.SwitchChar(i), () =>
            {
                StartPlaying();
                Hud.Toast($"Ты играешь за: {Save.CharName(Save.ActiveIndex)}!", UIKit.Lime);
            });
        }

        /// <summary>The «+»: a new chubrik with a brand new progress (50 coins, nothing found yet), played right away.</summary>
        public static void New()
        {
            if (Save.CharCount >= Save.MaxChars) return;
            Close();
            Boot.Restart(() => Save.NewChar(), () =>
            {
                StartPlaying();
                AudioFx.Play("buy");
                Hud.Toast("Новый чубрик! Всё с самого начала — наряди его в гардеробе", UIKit.Lime);
            });
        }

        /// <summary>After the rebuild: like pressing «ИГРАТЬ» on the title screen (the start of the lobby).</summary>
        static void StartPlaying()
        {
            if (PlayerCtl.I != null) PlayerCtl.I.MenuOrbit = true;
            Menus.Play();
        }

        /// <summary>v0.31: deletes chubrik <paramref name="i"/>; the played one too — then the game is rebuilt for another chubrik.</summary>
        public static void Delete(int i)
        {
            if (i != Save.ActiveIndex) { if (Save.DeleteChar(i)) AudioFx.Play("click"); Open(); return; }
            Close();
            Boot.Restart(() => Save.DeleteActive(), () =>
            {
                Open();
                Hud.Toast("Чубрик удалён", UIKit.Orange);
            });
        }

        static void AskDelete(int i)
        {
            if (confirm) Object.Destroy(confirm);
            var dim = UIKit.Img(root.transform, UIKit.White, new Color(0.02f, 0.06f, 0.18f, 0.6f), "Confirm", true);
            UIKit.Fill(dim.rectTransform);
            confirm = dim.gameObject;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 440));
            var q = UIKit.Txt(panel.transform, $"Удалить чубрика «{Save.CharName(i)}»?", 44, UIKit.Gold);
            UIKit.Place(q.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(840, 120));
            var sub = UIKit.Txt(panel.transform, "Пропадёт всё, что у него есть: монеты, вещи, друзья и пройденные миры", 28, Color.white);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(820, 90));
            var yes = UIKit.Button(panel.transform, "УДАЛИТЬ", UIKit.Red, () => Delete(i), 36, "Yes");
            UIKit.Place(yes.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 34), new Vector2(340, 100));
            var no = UIKit.Button(panel.transform, "ОТМЕНА", UIKit.Green, () => { if (confirm) Object.Destroy(confirm); confirm = null; }, 36, "No");
            UIKit.Place(no.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 34), new Vector2(340, 100));
        }
    }
}
