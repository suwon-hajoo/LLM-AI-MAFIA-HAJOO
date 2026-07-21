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

    public void UpdateChat()
    {
        if (inputField.text.Equals("")) return;

        GameObject clone = Instantiate(textChatPrefab, parentContent);
        clone.GetComponent<TextMeshProUGUI>().text=  $"{inputField.text}";
        inputField.text = "";
    }
}