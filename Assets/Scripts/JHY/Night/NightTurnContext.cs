using UnityEngine;

public static class NightTurnContext
{
    public static int MafiaTargetId = -1;   // 마피아 습격 대상 ID
    public static int DoctorTargetId = -1;  // 의사 보호 대상 ID

    // 밤이 끝날 때 초기화
    public static void Reset()
    {
        MafiaTargetId = -1;
        DoctorTargetId = -1;
    }
}