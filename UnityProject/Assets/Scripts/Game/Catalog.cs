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
            D("head", "Форма головы", false, "head", "head_0", 80, "Классический Чубрик", "Квадратная Голова", "Кругляш", "Остроух", "Улыбака", "Серьёзный", "Инопланетянин", "Робо-Лицо");
            D("hair", "Причёска", true, "head", "hair_0", 60, "Ёжик", "Ирокез", "Кудряшки", "Длинные пряди", "Лысина с хвостом", "Космо-косички", "Огненный чуб", "Взрыв на макушке");
            D("ears", "Уши", true, "head", "ears_1", 60, "Котёнка", "Медвежьи", "Заячьи длинные", "Эльфийские", "Локаторы", "Рожки демона", "Мягкие висячие", "Неоновые антенны");
            D("hat", "Головные уборы", true, "head", "", 90, "Кепка оранжевая", "Кепка жёлтая", "Бейсболка чёрная", "Шапка с помпоном", "Корона короля", "Пиратская треуголка", "Шляпа волшебника", "Каска строителя");
            D("eyes", "Очки и глаза", true, "head", "", 80, "Солнцезащитные авиаторы", "Очки в роговой оправе", "Кибер-визор", "Монокль", "Повязка пирата", "Звёздочки в глазах", "Неоновые очки", "Лентикулярные стикеры");
            D("mask", "Маски", true, "head", "", 90, "Респиратор", "Маска ниндзя", "Противогаз", "Карнавальная маска", "Кибер-забрало", "Шарф на нос", "Улыбка смайлика", "Маска робота");
            D("neck", "Шарфы и шея", true, "body", "", 70, "Шарф жёлтый", "Шарф полосатый", "Шарф чёрный вязаный", "Платок бандита", "Галстук-бабочка", "Накидка рыцаря", "Ошейник с шипами", "Меховой воротник");
            D("color", "Цвет тела", false, "body", "color_0", 50, "Солнечный жёлтый", "Небесный синий", "Ярко-красный", "Мрачный чёрный", "Зелёный лайм", "Фиолетовый неон", "Розовый фламинго", "Белый снежок");
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
            D("secret", "Секретный модификатор", true, "body", "", 0, "Режим гиганта", "Режим мини-чубрик", "Растянутый аватар", "Плоский персонаж", "Угольный силуэт", "Прозрачный призрак", "Зеркальное отражение", "Ультимативный аватар «Артём 22 X»");

            // every regular item gets its own price: later items of a department cost a bit more
            foreach (var d in Depts)
            {
                int basePrice = d.items[0].price;
                for (int i = 0; i < d.items.Count; i++)
                {
                    var it = d.items[i];
                    if (d.id == "secret") { it.tokens = true; it.price = 2 + i / 2; it.promoOnly = i == 7; continue; }
                    it.price = Mathf.RoundToInt(basePrice * (1f + i * 0.12f) / 5f) * 5;
                    if (it.id == d.def) it.price = 0;
                }
            }
            RegularCount = Items.Count;

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
            foreach (var it in Items.Values) if (!it.Premium && !it.tokens && it.price > 0) l.Add(it);
            return l;
        }

        public static int Index(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int u = id.LastIndexOf('_');
            if (u < 0) return -1;
            if (u + 1 < id.Length && id[u + 1] == 'r' && int.TryParse(id.Substring(u + 2), out var r)) return 200 + r;   // reward item
            return int.TryParse(id.Substring(u + 1), out var n) ? n : -1;
        }

        /// <summary>Premium set index of "dept_pK", else -1.</summary>
        public static int SetOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int u = id.LastIndexOf("_p");
            return u >= 0 && int.TryParse(id.Substring(u + 2), out var n) ? n : -1;
        }
    }
}
