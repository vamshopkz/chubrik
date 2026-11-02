using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// All sounds are synthesized at start (no audio files): 8 jump sounds, 8 landing sounds, notes of the musical
    /// path, victory jingles, UI clicks and a cheerful lobby music loop.
    /// </summary>
    public class AudioFx : MonoBehaviour
    {
        static AudioFx I;
        AudioSource src, music;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int SR = 22050;
        const float TAU = Mathf.PI * 2;

        public static void Create()
        {
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioFx>();
            I.src = go.AddComponent<AudioSource>();
            I.music = go.AddComponent<AudioSource>();
            I.music.loop = true; I.music.volume = 0.32f;
            I.Make();
            SetVolume(Save.P.volume);
            SetMusic(Save.P.music);
        }

        public static void SetVolume(float v) { AudioListener.volume = Mathf.Clamp01(v); }

        public static void SetMusic(bool on)
        {
            if (I == null) return;
            if (on && !Boot.Testing) { if (!I.music.isPlaying) { I.music.clip = I.clips["music"]; I.music.Play(); } }
            else I.music.Stop();
        }

        delegate float Wave(float t);

        void Clip(string name, float len, Wave w)
        {
            int n = (int)(SR * len);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(w((float)i / SR), -1, 1);
            var c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(data, 0);
            clips[name] = c;
        }

        static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Exp(-(t - a) / d);
        static float Sq(float x) => Mathf.Sin(x) >= 0 ? 1 : -1;

        void Make()
        {
            var rnd = new System.Random(5);
            float Noise() => (float)(rnd.NextDouble() * 2 - 1);
            float lp = 0;
            float Low(float k) { lp += (Noise() - lp) * k; return lp; }
            float hpPrev = 0, hpOut = 0;
            float High() { float x = Noise(); hpOut = 0.7f * (hpOut + x - hpPrev); hpPrev = x; return hpOut; }

            Clip("click", 0.06f, t => Mathf.Sin(t * TAU * 900) * Env(t, 0.002f, 0.015f) * 0.35f);
            Clip("coin", 0.32f, t => (Mathf.Sin(t * TAU * (t < 0.07f ? 1320 : 1980)) * 0.5f + Mathf.Sin(t * TAU * 2640) * 0.12f) * Env(t, 0.003f, 0.1f) * 0.45f);
            Clip("buy", 0.5f, t => (Mathf.Sin(t * TAU * 660) + Mathf.Sin(t * TAU * (t < 0.12f ? 880 : t < 0.24f ? 1100 : 1320))) * 0.22f * Env(t, 0.005f, 0.18f));
            Clip("error", 0.3f, t => Sq(t * TAU * 160) * 0.16f * Env(t, 0.005f, 0.12f));
            Clip("token", 0.9f, t => (Mathf.Sin(t * TAU * (t < 0.12f ? 784 : t < 0.24f ? 988 : t < 0.36f ? 1175 : 1568)) * 0.35f + Mathf.Sin(t * TAU * 3136) * 0.05f) * Env(t, 0.005f, 0.35f));
            Clip("boing", 0.5f, t => Mathf.Sin(TAU * (180 * t + 60 * Mathf.Sin(t * 40) * t + 500 * t * t)) * Env(t, 0.005f, 0.2f) * 0.5f);
            Clip("splash", 0.8f, t => (Low(0.25f) * 1.2f + Mathf.Sin(t * TAU * (500 - t * 400)) * 0.15f * Env(t, 0.01f, 0.1f)) * Env(t, 0.01f, 0.25f));
            Clip("hurt", 0.3f, t => (Sq(t * TAU * (420 - t * 900)) * 0.18f + Low(0.4f) * 0.3f) * Env(t, 0.005f, 0.1f));
            Clip("zap", 0.5f, t => (High() * 0.7f + Sq(t * TAU * (90 + Mathf.Sin(t * 300) * 40)) * 0.3f) * Env(t, 0.002f, 0.15f));
            Clip("crate", 0.35f, t => (Low(0.5f) * 1.2f + Sq(t * TAU * 110) * 0.2f) * Env(t, 0.002f, 0.07f));
            Clip("bonk", 0.25f, t => Mathf.Sin(t * TAU * (300 - t * 600)) * Env(t, 0.002f, 0.07f) * 0.6f);
            Clip("swing", 0.22f, t => Noise() * Mathf.Sin(t * Mathf.PI / 0.22f) * 0.3f);
            Clip("saber", 0.45f, t => (Mathf.Sin(t * TAU * (110 + t * 200)) * 0.3f + Sq(t * TAU * 55) * 0.1f + Noise() * 0.08f) * Mathf.Sin(t * Mathf.PI / 0.45f));
            Clip("blaster", 0.3f, t => Sq(t * TAU * (1400 - t * 4000)) * Env(t, 0.002f, 0.08f) * 0.2f);
            Clip("magic", 0.6f, t => (Mathf.Sin(t * TAU * (800 + t * 1500)) * 0.2f + Mathf.Sin(t * TAU * (1200 + t * 1800)) * 0.12f) * Env(t, 0.01f, 0.22f));
            Clip("portal", 0.9f, t => Mathf.Sin(t * TAU * (200 + t * 900)) * Env(t, 0.05f, 0.35f) * 0.3f);
            Clip("box", 1.2f, t => (Mathf.Sin(t * TAU * (300 + t * 600)) * 0.2f * (t < 0.7f ? 1 : 0) + (t > 0.7f ? Mathf.Sin(t * TAU * 1046) * 0.3f * Env(t - 0.7f, 0.005f, 0.3f) : 0)) + Noise() * 0.05f * (t < 0.7f ? 1 : 0));
            Clip("open", 0.6f, t => (Mathf.Sin(t * TAU * 523) + Mathf.Sin(t * TAU * 659) + Mathf.Sin(t * TAU * 784)) * 0.15f * Env(t, 0.01f, 0.3f));

            // ---- 8 jump sounds ----
            Clip("jump0", 0.25f, t => (Mathf.Sin(t * TAU * (120 - t * 200)) * 0.7f + Low(0.3f) * 0.3f) * Env(t, 0.002f, 0.08f));                         // «Бух!»
            Clip("jump1", 0.4f, t => Mathf.Sin(TAU * (220 * t + 80 * Mathf.Sin(t * 50) * t + 900 * t * t)) * Env(t, 0.003f, 0.15f) * 0.45f);           // пружина «Бой»
            Clip("jump2", 0.18f, t => Sq(t * TAU * (t < 0.06f ? 660 : t < 0.12f ? 880 : 1320)) * Env(t, 0.002f, 0.08f) * 0.15f);                        // пиксельный писк
            Clip("jump3", 0.4f, t => (Noise() * 0.4f + Mathf.Sin(t * TAU * (200 + t * 1600)) * 0.3f) * Env(t, 0.005f, 0.12f));                           // энергетический взрыв
            Clip("jump4", 0.15f, t => Low(0.2f) * Env(t, 0.002f, 0.035f) * 1.4f);                                                                         // мягкий шлепок
            Clip("jump5", 0.45f, t => High() * Mathf.Sin(t * Mathf.PI / 0.45f) * 0.35f + Mathf.Sin(t * TAU * (900 + t * 1500)) * 0.1f * Env(t, 0.01f, 0.2f)); // реактивный свист
            Clip("jump6", 0.35f, t => Mathf.Sin(TAU * (500 * t + 1600 * t * t + 30 * Mathf.Sin(t * 70) * t)) * Env(t, 0.003f, 0.12f) * 0.4f);            // пружина аниме
            Clip("jump7", 0.4f, t => (Mathf.Sin(t * TAU * (65 - t * 60)) * 0.9f + Sq(t * TAU * 65) * 0.1f) * Env(t, 0.002f, 0.14f));                     // басовый удар

            // ---- 8 landing sounds ----
            Clip("land0", 0.2f, t => (Low(0.45f) * 0.9f + Mathf.Sin(t * TAU * 90) * 0.4f) * Env(t, 0.001f, 0.04f));                                       // стук ботинок
            Clip("land1", 0.45f, t => ((Mathf.Sin(t * TAU * 340) + Mathf.Sin(t * TAU * 517) + Mathf.Sin(t * TAU * 893)) * 0.2f * Env(t, 0.001f, 0.06f)) + Sq(t * TAU * (180 + t * 300)) * 0.06f * (t > 0.1f ? Env(t - 0.1f, 0.01f, 0.1f) : 0)); // робот
            Clip("land2", 0.3f, t => High() * Env(t, 0.005f, 0.08f) * 0.45f);                                                                            // трава
            Clip("land3", 0.4f, t => Mathf.Sin(t * TAU * (900 - t * 1800)) * Env(t, 0.003f, 0.06f) * 0.4f + Low(0.3f) * Env(t, 0.01f, 0.12f) * 0.6f);    // плюх
            Clip("land4", 0.6f, t => (Mathf.Sin(t * TAU * 523) + Mathf.Sin(t * TAU * 1230) * 0.7f + Mathf.Sin(t * TAU * 1987) * 0.5f) * Env(t, 0.001f, 0.15f) * 0.2f); // лязг
            Clip("land5", 0.12f, t => Low(0.12f) * Env(t, 0.005f, 0.03f) * 0.8f);                                                                       // мягкие лапки
            Clip("land6", 0.3f, t => (Sq(t * TAU * (150 - t * 300)) * 0.25f + Noise() * 0.15f) * Env(t, 0.002f, 0.07f));                                 // электронный бах
            Clip("land7", 0.45f, t => (Low(0.15f) * 1.5f + Mathf.Sin(t * TAU * (55 - t * 40)) * 0.8f) * Env(t, 0.002f, 0.12f));                          // тяжёлый топот

            Clip("step", 0.07f, t => Low(0.3f) * Env(t, 0.002f, 0.018f) * 0.35f);
            Clip("steprobot", 0.08f, t => Mathf.Sin(t * TAU * 1400) * Env(t, 0.001f, 0.015f) * 0.15f);

            // ---- notes (C major) ----
            float[] notes = { 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880f, 987.77f, 1046.5f };
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                Clip("note" + k, 1f, t => (Mathf.Sin(t * TAU * f) * 0.5f + Mathf.Sin(t * TAU * f * 2) * 0.15f + Mathf.Sin(t * TAU * f * 3) * 0.05f) * Env(t, 0.004f, 0.35f) * 0.5f);
            }

            // ---- victory jingles ----
            float N(int semis) => 523.25f * Mathf.Pow(2, semis / 12f);
            int[][] mel = { new[] { 0, 4, 7, 12 }, new[] { 7, 9, 11, 12 }, new[] { 0, 7, 12, 19 }, new[] { 12, 7, 4, 0 }, new[] { 0, 4, 7, 11 }, new[] { 0, 5, 9, 14 }, new[] { 0, 3, 7, 10 }, new[] { 4, 7, 11, 16 } };
            for (int k = 0; k < 8; k++)
            {
                var m = mel[k];
                Clip("win" + k, 1f, t =>
                {
                    int i = Mathf.Min(3, (int)(t / 0.12f));
                    float lt = t - i * 0.12f;
                    float f = N(m[i]);
                    return (Mathf.Sin(t * TAU * f) * 0.3f + Sq(t * TAU * f) * 0.05f) * Env(lt, 0.004f, i == 3 ? 0.35f : 0.1f);
                });
            }

            MakeMusic();
        }

        /// <summary>16-bar cheerful loop: bass, chords and a simple melody (C – G – Am – F).</summary>
        void MakeMusic()
        {
            const float bpm = 112, beat = 60f / bpm;
            int bars = 8;
            float len = bars * 4 * beat;
            int n = (int)(SR * len);
            var d = new float[n];
            int[][] chords = { new[] { 0, 4, 7 }, new[] { 7, 11, 14 }, new[] { 9, 12, 16 }, new[] { 5, 9, 12 } };
            int[] melody = { 12, 14, 16, 19, 16, 14, 12, -1, 14, 16, 19, 21, 19, 16, 14, -1, 16, 19, 21, 24, 21, 19, 16, 14, 12, 14, 12, 9, 7, 9, 12, -1 };
            float F(float semi) => 261.63f * Mathf.Pow(2, semi / 12f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float b = t / beat;
                int bar = (int)(b / 4) % 4;
                float inBeat = b - Mathf.Floor(b);
                var ch = chords[bar];
                float s = 0;
                // bass on every beat
                float bf = F(ch[0] - 24);
                s += (Mathf.Sin(t * TAU * bf) * 0.5f + Sq(t * TAU * bf) * 0.08f) * Env(inBeat * beat, 0.005f, 0.18f) * 0.6f;
                // chord stabs on off-beats
                float off = b + 0.5f; float inOff = off - Mathf.Floor(off);
                foreach (var c in ch) s += Mathf.Sin(t * TAU * F(c)) * Env(inOff * beat, 0.005f, 0.09f) * 0.08f;
                // melody, eighth notes
                int e = (int)(b * 2) % melody.Length;
                float inE = b * 2 - Mathf.Floor(b * 2);
                if (melody[e] >= 0)
                {
                    float mf = F(melody[e]);
                    s += (Mathf.Sin(t * TAU * mf) * 0.6f + Mathf.Sin(t * TAU * mf * 2) * 0.15f) * Env(inE * beat / 2, 0.006f, 0.12f) * 0.18f;
                }
                // hi-hat
                float hh = b * 2; float inH = hh - Mathf.Floor(hh);
                s += ((float)((i * 1103515245 + 12345) & 0xFFFF) / 32768f - 1) * Env(inH * beat / 2, 0.001f, 0.012f) * 0.05f;
                d[i] = Mathf.Clamp(s, -1, 1) * 0.8f;
            }
            var clip = AudioClip.Create("music", n, 1, SR, false);
            clip.SetData(d, 0);
            clips["music"] = clip;
        }

        public static void Play(string name, float vol = 1f)
        {
            if (I == null || !I.clips.TryGetValue(name, out var c)) return;
            I.src.PlayOneShot(c, vol);
        }

        public static void Jump(int i, bool air) => Play("jump" + Mathf.Clamp(i, 0, 7), air ? 0.8f : 1f);
        public static void Land(int i) => Play("land" + Mathf.Clamp(i, 0, 7), 0.9f);
        public static void Note(int i) => Play("note" + Mathf.Clamp(i, 0, 7));
        public static void Step(int shoes, bool robot) => Play(robot ? "steprobot" : "step", shoes == 2 || shoes == 6 ? 0.9f : 0.5f);
        public static bool Has(string name) => I != null && I.clips.ContainsKey(name);
    }
}
