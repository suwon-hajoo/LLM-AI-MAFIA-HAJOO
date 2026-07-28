using UnityEngine;
using TMPro;

public class ChatController : MonoBehaviour
{
    public static ChatController Instance;
    [SerializeField]
    private GameObject textChatPrefab;
    [SerializeField]
    private Transform parentContent;
    [SerializeField]
    private TMP_InputField inputField;
    private GameDataManager gameDataManager;
    private ChatService chatService;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameDataManager = GameDataManager.Instance;
        chatService = ChatService.GetInstance();
    }

    public void UpdateChat()
    {
        if (!inputField.isFocused) return;
        string text = inputField.text;
        if (text.Equals("")) return;
        AddChat($"[나] : {text}");
        var me = gameDataManager.GetMyParticipantData();
        chatService.AddMessageByDefault(new(){role=LLMRole.User, name=me.Name, content=text});
        inputField.text = "";
    }

    public void AddChat(string text)
    {
        GameObject clone = Instantiate(textChatPrefab, parentContent);
        clone.GetComponent<TextMeshProUGUI>().text=  text;
    }
}