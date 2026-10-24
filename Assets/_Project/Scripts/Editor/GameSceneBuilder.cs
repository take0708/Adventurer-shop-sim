using System.Collections.Generic;
using Shop.Data;
using Shop.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shop.Editor
{
    /// <summary>
    /// Game シーンを組み立てる（kousei.md 2 章のシーン）。カメラ、GameRunner、EventRouter、View、配置モードを置いてつなぐ。
    /// 作り直すと手で足した物は消えるので、シーンを手で触り始めたら使わない。
    /// </summary>
    public static class GameSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Shop/Build Game Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabase.DefaultPath) == null) Step1DataCreator.Create();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene で読み込み済みのアセットが外れることがあるので、シーンを作ったあとで読む
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabase.DefaultPath);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.55f, 0.36f);
            camGo.transform.position = new Vector3(0, 0, -10);

            var game = new GameObject("Game");
            var runner = game.AddComponent<GameRunner>();
            var router = game.AddComponent<EventRouter>();

            var grid = new GameObject("Grid").AddComponent<GridView>();
            var fixtures = new GameObject("Fixtures").AddComponent<FixtureView>();
            var placement = new GameObject("PlacementMode").AddComponent<PlacementMode>();

            Set(runner, "_database", db);
            Set(runner, "_router", router);
            Set(grid, "_runner", runner);
            Set(grid, "_camera", cam);
            Set(fixtures, "_runner", runner);
            Set(placement, "_runner", runner);
            Set(placement, "_camera", cam);
            SetList(router, "_handlers", new List<Object> { grid, fixtures, placement });

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log($"Game シーンを作りました: {ScenePath}");
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetList(Object target, string field, List<Object> values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
