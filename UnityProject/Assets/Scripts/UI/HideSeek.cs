using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// v0.32 hide-and-seek with stickers.
    /// «Прятки: превратиться» — a list of things from the game (a crate, a barrel, a bush, a mushroom, a bench…): pick one
    /// and you become it (the chubrik and the pet disappear, the friend in the room sees the thing too). «Снять маскировку»
    /// turns you back.
    /// «Прятки: я вожу» — the seeker goes into a black square (a dark room far away) and picks how long to wait there:
    /// 5…35 seconds; then he comes back to where he stood and goes looking.
    /// </summary>
    public static class HideSeek
    {
        // ---------------- disguises ----------------

        public static readonly string[] Props =
        {
            "Ящик", "Бочка", "Камень", "Куст", "Гриб", "Пенёк", "Снеговик", "Скамейка", "Ёлочка", "Фонарь",
            "Кактус", "Тыква", "Сундук", "Мяч", "Ведро", "Шина", "Цветок", "Подарок", "Конус", "Арбуз",
            "Почтовый ящик", "Мусорка", "Стул", "Пожарный кран",
        };

        public static int Current { get; private set; } = -1;
        static GameObject myProp, offBtn;
        internal static void ResetStatics() { Current = -1; myProp = null; offBtn = null; seekRoom = null; seekUntil = -1; }

        public static void OpenProps()
        {
            Window.Show("ПРЯТКИ: В КОГО ПРЕВРАТИТЬСЯ?", w =>
            {
                w.Text("Выбери вещь — и ты станешь ею. Спрячься среди таких же вещей, чтобы друг тебя не нашёл!", 26);
                var grid = UIKit.Rect("Props", w.Content);
                grid.gameObject.AddComponent<LayoutElement>().minHeight = (Props.Length + 5) / 6 * 96;
                var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
                gl.cellSize = new Vector2(176, 84); gl.spacing = new Vector2(10, 12);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 6;
                for (int i = 0; i < Props.Length; i++)
                {
                    int k = i;
                    UIKit.Button(grid, Props[i].ToUpperInvariant(), i == Current ? UIKit.Gold : UIKit.Blue, () => { Window.Close(); Become(k); }, 20, "Prop " + i);
                }
                if (Current >= 0) w.Row("Снять маскировку", "Снова стать чубриком", "СНЯТЬ", UIKit.Red, () => { Window.Close(); Become(-1); });
            }, 1200, 760);
        }

        /// <summary>Become thing <paramref name="k"/> (-1: the chubrik again).</summary>
        public static void Become(int k)
        {
            var pl = PlayerCtl.I;
            if (pl == null) return;
            if (myProp) Object.Destroy(myProp);
            myProp = null;
            Current = k;
            pl.Model.gameObject.SetActive(k < 0);
            if (pl.PetNow) pl.PetNow.gameObject.SetActive(k < 0);
            if (k >= 0)
            {
                myProp = Build(k, pl.transform, pl.gameObject.layer);
                Effects.Emit("puff", pl.transform.position + Vector3.up * 0.6f, 20, 0.6f);
                AudioFx.Play("magic");
                Hud.Toast($"Ты — {Props[k].ToLowerInvariant()}! Стой тихо, чтобы тебя не нашли", UIKit.Lime);
            }
            else { Effects.Emit("puff", pl.transform.position + Vector3.up * 0.6f, 20, 0.6f); AudioFx.Play("portal", 0.5f); }
            ShowOff(k >= 0);
            var on = Online.I;
            if (on != null && on.RoomWith != null) on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"KP|{on.Id}|{k}");
        }

        static void ShowOff(bool show)
        {
            if (!show) { if (offBtn) Object.Destroy(offBtn); offBtn = null; return; }
            if (offBtn) return;
            var b = UIKit.Button(UIRoot.HudLayer, "СНЯТЬ МАСКИРОВКУ", UIKit.Red, () => Become(-1), 22, "PropOff");
            UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96 - UIRoot.Safe().w), new Vector2(330, 64));
            offBtn = b.Bg.gameObject;
        }

        /// <summary>«KP|id|k» from the room friend: he turned into a thing (or back).</summary>
        public static void OnNet(string[] f)
        {
            var on = Online.I;
            if (on == null || f.Length < 3 || f[1] != on.RoomWith || !int.TryParse(f[2], out int k)) return;
            if (!on.Peers.TryGetValue(f[1], out var p) || p.avatar == null) return;
            var av = p.avatar.transform;
            var old = av.Find("Disguise");
            if (old) Object.Destroy(old.gameObject);
            var model = av.GetComponentInChildren<ChubrikModel>(true);
            if (model) model.gameObject.SetActive(k < 0);
            if (p.avatar.PetNow) p.avatar.PetNow.gameObject.SetActive(k < 0);
            if (k >= 0 && k < Props.Length) Build(k, av, av.gameObject.layer);
            Effects.Emit("puff", av.position + Vector3.up * 0.6f, 20, 0.6f);
        }

        /// <summary>The things, built from code at about real size.</summary>
        static GameObject Build(int k, Transform parent, int layer)
        {
            var t = Node("Disguise", parent, parent.position);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity;
            int was = Parts.Layer; Parts.Layer = layer;
            switch (k)
            {
                case 0: B(t, new Vector3(0, 0.5f, 0), Vector3.one, M("B07A3E", 0.2f)); foreach (var y in new[] { 0.15f, 0.85f }) B(t, new Vector3(0, y, 0), new Vector3(1.02f, 0.1f, 1.02f), M("7A4E22", 0.2f)); break;
                case 1: Cy(t, new Vector3(0, 0.6f, 0), new Vector3(0.9f, 0.6f, 0.9f), M("8B5A2B", 0.25f)); foreach (var y in new[] { 0.25f, 0.95f }) Cy(t, new Vector3(0, y, 0), new Vector3(0.93f, 0.04f, 0.93f), M("3A3F48", 0.6f, 0.6f)); break;
                case 2: S(t, new Vector3(0, 0.45f, 0), new Vector3(1.3f, 0.9f, 1.1f), M("9AA0A8", 0.25f)); break;
                case 3: for (int i = 0; i < 5; i++) S(t, new Vector3(Mathf.Cos(i * 1.3f) * 0.35f, 0.5f + (i % 2) * 0.2f, Mathf.Sin(i * 1.3f) * 0.35f), 0.8f, M(i % 2 == 0 ? "2E9E3E" : "3FB34F", 0.2f)); break;
                case 4: Cy(t, new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.35f, 0.35f), M("F4EBD9", 0.2f)); S(t, new Vector3(0, 0.75f, 0), new Vector3(1.2f, 0.6f, 1.2f), M("E53935", 0.3f)); for (int i = 0; i < 5; i++) S(t, new Vector3(Mathf.Cos(i * 1.25f) * 0.35f, 0.95f, Mathf.Sin(i * 1.25f) * 0.35f), 0.14f, M("FFFFFF", 0.2f)); break;
                case 5: Cy(t, new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.35f, 0.9f), M("7A4E22", 0.2f)); Cy(t, new Vector3(0, 0.71f, 0), new Vector3(0.84f, 0.01f, 0.84f), M("D9B98A", 0.2f)); break;
                case 6: S(t, new Vector3(0, 0.45f, 0), 0.9f, M("FFFFFF", 0.2f)); S(t, new Vector3(0, 1.1f, 0), 0.6f, M("FFFFFF", 0.2f)); Cone(t, new Vector3(0, 1.12f, 0.32f), new Vector3(0.1f, 0.25f, 0.1f), M("FF8A1E", 0.3f), new Vector3(90, 0, 0)); break;
                case 7: B(t, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.1f, 0.5f), M("A0642F", 0.2f)); B(t, new Vector3(0, 0.75f, 0.22f), new Vector3(1.6f, 0.5f, 0.08f), M("A0642F", 0.2f)); foreach (var x in new[] { -0.7f, 0.7f }) B(t, new Vector3(x, 0.22f, 0), new Vector3(0.08f, 0.45f, 0.45f), M("3A3F48", 0.5f)); break;
                case 8: Cy(t, new Vector3(0, 0.2f, 0), new Vector3(0.2f, 0.2f, 0.2f), M("6D4425", 0.2f)); for (int i = 0; i < 3; i++) Cone(t, new Vector3(0, 0.5f + i * 0.4f, 0), new Vector3(1.1f - i * 0.3f, 0.6f, 1.1f - i * 0.3f), M("1F7A35", 0.2f)); break;
                case 9: Cy(t, new Vector3(0, 0.9f, 0), new Vector3(0.12f, 0.9f, 0.12f), M("2B2F36", 0.5f)); S(t, new Vector3(0, 1.9f, 0), 0.35f, G("FFE58A", 1.5f)); break;
                case 10: Ca(t, new Vector3(0, 0.7f, 0), new Vector3(0.45f, 0.7f, 0.45f), M("3B9B45", 0.2f)); Ca(t, new Vector3(0.35f, 0.8f, 0), new Vector3(0.22f, 0.3f, 0.22f), M("3B9B45", 0.2f), new Vector3(0, 0, -40)); break;
                case 11: S(t, new Vector3(0, 0.38f, 0), new Vector3(0.9f, 0.7f, 0.9f), M("FF8A1E", 0.3f)); Cy(t, new Vector3(0, 0.78f, 0), new Vector3(0.08f, 0.1f, 0.08f), M("4E7A2A", 0.2f)); break;
                case 12: B(t, new Vector3(0, 0.3f, 0), new Vector3(0.9f, 0.6f, 0.6f), M("8B4A1F", 0.25f)); Cy(t, new Vector3(0, 0.6f, 0), new Vector3(0.6f, 0.45f, 0.45f), M("8B4A1F", 0.25f), new Vector3(0, 0, 90)); B(t, new Vector3(0, 0.55f, -0.31f), new Vector3(0.14f, 0.18f, 0.04f), M("FFC83A", 0.9f, 0.85f)); break;
                case 13: S(t, new Vector3(0, 0.35f, 0), 0.7f, Mats.Tex(Tex.Ball(), Color.white, 0.4f)); break;
                case 14: Cy(t, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.3f, 0.6f), M("2196F3", 0.4f)); Ring(t, new Vector3(0, 0.75f, 0), 0.3f, 0.02f, M("8C96A3", 0.5f), new Vector3(0, 0, 90)); break;
                case 15: Ring(t, new Vector3(0, 0.4f, 0), 0.38f, 0.14f, M("222222", 0.3f), new Vector3(90, 0, 0)); break;
                case 16: Cy(t, new Vector3(0, 0.4f, 0), new Vector3(0.06f, 0.4f, 0.06f), M("3B9B45", 0.2f)); for (int i = 0; i < 6; i++) S(t, new Vector3(Mathf.Cos(i * 1.05f) * 0.17f, 0.85f, Mathf.Sin(i * 1.05f) * 0.17f), 0.2f, M("FF6FA8", 0.3f)); S(t, new Vector3(0, 0.85f, 0), 0.16f, M("FFE14D", 0.3f)); break;
                case 17: B(t, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.7f, 0.7f), M("E53935", 0.3f)); B(t, new Vector3(0, 0.35f, 0), new Vector3(0.72f, 0.72f, 0.14f), M("FFC83A", 0.6f)); B(t, new Vector3(0, 0.35f, 0), new Vector3(0.14f, 0.72f, 0.72f), M("FFC83A", 0.6f)); break;
                case 18: Cone(t, new Vector3(0, 0, 0), new Vector3(0.5f, 0.9f, 0.5f), M("FF6A00", 0.3f)); Cy(t, new Vector3(0, 0.45f, 0), new Vector3(0.36f, 0.06f, 0.36f), M("FFFFFF", 0.3f)); break;
                case 19: S(t, new Vector3(0, 0.4f, 0), new Vector3(1.1f, 0.8f, 0.8f), M("2E7D32", 0.4f)); break;
                case 20: Cy(t, new Vector3(0, 0.5f, 0), new Vector3(0.1f, 0.5f, 0.1f), M("3A3F48", 0.5f)); B(t, new Vector3(0, 1.1f, 0), new Vector3(0.4f, 0.35f, 0.6f), M("1E5BD8", 0.4f)); break;
                case 21: Cy(t, new Vector3(0, 0.45f, 0), new Vector3(0.6f, 0.45f, 0.6f), M("4E5560", 0.5f, 0.4f)); Cy(t, new Vector3(0, 0.92f, 0), new Vector3(0.66f, 0.04f, 0.66f), M("3A3F48", 0.5f)); break;
                case 22: B(t, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.08f, 0.5f), M("A0642F", 0.2f)); B(t, new Vector3(0, 0.75f, 0.22f), new Vector3(0.5f, 0.6f, 0.06f), M("A0642F", 0.2f)); foreach (var x in new[] { -0.2f, 0.2f }) foreach (var z in new[] { -0.2f, 0.2f }) B(t, new Vector3(x, 0.22f, z), new Vector3(0.05f, 0.45f, 0.05f), M("6D4425", 0.2f)); break;
                default: Cy(t, new Vector3(0, 0.4f, 0), new Vector3(0.4f, 0.4f, 0.4f), M("E53935", 0.4f)); S(t, new Vector3(0, 0.82f, 0), 0.4f, M("E53935", 0.4f)); foreach (var x in new[] { -0.25f, 0.25f }) Cy(t, new Vector3(x, 0.5f, 0), new Vector3(0.12f, 0.08f, 0.12f), M("C62828", 0.4f), new Vector3(0, 0, 90)); break;
            }
            Parts.Layer = was;
            foreach (var c in t.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return t.gameObject;
        }

        // ---------------- the seeker's black square ----------------

        public static readonly int[] Seconds = { 5, 10, 15, 20, 25, 30, 35 };
        static readonly Vector3 RoomPos = new Vector3(-3000, 600, 0);   // high up and far away (below the water the game used to send him back)
        static GameObject seekRoom, seekUi;
        static TextMeshProUGUI seekTxt;
        static Vector3 backPos; static float backYaw; static bool fogWas;
        static float seekUntil = -1;
        public static bool Seeking => seekUntil > 0 || seekUi != null;
        public static int SeekDone { get; private set; }

        /// <summary>«Прятки: я вожу»: into the black square, pick how many seconds to wait.</summary>
        public static void StartSeek()
        {
            var pl = PlayerCtl.I;
            if (pl == null || Seeking) return;
            backPos = pl.transform.position; backYaw = pl.yaw;
            if (seekRoom == null)
            {
                seekRoom = new GameObject("SeekRoom");
                Parts.Layer = Layers.World;
                // a big closed black cube: the camera stays inside, nothing of the world is seen
                var black = M("000000", 0f);
                const float W = 34, H = 18;
                Lobby.Solid(PrimitiveType.Cube, RoomPos + new Vector3(0, -0.5f, 0), new Vector3(W + 2, 1, W + 2), M("060607", 0f), default, seekRoom.transform);
                Lobby.Solid(PrimitiveType.Cube, RoomPos + new Vector3(0, H + 0.5f, 0), new Vector3(W + 2, 1, W + 2), black, default, seekRoom.transform);
                foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    Lobby.Solid(PrimitiveType.Cube, RoomPos + d * (W / 2 + 0.5f) + Vector3.up * H / 2, d.x != 0 ? new Vector3(1, H + 2, W + 2) : new Vector3(W + 2, H + 2, 1), black, default, seekRoom.transform);
            }
            seekRoom.SetActive(true);
            fogWas = RenderSettings.fog; RenderSettings.fog = false;   // the fog would paint the black walls grey
            pl.Teleport(RoomPos + Vector3.up * 0.2f, 0);
            AudioFx.Play("portal");
            // the choice: 5 … 35 seconds
            var bg = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(0.05f, 0.05f, 0.08f, 0.92f), "SeekPanel", true);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1240, 330));
            seekUi = bg.gameObject;
            seekTxt = UIKit.Txt(bg.transform, "Ты водишь! Сколько секунд считать в чёрном квадрате?", 34, Color.white);
            UIKit.Place(seekTxt.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1180, 110));
            seekTxt.enableAutoSizing = true; seekTxt.fontSizeMin = 24; seekTxt.fontSizeMax = 60;
            for (int i = 0; i < Seconds.Length; i++)
            {
                int sec = Seconds[i];
                var b = UIKit.Button(bg.transform, sec + " СЕК", UIKit.Blue, () => Count(sec), 28, "Seek" + sec);
                UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-516 + i * 172, 40), new Vector2(160, 110));
            }
            seekUi.AddComponent<SeekRunner>();
        }

        public static void Count(int sec)
        {
            if (seekUi == null) return;
            seekUntil = Time.time + sec;
            foreach (Transform c in seekUi.transform) if (c.name.StartsWith("Seek")) c.gameObject.SetActive(false);
            AudioFx.Play("click");
            var on = Online.I;
            if (on != null && on.RoomWith != null) on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"KS|{on.Id}|{sec}");
        }

        public static void EndSeek()
        {
            var pl = PlayerCtl.I;
            seekUntil = -1;
            if (seekUi) Object.Destroy(seekUi);
            seekUi = null;
            if (seekRoom) seekRoom.SetActive(false);
            RenderSettings.fog = fogWas;
            if (pl) pl.Teleport(backPos + Vector3.up * 0.2f, backYaw);
            SeekDone++;
            AudioFx.Play("portal");
            Hud.Toast("Кто не спрятался — я не виноват! Ищи!", UIKit.Gold);
        }

        /// <summary>«KS|id|sec»: the friend is counting — hide!</summary>
        public static void OnSeekNet(string[] f)
        {
            var on = Online.I;
            if (on == null || f.Length < 3 || f[1] != on.RoomWith || !int.TryParse(f[2], out int sec)) return;
            Hud.Toast($"Друг водит и считает до {sec}! Прячься — можно превратиться в вещь (стикер «Прятки»)", UIKit.Lime);
            AudioFx.Play("note4");
        }

        class SeekRunner : MonoBehaviour
        {
            void Update()
            {
                if (seekUntil < 0) return;
                float left = seekUntil - Time.time;
                if (seekTxt) seekTxt.text = $"Считаю… {Mathf.CeilToInt(Mathf.Max(0, left))}";
                if (left <= 0) EndSeek();
            }
        }
    }
}
