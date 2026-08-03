#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameDataManager;

public class GameResultUIControllerBK : MonoBehaviour
{
    [Header("결과 패널 오브젝트")]
    [SerializeField] private GameObject? resultPanel;           // 결과 전체 화면 패널
    [SerializeField] private TextMeshProUGUI? outcomeTitleText;  // "승리" 또는 "패배" 텍스트
    [SerializeField] private TextMeshProUGUI? teamResultText;    // "시민 진영 승리" 등 세부 설명
    [SerializeField] private TextMeshProUGUI? roleListText;      // 전체 AI 및 유저 역할 공개 텍스트
    [SerializeField] private Button? restartButton;             // 시작 화면으로 돌아가는 버튼

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false); // 시작 시 패널 숨김
        }
    }

    // 승리 판정 시 호출되는 함수
    public void ShowResultPanel(GameResult result)
    {
        if (result == GameResult.None || resultPanel == null) return;

        resultPanel.SetActive(true);

        Participant? myData = GameDataManager.Instance.GetMyParticipantData();
        Team myTeam = myData != null ? myData.Role.Team : Team.Citizen;

        // 1. 결과 타이틀 & 설명 텍스트 분기 처리
        if (result == GameResult.MyDeath)
        {
            // 💡 [유저 본인 사망 시]
            if (outcomeTitleText != null)
                outcomeTitleText.text = "<color=red>사망</color>";

            if (teamResultText != null)
                teamResultText.text = "당신은 밤/낮 공작으로 인해 사망하셨습니다.";
        }
        else
        {
            // 💡 [진영 승리 / 패배 시]
            bool isMyWin = (result == GameResult.CitizenWin && myTeam != Team.Mafia) ||
                           (result == GameResult.MafiaWin && myTeam == Team.Mafia);

            if (outcomeTitleText != null)
            {
                outcomeTitleText.text = isMyWin ? "<color=green>승리</color>" : "<color=red>패배</color>";
            }

            if (teamResultText != null)
            {
                teamResultText.text = result == GameResult.CitizenWin ? "시민 진영이 승리하였습니다!" : "마피아 진영이 승리하였습니다!";
            }
        }

        // 2. 전체 AI 및 유저의 역할 공개 텍스트 생성 (기존 코드 100% 동일)
        if (roleListText != null && GameDataManager.Instance != null)
        {
            string roleSummary = "<b>[ 참가자 역할 공개 ]</b>\n\n";
            IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;

            foreach (var p in participants)
            {
                string status = p.IsAlive ? "(생존)" : "<color=red>(사망)</color>";
                string userTag = p.IsAI ? "" : " ★나";
                roleSummary += $"{p.Name}{userTag} : {p.Role.RoleName} {status}\n";
            }

            roleListText.text = roleSummary;
        }

        // 스케줄러 자동 루프 정지
        if (AITalkScheduler.Instance != null)
        {
            AITalkScheduler.Instance.StopAutoScheduleLoop();
        }
    }

    // 게임 시작 화면(메인 씬)으로 돌아가기
    private void OnRestartButtonClicked()
    {
        // 필요 시 GameDataManager 인스턴스 파괴 후 메인 씬 재로드
        if (GameDataManager.Instance != null)
        {
            Destroy(GameDataManager.Instance.gameObject);
        }
        SceneManager.LoadScene("Start_Scenes"); // 실제 메인 씬 이름으로 변경하세요
    }
}