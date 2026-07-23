using UnityEngine;

public class MafiaSkill : INightSkill
{
    public string RoleId => "Mafia";
    public string RoleName => "마피아";

    public int SelectedTargetId { get; private set; } = -1;

    public void ExecuteSkill(int actorId, int targetId)
    {
        SelectedTargetId = targetId;
        Debug.Log($"<color=red>[마피아] {targetId}번 참가자를 습격 대상으로 지정했습니다.</color>");
    }

    public void ResolveSkill()
    {
        // 마피아 정산 로직은 NightManager의 의사 방어 여부 확인 후 처리
    }
}