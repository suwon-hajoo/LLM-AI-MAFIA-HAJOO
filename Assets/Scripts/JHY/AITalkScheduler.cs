#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/*
 * 코루틴 시작 시 정지할 것을 고려하여 임의로 float 저장하는 방식 고려 중
 */

public class AITalkScheduler : MonoBehaviour
{
    public static AITalkScheduler? Instance { get; private set; }

    // 2초마다 추첨된 Participant 객체 자체를 보관! (없으면 null)
    public Participant? CurrentSpeaker { get; private set; } = null;

    [Header("글로벌 LLM 대화 쿨타임 (초)")]
    [SerializeField] private float globalLlmCooldown = 2.0f; // 인스펙터 수정 가능

    [Header("AI 개인 쿨타임 기본 설정")]
    [SerializeField] private float basePersonalCooldown = 10.0f; // 사교성 1점일 때 쿨타임 (초)
    [SerializeField] private float minPersonalCooldown = 3.0f;   // 사교성 100점일 때 최소 쿨타임 (초)
    [SerializeField] private int maxTalkCountBeforeRest = 3;    // 발언 횟수 제약 (예: 3번 말하면 쿨타임)

    [Header("스탯 Key 또는 Name 설정")]
    [SerializeField] private string sociabilityStatKey = "Sociable"; // 사교성 SO Key
    [SerializeField] private string passionStatKey = "Passionate";         // 열정 SO Key

    // 인스펙터 수정 가능 자동 타이머 설정
    [Header("자체 자동 루프 설정")]
    [SerializeField] private float autoCheckInterval = 2.0f;
    //[SerializeField] private bool autoStartLoop = true;

    [Header("한번 지목 당했을 때의 가중치")]
    [SerializeField] private float mentionedWeight = 1.0f;

    // AI 개별 데이터 캐싱 클래스
    private class AICooldownData
    {
        public Participant? Participant;
        public float CooldownEndTime = 0f;    // 쿨타임이 해제되는 절대 시각 (Time.time 기준)
        public int RecentTalkCount = 0;       // 최근 발언 횟수
        public float TalkWeight = 1.0f;       // 열정 스탯 기반 추첨 가중치
        public float PersonalCooldown = 5.0f; // 사교성 스탯 기반 쿨타임 시간
        public int SociabilityValue = 50;     // 디버그 출력용 사교성 원본 수치
        public int PassionValue = 50;         // 디버그 출력용 열정 원본 수치
    }

    private readonly Dictionary<int, AICooldownData> aiDataMap = new Dictionary<int, AICooldownData>();
    private bool isGlobalCooldownActive = false;

    // GC 방지용 리스트 재사용
    private readonly List<AICooldownData> candidateList = new List<AICooldownData>();

    private Coroutine? autoLoopCoroutine;
    private readonly Dictionary<Participant, int> mentionedParticipants = new();

    private void Awake()
    {
        // 💡 파괴되지 않는 싱글톤 세팅
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 전환되어도 유지!
        StopAutoScheduleLoop();
    }

    /*private void Start()
    {
        InitializeAIData();
    }*/

    // 🌟 [추가] 2초마다 스케줄러가 스스로 작동하는 루프 함수들
    public void StartAutoScheduleLoop()
    {
        if (autoLoopCoroutine != null) StopCoroutine(autoLoopCoroutine);
        autoLoopCoroutine = StartCoroutine(AutoScheduleRoutine());
    }

    public void StopAutoScheduleLoop()
    {
        if (autoLoopCoroutine != null)
        {
            StopCoroutine(autoLoopCoroutine);
            autoLoopCoroutine = null;
        }
    }

