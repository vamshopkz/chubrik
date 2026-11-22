using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Chubrik
{
    /// <summary>Entry point (the only object in the scene): builds the whole game from code.</summary>
    public class Boot : MonoBehaviour
    {
        public static string[] Args = Array.Empty<string>();
        public static bool Testing;
        public static string Arg(string key)
        {
            int i = Array.IndexOf(Args, key);
            return i >= 0 && i + 1 < Args.Length ? Args[i + 1] : null;
        }
        public static bool Has(string key) => Array.IndexOf(Args, key) >= 0;

        bool wasLocked;

        void Awake()
        {
            Args = Environment.GetCommandLineArgs();
            Testing = Has("-autotest");
            if (Testing) Save.FileName = "test_auto.json";
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Save.Load();
            if (Testing) Save.FreshForTest();
            bool mobile = Application.isMobilePlatform;
            bool shadows = !(mobile && WebBridge.IsWeb);
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = mobile ? (WebBridge.IsWeb ? 0.7f : 0.85f) : 1f;
                urp.shadowDistance = shadows ? 45 : 0;
                urp.msaaSampleCount = mobile ? 1 : 4;
                urp.supportsHDR = !mobile;
            }
            QualitySettings.shadows = shadows ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;

            // phones: touches drive the UI themselves; mouse events made up from touches made the camera jump to the jump button
            if (Hud.Touch) Input.simulateMouseWithTouches = false;
            I = this;
            Build(true);
            Save.Changed += WardrobeUI.Changed;

            if (Testing) gameObject.AddComponent<AutoTest>();
            else Menus.Main();
        }

        /// <summary>Everything of the game that lives in the scene: UI, the player, the lobby with its friends,
        /// pickups and gates (built from Save.P). The worlds are built when entered. Run at start and after
        /// switching to another chubrik (<see cref="Restart"/>); the sound, the updater and online stay.</summary>
        static void Build(bool first)
        {
            bool mobile = Application.isMobilePlatform;
            UIRoot.Create();
            if (first)
            {
                AudioFx.Create();
                Updater.Create();
                Online.Create();
                Voice.Create();
                RoomChat.Create();
            }
            Effects.Create();
            PlayerCtl.Create();
            PostFx(mobile);
            Pickups.Create(null);
            Lobby.Build();
            Rescue.SpawnLobbyFriends();
            Preview.Create();
            Hud.Create();
            PlayerCtl.I.Teleport(Lobby.Spawn, 0);
            if (Save.P.butter) Butter.Set(true);
        }

        // ---------------- 22.11: another chubrik = another progress: the game is built again ----------------

        static Boot I;
        /// <summary>The old game is being taken down and the new one built (nothing should run meanwhile).</summary>
        public static bool Restarting { get; private set; }
        /// <summary>How many times the game was rebuilt (autotest).</summary>
        public static int Restarts { get; private set; }

        /// <summary>
        /// Soft restart: everything in the scene except this object (the autotest lives on it) is destroyed, then
        /// <paramref name="change"/> runs (Save loads the other chubrik's profile), every static list and link of the
        /// game is cleared (<see cref="GameReset.Statics"/>) and the game is built again exactly like at start,
        /// the player at the start of the lobby; then <paramref name="after"/>. Works the same on phones, in the
        /// browser and in the editor. The sound, the updater, online (and its players), voice and chat stay.
        /// </summary>
        public static void Restart(Action change, Action after = null)
        {
            if (I == null || Restarting) return;
            I.StartCoroutine(I.RestartRun(change, after));
        }

        System.Collections.IEnumerator RestartRun(Action change, Action after)
        {
            Restarting = true;
            Save.Write();
            try { if (Online.I != null) Online.I.LeaveQuiet(); } catch (Exception e) { Debug.LogException(e); }
            Menus.Unlock();
            foreach (var go in gameObject.scene.GetRootGameObjects())
                if (go != gameObject) Destroy(go);
            yield return null;   // the old game is gone at the end of that frame
            try { change?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            try { GameReset.Statics(); } catch (Exception e) { Debug.LogException(e); }
            try
            {
                Build(false);
                World3.Env(1);   // the lobby's light, whichever world we were in
                AudioFx.SetVolume(Save.P.volume);
                AudioFx.SetMusic(Save.P.music);
            }
            catch (Exception e) { Debug.LogException(e); }
            Restarts++;
            Restarting = false;
            try { after?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            Resources.UnloadUnusedAssets();   // the old lobby's meshes and textures
            Debug.Log($"[Boot] rebuilt for «{Save.CharName(Save.ActiveIndex)}» (restart {Restarts})");
        }

        static void PostFx(bool mobile)
        {
            var go = new GameObject("PostFx");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(false);
            var tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            var color = p.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(8f);
            color.saturation.Override(22f);
            var vig = p.Add<Vignette>(true);
            vig.intensity.Override(0.16f);
            vol.profile = p;
            var data = PlayerCtl.I.Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = !(mobile && WebBridge.IsWeb);
            data.antialiasing = mobile ? AntialiasingMode.None : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        void Update()
        {
            if (Testing || Restarting) return;
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIRoot.Open != null) Window.Close();
                else if (ShopUI.BoxOpen) ShopUI.CloseBox();
                else if (WardrobeUI.IsOpen) WardrobeUI.Close();
                else if (!Menus.MainOpen) Menus.Pause();
            }
            else if (wasLocked && !locked && !Menus.AnyOpen) Menus.Pause();   // the browser released the mouse (Esc)
            if (!Hud.Touch && !locked && !Menus.AnyOpen && !AdminPanel.IsOpen && Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                Menus.LockCursor();
            if (!Hud.Touch && Menus.AnyOpen && locked) Menus.Unlock();
            wasLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        void OnApplicationPause(bool paused) { if (paused && !Restarting) Save.Write(); }
        void OnApplicationQuit() { if (!Restarting) Save.Write(); }
    }
}
