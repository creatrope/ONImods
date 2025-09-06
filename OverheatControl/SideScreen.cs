using UnityEngine;
using System.Runtime.CompilerServices;
using HLib;
using OverheatControl;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using PeterHan.PLib.UI;
using TMPro;

namespace OverheatControl
{
    public class SimpleSideScreen : SideScreenContent
    {
        private bool uiInitialized = false;
        private PPanel panel;
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
                Patches.logger.LogDebug("SideScreen KSelectable component is null.");
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
                    "status_item_exclamation",
                    StatusItem.IconType.Info, // Try Info instead of Exclamation
                    NotificationType.Bad,
                    false,
                    OverlayModes.None.ID);
            }
        }

        public override string GetTitle() => "SideScreen";

        public override int GetSideScreenSortOrder() => -100;

        public override void OnPrefabInit()
        {
            Patches.logger.LogDebug("SimpleSideScreen OnPrefabInit called.");
            base.OnPrefabInit();

            panel = new PPanel("Panel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10
            };
        }

        public override void OnSpawn()
        {
            base.OnSpawn();

            if (uiInitialized)
                return;
            uiInitialized = true;

            GameObject container = ContentContainer != null ? ContentContainer : gameObject;

            var testlabel = new PLabel("test")
            {
                Text = "testlabel",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            };

            panel.AddChild(testlabel);

            var idLabel = new PLabel("InstanceIDLabel")
            {
                Text = "Building: N/A, Instance ID: N/A",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(go =>
            {
                idLocText = go.transform.Find("Text")?.GetComponent<TMP_Text>();
                Patches.logger.LogDebug($"SimpleSideScreen OnRealize: idLocText assigned? {idLocText != null}");
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
                Patches.logger.LogDebug("SideScreen ContentContainer is null.");
            }
            else
            {
                Building building = ContentContainer.GetComponent<Building>();
                if (building == null)
                    Patches.logger.LogDebug("SideScreen Building component is null.");
                else if (coolingStatusItem == null)
                {
                    Patches.logger.LogDebug("SideScreen coolingStatusItem is null.");
                }
                else
                {
                    BuildingTemperatureMonitor monitor = building.gameObject.GetComponent<BuildingTemperatureMonitor>();
                    if (monitor == null)
                    {
                        Patches.logger.LogDebug("SideScreen BuildingTemperatureMonitor component is null.");
                    }
                    else
                    {
                        KSelectable selectable = building.gameObject.GetComponent<KSelectable>();
                        if (selectable == null)
                        {
                            Patches.logger.LogDebug("SideScreen KSelectable component is null.");
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
}