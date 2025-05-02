using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace ConveyorLoaderPatch
{
    public class ModLoad
    {
        public static void OnLoad()
        {
            Debug.Log("[ConveyorLoaderPatch] Mod loaded");
            new Harmony("com.yourname.conveyorloaderpatch").PatchAll();
        }
    }

    [HarmonyPatch(typeof(SolidConduitDispenser))]
    [HarmonyPatch("OnSpawn")]
    public static class SolidConduitDispenser_OnSpawn_Patch
    {
        public static void Postfix(SolidConduitDispenser __instance)
        {
            __instance.alwaysDispense = true;

            if (__instance.GetComponent<Storage>() != null)
            {
                __instance.gameObject.AddOrGet<ZeroOutStorageWhenAutomationOff>();
            }
        }
    }

    public class ZeroOutStorageWhenAutomationOff : KMonoBehaviour, ISim1000ms
    {
        private Operational op;
        private Storage storage;
        private float originalCapacity = -1f;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            op = GetComponent<Operational>();
            storage = GetComponent<Storage>();

            if (storage != null)
            {
                originalCapacity = storage.capacityKg;
                UpdateState();
            }
        }

        public void Sim1000ms(float dt) => UpdateState();

        private void UpdateState()
        {
            if (storage == null || op == null)
                return;

            bool isOn = op.IsOperational;

            if (!isOn && storage.capacityKg != 0f)
            {
                storage.capacityKg = 0f;
                storage.Trigger((int)GameHashes.OnStorageChange, null);
                Debug.Log($"[ConveyorLoaderPatch] Disabled loader input by zeroing capacity on {gameObject.name}");
            }
            else if (isOn && storage.capacityKg == 0f)
            {
                storage.capacityKg = originalCapacity;
                storage.Trigger((int)GameHashes.OnStorageChange, null);
                Debug.Log($"[ConveyorLoaderPatch] Re-enabled loader input by restoring capacity on {gameObject.name}");
            }
        }
    }
}
