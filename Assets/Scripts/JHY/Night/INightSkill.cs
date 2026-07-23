public interface INightSkill
{
    string RoleId { get; }      // 직업 식별자 (예: Mafia, Doctor, Police)
    string RoleName { get; }    // 직업 한글 이름

    // 유저가 대상을 지정해서 능력을 발동할 때
    void ExecuteSkill(int actorId, int targetId);

    // 밤이 끝나고 아침이 될 때 결과 정산
    void ResolveSkill();
}