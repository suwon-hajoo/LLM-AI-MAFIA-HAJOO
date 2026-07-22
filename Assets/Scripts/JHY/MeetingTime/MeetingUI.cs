using UnityEngine;
using TMPro;

public class MeetingUI : MonoBehaviour
{
    [SerializeField] private GameObject AiInformPanel;

    [Header("MeetingTimer 부분")]
    [SerializeField] private MeetingTimer meetingTimer;
    [SerializeField] private TextMeshProUGUI timerText;

    private void Update()
    {
        TimerUpdateUI();
    }

    public void OpenAiInformationButton()
    {
        if (AiInformPanel != null)
            AiInformPanel.SetActive(true);
    }

    public void CloseAiInformationButton()
    {
        if (AiInformPanel != null)
            AiInformPanel.SetActive(false);
    }

    public void MettingSkipButton()
    {
        meetingTimer.SkipMetting();
    }

    private void TimerUpdateUI()
    {
        timerText.text = $"{meetingTimer.minutes:00}:{meetingTimer.seconds:00}";
    }
}
