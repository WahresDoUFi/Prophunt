using TMPro;
using UnityEngine;

namespace UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameUI;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI propCountText;

        private void Update()
        {
            gameUI.SetActive(GameManager.Instance.State == GameManager.GameState.Running);
            timerText.text = SecondsToClockDisplay(GameManager.Instance.HunterSpawnDelay > 0 ? GameManager.Instance.HunterSpawnDelay : GameManager.Instance.RemainingTime);
            propCountText.text = PropController.GetPropCount.ToString();
        }

        private static string SecondsToClockDisplay(float time)
        {
            int seconds = Mathf.CeilToInt(time);
            int minutes = seconds / 60;
            seconds %= 60;
            return minutes + ":" + (seconds > 10 ? seconds : "0" + seconds);
        }
    }
}
