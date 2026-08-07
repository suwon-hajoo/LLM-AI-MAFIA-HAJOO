#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

public class WebBuildScript
{
    public static void Build()
    {
        string profilePath = "Assets/Settings/Build Profiles/Web - Desktop - Release.asset";
        BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
        if (profile == null)
        {
            Debug.LogError("Build Profile Not Found");
            EditorApplication.Exit(1);
            return;
        }
        BuildPlayerWithProfileOptions options = new();
        options.buildProfile = profile;
        options.locationPathName = Path.Combine(Directory.GetCurrentDirectory(), "build/WebGL");
        options.options = BuildOptions.None;
        
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build Succeeded");
        }else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("Build Failed");
            EditorApplication.Exit(1);
        }
    }
}
#endif
