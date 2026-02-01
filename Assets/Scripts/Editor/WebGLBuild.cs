using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MaskGame.Editor
{
    public static class WebGLBuild
    {
        private const string MenuRoot = "Tools/Mask Game/Build/";
        private const string DefaultOut = "Builds";
        private const string ArgOut = "-outDir";
        private const string ArgId = "-buildId";
        private const string ArgDev = "-dev";

        [MenuItem(MenuRoot + "WebGL")]
        public static void BuildWebGL()
        {
            Build();
        }

        private static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException(
                    "WebGL build target not supported. Please install WebGL Build Support."
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
            string name = $"{product}_webgl_{SanitizeName(buildId)}";
            string outPath = Path.Combine(outDir, name);
            Directory.CreateDirectory(outPath);

            BuildOptions options = BuildOptions.None;
            if (dev)
                options |= BuildOptions.Development;

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = BuildTarget.WebGL,
                options = options,
            };

            Debug.Log(
                $"Build start: target={BuildTarget.WebGL} dev={dev} out={outPath} scenes={scenes.Length}"
            );
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log(
                $"Build result: {report.summary.result} size={report.summary.totalSize} time={report.summary.totalTime}"
            );

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}");

            File.WriteAllText(Path.Combine(outPath, ".nojekyll"), string.Empty);
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
