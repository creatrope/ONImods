using HarmonyLib;
using KeyBindLogTest;
using PeterHan.PLib.Core;
using PeterHan.PLib.PatchManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeybindLogTest
{
    internal class Patches
    {

        public class Mod : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);
                PUtil.InitLibrary();
                Keybinder.KeyInputHandler.Register(new PPatchManager(harmony), HotKeys.All);
            }
        }
    }
}
