using UnityEngine;

/// <summary>Enemyの移動・攻撃パラメータと追撃専用被弾Layerを制御する。AI判断と位置移動は担当しない。</summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    /// <summary>Enemy Animatorで使う移動・攻撃・死亡パラメータ名の設定。</summary>
    public AnimationName AnimName => _animName;
    public bool IsReacting { get; private set; }
    public bool IsHeavyReacting { get; private set; }
    /// <summary>Enemy Animatorで使う移動・攻撃・死亡パラメータ名の設定。</summary>
    [UnityEngine.Tooltip("Enemy Animatorで使う移動・攻撃・死亡パラメータ名の設定。")]
    [SerializeField] private AnimationName _animName;
    private Animator _animator;
    private int _moveVelocityHash;
    private int _moveVectorXHash;
    private int _moveVectorYHash;
    /// <summary>Just Avoid追撃の大きなHitReactionを再生するAnimator Layer名。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃の大きなHitReactionを再生するAnimator Layer名。")]
    [Header("Hit Reaction")]
    [SerializeField] private string _reactionLayerName = "HitReaction";
    /// <summary>Light用の大Reaction時間設定（秒）。通常Lightでは大Reactionを要求しない仕様。</summary>
    [UnityEngine.Tooltip("Light用の大Reaction時間設定（秒）。通常Lightでは大Reactionを要求しない仕様。")]
    [SerializeField, Range(.15f, .3f)] private float _lightHitDuration = .24f;
    /// <summary>Just Avoid追撃の大Reactionを維持する時間（秒）。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃の大Reactionを維持する時間（秒）。")]
    [SerializeField, Range(.3f, .5f)] private float _heavyHitDuration = .4f;
    /// <summary>Light用Reaction Layer Weight。通常Lightでは大Reactionを要求しない仕様。</summary>
    [UnityEngine.Tooltip("Light用Reaction Layer Weight。通常Lightでは大Reactionを要求しない仕様。")]
    [SerializeField, Range(0f, 1f)] private float _lightHitWeight = .6f;
    /// <summary>大Reactionへ入る短いBlend時間（秒）。Poseの急変を抑える。</summary>
    [UnityEngine.Tooltip("大Reactionへ入る短いBlend時間（秒）。Poseの急変を抑える。")]
    [SerializeField, Range(.05f, .12f)] private float _reactionBlendIn = .08f;
    /// <summary>大Reactionから通常状態へ戻すBlend時間（秒）。</summary>
    [UnityEngine.Tooltip("大Reactionから通常状態へ戻すBlend時間（秒）。")]
    [SerializeField, Range(.05f, .12f)] private float _reactionBlendOut = .08f;
    private static readonly string[] HitDirections = { "Front", "Back", "Left", "Right" };
    private int _reactionLayer = -1;
    private bool _reactionConfigured;
    private float _reactionElapsed, _reactionDuration, _reactionWeight, _reactionStartWeight;
    private bool _initialized;
    [SerializeField] private JustAvoidCounterConfig _counterPresentation;
    private float _activeReactionBlendIn;
    private CounterHitPose _counterPose;
    private float _visibleReactionDuration;
    /// <summary>A/C比較用。通常Heavyだけ上半身ポーズを重ねる。弱攻撃と追撃には使用しない。</summary>
    [Header("通常Heavy HitReaction / A-C比較")]
    [SerializeField, Tooltip("C案の上半身被弾ポーズを使用。オフで従来のProceduralのみ。")]
    private bool _enableNormalHeavyPose;
    /// <summary>通常Heavyの上半身Override Layerの最大重み。</summary>
    [SerializeField, Range(0f, 1f), Tooltip("通常Heavyの上半身ポーズの最大重み。")]
    private float _normalHitWeight = .8f;
    /// <summary>命中した瞬間に与えるピーク重みの割合。HitStop中にも衝撃姿勢を見せる。</summary>
    [SerializeField, Range(.5f, 1f), Tooltip("命中時のポーズ重みの割合。HitStop中はこの姿勢を保持し、解除後にピークへ接続します。")]
    private float _normalHitImpactWeight = .9f;
    /// <summary>命中時の衝撃姿勢からピークへ移行する時間。</summary>
    [SerializeField, Range(.02f, .08f), Tooltip("被弾ポーズがピークに達する時間（秒）。")]
    private float _normalHitBlendIn = .035f;
    /// <summary>上半身の衝撃姿勢を保持する時間。HitStop時間は含まない。</summary>
    [SerializeField, Range(0f, .15f), Tooltip("被弾ポーズの短い保持時間（秒）。")]
    private float _normalHitHold = .065f;
    /// <summary>Base Layerへ戻す時間。</summary>
    [SerializeField, Range(.08f, .25f), Tooltip("通常アニメーションへ戻す時間（秒）。")]
    private float _normalHitBlendOut = .14f;
    /// <summary>C案のポーズに加算する既存Procedural補正の倍率。</summary>
    [SerializeField, Range(0f, 1f), Tooltip("C案使用時の方向補正倍率。ポーズとの過剰な重なりを抑えます。")]
    private float _normalHitProceduralScale = .35f;
    private int _normalHitLayer = -1;
    private float _normalHitElapsed, _normalHitTarget, _normalHitStart;
    private bool _normalHitActive;