    private IEnumerator AutoScheduleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoCheckInterval);

            CurrentSpeaker = SelectNextSpeaker();

            if (CurrentSpeaker != null)
            {
                // 쓸데 없는 반복문 사용으로 메모리 지출 (DDD)
                Debug.Log($"<color=lime>[스케줄러 자동 루프] 뽑힌 AI ID: {CurrentSpeaker.Id} | 이름: {CurrentSpeaker.Name} | 직업: {CurrentSpeaker.Role}</color>\n<color=yellow>[성격 정보] {CurrentSpeaker.Personality.GetDebugLogString()}</color>");

                // 2. 🔥 AI 대화 실행 (비동기 Task 실행)
                _ = ProcessAIDayChatAsync(CurrentSpeaker);
            }
        }
    }

    // 💡 1. 초기 데이터 세팅 및 스탯 미리 계산 (캐싱 + 디버그 로그)
    public void InitializeAIData()
    {
        aiDataMap.Clear();

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("<color=red>[오류] GameDataManager 인스턴스를 찾을 수 없습니다!</color>");
            return;
        }

        Debug.Log("<color=purple>==================================================</color>");
        Debug.Log("<color=purple>★ [AITalkScheduler] AI 스탯 캐싱 및 스케줄러 초기화 시작</color>");

        foreach (var p in GameDataManager.Instance.Participants)
        {
            if (p.IsAI)
            {
                AICooldownData data = new AICooldownData
                {
                    Participant = p,
                    CooldownEndTime = 0f,
                    RecentTalkCount = 0
                };

                // 스탯 수치 읽기 및 캐싱
                data.SociabilityValue = p.Personality != null ? p.Personality.GetStatValue(sociabilityStatKey) : 50;
                data.PassionValue = p.Personality != null ? p.Personality.GetStatValue(passionStatKey) : 50;

                data.TalkWeight = CalculateTalkWeight(p);
                data.PersonalCooldown = CalculatePersonalCooldown(p);

                aiDataMap[p.Id] = data;

                mentionedParticipants.Add(p, 0);

                Debug.Log($"<color=white> - [AI ID {p.Id}: {p.Name}] 열정: {data.PassionValue}점 (가중치: {data.TalkWeight:F2}) | 사교성: {data.SociabilityValue}점 (휴식 쿨타임: {data.PersonalCooldown:F1}초)</color>");
            }
        }

        Debug.Log($"<color=purple>★ 등록 완료된 AI 총 {aiDataMap.Count}명</color>");
        Debug.Log("<color=purple>==================================================</color>");
    }

    // 💡 2. 발언 요청 시 추첨 수행 및 상세 디버그 출력
    public Participant? SelectNextSpeaker()
    {
        // 글로벌 LLM 쿨타임(2초) 체크
        if (isGlobalCooldownActive)
        {
            Debug.Log("<color=orange>[AITalkScheduler] 글로벌 LLM 쿨타임(2초) 진행 중입니다. 발언 요청 거절됨.</color>");
            return null;
        }

        candidateList.Clear();
        float totalWeight = 0f;
        float currentTime = Time.time;

        // 후보 AI 선별
        foreach (var kvp in aiDataMap)
        {
            var data = kvp.Value;

            // 조건: 생존 + 쿨타임 해제 시각 도달
            if (data.Participant!.IsAlive && currentTime >= data.CooldownEndTime)
            {
                candidateList.Add(data);
                totalWeight += data.TalkWeight;
                mentionedParticipants.TryGetValue(data.Participant, out int mentionCount);
                totalWeight += mentionCount * mentionedWeight;
            }
            else if (data.Participant.IsAlive && currentTime < data.CooldownEndTime)
            {
                float remainTime = data.CooldownEndTime - currentTime;
                Debug.Log($"<color=gray>   (대기 중) [ID {data.Participant.Id} {data.Participant.Name}] 남은 휴식 쿨타임: {remainTime:F1}초</color>");
            }
        }

        if (candidateList.Count == 0)
        {
            Debug.LogWarning("<color=yellow>[AITalkScheduler] 현재 발언 가능한 AI가 없습니다 (모두 휴식 쿨타임 진행 중).</color>");
            return null;
        }

        // 가중치 기반 무작위 추첨
        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        for (int i = 0; i < candidateList.Count; i++)
        {
            currentSum += candidateList[i].TalkWeight;
            if (randomVal <= currentSum)
            {
                AICooldownData selected = candidateList[i];

                // 💡 [콘솔 디버그 로그] 추첨 결과 출력
                Debug.Log($"<color=lime>==================================================</color>");
                Debug.Log($"<color=lime>★ [발언자 추첨 성공] ID: {selected.Participant!.Id} | 이름: {selected.Participant.Name}</color>");
                Debug.Log($"<color=lime>   - 열정 수치: {selected.PassionValue}점 (가중치 {selected.TalkWeight:F2} / 총합 {totalWeight:F2})</color>");
                Debug.Log($"<color=lime>   - 연속 발언: {selected.RecentTalkCount + 1} / {maxTalkCountBeforeRest}회</color>");
                Debug.Log($"<color=lime>==================================================</color>");
                mentionedParticipants[selected.Participant] = 0;
                OnAISpoken(selected);
                return selected.Participant;
            }
        }

        return candidateList[0].Participant;
    }

    // 💡 3. AI 발언 확정 및 쿨타임 처리 로그
    private void OnAISpoken(AICooldownData data)
    {
        data.RecentTalkCount++;

        // 지정 발언 횟수(예: 3회) 도달 시 개인 쿨타임 진입
        if (data.RecentTalkCount >= maxTalkCountBeforeRest)
        {
            data.CooldownEndTime = Time.time + data.PersonalCooldown;
            data.RecentTalkCount = 0;

            Debug.Log($"<color=yellow>★ [휴식 진입] [ID {data.Participant!.Id} {data.Participant.Name}] 님이 {maxTalkCountBeforeRest}회 발언하여 사교성({data.SociabilityValue}점) 반영 쿨타임({data.PersonalCooldown:F1}초)이 적용됩니다.</color>");
        }

        StartCoroutine(GlobalCooldownRoutine());
    }

    private IEnumerator GlobalCooldownRoutine()
    {
        isGlobalCooldownActive = true;
        yield return new WaitForSeconds(globalLlmCooldown);
        isGlobalCooldownActive = false;
        Debug.Log($"<color=cyan>[AITalkScheduler] 글로벌 LLM 쿨타임({globalLlmCooldown}초) 해제됨. 다음 발언 요청 가능.</color>");
    }

    // --- 스탯 계산 헬퍼 메서드 ---
    private float CalculatePersonalCooldown(Participant p)
    {
        if (p == null || p.Personality == null) return basePersonalCooldown;

        int rawValue = p.Personality.GetStatValue(sociabilityStatKey);
        float normalized = Mathf.Clamp01((rawValue - 1) / 99.0f);
        return Mathf.Lerp(basePersonalCooldown, minPersonalCooldown, normalized);
    }

    private float CalculateTalkWeight(Participant p)
    {
        if (p == null || p.Personality == null) return 1.0f;

        int rawValue = p.Personality.GetStatValue(passionStatKey);
        float normalized = Mathf.Clamp01((rawValue - 1) / 99.0f);
        return 1.0f + (normalized * 4.0f);
    }

    // 외부에서 ID만 바로 필요할 때 쓰는 단축 메서드
    public int SelectNextSpeakerId()
    {
        Participant? selected = SelectNextSpeaker();
        return selected != null ? selected.Id : -1;
    }

    // 💡 2단계: AI 대화 생성 및 ChatService 전파 처리
    private async Task ProcessAIDayChatAsync(Participant speaker)
    {
        //LLMPrompt llmPrompt = new LLMPrompt();
        PromptRepository repo = new PromptRepository();
        LLMPrompt llmPrompt = new LLMPrompt(repo);

        ChatService chatService = ChatService.GetInstance();

        // ① 1단계에서 저장한 AI의 대화 기억 장부(GameConversation) 꺼내기
        var conversation = chatService.GetGameConversationById(speaker.Id);
        if (conversation == null) return;

        // ② "성격에 맞게 대화해봐" 지시어 생성
        // 💡 [수정] speaker와 전체 참가자 리스트(GameDataManager.Instance.Participants)를 전달!
        var allParticipants = GameDataManager.Instance.Participants.ToList();
        string queryPrompt = llmPrompt.GetConversationPrompt(speaker, allParticipants);

        // ③ OpenAIChatManager로 LLM API 호출
        string? aiReply = await OpenAIChatManager.Instance!.SendChatRequest(conversation, queryPrompt, LLMResponseFormat.JsonObject);

        if (!string.IsNullOrEmpty(aiReply))
        {
            ChatTarget? chatTarget = llmPrompt.GetChatTarget(aiReply);
            if (chatTarget == null)
            {
                Debug.LogError("AI가 json 형식에 맞게 대답하지 않았습니다.");
                return;
            }

            foreach (Participant participant in GameDataManager.Instance.Participants)
            {
                if (!participant.IsAI && chatTarget.target == participant.Name) break;
                if (participant.Name != chatTarget.target) continue;
                mentionedParticipants[participant]++;
                break;
            }
            mentionedParticipants[speaker] = 0;


            // 기존 : ChatController.Instance!.AddChat($"[{speaker.Name}] : {chatTarget.content}");
            // 변경 : 발신자 이름과 대화 내용을 따로 넘겨서 말풍선으로 생성!

            /*if (ChatController.Instance != null)
            {
                ChatController.Instance.AddChat(speaker.Name, chatTarget.content);
            }*/

            // ④ 메시지 생성
            OpenAIMessage message = new OpenAIMessage
            {
                role = LLMRole.User, // ChatService가 본인/남 분기 처리해줌
                name = speaker.Name,
                content = aiReply
            };

            // ⑤ 🔥 [2단계 핵심] 모든 AI 대화 장부(ChatData)에 전파!
            chatService.AddMessageByDefault(message);

            // ⑥ UI 표시용 텍스트 추가 (UI 매니저나 채팅 UI 뷰가 있다면 전달만 수행)
            // ChatUI.Instance.ShowMessage(speaker.Name, aiReply);

            // 싱글톤을 사용하여 AI 말풍선 동적 생성
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.CreateAIMessage(speaker.Name, chatTarget.content);
            }

            Debug.Log($"<color=cyan>[{speaker.Name}]</color> : {aiReply}");
        }
    }

    public void AddMentionedParticipant(string participantName)
    {
        foreach (Participant p in GameDataManager.Instance.Participants)
        {
            if (p.Name == participantName)
            {
                mentionedParticipants[p]++;
                break;
            }
        }
    }
}