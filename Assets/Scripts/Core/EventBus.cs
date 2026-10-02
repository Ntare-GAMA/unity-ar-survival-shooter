using System;

namespace ARSurvival.Core
{
    /// <summary>Marker for event payloads sent through <see cref="EventBus{T}"/>.</summary>
    public interface IGameEvent { }

    /// <summary>
    /// Observer pattern: a typed, static publish/subscribe channel per event struct.
    /// Publishers and subscribers never reference each other, e.g. the HUD listens for
    /// <see cref="ScoreChangedEvent"/> without knowing the GameManager exists.
    /// Subscribers must unsubscribe in OnDisable/OnDestroy.
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        static event Action<T> Handlers;

        public static void Subscribe(Action<T> handler) => Handlers += handler;
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;
        public static void Raise(T gameEvent) => Handlers?.Invoke(gameEvent);
    }
}
