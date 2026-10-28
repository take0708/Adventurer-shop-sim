using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Shop.Editor
{
    /// <summary>
    /// Play の開始・終了と、スクリプトのコンパイル・アセンブリの読み直しをコンソールと Editor.log に残す。
    /// MCP から Play して確かめるときに、Play が途中で終わった理由を追うため（kousei.md 9 章）。
    /// 行の頭は [PlayModeLog]。
    /// </summary>
    [InitializeOnLoad]
    static class PlayModeLogger
    {
        static PlayModeLogger()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.pauseStateChanged += s => Log($"一時停止: {s}");
            CompilationPipeline.compilationStarted += _ => Log("コンパイル開始");
            CompilationPipeline.compilationFinished += _ => Log($"コンパイル終了（失敗 {EditorUtility.scriptCompilationFailed}）");
            AssemblyReloadEvents.beforeAssemblyReload += () => Log("アセンブリの読み直し（前）");
            AssemblyReloadEvents.afterAssemblyReload += () => Log("アセンブリの読み直し（後）");
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode: Log("Play を始める"); break;
                case PlayModeStateChange.EnteredPlayMode: Log("Play に入った"); break;
                case PlayModeStateChange.ExitingPlayMode: Log($"Play を終える（コンパイル中 {EditorApplication.isCompiling}）"); break;
                case PlayModeStateChange.EnteredEditMode: Log("Play が終わった"); break;
            }
        }

        static void Log(string message)
        {
            Debug.Log($"[PlayModeLog] {DateTime.Now:HH:mm:ss.fff} {message}（フォーカス {UnityEditorInternal.InternalEditorUtility.isApplicationActive}、フレーム {Time.frameCount}）");
        }
    }
}
