using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// v0.32 «PvP-арена»: a pad next to the football in the neon quarter. A blocky arena of our own (bright blocks,
    /// cover pillars) and the classic 1.8-style kit: a sword with no attack cooldown (click as fast as you can), knockback,
    /// critical hits while falling, a bow with arrows, golden apples (heal + extra yellow hearts) and armour.
    /// 1 against 1 with a bot (easy / medium / hard), the first to win 3 rounds wins the duel.
    /// </summary>
    public static class PvpArena
    {
        public static readonly Vector3 O = new Vector3(3000, 30, 0);
        public const float Half = 13f, MaxHp = 20f;
        public static Transform Root;
        public static Vector3 PadPos;
        public static bool Active { get; private set; }
        public static int Diff { get; private set; }
        public static float Hp, Gold, BotGold;
        public static int Apples, Arrows, MyRounds, BotRounds, Duels, Wins;
        public static PvpBot Bot;
        static float startAt, eatUntil;
        static GameObject hud, sword;
        static TextMeshProUGUI hudTxt, applesTxt, arrowsTxt;
        static RectTransform myBar, myGold, botBar;
        public static readonly string[] DiffNames = { "ЛЁГКИЙ", "СРЕДНИЙ", "СЛОЖНЫЙ" };
        internal static void ResetStatics() { Root = null; Active = false; Bot = null; hud = null; sword = null; }

        public static bool Inside(Vector3 p) => Root != null && Mathf.Abs(p.x - O.x) < 30 && Mathf.Abs(p.z - O.z) < 30 && Mathf.Abs(p.y - O.y) < 25;
        public static bool Fighting => Active && Time.time > startAt;

        // ---------------- the pad in the lobby ----------------

        public static void BuildPad(Transform lobby)
        {
            Physics.SyncTransforms();
            var near = Lobby.FootballNear;
            PadPos = near + new Vector3(-8, 0, -6);
            bool found = false;
            for (int ring = 0; ring < 6 && !found; ring++)
                for (int k = 0; k < 16 && !found; k++)
                {
                    float a = k * Mathf.PI * 2 / 16, r = 6f + ring * 1.5f;
                    var c = new Vector3(near.x + Mathf.Cos(a) * r, 0, near.z + Mathf.Sin(a) * r);
                    if (!Physics.Raycast(c + Vector3.up * 5, Vector3.down, out var hit, 8, Layers.SolidMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y) > 0.2f) continue;
                    if (Physics.CheckBox(c + Vector3.up * 1.5f, new Vector3(1.8f, 1.3f, 1.8f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                    bool busy = false;
                    foreach (var z in Zones.All) if ((z.pos - c).magnitude < 5) busy = true;
                    foreach (var t in Lobby.TokenSpots) if (new Vector2(t.x - c.x, t.z - c.z).magnitude < 3) busy = true;
                    if (busy) continue;
                    PadPos = hit.point; found = true;
                }
            Parts.Layer = Layers.World;
            var pt = Node("PvpPad", lobby, PadPos);
            Cy(pt, new Vector3(0, 0.05f, 0), new Vector3(3.2f, 0.05f, 3.2f), M("B71C1C", 0.3f));
            Ring(pt, new Vector3(0, 0.11f, 0), 1.55f, 0.07f, M("FFFFFF", 0.4f));
            var icon = Node("Swords", pt, new Vector3(0, 1.7f, 0));
            foreach (var sd in new[] { -1f, 1f }) { var s = Node("Sword", icon, Vector3.zero, new Vector3(0, 0, sd * 40)); SwordModel(s, 1.6f); }
            icon.gameObject.AddComponent<Spin>().speed = 50;
            var lbl = Prim.Text(pt, "PVP-АРЕНА", new Vector3(0, 3.2f, 0), 4.2f, Hex("FFFFFF"));
            lbl.gameObject.AddComponent<Billboard>();
            Zones.Add(PadPos, 1.6f, "PVP-АРЕНА", Menu, () => Worlds.Current == 1 && !Inside(PlayerCtl.I.transform.position), Zones.Kind.Step);
        }

        /// <summary>A sword: a light blue blade, a gold guard, a dark grip.</summary>
        public static void SwordModel(Transform t, float len)
        {
            B(t, new Vector3(0, len * 0.55f, 0), new Vector3(0.12f, len * 0.75f, 0.04f) , M("7FE3FF", 0.8f, 0.6f));
            Cone(t, new Vector3(0, len * 0.925f, 0), new Vector3(0.12f, 0.12f, 0.04f), M("7FE3FF", 0.8f, 0.6f));
            B(t, new Vector3(0, len * 0.17f, 0), new Vector3(0.36f, 0.06f, 0.08f), M("FFC83A", 0.9f, 0.8f));
            B(t, new Vector3(0, len * 0.06f, 0), new Vector3(0.06f, len * 0.2f, 0.06f), M("4E342E", 0.3f));
        }

        public static void Menu()
        {
            Window.Show("PVP-АРЕНА", w =>
            {
                w.Text("Набор PvP 1.8: меч без задержки удара — жми B как можно быстрее, отбрасывание, удар в падении сильнее (критический). Лук со стрелами, золотые яблоки лечат и дают жёлтые сердечки, броня. Победи бота в 3 раундах!", 24, UIKit.Gold);
                for (int d = 0; d < 3; d++)
                {
                    int dd = d;
                    w.Row($"Бот: {DiffNames[d].ToLowerInvariant()}", d == 0 ? "Медленно бьёт, часто промахивается" : d == 1 ? "Бьёт быстрее, иногда стреляет из лука" : "Кликает как профи, стреляет и ест яблоки", "В БОЙ", new[] { UIKit.Green, UIKit.Orange, UIKit.Red }[d], () => { Window.Close(); Start(dd); });
                }
                w.Footer.text = $"Побед на арене: {Wins} · награда: 200 / 400 / 800 монет";
            }, 1050, 700);
        }

        // ---------------- the arena ----------------

        static void Build()
        {
            if (Root != null) return;
            Root = new GameObject("PvpArena").transform;
            Parts.Layer = Layers.World;
            var grass = Mats.Tex(Tex.Grid("5BBF4A", "3E8E35", 26), Color.white, 0.1f); grass.mainTextureScale = Vector2.one;
            var stone = Mats.Tex(Tex.Grid("8D949C", "6B7178", 4), Color.white, 0.1f);
            var wood = Mats.Tex(Tex.Grid("B8864B", "8A5F2E", 4), Color.white, 0.1f);
            var leaves = Mats.Tex(Tex.Grid("3FA34D", "2C7A38", 4), Color.white, 0.1f);
            var glass = Glass("BFE8FF", 0.15f);
            Lobby.Solid(PrimitiveType.Cube, O + new Vector3(0, -0.5f, 0), new Vector3(Half * 2, 1, Half * 2), grass, default, Root);
            // walls: stone blocks 4 high, glass above (you see out, can't get out)
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var size = d.x != 0 ? new Vector3(1, 4, Half * 2 + 2) : new Vector3(Half * 2 + 2, 4, 1);
                var w = Lobby.Solid(PrimitiveType.Cube, O + d * (Half + 0.5f) + Vector3.up * 2, size, stone, default, Root);
                w.GetComponent<Renderer>().material.mainTextureScale = new Vector2(Mathf.Max(size.x, size.z) / 1f, 4) / 4f;
                Lobby.Solid(PrimitiveType.Cube, O + d * (Half + 0.5f) + Vector3.up * 9, d.x != 0 ? new Vector3(1, 10, Half * 2 + 2) : new Vector3(Half * 2 + 2, 10, 1), glass, default, Root);
            }
            // cover: block pillars and two little block trees
            var rng = new Rng(18);
            foreach (var p in new[] { new Vector3(-5, 0, 0), new Vector3(5, 0, 0), new Vector3(0, 0, 4), new Vector3(0, 0, -4), new Vector3(-8, 0, 7), new Vector3(8, 0, -7) })
            {
                int h = 1 + rng.I(3);
                for (int y = 0; y < h; y++) Lobby.Solid(PrimitiveType.Cube, O + p + new Vector3(0, 0.5f + y, 0), Vector3.one, stone, default, Root);
            }
            foreach (var p in new[] { new Vector3(-9, 0, -8), new Vector3(9, 0, 8) })
            {
                for (int y = 0; y < 4; y++) Lobby.Solid(PrimitiveType.Cube, O + p + new Vector3(0, 0.5f + y, 0), Vector3.one, wood, default, Root);
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) for (int y = 0; y < 2; y++)
                    if (!(x == 0 && z == 0 && y == 0)) Lobby.Solid(PrimitiveType.Cube, O + p + new Vector3(x, 4.5f + y, z), Vector3.one, leaves, default, Root);
            }
            // the way out
            var exit = O + new Vector3(0, 0.05f, -Half + 1.5f);
            Cy(Root, exit, new Vector3(2.4f, 0.04f, 2.4f), M("1E88E5", 0.4f));
            var t = Prim.Text(Root, "В ЛОББИ", exit + Vector3.up * 2.4f, 3, Color.white); t.gameObject.AddComponent<Billboard>();
            Zones.Add(exit, 1.2f, "В ЛОББИ", Leave, () => Worlds.Current == 1 && Inside(PlayerCtl.I.transform.position) && !Active, Zones.Kind.Step);
            var light = new GameObject("ArenaLight").AddComponent<Light>();
            light.transform.SetParent(Root, false); light.transform.position = O + Vector3.up * 12; light.type = LightType.Point; light.range = 40; light.intensity = 0.6f;
        }

        public static void Start(int diff)
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            Build();
            Diff = Mathf.Clamp(diff, 0, 2);
            MyRounds = BotRounds = 0;
            Active = true;
            Duels++;
            pl.PlayVictory(() => { NewRound(); });
            ShowHud(true);
        }

        static void NewRound()
        {
            var pl = PlayerCtl.I;
            Hp = MaxHp; Gold = 0; Apples = 3; Arrows = 16; eatUntil = -1;
            pl.Teleport(O + new Vector3(0, 0.3f, -9), 0);
            pl.yaw = 0; pl.pitch = 10;
            pl.WaterY = O.y - 15; pl.SafePos = pl.Checkpoint = O + new Vector3(0, 0.5f, -9);
            if (Bot == null) Bot = PvpBot.Spawn(Root, Diff);
            Bot.ResetAt(O + new Vector3(0, 0, 9), Diff);
            GiveSword(true);
            startAt = Time.time + 3f;
            Hud.Toast($"Раунд {MyRounds + BotRounds + 1} · {MyRounds} : {BotRounds} · 3… 2… 1…", UIKit.Gold);
            AudioFx.Play("note4");
        }

        public static void Leave()
        {
            var pl = PlayerCtl.I;
            Active = false;
            ShowHud(false);
            GiveSword(false);
            if (Bot) { Object.Destroy(Bot.gameObject); Bot = null; }
            if (pl == null) return;
            pl.PlayVictory(() =>
            {
                pl.Teleport(PadPos + new Vector3(0, 0.3f, -2.5f), 180);
                pl.WaterY = -0.9f;
                pl.SafePos = pl.Checkpoint = Lobby.Spawn;
            });
        }

        static void GiveSword(bool on)
        {
            if (sword) Object.Destroy(sword);
            sword = null;
            var pl = PlayerCtl.I;
            if (!on || pl == null || pl.Model == null || pl.Model.HandR == null) return;
            var t = Node("PvpSword", pl.Model.HandR, Vector3.zero, new Vector3(90, 0, 0));
            int was = Parts.Layer; Parts.Layer = pl.Model.gameObject.layer;
            SwordModel(t, 1.1f);
            Parts.Layer = was;
            sword = t.gameObject;
        }

        // ---------------- fighting ----------------

        /// <summary>B in the arena: a sword swing (no cooldown, 1.8 style). True when handled.</summary>
        public static bool Swing(PlayerCtl pl)
        {
            if (!Active || !Inside(pl.transform.position)) return false;
            pl.Anim.Attack();
            AudioFx.Play("swing", 0.5f);
            if (!Fighting || Bot == null || Bot.Dead) return true;
            var to = Bot.transform.position - pl.transform.position; to.y = 0;
            if (to.magnitude > 3.1f || Vector3.Angle(pl.transform.forward, to) > 70) return true;
            bool crit = !pl.Grounded && pl.FallingSpeed < -1f;
            Bot.Hit(crit ? 2.4f : 1.6f, to.normalized, crit);
            return true;
        }

        public static void Bow()
        {
            var pl = PlayerCtl.I;
            if (!Fighting || pl == null) return;
            if (Arrows <= 0) { Hud.Toast("Стрелы закончились", UIKit.Orange); return; }
            Arrows--;
            var dir = pl.Cam.transform.forward;
            var from = pl.transform.position + Vector3.up * 1.3f + pl.transform.forward * 0.5f;
            Arrow.Shoot(from, dir * 30f, true);
            AudioFx.Play("swing", 0.8f);
        }

        public static void Apple()
        {
            if (!Fighting || Apples <= 0 || Time.time < eatUntil) return;
            Apples--;
            eatUntil = Time.time + 1.6f;
            AudioFx.Play("coin");
            Hud.Toast("Ням! Золотое яблоко: лечит и даёт жёлтые сердечки", UIKit.Gold);
        }

        /// <summary>The bot (or its arrow) hits us.</summary>
        public static void HurtMe(float dmg, Vector3 dir)
        {
            var pl = PlayerCtl.I;
            if (!Fighting || pl == null) return;
            dmg *= 0.75f;   // armour
            if (Gold > 0) { float g = Mathf.Min(Gold, dmg); Gold -= g; dmg -= g; }
            Hp -= dmg;
            dir.y = 0;
            pl.Knock(dir.normalized * 6.5f + Vector3.up * 5f);
            AudioFx.Play("punch", 0.8f);
            Effects.Emit("star", pl.transform.position + Vector3.up * 1.2f, 5, 0.3f);
            if (Hp <= 0) RoundOver(false);
        }

        public static void BotDown() => RoundOver(true);

        static void RoundOver(bool iWon)
        {
            if (iWon) MyRounds++; else BotRounds++;
            AudioFx.Play(iWon ? "buy" : "error");
            if (MyRounds >= 3 || BotRounds >= 3)
            {
                bool won = MyRounds >= 3;
                if (won)
                {
                    Wins++;
                    int reward = Diff == 0 ? 200 : Diff == 1 ? 400 : 800;
                    Save.AddCoins(reward);
                    Hud.Toast($"ПОБЕДА {MyRounds} : {BotRounds}! +{reward} монет", UIKit.Gold);
                    Effects.Emit("confetti", PlayerCtl.I.transform.position + Vector3.up * 2, 60, 1f, Vector3.up * 4);
                }
                else Hud.Toast($"Бот победил {BotRounds} : {MyRounds}. Попробуй ещё раз!", UIKit.Orange);
                Active = false;
                ShowHud(false);
                GiveSword(false);
                if (Bot) { Object.Destroy(Bot.gameObject); Bot = null; }
                return;
            }
            Hud.Toast(iWon ? "Раунд твой!" : "Раунд у бота", iWon ? UIKit.Lime : UIKit.Orange);
            NewRound();
        }

        // ---------------- the arena's HUD ----------------

        static void ShowHud(bool on)
        {
            if (!on) { if (hud) Object.Destroy(hud); hud = null; return; }
            if (hud) return;
            var root = UIKit.Rect("PvpHud", UIRoot.HudLayer);
            UIKit.Fill(root);
            hud = root.gameObject;
            hud.AddComponent<Runner>();
            var safe = UIRoot.Safe();
            hudTxt = UIKit.Txt(root, "", 30, Color.white);
            UIKit.Place(hudTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90 - safe.w), new Vector2(900, 44));
            myBar = Bar(root, new Vector2(-260, -140 - safe.w), UIKit.Red, "ТЫ", out myGold);
            botBar = Bar(root, new Vector2(260, -140 - safe.w), UIKit.Violet, "БОТ", out _);
            var bow = UIKit.Round(root, "ЛУК", UIKit.Orange, Bow, 110, 24);
            UIKit.Place(bow.Bg.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-520 - safe.z, 140 + safe.y), new Vector2(110, 110));
            arrowsTxt = UIKit.Txt(bow.Bg.transform, "", 20, Color.white);
            UIKit.Place(arrowsTxt.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(120, 30));
            var apple = UIKit.Round(root, "ЯБЛОКО", UIKit.Gold, Apple, 110, 20);
            UIKit.Place(apple.Bg.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-520 - safe.z, 290 + safe.y), new Vector2(110, 110));
            applesTxt = UIKit.Txt(apple.Bg.transform, "", 20, Color.white);
            UIKit.Place(applesTxt.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(120, 30));
            var quit = UIKit.Button(root, "ВЫЙТИ", UIKit.Blue, Leave, 22, "PvpQuit");
            UIKit.Place(quit.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190 - safe.w), new Vector2(180, 56));
        }

        static RectTransform Bar(Transform root, Vector2 pos, Color c, string who, out RectTransform gold)
        {
            var bg = UIKit.Img(root, UIKit.Pill, new Color(0.05f, 0.08f, 0.2f, 0.8f), "Bar " + who);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, new Vector2(420, 40));
            var fill = UIKit.Img(bg.transform, UIKit.Pill, c, "Fill");
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = new Vector2(3, 3); fill.rectTransform.offsetMax = new Vector2(-3, -3);
            var g = UIKit.Img(bg.transform, UIKit.Pill, UIKit.Gold, "Gold");
            g.rectTransform.anchorMin = Vector2.zero; g.rectTransform.anchorMax = new Vector2(0, 1); g.rectTransform.offsetMin = new Vector2(3, 3); g.rectTransform.offsetMax = new Vector2(-3, -3);
            gold = g.rectTransform;
            var t = UIKit.Txt(bg.transform, who, 22, Color.white);
            UIKit.Fill(t.rectTransform);
            return fill.rectTransform;
        }

        class Runner : MonoBehaviour
        {
            void Update()
            {
                var pl = PlayerCtl.I;
                if (pl == null || !Active) return;
                if (!Inside(pl.transform.position) && Time.time > startAt + 1) { Leave(); return; }
                if (eatUntil > 0 && Time.time > eatUntil) { eatUntil = -1; Hp = Mathf.Min(MaxHp, Hp + 4); Gold = 4; Effects.Emit("heart", pl.transform.position + Vector3.up * 1.5f, 8, 0.4f); }
                if (Input.GetKeyDown(KeyCode.R) && !UIRoot.TypingText) Bow();
                if (Input.GetKeyDown(KeyCode.G) && !UIRoot.TypingText) Apple();
                float left = startAt - Time.time;
                hudTxt.text = left > 0 ? $"Раунд {MyRounds + BotRounds + 1} — {Mathf.CeilToInt(left)}" : $"Счёт {MyRounds} : {BotRounds}   ·   бот: {DiffNames[Diff].ToLowerInvariant()}";
                myBar.anchorMax = new Vector2(Mathf.Clamp01(Hp / MaxHp), 1);
                myGold.anchorMax = new Vector2(Mathf.Clamp01(Gold / MaxHp), 1);
                if (Bot) botBar.anchorMax = new Vector2(Mathf.Clamp01(Bot.Hp / MaxHp), 1);
                applesTxt.text = "×" + Apples; arrowsTxt.text = "×" + Arrows;
            }
        }
    }

    /// <summary>The arena bot: walks at you, strafes, clicks (faster when harder), shoots from far away, eats apples.</summary>
    public class PvpBot : MonoBehaviour
    {
        public float Hp;
        public bool Dead;
        int diff, apples;
        Vector3 vel; float hitCd, bowCd, strafe, hurtT, eatUntil = -1;
        ChubrikModel model; ChubrikAnim anim;

        public static PvpBot Spawn(Transform parent, int diff)
        {
            var go = new GameObject("PvpBot");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PvpBot>();
            var l = Look.Default();
            l["color"] = "color_3"; l["neck"] = "neck_5"; l["hands"] = "hands_4"; l["shoes"] = "shoes_2"; l["back"] = "back_2"; l["head"] = "head_5";
            b.model = ChubrikModel.Create("Model", go.transform, l, Layers.World);
            b.anim = b.model.gameObject.AddComponent<ChubrikAnim>(); b.anim.model = b.model;
            var t = Node("BotSword", b.model.HandR, Vector3.zero, new Vector3(90, 0, 0));
            PvpArena.SwordModel(t, 1.1f);
            var tag = Prim.Text(go.transform, "БОТ", new Vector3(0, 2.2f, 0), 2.2f, Hex("FF8080")); tag.gameObject.AddComponent<Billboard>();
            return b;
        }

        public void ResetAt(Vector3 p, int d)
        {
            diff = d; Hp = PvpArena.MaxHp; Dead = false; apples = d == 2 ? 2 : d == 1 ? 1 : 0; vel = Vector3.zero; eatUntil = -1;
            transform.position = p; transform.rotation = Quaternion.Euler(0, 180, 0);
            gameObject.SetActive(true);
        }

        public void Hit(float dmg, Vector3 dir, bool crit)
        {
            if (Dead || hurtT > 0) return;
            hurtT = 0.45f;   // like the 1.8 «hurt ticks»: spam still combos, but not every click counts
            Hp -= dmg * 0.75f;
            vel = dir * 7f + Vector3.up * 5.5f;
            AudioFx.Play("punch", crit ? 1f : 0.7f);
            Effects.Emit(crit ? "star" : "dust", transform.position + Vector3.up * 1.1f, crit ? 10 : 5, 0.35f);
            if (Hp <= 0) { Dead = true; Effects.Emit("smoke", transform.position + Vector3.up, 20, 0.8f); gameObject.SetActive(false); PvpArena.BotDown(); }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            hurtT -= dt; hitCd -= dt; bowCd -= dt;
            var pl = PlayerCtl.I;
            var O = PvpArena.O;
            // gravity and the flat floor
            vel.y -= 25 * dt;
            var p = transform.position + vel * dt;
            if (p.y <= O.y) { p.y = O.y; vel.y = 0; vel.x *= 1 - 6 * dt; vel.z *= 1 - 6 * dt; }
            p.x = Mathf.Clamp(p.x, O.x - PvpArena.Half + 0.6f, O.x + PvpArena.Half - 0.6f);
            p.z = Mathf.Clamp(p.z, O.z - PvpArena.Half + 0.6f, O.z + PvpArena.Half - 0.6f);
            transform.position = p;
            anim.grounded = p.y <= O.y + 0.01f; anim.vy = vel.y;
            if (pl == null || !PvpArena.Fighting) { anim.speed01 = 0; return; }
            var to = pl.transform.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            if (dist > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 10);
            if (eatUntil > 0) { anim.speed01 = 0; if (Time.time > eatUntil) { eatUntil = -1; Hp = Mathf.Min(PvpArena.MaxHp, Hp + 6); Effects.Emit("heart", transform.position + Vector3.up * 1.5f, 8, 0.4f); } return; }
            if (Hp < 8 && apples > 0 && dist > 4) { apples--; eatUntil = Time.time + 1.6f; return; }
            float speed = diff == 0 ? 3.2f : diff == 1 ? 4.2f : 5.2f;
            strafe += dt;
            var side = Vector3.Cross(Vector3.up, to.normalized) * Mathf.Sin(strafe * (diff + 1) * 0.9f);
            var move = dist > 2.2f ? to.normalized + side * 0.5f : side * 0.8f;
            if (anim.grounded && hurtT < 0.2f) { transform.position += move.normalized * speed * dt * Mathf.Clamp01(move.magnitude); anim.speed01 = Mathf.Clamp01(move.magnitude) * 0.9f; }
            // shoot from far away
            if (dist > 8 && diff > 0 && bowCd <= 0)
            {
                bowCd = diff == 1 ? 3.5f : 2.2f;
                var from = transform.position + Vector3.up * 1.3f + transform.forward * 0.5f;
                var aim = pl.transform.position + Vector3.up * 1f - from;
                Arrow.Shoot(from, aim.normalized * 26f + Vector3.up * aim.magnitude * 0.18f, false);
            }
            // click, click, click
            if (dist < 3.0f && hitCd <= 0)
            {
                hitCd = diff == 0 ? 0.6f : diff == 1 ? 0.38f : 0.22f;
                anim.Attack();
                float acc = diff == 0 ? 0.55f : diff == 1 ? 0.72f : 0.88f;
                if (Random.value < acc) PvpArena.HurtMe(1.6f, to);
            }
        }
    }

    /// <summary>An arrow: flies with a little gravity, hits the bot (ours) or us (the bot's), sticks into walls.</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 vel; bool mine; float life;
        public static void Shoot(Vector3 from, Vector3 v, bool mine)
        {
            var go = new GameObject("Arrow");
            go.transform.position = from;
            Parts.Layer = Layers.World;
            B(go.transform, Vector3.zero, new Vector3(0.04f, 0.04f, 0.7f), M("8D6E63", 0.2f));
            Cone(go.transform, new Vector3(0, 0, 0.38f), new Vector3(0.08f, 0.1f, 0.08f), M("B0BEC5", 0.6f, 0.5f), new Vector3(90, 0, 0));
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var a = go.AddComponent<Arrow>(); a.vel = v; a.mine = mine;
            go.transform.rotation = Quaternion.LookRotation(v);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life += dt;
            if (life > 4 || !PvpArena.Active) { Destroy(gameObject); return; }
            if (vel == Vector3.zero) return;
            vel.y -= 9 * dt;
            var step = vel * dt;
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude, Layers.SolidMask, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PvpBot>() == null)
            { transform.position = hit.point; vel = Vector3.zero; return; }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(vel);
            if (mine && PvpArena.Bot != null && !PvpArena.Bot.Dead && Near(PvpArena.Bot.transform.position)) { PvpArena.Bot.Hit(3f, vel.normalized, false); Destroy(gameObject); }
            else if (!mine && PlayerCtl.I != null && Near(PlayerCtl.I.transform.position)) { PvpArena.HurtMe(2.5f, vel); Destroy(gameObject); }
        }

        bool Near(Vector3 who) { var d = transform.position - (who + Vector3.up * 0.8f); return new Vector2(d.x, d.z).magnitude < 0.55f && Mathf.Abs(d.y) < 0.9f; }
    }
}
