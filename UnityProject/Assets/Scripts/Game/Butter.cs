using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Promo code «BUTTER»: every surface in the game turns into butter (pale yellow, glossy, soft streaks).
    /// «BUTTER 22X» melts it back. New things (a world built later, a new outfit) get buttered within a second.
    /// </summary>
    public class Butter : MonoBehaviour
    {
        static Butter I;
        static Material butterM;
        readonly Dictionary<Renderer, Material[]> was = new Dictionary<Renderer, Material[]>();
        float t;
        public static bool On => I != null && I.enabled;
        public static int Buttered => I != null ? I.was.Count : 0;

        public static void Set(bool on)
        {
            if (on)
            {
                if (I == null) I = new GameObject("Butter").AddComponent<Butter>();
                I.enabled = true;
                I.Scan();
            }
            else if (I != null)
            {
                foreach (var kv in I.was) if (kv.Key) kv.Key.sharedMaterials = kv.Value;
                I.was.Clear();
                I.enabled = false;
            }
        }

        public static Material Mat()
        {
            if (butterM) return butterM;
            var cv = new Canvas2(64, 64, Parts.Hex("FFE58A"));
            var r = new Rng(22);
            for (int i = 0; i < 26; i++)
            {
                // soft knife streaks and little curls
                int y = r.I(64), x = r.I(64), len = 8 + r.I(20);
                cv.Rect(x, y, len, 1 + r.I(2), Parts.Hex("FFF2B8") * new Color(1, 1, 1, 0.7f));
                cv.Rect(x, y + 2, len, 1, Parts.Hex("F2CF5E") * new Color(1, 1, 1, 0.5f));
            }
            for (int i = 0; i < 40; i++) cv.Circle(r.I(64), r.I(64), 1 + r.F() * 1.5f, Parts.Hex("FFF6CC") * new Color(1, 1, 1, 0.6f));
            butterM = Mats.Tex(cv.Tex(true, true, FilterMode.Bilinear), Color.white, 0.82f);
            return butterM;
        }

        void Update()
        {
            if ((t -= Time.unscaledDeltaTime) > 0) return;
            t = 1f;
            Scan();
        }

        void Scan()
        {
            var m = Mat();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (was.ContainsKey(r)) continue;
                int l = r.gameObject.layer;
                if (l == Layers.Preview || l == Layers.Icon || l == 5) continue;                  // wardrobe pictures and UI stay readable
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                var mats = r.sharedMaterials;
                bool see = false;
                foreach (var mm in mats) if (mm != null && mm.renderQueue >= 2900) see = true;   // glass, water, barriers stay see-through
                if (see) continue;
                was[r] = mats;
                var nm = new Material[mats.Length];
                for (int i = 0; i < nm.Length; i++) nm[i] = m;
                r.sharedMaterials = nm;
            }
        }
    }
}
