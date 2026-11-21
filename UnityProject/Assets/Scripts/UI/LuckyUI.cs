using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// 17.11 lucky blocks: the only way to get the wardrobe's skins. 400 coins = one thing, 1 000 coins = ten small
    /// blocks (ten things), the rainbow block 5 000 coins = only the rarest things. Things you have don't drop again.
    /// </summary>
    public static class Lucky
    {
        public static readonly int[] Price = { 400, 1000, 5000 };
        public static readonly string[] Name = { "ЛАКИ-БЛОК", "МЕГА-БЛОК ×10", "РАДУЖНЫЙ БЛОК" };
        public static readonly string[] RarityName = { "ОБЫЧНАЯ", "РЕДКАЯ", "ЭПИЧЕСКАЯ", "ЛЕГЕНДА" };
        public static readonly Color[] RarityColor = { new Color(0.75f, 0.82f, 0.9f), UIKit.Hex("4FA8FF"), UIKit.Hex("C77DFF"), UIKit.Hex("FFC83A") };
        public static int Opened;   // autotest

        /// <summary>Things that come out of lucky blocks: every wardrobe item except premium sets, game rewards,
        /// the developer's avatar and the free starting things.</summary>
        public static bool InPool(Item it) => !it.Premium && !it.Reward && it.id != "secret_7" && it.id != it.D.def;

        public static List<Item> Pool()
        {
            var l = new List<Item>();
            foreach (var it in Catalog.Items.Values) if (InPool(it)) l.Add(it);
            return l;
        }

        /// <summary>0 ordinary, 1 rare, 2 epic, 3 legendary.</summary>
        public static int Rarity(Item it)
        {
            if (it.rarity >= 0) return it.rarity;                      // variants: their finish / tint / tempo / pitch / colour
            if (it.finish >= 0) return SkinFinish.Rarity(it.finish);   // v0.27 finishes: gold and galaxy are legendary
            int i = Catalog.Index(it.id);
            if (it.dept == "secret") return 3;
            if (it.dept == "suit") return i == 1 || i == 2 || i == 26 ? 3 : 2;
            if (it.dept == "aura" || it.dept == "pet" || it.dept == "wings") return i >= 5 ? 3 : 2;
            if (it.price >= 140 || i >= 6) return 1;
            return 0;
        }

        /// <summary>Picks the things for a block of the given kind (none you already have). Empty: you have everything.</summary>
        public static List<Item> Roll(int kind)
        {
            var free = Pool().FindAll(i => !Save.Owns(i.id));
            var got = new List<Item>();
            int n = kind == 1 ? 10 : 1;
            for (int k = 0; k < n && free.Count > 0; k++)
            {
                int want;
                float r = Random.value;
                if (kind == 2) want = r < 0.4f ? 3 : 2;                                   // the rainbow block: only the rarest
                else want = r < 0.03f ? 3 : r < 0.13f ? 2 : r < 0.43f ? 1 : 0;
                var pick = free.FindAll(i => Rarity(i) == want);
                if (pick.Count == 0 && kind == 2) pick = free.FindAll(i => Rarity(i) >= 2);
                if (pick.Count == 0 && kind == 2) pick = free.FindAll(i => Rarity(i) >= 1);
                if (pick.Count == 0) pick = free;
                var it = pick[Random.Range(0, pick.Count)];
                got.Add(it); free.Remove(it);
            }
            return got;
        }

        /// <summary>Pays and opens a block: the things are yours at once. Null if there aren't enough coins.</summary>
        public static List<Item> Open(int kind)
        {
            var items = Roll(kind);
            if (items.Count == 0) { Hud.Toast("У тебя уже есть все вещи из лаки-блоков!", UIKit.Gold); return null; }
            if (!Save.Spend(Price[kind])) { AudioFx.Play("error"); Hud.Toast($"Не хватает монет: нужно {UIKit.Num(Price[kind])}", UIKit.Red); return null; }
            foreach (var it in items) Save.Give(it.id);
            Save.P.boxes += items.Count;
            Opened++;
            Save.Write();
            return items;
        }
    }

    public static class LuckyUI
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        static RectTransform body;
        static TextMeshProUGUI money;

        public static void Open()
        {
            Close();
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.05f, 0.08f, 0.2f, 0.72f), "LuckyBlocks", true);
            UIKit.Fill(dim.rectTransform);
            root = dim.gameObject;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 800));
            var title = UIKit.Txt(panel.transform, "ЛАКИ-БЛОКИ", 46, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -16), new Vector2(500, 64));
            money = UIKit.Txt(panel.transform, "", 28, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(money.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(380, -20), new Vector2(700, 56));
            var x = UIKit.Button(panel.transform, "X", UIKit.Red, Close, 34, "Close");
            UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(84, 70));
            body = UIKit.Rect("Body", panel.transform);
            UIKit.Fill(body, 24, 24, 24, 100);
            Choose();
            AudioFx.Play("click");
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null;
            WardrobeUI.Changed();
        }

        static void Clear() { for (int i = body.childCount - 1; i >= 0; i--) Object.Destroy(body.GetChild(i).gameObject); }
        static void Money() { if (money) money.text = $"Монеты: {UIKit.Num(Save.P.coins)}   ·   вещей из блоков: {Lucky.Pool().FindAll(i => Save.Owns(i.id)).Count} из {Lucky.Pool().Count}"; }

        /// <summary>The three blocks side by side.</summary>
        static void Choose()
        {
            Clear(); Money();
            string[] about =
            {
                "Внутри — одна вещь из гардероба. Может выпасть и редкая!",
                "Сразу 10 маленьких лаки-блоков — в каждом скин. 10 вещей по цене двух с половиной блоков",
                "Только самые редкие вещи: эпические и легендарные. Богачи, ауры, питомцы, крылья…",
            };
            for (int k = 0; k < 3; k++)
            {
                int kind = k;
                var card = UIKit.Img(body, UIKit.Glass, Color.white, "Card" + k, true);
                UIKit.Place(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(k * 420, 0), new Vector2(400, 670));
                var blk = Block(card.transform, kind, new Vector2(0, -40), 230);
                blk.gameObject.AddComponent<Bob>();
                var nm = UIKit.Txt(card.transform, Lucky.Name[k], 34, kind == 2 ? UIKit.Pink : UIKit.Gold);
                UIKit.Place(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(380, 50));
                var ab = UIKit.Txt(card.transform, about[k], 22, Color.white, TextAlignmentOptions.Top, false);
                UIKit.Place(ab.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -360), new Vector2(360, 150));
                bool can = Save.P.coins >= Lucky.Price[k];
                var b = UIKit.Button(card.transform, $"ОТКРЫТЬ: {UIKit.Num(Lucky.Price[k])}", can ? (kind == 2 ? UIKit.Pink : UIKit.Orange) : new Color(0.4f, 0.4f, 0.5f), () => Buy(kind), 28, "Open" + k);
                UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(360, 86));
            }
        }

        /// <summary>A lucky block picture: a yellow cube with a «?» (the rainbow one shimmers).</summary>
        static RectTransform Block(Transform p, int kind, Vector2 pos, float size)
        {
            var b = UIKit.Img(p, UIKit.Btn, kind == 1 ? UIKit.Hex("FF9F1C") : UIKit.Hex("FFD23A"), "Block");
            UIKit.Place(b.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, new Vector2(size, size));
            if (kind == 2) b.gameObject.AddComponent<WardrobeUI.Rainbow>();
            var inner = UIKit.Img(b.transform, UIKit.Btn, new Color(1, 1, 1, 0.25f), "Shine");
            UIKit.Place(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.8f, size * 0.8f));
            var q = UIKit.Txt(b.transform, kind == 1 ? "×10" : "?", size * (kind == 1 ? 0.36f : 0.6f), Color.white);
            UIKit.Fill(q.rectTransform);
            return b.rectTransform;
        }

        public static void Buy(int kind)
        {
            var items = Lucky.Open(kind);
            if (items == null) return;
            Clear();
            Money();
            root.GetComponent<Opener>()?.StopAllCoroutines();
            var op = root.GetComponent<Opener>() ?? root.AddComponent<Opener>();
            op.StartCoroutine(op.Run(kind, items));
        }

        class Bob : MonoBehaviour
        {
            float s = Random.value * 6;
            void Update() { transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2 + s) * 5); }
        }

        class Opener : MonoBehaviour
        {
            public IEnumerator Run(int kind, List<Item> items)
            {
                // the block shakes harder and harder and pops
                var blk = Block(body, kind, new Vector2(0, -60), 300);
                blk.anchorMin = blk.anchorMax = new Vector2(0.5f, 1);
                AudioFx.Play("box");
                for (float t = 0; t < 1.2f; t += Time.unscaledDeltaTime)
                {
                    blk.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 50) * 14 * t);
                    blk.localScale = Vector3.one * (1 + t * 0.2f);
                    yield return null;
                }
                Destroy(blk.gameObject);
                AudioFx.Play("open");
                // the prizes: one big picture, or ten small blocks that open one after another
                int n = items.Count;
                float cell = n == 1 ? 340 : 205, gap = 14;
                int cols = n == 1 ? 1 : 5;
                float w = cols * cell + (cols - 1) * gap;
                var slots = new List<(RawImage pic, TextMeshProUGUI name, Image frame)>();
                for (int k = 0; k < n; k++)
                {
                    var fr = UIKit.Img(body, UIKit.Btn, Lucky.RarityColor[Lucky.Rarity(items[k])], "Prize" + k);
                    UIKit.Place(fr.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-w / 2 + (k % cols) * (cell + gap), -6 - (k / cols) * (cell + 44)), new Vector2(cell, cell));
                    var pic = new GameObject("Pic", typeof(RectTransform)).AddComponent<RawImage>();
                    pic.transform.SetParent(fr.transform, false); pic.raycastTarget = false;
                    UIKit.Fill(pic.rectTransform, 8, 8, 8, 8);
                    var nm = UIKit.Txt(fr.transform, items[k].name, n == 1 ? 34 : 20, Color.white);
                    UIKit.Place(nm.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(cell + 10, n == 1 ? 48 : 36));
                    nm.enableAutoSizing = true; nm.fontSizeMin = 12; nm.fontSizeMax = n == 1 ? 34 : 20;
                    fr.gameObject.SetActive(false);
                    slots.Add((pic, nm, fr));
                }
                var list = new List<Item>(items);
                var texs = new Dictionary<string, Texture>();
                Preview.I.Thumbs(list, (it, tex) => texs[it.id] = tex);
                for (int k = 0; k < n; k++)
                {
                    var (pic, nm, fr) = slots[k];
                    fr.gameObject.SetActive(true);
                    if (texs.TryGetValue(items[k].id, out var tx)) pic.texture = tx;
                    int r = Lucky.Rarity(items[k]);
                    AudioFx.Play(r >= 2 ? "token" : "coin");
                    for (float t = 0; t < 0.18f; t += Time.unscaledDeltaTime) { fr.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.08f, t / 0.18f); yield return null; }
                    fr.rectTransform.localScale = Vector3.one;
                    if (n > 1) yield return new WaitForSecondsRealtime(0.12f);
                }
                var sub = UIKit.Txt(body, n == 1 ? $"{Lucky.RarityName[Lucky.Rarity(items[0])]} · отдел «{items[0].D.name}»" : "10 вещей уже в гардеробе!", 26, UIKit.Muted);
                UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 116), new Vector2(1100, 40));
                if (n == 1)
                {
                    var it = items[0];
                    var wear = UIKit.Button(body, "НАДЕТЬ", UIKit.Green, () => { Save.Wear(it.dept, it.id); Close(); WardrobeUI.SelectDept(it.dept); }, 30, "Wear");
                    UIKit.Place(wear.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-380, 10), new Vector2(330, 86));
                }
                var again = UIKit.Button(body, $"ЕЩЁ: {UIKit.Num(Lucky.Price[kind])}", UIKit.Orange, () => Buy(kind), 28, "Again");
                UIKit.Place(again.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(n == 1 ? 0 : -190, 10), new Vector2(330, 86));
                var back = UIKit.Button(body, "К БЛОКАМ", UIKit.Blue, Choose, 28, "Back");
                UIKit.Place(back.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(n == 1 ? 380 : 190, 10), new Vector2(330, 86));
                WardrobeUI.Changed();
            }
        }
    }
}
