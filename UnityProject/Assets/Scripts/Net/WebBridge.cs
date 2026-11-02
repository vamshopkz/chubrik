using System.Runtime.InteropServices;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Browser build helpers (Plugins/WebGL/GadWeb.jslib): save flushing, page reload.</summary>
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GadSyncFs();
        [DllImport("__Internal")] static extern void GadReload();
        /// <summary>Reload the page (the new version of the game is on the server).</summary>
        public static void Reload() => GadReload();
        /// <summary>Flush the save files to the browser's storage (IndexedDB) right away.</summary>
        public static void SyncFiles() => GadSyncFs();
#else
        public static void SyncFiles() { }
        public static void Reload() { }
#endif
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
    }
}
