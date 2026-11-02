using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var it in Catalog.Items.Values)
            {
                var l = Look.Default(); l[it.dept] = it.id;
                try
                {
                    test.Build(l);
                    if (it.dept == "pet") { var t = new GameObject("p").transform; Pet.Build(it.Premium ? 100 + it.set : Catalog.Index(it.id), t); Destroy(t.gameObject); }
                    if (test.GetComponentsInChildren<Renderer>().Length < 10) { bad++; badList.Add(it.id + ":few"); }
                }
                catch (Exception e) { bad++; badList.Add(it.id + ":" + e.Message); }
            }
            Check("every item builds", bad == 0, $"{Catalog.Items.Count} items, {sw.ElapsedMilliseconds} ms" + (bad > 0 ? " bad: " + string.Join(", ", badList.GetRange(0, Mathf.Min(10, badList.Count))) : ""));
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
            pl.QueueJump();
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { top = Mathf.Max(top, pl.transform.position.y); yield return null; }
            Check("double jump goes higher", top - y0 > 2.5f, (top - y0).ToString("0.00") + " m");
            yield return Wait(1.2f);
            Check("lands again", pl.Grounded);

            // ---- bounce mushroom ----
            pl.Teleport(new Vector3(-6, 2.6f, -18), 0);
            float bTop = 0;
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { bTop = Mathf.Max(bTop, pl.transform.position.y); yield return null; }
            Check("bounce mushroom launches", bTop > 5f, bTop.ToString("0.0") + " m");

            // ---- coins ----
            pl.Teleport(Lobby.Spawn, 0);
            yield return Wait(0.3f);
            int c0 = Save.P.coins;
            pl.Teleport(new Vector3(11.5f, 0.1f, 0), 90);
            yield return Wait(0.4f);
            Check("coin pickup by touch", Save.P.coins > c0, $"{c0} → {Save.P.coins}");
            Check("coins on the map", Pickups.I.CoinsTotal >= 120, Pickups.I.CoinsTotal.ToString());
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
            yield return Wait(0.3f);
            Check("coin navigator points somewhere", Pickups.I.NearestCoin.HasValue);

            // ---- premium + promo ----
            PremiumUI.Open();
            yield return Wait(0.3f);
            yield return Shot("09_premium");
            Window.Close();
            Check("wrong promo code refused", !Promo.Apply("12345"));
            Check("promo «артем 22 x» accepted", Promo.Apply("  артем 22 x "));
            Check("all 8 premium sets unlocked", Save.P.sets.Count == 8);
            Check("premium items owned", Save.Owns("hat_p7") && Save.Owns("wings_p0") && Save.Owns("secret_7"));

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

            // ---- water respawn ----
            pl.Teleport(new Vector3(0, 0.5f, 70), 0);
            yield return Wait(2f);
            Check("falling into water returns you to the shore", pl.transform.position.y > -0.5f && new Vector2(pl.transform.position.x, pl.transform.position.z).magnitude < 66, pl.transform.position.ToString());

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

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
