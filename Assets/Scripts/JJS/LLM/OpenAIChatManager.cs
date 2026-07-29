#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;


public class OpenAIChatManager : MonoBehaviour
{
    private string apiKey = "";
    private string apiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";
    public static OpenAIChatManager? Instance {get; private set;}

    void Awake()
    {
        EnvLoader.Load();
        apiKey = EnvLoader.Get("Open_AI_API_Key") ?? "";
        Instance ??= this;
    }

    void Start()
    {
        
    }

    public async Task<string?> SendChatRequest(GameConversation gameConversation, string queryMessage, string? response_format = null)
    {
        // 💡 LLM으로 실제로 날아가는 첫 번째 메시지(System Prompt) 로그 출력
        Debug.Log($"<color=orange>[LLM 실제 전송 프롬프트 내용]</color>\n{gameConversation.MessageList[0].content}");

        List<OpenAIMessage> requestMessageList = new(gameConversation.MessageList)
        {
            new() { role = "user", name = "user", content = queryMessage }
        };
        OpenAIRequest requestData = new()
        {
            model = "google/diffusiongemma-26b-a4b-it",
            messages = requestMessageList,
            // json_object 일 때만 전달, 그 외엔 null 처리
            /*response_format = (response_format == "json_object")
            ? new ResponseFormat { type = "json_object" }
            : null*/
            response_format = null
        };

        //string jsonPayload = JsonUtility.ToJson(requestData);
        // 💡 JsonUtility.ToJson 대신 JsonConvert.SerializeObject 사용 (null 값 생략 설정)
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        string jsonPayload = JsonConvert.SerializeObject(requestData, settings);
        Debug.Log(jsonPayload);
        byte[] rawData = Encoding.UTF8.GetBytes(jsonPayload);

        using UnityWebRequest request = new(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(rawData);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

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
    
