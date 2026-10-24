using System;
using NUnit.Framework;
using Shop.Core;

namespace Shop.Tests.EditMode
{
    public class ConfigTests
    {
        [Test]
        public void UT_CFG_01_Step1ConfigPassesValidation()
        {
            var config = TestConfig.Step1();
            Assert.That(ConfigValidator.Check(config), Is.Empty);
            Assert.That(config.MaxStar, Is.EqualTo(5));
            Assert.That(config.MaxStage, Is.EqualTo(3));
            Assert.That(config.Items.Count, Is.EqualTo(13));
        }

        static void AssertInvalid(Action<TestConfig.Spec> modify, string expectedRule)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => TestConfig.Step1(modify));
            StringAssert.Contains(expectedRule, ex.Message);
        }

        [Test]
        public void UT_CFG_02_DuplicateId()
        {
            AssertInvalid(s => s.Items.Add(new ItemDef("potion", "二つ目", ItemCategory.Rare, 1, 2, 1)), "V1");
        }

        [Test]
        public void UT_CFG_02_RequiredRepDecreases()
        {
            AssertInvalid(s => s.Stars[2] = new StarDef(3, 40, 32, 80, 380, 30), "V2");
        }

        [Test]
        public void UT_CFG_02_BudgetMinAboveMax()
        {
            AssertInvalid(s => s.Stars[0] = new StarDef(1, 0, 15, 200, 130, 20), "V2");
        }

        [Test]
        public void UT_CFG_02_EntranceOutside()
        {
            AssertInvalid(s => s.Entrance = new Tile(7, 0), "V3");
        }

        [Test]
        public void UT_CFG_02_SizeDoesNotGrow()
        {
            AssertInvalid(s => s.Sizes[1] = new ShopSizeDef(2, 8, 5, 10, 3, 1500), "V3");
        }

        [Test]
        public void UT_CFG_02_JobWithoutStar1Wants()
        {
            AssertInvalid(s => s.Jobs.Add(new JobDef("knight", "騎士", new[] { (s.Item("iron_sword"), 1) })), "V4");
        }

        [Test]
        public void UT_CFG_02_MissingShelfCategory()
        {
            AssertInvalid(s => s.Fixtures.Remove(s.Fixture("shelf_tool")), "V5");
        }

        [Test]
        public void UT_CFG_02_StarWithoutRareItem()
        {
            AssertInvalid(s => s.Items.RemoveAll(i => i.Id == "elixir"), "V6");
        }

        [Test]
        public void UT_CFG_02_InitialLayoutNotPlaceable()
        {
            AssertInvalid(s => s.InitialLayout[0] = (s.Fixture("counter"), new Tile(4, 0)), "V7");
        }

        [Test]
        public void UT_CFG_02_MerchantCrossesMonth()
        {
            AssertInvalid(s => s.MerchantArriveMaxTick = 500, "V9");
        }

        [Test]
        public void UT_CFG_02_MessageListsEveryProblem()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => TestConfig.Step1(s =>
            {
                s.Stars[0] = new StarDef(1, 0, 15, 200, 130, 20);
                s.MerchantArriveMaxTick = 500;
            }));
            StringAssert.Contains("V2", ex.Message);
            StringAssert.Contains("V9", ex.Message);
        }
    }
}
