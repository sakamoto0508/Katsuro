using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵の攻撃を制御するクラス。EnemyWeapon のヒットリレーを購読し、ヒット時にダメージを適用します。
/// </summary>
public class EnemyAttacker : IDisposable
{
    /// <summary>Enemyの攻撃設定、武器、Animator、基礎威力と攻撃者を保持する。</summary>
    public EnemyAttacker(EnemyAnimationController aniController, EnemyAttackData[] attackData
        , EnemyWeapon[] weapons, EnemyStuts status, Transform owner)
    {
        _animController = aniController;
        _attackData = attackData;
        _weapons = weapons;
        _status = status;
        _ownerTransform = owner;

        // 各武器のリレーを購読してヒット通知を受ける
        if (_weapons != null)
        {
            foreach (var w in _weapons)
            {
                if (w == null) continue;
                Action<Collider> handler = (other) => HandleWeaponHit(other, w);
                _handlerMap[w] = handler;
                w.RegisterHitObserver(handler);
            }
        }
    }

    private EnemyAttackData[] _attackData;
    private EnemyAnimationController _animController;
    private EnemyWeapon[] _weapons;
    private readonly HashSet<IDamageable> _hitTargets = new();
    private bool _isHitboxActive;
    private bool _currentIsHeavy;
    private EnemyWeapon _currentWeapon;
    private float _currentDamage;
    private ComboSoundLayer[] _currentHitSounds;
    private HitStopManager _hitStop;
    private bool _initialized;
    private readonly Transform _ownerTransform;
    private readonly EnemyStuts _status;
    private readonly Dictionary<EnemyWeapon, Action<Collider>> _handlerMap = new();

    /// <summary>命中時に使用するHitStopManagerを一度だけ接続する。</summary>
    public void Init(HitStopManager hitStop)
    {
        if (_initialized) return;
        _initialized = true;
        _hitStop = hitStop;
    }

    /// <summary>攻撃を実行する。攻撃データに基づき Animator トリガーを発火し、武器にダメージ値を設定します。</summary>
    public bool PerformAttack(EnemyActionType attackType)
    {
        var data = FindData(attackType);
        if (data == null)
        {
            Debug.LogWarning($"EnemyAttacker: no attack data found for action={attackType}");
            return false;
        }

        return PerformAttack(data);
    }

    /// <summary>旧呼び出し互換用。heavy引数にかかわらずデータ定義の強弱を使用する。</summary>
    public bool PerformAttack(EnemyAttackData data, bool heavy) => PerformAttack(data);

    /// <summary>選択済みデータを検証して実行する。強弱はデータを唯一の正とする。</summary>
    public bool PerformAttack(EnemyAttackData data)
    {
        DisableWeaponHitbox();
        _currentWeapon = null;
        _currentHitSounds = null;
        if (!CanPerformAttack(data))
        {
            Debug.LogWarning($"EnemyAttacker: invalid attack data, Animator trigger or weapon: {data?.name}");
            return false;
        }
        _currentIsHeavy = data.IsHeavy;
        _currentWeapon = _weapons[data.HitboxIndex];
        _currentDamage = data.Damage;
        _currentWeapon.CurrentAttackDamage = _currentDamage;
        _animController.PlayTrigger(data.AnimatorTrigger);
        return true;
    }

    /// <summary>開始前に攻撃データ・Trigger・武器の参照を検証する。</summary>
    public bool CanPerformAttack(EnemyAttackData data) => data != null && _animController != null &&
        _animController.HasTrigger(data.AnimatorTrigger) && _weapons != null && data.HitboxIndex >= 0 &&
        data.HitboxIndex < _weapons.Length && _weapons[data.HitboxIndex] != null && _weapons[data.HitboxIndex].HasValidHitbox;

    /// <summary>指定行動に対応する攻撃設定を候補から選ぶ。</summary>
    /// <returns>対応する攻撃設定。候補がなければnull。</returns>
    private EnemyAttackData FindData(EnemyActionType action)
    {
        if (_attackData == null) return null;
        EnemyAttackData selected = null;
        int count = 0;
        foreach (var d in _attackData)
        {
            if (d == null || d.ActionType != action) continue;
            if (UnityEngine.Random.Range(0, ++count) == 0) selected = d;
        }
        // 同じ行動種別の攻撃データが複数ある場合は、ランダムに1つ選ぶ。
        return selected;
    }

