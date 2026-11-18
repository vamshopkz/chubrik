using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 18.11 promo code «Лукашарики»: a real 3D cartoon with voices (16 scenes, ~2.5 min).
    /// Round little balls — Лукаш, Пухля, Бублик, Профессор and Чихуня — grow a giant onion, cook onion soup,
    /// cry onion tears, put birds to sleep with onion breath, chase the runaway (and very much alive) onion
    /// through the valley, over the river and onto a carousel gone wild — and make friends with it in the end.
    /// The set is built from code far away from the game (<see cref="Origin"/>) and filmed by its own camera;
    /// the player camera, lights, fog and music are put back after <see cref="Stop"/>.
    /// Voices: Resources/Sounds/cartoon/lNN.wav (macOS «say», Milena), pitch per character.
    /// </summary>
    public static class Cartoon
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        public static int Scene { get; private set; } = -1;
        public const int Scenes = 16;
        /// <summary>Speeds the whole cartoon up (animation, waits, voices); tests set 4.</summary>
        public static float Speed = 1f;
        static readonly Vector3 Origin = new Vector3(0, 600, -6000);

        public static void Play()
        {
            Stop();
            Menus.Unlock();
            if (PlayerCtl.I) PlayerCtl.I.Frozen = true;
            var bg = UIKit.Img(UIRoot.TopLayer, UIKit.White, new Color(0, 0, 0, 0), "Cartoon", true);
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

        // ---------------- what the cartoon changes in the game (put back by Restore) ----------------
        static bool saved;
        static GameObject set;
        static Camera gameCam;
        static int gameMask;
        static bool gamePost;
        static readonly List<Light> offLights = new List<Light>();
        static Light oldSun;
        static bool oldFog;
        static AmbientMode oldAmb;
        static Color ambSky, ambEq, ambGr;

        static void Grab(GameObject newSet, Light sun)
        {
            Restore();
            saved = true; set = newSet;
            gameCam = PlayerCtl.I ? PlayerCtl.I.Cam : null;
            if (gameCam)
            {
                // the game camera keeps running (Camera.main stays valid) but draws nothing
                gameMask = gameCam.cullingMask; gameCam.cullingMask = 0;
                var d = gameCam.GetUniversalAdditionalCameraData();
                gamePost = d.renderPostProcessing; d.renderPostProcessing = false;
            }
            offLights.Clear();
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.enabled && l.type == LightType.Directional && l != sun) { l.enabled = false; offLights.Add(l); }
            oldSun = RenderSettings.sun; oldFog = RenderSettings.fog; oldAmb = RenderSettings.ambientMode;
            ambSky = RenderSettings.ambientSkyColor; ambEq = RenderSettings.ambientEquatorColor; ambGr = RenderSettings.ambientGroundColor;
            RenderSettings.sun = sun;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("CFE9FF");
            RenderSettings.ambientEquatorColor = Hex("D8F0D0");
            RenderSettings.ambientGroundColor = Hex("8FA86A");
            AudioFx.SetMusic(false);
        }

        static void Restore()
        {
            if (set) UnityEngine.Object.Destroy(set);
            set = null;
            if (!saved) return;
            saved = false;
            if (gameCam)
            {
                gameCam.cullingMask = gameMask;
                gameCam.GetUniversalAdditionalCameraData().renderPostProcessing = gamePost;
            }
            gameCam = null;
            foreach (var l in offLights) if (l) l.enabled = true;
            offLights.Clear();
            RenderSettings.sun = oldSun; RenderSettings.fog = oldFog; RenderSettings.ambientMode = oldAmb;
            RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGr;
            AudioFx.SetMusic(Save.P.music);
        }

        // ---------------- a cheerful little tune (synthesized once) ----------------
        static AudioClip tune;
        static AudioClip Tune()
        {
            if (tune) return tune;
            const int SR = 22050;
            float beat = 60f / 132f, e8 = beat / 2;
            int[] mel =
            {
                0, 4, 7, 4, 12, 7, 4, 7,     9, -1, 9, 7, 4, -1, 0, -1,
                5, 9, 12, 9, 5, 9, 12, 14,   11, -1, 7, -1, 2, 4, 5, 7,
                12, -1, 7, 9, 7, 4, 0, 4,    4, 2, 0, 2, 4, -1, 9, -1,
                5, 4, 5, 9, 12, 9, 5, 2,     7, -1, 11, -1, 12, -1, -1, -1,
            };
            int[] roots = { 0, -3, 5, 7 };
            int n = (int)(SR * 32 * beat);
            var d = new float[n];
            void Note(float start, float f, float amp, float decay, float len, bool soft)
            {
                int s0 = (int)(start * SR), cnt = (int)(len * SR);
                for (int i = 0; i < cnt; i++)
                {
                    float t = (float)i / SR;
                    float w = Mathf.Sin(2 * Mathf.PI * f * t) + (soft ? 0.15f : 0.3f) * Mathf.Sin(4 * Mathf.PI * f * t);
                    d[(s0 + i) % n] += w * amp * Mathf.Exp(-t * decay) * Mathf.Min(1, t * 300);
                }
            }
            for (int k = 0; k < mel.Length; k++)
                if (mel[k] >= 0) Note(k * e8, 523.25f * Mathf.Pow(2, mel[k] / 12f), 0.2f, 6.5f, 0.5f, false);
            for (int b = 0; b < 32; b++)
            {
                int root = roots[(b / 4) % 4];
                int nt = b % 2 == 0 ? root : root + 7;
                Note(b * beat, 130.81f * Mathf.Pow(2, nt / 12f), b % 2 == 0 ? 0.32f : 0.2f, 5f, 0.42f, true);
            }
            var rnd = new System.Random(7);
            for (int k = 1; k < 64; k += 2)
            {
                int s0 = (int)(k * e8 * SR);
                for (int i = 0; i < SR / 25; i++) d[(s0 + i) % n] += (float)(rnd.NextDouble() * 2 - 1) * 0.06f * Mathf.Exp(-i / (SR * 0.012f));
            }
            float peak = 0.001f;
            foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < n; i++) d[i] *= 0.8f / peak;
            tune = AudioClip.Create("cartoon_tune", n, 1, SR, false);
            tune.SetData(d, 0);
            return tune;
        }

        // ---------------- a character: a round ball with eyes that blink, a mouth that talks, squash-and-stretch ----------------
        class Toon
        {
            public string name;
            public float r;
            public Transform root, sq, roll, eyeL, eyeR, pupL, pupR, mouth, top, nose, wingL, wingR;
            readonly Vector3 eyeBase, mouthBase, noseBase;
            public Vector3 pos;
            public float yaw, hop, spin, tilt, lean, puff = 1, stretch = 1, bob, noseK = 1, flap;
            float squash = 1, sqV, blinkT, blinkLeft, bobH;
            readonly float ph;
            public bool talk, crying;
            public int mood;        // 0 smile, 1 laugh, 2 cry, 3 dizzy, 4 asleep, 5 shock, 6 chew
            public Vector2 look;
            public float zT;        // «Z» timer while asleep

            public Toon(Transform parent, string name, string hex, float r, string kind)
            {
                this.name = name; this.r = r;
                ph = UnityEngine.Random.value * 10; blinkT = 0.5f + UnityEngine.Random.value * 2;
                root = Node(name, parent, Vector3.zero);
                sq = Node("Squash", root, Vector3.zero);
                roll = Node("Roll", sq, new Vector3(0, r, 0));
                var c = Hex(hex);
                var col = M(hex, kind == "onion" ? 0.55f : 0.45f);
                var dark = M(ColorUtility.ToHtmlStringRGB(c * 0.72f), 0.3f);
                S(roll, Vector3.zero, 2 * r, col);
                bool bird = kind == "bird", onion = kind == "onion";
                float es = onion ? 0.8f : bird ? 1.15f : 1f;
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    var dir = new Vector3(sd * 0.36f, 0.24f, 0.9f).normalized;
                    var e = S(roll, dir * r * 0.9f, new Vector3(0.36f, 0.44f, 0.26f) * r * es, M("FFFFFF", 0.6f)).transform;
                    e.localRotation = Quaternion.LookRotation(dir);
                    var p = S(e, new Vector3(0, 0, 0.36f), new Vector3(0.55f, 0.58f, 0.5f), M("1A1A2A", 0.7f)).transform;
                    S(p, new Vector3(0.22f, 0.25f, 0.42f), new Vector3(0.3f, 0.3f, 0.3f), M("FFFFFF", 0.8f));
                    if (sd < 0) { eyeL = e; pupL = p; } else { eyeR = e; pupR = p; }
                }
                eyeBase = eyeL.localScale;
                var md = new Vector3(0, -0.28f, 0.96f).normalized;
                if (bird)
                {
                    Spike(roll, new Vector3(0, -0.05f, 0.8f) * r, new Vector3(0, -0.12f, 1.55f) * r, 0.42f * r, M("FF9F1C", 0.4f));
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        var w = Node("Wing", roll, new Vector3(sd * 0.85f * r, 0, -0.05f * r));
                        S(w, new Vector3(sd * 0.2f * r, 0, 0), new Vector3(0.5f, 0.25f, 0.8f) * r, dark);
                        if (sd < 0) wingL = w; else wingR = w;
                    }
                    Cone(roll, new Vector3(0, 0.1f * r, -0.8f * r), new Vector3(0.6f, 0.7f, 0.15f) * r, dark, new Vector3(-110, 0, 0));
                }
                else
                {
                    mouth = S(roll, md * r * 0.93f, Vector3.one, M("6B1A2A", 0.5f)).transform;
                    mouth.localRotation = Quaternion.LookRotation(md);
                    mouthBase = new Vector3(0.42f, 0.3f, 0.2f) * r;
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        var cd = new Vector3(sd * 0.62f, -0.08f, 0.78f).normalized;
                        var ch = S(roll, cd * r * 0.93f, new Vector3(0.24f, 0.15f, 0.08f) * r, M("FF8FA8", 0.3f)).transform;
                        ch.localRotation = Quaternion.LookRotation(cd);
                    }
                }
                if (!onion)
                    for (int sd = -1; sd <= 1; sd += 2)
                        S(sq, new Vector3(sd * 0.4f * r, 0.09f * r, 0.22f * r), new Vector3(0.42f, 0.2f, 0.62f) * r, dark);

                switch (kind)
                {
                    case "sprout":   // Лукаш: a little onion bulb with three green feathers
                        top = Node("Sprout", roll, new Vector3(0, 0.88f * r, 0));
                        S(top, new Vector3(0, 0.12f * r, 0), new Vector3(0.38f, 0.34f, 0.38f) * r, M("F5EBD3", 0.5f));
                        for (int k = -1; k <= 1; k++)
                            Ca(top, new Vector3(k * 0.12f * r, 0.55f * r, 0), new Vector3(0.1f, 0.32f, 0.1f) * r, M("3FAE3A", 0.4f), new Vector3(0, 0, -k * 24));
                        break;
                    case "bow":      // Пухля: a big pink bow
                        top = Node("Bow", roll, new Vector3(0.5f, 0.78f, 0.25f) * r);
                        top.localEulerAngles = new Vector3(0, 0, -35);
                        for (int sd = -1; sd <= 1; sd += 2)
                            S(top, new Vector3(sd * 0.2f * r, 0, 0), new Vector3(0.34f, 0.26f, 0.16f) * r, M("FF2D7A", 0.5f), new Vector3(0, 0, sd * 22));
                        S(top, Vector3.zero, 0.14f * r, M("D81B60", 0.5f));
                        break;
                    case "cap":      // Бублик: a blue cap
                        S(roll, new Vector3(0, 0.62f, -0.05f) * r, new Vector3(1.9f, 1.1f, 1.9f) * r, M("1E5BD8", 0.35f));
                        Cy(roll, new Vector3(0, 0.52f, 0.82f) * r, new Vector3(1.0f, 0.025f, 0.62f) * r, M("1747A8", 0.35f), new Vector3(-12, 0, 0));
                        S(roll, new Vector3(0, 1.15f, -0.05f) * r, 0.16f * r, M("FFFFFF", 0.4f));
                        break;
                    case "glasses":  // Профессор: round glasses, a moustache and a mortarboard
                        for (int sd = -1; sd <= 1; sd += 2)
                        {
                            var dir = new Vector3(sd * 0.36f, 0.24f, 0.9f).normalized;
                            var g = Ring(roll, dir * r * 1.02f, 0.24f * r, 0.03f * r, M("222222", 0.7f, 0.3f));
                            g.transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                            Ca(roll, new Vector3(sd * 0.17f, -0.1f, 0.97f) * r, new Vector3(0.09f, 0.2f, 0.09f) * r, M("EDEDED", 0.3f), new Vector3(0, 0, sd * 70));
                        }
                        Rod(roll, new Vector3(-0.1f, 0.25f, 1.0f) * r, new Vector3(0.1f, 0.25f, 1.0f) * r, 0.03f * r, M("222222", 0.7f, 0.3f));
                        Cy(roll, new Vector3(0, 0.92f, 0) * r, new Vector3(0.75f, 0.14f, 0.75f) * r, M("2B2340", 0.4f));
                        B(roll, new Vector3(0, 1.06f, 0) * r, new Vector3(1.15f, 0.06f, 1.15f) * r, M("2B2340", 0.4f), new Vector3(0, 45, 0));
                        Rod(roll, new Vector3(0, 1.1f, 0) * r, new Vector3(0.55f, 1.08f, 0.25f) * r, 0.03f * r, M("FFC83A", 0.5f));
                        S(roll, new Vector3(0.57f, 0.88f, 0.26f) * r, 0.12f * r, M("FFC83A", 0.5f));
                        break;
                    case "nose":     // Чихуня: a big red nose and a curl
                        nose = S(roll, new Vector3(0, 0.02f, 1.0f) * r, new Vector3(0.36f, 0.32f, 0.36f) * r, M("E53935", 0.6f)).transform;
                        noseBase = nose.localScale;
                        Ring(roll, new Vector3(0, 1.08f, 0) * r, 0.18f * r, 0.045f * r, dark, new Vector3(90, 0, 0));
                        break;
                    case "onion":    // the giant onion: layers, a neck, green feathers and roots
                        for (int k = 0; k < 4; k++) Ring(roll, Vector3.zero, r * 1.0f, r * 0.022f, M("D9B98A", 0.4f), new Vector3(90, k * 45, 0));
                        Cone(roll, new Vector3(0, 0.82f * r, 0), new Vector3(0.55f, 0.75f, 0.55f) * r, M("EAD7AE", 0.5f));
                        top = Node("Leaves", roll, new Vector3(0, 1.45f * r, 0));
                        for (int k = -1; k <= 1; k++)
                            Ca(top, new Vector3(k * 0.15f * r, 0.4f * r, 0), new Vector3(0.12f, 0.5f, 0.12f) * r, M("3FAE3A", 0.4f), new Vector3(0, 0, -k * 18));
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 60 * Mathf.Deg2Rad;
                            var d0 = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            Rod(roll, d0 * 0.15f * r + Vector3.down * 0.95f * r, d0 * 0.38f * r + Vector3.down * 1.18f * r, 0.03f * r, M("D9C7A0", 0.3f));
                        }
                        break;
                }
            }

            public Vector3 Fwd => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            public Vector3 Right => Quaternion.Euler(0, yaw, 0) * Vector3.right;
            public Vector3 Center => pos + Vector3.up * ((hop + bobH) + r * puff);
            public Vector3 Head => pos + Vector3.up * ((hop + bobH) + 2 * r * puff);
            public Vector3 MouthAt => Center + Fwd * r * 0.95f + Vector3.down * r * 0.25f;

            public void Face(Vector3 p) { var d = p - pos; d.y = 0; if (d.sqrMagnitude > 1e-4f) yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
            /// <summary>Squash now (k &gt; 0) or stretch (k &lt; 0); the spring brings it back with a wobble.</summary>
            public void Land(float k = 0.35f) { squash = 1 - k; sqV = 0; }
            public void Reset()
            {
                hop = spin = tilt = lean = bob = flap = 0; stretch = puff = noseK = 1; talk = crying = false; mood = 0; look = Vector2.zero;
            }

            public void Tick(float dt, float time)
            {
                // spring (sub-steps: stable at any Speed)
                int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.008f), 1, 40);
                float h = dt / steps;
                for (int i = 0; i < steps; i++)
                {
                    float acc = -(squash - 1) * 240f - sqV * 13f;
                    sqV += acc * h; squash += sqV * h;
                }
                // idle bouncing
                float nb = bob > 0 ? Mathf.Abs(Mathf.Sin(time * 5.5f + ph)) * bob : 0;
                if (bob > 0 && bobH > bob * 0.15f && nb <= bob * 0.15f) Land(0.18f);
                bobH = nb;
                float s = Mathf.Clamp(squash * stretch, 0.35f, 1.9f), w = 1 / Mathf.Sqrt(s);
                root.localPosition = pos + Vector3.up * (hop + bobH);
                root.localRotation = Quaternion.Euler(0, yaw, 0);
                sq.localScale = new Vector3(w * puff, s * puff, w * puff);
                sq.localRotation = Quaternion.Euler(lean, 0, tilt);
                roll.localRotation = Quaternion.Euler(spin, 0, 0);
                // eyes
                blinkT -= dt;
                if (blinkT <= 0) { blinkLeft = 0.14f; blinkT = 1.6f + UnityEngine.Random.value * 2.6f; }
                blinkLeft -= dt;
                float open = mood == 4 ? 0.07f : blinkLeft > 0 ? 0.08f : 1f;
                if (mood == 1 && !talk) open = Mathf.Min(open, 0.45f);
                float big = mood == 5 ? 1.3f : mood == 2 ? 0.9f : 1f;
                eyeL.localScale = eyeR.localScale = new Vector3(eyeBase.x * big, eyeBase.y * big * open, eyeBase.z);
                Vector2 lk = mood == 3 ? new Vector2(Mathf.Cos(time * 9 + ph), Mathf.Sin(time * 9 + ph)) : look;
                pupL.localPosition = new Vector3(lk.x * 0.17f, lk.y * 0.17f, 0.36f);
                pupR.localPosition = new Vector3((mood == 3 ? -lk.x : lk.x) * 0.17f, lk.y * 0.17f, 0.36f);
                // mouth
                if (mouth)
                {
                    float mh, wd = 1;
                    if (talk) mh = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(time * 15 + ph)) * (0.55f + 0.45f * Mathf.PerlinNoise(time * 6, ph));
                    else switch (mood)
                        {
                            case 1: mh = 0.7f + 0.25f * Mathf.Abs(Mathf.Sin(time * 22)); wd = 1.2f; break;
                            case 2: mh = 0.45f + 0.15f * Mathf.Sin(time * 18); wd = 0.8f; break;
                            case 3: mh = 0.3f; wd = 0.7f + 0.2f * Mathf.Sin(time * 7); break;
                            case 4: mh = 0.12f + 0.15f * Mathf.Max(0, Mathf.Sin(time * 2.5f)); wd = 0.6f; break;
                            case 5: mh = 1.1f; wd = 0.75f; break;
                            case 6: mh = 0.1f + 0.5f * Mathf.Abs(Mathf.Sin(time * 11)); break;
                            default: mh = 0.16f; wd = 1.05f; break;
                        }
                    mouth.localScale = new Vector3(mouthBase.x * wd, mouthBase.y * mh + r * 0.03f, mouthBase.z);
                }
                if (top) top.localRotation = Quaternion.Euler(Mathf.Sin(time * 4.3f + ph) * 5 - sqV * 0.8f, 0, Mathf.Sin(time * 3.1f + ph) * 7 + (top.name == "Bow" ? -35 : 0));
                if (nose) nose.localScale = noseBase * noseK;
                if (wingL)
                {
                    float f = flap > 0 ? Mathf.Sin(time * 45) * 55 * flap : (talk ? Mathf.Sin(time * 20) * 15 : 0);
                    wingL.localRotation = Quaternion.Euler(0, 0, -f);
                    wingR.localRotation = Quaternion.Euler(0, 0, f);
                }
            }
        }

        /// <summary>Who speaks a line: the name in the subtitles, the voice pitch and whose mouth moves.</summary>
        class Who
        {
            public string name; public Color col; public float pitch; public Toon[] toons; public bool chorus;
        }

        /// <summary>A tiny flying bit: a tear, a drop, a puff of pepper, a petal, a «Z».</summary>
        class Bit
        {
            public Transform t; public Vector3 v, rot; public float life, max, grav, s0, grow; public bool bill, keep;
        }

        class Show : MonoBehaviour
        {
            Transform W;            // the set (local coordinates everywhere)
            Camera cam;
            GameObject setGo, subBar, againBtn;
            TextMeshProUGUI sub, speaker, title, title2;
            float titleT = -1, titleKick;
            AudioSource[] vsrc;
            RenderTexture rt;
            RawImage view;
            AudioSource mus;
            Who talking;
            float clock, Dt;
            readonly List<Toon> toons = new List<Toon>();
            readonly List<Toon> birds = new List<Toon>();
            Toon luk, puh, bub, prof, chi, onion;
            Toon[] five;
            Who wN, wL, wP, wB, wPr, wC, wO, wBird, wAll;
            readonly List<Bit> bits = new List<Bit>();
            readonly List<Transform> fieldOnions = new List<Transform>(), bowls = new List<Transform>(), clouds = new List<Transform>();
            Transform pot, soup, spoon, can, shaker, nest, puddle, carSpin, flower, hay, tree1;
            bool stirring, steam, petals, riding;
            float steamT, tearT, petalT, canT;
            float carAngle, carSpeed;
            readonly float[] rideSlot = { 0, 72, 144, 216, 288 };
            int rideCam = -1;
            // camera
            Vector3 camPos, camLook, tgtPos, tgtLook;
            float camRate = 2.5f, shake, fovT = 50;
            Func<Vector3> follow;
            Vector3 followOff;
            Func<(Vector3, Vector3)> camRig;

            static readonly Vector3 G0 = new Vector3(8, 0, 6);          // where the giant onion grows
            static readonly Vector3 P0 = new Vector3(-1, 0, -2);        // the soup pot
            static readonly Vector3 T2 = new Vector3(22.4f, 0, -1.4f);  // the tree Бублик bumps into
            static readonly Vector3 CAR = new Vector3(52, 0, -8);       // the carousel
            static readonly Vector3 HAY = new Vector3(65, 0, 3);
            static readonly Vector3 FO = new Vector3(60.5f, 0, 2.5f);   // where the onion is caught

            void Start()
            {
                BuildUI();
                Build();
                vsrc = new AudioSource[3];
                for (int i = 0; i < 3; i++) { vsrc[i] = gameObject.AddComponent<AudioSource>(); vsrc[i].playOnAwake = false; }
                mus = gameObject.AddComponent<AudioSource>();
                mus.clip = Tune(); mus.loop = true; mus.volume = 0.13f; mus.playOnAwake = false;
                StartCoroutine(Run());
            }

            void OnDestroy()
            {
                // closed some other way than Stop(): still give everything back
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
                title = UIKit.Txt(stage, "", 120, UIKit.Gold);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 170));
                title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 120;
                title2 = UIKit.Txt(stage, "", 46, Color.white);
                UIKit.Place(title2.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 70));
                var bar = UIKit.Img(stage, UIKit.Panel, new Color(0, 0, 0, 0.55f), "SubBar");
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1400, 124));
                subBar = bar.gameObject;
                speaker = UIKit.Txt(bar.transform, "", 28, UIKit.Gold, TextAlignmentOptions.TopLeft);
                UIKit.Place(speaker.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -8), new Vector2(600, 36));
                sub = UIKit.Txt(bar.transform, "", 38, Color.white, TextAlignmentOptions.Left);
                UIKit.Place(sub.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 8), new Vector2(1350, 74));
                sub.enableAutoSizing = true; sub.fontSizeMin = 20; sub.fontSizeMax = 38;
                subBar.SetActive(false);
                var x = UIKit.Button(stage, "X", UIKit.Red, Stop, 40, "Close");
                var safe = UIRoot.Safe();
                UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20 - safe.z, -20 - safe.w), new Vector2(96, 86));
            }

            void Title(string a, string b = "")
            {
                title.text = a ?? ""; title2.text = b ?? "";
                titleT = a == null ? -1 : 0;
                title.rectTransform.localScale = Vector3.zero;
            }

            // ================= the set =================
            void Build()
            {
                int oldLayer = Parts.Layer;
                Parts.Layer = Layers.Preview;
                try { BuildSet(); }
                finally { Parts.Layer = oldLayer; }
            }

            static void NoShadow(GameObject go)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }

            Transform Tree(Vector3 p, float s, string leaf)
            {
                var t = Node("Tree", W, p);
                Cy(t, new Vector3(0, 1.1f * s, 0), new Vector3(0.45f, 1.1f, 0.45f) * s, M("8B5A2B", 0.2f));
                var lm = M(leaf, 0.2f);
                S(t, new Vector3(0, 3.0f * s, 0), 2.4f * s, lm);
                S(t, new Vector3(0.8f, 2.5f, 0.3f) * s, 1.6f * s, lm);
                S(t, new Vector3(-0.7f, 2.6f, -0.2f) * s, 1.7f * s, lm);
                S(t, new Vector3(0.1f, 3.9f, 0.1f) * s, 1.4f * s, lm);
                return t;
            }

            void BuildSet()
            {
                setGo = new GameObject("CartoonSet") { layer = Layers.Preview };
                W = setGo.transform;
                W.position = Origin;

                // camera + sun
                var cg = new GameObject("CartoonCam") { layer = Layers.Preview };
                cg.transform.SetParent(W, false);
                cam = cg.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Hex("8FD3FF");
                cam.cullingMask = 1 << Layers.Preview;
                cam.fieldOfView = 50; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600;
                var gc = PlayerCtl.I ? PlayerCtl.I.Cam : null;
                cam.depth = (gc ? gc.depth : 0) - 5;
                MakeRT();
                camPos = tgtPos = new Vector3(0, 16, -34); camLook = tgtLook = new Vector3(2, 1, 6);
                var cd = cam.GetUniversalAdditionalCameraData();
                if (gc)
                {
                    var gd = gc.GetUniversalAdditionalCameraData();
                    cd.renderPostProcessing = gd.renderPostProcessing;
                    cd.antialiasing = gd.antialiasing;
                }
                var lg = new GameObject("CartoonSun");
                lg.transform.SetParent(W, false);
                lg.transform.rotation = Quaternion.Euler(50, -30, 0);
                var sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional; sun.color = Hex("FFF4DE"); sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.55f;
                Grab(setGo, sun);

                // ground, hills, sun, clouds
                Cy(W, new Vector3(25, -0.5f, 0), new Vector3(260, 0.5f, 260), M("7ED957", 0.12f));
                S(W, new Vector3(-40, -20, 80), new Vector3(110, 60, 70), M("6CC94A", 0.1f));
                S(W, new Vector3(30, -25, 95), new Vector3(130, 70, 70), M("5DBB45", 0.1f));
                S(W, new Vector3(95, -22, 70), new Vector3(100, 55, 60), M("79D35A", 0.1f));
                S(W, new Vector3(-95, -15, 40), new Vector3(80, 45, 80), M("66C24C", 0.1f));
                S(W, new Vector3(60, -18, -110), new Vector3(140, 50, 60), M("6CC94A", 0.1f));
                NoShadow(S(W, new Vector3(-80, 95, 230), 26, G("FFE14D", 1.4f)));
                var rnd = new System.Random(5);
                float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
                for (int k = 0; k < 9; k++)
                {
                    var c = Node("Cloud", W, new Vector3(-70 + k * 22 + Rf(-5, 5), Rf(26, 40), Rf(60, 120)));
                    for (int j = 0; j < 4; j++) S(c, new Vector3(j * 3.2f - 4.8f, (j % 2) * 1.4f, Rf(-1, 1)), new Vector3(6, 4, 4) * Rf(0.8f, 1.3f), M("FFFFFF", 0.05f));
                    NoShadow(c.gameObject);
                    clouds.Add(c);
                }

                // the little house
                var h = Node("House", W, new Vector3(-8, 0, 7), new Vector3(0, 15, 0));
                B(h, new Vector3(0, 1.25f, 0), new Vector3(3.2f, 2.5f, 3f), M("FFD27A", 0.2f));
                Cone(h, new Vector3(0, 2.45f, 0), new Vector3(4.8f, 2.4f, 4.8f), M("E5484D", 0.3f));
                B(h, new Vector3(0.55f, 0.8f, -1.52f), new Vector3(0.9f, 1.6f, 0.08f), M("8A5A2B", 0.2f));
                S(h, new Vector3(0.85f, 0.8f, -1.58f), 0.12f, M("FFC83A", 0.6f, 0.5f));
                B(h, new Vector3(-0.8f, 1.5f, -1.52f), new Vector3(0.75f, 0.75f, 0.06f), M("BDE9FF", 0.9f));
                B(h, new Vector3(-0.8f, 1.5f, -1.56f), new Vector3(0.82f, 0.08f, 0.04f), M("FFFFFF", 0.3f));
                B(h, new Vector3(-0.8f, 1.5f, -1.56f), new Vector3(0.08f, 0.82f, 0.04f), M("FFFFFF", 0.3f));
                B(h, new Vector3(0.9f, 3.6f, 0.5f), new Vector3(0.5f, 1.2f, 0.5f), M("B5523B", 0.2f));

                // trees: the bird tree (a long branch), the tree Бублик bumps into and a few more
                tree1 = Tree(new Vector3(-3.5f, 0, 9), 1.2f, "4CAF50");
                Rod(tree1, new Vector3(0.3f, 1.8f, 0), new Vector3(2.6f, 2.0f, -0.3f), 0.16f, M("8B5A2B", 0.2f));
                S(tree1, new Vector3(2.75f, 2.15f, -0.3f), 0.5f, M("57B85A", 0.2f));
                Tree(T2, 1f, "43A047");
                foreach (var p in new[] { new Vector3(-16, 0, 14), new Vector3(-21, 0, 4), new Vector3(-14, 0, 22), new Vector3(13, 0, 18), new Vector3(19, 0, 12),
                                          new Vector3(27, 0, 15), new Vector3(41, 0, 10), new Vector3(47, 0, 17), new Vector3(58, 0, 15), new Vector3(71, 0, 8),
                                          new Vector3(73, 0, -12), new Vector3(5, 0, -24), new Vector3(30, 0, -27), new Vector3(62, 0, -25), new Vector3(-12, 0, -16) })
                    Tree(p, Rf(0.85f, 1.25f), rnd.Next(2) == 0 ? "4CAF50" : "66BB6A");
                // flowers and bushes
                string[] fc = { "FF5FA2", "FFE14D", "FFFFFF", "B266FF", "FF7043", "5FD3FF" };
                for (int k = 0; k < 90; k++)
                {
                    var p = new Vector3(Rf(-20, 75), 0, Rf(-20, 22));
                    if (Mathf.Abs(p.x - 36) < 3.5f) continue;   // not in the river
                    var f = Node("Flower", W, p);
                    Cy(f, new Vector3(0, 0.15f, 0), new Vector3(0.04f, 0.15f, 0.04f), M("3FAE3A", 0.2f));
                    S(f, new Vector3(0, 0.32f, 0), 0.18f, M(fc[k % fc.Length], 0.3f));
                    NoShadow(f.gameObject);
                }
                foreach (var p in new[] { new Vector3(57.5f, 0, 1), new Vector3(-11, 0, 2), new Vector3(15, 0, 9), new Vector3(44, 0, 3), new Vector3(68, 0, -4) })
                    S(W, p + Vector3.up * 0.5f, new Vector3(1.8f, 1.3f, 1.8f), M("4CAF50", 0.2f));

                // the onion field
                B(W, new Vector3(8, 0.02f, 6), new Vector3(10, 0.06f, 6.4f), M("8B5E3C", 0.1f));
                for (int ix = 0; ix < 8; ix++)
                    for (int iz = 0; iz < 5; iz++)
                    {
                        var p = new Vector3(3.8f + ix * 1.2f, 0, 3.6f + iz * 1.2f);
                        var o = Node("Onion", W, p);
                        S(o, new Vector3(0, 0.15f, 0), new Vector3(0.34f, 0.3f, 0.34f), M("F3E3C3", 0.5f));
                        Cy(o, new Vector3(0.04f, 0.45f, 0), new Vector3(0.04f, 0.22f, 0.04f), M("3FAE3A", 0.3f), new Vector3(0, 0, -12));
                        Cy(o, new Vector3(-0.04f, 0.45f, 0), new Vector3(0.04f, 0.22f, 0.04f), M("3FAE3A", 0.3f), new Vector3(0, 0, 12));
                        NoShadow(o.gameObject);
                        fieldOnions.Add(o);
                    }

                // the soup pot (appears in scene 4)
                pot = Node("Pot", W, P0);
                Cy(pot, new Vector3(0, 1.2f, 0), new Vector3(2.6f, 0.8f, 2.6f), M("3B3F4A", 0.6f, 0.4f));
                Ring(pot, new Vector3(0, 2.0f, 0), 1.3f, 0.12f, M("2A2D35", 0.6f, 0.4f));
                soup = Cy(pot, new Vector3(0, 1.93f, 0), new Vector3(2.45f, 0.03f, 2.45f), M("E8C35A", 0.7f)).transform;
                for (int k = 0; k < 3; k++)
                {
                    float a = k * 120 * Mathf.Deg2Rad;
                    Cy(pot, new Vector3(Mathf.Cos(a) * 0.9f, 0.25f, Mathf.Sin(a) * 0.9f), new Vector3(0.18f, 0.25f, 0.18f), M("2A2D35", 0.5f));
                }
                B(pot, new Vector3(0, 0.12f, 0), new Vector3(1.8f, 0.22f, 0.25f), M("7A4A1F", 0.2f), new Vector3(0, 30, 0));
                B(pot, new Vector3(0, 0.12f, 0), new Vector3(1.8f, 0.22f, 0.25f), M("7A4A1F", 0.2f), new Vector3(0, -30, 0));
                for (int k = 0; k < 3; k++)
                {
                    var fl = Cone(pot, new Vector3((k - 1) * 0.35f, 0.15f, 0), new Vector3(0.5f, 0.75f - (k % 2) * 0.2f, 0.5f), G(k == 1 ? "FFD23F" : "FF8A1E", 1.8f));
                    NoShadow(fl);
                    fl.AddComponent<Pulse>().amount = 0.18f;
                }
                B(pot, new Vector3(2.1f, 0.4f, -0.2f), new Vector3(0.8f, 0.8f, 0.8f), M("C68A4A", 0.2f));   // the crate Бублик stands on
                spoon = Node("Spoon", pot, Vector3.zero);
                Rod(spoon, new Vector3(0.3f, 1.5f, -0.2f), new Vector3(1.55f, 2.7f, -0.2f), 0.1f, M("A0642F", 0.3f));
                S(spoon, new Vector3(0.3f, 1.5f, -0.2f), new Vector3(0.4f, 0.15f, 0.3f), M("A0642F", 0.3f));
                pot.gameObject.SetActive(false);

                // bowls (eating; the middle one becomes Чихуня's boat)
                for (int k = 0; k < 5; k++)
                {
                    var b = Node("Bowl", W, Vector3.zero);
                    S(b, new Vector3(0, 0.02f, 0), new Vector3(0.72f, 0.36f, 0.72f), M("C0703A", 0.4f));
                    Cy(b, new Vector3(0, 0.19f, 0), new Vector3(0.56f, 0.008f, 0.56f), M("E8C35A", 0.7f));
                    Ring(b, new Vector3(0, 0.19f, 0), 0.31f, 0.05f, M("A85A2A", 0.4f));
                    b.gameObject.SetActive(false);
                    bowls.Add(b);
                }
                puddle = Cy(W, new Vector3(-1, 0.012f, -4.4f), new Vector3(1, 0.01f, 1), M("5FC8F5", 0.95f)).transform;
                puddle.gameObject.SetActive(false);

                // the river and stones
                B(W, new Vector3(36, 0.03f, -5), new Vector3(5, 0.06f, 140), M("4FC3F7", 0.92f));
                B(W, new Vector3(33.4f, 0.025f, -5), new Vector3(0.4f, 0.05f, 140), M("E8D9A8", 0.1f));
                B(W, new Vector3(38.6f, 0.025f, -5), new Vector3(0.4f, 0.05f, 140), M("E8D9A8", 0.1f));
                S(W, new Vector3(35f, 0.02f, -7.2f), new Vector3(1.5f, 0.5f, 1.4f), M("9EA7AD", 0.3f));
                S(W, new Vector3(37.3f, 0.02f, -7.0f), new Vector3(1.4f, 0.5f, 1.5f), M("8F989E", 0.3f));

                // the carousel
                var car = Node("Carousel", W, CAR);
                Cy(car, new Vector3(0, 0.15f, 0), new Vector3(11, 0.15f, 11), M("F2F2F2", 0.4f));
                carSpin = Node("Spin", car, Vector3.zero);
                Cy(carSpin, new Vector3(0, 0.4f, 0), new Vector3(10, 0.1f, 10), M("5BC0EB", 0.5f));
                Cy(carSpin, new Vector3(0, 2.6f, 0), new Vector3(0.35f, 2.3f, 0.35f), M("FFC83A", 0.6f, 0.5f));
                Cone(carSpin, new Vector3(0, 4.6f, 0), new Vector3(11.5f, 2.2f, 11.5f), M("F0435A", 0.4f));
                Cy(carSpin, new Vector3(0, 4.58f, 0), new Vector3(11.4f, 0.04f, 11.4f), M("FFD6E0", 0.3f));
                for (int k = 0; k < 16; k++)
                {
                    float a = k * 22.5f * Mathf.Deg2Rad;
                    S(carSpin, new Vector3(Mathf.Sin(a) * 5.6f, 4.5f, Mathf.Cos(a) * 5.6f), 0.7f, M(k % 2 == 0 ? "FFFFFF" : "FFE14D", 0.4f));
                    NoShadow(S(carSpin, new Vector3(Mathf.Sin(a + 0.2f) * 5.3f, 4.2f, Mathf.Cos(a + 0.2f) * 5.3f), 0.22f, G("FFE36B", 2f)));
                }
                string[] seat = { "FF5FA2", "5FD3FF", "FFC83A", "6EDC3C", "B266FF" };
                for (int k = 0; k < 5; k++)
                {
                    float a = (k * 72 + 36) * Mathf.Deg2Rad;
                    var p = new Vector3(Mathf.Sin(a) * 4.3f, 0, Mathf.Cos(a) * 4.3f);
                    Rod(carSpin, p + Vector3.up * 4.55f, p + Vector3.up * 1.4f, 0.08f, M("FFC83A", 0.6f, 0.5f));
                    S(carSpin, p + Vector3.up * 1.2f, 0.95f, M(seat[k], 0.5f));
                }
                Rod(carSpin, new Vector3(0, 6.7f, 0), new Vector3(0, 8.1f, 0), 0.07f, M("FFC83A", 0.6f, 0.5f));
                B(carSpin, new Vector3(0.45f, 7.8f, 0), new Vector3(0.8f, 0.45f, 0.04f), M("6EDC3C", 0.3f));

                // the haystack
                hay = S(W, HAY + Vector3.up * 1.0f, new Vector3(4.2f, 3.2f, 4.2f), M("E8C85A", 0.1f)).transform;
                for (int k = 0; k < 10; k++)
                {
                    float a = k * 36 * Mathf.Deg2Rad;
                    Rod(W, HAY + new Vector3(Mathf.Cos(a) * 1.9f, 1.2f, Mathf.Sin(a) * 1.9f), HAY + new Vector3(Mathf.Cos(a) * 2.4f, 1.6f + (k % 3) * 0.2f, Mathf.Sin(a) * 2.4f), 0.05f, M("F2D675", 0.1f));
                }

                // the characters
                luk = Add(new Toon(W, "Лукаш", "8BD450", 0.55f, "sprout"));
                puh = Add(new Toon(W, "Пухля", "FF8FC0", 0.5f, "bow"));
                bub = Add(new Toon(W, "Бублик", "FFA640", 0.6f, "cap"));
                prof = Add(new Toon(W, "Профессор", "A58BFF", 0.55f, "glasses"));
                chi = Add(new Toon(W, "Чихуня", "FFE04D", 0.38f, "nose"));
                onion = Add(new Toon(W, "Лук", "F5E6C8", 2.3f, "onion"));
                onion.mood = 4;
                onion.pos = G0 + Vector3.down * 7.5f;
                onion.root.gameObject.SetActive(false);
                for (int k = 0; k < 3; k++)
                {
                    var bd = Add(new Toon(W, "Птичка", k == 1 ? "FFB74D" : "4FC3F7", 0.2f, "bird"));
                    bd.pos = BirdSpot(k); bd.yaw = 180 + (k - 1) * 20;
                    birds.Add(bd);
                }
                five = new[] { luk, puh, bub, prof, chi };

                // props held by the characters
                can = Node("Can", luk.root, new Vector3(0.75f, 0.55f, 0.25f));
                Cy(can, Vector3.zero, new Vector3(0.42f, 0.22f, 0.42f), M("4FA3E0", 0.5f, 0.2f));
                Rod(can, new Vector3(0, 0.05f, 0.15f), new Vector3(0, 0.3f, 0.6f), 0.06f, M("4FA3E0", 0.5f, 0.2f));
                S(can, new Vector3(0, 0.32f, 0.63f), 0.12f, M("3B8BC8", 0.5f, 0.2f));
                Ring(can, new Vector3(0, 0.18f, -0.1f), 0.15f, 0.03f, M("3B8BC8", 0.5f, 0.2f), new Vector3(0, 0, 90));
                can.gameObject.SetActive(false);
                shaker = Node("Shaker", chi.root, new Vector3(0.5f, 0.5f, 0.15f));
                Cy(shaker, Vector3.zero, new Vector3(0.18f, 0.16f, 0.18f), M("E53935", 0.5f));
                S(shaker, new Vector3(0, 0.17f, 0), new Vector3(0.18f, 0.12f, 0.18f), M("EEEEEE", 0.6f, 0.4f));
                shaker.gameObject.SetActive(false);
                nest = Node("Nest", W, Vector3.zero);
                Ring(nest, Vector3.zero, 0.32f, 0.11f, M("8D6E3F", 0.1f));
                for (int k = 0; k < 3; k++) S(nest, new Vector3((k - 1) * 0.14f, 0.05f, (k % 2) * 0.08f), new Vector3(0.14f, 0.18f, 0.14f), M("E3F2FD", 0.5f));
                nest.gameObject.SetActive(false);
                // the onion's flower (the gift at the end)
                flower = Node("Flower", onion.roll, new Vector3(0, 1.3f * onion.r, 0));
                Cy(flower, new Vector3(0, 1.1f, 0), new Vector3(0.14f, 1.1f, 0.14f), M("3FAE3A", 0.3f));
                S(flower, new Vector3(0, 2.7f, 0), 1.0f, M("9C4DFF", 0.4f));
                for (int k = 0; k < 40; k++)
                {
                    var d = UnityEngine.Random.onUnitSphere;
                    Ls(flower, new Vector3(0, 2.7f, 0) + d * 0.6f, 0.3f, M(k % 2 == 0 ? "B266FF" : "E1B3FF", 0.4f));
                }
                flower.localScale = Vector3.zero;
                flower.gameObject.SetActive(false);
            }

            Toon Add(Toon t) { toons.Add(t); return t; }

            /// <summary>The cartoon camera draws into a texture shown full-screen on the top UI layer (over the HUD).</summary>
            void MakeRT()
            {
                int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
                if (rt && rt.width == w && rt.height == h) return;
                if (rt) { cam.targetTexture = null; rt.Release(); Destroy(rt); }
                bool low = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
                rt = new RenderTexture(w, h, 24) { name = "CartoonRT", antiAliasing = low ? 1 : 2 };
                cam.targetTexture = rt;
                view.texture = rt;
            }
            Vector3 BirdSpot(int k) => new Vector3(-3.5f + 1.25f + k * 0.5f, 2.02f + k * 0.03f, 9 - 0.12f - k * 0.04f);

            // ================= time =================
            void Update()
            {
                Dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f) * Mathf.Max(0.05f, Speed);
                clock += Dt;
                if (cam) MakeRT();
                if (riding)
                    for (int i = 0; i < five.Length; i++)
                    {
                        float a = (carAngle + rideSlot[i]) * Mathf.Deg2Rad;
                        five[i].pos = CAR + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 2.7f + Vector3.up * 0.5f;
                        five[i].yaw = carAngle + rideSlot[i] + 90;
                        five[i].tilt = Mathf.Clamp(carSpeed / 12f, 0, 35);
                    }
                foreach (var t in toons) t.Tick(Dt, clock);
                if (carSpin) { carAngle += carSpeed * Dt; carSpin.localRotation = Quaternion.Euler(0, carAngle, 0); }
                foreach (var c in clouds)
                {
                    var p = c.localPosition; p.x += 0.6f * Dt; if (p.x > 140) p.x -= 220; c.localPosition = p;
                }
                if (stirring)
                {
                    spoon.localRotation = Quaternion.Euler(0, Mathf.Sin(clock * 5) * 28, 0);
                    bub.lean = Mathf.Sin(clock * 5) * 10;
                    bub.tilt = Mathf.Cos(clock * 5) * 6;
                }
                // tears, steam, Z's, petals, water from the can
                tearT -= Dt;
                if (tearT <= 0)
                {
                    tearT = 0.05f;
                    foreach (var t in toons)
                    {
                        if (!t.crying || !t.root.gameObject.activeSelf) continue;
                        for (int sd = -1; sd <= 1; sd += 2)
                        {
                            var e = sd < 0 ? t.eyeL : t.eyeR;
                            var v = t.Right * sd * 1.6f * Mathf.Sqrt(t.r) + Vector3.up * 2.4f * Mathf.Sqrt(t.r) + t.Fwd * 0.4f;
                            Spray(W.InverseTransformPoint(e.position), M("5FD3FF", 0.9f), 1, 0.25f, 0.09f * Mathf.Sqrt(t.r / 0.5f), 0.7f, 9, v);
                        }
                    }
                }
                foreach (var t in toons)
                {
                    if (t.mood != 4 || !t.root.gameObject.activeSelf || t == onion) continue;
                    t.zT -= Dt;
                    if (t.zT <= 0) { t.zT = 0.9f; ZText(t.Head + Vector3.up * 0.2f); }
                }
                if (steam)
                {
                    steamT -= Dt;
                    if (steamT <= 0)
                    {
                        steamT = 0.22f;
                        Spray(P0 + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 2.0f, UnityEngine.Random.Range(-0.8f, 0.8f)), Glass("FFFFFF", 0.45f), 1, 0.1f, 0.35f, 1.6f, -0.2f, Vector3.up * 0.8f, 1.6f);
                    }
                }
                if (petals)
                {
                    petalT -= Dt;
                    if (petalT <= 0)
                    {
                        petalT = 0.05f;
                        string[] pc = { "B266FF", "E1B3FF", "FF5FA2", "FFE14D", "FFFFFF" };
                        var p = FO + new Vector3(UnityEngine.Random.Range(-9f, 9f), 9, UnityEngine.Random.Range(-9f, 3f));
                        var go = B(W, p, new Vector3(0.18f, 0.02f, 0.12f), M(pc[UnityEngine.Random.Range(0, pc.Length)], 0.3f));
                        NoShadow(go);
                        go.layer = Layers.Preview;
                        bits.Add(new Bit { t = go.transform, v = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), -1.2f, 0), rot = UnityEngine.Random.insideUnitSphere * 360, life = 7.4f, max = 7.4f, grav = 0, s0 = 1, keep = true });
                    }
                }
                if (can && can.gameObject.activeInHierarchy)
                {
                    canT -= Dt;
                    if (canT <= 0)
                    {
                        canT = 0.04f;
                        var tip = W.InverseTransformPoint(can.TransformPoint(new Vector3(0, 0.32f, 0.66f)));
                        Spray(tip, M("8FD8FF", 0.9f), 1, 0.3f, 0.06f, 0.6f, 9, luk.Fwd * 1.2f);
                    }
                }
                TickBits();
                // title pop
                if (titleT >= 0)
                {
                    titleT += Dt;
                    float k = Mathf.Min(1, titleT * 2.5f);
                    float s = k < 1 ? Mathf.Sin(k * Mathf.PI * 0.5f) * 1.12f : 1 + Mathf.Sin(titleT * 4) * 0.04f;
                    titleKick = Mathf.Max(0, titleKick - Dt * 2);
                    title.rectTransform.localScale = Vector3.one * s * (1 + titleKick * 0.4f);
                    title.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 30) * titleKick * 12);
                }
                // music: a bit quieter under the voices, speeds up with the cartoon
                if (mus)
                {
                    mus.pitch = Mathf.Clamp(Speed, 0.5f, 3f);
                    mus.volume = Mathf.MoveTowards(mus.volume, talking != null ? 0.07f : 0.14f, Time.unscaledDeltaTime * 0.5f);
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
                shake = Mathf.Max(0, shake - Dt * 1.2f);
                ApplyCam();
            }

            void ApplyCam()
            {
                var sh = shake > 0 ? UnityEngine.Random.insideUnitSphere * shake * 0.25f : Vector3.zero;
                cam.transform.localPosition = camPos + sh;
                cam.transform.LookAt(W.TransformPoint(camLook + sh * 0.5f));
            }

            // ================= camera moves =================
            void Cut(Vector3 pos, Vector3 look, float fov = 50)
            {
                follow = null; camRig = null;
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

            void Track(Func<Vector3> f, Vector3 off, float rate = 3f)
            {
                camRig = null; follow = f; followOff = off; camRate = rate;
            }

            /// <summary>A close-up of a character's face.</summary>
            void CloseUp(Toon a, float dist = 4.6f, float side = 0.5f, float fov = 40)
            {
                var c = a.Center;
                Cut(c + a.Fwd * dist * a.r + a.Right * side * a.r * 2 + Vector3.up * a.r * 0.45f, c + Vector3.up * a.r * 0.15f, fov);
            }

            // ================= animation helpers (cartoon time = real time × Speed) =================
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

            Coroutine Bg(IEnumerator e) => StartCoroutine(e);

            IEnumerator Hop(Toon a, Vector3 to, float dur, float h = 0.45f, int hops = 3, bool snd = false)
            {
                var from = a.pos;
                a.Face(to);
                int last = 0;
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    a.pos = Vector3.Lerp(from, to, u);
                    float ph = u * hops;
                    int k = Mathf.Min((int)ph, hops);
                    float f = k >= hops ? 0 : ph - k;
                    a.hop = Mathf.Sin(f * Mathf.PI) * h;
                    a.stretch = 1 + 0.2f * Mathf.Abs(Mathf.Cos(f * Mathf.PI)) * (k >= hops ? 0 : 1);
                    if (k != last) { last = k; a.Land(0.28f); if (snd) AudioFx.Play("step" + (k % 2), 0.4f); }
                });
                a.hop = 0; a.stretch = 1;
            }

            IEnumerator Jump(Toon a, float h = 0.8f, float dur = 0.5f, string snd = "jump2")
            {
                a.Land(0.3f);
                yield return Wait(0.08f);
                if (snd != null) AudioFx.Play(snd, 0.6f);
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    a.hop = 4 * h * u * (1 - u);
                    a.stretch = 1 + 0.25f * Mathf.Abs(1 - 2 * u);
                });
                a.hop = 0; a.stretch = 1;
                a.Land(0.35f);
            }

            /// <summary>A flight along an arc (with somersaults); lands with a squash.</summary>
            IEnumerator Fly(Toon a, Vector3 to, float h, float dur, float spinDeg = 0, string snd = "boing")
            {
                var from = a.pos;
                float s0 = a.spin;
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    a.pos = Vector3.Lerp(from, to, u) + Vector3.up * (4 * h * u * (1 - u));
                    a.spin = s0 + spinDeg * u;
                    a.stretch = 1 + 0.22f * Mathf.Abs(1 - 2 * u);
                });
                a.stretch = 1; a.spin = s0 + spinDeg;
                a.Land(0.45f);
                if (snd != null) AudioFx.Play(snd, 0.7f);
            }

            IEnumerator RollTo(Toon a, Vector3 to, float dur, float bump = 0.15f)
            {
                var from = a.pos;
                a.Face(to);
                float d = (to - from).magnitude, s0 = a.spin;
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    a.pos = Vector3.Lerp(from, to, u);
                    a.spin = s0 + u * d / a.r * Mathf.Rad2Deg;
                    a.hop = bump * Mathf.Abs(Mathf.Sin(u * d * 1.3f));
                });
                a.hop = 0;
                yield return Settle(a);
            }

            /// <summary>After rolling: turn the face back up.</summary>
            IEnumerator Settle(Toon a)
            {
                float s0 = a.spin, s1 = Mathf.Round(a.spin / 360f) * 360f;
                yield return Act(0.25f, t => a.spin = Mathf.Lerp(s0, s1, t / 0.25f));
                a.spin = 0;
            }

            IEnumerator FlyObj(Transform o, Vector3 to, float h, float dur, float rotDeg = 360)
            {
                var from = o.localPosition;
                var r0 = o.localRotation;
                yield return Act(dur, t =>
                {
                    float u = t / dur;
                    o.localPosition = Vector3.Lerp(from, to, u) + Vector3.up * (4 * h * u * (1 - u));
                    o.localRotation = r0 * Quaternion.Euler(rotDeg * u, rotDeg * 0.5f * u, 0);
                });
            }

            IEnumerator Sneeze(Toon a, float wind = 0.35f, float power = 1f)
            {
                int m0 = a.mood;
                a.mood = 5;
                yield return Act(wind, t => { float u = t / wind; a.lean = -18 * u; a.noseK = 1 + 0.7f * u; a.stretch = 1 + 0.12f * u; });
                AudioFx.Play("swing");
                a.lean = 28; a.stretch = 1; a.noseK = 1;
                a.Land(-0.35f * power);
                Spray(a.Center + a.Fwd * a.r, M("DFF6FF", 0.8f), (int)(14 * power), 1.2f, 0.07f, 0.6f, 3f, a.Fwd * 4f * power);
                yield return Act(0.35f, t => a.lean = Mathf.Lerp(28, 0, t / 0.35f));
                a.mood = m0;
            }

            void Spray(Vector3 p, Material m, int n, float rand, float size, float life, float grav, Vector3 vel = default, float grow = 0)
            {
                for (int i = 0; i < n; i++)
                {
                    var go = Ls(W, p, size, m);
                    NoShadow(go);
                    go.layer = Layers.Preview;
                    float l = life * UnityEngine.Random.Range(0.7f, 1.3f);
                    bits.Add(new Bit { t = go.transform, v = vel + UnityEngine.Random.insideUnitSphere * rand, life = l, max = l, grav = grav, s0 = size, grow = grow });
                }
            }

            void ZText(Vector3 p)
            {
                var tx = Prim.Text(W, "Z", p, 5, Hex("FFFFFF"));
                Prim.SetLayer(tx.gameObject, Layers.Preview);
                bits.Add(new Bit { t = tx.transform, v = new Vector3(0.35f, 0.7f, 0), life = 1.6f, max = 1.6f, s0 = 1, bill = true });
            }

            void TickBits()
            {
                for (int i = bits.Count - 1; i >= 0; i--)
                {
                    var b = bits[i];
                    b.life -= Dt;
                    if (b.life <= 0 || !b.t)
                    {
                        if (b.t) Destroy(b.t.gameObject);
                        bits.RemoveAt(i);
                        continue;
                    }
                    b.v.y -= b.grav * Dt;
                    b.t.localPosition += b.v * Dt;
                    if (b.rot != Vector3.zero) b.t.localRotation *= Quaternion.Euler(b.rot * Dt);
                    if (b.bill) b.t.rotation = cam.transform.rotation;
                    if (!b.keep)
                    {
                        float fade = Mathf.Clamp01(b.life / (b.max * 0.35f));
                        float s = b.s0 * (1 + b.grow * (b.max - b.life)) * fade;
                        b.t.localScale = Vector3.one * s;
                    }
                }
            }

            void ClearBits()
            {
                foreach (var b in bits) if (b.t) Destroy(b.t.gameObject);
                bits.Clear();
            }

            // ================= voices and subtitles =================
            /// <summary>Starts a line: subtitles, the voice clip, moving mouths. Returns its length in cartoon seconds.</summary>
            float Speak(Who w, string id, string text)
            {
                subBar.SetActive(true);
                speaker.text = w.name; speaker.color = w.col; sub.text = text;
                var clip = Resources.Load<AudioClip>("Sounds/cartoon/" + id);
                float sp = Mathf.Clamp(Speed, 0.5f, 3f);
                foreach (var s in vsrc) s.Stop();
                float[] ps = w.chorus ? new[] { 1.12f, 1.42f, 0.86f } : new[] { w.pitch };
                float minP = 9;
                for (int i = 0; i < ps.Length; i++)
                {
                    minP = Mathf.Min(minP, ps[i]);
                    if (!clip) continue;
                    vsrc[i].clip = clip;
                    vsrc[i].pitch = Mathf.Min(3f, ps[i] * sp);
                    vsrc[i].volume = w.chorus ? 0.75f : 1f;
                    vsrc[i].Play();
                }
                foreach (var t in toons) t.talk = false;
                if (w.toons != null) foreach (var t in w.toons) t.talk = true;
                talking = w;
                return clip ? clip.length / minP : 0.8f + text.Length * 0.06f;
            }

            void Hush()
            {
                foreach (var t in toons) t.talk = false;
                talking = null;
                subBar.SetActive(false);
            }

            IEnumerator Say(Who w, string id, string text, float tail = 0.3f)
            {
                float len = Speak(w, id, text);
                yield return Wait(len + tail);
                Hush();
            }

            Who Voice(string name, string col, float pitch, params Toon[] t) => new Who { name = name, col = UIKit.Hex(col), pitch = pitch, toons = t };

            // ================= placing =================
            void Put(Toon t, Vector3 p, Vector3? faceTo = null)
            {
                t.root.gameObject.SetActive(true);
                t.Reset();
                t.pos = p; t.spin = 0;
                if (faceTo.HasValue) t.Face(faceTo.Value);
            }

            void Away(Toon t) { t.Reset(); t.pos = new Vector3(-60, 0, -60); t.root.gameObject.SetActive(false); }

            static readonly Vector3 Cam0 = new Vector3(0, 0, -40);

            // ================= the story =================
            IEnumerator Run()
            {
                wN = Voice("Рассказчик", "FFFFFF", 1.0f);
                wL = Voice("Лукаш", "8BD450", 1.12f, luk);
                wP = Voice("Пухля", "FF8FC0", 1.38f, puh);
                wB = Voice("Бублик", "FFA640", 1.22f, bub);
                wPr = Voice("Профессор", "B9A4FF", 0.84f, prof);
                wC = Voice("Чихуня", "FFE04D", 1.6f, chi);
                wO = Voice("Лук", "F5E6C8", 0.74f, onion);
                wBird = Voice("Птички", "4FC3F7", 1.85f, birds.ToArray());
                wAll = Voice("Все", "FFC83A", 1.12f, five);
                wAll.chorus = true;
                yield return null;
                mus.Play();

                // ---------- 0. title ----------
                Scene = 0;
                Put(luk, new Vector3(-3, 0, 0), Cam0); Put(puh, new Vector3(-1.5f, 0, 0.5f), Cam0); Put(bub, new Vector3(0, 0, 0), Cam0);
                Put(prof, new Vector3(1.5f, 0, 0.5f), Cam0); Put(chi, new Vector3(3, 0, 0.2f), Cam0);
                foreach (var t in five) t.bob = 0.3f;
                Cut(new Vector3(0, 16, -34), new Vector3(2, 1, 6));
                Glide(new Vector3(0, 2.2f, -9.5f), new Vector3(0, 1.1f, 2), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Луковый переполох»");
                AudioFx.Play("portal");
                yield return Say(wN, "l01", "Лукашарики! Серия первая: «Луковый переполох»!");
                yield return Say(wN, "l02", "В одной круглой-прекруглой долине жили-были весёлые Лукашарики.");
                Title(null);

                // ---------- 1. meet the Лукашарики ----------
                Scene = 1;
                foreach (var t in five) t.bob = 0;
                yield return Intro(luk, wL, "l03", "Привет! Я Лукаш. У меня на макушке растёт лучок!");
                yield return Intro(puh, wP, "l04", "А я Пухля! У меня самый красивый бантик!");
                yield return Intro(bub, wB, "l05", "Бублик! Люблю мою кепку. И покушать!");
                yield return Intro(prof, wPr, "l06", "Профессор. Я знаю всё. Ну... почти всё.");
                CloseUp(chi, 5f, 0.4f);
                Bg(Jump(chi, 0.6f, 0.45f));
                float len = Speak(wC, "l07", "А я Чихуня! Я... я... я... АПЧХИ!");
                yield return Wait(len - 0.9f);
                yield return Sneeze(chi, 0.45f, 1.4f);
                shake = 0.5f;
                Bg(Fly(chi, chi.pos + new Vector3(0.8f, 0, 8f), 1.6f, 0.9f, -720, "bonk"));
                yield return Wait(0.5f);
                Hush();
                Cut(new Vector3(0, 2.2f, -9.5f), new Vector3(0, 1.1f, 2.5f));
                yield return Wait(0.6f);
                chi.mood = 3;
                Bg(Hop(chi, new Vector3(3, 0, 0.2f), 1.8f, 0.4f, 5));
                yield return Say(wL, "l08", "Будь здоров, Чихуня!");
                chi.mood = 0; chi.Face(Cam0);
                foreach (var t in five) t.mood = 1;
                AudioFx.Play("win0", 0.6f);
                yield return Wait(0.7f);

                // ---------- 2. the giant onion grows ----------
                Scene = 2;
                Put(luk, G0, Cam0);
                Put(puh, new Vector3(4.4f, 0, 1.6f), G0); Put(bub, new Vector3(2.4f, 0, 0.6f), G0); Put(prof, new Vector3(2.8f, 0, 2.0f), G0); Put(chi, new Vector3(11, 0, 1.4f), G0);
                foreach (var o in fieldOnions) if ((o.localPosition - G0).magnitude < 1.5f) o.gameObject.SetActive(false);
                can.gameObject.SetActive(true);
                luk.mood = 1;
                Cut(new Vector3(8.6f, 1.6f, 1.6f), new Vector3(8, 0.8f, 5.6f), 45);
                yield return Say(wL, "l09", "Расти, лучок, большой-пребольшой!");
                can.gameObject.SetActive(false);
                luk.mood = 5;
                AudioFx.Play("hum");
                shake = 0.7f;
                yield return Act(0.9f, t => { foreach (var o in fieldOnions) if (o.gameObject.activeSelf) o.localRotation = Quaternion.Euler(Mathf.Sin(t * 50 + o.localPosition.x) * 8, 0, Mathf.Cos(t * 43 + o.localPosition.z) * 8); });
                foreach (var o in fieldOnions) if ((o.localPosition - G0).magnitude < 2.9f) { o.gameObject.SetActive(false); Spray(o.localPosition + Vector3.up * 0.2f, M("8B5E3C", 0.1f), 3, 2.5f, 0.15f, 0.9f, 9, Vector3.up * 4); }
                Cut(new Vector3(8, 0.5f, -7.5f), new Vector3(8, 2.5f, 6), 55);
                Glide(new Vector3(8, 1.2f, -9), new Vector3(8, 4.2f, 6), 0.8f);
                onion.root.gameObject.SetActive(true);
                var narr = Bg(Say(wN, "l10", "И лучок вырос. Очень. Очень. Большой."));
                shake = 1f;
                yield return Act(2.6f, t =>
                {
                    float u = t / 2.6f;
                    float e = 1 + 2.2f * Mathf.Pow(u - 1, 3) + 1.2f * Mathf.Pow(u - 1, 2);   // ease out with an overshoot
                    onion.pos = G0 + Vector3.down * 7.5f * (1 - e);
                    luk.pos = G0 + Vector3.up * Mathf.Max(0, onion.pos.y + 2 * onion.r);
                    if (UnityEngine.Random.value < 0.3f) Spray(G0 + new Vector3(UnityEngine.Random.Range(-2.4f, 2.4f), 0.1f, UnityEngine.Random.Range(-2.4f, 2.4f)), M("8B5E3C", 0.1f), 1, 1.5f, 0.2f, 0.8f, 9, Vector3.up * 3);
                });
                onion.Land(0.25f); luk.Land(0.4f);
                AudioFx.Play("boing");
                yield return narr;
                CloseUp(luk, 5.2f, 0.3f, 42);
                luk.look = new Vector2(0, -1);
                Bg(Jump(luk, 0.3f, 0.3f, null));
                yield return Say(wL, "l11", "А-а-а! Снимите меня отсюда!");
                Cut(new Vector3(1.5f, 2.6f, -11.5f), new Vector3(3.5f, 2.2f, 4.5f), 50);
                yield return Fly(luk, new Vector3(8.6f, 0, 2.6f), 1.4f, 0.9f, 360, "boing");
                luk.Land(0.65f); luk.mood = 3; luk.look = Vector2.zero;
                Bg(Hop(bub, new Vector3(5.6f, 0, 1.0f), 0.9f, 0.4f, 3));
                yield return Wait(0.9f);
                bub.mood = 5; bub.look = new Vector2(0.4f, 1);
                yield return Say(wB, "l12", "Ого! Он больше нашего домика!");
                bub.mood = 0; luk.mood = 0;

                // ---------- 3. the plan: soup! (and the onion is listening) ----------
                Scene = 3;
                Put(luk, new Vector3(8.6f, 0, 2.4f), Cam0); Put(bub, new Vector3(6.2f, 0, 1.0f), Cam0);
                Put(puh, new Vector3(4.4f, 0, 1.6f), Cam0); Put(prof, new Vector3(2.8f, 0, 2.0f), Cam0); Put(chi, new Vector3(10.6f, 0, 2.2f), Cam0);
                prof.look = new Vector2(0.3f, 0.7f);
                CloseUp(prof, 5f, -0.4f);
                Bg(Act(3f, t => prof.tilt = Mathf.Sin(t * 2) * 8));
                yield return Say(wPr, "l13", "По моим научным расчётам... это очень большой лук.");
                prof.look = Vector2.zero; prof.tilt = 0;
                CloseUp(puh, 5f, 0.4f);
                puh.mood = 1;
                Bg(Jump(puh, 0.7f, 0.45f));
                yield return Say(wP, "l14", "Ура! Сварим луковый суп! Но сначала — лучок с грядки.");
                puh.mood = 0;
                // Бублик pulls a field onion
                Transform fo = null;
                foreach (var o in fieldOnions) if (o.gameObject.activeSelf && (fo == null || (o.localPosition - new Vector3(4.6f, 0, 3.6f)).sqrMagnitude < (fo.localPosition - new Vector3(4.6f, 0, 3.6f)).sqrMagnitude)) fo = o;
                var fop = fo.localPosition;
                Put(bub, fop - new Vector3(0, 0, 1.0f), fop);
                Cut(new Vector3(fop.x + 3.2f, 1.3f, fop.z - 3.0f), new Vector3(fop.x, 0.55f, fop.z - 0.6f), 45);
                len = Speak(wB, "l15", "Тяну-потяну... Ой-ой-ой!");
                yield return Act(Mathf.Max(0.5f, len - 0.9f), t =>
                {
                    bub.lean = -12 - 10 * Mathf.Abs(Mathf.Sin(t * 13));
                    bub.tilt = Mathf.Sin(t * 9) * 6;
                    fo.localPosition = fop + Vector3.up * 0.06f * Mathf.Abs(Mathf.Sin(t * 20));
                });
                AudioFx.Play("open");
                bub.tilt = 0; bub.lean = 0;
                Bg(Fly(bub, bub.pos + new Vector3(0, 0, -2.4f), 1.0f, 0.7f, -360, "bonk"));
                yield return FlyObj(fo, bub.pos + new Vector3(0, 0, -2.4f) + new Vector3(0, 0.66f, -0.6f), 2.2f, 0.9f, 720);
                bub.lean = -85; bub.mood = 3;
                yield return Wait(0.4f);
                Hush();
                yield return Wait(0.3f);
                // the giant onion opens its eyes...
                Cut(G0 + new Vector3(0, 2.5f, -6.8f), G0 + new Vector3(0, 2.4f, 0), 38);
                AudioFx.Play("hum", 0.5f);
                narr = Bg(Say(wN, "l16", "А гигантский лук всё слышал..."));
                yield return Act(3.2f, t =>
                {
                    onion.mood = t > 0.7f ? 0 : 4;
                    onion.look = t < 1.3f ? Vector2.zero : t < 2.0f ? new Vector2(-1, 0) : t < 2.6f ? new Vector2(1, 0) : new Vector2(0, -0.3f);
                });
                onion.Land(0.2f);
                AudioFx.Play("click");
                yield return narr;
                fo.gameObject.SetActive(false);

                // ---------- 4. cooking the soup ----------
                Scene = 4;
                pot.gameObject.SetActive(true);
                steam = true;
                onion.look = Vector2.zero;
                Put(bub, P0 + new Vector3(2.1f, 0.8f, -0.2f), P0);
                Put(luk, new Vector3(-3.4f, 0, -3.0f), P0); Put(puh, new Vector3(-2.4f, 0, -4.1f), P0); Put(prof, new Vector3(0.3f, 0, -4.4f), P0); Put(chi, new Vector3(-4.6f, 0, -4.8f), P0);
                Cut(new Vector3(-1, 3.0f, -10.5f), new Vector3(-1, 1.3f, -2), 50);
                stirring = true; bub.mood = 1;
                yield return Say(wB, "l17", "Мешаю-мешаю, суп помешаю!");
                shaker.gameObject.SetActive(true);
                yield return Hop(chi, P0 + new Vector3(0, 0, -1.75f), 1.0f, 0.4f, 3, true);
                chi.Face(P0);
                Cut(new Vector3(-1, 2.4f, -7.4f), new Vector3(-1, 1.6f, -2.6f), 45);
                len = Speak(wC, "l18", "А я добавлю перчику! Ой... ой... АПЧХИ!");
                yield return Act(Mathf.Max(0.5f, len - 1.0f), t =>
                {
                    chi.hop = Mathf.Abs(Mathf.Sin(t * 5)) * 1.7f;
                    if (UnityEngine.Random.value < 0.25f) Spray(W.InverseTransformPoint(shaker.position) + Vector3.up * 0.2f, M("5A3A22", 0.2f), 1, 0.3f, 0.05f, 0.7f, 6);
                });
                chi.hop = 0;
                yield return Sneeze(chi, 0.5f, 1.6f);
                shake = 0.6f;
                for (int k = 0; k < 14; k++)
                    Spray(P0 + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 2.2f, UnityEngine.Random.Range(-1.6f, 0.4f)), Glass("C98A4B", 0.5f), 1, 1.5f, 0.6f, 2.2f, -0.3f, Vector3.zero, 1.2f);
                Hush();
                foreach (var t in five) t.mood = 5;
                Cut(new Vector3(-1, 3.0f, -10.5f), new Vector3(-1, 1.3f, -2), 50);
                len = Speak(wAll, "l19", "Апчхи! Апчхи! Апчхи!");
                var fired = new bool[3];
                yield return Act(len + 0.2f, t =>
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float tk = len * (0.12f + k * 0.3f);
                        if (!fired[k] && t >= tk)
                        {
                            fired[k] = true;
                            foreach (var a in five) { a.Land(-0.3f); Spray(a.Center + a.Fwd * a.r, M("DFF6FF", 0.8f), 5, 1f, 0.06f, 0.5f, 3, a.Fwd * 3); }
                            AudioFx.Play("swing", 0.6f);
                        }
                    }
                    foreach (var a in five)
                    {
                        float p = 0;
                        for (int k = 0; k < 3; k++) { float d = (t - len * (0.12f + k * 0.3f)) * 6; p = Mathf.Max(p, Mathf.Exp(-d * d)); }
                        if (a != bub) a.lean = 25 * p;
                    }
                });
                Hush();
                foreach (var t in five) { t.mood = 0; if (t != bub) t.lean = 0; }
                prof.mood = 3;
                CloseUp(prof, 5f, 0.3f);
                yield return Say(wPr, "l20", "Перец — это сильно. Даже слишком.");
                stirring = false; spoon.localRotation = Quaternion.identity;
                shaker.gameObject.SetActive(false);

                // ---------- 5. lunch ----------
                Scene = 5;
                var row = new[] { luk, puh, chi, bub, prof };
                float[] rx = { -3.8f, -2.4f, -1.0f, 0.4f, 1.8f }, rz = { -4.0f, -4.6f, -4.9f, -4.6f, -4.0f };
                for (int i = 0; i < 5; i++)
                {
                    Put(row[i], new Vector3(rx[i], 0, rz[i]), Cam0);
                    bowls[i].gameObject.SetActive(true);
                    bowls[i].localPosition = new Vector3(rx[i], 0, rz[i] - 0.8f);
                    bowls[i].localScale = Vector3.one;
                    row[i].mood = 6;
                }
                Cut(new Vector3(-1, 2.4f, -10.5f), new Vector3(-1, 0.9f, -4.4f), 46);
                narr = Bg(Say(wAll, "l21", "Ням-ням-ням! Вкуснотища!"));
                yield return Act(2.6f, t =>
                {
                    for (int i = 0; i < 5; i++) row[i].lean = 14 * Mathf.Abs(Mathf.Sin(t * 6 + i));
                    if (UnityEngine.Random.value < 0.06f) AudioFx.Play("click", 0.25f);
                });
                yield return narr;
                foreach (var t in five) t.lean = 0;
                CloseUp(bub, 5.4f, 0.3f);
                len = Speak(wB, "l22", "Мне добавки! И ещё добавки! И ещё!");
                int grown = 0;
                yield return Act(len + 0.2f, t =>
                {
                    int want = t > len * 0.85f ? 3 : t > len * 0.55f ? 2 : t > len * 0.2f ? 1 : 0;
                    if (want > grown) { grown = want; bub.Land(0.3f); AudioFx.Play("boing", 0.5f); }
                    bub.puff = Mathf.MoveTowards(bub.puff, 1 + 0.15f * grown, Dt * 1.5f);
                    soup.localPosition = new Vector3(0, 1.93f - 0.25f * grown, 0);
                });
                Hush();
                bub.mood = 6;
                puh.Face(bub.pos);
                Cut(new Vector3(-1, 1.6f, -8.6f), new Vector3(-0.8f, 0.8f, -4.6f), 42);
                yield return Say(wP, "l23", "Бублик, ты уже круглый, как арбуз!");
                CloseUp(bub, 6f, 0.3f);
                bub.mood = 1;
                Bg(Jump(bub, 0.9f, 0.6f, "boing"));
                yield return Say(wB, "l24", "А я всегда круглый!");
                foreach (var t in five) t.mood = 1;
                Cut(new Vector3(-1, 2.4f, -10.5f), new Vector3(-1, 0.9f, -4.4f), 46);
                AudioFx.Play("win1", 0.6f);
                yield return Act(0.9f, t => { foreach (var a in five) a.tilt = Mathf.Sin(t * 14 + a.r * 10) * 8; });
                foreach (var t in five) t.tilt = 0;

                // ---------- 6. onion tears ----------
                Scene = 6;
                foreach (var t in five) { t.mood = 2; t.crying = true; t.Face(Cam0); }
                puddle.gameObject.SetActive(true);
                puddle.localScale = new Vector3(0.1f, 0.01f, 0.1f);
                AudioFx.Play("error");
                Bg(Act(9f, t => { float k = Mathf.Min(1, t / 8f); puddle.localScale = new Vector3(Mathf.Lerp(0.1f, 9f, k), 0.01f, Mathf.Lerp(0.1f, 5.5f, k)); }));
                CloseUp(bub, 5.4f, 0.3f);
                yield return Say(wB, "l25", "Ой-ой-ой! Глаза щиплет!");
                CloseUp(puh, 5.2f, -0.3f);
                Bg(Act(2.5f, t => puh.tilt = Mathf.Sin(t * 6) * 10));
                yield return Say(wP, "l26", "Я плачу... но мне совсем не грустно!");
                Cut(new Vector3(-1, 2.2f, -9.5f), new Vector3(-0.4f, 0.9f, -4.4f), 46);
                Bg(Jump(prof, 0.5f, 0.4f));
                yield return Say(wPr, "l27", "Спокойно, коллеги! Это не грусть. Это луковые слёзы!");
                // Чихуня sails off in his bowl
                var boat = bowls[2];
                chi.crying = false; chi.mood = 1;
                AudioFx.Play("splash");
                Cut(new Vector3(-1, 1.8f, -9.2f), new Vector3(-1, 0.6f, -5.2f), 40);
                var c0 = chi.pos;
                boat.localScale = Vector3.one * 1.6f;
                len = Speak(wC, "l28", "Ура! У нас теперь своё море!");
                yield return Act(len + 0.3f, t =>
                {
                    chi.pos = c0 + new Vector3(Mathf.Sin(t * 0.8f) * 0.6f, 0.28f + Mathf.Sin(t * 3) * 0.04f, -0.5f - t * 0.15f);
                    chi.tilt = Mathf.Sin(t * 3) * 10;
                    boat.localPosition = chi.pos - Vector3.up * 0.28f;
                    boat.localRotation = Quaternion.Euler(0, 0, chi.tilt);
                });
                Hush();

                // ---------- 7. onion breath: the birds fall asleep ----------
                Scene = 7;
                foreach (var t in five) t.crying = false;
                ClearBits();
                puddle.gameObject.SetActive(false);
                foreach (var b in bowls) { b.gameObject.SetActive(false); b.localRotation = Quaternion.identity; }
                pot.gameObject.SetActive(false); steam = false;
                Put(bub, new Vector3(-0.8f, 0, 5.6f), BirdSpot(1));
                bub.puff = 1.45f;
                Put(chi, new Vector3(-2.7f, 0, 5.0f), BirdSpot(1));
                Away(luk); Away(prof);
                Put(puh, new Vector3(4.5f, 0, 6.5f), bub.pos);
                for (int k = 0; k < 3; k++) { birds[k].Reset(); birds[k].pos = BirdSpot(k); birds[k].yaw = 160 + k * 15; }
                Cut(new Vector3(3.2f, 1.8f, 0.6f), new Vector3(-1.6f, 1.4f, 7.4f), 48);
                len = Speak(wB, "l29", "Ха-а-ам... Что-то я устал.");
                float puffT = 0;
                yield return Act(len + 0.2f, t =>
                {
                    bub.mood = t > 0.2f && t < len - 0.2f ? 5 : 0;
                    bub.lean = -12 * Mathf.Sin(Mathf.Clamp01(t / len) * Mathf.PI);
                    bub.stretch = 1 + 0.15f * Mathf.Sin(Mathf.Clamp01(t / len) * Mathf.PI);
                    puffT -= Dt;
                    if (t > 0.4f && puffT <= 0)
                    {
                        puffT = 0.12f;
                        var dir = (BirdSpot(1) - bub.MouthAt).normalized;
                        Spray(bub.MouthAt, Glass("9BE15A", 0.45f), 1, 0.2f, 0.3f, 2.4f, 0, dir * 2.2f, 0.9f);
                    }
                });
                bub.mood = 0; bub.lean = 0; bub.stretch = 1;
                narr = Bg(Say(wBird, "l30", "Чик-чирик... хр-р-р..."));
                yield return Wait(0.6f);
                for (int k = 0; k < 3; k++)
                {
                    var bd = birds[k];
                    bd.mood = 4;
                    var target = new Vector3(bd.pos.x + 0.3f, 0, bd.pos.z - 0.9f - k * 0.2f);
                    Bg(Fly(bd, target, 0.3f, 0.6f, 200, "boing"));
                    yield return Wait(0.45f);
                }
                yield return narr;
                foreach (var bd in birds) { bd.spin = 0; bd.tilt = 70; }
                Bg(Hop(puh, new Vector3(1.2f, 0, 5.2f), 1.0f, 0.4f, 3));
                yield return Wait(0.9f);
                puh.Face(bub.pos); bub.Face(puh.pos);
                Cut(new Vector3(0.2f, 1.4f, 1.4f), new Vector3(0.2f, 0.8f, 5.4f), 44);
                Bg(Act(2f, t => puh.lean = 10 * Mathf.Abs(Mathf.Sin(t * 8))));
                yield return Say(wP, "l31", "Бублик! Не дыши на птичек!");
                puh.lean = 0;
                len = Speak(wB, "l32", "Я не дышу! Я... просто зевнул.");
                yield return Act(len, t => bub.tilt = Mathf.Sin(t * 5) * 8);
                Hush();
                bub.tilt = 0;
                // one more little yawn — and Чихуня falls asleep too
                bub.Face(chi.pos);
                bub.mood = 5;
                for (int k = 0; k < 5; k++) Spray(bub.MouthAt, Glass("9BE15A", 0.45f), 1, 0.2f, 0.3f, 1.8f, 0, (chi.Center - bub.MouthAt).normalized * 2f, 0.9f);
                Cut(new Vector3(-1.6f, 1.6f, 1.2f), new Vector3(-1.8f, 0.7f, 5.2f), 46);
                yield return Wait(0.5f);
                bub.mood = 0;
                chi.mood = 4;
                yield return Act(0.8f, t => chi.tilt = Mathf.Lerp(0, 85, (t / 0.8f) * (t / 0.8f)));
                chi.Land(0.3f);
                AudioFx.Play("boing", 0.6f);
                yield return Wait(0.6f);

                // ---------- 8. science saves the day: mint candies ----------
                Scene = 8;
                Put(prof, new Vector3(3.6f, 0, 4.0f), bub.pos);
                Cut(new Vector3(1.2f, 2.4f, -2.2f), new Vector3(0, 1.1f, 6), 50);
                AudioFx.Play("magic");
                Bg(Jump(prof, 0.9f, 0.55f));
                yield return Say(wPr, "l33", "Наука спешит на помощь! Мятные конфетки — всем!");
                var eaters = new List<Toon> { bub, puh, chi };
                eaters.AddRange(birds);
                for (int i = 0; i < eaters.Count; i++)
                {
                    Bg(Candy(prof.Head + Vector3.up * 0.2f, eaters[i]));
                    yield return Wait(0.18f);
                }
                yield return Wait(1.1f);
                chi.tilt = 0; chi.mood = 1;
                foreach (var t in five) if (t.root.gameObject.activeSelf) t.mood = 1;
                narr = Bg(Say(wAll, "l34", "Ух ты! Свежо-свежо!"));
                yield return Act(1.2f, t => bub.puff = Mathf.MoveTowards(bub.puff, 1f, Dt * 0.8f));
                yield return narr;
                bub.puff = 1;
                for (int k = 0; k < 3; k++)
                {
                    var bd = birds[k];
                    bd.tilt = 0; bd.mood = 0; bd.flap = 1;
                    Bg(Fly(bd, BirdSpot(k), 1.4f, 1.0f, 0, null));
                }
                Glide(new Vector3(0.6f, 2.3f, 2.2f), new Vector3(-1.6f, 1.8f, 8.6f), 1.6f, 44);
                yield return Wait(1.0f);
                foreach (var bd in birds) { bd.flap = 0; bd.yaw = 200; }
                yield return Say(wBird, "l35", "Чик-чирик! Доброе утро!");

                // ---------- 9. the onion runs away ----------
                Scene = 9;
                Put(luk, new Vector3(6, 0, 1.4f), Cam0); Put(puh, new Vector3(4.2f, 0, 1.8f), Cam0); Put(bub, new Vector3(8, 0, 1.0f), Cam0);
                Put(prof, new Vector3(2.6f, 0, 2.0f), Cam0); Put(chi, new Vector3(1.2f, 0, 2.6f), Cam0);
                onion.mood = 0; onion.Face(Cam0);
                Cut(puh.pos + new Vector3(-0.6f, 1.2f, -3.4f), puh.pos + new Vector3(1.6f, 1.6f, 4), 46);
                puh.mood = 1;
                Bg(Jump(puh, 0.6f, 0.45f));
                len = Speak(wP, "l36", "А теперь — суп из гигантского лука!");
                yield return Wait(len * 0.6f);
                onion.mood = 5; AudioFx.Play("glitch", 0.5f);
                yield return Wait(len * 0.4f + 0.3f);
                Hush();
                CloseUp(onion, 3.0f, 0, 42);
                Bg(Act(2f, t => onion.tilt = Mathf.Sin(t * 30) * 3));
                yield return Say(wO, "l37", "Что?! Только не в суп!");
                Cut(new Vector3(8, 2.6f, -9), new Vector3(8, 2.4f, 3.5f), 52);
                foreach (var t in five) { t.mood = 5; t.Face(G0); }
                yield return Fly(onion, G0 + new Vector3(0, 0, 0.5f), 2.2f, 0.8f, 0, "boing");
                shake = 0.6f;
                onion.Face(new Vector3(16, 0, -2));
                Bg(RollTo(onion, new Vector3(16, 0, -2), 1.6f, 0.3f));
                CloseUp(luk, 5f, -0.4f);
                luk.Face(new Vector3(16, 0, -2) + Cam0 * 0.2f);
                yield return Say(wL, "l38", "Он живой! Он убегает! Держи его!");
                foreach (var t in five) t.mood = 0;

                // ---------- 10. the great onion chase ----------
                Scene = 10;
                onion.pos = new Vector3(16, 0, -2); onion.spin = 0;
                var path = new[] { new Vector3(16, 0, -2), new Vector3(30, 0, -8) };
                var dir0 = (path[1] - path[0]).normalized;
                var perp = new Vector3(-dir0.z, 0, dir0.x);
                var chasers = new[] { luk, puh, prof, chi };
                float[] lagD = { 3.5f, 5.5f, 4.5f, 7f }, side = { -1.2f, 0.8f, -2.2f, -0.2f };
                var start = new Vector3[4];
                for (int i = 0; i < 4; i++) start[i] = chasers[i].pos;
                var bubStart = bub.pos;
                var bubStop = T2 - (T2 - new Vector3(17.5f, 0, 0.6f)).normalized * 0.95f;
                bool bonked = false;
                Track(() => (onion.pos * 0.45f + (luk.pos + puh.pos + prof.pos) * (0.55f / 3)) + Vector3.up * 1.5f, new Vector3(-1, 3.4f, -13.5f), 3f);
                ApplyCamNow();
                narr = Bg(Say(wN, "l39", "И началась большая луковая погоня!"));
                const float D = 6.5f;
                float L = (path[1] - path[0]).magnitude;
                yield return Act(D, t =>
                {
                    float u = t / D;
                    float s = L * u;
                    onion.pos = path[0] + dir0 * s;
                    onion.spin = s / onion.r * Mathf.Rad2Deg;
                    onion.hop = 0.35f * Mathf.Abs(Mathf.Sin(s * 0.9f));
                    onion.yaw = Mathf.Atan2(dir0.x, dir0.z) * Mathf.Rad2Deg;
                    for (int i = 0; i < 4; i++)
                    {
                        var a = chasers[i];
                        float si = s - lagD[i];
                        Vector3 p = si <= 0
                            ? Vector3.Lerp(start[i], path[0] + perp * side[i], Mathf.Clamp01(s / lagD[i]))
                            : path[0] + dir0 * si + perp * side[i];
                        var dp = p - a.pos;
                        if (dp.sqrMagnitude > 1e-5f) a.yaw = Mathf.Atan2(dp.x, dp.z) * Mathf.Rad2Deg;
                        a.pos = p;
                        if (a == prof) { a.spin += dp.magnitude / a.r * Mathf.Rad2Deg; a.hop = 0; }
                        else
                        {
                            float hh = Mathf.Abs(Mathf.Sin(t * 7 + i));
                            if (a.hop > 0.05f && hh * 0.5f <= 0.05f) a.Land(0.25f);
                            a.hop = hh * 0.5f;
                        }
                    }
                    if (!bonked)
                    {
                        float ub = Mathf.Clamp01(t / (D * 0.62f));
                        var mid = new Vector3(17.5f, 0, 0.6f);
                        var p = ub < 0.35f ? Vector3.Lerp(bubStart, mid, ub / 0.35f) : Vector3.Lerp(mid, bubStop, (ub - 0.35f) / 0.65f);
                        bub.Face(p + (p - bub.pos));
                        bub.pos = p;
                        bub.hop = Mathf.Abs(Mathf.Sin(t * 7.5f)) * 0.5f;
                        if (ub >= 1)
                        {
                            bonked = true; bub.hop = 0; bub.Face(T2); bub.Land(0.6f); bub.mood = 3;
                            AudioFx.Play("bonk"); shake = 0.3f;
                        }
                    }
                });
                foreach (var a in chasers) a.hop = 0;
                yield return narr;
                // Бублик and the tree
                bub.pos = bubStop; bub.Face(bubStop - (T2 - bubStop));
                nest.gameObject.SetActive(true);
                nest.SetParent(W, false);
                nest.localPosition = bub.Head + Vector3.up * 3; nest.localRotation = Quaternion.identity;
                CloseUp(bub, 6f, 0.6f, 45);
                yield return Act(0.45f, t => nest.localPosition = Vector3.Lerp(bub.Head + Vector3.up * 3, bub.Head, t / 0.45f));
                nest.SetParent(bub.roll, true);
                bub.Land(0.3f);
                AudioFx.Play("crate", 0.6f);
                yield return Say(wB, "l40", "Ой! Дерево! Кто поставил тут дерево?!");
                bub.mood = 0;
                // the Professor rolls on
                prof.spin = 0;
                Track(() => prof.Center, new Vector3(-2.8f, 1.2f, -4.2f), 4f);
                ApplyCamNow();
                Bg(RollTo(prof, new Vector3(31.5f, 0, -6.4f), 2.6f, 0.2f));
                prof.mood = 1;
                yield return Say(wPr, "l41", "Не отставать! Катимся по науке!");

                // ---------- 11. across the river ----------
                Scene = 11;
                Put(onion, new Vector3(29.5f, 0, -10.5f));
                onion.Face(new Vector3(45, 0, -10.5f));
                Put(luk, new Vector3(32.6f, 0, -7.3f), new Vector3(40, 0, -7)); Put(puh, new Vector3(31.6f, 0, -6.2f), new Vector3(40, 0, -7));
                Put(prof, new Vector3(31.3f, 0, -7.9f), new Vector3(40, 0, -7)); Put(chi, new Vector3(30.6f, 0, -6.8f), new Vector3(40, 0, -7));
                Put(bub, new Vector3(28.2f, 0, -5.4f), new Vector3(40, 0, -7));
                nest.gameObject.SetActive(true);
                Cut(new Vector3(36, 2.6f, -19.5f), new Vector3(36, 1.2f, -7.5f), 50);
                yield return Fly(onion, new Vector3(36, 0, -10.5f), 3f, 0.8f, 200, "splash");
                Spray(new Vector3(36, 0.2f, -10.5f), M("BDEBFF", 0.9f), 26, 3f, 0.18f, 1f, 9, Vector3.up * 5);
                yield return Fly(onion, new Vector3(42.5f, 0, -10.5f), 3f, 0.8f, 200, "boing");
                onion.spin = 0;
                luk.mood = 1;
                yield return Say(wL, "l42", "Прыгаем по камушкам!");
                var st1 = new Vector3(35, 0.27f, -7.2f); var st2 = new Vector3(37.3f, 0.27f, -7.0f);
                var hoppers = new[] { luk, puh, prof, chi };
                for (int i = 0; i < 4; i++) Bg(StoneChain(hoppers[i], i * 0.4f, st1, st2, new Vector3(40.2f + i * 0.9f, 0, -6.4f - (i % 2) * 1.1f)));
                yield return Hop(bub, new Vector3(32.6f, 0, -7.0f), 1.0f, 0.4f, 3);
                yield return Fly(bub, st1, 0.9f, 0.5f, 0, "land1");
                bub.Face(st2);
                yield return Wait(0.3f);
                bub.mood = 5;
                yield return Fly(bub, new Vector3(36.4f, -0.55f, -7.3f), 1.0f, 0.55f, 0, "splash");
                Spray(new Vector3(36.4f, 0.2f, -7.3f), M("BDEBFF", 0.9f), 22, 2.5f, 0.14f, 0.9f, 9, Vector3.up * 4);
                bub.mood = 3;
                Cut(new Vector3(36.4f, 1.0f, -10.4f), new Vector3(36.4f, 0.3f, -7.3f), 42);
                len = Speak(wB, "l43", "Буль-буль! Я теперь мокрый бублик!");
                yield return Act(len + 0.2f, t => { bub.pos.y = -0.55f + Mathf.Sin(t * 4) * 0.06f; bub.tilt = Mathf.Sin(t * 3) * 8; });
                Hush();
                bub.tilt = 0;
                Cut(new Vector3(37.5f, 2.2f, -15.5f), new Vector3(38.5f, 0.8f, -6.8f), 46);
                yield return Fly(bub, new Vector3(39.6f, 0, -7.9f), 1.4f, 0.6f, 0, "land2");
                bub.Face(Cam0); bub.mood = 1;
                float y0 = bub.yaw;
                yield return Act(0.9f, t =>
                {
                    bub.yaw = y0 + Mathf.Sin(t * 40) * 40;
                    if (UnityEngine.Random.value < 0.6f) Spray(bub.Center, M("8FD8FF", 0.9f), 2, 3f, 0.07f, 0.5f, 6);
                });
                bub.yaw = y0;

                // ---------- 12. the carousel gone wild ----------
                Scene = 12;
                Put(onion, new Vector3(44.5f, 0, -9.0f));
                Put(luk, new Vector3(43.5f, 0, -4.8f), CAR); Put(puh, new Vector3(42.4f, 0, -5.6f), CAR); Put(bub, new Vector3(41.6f, 0, -4.2f), CAR);
                Put(prof, new Vector3(42.0f, 0, -7.0f), CAR); Put(chi, new Vector3(40.8f, 0, -6.2f), CAR);
                Cut(new Vector3(49.5f, 4.2f, -25.5f), new Vector3(50.5f, 3.4f, -8), 50);
                narr = Bg(Say(wN, "l44", "А лук закатился прямо на карусель!"));
                yield return RollTo(onion, new Vector3(46.8f, 0, -8.6f), 0.8f, 0.2f);
                yield return Fly(onion, CAR + Vector3.up * 6.75f, 3.2f, 1.0f, 360, "boing");
                onion.spin = 0; onion.mood = 5;
                Bg(Act(1.2f, t => carSpeed = Mathf.Lerp(0, 70, t / 1.2f)));
                for (int i = 0; i < 5; i++)
                {
                    float a = (carAngle + carSpeed * (0.7f + i * 0.12f) + rideSlot[i]) * Mathf.Deg2Rad;
                    Bg(Fly(five[i], CAR + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 2.7f + Vector3.up * 0.5f, 1.4f, 0.7f, 0, "land3"));
                    yield return Wait(0.12f);
                }
                yield return Wait(0.6f);
                riding = true;
                yield return narr;
                foreach (var t in five) t.mood = 1;
                var rideCo = Bg(Act(30f, t => { carSpeed = Mathf.Lerp(70, 300, t / 9f); onion.yaw = carAngle * 2.2f; onion.pos = CAR + Vector3.up * 6.75f; }));
                RideCam(1);
                puh.mood = 5;
                yield return Say(wP, "l45", "А-а-а! Слишком быстро!");
                RideCam(3);
                prof.mood = 5;
                yield return Say(wPr, "l46", "Где тормоза? Почему у карусели нет тормозов?!");
                RideCam(4);
                chi.mood = 5;
                len = Speak(wC, "l47", "Ой... сейчас... сейчас... АПЧХИ!");
                yield return Wait(Mathf.Max(0.3f, len - 0.9f));
                yield return Sneeze(chi, 0.45f, 1.6f);
                Hush();
                Cut(new Vector3(51, 7.5f, -31), new Vector3(54, 3.2f, -5), 55);
                AudioFx.Play("zap");
                StopCoroutine(rideCo);
                float cs0 = carSpeed;
                yield return Act(0.6f, t => { carSpeed = Mathf.Lerp(cs0, 1100, t / 0.6f); onion.yaw = carAngle * 2.2f; });
                shake = 1.2f;
                foreach (var t in five) t.mood = 5;
                narr = Bg(Say(wAll, "l48", "А-а-а-а-а!"));
                yield return Act(0.5f, t => onion.yaw = carAngle * 2.2f);
                riding = false;
                foreach (var t in five) t.tilt = 0;
                onion.pos = CAR + Vector3.up * 6.75f;
                Bg(Fly(bub, new Vector3(60, 0, -3.2f), 5f, 1.4f, 720, "boing"));
                Bg(Fly(chi, new Vector3(57.5f, 1.25f, 1), 6f, 1.5f, -720, "boing"));
                Bg(Fly(prof, HAY + Vector3.up * 0.4f, 7f, 1.6f, 540, "crate"));
                Bg(Fly(onion, FO, 6f, 1.6f, 540, "bonk"));
                Bg(Fly(puh, new Vector3(60, 2 * bub.r, -3.2f), 6f, 1.7f, -540, "boing"));
                Bg(Fly(luk, FO + Vector3.up * 2 * onion.r, 10f, 2.2f, 1080, "boing"));
                Glide(new Vector3(60.5f, 4.2f, -13.5f), new Vector3(61.5f, 2.0f, 0.5f), 1.3f, 50);
                yield return Wait(1.7f);
                bub.Land(0.6f);
                Bg(Fly(puh, new Vector3(61.8f, 0, -1.6f), 1.4f, 0.6f, 360, "land4"));
                Bg(Fly(chi, new Vector3(58.6f, 0, -0.6f), 1.2f, 0.6f, 0, "land5"));
                shake = 0.5f;
                yield return narr;
                yield return Wait(0.6f);
                Bg(Act(3f, t => carSpeed = Mathf.Lerp(carSpeed, 20, t / 3f)));

                // ---------- 13. caught! ----------
                Scene = 13;
                onion.spin = 0; luk.spin = 0; onion.Face(Cam0 + FO); onion.yaw = 180;
                luk.pos = FO + Vector3.up * 2 * onion.r; luk.yaw = 180;
                foreach (var t in five) { t.spin = 0; t.mood = 3; }
                luk.mood = 1;
                Cut(new Vector3(59.6f, 3.4f, -7.2f), new Vector3(60.5f, 3.6f, 2.5f), 45);
                Bg(Jump(luk, 0.5f, 0.4f, "boing"));
                yield return Say(wL, "l49", "Поймал! Я его поймал!");
                onion.mood = 2; onion.crying = true;
                CloseUp(onion, 3.0f, 0, 42);
                yield return Say(wO, "l50", "Пожалуйста, не варите меня...");
                onion.crying = false;
                // the Professor climbs out of the haystack
                Cut(new Vector3(63, 2.6f, -6.5f), new Vector3(64, 1.2f, 2), 46);
                AudioFx.Play("open");
                Spray(HAY + Vector3.up * 2.4f, M("F2D675", 0.1f), 16, 2f, 0.12f, 1f, 6, Vector3.up * 3);
                yield return Fly(prof, new Vector3(63.4f, 0, -0.6f), 2.2f, 0.7f, 360, "land6");
                prof.mood = 3;
                yield return Wait(0.5f);

                // ---------- 14. friends — and a gift ----------
                Scene = 14;
                onion.mood = 2;
                Bg(Fly(luk, FO + new Vector3(-0.3f, 0, -3.6f), 1.6f, 0.8f, 360, "land7"));
                float[] ox = { -0.3f, -2.8f, 1.2f, 2.6f, -1.6f }, oz = { -3.6f, -2.6f, -3.5f, -2.7f, -3.4f };
                for (int i = 1; i < 5; i++) Bg(Hop(five[i], FO + new Vector3(ox[i], 0, oz[i]), 0.9f, 0.4f, 3));
                yield return Wait(1.0f);
                foreach (var t in five) { t.Face(FO); t.mood = 0; t.spin = 0; }
                Cut(FO + new Vector3(-2.6f, 1.8f, -7.6f), FO + new Vector3(0, 1.7f, 0), 45);
                luk.mood = 1;
                yield return Say(wL, "l51", "Не бойся! Мы тебя не съедим. Давай дружить!");
                onion.mood = 1;
                CloseUp(onion, 3.4f, 0, 44);
                yield return Say(wO, "l52", "Правда? Ура! Тогда держите подарок!");
                // the onion blooms
                Cut(FO + new Vector3(0, 3.2f, -12), FO + new Vector3(0, 5.2f, 0), 50);
                flower.gameObject.SetActive(true);
                AudioFx.Play("magic");
                int noteK = 0;
                yield return Act(1.6f, t =>
                {
                    float u = Mathf.Clamp01(t / 1.2f);
                    float s = u < 1 ? Mathf.Sin(u * Mathf.PI * 0.5f) * (1 + 0.25f * Mathf.Sin(u * Mathf.PI)) : 1;
                    flower.localScale = Vector3.one * s;
                    if (noteK < 8 && t > noteK * 0.15f) { AudioFx.Note(noteK); noteK++; }
                });
                onion.Land(0.3f);
                Spray(W.InverseTransformPoint(flower.TransformPoint(new Vector3(0, 2.7f, 0))), M("E1B3FF", 0.4f), 30, 4f, 0.14f, 1.6f, 3, Vector3.up * 2);
                petals = true;
                AudioFx.Play("win3", 0.7f);
                foreach (var t in five) t.mood = 1;
                yield return Wait(0.8f);
                CloseUp(puh, 5.2f, 0.4f);
                puh.look = new Vector2(0, 0.8f);
                yield return Say(wP, "l53", "Ой, как красиво! Лук зацвёл!");
                puh.look = Vector2.zero;
                CloseUp(bub, 5.2f, -0.4f);
                bub.mood = 0; bub.look = new Vector2(0.5f, 0.8f);
                Bg(Act(2.2f, t => bub.tilt = Mathf.Sin(t * 1.5f) * 12));
                yield return Say(wB, "l54", "А суп из цветов бывает?");
                bub.look = Vector2.zero; bub.tilt = 0;
                Cut(FO + new Vector3(0, 2.2f, -10), FO + new Vector3(0, 1.2f, -2.8f), 46);
                foreach (var t in five) if (t != bub) { t.Face(bub.pos); t.mood = 5; }
                onion.look = new Vector2(0.6f, -0.4f);
                narr = Bg(Say(wAll, "l55", "Бублик!"));
                yield return Wait(0.3f);
                bub.Land(0.55f); bub.mood = 3;
                AudioFx.Play("boing");
                yield return narr;
                foreach (var t in five) t.mood = 1;
                AudioFx.Play("win2", 0.6f);
                yield return Act(1.0f, t => { foreach (var a in five) a.tilt = Mathf.Sin(t * 14 + a.r * 10) * 9; });
                foreach (var t in five) t.tilt = 0;

                // ---------- 15. the moral, «КОНЕЦ» ----------
                Scene = 15;
                onion.look = Vector2.zero; onion.mood = 1;
                float[] ex = { -0.6f, -3.2f, 0.8f, 2.3f, -1.9f }, ez = { -4.1f, -3.4f, -4.0f, -3.5f, -3.9f };
                for (int i = 0; i < 5; i++) { Put(five[i], FO + new Vector3(ex[i], 0, ez[i]), FO + Cam0); five[i].mood = 1; }
                if (nest) nest.gameObject.SetActive(true);
                Cut(FO + new Vector3(0, 2.4f, -12), FO + new Vector3(0, 2.4f, 0), 48);
                prof.mood = 0;
                Bg(Jump(prof, 0.6f, 0.45f));
                yield return Say(wPr, "l56", "Мораль: лук полезный. Но ешьте его понемножку!");
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                yield return Say(wN, "l57", "А друзей много не бывает! Конец!");
                len = Speak(wC, "l58", "А-а-апчхи! До свидания!");
                yield return Wait(Mathf.Max(0.2f, len * 0.35f));
                yield return Sneeze(chi, 0.3f, 1.4f);
                titleKick = 1;
                foreach (var t in five) if (t != chi) { t.Land(0.3f); t.lean = -15; }
                yield return Act(0.6f, t => { foreach (var a in five) if (a != chi) a.lean = Mathf.Lerp(-15, 0, t / 0.6f); });
                yield return Wait(Mathf.Max(0, len * 0.65f - 0.9f));
                Hush();
                var again = UIKit.Button(transform, "ЕЩЁ РАЗ", UIKit.Green, Play, 32, "Again");
                UIKit.Place(again.Bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(300, 86));
                againBtn = again.gameObject;
                foreach (var t in five) t.bob = 0.3f;
                var c = FO + new Vector3(0, 2.4f, 0);
                yield return Act(99999f, t =>
                {
                    float a = Mathf.Sin(t * 0.25f) * 0.5f;
                    camRig = () => (c + new Vector3(Mathf.Sin(a) * 12, 0.2f, -Mathf.Cos(a) * 12), c);
                });
            }

            void ApplyCamNow()
            {
                if (follow != null) { camLook = tgtLook = follow(); camPos = tgtPos = camLook + followOff; ApplyCam(); }
            }

            /// <summary>The camera rides on the carousel next to one of the riders.</summary>
            void RideCam(int i)
            {
                follow = null;
                camRig = () =>
                {
                    float a = (carAngle + rideSlot[i] - 32) * Mathf.Deg2Rad;
                    var p = CAR + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 6.4f + Vector3.up * 1.7f;
                    return (p, five[i].Center + Vector3.up * 0.2f);
                };
                fovT = 46; cam.fieldOfView = 46;
            }

            IEnumerator Intro(Toon t, Who w, string id, string text)
            {
                CloseUp(t, 5f, t == puh || t == prof ? -0.4f : 0.4f);
                t.mood = 1;
                Bg(Jump(t, 0.7f, 0.5f));
                yield return Say(w, id, text);
                t.mood = 0;
            }

            IEnumerator Candy(Vector3 from, Toon to)
            {
                var c = Node("Candy", W, from);
                S(c, Vector3.zero, 0.26f, M("FFFFFF", 0.7f));
                Ring(c, Vector3.zero, 0.13f, 0.03f, M("2ECC71", 0.6f), new Vector3(90, 0, 0));
                NoShadow(c.gameObject);
                Prim.SetLayer(c.gameObject, Layers.Preview);
                AudioFx.Play("flip", 0.4f);
                yield return FlyObj(c, to.mouth ? to.MouthAt : to.Center, 1.6f, 0.7f, 540);
                Destroy(c.gameObject);
                to.Land(0.3f);
                to.mood = to.mood == 4 ? 0 : 1;
                AudioFx.Play("coin", 0.5f);
                Spray(to.Center, G("B6FFDA", 2f), 8, 2f, 0.08f, 0.6f, 0, Vector3.up * 0.8f);
            }

            IEnumerator StoneChain(Toon a, float delay, Vector3 s1, Vector3 s2, Vector3 bank)
            {
                yield return Wait(delay);
                a.Face(s1);
                yield return Fly(a, s1, 0.9f, 0.5f, 0, "land1");
                a.Face(s2);
                yield return Fly(a, s2, 0.9f, 0.5f, 0, "land2");
                a.Face(bank);
                yield return Fly(a, bank, 0.9f, 0.55f, 0, "land3");
                a.Face(Cam0 + bank); a.mood = 1;
            }
        }
    }

    /// <summary>17.11 promo code «Секрет»: a list on the screen of where each of the 10 hidden nooks (golden eggs) is.</summary>
    public static class Secrets
    {
        static GameObject root;
        public static bool IsOpen => root != null;

        public static void Show()
        {
            Hide();
            var safe = UIRoot.Safe();
            var panel = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(1, 1, 1, 0.94f), "Secrets", true);
            UIKit.Place(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14 - safe.z, -20), new Vector2(640, 700));
            root = panel.gameObject;
            var title = UIKit.Txt(panel.transform, $"НЫЧКИ: {Eggs.Found} из {Eggs.Total}", 36, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -12), new Vector2(480, 54));
            var x = UIKit.Button(panel.transform, "X", UIKit.Red, Hide, 30, "Close");
            UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(74, 62));
            for (int k = 0; k < Eggs.Total; k++)
            {
                bool got = Save.P.found.Contains("egg" + k);
                var t = UIKit.Txt(panel.transform, $"{(got ? "✔" : (k + 1) + ".")} {Eggs.Where[k]}", 21, got ? UIKit.Lime : Color.white, TextAlignmentOptions.MidlineLeft, false);
                if (got && UIKit.Font != null && !UIKit.Font.HasCharacter('✔')) t.text = "+ " + Eggs.Where[k];
                UIKit.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -74 - k * 61), new Vector2(600, 58));
                t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 21;
            }
            AudioFx.Play("magic");
        }

        public static void Hide()
        {
            if (root) UnityEngine.Object.Destroy(root);
            root = null;
        }
    }
}
