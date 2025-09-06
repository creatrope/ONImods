using KSerialization;
using STRINGS;
using UnityEngine;
using System;
using Newtonsoft.Json;
using HarmonyLib;

namespace Heinermann.CritterRename
{
    public class CritterName : KMonoBehaviour, ISaveLoadable
    {
        [SerializeField]
        [Serialize]
        private string critterName = "";

        [SerializeField]
        [Serialize]
        private uint generation = 1;

        private static readonly EventSystem.IntraObjectHandler<CritterName> OnSpawnedFromDelegate =
            new EventSystem.IntraObjectHandler<CritterName>(OnSpawnedFrom);

        private static readonly EventSystem.IntraObjectHandler<CritterName> OnLayEggDelegate =
            new EventSystem.IntraObjectHandler<CritterName>(OnLayEgg);

        public override void OnPrefabInit()
        {
            Subscribe((int)GameHashes.SpawnedFrom, OnSpawnedFromDelegate);
            Subscribe((int)GameHashes.LayEgg, OnLayEggDelegate);
        }

        public override void OnSpawn()
        {
            if (!string.IsNullOrWhiteSpace(critterName))
            {
                ApplyName();
            }
        }

        private static void OnSpawnedFrom(CritterName component, object data)
        {
            var other = (data as GameObject)?.AddOrGet<CritterName>();
            other?.TransferTo(component);
        }

        private static void OnLayEgg(CritterName parentCN, object eggGO)
        {
            var eggCN = (eggGO as GameObject)?.AddOrGet<CritterName>();
            parentCN.TransferTo(eggCN);
        }

        private bool IsCritter()
        {
            bool result = this.HasTag(GameTags.Creature);
            return result;
        }

        private bool IsEgg()
        {
            bool result = this.HasTag(GameTags.Egg);
            return result;
        }

        public void SetName(string newName)
        {
            generation = 1;
            if (string.IsNullOrWhiteSpace(newName) || newName.ToLower() == UI.StripLinkFormatting(GetPrefabName()).ToLower())
            {
                ResetToPrefabName();
                return;
            }

            critterName = newName;
            ApplyName();
        }

        private void setGameObjectName(string newName)
        {
            KSelectable selectable = GetComponent<KSelectable>();

            name = newName;
            selectable?.SetName(newName);
            gameObject.name = newName;
        }

        public void ApplyName()
        {
            if (!IsCritter()) return;

            string newName = critterName;
            if (generation == 2)
            {
                newName += " Jr.";
            }
            else if (generation > 2)
            {
                newName += " " + Util.ToRoman(generation);
            }
            setGameObjectName(newName);
        }

        public bool HasName()
        {
            bool result = !string.IsNullOrWhiteSpace(critterName);
            return result;
        }

        public void TransferTo(CritterName other)
        {
            if (other == null || !HasName())
            {
                return;
            }

            other.critterName = critterName;
            other.generation = generation;

            if (other.IsEgg())
            {
                other.generation += 1;
            }

            other.ApplyName();
        }

        public string GetPrefabName()
        {
            KPrefabID prefab = GetComponent<KPrefabID>();
            string result = prefab != null ? TagManager.GetProperName(prefab.PrefabTag) : null;
            return result;
        }

        public void ResetToPrefabName()
        {
            string prefabName = GetPrefabName();
            if (prefabName != null)
            {
                critterName = "";
                setGameObjectName(prefabName);
            }
        }

        public static string ToJson(CritterName component)
        {
            if (component == null)
                return "null";
            try
            {
                var data = new
                {
                    name = component.gameObject?.name,
                    critterName = component.critterName,
                    generation = component.generation
                };
                return JsonConvert.SerializeObject(data);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

