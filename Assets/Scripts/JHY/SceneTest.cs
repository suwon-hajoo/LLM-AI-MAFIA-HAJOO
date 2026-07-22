using UnityEngine;
using TMPro;

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
            // 💡 디버그 로그에 [유저 이름] 출력 추가
            Debug.Log($"[2번째 씬] 데이터 유지 성공! 내 이름: {myData.Name} | 내 역할: {myData.Role.RoleName}");

            // UI 텍스트가 연결되어 있다면 화면에 [이름]과 [직업]을 함께 출력
            if (myRoleText != null)
            {
                myRoleText.text = $"이름: {myData.Name}\n직업: {myData.Role.RoleName}";
            }
        }

        // 3. AI 7명의 이름 및 역할 데이터도 잘 넘어왔는지 콘솔 출력
        Debug.Log("=== [2번째 씬] 전체 참가자 데이터 목록 ===");
        foreach (var p in GameDataManager.Instance.Participants)
        {
            if (p.IsAI)
            {
                // 💡 AI의 이름(p.Name), 역할, 1~100 성격 스탯 유지 확인
                Debug.Log($"<color=cyan>[{p.Name}]</color> 역할: <color=lime>{p.Role.RoleName}</color> | 성격: {p.Personality.GetDebugLogString()}");
            }
            else
            {
                // 💡 유저의 데이터도 목록에서 같이 확인
                Debug.Log($"<color=orange>[★유저: {p.Name}]</color> 역할: <color=lime>{p.Role.RoleName}</color>");
            }
        }
    }
}