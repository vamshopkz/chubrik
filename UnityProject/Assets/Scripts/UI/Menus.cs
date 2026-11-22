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
        public static bool OverlayOpen => MainOpen || WardrobeUI.IsOpen || ShopUI.BoxOpen || Cartoon.IsOpen || LuckyUI.IsOpen || Tutorial.IsOpen;
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
            CharSlots.Close();
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
            UIKit.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(560, 720));
            var vl = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 16; vl.childControlHeight = false; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            void Btn(string label, Color c, System.Action a, float h = 92, float font = 40)
            {
                var b = UIKit.Button(col, label, c, a, font);
                b.Bg.rectTransform.sizeDelta = new Vector2(560, h);
            }
            Btn("ИГРАТЬ", UIKit.Green, CharSlots.Open, 112, 52);   // 22.11: first pick a chubrik (or make a new one)
            Btn("ИГРОКИ ОНЛАЙН", UIKit.Violet, () => { CloseMain(); PlayersUI.Open(); });
            Btn("ТАБЛИЦА РЕКОРДОВ", UIKit.Orange, () => { CloseMain(); RecordsUI.Open(); });
            Btn("ПРОМОКОДЫ", UIKit.Pink, () => { CloseMain(); PromoWindow(); });
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
                Save.P.keys = 0; Rescue.Held.Clear(); Save.Write();   // 18.11: you start in the lobby without keys
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
                w.Row("Игроки онлайн", Online.I != null && Online.I.Peers.Count > 0 ? $"Сейчас играют: {Online.I.Peers.Count} — нажми и окажешься рядом" : "Кто сейчас играет — можно прийти к ним", "СМОТРЕТЬ", UIKit.Violet, PlayersUI.Open);
                if (Online.I != null && Online.I.RoomWith != null) w.Row("Ты в комнате с другом", "Выйди, если хочешь играть один", "ВЫЙТИ", UIKit.Red, () => Online.I.LeaveRoom());
                if (Worlds.Current == 1 && !Worlds.Level) w.Row("Футбол", Football.Inside(PlayerCtl.I.transform.position) ? "Ты на стадионе" : "Стадион: 10 уровней или матч с другом", Football.Inside(PlayerCtl.I.transform.position) ? "В ЛОББИ" : "ИГРАТЬ", UIKit.Green, () => { if (Football.Inside(PlayerCtl.I.transform.position)) { Window.Close(); Football.Leave(); } else Football.Menu(); });
                if (Save.P.cheatAdmin) w.Row("Админ-панель", "Копируй вещи и ставь их куда хочешь", "ОТКРЫТЬ", UIKit.Red, () => { Window.Close(); AdminPanel.Open(); });
                w.Row("Промокоды", "Напиши секретное слово — и случится что-то интересное", "ВВЕСТИ", UIKit.Pink, PromoWindow);
                w.Row("Таблица рекордов", "Кто больше всех собрал монет, друзей, миров, жетонов и яиц", "СМОТРЕТЬ", UIKit.Orange, RecordsUI.Open);
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
                    WorldRow(World8.Id, "Мир 8: Самый мемный мир", "Неоновые острова, мемы, батуты и робот Пыжик", UIKit.Pink);
                    WorldRow(World67.Id, "Мир 67: Глючный Сикс-Севен", "67 друзей, 67 ключей… или нет?", UIKit.Lime);
                    w.Row("Вернуться в центр лобби", "Телепорт к табличке «Лобби Чубриков»", "ТЕЛЕПОРТ", UIKit.Cyan, () => { Window.Close(); PlayerCtl.I.PlayVictory(() => PlayerCtl.I.Teleport(Lobby.Spawn, 0)); });
                }
                w.Row("История Чубрика", "Сюжет: прочитанные главы", "ЧИТАТЬ", UIKit.Pink, Story.Book);
                w.Row("Главное меню", null, "ВЫЙТИ", UIKit.Red, Main);
                w.Text("Гардероб, магазин примочек и премиум-наборы открываются, когда подходишь к ним в лобби.", 24, UIKit.Muted);
                w.Footer.text = $"Ключей: {Save.P.keys} · спасено друзей: {Save.P.rescued.Count} · золотых яиц: {Eggs.Found} из {Eggs.Total} · вещей: {OwnedCount()} из {Catalog.Items.Count}";
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
                NickRow(w);
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
                w.Row("Обучение", "Видео с голосом: как играть, куда идти и что нажимать", "СМОТРЕТЬ", UIKit.Green, () => { Window.Close(); Tutorial.Play(); });   // 21.11
                w.Row("Читы", "Полёт, бесконечные монеты и админ-панель", "ЧИТЫ", UIKit.Red, Cheats.Open, true, UIKit.Red);
                w.Row("Версии игры", "Сейчас: " + Application.version + " · можно выбрать новую или старую", "ВЫБРАТЬ", UIKit.Blue, VersionsUI.Open);
                w.Text("На телефоне: A — прыжок (нажми два раза — двойной прыжок), B — удар. Рывок вперёд «хэ-хэй»: беги, нажми B, а сразу за ним A. Не допрыгнул до края — чубрик сам зацепится и залезет. Прыжок вниз: A, потом B — маленький; A или 2×A и держи B — летит вниз головой и смешно плюхается.", 24, UIKit.Gold);
                w.Text("Управление на компьютере: WASD — бег, ПРОБЕЛ — прыжок и двойной прыжок (держи с крыльями — полёт), F или клик — удар (рывок: беги, F, потом ПРОБЕЛ), Q — эмоция, E — войти, мышь — камера, Esc — меню.", 24, UIKit.Muted);
            }, 1000, 760);
        }

        /// <summary>The nickname: shown in the records table and to the friend you play with.</summary>
        static void NickRow(Window w)
        {
            w.Text("Твой ник: " + Online.Nick, 28, UIKit.Gold);
            var holder = UIKit.Rect("NickRow", w.Content);
            holder.gameObject.AddComponent<LayoutElement>().minHeight = 92;
            var input = UIKit.Input(holder, "Новый ник", 16, 32);
            input.text = Save.P.nick;
            UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(560, 82));
            var ok = UIKit.Button(holder, "СОХРАНИТЬ", UIKit.Green, () =>
            {
                var n = Online.Clean(input.text);
                if (n.Length > 16) n = n.Substring(0, 16);
                Save.P.nick = n; Save.Write();
                AudioFx.Play("buy");
                Hud.Toast(n.Length > 0 ? "Ник сохранён: " + n : "Ник сброшен: " + Online.Nick, UIKit.Green);
                Window.Refresh();
            }, 28);
            UIKit.Place(ok.Bg.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(300, 82));
        }

        static void SetVol(float v) { Save.P.volume = v; AudioFx.SetVolume(v); Save.Write(); Window.Refresh(); }
        static void SetSens(float v) { Save.P.sens = v; Save.Write(); Window.Refresh(); }

        // ---------------- promo ----------------

        public static void PromoWindow()
        {
            Window.Show("ПРОМОКОД", w =>
            {
                w.Text("Введите промокод — секретное слово, и случится что-то интересное.", 28);
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

        public const long BillionSum = 300_000_000_000L, DevCoins = 10_000_000L, DevTokens = 100_000L;

        public static bool Apply(string code)
        {
            var n = Norm(code);
            var P = Save.P;
            // 17.11 (v0.19): the new set of codes — the old ones (Курица, Чайник, BUTTER 22X…) are gone
            if (n == Norm("BUTTER"))
            {
                P.butter = !P.butter; Save.Write(); Butter.Set(P.butter);
                AudioFx.Play("magic");
                Hud.Toast(P.butter ? "BUTTER! Теперь всё из сливочного масла. Ещё раз «BUTTER» — растопить" : "Масло растаяло — всё снова как было", Parts.Hex("FFE680"));
                return true;
            }
            if (n == Norm("iPhone"))
            {
                // 18.11: every skin in the game at once
                foreach (var it in Catalog.Items.Values) Save.Give(it.id);
                foreach (var st in Catalog.Sets) UnlockSet(st, false);
                Save.Write();
                AudioFx.Play("token"); PlayerCtl.I?.PlayVictory();
                Hud.Toast($"iPhone! Все скины твои: {Catalog.Items.Count} вещей в гардеробе", UIKit.Gold);
                WardrobeUI.Changed();
                return true;
            }
            if (n == Norm("Тайфун"))
            {
                // 18.11: the typhoon blows every skin off the account (the starting look stays)
                P.owned.Clear(); P.sets.Clear(); P.look.Clear(); P.promo = false;
                Save.Fix(); Save.Write();
                PlayerCtl.I?.ApplyLook(Save.CurrentLook());
                AudioFx.Play("portal");
                Hud.Toast("ТАЙФУН! Все скины унесло ветром — ты снова обычный чубрик", UIKit.Cyan);
                WardrobeUI.Changed();
                return true;
            }
            if (n == Norm("Бомж"))
            {
                P.coins = 0; P.tokens = 0; P.teapot = false; Save.Write();
                AudioFx.Play("error");
                Hud.Toast("Все монеты и жетоны пропали — карманы пустые!", UIKit.Orange);
                WardrobeUI.Changed();
                return true;
            }
            // 19.11: songs — the game opens them in YouTube Music (the songs belong to their authors, they are not inside the game)
            if (n == Norm("Фиксай")) { OpenSong("Фиксай Жизнь в розовых очках", "«Жизнь в розовых очках»"); return true; }
            if (n == Norm("Домер")) { OpenSong("Домер Семицвет", "«Семицвет»"); return true; }
            if (n == Norm("Миллиард"))
            {
                P.coins += BillionSum; P.tokens += BillionSum; Save.Write();
                AudioFx.Play("buy"); PlayerCtl.I?.PlayVictory();
                Hud.Toast("МИЛЛИАРД! +300 000 000 000 монет и +300 000 000 000 жетонов!", UIKit.Gold);
                WardrobeUI.Changed();
                return true;
            }
            if (n == Norm("Лукашарики"))
            {
                Window.Close();
                Cartoon.Play();
                return true;
            }
            if (n == Norm("Секрет"))
            {
                Window.Close();
                Secrets.Show();
                return true;
            }
            if (n != Norm(DevCode))
            {
                AudioFx.Play("error");
                Hud.Toast("Такого промокода нет", UIKit.Red);
                return false;
            }
            bool again = Save.P.promo;
            UnlockAll();
            Save.P.coins += DevCoins; Save.P.tokens += DevTokens; Save.Write();
            WardrobeUI.Changed();
            AudioFx.Play("token");
            PlayerCtl.I?.PlayVictory();
            Hud.Toast(again ? "Артём22X снова: +10 000 000 монет, +100 000 жетонов, все 8 премиум-наборов твои!" : "ПРОМОКОД «АРТЁМ22X»: +10 000 000 монет, +100 000 жетонов и все 8 премиум-наборов!", UIKit.Gold);
            return true;
        }

        public static string LastSongUrl;

        static void OpenSong(string query, string title)
        {
            LastSongUrl = "https://music.youtube.com/search?q=" + System.Uri.EscapeDataString(query);
            AudioFx.Play("magic");
            Hud.Toast($"Открываю песню {title} в YouTube Music!", UIKit.Pink);
            if (!Boot.Testing) Application.OpenURL(LastSongUrl);
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
