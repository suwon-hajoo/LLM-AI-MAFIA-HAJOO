using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    [SerializeField] private TMP_Text senderNameText; // 발신자 이름
    [SerializeField] private TMP_Text contentText;    // 대화 본문 내용
    [SerializeField] private TMP_Text timeText;       // 발언 시각

    [Header("UI 높이 조절용 드래그 연결")]
    [SerializeField] private RectTransform headerRect;     // [Paragraph] (상단 시간/이름)
    [SerializeField] private RectTransform contentBoxRect; // [Overlay+Border] (본문 대화창)
    [SerializeField] private RectTransform containerRect;  // [Container] (중간 부모)

    [Header("여백 설정")]
    [SerializeField] private float verticalPadding = 20f;   // 위아래/요소간 여백 보정값

    public void SetMessage(string senderName, string content, string timeStr = "")
    {
        if (senderNameText != null) senderNameText.text = senderName;
        if (contentText != null) contentText.text = content;
        if (timeText != null)
        {
            timeText.text = string.IsNullOrEmpty(timeStr)
                ? System.DateTime.Now.ToString("HH:mm")
                : timeStr;
        }

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (contentBoxRect == null) return;

        // ★ 추가: 헤더(Paragraph) 내부의 이름과 시간 텍스트 가로 배치를 즉시 재계산!
        if (headerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);
        }

        // 1. 본문 박스 텍스트 길이에 맞춰 높이 즉시 계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentBoxRect);

        // 2. 헤더 + 본문 박스 + 여백을 합산하여 총 높이 산출
        float headerHeight = (headerRect != null) ? headerRect.rect.height : 0f;
        float contentHeight = contentBoxRect.rect.height;
        float totalHeight = headerHeight + contentHeight + verticalPadding;

        // 3. 인스펙터로 연결해 둔 [Container] 높이 변경
        if (containerRect != null)
        {
            containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
        }

        // 4. 스크립트가 붙어있는 [User_Message](최상위 자기 자신) 높이 변경
        RectTransform selfRect = GetComponent<RectTransform>();
        if (selfRect != null)
        {
            selfRect.sizeDelta = new Vector2(selfRect.sizeDelta.x, totalHeight);
        }
    }
}