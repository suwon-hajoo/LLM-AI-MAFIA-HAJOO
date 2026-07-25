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

    // [추가] response_format 필드 추가!
    public ResponseFormat response_format;
}

// [추가] JSON 포맷을 전달하기 위한 DTO 클래스 정의
[Serializable]
public class ResponseFormat
{
    public string type; // 예: "json_object"
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
    

