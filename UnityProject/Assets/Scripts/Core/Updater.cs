using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Chubrik
{
    /// <summary>
    /// 1.1: updates from the menu. The game looks for a newer version on start and every 10 minutes;
    /// only when there is one does the main menu show the «ОБНОВИТЬ» button.
    /// Phone (APK): version.json + Chubrik.apk of the latest GitHub release (release.sh); the button
    /// opens the APK link in the phone's browser — the save is kept, every build is signed with the same key.
    /// Browser: version.json next to the page (publish_web.sh); the button reloads the page.
    /// </summary>
    public class Updater : MonoBehaviour
    {
        public static Updater I;

        [Serializable]
        class VersionInfo { public string version = "", notes = "", apk = "", sha256 = ""; public long size; }

        public enum State { Off, Idle, Checking, UpToDate, Available }

        public State Status { get; private set; } = State.Idle;
        public string LatestVersion { get; private set; } = "";
        /// <summary>v0.27: what is new in the latest version (shown in the «Вышло обновление» message).</summary>
        public string LatestNotes { get; private set; } = "";
        bool messagePending;
        /// <summary>The menu shows the button only while there is something newer to get.</summary>
        public static bool HasUpdate => I != null && I.Status >= State.Available && !string.IsNullOrEmpty(I.LatestVersion);

        string baseUrl = "", apkName = "Chubrik.apk";
        bool toasted;
        float nextCheck;

        static string UpdateDir => Path.Combine(Application.persistentDataPath, "update");

        public static void Create()
        {
            I = new GameObject("Updater").AddComponent<Updater>();
            DontDestroyOnLoad(I.gameObject);
        }

        void Awake()
        {
            if (WebBridge.IsWeb)
            {
                // the page's own folder: https://…github.io/chubrik/
                var u = Application.absoluteURL ?? "";
                int q = u.IndexOfAny(new[] { '?', '#' });
                if (q >= 0) u = u.Substring(0, q);
                baseUrl = u.Substring(0, u.LastIndexOf('/') + 1);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                var cfg = Resources.Load<TextAsset>("update_base");
                baseUrl = cfg != null ? cfg.text.Trim() : "";
            }
            if (string.IsNullOrEmpty(baseUrl)) Status = State.Off;   // computers, the editor: nothing to update
            // anything downloaded earlier is either installed by now (we are running it) or stale
            try { if (Directory.Exists(UpdateDir)) Directory.Delete(UpdateDir, true); } catch { }
        }

        void Update()
        {
            // v0.27: «Вышло обновление 0.27!» with what is new — once per version, when nothing else is open
            if (messagePending && HasUpdate && PlayerCtl.I != null && !PlayerCtl.I.MenuOrbit && !Menus.AnyOpen && !Boot.Testing)
            {
                messagePending = false;
                if (Save.P.updateSeen != LatestVersion) { Save.P.updateSeen = LatestVersion; Save.Write(); PhoneNotify(); ShowMessage(); }
            }
            if (Status == State.UpToDate && Time.unscaledTime - checkedAt > 600f) Status = State.Idle;   // v0.27: look again every 10 minutes
            if (Status == State.Off || Status == State.Checking || HasUpdate) return;
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 600f;
            StartCoroutine(Check());
        }

        float checkedAt;

        IEnumerator Check()
        {
            Status = State.Checking;
            checkedAt = Time.unscaledTime;
            // cache-buster: the CDN may hand back a stale version.json for a while
            using var req = UnityWebRequest.Get(baseUrl + "version.json?t=" + DateTime.UtcNow.Ticks);
            req.timeout = 15;
            yield return req.SendWebRequest();
            VersionInfo info = null;
            if (req.result == UnityWebRequest.Result.Success)
                try { info = JsonUtility.FromJson<VersionInfo>(req.downloadHandler.text); } catch { }
            if (info == null || string.IsNullOrEmpty(info.version)) { Status = State.Idle; yield break; }   // no internet: try later
            if (Compare(info.version, Application.version) <= 0) { Status = State.UpToDate; yield break; }
            LatestVersion = info.version;
            LatestNotes = info.notes ?? "";
            messagePending = true;
            if (!string.IsNullOrEmpty(info.apk)) apkName = info.apk;
            Status = State.Available;
            Debug.Log($"[Update] {Application.version} → {LatestVersion} available");
            if (!toasted && !Menus.MainOpen) { toasted = true; Hud.Toast("Вышла новая версия " + LatestVersion + " — кнопка «ОБНОВИТЬ» в меню", UIKit.Lime); }
        }

        /// <summary>Compare dotted versions numerically: "1.10" > "1.9".</summary>
        public static int Compare(string a, string b)
        {
            var pa = a.Split('.');
            var pb = b.Split('.');
            for (int i = 0; i < Mathf.Max(pa.Length, pb.Length); i++)
            {
                int x = i < pa.Length && int.TryParse(pa[i], out var xa) ? xa : 0;
                int y = i < pb.Length && int.TryParse(pb[i], out var yb) ? yb : 0;
                if (x != y) return x.CompareTo(y);
            }
            return 0;
        }

        /// <summary>What the menu button says right now.</summary>
        public string ButtonText => "ОБНОВИТЬ ДО " + LatestVersion;

        /// <summary>The menu button.</summary>
        public void Press()
        {
            if (!HasUpdate) return;
            Save.Write();
            if (WebBridge.IsWeb) { WebBridge.SyncFiles(); WebBridge.Reload(); return; }
            // the phone's browser downloads the new APK; the game itself never installs apps
            // (an app that installs APKs looks like a "dropper" to Play Protect and gets blocked)
            Application.OpenURL(DownloadUrl);
            Hud.Toast("Браузер скачает обновление — откройте файл и нажмите «Обновить»", UIKit.Cyan);
        }

        string DownloadUrl => baseUrl.EndsWith("/releases/latest/download/")
            ? baseUrl.Substring(0, baseUrl.Length - "latest/download/".Length) + $"download/v{LatestVersion}/{apkName}"   // exactly the version we checked
            : baseUrl + apkName;

        /// <summary>v0.27: a real notification on the phone («Вышло обновление 0.27») and two little notes.</summary>
        public void PhoneNotify()
        {
            StartCoroutine(TwoNotes());
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(Notify());
#endif
        }

        System.Collections.IEnumerator TwoNotes()
        {
            AudioFx.Play("note2", 0.9f);
            yield return new WaitForSecondsRealtime(0.16f);
            AudioFx.Play("note6", 0.9f);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        System.Collections.IEnumerator Notify()
        {
            var req = new Unity.Notifications.Android.PermissionRequest();
            while (req.Status == Unity.Notifications.Android.PermissionStatus.RequestPending) yield return null;
            if (req.Status != Unity.Notifications.Android.PermissionStatus.Allowed) yield break;
            Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(new Unity.Notifications.Android.AndroidNotificationChannel
            {
                Id = "updates", Name = "Обновления Чубрика", Description = "Когда выходит новая версия игры",
                Importance = Unity.Notifications.Android.Importance.Default,
            });
            var notes = string.IsNullOrEmpty(LatestNotes) ? "Исправления и улучшения" : LatestNotes;
            Unity.Notifications.Android.AndroidNotificationCenter.SendNotification(new Unity.Notifications.Android.AndroidNotification
            {
                Title = "Вышло обновление " + LatestVersion + "!",
                Text = notes.Length > 240 ? notes.Substring(0, 237) + "…" : notes,
                FireTime = DateTime.Now,
            }, "updates");
        }
#endif

        /// <summary>The message: «Вышло обновление X!» and what is new in it, with «ОБНОВИТЬ».</summary>
        public void ShowMessage()
        {
            Menus.Unlock();
            Window.Show("ВЫШЛО ОБНОВЛЕНИЕ " + LatestVersion + "!", w =>
            {
                w.Text("Что нового:", 30, UIKit.Gold);
                foreach (var part in (string.IsNullOrEmpty(LatestNotes) ? "Исправления и улучшения" : LatestNotes).Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries))
                    w.Text("• " + part.Trim().TrimEnd('.') , 26);
                w.Row("Обновить сейчас", WebBridge.IsWeb ? "Страница перезагрузится — всё сохранится" : "Скачается новая версия игры", "ОБНОВИТЬ", UIKit.Green, Press, true, UIKit.Lime);
                w.Row("Потом", "Кнопка «ОБНОВИТЬ» будет в меню", "ПОТОМ", UIKit.Blue, Window.Close);
            }, 1100, 760);
        }

        /// <summary>Tests: pretend a newer version came out (or not).</summary>
        public void Fake(string version, string notes = "")
        {
            LatestNotes = notes ?? "";
            LatestVersion = version ?? "";
            Status = string.IsNullOrEmpty(version) ? State.UpToDate : State.Available;
        }
    }
}
