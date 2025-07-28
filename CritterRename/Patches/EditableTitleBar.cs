using HarmonyLib;
using UnityEngine;

namespace Heinermann.CritterRename.Patches
{
  [HarmonyPatch(typeof(EditableTitleBar), "OnEndEdit")]
  class EditableTitleBar_OnEndEdit
  {
    static void Prefix(ref string finalStr)
    {
      if (string.IsNullOrEmpty(finalStr))
      {
        finalStr = " ";
      }
    }

    static void Postfix(string finalStr)
    {
      if (finalStr.Trim().Length == 0)
      {
        DetailsScreen.Instance.RefreshTitle();
      }
    }
  }
}
