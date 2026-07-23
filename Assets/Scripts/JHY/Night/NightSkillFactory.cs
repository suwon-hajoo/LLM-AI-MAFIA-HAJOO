public static class NightSkillFactory
{
    public static INightSkill CreateSkill(string roleId)
    {
        return roleId switch
        {
            "Mafia" => new MafiaSkill(),
            "Doctor" => new DoctorSkill(),
            "Police" => new PoliceSkill(),
            _ => new CitizenSkill() // 그 외 시민 등
        };
    }
}