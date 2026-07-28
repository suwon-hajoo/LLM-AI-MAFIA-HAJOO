using UnityEngine;
using TMPro; // TextMeshPro UI 사용
using UnityEngine.SceneManagement;

public class NameInputUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_InputField nameInputField; // 유저 이름 입력창

    private void Start()
    {
        // 씬 시작 시 기본적으로 무작위 이름 하나를 띄워둠 (선택 사항)
        OnClickRandomNameButton();
    }

    // 🎲 [랜덤 버튼 클릭 이벤트] 인풋 필드의 글자만 바꾸어 줌
    public void OnClickRandomNameButton()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.NameListSO != null)
        {
            string randomName = GameDataManager.Instance.NameListSO.GetRandomName();
            nameInputField.text = randomName; // 인풋필드 텍스트 갱신만 수행
        }
    }

    // 🚀 [게임 시작 버튼 클릭 이벤트] 입력된 이름을 GameDataManager에 전달하고 씬 이동
    public void OnClickStartGameButton()
    {
        string finalUserName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(finalUserName))
        {
            Debug.LogWarning("이름이 비어있습니다! 랜덤 이름을 지정합니다.");
            finalUserName = GameDataManager.Instance.NameListSO.GetRandomName();
        }

        // 1. 유저 이름과 함께 데이터 초기화 실행 (중복 제거 및 AI 이름 생성)
        GameDataManager.Instance.InitializeAndAssignRoles(finalUserName);

        GameDataManager.Instance.OneDay = true;

        // 2. Scene2로 이동
        SceneManager.LoadScene("Meeting_Scenes");
    }
}