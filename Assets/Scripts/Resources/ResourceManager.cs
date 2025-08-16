using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Core
{
    public class ResourceManager : NetworkBehaviour
    {
        [System.Serializable]
        public class PlayerResources
        {
            public NetworkVariable<int> gold = new NetworkVariable<int>(1000);
            public NetworkVariable<int> wood = new NetworkVariable<int>(500);
            public NetworkVariable<int> food = new NetworkVariable<int>(500);
            public NetworkVariable<int> stone = new NetworkVariable<int>(200);
            public NetworkVariable<int> population = new NetworkVariable<int>(0);
            public NetworkVariable<int> populationCap = new NetworkVariable<int>(200);
        }

        private Dictionary<ulong, PlayerResources> playerResources = new Dictionary<ulong, PlayerResources>();

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
            if (IsServer)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                InitializePlayerResources(clientId);
            }
        }

        private void InitializePlayerResources(ulong clientId)
        {
            if (!playerResources.ContainsKey(clientId))
            {
                PlayerResources resources = new PlayerResources();
                resources.gold.Value = startingGold;
                resources.wood.Value = startingWood;
                resources.food.Value = startingFood;
                resources.stone.Value = startingStone;
                resources.population.Value = 0;
                resources.populationCap.Value = startingPopCap;
                
                playerResources[clientId] = resources;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddResourceServerRpc(ulong playerId, ResourceType resourceType, int amount)
        {
            if (!playerResources.ContainsKey(playerId))
            {
                InitializePlayerResources(playerId);
            }

            PlayerResources resources = playerResources[playerId];
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    resources.gold.Value += amount;
                    break;
                case ResourceType.Wood:
                    resources.wood.Value += amount;
                    break;
                case ResourceType.Food:
                    resources.food.Value += amount;
                    break;
                case ResourceType.Stone:
                    resources.stone.Value += amount;
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpendResourceServerRpc(ulong playerId, ResourceType resourceType, int amount, ServerRpcParams rpcParams = default)
        {
            if (!CanAfford(playerId, resourceType, amount))
            {
                ResourceTransactionFailedClientRpc(resourceType, amount, 
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { playerId } } });
                return;
            }

            PlayerResources resources = playerResources[playerId];
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    resources.gold.Value -= amount;
                    break;
                case ResourceType.Wood:
                    resources.wood.Value -= amount;
                    break;
                case ResourceType.Food:
                    resources.food.Value -= amount;
                    break;
                case ResourceType.Stone:
                    resources.stone.Value -= amount;
                    break;
            }

            ResourceTransactionSuccessClientRpc(resourceType, amount,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { playerId } } });
        }

        public bool CanAfford(ulong playerId, ResourceType resourceType, int amount)
        {
            if (!playerResources.ContainsKey(playerId))
                return false;

            PlayerResources resources = playerResources[playerId];
            
            switch (resourceType)
            {
                case ResourceType.Gold:
                    return resources.gold.Value >= amount;
                case ResourceType.Wood:
                    return resources.wood.Value >= amount;
                case ResourceType.Food:
                    return resources.food.Value >= amount;
                case ResourceType.Stone:
                    return resources.stone.Value >= amount;
                default:
                    return false;
            }
        }

        public bool CanAffordMultiple(ulong playerId, ResourceCost cost)
        {
            if (!playerResources.ContainsKey(playerId))
                return false;

            PlayerResources resources = playerResources[playerId];
            
            return resources.gold.Value >= cost.gold &&
                   resources.wood.Value >= cost.wood &&
                   resources.food.Value >= cost.food &&
                   resources.stone.Value >= cost.stone;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpendMultipleResourcesServerRpc(ulong playerId, ResourceCost cost)
        {
            if (!CanAffordMultiple(playerId, cost))
            {
                MultiResourceTransactionFailedClientRpc(
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { playerId } } });
                return;
            }

            PlayerResources resources = playerResources[playerId];
            resources.gold.Value -= cost.gold;
            resources.wood.Value -= cost.wood;
            resources.food.Value -= cost.food;
            resources.stone.Value -= cost.stone;

            MultiResourceTransactionSuccessClientRpc(
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { playerId } } });
        }

        [ClientRpc]
        private void ResourceTransactionSuccessClientRpc(ResourceType resourceType, int amount, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"Successfully spent {amount} {resourceType}");
        }

        [ClientRpc]
        private void ResourceTransactionFailedClientRpc(ResourceType resourceType, int amount, ClientRpcParams clientRpcParams = default)
        {
            Debug.LogWarning($"Not enough {resourceType}! Needed: {amount}");
        }

        [ClientRpc]
        private void MultiResourceTransactionSuccessClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Debug.Log("Resource transaction successful");
        }

        [ClientRpc]
        private void MultiResourceTransactionFailedClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Debug.LogWarning("Not enough resources!");
        }

        public PlayerResources GetPlayerResources(ulong playerId)
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