using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PromptRepository
{
    private readonly Dictionary<string, string> templateCache = new();

    public PromptRepository()
    {
        LoadAllTemplates();
    }

    public void LoadAllTemplates()
    {
        string basePath = Path.Combine(Application.streamingAssetsPath, "Prompts");

        if (!Directory.Exists(basePath))
        {
            Debug.LogError($"[PromptRepository] 경로가 존재하지 않습니다: {basePath}");
            return;
        }

        templateCache.Clear();
        foreach (string filePath in Directory.GetFiles(basePath, "*.txt"))
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string content = File.ReadAllText(filePath);
            templateCache[fileName] = content;
        }
    }

    public string GetTemplate(string templateName)
    {
        if (templateCache.TryGetValue(templateName, out string content))
        {
            return content;
        }
        Debug.LogWarning($"[PromptRepository] '{templateName}' 템플릿을 찾을 수 없습니다.");
        return string.Empty;
    }
}