    /// <summary>攻撃フレームに合わせてヒットボックスを有効化し、ヒット済み管理を初期化。</summary>
    public void EnableWeaponHitbox()
    {
        if (_currentWeapon == null || !_currentWeapon.HasValidHitbox) return;
        _hitTargets.Clear();
        _isHitboxActive = true;
        // OFF時にWeaponが威力を消去するため、各Event区間の開始で再設定する。
        _currentWeapon.CurrentAttackDamage = _currentDamage;
        _currentWeapon.SetTrailStyle(_currentIsHeavy ? SwordTrail.AttackStyle.Heavy : SwordTrail.AttackStyle.Light);
        _currentWeapon.EnableHitbox();
    }

    /// <summary>ヒットボックスを無効化し、新規ヒットを発生させない。</summary>
    public void DisableWeaponHitbox()
    {
        _isHitboxActive = false;
        if (_weapons == null) return;
        foreach (var w in _weapons)
        {
            w?.DisableHitbox();
        }
    }

    /// <summary>以降の命中で被弾側へ渡す段別の命中音を設定する。nullなら被弾側の通常命中音を使う。</summary>
    public void SetHitSounds(ComboSoundLayer[] sounds) => _currentHitSounds = EnemyComboStageSound.HasAny(sounds) ? sounds : null;

    public void SetSweptHits(bool active)
    {
        if (_weapons == null) return;
        foreach (var weapon in _weapons) weapon?.SetSweptHits(active);
    }
    public void FlushSweptHits()
    {
        if (_isHitboxActive) _currentWeapon?.FlushSweptHits();
    }

    /// <summary>武器コライダーにヒットした相手へ一度だけダメージを適用する（Weaponごとのハンドラで呼ばれる）。</summary>
    private void HandleWeaponHit(Collider other, EnemyWeapon sourceWeapon)
    {
        if (!_isHitboxActive || other == null) return;

        // 自分自身(敵)へのヒットは無視
        if (_ownerTransform != null && other.transform.IsChildOf(_ownerTransform)) return;


        var damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null) return;
        if (!_hitTargets.Add(damageable)) return;

        Vector3 origin = _ownerTransform != null ? _ownerTransform.position : other.bounds.center;
        Vector3 hitPoint = sourceWeapon != null ? sourceWeapon.GetContactPoint(other) : other.ClosestPoint(origin);
        Vector3 hitNormal = (hitPoint - origin).sqrMagnitude > 0.0001f ? (hitPoint - origin).normalized : Vector3.forward;

        float damage = sourceWeapon != null ? sourceWeapon.Damage() : (_status != null ? _status.EnemyPower : 0f);

        var owner = _ownerTransform != null ? _ownerTransform.GetComponent<EnemyController>() : null;
        damage *= owner != null && owner.IsTraining ? 1f : RunSession.EnemyDamage(owner != null ? owner.HpRatio : 1f);
        DamageInfo damageInfo = new DamageInfo(damage, hitPoint, hitNormal, _ownerTransform != null ? _ownerTransform.gameObject : null, other, _currentIsHeavy,
            sourceWeapon != null ? sourceWeapon.GetSlashDirection(other) : Vector3.zero, hitSounds: _currentHitSounds);
        // Debug: 出力（誰がどれだけのダメージを誰に与えたか）
        CombatLog.Trace($"EnemyAttacker: Hit target={other.gameObject.name} damage={damage} instigator={_ownerTransform?.gameObject.name} hitPoint={hitPoint}");
        damageable.ApplyDamage(damageInfo);
    }

    /// <summary>全武器の命中判定を停止し、自身が登録した命中通知ハンドラを解除する。</summary>
    public void Dispose()
    {
        DisableWeaponHitbox();
        // 登録したハンドラを解除
        if (_weapons != null)
        {
            foreach (var w in _weapons)
            {
                if (w == null) continue;
                if (_handlerMap.TryGetValue(w, out var h))
                {
                    w.UnregisterHitObserver(h);
                }
            }
        }
        _handlerMap.Clear();
        _hitTargets.Clear();
    }
}
