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
            if (Boot.Arg("-icons") != null)
            {
                // only render the app icon
                yield return IconMaker.Make(Boot.Arg("-icons"));
                Application.Quit();
                yield break;
            }
            var pl = PlayerCtl.I;

            // ---- catalogue ----
            Check("31 departments", Catalog.Depts.Count == 31, Catalog.Depts.Count.ToString());
            bool eight = true;
            foreach (var d in Catalog.Depts) if (d.items.FindAll(i => !i.Premium).Count != 8) eight = false;
            Check("8 named items in every department", eight);
            Check("200+ items in total", Catalog.Items.Count >= 200, Catalog.Items.Count + " (regular " + Catalog.RegularCount + ")");
            Check("8 premium sets priced 1000–3000 ₸", Catalog.Sets.Count == 8 && Catalog.Sets.TrueForAll(s => s.price >= 1000 && s.price <= 3000 && s.items.Count >= 3));

            // ---- every item builds ----
            var test = ChubrikModel.Create("TestModel", null, Look.Default(), Layers.Preview);
            test.transform.position = new Vector3(200, -300, 0);
            int bad = 0; var badList = new List<string>();
            var few = new List<string>();
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
                        Pet.Build(it.Premium ? 100 + it.set : Catalog.Index(it.id), t);
                        parts = Parts.Made - made;
                        Destroy(t.gameObject);
                    }
                    int verts = 0;
                    foreach (var mf in test.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh) verts += mf.sharedMesh.vertexCount;
                    if (verts > maxVerts) { maxVerts = verts; maxVertsId = it.id; }
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

            // ---- bounce mushroom ----
            pl.Teleport(new Vector3(-6, 2.6f, -18), 0);
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

            // ---- teleport pad ----
            pl.Teleport(new Vector3(-5, 0.2f, -7), 0);
            yield return Wait(2.2f);
            Check("teleport pad", Vector3.Distance(pl.transform.position, new Vector3(0, 6.3f, -30)) < 3, pl.transform.position.ToString());
            yield return Shot("05_course_platform");
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
            int coinsBefore = Save.P.coins, tokensBefore = Save.P.tokens;
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
            Check("set bonus is paid only once", Save.P.coins == coinsBefore && Save.P.tokens == tokensBefore, $"coins {coinsBefore} → {Save.P.coins}");

            // ---- wardrobe ----
            WardrobeUI.Open("hat");
            yield return Wait(0.5f);
            yield return Shot("10_wardrobe_hats");
            WardrobeUI.SelectDept("wings");
            yield return Wait(0.5f);
            yield return Shot("11_wardrobe_wings");
            WardrobeUI.SelectDept("pet");
            yield return Wait(0.5f);
            yield return Shot("12_wardrobe_pets");
            WardrobeUI.Close();

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
            pl.Teleport(new Vector3(-11, 0.2f, -8.4f), 180);
            pl.MoveInput = new Vector2(0, 1);
            yield return Wait(0.7f);
            pl.MoveInput = Vector2.zero;
            Check("cactus bumps back", Vector3.Distance(pl.transform.position, new Vector3(-11, 0, -10)) > 1.2f);

            // ---- world 2: floating ruins ----
            Check("promo gives 6 hearts", Save.P.maxHearts == 6);
            Worlds.Enter(2);
            yield return Wait(2f);
            Check("world 2 entered", Worlds.Current == 2 && Vector3.Distance(pl.transform.position, World2.Spawn) < 3, pl.transform.position.ToString());
            Check("world 2 has easy mobs", World2.Enemies.Count >= 8, World2.Enemies.Count.ToString());
            Check("hearts full in world 2", pl.Hearts == 6);
            yield return Wait(0.2f);
            Check("coins in world 2", Pickups.I.CoinsWithin(World2.O, 300) >= 30, Pickups.I.CoinsWithin(World2.O, 300).ToString());
            Check("coin navigator points somewhere", Pickups.I.NearestCoin.HasValue);
            var coinPos = Pickups.I.CoinNear(pl.transform.position).Value;
            int c0 = Save.P.coins;
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
            int cs0 = Save.P.coins;
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

            // ---- keys and cages ----
            Check("4 cages and 4 golden keys in world 2", Rescue.Total("w2") == 4 && Pickups.I.KeysLeft == 4, $"cages {Rescue.Total("w2")}, keys {Pickups.I.KeysLeft}");
            var cage = Rescue.Cages.Find(c => c.id == "w2c0");
            pl.Teleport(cage.transform.position + cage.transform.forward * 1.7f + Vector3.up * 0.1f, cage.transform.eulerAngles.y + 180);
            yield return Wait(0.6f);
            pl.yaw = cage.transform.eulerAngles.y + 180; pl.pitch = 12;
            yield return Wait(0.3f);
            yield return Shot("24_cage_closed");
            pl.Attack();
            Check("a cage stays shut without a key", !cage.Open);
            pl.Teleport(World2.O + new Vector3(10, 3.5f, 40), 0);
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
            yield return Wait(2.5f);

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
            Worlds.Enter(1);
            yield return Wait(1.6f);
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
            Worlds.Enter(3);
            yield return Wait(2f);
            Check("world 3 entered", Worlds.Current == 3 && Vector3.Distance(pl.transform.position, World3.Spawn) < 3, pl.transform.position.ToString());
            Check("cave light (no sky)", RenderSettings.skybox == null);
            Check("world 3 has cave mobs", World3.Enemies.Count >= 6, World3.Enemies.Count.ToString());
            Check("30 rare crystals", Pickups.I.CrystalsLeft + World3.Crystals() == World3.CrystalsTotal, (Pickups.I.CrystalsLeft + World3.Crystals()).ToString());
            Check("4 cages and 4 keys in world 3", Rescue.Total("w3") == 4 && Pickups.I.KeysLeftIn("w3") == 4, $"cages {Rescue.Total("w3")}, keys {Pickups.I.KeysLeftIn("w3")}");
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
            Worlds.Enter(1);
            yield return Wait(1.6f);
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

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
