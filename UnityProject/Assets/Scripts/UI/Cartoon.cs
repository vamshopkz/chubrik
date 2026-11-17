using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chubrik
{
    /// <summary>
    /// 17.11 promo code «Лукашарики»: a short cartoon of our own about round little balls who ate a whole giant onion —
    /// they cry onion tears, wobble, faint from onion breath and roll down the hill laughing. A cross closes it.
    /// </summary>
    public static class Cartoon
    {
        static GameObject root;
        public static bool IsOpen => root != null;
        public static int Scene { get; private set; } = -1;
        public const int Scenes = 9;

        public static void Play()
        {
            Stop();
            Menus.Unlock();
            if (PlayerCtl.I) PlayerCtl.I.Frozen = true;
            var bg = UIKit.Img(UIRoot.TopLayer, UIKit.White, UIKit.Hex("8FD3FF"), "Cartoon", true);
            UIKit.Fill(bg.rectTransform);
            root = bg.gameObject;
            root.AddComponent<Show>();
        }

        public static void Stop()
        {
            if (root) UnityEngine.Object.Destroy(root);
            root = null; Scene = -1;
            if (PlayerCtl.I && !Menus.AnyOpen) PlayerCtl.I.Frozen = false;
        }

        /// <summary>One round character: a coloured ball with eyes, a mouth and an onion sprout.</summary>
        class Ball
        {
            public RectTransform rt, body, pupilL, pupilR, sprout;
            public TextMeshProUGUI mouth;
            public Vector2 home;
            public float size;

            public Ball(Transform p, string name, Color col, Vector2 pos, float size, string extra)
            {
                this.size = size; home = pos;
                rt = UIKit.Rect(name, p);
                UIKit.Place(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, new Vector2(size, size * 1.3f));
                // the onion sprout on top
                sprout = UIKit.Rect("Sprout", rt);
                UIKit.Place(sprout, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, size * 0.9f), new Vector2(size * 0.4f, size * 0.4f));
                for (int k = -1; k <= 1; k++)
                {
                    var leaf = UIKit.Img(sprout, UIKit.Pill, UIKit.Hex("3FAE3A"), "Leaf");
                    UIKit.Place(leaf.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(k * size * 0.07f, 0), new Vector2(size * 0.09f, size * 0.36f));
                    leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, -k * 22);
                }
                var shadow = UIKit.Img(rt, UIKit.Circle, new Color(0, 0, 0, 0.18f), "Shadow");
                UIKit.Place(shadow.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(size * 0.9f, size * 0.18f));
                var b = UIKit.Img(rt, UIKit.Circle, col, "Body");
                body = UIKit.Place(b.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(size, size));
                var shine = UIKit.Img(body, UIKit.Circle, new Color(1, 1, 1, 0.35f), "Shine");
                UIKit.Place(shine.rectTransform, new Vector2(0.3f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.22f, size * 0.16f));
                foreach (var sd in new[] { -1, 1 })
                {
                    var eye = UIKit.Img(body, UIKit.Circle, Color.white, "Eye");
                    UIKit.Place(eye.rectTransform, new Vector2(0.5f + sd * 0.17f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.26f, size * 0.3f));
                    var pu = UIKit.Img(eye.transform, UIKit.Circle, UIKit.Hex("1A1A2A"), "Pupil");
                    var prt = UIKit.Place(pu.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.12f, size * 0.14f));
                    if (sd < 0) pupilL = prt; else pupilR = prt;
                }
                mouth = UIKit.Txt(body, "u", size * 0.34f, UIKit.Hex("7A1F2B"), TextAlignmentOptions.Center, false);
                UIKit.Place(mouth.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.6f, size * 0.4f));
                if (extra == "glasses")
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var g = UIKit.Img(body, UIKit.Ring, UIKit.Hex("222222"), "Glass");
                        UIKit.Place(g.rectTransform, new Vector2(0.5f + sd * 0.17f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.34f, size * 0.34f));
                    }
                if (extra == "bow")
                    foreach (var sd in new[] { -1, 1 })
                    {
                        var bw = UIKit.Img(body, UIKit.Circle, UIKit.Hex("FF2D7A"), "Bow");
                        UIKit.Place(bw.rectTransform, new Vector2(0.78f + sd * 0.08f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.18f, size * 0.14f));
                    }
                if (extra == "cap")
                {
                    var c = UIKit.Img(body, UIKit.Pill, UIKit.Hex("1E5BD8"), "Cap");
                    UIKit.Place(c.rectTransform, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.7f, size * 0.18f));
                }
            }

            public void Look(Vector2 d) { pupilL.anchoredPosition = pupilR.anchoredPosition = d * size * 0.05f; }
        }

        class Show : MonoBehaviour
        {
            RectTransform stage;
            TextMeshProUGUI sub, speaker;
            readonly List<Ball> balls = new List<Ball>();
            RectTransform onion, pot, cloud, puddle;
            readonly List<RectTransform> tears = new List<RectTransform>();

            void Start()
            {
                stage = (RectTransform)transform;
                // the hill, the sun and a little house
                var sun = UIKit.Img(stage, UIKit.Circle, UIKit.Hex("FFE14D"), "Sun");
                UIKit.Place(sun.rectTransform, new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
                sun.gameObject.AddComponent<Spinner>();
                for (int k = 0; k < 3; k++)
                {
                    var cl = UIKit.Img(stage, UIKit.Pill, new Color(1, 1, 1, 0.9f), "Cloud");
                    UIKit.Place(cl.rectTransform, new Vector2(0.15f + k * 0.28f, 0.82f - k * 0.04f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 70));
                    cl.gameObject.AddComponent<Drift>();
                }
                var hill = UIKit.Img(stage, UIKit.Circle, UIKit.Hex("6EDC3C"), "Hill");
                UIKit.Place(hill.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(3200, 700));
                var house = UIKit.Img(stage, UIKit.Btn, UIKit.Hex("FF9F1C"), "House");
                UIKit.Place(house.rectTransform, new Vector2(0.12f, 0), new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(200, 160));
                var roof = UIKit.Txt(house.transform, "▲", 220, UIKit.Hex("C62828"), TextAlignmentOptions.Center, false);
                UIKit.Place(roof.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, -40), new Vector2(260, 200));
                var door = UIKit.Img(house.transform, UIKit.Btn, UIKit.Hex("7A4A1F"), "Door");
                UIKit.Place(door.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(60, 90));
                // the onion field
                for (int k = 0; k < 9; k++)
                {
                    var o = UIKit.Img(stage, UIKit.Circle, UIKit.Hex("F3E3C3"), "Onion");
                    UIKit.Place(o.rectTransform, new Vector2(0.55f + (k % 5) * 0.08f, 0), new Vector2(0.5f, 0), new Vector2(0, 230 + (k / 5) * 40), new Vector2(48, 40));
                    var lf = UIKit.Img(o.transform, UIKit.Pill, UIKit.Hex("3FAE3A"), "Leaf");
                    UIKit.Place(lf.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0), Vector2.zero, new Vector2(8, 34));
                }

                balls.Add(new Ball(stage, "Лукаш", UIKit.Hex("8BD450"), new Vector2(-420, 250), 170, ""));
                balls.Add(new Ball(stage, "Пухля", UIKit.Hex("FF8FC0"), new Vector2(-170, 250), 150, "bow"));
                balls.Add(new Ball(stage, "Бублик", UIKit.Hex("FFA640"), new Vector2(90, 250), 160, "cap"));
                balls.Add(new Ball(stage, "Профессор", UIKit.Hex("A58BFF"), new Vector2(340, 250), 180, "glasses"));

                // subtitles
                var bar = UIKit.Img(stage, UIKit.White, new Color(0, 0, 0, 0.55f), "SubBar");
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 120));
                speaker = UIKit.Txt(bar.transform, "", 28, UIKit.Gold, TextAlignmentOptions.TopLeft);
                UIKit.Place(speaker.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -8), new Vector2(600, 36));
                sub = UIKit.Txt(bar.transform, "", 36, Color.white, TextAlignmentOptions.Left);
                UIKit.Place(sub.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 8), new Vector2(1350, 70));
                sub.enableAutoSizing = true; sub.fontSizeMin = 20; sub.fontSizeMax = 36;

                var x = UIKit.Button(stage, "X", UIKit.Red, Stop, 40, "Close");
                var safe = UIRoot.Safe();
                UIKit.Place(x.Bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20 - safe.z, -20 - safe.w), new Vector2(96, 86));
                StartCoroutine(Run());
            }

            void Say(string who, string text) { speaker.text = who; sub.text = text; }

            IEnumerator Clip(float dur, Action<float> f)
            {
                for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { f(t); yield return null; }
                f(dur);
            }

            void Home(float t, float bounce = 0)
            {
                for (int i = 0; i < balls.Count; i++)
                {
                    var b = balls[i];
                    float h = Mathf.Abs(Mathf.Sin(t * 6 + i)) * bounce;
                    b.rt.anchoredPosition = b.home + Vector2.up * h;
                    b.rt.localRotation = Quaternion.identity;
                    b.body.localScale = new Vector3(1 + (bounce > 0 && h < 4 ? 0.08f : 0), 1 - (bounce > 0 && h < 4 ? 0.08f : 0), 1);
                }
            }

            RectTransform Prop(string name, Sprite sp, Color c, Vector2 pos, Vector2 size)
            {
                var i = UIKit.Img(stage, sp, c, name);
                var r = UIKit.Place(i.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, size);
                r.SetSiblingIndex(stage.childCount - 3);   // under the subtitles and the cross
                return r;
            }

            IEnumerator Run()
            {
                // 1. title
                Scene = 0;
                var title = UIKit.Txt(stage, "ЛУКАШАРИКИ", 120, UIKit.Gold);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 160));
                Say("", "Мультфильм «Лукашарики». Серия «Гигантский лук»");
                AudioFx.Play("portal");
                yield return Clip(3f, t => { title.rectTransform.localScale = Vector3.one * (0.6f + Mathf.Min(1, t * 2) * 0.4f + Mathf.Sin(t * 6) * 0.03f); Home(t, 20); });
                Destroy(title.gameObject);

                // 2. a giant onion
                Scene = 1;
                onion = Prop("GiantOnion", UIKit.Circle, UIKit.Hex("F3E3C3"), new Vector2(0, 600), new Vector2(260, 240));
                var stripes = UIKit.Txt(onion, ")  (  )", 120, UIKit.Hex("D8B98A"), TextAlignmentOptions.Center, false); UIKit.Fill(stripes.rectTransform);
                var top = UIKit.Img(onion, UIKit.Pill, UIKit.Hex("3FAE3A"), "Top"); UIKit.Place(top.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(30, 120));
                Say("Лукаш", "Смотрите! На грядке вырос ОГРОМНЫЙ лук!");
                AudioFx.Play("magic");
                yield return Clip(3.5f, t => { onion.anchoredPosition = new Vector2(0, Mathf.Lerp(900, 420, Mathf.Min(1, t * 1.5f))); Home(t, 30); foreach (var b in balls) b.Look(Vector2.up); });

                // 3. soup
                Scene = 2;
                pot = Prop("Pot", UIKit.Btn, UIKit.Hex("555B66"), new Vector2(0, 250), new Vector2(300, 150));
                Say("Пухля", "Давайте сварим из него луковый суп! Весь-весь!");
                yield return Clip(3.5f, t => { onion.anchoredPosition = new Vector2(0, Mathf.Lerp(420, 300, t / 3.5f)); onion.localScale = Vector3.one * Mathf.Lerp(1, 0.6f, t / 3.5f); Home(t, 10); foreach (var b in balls) b.Look(Vector2.zero); });
                Destroy(onion.gameObject);

                // 4. eating
                Scene = 3;
                Say("Все", "Ам-ам-ам! Ням-ням! Вкуснотища!");
                yield return Clip(3.5f, t =>
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var b = balls[i];
                        b.rt.anchoredPosition = Vector2.Lerp(b.home, new Vector2((i - 1.5f) * 190, 300), Mathf.Min(1, t)) + Vector2.up * Mathf.Abs(Mathf.Sin(t * 10 + i)) * 25;
                        b.mouth.text = ((int)(t * 8) + i) % 2 == 0 ? "o" : "u";
                        if (((int)(t * 8) + i) % 4 == 0) AudioFx.Play("click", 0.3f);
                    }
                });

                // 5. onion tears
                Scene = 4;
                Say("Бублик", "Ой-ой-ой! Глаза щиплет! Это луковые слёзы!");
                AudioFx.Play("error");
                for (int k = 0; k < 16; k++) tears.Add(Prop("Tear", UIKit.Circle, UIKit.Hex("5FD3FF"), Vector2.zero, new Vector2(18, 26)));
                yield return Clip(4f, t =>
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var b = balls[i];
                        b.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 5 + i) * 12);
                        b.mouth.text = "n";
                        b.Look(Vector2.down);
                    }
                    for (int k = 0; k < tears.Count; k++)
                    {
                        var b = balls[k % 4];
                        float ph = Mathf.Repeat(t * 1.4f + k * 0.37f, 1f);
                        tears[k].anchoredPosition = b.rt.anchoredPosition + new Vector2(((k / 4) % 2 == 0 ? -1 : 1) * b.size * 0.3f, b.size * 0.55f - ph * b.size * 0.6f);
                    }
                });

                // 6. dizzy
                Scene = 5;
                Say("Профессор", "Спокойно! Научный факт: от целого лука голова кружится — шарики шатаются!");
                yield return Clip(4.5f, t =>
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var b = balls[i];
                        var p = new Vector2((i - 1.5f) * 190, 300);
                        b.rt.anchoredPosition = p + new Vector2(Mathf.Sin(t * 3 + i * 2) * 50, 0);
                        b.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3 + i * 2) * 25);
                        b.Look(new Vector2(Mathf.Cos(t * 9 + i), Mathf.Sin(t * 9 + i)));   // spinning eyes
                        b.mouth.text = "~";
                    }
                    foreach (var tr in tears) tr.anchoredPosition += Vector2.down * 3;
                });

                // 7. onion breath
                Scene = 6;
                cloud = Prop("Breath", UIKit.Circle, new Color(0.55f, 0.9f, 0.3f, 0.5f), new Vector2(0, 360), new Vector2(100, 70));
                Say("Пухля", "Фу-у-у! Кто тут дышит луком?! Бублик, это ты?!");
                AudioFx.Play("glitch");
                yield return Clip(4f, t =>
                {
                    cloud.localScale = Vector3.one * (1 + t * 2.2f);
                    cloud.anchoredPosition = new Vector2(Mathf.Sin(t) * 60, 360 + t * 30);
                    var bub = balls[2];
                    // Бублик faints and rolls away
                    float f = Mathf.Clamp01((t - 1.5f) / 1.5f);
                    bub.rt.localRotation = Quaternion.Euler(0, 0, -f * 360 * 1.5f);
                    bub.rt.anchoredPosition = new Vector2(Mathf.Lerp(95, 560, f), 300);
                    bub.mouth.text = f > 0 ? "x" : "o";
                    for (int i = 0; i < 4; i++) if (i != 2) balls[i].Look(Vector2.right);
                });
                Destroy(cloud.gameObject);

                // 8. sliding down the hill on the tears, laughing
                Scene = 7;
                puddle = Prop("Puddle", UIKit.Circle, new Color(0.37f, 0.83f, 1f, 0.8f), new Vector2(0, 230), new Vector2(900, 90));
                Say("Все", "Ха-ха-ха! Луковая горка! Поехали-и-и!");
                AudioFx.Play("jump4");
                yield return Clip(4.5f, t =>
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var b = balls[i];
                        float u = Mathf.Repeat(t * 0.5f + i * 0.25f, 1f);
                        b.rt.anchoredPosition = new Vector2(Mathf.Lerp(-700, 700, u), 270 + Mathf.Abs(Mathf.Sin(u * 18)) * 60);
                        b.rt.localRotation = Quaternion.Euler(0, 0, -u * 720);
                        b.mouth.text = "D";
                        b.Look(Vector2.zero);
                    }
                    foreach (var tr in tears) tr.gameObject.SetActive(false);
                });

                // 9. the end
                Scene = 8;
                var end = UIKit.Txt(stage, "КОНЕЦ", 120, UIKit.Gold);
                UIKit.Place(end.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 160));
                Say("Профессор", "Мораль: лук полезный — но не ешьте его весь сразу!");
                AudioFx.Play("token");
                var again = UIKit.Button(stage, "ЕЩЁ РАЗ", UIKit.Green, Play, 32, "Again");
                UIKit.Place(again.Bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(300, 86));
                yield return Clip(999f, t =>
                {
                    end.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(t * 4) * 0.05f);
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var b = balls[i];
                        b.rt.anchoredPosition = b.home + Vector2.up * Mathf.Abs(Mathf.Sin(t * 5 + i)) * 30;
                        b.rt.localRotation = Quaternion.identity; b.mouth.text = "u";
                    }
                });
            }
        }

        class Spinner : MonoBehaviour { void Update() { transform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 20); } }
        class Drift : MonoBehaviour
        {
            float s;
            void Start() { s = UnityEngine.Random.value * 10; }
            void Update() { var r = (RectTransform)transform; r.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 0.2f + s) * 80, 0); }
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
