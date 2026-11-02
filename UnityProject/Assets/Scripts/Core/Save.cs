using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Chubrik
{
    [Serializable]
    public class Profile
    {
        public string id = "";
        public int coins = 50, tokens = 0;
        public int maxHearts = 3;
        public bool navigator, navigatorOn = true, promo;
        public List<string> owned = new List<string>();     // item ids
        public List<string> look = new List<string>();      // "dept=item" (item "" = taken off)
        public List<int> sets = new List<int>();            // premium sets unlocked
        public List<string> found = new List<string>();     // hidden tokens / secret crates already taken
        public List<int> done = new List<int>();            // worlds finished
        public int boxes;                                    // loot boxes opened
        public float volume = 0.8f, sens = 1f;
        public bool music = true;
        public int quality = -1;
    }

    /// <summary>Local profile: coins, tokens, owned items, current look, settings (JSON in persistentDataPath).</summary>
    public static class Save
    {
        public static Profile P = new Profile();
        public static string FileName = "chubrik.json";
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);
        public static event Action Changed;

        public static void Load()
        {
            try { if (File.Exists(Path)) P = JsonUtility.FromJson<Profile>(File.ReadAllText(Path)) ?? new Profile(); }
            catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); P = new Profile(); }
            if (string.IsNullOrEmpty(P.id)) P.id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Fix();
        }

        /// <summary>Default items are always owned; hearts stay in 3..6.</summary>
        public static void Fix()
        {
            foreach (var d in Catalog.Depts)
                if (!string.IsNullOrEmpty(d.def) && !P.owned.Contains(d.def)) P.owned.Add(d.def);
            P.maxHearts = Mathf.Clamp(P.maxHearts, 3, 6);
            if (P.promo) P.maxHearts = 6;   // developer promo code: 6 hearts
        }

        public static void Write()
        {
            Changed?.Invoke();
            if (Boot.Testing && !Boot.Has("-keepsave")) return;
            try { File.WriteAllText(Path, JsonUtility.ToJson(P)); WebBridge.SyncFiles(); }
            catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
        }

        public static bool Owns(string id)
        {
            if (string.IsNullOrEmpty(id) || P.owned.Contains(id)) return true;
            int set = Catalog.SetOf(id);
            return set >= 0 && P.sets.Contains(set);
        }

        public static void Give(string id)
        {
            if (!P.owned.Contains(id)) P.owned.Add(id);
        }

        /// <summary>The worn item of a department ("" = nothing).</summary>
        public static string Worn(string dept)
        {
            foreach (var e in P.look)
                if (e.StartsWith(dept + "=")) return e.Substring(dept.Length + 1);
            return Catalog.Dept(dept)?.def ?? "";
        }

        public static Look CurrentLook()
        {
            var l = new Look();
            foreach (var d in Catalog.Depts) l[d.id] = Worn(d.id);
            return l;
        }

        public static void Wear(string dept, string id)
        {
            P.look.RemoveAll(e => e.StartsWith(dept + "="));
            P.look.Add(dept + "=" + (id ?? ""));
            Write();
        }

        public static bool Spend(int coins)
        {
            if (P.coins < coins) return false;
            P.coins -= coins;
            Write();
            return true;
        }

        public static bool SpendTokens(int n)
        {
            if (P.tokens < n) return false;
            P.tokens -= n;
            Write();
            return true;
        }

        public static void AddCoins(int n) { P.coins += n; Write(); }
    }

    /// <summary>What the chubrik wears: department → item id.</summary>
    public class Look : Dictionary<string, string>
    {
        public string Of(string dept) => TryGetValue(dept, out var v) ? v ?? "" : "";
        public int Idx(string dept) => Catalog.Index(Of(dept));
        public Look Copy() { var l = new Look(); foreach (var kv in this) l[kv.Key] = kv.Value; return l; }
        public static Look Default()
        {
            var l = new Look();
            foreach (var d in Catalog.Depts) l[d.id] = d.def;
            return l;
        }
    }
}
