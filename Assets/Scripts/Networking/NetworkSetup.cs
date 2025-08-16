using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

namespace MBTICivilizations.Networking
{
    public class NetworkSetup : MonoBehaviour
    {
        public static NetworkSetup Instance { get; private set; }

        [Header("Network Settings")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private string defaultLobbyName = "MBTI RTS Match";
        [SerializeField] private bool useRelay = true;
        
        [Header("Connection Info")]
        [SerializeField] private string joinCode = "";
        private Lobby currentLobby;
        private string playerId;
        
        private NetworkManager networkManager;
        private UnityTransport transport;

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
            
            transport = GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = gameObject.AddComponent<UnityTransport>();
                networkManager.NetworkConfig.NetworkTransport = transport;
            }
        }

        private async void Start()
        {
            await InitializeUnityServices();
        }

        private async Task InitializeUnityServices()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                try
                {
                    await UnityServices.InitializeAsync();
                    
                    if (!AuthenticationService.Instance.IsSignedIn)
                    {
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                        playerId = AuthenticationService.Instance.PlayerId;
                        Debug.Log($"Signed in as: {playerId}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
                }
            }
        }

        public async void CreateMatch(string matchName = null, bool isPrivate = false)
        {
            try
            {
                if (useRelay)
                {
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                    joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    
                    transport.SetHostRelayData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData
                    );
                    
                    Debug.Log($"Relay created with join code: {joinCode}");
                }
                
                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                        { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Skirmish") }
                    }
                };
                
                currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                    matchName ?? defaultLobbyName,
                    maxPlayers,
                    options
                );
                
                StartCoroutine(HeartbeatLobby());
                
                networkManager.StartHost();
                
                OnMatchCreated();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create match: {e.Message}");
            }
        }

        public async void JoinMatch(string lobbyIdOrCode)
        {
            try
            {
                if (lobbyIdOrCode.Length == 6)
                {
                    currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyIdOrCode);
                }
                else
                {
                    currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyIdOrCode);
                }
                
                string relayJoinCode = currentLobby.Data["JoinCode"].Value;
                
                if (useRelay && !string.IsNullOrEmpty(relayJoinCode))
                {
                    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                    
                    transport.SetClientRelayData(
                        joinAllocation.RelayServer.IpV4,
                        (ushort)joinAllocation.RelayServer.Port,
                        joinAllocation.AllocationIdBytes,
                        joinAllocation.Key,
                        joinAllocation.ConnectionData,
                        joinAllocation.HostConnectionData
                    );
                }
                
                networkManager.StartClient();
                
                OnMatchJoined();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join match: {e.Message}");
            }
        }

        public void StartLocalHost()
        {
            networkManager.StartHost();
            OnMatchCreated();
        }

        public void StartLocalClient(string ipAddress = "127.0.0.1", ushort port = 7777)
        {
            transport.SetConnectionData(ipAddress, port);
            networkManager.StartClient();
            OnMatchJoined();
        }

        public void Disconnect()
        {
            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
                LeaveLobby();
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
                LeaveLobby();
            }
        }

        private async void LeaveLobby()
        {
            if (currentLobby != null)
            {
                try
                {
                    if (networkManager.IsHost)
                    {
                        await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                    }
                    else
                    {
                        await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                    }
                    currentLobby = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error leaving lobby: {e.Message}");
                }
            }
        }

        private IEnumerator HeartbeatLobby()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(15);
            
            while (currentLobby != null)
            {
                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                yield return wait;
            }
        }

        public async Task<List<Lobby>> SearchLobbies()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };
                
                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
                return response.Results;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to search lobbies: {e.Message}");
                return new List<Lobby>();
            }
        }

        private void OnMatchCreated()
        {
            Debug.Log("Match created successfully!");
            if (!string.IsNullOrEmpty(joinCode))
            {
                Debug.Log($"Share this code with other players: {joinCode}");
            }
        }

        private void OnMatchJoined()
        {
            Debug.Log("Successfully joined match!");
        }

        public string GetJoinCode()
        {
            return joinCode;
        }

        public Lobby GetCurrentLobby()
        {
            return currentLobby;
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

        private void OnDestroy()
        {
            if (currentLobby != null)
            {
                LeaveLobby();
            }
        }
    }
}