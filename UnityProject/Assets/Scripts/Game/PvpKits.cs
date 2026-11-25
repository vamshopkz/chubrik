using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// v0.33 the PvP arena's 56 abilities. One is chosen before the fight; «active» ones have a button «СИЛА» with a
    /// cooldown, the others work by themselves. Each costs coins (the game's own coins) or comes free at its level
    /// (a win in the arena = a quarter of a level, up to level 140).
    /// </summary>
    public static class PvpKits
    {
        public class Kit
        {
            public string name, about;
            public bool active;
            public float cd;
            public int price, level;
        }

        public static readonly List<Kit> All = new List<Kit>();
        static void K(string name, bool active, float cd, string about) { int i = All.Count; All.Add(new Kit { name = name, active = active, cd = cd, about = about, price = 300 + i * 250, level = 2 + i * 2 }); }

        static PvpKits()
        {
            K("Мечник", false, 0, "Меч бьёт на 40 % сильнее");
            K("Лучник", false, 0, "Стрелы бьют вдвое сильнее, стрел в два раза больше");
            K("Охранник", true, 12, "Щит: 3 секунды тебя нельзя ранить");
            K("Глайдер", true, 10, "Прыжок вверх и долгое плавное парение");
            K("Весна", true, 7, "Пружина: прыжок выше дома");
            K("ТНТ", true, 9, "Бросает взрывчатку: бах через секунду рядом с врагами");
            K("Снеговик", true, 9, "Снежок замораживает врага на 2 секунды");
            K("Захватчик", true, 8, "Притягивает ближайшего врага к тебе и бьёт");
            K("Летающая рыба", true, 8, "Ныряет по воздуху вперёд и сбивает всех на пути");
            K("Гонщик", false, 0, "Бегаешь на 40 % быстрее");
            K("Джетпак", true, 12, "Летаешь вверх 3 секунды");
            K("Ходок по лаве", false, 0, "Твои удары поджигают врагов");
            K("Целитель", true, 14, "Лечит 8 здоровья");
            K("Рикошет", false, 0, "Стрела отскакивает во второго врага");
            K("Строитель", true, 10, "Ставит перед тобой стенку из блоков");
            K("Алхимик", true, 10, "Случайное зелье: лечение, скорость или сила");
            K("Статика", true, 9, "Молния бьёт врага и перескакивает на соседа");
            K("Один выстрел", true, 15, "Следующая стрела — очень мощная");
            K("Пого", true, 6, "Прыжок-пружина: падаешь на врагов сверху");
            K("Маг", true, 6, "Огненный шар летит во врага");
            K("Скоростной бегун", true, 10, "4 секунды бегаешь вдвое быстрее");
            K("Дуэлист", false, 0, "Ближайший враг получает на 30 % больше урона");
            K("Кактус", false, 0, "Кто тебя бьёт — сам колется");
            K("Рыбак", true, 8, "Удочка: цепляет врага, тянет и оглушает");
            K("Обменщик", true, 10, "Меняется местами с врагом");
            K("Каток", false, 0, "Скользишь быстро, как на коньках");
            K("Коза", true, 8, "Разбег и удар рогами: враги отлетают");
            K("Охотник", true, 10, "Метка: враг 6 секунд получает в полтора раза больше урона");
            K("Телепортёр", true, 9, "Телепорт за спину врагу");
            K("Чемпион", false, 0, "Сильнее бьёшь и меньше получаешь");
            K("Проклятый", false, 0, "Бьёшь очень сильно, но и сам получаешь больше");
            K("Громила", false, 0, "Удар в полтора раза сильнее, но ты медленнее");
            K("Ниндзя", true, 12, "Невидимость 4 секунды, первый удар двойной");
            K("Ползун", true, 12, "Взрыв вокруг себя (немного задевает и тебя)");
            K("Инженер", true, 15, "Ставит турель, она стреляет во врагов");
            K("Дэшер", true, 4, "Быстрый рывок вперёд");
            K("Рыцарь", false, 0, "Крепкая броня: урона на треть меньше");
            K("Убийца", false, 0, "Удар в спину — двойной");
            K("Парные клинки", false, 0, "Каждый удар — сразу двумя клинками");
            K("Некромант", true, 16, "Призывает двух помощников-скелетов");
            K("Счастливый", true, 8, "Случайная способность — повезёт или нет?");
            K("Вампир", false, 0, "Удары лечат тебя");
            K("Паук", true, 10, "Паутина замедляет врагов вокруг");
            K("Ястреб", true, 8, "Ястреб пикирует на врага");
            K("Разрушитель", true, 10, "Удар по земле: враги вокруг подлетают");
            K("Жнец", false, 0, "Слабых врагов добиваешь двойным ударом");
            K("Ангел", true, 14, "Лечит и поднимает в воздух");
            K("Повелитель приливов", true, 12, "Волна отталкивает всех врагов");
            K("Чумной доктор", true, 12, "Ядовитое облако вокруг");
            K("Хранитель", true, 14, "Пузырь: 5 секунд урона на 70 % меньше");
            K("Берсерк", false, 0, "Чем меньше здоровья, тем сильнее удар");
            K("Монах", true, 12, "Лечение и толчок ладонями");
            K("Хрономант", true, 15, "Возвращает здоровье, какое было 4 секунды назад");
            K("Булава", true, 9, "Прыжок и удар булавой по земле");
            K("Феникс", false, 0, "Один раз за раунд оживает из огня");
            K("Подражатель", true, 10, "Повторяет случайную чужую способность");
        }

        public static int Level => Mathf.Min(140, Mathf.FloorToInt(Save.P.pvpXp) + 1);
        public static bool Owns(int i) => i >= 0 && (Save.P.pvpKits.Contains(i) || Level >= All[i].level);
        public static int Chosen => Owns(Save.P.pvpKit) ? Save.P.pvpKit : -1;
        public static bool Has(string name) { int c = Chosen; return c >= 0 && All[c].name == name; }

        // ---------------- the player's state ----------------
        public static float shieldUntil, bubbleUntil, invisUntil, speedUntil, strongUntil, flyUntil, glideUntil, cdUntil, oneShot;
        public static bool firstHitDouble, phoenixUsed;
        static readonly Queue<(float t, float hp)> history = new Queue<(float, float)>();
        public static int Uses { get; private set; }
        public static void ResetRound() { shieldUntil = bubbleUntil = invisUntil = speedUntil = strongUntil = flyUntil = glideUntil = cdUntil = 0; oneShot = 0; firstHitDouble = false; phoenixUsed = false; history.Clear(); }

        public static float SpeedK()
        {
            float k = 1;
            if (Has("Гонщик")) k *= 1.4f;
            if (Has("Каток")) k *= 1.25f;
            if (Has("Громила")) k *= 0.85f;
            if (Time.time < speedUntil) k *= 2f;
            return k;
        }

        /// <summary>How much harder our hit is on this bot.</summary>
        public static float DamageK(PvpBot b, bool melee)
        {
            float k = 1;
            if (melee && Has("Мечник")) k *= 1.4f;
            if (Has("Чемпион")) k *= 1.2f;
            if (Has("Проклятый")) k *= 1.6f;
            if (Has("Громила")) k *= 1.5f;
            if (Has("Берсерк")) k *= 1 + (1 - PvpArena.Hp / PvpArena.MaxHp);
            if (Has("Дуэлист") && PvpArena.Nearest(PlayerCtl.I.transform.position, 99) == b) k *= 1.3f;
            if (Has("Жнец") && b.Hp < PvpArena.MaxHp * 0.25f) k *= 2;
            if (Has("Убийца") && melee && Vector3.Dot(b.transform.forward, (b.transform.position - PlayerCtl.I.transform.position).normalized) > 0.3f) k *= 2;
            if (Time.time < strongUntil) k *= 1.5f;
            if (b.markedUntil > Time.time) k *= 1.5f;
            if (firstHitDouble && melee) { k *= 2; firstHitDouble = false; invisUntil = 0; }
            return k;
        }

        public static float TakenK()
        {
            float k = 1;
            if (Has("Рыцарь")) k *= 0.65f;
            if (Has("Чемпион")) k *= 0.8f;
            if (Has("Проклятый")) k *= 1.3f;
            if (Time.time < bubbleUntil) k *= 0.3f;
            if (Time.time < shieldUntil) k = 0;
            return k;
        }

        /// <summary>After our hit lands: vampire heals, lava burns, twin blades hit again.</summary>
        public static void AfterHit(PvpBot b, float dealt, bool melee)
        {
            if (Has("Вампир")) PvpArena.Hp = Mathf.Min(PvpArena.MaxHp, PvpArena.Hp + dealt * 0.3f);
            if (melee && Has("Ходок по лаве")) b.burnUntil = Time.time + 3;
            if (melee && Has("Парные клинки") && !b.Dead) b.Hit(dealt * 0.5f, Vector3.zero, false, true);
        }

        /// <summary>The bot hit us: the cactus pricks back.</summary>
        public static void WhenHurt(PvpBot from, float dmg) { if (from != null && Has("Кактус")) from.Hit(dmg * 0.4f + 0.4f, Vector3.zero, false, true); }

        /// <summary>We would fall: the phoenix rises once a round.</summary>
        public static bool Revive()
        {
            if (!Has("Феникс") || phoenixUsed) return false;
            phoenixUsed = true;
            PvpArena.Hp = 10;
            var p = PlayerCtl.I.transform.position;
            Effects.Emit("fire", p + Vector3.up, 50, 1.2f, Vector3.up * 3); AudioFx.Play("magic");
            Hud.Toast("Феникс! Ты возродился из огня", UIKit.Orange);
            return true;
        }

        public static void Tick()
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            history.Enqueue((Time.time, PvpArena.Hp));
            while (history.Count > 0 && Time.time - history.Peek().t > 4.2f) history.Dequeue();
            if (Time.time < flyUntil) pl.SetUpVel(7f);
            else if (Time.time < glideUntil && pl.FallingSpeed < -1.5f) pl.SetUpVel(-1.5f);
            pl.Model.gameObject.SetActive(!(Time.time < invisUntil) && HideSeek.Current < 0);
        }

        public static float CdLeft => Mathf.Max(0, cdUntil - Time.time);

        /// <summary>The «СИЛА» button.</summary>
        public static void Use()
        {
            int c = Chosen;
            if (c < 0 || !All[c].active || !PvpArena.Fighting) return;
            if (Time.time < cdUntil) { AudioFx.Play("error"); return; }
            cdUntil = Time.time + All[c].cd;
            Uses++;
            Do(All[c].name, 0);
        }

        static void Do(string name, int depth)
        {
            var pl = PlayerCtl.I; var p = pl.transform.position; var fwd = pl.transform.forward;
            var near = PvpArena.Nearest(p, 30);
            AudioFx.Play("magic", 0.6f);
            switch (name)
            {
                case "Охранник": shieldUntil = Time.time + 3; Effects.Emit("gold", p + Vector3.up, 30, 0.8f); break;
                case "Глайдер": pl.SetUpVel(13); glideUntil = Time.time + 5; break;
                case "Весна": pl.SetUpVel(20); AudioFx.Play("boing"); break;
                case "ТНТ": Bomb.Throw(p + Vector3.up * 1.2f, fwd * 9 + Vector3.up * 5, 6f, 3.5f); break;
                case "Снеговик": if (near) { near.freezeUntil = Time.time + 2; Effects.Emit("ice", near.transform.position + Vector3.up, 30, 0.6f); } break;
                case "Захватчик": if (near) { near.Pull(p + fwd * 1.5f); PvpArena.Damage(near, 2, (near.transform.position - p).normalized, false); } break;
                case "Летающая рыба": pl.Knock(fwd * 16 + Vector3.up * 3); foreach (var b in PvpArena.Within(p + fwd * 4, 5)) PvpArena.Damage(b, 3, fwd, false); Effects.Emit("splash", p, 30, 0.5f, Vector3.up * 3); break;
                case "Джетпак": flyUntil = Time.time + 3; Effects.Emit("fire", p, 20, 0.4f, Vector3.down * 2); break;
                case "Целитель": PvpArena.Hp = Mathf.Min(PvpArena.MaxHp, PvpArena.Hp + 8); Effects.Emit("heart", p + Vector3.up * 1.5f, 15, 0.5f); break;
                case "Строитель": PvpArena.Wall(p + fwd * 2.2f, pl.transform.rotation); break;
                case "Алхимик": { int r = Random.Range(0, 3); if (r == 0) PvpArena.Hp = Mathf.Min(PvpArena.MaxHp, PvpArena.Hp + 6); else if (r == 1) speedUntil = Time.time + 5; else strongUntil = Time.time + 5; Hud.Toast(r == 0 ? "Зелье лечения!" : r == 1 ? "Зелье скорости!" : "Зелье силы!", UIKit.Violet); Effects.Emit("magic", p + Vector3.up, 25, 0.6f); break; }
                case "Статика": if (near) { PvpArena.Damage(near, 4, Vector3.zero, false); Effects.Emit("elec", near.transform.position + Vector3.up, 30, 0.6f); var n2 = PvpArena.Nearest(near.transform.position, 8, near); if (n2) { PvpArena.Damage(n2, 2.5f, Vector3.zero, false); Effects.Emit("elec", n2.transform.position + Vector3.up, 20, 0.5f); } } break;
                case "Один выстрел": oneShot = 1; Hud.Toast("Следующая стрела — мощная!", UIKit.Gold); break;
                case "Пого": pl.SetUpVel(14); PvpArena.SlamOnLand = 4; break;
                case "Маг": Bomb.Fireball(p + Vector3.up * 1.3f + fwd * 0.6f, (near ? (near.transform.position + Vector3.up - p - Vector3.up * 1.3f).normalized : fwd) * 20, 5); break;
                case "Скоростной бегун": speedUntil = Time.time + 4; break;
                case "Рыбак": if (near) { near.Pull(p + fwd * 2); near.freezeUntil = Time.time + 1; } break;
                case "Обменщик": if (near) { var bp = near.transform.position; near.transform.position = p; pl.Teleport(bp + Vector3.up * 0.2f); Effects.Emit("magic", bp, 20, 0.5f); } break;
                case "Коза": pl.Knock(fwd * 14 + Vector3.up * 2); foreach (var b in PvpArena.Within(p + fwd * 3, 4)) PvpArena.Damage(b, 3, fwd, false); break;
                case "Охотник": if (near) { near.markedUntil = Time.time + 6; Effects.Emit("star", near.transform.position + Vector3.up * 2, 15, 0.4f); } break;
                case "Телепортёр": if (near) { var behind = near.transform.position - near.transform.forward * 1.6f; pl.Teleport(behind + Vector3.up * 0.2f, near.transform.eulerAngles.y); Effects.Emit("magic", behind, 30, 0.6f); } break;
                case "Ниндзя": invisUntil = Time.time + 4; firstHitDouble = true; Effects.Emit("smoke", p + Vector3.up, 25, 0.6f); break;
                case "Ползун": foreach (var b in PvpArena.Within(p, 3.5f)) PvpArena.Damage(b, 7, (b.transform.position - p).normalized, false); PvpArena.Hp -= 2; Effects.Emit("smoke", p + Vector3.up, 40, 1f); Effects.Emit("fire", p + Vector3.up, 30, 1f); AudioFx.Play("box"); break;
                case "Инженер": Helper.Turret(p + fwd * 1.5f); break;
                case "Дэшер": pl.Knock(fwd * 18 + Vector3.up * 1.5f); break;
                case "Некромант": Helper.Skeleton(p + pl.transform.right * 1.5f); Helper.Skeleton(p - pl.transform.right * 1.5f); break;
                case "Счастливый": if (depth == 0) { var pick = All[Random.Range(0, All.Count)]; int guard = 0; while ((!pick.active || pick.name == "Счастливый" || pick.name == "Подражатель") && guard++ < 50) pick = All[Random.Range(0, All.Count)]; Hud.Toast("Повезло: " + pick.name, UIKit.Gold); Do(pick.name, 1); } break;
                case "Паук": foreach (var b in PvpArena.Within(p, 5)) b.slowUntil = Time.time + 4; Effects.Emit("puff", p, 30, 1.2f); break;
                case "Ястреб": if (near) Bomb.Hawk(p + Vector3.up * 6, near, 4); break;
                case "Разрушитель": foreach (var b in PvpArena.Within(p, 4.5f)) { PvpArena.Damage(b, 5, Vector3.zero, false); b.Launch(Vector3.up * 9); } Effects.Emit("dust", p, 50, 1.5f); AudioFx.Play("land4"); break;
                case "Ангел": PvpArena.Hp = Mathf.Min(PvpArena.MaxHp, PvpArena.Hp + 6); flyUntil = Time.time + 1.5f; Effects.Emit("gold", p + Vector3.up, 30, 0.8f); break;
                case "Повелитель приливов": foreach (var b in PvpArena.Within(p, 9)) PvpArena.Damage(b, 3, (b.transform.position - p).normalized * 2, false); Effects.Emit("splash", p, 70, 2f, Vector3.up * 5); AudioFx.Play("splash"); break;
                case "Чумной доктор": Helper.Cloud(p); break;
                case "Хранитель": bubbleUntil = Time.time + 5; Effects.Emit("bubble", p + Vector3.up, 30, 1f); break;
                case "Монах": PvpArena.Hp = Mathf.Min(PvpArena.MaxHp, PvpArena.Hp + 4); foreach (var b in PvpArena.Within(p, 4)) PvpArena.Damage(b, 1.5f, (b.transform.position - p).normalized * 1.5f, false); break;
                case "Хрономант": if (history.Count > 0) { PvpArena.Hp = Mathf.Max(PvpArena.Hp, history.Peek().hp); Effects.Emit("magic", p + Vector3.up, 30, 0.8f); } break;
                case "Булава": pl.SetUpVel(12); PvpArena.SlamOnLand = 6; break;
                case "Подражатель": if (depth == 0) { var pick = All[Random.Range(0, All.Count)]; int guard = 0; while ((!pick.active || pick.name == "Подражатель" || pick.name == "Счастливый") && guard++ < 50) pick = All[Random.Range(0, All.Count)]; Hud.Toast("Повторяю: " + pick.name, UIKit.Violet); Do(pick.name, 1); } break;
            }
        }
    }

    /// <summary>Thrown and flying things of the abilities: TNT, fireballs, the hawk.</summary>
    public class Bomb : MonoBehaviour
    {
        Vector3 vel; float dmg, radius, fuse = -1; PvpBot target; int kind;   // 0 tnt, 1 fireball, 2 hawk

        public static void Throw(Vector3 from, Vector3 v, float dmg, float r)
        {
            var b = Make(from, 0);
            Parts.B(b.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), Parts.M("D32F2F", 0.3f));
            Parts.B(b.transform, Vector3.zero, new Vector3(0.52f, 0.18f, 0.52f), Parts.M("FAFAFA", 0.3f));
            b.vel = v; b.dmg = dmg; b.radius = r; b.fuse = 1.2f;
        }

        public static void Fireball(Vector3 from, Vector3 v, float dmg)
        {
            var b = Make(from, 1);
            Parts.S(b.transform, Vector3.zero, 0.45f, Parts.G("FF7A1E", 2f));
            b.vel = v; b.dmg = dmg; b.radius = 1.6f;
        }

        public static void Hawk(Vector3 from, PvpBot t, float dmg)
        {
            var b = Make(from, 2);
            Parts.S(b.transform, Vector3.zero, new Vector3(0.4f, 0.3f, 0.6f), Parts.M("8D6E63", 0.3f));
            foreach (var sd in new[] { -1f, 1f }) Parts.B(b.transform, new Vector3(sd * 0.45f, 0.05f, 0), new Vector3(0.6f, 0.04f, 0.3f), Parts.M("6D4C41", 0.3f), new Vector3(0, 0, sd * 15));
            Parts.Cone(b.transform, new Vector3(0, 0, 0.35f), new Vector3(0.1f, 0.15f, 0.1f), Parts.M("FFC107", 0.4f), new Vector3(90, 0, 0));
            b.target = t; b.dmg = dmg;
        }

        static Bomb Make(Vector3 at, int kind)
        {
            var go = new GameObject("AbilityThing");
            go.transform.position = at;
            Parts.Layer = Layers.World;
            var b = go.AddComponent<Bomb>(); b.kind = kind;
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!PvpArena.Active) { Destroy(gameObject); return; }
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            if (kind == 2)
            {
                if (target == null || target.Dead) { Destroy(gameObject); return; }
                var to = target.transform.position + Vector3.up - transform.position;
                transform.position += to.normalized * 16 * dt;
                transform.rotation = Quaternion.LookRotation(to);
                if (to.magnitude < 0.8f) { PvpArena.Damage(target, dmg, to.normalized, false); Effects.Emit("dust", transform.position, 15, 0.4f); Destroy(gameObject); }
                return;
            }
            vel.y -= (kind == 0 ? 18 : 2) * dt;
            transform.position += vel * dt;
            if (transform.position.y < PvpArena.O.y + 0.25f) { transform.position = new Vector3(transform.position.x, PvpArena.O.y + 0.25f, transform.position.z); vel = Vector3.zero; }
            if (kind == 1)
            {
                Effects.Emit("fire", transform.position, 1, 0.1f);
                var hit = PvpArena.Nearest(transform.position, 1.2f);
                if (hit != null || vel == Vector3.zero || (transform.position - PvpArena.O).magnitude > 30) Boom();
                return;
            }
            fuse -= dt;
            if (fuse <= 0) Boom();
        }

        void Boom()
        {
            foreach (var b in PvpArena.Within(transform.position, radius)) PvpArena.Damage(b, dmg, (b.transform.position - transform.position).normalized * 1.5f, false);
            Effects.Emit("fire", transform.position, 30, radius * 0.4f); Effects.Emit("smoke", transform.position, 20, radius * 0.4f);
            AudioFx.Play("box");
            Destroy(gameObject);
        }
    }

    /// <summary>Helpers on our side: the engineer's turret, the necromancer's skeletons, the plague cloud.</summary>
    public class Helper : MonoBehaviour
    {
        int kind; float life, t;
        public static void Turret(Vector3 at)
        {
            var h = Make(at, 0, 10);
            Parts.Cy(h.transform, new Vector3(0, 0.4f, 0), new Vector3(0.6f, 0.4f, 0.6f), Parts.M("607D8B", 0.5f, 0.5f));
            Parts.B(h.transform, new Vector3(0, 0.95f, 0.2f), new Vector3(0.25f, 0.25f, 0.8f), Parts.M("37474F", 0.5f, 0.5f));
        }
        public static void Skeleton(Vector3 at)
        {
            var h = Make(at, 1, 12);
            var l = Look.Default(); l["color"] = "color_7"; l["head"] = "head_7";
            var m = ChubrikModel.Create("Skeleton", h.transform, l, Layers.World);
            m.transform.localScale = Vector3.one * 0.8f;
            var an = m.gameObject.AddComponent<ChubrikAnim>(); an.model = m;
        }
        public static void Cloud(Vector3 at) { Make(at, 2, 5); }

        static Helper Make(Vector3 at, int kind, float life)
        {
            var go = new GameObject("AbilityHelper");
            go.transform.position = new Vector3(at.x, PvpArena.O.y, at.z);
            Parts.Layer = Layers.World;
            var h = go.AddComponent<Helper>(); h.kind = kind; h.life = life;
            return h;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt; t -= dt;
            if (life <= 0 || !PvpArena.Active) { Destroy(gameObject); return; }
            foreach (var c in GetComponentsInChildren<Collider>()) Destroy(c);
            var near = PvpArena.Nearest(transform.position, kind == 0 ? 20 : 30);
            if (kind == 0 && near && t <= 0)
            {
                t = 0.8f;
                var from = transform.position + Vector3.up * 1f;
                Arrow.Shoot(from, (near.transform.position + Vector3.up - from).normalized * 28, true);
                transform.rotation = Quaternion.LookRotation(near.transform.position - transform.position);
            }
            else if (kind == 1 && near)
            {
                var to = near.transform.position - transform.position; to.y = 0;
                if (to.magnitude > 1.6f) transform.position += to.normalized * 3.5f * dt;
                else if (t <= 0) { t = 0.9f; PvpArena.Damage(near, 1.2f, to.normalized, false); }
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
                var an = GetComponentInChildren<ChubrikAnim>(); if (an) an.speed01 = to.magnitude > 1.6f ? 0.7f : 0;
            }
            else if (kind == 2)
            {
                if (Random.value < dt * 20) Effects.Emit("slime", transform.position + Random.insideUnitSphere * 3 + Vector3.up, 1, 0.3f);
                if (t <= 0) { t = 1f; foreach (var b in PvpArena.Within(transform.position, 4)) PvpArena.Damage(b, 1, Vector3.zero, false); }
            }
        }
    }
}
