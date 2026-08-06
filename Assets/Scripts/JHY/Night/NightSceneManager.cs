#nullable enable
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameDataManager;
using static VoteUIController;

public class NightSceneManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject? nightPanel;             // 밤 전체 UI 패널

    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI? infoText;           // 역할 안내 텍스트
    [SerializeField] private TextMeshProUGUI? timerText;          // 남은 시간 텍스트
    [SerializeField] private ScrollRect? voteScrollRect;         // 스크롤 뷰
    [SerializeField] private Transform? targetListPanel;         // Vote_Panel 프리팹들이 생성될 Content 패널
    [SerializeField] private GameObject? nameButtonPrefab;        // Vote_Panel 프리팹
    [SerializeField] private MemoManager? memoManager;

    [Header("버튼 연결")]
    [SerializeField] private Button? confirmButton;              // 💡 [추가] 밤 능력 최종 확정 버튼

    [Header("결과 UI 컨트롤러 연결")]
    [SerializeField] private NightResultUIController? nightResultUI;
    [SerializeField] private GameResultUIController? resultUI;

    [Header("시간 및 연출 설정")]
    [SerializeField] private float nightDuration = 30f;        // 고민 시간 (30초)
    [SerializeField] private string nextSceneName = "Mafia_Meeting_Scene";

    [Header("투표 컨트롤러 연결")]
    [SerializeField] private VoteUIController? voteUIController;

    [Header("회의 타이머 연결")]
    [SerializeField] private MeetingTimer? meetingTimer;

    private INightSkill? currentSkill;
    private Participant? myData;
    private float currentTimer;
    private bool isNightActive = false;
    private Image? currentSelectedImage = null;
    private int selectedTargetId = -1;                           // 💡 선택한 대상 ID 저장용
    private Task? aiTask = null;

    private void Start()
    {
        // Confirm 버튼 클릭 이벤트 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    // 🌙 밤 페이즈 시작 함수 (VoteUIController에서 낮 투표 후 호출됨)
    public void StartNightPhase()
    {
        if (GameDataManager.Instance == null) return;

        if (nightPanel != null) nightPanel.SetActive(true);

        // 1. 유저 정보 및 직업 스킬 생성
        myData = GameDataManager.Instance.GetMyParticipantData();
        if (myData != null && myData.Role != null)
        {
            currentSkill = NightSkillFactory.CreateSkill(myData.Role.RoleId);

            if (infoText != null)
            {
                infoText.text = $"당신의 역할: <color=yellow>[{currentSkill.RoleName}]</color>\n능력을 사용할 대상을 선택한 뒤 확인을 누르세요.";
            }
        }

        // 2. 카드 목록 생성 및 Confirm 버튼 세팅
        selectedTargetId = -1; // 초기화
        currentSelectedImage = null;

        if (confirmButton != null)
        {
            confirmButton.interactable = true;
            confirmButton.gameObject.SetActive(true);
        }

        GenerateTargetButtons();

        Canvas.ForceUpdateCanvases();
        if (voteScrollRect != null)
        {
            voteScrollRect.verticalNormalizedPosition = 1f;
        }

        // [SystemMessage] 밤 시작 메시지
        int currentDay = GameDataManager.Instance.CurrentDay;
        SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, "밤이 시작되었습니다.");

        // [AI SystemMessage] AI들에게 밤이 시작되었음을 알림
        ChatService chatService = ChatService.GetInstance();
        OpenAIMessage nightStartMsg = new OpenAIMessage
        {
            role = LLMRole.System,
            name = "시스템",
            content = $"[{currentDay}일차 밤이 시작되었습니다. 각자의 직업 능력을 사용할 시간입니다.]"
        };

        foreach (var p in GameDataManager.Instance.Participants.Where(p => p.IsAI && p.IsAlive))
        {
            chatService.AddMessageById(p.Id, nightStartMsg);
        }

        // 3. AI 밤 능력 비동기 수집 시작
        AINightSkillProcessor aiProcessor = new AINightSkillProcessor();
        aiTask = aiProcessor.ProcessAllAINightSkillsAsync();

        // 4. 타이머 시작
        currentTimer = nightDuration;
        isNightActive = true;
        StartCoroutine(NightTimerRoutine());
    }

    private void GenerateTargetButtons()
    {
        if (targetListPanel == null || nameButtonPrefab == null || GameDataManager.Instance == null) return;

        foreach (Transform child in targetListPanel)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;

        foreach (var p in participants)
        {
            GameObject newBtnObj = Instantiate(nameButtonPrefab, targetListPanel);

            // [1] 이름 텍스트 설정
            TextMeshProUGUI[] allTexts = newBtnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI? nameText = System.Array.Find(allTexts, t => t.transform.parent != null && t.transform.parent.name.Contains("Heading"));
            if (nameText == null && allTexts.Length > 0) nameText = allTexts[0];
            if (nameText != null) nameText.text = p.Name;

            // [2] 버튼 탐색
            Button[] allButtons = newBtnObj.GetComponentsInChildren<Button>(true);
            Button? memoBtn = (allButtons.Length > 0) ? allButtons[0] : null;
            Button? voteBtn = (allButtons.Length > 1) ? allButtons[1] : null;

            bool isMe = (myData != null && p.Id == myData.Id);
            int targetId = p.Id;

            // 메모장 버튼 (NOTE)
            if (memoBtn != null)
            {
                bool isMemoable = p.IsAlive && !isMe;
                memoBtn.interactable = isMemoable;
                if (isMemoable)
                {
                    memoBtn.onClick.RemoveAllListeners();
                    memoBtn.onClick.AddListener(() =>
                    {
                        if (memoManager != null)
                        {
                            memoManager.SelectMemoById(targetId);
                            memoManager.OpenMemoPanel();
                        }
                    });
                }
            }

            // 능력 사용 버튼 (VOTE 버튼 재사용)
            if (voteBtn != null)
            {
                Image? btnImage = voteBtn.GetComponent<Image>();
                TextMeshProUGUI[] voteTexts = voteBtn.GetComponentsInChildren<TextMeshProUGUI>(true);

                if (btnImage != null)
                {
                    Color c = btnImage.color;
                    c.a = 1.0f; // 기본 Alpha 255
                    btnImage.color = c;
                }

                if (!p.IsAlive)
                {
                    voteBtn.interactable = false;
                    foreach (var t in voteTexts) t.text = "DEAD";
                }
                else if (isMe)
                {
                    // 자신에게는 능력 사용 불가
                    voteBtn.interactable = false;
                    foreach (var t in voteTexts) t.text = "ME";
                }
                else
                {
                    voteBtn.interactable = true;
                    foreach (var t in voteTexts) t.text = "SELECT";

                    voteBtn.onClick.RemoveAllListeners();
                    voteBtn.onClick.AddListener(() => OnTargetButtonClicked(btnImage!, targetId));
                }
            }

            // 카드 배경 색상
            Image panelImage = newBtnObj.GetComponent<Image>();
            if (panelImage != null)
            {
                if (!p.IsAlive) panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                else if (isMe) panelImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            }
        }
    }

    // 🎨 카드를 누르면 즉시 발동하지 않고, 선택 하이라이트(Alpha 20) 및 targetId 저장만 수행
    private void OnTargetButtonClicked(Image clickedImage, int targetId)
    {
        if (currentSelectedImage != null)
        {
            Color prevColor = currentSelectedImage.color;
            prevColor.a = 1.0f; // 이전 선택 Alpha 255 원복
            currentSelectedImage.color = prevColor;
        }

        if (clickedImage != null)
        {
            Color nextColor = clickedImage.color;
            nextColor.a = 20f / 255f; // 선택 강조 (Alpha 20)
            clickedImage.color = nextColor;
        }

        currentSelectedImage = clickedImage;
        selectedTargetId = targetId; // 선택된 대상 ID만 기억해둠!

        Debug.Log($"<color=cyan>[밤 능력 지목 변경]</color> 선택 대상 ID: {selectedTargetId}");
    }

    // 🔘 [확인 버튼 클릭 시 실행] 최종 제출
    private void OnConfirmButtonClicked()
    {
        if (!isNightActive) return;

        if (confirmButton != null) confirmButton.interactable = false; // 중복 클릭 방지

        if (selectedTargetId != -1)
        {
            SelectTarget(selectedTargetId); // 지목한 대상에게 능력 발동!
        }
        else
        {
            // 대상을 선택하지 않고 확인을 누른 경우 (자동 무작위 지정 혹은 넘어가기)
            Debug.Log("<color=yellow>[알림] 대상을 선택하지 않아 무작위 지목으로 넘어갑니다.</color>");
            AutoSelectRandomTarget();
        }
    }

    private IEnumerator NightTimerRoutine()
    {
        while (currentTimer > 0f && isNightActive)
        {
            currentTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = $"밤 시간: {Mathf.CeilToInt(currentTimer)}초";
            yield return null;
        }

        // 시간 초과 시 자동 제출
        if (isNightActive)
        {
            Debug.LogWarning($"<color=yellow>[시간 초과] 무작위 대상을 지정합니다.</color>");
            AutoSelectRandomTarget();
        }
    }

    public void SelectTarget(int targetId)
    {
        if (currentSkill == null || myData == null) return;

        Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);
        string targetName = target != null ? target.Name : $"ID:{targetId}";

        Debug.Log($"<color=cyan>[밤 능력 제출 완료] 플레이어({myData.Name}) ➔ {targetName} (ID:{targetId})</color>");

        // 능력 실행
        currentSkill.ExecuteSkill(myData.Id, targetId);

        // [SysteMessage] 내 직업별 결과 메시지 (유저 채팅창에만 출력)
        if (target != null)
        {
            switch (myData.Role.RoleId)
            {
                case "Doctor":
                    SystemMessageManager.Instance?.AddSystemMessage($"[의사] 오늘 밤 '{targetName}' 님을 살리기로 결정하였습니다.");
                    break;

                case "Police":
                    bool isMafia = target.Role.Team == Team.Mafia && target.Role.RoleId != "Spy";
                    string resultStr = isMafia ? "마피아가 맞습니다." : "마피아가 아닙니다.";
                    SystemMessageManager.Instance?.AddSystemMessage($"[경찰] 조사 결과 '{targetName}' 님은 {resultStr}");
                    break;

                case "Mafia":
                    SystemMessageManager.Instance?.AddSystemMessage($"[마피아] 오늘 밤 '{targetName}' 님을 습격하기로 하였습니다.");
                    break;

                case "Spy":
                    bool foundMafia = target.Role.RoleId == "Mafia";
                    if (foundMafia)
                    {
                        SystemMessageManager.Instance?.AddSystemMessage($"[스파이] 조사 결과 '{targetName}' 님은 마피아가 맞습니다. (접선 성공)");
                    }
                    else
                    {
                        SystemMessageManager.Instance?.AddSystemMessage($"[스파이] 조사 결과 '{targetName}' 님은 마피아가 아닙니다.");
                    }
                    break;
            }
        }

        // 연출 및 밤 정산 진입
        StartCoroutine(EndNightPhaseRoutine(target));
    }

    private void AutoSelectRandomTarget()
    {
        if (myData == null) return;

        List<Participant> validTargets = GameDataManager.Instance.Participants.Where(p => p.IsAlive && p.Id != myData.Id).ToList();

        if (validTargets.Count > 0)
        {
            int randomTargetId = validTargets[Random.Range(0, validTargets.Count)].Id;
            SelectTarget(randomTargetId);
        }
        else
        {
            StartCoroutine(EndNightPhaseRoutine(null));
        }
    }

    private IEnumerator EndNightPhaseRoutine(Participant? target)
    {
        isNightActive = false;

        Coroutine? uiCoroutine = null;
        if (nightResultUI != null && target != null && myData != null)
        {
            uiCoroutine = StartCoroutine(nightResultUI.ShowResultRoutine(myData, target));
        }

        if (aiTask != null)
        {
            while (!aiTask.IsCompleted)
            {
                yield return null;
            }

            if (aiTask.IsFaulted)
            {
                Debug.LogError($"<color=red>[오류] AI 처리 중 예외 발생: {aiTask.Exception?.InnerException?.Message}</color>");
            }
        }

        if (uiCoroutine != null)
        {
            yield return uiCoroutine;
        }

        if (ResolveNightResults())
        {
            yield break;
        }

        // 밤 패널 끄고 다음 회의 패널로 전환/씬 로딩
        if (nightPanel != null) nightPanel.SetActive(false);
        Debug.Log($"<color=green>[밤 종료] {nextSceneName} 씬(또는 낮 회의 패널)으로 진행합니다.</color>");

        // 💡 [밤 상태로 UI 일괄 전환]
        voteUIController?.SetUIPhaseState(GamePhase.Meeting);

        if (meetingTimer != null)
        {
            meetingTimer.ResetAndStartTimer(); // 3분 타이머 + AI 루프 재시작
        }

        Debug.Log("<color=green>[아침 시작] 새로운 낮 회의 페이즈가 시작되었습니다.</color>");

        // 씬 전환일 경우: SceneManager.LoadScene(nextSceneName);
    }

    private bool ResolveNightResults()
    {
        int attackId = NightTurnContext.MafiaTargetId;
        int protectId = NightTurnContext.DoctorTargetId;
        int currentDay = GameDataManager.Instance.CurrentDay; // [SystemMessage] 날짜

        string nightLogMessage = $"[{currentDay}일차 밤 결과] 아무도 사망하지 않았습니다."; // [AI SystemMessage] 기본 메시지 설정

        if (attackId != -1)
        {
            Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == attackId);

            if (target != null && target.IsAlive)
            {
                if (attackId == protectId) // 의사 세이브 조건
                {
                    Debug.Log($"<color=cyan>★ [정산 결과] 의사가 '{target.Name}' 님을 치료하여 살아남았습니다!</color>");

                    // 🌟 [추가 위치 1] 의사 세이브 메시지
                    SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, $"밤 '{target.Name}' 님이 마피아에게 공격당하였지만, 의사로 인해 목숨을 잃지 않았습니다.");

                    nightLogMessage = $"[{currentDay}일차 밤 결과] 마피아의 습격이 있었으나, 의사의 치료로 아무도 사망하지 않았습니다."; // [AI SystemMessage]
                }
                else // 마피아 살해 성공 조건
                {
                    target.Die();
                    Debug.Log($"<color=red>★ [정산 결과] {target.Name} 님이 습격당해 사망했습니다.</color>");

                    // 🌟 [추가 위치 2] 마피아 살해 사망 메시지
                    SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, $"밤 '{target.Name}' 님이 마피아에게 살해당했습니다.");
                    nightLogMessage = $"[{currentDay}일차 밤 결과] '{target.Name}' 님이 마피아에게 살해당했습니다."; // [AI SystemMessage]
                }
            }
        }

        // [AI SystemMessage] 밤 정산 결과를 살아있는 AI들에게 전송
        ChatService chatService = ChatService.GetInstance();
        OpenAIMessage nightResultSysMsg = new OpenAIMessage { role = LLMRole.System, name = "시스템", content = nightLogMessage };

        foreach (var p in GameDataManager.Instance.Participants.Where(p => p.IsAI && p.IsAlive))
        {
            chatService.AddMessageById(p.Id, nightResultSysMsg);
        }

        NightTurnContext.Reset();

        if (GameDataManager.Instance != null)
        {
            //  [SystemMessage] 밤 정산 메시지 출력이 다 끝난 후, 다음 날 아침으로 넘어가기 위해 +1 증가!
            GameDataManager.Instance.CurrentDay++;

            // [AI SystemMessage] 날짜가 갱신된 후, "다음 날 아침 시작" 알림을 전송
            int newDay = GameDataManager.Instance.CurrentDay;
            OpenAIMessage newMorningMsg = new OpenAIMessage
            {
                role = LLMRole.System,
                name = "시스템",
                content = $"[{newDay}일차 아침이 밝았습니다. 간밤의 결과를 확인하고 다시 낮 회의를 시작합니다.]"
            };

            foreach (var p in GameDataManager.Instance.Participants.Where(p => p.IsAI && p.IsAlive))
            {
                chatService.AddMessageById(p.Id, newMorningMsg);
            }

            GameResult result = GameDataManager.Instance.CheckGameResult();

            if (result != GameResult.None)
            {
                if (resultUI != null)
                {
                    resultUI.ShowResultPanel(result);
                }
                return true;
            }
        }

        return false;
    }
}