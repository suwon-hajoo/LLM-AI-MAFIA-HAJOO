using UnityEngine;

public class CitizenSkill : INightSkill
{
    public string RoleId => "Citizen";
    public string RoleName => "시민";

    public void ExecuteSkill(int actorId, int targetId)
    {
        Debug.Log("[시민] 밤 동안 별도의 능력을 사용할 수 없습니다.");
    }

    public void ResolveSkill() { }
}