using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PropUI : MonoBehaviour
    {
        [SerializeField] private GameObject propUI;
        [SerializeField] private Image healthbarFill;
        [SerializeField] private TextMeshProUGUI healthText;

        private void Update()
        {
            propUI.SetActive(NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient && 
                GameManager.Instance.State == GameManager.GameState.Running && 
                GameManager.Instance.HunterClientId != NetworkManager.Singleton.LocalClientId);
            var playerProp = PropController.AliveProps.FirstOrDefault(prop => prop.IsOwner);
            healthText.text = playerProp == null ? string.Empty : Mathf.CeilToInt(playerProp.Health).ToString();
            healthbarFill.fillAmount = playerProp == null ? 0 : playerProp.Health / playerProp.MaxHealth;
        }
    }
}