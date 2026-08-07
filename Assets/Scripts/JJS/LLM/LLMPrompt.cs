#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class LLMPrompt
{
    private readonly PromptRepository repository;

    public LLMPrompt(PromptRepository repository)
    {
        this.repository = repository;
    }

    private string TeamToString(Team team) => team switch
    {
        Team.Citizen => "시민",
        Team.Mafia => "마피아",
        Team.Neutral => "중립",
        _ => ""
    };

    private string TeamPurpose(Team team) => team switch
    {
        Team.Citizen => "마피아를 찾아서 투표를 통해 잡는 것",
        Team.Mafia => "밤에 능력을 사용하여 시민을 죽이거나 투표를 시민에게 유도하여 죽여 살아남는 것",
        Team.Neutral => "특정 조건을 맞추는 것",
        _ => ""
    };

    // 팁을 RoleData 자체에 텍스트 필드로 두지 않고 스위치로 유지할 때의 예시
    public string GetTip(RoleData roleData) => roleData.RoleId switch
    {
        "Citizen" => "팁으로는 의심받지 말고 마피아를 찾는게 좋아\n",
        "Doctor" => "팁으로는 자신이 의사라는 걸 들키지 말고 마피아가 노릴 사람을 찾아서 능력을 사용하는게 좋아\n",
        "Mafia" => "팁으로는 자신이 마피아라는 것을 들키지 말고 시민 중에서 마피아를 위협하는 능력을 가진 사람을 찾아내서 밤에 죽이고 다른 사람을 마피아로 몰아서 다른 시민들이 그 사람을 투표로 죽이게 하는게 좋지\n",
        "Spy" => "팁으로는 자신이 스파이라는 것을 들키지 말고 마피아를 찾아서 접선해서 마피아가 죽지 않게 도와줘\n",
        "Police" => "팁으로는 최대한 빨리 마피아를 찾아내서 다른 시민들과 공유하면 이길 수 있어\n",
        _ => ""
    };

    public string GetSystemMessage(Participant participant, List<RoleData> gameRoles)
    {
        string rawTemplate = repository.GetTemplate("SystemTemplate");

        StringBuilder rolesSb = new();
        foreach (var gameRole in gameRoles)
        {
            rolesSb.AppendLine($"{gameRole.RoleName} : {gameRole.Description}");
        }


        // txt 파일에서 읽어온 템플릿에 데이터 치환(Replace) 적용
        return rawTemplate
            .Replace("{ALL_ROLES_LIST}", rolesSb.ToString());
    }

    // [낮 대화 프롬프트] 지침 ➔ [게임 상황 정보] 순서로 결합
    public string GetConversationPrompt(Participant participant, List<Participant> participantList)
    {
        string conversationInstructions = repository.GetTemplate("ConversationTemplate");
        string contextPrompt = GetPhaseContextPrompt(participant, participantList);

        // 💡 지침 뒤에 게임 상황 정보를 배치!
        return conversationInstructions + "\n\n" + contextPrompt;
    }

    private string GetPhaseContextPrompt(Participant participant, List<Participant> participantList)
    {
        string phaseContextRaw = repository.GetTemplate("PhaseContextTemplate");

        int totalCount = participantList.Count;
        int aliveCount = participantList.Count(p => p.IsAlive);
        int deadCount = totalCount - aliveCount;

        string aliveNames = string.Join(", ", participantList.Where(p => p.IsAlive).Select(p => p.Name));
        string deadNames = deadCount > 0
            ? string.Join(", ", participantList.Where(p => !p.IsAlive).Select(p => p.Name))
            : "없음";

        // 가변 동적 문자열 가공
        StringBuilder statsSb = new();
        foreach (var kvp in participant.Personality.Stats)
        {
            statsSb.AppendLine($"{kvp.Key.StatName} : {kvp.Value}점  ({kvp.Key.Description})");
        }

        return phaseContextRaw
            .Replace("{TOTAL_COUNT}", totalCount.ToString())
            .Replace("{ALIVE_COUNT}", aliveCount.ToString())
            .Replace("{DEAD_COUNT}", deadCount.ToString())
            .Replace("{MY_NAME}", participant.Name)
            .Replace("{ALIVE_PLAYERS}", GetAliveParticipantString(participantList))
            .Replace("{DEAD_PLAYERS}", GetDeadParticipantString(participantList))
            .Replace("{ROLE_TIP}", GetTip(participant.Role))
            .Replace("{TEAM_NAME}", TeamToString(participant.Role.Team))
            .Replace("{TEAM_PURPOSE}", TeamPurpose(participant.Role.Team))
            .Replace("{ROLE_NAME}", participant.Role.RoleName)
            .Replace("{ROLE_DESC}", participant.Role.Description)
            .Replace("{PERSONALITY_STATS}", statsSb.ToString());
    }

    private string GetAliveParticipantString(List<Participant> participants)
    {
        return string.Join(", ", participants.Where(p => p.IsAlive).Select(p => p.Name));
    }

    private string GetDeadParticipantString(List<Participant> participants)
    {
        return string.Join(", ", participants.Where(p => !p.IsAlive).Select(p => p.Name));
    }

    // [낮 투표 프롬프트] 지침 ➔ [게임 상황 정보] 순서로 결합
    public string GetVotePrompt(Participant participant, List<Participant> participantList)
    {
        string rawTemplate = repository.GetTemplate("VoteTemplate");

        string teamGoal = participant.Role.Team switch
        {
            Team.Citizen => "누가 마피아일지 골라야 해",
            Team.Mafia => "가장 마피아로 몰려있는 사람을 골라야 해",
            Team.Neutral => "너의 목적을 달성하기 위해 제거해야하는 사람을 골라야 해",
            _ => ""
        };

        string voteInstructions = rawTemplate
            .Replace("{MY_NAME}", participant.Name)
            .Replace("{TEAM_GOAL}", teamGoal);

        string contextPrompt = GetPhaseContextPrompt(participant, participantList);

        // 투표 지침 뒤에 게임 상황 정보를 배치!
        return contextPrompt + "\n\n" + voteInstructions;
    }

    // [밤 능력 프롬프트] 지침 ➔ [게임 상황 정보] 순서로 결합
    public string GetAbilityPrompt(Participant participant, List<Participant> participantList)
    {
        string rawTemplate = repository.GetTemplate("AbilityTemplate");

        string abilityInstructions = rawTemplate
            .Replace("{MY_NAME}", participant.Name)
            .Replace("{ABILITY_DESC}", participant.Role.Description);

        string contextPrompt = GetPhaseContextPrompt(participant, participantList);

        // 능력 지침 뒤에 게임 상황 정보를 배치!
        return contextPrompt + "\n\n" + abilityInstructions;
    }

    // JSON 파싱 메서드들 (동일하게 유지)
    public ChatTarget? GetChatTarget(string answer) => ExtractJson<ChatTarget>(answer);
    public VoteTarget? GetVoteTarget(string answer) => ExtractJson<VoteTarget>(answer);
    public AbilityTarget? GetAbilityTarget(string answer) => ExtractJson<AbilityTarget>(answer);

    private T? ExtractJson<T>(string answer) where T : class
    {
        Match jsonMatch = Regex.Match(answer, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success) return null;
        return JsonSerializer.Deserialize<T>(jsonMatch.Value);
    }
}