using UnityEngine;
using TMPro;

public class ChatController : MonoBehaviour
{
    [SerializeField]
    private GameObject textChatPrefab;
    [SerializeField]
    private Transform parentContent;
    [SerializeField]
    private TMP_InputField inputField;
    private OpenAIChatManager openAIChatManager;

    // 테스트용
    private GameConversation gameConversation;

    void Start()
    {
        openAIChatManager = OpenAIChatManager.Instance;
        gameConversation = new("한국어로 대답해줘");
        
    }

    public void UpdateChat()
    {
        if (!inputField.isFocused) return;
        if (inputField.text.Equals("")) return;

        GameObject clone = Instantiate(textChatPrefab, parentContent);
        clone.GetComponent<TextMeshProUGUI>().text=  $"[나] : {inputField.text}";
        // MakeAnswer(inputField.text);
        inputField.text = "";
    }

    // 이건 테스트용
    private async void MakeAnswer(string question)
    {
        gameConversation.Add("user", "user", question);
        string answer = await openAIChatManager.SendChatRequest(gameConversation);
        gameConversation.Add("assistant", "assistant", answer);
        GameObject clone = Instantiate(textChatPrefab, parentContent);
        clone.GetComponent<TextMeshProUGUI>().text=$"[AI] : {answer}";
    }
}