using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Core
{
    public class INTJCivilization : CivilizationBase
    {
        [Header("INTJ Specific Settings")]
        [SerializeField] private float techResearchBonus = 1.25f;
        [SerializeField] private float planningEfficiencyBonus = 1.15f;
        [SerializeField] private float unitUpgradeDiscount = 0.8f;
        [SerializeField] private float visionRangeBonus = 1.2f;
        
        [Header("Strategic Planning")]
        [SerializeField] private int maxStrategicPlans = 3;
        [SerializeField] private float strategicPlanCooldown = 120f;
        private List<StrategicPlan> activePlans = new List<StrategicPlan>();
        private float lastPlanTime = 0f;
        
        [Header("Unique Abilities")]
        [SerializeField] private float masterPlanDuration = 30f;
        [SerializeField] private float masterPlanCooldown = 300f;
        private bool masterPlanActive = false;
        private float masterPlanEndTime = 0f;
        private float lastMasterPlanTime = 0f;

        private void Awake()
        {
            civilizationType = CivilizationType.INTJ;
            civilizationName = "The Strategist Empire";
            civilizationDescription = "Masters of long-term planning and technological advancement. " +
                                     "Excel at research, efficiency, and strategic warfare.";
            
            SetupBonuses();
        }

        private void SetupBonuses()
        {
            bonuses = new CivilizationBonuses
            {
                resourceGatheringBonus = 1.0f,
                unitProductionSpeed = 0.9f,
                buildingConstructionSpeed = 1.1f,
                researchSpeed = techResearchBonus,
                unitDamageBonus = 1.05f,
                unitDefenseBonus = 1.1f,
                unitSpeedBonus = 0.95f,
                economicEfficiency = planningEfficiencyBonus,
                visionRange = visionRangeBonus
            };
        }

        protected override void InitializeUniqueMechanics()
        {
            activeModifiers["TechCostReduction"] = unitUpgradeDiscount;
            activeModifiers["StrategicPlanning"] = 1.0f;
            activeModifiers["InformationWarfare"] = 1.15f;
            
            if (IsServer)
            {
                InvokeRepeating(nameof(ApplyPassiveAbility), 1f, 5f);
            }
        }

        public override void ApplyPassiveAbility()
        {
            if (activePlans.Count > 0)
            {
                foreach (var plan in activePlans)
                {
                    if (Time.time < plan.endTime)
                    {
                        ApplyPlanEffects(plan);
                    }
                }
                
                activePlans.RemoveAll(p => Time.time >= p.endTime);
            }
            
            if (masterPlanActive && Time.time >= masterPlanEndTime)
            {
                DeactivateMasterPlan();
            }
        }

        protected override bool CanActivateSpecialAbility()
        {
            return Time.time - lastMasterPlanTime >= masterPlanCooldown && !masterPlanActive;
        }

        protected override void ExecuteSpecialAbility()
        {
            ActivateMasterPlan();
        }

        protected override void StartCooldown()
        {
            lastMasterPlanTime = Time.time;
        }

        private void ActivateMasterPlan()
        {
            if (!IsServer) return;
            
            masterPlanActive = true;
            masterPlanEndTime = Time.time + masterPlanDuration;
            
            activeModifiers["ResourceGatherRate"] *= 1.5f;
            activeModifiers["ResearchSpeed"] *= 2.0f;
            activeModifiers["UnitProductionSpeed"] *= 1.3f;
            activeModifiers["BuildingConstructionSpeed"] *= 1.4f;
            activeModifiers["VisionRange"] *= 1.5f;
            
            MasterPlanActivatedClientRpc();
        }

        private void DeactivateMasterPlan()
        {
            masterPlanActive = false;
            
            activeModifiers["ResourceGatherRate"] /= 1.5f;
            activeModifiers["ResearchSpeed"] /= 2.0f;
            activeModifiers["UnitProductionSpeed"] /= 1.3f;
            activeModifiers["BuildingConstructionSpeed"] /= 1.4f;
            activeModifiers["VisionRange"] /= 1.5f;
            
            MasterPlanDeactivatedClientRpc();
        }

        [ClientRpc]
        private void MasterPlanActivatedClientRpc()
        {
            Debug.Log("INTJ Master Plan Activated! All operations enhanced for 30 seconds.");
        }

        [ClientRpc]
        private void MasterPlanDeactivatedClientRpc()
        {
            Debug.Log("INTJ Master Plan completed.");
        }

        public void CreateStrategicPlan(StrategicPlanType type)
        {
            if (!IsServer) return;
            
            if (activePlans.Count >= maxStrategicPlans)
            {
                Debug.LogWarning("Maximum strategic plans reached!");
                return;
            }
            
            if (Time.time - lastPlanTime < strategicPlanCooldown / maxStrategicPlans)
            {
                Debug.LogWarning("Strategic plan on cooldown!");
                return;
            }
            
            StrategicPlan newPlan = new StrategicPlan
            {
                type = type,
                startTime = Time.time,
                endTime = Time.time + 60f,
                effectiveness = CalculatePlanEffectiveness()
            };
            
            activePlans.Add(newPlan);
            lastPlanTime = Time.time;
            
            StrategicPlanCreatedClientRpc(type);
        }

        private float CalculatePlanEffectiveness()
        {
            float baseEffectiveness = 1.0f;
            
            baseEffectiveness += activeModifiers["StrategicPlanning"] * 0.2f;
            
            if (masterPlanActive)
            {
                baseEffectiveness *= 1.5f;
            }
            
            return baseEffectiveness;
        }

        private void ApplyPlanEffects(StrategicPlan plan)
        {
            switch (plan.type)
            {
                case StrategicPlanType.EconomicDevelopment:
                    activeModifiers["ResourceGatherRate"] = 1.0f + (0.2f * plan.effectiveness);
                    break;
                case StrategicPlanType.MilitarySupremacy:
                    activeModifiers["UnitDamage"] = 1.0f + (0.15f * plan.effectiveness);
                    activeModifiers["UnitProductionSpeed"] = 1.0f + (0.1f * plan.effectiveness);
                    break;
                case StrategicPlanType.TechnologicalAdvancement:
                    activeModifiers["ResearchSpeed"] = techResearchBonus + (0.3f * plan.effectiveness);
                    break;
                case StrategicPlanType.DefensivePosture:
                    activeModifiers["UnitDefense"] = 1.0f + (0.25f * plan.effectiveness);
                    activeModifiers["BuildingConstructionSpeed"] = 1.0f + (0.15f * plan.effectiveness);
                    break;
            }
        }

        [ClientRpc]
        private void StrategicPlanCreatedClientRpc(StrategicPlanType type)
        {
            Debug.Log($"INTJ Strategic Plan initiated: {type}");
        }

        public override void OnUnitCreated(GameObject unit)
        {
            base.OnUnitCreated(unit);
            
            if (masterPlanActive)
            {
                Debug.Log("Unit created with Master Plan bonuses!");
            }
        }

        public override void OnBuildingConstructed(GameObject building)
        {
            base.OnBuildingConstructed(building);
            
            if (building.name.Contains("ResearchLab") || building.name.Contains("Observatory"))
            {
                activeModifiers["ResearchSpeed"] *= 1.05f;
                Debug.Log("INTJ Research speed increased from new research facility!");
            }
        }

        public override void ResearchTechnology(string techName)
        {
            base.ResearchTechnology(techName);
            
            Debug.Log($"INTJ researching {techName} with {GetModifier("ResearchSpeed"):F2}x speed bonus");
        }

        protected override void ApplyTechnologyEffects(Technology tech)
        {
            base.ApplyTechnologyEffects(tech);
            
            if (tech.name.Contains("Advanced") || tech.name.Contains("Quantum"))
            {
                activeModifiers["StrategicPlanning"] *= 1.1f;
                Debug.Log("INTJ Strategic Planning enhanced from advanced technology!");
            }
        }
    }

    [System.Serializable]
    public class StrategicPlan
    {
        public StrategicPlanType type;
        public float startTime;
        public float endTime;
        public float effectiveness;
    }

    public enum StrategicPlanType
    {
        EconomicDevelopment,
        MilitarySupremacy,
        TechnologicalAdvancement,
        DefensivePosture
    }
}