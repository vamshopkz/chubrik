using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 18.11: every world has a BIG boss — a titan of 9–10 m in its own arena (a red pad near the world's start
    /// leads there). It stomps (a shock wave runs along the ground — jump over it), throws rocks and charges.
    /// After a few stomps it gets tired and kneels: its glowing heart comes down — hit it! Beat it and the friend
    /// in the cage at the back of the arena is free. Later worlds have stronger titans.
    /// </summary>
    public static class TitanArena
    {
        public static readonly Dictionary<int, Titan> Bosses = new Dictionary<int, Titan>();
        public static readonly Dictionary<int, Vector3> Pads = new Dictionary<int, Vector3>();
        public static readonly Dictionary<int, Vector3> Centers = new Dictionary<int, Vector3>();
        public static string FriendOf(int w) => "titan" + w;   // not wN...: the 8 cages of a world are counted by that prefix
        public const float R = 24f;

        static Vector3 SpawnOf(int w) => w == 2 ? World2.Spawn : w == 3 ? World3.Spawn : w == 4 ? World4.Spawn : w == 5 ? World5.Spawn : w == 6 ? World6.Spawn : World67.Spawn;
        public static int Level(int w) => w == 2 ? 1 : w == 4 ? 2 : w == 5 ? 3 : w == 3 ? 4 : w == 6 ? 5 : 6;   // the order the worlds open in

        public static string NameOf(int w) => w switch
        {
            2 => "КАМЕННЫЙ ТИТАН", 3 => "КРИСТАЛЬНЫЙ КОЛОСС", 4 => "ЛЕДЯНОЙ ВЕЛИКАН", 5 => "ДРЕВНИЙ ЛЕСНОЙ ИСПОЛИН", 6 => "ГЛЮК-ГИГАНТ", _ => "ТИТАН-67",
        };

        /// <summary>Built together with the world: the arena far to the side, the pad next to the start.</summary>
        public static void Build(int w, Transform root)
        {
            if (Bosses.ContainsKey(w)) return;
            var spawn = SpawnOf(w);
            var c = new Vector3(spawn.x + 420f, spawn.y - 0.3f, spawn.z + 40f);
            Centers[w] = c;
            Parts.Layer = Layers.World;
            var t = new GameObject("TitanArena" + w).transform;
            t.SetParent(root, true);
            t.position = c;
            var (floorCol, rimCol, glow) = Theme(w);
            // the round arena with a rim of stone blocks, torches and an invisible wall round it
            var floor = Lobby.Solid(PrimitiveType.Cylinder, Vector3.down * 1f, new Vector3(R * 2 + 4, 1f, R * 2 + 4), Lobby.Tiled(Tex.Concrete(), 8), default, t);
            floor.GetComponent<Renderer>().sharedMaterial.color = Hex(floorCol);
            Cy(t, new Vector3(0, 0.02f, 0), new Vector3(10, 0.01f, 10), M(rimCol, 0.3f));
            Ring(t, new Vector3(0, 0.04f, 0), 5.2f, 0.12f, G(glow, 1.4f));
            for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI * 2 / 24;
                var p = new Vector3(Mathf.Cos(a) * (R + 1.2f), 0, Mathf.Sin(a) * (R + 1.2f));
                var blk = Lobby.Solid(PrimitiveType.Cube, p + Vector3.up * 2.2f, new Vector3(2.4f, 4.4f, 6.6f), M(rimCol, 0.2f), new Vector3(0, -a * Mathf.Rad2Deg, 0), t);
                blk.name = "ArenaWall";
                if (k % 3 == 0)
                {
                    Rod(t, p + Vector3.up * 4.4f, p + Vector3.up * 5.6f, 0.12f, M("5D4037"));
                    S(t, p + Vector3.up * 5.9f, 0.5f, G("FF9A1E", 2.5f)).AddComponent<Pulse>();
                }
                var bar = Prim.Make(PrimitiveType.Cube, t, p + Vector3.up * 20, new Vector3(2.4f, 32, 6.6f), null, "Barrier", Layers.Barrier);
                bar.transform.localEulerAngles = new Vector3(0, -a * Mathf.Rad2Deg, 0);
                bar.AddComponent<BoxCollider>();
                bar.GetComponent<Renderer>().enabled = false;
            }
            var title = Prim.Text(t, NameOf(w), new Vector3(0, 14, R + 1), 14, Hex(glow));
            title.transform.localRotation = Quaternion.Euler(0, 180, 0);
            // the friend's cage at the back
            Rescue.SpawnCage(FriendOf(w), c + new Vector3(0, 0.05f, R - 4), 180, t);
            // the titan
            var go = new GameObject("Titan" + w);
            go.transform.SetParent(t, false);
            go.transform.localPosition = new Vector3(0, 0, 6);
            var boss = go.AddComponent<Titan>();
            boss.Init(w, c, Level(w));
            Bosses[w] = boss;
            // the way back: a portal next to where you arrive
            var exitP = c + new Vector3(0, 0, -R + 4);
            Portal(t, exitP, "НАЗАД", "35E0FF");
            Zones.Add(exitP, 2f, "НАЗАД В МИР", () => Leave(w), () => Worlds.Current == w, Zones.Kind.Step);
            // the red pad near the world's start
            var pad = FindPadSpot(spawn);
            Pads[w] = pad;
            var pt = new GameObject("TitanPad" + w).transform;
            pt.SetParent(root, true);
            pt.position = pad;
            Cy(pt, new Vector3(0, 0.06f, 0), new Vector3(2.6f, 0.06f, 2.6f), M("B71C1C", 0.4f));
            Ring(pt, new Vector3(0, 0.14f, 0), 1.3f, 0.08f, G("FF5252", 2f)).AddComponent<Pulse>();
            var lbl = Prim.Text(pt, "АРЕНА БОССА", new Vector3(0, 2.4f, 0), 4, Hex("FF5252"));
            lbl.gameObject.AddComponent<Billboard>();
            S(pt, new Vector3(0, 1.3f, 0), 0.5f, G("FF5252", 1.8f)).AddComponent<Pulse>();
            Zones.Add(pad, 1.5f, "АРЕНА БОССА", () => Go(w), () => Worlds.Current == w, Zones.Kind.Step);
        }

        static (string floor, string rim, string glow) Theme(int w) => w switch
        {
            2 => ("9AA3AE", "6D6875", "B57CFF"),
            3 => ("4A4560", "3A3350", "FF8FC8"),
            4 => ("DDEFFF", "9CC8E8", "6FD8FF"),
            5 => ("6D8B4A", "4E6B35", "9BE35A"),
            6 => ("FF5FA2", "2B2B2B", "39FF14"),
            _ => ("2E2E3A", "1A1A24", "FFE14D"),
        };

        static void Portal(Transform t, Vector3 p, string text, string col)
        {
            Ring(t, p + Vector3.up * 1.8f, 1.5f, 0.15f, G(col, 2f)).transform.rotation = Quaternion.Euler(90, 0, 0);
            Cy(t, p + Vector3.up * 0.05f, new Vector3(2.4f, 0.05f, 2.4f), G(col, 0.8f));
            var l = Prim.Text(t, text, p + Vector3.up * 3.8f, 3.5f, Hex(col));
            l.gameObject.AddComponent<Billboard>();
        }

        /// <summary>Flat free ground close to the world's start (tries spots round it).</summary>
        static Vector3 FindPadSpot(Vector3 spawn)
        {
            Physics.SyncTransforms();
            var tries = new[] { new Vector3(3.5f, 0, 1), new Vector3(-3.5f, 0, 1), new Vector3(3, 0, 4), new Vector3(-3, 0, 4), new Vector3(0, 0, 6), new Vector3(4.5f, 0, -1), new Vector3(-4.5f, 0, -1), new Vector3(2, 0, -2), new Vector3(-2, 0, -2), new Vector3(0, 0, 9) };
            foreach (var o in tries)
            {
                var p = spawn + o;
                if (!Physics.Raycast(p + Vector3.up * 3, Vector3.down, out var hit, 6, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(hit.point.y - (spawn.y - 0.3f)) > 0.6f || hit.normal.y < 0.9f) continue;
                if (Physics.CheckBox(hit.point + Vector3.up * 1.3f, new Vector3(1.2f, 1.1f, 1.2f), Quaternion.identity, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                return hit.point;
            }
            return spawn + new Vector3(0, -0.3f, 1.2f);
        }

        public static void Go(int w)
        {
            var pl = PlayerCtl.I;
            if (pl == null || !Centers.TryGetValue(w, out var c)) return;
            pl.PlayVictory(() =>
            {
                pl.Teleport(c + new Vector3(0, 0.3f, -R + 7), 0);
                pl.SafePos = pl.transform.position;
                pl.Checkpoint = pl.transform.position;
                var b = Bosses[w];
                Hud.Toast(b.Defeated ? $"{NameOf(w)} уже побеждён — друг на свободе!" : $"{NameOf(w)}! Перепрыгивай волны, жди, пока он устанет и встанет на колено, — бей в светящееся сердце!", UIKit.Red);
                if (!b.Defeated) b.Wake();
            });
        }

        public static void Leave(int w)
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            pl.PlayVictory(() =>
            {
                var sp = SpawnOf(w);
                pl.Teleport(sp, 0);
                pl.SafePos = pl.Checkpoint = sp;
                if (Bosses.TryGetValue(w, out var b)) b.Sleep();
            });
        }

        public static bool InArena(int w, Vector3 p) => Centers.TryGetValue(w, out var c) && new Vector2(p.x - c.x, p.z - c.z).magnitude < R + 3 && Mathf.Abs(p.y - c.y) < 30;
    }

    public class Titan : MonoBehaviour, IHittable
    {
        public int world, level;
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool Defeated { get; private set; }
        public bool Tired => state == State.Tired;
        public bool Awake => state != State.Sleep && !Defeated;
        public int Stomps { get; private set; }
        public int Rocks { get; private set; }
        enum State { Sleep, Walk, Stomp, Tired, Dying }
        State state = State.Sleep;
        float stateT, rockCd, hitCd, touchCd;
        int stompsLeft;
        Vector3 center;
        Transform body, legL, legR, armL, armR, head, core;
        TMPro.TextMeshPro bar;
        float scale;
        readonly List<Wave> waves = new List<Wave>();
        readonly List<Rock> rocks = new List<Rock>();
        class Wave { public Transform ring; public Vector3 at; public float r; public bool hit; }
        class Rock { public Transform t; public Vector3 v; }
        Material coreM;

        float Speed => 2.4f + level * 0.35f;

        public void Init(int w, Vector3 c, int lvl)
        {
            world = w; center = c; level = lvl;
            MaxHp = Hp = 6 + lvl * 2;
            scale = 1.1f + lvl * 0.06f;
            Build();
            // 18.11: the titan is always there when you come into the world (beaten before or not)
        }

        void Build()
        {
            Parts.Layer = Layers.World;
            var root = Node("Body", transform, Vector3.zero);
            root.localScale = Vector3.one * scale;
            body = root;
            Material skin, dark, eye;
            switch (world)
            {
                case 2: skin = Lobby.Tiled(Tex.Concrete(), 2); skin.color = Hex("8C8796"); dark = M("4A4652", 0.2f); eye = G("B57CFF", 3f); break;
                case 3: skin = M("5E4B8B", 0.6f, 0.3f); dark = M("2B2540", 0.5f); eye = G("FF8FC8", 3f); break;
                case 4: skin = Glass("BFEAFF", 0.85f); dark = M("E8F6FF", 0.5f); eye = G("6FD8FF", 3f); break;
                case 5: skin = Mats.Tex(Tex.Bark(), Color.white, 0.1f); dark = M("33691E", 0.2f); eye = G("9BE35A", 3f); break;
                case 6: skin = Mats.Tex(World6.MissingTex(), Color.white, 0.2f); dark = M("2B2B2B", 0.4f); eye = G("39FF14", 3f); break;
                default: skin = M("1A1A24", 0.6f, 0.4f); dark = M("FFC83A", 0.9f, 0.8f); eye = G("FFE14D", 3f); break;
            }
            coreM = G("FF5252", 1.2f);
            // legs and feet
            legL = Node("LegL", root, new Vector3(-1.1f, 3.4f, 0)); legR = Node("LegR", root, new Vector3(1.1f, 3.4f, 0));
            foreach (var g in new[] { legL, legR })
            {
                Ca(g, new Vector3(0, -1.7f, 0), new Vector3(1.4f, 1.6f, 1.4f), skin);
                B(g, new Vector3(0, -3.25f, 0.35f), new Vector3(1.7f, 0.55f, 2.4f), dark);
                for (int k = 0; k < 3; k++) Cone(g, new Vector3(-0.5f + k * 0.5f, -3.3f, 1.6f), new Vector3(0.3f, 0.4f, 0.3f), dark, new Vector3(90, 0, 0));
            }
            // the big body with armour plates and the heart in the middle
            S(root, new Vector3(0, 5.6f, 0), new Vector3(4.2f, 4.2f, 3.2f), skin);
            for (int k = 0; k < 5; k++) B(root, new Vector3(-1.4f + k * 0.7f, 6.6f, 1.25f), new Vector3(0.55f, 1.1f, 0.5f), dark);
            Cy(root, new Vector3(0, 3.9f, 0), new Vector3(3.6f, 0.4f, 2.8f), dark);   // the belt
            core = Node("Heart", root, new Vector3(0, 5.3f, 1.55f));
            S(core, Vector3.zero, 1.1f, coreM).AddComponent<Pulse>();
            Ring(core, Vector3.zero, 0.62f, 0.12f, dark, new Vector3(90, 0, 0));
            var hc = core.gameObject.AddComponent<SphereCollider>(); hc.isTrigger = true; hc.radius = 1.6f;
            // shoulders, arms, fists
            armL = Node("ArmL", root, new Vector3(-2.5f, 7f, 0)); armR = Node("ArmR", root, new Vector3(2.5f, 7f, 0));
            foreach (var a in new[] { armL, armR })
            {
                S(a, Vector3.zero, new Vector3(1.8f, 1.5f, 1.8f), dark);
                Ca(a, new Vector3(0, -1.8f, 0), new Vector3(1.1f, 1.6f, 1.1f), skin);
                S(a, new Vector3(0, -3.6f, 0.2f), 1.5f, dark);
                for (int k = 0; k < 4; k++) Cone(a, new Vector3(-0.45f + k * 0.3f, -3.3f, 0.85f), new Vector3(0.2f, 0.35f, 0.2f), M("EEEEEE", 0.4f), new Vector3(90, 0, 0));
            }
            // the head: glowing eyes, an angry brow and horns / crown
            head = Node("Head", root, new Vector3(0, 8.4f, 0.2f));
            S(head, Vector3.zero, new Vector3(2.4f, 2.1f, 2.1f), skin);
            foreach (var sd in new[] { -1f, 1f })
            {
                S(head, new Vector3(sd * 0.5f, 0.15f, 0.95f), new Vector3(0.5f, 0.35f, 0.2f), eye);
                B(head, new Vector3(sd * 0.5f, 0.55f, 0.98f), new Vector3(0.7f, 0.16f, 0.2f), dark).transform.localRotation = Quaternion.Euler(0, 0, sd * -18);
                Cone(head, new Vector3(sd * 0.9f, 0.9f, 0), new Vector3(0.45f, 1.3f, 0.45f), dark, new Vector3(0, 0, sd * -25));
            }
            B(head, new Vector3(0, -0.5f, 1.0f), new Vector3(1.2f, 0.3f, 0.2f), M("1A1A1A", 0.3f));
            for (int k = 0; k < 4; k++) Cone(head, new Vector3(-0.42f + k * 0.28f, -0.4f, 1.05f), new Vector3(0.14f, 0.22f, 0.14f), M("FFFFFF", 0.3f), new Vector3(180, 0, 0));
            if (world == World67.Id) for (int k = 0; k < 5; k++) Cone(head, new Vector3(-0.8f + k * 0.4f, 1.15f, 0), new Vector3(0.3f, 0.7f, 0.3f), M("FFC83A", 0.9f, 0.8f));
            // a big «solid» capsule so you can't walk through it
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 5f * scale, 0); col.radius = 1.3f * scale; col.height = 10f * scale;
            bar = Prim.Text(transform, "", new Vector3(0, 11.5f * scale, 0), 6, Hex("FF5252"));
            bar.gameObject.AddComponent<Billboard>();
            UpdateBar();
        }

        void UpdateBar()
        {
            if (!bar) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < MaxHp; i++) sb.Append(i < Hp ? '■' : '□');
            bar.text = sb.ToString();
        }

        /// <summary>18.11: back in the world — the titan stands again, full of lives; its friend waits in a see-through cage.</summary>
        public void Respawn()
        {
            ClearAttacks();
            Defeated = false; Hp = MaxHp; state = State.Sleep; stateT = 0;
            gameObject.SetActive(true);
            transform.localPosition = new Vector3(0, 0, 6); transform.localRotation = Quaternion.identity;
            body.localPosition = Vector3.zero; body.localRotation = Quaternion.identity;
            legL.localRotation = legR.localRotation = armL.localRotation = armR.localRotation = Quaternion.identity;
            UpdateBar();
        }

        public void Wake() { if (Defeated) return; state = State.Walk; stateT = 0; stompsLeft = 3; rockCd = 3; transform.localPosition = new Vector3(0, 0, 6); }
        public void Sleep() { if (Defeated) return; state = State.Sleep; ClearAttacks(); Hp = MaxHp; UpdateBar(); }

        void ClearAttacks()
        {
            foreach (var w in waves) if (w.ring) Destroy(w.ring.gameObject);
            foreach (var r in rocks) if (r.t) Destroy(r.t.gameObject);
            waves.Clear(); rocks.Clear();
        }

        void Update()
        {
            var pl = PlayerCtl.I;
            float dt = Time.deltaTime;
            UpdateWaves(dt, pl);
            UpdateRocks(dt, pl);
            if (state == State.Sleep || pl == null) { Idle(); return; }
            if (state == State.Dying) { Dying(dt); return; }
            if (!TitanArena.InArena(world, pl.transform.position)) { Sleep(); return; }
            stateT += dt; rockCd -= dt; hitCd -= dt; touchCd -= dt;
            var to = pl.transform.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            switch (state)
            {
                case State.Walk:
                {
                    if (dist > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 2.5f);
                    float sp = dist > 9 ? Speed : Speed * 0.6f;
                    var next = transform.position + transform.forward * sp * dt;
                    var fromC = next - center; fromC.y = 0;
                    if (fromC.magnitude < TitanArena.R - 4) transform.position = next;
                    float t = Time.time * sp * 0.9f;
                    legL.localRotation = Quaternion.Euler(Mathf.Sin(t) * 22, 0, 0); legR.localRotation = Quaternion.Euler(-Mathf.Sin(t) * 22, 0, 0);
                    armL.localRotation = Quaternion.Euler(-Mathf.Sin(t) * 18, 0, 0); armR.localRotation = Quaternion.Euler(Mathf.Sin(t) * 18, 0, 0);
                    body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(t)) * 0.25f;
                    if (Mathf.Abs(Mathf.Sin(t)) < 0.05f && Time.frameCount % 4 == 0) AudioFx.Play("land4", 0.25f);
                    if (dist < 8.5f && stateT > 1.2f) { state = State.Stomp; stateT = 0; AudioFx.Play("jump5", 0.6f); }
                    else if (dist > 11 && rockCd <= 0) { Throw(pl); rockCd = Mathf.Max(1.6f, 4.2f - level * 0.4f); }
                    break;
                }
                case State.Stomp:
                {
                    // lifts the right foot high … and slams it down
                    float up = Mathf.Clamp01(stateT / 0.9f);
                    legR.localRotation = Quaternion.Euler(-70 * up, 0, 0);
                    body.localRotation = Quaternion.Euler(-8 * up, 0, 0);
                    if (stateT > 0.9f)
                    {
                        legR.localRotation = Quaternion.identity; body.localRotation = Quaternion.identity;
                        var foot = transform.position + transform.forward * 1.5f * scale + transform.right * 1.1f * scale;
                        SpawnWave(foot);
                        if (level >= 4) SpawnWave(foot, -2.5f);   // later titans send two waves
                        Stomps++;
                        stompsLeft--;
                        state = stompsLeft <= 0 ? State.Tired : State.Walk;
                        stateT = 0;
                        if (state == State.Tired) { AudioFx.Play("bonk", 1f); Hud.Toast("Он устал и встал на колено! Бей в светящееся сердце!", UIKit.Gold); }
                    }
                    break;
                }
                case State.Tired:
                {
                    // kneels: the heart comes down to where you can hit it
                    float k = Mathf.Clamp01(stateT * 3);
                    body.localPosition = Vector3.down * 3.4f * k;
                    legL.localRotation = Quaternion.Euler(-80 * k, 0, 0); legR.localRotation = Quaternion.Euler(80 * k, 0, 0);
                    armL.localRotation = Quaternion.Euler(-20 * k, 0, 15); armR.localRotation = Quaternion.Euler(-20 * k, 0, -15);
                    coreM.SetColor("_EmissionColor", Color.Lerp(Hex("FF5252"), Hex("FFE14D"), Mathf.PingPong(Time.time * 3, 1)) * 3f);
                    if (stateT > Mathf.Max(3f, 5f - level * 0.3f))
                    {
                        state = State.Walk; stateT = 0; stompsLeft = level >= 3 ? 2 : 3;
                        body.localPosition = Vector3.zero;
                        coreM.SetColor("_EmissionColor", Hex("FF5252") * 1.2f);
                    }
                    break;
                }
            }
            // walking into its feet hurts
            if (state != State.Tired && dist < 2.6f * scale && touchCd <= 0 && pl.transform.position.y < transform.position.y + 3) { touchCd = 1f; pl.HurtBy(transform.position); }
        }

        void Idle()
        {
            float t = Time.time;
            body.localPosition = Vector3.up * Mathf.Sin(t * 0.8f) * 0.15f;
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.4f) * 25, 0);
        }

        void SpawnWave(Vector3 at, float startR = 0)
        {
            var go = new GameObject("ShockWave");
            go.transform.position = new Vector3(at.x, center.y + 0.15f, at.z);
            Ring(go.transform, Vector3.zero, 1f, 0.12f, G("FFE14D", 2.5f));
            waves.Add(new Wave { ring = go.transform, at = go.transform.position, r = startR });
            Effects.Emit("dust", at, 40, 1.2f);
            AudioFx.Play("land4", 1f); AudioFx.Play("punch", 0.8f);
        }

        void UpdateWaves(float dt, PlayerCtl pl)
        {
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var w = waves[i];
                w.r += (10f + level * 1.2f) * dt;
                if (w.r > 0) w.ring.localScale = new Vector3(w.r, 1, w.r);
                if (w.r > TitanArena.R + 2) { Destroy(w.ring.gameObject); waves.RemoveAt(i); continue; }
                if (pl == null || w.hit || w.r <= 0) continue;
                var d = pl.transform.position - w.at; float h = d.y; d.y = 0;
                if (Mathf.Abs(d.magnitude - w.r) < 0.7f && h < 0.7f) { w.hit = true; pl.HurtBy(w.at); }   // jump over the wave!
            }
        }

        void Throw(PlayerCtl pl)
        {
            Rocks++;
            var from = transform.position + Vector3.up * 8f * scale + transform.right * 2.5f * scale;
            var target = pl.transform.position + pl.Velocity * 0.6f;
            float T = 1.5f;
            var v = (target - from) / T - 0.5f * Physics.gravity * T;
            var go = new GameObject("TitanRock");
            go.transform.position = from;
            S(go.transform, Vector3.zero, 1.1f, M(world == 4 ? "E8F6FF" : world == 5 ? "6D4C41" : "8C8796", 0.2f));
            rocks.Add(new Rock { t = go.transform, v = v });
            armR.localRotation = Quaternion.Euler(-120, 0, 0);
            AudioFx.Play("swing");
        }

        void UpdateRocks(float dt, PlayerCtl pl)
        {
            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                var r = rocks[i];
                r.v += Physics.gravity * dt;
                r.t.position += r.v * dt;
                r.t.Rotate(200 * dt, 90 * dt, 0);
                bool hitP = pl != null && (pl.transform.position + Vector3.up * 0.8f - r.t.position).magnitude < 1.3f;
                if (hitP || r.t.position.y < center.y + 0.3f)
                {
                    if (hitP) pl.HurtBy(r.t.position);
                    Effects.Emit("dust", r.t.position, 20, 0.6f);
                    AudioFx.Play("crate", 0.6f);
                    Destroy(r.t.gameObject); rocks.RemoveAt(i);
                }
            }
        }

        /// <summary>The player's hit (punch, weapon, plunge). Only the heart of a kneeling titan takes damage.</summary>
        public bool OnHit(Vector3 from)
        {
            if (Defeated || state == State.Sleep || state == State.Dying) return false;
            if (state != State.Tired)
            {
                if (hitCd <= 0) { hitCd = 0.8f; AudioFx.Play("bonk", 0.6f); Hud.Toast("Броня! Жди, пока он устанет и встанет на колено", UIKit.Orange); }
                return true;
            }
            if (hitCd > 0) return true;
            hitCd = 0.3f;
            Damage(1);
            return true;
        }

        public void Damage(int n)
        {
            Hp = Mathf.Max(0, Hp - n);
            UpdateBar();
            AudioFx.Play("punch", 1f);
            Effects.Emit("star", core.position, 16, 0.6f);
            Effects.Float("-" + n, core.position + Vector3.up, UIKit.Red);
            if (Hp <= 0) { state = State.Dying; stateT = 0; ClearAttacks(); AudioFx.Play("victory"); }
        }

        /// <summary>Tests: put the titan straight into the tired state.</summary>
        public void TestTire() { state = State.Tired; stateT = 0; }

        void Dying(float dt)
        {
            stateT += dt;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-80, transform.eulerAngles.y, 0), dt * 1.5f);
            if (Time.frameCount % 5 == 0) Effects.Emit("stardust", transform.position + Vector3.up * Random.Range(1f, 8f), 6, 1f);
            if (stateT < 2.2f) return;
            Defeated = true;
            Effects.Emit("confetti", transform.position + Vector3.up * 3, 120, 2f, Vector3.up * 6);
            bool first = !Save.P.found.Contains("titan" + world);
            if (first) Save.P.found.Add("titan" + world);
            int coins = first ? 300 + level * 100 : 100 + level * 20;
            Save.AddCoins(coins);
            if (first) Save.P.tokens += 2;
            Save.Write();
            Hud.Toast(first ? $"{TitanArena.NameOf(world)} ПОБЕЖДЁН! +{coins} монет и 2 жетона. Друг свободен!" : $"{TitanArena.NameOf(world)} снова побеждён! +{coins} монет. Друг свободен!", UIKit.Gold);
            // the cage opens by itself
            var cage = Rescue.Cages.Find(c => c.id == TitanArena.FriendOf(world));
            if (cage != null) cage.Free();
            gameObject.SetActive(false);
        }
    }
}
