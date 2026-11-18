using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 17.11 «Закулисье» (the backrooms): a hidden yellow maze of empty office rooms with buzzing, flickering lamps.
    /// The way in is a glitch in the air over the place where Пончик walks in the lobby: double-jump into it.
    /// Somewhere deep inside there is a glowing EXIT door back to the lobby (and three golden eggs in dead ends).
    /// </summary>
    public static class Backrooms
    {
        public static readonly Vector3 O = new Vector3(3000, 30, 3000);
        const int N = 9;            // cells per side
        const float Cell = 6f, H = 3.4f;
        public static Transform Root;
        public static Vector3 Spawn => O + new Vector3(Cell / 2, 0.2f, Cell / 2);
        public static Vector3 ExitPos { get; private set; }
        public static readonly List<Vector3> DeadEnds = new List<Vector3>();
        public static int Entered { get; private set; }
        static GameObject seam;
        static AudioSource hum;
        static bool wasInside;

        public static bool Inside => PlayerCtl.I != null && (PlayerCtl.I.transform.position - (O + new Vector3(N * Cell / 2, 0, N * Cell / 2))).sqrMagnitude < 90 * 90;

        /// <summary>Пончик's home in the lobby (he walks around it); before he is rescued — his future spot.</summary>
        public static Vector3 Anchor
        {
            get
            {
                var f = GameObject.Find("Friend w2c0");
                var fr = f ? f.GetComponent<Friend>() : null;
                return fr != null ? new Vector3(fr.Home.x, 0, fr.Home.z) : new Vector3(-7, 0, 10);
            }
        }
        /// <summary>The glitch to jump into: high over Пончик, only a double jump gets there.</summary>
        public static Vector3 SeamPos => Anchor + Vector3.up * 3.1f;

        /// <summary>Every frame from the player: the way in (lobby), the way out, the lamp buzz.</summary>
        public static int FromTree;   // autotest

        public static void Tick(PlayerCtl pl)
        {
            bool inside = Inside;
            if (inside != wasInside)
            {
                wasInside = inside;
                pl.WaterY = inside ? O.y - 8 : -0.9f;
                if (hum) { if (inside) hum.Play(); else hum.Stop(); }
            }
            if (inside)
            {
                var d = pl.transform.position - ExitPos; d.y = 0;
                if (d.magnitude < 1.3f) Exit();
                return;
            }
            if (Worlds.Current != 1 || pl.MenuOrbit) { if (seam) seam.SetActive(false); return; }
            var s = SeamPos;
            if (seam == null) MakeSeam();
            seam.SetActive(true);
            seam.transform.position = s;
            var p = pl.transform.position;
            var h = p - s; h.y = 0;
            if (!pl.Grounded && pl.AirJumped && h.magnitude < 1.7f && p.y > s.y - 1.6f && p.y < s.y + 1.5f) { Enter(); return; }
            // 18.11: or climb a tree and jump off it with two jumps
            if (!pl.Grounded && pl.AirJumped && pl.LastTreeTop > 0 && Time.time - pl.LastTreeTop < 3f) { pl.LastTreeTop = 0; FromTree++; Enter(); }
        }

        static void MakeSeam()
        {
            Parts.Layer = Layers.World;
            seam = new GameObject("BackroomsSeam");
            var m = Mats.Tex(Wallpaper(), Color.white, 0.1f);
            var q = B(seam.transform, Vector3.zero, new Vector3(0.7f, 0.5f, 0.02f), m);
            q.AddComponent<Spin>().speed = 25;
            var f = seam.AddComponent<SeamFlicker>();
            f.r = q.GetComponent<Renderer>();
        }

        public static void Enter()
        {
            bool fresh = Root == null;
            if (fresh) Build();
            else Rescue.ResetWorld("vip", "brk");   // 18.11: cages and keys again (see-through if done before)
            Entered++;
            var pl = PlayerCtl.I;
            Hud.Glitch();
            AudioFx.Play("modem", 0.5f);
            pl.WaterY = O.y - 8; wasInside = true;
            if (hum) hum.Play();
            pl.Teleport(Spawn, 45);
            pl.SafePos = Spawn;
            Hud.Toast("Ты провалился в ЗАКУЛИСЬЕ… Тут пусто, жёлтые стены и гудят лампы. Найди дверь «ВЫХОД»!", Parts.Hex("FFE680"));
        }

        public static void Exit()
        {
            var pl = PlayerCtl.I;
            Hud.Glitch();
            AudioFx.Play("portal");
            pl.WaterY = -0.9f; wasInside = false;
            if (hum) hum.Stop();
            pl.Teleport(Lobby.Spawn, 0);
            pl.SafePos = Lobby.Spawn;
            pl.PlayVictory();
            Hud.Toast("Фух! Ты выбрался из закулисья", UIKit.Gold);
        }

        // ---------------- building ----------------

        public static void Build()
        {
            Root = new GameObject("Backrooms").transform;
            Root.position = O;
            Parts.Layer = Layers.World;
            var paper = Mats.Tex(Wallpaper(), Color.white, 0.08f); paper.mainTextureScale = new Vector2(3, 1);
            var carpet = Mats.Tex(Carpet(), Color.white, 0.05f); carpet.mainTextureScale = new Vector2(N * 3, N * 3);
            var ceil = Mats.Tex(CeilingTex(), Color.white, 0.1f); ceil.mainTextureScale = new Vector2(N * 3, N * 3);
            var lamp = G("FFF8DC", 1.4f);
            float S = N * Cell;
            var floor = Lobby.Solid(PrimitiveType.Cube, new Vector3(S / 2, -0.25f, S / 2), new Vector3(S, 0.5f, S), carpet, default, Root);
            var top = Lobby.Solid(PrimitiveType.Cube, new Vector3(S / 2, H + 0.25f, S / 2), new Vector3(S, 0.5f, S), ceil, default, Root);
            top.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // a maze: depth-first, then a few walls knocked out so there are loops (easy to get lost)
            var rng = new Rng(1958);
            bool[,] eW = new bool[N + 1, N], nW = new bool[N, N + 1];   // eW[x,z]: wall on the west side of cell x; nW[x,z]: south side of cell z
            for (int x = 0; x <= N; x++) for (int z = 0; z < N; z++) eW[x, z] = true;
            for (int x = 0; x < N; x++) for (int z = 0; z <= N; z++) nW[x, z] = true;
            var seen = new bool[N, N];
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(0, 0)); seen[0, 0] = true;
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            while (stack.Count > 0)
            {
                var c = stack.Peek();
                var opts = new List<Vector2Int>();
                foreach (var d in dirs) { var n = c + d; if (n.x >= 0 && n.y >= 0 && n.x < N && n.y < N && !seen[n.x, n.y]) opts.Add(d); }
                if (opts.Count == 0) { stack.Pop(); continue; }
                var dd = opts[rng.I(opts.Count)];
                var nn = c + dd;
                if (dd.x == 1) eW[c.x + 1, c.y] = false; else if (dd.x == -1) eW[c.x, c.y] = false;
                else if (dd.y == 1) nW[c.x, c.y + 1] = false; else nW[c.x, c.y] = false;
                seen[nn.x, nn.y] = true;
                stack.Push(nn);
            }
            for (int k = 0; k < 10; k++)
            {
                int x = 1 + rng.I(N - 1), z = rng.I(N);
                if (rng.Chance(0.5f)) eW[x, z] = false; else nW[z, x] = false;
            }

            // walls (with a dark skirting board)
            var skirt = M("6B5A2A", 0.2f);
            void WallSeg(Vector3 a, Vector3 b)
            {
                var mid = (a + b) / 2; var d = b - a;
                float len = d.magnitude + 0.3f;
                float yaw = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? 90 : 0;
                var w = Lobby.Solid(PrimitiveType.Cube, mid + Vector3.up * H / 2, new Vector3(0.3f, H, len), paper, new Vector3(0, yaw, 0), Root);
                B(w.transform, new Vector3(0, -0.47f, 0), new Vector3(1.06f, 0.06f, 1.0f), skirt);
            }
            for (int x = 0; x <= N; x++) for (int z = 0; z < N; z++) if (eW[x, z]) WallSeg(new Vector3(x * Cell, 0, z * Cell), new Vector3(x * Cell, 0, (z + 1) * Cell));
            for (int x = 0; x < N; x++) for (int z = 0; z <= N; z++) if (nW[x, z]) WallSeg(new Vector3(x * Cell, 0, z * Cell), new Vector3((x + 1) * Cell, 0, z * Cell));

            // lamps: a panel in every room, some flicker; a few real lights
            for (int x = 0; x < N; x++)
                for (int z = 0; z < N; z++)
                {
                    var p = new Vector3((x + 0.5f) * Cell, H - 0.02f, (z + 0.5f) * Cell);
                    var panel = B(Root, p, new Vector3(1.6f, 0.04f, 0.7f), lamp);
                    if (rng.Chance(0.18f)) panel.AddComponent<LampFlicker>();
                }
            for (int k = 0; k < 4; k++)
            {
                var lg = new GameObject("BackroomsLight"); lg.transform.SetParent(Root, false);
                lg.transform.localPosition = new Vector3((1.5f + (k % 2) * 6) * Cell, H - 0.4f, (1.5f + (k / 2) * 6) * Cell);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Hex("FFF1B0"); l.range = 22; l.intensity = 1.6f; l.shadows = LightShadows.None;
            }

            // distances from the start: the exit goes to the farthest room, eggs into far dead ends
            var dist = new int[N, N];
            for (int x = 0; x < N; x++) for (int z = 0; z < N; z++) dist[x, z] = -1;
            var q = new Queue<Vector2Int>(); q.Enqueue(Vector2Int.zero); dist[0, 0] = 0;
            bool Open(Vector2Int c, Vector2Int d)
            {
                if (d.x == 1) return !eW[c.x + 1, c.y]; if (d.x == -1) return !eW[c.x, c.y];
                if (d.y == 1) return !nW[c.x, c.y + 1]; return !nW[c.x, c.y];
            }
            Vector2Int far = Vector2Int.zero;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (dist[c.x, c.y] > dist[far.x, far.y]) far = c;
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= N || n.y >= N || dist[n.x, n.y] >= 0 || !Open(c, d)) continue;
                    dist[n.x, n.y] = dist[c.x, c.y] + 1; q.Enqueue(n);
                }
            }
            DeadEnds.Clear();
            var ends = new List<Vector2Int>();
            for (int x = 0; x < N; x++)
                for (int z = 0; z < N; z++)
                {
                    var c = new Vector2Int(x, z);
                    int open = 0; foreach (var d in dirs) { var n = c + d; if (n.x >= 0 && n.y >= 0 && n.x < N && n.y < N && Open(c, d)) open++; }
                    if (open == 1 && c != far && c != Vector2Int.zero) ends.Add(c);
                }
            ends.Sort((a, b) => dist[b.x, b.y] - dist[a.x, a.y]);
            foreach (var e in ends) DeadEnds.Add(O + new Vector3((e.x + 0.5f) * Cell, 0, (e.y + 0.5f) * Cell));

            // the EXIT door against a wall of the farthest room
            var fc = new Vector3((far.x + 0.5f) * Cell, 0, (far.y + 0.5f) * Cell);
            ExitPos = O + fc;
            var door = Node("ExitDoor", Root, fc);
            B(door, new Vector3(0, 1.2f, 0), new Vector3(1.5f, 2.4f, 0.12f), G("7DFF9A", 1.3f));
            B(door, new Vector3(0, 1.2f, -0.02f), new Vector3(1.7f, 2.6f, 0.08f), M("3A2A10", 0.3f));
            var sign = Prim.Text(door, "ВЫХОД", new Vector3(0, 2.85f, 0.1f), 3.2f, Hex("2EFF5A"));
            sign.gameObject.AddComponent<Billboard>();
            door.gameObject.AddComponent<Spin>().speed = 30;
            var dl = new GameObject("ExitLight"); dl.transform.SetParent(door, false); dl.transform.localPosition = Vector3.up * 1.5f;
            var li = dl.AddComponent<Light>(); li.type = LightType.Point; li.color = Hex("7DFF9A"); li.range = 8; li.intensity = 2.5f; li.shadows = LightShadows.None;

            // a sign at the start and a few odd things lying around
            var st = Prim.Text(Root, "ЗАКУЛИСЬЕ · УРОВЕНЬ 0", new Vector3(Cell / 2, 2.4f, 0.4f), 3f, Hex("6B5A2A"));
            st.transform.localRotation = Quaternion.identity;
            var chair = M("5A6070", 0.4f);
            for (int k = 0; k < 6; k++)
            {
                var c = new Vector3((rng.I(N) + 0.5f) * Cell + rng.F(-1.5f, 1.5f), 0, (rng.I(N) + 0.5f) * Cell + rng.F(-1.5f, 1.5f));
                if (c.magnitude < Cell) continue;
                Lobby.Solid(PrimitiveType.Cube, c + Vector3.up * 0.45f, new Vector3(0.5f, 0.06f, 0.5f), chair, new Vector3(0, rng.F(0, 360), 0), Root);
            }

            hum = Root.gameObject.AddComponent<AudioSource>();
            hum.loop = true; hum.volume = 0.6f; hum.spatialBlend = 0;
            hum.clip = AudioFx.Get("hum");
            foreach (var r in Root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Eggs.SpawnBackrooms();
            // Артемий, Артём and Али are locked in cages in far dead ends; three golden keys lie in other ones
            for (int k = 0; k < 3; k++)
            {
                var c = DeadEnds.Count > 3 + k ? DeadEnds[3 + k] : O + new Vector3((N - 1.5f - k) * Cell, 0, (N - 0.5f) * Cell);
                Rescue.SpawnCage(Rescue.Vips[k], c, rng.F(0, 360), Root);
                var kp = DeadEnds.Count > 6 + k ? DeadEnds[6 + k] : O + new Vector3((1.5f + k * 2) * Cell, 0, (N - 2.5f) * Cell);
                Pickups.I.Key("brk" + k, kp + Vector3.up * 1.1f);
            }
        }

        // ---------------- textures ----------------

        static Texture2D paperTex, carpetTex, ceilTex;

        public static Texture2D Wallpaper()
        {
            if (paperTex) return paperTex;
            var cv = new Canvas2(64, 64, Hex("D8C46A"));
            var r = new Rng(5);
            for (int x = 0; x < 64; x += 8) cv.Rect(x, 0, 3, 64, Hex("C9B254") * new Color(1, 1, 1, 0.8f));
            for (int y = 4; y < 64; y += 16) for (int x = 1; x < 64; x += 8) { cv.Rect(x + 1, y, 2, 2, Hex("B39B3E")); cv.Rect(x + 5, y + 8, 2, 2, Hex("B39B3E")); }
            for (int i = 0; i < 90; i++) cv.Rect(r.I(64), r.I(64), 1 + r.I(3), 1 + r.I(3), Hex("A8913A") * new Color(1, 1, 1, 0.25f));   // old stains
            return paperTex = cv.Tex(true, true, FilterMode.Bilinear);
        }

        static Texture2D Carpet()
        {
            if (carpetTex) return carpetTex;
            var cv = new Canvas2(64, 64, Hex("9C8A4A"));
            var r = new Rng(6);
            for (int i = 0; i < 900; i++) cv.Rect(r.I(64), r.I(64), 1, 1, (r.Chance(0.5f) ? Hex("8A783C") : Hex("AD9A58")) * new Color(1, 1, 1, 0.7f));
            return carpetTex = cv.Tex(true, true, FilterMode.Bilinear);
        }

        static Texture2D CeilingTex()
        {
            if (ceilTex) return ceilTex;
            var cv = new Canvas2(64, 64, Hex("E2DCC0"));
            var r = new Rng(7);
            for (int i = 0; i < 300; i++) cv.Rect(r.I(64), r.I(64), 1, 1, Hex("C8C0A0"));
            cv.Rect(0, 0, 64, 2, Hex("A8A088")); cv.Rect(0, 0, 2, 64, Hex("A8A088"));
            return ceilTex = cv.Tex(true, true, FilterMode.Bilinear);
        }
    }

    /// <summary>An old lamp that buzzes and blinks off now and then.</summary>
    public class LampFlicker : MonoBehaviour
    {
        Renderer r; float t;
        void Start() { r = GetComponent<Renderer>(); t = Random.Range(1f, 4f); }
        void Update()
        {
            t -= Time.deltaTime;
            if (t > 0) return;
            r.enabled = !r.enabled;
            t = r.enabled ? Random.Range(0.6f, 4f) : Random.Range(0.04f, 0.2f);
        }
    }

    /// <summary>The way into the backrooms: a scrap of yellow wallpaper hanging in the air, blinking in and out.</summary>
    public class SeamFlicker : MonoBehaviour
    {
        public Renderer r; float t;
        void Update()
        {
            t -= Time.deltaTime;
            if (t > 0 || r == null) return;
            r.enabled = !r.enabled;
            t = r.enabled ? Random.Range(0.3f, 1.4f) : Random.Range(0.05f, 0.25f);
            transform.localScale = Vector3.one * Random.Range(0.85f, 1.15f);
        }
    }

    /// <summary>18.11: the top of a tree (or the tree house) — jump off it with two jumps to fall into the backrooms.</summary>
    public class TreeTop : MonoBehaviour { }
}
