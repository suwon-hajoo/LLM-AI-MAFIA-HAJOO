using System;
using UnityEngine;

[Serializable]
public class Participant
{
    // 프로퍼티 캡슐화: 외부에서는 읽기만 가능 (get), 대입은 불가 (private set)
    public int Id { get; private set; }
    public string Name { get; private set; }
    public bool IsAI { get; private set; }
    public RoleData Role { get; private set; }

    public Participant(int id, string name, bool isAI)
    {
        Id = id;
        Name = name;
        IsAI = isAI;
    }

    // 역할을 설정할 수 있는 공식 검증 메서드
    public void SetRole(RoleData newRole)
    {
        if (newRole == null)
        {
            Debug.LogError("오류: 유효하지 않은 RoleData입니다.");
            return;
        }
        Role = newRole;
    }
}