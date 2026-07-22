#nullable enable
using System.Collections.Generic;
using Unity.VisualScripting;

public class GameConversation
{
    public readonly List<OpenAIMessage> MessageList;

    public GameConversation(string systemPrompt)
    {
        MessageList = new()
        {
            new OpenAIMessage() { role = "system", name = "system", content = systemPrompt }
        };
    }

    public void UpdateSystemPrompt(string newPrompt)
    {
        if (MessageList.Count > 0 && MessageList[0].role == "system")
        {
            MessageList[0].content = newPrompt;
        }
    }

    public void Add(string role, string name, string content)
    {
        MessageList.Add(new(){role=role, name=name, content=content});
    }
}