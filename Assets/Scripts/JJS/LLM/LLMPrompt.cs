#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

public class LLMPrompt
{

    private string TeamToString(Team team)
    {
        switch (team)
        {
            case Team.Citizen: return "시민";
            case Team.Mafia: return "마피아";
            case Team.Neutral: return "중립";
        }
        return "";
    }

    private string TeamPurpose(Team team)
    {
        switch (team)
        {
            case Team.Citizen: return "마피아를 찾아서 잡는 것";
            case Team.Mafia: return "시민을 죽여 살아남는 것";
            case Team.Neutral: return "특정 조건을 맞추는 것";
        }
        return "";
    }

    public string GetSystemMessage(Participant participant, List<RoleData> gameRoles)
    {
        StringBuilder sb = new();
        sb.Append($"너는 지금 부터 마피아 게임의 {TeamToString(participant.Role.Team)} 진영이야\n");
        sb.Append($"목적은 {TeamPurpose(participant.Role.Team)}이야\n");
        sb.Append($"너의 직업은 {participant.Role.RoleName}이고 능력은 {participant.Role.Description}이야.\n");
        sb.Append("이 마피아 게임에서는 다양한 직업이 있는데");
        foreach (var gameRole in gameRoles)
        {
            sb.Append($"{gameRole.RoleName} : {gameRole.Description}");
        }
        sb.Append("너의 성격은");
        sb.Append(participant.Personality.GetPromptRawStats());
        return sb.ToString();
    }

    public string GetConversationPrompt()
    {
        return "성격에 맞게 대화해봐";
    }

    private string GetAliveParticipantString(List<Participant> participants)
    {
        string result = string.Join(", ", participants.Select(p=>p.Name));
        return result;
    }

    public string GetVotePrompt(Participant participant,List<Participant> participantList)
    {
        StringBuilder sb = new();
        sb.Append("투표시간이야\n");
        sb.Append($"인원으로는 {GetAliveParticipantString(participantList)}가 있어\n");
        switch (participant.Role.Team)
        {
            case Team.Citizen: sb.Append("누가 마피아일지 골라야 해"); break;
            case Team.Mafia: sb.Append("가장 마피아로 몰려있는 사람을 골라야 해"); break;
            case Team.Neutral: sb.Append("너의 목적을 달성하기 위해 제거해야하는 사람을 골라야 해"); break;
        }
        sb.Append("다음 조건에 맞게 JSON 데이터를 채워서 출력해줘 { \"target\" : \"string\" }\n");
        sb.Append(" target의 값에는 투표할 사람의 이름을 적어줘");
        return sb.ToString();
    }

    public VoteTarget? GetVoteTarget(string answer)
    {
        Match jsonMatch = Regex.Match(answer, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success)
        {
            return null;
        }
        string jsonText= jsonMatch.Value;
        VoteTarget? target = JsonSerializer.Deserialize<VoteTarget>(jsonText);
        return target;
    }

    public string GetAbilityPrompt(Participant participant, List<Participant> participantList)
    {
        StringBuilder sb = new();
        sb.Append("너의 능력을 사용할거야\n");
        sb.Append($"너의 능력인 {participant.Role.Description}을 어느 사람에게 사용할지 정해줘");
        sb.Append("다음 조건에 맞게 JSON 데이터를 채워서 출력해줘 { \"target\" : \"string\" }\n");
        sb.Append("target의 값에는 능력을 사용할 사람의 이름을 적어줘\n");
        return sb.ToString();
    }

    public AbilityTarget? GetAbilityTarget(string answer)
    {
        Match jsonMatch = Regex.Match(answer, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success) return null;
        string jsonText= jsonMatch.Value;
        AbilityTarget? target = JsonSerializer.Deserialize<AbilityTarget>(jsonText);
        return target;
    }
}