using System.Linq;
using UnityEngine;

public class SpySkill : INightSkill
{
    public string RoleId => "Spy";
    public string RoleName => "스파이";

    public int SelectedTargetId { get; private set; } = -1;

    public void ExecuteSkill(int actorId, int targetId)
    {
        SelectedTargetId = targetId;
        Debug.Log($"<color=magenta>[스파이] {targetId}번 참가자에게 접촉을 시도합니다.</color>");

        // 1. 스파이 본인과 대상 정보 가져오기
        Participant actor = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == actorId);
        Participant target = GameDataManager.Instance.Participants.FirstOrDefault(p => p.Id == targetId);

        if (actor == null || target == null) return;

        if (actor.IsAI == true) return;

        bool foundMafia = target.Role.RoleId == "Mafia";

        // 2. 접촉 대상이 마피아인 경우 ➔ 마피아 장부(ChatData)에 스파이 정체 통보 메시지 주입
        if (foundMafia)
        {
            string mafiaNotificationContent = $"[조직원 접촉 알림] 스파이인 '{actor.Name}' 님이 당신에게 접촉했습니다. 서로 마피아 팀임을 확인했습니다.";

            OpenAIMessage mafiaMessage = new OpenAIMessage
            {
                role = LLMRole.System,
                name = "시스템",
                content = mafiaNotificationContent
            };

            // 마피아 팀 전체(또는 해당 마피아) 장부에 알림 추가
            ChatService.GetInstance().AddMessageByTeam(Team.Mafia, mafiaMessage);

            Debug.Log($"<color=magenta>[스파이 접촉 성공]</color> 스파이({actor.Name})가 마피아({target.Name})에게 접선하여 장부에 기록되었습니다.");
        }

        /*// 3. 스파이 본인이 AI인 경우 ➔ 본인 장부에도 접선 결과 기록
        if (actor.IsAI)
        {
            string resultText = foundMafia
                ? $"[스파이 접촉 성공] '{target.Name}' 님은 마피아입니다! 서로의 정체를 확인했습니다."
                : $"[스파이 접촉 실패] '{target.Name}' 님은 마피아가 아닙니다.";

            OpenAIMessage sysMsg = new OpenAIMessage
            {
                role = LLMRole.System,
                name = "시스템",
                content = resultText
            };

            ChatService.GetInstance().AddMessageById(actorId, sysMsg);
        }*/
    }

    public void ResolveSkill()
    {
        // 정산은 밤 끝날 때 처리
    }
}