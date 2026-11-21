using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 21.11 «Обучение» (Settings → СМОТРЕТЬ): a ~2.5-minute tutorial video with a voice (18 scenes), filmed in the
    /// real lobby by its own camera. A DEMO chubrik puppet (ChubrikModel + ChubrikAnim, not the player) shows every
    /// move and every place; captions, key chips, arrows and circles on the screen point at things, mock windows show
    /// the menus. Everything it makes in the world lives under one «TutorialSet» object that <see cref="Stop"/> removes;
    /// the player is frozen and hidden (the tutorial camera does not draw the player layer), saves are never touched.
    /// Voices: Resources/Sounds/tutorial/tNN.wav (macOS «say», Milena).
    /// </summary>
    public static class Tutorial
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        public static int Scene { get; private set; } = -1;
        public const int Scenes = 18;
        /// <summary>Speeds the whole tutorial up (moves, waits, voices); tests set 4 (≈40 s).</summary>
        public static float Speed = 1f;
        const float Pitch = 1.08f;
        static float D => Mathf.Min(Time.unscaledDeltaTime, 0.1f) * Mathf.Max(0.05f, Speed);

        public static void Play()
        {
            Stop();
            if (Cartoon.IsOpen) Cartoon.Stop();
            if (UIRoot.Open != null) Window.Close();
            Menus.Unlock();
            if (PlayerCtl.I) PlayerCtl.I.Frozen = true;
            var bg = UIKit.Img(UIRoot.TopLayer, UIKit.White, Color.black, "Tutorial", true);
            UIKit.Fill(bg.rectTransform);
            root = bg.gameObject;
            root.AddComponent<Show>();
        }

        public static void Stop()
        {
            if (root) UnityEngine.Object.Destroy(root);
            root = null; Scene = -1;
            Restore();
            if (PlayerCtl.I && !Menus.AnyOpen) PlayerCtl.I.Frozen = false;
        }

        // ---------------- what the tutorial changes in the game (put back by Restore) ----------------
        static bool saved, envSwapped;
        static GameObject set;
        static Camera gameCam, tutCam;
        static int gameMask;
        static bool gamePost;
        static string gameTag;

        static void Grab(GameObject newSet, Camera cam)
        {
            Restore();
            saved = true; set = newSet; tutCam = cam;
            // filmed in the lobby: if we are in another world, the lobby's sky and fog for the time being
            envSwapped = Worlds.Current != 1;
            if (envSwapped) World3.Env(1);
            gameCam = PlayerCtl.I ? PlayerCtl.I.Cam : null;
            if (gameCam)
            {
                // the game camera keeps running but draws nothing; ours becomes Camera.main so billboards and effects face it
                gameMask = gameCam.cullingMask; gameCam.cullingMask = 0;
                var d = gameCam.GetUniversalAdditionalCameraData();
                gamePost = d.renderPostProcessing; d.renderPostProcessing = false;
                gameTag = gameCam.tag; gameCam.tag = "Untagged";
            }
            if (cam) cam.tag = "MainCamera";
            AudioFx.SetMusic(false);
        }

        static void Restore()
        {
            if (tutCam) { tutCam.tag = "Untagged"; tutCam.enabled = false; }
            tutCam = null;
            if (set) UnityEngine.Object.Destroy(set);
            set = null;
            if (!saved) return;
            saved = false;
            if (gameCam)
            {
                gameCam.cullingMask = gameMask;
                gameCam.GetUniversalAdditionalCameraData().renderPostProcessing = gamePost;
                gameCam.tag = string.IsNullOrEmpty(gameTag) ? "MainCamera" : gameTag;
            }
            gameCam = null;
            if (envSwapped) World3.Env(Worlds.Current);
            envSwapped = false;
            AudioFx.SetMusic(Save.P.music);
        }

        // ---------------- a light background tune (synthesized once) ----------------
        static AudioClip tune;
        static AudioClip Tune()
        {
            if (tune) return tune;
            const int SR = 22050;
            float beat = 60f / 116f;
            const int bars = 8;
            int n = (int)(SR * beat * 4 * bars);
            var d = new float[n];
            void Note(float start, float f, float amp, float decay, float len)
            {
                int s0 = (int)(start * SR), cnt = (int)(len * SR);
                for (int i = 0; i < cnt; i++)
                {
                    float t = (float)i / SR;
                    float w = Mathf.Sin(2 * Mathf.PI * f * t) + 0.25f * Mathf.Sin(4 * Mathf.PI * f * t);
                    d[(s0 + i) % n] += w * amp * Mathf.Exp(-t * decay) * Mathf.Min(1, t * 250);
                }
            }
            int[][] chords = { new[] { 0, 4, 7 }, new[] { -3, 0, 4 }, new[] { -7, -3, 0 }, new[] { -5, -1, 2 } };   // C Am F G
            for (int b = 0; b < bars; b++)
            {
                var c = chords[b % 4];
                for (int s = 0; s < 8; s++)
                {
                    int note = c[s % 3] + (s >= 3 && s < 6 ? 12 : 0);
                    Note((b * 4 + s * 0.5f) * beat, 261.63f * Mathf.Pow(2, note / 12f), 0.12f, 5f, 0.45f);
                }
                Note(b * 4 * beat, 65.41f * Mathf.Pow(2, c[0] / 12f), 0.2f, 1.6f, beat * 3.8f);
                Note((b * 4 + 2) * beat, 65.41f * Mathf.Pow(2, (c[0] + 7) / 12f), 0.14f, 2.2f, beat * 1.8f);
            }
            float peak = 0.001f;
            foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < n; i++) d[i] *= 0.7f / peak;
            tune = AudioClip.Create("tutorial_tune", n, 1, SR, false);
            tune.SetData(d, 0);
            return tune;
        }

        // ---------------- a down-pointing triangle for the on-screen arrows ----------------
        static Sprite tri;
        static Sprite Tri()
        {
            if (tri) return tri;
            const int N = 64;
            var tx = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TutArrow" };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float fy = (y + 0.5f) / N, dx = Mathf.Abs((x + 0.5f) / N - 0.5f);
                    float a = Mathf.Clamp01((fy * 0.5f - dx) * N * 0.8f) * Mathf.Clamp01((1 - fy) * N * 0.8f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tx.SetPixels32(px); tx.Apply();
            tri = Sprite.Create(tx, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100);
            return tri;
        }

        /// <summary>Runs a function every frame with the tutorial's own (Speed-scaled) clock; removes itself after <c>life</c>.</summary>
        class Ticker : MonoBehaviour
        {
            public Action<float> f;
            public float life = -1;
            float t;
            void Update()
            {
                t += D;
                if (life >= 0 && t >= life) { f?.Invoke(life); Destroy(this); return; }
                f?.Invoke(t);
            }
        }

        static Ticker Tick(Component c, Action<float> f, float life = -1)
        {
            var k = c.gameObject.AddComponent<Ticker>();
            k.f = f; k.life = life;
            return k;
        }

        /// <summary>The demo chubrik: a model and its animator, moved by script.</summary>
        class Pup
        {
            public Transform t;
            public ChubrikModel m;
            public ChubrikAnim a;
            public Vector3 pos;
            public float yaw, scale = 1;
            public Vector3 Fwd => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            public Vector3 Chest => pos + Vector3.up * 0.8f * scale;
            public void Face(Vector3 p) { var d = p - pos; d.y = 0; if (d.sqrMagnitude > 1e-4f) yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
            public void Apply()
            {
                t.position = pos;
                t.rotation = Quaternion.Euler(0, yaw, 0);
                t.localScale = Vector3.one * Mathf.Max(0.001f, scale);
            }
        }

        class Pin { public RectTransform rt; public Func<Vector3> at; public float radius; }

        class Coin { public Transform t; public bool token; }

        /// <summary>A flat demo platform (no collider) in lane coordinates: the puppet can stand on it.</summary>
        struct Plat { public float x0, x1, s0, s1, top; }

        class Show : MonoBehaviour
        {
            GameObject setGo;
            Transform W, props;
            Camera cam;
            RenderTexture rt;
            RawImage view;
            RectTransform ov, keyRow, capRt, hudBar;
            readonly List<RectTransform> chips = new List<RectTransform>();
            GameObject subBar;
            TextMeshProUGUI sub, title, title2, coinTxt, tokTxt;
            float titleT = -1;
            AudioSource vsrc, mus;
            bool talking;
            float clock, Dt;
            Pup hero, pal;
            Look heroLook;
            readonly List<Pup> pups = new List<Pup>();
            readonly List<Pin> pins = new List<Pin>();
            readonly List<Coin> coins = new List<Coin>();
            readonly List<Plat> plats = new List<Plat>();
            int coinN, tokN;
            // the lane: a free flat strip near the plaza where the moves are shown
            Vector3 L0, LF = Vector3.forward, LR = Vector3.right;
            float side = 1;
            // camera
            Vector3 camPos, camLook, tgtPos, tgtLook;
            float camRate = 2.5f, fovT = 52;
            Func<Vector3> follow;
            Vector3 followOff;
            Func<(Vector3, Vector3)> camRig;
            bool safeCam;
            Vector4 safe;

            void Start()
            {
                safe = UIRoot.Safe();
                BuildUI();
                Build();
                vsrc = gameObject.AddComponent<AudioSource>(); vsrc.playOnAwake = false;
                mus = gameObject.AddComponent<AudioSource>();
                mus.clip = Tune(); mus.loop = true; mus.volume = 0.09f; mus.playOnAwake = false;
                StartCoroutine(Run());
            }

            void OnDestroy()
            {
                if (setGo && set == setGo) Restore();
                if (cam) cam.targetTexture = null;
                if (rt) { rt.Release(); Destroy(rt); }
            }

            // ================= UI =================
            void BuildUI()
            {
                var stage = transform;
                var vr = UIKit.Rect("View", stage);
                view = vr.gameObject.AddComponent<RawImage>();
                view.raycastTarget = false; view.color = Color.white;
                UIKit.Fill(vr);
                ov = UIKit.Fill(UIKit.Rect("Overlay", stage));
                title = UIKit.Txt(stage, "", 110, UIKit.Gold);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 160));
                title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 110;
                title2 = UIKit.Txt(stage, "", 44, Color.white);
                UIKit.Place(title2.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 70));
                var bar = UIKit.Img(stage, UIKit.Panel, new Color(0, 0, 0, 0.55f), "SubBar");
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22 + safe.y), new Vector2(1400, 104));
                subBar = bar.gameObject;
                sub = UIKit.Txt(bar.transform, "", 38, Color.white);
                UIKit.Fill(sub.rectTransform, 26, 8, 26, 8);
                sub.enableAutoSizing = true; sub.fontSizeMin = 20; sub.fontSizeMax = 38;
                subBar.SetActive(false);
                var x = UIKit.Button(stage, "X", UIKit.Red, Tutorial.Stop, 40, "Close");
                UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20 - safe.z, -20 - safe.w), new Vector2(96, 86));
            }

            void Title(string a, string b = "")
            {
                title.text = a ?? ""; title2.text = b ?? "";
                titleT = a == null ? -1 : 0;
                title.rectTransform.localScale = Vector3.zero;
            }

            /// <summary>Removes this scene's captions, chips, arrows, mock windows.</summary>
            void Clear()
            {
                for (int i = ov.childCount - 1; i >= 0; i--) Destroy(ov.GetChild(i).gameObject);
                pins.Clear(); chips.Clear();
                keyRow = capRt = hudBar = null;
            }

            /// <summary>A big caption at the top of the screen.</summary>
            void Cap(string text, Color col)
            {
                if (capRt) Destroy(capRt.gameObject);
                var bg = UIKit.Img(ov, UIKit.Pill, new Color(0.04f, 0.1f, 0.28f, 0.78f), "Caption");
                var t = UIKit.Txt(bg.transform, text, 50, col);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                UIKit.Fill(t.rectTransform, 30, 4, 30, 4);
                float w = Mathf.Clamp(t.GetPreferredValues(text, 3000, 90).x + 90, 320, 1450);
                UIKit.Place(bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -122 - safe.w), new Vector2(w, 88));
                capRt = bg.rectTransform;
                var r = capRt;
                Tick(bg, k =>
                {
                    float u = Mathf.Min(1, k * 4);
                    r.localScale = Vector3.one * (u < 1 ? Mathf.Sin(u * Mathf.PI * 0.5f) * 1.1f : 1 + Mathf.Sin(k * 3) * 0.02f);
                });
                AudioFx.Play("click", 0.4f);
            }

            /// <summary>A row of control chips above the subtitles: "A", "B" (phone buttons), "J" (joystick),
            /// "k:ПРОБЕЛ" (a computer key), "b:ВОЙТИ" (a game button), "t:или" (text), "h:ТЕЛЕФОН" (a small heading).</summary>
            void Keys(params string[] items)
            {
                if (keyRow) Destroy(keyRow.gameObject);
                chips.Clear();
                keyRow = null;
                if (items == null || items.Length == 0) return;
                keyRow = UIKit.Rect("Keys", ov);
                var made = new List<(RectTransform rt, float w)>();
                foreach (var it in items)
                {
                    string kind = it.Length > 2 && it[1] == ':' ? it.Substring(0, 1) : it;
                    string label = it.Length > 2 && it[1] == ':' ? it.Substring(2) : it;
                    RectTransform r; float w;
                    switch (kind)
                    {
                        case "A":
                        case "B":
                        {
                            var img = UIKit.Img(keyRow, UIKit.Circle, UIKit.BtnFill(kind == "A" ? UIKit.Green : UIKit.Red), "Round " + kind);
                            var ring = UIKit.Img(img.transform, UIKit.RimRing, UIKit.BtnRim, "Ring"); UIKit.Fill(ring.rectTransform);
                            UIKit.Fill(UIKit.Txt(img.transform, kind, 54, Color.white).rectTransform, 4, 4, 4, 4);
                            r = img.rectTransform; w = 104;
                            r.sizeDelta = new Vector2(104, 104);
                            break;
                        }
                        case "J":
                        {
                            var img = UIKit.Img(keyRow, UIKit.Ring, new Color(1, 1, 1, 0.85f), "Joystick");
                            var knob = UIKit.Img(img.transform, UIKit.Circle, new Color(1, 1, 1, 0.9f), "Knob");
                            UIKit.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
                            var kr = knob.rectTransform;
                            Tick(img, k => kr.anchoredPosition = new Vector2(Mathf.Cos(k * 2.6f), Mathf.Sin(k * 2.6f)) * 24);
                            r = img.rectTransform; w = 104;
                            r.sizeDelta = new Vector2(104, 104);
                            break;
                        }
                        case "k":
                        case "b":
                        {
                            w = Mathf.Max(86, label.Length * 23 + 46);
                            var img = UIKit.Img(keyRow, UIKit.Btn, kind == "k" ? new Color(0.96f, 0.97f, 1f) : UIKit.BtnFill(UIKit.Orange), "Key " + label);
                            if (kind == "b") { var rim = UIKit.Img(img.transform, UIKit.BtnFrame, UIKit.BtnRim, "Rim"); UIKit.Fill(rim.rectTransform, -3, -3, -3, -3); }
                            var t = UIKit.Txt(img.transform, label, 34, kind == "k" ? UIKit.Ink : Color.white, TextAlignmentOptions.Center, kind == "b");
                            t.textWrappingMode = TextWrappingModes.NoWrap;
                            UIKit.Fill(t.rectTransform, 6, 4, 6, 4);
                            r = img.rectTransform;
                            r.sizeDelta = new Vector2(w, 84);
                            break;
                        }
                        case "h":
                        {
                            w = label.Length * 17 + 20;
                            var t = UIKit.Txt(keyRow, label, 26, UIKit.Gold);
                            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                            r = t.rectTransform; r.sizeDelta = new Vector2(w, 40);
                            break;
                        }
                        default:
                        {
                            w = Mathf.Max(18, label.Length * 21 + 14);
                            var t = UIKit.Txt(keyRow, label, 36, Color.white);
                            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                            r = t.rectTransform; r.sizeDelta = new Vector2(w, 60);
                            break;
                        }
                    }
                    made.Add((r, w));
                    chips.Add(r);
                }
                const float gap = 14;
                float total = -gap;
                foreach (var m in made) total += m.w + gap;
                UIKit.Place(keyRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 196 + safe.y), new Vector2(total, 110));
                float x = -total / 2;
                foreach (var m in made)
                {
                    m.rt.anchorMin = m.rt.anchorMax = new Vector2(0.5f, 0.5f);
                    m.rt.pivot = new Vector2(0.5f, 0.5f);
                    m.rt.anchoredPosition = new Vector2(x + m.w / 2, 0);
                    x += m.w + gap;
                }
                var kr2 = keyRow;
                Tick(keyRow, k => kr2.localScale = Vector3.one * Mathf.Min(1, 0.6f + k * 3));
            }

            /// <summary>«Presses» a chip: a quick punch and a ripple.</summary>
            void Press(params int[] idx)
            {
                foreach (var i in idx)
                {
                    if (i < 0 || i >= chips.Count || !chips[i]) continue;
                    var r = chips[i];
                    Tick(r, k => r.localScale = Vector3.one * (1 + 0.28f * Mathf.Sin(Mathf.PI * k / 0.3f)), 0.3f);
                    Ripple(r);
                }
            }

            void Ripple(RectTransform r)
            {
                var ring = UIKit.Img(r, UIKit.Ring, UIKit.Gold, "Ripple");
                var rr = ring.rectTransform;
                UIKit.Fill(rr);
                Tick(ring, k =>
                {
                    float u = k / 0.5f;
                    rr.localScale = Vector3.one * (1 + u * 0.7f);
                    ring.color = new Color(1, 0.85f, 0.3f, 1 - u);
                    if (u >= 1) Destroy(ring.gameObject);
                });
            }

            /// <summary>A pulsing ring round a UI element (a mock button) — «press here».</summary>
            void Halo(RectTransform r)
            {
                var ring = UIKit.Img(r, UIKit.Ring, UIKit.Gold, "Halo");
                var rr = ring.rectTransform;
                UIKit.Fill(rr, -16, -16, -16, -16);
                Tick(ring, k =>
                {
                    float s = 1 + Mathf.Abs(Mathf.Sin(k * 4)) * 0.12f;
                    rr.localScale = Vector3.one * s;
                    ring.color = new Color(1, 0.85f, 0.25f, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(k * 4)));
                });
            }

            /// <summary>A tap on a mock button: ripple + punch + click.</summary>
            void Tap(RectTransform r)
            {
                if (!r) return;
                Tick(r, k => r.localScale = Vector3.one * (1 - 0.15f * Mathf.Sin(Mathf.PI * k / 0.25f)), 0.25f);
                Ripple(r);
                AudioFx.Play("click", 0.8f);
            }

            /// <summary>A bouncing arrow on the screen pointing down at a point in the world.</summary>
            void Arrow(Func<Vector3> at, string label, Color col)
            {
                var box = UIKit.Rect("Arrow", ov);
                box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
                box.pivot = new Vector2(0.5f, 0); box.sizeDelta = new Vector2(10, 10);
                var inner = UIKit.Rect("In", box);
                inner.anchorMin = inner.anchorMax = new Vector2(0.5f, 0); inner.pivot = new Vector2(0.5f, 0);
                inner.sizeDelta = new Vector2(90, 90);
                var back = UIKit.Img(inner, Tri(), new Color(0.08f, 0.08f, 0.2f, 0.85f), "Edge");
                UIKit.Place(back.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -7), new Vector2(88, 78));
                var head = UIKit.Img(inner, Tri(), col, "Head");
                UIKit.Place(head.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(70, 62));
                if (!string.IsNullOrEmpty(label))
                {
                    var pill = UIKit.Img(inner, UIKit.Pill, new Color(0.04f, 0.1f, 0.28f, 0.85f), "Label");
                    var t = UIKit.Txt(pill.transform, label, 34, col);
                    t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                    UIKit.Fill(t.rectTransform, 18, 2, 18, 2);
                    float w = Mathf.Clamp(t.GetPreferredValues(label, 2000, 60).x + 50, 120, 900);
                    UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(w, 58));
                }
                Tick(inner, k => inner.anchoredPosition = new Vector2(0, 10 + Mathf.Abs(Mathf.Sin(k * 4)) * 24));
                pins.Add(new Pin { rt = box, at = at });
            }

            /// <summary>A pulsing circle on the screen round a spot in the world (radius in metres).</summary>
            void Circle(Func<Vector3> at, float radius, Color col)
            {
                var ring = UIKit.Img(ov, UIKit.Ring, col, "Circle");
                var rr = ring.rectTransform;
                rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f); rr.pivot = new Vector2(0.5f, 0.5f);
                var inner = UIKit.Img(rr, UIKit.Ring, new Color(1, 1, 1, 0.6f), "In");
                UIKit.Fill(inner.rectTransform, 10, 10, 10, 10);
                Tick(ring, k => { float a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(k * 3.5f)); ring.color = new Color(col.r, col.g, col.b, a); inner.rectTransform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(k * 5)); });
                pins.Add(new Pin { rt = rr, at = at, radius = radius });
            }

            /// <summary>A speech bubble on the screen over somebody's head.</summary>
            void Bubble(Func<Vector3> at, string text)
            {
                var box = UIKit.Rect("Bubble", ov);
                box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
                box.pivot = new Vector2(0.5f, 0); box.sizeDelta = new Vector2(10, 10);
                var pill = UIKit.Img(box, UIKit.Pill, Color.white, "Pill");
                var t = UIKit.Txt(pill.transform, text, 34, UIKit.Ink, TextAlignmentOptions.Center, false);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                UIKit.Fill(t.rectTransform, 16, 2, 16, 2);
                float w = Mathf.Clamp(t.GetPreferredValues(text, 2000, 60).x + 46, 120, 700);
                UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(w, 60));
                var pr = pill.rectTransform;
                Tick(pill, k => pr.localScale = Vector3.one * Mathf.Min(1, k * 5));
                pins.Add(new Pin { rt = box, at = at });
            }

            RectTransform MockRound(string label, Color col, Vector2 anchor, Vector2 pos, float size, float font)
            {
                var img = UIKit.Img(ov, UIKit.Circle, UIKit.BtnFill(col), "Mock " + label);
                var ring = UIKit.Img(img.transform, UIKit.RimRing, UIKit.BtnRim, "Ring"); UIKit.Fill(ring.rectTransform);
                var t = UIKit.Txt(img.transform, label, font, Color.white);
                UIKit.Fill(t.rectTransform, 6, 6, 6, 6);
                t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = font;
                UIKit.Place(img.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
                return img.rectTransform;
            }

            RectTransform MockBtn(Transform parent, string label, Color col, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float font)
            {
                var img = UIKit.Img(parent, UIKit.Btn, UIKit.BtnFill(col), "Mock " + label);
                var rim = UIKit.Img(img.transform, UIKit.BtnFrame, UIKit.BtnRim, "Rim");
                UIKit.Fill(rim.rectTransform, -3, -3, -3, -3);
                var t = UIKit.Txt(img.transform, label, font, Color.white);
                UIKit.Fill(t.rectTransform, 10, 4, 10, 4);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = font;
                UIKit.Place(img.rectTransform, anchor, pivot, pos, size);
                return img.rectTransform;
            }

            /// <summary>A mock of the game's window (same look as <see cref="Window"/>), drawn over the film.</summary>
            RectTransform MockWin(string head, float w, float h)
            {
                var dim = UIKit.Img(ov, UIKit.White, new Color(0.05f, 0.1f, 0.25f, 0.35f), "MockDim");
                UIKit.Fill(dim.rectTransform);
                var panel = UIKit.Img(dim.transform, UIKit.Panel, Color.white, "MockWindow");
                UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(w, h));
                var t = UIKit.Txt(panel.transform, head, 42, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
                UIKit.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -18), new Vector2(w - 150, 64));
                MockBtn(panel.transform, "X", UIKit.Red, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -16), new Vector2(84, 70), 34);
                var pr = panel.rectTransform;
                Tick(panel, k => { float u = Mathf.Min(1, k * 5); pr.localScale = Vector3.one * (0.85f + 0.15f * u); }, 0.4f);
                AudioFx.Play("open", 0.5f);
                return pr;
            }

            RectTransform MockRow(RectTransform win, int i, string head, string about, string btn, Color col)
            {
                var bg = UIKit.Img(win, UIKit.Glass, Color.white, "Row");
                var r = bg.rectTransform;
                r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
                r.offsetMin = new Vector2(30, 0); r.offsetMax = new Vector2(-30, 0);
                r.anchoredPosition = new Vector2(0, -98 - i * 114); r.sizeDelta = new Vector2(-60, 104);
                var t = UIKit.Txt(bg.transform, head, 30, Color.white, TextAlignmentOptions.TopLeft, false);
                t.rectTransform.anchorMin = new Vector2(0, 0); t.rectTransform.anchorMax = new Vector2(1, 1);
                t.rectTransform.offsetMin = new Vector2(22, 46); t.rectTransform.offsetMax = new Vector2(-300, -12);
                if (!string.IsNullOrEmpty(about))
                {
                    var s = UIKit.Txt(bg.transform, about, 22, UIKit.Muted, TextAlignmentOptions.TopLeft, false);
                    s.rectTransform.anchorMin = new Vector2(0, 0); s.rectTransform.anchorMax = new Vector2(1, 0);
                    s.rectTransform.pivot = new Vector2(0, 0);
                    s.rectTransform.offsetMin = new Vector2(22, 8); s.rectTransform.offsetMax = new Vector2(-300, 48);
                }
                if (btn == null) return r;
                return MockBtn(bg.transform, btn, col, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(260, 76), 26);
            }

            void CloseWin(RectTransform win) { if (win && win.parent) Destroy(win.parent.gameObject); }

            /// <summary>The coin / token counter (like the HUD's top bar).</summary>
            void HudBar()
            {
                var bar = UIKit.Img(ov, UIKit.Pill, new Color(0.05f, 0.15f, 0.35f, 0.75f), "TopBar");
                UIKit.Place(bar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + safe.x, -14 - safe.w), new Vector2(330, 64));
                hudBar = bar.rectTransform;
                var ci = UIKit.Img(bar.transform, UIKit.Circle, UIKit.Gold, "Coin");
                UIKit.Place(ci.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(46, 46));
                var ci2 = UIKit.Img(ci.transform, UIKit.Circle, Hex("FFB300"), "In");
                UIKit.Place(ci2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
                coinTxt = UIKit.Txt(bar.transform, "0", 36, Color.white, TextAlignmentOptions.MidlineLeft);
                UIKit.Place(coinTxt.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(100, 56));
                var ti = UIKit.Img(bar.transform, Hud.TokenSprite != null ? Hud.TokenSprite : UIKit.Circle, UIKit.Violet, "Token");
                UIKit.Place(ti.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(176, 0), new Vector2(50, 50));
                tokTxt = UIKit.Txt(bar.transform, "0", 36, Color.white, TextAlignmentOptions.MidlineLeft);
                UIKit.Place(tokTxt.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(232, 0), new Vector2(90, 56));
                coinN = tokN = 0;
            }

            // ================= the set =================
            void Build()
            {
                setGo = new GameObject("TutorialSet") { layer = Layers.Preview };
                W = setGo.transform;
                var cg = new GameObject("TutorialCam") { layer = Layers.Preview };
                cg.transform.SetParent(W, false);
                cam = cg.AddComponent<Camera>();
                // everything in the lobby except the player (hidden), the UI and the icon studio
                cam.cullingMask = ~((1 << Layers.Player) | (1 << 5) | (1 << Layers.Icon) | (1 << Layers.Barrier));
                cam.fieldOfView = 52; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600;
                var gc = PlayerCtl.I ? PlayerCtl.I.Cam : null;
                cam.depth = (gc ? gc.depth : 0) - 5;
                Grab(setGo, cam);
                if (gc)
                {
                    cam.clearFlags = gc.clearFlags; cam.backgroundColor = gc.backgroundColor;
                    var gd = gc.GetUniversalAdditionalCameraData();
                    var cd = cam.GetUniversalAdditionalCameraData();
                    cd.renderPostProcessing = gamePost;
                    cd.antialiasing = gd.antialiasing;
                }
                MakeRT();
                props = Node("Props", W, Vector3.zero);
                FindLane();
                heroLook = Save.CurrentLook();
                hero = MakePup("TutorialChubrik", heroLook, LP(0), 0);
                camPos = tgtPos = LP(0) + Vector3.up * 14 + LR * side * 20; camLook = tgtLook = LP(0);
            }

            void MakeRT()
            {
                int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
                if (rt && rt.width == w && rt.height == h) return;
                if (rt) { cam.targetTexture = null; rt.Release(); Destroy(rt); }
                bool low = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
                rt = new RenderTexture(w, h, 24) { name = "TutorialRT", antiAliasing = low ? 1 : 2 };
                cam.targetTexture = rt;
                view.texture = rt;
            }

            /// <summary>Builds things in the world on the tutorial's own layer (the game's builders use Parts.Layer).</summary>
            static void Make(Action a)
            {
                int old = Parts.Layer;
                Parts.Layer = Layers.Preview;
                try { a(); }
                finally { Parts.Layer = old; }
            }

            Pup MakePup(string name, Look look, Vector3 at, float yaw)
            {
                var p = new Pup { pos = at, yaw = yaw };
                var go = new GameObject(name) { layer = Layers.Preview };
                go.transform.SetParent(W, false);
                p.t = go.transform;
                Make(() => p.m = ChubrikModel.Create("Model", p.t, look, Layers.Preview));
                p.a = go.AddComponent<ChubrikAnim>();
                Dress(p, look);
                p.Apply();
                pups.Add(p);
                return p;
            }

            void Dress(Pup p, Look look)
            {
                Make(() => p.m.Build(look));
                p.a.model = p.m;
                p.a.walkStyle = Mathf.Max(0, look.Idx("walk"));
                p.a.emote = Mathf.Max(0, look.Idx("emote"));
            }

            void Put(Pup p, Vector3 at, Vector3? face = null)
            {
                p.t.gameObject.SetActive(true);
                p.pos = at; p.pos.y = Ground(at); p.scale = 1;
                if (face.HasValue) p.Face(face.Value);
                var a = p.a;
                a.StopEmote();
                a.speed01 = 0; a.grounded = true; a.vy = 0; a.gliding = false;
                a.leap = a.plunge = a.belly = a.hang = false;
                p.Apply();
            }

            // ---------- where things are ----------
            /// <summary>The ground under a point: the real lobby (raycast) or a demo platform.</summary>
            float Ground(Vector3 p)
            {
                float g = L0.y;
                if (Physics.Raycast(new Vector3(p.x, Mathf.Max(p.y, L0.y) + 2.5f, p.z), Vector3.down, out var hit, 30, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                    g = hit.point.y;
                var d = p - L0;
                float fx = Vector3.Dot(d, LF), fs = Vector3.Dot(d, LR);
                foreach (var pl in plats)
                    if (fx >= pl.x0 && fx <= pl.x1 && fs >= pl.s0 && fs <= pl.s1) g = Mathf.Max(g, pl.top);
                return g;
            }

            /// <summary>A point on the lane: x along it, s to the side (on the ground).</summary>
            Vector3 LP(float x, float s = 0)
            {
                var p = L0 + LF * x + LR * s;
                p.y = Ground(p);
                return p;
            }

            /// <summary>Looks for a free flat strip (16 m) near the plaza, away from the river.</summary>
            void FindLane()
            {
                Physics.SyncTransforms();
                var best = new Vector3(0, 0.1f, 1.5f);
                float bestYaw = 90;
                bool found = false;
                for (int ring = 0; ring < 9 && !found; ring++)
                    for (int k = 0; k < (ring == 0 ? 1 : 12) && !found; k++)
                    {
                        float a = k * 30 * Mathf.Deg2Rad, r = ring * 2.5f;
                        var c = new Vector3(-2 + Mathf.Cos(a) * r, 0, 2 + Mathf.Sin(a) * r);
                        if (c.x > 5) continue;   // the river runs east of the plaza
                        for (int yi = 0; yi < 4 && !found; yi++)
                        {
                            float yaw = 90 + yi * 45;
                            if (LaneOk(ref c, yaw)) { best = c; bestYaw = yaw; found = true; }
                        }
                    }
                if (!found && Physics.Raycast(best + Vector3.up * 6, Vector3.down, out var h0, 12, Layers.SolidMask, QueryTriggerInteraction.Ignore)) best.y = h0.point.y;
                L0 = best;
                LF = Quaternion.Euler(0, bestYaw, 0) * Vector3.forward;
                LR = Vector3.Cross(Vector3.up, LF);
                // the camera stands on the side with more free space
                float Clear(float s) => Physics.Raycast(L0 + Vector3.up * 2, LR * s, out var h, 14, Layers.SolidMask, QueryTriggerInteraction.Ignore) ? h.distance : 14;
                side = Clear(1) >= Clear(-1) ? 1 : -1;
            }

            static bool LaneOk(ref Vector3 c, float yaw)
            {
                if (!Physics.Raycast(c + Vector3.up * 6, Vector3.down, out var h0, 12, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
                if (h0.normal.y < 0.92f || Mathf.Abs(h0.point.y) > 0.4f) return false;
                c.y = h0.point.y;
                var rot = Quaternion.Euler(0, yaw, 0);
                if (Physics.CheckBox(c + Vector3.up * 1.9f, new Vector3(1.7f, 1.55f, 8.4f), rot, Layers.SolidMask | (1 << Layers.Barrier), QueryTriggerInteraction.Ignore)) return false;
                for (int i = -4; i <= 4; i++)
                {
                    var p = c + rot * Vector3.forward * (i * 2f);
                    if (!Physics.Raycast(p + Vector3.up * 6, Vector3.down, out var h, 12, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
                    if (Mathf.Abs(h.point.y - c.y) > 0.2f || h.normal.y < 0.92f) return false;
                }
                return true;
            }

            // ================= time =================
            void Update()
            {
                Dt = D;
                clock += Dt;
                if (PlayerCtl.I) PlayerCtl.I.Frozen = true;
                if (Input.GetKeyDown(KeyCode.Escape) && !Boot.Testing) { Tutorial.Stop(); return; }
                if (cam) MakeRT();
                foreach (var p in pups) if (p.t) p.Apply();
                // demo coins and tokens: the puppet runs through them
                for (int i = coins.Count - 1; i >= 0; i--)
                {
                    var c = coins[i];
                    if (!c.t) { coins.RemoveAt(i); continue; }
                    if (hero == null || !hero.t.gameObject.activeSelf || (c.t.position - hero.Chest).magnitude > (c.token ? 1.1f : 0.85f)) continue;
                    coins.RemoveAt(i);
                    Collect(c);
                }
                if (titleT >= 0)
                {
                    titleT += Dt;
                    float k = Mathf.Min(1, titleT * 2.5f);
                    title.rectTransform.localScale = Vector3.one * (k < 1 ? Mathf.Sin(k * Mathf.PI * 0.5f) * 1.12f : 1 + Mathf.Sin(titleT * 4) * 0.04f);
                }
                if (mus)
                {
                    mus.pitch = Mathf.Clamp(Speed, 0.5f, 3f);
                    mus.volume = Mathf.MoveTowards(mus.volume, talking && vsrc.isPlaying ? 0.05f : 0.1f, Time.unscaledDeltaTime * 0.5f);
                }
            }

            void LateUpdate()
            {
                if (!cam) return;
                if (camRig != null)
                {
                    var (p, l) = camRig();
                    camPos = tgtPos = p; camLook = tgtLook = l;
                }
                else
                {
                    if (follow != null) { tgtLook = follow(); tgtPos = tgtLook + followOff; }
                    float k = 1 - Mathf.Exp(-camRate * Dt);
                    camPos = Vector3.Lerp(camPos, tgtPos, k);
                    camLook = Vector3.Lerp(camLook, tgtLook, k);
                }
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovT, 1 - Mathf.Exp(-3 * Dt));
                ApplyCam();
                PlacePins();
            }

            void ApplyCam()
            {
                var pos = camPos;
                if (safeCam)
                {
                    // don't let a tracking shot end up inside a wall or a tree
                    var d = pos - camLook; float len = d.magnitude;
                    if (len > 0.5f && Physics.SphereCast(camLook, 0.3f, d / len, out var hit, len, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                        pos = camLook + d / len * Mathf.Max(1.2f, hit.distance - 0.15f);
                }
                var drift = new Vector3(Mathf.PerlinNoise(clock * 0.21f, 1.7f) - 0.5f, Mathf.PerlinNoise(4.1f, clock * 0.17f) - 0.5f, 0) * 0.05f;
                cam.transform.position = pos;
                cam.transform.LookAt(camLook + drift);
            }

            void PlacePins()
            {
                var size = ov.rect.size;
                foreach (var p in pins)
                {
                    if (!p.rt) continue;
                    var w = p.at();
                    var vp = cam.WorldToViewportPoint(w);
                    bool on = vp.z > 0.3f;
                    if (p.rt.gameObject.activeSelf != on) p.rt.gameObject.SetActive(on);
                    if (!on) continue;
                    p.rt.anchoredPosition = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
                    if (p.radius > 0)
                    {
                        var e = cam.WorldToViewportPoint(w + cam.transform.right * p.radius);
                        float px = Mathf.Clamp(Mathf.Abs(e.x - vp.x) * size.x * 2, 40, 1400);
                        p.rt.sizeDelta = new Vector2(px, px * 0.55f + 20);
                    }
                }
            }

            // ================= camera moves =================
            void Cut(Vector3 pos, Vector3 look, float fov = 52, bool safeShot = false)
            {
                follow = null; camRig = null; safeCam = safeShot;
                camPos = tgtPos = pos; camLook = tgtLook = look;
                fovT = fov; cam.fieldOfView = fov;
                ApplyCam();
            }

            void Glide(Vector3 pos, Vector3 look, float rate = 1.5f, float fov = -1)
            {
                follow = null; camRig = null;
                tgtPos = pos; tgtLook = look; camRate = rate;
                if (fov > 0) fovT = fov;
            }

            void Track(Func<Vector3> f, Vector3 off, float rate = 3f, float fov = 52)
            {
                camRig = null; follow = f; followOff = off; camRate = rate; fovT = fov; safeCam = true;
            }

            /// <summary>Side view of the lane, following the puppet.</summary>
            void SideTrack(float dist = 7.5f, float h = 2.3f, float lead = 0.8f, bool cut = true, float fov = 50)
            {
                Func<Vector3> f = () => hero.pos + Vector3.up * 1.0f + LF * lead;
                var off = LR * side * dist + Vector3.up * h - LF * 0.6f;
                if (cut) Cut(f() + off, f(), fov, true);
                Track(f, off, 3f, fov);
            }

            // ================= animation helpers (tutorial time = real time × Speed) =================
            IEnumerator Act(float dur, Action<float> f)
            {
                float t = 0;
                while (t < dur) { f(t); yield return null; t += Dt; }
                f(dur);
            }

            IEnumerator Wait(float s)
            {
                for (float t = 0; t < s; t += Dt) yield return null;
            }

            /// <summary>Runs coroutines side by side; finishes when all of them are done.</summary>
            IEnumerator Par(params IEnumerator[] es)
            {
                int left = es.Length;
                foreach (var e in es) StartCoroutine(Wrap(e, () => left--));
                while (left > 0) yield return null;
            }

            IEnumerator Wrap(IEnumerator e, Action done)
            {
                yield return StartCoroutine(e);
                done();
            }

            IEnumerator Walk(Pup p, Vector3 to, float speed = 6.8f)
            {
                var a = p.a;
                float limit = (new Vector3(to.x - p.pos.x, 0, to.z - p.pos.z).magnitude / Mathf.Max(0.5f, speed)) * 2 + 1;
                for (float t = 0; t < limit; t += Dt)
                {
                    var d = new Vector3(to.x - p.pos.x, 0, to.z - p.pos.z);
                    float dist = d.magnitude;
                    if (dist < 0.04f) break;
                    var dir = d / dist;
                    p.yaw = Mathf.MoveTowardsAngle(p.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 900 * Dt);
                    p.pos += dir * Mathf.Min(speed * Dt, dist);
                    p.pos.y = Ground(p.pos);
                    a.speed01 = Mathf.Clamp01(speed / 6.8f); a.grounded = true; a.vy = 0;
                    yield return null;
                }
                a.speed01 = 0;
            }

            /// <summary>Flies along an arc to <paramref name="end"/> (absolute height), <paramref name="h"/> over the straight line.</summary>
            IEnumerator Arc(Pup p, Vector3 end, float h, float dur, bool flip = false, string snd = "jump0")
            {
                var a = p.a;
                var s = p.pos;
                if (new Vector2(end.x - s.x, end.z - s.z).sqrMagnitude > 0.01f) p.Face(end);
                a.grounded = false; a.speed01 = 0;
                if (snd != null) AudioFx.Play(snd, 0.7f);
                if (flip) a.Flip();
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    var q = Vector3.Lerp(s, end, u);
                    q.y = Mathf.Lerp(s.y, end.y, u) + 4 * h * u * (1 - u);
                    a.vy = Dt > 0 ? (q.y - p.pos.y) / Dt : 0;
                    p.pos = q;
                });
            }

            void Land(Pup p, float k = 0.6f)
            {
                p.pos.y = Ground(p.pos);
                p.a.grounded = true; p.a.vy = 0;
                p.a.Land(k);
                AudioFx.Land(1);
                Effects.Emit("dust", p.pos, 6, 0.3f);
            }

            IEnumerator JumpFwd(Pup p, float dist, float h = 1.4f, float dur = 0.6f)
            {
                var end = p.pos + p.Fwd * dist;
                end.y = Ground(end);
                yield return Arc(p, end, h, dur);
                Land(p);
            }

            void Swing(Pup p)
            {
                p.a.Attack();
                AudioFx.Play("swing", 0.7f);
            }

            // ================= props =================
            Transform Crate(Vector3 at)
            {
                Transform c = null;
                Make(() =>
                {
                    c = Node("DemoCrate", props, at);
                    B(c, new Vector3(0, 0.55f, 0), Vector3.one * 1.1f, Mats.Tex(Tex.Planks("C08040"), Color.white, 0.2f));
                    B(c, new Vector3(0, 0.55f, 0), new Vector3(1.14f, 0.15f, 1.14f), M("6D4425"));
                    B(c, new Vector3(0, 0.55f, 0), new Vector3(0.15f, 1.14f, 1.14f), M("6D4425"));
                });
                return c;
            }

            /// <summary>Breaks a demo thing into flying bits.</summary>
            void Burst(Transform t, string hex, int n = 8, float size = 0.32f)
            {
                if (!t) return;
                var c = t.position + Vector3.up * 0.55f;
                Make(() =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        var b = B(props, c + UnityEngine.Random.insideUnitSphere * 0.3f, Vector3.one * size, M(hex, 0.3f), UnityEngine.Random.insideUnitSphere * 180).transform;
                        var v = UnityEngine.Random.insideUnitSphere * 3 + Vector3.up * 4.5f;
                        var rot = UnityEngine.Random.insideUnitSphere * 400;
                        Tick(b, k =>
                        {
                            if (k >= 1.1f) { Destroy(b.gameObject); return; }
                            v += Vector3.down * 14 * Dt;
                            b.position += v * Dt;
                            b.Rotate(rot * Dt);
                            b.localScale = Vector3.one * size * Mathf.Clamp01((1.1f - k) * 3);
                        });
                    }
                });
                Destroy(t.gameObject);
            }

            void DemoCoin(Vector3 pos)
            {
                Make(() =>
                {
                    var t = Node("DemoCoin", props, pos);
                    Cy(t, Vector3.zero, new Vector3(0.45f, 0.04f, 0.45f), M("FFC83A", 0.9f, 0.85f), new Vector3(90, 0, 0));
                    Cy(t, Vector3.zero, new Vector3(0.32f, 0.05f, 0.32f), M("FFB300", 0.9f, 0.85f), new Vector3(90, 0, 0));
                    t.gameObject.AddComponent<Spin>().speed = 150;
                    coins.Add(new Coin { t = t });
                });
            }

            Transform DemoToken(Vector3 pos)
            {
                Transform t = null;
                Make(() =>
                {
                    t = Node("DemoToken", props, pos);
                    var inner = Node("Bob", t, Vector3.zero);
                    Msh(Meshes.Star(), inner, Vector3.zero, new Vector3(0.8f, 0.8f, 0.25f), G("B57CFF", 1.6f));
                    Ring(inner, Vector3.zero, 0.5f, 0.04f, G("FFFFFF", 1.5f), new Vector3(90, 0, 0));
                    S(inner, Vector3.zero, 1.3f, Add("B57CFF", 0.15f));
                    inner.gameObject.AddComponent<Spin>().speed = 90;
                    inner.gameObject.AddComponent<Bob>().amp = 0.15f;
                    coins.Add(new Coin { t = t, token = true });
                });
                return t;
            }

            void Collect(Coin c)
            {
                var p = c.t.position;
                if (c.token)
                {
                    AudioFx.Play("token");
                    Effects.Emit("magic", p, 24, 0.5f, Vector3.up * 1.5f);
                    tokN++; if (tokTxt) tokTxt.text = tokN.ToString();
                    Effects.Float("+1 ЖЕТОН", p + Vector3.up * 0.6f, UIKit.Violet);
                }
                else
                {
                    AudioFx.Play("coin", 0.8f);
                    Effects.Emit("coin", p, 6, 0.2f, Vector3.up * 2);
                    coinN++; if (coinTxt) coinTxt.text = coinN.ToString();
                }
                if (hudBar) { var hb = hudBar; Tick(hb, k => hb.localScale = Vector3.one * (1 + 0.12f * Mathf.Sin(Mathf.PI * k / 0.25f)), 0.25f); }
                var t = c.t;
                Tick(t, k =>
                {
                    if (k >= 0.3f) { Destroy(t.gameObject); return; }
                    t.position += Vector3.up * 5 * Dt;
                    t.localScale = Vector3.one * (1 - k / 0.3f);
                });
            }

            Transform DemoKey(Vector3 pos, string label)
            {
                Transform t = null;
                Make(() =>
                {
                    t = Node("DemoKey", props, pos);
                    var k = Node("Key", t, Vector3.zero);
                    k.localScale = Vector3.one * 1.6f;
                    KeyModel.Build(k);
                    k.gameObject.AddComponent<Spin>().speed = 120;
                    var tx = Prim.Text(t, label, new Vector3(0, 1.0f, 0), 3, Hex("FFE14D"));
                    tx.gameObject.AddComponent<Billboard>();
                });
                return t;
            }

            /// <summary>A demo cage with a captured friend (geometry like <see cref="Cage"/>, but not a real cage).</summary>
            (Transform cage, Transform bars, ChubrikModel friend) DemoCage(Vector3 pos, float yaw, string id, string label)
            {
                Transform c = null, bars = null; ChubrikModel f = null;
                Make(() =>
                {
                    c = Node("DemoCage", props, pos, new Vector3(0, yaw, 0));
                    var metal = M("5B6370", 0.7f, 0.7f);
                    var gold = M("FFC83A", 0.9f, 0.85f);
                    Cy(c, new Vector3(0, 0.1f, 0), new Vector3(1.9f, 0.1f, 1.9f), metal);
                    bars = Node("Bars", c, Vector3.zero);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k * Mathf.PI * 2 / 14;
                        Cy(bars, new Vector3(Mathf.Cos(a) * 0.9f, 1.1f, Mathf.Sin(a) * 0.9f), new Vector3(0.06f, 1f, 0.06f), metal);
                    }
                    Ring(bars, new Vector3(0, 1.1f, 0), 0.9f, 0.04f, metal);
                    Cy(bars, new Vector3(0, 2.15f, 0), new Vector3(1.95f, 0.06f, 1.95f), metal);
                    Cone(bars, new Vector3(0, 2.2f, 0), new Vector3(1.9f, 0.5f, 1.9f), M("C62828", 0.4f));
                    var lockT = Node("Lock", bars, new Vector3(0, 1.0f, 0.95f));
                    B(lockT, Vector3.zero, new Vector3(0.32f, 0.28f, 0.1f), gold);
                    Ring(lockT, new Vector3(0, 0.17f, 0), 0.1f, 0.03f, gold, new Vector3(90, 0, 0));
                    var tag = Prim.Text(c, label, new Vector3(0, 3.2f, 0), 3, Hex("FFE14D"));
                    tag.gameObject.AddComponent<Billboard>();
                    f = ChubrikModel.Create("Captive", c, Rescue.LookOf(id), Layers.Preview);
                    f.transform.localPosition = new Vector3(0, 0.2f, 0);
                    var an = f.gameObject.AddComponent<ChubrikAnim>();
                    an.model = f; an.Preview = true; an.emote = 6; an.PlayEmote();   // shrugging, waiting for help
                });
                return (c, bars, f);
            }

            // ================= voice and subtitles =================
            float Speak(string id, string text)
            {
                subBar.SetActive(true);
                sub.text = text;
                var clip = Resources.Load<AudioClip>("Sounds/tutorial/" + id);
                vsrc.Stop();
                if (clip)
                {
                    vsrc.clip = clip;
                    vsrc.pitch = Mathf.Min(3f, Pitch * Mathf.Clamp(Speed, 0.5f, 3f));
                    vsrc.Play();
                }
                talking = true;
                return clip ? clip.length / Pitch : 1f + text.Length * 0.06f;
            }

            IEnumerator Say(string id, string text, float tail = 0.25f)
            {
                float len = Speak(id, text);
                yield return Wait(len + tail);
            }

            // ================= the tutorial =================
            IEnumerator Run()
            {
                yield return null;
                mus.Play();
                var up = Vector3.up;
                Vector3 camSide = L0 + LR * side * 30;

                // ---------- 0. title ----------
                Scene = 0;
                Clear();
                Put(hero, LP(0), camSide);
                Cut(LP(-6) + LR * side * 18 + up * 13, LP(0) + up, 52);
                Glide(LP(0.6f) + LR * side * 5.2f + up * 1.8f, LP(0) + up * 1.05f, 0.7f);
                Title("КАК ИГРАТЬ", "обучение для чубриков");
                AudioFx.Play("portal");
                hero.a.PlayEmote();
                yield return Say("t01", "Привет! Это обучение игре «Чубрик».");
                yield return Say("t02", "Я покажу, что умеет чубрик и куда тут идти. Поехали!");
                Title(null);
                hero.a.StopEmote();

                // ---------- 1. running and jumping ----------
                Scene = 1;
                Clear();
                Put(hero, LP(-6), LP(6));
                SideTrack();
                Cap("БЕГ", UIKit.Lime);
                Keys("h:ТЕЛЕФОН", "J", "t: ", "h:КОМПЬЮТЕР", "k:W", "k:A", "k:S", "k:D");
                IEnumerator Run1()
                {
                    yield return Wait(0.6f);
                    yield return Walk(hero, LP(5));
                    yield return Wait(0.3f);
                    yield return Walk(hero, LP(-5));
                    hero.Face(LP(6));
                }
                yield return Par(Say("t03", "Бегать просто: на телефоне — джойстик слева, на компьютере — клавиши W, A, S, D."), Run1());
                Cap("ПРЫЖОК", UIKit.Lime);
                Keys("h:ТЕЛЕФОН", "A", "t: ", "h:КОМПЬЮТЕР", "k:ПРОБЕЛ");
                IEnumerator Jumps()
                {
                    yield return Wait(0.5f);
                    for (int i = 0; i < 3; i++)
                    {
                        Press(1, 4);
                        yield return JumpFwd(hero, 2.6f, 1.3f, 0.6f);
                        yield return Wait(0.2f);
                    }
                }
                yield return Par(Say("t04", "Прыжок — кнопка A на телефоне или пробел на компьютере. Прыг!"), Jumps());

                // ---------- 2. double jump ----------
                Scene = 2;
                Clear();
                Put(hero, LP(-5.5f), LP(6));
                SideTrack(8.5f, 2.8f);
                Cap("ДВОЙНОЙ ПРЫЖОК", UIKit.Lime);
                Keys("h:ТЕЛЕФОН", "A", "A", "t: ", "h:КОМПЬЮТЕР", "k:ПРОБЕЛ", "k:ПРОБЕЛ");
                IEnumerator Doubles()
                {
                    yield return Wait(0.4f);
                    for (int i = 0; i < 2; i++)
                    {
                        Press(1, 5);
                        var mid = hero.pos + hero.Fwd * 1.6f; mid.y = hero.pos.y + 1.5f;
                        yield return Arc(hero, mid, 0.35f, 0.36f);
                        Press(2, 6);
                        var end = hero.pos + hero.Fwd * 2.6f; end.y = Ground(end);
                        yield return Arc(hero, end, 1.8f, 0.72f, true, "jump2");
                        Land(hero, 0.8f);
                        yield return Walk(hero, hero.pos + hero.Fwd * 1.2f);
                    }
                }
                yield return Par(Say("t05", "Нажми прыжок ещё раз прямо в воздухе — будет двойной прыжок с кувырком!"), Doubles());

                // ---------- 3. hitting ----------
                Scene = 3;
                Clear();
                var crate1 = Crate(LP(1.2f));
                var crate2 = Crate(LP(3.9f));
                Put(hero, LP(-0.4f), LP(6));
                SideTrack(6.5f, 2.0f, 1.2f);
                Cap("УДАР", UIKit.Red);
                Keys("h:ТЕЛЕФОН", "B", "t: ", "h:КОМПЬЮТЕР", "k:F", "t:или", "k:КЛИК");
                IEnumerator Swings()
                {
                    yield return Wait(1.0f);
                    Press(1, 4); Swing(hero);
                    yield return Wait(1.2f);
                    Press(1, 6); Swing(hero);
                }
                yield return Par(Say("t06", "Удар — кнопка B. На компьютере — клавиша F или клик мышкой."), Swings());
                IEnumerator Breaks()
                {
                    yield return Wait(0.5f);
                    Press(1, 4); Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("crate"); Effects.Emit("coin", LP(1.2f) + up * 0.8f, 10, 0.3f, up * 3);
                    Burst(crate1, "C08040");
                    yield return Wait(0.6f);
                    yield return Walk(hero, LP(2.5f), 5f);
                    Press(1, 4); Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("crate"); Effects.Emit("coin", LP(3.9f) + up * 0.8f, 10, 0.3f, up * 3);
                    Burst(crate2, "C08040");
                }
                yield return Par(Say("t07", "Ударом ломают ящики. А в мирах в ящиках прячутся монетки!"), Breaks());

                // ---------- 4. the leap «хэ-хэй» ----------
                Scene = 4;
                Clear();
                Put(hero, LP(-7), LP(7));
                SideTrack(9f, 2.6f, 2f);
                Cap("РЫВОК «ХЭ-ХЭЙ»", UIKit.Gold);
                Keys("h:БЕГ", "t:+", "B", "t:+", "A", "t: ", "h:КОМПЬЮТЕР", "k:F", "t:+", "k:ПРОБЕЛ");
                IEnumerator Leap(bool voice)
                {
                    yield return Walk(hero, LP(-3.8f));
                    Press(2, 7); Swing(hero);
                    yield return Wait(0.12f);
                    Press(4, 9);
                    hero.a.leap = true;
                    if (voice) AudioFx.Voice("heyhey", 1.35f);
                    AudioFx.Play("swing", 0.6f);
                    Effects.Emit("stardust", hero.pos + up * 0.5f, 12, 0.35f);
                    var end = LP(4.2f);
                    yield return Arc(hero, end, 2.4f, 0.95f, false, null);
                    hero.a.leap = false;
                    Land(hero, 1f);
                }
                IEnumerator Leap1()
                {
                    yield return Wait(0.3f);
                    yield return Leap(true);
                    yield return Wait(0.2f);
                    yield return Walk(hero, LP(-7), 9f);
                    hero.Face(LP(7));
                }
                yield return Par(Say("t08", "А теперь крутой трюк! Беги, нажми B, а сразу за ним A."), Leap1());
                yield return Par(Say("t09", "Чубрик прыгнет далеко-далеко вперёд: хэ-хэй!"), Leap(true));

                // ---------- 5. the plunge and the super bounce ----------
                Scene = 5;
                Clear();
                Put(hero, LP(-3), LP(7));
                SideTrack(8.5f, 2.4f, 0.5f);
                Cap("НЫРОК ВНИЗ", UIKit.Cyan);
                Keys("h:ТЕЛЕФОН", "A", "t:+", "B", "t: ", "h:КОМПЬЮТЕР", "k:ПРОБЕЛ", "t:+", "k:F");
                IEnumerator Small()
                {
                    yield return Wait(0.5f);
                    Press(1, 6);
                    var top = hero.pos + hero.Fwd * 1.2f; top.y = hero.pos.y + 2.2f;
                    yield return Arc(hero, top, 0.2f, 0.42f);
                    Press(3, 8);
                    hero.a.plunge = true;
                    AudioFx.Play("swing"); AudioFx.Play("jump4", 0.7f);
                    Effects.Emit("stardust", hero.pos + up * 0.6f, 6, 0.3f);
                    float y0 = hero.pos.y, g = Ground(hero.pos);
                    yield return Act(0.22f, t => { float u = t / 0.22f; hero.pos.y = Mathf.Lerp(y0, g, u * u); hero.a.vy = -11; });
                    hero.a.plunge = false; hero.a.belly = true;
                    AudioFx.Play("land4", 0.6f); AudioFx.Play("punch", 0.5f); AudioFx.Voice("heyhey", 0.9f, 0.6f);
                    Effects.Emit("dust", hero.pos, 12, 0.5f);
                    yield return Arc(hero, hero.pos, 0.35f, 0.32f, false, null);
                    Land(hero, 0.6f);
                    yield return Wait(0.6f);
                    hero.a.belly = false;
                }
                yield return Par(Say("t10", "Прыгни и нажми B — чубрик нырнёт вниз и смешно плюхнется."), Small());
                Cap("СУПЕР-ОТСКОК!", UIKit.Gold);
                Keys("h:ТЕЛЕФОН", "A", "A", "t:+", "B", "t: ", "h:КОМПЬЮТЕР", "k:ПРОБЕЛ", "k:ПРОБЕЛ", "t:+", "k:F");
                SideTrack(10.5f, 3.2f, 0, false, 54);
                IEnumerator Big()
                {
                    yield return Wait(0.8f);
                    Press(1, 7);
                    var a1 = hero.pos + hero.Fwd * 0.8f; a1.y = hero.pos.y + 1.6f;
                    yield return Arc(hero, a1, 0.3f, 0.38f);
                    Press(2, 8);
                    var a2 = hero.pos + hero.Fwd * 0.8f; a2.y = hero.pos.y + 2.2f;
                    yield return Arc(hero, a2, 0.4f, 0.45f, true, "jump2");
                    Press(4, 10);
                    hero.a.plunge = true;
                    AudioFx.Play("swing"); AudioFx.Play("jump5", 0.4f);
                    Effects.Emit("stardust", hero.pos + up * 0.6f, 14, 0.3f);
                    float y0 = hero.pos.y, g = Ground(hero.pos);
                    yield return Act(0.26f, t => { float u = t / 0.26f; hero.pos.y = Mathf.Lerp(y0, g, u * u); hero.a.vy = -24; });
                    hero.a.plunge = false;
                    AudioFx.Play("boing", 1f); AudioFx.Play("land4", 0.8f);
                    Effects.Emit("dust", hero.pos, 26, 0.8f);
                    Effects.Emit("stardust", hero.pos + up * 0.5f, 18, 0.6f);
                    hero.a.Land(1f);
                    yield return Arc(hero, hero.pos + hero.Fwd * 1.5f + Vector3.zero, 4.6f, 1.25f, true, null);
                    Land(hero, 1f);
                }
                yield return Par(Say("t11", "А если прыгнуть два раза и нажать B, он полетит прямо вниз и отскочит выше двойного прыжка!"), Big());

                // ---------- 6. grabbing a ledge ----------
                Scene = 6;
                Clear();
                Keys();
                float top0 = LP(4.4f).y + 2.3f;
                Make(() =>
                {
                    var blk = Node("DemoLedge", props, LP(4.4f));
                    B(blk, new Vector3(0, 1.15f, 0), new Vector3(2.4f, 2.3f, 2.8f), M("9AA3AE", 0.2f), new Vector3(0, Mathf.Atan2(LR.x, LR.z) * Mathf.Rad2Deg, 0));
                    B(blk, new Vector3(0, 2.33f, 0), new Vector3(2.5f, 0.08f, 2.9f), M("6EBE4A", 0.2f), new Vector3(0, Mathf.Atan2(LR.x, LR.z) * Mathf.Rad2Deg, 0));
                });
                plats.Add(new Plat { x0 = 3.2f, x1 = 5.6f, s0 = -1.4f, s1 = 1.4f, top = top0 });
                Put(hero, LP(-3), LP(7));
                SideTrack(8f, 2.4f, 1.5f);
                Cap("НЕ ДОПРЫГНУЛ? ЗАЦЕПИСЬ!", UIKit.Gold);
                IEnumerator Ledge()
                {
                    yield return Wait(0.6f);
                    yield return Walk(hero, LP(0.4f));
                    var hangP = LP(2.78f); hangP.y = top0 - 1.3f;
                    yield return Arc(hero, hangP, 0.5f, 0.45f);
                    hero.a.hang = true; hero.a.grounded = false; hero.a.vy = 0;
                    AudioFx.Play("land4", 0.5f);
                    Effects.Emit("dust", LP(3.2f) + Vector3.up * (top0 - LP(3.2f).y), 6, 0.25f);
                    var edgeP = L0 + LF * 3.2f; edgeP.y = top0 + 0.15f;
                    Arrow(() => edgeP, "ЦЕПЛЯЕТСЯ САМ", UIKit.Gold);
                    yield return Wait(0.8f);
                    var from = hero.pos; var to = LP(3.8f);
                    yield return Act(0.45f, t =>
                    {
                        float u = t / 0.45f;
                        hero.pos = new Vector3(Mathf.Lerp(from.x, to.x, u), Mathf.Lerp(from.y, top0, Mathf.Min(1, u * 1.6f)), Mathf.Lerp(from.z, to.z, u));
                        if (u > 0.6f) hero.a.hang = false;
                    });
                    hero.a.hang = false;
                    Land(hero, 0.4f);
                    yield return Wait(0.5f);
                    yield return Arc(hero, LP(6.6f), 0.7f, 0.55f);
                    Land(hero);
                }
                yield return Par(Say("t12", "Не допрыгнул до края? Не беда! Чубрик сам зацепится руками и залезет наверх."), Ledge());

                // ---------- 7. coins and tokens ----------
                Scene = 7;
                Clear();
                HudBar();
                for (int i = 0; i < 7; i++) DemoCoin(LP(-4.5f + i * 0.9f) + up * 0.9f);
                var token = DemoToken(LP(4.6f) + up * (top0 - LP(4.6f).y + 0.9f));
                Put(hero, LP(-6.5f), LP(7));
                SideTrack(7.5f, 2.3f, 1.2f);
                Cap("МОНЕТКИ", UIKit.Gold);
                IEnumerator CoinRun()
                {
                    yield return Wait(0.4f);
                    yield return Walk(hero, LP(1.5f));
                }
                yield return Par(Say("t13", "Собирай золотые монетки. Просто пробеги через них!"), CoinRun());
                Cap("ЖЕТОНЫ — ФИОЛЕТОВЫЕ ЗВЁЗДЫ", UIKit.Violet);
                Arrow(() => token ? token.position + up * 0.9f : L0, "ЖЕТОН", UIKit.Violet);
                IEnumerator TokenRun()
                {
                    yield return Wait(1.2f);
                    var mid = hero.pos + hero.Fwd * 0.9f; mid.y = hero.pos.y + 1.5f;
                    yield return Arc(hero, mid, 0.35f, 0.36f);
                    var onTop = LP(3.9f);
                    yield return Arc(hero, onTop, 1.4f, 0.62f, true, "jump2");
                    Land(hero, 0.7f);
                    yield return Walk(hero, LP(4.9f), 4f);
                    yield return Wait(0.6f);
                    Cap("В ЛОББИ ИХ 10 — ИЩИ ВЫСОКО И В УГОЛКАХ", UIKit.Violet);
                    yield return Wait(0.8f);
                    yield return Arc(hero, LP(6.8f), 0.7f, 0.55f);
                    Land(hero);
                }
                yield return Par(Say("t14", "А ещё ищи фиолетовые звёздочки — это жетоны. В лобби их десять, они спрятаны высоко и в уголках."), TokenRun());
                for (int i = props.childCount - 1; i >= 0; i--) Destroy(props.GetChild(i).gameObject);
                plats.Clear(); coins.Clear();

                // ---------- 8. the wardrobe ----------
                Scene = 8;
                Clear();
                var wc = new Vector3(-5, 0, 18.2f); wc.y = Ground(wc + up * 0.5f);
                Put(hero, wc + new Vector3(0, 0, -5.6f), wc);
                Cut(wc + new Vector3(0, 6.6f, -9.2f), wc + new Vector3(0, 0.3f, 0.2f), 52);
                Circle(() => wc, 3.3f, UIKit.Pink);
                Arrow(() => wc + up * 0.4f, "ГАРДЕРОБ", UIKit.Pink);
                Cap("ВСТАНЬ НА ЛИЦО НА ПОЛУ", UIKit.Pink);
                IEnumerator ToWardrobe()
                {
                    yield return Wait(1.6f);
                    yield return Walk(hero, wc + new Vector3(0, 0, 0.1f), 5f);
                    hero.Face(wc + new Vector3(0, 0, -10));
                    AudioFx.Play("magic", 0.7f);
                    Effects.Emit("magic", hero.pos + up, 24, 0.6f, up);
                }
                yield return Par(Say("t15", "Видишь весёлое лицо на полу? Это гардероб! Встань на него — и наряжай чубрика."), ToWardrobe());
                Clear();
                Cap("ШАПКИ · КОСТЮМЫ · КРЫЛЬЯ · ПИТОМЦЫ", UIKit.Pink);
                Glide(hero.pos + hero.Fwd * 4.4f + up * 1.9f + LR * 0.4f, hero.pos + up * 1.0f, 1.2f, 48);
                IEnumerator Outfits()
                {
                    for (int i = 0; i < 4; i++)
                    {
                        yield return Wait(0.25f);
                        Dress(hero, Rescue.LookOf("tutorial" + i));
                        AudioFx.Play("click", 0.8f);
                        Effects.Emit("magic", hero.pos + up, 18, 0.5f, up);
                        hero.a.Land(0.5f);
                        yield return Wait(0.8f);
                    }
                }
                yield return Par(Say("t16", "Шапки, костюмы, крылья, питомцы — выбирай, что нравится!"), Outfits());
                Dress(hero, heroLook);

                // ---------- 9. lucky blocks ----------
                Scene = 9;
                Clear();
                var lp = wc + new Vector3(0, 0, -4.6f); lp.y = Ground(lp + up * 0.5f);
                Put(hero, lp, lp + new Vector3(0, 0, -10));
                Cut(lp + new Vector3(1.2f, 1.9f, -6.2f), lp + up * 1.7f, 50);
                Transform block = null;
                void LuckyBlock(bool rainbow)
                {
                    Make(() =>
                    {
                        block = Node("DemoLucky", props, lp + up * 2.85f);
                        var cube = B(block, Vector3.zero, Vector3.one * 0.95f, rainbow ? G("FF5FA2", 1.2f) : M("FFC83A", 0.6f, 0.3f));
                        if (rainbow) { var h = cube.AddComponent<Hue>(); h.speed = 0.6f; h.glow = 0.8f; }
                        foreach (var sd in new[] { -1, 1 })
                        {
                            var q = Prim.Text(block, "?", new Vector3(0, -0.05f, sd * 0.49f), 7, Color.white);
                            q.transform.localEulerAngles = new Vector3(0, sd > 0 ? 180 : 0, 0);
                        }
                        block.gameObject.AddComponent<Spin>().speed = 50;
                    });
                    var b0 = block;
                    Tick(b0, k => b0.localScale = Vector3.one * Mathf.Min(1, k * 4), 0.3f);
                }
                LuckyBlock(false);
                var lpanel = UIKit.Img(ov, UIKit.Panel, Color.white, "MockLucky");
                UIKit.Place(lpanel.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26 - safe.z, 40), new Vector2(500, 404));
                var lh = UIKit.Txt(lpanel.transform, "ЛАКИ-БЛОКИ", 36, UIKit.Gold);
                UIKit.Place(lh.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(460, 50));
                for (int i = 0; i < 3; i++)
                {
                    var row = UIKit.Img(lpanel.transform, UIKit.Glass, Color.white, "Row");
                    UIKit.Place(row.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -72 - i * 74), new Vector2(456, 66));
                    var n = UIKit.Txt(row.transform, Lucky.Name[i], 26, i == 2 ? Lucky.RarityColor[3] : Color.white, TextAlignmentOptions.MidlineLeft, false);
                    UIKit.Fill(n.rectTransform, 18, 0, 150, 0);
                    var pr = UIKit.Txt(row.transform, UIKit.Num(Lucky.Price[i]) + " монет", 26, UIKit.Gold, TextAlignmentOptions.MidlineRight, false);
                    UIKit.Fill(pr.rectTransform, 200, 0, 18, 0);
                }
                var openBtn = MockBtn(lpanel.transform, "ОТКРЫТЬ ЛАКИ-БЛОК", UIKit.Orange, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(420, 76), 26);
                Halo(openBtn);
                Cap("НОВЫЕ ВЕЩИ — ИЗ ЛАКИ-БЛОКОВ", UIKit.Gold);
                IEnumerator Pop(bool rainbow)
                {
                    Tap(openBtn);
                    yield return Wait(0.3f);
                    var top = hero.pos; top.y += 1.15f;
                    var g = hero.pos;
                    yield return Arc(hero, top, 0.1f, 0.32f);
                    AudioFx.Play("box", 0.8f);
                    var bp = block ? block.position : lp + up * 2.85f;
                    if (block) Destroy(block.gameObject);
                    Effects.Emit(rainbow ? "rainbow" : "confetti", bp, 40, 0.5f, up * 2);
                    Effects.Emit("star", bp, 12, 0.4f);
                    var l = heroLook.Copy();
                    if (rainbow) { l["wings"] = "wings_0"; l["hat"] = "hat_6"; } else l["hat"] = "hat_4";
                    Dress(hero, l);
                    AudioFx.Play(rainbow ? "win3" : "buy", 0.8f);
                    Effects.Float(rainbow ? "ЛЕГЕНДА!" : "НОВАЯ ВЕЩЬ!", bp + up * 0.4f, rainbow ? Lucky.RarityColor[3] : UIKit.Lime, 4f);
                    yield return Arc(hero, g, 0.05f, 0.3f, false, null);
                    Land(hero, 0.5f);
                }
                IEnumerator Lucky1() { yield return Wait(1.8f); yield return Pop(false); }
                yield return Par(Say("t17", "Новые вещи выпадают из лаки-блоков. Их открывают в гардеробе за монетки."), Lucky1());
                Cap("РАДУЖНЫЙ БЛОК — САМЫЕ РЕДКИЕ ВЕЩИ", Lucky.RarityColor[3]);
                LuckyBlock(true);
                IEnumerator Lucky2() { yield return Wait(1.0f); yield return Pop(true); }
                yield return Par(Say("t18", "Радужный блок дороже, зато в нём самые редкие вещи!"), Lucky2());
                Dress(hero, heroLook);
                for (int i = props.childCount - 1; i >= 0; i--) Destroy(props.GetChild(i).gameObject);

                // ---------- 10. the shop and the premium sets ----------
                Scene = 10;
                Clear();
                var shopFront = new Vector3(-2.9f, 0, -9.4f); shopFront.y = Ground(shopFront + up * 0.5f);
                Put(hero, new Vector3(1.4f, 0, -10.6f), shopFront);
                Cut(new Vector3(0.6f, 2.9f, -12.9f), new Vector3(-6.5f, 2.0f, -13.3f), 62);
                Arrow(() => new Vector3(-3.4f, 5.6f, -9.4f), "МАГАЗИН", UIKit.Red);
                Arrow(() => new Vector3(-3.4f, 6.4f, -17.2f), "ПРЕМИУМ-НАБОРЫ", UIKit.Gold);
                Cap("МАГАЗИН И ПРЕМИУМ-НАБОРЫ", UIKit.Gold);
                IEnumerator ToShop()
                {
                    yield return Wait(0.8f);
                    yield return Walk(hero, shopFront, 5f);
                    hero.Face(shopFront + new Vector3(-5, 0, 0));
                }
                yield return Par(Say("t19", "Это магазин: там сердечки и указатель монет. А рядом — премиум-наборы."), ToShop());
                Clear();
                Glide(new Vector3(0.2f, 2.3f, -6.6f), new Vector3(-4.6f, 1.5f, -9.6f), 1.6f, 52);
                Cap("ПОДОЙДИ И НАЖМИ «ВОЙТИ»", UIKit.Orange);
                Keys("b:ВОЙТИ", "t:или", "k:E", "t:или", "B");
                Halo(chips[0]);
                IEnumerator Enter()
                {
                    yield return Wait(1.0f);
                    Press(0, 2);
                    yield return Wait(1.6f);
                    Press(4); Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("punch");
                    Effects.Emit("star", hero.pos + hero.Fwd * 1.1f + up * 1.0f, 12, 0.4f);
                    Effects.Float("ВХОДИ!", hero.pos + hero.Fwd * 1.5f + up * 2.0f, UIKit.Gold, 4f);
                }
                yield return Par(Say("t20", "Подойди и нажми «Войти». Или просто ударь по прилавку!"), Enter());

                // ---------- 11. portals and gates ----------
                Scene = 11;
                Clear();
                var portal = new Vector3(18, 0, 19.5f); portal.y = Ground(portal + new Vector3(0, 0.5f, -2));
                Put(hero, portal + new Vector3(0, 0, -6.5f), portal);
                Cut(portal + new Vector3(2.4f, 2.4f, -10.5f), portal + new Vector3(0, 2.8f, 0), 55);
                Arrow(() => portal + new Vector3(0, 7.4f, 0), "ПОРТАЛ В МИР 2", UIKit.Violet);
                Cap("ПОРТАЛЫ — В ДРУГИЕ МИРЫ", UIKit.Violet);
                IEnumerator ToPortal()
                {
                    yield return Wait(0.9f);
                    yield return Walk(hero, portal + new Vector3(0, 0, -0.4f), 5.5f);
                    AudioFx.Play("portal");
                    Effects.Emit("magic", portal + up * 2.5f, 40, 1.2f, up);
                    yield return Act(0.3f, t => hero.scale = 1 - t / 0.3f);
                    hero.t.gameObject.SetActive(false);
                }
                yield return Par(Say("t21", "Порталы ведут в другие миры. Зайди в арку — и ты уже там!"), ToPortal());
                Clear();
                LobbyGate gate = null;
                foreach (var g in Lobby.Gates) if (g && !g.IsOpen) { gate = g; break; }
                if (gate == null && Lobby.Gates.Count > 0) gate = Lobby.Gates[0];
                if (gate)
                {
                    var gt = gate.transform;
                    var gp = gt.TransformPoint(new Vector3(0, 0, -2.8f));
                    Put(hero, gp, gt.position);
                    Cut(gt.TransformPoint(new Vector3(1.5f, 2.6f, -8.2f)), gt.TransformPoint(new Vector3(0, 2.7f, 0)), 54);
                    Arrow(() => gt.TransformPoint(new Vector3(0, 5.0f, -0.3f)), gate.IsOpen ? "ВОРОТА" : "СПАСИ " + gate.need + " ДРУЗЕЙ", UIKit.Cyan);
                }
                else Put(hero, portal + new Vector3(0, 0, -4), portal);
                hero.a.emote = 6; hero.a.PlayEmote();
                Cap("ВОРОТА ОТКРЫВАЮТ СПАСЁННЫЕ ДРУЗЬЯ", UIKit.Cyan);
                yield return Say("t22", "Некоторые дороги закрыты воротами. На воротах написано, сколько друзей нужно спасти.");
                hero.a.StopEmote();
                Dress(hero, heroLook);

                // ---------- 12. keys and cages ----------
                Scene = 12;
                Clear();
                string id1 = "w2c1", id2 = "w2c2";
                string n1 = Rescue.NameOf(id1), n2 = Rescue.NameOf(id2);
                float cageYaw = Mathf.Atan2(LR.x * side, LR.z * side) * Mathf.Rad2Deg;
                var c1 = DemoCage(LP(1.8f), cageYaw, id1, n1.ToUpper());
                var c2 = DemoCage(LP(5.6f), cageYaw, id2, n2.ToUpper());
                var key1 = DemoKey(LP(-3.2f) + up * 1.0f, "КЛЮЧ: " + n1.ToUpper());
                var key2 = DemoKey(LP(-0.6f, -1.8f * side) + up * 1.0f, "КЛЮЧ: " + n2.ToUpper());
                Put(hero, LP(-6.5f), LP(7));
                Cut(LP(-0.2f) + LR * side * 9.5f + up * 3.0f, LP(0.4f) + up * 1.2f, 54, true);
                Cap("У КАЖДОГО ДРУГА — СВОЙ КЛЮЧ", UIKit.Gold);
                IEnumerator Rescue1()
                {
                    yield return Wait(0.6f);
                    yield return Walk(hero, LP(-3.2f), 6f);
                    AudioFx.Play("token", 0.8f);
                    Effects.Emit("gold", key1.position, 16, 0.4f);
                    var kk = key1;
                    Tick(kk, k => { if (kk && hero != null) kk.position = Vector3.Lerp(kk.position, hero.pos + up * 2.3f, 1 - Mathf.Exp(-12 * Dt)); });
                    yield return Walk(hero, LP(0.55f), 6f);
                    hero.Face(LP(1.8f));
                    yield return Wait(0.3f);
                    Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("open"); AudioFx.Play("win2", 0.6f);
                    if (kk) Destroy(kk.gameObject);
                    Burst(c1.bars, "5B6370", 10, 0.22f);
                    Effects.Emit("confetti", c1.cage.position + up * 1.5f, 40, 0.6f, up * 2);
                    Effects.Float("СПАСЁН!", c1.cage.position + up * 2.6f, UIKit.Lime, 4f);
                    var fm = c1.friend.transform;
                    var fa = c1.friend.GetComponent<ChubrikAnim>();
                    if (fa) { fa.StopEmote(); fa.Preview = false; fa.emote = 1; }
                    yield return Act(0.5f, t => { float u = t / 0.5f; fm.localPosition = new Vector3(0, 0.2f + 4 * 1.2f * u * (1 - u), u * 1.6f); });
                    if (fa) fa.PlayEmote();
                }
                Arrow(() => key2 ? key2.position + up * 1.3f : L0, null, UIKit.Gold);
                yield return Par(Say("t23", "Друзья сидят в клетках. У каждого друга свой ключ. Найди его и ударь по клетке!"), Rescue1());
                Clear();
                Cap("СПАСЁН РАНЬШЕ? ЖДЁТ В ПРОЗРАЧНОЙ КЛЕТКЕ", UIKit.Cyan);
                Destroy(c1.cage.gameObject);
                var c1b = DemoCage(LP(1.8f), cageYaw, id1, n1.ToUpper() + " — СНОВА ЖДЁТ");
                Rescue.Ghost(c1b.bars);
                Effects.Emit("magic", c1b.cage.position + up, 24, 0.8f, up);
                Glide(LP(0.6f) + LR * side * 5.2f + up * 2.2f, LP(1.8f) + up * 1.2f, 1.4f, 50);
                Circle(() => c1b.cage ? c1b.cage.position : L0, 1.3f, UIKit.Cyan);
                hero.Face(LP(1.8f));
                yield return Say("t24", "Спасённый друг снова ждёт в прозрачной клетке. Его можно спасти ещё раз.");
                for (int i = props.childCount - 1; i >= 0; i--) Destroy(props.GetChild(i).gameObject);

                // ---------- 13. the boss arena pad ----------
                Scene = 13;
                Clear();
                var padP = LP(-3.5f);
                Make(() =>
                {
                    var pt = Node("DemoBossPad", props, padP);
                    Ring(pt, up * 1.8f, 1.5f, 0.15f, G("FF5252", 2f)).transform.rotation = Quaternion.LookRotation(LR) * Quaternion.Euler(90, 0, 0);
                    Cy(pt, up * 0.05f, new Vector3(2.4f, 0.05f, 2.4f), G("FF5252", 0.8f));
                    var l = Prim.Text(pt, "АРЕНА БОССА", up * 3.8f, 3.5f, Hex("FF5252"));
                    l.gameObject.AddComponent<Billboard>();
                });
                Put(hero, LP(-7), LP(7));
                SideTrack(8f, 2.6f, 1.2f);
                Cap("АРЕНА БОССА — В КАЖДОМ МИРЕ", UIKit.Red);
                Circle(() => padP, 1.4f, UIKit.Red);
                IEnumerator ToPad()
                {
                    yield return Wait(1.6f);
                    yield return Walk(hero, padP, 5f);
                    AudioFx.Play("portal");
                    Effects.Emit("fire", padP + up * 0.5f, 30, 1f, up * 2);
                    hero.Face(LP(7));
                }
                yield return Par(Say("t25", "В каждом мире есть площадка «Арена босса». Встань на неё — и начнётся битва!"), ToPad());
                Clear();
                Transform heart = null;
                var heartP = LP(4.6f) + up * 1.7f;
                Make(() =>
                {
                    heart = Node("DemoHeart", props, heartP);
                    Msh(Meshes.Heart(), heart, Vector3.zero, Vector3.one * 1.3f, G("FF2D55", 2f));
                    heart.gameObject.AddComponent<Spin>().speed = 70;
                    heart.gameObject.AddComponent<Pulse>().amount = 0.12f;
                });
                Put(hero, LP(-1.2f), LP(7));
                SideTrack(8.5f, 2.6f, 1.6f);
                Cap("ПЕРЕПРЫГИВАЙ ВОЛНЫ!", UIKit.Orange);
                IEnumerator Wave()
                {
                    Transform w = null;
                    Make(() => w = Ring(props, LP(4.6f) + up * 0.15f, 1f, 0.12f, G("FF8A3D", 2f)).transform);
                    AudioFx.Play("bonk", 0.6f);
                    yield return Act(1.4f, t => { if (w) w.localScale = new Vector3(0.5f + t * 6.5f, 1, 0.5f + t * 6.5f); });
                    if (w) Destroy(w.gameObject);
                }
                IEnumerator Fight()
                {
                    yield return Wait(0.3f);
                    for (int i = 0; i < 2; i++)
                    {
                        StartCoroutine(Wave());
                        yield return Wait(0.55f);
                        yield return Arc(hero, hero.pos, 1.2f, 0.55f);
                        Land(hero, 0.5f);
                        yield return Wait(0.25f);
                    }
                    Cap("А ТЕПЕРЬ — В СЕРДЦЕ!", UIKit.Red);
                    yield return Walk(hero, LP(3.4f), 6f);
                    Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("punch"); AudioFx.Play("zap", 0.6f);
                    Effects.Emit("star", heartP, 20, 0.5f);
                    Effects.Float("БАХ!", heartP + up * 1.2f, UIKit.Gold, 4f);
                    var hp = heart;
                    if (hp) Tick(hp, k => { if (hp) hp.position = heartP + UnityEngine.Random.insideUnitSphere * 0.15f * (1 - k / 0.5f); }, 0.5f);
                }
                yield return Par(Say("t26", "Перепрыгивай волны, а когда босс устанет, бей в светящееся сердце!"), Fight());
                for (int i = props.childCount - 1; i >= 0; i--) Destroy(props.GetChild(i).gameObject);

                // ---------- 14. football ----------
                Scene = 14;
                Clear();
                var fp = Football.PadPos;
                if (fp == Vector3.zero) fp = LP(0);
                fp.y = Ground(fp + up * 0.5f);
                Vector3 fromDir = Vector3.back;
                foreach (var dir in new[] { Vector3.back, Vector3.right, Vector3.left, Vector3.forward })
                    if (!Physics.Raycast(fp + up * 1.0f, dir, 5f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { fromDir = dir; break; }
                var fr = Vector3.Cross(Vector3.up, fromDir);
                Put(hero, fp + fromDir * 4.2f, fp);
                Cut(fp + fromDir * 7.5f + fr * 3.5f + up * 3.4f, fp + up * 1.2f, 52, true);
                Circle(() => fp, 1.8f, UIKit.Lime);
                Arrow(() => fp + up * 3.9f, "ФУТБОЛ", UIKit.Lime);
                Cap("10 УРОВНЕЙ ИЛИ МАТЧ С ДРУГОМ", UIKit.Lime);
                IEnumerator ToBall()
                {
                    yield return Wait(1.0f);
                    yield return Walk(hero, fp + fromDir * 1.3f, 5f);
                    hero.Face(fp);
                    yield return Wait(0.3f);
                    Swing(hero);
                    yield return Wait(0.15f);
                    AudioFx.Play("punch", 0.7f);
                    Effects.Emit("star", fp + up * 1.4f, 10, 0.5f);
                }
                yield return Par(Say("t27", "А вот зелёная площадка с мячом — это футбол! Играй уровни или матч с другом."), ToBall());

                // ---------- 15. players online: invite a friend ----------
                Scene = 15;
                Clear();
                Put(hero, LP(0.6f), camSide);
                pal = MakePup("TutorialFriend", Rescue.LookOf("w2c5"), LP(-0.9f), 0);
                pal.t.gameObject.SetActive(false);
                Cut(LP(-0.2f) + LR * side * 5.4f + up * 1.9f, LP(-0.2f) + up * 1.05f, 50);
                Cap("ИГРА С ДРУГОМ", UIKit.Violet);
                var menuBtn = MockBtn(ov, "МЕНЮ", UIKit.Blue, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-136 - safe.z, -20 - safe.w), new Vector2(150, 66), 26);
                Halo(menuBtn);
                RectTransform win = null, inviteBtn = null, popup = null, accept = null;
                IEnumerator Online1()
                {
                    yield return Wait(1.0f);
                    Tap(menuBtn);
                    yield return Wait(0.3f);
                    win = MockWin("МЕНЮ", 1000, 600);
                    MockRow(win, 0, "Продолжить", null, "ИГРАТЬ", UIKit.Green);
                    var see = MockRow(win, 1, "Игроки онлайн", "Кто сейчас играет — можно прийти к ним", "СМОТРЕТЬ", UIKit.Violet);
                    MockRow(win, 2, "Промокоды", "Напиши секретное слово", "ВВЕСТИ", UIKit.Pink);
                    MockRow(win, 3, "Настройки", null, "ОТКРЫТЬ", UIKit.Blue);
                    Halo(see);
                    yield return Wait(1.7f);
                    Tap(see);
                }
                yield return Par(Say("t28", "Хочешь играть с другом? Открой меню, а потом «Игроки онлайн»."), Online1());
                IEnumerator Online2()
                {
                    CloseWin(win);
                    win = MockWin("ИГРОКИ ОНЛАЙН", 1100, 520);
                    inviteBtn = MockRow(win, 0, "Мира", "Сейчас: лобби", "ПРИГЛАСИТЬ", UIKit.Green);
                    MockRow(win, 1, "Тимур", "Сейчас: Мир 2", "ПРИГЛАСИТЬ", UIKit.Green);
                    MockRow(win, 2, "Заблокировать комнату", "Пока закрыто, тебе не придут приглашения", "ЗАБЛОКИРОВАТЬ", UIKit.Red);
                    Halo(inviteBtn);
                    yield return Wait(1.2f);
                    Tap(inviteBtn);
                    var lbl = inviteBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl) lbl.text = "ЖДЁМ…";
                    yield return Wait(0.8f);
                    CloseWin(win); win = null;
                    var pdim = UIKit.Img(ov, UIKit.White, new Color(0.05f, 0.1f, 0.25f, 0.3f), "MockDim");
                    UIKit.Fill(pdim.rectTransform);
                    var pp = UIKit.Img(pdim.transform, UIKit.Panel, Color.white, "MockInvite");
                    UIKit.Place(pp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(820, 300));
                    popup = pp.rectTransform;
                    var who = UIKit.Txt(pp.transform, "У ДРУГА НА ЭКРАНЕ:", 24, UIKit.Muted);
                    UIKit.Place(who.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(760, 36));
                    var msg = UIKit.Txt(pp.transform, "Тебя зовут играть вместе!", 36, Color.white);
                    UIKit.Place(msg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(760, 80));
                    accept = MockBtn(pp.transform, "ПРИНЯТЬ", UIKit.Green, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-14, 26), new Vector2(330, 86), 30);
                    MockBtn(pp.transform, "ОТКАЗАТЬСЯ", UIKit.Red, new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(14, 26), new Vector2(330, 86), 30);
                    Halo(accept);
                    AudioFx.Play("note", 0.7f);
                    yield return Wait(1.4f);
                    Tap(accept);
                    yield return Wait(0.3f);
                    if (popup && popup.parent) Destroy(popup.parent.gameObject);
                    Put(pal, LP(-0.9f), camSide);
                    pal.Face(hero.pos);
                    hero.Face(pal.pos);
                    AudioFx.Play("portal");
                    Effects.Emit("magic", pal.pos + up, 40, 0.7f, up);
                    yield return Act(0.35f, t => pal.scale = t / 0.35f);
                    pal.scale = 1;
                    Bubble(() => pal.pos + up * 2.2f, "Привет!");
                    StartCoroutine(JumpFwd(hero, 0.01f, 0.8f, 0.45f));
                    yield return JumpFwd(pal, 0.01f, 0.8f, 0.45f);
                }
                yield return Par(Say("t29", "Нажми «Пригласить». Друг нажмёт «Принять» — и вы играете вместе!"), Online2());

                // ---------- 16. voice and chat ----------
                Scene = 16;
                Clear();
                Glide(LP(-0.15f) + LR * side * 4.6f + up * 1.7f, LP(-0.15f) + up * 1.1f, 1.2f, 48);
                Cap("ГОВОРИ ГОЛОСОМ", UIKit.Violet);
                var voiceBtn = MockRound("ГОЛОС", UIKit.Violet, new Vector2(1, 0.5f), new Vector2(-110 - safe.z, 60), 130, 24);
                Halo(voiceBtn);
                IEnumerator Talk()
                {
                    yield return Wait(1.4f);
                    for (int i = 0; i < 4; i++) { Ripple(voiceBtn); yield return Wait(0.3f); }
                    Bubble(() => hero.pos + up * 2.2f, "Го в Мир 2!");
                    AudioFx.Play("note", 0.5f);
                    for (int i = 0; i < 3; i++) { Ripple(voiceBtn); yield return Wait(0.3f); }
                }
                yield return Par(Say("t30", "Вдвоём можно болтать! Держи кнопку «Голос» и говори."), Talk());
                Cap("ИЛИ ПИШИ В ЧАТ", UIKit.Gold);
                var chat = UIKit.Img(ov, UIKit.Panel, new Color(1, 1, 1, 0.95f), "MockChat");
                UIKit.Place(chat.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + safe.x, -16 - safe.w), new Vector2(430, 280));
                var ct = UIKit.Txt(chat.transform, "ЧАТ С ДРУГОМ", 22, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
                UIKit.Place(ct.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -6), new Vector2(400, 32));
                var log = UIKit.Txt(chat.transform, "", 24, Color.white, TextAlignmentOptions.TopLeft, false);
                UIKit.Place(log.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -42), new Vector2(400, 160));
                var inBox = UIKit.Img(chat.transform, UIKit.Glass, Color.white, "Input");
                UIKit.Place(inBox.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 10), new Vector2(320, 54));
                var inTxt = UIKit.Txt(inBox.transform, "", 24, Color.white, TextAlignmentOptions.MidlineLeft, false);
                UIKit.Fill(inTxt.rectTransform, 12, 0, 12, 0);
                var send = MockBtn(chat.transform, "ОТПР", UIKit.Green, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(84, 54), 18);
                Halo(chat.rectTransform);
                IEnumerator Chat()
                {
                    yield return Wait(0.6f);
                    log.text = "<color=#FFC83A>Мира:</color> привет!";
                    AudioFx.Play("note", 0.4f);
                    const string msg = "погнали!";
                    for (int i = 1; i <= msg.Length; i++) { inTxt.text = msg.Substring(0, i); yield return Wait(0.1f); }
                    Tap(send);
                    inTxt.text = "";
                    log.text += "\n<color=#6EDC3C>Ты:</color> " + msg;
                }
                yield return Par(Say("t31", "А ещё можно писать в чат: он слева сверху."), Chat());

                // ---------- 17. promo codes, settings — and the end ----------
                Scene = 17;
                Clear();
                Cap("ПРОМОКОДЫ", UIKit.Pink);
                IEnumerator Promo1()
                {
                    win = MockWin("МЕНЮ", 1000, 600);
                    MockRow(win, 0, "Продолжить", null, "ИГРАТЬ", UIKit.Green);
                    MockRow(win, 1, "Игроки онлайн", "Кто сейчас играет — можно прийти к ним", "СМОТРЕТЬ", UIKit.Violet);
                    var promo = MockRow(win, 2, "Промокоды", "Напиши секретное слово — и случится что-то интересное", "ВВЕСТИ", UIKit.Pink);
                    MockRow(win, 3, "Настройки", null, "ОТКРЫТЬ", UIKit.Blue);
                    Halo(promo);
                    yield return Wait(1.0f);
                    Tap(promo);
                    yield return Wait(0.3f);
                    CloseWin(win);
                    win = MockWin("ПРОМОКОД", 1000, 420);
                    var tx = UIKit.Txt(win, "Введите промокод — секретное слово, и случится что-то интересное.", 28, Color.white, TextAlignmentOptions.TopLeft, false);
                    UIKit.Place(tx.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -96), new Vector2(930, 80));
                    var box = UIKit.Img(win, UIKit.Glass, Color.white, "Input");
                    UIKit.Place(box.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(34, 60), new Vector2(580, 86));
                    var typed = UIKit.Txt(box.transform, "", 40, Color.white, TextAlignmentOptions.MidlineLeft, false);
                    UIKit.Fill(typed.rectTransform, 20, 0, 20, 0);
                    var go = MockBtn(win, "АКТИВИРОВАТЬ", UIKit.Green, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-34, 60), new Vector2(300, 86), 30);
                    for (int i = 1; i <= 6; i++) { typed.text = new string('*', i); AudioFx.Play("click", 0.3f); yield return Wait(0.18f); }
                    Halo(go);
                    yield return Wait(0.5f);
                    Tap(go);
                    yield return Wait(0.3f);
                    CloseWin(win); win = null;
                    AudioFx.Play("magic");
                    Effects.Emit("magic", hero.pos + up, 40, 0.8f, up * 1.5f);
                    Effects.Emit("rainbow", pal.pos + up, 30, 0.8f, up * 1.5f);
                    Effects.Float("?!", hero.pos + up * 2.3f, UIKit.Pink, 5f);
                }
                yield return Par(Say("t32", "В меню есть промокоды. Напиши секретное слово — и случится что-то интересное!"), Promo1());
                Cap("НАСТРОЙКИ", UIKit.Blue);
                IEnumerator Settings1()
                {
                    win = MockWin("НАСТРОЙКИ", 1000, 600);
                    MockRow(win, 0, "Твой ник: " + Online.Nick, "Его видят друзья и таблица рекордов", "СОХРАНИТЬ", UIKit.Green);
                    MockRow(win, 1, "Громкость", "0% · 25% · 50% · 80% · 100%", null, UIKit.Blue);
                    MockRow(win, 2, "Музыка", null, "ВКЛ", UIKit.Green);
                    var tut = MockRow(win, 3, "Обучение", "Видео с голосом: как играть", "СМОТРЕТЬ", UIKit.Green);
                    yield return Wait(2.2f);
                    Halo(tut);
                    yield return Wait(1.8f);
                    CloseWin(win); win = null;
                }
                yield return Par(Say("t33", "А в настройках — громкость, музыка и твой ник. Там же и это обучение."), Settings1());
                Clear();
                Title("УДАЧИ, ЧУБРИК!", "беги играть!");
                AudioFx.Play("win4", 0.7f);
                hero.a.emote = Mathf.Max(0, heroLook.Idx("emote")); hero.a.PlayEmote();
                pal.a.emote = 1; pal.a.PlayEmote();
                Effects.Emit("confetti", LP(-0.15f) + up * 2.5f, 60, 1.2f, up * 2);
                var c = LP(-0.15f) + up * 1.1f;
                float a0 = Mathf.Atan2(LR.x * side, LR.z * side);
                camRig = () =>
                {
                    float a = a0 + Mathf.Sin(clock * 0.3f) * 0.6f;
                    return (c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 5.2f + up * 0.9f, c);
                };
                safeCam = true;
                yield return Say("t34", "Вот и всё! Беги играть. Удачи, чубрик!", 0.4f);
                subBar.SetActive(false);
                talking = false;
                var again = UIKit.Button(transform, "ЕЩЁ РАЗ", UIKit.Green, Tutorial.Play, 32, "Again");
                UIKit.Place(again.Bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(300, 86));
                while (true)
                {
                    // the chubriks keep dancing until the cross or «ЕЩЁ РАЗ»
                    yield return Wait(5.5f);
                    if (!hero.a.emoting) hero.a.PlayEmote();
                    if (pal != null && !pal.a.emoting) pal.a.PlayEmote();
                }
            }
        }
    }
}
