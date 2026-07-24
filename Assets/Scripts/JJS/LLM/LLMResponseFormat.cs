/**
LLM에게 ResponseFormat을 강제화하기 위한 값을 오류 없이 작성하기 위한 클래스입니다.
*/
public class LLMResponseFormat
{
    // 대답을 유효한 JSON 구조로 대답하게 강제합니다.
    public static string JsonObject = "json_object";
    // 개발자가 지정한 C# 클래스 구조와 100% 일치하는 JSON만 나오게 강제합니다.
    public static string JsonSchema = "json_schema";
    // 아무런 제약을 두지 않고 LLM이 자유롭게 대화체를 섞어서 답변합니다.
    public static string Text = "text";
}