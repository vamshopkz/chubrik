using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// v0.32–0.33 «PvP-арена»: a pad next to the football in the neon quarter. A blocky arena of our own and the 1.8-style
    /// fight: a sword with no attack cooldown, knockback, critical hits while falling, a bow, golden apples, armour.
    /// v0.33: 56 abilities (<see cref="PvpKits"/>), an inventory row at the bottom, the level bar (every win = a quarter
    /// of a level, up to 140; each level brings its ability), 100 coins for every win, and «Бой со всеми» — four bots
    /// that keep coming back — besides the 1-on-1 duel to 3 rounds.
    /// </summary>
    public static class PvpArena
    {
        public static readonly Vector3 O = new Vector3(3000, 30, 0);
        public const float Half = 13f, MaxHp = 20f;
        public static Transform Root;
        public static Vector3 PadPos;
        public static bool Active { get; private set; }
        public static bool Ffa { get; private set; }
        public static int Diff { get; private set; }
        public static float Hp, Gold;
        public static int Apples, Arrows, MyRounds, BotRounds, Duels, Wins, Kills;
        public static readonly List<PvpBot> Bots = new List<PvpBot>();
        public static PvpBot Bot => Bots.Count > 0 ? Bots[0] : null;
        public static float SlamOnLand;
        static float startAt, eatUntil;
        static GameObject hud, sword;
        static TextMeshProUGUI hudTxt, applesTxt, arrowsTxt, powerTxt, lvlTxt;
        static RectTransform myBar, myGold, botBar, xpFill;
        static Image powerImg;
        public static readonly string[] DiffNames = { "ЛЁГКИЙ", "СРЕДНИЙ", "СЛОЖНЫЙ" };
        internal static void ResetStatics() { Root = null; Active = false; Bots.Clear(); hud = null; sword = null; }

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
            B(t, new Vector3(0, len * 0.55f, 0), new Vector3(0.12f, len * 0.75f, 0.04f), M("7FE3FF", 0.8f, 0.6f));
            Cone(t, new Vector3(0, len * 0.925f, 0), new Vector3(0.12f, 0.12f, 0.04f), M("7FE3FF", 0.8f, 0.6f));
            B(t, new Vector3(0, len * 0.17f, 0), new Vector3(0.36f, 0.06f, 0.08f), M("FFC83A", 0.9f, 0.8f));
            B(t, new Vector3(0, len * 0.06f, 0), new Vector3(0.06f, len * 0.2f, 0.06f), M("4E342E", 0.3f));
        }

        public static void Menu()
        {
            Window.Show("PVP-АРЕНА", w =>
            {
                int c = PvpKits.Chosen;
                w.Text($"Уровень {PvpKits.Level} из 140 · способность: {(c >= 0 ? PvpKits.All[c].name : "нет")} · монеты: {UIKit.Num(Save.P.coins)}", 26, UIKit.Gold);
                w.Row("Способности", $"56 способностей: открыто {OwnedCount()} — купи за монеты или получи на своём уровне", "ВЫБРАТЬ", UIKit.Violet, KitsWindow, true, UIKit.Violet);
                var fr = Online.I != null ? Online.I.RoomPeer : null;
                w.Row("Играть с другом по сети", fr != null ? $"Дуэль с «{(string.IsNullOrEmpty(fr.nick) ? "другом" : fr.nick)}» до 3 побед" : "Сначала позови друга: «Игроки онлайн» → пригласить", "ИГРАТЬ", UIKit.Green, InviteFriend, fr != null, UIKit.Green);
                w.Row("Бой со всеми", "4 бота против тебя, побеждённые возвращаются — бейся сколько хочешь", "В БОЙ", UIKit.Red, () => { Window.Close(); Start(1, true); });
                w.Footer.text = "За каждого побеждённого: +100 монет и четверть уровня. Меч — B, лук — R, яблоко — G, сила — F";
            }, 1100, 780);
        }

        static int OwnedCount() { int n = 0; for (int i = 0; i < PvpKits.All.Count; i++) if (PvpKits.Owns(i)) n++; return n; }

        /// <summary>All 56 abilities: buy, choose; locked ones show their price and level.</summary>
        public static void KitsWindow()
        {
            Window.Show($"СПОСОБНОСТИ · уровень {PvpKits.Level}", w =>
            {
                for (int i = 0; i < PvpKits.All.Count; i++)
                {
                    int k = i; var kit = PvpKits.All[i];
                    bool own = PvpKits.Owns(i), chosen = Save.P.pvpKit == i;
                    string sub = kit.about + (kit.active ? $" (кнопка «СИЛА», раз в {kit.cd:0} с)" : " (работает само)");
                    if (own) w.Row((chosen ? "✓ " : "") + kit.name, sub, chosen ? "ВЫБРАНО" : "ВЫБРАТЬ", chosen ? UIKit.Gold : UIKit.Green, () => { Save.P.pvpKit = k; Save.Write(); AudioFx.Play("click"); Window.Refresh(); });
                    else w.Row(kit.name + $" · ур. {kit.level}", sub, UIKit.Num(kit.price) + " МОНЕТ", UIKit.Orange, () =>
                    {
                        if (!Save.Spend(kit.price)) { AudioFx.Play("error"); Hud.Toast("Не хватает монет — побеждай на арене (+100 за каждого)", UIKit.Red); return; }
                        Save.P.pvpKits.Add(k); Save.P.pvpKit = k; Save.Write(); AudioFx.Play("buy");
                        Hud.Toast($"Способность «{kit.name}» твоя!", UIKit.Lime); Window.Refresh();
                    }, Save.P.coins >= kit.price || Cheats.EndlessCoins);
                }
            }, 1150, 820);
        }

        // ---------------- the arena ----------------

        static void Build()
        {
            if (Root != null) return;
            Root = new GameObject("PvpArena").transform;
            Parts.Layer = Layers.World;
            var grass = Mats.Tex(Tex.Grid("5BBF4A", "3E8E35", 26), Color.white, 0.1f);
            var stone = Mats.Tex(Tex.Grid("8D949C", "6B7178", 4), Color.white, 0.1f);
            var wood = Mats.Tex(Tex.Grid("B8864B", "8A5F2E", 4), Color.white, 0.1f);
            var leaves = Mats.Tex(Tex.Grid("3FA34D", "2C7A38", 4), Color.white, 0.1f);
            var glass = Glass("BFE8FF", 0.15f);
            Lobby.Solid(PrimitiveType.Cube, O + new Vector3(0, -0.5f, 0), new Vector3(Half * 2, 1, Half * 2), grass, default, Root);
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var size = d.x != 0 ? new Vector3(1, 4, Half * 2 + 2) : new Vector3(Half * 2 + 2, 4, 1);
                var w = Lobby.Solid(PrimitiveType.Cube, O + d * (Half + 0.5f) + Vector3.up * 2, size, stone, default, Root);
                w.GetComponent<Renderer>().material.mainTextureScale = new Vector2(Mathf.Max(size.x, size.z), 4) / 4f;
                Lobby.Solid(PrimitiveType.Cube, O + d * (Half + 0.5f) + Vector3.up * 9, d.x != 0 ? new Vector3(1, 10, Half * 2 + 2) : new Vector3(Half * 2 + 2, 10, 1), glass, default, Root);
            }
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
            var exit = O + new Vector3(0, 0.05f, -Half + 1.5f);
            Cy(Root, exit, new Vector3(2.4f, 0.04f, 2.4f), M("1E88E5", 0.4f));
            var t = Prim.Text(Root, "В ЛОББИ", exit + Vector3.up * 2.4f, 3, Color.white); t.gameObject.AddComponent<Billboard>();
            Zones.Add(exit, 1.2f, "В ЛОББИ", Leave, () => Worlds.Current == 1 && Inside(PlayerCtl.I.transform.position) && !Active, Zones.Kind.Step);
            var light = new GameObject("ArenaLight").AddComponent<Light>();
            light.transform.SetParent(Root, false); light.transform.position = O + Vector3.up * 12; light.type = LightType.Point; light.range = 40; light.intensity = 0.6f;
        }

        static Material blockM;
        /// <summary>The builder's wall: 3 × 2 blocks in front (gone after 8 s).</summary>
        public static void Wall(Vector3 at, Quaternion rot)
        {
            if (blockM == null) blockM = Mats.Tex(Tex.Grid("C9A26B", "8A6A3E", 4), Color.white, 0.1f);
            var w = new GameObject("BuilderWall").transform;
            w.SetParent(Root, false);
            w.SetPositionAndRotation(new Vector3(at.x, O.y, at.z), Quaternion.Euler(0, rot.eulerAngles.y, 0));
            for (int x = -1; x <= 1; x++) for (int y = 0; y < 2; y++) Lobby.Solid(PrimitiveType.Cube, new Vector3(x, 0.5f + y, 0), Vector3.one, blockM, default, w);
            Object.Destroy(w.gameObject, 8f);
            AudioFx.Play("land4", 0.6f);
        }

        /// <summary>The friend we fight over the network (null: bots).</summary>
        public static string FriendId { get; private set; }
        static bool host = true, entered;

        public static void Start(int diff, bool ffa, string friend = null, bool isHost = true)
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            Build();
            Diff = Mathf.Clamp(diff, 0, 2);
            Ffa = ffa && friend == null;
            FriendId = friend; host = isHost;
            MyRounds = BotRounds = 0;
            entered = false;
            startAt = Time.time + 999;   // v0.33 fix: not «out of the arena» while the chubrik is still teleporting in
            foreach (var b in Bots) if (b) Object.Destroy(b.gameObject);
            Bots.Clear();
            Active = true;
            Duels++;
            pl.PlayVictory(() => { NewRound(); });
            ShowHud(true);
        }

        static void NewRound()
        {
            var pl = PlayerCtl.I;
            Hp = MaxHp; Gold = 0; Apples = 3; Arrows = PvpKits.Has("Лучник") ? 32 : 16; eatUntil = -1; SlamOnLand = 0;
            PvpKits.ResetRound();
            float side = host ? -9 : 9;
            pl.Teleport(O + new Vector3(0, 0.3f, side), host ? 0 : 180);
            pl.yaw = host ? 0 : 180; pl.pitch = 10;
            pl.WaterY = O.y - 15; pl.SafePos = pl.Checkpoint = O + new Vector3(0, 0.5f, side);
            entered = true;
            if (FriendId != null)
            {
                if (Bots.Count == 0) Bots.Add(PvpBot.SpawnRemote(Root, FriendId));
                Bots[0].ResetAt(O + new Vector3(0, 0, -side), 0);
                Net($"PVS|{{0}}|{Hp:0.0}");
            }
            else
            {
                int want = Ffa ? 4 : 1;
                while (Bots.Count < want) Bots.Add(PvpBot.Spawn(Root, Diff, Bots.Count));
                for (int i = 0; i < Bots.Count; i++) Bots[i].ResetAt(BotSpot(i), Diff);
            }
            GiveSword(true);
            startAt = Time.time + 3f;
            Hud.Toast(Ffa ? "Бой со всеми! 3… 2… 1…" : $"Раунд {MyRounds + BotRounds + 1} · {MyRounds} : {BotRounds} · 3… 2… 1…", UIKit.Gold);
            AudioFx.Play("note4");
        }

        static Vector3 BotSpot(int i) => O + (i == 0 ? new Vector3(0, 0, 9) : i == 1 ? new Vector3(-9, 0, 6) : i == 2 ? new Vector3(9, 0, 6) : new Vector3(0, 0, 11));

        public static void Leave()
        {
            var pl = PlayerCtl.I;
            if (FriendId != null && Active) Net("PVQ|{0}");
            FriendId = null;
            Active = false;
            ShowHud(false);
            GiveSword(false);
            foreach (var b in Bots) if (b) Object.Destroy(b.gameObject);
            Bots.Clear();
            if (pl == null) return;
            pl.Model.gameObject.SetActive(true);
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

        // ---------------- with a friend over the network ----------------
        // PVI invite · PVA yes · PVN no · PVH hit (dmg, dir) · PVS my health · PVD I fell · PVQ I left

        static void Net(string fmt)
        {
            var on = Online.I;
            if (on == null || on.RoomWith == null) return;
            on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, string.Format(System.Globalization.CultureInfo.InvariantCulture, fmt, on.Id));
        }

        public static void InviteFriend()
        {
            var fr = Online.I != null ? Online.I.RoomPeer : null;
            if (fr == null) { AudioFx.Play("error"); Hud.Toast("Сначала позови друга: «Игроки онлайн» → пригласить", UIKit.Orange); return; }
            Window.Close();
            Net("PVI|{0}");
            Hud.Toast("Друг получил приглашение на PvP-арену — ждём ответа", UIKit.Cyan);
        }

        public static void OnNet(string[] f)
        {
            var on = Online.I;
            if (on == null || f.Length < 2 || f[1] != on.RoomWith) return;
            var I = System.Globalization.CultureInfo.InvariantCulture;
            switch (f[0])
            {
                case "PVI":
                    Window.Show("PVP-АРЕНА", w =>
                    {
                        w.Text("Друг зовёт тебя на дуэль на PvP-арене!", 30, UIKit.Gold);
                        w.Row("Принять", "Бой до 3 побед", "ИГРАТЬ", UIKit.Green, () => { Window.Close(); Net("PVA|{0}"); Start(1, false, f[1], false); });
                        w.Row("Отказаться", "", "НЕТ", UIKit.Red, () => { Window.Close(); Net("PVN|{0}"); });
                    }, 900, 480);
                    AudioFx.Play("note4");
                    break;
                case "PVA": Hud.Toast("Друг принял! В бой!", UIKit.Lime); Start(1, false, f[1], true); break;
                case "PVN": Hud.Toast("Друг сейчас не хочет на арену", UIKit.Orange); break;
                case "PVH":
                    if (f.Length >= 5 && Active && FriendId == f[1] && float.TryParse(f[2], System.Globalization.NumberStyles.Float, I, out float dmg))
                    {
                        float.TryParse(f[3], System.Globalization.NumberStyles.Float, I, out float dx); float.TryParse(f[4], System.Globalization.NumberStyles.Float, I, out float dz);
                        HurtMe(dmg / 0.75f, new Vector3(dx, 0, dz), Bot);
                    }
                    break;
                case "PVS":
                    if (f.Length >= 3 && Bot != null && Bot.Remote && float.TryParse(f[2], System.Globalization.NumberStyles.Float, I, out float hp)) Bot.Hp = hp;
                    break;
                case "PVD": if (Active && FriendId == f[1] && Bot != null && !Bot.Dead) Bot.RemoteDown(); break;
                case "PVQ": if (Active && FriendId == f[1]) { Hud.Toast("Друг ушёл с арены", UIKit.Orange); FriendId = null; Leave(); } break;
            }
        }

        /// <summary>Our hit on the friend goes to him (his own game counts his health).</summary>
        public static void HitFriend(float dmg, Vector3 dir) => Net(string.Format(System.Globalization.CultureInfo.InvariantCulture, "PVH|{{0}}|{0:0.00}|{1:0.00}|{2:0.00}", dmg, dir.x, dir.z));

        // ---------------- bots near / damage ----------------

        public static PvpBot Nearest(Vector3 p, float max, PvpBot except = null)
        {
            PvpBot best = null; float bd = max;
            foreach (var b in Bots) { if (b == null || b.Dead || b == except) continue; float d = Vector3.Distance(b.transform.position, p); if (d < bd) { bd = d; best = b; } }
            return best;
        }

        public static List<PvpBot> Within(Vector3 p, float r)
        {
            var l = new List<PvpBot>();
            foreach (var b in Bots) if (b != null && !b.Dead && Vector3.Distance(b.transform.position, p) < r) l.Add(b);
            return l;
        }

        /// <summary>Our damage on a bot, with the ability's bonuses (not for melee: <see cref="Swing"/> adds its own).</summary>
        public static void Damage(PvpBot b, float dmg, Vector3 dir, bool melee)
        {
            if (b == null || b.Dead) return;
            float d = dmg * PvpKits.DamageK(b, melee);
            b.Hit(d, dir, false, dir == Vector3.zero);
            PvpKits.AfterHit(b, d, melee);
        }

        // ---------------- fighting ----------------

        /// <summary>B in the arena: a sword swing (no cooldown, 1.8 style). True when handled.</summary>
        public static bool Swing(PlayerCtl pl)
        {
            if (!Active || !Inside(pl.transform.position)) return false;
            pl.Anim.Attack();
            AudioFx.Play("swing", 0.5f);
            if (!Fighting) return true;
            PvpBot target = null; float bd = 3.1f;
            foreach (var b in Bots)
            {
                if (b == null || b.Dead) continue;
                var to = b.transform.position - pl.transform.position; to.y = 0;
                if (to.magnitude < bd && Vector3.Angle(pl.transform.forward, to) < 70) { bd = to.magnitude; target = b; }
            }
            if (target == null) return true;
            bool crit = !pl.Grounded && pl.FallingSpeed < -1f;
            var dir = target.transform.position - pl.transform.position; dir.y = 0;
            float dmg = (crit ? 2.4f : 1.6f) * PvpKits.DamageK(target, true);
            target.Hit(dmg, dir.normalized, crit, false);
            PvpKits.AfterHit(target, dmg, true);
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

        /// <summary>A bot (or its arrow) hits us.</summary>
        public static void HurtMe(float dmg, Vector3 dir, PvpBot from = null)
        {
            var pl = PlayerCtl.I;
            if (!Fighting || pl == null) return;
            dmg *= 0.75f * PvpKits.TakenK();   // armour and the ability
            if (dmg <= 0) { Effects.Emit("gold", pl.transform.position + Vector3.up, 4, 0.3f); return; }
            PvpKits.WhenHurt(from, dmg);
            if (Gold > 0) { float g = Mathf.Min(Gold, dmg); Gold -= g; dmg -= g; }
            Hp -= dmg;
            if (FriendId != null) Net($"PVS|{{0}}|{Mathf.Max(0, Hp):0.0}");
            dir.y = 0;
            pl.Knock(dir.normalized * 6.5f + Vector3.up * 5f);
            AudioFx.Play("punch", 0.8f);
            Effects.Emit("star", pl.transform.position + Vector3.up * 1.2f, 5, 0.3f);
            if (Hp <= 0 && !PvpKits.Revive()) MeDown();
        }

        /// <summary>A bot fell: +100 coins and a quarter of a level (a new level brings its ability).</summary>
        public static void BotDown(PvpBot b)
        {
            Kills++;
            int before = PvpKits.Level;
            Save.P.pvpXp = Mathf.Min(140f, Save.P.pvpXp + 0.25f);
            Save.AddCoins(100);
            int after = PvpKits.Level;
            Hud.Toast($"+100 монет · опыт +¼ уровня", UIKit.Gold);
            if (after > before)
            {
                AudioFx.Play("magic");
                var got = new List<string>();
                for (int i = 0; i < PvpKits.All.Count; i++) if (PvpKits.All[i].level > before && PvpKits.All[i].level <= after) got.Add(PvpKits.All[i].name);
                Hud.Toast($"Уровень {after}!" + (got.Count > 0 ? " Новая способность: " + string.Join(", ", got) : ""), UIKit.Lime);
            }
            Save.Write();
            if (Ffa) { b.RespawnLater(BotSpot(Random.Range(0, 4))); return; }
            RoundOver(true);
        }

        static void MeDown()
        {
            if (FriendId != null) Net("PVD|{0}");
            if (Ffa)
            {
                Hud.Toast("Тебя победили! Снова в бой", UIKit.Orange);
                AudioFx.Play("error");
                Hp = MaxHp; Gold = 0; Apples = 3;
                PvpKits.ResetRound();
                PlayerCtl.I.Teleport(O + new Vector3(0, 0.3f, -9), 0);
                startAt = Time.time + 2f;
                return;
            }
            RoundOver(false);
        }

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
                else Hud.Toast(FriendId != null ? $"Друг победил {BotRounds} : {MyRounds}. Реванш?" : $"Бот победил {BotRounds} : {MyRounds}. Попробуй ещё раз!", UIKit.Orange);
                FriendId = null;
                Active = false;
                ShowHud(false);
                GiveSword(false);
                PlayerCtl.I.Model.gameObject.SetActive(true);
                foreach (var b in Bots) if (b) Object.Destroy(b.gameObject);
                Bots.Clear();
                return;
            }
            Hud.Toast(iWon ? "Раунд твой!" : FriendId != null ? "Раунд у друга" : "Раунд у бота", iWon ? UIKit.Lime : UIKit.Orange);
            NewRound();
        }

        // ---------------- the arena's HUD: bars, the inventory row, the level bar ----------------

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
            var quit = UIKit.Button(root, "ВЫЙТИ", UIKit.Blue, Leave, 22, "PvpQuit");
            UIKit.Place(quit.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190 - safe.w), new Vector2(180, 56));
            // the inventory row: sword, bow, apples, the ability
            var row = UIKit.Img(root, UIKit.Panel, new Color(0.05f, 0.06f, 0.1f, 0.7f), "Inventory");
            UIKit.Place(row.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46 + safe.y), new Vector2(4 * 112 + 20, 120));
            Slot(row.transform, 0, "МЕЧ", UIKit.Cyan, () => Swing(PlayerCtl.I), out _);
            Slot(row.transform, 1, "ЛУК", UIKit.Orange, Bow, out arrowsTxt);
            Slot(row.transform, 2, "ЯБЛОКО", UIKit.Gold, Apple, out applesTxt);
            var pw = Slot(row.transform, 3, "СИЛА", UIKit.Violet, PvpKits.Use, out powerTxt);
            powerImg = pw;
            // the level bar under it
            var xb = UIKit.Img(root, UIKit.Pill, new Color(0.05f, 0.08f, 0.2f, 0.85f), "XpBar");
            UIKit.Place(xb.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14 + safe.y), new Vector2(4 * 112 + 20, 26));
            var xf = UIKit.Img(xb.transform, UIKit.Pill, UIKit.Lime, "Fill");
            xpFill = xf.rectTransform; xpFill.anchorMin = Vector2.zero; xpFill.anchorMax = new Vector2(0, 1); xpFill.offsetMin = new Vector2(3, 3); xpFill.offsetMax = new Vector2(-3, -3);
            lvlTxt = UIKit.Txt(xb.transform, "", 18, Color.white);
            UIKit.Fill(lvlTxt.rectTransform);
        }

        static Image Slot(Transform row, int i, string label, Color c, System.Action act, out TextMeshProUGUI count)
        {
            var s = UIKit.Img(row, UIKit.Btn, c, "Slot " + label, true);
            UIKit.Place(s.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10 + i * 112, 0), new Vector2(104, 104));
            var b = s.gameObject.AddComponent<UIButton>(); b.Bg = s; b.OnClick = act; b.FireOnDown = true;
            var t = UIKit.Txt(s.transform, label, 18, Color.white);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(100, 40));
            t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = 18;
            count = UIKit.Txt(s.transform, "", 24, Color.white);
            UIKit.Place(count.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(100, 40));
            count.enableAutoSizing = true; count.fontSizeMin = 12; count.fontSizeMax = 24;
            return s;
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
                if (entered && !Inside(pl.transform.position) && Time.time > startAt + 1) { Leave(); return; }
                if (FriendId != null && (Online.I == null || Online.I.RoomPeer == null || Online.I.RoomWith != FriendId)) { Hud.Toast("Друг вышел — бой окончен", UIKit.Orange); Leave(); return; }
                PvpKits.Tick();
                if (eatUntil > 0 && Time.time > eatUntil) { eatUntil = -1; Hp = Mathf.Min(MaxHp, Hp + 4); Gold = 4; Effects.Emit("heart", pl.transform.position + Vector3.up * 1.5f, 8, 0.4f); }
                if (SlamOnLand > 0 && pl.Grounded && pl.FallingSpeed <= 0.1f && Time.time > startAt)
                {
                    foreach (var b in Within(pl.transform.position, 3.8f)) { Damage(b, SlamOnLand, (b.transform.position - pl.transform.position).normalized, false); b.Launch(Vector3.up * 7); }
                    Effects.Emit("dust", pl.transform.position, 40, 1.2f); AudioFx.Play("land4");
                    SlamOnLand = 0;
                }
                if (!UIRoot.TypingText)
                {
                    if (Input.GetKeyDown(KeyCode.R)) Bow();
                    if (Input.GetKeyDown(KeyCode.G)) Apple();
                    if (Input.GetKeyDown(KeyCode.F)) PvpKits.Use();
                }
                float left = startAt - Time.time;
                hudTxt.text = left > 0 ? (Ffa ? $"Бой со всеми — {Mathf.CeilToInt(left)}" : $"Раунд {MyRounds + BotRounds + 1} — {Mathf.CeilToInt(left)}") : Ffa ? $"Побеждено: {Kills}" : $"Счёт {MyRounds} : {BotRounds}   ·   бот: {DiffNames[Diff].ToLowerInvariant()}";
                myBar.anchorMax = new Vector2(Mathf.Clamp01(Hp / MaxHp), 1);
                myGold.anchorMax = new Vector2(Mathf.Clamp01(Gold / MaxHp), 1);
                var nb = Nearest(pl.transform.position, 99);
                botBar.anchorMax = new Vector2(nb ? Mathf.Clamp01(nb.Hp / MaxHp) : 0, 1);
                applesTxt.text = "×" + Apples; arrowsTxt.text = "×" + Arrows;
                int c = PvpKits.Chosen;
                if (c < 0) { powerTxt.text = "нет"; powerImg.color = new Color(0.4f, 0.4f, 0.5f); }
                else if (!PvpKits.All[c].active) { powerTxt.text = PvpKits.All[c].name; powerImg.color = UIKit.Violet * 0.8f; }
                else { float cd = PvpKits.CdLeft; powerTxt.text = cd > 0 ? Mathf.CeilToInt(cd) + " с" : PvpKits.All[c].name; powerImg.color = cd > 0 ? new Color(0.35f, 0.3f, 0.5f) : UIKit.Violet; }
                float xp = Save.P.pvpXp;
                xpFill.anchorMax = new Vector2(PvpKits.Level >= 140 ? 1 : xp - Mathf.Floor(xp), 1);
                lvlTxt.text = $"УРОВЕНЬ {PvpKits.Level}";
            }
        }
    }

    /// <summary>An arena bot: walks at you, strafes, clicks (faster when harder), shoots from far away, eats apples.</summary>
    public class PvpBot : MonoBehaviour
    {
        public float Hp;
        public bool Dead;
        public float freezeUntil, slowUntil, burnUntil, markedUntil;
        int diff, apples;
        Vector3 vel; float hitCd, bowCd, strafe, hurtT, eatUntil = -1, burnT, respawnAt = -1; Vector3 respawnPos;
        ChubrikModel model; ChubrikAnim anim;

        public static PvpBot Spawn(Transform parent, int diff, int index)
        {
            var go = new GameObject("PvpBot " + index);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PvpBot>();
            var l = Look.Default();
            string[] colors = { "color_3", "color_2", "color_1", "color_5" };
            l["color"] = colors[index % 4]; l["neck"] = "neck_5"; l["hands"] = "hands_4"; l["shoes"] = "shoes_2"; l["back"] = "back_2"; l["head"] = "head_5";
            b.model = ChubrikModel.Create("Model", go.transform, l, Layers.World);
            b.anim = b.model.gameObject.AddComponent<ChubrikAnim>(); b.anim.model = b.model;
            var t = Node("BotSword", b.model.HandR, Vector3.zero, new Vector3(90, 0, 0));
            PvpArena.SwordModel(t, 1.1f);
            var tag = Prim.Text(go.transform, "БОТ " + (index + 1), new Vector3(0, 2.2f, 0), 2.2f, Hex("FF8080")); tag.gameObject.AddComponent<Billboard>();
            return b;
        }

        /// <summary>The friend over the network as a «bot»: follows his avatar, our hits go to him.</summary>
        public bool Remote { get; private set; }
        string peerId;
        public static PvpBot SpawnRemote(Transform parent, string id)
        {
            var go = new GameObject("PvpFriend");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PvpBot>();
            b.Remote = true; b.peerId = id;
            return b;
        }

        /// <summary>The friend says he fell: our point.</summary>
        public void RemoteDown() { Dead = true; PvpArena.BotDown(this); }

        public void ResetAt(Vector3 p, int d)
        {
            if (Remote) { Hp = PvpArena.MaxHp; Dead = false; transform.position = p; return; }
            diff = d; Hp = PvpArena.MaxHp; Dead = false; apples = d == 2 ? 2 : d == 1 ? 1 : 0; vel = Vector3.zero; eatUntil = -1; respawnAt = -1;
            freezeUntil = slowUntil = burnUntil = markedUntil = 0;
            transform.position = p; transform.rotation = Quaternion.Euler(0, 180, 0);
            model.gameObject.SetActive(true);
        }

        public void RespawnLater(Vector3 p) { respawnAt = Time.time + 3f; respawnPos = p; }
        public void Pull(Vector3 to) { var d = to - transform.position; d.y = 0; vel = d * 3f + Vector3.up * 4f; }
        public void Launch(Vector3 v) { vel += v; }

        /// <summary>A hit: <paramref name="raw"/> = from an ability (no hurt pause, no knockback when dir is zero).</summary>
        public void Hit(float dmg, Vector3 dir, bool crit, bool raw)
        {
            if (Dead || (!raw && hurtT > 0)) return;
            if (!raw) hurtT = 0.45f;
            if (Remote)
            {
                PvpArena.HitFriend(dmg * 0.75f, dir);
                AudioFx.Play("punch", crit ? 1f : 0.7f);
                Effects.Emit(crit ? "star" : "dust", transform.position + Vector3.up * 1.1f, crit ? 10 : 5, 0.35f);
                return;
            }   // like the 1.8 «hurt ticks»: spam still combos, but not every click counts
            Hp -= dmg * 0.75f;
            if (dir != Vector3.zero) vel = dir * 7f + Vector3.up * 5.5f;
            AudioFx.Play("punch", crit ? 1f : 0.7f);
            Effects.Emit(crit ? "star" : "dust", transform.position + Vector3.up * 1.1f, crit ? 10 : 5, 0.35f);
            if (Hp <= 0)
            {
                Dead = true;
                Effects.Emit("smoke", transform.position + Vector3.up, 20, 0.8f);
                model.gameObject.SetActive(false);
                PvpArena.BotDown(this);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Remote)
            {
                hurtT -= dt;
                var on = Online.I;
                if (on != null && on.Peers.TryGetValue(peerId, out var peer) && peer.avatar != null) { transform.position = peer.avatar.transform.position; transform.rotation = peer.avatar.transform.rotation; }
                return;
            }
            if (Dead)
            {
                if (respawnAt > 0 && Time.time > respawnAt) { ResetAt(respawnPos, diff); Effects.Emit("magic", transform.position + Vector3.up, 20, 0.6f); }
                return;
            }
            hurtT -= dt; hitCd -= dt; bowCd -= dt;
            var pl = PlayerCtl.I;
            var O = PvpArena.O;
            vel.y -= 25 * dt;
            var p = transform.position + vel * dt;
            if (p.y <= O.y) { p.y = O.y; vel.y = 0; vel.x *= 1 - 6 * dt; vel.z *= 1 - 6 * dt; }
            p.x = Mathf.Clamp(p.x, O.x - PvpArena.Half + 0.6f, O.x + PvpArena.Half - 0.6f);
            p.z = Mathf.Clamp(p.z, O.z - PvpArena.Half + 0.6f, O.z + PvpArena.Half - 0.6f);
            transform.position = p;
            anim.grounded = p.y <= O.y + 0.01f; anim.vy = vel.y;
            if (burnUntil > Time.time && (burnT -= dt) <= 0) { burnT = 0.7f; Hit(0.6f, Vector3.zero, false, true); Effects.Emit("fire", transform.position + Vector3.up, 4, 0.3f); if (Dead) return; }
            if (pl == null || !PvpArena.Fighting || Time.time < freezeUntil) { anim.speed01 = 0; return; }
            if (Time.time < PvpKits.invisUntil) { anim.speed01 = 0.3f; transform.Rotate(0, 60 * dt, 0); return; }   // the ninja is invisible: the bot looks around
            var to = pl.transform.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            if (dist > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 10);
            if (eatUntil > 0) { anim.speed01 = 0; if (Time.time > eatUntil) { eatUntil = -1; Hp = Mathf.Min(PvpArena.MaxHp, Hp + 6); Effects.Emit("heart", transform.position + Vector3.up * 1.5f, 8, 0.4f); } return; }
            if (Hp < 8 && apples > 0 && dist > 4) { apples--; eatUntil = Time.time + 1.6f; return; }
            float speed = (diff == 0 ? 3.2f : diff == 1 ? 4.2f : 5.2f) * (Time.time < slowUntil ? 0.4f : 1f);
            strafe += dt;
            var side = Vector3.Cross(Vector3.up, to.normalized) * Mathf.Sin(strafe * (diff + 1) * 0.9f + GetInstanceID());
            var move = dist > 2.2f ? to.normalized + side * 0.5f : side * 0.8f;
            if (anim.grounded && hurtT < 0.2f) { transform.position += move.normalized * speed * dt * Mathf.Clamp01(move.magnitude); anim.speed01 = Mathf.Clamp01(move.magnitude) * 0.9f; }
            if (dist > 8 && diff > 0 && bowCd <= 0)
            {
                bowCd = (diff == 1 ? 3.5f : 2.2f) * (PvpArena.Ffa ? 1.8f : 1f);
                var from = transform.position + Vector3.up * 1.3f + transform.forward * 0.5f;
                var aim = pl.transform.position + Vector3.up * 1f - from;
                Arrow.Shoot(from, aim.normalized * 26f + Vector3.up * aim.magnitude * 0.18f, false, this);
            }
            if (dist < 3.0f && hitCd <= 0)
            {
                hitCd = (diff == 0 ? 0.6f : diff == 1 ? 0.38f : 0.22f) * (PvpArena.Ffa ? 1.6f : 1f);
                anim.Attack();
                float acc = diff == 0 ? 0.55f : diff == 1 ? 0.72f : 0.88f;
                if (Random.value < acc) PvpArena.HurtMe(1.6f, to, this);
            }
        }
    }

    /// <summary>An arrow: flies with a little gravity, hits a bot (ours) or us (a bot's), sticks into walls.
    /// Ours: the archer's arrows hit twice as hard, «Один выстрел» makes one huge, «Рикошет» bounces into a second bot.</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 vel; bool mine; float life, dmg = 3f; PvpBot from, skip; bool bounced;
        public static void Shoot(Vector3 pos, Vector3 v, bool mine, PvpBot from = null)
        {
            var go = new GameObject("Arrow");
            go.transform.position = pos;
            Parts.Layer = Layers.World;
            B(go.transform, Vector3.zero, new Vector3(0.04f, 0.04f, 0.7f), M("8D6E63", 0.2f));
            Cone(go.transform, new Vector3(0, 0, 0.38f), new Vector3(0.08f, 0.1f, 0.08f), M("B0BEC5", 0.6f, 0.5f), new Vector3(90, 0, 0));
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var a = go.AddComponent<Arrow>(); a.vel = v; a.mine = mine; a.from = from;
            if (mine)
            {
                if (PvpKits.Has("Лучник")) a.dmg *= 2;
                if (PvpKits.oneShot > 0) { a.dmg = 14; PvpKits.oneShot = 0; Effects.Emit("gold", pos, 10, 0.3f); }
            }
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
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude, Layers.SolidMask, QueryTriggerInteraction.Ignore))
            { transform.position = hit.point; vel = Vector3.zero; return; }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(vel);
            if (mine)
            {
                foreach (var b in PvpArena.Bots)
                {
                    if (b == null || b.Dead || b == skip || !Near(b.transform.position)) continue;
                    PvpArena.Damage(b, dmg, vel.normalized, false);
                    var next = !bounced && PvpKits.Has("Рикошет") ? PvpArena.Nearest(b.transform.position, 10, b) : null;
                    if (next != null) { bounced = true; skip = b; vel = (next.transform.position + Vector3.up - transform.position).normalized * 25; dmg *= 0.7f; return; }
                    Destroy(gameObject); return;
                }
            }
            else if (PlayerCtl.I != null && Near(PlayerCtl.I.transform.position)) { PvpArena.HurtMe(2.5f, vel, from); Destroy(gameObject); }
        }

        bool Near(Vector3 who) { var d = transform.position - (who + Vector3.up * 0.8f); return new Vector2(d.x, d.z).magnitude < 0.55f && Mathf.Abs(d.y) < 0.9f; }
    }
}
