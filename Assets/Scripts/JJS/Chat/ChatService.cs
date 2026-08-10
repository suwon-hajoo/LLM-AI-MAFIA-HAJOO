#nullable enable

using System.Collections.Generic;

public class ChatService
{
    static ChatService? Instance;
    public readonly List<ChatData> ChatDataList = new();

    private ChatService(){}

    public static ChatService GetInstance()
    {
        Instance ??= new();
        return Instance;
    }

    public void AddChatData(Participant participant, string systemPrompt)
    {
        ChatDataList.Add(new(participant, new(systemPrompt)));
    }
    public void ClearChatData()
    {
        ChatDataList.Clear();
    }

    public GameConversation? GetGameConversationById(int participantId)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Id == participantId)
            {
                return chatData.gameConversation;
            }
        }
        return null;
    }

    public void AddMessageById(int participantId, OpenAIMessage message)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Id == participantId)
            {
                chatData.AddMessage(message);
                return;
            }
        }
    }

    private OpenAIMessage ConvertRolesForCurrentAI(Participant participant, OpenAIMessage message)
    {
        if (participant.Name == message.name)
        {
            return new(){role=LLMRole.Assistant, name=message.name, content=message.content};
        }
        return message;
    }

    public void AddMessageByTeam(Team team, OpenAIMessage message)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Role.Team == team)
            {
                OpenAIMessage fixedMessage = ConvertRolesForCurrentAI(chatData.participant, message);
                chatData.AddMessage(fixedMessage);
            }
        }
    }

    public void AddMessageByRole(string roleId, OpenAIMessage message)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Role.RoleId == roleId)
            {
                OpenAIMessage fixedMessage = ConvertRolesForCurrentAI(chatData.participant, message);
                chatData.AddMessage(fixedMessage);
            }
        }
    }

    public void AddMessageByDefault(OpenAIMessage message)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            OpenAIMessage fixedMessage = ConvertRolesForCurrentAI(chatData.participant, message);
            chatData.AddMessage(fixedMessage);
        }
    }

    // [AI System] 특정 AI의 비밀 일지에 텍스트 추가하기
    public void AddPrivateSystemLogById(int participantId, string logMessage)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Id == participantId)
            {
                chatData.AddPrivateLog(logMessage);
                return;
            }
        }
    }

    // [AI System] 특정 AI의 비밀 일지 텍스트 리스트 가져오기 (프롬프트 조립할 때 사용)
    public List<string>? GetPrivateSystemLogsById(int participantId)
    {
        foreach (ChatData chatData in ChatDataList)
        {
            if (chatData.participant.Id == participantId)
            {
                return chatData.PrivateSystemLogs;
            }
        }
        return null;
    }
}