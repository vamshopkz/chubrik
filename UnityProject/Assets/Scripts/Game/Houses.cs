using System.Collections.Generic;
using UnityEngine;
using static Chubrik.Parts;

namespace Chubrik
{
    /// <summary>
    /// 17.11: little houses in the lobby you can walk into (a bed, a table with a lamp, a rug, a bookshelf, pictures).
    /// Now and then a rescued friend comes to visit: walks in through the door, stays a while and goes home.
    /// Spots are picked where nothing else stands (checked against everything already built in the lobby).
    /// </summary>
    public static class Houses
    {
        public class House
        {
            public Transform t;
            public string name;
            public Vector3 Door => t.TransformPoint(new Vector3(0, 0, 3.2f));
            public Vector3 Front => t.TransformPoint(new Vector3(0, 0, 5.5f));
            public Vector3 Inside => t.TransformPoint(new Vector3(0.6f, 0, 0.4f));
            public bool Contains(Vector3 p) { var l = t.InverseTransformPoint(p); return Mathf.Abs(l.x) < 2.9f && Mathf.Abs(l.z) < 2.9f && l.y > -0.5f && l.y < 3.2f; }
        }
        public static readonly List<House> All = new List<House>();
        public static int Visits;          // autotest
        public static Guest Current;
        const float Half = 3f, Wall = 0.25f, H = 3.2f;
        static readonly string[] Names = { "ДОМИК ЧУБРИКОВ", "ДОМ ДРУЗЕЙ", "ГОСТЕВОЙ ДОМИК" };
        static readonly string[] Colors = { "FFB74D", "81D4FA", "F48FB1" };

        public static void Build(Transform root)
        {
            All.Clear(); Current = null;
            Physics.SyncTransforms();
            var spots = PickSpots(root, 3);
            for (int i = 0; i < spots.Count; i++) All.Add(Make(root, spots[i].pos, spots[i].yaw, i));
            if (All.Count > 0) root.gameObject.AddComponent<Visitors>();
        }

        // ---------------- where ----------------

        static List<(Vector3 pos, float yaw)> PickSpots(Transform root, int want)
        {
            var obstacles = new List<Bounds>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds; var n = r.gameObject.name;
                bool water = n is "River" or "Lake" or "Pool" or "Pond" or "Water";
                if (!water && (b.size.x > 9 || b.size.z > 9 || b.size.y < 0.05f || b.min.y > 3.5f)) continue;
                if (n == "Water") continue;   // the sea under everything
                obstacles.Add(b);
            }
            var avoid = new List<Vector3> { Lobby.Spawn, new Vector3(-5, 0, 18.2f), new Vector3(-6.5f, 0, -9.4f), new Vector3(-6.5f, 0, -17.2f), Lobby.PadFrom, Lobby.TestMushroom, Lobby.CactusPos };
            avoid.AddRange(Lobby.TokenSpots);
            var found = new List<(Vector3, float)>();
            // the park first, then the hub
            var areas = new[] { new Rect(-49, -5.5f, 37, 22), new Rect(-10, -20, 36, 42) };
            foreach (var a in areas)
                for (float z = a.yMax; z >= a.yMin && found.Count < want; z -= 1.5f)
                    for (float x = a.xMin; x <= a.xMax && found.Count < want; x += 1.5f)
                    {
                        var c = new Vector3(x, 0, z);
                        if (found.Exists(f => (f.Item1 - c).magnitude < 13)) continue;
                        if (avoid.Exists(v => new Vector2(v.x - c.x, v.z - c.z).magnitude < 7)) continue;
                        // the door looks at the middle of the hub
                        var to = new Vector3(2 - x, 0, 2 - z);
                        float yaw = Mathf.Round(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg / 90f) * 90f;
                        if (Free(c, yaw, obstacles)) found.Add((c, yaw));
                    }
            return found;
        }

