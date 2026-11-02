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
            if (Testing) { Save.P = new Profile(); Save.Fix(); }
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

            UIRoot.Create();
            AudioFx.Create();
            Updater.Create();
            Effects.Create();
            PlayerCtl.Create();
            PostFx(mobile);
            Pickups.Create(null);
            Lobby.Build();
            Preview.Create();
            Hud.Create();
            Save.Changed += WardrobeUI.Changed;
            PlayerCtl.I.Teleport(Lobby.Spawn, 0);

            if (Testing) gameObject.AddComponent<AutoTest>();
            else Menus.Main();
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
            if (Testing) return;
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIRoot.Open != null) Window.Close();
                else if (ShopUI.BoxOpen) ShopUI.CloseBox();
                else if (WardrobeUI.IsOpen) WardrobeUI.Close();
                else if (!Menus.MainOpen) Menus.Pause();
            }
            else if (wasLocked && !locked && !Menus.AnyOpen) Menus.Pause();   // the browser released the mouse (Esc)
            if (!Hud.Touch && !locked && !Menus.AnyOpen && Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                Menus.LockCursor();
            if (!Hud.Touch && Menus.AnyOpen && locked) Menus.Unlock();
            wasLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        void OnApplicationPause(bool paused) { if (paused) Save.Write(); }
        void OnApplicationQuit() { Save.Write(); }
    }
}
