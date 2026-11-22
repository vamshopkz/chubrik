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
        public long coins = 50, tokens = 0;                  // long: the «Миллиард» promo gives 300 billion
        public int maxHearts = 3;
        public bool navigator, navigatorOn = true, promo;
        public List<string> owned = new List<string>();     // item ids
        public List<string> look = new List<string>();      // "dept=item" (item "" = taken off)
        public List<int> sets = new List<int>();            // premium sets unlocked
        public List<int> setBonus = new List<int>();        // sets whose bonus coins/tokens were already given (once ever)
        public List<string> found = new List<string>();     // hidden tokens / secret crates already taken
        public List<int> done = new List<int>();            // worlds finished
        public int keys;                                     // golden keys in the pocket
        public List<string> rescued = new List<string>();   // cages opened (friends now live in the lobby)
        public int boxes;                                    // loot boxes opened
        public List<string> seen = new List<string>();      // story chapters already shown
        public float volume = 0.8f, sens = 1f;
        public bool music = true;
        public int quality = -1;
        public bool wolf;                                    // 15.11: the hero became a wolf (one-time look change done)
        public string nick = "";                             // 17.11: nickname for the records table (Settings)
        public int footLevel = 1;
        public int footDiff = 1;
        public long roomLockUntil;
        public string updateSeen = "";                       // v0.27: the «Вышло обновление» message was shown for this version                           // 19.11 «Заблокировать комнату» until (unix seconds)                             // 18.11 football difficulty: 0 super easy … 3 hard
        public bool cheatFly, flyOn, cheatCoins, coinsOn, cheatAdmin;   // 18.11 cheats (Settings → «Читы»)
        public List<string> placed = new List<string>();     // admin panel copies: id|path|pos|rot|scale|1                            // 18.11 football: the first level not yet won
        public bool teapot, butter;                          // teapot: old «Чайник» code (unused); «BUTTER» (everything made of butter)
        // 22.11: up to 3 chubriks on one account — coins, items, friends and worlds are shared, each has its own name and outfit.
        // `look` above is always the active one's outfit (kept in chars[activeChar].look too).
        public List<CharSlot> chars = new List<CharSlot>();
        public int activeChar;
    }

    /// <summary>One of the player's chubriks: its name ("" = the nickname / «Чубрик 1») and outfit ("dept=item").</summary>
    [Serializable]
    public class CharSlot
    {
        public string name = "";
        public List<string> look = new List<string>();
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
            if (!P.wolf)
            {
                // 15.11: the hero is a wolf now — wolf head, wolf ears, no spiky hair; hats, clothes and colours stay
                P.wolf = true;
                P.look.RemoveAll(e => e.StartsWith("head=") || e == "ears=ears_1" || e == "hair=hair_0");
            }
            Fix();
        }

        /// <summary>Default items are always owned; hearts stay in 3..6.</summary>
        public static void Fix()
        {
            foreach (var d in Catalog.Depts)
                if (!string.IsNullOrEmpty(d.def) && !P.owned.Contains(d.def)) P.owned.Add(d.def);
            P.maxHearts = Mathf.Clamp(P.maxHearts, 3, 6);
            if (P.promo) P.maxHearts = 6;   // developer promo code: 6 hearts
            foreach (var set in P.sets) if (!P.setBonus.Contains(set)) P.setBonus.Add(set);
            // 21.11: a thing that left the catalogue (walk_6 «Лунная походка») is taken off: the department's default comes back
            P.look.RemoveAll(e => { int k = e.IndexOf('='); var id = k > 0 ? e.Substring(k + 1) : ""; return k > 0 && id != "" && Catalog.Get(id) == null; });
            FixChars();
        }

        // ---------------- 22.11: several chubriks ----------------

        public const int MaxChars = 3;

        /// <summary>Old saves: one chubrik from the current look; the active index in range; the active outfit in its slot.</summary>
        static void FixChars()
        {
            if (P.chars == null) P.chars = new List<CharSlot>();
            P.chars.RemoveAll(c => c == null);
            if (P.chars.Count == 0) P.chars.Add(new CharSlot { name = "", look = new List<string>(P.look) });
            while (P.chars.Count > MaxChars) P.chars.RemoveAt(P.chars.Count - 1);
            P.activeChar = Mathf.Clamp(P.activeChar, 0, P.chars.Count - 1);
            foreach (var c in P.chars)
            {
                if (c.look == null) c.look = new List<string>();
                c.look.RemoveAll(e => { int k = e.IndexOf('='); var id = k > 0 ? e.Substring(k + 1) : ""; return k > 0 && id != "" && Catalog.Get(id) == null; });
            }
            SyncActive();
        }

        /// <summary>The active chubrik's slot gets the outfit being worn now.</summary>
        static void SyncActive()
        {
            if (P.chars == null || P.chars.Count == 0) return;
            P.activeChar = Mathf.Clamp(P.activeChar, 0, P.chars.Count - 1);
            var a = P.chars[P.activeChar];
            if (a.look == null) a.look = new List<string>();
            a.look.Clear(); a.look.AddRange(P.look);
        }

        public static CharSlot ActiveChar => P.chars.Count > 0 ? P.chars[Mathf.Clamp(P.activeChar, 0, P.chars.Count - 1)] : null;

        /// <summary>«Чубрик 2»…; the first one is called by the nickname when it is set.</summary>
        public static string CharName(int i)
        {
            if (i < 0 || i >= P.chars.Count) return "";
            var n = P.chars[i].name;
            if (!string.IsNullOrEmpty(n)) return n;
            if (i == 0 && !string.IsNullOrEmpty(P.nick)) return P.nick;
            return "Чубрик " + (i + 1);
        }

        /// <summary>The outfit of chubrik <paramref name="i"/> (the active one: what is worn now).</summary>
        public static Look CharLook(int i)
        {
            if (i == P.activeChar || i < 0 || i >= P.chars.Count) return CurrentLook();
            return LookOf(P.chars[i].look);
        }

        /// <summary>Plays as chubrik <paramref name="i"/> from now on (the player model is not touched here).</summary>
        public static void SwitchChar(int i)
        {
            if (i < 0 || i >= P.chars.Count) return;
            SyncActive();
            P.activeChar = i;
            P.look = new List<string>(P.chars[i].look);
            Fix();
            Write();
        }

        /// <summary>A new chubrik in the starting outfit, made active. −1 when there are 3 already.</summary>
        public static int NewChar()
        {
            if (P.chars.Count >= MaxChars) return -1;
            int n = 2;
            for (; n < 100; n++)
            {
                bool used = false;
                for (int k = 0; k < P.chars.Count; k++) if (CharName(k) == "Чубрик " + n) used = true;
                if (!used) break;
            }
            SyncActive();
            P.chars.Add(new CharSlot { name = "Чубрик " + n, look = new List<string>() });
            SwitchChar(P.chars.Count - 1);
            return P.activeChar;
        }

        /// <summary>Deletes a chubrik that is not being played now (its outfit only — things stay on the account).</summary>
        public static bool DeleteChar(int i)
        {
            if (i < 0 || i >= P.chars.Count || i == P.activeChar || P.chars.Count <= 1) return false;
            SyncActive();
            P.chars.RemoveAt(i);
            if (i < P.activeChar) P.activeChar--;
            Write();
            return true;
        }

        /// <summary>Gives a premium set back: its items are no longer owned and are taken off (it can be bought or unlocked by code again).</summary>
        public static void CancelSet(int set)
        {
            P.sets.Remove(set);
            P.look.RemoveAll(e => { int k = e.IndexOf('='); return k > 0 && Catalog.SetOf(e.Substring(k + 1)) == set; });
            foreach (var c in P.chars) c.look.RemoveAll(e => { int k = e.IndexOf('='); return k > 0 && Catalog.SetOf(e.Substring(k + 1)) == set; });
        }

        public static void Write()
        {
            SyncActive();
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
        public static string Worn(string dept) => WornIn(P.look, dept);

        /// <summary>The worn item of a department in an outfit list.</summary>
        public static string WornIn(List<string> list, string dept)
        {
            foreach (var e in list)
                if (e.StartsWith(dept + "="))
                {
                    var id = e.Substring(dept.Length + 1);
                    if (id == "" || Catalog.Get(id) != null) return id;
                    break;   // not in the catalogue any more (walk_6): the default is worn
                }
            return Catalog.Dept(dept)?.def ?? "";
        }

        public static Look CurrentLook() => LookOf(P.look);

        public static Look LookOf(List<string> list)
        {
            var l = new Look();
            foreach (var d in Catalog.Depts) l[d.id] = WornIn(list, d.id);
            return l;
        }

        /// <summary>18.11 «Снять все скины»: every department back to its starting item (or nothing).</summary>
        public static void TakeOffAll()
        {
            P.look.Clear();
            Write();
        }

        public static void Wear(string dept, string id)
        {
            P.look.RemoveAll(e => e.StartsWith(dept + "="));
            P.look.Add(dept + "=" + (id ?? ""));
            Write();
        }

        public static bool Spend(long coins)
        {
            if (Cheats.EndlessCoins) return true;   // 18.11 the cheat: buying costs nothing
            if (P.coins < coins) return false;
            P.coins -= coins;
            Write();
            return true;
        }

        public static bool SpendTokens(long n)
        {
            if (P.tokens < n) return false;
            P.tokens -= n;
            Write();
            return true;
        }

        public static void AddCoins(long n) { P.coins += n; Write(); }
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
