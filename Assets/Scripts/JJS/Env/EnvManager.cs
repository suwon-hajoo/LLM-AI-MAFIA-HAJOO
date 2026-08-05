
using UnityEngine;

public class EnvManager: MonoBehaviour
{
    
    public void ReceiveEnv(string jsonString)
    {
        EnvData envData = JsonUtility.FromJson<EnvData>(jsonString);
        EnvLoader.AddEnvVariable("Open_AI_API_Key", envData.Open_AI_API_Key);
        EnvLoader.AddEnvVariable("Open_AI_API_URL", envData.Open_AI_API_URL);
        EnvLoader.AddEnvVariable("Model_Name", envData.Model_Name);
        EnvLoader.SetInitialized();
    }
}