using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static Chubrik.Parts;
using URandom = UnityEngine.Random;

namespace Chubrik
{
    /// <summary>
    /// 22.11 «Лукашарики»: 7 episodes. The cartoon opens with an episode picker (<see cref="Cartoon.Play()"/>);
    /// <see cref="Cartoon.Play(int)"/> plays one episode straight away (the autotest uses it).
    /// Episode 1 is <c>Run()</c> in Cartoon.cs; episodes 2–7 live here and reuse the same set, actors and helpers.
    /// Voices: Resources/Sounds/cartoon/eN_MM.wav (macOS «say», Milena), pitch per character as in episode 1.
    /// </summary>
    public static partial class Cartoon
    {
        public const int Episodes = 7;
        /// <summary>The episode playing now (1…7); 0 while the picker is shown.</summary>
        public static int Episode { get; private set; }
        static readonly string[] EpTitles =
        {
            "Луковый переполох", "Чих-пых", "Луковый день рождения", "Профессор и ракета",
            "Зимние Лукашарики", "Бублик-сыщик", "Большой концерт",
        };
        static readonly string[] EpOrdinal = { "первая", "вторая", "третья", "четвёртая", "пятая", "шестая", "седьмая" };
        static readonly int[] EpScenes = { 16, 9, 9, 7, 7, 8, 7 };
        /// <summary>Scenes in the current episode (0 while the picker is shown).</summary>
        public static int Scenes => Episode >= 1 && Episode <= Episodes ? EpScenes[Episode - 1] : 0;
        public static string EpisodeTitle(int ep) => EpTitles[Mathf.Clamp(ep, 1, Episodes) - 1];
        public static int ScenesIn(int ep) => EpScenes[Mathf.Clamp(ep, 1, Episodes) - 1];

        /// <summary>Plays one episode (1…7) without the picker.</summary>
        public static void Play(int episode) => Open(Mathf.Clamp(episode, 1, Episodes));

        partial class Show
        {
            Who wFour;            // everybody but Чихуня (a chorus)
            GameObject pickerGo;
            int chosen;
            bool raining;

            void SetVoices()
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
                wFour = Voice("Все", "FFC83A", 1.12f, luk, puh, bub, prof);
                wFour.chorus = true;
            }

            IEnumerator Begin()
            {
                SetVoices();
                yield return null;
                mus.Play();
                if (Episode == 0) yield return Picker();
                switch (Episode)
                {
                    case 2: yield return E2(); break;
                    case 3: yield return E3(); break;
                    case 4: yield return E4(); break;
                    case 5: yield return E5(); break;
                    case 6: yield return E6(); break;
                    case 7: yield return E7(); break;
                    default: yield return Run(); break;
                }
            }

