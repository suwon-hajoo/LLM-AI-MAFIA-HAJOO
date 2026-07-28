using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameDataManager;

public class NightSceneManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI infoText;          // 역할 안내 텍스트
    [SerializeField] private TextMeshProUGUI timerText;         // 남은 시간 텍스트
    [SerializeField] private Transform targetListPanel;         // Grid Layout Group 부모 패널
    [SerializeField] private GameObject nameButtonPrefab;       // Name_Panel 프리팹

    [Header("결과 UI 컨트롤러 연결")]
    [SerializeField] private NightResultUIController nightResultUI; // 새로 만든 UI 컨트롤러

    [Header("시간 및 연출 설정")]
    [SerializeField] private float nightDuration = 30f;       // 고민 시간 (30초)

    [Header("씬 전환 설정")]
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

            Button btn = newBtnObj.GetComponentInChildren<Button>();
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

        // 2. 💡 [동시 실행 2] AI들의 밤 능력 비동기 통신 처리 즉시 시작
        AINightSkillProcessor aiProcessor = new AINightSkillProcessor();
        Task aiTask = aiProcessor.ProcessAllAINightSkillsAsync();

        // 3. AI 통신이 끝날 때까지 대기
        while (!aiTask.IsCompleted)
        {
            yield return null;
        }

        Debug.Log("<color=green>[AI 처리 완료] 모든 AI의 밤 능력이 정산되었습니다.</color>");

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
    }
}