using UnityEngine;

[CreateAssetMenu(fileName = "Stat_New", menuName = "MafiaGame/Personality Stat")]
public class PersonalityStatSO : ScriptableObject
{
    [SerializeField] private string statKey;      // 영문 식별자 (예: Purity)
    [SerializeField] private string statName;     // 한글 표시 이름 (예: 순수함)
    [TextArea]
    [SerializeField] private string description;  // 스탯에 대한 설명 가이드

    public string StatKey => statKey;
    public string StatName => statName;
    public string Description => description;
}