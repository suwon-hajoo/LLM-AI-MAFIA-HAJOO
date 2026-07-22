#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EnvLoader
{
    private static readonly Dictionary<string, string> EnvVariables = new();
    private static bool _isInitialized = false;

    public static void Load()
    {
        if (_isInitialized) return;

        string filePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[EvnLoader] .env 파일을 찾을 수 없습니다! 경로: {filePath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                int separatorIndex = line.IndexOf('=');
                if (separatorIndex == -1) continue;
                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex+1).Trim();
                if (EnvVariables.ContainsKey(key)) continue;
                EnvVariables.Add(key, value);
            }
            _isInitialized = true;
            Debug.Log("[EvnLoader] .env 파일이 성공적으로 로드되었습니다.");

        } catch (Exception ex)
        {
            Debug.LogError($"[EnvLoader] 파일 읽기 중 에러 발생: {ex.Message}");
        }
    }

    public static string? Get(string key)
    {
        if (!_isInitialized) Load();
        return EnvVariables.TryGetValue(key, out string value) ? value : null;
    }
}