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
            var shelves = new GameObject("Shelves").AddComponent<ShelfView>();
            var counters = new GameObject("Counters").AddComponent<CounterView>();
            var customers = new GameObject("Customers").AddComponent<CustomerView>();
            var floating = new GameObject("FloatingText").AddComponent<FloatingText>();
            var placement = new GameObject("PlacementMode").AddComponent<PlacementMode>();
            var ui = new GameObject("UI");
            var hud = ui.AddComponent<Hud>();
            var menu = ui.AddComponent<BottomMenu>();
            var wholesaler = ui.AddComponent<WholesalerPanel>();
            var shelfPanel = ui.AddComponent<ShelfAssignPanel>();
            var modal = ui.AddComponent<ModalQueue>();

            Set(runner, "_database", db);
            Set(runner, "_router", router);
            Set(grid, "_runner", runner);
            Set(grid, "_camera", cam);
            Set(fixtures, "_runner", runner);
            Set(shelves, "_runner", runner);
            Set(counters, "_runner", runner);
            Set(customers, "_runner", runner);
            Set(floating, "_runner", runner);
            Set(floating, "_camera", cam);
            Set(placement, "_runner", runner);
            Set(placement, "_camera", cam);
            Set(placement, "_wholesaler", wholesaler);
            Set(hud, "_runner", runner);
            Set(menu, "_runner", runner);
            Set(menu, "_placement", placement);
            Set(menu, "_wholesaler", wholesaler);
            Set(menu, "_modal", modal);
            Set(wholesaler, "_runner", runner);
            Set(shelfPanel, "_runner", runner);
            Set(shelfPanel, "_camera", cam);
            Set(shelfPanel, "_placement", placement);
            Set(modal, "_runner", runner);
            // 描き直しの順: 床 → 配置物 → 棚の中身 → カウンター → 客 → 文字 → 配置モード → パネル → 月末などのダイアログ
            SetList(router, "_handlers", new List<Object> { grid, fixtures, shelves, counters, customers, floating, placement, wholesaler, shelfPanel, modal });

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
