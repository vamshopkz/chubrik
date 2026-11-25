using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// v0.31 stickers instead of emotions: the «СТИКЕРЫ» button on top opens 70 stickers (10 from the start, the rest
    /// 4 per hidden chest; the locked ones have a padlock). A sticker pops up over the chubrik's head (the friend in the
    /// room sees it too). Some do something: dances, hearts, confetti, a rocket jump, «Взлёт и плюх» with rainbow drops,
    /// your own word over your head, and «Камень, ножницы, бумага» with the friend who plays with you.
    /// </summary>
    public static class Stickers
    {
        public enum Kind { Say, Emote, Fx, Word, Rps, Splash, Flip, Rocket, Jump, Prop, Seek }
        public enum Move { Hop, Spin, Wobble, Nod, Shake, Squash, Grow, Shiver, LeanBack, Twirl, Bounce3, Tilt, Float, Dizzy, Bow, Jumpy, Sway, None }
        public enum Face { Happy, Laugh, Cool, Wow, Sad, Angry, Love, Sleepy, Tongue, Wink }

        public class St
        {
            public string name, text, fx;
            public Kind kind;
            public Face face;
            public Color col;
            public int emote = -1;
            /// <summary>v0.31: every sticker has its own animation — how the chubrik moves and how the bubble pops in.</summary>
            public Move move;
            public int pop;
            /// <summary>v0.31: a mini video over the head (3 s): a little chubrik in a frame shows the move, the big one does it too.</summary>
            public bool video;
        }

        public const int Free = 10, PerChest = 2;
        public static readonly List<St> All = new List<St>();

        static St A(string name, Kind k, Face f, string col, string text = null, string fx = null, int emote = -1)
        {
            var s = new St { name = name, kind = k, face = f, col = UIKit.Hex(col), text = text ?? name, fx = fx, emote = emote };
            All.Add(s);
            return s;
        }

        static Stickers()
        {
            // free
            A("Привет!", Kind.Say, Face.Happy, "FFD54F");
            A("Ура!", Kind.Say, Face.Laugh, "FFB74D");
            A("Класс!", Kind.Say, Face.Cool, "4FC3F7");
            A("Пошли!", Kind.Say, Face.Happy, "81C784");
            A("Брейк-данс", Kind.Emote, Face.Cool, "BA68C8", "Танцую!", null, 0);
            A("Подмигнуть", Kind.Emote, Face.Wink, "FFD54F", "Подмигнул!", null, 7);
            A("Люблю!", Kind.Fx, Face.Love, "FF80AB", null, "heart");
            A("Своё слово", Kind.Word, Face.Happy, "FFFFFF", "");
            A("Камень, ножницы, бумага", Kind.Rps, Face.Cool, "90CAF9", "Сыграем в КНБ?");
            A("Спасибо!", Kind.Say, Face.Love, "F48FB1");
            // from the chests (4 each, in this order)
            A("Взлёт и плюх", Kind.Splash, Face.Laugh, "80DEEA", "Плюх!");
            A("Ха-ха!", Kind.Say, Face.Laugh, "FFCC80");
            A("Ого!", Kind.Say, Face.Wow, "FFF59D");
            A("Поза супергероя", Kind.Emote, Face.Cool, "EF5350", "Я супергерой!", null, 5);
            A("Хорошая игра!", Kind.Say, Face.Cool, "A5D6A7");
            A("Помоги!", Kind.Say, Face.Sad, "B0BEC5");
            A("Сюда!", Kind.Say, Face.Happy, "AED581");
            A("Праздник!", Kind.Fx, Face.Laugh, "FFAB91", null, "confetti");
            A("Жонглирую", Kind.Emote, Face.Happy, "FFE082", "Смотри, как умею!", null, 4);
            A("Бу-у!", Kind.Say, Face.Angry, "CE93D8");
            A("Не догонишь!", Kind.Say, Face.Tongue, "80CBC4");
            A("Салют!", Kind.Fx, Face.Wow, "FFD54F", null, "star");
            A("Сальто", Kind.Flip, Face.Laugh, "4DD0E1", "Сальто!");
            A("Я тут!", Kind.Say, Face.Happy, "FFF176");
            A("Жду тебя", Kind.Say, Face.Sleepy, "B39DDB");
            A("Сплю…", Kind.Emote, Face.Sleepy, "9FA8DA", "Хр-р-р…", null, 3);
            A("Ой!", Kind.Say, Face.Wow, "FFCDD2");
            A("Молодец!", Kind.Say, Face.Love, "F8BBD0");
            A("Радуга", Kind.Fx, Face.Happy, "B2EBF2", "Радуга!", "rainbow");
            A("Футбол?", Kind.Say, Face.Happy, "C5E1A5", "Сыграем в футбол?");
            A("Давай дружить!", Kind.Say, Face.Love, "F48FB1");
            A("Не знаю", Kind.Emote, Face.Wow, "CFD8DC", "Не знаю…", null, 6);
            A("Смотри!", Kind.Say, Face.Wow, "FFE0B2");
            A("Снегопад", Kind.Fx, Face.Happy, "E1F5FE", "Снежок!", "snow");
            A("Вперёд!", Kind.Say, Face.Angry, "FF8A65");
            A("Победа!", Kind.Say, Face.Cool, "FFD740");
            A("Ракета", Kind.Rocket, Face.Wow, "FF7043", "Поехали!");
            A("Скучно…", Kind.Say, Face.Sad, "B0BEC5");
            A("Обнимашки", Kind.Fx, Face.Love, "FF80AB", "Обнимаю!", "heart");
            A("Пузыри", Kind.Fx, Face.Happy, "B3E5FC", "Буль-буль!", "bubble");
            A("Зарядка", Kind.Emote, Face.Happy, "AED581", "Раз-два!", null, 1);
            A("Я лучший!", Kind.Say, Face.Cool, "FFD54F");
            A("Хи-хи", Kind.Say, Face.Laugh, "F8BBD0");
            A("Где ты?", Kind.Say, Face.Wow, "B3E5FC");
            A("Богатство", Kind.Fx, Face.Cool, "FFD54F", "Я богач!", "coin");
            A("Ну погоди!", Kind.Say, Face.Angry, "FFAB91");
            A("Вкусно!", Kind.Say, Face.Tongue, "FFCC80");
            A("Прости", Kind.Say, Face.Sad, "C5CAE9");
            A("Волшебство", Kind.Fx, Face.Wink, "D1C4E9", "Абракадабра!", "magic");
            A("Где все?", Kind.Emote, Face.Wow, "CFD8DC", "Где все?", null, 2);
            A("Отлично!", Kind.Say, Face.Cool, "80CBC4");
            A("Хочу пиццу", Kind.Say, Face.Tongue, "FFAB91");
            A("Тсс!", Kind.Say, Face.Wink, "B39DDB", "Тсс, тихо!");
            A("Молния", Kind.Fx, Face.Angry, "FFF59D", "Бах!", "elec");
            A("Я устал", Kind.Say, Face.Sleepy, "CFD8DC");
            A("Пока!", Kind.Say, Face.Happy, "FFE082");
            A("Двойное сальто", Kind.Flip, Face.Cool, "4DB6AC", "Двойное сальто!");
            A("Ты супер!", Kind.Say, Face.Love, "F48FB1");
            A("Звёздочки", Kind.Fx, Face.Wink, "FFF9C4", "Звёздочки!", "stardust");
            A("Сыграем?", Kind.Say, Face.Happy, "C5E1A5");
            A("Бегу!", Kind.Say, Face.Wow, "B2DFDB");
            A("Ура, сундук!", Kind.Say, Face.Laugh, "FFD54F");
            A("Огонь!", Kind.Fx, Face.Cool, "FF8A65", null, "fire");
            A("Я король!", Kind.Say, Face.Cool, "FFD740");
            A("Удачи!", Kind.Say, Face.Happy, "C8E6C9");
            A("Ледяной вихрь", Kind.Fx, Face.Wow, "B3E5FC", "Брр!", "ice");
            A("Ням-ням", Kind.Say, Face.Tongue, "FFCC80");
            A("Спокойной ночи", Kind.Say, Face.Sleepy, "9FA8DA");
            A("Прыжок до неба", Kind.Jump, Face.Laugh, "81D4FA", "Ух!");
            A("Мы команда!", Kind.Say, Face.Love, "A5D6A7");
            // v0.32 hide-and-seek (open from the start)
            A("Прятки: превратиться", Kind.Prop, Face.Wink, "B39DDB", "Ищи меня!");
            A("Прятки: я вожу", Kind.Seek, Face.Cool, "455A64", "Считаю!");
            // every sticker its own pair (chubrik move, bubble style): no two the same
            const int M = (int)Move.None;
            for (int i = 0; i < All.Count; i++) { All[i].move = (Move)(i % M); All[i].pop = (i / M + i) % 5; }
            // the mini videos: all dances and tricks, and every third of the others
            for (int i = 0; i < All.Count; i++)
                All[i].video = All[i].kind == Kind.Emote || All[i].kind == Kind.Flip || All[i].kind == Kind.Splash || All[i].kind == Kind.Rocket || All[i].kind == Kind.Jump
                               || ((All[i].kind == Kind.Say || All[i].kind == Kind.Fx) && i % 3 == 1);
            // a few that must fit their words
            Set("Ура!", Move.Bounce3); Set("Ой!", Move.Shiver); Set("Я устал", Move.Squash); Set("Спокойной ночи", Move.Bow);
            Set("Ха-ха!", Move.LeanBack); Set("Ого!", Move.Grow); Set("Не знаю", Move.None); Set("Привет!", Move.Hop);
            Set("Хи-хи", Move.Jumpy); Set("Помоги!", Move.Shake); Set("Где ты?", Move.Tilt); Set("Бегу!", Move.Jumpy);
        }

        static void Set(string name, Move m)
        {
            var s = All.Find(x => x.name == name);
            if (s == null) return;
            var other = All.Find(x => x != s && x.move == m && x.pop == s.pop);   // keep every pair different: swap
            if (other != null) other.move = s.move;
            s.move = m;
        }

        public static bool Unlocked(int i) => i < Free || i >= Free + Chests.Total * PerChest || (i < All.Count && Chests.Opened((i - Free) / PerChest));
        public static int UnlockedCount { get { int n = 0; for (int i = 0; i < All.Count; i++) if (Unlocked(i)) n++; return n; } }
        public static List<int> FromChest(int chest) { var l = new List<int>(); for (int k = 0; k < PerChest; k++) { int i = Free + chest * PerChest + k; if (i < All.Count) l.Add(i); } return l; }
        public static int Used { get; private set; }
        public static int LastUsed { get; private set; } = -1;

        // ---------------- the picker ----------------

        public static void Open()
        {
            Window.Show($"СТИКЕРЫ  {UnlockedCount} / {All.Count}", w =>
            {
                var grid = UIKit.Rect("Grid", w.Content);
                int rows = (All.Count + 9) / 10;
                grid.gameObject.AddComponent<LayoutElement>().minHeight = rows * 132;
                var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
                gl.cellSize = new Vector2(118, 122); gl.spacing = new Vector2(8, 10);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 10;
                for (int i = 0; i < All.Count; i++)
                {
                    int k = i; var s = All[i]; bool open = Unlocked(i);
                    var tile = UIKit.Img(grid, UIKit.Btn, open ? Color.white : new Color(0.55f, 0.6f, 0.7f), "Sticker " + i, true);
                    var b = tile.gameObject.AddComponent<UIButton>(); b.Bg = tile;
                    b.OnClick = () => { if (open) { Window.Close(); Use(k); } else { AudioFx.Play("error"); Hud.Toast("Этот стикер в спрятанном сундуке — ищи сундуки и открывай кнопкой B", UIKit.Orange); } };
                    var pic = new GameObject("Face", typeof(RectTransform)).AddComponent<RawImage>();
                    pic.transform.SetParent(tile.transform, false); pic.raycastTarget = false;
                    UIKit.Place(pic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(70, 70));
                    pic.texture = open ? FaceTex(s.face, s.col) : LockTex();
                    var t = UIKit.Txt(tile.transform, s.name, 15, open ? UIKit.Ink : new Color(0.2f, 0.22f, 0.3f), TextAlignmentOptions.Center, false);
                    UIKit.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(112, 42));
                    t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 15;
                }
                w.Footer.text = $"Новые стикеры — в спрятанных сундуках (открыто {Chests.OpenedCount} из {Chests.Total}, кнопка B рядом с сундуком)";
            }, 1360, 860);
        }

        /// <summary>Plays sticker <paramref name="i"/> on our chubrik and sends it to the room friend.</summary>
        public static void Use(int i, string word = null)
        {
            if (i < 0 || i >= All.Count || !Unlocked(i)) return;
            var s = All[i];
            var pl = PlayerCtl.I;
            if (pl == null) return;
            if (s.kind == Kind.Word && word == null) { AskWord(i); return; }
            if (s.kind == Kind.Rps) { Rps.Ask(); return; }
            if (s.kind == Kind.Prop) { HideSeek.OpenProps(); return; }
            if (s.kind == Kind.Seek) { Pop(pl.transform, i, s.text, pl.Model.SizeScale); HideSeek.StartSeek(); return; }
            Used++; LastUsed = i;
            string text = s.kind == Kind.Word ? word : s.text;
            Pop(pl.transform, i, text, pl.Model.SizeScale, pl.Model.look);
            Animate(pl.Model, s.kind == Kind.Emote || s.kind == Kind.Flip || s.kind == Kind.Splash ? Move.None : s.move);
            Body(pl.Anim, s);
            if (pl.PetNow != null) pl.PetNow.Trick(i);   // the pet's own trick for this sticker
            Act(i, pl);
            var on = Online.I;
            if (on != null && on.RoomWith != null)
                on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"K|{on.Id}|{i}|{Online.Clean(text ?? "")}");
            AudioFx.Play("click", 0.6f);
        }

        static void Act(int i, PlayerCtl pl)
        {
            var s = All[i];
            var at = pl.transform.position + Vector3.up * 1.2f * pl.Model.SizeScale;
            switch (s.kind)
            {
                case Kind.Emote: pl.Anim.emote = s.emote; pl.Anim.PlayEmote(); break;
                case Kind.Fx: Effects.Emit(s.fx, at, 30, 0.7f, Vector3.up * 2); break;
                case Kind.Splash: pl.StickerSplash(); break;
                case Kind.Flip: pl.StickerJump(9f); pl.Anim.Flip(); break;
                case Kind.Rocket: pl.StickerJump(20f); Effects.Emit("fire", pl.transform.position, 30, 0.4f, Vector3.down * 2); AudioFx.Play("portal", 0.6f); break;
                case Kind.Jump: pl.StickerJump(16f); AudioFx.Play("boing"); break;
            }
        }

        static void AskWord(int i)
        {
            Window.Show("СВОЁ СЛОВО", w =>
            {
                w.Text("Напиши любое слово — оно появится у тебя над головой", 28);
                var holder = UIKit.Rect("WordRow", w.Content);
                holder.gameObject.AddComponent<LayoutElement>().minHeight = 96;
                var input = UIKit.Input(holder, "Слово", 24, 34);
                UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(600, 86));
                var go = UIKit.Button(holder, "ПОКАЗАТЬ", UIKit.Green, () =>
                {
                    var t = Online.Clean(input.text).Trim();
                    if (t.Length == 0) return;
                    if (t.Length > 24) t = t.Substring(0, 24);
                    Window.Close(); UIRoot.TypingText = false;
                    Use(i, t);
                }, 30);
                UIKit.Place(go.Bg.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(300, 86));
            }, 1000, 420);
        }

        /// <summary>«K|id|sticker|text» from the room friend: the sticker over his head (and its effect).</summary>
        public static void OnNet(string[] f)
        {
            var on = Online.I;
            if (on == null || f.Length < 4 || f[1] != on.RoomWith) return;
            if (!int.TryParse(f[2], out int i) || i < 0 || i >= All.Count) return;
            if (!on.Peers.TryGetValue(f[1], out var p) || p.avatar == null) return;
            var s = All[i];
            Pop(p.avatar.transform, i, string.Join("|", f, 3, f.Length - 3), 1, p.look);
            Animate(p.avatar.GetComponentInChildren<ChubrikModel>(), s.move);
            Body(p.avatar.GetComponentInChildren<ChubrikAnim>(), s);
            if (p.avatar.PetNow != null) p.avatar.PetNow.Trick(i);
            if (s.kind == Kind.Emote) { var an = p.avatar.GetComponentInChildren<ChubrikAnim>(); if (an) { an.emote = s.emote; an.PlayEmote(); } }
            if (s.kind == Kind.Flip) p.avatar.GetComponentInChildren<ChubrikAnim>()?.Flip();
            var at = p.avatar.transform.position + Vector3.up * 1.2f;
            if (s.kind == Kind.Fx) Effects.Emit(s.fx, at, 30, 0.7f, Vector3.up * 2);
            if (s.kind == Kind.Splash) p.avatar.gameObject.AddComponent<SplashLater>();
        }

        class SplashLater : MonoBehaviour
        {
            float t;
            void Update() { t += Time.deltaTime; if (t > 1.3f) { Rainbow(transform.position); Destroy(this); } }
        }

        /// <summary>Rainbow drops splashing round a point (the end of «Взлёт и плюх»).</summary>
        public static void Rainbow(Vector3 p)
        {
            Effects.Emit("raindrop", p + Vector3.up * 0.3f, 70, 1.2f, Vector3.up * 5);
            Effects.Emit("rainbow", p + Vector3.up * 0.5f, 20, 0.8f);
            AudioFx.Play("splash", 0.9f);
        }

        // ---------------- over the head ----------------

        /// <summary>The sticker over a chubrik: the face in a round bubble and the words under it; pops in, floats, goes.</summary>
        public static GameObject Pop(Transform who, int i, string text, float scale, Look look = null)
        {
            var old = who.Find("StickerPop");
            if (old) Object.Destroy(old.gameObject);
            var s = All[i];
            var go = new GameObject("StickerPop");
            go.transform.SetParent(who, false);
            go.transform.localPosition = Vector3.up * (2.55f * scale);
            int layer = who.gameObject.layer;
            bool video = s.video && look != null;
            if (video) MiniVideo(go.transform, s, look, layer);
            else
            {
                var q = Prim.Make(PrimitiveType.Quad, go.transform, Vector3.zero, Vector3.one * 0.9f, Mats.UnlitTransparent(Color.white, FaceTex(s.face, s.col)), "Face", layer);
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Object.Destroy(q.GetComponent<Collider>());
            }
            if (!string.IsNullOrEmpty(text))
            {
                var t = Prim.Text(go.transform, text, new Vector3(0, video ? -0.78f : -0.62f, 0), 2.2f, Color.white);
                t.gameObject.layer = layer;
            }
            go.AddComponent<Billboard>();
            go.AddComponent<PopLife>().style = s.pop;
            return go;
        }

        /// <summary>The bubble: 5 ways to come in (bounce, spin in, drop from above, zoom with a wiggle, float up), floats, goes.</summary>
        class PopLife : MonoBehaviour
        {
            public int style;
            float t; Vector3 p0;
            void Start() { p0 = transform.localPosition; transform.localScale = Vector3.zero; }
            void Update()
            {
                t += Time.deltaTime;
                float inK = Mathf.Clamp01(t / 0.35f);
                float k = t > 3.6f ? Mathf.Lerp(1, 0, (t - 3.6f) / 0.4f) : 1;
                Vector3 off = Vector3.up * Mathf.Sin(t * 3) * 0.06f; float rz = 0;
                switch (style)
                {
                    case 0: k *= t < 0.25f ? Mathf.SmoothStep(0, 1.15f, t / 0.25f) : t < 0.4f ? Mathf.Lerp(1.15f, 1f, (t - 0.25f) / 0.15f) : 1; break;
                    case 1: k *= inK; rz = (1 - inK) * 540; break;
                    case 2: k *= Mathf.Min(1, inK * 2); off += Vector3.up * (1 - EaseOutBounce(inK)) * 1.4f; break;
                    case 3: k *= Mathf.Lerp(0, 1, inK) + Mathf.Sin(t * 18) * 0.08f * Mathf.Max(0, 1 - t); rz = Mathf.Sin(t * 20) * 12 * Mathf.Max(0, 1 - t); break;
                    default: k *= inK; off += Vector3.up * (t * 0.12f - 0.4f * (1 - inK)); break;
                }
                transform.localScale = Vector3.one * k;
                transform.localPosition = p0 + off;
                foreach (Transform c in transform) c.localRotation = Quaternion.Euler(0, 0, rz);
                if (t > 4f) Destroy(gameObject);
            }

            static float EaseOutBounce(float x)
            {
                const float n = 7.5625f, d = 2.75f;
                if (x < 1 / d) return n * x * x;
                if (x < 2 / d) { x -= 1.5f / d; return n * x * x + 0.75f; }
                if (x < 2.5 / d) { x -= 2.25f / d; return n * x * x + 0.9375f; }
                x -= 2.625f / d; return n * x * x + 0.984375f;
            }
        }

        /// <summary>The mini video: a rounded screen in the sticker's colour with a little copy of the chubrik (same outfit)
        /// doing the sticker's move or dance for 3 seconds, a tiny face in the corner.</summary>
        static void MiniVideo(Transform parent, St s, Look look, int layer)
        {
            var scr = new GameObject("Video").transform;
            scr.SetParent(parent, false);
            var frame = Prim.Make(PrimitiveType.Quad, scr, new Vector3(0, 0.05f, 0.02f), new Vector3(1.25f, 1.05f, 1), Mats.UnlitTransparent(Color.white, ScreenTex(s.col)), "Screen", layer);
            frame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(frame.GetComponent<Collider>());
            var face = Prim.Make(PrimitiveType.Quad, scr, new Vector3(0.5f, 0.45f, 0.01f), Vector3.one * 0.32f, Mats.UnlitTransparent(Color.white, FaceTex(s.face, s.col)), "Face", layer);
            Object.Destroy(face.GetComponent<Collider>());
            var holder = new GameObject("Mini").transform;
            holder.SetParent(scr, false);
            holder.localPosition = new Vector3(0, -0.36f, -0.05f);
            holder.localRotation = Quaternion.Euler(0, 180, 0);   // faces the camera
            holder.localScale = Vector3.one * 0.42f;
            var m = ChubrikModel.Create("MiniChubrik", holder, look, layer);
            foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var an = m.gameObject.AddComponent<ChubrikAnim>();
            an.model = m; an.Preview = true; an.emote = Mathf.Max(0, look.Idx("emote"));
            var player = m.gameObject.AddComponent<MiniPlayer>();
            player.st = s; player.anim = an; player.model = m;
        }

        /// <summary>The body move (arms, legs, head) that goes with the sticker's move (dances have their own already).</summary>
        static void Body(ChubrikAnim an, St s)
        {
            if (an == null || s.kind == Kind.Emote) return;
            int pose = s.move == Move.None ? 2 : (int)s.move;
            an.StickerPose(pose, 1.8f);
        }

        /// <summary>Plays the sticker's move on the mini chubrik, again and again for the 3 seconds.</summary>
        class MiniPlayer : MonoBehaviour
        {
            public St st; public ChubrikAnim anim; public ChubrikModel model;
            float t, next;
            void Update()
            {
                t += Time.deltaTime;
                if (t < next) return;
                next = t + StickerMove.Len + 0.15f;
                switch (st.kind)
                {
                    case Kind.Emote: anim.emote = st.emote; anim.PlayEmote(); next = 99; break;
                    case Kind.Flip: anim.Flip(); Animate(model, Move.Hop); break;
                    case Kind.Splash: Animate(model, Move.Bounce3); break;
                    case Kind.Rocket: Animate(model, Move.Float); break;
                    case Kind.Jump: Animate(model, Move.Twirl); break;
                    default: Animate(model, st.move == Move.None ? Move.Hop : st.move); anim.StickerPose(st.move == Move.None ? 2 : (int)st.move, StickerMove.Len); break;
                }
            }
        }

        static readonly Dictionary<string, Texture2D> screens = new Dictionary<string, Texture2D>();
        /// <summary>A little TV screen: a white rounded frame, inside a soft gradient of the sticker's colour.</summary>
        static Texture2D ScreenTex(Color col)
        {
            string key = ColorUtility.ToHtmlStringRGB(col);
            if (screens.TryGetValue(key, out var have) && have) return have;
            const int W = 120, H = 100;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - W / 2f + 0.5f) - (W / 2f - 14)), dy = Mathf.Max(0, Mathf.Abs(y - H / 2f + 0.5f) - (H / 2f - 14));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = new Color(0, 0, 0, 0);
                    if (d < 13) c = Color.white;
                    if (d < 9) c = Color.Lerp(Color.Lerp(col, new Color(0.1f, 0.15f, 0.35f), 0.35f), Color.Lerp(col, Color.white, 0.45f), y / (float)H);
                    if (d > 12) c.a *= Mathf.InverseLerp(13, 12, d);
                    px[y * W + x] = c;
                }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px); tex.Apply(false, true);
            screens[key] = tex;
            return tex;
        }

        /// <summary>The chubrik's own little animation for a sticker (the model's root moves; put back exactly after).</summary>
        public static void Animate(ChubrikModel m, Move mv)
        {
            if (m == null || mv == Move.None) return;
            var old = m.GetComponent<StickerMove>();
            if (old) old.Stop();
            var a = m.gameObject.AddComponent<StickerMove>();
            a.move = mv;
        }

        public class StickerMove : MonoBehaviour
        {
            public Move move;
            float t; Vector3 p0, s0; Quaternion r0; bool started;
            public const float Len = 1.6f;
            void Start() { p0 = transform.localPosition; s0 = transform.localScale; r0 = transform.localRotation; started = true; }
            public void Stop() { if (started) { transform.localPosition = p0; transform.localScale = s0; transform.localRotation = r0; } Destroy(this); }
            void LateUpdate()
            {
                if (!started) return;
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Len), env = Mathf.Sin(u * Mathf.PI);
                Vector3 dp = Vector3.zero, ds = Vector3.one; Vector3 rot = Vector3.zero;
                switch (move)
                {
                    case Move.Hop: dp.y = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2)) * 0.5f; break;
                    case Move.Spin: rot.y = Mathf.SmoothStep(0, 360, u); break;
                    case Move.Wobble: rot.z = Mathf.Sin(t * 14) * 14 * env; break;
                    case Move.Nod: rot.x = Mathf.Max(0, Mathf.Sin(t * 10)) * 18 * env; break;
                    case Move.Shake: rot.y = Mathf.Sin(t * 26) * 22 * env; break;
                    case Move.Squash: ds = new Vector3(1 + 0.25f * env, 1 - 0.3f * env, 1 + 0.25f * env); break;
                    case Move.Grow: ds = Vector3.one * (1 + 0.35f * env); break;
                    case Move.Shiver: dp.x = Mathf.Sin(t * 70) * 0.05f * env; dp.z = Mathf.Cos(t * 63) * 0.04f * env; break;
                    case Move.LeanBack: rot.x = -22 * env + Mathf.Sin(t * 30) * 4 * env; break;
                    case Move.Twirl: dp.y = env * 0.7f; rot.y = u * 720; break;
                    case Move.Bounce3: { float b = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3)); dp.y = b * 0.45f; ds = new Vector3(1 + (1 - b) * 0.12f, 1 - (1 - b) * 0.12f, 1 + (1 - b) * 0.12f); break; }
                    case Move.Tilt: rot.z = 20 * env * Mathf.Sign(Mathf.Sin(u * Mathf.PI * 2 + 0.01f)); break;
                    case Move.Float: dp.y = env * 0.9f; rot.y = Mathf.Sin(t * 4) * 10 * env; break;
                    case Move.Dizzy: rot.x = Mathf.Sin(t * 9) * 12 * env; rot.z = Mathf.Cos(t * 9) * 12 * env; break;
                    case Move.Bow: rot.x = 38 * env; break;
                    case Move.Jumpy: dp.y = Mathf.Abs(Mathf.Sin(t * 22)) * 0.2f * env; break;
                    case Move.Sway: dp.x = Mathf.Sin(t * 6) * 0.25f * env; rot.z = -Mathf.Sin(t * 6) * 10 * env; break;
                }
                transform.localPosition = p0 + transform.localRotation * Vector3.zero + dp;
                transform.localScale = Vector3.Scale(s0, ds);
                transform.localRotation = r0 * Quaternion.Euler(rot);
                if (t >= Len) Stop();
            }
        }

        // ---------------- pictures ----------------

        static readonly Dictionary<string, Texture2D> faces = new Dictionary<string, Texture2D>();
        static Texture2D lockTex;

        /// <summary>A round sticker face drawn from code: a coloured ball with a white rim, eyes and a mouth by mood.</summary>
        public static Texture2D FaceTex(Face f, Color col)
        {
            string key = f + ColorUtility.ToHtmlStringRGB(col);
            if (faces.TryGetValue(key, out var have) && have) return have;
            const int N = 128;
            var px = new Color[N * N];
            var ink = new Color(0.18f, 0.12f, 0.1f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1;
                    float r = Mathf.Sqrt(u * u + v * v);
                    Color c = new Color(0, 0, 0, 0);
                    if (r < 0.98f) c = Color.white;
                    if (r < 0.88f) c = Color.Lerp(col, Color.white, Mathf.Clamp01((v + u * 0.3f) * 0.35f));
                    if (r < 0.88f)
                    {
                        // eyes
                        foreach (var sx in new[] { -0.32f, 0.32f })
                        {
                            float ex = u - sx, ey = v - 0.2f;
                            bool wink = f == Face.Wink && sx > 0;
                            if (f == Face.Cool) { if (Mathf.Abs(ey) < 0.13f && Mathf.Abs(ex) < 0.24f) c = ink; if (Mathf.Abs(ey - 0.08f) < 0.03f && Mathf.Abs(u) < 0.6f) c = ink; }
                            else if (f == Face.Love) { if (Heart(ex / 0.2f, ey / 0.2f)) c = new Color(0.9f, 0.15f, 0.3f); }
                            else if (f == Face.Laugh || f == Face.Sleepy || wink) { float yy = ey + (f == Face.Sleepy ? 0.04f : -0.0f); if (Mathf.Abs(yy - ex * ex * (f == Face.Sleepy ? -1.2f : 2.2f)) < 0.035f && Mathf.Abs(ex) < 0.14f) c = ink; }
                            else if (f == Face.Angry) { if (ex * ex + ey * ey < 0.012f) c = ink; if (Mathf.Abs(ey - 0.16f + ex * (sx > 0 ? 0.6f : -0.6f)) < 0.03f && Mathf.Abs(ex) < 0.16f) c = ink; }
                            else { float er = f == Face.Wow ? 0.13f : 0.1f; if (ex * ex + ey * ey < er * er) c = ink; if ((ex + 0.03f) * (ex + 0.03f) + (ey - 0.04f) * (ey - 0.04f) < 0.0012f) c = Color.white; }
                        }
                        // mouth
                        float mx = u, my = v + 0.32f;
                        switch (f)
                        {
                            case Face.Happy: case Face.Love: case Face.Wink: case Face.Cool:
                                if (Mathf.Abs(my - mx * mx * 0.9f) < 0.04f && Mathf.Abs(mx) < 0.34f) c = ink; break;
                            case Face.Laugh: case Face.Tongue:
                                if (my < mx * mx * 0.9f + 0.02f && my > -0.2f + mx * mx * 0.4f && Mathf.Abs(mx) < 0.36f) c = my < -0.12f && f == Face.Tongue ? new Color(1f, 0.45f, 0.55f) : (my < -0.13f ? new Color(0.9f, 0.3f, 0.35f) : ink);
                                if (f == Face.Tongue && mx * mx + (my + 0.2f) * (my + 0.2f) < 0.018f) c = new Color(1f, 0.45f, 0.55f);
                                break;
                            case Face.Wow: if (mx * mx / 0.016f + (my + 0.04f) * (my + 0.04f) / 0.03f < 1) c = ink; break;
                            case Face.Sad: if (Mathf.Abs(my + 0.08f + mx * mx * 0.9f) < 0.04f && Mathf.Abs(mx) < 0.3f) c = ink; if ((u + 0.32f) * (u + 0.32f) / 0.003f + (v - 0.02f) * (v - 0.02f) / 0.012f < 1) c = new Color(0.4f, 0.7f, 1f); break;
                            case Face.Angry: if (Mathf.Abs(my + 0.05f + mx * mx * 0.6f) < 0.04f && Mathf.Abs(mx) < 0.28f) c = ink; break;
                            case Face.Sleepy: if (mx * mx + (my + 0.02f) * (my + 0.02f) < 0.006f) c = ink; break;
                        }
                        // rosy cheeks
                        if (f != Face.Cool && f != Face.Angry && ((u - 0.55f) * (u - 0.55f) + (v + 0.12f) * (v + 0.12f) < 0.012f || (u + 0.55f) * (u + 0.55f) + (v + 0.12f) * (v + 0.12f) < 0.012f)) c = Color.Lerp(c, new Color(1f, 0.5f, 0.55f), 0.55f);
                    }
                    // soft edge
                    if (r > 0.96f && r < 1f) c.a *= Mathf.InverseLerp(1f, 0.96f, r);
                    px[y * N + x] = c;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px); tex.Apply(false, true);
            faces[key] = tex;
            return tex;
        }

        static bool Heart(float x, float y)
        {
            y = -y + 0.3f; x = Mathf.Abs(x);
            float a = x * x + y * y - 1;
            return a * a * a - x * x * y * y * y < 0;
        }

        /// <summary>A grey padlock (a locked sticker).</summary>
        static Texture2D LockTex()
        {
            if (lockTex) return lockTex;
            const int N = 96;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1;
                    Color c = new Color(0, 0, 0, 0);
                    if (Mathf.Abs(u) < 0.55f && v < 0.1f && v > -0.75f) c = new Color(0.3f, 0.33f, 0.42f);
                    float rr = Mathf.Sqrt(u * u + (v - 0.15f) * (v - 0.15f));
                    if (v > 0.05f && rr > 0.28f && rr < 0.42f) c = new Color(0.3f, 0.33f, 0.42f);
                    if (u * u + (v + 0.28f) * (v + 0.28f) < 0.012f || (Mathf.Abs(u) < 0.04f && v < -0.28f && v > -0.5f)) c = new Color(0.85f, 0.88f, 0.95f);
                    px[y * N + x] = c;
                }
            lockTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            lockTex.SetPixels(px); lockTex.Apply(false, true);
            return lockTex;
        }

        // ---------------- nicknames over heads ----------------

        /// <summary>v0.31: the nickname over our chubrik and over the friend's (kept up to date).</summary>
        public class NickTag : MonoBehaviour
        {
            public System.Func<string> nick;
            public System.Func<float> height;
            TextMeshPro t; string shown;
            void Start()
            {
                t = Prim.Text(transform, "", Vector3.zero, 2.0f, new Color(1f, 0.95f, 0.6f));
                t.gameObject.layer = gameObject.layer;
                t.gameObject.AddComponent<Billboard>();
            }
            void LateUpdate()
            {
                if (t == null) return;
                var n = nick != null ? nick() : "";
                if (n != shown) { shown = n; t.text = n; }
                t.transform.position = transform.position + Vector3.up * (height != null ? height() : 2.1f);
                bool hide = WardrobeUI.IsOpen || Menus.MainOpen || transform.Find("StickerPop") != null;
                if (t.gameObject.activeSelf == hide) t.gameObject.SetActive(!hide);
            }
        }

        public static void Tag(GameObject go, System.Func<string> nick, System.Func<float> height)
        {
            if (go == null || go.GetComponent<NickTag>() != null) return;
            var n = go.AddComponent<NickTag>(); n.nick = nick; n.height = height;
        }
    }

    /// <summary>
    /// v0.31 «Камень, ножницы, бумага» with the friend who plays with you (in the room): the sticker sends a request,
    /// both get the three buttons at the bottom (and a «×» that stops the game). Both pick → the picks pop over the
    /// heads and the winner is shown; then the next round.
    /// </summary>
    public static class Rps
    {
        public static readonly string[] Names = { "Камень", "Ножницы", "Бумага" };
        static GameObject bar;
        static TextMeshProUGUI status;
        public static bool IsOpen => bar != null;
        public static int Mine = -1, Theirs = -1, Wins, Losses;
        static float nextRound = -1;
        internal static void ResetStatics() { if (bar) Object.Destroy(bar); bar = null; Mine = Theirs = -1; }

        /// <summary>0 draw, 1 a wins, 2 b wins (rock beats scissors, scissors beat paper, paper beats rock).</summary>
        public static int Judge(int a, int b) => a == b ? 0 : (a + 1) % 3 == b ? 1 : 2;

        static Online.Peer Friend()
        {
            var on = Online.I;
            return on != null ? on.RoomPeer : null;
        }

        public static void Ask()
        {
            var p = Friend();
            if (p == null) { AudioFx.Play("error"); Hud.Toast("Камень, ножницы, бумага — с другом, который играет с тобой («Игроки онлайн» → пригласить)", UIKit.Orange); return; }
            if (PlayerCtl.I && Vector3.Distance(PlayerCtl.I.transform.position, p.pos) > 12f) { AudioFx.Play("error"); Hud.Toast("Подойди поближе к другу", UIKit.Orange); return; }
            Send("ask");
            Show("Ждём друга… Выбирай!");
            Stickers.Pop(PlayerCtl.I.transform, 8, "Сыграем в КНБ?", PlayerCtl.I.Model.SizeScale);
        }

        static void Send(string what)
        {
            var on = Online.I;
            if (on != null && on.RoomWith != null) on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"KR|{on.Id}|{what}");
        }

        public static void OnNet(string[] f)
        {
            var on = Online.I;
            if (on == null || f.Length < 3 || f[1] != on.RoomWith) return;
            switch (f[2])
            {
                case "ask": Show("Друг зовёт сыграть! Выбирай:"); AudioFx.Play("note4"); break;
                case "stop": Close(false); Hud.Toast("Друг закончил игру «Камень, ножницы, бумага»", UIKit.Muted); break;
                default: if (int.TryParse(f[2], out int k) && k >= 0 && k < 3) { Theirs = k; if (!IsOpen) Show("Друг уже выбрал! Твой ход:"); Check(); } break;
            }
        }

        public static void Pick(int k)
        {
            if (Mine >= 0 || k < 0 || k > 2) return;
            Mine = k;
            Send(k.ToString(CultureInfo.InvariantCulture));
            if (status) status.text = $"Ты выбрал: {Names[k]}. Ждём друга…";
            AudioFx.Play("click");
            Check();
        }

        static void Check()
        {
            if (Mine < 0 || Theirs < 0) return;
            int r = Judge(Mine, Theirs);
            if (r == 1) Wins++; else if (r == 2) Losses++;
            var pl = PlayerCtl.I;
            if (pl) Stickers.Pop(pl.transform, 8, Names[Mine], pl.Model.SizeScale);
            var p = Friend();
            if (p != null && p.avatar != null) Stickers.Pop(p.avatar.transform, 8, Names[Theirs], 1);
            string res = r == 0 ? "Ничья!" : r == 1 ? "Ты победил!" : "Друг победил!";
            if (status) status.text = $"{Names[Mine]} против «{Names[Theirs]}» — {res}  (счёт {Wins} : {Losses})";
            AudioFx.Play(r == 1 ? "buy" : r == 2 ? "error" : "click");
            if (r == 1 && pl) Effects.Emit("confetti", pl.transform.position + Vector3.up * 2, 30, 0.5f, Vector3.up * 3);
            nextRound = Time.unscaledTime + 2.5f;
        }

        static void Show(string text)
        {
            if (bar == null)
            {
                var bg = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(0.08f, 0.12f, 0.3f, 0.92f), "Rps", true);
                UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16 + UIRoot.Safe().y), new Vector2(880, 190));
                bar = bg.gameObject;
                status = UIKit.Txt(bg.transform, "", 26, Color.white);
                UIKit.Place(status.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-20, -6), new Vector2(780, 44));
                status.enableAutoSizing = true; status.fontSizeMin = 16; status.fontSizeMax = 26;
                for (int k = 0; k < 3; k++)
                {
                    int kk = k;
                    var b = UIKit.Button(bg.transform, Names[k].ToUpperInvariant(), new[] { UIKit.Blue, UIKit.Orange, UIKit.Green }[k], () => Pick(kk), 28, "Rps" + k);
                    UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-290 + k * 260, 16), new Vector2(240, 110));
                }
                var x = UIKit.Button(bg.transform, "×", UIKit.Red, () => Close(true), 40, "RpsStop");
                UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(66, 60));
                bar.AddComponent<Runner>();
            }
            Mine = -1; Theirs = Theirs >= 0 && nextRound < 0 ? Theirs : -1;
            status.text = text;
        }

        public static void Close(bool tell)
        {
            if (tell) Send("stop");
            if (bar) Object.Destroy(bar);
            bar = null; Mine = Theirs = -1; nextRound = -1;
        }

        class Runner : MonoBehaviour
        {
            void Update()
            {
                if (Friend() == null) { Close(false); return; }
                if (nextRound > 0 && Time.unscaledTime > nextRound) { nextRound = -1; Mine = Theirs = -1; if (status) status.text = "Новый раунд! Выбирай:"; }
            }
        }
    }
}
