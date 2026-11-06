using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// All sounds are synthesized at start (no audio files): 8 jump sounds, 8 landing sounds, steps, somersault,
    /// punches, UI, notes and jingles — and the lobby song played by a little band: piano, plucked guitar,
    /// bass, drums and bells (event renderer with Karplus–Strong strings and a soft echo).
    /// </summary>
    public class AudioFx : MonoBehaviour
    {
        static AudioFx I;
        AudioSource src, music;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int SR = 22050;
        const float TAU = Mathf.PI * 2;
        int stepFoot;

        public static void Create()
        {
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioFx>();
            I.src = go.AddComponent<AudioSource>();
            I.music = go.AddComponent<AudioSource>();
            I.music.loop = true; I.music.volume = 0.42f;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            I.Make();
            Debug.Log($"[Audio] synthesized in {sw.ElapsedMilliseconds} ms");
            SetVolume(Save.P.volume);
            SetMusic(Save.P.music);
        }

        public static void SetVolume(float v) { AudioListener.volume = Mathf.Clamp01(v); }

        public static void SetMusic(bool on)
        {
            if (I == null) return;
            I.wantMusic = on && !Boot.Testing;
            if (I.wantMusic) { if (!I.music.isPlaying && I.clips.TryGetValue("music", out var c)) { I.music.clip = c; I.music.Play(); } }
            else I.music.Stop();
        }

        bool wantMusic;
        Task<float[]> musicTask;
        /// <summary>The song is rendered on a background thread (it takes a couple of seconds on a phone).</summary>
        public static bool MusicReady => I != null && I.clips.ContainsKey("music");

        void Update()
        {
            if (musicTask == null || !musicTask.IsCompleted) return;
            var data = musicTask.Status == TaskStatus.RanToCompletion ? musicTask.Result : null;
            musicTask = null;
            if (data == null) { Debug.LogWarning("[Audio] music failed"); return; }
            var clip = AudioClip.Create("music", data.Length, 1, SR, false);
            clip.SetData(data, 0);
            clips["music"] = clip;
            SetMusic(wantMusic);
        }

        delegate float Wave(float t);

        void Clip(string name, float len, Wave w)
        {
            int n = (int)(SR * len);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(w((float)i / SR), -1, 1);
            // tiny fade-out: no clicks at the end
            int fade = Mathf.Min(n, SR / 200);
            for (int i = 0; i < fade; i++) data[n - 1 - i] *= (float)i / fade;
            var c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(data, 0);
            clips[name] = c;
        }

        static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Exp(-(t - a) / d);
        static float Sq(float x) => Mathf.Sin(x) >= 0 ? 1 : -1;
        static float Tri(float x) { float p = x / TAU; p -= Mathf.Floor(p); return 4 * Mathf.Abs(p - 0.5f) - 1; }
        static float Bell(float t, float f) => (Mathf.Sin(t * TAU * f) + Mathf.Sin(t * TAU * f * 2.76f) * 0.35f * Mathf.Exp(-t * 8) + Mathf.Sin(t * TAU * f * 5.4f) * 0.15f * Mathf.Exp(-t * 14));

        /// <summary>Filtered noise sources with their own state (one per clip).</summary>
        class Nz
        {
            readonly System.Random r; float lp, bp1, bp2, hpPrev, hpOut;
            public Nz(int seed) { r = new System.Random(seed); }
            public float W() => (float)(r.NextDouble() * 2 - 1);
            public float Low(float k) { lp += (W() - lp) * k; return lp; }
            public float High() { float x = W(); hpOut = 0.75f * (hpOut + x - hpPrev); hpPrev = x; return hpOut; }
            /// <summary>Band noise around cutoff k (0..1), for whooshes.</summary>
            public float Band(float k) { bp1 += (W() - bp1) * k; bp2 += (bp1 - bp2) * k; return (bp1 - bp2) * 2.2f; }
        }

        void Make()
        {
            int seed = 11;
            Nz N() => new Nz(seed++);

            // ---- UI ----
            Clip("click", 0.07f, t => Mathf.Sin(TAU * (1500 * t - 3500 * t * t)) * Env(t, 0.002f, 0.018f) * 0.35f);
            Clip("coin", 0.45f, t => (Bell(t, t < 0.075f ? 1568 : 2093) * 0.4f) * Env(t, 0.002f, t < 0.075f ? 0.05f : 0.16f));
            Clip("buy", 0.7f, t => { int i = Mathf.Min(3, (int)(t / 0.09f)); float f = 1046.5f * Mathf.Pow(2, new[] { 0, 4, 7, 12 }[i] / 12f); return Bell(t - i * 0.09f, f) * 0.28f * Env(t - i * 0.09f, 0.002f, i == 3 ? 0.25f : 0.08f); });
            Clip("error", 0.32f, t => { float f = t < 0.14f ? 330 : 247; return (Tri(t * TAU * f) * 0.3f + Mathf.Sin(t * TAU * f * 0.5f) * 0.2f) * Env(t % 0.16f, 0.004f, 0.06f); });
            { var nz = N(); Clip("token", 1.0f, t => { int i = Mathf.Min(4, (int)(t / 0.08f)); float f = 1046.5f * Mathf.Pow(2, new[] { 0, 4, 7, 12, 16 }[i] / 12f); return Bell(t - i * 0.08f, f) * 0.25f * Env(t - i * 0.08f, 0.002f, i == 4 ? 0.4f : 0.07f) + nz.High() * 0.03f * Env(t, 0.01f, 0.3f); }); }
            Clip("boing", 0.55f, t => Mathf.Sin(TAU * (160 * t + 70 * Mathf.Sin(t * 38) * t + 700 * t * t)) * Env(t, 0.004f, 0.22f) * 0.5f);
            { var nz = N(); Clip("splash", 0.8f, t => (nz.Band(0.35f) * 0.9f + Mathf.Sin(TAU * (700 * t - 600 * t * t)) * 0.12f * Env(t, 0.005f, 0.08f)) * Env(t, 0.01f, 0.22f)); }
            { var nz = N(); Clip("hurt", 0.38f, t => (Mathf.Sin(TAU * (520 * t - 700 * t * t + 6 * Mathf.Sin(t * 60))) * 0.35f + nz.Low(0.3f) * 0.25f) * Env(t, 0.004f, 0.12f)); }
            { var nz = N(); Clip("zap", 0.45f, t => (nz.High() * 0.5f + Sq(t * TAU * (110 + Mathf.Sin(t * 260) * 50)) * 0.18f) * Env(t, 0.002f, 0.13f)); }
            { var nz = N(); Clip("crate", 0.4f, t => (nz.Low(0.55f) * 1.1f * Env(t, 0.001f, 0.05f) + Tri(t * TAU * 180) * 0.25f * Env(t, 0.001f, 0.04f) + nz.Band(0.25f) * 0.4f * Env(t, 0.02f, 0.09f))); }
            Clip("bonk", 0.3f, t => (Mathf.Sin(TAU * (620 * t - 900 * t * t)) * 0.55f + Mathf.Sin(TAU * 1240 * t) * 0.15f * Env(t, 0.001f, 0.02f)) * Env(t, 0.001f, 0.08f));

            // ---- moves: attack, somersault ----
            { var nz = N(); Clip("swing", 0.24f, t => nz.Band(0.08f + t * 1.2f) * Mathf.Sin(t * Mathf.PI / 0.24f) * 0.55f); }
            { var nz = N(); Clip("punch", 0.3f, t => nz.Band(0.1f + t * 1.6f) * Mathf.Sin(Mathf.Min(1, t / 0.12f) * Mathf.PI) * 0.45f * (t < 0.12f ? 1 : 0) + (t > 0.1f ? (Mathf.Sin(TAU * (150 * (t - 0.1f) - 200 * (t - 0.1f) * (t - 0.1f))) * 0.5f + nz.Low(0.4f) * 0.3f) * Env(t - 0.1f, 0.001f, 0.05f) : 0)); }
            { var nz = N(); Clip("flip", 0.5f, t => nz.Band(0.05f + t * 0.9f) * Mathf.Sin(t * Mathf.PI / 0.5f) * 0.5f + (t > 0.28f ? Bell(t - 0.28f, 2349) * 0.12f * Env(t - 0.28f, 0.002f, 0.08f) : 0) + Mathf.Sin(TAU * (400 * t + 900 * t * t)) * 0.06f * Env(t, 0.02f, 0.15f)); }
            { var nz = N(); Clip("saber", 0.45f, t => (Mathf.Sin(t * TAU * (110 + t * 220)) * 0.3f + Tri(t * TAU * 55) * 0.12f + nz.Band(0.3f) * 0.12f) * Mathf.Sin(t * Mathf.PI / 0.45f)); }
            Clip("blaster", 0.3f, t => (Sq(t * TAU * (1500 - t * 4200)) * 0.12f + Mathf.Sin(t * TAU * (900 - t * 2500)) * 0.25f) * Env(t, 0.002f, 0.08f));
            Clip("magic", 0.7f, t => (Bell(t, 1200 + t * 900) * 0.12f + Mathf.Sin(t * TAU * (700 + t * 1400)) * 0.12f) * Env(t, 0.01f, 0.25f));
            { var nz = N(); Clip("portal", 1.0f, t => (Mathf.Sin(t * TAU * (180 + t * 700)) * 0.22f + Mathf.Sin(t * TAU * (270 + t * 1050)) * 0.12f + nz.Band(0.1f + t * 0.3f) * 0.2f) * Env(t, 0.06f, 0.4f)); }
            { var nz = N(); Clip("box", 1.3f, t => t < 0.75f ? (Mathf.Sin(t * TAU * (280 + t * 700)) * 0.16f + nz.High() * 0.05f) * Mathf.Min(1, t * 4) : Bell(t - 0.75f, 1046.5f) * 0.3f * Env(t - 0.75f, 0.003f, 0.3f) + Bell(t - 0.75f, 1568) * 0.15f * Env(t - 0.75f, 0.003f, 0.25f)); }
            Clip("open", 0.7f, t => (Bell(t, 523.25f) + Bell(t - 0.06f, 659.25f) * (t > 0.06f ? 1 : 0) + Bell(t - 0.12f, 783.99f) * (t > 0.12f ? 1 : 0)) * 0.13f * Env(t, 0.005f, 0.3f));

            // ---- 8 jump sounds (one per «Звук прыжка» item) ----
            { var nz = N(); Clip("jump0", 0.28f, t => (Mathf.Sin(TAU * (150 * t - 220 * t * t)) * 0.75f + nz.Low(0.25f) * 0.35f) * Env(t, 0.002f, 0.07f)); }                                   // «Бух!»
            Clip("jump1", 0.42f, t => Mathf.Sin(TAU * (260 * t + 90 * Mathf.Sin(t * 46) * t + 1100 * t * t)) * Env(t, 0.003f, 0.16f) * 0.42f);                                                 // пружина «Бой»
            Clip("jump2", 0.2f, t => Sq(t * TAU * (t < 0.05f ? 784 : t < 0.1f ? 1046 : 1568)) * Env(t, 0.002f, 0.08f) * 0.12f + Tri(t * TAU * 1568) * 0.1f * Env(t, 0.002f, 0.06f));            // пиксельный писк
            { var nz = N(); Clip("jump3", 0.42f, t => (nz.Band(0.2f + t) * 0.35f + Mathf.Sin(t * TAU * (220 + t * 1700)) * 0.25f) * Env(t, 0.004f, 0.13f)); }                                  // энергетический взрыв
            { var nz = N(); Clip("jump4", 0.16f, t => (nz.Low(0.18f) * 1.2f + Mathf.Sin(TAU * 240 * t) * 0.2f) * Env(t, 0.002f, 0.035f)); }                                                      // мягкий шлепок
            { var nz = N(); Clip("jump5", 0.45f, t => nz.Band(0.3f + t * 0.8f) * Mathf.Sin(t * Mathf.PI / 0.45f) * 0.35f + Mathf.Sin(t * TAU * (900 + t * 1600)) * 0.1f * Env(t, 0.01f, 0.2f)); } // реактивный свист
            Clip("jump6", 0.36f, t => (Mathf.Sin(TAU * (540 * t + 1800 * t * t + 30 * Mathf.Sin(t * 70) * t)) * 0.35f + Bell(t, 1760) * 0.08f * Env(t, 0.002f, 0.05f)) * Env(t, 0.003f, 0.12f)); // пружина аниме
            Clip("jump7", 0.42f, t => (Mathf.Sin(TAU * (70 * t - 50 * t * t)) * 0.9f + Tri(t * TAU * 70) * 0.12f) * Env(t, 0.002f, 0.14f));                                                    // басовый удар

            // ---- 8 landing sounds ----
            { var nz = N(); Clip("land0", 0.2f, t => (nz.Low(0.45f) * 0.7f + Mathf.Sin(t * TAU * 95) * 0.4f) * Env(t, 0.001f, 0.04f)); }
            Clip("land1", 0.45f, t => (Mathf.Sin(t * TAU * 340) + Mathf.Sin(t * TAU * 517) + Mathf.Sin(t * TAU * 893)) * 0.18f * Env(t, 0.001f, 0.06f) + Sq(t * TAU * (180 + t * 300)) * 0.05f * (t > 0.1f ? Env(t - 0.1f, 0.01f, 0.1f) : 0));
            { var nz = N(); Clip("land2", 0.3f, t => nz.High() * Env(t, 0.005f, 0.07f) * 0.4f); }
            { var nz = N(); Clip("land3", 0.4f, t => Mathf.Sin(TAU * (900 * t - 1600 * t * t)) * Env(t, 0.003f, 0.06f) * 0.35f + nz.Band(0.3f) * Env(t, 0.01f, 0.12f) * 0.5f); }
            Clip("land4", 0.6f, t => (Mathf.Sin(t * TAU * 523) + Mathf.Sin(t * TAU * 1230) * 0.7f + Mathf.Sin(t * TAU * 1987) * 0.5f) * Env(t, 0.001f, 0.15f) * 0.18f);
            { var nz = N(); Clip("land5", 0.12f, t => nz.Low(0.12f) * Env(t, 0.005f, 0.03f) * 0.9f); }
            { var nz = N(); Clip("land6", 0.3f, t => (Sq(t * TAU * (150 - t * 300)) * 0.2f + nz.W() * 0.12f) * Env(t, 0.002f, 0.07f)); }
            { var nz = N(); Clip("land7", 0.45f, t => (nz.Low(0.15f) * 1.4f + Mathf.Sin(TAU * (60 * t - 35 * t * t)) * 0.8f) * Env(t, 0.002f, 0.12f)); }

            // ---- steps: soft cute «туп-туп», left and right feet a little different ----
            { var nz = N(); Clip("step0", 0.09f, t => (Mathf.Sin(TAU * (230 * t - 600 * t * t)) * 0.5f + nz.Low(0.35f) * 0.35f) * Env(t, 0.002f, 0.022f)); }
            { var nz = N(); Clip("step1", 0.09f, t => (Mathf.Sin(TAU * (200 * t - 520 * t * t)) * 0.5f + nz.Low(0.3f) * 0.35f) * Env(t, 0.002f, 0.024f)); }
            Clip("steprobot", 0.08f, t => (Mathf.Sin(t * TAU * 1400) * 0.12f + Sq(t * TAU * 350) * 0.05f) * Env(t, 0.001f, 0.015f));

            // ---- notes (C major): a soft piano ----
            float[] notes = { 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880f, 987.77f, 1046.5f };
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                Clip("note" + k, 1.2f, t => PianoTone(t, f) * 0.45f);
            }

            // ---- victory jingles ----
            float Nt(int semis) => 523.25f * Mathf.Pow(2, semis / 12f);
            int[][] mel = { new[] { 0, 4, 7, 12 }, new[] { 7, 9, 11, 12 }, new[] { 0, 7, 12, 19 }, new[] { 12, 7, 4, 0 }, new[] { 0, 4, 7, 11 }, new[] { 0, 5, 9, 14 }, new[] { 0, 3, 7, 10 }, new[] { 4, 7, 11, 16 } };
            for (int k = 0; k < 8; k++)
            {
                var m = mel[k];
                Clip("win" + k, 1.1f, t =>
                {
                    int i = Mathf.Min(3, (int)(t / 0.12f));
                    float lt = t - i * 0.12f;
                    return Bell(lt, Nt(m[i])) * 0.22f * Env(lt, 0.003f, i == 3 ? 0.4f : 0.1f) + PianoTone(lt, Nt(m[i]) / 2) * 0.15f;
                });
            }

            // browsers have no worker threads for this: render right away there
            musicTask = Application.platform == RuntimePlatform.WebGLPlayer ? Task.FromResult(MakeMusic()) : Task.Run(MakeMusic);
        }

        /// <summary>Piano-like tone: hammer, a few partials that die away faster the higher they are.</summary>
        static float PianoTone(float t, float f)
        {
            if (t < 0) return 0;
            float d = 0.9f * Mathf.Pow(440f / f, 0.35f);
            float s = 0;
            for (int h = 1; h <= 6; h++)
            {
                float amp = 1f / (h * h * 0.6f + 0.4f);
                s += Mathf.Sin(t * TAU * f * h * (1 + 0.0004f * h * h)) * amp * Mathf.Exp(-t * h * 0.9f / d);
            }
            s += Mathf.Sin(t * TAU * f * 1.003f) * 0.25f * Mathf.Exp(-t / d);   // a second string, slightly detuned
            float attack = Mathf.Min(1, t / 0.003f);
            return s * attack * Mathf.Exp(-t / (d * 1.6f)) * 0.5f;
        }

        // ================= the lobby song =================

        float[] mix;

        void Add(int at, float v) { if (at >= 0 && at < mix.Length) mix[at] += v; }

        readonly Dictionary<(float, int), float[]> pianoCache = new Dictionary<(float, int), float[]>();

        void Piano(float t0, float f, float dur, float vol)
        {
            // the same notes come back again and again: render each (pitch, length) once
            var key = (f, (int)(dur * 100));
            if (!pianoCache.TryGetValue(key, out var w))
            {
                w = new float[(int)((dur + 0.6f) * SR)];
                for (int i = 0; i < w.Length; i++)
                {
                    float t = (float)i / SR;
                    w[i] = PianoTone(t, f) * (t > dur ? Mathf.Exp(-(t - dur) / 0.12f) : 1);
                }
                pianoCache[key] = w;
            }
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < w.Length; i++) Add(s0 + i, w[i] * vol);
        }

        /// <summary>Karplus–Strong plucked string (guitar, bass).</summary>
        void Pluck(float t0, float f, float len, float vol, float bright, float decay, System.Random r)
        {
            int p = Mathf.Max(2, (int)(SR / f));
            var buf = new float[p];
            float prev = 0;
            for (int i = 0; i < p; i++) { float x = (float)(r.NextDouble() * 2 - 1); prev += (x - prev) * bright; buf[i] = prev; }
            int s0 = (int)(t0 * SR), n = (int)(len * SR), idx = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (idx + 1) % p;
                float v = buf[idx];
                buf[idx] = (buf[idx] + buf[j]) * 0.5f * decay;
                idx = j;
                float fade = i > n - SR / 20 ? (n - i) / (SR / 20f) : 1;
                Add(s0 + i, v * vol * fade);
            }
        }

        void Kick(float t0, float vol)
        {
            int s0 = (int)(t0 * SR), n = (int)(0.35f * SR);
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float f = 45 + 110 * Mathf.Exp(-t * 28);
                ph += TAU * f / SR;
                Add(s0 + i, (Mathf.Sin(ph) * Mathf.Exp(-t / 0.13f) + (t < 0.004f ? 0.4f : 0)) * vol);
            }
        }

        void Snare(float t0, float vol, System.Random r)
        {
            int s0 = (int)(t0 * SR), n = (int)(0.25f * SR);
            float a = 0, b = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float x = (float)(r.NextDouble() * 2 - 1);
                a += (x - a) * 0.6f; b += (a - b) * 0.6f;
                float noise = (a - b) * 2.2f * Mathf.Exp(-t / 0.07f);
                float tone = Mathf.Sin(TAU * 190 * t) * Mathf.Exp(-t / 0.035f) * 0.5f;
                Add(s0 + i, (noise + tone) * vol);
            }
        }

        void Hat(float t0, float vol, float len, System.Random r)
        {
            int s0 = (int)(t0 * SR), n = (int)(len * 4 * SR);
            float prev = 0, o = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float x = (float)(r.NextDouble() * 2 - 1);
                o = 0.6f * (o + x - prev); prev = x;
                Add(s0 + i, o * Mathf.Exp(-t / len) * vol);
            }
        }

        void Glock(float t0, float f, float vol)
        {
            int s0 = (int)(t0 * SR), n = (int)(1.2f * SR);
            for (int i = 0; i < n; i++) { float t = (float)i / SR; Add(s0 + i, Bell(t, f) * Env(t, 0.002f, 0.35f) * vol); }
        }

        void Pad(float t0, float f, float dur, float vol)
        {
            int s0 = (int)(t0 * SR), n = (int)(dur * SR);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float e = Mathf.Min(1, t / 0.4f) * Mathf.Min(1, (dur - t) / 0.3f);
                Add(s0 + i, (Mathf.Sin(t * TAU * f) + Mathf.Sin(t * TAU * f * 1.004f) * 0.7f + Mathf.Sin(t * TAU * f * 2.002f) * 0.2f) * e * vol);
            }
        }

        /// <summary>
        /// «Песенка долины», 16 bars at 104 bpm: piano melody with left-hand chords, fingerpicked then strummed
        /// guitar, plucked bass, drums with a fill every 8 bars, bells doubling the second half, a soft pad and echo.
        /// </summary>
        float[] MakeMusic()
        {
            const float bpm = 104, beat = 60f / bpm;
            const int bars = 16;
            float barLen = 4 * beat, len = bars * barLen;
            mix = new float[(int)(len * SR)];
            var rnd = new System.Random(2026);
            float F(float semi) => 261.63f * Mathf.Pow(2, semi / 12f);   // 0 = C4

            // chords: root (semitones from C4, kept low) and minor flag
            (int root, bool minor)[] prog = { (0, false), (-5, false), (-3, true), (-7, false), (0, false), (-5, false), (-7, false), (-5, false),
                                              (-3, true), (-7, false), (0, false), (-5, false), (-7, false), (-5, false), (0, false), (0, false) };
            // melody: (bar, beat, semitone, length in beats)
            var mel = new List<(int, float, int, float)>();
            void M(int bar, params float[] v) { for (int i = 0; i < v.Length; i += 3) mel.Add((bar, v[i], (int)v[i + 1], v[i + 2])); }
            M(0, 0, 7, 1, 1, 9, 0.5f, 1.5f, 7, 0.5f, 2, 4, 1, 3, 0, 1);
            M(1, 0, 2, 1, 1, 4, 0.5f, 1.5f, 7, 0.5f, 2, 11, 1.5f, 3.5f, 9, 0.5f);
            M(2, 0, 12, 1, 1, 11, 0.5f, 1.5f, 9, 0.5f, 2, 7, 1, 3, 4, 1);
            M(3, 0, 5, 1, 1, 4, 0.5f, 1.5f, 5, 0.5f, 2, 9, 2);
            M(4, 0, 7, 0.5f, 0.5f, 7, 0.5f, 1, 12, 1, 2, 11, 0.5f, 2.5f, 9, 0.5f, 3, 7, 1);
            M(5, 0, 9, 1, 1, 7, 0.5f, 1.5f, 5, 0.5f, 2, 4, 1, 3, 2, 1);
            M(6, 0, 5, 0.5f, 0.5f, 9, 0.5f, 1, 12, 1, 2, 14, 1, 3, 12, 1);
            M(7, 0, 11, 1.5f, 1.5f, 9, 0.5f, 2, 7, 2);
            M(8, 0, 16, 1, 1, 14, 0.5f, 1.5f, 12, 0.5f, 2, 9, 1, 3, 12, 1);
            M(9, 0, 17, 1, 1, 16, 0.5f, 1.5f, 14, 0.5f, 2, 12, 2);
            M(10, 0, 16, 0.5f, 0.5f, 14, 0.5f, 1, 12, 1, 2, 7, 1, 3, 12, 1);
            M(11, 0, 14, 1.5f, 1.5f, 12, 0.5f, 2, 11, 1, 3, 7, 1);
            M(12, 0, 9, 1, 1, 12, 0.5f, 1.5f, 14, 0.5f, 2, 17, 1, 3, 16, 1);
            M(13, 0, 14, 1, 1, 12, 0.5f, 1.5f, 11, 0.5f, 2, 14, 1, 3, 19, 1);
            M(14, 0, 19, 1, 1, 16, 0.5f, 1.5f, 14, 0.5f, 2, 12, 2);
            M(15, 0, 7, 0.5f, 0.5f, 9, 0.5f, 1, 11, 0.5f, 1.5f, 12, 2.5f);

            for (int bar = 0; bar < bars; bar++)
            {
                float b0 = bar * barLen;
                var (root, minor) = prog[bar];
                int third = minor ? 3 : 4;
                int[] triad = { root, root + third, root + 7 };

                // piano left hand: chord on 1, softer on 3
                foreach (var s in triad) { Piano(b0, F(s), beat * 1.8f, 0.07f); Piano(b0 + 2 * beat, F(s), beat * 1.6f, 0.05f); }
                // pad
                foreach (var s in triad) Pad(b0, F(s - 12), barLen, 0.012f);

                // guitar: fingerpicked in the first half, strummed in the second
                int g = root - 12;   // guitar voicing around the third octave
                int[] strings = { g, g + 7, g + 12, g + 12 + third, g + 19, g + 24 };
                if (bar < 8)
                {
                    int[] pick = { 0, 2, 3, 4, 1, 3, 4, 5 };
                    for (int k = 0; k < 8; k++) Pluck(b0 + k * beat / 2, F(strings[pick[k]]), 1.2f, 0.16f, 0.55f, 0.996f, rnd);
                }
                else
                {
                    float[] hits = { 0, 1, 1.5f, 2.5f, 3, 3.5f };
                    bool[] down = { true, true, false, false, true, false };
                    for (int h = 0; h < hits.Length; h++)
                        for (int sIdx = 0; sIdx < strings.Length; sIdx++)
                        {
                            int si = down[h] ? sIdx : strings.Length - 1 - sIdx;
                            Pluck(b0 + hits[h] * beat + sIdx * 0.011f, F(strings[si]), 0.9f, down[h] ? 0.075f : 0.05f, 0.7f, 0.994f, rnd);
                        }
                }

                // bass: root on 1, fifth on «and» of 2, root on 3, octave walk-up on 4
                float bf = F(root - 24);
                Pluck(b0, bf, beat * 1.4f, 0.42f, 0.25f, 0.998f, rnd);
                Pluck(b0 + 1.5f * beat, F(root - 24 + 7), beat * 0.9f, 0.3f, 0.25f, 0.998f, rnd);
                Pluck(b0 + 2 * beat, bf, beat * 1.2f, 0.38f, 0.25f, 0.998f, rnd);
                Pluck(b0 + 3.5f * beat, F(root - 12), beat * 0.6f, 0.28f, 0.3f, 0.998f, rnd);

                // drums
                bool fill = bar % 8 == 7;
                Kick(b0, 0.55f); Kick(b0 + 2 * beat, 0.5f); Kick(b0 + 2.5f * beat, 0.3f);
                Snare(b0 + beat, 0.28f, rnd); Snare(b0 + 3 * beat, fill ? 0.2f : 0.28f, rnd);
                if (fill) for (int k = 0; k < 4; k++) Snare(b0 + 3 * beat + k * beat / 4, 0.12f + k * 0.04f, rnd);
                for (int k = 0; k < 8; k++) Hat(b0 + k * beat / 2, k % 2 == 0 ? 0.05f : 0.035f, k == 7 ? 0.05f : 0.018f, rnd);
            }

            foreach (var (bar, at, semi, lenB) in mel)
            {
                float t0 = bar * barLen + at * beat;
                Piano(t0, F(semi), lenB * beat * 0.95f, 0.2f);
                if (bar >= 8) Glock(t0, F(semi + 12), 0.05f);
            }

            // soft echo (dotted eighth), wrapped round so the loop stays seamless
            int d = (int)(beat * 0.75f * SR);
            var dry = (float[])mix.Clone();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < mix.Length; i++)
                {
                    int j = i - d; if (j < 0) j += mix.Length;
                    mix[i] = dry[i] + mix[j] * 0.22f;
                }
            float peak = 0.001f;
            foreach (var v in mix) peak = Mathf.Max(peak, Mathf.Abs(v));
            float g2 = 0.9f / peak;
            for (int i = 0; i < mix.Length; i++) mix[i] = (float)Math.Tanh(mix[i] * g2 * 1.1f) * 0.9f;
            var result = mix;
            mix = null;
            pianoCache.Clear();
            return result;
        }

        public static void Play(string name, float vol = 1f)
        {
            if (I == null || !I.clips.TryGetValue(name, out var c)) return;
            I.src.PlayOneShot(c, vol);
        }

        public static void Jump(int i, bool air) => Play("jump" + Mathf.Clamp(i, 0, 7), air ? 0.8f : 1f);
        public static void Land(int i) => Play("land" + Mathf.Clamp(i, 0, 7), 0.9f);
        public static void Note(int i) => Play("note" + Mathf.Clamp(i, 0, 7));
        public static void Step(int shoes, bool robot)
        {
            if (I == null) return;
            I.stepFoot ^= 1;
            Play(robot ? "steprobot" : "step" + I.stepFoot, shoes == 2 || shoes == 6 ? 0.9f : 0.55f);
        }
        /// <summary>Tests: writes clips as 16-bit WAV files to listen to them.</summary>
        public static void Export(string dir, params string[] names)
        {
            System.IO.Directory.CreateDirectory(dir);
            foreach (var name in names)
            {
                if (I == null || !I.clips.TryGetValue(name, out var c)) continue;
                var d = new float[c.samples];
                c.GetData(d, 0);
                using var f = new System.IO.BinaryWriter(System.IO.File.Create(System.IO.Path.Combine(dir, name + ".wav")));
                f.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + d.Length * 2); f.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                f.Write(16); f.Write((short)1); f.Write((short)1); f.Write(SR); f.Write(SR * 2); f.Write((short)2); f.Write((short)16);
                f.Write(System.Text.Encoding.ASCII.GetBytes("data")); f.Write(d.Length * 2);
                foreach (var v in d) f.Write((short)(Mathf.Clamp(v, -1, 1) * 32767));
            }
        }

        public static bool Has(string name) => I != null && I.clips.ContainsKey(name);
    }
}
