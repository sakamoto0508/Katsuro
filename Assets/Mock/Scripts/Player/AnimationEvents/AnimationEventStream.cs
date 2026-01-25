using System;
using UniRx;

/// <summary>
/// アニメーションイベントを UniRx で多播するためのストリーム。
/// </summary>
public enum AnimationEventType
{
    WeaponHitboxEnabled,
    WeaponHitboxDisabled,
    ComboWindowOpened,
    ComboWindowClosed,
    AttackFinished,
    SwordDrawCompleted,
}

public interface IAnimationEventStream : IDisposable
{
    IObservable<AnimationEventType> OnEvent { get; }
    void Publish(AnimationEventType eventType);
}

public sealed class AnimationEventStream : IAnimationEventStream
{
    private readonly Subject<AnimationEventType> _subject = new();

    public IObservable<AnimationEventType> OnEvent => _subject;

    public void Publish(AnimationEventType eventType)
    {
        _subject.OnNext(eventType);
    }

    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}