using System.Collections.Generic;
using Shop.Data;
using Shop.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shop.Editor
{
    /// <summary>
    /// Game シーンと Title シーンを組み立てる（kousei.md 2 章のシーン）。カメラ、GameRunner、EventRouter、View、配置モードを置いてつなぐ。
    /// 作り直すと手で足した物は消えるので、シーンを手で触り始めたら使わない。
    /// </summary>
    public static class GameSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";

        [MenuItem("Shop/Build Game Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabase.DefaultPath) == null) Step1DataCreator.Create();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene で読み込み済みのアセットが外れることがあるので、シーンを作ったあとで読む
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabase.DefaultPath);
            var comments = Step1DataCreator.CreateCommentTable();

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
            var merchantView = new GameObject("Merchant").AddComponent<MerchantView>();
            var floating = new GameObject("FloatingText").AddComponent<FloatingText>();
            var bubbles = new GameObject("CommentBubbles").AddComponent<CommentBubbles>();
            var placement = new GameObject("PlacementMode").AddComponent<PlacementMode>();
            var ui = new GameObject("UI");
            var hud = ui.AddComponent<Hud>();
            var menu = ui.AddComponent<BottomMenu>();
            var wholesaler = ui.AddComponent<WholesalerPanel>();
            var shelfPanel = ui.AddComponent<ShelfAssignPanel>();
            var expand = ui.AddComponent<ExpandPanel>();
            var merchantPanel = ui.AddComponent<MerchantPanel>();
            var modal = ui.AddComponent<ModalQueue>();
            var result = ui.AddComponent<ResultDialog>();
            var submitter = ui.AddComponent<LogScoreSubmitter>();

            Set(runner, "_database", db);
            Set(runner, "_router", router);
            Set(grid, "_runner", runner);
            Set(grid, "_camera", cam);
            Set(fixtures, "_runner", runner);
            Set(shelves, "_runner", runner);
            Set(counters, "_runner", runner);
            Set(customers, "_runner", runner);
            Set(merchantView, "_runner", runner);
            Set(merchantView, "_camera", cam);
            Set(merchantView, "_panel", merchantPanel);
            Set(merchantView, "_placement", placement);
            Set(floating, "_runner", runner);
            Set(floating, "_camera", cam);
            Set(bubbles, "_table", comments);
            Set(bubbles, "_customers", customers);
            Set(bubbles, "_camera", cam);
            Set(placement, "_runner", runner);
            Set(placement, "_camera", cam);
            Set(placement, "_wholesaler", wholesaler);
            Set(hud, "_runner", runner);
            Set(menu, "_runner", runner);
            Set(menu, "_placement", placement);
            Set(menu, "_wholesaler", wholesaler);
            Set(menu, "_expand", expand);
            Set(menu, "_merchant", merchantPanel);
            Set(menu, "_modal", modal);
            Set(expand, "_runner", runner);
            Set(merchantPanel, "_runner", runner);
            Set(wholesaler, "_runner", runner);
            Set(shelfPanel, "_runner", runner);
            Set(shelfPanel, "_camera", cam);
            Set(shelfPanel, "_placement", placement);
            Set(modal, "_runner", runner);
            Set(modal, "_result", result);
            Set(result, "_runner", runner);
            Set(result, "_submitter", submitter);
            // 描き直しの順: 床 → 配置物 → 棚の中身 → カウンター → 客 → 行商人 → 文字 → 配置モード → パネル → 月末などのダイアログ
            SetList(router, "_handlers", new List<Object> { grid, fixtures, shelves, counters, customers, merchantView, floating, bubbles, placement, wholesaler, shelfPanel, expand, merchantPanel, modal });

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath, 1);
            Debug.Log($"Game シーンを作りました: {ScenePath}");
        }

        /// <summary>Title シーン（SC-01）。カメラとタイトル画面だけ。ビルドの最初のシーンにする。</summary>
        [MenuItem("Shop/Build Title Scene")]
        public static void BuildTitle()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.3f, 0.22f);
            camGo.transform.position = new Vector3(0, 0, -10);
            new GameObject("Title").AddComponent<TitleScreen>();

            EditorSceneManager.SaveScene(scene, TitleScenePath);
            AddToBuildSettings(TitleScenePath, 0);
            Debug.Log($"Title シーンを作りました: {TitleScenePath}");
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

        /// <summary>ビルドのシーン一覧の index 番目に置く（Title が 0、Game が 1）。</summary>
        static void AddToBuildSettings(string path, int index)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(Mathf.Min(index, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
