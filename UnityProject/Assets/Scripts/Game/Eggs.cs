using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// 17.11: 10 golden eggs — the best hidden things in the game: tiny, tucked away in gaps, on roofs, in secret nooks
    /// and deep in the backrooms. Each gives 100 coins and a token; all ten give the «Золотая курица» costume.
    /// </summary>
    public static class Eggs
    {
        public const int Total = 10;
        public const string Prize = "suit_r0";
        /// <summary>Spots where no ground was found under an egg (the autotest wants 0).</summary>
        public static readonly List<string> Bad = new List<string>();
        static readonly HashSet<int> spawned = new HashSet<int>();

        public static int Found { get { int n = 0; foreach (var f in Save.P.found) if (f.StartsWith("egg")) n++; return n; } }

        public static void SpawnLobby()
        {
            spawned.Add(1);
            Put("egg0", new Vector3(-4.6f, 0.6f, -13.3f));       // in the narrow gap between the red and the blue shop
            Put("egg1", new Vector3(-8.6f, 5.2f, -19.6f));       // on the roof of the premium shop (double jump + grab the edge)
            Put("egg2", new Vector3(35.8f, 0.8f, 41.2f));        // in the far corner of the water cave
        }

        public static void SpawnBackrooms()
        {
            var ends = Backrooms.DeadEnds;
            for (int k = 0; k < 3; k++)
                if (k < ends.Count) Put("egg" + (3 + k), ends[k] + new Vector3(0, 0.8f, 0));
                else Bad.Add("egg" + (3 + k) + ": no dead end");
        }

        /// <summary>The worlds' eggs appear when the world is built.</summary>
        public static void SpawnWorld(int w)
        {
            if (!spawned.Add(w)) return;
            switch (w)
            {
                case 2: Put("egg6", World2.O + new Vector3(-20.5f, 11.4f, 82.6f)); break;                // the secret grove on the side path
                case 5: Put("egg7", World5.CaveC + new Vector3(0.6f, 1.2f, -2.2f)); break;               // at the back of the jungle cave
                case 7:
                    Put("egg8", World67.O + new Vector3(-12.6f, 3.8f, 47.2f));                           // behind the first fake wall
                    Put("egg9", World67.O + new Vector3(12.6f, 7.6f, 150.6f));                           // behind the second fake wall
                    break;
            }
        }

        static void Put(string id, Vector3 pos)
        {
            Physics.SyncTransforms();
            if (Physics.Raycast(pos + Vector3.up * 0.8f, Vector3.down, out var hit, 4f, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                pos = hit.point + Vector3.up * 0.32f;
            else Bad.Add(id + " " + pos);
            if (Physics.CheckSphere(pos, 0.15f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) Bad.Add(id + " inside something " + pos);
            Pickups.I.Egg(id, pos);
        }

        /// <summary>Called when an egg is picked up.</summary>
        public static void OnFound()
        {
            int n = Found;
            Save.P.coins += 100; Save.P.tokens += 1;
            if (n >= Total && !Save.P.owned.Contains(Prize))
            {
                Save.Give(Prize);
                Hud.Toast("ВСЕ 10 ЗОЛОТЫХ ЯИЦ! Твой приз — костюм «Золотая курица» в гардеробе (отдел «Костюмы»)", UIKit.Gold);
            }
            else Hud.Toast($"Золотое яйцо! +100 монет и жетон · найдено {n} из {Total}", UIKit.Gold);
            Save.Write();
        }

        /// <summary>A small golden egg with a shine.</summary>
        public static void Model(Transform t)
        {
            var gold = Parts.M("FFCC33", 0.95f, 0.9f);
            Parts.S(t, Vector3.zero, new Vector3(0.3f, 0.4f, 0.3f), gold);
            Parts.S(t, new Vector3(-0.05f, 0.08f, -0.11f), new Vector3(0.07f, 0.1f, 0.04f), Parts.G("FFFFFF", 1.2f));
            Parts.Ring(t, new Vector3(0, -0.03f, 0), 0.15f, 0.02f, Parts.M("FF9F1C", 0.9f, 0.8f));
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3;
                Parts.S(t, new Vector3(Mathf.Cos(a) * 0.145f, 0.07f, Mathf.Sin(a) * 0.145f), 0.035f, Parts.M("E53935", 0.8f, 0.4f));
            }
        }
    }
}
