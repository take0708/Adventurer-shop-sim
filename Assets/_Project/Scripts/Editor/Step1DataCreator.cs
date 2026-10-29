using System.Collections.Generic;
using System.IO;
using Shop.Core;
using Shop.Data;
using Shop.Game;
using UnityEditor;
using UnityEngine;

namespace Shop.Editor
{
    /// <summary>
    /// youken.md 5 章の値で、数値のアセットを一式作る（shousaisekkei.md 11.1）。
    /// 数値の正本は youken.md のまま。アセットが youken と一致するかは ConfigAssetTests で確かめる。
    /// 既にあるアセットは値を上書きする（Sprite などの見た目の参照は残る）。
    /// </summary>
    public static class Step1DataCreator
    {
        const string Root = "Assets/_Project/Data";

        [MenuItem("Shop/Create Step1 Data")]
        public static void Create()
        {
            // youken.md 5.1
            var potion = Item("potion", "回復薬", ItemCategory.Tool, 6, 10, 1);
            var antidote = Item("antidote", "解毒薬", ItemCategory.Tool, 9, 15, 2);
            var staff = Item("wooden_staff", "木の杖", ItemCategory.Weapon, 30, 50, 1);
            var copper = Item("copper_sword", "銅の剣", ItemCategory.Weapon, 60, 100, 1);
            var ironSword = Item("iron_sword", "鉄の剣", ItemCategory.Weapon, 180, 300, 3);
            var cloth = Item("cloth_armor", "布の服", ItemCategory.Armor, 24, 40, 1);
            var shield = Item("leather_shield", "革の盾", ItemCategory.Armor, 45, 75, 2);
            var ironArmor = Item("iron_armor", "鉄の鎧", ItemCategory.Armor, 240, 400, 4);
            var items = new List<ItemData> { potion, antidote, staff, copper, ironSword, cloth, shield, ironArmor };

            // youken.md 5.3
            var warrior = Job("warrior", "戦士", (potion, 2), (copper, 3), (ironSword, 3), (shield, 2), (ironArmor, 2));
            var mage = Job("mage", "魔法使い", (potion, 2), (antidote, 1), (staff, 4), (cloth, 3));
            var priest = Job("priest", "僧侶", (potion, 3), (antidote, 3), (staff, 1), (cloth, 2), (shield, 1));
            var jobs = new List<JobData> { warrior, mage, priest };

            // youken.md 5.7
            var rares = new List<RareItemData>
            {
                Rare("fairy_salve", "妖精の傷薬", 45, 75, 1, null, 3),
                Rare("elixir", "霊薬", 70, 120, 2, priest, 4),
                Rare("silver_robe", "銀糸のローブ", 150, 250, 3, mage, 6),
                Rare("stardust_staff", "星屑の杖", 270, 450, 4, mage, 8),
                Rare("dragon_shield", "竜鱗の盾", 420, 700, 5, warrior, 10),
            };

            // youken.md 5.2, 5.4, 5.7
            var stars = Load<StarTable>($"{Root}/StarTable.asset");
            stars.EditorSet(new List<StarTable.Row>
            {
                new StarTable.Row { requiredRep = 0, baseVisits = 15, budgetMin = 40, budgetMax = 130, merchantChancePercent = 20 },
                new StarTable.Row { requiredRep = 50, baseVisits = 22, budgetMin = 40, budgetMax = 180, merchantChancePercent = 25 },
                new StarTable.Row { requiredRep = 200, baseVisits = 32, budgetMin = 80, budgetMax = 380, merchantChancePercent = 30 },
                new StarTable.Row { requiredRep = 480, baseVisits = 44, budgetMin = 150, budgetMax = 650, merchantChancePercent = 35 },
                new StarTable.Row { requiredRep = 1050, baseVisits = 58, budgetMin = 250, budgetMax = 1000, merchantChancePercent = 40 },
            });
            EditorUtility.SetDirty(stars);

            // youken.md 5.5、kihonsekkei.md 4.2, 6.2
            var counter = Fixture("counter", "カウンター", FixtureKind.Counter, ItemCategory.Tool, 250, 2, 1, false, 0, 0, 0, 0, false, false, new Color(0.55f, 0.35f, 0.2f));
            var fixtures = new List<FixtureData>
            {
                Fixture("shelf_weapon", "武器棚", FixtureKind.Shelf, ItemCategory.Weapon, 100, 1, 1, false, 4, 40, 0, 0, true, true, new Color(0.75f, 0.3f, 0.3f)),
                Fixture("shelf_armor", "防具棚", FixtureKind.Shelf, ItemCategory.Armor, 100, 1, 1, false, 4, 40, 0, 0, true, true, new Color(0.3f, 0.45f, 0.8f)),
                Fixture("shelf_tool", "道具棚", FixtureKind.Shelf, ItemCategory.Tool, 100, 1, 1, false, 10, 15, 0, 0, true, true, new Color(0.35f, 0.7f, 0.35f)),
                counter,
                Fixture("bargain_table", "特売台", FixtureKind.Shelf, ItemCategory.Rare, 200, 1, 1, false, 2, 15, 0, 1, false, false, new Color(0.9f, 0.75f, 0.2f)),
                Fixture("plant", "観葉植物", FixtureKind.Decor, ItemCategory.Tool, 80, 1, 1, false, 0, 0, 1, 0, false, false, new Color(0.2f, 0.5f, 0.25f)),
                Fixture("rug", "絨毯", FixtureKind.Decor, ItemCategory.Tool, 150, 2, 2, true, 0, 0, 2, 0, false, false, new Color(0.65f, 0.25f, 0.45f, 0.8f)),
                Fixture("armor_stand", "甲冑の飾り", FixtureKind.Decor, ItemCategory.Tool, 300, 1, 1, false, 0, 0, 4, 0, false, false, new Color(0.6f, 0.6f, 0.65f)),
            };

            // youken.md 5.6
            var sizes = Load<ShopSizeTable>($"{Root}/ShopSizeTable.asset");
            sizes.EditorSet(new List<ShopSizeTable.Row>
            {
                new ShopSizeTable.Row { width = 6, height = 5, shelfMax = 6, counterMax = 2, cost = 0 },
                new ShopSizeTable.Row { width = 8, height = 6, shelfMax = 10, counterMax = 3, cost = 1500 },
                new ShopSizeTable.Row { width = 10, height = 7, shelfMax = 15, counterMax = 4, cost = 5000 },
            });
            EditorUtility.SetDirty(sizes);

            // 表にならない値は BalanceConfig の初期値（shousaisekkei.md 3.2）
            var balance = Load<BalanceConfig>($"{Root}/BalanceConfig.asset");
            balance.startMoney = 800;
            balance.initialLayout = new List<BalanceConfig.InitialFixture>
            {
                new BalanceConfig.InitialFixture { fixture = counter, origin = new Vector2Int(4, 1) },
            };
            EditorUtility.SetDirty(balance);

            var db = Load<GameDatabase>(GameDatabase.DefaultPath);
            db.EditorSet(items, rares, jobs, stars, fixtures, sizes, balance);
            EditorUtility.SetDirty(db);

            CreateCommentTable();

            AssetDatabase.SaveAssets();
            Debug.Log($"Step 1 のデータを作りました: {GameDatabase.DefaultPath}");
        }

