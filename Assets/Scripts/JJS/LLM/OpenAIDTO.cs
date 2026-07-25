using System;
using System.Collections.Generic;



[Serializable]
public class OpenAIRequest
{
    public string model;
    public List<OpenAIMessage> messages;
    public int max_tokens = 4096;
    public int temperature = 1;
    public double top_p = 0.95;
}


[Serializable]
public class OpenAIMessage
{
    public string role;
    public string name;
    public string content;
}

[Serializable]
public class OpenAIResponse
{
    public List<OpenAIChoice> choices;
}

[Serializable]
public class OpenAIChoice
{
    public OpenAIMessage message;
}
    

