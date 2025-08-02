using HarmonyLib;
using UnityEngine;
using Klei.AI;

namespace CafePlus
{
    public partial class EspressoMachine : KMonoBehaviour, IStateMachineTarget, ISaveLoadable, IUniformGridObject, FewOptionSideScreen.IFewOptionSideScreen
    {
        public const float WATER_MASS_PER_USE = 1;
        public const float INGREDIENT_MASS_PER_USE = 0.5f;
        public static readonly Tag INGREDIENT_TAG = new Tag("Ingredient");

        // Use the static options array from your type signature context
        private static readonly FewOptionSideScreen.IFewOptionSideScreen.Option[] options = new[]
        {
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option1"),
                labelText = "Option 1",
                tooltipText = "First option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            },
            new FewOptionSideScreen.IFewOptionSideScreen.Option
            {
                tag = new Tag("Option2"),
                labelText = "Option 2",
                tooltipText = "Second option",
                iconSpriteColorTuple = new Tuple<UnityEngine.Sprite, UnityEngine.Color>(null, UnityEngine.Color.white)
            }
        };

        private Tag selectedOption = options[0].tag;

        public FewOptionSideScreen.IFewOptionSideScreen.Option[] GetOptions() => options;

        public void OnOptionSelected(FewOptionSideScreen.IFewOptionSideScreen.Option option)
        {
            selectedOption = option.tag;
            Debug.Log("[CafePlus] EspressoMachine: Selected " + option.labelText);
        }

        public Tag GetSelectedOption() => selectedOption;

        protected override void OnSpawn()
        {
            Debug.Log("[CafePlus] EspressoMachine OnSpawn called");
            base.OnSpawn();
        }

        public EspressoMachine() {
              Debug.Log("[CafePlus] EspressoMachine constructor called");
          }

        public class StatesInstance : GameStateMachine<States, StatesInstance, EspressoMachine, object>.GameInstance
        {
            public StatesInstance(EspressoMachine master) : base(master) { }
        }

        public class States : GameStateMachine<States, StatesInstance, EspressoMachine>
        {
            public override void InitializeStates(out BaseState default_state)
            {
                default_state = null; // Define your default state here
            }
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
}