using System.Collections;
using System.Collections.Generic;
using System.Linq; // IReadOnlyList 탐색용 (FirstOrDefault)
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // 💡 [추가] 씬 이동용 네임스페이스
using UnityEngine.UI;
using static GameDataManager;

public class NightSceneManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI infoText;          // 역할 안내 텍스트
    [SerializeField] private TextMeshProUGUI timerText;         // 남은 시간 텍스트
    [SerializeField] private Transform targetListPanel;         // Grid Layout Group 부모 패널
    [SerializeField] private GameObject nameButtonPrefab;       // Name_Panel 프리팹

    [Header("경찰 결과 UI")]
    [SerializeField] private GameObject policeResultPanel;      // 경찰 결과 팝업 패널
    [SerializeField] private TextMeshProUGUI policeResultText;  // 경찰 결과 텍스트

    [Header("시간 및 연출 설정")]
    [SerializeField] private float nightDuration = 30f;       // 고민 시간 (30초)
    [SerializeField] private float policeNoticeDuration = 5f; // 경찰 조사 결과 대기 시간 (5초)

    // 💡 [추가] 능력 선택 완료 후 씬 전환까지 대기할 시간 및 이동할 씬 이름
    [Header("씬 전환 설정")]
    [SerializeField] private float transitionDelay = 5f;     // 능력 사용 후 5초 뒤 이동 (인스펙터에서 변경 가능)
    [SerializeField] private string nextSceneName = "Meeting_Scenes"; // 이동할 다음 씬 이름

    [Header("버튼 스프라이트")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    [Header("게임 결과 컨트롤러")]
    [SerializeField] private GameResultUIController resultUI;

    private INightSkill currentSkill;
    private Participant myData;
    private float currentTimer;
    private bool isNightActive = false;
    private Image currentSelectedImage = null;

    private void Start()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("<color=red>[오류] GameDataManager를 찾을 수 없습니다!</color>");
            return;
        }

        // 1. 유저 정보 가져오기 및 역할 스킬 생성
        myData = GameDataManager.Instance.GetMyParticipantData();
        currentSkill = NightSkillFactory.CreateSkill(myData.Role.RoleId);

        // 💡 [알림 1] 밤 시작 디버그 로그
        Debug.Log($"<color=purple>==================================================</color>");
        Debug.Log($"<color=purple>★ [밤 씬 시작] 플레이어: {myData.Name} | 직업: {currentSkill.RoleName} ({myData.Role.RoleId})</color>");
        Debug.Log($"<color=purple>==================================================</color>");

        // 2. UI 문구 초기화
        if (infoText != null)
        {
            infoText.text = $"당신의 역할: <color=yellow>[{currentSkill.RoleName}]</color>\n능력을 사용할 대상을 선택하세요.";
        }

        if (policeResultPanel != null)
        {
            policeResultPanel.SetActive(false);
        }

        // 3. 버튼 동적 생성
        GenerateTargetButtons();

        // 4. 밤 타이머 시작
        currentTimer = nightDuration;
        isNightActive = true;
        StartCoroutine(NightTimerRoutine());
    }

    private void GenerateTargetButtons()
    {
        foreach (Transform child in targetListPanel)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;

        foreach (var p in participants)
        {
            GameObject newBtnObj = Instantiate(nameButtonPrefab, targetListPanel);

            // 텍스트 설정
            TextMeshProUGUI textComp = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = p.Name;
            }

            // 사망자는 부모 패널 배경을 빨간색으로 물들임
            Image panelImage = newBtnObj.GetComponent<Image>();
            if (!p.IsAlive && panelImage != null)
            {
                panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            }

            // 자식 버튼(Button_Vote) 컴포넌트 추출
            Button btn = newBtnObj.GetComponentInChildren<Button>();
            Image btnImage = btn != null ? btn.GetComponent<Image>() : null;

            // 살아있고 + 자기 자신이 아닌 경우만 선택 가능
            bool isSelectable = p.IsAlive && (myData == null || p.Id != myData.Id);

            if (btn != null)
            {
                btn.interactable = isSelectable;
            }

            if (btnImage != null && normalSprite != null)
            {
                btnImage.sprite = normalSprite;
            }

            int targetId = p.Id;

            if (btn != null && isSelectable)
            {
                btn.onClick.AddListener(() => OnTargetButtonClicked(btnImage, targetId));
            }
        }
    }

    private void OnTargetButtonClicked(Image clickedImage, int targetId)
    {
        if (currentSelectedImage != null && normalSprite != null)
        {
            currentSelectedImage.sprite = normalSprite;
        }

        if (clickedImage != null && selectedSprite != null)
        {
            clickedImage.sprite = selectedSprite;
        }

        currentSelectedImage = clickedImage;

        // 선택 즉시 스킬 사용
        SelectTarget(targetId);
    }

    private IEnumerator NightTimerRoutine()
    {
        while (currentTimer > 0f && isNightActive)
        {
            currentTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = $"밤 시간: {Mathf.CeilToInt(currentTimer)}초";
            yield return null;
        }

        // 30초 카운트다운 종료 시 자동 무작위 선택
        if (isNightActive)
        {
            // 💡 [알림 2] 시간 초과 알림
            Debug.LogWarning($"<color=yellow>[시간 초과] {nightDuration}초 동안 대상을 고르지 않아 자동으로 살아있는 무작위 대상을 지정합니다.</color>");
            AutoSelectRandomTarget();
        }
    }

    public void SelectTarget(int targetId)
    {
        if (!isNightActive || currentSkill == null) return;

        // 선택한 참가자 정보 탐색
        Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);
        string targetName = target != null ? target.Name : $"ID:{targetId}";

        // 💡 [알림 3] 유저의 대상 선택 알림
        Debug.Log($"<color=cyan>[능력 사용 입력] 플레이어({myData.Name})가 {targetName} (ID:{targetId}) 님을 선택했습니다.</color>");

        // 인터페이스 다형성으로 스킬 실행
        currentSkill.ExecuteSkill(myData.Id, targetId);

        // 경찰인 경우 전용 5초 결과창 대기 후 종료
        if (currentSkill is PoliceSkill)
        {
            StartCoroutine(PoliceWaitRoutine(targetId, targetName));
        }
        else
        {
            // 마피아, 의사, 시민 등은 선택 후 5초 대기 코루틴 호출
            StartCoroutine(EndNightPhaseRoutine());
        }
    }

    private IEnumerator PoliceWaitRoutine(int targetId, string targetName)
    {
        isNightActive = false; // 카운트다운 타이머 중지

        Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);

        if (target != null)
        {

            bool isMafia = target.Role.Team == Team.Mafia;

            // 스파이는 false가 되어 마피아가 아니라고 뜸
            if (target.Role.RoleId == "Spy")
            {
                isMafia = false;
            }

            string msg = $"[조사 결과] {target.Name} 님은 <color={(isMafia ? "red" : "cyan")}>{(isMafia ? "마피아입니다!" : "마피아가 아닙니다.")}</color>";

            // 💡 [알림 4] 경찰 전용 결과 디버그 알림
            Debug.Log($"<color=cyan>★ [경찰 조사 완료] 대상: {targetName} | 결과: {(isMafia ? "마피아 O" : "마피아 X")} ({policeNoticeDuration}초 동안 결과 표시)</color>");

            if (policeResultPanel != null) policeResultPanel.SetActive(true);
            if (policeResultText != null) policeResultText.text = msg;
        }

        // 경찰 전용 대기시간(5초) 후 밤 종료 처리
        yield return new WaitForSeconds(policeNoticeDuration);

        StartCoroutine(EndNightPhaseRoutine());
    }

    private void AutoSelectRandomTarget()
    {
        List<Participant> validTargets = new List<Participant>();
        foreach (var p in GameDataManager.Instance.Participants)
        {
            if (p.IsAlive && p.Id != myData.Id) validTargets.Add(p);
        }

        if (validTargets.Count > 0)
        {
            int randomTargetId = validTargets[Random.Range(0, validTargets.Count)].Id;
            SelectTarget(randomTargetId);
        }
        else
        {
            Debug.Log("<color=yellow>[알림] 선택할 수 있는 살아있는 대상이 없어 능력을 사용하지 않고 넘어갑니다.</color>");
            StartCoroutine(EndNightPhaseRoutine());
        }
    }

    // 💡 [수정] 밤 종료 및 5초 대기 후 씬 이동을 처리하는 코루틴
    private IEnumerator EndNightPhaseRoutine()
    {
        isNightActive = false;

        // 💡 [알림 5] 밤 종료 알림
        Debug.Log("<color=purple>[밤 씬 선택 완료] 밤 사이 발생한 사건을 정산합니다.</color>");

        // 💡 [4단계 연동] AI들의 밤 능력(경찰 조사, 의사 보호, 마피아 습격) 비동기 실행!
        AINightSkillProcessor aiProcessor = new AINightSkillProcessor();
        Task aiTask = aiProcessor.ProcessAllAINightSkillsAsync();

        // AI 통신 처리가 완료될 때까지 코루틴 대기
        while (!aiTask.IsCompleted)
        {
            yield return null;
        }

        // 사망 정산 실행
        // [핵심] 정산 결과 승리 조건이 맞아 게임이 끝났다면 대기 및 씬 이동을 하지 않고 즉시 종료!
        if (ResolveNightResults())
        {
            yield break; // 👈 코루틴을 그 자리에서 즉시 끊고 나감
        }

        // 경찰이 아닌 일반 능력(마피아/의사 등) 사용자도 바로 씬이 안 꺼지고 5초 대기 안내 메시지 출력
        if (!(currentSkill is PoliceSkill))
        {
            Debug.Log($"<color=yellow>[씬 전환 대기] {transitionDelay}초 후 [{nextSceneName}] 씬으로 이동합니다...</color>");
        }

        // 지정된 시간(5초) 대기
        yield return new WaitForSeconds(transitionDelay);

        // 💡 [핵심] 지정한 다음 씬으로 전환!
        Debug.Log($"<color=green>[씬 이동] {nextSceneName} 씬으로 로딩합니다.</color>");
        SceneManager.LoadScene(nextSceneName);
    }

    private bool ResolveNightResults()
    {
        // 마피아가 습격을 진행한 경우
        if (currentSkill is MafiaSkill mafiaSkill && mafiaSkill.SelectedTargetId != -1)
        {
            int targetId = mafiaSkill.SelectedTargetId;
            Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);

            if (target != null && target.IsAlive)
            {
                target.Die(); // 사망 처리

                // 💡 [알림 6] 최종 사망 정산 결과 알림
                Debug.Log($"<color=red>★ [정산 결과] {target.Name} (ID:{target.Id}) 님이 마피아에게 습격당해 사망(IsAlive = false) 처리되었습니다.</color>");
            }
        }
        else
        {
            Debug.Log("<color=green>[정산 결과] 밤사이 아무도 사망하지 않았거나 공격 능력이 사용되지 않았습니다.</color>");
        }

        Debug.Log($"<color=purple>==================================================</color>");

        // 💡 사망 정산이 완료되었으므로 승리 조건 검사
        if (GameDataManager.Instance != null)
        {
            GameResult result = GameDataManager.Instance.CheckGameResult();

            // 승리 진영이 결정된 경우 (CitizenWin 또는 MafiaWin)
            if (result != GameResult.None)
            {
                Debug.Log($"<color=yellow>★ [게임 종료] 승리 조건 충족! 결과: {result}</color>");

                if (resultUI != null)
                {
                    resultUI.ShowResultPanel(result); // 승리/패배 결과 패널 출력
                }
                else
                {
                    Debug.LogError("[NightSceneManager] resultUI(GameResultUIController)가 인스펙터에 연결되어 있지 않습니다!");
                }

                return true; // 👈 게임이 종료되었음을 알림!
            }
        }

        return false; // 👈 게임을 계속 진행함!
    }
}