#if UNITY_EDITOR
    /// <summary>通常Heavyの命中要求と描画後4フレームを記録する検証スイッチ。製品ビルドには含めない。</summary>
    [SerializeField, Tooltip("検証用。通常Heavyの条件・成否・理由・HitStop・描画後4フレームのStateと重みをConsoleへ記録します。")]
    private bool _diagnoseNormalHeavyPose;
    private Coroutine _normalHitDiagnostic;
    private string _normalHitResult;
#endif
    /// <summary>通常Heavy用の視覚ポーズLayer重み。AIの行動不能状態とは別。</summary>
    public float NormalHitPoseWeight => _animator != null && _normalHitLayer >= 0 ? _animator.GetLayerWeight(_normalHitLayer) : 0f;
    /// <summary>C案で加算するProcedural反応の倍率。</summary>
    public float NormalHitProceduralScale => Mathf.Clamp01(_normalHitProceduralScale);

    /// <summary>Animatorと被弾Layer・必須パラメータを取得して、専用反応が使用可能か一度だけ確認する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _animator = GetComponent<Animator>();
        _counterPose = new CounterHitPose(transform, _animator);
        _normalHitLayer = _animator.GetLayerIndex("NormalHeavyHit");
        CancelNormalHitPose();
        _reactionLayer = _animator.GetLayerIndex(_reactionLayerName);
        bool trigger = false, direction = false, type = false, speed = false, counter = false;
        foreach (var p in _animator.parameters)
        {
            trigger |= p.name == "HitReaction" && p.type == AnimatorControllerParameterType.Trigger;
            direction |= p.name == "HitDirection" && p.type == AnimatorControllerParameterType.Int;
            type |= p.name == "HitType" && p.type == AnimatorControllerParameterType.Int;
            speed |= p.name == "HitPlaybackSpeed" && p.type == AnimatorControllerParameterType.Float;
            counter |= p.name == "IsJustAvoidCounter" && p.type == AnimatorControllerParameterType.Bool;
        }
        _reactionConfigured = _reactionLayer >= 0 && trigger && direction && type && speed && counter;
        if (_reactionConfigured)
        {
            _animator.SetLayerWeight(_reactionLayer, 0f);
            _animator.SetBool("IsJustAvoidCounter", false);
        }
        // エディターで検証処理が呼ばれていなくても、実行時にパラメーターのハッシュ値を初期化する。
        _moveVelocityHash = Animator.StringToHash(_animName.MoveVelocity);
        _moveVectorXHash = Animator.StringToHash(_animName.MoveVectorX);
        _moveVectorYHash = Animator.StringToHash(_animName.MoveVectorY);
    }

    // Direction identifies the side the attacker occupies in the enemy's local space.
    /// <summary>攻撃者位置または命中情報をEnemyのローカル方向へ変換し、四方向の被弾Clipを選ぶ。</summary>
    /// <returns>前・後・左・右を表す方向インデックス。</returns>
    public static int GetHitDirection(Transform enemy, DamageInfo info)
    {
        Vector3 source = info.Instigator != null ? info.Instigator.transform.position - enemy.position : Vector3.zero;
        source.y = 0f;
        if (source.sqrMagnitude < .0001f) source = -info.HitNormal;
        source.y = 0f;
        if (source.sqrMagnitude < .0001f) source = info.HitPoint - enemy.position;
        source.y = 0f;
        Vector3 local = enemy.InverseTransformDirection(source);
        if (Mathf.Abs(local.x) > Mathf.Abs(local.z)) return local.x > 0f ? 3 : 2;
        return local.z >= 0f ? 0 : 1;
    }

    /// <summary>有効な専用Layerと追撃フラグがある場合だけ大きい被弾反応を開始する。</summary>
    /// <returns>追撃専用の被弾Stateを開始した場合はtrue。</returns>
    public bool TryPlayHitReaction(DamageInfo info)
    {
        if (!info.IsJustAvoidCounter || !_reactionConfigured || !_animator.isActiveAndEnabled) return false;
        CancelNormalHitPose();
        int direction = GetHitDirection(transform, info);
        string state = _reactionLayerName + (info.IsCounterFirst ? ".Light" : ".Heavy") + HitDirections[direction];
        if (!_animator.HasState(_reactionLayer, Animator.StringToHash(state))) return false;
        IsReacting = true;
        IsHeavyReacting = true;
        _reactionElapsed = 0f;
        _reactionDuration = _heavyHitDuration;
        _reactionWeight = 1f;
        _activeReactionBlendIn = _reactionBlendIn;
        if (_counterPresentation != null)
        {
            _reactionWeight = info.IsCounterFirst ? _counterPresentation.FirstReactionWeight : 1f;
            if (!info.IsCounterFirst) _activeReactionBlendIn = _counterPresentation.SecondReactionBlendIn;
        }
        // Blend from the visible layer weight, including consecutive counter hits.
        _reactionStartWeight = _animator.GetLayerWeight(_reactionLayer);
        if (_counterPresentation != null && info.IsCounterFirst)
            _reactionStartWeight = Mathf.Min(_reactionStartWeight, _reactionWeight);
        if (_counterPresentation != null && !info.IsCounterFirst)
            _reactionStartWeight = Mathf.Max(_reactionStartWeight, _counterPresentation.SecondReactionImpactWeight);
        _animator.SetLayerWeight(_reactionLayer, _reactionStartWeight);
        _animator.SetInteger("HitDirection", direction);
        _animator.SetInteger("HitType", 1);
        _animator.SetBool("IsJustAvoidCounter", true);
        _animator.SetFloat("HitPlaybackSpeed", 1f / _reactionDuration);
        _counterPose?.Clear();
        // A zero-time trigger evaluation leaves the old transition pose during HitStop.
        // Evaluate the actual impact sample directly on the dedicated layer before freezing.
        _animator.ResetTrigger("HitReaction");
        var style = _counterPresentation != null ? _counterPresentation.GetStrike(info.CounterStrike) : null;
        _visibleReactionDuration = style != null ? Mathf.Min(_reactionDuration, style.PoseDuration) : _reactionDuration;
        _animator.Play(Animator.StringToHash(state), _reactionLayer, style != null ? style.ImpactPoseTime : .05f);
        if (info.IsCounterFirst) { _reactionStartWeight = _reactionWeight; _animator.SetLayerWeight(_reactionLayer, _reactionWeight); }
        _animator.Update(0f);
        if (style != null) _counterPose?.Begin(info, style, _visibleReactionDuration);
        return true;
    }

    /// <summary>攻撃用トリガーと攻撃再生を解除して被弾への短いBlendを要求する。死亡トリガーは保持する。</summary>
    public void InterruptAttackForHitReaction()
    {
        if (_animator == null) return;
        // Even when reaction clips are unavailable, cancel the outgoing attack.
        foreach (var parameter in _animator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name != "HitReaction" && parameter.name != _animName.EnemyDead)
                _animator.ResetTrigger(parameter.nameHash);
        int idle = Animator.StringToHash("Base Layer.Idle");
        if (_animator.HasState(0, idle)) _animator.CrossFadeInFixedTime(idle, _reactionBlendIn, 0, 0f);
    }

    /// <summary>Animator速度に合わせて被弾Layerと減速移動の進行を更新し、復帰完了を通知する。</summary>
    /// <returns>被弾反応がこの更新で完了した場合はtrue。</returns>
    public bool TickHitReaction(float deltaTime)
    {
        if (!IsReacting) return false;
        _reactionElapsed += deltaTime * Mathf.Max(0f, _animator.speed);
        float blendIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_reactionElapsed / Mathf.Max(.01f, _activeReactionBlendIn)));
        float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_reactionElapsed - _visibleReactionDuration) / _reactionBlendOut));
        _animator.SetLayerWeight(_reactionLayer, Mathf.Lerp(_reactionStartWeight, _reactionWeight, blendIn) * (1f - fade));
        // Keep the original gameplay hold even when the first strike's visual pose ends earlier.
        if (_reactionElapsed < _reactionDuration + _reactionBlendOut) return false;
        CancelHitReaction();
        return true;
    }

    /// <summary>被弾状態・専用パラメータ・Layer重みを解除し、死亡や中断に反応を持ち越さない。</summary>
    public void CancelHitReaction()
    {
        _counterPose?.Clear();
        CancelNormalHitPose();
        IsReacting = IsHeavyReacting = false;
        if (!_reactionConfigured) return;
        _animator.ResetTrigger("HitReaction");
        _animator.SetBool("IsJustAvoidCounter", false);
        _animator.SetLayerWeight(_reactionLayer, 0f);
        _animator.Play(Animator.StringToHash(_reactionLayerName + ".Empty"), _reactionLayer, 0f);
    }

    /// <summary>通常Heavyの上半身ポーズだけを開始する。Base Layer・攻撃・移動・AIは操作しない。</summary>
    public bool TryPlayNormalHeavyPose(float duringAttackScale)
    {
        int state = Animator.StringToHash("NormalHeavyHit.Pose");
        if (!_enableNormalHeavyPose) return RejectNormalHeavyPose("C案無効");
        if (IsReacting) return RejectNormalHeavyPose("追撃専用Reaction中");
        if (_animator == null || !_animator.isActiveAndEnabled) return RejectNormalHeavyPose("Animator未初期化または無効");
        if (_normalHitLayer < 0) return RejectNormalHeavyPose("NormalHeavyHit Layerなし");
        if (!_animator.HasState(_normalHitLayer, state)) return RejectNormalHeavyPose("NormalHeavyHit.Pose Stateなし");
        _normalHitTarget = Mathf.Clamp01(_normalHitWeight) * Mathf.Clamp01(duringAttackScale);
        float impact = _normalHitTarget * Mathf.Clamp01(_normalHitImpactWeight);
        // 再被弾も衝撃姿勢まで戻すが、前回の角度を加算して累積させない。
        _normalHitStart = _normalHitActive ? Mathf.Max(NormalHitPoseWeight, impact) : impact;
        _normalHitElapsed = 0f;
        _normalHitActive = true;
        _animator.Play(state, _normalHitLayer, 0f);
        _animator.SetLayerWeight(_normalHitLayer, _normalHitStart);
        RecordNormalHeavyPoseResult("成功");
        return true;
    }

    /// <summary>被弾ポーズを開始できない理由を検証用に記録し、従来のProcedural処理へ戻す。</summary>
    private bool RejectNormalHeavyPose(string reason)
    {
#if UNITY_EDITOR
        if (_diagnoseNormalHeavyPose) RecordNormalHeavyPoseResult("失敗: " + reason);
#endif
        return false;
    }

    /// <summary>Editorの診断が有効なときだけ、通常Heavyポーズ開始の成否と理由を保持する。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void RecordNormalHeavyPoseResult(string result)
    {
#if UNITY_EDITOR
        if (_diagnoseNormalHeavyPose) _normalHitResult = result;
#endif
    }

    /// <summary>通常Heavy命中の条件を記録し、Layer開始前に追撃や骨反応無効の理由を区別する。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DiagnoseNormalHitRequest(DamageInfo info, bool useBoneReaction)
    {
#if UNITY_EDITOR
        if (!_diagnoseNormalHeavyPose || !info.IsHeavy) return;
        _normalHitResult = info.IsJustAvoidCounter ? "対象外: JustAvoid追撃" :
            !useBoneReaction ? "対象外: useBoneReaction=false" : "未要求";
        Debug.Log($"[NormalHeavyHit] request IsHeavy={info.IsHeavy} counter={info.IsJustAvoidCounter} useBoneReaction={useBoneReaction} enabled={_enableNormalHeavyPose} attack={GetComponent<EnemyController>()?.IsAttackAnimationActive}", this);
#endif
    }

    /// <summary>HitStop適用後の成否と状態、および自然なAnimator評価後の4フレームをEditorだけで記録する。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DiagnoseNormalHitAfterHitStop(DamageInfo info)
    {
#if UNITY_EDITOR
        if (!_diagnoseNormalHeavyPose || !info.IsHeavy) return;
        LogNormalHeavyPoseSnapshot("HitStop要求後");
        if (_normalHitDiagnostic != null) StopCoroutine(_normalHitDiagnostic);
        _normalHitDiagnostic = StartCoroutine(SampleNormalHeavyPoseDiagnostic());
#endif
    }

