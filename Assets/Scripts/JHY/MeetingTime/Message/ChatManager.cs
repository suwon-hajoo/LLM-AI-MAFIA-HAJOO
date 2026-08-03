using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    // 💡 싱글톤 인스턴스 선언
    public static ChatManager Instance { get; private set; }

    [Header("스크롤 뷰 및 컨테이너")]
    [SerializeField] private ScrollRect scrollRect;   // Chat_Log에 있는 Scroll Rect
    [SerializeField] private Transform chatContainer; // Container 오브젝트

    [Header("메시지 프리팹")]
    [SerializeField] private GameObject aiMessagePrefab;   // AI_Message 프리팹
    [SerializeField] private GameObject userMessagePrefab; // User_Message 프리팹

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// AI의 메시지를 화면에 말풍선으로 생성
    /// </summary>
    public void CreateAIMessage(string senderName, string content)
    {
        Debug.Log($"<color=orange>[ChatManager] AI 메시지 생성 요청 들어옴!</color> 발신자: {senderName}, 내용: {content}");
        CreateMessageBubble(aiMessagePrefab, senderName, content);
    }

    /// <summary>
    /// 유저의 메시지를 화면에 말풍선으로 생성
    /// </summary>
    public void CreateUserMessage(string senderName, string content)
    {
        CreateMessageBubble(userMessagePrefab, senderName, content);
    }

    private void CreateMessageBubble(GameObject prefab, string senderName, string content)
    {
        if (prefab == null || chatContainer == null) return;

        // 1. Container 하위에 프리팹 생성
        GameObject bubbleObj = Instantiate(prefab, chatContainer);

        // 2. ChatMessageUI 스크립트로 텍스트 바인딩
        if (bubbleObj.TryGetComponent<ChatMessageUI>(out var messageUI))
        {
            messageUI.SetMessage(senderName, content);
        }

        // 3. 최하단 스크롤 이동
        ScrollToBottom();
    }

    public void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}