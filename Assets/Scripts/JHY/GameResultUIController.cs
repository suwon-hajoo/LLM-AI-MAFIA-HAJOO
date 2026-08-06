#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameDataManager;

public class GameResultUIController : MonoBehaviour
{
    [Header("캔버스 연결")]
    [SerializeField] private GameObject? gameCanvas;   // 메인 게임 캔버스
    [SerializeField] private GameObject? resultCanvas; // 결과 화면 캔버스

    [Header("결과 패널 오브젝트")]
    [SerializeField] private TextMeshProUGUI? outcomeTitleText;  // "MAFIA VICTORY" 등 메인 타이틀 텍스트
    [SerializeField] private TextMeshProUGUI? teamResultText;    // 서브 설명 텍스트
    [SerializeField] private TextMeshProUGUI? roleListText;      // 전체 AI 및 유저 역할 공개 텍스트
    [SerializeField] private Button? restartButton;             // 시작 화면으로 돌아가는 버튼

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        // 시작 시 게임 캔버스는 켜고, 결과 캔버스는 꺼둠
        if (gameCanvas != null) gameCanvas.SetActive(true);
        if (resultCanvas != null) resultCanvas.SetActive(false);
    }

    // 승리 판정 시 호출되는 함수
    public void ShowResultPanel(GameResult result)
    {
        if (result == GameResult.None) return;

        // 💡 캔버스 전환 (게임 캔버스 Off -> 결과 캔버스 On)
        if (gameCanvas != null) gameCanvas.SetActive(false);
        if (resultCanvas != null) resultCanvas.SetActive(true);

        Participant? myData = GameDataManager.Instance.GetMyParticipantData();
        Team myTeam = myData != null ? myData.Role.Team : Team.Citizen;

        // 1. 🌟 [수정] 결과 타이틀 & 서브 설명 텍스트 분기 처리
        if (outcomeTitleText != null)
        {
            if (result == GameResult.MyDeath)
            {
                // 텍스트 엑스트라 세팅의 Rich Text 활성화로 아래 방식 컬러 적용
                // 💡 [1] 유저 본인 사망 시 ➔ "YOU DEAD" (빨간색)
                outcomeTitleText.text = "<color=#FF0011>YOU\nDEAD</color>";

                if (teamResultText != null)
                    teamResultText.text = "You have been eliminated during operation.";
            }
            else if (result == GameResult.CitizenWin)
            {
                // 💡 [2] 시민 진영 승리 시 ➔ "CITIZEN VICTORY" (파란색: #538DFC)
                outcomeTitleText.text = "<color=#538DFC>CITIZEN\nVICTORY</color>";

                if (teamResultText != null)
                    teamResultText.text = "Order has been restored to the city.";
            }
            else if (result == GameResult.MafiaWin)
            {
                // 💡 [3] 마피아 진영 승리 시 ➔ "MAFIA VICTORY" (기존 빨간색 계열 그대로)
                outcomeTitleText.text = "<color=#FC536D>MAFIA\nVICTORY</color>";

                if (teamResultText != null)
                    teamResultText.text = "The Syndicate has eliminated all threats.";
            }
        }

        // 2. 전체 AI 및 유저의 역할 공개 텍스트 생성
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
        if (GameDataManager.Instance != null)
        {
            Destroy(GameDataManager.Instance.gameObject);
        }
        SceneManager.LoadScene("Mafia_Main_Scene");
    }
}