

using System.Collections.Generic;

public static class ChatExtension
{
    public static OpenAIMessage toDto(this ChatLog chatLog, Participant participant)
    {
        return new OpenAIMessage()
        {
            role = chatLog.Participant.Id == participant.Id ? "assistant":"user",
            content = chatLog.Content,
            name = chatLog.Participant.Name
        };
    }

    public static List<OpenAIMessage> toDtoList(this ChatService chatService, Participant participant)
    {
        List<OpenAIMessage> messages = new();
        foreach (ChatLog chatLog in chatService.ChatLogList)
        {
            if (chatLog.AllowedPlayerIdList.Count > 0 && !chatLog.AllowedPlayerIdList.Contains(participant.Id)) continue;
            if (chatLog.AllowedRoleList.Count > 0 && !chatLog.AllowedRoleList.Contains(participant.Role.RoleId)) continue;
            messages.Add(chatLog.toDto(participant));
        }
        return messages;
    }
}