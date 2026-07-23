

using System.Collections.Generic;

public class ChatService
{
    static ChatService Instance;
    public readonly List<ChatLog> ChatLogList;

    private ChatService()
    {
        ChatLogList = new();
    }

    public static ChatService GetInstance()
    {
        if (Instance != null) return Instance;
        Instance = new();
        return Instance;
    }

    public void Add(Participant participant, string content)
    {
        ChatLogList.Add(new(participant, content));
    }
}