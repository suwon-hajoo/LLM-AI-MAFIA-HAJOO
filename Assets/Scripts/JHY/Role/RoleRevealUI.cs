using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // TextMeshPro 사용 시 (일반 Text라면 UnityEngine.UI.Text 사용)

public class RoleRevealUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image roleImage;        // 역할 카드/일러스트 이미지
    [SerializeField] private TMP_Text roleNameText;  // 역할 이름 (ex: 마피아, 의사)
    [SerializeField] private TMP_Text roleNameText2;  // 역할 이름 (ex: 마피아, 의사)
    [SerializeField] private TMP_Text roleNameText3;  // 역할 이름 (ex: 마피아, 의사)
    [SerializeField] private TMP_Text teamText;      // 진영 이름 (ex: 마피아 진영, 시민 진영)
    [SerializeField] private TMP_Text descriptionText; // 역할 설명 문구

    [Header("선택 사항: Confirm 버튼")]
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        DisplayMyRole();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    private void DisplayMyRole()
    {
        // GameDataManager 싱글톤을 통해 플레이어 본인 정보 가져오기
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("GameDataManager를 찾을 수 없습니다!");
            return;
        }

        Participant myData = GameDataManager.Instance.GetMyParticipantData();

        if (myData == null || myData.Role == null)
        {
            Debug.LogError("플레이어의 역할 정보가 존재하지 않습니다.");
            return;
        }

        RoleData myRole = myData.Role;

        // 1. 역할 이미지 갱신 (이미지 설정이 안 된 경우 대비)
        if (roleImage != null)
        {
            if (myRole.RoleIcon != null)
            {
                roleImage.sprite = myRole.RoleIcon;
                roleImage.gameObject.SetActive(true);
            }
            else
            {
                // 스프라이트가 없는 경우 비활성화 또는 기본 더미 이미지
                roleImage.gameObject.SetActive(false);
            }
        }

        // 2. 텍스트 갱신
        if (roleNameText != null) roleNameText.text = myRole.RoleName;
        if (roleNameText != null) roleNameText2.text = myRole.RoleName;
        if (roleNameText != null) roleNameText3.text = myRole.RoleName;
        if (descriptionText != null) descriptionText.text = myRole.Description;

        // 3. 진영 표기
        if (teamText != null)
        {
            switch (myRole.Team)
            {
                case Team.Citizen:
                    teamText.text = "시민 진영";
                    teamText.color = Color.green;
                    break;
                case Team.Mafia:
                    teamText.text = "마피아 진영";
                    teamText.color = Color.red;
                    break;
                case Team.Neutral:
                    teamText.text = "중립 진영";
                    teamText.color = Color.yellow;
                    break;
            }
        }
    }

    // 확인 버튼 눌렀을 때 실행될 로직 (팝업 닫기 또는 Chat 씬 이동)
    private void OnConfirmButtonClicked()
    {
        // 상황 1: 현재 씬이 역할 공개 씬일 경우 -> Chat 씬으로 이동
        // UnityEngine.SceneManagement.SceneManager.LoadScene("Chat");

        // 상황 2: Chat 씬 내부의 팝업 창일 경우 -> 팝업 비활성화
        gameObject.SetActive(false);

        SceneManager.LoadScene("Mafia_Meeting_Scene");
    }
}