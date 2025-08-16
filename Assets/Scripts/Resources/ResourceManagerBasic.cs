using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBTICivilizations.Core
{
    public class ResourceManagerBasic : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerResourcesBasic
        {
            public int gold = 1000;
            public int wood = 500;
            public int food = 500;
            public int stone = 200;
            public int population = 0;
            public int populationCap = 200;
        }

        private Dictionary<string, PlayerResourcesBasic> playerResources = new Dictionary<string, PlayerResourcesBasic>();

        [Header("Resource Settings")]
        [SerializeField] private int startingGold = 1000;
        [SerializeField] private int startingWood = 500;
        [SerializeField] private int startingFood = 500;
        [SerializeField] private int startingStone = 200;
        [SerializeField] private int startingPopCap = 200;

        [Header("Gathering Rates")]
        [SerializeField] private float goldGatherRate = 0.8f;
        [SerializeField] private float woodGatherRate = 0.6f;
        [SerializeField] private float foodGatherRate = 0.7f;
        [SerializeField] private float stoneGatherRate = 0.5f;

        public void InitializeResources()
        {
            string localPlayerId = "LocalPlayer";
            InitializePlayerResources(localPlayerId);
            Debug.Log("Resources initialized for local player");
        }

        private void InitializePlayerResources(string playerId)
        {
            if (!playerResources.ContainsKey(playerId))
            {
                PlayerResourcesBasic resources = new PlayerResourcesBasic();
                resources.gold = startingGold;
                resources.wood = startingWood;
                resources.food = startingFood;
                resources.stone = startingStone;
                resources.population = 0;
                resources.populationCap = startingPopCap;
                
                playerResources[playerId] = resources;
            }
        }

        public void AddResource(string playerId, ResourceType resourceType, int amount)
        {
            if (!playerResources.ContainsKey(playerId))
            {
                InitializePlayerResources(playerId);
            }

            PlayerResourcesBasic resources = playerResources[playerId];
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    resources.gold += amount;
                    break;
                case ResourceType.Wood:
                    resources.wood += amount;
                    break;
                case ResourceType.Food:
                    resources.food += amount;
                    break;
                case ResourceType.Stone:
                    resources.stone += amount;
                    break;
            }
            
            Debug.Log($"Added {amount} {resourceType} to {playerId}");
        }

        public bool CanAfford(string playerId, ResourceType resourceType, int amount)
        {
            if (!playerResources.ContainsKey(playerId))
                return false;

            PlayerResourcesBasic resources = playerResources[playerId];
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    return resources.gold >= amount;
                case ResourceType.Wood:
                    return resources.wood >= amount;
                case ResourceType.Food:
                    return resources.food >= amount;
                case ResourceType.Stone:
                    return resources.stone >= amount;
                default:
                    return false;
            }
        }

        public bool CanAffordMultiple(string playerId, ResourceCost cost)
        {
            if (!playerResources.ContainsKey(playerId))
                return false;

            PlayerResourcesBasic resources = playerResources[playerId];
            
            return resources.gold >= cost.gold &&
                   resources.wood >= cost.wood &&
                   resources.food >= cost.food &&
                   resources.stone >= cost.stone;
        }

        public void SpendMultipleResources(string playerId, ResourceCost cost)
        {
            if (!CanAffordMultiple(playerId, cost))
            {
                Debug.LogWarning("Not enough resources!");
                return;
            }

            PlayerResourcesBasic resources = playerResources[playerId];
            resources.gold -= cost.gold;
            resources.wood -= cost.wood;
            resources.food -= cost.food;
            resources.stone -= cost.stone;

            Debug.Log("Resource transaction successful");
        }

        public PlayerResourcesBasic GetPlayerResources(string playerId)
        {
            return playerResources.ContainsKey(playerId) ? playerResources[playerId] : null;
        }

        public float GetGatherRate(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    return goldGatherRate;
                case ResourceType.Wood:
                    return woodGatherRate;
                case ResourceType.Food:
                    return foodGatherRate;
                case ResourceType.Stone:
                    return stoneGatherRate;
                default:
                    return 0f;
            }
        }
    }

    public enum ResourceType
    {
        Gold,
        Wood,
        Food,
        Stone
    }

    [System.Serializable]
    public struct ResourceCost
    {
        public int gold;
        public int wood;
        public int food;
        public int stone;

        public ResourceCost(int gold = 0, int wood = 0, int food = 0, int stone = 0)
        {
            this.gold = gold;
            this.wood = wood;
            this.food = food;
            this.stone = stone;
        }
    }
}