#if UNITY_EDITOR
    /// <summary>通常の描画完了後にStateとLayer重みを取得する。Animatorの強制更新は行わない。</summary>
    private System.Collections.IEnumerator SampleNormalHeavyPoseDiagnostic()
    {
        for (int frame = 1; frame <= 4; frame++)
        {
            yield return new WaitForEndOfFrame();
            LogNormalHeavyPoseSnapshot("描画後" + frame);
        }
        _normalHitDiagnostic = null;
    }

    /// <summary>ポーズ開始の結果、State、重み、経過時間、Animator速度と攻撃状態をまとめて記録する。</summary>
    private void LogNormalHeavyPoseSnapshot(string phase)
    {
        if (_animator == null) return;
        bool pose = _normalHitLayer >= 0 && _animator.GetCurrentAnimatorStateInfo(_normalHitLayer).IsName("NormalHeavyHit.Pose");
        Debug.Log($"[NormalHeavyHit] {phase} result={_normalHitResult} layer={_normalHitLayer} state={(pose ? "NormalHeavyHit.Pose" : "未評価/別State")} weight={NormalHitPoseWeight:F3} target={_normalHitTarget:F3} elapsed={_normalHitElapsed:F3} speed={_animator.speed:F3} hitStop={(_animator.speed == 0f)} attack={GetComponent<EnemyController>()?.IsAttackAnimationActive}", this);
    }
