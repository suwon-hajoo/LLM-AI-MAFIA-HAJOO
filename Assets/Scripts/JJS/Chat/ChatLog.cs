
using System;

public class ChatLog
{
    public Participant Participant {get; private set;}

    public string Content {get; private set;}

    public ChatLog(Participant participant, string content)
    {
        Participant = participant;
        Content = content;
    }

}