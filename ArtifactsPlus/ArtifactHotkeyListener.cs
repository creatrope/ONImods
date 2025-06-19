using UnityEngine;
using System.Collections.Generic;
using HLib;

namespace ArtifactsPlus
{
    /// <summary>
    /// MonoBehaviour to call HotkeyListener.Update every frame, symmetric with SensorsPlus.
    /// </summary>
    public class ArtifactHotkeyListenerUpdater : KMonoBehaviour
    {
        private static ArtifactHotkeyListenerUpdater _instance;

        public static void Create()
        {
            if (_instance == null)
            {
                var go = new GameObject("ArtifactsPlus_HotkeyListenerUpdater");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<ArtifactHotkeyListenerUpdater>();
            }
            HLib.CustomLogger.Log("ArtifactHotkeyListenerUpdater.Create called");
        }

        void Update()
        {
            // Gather pressed keys for hotkey system
            var pressed = new List<string>();
            bool ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool f12Down = Input.GetKey(KeyCode.F12);
            bool f12JustPressed = Input.GetKeyDown(KeyCode.F12);

            // Example: Only add "Ctrl+F12" if Ctrl is held and F12 is pressed
            if (ctrlDown && f12Down)
                pressed.Add("Ctrl+F12");

            // Call the static hotkeyListener from ArtifactsPlusHotkeys
            if (ArtifactsPlusHotkeys.hotkeyListener != null)
            {
                ArtifactsPlusHotkeys.hotkeyListener.Update(pressed);
            }
            else
            {
                HLib.CustomLogger.Log("[ArtifactHotkeyListenerUpdater] ArtifactsPlusHotkeys.hotkeyListener is null.");
            }

            // Only log when Ctrl+F12 is actually pressed
            if (ctrlDown && f12JustPressed)
            {
                HLib.CustomLogger.Log("[ArtifactHotkeyListenerUpdater] Ctrl+F12 pressed (Input.GetKeyDown).");
            }
        }
    }

    /// <summary>
    /// Static hotkey listener and registration, matching SensorsPlus.
    /// </summary>
    public static class ArtifactsPlusHotkeys
    {
        public static HLib.HotkeyListener hotkeyListener;

        static ArtifactsPlusHotkeys()
        {
            HLib.CustomLogger.Log("ArtifactsPlus: ArtifactsPlusHotkeys static ctor loaded.");

            // Initialize and register hotkey(s)
            hotkeyListener = new HLib.HotkeyListener();
            hotkeyListener.RegisterHotkey("Ctrl+F12", () =>
            {
                HLib.CustomLogger.Log("[HOTKEY] Ctrl+F12 pressed: Example hotkey log from ArtifactsPlus.");
            });

            // Register for Unity update loop
            ArtifactHotkeyListenerUpdater.Create();
        }
    }

    /// <summary>
    /// Static class to ensure initialization.
    /// </summary>
    public static class ArtifactHotkeyListener
    {
        public static void OnLoad()
        {
            // Ensures static constructor runs
            var _ = typeof(ArtifactsPlusHotkeys);
        }
    }
}
