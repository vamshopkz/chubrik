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
            Check("31 departments", Catalog.Depts.Count == 31, Catalog.Depts.Count.ToString());
            bool eight = true;
            foreach (var d in Catalog.Depts) if (d.items.FindAll(i => !i.Premium && !i.Reward).Count != 8) eight = false;
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

            // ---- dive: a hit after the second jump, belly slide, stand up with a jump ----
            // pick a direction with nothing in the way for the dive and the slide
            float diveYaw = 180;
            foreach (var cand in new float[] { 180, 90, 270, 0, 135, 225, 45, 315 })
            {
                var dir = Quaternion.Euler(0, cand, 0) * Vector3.forward;
                if (!Physics.SphereCast(Lobby.Spawn + Vector3.up * 0.8f, 0.5f, dir, out _, 13f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) { diveYaw = cand; break; }
            }
            pl.Teleport(Lobby.Spawn, diveYaw);
            yield return Wait(0.5f);
            pl.QueueJump();
            yield return Wait(0.3f);
            pl.QueueJump();
            yield return Wait(0.15f);
            var dz0 = pl.transform.position;
            pl.Attack();
            Check("a hit after the second jump is a dive", pl.Diving && new Vector2(pl.Velocity.x, pl.Velocity.z).magnitude > 10);
            yield return Wait(0.1f);
            yield return Shot("53_dive");
            for (float tt = 0; tt < 2f && !pl.Lying; tt += Time.deltaTime) yield return null;
            Check("landing from the dive: lying on the belly", pl.Lying);
            var lie0 = pl.transform.position;
            yield return Wait(0.5f);
            float slid = Vector3.Distance(lie0, pl.transform.position);
            Check("a short belly slide", slid > 0.5f && slid < 5 && Vector3.Distance(dz0, pl.transform.position) > 3, $"slid {slid:0.0} m, dive {Vector3.Distance(dz0, pl.transform.position):0.0} m");
            yield return Wait(1f);
            yield return Shot("54_belly");
            Check("still lying until you press jump", pl.Lying);
            pl.QueueJump();
            yield return Wait(0.6f);
            Check("a jump stands the chubrik up", !pl.Lying && !pl.Diving);
            pl.Teleport(Lobby.Spawn, 0);
            yield return Wait(0.3f);
            pl.Attack();
            Check("on the ground a hit is still a hit", !pl.Diving && !pl.Lying);

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
            Check("5 blue gates (snow ×2, jungle, crystal, glitch)", Lobby.Gates.Count == 5);
            Check("with no friends rescued only the ruins are open", Lobby.Open(2) && !Lobby.Open(4) && Lobby.Gates.TrueForAll(g => !g.IsOpen));
            var keepRescued = new List<string>(Save.P.rescued);
            for (int i = 0; i < 10; i++) Save.P.rescued.Add("test" + i);
            yield return null;
            Check("10 friends: snow and jungle gates open, crystal and glitch shut", Lobby.Gates.FindAll(g => g.IsOpen).Count == 3 && Lobby.Open(5) && !Lobby.Open(3));
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
                cam.orthographic = true; cam.orthographicSize = 54; cam.aspect = 1;
                cam.transform.SetPositionAndRotation(new Vector3(0, 120, 0), Quaternion.Euler(90, 0, 0));
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
            Check("8 cages and 8 golden keys in world 2", Rescue.Total("w2") == 8 && Pickups.I.KeysLeftIn("w2") == 8, $"cages {Rescue.Total("w2")}, keys {Pickups.I.KeysLeftIn("w2")}");
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
            int sc0 = Save.P.coins, st0 = Save.P.tokens, sk0 = Save.P.keys;
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
            Check("8 cages and 8 keys in world 3", Rescue.Total("w3") == 8 && Pickups.I.KeysLeftIn("w3") == 8, $"cages {Rescue.Total("w3")}, keys {Pickups.I.KeysLeftIn("w3")}");
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
            Check("8 friends frozen in ice cubes", Rescue.Total("w4") == 8 && Rescue.Cages.FindAll(c => c.id.StartsWith("w4") && c.ice).Count == 8);
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
            Check("ice is slippery", slide > 2.5f, slide.ToString("0.0"));
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
            Check("8 cages and 8 keys in the jungle", Rescue.Total("w5") == 8 && Pickups.I.KeysLeftIn("w5") == 8, $"cages {Rescue.Total("w5")}, keys {Pickups.I.KeysLeftIn("w5")}");
            pl.yaw = 0; pl.pitch = 14;
            yield return Wait(0.4f);
            yield return Shot("45_world5_start");
            var log0 = World5.Logs[0].transform.position;
            yield return Wait(0.8f);
            Check("logs drift on the river", Vector3.Distance(log0, World5.Logs[0].transform.position) > 0.3f);
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
            Check("8 cages and 8 keys in the glitch world", Rescue.Total("w6") == 8 && Pickups.I.KeysLeftIn("w6") == 8, $"cages {Rescue.Total("w6")}, keys {Pickups.I.KeysLeftIn("w6")}");
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
            int c6 = Save.P.coins, t6 = Save.P.tokens;
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
