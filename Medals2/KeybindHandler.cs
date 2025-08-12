using PeterHan.PLib.Actions;
using PeterHan.PLib.PatchManager;
using UnityEngine;
using System.Linq;
using System;

namespace Medals
{
    internal sealed class KeybindHandler : IInputHandler
    {
        internal static PAction incapacitateAction;
        internal static PAction damageAction;
        internal static PAction createRandomMedalAction;
        private Action incapacitateSnapshotAction;
        private Action damageSnapshotAction;
        private Action createRandomMedalSnapshotAction;
        private float lastSnapshotTime = 0f;
        private readonly float debounceInterval = 1.0f; // seconds

        public string handlerName => "KeybindHandler";
        public KInputHandler inputHandler { get; set; }
        internal static MinionIdentity SelectedMinion;
        internal static PAction eraseMedalsAction;
        private Action eraseMedalsSnapshotAction;

        private bool keyIsDown = false;

        public KeybindHandler()
        {
            incapacitateSnapshotAction = incapacitateAction != null ? incapacitateAction.GetKAction() : PAction.MaxAction;
            damageSnapshotAction = damageAction != null ? damageAction.GetKAction() : PAction.MaxAction;
            eraseMedalsSnapshotAction = eraseMedalsAction != null ? eraseMedalsAction.GetKAction() : PAction.MaxAction;
            createRandomMedalSnapshotAction = createRandomMedalAction != null ? createRandomMedalAction.GetKAction() : PAction.MaxAction;
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (keyIsDown)
                return; // Ignore repeated keydown until keyup

            keyIsDown = true;

            float now = Time.time;

            bool incapacitatePressed = e.TryConsume(incapacitateSnapshotAction);
            bool damagePressed = e.TryConsume(damageSnapshotAction);
            bool eraseMedalsPressed = e.TryConsume(eraseMedalsSnapshotAction);
            bool createRandomMedalPressed = e.TryConsume(createRandomMedalSnapshotAction);

            // Debounce: Only allow one hotkey action per debounceInterval
            if ((damagePressed || incapacitatePressed || eraseMedalsPressed || createRandomMedalPressed) && now - lastSnapshotTime < debounceInterval)
                return;

            if (damagePressed)
            {
                lastSnapshotTime = now;
                HandleDamageHotkey();
            }
            else if (incapacitatePressed)
            {
                lastSnapshotTime = now;
                HandleIncapacitateHotkey();
            }
            else if (eraseMedalsPressed)
            {
                lastSnapshotTime = now;
                HandleEraseMedalsHotkey();
            }
            else if (createRandomMedalPressed)
            {
                lastSnapshotTime = now;
                OnCreateRandomMedal();
            }
        }

        // Add this method to handle key up events
        public void OnKeyUp(KButtonEvent e)
        {
            keyIsDown = false;
        }

        private static bool handlerRegistered = false;

        [PLibMethod(RunAt.AfterLayerableLoad)]
        public static void AddKeycodeHandler()
        {
            if (!handlerRegistered)
            {
                KInputHandler.Add(Global.GetInputManager().GetDefaultController(),
                    new KeybindHandler(), 512);
                handlerRegistered = true;
            }
        }

        public static void Register(PPatchManager manager)
        {
            manager.RegisterPatchClass(typeof(KeybindHandler));
            incapacitateAction = new PActionManager().CreateAction(
                "Medals.incapacitateAction", "Incapacitate", new PKeyBinding(KKeyCode.F5, Modifier.Ctrl));
            damageAction = new PActionManager().CreateAction(
                "Medals.damageAction", "Damage", new PKeyBinding(KKeyCode.F4, Modifier.Ctrl));
            eraseMedalsAction = new PActionManager().CreateAction(
                "Medals.eraseMedalsAction", "Erase All Medals", new PKeyBinding(KKeyCode.F6, Modifier.Ctrl));
            createRandomMedalAction = new PActionManager().CreateAction(
                "Medals.createRandomMedalAction", "Create Random Medal", new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
        }

        // --- SUPPORT FUNCTIONS BELOW ---

        private void HandleDamageHotkey()
        {
            Debug.Log("[OnKeyDown] Damage hotkey detected.");
            if (SelectedMinion != null)
            {
                var health = SelectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    float damageAmount = 10f;
                    Debug.Log($"[OnKeyDown] (before health.Damage) Damaged '{SelectedMinion.GetProperName()}' for {damageAmount} HP via hotkey.");
                    health.Damage(damageAmount);
                    Debug.Log($"[OnKeyDown] (after health.Damage).");
                }
                else
                {
                    Debug.Log("[OnKeyDown] Health component not found on selected minion.");
                }
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        private void HandleIncapacitateHotkey()
        {
            Debug.Log("[OnKeyDown] Incapacitate hotkey detected.");
            if (SelectedMinion != null)
            {
                var health = SelectedMinion.GetComponent<Health>();
                if (health != null)
                {
                    Debug.Log($"[OnKeyDown] Health component found. Can be incapacitated: {health.canBeIncapacitated}, IsIncapacitated: {health.IsIncapacitated()}");
                    if (health.canBeIncapacitated && !health.IsIncapacitated())
                    {
                        health.Incapacitate(new Tag("ManualIncapacitate"));
                        Debug.Log($"[OnKeyDown] Incapacitated '{SelectedMinion.GetProperName()}' via hotkey.");
                        SelectedMinion = null;
                    }
                    else
                    {
                        Debug.Log("[OnKeyDown] Minion cannot be incapacitated or is already incapacitated.");
                    }
                }
                else
                {
                    Debug.Log("[OnKeyDown] Health component not found on selected minion.");
                }
            }
            else
            {
                Debug.Log("[OnKeyDown] No minion selected.");
            }
        }

        private void HandleEraseMedalsHotkey()
        {
            Debug.Log("[EraseMedals] Erase medals hotkey pressed. Removing all medals, effects, keepsakes, and minions.");

            // Remove keepsake medal objects from the scene
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("keepsake_medal"))
                {
                    UnityEngine.Object.Destroy(go);
                }
            }

            // Remove medal-related prefabs from the prefab map
            if (Assets.Prefabs != null)
            {
                var medalPrefabs = Assets.Prefabs
                    .Where(p => p != null && p.name.StartsWith("keepsake_medal"))
                    .ToList();

                foreach (var prefab in medalPrefabs)
                {
                    Assets.Prefabs.Remove(prefab);
                    Debug.Log($"[EraseMedals] Removed prefab '{prefab.name}' from prefab map.");
                }
            }
        }

        private static void OnCreateRandomMedal()
        {
            if (SelectedMinion == null)
            {
                Debug.Log("[Medals] No minion selected for medal assignment.");
                return;
            }

            var medalInfo = SelectedMinion.GetComponent<MedalInfo>();
            if (medalInfo == null)
            {
                medalInfo = SelectedMinion.gameObject.AddComponent<MedalInfo>();
            }

            // Generate random medal parameters
            var rand = new System.Random();
            string name = "Medal " + rand.Next(1000, 9999);
            string desc = "Randomly generated medal #" + rand.Next(1000, 9999);
            MedalType type = (MedalType)rand.Next(Enum.GetValues(typeof(MedalType)).Length);
            bool repeatable = rand.Next(0, 2) == 0;

            var medal = MedalInfo.CreateMedal(name, desc, type, repeatable);
            medalInfo.Medals.Add(medal);

            Debug.Log($"[Medals] Assigned random medal '{name}' to {SelectedMinion.GetProperName()}.");
        }
    }
}