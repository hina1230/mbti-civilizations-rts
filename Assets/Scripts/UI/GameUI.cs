using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace MBTICivilizations.UI
{
    public class GameUI : NetworkBehaviour
    {
        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI foodText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI populationText;
        
        [Header("Unit Selection")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Image unitPortrait;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI unitHealthText;
        [SerializeField] private Slider unitHealthBar;
        [SerializeField] private Transform actionButtonsContainer;
        
        [Header("Building Panel")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private Transform buildingButtonsContainer;
        [SerializeField] private GameObject buildingButtonPrefab;
        
        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform minimapViewport;
        
        [Header("Game Menu")]
        [SerializeField] private GameObject gameMenuPanel;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button surrenderButton;
        [SerializeField] private Button exitButton;
        
        [Header("Notifications")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private float notificationDuration = 3f;
        
        [Header("Victory/Defeat")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button returnToMenuButton;
        
        private Core.ResourceManager resourceManager;
        private Core.GameManager gameManager;
        private List<Units.ISelectable> selectedUnits = new List<Units.ISelectable>();
        private Queue<GameObject> notificationQueue = new Queue<GameObject>();

        private void Start()
        {
            resourceManager = FindObjectOfType<Core.ResourceManager>();
            gameManager = Core.GameManager.Instance;
            
            SetupButtons();
            HideAllPanels();
            
            if (IsOwner)
            {
                StartCoroutine(UpdateResourceDisplay());
            }
        }

        private void SetupButtons()
        {
            menuButton.onClick.AddListener(ToggleGameMenu);
            resumeButton.onClick.AddListener(ResumeGame);
            settingsButton.onClick.AddListener(OpenSettings);
            surrenderButton.onClick.AddListener(Surrender);
            exitButton.onClick.AddListener(ExitToMainMenu);
            returnToMenuButton.onClick.AddListener(ExitToMainMenu);
        }

        private void HideAllPanels()
        {
            selectionPanel.SetActive(false);
            buildingPanel.SetActive(false);
            gameMenuPanel.SetActive(false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
        }

        private IEnumerator UpdateResourceDisplay()
        {
            while (true)
            {
                if (resourceManager != null && NetworkManager.Singleton.IsClient)
                {
                    var resources = resourceManager.GetPlayerResources(NetworkManager.Singleton.LocalClientId);
                    if (resources != null)
                    {
                        goldText.text = $"Gold: {resources.gold.Value}";
                        woodText.text = $"Wood: {resources.wood.Value}";
                        foodText.text = $"Food: {resources.food.Value}";
                        stoneText.text = $"Stone: {resources.stone.Value}";
                        populationText.text = $"Pop: {resources.population.Value}/{resources.populationCap.Value}";
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            
            HandleSelection();
            HandleHotkeys();
        }

        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    Units.ISelectable selectable = hit.collider.GetComponent<Units.ISelectable>();
                    
                    if (selectable != null)
                    {
                        if (!Input.GetKey(KeyCode.LeftShift))
                        {
                            ClearSelection();
                        }
                        
                        SelectUnit(selectable);
                    }
                    else if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        ClearSelection();
                    }
                }
            }
            
            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
            {
                // Box selection logic would go here
            }
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleGameMenu();
            }
            
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleGameMenu();
            }
            
            // Control groups
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        SetControlGroup(i);
                    }
                    else
                    {
                        SelectControlGroup(i);
                    }
                }
            }
        }

        private void SelectUnit(Units.ISelectable unit)
        {
            unit.Select();
            selectedUnits.Add(unit);
            UpdateSelectionPanel();
        }

        private void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                unit.Deselect();
            }
            selectedUnits.Clear();
            selectionPanel.SetActive(false);
        }

        private void UpdateSelectionPanel()
        {
            if (selectedUnits.Count > 0)
            {
                selectionPanel.SetActive(true);
                
                if (selectedUnits.Count == 1)
                {
                    var unit = selectedUnits[0] as Units.UnitBase;
                    if (unit != null)
                    {
                        unitNameText.text = unit.name;
                        // Update health and other info
                    }
                }
                else
                {
                    unitNameText.text = $"{selectedUnits.Count} Units Selected";
                }
            }
        }

        private Dictionary<int, List<Units.ISelectable>> controlGroups = new Dictionary<int, List<Units.ISelectable>>();

        private void SetControlGroup(int groupNumber)
        {
            controlGroups[groupNumber] = new List<Units.ISelectable>(selectedUnits);
            ShowNotification($"Control Group {groupNumber} Set");
        }

        private void SelectControlGroup(int groupNumber)
        {
            if (controlGroups.ContainsKey(groupNumber))
            {
                ClearSelection();
                foreach (var unit in controlGroups[groupNumber])
                {
                    if (unit != null)
                    {
                        SelectUnit(unit);
                    }
                }
            }
        }

        private void ToggleGameMenu()
        {
            gameMenuPanel.SetActive(!gameMenuPanel.activeSelf);
            Time.timeScale = gameMenuPanel.activeSelf ? 0f : 1f;
        }

        private void ResumeGame()
        {
            gameMenuPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void OpenSettings()
        {
            // Open settings panel
        }

        private void Surrender()
        {
            if (IsOwner)
            {
                SurrenderServerRpc();
            }
        }

        [ServerRpc]
        private void SurrenderServerRpc()
        {
            ShowDefeatClientRpc(new ClientRpcParams 
            { 
                Send = new ClientRpcSendParams 
                { 
                    TargetClientIds = new[] { OwnerClientId } 
                } 
            });
        }

        private void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            GameObject notification = Instantiate(notificationPrefab, notificationContainer);
            TextMeshProUGUI text = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }
            
            Image background = notification.GetComponent<Image>();
            if (background != null)
            {
                switch (type)
                {
                    case NotificationType.Info:
                        background.color = new Color(0.2f, 0.2f, 0.8f, 0.8f);
                        break;
                    case NotificationType.Warning:
                        background.color = new Color(0.8f, 0.8f, 0.2f, 0.8f);
                        break;
                    case NotificationType.Error:
                        background.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
                        break;
                    case NotificationType.Success:
                        background.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
                        break;
                }
            }
            
            StartCoroutine(RemoveNotificationAfterDelay(notification, notificationDuration));
        }

        private IEnumerator RemoveNotificationAfterDelay(GameObject notification, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            Animator animator = notification.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("FadeOut");
                yield return new WaitForSeconds(0.5f);
            }
            
            Destroy(notification);
        }

        [ClientRpc]
        public void ShowVictoryClientRpc(ClientRpcParams clientRpcParams = default)
        {
            victoryPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        [ClientRpc]
        public void ShowDefeatClientRpc(ClientRpcParams clientRpcParams = default)
        {
            defeatPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void UpdateMinimap(RenderTexture minimapTexture)
        {
            if (minimapImage != null && minimapTexture != null)
            {
                minimapImage.texture = minimapTexture;
            }
        }

        public enum NotificationType
        {
            Info,
            Warning,
            Error,
            Success
        }
    }
}