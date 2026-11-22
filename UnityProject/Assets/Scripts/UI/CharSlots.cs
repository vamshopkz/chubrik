using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// 22.11: «ИГРАТЬ» on the title screen → pick a chubrik. Three big squares: your chubriks on see-through squares,
    /// free places with a big «+» (a new chubrik). Coins and things are shared, each chubrik has its own name and outfit.
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
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.05f, 0.14f, 0.34f, 0.55f), "CharSlots", true);
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
                if (i < Save.P.chars.Count) Filled(cell, i, size);
                else Empty(cell, size);
            }

            var tip = UIKit.Txt(dim.transform, "Монеты, вещи и спасённые друзья — общие. Имя и наряд у каждого чубрика свои", 28, Color.white);
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
            bool active = i == Save.P.activeChar;
            var sq = Square(cell, 0.25f, Color.white, active ? UIKit.Gold : new Color(1, 1, 1, 0.85f));
            var b = sq.gameObject.AddComponent<UIButton>();
            b.Bg = sq;
            int idx = i;
            b.OnClick = () => Pick(idx);

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
            else
            {
                // a small «×»: delete this chubrik (asks first)
                var del = UIKit.Round(sq.transform, "×", UIKit.Red, () => AskDelete(idx), 84, 52);
                UIKit.Place(del.Bg.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -30), new Vector2(84, 84));
            }
        }

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

        /// <summary>Play as chubrik <paramref name="i"/>: its outfit goes on, then the usual «Играть».</summary>
        public static void Pick(int i)
        {
            if (i < 0 || i >= Save.P.chars.Count) return;
            if (i != Save.P.activeChar)
            {
                Save.SwitchChar(i);
                PlayerCtl.I.ApplyLook(Save.CurrentLook());
                CharStand.Build();
            }
            Close();
            Menus.Play();
        }

        /// <summary>The «+»: a new chubrik in the starting outfit, played right away.</summary>
        public static void New()
        {
            if (Save.NewChar() < 0) return;
            PlayerCtl.I.ApplyLook(Save.CurrentLook());
            CharStand.Build();
            Close();
            Menus.Play();
            AudioFx.Play("buy");
            Hud.Toast("Новый чубрик! Наряди его в гардеробе", UIKit.Lime);
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
            var sub = UIKit.Txt(panel.transform, "Его имя и наряд пропадут. Монеты и вещи останутся", 28, Color.white);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(820, 90));
            var yes = UIKit.Button(panel.transform, "УДАЛИТЬ", UIKit.Red, () =>
            {
                if (Save.DeleteChar(i)) { AudioFx.Play("click"); CharStand.Build(); }
                Open();
            }, 36, "Yes");
            UIKit.Place(yes.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 34), new Vector2(340, 100));
            var no = UIKit.Button(panel.transform, "ОТМЕНА", UIKit.Green, () => { if (confirm) Object.Destroy(confirm); confirm = null; }, 36, "No");
            UIKit.Place(no.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 34), new Vector2(340, 100));
        }
    }
}
