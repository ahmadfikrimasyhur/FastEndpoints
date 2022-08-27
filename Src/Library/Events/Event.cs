﻿namespace FastEndpoints;

/// <summary>
/// base class for the event bus
/// </summary>
public abstract class EventBase
{
    //key: TEvent 
    //val: unique list of concrete event handler instances (subscribers)
    internal static readonly Dictionary<Type, HashSet<IEventHandler>> handlerDict = new();
}

/// <summary>
/// event notification bus which uses an in-process pub/sub messaging system
/// </summary>
/// <typeparam name="TEvent">the type of notification event dto</typeparam>
public class Event<TEvent> : EventBase where TEvent : notnull
{
    private static readonly List<IEventHandler<TEvent>>? handlers;

    static Event()
    {
        if (handlerDict.TryGetValue(typeof(TEvent), out var hndlrs) && hndlrs.Count > 0)
        {
            handlers = new(hndlrs.Count);
            handlers.AddRange(hndlrs.Cast<IEventHandler<TEvent>>());
        }
    }

    /// <summary>
    /// publish the given model/dto to all the subscribers of the event notification
    /// </summary>
    /// <param name="eventModel">the notification event model/dto to publish</param>
    /// <param name="waitMode">specify whether to wait for none, any or all of the subscribers to complete their work</param>
    ///<param name="cancellation">an optional cancellation token</param>
    /// <returns>a Task that matches the wait mode specified.
    /// <see cref="Mode.WaitForNone"/> returns an already completed Task (fire and forget).
    /// <see cref="Mode.WaitForAny"/> returns a Task that will complete when any of the subscribers complete their work.
    /// <see cref="Mode.WaitForAll"/> return a Task that will complete only when all of the subscribers complete their work.</returns>
    public static Task PublishAsync(TEvent eventModel, Mode waitMode = Mode.WaitForAll, CancellationToken cancellation = default)
    {
        if (handlers?.Count > 0)
        {
            switch (waitMode)
            {
                case Mode.WaitForNone:
                    _ = Task.WhenAll(handlers.Select(h => h.HandleAsync(eventModel, cancellation)));
                    return Task.CompletedTask;

                case Mode.WaitForAny:
                    return Task.WhenAny(handlers.Select(h => h.HandleAsync(eventModel, cancellation)));

                case Mode.WaitForAll:
                    return Task.WhenAll(handlers.Select(h => h.HandleAsync(eventModel, cancellation)));

                default:
                    return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }
}