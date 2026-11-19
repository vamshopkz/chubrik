using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 19.11: the «film» look of the «Лукашарики» cartoon: procedural textures (grass, soil, bark, leaves, planks,
    /// roof tiles, water, onion skin, character skins), a sky with lit clouds, fog, a meadow of grass tufts and flowers,
    /// clustered trees and a cartoon-only post-processing Volume. Static parts are merged into a few meshes (phones).
    /// Textures and materials are made once and kept for replays; meshes and the Volume profile are freed on Stop.
    /// </summary>
    public static partial class Cartoon
    {
        static readonly List<Mesh> madeMeshes = new List<Mesh>();
        static VolumeProfile fxProfile;

        static void FreeLook()
        {
            foreach (var m in madeMeshes) if (m) UnityEngine.Object.Destroy(m);
            madeMeshes.Clear();
            if (fxProfile) UnityEngine.Object.Destroy(fxProfile);
            fxProfile = null;
        }

        static void NoShadowGo(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Meshes built for this run (freed in <see cref="FreeLook"/>).</summary>
        static GameObject Keep(GameObject go)
        {
            if (go) foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh) madeMeshes.Add(mf.sharedMesh);
            return go;
        }

        // ================= textures and materials =================
        static class Look
        {
            static readonly Dictionary<string, Texture2D> texs = new Dictionary<string, Texture2D>();
            static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

            /// <summary>Tileable fractal noise over u, v in 0..1 (<paramref name="per"/> cells across).</summary>
            static float N(float u, float v, int per, int seed, int oct = 4) => Tex.Fbm(u * per, v * per, per, seed, oct);
            static float Sm(float a, float b, float x) { x = Mathf.Clamp01((x - a) / (b - a)); return x * x * (3 - 2 * x); }
            static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

            static Texture2D T(string key, Func<Texture2D> make)
            {
                if (texs.TryGetValue(key, out var t) && t) return t;
                t = make(); t.name = "cartoon_" + key; texs[key] = t;
                return t;
            }

            static Texture2D Px(int w, int h, Func<float, float, Color> f, bool clampV = false, bool mip = true)
            {
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++) px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
                var t = new Texture2D(w, h, TextureFormat.RGBA32, mip)
                {
                    wrapModeU = TextureWrapMode.Repeat, wrapModeV = clampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
                    filterMode = mip ? FilterMode.Trilinear : FilterMode.Bilinear, anisoLevel = 4
                };
                t.SetPixels(px);
                t.Apply(mip, true);
                return t;
            }

            static Material Mt(string key, Func<Material> make)
            {
                if (mats.TryGetValue(key, out var m) && m) return m;
                m = make(); m.name = "cartoon_" + key; mats[key] = m;
                return m;
            }

            public static Material Lit(string key, Texture2D t, Color tint, float smooth, float metal = 0, Vector2? scale = null)
                => Mt(key, () =>
                {
                    var m = Mats.Tex(t, tint, smooth, metal);
                    if (scale.HasValue) m.mainTextureScale = scale.Value;
                    return m;
                });

            static Material TwoSided(string key, Texture2D t, float smooth)
                => Mt(key, () =>
                {
                    var m = Mats.Tex(t, Color.white, smooth);
                    m.SetFloat("_Cull", 0);
                    return m;
                });

            static Material Clear(string key, Texture2D t, Color tint, float smooth)
                => Mt(key, () =>
                {
                    var m = Mats.Transparent(tint);
                    m.mainTexture = t;
                    m.SetFloat("_Smoothness", smooth);
                    return m;
                });

            // ---------- ground ----------
            public static Texture2D GrassTex() => T("grass", () =>
            {
                const int S = 512;
                var r = new Rng(301);
                Color c0 = Hex("4C8A2A"), c1 = Hex("7DBA45"), dry = Hex("A9B85A");
                var cv = new Canvas2(S, S, c0);
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float u = (float)x / S, v = (float)y / S;
                        float a = N(u, v, 3, 11), b = N(u, v, 10, 12), d = N(u, v, 64, 13, 2);
                        var c = Color.Lerp(c0, c1, Sm(0.3f, 0.7f, a));
                        c = Color.Lerp(c, dry, Sm(0.58f, 0.8f, b) * 0.45f);
                        cv.px[y * S + x] = Mul(c, 0.86f + 0.26f * d);
                    }
                string[] cols = { "8CCB4E", "5E9E34", "3D7424", "9ED15C", "6FAE3C", "2F6220" };
                for (int i = 0; i < 9000; i++)
                {
                    float x = r.F() * S, y = r.F() * S;
                    var c = Hex(cols[r.I(cols.Length)]); c.a = r.F(0.35f, 0.75f);
                    cv.Line(x, y, x + r.F(-2.5f, 2.5f), y + r.F(3, 9), 1f, c);
                }
                string[] dots = { "FFFFFF", "FFF27A", "FFFFFF", "F7B6D8" };
                for (int i = 0; i < 260; i++) { var c = Hex(dots[r.I(dots.Length)]); c.a = 0.85f; cv.Circle(r.F() * S, r.F() * S, r.F(0.6f, 1.1f), c); }
                return cv.Tex();
            });
            public static Material Grass() => Lit("grass", GrassTex(), Color.white, 0.12f);
            public static Material Hill(string key, Color tint) => Lit("hill" + key, GrassTex(), tint, 0.05f, 0, new Vector2(10, 5));

            public static Texture2D SoilTex() => T("soil", () =>
            {
                const int S = 256;
                var r = new Rng(311);
                var cv = new Canvas2(S, S, Hex("6B4529"));
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float u = (float)x / S, v = (float)y / S;
                        float a = N(u, v, 6, 21), b = N(u, v, 24, 22, 3);
                        var c = Color.Lerp(Hex("553620"), Hex("8A6240"), Sm(0.35f, 0.7f, a));
                        c = Color.Lerp(c, Hex("3A2414"), Sm(0.55f, 0.35f, b) * 0.7f);
                        cv.px[y * S + x] = c;
                    }
                for (int i = 0; i < 700; i++) { var c = Hex(r.Chance(0.5f) ? "A08060" : "9A8F84"); c.a = r.F(0.5f, 0.9f); cv.Circle(r.F() * S, r.F() * S, r.F(0.6f, 1.8f), c); }
                for (int i = 0; i < 400; i++) { var c = Hex("2E1C10"); c.a = 0.5f; cv.Circle(r.F() * S, r.F() * S, r.F(0.5f, 1.4f), c); }
                return cv.Tex();
            });
            public static Material Soil() => Lit("soil", SoilTex(), Color.white, 0.08f);
            public static Material SoilDark() => Lit("soilDark", SoilTex(), Hex("B9A89A"), 0.18f);

            /// <summary>Soft-edged strip: u along (tiles), v across (alpha fades at the edges).</summary>
            public static Texture2D PathTex() => T("path", () =>
            {
                const int W = 256, H = 128;
                var r = new Rng(321);
                var cv = new Canvas2(W, H, Hex("B8946A"));
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (float)x / W, v = (float)y / H;
                        float a = N(u, v, 8, 31), b = N(u, v, 32, 32, 3);
                        var c = Color.Lerp(Hex("9C774F"), Hex("CDAE82"), Sm(0.3f, 0.7f, a));
                        float rut = Mathf.Exp(-Mathf.Pow((v - 0.32f) / 0.06f, 2)) + Mathf.Exp(-Mathf.Pow((v - 0.68f) / 0.06f, 2));
                        c = Mul(c, (0.9f + 0.18f * b) * (1 - rut * 0.08f));
                        cv.px[y * W + x] = c;
                    }
                for (int i = 0; i < 500; i++) { var c = Hex(r.Chance(0.6f) ? "E2D0B0" : "7E6448"); c.a = r.F(0.5f, 0.9f); cv.Circle(r.F() * W, r.F(0.15f, 0.85f) * H, r.F(0.6f, 1.8f), c); }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (float)x / W, v = (float)y / H;
                        float e = Mathf.Min(v, 1 - v) * 2 + (N(u, v * 0.25f, 16, 33, 3) - 0.5f) * 0.55f;
                        var c = cv.px[y * W + x]; c.a = Sm(0.1f, 0.5f, e);
                        cv.px[y * W + x] = c;
                    }
                var t = cv.Tex(); t.wrapModeV = TextureWrapMode.Clamp;
                return t;
            });
            public static Material Path() => Clear("path", PathTex(), Color.white, 0.1f);

            /// <summary>Shore sand: v = 0 at the water (wet, dark), fading out into the grass at v = 1.</summary>
            public static Texture2D ShoreTex() => T("shore", () =>
            {
                const int W = 256, H = 64;
                var r = new Rng(331);
                var cv = new Canvas2(W, H, Hex("D9C38F"));
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (float)x / W, v = (float)y / H;
                        float a = N(u, v * 0.25f, 12, 41);
                        var c = Color.Lerp(Hex("8E7A58"), Hex("E2CF9E"), Sm(0.05f, 0.4f, v + (a - 0.5f) * 0.2f));
                        cv.px[y * W + x] = Mul(c, 0.92f + 0.14f * a);
                    }
                for (int i = 0; i < 380; i++) { var c = Hex(r.Chance(0.5f) ? "A8A39A" : "F2E6C8"); c.a = 0.8f; cv.Circle(r.F() * W, r.F() * H, r.F(0.6f, 1.6f), c); }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (float)x / W, v = (float)y / H;
                        var c = cv.px[y * W + x]; c.a = 1 - Sm(0.55f, 0.95f, v + (N(u, v * 0.25f, 16, 43, 3) - 0.5f) * 0.4f);
                        cv.px[y * W + x] = c;
                    }
                var t = cv.Tex(); t.wrapModeV = TextureWrapMode.Clamp;
                return t;
            });
            public static Material Shore() => Clear("shore", ShoreTex(), Color.white, 0.2f);

            /// <summary>River water: u across (deep in the middle, light at the banks), v along the flow (tiles).</summary>
            public static Texture2D WaterTex() => T("water", () => Px(128, 256, (u, v) =>
            {
                float depth = 1 - Mathf.Abs(u - 0.5f) * 2;
                var c = Color.Lerp(Hex("6CCFE0"), Hex("2C78B4"), Sm(0.05f, 0.8f, depth));
                float n = N(u * 0.5f, v, 8, 51);
                float w = Mathf.Sin((v * 10 + n * 2.2f + u * 0.6f) * Mathf.PI * 2) * 0.5f + 0.5f;
                float w2 = Mathf.Sin((v * 23 - n * 1.6f) * Mathf.PI * 2) * 0.5f + 0.5f;
                c = Color.Lerp(c, Hex("CFF3FF"), Mathf.Pow(w, 14) * 0.55f + Mathf.Pow(w2, 20) * 0.3f);
                c = Color.Lerp(c, Hex("F2FBFF"), Sm(0.1f, 0.02f, Mathf.Min(u, 1 - u)) * 0.6f);
                return Mul(c, 0.94f + 0.12f * n);
            }));
            public static Material Water() => Lit("water", WaterTex(), Color.white, 0.94f);

            public static Texture2D StoneTex() => T("stone", () =>
            {
                var r = new Rng(341);
                var cv = new Canvas2(128, 128, Hex("9CA1A5"));
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float u = x / 128f, v = y / 128f;
                        float a = N(u, v, 4, 61), b = N(u, v, 16, 62, 3);
                        cv.px[y * 128 + x] = Mul(Color.Lerp(Hex("80878C"), Hex("C3C6C4"), Sm(0.3f, 0.75f, a)), 0.85f + 0.25f * b);
                    }
                for (int i = 0; i < 160; i++) { var c = Hex(r.Chance(0.5f) ? "5E6266" : "E6E4DE"); c.a = 0.6f; cv.Circle(r.F() * 128, r.F() * 128, r.F(0.5f, 1.5f), c); }
                return cv.Tex();
            });
            public static Material Stone(int k = 0) => Lit("stone" + k, StoneTex(), k == 0 ? Color.white : k == 1 ? Hex("D9CDBE") : Hex("B9C0C4"), 0.25f);

            // ---------- trees ----------
            public static Texture2D BarkTex() => T("bark", () => Px(128, 256, (u, v) =>
            {
                float f = Tex.Fbm(u * 10, v * 3, 10, 71);
                float g = N(u, v, 6, 72, 3);
                var c = Color.Lerp(Hex("5A3A22"), Hex("9A6E46"), Sm(0.25f, 0.75f, g));
                float crack = Sm(0.42f, 0.3f, f);
                c = Color.Lerp(c, Hex("2E1D10"), crack * 0.85f);
                return Mul(c, 0.9f + 0.2f * Tex.Fbm(u * 32, v * 8, 32, 73, 2));
            }));
            public static Material Bark() => Lit("bark", BarkTex(), Color.white, 0.1f);

            public static Texture2D LeafTex() => T("leaf", () =>
            {
                const int S = 256;
                var r = new Rng(351);
                var cv = new Canvas2(S, S, Hex("2C5A20"));
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float a = N((float)x / S, (float)y / S, 6, 81);
                        cv.px[y * S + x] = Color.Lerp(Hex("24501A"), Hex("3E7A2C"), a);
                    }
                string[] cols = { "6FB043", "88C653", "5A9C38", "A6D865", "4A8A30", "7EBE4C" };
                for (int i = 0; i < 2600; i++)
                {
                    float x = r.F() * S, y = r.F() * S, a = r.F() * Mathf.PI * 2, l = r.F(2.5f, 5.5f);
                    var c = Hex(cols[r.I(cols.Length)]); c.a = r.F(0.7f, 1f);
                    cv.Line(x, y, x + Mathf.Cos(a) * l, y + Mathf.Sin(a) * l, r.F(2f, 3.4f), c);
                    var hl = Hex("D6F08A"); hl.a = 0.35f;
                    cv.Circle(x + Mathf.Cos(a) * l * 0.4f, y + Mathf.Sin(a) * l * 0.4f, 0.7f, hl);
                }
                return cv.Tex();
            });
            /// <summary>0 dark (inside / below), 1 mid, 2 light (on top, in the sun), 3 yellowish.</summary>
            public static Material Leaf(int k)
            {
                Color[] tint = { Hex("A8BBA4"), Hex("E2EEDA"), Hex("FFFFFF"), Hex("FFF6C8") };
                return Lit("leaf" + k, LeafTex(), tint[Mathf.Clamp(k, 0, 3)], 0.18f);
            }

            // ---------- the house ----------
            public static Material Planks(string hex, float smooth = 0.15f) => Lit("planks" + hex, Tex.Planks(hex), Color.white, smooth);

            public static Texture2D RoofTex() => T("roof", () =>
            {
                const int S = 256, w = 32, h = 32;
                var px = new Color[S * S];
                var r = new Rng(361);
                var tileK = new float[8 * 9];
                for (int i = 0; i < tileK.Length; i++) tileK[i] = r.F(0.86f, 1.1f);
                Color b = Hex("C4523A");
                for (int y = 0; y < S; y++)
                {
                    int row = y / h, ty = y % h;
                    int off = row % 2 == 0 ? 0 : w / 2;
                    for (int x = 0; x < S; x++)
                    {
                        int xx = (x + off) % S, col = xx / w, tx = xx % w;
                        float k = tileK[(row * 9 + col) % tileK.Length];
                        float down = (float)ty / h;                                 // v grows down the slope
                        float sh = 0.62f + 0.48f * Sm(0f, 0.85f, down);              // under the tile above: shade
                        float barrel = 0.88f + 0.14f * Mathf.Sin(Mathf.PI * tx / w);
                        float edge = Mathf.Abs(tx + 0.5f - w / 2f) / (w / 2f);
                        float lip = h - 2 - 4 * (1 - Mathf.Sqrt(Mathf.Max(0, 1 - edge * edge)));   // rounded lower edge
                        var c = Mul(b, k * sh * barrel);
                        if (ty > lip) c = Mul(b, 0.42f);
                        if (tx == 0) c = Mul(c, 0.6f);
                        float n = Tex.Fbm(x / 16f, y / 16f, 16, 362, 3);
                        px[y * S + x] = Mul(c, 0.92f + 0.16f * n);
                    }
                }
                var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                t.SetPixels(px); t.Apply(true, true);
                return t;
            });
            public static Material Roof() => Lit("roof", RoofTex(), Color.white, 0.32f);

            public static Texture2D BrickTex() => T("brick", () =>
            {
                const int S = 128;
                var r = new Rng(371);
                var cv = new Canvas2(S, S, Hex("CFC6B8"));
                for (int row = 0; row < 8; row++)
                    for (int col = 0; col < 5; col++)
                    {
                        float x0 = col * 32 - (row % 2) * 16, y0 = row * 16;
                        var c = Mul(Hex("A65A42"), r.F(0.82f, 1.12f)); c.a = 1;
                        cv.Rect(x0 + 1.5f, y0 + 1.5f, 29, 13, c);
                    }
                cv.Noise(r, 0.07f);
                return cv.Tex();
            });
            public static Material Brick() => Lit("brick", BrickTex(), Color.white, 0.12f);

            // ---------- props ----------
            public static Texture2D HayTex() => T("hay", () =>
            {
                const int S = 256;
                var r = new Rng(381);
                var cv = new Canvas2(S, S, Hex("C9A24C"));
                string[] cols = { "F2D98A", "E2BE60", "A8863A", "FFE9A8", "BF9840" };
                for (int i = 0; i < 4200; i++)
                {
                    float x = r.F() * S, y = r.F() * S, a = r.F(-0.5f, 0.5f) + (r.Chance(0.2f) ? 1.4f : 0), l = r.F(6, 18);
                    var c = Hex(cols[r.I(cols.Length)]); c.a = r.F(0.6f, 1f);
                    cv.Line(x, y, x + Mathf.Cos(a) * l, y + Mathf.Sin(a) * l, r.F(0.8f, 1.6f), c);
                }
                return cv.Tex();
            });
            public static Material Hay() => Lit("hay", HayTex(), Color.white, 0.1f, 0, new Vector2(3, 2));
            public static Material Straw() => Lit("straw", HayTex(), Color.white, 0.1f);

            public static Texture2D MetalTex() => T("metal", () => Px(128, 128, (u, v) =>
            {
                float brush = Tex.Fbm(u * 64, v * 2, 64, 391, 2), n = N(u, v, 4, 392);
                return Mul(Hex("565C68"), 0.82f + 0.2f * brush + 0.12f * n);
            }));
            public static Material Metal(string hex = "FFFFFF") => Lit("metal" + hex, MetalTex(), Hex(hex), 0.68f, 0.55f);

            public static Texture2D StripesTex() => T("stripes", () => Px(256, 64, (u, v) =>
            {
                float s = Mathf.Sin(u * 8 * Mathf.PI * 2);
                float k = Sm(-0.12f, 0.12f, s);
                var c = Color.Lerp(Hex("FFF6EE"), Hex("E8414F"), k);
                float weave = ((int)(u * 256) + (int)(v * 64)) % 2 == 0 ? 0.03f : -0.03f;
                return Mul(c, 0.94f + weave + 0.08f * N(u, v, 8, 401, 2) - 0.12f * Sm(0.25f, 0f, v));
            }));
            public static Material Stripes() => Lit("stripes", StripesTex(), Color.white, 0.3f);

            public static Texture2D SoupTex() => T("soup", () =>
            {
                var r = new Rng(411);
                var cv = new Canvas2(128, 128, Hex("E2B04A"));
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                        cv.px[y * 128 + x] = Color.Lerp(Hex("D69A36"), Hex("F2C860"), N(x / 128f, y / 128f, 4, 412));
                for (int i = 0; i < 26; i++) { var c = Hex("FFF1C4"); c.a = 0.75f; cv.Ring(r.F() * 128, r.F() * 128, r.F(4, 9), 1.6f, c); }
                for (int i = 0; i < 40; i++) { var c = Hex("6FAE3C"); c.a = 0.9f; cv.Circle(r.F() * 128, r.F() * 128, r.F(0.8f, 1.6f), c); }
                for (int i = 0; i < 60; i++) { var c = Hex("FFF8DC"); c.a = 0.7f; cv.Circle(r.F() * 128, r.F() * 128, r.F(0.5f, 1.4f), c); }
                return cv.Tex();
            });
            public static Material Soup() => Lit("soup", SoupTex(), Color.white, 0.82f);

            // ---------- characters ----------
            /// <summary>A soft vinyl-like skin: gentle mottling, a bit darker below (sphere v = 0 at the bottom).</summary>
            public static Material Body(string hex, float smooth)
            {
                var tex = T("body" + hex, () =>
                {
                    var b = Hex(hex);
                    int seed = hex.GetHashCode() & 0xFFFF;
                    return Px(128, 64, (u, v) =>
                    {
                        float n = N(u, v, 4, seed), f = N(u, v, 16, seed + 7, 2);
                        float g = Mathf.Lerp(0.8f, 1.04f, Sm(0.02f, 0.7f, v));
                        var c = Mul(b, g * (0.95f + 0.09f * n + 0.04f * f));
                        c = Color.Lerp(c, new Color(b.r * 1.04f, b.g * 0.97f, b.b * 0.92f), (n - 0.5f) * 0.6f);   // a hint of warm/cool
                        c.a = 1;
                        return c;
                    });
                });
                return Lit("body" + hex + smooth, tex, Color.white, smooth);
            }

            /// <summary>Onion skin: thin wavy stripes along the meridians, papery light patches, browner at the ends.</summary>
            public static Texture2D OnionTex() => T("onion", () => Px(512, 256, (u, v) =>
            {
                float n = N(u, v, 6, 421), n2 = N(u, v, 12, 422, 3);
                var c = Color.Lerp(Hex("E3BC7E"), Hex("F7E6C4"), Mathf.Sin(v * Mathf.PI) * 0.9f + (n - 0.5f) * 0.3f);
                float s1 = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(2 * Mathf.PI * (u * 34 + (n - 0.5f) * 0.9f)), 22);
                float s2 = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(2 * Mathf.PI * (u * 97 + (n2 - 0.5f) * 1.4f)), 10);
                float pole = Mathf.Max(Sm(0.3f, 0.02f, v), Sm(0.78f, 0.99f, v));
                c = Color.Lerp(c, Hex("B88A4E"), (s1 * 0.55f + s2 * 0.18f) * (0.5f + 0.5f * pole + 0.3f));
                c = Color.Lerp(c, Hex("FFF7E6"), Sm(0.62f, 0.8f, n2) * 0.5f);            // papery peel
                c = Color.Lerp(c, Hex("B57A3E"), pole * 0.55f);                          // root / neck ends
                c = Color.Lerp(c, Hex("9C6A38"), Sm(0.66f, 0.82f, N(u, v, 8, 423)) * Sm(0.35f, 0.1f, v) * 0.6f);
                c.a = 1;
                return c;
            }));
            public static Material Onion() => Lit("onion", OnionTex(), Color.white, 0.62f);

            public static Material Sclera => M("F8F6F1", 0.86f);
            public static Material Iris(string hex) => M(hex, 0.92f);
            public static Material Pupil => M("0B0B12", 0.95f);
            public static Material Catch => G("FFFFFF", 1.25f);

            // ---------- sky ----------
            public static Material SkyMat()
            {
                if (mats.TryGetValue("sky", out var m) && m) return m;
                var b = Resources.Load<Material>("Materials/Sky");
                if (b == null) return null;
                m = new Material(b) { name = "cartoon_sky" };
                m.SetColor("_SkyTint", Hex("62A6EE"));
                m.SetColor("_GroundColor", Hex("C4DCEA"));
                m.SetFloat("_AtmosphereThickness", 0.82f);
                m.SetFloat("_Exposure", 1.22f);
                m.SetFloat("_SunSize", 0.045f);
                m.SetFloat("_SunSizeConvergence", 6f);
                mats["sky"] = m;
                return m;
            }

            public static Material Cloud() => Mt("cloud", () => Mats.Emissive(null, Hex("FFFFFF"), Hex("6E7F98") * 0.55f, null, 0.05f));

            // ---------- the meadow ----------
            /// <summary>Grass blades: u picks one of four greens, v runs from the dark base to the light tip.</summary>
            public static Texture2D BladeTex() => T("blade", () =>
            {
                string[] baseC = { "2D6A1E", "3B7422", "26603A", "4A7A1E" }, tipC = { "9ED65A", "C4DE6A", "86CC6E", "B8D24E" };
                return Px(16, 64, (u, v) =>
                {
                    int k = Mathf.Min(3, (int)(u * 4));
                    var c = Color.Lerp(Hex(baseC[k]), Hex(tipC[k]), Sm(0f, 1f, v));
                    c.a = 1; return c;
                }, true, false);
            });
            public static Material Blades() => TwoSided("blades", BladeTex(), 0.22f);

            /// <summary>Flower colours: 0 pink, 1 yellow, 2 white, 3 violet, 4 orange, 5 light blue, 6 centre yellow, 7 centre brown.</summary>
            public static Texture2D PetalTex() => T("petal", () =>
            {
                string[] c = { "FF6FAE", "FFE14D", "FFFFFF", "B47BFF", "FF8A4A", "7FD3FF", "FFC21A", "8A5A20" };
                var t = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
                var px = new Color[8];
                for (int i = 0; i < 8; i++) px[i] = Hex(c[i]);
                t.SetPixels(px); t.Apply(false, true);
                return t;
            });
            public static Material Petals() => TwoSided("petals", PetalTex(), 0.35f);

            /// <summary>Round soft blot for meadow colour patches.</summary>
            public static Material Patch(string hex, float a) => Clear("patch" + hex + a, Tex.Glow(), new Color(Hex(hex).r, Hex(hex).g, Hex(hex).b, a), 0.05f);
        }

        // ================= a tiny mesh builder =================
        class MB
        {
            public readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector3> n = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> t = new List<int>();

            public int V(Vector3 p, Vector3 nn, Vector2 u) { v.Add(p); n.Add(nn); uv.Add(u); return v.Count - 1; }
            /// <summary>Triangle facing <paramref name="want"/> (same winding rule as <see cref="Geo"/>).</summary>
            public void Tri(int a, int b, int c, Vector3 want)
            {
                if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), want) >= 0) { t.Add(a); t.Add(b); t.Add(c); }
                else { t.Add(a); t.Add(c); t.Add(b); }
            }

            public GameObject Build(string name, Transform parent, Material m, bool cast)
            {
                if (v.Count == 0) return null;
                var mesh = new Mesh { name = "Cartoon " + name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
                mesh.RecalculateBounds();
                madeMeshes.Add(mesh);
                var go = new GameObject(name) { layer = Layers.Preview };
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
                return go;
            }
        }

        partial class Show
        {
            DepthOfField dof;
            Material riverMat;
            Vector3 chimTop;
            float smokeT;
            readonly List<Vector3> pathPts = new List<Vector3>();

            static Vector3 V(float x, float z) => new Vector3(x, 0, z);

            /// <summary>The dirt path: house → past the field → the chase → river; river → carousel; carousel → haystack.</summary>
            static readonly Vector3[][] Paths =
            {
                new[] { V(-7.6f, 5.0f), V(-6.4f, 2.8f), V(-3.8f, 1.7f), V(-0.5f, 1.25f), V(3f, 1.45f), V(7f, 1.55f), V(11f, 1.3f), V(14f, 0.2f),
                        V(17f, -1.9f), V(21f, -4f), V(25.5f, -6f), V(30f, -7.6f), V(33.8f, -8.1f) },
                new[] { V(38.2f, -7.7f), V(41.5f, -7.3f), V(44.5f, -7.8f), V(47f, -8.1f) },
                new[] { V(57f, -7.3f), V(59.6f, -5.6f), V(61.6f, -3.4f), V(63f, -0.9f), V(63.3f, 0.5f) },
            };

            static Vector3 CR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                float t2 = t * t, t3 = t2 * t;
                return 0.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
            }

            static List<Vector3> Smooth(Vector3[] k)
            {
                var res = new List<Vector3>();
                for (int i = 0; i < k.Length - 1; i++)
                {
                    var p0 = k[Mathf.Max(0, i - 1)]; var p1 = k[i]; var p2 = k[i + 1]; var p3 = k[Mathf.Min(k.Length - 1, i + 2)];
                    int steps = Mathf.Max(2, Mathf.CeilToInt((p2 - p1).magnitude / 0.5f));
                    for (int s = 0; s < steps; s++) res.Add(CR(p0, p1, p2, p3, (float)s / steps));
                }
                res.Add(k[k.Length - 1]);
                return res;
            }

            float PathDist(Vector3 p)
            {
                float best = 1e9f;
                for (int i = 0; i < pathPts.Count; i++)
                {
                    float dx = pathPts[i].x - p.x, dz = pathPts[i].z - p.z;
                    float d = dx * dx + dz * dz;
                    if (d < best) best = d;
                }
                return Mathf.Sqrt(best);
            }

            /// <summary>Is this spot open meadow (not the field, river, path, house, carousel or haystack)?</summary>
            bool Meadow(Vector3 p, float pad = 0)
            {
                if (p.x > 2.2f - pad && p.x < 13.8f + pad && p.z > 2.4f - pad && p.z < 9.6f + pad) return false;
                if (Mathf.Abs(p.x - 36) < 4.4f + pad) return false;
                if ((p - CAR).magnitude < 6.0f + pad || (p - HAY).magnitude < 2.6f + pad) return false;
                if ((p - new Vector3(-8, 0, 7)).magnitude < 2.9f + pad) return false;
                if ((p - P0).magnitude < 1.5f + pad) return false;
                return PathDist(p) > 0.95f + pad;
            }

            // ---------- light, sky, post-processing ----------
            void Lights(Light sun)
            {
                sun.transform.localRotation = Quaternion.Euler(42, -38, 0);
                sun.color = Hex("FFE6C0"); sun.intensity = 1.38f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.78f;
                sun.cullingMask = 1 << Layers.Preview;
                // a cool fill from behind the actors: soft rim light, no shadows
                var fg = new GameObject("CartoonFill") { layer = Layers.Preview };
                fg.transform.SetParent(W, false);
                fg.transform.localRotation = Quaternion.Euler(26, 150, 0);
                var fill = fg.AddComponent<Light>();
                fill.type = LightType.Directional; fill.color = Hex("A9C9FF"); fill.intensity = 0.4f;
                fill.shadows = LightShadows.None; fill.cullingMask = 1 << Layers.Preview;
            }

            void SkyAndFx(UniversalAdditionalCameraData cd)
            {
                var sky = Look.SkyMat();
                if (sky != null)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.gameObject.AddComponent<Skybox>().material = sky;
                }
                cam.farClipPlane = 900;
                cd.volumeLayerMask = 1 << Layers.Preview;   // only the cartoon's own Volume (the game's stays untouched)
                if (!cd.renderPostProcessing) return;
                var go = new GameObject("CartoonFx") { layer = Layers.Preview };
                go.transform.SetParent(W, false);
                var vol = go.AddComponent<Volume>();
                vol.isGlobal = true; vol.priority = 50;
                var p = fxProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                p.name = "CartoonFx";
                var bloom = p.Add<Bloom>(true);
                bloom.threshold.Override(Application.isMobilePlatform ? 0.9f : 1.05f);   // phones render LDR
                bloom.intensity.Override(0.45f);
                bloom.scatter.Override(0.7f);
                bloom.highQualityFiltering.Override(false);
                var tm = p.Add<Tonemapping>(true);
                tm.mode.Override(TonemappingMode.Neutral);
                var ca = p.Add<ColorAdjustments>(true);
                ca.postExposure.Override(0.06f);
                ca.contrast.Override(12f);
                ca.saturation.Override(14f);
                var wb = p.Add<WhiteBalance>(true);
                wb.temperature.Override(6f);
                var st = p.Add<SplitToning>(true);
                st.shadows.Override(Hex("4F6A8F"));
                st.highlights.Override(Hex("FFD9A8"));
                st.balance.Override(10f);
                var vig = p.Add<Vignette>(true);
                vig.intensity.Override(0.24f);
                vig.smoothness.Override(0.45f);
                if (!Application.isMobilePlatform)
                {
                    dof = p.Add<DepthOfField>(true);
                    dof.mode.Override(DepthOfFieldMode.Gaussian);
                    dof.gaussianStart.Override(12f);
                    dof.gaussianEnd.Override(60f);
                    dof.gaussianMaxRadius.Override(0.8f);
                    dof.highQualitySampling.Override(false);
                }
                vol.profile = p;
            }

            /// <summary>Background blur starts a little behind whatever the camera looks at.</summary>
            void FocusDof()
            {
                if (dof == null) return;
                float d = (camLook - camPos).magnitude;
                dof.gaussianStart.value = d + 5f + d * 0.25f;
                dof.gaussianEnd.value = d + 40f + d * 1.2f;
            }

            // ---------- ground, hills, clouds ----------
            void Ground(Rng r)
            {
                var g = new Geo();
                g.Box(Look.Grass(), new Vector3(25, -0.5f, 0), new Vector3(1600, 1, 1600), Quaternion.identity, 14f);
                Keep(g.Build("Ground", W, false, Layers.Preview));
                // big soft colour patches: the meadow is not one flat green (three merged meshes)
                var patches = new[] { new MB(), new MB(), new MB() };
                for (int k = 0; k < 28; k++)
                {
                    var p = new Vector3(r.F(-30, 85), 0.004f, r.F(-30, 30));
                    float s = r.F(5, 14);
                    bool light = k % 3 != 0;
                    var mb = patches[light ? k % 2 : 2];
                    int a = mb.V(p + new Vector3(-s, 0, -s), Vector3.up, new Vector2(0, 0)), b = mb.V(p + new Vector3(s, 0, -s), Vector3.up, new Vector2(1, 0));
                    int c = mb.V(p + new Vector3(s, 0, s), Vector3.up, new Vector2(1, 1)), d = mb.V(p + new Vector3(-s, 0, s), Vector3.up, new Vector2(0, 1));
                    mb.Tri(a, b, c, Vector3.up); mb.Tri(a, c, d, Vector3.up);
                }
                patches[0].Build("Patches", W, Look.Patch("B9D86A", 0.32f), false);
                patches[1].Build("Patches", W, Look.Patch("C8D978", 0.32f), false);
                patches[2].Build("Patches", W, Look.Patch("2F6A22", 0.28f), false);
                // hills (textured, softly fogged far away) with little trees on them
                var hills = new[]
                {
                    (new Vector3(-40, -20, 80), new Vector3(110, 60, 70), "6CC94A"), (new Vector3(30, -25, 95), new Vector3(130, 70, 70), "5DBB45"),
                    (new Vector3(95, -22, 70), new Vector3(100, 55, 60), "79D35A"), (new Vector3(-95, -15, 40), new Vector3(80, 45, 80), "66C24C"),
                    (new Vector3(60, -18, -110), new Vector3(140, 50, 60), "6CC94A"), (new Vector3(-70, -16, -90), new Vector3(120, 48, 70), "62BE48"),
                    (new Vector3(150, -20, -10), new Vector3(90, 52, 120), "70C850"),
                };
                var far = new Geo();
                foreach (var (c, sc, hex) in hills)
                {
                    var hc = Hex(hex);
                    NoShadow(S(W, c, sc, Look.Hill(hex, Color.Lerp(Color.white, hc * 1.25f, 0.55f))));
                    float rx = sc.x / 2, ry = sc.y / 2, rz = sc.z / 2;
                    for (int k = 0; k < 14; k++)
                    {
                        float x = r.F(-0.8f, 0.8f), z = r.F(-0.8f, 0.8f);
                        float q = x * x + z * z;
                        if (q > 0.7f) continue;
                        float y = c.y + ry * Mathf.Sqrt(1 - q);
                        if (y < 1) continue;
                        FarTree(far, new Vector3(c.x + x * rx, y - 0.4f, c.z + z * rz), r.F(1.3f, 2.2f), r);
                    }
                }
                // a ring of trees around the valley
                for (int k = 0; k < 70; k++)
                {
                    float a = r.F() * Mathf.PI * 2, d = r.F(68, 150);
                    var p = new Vector3(25 + Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d * 0.8f);
                    if (Mathf.Abs(p.x - 36) < 7 && p.z > -80 && p.z < 70) continue;
                    FarTree(far, p, r.F(1.1f, 1.9f), r);
                }
                Keep(far.Build("FarTrees", W, false, Layers.Preview));
            }

            static void FarTree(Geo g, Vector3 p, float s, Rng r)
            {
                g.Cyl(Look.Bark(), p, 0.3f * s, 2.2f * s, 6, 0.2f * s, false, 1.5f);
                var ball = Meshes.Ball(8, 5);
                for (int i = 0; i < 4; i++)
                {
                    var o = new Vector3(r.F(-0.9f, 0.9f), 2.6f + r.F(0, 1.6f), r.F(-0.9f, 0.9f)) * s;
                    float d = r.F(2.0f, 3.0f) * s;
                    g.Msh(Look.Leaf(i == 3 ? 2 : i % 2), ball, Matrix4x4.TRS(p + o, Quaternion.Euler(0, r.F(0, 360), 0), new Vector3(d, d * 0.9f, d)));
                }
            }

            void Clouds(Rng r)
            {
                var ball = Meshes.Ball(12, 8);
                for (int k = 0; k < 16; k++)
                {
                    float a = (k / 16f + r.F(-0.02f, 0.02f)) * Mathf.PI * 2, d = r.F(110, 210);
                    var pos = new Vector3(25 + Mathf.Sin(a) * d, r.F(34, 62), Mathf.Cos(a) * d);
                    if (k < 6) pos = new Vector3(-70 + k * 30 + r.F(-6, 6), r.F(26, 42), r.F(70, 125));    // the ones behind the valley
                    var c = Node("Cloud", W, pos, new Vector3(0, r.F(0, 360), 0));
                    var g = new Geo();
                    float len = r.F(9, 16);
                    int n = 9 + r.I(5);
                    for (int i = 0; i < n; i++)
                    {
                        float t = (float)i / (n - 1) - 0.5f;
                        float big = 1 - Mathf.Abs(t) * 1.1f;
                        var o = new Vector3(t * len, r.F(0, 1.6f) * big + 0.6f, r.F(-1.6f, 1.6f));
                        float s = r.F(3.2f, 5.6f) * (0.55f + big * 0.6f);
                        g.Msh(Look.Cloud(), ball, Matrix4x4.TRS(o, Quaternion.identity, new Vector3(s, s * 0.78f, s)));
                    }
                    // a flat-ish bottom
                    g.Msh(Look.Cloud(), ball, Matrix4x4.TRS(new Vector3(0, 0.2f, 0), Quaternion.identity, new Vector3(len * 0.9f, 1.6f, 4.2f)));
                    Keep(g.Build("Puffs", c, false, Layers.Preview));
                    clouds.Add(c);
                }
            }

            // ---------- the dirt path, pebbles, the meadow (grass tufts + flowers) ----------
            void PathsAndMeadow(Rng r)
            {
                pathPts.Clear();
                var mb = new MB();
                foreach (var k in Paths)
                {
                    var pts = Smooth(k);
                    pathPts.AddRange(pts);
                    float total = 0;
                    for (int i = 1; i < pts.Count; i++) total += (pts[i] - pts[i - 1]).magnitude;
                    float run = 0;
                    for (int i = 0; i < pts.Count; i++)
                    {
                        if (i > 0) run += (pts[i] - pts[i - 1]).magnitude;
                        var dir = (pts[Mathf.Min(pts.Count - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                        var side = new Vector3(-dir.z, 0, dir.x);
                        float w = 0.8f * Mathf.Min(1, Mathf.Min(run, total - run) / 1.6f + 0.25f) * (1 + 0.12f * Mathf.Sin(run * 0.7f));
                        var c = pts[i] + Vector3.up * 0.012f;
                        int a = mb.V(c + side * w, Vector3.up, new Vector2(run / 4f, 0));
                        int b = mb.V(c - side * w, Vector3.up, new Vector2(run / 4f, 1));
                        if (i > 0) { mb.Tri(a - 2, b - 2, b, Vector3.up); mb.Tri(a - 2, b, a, Vector3.up); }
                    }
                }
                mb.Build("Path", W, Look.Path(), false);

                // pebbles along the path and the river banks, a few in the grass
                var g = new Geo();
                var ball = Meshes.Ball(8, 5);
                void Pebble(Vector3 p, float s)
                {
                    var sc = new Vector3(r.F(0.7f, 1.3f), r.F(0.35f, 0.6f), r.F(0.7f, 1.3f)) * s;
                    g.Msh(Look.Stone(r.I(3)), ball, Matrix4x4.TRS(p + Vector3.up * sc.y * 0.2f, Quaternion.Euler(r.F(-10, 10), r.F(0, 360), r.F(-10, 10)), sc));
                }
                for (int i = 0; i < pathPts.Count; i += 2)
                {
                    if (!r.Chance(0.55f)) continue;
                    int j = Mathf.Min(pathPts.Count - 1, i + 1);
                    var dir = pathPts[j] - pathPts[i]; dir.y = 0;
                    if (dir.sqrMagnitude < 1e-4f) continue;
                    var side = new Vector3(-dir.z, 0, dir.x).normalized * (r.Chance(0.5f) ? 1 : -1);
                    Pebble(pathPts[i] + side * r.F(0.55f, 1.2f), r.F(0.1f, 0.24f));
                }
                for (float z = -40; z < 30; z += 0.55f)
                    for (int s = -1; s <= 1; s += 2)
                        if (r.Chance(0.6f)) Pebble(new Vector3(36 + s * r.F(2.3f, 3.6f), 0, z + r.F(-0.2f, 0.2f)), r.F(0.1f, 0.32f));
                for (int k = 0; k < 70; k++)
                {
                    var p = new Vector3(r.F(-22, 78), 0, r.F(-22, 24));
                    if (Meadow(p)) Pebble(p, r.F(0.12f, 0.3f));
                }
                Keep(g.Build("Pebbles", W, false, Layers.Preview));

                // grass tufts: clumps near where the camera looks, a sprinkle everywhere else
                var blades = new MB();
                var heads = new MB();
                void Tuft(Vector3 p, float h)
                {
                    int nb = 5 + r.I(5);
                    float col = (r.I(4) + 0.5f) / 4f;
                    for (int i = 0; i < nb; i++)
                    {
                        float a = r.F() * Mathf.PI * 2;
                        var bp = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r.F(0, 0.09f);
                        float bw = r.F(0.018f, 0.032f), bh = h * r.F(0.55f, 1.1f);
                        float face = r.F() * Mathf.PI * 2;
                        var across = new Vector3(Mathf.Cos(face), 0, Mathf.Sin(face));
                        var lean = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * bh * r.F(0.15f, 0.5f);
                        var nrm = (Vector3.up * 2 + new Vector3(-across.z, 0, across.x) * 0.4f).normalized;
                        int i0 = blades.V(bp - across * bw, nrm, new Vector2(col, 0));
                        int i1 = blades.V(bp + across * bw, nrm, new Vector2(col, 0));
                        int i2 = blades.V(bp + lean + Vector3.up * bh, nrm, new Vector2(col, 1));
                        blades.Tri(i0, i1, i2, new Vector3(-across.z, 0, across.x));
                    }
                }
                void Flower(Vector3 p)
                {
                    float h = r.F(0.2f, 0.4f);
                    var top = p + new Vector3(r.F(-0.04f, 0.04f), h, r.F(-0.04f, 0.04f));
                    // stem (a thin blade) and two leaves
                    var nrm = Vector3.up;
                    int s0 = blades.V(p + new Vector3(-0.008f, 0, 0), nrm, new Vector2(0.125f, 0)), s1 = blades.V(p + new Vector3(0.008f, 0, 0), nrm, new Vector2(0.125f, 0));
                    int s2 = blades.V(top, nrm, new Vector2(0.125f, 0.7f));
                    blades.Tri(s0, s1, s2, Vector3.back);
                    int colour = r.I(6);
                    float pl = r.F(0.045f, 0.075f);
                    int np = colour == 2 ? 8 : 5;
                    float tilt = r.F(0.15f, 0.45f), rot = r.F() * Mathf.PI * 2;
                    var pu = new Vector2((colour + 0.5f) / 8f, 0.5f);
                    for (int i = 0; i < np; i++)
                    {
                        float a = rot + i * Mathf.PI * 2 / np;
                        var d = new Vector3(Mathf.Cos(a), tilt, Mathf.Sin(a)).normalized;
                        var sd = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * pl * (np == 8 ? 0.22f : 0.38f);
                        var hn = (Vector3.up * 2 + d).normalized;
                        int a0 = heads.V(top, hn, pu), a1 = heads.V(top + d * pl * 0.5f + sd, hn, pu), a2 = heads.V(top + d * pl, hn, pu), a3 = heads.V(top + d * pl * 0.5f - sd, hn, pu);
                        heads.Tri(a0, a1, a2, Vector3.up); heads.Tri(a0, a2, a3, Vector3.up);
                    }
                    var cu = new Vector2((colour == 1 ? 7.5f : 6.5f) / 8f, 0.5f);
                    float cs = pl * 0.28f;
                    int c0 = heads.V(top + Vector3.up * cs * 0.9f, Vector3.up, cu);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = rot + i * Mathf.PI / 2, b = a + Mathf.PI / 2;
                        int e0 = heads.V(top + new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * cs, Vector3.up, cu);
                        int e1 = heads.V(top + new Vector3(Mathf.Cos(b), 0.15f, Mathf.Sin(b)) * cs, Vector3.up, cu);
                        heads.Tri(c0, e0, e1, Vector3.up);
                    }
                }
                Vector3[] hot =
                {
                    new Vector3(0, 0, 0.5f), new Vector3(-3, 0, 3), new Vector3(3, 0, -1.5f), new Vector3(-4, 0, -6), new Vector3(1.5f, 0, -6.5f),
                    new Vector3(-1, 0, 6), new Vector3(15, 0, 3), new Vector3(16, 0, -5), new Vector3(22, 0, -1), new Vector3(26, 0, -10),
                    new Vector3(30, 0, -4), new Vector3(42, 0, -3), new Vector3(43, 0, -12), new Vector3(58, 0, 1), new Vector3(61, 0, -9),
                    new Vector3(66, 0, -3), new Vector3(57, 0, 6), new Vector3(68, 0, 7), new Vector3(-12, 0, 3), new Vector3(10, 0, -4),
                };
                foreach (var hc in hot)
                    for (int k = 0; k < 9; k++)
                    {
                        var cc = hc + new Vector3(r.F(-5, 5), 0, r.F(-5, 5));
                        int n = 6 + r.I(10);
                        for (int i = 0; i < n; i++)
                        {
                            var p = cc + new Vector3(r.F(-1.2f, 1.2f), 0, r.F(-1.2f, 1.2f));
                            if (Meadow(p, 0.1f)) Tuft(p, r.F(0.14f, 0.3f));
                        }
                    }
                for (int k = 0; k < 1100; k++)
                {
                    var p = new Vector3(r.F(-24, 80), 0, r.F(-26, 26));
                    if (Meadow(p, 0.1f)) Tuft(p, r.F(0.1f, 0.26f));
                }
                for (int k = 0; k < 60; k++)
                {
                    var cc = hot[r.I(hot.Length)] + new Vector3(r.F(-6, 6), 0, r.F(-6, 6));
                    if (k % 3 == 0) cc = new Vector3(r.F(-22, 78), 0, r.F(-22, 22));
                    int n = 2 + r.I(5);
                    for (int i = 0; i < n; i++)
                    {
                        var p = cc + new Vector3(r.F(-0.8f, 0.8f), 0, r.F(-0.8f, 0.8f));
                        if (Meadow(p, 0.15f)) Flower(p);
                    }
                }
                blades.Build("Grass", W, Look.Blades(), false);
                heads.Build("Flowers", W, Look.Petals(), false);
            }

            // ---------- trees and bushes (one merged mesh per material, casting shadows) ----------
            void Trees(Rng r, List<(Vector3 p, float s)> trees)
            {
                var g = new Geo();
                var ball = Meshes.Ball(10, 7);
                var t1 = trees[0].p;
                for (int i = 0; i < trees.Count; i++)
                    TreeGeo(g, ball, trees[i].p, trees[i].s, r, i == 0 ? t1 + new Vector3(1.0f, 2.2f, -0.2f) : (Vector3?)null, t1 + new Vector3(2.5f, 2.25f, -0.2f));
                // the bird branch on the first tree (birds sit on it: keep its line)
                g.Rod(Look.Bark(), t1 + new Vector3(0.2f, 1.8f, 0), t1 + new Vector3(2.65f, 2.0f, -0.3f), 0.1f, 8, 0.06f, 1.2f);
                for (int i = 0; i < 4; i++)
                    g.Msh(Look.Leaf(i % 3), ball, Matrix4x4.TRS(t1 + new Vector3(2.75f, 2.2f, -0.3f) + new Vector3(r.F(-0.25f, 0.25f), r.F(0, 0.2f), r.F(-0.25f, 0.25f)), Quaternion.Euler(0, r.F(0, 360), 0), Vector3.one * r.F(0.3f, 0.45f)));
                // bushes (the one at 57.5 / 1 is Чихуня's landing spot: top ≈ 1.2)
                foreach (var b in new[] { new Vector3(57.5f, 0, 1), new Vector3(-11, 0, 2), new Vector3(15, 0, 9), new Vector3(44, 0, 3), new Vector3(68, 0, -4),
                                          new Vector3(-11.4f, 0, 9.4f), new Vector3(-5.6f, 0, 9.8f), new Vector3(18, 0, -9), new Vector3(28, 0, 6), new Vector3(31, 0, -14),
                                          new Vector3(48, 0, -16), new Vector3(70, 0, 12), new Vector3(-17, 0, -5), new Vector3(6, 0, -14), new Vector3(24, 0, 14) })
                    BushGeo(g, ball, b, (b - new Vector3(57.5f, 0, 1)).sqrMagnitude < 0.01f ? 1f : r.F(0.8f, 1.3f), r);
                Keep(g.Build("Trees", W, true, Layers.Preview));
            }

            /// <summary>A tree; no leafy lump may cover the segment <paramref name="keepA"/>–<paramref name="keepB"/> (where the birds sit).</summary>
            static void TreeGeo(Geo g, Mesh ball, Vector3 p, float s, Rng r, Vector3? keepA = null, Vector3 keepB = default)
            {
                var bark = Look.Bark();
                g.Cyl(bark, p, 0.32f * s, 2.6f * s, 12, 0.19f * s, false, 1.2f);
                for (int k = 0; k < 5; k++)
                {
                    float a = (k * 72 + r.F(-15, 15)) * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    g.Rod(bark, p + d * 0.12f * s + Vector3.up * 0.42f * s, p + d * 0.55f * s + Vector3.up * 0.01f, 0.13f * s, 6, 0.04f * s, 1.2f);
                }
                for (int k = 0; k < 3; k++)
                {
                    float a = (k * 120 + r.F(0, 40)) * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    g.Rod(bark, p + Vector3.up * (1.7f + k * 0.25f) * s, p + (d * 1.0f + Vector3.up * (2.7f + k * 0.2f)) * s, 0.09f * s, 6, 0.04f * s, 1.2f);
                }
                // the old crown: four big blobs. Each is now a dark core covered by many small leafy lumps
                var blobs = new[] { (new Vector3(0, 3.0f, 0), 2.4f), (new Vector3(0.8f, 2.5f, 0.3f), 1.6f), (new Vector3(-0.7f, 2.6f, -0.2f), 1.7f), (new Vector3(0.1f, 3.9f, 0.1f), 1.4f) };
                foreach (var (c0, d0) in blobs)
                {
                    var c = p + c0 * s;
                    float d = d0 * s;
                    g.Msh(Look.Leaf(0), ball, Matrix4x4.TRS(c, Quaternion.Euler(0, r.F(0, 360), 0), Vector3.one * d * 0.86f));
                    int n = Mathf.RoundToInt(d0 * 5);
                    for (int i = 0; i < n; i++)
                    {
                        var dir = new Vector3(r.F(-1, 1), r.F(-1, 1), r.F(-1, 1));
                        if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
                        dir = new Vector3(dir.x, dir.y * 0.8f + 0.25f * dir.magnitude, dir.z).normalized;
                        float ld = d * r.F(0.36f, 0.52f);
                        int m = dir.y > 0.45f ? (r.Chance(0.25f) ? 3 : 2) : dir.y < -0.15f ? 0 : (r.Chance(0.3f) ? 2 : 1);
                        var lc = c + dir * d * 0.36f;
                        if (keepA.HasValue)
                        {
                            var ab = keepB - keepA.Value;
                            var q = keepA.Value + ab * Mathf.Clamp01(Vector3.Dot(lc - keepA.Value, ab) / ab.sqrMagnitude);
                            if ((lc - q).magnitude < ld * 0.5f + 0.45f) continue;
                        }
                        g.Msh(Look.Leaf(m), ball, Matrix4x4.TRS(lc, Quaternion.Euler(r.F(0, 360), r.F(0, 360), 0), new Vector3(ld, ld * 0.85f, ld)));
                    }
                }
            }

            static void BushGeo(Geo g, Mesh ball, Vector3 p, float s, Rng r)
            {
                g.Msh(Look.Leaf(0), ball, Matrix4x4.TRS(p + Vector3.up * 0.5f * s, Quaternion.identity, new Vector3(1.6f, 1.1f, 1.6f) * s));
                int n = 9;
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2 / n + r.F(-0.3f, 0.3f);
                    float up = i % 3 == 0 ? 0.35f : 0;
                    var o = new Vector3(Mathf.Cos(a) * 0.55f, 0.5f + up + r.F(0, 0.2f), Mathf.Sin(a) * 0.55f) * s;
                    float ld = r.F(0.65f, 0.9f) * s;
                    g.Msh(Look.Leaf(up > 0 ? 2 : r.I(2) + (r.Chance(0.2f) ? 2 : 0)), ball, Matrix4x4.TRS(p + o, Quaternion.Euler(0, r.F(0, 360), 0), new Vector3(ld, ld * 0.8f, ld)));
                }
                g.Msh(Look.Leaf(2), ball, Matrix4x4.TRS(p + Vector3.up * 0.88f * s, Quaternion.identity, new Vector3(0.9f, 0.6f, 0.9f) * s));
            }

            // ---------- the little house ----------
            void House(Transform h)
            {
                var g = new Geo();
                var walls = Look.Planks("E2B270", 0.12f);
                var dark = Look.Planks("8A5A33", 0.2f);
                g.Box(Look.Stone(1), new Vector3(0, 0.15f, 0), new Vector3(3.36f, 0.3f, 3.16f), Quaternion.identity, 1.2f);
                g.Box(walls, new Vector3(0, 1.4f, 0), new Vector3(3.2f, 2.2f, 3f), Quaternion.identity, 2.2f);
                // gables
                var tri = new[] { new Vector2(-1.5f, 2.5f), new Vector2(1.5f, 2.5f), new Vector2(0, 3.8f) };
                for (int sd = -1; sd <= 1; sd += 2)
                    g.Extrude(walls, tri, 0.1f, Matrix4x4.TRS(new Vector3(sd * 1.55f, 0, 0), Quaternion.Euler(0, 90, 0), Vector3.one), 0.45f);
                // the roof: two tiled planes (v runs down the slope on both), a ridge
                float ang = Mathf.Atan2(1.3f, 1.5f) * Mathf.Rad2Deg, run = 1.85f, len = run / Mathf.Cos(ang * Mathf.Deg2Rad), eaveY = 3.8f - run * 1.3f / 1.5f;
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    var rot = Quaternion.Euler(ang, sd > 0 ? 0 : 180, 0);
                    var c = new Vector3(0, (3.8f + eaveY) / 2, sd * run / 2) + rot * Vector3.up * 0.07f;
                    g.Box(Look.Roof(), c, new Vector3(3.9f, 0.14f, len), rot, 1.6f, true);
                }
                g.Rod(Look.Roof(), new Vector3(-1.98f, 3.86f, 0), new Vector3(1.98f, 3.86f, 0), 0.11f, 10, -1, 1.6f);
                // door, knob, step
                g.Box(dark, new Vector3(0.55f, 0.95f, -1.52f), new Vector3(0.9f, 1.6f, 0.08f), Quaternion.identity, 1.2f);
                g.Box(Look.Stone(0), new Vector3(0.55f, 0.06f, -1.85f), new Vector3(1.1f, 0.12f, 0.5f), Quaternion.identity, 1f);
                g.Msh(M("FFC83A", 0.75f, 0.7f), Meshes.Ball(), Matrix4x4.TRS(new Vector3(0.85f, 0.95f, -1.58f), Quaternion.identity, Vector3.one * 0.12f));
                // windows: a dark room behind glass, a white frame and a cross, a flower box
                foreach (var (wc, yaw) in new[] { (new Vector3(-0.8f, 1.55f, -1.5f), 0f), (new Vector3(1.6f, 1.55f, 0.2f), -90f), (new Vector3(-1.6f, 1.55f, -0.2f), 90f) })
                {
                    var q = Quaternion.Euler(0, yaw, 0);
                    var f = q * Vector3.back;
                    g.Box(M("3A4A5A", 0.3f), wc + f * 0.01f, new Vector3(0.74f, 0.74f, 0.04f), q, 1f);
                    g.Box(M("FFFFFF", 0.35f), wc + f * 0.04f, new Vector3(0.84f, 0.08f, 0.05f), q, 1f);
                    g.Box(M("FFFFFF", 0.35f), wc + f * 0.04f, new Vector3(0.08f, 0.84f, 0.05f), q, 1f);
                    g.Box(M("FFFFFF", 0.35f), wc + f * 0.04f + Vector3.up * 0.42f, new Vector3(0.9f, 0.07f, 0.06f), q, 1f);
                    g.Box(M("FFFFFF", 0.35f), wc + f * 0.04f - Vector3.up * 0.42f, new Vector3(0.9f, 0.07f, 0.06f), q, 1f);
                    g.Box(dark, wc + f * 0.14f - Vector3.up * 0.55f, new Vector3(0.9f, 0.18f, 0.22f), q, 1f);
                    for (int i = 0; i < 5; i++)
                        g.Msh(M(i % 2 == 0 ? "FF5FA2" : "FFE14D", 0.35f), Meshes.Ball(), Matrix4x4.TRS(wc + f * 0.14f - Vector3.up * 0.42f + q * Vector3.right * (i - 2) * 0.17f, Quaternion.identity, Vector3.one * 0.13f));
                }
                // the chimney
                g.Box(Look.Brick(), new Vector3(0.9f, 3.6f, 0.5f), new Vector3(0.5f, 1.4f, 0.5f), Quaternion.identity, 0.8f);
                g.Box(Look.Stone(2), new Vector3(0.9f, 4.33f, 0.5f), new Vector3(0.64f, 0.12f, 0.64f), Quaternion.identity, 1f);
                Keep(g.Build("HouseMesh", h, true, Layers.Preview));
                // glass panes (transparent, in front of the dark room)
                foreach (var (wc, yaw) in new[] { (new Vector3(-0.8f, 1.55f, -1.5f), 0f), (new Vector3(1.6f, 1.55f, 0.2f), -90f), (new Vector3(-1.6f, 1.55f, -0.2f), 90f) })
                {
                    var q = Quaternion.Euler(0, yaw, 0);
                    var gl = B(h, wc + q * Vector3.back * 0.035f, new Vector3(0.74f, 0.74f, 0.01f), Glass("CFEFFF", 0.32f));
                    gl.transform.localRotation = q;
                    NoShadow(gl);
                }
                chimTop = W.InverseTransformPoint(h.TransformPoint(new Vector3(0.9f, 4.45f, 0.5f)));
            }

            void Smoke()
            {
                smokeT -= Dt;
                if (smokeT > 0) return;
                smokeT = 0.32f;
                Spray(chimTop, Glass("EEF2F5", 0.42f), 1, 0.08f, 0.32f, 3.4f, -0.12f, new Vector3(0.28f, 0.75f, 0.1f), 0.9f);
            }

            // ---------- the onion field: soil, ridges (furrows between) ----------
            void Field()
            {
                var g = new Geo();
                g.Box(Look.SoilDark(), new Vector3(8, 0.01f, 6), new Vector3(10.8f, 0.04f, 6.9f), Quaternion.identity, 2f);
                Keep(g.Build("FieldBed", W, false, Layers.Preview));
                var mb = new MB();
                const int seg = 8;
                for (int k = 0; k < 5; k++)
                {
                    float z0 = 3.6f + k * 1.2f, w = 0.48f, h = 0.17f;
                    float[] xs = { 2.9f, 13.1f };
                    int start = mb.v.Count;
                    for (int e = 0; e < 2; e++)
                        for (int i = 0; i <= seg; i++)
                        {
                            float a = Mathf.PI * i / seg;
                            var p = new Vector3(xs[e], 0.02f + Mathf.Sin(a) * h, z0 + Mathf.Cos(a) * w);
                            var nn = new Vector3(0, Mathf.Sin(a) / h, Mathf.Cos(a) / w).normalized;
                            mb.V(p, nn, new Vector2(xs[e] / 1.5f, (float)i / seg * 0.8f));
                        }
                    for (int i = 0; i < seg; i++)
                    {
                        int a0 = start + i, a1 = start + i + 1, b0 = start + seg + 1 + i, b1 = b0 + 1;
                        var up = Vector3.up + Vector3.forward * Mathf.Cos(Mathf.PI * (i + 0.5f) / seg);
                        mb.Tri(a0, b0, b1, up); mb.Tri(a0, b1, a1, up);
                    }
                    // rounded ends
                    for (int e = 0; e < 2; e++)
                    {
                        int c = mb.V(new Vector3(xs[e] + (e == 0 ? -0.25f : 0.25f), 0.02f, z0), e == 0 ? Vector3.left : Vector3.right, new Vector2(0, 0.4f));
                        for (int i = 0; i < seg; i++) mb.Tri(c, start + e * (seg + 1) + i, start + e * (seg + 1) + i + 1, (e == 0 ? Vector3.left : Vector3.right) + Vector3.up);
                    }
                }
                mb.Build("Ridges", W, Look.Soil(), false);
            }

            // ---------- the river: moving water, sandy banks ----------
            void River()
            {
                var mb = new MB();
                float z0 = -75, z1 = 65, x0 = 33.4f, x1 = 38.6f, y = 0.035f;
                int a = mb.V(new Vector3(x0, y, z0), Vector3.up, new Vector2(0, z0 / 6)), b = mb.V(new Vector3(x1, y, z0), Vector3.up, new Vector2(1, z0 / 6));
                int c = mb.V(new Vector3(x1, y, z1), Vector3.up, new Vector2(1, z1 / 6)), d = mb.V(new Vector3(x0, y, z1), Vector3.up, new Vector2(0, z1 / 6));
                mb.Tri(a, b, c, Vector3.up); mb.Tri(a, c, d, Vector3.up);
                riverMat = Look.Water();
                mb.Build("River", W, riverMat, false);
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    var sb = new MB();
                    float inner = 36 + sd * 2.45f, outer = 36 + sd * 4.6f;
                    int i0 = sb.V(new Vector3(inner, 0.018f, z0), Vector3.up, new Vector2(z0 / 4, 0)), i1 = sb.V(new Vector3(inner, 0.018f, z1), Vector3.up, new Vector2(z1 / 4, 0));
                    int o0 = sb.V(new Vector3(outer, 0.018f, z0), Vector3.up, new Vector2(z0 / 4, 1)), o1 = sb.V(new Vector3(outer, 0.018f, z1), Vector3.up, new Vector2(z1 / 4, 1));
                    sb.Tri(i0, o0, o1, Vector3.up); sb.Tri(i0, o1, i1, Vector3.up);
                    sb.Build("Shore", W, Look.Shore(), false);
                }
            }

            void FlowRiver()
            {
                if (!riverMat) return;
                var o = riverMat.mainTextureOffset;
                o.y -= Dt * 0.09f;
                if (o.y < -100) o.y += 100;
                riverMat.mainTextureOffset = o;
            }
        }
    }
}
