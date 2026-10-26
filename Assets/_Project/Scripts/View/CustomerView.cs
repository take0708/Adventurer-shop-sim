using System.Collections.Generic;
using Shop.Core;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>
    /// 客の表示（shousaisekkei.md 11.3）。CustomerMoved の Ticks を秒にしてマスの間を補間する。
    /// 素材が決まるまでは職業の色の四角と、頭の上の小さな四角（気持ち）。
    /// </summary>
    public sealed class CustomerView : MonoBehaviour, IGameEventHandler
    {
        sealed class View
        {
            public SpriteRenderer Body;
            public SpriteRenderer Emote;
            public Vector3 From, To;
            public float Duration, Elapsed;
            public float EmoteLeft;
        }

        [SerializeField] GameRunner _runner;
        [SerializeField] float _emoteSeconds = 1f;

        readonly Dictionary<int, View> _views = new Dictionary<int, View>();
        readonly Stack<View> _pool = new Stack<View>();

        public void Handle(GameEvent e)
        {
            switch (e)
            {
                case SessionStartedEvent _:
                    foreach (var id in new List<int>(_views.Keys)) Release(id);
                    break;
                case CustomerSpawnedEvent s:
                {
                    var v = Get(s.CustomerId);
                    v.Body.color = JobColor(s.Job);
                    // 入口の下の道から歩いてくる
                    var entrance = TilePosition.ToWorld(s.Tile);
                    MoveTo(v, entrance + Vector3.down * 2f * TilePosition.TileSize, entrance,
                        _runner.Session.Config.Balance.WalkTicksPerTile * 2);
                    break;
                }
                case CustomerMovedEvent m when _views.TryGetValue(m.CustomerId, out var v):
                    MoveTo(v, TilePosition.ToWorld(m.From), TilePosition.ToWorld(m.To), m.Ticks);
                    break;
                case CustomerEmoteEvent em when _views.TryGetValue(em.CustomerId, out var v):
                    ShowEmote(v, EmoteColor(em.Kind));
                    break;
                case CustomerLeftEvent l when _views.TryGetValue(l.CustomerId, out var v):
                    ShowEmote(v, ResultColor(l.Result));
                    break;
                case CustomerExitedEvent x:
                    Release(x.CustomerId);
                    break;
            }
        }

        /// <summary>客の見た目の今の位置（吹き出しなどを出すため）。</summary>
        public bool TryGetPosition(int customerId, out Vector3 position)
        {
            if (_views.TryGetValue(customerId, out var v))
            {
                position = v.Body.transform.position;
                return true;
            }
            position = default;
            return false;
        }

        void Update()
        {
            if (_runner.Session == null) return;
            float dt = _runner.IsPaused ? 0f : Time.unscaledDeltaTime * _runner.Speed;
            foreach (var v in _views.Values)
            {
                v.Elapsed += dt;
                float t = v.Duration <= 0f ? 1f : Mathf.Clamp01(v.Elapsed / v.Duration);
                v.Body.transform.position = Vector3.Lerp(v.From, v.To, t);
                if (v.EmoteLeft > 0f)
                {
                    v.EmoteLeft -= Time.unscaledDeltaTime;
                    if (v.EmoteLeft <= 0f) v.Emote.enabled = false;
                }
            }
        }

        void MoveTo(View v, Vector3 from, Vector3 to, int ticks)
        {
            v.From = from;
            v.To = to;
            v.Duration = ticks * TimeConfig.SecondsPerTick;
            v.Elapsed = 0f;
            v.Body.transform.position = ticks <= 0 ? to : from;
        }

        void ShowEmote(View v, Color color)
        {
            v.Emote.color = color;
            v.Emote.enabled = true;
            v.EmoteLeft = _emoteSeconds;
        }

        View Get(int id)
        {
            var v = _pool.Count > 0 ? _pool.Pop() : Create();
            v.Body.gameObject.name = $"Customer #{id}";
            v.Body.gameObject.SetActive(true);
            v.Emote.enabled = false;
            v.EmoteLeft = 0f;
            _views[id] = v;
            return v;
        }

        View Create()
        {
            var body = PlaceholderSprite.Create(transform, "Customer", Color.white, 5);
            body.transform.localScale = Vector3.one * 0.5f * TilePosition.TileSize;
            var emote = PlaceholderSprite.Create(body.transform, "Emote", Color.white, 6);
            emote.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            emote.transform.localScale = Vector3.one * 0.5f;
            return new View { Body = body, Emote = emote };
        }

        void Release(int id)
        {
            if (!_views.TryGetValue(id, out var v)) return;
            _views.Remove(id);
            v.Body.gameObject.SetActive(false);
            _pool.Push(v);
        }

        static Color JobColor(JobDef job)
        {
            switch (job.Id)
            {
                case "warrior": return new Color(0.85f, 0.35f, 0.25f);
                case "mage": return new Color(0.35f, 0.4f, 0.9f);
                case "priest": return new Color(0.95f, 0.95f, 0.95f);
                default: return Color.gray;
            }
        }

        static Color EmoteColor(EmoteKind kind)
        {
            switch (kind)
            {
                case EmoteKind.Picked: return new Color(0.4f, 0.9f, 0.4f);
                case EmoteKind.Treasure: return new Color(1f, 0.85f, 0.2f);
                case EmoteKind.TooExpensive: return new Color(1f, 0.6f, 0.1f);
                case EmoteKind.OutOfStock: return new Color(0.4f, 0.4f, 0.45f);
                default: return new Color(0.6f, 0.3f, 0.7f);   // NotCarried
            }
        }

        static Color ResultColor(CustomerResult result)
        {
            switch (result)
            {
                case CustomerResult.Full: return new Color(1f, 0.45f, 0.6f);
                case CustomerResult.Partial: return new Color(0.9f, 0.9f, 0.5f);
                case CustomerResult.QueueLeft: return new Color(0.9f, 0.2f, 0.2f);
                default: return new Color(0.3f, 0.3f, 0.6f);
            }
        }
    }
}
