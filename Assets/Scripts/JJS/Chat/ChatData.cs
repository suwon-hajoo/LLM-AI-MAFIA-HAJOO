

using System.Collections.Generic;

public class ChatData{
    public Participant participant {get; private set;}
    public GameConversation gameConversation {get; private set;}

    // [AI System] 일반 대화와 분리된 '개인 직업 시스템 알림' 전용 보관함
    public List<string> PrivateSystemLogs { get; private set; } = new();

    public ChatData(Participant participant, GameConversation gameConversation)
    {
        this.participant = participant;
        this.gameConversation = gameConversation;
    }

    public void AddMessage(OpenAIMessage message)
    {
        gameConversation.AddItem(message);
    }

    // [AI System] 개인 알림을 리스트에 넣는 전용 메서드
    public void AddPrivateLog(string logMsg)
    {
        PrivateSystemLogs.Add(logMsg);
    }

}