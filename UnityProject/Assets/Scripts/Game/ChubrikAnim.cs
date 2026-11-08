using System;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Procedural animation of the chubrik rig: 8 walk styles, jump squash &amp; stretch, glide, 8 idle emotes,
    /// weapon swing, blinking. Fires OnStep at every footfall (step effects / sounds).
    /// </summary>
    public class ChubrikAnim : MonoBehaviour
    {
        public ChubrikModel model;
        public float speed01;          // horizontal speed / run speed
        public bool grounded = true, gliding;
        public float vy;
        public int walkStyle, emote = 2;
        public bool emoting;           // playing the emote right now
        public float emoteT;
        public Action<bool> OnStep;    // true = left foot
        float phase, land, attackT = -1, blinkT = 2, prevSin;
        public bool Preview;           // wardrobe mannequin: slow idle + emote loop
        GameObject coin;

        public void Land(float impact) { land = Mathf.Clamp01(impact); }
        public void Attack() { attackT = 0; }
        /// <summary>Somersault (the second jump in the air).</summary>
        public void Flip() { flipT = 0; }
        public bool Flipping => flipT >= 0;
        /// <summary>Lying on the belly (the dive and the slide after it).</summary>
        public bool belly;
        float bellyK;
        float flipT = -1;
        public bool Attacking => attackT >= 0 && attackT < 0.35f;

        public void PlayEmote() { emoting = true; emoteT = 0; }
        public void StopEmote() { emoting = false; if (coin) Destroy(coin); }

        void LateUpdate()
        {
            var m = model;
            if (m == null || m.Body == null) return;
            float dt = Time.deltaTime;
            m.speed01 = speed01;
            m.flap = Mathf.MoveTowards(m.flap, gliding || !grounded ? 1 : 0, dt * 3);

            // reset pose
            m.Body.localPosition = Vector3.zero; m.Body.localRotation = Quaternion.identity;
            var bs = m.Body.localScale; // keep secret stretch modifiers
            float sx = 1, sy = 1;
            m.Torso.localRotation = Quaternion.identity;
            m.Head.localRotation = Quaternion.identity;
            Quaternion armL = Quaternion.Euler(0, 0, -18), armR = Quaternion.Euler(0, 0, 18);
            Quaternion legL = Quaternion.identity, legR = Quaternion.identity;
            float lift = 0;

            if (grounded)
            {
                float freq = walkStyle switch { 1 => 2.6f, 2 => 1.6f, 3 => 2.4f, 4 => 1.8f, 5 => 1.5f, 6 => 1.8f, _ => 2.1f };
                phase += dt * freq * Mathf.Lerp(0.6f, 1.6f, speed01) * Mathf.PI * 2 * (speed01 > 0.05f ? 1 : 0);
                float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
                float a = speed01;
                if (a > 0.05f)
                {
                    switch (walkStyle)
                    {
                        case 1: // springy
                            lift = Mathf.Abs(s) * 0.16f * a; sy = 1 + Mathf.Abs(c) * 0.08f * a; sx = 2 - sy;
                            legL = Quaternion.Euler(s * 35 * a, 0, 0); legR = Quaternion.Euler(-s * 35 * a, 0, 0);
                            armL = Quaternion.Euler(-s * 30 * a, 0, -30); armR = Quaternion.Euler(s * 30 * a, 0, 30);
                            break;
                        case 2: // sneaky
                            lift = -0.08f; m.Torso.localRotation = Quaternion.Euler(18, 0, 0); m.Head.localRotation = Quaternion.Euler(-14, s * 10, 0);
                            legL = Quaternion.Euler(s * 25 * a - 15, 0, 0); legR = Quaternion.Euler(-s * 25 * a - 15, 0, 0);
                            armL = Quaternion.Euler(-60, 0, -25); armR = Quaternion.Euler(-60, 0, 25);
                            break;
                        case 3: // skipping
                            lift = Mathf.Max(0, s) * 0.22f * a;
                            legL = Quaternion.Euler(Mathf.Max(0, s) * 60 * a, 0, 0); legR = Quaternion.Euler(Mathf.Max(0, -s) * 60 * a, 0, 0);
                            armL = Quaternion.Euler(0, 0, -50 - s * 30); armR = Quaternion.Euler(0, 0, 50 + s * 30);
                            break;
                        case 4: // robotic
                        {
                            float q = Mathf.Round(s * 2) / 2;
                            legL = Quaternion.Euler(q * 30 * a, 0, 0); legR = Quaternion.Euler(-q * 30 * a, 0, 0);
                            armL = Quaternion.Euler(-q * 40 * a, 0, -8); armR = Quaternion.Euler(q * 40 * a, 0, 8);
                            m.Head.localRotation = Quaternion.Euler(0, q * 12, 0);
                            break;
                        }
                        case 5: // swagger
                            m.Torso.localRotation = Quaternion.Euler(-6, s * 10, s * 8); lift = Mathf.Abs(s) * 0.05f;
                            legL = Quaternion.Euler(s * 28 * a, 0, -6); legR = Quaternion.Euler(-s * 28 * a, 0, 6);
                            armL = Quaternion.Euler(-s * 15, 0, -10); armR = Quaternion.Euler(s * 15, 0, 10);
                            break;
                        case 6: // moonwalk: feet slide backwards
                            legL = Quaternion.Euler(-Mathf.Max(0, s) * 30 * a, 0, 0); legR = Quaternion.Euler(-Mathf.Max(0, -s) * 30 * a, 0, 0);
                            m.Body.localPosition = new Vector3(0, 0, -0.05f);
                            m.Torso.localRotation = Quaternion.Euler(-8, 0, 0);
                            armL = Quaternion.Euler(10, 0, -25 + s * 10); armR = Quaternion.Euler(10, 0, 25 - s * 10);
                            break;
                        case 7: // levitation
                            lift = 0.3f + Mathf.Sin(Time.time * 2) * 0.05f;
                            legL = Quaternion.Euler(20, 0, 0); legR = Quaternion.Euler(20, 0, 0);
                            armL = Quaternion.Euler(0, 0, -45); armR = Quaternion.Euler(0, 0, 45);
                            m.Torso.localRotation = Quaternion.Euler(12 * a, 0, 0);
                            break;
                        default: // confident
                            lift = Mathf.Abs(s) * 0.07f * a;
                            legL = Quaternion.Euler(s * 38 * a, 0, 0); legR = Quaternion.Euler(-s * 38 * a, 0, 0);
                            armL = Quaternion.Euler(-s * 35 * a, 0, -16); armR = Quaternion.Euler(s * 35 * a, 0, 16);
                            m.Torso.localRotation = Quaternion.Euler(6 * a, s * 5 * a, 0);
                            break;
                    }
                    // footfall when the swing changes direction
                    if (walkStyle != 7 && Mathf.Sign(s) != Mathf.Sign(prevSin)) OnStep?.Invoke(s > 0);
                    prevSin = s;
                }
                else if (walkStyle == 7) lift = 0.3f + Mathf.Sin(Time.time * 2) * 0.05f;
                else
                {
                    // breathing
                    float br = Mathf.Sin(Time.time * 2.2f);
                    sy = 1 + br * 0.02f; sx = 1 - br * 0.01f;
                    armL = Quaternion.Euler(0, 0, -16 - br * 3); armR = Quaternion.Euler(0, 0, 16 + br * 3);
                }
                // landing squash
                if (land > 0) { sy *= 1 - land * 0.25f; sx *= 1 + land * 0.18f; land = Mathf.MoveTowards(land, 0, dt * 4); }
            }
            else
            {
                // air: stretch going up, tuck coming down; wings out while gliding
                float k = Mathf.Clamp(vy / 12f, -1, 1);
                sy = 1 + Mathf.Max(0, k) * 0.15f; sx = 1 - Mathf.Max(0, k) * 0.08f;
                legL = Quaternion.Euler(-30 + k * 20, 0, -5); legR = Quaternion.Euler(10 - k * 20, 0, 5);
                armL = gliding ? Quaternion.Euler(0, 0, -95) : Quaternion.Euler(-20, 0, -60 - k * 40);
                armR = gliding ? Quaternion.Euler(0, 0, 95) : Quaternion.Euler(-20, 0, 60 + k * 40);
                if (gliding) m.Torso.localRotation = Quaternion.Euler(20, 0, 0);
                if (flipT >= 0 && !gliding)
                {
                    // tucked into a ball for the somersault
                    legL = Quaternion.Euler(-75, 0, -6); legR = Quaternion.Euler(-75, 0, 6);
                    armL = Quaternion.Euler(-70, 0, -25); armR = Quaternion.Euler(-70, 0, 25);
                    sy = 0.92f; sx = 1.04f;
                }
            }

            // emote (idle only)
            if (emoting && speed01 < 0.05f && grounded)
            {
                emoteT += dt;
                Emote(m, ref armL, ref armR, ref legL, ref legR, ref lift, ref sx, ref sy);
                if (!Preview && emoteT > 6f) StopEmote();
            }
            else if (emoting && !Preview) StopEmote();

            // weapon swing overrides the right arm
            if (attackT >= 0)
            {
                attackT += dt;
                float t = attackT / 0.35f;
                if (t >= 1) attackT = -1;
                else armR = Quaternion.Euler(-120 + Mathf.Sin(t * Mathf.PI) * -40 + t * 200, 0, 20);
            }

            m.Body.localPosition += Vector3.up * lift;
            // belly-first: the whole body tips forward round the tummy, arms stretched ahead like a superhero
            bellyK = Mathf.MoveTowards(bellyK, belly ? 1 : 0, dt * 7);
            if (bellyK > 0.001f)
            {
                flipT = -1;
                var r = Quaternion.Euler(84 * bellyK, 0, 0);
                var pivot = new Vector3(0, 0.34f, 0);
                var p0 = m.Body.localPosition;
                m.Body.localRotation = r * m.Body.localRotation;
                m.Body.localPosition = pivot + r * (p0 - pivot) + Vector3.down * 0.06f * bellyK;
                armL = Quaternion.Slerp(armL, Quaternion.Euler(0, 0, -172), bellyK);
                armR = Quaternion.Slerp(armR, Quaternion.Euler(0, 0, 172), bellyK);
                legL = Quaternion.Slerp(legL, Quaternion.Euler(-8, 0, -6), bellyK);
                legR = Quaternion.Slerp(legR, Quaternion.Euler(-8, 0, 6), bellyK);
                if (belly && grounded && speed01 > 0.05f && Time.frameCount % 3 == 0) Effects.Emit("dust", m.transform.position + m.transform.forward * 0.5f, 1, 0.2f);
            }
            if (flipT >= 0)
            {
                flipT += dt;
                float t = flipT / FlipTime;
                if (t >= 1 || grounded || gliding) flipT = -1;
                else
                {
                    // one full forward turn around the belly (not the feet), fast in the middle
                    float e = t * t * (3 - 2 * t);
                    var r = Quaternion.Euler(e * 360, 0, 0);
                    var pivot = new Vector3(0, 0.62f, 0);
                    m.Body.localRotation = r;
                    m.Body.localPosition += pivot - r * pivot;
                }
            }
            m.Body.localScale = new Vector3(sx * Mathf.Sign(bs.x) * Mathf.Abs(BaseScale(m).x), sy * BaseScale(m).y, sx * BaseScale(m).z);
            m.ArmL.localRotation = armL; m.ArmR.localRotation = armR;
            m.LegL.localRotation = legL; m.LegR.localRotation = legR;
            foreach (var f in m.JetFlames) if (f) f.SetActive(!grounded && vy > -2 || gliding);

            // blink
            blinkT -= dt;
            float eye = 1;
            if (blinkT < 0.12f) eye = Mathf.Abs(blinkT - 0.06f) / 0.06f;
            if (blinkT < 0) blinkT = UnityEngine.Random.Range(2f, 5f);
            if (m.EyeL) m.EyeL.localScale = new Vector3(1, Mathf.Max(0.1f, eye * (winkL ? 0.1f : 1)), 1);
            if (m.EyeR) m.EyeR.localScale = new Vector3(1, Mathf.Max(0.1f, eye), 1);
            winkL = false;
        }

        bool winkL;
        const float FlipTime = 0.55f;

        static Vector3 BaseScale(ChubrikModel m)
        {
            int i = m.look.Idx("secret");
            return i == 2 ? new Vector3(0.8f, 1.5f, 0.8f) : i == 3 ? new Vector3(1.1f, 1f, 0.22f) : Vector3.one;
        }

        void Emote(ChubrikModel m, ref Quaternion armL, ref Quaternion armR, ref Quaternion legL, ref Quaternion legR, ref float lift, ref float sx, ref float sy)
        {
            float t = emoteT;
            switch (emote)
            {
                case 0: // breakdance: spin on the back with kicking legs
                {
                    float spin = t * 540;
                    m.Body.localRotation = Quaternion.Euler(0, spin, 0) * Quaternion.Euler(-70, 0, 0);
                    lift = 0.35f;
                    legL = Quaternion.Euler(Mathf.Sin(t * 14) * 60, 0, -30); legR = Quaternion.Euler(-Mathf.Sin(t * 14) * 60, 0, 30);
                    armL = Quaternion.Euler(0, 0, -120); armR = Quaternion.Euler(0, 0, 120);
                    break;
                }
                case 1: // stretching
                {
                    float s = Mathf.Sin(t * 2.5f);
                    armL = Quaternion.Euler(0, 0, -170 + s * 10); armR = Quaternion.Euler(0, 0, 170 + s * 10);
                    m.Torso.localRotation = Quaternion.Euler(0, 0, s * 18);
                    sy *= 1 + Mathf.Abs(s) * 0.06f;
                    break;
                }
                case 2: // looking around
                    m.Head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 8, Mathf.Sin(t * 1.7f) * 55, 0);
                    armL = Quaternion.Euler(0, 0, -14); armR = Quaternion.Euler(-90 + Mathf.Sin(t) * 5, 0, 30);
                    break;
                case 3: // sleeping standing
                    m.Head.localRotation = Quaternion.Euler(18 + Mathf.Sin(t * 1.2f) * 4, 0, 14);
                    sy *= 1 + Mathf.Sin(t * 1.2f) * 0.04f;
                    armL = Quaternion.Euler(0, 0, -8); armR = Quaternion.Euler(0, 0, 8);
                    if (m.EyeL) { winkL = true; }
                    if (m.EyeR) m.EyeR.localScale = new Vector3(1, 0.1f, 1);
                    if (Mathf.Repeat(t, 1.6f) < Time.deltaTime) Effects.Zzz(m.HeadC.position + Vector3.up * 0.5f);
                    break;
                case 4: // juggling a coin
                {
                    if (coin == null)
                    {
                        Parts.Layer = m.gameObject.layer;
                        coin = Parts.Cy(m.transform, Vector3.zero, new Vector3(0.18f, 0.015f, 0.18f), Parts.M("FFC83A", 0.9f, 0.9f));
                    }
                    float c = Mathf.Repeat(t, 0.9f) / 0.9f;
                    armR = Quaternion.Euler(-70 - Mathf.Sin(c * Mathf.PI) * 30, 0, 10);
                    coin.transform.localPosition = new Vector3(0.25f, 0.75f + Mathf.Sin(c * Mathf.PI) * 0.9f, 0.3f);
                    coin.transform.localRotation = Quaternion.Euler(t * 900, 0, 0);
                    m.Head.localRotation = Quaternion.Euler(-Mathf.Sin(c * Mathf.PI) * 25, 10, 0);
                    break;
                }
                case 5: // superhero pose
                    armL = Quaternion.Euler(0, 0, -40) * Quaternion.Euler(0, -60, 0); armR = Quaternion.Euler(0, 0, 40) * Quaternion.Euler(0, 60, 0);
                    m.Torso.localRotation = Quaternion.Euler(-10, 0, 0);
                    m.Head.localRotation = Quaternion.Euler(-10, 15, 0);
                    legL = Quaternion.Euler(0, 0, -12); legR = Quaternion.Euler(0, 0, 12);
                    sy *= 1.04f;
                    break;
                case 6: // shrug
                {
                    float s = Mathf.Clamp01(Mathf.Sin(t * 3) * 1.5f);
                    armL = Quaternion.Euler(-30 * s, 0, -60 - 30 * s); armR = Quaternion.Euler(-30 * s, 0, 60 + 30 * s);
                    m.Head.localRotation = Quaternion.Euler(0, 0, 12 * s);
                    lift = 0.03f * s;
                    break;
                }
                default: // wink
                    m.Head.localRotation = Quaternion.Euler(0, 10, -12);
                    armR = Quaternion.Euler(-100, 0, 30);
                    if (Mathf.Repeat(t, 1.5f) < 0.6f) winkL = true;
                    break;
            }
        }
    }
}
