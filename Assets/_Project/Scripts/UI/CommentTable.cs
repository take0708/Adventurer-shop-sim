using System;
using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 客の吹き出しのセリフ（shousaisekkei.md 11.6、K-08）。結果ごとの一覧から View が選ぶ（Core の乱数は使わない）。
    /// {item} は買った品の名前に置き換える（買った品がなければ、{item} を含まないセリフから選ぶ）。
    /// セリフ集（T-13）が決まるまでは仮の数行。
    /// </summary>
    [CreateAssetMenu(menuName = "Shop/Comment Table")]
    public sealed class CommentTable : ScriptableObject
    {
        public const string DefaultPath = "Assets/_Project/Data/CommentTable.asset";

        [Serializable]
        public struct Entry
        {
            public CustomerResult result;
            [TextArea] public List<string> lines;
        }

        [SerializeField] List<Entry> _entries = new List<Entry>();
        [Tooltip("掘り出し物（限定品）を会計したとき")]
        [SerializeField] List<string> _treasureLines = new List<string>();

        /// <summary>結果と買った品からセリフを 1 つ選ぶ。なければ null。</summary>
        public string Pick(CustomerResult result, IReadOnlyList<ItemDef> bought)
        {
            ItemDef rare = null, any = null;
            foreach (var i in bought)
            {
                if (i.IsRare && rare == null) rare = i;
                if (any == null) any = i;
            }
            if (rare != null && _treasureLines.Count > 0) return Fill(_treasureLines[UnityEngine.Random.Range(0, _treasureLines.Count)], rare);

            foreach (var e in _entries)
            {
                if (e.result != result || e.lines == null || e.lines.Count == 0) continue;
                var candidates = new List<string>();
                foreach (var line in e.lines)
                    if (any != null || !line.Contains("{item}")) candidates.Add(line);
                if (candidates.Count == 0) return null;
                return Fill(candidates[UnityEngine.Random.Range(0, candidates.Count)], any);
            }
            return null;
        }

        static string Fill(string line, ItemDef item) => item == null ? line : line.Replace("{item}", item.Name);

#if UNITY_EDITOR
        public void EditorSet(List<Entry> entries, List<string> treasureLines)
        {
            _entries = entries;
            _treasureLines = treasureLines;
        }
#endif
    }
}
