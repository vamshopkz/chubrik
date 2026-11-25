using System;
using System.Reflection;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// 22.11: switching to another chubrik rebuilds the whole game (see <see cref="Boot.Restart"/>). After the old
    /// scene objects are destroyed, every static thing of the game that was made from the old profile or points at
    /// the old objects is cleared here, so the lobby, the worlds, the stadium and the backrooms are built again from
    /// the new profile exactly like at start.
    /// </summary>
    public static class GameReset
    {
        public static void Statics()
        {
            NullDestroyed();

            // lists of things in the lobby and the worlds (their objects are gone)
            Zones.All.Clear();
            Breakable.All.Clear();
            Bird.ResetStatics();
            Rescue.ResetStatics();
            Eggs.ResetStatics();
            Chests.ResetStatics(); Friends.ResetStatics(); Rps.ResetStatics(); HideSeek.ResetStatics(); PvpArena.ResetStatics();
            TitanArena.Bosses.Clear(); TitanArena.Pads.Clear(); TitanArena.Centers.Clear();
            AdminPanel.ResetStatics();
            Water.ResetStatics();
            Backrooms.ResetStatics();
            Football.ResetStatics();
            Lobby.TokenSpots.Clear(); Lobby.Gates.Clear(); Lobby.Barriers = 0;
            World2.Enemies.Clear();
            World3.ResetStatics();
            World4.ResetStatics();
            World5.Logs.Clear(); World5.Slabs.Clear();
            World6.Bananas.Clear(); World6.Marks.Clear(); World6.PausedUntil = 0;
            World67.Bots.Clear(); World67.Flickers.Clear(); World67.FakeWalls.Clear(); Popup3D.All.Clear();
            World8.Mobs.Clear(); World8.PadSpots.Clear();

            // where the player is and what is open
            Worlds.Current = 1;
            UIRoot.TypingText = false; UIRoot.LookActive = false;
        }

        /// <summary>
        /// Every static field of the game (namespace Chubrik) that holds a scene object which is destroyed now —
        /// roots of the lobby and the worlds, the player, the HUD, menus, bosses… — becomes a real null
        /// (Unity's «destroyed» objects are not null for <c>??</c> and <c>?.</c>). Living things (the sound, online,
        /// the updater, voice, chat; materials, meshes, textures) are left alone.
        /// </summary>
        static void NullDestroyed()
        {
            const BindingFlags F = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            int n = 0;
            Type[] types;
            try { types = typeof(GameReset).Assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            foreach (var t in types)
            {
                if (t == null || t.Namespace != "Chubrik" || t.ContainsGenericParameters) continue;
                FieldInfo[] fields;
                try { fields = t.GetFields(F); } catch { continue; }
                foreach (var f in fields)
                {
                    if (f.IsLiteral || f.IsInitOnly || !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)) continue;
                    try
                    {
                        var v = f.GetValue(null) as UnityEngine.Object;
                        if (!ReferenceEquals(v, null) && v == null) { f.SetValue(null, null); n++; }
                    }
                    catch (Exception e) { Debug.LogWarning($"[Reset] {t.Name}.{f.Name}: {e.Message}"); }
                }
            }
            Debug.Log($"[Reset] {n} links to the old game cleared");
        }
    }
}
