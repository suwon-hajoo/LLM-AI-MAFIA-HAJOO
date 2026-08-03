#nullable enable
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ChatController : MonoBehaviour
{
    public static ChatController? Instance;

    [Header("UI 연결")]
    [SerializeField] private TMP_InputField? inputField;
    [SerializeField] private ChatManager? chatManager; // 💡 새로 만든 ChatManager 참조 연결!

    private GameDataManager? gameDataManager;
    private ChatService? chatService;

    private readonly Regex MentionRegex = new(@"@(?<nickname>[a-zA-Z0-9가-힣_]+)", RegexOptions.Compiled);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameDataManager = GameDataManager.Instance;
        chatService = ChatService.GetInstance();
    }

    private string? getMentionedParticipant(string text)
    {
        Match match = MentionRegex.Match(text);
        if (!match.Success) return null;
        return match.Groups["nickname"].Value;
    }

    // 💡 유저가 입력창에서 엔터를 치거나 [전송] 버튼을 눌렀을 때 호출하는 함수
    public void UpdateChat()
    {
        if (inputField == null) return;

        string text = inputField.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        var me = gameDataManager!.GetMyParticipantData();

        // 1. 멘션 처리 (@이름)
        var mentionedParticipant = getMentionedParticipant(text);
        if (mentionedParticipant != null)
        {
            AITalkScheduler.Instance!.AddMentionedParticipant(mentionedParticipant);
            Debug.Log($"{mentionedParticipant}을 언급했습니다.");
        }

        // 2. ChatService에 유저 대화 저장
        chatService!.AddMessageByDefault(new() { role = LLMRole.User, name = me.Name, content = text });

        // 3. 🔥 메신저 UI에 유저 말풍선 생성!
        if (chatManager != null)
        {
            chatManager.CreateUserMessage(me.Name, text);
        }
        else if (ChatManager.Instance != null)
        {
            ChatManager.Instance.CreateUserMessage(me.Name, text);
        }

        // 4. 입력창 비우기
        inputField.text = "";
        inputField.ActivateInputField(); // 입력창 다시 포커스
    }

    // 💡 AI 및 다른 시스템에서 텍스트를 추가할 때 호출되는 함수
    public void AddChat(string senderName, string content)
    {
        // 구형 텍스트 생성 대신 ChatManager 말풍선 생성 호출
        if (chatManager != null)
        {
            chatManager.CreateAIMessage(senderName, content);
        }
        else if (ChatManager.Instance != null)
        {
            ChatManager.Instance.CreateAIMessage(senderName, content);
        }
    }
}