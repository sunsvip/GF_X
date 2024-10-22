using GameFramework.Event;
using GameFramework;
/// <summary>
/// 玩家数据改变通知事件
/// </summary>
public class PlayerDataChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(PlayerDataChangedEventArgs).GetHashCode();
    public override int Id => EventId;
    public PlayerDataType DataType { get; private set; }
    public int OldValue { get; private set; }
    public int Value { get; private set; }

    public static PlayerDataChangedEventArgs Create(PlayerDataType type, int oldV, int newV)
    {
        var instance = ReferencePool.Acquire<PlayerDataChangedEventArgs>();
        instance.DataType = type;
        instance.OldValue = oldV;
        instance.Value = newV;
        return instance;
    }
    public override void Clear()
    {
        DataType = default;
        Value = 0;
        OldValue = 0;
    }
}
