using HarmonyLib;
using UnityEngine;

public partial class EspressoMachine : KMonoBehaviour, IStateMachineTarget, ISaveLoadable, IUniformGridObject, FewOptionSideScreen.IFewOptionSideScreen
{
    public const float WATER_MASS_PER_USE = 1;
    public const float INGREDIENT_MASS_PER_USE = 0.5f; // Define INGREDIENT_MASS_PER_USE
    public static readonly Tag INGREDIENT_TAG = new Tag("Ingredient"); // Define INGREDIENT_TAG

    private Tag selectedOption = new Tag("Option1");

    public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions()
    {
        Debug.Log("[CafePlus] GetOptions called on EspressoMachine");
        return new FewOptionSideScreen.IFewOptionSideScreen.Option[]
        {
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option1"),
                labelText = "Option 1",
                tooltipText = "Tooltip for Option 1",
                iconSpriteColorTuple = null // You can set an icon if desired
            }
        };
    }

    public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
    {
        Debug.Log($"[CafePlus] OnOptionSelected called with tag: {option.tag}");
        selectedOption = option.tag;
    }

    public Tag GetSelectedOption()
    {
        Debug.Log($"[CafePlus] GetSelectedOption called, returning: {selectedOption}");
        return selectedOption;
    }

    protected override void OnSpawn()
    {
        Debug.Log("[CafePlus] EspressoMachine OnSpawn called");
        base.OnSpawn();
    }

    public EspressoMachine() {
          Debug.Log("[CafePlus] EspressoMachine constructor called");
      }
}

[HarmonyPatch(typeof(EspressoMachineConfig), "ConfigureBuildingTemplate")]
public static class EspressoMachineConfig_ConfigureBuildingTemplate_AnyLiquidPatch
{
    static void Postfix(GameObject go, Tag prefab_tag)
    {
        // ...other setup...
        Debug.Log("[CafePlus] called AddOrGet<EspressoMachine>();");
        var comp = go.AddOrGet<EspressoMachine>();
        Debug.Log("[CafePlus] EspressoMachine type on prefab: " + comp.GetType().AssemblyQualifiedName);
    }
}