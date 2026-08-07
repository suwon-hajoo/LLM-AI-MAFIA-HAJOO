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
        TextAsset[] allPrompt = Resources.LoadAll<TextAsset>("Prompts");

        templateCache.Clear();
        foreach (TextAsset prompt in allPrompt)
        {
            string fileName = prompt.name;
            string fileContent = prompt.text;
            templateCache[fileName] = fileContent;
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