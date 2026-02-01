using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MaskGame.Editor
{
    public static class StandaloneBuild
    {
        private const string MenuRoot = "Tools/Mask Game/Build/";
        private const string DefaultOut = "Builds";
        private const string ArgOut = "-outDir";
        private const string ArgId = "-buildId";
        private const string ArgDev = "-dev";

        [MenuItem(MenuRoot + "Standalone/macOS")]
        public static void BuildMac()
        {
            BuildStandalone(BuildTarget.StandaloneOSX);
        }

        [MenuItem(MenuRoot + "Standalone/Linux")]
        public static void BuildLinux()
        {
            BuildStandalone(BuildTarget.StandaloneLinux64);
        }

        private static void BuildStandalone(BuildTarget target)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new BuildFailedException(
                    $"Build target not supported: {target}. Please install the Unity build support module."
                );

            string[] scenes = EditorBuildSettings
                .scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes in Build Settings.");

            string outDir = GetArg(ArgOut) ?? DefaultOut;
            string buildId = GetArg(ArgId) ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            bool dev = HasArg(ArgDev);

            Directory.CreateDirectory(outDir);

            string product = SanitizeName(PlayerSettings.productName);
            string platform = target == BuildTarget.StandaloneOSX ? "mac" : "linux";
            string name = $"{product}_{platform}_{SanitizeName(buildId)}";
            string outPath = target switch
            {
                BuildTarget.StandaloneOSX => Path.Combine(outDir, $"{name}.app"),
                BuildTarget.StandaloneLinux64 => Path.Combine(outDir, name),
                _ => Path.Combine(outDir, name),
            };

            BuildOptions options = BuildOptions.None;
            if (dev)
                options |= BuildOptions.Development;

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = target,
                options = options,
            };

            Debug.Log(
                $"Build start: target={target} dev={dev} out={outPath} scenes={scenes.Length}"
            );
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log(
                $"Build result: {report.summary.result} size={report.summary.totalSize} time={report.summary.totalTime}"
            );

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}");
        }

        private static bool HasArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.Ordinal))
                    continue;

                string value = args[i + 1];
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return value;
            }

            return null;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "MaskGame";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == ' ')
                {
                    sb.Append('_');
                    continue;
                }

                bool isInvalid = false;
                for (int n = 0; n < invalid.Length; n++)
                {
                    if (c == invalid[n])
                    {
                        isInvalid = true;
                        break;
                    }
                }

                sb.Append(isInvalid ? '_' : c);
            }

            return sb.ToString();
        }
    }
}
