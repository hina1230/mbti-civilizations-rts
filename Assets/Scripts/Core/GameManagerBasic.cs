using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBTICivilizations.Core
{
    // Basic version without Netcode dependency for compilation
    public class GameManagerBasic : MonoBehaviour
    {
        public static GameManagerBasic Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private GameMode gameMode = GameMode.Skirmish;
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private float gameSpeed = 1.0f;

        [Header("Match Settings")]
        public int currentPlayerCount = 0;
        public GameState currentGameState = GameState.Lobby;

        private Dictionary<string, PlayerDataBasic> players = new Dictionary<string, PlayerDataBasic>();
        private ResourceManagerBasic resourceManager;
        private CivilizationManagerBasic civilizationManager;

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
            resourceManager = GetComponent<ResourceManagerBasic>();
            civilizationManager = GetComponent<CivilizationManagerBasic>();
            
            Debug.Log("Basic Game Manager initialized - Install Netcode packages for full functionality");
        }

        public void StartGame()
        {
            if (currentPlayerCount >= 2)
            {
                currentGameState = GameState.InGame;
                InitializeGame();
            }
        }

        private void InitializeGame()
        {
            Debug.Log("Game Started!");
            InitializeLocalPlayer();
        }

        private void InitializeLocalPlayer()
        {
            if (resourceManager != null)
                resourceManager.InitializeResources();
            if (civilizationManager != null)
                civilizationManager.InitializeCivilization(CivilizationType.INTJ);
        }

        public PlayerDataBasic GetPlayerData(string playerId)
        {
            return players.ContainsKey(playerId) ? players[playerId] : null;
        }

        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.5f, 3.0f);
            Time.timeScale = gameSpeed;
        }
    }

    [System.Serializable]
    public class PlayerDataBasic
    {
        public string playerId;
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