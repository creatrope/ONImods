using HarmonyLib;
using UnityEngine;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using Heinermann.CritterRename.Patches;

namespace Heinermann.CritterRename
{
    public class CritterRename : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            PUtil.InitLibrary();
            // remove the hotkeys except during development
            //CritterRenameKeybindHandler.Register(new PPatchManager(harmony));
        }
    }
}
