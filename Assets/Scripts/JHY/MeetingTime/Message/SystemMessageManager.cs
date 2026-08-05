#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

public class SystemMessageManager : MonoBehaviour
{
    public static SystemMessageManager? Instance { get; private set; }

    [Header("UI 및 프리팹 연결")]
    [SerializeField] private Transform? chatContentPanel;
    [SerializeField] private GameObject? systemMessagePrefab;
    [SerializeField] private ScrollRect? chatScrollRect;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 💡 기존 일반 메시지 출력 함수
    public void AddSystemMessage(string messageContent)
    {
        if (chatContentPanel == null || systemMessagePrefab == null) return;

        GameObject newMsgObj = Instantiate(systemMessagePrefab, chatContentPanel);
        ChatMessageUI msgUI = newMsgObj.GetComponent<ChatMessageUI>();

        if (msgUI != null)
        {
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            msgUI.SetMessage("", messageContent, currentTime);
        }

        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // 🌟 [추가] 일차(day) 수치를 받아서 자동으로 한글 날짜 문구로 조합해 주는 편리한 함수!
    public void AddDaySystemMessage(int day, string suffixMessage)
    {
        string dayText = day switch
        {
            1 => "첫 째",
            2 => "둘 째",
            3 => "셋 째",
            4 => "넷 째",
            _ => $"{day} 번째"
        };

        AddSystemMessage($"{dayText} 날 {suffixMessage}");
    }
}