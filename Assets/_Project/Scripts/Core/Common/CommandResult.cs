namespace Shop.Core
{
    /// <summary>コマンドが失敗した理由（shousaisekkei.md 10.2）。</summary>
    public enum FailReason
    {
        None,
        InvalidCount, NotEnoughMoney, Locked, NotSoldHere,
        MerchantGone,
        OutOfBounds, BlocksEntrance, Occupied, NoPayTile,
        LimitReached, BlocksPath, LastCounter, NotFound,
        WrongCategory, NotInWarehouse,
        MaxStage,
        GameOver,
    }

    /// <summary>プレイヤーの操作の結果。失敗は例外にせずこれで返す（kihonsekkei.md P5）。</summary>
    public readonly struct CommandResult
    {
        public bool Ok => Reason == FailReason.None;
        public FailReason Reason { get; }

        CommandResult(FailReason reason)
        {
            Reason = reason;
        }

        public static CommandResult Success => default;
        public static CommandResult Fail(FailReason reason) => new CommandResult(reason);

        public override string ToString() => Ok ? "Ok" : Reason.ToString();
    }
}
