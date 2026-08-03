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

    [Header("게임 결과 컨트롤러")]
    [SerializeField] private GameResultUIController? resultUI;

    private Image? currentSelectedImage = null;
    private int selectedTargetId = -1; // -1: 스킵(기권)

    private LLMPrompt llmPrompt = new LLMPrompt();
    private Dictionary<string, int> voteCounts = new();
    private List<Task> tasks = new();

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
    }

    public void OpenVotePanel()
    {
        AITalkScheduler.Instance!.StopAutoScheduleLoop();
        tasks.Clear();
        voteCounts.Clear();

        /*
        if (GameDataManager.Instance.OneDay)
        {
            GameDataManager.Instance.OneDay = false;
            SceneManager.LoadScene("Night_Scenes");
            return;
        }
        */

        if (votePanel != null) votePanel.SetActive(true);


        if (confirmVoteButton != null)
        {
            confirmVoteButton.gameObject.SetActive(true);
        }

        skipMeetingButton?.gameObject.SetActive(false);

        GenerateVoteButtons();
        ProcessAllAIVote();

        Canvas.ForceUpdateCanvases();
        if (voteScrollRect != null)
        {
            voteScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void GenerateVoteButtons()
    {
        if (voteListPanel == null || voteButtonPrefab == null)
        {
            Debug.LogError("[VoteUIController] voteListPanel 또는 voteButtonPrefab이 연결되지 않았습니다!");
            return;
        }

        foreach (Transform child in voteListPanel)
        {
            Destroy(child.gameObject);
        }

        if (GameDataManager.Instance == null) return;

        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;
        Participant myData = GameDataManager.Instance.GetMyParticipantData();

        currentSelectedImage = null; // 선택 상태 초기화

        foreach (var p in participants)
        {
            GameObject newBtnObj = Instantiate(voteButtonPrefab, voteListPanel);

            // [1] 이름 텍스트 설정
            TextMeshProUGUI[] allTexts = newBtnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI? nameText = System.Array.Find(allTexts, t => t.transform.parent != null && t.transform.parent.name.Contains("Heading"));
            if (nameText == null && allTexts.Length > 0) nameText = allTexts[0];

            if (nameText != null)
            {
                nameText.text = p.Name;
            }

            // [2] 버튼 탐색
            Button[] allButtons = newBtnObj.GetComponentsInChildren<Button>(true);
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

                // 💡 [선택 전] 기본 알파값 255 (1.0f)로 선명하게 설정
                if (btnImage != null)
                {
                    Color c = btnImage.color;
                    c.a = 1.0f; // 255
                    btnImage.color = c;
                }

                if (!p.IsAlive)
                {
                    voteBtn.interactable = false;
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

            // [3] 사망자 카드 처리
            Image panelImage = newBtnObj.GetComponent<Image>();
            if (!p.IsAlive && panelImage != null)
            {
                panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
        }
    }

    // 💡 [핵심] 클릭 시 Alpha를 20(20f/255f)으로 변경하는 함수
    private void OnTargetButtonClicked(Image clickedImage, int targetId)
    {
        // 1. 이전에 선택되어 찌그러졌던(Alpha 20) 버튼을 다시 알파 255(1.0f)로 원복
        if (currentSelectedImage != null)
        {
            Color prevColor = currentSelectedImage.color;
            prevColor.a = 1.0f; // 255
            currentSelectedImage.color = prevColor;
        }

        // 2. 새롭게 선택한 버튼의 Alpha를 20(20f/255f ≈ 0.078f)으로 흐리게 변경
        if (clickedImage != null)
        {
            Color nextColor = clickedImage.color;
            nextColor.a = 20f / 255f; // Alpha 20
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
        var conversation = chatService.GetGameConversationById(p.Id);
        if (conversation == null) return;

        string votePrompt = llmPrompt.GetVotePrompt(p, aliveList);
        string? jsonResponse = await OpenAIChatManager.Instance!.SendChatRequest(conversation, votePrompt, LLMResponseFormat.JsonObject);

        if (!string.IsNullOrEmpty(jsonResponse))
        {
            VoteTarget? result = llmPrompt.GetVoteTarget(jsonResponse);
            if (result != null)
            {
                // 💡 [수정] target 체크 전에 is_skip 여부를 먼저 확인합니다!
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

            if (skipCount >= majorityThreshold || skipCount >= maxVotes)
            {
                Debug.Log("<color=yellow><b>[최종 결과] 과반수 이상이 건너뛰었습니다.</b></color>");
            }
            else if (isTie)
            {
                Debug.Log("<color=yellow><b>[최종 결과] 동점이 발생하여 무효 처리되었습니다.</b></color>");
            }
            else if (maxVotes > 0 && !string.IsNullOrEmpty(topVotedName))
            {
                Participant executedPerson = aliveList.FirstOrDefault(p => p.Name == topVotedName);
                if (executedPerson != null)
                {
                    executedPerson.Die();
                    Debug.Log($"<color=red><b>[최종 결과] {executedPerson.Name} 님이 처형되었습니다!</b></color>");
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
                SceneManager.LoadScene("Night_Scenes");
            }
            else if (resultUI != null)
            {
                resultUI.ShowResultPanel(result);
            }
        }
    }
}