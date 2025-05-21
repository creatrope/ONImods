using System.Collections.Generic;

namespace ArtifactsPlus
{
    // Attach this to each artifact instance to track what it changed and to whom
    public class ArtifactEffectTracker
    {
        // Tracks which minion (by id) had which attribute modified and by how much
        // Key: minion unique id (string), Value: attributeId -> value
        public Dictionary<string, Dictionary<string, float>> MinionAttributeModifiers { get; } = new Dictionary<string, Dictionary<string, float>>();

        // Call this when applying an attribute modifier to a minion
        public void ApplyAttributeModifier(string minionId, string attributeId, float value, System.Action<string, string, float> applyEffect)
        {
            if (!MinionAttributeModifiers.TryGetValue(minionId, out var attrDict))
            {
                attrDict = new Dictionary<string, float>();
                MinionAttributeModifiers[minionId] = attrDict;
            }
            // Debug log before applying effect
            CustomLogger.Log($"[DEBUG] Applying attribute modifier: MinionId={minionId}, Attribute={attributeId}, Value={value}");
            // Apply the effect
            applyEffect(minionId, attributeId, value);
            attrDict[attributeId] = value;
        }

        // Call this when reversing all effects for a minion (e.g., artifact deactivation)
        public void ReverseAllForMinion(string minionId, System.Action<string, string, float> reverseEffect)
        {
            if (MinionAttributeModifiers.TryGetValue(minionId, out var attrDict))
            {
                foreach (var kvp in attrDict)
                {
                    // Debug log before reversing effect
                    CustomLogger.Log($"[DEBUG] Reversing attribute modifier: MinionId={minionId}, Attribute={kvp.Key}, Value={kvp.Value}");
                    // Reverse the effect
                    reverseEffect(minionId, kvp.Key, kvp.Value);
                }
                MinionAttributeModifiers.Remove(minionId);
            }
        }

        // Get all attribute modifiers for a minion
        public Dictionary<string, float> GetModifiersForMinion(string minionId)
        {
            MinionAttributeModifiers.TryGetValue(minionId, out var attrDict);
            return attrDict;
        }

        // Call this to clear all tracking (e.g., when artifact is destroyed)
        public void ClearAll()
        {
            MinionAttributeModifiers.Clear();
        }
    }
}