        static bool Free(Vector3 c, float yaw, List<Bounds> obstacles)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            // the house and the space in front of its door
            var box = new Bounds(c + Vector3.up * 1.6f, new Vector3(2 * Half + 1.2f, 3f, 2 * Half + 1.2f));
            var front = new Bounds(c + rot * new Vector3(0, 1.6f, Half + 2.2f), new Vector3(3.2f, 3f, 3.2f));
            if (Physics.CheckBox(box.center, box.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (Physics.CheckBox(front.center, front.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) return false;
            foreach (var o in obstacles) if (o.Intersects(box) || o.Intersects(front)) return false;
            // flat ground under every corner, the middle and the door step
            foreach (var p in new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1), Vector3.zero, new Vector3(0, 0, 1.6f) })
            {
                var q = c + rot * (p * (Half + 0.4f));
                if (!Physics.Raycast(q + Vector3.up * 6, Vector3.down, out var hit, 9, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
                if (Mathf.Abs(hit.point.y) > 0.15f) return false;
            }
            return true;
        }

        // ---------------- the house ----------------

        static House Make(Transform root, Vector3 c, float yaw, int i)
        {
            var t = Node("House" + i, root, c, new Vector3(0, yaw, 0));
            var wallM = Lobby.Tiled(Tex.Planks(Colors[i]), 1);
            var inner = M("FFF3E0", 0.2f);
            var floorM = Lobby.Tiled(Tex.Planks("A0642F"), 1);
            var roofM = M(i == 1 ? "1E5BD8" : i == 2 ? "8E24AA" : "C62828", 0.3f);
            var trim = M("FFFFFF", 0.3f);
            Box(t, new Vector3(0, 0.06f, 0), new Vector3(2 * Half, 0.12f, 2 * Half), floorM);
            // walls (the front one has a doorway)
            Box(t, new Vector3(0, H / 2, -Half + Wall / 2), new Vector3(2 * Half, H, Wall), wallM);
            foreach (var sd in new[] { -1f, 1f })
            {
                Box(t, new Vector3(sd * (Half - Wall / 2), H / 2, 0), new Vector3(Wall, H, 2 * Half), wallM);
                Box(t, new Vector3(sd * 1.95f, H / 2, Half - Wall / 2), new Vector3(2.1f, H, Wall), wallM);
                // windows with frames and a flower box
                foreach (var z in new[] { -1.2f, 1.2f })
                {
                    B(t, new Vector3(sd * (Half + 0.01f), 1.7f, z), new Vector3(0.03f, 0.9f, 0.9f), Parts.Glass("BFE9FF", 0.5f));
                    B(t, new Vector3(sd * (Half + 0.03f), 1.7f, z), new Vector3(0.04f, 1.0f, 0.08f), trim);
                    B(t, new Vector3(sd * (Half + 0.03f), 1.7f, z), new Vector3(0.04f, 0.08f, 1.0f), trim);
                    B(t, new Vector3(sd * (Half + 0.12f), 1.16f, z), new Vector3(0.22f, 0.16f, 0.95f), M("8D5524", 0.2f));
                    for (int k = 0; k < 4; k++) S(t, new Vector3(sd * (Half + 0.14f), 1.32f, z - 0.33f + k * 0.22f), 0.13f, M(k % 2 == 0 ? "FF5FA2" : "FFE14D", 0.3f));
                }
            }
            Box(t, new Vector3(0, H - 0.35f, Half - Wall / 2), new Vector3(1.8f, 0.7f, Wall), wallM);   // over the door
            B(t, new Vector3(0, 2.5f, Half + 0.02f), new Vector3(1.9f, 0.1f, 0.06f), trim);
            foreach (var sd in new[] { -1f, 1f }) B(t, new Vector3(sd * 0.92f, 1.25f, Half + 0.02f), new Vector3(0.1f, 2.5f, 0.06f), trim);
            // the open door
            var door = Node("Door", t, new Vector3(-0.85f, 0, Half + 0.05f), new Vector3(0, -100, 0));
            B(door, new Vector3(0.8f, 1.2f, 0), new Vector3(1.6f, 2.35f, 0.08f), M("7A4A1F", 0.3f));
            S(door, new Vector3(1.4f, 1.15f, 0.08f), 0.09f, M("FFC83A", 0.9f, 0.8f));
            // a gable roof with a chimney
            float rise = 2.0f, run = Half + 0.4f, len = Mathf.Sqrt(rise * rise + run * run), ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            foreach (var sd in new[] { -1f, 1f })
                Solid(t, new Vector3(0, H + rise / 2, sd * run / 2), new Vector3(2 * Half + 0.8f, 0.2f, len + 0.15f), roofM, new Vector3(sd * ang, 0, 0));
            foreach (var sd in new[] { -1f, 1f })
                for (int k = 0; k < 5; k++)
                {
                    float w = 2 * Half * (1 - (k + 0.5f) / 5f);
                    B(t, new Vector3(sd * (Half - Wall / 2), H + rise * (k + 0.5f) / 5f, 0), new Vector3(Wall, rise / 5f + 0.02f, w), wallM);
                }
            Box(t, new Vector3(1.6f, H + 1.8f, -1.2f), new Vector3(0.6f, 1.8f, 0.6f), M("8D6E63", 0.2f));
            Box(t, new Vector3(1.6f, H + 2.75f, -1.2f), new Vector3(0.75f, 0.15f, 0.75f), M("6D4C41", 0.2f));
            var sign = Prim.Text(t, Names[i], new Vector3(0, 2.9f, Half + 0.16f), 3.4f, Color.white);
            sign.gameObject.layer = Layers.World;
            // inside: a rug, a bed, a table with a lamp and stools, a bookshelf, pictures
            Cy(t, new Vector3(0.4f, 0.13f, 0.3f), new Vector3(3f, 0.01f, 2.4f), M(i == 0 ? "E53935" : i == 1 ? "1E88E5" : "8E24AA", 0.2f));
            Cy(t, new Vector3(0.4f, 0.14f, 0.3f), new Vector3(2.2f, 0.01f, 1.7f), M("FFE082", 0.2f));
            var bed = Node("Bed", t, new Vector3(-1.7f, 0.12f, -1.6f));
            Box(bed, new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.5f, 2.2f), M("8D5524", 0.3f));
            B(bed, new Vector3(0, 0.56f, 0.05f), new Vector3(1.3f, 0.14f, 2.0f), M("FFFFFF", 0.2f));
            B(bed, new Vector3(0, 0.66f, 0.35f), new Vector3(1.32f, 0.08f, 1.3f), M(Colors[(i + 1) % 3], 0.2f));
            B(bed, new Vector3(0, 0.7f, -0.75f), new Vector3(0.9f, 0.16f, 0.4f), M("FFFDE7", 0.2f));
            B(bed, new Vector3(0, 0.75f, -1.08f), new Vector3(1.4f, 0.9f, 0.1f), M("6D4C41", 0.3f));
            var table = Node("Table", t, new Vector3(1.3f, 0.12f, -0.9f));
            Box(table, new Vector3(0, 0.75f, 0), new Vector3(1.2f, 0.08f, 0.9f), M("A1887F", 0.3f));
            foreach (var p in new[] { new Vector3(-0.5f, 0.37f, -0.35f), new Vector3(0.5f, 0.37f, -0.35f), new Vector3(-0.5f, 0.37f, 0.35f), new Vector3(0.5f, 0.37f, 0.35f) })
                B(table, p, new Vector3(0.08f, 0.74f, 0.08f), M("6D4C41", 0.3f));
            Cy(table, new Vector3(-0.3f, 0.86f, 0), new Vector3(0.12f, 0.12f, 0.12f), M("ECEFF1", 0.3f));
            Cone(table, new Vector3(-0.3f, 1.08f, 0), new Vector3(0.32f, 0.22f, 0.32f), M("FFE082", 0.2f));
            S(table, new Vector3(-0.3f, 1.0f, 0), 0.12f, G("FFF59D", 2.5f));
            S(table, new Vector3(0.3f, 0.84f, 0.1f), new Vector3(0.24f, 0.08f, 0.24f), M("FFFFFF", 0.4f));   // a plate with a cake
            Cy(table, new Vector3(0.3f, 0.92f, 0.1f), new Vector3(0.18f, 0.06f, 0.18f), M("F48FB1", 0.3f));
            S(table, new Vector3(0.3f, 0.99f, 0.1f), 0.05f, M("E53935", 0.5f));
            foreach (var sd in new[] { -1f, 1f }) Box(table, new Vector3(sd * 0.85f, 0.25f, 0.5f), new Vector3(0.4f, 0.5f, 0.4f), M("8D6E63", 0.3f));
            var light = new GameObject("Lamp").AddComponent<Light>();
            light.transform.SetParent(t, false); light.transform.localPosition = new Vector3(0.9f, 2.2f, -0.4f);
            light.type = LightType.Point; light.range = 6.5f; light.intensity = 1.6f; light.color = new Color(1f, 0.85f, 0.6f); light.shadows = LightShadows.None;
            var shelf = Node("Shelf", t, new Vector3(Half - 0.45f, 0.12f, 1.6f), new Vector3(0, -90, 0));
            Box(shelf, new Vector3(0, 0.9f, 0), new Vector3(1.4f, 1.8f, 0.45f), M("6D4C41", 0.3f));
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < 7; k++)
                    B(shelf, new Vector3(-0.55f + k * 0.17f, 0.45f + r * 0.55f, 0.15f), new Vector3(0.12f, 0.36f, 0.3f), M(Colors[(k + r) % 3], 0.2f));
            // pictures: a chubrik portrait and a sun
            B(t, new Vector3(0.3f, 1.9f, -Half + Wall + 0.02f), new Vector3(1.1f, 0.8f, 0.04f), M("FFC83A", 0.8f, 0.6f));
            B(t, new Vector3(0.3f, 1.9f, -Half + Wall + 0.04f), new Vector3(0.95f, 0.65f, 0.03f), M("8FD3FF", 0.2f));
            S(t, new Vector3(0.3f, 1.85f, -Half + Wall + 0.07f), new Vector3(0.32f, 0.36f, 0.04f), M("9AA3AE", 0.2f));
            foreach (var sd in new[] { -1f, 1f }) S(t, new Vector3(0.3f + sd * 0.07f, 1.92f, -Half + Wall + 0.1f), 0.06f, M("FFFFFF", 0.2f));
            Cy(t, new Vector3(-Half + Wall + 0.03f, 1.9f, 0.6f), new Vector3(0.6f, 0.02f, 0.6f), M("FFE14D", 0.4f), new Vector3(0, 0, 90));
            return new House { t = t, name = Names[i] };
        }

