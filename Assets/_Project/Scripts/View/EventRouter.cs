using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>イベントを受け取る View / UI。</summary>
    public interface IGameEventHandler
    {
        void Handle(GameEvent e);
    }

    /// <summary>受け取ったイベントを順に各 View / UI へ渡す（shousaisekkei.md 11.3）。並べた順に配る。</summary>
    public sealed class EventRouter : MonoBehaviour
    {
        [Tooltip("IGameEventHandler を実装したコンポーネント。上から順に配る")]
        [SerializeField] List<MonoBehaviour> _handlers = new List<MonoBehaviour>();

        readonly List<IGameEventHandler> _resolved = new List<IGameEventHandler>();

        void Awake()
        {
            foreach (var h in _handlers)
            {
                if (h is IGameEventHandler handler) _resolved.Add(handler);
                else if (h != null) Debug.LogWarning($"{h.name} の {h.GetType().Name} は IGameEventHandler ではない", h);
            }
        }

        public void Dispatch(IReadOnlyList<GameEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
                foreach (var h in _resolved)
                    h.Handle(events[i]);
        }
    }
}
