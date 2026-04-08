using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI 
{     
    public class GameManagerUI : MonoBehaviour
    {
        public static string JoinCode;
        public static int MaxPlayers;

        private const string SearchTimeDescription = "Search Time: {0}";
        private const string HideTimeDescription = "Hide Time: {0}";
        private const string TauntFrequencyDescription = "Taunt Frequency: {0}";
        private const string MaxRerollDescription = "Max Rerolls: {0}";
        private const string PlayerCountDescription = "Players: {0}/{1}";

        private const int searchTimeMultiplier = 15;
        private const int hideTimeMultiplier = 5;
        private const int tauntFrequencyMultiplier = 10;

        [Header("References")]
        [SerializeField] private GameObject lobbyUI;
        [SerializeField] private GameObject settingsUI;
        [SerializeField] private Slider searchTimeSlider;
        [SerializeField] private TextMeshProUGUI searchTimeText;
        [SerializeField] private Slider hideTimeSlider;
        [SerializeField] private TextMeshProUGUI hideTimeText;
        [SerializeField] private Slider tauntFrequencySlider;
        [SerializeField] private TextMeshProUGUI tauntFrequencyText;
        [SerializeField] private Slider maxRerollsSlider;
        [SerializeField] private TextMeshProUGUI maxRerollsText;
        [SerializeField] private TextMeshProUGUI joinCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveGameButton;
        [SerializeField] private GameObject hunterWinScreen;
        [SerializeField] private GameObject propsWinScreen;

        private void Awake()
        {
            SetupBindings();
            lobbyUI.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance == null || NetworkManager.Singleton == null) return;
            var uiActive = lobbyUI.activeSelf;
            lobbyUI.SetActive(GameManager.Instance.State == GameManager.GameState.Waiting);
            if (uiActive != lobbyUI.activeSelf)
            {
                if (lobbyUI.activeSelf)
                {
                    CursorManager.RequestCursor();
                } else
                {
                    CursorManager.ReturnCursor();
                }
            }
            settingsUI.SetActive(NetworkManager.Singleton.IsHost);
            hunterWinScreen.SetActive(GameManager.Instance.WinningTeam == GameManager.Team.Hunter);
            propsWinScreen.SetActive(GameManager.Instance.WinningTeam == GameManager.Team.Props);
            joinCodeText.text = JoinCode;
            playerCountText.text = NetworkManager.Singleton.IsHost ? 
                string.Format(PlayerCountDescription, NetworkManager.Singleton.ConnectedClientsIds.Count, MaxPlayers) :
                string.Empty;
        }

        private void SetupBindings()
        {
            searchTimeSlider.onValueChanged.AddListener(SearchTimeChanged);
            hideTimeSlider.onValueChanged.AddListener(HideTimeChanged);
            tauntFrequencySlider.onValueChanged.AddListener(TauntFrequencyChanged);
            maxRerollsSlider.onValueChanged.AddListener(MaxRerollsChanged);
            startGameButton.onClick.AddListener(StartGameButtonClicked);
            leaveGameButton.onClick.AddListener(() => NetworkManager.Singleton.Shutdown());
        }

        private void SearchTimeChanged(float time)
        {
            searchTimeText.text = string.Format(SearchTimeDescription, SecondsToTimeText((int)time * searchTimeMultiplier));
        }

        private void HideTimeChanged(float time)
        {
            hideTimeText.text = string.Format(HideTimeDescription, SecondsToTimeText((int)time * hideTimeMultiplier));
        }

        private void TauntFrequencyChanged(float time)
        {
            tauntFrequencyText.text = string.Format(TauntFrequencyDescription, SecondsToTimeText((int)time * tauntFrequencyMultiplier));
        }

        private void MaxRerollsChanged(float rerolls)
        {
            maxRerollsText.text = string.Format(MaxRerollDescription, rerolls);
        }

        private void StartGameButtonClicked()
        {
            GameManager.Instance.InitializeGame(
                searchTimeSlider.value * searchTimeMultiplier,
                hideTimeSlider.value * hideTimeMultiplier,
                tauntFrequencySlider.value * tauntFrequencyMultiplier,
                (byte)maxRerollsSlider.value);
        }

        public static string SecondsToTimeText(int seconds)
        {
            StringBuilder result = new StringBuilder();
            int minutes = seconds / 60;
            if (minutes > 0)
                result.Append(minutes + "min ");
            result.Append((seconds % 60) + "sec");
            return result.ToString();
        }
    }
}
