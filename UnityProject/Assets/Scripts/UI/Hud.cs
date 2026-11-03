using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>In-game overlay: coins, tokens, hearts, menu button, touch controls, enter button, toasts and screen effects.</summary>
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        public static bool Touch => Application.isMobilePlatform || Boot.Has("-touch");
        public static bool JumpButtonHeld => I != null && I.jumpBtn != null && I.jumpBtn.Held;
        RectTransform root, touchRoot, heartsRow;
        TextMeshProUGUI coins, tokens, keys, friends, toast, hint, navTxt;
        Image flash, crack;
        RectTransform glitch;
        UIButton menuBtn, enterBtn, jumpBtn, hitBtn, emoteBtn;
        readonly List<Image> hearts = new List<Image>();
        float toastUntil, flashA, crackT, glitchT, heartT;
        int shownCoins = -1, shownTokens = -1, shownKeys = -1, shownHearts = -1, shownFull = -1;
        public static Sprite CoinSprite, TokenSprite, HeartSprite, KeySprite;

        public static void Create()
        {
            I = new GameObject("Hud").AddComponent<Hud>();
            I.Build();
        }

        static Sprite SpriteOf(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

        TextMeshProUGUI T(string s, float size, Color c, TextAlignmentOptions a, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sz)
        {
            var t = UIKit.Txt(root, s, size, c, a);
            UIKit.Place(t.rectTransform, anchor, pivot, pos, sz);
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        void Build()
        {
            root = UIRoot.HudLayer;
            var safe = UIRoot.Safe();
            CoinSprite = UIKit.Circle;
            TokenSprite = SpriteOf(Patterns.Shape("star"));
            HeartSprite = SpriteOf(Patterns.Shape("heart"));
            KeySprite = SpriteOf(KeyTex());

            flash = UIKit.Img(root, UIKit.White, new Color(1, 0, 0, 0), "Flash");
            UIKit.Fill(flash.rectTransform);
            crack = UIKit.Img(root, null, new Color(1, 1, 1, 0), "Crack");
            crack.sprite = SpriteOf(CrackTex());
            UIKit.Fill(crack.rectTransform);
            glitch = UIKit.Rect("Glitch", root);
            UIKit.Fill(glitch);
            for (int i = 0; i < 14; i++)
            {
                var b = UIKit.Img(glitch, UIKit.White, Color.HSVToRGB(Random.value, 0.9f, 1f), "Bar");
                UIKit.Place(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700, 20));
            }
            glitch.gameObject.SetActive(false);

            // coins / tokens / hearts (top left)
            var bar = UIKit.Img(root, UIKit.Pill, new Color(0.05f, 0.15f, 0.35f, 0.55f), "TopBar");
            UIKit.Place(bar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + safe.x, -14 - safe.w), new Vector2(560, 64));
            var ci = UIKit.Img(bar.transform, CoinSprite, UIKit.Gold, "Coin");
            UIKit.Place(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(46, 46));
            var ci2 = UIKit.Img(ci.transform, CoinSprite, Parts.Hex("FFB300"), "In");
            UIKit.Place(ci2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
            coins = UIKit.Txt(bar.transform, "0", 36, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(coins.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(170, 56));
            var ti = UIKit.Img(bar.transform, TokenSprite, UIKit.Violet, "Token");
            UIKit.Place(ti.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(246, 0), new Vector2(50, 50));
            tokens = UIKit.Txt(bar.transform, "0", 36, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(tokens.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(302, 0), new Vector2(110, 56));
            var ki = UIKit.Img(bar.transform, KeySprite, UIKit.Gold, "Key");
            UIKit.Place(ki.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(410, 0), new Vector2(52, 52));
            keys = UIKit.Txt(bar.transform, "0", 36, Color.white, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(keys.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(466, 0), new Vector2(90, 56));
            heartsRow = UIKit.Rect("Hearts", root);
            UIKit.Place(heartsRow, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22 + safe.x, -86 - safe.w), new Vector2(400, 50));
            friends = T("", 26, UIKit.Gold, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24 + safe.x, -172 - safe.w), new Vector2(520, 34));
            navTxt = T("", 24, Parts.Hex("7DFF9A"), TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24 + safe.x, -140 - safe.w), new Vector2(500, 34));

            menuBtn = UIKit.Button(root, "МЕНЮ", UIKit.Blue, () => Menus.Pause(), 26);
            UIKit.Place(menuBtn.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16 - safe.z, -14 - safe.w), new Vector2(150, 66));

            toast = T("", 32, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100 - safe.w), new Vector2(1300, 50));
            hint = T(Touch ? "" : "WASD — бег · ПРОБЕЛ — прыжок (держи с крыльями — полёт) · F — удар · Q — эмоция · E — войти · мышь — камера",
                20, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10 + safe.y), new Vector2(1500, 30));

            enterBtn = UIKit.Button(root, "ВОЙТИ", UIKit.Orange, () => Zones.Interact(), 30, "Enter");
            UIKit.Place(enterBtn.Bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120 + safe.y), new Vector2(520, 84));
            enterBtn.gameObject.SetActive(false);

            // touch controls
            touchRoot = UIKit.Fill(UIKit.Rect("Touch", root));
            var lookArea = UIKit.Img(touchRoot, UIKit.White, new Color(0, 0, 0, 0.001f), "LookArea", true);
            UIKit.Place(lookArea.rectTransform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(1000, 760));
            lookArea.rectTransform.anchorMin = new Vector2(0.38f, 0); lookArea.rectTransform.anchorMax = new Vector2(1, 0.86f);
            lookArea.rectTransform.offsetMin = Vector2.zero; lookArea.rectTransform.offsetMax = Vector2.zero;
            lookArea.gameObject.AddComponent<LookArea>();
            var joyZone = UIKit.Img(touchRoot, UIKit.White, new Color(0, 0, 0, 0.001f), "JoyZone", true);
            joyZone.rectTransform.anchorMin = new Vector2(0, 0); joyZone.rectTransform.anchorMax = new Vector2(0.38f, 0.75f);
            joyZone.rectTransform.offsetMin = Vector2.zero; joyZone.rectTransform.offsetMax = Vector2.zero;
            var jb = UIKit.Img(joyZone.transform, UIKit.Ring, new Color(1, 1, 1, 0.75f), "Base");
            UIKit.Place(jb.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(200 + safe.x, 190 + safe.y), new Vector2(240, 240));
            var jk = UIKit.Img(jb.transform, UIKit.Circle, new Color(1, 1, 1, 0.85f), "Knob");
            UIKit.Place(jk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
            joyZone.gameObject.AddComponent<Joystick>().Init(jb.rectTransform, jk.rectTransform);

            jumpBtn = UIKit.Round(touchRoot, "ПРЫЖОК", UIKit.Green, () => PlayerCtl.I.QueueJump(), 200, 30);
            jumpBtn.FireOnDown = true;
            UIKit.Place(jumpBtn.Bg.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-170 - safe.z, 170 + safe.y), new Vector2(200, 200));
            hitBtn = UIKit.Round(touchRoot, "УДАР", UIKit.Red, () => PlayerCtl.I.Attack(), 130, 26);
            hitBtn.FireOnDown = true;
            UIKit.Place(hitBtn.Bg.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-370 - safe.z, 120 + safe.y), new Vector2(130, 130));
            emoteBtn = UIKit.Round(touchRoot, "ЭМОЦИЯ", UIKit.Violet, () => PlayerCtl.I.Emote(), 120, 22);
            UIKit.Place(emoteBtn.Bg.rectTransform, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-300 - safe.z, 300 + safe.y), new Vector2(120, 120));
            touchRoot.gameObject.SetActive(Touch);
            // the enter button must stay above the invisible camera/joystick areas, or they swallow the tap on phones
            enterBtn.transform.SetAsLastSibling();
        }

        /// <summary>Autotest: show the phone controls and «ВОЙТИ» on a computer.</summary>
        public bool ForceTouch;
        public string TapReport;

        /// <summary>Autotest: does a tap in the middle of «ВОЙТИ» reach the button itself (not the camera area)?</summary>
        public bool EnterButtonGetsTap()
        {
            var pos = RectTransformUtility.WorldToScreenPoint(null, enterBtn.Bg.rectTransform.TransformPoint(enterBtn.Bg.rectTransform.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, hits);
            TapReport = $"at {pos}, hits: " + string.Join(", ", hits.ConvertAll(h => h.gameObject.name));
            return hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UIButton>() == enterBtn;
        }

        static Texture2D KeyTex()
        {
            var cv = new Canvas2(64, 64, new Color(1, 1, 1, 0));
            cv.Ring(20, 32, 11, 7, Color.white);
            cv.Rect(30, 29, 28, 7, Color.white);
            cv.Rect(46, 20, 5, 10, Color.white);
            cv.Rect(54, 20, 5, 10, Color.white);
            return cv.Tex(false);
        }

        static Texture2D CrackTex()
        {
            var cv = new Canvas2(512, 288, new Color(1, 1, 1, 0));
            var r = new Rng(9);
            var c = new Vector2(330, 160);
            for (int k = 0; k < 14; k++)
            {
                var p = c; float a = k * Mathf.PI * 2 / 14 + r.F(-0.2f, 0.2f);
                for (int s = 0; s < 7; s++)
                {
                    var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r.F(20, 45);
                    a += r.F(-0.5f, 0.5f);
                    for (int i = 0; i <= 20; i++)
                    {
                        var m = Vector2.Lerp(p, q, i / 20f);
                        int x = (int)m.x, y = (int)m.y;
                        if (x >= 0 && x < 512 && y >= 0 && y < 288) { cv.px[y * 512 + x] = new Color(1, 1, 1, 0.95f); if (x + 1 < 512) cv.px[y * 512 + x + 1] = new Color(0.8f, 0.9f, 1f, 0.6f); }
                    }
                    p = q;
                }
            }
            for (int ring = 1; ring <= 3; ring++)
                for (int i = 0; i < 360; i += 2)
                {
                    float a = i * Mathf.Deg2Rad, rr = ring * 38 + Mathf.Sin(i * 0.3f) * 6;
                    int x = (int)(c.x + Mathf.Cos(a) * rr), y = (int)(c.y + Mathf.Sin(a) * rr);
                    if (x >= 0 && x < 512 && y >= 0 && y < 288 && r.Chance(0.6f)) cv.px[y * 512 + x] = new Color(1, 1, 1, 0.8f);
                }
            return cv.Tex(false, false, FilterMode.Bilinear);
        }

        void Update()
        {
            var P = Save.P;
            if (P.coins != shownCoins) { shownCoins = P.coins; coins.text = UIKit.Num(P.coins); }
            if (P.tokens != shownTokens) { shownTokens = P.tokens; tokens.text = P.tokens.ToString(); }
            if (P.keys != shownKeys) { shownKeys = P.keys; keys.text = P.keys.ToString(); }
            friends.text = Worlds.Current == 2 ? $"Друзья спасены: {Rescue.Rescued("w2")} из {Rescue.Total("w2")}" : (P.rescued.Count > 0 ? $"Спасённых друзей: {P.rescued.Count}" : "");
            if (P.maxHearts != shownHearts) RebuildHearts();
            int full = Worlds.Current == 2 && PlayerCtl.I ? Mathf.Clamp(PlayerCtl.I.Hearts, 0, P.maxHearts) : P.maxHearts;
            if (full != shownFull)
            {
                shownFull = full;
                for (int i = 0; i < hearts.Count; i++) hearts[i].color = i < full ? UIKit.Red : new Color(0.15f, 0.2f, 0.35f, 0.6f);
            }
            if (heartT > 0)
            {
                heartT -= Time.deltaTime;
                heartsRow.anchoredPosition = new Vector2(22 + UIRoot.Safe().x + Mathf.Sin(heartT * 60) * 6 * heartT, heartsRow.anchoredPosition.y);
            }
            bool menus = Menus.AnyOpen;
            root.gameObject.SetActive(!Menus.MainOpen);
            touchRoot.gameObject.SetActive((Touch || ForceTouch) && !menus);
            hint.gameObject.SetActive(!menus && Time.time < 60f);
            menuBtn.Bg.color = Updater.HasUpdate ? UIKit.Orange : UIKit.Blue;
            menuBtn.Set(Updater.HasUpdate ? "ОБНОВИТЬ" : "МЕНЮ");

            var z = menus ? null : Zones.Near();
            enterBtn.gameObject.SetActive(z != null || ForceTouch && !menus);
            if (z != null) enterBtn.Set((Touch ? "ВОЙТИ: " : "ВОЙТИ (E): ") + z.label);

            var pk = Pickups.I;
            if (pk != null && P.navigator && P.navigatorOn && pk.NearestCoin.HasValue) navTxt.text = $"Навигатор: сокровище в {Mathf.RoundToInt(pk.NearestCoinDist)} м";
            else navTxt.text = "";

            if (Time.time > toastUntil && toast.text != "") toast.text = "";
            flashA = Mathf.MoveTowards(flashA, 0, Time.deltaTime * 1.5f);
            var fc = flash.color; fc.a = flashA; flash.color = fc;
            crackT -= Time.deltaTime;
            crack.color = new Color(1, 1, 1, Mathf.Clamp01(crackT));
            glitchT -= Time.deltaTime;
            glitch.gameObject.SetActive(glitchT > 0);
            if (glitchT > 0)
                foreach (RectTransform b in glitch)
                {
                    b.anchoredPosition = new Vector2(Random.Range(-200f, 200f), Random.Range(-450f, 450f));
                    b.sizeDelta = new Vector2(Random.Range(300f, 1800f), Random.Range(6f, 40f));
                    b.GetComponent<Image>().color = Color.HSVToRGB(Random.value, 0.9f, 1f) * new Color(1, 1, 1, 0.7f);
                }
        }

        void RebuildHearts()
        {
            shownHearts = Save.P.maxHearts;
            shownFull = -1;
            foreach (var h in hearts) Destroy(h.gameObject);
            hearts.Clear();
            for (int i = 0; i < shownHearts; i++)
            {
                var h = UIKit.Img(heartsRow, HeartSprite, UIKit.Red, "Heart");
                UIKit.Place(h.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 52, 0), new Vector2(48, 48));
                var sh = h.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.5f); sh.effectDistance = new Vector2(2, -2);
                hearts.Add(h);
            }
        }

        public static void Toast(string text, Color c)
        {
            if (I == null) return;
            I.toast.text = text; I.toast.color = c;
            I.toastUntil = Time.time + 3.5f;
        }

        public static void Flash(Color c) { if (I == null) return; I.flash.color = c; I.flashA = c.a; }
        public static void Crack() { if (I != null) I.crackT = 1.6f; }
        public static void Glitch() { if (I != null) I.glitchT = 0.6f; }
        public static void HeartFlash() { if (I != null) I.heartT = 0.6f; }
    }
}