#endif

    /// <summary>Animator評価前に通常被弾Layerの重みを更新する。HitStop中は同じ時計・重みを保持する。</summary>
    private void Update()
    {
        _counterPose?.RemoveOffsets();
        if (!_normalHitActive) return;
        if (!_enableNormalHeavyPose || IsReacting) { CancelNormalHitPose(); return; }
        _normalHitElapsed += Time.deltaTime * Mathf.Max(0f, _animator.speed);
        float fadeStart = _normalHitBlendIn + _normalHitHold;
        float weight = Mathf.Lerp(_normalHitStart, _normalHitTarget,
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_normalHitElapsed / Mathf.Max(.001f, _normalHitBlendIn))));
        weight *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_normalHitElapsed - fadeStart) / Mathf.Max(.001f, _normalHitBlendOut)));
        _animator.SetLayerWeight(_normalHitLayer, weight);
        if (_normalHitElapsed >= fadeStart + _normalHitBlendOut) CancelNormalHitPose();
    }

    /// <summary>通常の視覚ポーズだけを解除する。死亡・追撃・終了時にLayer重みを残さない。</summary>
    public void CancelNormalHitPose()
    {
        _normalHitActive = false;
        _normalHitElapsed = 0f;
        if (_animator != null && _normalHitLayer >= 0) _animator.SetLayerWeight(_normalHitLayer, 0f);
    }

    /// <summary>非表示・破棄へ通常被弾ポーズを持ち越さない。</summary>
    private void OnDisable()
    {
        _counterPose?.Clear();
        CancelNormalHitPose();
#if UNITY_EDITOR
        if (_normalHitDiagnostic != null) StopCoroutine(_normalHitDiagnostic);
        _normalHitDiagnostic = null;
#endif
    }

    /// <summary>HitStop中は命中姿勢を保持し、解除後は既存Reaction時計で胸・肩・頭・腰のFollow Throughを重ねる。</summary>
    private void LateUpdate()
    {
        if (IsReacting) _counterPose?.Apply(_reactionElapsed);
    }

    /// <summary>
    /// 速度をスムージング付きで Animator に反映する。
    /// </summary>
    public void MoveVelocity(float speed)
    {
        _animator?.SetFloat(_moveVelocityHash, speed, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// 移動ベクトル（X/Z）をスムージング付きで設定する。
    /// </summary>
    public void MoveVector(Vector2 input)
    {
        _animator?.SetFloat(_moveVectorXHash, input.x, 0.1f, Time.deltaTime);
        _animator?.SetFloat(_moveVectorYHash, input.y, 0.1f, Time.deltaTime);
    }

    /// <summary>
    /// 指定トリガーを発火する。未設定名は無視する。
    /// </summary>
    public bool HasTrigger(string trigger)
    {
        if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(trigger)) return false;
        foreach (var parameter in _animator.parameters)
            if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger) return true;
        return false;
    }

    public void PlayTrigger(string animationName)
    {
        _animator?.SetTrigger(animationName);
    }

    /// <summary>
    /// 指定 Bool パラメーターを更新する。
    /// </summary>
    public void PlayBool(string animationName, bool value)
    {
        _animator?.SetBool(animationName, value);
    }

    /// <summary>
    /// 整数パラメーター（例: ComboStep）を設定する。空文字は無視。
    /// </summary>
    public void SetInteger(string parameterName, int value)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        _animator?.SetInteger(parameterName, value);
    }

    /// <summary>
    /// インスペクター更新時にハッシュ値を再計算しておく。
    /// </summary>
    private void OnValidate()
    {
        _moveVelocityHash = Animator.StringToHash(_animName.MoveVelocity);
        _moveVectorXHash = Animator.StringToHash(_animName.MoveVectorX);
        _moveVectorYHash = Animator.StringToHash(_animName.MoveVectorY);
    }

    /// <summary>
    /// アニメーションイベント受け口（Animator がアタッチされた GameObject 上でイベントを呼ぶ場合用）。
    /// EnemyController の同名メソッドへ転送し、ログを出力します。
    /// </summary>
    public void AnimEvent_OnAttackFinished()
    {
        var enemy = GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.AnimEvent_OnAttackFinished();
        }
        else
        {
            Debug.LogWarning("EnemyAnimationController: EnemyController not found in parents to forward AnimEvent_OnAttackFinished");
        }
    }
}
