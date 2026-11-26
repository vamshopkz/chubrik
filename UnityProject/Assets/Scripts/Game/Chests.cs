using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// v0.31: 30 treasure chests hidden in the game — one by every golden egg (the 10 best nooks: the shops' gap, the
    /// premium roof, the water cave, three dead ends in the backrooms, World 2's grove, World 5's cave, behind World 67's
    /// fake walls) and five more in the lobby's hidden token spots (high over the mushroom, the wardrobe corner…).
    /// No key needed: stand next to one and press B. Each chest gives 2 stickers (<see cref="Stickers"/>).
    /// </summary>
    public static class Chests
    {
        public const int Total = 30;
        public static readonly List<Chest> All = new List<Chest>();
        /// <summary>Spots where a chest found no ground (the autotest wants none).</summary>
        public static readonly List<string> Bad = new List<string>();
        internal static void ResetStatics() { All.Clear(); Bad.Clear(); }

        public static bool Opened(int i) => Save.P.found.Contains("chest" + i);
        public static int OpenedCount { get { int n = 0; for (int i = 0; i < Total; i++) if (Opened(i)) n++; return n; } }

        /// <summary>The egg «eggN» has chest N right next to it.</summary>
        public static void ByEgg(string eggId, Vector3 eggPos)
        {
            if (!int.TryParse(eggId.Substring(3), out int n)) return;
            // 26.11: the premium-roof chest fell inside the closed shop — it now sits on the stone in the pond
            if (n == 1) { SpawnAt(n, new Vector3(-46.2f, 0.7f, 14.8f), Vector3.forward); return; }
            Spawn(n, eggPos);
        }

        /// <summary>v0.31 (later): 20 more by hidden tokens — the lobby's ten (chests 10..19) and ten in the worlds (20..29).</summary>
        static readonly string[] TokenChests = { "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9",
                                                 "w4t3", "w4t4", "w4t5", "w5t0", "w5t1", "w5t2", "w5t3", "w5t4", "w7t0", "w7t1" };

        public static void ByToken(string tokenId, Vector3 pos)
        {
            int k = System.Array.IndexOf(TokenChests, tokenId);
            if (k < 0) return;
            // 26.11: the chest high over the mushroom ended up in the invisible wall — it now stands on a bridge
            if ((pos - (Lobby.TestMushroom + Vector3.up * 6.2f)).sqrMagnitude < 1f && OnBridge(out var bp, out var bf)) { SpawnAt(10 + k, bp, bf); return; }
            Spawn(10 + k, pos);
        }

        /// <summary>The middle of the south bridge's deck (spawn → the jungle gate), facing along it.</summary>
        static bool OnBridge(out Vector3 p, out Vector3 fwd)
        {
            p = default; fwd = Vector3.forward; float bd = 1e9f; Transform best = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != "BridgeDeck") continue;
                float d = (t.position - new Vector3(9.6f, 0, -15f)).sqrMagnitude;
                if (d < bd) { bd = d; best = t; }
            }
            if (best == null) return false;
            fwd = best.right;   // the deck's local z goes across the river: the chest faces the walkers
            p = best.position + best.right * 0.55f + Vector3.up * (best.lossyScale.y / 2);
            Physics.SyncTransforms();
            if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var h, 3f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) p = h.point;
            return true;
        }

        static void SpawnAt(int i, Vector3 at, Vector3 face)
        {
            if (All.Exists(c => c && c.index == i)) return;
            var go = new GameObject("Chest " + i);
            go.transform.SetParent(Pickups.I != null ? Pickups.I.transform : null, false);
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face));
            var c = go.AddComponent<Chest>();
            c.index = i;
            c.Build();
            All.Add(c);
        }

        static void Spawn(int i, Vector3 near)
        {
            if (All.Exists(c => c && c.index == i)) return;
            Physics.SyncTransforms();
            // a free place on the ground next to the spot (the egg / the token stays where it is)
            Vector3 best = default; bool ok = false;
            foreach (float r in new[] { 0.9f, 1.3f, 0.6f, 1.8f })
            {
                for (int k = 0; k < 8 && !ok; k++)
                {
                    var p = near + Quaternion.Euler(0, k * 45 + 20, 0) * Vector3.forward * r;
                    if (!Physics.Raycast(p + Vector3.up * 0.9f, Vector3.down, out var hit, 3.5f, Layers.SolidMask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.8f) continue;
                    if (Mathf.Abs(hit.point.y - near.y) > 1.6f) continue;
                    if (Physics.CheckBox(hit.point + Vector3.up * 0.4f, new Vector3(0.5f, 0.3f, 0.34f), Quaternion.identity, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    if (Water.At(hit.point + Vector3.up * 0.1f, out _, out _)) continue;
                    best = hit.point; ok = true;
                }
                if (ok) break;
            }
            if (!ok)
            {
                if (Physics.Raycast(near + Vector3.up * 0.6f, Vector3.down, out var h2, 40f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) best = h2.point;
                else { Bad.Add("chest" + i + " " + near); best = near; }
            }
            var go = new GameObject("Chest " + i);
            go.transform.SetParent(Pickups.I != null ? Pickups.I.transform : null, false);
            var face = near - best; face.y = 0;
            go.transform.SetPositionAndRotation(best, Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? -face : Vector3.forward));
            var c = go.AddComponent<Chest>();
            c.index = i;
            c.Build();
            All.Add(c);
        }

        /// <summary>26.11 promo «Чубрик»: every chest opens at once — all the stickers are yours. Returns how many were new.</summary>
        public static int OpenAll()
        {
            int n = 0;
            for (int i = 0; i < Total; i++) if (!Opened(i)) { Save.P.found.Add("chest" + i); n++; }
            foreach (var c in All) if (c) c.ShowOpen();
            Save.Write();
            return n;
        }

        /// <summary>B next to a closed chest opens it.</summary>
        public static bool OpenNear(Vector3 p)
        {
            Chest best = null; float bd = 1.9f;
            foreach (var c in All)
            {
                if (c == null || !c.gameObject.activeInHierarchy || c.IsOpen) continue;
                float d = Vector3.Distance(c.transform.position, p);
                if (d < bd) { bd = d; best = c; }
            }
            if (best == null) return false;
            best.Open();
            return true;
        }
    }

    public class Chest : MonoBehaviour
    {
        public int index;
        public bool IsOpen => Chests.Opened(index);
        Transform lid;
        float openT = -1;

        /// <summary>v0.31: a real treasure chest — wooden planks, a round lid, gold corners and bands, a big lock with a
        /// keyhole, iron handles on the sides, coins and glowing stickers inside (seen when it is open).</summary>
        public void Build()
        {
            Parts.Layer = Layers.World;
            var wood = M("8B4A1F", 0.25f); var plank = M("6B3614", 0.3f); var gold = M("FFC83A", 0.9f, 0.85f);
            var iron = M("3A3F48", 0.6f, 0.6f); var inside = M("2A1406", 0.2f);
            const float W = 1.0f, D = 0.62f, H = 0.5f;
            // the box: planks with dark seams, a dark inside
            B(transform, new Vector3(0, H / 2, 0), new Vector3(W, H, D), wood);
            for (int k = 1; k < 4; k++) B(transform, new Vector3(0, k * H / 4, 0), new Vector3(W + 0.01f, 0.018f, D + 0.01f), plank);
            B(transform, new Vector3(0, H - 0.02f, 0), new Vector3(W - 0.08f, 0.02f, D - 0.08f), inside);
            // gold bands and corners
            foreach (var x in new[] { -0.36f, 0.36f }) B(transform, new Vector3(x, H / 2, 0), new Vector3(0.08f, H + 0.02f, D + 0.03f), gold);
            foreach (var x in new[] { -1f, 1f })
                foreach (var z in new[] { -1f, 1f })
                    B(transform, new Vector3(x * (W / 2), H / 2, z * (D / 2)), new Vector3(0.07f, H + 0.03f, 0.07f), gold);
            B(transform, new Vector3(0, 0.02f, 0), new Vector3(W + 0.04f, 0.05f, D + 0.04f), gold);
            // iron handles on the sides
            foreach (var x in new[] { -1f, 1f }) Ring(transform, new Vector3(x * (W / 2 + 0.03f), H * 0.6f, 0), 0.09f, 0.022f, iron, new Vector3(0, 0, 90));
            // treasure inside: coins and two glowing stickers
            for (int k = 0; k < 7; k++) Cy(transform, new Vector3(-0.3f + k * 0.1f, H - 0.01f + (k % 2) * 0.03f, (k % 3 - 1) * 0.12f), new Vector3(0.12f, 0.012f, 0.12f), gold, new Vector3(k * 9, 0, k * 13));
            foreach (var x in new[] { -0.16f, 0.16f }) Cy(transform, new Vector3(x, H + 0.08f, 0), new Vector3(0.2f, 0.01f, 0.2f), G(x < 0 ? "FF80AB" : "80DEEA", 1.6f), new Vector3(70, 0, x * 60));
            // the round lid on its hinge at the back
            lid = Node("Lid", transform, new Vector3(0, H, D / 2));
            var top = Node("Top", lid, new Vector3(0, 0, -D / 2));
            Cy(top, Vector3.zero, new Vector3(D, W / 2, D * 0.75f), wood, new Vector3(0, 0, 90));
            for (int k = -2; k <= 2; k++) Cy(top, new Vector3(k * 0.12f, 0, 0), new Vector3(D + 0.012f, 0.008f, D * 0.75f + 0.012f), plank, new Vector3(0, 0, 90));
            foreach (var x in new[] { -0.36f, 0.36f, -W / 2, W / 2 }) Cy(top, new Vector3(x, 0, 0), new Vector3(D + 0.03f, 0.035f, D * 0.75f + 0.03f), gold, new Vector3(0, 0, 90));
            // the lock: a gold plate with a keyhole on the front
            var lk = Node("Lock", top, new Vector3(0, -0.02f, -D / 2 - 0.02f));
            B(lk, Vector3.zero, new Vector3(0.16f, 0.2f, 0.04f), gold);
            Cy(lk, new Vector3(0, 0.03f, -0.025f), new Vector3(0.045f, 0.005f, 0.045f), iron, new Vector3(90, 0, 0));
            B(lk, new Vector3(0, -0.02f, -0.025f), new Vector3(0.02f, 0.06f, 0.01f), iron);
            // a soft glow so a sharp eye spots it
            S(transform, new Vector3(0, 0.4f, 0), 1.4f, Add("FFE58A", 0.06f));
            if (IsOpen) lid.localRotation = Quaternion.Euler(110, 0, 0);
        }

        public void ShowOpen() { if (lid && openT < 0 && lid.localRotation.eulerAngles.x < 1) openT = 0; }

        public void Open()
        {
            if (IsOpen) return;
            Save.P.found.Add("chest" + index);
            openT = 0;
            AudioFx.Play("token"); AudioFx.Play("magic");
            Effects.Emit("confetti", transform.position + Vector3.up * 0.7f, 40, 0.6f, Vector3.up * 3);
            Effects.Emit("gold", transform.position + Vector3.up * 0.6f, 24, 0.5f, Vector3.up * 2);
            var got = Stickers.FromChest(index);
            var names = new List<string>();
            foreach (var s in got) names.Add("«" + Stickers.All[s].name + "»");
            Hud.Toast($"Сундук {Chests.OpenedCount} из {Chests.Total}! Новые стикеры: {string.Join(", ", names)}", UIKit.Gold);
            Save.Write();
        }

        void Update()
        {
            if (openT < 0) return;
            openT += Time.deltaTime;
            lid.localRotation = Quaternion.Euler(110 * Mathf.SmoothStep(0, 1, Mathf.Clamp01(openT / 0.6f)), 0, 0);
            if (openT > 0.6f) openT = -1;
        }
    }
}
