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
}