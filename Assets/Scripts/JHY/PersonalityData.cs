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

    public int GetStatValue(string statKeyOrName)
    {
        if (Stats == null || Stats.Count == 0)
        {
            Debug.LogWarning("[PersonalityData] Stats 딕셔너리가 비어있습니다!");
            return 50;
        }

        foreach (var kvp in Stats)
        {
            if (kvp.Key != null && (kvp.Key.StatKey == statKeyOrName || kvp.Key.StatName == statKeyOrName))
            {
                return kvp.Value; // 💡 찾은 실제 1~100 수치 반환!
            }
        }

        // 💡 키 이름을 못 찾았을 때 콘솔에 알림을 띄워 에셋 이름 확인을 유도
        Debug.LogError($"<color=red>[스탯 찾기 실패] '{statKeyOrName}' 키와 일치하는 PersonalityStatSO를 찾을 수 없어 기본값 50을 반환합니다. SO의 StatKey 또는 StatName을 확인하세요!</color>");
        return 50;
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