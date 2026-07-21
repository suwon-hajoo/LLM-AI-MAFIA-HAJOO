using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    // 인스펙터에 노출하되 외부 C# 코드에서는 변경 불가
    [SerializeField] private List<RoleData> gameRoles = new List<RoleData>();

    // 외부에는 읽기 전용(IReadOnlyList)으로만 노출
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

    // 나중에 변경 필요
    private void Start()
    {
        // 게임이 시작되자마자 참가자 8명 생성 및 역할 무작위 분배 실행!
        //InitializeAndAssignRoles();
    }

    public void RoleButton()
    {
        InitializeAndAssignRoles();
    }

    // 게임 시작 시 참가자 생성 및 역할 부여 실행
    public void InitializeAndAssignRoles()
    {
        _participants.Clear();

        // 유저 1명 (ID: 0)
        _participants.Add(new Participant(0, "나 (User)", false));

        // AI 7명 (ID: 1~7)
        for (int i = 1; i <= 7; i++)
        {
            _participants.Add(new Participant(i, $"AI 봇 {i}", true));
        }

        // 역할 분배 서비스 실행
        RoleAssigner assigner = new RoleAssigner();
        assigner.AssignRoles(_participants, gameRoles);
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