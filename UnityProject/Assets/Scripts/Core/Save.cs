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
        public List<SavedOutfit> outfits = new List<SavedOutfit>();   // v0.30 «Мои скины»
        public string updateSeen = "";                       // v0.27: the «Вышло обновление» message was shown for this version                           // 19.11 «Заблокировать комнату» until (unix seconds)                             // 18.11 football difficulty: 0 super easy … 3 hard
        public bool cheatFly, flyOn, cheatCoins, coinsOn, cheatAdmin;   // 18.11 cheats (Settings → «Читы»)
        public List<string> placed = new List<string>();     // admin panel copies: id|path|pos|rot|scale|1                            // 18.11 football: the first level not yet won
        public bool teapot, butter;                          // teapot: old «Чайник» code (unused); «BUTTER» (everything made of butter)
    }

    /// <summary>v0.30: a saved outfit («Мои скины» in the wardrobe): a name and "dept=item;…".</summary>
    [Serializable]
    public class SavedOutfit
    {
        public string name = "";
        public string look = "";
    }

    /// <summary>
    /// One of the player's chubriks (22.11: up to 3, each with its OWN progress — a whole Profile in its own file).
    /// The list of them (names and outfits for the picker's portraits) lives in a small file of its own.
    /// </summary>
    [Serializable]
    public class CharSlot
    {
        public string name = "";
        public string nick = "";                              // the profile's nickname (a copy, for the picker)
        public int file;                                      // 0 = chubrik.json, 1 = chubrik_1.json, 2 = chubrik_2.json
        public List<string> look = new List<string>();        // its outfit (a copy, for the picker's portrait)
    }

    /// <summary>chubrik_chars.json: which chubrik is played and the list of them.</summary>
    [Serializable]
    class CharsMeta
    {
        public int active;
        public List<CharSlot> slots = new List<CharSlot>();
    }

    /// <summary>v0.29 saves: several chubriks sharing one profile (read once to split them up).</summary>
    [Serializable]
    class LegacyChars
    {
        public List<CharSlot> chars = new List<CharSlot>();
        public int activeChar;
    }

    /// <summary>Local profile: coins, tokens, owned items, current look, settings (JSON in persistentDataPath).</summary>
    public static class Save
    {
        public static Profile P = new Profile();
        public static string FileName = "chubrik.json";
        public static event Action Changed;

        // ---------------- 22.11: up to 3 chubriks, each with its own progress ----------------
        // Every chubrik is a whole Profile in its own file: slot files 0 = chubrik.json (old saves stay there),
        // 1 = chubrik_1.json, 2 = chubrik_2.json. chubrik_chars.json keeps the list (names + outfits for the picker)
        // and which one is played. P is always the played one; switching = write it, read the other one.

        public const int MaxChars = 3;
        static CharsMeta meta = OneChar();
        static CharsMeta OneChar() { var m = new CharsMeta(); m.slots.Add(new CharSlot { file = 0 }); return m; }
        /// <summary>The played chubrik's entry in the list.</summary>
        static CharSlot Cur { get { if (meta.slots.Count == 0) meta.slots.Add(new CharSlot { file = 0 }); meta.active = Mathf.Clamp(meta.active, 0, meta.slots.Count - 1); return meta.slots[meta.active]; } }
        /// <summary>The last JSON of every slot file (the autotest writes nothing to disk, switching back still works).</summary>
        static readonly Dictionary<int, string> mem = new Dictionary<int, string>();
        static string metaWritten = "";

        static string Base => System.IO.Path.GetFileNameWithoutExtension(FileName);
        static string FileOf(int f) => System.IO.Path.Combine(Application.persistentDataPath, f == 0 ? FileName : Base + "_" + f + ".json");
        static string MetaPath => System.IO.Path.Combine(Application.persistentDataPath, Base + "_chars.json");
        static bool Disk => !Boot.Testing || Boot.Has("-keepsave");
        static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        public static void Load()
        {
            LoadMeta();
            P = ReadProfile(Cur.file);
            Fix();
        }

        /// <summary>Autotest: one fresh chubrik, nothing from the disk.</summary>
        public static void FreshForTest()
        {
            mem.Clear();
            meta = OneChar();
            P = new Profile();
            Fix();
        }

        static void LoadMeta()
        {
            CharsMeta m = null;
            try { if (File.Exists(MetaPath)) m = JsonUtility.FromJson<CharsMeta>(File.ReadAllText(MetaPath)); }
            catch (Exception e) { Debug.LogWarning("[Save] chars: " + e.Message); }
            bool migrated = m == null || m.slots == null || m.slots.Count == 0;
            meta = migrated ? Migrate() : m;
            FixMeta();
            metaWritten = migrated ? "" : JsonUtility.ToJson(meta);
            if (migrated) { WriteMeta(); SyncDisk(); }   // at once: the old file no longer lists the v0.29 chubriks
        }

        /// <summary>No list yet: the old save is chubrik 1. v0.29 had several chubriks sharing one profile —
        /// each of them gets a copy of that progress (nobody loses anything) in its own file.</summary>
        static CharsMeta Migrate()
        {
            var m = new CharsMeta();
            string text = null;
            try { if (File.Exists(FileOf(0))) text = File.ReadAllText(FileOf(0)); } catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
            LegacyChars old = null;
            try { if (!string.IsNullOrEmpty(text)) old = JsonUtility.FromJson<LegacyChars>(text); } catch { }
            if (old == null || old.chars == null || old.chars.Count <= 1)
            {
                m.slots.Add(new CharSlot { file = 0, name = old != null && old.chars != null && old.chars.Count == 1 ? old.chars[0].name ?? "" : "" });
                return m;
            }
            var baseP = ReadProfile(0);
            int n = Mathf.Min(old.chars.Count, MaxChars);
            for (int i = 0; i < n; i++)
            {
                var c = old.chars[i] ?? new CharSlot();
                var copy = JsonUtility.FromJson<Profile>(JsonUtility.ToJson(baseP));
                copy.look = new List<string>(i == old.activeChar ? baseP.look : c.look ?? new List<string>());
                if (i > 0) copy.id = NewId();   // its own line in the records table
                m.slots.Add(new CharSlot { file = i, name = c.name ?? "", nick = copy.nick, look = new List<string>(copy.look) });
                WriteProfile(i, copy);
            }
            m.active = Mathf.Clamp(old.activeChar, 0, n - 1);
            Debug.Log($"[Save] {n} chubriks of v0.29 split into their own saves");
            return m;
        }

        static void FixMeta()
        {
            meta.slots.RemoveAll(c => c == null);
            if (meta.slots.Count == 0) meta.slots.Add(new CharSlot { file = 0 });
            while (meta.slots.Count > MaxChars) meta.slots.RemoveAt(meta.slots.Count - 1);
            var used = new HashSet<int>();
            foreach (var c in meta.slots)
            {
                if (c.look == null) c.look = new List<string>();
                if (c.name == null) c.name = "";
                if (c.nick == null) c.nick = "";
                if (c.file < 0 || c.file >= MaxChars || used.Contains(c.file)) c.file = FreeFile(used);
                used.Add(c.file);
            }
            meta.active = Mathf.Clamp(meta.active, 0, meta.slots.Count - 1);
        }

        static int FreeFile(HashSet<int> used)
        {
            for (int f = 0; f < MaxChars; f++) if (!used.Contains(f)) return f;
            return 0;
        }

        static Profile ReadProfile(int file)
        {
            string json = null;
            if (!mem.TryGetValue(file, out json))
                try { if (File.Exists(FileOf(file))) json = File.ReadAllText(FileOf(file)); }
                catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
            Profile pr = null;
            try { if (!string.IsNullOrEmpty(json)) pr = JsonUtility.FromJson<Profile>(json); }
            catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
            if (pr == null) pr = new Profile();
            if (string.IsNullOrEmpty(pr.id) && !Boot.Testing) pr.id = NewId();
            if (!pr.wolf)
            {
                // 15.11: the hero is a wolf now — wolf head, wolf ears, no spiky hair; hats, clothes and colours stay
                pr.wolf = true;
                pr.look.RemoveAll(e => e.StartsWith("head=") || e == "ears=ears_1" || e == "hair=hair_0");
            }
            return pr;
        }

        static void WriteProfile(int file, Profile pr)
        {
            var json = JsonUtility.ToJson(pr);
            mem[file] = json;
            if (!Disk) return;
            try { File.WriteAllText(FileOf(file), json); }
            catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
        }

        static void WriteMeta()
        {
            var json = JsonUtility.ToJson(meta);
            if (json == metaWritten) return;
            metaWritten = json;
            if (!Disk) return;
            try { File.WriteAllText(MetaPath, json); }
            catch (Exception e) { Debug.LogWarning("[Save] chars: " + e.Message); }
        }

        /// <summary>The played chubrik's outfit and nickname go into the list (the picker's portraits).</summary>
        static void SyncMeta()
        {
            var a = Cur;
            if (a.look.Count != P.look.Count || !a.look.TrueForAll(P.look.Contains)) { a.look.Clear(); a.look.AddRange(P.look); }
            a.nick = P.nick ?? "";
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
            foreach (var c in meta.slots)
                c.look.RemoveAll(e => { int k = e.IndexOf('='); var id = k > 0 ? e.Substring(k + 1) : ""; return k > 0 && id != "" && Catalog.Get(id) == null; });
        }

        /// <summary>How many chubriks there are and which one is played now.</summary>
        public static int CharCount => meta.slots.Count;
        public static int ActiveIndex => meta.active;

        /// <summary>«Чубрик 2»…; without a name — the nickname, then «Чубрик N».</summary>
        public static string CharName(int i)
        {
            if (i < 0 || i >= meta.slots.Count) return "";
            var c = meta.slots[i];
            if (!string.IsNullOrEmpty(c.name)) return c.name;
            var nick = i == meta.active ? P.nick : c.nick;
            if (!string.IsNullOrEmpty(nick)) return nick;
            return "Чубрик " + (i + 1);
        }

        /// <summary>The outfit of chubrik <paramref name="i"/> (the active one: what is worn now).</summary>
        public static Look CharLook(int i)
        {
            if (i == meta.active || i < 0 || i >= meta.slots.Count) return CurrentLook();
            return LookOf(meta.slots[i].look);
        }

        /// <summary>Plays as chubrik <paramref name="i"/> from now on: the played one is saved, the other one's whole
        /// progress is loaded. Only the data — the game world is rebuilt by <see cref="Boot.Restart"/>.</summary>
        public static bool SwitchChar(int i)
        {
            if (i < 0 || i >= meta.slots.Count || i == meta.active) return false;
            Write();
            meta.active = i;
            P = ReadProfile(meta.slots[i].file);
            Fix();
            Write();
            return true;
        }

        /// <summary>A new chubrik with a brand new progress (50 coins, nothing found, no friends, no worlds), made
        /// active. The settings (sound, music, sensitivity) come along. −1 when there are 3 already.</summary>
        public static int NewChar()
        {
            if (meta.slots.Count >= MaxChars) return -1;
            int n = 2;
            for (; n < 100; n++)
            {
                bool used = false;
                for (int k = 0; k < meta.slots.Count; k++) if (CharName(k) == "Чубрик " + n) used = true;
                if (!used) break;
            }
            Write();
            var files = new HashSet<int>();
            foreach (var c in meta.slots) files.Add(c.file);
            int file = FreeFile(files);
            var was = P;
            P = new Profile
            {
                id = Boot.Testing ? "" : NewId(), wolf = true,
                volume = was.volume, music = was.music, sens = was.sens, quality = was.quality, updateSeen = was.updateSeen,
            };
            mem.Remove(file);
            meta.slots.Add(new CharSlot { name = "Чубрик " + n, file = file });
            meta.active = meta.slots.Count - 1;
            Fix();
            Write();
            return meta.active;
        }

        /// <summary>Deletes a chubrik that is not being played now — with all its progress (its file too).</summary>
        public static bool DeleteChar(int i)
        {
            if (i < 0 || i >= meta.slots.Count || i == meta.active || meta.slots.Count <= 1) return false;
            int file = meta.slots[i].file;
            meta.slots.RemoveAt(i);
            if (i < meta.active) meta.active--;
            mem.Remove(file);
            if (Disk)
                try { if (File.Exists(FileOf(file))) File.Delete(FileOf(file)); }
                catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
            WriteMeta();
            SyncDisk();
            return true;
        }

        /// <summary>Gives a premium set back: its items are no longer owned and are taken off (it can be bought or unlocked by code again).</summary>
        public static void CancelSet(int set)
        {
            P.sets.Remove(set);
            P.look.RemoveAll(e => { int k = e.IndexOf('='); return k > 0 && Catalog.SetOf(e.Substring(k + 1)) == set; });
        }

        public static void Write()
        {
            SyncMeta();
            Changed?.Invoke();
            WriteProfile(Cur.file, P);
            WriteMeta();
            SyncDisk();
        }

        /// <summary>The browser keeps files in IndexedDB: flush them.</summary>
        static void SyncDisk()
        {
            if (!Disk) return;
            try { WebBridge.SyncFiles(); } catch (Exception e) { Debug.LogWarning("[Save] " + e.Message); }
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