            // ================= the episode picker =================
            IEnumerator Picker()
            {
                chosen = 0;
                Row(Vector3.zero);
                foreach (var t in five) { t.bob = 0.3f; t.mood = 1; }
                var panel = UIKit.Img(transform, UIKit.Panel, new Color(1, 1, 1, 0.9f), "Episodes", true);
                UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1160, 780));
                pickerGo = panel.gameObject;
                var head = UIKit.Txt(panel.transform, "ЛУКАШАРИКИ", 66, UIKit.Gold);
                UIKit.Place(head.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(1060, 84));
                var sub2 = UIKit.Txt(panel.transform, "Выбери серию", 32, Color.white);
                UIKit.Place(sub2.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(1060, 44));
                for (int i = 0; i < Episodes; i++)
                {
                    int ep = i + 1;
                    var b = UIKit.Button(panel.transform, $"Серия {ep}\n<size=72%>«{EpTitles[i]}»</size>", UIKit.Green, () => { chosen = ep; AudioFx.Play("click"); }, 36, "Episode" + ep);
                    b.Label.lineSpacing = -12;
                    int row = i / 2, col = i % 2;
                    float x = i == Episodes - 1 ? 0 : (col == 0 ? -282 : 282);
                    UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 168 - row * 134), new Vector2(540, 116));
                }
                var c = new Vector3(0, 1.1f, 0.2f);
                float clock0 = 0;
                while (chosen == 0)
                {
                    clock0 += Dt;
                    float a = Mathf.Sin(clock0 * 0.2f) * 0.45f;
                    camRig = () => (c + new Vector3(Mathf.Sin(a) * 9.5f, 1.4f, -Mathf.Cos(a) * 9.5f), c);
                    yield return null;
                }
                Destroy(pickerGo);
                pickerGo = null;
                camRig = null;
                foreach (var t in five) { t.bob = 0; t.mood = 0; }
                Episode = chosen;
            }

            /// <summary>The end of an episode: buttons «все серии», «следующая серия» (or «ещё раз»), «выход»; the camera sways.</summary>
            IEnumerator EndCard(Vector3 c, float dist = 12)
            {
                Hush();
                var bar = UIKit.Rect("EndButtons", transform);
                UIKit.Place(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1100, 96));
                againBtn = bar.gameObject;
                int ep = Mathf.Clamp(Episode, 1, Episodes);
                void Btn(string label, Action a, float x, float w)
                {
                    var b = UIKit.Button(bar, label, UIKit.Green, a, 32, label);
                    UIKit.Place(b.Bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(w, 90));
                }
                Btn("ВСЕ СЕРИИ", Cartoon.Play, -385, 300);
                if (ep < Episodes) Btn("СЛЕДУЮЩАЯ СЕРИЯ", () => Cartoon.Play(ep + 1), 0, 420);
                else Btn("ЕЩЁ РАЗ", () => Cartoon.Play(ep), 0, 420);
                Btn("ВЫХОД", Cartoon.Stop, 385, 300);
                foreach (var t in five) if (t.root.gameObject.activeSelf) t.bob = 0.3f;
                yield return Act(99999f, t =>
                {
                    float a = Mathf.Sin(t * 0.25f) * 0.5f;
                    camRig = () => (c + new Vector3(Mathf.Sin(a) * dist, 0.2f, -Mathf.Cos(a) * dist), c);
                });
            }

            // ================= shared helpers for the new episodes =================
            IEnumerator SayDo(Who w, string id, string text, Action<float> f, float tail = 0.2f)
            {
                float len = Speak(w, id, text);
                yield return Act(len + tail, f);
                Hush();
            }

            /// <summary>The five in a row around <paramref name="c"/>, facing the camera side.</summary>
            void Row(Vector3 c)
            {
                float[] x = { -3, -1.5f, 0, 1.5f, 3 }, z = { 0, 0.5f, 0, 0.5f, 0.2f };
                for (int i = 0; i < 5; i++) Put(five[i], c + new Vector3(x[i], 0, z[i]), c + new Vector3(x[i], 0, z[i]) + Vector3.back * 40);
            }

            void RowCam(Vector3 c, float fov = 50) => Cut(c + new Vector3(0, 2.2f, -9.5f), c + new Vector3(0, 1.1f, 2), fov);

            IEnumerator Cheer(float dur = 1f, string snd = "win1")
            {
                foreach (var a in five) a.mood = 1;
                if (snd != null) AudioFx.Play(snd, 0.6f);
                yield return Act(dur, t =>
                {
                    foreach (var a in five)
                    {
                        a.tilt = Mathf.Sin(t * 14 + a.r * 10) * 9;
                        a.hop = Mathf.Abs(Mathf.Sin(t * 7 + a.r * 13)) * 0.35f;
                    }
                });
                foreach (var a in five) { a.tilt = 0; a.hop = 0; }
            }

            /// <summary>A prop built from parts on the cartoon layer.</summary>
            Transform Prop(string name, Transform parent, Vector3 pos, Action<Transform> make)
            {
                int old = Parts.Layer;
                Parts.Layer = Layers.Preview;
                Transform t;
                try
                {
                    t = Node(name, parent ? parent : W, pos);
                    make?.Invoke(t);
                }
                finally { Parts.Layer = old; }
                Prim.SetLayer(t.gameObject, Layers.Preview);
                return t;
            }

            static readonly string[] Party = { "FF5FA2", "5FD3FF", "FFE14D", "6EDC3C", "B266FF", "FF9A2E" };

            void Confetti(Vector3 p, int n, float spread, Vector3 vel, bool glow = false)
            {
                foreach (var c in Party) Spray(p, glow ? G(c, 2.2f) : M(c, 0.5f), Mathf.Max(1, n / Party.Length), spread, glow ? 0.12f : 0.09f, glow ? 1.3f : 1.6f, glow ? 2f : 4f, vel);
            }

            Transform Dandelion(Vector3 p, Transform parent = null, float s = 1)
            {
                return Prop("Dandelion", parent, p, t =>
                {
                    t.localScale = Vector3.one * s;
                    Rod(t, Vector3.zero, new Vector3(0, 0.62f, 0), 0.035f, M("5DA83A", 0.3f));
                    var h = S(t, new Vector3(0, 0.7f, 0), 0.32f, M("FFFFFF", 0.25f));
                    h.name = "Head";
                    S(h.transform, Vector3.zero, 0.45f, M("F2E9A0", 0.3f));
                    Ca(t, new Vector3(0.09f, 0.06f, 0), new Vector3(0.07f, 0.16f, 0.03f), M("4C9A30", 0.3f), new Vector3(0, 0, -60));
                    Ca(t, new Vector3(-0.09f, 0.06f, 0.02f), new Vector3(0.07f, 0.16f, 0.03f), M("4C9A30", 0.3f), new Vector3(0, 30, 60));
                });
            }

            Transform RainCloud(Vector3 p)
            {
                return Prop("RainCloud", null, p, t =>
                {
                    var m = M("C3CEDD", 0.1f);
                    S(t, Vector3.zero, new Vector3(5.2f, 2.6f, 3.6f), m);
                    S(t, new Vector3(-2.2f, -0.3f, 0.3f), new Vector3(3.2f, 2.2f, 2.8f), m);
                    S(t, new Vector3(2.3f, -0.2f, -0.2f), new Vector3(3.4f, 2.3f, 3.0f), m);
                    S(t, new Vector3(0.6f, 1.0f, 0.2f), new Vector3(3.0f, 2.4f, 2.6f), m);
                    S(t, new Vector3(-1.0f, 0.7f, -0.6f), new Vector3(2.4f, 1.9f, 2.0f), m);
                    S(t, new Vector3(0, -0.7f, 0), new Vector3(5.6f, 1.2f, 3.2f), M("A9B6C8", 0.1f));
                });
            }

            IEnumerator Rain(Transform cloud)
            {
                var m = M("8FD8FF", 0.9f);
                while (raining && cloud)
                {
                    var p = cloud.localPosition + new Vector3(URandom.Range(-4f, 4f), -1.2f, URandom.Range(-2.2f, 2.2f));
                    Spray(p, m, 2, 0.05f, 0.08f, 1.3f, 9, Vector3.down * 5);
                    yield return Wait(0.03f);
                }
            }

            /// <summary>Snow keeps falling around whatever the camera looks at (episode 5).</summary>
            IEnumerator Snowfall()
            {
                var m = M("FFFFFF", 0.3f);
                while (true)
                {
                    var p = camLook + new Vector3(URandom.Range(-14f, 14f), URandom.Range(6f, 10f), URandom.Range(-8f, 10f));
                    Spray(p, m, 2, 0.35f, 0.09f, 6f, 0, new Vector3(0.25f, -1.4f, 0));
                    yield return Wait(0.05f);
                }
            }

            Transform Balloon(Vector3 p, string col, Transform parent = null)
            {
                return Prop("Balloon", parent, p, t =>
                {
                    S(t, Vector3.zero, new Vector3(0.6f, 0.72f, 0.6f), M(col, 0.8f));
                    Cone(t, new Vector3(0, -0.4f, 0), new Vector3(0.12f, 0.1f, 0.12f), M(col, 0.6f), new Vector3(180, 0, 0));
                    Rod(t, new Vector3(0, -0.42f, 0), new Vector3(0, -1.5f, 0), 0.015f, M("FFFFFF", 0.2f));
                    NoShadow(t.gameObject);
                });
            }

            IEnumerator Grow(Transform t, float to, float dur)
            {
                float s0 = t.localScale.x;
                yield return Act(dur, u => { float k = u / dur; t.localScale = Vector3.one * Mathf.LerpUnclamped(s0, to, k < 1 ? 1 - Mathf.Pow(1 - k, 3) * Mathf.Cos(k * 6) : 1); });
                t.localScale = Vector3.one * to;
            }

            // =====================================================================================
            // ================= Серия 2: «Чих-пых» ==================================================
            // =====================================================================================
            IEnumerator E2()
            {
                // ---------- 0. title ----------
                Scene = 0;
                Put(onion, G0, Cam0); onion.mood = 2;
                onion.top.localScale = new Vector3(1.3f, 0.45f, 1.3f);      // wilted feathers
                foreach (var o in fieldOnions) if ((o.localPosition - G0).magnitude < 2.9f) o.gameObject.SetActive(false);
                var cloud = RainCloud(new Vector3(-6.6f, 10.5f, 9.2f));
                var dands = new List<Transform>();
                foreach (var p in new[] { V(3.8f, -2.4f), V(5.0f, -2.0f), V(6.2f, -2.6f), V(4.4f, -3.5f), V(5.7f, -3.7f), V(7.0f, -3.2f), V(2.6f, -3.1f) })
                    dands.Add(Dandelion(p));
                Row(Vector3.zero);
                foreach (var t in five) t.bob = 0.3f;
                Cut(new Vector3(0, 16, -34), new Vector3(2, 1, 6));
                Glide(new Vector3(0, 2.2f, -9.5f), new Vector3(0, 1.1f, 2), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Чих-пых»");
                AudioFx.Play("portal");
                yield return Say(wN, "e2_01", "Лукашарики! Серия вторая: «Чих-пых»!");
                Title(null);
                foreach (var t in five) t.bob = 0;
                Glide(new Vector3(5, 1.5f, -9f), new Vector3(5, 0.7f, -2.5f), 0.9f, 46);
                yield return Say(wN, "e2_02", "Было жаркое-прежаркое утро. На лугу распушились одуванчики.");

                // ---------- 1. a dandelion and a tickly nose ----------
                Scene = 1;
                Put(chi, V(2.0f, -1.2f), Cam0); Put(luk, V(3.5f, -0.6f), Cam0); Put(puh, V(5f, -1.0f), V(5f, -2.0f));
                Put(bub, V(6.6f, -0.9f), Cam0); Put(prof, V(8f, -1.2f), Cam0);
                puh.mood = 1;
                Cut(new Vector3(3.0f, 1.0f, -4.4f), new Vector3(5f, 0.75f, -1.5f), 44);
                yield return Say(wP, "e2_03", "Смотрите, какие пушистые! Подую — и полетят!");
                var head = dands[1].Find("Head");
                yield return Act(0.5f, t => puh.puff = 1 + 0.18f * t / 0.5f);
                AudioFx.Play("swing", 0.5f);
                puh.puff = 1; puh.Land(-0.3f);
                var hp = W.InverseTransformPoint(head.position);
                Spray(hp, M("FFFFFF", 0.2f), 26, 0.45f, 0.07f, 2.8f, -0.12f, (chi.Center - hp).normalized * 1.3f);
                head.localScale = Vector3.one * 0.12f;
                Cut(new Vector3(3.6f, 1.3f, -7.0f), new Vector3(3.2f, 0.6f, -1.4f), 46);
                yield return Wait(1.5f);
                CloseUp(chi, 5.2f, 0.3f);
                chi.mood = 5;
                yield return SayDo(wC, "e2_04", "Ой... пушинка... мне в нос... щекотно...", t => chi.noseK = 1 + 0.3f * Mathf.Abs(Mathf.Sin(t * 6)));
                chi.noseK = 1;
                CloseUp(luk, 5f, -0.4f); luk.mood = 5;
                Bg(Jump(luk, 0.4f, 0.35f));
                yield return Say(wL, "e2_05", "Чихуня, держись! Только не чихай!");
                luk.mood = 0;

                // ---------- 2. sneeze, sneeze ----------
                Scene = 2;
                Put(chi, V(2.0f, -2.4f)); Put(bub, V(0.6f, -2.4f));
                chi.Face(bub.pos); bub.Face(chi.pos);
                Cut(new Vector3(1.2f, 2.0f, -10.5f), new Vector3(1.2f, 0.8f, -1.8f), 52);
                chi.mood = 5;
                float len = Speak(wC, "e2_06", "Не могу... а... а... АПЧХИ!");
                yield return Wait(Mathf.Max(0.3f, len - 0.9f));
                yield return Sneeze(chi, 0.45f, 1.6f);
                shake = 0.5f; bub.mood = 5;
                yield return Fly(bub, V(-3.2f, -2.6f), 1.2f, 0.7f, -540, "bonk");
                Hush();
                Glide(new Vector3(-3f, 1.8f, -10f), new Vector3(-4f, 0.6f, -2.8f), 2f);
                yield return RollTo(bub, V(-6.2f, -3.0f), 1.2f, 0.2f);
                bub.Face(Cam0); bub.mood = 3;
                yield return Wait(0.4f);
                CloseUp(bub, 5.4f, 0.4f); bub.mood = 1;
                yield return Say(wB, "e2_07", "Эй! Я вам не мячик! Хотя... катиться весело!");
                bub.mood = 0;
                Cut(new Vector3(2.8f, 1.7f, -9f), new Vector3(2.9f, 0.6f, -2.4f), 46);
                yield return Hop(prof, V(3.6f, -2.6f), 1.0f, 0.4f, 3);
                prof.Face(chi.pos); chi.Face(prof.pos);
                chi.mood = 5;
                len = Speak(wC, "e2_08", "Ой, опять... АПЧХИ!");
                yield return Wait(Mathf.Max(0.2f, len - 0.8f));
                yield return Sneeze(chi, 0.35f, 1.3f);
                Hush();
                prof.mood = 3; AudioFx.Play("flip");
                Cut(prof.Center + new Vector3(-1.2f, 0.5f, -3.0f), prof.Center + Vector3.up * 0.1f, 44);
                len = Speak(wPr, "e2_09", "Я... кручусь... как волчок... Очень научно!");
                float spinT = len + 0.3f;
                yield return Act(spinT, t =>
                {
                    prof.yaw += Mathf.Lerp(1400, 150, t / spinT) * Dt;
                    prof.tilt = Mathf.Sin(t * 9) * 12 * (1 - t / spinT);
                });
                Hush();
                prof.tilt = 0; prof.Face(Cam0); prof.Land(0.4f);
                yield return Wait(0.5f);
                prof.mood = 0; chi.mood = 0;

                // ---------- 3. the thirsty onion and a far-away cloud ----------
                Scene = 3;
                Cut(G0 + new Vector3(0, 2.4f, -7.5f), G0 + new Vector3(0, 2.3f, 0), 40);
                yield return SayDo(wO, "e2_10", "Ох, как жарко... Мои пёрышки совсем завяли.", t => onion.tilt = Mathf.Sin(t * 1.5f) * 4);
                onion.tilt = 0;
                Put(luk, V(6.5f, 1.3f), G0); Put(puh, V(5.2f, 0.9f), G0); Put(bub, V(9.6f, 1.0f), G0); Put(prof, V(8.2f, 0.6f), G0); Put(chi, V(11f, 1.2f), G0);
                luk.mood = 2;
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e2_11", "Бедный Лук! Ему нужен дождик!");
                luk.mood = 0;
                prof.look = new Vector2(-0.6f, 0.6f);
                CloseUp(prof, 5f, -0.4f);
                yield return Say(wPr, "e2_12", "Вон за домиком тучка! Но ветра нет, и она стоит на месте.");
                prof.look = Vector2.zero;
                Cut(new Vector3(-1, 3, -6), cloud.localPosition + Vector3.down, 45);
                yield return Wait(1.3f);
                CloseUp(puh, 5f, 0.4f);
                yield return Say(wP, "e2_13", "Ветра нет... Кто же её подтолкнёт?");
                Cut(new Vector3(8.5f, 2.0f, -5.5f), new Vector3(8.5f, 0.6f, 1.0f), 50);
                foreach (var t in five) if (t != chi) t.Face(chi.pos);
                chi.Face(Cam0);
                yield return Wait(0.7f);
                chi.mood = 5;
                yield return Say(wFour, "e2_14", "Чихуня!");
                CloseUp(chi, 5f, 0.3f);
                yield return SayDo(wC, "e2_15", "Я? Но я не умею чихать по заказу!", t => chi.tilt = Mathf.Sin(t * 10) * 8);
                chi.tilt = 0; chi.mood = 0;

                // ---------- 4. tickles and pepper ----------
                Scene = 4;
                var tickle = Dandelion(new Vector3(0.5f, 0.35f, 0.5f), bub.root, 0.8f);
                tickle.localEulerAngles = new Vector3(60, 0, 0);
                Cut(new Vector3(10.6f, 1.4f, -2.8f), new Vector3(10.6f, 0.6f, 1.1f), 42);
                bub.mood = 1;
                yield return Hop(bub, new Vector3(10.1f, 0, 1.05f), 0.6f, 0.3f, 2);
                bub.Face(chi.pos); chi.Face(Cam0);
                yield return SayDo(wB, "e2_16", "Сейчас помогу! Щекотка-щекотушка!", t =>
                {
                    bub.lean = 12 * Mathf.Sin(t * 14);
                    tickle.localRotation = Quaternion.Euler(60 + Mathf.Sin(t * 18) * 20, 0, 0);
                    chi.mood = 1; chi.tilt = Mathf.Sin(t * 16) * 10;
                });
                bub.lean = 0;
                yield return SayDo(wC, "e2_17", "Хи-хи-хи! Щекотно, но не чихается!", t => { chi.tilt = Mathf.Sin(t * 16) * 10; chi.hop = Mathf.Abs(Mathf.Sin(t * 8)) * 0.25f; });
                chi.tilt = 0; chi.hop = 0; chi.mood = 0;
                tickle.gameObject.SetActive(false);
                yield return Hop(prof, new Vector3(11.9f, 0, 0.6f), 0.7f, 0.3f, 2);
                prof.Face(chi.pos + Vector3.back * 3);
                CloseUp(prof, 5f, -0.4f);
                Bg(Jump(prof, 0.5f, 0.4f));
                AudioFx.Play("magic", 0.5f);
                yield return Say(wPr, "e2_18", "Нужно средство посильнее. Щепотка перца!");
                shaker.gameObject.SetActive(true);
                chi.Face(Cam0);
                Cut(chi.Center + new Vector3(0.6f, 0.35f, -2.6f), chi.Center, 40);
                chi.mood = 5;
                len = Speak(wC, "e2_19", "Ой-ой-ой... сейчас будет большой чих... Все держитесь!");
                yield return Act(len + 0.2f, t =>
                {
                    chi.noseK = 1 + 0.6f * Mathf.Clamp01(t / len);
                    chi.hop = Mathf.Abs(Mathf.Sin(t * 5)) * 0.5f;
                    if (URandom.value < 0.25f) Spray(W.InverseTransformPoint(shaker.position) + Vector3.up * 0.2f, M("5A3A22", 0.2f), 1, 0.3f, 0.05f, 0.7f, 6);
                });
                Hush();
                chi.hop = 0;
                shaker.gameObject.SetActive(false);

                // ---------- 5. the great sneeze-wind ----------
                Scene = 5;
                Put(chi, V(-12.5f, 10.5f), G0);
                Put(luk, V(-14f, 9.6f), G0); Put(puh, V(-14.2f, 11.6f), G0); Put(bub, V(-15.6f, 10.4f), G0); Put(prof, V(-13.7f, 12.7f), G0);
                chi.mood = 5; chi.noseK = 1.6f;
                foreach (var t in five) if (t != chi) t.mood = 5;
                Cut(chi.Center + new Vector3(2.8f, 0.5f, -1.2f), chi.Center + Vector3.up * 0.2f + Vector3.left * 1.2f, 46);
                len = Speak(wC, "e2_20", "А-а-а... А-А-АПЧХИ-И-И!");
                yield return Wait(Mathf.Max(0.3f, len - 1.0f));
                chi.noseK = 1;
                yield return Sneeze(chi, 0.6f, 2.6f);
                shake = 1.2f;
                AudioFx.Play("hum");
                foreach (var t in five) if (t != chi) { t.Land(0.5f); t.lean = -20; }
                Bg(Fly(chi, chi.pos + V(-1.3f, 0), 0.8f, 0.5f, -360, "land1"));
                yield return Wait(0.6f);
                Hush();
                foreach (var t in five) t.lean = 0;
                var c0 = cloud.localPosition;
                var c1 = new Vector3(8, 9, 6);
                Cut(new Vector3(-2, 5, -15), c0 + Vector3.down * 2, 58);
                camRate = 2f;
                var narr = Bg(Say(wN, "e2_21", "Апчхи-ветер подхватил тучку — и понёс прямо к огороду!"));
                yield return Act(3.4f, t =>
                {
                    float u = Mathf.SmoothStep(0, 1, t / 3.4f);
                    cloud.localPosition = Vector3.Lerp(c0, c1, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.5f;
                    cloud.localRotation = Quaternion.Euler(0, u * 30, Mathf.Sin(t * 6) * 4);
                    tgtLook = cloud.localPosition + Vector3.down * 3;
                    tgtPos = new Vector3(Mathf.Lerp(-2, 6, u), 5, -15);
                });
                yield return narr;

                // ---------- 6. rain! ----------
                Scene = 6;
                raining = true;
                Bg(Rain(cloud));
                float[] px = { 5, 6.5f, 8, 9.5f, 11 };
                for (int i = 0; i < 5; i++) { Put(five[i], V(px[i], 1.2f + (i % 2) * 0.3f), Cam0); five[i].mood = 1; }
                Cut(new Vector3(8, 2.4f, -7.5f), new Vector3(8, 2.6f, 4), 52);
                narr = Bg(Say(wAll, "e2_22", "Ура! Дождик! Дождик!"));
                yield return Act(2.6f, t => { for (int i = 0; i < 5; i++) five[i].hop = Mathf.Abs(Mathf.Sin(t * 6 + i)) * 0.6f; });
                yield return narr;
                foreach (var t in five) t.hop = 0;
                CloseUp(onion, 3.0f, 0, 42);
                var ts = onion.top.localScale;
                AudioFx.Play("magic");
                yield return Act(1.3f, t => onion.top.localScale = Vector3.Lerp(ts, Vector3.one, t / 1.3f));
                onion.mood = 1; onion.Land(0.3f);
                AudioFx.Play("boing", 0.6f);
                yield return Say(wO, "e2_23", "Ах, как хорошо! Спасибо, Чихуня!");

                // ---------- 7. a rainbow ----------
                Scene = 7;
                raining = false;
                var rainbow = Prop("Rainbow", null, new Vector3(8, 0, 20), t =>
                {
                    string[] cols = { "FF4D4D", "FF9A2E", "FFE14D", "6EDC3C", "5FD3FF", "3D8BFF", "9B6BFF" };
                    for (int k = 0; k < cols.Length; k++) Ring(t, Vector3.zero, 9.2f - k * 0.42f, 0.22f, Glass(cols[k], 0.6f), new Vector3(90, 0, 0));
                    NoShadow(t.gameObject);
                });
                rainbow.localScale = Vector3.zero;
                Bg(Act(8f, t => { if (cloud) { cloud.localPosition = c1 + new Vector3(t * 3, t * 0.6f, t * 2.5f); cloud.localScale = Vector3.one * Mathf.Max(0.2f, 1 - t * 0.08f); } }));
                Cut(new Vector3(8, 2.0f, -9.5f), new Vector3(8, 5.5f, 12), 55);
                AudioFx.Play("win3", 0.7f);
                yield return Act(1.6f, t => rainbow.localScale = Vector3.one * Mathf.SmoothStep(0, 1, t / 1.6f));
                puh.look = new Vector2(0, 0.8f);
                Bg(Jump(puh, 0.6f, 0.45f));
                yield return Say(wP, "e2_24", "Смотрите! Радуга!");
                puh.look = Vector2.zero;

                // ---------- 8. the moral ----------
                Scene = 8;
                foreach (var t in five) { t.Face(Cam0); t.mood = 1; }
                Cut(new Vector3(8, 2.2f, -9f), new Vector3(8, 1.9f, 3), 48);
                Bg(Jump(prof, 0.6f, 0.45f));
                yield return Say(wPr, "e2_25", "Вывод: чихать полезно! Но только в платочек!");
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                len = Speak(wC, "e2_26", "Будьте здоровы! Ой... то есть... АПЧХИ!");
                yield return Wait(Mathf.Max(0.2f, len - 0.8f));
                yield return Sneeze(chi, 0.3f, 1.4f);
                titleKick = 1;
                foreach (var t in five) if (t != chi) t.Land(0.3f);
                yield return Wait(0.5f);
                yield return EndCard(new Vector3(8, 1.6f, 1.3f), 11);
            }

            // =====================================================================================
            // ================= Серия 3: «Луковый день рождения» ====================================
            // =====================================================================================
            IEnumerator E3()
            {
                var CK = V(-1.5f, -1.5f);           // the cake
                var BS = V(-4.6f, -0.4f);           // the balloon stake
                var ON = V(3.9f, -1.6f);            // where the onion stops

                // ---------- 0. title ----------
                Scene = 0;
                Put(onion, G0, Cam0); onion.mood = 4;
                foreach (var o in fieldOnions) if ((o.localPosition - G0).magnitude < 2.9f) o.gameObject.SetActive(false);
                Row(V(-1.5f, 1.4f));
                foreach (var t in five) t.bob = 0.3f;
                Cut(new Vector3(0, 16, -34), new Vector3(0, 1, 4));
                Glide(new Vector3(-1.5f, 2.2f, -8.5f), new Vector3(-1.5f, 1.1f, 3.4f), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Луковый день рождения»");
                AudioFx.Play("portal");
                yield return Say(wN, "e3_01", "Лукашарики! Серия третья: «Луковый день рождения»!");
                Title(null);
                foreach (var t in five) t.bob = 0;
                Cut(G0 + new Vector3(-1.5f, 2.6f, -8f), G0 + new Vector3(0, 2.4f, 0), 42);
                Glide(G0 + new Vector3(1.5f, 2.6f, -7f), G0 + new Vector3(0, 2.4f, 0), 0.4f);
                yield return Say(wN, "e3_02", "Сегодня у большого Лука день рождения. Но он об этом ещё не знает.");

                // ---------- 1. a secret ----------
                Scene = 1;
                Row(V(-1.5f, 1.4f));
                CloseUp(luk, 5f, 0.4f);
                luk.look = new Vector2(-0.6f, 0);
                yield return SayDo(wL, "e3_03", "Тс-с-с! Готовим сюрприз! Только тихо!", t => luk.lean = 8 * Mathf.Sin(t * 3));
                luk.lean = 0; luk.look = Vector2.zero;

                // ---------- 2. balloons ----------
                Scene = 2;
                Prop("Stake", null, BS, t => { Rod(t, Vector3.zero, new Vector3(0, 1.0f, 0), 0.08f, Look.Planks("A0642F", 0.3f)); });
                Put(puh, BS + V(1.4f, -0.6f), BS + V(1.4f, -0.6f) + Vector3.back * 40);
                Put(bub, BS + V(3.4f, 0.4f), Cam0);
                Cut(puh.Center + new Vector3(0.5f, 0.6f, -3.6f), puh.Center + new Vector3(-0.6f, 0.6f, 0), 46);
                puh.mood = 1;
                string[] bc = { "FF5FA2", "5FD3FF", "FFE14D" };
                len0 = Speak(wP, "e3_04", "Я надую шарики! Раз шарик, два шарик...");
                for (int k = 0; k < 2; k++)
                {
                    var bl = Balloon(puh.MouthAt + puh.Fwd * 0.35f + Vector3.up * 0.1f, bc[k]);
                    bl.localScale = Vector3.zero;
                    yield return Act(len0 * 0.3f, t => { float u = t / (len0 * 0.3f); bl.localScale = Vector3.one * u; puh.puff = 1 + 0.15f * Mathf.Sin(u * Mathf.PI * 3); });
                    puh.puff = 1;
                    AudioFx.Play("flip", 0.5f);
                    Bg(FlyObj(bl, BS + new Vector3(-0.4f + k * 0.7f, 2.4f + k * 0.4f, 0.2f), 0.8f, 0.7f, 0));
                }
                yield return Wait(len0 * 0.35f);
                Hush();
                // the third balloon is too big
                var big = Balloon(new Vector3(0, 2 * puh.r + 0.6f, 0.1f), bc[2], puh.root);
                big.localScale = Vector3.zero;
                AudioFx.Play("hum", 0.6f);
                yield return Act(1.2f, t => { big.localScale = Vector3.one * Mathf.Lerp(0, 2.4f, t / 1.2f); big.localPosition = new Vector3(0, 2 * puh.r + 0.2f + big.localScale.x * 0.45f, 0.1f); });
                puh.mood = 5;
                Cut(puh.pos + new Vector3(0.6f, 2.0f, -8.5f), puh.pos + Vector3.up * 2.2f, 50);
                len0 = Speak(wP, "e3_05", "Ой! Я лечу! Снимите меня!");
                yield return Act(len0 + 0.6f, t => { puh.hop = Mathf.Min(3.2f, t * 1.6f); puh.tilt = Mathf.Sin(t * 3) * 10; });
                Hush();
                prof.Face(puh.pos); prof.look = new Vector2(0, 1);
                CloseUp(prof, 5f, -0.4f);
                Bg(Hop(bub, new Vector3(puh.pos.x, 0, puh.pos.z), 1.6f, 0.4f, 4));
                yield return Say(wPr, "e3_06", "Спокойно! Закон природы: что взлетело — то приземлится!");
                prof.look = Vector2.zero;
                Cut(puh.pos + new Vector3(0.6f, 2.0f, -8.5f), puh.pos + Vector3.up * 1.8f, 50);
                yield return Wait(0.2f);
                AudioFx.Play("bonk");
                Confetti(W.InverseTransformPoint(big.position), 18, 2f, Vector3.up * 1.5f);
                big.gameObject.SetActive(false);
                float h0 = puh.hop;
                yield return Act(0.45f, t => puh.hop = Mathf.Lerp(h0, 2 * bub.r, (t / 0.45f) * (t / 0.45f)));
                puh.tilt = 0; puh.Land(0.4f); bub.Land(0.6f); bub.mood = 3;
                AudioFx.Play("boing");
                yield return Wait(0.6f);
                puh.pos.y = 2 * bub.r; puh.hop = 0;
                yield return Fly(puh, puh.pos + new Vector3(-1.1f, -2 * bub.r, -0.3f), 0.6f, 0.5f, 360, "land2");
                puh.mood = 1; puh.Face(Cam0); bub.mood = 0;

                // ---------- 3. the cake (and Бублик) ----------
                Scene = 3;
                Transform top = null;
                var flames = new List<GameObject>();
                var candles = new List<GameObject>();
                var cake = Prop("Cake", null, CK, t =>
                {
                    Cy(t, new Vector3(0, 0.02f, 0), new Vector3(2.4f, 0.02f, 2.4f), M("FFFFFF", 0.6f));
                    Cy(t, new Vector3(0, 0.27f, 0), new Vector3(1.9f, 0.25f, 1.9f), M("FFF0F5", 0.45f));
                    Ring(t, new Vector3(0, 0.5f, 0), 0.95f, 0.07f, M("FF8FC0", 0.5f));
                    Cy(t, new Vector3(0, 0.75f, 0), new Vector3(1.35f, 0.25f, 1.35f), M("FFB6D0", 0.45f));
                    Ring(t, new Vector3(0, 0.99f, 0), 0.67f, 0.06f, M("FFFFFF", 0.5f));
                    top = Cy(t, new Vector3(0, 1.18f, 0), new Vector3(0.85f, 0.2f, 0.85f), M("FFF0F5", 0.45f)).transform;
                    S(top, new Vector3(0, 1.3f, 0), new Vector3(0.3f, 0.6f, 0.3f), M("E53935", 0.8f));
                    for (int k = 0; k < 5; k++)
                    {
                        float a = (k * 72 + 20) * Mathf.Deg2Rad;
                        var p = new Vector3(Mathf.Sin(a) * 0.52f, 1.14f, Mathf.Cos(a) * 0.52f);
                        var cd = Cy(t, p, new Vector3(0.07f, 0.14f, 0.07f), M(Party[k], 0.5f));
                        candles.Add(cd);
                        var f = Cone(t, p + Vector3.up * 0.16f, new Vector3(0.09f, 0.2f, 0.09f), G("FFC83A", 2f));
                        NoShadow(f);
                        flames.Add(f);
                    }
                    for (int k = 0; k < 3; k++)
                    {
                        var g = B(t, new Vector3(-1.9f + k * 0.5f, 0.25f, 1.6f - k * 0.9f), Vector3.one * (0.5f - k * 0.06f), M(Party[k + 1], 0.4f), new Vector3(0, 20 * k, 0));
                        B(g.transform, Vector3.zero, new Vector3(1.04f, 1.04f, 0.2f), M("FFFFFF", 0.5f));
                        B(g.transform, Vector3.zero, new Vector3(0.2f, 1.04f, 1.04f), M("FFFFFF", 0.5f));
                    }
                });
                foreach (var c in candles) c.SetActive(false);
                foreach (var f in flames) f.SetActive(false);
                cake.localScale = Vector3.zero;
                Put(bub, CK + V(1.6f, -0.6f), CK); Put(luk, CK + V(-1.8f, -1.2f), CK); Put(chi, CK + V(0.2f, -1.9f), CK);
                Put(prof, CK + V(2.4f, 0.8f), CK); Put(puh, CK + V(-2.4f, 0.4f), CK);
                Cut(CK + new Vector3(0.8f, 2.4f, -6.5f), CK + new Vector3(0, 0.7f, 0), 46);
                bub.mood = 1;
                Bg(Jump(bub, 0.6f, 0.45f));
                len0 = Speak(wB, "e3_07", "А я испеку торт! Большой-пребольшой!");
                yield return Wait(len0 * 0.5f);
                AudioFx.Play("magic");
                Spray(CK + Vector3.up * 0.8f, G("FFF2B0", 2f), 20, 2f, 0.1f, 0.8f, 0, Vector3.up);
                yield return Grow(cake, 1, 0.8f);
                yield return Wait(Mathf.Max(0, len0 * 0.5f - 0.8f));
                Hush();
                bub.mood = 6;
                CloseUp(bub, 5.4f, 0.3f);
                var topS = top.localScale;
                yield return SayDo(wB, "e3_08", "Одну ложечку попробую... и ещё одну... и ещё...", t =>
                {
                    bub.lean = 14 * Mathf.Abs(Mathf.Sin(t * 6));
                    top.localScale = new Vector3(topS.x, topS.y * Mathf.Max(0.2f, 1 - t * 0.22f), topS.z) * Mathf.Max(0.35f, 1 - t * 0.15f);
                    top.localPosition = new Vector3(0, 1.0f + top.localScale.y, 0);
                    if (URandom.value < 0.05f) AudioFx.Play("click", 0.25f);
                });
                bub.lean = 0;
                luk.Face(bub.pos); luk.mood = 5;
                CloseUp(luk, 5f, -0.4f);
                Bg(Jump(luk, 0.5f, 0.4f));
                yield return Say(wL, "e3_09", "Бублик! Торт — для Лука, а не для тебя!");
                bub.mood = 1;
                CloseUp(bub, 5.4f, 0.3f);
                yield return SayDo(wB, "e3_10", "Я проверял, вкусный ли он. Очень вкусный!", t => bub.tilt = Mathf.Sin(t * 5) * 8);
                bub.tilt = 0; luk.mood = 0;

                // ---------- 4. candles — and a sneeze too early ----------
                Scene = 4;
                chi.Face(CK);
                Cut(CK + new Vector3(-0.4f, 1.9f, -4.6f), CK + new Vector3(0, 0.9f, -0.2f), 44);
                chi.mood = 1;
                len0 = Speak(wC, "e3_11", "А я поставлю свечки! Одна, две, три... ой, пахнет ванилью... АПЧХИ!");
                for (int k = 0; k < 5; k++)
                {
                    yield return Wait(Mathf.Max(0.15f, len0 * 0.11f));
                    candles[k].SetActive(true);
                    AudioFx.Play("click", 0.5f);
                    chi.Land(0.25f);
                }
                yield return Wait(Mathf.Max(0.2f, len0 * 0.45f - 0.9f));
                yield return Sneeze(chi, 0.4f, 1.4f);
                shake = 0.5f;
                AudioFx.Play("win0", 0.6f);
                Confetti(CK + Vector3.up * 1.4f, 48, 3f, Vector3.up * 3.5f);
                yield return Wait(0.6f);
                Hush();
                foreach (var t in five) t.mood = 5;
                Cut(CK + new Vector3(0.6f, 2.6f, -7.2f), CK + new Vector3(0, 0.8f, 0), 50);
                yield return Wait(0.5f);
                prof.Face(Cam0); prof.mood = 0;
                CloseUp(prof, 5f, -0.4f);
                yield return Say(wPr, "e3_12", "Конфетти раньше времени. Ничего, наука всё соберёт!");
                foreach (var t in five) t.mood = 0;

                // ---------- 5. here comes the onion ----------
                Scene = 5;
                Cut(new Vector3(6.5f, 2.6f, -9f), new Vector3(5, 2.2f, 2), 50);
                foreach (var t in five) Bg(Hop(t, V(-8.3f + (t.r - 0.5f) * 3, 9.8f), 1.2f, 0.5f, 4));
                narr0 = Bg(Say(wN, "e3_13", "Все спрятались за домиком. И тут пришёл Лук."));
                onion.mood = 0;
                yield return Wait(0.8f);
                foreach (var t in five) Away(t);
                yield return RollTo(onion, ON, 2.4f, 0.3f);
                onion.Face(CK);
                yield return narr0;
                onion.mood = 2;
                onion.look = new Vector2(-0.6f, 0);
                CloseUp(onion, 3.2f, 0, 42);
                yield return SayDo(wO, "e3_14", "Эй, где все? Тихо... Наверное, все забыли про мой день рождения...", t => onion.look = new Vector2(Mathf.Sin(t * 1.2f) * 0.8f, -0.2f));
                onion.look = Vector2.zero;

                // ---------- 6. surprise! ----------
                Scene = 6;
                Vector3[] spot = { V(0.1f, -3.0f), V(-3.3f, -2.4f), V(-3.1f, -0.2f), V(0.0f, 0.4f), V(-1.4f, -3.4f) };
                for (int i = 0; i < 5; i++) Put(five[i], V(-8.3f + i * 0.6f, 9.8f));
                Cut(new Vector3(0.5f, 3.0f, -12.5f), new Vector3(0.5f, 1.6f, -0.5f), 55);
                for (int i = 0; i < 5; i++) Bg(Fly(five[i], spot[i], 4f, 1.1f, 360, "land" + i));
                yield return Wait(1.15f);
                foreach (var t in five) { t.Face(onion.pos); t.mood = 1; t.spin = 0; }
                AudioFx.Play("win2", 0.7f);
                Confetti(CK + Vector3.up * 2f, 60, 4f, Vector3.up * 4);
                onion.mood = 5;
                narr0 = Bg(Say(wAll, "e3_15", "Сюрприз! С днём рождения!"));
                yield return Act(1.8f, t => { foreach (var a in five) a.hop = Mathf.Abs(Mathf.Sin(t * 7 + a.r * 9)) * 0.5f; });
                yield return narr0;
                foreach (var t in five) t.hop = 0;
                onion.mood = 1;
                CloseUp(onion, 3.2f, 0, 44);
                yield return SayDo(wO, "e3_16", "Ой! Это всё мне? Вот это да!", t => onion.hop = Mathf.Abs(Mathf.Sin(t * 5)) * 0.4f);
                onion.hop = 0; onion.Land(0.3f);

                // ---------- 7. the wish ----------
                Scene = 7;
                AudioFx.Play("magic");
                foreach (var f in flames) f.SetActive(true);
                Cut(new Vector3(0.5f, 3.0f, -12.5f), new Vector3(0.5f, 1.6f, -0.5f), 55);
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e3_17", "Загадай желание и задуй свечки!");
                Cut(new Vector3(0.5f, 3.0f, -12.5f), new Vector3(0.5f, 1.6f, -0.5f), 55);
                onion.look = new Vector2(0, 0.6f);
                len0 = Speak(wO, "e3_18", "Загадал! Фу-у-у-у!");
                yield return Wait(len0 * 0.35f);
                onion.look = Vector2.zero; onion.mood = 5;
                yield return Act(0.5f, t => onion.puff = 1 + 0.1f * t / 0.5f);
                onion.puff = 1; onion.mood = 6;
                AudioFx.Play("swing"); AudioFx.Play("hum", 0.6f);
                shake = 1f;
                for (int k = 0; k < 10; k++) Spray(onion.MouthAt, Glass("FFFFFF", 0.4f), 2, 0.6f, 0.4f, 1.4f, 0, Vector3.left * 9 + URandom.insideUnitSphere, 1f);
                foreach (var f in flames) { f.SetActive(false); Spray(W.InverseTransformPoint(f.transform.position), Glass("DDDDDD", 0.5f), 1, 0.2f, 0.12f, 1f, -0.5f, Vector3.left * 2, 1); }
                for (int i = 0; i < 5; i++)
                {
                    var a = five[i];
                    a.mood = 5;
                    if (a == bub) continue;
                    Bg(Fly(a, a.pos + V(-3.2f - i * 0.3f, 0), 1.0f, 0.7f, -360, "land" + (i + 2)));
                }
                yield return Fly(bub, bub.pos + V(-2.5f, 0), 0.6f, 0.4f, -180, null);
                yield return RollTo(bub, V(-9.5f, -1.2f), 1.1f, 0.2f);
                Hush();
                onion.mood = 1;
                bub.Face(Cam0); bub.mood = 3;
                CloseUp(bub, 5.4f, 0.3f);
                yield return Say(wB, "e3_19", "Вот это ветер! Сильнее, чем у Чихуни!");
                CloseUp(chi, 5f, 0.3f);
                chi.Face(Cam0); chi.mood = 0;
                yield return SayDo(wC, "e3_20", "Эй! Чихать — это моя работа!", t => chi.tilt = Mathf.Sin(t * 8) * 10);
                chi.tilt = 0;

                // ---------- 8. friends, cake, the end ----------
                Scene = 8;
                for (int i = 0; i < 5; i++) { Put(five[i], spot[i], onion.pos); five[i].mood = 1; }
                onion.mood = 1;
                CloseUp(onion, 3.4f, 0, 44);
                yield return Say(wO, "e3_21", "А загадал я вот что: чтобы у меня всегда были такие друзья!");
                Cut(new Vector3(0.5f, 3.0f, -12.5f), new Vector3(0.5f, 1.6f, -0.5f), 55);
                yield return Cheer(0.9f, "win3");
                CloseUp(puh, 5f, 0.4f);
                Bg(Jump(puh, 0.6f, 0.45f));
                yield return Say(wP, "e3_22", "Ура! А теперь — торт!");
                bub.Face(CK); bub.mood = 6;
                Cut(CK + new Vector3(-1.8f, 1.6f, -4.6f), CK + new Vector3(-0.8f, 0.7f, 0), 44);
                yield return Hop(bub, CK + V(-1.2f, -0.5f), 0.5f, 0.3f, 2);
                yield return SayDo(wB, "e3_23", "Наконец-то! Чур, я первый!", t => bub.lean = 14 * Mathf.Abs(Mathf.Sin(t * 7)));
                bub.lean = 0;
                Cut(new Vector3(0.5f, 3.4f, -13f), new Vector3(0.5f, 2f, -0.5f), 55);
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                yield return Say(wN, "e3_24", "И все ели торт и веселились до самого вечера.");
                titleKick = 1;
                Confetti(CK + Vector3.up * 2.5f, 60, 4f, Vector3.up * 4);
                narr0 = Bg(Say(wAll, "e3_25", "С днём рождения, Лук!"));
                yield return Cheer(1.4f, "win2");
                yield return narr0;
                yield return EndCard(new Vector3(0.5f, 1.6f, -1.2f), 13);
            }
            float len0;
            Coroutine narr0;

            // =====================================================================================
            // ================= Серия 4: «Профессор и ракета» =======================================
            // =====================================================================================
            IEnumerator E4()
            {
                var R0 = V(2.5f, -5.5f);
                var LND = V(59.5f, -3.5f);
                Transform hatch = null, nozzle = null;
                var rocket = Prop("Rocket", null, R0, t =>
                {
                    var metal = Look.Metal("E8ECF2");
                    Cy(t, new Vector3(0, 0.55f, 0), new Vector3(2.0f, 0.35f, 2.0f), M("3D8BFF", 0.55f));        // the bucket
                    Ring(t, new Vector3(0, 0.88f, 0), 1.0f, 0.06f, M("2E68CC", 0.5f));
                    Cy(t, new Vector3(0, 2.45f, 0), new Vector3(2.2f, 1.55f, 2.2f), metal);                      // the pot
                    Ring(t, new Vector3(0, 3.95f, 0), 1.12f, 0.1f, Look.Metal("C8CCD4"));
                    Ring(t, new Vector3(0, 2.0f, 0), 1.12f, 0.1f, M("FF5252", 0.5f));
                    for (int sd = -1; sd <= 1; sd += 2) Ring(t, new Vector3(sd * 1.15f, 3.4f, 0), 0.22f, 0.05f, Look.Metal("A0A4AC"), new Vector3(0, 0, 90));   // pot handles
                    Spike(t, new Vector3(0, 4.0f, 0), new Vector3(0, 6.0f, 0), 2.15f, M("FF8A1E", 0.4f));         // the carrot nose
                    for (int k = 0; k < 4; k++) Ring(t, new Vector3(0, 4.35f + k * 0.38f, 0), 0.9f - k * 0.21f, 0.03f, M("E07010", 0.4f));
                    for (int k = -1; k <= 1; k++) Ca(t, new Vector3(k * 0.12f, 6.3f, 0), new Vector3(0.14f, 0.45f, 0.14f), M("4CB848", 0.5f), new Vector3(0, 0, -k * 22));
                    for (int k = 0; k < 3; k++)
                    {
                        float a = (k * 120 + 60) * Mathf.Deg2Rad;
                        var d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                        var fin = B(t, d * 1.25f + Vector3.up * 0.9f, new Vector3(0.12f, 1.4f, 0.8f), M("FF5252", 0.5f));
                        fin.transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 90, 0);
                    }
                    nozzle = Node("Nozzle", t, new Vector3(0, 0.05f, 0));
                    Cone(t, new Vector3(0, 0.35f, 0), new Vector3(1.0f, 0.5f, 1.0f), Look.Metal("6A6E76"), new Vector3(180, 0, 0));
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var w = S(t, new Vector3(k * 0.55f, 3.1f, -0.92f), new Vector3(0.5f, 0.5f, 0.3f), Glass("CFEFFF", 0.5f));
                        Ring(t, new Vector3(k * 0.55f, 3.1f, -0.96f), 0.26f, 0.05f, Look.Metal("FFC83A"), new Vector3(90, 0, 0));
                        NoShadow(w);
                    }
                    hatch = B(t, new Vector3(0, 1.75f, -1.08f), new Vector3(0.8f, 0.9f, 0.1f), Look.Metal("8A90A0")).transform;
                    S(hatch, new Vector3(0.3f, 0, -0.6f), new Vector3(0.12f, 0.1f, 1f), M("FFC83A", 0.8f, 0.7f));
                });
                Vector3 HatchAt() => W.InverseTransformPoint(rocket.TransformPoint(new Vector3(0, 1.75f, -1.3f))) + Vector3.down * 0.6f;

                // ---------- 0. title ----------
                Scene = 0;
                Put(prof, R0 + V(-1.8f, -1.6f), R0);
                Away(luk); Away(puh); Away(bub); Away(chi);
                Cut(new Vector3(2.5f, 14, -30), new Vector3(2.5f, 2, -5));
                Glide(new Vector3(2.5f, 3.2f, -16.5f), new Vector3(2.5f, 2.6f, -5.5f), 0.5f);
                Title("ЛУКАШАРИКИ", "серия «Профессор и ракета»");
                AudioFx.Play("portal");
                yield return Say(wN, "e4_01", "Лукашарики! Серия четвёртая: «Профессор и ракета»!");
                Title(null);
                var knock = Bg(Act(99f, t =>
                {
                    float a = t * 1.3f;
                    prof.pos = R0 + new Vector3(Mathf.Sin(a) * 1.9f, 0, -Mathf.Cos(a) * 1.9f);
                    prof.Face(R0);
                    prof.hop = Mathf.Abs(Mathf.Sin(t * 8)) * 0.25f;
                    if (URandom.value < 0.08f) { AudioFx.Play(URandom.value < 0.5f ? "crate" : "click", 0.5f); Spray(prof.Center + prof.Fwd * 0.6f, G("FFE36B", 2f), 3, 1.5f, 0.06f, 0.4f, 6); }
                }));
                yield return Say(wN, "e4_02", "Профессор всю ночь что-то мастерил. Стук-стук, бряк-бряк!");
                StopCoroutine(knock);
                prof.hop = 0;

                // ---------- 1. the rocket ----------
                Scene = 1;
                Row(R0 + V(0, -4.0f));
                Cut(R0 + new Vector3(0, 2.6f, -13.5f), R0 + new Vector3(0, 2.3f, -2f), 52);
                prof.mood = 1;
                Bg(Jump(prof, 0.9f, 0.55f));
                AudioFx.Play("win1", 0.6f);
                yield return Say(wPr, "e4_03", "Друзья! Встречайте: ракета «Лукашарик-один»! Летим в космос!");
                foreach (var t in five) if (t != prof) { t.mood = 5; t.look = new Vector2(0, 0.7f); }
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e4_04", "Ух ты! Настоящая ракета?");
                foreach (var t in five) t.look = Vector2.zero;
                CloseUp(prof, 5f, -0.4f);
                yield return SayDo(wPr, "e4_05", "Почти настоящая. Из кастрюли, ведра и морковки.", t => prof.tilt = Mathf.Sin(t * 2) * 8);
                prof.tilt = 0;
                CloseUp(bub, 5.4f, 0.3f); bub.mood = 0;
                yield return Say(wB, "e4_06", "А в космосе есть еда? Без бутербродов я не полечу.");
                CloseUp(puh, 5f, 0.4f); puh.mood = 1;
                Bg(Act(2.5f, t => puh.tilt = Mathf.Sin(t * 6) * 10));
                yield return Say(wP, "e4_07", "А я возьму бантик. В космосе тоже надо быть красивой!");
                puh.tilt = 0;

                // ---------- 2. boarding and the countdown ----------
                Scene = 2;
                Cut(R0 + new Vector3(2.5f, 2.8f, -12.5f), R0 + new Vector3(0, 2f, -1.5f), 52);
                foreach (var t in five) t.mood = 1;
                for (int i = 0; i < 5; i++)
                {
                    var a = five[i];
                    a.Face(HatchAt());
                    Bg(Fly(a, HatchAt(), 1.4f, 0.6f, 0, "land" + i));
                    yield return Wait(0.35f);
                }
                yield return Wait(0.4f);
                foreach (var t in five) Away(t);
                AudioFx.Play("crate");
                Cut(R0 + new Vector3(0, 3.0f, -11f), R0 + new Vector3(0, 3f, 0), 48);
                yield return SayDo(wAll, "e4_08", "Три... два... один... Пуск!", t => rocket.localScale = new Vector3(1, 1 + Mathf.Sin(t * 30) * 0.01f, 1));
                AudioFx.Play("error");
                Spray(R0 + Vector3.up * 0.2f, Glass("BBBBBB", 0.5f), 4, 0.3f, 0.3f, 1.2f, -0.2f, Vector3.zero, 1);
                yield return Wait(1.2f);
                yield return SayDo(wPr, "e4_09", "Хм. Не летит. Я забыл самое главное — топливо!", t => rocket.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 12) * 1.5f));
                yield return Say(wB, "e4_10", "А может, суп? Суп у нас всегда есть.");
                yield return SayDo(wC, "e4_11", "А можно я? У меня в носу такая сила... а... а...", t => rocket.localScale = new Vector3(1 - 0.03f * Mathf.Clamp01(t / 2), 1 + 0.05f * Mathf.Clamp01(t / 2), 1 - 0.03f * Mathf.Clamp01(t / 2)));
                yield return Say(wFour, "e4_12", "Нет-нет-нет, Чихуня!", 0.05f);

                // ---------- 3. lift-off ----------
                Scene = 3;
                len0 = Speak(wC, "e4_13", "АПЧХИ!");
                AudioFx.Play("swing"); AudioFx.Play("zap");
                rocket.localScale = Vector3.one;
                rocket.localRotation = Quaternion.identity;
                shake = 1.6f;
                for (int k = 0; k < 30; k++) Spray(R0 + Vector3.up * 0.3f, Glass("FFFFFF", 0.55f), 1, 4f, 0.7f, 1.8f, -0.3f, new Vector3(0, -0.5f, 0), 1.4f);
                yield return Wait(0.3f);
                Hush();
                Track(() => rocket.localPosition + Vector3.up * 3f, new Vector3(-7, 2.5f, -15), 2.5f);
                ApplyCamNow();
                narr0 = Bg(Say(wN, "e4_14", "И ракета полетела! Высоко-высоко... ну, не очень высоко."));
                var smokeMat = Glass("F4F4F4", 0.5f);
                void Trail()
                {
                    Spray(W.InverseTransformPoint(nozzle.position), smokeMat, 1, 0.3f, 0.45f, 1.6f, -0.2f, Vector3.zero, 1.2f);
                    Spray(W.InverseTransformPoint(nozzle.position), G("FFB13B", 2f), 1, 0.6f, 0.2f, 0.3f, 0);
                }
                yield return Act(1.2f, t => { float u = t / 1.2f; rocket.localPosition = R0 + Vector3.up * 6 * u * u; Trail(); });
                var a0 = R0 + Vector3.up * 6; var a1 = LND + Vector3.up * 6;
                const float H = 16, FD = 4.2f;
                yield return Act(FD, t =>
                {
                    float u = t / FD;
                    rocket.localPosition = Vector3.Lerp(a0, a1, u) + Vector3.up * (4 * H * u * (1 - u));
                    var vel = (a1 - a0) + Vector3.up * (4 * H * (1 - 2 * u));
                    rocket.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, vel.normalized), Mathf.Sin(u * Mathf.PI));
                    Trail();
                });
                rocket.localRotation = Quaternion.identity;
                Cut(LND + new Vector3(-3, 3.5f, -15), LND + new Vector3(0, 3, 0), 52);
                yield return Act(1.0f, t => { float u = t / 1.0f; rocket.localPosition = LND + Vector3.up * 6 * (1 - u) * (1 - u); if (URandom.value < 0.5f) Trail(); });
                rocket.localPosition = LND;
                AudioFx.Play("bonk"); shake = 1.1f;
                Spray(LND + Vector3.up * 0.3f, Glass("FFFFFF", 0.5f), 18, 3f, 0.6f, 1.4f, -0.2f, Vector3.zero, 1.2f);
                yield return Act(0.6f, t => { float k = Mathf.Exp(-t * 6) * Mathf.Cos(t * 30); rocket.localScale = new Vector3(1 + 0.12f * k, 1 - 0.15f * k, 1 + 0.12f * k); });
                rocket.localScale = Vector3.one;
                yield return narr0;

                // ---------- 4. another planet? ----------
                Scene = 4;
                carSpeed = 25;
                AudioFx.Play("open");
                var hTo = HatchAt() + new Vector3(1.6f, -0.9f, -2.2f);
                hatch.SetParent(W, true);
                Bg(FlyObj(hatch, hTo, 1.2f, 0.6f, 300));
                Vector3[] outSpot = { LND + V(-2.8f, -3.0f), LND + V(-1.4f, -3.6f), LND + V(0, -3.2f), LND + V(1.4f, -3.6f), LND + V(2.8f, -3.0f) };
                for (int i = 0; i < 5; i++)
                {
                    Put(five[i], HatchAt());
                    five[i].mood = 3;
                    Bg(Fly(five[i], outSpot[i], 1.4f, 0.65f, 360, "land" + (i + 2)));
                    yield return Wait(0.3f);
                }
                yield return Wait(0.6f);
                foreach (var t in five) { t.spin = 0; t.Face(t.pos + Vector3.back * 40); }
                Cut(LND + new Vector3(0, 2.0f, -12f), LND + new Vector3(0, 1.2f, -3f), 48);
                yield return Wait(0.8f);
                luk.mood = 0;
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e4_15", "Мы... уже в космосе?");
                prof.mood = 5; prof.Face(CAR);
                Cut(prof.Center + new Vector3(1.4f, 0.6f, -2.2f), CAR + Vector3.up * 3, 50);
                yield return Say(wPr, "e4_16", "Похоже, это другая планета! Смотрите, крутящаяся башня!");

                // ---------- 5. «aliens» ----------
                Scene = 5;
                for (int k = 0; k < 3; k++)
                {
                    var bd = birds[k];
                    bd.Reset();
                    bd.pos = LND + new Vector3(-6 + k, 7 + k, -10);
                    bd.flap = 1;
                    Bg(Fly(bd, LND + V(-1.1f + k * 1.1f, -6.1f - (k % 2) * 0.3f), 1.0f, 1.3f, 0, null));
                }
                foreach (var t in five) { t.mood = 5; t.Face(LND + V(0, -6.2f)); }
                Cut(LND + new Vector3(7.5f, 2.0f, -6.5f), LND + new Vector3(0, 0.8f, -4.8f), 50);
                yield return Wait(1.4f);
                foreach (var bd in birds) { bd.flap = 0; bd.Face(LND + V(0, -3.3f)); bd.Land(0.3f); }
                puh.mood = 1;
                CloseUp(puh, 5f, 0.4f);
                Bg(Jump(puh, 0.5f, 0.4f));
                yield return Say(wP, "e4_17", "Ой, инопланетяне! Здравствуйте! Мы прилетели с миром!");
                Cut(birds[1].Center + new Vector3(0.4f, 0.4f, 2.4f), birds[1].Center, 42);
                yield return SayDo(wBird, "e4_18", "Чик-чирик! Какие инопланетяне? Это наша карусель!", t => { foreach (var bd in birds) bd.hop = Mathf.Abs(Mathf.Sin(t * 9 + bd.r * 30)) * 0.15f; });
                foreach (var bd in birds) bd.hop = 0;
                bub.mood = 0;
                CloseUp(bub, 5.4f, 0.3f);
                yield return Say(wB, "e4_19", "Инопланетяне говорят «чик-чирик». Запишите, Профессор!");
                prof.Face(Cam0 + prof.pos); prof.mood = 3;
                CloseUp(prof, 5f, -0.4f);
                yield return SayDo(wPr, "e4_20", "Кхм... По моим расчётам... мы прилетели за речку.", t => prof.tilt = Mathf.Sin(t * 3) * 6);
                prof.tilt = 0;
                Cut(LND + new Vector3(0, 2.2f, -13f), LND + new Vector3(0, 1.2f, -4.5f), 50);
                foreach (var t in five) t.Face(t.pos + Vector3.back * 40);
                narr0 = Bg(Say(wAll, "e4_21", "Ха-ха-ха! За речку!"));
                yield return Cheer(1.6f, "win1");
                yield return narr0;

                // ---------- 6. the end ----------
                Scene = 6;
                luk.mood = 1;
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e4_22", "Ну и пусть! Зато это был самый настоящий полёт!");
                prof.mood = 1;
                CloseUp(prof, 5f, -0.4f);
                Bg(Jump(prof, 0.6f, 0.45f));
                yield return Say(wPr, "e4_23", "Завтра построим ракету побольше. И возьмём двух Чихунь!");
                Cut(LND + new Vector3(0, 2.6f, -14f), LND + new Vector3(0, 2.0f, -2f), 52);
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                chi.Face(rocket.localPosition);
                len0 = Speak(wC, "e4_24", "Я всегда готов! А... а... АПЧХИ!");
                yield return Wait(Mathf.Max(0.2f, len0 - 0.8f));
                yield return Sneeze(chi, 0.3f, 1.4f);
                titleKick = 1;
                AudioFx.Play("boing");
                yield return Act(0.7f, t => { float u = t / 0.7f; rocket.localPosition = LND + Vector3.up * 4 * 1.2f * u * (1 - u); });
                rocket.localPosition = LND;
                shake = 0.6f;
                foreach (var t in five) { t.Land(0.4f); t.mood = 1; }
                Hush();
                yield return Say(wN, "e4_25", "Вот так Лукашарики слетали в космос... за речку! Конец!");
                yield return EndCard(LND + new Vector3(0, 1.8f, -2.5f), 13);
            }

            // =====================================================================================
            // ================= Серия 5: «Зимние Лукашарики» ========================================
            // =====================================================================================
            IEnumerator E5()
            {
                // winter: snow over the meadow and the field, ice on the river, a white fog, falling snow
                var snowM = M("F4F8FF", 0.28f);
                Prop("Winter", null, Vector3.zero, t =>
                {
                    B(t, new Vector3(-83.3f, 0.03f, 0), new Vector3(233.4f, 0.1f, 400), snowM);
                    B(t, new Vector3(119.3f, 0.03f, 0), new Vector3(161.4f, 0.1f, 400), snowM);
                    B(t, new Vector3(8, 0.11f, 6), new Vector3(11.2f, 0.18f, 7.3f), snowM);
                    var ice = B(t, new Vector3(36, 0.05f, 0), new Vector3(5.4f, 0.04f, 400), Glass("E3F6FF", 0.82f));
                    NoShadow(ice);
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                    // snow on the house roof and the haystack
                    S(t, new Vector3(-8, 3.75f, 7), new Vector3(3.9f, 0.5f, 3.4f), snowM, new Vector3(0, 15, 0));
                    S(t, HAY + Vector3.up * 2.45f, new Vector3(3.6f, 1.2f, 3.6f), snowM);
                });
                foreach (var o in fieldOnions) o.gameObject.SetActive(false);
                RenderSettings.fogColor = Hex("DDE8F2");
                RenderSettings.ambientEquatorColor = Hex("D6DEE8");
                RenderSettings.ambientGroundColor = Hex("C9D3DE");
                Bg(Snowfall());
                var SM = V(-1.5f, -5.6f);       // the snowman

                // ---------- 0. title ----------
                Scene = 0;
                Row(Vector3.zero);
                foreach (var t in five) t.bob = 0.3f;
                Cut(new Vector3(0, 16, -34), new Vector3(2, 1, 6));
                Glide(new Vector3(0, 2.2f, -9.5f), new Vector3(0, 1.1f, 2), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Зимние Лукашарики»");
                AudioFx.Play("portal");
                yield return Say(wN, "e5_01", "Лукашарики! Серия пятая: «Зимние Лукашарики»!");
                Title(null);
                yield return Say(wN, "e5_02", "Ночью в долину пришла зима. Всё стало белым-белым.");
                foreach (var t in five) t.bob = 0;

                // ---------- 1. snow! a giant snowball ----------
                Scene = 1;
                luk.mood = 1;
                CloseUp(luk, 5f, 0.4f);
                Bg(Jump(luk, 0.7f, 0.5f));
                yield return Say(wL, "e5_03", "Ура! Снег! Кто со мной лепить снеговика?");
                CloseUp(puh, 5f, -0.4f); puh.mood = 1;
                Bg(Jump(puh, 0.6f, 0.45f));
                yield return Say(wP, "e5_04", "Я! Я! Только чтобы снеговик был с бантиком!");
                puh.mood = 0;
                var ballM = M("F7FAFF", 0.3f);
                var ball = Prop("Snowball", null, V(-4.5f, -4.2f), t => S(t, Vector3.zero, 1f, ballM));
                float br = 0.15f;
                ball.localScale = Vector3.one * 2 * br;
                ball.localPosition = V(-4.5f, -4.2f) + Vector3.up * br;
                Put(bub, V(-3.9f, -3.3f), V(-4.5f, -4.2f));
                var bdir = (V(-12, -16) - V(-4.5f, -4.2f)).normalized;
                var bax = Vector3.Cross(Vector3.up, bdir);
                Cut(new Vector3(-1.2f, 1.6f, -8.5f), new Vector3(-5.5f, 0.6f, -5.5f), 46);
                bub.mood = 1;
                float rolled = 0;
                var bStart = V(-4.5f, -4.2f);
                void PlaceBall(float d)
                {
                    rolled = d;
                    ball.localScale = Vector3.one * 2 * br;
                    ball.localPosition = bStart + bdir * d + Vector3.up * br;
                    ball.localRotation = Quaternion.AngleAxis(d / Mathf.Max(0.1f, br) * Mathf.Rad2Deg, bax);
                }
                yield return SayDo(wB, "e5_05", "А я скатаю самый большой ком! Смотрите!", t =>
                {
                    br = Mathf.Min(0.95f, 0.15f + t * 0.22f);
                    PlaceBall(t * 0.5f);
                    bub.pos = ball.localPosition - bdir * (br + bub.r * 0.9f); bub.pos.y = 0;
                    bub.Face(ball.localPosition);
                    bub.lean = 15; bub.hop = Mathf.Abs(Mathf.Sin(t * 8)) * 0.15f;
                });
                // the ball runs away, with Бублик on top
                bub.lean = 0; bub.mood = 5;
                yield return Fly(bub, ball.localPosition + Vector3.up * br, 0.6f, 0.4f, 0, "boing");
                Track(() => ball.localPosition + Vector3.up * 0.6f, new Vector3(4.5f, 2.2f, -4.5f), 3f);
                ApplyCamNow();
                var chasers = new[] { luk, puh, prof, chi };
                foreach (var c in chasers) { c.mood = 5; }
                float d0 = rolled, dEnd = (V(-12, -16) - bStart).magnitude - 1.5f - br;
                len0 = Speak(wB, "e5_06", "Ой-ой-ой! Ком меня катит! Помогите!");
                float RD = Mathf.Max(3.2f, len0 + 0.6f);
                yield return Act(RD, t =>
                {
                    float u = t / RD;
                    PlaceBall(Mathf.Lerp(d0, dEnd, u * u * (3 - 2 * u)));
                    bub.pos = ball.localPosition + Vector3.up * br;
                    bub.Face(bub.pos - bdir); bub.hop = Mathf.Abs(Mathf.Sin(t * 14)) * 0.25f;
                    for (int i = 0; i < 4; i++)
                    {
                        var c = chasers[i];
                        var p = bStart + bdir * Mathf.Max(0, rolled - 2.2f - i * 0.9f) + bax * ((i % 2 == 0 ? 1 : -1) * (0.9f + i * 0.2f)) + Vector3.back * 0.6f;
                        c.Face(p + bdir); c.pos = p;
                        c.hop = Mathf.Abs(Mathf.Sin(t * 7 + i)) * 0.45f;
                    }
                    if (URandom.value < 0.3f) Spray(ball.localPosition - bdir * br + Vector3.down * br * 0.8f, snowM, 1, 0.8f, 0.12f, 0.6f, 8, Vector3.up * 1.5f);
                });
                Hush();
                foreach (var c in chasers) c.hop = 0;
                // bump into the tree
                AudioFx.Play("bonk"); shake = 0.8f;
                var bp = ball.localPosition;
                ball.gameObject.SetActive(false);
                Spray(bp, snowM, 30, 3.5f, 0.3f, 1.2f, 9, Vector3.up * 3);
                for (int k = 0; k < 20; k++) Spray(V(-12, -16) + new Vector3(URandom.Range(-1.6f, 1.6f), 4.5f, URandom.Range(-1.6f, 1.6f)), snowM, 1, 0.4f, 0.22f, 1.4f, 7);
                bub.hop = 0;
                yield return Fly(bub, new Vector3(bp.x, 0, bp.z) - bdir * 0.6f, 1.2f, 0.6f, 360, "land2");
                bub.spin = 0; bub.mood = 3;
                var cap = Prop("SnowCap", bub.root, new Vector3(0, 2 * bub.r + 0.05f, 0), t => { S(t, Vector3.zero, new Vector3(0.9f, 0.42f, 0.9f), snowM); S(t, new Vector3(0.1f, 0.18f, 0), 0.34f, snowM); });
                bub.Face(bub.pos + Vector3.back * 40);
                CloseUp(bub, 5.6f, 0.4f);
                yield return Wait(0.5f);
                bub.mood = 1;
                yield return Say(wB, "e5_07", "Я теперь не Бублик. Я — снежный бублик!");

                // ---------- 2. the snowman ----------
                Scene = 2;
                cap.gameObject.SetActive(false);
                Transform smB = null, smM = null, smH = null, carrot = null;
                var snowman = Prop("Snowman", null, SM, t =>
                {
                    smB = S(t, new Vector3(0, 0.75f, 0), 1.6f, ballM).transform;
                    smM = S(t, new Vector3(0, 1.95f, 0), 1.1f, ballM).transform;
                    for (int k = 0; k < 3; k++) S(smM, new Vector3(0, 0.25f - k * 0.25f, -0.47f), 0.1f, M("222222", 0.4f));
                    for (int sd = -1; sd <= 1; sd += 2) Rod(smM, new Vector3(sd * 0.42f, 0.1f, 0), new Vector3(sd * 1.0f, 0.45f, 0), 0.05f, Look.Bark());
                    smH = Node("Head", t, new Vector3(0, 2.8f, 0));
                    S(smH, Vector3.zero, 0.78f, ballM);
                    for (int sd = -1; sd <= 1; sd += 2) S(smH, new Vector3(sd * 0.14f, 0.1f, -0.33f), 0.09f, M("222222", 0.4f));
                    Cy(smH, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.18f, 0.5f), M("3D8BFF", 0.5f));
                    carrot = Spike(W, Vector3.zero, new Vector3(0, 0, 0.45f), 0.12f, M("FF8A1E", 0.4f)).transform;
                });
                smB.gameObject.SetActive(false); smM.gameObject.SetActive(false); smH.gameObject.SetActive(false); carrot.gameObject.SetActive(false);
                Put(luk, SM + V(-2.4f, -0.8f), SM); Put(puh, SM + V(-1.4f, -1.9f), SM); Put(bub, SM + V(2.4f, -0.8f), SM);
                Put(prof, SM + V(1.2f, -1.7f), SM); Put(chi, SM + V(0.1f, -2.1f), SM);
                Cut(SM + new Vector3(0.4f, 2.4f, -8.5f), SM + new Vector3(0, 1.5f, 0), 48);
                prof.mood = 0;
                Bg(Jump(prof, 0.6f, 0.45f));
                yield return Say(wPr, "e5_08", "Коллеги, правильный снеговик — это три шара. Большой, средний и маленький!");
                foreach (var (part, who) in new[] { (smB, luk), (smM, puh), (smH, bub) })
                {
                    var p0 = part.localPosition;
                    part.gameObject.SetActive(true);
                    part.localPosition = W.InverseTransformPoint(snowman.TransformPoint(Vector3.zero)) - SM + (who.pos - SM) + Vector3.up * 0.6f;
                    who.mood = 1;
                    AudioFx.Play("flip", 0.5f);
                    yield return FlyObj(part, p0, 1.2f, 0.55f, 0);
                    AudioFx.Play("land" + URandom.Range(0, 7), 0.6f);
                    Spray(W.InverseTransformPoint(part.position), snowM, 6, 1.2f, 0.1f, 0.6f, 6, Vector3.up);
                }
                chi.mood = 1;
                CloseUp(chi, 5f, 0.3f);
                carrot.gameObject.SetActive(true);
                carrot.SetParent(chi.root, false);
                carrot.localPosition = new Vector3(0.45f, 0.45f, 0.25f); carrot.localRotation = Quaternion.identity;
                Bg(Jump(chi, 0.5f, 0.4f));
                yield return Say(wC, "e5_09", "А нос? Нужен нос! Вот морковка!");
                carrot.SetParent(W, true);
                var noseAt = SM + new Vector3(0, 2.8f, -0.36f);
                Cut(SM + new Vector3(1.6f, 2.4f, -4.5f), SM + new Vector3(0, 2.3f, 0), 44);
                yield return FlyObj(carrot, noseAt, 1.0f, 0.5f, 0);
                carrot.SetParent(smH, true);
                carrot.localRotation = Quaternion.LookRotation(Vector3.forward);
                carrot.localRotation = Quaternion.Euler(0, 180, 0);
                AudioFx.Play("click");
                chi.mood = 5; chi.Face(SM);
                len0 = Speak(wC, "e5_10", "Ой, холодно... нос замёрз... АПЧХИ!");
                yield return Wait(Mathf.Max(0.3f, len0 - 0.8f));
                yield return Sneeze(chi, 0.35f, 1.6f);
                shake = 0.5f;
                var headTo = prof.pos + Vector3.up * (2 * prof.r + 0.3f);
                smH.SetParent(W, true);
                yield return FlyObj(smH, headTo, 1.6f, 0.6f, 200);
                smH.localRotation = Quaternion.Euler(0, 180 + prof.yaw, 0);
                smH.SetParent(prof.roll, true);
                prof.Land(0.5f); prof.mood = 3;
                AudioFx.Play("bonk");
                Hush();
                Cut(prof.Center + prof.Fwd * 3.4f + Vector3.up * 0.9f, prof.Center + Vector3.up * 0.5f, 44);
                yield return SayDo(wPr, "e5_11", "Так. Я теперь тоже снеговик. Очень научный снеговик.", t => prof.tilt = Mathf.Sin(t * 2) * 5);
                prof.tilt = 0;
                Cut(SM + new Vector3(0.4f, 2.4f, -8.5f), SM + new Vector3(0, 1.5f, 0), 48);
                var laugh = Voice("Все", "FFC83A", 1.12f, luk, puh, bub, chi); laugh.chorus = true;
                foreach (var t in five) t.mood = 1;
                narr0 = Bg(Say(laugh, "e5_12", "Ха-ха-ха-ха!"));
                yield return Act(1.6f, t => { foreach (var a in five) if (a != prof) a.tilt = Mathf.Sin(t * 14 + a.r * 10) * 10; });
                yield return narr0;
                foreach (var t in five) t.tilt = 0;
                smH.SetParent(W, true);
                yield return FlyObj(smH, SM + Vector3.up * 2.8f, 1.0f, 0.5f, 0);
                smH.SetParent(snowman, true);
                smH.localRotation = Quaternion.identity;
                AudioFx.Play("coin", 0.6f);

                // ---------- 3. skating on the frozen river ----------
                Scene = 3;
                Cut(SM + new Vector3(0.4f, 2.4f, -8.5f), SM + new Vector3(0, 1.5f, 0), 48);
                luk.mood = 1; luk.Face(Cam0);
                CloseUp(luk, 5f, 0.4f);
                yield return Say(wL, "e5_13", "А речка замёрзла! Побежали кататься!");
                float[] lx = { 34.6f, 35.6f, 36.6f, 37.4f, 35.1f };
                for (int i = 0; i < 5; i++) { Put(five[i], new Vector3(lx[i], 0.07f, -16f - i * 0.8f), new Vector3(lx[i], 0, 10)); five[i].mood = 1; }
                var skate = Bg(Act(60f, t =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var a = five[i];
                        float z = -16 - i * 0.8f + t * 2.2f;
                        float x = lx[i] + Mathf.Sin(t * 1.3f + i * 1.7f) * 0.7f;
                        a.pos = new Vector3(x, 0.07f, z);
                        a.yaw = Mathf.Cos(t * 1.3f + i * 1.7f) * 25;
                        a.tilt = -Mathf.Cos(t * 1.3f + i * 1.7f) * 12;
                        if (a == puh) { a.yaw = t * 540; a.tilt = 0; }
                    }
                }));
                Track(() => (luk.pos + puh.pos + bub.pos + chi.pos) * 0.25f + Vector3.up * 0.6f, new Vector3(-6.5f, 2.2f, -4.5f), 3f);
                ApplyCamNow();
                yield return Wait(1.0f);
                yield return Say(wP, "e5_14", "Я как фигуристка! Смотрите, кручусь!");
                yield return Say(wPr, "e5_15", "Главное — не падать. Наука говорит... ой-ой-ой!", 0.05f);
                StopCoroutine(skate);
                foreach (var t in five) { t.tilt = 0; }
                puh.yaw = 0;
                prof.mood = 5;
                var pp0 = prof.pos;
                AudioFx.Play("glitchboing");
                Track(() => prof.Center, new Vector3(-4.5f, 1.6f, -3.5f), 4f);
                yield return Act(1.4f, t =>
                {
                    float u = t / 1.4f;
                    prof.pos = Vector3.Lerp(pp0, new Vector3(40.4f, 0.07f, pp0.z + 3.5f), u);
                    prof.spin = u * 900;
                });
                prof.spin = 0;
                AudioFx.Play("bonk"); shake = 0.4f;
                Spray(prof.Center, snowM, 26, 3f, 0.22f, 1f, 8, Vector3.up * 2.5f);
                prof.mood = 3; prof.Face(Cam0 + prof.pos);
                yield return Wait(0.9f);
                Cut(prof.Center + new Vector3(-1.6f, 0.5f, -3.2f), prof.Center, 44);
                yield return Wait(0.8f);

                // ---------- 4. a snowball fight ----------
                Scene = 4;
                var FT = V(-4f, -2.5f);
                Put(luk, FT + V(-2.4f, -0.3f), FT + V(2.4f, 0)); Put(puh, FT + V(-2.8f, 1.0f), FT + V(2.4f, 0));
                Put(bub, FT + V(2.4f, -0.2f), FT + V(-2.4f, 0)); Put(chi, FT + V(2.8f, 1.0f), FT + V(-2.4f, 0));
                Put(prof, FT + V(0, 2.4f), Cam0);
                foreach (var t in five) t.mood = 1;
                Cut(FT + new Vector3(0, 2.4f, -8.5f), FT + new Vector3(0, 0.8f, 0.4f), 50);
                bub.mood = 1;
                Bg(Jump(bub, 0.5f, 0.4f));
                yield return Say(wB, "e5_16", "А теперь — снежки! Кто не спрятался, я не виноват!");
                IEnumerator Throw(Toon from, Toon to, bool hit)
                {
                    var sb = Prop("Snowball", null, from.Center + Vector3.up * 0.4f, t => S(t, Vector3.zero, 0.26f, ballM));
                    AudioFx.Play("swing", 0.4f);
                    from.Land(-0.25f);
                    var aim = hit ? to.Center + Vector3.up * 0.2f : to.Center + Vector3.up * 1.4f + to.Right * 0.8f;
                    yield return FlyObj(sb, aim, 0.8f, 0.5f, 0);
                    Spray(sb.localPosition, snowM, 10, 1.5f, 0.1f, 0.6f, 6, Vector3.up);
                    Destroy(sb.gameObject);
                    if (hit) { to.Land(0.4f); to.mood = 5; AudioFx.Play("box", 0.6f); }
                }
                var pairs = new[] { (luk, bub, true), (bub, luk, false), (chi, puh, true), (puh, chi, true), (bub, luk, true), (luk, chi, false) };
                foreach (var (a, b, hit) in pairs)
                {
                    Bg(Throw(a, b, hit));
                    yield return Wait(0.35f);
                }
                yield return Wait(0.6f);
                foreach (var t in five) t.mood = 1;
                luk.mood = 1;
                CloseUp(luk, 5f, -0.4f);
                len0 = Speak(wL, "e5_17", "Попал! Ой... и в меня попали!");
                yield return Wait(len0 * 0.45f);
                yield return Throw(bub, luk, true);
                luk.mood = 3;
                yield return Wait(Mathf.Max(0.2f, len0 * 0.55f - 0.5f));
                Hush();
                luk.mood = 1;

                // ---------- 5. Лук with hot tea ----------
                Scene = 5;
                foreach (var t in five) Put(t, t.pos, FT + V(5, -1));
                Put(onion, V(10.5f, -1.2f), FT);
                Prop("Scarf", onion.roll, new Vector3(0, 0.91f * onion.r, 0), t =>
                {
                    Ring(t, Vector3.zero, 0.86f, 0.22f, M("E53935", 0.3f));
                    B(t, new Vector3(0.45f, -0.5f, 0.78f), new Vector3(0.32f, 0.9f, 0.1f), M("E53935", 0.3f), new Vector3(-10, 0, 8));
                });
                for (int i = 0; i < 5; i++) bowls[i].gameObject.SetActive(false);
                Cut(FT + new Vector3(3.5f, 2.6f, -10.5f), FT + new Vector3(4, 1.8f, 0), 52);
                onion.mood = 1;
                yield return RollTo(onion, FT + V(5.6f, -0.6f), 1.8f, 0.25f);
                onion.Face(FT);
                CloseUp(onion, 3.2f, 0, 44);
                yield return Say(wO, "e5_18", "Привет, друзья! Я принёс вам тёплый чай с мёдом!");
                Vector3[] seat = { FT + V(-1.8f, -1.6f), FT + V(-0.6f, -2.2f), FT + V(0.8f, -2.3f), FT + V(2.0f, -1.7f), FT + V(-2.8f, -0.8f) };
                for (int i = 0; i < 5; i++)
                {
                    Put(five[i], seat[i], seat[i] + Vector3.back * 40);
                    bowls[i].gameObject.SetActive(true);
                    bowls[i].localScale = Vector3.one * 0.8f;
                    bowls[i].localPosition = seat[i] + Vector3.back * 0.72f;
                    five[i].mood = 1;
                }
                var cups = Bg(Act(99f, t =>
                {
                    if (URandom.value < 0.25f)
                    {
                        var b = bowls[URandom.Range(0, 5)];
                        Spray(b.localPosition + Vector3.up * 0.25f, Glass("FFFFFF", 0.4f), 1, 0.05f, 0.14f, 1.4f, -0.2f, Vector3.up * 0.5f, 1.2f);
                    }
                }));
                Cut(FT + new Vector3(0.2f, 1.8f, -8.5f), FT + new Vector3(0.2f, 0.8f, -1.5f), 46);
                chi.mood = 1;
                yield return SayDo(wC, "e5_19", "Ура! Чай — лучшее средство от чиха!", t => chi.lean = 12 * Mathf.Abs(Mathf.Sin(t * 5)));
                chi.lean = 0;
                CloseUp(puh, 5f, 0.4f);
                yield return Say(wP, "e5_20", "Зима — это так весело, когда рядом друзья!");

                // ---------- 6. the end ----------
                Scene = 6;
                Cut(FT + new Vector3(1.2f, 3.0f, -11f), FT + new Vector3(1.2f, 1.8f, 0), 52);
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                yield return Say(wN, "e5_21", "И Лукашарики пили чай и смотрели, как падает снег. Конец!");
                titleKick = 1;
                chi.mood = 5;
                yield return SayDo(wC, "e5_22", "Ап... ап... Не чихнул! Ура! Чай помог!", t => chi.noseK = 1 + 0.3f * Mathf.Abs(Mathf.Sin(t * 4)));
                chi.noseK = 1;
                StopCoroutine(cups);
                yield return Cheer(1.0f, "win2");
                yield return EndCard(FT + new Vector3(1.2f, 1.6f, -1.4f), 12);
            }

            // =====================================================================================
            // ================= Серия 6: «Бублик-сыщик» =============================================
            // =====================================================================================
            IEnumerator E6()
            {
                var NEST = new Vector3(-2.8f, 1.98f, 8.88f);
                Transform BowCopy(Transform parent, Vector3 p, string a, string b, float s)
                {
                    return Prop("Bow", parent, p, t =>
                    {
                        t.localScale = Vector3.one * s;
                        for (int sd = -1; sd <= 1; sd += 2)
                            S(t, new Vector3(sd * 0.1f, 0, 0), new Vector3(0.17f, 0.13f, 0.08f), M(a, 0.5f), new Vector3(0, 0, sd * 22));
                        S(t, Vector3.zero, 0.07f, M(b, 0.5f));
                    });
                }
                puh.top.gameObject.SetActive(false);
                nest.gameObject.SetActive(true);
                nest.SetParent(W, false);
                nest.localPosition = NEST; nest.localRotation = Quaternion.identity;
                var stolen = BowCopy(W, NEST + new Vector3(0.05f, 0.22f, -0.05f), "FF2D7A", "D81B60", 1.6f);
                stolen.localEulerAngles = new Vector3(0, 0, -25);
                for (int k = 0; k < 3; k++) { birds[k].pos = BirdSpot(k); birds[k].yaw = 180 + (k - 1) * 20; }
                // the trail of pink threads
                var trail = new List<Vector3>();
                for (int k = 0; k < 8; k++)
                {
                    float u = k / 7f;
                    trail.Add(Vector3.Lerp(V(-1.0f, -0.6f), V(-2.4f, 7.5f), u) + V(Mathf.Sin(u * 7) * 0.5f, 0));
                }
                Prop("Threads", null, Vector3.zero, t =>
                {
                    foreach (var p in trail) Ca(t, p + Vector3.up * 0.04f, new Vector3(0.05f, 0.16f, 0.05f), M("FF2D7A", 0.4f), new Vector3(90, URandom.Range(0, 180f), 0));
                });
                Transform lens = Prop("Lens", bub.root, new Vector3(0.62f, 0.62f, 0.35f), t =>
                {
                    Ring(t, Vector3.zero, 0.24f, 0.04f, M("8A5A33", 0.5f), new Vector3(90, 0, 0));
                    var g = Cy(t, Vector3.zero, new Vector3(0.44f, 0.01f, 0.44f), Glass("CFEFFF", 0.35f), new Vector3(90, 0, 0));
                    NoShadow(g);
                    Rod(t, new Vector3(0, -0.24f, 0), new Vector3(0, -0.6f, 0), 0.06f, M("5A3A22", 0.4f));
                });
                lens.gameObject.SetActive(false);

                // ---------- 0. title ----------
                Scene = 0;
                Row(Vector3.zero);
                foreach (var t in five) t.bob = 0.3f;
                Cut(new Vector3(0, 16, -34), new Vector3(2, 1, 6));
                Glide(new Vector3(0, 2.2f, -9.5f), new Vector3(0, 1.1f, 2), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Бублик-сыщик»");
                AudioFx.Play("portal");
                yield return Say(wN, "e6_01", "Лукашарики! Серия шестая: «Бублик-сыщик»!");
                Title(null);
                foreach (var t in five) t.bob = 0;

                // ---------- 1. the bow is gone ----------
                Scene = 1;
                puh.mood = 2; puh.crying = true;
                CloseUp(puh, 5.2f, -0.3f);
                AudioFx.Play("error");
                yield return SayDo(wP, "e6_02", "Ой-ой-ой! Пропал мой бантик! Мой самый красивый бантик!", t => puh.tilt = Mathf.Sin(t * 6) * 10);
                puh.tilt = 0; puh.crying = false;
                CloseUp(bub, 5.6f, 0.4f);
                bub.mood = 1;
                lens.gameObject.SetActive(true);
                lens.localScale = Vector3.zero;
                AudioFx.Play("magic");
                Bg(Grow(lens, 1, 0.6f));
                Bg(Jump(bub, 0.8f, 0.5f));
                yield return Say(wB, "e6_03", "Без паники! За дело берётся сыщик Бублик!");
                CloseUp(luk, 5f, -0.4f);
                yield return Say(wL, "e6_04", "Бублик, а откуда у тебя лупа?");
                CloseUp(bub, 5.6f, 0.4f);
                yield return SayDo(wB, "e6_05", "Сыщик без лупы — как суп без ложки!", t => bub.tilt = Mathf.Sin(t * 4) * 6);
                bub.tilt = 0;

                // ---------- 2. a clue ----------
                Scene = 2;
                Cut(new Vector3(1.6f, 1.4f, -4.6f), trail[0] + Vector3.up * 0.3f, 44);
                yield return Hop(bub, trail[0] + V(0.4f, -0.9f), 0.8f, 0.3f, 3);
                bub.Face(trail[0]);
                Cut(trail[0] + new Vector3(0.9f, 0.5f, -1.4f), trail[0] + Vector3.up * 0.1f, 38);
                yield return SayDo(wB, "e6_06", "Так-так-так... Розовая ниточка! Улика номер один!", t => { bub.lean = 22 + Mathf.Sin(t * 3) * 4; bub.look = new Vector2(0, -0.8f); });
                bub.lean = 0; bub.look = Vector2.zero;

                // ---------- 3. suspects ----------
                Scene = 3;
                Put(chi, bub.pos + V(1.6f, -0.4f)); chi.Face(bub.pos); bub.Face(chi.pos);
                Put(prof, bub.pos + V(-1.6f, -0.5f), bub.pos);
                Put(luk, bub.pos + V(-0.6f, 1.6f), bub.pos); Put(puh, bub.pos + V(0.8f, 1.7f), bub.pos);
                var mid = (bub.pos + chi.pos) * 0.5f;
                Cut(mid + new Vector3(0.3f, 1.2f, -4.2f), mid + Vector3.up * 0.5f, 44);
                bub.lean = 15;
                yield return Say(wB, "e6_07", "Чихуня! Признавайся! Где ты был сегодня утром?");
                bub.lean = 0;
                chi.mood = 1;
                len0 = Speak(wC, "e6_08", "Я? Чихал! Как всегда! АПЧХИ!");
                yield return Wait(Mathf.Max(0.3f, len0 - 0.8f));
                yield return Sneeze(chi, 0.3f, 1.4f);
                bub.mood = 5;
                yield return Fly(bub, bub.pos - bub.Fwd * 1.6f, 0.6f, 0.5f, -360, "bonk");
                Hush();
                bub.Face(chi.pos); bub.mood = 1;
                CloseUp(bub, 5.6f, 0.4f);
                yield return Say(wB, "e6_09", "Ага! Чихнул — и бантик улетел! Подозрительно!");
                prof.Face(bub.pos);
                CloseUp(prof, 5f, -0.4f);
                yield return SayDo(wPr, "e6_10", "Позвольте! Чихуня чихает вперёд, а ниточки ведут назад. Научный факт!", t => prof.tilt = Mathf.Sin(t * 2) * 6);
                prof.tilt = 0;
                bub.Face(prof.pos);
                yield return Hop(bub, prof.pos + (bub.pos - prof.pos).normalized * 1.35f, 0.5f, 0.3f, 2);
                bub.Face(prof.pos);
                Cut(prof.Center + prof.Fwd * 2.6f + prof.Right * 0.9f + Vector3.up * 0.4f, (prof.Center + bub.Center) * 0.5f, 46);
                yield return SayDo(wB, "e6_11", "Хм... Тогда, может, это вы, Профессор? Для опытов?", t => bub.lean = 18 * Mathf.Abs(Mathf.Sin(t * 2)));
                bub.lean = 0;
                prof.mood = 5;
                CloseUp(prof, 5f, -0.4f);
                Bg(Jump(prof, 0.5f, 0.4f));
                yield return Say(wPr, "e6_12", "Я? Я ставлю опыты только на манной каше!");
                prof.mood = 0;

                // ---------- 4. the trail leads to the tree ----------
                Scene = 4;
                Cut(new Vector3(1.8f, 2.6f, -3.5f), new Vector3(-2.2f, 1.2f, 6), 50);
                var followers = new[] { luk, puh, prof, chi };
                yield return Hop(bub, trail[3] + V(0.3f, -0.4f), 1.2f, 0.35f, 4);
                for (int i = 0; i < 4; i++) Bg(Hop(followers[i], trail[2] + V(-1.4f + i * 0.9f, -1.2f - (i % 2) * 0.5f), 1.2f, 0.35f, 4));
                yield return Hop(bub, trail[7] + V(0.5f, -0.6f), 1.4f, 0.35f, 5);
                for (int i = 0; i < 4; i++) Bg(Hop(followers[i], trail[6] + V(-1.6f + i * 1.0f, -1.0f - (i % 2) * 0.5f), 1.2f, 0.35f, 4));
                bub.Face(NEST); bub.look = new Vector2(0, 0.9f);
                yield return Wait(1.0f);
                Cut(bub.pos + new Vector3(1.4f, 1.1f, -2.8f), (bub.Center + NEST) * 0.5f, 52);
                yield return Say(wB, "e6_13", "Ниточки ведут к дереву... А на дереве... птички!");
                bub.look = Vector2.zero;
                Cut(NEST + new Vector3(0.9f, 0.4f, -3.4f), BirdSpot(1) + Vector3.up * 0.1f, 40);
                yield return SayDo(wBird, "e6_14", "Чик-чирик... А мы ничего не брали...", t =>
                {
                    for (int k = 0; k < 3; k++) birds[k].look = new Vector2(Mathf.Sin(t * 4 + k) > 0 ? 1 : -1, 0);
                });
                foreach (var bd in birds) bd.look = Vector2.zero;

                // ---------- 5. found! ----------
                Scene = 5;
                foreach (var t in five) { t.Face(NEST); t.look = new Vector2(0, 0.9f); }
                luk.mood = 5;
                Cut(NEST + new Vector3(1.2f, -1.2f, -3.6f), NEST + Vector3.up * 0.1f, 36);
                yield return Wait(0.6f);
                Cut(luk.Center + luk.Fwd * 2.4f + Vector3.up * 0.2f + luk.Right * 0.4f, luk.Center + Vector3.up * 0.2f, 42);
                Bg(Jump(luk, 0.5f, 0.4f));
                yield return Say(wL, "e6_15", "Смотрите! В гнезде что-то розовое!");
                foreach (var t in five) t.look = Vector2.zero;
                Cut(NEST + new Vector3(0.9f, 0.4f, -3.4f), NEST + new Vector3(0.6f, 0.15f, 0), 40);
                foreach (var bd in birds) bd.mood = 2;
                yield return SayDo(wBird, "e6_16", "Простите! Бантик такой красивый... Мы хотели украсить гнёздышко.", t => { foreach (var bd in birds) bd.tilt = Mathf.Sin(t * 5 + bd.r * 20) * 10; });
                foreach (var bd in birds) bd.tilt = 0;

                // ---------- 6. kindness and a new ribbon ----------
                Scene = 6;
                puh.mood = 1;
                puh.Face(NEST);
                Cut(puh.Center + puh.Fwd * 2.6f + Vector3.up * 0.3f, puh.Center + Vector3.up * 0.3f, 42);
                yield return Say(wP, "e6_17", "Ой... ну, раз для гнёздышка... Я не сержусь!");
                prof.mood = 1;
                CloseUp(prof, 5f, -0.4f);
                AudioFx.Play("magic");
                var blue = BowCopy(prof.root, new Vector3(0.6f, 0.8f, 0.3f), "5FD3FF", "3D8BFF", 1.4f);
                Bg(Jump(prof, 0.6f, 0.45f));
                yield return Say(wPr, "e6_18", "Есть научное решение! Вот голубая ленточка. Сделаем птичкам свой бантик!");
                blue.SetParent(W, true);
                Cut(NEST + new Vector3(2.4f, -0.6f, -5.4f), NEST + new Vector3(0.2f, -0.6f, -1.0f), 50);
                AudioFx.Play("flip");
                Bg(FlyObj(blue, NEST + new Vector3(-0.1f, 0.22f, 0.05f), 1.4f, 0.8f, 360));
                yield return Wait(0.3f);
                stolen.SetParent(W, true);
                yield return FlyObj(stolen, puh.Head + Vector3.up * 0.1f, 1.2f, 0.8f, 360);
                stolen.gameObject.SetActive(false);
                puh.top.gameObject.SetActive(true);
                puh.Land(0.4f);
                AudioFx.Play("coin");
                Spray(puh.Head, G("FFB6D0", 2f), 14, 2f, 0.08f, 0.7f, 0, Vector3.up);
                foreach (var bd in birds) { bd.mood = 1; bd.flap = 1; }
                Cut(NEST + new Vector3(0.9f, 0.4f, -3.4f), NEST + new Vector3(0.6f, 0.15f, 0), 40);
                yield return SayDo(wBird, "e6_19", "Ура! Спасибо! Чик-чирик!", t => { foreach (var bd in birds) bd.hop = Mathf.Abs(Mathf.Sin(t * 8 + bd.r * 30)) * 0.25f; });
                foreach (var bd in birds) { bd.flap = 0; bd.hop = 0; }
                puh.Face(Cam0 + puh.pos);
                CloseUp(puh, 5f, 0.4f);
                Bg(Jump(puh, 0.7f, 0.5f));
                yield return Say(wP, "e6_20", "Мой бантик снова со мной! Бублик, ты самый лучший сыщик!");

                // ---------- 7. case closed ----------
                Scene = 7;
                foreach (var t in five) { t.Face(t.pos + Vector3.back * 40); t.mood = 1; }
                bub.Face(Cam0 + bub.pos);
                CloseUp(bub, 5.6f, 0.4f);
                yield return SayDo(wB, "e6_21", "Дело раскрыто! А за раскрытое дело сыщику полагается... пирожок!", t => bub.tilt = Mathf.Sin(t * 3) * 6);
                bub.tilt = 0;
                var c = (luk.pos + puh.pos + prof.pos + chi.pos + bub.pos) / 5;
                Cut(c + new Vector3(0, 2.2f, -8.5f), c + new Vector3(0, 1.1f, 0), 50);
                foreach (var t in five) if (t != bub) { t.Face(bub.pos); t.mood = 5; }
                narr0 = Bg(Say(wFour, "e6_22", "Бублик!"));
                yield return Wait(0.3f);
                bub.Land(0.55f); bub.mood = 3;
                AudioFx.Play("boing");
                yield return narr0;
                foreach (var t in five) { t.Face(t.pos + Vector3.back * 40); }
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                yield return Say(wN, "e6_23", "Так сыщик Бублик раскрыл своё первое дело. Конец!");
                titleKick = 1;
                yield return Cheer(1.0f, "win2");
                yield return EndCard(c + new Vector3(0, 1.4f, 0), 11);
            }

            // =====================================================================================
            // ================= Серия 7: «Большой концерт» ==========================================
            // =====================================================================================
            IEnumerator E7()
            {
                // the carousel is the stage: the poles stand aside (angle 36), the friends in an arc in front
                carAngle = 36; carSpeed = 0;
                Vector3 Spot(float deg) => CAR + new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad) * 2.3f, 0.5f, Mathf.Cos(deg * Mathf.Deg2Rad) * 2.3f);
                var front = CAR + new Vector3(0, 0, -30);
                // left → right from the audience: Пухля, Бублик, Лукаш, Профессор, Чихуня
                var sp = new Dictionary<Toon, Vector3> { { puh, Spot(240) }, { bub, Spot(210) }, { luk, Spot(180) }, { prof, Spot(150) }, { chi, Spot(120) } };
                var AUD = CAR + V(3.4f, -12.5f);
                Transform drum = null, xylo = null;
                var bars = new List<Transform>();
                Prop("Instruments", null, Vector3.zero, t =>
                {
                    var lp = sp[luk];
                    drum = Node("Drum", t, lp + new Vector3(0, 0, -0.9f));
                    Cy(drum, new Vector3(0, 0.32f, 0), new Vector3(0.9f, 0.3f, 0.9f), M("FF5252", 0.5f));
                    Cy(drum, new Vector3(0, 0.63f, 0), new Vector3(0.92f, 0.01f, 0.92f), M("FFF6E6", 0.3f));
                    Ring(drum, new Vector3(0, 0.6f, 0), 0.46f, 0.04f, M("FFC83A", 0.8f, 0.7f));
                    Ring(drum, new Vector3(0, 0.05f, 0), 0.46f, 0.04f, M("FFC83A", 0.8f, 0.7f));
                    var pp = sp[prof];
                    xylo = Node("Xylophone", t, pp + new Vector3(0.1f, 0, -0.95f));
                    xylo.localRotation = Quaternion.Euler(0, -30, 0);
                    for (int sd = -1; sd <= 1; sd += 2) Rod(xylo, new Vector3(sd * 0.25f, 0, 0), new Vector3(sd * 0.25f, 0.45f, 0), 0.06f, Look.Planks("A0642F"));
                    B(xylo, new Vector3(0, 0.46f, 0), new Vector3(0.66f, 0.04f, 0.9f), Look.Planks("8A5A33"));
                    for (int k = 0; k < 6; k++)
                        bars.Add(B(xylo, new Vector3(0, 0.52f, -0.36f + k * 0.145f), new Vector3(0.62f - k * 0.06f, 0.05f, 0.11f), M(Party[k], 0.6f)).transform);
                    var mp = sp[puh];
                    Rod(t, mp + new Vector3(0, 0, -0.75f), mp + new Vector3(0, 0.95f, -0.75f), 0.04f, Look.Metal("A0A4AC"));
                    S(t, mp + new Vector3(0, 1.02f, -0.75f), 0.18f, Look.Metal("6A6E76"));
                    // footlights along the front of the stage
                    for (int k = 0; k < 9; k++)
                    {
                        float a = (140 + k * 10) * Mathf.Deg2Rad;
                        NoShadow(S(t, CAR + new Vector3(Mathf.Sin(a) * 4.9f, 0.55f, Mathf.Cos(a) * 4.9f), 0.22f, G(Party[k % Party.Length], 2.2f)));
                    }
                });
                var trumpet = Prop("Trumpet", bub.root, new Vector3(0, 0.42f, 0.55f), t =>
                {
                    Rod(t, Vector3.zero, new Vector3(0, 0, 0.45f), 0.07f, Look.Metal("FFC83A"));
                    Spike(t, new Vector3(0, 0, 0.75f), new Vector3(0, 0, 0.35f), 0.36f, Look.Metal("FFC83A"));
                    Ring(t, new Vector3(0, 0, 0.75f), 0.18f, 0.025f, Look.Metal("FFE07A"), new Vector3(90, 0, 0));
                });
                var baton = Prop("Baton", prof.root, new Vector3(0.6f, 0.7f, 0.3f), t => Rod(t, Vector3.zero, new Vector3(0.1f, 0.4f, 0.15f), 0.035f, M("FFFFFF", 0.5f)));
                baton.gameObject.SetActive(false);
                void Stage() { foreach (var kv in sp) Put(kv.Key, kv.Value, kv.Value + Vector3.back * 40); }
                var stageCam = (CAR + new Vector3(0, 2.3f, -11.5f), CAR + new Vector3(0, 1.4f, -1.6f));

                // ---------- 0. title ----------
                Scene = 0;
                Stage();
                foreach (var t in five) t.bob = 0.3f;
                carSpeed = 30;
                Cut(CAR + new Vector3(0, 12, -32), CAR + new Vector3(0, 3, 0));
                Glide(CAR + new Vector3(0, 3.6f, -15), CAR + new Vector3(0, 2.6f, 0), 0.55f);
                Title("ЛУКАШАРИКИ", "серия «Большой концерт»");
                AudioFx.Play("portal");
                yield return Say(wN, "e7_01", "Лукашарики! Серия седьмая: «Большой концерт»!");
                Title(null);
                yield return Say(wN, "e7_02", "Сегодня в долине праздник — большой концерт! Все готовятся.");
                carSpeed = 0; carAngle = 36;
                foreach (var t in five) t.bob = 0;

                // ---------- 1. the musicians ----------
                Scene = 1;
                Stage();
                CloseUp(luk, 5f, 0.4f);
                luk.mood = 1;
                len0 = Speak(wL, "e7_03", "Я буду играть на барабане! Бум-бум-бум!");
                int hits = 0;
                yield return Act(len0 + 0.2f, t =>
                {
                    if (t > len0 * 0.55f && hits < 3 && t > len0 * 0.55f + hits * 0.3f)
                    {
                        hits++; AudioFx.Play("box", 0.8f); luk.Land(0.3f);
                        drum.localScale = new Vector3(1.08f, 0.9f, 1.08f);
                    }
                    drum.localScale = Vector3.Lerp(drum.localScale, Vector3.one, Dt * 10);
                });
                Hush();
                CloseUp(puh, 5f, -0.4f); puh.mood = 1;
                int nk = 0;
                yield return SayDo(wP, "e7_04", "А я буду петь! Ля-ля-ля-ля!", t =>
                {
                    puh.tilt = Mathf.Sin(t * 4) * 8;
                    if (t > 1.2f && nk < 4 && t > 1.2f + nk * 0.25f) { AudioFx.Note(4 + nk); nk++; }
                });
                puh.tilt = 0;
                CloseUp(bub, 5.6f, 0.4f); bub.mood = 1;
                yield return SayDo(wB, "e7_05", "А у меня труба! Ту-ту-у-у!", t => { bub.puff = 1 + 0.12f * Mathf.Abs(Mathf.Sin(t * 3)); trumpet.localScale = Vector3.one * (1 + 0.1f * Mathf.Abs(Mathf.Sin(t * 9))); });
                bub.puff = 1; trumpet.localScale = Vector3.one;
                AudioFx.Play("hum", 0.6f);
                Cut(sp[prof] + new Vector3(-0.9f, 1.4f, -2.8f), xylo.localPosition + Vector3.up * 0.7f, 44);
                prof.mood = 1;
                nk = 0;
                yield return SayDo(wPr, "e7_06", "А я — на ксилофоне. Научно и мелодично!", t =>
                {
                    prof.lean = 10 * Mathf.Abs(Mathf.Sin(t * 6));
                    if (nk < 6 && t > 0.3f + nk * 0.28f) { AudioFx.Note(nk); bars[nk].localScale = new Vector3(bars[nk].localScale.x, 0.1f, 0.11f); nk++; }
                    foreach (var b in bars) b.localScale = new Vector3(b.localScale.x, Mathf.Lerp(b.localScale.y, 0.05f, Dt * 8), 0.11f);
                });
                prof.lean = 0;

                // ---------- 2. rehearsal: chaos! ----------
                Scene = 2;
                for (int k = 0; k < 3; k++) { birds[k].Reset(); birds[k].pos = AUD + new Vector3(-2.6f + k * 0.7f, 0, 1.2f); birds[k].Face(CAR); }
                Cut(stageCam.Item1, stageCam.Item2, 54);
                foreach (var t in five) t.mood = 1;
                len0 = Speak(wAll, "e7_07", "Бум! Ля-ля! Ту-ту! Динь-дилинь!");
                string[] noise = { "box", "boing", "bonk", "hum", "crate", "click", "flip" };
                yield return Act(len0 + 0.8f, t =>
                {
                    if (URandom.value < 0.35f) AudioFx.Play(noise[URandom.Range(0, noise.Length)], 0.5f);
                    if (URandom.value < 0.2f) AudioFx.Note(URandom.Range(0, 8));
                    foreach (var a in five) { a.hop = Mathf.Abs(Mathf.Sin(t * (6 + a.r * 8))) * 0.45f; a.tilt = Mathf.Sin(t * 11 + a.r * 20) * 12; }
                    shake = 0.15f;
                });
                Hush();
                foreach (var a in five) { a.hop = 0; a.tilt = 0; a.mood = 0; }
                Cut(AUD + new Vector3(-1.9f, 0.7f, -1.6f), birds[1].Center, 40);
                foreach (var bd in birds) bd.mood = 5;
                yield return SayDo(wBird, "e7_08", "Ой-ой-ой! Слишком громко! Чик-чирик!", t =>
                {
                    foreach (var bd in birds) { bd.flap = 1; bd.hop = Mathf.Abs(Mathf.Sin(t * 6 + bd.r * 30)) * 0.5f; }
                });
                foreach (var bd in birds) { bd.flap = 0; bd.hop = 0; bd.mood = 3; }
                baton.gameObject.SetActive(true);
                prof.mood = 0;
                CloseUp(prof, 5f, -0.4f);
                yield return SayDo(wPr, "e7_09", "Стоп-стоп-стоп! Нужен дирижёр. Играем по очереди, вместе и дружно!", t => baton.localRotation = Quaternion.Euler(Mathf.Sin(t * 7) * 30, 0, Mathf.Cos(t * 7) * 20));

                // ---------- 3. Чихуня has no instrument ----------
                Scene = 3;
                chi.mood = 2;
                CloseUp(chi, 5.2f, 0.3f);
                yield return SayDo(wC, "e7_10", "А у меня нет инструмента... Я умею только чихать.", t => chi.lean = 10 * Mathf.Min(1, t));
                chi.lean = 0;
                luk.Face(chi.pos);
                CloseUp(luk, 5f, 0.4f);
                luk.mood = 1;
                yield return Say(wL, "e7_11", "Не грусти, Чихуня! Мы обязательно что-нибудь придумаем!");
                luk.Face(luk.pos + Vector3.back * 40);
                chi.mood = 0;

                // ---------- 4. the concert ----------
                Scene = 4;
                Put(onion, AUD, CAR); onion.mood = 1;
                for (int k = 0; k < 3; k++) { birds[k].Reset(); birds[k].mood = 1; }
                Vector3 BirdOnOnion(int k) => onion.Head + onion.Right * ((k - 1) * 0.75f) + Vector3.down * 0.35f - onion.Fwd * 0.3f;
                for (int k = 0; k < 3; k++) { birds[k].pos = BirdOnOnion(k); birds[k].Face(CAR); }
                Cut(CAR + new Vector3(-2.5f, 3.4f, -21f), CAR + new Vector3(0, 1.6f, -2f), 50);
                Glide(CAR + new Vector3(-1.2f, 3.0f, -17f), CAR + new Vector3(0, 1.6f, -2f), 0.5f);
                AudioFx.Play("win3", 0.7f);
                yield return Say(wN, "e7_12", "И вот — концерт начинается! Музыка!");
                // a little show: drum, singing, trumpet, xylophone — all together
                float beat = 60f / 132f;
                int lastB = -1;
                var show = Bg(Act(99f, t =>
                {
                    int b = (int)(t / beat);
                    if (b != lastB)
                    {
                        lastB = b;
                        if (b % 2 == 0) { AudioFx.Play("box", 0.45f); luk.Land(0.3f); drum.localScale = new Vector3(1.08f, 0.9f, 1.08f); }
                        if (b % 4 == 1) { int n = URandom.Range(0, 6); AudioFx.Note(n); bars[n].localScale = new Vector3(bars[n].localScale.x, 0.1f, 0.11f); }
                    }
                    drum.localScale = Vector3.Lerp(drum.localScale, Vector3.one, Dt * 10);
                    foreach (var br2 in bars) br2.localScale = new Vector3(br2.localScale.x, Mathf.Lerp(br2.localScale.y, 0.05f, Dt * 8), 0.11f);
                    puh.tilt = Mathf.Sin(t * 3.4f) * 10; puh.mood = 1;
                    bub.puff = 1 + 0.1f * Mathf.Abs(Mathf.Sin(t * 3.4f));
                    prof.lean = 10 * Mathf.Abs(Mathf.Sin(t * 6.8f));
                    baton.gameObject.SetActive(false);
                    chi.hop = Mathf.Abs(Mathf.Sin(t * 6.8f)) * 0.3f;
                    for (int k = 0; k < 3; k++) { birds[k].pos = BirdOnOnion(k); birds[k].hop = Mathf.Abs(Mathf.Sin(t * 6.8f + k)) * 0.15f; }
                }));
                Cut(sp[luk] + new Vector3(1.4f, 1.2f, -3.2f), sp[luk] + Vector3.up * 0.7f, 46);
                yield return Wait(1.6f);
                Cut(sp[puh] + new Vector3(0.6f, 1.3f, -3.0f), sp[puh] + Vector3.up * 0.7f, 44);
                yield return Wait(1.6f);
                Cut(sp[bub] + new Vector3(-0.8f, 1.3f, -3.2f), sp[bub] + Vector3.up * 0.8f, 44);
                yield return Wait(1.6f);
                CloseUp(onion, 3.4f, 0, 46);
                onion.mood = 1;
                yield return SayDo(wO, "e7_13", "Браво! Как красиво! Ой, я сейчас затанцую!", t => onion.tilt = Mathf.Sin(t * 3.4f) * 6);
                Cut(CAR + new Vector3(-4f, 3.2f, -22f), CAR + new Vector3(0, 2.0f, -4f), 52);
                var dance = Bg(Act(99f, t =>
                {
                    onion.hop = Mathf.Abs(Mathf.Sin(t * 3.4f)) * 1.2f;
                    onion.tilt = Mathf.Sin(t * 3.4f) * 12;
                    if (onion.hop < 0.05f) { shake = Mathf.Max(shake, 0.4f); }
                    foreach (var a in five) if (a != chi) a.hop = Mathf.Abs(Mathf.Sin(t * 3.4f + 1.2f)) * 0.5f;
                    for (int k = 0; k < 3; k++) birds[k].pos = BirdOnOnion(k);
                }));
                yield return Wait(1.0f);
                CloseUp(puh, 5f, 0.4f);
                yield return Say(wP, "e7_14", "Лук танцует! Вся долина прыгает!");

                // ---------- 5. the loudest finale ----------
                Scene = 5;
                StopCoroutine(dance); StopCoroutine(show);
                onion.hop = 0; onion.tilt = 0;
                foreach (var a in five) { a.hop = 0; a.tilt = 0; a.lean = 0; a.puff = 1; }
                Stage();
                for (int k = 0; k < 3; k++) birds[k].pos = BirdOnOnion(k);
                baton.gameObject.SetActive(true);
                prof.Face(chi.pos); prof.mood = 1;
                CloseUp(prof, 5f, -0.4f);
                yield return SayDo(wPr, "e7_15", "А теперь — самый громкий финал! Чихуня, твой выход!", t => baton.localRotation = Quaternion.Euler(Mathf.Sin(t * 7) * 30, 0, Mathf.Cos(t * 7) * 20));
                chi.mood = 5;
                yield return Hop(chi, CAR + new Vector3(0, 0.5f, -3.25f), 0.8f, 0.4f, 3);
                chi.Face(chi.pos + Vector3.back * 40);
                Cut(chi.Center + new Vector3(0.5f, 0.4f, -2.8f), chi.Center, 42);
                len0 = Speak(wC, "e7_16", "Мой? Правда? Ну... а... а... АПЧХИ!");
                yield return Act(Mathf.Max(0.3f, len0 - 0.9f), t => chi.noseK = 1 + 0.7f * Mathf.Clamp01(t / len0));
                chi.noseK = 1;
                yield return Sneeze(chi, 0.4f, 2.4f);
                Hush();
                shake = 1.0f;
                AudioFx.Play("zap");
                Cut(CAR + new Vector3(0, 4.5f, -24f), CAR + new Vector3(0, 6f, -2f), 58);
                carSpeed = 90;
                var fire = Bg(Act(99f, t =>
                {
                    if (URandom.value < 0.07f)
                    {
                        var p = CAR + new Vector3(URandom.Range(-9f, 9f), URandom.Range(8f, 13f), URandom.Range(-2f, 6f));
                        Confetti(p, 30, 5f, Vector3.zero, true);
                        AudioFx.Play(URandom.value < 0.5f ? "coin" : "zap", 0.35f);
                    }
                }));
                Confetti(CAR + new Vector3(0, 3, -3), 60, 6f, Vector3.up * 5);
                foreach (var a in five) a.mood = 1;
                yield return Say(wN, "e7_17", "Это был самый громкий финал на свете!");
                narr0 = Bg(Say(wFour, "e7_18", "Ура! Браво, Чихуня!"));
                yield return Cheer(1.6f, "win2");
                yield return narr0;

                // ---------- 6. goodbye ----------
                Scene = 6;
                carSpeed = 0; carAngle = 36;
                CloseUp(chi, 5.2f, 0.3f);
                chi.mood = 1;
                Bg(Jump(chi, 0.6f, 0.45f));
                yield return Say(wC, "e7_19", "Я тоже музыкант! Я играю на чихе!");
                CloseUp(onion, 3.4f, 0, 46);
                yield return Say(wO, "e7_20", "Спасибо, друзья! Это был самый лучший концерт!");
                Stage();
                Put(chi, CAR + new Vector3(0, 0.5f, -3.25f), front);
                CloseUp(luk, 5f, 0.4f);
                luk.mood = 1;
                yield return Say(wL, "e7_21", "Вот и закончились наши приключения. Но мы обязательно вернёмся!");
                Cut(CAR + new Vector3(0, 3.4f, -17f), CAR + new Vector3(0, 2.2f, -2f), 52);
                Title("КОНЕЦ");
                AudioFx.Play("win4", 0.7f);
                yield return Say(wN, "e7_22", "Лукашарики! Конец!");
                titleKick = 1;
                narr0 = Bg(Say(wAll, "e7_23", "До свидания, ребята! Пока-пока!"));
                yield return Cheer(1.4f, null);
                yield return narr0;
                StopCoroutine(fire);
                yield return EndCard(CAR + new Vector3(0, 2.2f, -2f), 15);
            }
        }
    }
}
