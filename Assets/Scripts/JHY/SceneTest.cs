using UnityEngine;
using TMPro; // TextMeshPro를 사용하신다면 추가

public class SceneTest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI myRoleText;

    void Start()
    {
        // 1. 이전 씬에서 넘겨받은 싱글톤 데이터가 살아있는지 검사
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("오류: GameDataManager를 찾을 수 없습니다! 첫 번째 씬에서 시작했는지 확인하세요.");
            return;
        }

        // 2. 내(플레이어) 역할 데이터 불러오기
        Participant myData = GameDataManager.Instance.GetMyParticipantData();

        if (myData != null && myData.Role != null)
        {
            Debug.Log($"[2번째 씬] 데이터 유지 성공! 내 역할: {myData.Role.RoleName}");

            // UI 텍스트가 연결되어 있다면 화면에 출력
            if (myRoleText != null)
            {
                myRoleText.text = $"내 직업: {myData.Role.RoleName}";
            }
        }

        // 3. AI 7명의 역할 데이터도 잘 넘어왔는지 콘솔 출력
        Debug.Log("=== [2번째 씬] AI 봇 7명 역할 목록 ===");
        foreach (var p in GameDataManager.Instance.Participants)
        {
            if (p.IsAI)
            {
                Debug.Log($"{p.Name} : {p.Role.RoleName}");
            }
        }
    }
}