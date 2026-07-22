using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NameList_Default", menuName = "MafiaGame/Name List")]
public class NameListSO : ScriptableObject
{
    [SerializeField] private List<string> names = new List<string>();

    public List<string> Names => names;

    // 무작위로 이름 하나 뽑아오기 (랜덤 버튼용)
    public string GetRandomName()
    {
        if (names == null || names.Count == 0) return "플레이어";
        int randomIndex = Random.Range(0, names.Count);
        return names[randomIndex];
    }
}