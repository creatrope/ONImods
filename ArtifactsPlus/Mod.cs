using HarmonyLib;
using KMod;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using System.Collections.Generic;
using UnityEngine;

namespace ArtifactsPlus
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            new POptions().RegisterOptions(this, typeof(ArtifactsPlusOptions)); // Register the options

            var options = ArtifactsPlusOptions.Instance;
            if (options != null)
            {
                string optionsJson = JsonConvert.SerializeObject(options, Formatting.Indented);
            }
            else
            {
                Debug.Log("[ArtifactsPlus] Options instance is null. Ensure ArtifactsPlusOptions is properly initialized.");
            }

            PUtil.InitLibrary();

            Patches.OnLoad();

            if (harmony == null)
            {
                Debug.Log("[ArtifactsPlus] Harmony instance is null.");
                return;
            }

            harmony.PatchAll();

            ArtifactStateTracker.LoadArtifactConfig(); // fallback to default

            Keybinder.KeyInputHandler.Register(new PPatchManager(harmony), HotKeys.All);
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            List<string> activeMods = new List<string>();
            foreach (KMod.Mod mod in mods)
            {
                if (mod.IsActive())
                {
                    activeMods.Add(mod.staticID);
                    // Set the flag if RoomsExpanded is present
                    if (mod.staticID == "pether-pg.RoomsExpanded")
                    {
                        Patches.IsRoomsExpandedPresent = true;
                        Patches.logger.LogDebug("[ArtifactsPlus] Detected pether-pg.RoomsExpanded mod. Setting IsRoomsExpandedPresent = true.");
                    }
                }
            }

            //CrossModManager.Initalize(activeMods);
        }
    }
}