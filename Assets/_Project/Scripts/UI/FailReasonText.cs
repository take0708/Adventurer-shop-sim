using Shop.Core;

namespace Shop.Game
{
    /// <summary>FailReason → 表示文（kihonsekkei.md 4.3 の文言、shousaisekkei.md 10.2）。</summary>
    public static class FailReasonText
    {
        public static string Get(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.None: return "";
                case FailReason.InvalidCount: return "個数が正しくありません";
                case FailReason.NotEnoughMoney: return "お金が足りません";
                case FailReason.Locked: return "まだ仕入れられません";
                case FailReason.NotSoldHere: return "ここでは売っていません";
                case FailReason.MerchantGone: return "行商人は帰ってしまいました";
                case FailReason.OutOfBounds:
                case FailReason.BlocksEntrance:
                case FailReason.Occupied: return "ここには置けません";
                case FailReason.NoPayTile: return "カウンターの前（下）を空けてください";
                case FailReason.LimitReached: return "これ以上置けません。拡張しましょう";
                case FailReason.BlocksPath: return "通路がふさがります";
                case FailReason.LastCounter: return "カウンターは 1 台以上必要です";
                case FailReason.NotFound: return "見つかりません";
                case FailReason.WrongCategory: return "この棚には並べられません";
                case FailReason.NotInWarehouse: return "倉庫にありません";
                case FailReason.MaxStage: return "これ以上広げられません";
                case FailReason.GameOver: return "ゲームは終わりました";
                default: return reason.ToString();
            }
        }
    }
}
