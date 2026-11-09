using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>«Магазин примочек»: loot boxes, coin navigator, 100-coin packs, tokens, extra hearts up to 6.</summary>
    public static class ShopUI
    {
        public const int BoxPrice = 200, NavPrice = 500, TokenPrice = 250, PackTokens = 1;
        public static int HeartPrice(int current) => current <= 3 ? 300 : current == 4 ? 600 : 1000;
        static GameObject box;
        public static bool BoxOpen => box != null;

        public static void Open()
        {
            Menus.Unlock();
            Window.Show("МАГАЗИН ПРИМОЧЕК", w =>
            {
                var P = Save.P;
                w.Text($"Монеты: {UIKit.Num(P.coins)}    Жетоны: {P.tokens}", 30, UIKit.Gold);
                if (!P.navigator)
                    w.Row("Указатель монет", "Стрелочка поднимается над тобой и показывает ближайшую монету", $"{NavPrice} МОНЕТ", UIKit.Green, () =>
                    {
                        if (!Save.Spend(NavPrice)) return;
                        P.navigator = true; P.navigatorOn = true; Save.Write();
                        AudioFx.Play("buy"); Hud.Toast("Указатель монет куплен — смотри на зелёную стрелочку над собой!", UIKit.Lime);
                        Window.Refresh();
                    }, P.coins >= NavPrice, UIKit.Lime);
                else
                    w.Row("Указатель монет", P.navigatorOn ? "Включён" : "Выключен", P.navigatorOn ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", P.navigatorOn ? UIKit.Red : UIKit.Green,
                        () => { P.navigatorOn = !P.navigatorOn; Save.Write(); Window.Refresh(); }, true, UIKit.Lime);
                w.Row("Набор 100 монет", "Обменяй жетон на 100 монет", $"{PackTokens} ЖЕТОН", UIKit.Gold, () =>
                {
                    if (!Save.SpendTokens(PackTokens)) return;
                    Save.AddCoins(100); AudioFx.Play("coin"); Hud.Toast("+100 монет!", UIKit.Gold); Window.Refresh();
                }, P.tokens >= PackTokens, UIKit.Gold);
                if (P.maxHearts < 6)
                {
                    int price = HeartPrice(P.maxHearts);
                    w.Row($"Жизнь +1 (сейчас {P.maxHearts} из 6)", "Больше сердечек для будущих миров", $"{price} МОНЕТ", UIKit.Red, () =>
                    {
                        if (!Save.Spend(price)) return;
                        P.maxHearts++; Save.Write(); AudioFx.Play("buy"); Hud.HeartFlash(); Window.Refresh();
                    }, P.coins >= price, UIKit.Red);
                }
                else w.Row("Жизни: 6 из 6", "Максимум сердечек!", null, UIKit.Red, null, true, UIKit.Red);
                w.Footer.text = "Монеты собирай в мирах; лутбоксы прячутся в золотых ящиках миров";
            }, 1100, 800);
        }

        public static Item Roll()
        {
            var pool = Catalog.LootPool().FindAll(i => !Save.Owns(i.id));
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public static void BuyBox(bool withToken)
        {
            if (withToken ? !Save.SpendTokens(1) : !Save.Spend(BoxPrice)) { AudioFx.Play("error"); return; }
            var it = Roll();
            if (it == null) { if (withToken) Save.P.tokens++; else Save.P.coins += BoxPrice; Save.Write(); return; }
            Save.Give(it.id);
            Save.P.boxes++;
            Save.Write();
            ShowBox(it);
        }

        /// <summary>Secret golden crate in a world: a loot box for free.</summary>
        public static void FreeBox()
        {
            var it = Roll();
            if (it == null) { Save.AddCoins(BoxPrice); Hud.Toast($"Секретный лутбокс: +{BoxPrice} монет (все вещи уже есть)", UIKit.Gold); return; }
            Save.Give(it.id);
            Save.P.boxes++;
            Save.Write();
            ShowBox(it);
        }

        /// <summary>Loot box opening: names flicker like a slot machine, then the prize with its picture.</summary>
        public static void ShowBox(Item it)
        {
            if (box) Object.Destroy(box);
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.05f, 0.1f, 0.25f, 0.7f), "LootBox", true);
            UIKit.Fill(dim.rectTransform);
            box = dim.gameObject;
            var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "Panel", true);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 680));
            var title = UIKit.Txt(panel.transform, "ЛУТБОКС", 46, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(700, 60));
            var pic = new GameObject("Pic", typeof(RectTransform)).AddComponent<RawImage>();
            pic.transform.SetParent(panel.transform, false);
            UIKit.Place(pic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(330, 330));
            var giftColor = UIKit.Img(panel.transform, UIKit.Btn, UIKit.Red, "Gift");
            UIKit.Place(giftColor.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(240, 240));
            var bow = UIKit.Img(giftColor.transform, UIKit.White, UIKit.Gold, "Bow");
            UIKit.Place(bow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 240));
            var bow2 = UIKit.Img(giftColor.transform, UIKit.White, UIKit.Gold, "Bow2");
            UIKit.Place(bow2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 50));
            var name = UIKit.Txt(panel.transform, "", 40, Color.white);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -440), new Vector2(720, 60));
            var dept = UIKit.Txt(panel.transform, "", 26, UIKit.Muted);
            UIKit.Place(dept.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -500), new Vector2(720, 40));
            var row = UIKit.Rect("Buttons", panel.transform);
            UIKit.Place(row, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(700, 90));
            var wear = UIKit.Button(row, "НАДЕТЬ", UIKit.Green, () => { Save.Wear(it.dept, it.id); PlayerCtl.I.ApplyLook(Save.CurrentLook()); CloseBox(); }, 32);
            UIKit.Place(wear.Bg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(330, 86));
            var ok = UIKit.Button(row, "ЗАБРАТЬ", UIKit.Blue, CloseBox, 32);
            UIKit.Place(ok.Bg.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(330, 86));
            row.gameObject.SetActive(false);
            pic.gameObject.SetActive(false);
            dim.gameObject.AddComponent<BoxAnim>().Init(it, giftColor.rectTransform, pic, name, dept, row.gameObject);
            AudioFx.Play("box");
        }

        public static void CloseBox()
        {
            if (box) Object.Destroy(box);
            box = null;
            Window.Refresh();
        }

        class BoxAnim : MonoBehaviour
        {
            Item it; RectTransform gift; RawImage pic; TextMeshProUGUI title, dept; GameObject row;
            public void Init(Item i, RectTransform g, RawImage p, TextMeshProUGUI n, TextMeshProUGUI d, GameObject r)
            {
                it = i; gift = g; pic = p; title = n; dept = d; row = r;
                StartCoroutine(Run());
            }

            IEnumerator Run()
            {
                var pool = Catalog.LootPool();
                for (float t = 0; t < 1.3f; t += 0.07f)
                {
                    var r = pool[Random.Range(0, pool.Count)];
                    title.text = r.name; title.color = Color.HSVToRGB(Random.value, 0.4f, 1f);
                    gift.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 40) * 10 * (t / 1.3f));
                    gift.localScale = Vector3.one * (1 + t * 0.15f);
                    yield return new WaitForSecondsRealtime(0.07f);
                }
                gift.gameObject.SetActive(false);
                Preview.I.Thumbs(new List<Item> { it }, (_, tex) => pic.texture = tex);
                pic.gameObject.SetActive(true);
                title.text = it.name; title.color = UIKit.Gold;
                dept.text = "Отдел «" + it.D.name + "»";
                row.SetActive(true);
                AudioFx.Play("open");
            }
        }
    }

    /// <summary>8 premium sets (1 000 – 2 990 ₸) with exclusive skins; the developer promo code opens all of them.</summary>
    public static class PremiumUI
    {
        public static string Tenge(int v) => UIKit.Num(v) + (UIKit.Font != null && UIKit.Font.HasCharacter('₸') ? " ₸" : " тг");

        static readonly string[] Short = { "Дракон", "Лёд", "Космос", "Ниндзя", "Самурай", "Магнат", "Единорог", "Король" };
        /// <summary>Owned sets picked for cancelling (several at once).</summary>
        static readonly HashSet<int> picked = new HashSet<int>();

        public static void Open()
        {
            Menus.Unlock();
            picked.RemoveWhere(i => !Save.P.sets.Contains(i));
            Window.Show("ПРЕМИУМ-НАБОРЫ", w =>
            {
                w.Text("Внутри каждого набора — эксклюзивные скины, которых нет в лутбоксах, плюс 300 монет и 2 жетона.", 26, UIKit.Muted);
                foreach (var s in Catalog.Sets)
                {
                    bool own = Save.P.sets.Contains(s.index);
                    var names = string.Join(", ", s.items.ConvertAll(i => i.name));
                    var set = s;
                    w.Row($"«{s.name}» — {Tenge(s.price)}" + (own ? " · ОТКРЫТ" : ""), names, own ? "ОТМЕНИТЬ" : "КУПИТЬ", own ? UIKit.Orange : UIKit.Gold,
                        own ? (System.Action)(() => AskCancel(new List<int> { set.index })) : () => Buy(set), true, s.color);
                }
                var owned = Catalog.Sets.FindAll(s => Save.P.sets.Contains(s.index));
                if (owned.Count > 1)
                {
                    // pick several (or all) and cancel them in one go
                    var chips = new List<(string, Color, bool, System.Action)>();
                    foreach (var s in owned)
                    {
                        int k = s.index;
                        chips.Add((Short[k], picked.Contains(k) ? s.color : new Color(0.25f, 0.3f, 0.4f), picked.Contains(k), () => { if (!picked.Remove(k)) picked.Add(k); Window.Refresh(); }));
                    }
                    w.Chips("Выбери наборы для отмены (нажми на нужные):", chips);
                    bool all = picked.Count == owned.Count;
                    w.Row(all ? "Выбраны все наборы" : "Выбрать все открытые наборы", null, all ? "СНЯТЬ ВЫБОР" : "ВЫБРАТЬ ВСЕ", UIKit.Blue, () =>
                    {
                        if (all) picked.Clear(); else foreach (var s in owned) picked.Add(s.index);
                        Window.Refresh();
                    });
                    w.Row($"Отменить выбранные: {picked.Count}", "Вещи этих наборов снимутся. Вернуть их можно покупкой или промокодом", "ОТМЕНИТЬ", UIKit.Orange,
                        () => AskCancel(new List<int>(picked)), picked.Count > 0);
                }
                w.Row("Есть промокод?", "Промокод разработчика открывает все 8 наборов (и снова возвращает отменённые)", "ВВЕСТИ", UIKit.Violet, Menus.PromoWindow, true, UIKit.Violet);
                w.Footer.text = Save.P.promo ? $"Промокод «Артём 22 X» активирован · открыто наборов: {owned.Count} из {Catalog.Sets.Count}" : "Покупки за тенге появятся позже";
            }, 1200, 820);
        }

        static void AskCancel(List<int> sets)
        {
            if (sets.Count == 0) return;
            var names = string.Join(", ", sets.ConvertAll(i => "«" + Catalog.Sets[i].name + "»"));
            Window.Show(sets.Count == 1 ? "ОТМЕНИТЬ НАБОР?" : "ОТМЕНИТЬ НАБОРЫ?", w =>
            {
                w.Text((sets.Count == 1 ? "Набор " : "Наборы ") + names + (sets.Count == 1 ? " будет отменён." : " будут отменены."), 30);
                w.Text("Вещи из них снимутся с чубрика и пропадут из гардероба. Потом их можно снова купить или получить промокодом.", 26, UIKit.Muted);
                w.Row("Да, отменить", null, "ОТМЕНИТЬ", UIKit.Orange, () => { Cancel(sets); Open(); });
                w.Row("Нет, оставить", null, "НАЗАД", UIKit.Blue, Open);
            }, 1000, 560);
        }

        public static void Cancel(IEnumerable<int> sets)
        {
            int n = 0;
            foreach (var i in sets) if (Save.P.sets.Contains(i)) { Save.CancelSet(i); picked.Remove(i); n++; }
            if (n == 0) return;
            Save.Write();
            if (PlayerCtl.I) PlayerCtl.I.ApplyLook(Save.CurrentLook());
            AudioFx.Play("click");
            Hud.Toast(n == 1 ? "Набор отменён" : $"Отменено наборов: {n}", UIKit.Orange);
        }

        static void Buy(PremiumSet s)
        {
            // Real payments are not connected yet: explain and offer the promo code.
            Window.Show("«" + s.name + "»", w =>
            {
                w.Text($"Цена набора: {Tenge(s.price)}", 34, UIKit.Gold);
                w.Text("В наборе: " + string.Join(", ", s.items.ConvertAll(i => i.name)) + $". Бонус: {s.bonusCoins} монет и {s.bonusTokens} жетона.", 28);
                w.Text("Оплата в тенге пока не подключена — она появится в одном из обновлений. Если у тебя есть промокод, введи его.", 26, UIKit.Muted);
                w.Row("Промокод", null, "ВВЕСТИ", UIKit.Violet, Menus.PromoWindow);
                w.Row("Назад к наборам", null, "НАЗАД", UIKit.Blue, Open);
            }, 1000, 620);
        }
    }
}
