using System.Collections.Generic;
using UnityEngine;

public class RoleAssigner
{
    public void AssignRoles(List<Participant> participants, List<RoleData> availableRoles)
    {
        if (participants == null || participants.Count == 0) return;

        // 1. 역할 풀(Pool) 구성
        List<RoleData> rolePool = new List<RoleData>();
        RoleData defaultCitizen = null;

        foreach (var role in availableRoles)
        {
            if (role == null) continue;

            if (role.RoleId == "Citizen")
            {
                defaultCitizen = role;
                continue; // 시민은 여기서 폴에 넣지 않고 일단 패스
            }

            for (int i = 0; i < role.MaxCountInGame; i++)
            {
                rolePool.Add(role);
            }
        }

        // 인원이 부족할 경우 기본 '시민' 역할로 채움
        while (rolePool.Count < participants.Count)
        {
            if (defaultCitizen != null)
            {
                rolePool.Add(defaultCitizen);
            }
            else
            {
                Debug.LogWarning("기본 시민(Citizen) 역할 데이터를 찾을 수 없어 첫 번째 역할로 채웁니다.");
                rolePool.Add(availableRoles[0]);
            }
        }

        // 2. Fisher-Yates 셔플 알고리즘으로 무작위 섞기
        for (int i = 0; i < rolePool.Count; i++)
        {
            int randomIndex = Random.Range(i, rolePool.Count);
            RoleData temp = rolePool[i];
            rolePool[i] = rolePool[randomIndex];
            rolePool[randomIndex] = temp;
        }

        // 3. 참가자들에게 안전하게 역할 부여
        for (int i = 0; i < participants.Count; i++)
        {
            participants[i].SetRole(rolePool[i]);
            Debug.Log($"[{participants[i].Name}] 역할 할당 -> {participants[i].Role.RoleName}");
        }
    }
}