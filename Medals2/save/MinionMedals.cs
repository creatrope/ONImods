using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Medals
{
    [Serializable]
    [AddComponentMenu("Medals/MinionMedals")]
    public class MinionMedals : KMonoBehaviour, ISaveLoadable, ISerializationCallbackReceiver
    {
        // Replace [Serialize] with [SerializeField] for Unity serialization
        [SerializeField]
        private List<MedalInfo> medals = new List<MedalInfo>();

        public IReadOnlyList<MedalInfo> Medals => medals;

        // Implement ISerializationCallbackReceiver methods (no override)
        public void OnBeforeSerialize()
        {
            Debug.Log($"[OnBeforeSerialize] Saving {medals.Count} medals for minion '{gameObject.name}'.");
            foreach (var medal in medals)
            {
                Debug.Log($"[OnBeforeSerialize] Saving medal: Name='{medal.Name}', Desc='{medal.Description}', Repeatable={medal.IsRepeatable}");
            }
        }

        public void OnAfterDeserialize()
        {
            Debug.Log($"[OnAfterDeserialize] Loaded {medals.Count} medals for minion '{gameObject.name}'.");
            foreach (var medal in medals)
            {
                Debug.Log($"[OnAfterDeserialize] Loaded medal: Name='{medal.Name}', Desc='{medal.Description}', Repeatable={medal.IsRepeatable}");
            }
        }

        public static void AddMedalToMinion(string minionName, MedalData medalData)
        {
            var minion = Components.MinionIdentities?.Items?.FirstOrDefault(m => m.GetProperName() == minionName);
            if (minion == null)
            {
                Debug.Log($"[AddMedalToMinion] Minion '{minionName}' not found.");
                return;
            }

            // Get or add MedalInfo component
            var medalInfo = minion.GetComponent<MedalInfo>();
            if (medalInfo == null)
                medalInfo = minion.gameObject.AddComponent<MedalInfo>();

            // Only award once if not repeatable
            if (!medalData.IsRepeatable && medalInfo.Medals.Any(m => m.Name == medalData.Name))
            {
                Debug.Log($"[AddMedalToMinion] Minion '{minionName}' already has non-repeatable medal '{medalData.Name}'.");
                return;
            }

            medalInfo.Medals.Add(medalData);
            Debug.Log($"[AddMedalToMinion] Added medal '{medalData.Name}' to minion '{minionName}'.");

            // Always create a fresh keepsake prefab for this medal
            //MedalsManager.CreateAndSpawnKeepsakeForMedal(minion, medalData);
        }
        public void AddMedal(MedalInfo medal)
        {
            if (medal == null)
                return;
            medals.Add(medal);
            Debug.Log($"[AddMedal] Adding medal '{medal.Name}' to list. Total medals now: {medals.Count}");
        }
        public bool HasMedal(MedalInfo medal)
        {
            if (medal == null)
                return false;
            // Checks for reference equality or matching name/description/repeatable
            return medals.Contains(medal) ||
                   medals.Exists(m =>
                       m.Name == medal.Name &&
                       m.Description == medal.Description &&
                       m.IsRepeatable == medal.IsRepeatable);
        }
        public void ClearAllMedals()
        {
            medals.Clear();
            Debug.Log($"[ClearAllMedals] Cleared all medals from minion '{gameObject.name}'.");
        }

        // New method to add MedalInfo component directly
        public static void AddMedalInfoToMinion(string minionName, string name, string description, bool isRepeatable)
        {
            var minion = Components.MinionIdentities?.Items?.FirstOrDefault(m => m.GetProperName() == minionName);
            if (minion == null)
            {
                Debug.Log($"[AddMedalInfoToMinion] Minion '{minionName}' not found.");
                return;
            }

            // Add or get MedalInfo component
            var medalInfo = minion.gameObject.AddComponent<MedalInfo>();
            medalInfo.Name = name;
            medalInfo.Description = description;
            medalInfo.IsRepeatable = isRepeatable;
            Debug.Log($"[AddMedalInfoToMinion] Added MedalInfo to minion '{minionName}': Name='{name}', Desc='{description}', Repeatable={isRepeatable}");
        }
    }
}