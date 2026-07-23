

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
            messages.Add(chatLog.toDto(participant));
        }
        return messages;
    }
}