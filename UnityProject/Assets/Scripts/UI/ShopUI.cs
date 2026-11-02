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
                int left = Catalog.LootPool().FindAll(i => !Save.Owns(i.id)).Count;
                w.Row("Лутбокс", left > 0 ? $"Случайная вещь из гардероба (ещё не открыто: {left})" : "Ты собрал все обычные вещи!",
                    $"{BoxPrice} МОНЕТ", UIKit.Orange, () => BuyBox(false), left > 0 && P.coins >= BoxPrice, UIKit.Orange);
                w.Row("Лутбокс за жетон", "Тот же лутбокс, но за 1 жетон", "1 ЖЕТОН", UIKit.Violet, () => BuyBox(true), left > 0 && P.tokens >= 1, UIKit.Violet);
                if (!P.navigator)
                    w.Row("Навигатор монет", "Стрелка над головой показывает ближайшую монету", $"{NavPrice} МОНЕТ", UIKit.Green, () =>
                    {
                        if (!Save.Spend(NavPrice)) return;
                        P.navigator = true; P.navigatorOn = true; Save.Write();
                        AudioFx.Play("buy"); Hud.Toast("Навигатор монет куплен — ищи зелёную стрелку!", UIKit.Lime);
                        Window.Refresh();
                    }, P.coins >= NavPrice, UIKit.Lime);
                else
                    w.Row("Навигатор монет", P.navigatorOn ? "Включён" : "Выключен", P.navigatorOn ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", P.navigatorOn ? UIKit.Red : UIKit.Green,
                        () => { P.navigatorOn = !P.navigatorOn; Save.Write(); Window.Refresh(); }, true, UIKit.Lime);
                w.Row("Набор 100 монет", "Обменяй жетон на 100 монет", $"{PackTokens} ЖЕТОН", UIKit.Gold, () =>
                {
                    if (!Save.SpendTokens(PackTokens)) return;
                    Save.AddCoins(100); AudioFx.Play("coin"); Hud.Toast("+100 монет!", UIKit.Gold); Window.Refresh();
                }, P.tokens >= PackTokens, UIKit.Gold);
                w.Row("Жетон", "Жетоны открывают лутбоксы и секретные модификаторы", $"{TokenPrice} МОНЕТ", UIKit.Violet, () =>
                {
                    if (!Save.Spend(TokenPrice)) return;
                    P.tokens++; Save.Write(); AudioFx.Play("token"); Window.Refresh();
                }, P.coins >= TokenPrice, UIKit.Violet);
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
                w.Row("Премиум-наборы", "8 наборов с эксклюзивными скинами", "СМОТРЕТЬ", UIKit.Gold, PremiumUI.Open, true, UIKit.Gold);
                w.Footer.text = "Монеты появляются снова через минуту — собирай их по всему лобби";
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

        /// <summary>Loot box opening: names flicker like a slot machine, then the prize with its picture.</summary>
        static void ShowBox(Item it)
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

        public static void Open()
        {
            Menus.Unlock();
            Window.Show("ПРЕМИУМ-НАБОРЫ", w =>
            {
                w.Text("Внутри каждого набора — эксклюзивные скины, которых нет в лутбоксах, плюс 300 монет и 2 жетона.", 26, UIKit.Muted);
                foreach (var s in Catalog.Sets)
                {
                    bool own = Save.P.sets.Contains(s.index);
                    var names = string.Join(", ", s.items.ConvertAll(i => i.name));
                    var set = s;
                    w.Row($"«{s.name}» — {Tenge(s.price)}", names, own ? "ОТКРЫТ" : "КУПИТЬ", own ? UIKit.Green : UIKit.Gold,
                        own ? (System.Action)(() => { Window.Close(); WardrobeUI.Open(set.items[0].dept); }) : () => Buy(set), true, s.color);
                }
                w.Row("Есть промокод?", "Промокод разработчика открывает все 8 наборов", "ВВЕСТИ", UIKit.Violet, Menus.PromoWindow, true, UIKit.Violet);
                w.Footer.text = Save.P.promo ? "Промокод «Артём 22 X» активирован — все наборы твои!" : "Покупки за тенге появятся позже";
            }, 1200, 820);
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
