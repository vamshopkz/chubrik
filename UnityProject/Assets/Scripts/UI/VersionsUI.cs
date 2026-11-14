using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Chubrik
{
    /// <summary>
    /// Settings → «Версии игры»: every released version from GitHub with its notes; pick one to get it.
    /// Phone: opens that version's APK in the browser. A newer one installs over the game (progress kept);
    /// an older one Android installs only after the game is removed (progress is lost), so we ask first.
    /// Browser: versions from 0.12 on are kept on the site (publish_web.sh → v/VER/) and open in a new tab.
    /// </summary>
    public static class VersionsUI
    {
        [Serializable] class Asset { public string name = "", browser_download_url = ""; }
        [Serializable] class Rel { public string tag_name = "", name = "", body = "", published_at = ""; public bool draft, prerelease; public Asset[] assets; }
        [Serializable] class RelList { public Rel[] items; }

        const string Repo = "vamshopkz/chubrik";
        const string Api = "https://api.github.com/repos/" + Repo + "/releases?per_page=100";
        /// <summary>The first version whose browser build is kept on the site.</summary>
        public const string FirstWebVersion = "0.12";

        static List<Rel> rels;
        static bool loading, failed;
        static Window win;

        static string Ver(Rel r) => r.tag_name.TrimStart('v', 'V');

        public static void Open()
        {
            Show();
            if (rels == null && !loading && Updater.I != null) Updater.I.StartCoroutine(Load());
        }

        static IEnumerator Load()
        {
            loading = true; failed = false;
            using (var req = UnityWebRequest.Get(Api))
            {
                req.SetRequestHeader("Accept", "application/vnd.github+json");
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var list = JsonUtility.FromJson<RelList>("{\"items\":" + req.downloadHandler.text + "}");
                        rels = new List<Rel>();
                        foreach (var r in list.items) if (!r.draft && !string.IsNullOrEmpty(r.tag_name)) rels.Add(r);
                        rels.Sort((a, b) => Updater.Compare(Ver(b), Ver(a)));
                    }
                    catch { failed = true; }
                }
                else failed = true;
            }
            loading = false;
            if (win != null && UIRoot.Open == win) Window.Refresh();
        }

        static void Show()
        {
            win = Window.Show("ВЕРСИИ ИГРЫ", w =>
            {
                string cur = Application.version;
                w.Text($"Сейчас у тебя версия {cur}.", 30, UIKit.Gold);
                if (loading || (rels == null && !failed)) { w.Text("Загружаю список версий…", 28, UIKit.Muted); return; }
                if (failed || rels == null)
                {
                    w.Text("Не получилось загрузить список. Проверь интернет.", 28, UIKit.Muted);
                    w.Row("Попробовать ещё раз", null, "ОБНОВИТЬ", UIKit.Blue, () => { rels = null; Open(); });
                    return;
                }
                bool web = WebBridge.IsWeb;
                foreach (var r in rels)
                {
                    var rel = r;
                    string v = Ver(rel);
                    int cmp = Updater.Compare(v, cur);
                    string date = rel.published_at.Length >= 10 ? $"{rel.published_at.Substring(8, 2)}.{rel.published_at.Substring(5, 2)}.{rel.published_at.Substring(0, 4)}" : "";
                    string notes = (rel.body ?? "").Replace("\r", " ").Replace("\n", " ");
                    if (notes.Length > 90) notes = notes.Substring(0, 90) + "…";
                    string title = $"Версия {v}" + (rels[0] == rel ? "  · новая" : "") + (date != "" ? $"  · {date}" : "");
                    if (cmp == 0) { w.Row(title, notes, "ИГРАЕШЬ", UIKit.Green, null, false); continue; }
                    if (web)
                    {
                        bool kept = rels[0] == rel || Updater.Compare(v, FirstWebVersion) >= 0;
                        w.Row(title, kept ? notes : "Эта версия есть только для телефона", "ИГРАТЬ", UIKit.Blue, () => PlayWeb(rel, v), kept);
                    }
                    else
                        w.Row(title, notes, cmp > 0 ? "ОБНОВИТЬ" : "СКАЧАТЬ", cmp > 0 ? UIKit.Lime : UIKit.Blue, () => { if (cmp > 0) Download(rel); else AskOlder(rel, v); });
                }
                w.Footer.text = $"Всего версий: {rels.Count}";
            }, 1060, 780);
        }

        static string ApkUrl(Rel r)
        {
            if (r.assets != null) foreach (var a in r.assets) if (a.name.EndsWith(".apk")) return a.browser_download_url;
            return $"https://github.com/{Repo}/releases/tag/{r.tag_name}";
        }

        static void Download(Rel r)
        {
            Save.Write();
            Application.OpenURL(ApkUrl(r));
            Hud.Toast("Браузер скачает игру — открой файл и нажми «Установить»", UIKit.Cyan);
        }

        /// <summary>Android will not put an older version over a newer one: say so before downloading.</summary>
        static void AskOlder(Rel r, string v)
        {
            Window.Show("СТАРАЯ ВЕРСИЯ " + v, w =>
            {
                bool otherApp = Updater.Compare(v, "0.5.2") < 0;
                if (otherApp)
                    w.Text("Эта версия поставится рядом, как отдельная игра. Твоя игра и прогресс останутся.", 30);
                else
                {
                    w.Text("Телефон не ставит старую версию поверх новой.", 30);
                    w.Text("Сначала удали игру, потом установи скачанный файл. Прогресс, монеты и покупки пропадут!", 30, UIKit.Gold);
                }
                w.Row("Скачать версию " + v, null, "СКАЧАТЬ", UIKit.Blue, () => { Window.Close(); Download(r); });
                w.Row("Не надо", null, "НАЗАД", UIKit.Green, Show);
            }, 900, 560);
        }

        static void PlayWeb(Rel r, string v)
        {
            Save.Write(); WebBridge.SyncFiles();
            var u = Application.absoluteURL ?? "";
            int q = u.IndexOfAny(new[] { '?', '#' });
            if (q >= 0) u = u.Substring(0, q);
            int vi = u.IndexOf("/v/");
            string root = vi >= 0 ? u.Substring(0, vi + 1) : u.Substring(0, u.LastIndexOf('/') + 1);
            Application.OpenURL(rels[0] == r ? root : root + "v/" + v + "/");
        }
    }
}
