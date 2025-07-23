using UnityEngine;
using Klei;
using System.Text.RegularExpressions;

namespace OverheatControl
{
    public class BuildingTemperatureMonitor : MonoBehaviour
    {
        private Building building;
        private bool isOverheatable;
        private float lastCheckTime;

        public BuildingState State { get; set; }
        public float ShutdownTemperature { get; set; }
        public float RestoreTemperature { get; set; }

        private Color? originalTint = null;

        public void Initialize(Building building)
        {
            this.building = building;
            this.isOverheatable = building.Def.Overheatable;
            Overheatable component = building.GetComponent<Overheatable>();
            float num = (component != null ? component.OverheatTemperature : 10000f) - 273.15f;
            this.ShutdownTemperature = Mathf.Round(num * (Patches.ShutdownPercent / 100f));
            this.RestoreTemperature = Mathf.Round(num * (Patches.RestorePercent / 100f));
            string str = Regex.Replace(building.name, "[^a-zA-Z0-9]", "_");
            Patches.logger.LogDebug(string.Format("Activated {0}, Final Overheat: {1}, Shutdown: {2}, Restore: {3}", str, num, this.ShutdownTemperature, this.RestoreTemperature));
        }
        private void Update()
        {
            if ((UnityEngine.Object)GameClock.Instance != null)
            {
                float time = GameClock.Instance.GetTime();
                if ((double)time - (double)this.lastCheckTime < Patches.TemperatureCheckInterval)
                    return;
                this.lastCheckTime = time;
            }
            if (this.building == null)
                return;
            PrimaryElement component = this.building.gameObject.GetComponent<PrimaryElement>();
            if ((UnityEngine.Object)component == null)
                return;
            float num = component.Temperature - 273.15f;
            var animController = this.building.gameObject.GetComponent<KBatchedAnimController>();
            if (this.State == BuildingState.Active && num >= this.ShutdownTemperature)
            {
                Game.Instance.circuitManager.Disconnect(this.building.gameObject.GetComponent<IEnergyConsumer>(), false);
                this.State = BuildingState.Cooling;
                Patches.PopUpMessage("Shutdown", "Thermal Shutdown: Cooling!", this.building.gameObject);
                Patches.logger.LogDebug($"[OverheatControl] Cooling triggered for {building.name} at {num}°C (ShutdownThreshold: {ShutdownTemperature}°C)");
                // Visual change: tint building red
                if (animController != null)
                    animController.TintColour = Color.red;
            }
            else if (this.State == BuildingState.Cooling && num <= this.RestoreTemperature)
            {
                Game.Instance.circuitManager.Connect(this.building.gameObject.GetComponent<IEnergyConsumer>());
                this.State = BuildingState.Active;
                Patches.PopUpMessage("Restored", "Thermal Shutdown: Power Restored!", this.building.gameObject);
                Patches.logger.LogDebug($"[OverheatControl] Power restored for {building.name} at {num}°C (RestoreThreshold: {RestoreTemperature}°C)");
                // Visual change: reset tint to white
                if (animController != null)
                    animController.TintColour = Color.white;
            }
        }
    }
}