using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Core
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private GameMode gameMode = GameMode.Skirmish;
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private float gameSpeed = 1.0f;

        [Header("Match Settings")]
        public NetworkVariable<int> currentPlayerCount = new NetworkVariable<int>(0);
        public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.Lobby);

        private Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
        private ResourceManager resourceManager;
        private CivilizationManager civilizationManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            resourceManager = GetComponent<ResourceManager>();
            civilizationManager = GetComponent<CivilizationManager>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                currentPlayerCount.Value++;
                PlayerData newPlayer = new PlayerData
                {
                    clientId = clientId,
                    playerName = $"Player {clientId}",
                    civilizationType = CivilizationType.INTJ,
                    teamId = (int)(clientId % 2)
                };
                players[clientId] = newPlayer;
                Debug.Log($"Player {clientId} connected. Total players: {currentPlayerCount.Value}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (IsServer)
            {
                currentPlayerCount.Value--;
                if (players.ContainsKey(clientId))
                {
                    players.Remove(clientId);
                }
                Debug.Log($"Player {clientId} disconnected. Total players: {currentPlayerCount.Value}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartGameServerRpc()
        {
            if (currentPlayerCount.Value >= 2)
            {
                currentGameState.Value = GameState.InGame;
                InitializeGameClientRpc();
            }
        }

        [ClientRpc]
        private void InitializeGameClientRpc()
        {
            Debug.Log("Game Started!");
            if (IsLocalPlayer)
            {
                InitializeLocalPlayer();
            }
        }

        private void InitializeLocalPlayer()
        {
            resourceManager.InitializeResources();
            civilizationManager.InitializeCivilization(players[NetworkManager.Singleton.LocalClientId].civilizationType);
        }

        public PlayerData GetPlayerData(ulong clientId)
        {
            return players.ContainsKey(clientId) ? players[clientId] : null;
        }

        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.5f, 3.0f);
            Time.timeScale = gameSpeed;
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public ulong clientId;
        public string playerName;
        public CivilizationType civilizationType;
        public int teamId;
        public Color playerColor;
    }

    public enum GameState
    {
        Lobby,
        Loading,
        InGame,
        Paused,
        GameOver
    }

    public enum GameMode
    {
        Skirmish,
        Campaign,
        Tutorial,
        Custom
    }
}