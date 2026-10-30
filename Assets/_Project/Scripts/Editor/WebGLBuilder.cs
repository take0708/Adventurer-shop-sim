using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Shop.Editor
{
    /// <summary>
    /// unityroom 向けの WebGL ビルド（kousei.md 8 章 M8、NFR-02）。出力は Builds/WebGL（Git に入れない）。
    /// 圧縮は Gzip、Decompression Fallback を有効にする（サーバーの設定によらずブラウザ側で展開できる）。
    /// unityroom の推奨設定は、公開前に unityroom のヘルプで確かめる。
    /// </summary>
    public static class WebGLBuilder
    {
        const string OutputPath = "Builds/WebGL";
        const int Width = 960, Height = 600;   // unityroom の既定の大きさ

        [MenuItem("Shop/Build WebGL (Release)")]
        public static void BuildRelease() => Build(false);

        /// <summary>開発ビルド。デバッグ用のボタン（月末まで進めるなど）が出る。</summary>
        [MenuItem("Shop/Build WebGL (Development)")]
        public static void BuildDevelopment() => Build(true);

        public static void ApplySettings()
        {
            // 会社名・製品名はゲームの名前（T-10）が決まってから変える（保存先の場所が変わるため）
            PlayerSettings.defaultWebScreenWidth = Width;
            PlayerSettings.defaultWebScreenHeight = Height;
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
        }

        public static BuildSummary Build(bool development)
        {
            ApplySettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[WebGLBuild] {s.result}（{(development ? "開発" : "リリース")}）: {s.outputPath}, {s.totalSize / 1024 / 1024}MB, {s.totalTime.TotalSeconds:F0} 秒, エラー {s.totalErrors}, シーン {string.Join(", ", scenes)}");
            return s;
        }
    }
}
