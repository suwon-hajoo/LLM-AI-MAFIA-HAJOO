#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VoteUIController : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject? votePanel;        // 투표 전체 화면 패널
    [SerializeField] private Transform? voteListPanel;     // 투표 버튼이 깔릴 부모 패널
    [SerializeField] private GameObject? voteButtonPrefab;  // 기본 interactable = false로 되어있는 프리팹
    [SerializeField] private MemoManager? memoManager;

    [Header("버튼 및 스프라이트")]
    [SerializeField] private Button? skipVoteButton;      // 건너뛰기 버튼
    [SerializeField] private Button? confirmVoteButton;   // 투표 확정 버튼
    [SerializeField] private Sprite? normalSprite;        // 기본 배경 (없으면 기본 이미지 유지)
    [SerializeField] private Sprite? selectedSprite;      // 선택 시 배경

    [Header("게임 결과 컨트롤러")]
    [SerializeField] private GameResultUIController? resultUI;

    private Image? currentSelectedImage = null;
    private int selectedTargetId = -1;

    private LLMPrompt llmPrompt = new LLMPrompt();
    private Dictionary<string, int> voteCounts = new();
    private List<Task> tasks = new();

    private void Start()
    {
        if (confirmVoteButton != null)
            confirmVoteButton.onClick.AddListener(SubmitVote);

        if (skipVoteButton != null)
            skipVoteButton.onClick.AddListener(OnSkipButtonClicked);
    }

    // 회의 타이머 종료 시 호출
    public void OpenVotePanel()
    {
        AITalkScheduler.Instance!.StopAutoScheduleLoop();
        tasks.Clear();
        voteCounts.Clear();

        // 첫 째 날 투표 스킵
        if (GameDataManager.Instance.OneDay == true)
        {
            GameDataManager.Instance.OneDay = false;
            SceneManager.LoadScene("Night_Scenes");
            return;
        }

        if (votePanel != null) votePanel.SetActive(true);

        GenerateVoteButtons();
        ProcessAllAIVote();
        SelectSkipButtonDefault();
    }
    private void ProcessAllAIVote()
    {
        try
        {
            // [B] AI 투표 진행 (비동기)
            List<Participant> aliveList = GameDataManager.Instance.Participants
                .Where(p => p.IsAlive)
                .ToList();
            foreach (var p in aliveList)
            {
                voteCounts[p.Name] = 0;
            }

            ChatService chatService = ChatService.GetInstance();

            foreach (var p in aliveList)
            {
                if (p.IsAI)
                {
                    tasks.Add(ProcessAIVote(chatService, aliveList, voteCounts, p));
                }
            }
            
        } catch (System.Exception ex)
        {
            Debug.LogError($"[투표 처리 중 오류 발생] {ex.Message}");
        }

    }

    private void GenerateVoteButtons()
    {
        // 🛡️ 안전장치
        if (voteListPanel == null || voteButtonPrefab == null)
        {
            Debug.LogError("[VoteUIController] voteListPanel 또는 voteButtonPrefab이 연결되지 않았습니다!");
            return;
        }

        // 기존 자식 UI 삭제
        foreach (Transform child in voteListPanel)
        {
            Destroy(child.gameObject);
        }

        if (GameDataManager.Instance == null) return;

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;
        Participant myData = GameDataManager.Instance.GetMyParticipantData(); // 내(유저) 정보

        foreach (var p in participants)
        {
            // 1) 프리팹 생성 (사망자 포함 무조건 생성)
            GameObject newBtnObj = Instantiate(voteButtonPrefab, voteListPanel);

            // 2) 텍스트 설정 (텍스트 수정 없이 원본 이름 그대로 표시)
            TextMeshProUGUI textComp = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = p.Name;
            }

            // 3) 💡 [요청사항 반영] 사망한 경우 전체 패널(부모 Image)을 빨간색으로 물들임!
            Image panelImage = newBtnObj.GetComponent<Image>();
            if (!p.IsAlive && panelImage != null)
            {
                // 원하는 빨간색 톤으로 지정 (예: 반투명한 어두운 빨간색)
                panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            }

            // =========================================================
            // 💡 [추가] 4-1) 메모장 버튼 찾기 및 연결 (ID 1 ~ 7번 매칭)
            // =========================================================
            // 프리팹 자식들 중에서 "Button_Memo" 라는 이름을 가진 버튼을 찾습니다.
            Button[] allButtons = newBtnObj.GetComponentsInChildren<Button>(true);
            Button? memoBtn = System.Array.Find(allButtons, b => b.name == "Button_Memo");

            if (memoBtn != null)
            {
                int memoId = p.Id; // 참가자의 ID (1 ~ 7)

                // [핵심] 메모장 버튼도 활성화 조건 체크! 
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
            Button? voteBtn = System.Array.Find(allButtons, b => b.name == "Button_Vote");
            if (voteBtn == null) voteBtn = newBtnObj.GetComponentInChildren<Button>(); // 못찾으면 기본 검색

            Image? btnImage = voteBtn != null ? voteBtn.GetComponent<Image>() : null;
            bool isVoteable = p.IsAlive && (myData == null || p.Id != myData.Id);

            if (voteBtn != null)
            {
                voteBtn.interactable = isVoteable;
            }

            if (btnImage != null && normalSprite != null)
            {
                btnImage.sprite = normalSprite;
            }

            int targetId = p.Id;

            if (voteBtn != null && isVoteable)
            {
                voteBtn.onClick.AddListener(() => OnTargetButtonClicked(btnImage!, targetId));
            }

            /* // 4) 자식 오브젝트(Button_Vote)에서 Button 및 Image 컴포넌트 찾기
             Button btn = newBtnObj.GetComponentInChildren<Button>();
             Image? btnImage = btn != null ? btn.GetComponent<Image>() : null;

             // 5) 💡 버튼 활성화 조건 (살아있고 + 자기 자신이 아닌 경우만 true)
             bool isVoteable = p.IsAlive && (myData == null || p.Id != myData.Id);

             if (btn != null)
             {
                 btn.interactable = isVoteable; // 사망자 및 본인은 클릭 불가(false)
             }

             // 6) 투표 버튼 기본 스프라이트 지정
             if (btnImage != null && normalSprite != null)
             {
                 btnImage.sprite = normalSprite;
             }

             int targetId = p.Id;

             // 7) 투표 가능한 대상만 클릭 이벤트 등록
             if (btn != null && isVoteable)
             {
                 btn.onClick.AddListener(() => OnTargetButtonClicked(btnImage!, targetId));
             }*/
        }
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

    private void OnTargetButtonClicked(Image clickedImage, int targetId)
    {
        // 이전 선택 해제
        if (currentSelectedImage != null && normalSprite != null)
        {
            currentSelectedImage.sprite = normalSprite;
        }

        // 방금 선택한 버튼 강조
        if (clickedImage != null && selectedSprite != null)
        {
            clickedImage.sprite = selectedSprite;
        }

        currentSelectedImage = clickedImage;
        selectedTargetId = targetId;

        Debug.Log($"[선택 변경] 현재 선택된 Target ID: {selectedTargetId}");
    }

    /*public void SubmitVote()
    {
        if (selectedTargetId == -1)
        {
            Debug.Log("[투표 결과] 건너뛰기(기권) 처리되었습니다.");
        }
        else
        {
            foreach (var p in GameDataManager.Instance.Participants)
            {
                if (p.Id == selectedTargetId && p.IsAlive)
                {
                    p.Die();
                    Debug.Log($"<color=red>[투표 결과] {p.Name} 님이 사망 처리되었습니다.</color>");
                    break;
                }
            }
        }

        if (votePanel != null) votePanel.SetActive(false);

        SceneManager.LoadScene("Night_Scenes");
    }*/

    private async Task ProcessAIVote(ChatService chatService, List<Participant> aliveList, Dictionary<string, int> voteCounts, Participant p)
    {
        var conversation = chatService.GetGameConversationById(p.Id);
        if (conversation == null) return;

        string votePrompt = llmPrompt.GetVotePrompt(p, aliveList);

        // 💡 AI가 고민하는 시간 동안 멈추지 않고 비동기 대기
        string? jsonResponse = await OpenAIChatManager.Instance!.SendChatRequest(conversation, votePrompt, LLMResponseFormat.JsonObject);

        if (!string.IsNullOrEmpty(jsonResponse))
        {
            VoteTarget? result = llmPrompt.GetVoteTarget(jsonResponse);
            if (result != null && !string.IsNullOrEmpty(result.target))
            {
                if (result.is_skip)
                {
                    Debug.Log($"<color=cyan>[AI 투표]</color> {p.Name}가 투표를 건너뛰었습니다.");
                }
                else {
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
        // 1. 중복 클릭 방지
        if (confirmVoteButton != null) confirmVoteButton.interactable = false;

        // 💡 [개선 1] 로딩 텍스트/UI가 있다면 켜주기 (예시)
        // loadingText.text = "AI 플레이어들이 투표 중입니다...";
        // loadingPanel.SetActive(true);

        try
        {

            List<Participant> aliveList = GameDataManager.Instance.Participants
                .Where(p => p.IsAlive)
                .ToList();

            // [A] 유저 투표 집계
            if (selectedTargetId != -1)
            {
                Participant myTarget = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == selectedTargetId);
                if (myTarget != null && voteCounts.ContainsKey(myTarget.Name))
                {
                    voteCounts[myTarget.Name]++;
                    Debug.Log($"<color=orange>[유저 투표]</color> 나 ➔ {myTarget.Name}");
                }
            }

            await Task.WhenAll(tasks);

            // [C] 최종 집계 및 처형
            string topVotedName = "";
            int maxVotes = 0;
            bool isTie = false; // 동점 체크
            int totalPlayerVotes = 0; // 지목된 표들의 총합

            foreach (var kvp in voteCounts)
            {
                Debug.Log($"[투표 현황] {kvp.Key} : {kvp.Value}표");

                totalPlayerVotes += kvp.Value; // 누군가 지목받은 표 합산

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

            // 건너뛰기 표수 = 전체 생존자 수 - 누군가를 지목한 표의 총합
            int skipCount = aliveList.Count - totalPlayerVotes;

            // 과반수 기준 계산 (예: 7명 중 4명, 6명 중 4명, 5명 중 3명)
            int majorityThreshold = (aliveList.Count / 2) + 1;

            if (skipCount == maxVotes)
            {
                isTie = true;
            }

            Debug.Log($"[집계 요약] 최다 득표: {topVotedName} ({maxVotes}표) | 동점: {isTie} | 건너뛰기: {skipCount}표 / 과반기준: {majorityThreshold}표");

            // 최종 분기 처리
            if (isTie)
            {
                Debug.Log($"<color=yellow><b>[최종 결과] 동점({maxVotes}표)이 발생하여 무효 처리되었습니다. 아무도 처형되지 않습니다.</b></color>");
            }
            else if (skipCount >= majorityThreshold || skipCount >= maxVotes)
            {
                // 1) 건너뛰기가 과반수 이상인 경우
                Debug.Log($"<color=yellow><b>[최종 결과] 과반수({skipCount}/{aliveList.Count}명)가 건너뛰어 아무도 처형되지 않습니다.</b></color>");
            }
            else if (maxVotes > 0 && !string.IsNullOrEmpty(topVotedName))
            {
                Participant executedPerson = aliveList.FirstOrDefault(p => p.Name == topVotedName);
                if (executedPerson != null)
                {
                    executedPerson.Die();
                    Debug.Log($"<color=red><b>[최종 결과] {executedPerson.Name} 님이 {maxVotes}표로 처형되었습니다!</b></color>");
                }
            }
        }
        catch (System.Exception ex)
        {
            // 💡 [개선 2] 에러가 터져도 게임이 멈추지 않도록 예외 처리
            Debug.LogError($"[투표 처리 중 오류 발생] {ex.Message}");
        }
        finally
        {
            // 승리 조건 체크
            GameDataManager.GameResult result = GameDataManager.Instance.CheckGameResult();
            if (result == GameDataManager.GameResult.None)
            {
                // [D] 성공하든 실패하든 무조건 밤 씬으로 안전하게 전환
                if (votePanel != null) votePanel.SetActive(false);
                SceneManager.LoadScene("Night_Scenes");
            }
            else if (result != GameDataManager.GameResult.None)
            {
                if (resultUI != null)
                {
                    resultUI.ShowResultPanel(result);
                }
            }
        }
    }
}