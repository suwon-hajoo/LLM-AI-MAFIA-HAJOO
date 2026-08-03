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

public class NightSceneManagerBK : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI infoText;          // 역할 안내 텍스트
    [SerializeField] private TextMeshProUGUI timerText;         // 남은 시간 텍스트
    [SerializeField] private Transform targetListPanel;         // Grid Layout Group 부모 패널
    [SerializeField] private GameObject nameButtonPrefab;       // Name_Panel 프리팹
    [SerializeField] private MemoManager memoManager;

    [Header("결과 UI 컨트롤러 연결")]
    [SerializeField] private NightResultUIController nightResultUI; // 새로 만든 UI 컨트롤러

    [Header("시간 및 연출 설정")]
    [SerializeField] private float nightDuration = 30f;       // 고민 시간 (30초)

    [Header("씬 전환 설정")]
    [SerializeField] private string nextSceneName = "Mafia_Meeting_Scene"; // 이동할 다음 씬 이름

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
    private Task aiTask = null;

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

        Debug.Log($"<color=purple>==================================================</color>");
        Debug.Log($"<color=purple>★ [밤 씬 시작] 플레이어: {myData.Name} | 직업: {currentSkill.RoleName} ({myData.Role.RoleId})</color>");
        Debug.Log($"<color=purple>==================================================</color>");

        // 2. UI 문구 초기화
        if (infoText != null)
        {
            infoText.text = $"당신의 역할: <color=yellow>[{currentSkill.RoleName}]</color>\n능력을 사용할 대상을 선택하세요.";
        }

        // 3. 버튼 동적 생성
        GenerateTargetButtons();
        AINightSkillProcessor aiProcessor = new AINightSkillProcessor();
        aiTask = aiProcessor.ProcessAllAINightSkillsAsync();

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

            TextMeshProUGUI textComp = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = p.Name;
            }

            Image panelImage = newBtnObj.GetComponent<Image>();
            if (!p.IsAlive && panelImage != null)
            {
                panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            }

            // =========================================================
            // 💡 [추가] 4-1) 메모장 버튼 찾기 및 연결 (ID 1 ~ 7번 매칭)
            // =========================================================
            // 프리팹 자식들 중에서 "Button_Memo" 라는 이름을 가진 버튼을 찾습니다.
            Button[] allButtons = newBtnObj.GetComponentsInChildren<Button>(true);
            Button memoBtn = System.Array.Find(allButtons, b => b.name == "Button_Memo");

            if (memoBtn != null)
            {
                int memoId = p.Id; // 참가자의 ID (1 ~ 7)

                // 💡 [핵심] 메모장 버튼도 활성화 조건 체크! 
                // (살아있고 + 자기 자신/0번 유저가 아닌 경우만 true)
                bool isMemoable = p.IsAlive && (myData == null || p.Id != myData.Id);

                // 조건에 안 맞으면 버튼 클릭 불가능(interactable = false) 처리
                memoBtn.interactable = isMemoable;

                // 클릭 가능한 대상일 때만 이벤트 등록
                if (isMemoable)
                {
                    memoBtn.onClick.AddListener(() =>
                    {
                        if (memoManager != null)
                        {
                            memoManager.SelectMemoById(memoId);
                            memoManager.OpenMemoPanel();
                        }
                    });
                }
            }

            // =========================================================
            // 4-2) 기존 투표 버튼(Button_Vote) 찾기 및 연결
            // =========================================================
            Button voteBtn = System.Array.Find(allButtons, b => b.name == "Button_Vote");
            if (voteBtn == null) voteBtn = newBtnObj.GetComponentInChildren<Button>(); // 못찾으면 기본 검색

            Image btnImage = voteBtn != null ? voteBtn.GetComponent<Image>() : null;
            bool isSelectable = p.IsAlive && (myData == null || p.Id != myData.Id);

            if (voteBtn != null)
            {
                voteBtn.interactable = isSelectable;
            }

            if (btnImage != null && normalSprite != null)
            {
                btnImage.sprite = normalSprite;
            }

            int targetId = p.Id;

            if (voteBtn != null && isSelectable)
            {
                voteBtn.onClick.AddListener(() => OnTargetButtonClicked(btnImage!, targetId));
            }

            /*Button btn = newBtnObj.GetComponentInChildren<Button>();
            Image btnImage = btn != null ? btn.GetComponent<Image>() : null;

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
            }*/
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

        if (isNightActive)
        {
            Debug.LogWarning($"<color=yellow>[시간 초과] {nightDuration}초 동안 대상을 고르지 않아 자동으로 살아있는 무작위 대상을 지정합니다.</color>");
            AutoSelectRandomTarget();
        }
    }

    public void SelectTarget(int targetId)
    {
        if (!isNightActive || currentSkill == null) return;

        Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);
        string targetName = target != null ? target.Name : $"ID:{targetId}";

        Debug.Log($"<color=cyan>[능력 사용 입력] 플레이어({myData.Name})가 {targetName} (ID:{targetId}) 님을 선택했습니다.</color>");

        // 능력 실행
        currentSkill.ExecuteSkill(myData.Id, targetId);

        // 선택 후 밤 정산 및 UI 연출 진입
        StartCoroutine(EndNightPhaseRoutine(target));
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
            StartCoroutine(EndNightPhaseRoutine(null));
        }
    }

    private IEnumerator EndNightPhaseRoutine(Participant target)
    {
        isNightActive = false;

        Debug.Log("<color=purple>[밤 씬 선택 완료] 유저 UI 연출과 AI 처리 통신을 동시에 시작합니다.</color>");

        // 1. 💡 [동시 실행 1] 유저 결과 UI 팝업 연출 코루틴 시작 (yield return을 빠뜨려서 즉시 다음 줄 실행!)
        Coroutine uiCoroutine = null;
        if (nightResultUI != null && target != null)
        {
            uiCoroutine = StartCoroutine(nightResultUI.ShowResultRoutine(myData, target));
        }


        // 3. AI 통신이 끝날 때까지 대기
        while (!aiTask.IsCompleted)
        {
            yield return null;
        }

        //  AI 처리 작업의 완료 상태 및 오류 여부 검증 로그
        if (aiTask.IsFaulted)
        {
            Debug.LogError($"<color=red>[오류 발생] AI 처리 작업 중 오류가 발생했습니다: {aiTask.Exception?.InnerException?.Message}</color>");
        }
        else
        {
            Debug.Log("<color=green>[AI 처리 완료] 모든 AI의 밤 능력이 정산되었습니다.</color>");
        }

        // 4. 💡 유저 UI 연출(5초 패널)이 혹시 AI 통신보다 일찍 안 끝났다면 5초 연출이 다 끝날 때까지 남은 대기
        if (uiCoroutine != null)
        {
            yield return uiCoroutine;
        }

        // 5. 사망 정산 및 승리 조건 체크 (승리 시 게임 종료 처리)
        if (ResolveNightResults())
        {
            yield break;
        }

        // 6. 다음 씬으로 전환
        Debug.Log($"<color=green>[씬 이동] {nextSceneName} 씬으로 로딩합니다.</color>");
        SceneManager.LoadScene(nextSceneName);
    }

    private bool ResolveNightResults()
    {
        // 1. NightTurnContext에 기록된 마피아 습격 대상과 의사 보호 대상 ID 가져오기
        int attackId = NightTurnContext.MafiaTargetId;
        int protectId = NightTurnContext.DoctorTargetId;

        Debug.Log($"<color=purple>[밤 정산 시작] 마피아 표적: ID {attackId} | 의사 보호: ID {protectId}</color>");

        // 2. 마피아의 공격 대상이 존재하는 경우
        if (attackId != -1)
        {
            Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == attackId);

            if (target != null && target.IsAlive)
            {
                // 🌟 [핵심 판정]: 마피아 타깃과 의사 타깃이 같은 경우 세이브!
                if (attackId == protectId)
                {
                    Debug.Log($"<color=cyan>★ [정산 결과] 의사가 '{target.Name}' 님을 치료하여 마피아의 습격으로부터 살아남았습니다!</color>");
                }
                else
                {
                    // 보호 실패 ➔ 사망 처리
                    target.Die();
                    Debug.Log($"<color=red>★ [정산 결과] {target.Name} (ID:{target.Id}) 님이 마피아에게 습격당해 사망했습니다.</color>");
                }
            }
        }
        else
        {
            Debug.Log("<color=green>[정산 결과] 밤사이 아무도 공격받지 않았거나 마피아가 타깃을 지정하지 않았습니다.</color>");
        }

        // 3. 🌟 정산 완료 후 다음 밤을 위해 context 데이터 초기화
        NightTurnContext.Reset();

        Debug.Log($"<color=purple>==================================================</color>");

        // 4. 승리 조건 체크
        if (GameDataManager.Instance != null)
        {
            GameResult result = GameDataManager.Instance.CheckGameResult();

            if (result != GameResult.None)
            {
                Debug.Log($"<color=yellow>★ [게임 종료] 승리 조건 충족! 결과: {result}</color>");

                if (resultUI != null)
                {
                    resultUI.ShowResultPanel(result);
                }
                return true;
            }
        }

        return false;
    }

    /*private bool ResolveNightResults()
    {
        if (currentSkill is MafiaSkill mafiaSkill && mafiaSkill.SelectedTargetId != -1)
        {
            int targetId = mafiaSkill.SelectedTargetId;
            Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);

            if (target != null && target.IsAlive)
            {
                target.Die();
                Debug.Log($"<color=red>★ [정산 결과] {target.Name} (ID:{target.Id}) 님이 마피아에게 습격당해 사망 처리되었습니다.</color>");
            }
        }
        else
        {
            Debug.Log("<color=green>[정산 결과] 밤사이 아무도 사망하지 않았거나 공격 능력이 사용되지 않았습니다.</color>");
        }

        Debug.Log($"<color=purple>==================================================</color>");

        if (GameDataManager.Instance != null)
        {
            GameResult result = GameDataManager.Instance.CheckGameResult();

            if (result != GameResult.None)
            {
                Debug.Log($"<color=yellow>★ [게임 종료] 승리 조건 충족! 결과: {result}</color>");

                if (resultUI != null)
                {
                    resultUI.ShowResultPanel(result);
                }
                return true;
            }
        }

        return false;
    }*/
}