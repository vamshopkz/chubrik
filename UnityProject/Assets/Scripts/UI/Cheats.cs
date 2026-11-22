using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// 18.11 Settings → «Читы»: bought with coins. Flying (50 000) — hold A to go up, B to go down.
    /// Endless coins (100 000) — buying no longer takes coins. The admin panel (30 000 000 000) — copy any thing in the
    /// game and put copies of it anywhere, move / stretch / shrink / turn them, and take them away again.
    /// </summary>
    public static class Cheats
    {
        public const long FlyPrice = 50_000, CoinsPrice = 100_000, AdminPrice = 30_000_000_000L;
        public static bool Flying => Save.P.cheatFly && Save.P.flyOn;
        public static bool EndlessCoins => Save.P.cheatCoins && Save.P.coinsOn;

        public static void Open()
        {
            Window.Show("ЧИТЫ", w =>
            {
                var P = Save.P;
                w.Text($"Монеты: {(EndlessCoins ? "∞" : UIKit.Num(P.coins))}", 30, UIKit.Gold);
                Row(w, "Полёт", "Держи A — вверх, B — вниз. Летай где хочешь", FlyPrice, P.cheatFly, P.flyOn,
                    () => { P.cheatFly = true; P.flyOn = true; }, on => P.flyOn = on, UIKit.Cyan);
                Row(w, "Бесконечные монеты", "Покупки больше не тратят монеты", CoinsPrice, P.cheatCoins, P.coinsOn,
                    () => { P.cheatCoins = true; P.coinsOn = true; }, on => P.coinsOn = on, UIKit.Gold);
                if (!P.cheatAdmin)
                    w.Row("Админ-панель", "Копируй любую вещь в игре и ставь её куда хочешь", UIKit.Num(AdminPrice) + " МОНЕТ", UIKit.Red, () => Buy(AdminPrice, () => P.cheatAdmin = true), P.coins >= AdminPrice || EndlessCoins, UIKit.Red);
                else
                    w.Row("Админ-панель", $"Поставлено вещей: {P.placed.Count}", "ОТКРЫТЬ", UIKit.Red, () => { Window.Close(); AdminPanel.Open(); }, true, UIKit.Red);
                w.Footer.text = "Читы покупаются один раз и включаются / выключаются здесь";
            }, 1000, 640);
        }

        static void Row(Window w, string name, string about, long price, bool owned, bool on, System.Action give, System.Action<bool> set, Color c)
        {
            if (!owned) w.Row(name, about, UIKit.Num(price) + " МОНЕТ", UIKit.Orange, () => Buy(price, give), Save.P.coins >= price || EndlessCoins, c);
            else w.Row(name + (on ? " — ВКЛЮЧЁН" : " — выключен"), about, on ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", on ? UIKit.Red : UIKit.Green, () => { set(!on); Save.Write(); AudioFx.Play("click"); Window.Refresh(); }, true, c);
        }

        public static void BuyFly() => Buy(FlyPrice, () => { Save.P.cheatFly = true; Save.P.flyOn = true; });
        public static void BuyCoins() => Buy(CoinsPrice, () => { Save.P.cheatCoins = true; Save.P.coinsOn = true; });
        public static void BuyAdmin() => Buy(AdminPrice, () => Save.P.cheatAdmin = true);

        static void Buy(long price, System.Action give)
        {
            if (!Save.Spend(price)) { AudioFx.Play("error"); Hud.Toast("Не хватает монет", UIKit.Red); return; }
            give();
            Save.Write();
            AudioFx.Play("buy");
            Hud.Toast("Чит куплен и включён!", UIKit.Lime);
            Window.Refresh();
        }
    }

    /// <summary>The admin panel: copy a thing (tap it), put copies (move, stretch, shrink, turn), take them away.</summary>
    public static class AdminPanel
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        public enum Mode { None, Copy, Place }
        public static Mode Now { get; private set; }
        public static GameObject Template;          // the copied thing (hidden)
        public static string TemplatePath;
        public static Placed Selected;
        static TextMeshProUGUI info;
        static RectTransform controls;
        static GameObject marker;

        public static void Open()
        {
            Close();
            var safe = UIRoot.Safe();
            var panel = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(1, 1, 1, 0.95f), "AdminPanel", true);
            UIKit.Place(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12 - safe.z, 0), new Vector2(430, 780));
            root = panel.gameObject;
            var P = panel.transform;
            var t = UIKit.Txt(P, "АДМИН-ПАНЕЛЬ", 32, UIKit.Red, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(330, 50));
            var x = UIKit.Button(P, "X", UIKit.Red, Close, 28, "Close");
            UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -8), new Vector2(64, 56));
            var copy = UIKit.Button(P, "СКОПИРОВАТЬ ВЕЩЬ", UIKit.Blue, StartCopy, 24, "CopyThing");
            UIKit.Place(copy.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -72), new Vector2(400, 70));
            var put = UIKit.Button(P, "ПОСТАВИТЬ ВЕЩЬ", UIKit.Green, StartPlace, 24, "PlaceThing");
            UIKit.Place(put.Bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(400, 70));
            info = UIKit.Txt(P, "", 21, Color.white, TextAlignmentOptions.TopLeft, false);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -228), new Vector2(400, 80));
            info.enableAutoSizing = true; info.fontSizeMin = 14; info.fontSizeMax = 21;
            controls = UIKit.Rect("Controls", P);
            UIKit.Place(controls, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(410, 460));
            BuildControls();
            root.AddComponent<Picker>();
            Now = Mode.None;
            Refresh();
            Menus.Unlock();
        }

        public static void Close()
        {
            if (root) Object.Destroy(root);
            root = null; Now = Mode.None; Select(null);
        }

        static void BuildControls()
        {
            var acts = new (string label, Color c, System.Action a)[]
            {
                ("ВПЕРЁД", UIKit.Blue, () => Move(Vector3.forward)), ("НАЗАД", UIKit.Blue, () => Move(Vector3.back)), ("ВЛЕВО", UIKit.Blue, () => Move(Vector3.left)),
                ("ВПРАВО", UIKit.Blue, () => Move(Vector3.right)), ("ВЫШЕ", UIKit.Blue, () => Move(Vector3.up)), ("НИЖЕ", UIKit.Blue, () => Move(Vector3.down)),
                ("БОЛЬШЕ", UIKit.Green, () => Scale(new Vector3(1.1f, 1.1f, 1.1f))), ("МЕНЬШЕ", UIKit.Green, () => Scale(new Vector3(0.91f, 0.91f, 0.91f))), ("ПОВЕРНУТЬ", UIKit.Violet, () => Turn(15)),
                ("ДЛИННЕЕ", UIKit.Orange, () => Scale(new Vector3(1, 1, 1.15f))), ("КОРОЧЕ", UIKit.Orange, () => Scale(new Vector3(1, 1, 0.87f))), ("ВЫШЕ РОСТОМ", UIKit.Orange, () => Scale(new Vector3(1, 1.15f, 1))),
                ("ШИРЕ", UIKit.Orange, () => Scale(new Vector3(1.15f, 1, 1))), ("УЖЕ", UIKit.Orange, () => Scale(new Vector3(0.87f, 1, 1))), ("НИЖЕ РОСТОМ", UIKit.Orange, () => Scale(new Vector3(1, 0.87f, 1))),
            };
            for (int i = 0; i < acts.Length; i++)
            {
                var (label, c, a) = acts[i];
                var b = UIKit.Button(controls, label, c, a, 17, "Ctl " + label);
                UIKit.Place(b.Bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(2 + (i % 3) * 137, -2 - (i / 3) * 66), new Vector2(132, 60));
                b.FireOnDown = true;
                b.gameObject.AddComponent<Repeat>().btn = b;
            }
            var del = UIKit.Button(controls, "УБРАТЬ ИЗ ИГРЫ", UIKit.Red, DeleteSelected, 22, "Delete");
            UIKit.Place(del.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-100, 2), new Vector2(200, 64));
            var more = UIKit.Button(controls, "ЕЩЁ ОДНУ", UIKit.Green, () => { if (Template) PlaceNew(); }, 22, "PlaceMore");
            UIKit.Place(more.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(105, 2), new Vector2(196, 64));
        }

        static void Refresh()
        {
            if (!info) return;
            controls.gameObject.SetActive(Now == Mode.Place && Selected != null);
            string have = Template ? $"Скопировано: «{Nice(Template.name)}»" : "Ещё ничего не скопировано";
            info.text = Now == Mode.Copy ? "Нажми на любую вещь в игре — она скопируется" :
                        Now == Mode.Place ? (Selected != null ? "Двигай, растягивай, поворачивай. Нажми на другую поставленную вещь — выберешь её" : "Нажми на поставленную вещь — её можно двигать или убрать") + "\n" + have :
                        have;
        }

        static string Nice(string n) => n.Replace("(Clone)", "").Replace("Placed ", "").Trim();

        public static void StartCopy() { Now = Mode.Copy; Select(null); AudioFx.Play("click"); Refresh(); }

        public static void StartPlace()
        {
            Now = Mode.Place;
            if (Template == null) { Hud.Toast("Сначала скопируй вещь", UIKit.Orange); Refresh(); return; }
            PlaceNew();
        }

        // ---------------- picking things with a tap ----------------

        /// <summary>The thing under a tap: the hit collider climbed up to a sensible whole (not the whole world).</summary>
        public static GameObject ThingAt(Vector2 screen)
        {
            var cam = PlayerCtl.I ? PlayerCtl.I.Cam : Camera.main;
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(screen);
            int mask = ~((1 << Layers.Player) | (1 << Layers.Preview) | (1 << Layers.Barrier) | (1 << 5));
            if (!Physics.Raycast(ray, out var hit, 150, mask, QueryTriggerInteraction.Ignore)) return null;
            return Whole(hit.collider.transform);
        }

        static Transform TopRoot(Transform t) { while (t.parent != null) t = t.parent; return t; }

        static GameObject Whole(Transform t)
        {
            var placed = t.GetComponentInParent<Placed>();
            if (placed) return placed.gameObject;
            var top = TopRoot(t);
            while (t.parent != null && t.parent != top)
            {
                var b = Bounds(t.parent);
                if (b.size.x > 10 || b.size.z > 10 || b.size.y > 12) break;
                t = t.parent;
            }
            return t.gameObject;
        }

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 100);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public static void Pick(GameObject go)
        {
            if (go == null) return;
            if (Now == Mode.Copy)
            {
                if (go.transform.parent == null) { Hud.Toast("Это слишком большое — выбери вещь поменьше", UIKit.Orange); return; }
                var src = go.GetComponent<Placed>() ? go.GetComponent<Placed>().source : go.transform;
                if (src == null) return;
                Copy(src);
                return;
            }
            if (Now == Mode.Place)
            {
                var p = go.GetComponent<Placed>();
                if (p != null) { Select(p); AudioFx.Play("click"); }
                else Hud.Toast("Это не поставленная вещь. Поставь новую — «ЕЩЁ ОДНУ»", UIKit.Muted);
            }
        }

        static void Copy(Transform src)
        {
            if (Template) Object.Destroy(Template);
            Template = MakeClone(src, null);
            Template.SetActive(false);
            TemplatePath = PathOf(src);
            AudioFx.Play("token");
            Hud.Toast($"Скопировано: «{Nice(src.name)}». Теперь «ПОСТАВИТЬ ВЕЩЬ»", UIKit.Lime);
            Now = Mode.None;
            Refresh();
        }

        /// <summary>A copy without the original's behaviour (no walking enemies, no cages that count): only how it looks and its solid shape.</summary>
        static GameObject MakeClone(Transform src, Transform parent)
        {
            var go = Object.Instantiate(src.gameObject, parent);
            go.name = "Placed " + Nice(src.name);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is Spin || mb is Pulse || mb is Hue || mb is Billboard || mb is TMP_Text) continue;
                Object.DestroyImmediate(mb);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) if (c.isTrigger) Object.DestroyImmediate(c);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            return go;
        }

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            while (t.parent != null) { parts.Insert(0, t.GetSiblingIndex().ToString()); t = t.parent; }
            parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        static Transform FromPath(string path)
        {
            var parts = path.Split('/');
            var top = GameObject.Find("/" + parts[0]);
            if (top == null) return null;
            var t = top.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var k) || k >= t.childCount) return null;
                t = t.GetChild(k);
            }
            return t;
        }

        // ---------------- putting copies ----------------

        static void PlaceNew()
        {
            var pl = PlayerCtl.I;
            if (Template == null || pl == null) return;
            var src = FromPath(TemplatePath);
            var f = pl.transform.forward; f.y = 0; f.Normalize();
            var pos = pl.transform.position + f * 3f;
            if (Physics.Raycast(pos + Vector3.up * 4, Vector3.down, out var hit, 10, Layers.SolidMask, QueryTriggerInteraction.Ignore)) pos = hit.point;
            var go = Object.Instantiate(Template, Holder);
            go.SetActive(true);
            go.name = Template.name;
            var b = Bounds(go.transform);
            go.transform.position = pos + (go.transform.position - new Vector3(b.center.x, b.min.y, b.center.z));
            var p = go.AddComponent<Placed>();
            p.source = src; p.path = TemplatePath;
            p.Save();
            restored.Add(p.id);
            Select(p);
            Effects.Emit("stardust", pos + Vector3.up, 16, 0.6f);
            AudioFx.Play("magic");
            Now = Mode.Place;
            Refresh();
        }

        static void Select(Placed p)
        {
            Selected = p;
            if (marker == null && p != null)
            {
                marker = new GameObject("AdminMarker");
                Parts.Layer = 0;
                Parts.Cone(marker.transform, Vector3.zero, new Vector3(0.5f, 0.6f, 0.5f), Parts.G("35E0FF", 2.5f), new Vector3(180, 0, 0));
                marker.AddComponent<Spin>();
            }
            if (marker) marker.SetActive(p != null);
            Refresh();
        }

        static Vector3 CamDir(Vector3 d)
        {
            var yaw = PlayerCtl.I ? PlayerCtl.I.yaw : 0;
            return d.y != 0 ? d : Quaternion.Euler(0, yaw, 0) * d;
        }

        public static void Move(Vector3 d) { if (Selected == null) return; Selected.transform.position += CamDir(d) * 0.25f; Selected.Save(); }
        public static void Turn(float a) { if (Selected == null) return; Selected.transform.Rotate(0, a, 0, Space.World); Selected.Save(); }
        public static void Scale(Vector3 k)
        {
            if (Selected == null) return;
            var s = Selected.transform.localScale;
            s = new Vector3(Mathf.Clamp(s.x * k.x, 0.05f, 60), Mathf.Clamp(s.y * k.y, 0.05f, 60), Mathf.Clamp(s.z * k.z, 0.05f, 60));
            Selected.transform.localScale = s;
            Selected.Save();
        }

        public static void DeleteSelected()
        {
            if (Selected == null) return;
            var p = Selected;
            Select(null);
            p.Forget();
            Effects.Emit("smoke", p.transform.position + Vector3.up, 14, 0.6f);
            AudioFx.Play("portal");
            Object.Destroy(p.gameObject);
            Hud.Toast("Вещь убрана из игры", UIKit.Orange);
        }

        /// <summary>All the copies live here (not inside the lobby or a world, so the originals' places never shift).</summary>
        static Transform holder;
        static Transform Holder { get { if (holder == null) holder = new GameObject("AdminPlaced").transform; return holder; } }
        static readonly HashSet<string> restored = new HashSet<string>();
        public static int Restored => restored.Count;
        /// <summary>22.11 soft restart: the new profile's copies are put back with the new lobby.</summary>
        internal static void ResetStatics() { restored.Clear(); Now = Mode.None; TemplatePath = null; }

        /// <summary>Puts back the saved copies whose originals exist now (called when the lobby or a world is built).</summary>
        public static void Restore()
        {
            foreach (var line in new List<string>(Save.P.placed))
            {
                var f = line.Split('|');
                if (f.Length < 12 || restored.Contains(f[0])) continue;
                var src = FromPath(f[1]);
                if (src == null) continue;
                restored.Add(f[0]);
                var I = CultureInfo.InvariantCulture;
                var go = MakeClone(src, Holder);
                go.transform.SetPositionAndRotation(new Vector3(float.Parse(f[2], I), float.Parse(f[3], I), float.Parse(f[4], I)), Quaternion.Euler(float.Parse(f[5], I), float.Parse(f[6], I), float.Parse(f[7], I)));
                go.transform.localScale = new Vector3(float.Parse(f[8], I), float.Parse(f[9], I), float.Parse(f[10], I));
                var p = go.AddComponent<Placed>();
                p.id = f[0]; p.path = f[1]; p.source = src;
                go.SetActive(true);
            }
        }

        /// <summary>A tap (not a drag) on the game picks a thing while copying or placing.</summary>
        class Picker : MonoBehaviour
        {
            Vector2 down; float downT; bool pressed;
            void Update()
            {
                if (marker && Selected) marker.transform.position = Bounds(Selected.transform).max.y * Vector3.up + new Vector3(Selected.transform.position.x, 0.6f, Selected.transform.position.z);
                if (Now == Mode.None || Menus.AnyOpen) return;
                bool isDown = Input.GetMouseButtonDown(0), isUp = Input.GetMouseButtonUp(0);
                if (isDown) { pressed = !OverUi(Input.mousePosition); down = Input.mousePosition; downT = Time.unscaledTime; }
                if (isUp && pressed)
                {
                    pressed = false;
                    if (((Vector2)Input.mousePosition - down).magnitude < 20 && Time.unscaledTime - downT < 0.6f) Pick(ThingAt(Input.mousePosition));
                }
            }

            static readonly List<RaycastResult> hits = new List<RaycastResult>();
            /// <summary>Over a button or a panel? (the phone's invisible camera-drag area doesn't count: taps there pick things)</summary>
            static bool OverUi(Vector2 at)
            {
                if (EventSystem.current == null) return false;
                hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = at }, hits);
                return hits.Count > 0 && hits[0].gameObject.name != "LookArea";
            }
        }

        class Repeat : MonoBehaviour
        {
            public UIButton btn;
            float t;
            void Update()
            {
                if (btn == null || !btn.Held) { t = 0; return; }
                t += Time.unscaledDeltaTime;
                if (t > 0.35f) { t -= 0.07f; btn.OnClick?.Invoke(); }
            }
        }
    }

    /// <summary>A thing put by the admin panel (saved: where it is, how big, which original it copies).</summary>
    public class Placed : MonoBehaviour
    {
        public string id, path;
        public Transform source;

        public void Save()
        {
            if (string.IsNullOrEmpty(id)) id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var I = CultureInfo.InvariantCulture;
            var p = transform.position; var r = transform.eulerAngles; var s = transform.localScale;
            var line = string.Format(I, "{0}|{1}|{2:0.###}|{3:0.###}|{4:0.###}|{5:0.#}|{6:0.#}|{7:0.#}|{8:0.###}|{9:0.###}|{10:0.###}|1", id, path, p.x, p.y, p.z, r.x, r.y, r.z, s.x, s.y, s.z);
            Forget(false);
            Chubrik.Save.P.placed.Add(line);
            Chubrik.Save.Write();
        }

        public void Forget(bool write = true)
        {
            Chubrik.Save.P.placed.RemoveAll(l => l.StartsWith(id + "|"));
            if (write) Chubrik.Save.Write();
        }
    }
}