        static GameObject Box(Transform t, Vector3 p, Vector3 s, Material m) => Lobby.Solid(PrimitiveType.Cube, p, s, m, default, t);
        static GameObject Solid(Transform t, Vector3 p, Vector3 s, Material m, Vector3 e) => Lobby.Solid(PrimitiveType.Cube, p, s, m, e, t);

        // ---------------- visitors ----------------

        /// <summary>Sends a rescued friend to visit house <paramref name="h"/> (the autotest calls it straight away).</summary>
        public static bool Visit(int h)
        {
            if (Current != null || h < 0 || h >= All.Count) return false;
            var ids = new List<string>(Save.P.rescued);
            if (ids.Count == 0) ids.Add("w2c0");
            var id = ids[Random.Range(0, ids.Count)];
            var house = All[h];
            var go = new GameObject("Guest " + id);
            go.transform.SetParent(Lobby.root, true);
            go.transform.position = house.Front + house.t.right * 3f;
            var m = ChubrikModel.Create("Model", go.transform, Rescue.LookOf(id), Layers.World);
            Current = go.AddComponent<Guest>();
            Current.Init(m, id, house);
            Visits++;
            return true;
        }

        class Visitors : MonoBehaviour
        {
            float next = 25f;
            void Update()
            {
                if (Worlds.Current != 1 || Current != null) return;
                next -= Time.deltaTime;
                if (next > 0) return;
                next = Random.Range(35f, 70f);
                // the house nearest to the player (so you can see the visit), otherwise any
                int best = Random.Range(0, All.Count);
                var p = PlayerCtl.I ? PlayerCtl.I.transform.position : Vector3.zero;
                for (int i = 0; i < All.Count; i++) if ((All[i].t.position - p).magnitude < 30) best = i;
                Visit(best);
            }
        }

