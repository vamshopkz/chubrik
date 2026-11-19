using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// v0.27: a little chat in the top left corner while you are in a room with a friend. Write if you don't want
    /// to talk; your voice talks show up in it too («голос, 3 сек»). The messages go only to that friend.
    /// While it is open, the coins, hearts and the other top-left things move to the right.
    /// </summary>
    public class RoomChat : MonoBehaviour
    {
        public static RoomChat I;
        public const float W = 300, H = 290;
        public static bool Visible => I != null && I.panel != null && I.panel.activeSelf;
        public readonly List<string> Lines = new List<string>();
        GameObject panel;
        TextMeshProUGUI log;
        TMP_InputField input;
        int seq, lastSeq = -1;
        float friendTalkFrom = -1, friendTalkLast = -1, meTalkFrom = -1;

        public static void Create()
        {
            if (I != null) return;
            I = new GameObject("RoomChat").AddComponent<RoomChat>();
            DontDestroyOnLoad(I.gameObject);
        }

        void Build()
        {
            var safe = UIRoot.Safe();
            var bg = UIKit.Img(UIRoot.HudLayer, UIKit.Panel, new Color(0.08f, 0.12f, 0.3f, 0.82f), "RoomChat", true);
            UIKit.Place(bg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12 + safe.x, -12 - safe.w), new Vector2(W, H));
            panel = bg.gameObject;
            var title = UIKit.Txt(bg.transform, "ЧАТ С ДРУГОМ", 20, UIKit.Gold, TextAlignmentOptions.MidlineLeft);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -4), new Vector2(W - 24, 30));
            log = UIKit.Txt(bg.transform, "", 18, Color.white, TextAlignmentOptions.BottomLeft, false);
            UIKit.Place(log.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -36), new Vector2(W - 24, H - 100));
            log.textWrappingMode = TextWrappingModes.Normal; log.overflowMode = TextOverflowModes.Truncate;
            input = UIKit.Input(bg.transform, "Написать…", 120, 20);
            UIKit.Place(input.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(W - 92, 52));
            input.onSubmit.AddListener(_ => Send());
            var go = UIKit.Button(bg.transform, "ОТПР", UIKit.Green, Send, 18, "ChatSend");
            UIKit.Place(go.Bg.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(76, 52));
            Redraw();
        }

        void Update()
        {
            var on = Online.I;
            bool room = on != null && on.RoomPeer != null && PlayerCtl.I != null && !PlayerCtl.I.MenuOrbit;
            if (room && panel == null) Build();
            if (panel != null && panel.activeSelf != (room && !Menus.OverlayOpen)) panel.SetActive(room && !Menus.OverlayOpen);
            if (!room) { if (Lines.Count > 0 && on != null && on.RoomWith == null) { Lines.Clear(); Redraw(); } return; }
            // voice talks go into the chat when they end
            var v = Voice.I;
            if (v != null)
            {
                if (v.Talking && meTalkFrom < 0) meTalkFrom = Time.unscaledTime;
                if (!v.Talking && meTalkFrom >= 0) { Add("Ты", $"(голос, {Mathf.Max(1, Mathf.RoundToInt(Time.unscaledTime - meTalkFrom))} сек)"); meTalkFrom = -1; }
                if (Time.unscaledTime - v.HeardAt < 0.3f) { if (friendTalkFrom < 0) friendTalkFrom = v.HeardAt; friendTalkLast = v.HeardAt; }
                else if (friendTalkFrom >= 0 && Time.unscaledTime - friendTalkLast > 0.8f)
                {
                    Add(FriendName, $"(голос, {Mathf.Max(1, Mathf.RoundToInt(friendTalkLast - friendTalkFrom + 0.1f))} сек)");
                    friendTalkFrom = -1;
                }
            }
        }

        static string FriendName { get { var p = Online.I?.RoomPeer; return p != null && !string.IsNullOrEmpty(p.nick) ? p.nick : "Друг"; } }

        public void Send() => Send(input != null ? input.text : "");

        public void Send(string text)
        {
            text = Online.Clean(text ?? "");
            if (text.Length == 0) return;
            if (text.Length > 120) text = text.Substring(0, 120);
            var on = Online.I;
            if (on == null || on.RoomWith == null) return;
            on.SendTo(Online.VoiceTopic + "/" + on.RoomWith, $"C|{on.Id}|{++seq}|{text}");
            Add("Ты", text);
            if (input) { input.text = ""; input.DeactivateInputField(); }
            UIRoot.TypingText = false;
            AudioFx.Play("click", 0.5f);
        }

        /// <summary>«C|id|seq|text» from the friend (both relays bring it — shown once).</summary>
        public void OnChat(string[] f)
        {
            if (f.Length < 4 || Online.I == null || f[1] != Online.I.RoomWith) return;
            if (!int.TryParse(f[2], out var s) || (s <= lastSeq && lastSeq - s < 1000)) return;
            lastSeq = s;
            Add(FriendName, string.Join("|", f, 3, f.Length - 3));
            AudioFx.Play("note4", 0.4f);
        }

        public void Add(string who, string text)
        {
            Lines.Add($"<color=#FFC83A>{who}:</color> {text}");
            while (Lines.Count > 30) Lines.RemoveAt(0);
            Redraw();
        }

        void Redraw()
        {
            if (log == null) return;
            int from = Mathf.Max(0, Lines.Count - 7);
            log.text = string.Join("\n", Lines.GetRange(from, Lines.Count - from));
        }
    }
}
