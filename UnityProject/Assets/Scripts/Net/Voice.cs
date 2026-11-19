using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// v0.27 voice: while you are in a room with a friend, hold «ГОЛОС» (V on a computer) and your friend hears you;
    /// you hear your friend. The sound goes only to that friend (their own topic on the relay): 8 kHz, μ-law,
    /// 100 ms pieces, like a phone call. The browser build can't use the microphone (Unity), it can only listen.
    /// </summary>
    public class Voice : MonoBehaviour
    {
        public static Voice I;
        public const int Rate = 8000, Chunk = 800;   // 100 ms
        public static bool CanTalk => Application.platform != RuntimePlatform.WebGLPlayer;
        public bool Talking { get; private set; }
        public float HeardAt = -99;                    // when the friend's voice last came
        public int Sent, Heard;                        // autotest
        public bool HoldTalk;                          // tests / the HUD button
        string mic; AudioClip micClip; int micPos, micRate;
        readonly List<float> pending = new List<float>();
        int seq, lastSeq = -1;
        // playback: a ring buffer read by the audio thread
        readonly float[] ring = new float[Rate * 2];
        int wr, rd, filled;
        readonly object locker = new object();
        AudioSource src;

        public static void Create()
        {
            if (I != null) return;
            I = new GameObject("Voice").AddComponent<Voice>();
            DontDestroyOnLoad(I.gameObject);
        }

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.clip = AudioClip.Create("FriendVoice", Rate, 1, Rate, true, OnRead);
            src.loop = true; src.spatialBlend = 0; src.volume = 1f;
            src.Play();
        }

        void OnRead(float[] data)
        {
            lock (locker)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (filled > 0) { data[i] = ring[rd]; rd = (rd + 1) % ring.Length; filled--; }
                    else data[i] = 0;
                }
            }
        }

        bool InRoom => Online.I != null && Online.I.RoomPeer != null;

        void Update()
        {
            bool want = InRoom && CanTalk && (HoldTalk || Hud.VoiceButtonHeld || (!UIRoot.TypingText && Input.GetKey(KeyCode.V)));
            if (want && !Talking) StartTalk();
            else if (!want && Talking) StopTalk();
            if (Talking) Capture();
        }

        void StartTalk()
        {
#if !UNITY_WEBGL
            if (Application.platform == RuntimePlatform.Android && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                return;
            }
            if (Microphone.devices.Length == 0) { if (!Boot.Testing) Hud.Toast("Нет микрофона", UIKit.Orange); return; }
            mic = Microphone.devices[0];
            Microphone.GetDeviceCaps(mic, out var minF, out var maxF);
            micRate = maxF == 0 ? 16000 : Mathf.Clamp(16000, minF == 0 ? 8000 : minF, maxF);
            micClip = Microphone.Start(mic, true, 1, micRate);
            micPos = 0;
            Talking = true;
#endif
        }

        void StopTalk()
        {
#if !UNITY_WEBGL
            if (mic != null) Microphone.End(mic);
#endif
            Talking = false; micClip = null; pending.Clear();
        }

        void Capture()
        {
#if !UNITY_WEBGL
            if (micClip == null) return;
            int pos = Microphone.GetPosition(mic);
            if (pos < 0 || pos == micPos) return;
            int n = pos > micPos ? pos - micPos : micClip.samples - micPos + pos;
            var buf = new float[n];
            micClip.GetData(buf, micPos);
            micPos = pos;
            // down to 8 kHz
            float step = micRate / (float)Rate;
            for (float k = 0; k < n; k += step) pending.Add(buf[(int)k]);
            while (pending.Count >= Chunk)
            {
                var piece = pending.GetRange(0, Chunk);
                pending.RemoveRange(0, Chunk);
                SendPiece(piece.ToArray());
            }
#endif
        }

        /// <summary>One 100 ms piece to the friend (μ-law, base64).</summary>
        public void SendPiece(float[] pcm)
        {
            var on = Online.I;
            if (on == null || on.RoomWith == null) return;
            var b = new byte[pcm.Length];
            for (int i = 0; i < pcm.Length; i++) b[i] = MuLaw(pcm[i]);
            on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"V|{on.Id}|{++seq}|{Convert.ToBase64String(b)}");
            Sent++;
        }

        /// <summary>A piece from the friend (both relays bring it: the same number is played once).</summary>
        public void OnVoice(string[] f)
        {
            if (f.Length < 4 || Online.I == null || f[1] != Online.I.RoomWith) return;
            if (!int.TryParse(f[2], out var s) || (s <= lastSeq && lastSeq - s < 1000)) return;
            lastSeq = s;
            byte[] b;
            try { b = Convert.FromBase64String(f[3]); } catch { return; }
            lock (locker)
            {
                foreach (var x in b)
                {
                    if (filled >= ring.Length) break;
                    ring[wr] = UnMuLaw(x); wr = (wr + 1) % ring.Length; filled++;
                }
            }
            Heard++;
            HeardAt = Time.unscaledTime;
        }

        public int Buffered { get { lock (locker) return filled; } }

        // ---- μ-law (G.711) ----
        public static byte MuLaw(float x)
        {
            int s = Mathf.Clamp((int)(x * 32767f), -32767, 32767);
            int sign = s < 0 ? 0x80 : 0;
            if (s < 0) s = -s;
            s = Mathf.Min(s + 132, 32635);
            int exp = 7;
            for (int m = 0x4000; (s & m) == 0 && exp > 0; m >>= 1) exp--;
            int mant = (s >> (exp + 3)) & 0x0F;
            return (byte)~(sign | (exp << 4) | mant);
        }

        public static float UnMuLaw(byte b)
        {
            int u = ~b & 0xFF;
            int sign = u & 0x80, exp = (u >> 4) & 0x07, mant = u & 0x0F;
            int s = ((mant << 3) + 132) << exp;
            s -= 132;
            return (sign != 0 ? -s : s) / 32768f;
        }
    }
}
