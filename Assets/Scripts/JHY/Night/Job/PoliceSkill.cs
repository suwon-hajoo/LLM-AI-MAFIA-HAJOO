using UnityEngine;

public class PoliceSkill : INightSkill
{
    public string RoleId => "Police";
    public string RoleName => "경찰";

    public int InspectedTargetId { get; private set; } = -1;

    public void ExecuteSkill(int actorId, int targetId)
    {
        InspectedTargetId = targetId;

        // 조사 대상 정보 가져오기
        Participant target = null;
        foreach (var p in GameDataManager.Instance.Participants)
        {
            if (p.Id == targetId) { target = p; break; }
        }

        if (target != null)
        {
            bool isMafia = target.Role.RoleId == "Mafia";
            Debug.Log($"<color=cyan>[경찰 조사] {target.Name} 님은 {(isMafia ? "마피아입니다!" : "마피아가 아닙니다.")}</color>");
        }
    }

    public void ResolveSkill() { }
}