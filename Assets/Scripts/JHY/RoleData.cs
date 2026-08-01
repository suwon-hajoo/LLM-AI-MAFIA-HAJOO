using UnityEngine;

public enum Team
{
    Citizen, // 시민 진영
    Mafia,   // 마피아 진영
    Neutral  // 중립 진영
}

[CreateAssetMenu(fileName = "NewRoleData", menuName = "MafiaGame/Role Data")]
public class RoleData : ScriptableObject
{
    [Header("역할 기본 정보")]
    [SerializeField] private string roleId = "Citizen";
    [SerializeField] private string roleName = "시민";
    [SerializeField] private Team team = Team.Citizen;
    [SerializeField] private int maxCountInGame = 1;

    [Header("역할 일러스트")]
    [SerializeField] private Sprite roleicon;

    [TextArea]
    [SerializeField] private string description;

    // 캡슐화: 외부에서는 읽기 전용 프로퍼티로 접근
    public string RoleId => roleId;
    public string RoleName => roleName;
    public Team Team => team;
    public int MaxCountInGame => maxCountInGame;
    public Sprite RoleIcon => roleicon;
    public string Description => description;
}
