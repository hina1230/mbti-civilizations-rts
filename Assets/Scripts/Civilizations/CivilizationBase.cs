using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Core
{
    public enum CivilizationType
    {
        INTJ, INTP, ENTJ, ENTP,
        INFJ, INFP, ENFJ, ENFP,
        ISTJ, ISFJ, ESTJ, ESFJ,
        ISTP, ISFP, ESTP, ESFP
    }

    public abstract class CivilizationBase : NetworkBehaviour
    {
        [Header("Civilization Info")]
        [SerializeField] protected CivilizationType civilizationType;
        [SerializeField] protected string civilizationName;
        [SerializeField] protected string civilizationDescription;
        [SerializeField] protected Sprite civilizationIcon;
        
        [Header("Civilization Bonuses")]
        [SerializeField] protected CivilizationBonuses bonuses;
        
        [Header("Unique Units")]
        [SerializeField] protected List<GameObject> uniqueUnitPrefabs = new List<GameObject>();
        
        [Header("Unique Buildings")]
        [SerializeField] protected List<GameObject> uniqueBuildingPrefabs = new List<GameObject>();
        
        [Header("Unique Technologies")]
        [SerializeField] protected List<Technology> uniqueTechnologies = new List<Technology>();

        protected NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();
        protected Dictionary<string, float> activeModifiers = new Dictionary<string, float>();

        public CivilizationType Type => civilizationType;
        public string Name => civilizationName;
        public string Description => civilizationDescription;
        public CivilizationBonuses Bonuses => bonuses;

        public virtual void Initialize(ulong clientId)
        {
            if (IsServer)
            {
                ownerClientId.Value = clientId;
                ApplyCivilizationBonuses();
                InitializeUniqueMechanics();
            }
        }

        protected virtual void ApplyCivilizationBonuses()
        {
            activeModifiers["ResourceGatherRate"] = bonuses.resourceGatheringBonus;
            activeModifiers["UnitProductionSpeed"] = bonuses.unitProductionSpeed;
            activeModifiers["BuildingConstructionSpeed"] = bonuses.buildingConstructionSpeed;
            activeModifiers["ResearchSpeed"] = bonuses.researchSpeed;
            activeModifiers["UnitDamage"] = bonuses.unitDamageBonus;
            activeModifiers["UnitDefense"] = bonuses.unitDefenseBonus;
            activeModifiers["UnitSpeed"] = bonuses.unitSpeedBonus;
        }

        protected abstract void InitializeUniqueMechanics();

        public virtual float GetModifier(string modifierType)
        {
            return activeModifiers.ContainsKey(modifierType) ? activeModifiers[modifierType] : 1.0f;
        }

        public virtual void ApplyPassiveAbility()
        {
        }

        public virtual void ActivateSpecialAbility()
        {
            if (CanActivateSpecialAbility())
            {
                ExecuteSpecialAbility();
                StartCooldown();
            }
        }

        protected abstract bool CanActivateSpecialAbility();
        protected abstract void ExecuteSpecialAbility();
        protected abstract void StartCooldown();

        public virtual GameObject GetUniqueUnit(int index)
        {
            if (index >= 0 && index < uniqueUnitPrefabs.Count)
                return uniqueUnitPrefabs[index];
            return null;
        }

        public virtual GameObject GetUniqueBuilding(int index)
        {
            if (index >= 0 && index < uniqueBuildingPrefabs.Count)
                return uniqueBuildingPrefabs[index];
            return null;
        }

        public virtual bool HasTechnology(string techName)
        {
            return uniqueTechnologies.Exists(t => t.name == techName);
        }

        public virtual void ResearchTechnology(string techName)
        {
            Technology tech = uniqueTechnologies.Find(t => t.name == techName);
            if (tech != null && !tech.isResearched)
            {
                StartCoroutine(ResearchTechnologyCoroutine(tech));
            }
        }

        protected IEnumerator ResearchTechnologyCoroutine(Technology tech)
        {
            float researchTime = tech.researchTime * GetModifier("ResearchSpeed");
            yield return new WaitForSeconds(researchTime);
            
            tech.isResearched = true;
            ApplyTechnologyEffects(tech);
        }

        protected virtual void ApplyTechnologyEffects(Technology tech)
        {
            foreach (var effect in tech.effects)
            {
                if (activeModifiers.ContainsKey(effect.modifierType))
                {
                    activeModifiers[effect.modifierType] *= effect.value;
                }
                else
                {
                    activeModifiers[effect.modifierType] = effect.value;
                }
            }
        }

        public virtual void OnUnitCreated(GameObject unit)
        {
        }

        public virtual void OnBuildingConstructed(GameObject building)
        {
        }

        public virtual void OnResourceGathered(ResourceType resourceType, float amount)
        {
        }

        public virtual void OnEnemyDefeated(GameObject enemy)
        {
        }
    }

    [System.Serializable]
    public class CivilizationBonuses
    {
        [Range(0.5f, 2.0f)] public float resourceGatheringBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float unitProductionSpeed = 1.0f;
        [Range(0.5f, 2.0f)] public float buildingConstructionSpeed = 1.0f;
        [Range(0.5f, 2.0f)] public float researchSpeed = 1.0f;
        [Range(0.5f, 2.0f)] public float unitDamageBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float unitDefenseBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float unitSpeedBonus = 1.0f;
        [Range(0.5f, 2.0f)] public float economicEfficiency = 1.0f;
        [Range(0.5f, 2.0f)] public float visionRange = 1.0f;
    }

    [System.Serializable]
    public class Technology
    {
        public string name;
        public string description;
        public Sprite icon;
        public float researchTime = 60f;
        public ResourceCost cost;
        public bool isResearched = false;
        public List<TechnologyEffect> effects = new List<TechnologyEffect>();
        public List<string> prerequisites = new List<string>();
    }

    [System.Serializable]
    public class TechnologyEffect
    {
        public string modifierType;
        public float value;
        public string description;
    }
}