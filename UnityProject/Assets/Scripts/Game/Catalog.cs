using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>One thing the chubrik can wear / use. Id = "dept_N" (regular) or "dept_pK" (premium set K).</summary>
    public class Item
    {
        public string id, dept, name;
        public int price;          // coins (secret items: tokens)
        public int set = -1;       // premium set index, -1 = regular
        public int finish = -1;    // v0.27: a finish variant (SkinFinish) of the regular item number baseIndex
        public int baseIndex = -1;
        // 21.11: every department as big as «Оружие и инструменты» — what a variant changes besides the finish
        public int tint = -1;      // a colour of Catalog.Tints (effects, skin patterns)
        public float speed = 1f, amp = 1f;   // walk / emote: tempo and how big the moves are
        public float pitch = 1f;   // jump / landing sound
        public int rarity = -1;    // lucky-block rarity of a variant (-1: by the department rules)
        /// <summary>A variant ("dept_vN"): a finish / tint / tempo / pitch of a regular item, or a new named colour.</summary>
        public bool Variant => baseIndex >= 0;
        public bool tokens;        // price is in tokens
        public bool promoOnly;     // only the developer promo code unlocks it
        public string reward;      // won in the game (boss, lucky box), never sold: where to get it
        public Dept D => Catalog.Dept(dept);
        public bool Premium => set >= 0;
        public bool Reward => reward != null;
    }

    public class Dept
    {
        public string id, name;
        public bool optional;      // can be taken off («Снять»)
        public string view;        // wardrobe camera: head / body / back / feet / fx
        public string def;         // default item ("" = nothing)
        public readonly List<Item> items = new List<Item>();
    }

    public class PremiumSet
    {
        public int index, price;   // ₸
        public string name, desc;
        public Color color;
        public readonly List<Item> items = new List<Item>();
        public int bonusCoins = 300, bonusTokens = 2;
    }

    /// <summary>31 departments × 8 items from the design document + 8 premium sets with exclusive items.</summary>
    public static class Catalog
    {
        public static readonly List<Dept> Depts = new List<Dept>();
        public static readonly Dictionary<string, Item> Items = new Dictionary<string, Item>();
        public static readonly List<PremiumSet> Sets = new List<PremiumSet>();
        static readonly Dictionary<string, Dept> byId = new Dictionary<string, Dept>();

        public static Dept Dept(string id) => byId.TryGetValue(id, out var d) ? d : null;
        public static Item Get(string id) => id != null && Items.TryGetValue(id, out var i) ? i : null;
        public static int RegularCount { get; private set; }

        static Catalog()
        {
            D("head", "Форма головы", false, "head", "head_0", 80, "Волчонок", "Квадратная Голова", "Кругляш", "Остроух", "Улыбака", "Серьёзный", "Инопланетянин", "Робо-Лицо");
            D("hair", "Причёска", true, "head", "", 60, "Ёжик", "Ирокез", "Кудряшки", "Длинные пряди", "Лысина с хвостом", "Космо-косички", "Огненный чуб", "Взрыв на макушке");
            D("ears", "Уши", true, "head", "ears_0", 60, "Волчьи", "Медвежьи", "Заячьи длинные", "Эльфийские", "Локаторы", "Рожки демона", "Мягкие висячие", "Неоновые антенны");
            D("hat", "Головные уборы", true, "head", "", 90, "Кепка оранжевая", "Кепка жёлтая", "Бейсболка чёрная", "Шапка с помпоном", "Корона короля", "Пиратская треуголка", "Шляпа волшебника", "Каска строителя");
            D("eyes", "Очки и глаза", true, "head", "", 80, "Солнцезащитные авиаторы", "Очки в роговой оправе", "Кибер-визор", "Монокль", "Повязка пирата", "Звёздочки в глазах", "Неоновые очки", "Лентикулярные стикеры");
            D("mask", "Маски", true, "head", "", 90, "Респиратор", "Маска ниндзя", "Противогаз", "Карнавальная маска", "Кибер-забрало", "Шарф на нос", "Улыбка смайлика", "Маска робота");
            D("neck", "Шарфы и шея", true, "body", "", 70, "Шарф жёлтый", "Шарф полосатый", "Шарф чёрный вязаный", "Платок бандита", "Галстук-бабочка", "Накидка рыцаря", "Ошейник с шипами", "Меховой воротник");
            D("color", "Цвет тела", false, "body", "color_0", 50, "Волчий серый", "Небесный синий", "Ярко-красный", "Мрачный чёрный", "Зелёный лайм", "Фиолетовый неон", "Розовый фламинго", "Белый снежок");
            D("skin", "Узор кожи", true, "body", "", 110, "Тигровые полоски", "Пятна леопарда", "Камуфляж военный", "Звёздное небо", "Клетки шахматные", "Волновой узор", "Голографический блик", "Карбоновое покрытие");
            D("top", "Худи и куртки", true, "body", "", 100, "Худи оверсайз", "Футболка со звездой", "Броник тактический", "Свитер с оленями", "Майка спортивная", "Куртка кожаная", "Шуба жёлтая", "Шуба чёрная");
            D("wings", "Наплечники и крылья", true, "back", "", 220, "Ангельские крылья", "Драконьи крылья", "Плечевые щитки", "Металлические шипы", "Энергетические крылья", "Пушистые крылышки", "Реактивные ранец-крылья", "Наплечные фонарики");
            D("hands", "Перчатки и лапы", true, "body", "", 70, "Перчатки боксёрские", "Лапки кошачьи", "Перчатки без пальцев", "Робо-манипуляторы", "Кожаные краги", "Варежки зимние", "Когтистые лапы", "Перчатки с подсветкой");
            D("wrist", "Браслеты и часы", true, "body", "", 60, "Умные часы", "Напульсник неоновый", "Золотой браслет", "Кожаный ремешок", "Браслет выживальщика", "Часы с голограммой", "Шипованный браслет", "Магический амулет");
            D("back", "Рюкзаки", true, "back", "", 120, "Ранец школьный", "Реактивный ранец", "Щит за спиной", "Гитара рок-звезды", "Рюкзак-слизь", "Крылатый ранец", "Космический баллон", "Сумка-бананка");
            D("tail", "Хвосты", true, "back", "", 120, "Хвост кошачий пушистый", "Неоновый хвост-молния", "Механический хвост", "Драконий хвост с шипами", "Пушистый лисий хвост", "Энергетический хлыст", "Робо-хвост", "Короткий хвостик");
            D("pants", "Шорты и штаны", true, "feet", "", 80, "Шорты летние", "Штаны спортивные", "Джинсы потёртые", "Штаны карго", "Шорты пляжные", "Юбка клетчатая", "Штаны киберпанк", "Брюки классические");
            D("belt", "Пояса и ремни", true, "body", "", 60, "Ремень с бляхой-звездой", "Пояс с подсумками", "Шипованный ремень", "Пояс ниндзя", "Портупея кобальтовая", "Пояс с инструментами", "Золотая цепь", "Тканевый кушак");
            D("shoes", "Обувь", true, "feet", "", 80, "Кроссовки беговые", "Кеды яркие", "Ботинки тяжёлые", "Сандалии с носками", "Сапоги космические", "Кроссовки с подсветкой", "Валенки кибернетические", "Тапочки-лапки");
            D("steps", "Эффекты шагов", true, "fx", "", 150, "Огненные искры", "Мыльные пузыри", "Зелёная слизь", "Пиксельные следы", "Звёздная пыль", "Следы молний", "Ледяные следы", "Золотые монетки");
            D("walk", "Походка", false, "fx", "walk_0", 90, "Уверенная", "Пружинистый шаг", "Крадущийся шаг", "Бег вприпрыжку", "Роботизированная", "Вальяжная", "Лунная походка", "Левитация");
            D("emote", "Эмоции", false, "fx", "emote_2", 70, "Брейк-данс", "Разминка мышц", "Оглядывание", "Сон стоя", "Жонглирование монеткой", "Поза супергероя", "Пожимание плечами", "Подмигивание");
            D("pet", "Питомцы", true, "fx", "", 250, "Летающий робо-шар", "Мини-медвежонок", "Котёнок-призрак", "Энергетический огонёк", "Парящий пиксель", "Маленький дракончик", "Летающая рыба", "Питомец-сгусток");
            D("weapon", "Оружие и инструменты", true, "body", "", 150, "Бластер энергетический", "Гаечный ключ", "Магический посох", "Световой меч", "Рогатка пиксельная", "Бумеранг", "Леденец-дубинка", "Кибер-нож");
            D("wcolor", "Окраска оружия", true, "body", "", 80, "Камуфляж космос", "Золотое покрытие", "Неоновый синий градиент", "Ржавый металл", "Огненный принт", "Карбон чёрный", "Радужная переливчатая", "Кристальный лёд");
            D("aura", "Ауры", true, "fx", "", 260, "Огненное пламя", "Ледяное свечение", "Электрические разряды", "Цифровой глитч", "Золотое сияние", "Тёмная материя", "Радужный ореол", "Розовые сердечки");
            D("jsnd", "Звук прыжка", false, "fx", "jsnd_0", 40, "«Бух!»", "Пружина «Бой»", "Пиксельный писк", "Энергетический взрыв", "Мягкий шлепок", "Реактивный свист", "Пружина аниме", "Басовый удар");
            D("lsnd", "Звук приземления", false, "fx", "lsnd_0", 40, "Стук ботинок", "Приземление робота", "Шуршание травы", "Плюх в воду", "Металлический лязг", "Мягкие лапки котика", "Электронный бах", "Тяжёлый топот");
            D("shadow", "Тень", false, "fx", "shadow_0", 40, "Круглая классическая", "Тень-лужица", "Квадратная", "Неоновая", "Тень-призрак", "Разноцветная", "Тень-звёздочка", "Без тени");
            D("dmg", "Эффект урона", false, "fx", "dmg_0", 60, "Вспышка красного экрана", "Трещина стекла", "Разлёт звёздочек", "Цифровые помехи", "Потеря пикселей", "Удар молнии", "Ледяной осколок", "Взрыв брызг");
            D("win", "Победа и телепорт", false, "fx", "win_0", 90, "Телепортация пикселей", "Салют из конфетти", "Взрыв портала", "Растворение в дыму", "Золотой дождь", "Взлёт на ракете", "Код матрицы", "Магическое сияние");
            D("suit", "Костюмы", true, "body", "", 300, "Курица", "Богач Артём", "Богач Али", "Футболист: красная форма №7", "Футболист: бело-синяя №10", "Футболист: жёлто-зелёная №9",
              "Футболист: полосатая №11", "Вратарь №1", "Баскетболист №23", "Хоккеист №99", "Боксёр-чемпион", "Теннисист", "Пловец", "Лыжник", "Каратист", "Штангист",
              "Робот-машина красный", "Робот-грузовик синий", "Робот-спорткар жёлтый", "Робот-самолёт", "Смартфон", "Кнопочный телефон", "Ноутбук", "Монитор-голова", "Игровой компьютер", "Планшет",
              "Богач Артемий", "Асхан-бродяга");
            D("secret", "Секретный модификатор", true, "body", "", 0, "Режим гиганта", "Режим мини-чубрик", "Растянутый аватар", "Плоский персонаж", "Угольный силуэт", "Прозрачный призрак", "Зеркальное отражение", "Ультимативный аватар «Артём 22 X»");

            // every regular item gets its own price: later items of a department cost a bit more
            foreach (var d in Depts)
            {
                int basePrice = d.items[0].price;
                for (int i = 0; i < d.items.Count; i++)
                {
                    var it = d.items[i];
                    if (d.id == "secret") { it.tokens = true; it.price = 2 + i / 2; it.promoOnly = i == 7; continue; }
                    if (d.id == "suit")
                    {
                        // the chicken comes only with the promo code; Артём's and Али's outfits are the priciest
                        // 17.11: every costume falls out of lucky blocks (the rich ones are legendary)
                        it.price = i == 1 || i == 2 || i == 26 ? 5000 : 400 + (i % 6) * 60;
                        continue;
                    }
                    it.price = Mathf.RoundToInt(basePrice * (1f + i * 0.12f) / 5f) * 5;
                    if (it.id == d.def) it.price = 0;
                }
            }
            // 21.11: «Лунная походка» (walking backwards) is gone; the other walks keep their ids
            Drop("walk_6");
            RegularCount = Items.Count;
            Variants();

            S(1000, "Огненный дракон", "Корона, крылья феникса, лавовый хвост и аура «Инферно»", "FF5A1F",
                ("hat", "Драконья корона"), ("wings", "Крылья феникса"), ("tail", "Лавовый хвост"), ("aura", "Аура «Инферно»"));
            S(1290, "Ледяной принц", "Ледяная корона, шуба барса, посох вьюги и снежный совёнок", "7FD8FF",
                ("hat", "Ледяная корона"), ("top", "Шуба снежного барса"), ("weapon", "Посох вьюги"), ("pet", "Снежный совёнок"));
            S(1490, "Космонавт", "Шлем, ранец-ракета, лунные ботинки и следы комет", "B8C4D6",
                ("hat", "Шлем космонавта"), ("back", "Ранец-ракета"), ("shoes", "Лунные ботинки"), ("steps", "Следы комет"));
            S(1690, "Ниндзя тени", "Маска, катана тени, кимоно ночи и тёмный дым", "5B4A8C",
                ("mask", "Маска теневого ниндзя"), ("weapon", "Катана тени"), ("top", "Кимоно ночи"), ("aura", "Тёмный дым"));
            S(1990, "Кибер-самурай", "Кибер-кабуто, неоновые клинки, кибер-перчатки и плазменная катана", "26F0FF",
                ("hat", "Кибер-кабуто"), ("wings", "Неоновые клинки"), ("hands", "Кибер-перчатки"), ("weapon", "Плазменная катана"));
            S(2290, "Золотой магнат", "Золотой цилиндр, бриллиантовая цепь, очки-бриллианты и свинка-копилка", "FFC83A",
                ("hat", "Золотой цилиндр"), ("neck", "Бриллиантовая цепь"), ("eyes", "Очки-бриллианты"), ("pet", "Золотая свинка-копилка"));
            S(2590, "Радужный единорог", "Рог единорога, радужный хвост, крылья пегаса и радужные следы", "FF7AD9",
                ("hat", "Рог единорога"), ("tail", "Радужный хвост"), ("wings", "Крылья пегаса"), ("steps", "Радужные следы"));
            S(2990, "Король Чубриков", "Корона, королевская мантия, скипетр и королевское сияние", "FFD700",
                ("hat", "Корона Короля Чубриков"), ("top", "Королевская мантия"), ("weapon", "Скипетр"), ("aura", "Королевское сияние"));

            R("hat", "Корона Шипозавра", "Победи Шипозавра на Арене шипов в Мире 2. Защищает от шипов на мостиках");
            R("hat", "Шапка-ушанка", "Морозный лаки-бокс в тайной пещере Мира 4");
            R("neck", "Красный шарф", "Морозный лаки-бокс в тайной пещере Мира 4");
            R("shoes", "Ледяные коньки", "Морозный лаки-бокс в Мире 4. На льду не скользишь и бегаешь на 20% быстрее");
            R("skin", "Алмазный Ледяной Чубрик", "Морозный лаки-бокс в Мире 4. Переливается голубым и оставляет морозный след");
            R("suit", "Золотая курица", "Найди все 10 золотых яиц — они спрятаны лучше всего в игре");
            FillUp();
        }

        static void Drop(string id)
        {
            var it = Get(id);
            if (it == null) return;
            it.D.items.Remove(it);
            Items.Remove(id);
        }

        /// <summary>v0.27: the departments that get finish variants and how many (1000 in all).</summary>
        public static readonly string[] VariantDepts = { "hat", "hair", "ears", "eyes", "mask", "neck", "top", "wings", "hands", "back", "tail", "pants", "belt", "shoes", "weapon", "suit", "wrist" };
        public static int VariantCount { get; private set; }

        /// <summary>v0.27: 1000 more skins — the regular things of 18 departments in 10 finishes («Кепка оранжевая (золото)»).</summary>
        static void Variants()
        {
            int left = 1000;
            for (int di = 0; di < VariantDepts.Length; di++)
            {
                var d = Dept(VariantDepts[di]);
                var baseItems = d.items.FindAll(i => !i.Premium && !i.Reward && i.finish < 0 && i.id != d.def);
                int n = left / (VariantDepts.Length - di);
                for (int v = 0; v < n; v++)
                {
                    var b = baseItems[v % baseItems.Count];
                    int f = (v / baseItems.Count + (v % baseItems.Count)) % SkinFinish.Count;
                    if (v / baseItems.Count >= SkinFinish.Count) break;
                    var it = new Item { id = d.id + "_v" + v, dept = d.id, name = b.name + " (" + SkinFinish.Names[f] + ")", finish = f, baseIndex = Index(b.id), rarity = SkinFinish.Rarity(f), price = Mathf.RoundToInt(b.price * (1.3f + f * 0.1f) / 5f) * 5 };
                    d.items.Add(it);
                    Items[it.id] = it;
                    VariantCount++;
                    left--;
                }
            }
        }

        // ================= 21.11: every department as big as «Оружие и инструменты» =================

        /// <summary>M: things in every department = the biggest department before the top-up (the costumes, 88), counted at start.</summary>
        public static int PerDept { get; private set; }
        /// <summary>Variants added by FillUp (on top of the 1000 of v0.27).</summary>
        public static int FillCount { get; private set; }

        static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        /// <summary>Tints of effects and skin patterns: «Огненные искры (мята)». All bright (the additive sparks need light colours).</summary>
        public static readonly (string n, Color c, int r)[] Tints =
        {
            ("мята", H("5FF0C0"), 1), ("лимон", H("FFF04A"), 0), ("малина", H("FF2E7A"), 1), ("лаванда", H("B48CFF"), 1),
            ("небо", H("5BC8FF"), 0), ("апельсин", H("FF8A1E"), 0), ("лайм", H("A6FF2E"), 0), ("бирюза", H("1FE0D0"), 1),
            ("фуксия", H("FF2DD4"), 2), ("янтарь", H("FFB21E"), 2), ("рубин", H("E0113A"), 2), ("индиго", H("5B4CFF"), 2),
            ("коралл", H("FF7060"), 1), ("снег", H("F4FAFF"), 1), ("перламутр", H("F2D8FF"), 3), ("электрик", H("00E5FF"), 3),
        };

        /// <summary>Walk and emote tempos: speed × size of the moves («Пружинистый шаг (вихрем)»).</summary>
        public static readonly (string n, float speed, float amp, int r)[] Tempos =
        {
            ("быстро", 1.4f, 1f, 0), ("медленно", 0.7f, 1f, 0), ("широко", 1f, 1.35f, 1), ("скромно", 1f, 0.7f, 0),
            ("вихрем", 1.9f, 1.05f, 2), ("лениво", 0.55f, 0.85f, 1), ("размашисто", 1.2f, 1.5f, 2), ("мелко-мелко", 1.65f, 0.6f, 1),
            ("плавно", 0.85f, 1.15f, 1), ("бодро", 1.2f, 1.1f, 0), ("мощно", 1.05f, 1.6f, 3), ("сонно", 0.5f, 0.7f, 2),
        };

        /// <summary>Jump / landing sound pitches («Бух! (тонкий)»).</summary>
        public static readonly (string n, float p, int r)[] Pitches =
        {
            ("тонкий", 1.5f, 1), ("басовитый", 0.6f, 1), ("звонкий", 1.25f, 0), ("гулкий", 0.72f, 0), ("писклявый", 1.85f, 2), ("низкий", 0.84f, 0),
            ("высокий", 1.36f, 0), ("громовой", 0.5f, 3), ("мультяшный", 2.15f, 3), ("бархатный", 0.92f, 1), ("комариный", 2.5f, 2),
        };

        /// <summary>Body colours after the first 8 (colour number 8, 9, …): ChubrikModel.BodyColor.</summary>
        public static readonly (string n, string hex)[] BodyColors =
        {
            ("Мятный леденец", "7FE8C4"), ("Лимонный сорбет", "FFF06A"), ("Мандариновый", "FF9A2E"), ("Коралловый риф", "FF6F61"),
            ("Персиковый", "FFB38A"), ("Абрикосовый", "FFC26B"), ("Вишнёвый сок", "B3123A"), ("Малиновый морс", "E0306E"),
            ("Клубничный", "FF4F6E"), ("Арбузная мякоть", "FF7088"), ("Шоколадный", "6B3A1F"), ("Карамельный", "C98A3E"),
            ("Кофе с молоком", "A9805E"), ("Песочный пляж", "E8CF9A"), ("Медовый", "E8A317"), ("Горчичный", "C9A227"),
            ("Оливковый", "8A8F2E"), ("Фисташковый", "A8D58A"), ("Травяной", "4CAF50"), ("Изумрудный лес", "1E8C5A"),
            ("Хвойный", "2F5E3E"), ("Бирюзовое море", "1FC7C1"), ("Морская волна", "2A9DB8"), ("Аквамарин", "6FE3D9"),
            ("Голубой лёд", "A8E4FF"), ("Васильковый", "4F6BFF"), ("Сапфировый", "1F3FA8"), ("Ночной индиго", "2E2A7A"),
            ("Королевский синий", "2D5BE3"), ("Джинсовый", "4A6E9E"), ("Сиреневый туман", "B79BE8"), ("Лавандовый", "C4A8FF"),
            ("Сливовый", "6E2A6E"), ("Баклажанный", "4A2048"), ("Пурпурный", "A0208C"), ("Фуксия", "FF2DB4"),
            ("Пыльная роза", "D98C9E"), ("Сахарная вата", "FFC1E3"), ("Розовая жвачка", "FF8AD8"), ("Рубиновый", "9B111E"),
            ("Томатный", "E5452E"), ("Кирпичный", "A84632"), ("Ржаво-рыжий", "B5541E"), ("Лисий рыжий", "E9762B"),
            ("Тыквенный", "F28A1D"), ("Солнечный жёлтый", "FFD21F"), ("Банановый", "FFE57A"), ("Ванильный крем", "F7EBC8"),
            ("Молочный", "FFF8EE"), ("Жемчужный", "E8E6F0"), ("Серебристый", "C0C7D0"), ("Графитовый", "3F444C"),
            ("Пепельный", "9A9A9A"), ("Мокрый асфальт", "55595F"), ("Космический чёрный", "15151F"), ("Полярная ночь", "1E2A44"),
            ("Северное сияние", "54F2A8"), ("Кислотный лайм", "C6FF1A"), ("Неоновый оранжевый", "FF6A00"), ("Электрик", "00B3FF"),
            ("Черничный", "3B2F7A"), ("Ежевичный", "4B1F4F"), ("Облачный", "DDE7F2"), ("Карри", "D6A21E"),
            ("Нефритовый", "3FA97A"), ("Терракотовый", "C2603E"), ("Бордовый", "7A1430"), ("Кремовый", "FFF1C9"), ("Мокко", "7A5236"),
            ("Хаки", "8A8A5C"), ("Мандариновый закат", "FF7B3A"), ("Ледяная мята", "BDF5E2"), ("Морковный", "F26B1D"),
            ("Голубика", "6B7FD6"), ("Закатный розовый", "FF7FA0"), ("Шалфей", "9CB59A"), ("Кукурузный", "F5D547"),
            ("Гранатовый", "C0152F"), ("Сумеречный синий", "3A4A8A"), ("Лиловый", "9B5FC0"), ("Глубокий океан", "0F4C6E"),
            ("Медный", "B87333"), ("Тёплый беж", "D9C2A3"), ("Мох", "5E7A3A"), ("Розовый кварц", "F4C2C8"),
        };

        /// <summary>Weapon paints after the first 8 (paint number 8, 9, …): kind 0 metallic, 1 lacquer, 2 pearl, 3 neon glow.</summary>
        public static readonly List<(string n, string hex, int kind)> WeaponColors = MakeWeaponColors();

        static List<(string, string, int)> MakeWeaponColors()
        {
            (string, string)[] stems =
            {
                ("Вишнёвый", "B3123A"), ("Мятный", "5FE0B5"), ("Лимонный", "F5E04A"), ("Мандариновый", "FF8A1E"), ("Коралловый", "FF6F61"),
                ("Малиновый", "D6286A"), ("Изумрудный", "18A060"), ("Бирюзовый", "1FC7C1"), ("Васильковый", "4F6BFF"), ("Сапфировый", "1F3FA8"),
                ("Лавандовый", "B49BFF"), ("Сливовый", "7A2A7A"), ("Розовый", "FF8AC8"), ("Медный", "C26A2E"), ("Бронзовый", "A8742A"),
                ("Кремовый", "F4E8C8"), ("Оливковый", "7A8A2E"), ("Песочный", "D8BE8A"), ("Небесный", "6EC6FF"),
                ("Шоколадный", "6B3A1F"), ("Графитовый", "3F444C"),
            };
            string[] kinds = { "металлик", "лак", "перламутр", "неон" };
            var l = new List<(string, string, int)>();
            for (int k = 0; k < kinds.Length; k++)
                foreach (var (n, hex) in stems)
                {
                    if (k == 3 && (n == "Шоколадный" || n == "Графитовый")) continue;   // a dark neon does not glow
                    l.Add((n + " " + kinds[k], hex, k));
                }
            return l;
        }

        /// <summary>
        /// 21.11: every department gets as many things as «Оружие и инструменты» (N, counted here). Things of v0.27 keep
        /// their ids; the new ones continue the "dept_vN" numbers: geometry, head shapes, pets and secret modifiers —
        /// base × finish; body / weapon colours — new named colours; skin patterns and effects — base × tint;
        /// walks and emotes — base × tempo; jump / landing sounds — base × pitch.
        /// </summary>
        static void FillUp()
        {
            int N = 0;
            foreach (var d in Depts) N = Mathf.Max(N, d.items.Count);   // every department grows to the biggest one (weapons too)
            PerDept = N;
            foreach (var d in Depts)
            {
                if (d.items.Count >= N) continue;
                int v = d.items.FindAll(i => i.Variant).Count;   // the next free "_vN"
                var bases = d.items.FindAll(i => !i.Premium && !i.Reward && !i.Variant);
                var priced = bases.Find(i => i.price > 0);
                int deptPrice = priced != null ? priced.price : 50;
                int BP(Item b) => b.price > 0 ? b.price : deptPrice;
                int Round5(float p) => Mathf.Max(5, Mathf.RoundToInt(p / 5f) * 5);
                Item Add(string name, int baseIndex, int price, int rarity)
                {
                    var it = new Item { id = d.id + "_v" + v, dept = d.id, name = name, baseIndex = baseIndex, price = price, rarity = rarity };
                    d.items.Add(it);
                    Items[it.id] = it;
                    FillCount++;
                    v++;
                    return it;
                }
                // base × modifier, walked diagonally so neighbours differ: k-th pair = (k % bases, (k / bases + k % bases) % mods)
                void Combos(List<Item> bs, int mods, System.Action<Item, int> make, int key0 = 0)
                {
                    if (bs.Count == 0) return;
                    var used = new HashSet<(int, int)>();
                    foreach (var it in d.items) if (it.Variant) used.Add((it.baseIndex, Mod(it)));
                    for (int k = 0; d.items.Count < N && k / bs.Count < mods; k++)
                    {
                        var b = bs[k % bs.Count];
                        int m = (k / bs.Count + k % bs.Count) % mods;
                        if (!used.Add((Index(b.id), key0 + m))) continue;
                        make(b, m);
                    }
                }

                switch (d.id)
                {
                    case "color":
                        for (int k = 0; d.items.Count < N && k < BodyColors.Length; k++)
                        {
                            int r = k % 7 == 6 ? 3 : k % 5 == 4 ? 2 : k % 2 == 1 ? 1 : 0;
                            Add(BodyColors[k].n, 8 + k, Round5(deptPrice * (1.2f + r * 0.25f)), r);
                        }
                        break;
                    case "wcolor":
                        for (int k = 0; d.items.Count < N && k < WeaponColors.Count; k++)
                        {
                            int r = WeaponColors[k].kind == 0 ? 1 : WeaponColors[k].kind == 1 ? 0 : 2;
                            if (r == 2 && k % 4 == 0) r = 3;
                            Add(WeaponColors[k].n, 8 + k, Round5(deptPrice * (1.2f + r * 0.25f)), r);
                        }
                        break;
                    case "skin": case "steps": case "aura": case "dmg": case "win": case "shadow":
                    {
                        var bs = bases;
                        if (d.id == "dmg") bs = bases.FindAll(i => Index(i.id) != 1 && Index(i.id) != 3);      // the screen crack and glitch have no colour
                        if (d.id == "shadow") bs = bases.FindAll(i => Index(i.id) != 5 && Index(i.id) != 7);   // «Разноцветная» and «Без тени»
                        Combos(bs, Tints.Length, (b, m) => Add(b.name + " (" + Tints[m].n + ")", Index(b.id), Round5(BP(b) * (1.25f + Tints[m].r * 0.2f)), Tints[m].r).tint = m);
                        break;
                    }
                    case "walk": case "emote":
                        Combos(bases, Tempos.Length, (b, m) =>
                        {
                            var it = Add(b.name + " (" + Tempos[m].n + ")", Index(b.id), Round5(BP(b) * (1.25f + Tempos[m].r * 0.2f)), Tempos[m].r);
                            it.speed = Tempos[m].speed; it.amp = Tempos[m].amp;
                        });
                        break;
                    case "jsnd": case "lsnd":
                        Combos(bases, Pitches.Length, (b, m) => Add(b.name + " (" + Pitches[m].n + ")", Index(b.id), Round5(BP(b) * (1.25f + Pitches[m].r * 0.2f)), Pitches[m].r).pitch = Pitches[m].p);
                        break;
                    case "secret":
                    {
                        // modifier × body finish, then (when the 10 finishes run out) modifier × body tint
                        var bs = bases.FindAll(i => !i.promoOnly);
                        Combos(bs, SkinFinish.Count, (b, f) =>
                        {
                            var it = Add(b.name + " (" + SkinFinish.Names[f] + ")", Index(b.id), b.price + 1 + (SkinFinish.Rarity(f) >= 3 ? 1 : 0), SkinFinish.Rarity(f));
                            it.finish = f; it.tokens = true;
                        });
                        Combos(bs, Tints.Length, (b, t) =>
                        {
                            var it = Add(b.name + " (" + Tints[t].n + ")", Index(b.id), b.price + 1 + (Tints[t].r >= 3 ? 1 : 0), Tints[t].r);
                            it.tint = t; it.tokens = true;
                        }, 100);
                        break;
                    }
                    default:
                    {
                        // geometry (the v0.27 departments go on with the same bases), head shapes and pets: base × finish
                        // the v0.27 bases first (no default thing), then the default one too (ears) when they run out
                        var bs = System.Array.IndexOf(VariantDepts, d.id) >= 0 ? bases.FindAll(i => i.id != d.def) : bases;
                        Combos(bs, SkinFinish.Count, (b, f) => Add(b.name + " (" + SkinFinish.Names[f] + ")", Index(b.id), Round5(BP(b) * (1.3f + f * 0.1f)), SkinFinish.Rarity(f)).finish = f);
                        bs = bases;
                        Combos(bs, SkinFinish.Count, (b, f) => Add(b.name + " (" + SkinFinish.Names[f] + ")", Index(b.id), Round5(BP(b) * (1.3f + f * 0.1f)), SkinFinish.Rarity(f)).finish = f);
                        break;
                    }
                }
            }
        }

        /// <summary>The modifier number of a variant (finish / tint / tempo / pitch), for the «no two alike» rule.</summary>
        public static int Mod(Item it)
        {
            if (it.finish >= 0) return it.finish;
            if (it.tint >= 0) return it.dept == "secret" ? 100 + it.tint : it.tint;
            if (it.dept == "walk" || it.dept == "emote") return System.Array.FindIndex(Tempos, t => t.speed == it.speed && t.amp == it.amp);
            if (it.dept == "jsnd" || it.dept == "lsnd") return System.Array.FindIndex(Pitches, t => t.p == it.pitch);
            return -1;
        }

        /// <summary>Reward items ("dept_rK"): won, not bought. Their builders use index 200 + K.</summary>
        static void R(string dept, string name, string where)
        {
            var d = Dept(dept);
            int k = d.items.FindAll(i => i.Reward).Count;
            var it = new Item { id = dept + "_r" + k, dept = dept, name = name, reward = where };
            d.items.Add(it);
            Items[it.id] = it;
        }

        public const string Chicken = "suit_0", RichArtem = "suit_1", RichAli = "suit_2", RichArtemiy = "suit_26", Tramp = "suit_27";
        public const string ShipoCrown = "hat_r0", Ushanka = "hat_r1", RedScarf = "neck_r0", Skates = "shoes_r0", IceSkin = "skin_r0";
        /// <summary>The Frosty lucky box of World 4 drops these.</summary>
        public static readonly string[] WinterItems = { Ushanka, RedScarf, Skates, IceSkin };

        static void D(string id, string name, bool optional, string view, string def, int price, params string[] names)
        {
            var d = new Dept { id = id, name = name, optional = optional, view = view, def = def };
            for (int i = 0; i < names.Length; i++)
            {
                var it = new Item { id = id + "_" + i, dept = id, name = names[i], price = price };
                d.items.Add(it);
                Items[it.id] = it;
            }
            Depts.Add(d);
            byId[id] = d;
        }

        static void S(int price, string name, string desc, string hex, params (string dept, string name)[] items)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            var s = new PremiumSet { index = Sets.Count, price = price, name = name, desc = desc, color = c };
            foreach (var (dept, iname) in items)
            {
                var it = new Item { id = dept + "_p" + s.index, dept = dept, name = iname, set = s.index };
                s.items.Add(it);
                Dept(dept).items.Add(it);
                Items[it.id] = it;
            }
            Sets.Add(s);
        }

        /// <summary>Items a loot box can drop: regular, bought with coins.</summary>
        public static List<Item> LootPool()
        {
            var l = new List<Item>();
            foreach (var it in Items.Values) if (!it.Premium && !it.tokens && !it.promoOnly && !it.Reward && it.price > 0 && it.dept != "suit") l.Add(it);
            return l;
        }

        public static int Index(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            if (id.Contains("_v") && Items.TryGetValue(id, out var vi) && vi.baseIndex >= 0) return vi.baseIndex;   // a variant builds as its base (a new colour: its colour number)
            int u = id.LastIndexOf('_');
            if (u < 0) return -1;
            if (u + 1 < id.Length && id[u + 1] == 'r' && int.TryParse(id.Substring(u + 2), out var r)) return 200 + r;   // reward item
            return int.TryParse(id.Substring(u + 1), out var n) ? n : -1;
        }

        /// <summary>The SkinFinish of a finish variant, else -1.</summary>
        public static int FinishOf(string id) => id != null && id.Contains("_v") && Items.TryGetValue(id, out var vi) ? vi.finish : -1;
        /// <summary>The tint colour of a tinted variant (effects, skin pattern), else null.</summary>
        public static Color? TintOf(string id) => id != null && id.Contains("_v") && Items.TryGetValue(id, out var vi) && vi.tint >= 0 ? Tints[vi.tint].c : (Color?)null;
        public static float PitchOf(string id) => id != null && id.Contains("_v") && Items.TryGetValue(id, out var vi) ? vi.pitch : 1f;

        /// <summary>Premium set index of "dept_pK", else -1.</summary>

        public static int SetOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int u = id.LastIndexOf("_p");
            return u >= 0 && int.TryParse(id.Substring(u + 2), out var n) ? n : -1;
        }
    }
}
