#nullable enable
using System.Collections;
using TMPro;
using UnityEngine;

public class NightResultUIController : MonoBehaviour
{
    [Header("직업별 결과 UI 패널 연결")]
    [SerializeField] private GameObject? resultPanel;        // 공용 결과 팝업 패널
    [SerializeField] private TextMeshProUGUI? resultText;    // 결과 메시지 텍스트

    [Header("결과 창 표시 대기 시간 (초)")]
    [SerializeField] private float displayDuration = 5f;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 유저의 직업 및 스킬 실행 결과에 맞춰 패널을 띄우는 메서드
    /// </summary>
    public IEnumerator ShowResultRoutine(Participant actor, Participant target)
    {
        if (resultPanel == null || resultText == null)
        {
            yield break;
        }

        string roleId = actor.Role.RoleId;
        string message = BuildMessageByRole(roleId, target);

        // 메시지가 존재하는 직업군(경찰, 의사, 스파이, 마피아)만 패널 출력
        if (!string.IsNullOrEmpty(message))
        {
            resultText.text = message;
            resultPanel.SetActive(true);

            yield return new WaitForSeconds(displayDuration);

            resultPanel.SetActive(false);
        }
    }

    private string BuildMessageByRole(string roleId, Participant target)
    {
        string targetName = target != null ? target.Name : "지정된 대상";

        switch (roleId)
        {
            case "Police":
                bool isMafia = target?.Role.Team == Team.Mafia;
                if (target?.Role.RoleId == "Spy") isMafia = false; // 스파이는 경찰 조사 시 시민으로 위장
                return $"[조사 결과]\n<color=yellow>{targetName}</color> 님은 <color={(isMafia ? "red" : "cyan")}>{(isMafia ? "마피아가 맞습니다!" : "마피아가 아닙니다.")}</color>";

            case "Doctor":
                return $"[치유 대상 지정]\n오늘 밤 <color=green>{targetName}</color> 님을 마피아의 공격으로부터 보호합니다.";

            case "Mafia":
                return $"[습격 대상 지정]\n오늘 밤 <color=red>{targetName}</color> 님을 습격 대상으로 지정했습니다.";

            case "Spy":
                bool isTargetMafia = target?.Role.RoleId == "Mafia";
                if (isTargetMafia)
                {
                    return $"[접촉 성공]\n<color=yellow>{targetName}</color> 님과 접촉했습니다.\n그는 <color=red>마피아입니다!</color> (상호 정체 확인 완료)";
                }
                else
                {
                    return $"[접촉 실패]\n<color=yellow>{targetName}</color> 님과 접촉했습니다.\n그는 <color=cyan>마피아가 아닙니다.</color>";
                }

            default:
                return ""; // 시민 등 능력 결과 표시가 필요 없는 직업
        }
    }
}