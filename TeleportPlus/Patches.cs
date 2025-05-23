// Decompiled with JetBrains decompiler
// Type: WarpPortal
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 5D1EF2B8-AA4E-4702-B74E-0141C7D16DB1
// Assembly location: C:\Users\sendh\Documents\GitHub\Sendhb-ONI\lib\Assembly-CSharp.dll
// Metadata token values are shown

using Klei.AI;
using KSerialization;
using STRINGS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Reflection.Emit;

namespace TeleportPlus
{
    public static class TeleportConfig
    {
        // Set your desired recharge time here (in seconds)
        public static float WarpPortalRechargeTime = 5f;
    }

    public class Patches
    {
        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                Debug.Log("[TeleportPlus] installed!");
            }
        }

        // Transpiler to replace the hardcoded 3000f recharge time in WarpPortal's state machine
        [HarmonyPatch]
        public static class WarpPortalSM_RechargeTime_Transpiler
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                // Target the Update lambda in WarpPortal.WarpPortalSM.InitializeStates
                var smType = AccessTools.Inner(typeof(WarpPortal), "WarpPortalSM");
                // The lambda is usually named <InitializeStates>b__8_12
                return AccessTools.Method(smType, "<InitializeStates>b__8_12");
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    // Replace ldc.r4 3000 with our config value
                    if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 3000f)
                    {
                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TeleportConfig), nameof(TeleportConfig.WarpPortalRechargeTime)));
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
        }
    }
}
