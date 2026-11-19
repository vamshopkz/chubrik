using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// 19.11 (v0.27): 1000 more skins — every wardrobe thing again in 10 «finishes»: gold, chrome, neon, rainbow,
    /// galaxy, ice, lava, shadow, candy, emerald. A finish turns each material of the thing while it is built
    /// (Parts.Finish), keeping its light and dark parts, so all the details stay.
    /// </summary>
    public static class SkinFinish
    {
        public static readonly string[] Names = { "золото", "хром", "неон", "радуга", "галактика", "лёд", "лава", "тень", "конфета", "изумруд" };
        public const int Count = 10;
        static readonly Dictionary<(Material, int), Material> cache = new Dictionary<(Material, int), Material>();

        /// <summary>0 ordinary … 3 legendary (the lucky blocks).</summary>
        public static int Rarity(int f) => f == 0 || f == 4 ? 3 : f == 2 || f == 6 || f == 9 ? 2 : 1;

        public static System.Func<Material, Material> For(int f)
        {
            if (f < 0) return null;
            return m => Apply(m, f);
        }

        static Color BaseColor(Material m) => m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;

        public static Material Apply(Material m, int f)
        {
            if (m == null) return null;
            if (cache.TryGetValue((m, f), out var have) && have) return have;
            var c = BaseColor(m);
            float L = Mathf.Clamp01(c.r * 0.3f + c.g * 0.59f + c.b * 0.11f);
            Color.RGBToHSV(c, out var h, out var s, out var v);
            Material r;
            switch (f)
            {
                case 0: r = Mats.Lit(Color.Lerp(Hex("8A5A08"), Hex("FFE27A"), L), 0.92f, 0.95f); break;                         // gold
                case 1: r = Mats.Lit(Color.Lerp(Hex("3A4048"), Hex("F2F6FA"), L), 0.96f, 1f); break;                           // chrome
                case 2: { var n = Color.HSVToRGB(h, 0.85f, 1f); r = Mats.Emissive(null, n, n * (0.6f + L), null, 0.7f); break; } // neon
                case 3: { var n = Color.HSVToRGB(Mathf.Repeat(h + L * 0.9f + 0.15f, 1f), 0.75f, 0.95f); r = Mats.Lit(n, 0.6f); break; } // rainbow
                case 4: { var n = Color.Lerp(Hex("120A2E"), Hex("7A4CFF"), L); r = Mats.Emissive(null, n, Hex("3A1C9A") * (0.3f + L * 0.6f), null, 0.85f); break; } // galaxy
                case 5: r = Parts.Glass(ColorUtility.ToHtmlStringRGB(Color.Lerp(Hex("7FD0FF"), Hex("EAF8FF"), L)), 0.7f); break;  // ice
                case 6: { var n = Color.Lerp(Hex("3A0A00"), Hex("FF7A1A"), L); r = Mats.Emissive(null, n, Hex("FF4A00") * (0.2f + L), null, 0.5f); break; } // lava
                case 7: r = Mats.Lit(Color.Lerp(Hex("050508"), Hex("3A3A48"), L), 0.9f, 0.2f); break;                           // shadow
                case 8: r = Mats.Lit(Color.Lerp(Hex("FF8FC8"), Hex("FFF0F8"), L * 0.8f), 0.75f); break;                         // candy
                default: r = Mats.Lit(Color.Lerp(Hex("06402A"), Hex("4CFFA8"), L), 0.9f, 0.35f); break;                         // emerald
            }
            cache[(m, f)] = r;
            return r;
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    }
}
