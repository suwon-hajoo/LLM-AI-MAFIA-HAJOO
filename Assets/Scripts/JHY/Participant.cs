using System;

[Serializable]
public class Participant
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public bool IsAI { get; private set; }
    public RoleData Role { get; private set; }
    public PersonalityData Personality { get; private set; } //  성격 데이터

    public bool IsAlive { get; private set; } = true;

    public Participant(int id, string name, bool isAI, System.Collections.Generic.List<PersonalityStatSO> statList)
    {
        Id = id;
        Name = name;
        IsAI = isAI;
        IsAlive = true; // 생성 시 생존 상태

        // AI인 경우 프로젝트에 등록된 성격 에셋들을 기반으로 수치 생성
        if (isAI)
        {
            Personality = new PersonalityData(statList);
        }
    }

    public void SetRole(RoleData newRole)
    {
        Role = newRole;
    }

    // 참가자 사망 처리
    public void Die()
    {
        IsAlive = false;
    }
}