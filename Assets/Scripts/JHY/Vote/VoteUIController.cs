#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VoteUIController : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject? votePanel;
    [SerializeField] private ScrollRect? voteScrollRect;
    [SerializeField] private Transform? voteListPanel;
    [SerializeField] private GameObject? voteButtonPrefab;
    [SerializeField] private MemoManager? memoManager;

    [Header("버튼 연결")]
    [SerializeField] private Button? skipVoteButton;
    [SerializeField] private Button? confirmVoteButton;
    [SerializeField] private Button? skipMeetingButton;
    [SerializeField] private Button? nightConfirmButton;

    [Header("게임 결과 컨트롤러")]
    [SerializeField] private GameResultUIController? resultUI;

    [Header("밤 컨트롤러 연결")]
    [SerializeField] private NightSceneManager? nightSceneManager;

    private Image? currentSelectedImage = null;
    private int selectedTargetId = -1; // -1: 스킵(기권)

    //private LLMPrompt llmPrompt = new LLMPrompt();
    // 💡 [수정] txt 파일 읽기용 Repository 생성 및 LLMPrompt에 주입
    private PromptRepository? promptRepository;
    private LLMPrompt? llmPrompt;

    private Dictionary<string, int> voteCounts = new();
    private List<Task> tasks = new();

    // 💡 [추가] 생성된 프리팹 카드 오브젝트들을 보관할 리스트 (오브젝트 재사용)
    private List<GameObject> createdCards = new();

    private void Awake()
    {
        promptRepository = new PromptRepository();
        llmPrompt = new LLMPrompt(promptRepository);
    }

    private void Start()
    {
        if (confirmVoteButton != null)
        {
            confirmVoteButton.onClick.AddListener(SubmitVote);
            confirmVoteButton.gameObject.SetActive(false);
        }

        if (skipVoteButton != null)
        {
            skipVoteButton.onClick.AddListener(OnSkipButtonClicked);
        }

        skipMeetingButton?.gameObject.SetActive(true);

        // 💡 [최적화] 게임 시작 시 참가자 수만큼 카드를 딱 한 번 미리 생성해 둡니다.
        InitVoteButtons();
    }

    // 💡 [1] 게임 시작 시 최초 1회만 카드를 싹 생성하는 함수
    private void InitVoteButtons()
    {
        if (voteListPanel == null || voteButtonPrefab == null || GameDataManager.Instance == null) return;

        // 기존에 혹시 남아있을 수 있는 자식 제거
        foreach (Transform child in voteListPanel)
        {
            Destroy(child.gameObject);
        }
        createdCards.Clear();

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;

        foreach (var p in participants)
        {
            GameObject newBtnObj = Instantiate(voteButtonPrefab, voteListPanel);
            createdCards.Add(newBtnObj);
        }
    }

    public void OpenVotePanel()
    {
        AITalkScheduler.Instance!.StopAutoScheduleLoop();
        tasks.Clear();
        voteCounts.Clear();

        if (GameDataManager.Instance.OneDay)
        {
            GameDataManager.Instance.OneDay = false;
            //SceneManager.LoadScene("Night_Scenes");

            // 💡 [밤 상태로 UI 일괄 전환]
            SetUIPhaseState(GamePhase.Night);

            if (nightSceneManager != null)
            {
                nightSceneManager.StartNightPhase();
            }
            return;
        }

        if (votePanel != null) votePanel.SetActive(true);

        if (confirmVoteButton != null)
        {
            confirmVoteButton.gameObject.SetActive(true);
        }

        skipMeetingButton?.gameObject.SetActive(false);

        // 💡 [2] 파괴/생성 대신, 기존 카드들의 상태만 최신으로 갱신해 줍니다!
        UpdateVoteButtons();
        ProcessAllAIVote();

        Canvas.ForceUpdateCanvases();
        if (voteScrollRect != null)
        {
            voteScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    // 💡 [3] 투표 패널이 열릴 때 카드 데이터 상태만 갱신하는 함수 (파괴 X)
    private void UpdateVoteButtons()
    {
        if (GameDataManager.Instance == null) return;

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;
        Participant myData = GameDataManager.Instance.GetMyParticipantData();

        currentSelectedImage = null; // 선택 상태 초기화
        selectedTargetId = -1;       // 기본 선택 스킵으로 초기화

        for (int i = 0; i < participants.Count && i < createdCards.Count; i++)
        {
            Participant p = participants[i];
            GameObject cardObj = createdCards[i];

            // [1] 이름 텍스트 설정
            TextMeshProUGUI[] allTexts = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI? nameText = System.Array.Find(allTexts, t => t.transform.parent != null && t.transform.parent.name.Contains("Heading"));
            if (nameText == null && allTexts.Length > 0) nameText = allTexts[0];

            if (nameText != null)
            {
                nameText.text = p.Name;
            }

            // [2] 버튼 탐색
            Button[] allButtons = cardObj.GetComponentsInChildren<Button>(true);
            Button? memoBtn = (allButtons.Length > 0) ? allButtons[0] : null;
            Button? voteBtn = (allButtons.Length > 1) ? allButtons[1] : null;

            bool isMe = (myData != null && p.Id == myData.Id);
            int targetId = p.Id;

            // [A] 메모 버튼 처리
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

            // [B] 투표 버튼 처리
            if (voteBtn != null)
            {
                Image? btnImage = voteBtn.GetComponent<Image>();
                TextMeshProUGUI[] voteTexts = voteBtn.GetComponentsInChildren<TextMeshProUGUI>(true);

                // 기본 알파값 255 (1.0f)로 복원
                if (btnImage != null)
                {
                    Color c = btnImage.color;
                    c.a = 1.0f;
                    btnImage.color = c;
                }

                // 💀 사망자 및 유저/AI 상태별 처리
                if (!p.IsAlive)
                {
                    voteBtn.interactable = false; // 클릭 불가
                    foreach (var t in voteTexts) t.text = "DEAD";
                }
                else if (isMe)
                {
                    voteBtn.interactable = true;
                    foreach (var t in voteTexts) t.text = "SKIP";

                    voteBtn.onClick.RemoveAllListeners();
                    voteBtn.onClick.AddListener(() =>
                    {
                        OnTargetButtonClicked(btnImage!, -1);
                        Debug.Log("<color=cyan>[투표 선택]</color> 스킵(Skip)을 선택했습니다.");
                    });
                }
                else
                {
                    voteBtn.interactable = true;
                    foreach (var t in voteTexts) t.text = "VOTE";

                    voteBtn.onClick.RemoveAllListeners();
                    voteBtn.onClick.AddListener(() =>
                    {
                        OnTargetButtonClicked(btnImage!, targetId);
                        Debug.Log($"<color=cyan>[투표 선택]</color> {p.Name} (ID: {targetId})을(를) 선택했습니다.");
                    });
                }
            }

            // [3] 사망자 카드 전체 배경 어둡게 처리
            Image panelImage = cardObj.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = !p.IsAlive ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : Color.white;
            }
        }
    }

    private void OnTargetButtonClicked(Image clickedImage, int targetId)
    {
        if (currentSelectedImage != null)
        {
            Color prevColor = currentSelectedImage.color;
            prevColor.a = 1.0f; // Alpha 255 원복
            currentSelectedImage.color = prevColor;
        }

        if (clickedImage != null)
        {
            Color nextColor = clickedImage.color;
            nextColor.a = 20f / 255f; // 선택 시 Alpha 20 적용
            clickedImage.color = nextColor;
        }

        currentSelectedImage = clickedImage;
        selectedTargetId = targetId;

        Debug.Log($"[선택 변경] 선택된 Target ID: {selectedTargetId}");
    }

    private void SelectSkipButtonDefault()
    {
        if (skipVoteButton != null)
        {
            Image skipImage = skipVoteButton.GetComponent<Image>();
            OnTargetButtonClicked(skipImage, -1);
        }
    }

    private void OnSkipButtonClicked()
    {
        if (skipVoteButton != null)
        {
            Image skipImage = skipVoteButton.GetComponent<Image>();
            OnTargetButtonClicked(skipImage, -1);
        }
    }

    private void ProcessAllAIVote()
    {
        try
        {
            List<Participant> aliveList = GameDataManager.Instance.Participants.Where(p => p.IsAlive).ToList();
            foreach (var p in aliveList) voteCounts[p.Name] = 0;

            ChatService chatService = ChatService.GetInstance();

            foreach (var p in aliveList)
            {
                if (p.IsAI)
                {
                    tasks.Add(ProcessAIVote(chatService, aliveList, voteCounts, p));
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[투표 처리 중 오류 발생] {ex.Message}");
        }
    }

    private async Task ProcessAIVote(ChatService chatService, List<Participant> aliveList, Dictionary<string, int> voteCounts, Participant p)
    {
        if (llmPrompt == null) return;

        var conversation = chatService.GetGameConversationById(p.Id);
        if (conversation == null) return;

        string votePrompt = llmPrompt.GetVotePrompt(p, aliveList);
        string? jsonResponse = await OpenAIChatManager.Instance!.SendChatRequest(conversation, votePrompt, LLMResponseFormat.JsonObject);

        if (!string.IsNullOrEmpty(jsonResponse))
        {
            VoteTarget? result = llmPrompt.GetVoteTarget(jsonResponse);
            if (result != null)
            {
                // is_skip 검사를 target 검사보다 먼저 수행
                if (result.is_skip)
                {
                    Debug.Log($"<color=cyan>[AI 투표]</color> {p.Name}가 투표를 건너뛰었습니다.");
                }
                else if (!string.IsNullOrEmpty(result.target))
                {
                    Debug.Log($"<color=cyan>[AI 투표]</color> {p.Name} ➔ {result.target}");
                    if (voteCounts.ContainsKey(result.target))
                    {
                        voteCounts[result.target]++;
                    }
                }
            }
        }
    }

    public async void SubmitVote()
    {
        if (confirmVoteButton != null) confirmVoteButton.interactable = false;

        try
        {
            List<Participant> aliveList = GameDataManager.Instance.Participants.Where(p => p.IsAlive).ToList();

            if (selectedTargetId != -1)
            {
                Participant myTarget = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == selectedTargetId);
                if (myTarget != null && voteCounts.ContainsKey(myTarget.Name))
                {
                    voteCounts[myTarget.Name]++;
                    Debug.Log($"<color=orange>[유저 투표]</color> 나 ➔ {myTarget.Name}");
                }
            }
            else
            {
                Debug.Log("<color=orange>[유저 투표]</color> 나 ➔ 스킵(기권)");
            }

            await Task.WhenAll(tasks);

            string topVotedName = "";
            int maxVotes = 0;
            bool isTie = false;
            int totalPlayerVotes = 0;

            foreach (var kvp in voteCounts)
            {
                Debug.Log($"[투표 현황] {kvp.Key} : {kvp.Value}표");
                totalPlayerVotes += kvp.Value;

                if (kvp.Value > maxVotes)
                {
                    maxVotes = kvp.Value;
                    topVotedName = kvp.Key;
                    isTie = false;
                }
                else if (kvp.Value == maxVotes && maxVotes > 0)
                {
                    isTie = true;
                }
            }

            int skipCount = aliveList.Count - totalPlayerVotes;
            int majorityThreshold = (aliveList.Count / 2) + 1;

            if (skipCount == maxVotes) isTie = true;

            int currentDay = GameDataManager.Instance.CurrentDay; // [SystemMessage] 현재 날짜 가져오기

            if (skipCount >= majorityThreshold || skipCount >= maxVotes)
            {
                Debug.Log("<color=yellow><b>[최종 결과] 과반수 이상이 건너뛰었습니다.</b></color>");

                // [SystemMessage] 투표 스킵 메시지
                SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, "과반수 이상의 스킵 투표로 인해 낮 투표를 건너뛰었습니다.");

            }
            else if (isTie)
            {
                Debug.Log("<color=yellow><b>[최종 결과] 동점이 발생하여 무효 처리되었습니다.</b></color>");

                // [SystemMessage] 투표 무효 메시지
                SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, "동점 투표로 인해 낮 투표를 건너뛰었습니다.");
            }
            else if (maxVotes > 0 && !string.IsNullOrEmpty(topVotedName))
            {
                Participant executedPerson = aliveList.FirstOrDefault(p => p.Name == topVotedName);
                if (executedPerson != null)
                {
                    executedPerson.Die();
                    Debug.Log($"<color=red><b>[최종 결과] {executedPerson.Name} 님이 처형되었습니다!</b></color>");

                    // [SystemMessage] 처형 발생 메시지
                    SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, $"낮 '{executedPerson.Name}' 님이 투표로 처형당하였습니다.");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[투표 처리 중 오류 발생] {ex.Message}");
        }
        finally
        {
            GameDataManager.GameResult result = GameDataManager.Instance.CheckGameResult();
            if (result == GameDataManager.GameResult.None)
            {
                if (votePanel != null) votePanel.SetActive(false);
                //SceneManager.LoadScene("Night_Scenes");
                // 밤 컨트롤러 실행! (씬 로딩 대신 단일 씬에서 밤 패널 전환)

                // 💡 [밤 상태로 UI 일괄 전환]
                SetUIPhaseState(GamePhase.Night);

                if (nightSceneManager != null)
                {
                    nightSceneManager.StartNightPhase();
                }
            }
            else if (resultUI != null)
            {
                resultUI.ShowResultPanel(result);
            }
        }
    }

    public enum GamePhase
    {
        Meeting,   // 낮 회의 (채팅 중심)
        DayVote,   // 낮 투표 (스킵/확인 버튼 활성화, 밤 버튼 비활성화)
        Night      // 밤 능력 사용 (스킵 버튼 비활성화, 밤 전용 버튼 활성화)
    }

    public void SetUIPhaseState(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Meeting:
                // [낮 회의] 스킵/투표 UI 끄고 회의 UI 켜기
                if (skipVoteButton != null) skipVoteButton.gameObject.SetActive(false);
                if (confirmVoteButton != null) confirmVoteButton.gameObject.SetActive(false);
                if (skipMeetingButton != null) skipMeetingButton.gameObject.SetActive(true);

                if (nightConfirmButton != null) nightConfirmButton.gameObject.SetActive(false);
                break;

            case GamePhase.DayVote:
                // [낮 투표] 투표용 스킵/확인 버튼 활성화, 밤 버튼 비활성화
                if (skipVoteButton != null) skipVoteButton.gameObject.SetActive(true);
                if (confirmVoteButton != null) confirmVoteButton.gameObject.SetActive(true);
                if (skipMeetingButton != null) skipMeetingButton.gameObject.SetActive(false);

                if (nightConfirmButton != null) nightConfirmButton.gameObject.SetActive(false);
                break;

            case GamePhase.Night:
                // [밤] 투표 스킵/확인 버튼 비활성화, 밤 전용 버튼 활성화
                if (skipVoteButton != null) skipVoteButton.gameObject.SetActive(false);
                if (confirmVoteButton != null) confirmVoteButton.gameObject.SetActive(false);
                if (skipMeetingButton != null) skipMeetingButton.gameObject.SetActive(false);

                if (nightConfirmButton != null) nightConfirmButton.gameObject.SetActive(true);
                break;
        }
    }
}