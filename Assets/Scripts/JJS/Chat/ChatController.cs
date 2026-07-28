#nullable enable
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ChatController : MonoBehaviour
{
    public static ChatController? Instance;
    [SerializeField]
    private GameObject? textChatPrefab;
    [SerializeField]
    private Transform? parentContent;
    [SerializeField]
    private TMP_InputField? inputField;
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

    public void UpdateChat()
    {
        if (!inputField!.isFocused) return;
        string text = inputField.text;
        if (text.Equals("")) return;
        AddChat($"[나] : {text}");
        var me = gameDataManager!.GetMyParticipantData();
        var mentionedParticipant = getMentionedParticipant(text);
        if (mentionedParticipant != null) 
        {
            AITalkScheduler.Instance!.AddMentionedParticipant(mentionedParticipant);
            Debug.Log($"{mentionedParticipant}을 언급했습니다.");
        }
        chatService!.AddMessageByDefault(new(){role=LLMRole.User, name=me.Name, content=text});
        inputField.text = "";
    }

    public void AddChat(string text)
    {
        GameObject? clone = Instantiate(textChatPrefab, parentContent);
        clone!.GetComponent<TextMeshProUGUI>().text=  text;
    }
}