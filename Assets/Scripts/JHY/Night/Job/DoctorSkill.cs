using UnityEngine;

public class DoctorSkill : INightSkill
{
    public string RoleId => "Doctor";
    public string RoleName => "의사";

    public int ProtectedTargetId { get; private set; } = -1;

    public void ExecuteSkill(int actorId, int targetId)
    {
        ProtectedTargetId = targetId;
        Debug.Log($"<color=green>[의사] {targetId}번 참가자를 보호했습니다.</color>");
    }

    public void ResolveSkill()
    {
        // 정산 로직
    }
}