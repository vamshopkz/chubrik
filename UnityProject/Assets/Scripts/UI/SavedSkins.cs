using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// v0.30: the wardrobe's «СОХРАНИТЬ СКИН» button (top left) opens «Мои скины»: save the whole outfit you have on,
    /// put a saved one on with one tap, delete with «×». Up to 12 saved outfits, shared by all your chubriks.
    /// </summary>
    public static class SavedSkins
    {
        public const int Max = 12;
        static GameObject root;
        public static bool IsOpen => root != null;
        static RectTransform grid;

        /// <summary>The outfit as "dept=item;dept=item…".</summary>
        public static string Pack(Look l) => Online.LookString(l);

        public static void Open()
        {
            Close();
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.05f, 0.08f, 0.2f, 0.72f), "SavedSkins", true);
            UIKit.Fill(dim.rectTransform);
            root = dim.gameObject;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 820));
            var title = UIKit.Txt(panel.transform, "МОИ СКИНЫ", 46, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -16), new Vector2(500, 64));
            var x = UIKit.Button(panel.transform, "X", UIKit.Red, Close, 34, "Close");
            UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(84, 70));
            var save = UIKit.Button(panel.transform, "СОХРАНИТЬ ЭТОТ СКИН", UIKit.Green, SaveCurrent, 28, "SaveCurrent");
            UIKit.Place(save.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-118, -16), new Vector2(420, 70));
            grid = UIKit.Rect("Grid", panel.transform);
            UIKit.Fill(grid, 30, 30, 30, 110);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(190, 300); gl.spacing = new Vector2(16, 16);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 6;
            Fill();
            AudioFx.Play("click");
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null;
        }

        static void Fill()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Object.Destroy(grid.GetChild(i).gameObject);
            var list = Save.P.outfits;
            if (list.Count == 0)
            {
                var t = UIKit.Txt(grid, "Тут пока пусто. Нажми «Сохранить этот скин» — и наряд, который сейчас на чубрике, сохранится здесь.", 30, Color.white, TextAlignmentOptions.TopLeft, false);
                t.rectTransform.sizeDelta = new Vector2(1180, 120);
                grid.GetComponent<GridLayoutGroup>().cellSize = new Vector2(1180, 120);
                return;
            }
            grid.GetComponent<GridLayoutGroup>().cellSize = new Vector2(190, 300);
            for (int i = 0; i < list.Count; i++)
            {
                int k = i;
                var o = list[i];
                var look = Online.ParseLook(o.look);
                var tile = UIKit.Img(grid, UIKit.Btn, Color.white, "Outfit " + i, true);
                var btn = tile.gameObject.AddComponent<UIButton>();
                btn.Bg = tile; btn.OnClick = () => Wear(k);
                var raw = new GameObject("Pic", typeof(RectTransform)).AddComponent<RawImage>();
                raw.transform.SetParent(tile.transform, false); raw.raycastTarget = false;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(174, 200));
                if (Preview.I != null) raw.texture = Preview.I.Portrait(look, "outfit:" + o.look);
                var nm = UIKit.Txt(tile.transform, o.name, 22, UIKit.Ink, TextAlignmentOptions.Center, false);
                UIKit.Place(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(180, 36));
                var wear = UIKit.Button(tile.transform, "НАДЕТЬ", UIKit.Green, () => Wear(k), 20, "Wear");
                UIKit.Place(wear.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(170, 46));
                var del = UIKit.Button(tile.transform, "×", UIKit.Red, () => Delete(k), 26, "Delete");
                UIKit.Place(del.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(46, 46));
            }
        }

        public static void SaveCurrent()
        {
            var list = Save.P.outfits;
            if (list.Count >= Max) { AudioFx.Play("error"); Hud.Toast($"Можно сохранить до {Max} скинов — удали один", UIKit.Orange); return; }
            var packed = Pack(Save.CurrentLook());
            if (list.Exists(o => o.look == packed)) { Hud.Toast("Этот скин уже сохранён", UIKit.Cyan); return; }
            int n = 1; while (list.Exists(o => o.name == "Скин " + n)) n++;
            list.Add(new SavedOutfit { name = "Скин " + n, look = packed });
            Save.Write();
            AudioFx.Play("token");
            Hud.Toast($"Скин сохранён: «Скин {n}»", UIKit.Lime);
            if (IsOpen) Fill();
        }

        /// <summary>Puts a saved outfit on (only things you still own; the rest of the departments stay as they are).</summary>
        public static void Wear(int i)
        {
            if (i < 0 || i >= Save.P.outfits.Count) return;
            var look = Online.ParseLook(Save.P.outfits[i].look);
            int missing = 0;
            foreach (var d in Catalog.Depts)
            {
                var id = look.Of(d.id);
                if (string.IsNullOrEmpty(id)) { if (d.optional) Save.Wear(d.id, ""); continue; }
                if (Save.Owns(id)) Save.Wear(d.id, id); else missing++;
            }
            PlayerCtl.I?.ApplyLook(Save.CurrentLook());
            WardrobeUI.Changed();
            AudioFx.Play("buy");
            Hud.Toast(missing == 0 ? $"Надет «{Save.P.outfits[i].name}»" : $"Надет «{Save.P.outfits[i].name}» (вещей, которых у тебя нет: {missing})", UIKit.Lime);
            Close();
        }

        static void Delete(int i)
        {
            if (i < 0 || i >= Save.P.outfits.Count) return;
            Save.P.outfits.RemoveAt(i);
            Save.Write();
            AudioFx.Play("click");
            Fill();
        }
    }
}
