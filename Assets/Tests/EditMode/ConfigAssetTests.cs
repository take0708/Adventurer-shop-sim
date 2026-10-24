using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shop.Core;
using Shop.Data;
using UnityEditor;

namespace Shop.Tests.EditMode
{
    /// <summary>ScriptableObject のアセットが youken.md 5 章（TestConfig）と一致するか（UT-CFG-03、K-09）。</summary>
    public class ConfigAssetTests
    {
        GameConfig _fromAsset;
        GameConfig _expected;

        [OneTimeSetUp]
        public void Load()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabase.DefaultPath);
            Assert.That(db, Is.Not.Null, $"{GameDatabase.DefaultPath} がない（メニュー Shop/Create Step1 Data で作る）");
            _fromAsset = ConfigBuilder.Build(db);
            _expected = TestConfig.Step1();
        }

        [Test]
        public void UT_CFG_03_Time()
        {
            Assert.That(_fromAsset.Time.TicksPerDay, Is.EqualTo(_expected.Time.TicksPerDay));
            Assert.That(_fromAsset.Time.DaysPerMonth, Is.EqualTo(_expected.Time.DaysPerMonth));
            Assert.That(_fromAsset.Time.MonthsPerYear, Is.EqualTo(_expected.Time.MonthsPerYear));
            Assert.That(_fromAsset.Time.TotalMonths, Is.EqualTo(_expected.Time.TotalMonths));
        }

        [Test]
        public void UT_CFG_03_Balance()
        {
            AssertSameProperties(_fromAsset.Balance, _expected.Balance, nameof(BalanceDef.InitialLayout));
            Assert.That(Describe(_fromAsset.Balance.InitialLayout), Is.EqualTo(Describe(_expected.Balance.InitialLayout)));
        }

        [Test]
        public void UT_CFG_03_Items()
        {
            Assert.That(_fromAsset.Items.Select(i => i.Id), Is.EqualTo(_expected.Items.Select(i => i.Id)));
            for (int i = 0; i < _expected.Items.Count; i++)
            {
                var a = _fromAsset.Items[i];
                var e = _expected.Items[i];
                AssertSameProperties(a, e, nameof(ItemDef.RareJob));
                Assert.That(a.RareJob?.Id, Is.EqualTo(e.RareJob?.Id), a.Id);
            }
        }

        [Test]
        public void UT_CFG_03_Jobs()
        {
            Assert.That(_fromAsset.Jobs.Select(j => j.Id), Is.EqualTo(_expected.Jobs.Select(j => j.Id)));
            for (int i = 0; i < _expected.Jobs.Count; i++)
            {
                Assert.That(_fromAsset.Jobs[i].Name, Is.EqualTo(_expected.Jobs[i].Name));
                Assert.That(_fromAsset.Jobs[i].Wants.Select(w => (w.Item.Id, w.Weight)),
                    Is.EqualTo(_expected.Jobs[i].Wants.Select(w => (w.Item.Id, w.Weight))), _expected.Jobs[i].Id);
            }
        }

        [Test]
        public void UT_CFG_03_Stars()
        {
            Assert.That(_fromAsset.Stars.Count, Is.EqualTo(_expected.Stars.Count));
            for (int i = 0; i < _expected.Stars.Count; i++) AssertSameProperties(_fromAsset.Stars[i], _expected.Stars[i]);
        }

        [Test]
        public void UT_CFG_03_Fixtures()
        {
            Assert.That(_fromAsset.Fixtures.Select(f => f.Id), Is.EqualTo(_expected.Fixtures.Select(f => f.Id)));
            for (int i = 0; i < _expected.Fixtures.Count; i++) AssertSameProperties(_fromAsset.Fixtures[i], _expected.Fixtures[i]);
        }

        [Test]
        public void UT_CFG_03_ShopSizes()
        {
            Assert.That(_fromAsset.ShopSizes.Count, Is.EqualTo(_expected.ShopSizes.Count));
            for (int i = 0; i < _expected.ShopSizes.Count; i++) AssertSameProperties(_fromAsset.ShopSizes[i], _expected.ShopSizes[i]);
        }

        static IEnumerable<string> Describe(IReadOnlyList<(FixtureDef Def, Tile Origin)> layout) =>
            layout.Select(x => $"{x.Def.Id} {x.Origin}");

        /// <summary>値の型（int / bool / string / enum / Tile）の公開プロパティをすべて比べる。</summary>
        static void AssertSameProperties<T>(T actual, T expected, params string[] skip)
        {
            foreach (var p in typeof(T).GetProperties())
            {
                if (skip.Contains(p.Name)) continue;
                var type = p.PropertyType;
                if (!(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Tile))) continue;
                Assert.That(p.GetValue(actual), Is.EqualTo(p.GetValue(expected)), $"{typeof(T).Name}.{p.Name}");
            }
        }
    }
}