        public class Guest : MonoBehaviour
        {
            ChubrikAnim anim;
            House house;
            string id;
            readonly List<Vector3> path = new List<Vector3>();
            float stay = -1;
            public bool InsideNow { get; private set; }

            public void Init(ChubrikModel m, string fid, House h)
            {
                id = fid; house = h;
                anim = gameObject.AddComponent<ChubrikAnim>();
                anim.model = m;
                anim.emote = Mathf.Max(0, m.look.Idx("emote"));
                var tag = Prim.Text(transform, Rescue.NameOf(id), new Vector3(0, 2.1f, 0), 2.4f, Color.white);
                tag.gameObject.layer = Layers.World;
                tag.gameObject.AddComponent<Billboard>();
                path.Add(h.Front); path.Add(h.Door); path.Add(h.Inside);
                Effects.Emit("stardust", transform.position + Vector3.up, 8, 0.4f);
            }

            void Update()
            {
                if (path.Count == 0)
                {
                    if (stay < 0) return;
                    if (!InsideNow)
                    {
                        InsideNow = true;
                        anim.PlayEmote();
                        if (PlayerCtl.I && house.Contains(PlayerCtl.I.transform.position)) Hud.Toast($"В гости зашёл друг — {Rescue.NameOf(id)}!", UIKit.Lime);
                    }
                    stay -= Time.deltaTime;
                    if (stay <= 0)
                    {
                        // time to go home
                        stay = -1; InsideNow = false;
                        path.Add(house.Door); path.Add(house.Front); path.Add(house.Front - house.t.right * 3f);
                        leaving = true;
                    }
                    else if (Random.value < 0.004f) anim.PlayEmote();
                    anim.speed01 = 0;
                    return;
                }
                var to = path[0] - transform.position; to.y = 0;
                if (to.magnitude < 0.15f)
                {
                    path.RemoveAt(0);
                    if (path.Count == 0)
                    {
                        if (leaving) { Effects.Emit("stardust", transform.position + Vector3.up, 8, 0.4f); Current = null; Destroy(gameObject); return; }
                        stay = Random.Range(16f, 26f);
                    }
                    return;
                }
                if (anim.emoting) anim.StopEmote();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 8);
                transform.position += to.normalized * Mathf.Min(to.magnitude, 2.4f * Time.deltaTime);
                var y = transform.position; y.y = house.t.position.y + (house.Contains(y) ? 0.12f : 0f); transform.position = y;
                anim.speed01 = 0.45f;
            }
            bool leaving;

            void OnDestroy() { if (Current == this) Current = null; }
        }
    }
}
