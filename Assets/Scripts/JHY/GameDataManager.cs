using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("역할 데이터 목록")]
    [SerializeField] private List<RoleData> gameRoles = new List<RoleData>();

    [Header("성격 스탯 에셋 목록 (11가지 SO 파일들을 여기에 드래그)")]
    [SerializeField] private List<PersonalityStatSO> personalityStats = new List<PersonalityStatSO>();

    // 💡 [추가] 사전 정의된 AI/유저용 이름 목록 ScriptableObject 에셋
    [Header("이름 데이터 목록")]
    [SerializeField] private NameListSO nameListSO;

    [Header("AI 참가자 수")]
    [SerializeField] private int AiParticipant;

    // 외부용 프로퍼티 (NameInputUI 등에서 접근용)
    public NameListSO NameListSO => nameListSO;

    private readonly List<Participant> _participants = new List<Participant>();
    public IReadOnlyList<Participant> Participants => _participants;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // UI를 통해 유저 이름을 전달받고 게임을 시작할 것이므로 주석 유지
        //InitializeAndAssignRoles("나 (User)");
    }

    // 테스트용 버튼 이벤트 (기존 RoleButton 활용 - 매개변수 없을 시 기본 이름 사용)
    public void RoleButton()
    {
        InitializeAndAssignRoles("나 (User)");
    }

    // 💡 [수정] 게임 시작 시 유저 이름을 입력받도록 매개변수(userName) 추가
    public void InitializeAndAssignRoles(string userName)
    {
        _participants.Clear();

        // 💡 [추가] 유저 이름 예외 처리 (공백이나 빈 칸일 경우 기본값 지정)
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "나 (User)";
        }

        // 1. 유저 생성 (입력받은 유저 이름 적용)
        _participants.Add(new Participant(0, userName, false, personalityStats));

        // 2. 💡 [추가] AI 이름 무작위 배정 및 유저 이름 중복 방지 로직
        List<string> availableNames = new List<string>();

        if (nameListSO != null && nameListSO.Names != null)
        {
            availableNames = new List<string>(nameListSO.Names);
        }

        // 💡 핵심: 유저가 직접 입력했거나 선택한 이름이 목록에 있다면 AI가 쓰지 못하도록 100% 제거!
        if (availableNames.Contains(userName))
        {
            availableNames.Remove(userName);
        }

        // 💡 AI 이름 목록 무작위 셔플 (Fisher-Yates 셔플)
        for (int i = 0; i < availableNames.Count; i++)
        {
            int randomIndex = Random.Range(i, availableNames.Count);
            string temp = availableNames[i];
            availableNames[i] = availableNames[randomIndex];
            availableNames[randomIndex] = temp;
        }

        // 3. AI AiParticipant명 생성 (중복 제거된 무작위 이름 할당)
        for (int i = 1; i <= AiParticipant; i++)
        {
            // 이름 데이터가 모자랄 경우를 대비한 안전 장치
            string aiName = (i - 1 < availableNames.Count) ? availableNames[i - 1] : $"AI 봇 {i}";
            _participants.Add(new Participant(i, aiName, true, personalityStats));
        }

        // 4. 역할 배정
        RoleAssigner assigner = new RoleAssigner();
        assigner.AssignRoles(_participants, gameRoles);

        // 5. 💡 [수정] 디버그 출력 시 유저 이름과 AI 이름이 각각 올바르게 찍히도록 변경
        Debug.Log("<color=yellow>========== [초기화 완료: 역할, 성격, 중복 없는 이름] ==========</color>");
        foreach (var p in _participants)
        {
            if (p.IsAI)
            {
                Debug.Log($"<color=cyan>[{p.Name}]</color> 역할: <color=lime>{p.Role.RoleName}</color> | 성격: {p.Personality.GetDebugLogString()}");
            }
            else
            {
                Debug.Log($"<color=orange>[★유저: {p.Name}]</color> 역할: <color=lime>{p.Role.RoleName}</color>");
            }
        }
    }

    // 유저(나)의 데이터만 추출하는 편의 함수
    public Participant GetMyParticipantData()
    {
        return _participants.Find(p => p.IsAI == false);
    }

    // 역할 분배 후 다음 씬으로 넘어가는 테스트
    public void GoToNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("JHYScene2");
    }
}