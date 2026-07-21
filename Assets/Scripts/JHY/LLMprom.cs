using UnityEngine;

public class LLMprom
{
    /*// 나중에 LLM 통신 요청 시 System 프롬프트 조립 예시
    public string BuildSystemPromptForAi(Participant ai)
    {
        StringBuilder sb = new StringBuilder();

        // 1. 이름과 정체 지정
        sb.AppendLine($"너의 이름은 '{ai.Name}'이고, 마피아 게임 참가자이다.");
        sb.AppendLine($"너의 숨겨진 역할은 [{ai.Role.RoleName}]이다.");
        sb.AppendLine();

        // 2. 💡 방금 구상한 11가지 성격 스탯 텍스트 통째로 추출하여 삽입!
        sb.AppendLine(ai.Personality.GetPromptRawStats());
        sb.AppendLine();

        // 3. LLM 행동 지침
        sb.AppendLine("위 1~100 성격 수치를 반영하여 말투와 대화 톤을 정해라.");
        sb.AppendLine("100에 가까울수록 해당 성향이 매우 강하고, 1에 가까울수록 반대 성향을 띱니다.");
        sb.AppendLine("자신의 정체를 들키지 말고 한국어 2~3문장으로 답변해라.");

        return sb.ToString();
    }*/
}
