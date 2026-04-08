using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public class HunterUI : MonoBehaviour
    {
        private const string SpawnDelayText = "You will spawn in: {0}";

        [SerializeField] private GameObject hunterUI;
        [SerializeField] private GameObject spawnDelayScreen;
        [SerializeField] private TextMeshProUGUI spawnDelayText;

        private void Update()
        {
            if (GameManager.Instance == null || NetworkManager.Singleton == null) return;
            hunterUI.SetActive(GameManager.Instance.HunterClientId == NetworkManager.Singleton.LocalClientId);
            spawnDelayScreen.SetActive(GameManager.Instance.HunterSpawnDelay > 0);
            spawnDelayText.text = string.Format(SpawnDelayText, GameManagerUI.SecondsToTimeText(Mathf.CeilToInt(GameManager.Instance.HunterSpawnDelay)));
        }
    }
}
