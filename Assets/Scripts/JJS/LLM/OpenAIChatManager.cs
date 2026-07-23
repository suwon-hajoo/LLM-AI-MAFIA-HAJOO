#nullable enable
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

    async void Start()
    {
        
    }

    public async Task<string?> SendChatRequest(GameConversation gameConversation)
    {
        OpenAIRequest requestData = new()
        {
            model = "google/diffusiongemma-26b-a4b-it",
            messages = gameConversation.MessageList
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log(jsonPayload);
        byte[] rawData = Encoding.UTF8.GetBytes(jsonPayload);

        using UnityWebRequest request = new(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(rawData);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        await request.SendWebRequest();

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
    
