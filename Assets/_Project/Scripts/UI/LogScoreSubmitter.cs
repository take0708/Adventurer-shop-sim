using System;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// スコアをログに出すだけの送信（unityroom の送信を入れるまでの仮。shousaisekkei.md 11.7）。
    /// M8 で UnityroomScoreSubmitter に置き換える。
    /// </summary>
    public sealed class LogScoreSubmitter : MonoBehaviour, IScoreSubmitter
    {
        public void Submit(int score, Action<bool> onDone)
        {
            Debug.Log($"[Score] {score}（仮の送信。ログに出すだけ）");
            onDone?.Invoke(true);
        }
    }
}
