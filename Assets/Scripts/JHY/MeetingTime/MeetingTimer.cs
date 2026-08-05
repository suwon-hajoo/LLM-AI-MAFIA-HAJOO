using UnityEngine;
using TMPro;

public class MeetingTimer : MonoBehaviour
{
    [Header("회의 시간")]
    [SerializeField] private float durationSeconds = 180f; // 3분 = 180초

    [SerializeField] private VoteUIController voteUIController;

    private float currentTimer;
    private bool isTimerRunning = false;

    public int minutes;
    public int seconds;

    private void Start()
    {
        // 최초 시작
        ResetAndStartTimer();
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        if (currentTimer > 0f)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                isTimerRunning = false;
                OnTimerEnd();
            }

            TimerUpdate();
        }
    }

    // 💡 [추가] 회의 타이머 및 AI 대화 루프를 완전히 리셋하고 다시 시작하는 함수
    public void ResetAndStartTimer()
    {
        currentTimer = durationSeconds;
        isTimerRunning = true;
        TimerUpdate();

        // AI 스케줄러가 있다면 자동 대화 루프 재가동
        if (AITalkScheduler.Instance != null)
        {
            AITalkScheduler.Instance.StartAutoScheduleLoop();
        }

        //  [SystemMessage] 아침 시작 메시지 출력
        if (GameDataManager.Instance != null)
        {
            int currentDay = GameDataManager.Instance.CurrentDay;
            SystemMessageManager.Instance?.AddDaySystemMessage(currentDay, "아침이 시작되었습니다.");
        }
    }

    public void SkipMetting()
    {
        currentTimer = 0f;
        minutes = 0;
        seconds = 0;
        isTimerRunning = false;
        OnTimerEnd();
    }

    public void TimerUpdate()
    {
        minutes = Mathf.FloorToInt(currentTimer / 60f);
        seconds = Mathf.FloorToInt(currentTimer % 60f);
    }

    private void OnTimerEnd()
    {
        Debug.Log("타이머가 종료되었습니다.");

        if (voteUIController != null)
        {
            voteUIController.OpenVotePanel();
        }
    }
}