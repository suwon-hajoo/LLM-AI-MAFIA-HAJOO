using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MeetingUI : MonoBehaviour
{
    [SerializeField] private GameObject AiInformPanel;

    [Header("MeetingTimer 부분")]
    [SerializeField] private MeetingTimer meetingTimer;
    [SerializeField] private TextMeshProUGUI timerText;

    // 💡 [추가] 채팅 관련 UI 및 스크립트 연결
    [Header("채팅 전송 부분")]
    [SerializeField] private Button sendButton;        // 전송 버튼 (왼쪽 아이콘/버튼)
    [SerializeField] private TMP_InputField inputField;// 입력창 (엔터키 처리용)
    [SerializeField] private ChatController chatController;

    private void Start()
    {
        // 💡 [AddListener] 코드에서 전송 버튼 클릭 이벤트 동적 등록!
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnClickSendMessageButton);
        }

        // 💡 (선택) 입력창에서 엔터키(Submit)를 쳤을 때도 메시지가 가도록 AddListener 처리
        if (inputField != null)
        {
            inputField.onSubmit.AddListener((text) => OnClickSendMessageButton());
        }
    }


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

    // 전송 버튼 클릭 및 엔터 키 입력 시 호출할 메서드
    public void OnClickSendMessageButton()
    {
        if (chatController != null)
        {
            // ChatController의 전송 로직 실행
            chatController.UpdateChat();
        }
        else
        {
            Debug.LogWarning("ChatController가 MeetingUI에 연결되지 않았습니다.");
        }
    }

    // 💡 [메모리 누수 방지] 오브젝트 파괴 시 리스너 해제
    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnClickSendMessageButton);
        }
        if (inputField != null)
        {
            inputField.onSubmit.RemoveAllListeners();
        }
    }
}
