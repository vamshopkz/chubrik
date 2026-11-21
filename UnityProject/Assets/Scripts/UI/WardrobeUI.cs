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

        static RectTransform leftPanel, rightPanel;
        static ChubrikModel M => PlayerCtl.I.Model;
        static ChubrikAnim A => PlayerCtl.I.Anim;

        /// <summary>17.11: the try-on goes onto the real chubrik in the world.</summary>
        static void TryOn(Look l) { PlayerCtl.I.ApplyLook(l); }

        public static void Open(string startDept = null)
        {
            Window.Close();
            Close();
            Menus.Unlock();
            if (startDept != null) dept = startDept;
            PlayerCtl.I.Frozen = true;
            PlayerCtl.I.StartWardrobeView();
            TryOn(Save.CurrentLook());
            var safe = UIRoot.Safe();

            // no dark curtain: the chubrik stands in the world between the panels
            var rootRt = UIKit.Rect("Wardrobe", UIRoot.WinLayer);
            UIKit.Fill(rootRt);
            rootRt.SetAsFirstSibling();
            root = rootRt.gameObject;
            var drag = UIKit.Img(rootRt, UIKit.White, new Color(0, 0, 0, 0.001f), "Drag", true);
            UIKit.Fill(drag.rectTransform);
            drag.gameObject.AddComponent<PreviewDrag>();
            var P = rootRt;

            var bar = UIKit.Img(P, UIKit.White, new Color(0.07f, 0.14f, 0.34f, 1f), "TopBar", true);
            UIKit.Place(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(4000, 80));
            var title = UIKit.Txt(P, "ГАРДЕРОБ", 44, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20 + safe.x, -10), new Vector2(300, 60));
            money = UIKit.Txt(P, "", 28, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(money.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(330 + safe.x, -12), new Vector2(760, 56));
            money.enableAutoSizing = true; money.fontSizeMin = 16; money.fontSizeMax = 28;
            var close = UIKit.Button(P, "ГОТОВО", UIKit.Green, Close, 30);
            UIKit.Place(close.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14 - safe.z, -7), new Vector2(210, 66));

            // left: the departments, the lucky block button under them
            var lp = UIKit.Img(P, UIKit.Panel, new Color(1, 1, 1, 0.92f), "Left", true);
            leftPanel = UIKit.Place(lp.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12 + safe.x, -90), new Vector2(300, 700));
            deptList = ScrollList(lp.transform, new Vector2(10, -10), new Vector2(280, 680), true);
            deptBtns.Clear();
            foreach (var d in Catalog.Depts)
            {
                var dd = d;
                var b = UIKit.Button(deptList, d.name, UIKit.Blue, () => SelectDept(dd.id), 22, "Dept " + d.id);
                b.Bg.gameObject.AddComponent<LayoutElement>().minHeight = 58;
                b.Label.alignment = TextAlignmentOptions.MidlineLeft;
                deptBtns[d.id] = b;
            }
            var lucky = UIKit.Button(P, "ОТКРЫТЬ ЛАКИ-БЛОК", UIKit.Orange, LuckyUI.Open, 24, "LuckyButton");
            UIKit.Place(lucky.Bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12 + safe.x, 14 + safe.y), new Vector2(300, 86));
            lucky.Bg.gameObject.AddComponent<Rainbow>();

            // right: the skins and under them the buy / wear button
            var rp = UIKit.Img(P, UIKit.Panel, new Color(1, 1, 1, 0.92f), "Right", true);
            rightPanel = UIKit.Place(rp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12 - safe.z, -90), new Vector2(570, 796 - safe.y));
            var R = rp.transform;
            grid = ScrollList(R, new Vector2(10, -10), new Vector2(550, 560), false);
            selName = UIKit.Txt(R, "", 30, Color.white, TextAlignmentOptions.TopLeft);
            UIKit.Place(selName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -580), new Vector2(430, 44));
            selName.enableAutoSizing = true; selName.fontSizeMin = 18; selName.fontSizeMax = 30;
            selInfo = UIKit.Txt(R, "", 22, UIKit.Muted, TextAlignmentOptions.TopLeft, false);
            UIKit.Place(selInfo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -624), new Vector2(530, 34));
            selInfo.enableAutoSizing = true; selInfo.fontSizeMin = 14; selInfo.fontSizeMax = 22;
            test = UIKit.Button(R, "ТЕСТ", UIKit.Violet, Test, 24);
            UIKit.Place(test.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -576), new Vector2(104, 52));
            action = UIKit.Button(R, "", UIKit.Green, OnAction, 28, "ActionButton");
            UIKit.Place(action.Bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 14), new Vector2(390, 84));
            takeOff = UIKit.Button(R, "СНЯТЬ", UIKit.Red, OnTakeOff, 26);
            UIKit.Place(takeOff.Bg.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 14), new Vector2(150, 84));

            var offAll = UIKit.Button(P, "СНЯТЬ ВСЕ СКИНЫ", UIKit.Red, TakeOffAll, 22, "TakeOffAll");
            UIKit.Place(offAll.Bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(322 + safe.x, 14 + safe.y), new Vector2(250, 86));

            var hintT = UIKit.Txt(P, "крути чубрика пальцем", 22, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Bottom);
            UIKit.Place(hintT.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 112 + safe.y), new Vector2(400, 30));
            root.AddComponent<CenterKeeper>().hint = hintT.rectTransform;

            SelectDept(dept);
            Refresh();
        }

        /// <summary>Keeps the chubrik in the middle of the free space between the two panels (any screen width).</summary>
        class CenterKeeper : MonoBehaviour
        {
            public RectTransform hint;
            readonly Vector3[] c = new Vector3[4];
            void LateUpdate()
            {
                if (!leftPanel || !rightPanel || PlayerCtl.I == null) return;
                leftPanel.GetWorldCorners(c); float l = c[2].x;
                rightPanel.GetWorldCorners(c); float r = c[0].x;
                float cx = (l + r) * 0.5f / Mathf.Max(1, Screen.width);
                PlayerCtl.I.WardrobeCx = cx;
                if (hint) hint.position = new Vector3((l + r) * 0.5f, hint.position.y, 0);
            }
        }

        /// <summary>A slowly shifting rainbow tint (the lucky block button).</summary>
        public class Rainbow : MonoBehaviour
        {
            Image img;
            void Start() { img = GetComponent<Image>(); }
            void Update() { if (img) img.color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.15f, 1), 0.55f, 1f); }
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
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
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
            foreach (var kv in deptBtns) kv.Value.Bg.color = kv.Key == id ? UIKit.BlueSel : UIKit.Blue;
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
                    raw.color = Catalog.TintOf(it.id) is Color tc ? Color.Lerp(tc, Color.white, 0.25f) : Color.HSVToRGB((Catalog.Index(it.id) % 100 + 1) / 9f, 0.45f, 0.95f);
                    var word = it.dept switch { "jsnd" => "ПРЫЖОК", "lsnd" => "ПРИЗЕМЛ.", "dmg" => "УРОН", "win" => "ПОБЕДА", "walk" => "ПОХОДКА", "emote" => "ЭМОЦИЯ", "shadow" => "ТЕНЬ", _ => "СЛЕДЫ" };
                    var big = UIKit.Txt(raw.transform, word + "\n" + (it.Premium || it.Reward ? "★" : (Catalog.Index(it.id) + 1).ToString()), 26, Color.white);
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
            PlayerCtl.I.WardrobeZoom = d.view == "fx" ? "body" : d.view;
            var worn = Catalog.Get(Save.Worn(id));
            sel = worn ?? (d.items.Count > 0 ? d.items[0] : null);
            TryOn(Save.CurrentLook());
            PlayerCtl.I.ShowWalk = id == "walk" || id == "steps";
            Refresh();
        }

        static void Select(Item it)
        {
            sel = it;
            var l = Save.CurrentLook();
            l[it.dept] = it.id;
            TryOn(l);
            Test();
            Refresh();
        }

        /// <summary>Plays the selected effect / sound on the mannequin.</summary>
        static void Test()
        {
            if (sel == null) return;
            var m = M;
            int i = Mathf.Max(0, Catalog.Index(sel.id));
            switch (sel.dept)
            {
                case "jsnd": AudioFx.Jump(i, false, Catalog.PitchOf(sel.id)); A.Land(0.5f); break;
                case "lsnd": AudioFx.Land(i, Catalog.PitchOf(sel.id)); A.Land(1f); break;
                case "dmg": Effects.Damage(i, m); break;
                case "win": Effects.Victory(i, m); break;
                case "emote": A.emote = i; A.PlayEmote(); break;
                case "weapon": A.Attack(); break;
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
                else if (it.Reward) { st.text = "НАГРАДА"; st.color = UIKit.Gold; }
                else if (it.promoOnly) { st.text = "ПРОМОКОД"; st.color = UIKit.Violet; }
                else { int r = Lucky.Rarity(it); st.text = Lucky.RarityName[r]; st.color = Lucky.RarityColor[r]; }
                var tileImg = frame.transform.parent.GetComponent<Image>();
                tileImg.color = own ? Color.white : new Color(0.82f, 0.86f, 0.92f);
            }
            var d = Catalog.Dept(dept);
            if (sel == null) { selName.text = ""; selInfo.text = ""; action.gameObject.SetActive(false); takeOff.gameObject.SetActive(false); return; }
            selName.text = sel.name;
            selInfo.text = sel.Premium ? $"Премиум-набор «{Catalog.Sets[sel.set].name}»" : sel.Reward ? "Награда: " + sel.reward : Lucky.InPool(sel) ? d.name + " · " + Lucky.RarityName[Lucky.Rarity(sel)].ToLower() : d.name;
            var pm = M;
            if (pm && pm.look.Of(sel.dept) == sel.id && pm.PartCount.TryGetValue(sel.dept, out var pc) && pc >= 20) selInfo.text += $" · {pc} деталей";
            bool isWorn = Save.Worn(sel.dept) == sel.id, owns = Save.Owns(sel.id);
            action.gameObject.SetActive(true);
            action.Interactable = true;
            if (isWorn) { action.Set("НАДЕТО", new Color(0.4f, 0.5f, 0.6f)); action.Interactable = false; }
            else if (owns) action.Set("НАДЕТЬ", UIKit.Green);
            else if (sel.Premium) action.Set("ПРЕМИУМ-НАБОР", UIKit.Pink);
            else if (sel.Reward) action.Set("НАГРАДА", new Color(0.4f, 0.4f, 0.5f));
            else if (sel.promoOnly) action.Set("ПРОМОКОД", UIKit.Violet);
            else action.Set("КУПИТЬ ЛАКИ-БЛОК", UIKit.Orange);   // 17.11: skins come only from lucky blocks
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
                if (sel.Reward) { AudioFx.Play("error"); Hud.Toast(sel.reward, UIKit.Gold); return; }
                // 17.11: no buying a skin for coins — it falls out of a lucky block
                Hud.Toast("Эта вещь выпадает только из лаки-блоков", UIKit.Orange);
                LuckyUI.Open();
                return;
            }
            Save.Wear(sel.dept, sel.id);
            TryOn(Save.CurrentLook());
            Refresh();
        }

        /// <summary>18.11: every skin off at once (things stay in the wardrobe).</summary>
        public static void TakeOffAll()
        {
            Save.TakeOffAll();
            TryOn(Save.CurrentLook());
            AudioFx.Play("click");
            SelectDept(dept);
        }

        static void OnTakeOff()
        {
            Save.Wear(dept, "");
            TryOn(Save.CurrentLook());
            sel = null;
            Refresh();
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null;
            LuckyUI.Close();
            if (PlayerCtl.I) { PlayerCtl.I.StopWardrobeView(); PlayerCtl.I.ApplyLook(Save.CurrentLook()); PlayerCtl.I.Frozen = false; }
        }

        /// <summary>Re-reads coins / ownership after a purchase made elsewhere (promo code, premium).</summary>
        public static void Changed() { if (IsOpen) { Refresh(); TryOn(Save.CurrentLook()); } }

        class PreviewDrag : MonoBehaviour, IDragHandler
        {
            public void OnDrag(PointerEventData e) { PlayerCtl.I.WardrobeSpin -= e.delta.x * 0.5f; }
        }
    }
}
