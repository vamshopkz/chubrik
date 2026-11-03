using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>Weapons and tools held in the right hand, weapon paint.</summary>
    public partial class ChubrikModel
    {
        // ================= weapon =================

        public Material WeaponMat(string fallbackHex, float smooth = 0.6f, float metal = 0.5f)
        {
            int c = look.Idx("wcolor");
            switch (c)
            {
                case 0: return Mats.Tex(Patterns.Skin(3, Hex("3050FF")), Color.white, 0.6f);
                case 1: return M("FFC83A", 0.95f, 0.95f);
                case 2: return G("2E8BFF", 1.4f);
                case 3: return M("8A4B22", 0.1f, 0.3f);
                case 4: return Tex.SkinMat("flames");
                case 5: return Mats.Tex(Patterns.Skin(7, Color.black), Color.white, 0.8f, 0.3f);
                case 6: return Tex.SkinMat("rainbow");
                case 7: return Glass("BFEFFF", 0.7f);
                default: return M(fallbackHex, smooth, metal);
            }
        }

        void WeaponBuild(int i, int set)
        {
            WeaponId = look.Of("weapon");
            Weapon = Node("Weapon", HandR, new Vector3(0, -0.04f, 0.02f), new Vector3(90, 0, 0));
            var w = Weapon;
            var main = WeaponMat("6B7685");
            var dark = M("2A2E36", 0.5f, 0.3f);
            var fwd = Vector3.forward;
            // cylinder along Z (low-poly by default)
            GameObject CZ(Vector3 c, float r, float len, Material m, bool low = true)
                => low ? Lc(w, c, new Vector3(r * 2, len / 2, r * 2), m, new Vector3(90, 0, 0)) : Cy(w, c, new Vector3(r * 2, len / 2, r * 2), m, new Vector3(90, 0, 0));
            // cylinder along X (pins, screws through the body)
            GameObject CX(Vector3 c, float r, float len, Material m) => Lc(w, c, new Vector3(r * 2, len / 2, r * 2), m, new Vector3(0, 0, 90));
            // n boxes around the Z axis (fins, ridges, knurls): size = (tangential, radial, axial)
            void Around(Vector3 c, float rad, int n, Vector3 size, Material m, float a0 = 0)
            {
                for (int k = 0; k < n; k++)
                {
                    float a = (a0 + k * 360f / n) * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    B(w, c + d * rad, size, m).transform.localRotation = Quaternion.LookRotation(fwd, d);
                }
            }
            // the same detail on both X sides: rivets / slots
            void Rivets2(float x, float y, float z0, float z1, int n, float d, Material m)
            {
                for (int k = 0; k < n; k++) CX(new Vector3(0, y, n == 1 ? (z0 + z1) / 2 : Mathf.Lerp(z0, z1, k / (float)(n - 1))), d / 2, x * 2 + 0.006f, m);
            }
            void Slots2(float x, float y, float z0, float z1, int n, Vector3 size, Material m)
            {
                for (int k = 0; k < n; k++) foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * x, y, Mathf.Lerp(z0, z1, (k + 0.5f) / n)), size, m);
            }
            // helix along the shaft (carvings, vines, wraps)
            System.Func<float, Vector3> Helix(float z0, float z1, float r, float turns, float a0 = 0) => t =>
            {
                float a = (a0 + t * turns * 360) * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Lerp(z0, z1, t));
            };
            // box in a local frame (o, q)
            GameObject Bq(Transform par, Vector3 o, Quaternion q, Vector3 lp, Vector3 size, Material m, Vector3 euler = default)
            { var go = B(par, o + q * lp, size, m); go.transform.localRotation = q * Quaternion.Euler(euler); return go; }
            var steel = M("D8DEE6", 0.85f, 0.9f);
            switch (set >= 0 ? 100 + set : i)
            {
                case 0:   // energy blaster: receiver with rail, vents, armour plates, energy cell, finned coiled barrel, emitter prongs, holo sight
                {
                    var glow = G("35E0FF", 2.5f); var rub = M("15171C", 0.1f);
                    B(w, new Vector3(0, 0, 0.12f), new Vector3(0.1f, 0.13f, 0.3f), main);
                    B(w, new Vector3(0, 0.072f, 0.12f), new Vector3(0.085f, 0.02f, 0.26f), main);
                    B(w, new Vector3(0, 0.086f, 0.12f), new Vector3(0.03f, 0.008f, 0.22f), dark);
                    for (int k = 0; k < 11; k++) B(w, new Vector3(0, 0.092f, 0.02f + k * 0.02f), new Vector3(0.036f, 0.006f, 0.007f), dark);
                    B(w, new Vector3(0, -0.068f, 0.21f), new Vector3(0.026f, 0.008f, 0.1f), dark);
                    for (int k = 0; k < 6; k++) B(w, new Vector3(0, -0.074f, 0.17f + k * 0.016f), new Vector3(0.032f, 0.006f, 0.006f), dark);
                    Slots2(0.051f, 0.012f, 0.15f, 0.26f, 4, new Vector3(0.004f, 0.034f, 0.012f), dark);
                    foreach (var s in new[] { -1, 1 })
                    {
                        B(w, new Vector3(s * 0.051f, -0.035f, 0.12f), new Vector3(0.004f, 0.008f, 0.24f), glow);
                        B(w, new Vector3(s * 0.054f, 0.012f, 0.055f), new Vector3(0.006f, 0.06f, 0.1f), main);
                        foreach (var y in new[] { -0.012f, 0.036f }) foreach (var z in new[] { 0.015f, 0.095f })
                            Lc(w, new Vector3(s * 0.058f, y, z), new Vector3(0.01f, 0.002f, 0.01f), steel, new Vector3(0, 0, 90));
                    }
                    Rivets2(0.05f, 0.048f, 0.0f, 0.24f, 4, 0.01f, steel);
                    CZ(new Vector3(0.066f, -0.02f, 0.12f), 0.018f, 0.1f, glow);
                    foreach (var z in new[] { 0.068f, 0.172f }) CZ(new Vector3(0.066f, -0.02f, z), 0.022f, 0.012f, dark);
                    for (int k = 0; k < 3; k++) { float a = k * 120 * Mathf.Deg2Rad; B(w, new Vector3(0.066f + Mathf.Cos(a) * 0.021f, -0.02f + Mathf.Sin(a) * 0.021f, 0.12f), new Vector3(0.005f, 0.005f, 0.1f), dark); }
                    Cy(w, new Vector3(0, 0.02f, 0.32f), new Vector3(0.05f, 0.08f, 0.05f), dark, new Vector3(90, 0, 0));
                    for (int k = 0; k < 7; k++) CZ(new Vector3(0, 0.02f, 0.27f + k * 0.019f), 0.033f, 0.007f, k % 2 == 0 ? glow : main);
                    foreach (var z in new[] { 0.255f, 0.395f }) Around(new Vector3(0, 0.02f, z), 0.034f, 8, new Vector3(0.006f, 0.018f, 0.012f), dark, 22.5f);
                    CZ(new Vector3(0, 0.02f, 0.408f), 0.04f, 0.015f, main);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (45 + k * 90) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                        Spike(w, new Vector3(0, 0.02f, 0.41f) + d * 0.035f, new Vector3(0, 0.02f, 0.465f) + d * 0.024f, 0.014f, main);
                    }
                    Ls(w, new Vector3(0, 0.02f, 0.415f), 0.06f, glow);
                    B(w, new Vector3(0, 0.101f, 0.08f), new Vector3(0.04f, 0.012f, 0.05f), dark);
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * 0.02f, 0.127f, 0.08f), new Vector3(0.005f, 0.04f, 0.006f), dark);
                    B(w, new Vector3(0, 0.149f, 0.08f), new Vector3(0.045f, 0.005f, 0.006f), dark);
                    B(w, new Vector3(0, 0.127f, 0.08f), new Vector3(0.036f, 0.036f, 0.002f), Glass("7FFFD4", 0.35f));
                    Ls(w, new Vector3(0, 0.127f, 0.082f), 0.007f, G("FF3B3B", 3f));
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * 0.012f, 0.104f, 0.0f), new Vector3(0.006f, 0.02f, 0.008f), dark);
                    B(w, new Vector3(0, 0.104f, 0.25f), new Vector3(0.006f, 0.02f, 0.006f), glow);
                    var gc = new Vector3(0, -0.09f, 0.02f); var gq = Quaternion.Euler(-15, 0, 0);
                    Bq(w, gc, gq, Vector3.zero, new Vector3(0.07f, 0.14f, 0.07f), dark);
                    for (int k = 0; k < 6; k++) Bq(w, gc, gq, new Vector3(0, -0.07f + 0.14f * (k + 0.5f) / 6, 0.037f), new Vector3(0.06f, 0.01f, 0.006f), rub);
                    CX(gc + gq * new Vector3(0, 0.02f, 0), 0.006f, 0.076f, steel);
                    Bq(w, gc, gq, new Vector3(0, -0.074f, 0), new Vector3(0.075f, 0.012f, 0.075f), main);
                    B(w, new Vector3(0, -0.078f, 0.08f), new Vector3(0.012f, 0.03f, 0.008f), main, new Vector3(20, 0, 0));
                    B(w, new Vector3(0, -0.105f, 0.08f), new Vector3(0.014f, 0.006f, 0.07f), dark);
                    B(w, new Vector3(0, -0.085f, 0.115f), new Vector3(0.014f, 0.04f, 0.006f), dark);
                    for (int k = 0; k < 3; k++) Ls(w, new Vector3(-0.02f + k * 0.02f, 0.035f, -0.032f), 0.009f, G(k == 0 ? "3DFF6E" : k == 1 ? "FFE14D" : "FF3B3B", 2.4f));
                    B(w, new Vector3(0, -0.005f, -0.031f), new Vector3(0.06f, 0.03f, 0.003f), glow);
                    B(w, new Vector3(-0.054f, 0.05f, 0.2f), new Vector3(0.012f, 0.012f, 0.02f), dark);
                    Ls(w, new Vector3(-0.064f, 0.05f, 0.2f), 0.014f, main);
                    break;
                }
                case 1:   // adjustable wrench: rubber grip with diamond texture, scale marks, jaws with teeth, knurled worm screw, lanyard
                {
                    var grip = M("E53935", 0.2f); var grip2 = M("B71C1C", 0.2f);
                    B(w, new Vector3(0, 0, 0.1f), new Vector3(0.045f, 0.022f, 0.3f), main);
                    Lc(w, new Vector3(0, 0, -0.05f), new Vector3(0.045f, 0.011f, 0.045f), main);
                    Lc(w, new Vector3(0, 0, -0.05f), new Vector3(0.02f, 0.0115f, 0.02f), dark);
                    B(w, new Vector3(0, 0, 0.03f), new Vector3(0.052f, 0.03f, 0.17f), grip);
                    for (int k = 0; k < 8; k++) B(w, new Vector3(0, 0, -0.045f + k * 0.021f), new Vector3(0.056f, 0.034f, 0.006f), grip2);
                    foreach (var s in new[] { -1, 1 })
                        for (int r = 0; r < 4; r++) for (int c = 0; c < 6; c++)
                            B(w, new Vector3(-0.015f + r * 0.01f, s * 0.0165f, -0.035f + c * 0.021f), new Vector3(0.007f, 0.004f, 0.007f), grip2, new Vector3(0, 45, 0));
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * 0.0235f, 0, 0.18f), new Vector3(0.004f, 0.024f, 0.13f), steel);
                    for (int k = 0; k < 6; k++) B(w, new Vector3(-0.012f + (k % 2) * 0.004f, 0.0115f, 0.13f + k * 0.018f), new Vector3(0.016f - (k % 2) * 0.008f, 0.002f, 0.003f), dark);
                    Msh(Meshes.Star(), w, new Vector3(0.008f, 0.0118f, 0.2f), new Vector3(0.016f, 0.016f, 0.004f), grip, new Vector3(90, 0, 0));
                    // head
                    B(w, new Vector3(0.02f, 0, 0.3f), new Vector3(0.11f, 0.03f, 0.07f), main);
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(0.02f + s * 0.05f, 0, 0.268f), new Vector3(0.03f, 0.029f, 0.03f), main, new Vector3(0, s * 40, 0));
                    B(w, new Vector3(0.06f, 0, 0.36f), new Vector3(0.03f, 0.03f, 0.08f), main);
                    B(w, new Vector3(0.05f, 0, 0.405f), new Vector3(0.022f, 0.03f, 0.024f), main, new Vector3(0, 25, 0));
                    B(w, new Vector3(-0.02f, 0, 0.355f), new Vector3(0.03f, 0.03f, 0.07f), main);
                    B(w, new Vector3(-0.01f, 0, 0.395f), new Vector3(0.022f, 0.03f, 0.022f), main, new Vector3(0, -25, 0));
                    for (int k = 0; k < 6; k++)
                    {
                        B(w, new Vector3(0.043f, 0, 0.33f + k * 0.012f), new Vector3(0.006f, 0.03f, 0.005f), dark);
                        B(w, new Vector3(-0.003f, 0, 0.33f + k * 0.011f), new Vector3(0.006f, 0.03f, 0.005f), dark);
                    }
                    for (int k = 0; k < 4; k++) B(w, new Vector3(0.062f, 0.0155f, 0.33f + k * 0.015f), new Vector3(0.012f - (k % 2) * 0.005f, 0.002f, 0.003f), dark);
                    B(w, new Vector3(0.005f, 0.0005f, 0.285f), new Vector3(0.05f, 0.031f, 0.024f), dark);
                    CX(new Vector3(0.005f, 0, 0.285f), 0.012f, 0.044f, steel);
                    for (int k = 0; k < 7; k++) CX(new Vector3(-0.012f + k * 0.0057f, 0, 0.285f), 0.0145f, 0.003f, steel);
                    for (int k = 0; k < 5; k++) B(w, new Vector3(-0.034f + k * 0.007f, 0, 0.318f), new Vector3(0.004f, 0.028f, 0.006f), dark);
                    Lc(w, new Vector3(0.055f, 0, 0.3f), new Vector3(0.012f, 0.017f, 0.012f), steel);
                    // lanyard through the hanging hole
                    Ring(w, new Vector3(0, 0, -0.068f), 0.014f, 0.003f, steel, new Vector3(0, 0, 90));
                    LRod(w, new Vector3(0, 0, -0.082f), new Vector3(0.006f, 0, -0.12f), 0.006f, grip);
                    LRod(w, new Vector3(0.006f, 0, -0.12f), new Vector3(0, 0, -0.15f), 0.006f, grip);
                    Ls(w, new Vector3(0, 0, -0.155f), new Vector3(0.016f, 0.016f, 0.022f), grip2);
                    break;
                }
                case 2:
                case 101:
                {
                    if (set == 1)   // blizzard staff: ice shaft, frost vine, fur grip, snowflake head, orbiting flakes, icicles
                    {
                        var ice = M("BFEFFF", 0.8f); var gi = G("9FE8FF", 2f); var sil = M("E0E8F0", 0.9f, 0.9f);
                        var snow = M("FFFFFF", 0.1f); var gw = G("E8FAFF", 1.6f);
                        Rod(w, new Vector3(0, 0, -0.25f), new Vector3(0, 0, 0.62f), 0.05f, ice);
                        Dt.Stitches(w, Helix(-0.2f, 0.56f, 0.027f, 5), 20, 0.022f, 0.008f, gw);
                        foreach (var z in new[] { -0.22f, 0.0f, 0.3f, 0.58f }) CZ(new Vector3(0, 0, z), 0.031f, 0.014f, sil);
                        Dt.Path(Helix(-0.12f, 0.08f, 0.026f, 3), 14, (p, d, k) => Ls(w, p, 0.026f, snow));
                        var c = new Vector3(0, 0, 0.72f);
                        Ls(w, c, 0.07f, gi);
                        for (int k = 0; k < 6; k++)
                        {
                            float a = (30 + k * 60) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                            var side = new Vector3(-d.z, 0, d.x);
                            LRod(w, c, c + d * 0.13f, 0.014f, ice);
                            foreach (var s in new[] { -1, 1 }) LRod(w, c + d * 0.075f, c + d * 0.11f + side * s * 0.03f, 0.009f, ice);
                            Ls(w, c + d * 0.135f, 0.022f, gi);
                        }
                        for (int k = 0; k < 5; k++)
                        {
                            float a = k * 1.2566f; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            Spike(w, new Vector3(0, 0, 0.6f) + d * 0.02f, new Vector3(0, 0, 0.66f) + d * 0.075f, 0.03f, gi);
                        }
                        var orbit = Node("Flakes", w, c);
                        var sp = Add<Spin>(orbit.gameObject); sp.axis = Vector3.forward; sp.speed = 50;
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k * Mathf.PI / 2; var o = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.17f;
                            for (int r = 0; r < 3; r++) B(orbit, o, new Vector3(0.036f, 0.005f, 0.005f), gw, new Vector3(0, 0, r * 60));
                        }
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 60 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            Spike(w, new Vector3(0, 0, 0.307f) + d * 0.03f, new Vector3(0, 0, 0.37f - (k % 2) * 0.02f) + d * 0.034f, 0.012f, gi);
                        }
                        foreach (var z in new[] { -0.1f, 0.15f, 0.44f })
                            for (int k = 0; k < 3; k++)
                            {
                                float a = (k * 40 + z * 500) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                                Spike(w, new Vector3(0, 0, z + k * 0.01f) + d * 0.02f, new Vector3(0, 0, z + 0.03f + k * 0.01f) + d * 0.06f, 0.016f, gi);
                            }
                        Spike(w, new Vector3(0, 0, -0.25f), new Vector3(0, 0, -0.33f), 0.045f, gi);
                        CZ(new Vector3(0, 0, -0.245f), 0.03f, 0.016f, sil);
                        foreach (var s in new[] { -1, 1 }) Dt.Gem(w, new Vector3(0, s * 0.032f, 0), 0.026f, Glass("9FE8FF", 0.8f), sil, new Vector3(0, s, 0));
                    }
                    else            // magic staff: carved wood, gold bands, leather wrap, claw crown with pulsing orb, orbiting runes, charms
                    {
                        var stick = WeaponMat("6D4425", 0.3f, 0);
                        var gold = M("FFC83A", 0.8f, 0.8f); var groove = M("3E2410", 0.2f); var lea = M("5D3A1E", 0.2f);
                        var rune = G("D9B8FF", 2.4f); var leaf = M("5CC85A", 0.3f); var vio = G("B57CFF", 2.5f);
                        Rod(w, new Vector3(0, 0, -0.25f), new Vector3(0, 0, 0.65f), 0.05f, stick);
                        Dt.Stitches(w, Helix(-0.18f, 0.48f, 0.0255f, 4), 22, 0.024f, 0.009f, groove);
                        foreach (var z in new[] { -0.22f, 0.05f, 0.22f, 0.5f, 0.6f }) CZ(new Vector3(0, 0, z), 0.03f, 0.014f, gold);
                        for (int k = 0; k < 8; k++) Lc(w, new Vector3(0, 0, -0.1f + k * 0.025f), new Vector3(0.058f, 0.006f, 0.058f), lea, new Vector3(75, 0, 0));
                        for (int k = 0; k < 4; k++)
                        {
                            float a = (45 + k * 90) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            var b0 = new Vector3(0, 0, 0.61f) + d * 0.03f; var mid = new Vector3(0, 0, 0.66f) + d * 0.085f; var tip = new Vector3(0, 0, 0.76f) + d * 0.055f;
                            LRod(w, b0, mid, 0.016f, gold);
                            Ls(w, mid, 0.022f, gold);
                            Spike(w, mid, tip, 0.018f, gold);
                            Spike(w, new Vector3(0, 0, 0.6f) + d * 0.028f, new Vector3(0, 0, 0.6f) + d * 0.06f + Vector3.back * 0.02f, 0.016f, vio);
                        }
                        var orb = S(w, new Vector3(0, 0, 0.7f), 0.14f, G("B57CFF", 2.5f)); Add<Pulse>(orb);
                        Ring(w, new Vector3(0, 0, 0.66f), 0.07f, 0.015f, gold, new Vector3(90, 0, 0));
                        var runes = Node("Runes", w, new Vector3(0, 0, 0.7f));
                        var sp = Add<Spin>(runes.gameObject); sp.axis = Vector3.forward; sp.speed = 60;
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 60 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            var q = Quaternion.LookRotation(d, Vector3.forward); var o = d * 0.14f;
                            Bq(runes, o, q, Vector3.zero, new Vector3(0.006f, 0.034f, 0.004f), rune);
                            Bq(runes, o, q, new Vector3(0.007f, k % 2 == 0 ? 0.008f : -0.006f, 0), new Vector3(0.005f, 0.02f, 0.004f), rune, new Vector3(0, 0, k % 3 == 0 ? 40 : -40));
                            Bq(runes, o, q, new Vector3(-0.004f, k % 2 == 0 ? -0.01f : 0.012f, 0), new Vector3(0.016f, 0.005f, 0.004f), rune);
                        }
                        foreach (var s in new[] { -1, 1 })
                        {
                            var top = new Vector3(s * 0.03f, 0, 0.5f); var end = top + new Vector3(s * 0.025f, 0, 0.07f);
                            LRod(w, top, end, 0.005f, lea);
                            Ls(w, end, 0.02f, s > 0 ? vio : gold);
                            Dt.Feather(w, end + Vector3.forward * 0.008f, new Vector3(s * 0.25f, 0, 1), Vector3.up, 0.08f, M(s > 0 ? "4FC3F7" : "FF7043", 0.3f), M("F5F5F5"), 3);
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            float a = (k * 130 + 20) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                            var st = new Vector3(0, 0, 0.12f + k * 0.13f) + d * 0.024f; var lf = st + d * 0.03f + Vector3.forward * 0.02f;
                            LRod(w, st, lf, 0.006f, leaf);
                            Ls(w, lf + d * 0.012f, new Vector3(0.03f, 0.03f, 0.008f), leaf).transform.localRotation = Quaternion.LookRotation(Vector3.Cross(d, Vector3.forward), d);
                        }
                        CZ(new Vector3(0, 0, -0.245f), 0.03f, 0.018f, gold);
                        Spike(w, new Vector3(0, 0, -0.25f), new Vector3(0, 0, -0.31f), 0.04f, steel);
                        foreach (var z in new[] { 0.05f, 0.22f }) Dt.Gem(w, new Vector3(0, 0.032f, z), 0.022f, G(z > 0.1f ? "35E0FF" : "3DFF6E", 1.6f), gold, Vector3.up);
                    }
                    break;
                }
                case 3:   // lightsaber: hilt with grip pads, choke rings, vented emitter shroud, switch box, cell windows, pommel with ring, belt clip
                {
                    string col = "35E0FF";
                    var glow = G(col, 2.6f); var rub = M("15171C", 0.1f);
                    Cy(w, Vector3.zero, new Vector3(0.052f, 0.08f, 0.052f), main, new Vector3(90, 0, 0));
                    for (int r = 0; r < 5; r++) Around(new Vector3(0, 0, -0.06f + r * 0.019f), 0.027f, 8, new Vector3(0.012f, 0.006f, 0.016f), rub, r * 22.5f);
                    for (int k = 0; k < 3; k++) CZ(new Vector3(0, 0, 0.035f + k * 0.01f), 0.03f, 0.005f, dark);
                    CZ(new Vector3(0, 0, 0.088f), 0.032f, 0.04f, main);
                    Around(new Vector3(0, 0, 0.098f), 0.032f, 6, new Vector3(0.008f, 0.006f, 0.022f), dark);
                    Around(new Vector3(0, 0, 0.076f), 0.0325f, 8, new Vector3(0.006f, 0.004f, 0.008f), dark, 22.5f);
                    for (int k = 0; k < 4; k++) { float a = (45 + k * 90) * Mathf.Deg2Rad; Ls(w, new Vector3(Mathf.Cos(a) * 0.033f, Mathf.Sin(a) * 0.033f, 0.1f), 0.007f, steel); }
                    CZ(new Vector3(0, 0, 0.11f), 0.027f, 0.004f, glow);
                    B(w, new Vector3(0, 0.03f, 0.055f), new Vector3(0.02f, 0.014f, 0.035f), main);
                    Ls(w, new Vector3(0, 0.038f, 0.062f), 0.01f, G("FF3B3B", 2.5f));
                    B(w, new Vector3(0, 0.038f, 0.046f), new Vector3(0.006f, 0.004f, 0.01f), steel);
                    foreach (var s in new[] { -1, 1 })
                    {
                        B(w, new Vector3(s * 0.027f, 0, 0.0f), new Vector3(0.003f, 0.016f, 0.036f), dark);
                        B(w, new Vector3(s * 0.029f, 0, 0.0f), new Vector3(0.002f, 0.01f, 0.03f), glow);
                    }
                    CZ(new Vector3(0, 0, -0.088f), 0.03f, 0.02f, main);
                    Around(new Vector3(0, 0, -0.088f), 0.03f, 8, new Vector3(0.006f, 0.006f, 0.018f), dark);
                    Ls(w, new Vector3(0, 0, -0.1f), new Vector3(0.04f, 0.04f, 0.016f), dark);
                    Ring(w, new Vector3(0, 0, -0.112f), 0.012f, 0.003f, steel, new Vector3(0, 0, 90));
                    B(w, new Vector3(-0.03f, 0, -0.03f), new Vector3(0.004f, 0.016f, 0.06f), steel);
                    foreach (var z in new[] { -0.05f, -0.01f }) Ls(w, new Vector3(-0.032f, 0, z), 0.007f, dark);
                    for (int k = 0; k < 4; k++) B(w, new Vector3(0.012f, -0.027f, -0.06f + k * 0.012f), new Vector3(0.008f, 0.003f, 0.004f + (k % 2) * 0.004f), dark);
                    for (int k = 0; k < 4; k++) Ls(w, new Vector3(-0.012f, -0.028f, -0.06f + k * 0.012f), 0.005f, G(k % 2 == 0 ? col : "FFFFFF", 2.4f));
                    // blade: core, two halos, rings at the base, tip and small plasma arcs
                    Cy(w, new Vector3(0, 0, 0.48f), new Vector3(0.045f, 0.4f, 0.045f), glow, new Vector3(90, 0, 0));
                    Cy(w, new Vector3(0, 0, 0.48f), new Vector3(0.08f, 0.41f, 0.08f), Add(col, 0.35f), new Vector3(90, 0, 0));
                    Lc(w, new Vector3(0, 0, 0.48f), new Vector3(0.11f, 0.42f, 0.11f), Add(col, 0.12f), new Vector3(90, 0, 0));
                    Ls(w, new Vector3(0, 0, 0.88f), 0.045f, glow);
                    Ls(w, new Vector3(0, 0, 0.885f), 0.08f, Add(col, 0.3f));
                    for (int k = 0; k < 3; k++) CZ(new Vector3(0, 0, 0.125f + k * 0.02f), 0.034f - k * 0.004f, 0.003f, Add("FFFFFF", 0.5f));
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 60 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                        LRod(w, new Vector3(0, 0, 0.12f) + d * 0.02f, new Vector3(0, 0, 0.15f + (k % 3) * 0.02f) + d * 0.038f, 0.004f, G("E0FFFF", 3f));
                    }
                    break;
                }
                case 103:
                case 104:
                {
                    bool plasma = set == 4;
                    string col = plasma ? "FF2EB4" : "6B2BFF";
                    var core = plasma ? M("2A2E36", 0.5f, 0.3f) : M("F2EEE6", 0.3f);
                    var band = plasma ? M("15151C", 0.6f) : M("1A1426", 0.3f);
                    var gold = M("FFC83A", 0.8f, 0.8f); var gold2 = M("C8962A", 0.7f, 0.8f);
                    var blade = plasma ? G(col, 2f) : M("1A1426", 0.95f, 0.8f);
                    var edge = G(plasma ? "26F0FF" : col, 3f);
                    var cord = M(plasma ? "26F0FF" : "6B2BFF", 0.3f);
                    // tsuka: oval handle with crossing wrap bands, fuchi, kashira, menuki, mekugi pin, sageo tassel
                    Cy(w, new Vector3(0, 0, -0.02f), new Vector3(0.04f, 0.1f, 0.05f), core, new Vector3(90, 0, 0));
                    for (int k = 0; k < 9; k++) foreach (var s in new[] { -1, 1 })
                        B(w, new Vector3(0, 0, -0.112f + k * 0.0235f), new Vector3(0.046f, 0.056f, 0.01f), band, new Vector3(0, s * 28, 0));
                    Lc(w, new Vector3(0, 0, -0.125f), new Vector3(0.046f, 0.008f, 0.056f), gold, new Vector3(90, 0, 0));
                    Ls(w, new Vector3(0, 0, -0.133f), new Vector3(0.03f, 0.036f, 0.012f), gold2);
                    CX(new Vector3(0, -0.012f, -0.128f), 0.004f, 0.05f, gold2);
                    Lc(w, new Vector3(0, 0, 0.075f), new Vector3(0.046f, 0.008f, 0.056f), gold, new Vector3(90, 0, 0));
                    foreach (var s in new[] { -1, 1 })
                    {
                        Ls(w, new Vector3(s * 0.024f, 0, -0.02f), new Vector3(0.008f, 0.016f, 0.032f), gold);
                        B(w, new Vector3(s * 0.026f, 0, -0.02f), new Vector3(0.003f, 0.004f, 0.02f), gold2);
                    }
                    CX(new Vector3(0, 0, 0.045f), 0.004f, 0.046f, plasma ? edge : M("D7B98E", 0.2f));
                    for (int k = 0; k < 6; k++) Ls(w, new Vector3(0.004f * (k % 2), -0.02f - k * 0.008f, -0.14f - k * 0.006f), 0.01f, cord);
                    for (int k = 0; k < 4; k++) B(w, new Vector3((k - 1.5f) * 0.005f, -0.075f, -0.18f), new Vector3(0.003f, 0.03f, 0.003f), cord, new Vector3(0, 0, (k - 1.5f) * 8));
                    // tsuba with rim, seppa washers and petal ornaments on both faces
                    Cy(w, new Vector3(0, 0, 0.09f), new Vector3(0.12f, 0.01f, 0.12f), gold, new Vector3(90, 0, 0));
                    Ring(w, new Vector3(0, 0, 0.09f), 0.058f, 0.006f, gold2, new Vector3(90, 0, 0));
                    foreach (var z in new[] { 0.081f, 0.099f }) Lc(w, new Vector3(0, 0, z), new Vector3(0.05f, 0.002f, 0.06f), gold2, new Vector3(90, 0, 0));
                    for (int k = 0; k < 8; k++) foreach (var z in new[] { 0.0795f, 0.1005f })
                    {
                        float a = (22.5f + k * 45) * Mathf.Deg2Rad;
                        Ls(w, new Vector3(Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, z), new Vector3(0.014f, 0.014f, 0.004f), plasma ? edge : M("1A1426", 0.6f));
                    }
                    // habaki + curved blade (segments), shinogi ridges, glowing edge, spine, kissaki tip
                    B(w, new Vector3(0, 0.01f, 0.112f), new Vector3(0.026f, 0.07f, 0.025f), gold);
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * 0.0135f, 0.01f, 0.112f), new Vector3(0.002f, 0.05f, 0.004f), gold2);
                    const int N = 10;
                    float Yc(float t) => 0.01f - 0.06f * t * t;
                    for (int k = 0; k < N; k++)
                    {
                        float t = (k + 0.5f) / N, z = 0.1f + t * 0.8f;
                        float ang = Mathf.Atan(0.12f * t / 0.8f) * Mathf.Rad2Deg;
                        float h = Mathf.Lerp(0.06f, 0.048f, t);
                        var q = Quaternion.Euler(ang, 0, 0);
                        var c = new Vector3(0, Yc(t), z);
                        B(w, c, new Vector3(0.02f, h, 0.084f), blade).transform.localRotation = q;
                        B(w, c + q * new Vector3(0, h / 2 + 0.004f, 0), new Vector3(0.021f, 0.01f, 0.084f), edge).transform.localRotation = q;
                        if (k % 2 == 0)
                        {
                            var c2 = new Vector3(0, Yc(t + 0.05f), z + 0.04f);
                            foreach (var s in new[] { -1, 1 }) B(w, c2 + new Vector3(s * 0.0105f, -0.006f, 0), new Vector3(0.003f, 0.004f, 0.15f), plasma ? edge : steel).transform.localRotation = q;
                            B(w, c2 + q * new Vector3(0, -h / 2, 0), new Vector3(0.022f, 0.005f, 0.15f), plasma ? band : M("0E0A16", 0.9f, 0.8f)).transform.localRotation = q;
                        }
                        Ls(w, c + q * new Vector3(0.011f * (k % 2 == 0 ? 1 : -1), h / 2 - 0.008f, 0), new Vector3(0.004f, 0.008f, 0.02f), edge);
                    }
                    float tipA = Mathf.Atan(0.12f / 0.8f) * Mathf.Rad2Deg;
                    var tq = Quaternion.Euler(tipA + 40, 0, 0);
                    B(w, new Vector3(0, Yc(1) - 0.012f, 0.905f), new Vector3(0.019f, 0.04f, 0.05f), blade).transform.localRotation = tq;
                    B(w, new Vector3(0, Yc(1) + 0.006f, 0.92f), new Vector3(0.02f, 0.008f, 0.045f), edge).transform.localRotation = tq;
                    if (plasma)
                    {
                        // plasma: emitter prongs at the habaki, arcs along the flats, energy nodes on the spine, glowing pommel core
                        for (int k = 0; k < 4; k++)
                        {
                            float a = (45 + k * 90) * Mathf.Deg2Rad;
                            Spike(w, new Vector3(Mathf.Cos(a) * 0.02f, 0.01f + Mathf.Sin(a) * 0.03f, 0.12f), new Vector3(Mathf.Cos(a) * 0.016f, 0.01f + Mathf.Sin(a) * 0.022f, 0.16f), 0.01f, steel);
                        }
                        for (int k = 0; k < 8; k++)
                        {
                            float t = 0.1f + k * 0.1f;
                            foreach (var s in new[] { -1, 1 })
                                B(w, new Vector3(s * 0.0115f, Yc(t) + (k % 2 == 0 ? 0.008f : -0.008f), 0.1f + t * 0.8f), new Vector3(0.002f, 0.004f, 0.05f), Add("FFFFFF", 0.7f), new Vector3(k % 2 == 0 ? 25 : -25, 0, 0));
                        }
                        for (int k = 0; k < 6; k++) { float t = 0.12f + k * 0.15f; Ls(w, new Vector3(0, Yc(t) - 0.03f, 0.1f + t * 0.8f), 0.012f, edge); }
                        Ls(w, new Vector3(0, 0, -0.142f), 0.022f, edge);
                    }
                    else
                    {
                        // shadow wisps drifting along the blade
                        for (int k = 0; k < 6; k++) Ls(w, new Vector3((k % 2 == 0 ? 1 : -1) * 0.025f, Yc(k / 6f) + 0.01f, 0.2f + k * 0.12f), 0.04f + (k % 3) * 0.01f, Add(col, 0.25f));
                    }
                    break;
                }
                case 4:   // pixel slingshot: two voxel layers, pixel highlights, pixel band with pouch and pebble, ties, lanyard
                {
                    var wood = WeaponMat("8D5524", 0.2f, 0);
                    var woodD = look.Idx("wcolor") >= 0 ? wood : M("6B3F1A", 0.2f);
                    var lite = M("FFD54F", 0.3f); var red = M("E53935"); var red2 = M("9A1C1C"); var pink = M("FF3FA4");
                    string[] px = {
                        "WW.......WW", "AA.......AA", "AA.......AA", "AA.......AA", "AAA.....AAA", ".AA.....AA.", ".AAA...AAA.", "..AAA.AAA..",
                        "...AAAAA...", "....HHH....", "....GGG....", "....HHH....", "....GGG....", "....HHH....", "....GGG....", "....DDD...." };
                    const float s = 0.026f;
                    Vector3 V(int c, int j, float y) => new Vector3((c - 5) * s, y, (15 - j) * s - 0.06f);
                    for (int j = 0; j < px.Length; j++)
                        for (int c = 0; c < 11; c++)
                        {
                            char ch = px[j][c];
                            if (ch == '.') continue;
                            var mf = ch == 'W' ? lite : ch == 'H' ? red : ch == 'G' ? red2 : ch == 'D' ? dark : wood;
                            var mb = ch == 'A' ? woodD : mf;
                            B(w, V(c, j, 0.012f), new Vector3(s, 0.024f, s), mf);
                            B(w, V(c, j, -0.012f), new Vector3(s, 0.024f, s), mb);
                        }
                    for (int j = 1; j <= 4; j++) foreach (var c in new[] { 0, 9 }) B(w, V(c, j, 0.026f), new Vector3(s * 0.45f, 0.004f, s * 0.8f), lite);
                    foreach (var c in new[] { 2, 3, 7, 8 }) B(w, V(c, 0, 0), new Vector3(s, 0.012f, s * 0.6f), red);
                    for (int c = 4; c <= 6; c++) for (int j = 0; j <= 1; j++) B(w, V(c, j, 0), new Vector3(s, 0.03f, s), pink);
                    B(w, V(5, 0, 0.026f), new Vector3(s * 1.2f, 0.02f, s * 1.2f), M("9AA3AE", 0.4f));
                    B(w, V(5, 0, 0.037f) + new Vector3(-0.006f, 0, 0.006f), new Vector3(s * 0.35f, 0.004f, s * 0.35f), M("FFFFFF"));
                    foreach (var c in new[] { 1, 9 }) B(w, V(c, 0, 0.026f), new Vector3(s * 0.6f, 0.006f, s * 0.6f), dark);
                    for (int k = 0; k < 4; k++) B(w, V(5, 16 + k, 0), new Vector3(s * 0.6f, 0.012f, s * 0.9f), k % 2 == 0 ? red : lite);
                    break;
                }
                case 5:   // boomerang: dot painting on both faces, stripe bands, edge binding, sun emblem, leather wrap
                {
                    var bm = WeaponMat("FF7A1E", 0.4f, 0);
                    Msh(Meshes.Fan("boom", new[] { new Vector2(-0.3f, 0.05f), new Vector2(0, 0.2f), new Vector2(0.3f, 0.05f), new Vector2(0.28f, -0.03f), new Vector2(0, 0.1f), new Vector2(-0.28f, -0.03f) }, 0.04f), w, new Vector3(0, 0, 0.18f), Vector3.one, bm, new Vector3(90, 0, 0));
                    var dotM = new[] { M("FFFFFF", 0.3f), M("FFD23A", 0.3f), M("B5341C", 0.3f) };
                    var bandM = M("3A2418", 0.3f); var lea = M("6B3F1A", 0.2f);
                    Vector3 L(Vector2 m, float y) => new Vector3(m.x, y, m.y + 0.18f);
                    foreach (var sx in new[] { -1, 1 })
                    {
                        var p0 = new Vector2(0, 0.15f); var p1 = new Vector2(sx * 0.27f, 0.02f);
                        var dir = (p1 - p0).normalized; var perp = new Vector2(-dir.y, dir.x);
                        var d3 = new Vector3(dir.x, 0, dir.y); var pp3 = new Vector3(perp.x, 0, perp.y);
                        foreach (var face in new[] { -1, 1 })
                        {
                            for (int r = -1; r <= 1; r++)
                            {
                                int n = r == 0 ? 8 : 7;
                                for (int k = 0; k < n; k++)
                                {
                                    float t = 0.05f + (k + 0.5f) / n * 0.5f;
                                    B(w, L(p0 + (p1 - p0) * t + perp * r * 0.022f, face * 0.0215f), new Vector3(0.011f, 0.003f, 0.011f), dotM[r + 1], new Vector3(0, 45, 0));
                                }
                            }
                            foreach (var t in new[] { 0.62f, 0.7f })
                                B(w, L(p0 + (p1 - p0) * t, face * 0.0215f), new Vector3(0.09f, 0.003f, 0.012f), bandM).transform.localRotation = Quaternion.LookRotation(d3, Vector3.up);
                        }
                        var o0 = new Vector2(0, 0.2f); var o1 = new Vector2(sx * 0.3f, 0.05f);
                        var od = (o1 - o0).normalized; var on = new Vector3(-od.y * sx, 0, od.x * sx);
                        for (int k = 0; k < 5; k++)
                        {
                            var pt = Vector2.Lerp(o0, o1, (k + 0.5f) / 5);
                            B(w, L(pt, 0) + on.normalized * 0.002f, new Vector3(0.006f, 0.044f, 0.062f), bandM).transform.localRotation = Quaternion.LookRotation(new Vector3(od.x, 0, od.y), Vector3.up);
                        }
                        if (sx < 0)
                            for (int k = 0; k < 4; k++)
                                B(w, L(p0 + (p1 - p0) * (0.8f + k * 0.045f), 0), new Vector3(0.095f, 0.046f, 0.012f), lea).transform.localRotation = Quaternion.LookRotation(d3, Vector3.up);
                    }
                    Msh(Meshes.Star(8, 0.55f), w, L(new Vector2(0, 0.155f), 0.022f), new Vector3(0.05f, 0.05f, 0.006f), dotM[1], new Vector3(90, 0, 0));
                    Ls(w, L(new Vector2(0, 0.155f), 0.025f), new Vector3(0.022f, 0.006f, 0.022f), dotM[2]);
                    break;
                }
                case 6:   // lollipop club: swirl disc facing forward, sugar-bead rim, raised swirl, sprinkles, ribbon bow, paper stick
                {
                    var stickM = M("FFFFFF", 0.3f);
                    var candy = Mats.Tex(Patterns.Swirl(), look.Idx("wcolor") >= 0 ? (Color)WeaponMat("FF4FA0").color : Color.white, 0.6f);
                    var c = new Vector3(0, 0, 0.55f);
                    Rod(w, new Vector3(0, 0, -0.08f), new Vector3(0, 0, 0.42f), 0.03f, stickM);
                    for (int k = 0; k < 3; k++) CZ(new Vector3(0, 0, -0.04f + k * 0.04f), 0.0165f, 0.004f, M("E6E9ED", 0.3f));
                    Cy(w, c, new Vector3(0.32f, 0.03f, 0.32f), candy);
                    var bw = M("FFFFFF", 0.7f); var bp = M("FF7AB8", 0.7f);
                    for (int k = 0; k < 24; k++) { float a = k * Mathf.PI * 2 / 24; Ls(w, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.162f, 0.034f, k % 2 == 0 ? bw : bp); }
                    Dt.Stitches(w, t => c + new Vector3(Mathf.Cos(t * Mathf.PI * 5) * (0.02f + 0.12f * t), 0.032f, Mathf.Sin(t * Mathf.PI * 5) * (0.02f + 0.12f * t)), 28, 0.04f, 0.012f, M("FFFFFF", 0.8f));
                    var rng = new Rng(31);
                    string[] sp = { "FFD23A", "3DFF6E", "26C6FF", "B57CFF", "FF3B3B", "FFFFFF" };
                    foreach (var face in new[] { -1, 1 })
                        for (int k = 0; k < 20; k++)
                        {
                            float a = rng.F(0, Mathf.PI * 2), rad = rng.F(0.03f, 0.135f);
                            B(w, c + new Vector3(Mathf.Cos(a) * rad, face * 0.0325f, Mathf.Sin(a) * rad), new Vector3(0.022f, 0.006f, 0.007f), M(sp[k % 6], 0.6f), new Vector3(0, rng.F(0, 180), 0));
                        }
                    var rib = M("FF3FA4", 0.4f);
                    Ls(w, new Vector3(0, 0, 0.385f), 0.026f, rib);
                    foreach (var s in new[] { -1, 1 })
                    {
                        Ls(w, new Vector3(s * 0.035f, 0, 0.378f), new Vector3(0.05f, 0.014f, 0.03f), rib).transform.localRotation = Quaternion.Euler(0, s * 20, 0);
                        B(w, new Vector3(s * 0.02f, 0, 0.34f), new Vector3(0.016f, 0.004f, 0.06f), rib, new Vector3(0, -s * 20, 0));
                    }
                    var glint = G("FFFFFF", 2f);
                    for (int k = 0; k < 6; k++) { float a = (k * 60 + 15) * Mathf.Deg2Rad; Ls(w, c + new Vector3(Mathf.Cos(a) * 0.1f, 0.036f, Mathf.Sin(a) * 0.1f), 0.012f, glint); }
                    break;
                }
                case 7:   // cyber knife: scaled handle with diamond pads and glow lines, guard with prongs, tanto blade with serrations, circuits, edge nodes
                {
                    var glow = G("FF3FA4", 2.5f); var cyan = G("26F0FF", 2.5f);
                    B(w, Vector3.zero, new Vector3(0.05f, 0.06f, 0.14f), dark);
                    foreach (var s in new[] { -1, 1 }) B(w, new Vector3(s * 0.027f, 0, 0), new Vector3(0.006f, 0.05f, 0.12f), main);
                    Rivets2(0.027f, 0, -0.045f, 0.045f, 3, 0.01f, steel);
                    for (int k = 0; k < 4; k++) B(w, new Vector3(0, 0.031f, -0.045f + k * 0.03f), new Vector3(0.044f, 0.006f, 0.014f), dark);
                    foreach (var s in new[] { -1, 1 })
                    {
                        foreach (var yy in new[] { -0.021f, 0.021f }) B(w, new Vector3(s * 0.0305f, yy, 0), new Vector3(0.002f, 0.003f, 0.1f), cyan);
                        for (int r = 0; r < 3; r++) for (int k = 0; k < 4; k++)
                            B(w, new Vector3(s * 0.0305f, -0.01f + r * 0.01f, -0.05f + k * 0.02f), new Vector3(0.003f, 0.007f, 0.007f), dark, new Vector3(45, 0, 0));
                        B(w, new Vector3(s * 0.0305f, 0, 0.04f), new Vector3(0.002f, 0.012f, 0.025f), glow);
                    }
                    B(w, new Vector3(0, 0.005f, 0.075f), new Vector3(0.03f, 0.095f, 0.012f), dark);
                    Spike(w, new Vector3(0, 0.05f, 0.075f), new Vector3(0, 0.06f, 0.1f), 0.014f, main);
                    Spike(w, new Vector3(0, -0.04f, 0.075f), new Vector3(0, -0.05f, 0.1f), 0.014f, main);
                    foreach (var yy in new[] { -0.03f, 0.03f }) CX(new Vector3(0, yy, 0.075f), 0.005f, 0.034f, steel);
                    foreach (var s in new[] { -1, 1 }) Ls(w, new Vector3(s * 0.016f, 0.012f, 0.075f), 0.008f, glow);
                    B(w, new Vector3(0, 0, -0.08f), new Vector3(0.04f, 0.05f, 0.02f), dark);
                    Spike(w, new Vector3(0, 0, -0.09f), new Vector3(0, 0, -0.12f), 0.02f, steel);
                    CX(new Vector3(0, 0.012f, -0.08f), 0.006f, 0.042f, steel);
                    B(w, new Vector3(0, -0.031f, 0), new Vector3(0.03f, 0.002f, 0.04f), cyan);
                    for (int k = 0; k < 3; k++) B(w, new Vector3(-0.008f + k * 0.008f, -0.0325f, -0.005f + k * 0.004f), new Vector3(0.004f, 0.002f, 0.012f + k * 0.008f), glow);
                    for (int k = 0; k < 6; k++) B(w, new Vector3(0, -0.031f, 0.035f + k * 0.005f), new Vector3(0.03f, 0.004f, 0.003f), steel);
                    // blade
                    B(w, new Vector3(0, 0.01f, 0.17f), new Vector3(0.015f, 0.06f, 0.22f), main);
                    B(w, new Vector3(0, 0.045f, 0.17f), new Vector3(0.016f, 0.01f, 0.22f), glow);
                    B(w, new Vector3(0, 0.0f, 0.29f), new Vector3(0.0148f, 0.045f, 0.04f), main, new Vector3(40, 0, 0));
                    B(w, new Vector3(0, 0.03f, 0.29f), new Vector3(0.016f, 0.008f, 0.05f), glow, new Vector3(40, 0, 0));
                    for (int k = 0; k < 8; k++) { float z = 0.1f + k * 0.018f; Spike(w, new Vector3(0, -0.019f, z), new Vector3(0, -0.032f, z + 0.006f), 0.012f, main); }
                    foreach (var s in new[] { -1, 1 })
                    {
                        for (int k = 0; k < 3; k++) B(w, new Vector3(s * 0.0078f, 0.005f, 0.11f + k * 0.04f), new Vector3(0.001f, 0.012f, 0.022f), dark);
                        foreach (var yy in new[] { -0.008f, -0.002f }) B(w, new Vector3(s * 0.0078f, yy, 0.165f + yy * 3), new Vector3(0.001f, 0.003f, 0.06f), cyan);
                        foreach (var z in new[] { 0.135f, 0.2f }) Ls(w, new Vector3(s * 0.008f, -0.005f, z), 0.007f, cyan);
                        for (int k = 0; k < 5; k++) B(w, new Vector3(s * 0.0078f, -0.012f, 0.07f + k * 0.005f), new Vector3(0.001f, 0.003f + (k % 2) * 0.002f, 0.0035f), dark);
                    }
                    for (int k = 0; k < 8; k++) Ls(w, new Vector3(0, 0.05f, 0.075f + k * 0.027f), 0.009f, G("FFFFFF", 2.5f));
                    break;
                }
                case 107:   // royal sceptre: twisted gold shaft, velvet grip, pearl collar, set gems, crown of arches over the ruby, cross, tassels
                {
                    var gold = WeaponMat("FFD700", 0.95f, 0.95f);
                    var gold2 = M("C8962A", 0.8f, 0.9f); var pearl = M("FFF8EE", 0.8f); var velvet = M("6A1B9A", 0.1f);
                    Rod(w, new Vector3(0, 0, -0.2f), new Vector3(0, 0, 0.55f), 0.045f, gold);
                    foreach (var a0 in new[] { 0f, 180f }) Dt.Stitches(w, Helix(0.07f, 0.43f, 0.0225f, 3, a0), 14, 0.026f, 0.01f, gold2);
                    foreach (var z in new[] { -0.16f, 0.06f, 0.3f, 0.45f }) CZ(new Vector3(0, 0, z), 0.03f, 0.016f, gold2);
                    for (int k = 0; k < 10; k++) { float a = k * 36 * Mathf.Deg2Rad; Ls(w, new Vector3(Mathf.Cos(a) * 0.031f, Mathf.Sin(a) * 0.031f, 0.45f), 0.012f, pearl); }
                    CZ(new Vector3(0, 0, -0.04f), 0.026f, 0.18f, velvet);
                    for (int k = 0; k < 5; k++) CZ(new Vector3(0, 0, -0.11f + k * 0.035f), 0.0275f, 0.004f, gold);
                    Dt.Gem(w, new Vector3(0, 0.026f, 0.2f), 0.024f, G("1E63D9", 1.4f), gold2, Vector3.up);
                    Dt.Gem(w, new Vector3(0, -0.026f, 0.2f), 0.024f, G("2ECC71", 1.4f), gold2, Vector3.down);
                    Dt.Gem(w, new Vector3(0.026f, 0, 0.37f), 0.022f, G("B57CFF", 1.4f), gold2, Vector3.right);
                    Dt.Gem(w, new Vector3(-0.026f, 0, 0.37f), 0.022f, G("35E0FF", 1.4f), gold2, Vector3.left);
                    CZ(new Vector3(0, 0, 0.52f), 0.05f, 0.03f, gold);
                    S(w, new Vector3(0, 0, 0.62f), 0.15f, G("E53935", 1.6f));
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 72 * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                        var p0 = new Vector3(0, 0, 0.53f) + d * 0.045f; var p1 = new Vector3(0, 0, 0.6f) + d * 0.088f;
                        var p2 = new Vector3(0, 0, 0.69f) + d * 0.07f; var p3 = new Vector3(0, 0, 0.745f) + d * 0.015f;
                        LRod(w, p0, p1, 0.014f, gold); LRod(w, p1, p2, 0.014f, gold); LRod(w, p2, p3, 0.012f, gold);
                        Ls(w, p2, 0.018f, pearl);
                        var tip = p1 + d * 0.04f + fwd * 0.02f;
                        Spike(w, p1, tip, 0.016f, gold);
                        Ls(w, tip, 0.014f, pearl);
                    }
                    Ls(w, new Vector3(0, 0, 0.76f), 0.035f, gold);
                    B(w, new Vector3(0, 0, 0.8f), new Vector3(0.012f, 0.012f, 0.05f), gold);
                    B(w, new Vector3(0, 0, 0.805f), new Vector3(0.04f, 0.012f, 0.012f), gold);
                    Ls(w, new Vector3(0, 0, -0.215f), new Vector3(0.07f, 0.07f, 0.06f), gold);
                    Spike(w, new Vector3(0, 0, -0.24f), new Vector3(0, 0, -0.29f), 0.03f, gold2);
                    Dt.Gem(w, new Vector3(0, 0.031f, -0.215f), 0.02f, G("E53935", 1.6f), gold2, Vector3.up);
                    foreach (var s in new[] { -1, 1 })
                    {
                        var top = new Vector3(s * 0.032f, 0, 0.44f); var knot = top + new Vector3(s * 0.02f, 0, 0.04f);
                        LRod(w, top, knot, 0.005f, velvet);
                        Ls(w, knot, 0.016f, gold);
                        Spike(w, knot + fwd * 0.06f, knot, 0.03f, velvet);
                    }
                    break;
                }
            }
        }
    }
}
