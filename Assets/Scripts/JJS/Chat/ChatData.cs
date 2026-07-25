

public class ChatData{
    public Participant participant {get; private set;}
    public GameConversation gameConversation {get; private set;}

    public ChatData(Participant participant, GameConversation gameConversation)
    {
        this.participant = participant;
        this.gameConversation = gameConversation;
    }

    public void AddMessage(OpenAIMessage message)
    {
        gameConversation.AddItem(message);
    }

}