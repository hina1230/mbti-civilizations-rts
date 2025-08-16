using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MBTICivilizations.UI
{
    public class MainMenuUIBasic : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject civilizationSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("Main Menu Elements")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button civilizationButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Civilization Selection")]
        [SerializeField] private Transform civilizationGrid;
        [SerializeField] private GameObject civilizationButtonPrefab;
        [SerializeField] private TextMeshProUGUI civilizationNameText;
        [SerializeField] private TextMeshProUGUI civilizationDescriptionText;
        [SerializeField] private Button confirmCivilizationButton;
        [SerializeField] private Button backFromCivButton;
        
        private Core.CivilizationType selectedCivilization = Core.CivilizationType.INTJ;
        private Core.CivilizationManagerBasic civilizationManager;

        private void Start()
        {
            civilizationManager = FindObjectOfType<Core.CivilizationManagerBasic>();
            
            SetupButtons();
            ShowMainMenu();
            PopulateCivilizationGrid();
        }

        private void SetupButtons()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
            if (civilizationButton != null)
                civilizationButton.onClick.AddListener(OnCivilizationClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            if (confirmCivilizationButton != null)
                confirmCivilizationButton.onClick.AddListener(OnConfirmCivilization);
            if (backFromCivButton != null)
                backFromCivButton.onClick.AddListener(ShowMainMenu);
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (civilizationSelectPanel != null) civilizationSelectPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void ShowCivilizationSelect()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (civilizationSelectPanel != null) civilizationSelectPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnStartGameClicked()
        {
            Debug.Log("Starting local game...");
            // Start a basic local game
            Core.GameManagerBasic gameManager = FindObjectOfType<Core.GameManagerBasic>();
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        private void OnCivilizationClicked()
        {
            ShowCivilizationSelect();
        }

        private void OnSettingsClicked()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void OnQuitClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void PopulateCivilizationGrid()
        {
            if (civilizationManager == null || civilizationGrid == null || civilizationButtonPrefab == null) 
                return;
            
            List<Core.CivilizationType> civilizations = civilizationManager.GetAvailableCivilizations();
            
            foreach (var civType in civilizations)
            {
                GameObject civButton = Instantiate(civilizationButtonPrefab, civilizationGrid);
                CivilizationButtonUIBasic buttonUI = civButton.GetComponent<CivilizationButtonUIBasic>();
                
                if (buttonUI != null)
                {
                    buttonUI.Setup(civType, () => OnCivilizationSelected(civType));
                }
                else
                {
                    // Fallback: setup button directly
                    Button btn = civButton.GetComponent<Button>();
                    TextMeshProUGUI text = civButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btn != null && text != null)
                    {
                        text.text = civType.ToString();
                        btn.onClick.AddListener(() => OnCivilizationSelected(civType));
                    }
                }
            }
            
            OnCivilizationSelected(Core.CivilizationType.INTJ);
        }

        private void OnCivilizationSelected(Core.CivilizationType civType)
        {
            selectedCivilization = civType;
            
            if (civilizationManager != null)
            {
                if (civilizationNameText != null)
                    civilizationNameText.text = civType.ToString();
                if (civilizationDescriptionText != null)
                    civilizationDescriptionText.text = civilizationManager.GetCivilizationDescription(civType);
            }
        }

        private void OnConfirmCivilization()
        {
            PlayerPrefs.SetInt("SelectedCivilization", (int)selectedCivilization);
            Debug.Log($"Selected civilization: {selectedCivilization}");
            ShowMainMenu();
        }
    }

    public class CivilizationButtonUIBasic : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI civNameText;
        [SerializeField] private Image civIcon;
        [SerializeField] private Button button;
        
        public void Setup(Core.CivilizationType civType, System.Action onClicked)
        {
            if (civNameText != null)
                civNameText.text = civType.ToString();
            if (button != null)
                button.onClick.AddListener(() => onClicked());
        }
    }
}