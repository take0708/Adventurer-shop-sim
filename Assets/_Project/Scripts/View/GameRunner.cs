using System;
using Shop.Core;
using Shop.Data;
using UnityEngine;

namespace Shop.Game
{
    /// <summary>一時停止の理由。理由ごとのフラグで持つ（shousaisekkei.md 11.2）。</summary>
    [Flags]
    public enum PauseReason { None = 0, User = 1, Placement = 2, Expand = 4, Merchant = 8, Modal = 16 }

    /// <summary>実時間を Tick に直して GameSession を進め、イベントを配る（shousaisekkei.md 11.2）。</summary>
    public sealed class GameRunner : MonoBehaviour
    {
        const int MaxTicksPerFrame = 10;

        [SerializeField] GameDatabase _database;
        [SerializeField] EventRouter _router;
        [Tooltip("0 なら時刻から決める。不具合の再現には固定する")]
        [SerializeField] ulong _fixedSeed;

        PauseReason _pauseReasons;
        float _accumulator;

        public GameSession Session { get; private set; }
        public GameDatabase Database => _database;
        public int Speed { get; private set; } = 1;
        public bool IsPaused => _pauseReasons != PauseReason.None;
        public ulong Seed { get; private set; }

        void Start()
        {
            StartNewGame(_fixedSeed != 0 ? _fixedSeed : (ulong)DateTime.Now.Ticks);
        }

        public void StartNewGame(ulong seed)
        {
            Seed = seed;
            Session = new GameSession(ConfigBuilder.Build(_database), seed);
            _pauseReasons = PauseReason.None;
            _accumulator = 0;
            Route();
        }

        public void SetSpeed(int speed) => Speed = Mathf.Clamp(speed, 1, 2);
        public void Pause(PauseReason r) => _pauseReasons |= r;
        public void Resume(PauseReason r) => _pauseReasons &= ~r;
        public bool IsPausedBy(PauseReason r) => (_pauseReasons & r) != 0;

        void Update()
        {
            if (Session == null) return;
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// 実時間 seconds ぶん進めて、イベントを配る。1 回で進めるのは MaxTicksPerFrame まで。
        /// Update から呼ぶ。エディタからフレームを進めずに確かめるときにも使う。
        /// </summary>
        public void Advance(float seconds)
        {
            if (!Session.IsEnded && !IsPaused)
            {
                _accumulator += seconds * Speed;
                int n = 0;
                while (_accumulator >= TimeConfig.SecondsPerTick && n < MaxTicksPerFrame)
                {
                    Session.Tick();
                    _accumulator -= TimeConfig.SecondsPerTick;
                    n++;
                    // 月末で止める。イベントを配るのはループのあとなので、ここで止めないと翌月へ進んでしまう
                    if (Session.MonthEndedThisTick) Pause(PauseReason.Modal);
                    if (IsPaused) break;
                }
                // 超えた分は捨てる（kihonsekkei.md 2.2）
                if (n == MaxTicksPerFrame) _accumulator = 0;
            }
            Route();
        }

        /// <summary>一時停止中もコマンドのイベントを配る。</summary>
        void Route()
        {
            if (_router != null) _router.Dispatch(Session.DrainEvents());
            else Session.DrainEvents();
        }
    }
}
