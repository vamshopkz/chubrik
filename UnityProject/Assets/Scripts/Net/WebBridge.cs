using System.Runtime.InteropServices;
using UnityEngine;

namespace Chubrik
{
    /// <summary>Browser build helpers (Plugins/WebGL/GadWeb.jslib): save flushing, page reload, WebSocket for the online relay.</summary>
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GadSyncFs();
        [DllImport("__Internal")] static extern void GadReload();
        [DllImport("__Internal")] static extern void GadGo(string url);
        /// <summary>v0.27: go to another page in the same tab (another version of the game).</summary>
        public static void Go(string url) => GadGo(url);
        [DllImport("__Internal")] static extern int GadWsOpen(string url);
        [DllImport("__Internal")] static extern int GadWsState(int id);
        [DllImport("__Internal")] static extern int GadWsSend(int id, byte[] data, int length);
        [DllImport("__Internal")] static extern int GadWsRecv(int id, byte[] into, int offset, int max);
        [DllImport("__Internal")] static extern void GadWsClose(int id);
        public static int WsOpen(string url) => GadWsOpen(url);
        public static int WsState(int id) => GadWsState(id);
        public static bool WsSend(int id, byte[] data) => GadWsSend(id, data, data.Length) == 1;
        public static int WsRecv(int id, byte[] into, int offset) => System.Math.Max(0, GadWsRecv(id, into, offset, into.Length - offset));
        public static void WsClose(int id) => GadWsClose(id);
        /// <summary>Reload the page (the new version of the game is on the server).</summary>
        public static void Reload() => GadReload();
        /// <summary>Flush the save files to the browser's storage (IndexedDB) right away.</summary>
        public static void SyncFiles() => GadSyncFs();
#else
        public static int WsOpen(string url) => 0;
        public static int WsState(int id) => 2;
        public static bool WsSend(int id, byte[] data) => false;
        public static int WsRecv(int id, byte[] into, int offset) => 0;
        public static void WsClose(int id) { }
        public static void SyncFiles() { }
        public static void Reload() { }
        public static void Go(string url) => Application.OpenURL(url);
#endif
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
    }
}
