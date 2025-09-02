using System.Collections.Generic;
using UnityEngine;
using HLib;

namespace RocketSpeed
{
    internal static class HotKeys
    {
        public static void Warp()
        {
            foreach (var traveler in UnityEngine.Object.FindObjectsOfType<ClusterTraveler>())
            {
                if (traveler.IsTraveling())
                {
                    while (traveler.IsTraveling())
                    {
                        traveler.AdvancePathOneStep();
                    }
                    Debug.Log("[RocketSpeed] Instantly moved rocket to destination.");
                }
            }
        }

        public static readonly List<Keybinder.KeybindDef> All = new List<Keybinder.KeybindDef>
        {
            new Keybinder.KeybindDef
            {
                Id = "RocketSpeed.Warp",
                DisplayName = "Warp To Destination",
                Key = KKeyCode.F1,
                Modifiers = Modifier.Ctrl | Modifier.Shift,
                Handler = Warp
            }
        };
    }
}