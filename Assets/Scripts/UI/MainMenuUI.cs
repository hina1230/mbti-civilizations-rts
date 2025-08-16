using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

namespace MBTICivilizations.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject civilizationSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("Main Menu Elements")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Lobby Elements")]
        [SerializeField] private TMP_InputField lobbyNameInput;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Transform lobbyListContent;
        [SerializeField] private GameObject lobbyItemPrefab;
        [SerializeField] private TextMeshProUGUI joinCodeDisplay;
        
        [Header("Civilization Selection")]
        [SerializeField] private Transform civilizationGrid;
        [SerializeField] private GameObject civilizationButtonPrefab;
        [SerializeField] private TextMeshProUGUI civilizationNameText;
        [SerializeField] private TextMeshProUGUI civilizationDescriptionText;
        [SerializeField] private Button confirmCivilizationButton;
        [SerializeField] private Button backFromCivButton;
        
        private Core.CivilizationType selectedCivilization = Core.CivilizationType.INTJ;
        private Networking.NetworkSetup networkSetup;
        private Core.CivilizationManager civilizationManager;

        private void Start()
        {
            networkSetup = Networking.NetworkSetup.Instance;
            civilizationManager = FindObjectOfType<Core.CivilizationManager>();
            
            SetupButtons();
            ShowMainMenu();
            PopulateCivilizationGrid();
        }

        private void SetupButtons()
        {
            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
            refreshButton.onClick.AddListener(OnRefreshLobbiesClicked);
            backButton.onClick.AddListener(ShowMainMenu);
            
            confirmCivilizationButton.onClick.AddListener(OnConfirmCivilization);
            backFromCivButton.onClick.AddListener(ShowLobbyPanel);
        }

        private void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            civilizationSelectPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            mainMenuPanel.SetActive(false);
            lobbyPanel.SetActive(true);
            civilizationSelectPanel.SetActive(false);
            settingsPanel.SetActive(false);
            
            OnRefreshLobbiesClicked();
        }

        private void ShowCivilizationSelect()
        {
            mainMenuPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            civilizationSelectPanel.SetActive(true);
            settingsPanel.SetActive(false);
        }

        private void OnHostClicked()
        {
            ShowCivilizationSelect();
        }

        private void OnJoinClicked()
        {
            ShowLobbyPanel();
        }

        private void OnSettingsClicked()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnQuitClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnCreateLobbyClicked()
        {
            string lobbyName = string.IsNullOrEmpty(lobbyNameInput.text) ? "MBTI RTS Match" : lobbyNameInput.text;
            bool isPrivate = privateToggle.isOn;
            
            networkSetup.CreateMatch(lobbyName, isPrivate);
            
            StartCoroutine(ShowJoinCodeAfterCreation());
        }

        private IEnumerator ShowJoinCodeAfterCreation()
        {
            yield return new WaitForSeconds(1f);
            
            string joinCode = networkSetup.GetJoinCode();
            if (!string.IsNullOrEmpty(joinCode))
            {
                joinCodeDisplay.text = $"Join Code: {joinCode}";
                joinCodeDisplay.gameObject.SetActive(true);
            }
        }

        private void OnJoinLobbyClicked()
        {
            string code = joinCodeInput.text;
            if (!string.IsNullOrEmpty(code))
            {
                networkSetup.JoinMatch(code);
            }
        }

        private async void OnRefreshLobbiesClicked()
        {
            foreach (Transform child in lobbyListContent)
            {
                Destroy(child.gameObject);
            }
            
            List<Lobby> lobbies = await networkSetup.SearchLobbies();
            
            foreach (var lobby in lobbies)
            {
                GameObject lobbyItem = Instantiate(lobbyItemPrefab, lobbyListContent);
                LobbyItemUI lobbyItemUI = lobbyItem.GetComponent<LobbyItemUI>();
                if (lobbyItemUI != null)
                {
                    lobbyItemUI.Setup(lobby, () => OnLobbySelected(lobby));
                }
            }
        }

        private void OnLobbySelected(Lobby lobby)
        {
            networkSetup.JoinMatch(lobby.Id);
        }

        private void PopulateCivilizationGrid()
        {
            if (civilizationManager == null) return;
            
            List<Core.CivilizationType> civilizations = civilizationManager.GetAvailableCivilizations();
            
            foreach (var civType in civilizations)
            {
                GameObject civButton = Instantiate(civilizationButtonPrefab, civilizationGrid);
                CivilizationButtonUI buttonUI = civButton.GetComponent<CivilizationButtonUI>();
                
                if (buttonUI != null)
                {
                    buttonUI.Setup(civType, () => OnCivilizationSelected(civType));
                }
            }
            
            OnCivilizationSelected(Core.CivilizationType.INTJ);
        }

        private void OnCivilizationSelected(Core.CivilizationType civType)
        {
            selectedCivilization = civType;
            
            if (civilizationManager != null)
            {
                civilizationNameText.text = civType.ToString();
                civilizationDescriptionText.text = civilizationManager.GetCivilizationDescription(civType);
            }
        }

        private void OnConfirmCivilization()
        {
            PlayerPrefs.SetInt("SelectedCivilization", (int)selectedCivilization);
            ShowCivilizationSelect();
            networkSetup.CreateMatch();
        }
    }

    public class LobbyItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button joinButton;
        
        public void Setup(Lobby lobby, System.Action onJoinClicked)
        {
            lobbyNameText.text = lobby.Name;
            playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
            joinButton.onClick.AddListener(() => onJoinClicked());
        }
    }

    public class CivilizationButtonUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI civNameText;
        [SerializeField] private Image civIcon;
        [SerializeField] private Button button;
        
        public void Setup(Core.CivilizationType civType, System.Action onClicked)
        {
            civNameText.text = civType.ToString();
            button.onClick.AddListener(() => onClicked());
        }
    }
}