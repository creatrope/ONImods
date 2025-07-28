using Database;
using HarmonyLib;
using HLib;
using KMod;
using KSerialization;
using Newtonsoft.Json;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TUNING;
using UnityEngine;
using System.Reflection;
using SerializeTes; // Add this at the top if HarmonyDebugUtils is in the SerializeTes namespace


namespace SerializeTes
{
    internal sealed partial class MinimalKeybindHandler : IInputHandler
    {
        private static PAction DeleteCrittersAction;
        private static PAction AssignComponentAction;
        private static PAction PrintComponentJsonAction; // New action
        private static PAction PrintEggConfigPatchesAction; // Add this field
        private readonly Action deleteCrittersSnapshotAction;
        private readonly Action assignComponentSnapshotAction;
        private readonly Action printComponentJsonSnapshotAction; // New action field

        public string handlerName => "MinimalKeybindHandler";
        public KInputHandler inputHandler { get; set; }

        internal MinimalKeybindHandler()
        {
            Debug.Log("[MinimalKeybindHandler] Constructor called.");
            deleteCrittersSnapshotAction = DeleteCrittersAction != null ? DeleteCrittersAction.GetKAction() : PAction.MaxAction;
            assignComponentSnapshotAction = AssignComponentAction != null ? AssignComponentAction.GetKAction() : PAction.MaxAction;
            printComponentJsonSnapshotAction = PrintComponentJsonAction != null ? PrintComponentJsonAction.GetKAction() : PAction.MaxAction;
            Debug.Log($"[MinimalKeybindHandler] deleteCrittersSnapshotAction: {deleteCrittersSnapshotAction}, assignComponentSnapshotAction: {assignComponentSnapshotAction}, printComponentJsonSnapshotAction: {printComponentJsonSnapshotAction}");
        }

        public void OnKeyDown(KButtonEvent e)
        {
            Debug.Log($"[MinimalKeybindHandler] OnKeyDown called. KeyCode: {e.GetAction()}, Ctrl: {e.Controller.GetKeyDown(KKeyCode.LeftControl) || e.Controller.GetKeyDown(KKeyCode.RightControl)}, Alt: {e.Controller.GetKeyDown(KKeyCode.LeftAlt) || e.Controller.GetKeyDown(KKeyCode.RightAlt)}, Shift: {e.Controller.GetKeyDown(KKeyCode.LeftShift) || e.Controller.GetKeyDown(KKeyCode.RightShift)}");
            if (e.TryConsume(deleteCrittersSnapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] DeleteCritters hotkey pressed!");
                DeleteAllCrittersAndEggs();
                Debug.Log("[MinimalKeybindHandler] All critters and eggs deleted!");
            }
            else if (e.TryConsume(assignComponentSnapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] AssignComponent hotkey pressed!");
                AssignValuesToCrittersAndEggs(); // Correct method name
            }
            else if (e.TryConsume(printComponentJsonSnapshotAction))
            {
                Debug.Log("[MinimalKeybindHandler] PrintComponentJson hotkey pressed!");
                PrintAllCrittersAndEggsComponentJson();
            }
            else
            {
                Debug.Log("[MinimalKeybindHandler] No matching hotkey consumed.");
            }
        }

        [PLibMethod(RunAt.AfterLayerableLoad)]
        internal static void AddKeycodeHandler()
        {
            Debug.Log("[MinimalKeybindHandler] AddKeycodeHandler called.");
            KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                new MinimalKeybindHandler(), 512);
            Debug.Log("[MinimalKeybindHandler] Handler added to input manager.");
        }

