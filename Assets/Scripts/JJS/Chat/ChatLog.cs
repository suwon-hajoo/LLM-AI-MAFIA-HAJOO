#nullable enable
using System.Collections.Generic;

public class ChatLog
{
    public Participant Participant {get; private set;}

    public string Content {get; private set;}

    public List<int> AllowedPlayerIdList {get;}
    public List<string> AllowedRoleList {get;}

    public ChatLog(Participant participant, string content, List<int>? allowedPlayerIdList = null, List<string>? allowedRoleList = null)
    {
        Participant = participant;
        Content = content;
        AllowedPlayerIdList = allowedPlayerIdList ?? new();
        AllowedRoleList = allowedRoleList ?? new();
    }

}