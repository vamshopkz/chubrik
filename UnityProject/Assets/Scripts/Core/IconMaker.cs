using System.Collections;
using System.IO;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Renders the app icon from the real chubrik (`-autotest -icons DIR`): a sky background, the hero waving in
    /// front of it. Writes AppIcon.png (full), AppIconBg.png + AppIconFg.png (Android adaptive icon, hero inside
    /// the safe zone), icon.png / apple-touch-icon.png for the browser version.
    /// </summary>
    public static class IconMaker
    {
        const int N = 1024;

        public static IEnumerator Make(string dir)
        {
            Directory.CreateDirectory(dir);
            var origin = new Vector3(0, -800, 0);
            var stage = new GameObject("IconStage") { layer = Layers.Icon }.transform;
            stage.position = origin;
            var look = Look.Default();
            look["hat"] = "hat_0"; look["top"] = "top_1"; look["shoes"] = "shoes_1";
            var m = ChubrikModel.Create("IconHero", stage, look, Layers.Icon);
            m.transform.localRotation = Quaternion.Euler(0, 22, 0);   // three-quarter view, facing the camera
            yield return null;
            // waving pose
            m.ArmR.localRotation = Quaternion.Euler(0, 0, 150);
            m.ArmL.localRotation = Quaternion.Euler(0, 0, -30);
            m.Head.localRotation = Quaternion.Euler(-6, 0, 6);

            var cg = new GameObject("IconCam") { layer = Layers.Icon };
            var cam = cg.AddComponent<Camera>();
            cam.cullingMask = 1 << Layers.Icon;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 26; cam.nearClipPlane = 0.1f; cam.farClipPlane = 40;
            cam.enabled = false;
            var key = new GameObject("IconKey").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.25f; key.color = new Color(1, 0.97f, 0.9f);
            key.transform.rotation = Quaternion.Euler(30, 160, 0);
            key.cullingMask = 1 << Layers.Icon;
            var rim = new GameObject("IconRim").AddComponent<Light>();
            rim.type = LightType.Directional; rim.intensity = 0.7f; rim.color = new Color(0.7f, 0.85f, 1f);
            rim.transform.rotation = Quaternion.Euler(20, -30, 0);
            rim.cullingMask = 1 << Layers.Icon;

            var bg = Background();
            // full icon: the hero big; adaptive foreground: smaller, inside the central 66 %
            var fgBig = Render(cam, origin, 3.5f);
            var fgSafe = Render(cam, origin, 5.2f);
            var full = Over(bg, fgBig);
            File.WriteAllBytes(Path.Combine(dir, "AppIcon.png"), full.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, "AppIconBg.png"), bg.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, "AppIconFg.png"), fgSafe.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, "icon.png"), Scaled(full, 256).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, "thumbnail.png"), Scaled(full, 256).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dir, "apple-touch-icon.png"), Scaled(full, 180).EncodeToPNG());
            Object.Destroy(stage.gameObject); Object.Destroy(cg); Object.Destroy(key.gameObject); Object.Destroy(rim.gameObject);
        }

        /// <summary>The hero on a transparent background; dist = camera distance (bigger = smaller hero).</summary>
        static Texture2D Render(Camera cam, Vector3 origin, float dist)
        {
            var target = origin + Vector3.up * 1.12f;
            cam.transform.position = target + new Vector3(0, 0.12f, dist);
            cam.transform.LookAt(target);
            var rt = new RenderTexture(N, N, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, N, N), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            return t;
        }

        /// <summary>Sunny sky: radial gradient, soft clouds at the bottom, a few sparkles.</summary>
        static Texture2D Background()
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            var top = Parts.Hex("2E9BFF"); var mid = Parts.Hex("7FD3FF"); var glow = Parts.Hex("FFF6C8");
            var rng = new Rng(22);
            var clouds = new Vector3[9];
            for (int i = 0; i < clouds.Length; i++) clouds[i] = new Vector3(i * N / 8f + rng.F(-40, 40), rng.F(40, 170), rng.F(110, 190));
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float v = y / (float)N;
                    var c = Color.Lerp(mid, top, Mathf.SmoothStep(0, 1, v));
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.58f)) / N;
                    c = Color.Lerp(c, glow, Mathf.Clamp01(0.55f - d * 1.3f) * 0.8f);
                    foreach (var cl in clouds)
                    {
                        float k = Vector2.Distance(new Vector2(x, y), new Vector2(cl.x, cl.y)) / cl.z;
                        if (k < 1) c = Color.Lerp(c, Color.white, Mathf.SmoothStep(0, 1, (1 - k) * 2.2f) * 0.95f);
                    }
                    px[y * N + x] = c;
                }
            for (int s = 0; s < 14; s++)
            {
                int cx = (int)rng.F(60, N - 60), cy = (int)rng.F(N * 0.55f, N - 60);
                float r = rng.F(6, 14);
                for (int y = (int)(cy - r * 3); y <= cy + r * 3; y++)
                    for (int x = (int)(cx - r * 3); x <= cx + r * 3; x++)
                    {
                        if (x < 0 || y < 0 || x >= N || y >= N) continue;
                        float dx = Mathf.Abs(x - cx), dy = Mathf.Abs(y - cy);
                        float a = Mathf.Clamp01(1 - (dx * dy) / (r * 2) - Mathf.Max(dx, dy) / (r * 3));
                        px[y * N + x] = Color.Lerp(px[y * N + x], Color.white, a);
                    }
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        static Texture2D Over(Texture2D bg, Texture2D fg)
        {
            var b = bg.GetPixels(); var f = fg.GetPixels();
            for (int i = 0; i < b.Length; i++) { var c = Color.Lerp(b[i], f[i], f[i].a); c.a = 1; b[i] = c; }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.SetPixels(b); t.Apply();
            return t;
        }

        static Texture2D Scaled(Texture2D src, int size)
        {
            // halve step by step (one big bilinear jump looks jagged)
            Texture cur = src; RenderTexture prev = null;
            int w = src.width;
            while (w / 2 >= size * 2)
            {
                w /= 2;
                var h = RenderTexture.GetTemporary(w, w, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(cur, h);
                if (prev) RenderTexture.ReleaseTemporary(prev);
                prev = h; cur = h;
            }
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(cur, rt);
            if (prev) RenderTexture.ReleaseTemporary(prev);
            RenderTexture.active = rt;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }
    }
}
