using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Procedural skin / cloth textures for the chubrik (patterns follow the body colour).</summary>
    public static class Patterns
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        const int S = 256;

        static Texture2D C(string key, System.Func<Canvas2> make, bool repeat = true)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            t = make().Tex(repeat); t.name = key; cache[key] = t;
            return t;
        }

        static Color Dark(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1);
        static Color Light(Color c, float k) => Color.Lerp(c, Color.white, k);

        /// <summary>Skin pattern 0..7 of the "Узор кожи" department over the body colour.</summary>
        public static Texture2D Skin(int kind, Color body) => C("skin" + kind + ColorUtility.ToHtmlStringRGB(body), () =>
        {
            var cv = new Canvas2(S, S, body);
            var r = new Rng(kind * 17 + 3);
            switch (kind)
            {
                case 0: // tiger stripes
                {
                    var b = Color.Lerp(body, Parts.Hex("FF8A1E"), 0.5f);
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            float w = Mathf.Sin((x / (float)S) * Mathf.PI * 2 * 7 + Tex.Fbm(x / 32f, y / 32f, 8, 5) * 5f);
                            cv.px[y * S + x] = w > 0.55f ? Parts.Hex("1A1A1A") : b;
                        }
                    break;
                }
                case 1: // leopard spots
                {
                    var b = Color.Lerp(body, Parts.Hex("E8B04A"), 0.45f);
                    for (int i = 0; i < S * S; i++) cv.px[i] = b;
                    for (int i = 0; i < 70; i++)
                    {
                        float x = r.F() * S, y = r.F() * S, rad = r.F(7, 13);
                        cv.Circle(x, y, rad, Parts.Hex("2A1A0A"));
                        cv.Circle(x + r.F(-2, 2), y + r.F(-2, 2), rad * 0.55f, Dark(b, 0.75f));
                    }
                    break;
                }
                case 2: // military camo (own colours)
                {
                    string[] cols = { "4B5A2A", "6E7B3C", "3A2E1E", "8C8B5A" };
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            float f = Tex.Fbm(x / 40f, y / 40f, 6, 9);
                            int k = Mathf.Clamp((int)(f * 4.2f - 0.6f), 0, 3);
                            cv.px[y * S + x] = Parts.Hex(cols[k]);
                        }
                    break;
                }
                case 3: // starry sky
                {
                    var sky = Color.Lerp(Parts.Hex("0B1440"), body, 0.15f);
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            float f = Tex.Fbm(x / 50f, y / 50f, 5, 31);
                            cv.px[y * S + x] = Color.Lerp(sky, Parts.Hex("5B2A8C"), f * 0.6f);
                        }
                    for (int i = 0; i < 260; i++) cv.Circle(r.F() * S, r.F() * S, r.F(0.5f, 1.8f), new Color(1, 1, r.F(0.7f, 1f), 1));
                    break;
                }
                case 4: // chess checks
                {
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                            cv.px[y * S + x] = ((x / 32 + y / 32) % 2 == 0) ? body : Parts.Hex("1A1A1A");
                    break;
                }
                case 5: // waves
                {
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            float w = Mathf.Sin(y / (float)S * Mathf.PI * 2 * 6 + Mathf.Sin(x / (float)S * Mathf.PI * 2 * 3) * 1.6f);
                            cv.px[y * S + x] = w > 0.3f ? Light(body, 0.55f) : (w < -0.5f ? Dark(body, 0.7f) : body);
                        }
                    break;
                }
                case 6: // holographic shine (hue also cycles in game)
                {
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            float h = Mathf.Repeat((x + y) / (float)S * 1.5f + Tex.Fbm(x / 40f, y / 40f, 6, 4) * 0.4f, 1f);
                            cv.px[y * S + x] = Color.HSVToRGB(h, 0.35f, 1f);
                        }
                    break;
                }
                default: // carbon weave
                {
                    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                        {
                            int cx = x / 8, cy = y / 8;
                            bool a = (cx + cy) % 2 == 0;
                            float fx = (x % 8) / 8f, fy = (y % 8) / 8f;
                            float g = a ? 0.12f + Mathf.Sin(fx * Mathf.PI) * 0.12f : 0.1f + Mathf.Sin(fy * Mathf.PI) * 0.1f;
                            cv.px[y * S + x] = new Color(g, g, g * 1.1f, 1);
                        }
                    break;
                }
            }
            return cv;
        });

        public static Texture2D Stripes(string a, string b, int n = 8, bool vertical = false) => C("str" + a + b + n + vertical, () =>
        {
            var cv = new Canvas2(64, 64, Parts.Hex(a));
            var cb = Parts.Hex(b);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    if (((vertical ? x : y) * n / 64) % 2 == 1) cv.px[y * 64 + x] = cb;
            return cv;
        });

        public static Texture2D Plaid(string a, string b, string c) => C("plaid" + a + b + c, () =>
        {
            var cv = new Canvas2(128, 128, Parts.Hex(a));
            var cb = Parts.Hex(b); cb.a = 0.55f; var cc = Parts.Hex(c); cc.a = 0.8f;
            for (int i = 0; i < 128; i += 32) { cv.Rect(i, 0, 10, 128, cb); cv.Rect(0, i, 128, 10, cb); cv.Rect(i + 20, 0, 2, 128, cc); cv.Rect(0, i + 20, 128, 2, cc); }
            return cv;
        });

        public static Texture2D Denim() => C("denim", () =>
        {
            var r = new Rng(77);
            var cv = new Canvas2(128, 128, Parts.Hex("3E5F8A"));
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float d = ((x + y) % 4 < 2 ? 0.04f : -0.03f) + (Tex.Fbm(x / 20f, y / 20f, 6, 2) - 0.5f) * 0.25f;
                    var c = cv.px[y * 128 + x];
                    cv.px[y * 128 + x] = new Color(c.r + d, c.g + d, c.b + d, 1);
                }
            // rips: light patches with threads
            for (int i = 0; i < 4; i++)
            {
                float x = r.F() * 128, y = r.F() * 128;
                cv.Rect(x, y, r.F(14, 22), r.F(6, 9), Parts.Hex("D9DEE6"));
                for (int k = 0; k < 4; k++) cv.Rect(x, y + 1 + k * 2, 20, 0.7f, Parts.Hex("A9B6C8"));
            }
            return cv;
        });

        public static Texture2D Sweater(string bas) => C("sweat" + bas, () =>
        {
            var cv = new Canvas2(128, 128, Parts.Hex(bas));
            var w = Color.white;
            for (int x = 0; x < 128; x++)
            {
                int zig = Mathf.Abs((x % 16) - 8);
                for (int k = 0; k < 3; k++) cv.Set(x, 40 + zig + k, w);
                for (int k = 0; k < 3; k++) cv.Set(x, 96 + zig + k, w);
            }
            for (int i = 0; i < 128; i += 32)
            {
                // little deer: body + head + antlers
                cv.Rect(i + 8, 64, 12, 6, w); cv.Rect(i + 18, 70, 4, 6, w);
                cv.Rect(i + 9, 58, 2, 6, w); cv.Rect(i + 17, 58, 2, 6, w);
                cv.Rect(i + 18, 76, 1, 4, w); cv.Rect(i + 21, 76, 1, 4, w); cv.Rect(i + 17, 79, 6, 1, w);
            }
            return cv;
        });

        public static Texture2D Floral() => C("floral", () =>
        {
            var r = new Rng(12);
            var cv = new Canvas2(128, 128, Parts.Hex("FF8A3D"));
            for (int i = 0; i < 18; i++)
            {
                float x = r.F() * 128, y = r.F() * 128;
                var pc = i % 2 == 0 ? Color.white : Parts.Hex("FFE14D");
                for (int k = 0; k < 5; k++) { float a = k * Mathf.PI * 2 / 5; cv.Circle(x + Mathf.Cos(a) * 5, y + Mathf.Sin(a) * 5, 3.5f, pc); }
                cv.Circle(x, y, 2.5f, Parts.Hex("D9381E"));
                cv.Line(x + 8, y, x + 16, y + 6, 3, Parts.Hex("2E8B3A"));
            }
            return cv;
        });

        public static Texture2D Swirl() => C("swirl", () =>
        {
            var cv = new Canvas2(128, 128, Color.white);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float dx = x - 64, dy = y - 64;
                    float a = Mathf.Atan2(dy, dx) + Mathf.Sqrt(dx * dx + dy * dy) * 0.12f;
                    if (Mathf.Sin(a * 3) > 0) cv.px[y * 128 + x] = Parts.Hex("FF4FA0");
                }
            return cv;
        });

        /// <summary>Particle / shadow shapes (white with alpha).</summary>
        public static Texture2D Shape(string kind) => C("shape" + kind, () =>
        {
            const int N = 64;
            var cv = new Canvas2(N, N, new Color(1, 1, 1, 0));
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2 - 1, v = (y + 0.5f) / N * 2 - 1;
                    float a = 0;
                    switch (kind)
                    {
                        case "soft": a = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v)), 1.5f); break;
                        case "disc": a = Mathf.Clamp01((1 - Mathf.Sqrt(u * u + v * v)) * N * 0.5f); break;
                        case "ring": { float d = Mathf.Sqrt(u * u + v * v); a = Mathf.Clamp01(1 - Mathf.Abs(d - 0.8f) * 8) + Mathf.Clamp01(0.35f - Mathf.Sqrt((u + 0.3f) * (u + 0.3f) + (v - 0.3f) * (v - 0.3f))) * 2; break; }
                        case "square": a = (Mathf.Abs(u) < 0.8f && Mathf.Abs(v) < 0.8f) ? 1 : 0; break;
                        case "puddle": { float d = Mathf.Sqrt(u * u + v * v) - 0.12f * Mathf.Sin(Mathf.Atan2(v, u) * 5) - 0.6f; a = Mathf.Clamp01(-d * 6); break; }
                        case "star":
                        {
                            float ang = Mathf.Atan2(v, u), d = Mathf.Sqrt(u * u + v * v);
                            float rr = 0.45f + 0.45f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f)), 3);
                            a = Mathf.Clamp01((rr - d) * 10);
                            break;
                        }
                        case "heart":
                        {
                            float hx = u * 1.2f, hy = -v * 1.2f + 0.25f;
                            float q = hx * hx + hy * hy - 0.6f;
                            a = q * q * q - hx * hx * hy * hy * hy < 0 ? 1 : 0;
                            break;
                        }
                        case "spark":
                        {
                            float d = Mathf.Sqrt(u * u + v * v);
                            a = Mathf.Clamp01(1 - d * 1.2f) * 0.6f + Mathf.Clamp01(1 - Mathf.Abs(u) * 14) * Mathf.Clamp01(1 - Mathf.Abs(v)) + Mathf.Clamp01(1 - Mathf.Abs(v) * 14) * Mathf.Clamp01(1 - Mathf.Abs(u));
                            break;
                        }
                        case "streak": a = Mathf.Clamp01(1 - Mathf.Abs(u) * 3) * Mathf.Clamp01(1 - Mathf.Abs(v) * 0.9f); break;
                        case "glyph": a = ((x / 8 + y / 8 * 3) % 4 != 0 && (x % 8) > 1 && (y % 8) > 1) ? 1 : 0; break;
                    }
                    cv.px[y * N + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                }
            return cv;
        }, false);
    }
}
