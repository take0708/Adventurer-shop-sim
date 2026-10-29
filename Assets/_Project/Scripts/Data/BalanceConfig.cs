using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop.Data
{
    /// <summary>表にならない数値と時間の単位（shousaisekkei.md 3.2, 11.1）。</summary>
    [CreateAssetMenu(menuName = "Shop/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Serializable]
        public struct InitialFixture
        {
            public FixtureData fixture;
            public Vector2Int origin;
        }

        [Header("時間")]
        public int ticksPerDay = 20;
        public int daysPerMonth = 30;
        public int monthsPerYear = 12;
        public int totalMonths = 24;

        [Header("お金・評判")]
        public int startMoney = 800;
        public int repFull = 2;
        public int repPartial = 0;
        public int repNone = -2;
        public int repQueueLeft = -1;
        [Tooltip("雰囲気 1 あたりの来店数の増加（千分率）")]
        public int ambiencePermille = 15;
        public int ambienceMax = 20;

        [Header("客")]
        public int twoWantsChancePercent = 40;
        public int checkoutTicks = 24;
        public int queuePatienceTicks = 120;
        public int walkTicksPerTile = 3;
        public int pickTicks = 5;
        public int emoteTicks = 10;
        public int maxCustomerStayTicks = 400;
        public int spawnWindowEndTick = 570;

        [Header("行商人")]
        public int merchantArriveMinTick = 60;
        public int merchantArriveMaxTick = 400;
        public int merchantStayTicks = 150;
        public int merchantMaxBuy = 2;

        [Header("配置")]
        public int refundPercent = 100;
        public int freeShelfCount = 3;
        public Vector2Int entrance = new Vector2Int(2, 0);
        public List<InitialFixture> initialLayout = new List<InitialFixture>();

        [Header("年末審査（講評の境目。年ごと）")]
        [Tooltip("その年に増えた評判がこれ以上なら「よい」")]
        public List<int> reviewGoodRep = new List<int> { 150, 300 };
        [Tooltip("これ以上なら「すばらしい」")]
        public List<int> reviewExcellentRep = new List<int> { 450, 900 };
    }
}
