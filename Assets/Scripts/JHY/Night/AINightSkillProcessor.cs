using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AINightSkillProcessor
{
    private LLMPrompt llmPrompt = new LLMPrompt();

    // 💡 [4단계 핵심] 살아있는 모든 AI들의 밤 능력을 일괄 처리하는 메서드
    public async Task ProcessAllAINightSkillsAsync()
    {
        ChatService chatService = ChatService.GetInstance();
        var allParticipants = GameDataManager.Instance.Participants;

        // 1. 살아있는 플레이어 목록 추려내기
        List<Participant> aliveList = allParticipants.Where(p => p.IsAlive).ToList();

        foreach (var aiPlayer in aliveList)
        {
            // AI 플레이어이고, 시민(Citizen)처럼 능력이 없는 직업이 아닌 경우만 실행
            if (aiPlayer.IsAI && aiPlayer.Role.RoleId != "Citizen")
            {
                var conversation = chatService.GetGameConversationById(aiPlayer.Id);
                if (conversation == null) continue;

                // 2. 능력 사용 프롬프트 생성
                string abilityPrompt = llmPrompt.GetAbilityPrompt(aiPlayer, aliveList);

                // 3. LLM에 JSON 형태 결과 요청
                string? jsonResponse = await OpenAIChatManager.Instance.SendChatRequest(conversation, abilityPrompt, "json_object");

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    // 4. JSON 파싱하여 타겟 이름 얻기
                    AbilityTarget? targetResult = llmPrompt.GetAbilityTarget(jsonResponse);

                    if (targetResult != null && !string.IsNullOrEmpty(targetResult.target))
                    {
                        Participant targetPerson = aliveList.FirstOrDefault(p => p.Name == targetResult.target);
                        if (targetPerson == null) continue;

                        // 5. 직업별 C# 스킬 클래스 생성 및 실행
                        INightSkill aiSkill = NightSkillFactory.CreateSkill(aiPlayer.Role.RoleId);
                        aiSkill.ExecuteSkill(aiPlayer.Id, targetPerson.Id);

                        // 6. 🔥 [4단계의 핵심!] 능력 결과 텍스트 도출 및 해당 AI 단독 통보
                        string resultMessageContent = BuildSkillResultMessage(aiPlayer, targetPerson);

                        if (!string.IsNullOrEmpty(resultMessageContent))
                        {
                            OpenAIMessage systemNotification = new OpenAIMessage
                            {
                                role = LLMRole.User, // 또는 system
                                name = "시스템",
                                content = resultMessageContent
                            };

                            // 🌟 AddMessageById를 써서 다른 플레이어 몰래 '해당 AI 장부'에만 비밀 기록!
                            chatService.AddMessageById(aiPlayer.Id, systemNotification);

                            Debug.Log($"<color=cyan>[4단계 비밀 통보 완료]</color> [{aiPlayer.Name}] 전용 기록: {resultMessageContent}");
                        }
                    }
                }
            }
        }
    }

    // 직업별 능력 사용 결과 문장 조립 헬퍼 함수
    private string BuildSkillResultMessage(Participant actor, Participant target)
    {
        switch (actor.Role.RoleId)
        {
            case "Police":
                bool isMafia = target.Role.RoleId == "Mafia";
                return $"[경찰 조사 결과] 당신이 조사한 '{target.Name}' 님은 {(isMafia ? "마피아가 맞습니다." : "마피아가 아닙니다.")}";

            case "Doctor":
                return $"[의사 능력 성공] 당신은 오늘 밤 '{target.Name}' 님을 마피아의 공격으로부터 보호 대상으로 지정했습니다.";

            case "Mafia":
                return $"[마피아 습격 지정] 오늘 밤 조직원들과 함께 '{target.Name}' 님을 습격 대상으로 지정했습니다.";

            default:
                return "";
        }
    }
}