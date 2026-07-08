using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class READMEAutoUpdater : AssetPostprocessor
{
    private const string StartMarker = "<!-- AUTO-GENERATED:FILE_INDEX_START -->";
    private const string EndMarker = "<!-- AUTO-GENERATED:FILE_INDEX_END -->";
    private static readonly string[] IgnoredDirectories =
    {
        ".git",
        ".idea",
        "Library",
        "Temp",
        "obj",
        "Build",
        "Logs"
    };

    private static readonly string[] IgnoredExtensions =
    {
        ".meta",
        ".tmp",
        ".csproj",
        ".sln",
        ".slnx"
    };

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (ShouldRefresh(importedAssets) || ShouldRefresh(deletedAssets) || ShouldRefresh(movedAssets) || ShouldRefresh(movedFromAssetPaths))
        {
            EditorApplication.delayCall -= RefreshReadmeIndex;
            EditorApplication.delayCall += RefreshReadmeIndex;
        }
    }

    [MenuItem("Tools/Refresh README Index")]
    public static void RefreshReadmeIndex()
    {
        string projectRoot = GetProjectRoot();
        string readmePath = Path.Combine(projectRoot, "README.md");

        if (!File.Exists(readmePath))
        {
            Debug.LogWarning("README.md not found. Skipping automatic index generation.");
            return;
        }

        List<string> files = EnumerateTrackedFiles(projectRoot);
        string generatedBlock = BuildGeneratedBlock(files);

        string content = File.ReadAllText(readmePath);
        string updatedContent = ReplaceOrAppendBlock(content, generatedBlock);

        File.WriteAllText(readmePath, updatedContent);
        Debug.Log($"README index refreshed with {files.Count} tracked files.");
    }

    private static bool ShouldRefresh(string[] paths)
    {
        if (paths == null || paths.Length == 0)
        {
            return false;
        }

        return paths.Any(path => path.StartsWith("Assets/") || path.StartsWith("Packages/") || path.StartsWith("ProjectSettings/") || path.StartsWith("README") || path.EndsWith(".slnx"));
    }

    private static string ReplaceOrAppendBlock(string content, string generatedBlock)
    {
        if (content.Contains(StartMarker) && content.Contains(EndMarker))
        {
            int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
            int end = content.IndexOf(EndMarker, StringComparison.Ordinal) + EndMarker.Length;
            return content.Substring(0, start) + generatedBlock + content.Substring(end);
        }

        return content.TrimEnd() + Environment.NewLine + Environment.NewLine + generatedBlock;
    }

    private static string BuildGeneratedBlock(List<string> files)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(StartMarker);
        builder.AppendLine();
        builder.AppendLine("## Auto-generated project file index");
        builder.AppendLine();
        builder.AppendLine("This section is updated automatically when files are added, removed, or moved in the project.");
        builder.AppendLine();
        builder.AppendLine($"Total tracked files: {files.Count}");
        builder.AppendLine();

        foreach (string file in files)
        {
            builder.AppendLine($"- [{file}]({file})");
        }

        builder.AppendLine();
        builder.AppendLine(EndMarker);
        return builder.ToString();
    }

    private static List<string> EnumerateTrackedFiles(string projectRoot)
    {
        List<string> files = new List<string>();

        foreach (string root in new[] { "Assets", "Packages", "ProjectSettings" })
        {
            string absoluteRoot = Path.Combine(projectRoot, root);
            if (!Directory.Exists(absoluteRoot))
            {
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldIgnorePath(path))
                {
                    continue;
                }

                string relativePath = ToRelativePath(projectRoot, path);
                files.Add(relativePath.Replace("\\", "/"));
            }
        }

        foreach (string file in Directory.EnumerateFiles(projectRoot))
        {
            if (ShouldIgnorePath(file))
            {
                continue;
            }

            string relativePath = ToRelativePath(projectRoot, file);
            files.Add(relativePath.Replace("\\", "/"));
        }

        return files
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldIgnorePath(string path)
    {
        string normalizedPath = path.Replace("\\", "/");

        if (normalizedPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string extension in IgnoredExtensions)
        {
            if (normalizedPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (string directory in IgnoredDirectories)
        {
            if (normalizedPath.Contains($"/{directory}/", StringComparison.OrdinalIgnoreCase) || normalizedPath.StartsWith($"{directory}/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ToRelativePath(string root, string absolutePath)
    {
        Uri rootUri = new Uri(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        Uri pathUri = new Uri(absolutePath);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
    }

    private static string GetProjectRoot()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(currentDirectory, "Assets")))
        {
            return currentDirectory;
        }

        return Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
    }
}
