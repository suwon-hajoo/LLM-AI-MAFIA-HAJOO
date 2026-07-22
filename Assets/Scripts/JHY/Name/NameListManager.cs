using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameListManager : MonoBehaviour
{
    [Header("UI 연결 부분")]
    [SerializeField] private Transform nameListPanel; // Grid Layout Group이 붙어있는 부모 패널
    [SerializeField] private GameObject namePrefab;    // Project 창에 만든 이름 상자 프리팹 (NameItem)

    private void Start()
    {
        // 1. 싱글톤 GameDataManager가 정상적으로 들어와 있는지 검사
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("오류: GameDataManager를 찾을 수 없습니다! 첫 번째 씬에서 시작했는지 확인해 주세요.");
            return;
        }

        // 2. GameDataManager에서 생성된 전체 참가자(유저 + AI) 데이터 가져오기
        IReadOnlyList<Participant> participants = GameDataManager.Instance.Participants;

        // 3. UI에 생성 및 배치 실행
        UpdateParticipantList(participants);
    }

    // 참가자 목록 데이터를 받아서 UI를 새로 배치하는 함수
    public void UpdateParticipantList(IReadOnlyList<Participant> participants)
    {
        if (nameListPanel == null || namePrefab == null)
        {
            Debug.LogWarning("경고: nameListPanel 또는 namePrefab이 인스펙터에 연결되지 않았습니다.");
            return;
        }

        // [A단계] 기존에 혹시 생성되어 남아있던 UI들을 깨끗하게 삭제 (초기화)
        foreach (Transform child in nameListPanel)
        {
            Destroy(child.gameObject);
        }

        // [B단계] 데이터에 있는 참가자(유저 + AI) 수만큼 순회하며 생성
        foreach (var p in participants)
        {
            // 1) 프리팹 설계도를 복사해서 nameListPanel의 자식으로 생성
            GameObject newObj = Instantiate(namePrefab, nameListPanel);

            // 2) 생성된 프리팹 안에서 TextMeshProUGUI 컴포넌트 찾기
            TextMeshProUGUI textComp = newObj.GetComponentInChildren<TextMeshProUGUI>();

            // 3) 참가자의 이름(p.Name)을 UI 텍스트에 적용
            if (textComp != null)
            {
                // 유저인 경우 강조 표시를 원하면 살짝 가공도 가능합니다. (예: $"[★] {p.Name}" 등)
                textComp.text = p.Name;
            }
        }

        // [C단계] 부모 패널(nameListPanel)의 Grid Layout Group 설정(Start Axis: Vertical, Row Count: 4)에 의해
        // 1~4번째 인원은 1열 세로, 5~8번째 인원은 2열 세로로 자동 정렬됩니다!
    }
}