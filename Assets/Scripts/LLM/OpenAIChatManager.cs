using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;


public class OpenAIChatManager: MonoBehaviour
{
    private string apiKey = "";
    private string apiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";

    void Start()
    {
        
    }

    public IEnumerator SendChatRequest(List<OpenAIMessage> message_list, string userPrompt)
    {
        OpenAIRequest requestData = new()
        {
            model = "google/diffusiongemma-26b-a4b-it",
            messages = message_list
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] rawData = Encoding.UTF8.GetBytes(jsonPayload);

        using UnityWebRequest request = new(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(rawData);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API 호출 실패: {request.error}\n상세에러: {request.downloadHandler.text}");
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        OpenAIResponse responseData = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);

        if (responseData.choices != null && responseData.choices.Count > 0)
        {
            string reply = responseData.choices[0].message.content;
            Debug.Log($"<color=green>[LLM 답변]:</color> {reply}");
        }
    }
}
    
