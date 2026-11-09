using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>Title screen, pause menu, settings and the promo code window.</summary>
    public static class Menus
    {
        static GameObject main;
        public static bool MainOpen => main != null;
        /// <summary>A full-screen page (wardrobe, loot box) is open.</summary>
        public static bool OverlayOpen => MainOpen || WardrobeUI.IsOpen || ShopUI.BoxOpen;
        public static bool AnyOpen => OverlayOpen || UIRoot.Open != null;

        public static void LockCursor()
        {
            if (Hud.Touch || Boot.Testing) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        // ---------------- title ----------------

        public static void Main()
        {
            Window.Close();
            if (main) Object.Destroy(main);
            Unlock();
            PlayerCtl.I.MenuOrbit = true;
            var dim = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0.1f, 0.3f, 0.6f, 0.18f), "MainMenu", true);
            UIKit.Fill(dim.rectTransform);
            main = dim.gameObject;
            var title = UIKit.Txt(dim.transform, "ЧУБРИК", 150, UIKit.Gold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1200, 200));
            title.overflowMode = TextOverflowModes.Overflow;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.gameObject.AddComponent<TitleWobble>();
            var sub = UIKit.Txt(dim.transform, "ПРИКЛЮЧЕНИЯ ПРИКОЛЬНОГО ЧУБРИКА", 36, Color.white);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -220), new Vector2(1200, 50));

            var col = UIKit.Rect("Buttons", dim.transform);
            UIKit.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(560, 470));
            var vl = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16; vl.childControlHeight = false; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            void Btn(string label, Color c, System.Action a, float h = 92, float font = 40)
            {
                var b = UIKit.Button(col, label, c, a, font);
                b.Bg.rectTransform.sizeDelta = new Vector2(560, h);
            }
            Btn("ИГРАТЬ", UIKit.Green, Play, 112, 52);
            Btn("НАСТРОЙКИ", UIKit.Blue, () => { CloseMain(); Settings(); });
            var tip = UIKit.Txt(col, "Гардероб, магазин и премиум-наборы — в лобби: подойди к ним", 24, Color.white);
            tip.rectTransform.sizeDelta = new Vector2(560, 70);
            if (Updater.HasUpdate) Btn(Updater.I.ButtonText, UIKit.Orange, () => Updater.I.Press(), 80, 30);
            var ver = UIKit.Txt(dim.transform, "версия " + Application.version, 22, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.BottomRight, false);
            UIKit.Place(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 12), new Vector2(400, 30));
        }

        static void CloseMain()
        {
            if (main) Object.Destroy(main);
            main = null;
            if (PlayerCtl.I.MenuOrbit)
            {
                PlayerCtl.I.MenuOrbit = false;
                Worlds.Current = 1;
                PlayerCtl.I.WaterY = -0.9f;
                PlayerCtl.I.Hearts = Save.P.maxHearts;
                PlayerCtl.I.Teleport(Lobby.Spawn, 0);
            }
        }

        public static void Play()
        {
            CloseMain();
            AudioFx.Play("portal");
            PlayerCtl.I.PlayVictory();
            Hud.Toast("Добро пожаловать в Центральную долину! Ищи 10 жетонов и заходи в порталы", UIKit.Gold);
            if (!Story.Show("intro", LockCursor)) LockCursor();
        }

        // ---------------- pause ----------------

        public static void Pause()
        {
            Unlock();
            Window.Show("МЕНЮ", w =>
            {
                if (Updater.HasUpdate) w.Row("Вышла новая версия " + Updater.I.LatestVersion + "!", "Скачай обновление — в нём новые штуки", Updater.I.ButtonText, UIKit.Orange, () => Updater.I.Press());
                w.Row("Продолжить", null, "ИГРАТЬ", UIKit.Green, () => { Window.Close(); LockCursor(); });
                w.Row("Настройки", null, "ОТКРЫТЬ", UIKit.Blue, Settings);
                if (Worlds.Level)
                {
                    w.Row("Вернуться к флажку", "Последний чекпоинт мира " + Worlds.Current, "ТЕЛЕПОРТ", UIKit.Cyan, () => { Window.Close(); PlayerCtl.I.PlayVictory(() => PlayerCtl.I.Teleport(PlayerCtl.I.Checkpoint, 0)); });
                    w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Green, () => Worlds.Enter(1));
                }
                else
                {
                    void WorldRow(int world, string title, string about, Color col)
                    {
                        bool open = Lobby.Open(world);
                        int need = Lobby.NeedFor(world);
                        w.Row(title, !open ? $"Спаси {need} друзей (спасено {Save.P.rescued.Count})" : Save.P.done.Contains(world) ? "Пройден! Можно пройти ещё раз" : about, "ВПЕРЁД", col, () => Worlds.Enter(world), open);
                    }
                    WorldRow(2, "Мир 2 — Парящие руины", "Лёгкий мир с мобами и ящиками", UIKit.Green);
                    WorldRow(4, "Мир 4 — Снежная Долина", "Лёд, снеговики и Король Йети", UIKit.Cyan);
                    WorldRow(5, "Мир 5: Джунгли", "Река, болото, пирамида и Древний Голем", UIKit.Green);
                    WorldRow(3, "Мир 3 — Кристальные пещеры", "Лава, кристаллы и Страж", UIKit.Violet);
                    WorldRow(6, "Мир 6: Глюк-мир", "Мир-розыгрыш: всё сломалось, и это весело", UIKit.Pink);
                    w.Row("Вернуться в центр лобби", "Телепорт к табличке «Лобби Чубриков»", "ТЕЛЕПОРТ", UIKit.Cyan, () => { Window.Close(); PlayerCtl.I.PlayVictory(() => PlayerCtl.I.Teleport(Lobby.Spawn, 0)); });
                }
                w.Row("История Чубрика", "Сюжет: прочитанные главы", "ЧИТАТЬ", UIKit.Pink, Story.Book);
                w.Row("Главное меню", null, "ВЫЙТИ", UIKit.Red, Main);
                w.Text("Гардероб, магазин примочек и премиум-наборы открываются, когда подходишь к ним в лобби.", 24, UIKit.Muted);
                w.Footer.text = $"Ключей: {Save.P.keys} · спасено друзей: {Save.P.rescued.Count} · вещей: {OwnedCount()} из {Catalog.Items.Count}";
            }, 1000, 800);
        }

        public static int OwnedCount()
        {
            int n = 0;
            foreach (var it in Catalog.Items.Values) if (Save.Owns(it.id)) n++;
            return n;
        }

        // ---------------- settings ----------------

        public static void Settings()
        {
            Window.Show("НАСТРОЙКИ", w =>
            {
                var P = Save.P;
                w.Chips("Громкость", new (string, Color, bool, System.Action)[]
                {
                    ("0%", UIKit.Blue, P.volume < 0.05f, () => SetVol(0)), ("25%", UIKit.Blue, Mathf.Abs(P.volume - 0.25f) < 0.05f, () => SetVol(0.25f)),
                    ("50%", UIKit.Blue, Mathf.Abs(P.volume - 0.5f) < 0.05f, () => SetVol(0.5f)), ("80%", UIKit.Blue, Mathf.Abs(P.volume - 0.8f) < 0.05f, () => SetVol(0.8f)),
                    ("100%", UIKit.Blue, P.volume > 0.95f, () => SetVol(1)),
                });
                w.Chips("Музыка", new (string, Color, bool, System.Action)[]
                {
                    ("ВКЛ", UIKit.Green, P.music, () => { P.music = true; AudioFx.SetMusic(true); Save.Write(); Window.Refresh(); }),
                    ("ВЫКЛ", UIKit.Red, !P.music, () => { P.music = false; AudioFx.SetMusic(false); Save.Write(); Window.Refresh(); }),
                });
                w.Chips("Чувствительность камеры", new (string, Color, bool, System.Action)[]
                {
                    ("0.5", UIKit.Blue, P.sens < 0.6f, () => SetSens(0.5f)), ("0.75", UIKit.Blue, Mathf.Abs(P.sens - 0.75f) < 0.05f, () => SetSens(0.75f)),
                    ("1", UIKit.Blue, Mathf.Abs(P.sens - 1) < 0.05f, () => SetSens(1)), ("1.5", UIKit.Blue, Mathf.Abs(P.sens - 1.5f) < 0.05f, () => SetSens(1.5f)), ("2", UIKit.Blue, P.sens > 1.9f, () => SetSens(2)),
                });
                if (P.navigator)
                    w.Chips("Навигатор монет", new (string, Color, bool, System.Action)[]
                    {
                        ("ВКЛ", UIKit.Green, P.navigatorOn, () => { P.navigatorOn = true; Save.Write(); Window.Refresh(); }),
                        ("ВЫКЛ", UIKit.Red, !P.navigatorOn, () => { P.navigatorOn = false; Save.Write(); Window.Refresh(); }),
                    });
                w.Text("Управление на компьютере: WASD — бег, ПРОБЕЛ — прыжок и двойной прыжок (держи с крыльями — полёт), F или клик — удар, Q — эмоция, E — войти, мышь — камера, Esc — меню.", 24, UIKit.Muted);
            }, 1000, 760);
        }

        static void SetVol(float v) { Save.P.volume = v; AudioFx.SetVolume(v); Save.Write(); Window.Refresh(); }
        static void SetSens(float v) { Save.P.sens = v; Save.Write(); Window.Refresh(); }

        // ---------------- promo ----------------

        public static void PromoWindow()
        {
            Window.Show("ПРОМОКОД", w =>
            {
                w.Text("Введите промокод — он может открыть премиум-наборы бесплатно.", 28);
                var holder = UIKit.Rect("PromoRow", w.Content);
                holder.gameObject.AddComponent<LayoutElement>().minHeight = 96;
                var input = UIKit.Input(holder, "Промокод", 32, 34);
                UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(600, 86));
                var go = UIKit.Button(holder, "АКТИВИРОВАТЬ", UIKit.Green, () =>
                {
                    if (Promo.Apply(input.text)) Window.Refresh();
                }, 30);
                UIKit.Place(go.Bg.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(300, 86));
                if (Save.P.promo) w.Text("Промокод разработчика активирован: 6 сердечек, все 8 премиум-наборов и «Ультимативный аватар»!", 28, UIKit.Gold);
            }, 1000, 520);
        }
    }

    /// <summary>Developer cheat code: «Артём 22 X» opens all 8 premium sets for free.</summary>
    public static class Promo
    {
        public const string DevCode = "Артём 22 X";

        /// <summary>Lower case, no spaces, ё → е, Latin look-alikes → Cyrillic.</summary>
        public static string Norm(string s)
        {
            if (s == null) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var ch0 in s.Trim().ToLowerInvariant())
            {
                char ch = ch0;
                if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_' || ch == '.') continue;
                switch (ch)
                {
                    case 'ё': ch = 'е'; break;
                    case 'x': ch = 'х'; break;
                    case 'a': ch = 'а'; break;
                    case 'e': ch = 'е'; break;
                    case 'm': ch = 'м'; break;
                    case 't': ch = 'т'; break;
                    case 'p': ch = 'р'; break;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        public static bool Apply(string code)
        {
            if (Norm(code) != Norm(DevCode))
            {
                AudioFx.Play("error");
                Hud.Toast("Такого промокода нет", UIKit.Red);
                return false;
            }
            bool again = Save.P.promo;
            if (again && Save.P.sets.Count >= Catalog.Sets.Count)
            {
                Hud.Toast("Промокод уже активирован — всё открыто!", UIKit.Gold);
                return true;
            }
            UnlockAll();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            Hud.Toast(again ? "Промокод снова сработал — все 8 премиум-наборов опять твои!" : "ПРОМОКОД «АРТЁМ 22 X»: 6 сердечек и все 8 премиум-наборов бесплатно!", UIKit.Gold);
            return true;
        }

        public static void UnlockAll()
        {
            Save.P.promo = true;
            Save.P.maxHearts = 6;
            if (PlayerCtl.I) PlayerCtl.I.Hearts = 6;
            foreach (var s in Catalog.Sets) UnlockSet(s, false);
            Save.Give("secret_7");
            Save.Write();
        }

        public static void UnlockSet(PremiumSet s, bool write = true)
        {
            if (Save.P.sets.Contains(s.index)) return;
            Save.P.sets.Add(s.index);
            if (!Save.P.setBonus.Contains(s.index))
            {
                // the bonus comes once: cancelling and unlocking again doesn't pay twice
                Save.P.setBonus.Add(s.index);
                Save.P.coins += s.bonusCoins;
                Save.P.tokens += s.bonusTokens;
            }
            if (write) Save.Write();
        }
    }

    /// <summary>Title letters bounce gently.</summary>
    public class TitleWobble : MonoBehaviour
    {
        void Update() { transform.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.04f); transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.3f) * 2); }
    }
}
