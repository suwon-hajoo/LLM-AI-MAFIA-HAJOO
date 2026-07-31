#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        return team switch
        {
            Team.Citizen => "마피아를 찾아서 투표를 통해 잡는 것",
            Team.Mafia => "밤에 능력을 사용하여 시민을 죽이거나 투표를 시민에게 유도하여 죽여 살아남는 것",
            Team.Neutral => "특정 조건을 맞추는 것",
            _ => "",
        };
    }

    public string GetGameExplaination()
    {
        StringBuilder sb = new();
        sb.AppendLine("너는 사이버펑크 세계관의 마피아 게임에 참여했어");
        sb.AppendLine("이 게임의 방식은 낮, 투표시간, 밤 이렇게 있는데");
        sb.AppendLine("낮 시간에는 대화만 할 수 있고");
        sb.AppendLine("투표시간에는 가장 의심스러운 사람 한명을 골라 투표할 수 있어");
        sb.AppendLine("밤 시간에는 각자에게 부여된 능력을 사용할 수 있어");
        sb.AppendLine("게임이지만 실제 목숨이 오가는 것이야 따라서 조심해");
        return sb.ToString();
    }

    public string GetTip(RoleData roleData)
    {
        return roleData.RoleId switch
        {
            "Citizen" => "팁으로는 의심받지 말고 마피아를 찾는게 좋아\n",
            "Doctor" => "팁으로는 자신이 의사라는 걸 들키지 말고 마피아가 노릴 사람을 찾아서 능력을 사용하는게 좋아\n",
            "Mafia" => "팁으로는 자신이 마피아라는 것을 들키지 말고 시민 중에서 마피아를 위협하는 능력을 가진 사람을 찾아내서 밤에 죽이고 다른 사람을 마피아로 몰아서 다른 시민들이 그 사람을 투표로 죽이게 하는게 좋지\n",
            "Spy" => "팁으로는 자신이 스파이라는 것을 들키지 말고 마피아를 찾아서 접선해서 마피아가 죽지 않게 도와줘\n",
            "Police" => "팁으로는 최대한 빨리 마피아를 찾아내서 다른 시민들과 공유하면 이길 수 있어\n",
            _ => ""
        };
    }

    public string GetSystemMessage(Participant participant, List<RoleData> gameRoles)
    {
        StringBuilder sb = new();
        sb.Append(GetGameExplaination());
        sb.Append(GetTip(participant.Role));
        sb.AppendLine($"너는 지금 부터 마피아 게임의 {TeamToString(participant.Role.Team)} 진영이야");
        sb.AppendLine($"목적은 {TeamPurpose(participant.Role.Team)}이야");
        sb.AppendLine($"너의 직업은 {participant.Role.RoleName}이고 능력은 {participant.Role.Description}이야.");
        sb.AppendLine("이 마피아 게임에서는 다양한 직업이 있는데");
        foreach (var gameRole in gameRoles)
        {
            sb.AppendLine($"{gameRole.RoleName} : {gameRole.Description}");
        }
        sb.AppendLine("너의 성격은");
        sb.AppendLine(participant.Personality.GetPromptRawStats());
        return sb.ToString();
    }

    public string GetConversationPrompt()
    {
        StringBuilder sb = new();
        sb.AppendLine("성격에 맞게 대화해봐 가능하면 짧게 말하면 좋고");
        sb.AppendLine("target에는 말하고 싶은 대상의 이름을 말하면 되고, 말하고 싶은 대상이 없으면 모두라고 적어줘 content에는 하고 싶은 말을 작성하면 돼");
        sb.AppendLine("다음 조건에 맞게 JSON 데이터를 채워서 출력해줘 { \"target\" : \"string\", \"content\" : \"string\"}");
        return sb.ToString();
    }

    public ChatTarget? GetChatTarget(string answer)
    {
        Match jsonMatch = Regex.Match(answer, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success) return null;
        string jsonText = jsonMatch.Value;
        ChatTarget? target = JsonSerializer.Deserialize<ChatTarget>(jsonText);
        return target;
    }

    private string GetAliveParticipantString(List<Participant> participants)
    {
        //string result = string.Join(", ", participants.Select(p=>p.Name));
        string result = string.Join(", ", participants.Where(p => p.IsAlive).Select(p => p.Name));
        return result;
    }

    public string GetVotePrompt(Participant participant,List<Participant> participantList)
    {
        StringBuilder sb = new();
        sb.AppendLine("투표시간이야");
        sb.AppendLine($"인원으로는 {GetAliveParticipantString(participantList)}가 있어");
        sb.AppendLine($"너의 이름은 {participant.Name} 이야 해당 닉네임은 투표하면 안돼");
        switch (participant.Role.Team)
        {
            case Team.Citizen: sb.AppendLine("누가 마피아일지 골라야 해"); break;
            case Team.Mafia: sb.AppendLine("가장 마피아로 몰려있는 사람을 골라야 해"); break;
            case Team.Neutral: sb.AppendLine("너의 목적을 달성하기 위해 제거해야하는 사람을 골라야 해"); break;
        }
        sb.AppendLine("투표를 기권하고 싶다면 \"is_skip\": true 로 작성하고, 누군가에게 투표하려면 \"is_skip\": false 와 함께 target에 생존자 이름을 정확히 적어줘.");
        sb.AppendLine("다음 조건에 맞게 JSON 데이터를 채워서 출력해줘 { \"is_skip\" : true, \"target\" : \"string\" }");
        sb.AppendLine(" target의 값에는 투표할 사람의 이름을 적어줘");
        return sb.ToString();
    }

    public VoteTarget? GetVoteTarget(string answer)
    {
        Match jsonMatch = Regex.Match(answer, @"\{.*\}", RegexOptions.Singleline);
        if (!jsonMatch.Success) return null;
        string jsonText= jsonMatch.Value;
        VoteTarget? target = JsonSerializer.Deserialize<VoteTarget>(jsonText);
        return target;
    }

    public string GetAbilityPrompt(Participant participant, List<Participant> participantList)
    {
        StringBuilder sb = new();
        sb.AppendLine("너의 능력을 사용할거야");
        sb.AppendLine($"인원으로는 {GetAliveParticipantString(participantList)}가 있어");
        sb.AppendLine($"너의 능력인 {participant.Role.Description}을 어느 사람에게 사용할지 정해줘");
        sb.AppendLine("다음 조건에 맞게 JSON 데이터를 채워서 출력해줘 { \"target\" : \"string\" }");
        sb.AppendLine("target의 값에는 능력을 사용할 사람의 이름을 적어줘");
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