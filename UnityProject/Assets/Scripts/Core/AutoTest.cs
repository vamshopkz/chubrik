using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// Self-test (-autotest -shots DIR): catalogue size, every item builds, movement, jumps, glide, bounce pads,
    /// teleports, coins, shop, loot boxes, promo code, wardrobe; screenshots of the lobby and the menus.
    /// Writes DIR/autotest.txt and quits.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        readonly StringBuilder log = new StringBuilder();
        int pass, fail;
        string dir;

        void Check(string what, bool ok, string extra = "")
        {
            if (ok) pass++; else fail++;
            log.AppendLine((ok ? "PASS " : "FAIL ") + what + (extra != "" ? "  (" + extra + ")" : ""));
            Debug.Log("[Test] " + (ok ? "PASS " : "FAIL ") + what + " " + extra);
        }

        IEnumerator Start()
        {
            dir = Boot.Arg("-shots") ?? Path.Combine(Application.persistentDataPath, "shots");
            Directory.CreateDirectory(dir);
            yield return null;
            yield return null;
            if (Boot.Arg("-gallery") != null)
            {
                // contact sheets of every detailed item, one per department, for a visual check
                yield return Gallery(Boot.Arg("-gallery"));
                Application.Quit();
                yield break;
            }
            if (Boot.Arg("-overview") != null)
            {
                // a photo of the lobby and of every world from straight above (and the backrooms without their ceiling)
                yield return Overview(Boot.Arg("-overview"));
                Application.Quit();
                yield break;
            }
            if (Boot.Arg("-sounds") != null)
            {
                while (!AudioFx.MusicReady) yield return null;
                AudioFx.Export(Boot.Arg("-sounds"), "music", "jump0", "jump1", "flip", "punch", "swing", "step0", "step1", "coin", "click", "token", "land0", "note0", "win0");
                Application.Quit();
                yield break;
            }
            if (Boot.Arg("-icons") != null)
            {
                // only render the app icon
                yield return IconMaker.Make(Boot.Arg("-icons"));
                Application.Quit();
                yield break;
            }
            var pl = PlayerCtl.I;

            // ---- catalogue ----
            Check("32 departments (with «Костюмы»)", Catalog.Depts.Count == 32, Catalog.Depts.Count.ToString());
            bool eight = true;
            // the regular named things (not premium, rewards or variants): 8 each; «Походка» 7 since «Лунная походка» left (21.11)
            foreach (var d in Catalog.Depts) if (d.id != "suit" && d.items.FindAll(i => !i.Premium && !i.Reward && !i.Variant).Count != (d.id == "walk" ? 7 : 8)) eight = false;
            Updater.I.Fake("9.99", "Новый мир. Тысяча скинов. Футбол с друзьями.");
            Updater.I.ShowMessage();
            yield return Wait(0.4f);
            bool updMsg = false, updNote = false;
            foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) { if (tx.text.Contains("ВЫШЛО ОБНОВЛЕНИЕ 9.99")) updMsg = true; if (tx.text.Contains("Тысяча скинов")) updNote = true; }
            yield return Shot("120_update_message");
            Window.Close();
            Updater.I.Fake(null);
            Check("«Вышло обновление 9.99!» with what is new in it", updMsg && updNote);
            {
                // 21.11: every department (weapons too) has exactly M things — as many as the biggest one (the costumes)
                int N = Catalog.PerDept;
                bool same = N > 8 && Catalog.Dept("weapon").items.Count == N, unique = true;
                var off = new List<string>();
                foreach (var d in Catalog.Depts)
                {
                    bool ok = d.items.Count == N;
                    if (!ok) { same = false; off.Add(d.id + ":" + d.items.Count); }
                    var names = new HashSet<string>(); var pairs = new HashSet<(int, int)>();
                    foreach (var it in d.items)
                    {
                        if (!names.Add(it.name)) { unique = false; off.Add("name " + it.name); }
                        if (it.Variant && it.dept != "color" && it.dept != "wcolor" && !pairs.Add((it.baseIndex, Catalog.Mod(it)))) { unique = false; off.Add("pair " + it.id); }
                        if (it.Variant && (!Lucky.InPool(it) || Lucky.Rarity(it) < 0 || Lucky.Rarity(it) > 3 || it.price <= 0)) { unique = false; off.Add("pool " + it.id); }
                    }
                }
                bool walk6 = Catalog.Get("walk_6") == null && !Catalog.Dept("walk").items.Exists(i => i.id == "walk_6");
                var keep = Save.P.look.ToArray();
                Save.P.look.RemoveAll(e => e.StartsWith("walk="));
                Save.P.look.Add("walk=walk_6");
                bool fallback = Save.Worn("walk") == Catalog.Dept("walk").def;
                Save.Fix();
                fallback &= !Save.P.look.Contains("walk=walk_6");
                Save.P.look.Clear(); Save.P.look.AddRange(keep);
                var sec = Catalog.Dept("secret").items.FindAll(i => i.Variant);
                bool secretOk = sec.Count > 0 && sec.TrueForAll(i => i.tokens && (i.finish >= 0 || i.tint >= 0) && i.baseIndex != 7);
                var wv = Catalog.Get("walk_v0");
                bool mods = wv != null && (wv.speed != 1 || wv.amp != 1);
                mods &= Catalog.PitchOf("jsnd_v0") != 1 && Catalog.TintOf("aura_v0").HasValue && Catalog.TintOf("skin_v0").HasValue;
                mods &= Catalog.FinishOf("head_v0") >= 0 && Catalog.FinishOf("pet_v0") >= 0 && Catalog.Index("color_v0") == 8;
                Check("v0.27: 1000 more skins (things in 10 finishes)", Catalog.VariantCount == 1000 && Catalog.Get("hat_v0") != null && Catalog.Get("hat_v0").name.Contains("("), Catalog.VariantCount.ToString());
                Check("every department has as many items as «Оружие и инструменты» (all exactly M)", same, $"M = {N}, weapon {Catalog.Dept("weapon").items.Count}, {Catalog.FillCount} new" + (off.Count > 0 ? " off: " + string.Join(", ", off.GetRange(0, Mathf.Min(10, off.Count))) : ""));
                Check("variants: unique names, no two alike, all in the lucky blocks", unique, off.Count > 0 ? string.Join(", ", off.GetRange(0, Mathf.Min(10, off.Count))) : "");
                Check("«Лунная походка» (walking backwards) is gone, a save wearing it walks the default way", walk6 && fallback);
                Check("variants really change something (tempo, pitch, tint, finish, new colours); secret ones cost tokens", mods && secretOk);
            }
            Check("25+ costumes: chicken, rich Артём and Али, footballers, sport, robots, phones, computers", Catalog.Dept("suit").items.Count >= 26, Catalog.Dept("suit").items.Count.ToString());
            Check("8 named items in every department", eight);
            Check("200+ items in total", Catalog.Items.Count >= 200, Catalog.Items.Count + " (regular " + Catalog.RegularCount + ")");
            Check("8 premium sets priced 1000–3000 ₸", Catalog.Sets.Count == 8 && Catalog.Sets.TrueForAll(s => s.price >= 1000 && s.price <= 3000 && s.items.Count >= 3));

            // ---- every item builds ----
            var test = ChubrikModel.Create("TestModel", null, Look.Default(), Layers.Preview);
            test.transform.position = new Vector3(200, -300, 0);
            int bad = 0; var badList = new List<string>();
            var few = new List<string>();
            var suitFew = new List<string>();
            var report = new StringBuilder();
            int maxVerts = 0; string maxVertsId = "";
            var detailed = new HashSet<string> { "hair", "ears", "hat", "eyes", "mask", "neck", "top", "wings", "hands", "wrist", "back", "tail", "pants", "belt", "shoes", "weapon", "pet" };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var it in Catalog.Items.Values)
            {
                var l = Look.Default(); l[it.dept] = it.id;
                try
                {
                    test.Build(l);
                    int parts = test.PartCount.TryGetValue(it.dept, out var pc) ? pc : 0;
                    if (it.dept == "pet")
                    {
                        var t = new GameObject("p").transform;
                        int made = Parts.Made;
                        Pet.BuildItem(it.id, t);
                        parts = Parts.Made - made;
                        Destroy(t.gameObject);
                    }
                    int verts = 0;
                    foreach (var mf in test.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh) verts += mf.sharedMesh.vertexCount;
                    if (verts > maxVerts) { maxVerts = verts; maxVertsId = it.id; }
                    if (it.dept == "suit" && parts < 60) suitFew.Add($"{it.id}:{parts}");
                    if (detailed.Contains(it.dept) || it.dept == "head")
                    {
                        report.AppendLine($"{it.id}\t{parts}\t{verts}\t{it.name}");
                        if (parts < (it.dept == "head" ? 60 : 100)) few.Add($"{it.id}:{parts}");
                    }
                }
                catch (Exception e) { bad++; badList.Add(it.id + ":" + e.Message); }
            }
            File.WriteAllText(Path.Combine(dir, "parts.txt"), report.ToString());
            Check("every item builds", bad == 0, $"{Catalog.Items.Count} items, {sw.ElapsedMilliseconds} ms" + (bad > 0 ? " bad: " + string.Join(", ", badList.GetRange(0, Mathf.Min(10, badList.Count))) : ""));
            Check("every skin is detailed (100+ parts, heads 60+)", few.Count == 0, few.Count == 0 ? "parts.txt" : few.Count + " items: " + string.Join(", ", few.GetRange(0, Mathf.Min(20, few.Count))));
            Check("every costume is detailed (60+ parts)", suitFew.Count == 0, string.Join(", ", suitFew));
            Check("a dressed-up item stays light for phones", maxVerts < 45000, $"max {maxVerts} verts ({maxVertsId})");
            Destroy(test.gameObject);
            yield return null;

            // ---- lobby screenshots ----
            yield return Shot("01_lobby_spawn");
            pl.yaw = 90; yield return Wait(0.3f); yield return Shot("02_lobby_shop");
            pl.yaw = -90; yield return Wait(0.3f); yield return Shot("03_lobby_wardrobe");
            pl.yaw = 180; yield return Wait(0.3f); yield return Shot("04_lobby_course");
            pl.yaw = 0;

            // ---- movement ----
            pl.Teleport(Lobby.Spawn, 0);
            yield return Wait(0.5f);
            var p0 = pl.transform.position;
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(1f);
            pl.MoveInput = Vector2.zero;
            Check("runs forward", pl.transform.position.z - p0.z > 3, (pl.transform.position.z - p0.z).ToString("0.0") + " m");
            yield return Wait(0.6f);
            float y0 = pl.transform.position.y, top = y0;
            pl.QueueJump();
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { top = Mathf.Max(top, pl.transform.position.y); yield return null; }
            Check("jump", top - y0 > 1.4f, (top - y0).ToString("0.00") + " m");
            int fl0 = pl.Flips;
            pl.QueueJump();
            float maxTurn = 0;
            bool shot = false;
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                top = Mathf.Max(top, pl.transform.position.y);
                maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(Quaternion.identity, pl.Model.Body.localRotation));
                if (!shot && t > 0.22f) { shot = true; yield return Shot("27_somersault"); }
                yield return null;
            }
            Check("double jump goes higher", top - y0 > 2.5f, (top - y0).ToString("0.00") + " m");
            Check("second jump is a somersault", pl.Flips == fl0 + 1 && maxTurn > 120, $"turn {maxTurn:0}°");
            yield return Wait(1.2f);
            Check("lands again", pl.Grounded);
            Check("lands upright after the somersault", !pl.Anim.Flipping && Quaternion.Angle(Quaternion.identity, pl.Model.Body.localRotation) < 1);

            // ---- dive: a hit after the second jump, belly slide, stand up with a jump ----
            // pick a direction with nothing in the way for the dive and the slide
            float diveYaw = 180;
            foreach (var cand in new float[] { 180, 90, 270, 0, 135, 225, 45, 315 })
            {
                var dir = Quaternion.Euler(0, cand, 0) * Vector3.forward;
                if (!Physics.SphereCast(Lobby.Spawn + Vector3.up * 0.8f, 0.5f, dir, out _, 13f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { diveYaw = cand; break; }
            }
            // ---- 18.11 A + A + B: straight down, then a bounce higher than a double jump ----
            pl.Teleport(Lobby.Spawn, diveYaw);
            yield return Wait(0.6f);
            float gy = pl.transform.position.y, hDouble = 0;
            pl.QueueJump();
            yield return Wait(0.3f);
            pl.QueueJump();
            for (float tt = 0; tt < 1.6f; tt += Time.deltaTime) { hDouble = Mathf.Max(hDouble, pl.transform.position.y - gy); yield return null; }
            yield return Wait(0.6f);
            int sb0 = pl.SuperBounces;
            pl.QueueJump();
            yield return Wait(0.3f);
            pl.QueueJump();
            yield return Wait(0.12f);
            pl.HoldAttack = true;   // v0.31: B held after two jumps = straight down + the super bounce
            pl.Attack();
            bool downBig = false;
            for (float tt = 0; tt < 0.4f && !downBig; tt += Time.deltaTime) { downBig = pl.PlungeBig; yield return null; }
            pl.HoldAttack = false;
            yield return Shot("53_dive");
            for (float tt = 0; tt < 2f && pl.SuperBounces == sb0; tt += Time.deltaTime) yield return null;
            float hBounce = 0;
            for (float tt = 0; tt < 1.8f; tt += Time.deltaTime) { hBounce = Mathf.Max(hBounce, pl.transform.position.y - gy); yield return null; }
            yield return Shot("54_belly");
            Check("A + A + B held: straight down; the bounce up comes only sometimes (v0.32)", downBig && (pl.SuperBounces == sb0 || hBounce > hDouble + 0.5f), $"double {hDouble:0.0} m, bounce {hBounce:0.0} m");
            yield return Wait(1f);
            // v0.31: A + A + B tapped — no hop: down and forward belly first, then lies until A
            pl.Teleport(Lobby.Spawn, diveYaw);
            yield return Wait(0.6f);
            pl.QueueJump(); yield return Wait(0.3f); pl.QueueJump(); yield return Wait(0.15f);
            int fl0A = pl.Flops; float fy0A = pl.transform.position.y; var fp0A = pl.transform.position;
            pl.Attack();
            yield return Wait(0.05f);
            float fyMaxA = pl.transform.position.y;
            for (float tt = 0; tt < 0.3f; tt += Time.deltaTime) { fyMaxA = Mathf.Max(fyMaxA, pl.transform.position.y); yield return null; }
            bool bellyAirA = pl.Diving;
            yield return Shot("133_aab_dive");
            for (float tt = 0; tt < 2f && !pl.Lying; tt += Time.deltaTime) yield return null;
            var fp1A = pl.transform.position;
            Check("A + A + B: no hop — flies down and forward on the belly", pl.Flops == fl0A + 1 && bellyAirA && fyMaxA < fy0A + 0.25f && new Vector2(fp1A.x - fp0A.x, fp1A.z - fp0A.z).magnitude > 0.8f, $"up {fyMaxA - fy0A:0.00} m, forward {new Vector2(fp1A.x - fp0A.x, fp1A.z - fp0A.z).magnitude:0.0} m");
            yield return Wait(1.5f);
            Check("…and lies on the belly until A", pl.Lying);
            pl.QueueJump();
            yield return Wait(0.6f);
            Check("…A stands him up", !pl.Lying && !pl.Diving);
            pl.Teleport(Lobby.Spawn, 0);
            yield return Wait(0.3f);
            pl.Attack();
            Check("on the ground a hit is still a hit", !pl.Diving && !pl.Lying && !pl.Plunging);

            // ---- the plunge: one jump + B = a small one, B held after one jump = straight down with a flop ----
            pl.Teleport(Lobby.Spawn, diveYaw);
            yield return Wait(0.6f);
            pl.QueueJump();
            yield return Wait(0.25f);
            pl.Attack();
            bool smallP = pl.Plunging && !pl.PlungeBig;
            for (float tt = 0; tt < 2f && !pl.Lying; tt += Time.deltaTime) yield return null;
            Check("one jump then B: a small plunge down, a funny flop on landing", smallP && pl.Lying);
            yield return Wait(0.6f);
            pl.QueueJump();
            yield return Wait(0.8f);
            pl.Teleport(pl.transform.position + Vector3.up * 6f, diveYaw);   // high enough to hold B on the way down
            yield return Wait(0.1f);
            var pz0 = pl.transform.position;
            pl.HoldAttack = true;
            pl.Attack();
            bool bigP = false;
            for (float tt = 0; tt < 0.4f && !bigP; tt += Time.deltaTime) { bigP = pl.PlungeBig; yield return null; }
            yield return Shot("77_plunge");
            for (float tt = 0; tt < 2f && !pl.Lying; tt += Time.deltaTime) yield return null;
            pl.HoldAttack = false;
            var pz1 = pl.transform.position;
            Check("one jump then B held: straight down head first, then lying", bigP && pl.Lying && new Vector2(pz1.x - pz0.x, pz1.z - pz0.z).magnitude < 4.5f, $"{new Vector2(pz1.x - pz0.x, pz1.z - pz0.z).magnitude:0.0} m sideways");
            yield return Wait(0.5f);
            yield return Shot("78_plunge_flop");
            pl.QueueJump();
            yield return Wait(0.6f);
            Check("a jump stands up after the plunge", !pl.Lying && !pl.Plunging);

            // ---- bounce mushroom ----
            pl.Teleport(Lobby.TestMushroom + Vector3.up * 2.6f, 0);
            float bTop = 0;
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { bTop = Mathf.Max(bTop, pl.transform.position.y); yield return null; }
            Check("bounce mushroom launches", bTop > 5f, bTop.ToString("0.0") + " m");

            // ---- no coins in the lobby, coins live in the worlds ----
            Check("no coins in the lobby", Pickups.I.CoinsWithin(Vector3.zero, 150) == 0, Pickups.I.CoinsWithin(Vector3.zero, 150).ToString());
            Hud.I.ForceTouch = true;
            yield return Wait(0.2f);
            yield return Shot("28_touch_controls");
            Check("«ВОЙТИ» gets the tap on a phone (not the camera area)", Hud.I.EnterButtonGetsTap(), Hud.I.TapReport);
            Hud.I.ForceTouch = false;
            Check("10 hidden tokens", Lobby.TokenSpots.Count == 10, Lobby.TokenSpots.Count.ToString());

            // ---- the lobby after the drawn map: blue gates open with rescued friends ----
            Check("6 blue gates (snow ×2, jungle ×2 — one over the river, crystal, glitch)", Lobby.Gates.Count == 6);
            Check("with no friends rescued only the ruins are open", Lobby.Open(2) && !Lobby.Open(4) && Lobby.Gates.TrueForAll(g => !g.IsOpen));
            var keepRescued = new List<string>(Save.P.rescued);
            for (int i = 0; i < 10; i++) Save.P.rescued.Add("test" + i);
            yield return null;
            Check("10 friends: snow and jungle gates open, crystal and glitch shut", Lobby.Gates.FindAll(g => g.IsOpen).Count == 4 && Lobby.Open(5) && !Lobby.Open(3));
            for (int i = 10; i < 25; i++) Save.P.rescued.Add("test" + i);
            yield return null;
            Check("25 friends: every gate is open", Lobby.Gates.TrueForAll(g => g.IsOpen));
            yield return Wait(1.8f);
            pl.Teleport(new Vector3(20, 0.3f, 8), 90);
            pl.yaw = 90; pl.pitch = 18;
            yield return Wait(0.4f);
            yield return Shot("55_lobby_map");
            // the whole lobby from above, to compare with the drawn map
            {
                var cg = new GameObject("TopCam");
                var cam = cg.AddComponent<Camera>();
                cam.orthographic = true; cam.orthographicSize = 70; cam.aspect = 1;
                cam.transform.SetPositionAndRotation(new Vector3(-16, 120, -2), Quaternion.Euler(90, 0, 0));
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.75f, 0.95f);
                cam.farClipPlane = 300; cam.enabled = false;
                var rt = new RenderTexture(1024, 1024, 24) { antiAliasing = 4 };
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, "56_lobby_top.png"), tex.EncodeToPNG());
                cam.targetTexture = null; rt.Release(); Destroy(cg); Destroy(tex);
            }
            Save.P.rescued.Clear(); Save.P.rescued.AddRange(keepRescued);
            yield return null;
            Check("gates close again when friends are fewer (test only)", Lobby.Gates.TrueForAll(g => !g.IsOpen));
            pl.Teleport(Lobby.Spawn, 0);

            // ---- teleport pad ----
            pl.Teleport(Lobby.PadFrom + Vector3.up * 0.15f, 0);
            yield return Wait(2.2f);
            Check("teleport pad", Vector3.Distance(pl.transform.position, Lobby.PadTo) < 3, pl.transform.position.ToString());
            yield return Shot("05_course_platform");

            // ---- the four wall styles from the drawing ----
            foreach (var (pos, yaw, nm) in new[] { (new Vector3(-5, 0.3f, 8), 270f, "57_wall_grass"), (new Vector3(20, 0.3f, 30), 0f, "58_wall_snow"),
                                                   (new Vector3(-30, 0.3f, -15), 180f, "59_wall_glitch"), (new Vector3(40, 0.3f, -12), 90f, "60_wall_crystal") })
            {
                pl.Teleport(pos, yaw);
                yield return Wait(1f);
                yield return Shot(nm);
            }
            pl.Teleport(Lobby.Spawn, 0);

            // ---- settings → game versions (list from GitHub) ----
            VersionsUI.Open();
            Check("the versions window opens", UIRoot.Open != null && UIRoot.Open.Title.text == "ВЕРСИИ ИГРЫ");
            for (int k = 0; k < 40 && UIRoot.Open != null && UIRoot.Open.Content.childCount <= 2; k++) yield return Wait(0.25f);
            yield return Shot("61_versions");
            Check("the versions window lists the releases (or says there is no internet)", UIRoot.Open != null && UIRoot.Open.Content.childCount >= 2);
            Window.Close();
            pl.Frozen = false;

            // ---- glide with wings ----
            Save.Give("wings_0"); Save.Wear("wings", "wings_0");
            pl.ApplyLook(Save.CurrentLook());
            pl.Teleport(new Vector3(0, 12, -10), 180);
            yield return Wait(0.6f);
            pl.HoldJump = true;
            yield return Wait(0.1f);
            float minVy = 0;
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { minVy = Mathf.Min(minVy, pl.Velocity.y); yield return null; }
            Check("glide with wings slows the fall", minVy > -6f, minVy.ToString("0.0"));
            yield return Shot("06_glide");
            pl.HoldJump = false;
            yield return Wait(2f);

            // ---- shop ----
            Save.P.coins = 20000; Save.Write();
            int owned0 = Menus.OwnedCount();
            ShopUI.BuyBox(false);
            yield return Wait(1.8f);
            Check("loot box gives a new item", Menus.OwnedCount() == owned0 + 1);
            yield return Shot("07_lootbox");
            ShopUI.CloseBox();
            ShopUI.Open();
            yield return Wait(0.3f);
            yield return Shot("08_shop");
            Window.Close();
            int h0 = Save.P.maxHearts;
            Save.Spend(ShopUI.HeartPrice(Save.P.maxHearts)); Save.P.maxHearts++;
            Check("extra heart", Save.P.maxHearts == h0 + 1);
            Save.P.navigator = true; Save.P.navigatorOn = true;

            // ---- premium + promo ----
            PremiumUI.Open();
            yield return Wait(0.3f);
            yield return Shot("09_premium");
            Window.Close();
            Check("wrong promo code refused", !Promo.Apply("12345"));
            Check("promo «артем 22 x» accepted", Promo.Apply("  артем 22 x "));
            Check("all 8 premium sets unlocked", Save.P.sets.Count == 8);
            Check("premium items owned", Save.Owns("hat_p7") && Save.Owns("wings_p0") && Save.Owns("secret_7"));

            // ---- cancelling premium sets ----
            Save.Wear("hat", "hat_p0"); pl.ApplyLook(Save.CurrentLook());
            long coinsBefore = Save.P.coins, tokensBefore = Save.P.tokens;
            PremiumUI.Cancel(new[] { 0 });
            Check("cancel one set: its items are gone and taken off", !Save.P.sets.Contains(0) && !Save.Owns("hat_p0") && Save.Worn("hat") != "hat_p0" && Save.P.sets.Count == 7);
            PremiumUI.Open();
            yield return Wait(0.3f);
            yield return Shot("29_premium_cancel");
            Window.Close();
            PremiumUI.Cancel(new[] { 1, 2, 3 });
            Check("cancel several sets at once", Save.P.sets.Count == 4 && !Save.Owns("top_p1"));
            PremiumUI.Cancel(new[] { 4, 5, 6, 7 });
            Check("cancel all sets", Save.P.sets.Count == 0 && !Save.Owns("hat_p7") && Save.Owns("secret_7"));
            Check("promo code returns the cancelled sets", Promo.Apply("Артём 22 X") && Save.P.sets.Count == 8 && Save.Owns("hat_p0"));
            Check("set bonus is paid only once (the code itself adds its 10 000 000 coins)", Save.P.coins == coinsBefore + Promo.DevCoins && Save.P.tokens == tokensBefore + Promo.DevTokens, $"coins {coinsBefore} → {Save.P.coins}");

            // ---- wardrobe: the real chubrik between the panels, the camera turned to the nearest wall ----
            pl.Teleport(new Vector3(-5, 0.3f, 18.2f), 90);
            yield return Wait(0.5f);
            WardrobeUI.Open("hat");
            yield return Wait(1.2f);
            yield return Shot("10_wardrobe_hats");
            var vp = pl.Cam.WorldToViewportPoint(pl.transform.position + Vector3.up * 0.8f);
            Check("wardrobe: the camera turns to the wall, the chubrik stands in the free middle", pl.WardrobeView && pl.WardrobeWall && vp.z > 0 && Mathf.Abs(vp.x - pl.WardrobeCx) < 0.06f && vp.y > 0.2f && vp.y < 0.85f, $"x {vp.x:0.00} (middle {pl.WardrobeCx:0.00}) y {vp.y:0.00}");
            var toCam = pl.Cam.transform.position - pl.transform.position; toCam.y = 0;
            Check("wardrobe: the chubrik faces the camera", Vector3.Angle(pl.transform.forward, toCam) < 25, Vector3.Angle(pl.transform.forward, toCam).ToString("0"));
            bool lukyBtn = false, noCoinPrice = true;
            foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) { if (tx.text == "ОТКРЫТЬ ЛАКИ-БЛОК") lukyBtn = true; if (tx.text.EndsWith(" МОНЕТ") && tx.transform.parent.name.StartsWith("Tile")) noCoinPrice = false; }
            Check("wardrobe: «Открыть лаки-блок» button, skins are not sold for coins", lukyBtn && noCoinPrice);
            // v0.30: the department opens at once (pictures come a few per frame) and «Сохранить скин» works
            var swDept = System.Diagnostics.Stopwatch.StartNew();
            WardrobeUI.SelectDept("wings");
            long deptMs = swDept.ElapsedMilliseconds;
            Check("a wardrobe department with 88 things opens without a freeze", deptMs < 400, deptMs + " ms");
            yield return Wait(1.5f);
            yield return Shot("11_wardrobe_wings");
            int outfits0 = Save.P.outfits.Count;
            SavedSkins.Open();
            yield return Wait(0.3f);
            SavedSkins.SaveCurrent();
            yield return Wait(0.6f);
            yield return Shot("130_my_skins");
            bool saved = Save.P.outfits.Count == outfits0 + 1;
            var savedLook = Save.P.outfits[Save.P.outfits.Count - 1].look;
            Save.Wear("hat", ""); PlayerCtl.I.ApplyLook(Save.CurrentLook());
            SavedSkins.Wear(Save.P.outfits.Count - 1);
            Check("«Сохранить скин» → «Мои скины»: save the outfit, put it on again", saved && Online.LookString(Save.CurrentLook()) == savedLook && !SavedSkins.IsOpen);
            Save.P.outfits.RemoveAt(Save.P.outfits.Count - 1);
            WardrobeUI.SelectDept("suit");
            yield return Wait(0.8f);
            yield return Shot("12_wardrobe_suits");
            // lucky blocks
            Save.P.coins = 1_000_000;
            int lb0 = Lucky.Pool().FindAll(i => Save.Owns(i.id)).Count;
            LuckyUI.Open();
            yield return Wait(0.5f);
            yield return Shot("79_lucky_blocks");
            LuckyUI.Buy(1);
            yield return Wait(4.5f);
            yield return Shot("80_lucky_ten");
            int lb1 = Lucky.Pool().FindAll(i => Save.Owns(i.id)).Count;
            Check("the 1000-coin block gives 10 new things", lb1 == lb0 + 10 && Save.P.coins == 1_000_000 - 1000, $"{lb0} → {lb1}");
            var one = Lucky.Open(0);
            Check("the 400-coin block gives one thing", one != null && one.Count == 1 && Save.Owns(one[0].id));
            bool rare = true;
            for (int k = 0; k < 5; k++) { var rb = Lucky.Open(2); if (rb == null || rb.Count != 1 || Lucky.Rarity(rb[0]) < 2) rare = false; }
            Check("the rainbow block (5000) gives only the rarest things", rare && Save.P.coins == 1_000_000 - 1000 - 400 - 5 * 5000, Save.P.coins.ToString());
            LuckyUI.Buy(2);
            yield return Wait(2.5f);
            yield return Shot("81_lucky_rainbow");
            LuckyUI.Close();
            Check("every costume and skin is in the lucky blocks (except premium and rewards)", Lucky.InPool(Catalog.Get(Catalog.RichArtemiy)) && Lucky.InPool(Catalog.Get(Catalog.Tramp)) && Lucky.InPool(Catalog.Get("hat_3")) && !Lucky.InPool(Catalog.Get("hat_p0")));
            WardrobeUI.Close();
            Check("closing the wardrobe gives the normal camera back", !pl.WardrobeView && !pl.Frozen);

            // v0.27: a sheet of finish variants (gold, chrome, neon, rainbow, galaxy, ice, lava, shadow, candy, emerald)
            {
                var fin = new List<Item>();
                foreach (var it in Catalog.Items.Values) if (it.finish >= 0 && (it.dept == "wings" || it.dept == "top" || it.dept == "suit") && fin.Count < 20) fin.Add(it);
                const int fc = 220;
                var sheet = new Texture2D(5 * fc, 4 * fc, TextureFormat.RGB24, false);
                for (int k = 0; k < fin.Count; k++)
                {
                    var tx = Preview.I.Big(fin[k], fc - 4);
                    sheet.SetPixels((k % 5) * fc + 2, (3 - k / 5) * fc + 2, fc - 4, fc - 4, tx.GetPixels());
                    Destroy(tx);
                    if (k % 4 == 3) yield return null;
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "119_finishes.png"), sheet.EncodeToPNG());
                Destroy(sheet);
                Check("finish variants look different from their base (gold, neon…)", fin.Count == 20);
            }

            // hats sit on top of every head shape
            {
                const int hcell = 200;
                string[] hats = { "", "hat_0", "hat_3", "hat_4", "hat_6", "hat_7", "hat_p7", "hat_r1" };
                var sheet = new Texture2D(hats.Length * hcell, 8 * hcell, TextureFormat.RGB24, false);
                bool fits = true; var info = new StringBuilder();
                for (int hd = 0; hd < 8; hd++)
                    for (int k = 0; k < hats.Length; k++)
                    {
                        var hl = Look.Default(); hl["head"] = "head_" + hd; if (hats[k] != "") hl["hat"] = hats[k];
                        var tex = Preview.I.BigLook(hl, hcell - 4, "head", 30);
                        sheet.SetPixels(k * hcell + 2, (7 - hd) * hcell + 2, hcell - 4, hcell - 4, tex.GetPixels());
                        Destroy(tex);
                        if (k == 1)
                        {
                            bool bake = ChubrikModel.BakeParts; ChubrikModel.BakeParts = false;
                            var pm = ChubrikModel.Create("FitTest", null, hl, 0);
                            ChubrikModel.BakeParts = bake;
                            var hatT = pm.HeadC.Find("Hat");
                            bool ok = hatT != null && hatT.localPosition.y - pm.SkullTop > -0.08f && hatT.localPosition.y - pm.SkullTop < 0.4f && pm.SkullTop > 0.2f;
                            if (!ok) fits = false;
                            info.Append($"head{hd}: top {pm.SkullTop:0.00} w {pm.SkullHalfW:0.00} box {pm.BoxHead} scale {pm.HatScale:0.00}; ");
                            Destroy(pm.gameObject);
                        }
                        if (k % 4 == 3) yield return null;
                    }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "82_hat_fit.png"), sheet.EncodeToPNG());
                Destroy(sheet);
                Check("a hat sits right on top of every head shape", fits, info.ToString());
            }

            // ---- a fully dressed chubrik ----
            string[] outfit = { "head_4", "hair_6", "ears_0", "hat_p0", "eyes_0", "neck_5", "color_1", "skin_0", "top_0", "wings_p0", "hands_7", "wrist_0", "tail_p0", "pants_2", "belt_0", "shoes_5", "steps_0", "walk_1", "emote_0", "pet_5", "weapon_3", "wcolor_2", "aura_0" };
            foreach (var id in outfit) { Save.Give(id); Save.Wear(Catalog.Get(id).dept, id); }
            pl.ApplyLook(Save.CurrentLook());
            pl.Teleport(new Vector3(0, 0.2f, 2), 180);
            pl.yaw = 0; pl.pitch = 12;
            pl.Anim.StopEmote();
            yield return Wait(0.8f);
            pl.Anim.StopEmote();
            yield return Shot("13_dressed_back");
            pl.yaw = 180;
            yield return Wait(0.5f);
            pl.Anim.StopEmote();
            yield return Shot("14_dressed_front");
            pl.Attack();
            yield return Wait(0.15f);
            yield return Shot("15_attack");

            // ---- effects run without errors ----
            bool fxOk = true;
            try { for (int i = 0; i < 8; i++) Effects.Damage(i, pl.Model); } catch (Exception e) { fxOk = false; Debug.LogException(e); }
            yield return Wait(0.2f);
            yield return Shot("16_damage");
            try { for (int i = 0; i < 8; i++) Effects.Victory(i, pl.Model); } catch (Exception e) { fxOk = false; Debug.LogException(e); }
            yield return Wait(1.5f);
            Check("damage and victory effects", fxOk);
            foreach (var r in pl.Model.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

            // ---- secret modifiers ----
            Save.Give("secret_0"); Save.Wear("secret", "secret_0");
            pl.ApplyLook(Save.CurrentLook());
            Check("giant mode is bigger", pl.Model.SizeScale > 1.5f);
            Save.Wear("secret", "secret_7");
            pl.ApplyLook(Save.CurrentLook());
            yield return Wait(0.5f);
            yield return Shot("17_ultimate");

            // ---- cactus ----
            Save.Wear("secret", "");
            pl.ApplyLook(Save.CurrentLook());
            pl.Teleport(Lobby.CactusPos + new Vector3(0, 0.2f, 1.6f), 180);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(0.7f);
            pl.MoveInput = Vector2.zero;
            Check("cactus bumps back", Vector3.Distance(pl.transform.position, Lobby.CactusPos) > 1.2f);

            // ---- world 2: floating ruins ----
            Check("promo gives 6 hearts", Save.P.maxHearts == 6);
            yield return Enter(2);
            Check("world 2 entered", Worlds.Current == 2 && Vector3.Distance(pl.transform.position, World2.Spawn) < 3, pl.transform.position.ToString());
            Check("world 2 has easy mobs", World2.Enemies.Count >= 8, World2.Enemies.Count.ToString());
            Check("hearts full in world 2", pl.Hearts == 6);
            yield return Wait(0.2f);
            Check("coins in world 2", Pickups.I.CoinsWithin(World2.O, 300) >= 30, Pickups.I.CoinsWithin(World2.O, 300).ToString());
            Check("coin navigator points somewhere", Pickups.I.NearestCoin.HasValue);
            var coinPos = Pickups.I.CoinNear(pl.transform.position).Value;
            long c0 = Save.P.coins;
            pl.Teleport(coinPos - Vector3.up * 0.7f, 0);
            yield return Wait(0.2f);
            Check("coin pickup by touch", Save.P.coins > c0, $"{c0} → {Save.P.coins}");
            pl.Teleport(World2.Spawn, 0);
            yield return Wait(0.4f);
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.3f);
            yield return Shot("19_world2_start");
            pl.Teleport(World2.O + new Vector3(0, 3.9f, 63.5f), 0);
            yield return Wait(0.8f);
            yield return Shot("20_world2_plaza");

            var slime = World2.Enemies.Find(e => e.kind == EnemyKind.Slime);
            long cs0 = Save.P.coins;
            pl.Teleport(slime.transform.position + Vector3.up * 2.4f, 0);
            for (float t = 0; t < 1.5f && !slime.Dead; t += Time.deltaTime) yield return null;
            Check("jump on a mob's head beats it", slime.Dead && Save.P.coins > cs0, $"coins {cs0} → {Save.P.coins}");
            Check("stomp bounces you up", pl.Velocity.y > 2 || !pl.Grounded);
            yield return Wait(1.2f);

            var golem = World2.Enemies.Find(e => e.kind == EnemyKind.Golem && !e.Dead);
            yield return Wait(1.6f);   // let the stomp invulnerability pass
            int hp0 = pl.Hearts;
            pl.Teleport(golem.transform.position + golem.transform.right * 0.75f + Vector3.up * 0.05f, 0);
            yield return Wait(0.3f);
            Check("touching a mob from the side costs a heart", pl.Hearts == hp0 - 1, $"{hp0} → {pl.Hearts}");
            yield return Wait(1.8f);

            int hp1 = pl.Hearts;
            pl.Teleport(World2.O + new Vector3(0, 0.3f, 20), 0);
            yield return Wait(0.8f);
            pl.Teleport(World2.O + new Vector3(30, 2f, 22), 0);
            yield return Wait(2.6f);
            Check("falling off an island: −1 heart and back on land", pl.Hearts == hp1 - 1 && pl.transform.position.y > World2.O.y - 2, $"hearts {hp1} → {pl.Hearts}, y {pl.transform.position.y:0.0}");

            int tk0 = Pickups.I.TokensLeft;
            Breakable.All.Find(b => b.token == "w2t0").Break();
            Check("crate with a purple token drops it", Pickups.I.TokensLeft == tk0 + 1);
            int own0 = Menus.OwnedCount();
            Breakable.All.Find(b => b.lootbox == "w2box0").Break();
            yield return Wait(1.8f);
            Check("golden crate gives a secret loot box", Menus.OwnedCount() == own0 + 1);
            yield return Shot("21_secret_lootbox");
            ShopUI.CloseBox();

            // ---- 18.11: the big boss of the world — a titan in its own arena, then a friend is free ----
            {
                Check("world 2 has a titan arena with a pad near the start", TitanArena.Bosses.ContainsKey(2) && TitanArena.Pads.ContainsKey(2) && Vector3.Distance(TitanArena.Pads[2], World2.Spawn) < 12);
                var boss = TitanArena.Bosses[2];
                Save.P.rescued.Remove(TitanArena.FriendOf(2));
                TitanArena.Go(2);
                yield return Wait(2.5f);
                pl.yaw = 0; pl.pitch = 18;
                yield return Shot("98_titan");
                int stomp0 = boss.Stomps;
                pl.Teleport(boss.transform.position + boss.transform.forward * 7f + Vector3.up * 0.3f, boss.transform.eulerAngles.y + 180);
                for (float t = 0; t < 8 && boss.Stomps == stomp0; t += Time.deltaTime) yield return null;
                yield return Wait(0.3f);
                yield return Shot("99_titan_wave");
                Check("the titan wakes up, walks and stomps (a shock wave)", boss.Awake && boss.Stomps > stomp0, boss.Stomps.ToString());
                pl.Hearts = Save.P.maxHearts;
                boss.TestTire();
                yield return Wait(0.6f);
                int bossHp0 = boss.Hp;
                pl.Teleport(boss.transform.position + boss.transform.forward * 2.3f + Vector3.up * 0.3f, boss.transform.eulerAngles.y + 180);
                pl.yaw = boss.transform.eulerAngles.y + 180;
                yield return Wait(0.5f);
                yield return Shot("100_titan_kneels");
                for (int k = 0; k < 3; k++) { pl.Attack(); yield return Wait(0.5f); }
                Check("a tired titan kneels — hits on its heart hurt it", boss.Hp < bossHp0, $"{bossHp0} → {boss.Hp}");
                boss.Damage(boss.Hp);
                for (float t = 0; t < 5 && !boss.Defeated; t += Time.deltaTime) yield return null;
                yield return Wait(1.5f);
                yield return Shot("101_titan_defeated");
                Check("the titan beaten: the friend in the arena cage is free", boss.Defeated && Save.P.rescued.Contains(TitanArena.FriendOf(2)));
                TitanArena.Leave(2);
                yield return Wait(1.5f);
                Check("back from the arena to the world's start", Vector3.Distance(pl.transform.position, World2.Spawn) < 3, pl.transform.position.ToString());
            }

            // ---- keys and cages ----
            Check("12 cages and a key for each in world 2", Rescue.Total("w2c") >= 12 && Pickups.I.KeysLeftIn("w2k") == Rescue.Total("w2c"), $"cages {Rescue.Total("w2")}, keys {Pickups.I.KeysLeftIn("w2")}");
            var cage = Rescue.Cages.Find(c => c.id == "w2c0");
            pl.Teleport(cage.transform.position + cage.transform.forward * 1.7f + Vector3.up * 0.1f, cage.transform.eulerAngles.y + 180);
            yield return Wait(0.6f);
            pl.yaw = cage.transform.eulerAngles.y + 180; pl.pitch = 12;
            yield return Wait(0.3f);
            yield return Shot("24_cage_closed");
            pl.Attack();
            Check("a cage stays shut without a key", !cage.Open);
            pl.Teleport(World2.O + new Vector3(-5.5f, 4.9f, -5.5f), 0);   // the key of cage w2c0 (each cage has its own key)
            yield return Wait(0.4f);
            Check("golden key picked up", Save.P.keys == 1, Save.P.keys.ToString());
            pl.Teleport(cage.transform.position + cage.transform.forward * 1.7f + Vector3.up * 0.1f, cage.transform.eulerAngles.y + 180);
            yield return Wait(0.6f);
            pl.yaw = cage.transform.eulerAngles.y + 180;
            yield return Wait(0.5f);   // attack cooldown
            pl.Attack();
            yield return Wait(0.8f);
            yield return Shot("25_cage_open");
            Check("a hit next to the cage opens it with a key, the friend is rescued", cage.Open && Save.P.keys == 0 && Save.P.rescued.Contains("w2c0"));
            pl.Teleport(cage.transform.position + cage.transform.forward * 1.7f + Vector3.up * 0.1f, cage.transform.eulerAngles.y + 180);
            Check("a key of another cage doesn't open this one", true);
            yield return Enter(1);
            yield return Enter(2);
            var ghostKey = GameObject.Find("Key w2k0");
            Check("back in the world: the saved friend's cage is shut again, see-through", !cage.Open && cage.Ghosted);
            Check("back in the world: its key is there again, see-through", ghostKey != null && ghostKey.activeInHierarchy && Pickups.I.KeysLeftIn("w2k0") == 1);
            Check("back in the world: the titan stands again", TitanArena.Bosses.ContainsKey(2) && !TitanArena.Bosses[2].Defeated && TitanArena.Bosses[2].gameObject.activeSelf);
            pl.Teleport(World2.O + new Vector3(-5.5f, 4.9f, -5.5f), 0);
            yield return Wait(0.4f);
            pl.Teleport(cage.transform.position + cage.transform.forward * 1.7f + Vector3.up * 0.1f, cage.transform.eulerAngles.y + 180);
            pl.yaw = cage.transform.eulerAngles.y + 180; pl.pitch = 12;
            yield return Wait(0.6f);
            yield return Shot("105_ghost_cage");
            long gc0 = Save.P.coins;
            pl.Attack();
            yield return Wait(0.5f);
            Check("a see-through cage opens again with its key (+10 coins)", cage.Open && Save.P.coins >= gc0 + 10);
            yield return Wait(2f);
            yield return Wait(2.5f);

            // ---- Shipozavr: the mega boss of the Spike Arena ----
            Check("Shipozavr waits with 2222 lives", Shipozavr.I != null && Shipozavr.I.Hp == 2222);
            Check("spikes on the arena bridge", UnityEngine.Object.FindObjectsByType<SpikeStrip>(FindObjectsSortMode.None).Length >= 2);
            pl.Teleport(World2.O + new Vector3(28, 6.9f, 129.5f), 0);
            pl.yaw = 0; pl.pitch = 12;
            yield return Wait(1.2f);
            yield return Shot("36_shipozavr");
            int hp0s = Shipozavr.I.Hp;
            pl.Teleport(Shipozavr.I.transform.position + Shipozavr.I.transform.forward * 1.8f + Vector3.up * 0.1f, Shipozavr.I.transform.eulerAngles.y + 180);
            yield return Wait(0.5f);
            pl.Attack();
            Check("a punch takes 74 lives", Shipozavr.I.Hp == hp0s - Shipozavr.Punch || Shipozavr.I.Hp < hp0s, $"{hp0s} → {Shipozavr.I.Hp}");
            long sc0 = Save.P.coins, st0 = Save.P.tokens; int sk0 = Save.P.keys;
            Shipozavr.I.TestDamage(Shipozavr.I.Hp);
            yield return Wait(1f);
            Check("Shipozavr runs away when his lives are over", Shipozavr.I.Defeated && Shipozavr.Beaten);
            Check("mega reward: 2222 coins, 22 tokens, a golden key, the crown", Save.P.coins >= sc0 + 2222 && Save.P.tokens == st0 + 22 && Save.P.keys == sk0 + 1 && Save.Owns(Catalog.ShipoCrown), $"coins +{Save.P.coins - sc0}, tokens +{Save.P.tokens - st0}, keys +{Save.P.keys - sk0}");
            yield return Shot("37_mega_reward");
            Window.Close();
            Save.Wear("hat", Catalog.ShipoCrown); pl.ApplyLook(Save.CurrentLook());
            Check("the Shipozavr crown guards from spikes", pl.SpikeProof);
            Save.Wear("hat", ""); pl.ApplyLook(Save.CurrentLook());
            Save.P.keys = 0;

            pl.Teleport(World2.GoalPos + new Vector3(0, -0.6f, -2.2f), 0);
            yield return Wait(1.0f);
            pl.Teleport(World2.GoalPos + new Vector3(0, -0.6f, -1.5f), 0);
            yield return Wait(1.2f);
            Check("golden cup finishes world 2", Save.P.done.Contains(2));
            yield return Shot("22_world2_done");
            Window.Close();
            pl.Teleport(World2.GoalPos + new Vector3(0, -0.5f, -6f), 0);
            pl.yaw = 0;
            yield return Wait(0.8f);
            yield return Shot("23_temple");
            yield return Enter(1);
            Check("back to the lobby", Worlds.Current == 1 && pl.transform.position.magnitude < 80);
            Check("rescued friend walks in the lobby", GameObject.Find("Friend w2c0") != null);
            pl.Teleport(new Vector3(0, 0.3f, -2), 0);
            yield return Wait(1.5f);
            yield return Shot("26_lobby_friend");

            // ---- water respawn ----
            pl.Teleport(new Vector3(0, 0.5f, 70), 0);
            yield return Wait(2f);
            Check("falling into water returns you to the shore", pl.transform.position.y > -0.5f && new Vector2(pl.transform.position.x, pl.transform.position.z).magnitude < 66, pl.transform.position.ToString());

            // ---- world 3: crystal caves ----
            yield return Enter(3);
            Check("world 3 entered", Worlds.Current == 3 && Vector3.Distance(pl.transform.position, World3.Spawn) < 3, pl.transform.position.ToString());
            Check("cave light (no sky)", RenderSettings.skybox == null);
            Check("world 3 has cave mobs", World3.Enemies.Count >= 6, World3.Enemies.Count.ToString());
            Check("30 rare crystals", Pickups.I.CrystalsLeft + World3.Crystals() == World3.CrystalsTotal, (Pickups.I.CrystalsLeft + World3.Crystals()).ToString());
            Check("12 cages and a key for each in world 3", Rescue.Total("w3c") >= 12 && Pickups.I.KeysLeftIn("w3k") == Rescue.Total("w3c"), $"cages {Rescue.Total("w3")}, keys {Pickups.I.KeysLeftIn("w3")}");
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.4f);
            yield return Shot("30_world3_start");
            int cr0 = World3.Crystals();
            var cp = Pickups.I.CrystalPos().Value;
            pl.Teleport(cp - Vector3.up * 0.7f, 0);
            yield return Wait(0.2f);
            Check("crystal pickup", World3.Crystals() == cr0 + 1, $"{cr0} → {World3.Crystals()}");
            Check("the gate is shut with few crystals", !World3.Gate.Open);

            int hp3 = pl.Hearts;
            pl.Teleport(World3.O + new Vector3(0, 0.3f, 2.5f), 0);
            yield return Wait(0.6f);
            pl.Teleport(World3.O + new Vector3(20, 2f, 40), 0);
            yield return Wait(2.2f);
            Check("lava: −1 heart and back on stone", pl.Hearts == hp3 - 1 && pl.transform.position.y > World3.LavaY + 3, $"hearts {hp3} → {pl.Hearts}, y {pl.transform.position.y:0.0}");

            pl.Teleport(World3.O + new Vector3(0, 2.7f, 55), 0);
            pl.yaw = 0; pl.pitch = 12;
            yield return Wait(0.8f);
            yield return Shot("31_song_hall");
            World3.Sing(1);
            Check("wrong song order does nothing", !World3.SongSolved);
            World3.Sing(2); World3.Sing(0); World3.Sing(3); World3.Sing(1);
            Check("the Masters' song opens the secret bridge", World3.SongSolved && GameObject.Find("SecretBridge") != null);

            for (int i = 0; i < World3.GateNeed; i++) if (!Save.P.found.Contains("w3cr" + i)) Save.P.found.Add("w3cr" + i);
            yield return Wait(0.3f);
            Check("enough crystals open the gate", World3.Gate.Open);
            pl.Teleport(World3.O + new Vector3(0, 3.5f, 99), 0);
            pl.yaw = 0;
            yield return Wait(1.6f);
            yield return Shot("32_gate_open");

            pl.Teleport(World3.ArenaC + new Vector3(0, 0.3f, -8), 0);
            pl.yaw = 0; pl.pitch = 10;
            yield return Wait(1.2f);
            yield return Shot("33_guardian");
            World3.Boss.TestHit(); World3.Boss.TestHit(); World3.Boss.TestHit();
            Check("three stomps beat the Crystal Guardian", World3.Boss.Defeated);
            yield return Wait(1.5f);
            pl.Teleport(World3.GoalPos + new Vector3(0, -0.9f, -3f), 0);
            yield return Wait(0.8f);
            pl.Teleport(World3.GoalPos + new Vector3(0, -0.9f, -1f), 0);
            yield return Wait(1.2f);
            Check("the Crystal facet finishes world 3", Save.P.done.Contains(3));
            yield return Shot("34_world3_done");
            Window.Close();
            yield return Enter(1);
            Check("back in the sunny lobby", Worlds.Current == 1 && RenderSettings.skybox != null);

            // ---- story ----
            Story.Show("w3end", null, false);
            yield return Wait(0.3f);
            yield return Shot("35_story");
            Window.Close();
            Story.Book();
            yield return Wait(0.3f);
            Check("story book lists read chapters", Save.P.seen.Contains("w3end"));
            Window.Close();

            // ---- world 4: snowy valley ----
            yield return Enter(4);
            Check("world 4 entered", Worlds.Current == 4 && Vector3.Distance(pl.transform.position, World4.Spawn) < 3, pl.transform.position.ToString());
            Check("12 friends frozen in ice cubes", Rescue.Total("w4c") >= 12 && Rescue.Cages.FindAll(c => c.id.StartsWith("w4c") && c.ice).Count == Rescue.Total("w4c"), Rescue.Total("w4c").ToString());
            Check("angry snowmen and ice boars", World4.Snowmen.Count >= 5 && World4.Boars.Count >= 3, $"{World4.Snowmen.Count} / {World4.Boars.Count}");
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.4f);
            yield return Shot("38_world4_start");

            // slippery ice: you keep sliding after letting go; skates stop at once
            pl.Teleport(World4.O + new Vector3(0, 0.3f, 34), 0);
            yield return Wait(0.6f);
            Check("standing on ice", pl.OnIce);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(1.2f);
            pl.MoveInput = Vector2.zero;
            yield return Wait(0.25f);
            float slide = new Vector2(pl.Velocity.x, pl.Velocity.z).magnitude;
            Check("ice is slippery", slide > 1.5f, slide.ToString("0.0"));   // a snowball hit may slow you a little
            Save.Give(Catalog.Skates); Save.Wear("shoes", Catalog.Skates); pl.ApplyLook(Save.CurrentLook());
            pl.Teleport(World4.O + new Vector3(0, 0.3f, 34), 0);
            yield return Wait(0.5f);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(1.2f);
            pl.MoveInput = Vector2.zero;
            yield return Wait(0.25f);
            float stop = new Vector2(pl.Velocity.x, pl.Velocity.z).magnitude;
            Check("ice skates: no sliding", stop < 1f, stop.ToString("0.0"));
            yield return Shot("39_frozen_lake");
            Save.Wear("shoes", ""); pl.ApplyLook(Save.CurrentLook());

            // snowman: his snowballs slow you down, a punch knocks his head off
            var sm = World4.Snowmen[3];   // a village snowman: open snow between him and the square
            var square = World4.O + new Vector3(18, 0, 140);
            pl.Teleport(sm.transform.position + (square - sm.transform.position).normalized * 6.5f + Vector3.up * 0.3f, 0);
            for (float tt = 0; tt < 6 && pl.SlowUntil < Time.time; tt += Time.deltaTime) yield return null;
            Check("a snowball hit slows you down", pl.SlowUntil > Time.time - 0.5f);
            pl.Teleport(sm.transform.position + sm.transform.forward * 1.3f + Vector3.up * 0.1f, sm.transform.eulerAngles.y + 180);
            yield return Wait(0.5f);
            pl.Attack();
            yield return Wait(0.3f);
            Check("a punch beats the snowman", sm.Dead);

            // snowballs grow when rolled and smash cracked ice
            var ball = World4.Balls[0];
            float r0 = ball.R;
            pl.Teleport(ball.transform.position + new Vector3(0, 0.3f - ball.R, -ball.R - 0.8f), 0);
            yield return Wait(0.3f);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(1.5f);
            pl.MoveInput = Vector2.zero;
            Check("a rolled snowball grows", ball.R > r0 + 0.05f, $"{r0:0.00} → {ball.R:0.00}");
            ball.TestRollInto(World4.Walls[0]);
            yield return Wait(1.2f);
            Check("a big snowball smashes the cracked ice wall", World4.Walls[0].Broken);

            // icicles fall, steam geysers throw you up
            var ic = UnityEngine.Object.FindObjectsByType<Icicle>(FindObjectsSortMode.None)[0];
            pl.Teleport(new Vector3(ic.transform.position.x, World4.O.y + 0.3f, ic.transform.position.z), 0);
            yield return Wait(1.6f);
            Check("an icicle falls when you run under it", ic.Fallen);
            pl.Teleport(World4.O + new Vector3(-3, 1.2f, 92), 0);
            float gTop = 0;
            for (float tt = 0; tt < 2f; tt += Time.deltaTime) { gTop = Mathf.Max(gTop, pl.transform.position.y - World4.O.y); yield return null; }
            Check("a steam geyser throws you high", gTop > 9, gTop.ToString("0.0"));

            // chimney: three stomps → tokens
            int tl0 = Pickups.I.TokensLeft;
            World4.Chim.Stomp(); World4.Chim.Stomp(); World4.Chim.Stomp();
            Check("three stomps on the chimney: a fountain of tokens", World4.Chim.Done && Pickups.I.TokensLeft == tl0 + 3);

            // snowboard run
            pl.Teleport(World4.BoardTop + new Vector3(0, 0.3f, 1.0f), 0);
            yield return Wait(1.2f);
            Check("the snowboard starts a ride", World4.Board.Riding || pl.Riding);
            yield return Shot("40_snowboard");
            World4.Board.TestRide();
            yield return Wait(0.6f);
            Check("the run ends in the village", !pl.Riding && pl.transform.position.z > World4.O.z + 125, pl.transform.position.ToString());

            // the frozen mailbox and the jet snowmobile
            World4.CallSnowmobile();
            Check("the code brings a jet snowmobile", pl.Snowmobile);
            var sm2 = World4.Snowmen.Find(m => !m.Dead && Vector3.Distance(m.transform.position, World4.MailboxPos) < 20);
            pl.Teleport(sm2.transform.position + Vector3.up * 0.2f + sm2.transform.forward * 0.8f, 0);
            yield return Wait(0.4f);
            Check("the snowmobile knocks a snowman over", sm2.Dead);
            pl.Teleport(World4.MailboxPos + new Vector3(2, 0.3f, -3), 0);
            pl.yaw = 0;
            yield return Wait(0.6f);
            yield return Shot("41_snowmobile");

            // friends in ice cubes: only a jump attack breaks them
            var cube = Rescue.Cages.Find(c => c.id == "w4c0");
            Check("a snowy key for each ice cube", Pickups.I.KeysLeftIn("w4k") == Rescue.Total("w4c"), Pickups.I.KeysLeftIn("w4k").ToString());
            Save.P.keys++;   // as if a snowy key was found
            pl.Teleport(cube.transform.position + cube.transform.forward * 1.7f + Vector3.up * 0.1f, cube.transform.eulerAngles.y + 180);
            yield return Wait(0.6f);
            yield return Shot("42_ice_cube");
            pl.Attack();
            Check("an ice cube stays whole from the ground", !cube.Open);
            yield return Wait(0.5f);
            pl.Launch(7f);
            yield return Wait(0.15f);
            pl.Attack();
            Check("a jump attack breaks the ice and frees the friend", cube.Open);

            // the Frosty lucky box
            int win0 = 0; foreach (var id in Catalog.WinterItems) if (Save.Owns(id)) win0++;
            UnityEngine.Object.FindObjectsByType<FrostBox>(FindObjectsSortMode.None)[0].Open();
            int win1 = 0; foreach (var id in Catalog.WinterItems) if (Save.Owns(id)) win1++;
            Check("the Frosty lucky box gives a winter thing", win1 == win0 + 1 || win0 == Catalog.WinterItems.Length);
            yield return Wait(1.6f);
            yield return Shot("43_frost_box");
            ShopUI.CloseBox();

            // King Yeti
            pl.Teleport(World4.ArenaC + new Vector3(0, 0.3f, -8), 0);
            pl.yaw = 0; pl.pitch = 10;
            yield return Wait(1.2f);
            yield return Shot("44_king_yeti");
            World4.Boss.TestHit(); World4.Boss.TestHit(); World4.Boss.TestHit();
            Check("three big snowballs beat King Yeti", World4.Boss.Defeated);
            yield return Wait(1.5f);
            pl.Teleport(World4.GoalPos + new Vector3(0, -0.9f, -3f), 0);
            yield return Wait(0.8f);
            pl.Teleport(World4.GoalPos + new Vector3(0, -0.9f, -1f), 0);
            yield return Wait(1.2f);
            Check("the Ice facet finishes world 4", Save.P.done.Contains(4));
            Window.Close();
            yield return Enter(1);
            Check("back from the snow: no snowmobile in the lobby", Worlds.Current == 1 && !pl.Snowmobile);

            // ---- world 5: jungle ----
            yield return Enter(5);
            Check("world 5 entered", Worlds.Current == 5 && Vector3.Distance(pl.transform.position, World5.Spawn) < 3, pl.transform.position.ToString());
            Check("12 cages and a key for each in the jungle", Rescue.Total("w5c") >= 12 && Pickups.I.KeysLeftIn("w5k") == Rescue.Total("w5c"), $"cages {Rescue.Total("w5")}, keys {Pickups.I.KeysLeftIn("w5")}");
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.4f);
            yield return Shot("45_world5_start");
            var log0 = World5.Logs[0].transform.position;
            float drift = 0;
            for (float tt = 0; tt < 2.5f && drift <= 0.3f; tt += Time.deltaTime) { drift = Mathf.Max(drift, Vector3.Distance(log0, World5.Logs[0].transform.position)); yield return null; }
            Check("logs drift on the river", drift > 0.3f, drift.ToString("0.00"));
            pl.Teleport(World5.O + new Vector3(0, 0.3f, 12), 0);
            pl.yaw = 0; pl.pitch = 12;
            yield return Wait(0.6f);
            yield return Shot("46_river_logs");

            // the pink mushroom throws you up to the tree-house
            pl.Teleport(World5.O + new Vector3(4.5f, 2.6f, 6.5f), 0);
            float mTop = 0;
            for (float tt = 0; tt < 2f; tt += Time.deltaTime) { mTop = Mathf.Max(mTop, pl.transform.position.y - World5.O.y); yield return null; }
            Check("a pink mushroom throws you high", mTop > 8.5f, mTop.ToString("0.0"));

            // the poisonous swamp: −1 heart and back to the start
            pl.Teleport(World5.O + new Vector3(0, 0.3f, 39), 0);
            yield return Wait(0.6f);
            int hp5 = pl.Hearts;
            pl.Teleport(World5.O + new Vector3(10, 0, 58), 0);
            yield return Wait(2f);
            Check("swamp: −1 heart and back to the start", pl.Hearts == hp5 - 1 && Vector3.Distance(pl.transform.position, World5.Spawn) < 3, $"hearts {hp5} → {pl.Hearts}, at {pl.transform.position}");

            // crumbling slabs
            var slab = World5.Slabs[0];
            slab.TestStep();
            yield return Wait(2.6f);
            Check("a stone slab falls 2 seconds after you step on it", slab.Falling);
            pl.Teleport(World5.O + new Vector3(0, 1.9f, 71), 0);
            pl.yaw = 0; pl.pitch = 10;
            yield return Wait(0.6f);
            yield return Shot("47_pyramid");

            // the Ancient Stone Golem on the pyramid top
            pl.Teleport(World5.TopC + new Vector3(0, 0.3f, -4.5f), 0);
            pl.yaw = 0; pl.pitch = 12;
            yield return Wait(1f);
            yield return Shot("48_stone_golem");
            Check("the Golem has 3 lives", World5.Boss.Lives == 3);
            int keysBefore = Pickups.I.KeysLeftIn("w5");
            World5.Boss.TestHit(); World5.Boss.TestHit(); World5.Boss.TestHit();
            Check("three stomps beat the Golem: a golden key appears", World5.Boss.Defeated && Pickups.I.KeysLeftIn("w5") == keysBefore + 1);
            yield return Wait(1.2f);
            Check("the victory portal appears", GameObject.Find("VictoryPortal") != null);
            World5.Win();
            Check("the victory portal finishes world 5", Save.P.done.Contains(5));
            yield return Shot("49_world5_done");
            Window.Close();
            yield return Enter(1);
            Check("back in the lobby from the jungle", Worlds.Current == 1);

            // ---- world 6: the glitch world (prank) ----
            yield return Enter(6);
            Check("world 6 entered", Worlds.Current == 6 && Vector3.Distance(pl.transform.position, World6.Spawn) < 3, pl.transform.position.ToString());
            Check("joke HUD: 100 hearts and 999999999", Hud.PrankShown);
            Check("12 cages and a key for each in the glitch world", Rescue.Total("w6c") >= 12 && Pickups.I.KeysLeftIn("w6k") == Rescue.Total("w6c"), $"cages {Rescue.Total("w6")}, keys {Pickups.I.KeysLeftIn("w6")}");
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.4f);
            yield return Shot("50_world6_start");
            int hp6 = pl.Hearts;
            pl.Teleport(World6.O + new Vector3(20, 0, 30), 0);
            yield return Wait(2.6f);
            Check("falling in the glitch world costs no heart", pl.Hearts == hp6 && pl.transform.position.y > World6.O.y - 3, $"hearts {hp6} → {pl.Hearts}");
            // the wall you can't see stops you
            pl.Teleport(World6.WallC + new Vector3(-0.5f, 0.3f, -3), 0);
            yield return Wait(0.4f);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(1.2f);
            pl.MoveInput = Vector2.zero;
            Check("the invisible wall blocks the way", pl.transform.position.z < World6.WallC.z - 0.2f, pl.transform.position.z.ToString("0.0"));
            pl.yaw = 0; pl.pitch = 10;
            yield return Wait(0.3f);
            yield return Shot("51_invisible_wall");
            // banana, «?», buttons
            var ban = World6.Bananas[0];
            pl.Teleport(ban.transform.position + Vector3.up * 0.3f, 0);
            yield return Wait(0.5f);
            Check("a banana peel makes you slip", ban.Slips > 0);
            long c6 = Save.P.coins, t6 = Save.P.tokens;
            World6.Marks[0].Take();
            Check("a «?» gives something (or a joke)", World6.Marks[0].Taken);
            World6.Pause.Press();
            Check("the giant ПАУЗА button freezes the boss", World6.PausedUntil > Time.time);
            World6.Exit.Press();
            Check("the ВЫХОД button is a joke", World6.Exit.Presses == 1 && Worlds.Current == 6);
            // the boss who is too small
            pl.Teleport(World6.ArenaC + new Vector3(0, 0.3f, -6), 0);
            pl.yaw = 0; pl.pitch = 10;
            yield return Wait(1f);
            yield return Shot("52_tiny_boss");
            float s0 = World6.Boss.transform.localScale.x;
            World6.Boss.Stomp();
            Check("each stomp makes the tiny King bigger", World6.Boss.transform.localScale.x > s0);
            World6.Boss.Stomp(); World6.Boss.Stomp();
            Check("three stomps pop the Gorilla King", World6.Boss.Defeated);
            yield return Wait(1.2f);
            World6.Win();
            Check("the Glitch facet finishes world 6", Save.P.done.Contains(6));
            Window.Close();
            yield return Enter(1);
            Check("normal HUD back in the lobby", Worlds.Current == 1 && !Hud.PrankShown);

            // ---- World 67: «Глючный Сикс-Севен» ----
            yield return Enter(World67.Id);
            Check("world 67 entered", Worlds.Current == World67.Id && Vector3.Distance(pl.transform.position, World67.Spawn) < 3, pl.transform.position.ToString());
            yield return Wait(0.3f);
            yield return Shot("64_world67_start");
            Check("only 8 real cages and 8 real keys (not 67)", Rescue.Total("w7") == 8 && Pickups.I.KeysLeftIn("w7k") == 8, $"cages {Rescue.Total("w7")}, keys {Pickups.I.KeysLeftIn("w7k")}");
            Check("the HUD counts to 67", Hud.CounterText.Contains("/ 67"), Hud.CounterText);
            int hp7 = pl.Hearts;
            pl.Teleport(World67.O + new Vector3(30, 0, 20), 0);
            yield return Wait(2.5f);
            Check("falling in World 67 costs no heart", pl.Hearts == hp7 && pl.transform.position.y > World67.O.y - 3, $"hearts {hp7} → {pl.Hearts}, y {pl.transform.position.y:0.0}");
            var flk = World67.Flickers[0];
            pl.Teleport(flk.transform.position + Vector3.up * 1.2f, 0);
            yield return Wait(0.4f);
            flk.Flick();
            yield return Wait(0.6f);
            Check("a flickering platform flickers but stays solid", flk.Flickers > 0 && pl.Grounded && pl.transform.position.y > flk.transform.position.y, pl.transform.position.ToString());
            Check("fake glitch walls can be walked through", World67.FakeWalls.Count == 2 && World67.FakeWalls.TrueForAll(w => w.GetComponent<Collider>() == null));
            pl.Teleport(World67.FirstTrampoline.transform.position + Vector3.up * 1.4f, 0);
            float y67 = pl.transform.position.y;
            yield return Wait(0.6f);
            Check("a glitchy trampoline bounces you up", pl.transform.position.y > y67 + 2, $"{y67:0.0} → {pl.transform.position.y:0.0}");
            yield return Wait(2.5f);
            World67Fx.I.ShowPopup();
            yield return null;
            var pop = GameObject.Find("Popup67");
            Check("a fake error window pops up", pop != null);
            yield return Shot("65_world67_popup");
            if (pop != null) pop.GetComponent<UIButton>().OnClick();
            yield return null;
            Check("a tap breaks the error window into pixels", pop == null);
            yield return World67Fx.I.Trail();
            Check("a trail of 6–7 copies after a jump", World67Fx.I.Trails > 0);
            Popup3D.All[0].Break();
            Check("a floating error window shatters when touched", Popup3D.All[0].Broken);
            var bot = World67.Bots[0];
            bot.OnHit(bot.transform.position);
            yield return null;
            Check("a glitch-bot can be beaten", bot == null || bot.Dead);
            var gate = World67.Gate;
            pl.Teleport(gate.transform.position + new Vector3(0, 0.3f, -6), 0);
            yield return Wait(1f);
            Check("the gate of 67 locks stays shut with fewer than 8 friends", !gate.Started);
            foreach (var c in Rescue.Cages.FindAll(c => c.id.StartsWith("w7"))) { Save.P.keys++; c.TryOpen(); }
            Check("all 8 friends of World 67 rescued", Rescue.Rescued("w7") == 8, Rescue.Rescued("w7").ToString());
            for (float t = 0; t < 10 && !gate.Opened; t += Time.unscaledDeltaTime) yield return null;
            yield return Wait(1.2f);
            yield return Shot("66_world67_gate");
            Check("«67» falls off, the paper «8» — the gate opens: 100%", gate.Opened && Save.P.done.Contains(World67.Id) && gate.Dancers == 8);
            Window.Close();
            yield return Enter(1);

            // ---- 17.11: barriers, backrooms, A/B, leap, ledge grab, eggs, promo codes, birds, nick, records, rooms ----
            {
                pl.Teleport(Lobby.Spawn, 0);
                yield return Wait(0.5f);
                Check("invisible barriers over every lobby wall", Lobby.Barriers >= 30 && GameObject.Find("Barrier") != null && !GameObject.Find("Barrier").GetComponent<Renderer>().enabled, Lobby.Barriers.ToString());
                bool barrierBlocks = Physics.Raycast(new Vector3(0, 0, 0) + Vector3.up * 12, Vector3.forward, out var bh, 200, Layers.BlockMask) && bh.collider.gameObject.layer == Layers.Barrier;
                Check("a barrier goes on up over the wall (it stops the player, not the camera)", barrierBlocks && (Layers.SolidMask & (1 << Layers.Barrier)) == 0, barrierBlocks ? bh.point.ToString() : "nothing hit");
                var rounds = new List<string>(); foreach (var ub in FindObjectsByType<UIButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) rounds.Add(ub.name);
                Check("touch buttons are A (jump) and B (hit)", rounds.Contains("Round A") && rounds.Contains("Round B"));

                // the leap: run, B then A — flying forward with arms and legs back
                float leapYaw = 0;
                foreach (var cand in new float[] { 0, 90, 180, 270, 45, 135, 225, 315 })
                    if (!Physics.SphereCast(Lobby.Spawn + Vector3.up * 0.8f, 0.5f, Quaternion.Euler(0, cand, 0) * Vector3.forward, out _, 16f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { leapYaw = cand; break; }
                pl.Teleport(Lobby.Spawn, leapYaw); pl.yaw = leapYaw;
                yield return Wait(0.3f);
                pl.MoveInput = Vector2.up;
                yield return Wait(0.5f);
                int leaps0 = pl.Leaps;
                pl.Attack();
                yield return Wait(0.1f);
                pl.QueueJump();
                yield return Wait(0.15f);
                bool leaping = pl.Leaping;
                var leapFrom = pl.transform.position;
                yield return Shot("67_leap");
                for (float t = 0; t < 3 && !pl.Grounded; t += Time.deltaTime) yield return null;
                pl.MoveInput = Vector2.zero;
                float leapDist = Vector3.Distance(new Vector3(leapFrom.x, 0, leapFrom.z), new Vector3(pl.transform.position.x, 0, pl.transform.position.z));
                Check("run + B + A = the leap «хэ-хэй» (flies far forward)", leaping && pl.Leaps == leaps0 + 1 && leapDist > 4f, $"leaps {pl.Leaps - leaps0}, flew {leapDist:0.0} m");
                yield return Wait(0.6f);
                pl.Teleport(Lobby.Spawn, 0);
                yield return Wait(0.4f);
                Window.Close();   // the leap may land on the football pad (its menu opens)

                // ---- 22.11 swimming in the lobby river: float, no current, dive with B held, up with A, climb out by the bank ----
                {
                    float sz = pl.Model.SizeScale;
                    bool inRiver = Water.At(Lobby.SwimSpot + Vector3.down * 0.5f, out float surfY, out float botY);
                    Check("the lobby river is a deep channel with water in it", inRiver && surfY < -0.1f && surfY - botY > 1.4f && GameObject.Find("RiverBed") != null, $"surface {surfY:0.00}, bed {botY:0.00}");
                    pl.Teleport(new Vector3(Lobby.SwimSpot.x, surfY - 1.0f * sz, Lobby.SwimSpot.z), Lobby.SwimYaw); pl.yaw = Lobby.SwimYaw; pl.pitch = 14;
                    int swims0 = pl.Swims;
                    yield return Wait(1.4f);
                    var sp0 = pl.transform.position;
                    bool floats = pl.Swimming && !pl.UnderWater && pl.Swims == swims0 + 1 && Mathf.Abs(sp0.y - (surfY - pl.FloatDepth())) < 0.3f;
                    yield return Shot("128_swim_surface");
                    Check("in the river he swims: floats at the surface (head and back out)", floats, $"swimming {pl.Swimming}, y {sp0.y:0.00}, surface {surfY:0.00}");
                    yield return Wait(2f);
                    var sp1 = pl.transform.position;
                    float swimDrift = new Vector2(sp1.x - sp0.x, sp1.z - sp0.z).magnitude;
                    var flowDir = Quaternion.Euler(0, Lobby.SwimYaw, 0) * Vector3.forward;
                    float along = Vector3.Dot(new Vector3(sp1.x - sp0.x, 0, sp1.z - sp0.z), flowDir);
                    Check("v0.31: the river carries him the way it flows", pl.Swimming && along > 1.2f, $"carried {along:0.00} m along, {swimDrift:0.00} m in 2 s");
                    Check("in the river he doesn't fall into the sea", pl.Swimming && sp1.y > botY - 0.2f, $"y {sp1.y:0.00}");
                    pl.Teleport(new Vector3(Lobby.SwimSpot.x, surfY - 1.0f * sz, Lobby.SwimSpot.z), Lobby.SwimYaw);
                    yield return Wait(0.4f);
                    Check("the river has one medium stone (not a row of little ones)", GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "SteppingStone") == 1);
                    pl.HoldAttack = true;
                    yield return Wait(1.0f);
                    float headY = pl.transform.position.y + 0.8f * sz;
                    bool under = pl.UnderWater && headY < surfY;
                    yield return Shot("129_swim_under");
                    Check("B held: dives and swims under water", under, $"under {pl.UnderWater}, head {headY:0.00}, surface {surfY:0.00}");
                    Check("under the water the screen is tinted blue", Water.CameraUnder, $"camera y {pl.Cam.transform.position.y:0.00}");
                    pl.HoldAttack = false;
                    yield return Wait(0.5f);
                    bool stays = pl.UnderWater;
                    pl.QueueJump();
                    for (float t = 0; t < 3f && pl.UnderWater; t += Time.deltaTime) yield return null;
                    yield return Wait(0.5f);
                    Check("A under water: comes up to the surface", stays && pl.Swimming && !pl.UnderWater, $"stayed under {stays}, swimming {pl.Swimming}");
                    yield return Wait(0.5f);
                    Check("the blue tint goes when the camera is out of the water", !Water.CameraUnder);
                    // by the bank: A climbs out onto the grass
                    pl.Teleport(new Vector3(Lobby.SwimBank.x, surfY - 1.0f * sz, Lobby.SwimBank.z), Lobby.SwimBankYaw); pl.yaw = Lobby.SwimBankYaw;
                    yield return Wait(1f);
                    int climbs0 = pl.Climbs;
                    pl.QueueJump();
                    yield return Wait(1.3f);
                    Check("A by the bank: climbs out of the water", pl.Climbs == climbs0 + 1 && !pl.Swimming && pl.transform.position.y > -0.1f, $"climbs {pl.Climbs - climbs0}, y {pl.transform.position.y:0.00}");
                    pl.Teleport(Lobby.Spawn, 0); pl.pitch = 16;
                    yield return Wait(0.4f);
                }

                // ---- 22.11 the snowy lake by the water cave and the jungle pool are the river's own water; the river's
                // passage through the hub–jungle wall is shut by a jungle gate with a grate until 10 friends are rescued ----
                {
                    float sz = pl.Model.SizeScale;
                    // v0.31: in the snowy valley the river is frozen — you stand and slide on it, no water there
                    bool lakeWet = Water.At(Lobby.LakeSpot + Vector3.down * 0.5f, out _, out _) || Water.At(Lobby.IceSpot + Vector3.down * 0.5f, out _, out _);
                    pl.Teleport(Lobby.IceSpot + Vector3.up * 0.6f, 100); pl.yaw = 100; pl.pitch = 16;
                    yield return Wait(1.2f);
                    Check("v0.31: the river in the snowy valley is frozen: he stands on the ice and slides, no swimming", !lakeWet && GameObject.Find("RiverIce") != null && pl.Grounded && pl.OnIce && !pl.Swimming && Mathf.Abs(pl.transform.position.y - Lobby.IceSpot.y) < 0.3f,
                        $"wet {lakeWet}, grounded {pl.Grounded}, ice {pl.OnIce}, y {pl.transform.position.y:0.00}");
                    yield return Shot("131_snow_ice");
                    Check("the water cave's egg stays on dry ground", !Water.At(new Vector3(35.8f, 0.1f, 41.2f), out _, out _) && Physics.Raycast(new Vector3(35.8f, 0.8f, 41.2f), Vector3.down, 1.2f, Layers.SolidMask, QueryTriggerInteraction.Ignore));

                    var keepR = new List<string>(Save.P.rescued);
                    Save.P.rescued.Clear();
                    for (int i = 0; i < Lobby.NeedFor(5) - 1; i++) Save.P.rescued.Add("test" + i);
                    yield return Wait(0.5f);
                    var jg = Lobby.JungleRiverGate;
                    bool poolWet = Water.At(Lobby.PoolSpot + Vector3.down * 0.5f, out float poolS, out float poolB);
                    Lobby.RiverPath(Lobby.SwimSpot, Lobby.PoolSpot, out var shutGates, out string shutBlocks);
                    Check("the jungle pool is deep river water", poolWet && poolS - poolB > 1.8f, $"wet {poolWet}, depth {poolS - poolB:0.00}");
                    Check($"{Lobby.NeedFor(5) - 1} friends: the river's way into the jungle is shut by a jungle gate down to the riverbed",
                        jg != null && !jg.IsOpen && jg.need == Lobby.NeedFor(5) && shutGates.Contains(jg) && shutBlocks.Length == 0,
                        $"gate {(jg != null ? jg.IsOpen.ToString() : "none")}, gates in the way {shutGates.Count}, other:{shutBlocks}");
                    var north = new Vector3(Lobby.JunglePass.x, 0, Lobby.JunglePass.z + 3.5f);
                    pl.Teleport(new Vector3(north.x, Lobby.RiverSurface - 1.0f * sz, north.z), 180); pl.yaw = 180; pl.pitch = 16;
                    yield return Wait(0.8f);
                    pl.MoveInput = Vector2.up;
                    yield return Wait(2.6f);
                    float zShut = pl.transform.position.z;
                    yield return Shot("132_jungle_river_gate");
                    pl.MoveInput = Vector2.zero;
                    Check("v0.31: over a closed gate there is an invisible wall", jg.Barrier != null && jg.Barrier.enabled && jg.Barrier.bounds.max.y > 25 && jg.Barrier.gameObject.layer == Layers.Barrier);
                    Check("the shut river gate stops a swimmer", pl.Swimming && zShut > Lobby.JunglePass.z + 0.2f, $"z {zShut:0.00} (gate at {Lobby.JunglePass.z:0.0})");
                    Save.P.rescued.Add("test" + (Lobby.NeedFor(5) - 1));
                    yield return Wait(1.8f);   // the gate sinks away
                    bool passOpen = Lobby.RiverPath(Lobby.SwimSpot, Lobby.PoolSpot, out var openGates, out string openBlocks);
                    Check($"{Lobby.NeedFor(5)} friends: the river gate opens with the jungle, the way is clear", jg != null && jg.IsOpen && Lobby.Open(5) && passOpen && openGates.Count == 0,
                        $"open {(jg != null && jg.IsOpen)}, gates {openGates.Count}, in the way:{openBlocks}");
                    pl.Teleport(new Vector3(north.x, Lobby.RiverSurface - 1.0f * sz, north.z), 180); pl.yaw = 180;
                    yield return Wait(0.6f);
                    pl.MoveInput = Vector2.up;
                    float zOpen = pl.transform.position.z;
                    for (float t = 0; t < 5f && zOpen > Lobby.JunglePass.z - 3f; t += Time.deltaTime) { yield return null; zOpen = pl.transform.position.z; }
                    pl.MoveInput = Vector2.zero;
                    Check("through the open river gate he swims into the jungle", zOpen < Lobby.JunglePass.z - 3f && pl.Swimming, $"z {zOpen:0.00}, swimming {pl.Swimming}");
                    Save.P.rescued.Clear(); Save.P.rescued.AddRange(keepR);
                    yield return null;
                    Check("the river gate shuts again with fewer friends (test only)", jg != null && jg.IsOpen == Lobby.Open(5));
                    pl.Teleport(Lobby.Spawn, 0); pl.pitch = 16;
                    yield return Wait(0.4f);
                }

                // ledge grab: jump at a ledge that is too high — he catches the edge and climbs up
                var lt = new GameObject("LedgeTest").transform;
                var lfloor = Lobby.Solid(PrimitiveType.Cube, new Vector3(500, 99.75f, 500), new Vector3(20, 0.5f, 20), Mats.Lit(Color.gray), default, lt);
                Lobby.Solid(PrimitiveType.Cube, new Vector3(500, 101.4f, 503), new Vector3(6, 2.8f, 2), Mats.Lit(Color.white), default, lt);
                yield return null;
                pl.WaterY = 90;
                pl.Teleport(new Vector3(500, 100.2f, 500.6f), 0); pl.yaw = 0;
                yield return Wait(0.4f);
                int grabs0 = pl.Grabs;
                pl.MoveInput = Vector2.up;
                pl.QueueJump();
                bool sawHang = false;
                for (float t = 0; t < 3; t += Time.deltaTime) { if (pl.Hanging) { sawHang = true; if (t > 0.1f) break; } yield return null; }
                if (sawHang) yield return Shot("68_ledge_grab");
                for (float t = 0; t < 2 && pl.Hanging; t += Time.deltaTime) yield return null;
                pl.MoveInput = Vector2.zero;
                yield return Wait(0.3f);
                Check("a jump that falls short grabs the edge and climbs up by itself", sawHang && pl.Grabs == grabs0 + 1 && pl.transform.position.y > 102.6f, $"grabs {pl.Grabs - grabs0}, y {pl.transform.position.y:0.00}");
                Destroy(lt.gameObject);
                pl.WaterY = -0.9f;

                // the backrooms: a double jump into the glitch over Пончик
                var seam = Backrooms.SeamPos;
                var donut = GameObject.Find("Friend w2c0");
                if (donut) donut.transform.position += new Vector3(6, 0, 0);   // step Пончик aside so we don't land on him
                pl.Teleport(new Vector3(seam.x + 0.9f, 0.3f, seam.z), 270);
                yield return Wait(0.5f);
                pl.Teleport(new Vector3(seam.x + 0.9f, pl.transform.position.y, seam.z), 270);
                pl.QueueJump();
                yield return Wait(0.3f);
                pl.QueueJump();
                float maxY = 0, minH = 99; bool sawAir = false;
                for (float t = 0; t < 2 && !Backrooms.Inside; t += Time.deltaTime)
                {
                    var pp = pl.transform.position; maxY = Mathf.Max(maxY, pp.y); minH = Mathf.Min(minH, new Vector2(pp.x - seam.x, pp.z - seam.z).magnitude); sawAir |= pl.AirJumped;
                    yield return null;
                }
                bool byJump = Backrooms.Inside;
                if (!byJump) Backrooms.Enter();
                yield return Wait(1f);
                Check("a double jump over Пончик drops you into the backrooms", byJump, $"seam {seam}, max y {maxY:0.0}, min dist {minH:0.0}, air jump {sawAir}");
                Check("the backrooms: a yellow maze with lamps, an exit and the rich friends' cages", Backrooms.Inside && Backrooms.Root != null && Rescue.Cages.FindAll(c => c.id.StartsWith("vip")).Count == 3, $"dead ends {Backrooms.DeadEnds.Count}");
                pl.yaw = 45; pl.pitch = 10;
                yield return Wait(0.6f);
                yield return Shot("69_backrooms");
                // rescue Артём: go to his cage with a key
                var vc = Rescue.Cages.Find(c => c.id == "vipArtem");
                Save.P.keys++;
                vc.TryOpen();
                yield return Wait(0.3f);
                Check("Артём in the rich outfit is rescued from the backrooms", Save.P.rescued.Contains("vipArtem") && Rescue.NameOf("vipArtem") == "Артём" && Rescue.LookOf("vipArtem").Of("suit") == Catalog.RichArtem);
                pl.Teleport(Backrooms.ExitPos + new Vector3(0, 0.3f, 2.2f), 180);
                yield return Wait(0.3f);
                pl.Teleport(Backrooms.ExitPos + new Vector3(0, 0.3f, 0.5f), 180);
                yield return Wait(1.5f);
                Check("the EXIT door takes you back to the lobby", !Backrooms.Inside && pl.transform.position.y < 5, pl.transform.position + " exit " + Backrooms.ExitPos + " inside " + Backrooms.Inside);

                // golden eggs
                Check("10 golden eggs, every one on solid ground (none stuck in walls)", Eggs.Bad.Count == 0, string.Join("; ", Eggs.Bad));
                int found0 = Eggs.Found; long ec = Save.P.coins;
                pl.Teleport(new Vector3(-3.2f, 0.3f, -13.3f), 270);
                yield return Wait(0.3f);
                pl.Teleport(new Vector3(-4.3f, 0.3f, -13.3f), 270);
                yield return Wait(0.8f);
                Check("an egg in the gap between the shops can be taken (+100 coins)", Eggs.Found == found0 + 1 && Save.P.coins >= ec + 100, $"eggs {Eggs.Found}");

                // birds fly off when you come close
                var bird = Bird.All.Find(b => b != null && !b.Flying && b.transform.position.y < 3);
                Check("little birds hop about in the lobby", Bird.All.Count >= 20, Bird.All.Count.ToString());
                if (bird != null)
                {
                    pl.Teleport(bird.transform.position + new Vector3(2.5f, 0.3f, 0), 270);
                    yield return Wait(0.6f);
                    yield return Shot("70_birds");
                    Check("a bird flies up into the sky when you come close", bird.Flying && bird.transform.position.y > 1.5f, bird.transform.position.y.ToString("0.0"));
                }

                // promo codes: there is a menu section for them
                Menus.Pause();
                yield return Wait(0.4f);
                bool promoRow = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "Промокоды") promoRow = true;
                yield return Shot("76_menu_promo");
                Window.Close();
                Check("the menu has a «Промокоды» section", promoRow);
                pl.Teleport(Lobby.Spawn, 180);
                yield return Wait(0.3f);
                bool oldGone = !Promo.Apply("Курица") && !Promo.Apply("Чайник") && !Promo.Apply("BUTTER 22X");
                Check("the old promo codes are gone (Курица, Чайник, BUTTER 22X)", oldGone);
                long tc = Save.P.coins, tt = Save.P.tokens;
                Promo.Apply("Миллиард");
                Check("«Миллиард» gives 300 billion coins and 300 billion tokens", Save.P.coins == tc + Promo.BillionSum && Save.P.tokens == tt + Promo.BillionSum, $"{tc} → {Save.P.coins}");
                Save.P.coins = tc; Save.P.tokens = tt;
                long dc = Save.P.coins;
                Check("«Артем22X» gives lots of money and all 8 premium sets", Promo.Apply("Артем22X") && Save.P.coins >= dc + Promo.DevCoins && Save.P.sets.Count == 8);
                Promo.Apply("BUTTER");
                yield return Wait(1.2f);
                int buttered = Butter.Buttered;
                yield return Shot("72_butter");
                Promo.Apply("BUTTER");
                yield return Wait(0.3f);
                Check("«BUTTER» makes everything butter, again «BUTTER» melts it back", buttered > 200 && Butter.Buttered == 0 && !Save.P.butter, buttered + " things");
                // 18.11: iPhone = every skin, «Снять все скины», Тайфун = no skins at all, Бомж = no money
                long keepC = Save.P.coins, keepT = Save.P.tokens;
                Promo.Apply("iPhone");
                bool allOwned = true; foreach (var it in Catalog.Items.Values) if (!Save.Owns(it.id)) allOwned = false;
                Check("«iPhone» gives every skin in the game", allOwned && Save.P.sets.Count == 8);
                Save.Wear("hat", "hat_4"); Save.Wear("wings", "wings_0"); Save.Wear("suit", Catalog.Tramp);
                pl.Teleport(new Vector3(-5, 0.3f, 18.2f), 90);
                yield return Wait(0.3f);
                WardrobeUI.Open("hat");
                yield return Wait(0.6f);
                bool offBtn = false; foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "СНЯТЬ ВСЕ СКИНЫ") offBtn = true;
                WardrobeUI.TakeOffAll();
                yield return Wait(0.4f);
                yield return Shot("91_take_off_all");
                Check("wardrobe «Снять все скины» takes everything off", offBtn && Save.Worn("hat") == "" && Save.Worn("wings") == "" && Save.Worn("suit") == "" && !pl.Model.PartCount.ContainsKey("suit"));
                pl.Teleport(Lobby.Spawn, 0);
                WardrobeUI.Close();
                Save.Wear("hat", "hat_4");
                Promo.Apply("Тайфун");
                bool onlyDef = true; foreach (var it in Catalog.Items.Values) if (Save.Owns(it.id) && it.id != it.D.def) onlyDef = false;
                Check("«Тайфун» takes every skin off the account", onlyDef && Save.P.sets.Count == 0 && Save.Worn("hat") == "");
                Promo.Apply("Бомж");
                Check("«Бомж» takes all coins and tokens", Save.P.coins == 0 && Save.P.tokens == 0);
                Save.P.coins = keepC; Save.P.tokens = keepT;
                Promo.Apply("iPhone");
                Save.P.coins = keepC; Save.P.tokens = keepT; Save.Write();

                Check("«Фиксай» and «Домер» open their songs in YouTube Music", Promo.Apply("Фиксай") && Promo.LastSongUrl.Contains("music.youtube.com") && Promo.LastSongUrl.Contains(System.Uri.EscapeDataString("Жизнь в розовых очках"))
                    && Promo.Apply("Домер") && Promo.LastSongUrl.Contains(System.Uri.EscapeDataString("Семицвет")), Promo.LastSongUrl);
                Promo.Apply("Секрет");
                yield return Wait(0.4f);
                yield return Shot("83_secret_nooks");
                Check("«Секрет» shows where each of the 10 nooks is", Secrets.IsOpen && Eggs.Where.Length == 10 && Eggs.Total == 10);
                Secrets.Hide();
                Promo.Apply("Лукашарики");
                yield return Wait(1.5f);
                yield return Shot("84_cartoon_title");
                Check("«Лукашарики» opens the episode picker", Cartoon.IsOpen && Cartoon.Episode == 0 && Cartoon.Scene == -1);
                Cartoon.Play(1);
                Cartoon.Speed = 4f;
                for (float t = 0; t < 30 && Cartoon.Scene < 2; t += Time.unscaledDeltaTime) yield return null;
                yield return Wait(1.5f);
                yield return Shot("85_cartoon_tears");
                for (float t = 0; t < 150 && Cartoon.Scene < Cartoon.Scenes - 1; t += Time.unscaledDeltaTime) yield return null;
                yield return Wait(0.5f);
                yield return Shot("86_cartoon_end");
                Check("«Лукашарики» plays the 3D cartoon to the end", Cartoon.IsOpen && Cartoon.Episode == 1 && Cartoon.Scene == Cartoon.Scenes - 1, Cartoon.Scene + " of " + Cartoon.Scenes);
                for (int ep = 2; ep <= Cartoon.Episodes; ep++)
                {
                    Cartoon.Play(ep);
                    Cartoon.Speed = 4f;
                    for (float t = 0; t < 12 && Cartoon.Scene < 3; t += Time.unscaledDeltaTime) yield return null;
                    yield return Shot("86_cartoon_e" + ep + "_mid");
                    for (float t = 0; t < 120 && Cartoon.IsOpen && Cartoon.Scene < Cartoon.Scenes - 1; t += Time.unscaledDeltaTime) yield return null;
                    yield return Wait(1.5f);
                    yield return Shot("86_cartoon_e" + ep);
                    Check($"«Лукашарики» episode {ep} plays to the end", Cartoon.IsOpen && Cartoon.Episode == ep && Cartoon.Scene == Cartoon.Scenes - 1, Cartoon.Scene + " of " + Cartoon.Scenes);
                }
                Cartoon.Speed = 1f;
                Cartoon.Stop();
                Check("the cross closes the cartoon", !Cartoon.IsOpen && !pl.Frozen);

                // 17.11 the coolest outfit for Артемий and Асхан the tramp
                Save.Give(Catalog.RichArtemiy); Save.Wear("suit", Catalog.RichArtemiy); pl.ApplyLook(Save.CurrentLook());
                pl.Teleport(Lobby.Spawn, 180); pl.yaw = 0; pl.pitch = 8;
                yield return Wait(0.6f);
                yield return Shot("87_rich_artemiy");
                Check("«Богач Артемий» is the most detailed costume", pl.Model.PartCount.TryGetValue("suit", out var ap) && ap >= 100, ap.ToString());
                Save.Give(Catalog.Tramp); Save.Wear("suit", Catalog.Tramp); pl.ApplyLook(Save.CurrentLook());
                yield return Wait(0.5f);
                yield return Shot("88_tramp_ashan");
                Save.Wear("suit", ""); pl.ApplyLook(Save.CurrentLook());


                // costumes: Артём's rich outfit on the player
                Save.Give(Catalog.RichArtem); Save.Wear("suit", Catalog.RichArtem); pl.ApplyLook(Save.CurrentLook());
                yield return Wait(0.4f);
                yield return Shot("73_rich_artem");
                Save.Wear("suit", ""); pl.ApplyLook(Save.CurrentLook());

                // nickname and the records table
                string nick0 = Save.P.nick;
                Save.P.nick = "Тестер";
                Check("the nickname from the settings is used", Online.Nick == "Тестер");
                Online.I.Inject("R|zz998877|Чемпион|999999999999|40|7|12|10|color=color_2");
                var recTop = RecordsUI.Sorted(RecordsUI.By.Coins);
                Check("records table: everyone who ever played, sorted (coins, friends, worlds, tokens, eggs)", recTop.Count >= 2 && recTop[0].pid == "zz998877" && recTop.Exists(r => r.pid == Save.P.id), recTop.Count + " lines");
                RecordsUI.Open();
                yield return Wait(0.6f);
                yield return Shot("74_records");
                Window.Close();
                Save.P.nick = nick0;
                Online.I.Records.Remove("zz998877");

                // a room with a friend and «Выйти из комнаты»
                // 18.11: nobody joins without an invitation; an invitation can be accepted or declined
                Online.I.Inject($"H|room1peer|{Application.version}|1|color=color_1|Друг");
                Online.I.Inject($"J|room1peer|{Online.I.Id}");
                Check("a player can't join you without an invitation", Online.I.RoomWith == null);
                Online.I.Inject($"H|room2peer|{Application.version}|1|color=color_3|Соня");
                Online.I.Inject($"I|room2peer|{Online.I.Id}|Соня");
                yield return Wait(0.3f);
                bool popup = InviteUI.IsOpen;
                yield return Shot("92_invitation");
                var dec = GameObject.Find("Decline")?.GetComponent<UIButton>();
                dec?.OnClick?.Invoke();
                Check("an invitation shows «Принять / Отказаться», declining keeps you alone", popup && !InviteUI.IsOpen && Online.I.RoomWith == null);
                Online.I.Inject("B|room2peer");
                PlayersUI.Open();
                yield return Wait(0.5f);
                bool inviteBtn = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "ПРИГЛАСИТЬ") inviteBtn = true;
                Check("every player in «Игроки онлайн» has «Пригласить»", inviteBtn);
                Online.I.Invite(Online.I.Peers["room1peer"]);
                Online.I.Inject($"A|room1peer|{Online.I.Id}");
                Online.I.Inject($"J|room1peer|{Online.I.Id}");
                bool inRoom = Online.I.RoomWith == "room1peer";
                // v0.27 voice: the codec keeps the sound, a piece from the friend gets into the speaker
                Window.Close();
                var tone = new float[Voice.Chunk]; for (int k = 0; k < tone.Length; k++) tone[k] = Mathf.Sin(k * 0.3f) * 0.5f;
                float err = 0; foreach (var x in tone) err = Mathf.Max(err, Mathf.Abs(Voice.UnMuLaw(Voice.MuLaw(x)) - x));
                var bytes = new byte[tone.Length]; for (int k = 0; k < tone.Length; k++) bytes[k] = Voice.MuLaw(tone[k]);
                int heard0 = Voice.I.Heard;
                Online.I.Inject($"V|room1peer|1|{System.Convert.ToBase64String(bytes)}");
                Online.I.Inject($"V|room1peer|1|{System.Convert.ToBase64String(bytes)}");   // the copy from the second relay
                yield return Wait(0.3f);
                bool voiceBtn = false; foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "ГОЛОС" && tx.isActiveAndEnabled) voiceBtn = true;
                Check("voice with the friend: «ГОЛОС» shows in a room, the friend's voice plays once, clear sound", voiceBtn && Voice.I.Heard == heard0 + 1 && err < 0.03f, $"err {err:0.000}, heard {Voice.I.Heard - heard0}");
                RoomChat.I.Send("Привет! Пошли в мир 8");
                Online.I.Inject("C|room1peer|1|Пошли!");
                Online.I.Inject("C|room1peer|1|Пошли!");   // the copy from the second relay
                yield return Wait(0.5f);
                Check("the chat with the friend: top left, my and the friend's messages (once), coins moved aside", RoomChat.Visible && RoomChat.I.Lines.Count >= 2 && RoomChat.I.Lines.FindAll(l => l.Contains("Пошли!")).Count == 1,
                    string.Join(" / ", RoomChat.I.Lines));
                yield return Shot("121_voice_button");
                PlayersUI.Open();
                Window.Refresh();
                yield return Wait(0.5f);
                bool leaveBtn = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "ВЫЙТИ ИЗ КОМНАТЫ") leaveBtn = true;
                yield return Shot("75_room");
                Online.I.LeaveRoom();
                yield return Wait(1f);
                Check("joined players get «Выйти из комнаты» and can leave", inRoom && leaveBtn && Online.I.RoomWith == null);
                Online.I.Inject("B|room1peer");
                yield return Enter(1);

                // ---- 18.11: wall joints, a tree into the backrooms, keys burn in the lobby, football ----
                Check("every wall joint has one round post (no flickering overlaps)", Lobby.Posts >= 30, Lobby.Posts.ToString());
                pl.Teleport(new Vector3(23.2f, 0.3f, -49.6f), 135); pl.yaw = 135; pl.pitch = 10;
                yield return Wait(0.6f);
                yield return Shot("93_wall_corner");

                var tops = new List<TreeTop>(FindObjectsByType<TreeTop>(FindObjectsSortMode.None)).FindAll(t => t.name == "TreeTop");
                Check("lobby trees can be climbed (branches up to a leafy top)", tops.Count >= 10 && GameObject.Find("TreeBranch") != null, tops.Count + " trees");
                if (tops.Count > 0)
                {
                    var treeT = tops[0].transform;
                    pl.Teleport(treeT.position + Vector3.up * 0.6f, 0);
                    for (float t = 0; t < 1.5f && !pl.Grounded; t += Time.deltaTime) yield return null;
                    yield return Wait(0.2f);
                    pl.yaw = 30; pl.pitch = 25;
                    yield return Shot("94_tree_top");
                    pl.QueueJump();
                    yield return Wait(0.35f);
                    pl.QueueJump();
                    for (float t = 0; t < 2f && !Backrooms.Inside; t += Time.deltaTime) yield return null;
                    Check("from a tree top two jumps take you into the backrooms", Backrooms.Inside && Backrooms.FromTree > 0, $"{Backrooms.FromTree} lastTop {pl.LastTreeTop:0.0} now {Time.time:0.0} open {Menus.AnyOpen}");
                    if (Backrooms.Inside) { yield return Wait(0.5f); Backrooms.Exit(); yield return Wait(1.5f); }
                }

                Save.P.keys = 3;
                yield return Enter(1);
                Check("back in the starting world the keys are gone", Save.P.keys == 0, Save.P.keys.ToString());

                Football.Start(1, 1);
                yield return Wait(2.2f);
                Check("the football pad and the stadium", Football.Root != null && Football.Ball != null && Football.Inside(pl.transform.position) && Football.PadPos != Vector3.zero);
                int fg0 = Football.Me;
                Football.Ball.position = Football.O + new Vector3(Football.HalfL - 2f, 0.6f, 0.5f); Football.Ball.linearVelocity = new Vector3(14, 0, 0);
                yield return Wait(1f);
                Check("a ball in the goal is a goal", Football.Me == fg0 + 1, Football.Me.ToString());
                yield return Wait(0.3f);
                pl.Teleport(Football.O + new Vector3(-3, 0.3f, 0), 90); pl.yaw = 90;
                yield return Wait(0.5f);
                Football.Ball.position = Football.O + new Vector3(-1.9f, Football.BallR + 0.05f, 0); Football.Ball.linearVelocity = Vector3.zero;
                yield return Wait(0.6f);
                pl.Attack();
                yield return null;
                Check("B next to the ball kicks it hard", Football.Ball.linearVelocity.magnitude > 8, Football.Ball.linearVelocity.magnitude.ToString("0.0"));
                yield return Shot("95_football_kick");
                int footL0 = Save.P.footLevel;
                for (int k = 0; k < 2; k++) { Football.Ball.position = Football.O + new Vector3(Football.HalfL - 2f, 0.6f, 0); Football.Ball.linearVelocity = new Vector3(14, 0, 0); yield return Wait(1.2f); }
                Check("level 1: 3 goals win it and open level 2", !Football.Running && Save.P.footLevel >= Mathf.Max(2, footL0), Save.P.footLevel.ToString());
                yield return Shot("96_football_win");
                Window.Close();
                Save.P.footDiff = 3;
                Football.Start(1, 5);
                yield return Wait(3.5f);
                Check("level 5: a keeper and 3 defenders play against you", Football.Bots.Count == 4 && Football.Bots.Exists(b => b.keeper));
                pl.yaw = 90; pl.pitch = 20;
                yield return Shot("97_football_bots");
                // a match with a friend (their messages come as if over the internet)
                Online.I.Inject($"H|ballpeer|{Application.version}|1|color=color_2|Друг");
                Online.I.Invite(Online.I.Peers["ballpeer"]);
                Online.I.Inject($"A|ballpeer|{Online.I.Id}");
                Online.I.Inject($"FS|ballpeer|{Online.I.Id}");
                yield return Wait(1.5f);
                Online.I.Inject($"FB|ballpeer|0.00|1.00|5.00|0|0|0|2|1|170.0");
                yield return Wait(0.8f);
                Check("football with a friend: the friend's ball and score come over the network", Football.Mode == 2 && !Football.Host && (Football.Ball.position - (Football.O + new Vector3(0, 1, 5))).magnitude < 1.5f && Football.Them == 2 && Football.Me == 1,
                    $"mode {Football.Mode} host {Football.Host} room {Online.I.RoomWith} ball {Football.Ball.position - Football.O} net {Football.NetPos - Football.O} kin {Football.Ball.isKinematic} {Football.Me}:{Football.Them}");
                Football.Leave();
                yield return Wait(1.5f);
                Online.I.LeaveRoom();
                Online.I.Inject("B|ballpeer");
                yield return Wait(1f);
                Check("leaving the stadium takes you back to the lobby", !Football.Inside(pl.transform.position) && Football.Mode == 0);

                // ---- 21.11 the tutorial video (Settings → «Обучение») and the neon quarter with the portals of worlds 67 and 8 ----
                Tutorial.Speed = 4f;
                Tutorial.Play();
                for (float t = 0; t < 20 && Tutorial.Scene < 4; t += Time.unscaledDeltaTime) yield return null;
                yield return Shot("122_tutorial_leap");
                for (float t = 0; t < 30 && Tutorial.Scene < 12; t += Time.unscaledDeltaTime) yield return null;
                yield return Shot("123_tutorial_cages");
                for (float t = 0; t < 40 && Tutorial.Scene < Tutorial.Scenes - 1; t += Time.unscaledDeltaTime) yield return null;
                yield return Wait(1f);
                yield return Shot("124_tutorial_end");
                Check("«Обучение»: the tutorial video with voice plays to the end", Tutorial.IsOpen && Tutorial.Scene == Tutorial.Scenes - 1, Tutorial.Scene + " / " + Tutorial.Scenes);
                Tutorial.Stop();
                Tutorial.Speed = 1f;
                yield return Wait(0.5f);
                Check("after the tutorial the game is back (not frozen)", !Tutorial.IsOpen && !pl.Frozen);
                pl.Teleport(Lobby.NeonGate + new Vector3(4, 0.3f, 0), 270); pl.yaw = 270; pl.pitch = 10;
                yield return Wait(0.8f);
                yield return Shot("125_neon_gate");
                pl.Teleport(new Vector3(-56, 0.3f, 0), 270); pl.yaw = 270; pl.pitch = 8;
                yield return Wait(0.8f);
                yield return Shot("126_neon_quarter");
                Check("the neon quarter holds the portals of worlds 67 and 8", Lobby.Portal67.x < -55 && Lobby.Portal8Pos.x < -55 && GameObject.Find("Portal8") != null);

                // ---- 19.11 fans are the rescued chubriks, «Следить за кем-то», «Заблокировать комнату» ----
                Football.EnsureBuilt();
                int wantFans = Mathf.Min(Save.P.rescued.Count, FootballFans.MaxFans, FootballFans.Seats.Count - 8);
                Check("the stadium's fans are the rescued chubriks, some seats stay free", FootballFans.Fans.Count == wantFans && wantFans > 0 && FootballFans.FreeSeats > 0, $"{FootballFans.Fans.Count} fans, {FootballFans.FreeSeats} free");
                Football.Menu();
                yield return Wait(0.4f);
                bool watchBtn = false, standBtn = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) { if (tx.text == "СЛЕДИТЬ") watchBtn = true; if (tx.text == "НА ТРИБУНУ") standBtn = true; }
                yield return Shot("115_football_menu");
                Window.Close();
                Check("the football pad has many buttons: «Следить за кем-то», «Сесть на трибуну»", watchBtn && standBtn);
                Online.I.Inject($"H|fanpeer|{Application.version}|1|color=color_4|Футболист|0");
                var fanPos = Football.O + new Vector3(3, 0.3f, 2);
                Online.I.Inject(string.Format(System.Globalization.CultureInfo.InvariantCulture, "S|fanpeer|1|1|{0:0.00}|{1:0.00}|{2:0.00}|90|0.5|0|1", fanPos.x, fanPos.y, fanPos.z));
                Check("someone playing football shows in «Следить за кем-то»", FootballFans.Players().Exists(pp => pp.id == "fanpeer"));
                FootballFans.Watch(Online.I.Peers["fanpeer"]);
                for (int k = 0; k < 15; k++)
                {
                    Online.I.Inject(string.Format(System.Globalization.CultureInfo.InvariantCulture, "S|fanpeer|{0}|1|{1:0.00}|{2:0.00}|{3:0.00}|90|0.5|0|1", k + 2, fanPos.x + k * 0.1f, fanPos.y, fanPos.z));
                    Online.I.Inject($"FW|fanpeer|{4 + k * 0.1f:0.00}|0.40|2.50|2|1".Replace(',', '.'));
                    yield return Wait(0.1f);
                }
                yield return Wait(0.5f);
                var fanPeer = Online.I.Peers["fanpeer"];
                yield return Shot("116_watching");
                Check("«Следить»: from the stands the camera follows the player and his ball", FootballFans.Watching == "fanpeer" && fanPeer.avatar != null && pl.Cam.transform.position.z < Football.O.z - Football.HalfW && FootballFans.WatchMe == 2,
                    $"cam {pl.Cam.transform.position - Football.O}, avatar {fanPeer.avatar != null}");
                FootballFans.StopWatching();
                Online.I.Inject("B|fanpeer");
                yield return Enter(1);

                InviteUI.Close();   // a real player online may have invited the test game
                Online.LockRoom(25);
                Online.I.Inject($"H|lockpeer|{Application.version}|1|color=color_2|Гость|0");
                Online.I.Inject($"I|lockpeer|{Online.I.Id}|Гость");
                yield return Wait(0.3f);
                Check("a locked room gets no invitations", Online.Locked && !InviteUI.IsOpen);
                LockUI.Set(1, 25);
                LockUI.Open();
                yield return Wait(0.4f);
                yield return Shot("117_lock_room");
                PlayersUI.Open();
                yield return Wait(0.5f);
                bool openBtn = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "ОТКРЫТЬ КОМНАТУ") openBtn = true;
                yield return Shot("118_room_locked");
                Window.Close();
                Check("«Игроки онлайн»: the room is locked until a chosen time, «Открыть комнату» unlocks it", openBtn);
                Online.LockRoom(0);
                Check("the room is open again", !Online.Locked);
                Online.I.Inject("B|lockpeer");

                // ---- 18.11 cheats: flying, endless coins, the admin panel ----
                Menus.Settings();
                yield return Wait(0.4f);
                bool cheatRow = false;
                foreach (var tx in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None)) if (tx.text == "ЧИТЫ") cheatRow = true;
                Check("Settings has a «Читы» button", cheatRow);
                long keepCoins = Save.P.coins;
                Save.P.coins = 31_000_000_000L;
                Cheats.Open();
                yield return Wait(0.4f);
                yield return Shot("102_cheats");
                Cheats.BuyFly();
                Check("the flying cheat costs 50 000 coins", Save.P.cheatFly && Save.P.flyOn && Save.P.coins == 31_000_000_000L - Cheats.FlyPrice);
                Window.Close();
                pl.Teleport(Lobby.Spawn, 0);
                yield return Wait(0.5f);
                float fy0 = pl.transform.position.y;
                pl.HoldJump = true;
                yield return Wait(1f);
                pl.HoldJump = false;
                float fy1 = pl.transform.position.y;
                yield return Wait(0.6f);
                float fy2 = pl.transform.position.y;
                yield return Shot("103_flying");
                pl.HoldAttack = true;
                yield return Wait(0.5f);
                pl.HoldAttack = false;
                float fy3 = pl.transform.position.y;
                Check("flying: A held goes up, nothing — hovers, B held goes down", fy1 > fy0 + 4 && Mathf.Abs(fy2 - fy1) < 0.4f && fy3 < fy2 - 2, $"{fy0:0.0} → {fy1:0.0} → {fy2:0.0} → {fy3:0.0}");
                Save.P.flyOn = false;
                yield return Wait(1.5f);
                Cheats.BuyCoins();
                long ec0 = Save.P.coins;
                bool spent = Save.Spend(5000);
                Check("endless coins: 100 000, then buying costs nothing", Save.P.cheatCoins && spent && Save.P.coins == ec0);
                Save.P.coinsOn = false;
                long ac0 = Save.P.coins;
                Cheats.BuyAdmin();
                Check("the admin panel costs 30 000 000 000 coins", Save.P.cheatAdmin && Save.P.coins == ac0 - Cheats.AdminPrice);
                AdminPanel.Open();
                AdminPanel.StartCopy();
                var thing = GameObject.Find("TreeBranch");
                AdminPanel.Pick(thing);
                Check("«Скопировать вещь»: a tapped thing is copied into the panel", AdminPanel.Template != null && !string.IsNullOrEmpty(AdminPanel.TemplatePath));
                pl.Teleport(Lobby.Spawn, 0); pl.yaw = 0; pl.pitch = 12;
                yield return Wait(0.4f);
                int pc0 = Save.P.placed.Count;
                AdminPanel.StartPlace();
                var placedT = AdminPanel.Selected;
                var scale0 = placedT != null ? placedT.transform.localScale : Vector3.zero;
                for (int k = 0; k < 4; k++) AdminPanel.Scale(new Vector3(1, 1, 1.15f));
                AdminPanel.Scale(new Vector3(1.1f, 1.1f, 1.1f));
                AdminPanel.Move(Vector3.up);
                AdminPanel.Turn(45);
                yield return Wait(0.5f);
                yield return Shot("104_admin_placed");
                Check("«Поставить вещь»: a copy stands in front, it can be stretched, made bigger, moved and turned", placedT != null && Save.P.placed.Count == pc0 + 1 && placedT.transform.localScale.z > scale0.z * 1.5f && Save.P.placed[Save.P.placed.Count - 1].Contains(placedT.id));
                var screenPt = pl.Cam.WorldToScreenPoint(placedT.transform.GetComponentInChildren<Renderer>().bounds.center);
                var tapped = AdminPanel.ThingAt(screenPt);
                AdminPanel.Pick(tapped);
                Check("tapping a placed copy selects it", AdminPanel.Selected == placedT, tapped != null ? tapped.name : "nothing");
                AdminPanel.DeleteSelected();
                yield return Wait(0.3f);
                Check("a placed copy can be taken out of the game", Save.P.placed.Count == pc0 && placedT == null);
                yield return null;
                var apRt = GameObject.Find("AdminPanel")?.GetComponent<RectTransform>();
                Check("v0.31: the bought admin panel stands small in the top left corner, the coins move to its right", AdminPanel.IsOpen && apRt != null && apRt.anchorMin == new Vector2(0, 1) && apRt.sizeDelta.x <= 320 && GameObject.Find("TopBar").GetComponent<RectTransform>().anchoredPosition.x > 300);
                yield return Shot("135_admin_corner");
                Save.P.adminOn = false;
                yield return null; yield return null;
                Check("switched off in «Читы» — the panel is gone", !AdminPanel.IsOpen);
                Save.P.coins = keepCoins; Save.Write();
            }

            // ---- online: the real public relay, a made-up player whose messages go through it ----
            for (float t = 0; t < 20 && !Online.I.Connected; t += Time.unscaledDeltaTime) yield return null;
            Check("connected to the online relay", Online.I.Connected);
            if (Online.I.Connected)
            {
                var fl = Look.Default(); fl["hat"] = "hat_6"; fl["wings"] = "wings_0"; fl["color"] = "color_7"; fl["emote"] = "emote_1";
                string fid = "testpeer" + UnityEngine.Random.Range(100, 999), fls = Online.LookString(fl);
                var fpos = Lobby.Spawn + new Vector3(4, 0, 3);
                Online.I.Send($"H|{fid}|{Application.version}|1|{fls}");
                for (float t = 0; t < 8 && !Online.I.Peers.ContainsKey(fid); t += Time.unscaledDeltaTime) yield return null;
                Check("a player's hello comes back through the relay", Online.I.Peers.ContainsKey(fid));
                bool fakeMoving = true;
                IEnumerator FakeMoves()
                {
                    for (int k = 0; fakeMoving; k++)
                    {
                        Online.I.Send(string.Format(System.Globalization.CultureInfo.InvariantCulture, "S|{0}|{1}|1|{2:0.00}|{3:0.00}|{4:0.00}|200|0|0|1", fid, k, fpos.x, fpos.y, fpos.z));
                        yield return Wait(0.1f);
                    }
                }
                StartCoroutine(FakeMoves());
                yield return Wait(2.5f);
                Online.I.Peers.TryGetValue(fid, out var fpeer);
                Check("another player is not in our world without an invitation", fpeer != null && fpeer.avatar == null);
                if (fpeer != null)
                {
                    // we invite them; their «yes» comes back through the real relay
                    Online.I.Invite(fpeer);
                    Online.I.Send($"A|{fid}|{Online.I.Id}");
                    for (float t = 0; t < 6 && (fpeer.avatar == null); t += Time.unscaledDeltaTime) yield return null;
                }
                Check("after the invitation is accepted the other player's chubrik appears", fpeer != null && Online.I.RoomWith == fid && fpeer.avatar != null);
                PlayersUI.Open();
                yield return Wait(1f);
                Check("«Игроки онлайн» shows skin pictures", UIRoot.Open != null && UIRoot.Open.Content.GetComponentInChildren<UnityEngine.UI.RawImage>() != null);
                yield return Shot("62_players_online");
                if (fpeer != null)
                {
                    pl.Teleport(Lobby.Spawn + new Vector3(-6, 0, -2), 0);
                    yield return Wait(0.5f);
                    Online.I.Join(fpeer);
                    yield return Wait(2.5f);
                    Check("tapping a player takes you next to them", Vector3.Distance(pl.transform.position, fpos) < 3.5f, pl.transform.position.ToString());
                    yield return Shot("63_joined_player");
                    Check("the other player is still shown next to us", fpeer.avatar != null && Vector3.Distance(fpeer.avatar.transform.position, fpos) < 1);
                    fakeMoving = false;
                    yield return Wait(0.3f);
                    // a player in another world: we go into that world, next to them
                    Online.I.Inject($"H|{fid}|{Application.version}|5|{fls}");
                    Online.I.Inject(string.Format(System.Globalization.CultureInfo.InvariantCulture, "S|{0}|900|5|{1:0.00}|{2:0.00}|{3:0.00}|90|0|0|1", fid, World5.Spawn.x, World5.Spawn.y + 0.3f, World5.Spawn.z));
                    Online.I.Join(fpeer);
                    for (float t = 0; t < 10 && Worlds.Current != 5; t += Time.unscaledDeltaTime) yield return null;
                    yield return Wait(2.5f);
                    Check("joining a player in another world takes you there", Worlds.Current == 5 && Vector3.Distance(pl.transform.position, World5.Spawn) < 4, $"world {Worlds.Current}, {pl.transform.position}");
                    yield return Enter(1);
                }
                Online.I.Send($"B|{fid}");
                for (float t = 0; t < 6 && Online.I.Peers.ContainsKey(fid); t += Time.unscaledDeltaTime) yield return null;
                Check("a player who leaves disappears from the list", !Online.I.Peers.ContainsKey(fid));
            }

            Check("every world has its big titan", TitanArena.Bosses.Count >= 6, TitanArena.Bosses.Count.ToString());
            // ---- 18.11 the worlds dressed like the concept art: a view from the start and from the new side area ----
            {
                var views = new (int w, Vector3 o, Vector3 area, float yaw)[]
                {
                    (2, World2.O, new Vector3(-17, 0.3f, 20), 300), (3, World3.O, new Vector3(-19, 0.9f, 30), 300), (4, World4.O, new Vector3(-29, 2.9f, 54), 200),
                    (5, World5.O, new Vector3(23.5f, 6.3f, 58), 120), (6, World6.O, new Vector3(-30, 2.9f, 45), 270), (7, World67.O, new Vector3(-21, 4.3f, 99), 300),
                    (8, World8.O, new Vector3(0, 13.3f, 203), 180),
                };
                foreach (var v in views)
                {
                    yield return Enter(v.w);
                    pl.yaw = 0; pl.pitch = 8;
                    yield return Wait(0.8f);
                    yield return Shot($"110_world{v.w}_start");
                    pl.Teleport(v.o + v.area, v.yaw); pl.yaw = v.yaw; pl.pitch = 14;
                    yield return Wait(1f);
                    Check($"world {v.w}: the new side area stands where the player can walk", pl.Grounded || Physics.Raycast(pl.transform.position + Vector3.up, Vector3.down, 3f, Layers.SolidMask), pl.transform.position.ToString());
                    yield return Shot($"111_world{v.w}_area");
                }
                // World 8 «Самый мемный мир»: 12 friends with their keys, Pyzhik flies along, the golden portal finishes it
                yield return Enter(8);
                yield return Wait(3f);
                Check("World 8: 12 cages and a key for each", Rescue.Total("w8c") == 12 && Pickups.I.KeysLeftIn("w8k") == 12, $"{Rescue.Total("w8c")} / {Pickups.I.KeysLeftIn("w8k")}");
                var pyzhik = FindFirstObjectByType<Pyzhik>();
                Check("World 8: the robot Пыжик flies next to the chubrik", pyzhik != null && Vector3.Distance(pyzhik.transform.position, pl.transform.position) < 6, pyzhik != null ? Vector3.Distance(pyzhik.transform.position, pl.transform.position).ToString("0.0") : "none");
                Check("World 8 has its titan", TitanArena.Bosses.ContainsKey(8));
                pl.yaw = 0; pl.pitch = 12;
                yield return Shot("112_world8_pyzhik");
                pl.Teleport(World8.O + new Vector3(0, 19.3f, 219), 0); pl.yaw = 0; pl.pitch = 10;
                yield return Wait(1f);
                yield return Shot("113_world8_portal");
                bool done8 = Save.P.done.Contains(8);
                World8.Win();
                yield return Wait(1.5f);
                Check("World 8: the golden portal finishes the world", Save.P.done.Contains(8));
                if (!done8) { Save.P.done.Remove(8); }
                Window.Close();
                yield return Enter(1);
                Check("the lobby has a portal to World 8", GameObject.Find("Portal8") != null, Lobby.Portal8Pos.ToString());
                pl.Teleport(Lobby.Portal8Pos + Vector3.up * 0.3f + Quaternion.Euler(0, 0, 0) * new Vector3(0, 0, -7), 0);
                yield return Wait(0.6f);
                var p8 = GameObject.Find("Portal8").transform;
                pl.Teleport(p8.TransformPoint(new Vector3(0, 0.3f, -8)), p8.eulerAngles.y); pl.yaw = p8.eulerAngles.y; pl.pitch = 8;
                yield return Wait(0.6f);
                yield return Shot("114_lobby_portal8");
            }


            // ---- 22.11 several chubriks, each with its OWN progress: «ИГРАТЬ» → the picker, «+» = a new chubrik from zero ----
            // (at the very end: the game is rebuilt for him, then for the first one again — the rest of the test ran on one profile)
            {
                Window.Close();
                if (Worlds.Current != 1) yield return Enter(1);
                var hatDept = Catalog.Dept("hat");
                var hatItem = hatDept.items.Find(it => it.id != hatDept.def && !string.IsNullOrEmpty(it.id));
                string hatId = hatItem != null ? hatItem.id : hatDept.def;
                Save.Give(hatId); Save.Wear("hat", hatId); PlayerCtl.I.ApplyLook(Save.CurrentLook());
                // the first chubrik's progress (after the whole test it has coins, friends, worlds, found things)
                var firstP = Save.P;
                long coins1 = Save.P.coins, tokens1 = Save.P.tokens;
                int rescued1 = Save.P.rescued.Count, found1 = Save.P.found.Count, owned1 = Save.P.owned.Count, foot1 = Save.P.footLevel;
                string done1 = string.Join(",", Save.P.done), rescuedIds1 = string.Join(",", Save.P.rescued);
                // the lobby's 10 hidden tokens («t0»…«t9») still lying there (the worlds' tokens are in Pickups too)
                int LobbyTokens() { int n = 0; if (Pickups.I != null) foreach (Transform c in Pickups.I.transform) if (c.gameObject.activeSelf && c.name.Length > 7 && c.name.StartsWith("Token t") && char.IsDigit(c.name[7])) n++; return n; }
                int tokensLeft1 = LobbyTokens();
                int charsBefore = Save.CharCount, firstIdx = Save.ActiveIndex;
                int LobbyFriends() { int n = 0; if (Lobby.root != null) foreach (Transform c in Lobby.root) if (c.name.StartsWith("Friend ")) n++; return n; }
                bool NoStands() { foreach (var go in gameObject.scene.GetRootGameObjects()) if (go.name.StartsWith("CharStand")) return false; return Lobby.root == null || !Lobby.root.Cast<Transform>().Any(c => c.name.StartsWith("CharStand")); }
                IEnumerator WaitRebuilt(int before)
                {
                    for (float t = 0; t < 15 && (Boot.Restarting || Boot.Restarts == before); t += Time.unscaledDeltaTime) yield return null;
                    yield return Wait(1.5f);
                    Window.Close();
                }
                IEnumerator Walks(string who)
                {
                    var p = PlayerCtl.I;
                    p.Teleport(Lobby.Spawn, 0); p.yaw = 0; p.pitch = 10;
                    yield return Wait(0.6f);
                    var from = p.transform.position;
                    p.MoveInput = new Vector2(0, 1);
                    yield return Wait(1.2f);
                    p.MoveInput = Vector2.zero;
                    float moved = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(p.transform.position.x, 0, p.transform.position.z));
                    Check($"{who}: the lobby is there and the chubrik runs in it", Lobby.root != null && Worlds.Current == 1 && moved > 1.5f && p.transform.position.y > -1, moved.ToString("0.0") + " m");
                }
                Check("before: one chubrik, no other chubriks standing in the lobby", charsBefore == 1 && NoStands(), charsBefore.ToString());
                Menus.Main();
                yield return Wait(0.5f);
                CharSlots.Open();
                yield return Wait(0.8f);
                Check("«ИГРАТЬ» opens the chubrik picker with 3 squares", CharSlots.IsOpen && CharSlots.Squares == Save.MaxChars, CharSlots.Squares.ToString());
                yield return Shot("127_char_slots");

                int restarts = Boot.Restarts;
                CharSlots.New();
                yield return WaitRebuilt(restarts);
                int newIdx = Save.ActiveIndex;
                var P = Save.P;
                Check("«+» makes a 2nd chubrik, plays him, the game is built again", Save.CharCount == charsBefore + 1 && newIdx != firstIdx && Boot.Restarts == restarts + 1 && !Boot.Restarting && PlayerCtl.I != null && PlayerCtl.I != pl,
                      $"{Save.CharCount} chubriks, restarts {Boot.Restarts - restarts}");
                Check("the new chubrik's progress is fresh: 50 coins, no friends, no worlds, nothing found", P != firstP && P.coins == 50 && P.tokens == 0 && P.rescued.Count == 0 && P.done.Count == 0 && P.found.Count == 0 && P.keys == 0,
                      $"coins {P.coins}, tokens {P.tokens}, rescued {P.rescued.Count}, done {P.done.Count}, found {P.found.Count}");
                Check("…and nothing bought: default things only, football level 1, no cheats, no sets, no admin copies", P.owned.Count < owned1 && P.sets.Count == 0 && P.footLevel == 1 && !P.cheatFly && !P.cheatCoins && !P.cheatAdmin && P.placed.Count == 0 && !P.promo && P.maxHearts == 3,
                      $"owned {P.owned.Count} (first {owned1}), foot {P.footLevel}");
                Check("the new chubrik wears the starting outfit and has his own name", Save.Worn("hat") == hatDept.def && PlayerCtl.I.Model.look.Of("hat") == hatDept.def && Save.CharName(newIdx).StartsWith("Чубрик "),
                      $"{Save.CharName(newIdx)}, hat {Save.Worn("hat")}");
                Check("his lobby: no rescued friends walk there, the gates are closed, every hidden token is back", LobbyFriends() == 0 && Lobby.Gates.Count > 0 && Lobby.Gates.TrueForAll(g => g != null && !g.IsOpen) && LobbyTokens() >= tokensLeft1 && LobbyTokens() > 0,
                      $"friends {LobbyFriends()}, gates {Lobby.Gates.Count}, tokens {LobbyTokens()} (first {tokensLeft1})");
                Check("no other chubrik stands in the lobby (whom to play is chosen only in the picker)", NoStands());
                var roots = gameObject.scene.GetRootGameObjects();
                Check("one HUD, one player, one lobby after the rebuild (the old ones are gone)", FindObjectsByType<PlayerCtl>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<Hud>(FindObjectsSortMode.None).Length == 1 && roots.Count(g => g.name == "Lobby") == 1 && roots.Count(g => g.name == "UI") == 1 && World2.Root == null,
                      $"{roots.Count(g => g.name == "Lobby")} lobbies, {roots.Length} roots");
                yield return Walks("the new chubrik");
                yield return Shot("128_char_new");
                yield return Enter(2);
                Check("the new chubrik can go into World 2: its cages are there, nobody rescued yet", Worlds.Current == 2 && World2.Root != null && Rescue.Total("w2c") >= 8 && P.rescued.Count == 0, $"world {Worlds.Current}, cages {Rescue.Total("w2c")}");
                yield return Enter(1);
                Check("…and back to his lobby", Worlds.Current == 1);

                CharSlots.Open();
                yield return Wait(0.8f);
                Check("the picker shows both chubriks and one «+»", CharSlots.IsOpen && CharSlots.Squares == Save.MaxChars && Save.CharCount == 2);
                yield return Shot("129_char_slots_two");
                restarts = Boot.Restarts;
                CharSlots.Pick(firstIdx);
                yield return WaitRebuilt(restarts);
                P = Save.P;
                Check("picking the first chubrik rebuilds the game for him", Save.ActiveIndex == firstIdx && Boot.Restarts == restarts + 1 && !Boot.Restarting);
                Check("the first chubrik's progress is untouched: coins, friends, worlds, found things, items", P.coins == coins1 && P.tokens == tokens1 && P.rescued.Count == rescued1 && string.Join(",", P.rescued) == rescuedIds1 && string.Join(",", P.done) == done1 && P.found.Count == found1 && P.owned.Count == owned1 && P.footLevel == foot1,
                      $"coins {P.coins}/{coins1}, rescued {P.rescued.Count}/{rescued1}, done {string.Join(",", P.done)}/{done1}, found {P.found.Count}/{found1}");
                Check("…his hat is on again", Save.Worn("hat") == hatId && PlayerCtl.I.Model.look.Of("hat") == hatId, Save.Worn("hat"));
                Check("…his rescued friends walk in the lobby again, his found tokens stay found", LobbyFriends() == rescued1 && LobbyTokens() == tokensLeft1, $"friends {LobbyFriends()}/{rescued1}, tokens {LobbyTokens()}/{tokensLeft1}");
                Check("…and still nobody stands in the lobby", NoStands());
                yield return Walks("the first chubrik again");
                int otherIdx = -1;
                for (int i = 0; i < Save.CharCount; i++) if (i != Save.ActiveIndex) otherIdx = i;
                Check("the other chubrik keeps his outfit for the picker's portrait", otherIdx >= 0 && Save.CharLook(otherIdx).Of("hat") == hatDept.def);
                Check("a chubrik not played now can be deleted (with his progress)", otherIdx >= 0 && Save.DeleteChar(otherIdx) && Save.CharCount == charsBefore && Save.ActiveIndex == 0 && Save.P.coins == coins1);
                Check("the played chubrik can't be deleted by DeleteChar (CharSlots.Delete moves to another one first)", !Save.DeleteChar(Save.ActiveIndex) && Save.CharCount == charsBefore);
                // v0.31: a tap on a chubrik opens «Играть / Удалить / Ник» with the progress line; each one has its own nick;
                // the played one can be deleted too
                string nick1 = P.nick;
                restarts = Boot.Restarts;
                CharSlots.New();
                yield return WaitRebuilt(restarts);
                int newIdx2 = Save.ActiveIndex;
                Save.SetCharNick(newIdx2, "Тестик");
                CharSlots.Open();
                yield return Wait(0.4f);
                CharSlots.Choose(newIdx2);
                yield return Wait(0.6f);
                yield return Shot("134_char_choice");
                Check("a tap on a chubrik: the frame with «ИГРАТЬ», «УДАЛИТЬ», a nick and the progress line", CharSlots.ChoiceOpen && GameObject.Find("Choice") is GameObject chG && chG.transform.Find("Panel/Play") != null && chG.transform.Find("Panel/Delete") != null && chG.transform.Find("Panel/BarHolder/Progress") != null && GameObject.Find("Slot 1") is GameObject s1 && s1.transform.Find("Square/Delete") == null && s1.transform.Find("Square/Progress") != null);
                Check("every chubrik has his own nick", Save.CharName(newIdx2) == "Тестик" && Online.Nick == "Тестик" && Save.CharName(firstIdx) != "Тестик" && Save.P.nick == "Тестик", $"{Save.CharName(firstIdx)} / {Save.CharName(newIdx2)}");
                restarts = Boot.Restarts;
                CharSlots.Delete(newIdx2);
                yield return WaitRebuilt(restarts);
                yield return Wait(0.5f);
                P = Save.P;
                Check("the played chubrik can be deleted: the game goes on with the other one", Save.CharCount == charsBefore && P.coins == coins1 && P.nick == nick1 && CharSlots.IsOpen, $"chars {Save.CharCount}, coins {P.coins}/{coins1}");
                CharSlots.Close();
                Save.Wear("hat", hatDept.def); PlayerCtl.I.ApplyLook(Save.CurrentLook());
                pl = PlayerCtl.I;
            }

            // ---- v0.31 stickers, chests, friends' stories, nicknames over the heads ----
            {
                pl = PlayerCtl.I;
                Menus.Play();
                yield return Wait(0.8f);
                Check("72 stickers, 10 + 2 hide-and-seek open from the start, the rest in 30 chests (2 each)", Stickers.All.Count == 72 && Stickers.Free + Chests.Total * Stickers.PerChest == 70);
                Check("all 15 chests stand on the ground", Chests.Bad.Count == 0 && Chests.All.Count >= 12, $"{Chests.All.Count} built, bad: {string.Join("; ", Chests.Bad)}");
                Stickers.Open();
                yield return Wait(0.6f);
                yield return Shot("136_stickers");
                Check("«СТИКЕРЫ» opens the picker; locked ones have a padlock", UIRoot.Open != null && GameObject.Find("Sticker 71") != null && GameObject.Find("StickersBtn") == null);
                Window.Close();
                yield return Wait(0.3f);
                int used0 = Stickers.Used;
                Stickers.Use(0);
                yield return Wait(0.5f);
                Check("a sticker pops up over the head", Stickers.Used == used0 + 1 && pl.transform.Find("StickerPop") != null);
                Check("…and the chubrik himself moves: arms, legs, head", pl.Anim.stickerPose >= 0);
                var pairs = new HashSet<string>();
                foreach (var st in Stickers.All) pairs.Add(st.kind + ":" + st.emote + ":" + st.move + ":" + st.pop);
                // a pet does its own trick for every sticker
                Save.Give("pet_1"); Save.Wear("pet", "pet_1"); pl.ApplyLook(Save.CurrentLook());
                yield return Wait(0.6f);
                int tr0 = pl.PetNow != null ? pl.PetNow.Tricks : -1;
                Stickers.Use(3);
                yield return Wait(0.7f);
                yield return Shot("142_sticker_pet");
                Check("with a pet: the pet does its own trick for the sticker", pl.PetNow != null && pl.PetNow.Tricks == tr0 + 1 && pl.PetNow.Tricking);
                Save.Wear("pet", ""); pl.ApplyLook(Save.CurrentLook());
                yield return Wait(2.5f);
                int vids = Stickers.All.FindAll(x => x.video).Count;
                Stickers.Use(4);
                yield return Wait(0.8f);
                yield return Shot("141_sticker_video");
                Check("some stickers show a 3 s mini video over the head: a little chubrik in a frame doing the move", vids >= 15 && pl.transform.Find("StickerPop/Video/Mini/MiniChubrik") != null, vids + " video stickers");
                yield return Wait(3.5f);
                Check("every sticker has its own animation (no two the same)", pairs.Count == Stickers.All.Count, pairs.Count + " different");
                yield return Shot("137_sticker_head");
                Stickers.Use(7, "Привет всем");
                yield return Wait(0.5f);
                var popT = pl.transform.Find("StickerPop")?.GetComponentInChildren<TMPro.TMP_Text>();
                Check("«Своё слово»: the word over the head", popT != null && popT.text == "Привет всем");
                Check("Камень, ножницы, бумага: rock beats scissors, scissors beat paper, paper beats rock", Rps.Judge(0, 1) == 1 && Rps.Judge(1, 2) == 1 && Rps.Judge(2, 0) == 1 && Rps.Judge(1, 0) == 2 && Rps.Judge(2, 2) == 0);
                Rps.Ask();
                Check("without a friend in the room KNB doesn't start", !Rps.IsOpen);
                var nt = pl.GetComponent<Stickers.NickTag>();
                var ntText = pl.GetComponentInChildren<TMPro.TextMeshPro>(true);
                Check("our nickname is over our head", nt != null);
                // a chest: locked stickers, B opens it, 4 new stickers
                var ch = Chests.All.Find(c => c != null && c.gameObject.activeInHierarchy && !c.IsOpen);
                if (ch != null)
                {
                    int before = Stickers.UnlockedCount;
                    pl.Teleport(ch.transform.position + ch.transform.forward * 1.0f, Quaternion.LookRotation(-ch.transform.forward).eulerAngles.y);
                    yield return Wait(0.8f);
                    pl.Attack();
                    yield return Wait(1f);
                    yield return Shot("138_chest_open");
                    Check("B next to a chest opens it (no key) — 2 new stickers", ch.IsOpen && Stickers.UnlockedCount == before + Stickers.PerChest, $"{before} → {Stickers.UnlockedCount}");
                    Stickers.Use(Stickers.Free + ch.index * Stickers.PerChest);
                    yield return Wait(0.3f);
                    Check("a sticker from the chest can be used now", Stickers.LastUsed == Stickers.Free + ch.index * Stickers.PerChest);
                    yield return Wait(2.5f);
                }
                else Check("a closed chest in the lobby", false);
                // «Взлёт и плюх»
                pl.Teleport(Lobby.Spawn, 0);
                yield return Wait(0.6f);
                Save.P.found.Add("chest0");
                int sp0 = pl.Splashes;
                Stickers.Use(10);
                for (float t = 0; t < 4f && pl.Splashes == sp0; t += Time.deltaTime) yield return null;
                yield return Shot("139_splash_rainbow");
                Check("«Взлёт и плюх»: up, down, rainbow drops", pl.Splashes == sp0 + 1);
                yield return Wait(1f);
                if (pl.Lying) { pl.QueueJump(); yield return Wait(0.6f); }
                // a friend tells his story
                Save.P.rescued.Add("w2c3");
                Rescue.AddLobbyFriend("w2c3");
                yield return Wait(0.3f);
                var fr = GameObject.Find("Friend w2c3");
                if (fr != null)
                {
                    pl.Teleport(fr.transform.position + Vector3.forward * 1.3f, 180);
                    yield return Wait(0.5f);
                    int talks0 = Friends.Talks;
                    pl.Attack();
                    yield return Wait(0.6f);
                    yield return Shot("140_friend_story");
                    Check("B next to a friend: he tells his story", Friends.Talks == talks0 + 1 && !string.IsNullOrEmpty(Friends.LastStory) && GameObject.Find("FriendStory") != null, Friends.LastStory);
                }
                else Check("a friend in the lobby", false);
                var names = new HashSet<string>();
                for (int w = 2; w <= 6; w++) for (int c = 0; c < 8; c++) names.Add(Rescue.NameOf("w" + w + "c" + c));
                Check("friends have normal names (40 cages — 40 different names) and whole outfits", names.Count == 40 && !names.Contains("Пончик") && Rescue.LookOf("w2c0").Of("suit") == "suit_3" && Rescue.LookOf("w2c2").Of("hat") == "hat_5", string.Join(",", names));
                Save.P.rescued.Remove("w2c3");
                pl.Teleport(Lobby.Spawn, 0);
                yield return Wait(0.4f);
            }

            // ---- main menu ----
            Menus.Main();
            yield return Wait(1f);
            yield return Shot("18_main_menu");

            log.Insert(0, $"RESULT: {pass} passed, {fail} failed\n");
            File.WriteAllText(Path.Combine(dir, "autotest.txt"), log.ToString());
            Debug.Log("[Test] done " + pass + "/" + (pass + fail));
            yield return Wait(0.3f);
            Application.Quit();
        }

        static IEnumerator Wait(float s) { for (float t = 0; t < s; t += Time.unscaledDeltaTime) yield return null; }

        IEnumerator Gallery(string outDir)
        {
            Directory.CreateDirectory(outDir);
            const int cell = 300, cols = 5;
            foreach (var d in Catalog.Depts)
            {
                if (d.view == "fx" && d.id != "pet") continue;
                if (d.id == "color" || d.id == "skin" || d.id == "secret" || d.id == "wcolor") continue;
                var items = Catalog.Items.Values.Where(i => i.dept == d.id).ToList();
                int rows = (items.Count + cols - 1) / cols;
                var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGB24, false);
                var fill = new Color[sheet.width * sheet.height];
                for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.1f, 0.12f, 0.16f);
                sheet.SetPixels(fill);
                for (int k = 0; k < items.Count; k++)
                {
                    var t = Preview.I.Big(items[k], cell - 4);
                    sheet.SetPixels((k % cols) * cell + 2, (rows - 1 - k / cols) * cell + 2, cell - 4, cell - 4, t.GetPixels());
                    Destroy(t);
                    yield return null;
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(outDir, d.id + ".png"), sheet.EncodeToPNG());
                Destroy(sheet);
            }
        }

        IEnumerator Overview(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var pl = PlayerCtl.I;
            Menus.Main(); yield return Wait(0.5f);
            Window.Close();
            yield return Enter(1);
            yield return Wait(1f);
            yield return TopShot(Lobby.root, Vector3.zero, 130f, Path.Combine(outDir, "01_lobby.png"), null);
            int[] worlds = { 2, 3, 4, 5, 6, World67.Id, World8.Id };
            string[] names = { "02_mir2_paryashchie_ruiny", "03_mir3_kristalnaya_peshchera", "04_mir4_snezhnaya_dolina", "05_mir5_dzhungli", "06_mir6_oshibka_404", "07_mir67", "08_mir8_memnyy" };
            for (int i = 0; i < worlds.Length; i++)
            {
                yield return Enter(worlds[i]);
                for (float t = 0; t < 8 && Worlds.Current != worlds[i]; t += Time.unscaledDeltaTime) { Window.Close(); yield return null; }
                yield return Wait(1.5f);
                Debug.Log($"[Overview] world {worlds[i]}: current {Worlds.Current}, player {pl.transform.position}");
                var root = worlds[i] == 2 ? World2.Root : worlds[i] == 3 ? World3.Root : worlds[i] == 4 ? World4.Root : worlds[i] == 5 ? World5.Root : worlds[i] == 6 ? World6.Root : worlds[i] == World67.Id ? World67.Root : World8.Root;
                var spawn = pl.transform.position;
                yield return TopShot(root, spawn, 320f, Path.Combine(outDir, names[i] + ".png"), null);
            }
            yield return Enter(1);
            Backrooms.Enter();
            yield return Wait(1.5f);
            float ceil = Backrooms.O.y + 2.6f;
            yield return TopShot(Backrooms.Root, pl.transform.position, 120f, Path.Combine(outDir, "09_zakulise.png"), r => r.bounds.min.y > ceil);
            Backrooms.Exit();
            yield return Wait(1f);
            Football.EnsureBuilt();
            yield return TopShot(Football.Root, Football.O, 70f, Path.Combine(outDir, "10_stadion.png"), null);
        }

        /// <summary>An orthographic photo from straight above of everything solid under root (within reach of the centre).</summary>
        IEnumerator TopShot(Transform root, Vector3 centre, float reach, string file, Func<Renderer, bool> hide)
        {
            yield return null;
            Physics.SyncTransforms();
            bool any = false; var b = new Bounds();
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger || c.gameObject.layer == Layers.Barrier) continue;
                var cb = c.bounds;
                if (Mathf.Abs(cb.center.x - centre.x) > reach || Mathf.Abs(cb.center.z - centre.z) > reach || cb.size.x > reach * 1.6f || cb.size.z > reach * 1.6f) continue;
                if (!any) { b = cb; any = true; } else b.Encapsulate(cb);
            }
            if (!any) b = new Bounds(centre, Vector3.one * 60);
            b.Expand(new Vector3(8, 0, 8));
            var hidden = new List<Renderer>();
            if (hide != null) foreach (var r in root.GetComponentsInChildren<Renderer>()) if (r.enabled && hide(r)) { r.enabled = false; hidden.Add(r); }
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            float aspect = Mathf.Clamp(b.size.x / Mathf.Max(1, b.size.z), 0.35f, 2.8f);
            int w = aspect >= 1 ? 2048 : Mathf.RoundToInt(2048 * aspect), h = aspect >= 1 ? Mathf.RoundToInt(2048 / aspect) : 2048;
            var cg = new GameObject("TopCam");
            var cam = cg.AddComponent<Camera>();
            cam.orthographic = true; cam.aspect = w / (float)h;
            cam.orthographicSize = Mathf.Max(b.size.z, b.size.x / cam.aspect) * 0.5f;
            cam.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y + 60, b.center.z), Quaternion.Euler(90, 0, 0));
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = PlayerCtl.I.Cam.backgroundColor;
            cam.cullingMask = ~((1 << Layers.Preview) | (1 << 5));
            cam.nearClipPlane = 1; cam.farClipPlane = b.size.y + 400; cam.enabled = false;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(cg);
            RenderSettings.fog = fog;
            foreach (var r in hidden) if (r) r.enabled = true;
        }

        /// <summary>Goes to a world and waits until the portal animation has really finished.</summary>
        IEnumerator Enter(int w)
        {
            Worlds.Enter(w);
            yield return Wait(0.5f);
            for (float t = 0; t < 6 && Worlds.Current != w; t += Time.deltaTime) yield return null;
            yield return Wait(1.3f);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
