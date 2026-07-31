using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainUiManager : MonoBehaviour
{
    [Header("Main버튼 모음")]
    [SerializeField] private Button GameStartButton;
    [SerializeField] private Button CloseNameInputButton;

    [Header("NameInput 패널")]
    [SerializeField] private GameObject NameInputPanel;

    private void Awake()
    {
        // 프레임을 60으로 고정
        Application.targetFrameRate = 60;

        NameInputPanel.SetActive(false);

        GameStartButton.onClick.AddListener(OnGameStartButtonClicked);
        CloseNameInputButton.onClick.AddListener(OnCloseNameInputPanel);
    }

    private void Update()
    {
        
    }

    private void OnGameStartButtonClicked()
    {
        Debug.Log("게임 시작 버튼 클릭됨!");
        NameInputPanel.SetActive(true);
    }

    private void OnCloseNameInputPanel()
    {
        NameInputPanel.SetActive(false);
    }
}
