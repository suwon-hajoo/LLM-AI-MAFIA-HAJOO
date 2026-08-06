#nullable enable
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class InputBoxResizer : MonoBehaviour
{
    [Header("연결 대상")]
    [SerializeField] private TMP_InputField? inputField;
    [SerializeField] private RectTransform? inputRectTransform;

    [Header("높이 세팅")]
    [SerializeField] private float baseHeight = 47f; // 1줄 기본 높이
    [SerializeField] private float lineHeight = 16f; // 줄당 추가 높이

    private void Awake()
    {
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
        if (inputRectTransform == null) inputRectTransform = GetComponent<RectTransform>();

        if (inputRectTransform != null)
        {
            inputRectTransform.pivot = new Vector2(inputRectTransform.pivot.x, 0f);
        }
    }

    private void OnEnable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(HandleInputValueChanged);
        }
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(HandleInputValueChanged);
        }
    }

    private void HandleInputValueChanged(string text)
    {
        if (inputField == null || inputField.textComponent == null || inputRectTransform == null) return;

        // 💡 핵심: 인스펙터 설정(Margin) 없이 코드 내부에서 수학적으로 해결!
        // 실제 박스 너비에서 커서가 들어갈 가상의 최소 공간(-10f)을 뺀 '계산용 너비'를 만듭니다.
        float calculationWidth = inputField.textComponent.rectTransform.rect.width - 10f;

        // 좁혀진 가상 너비를 기준으로 높이를 미리 시뮬레이션하여 1글자 늦게 늘어나는 현상을 막습니다.
        float exactHeight = inputField.textComponent.GetPreferredValues(text, calculationWidth, 0).y;
        float singleLineHeight = inputField.textComponent.GetPreferredValues("A", calculationWidth, 0).y;

        int calculatedLines = 1;

        if (exactHeight > singleLineHeight + 1f)
        {
            calculatedLines = 1 + Mathf.RoundToInt((exactHeight - singleLineHeight) / lineHeight);
        }

        if (string.IsNullOrEmpty(text) || calculatedLines <= 1)
        {
            inputRectTransform.sizeDelta = new Vector2(inputRectTransform.sizeDelta.x, baseHeight);
        }
        else
        {
            float targetHeight = baseHeight + ((calculatedLines - 1) * lineHeight);
            inputRectTransform.sizeDelta = new Vector2(inputRectTransform.sizeDelta.x, targetHeight);
        }
    }
}