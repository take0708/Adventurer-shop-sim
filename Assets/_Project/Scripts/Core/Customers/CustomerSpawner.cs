using System.Collections.Generic;

namespace Shop.Core
{
    /// <summary>月の来店予定（kihonsekkei.md 5.1、shousaisekkei.md 8.1）。客の中身は来店の時刻に決める。</summary>
    internal sealed class CustomerSpawner
    {
        readonly GameConfig _config;
        readonly IRandom _random;
        readonly List<int> _plan = new List<int>();
        int _next;

        public CustomerSpawner(GameConfig config, IRandom random)
        {
            _config = config;
            _random = random;
        }

        /// <summary>今月の来店数。</summary>
        public int PlannedCount => _plan.Count;
        /// <summary>来店時刻（小さい順）。</summary>
        public IReadOnlyList<int> Plan => _plan;

        /// <summary>来店数 = 基本 ×（1 + 雰囲気 × 1.5%）を整数で四捨五入（K-11）。</summary>
        public static int VisitsFor(GameConfig config, int star, int ambience)
        {
            int baseVisits = config.StarOf(star).BaseVisits;
            int a = ambience < config.Balance.AmbienceMax ? ambience : config.Balance.AmbienceMax;
            return (baseVisits * (1000 + a * config.Balance.AmbiencePermille) + 500) / 1000;
        }

        /// <summary>月の初めに呼ぶ。来店時刻を決める。</summary>
        public void PlanMonth(int star, int ambience)
        {
            _plan.Clear();
            _next = 0;
            int visits = VisitsFor(_config, star, ambience);
            for (int i = 0; i < visits; i++) _plan.Add(_random.Range(0, _config.Balance.SpawnWindowEndTick));
            _plan.Sort();
        }

        /// <summary>予定の時刻になった客の数を返す（同じ時刻に何人いてもよい）。</summary>
        public int Due(int tickInMonth)
        {
            int n = 0;
            while (_next < _plan.Count && _plan[_next] == tickInMonth)
            {
                _next++;
                n++;
            }
            return n;
        }

        /// <summary>残りの予定を消す（テスト用）。</summary>
        public void Clear()
        {
            _plan.Clear();
            _next = 0;
        }
    }
}
