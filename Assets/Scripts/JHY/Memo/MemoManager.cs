using UnityEngine;
using TMPro;

public class MemoManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_InputField memoInputField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject MemoPanel;

    // 인덱스 1~7번을 그대로 쓰기 위해 크기를 8로 설정 (0번은 안 씀)
    private string[] memoContents = new string[8];

    // 현재 선택된 메모 번호 (1 ~ 7)
    private int currentMemoId = 1;

    private void Awake()
    {
        // 게임을 완전히 처음 켰을 때 1~7번 메모 초기화
        if (!PlayerPrefs.HasKey("Memo_1"))
        {
            ResetAllMemos();
        }
    }

    private void Start()
    {
        // PlayerPrefs에서 1~7번 메모 데이터 불러오기
        LoadAllMemos();

        // InputField 내용이 바뀔 때 실시간 저장
        memoInputField.onValueChanged.AddListener(OnMemoTextChanged);

        // 기본으로 1번 메모 선택
        SelectMemoById(1);

        MemoPanel.SetActive(false);
    }

    /// <summary>
    /// 저장소(PlayerPrefs)에서 1~7번 메모를 그대로 읽어옴
    /// </summary>
    private void LoadAllMemos()
    {
        for (int id = 1; id <= 7; id++)
        {
            memoContents[id] = PlayerPrefs.GetString($"Memo_{id}", $"메모 {id}번의 초기 내용입니다.");
        }
    }

    /// <summary>
    /// 1~7번 메모 전체 초기화
    /// </summary>
    public void ResetAllMemos()
    {
        for (int id = 1; id <= 7; id++)
        {
            memoContents[id] = $"메모 {id}번의 초기 내용입니다.";
            PlayerPrefs.SetString($"Memo_{id}", memoContents[id]);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// [버튼 클릭용] 1~7번 ID로 메모 선택
    /// </summary>
    /// <param name="memoId">메모 ID (1 ~ 7)</param>
    public void SelectMemoById(int memoId)
    {
        // 1~7 범위를 벗어나면 거름
        if (memoId < 1 || memoId > 7) return;

        currentMemoId = memoId;

        // 변환 없이 1~7번 인덱스 그대로 사용
        memoInputField.text = memoContents[currentMemoId];

        UpdateUI();
    }

    /// <summary>
    /// InputField 내용 수정 시 실시간 데이터 및 PlayerPrefs 업데이트
    /// </summary>
    private void OnMemoTextChanged(string newText)
    {
        if (currentMemoId >= 1 && currentMemoId <= 7)
        {
            // 인덱스 그대로 사용
            memoContents[currentMemoId] = newText;

            // PlayerPrefs 키 값도 ID 그대로 저장 ("Memo_1" ~ "Memo_7")
            PlayerPrefs.SetString($"Memo_{currentMemoId}", newText);
            PlayerPrefs.Save();
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"현재 메모: {currentMemoId}번";
        }
    }

    public void CloseMemoPanel()
    {
        MemoPanel.SetActive(false);
    }

    public void OpenMemoPanel()
    {
        MemoPanel.SetActive(true);
    }
}