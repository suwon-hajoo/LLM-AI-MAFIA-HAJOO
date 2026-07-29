#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AINightSkillProcessor
{
    private LLMPrompt llmPrompt = new LLMPrompt();

    public async Task ProcessAllAINightSkillsAsync()
    {
        ChatService chatService = ChatService.GetInstance();
        var allParticipants = GameDataManager.Instance.Participants;

        List<Participant> aliveList = allParticipants.Where(p => p.IsAlive).ToList();

        foreach (var aiPlayer in aliveList)
        {
            Debug.Log($"<color=cyan>[현재 AI 직업]</color> [{aiPlayer.Role.RoleId}]");

            // AI 플레이어이고, 시민(Citizen)처럼 능력이 없는 직업이 아닌 경우만 실행
            if (aiPlayer.IsAI && aiPlayer.Role.RoleId != "Citizen")
            {
                Debug.Log($"<color=yellow>[밤 능력 처리 검증] ID: {aiPlayer.Id} | 이름: {aiPlayer.Name} | 실제 직업: {aiPlayer.Role.RoleId}</color>");

                // 💡 [추가] AI 1명마다 예외 처리를 감싸서, 한 명의 통신에 실패해도 전체 처리 루프가 멈추지 않고 끝까지 완수되도록 설정
                try
                {
                    var conversation = chatService.GetGameConversationById(aiPlayer.Id);
                    if (conversation == null) continue;

                    // 2. 능력 사용 프롬프트 생성
                    string abilityPrompt = llmPrompt.GetAbilityPrompt(aiPlayer, aliveList);

                    // 3. LLM에 JSON 형태 결과 요청
                    string? jsonResponse = await OpenAIChatManager.Instance!.SendChatRequest(conversation, abilityPrompt, "json_object");

                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        AbilityTarget? targetResult = llmPrompt.GetAbilityTarget(jsonResponse);

                        if (targetResult != null && !string.IsNullOrEmpty(targetResult.target))
                        {
                            Participant targetPerson = aliveList.FirstOrDefault(p => p.Name == targetResult.target);
                            if (targetPerson == null) continue;

                            // 4. 직업별 C# 스킬 클래스 생성 및 실행
                            INightSkill aiSkill = NightSkillFactory.CreateSkill(aiPlayer.Role.RoleId);
                            aiSkill.ExecuteSkill(aiPlayer.Id, targetPerson.Id);

                            // 5. [스파이 특수 처리] 접촉 대상이 마피아인 경우 마피아 장부에도 알림 주입
                            if (aiPlayer.Role.RoleId == "Spy" && targetPerson.Role.RoleId == "Mafia")
                            {
                                string mafiaNotificationContent = $"[조직원 접촉 알림] 스파이인 '{aiPlayer.Name}' 님이 당신에게 접촉했습니다. 서로 마피아 팀임을 확인했습니다.";

                                OpenAIMessage mafiaMessage = new OpenAIMessage
                                {
                                    role = LLMRole.System,
                                    name = "시스템",
                                    content = mafiaNotificationContent
                                };

                                // 마피아 장부에 비밀 기록 추가
                                chatService.AddMessageById(targetPerson.Id, mafiaMessage);
                                //chatService.AddMessageByTeam(Team.Mafia, systemNotification); // 모든 마피아 팀에게 전달
                                Debug.Log($"<color=magenta>[스파이 접촉 완료]</color> [{targetPerson.Name}](마피아) 장부에 스파이 정체 통보 추가");
                            }

                            // 6. 능력 결과 텍스트 도출 및 해당 AI(스파이 본인 등) 단독 통보
                            string resultMessageContent = BuildSkillResultMessage(aiPlayer, targetPerson);

                            if (!string.IsNullOrEmpty(resultMessageContent))
                            {
                                OpenAIMessage systemNotification = new OpenAIMessage
                                {
                                    role = LLMRole.System,
                                    name = "시스템",
                                    content = resultMessageContent
                                };

                                chatService.AddMessageById(aiPlayer.Id, systemNotification);
                                Debug.Log($"<color=cyan>[비밀 통보 완료]</color> [{aiPlayer.Name}] 전용 기록: {resultMessageContent}");
                            }
                        }
                    }
                }
                catch (Exception ex) // 💡 [추가] 통신 오류나 파싱 실패 시 에러 로그를 출력하고 다음 AI 처리 계속 진행
                {
                    Debug.LogError($"<color=red>[AI 밤 능력 예외 발생] ID: {aiPlayer.Id} ({aiPlayer.Name}) - {ex.Message}</color>");
                }
            }
        }
    }

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

            case "Spy":
                bool foundMafia = target.Role.RoleId == "Mafia";
                if (foundMafia)
                {
                    return $"[스파이 접촉 성공] '{target.Name}' 님은 마피아입니다! 서로의 정체를 확인했습니다.";
                }
                else
                {
                    return $"[스파이 접촉 실패] '{target.Name}' 님은 마피아가 아닙니다. 정체를 밝히지 못했습니다.";
                }

            default:
                return "";
        }
    }
}