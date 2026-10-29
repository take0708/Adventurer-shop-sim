using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shop.Game
{
    /// <summary>
    /// タイトル（SC-01、kihonsekkei.md 3 章）。スタート、音量 ON/OFF、クレジット。
    /// ゲームの名前は仮（T-10 で決める）。
    /// 仮の UI: IMGUI で描く。日本語フォントを決めたら uGUI ＋ TextMeshPro に置き換える（kousei.md 11 章）。
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        public const string GameScene = "Game";
        const string SoundKey = "Shop.SoundOn";

        [SerializeField] string _title = "（仮）冒険者の店";

        bool _credits;
        GUIStyle _big;

        void Awake()
        {
            AudioListener.volume = SoundOn ? 1f : 0f;
        }

        static bool SoundOn
        {
            get
            {
                try { return PlayerPrefs.GetInt(SoundKey, 1) == 1; }
                catch { return true; }
            }
            set
            {
                try { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
                catch { /* 保存できなくても続ける */ }
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        void OnGUI()
        {
            if (_big == null)
            {
                _big = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _big.normal.textColor = new Color(1f, 0.9f, 0.6f);
            }
            const float w = 360f;
            var rect = new Rect((Screen.width - w) / 2, Screen.height * 0.18f, w, Screen.height * 0.7f);
            GUILayout.BeginArea(rect);
            GUILayout.Label(_title, _big, GUILayout.Height(80));
            GUILayout.Space(30);
            bool start = GUILayout.Button("スタート", GUILayout.Height(48));
            GUILayout.Space(8);
            bool sound = SoundOn;
            if (GUILayout.Toggle(sound, sound ? "音: ON" : "音: OFF", GUI.skin.button, GUILayout.Height(32)) != sound) SoundOn = !sound;
            _credits = GUILayout.Toggle(_credits, "クレジット", GUI.skin.button, GUILayout.Height(32));
            if (_credits)
            {
                GUILayout.Box("企画・制作: （未定）\n素材: （未定）\nMade with Unity", GUILayout.ExpandWidth(true));
            }
            GUILayout.EndArea();

            if (start) SceneManager.LoadScene(GameScene);
        }
    }
}
