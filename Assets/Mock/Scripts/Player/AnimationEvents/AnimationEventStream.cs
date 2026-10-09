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

/// <summary>
/// アニメーションイベントを購読・発行するためのインターフェース。
/// </summary>
public interface IAnimationEventStream : IDisposable
{
    /// <summary>発生したアニメーションイベントを購読するための IObservable。</summary>
    IObservable<AnimationEventType> OnEvent { get; }

    /// <summary>イベントを発行して全購読者へ通知する。</summary>
    void Publish(AnimationEventType eventType);
}

/// <summary>
/// UniRx を用いてアニメーションイベントを多播する実装。
/// </summary>
public sealed class AnimationEventStream : IAnimationEventStream
{
    private readonly Subject<AnimationEventType> _subject = new();

    public IObservable<AnimationEventType> OnEvent => _subject;

    /// <summary>Animation Eventの種類を購読中のPlayer状態管理へ通知する。</summary>
    public void Publish(AnimationEventType eventType)
    {
        _subject.OnNext(eventType);
    }

    /// <summary>通知を完了し、Subjectの購読リソースを解放する。</summary>
    public void Dispose()
    {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}