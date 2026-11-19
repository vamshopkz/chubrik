using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 18.11 football: a stadium off the side of the lobby (a green pad «ФУТБОЛ» near the start leads there).
    /// Alone — 10 levels against bots (an empty goal, then a keeper, then defenders, faster every level).
    /// With a friend (you are in a room together) — one ball for both, the one who started keeps the ball honest.
    /// Run into the ball to dribble it, press B (hit) next to it for a strong kick.
    /// </summary>
    public static class Football
    {
        public static readonly Vector3 O = new Vector3(-3000, 30, 0);
        public const float HalfL = 21f, HalfW = 13f, GoalW = 4.2f, GoalH = 2.9f, BallR = 0.38f;   // 18.11: wider goals
        public const int Levels = 10;
        public static Rigidbody Ball;
        public static Transform Root;
        public static Vector3 PadPos;
        public static int Mode;            // 0 nothing, 1 alone, 2 with a friend, 3 just kicking the ball about
        public static int Level, Me, Them;
        public static float TimeLeft;
        public static bool Running, Host = true;
        public static string Friend;
        public static readonly List<FootBot> Bots = new List<FootBot>();
        /// <summary>18.11 difficulty: 0 super easy, 1 easy, 2 medium, 3 hard.</summary>
        public static readonly string[] DiffNames = { "СУПЕРЛЁГКАЯ", "ЛЁГКАЯ", "СРЕДНЯЯ", "СЛОЖНАЯ" };
        public static int Diff => Mathf.Clamp(Save.P.footDiff, 0, 3);
        public static float BotSpeedK => Diff switch { 0 => 0.4f, 1 => 0.6f, 2 => 0.82f, _ => 1.05f };
        public static float BotKickK => Diff switch { 0 => 0.55f, 1 => 0.7f, 2 => 0.88f, _ => 1f };
        static int MaxDefenders => Diff switch { 0 => 1, 1 => 2, 2 => 3, _ => 4 };
        public static int Goals;           // autotest: goals counted
        public static Vector3 NetPos => netPos;
        static float startedAt;
        static TextMeshProUGUI board;
        static Runner runner;
        static Vector3 netPos, netVel; static float netT;

        public static bool Inside(Vector3 p) => Root != null && Mathf.Abs(p.x - O.x) < 70 && Mathf.Abs(p.z - O.z) < 60 && Mathf.Abs(p.y - O.y) < 40;

        // ---------------- the pad in the lobby ----------------

        /// <summary>A green pad with a big football close to the start of the lobby.</summary>
        public static void BuildPad(Transform lobby)
        {
            Physics.SyncTransforms();
            // 18.11: next to the portal of World 67 (in the park), not by the water
            var p67 = Lobby.Portal67;
            PadPos = p67 + new Vector3(6, 0, 3);
            for (int ring = 0; ring < 5; ring++)
            {
                bool found = false;
                for (int k = 0; k < 16 && !found; k++)
                {
                    float a = k * Mathf.PI * 2 / 16 + ring * 0.2f, r = 5f + ring * 1.5f;
                    var c = new Vector3(p67.x + Mathf.Cos(a) * r, 0, p67.z + Mathf.Sin(a) * r);
                    if (!Physics.Raycast(c + Vector3.up * 5, Vector3.down, out var hit, 8, Layers.SolidMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y) > 0.2f) continue;
                    if (Physics.CheckBox(c + Vector3.up * 1.5f, new Vector3(1.8f, 1.3f, 1.8f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                    bool near = false;
                    foreach (var z in Zones.All) if ((z.pos - c).magnitude < 5) near = true;
                    foreach (var t in Lobby.TokenSpots) if (new Vector2(t.x - c.x, t.z - c.z).magnitude < 3) near = true;
                    if (near) continue;
                    PadPos = hit.point; found = true;
                }
                if (found) break;
            }
            Parts.Layer = Layers.World;
            var pt = Node("FootballPad", lobby, PadPos);
            Cy(pt, new Vector3(0, 0.05f, 0), new Vector3(3.2f, 0.05f, 3.2f), M("2E9E3E", 0.3f));
            Ring(pt, new Vector3(0, 0.11f, 0), 1.55f, 0.07f, M("FFFFFF", 0.4f));
            var ball = Node("BigBall", pt, new Vector3(0, 1.6f, 0));
            BallModel(ball, 1.1f);
            ball.gameObject.AddComponent<Spin>().speed = 40;
            var lbl = Prim.Text(pt, "ФУТБОЛ", new Vector3(0, 3.0f, 0), 4.5f, Hex("FFFFFF"));
            lbl.gameObject.AddComponent<Billboard>();
            Zones.Add(PadPos, 1.6f, "ФУТБОЛ", Menu, () => Worlds.Current == 1 && !Inside(PlayerCtl.I.transform.position), Zones.Kind.Step);
        }

        static void BallModel(Transform t, float d)
        {
            S(t, Vector3.zero, d, M("FAFAFA", 0.5f));
            for (int k = 0; k < 12; k++)
            {
                float th = 30 + (k / 4) * 60, ph = (k % 4) * 90 + (k / 4) * 45;
                var q = Dt.Ell(Vector3.zero, Vector3.one * d / 2, th, ph, 1.0f);
                Msh(Meshes.Ball(6, 4), t, q, new Vector3(d * 0.3f, d * 0.3f, d * 0.12f), M("1A1A1A", 0.5f)).transform.localRotation = Quaternion.LookRotation(q);
            }
        }

        // ---------------- the stadium ----------------

        public static void EnsureBuilt() { Build(); FootballFans.Refresh(); }

        static void Build()
        {
            if (Root != null) return;
            Parts.Layer = Layers.World;
            Root = new GameObject("Stadium").transform;
            Root.SetParent(Lobby.root, true);
            Root.position = O;
            // the pitch with mown stripes
            var grassM = Mats.Tex(Patterns.Stripes("3FA34D", "4CB85A", 12, true), Color.white, 0.15f);
            var pitch = Lobby.Solid(PrimitiveType.Cube, new Vector3(0, -0.5f, 0), new Vector3(HalfL * 2 + 12, 1, HalfW * 2 + 12), grassM, default, Root);
            pitch.name = "Pitch";
            var white = M("FFFFFF", 0.4f);
            void Line(Vector3 c, Vector3 s) => B(Root, c + Vector3.up * 0.01f, new Vector3(s.x, 0.02f, s.z), white);
            Line(Vector3.zero, new Vector3(0.15f, 0, HalfW * 2));
            Line(new Vector3(0, 0, HalfW), new Vector3(HalfL * 2, 0, 0.15f)); Line(new Vector3(0, 0, -HalfW), new Vector3(HalfL * 2, 0, 0.15f));
            Line(new Vector3(HalfL, 0, 0), new Vector3(0.15f, 0, HalfW * 2)); Line(new Vector3(-HalfL, 0, 0), new Vector3(0.15f, 0, HalfW * 2));
            Ring(Root, new Vector3(0, 0.02f, 0), 3.5f, 0.06f, white);
            Cy(Root, new Vector3(0, 0.02f, 0), new Vector3(0.3f, 0.01f, 0.3f), white);
            foreach (var sd in new[] { -1f, 1f })
            {
                // penalty boxes
                Line(new Vector3(sd * (HalfL - 4.5f), 0, 0), new Vector3(0.15f, 0, 14));
                Line(new Vector3(sd * (HalfL - 2.25f), 0, 7), new Vector3(4.5f, 0, 0.15f)); Line(new Vector3(sd * (HalfL - 2.25f), 0, -7), new Vector3(4.5f, 0, 0.15f));
                Goal(sd);
            }
            // boards all round (the ball bounces back), invisible higher walls, stands with a crowd, lights
            var boardM = M("1E5BD8", 0.3f);
            void Board(Vector3 c, Vector3 s)
            {
                Lobby.Solid(PrimitiveType.Cube, c + Vector3.up * 0.6f, s + Vector3.up * 1.2f, boardM, default, Root);
                var inv = Prim.Make(PrimitiveType.Cube, Root, c + Vector3.up * 8, s + Vector3.up * 16, null, "Barrier", Layers.Barrier);
                inv.AddComponent<BoxCollider>(); inv.GetComponent<Renderer>().enabled = false;   // nobody (and no ball) flies out
            }
            float bx = HalfL + 3.2f, bz = HalfW + 1.2f;
            Board(new Vector3(0, 0, bz), new Vector3(bx * 2, 0, 0.3f)); Board(new Vector3(0, 0, -bz), new Vector3(bx * 2, 0, 0.3f));
            foreach (var sd in new[] { -1f, 1f })
            {
                Board(new Vector3(sd * bx, 0, (GoalW + 2 + bz) / 2), new Vector3(0.3f, 0, bz - GoalW - 2));
                Board(new Vector3(sd * bx, 0, -(GoalW + 2 + bz) / 2), new Vector3(0.3f, 0, bz - GoalW - 2));
                Board(new Vector3(sd * (HalfL + 2.6f), 0, 0), new Vector3(0.3f, 0, (GoalW + 2) * 2));   // behind the goal
            }
            // the stands (the fans are the friends you rescued — FootballFans)
            foreach (var sd in new[] { -1f, 1f })
                for (int row = 0; row < 4; row++)
                    B(Root, new Vector3(0, 0.5f + row * 0.9f, sd * (bz + 2 + row * 1.6f)), new Vector3(bx * 2, 1 + row * 1.8f, 1.6f), M("9AA3AE", 0.2f));
            FootballFans.Build(Root, bx, bz);
            foreach (var p in new[] { new Vector3(-bx - 3, 0, -bz - 3), new Vector3(bx + 3, 0, -bz - 3), new Vector3(-bx - 3, 0, bz + 3), new Vector3(bx + 3, 0, bz + 3) })
            {
                Rod(Root, p, p + Vector3.up * 14, 0.25f, M("8C96A3", 0.6f, 0.5f));
                B(Root, p + Vector3.up * 14.5f, new Vector3(2.5f, 1.4f, 0.4f), G("FFF9C4", 2.5f));
            }
            var sign = Prim.Text(Root, "СТАДИОН ЧУБРИКОВ", new Vector3(0, 9.5f, bz + 9), 9, Hex("FFC83A"));
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            // the exit
            var exitP = new Vector3(0, 0, -bz + 2.2f);
            Cy(Root, exitP + Vector3.up * 0.04f, new Vector3(2.2f, 0.04f, 2.2f), G("35E0FF", 0.8f));
            var ex = Prim.Text(Root, "В ЛОББИ", exitP + Vector3.up * 2.6f, 3.5f, Hex("35E0FF"));
            ex.gameObject.AddComponent<Billboard>();
            Zones.Add(O + exitP, 1.3f, "В ЛОББИ", Leave, () => Worlds.Current == 1 && Inside(PlayerCtl.I.transform.position), Zones.Kind.Step);
            // the ball
            var bg = new GameObject("Football") { layer = Layers.World };
            bg.transform.SetParent(Root, false);
            BallModel(bg.transform, BallR * 2);
            var sc = bg.AddComponent<SphereCollider>(); sc.radius = BallR;
            sc.sharedMaterial = new PhysicsMaterial("Ball") { bounciness = 0.55f, dynamicFriction = 0.4f, staticFriction = 0.4f, bounceCombine = PhysicsMaterialCombine.Maximum };
            Ball = bg.AddComponent<Rigidbody>();
            Ball.mass = 0.45f; Ball.linearDamping = 0.35f; Ball.angularDamping = 0.8f;
            Ball.interpolation = RigidbodyInterpolation.Interpolate; Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ResetBall();
            runner = Root.gameObject.AddComponent<Runner>();
        }

        static void Goal(float sd)
        {
            var post = M("FFFFFF", 0.6f, 0.3f);
            var net = Parts.Glass("FFFFFF", 0.25f);
            float x = sd * (HalfL + 0.1f), back = sd * (HalfL + 2.1f);
            foreach (var z in new[] { -GoalW, GoalW }) Lobby.Solid(PrimitiveType.Cylinder, new Vector3(x, GoalH / 2, z), new Vector3(0.24f, GoalH / 2, 0.24f), post, default, Root);
            Lobby.Solid(PrimitiveType.Cube, new Vector3(x, GoalH, 0), new Vector3(0.24f, 0.24f, GoalW * 2 + 0.24f), post, default, Root);
            // the net: back, sides and roof (solid for the ball)
            Lobby.Solid(PrimitiveType.Cube, new Vector3(back, GoalH / 2, 0), new Vector3(0.06f, GoalH, GoalW * 2), net, default, Root);
            foreach (var z in new[] { -GoalW, GoalW }) Lobby.Solid(PrimitiveType.Cube, new Vector3((x + back) / 2, GoalH / 2, z), new Vector3(2f, GoalH, 0.06f), net, default, Root);
            Lobby.Solid(PrimitiveType.Cube, new Vector3((x + back) / 2, GoalH + 0.02f, 0), new Vector3(2f, 0.06f, GoalW * 2), net, default, Root);
            for (int k = 0; k < 9; k++) B(Root, new Vector3(back - sd * 0.04f, GoalH / 2, -GoalW + k * GoalW / 4), new Vector3(0.03f, GoalH, 0.03f), post);
            for (int k = 0; k < 5; k++) B(Root, new Vector3(back - sd * 0.04f, 0.3f + k * 0.55f, 0), new Vector3(0.03f, 0.03f, GoalW * 2), post);
        }

        public static void ResetBall(float side = 0)
        {
            if (Ball == null) return;
            Ball.isKinematic = false;
            Ball.position = O + new Vector3(side, 1.2f, 0);
            Ball.transform.position = Ball.position;
            Ball.linearVelocity = Vector3.zero; Ball.angularVelocity = Vector3.zero;
        }

        // ---------------- menu, start, leave ----------------

        public static void Menu()
        {
            Menus.Unlock();
            Window.Show("ФУТБОЛ", w =>
            {
                w.Text("Беги в мяч — ведёшь его. Нажми B (удар) рядом с мячом — сильный удар. Забивай в ворота справа!", 24, UIKit.Muted);
                var chips = new List<(string, Color, bool, System.Action)>();
                for (int d = 0; d < 4; d++) { int dd = d; chips.Add((DiffNames[d], d == 0 ? UIKit.Lime : d == 1 ? UIKit.Green : d == 2 ? UIKit.Orange : UIKit.Red, Diff == d, () => { Save.P.footDiff = dd; Save.Write(); Window.Refresh(); })); }
                w.Chips("Сложность (боты, вратарь, время)", chips);
                var on = Online.I;
                bool friend = on != null && on.RoomPeer != null;
                w.Row("Играть с другом", friend ? "Один мяч на двоих, 3 минуты. Друг тоже окажется на стадионе" : "Сначала пригласи друга в «Игроки онлайн» — и вы будете в одной комнате",
                    "С ДРУГОМ", UIKit.Violet, () => StartFriend(), friend, UIKit.Violet);
                w.Row("Просто погонять мяч", "Без счёта и без ботов", "ВПЕРЁД", UIKit.Blue, () => Start(3, 0), true, UIKit.Blue);
                int playing = FootballFans.Players().Count;
                w.Row("Следить за кем-то", playing > 0 ? $"Сейчас играют: {playing} — смотри их матч с трибуны" : "Посмотреть, как играют другие", "СЛЕДИТЬ", UIKit.Violet, FootballFans.PickTarget, true, UIKit.Violet);
                w.Row("Сесть на трибуну", $"Болельщики — твои спасённые друзья ({Mathf.Min(Save.P.rescued.Count, FootballFans.MaxFans)}), есть свободные места", "НА ТРИБУНУ", UIKit.Cyan, FootballFans.SitDown, true, UIKit.Cyan);
                for (int l = 1; l <= Levels; l++)
                {
                    int lv = l;
                    bool open = l <= Save.P.footLevel;
                    bool done = l < Save.P.footLevel;
                    w.Row($"Уровень {l}: {About(l)}" + (done ? " ✓" : ""), open ? $"Победа: +{Reward(l)} монет" : "Сначала пройди уровень " + (l - 1),
                        done ? "ЕЩЁ РАЗ" : "ИГРАТЬ", l <= 3 ? UIKit.Green : l <= 6 ? UIKit.Orange : UIKit.Red, () => Start(1, lv), open);
                }
                w.Footer.text = $"Пройдено уровней: {Save.P.footLevel - 1} из {Levels}";
            }, 1150, 820);
        }

        public static string About(int l) => l == 1 ? "пустые ворота — забей 3 гола" : l == 2 ? "против вратаря" : $"вратарь и {Mathf.Min(l - 2, 4)} {(l - 2 == 1 ? "защитник" : "защитника")}" + (l >= 7 ? ", очень быстрые" : "");
        public static int Reward(int l) => 100 + l * 60;

        public static void Start(int mode, int level)
        {
            Build();
            FootballFans.Refresh();
            if (FootballFans.Watching != null) FootballFans.StopWatching();
            Window.Close();
            ClearBots();
            Mode = mode; Level = level; Me = Them = 0; Host = true; Friend = null; startedAt = Time.time;
            TimeLeft = mode == 1 ? (level == 1 ? 90 : 120) + (3 - Diff) * 30 : mode == 2 ? 180 : 0;
            Running = mode != 3;
            if (mode == 1 && level >= 2)
            {
                Bots.Add(FootBot.Spawn(true, 0, level));
                for (int i = 0; i < Mathf.Min(level - 2, MaxDefenders); i++) Bots.Add(FootBot.Spawn(false, i, level));
            }
            ResetBall();
            Board(true);
            var pl = PlayerCtl.I;
            pl.PlayVictory(() =>
            {
                pl.Teleport(O + new Vector3(-6, 0.3f, 0), 90);
                pl.WaterY = O.y - 10;
                pl.SafePos = pl.Checkpoint = pl.transform.position;
                pl.yaw = 90;
            });
            Menus.LockCursor();
            Hud.Toast(mode == 3 ? "Стадион! Гоняй мяч сколько хочешь" : mode == 2 ? "Матч с другом! Твои ворота — слева, бей вправо" : $"Уровень {level}: {About(level)}. Забивай в ворота справа!", UIKit.Lime);
        }

        public static void StartFriend()
        {
            var on = Online.I;
            if (on == null || on.RoomPeer == null) return;
            Start(2, 0);
            Friend = on.RoomWith; Host = true;
            on.Send($"FS|{on.Id}|{Friend}");
        }

        public static void Leave()
        {
            if (Mode == 2 && Online.I != null && Friend != null) Online.I.Send($"FE|{Online.I.Id}|{Friend}|{Me}|{Them}|0");
            Stop();
            var pl = PlayerCtl.I;
            pl.PlayVictory(() =>
            {
                pl.Teleport(PadPos + new Vector3(0, 0.3f, -2.5f), 180);
                pl.WaterY = -0.9f;
                pl.SafePos = pl.Checkpoint = Lobby.Spawn;
            });
        }

        static void Stop()
        {
            Running = false; Mode = 0;
            ClearBots();
            Board(false);
            if (Ball != null) { Ball.isKinematic = false; ResetBall(); }
        }

        static void ClearBots() { foreach (var b in Bots) if (b) Object.Destroy(b.gameObject); Bots.Clear(); }

        static void Board(bool on)
        {
            if (!on) { if (board) Object.Destroy(board.gameObject); board = null; return; }
            if (board) return;
            board = UIKit.Txt(UIRoot.HudLayer, "", 40, Color.white);
            UIKit.Place(board.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78 - UIRoot.Safe().w), new Vector2(900, 60));
        }

        static void UpdateBoard()
        {
            if (!board) return;
            if (Mode == 3) { board.text = "СТАДИОН · просто погонять мяч"; return; }
            string them = Mode == 2 ? "ДРУГ" : Level == 1 ? "ЦЕЛЬ 3" : "БОТЫ";
            int t = Mathf.CeilToInt(Mathf.Max(0, TimeLeft));
            board.text = Mode == 1 && Level == 1 ? $"ГОЛЫ: {Me} из 3   ·   {t / 60}:{t % 60:00}" : $"ТЫ {Me} : {Them} {them}   ·   {t / 60}:{t % 60:00}";
        }

        // ---------------- touching and kicking the ball ----------------

        /// <summary>The player ran into the ball: it rolls on ahead (dribbling).</summary>
        public static void Touch(Vector3 moveDir, float speed)
        {
            if (Ball == null) return;
            var d = moveDir; d.y = 0;
            if (d.sqrMagnitude < 0.01f) return;
            var v = d.normalized * Mathf.Max(speed * 1.25f, 3f) + Vector3.up * 0.6f;
            Push(v);
        }

        /// <summary>B next to the ball: a strong kick where the chubrik looks.</summary>
        public static bool TryKick(PlayerCtl pl)
        {
            if (Ball == null || !Inside(pl.transform.position)) return false;
            var d = Ball.position - pl.transform.position;
            if (new Vector2(d.x, d.z).magnitude > 1.9f || Mathf.Abs(d.y) > 1.8f) return false;
            var f = pl.transform.forward; f.y = 0; f.Normalize();
            Push(f * 16f + Vector3.up * 4.5f);
            AudioFx.Play("punch"); AudioFx.Play("boing", 0.4f);
            Effects.Emit("dust", Ball.position, 8, 0.3f);
            return true;
        }

        static float sendCd;
        static void Push(Vector3 v)
        {
            if (Mode == 2 && !Host)
            {
                // the friend keeps the ball: tell them, and move ours at once so it feels instant
                netVel = v; netPos = Ball.position; netT = 0.25f;
                if (Time.unscaledTime > sendCd) { sendCd = Time.unscaledTime + 0.1f; Online.I?.Send(string.Format(CultureInfo.InvariantCulture, "FK|{0}|{1:0.00}|{2:0.00}|{3:0.00}", Online.I.Id, v.x, v.y, v.z)); }
                return;
            }
            Ball.linearVelocity = v;
        }

        public static void BotKick(Vector3 v) { if (Ball != null) Ball.linearVelocity = v; }

        // ---------------- the friend over the network ----------------

        public static void OnNet(string[] f, string from)
        {
            var on = Online.I;
            if (on == null || from != on.RoomWith) return;
            switch (f[0])
            {
                case "FS":
                    if (f.Length >= 3 && f[2] == on.Id)
                    {
                        Start(2, 0);
                        Host = false; Friend = from;
                        if (Ball) Ball.isKinematic = true;
                        Hud.Toast("Друг позвал тебя на футбол! Твои ворота — справа, бей влево", UIKit.Lime);
                        PlayerCtl.I.yaw = 270;
                    }
                    break;
                case "FB":
                    if (Mode == 2 && !Host && f.Length >= 11 && Ball)
                    {
                        var I = CultureInfo.InvariantCulture;
                        netPos = O + new Vector3(float.Parse(f[2], I), float.Parse(f[3], I), float.Parse(f[4], I));
                        var v = new Vector3(float.Parse(f[5], I), float.Parse(f[6], I), float.Parse(f[7], I));
                        netVel = v; netT = 0;   // the host's ball is the truth
                        int.TryParse(f[8], out var hostGoals); int.TryParse(f[9], out var guestGoals);
                        Me = guestGoals; Them = hostGoals;
                        TimeLeft = float.Parse(f[10], I);
                    }
                    break;
                case "FK":
                    if (Mode == 2 && Host && f.Length >= 5 && Ball)
                    {
                        var I = CultureInfo.InvariantCulture;
                        Ball.linearVelocity = new Vector3(float.Parse(f[2], I), float.Parse(f[3], I), float.Parse(f[4], I));
                    }
                    break;
                case "FE":
                    if (Mode == 2) { int.TryParse(f.Length > 3 ? f[3] : "0", out var a); int.TryParse(f.Length > 4 ? f[4] : "0", out var b); if (Host) Finish(); else { Me = b; Them = a; Finish(); } }
                    break;
            }
        }

        // ---------------- the match ----------------

        static void Scored(bool forMe)
        {
            Goals++;
            if (forMe) Me++; else Them++;
            AudioFx.Play(forMe ? "victory" : "error");
            Effects.Emit("confetti", Ball.position + Vector3.up, 80, 1f, Vector3.up * 5);
            Hud.Toast(forMe ? "ГОООЛ!" : (Mode == 2 ? "Гол другу!" : "Гол в твои ворота!"), forMe ? UIKit.Gold : UIKit.Orange);
            FootballFans.Cheer();
            ResetBall(forMe ? 3 : -3);
            if (Mode == 1 && Level == 1 && Me >= 3) Finish();
        }

        static void Finish()
        {
            if (!Running) return;
            Running = false;
            bool win = Mode == 1 && Level == 1 ? Me >= 3 : Me > Them;
            bool draw = Me == Them && !(Mode == 1 && Level == 1);
            if (Mode == 2 && Host && Online.I != null && Friend != null) Online.I.Send($"FE|{Online.I.Id}|{Friend}|{Me}|{Them}|1");
            int reward = 0;
            if (Mode == 1 && win)
            {
                reward = Reward(Level);
                if (Level == Save.P.footLevel && Save.P.footLevel < Levels + 1) Save.P.footLevel++;
                Save.AddCoins(reward);
            }
            Save.Write();
            int lv = Level, mode = Mode;
            ClearBots();
            Menus.Unlock();
            Window.Show(win ? "ПОБЕДА!" : draw ? "НИЧЬЯ" : "ПОРАЖЕНИЕ", w =>
            {
                w.Text(mode == 1 && lv == 1 ? $"Голы: {Me} из 3" : $"Счёт: ты {Me} — {Them} {(mode == 2 ? "друг" : "боты")}", 40, win ? UIKit.Gold : Color.white);
                if (reward > 0) w.Text($"+{reward} монет! Открыт уровень {Mathf.Min(Save.P.footLevel, Levels)}", 30, UIKit.Lime);
                if (mode == 1 && win && lv < Levels) w.Row($"Уровень {lv + 1}", About(lv + 1), "ДАЛЬШЕ", UIKit.Green, () => Start(1, lv + 1));
                if (mode == 1) w.Row("Сыграть ещё раз", null, "ЕЩЁ", UIKit.Orange, () => Start(1, lv));
                if (mode == 2) w.Row("Ещё матч с другом", null, "ЕЩЁ", UIKit.Violet, () => StartFriend(), Online.I != null && Online.I.RoomPeer != null);
                w.Row("Все уровни", null, "МЕНЮ", UIKit.Blue, Menu);
                w.Row("Вернуться в лобби", null, "В ЛОББИ", UIKit.Cyan, () => { Window.Close(); Leave(); });
            }, 1000, 640);
            if (win) PlayerCtl.I?.PlayVictory();
        }

        /// <summary>Tests: end the match now.</summary>
        public static void TestFinish() => Finish();

        class Runner : MonoBehaviour
        {
            float sendT;
            void Update()
            {
                if (Ball == null) return;
                var pl = PlayerCtl.I;
                bool here = pl != null && Inside(pl.transform.position);
                if (!here && Mode != 0 && Time.time - startedAt > 4f && !Menus.AnyOpen) Stop();   // left the stadium some other way
                if (Mode == 2 && !Host)
                {
                    // the friend's ball: glide to where they say it is
                    Ball.isKinematic = true;
                    netT -= Time.deltaTime;
                    netPos += netVel * Time.deltaTime;
                    netVel += Physics.gravity * Time.deltaTime * (netPos.y > O.y + BallR + 0.02f ? 1 : 0);
                    if (netPos.y < O.y + BallR) { netPos.y = O.y + BallR; netVel.y = Mathf.Abs(netVel.y) * 0.4f; }
                    var np = Vector3.Lerp(Ball.position, netPos, 1 - Mathf.Exp(-10 * Time.deltaTime));
                    Ball.position = np; Ball.transform.position = np;
                }
                else
                {
                    var lp = Ball.position - O;
                    if (Running)
                    {
                        if (lp.x > HalfL + BallR + 0.1f && Mathf.Abs(lp.z) < GoalW && lp.y < GoalH) Scored(true);
                        else if (lp.x < -HalfL - BallR - 0.1f && Mathf.Abs(lp.z) < GoalW && lp.y < GoalH) Scored(false);
                    }
                    if (lp.y < -6 || Mathf.Abs(lp.x) > HalfL + 6 || Mathf.Abs(lp.z) > HalfW + 4) ResetBall();
                    if (Mode == 2 && Host && Online.I != null && (sendT -= Time.deltaTime) <= 0)
                    {
                        sendT = 0.1f;
                        var v = Ball.linearVelocity;
                        Online.I.Send(string.Format(CultureInfo.InvariantCulture, "FB|{0}|{1:0.00}|{2:0.00}|{3:0.00}|{4:0.00}|{5:0.00}|{6:0.00}|{7}|{8}|{9:0.0}", Online.I.Id, lp.x, lp.y, lp.z, v.x, v.y, v.z, Me, Them, TimeLeft));
                    }
                }
                if (Running && (Mode == 1 || (Mode == 2 && Host)))
                {
                    TimeLeft -= Time.deltaTime;
                    if (TimeLeft <= 0) Finish();
                }
                UpdateBoard();
            }
        }
    }

    /// <summary>A football bot: the keeper guards the right-hand goal, field players chase the ball and shoot left.</summary>
    public class FootBot : MonoBehaviour
    {
        public bool keeper;
        public int index, level;
        ChubrikAnim anim;
        float kickCd;
        Vector3 Home => Football.O + (keeper ? new Vector3(Football.HalfL - 1.2f, 0, 0) : new Vector3(6 + index * 3, 0, (index - 1.5f) * 4));
        float Speed => ((keeper ? 3.0f : 2.8f) + level * 0.22f) * Football.BotSpeedK;   // 18.11: easier, and by difficulty

        public static FootBot Spawn(bool keeper, int index, int level)
        {
            var go = new GameObject(keeper ? "Keeper" : "FootBot" + index);
            go.transform.SetParent(Football.Root, true);
            var b = go.AddComponent<FootBot>();
            b.keeper = keeper; b.index = index; b.level = level;
            go.transform.position = b.Home;
            go.transform.rotation = Quaternion.Euler(0, 270, 0);
            var look = Look.Default();
            look["suit"] = keeper ? "suit_7" : "suit_4";
            look["color"] = "color_" + (index % 8);
            var m = ChubrikModel.Create("Model", go.transform, look, Layers.World);
            b.anim = m.gameObject.AddComponent<ChubrikAnim>();
            b.anim.model = m;
            var tag = Prim.Text(go.transform, keeper ? "ВРАТАРЬ" : "БОТ", new Vector3(0, 2.1f, 0), 2.2f, Color.white);
            tag.gameObject.AddComponent<Billboard>();
            return b;
        }

        void Update()
        {
            var ball = Football.Ball;
            if (ball == null || !Football.Running) { if (anim) anim.speed01 = 0; return; }
            float dt = Time.deltaTime;
            kickCd -= dt;
            var bp = ball.position;
            Vector3 target;
            var O = Football.O;
            if (keeper)
            {
                // stays on the line, follows the ball sideways; comes out a little when the ball is close
                float z = Mathf.Clamp(bp.z - O.z, -Football.GoalW + 0.5f, Football.GoalW - 0.5f);
                float x = Football.HalfL - 1.0f - (bp.x - O.x > Football.HalfL - 7 ? 1.4f : 0);
                target = O + new Vector3(x, 0, z);
            }
            else
            {
                // the bot nearest to the ball chases it (getting behind it, towards the left goal); the others wait in their spots
                bool chaser = true;
                foreach (var o in Football.Bots) if (o != this && !o.keeper && (o.transform.position - bp).sqrMagnitude < (transform.position - bp).sqrMagnitude) chaser = false;
                var goal = O + new Vector3(-Football.HalfL - 1, 0, 0);
                var behind = bp + (bp - goal).normalized * 0.7f;
                target = chaser ? new Vector3(behind.x, O.y, behind.z) : Vector3.Lerp(Home, new Vector3(bp.x + 5, O.y, bp.z), 0.45f);
            }
            var to = target - transform.position; to.y = 0;
            float sp = Mathf.Min(Speed, to.magnitude / Mathf.Max(dt, 0.001f));
            if (to.magnitude > 0.05f)
            {
                transform.position += to.normalized * sp * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 8);
            }
            var p = transform.position; p.y = O.y; transform.position = p;
            if (anim) { anim.speed01 = Mathf.Clamp01(sp / 6.8f); anim.grounded = true; }
            // kick when the ball is at the feet
            var d = bp - transform.position;
            if (kickCd <= 0 && new Vector2(d.x, d.z).magnitude < (keeper ? 0.9f + Football.Diff * 0.2f : 1.0f) && d.y < 1.6f)
            {
                kickCd = 0.9f + (3 - Football.Diff) * 0.3f;
                var aim = O + new Vector3(-Football.HalfL - 1, 0, Random.Range(-Football.GoalW + 0.8f, Football.GoalW - 0.8f));
                var dir = aim - bp; dir.y = 0; dir.Normalize();
                float power = (keeper ? 13f : 7.5f + level * 0.5f) * Football.BotKickK;
                Football.BotKick(dir * power + Vector3.up * (keeper ? 5f : 1.8f));
                if (anim) anim.Attack();
                AudioFx.Play("punch", 0.5f);
            }
        }
    }
}
