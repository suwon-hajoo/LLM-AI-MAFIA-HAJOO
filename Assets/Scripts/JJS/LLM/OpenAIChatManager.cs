#nullable enable
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


public class OpenAIChatManager : MonoBehaviour
{
    private string apiKey = "";
    private string apiUrl = "";
    private string modelName = "";
    public static OpenAIChatManager? Instance {get; private set;}

    void Awake()
    {
        Instance ??= this;
    }

    void Start()
    {
        apiUrl = "https://llm-proxy.wjdwntls1225.workers.dev/";
        modelName = "openai/gpt-oss-120b";
    }

    public async Task<string?> SendChatRequest(GameConversation gameConversation, string queryMessage, string? response_format = null, bool enbaleThinking = true)
    {
        // 💡 LLM으로 실제로 날아가는 첫 번째 메시지(System Prompt) 로그 출력
        Debug.Log($"<color=orange>[LLM 실제 전송 프롬프트 내용]</color>\n{gameConversation.MessageList[0].content}");

        List<OpenAIMessage> requestMessageList = new(gameConversation.MessageList)
        {
            new() { role = "user", name = "user", content = queryMessage }
        };

        // =====================================================================================
        // 💡 [LLM 실제 최종 프롬프트] 사람이 읽기 편한 최종 전체 프롬프트 로그 생성
        // =====================================================================================
        StringBuilder cleanLogBuilder = new StringBuilder();
        foreach (var msg in requestMessageList)
        {
            // [역할 - 이름] 형태로 보기 좋게 구분선 추가
            string nameDisplay = string.IsNullOrEmpty(msg.name) ? "" : $" - {msg.name}";
            cleanLogBuilder.AppendLine($"<color=yellow>[{msg.role}{nameDisplay}]</color>");
            cleanLogBuilder.AppendLine(msg.content);
            cleanLogBuilder.AppendLine("--------------------------------------------------");
        }

        Debug.Log($"<color=magenta>[최종 완성된 전체 프롬프트 대본 (사람 읽기용)]</color>\n{cleanLogBuilder.ToString()}");
        // =====================================================================================

        OpenAIRequest requestData = new()
        {
            model = modelName,
            messages = requestMessageList,
            // json_object 일 때만 전달, 그 외엔 null 처리
            /*response_format = (response_format == "json_object")
            ? new ResponseFormat { type = "json_object" }
            : null*/
            response_format = null,
            chat_template_kwargs = new()
            {
                enable_thinking=enbaleThinking
            }
        };

        //string jsonPayload = JsonUtility.ToJson(requestData);
        // 💡 JsonUtility.ToJson 대신 JsonConvert.SerializeObject 사용 (null 값 생략 설정)
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        string jsonPayload = JsonConvert.SerializeObject(requestData, settings);
        Debug.Log(jsonPayload);
        /*// Formatting.Indented 옵션을 추가하면 JSON이 예쁘게 정렬되어 출력됩니다! [LLM 전송 편하게 확인]
        string jsonPayload = JsonConvert.SerializeObject(requestData, Formatting.Indented, settings);*/

        Debug.Log($"<color=cyan>[LLM 최종 전송 데이터]</color>\n{jsonPayload}");

        byte[] rawData = Encoding.UTF8.GetBytes(jsonPayload);
        using UnityWebRequest request = new(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(rawData);
        request.downloadHandler = new DownloadHandlerBuffer();

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API 호출 실패: {request.error}\n상세에러: {request.downloadHandler.text}");
            return null;
        }

        string jsonResponse = request.downloadHandler.text;
        OpenAIResponse responseData = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);

        if (responseData.choices != null && responseData.choices.Count > 0)
        {
            string reply = responseData.choices[0].message.content;
            Debug.Log($"<color=green>[LLM 답변]:</color> {reply}");
            return reply;
        }
        return null;
    }
}
    
