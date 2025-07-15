using PeterHan.PLib.UI;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using HLib;
using OverheatControl;

namespace OverheatControl
{
    public class SimpleSideScreen : SideScreenContent
    {
        // Add a guard to prevent duplicate UI creation
        private bool uiInitialized = false;

        // Declare the panel variable
        private PPanel panel;

        // Declare the text field for displaying instance ID
        private TMP_Text idLocText;

        private StatusItem coolingStatusItem;

        public override bool IsValidForTarget(GameObject target)
        {
            Building component = target?.GetComponent<Building>();
            return component != null && Patches.IsOverheatableAndPowered(component.Def);
        }

        public override void SetTarget(GameObject target)
        {
            Building building = target?.GetComponent<Building>();
            if (building == null)
                return;
            ContentContainer = building.gameObject;
            BuildingTemperatureMonitor monitor = building.gameObject.GetComponent<BuildingTemperatureMonitor>();
            if (monitor == null)
                return;
            KSelectable selectable = building.gameObject.GetComponent<KSelectable>();
            if (selectable == null)
            {
                Patches.Logger.Log("[SideScreen] KSelectable component is null.");
            }
            else
            {
                selectable.GetProperName();
                StatusItem status_item = new StatusItem(
                    "OverheatControlMonitoring",
                    $"Thermal Monitoring (Shutdown {monitor.ShutdownTemperature}°C, Restore {monitor.RestoreTemperature}°C)",
                    "Is Thermal Monitoring Active?",
                    "status_item_icon",
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID);
                selectable.AddStatusItem(status_item);
                coolingStatusItem = new StatusItem(
                    "OverheatControl",
                    "Thermal shutdown triggered: cooling",
                    "thermal shutdown status",
                    "status_item_icon",
                    StatusItem.IconType.Exclamation,
                    NotificationType.Bad,
                    false,
                    OverlayModes.None.ID);
            }
        }

        public override string GetTitle() => "SideScreen";

        public override int GetSideScreenSortOrder() => -100;

        protected override void OnPrefabInit()
        {
            Patches.Logger.Log("[SimpleSideScreen] OnPrefabInit called.");
            base.OnPrefabInit();

            // Initialize the panel
            panel = new PPanel("Panel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Prevent duplicate UI creation
            if (uiInitialized)
                return;
            uiInitialized = true;

            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            // Add text fields for sensor output information
            var testlabel = new PLabel("test")
            {
                Text = "testlabel",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            panel.AddChild(testlabel);

            // Add a text field for displaying instance ID
            var idLabel = new PLabel("InstanceIDLabel")
            {
                Text = "Building: N/A, Instance ID: N/A",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(go =>
            {
                idLocText = go.transform.Find("Text")?.GetComponent<TMP_Text>();
                Patches.Logger.Log($"[SimpleSideScreen] OnRealize: idLocText assigned? {idLocText != null}");
            });

            panel.AddChild(idLabel);

            var panelgo = panel.Build();
            panelgo.transform.SetParent(container.transform, false);
            panelgo.transform.SetAsFirstSibling();
        }

        private void Update()
        {
            if (ContentContainer == null)
            {
                Patches.Logger.Log("[SideScreen] ContentContainer is null.");
            }
            else
            {
                Building building = ContentContainer.GetComponent<Building>();
                if (building == null)
                    Patches.Logger.Log("[SideScreen] Building component is null.");
                else if (coolingStatusItem == null)
                {
                    Patches.Logger.Log("[SideScreen] coolingStatusItem is null.");
                }
                else
                {
                    BuildingTemperatureMonitor monitor = building.gameObject.GetComponent<BuildingTemperatureMonitor>();
                    if (monitor == null)
                    {
                        Patches.Logger.Log("[SideScreen] BuildingTemperatureMonitor component is null.");
                    }
                    else
                    {
                        KSelectable selectable = building.gameObject.GetComponent<KSelectable>();
                        if (selectable == null)
                        {
                            Patches.Logger.Log("[SideScreen] KSelectable component is null.");
                        }
                        else
                        {
                            BuildingState state = monitor.State;
                            if (state == BuildingState.Cooling && !selectable.HasStatusItem(coolingStatusItem))
                            {
                                selectable.AddStatusItem(coolingStatusItem);
                            }
                            else
                            {
                                if (state == BuildingState.Cooling || !selectable.HasStatusItem(coolingStatusItem))
                                    return;
                                selectable.RemoveStatusItem(coolingStatusItem);
                            }
                        }
                    }
                }
            }
        }
    }

    public class BuildingTemperatureMonitor : MonoBehaviour
    {
        private Building building;
        private bool isOverheatable;
        private float lastCheckTime;

        public BuildingState State { get; set; }
        public float ShutdownTemperature { get; set; }
        public float RestoreTemperature { get; set; }

        public void Initialize(Building building)
        {
            this.building = building;
            this.isOverheatable = building.Def.Overheatable;
            Overheatable component = building.GetComponent<Overheatable>();
            float num = (component != null ? component.OverheatTemperature : 10000f) - 273.15f;
            this.ShutdownTemperature = Mathf.Round(num * (Patches.ShutdownPercent / 100f));
            this.RestoreTemperature = Mathf.Round(num * (Patches.RestorePercent / 100f));
            string str = System.Text.RegularExpressions.Regex.Replace(building.name, "[^a-zA-Z0-9]", "_");
            Patches.Logger.Log(string.Format("[OverheatControl] Activated {0}, Final Overheat: {1}, Shutdown: {2}, Restore: {3}", str, num, this.ShutdownTemperature, this.RestoreTemperature));
        }

        private void Update()
        {
            if ((UnityEngine.Object)GameClock.Instance != null)
            {
                float time = GameClock.Instance.GetTime();
                if ((double)time - (double)this.lastCheckTime < 5.0)
                    return;
                this.lastCheckTime = time;
            }
            if ((UnityEngine.Object)this.building == null)
                return;
            PrimaryElement component = this.building.gameObject.GetComponent<PrimaryElement>();
            if ((UnityEngine.Object)component == null)
                return;
            float num = component.Temperature - 273.15f;
            if (this.State == BuildingState.Active && num >= this.ShutdownTemperature)
            {
                Game.Instance.circuitManager.Disconnect(this.building.gameObject.GetComponent<IEnergyConsumer>(), false);
                this.State = BuildingState.Cooling;
                Patches.PopUpMessage("Shutdown", "Thermal Shutdown: Cooling!", this.building.gameObject);
            }
            else if (this.State == BuildingState.Cooling && num <= this.RestoreTemperature)
            {
                Game.Instance.circuitManager.Connect(this.building.gameObject.GetComponent<IEnergyConsumer>());
                this.State = BuildingState.Active;
                Patches.PopUpMessage("Restored", "Thermal Shutdown: Restored!", this.building.gameObject);
            }
        }
    }
}