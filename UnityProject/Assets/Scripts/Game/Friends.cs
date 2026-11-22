using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// v0.31: every friend in a cage is a real character — a normal name, a whole outfit that fits together (a footballer,
    /// a pirate, a wizard, a builder, a cosmonaut…) instead of random things, and a little story of their own that they
    /// tell when you press B next to them in the lobby.
    /// </summary>
    public static class Friends
    {
        public static readonly string[] Names =
        {
            "Миша", "Даша", "Тимур", "Алина", "Арсен", "Соня", "Ерлан", "Вика", "Данияр", "Полина", "Артур", "Аружан",
            "Кирилл", "Мадина", "Саша", "Айгерим", "Максим", "Лиза", "Нурлан", "Катя", "Ваня", "Амина", "Егор", "Диана",
            "Алихан", "Настя", "Рустам", "Карина", "Дима", "Жанна", "Глеб", "Камила", "Арман", "Ульяна", "Лёша", "Милана",
            "Санжар", "Ева", "Паша", "Асель", "Марк", "Варя", "Олжас", "Злата", "Никита", "Дана", "Слава", "Аня",
            "Ильяс", "Маша", "Богдан", "Томирис", "Рома", "Кира", "Ансар", "Вера", "Денис", "Айым", "Федя", "Оля",
            "Мирас", "Таня", "Лёва", "Сабина", "Андрей", "Нина", "Ерасыл", "Лера", "Гоша", "Мира", "Батыр", "Юля",
            "Женя", "Аяна", "Стёпа", "Лиля", "Азамат", "Василиса", "Петя", "Инжу", "Тёма", "Зарина", "Костя", "Алиса",
            "Бекзат", "Света", "Серёжа", "Ажар", "Витя", "Яна", "Нурсултан", "Рита", "Коля", "Дильназ", "Захар", "Лена",
            "Ислам", "Галя", "Матвей", "Акбота", "Тима", "Женечка", "Гриша", "Жасмин", "Вова", "Аделина", "Лев", "Медина",
            "Елдос", "Ксюша", "Миро", "Сара", "Филя", "Айша", "Эмиль", "Ника",
        };

        /// <summary>One kind of friend: who they are, the outfit (dept=index …) and the stories they tell.</summary>
        public class Kind
        {
            public string who, outfit;
            public string[] stories;
        }

        public static readonly Kind[] Kinds =
        {
            K("футболист", "suit=3 color=2 emote=5",
              "Я забиваю голы с середины поля! Ну, почти. Один раз мяч улетел в реку, и мы полдня его вылавливали.",
              "Мой любимый футбол — у мира 67. Там болельщики так кричат, что у меня уши шевелятся. Приходи сыграть!"),
            K("любитель зимы", "hat=3 neck=1 top=3 hands=5 shoes=6 color=7",
              "Я слепил снеговика выше меня ростом! А утром он стоял уже в другом месте. Может, он ходит по ночам?",
              "Река в снежной долине замёрзла — по ней можно скользить, как на коньках! Только не разгоняйся у ворот."),
            K("пират", "hat=5 eyes=4 neck=3 belt=1 weapon=5 pants=2 shoes=2 color=2",
              "Я ищу сокровища! Говорят, в каждом тайнике спрятан сундук. Сундук открывается кнопкой B — проверь сам!",
              "Однажды мой корабль был размером с ванну. Но я всё равно доплыл до другого берега. Почти сухим!"),
            K("волшебник", "hat=6 weapon=2 neck=5 aura=6 eyes=5 color=5 wrist=7",
              "Я выучил заклинание, чтобы найти потерянные носки. Теперь у меня сорок носков и все чужие.",
              "Магия — это просто. Главное — верить и не взмахивать посохом рядом с бабушкиной вазой."),
            K("строитель", "hat=7 belt=5 weapon=1 top=0 pants=3 shoes=2 hands=4 color=0",
              "Я построил мост через речку! Правда, он был из печенья. Птички его очень хвалили.",
              "Каска — самое главное. Я в ней даже сплю. Мало ли что упадёт ночью!"),
            K("ниндзя", "mask=1 belt=3 color=3 weapon=7 pants=6 hands=2 shoes=7",
              "Я самый тихий ниндзя. Так тихий, что меня три часа не могли найти на обед.",
              "Хочешь секрет? Если два раза прыгнуть и нажать B — полетишь вниз животом. Это мой коронный приём!"),
            K("рок-звезда", "hair=1 eyes=0 top=5 back=3 pants=2 shoes=1 wrist=6 neck=6",
              "Я сыграл концерт для белок в парке. Они хлопали хвостами. Это был мой лучший концерт!",
              "Моя гитара знает только три аккорда. Но это очень громкие аккорды!"),
            K("космонавт", "eyes=2 back=6 shoes=4 hands=3 color=7 pet=0 wrist=5",
              "Я летал на Луну! Ну, на крышу премиум-киоска. Но оттуда тоже очень далеко видно.",
              "В космосе нет гравитации. А у меня в рюкзаке нет бутербродов. Обе проблемы серьёзные."),
            K("король", "hat=4 neck=7 belt=6 wrist=2 top=6 aura=4 color=1",
              "Я король этой полянки! Мои подданные — два жука и одна гусеница. Они очень послушные.",
              "Корона тяжёлая, но красивая. А ещё под ней удобно прятать конфету."),
            K("рыцарь", "neck=5 back=2 wings=2 weapon=3 shoes=2 hands=4 color=0",
              "Я сражался с драконом! Он оказался очень вежливым и угостил меня чаем с мёдом.",
              "Рыцарь всегда защищает друзей. Если кто-то обидит тебя в лобби — зови меня!"),
            K("школьник", "back=0 hat=0 top=1 pants=0 shoes=0 wrist=0 eyes=1",
              "Сегодня я получил пятёрку по математике! Посчитал всех спасённых друзей и не сбился.",
              "В моём рюкзаке тетради, ручка и запасная шапка. А ещё бутерброд, но это секрет."),
            K("хоккеист", "suit=9 emote=1",
              "Шайба — это маленький хоккейный пончик. Только кусать нельзя, проверено!",
              "Я тренируюсь на замёрзшей реке в снежной долине. Там лёд — как зеркало!"),
            K("баскетболист", "suit=8 emote=0",
              "Я прыгаю так высоко, что однажды достал до облака. Оно было мягкое и холодное.",
              "Мяч в кольцо — легко! А вот мяч в мусорку с первого раза — это настоящий талант."),
            K("боксёр", "suit=10 emote=1",
              "Я чемпион по боксу с подушками! Ни один соперник не устоял. Все уснули.",
              "Главное в спорте — зарядка. Каждое утро я машу руками, пока не взлетаю. Шучу!"),
            K("каратист", "suit=14 emote=5",
              "Я разбиваю доски одним ударом! А потом их чиню. Строитель меня уже знает.",
              "Кия! Это я так здороваюсь. Попробуй тоже, громко-громко!"),
            K("лыжник", "suit=13",
              "Я съехал с самой высокой горы! Правда, задом наперёд. Но очень красиво!",
              "Зимой я катаюсь на лыжах, а летом — на лыжах по траве. Это медленнее, зато смешнее."),
            K("пловец", "suit=12 emote=4",
              "Река в лобби течёт сама! Прыгни в неё — и она понесёт тебя, как на водной горке.",
              "Я умею плавать под водой! Зажми B — и ныряй. Там внизу песочек и тишина."),
            K("робот", "suit=16",
              "Бип-буп! Я робот-машина. Заряжаюсь от солнышка и от добрых слов. Скажи мне что-нибудь доброе!",
              "Моя любимая программа — «танцы». Пока она загружается, я просто моргаю фарами."),
            K("геймер", "suit=24",
              "Я прошёл все миры! Почти. В Закулисье я свернул не туда и нашёл там золотое яйцо.",
              "Знаешь, что лучше игры? Игра с другом! Позови кого-нибудь в «Игроки онлайн»."),
            K("ангелочек", "wings=0 aura=7 color=6 hair=2 pet=2",
              "Я умею летать, но только низко-низко. Зато никогда не падаю — просто мягко приземляюсь.",
              "Мой котёнок-призрак боится темноты. Поэтому я всегда сияю для него."),
            K("друг дракончика", "wings=1 tail=3 color=4 pet=5 hands=6",
              "Мой дракончик чихнул и поджарил мне хлеб. Теперь у нас каждый день тосты!",
              "Драконы очень добрые, если их почесать за ушком. Только ушки горячие."),
            K("сыщик", "eyes=3 hat=2 neck=4 pants=7 top=5 shoes=2",
              "Я раскрыл тайну пропавшего пирожка! Его съел… я. Но это было очень хорошее расследование.",
              "Внимательный сыщик видит всё. Например, что в стене рядом с магазином есть щель. Загляни туда!"),
            K("отдыхающий на пляже", "eyes=0 pants=4 shoes=3 top=4 hat=1 back=7 color=1",
              "Сандалии с носками — это стильно! Мне так сказала моя бабушка, а она разбирается.",
              "Я загорал на берегу реки, пока меня не унесло течением. Классно покатался!"),
            K("учёный", "eyes=1 hair=4 neck=4 pants=7 wrist=0 pet=0",
              "Я изобрёл машину, которая делает из одного носка два. Носков стало больше, но они разные.",
              "По моим расчётам, если прыгнуть два раза и зажать B, то отскочишь выше дома. Проверено наукой!"),
            K("котик", "hands=1 tail=0 shoes=7 color=6 ears=6",
              "Мур! Я обожаю спать на солнышке. А ещё гоняться за бабочками. Но спать больше.",
              "Кошачьи лапки очень мягкие, поэтому я хожу бесшумно. Даже ниндзя завидует!"),
            K("тигрёнок", "skin=0 tail=4 color=2 hands=6 ears=1",
              "Р-р-р! Это я рычу. Страшно? Нет? Ну ладно, я ещё маленький тигрёнок.",
              "Полоски у меня от рождения. Я посчитал — их двадцать семь! Или двадцать восемь…"),
            K("теннисист", "suit=11 emote=1",
              "Я отбиваю любой мяч! Даже тот, который кинули в меня случайно на перемене.",
              "Ракетка — мой лучший друг. Второй лучший друг — ты!"),
            K("вратарь", "suit=7 emote=5",
              "Я поймал сто мячей подряд! А сто первый был арбузом. Его мы съели всей командой.",
              "Вратарь должен быть быстрым. Я так быстро прыгаю, что иногда ловлю мяч раньше, чем его пнули."),
        };

        static Kind K(string who, string outfit, params string[] stories) => new Kind { who = who, outfit = outfit, stories = stories };

        /// <summary>The friend number of a cage id: «wNcI» → stable, so the same cage is always the same friend.</summary>
        public static int Index(string id)
        {
            int c = id.IndexOf('c');
            if (id.Length > 1 && id[0] == 'w' && c > 1 && int.TryParse(id.Substring(1, c - 1), out var w) && int.TryParse(id.Substring(c + 1), out var n))
                return w == 7 ? 48 + n : (w - 2) * 8 + n;
            return Mathf.Abs(id.GetHashCode());
        }

        public static string NameOf(string id) => Names[Index(id) % Names.Length];
        public static Kind KindOf(string id) => Kinds[Index(id) % Kinds.Length];

        public static string StoryOf(string id, int n)
        {
            var k = KindOf(id);
            int i = Index(id);
            return k.stories[(i / Kinds.Length + n) % k.stories.Length];
        }

        /// <summary>The friend's whole outfit (the default look plus the kind's things).</summary>
        public static Look LookOf(string id)
        {
            var l = Look.Default();
            var k = KindOf(id);
            foreach (var part in k.outfit.Split(' '))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string dept = part.Substring(0, eq), item = dept + "_" + part.Substring(eq + 1);
                if (Catalog.Get(item) != null) l[dept] = item;
            }
            // the same kind twice looks a little different: another body colour (when the kind doesn't set one) and head
            int i = Index(id), round = i / Kinds.Length;
            if (round > 0)
            {
                if (!k.outfit.Contains("color=")) l["color"] = "color_" + (round * 3 % 8);
                l["head"] = "head_" + new[] { 0, 2, 4, 3, 1, 5 }[round % 6];
            }
            if (!l.ContainsKey("emote") || k.outfit.IndexOf("emote=") < 0) l["emote"] = "emote_" + (i % 8 == 3 ? 2 : i % 8);   // no «сон стоя» for everyone
            return l;
        }

        // ---------------- talking (B next to a friend in the lobby) ----------------

        static GameObject bubble;
        static TextMeshProUGUI bubbleTxt;
        static float bubbleUntil;
        static readonly Dictionary<string, int> told = new Dictionary<string, int>();
        public static int Talks { get; private set; }
        public static string LastStory { get; private set; }
        internal static void ResetStatics() { told.Clear(); if (bubble) Object.Destroy(bubble); bubble = null; }

        /// <summary>B next to a friend in the lobby: he turns to you and tells his story (the next one each time).</summary>
        public static bool TalkNear(Vector3 p)
        {
            if (Worlds.Current != 1) return false;
            Friend best = null; float bd = 2.6f;
            foreach (var f in Object.FindObjectsByType<Friend>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(f.transform.position, p);
                if (d < bd) { bd = d; best = f; }
            }
            if (best == null) return false;
            Talk(best);
            return true;
        }

        public static void Talk(Friend f)
        {
            var id = f.Id;
            told.TryGetValue(id, out int n);
            told[id] = n + 1;
            var story = StoryOf(id, n);
            LastStory = story;
            Talks++;
            f.Listen(PlayerCtl.I ? PlayerCtl.I.transform.position : f.transform.position);
            Show($"<color=#FFC83A>{NameOf(id)}</color> <size=70%><color=#BFD4FF>({KindOf(id).who})</color></size>\n{story}");
            AudioFx.Play("note4", 0.6f);
        }

        static void Show(string text)
        {
            if (bubble == null)
            {
                var bg = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(0.08f, 0.12f, 0.3f, 0.9f), "FriendStory", true);
                UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 230 + UIRoot.Safe().y), new Vector2(1100, 200));
                bubble = bg.gameObject;
                var b = bubble.AddComponent<UIButton>(); b.Bg = bg; b.OnClick = () => bubbleUntil = 0;
                bubbleTxt = UIKit.Txt(bg.transform, "", 30, Color.white, TextAlignmentOptions.MidlineLeft, false);
                UIKit.Fill(bubbleTxt.rectTransform, 28, 14, 28, 14);
                bubbleTxt.enableAutoSizing = true; bubbleTxt.fontSizeMin = 18; bubbleTxt.fontSizeMax = 30;
                bubble.AddComponent<Fader>();
            }
            bubble.SetActive(true);
            bubbleTxt.text = text;
            bubbleUntil = Time.unscaledTime + 3.5f + text.Length * 0.05f;
        }

        class Fader : MonoBehaviour
        {
            void Update() { if (Time.unscaledTime > bubbleUntil || Menus.AnyOpen) gameObject.SetActive(false); }
        }
    }
}
