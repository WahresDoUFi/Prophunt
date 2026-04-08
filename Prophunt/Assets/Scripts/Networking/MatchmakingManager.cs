using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

namespace Networking
{
    public class MatchmakingManager : MonoBehaviour
    {
        private const string ConnectionType = "dtls";
        private const string MaxPlayersDescription = "Max Players: {0}";

        [Header("References")]
        [SerializeField] private GameObject lobbyUI;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeInputField;
        [SerializeField] private Slider maxPlayersSlider;
        [SerializeField] private TextMeshProUGUI maxPlayersText;

        private IEnumerator Start()
        {
            CursorManager.RequestCursor();
            InitializeCallbacks();
            SetButtonsEnabled(false);
            var serviceTask = UnityServices.InitializeAsync();
            while (!serviceTask.IsCompleted) yield return null;
            yield return new WaitForSeconds(0.5f);
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                var loginTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
                while (!loginTask.IsCompleted) yield return null;
            }
            Debug.Log("Connected and ready");
            SetButtonsEnabled(true);
        }

        private void InitializeCallbacks()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnect;
            NetworkManager.Singleton.OnClientStopped += _ => OnDisconnect();

            joinButton.onClick.AddListener(JoinButtonClicked);
            hostButton.onClick.AddListener(HostButtonClicked);
            maxPlayersSlider.onValueChanged.AddListener(MaxPlayersChanged);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            hostButton.interactable = enabled;
            joinButton.interactable = enabled;
        }

        private async Task Host()
        {
            SetButtonsEnabled(false);
            string code = await StartHostWithRelay();
            if (code.IsNullOrEmpty())
            {
                Debug.Log("Could not start host");
                SetButtonsEnabled(true);
                return;
            }
            Debug.Log("Join code is: " + code);
        }

        private void HostButtonClicked()
        {
            _ = Host();
        }

        private async Task Join()
        {
            SetButtonsEnabled(false);
            string code = joinCodeInputField.text;
            if (code.IsNullOrEmpty())
            {
                SetButtonsEnabled(true);
                return;
            }
            if (!await StartClientWithRelay(code))
            {
                Debug.Log("Could not start client");
                SetButtonsEnabled(true);
            }
        }

        private void JoinButtonClicked()
        {
            _ = Join();
        }

        private void MaxPlayersChanged(float maxPlayers)
        {
            maxPlayersText.text = string.Format(MaxPlayersDescription, maxPlayers);
        }

        private async Task<string> StartHostWithRelay()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            var allocation = await RelayService.Instance.CreateAllocationAsync((int)maxPlayersSlider.value);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }

        private async Task<bool> StartClientWithRelay(string joinCode)
        {
            if (joinCode.IsNullOrEmpty()) return false;
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));
            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }

        private void OnDisconnect()
        {
            SetButtonsEnabled(true);
            lobbyUI.SetActive(true);
            CursorManager.RequestCursor();
        }

        private void OnConnect(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;
            SetButtonsEnabled(false);
            lobbyUI.SetActive(false);
            CursorManager.ReturnCursor();
        }
    }
}