        internal static void Register(PPatchManager manager)
        {
            Debug.Log("[MinimalKeybindHandler] Register called.");
            manager.RegisterPatchClass(typeof(MinimalKeybindHandler));
            DeleteCrittersAction = new PActionManager().CreateAction(
                "SerializeTes.DeleteCrittersAction", "Delete All Critters and Eggs", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
            AssignComponentAction = new PActionManager().CreateAction(
                "SerializeTes.AssignComponentAction", "Assign Unique Data to MyComponent to Critters/Eggs", new PKeyBinding(KKeyCode.F8, Modifier.Ctrl));
            PrintComponentJsonAction = new PActionManager().CreateAction(
                "SerializeTes.PrintComponentJsonAction", "Print Critter/Egg MyComponent JSON", new PKeyBinding(KKeyCode.F9, Modifier.Ctrl));
            PrintEggConfigPatchesAction = new PActionManager().CreateAction(
                "SerializeTes.PrintEggConfigPatchesAction", "Print EggConfig Harmony Patches", new PKeyBinding(KKeyCode.F10, Modifier.Ctrl)); // New hotkey
            Debug.Log("[MinimalKeybindHandler] Actions registered: DeleteCrittersAction, AssignComponentAction, PrintComponentJsonAction, PrintEggConfigPatchesAction.");
        }

        // Function to delete all critters and eggs in the world
        private static void DeleteAllCrittersAndEggs()
        {
            Debug.Log("[MinimalKeybindHandler] DeleteAllCrittersAndEggs called.");
            var allGameObjects = UnityEngine.Object.FindObjectsOfType<KPrefabID>();
            int critterCount = 0;
            int eggCount = 0;
            int skipped = 0;
            foreach (var obj in allGameObjects)
            {
                if (obj == null || obj.gameObject == null)
                {
                    skipped++;
                    continue;
                }

                if (obj.HasTag(GameTags.Creature) && !obj.HasTag(GameTags.BaseMinion))
                {
                    Debug.Log($"[MinimalKeybindHandler] Deleting critter: {obj.PrefabTag.Name} (id={obj.gameObject.GetInstanceID()})");
                    UnityEngine.Object.Destroy(obj.gameObject);
                    critterCount++;
                }
                else if (obj.HasTag(GameTags.Egg))
                {
                    Debug.Log($"[MinimalKeybindHandler] Deleting egg: {obj.PrefabTag.Name} (id={obj.gameObject.GetInstanceID()})");
                    UnityEngine.Object.Destroy(obj.gameObject);
                    eggCount++;
                }
            }
            Debug.Log($"[MinimalKeybindHandler] DeleteAllCrittersAndEggs finished. Critters deleted: {critterCount}, Eggs deleted: {eggCount}, Skipped: {skipped}, Total checked: {allGameObjects.Length}");
        }

        private static void AssignValuesToCrittersAndEggs()
        {
            Debug.Log("[MinimalKeybindHandler] AssignValuesToCrittersAndEggs called.");
            var allGameObjects = UnityEngine.Object.FindObjectsOfType<KPrefabID>();
            int critterCount = 0;
            int eggCount = 0;
            int assigned = 0;
            var rng = new System.Random();

            // Simple mnemonic word lists (expand as desired)
            string[] adjectives = { "Brave", "Clever", "Swift", "Mighty", "Happy", "Fuzzy", "Tiny", "Lively", "Gentle", "Wild" };
            string[] nouns = { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma", "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega" };

            foreach (var obj in allGameObjects)
            {
                if (obj == null || obj.gameObject == null)
                {
                    Debug.Log("[MinimalKeybindHandler] Skipped null object.");
                    continue;
                }

                if ((obj.HasTag(GameTags.Creature) && !obj.HasTag(GameTags.BaseMinion)) || obj.HasTag(GameTags.Egg))
                {
                    var comp = obj.gameObject.AddOrGet<MyComponent>();
                    // Generate a mnemonic string: AdjectiveNoun-#####
                    string adjective = adjectives[rng.Next(adjectives.Length)];
                    string noun = nouns[rng.Next(nouns.Length)];
                    int number = rng.Next(1000, 10000);
                    comp.MyString = $"{adjective}{noun}-{number}";
                    comp.MyInteger = rng.Next(int.MinValue, int.MaxValue);

                    Debug.Log($"[MinimalKeybindHandler] MyComponent present/added to {obj.PrefabTag.Name} (id={obj.gameObject.GetInstanceID()}>): MyString='{comp.MyString}', MyInteger={comp.MyInteger}");
                    if (obj.HasTag(GameTags.Creature) && !obj.HasTag(GameTags.BaseMinion))
                        critterCount++;
                    else if (obj.HasTag(GameTags.Egg))
                        eggCount++;
                    assigned++;
                }
            }
            Debug.Log($"[MinimalKeybindHandler] AssignValuesToCrittersAndEggs finished. Critters assigned: {critterCount}, Eggs assigned: {eggCount}, Total assigned: {assigned}, Total checked: {allGameObjects.Length}");

            // Print all critters and eggs component JSON after assignment
            PrintAllCrittersAndEggsComponentJson();
        }

        private static void PrintAllCrittersAndEggsComponentJson()
        {
            var allGameObjects = UnityEngine.Object.FindObjectsOfType<KPrefabID>();
            foreach (var obj in allGameObjects)
            {
                if (obj == null || obj.gameObject == null)
                    continue;

                if ((obj.HasTag(GameTags.Creature) && !obj.HasTag(GameTags.BaseMinion)) || obj.HasTag(GameTags.Egg))
                {
                    string json = MyComponentUtils.GetMyComponentJson(obj.gameObject);
                    if (json != null)
                    {
                        Debug.Log($"{obj.gameObject.name} ({obj.gameObject.GetInstanceID()}) {json}");
                    }
                }
            }
        }
    }

    public class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("[Mod] OnLoad called.");
            base.OnLoad(harmony);
            Harmony.DEBUG = true;
            harmony.PatchAll();
            PUtil.InitLibrary();
            MinimalKeybindHandler.Register(new PPatchManager(harmony));
            Debug.Log("[Mod] OnLoad finished.");
            EggConfigPatches.PrintEggConfigPatches(harmony);
        }
    }

    public class MyClass
    {
        [Serializable]
        [JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class MyComponent
        {
            [JsonProperty]
            public string MyString;
            [JsonProperty]
            public int MyInteger;
        }
    }

    [HarmonyPatch(typeof(KPrefabID), "OnPrefabInit")]
    public static class KPrefabID_OnPrefabInit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(KPrefabID __instance)
        {
            if (__instance == null || __instance.gameObject == null)
            {
                Debug.Log("[KPrefabID_OnPrefabInit_Patch] __instance or gameObject is null, skipping.");
                return;
            }

            //Debug.Log($"[KPrefabID_OnPrefabInit_Patch] OnPrefabInit for {__instance.gameObject.name} (id={__instance.gameObject.GetInstanceID()})");

            // Only add to critter and egg prefabs (not minions)
            if ((__instance.HasTag(GameTags.Creature) && !__instance.HasTag(GameTags.BaseMinion)) ||
                __instance.HasTag(GameTags.Egg))
            {
                Debug.Log($"[KPrefabID_OnPrefabInit_Patch] Adding MyComponent to {__instance.gameObject.name} (id={__instance.gameObject.GetInstanceID()})");
                MyComponentPatcherUtils.AddMyComponent(__instance.gameObject);
            }
            else
            {
                //Debug.Log($"[KPrefabID_OnPrefabInit_Patch] Skipped {__instance.gameObject.name} (id={__instance.gameObject.GetInstanceID()}) - not a critter or egg.");
            }
        }
    }

    public class MyComponent : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        public string MyString;

        [Serialize]
        public int MyInteger;

        public MyClass.MyComponent Data
        {
            get => new MyClass.MyComponent { MyString = MyString, MyInteger = MyInteger };
            set
            {
                if (value != null)
                {
                    MyString = value.MyString;
                    MyInteger = value.MyInteger;
                }
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Debug.Log($"[MyComponent] OnSpawn called for {gameObject.name} (id={gameObject.GetInstanceID()}). MyString='{MyString}', MyInteger={MyInteger}");
        }
    }

    public static class MyComponentUtils
    {
        public static string GetMyComponentJson(GameObject go)
        {
            if (go == null)
            {
                Debug.Log("[MyComponentUtils] GetMyComponentJson called with null GameObject.");
                return null;
            }

            var comp = go.GetComponent<MyComponent>();
            if (comp == null || comp.Data == null)
            {
                Debug.Log($"[MyComponentUtils] MyComponent missing on {go.name} (id={go.GetInstanceID()}). Returning null.");
                return null;
            }

            string json = JsonConvert.SerializeObject(comp.Data, Formatting.Indented);
            //Debug.Log($"[MyComponentUtils] Serialized MyComponent for {go.name} (id={go.GetInstanceID()}): {json}");
            return json;
        }
    }

    internal static class MyComponentPatcherUtils
    {
        public static void AddMyComponent(GameObject go)
        {
            if (go == null)
            {
                Debug.Log("[MyComponentPatcherUtils] AddMyComponent called with null GameObject.");
                return;
            }

            var existing = go.GetComponent<MyComponent>();
            if (existing == null)
            {
                var comp = go.AddOrGet<MyComponent>();
                comp.MyString = "Default";
                comp.MyInteger = 0;
                Debug.Log($"[MyComponentPatcherUtils] MyComponent added to {go.name} (id={go.GetInstanceID()}) with default values.");
            }
            else
            {
                Debug.Log($"[MyComponentPatcherUtils] MyComponent already present on {go.name} (id={go.GetInstanceID()}).");
            }
        }
    }
}
