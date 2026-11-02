using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// The wardrobe: live 3D preview of the chubrik (drag to turn), 31 departments, item tiles with pictures,
    /// try-on, buy with coins / tokens, wear, take off. Effect departments play their effect on the mannequin.
    /// </summary>
    public static class WardrobeUI
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        static string dept = "head";
        static Item sel;
        static RectTransform deptList, grid;
        static TextMeshProUGUI money, selName, selInfo;
        static UIButton action, takeOff, test;
        static readonly Dictionary<string, UIButton> deptBtns = new Dictionary<string, UIButton>();
        static readonly List<(Item it, Image frame, TextMeshProUGUI status)> tiles = new List<(Item, Image, TextMeshProUGUI)>();
        static readonly HashSet<string> NoPicture = new HashSet<string> { "jsnd", "lsnd", "dmg", "win", "walk", "emote", "shadow", "steps" };

        public static void Open(string startDept = null)
        {
            Window.Close();
            Close();
            Menus.Unlock();
            if (startDept != null) dept = startDept;
            PlayerCtl.I.Frozen = true;
            Preview.I.Show(true);
            Preview.I.SetLook(Save.CurrentLook());
            Preview.I.Yaw = 20;

            var dim = UIKit.Img(UIRoot.WinLayer, UIKit.White, new Color(0.05f, 0.1f, 0.25f, 0.55f), "Wardrobe", true);
            UIKit.Fill(dim.rectTransform);
            dim.transform.SetAsFirstSibling();
            root = dim.gameObject;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1570, 870));
            var P = panel.transform;

            var title = UIKit.Txt(P, "ГАРДЕРОБ", 44, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -14), new Vector2(400, 60));
            money = UIKit.Txt(P, "", 30, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(money.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, -16), new Vector2(700, 56));
            var close = UIKit.Button(P, "ГОТОВО", UIKit.Green, Close, 30);
            UIKit.Place(close.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -14), new Vector2(210, 66));

            // preview
            var pv = new GameObject("Preview", typeof(RectTransform)).AddComponent<RawImage>();
            pv.transform.SetParent(P, false);
            pv.texture = Preview.I.RT;
            UIKit.Place(pv.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -92), new Vector2(448, 560));
            pv.gameObject.AddComponent<PreviewDrag>();
            var hintT = UIKit.Txt(pv.transform, "крути пальцем", 20, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.Bottom, false);
            UIKit.Place(hintT.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(400, 28));
            selName = UIKit.Txt(P, "", 30, Color.white, TextAlignmentOptions.TopLeft);
            UIKit.Place(selName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -660), new Vector2(450, 44));
            selName.enableAutoSizing = true; selName.fontSizeMin = 18; selName.fontSizeMax = 30;
            selInfo = UIKit.Txt(P, "", 22, UIKit.Muted, TextAlignmentOptions.TopLeft, false);
            UIKit.Place(selInfo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -702), new Vector2(450, 34));
            action = UIKit.Button(P, "", UIKit.Green, OnAction, 28);
            UIKit.Place(action.Bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(300, 84));
            takeOff = UIKit.Button(P, "СНЯТЬ", UIKit.Red, OnTakeOff, 26);
            UIKit.Place(takeOff.Bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(334, 24), new Vector2(140, 84));
            test = UIKit.Button(P, "ТЕСТ", UIKit.Violet, Test, 24);
            UIKit.Place(test.Bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(360, -100), new Vector2(104, 56));

            // departments
            deptList = ScrollList(P, new Vector2(490, -92), new Vector2(320, 760), true);
            deptBtns.Clear();
            foreach (var d in Catalog.Depts)
            {
                var dd = d;
                var b = UIKit.Button(deptList, d.name, UIKit.Blue, () => SelectDept(dd.id), 22, "Dept " + d.id);
                b.Bg.gameObject.AddComponent<LayoutElement>().minHeight = 58;
                b.Label.alignment = TextAlignmentOptions.MidlineLeft;
                deptBtns[d.id] = b;
            }

            // items
            grid = ScrollList(P, new Vector2(826, -92), new Vector2(720, 760), false);
            SelectDept(dept);
            Refresh();
        }

        static RectTransform ScrollList(Transform parent, Vector2 pos, Vector2 size, bool vertical)
        {
            var bg = UIKit.Img(parent, UIKit.Glass, Color.white, "Scroll", true);
            UIKit.Place(bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), pos, size);
            var view = UIKit.Rect("View", bg.transform);
            UIKit.Fill(view, 8, 8, 8, 8);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0.001f);
            var content = UIKit.Rect("Content", view);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            if (vertical)
            {
                var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vl.spacing = 6; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.padding = new RectOffset(4, 4, 4, 4);
            }
            else
            {
                var gl = content.gameObject.AddComponent<GridLayoutGroup>();
                gl.cellSize = new Vector2(166, 214); gl.spacing = new Vector2(10, 10); gl.padding = new RectOffset(6, 6, 6, 6);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            }
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.horizontal = false; sr.viewport = view; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            return content;
        }

        public static void SelectDept(string id)
        {
            dept = id;
            var d = Catalog.Dept(id);
            foreach (var kv in deptBtns) kv.Value.Bg.color = kv.Key == id ? UIKit.Orange : UIKit.Blue;
            for (int i = grid.childCount - 1; i >= 0; i--) Object.Destroy(grid.GetChild(i).gameObject);
            tiles.Clear();
            var raws = new Dictionary<string, RawImage>();
            foreach (var it in d.items)
            {
                var item = it;
                var tile = UIKit.Img(grid, UIKit.Btn, Color.white, "Tile " + it.id, true);
                var btn = tile.gameObject.AddComponent<UIButton>();
                btn.Bg = tile; btn.OnClick = () => Select(item);
                var raw = new GameObject("Pic", typeof(RectTransform)).AddComponent<RawImage>();
                raw.transform.SetParent(tile.transform, false);
                raw.raycastTarget = false;
                UIKit.Place(raw.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(150, 132));
                if (NoPicture.Contains(it.dept))
                {
                    raw.color = Color.HSVToRGB((Catalog.Index(it.id) + 1) / 9f, 0.45f, 0.95f);
                    var word = it.dept switch { "jsnd" => "ПРЫЖОК", "lsnd" => "ПРИЗЕМЛ.", "dmg" => "УРОН", "win" => "ПОБЕДА", "walk" => "ПОХОДКА", "emote" => "ЭМОЦИЯ", "shadow" => "ТЕНЬ", _ => "СЛЕДЫ" };
                    var big = UIKit.Txt(raw.transform, word + "\n" + (it.Premium ? "★" : (Catalog.Index(it.id) + 1).ToString()), 26, Color.white);
                    UIKit.Fill(big.rectTransform, 4, 4, 4, 4);
                }
                else raws[it.id] = raw;
                var nm = UIKit.Txt(tile.transform, it.name, 18, UIKit.Ink, TextAlignmentOptions.Center, false);
                UIKit.Place(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(158, 48));
                nm.enableAutoSizing = true; nm.fontSizeMin = 12; nm.fontSizeMax = 18;
                var st = UIKit.Txt(tile.transform, "", 18, Color.white, TextAlignmentOptions.Center);
                UIKit.Place(st.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(158, 28));
                var frame = UIKit.Img(tile.transform, UIKit.Btn, new Color(1, 1, 1, 0), "Frame");
                UIKit.Fill(frame.rectTransform, -5, -5, -5, -5);
                frame.transform.SetAsFirstSibling();
                tiles.Add((it, frame, st));
            }
            var list = new List<Item>();
            foreach (var it in d.items) if (raws.ContainsKey(it.id)) list.Add(it);
            Preview.I.Thumbs(list, (it, tex) => { if (raws.TryGetValue(it.id, out var r) && r) r.texture = tex; });
            Preview.I.View = d.view == "fx" ? "body" : d.view;
            var worn = Catalog.Get(Save.Worn(id));
            sel = worn ?? (d.items.Count > 0 ? d.items[0] : null);
            Preview.I.SetLook(Save.CurrentLook());
            Preview.I.Walk = id == "walk" || id == "steps";
            Refresh();
        }

        static void Select(Item it)
        {
            sel = it;
            var l = Save.CurrentLook();
            l[it.dept] = it.id;
            Preview.I.SetLook(l);
            Test();
            Refresh();
        }

        /// <summary>Plays the selected effect / sound on the mannequin.</summary>
        static void Test()
        {
            if (sel == null) return;
            var m = Preview.I.Model;
            int i = Mathf.Max(0, Catalog.Index(sel.id));
            switch (sel.dept)
            {
                case "jsnd": AudioFx.Jump(i, false); Preview.I.Anim.Land(0.5f); break;
                case "lsnd": AudioFx.Land(i); Preview.I.Anim.Land(1f); break;
                case "dmg": Effects.Damage(i, m); break;
                case "win": Effects.Victory(i, m); break;
                case "emote": Preview.I.Anim.emote = i; Preview.I.Anim.PlayEmote(); break;
                case "weapon": Preview.I.Anim.Attack(); break;
                default: AudioFx.Play("click"); break;
            }
        }

        static void Refresh()
        {
            if (!IsOpen) return;
            var P = Save.P;
            money.text = $"Монеты: {UIKit.Num(P.coins)}    Жетоны: {P.tokens}";
            foreach (var (it, frame, st) in tiles)
            {
                bool worn = Save.Worn(it.dept) == it.id, own = Save.Owns(it.id);
                frame.color = it == sel ? UIKit.Gold : worn ? UIKit.Green : new Color(1, 1, 1, 0);
                if (worn) { st.text = "НАДЕТО"; st.color = UIKit.Green; }
                else if (own) { st.text = "ЕСТЬ"; st.color = UIKit.Cyan; }
                else if (it.Premium) { st.text = "★ НАБОР"; st.color = UIKit.Pink; }
                else if (it.promoOnly) { st.text = "ПРОМОКОД"; st.color = UIKit.Violet; }
                else if (it.tokens) { st.text = it.price + " ЖЕТ."; st.color = UIKit.Violet; }
                else { st.text = it.price + " МОНЕТ"; st.color = P.coins >= it.price ? UIKit.Gold : UIKit.Red; }
                var tileImg = frame.transform.parent.GetComponent<Image>();
                tileImg.color = own ? Color.white : new Color(0.82f, 0.86f, 0.92f);
            }
            var d = Catalog.Dept(dept);
            if (sel == null) { selName.text = ""; selInfo.text = ""; action.gameObject.SetActive(false); takeOff.gameObject.SetActive(false); return; }
            selName.text = sel.name;
            selInfo.text = sel.Premium ? $"Премиум-набор «{Catalog.Sets[sel.set].name}»" : d.name;
            bool isWorn = Save.Worn(sel.dept) == sel.id, owns = Save.Owns(sel.id);
            action.gameObject.SetActive(true);
            action.Interactable = true;
            if (isWorn) { action.Set("НАДЕТО", new Color(0.4f, 0.5f, 0.6f)); action.Interactable = false; }
            else if (owns) action.Set("НАДЕТЬ", UIKit.Green);
            else if (sel.Premium) action.Set("ПРЕМИУМ-НАБОР", UIKit.Pink);
            else if (sel.promoOnly) action.Set("ПРОМОКОД", UIKit.Violet);
            else if (sel.tokens) action.Set($"КУПИТЬ: {sel.price} ЖЕТ.", P.tokens >= sel.price ? UIKit.Violet : new Color(0.4f, 0.4f, 0.5f));
            else action.Set($"КУПИТЬ: {sel.price}", P.coins >= sel.price ? UIKit.Orange : new Color(0.4f, 0.4f, 0.5f));
            takeOff.gameObject.SetActive(d.optional && !string.IsNullOrEmpty(Save.Worn(dept)));
            test.gameObject.SetActive(NoPicture.Contains(dept) || dept == "weapon");
        }

        static void OnAction()
        {
            if (sel == null) return;
            var P = Save.P;
            if (Save.Worn(sel.dept) == sel.id) return;
            if (!Save.Owns(sel.id))
            {
                if (sel.Premium) { AudioFx.Play("error"); Hud.Toast($"Набор «{Catalog.Sets[sel.set].name}» — в золотом премиум-киоске у фонтана", UIKit.Gold); return; }
                if (sel.promoOnly) { AudioFx.Play("error"); Hud.Toast("Промокод вводится в золотом премиум-киоске", UIKit.Violet); return; }
                bool paid = sel.tokens ? Save.SpendTokens(sel.price) : Save.Spend(sel.price);
                if (!paid)
                {
                    AudioFx.Play("error");
                    Hud.Toast(sel.tokens ? "Не хватает жетонов — ищи их в лобби" : "Не хватает монет — собирай их в лобби или открой лутбокс", UIKit.Red);
                    return;
                }
                Save.Give(sel.id);
                AudioFx.Play("buy");
                Effects.Victory(1, Preview.I.Model);
            }
            Save.Wear(sel.dept, sel.id);
            Preview.I.SetLook(Save.CurrentLook());
            Refresh();
        }

        static void OnTakeOff()
        {
            Save.Wear(dept, "");
            Preview.I.SetLook(Save.CurrentLook());
            sel = null;
            Refresh();
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null;
            if (Preview.I) { Preview.I.Show(false); Preview.I.Walk = false; }
            if (PlayerCtl.I) { PlayerCtl.I.ApplyLook(Save.CurrentLook()); PlayerCtl.I.Frozen = false; }
        }

        /// <summary>Re-reads coins / ownership after a purchase made elsewhere (promo code, premium).</summary>
        public static void Changed() { if (IsOpen) Refresh(); }

        class PreviewDrag : MonoBehaviour, IDragHandler
        {
            public void OnDrag(PointerEventData e) { Preview.I.Yaw -= e.delta.x * 0.6f; }
        }
    }
}
