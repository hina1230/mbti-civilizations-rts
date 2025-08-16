using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MBTICivilizations.Networking
{
    public class NetworkSetupBasic : MonoBehaviour
    {
        public static NetworkSetupBasic Instance { get; private set; }

        [Header("Network Settings")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private string defaultLobbyName = "MBTI RTS Match";
        
        private NetworkManager networkManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }
        }

        private void Start()
        {
            Debug.Log("Basic Network Setup initialized");
        }

        public void CreateMatch(string matchName = null)
        {
            try
            {
                Debug.Log($"Creating local match: {matchName ?? defaultLobbyName}");
                
                if (networkManager != null)
                {
                    bool success = networkManager.StartHost();
                    if (success)
                    {
                        OnMatchCreated();
                    }
                    else
                    {
                        Debug.LogError("Failed to start host");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create match: {e.Message}");
            }
        }

        public void JoinMatch(string address = "127.0.0.1")
        {
            try
            {
                Debug.Log($"Joining match at: {address}");
                
                if (networkManager != null)
                {
                    bool success = networkManager.StartClient();
                    if (success)
                    {
                        OnMatchJoined();
                    }
                    else
                    {
                        Debug.LogError("Failed to start client");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join match: {e.Message}");
            }
        }

        public void StartLocalHost()
        {
            if (networkManager != null)
            {
                networkManager.StartHost();
                OnMatchCreated();
            }
        }

        public void StartLocalClient()
        {
            if (networkManager != null)
            {
                networkManager.StartClient();
                OnMatchJoined();
            }
        }

        public void Disconnect()
        {
            if (networkManager != null)
            {
                if (networkManager.IsHost)
                {
                    networkManager.Shutdown();
                    Debug.Log("Host disconnected");
                }
                else if (networkManager.IsClient)
                {
                    networkManager.Shutdown();
                    Debug.Log("Client disconnected");
                }
            }
        }

        private void OnMatchCreated()
        {
            Debug.Log("Match created successfully!");
            Debug.Log("Other players can connect to this host");
        }

        private void OnMatchJoined()
        {
            Debug.Log("Successfully joined match!");
        }

        public bool IsHost()
        {
            return networkManager != null && networkManager.IsHost;
        }

        public bool IsClient()
        {
            return networkManager != null && networkManager.IsClient;
        }

        public bool IsConnected()
        {
            return networkManager != null && (networkManager.IsHost || networkManager.IsClient);
        }

        public int GetMaxPlayers()
        {
            return maxPlayers;
        }

        public void SetMaxPlayers(int max)
        {
            maxPlayers = Mathf.Clamp(max, 1, 8);
        }

        private void OnDestroy()
        {
            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient))
            {
                networkManager.Shutdown();
            }
        }

        // UI用のヘルパーメソッド
        public void StartAsHost()
        {
            CreateMatch();
        }

        public void StartAsClient()
        {
            JoinMatch();
        }

        public void StartSinglePlayer()
        {
            Debug.Log("Starting single player mode");
            // シングルプレイヤーモードの実装
            Core.GameManager gameManager = FindObjectOfType<Core.GameManager>();
            if (gameManager != null)
            {
                // ローカルゲームとして開始
                gameManager.StartGameServerRpc();
            }
        }
    }
}