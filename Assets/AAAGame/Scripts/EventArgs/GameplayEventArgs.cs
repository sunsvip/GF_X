using GameFramework.Event;
using GameFramework;

public enum GameplayEventType
{
    GameOver
}
public class GameplayEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(GameplayEventArgs).GetHashCode();
    public override int Id => EventId;
    public GameplayEventType EventType { get; private set; }
    public RefParams Params { get; private set; }
    public override void Clear()
    {
        if (Params != null)
            ReferencePool.Release(Params);
    }
    public static GameplayEventArgs Create(GameplayEventType eventType, RefParams eventData = null)
    {
        var instance = ReferencePool.Acquire<GameplayEventArgs>();
        instance.EventType = eventType;
        instance.Params = eventData;
        return instance;
    }
}
