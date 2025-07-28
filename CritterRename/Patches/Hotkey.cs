using HarmonyLib;
using HLib;
using Klei.AI;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Heinermann.CritterRename.Patches
{
    internal static class CritterRenameHotkeysPatch
    {
        public static void DeleteAllHatchEggs() { /* Implementation */ }
        public static void AssignUniqueNamesToAllEggs() { /* Implementation */ }
        public static void KillAllCrittersExceptHatches() { /* Implementation */ }
        public static void PrintAllHatchAndEggCritterNames() { /* Implementation */ }
    }

    internal sealed class CritterRenameKeybindHandler : IInputHandler
    {
        private static PAction DeleteAllHatchEggsAction;
        private static PAction AssignUniqueNamesToAllEggsAction;
        private static PAction KillAllCrittersExceptHatchesAction;
        private static PAction PrintAllHatchAndEggCritterNamesAction;

        private readonly Action deleteAllHatchEggsSnapshot;
        private readonly Action assignUniqueNamesToAllEggsSnapshot;
        private readonly Action killAllCrittersExceptHatchesSnapshot;
        private readonly Action printAllHatchAndEggCritterNamesSnapshot;

        public string handlerName => "CritterRenameKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal CritterRenameKeybindHandler()
        {
            deleteAllHatchEggsSnapshot = DeleteAllHatchEggsAction != null ? DeleteAllHatchEggsAction.GetKAction() : PAction.MaxAction;
            assignUniqueNamesToAllEggsSnapshot = AssignUniqueNamesToAllEggsAction != null ? AssignUniqueNamesToAllEggsAction.GetKAction() : PAction.MaxAction;
            killAllCrittersExceptHatchesSnapshot = KillAllCrittersExceptHatchesAction != null ? KillAllCrittersExceptHatchesAction.GetKAction() : PAction.MaxAction;
            printAllHatchAndEggCritterNamesSnapshot = PrintAllHatchAndEggCritterNamesAction != null ? PrintAllHatchAndEggCritterNamesAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(deleteAllHatchEggsSnapshot))
                CritterRenameHotkeysPatch.DeleteAllHatchEggs();
            if (e.TryConsume(assignUniqueNamesToAllEggsSnapshot))
                CritterRenameHotkeysPatch.AssignUniqueNamesToAllEggs();
            if (e.TryConsume(killAllCrittersExceptHatchesSnapshot))
                CritterRenameHotkeysPatch.KillAllCrittersExceptHatches();
            if (e.TryConsume(printAllHatchAndEggCritterNamesSnapshot))
                CritterRenameHotkeysPatch.PrintAllHatchAndEggCritterNames();
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new CritterRenameKeybindHandler(), 512);
        }

        internal static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(CritterRenameKeybindHandler));
            DeleteAllHatchEggsAction = new PActionManager().CreateAction(
                "CritterRename.DeleteAllHatchEggs", "Delete All Hatch Eggs", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
            AssignUniqueNamesToAllEggsAction = new PActionManager().CreateAction(
                "CritterRename.AssignUniqueNamesToAllEggs", "Assign Unique Names To All Eggs", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl));
            KillAllCrittersExceptHatchesAction = new PActionManager().CreateAction(
                "CritterRename.KillAllCrittersExceptHatches", "Kill All Critters Except Hatches", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl));
            PrintAllHatchAndEggCritterNamesAction = new PActionManager().CreateAction(
                "CritterRename.PrintAllHatchAndEggCritterNames", "Print All Hatch And Egg Critter Names", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl));
        }
    }
}