using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace MBTICivilizations.Core
{
    public class CivilizationManager : NetworkBehaviour
    {
        [Header("Civilization Prefabs")]
        [SerializeField] private List<GameObject> civilizationPrefabs = new List<GameObject>();
        
        private Dictionary<CivilizationType, GameObject> civilizationPrefabMap;
        private Dictionary<ulong, CivilizationBase> activeCivilizations;
        
        [Header("Civilization Descriptions")]
        [SerializeField] private List<CivilizationData> civilizationDataList = new List<CivilizationData>();

        private void Awake()
        {
            civilizationPrefabMap = new Dictionary<CivilizationType, GameObject>();
            activeCivilizations = new Dictionary<ulong, CivilizationBase>();
            
            InitializeCivilizationData();
        }

        private void InitializeCivilizationData()
        {
            foreach (var prefab in civilizationPrefabs)
            {
                var civBase = prefab.GetComponent<CivilizationBase>();
                if (civBase != null)
                {
                    civilizationPrefabMap[civBase.Type] = prefab;
                }
            }
        }

        public void InitializeCivilization(CivilizationType type)
        {
            if (IsServer)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                SpawnCivilizationServerRpc(clientId, type);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnCivilizationServerRpc(ulong clientId, CivilizationType type)
        {
            if (civilizationPrefabMap.ContainsKey(type))
            {
                GameObject civPrefab = civilizationPrefabMap[type];
                GameObject civInstance = Instantiate(civPrefab);
                
                NetworkObject netObj = civInstance.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.SpawnWithOwnership(clientId);
                    
                    CivilizationBase civBase = civInstance.GetComponent<CivilizationBase>();
                    if (civBase != null)
                    {
                        civBase.Initialize(clientId);
                        activeCivilizations[clientId] = civBase;
                        
                        OnCivilizationInitializedClientRpc(clientId, type);
                    }
                }
            }
        }

        [ClientRpc]
        private void OnCivilizationInitializedClientRpc(ulong clientId, CivilizationType type)
        {
            Debug.Log($"Player {clientId} initialized as {type} civilization");
        }

        public CivilizationBase GetPlayerCivilization(ulong clientId)
        {
            return activeCivilizations.ContainsKey(clientId) ? activeCivilizations[clientId] : null;
        }

        public CivilizationData GetCivilizationData(CivilizationType type)
        {
            return civilizationDataList.FirstOrDefault(c => c.type == type);
        }

        public List<CivilizationType> GetAvailableCivilizations()
        {
            return System.Enum.GetValues(typeof(CivilizationType)).Cast<CivilizationType>().ToList();
        }

        public string GetCivilizationDescription(CivilizationType type)
        {
            var data = GetCivilizationData(type);
            if (data != null)
            {
                return data.GetFullDescription();
            }
            
            return GetDefaultDescription(type);
        }

        private string GetDefaultDescription(CivilizationType type)
        {
            switch (type)
            {
                case CivilizationType.INTJ:
                    return "The Strategist - Masters of long-term planning and technological advancement";
                case CivilizationType.ENTJ:
                    return "The Commander - Natural leaders with military prowess";
                case CivilizationType.INTP:
                    return "The Thinker - Innovation through analysis and research";
                case CivilizationType.ENTP:
                    return "The Debater - Adaptable and resourceful in any situation";
                case CivilizationType.INFJ:
                    return "The Advocate - Defensive specialists with strong support abilities";
                case CivilizationType.ENFJ:
                    return "The Protagonist - Team synergy and morale bonuses";
                case CivilizationType.INFP:
                    return "The Mediator - Cultural and diplomatic advantages";
                case CivilizationType.ENFP:
                    return "The Campaigner - Exploration and expansion bonuses";
                case CivilizationType.ISTJ:
                    return "The Logistician - Economic efficiency and resource management";
                case CivilizationType.ESTJ:
                    return "The Executive - Production and organization bonuses";
                case CivilizationType.ISFJ:
                    return "The Defender - Defensive structures and healing";
                case CivilizationType.ESFJ:
                    return "The Consul - Population growth and happiness";
                case CivilizationType.ISTP:
                    return "The Virtuoso - Versatile units and quick adaptation";
                case CivilizationType.ESTP:
                    return "The Entrepreneur - Trade and raid bonuses";
                case CivilizationType.ISFP:
                    return "The Adventurer - Stealth and guerrilla tactics";
                case CivilizationType.ESFP:
                    return "The Entertainer - Morale and speed bonuses";
                default:
                    return "Unknown civilization type";
            }
        }
    }

    [System.Serializable]
    public class CivilizationData
    {
        public CivilizationType type;
        public string displayName;
        public string tagline;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public Color primaryColor;
        public Color secondaryColor;
        
        [Header("Strengths and Weaknesses")]
        public List<string> strengths = new List<string>();
        public List<string> weaknesses = new List<string>();
        
        [Header("Unique Features")]
        public string uniqueAbility;
        public string uniqueUnit;
        public string uniqueBuilding;
        public string uniqueTechnology;
        
        public string GetFullDescription()
        {
            string fullDesc = $"{displayName} - {tagline}\n\n";
            fullDesc += $"{description}\n\n";
            
            if (strengths.Count > 0)
            {
                fullDesc += "Strengths:\n";
                foreach (var strength in strengths)
                {
                    fullDesc += $"• {strength}\n";
                }
                fullDesc += "\n";
            }
            
            if (weaknesses.Count > 0)
            {
                fullDesc += "Weaknesses:\n";
                foreach (var weakness in weaknesses)
                {
                    fullDesc += $"• {weakness}\n";
                }
                fullDesc += "\n";
            }
            
            if (!string.IsNullOrEmpty(uniqueAbility))
                fullDesc += $"Unique Ability: {uniqueAbility}\n";
            if (!string.IsNullOrEmpty(uniqueUnit))
                fullDesc += $"Unique Unit: {uniqueUnit}\n";
            if (!string.IsNullOrEmpty(uniqueBuilding))
                fullDesc += $"Unique Building: {uniqueBuilding}\n";
            if (!string.IsNullOrEmpty(uniqueTechnology))
                fullDesc += $"Unique Technology: {uniqueTechnology}\n";
            
            return fullDesc;
        }
    }
}