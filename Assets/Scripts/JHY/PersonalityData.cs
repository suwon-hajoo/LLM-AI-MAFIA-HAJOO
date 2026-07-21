using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class PersonalityData
{
    public Dictionary<PersonalityStatSO, int> Stats { get; private set; } = new Dictionary<PersonalityStatSO, int>();

    // 11개 성격 에셋을 받아서 1~100 수치를 무작위 생성
    public PersonalityData(List<PersonalityStatSO> allStatSOList)
    {
        if (allStatSOList == null || allStatSOList.Count == 0)
        {
            Debug.LogError("성격 스탯 에셋 목록이 비어있습니다!");
            return;
        }

        foreach (var statSO in allStatSOList)
        {
            Stats[statSO] = UnityEngine.Random.Range(1, 101);
        }
    }

    // 콘솔 디버그 출력용 문자열
    public string GetDebugLogString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in Stats)
        {
            sb.Append($"{kvp.Key.StatName}({kvp.Value}) ");
        }
        return sb.ToString().TrimEnd();
    }

    // 나중에 LLM에 전달할 텍스트 형태
    public string GetPromptRawStats()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[성격 스탯 수치 목록 (각 1~100점 기준)]");

        foreach (var kvp in Stats)
        {
            sb.AppendLine($"- {kvp.Key.StatName} ({kvp.Key.StatKey}): {kvp.Value}점");
        }

        return sb.ToString();
    }
}