        /// <summary>
        /// 客の吹き出しのセリフ。セリフ集（T-13）が決まるまでの仮。kihonsekkei.md 5.5 の例から。
        /// 既にあるアセットは上書きしない（手で直したセリフを消さないため）。
        /// </summary>
        public static CommentTable CreateCommentTable()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CommentTable>(CommentTable.DefaultPath);
            if (existing != null) return existing;
            var table = Load<CommentTable>(CommentTable.DefaultPath);
            table.EditorSet(new List<CommentTable.Entry>
            {
                new CommentTable.Entry { result = CustomerResult.Full, lines = new List<string> { "いい{item}が買えた！", "また来るよ！" } },
                new CommentTable.Entry { result = CustomerResult.Partial, lines = new List<string> { "まあまあかな", "{item}は買えたけど…" } },
                new CommentTable.Entry { result = CustomerResult.None, lines = new List<string> { "品揃えがイマイチ…", "欲しいものがなかった…" } },
                new CommentTable.Entry { result = CustomerResult.QueueLeft, lines = new List<string> { "待ちくたびれた…" } },
            }, new List<string> { "すごい品を見つけた！", "{item}が手に入るなんて！" });
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return table;
        }

        static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            var dir = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(dir);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string dir)
        {
            if (AssetDatabase.IsValidFolder(dir)) return;
            var parent = Path.GetDirectoryName(dir).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
        }

        static ItemData Item(string id, string name, ItemCategory category, int buy, int sell, int unlockStar)
        {
            var a = Load<ItemData>($"{Root}/Items/{id}.asset");
            a.EditorSet(id, name, category, buy, sell, unlockStar);
            EditorUtility.SetDirty(a);
            return a;
        }

        static RareItemData Rare(string id, string name, int buy, int sell, int appearStar, JobData job, int repBonus)
        {
            var a = Load<RareItemData>($"{Root}/RareItems/{id}.asset");
            a.EditorSet(id, name, buy, sell, appearStar, job, repBonus);
            EditorUtility.SetDirty(a);
            return a;
        }

        static JobData Job(string id, string name, params (ItemData Item, int Weight)[] wants)
        {
            var a = Load<JobData>($"{Root}/Jobs/{id}.asset");
            var list = new List<JobData.Want>();
            foreach (var w in wants) list.Add(new JobData.Want { item = w.Item, weight = w.Weight });
            a.EditorSet(id, name, list);
            EditorUtility.SetDirty(a);
            return a;
        }

        static FixtureData Fixture(string id, string name, FixtureKind kind, ItemCategory shelfCategory, int cost,
            int width, int height, bool walkable, int displayMax, int restockTicks, int ambience,
            int maxCount, bool countsTowardShelfLimit, bool usesFreeShelf, Color color)
        {
            var a = Load<FixtureData>($"{Root}/Fixtures/{id}.asset");
            a.EditorSet(id, name, kind, shelfCategory, cost, width, height, walkable, displayMax, restockTicks, ambience,
                maxCount, countsTowardShelfLimit, usesFreeShelf, color);
            EditorUtility.SetDirty(a);
            return a;
        }
    }
}
