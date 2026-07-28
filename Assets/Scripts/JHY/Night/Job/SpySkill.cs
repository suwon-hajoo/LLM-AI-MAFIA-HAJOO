using UnityEngine;

public class SpySkill : INightSkill
{
    public string RoleId => "Spy";
    public string RoleName => "스파이";

    public int SelectedTargetId { get; private set; } = -1;

    public void ExecuteSkill(int actorId, int targetId)
    {
        SelectedTargetId = targetId;
        Debug.Log($"<color=magenta>[스파이] {targetId}번 참가자에게 접촉을 시도합니다.</color>");
    }

    public void ResolveSkill()
    {
        // 정산은 밤 끝날 때 처리
    }
}