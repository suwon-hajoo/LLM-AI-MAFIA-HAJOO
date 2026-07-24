using UnityEngine;

public class TestTalkTrigger : MonoBehaviour
{
    // Update() 함수를 지우면 키 충돌 에러가 사라집니다.

    // UI 버튼의 OnClick() 이벤트에 연결해서 테스트!
    public void TestSelectSpeaker()
    {
        if (AITalkScheduler.Instance == null)
        {
            Debug.LogError("AITalkScheduler가 씬에 없습니다!");
            return;
        }

        Participant selectedAI = AITalkScheduler.Instance.SelectNextSpeaker();

        if (selectedAI != null)
        {
            Debug.Log($"<color=lime>★ [발언권 획득] ID: {selectedAI.Id} | 이름: {selectedAI.Name}</color>");
        }
    }
}