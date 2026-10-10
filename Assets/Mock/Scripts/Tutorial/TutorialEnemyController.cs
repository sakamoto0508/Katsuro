using UnityEngine;

/// <summary>修練中だけ既存Enemyの同じ予備動作を反復する。判定時間・Animation Event・通常AIは変更しない。</summary>
public sealed class TutorialEnemyController : MonoBehaviour
{
    /// <summary>修練専用Sceneの共有Enemy。</summary>
    [SerializeField, Tooltip("修練のEnemyController。")] private EnemyController _enemy;
    /// <summary>間合いを確認するPlayer。</summary>
    [SerializeField, Tooltip("既存Playerの位置。")] private Transform _player;
    /// <summary>予備動作が見える既存攻撃。Animation Eventと威力は本編と共通。</summary>
    [SerializeField, Tooltip("反復する既存EnemyAttackData。")] private EnemyAttackData _attack;
    /// <summary>次の攻撃までの余裕を秒で指定する。</summary>
    [SerializeField, Min(1f), Tooltip("攻撃終了後の休止秒数。")] private float _interval = 3f;
    /// <summary>攻撃を開始する最大水平距離。</summary>
    [SerializeField, Min(.5f), Tooltip("相手が近づいた時だけ攻撃する距離。")] private float _practiceRange = 2.1f;
    /// <summary>攻撃前に密着を解く最小水平距離。既存攻撃の実命中を確認した範囲で指定する。</summary>
    [SerializeField, Min(.5f), Tooltip("近すぎる時だけ休止中に後退する距離。Hitboxは変更しない。")] private float _minimumSpacing = 1.45f;
    /// <summary>予備動作前の小さな後退速度。</summary>
    [SerializeField, Min(.1f), Tooltip("休止中の間合い調整速度（m/秒）。")] private float _spacingSpeed = .8f;
    private bool _practice;
    private float _next, _started;
    private TutorialUI _ui;
    private string _cue;
    /// <summary>表示先を接続してAnimation Eventの終了を購読する。</summary>
    public void Init(TutorialUI ui) { _ui = ui; _enemy.TrainingAttackFinished += OnFinished; }
    /// <summary>回避課題の間だけ反復を許可し、攻撃の持ち越しを止める。</summary>
    public void SetPractice(bool active)
    {
        if (_practice == active) return;
        _practice = active; _next = Time.time + _interval; _enemy.CancelTrainingAttack(); SetCue("");
    }
    /// <summary>実際の終了Eventから次の反復時刻を決める。</summary>
    private void OnFinished() { _next = Time.time + _interval; SetCue("一呼吸おいて、同じ攻撃を繰り返す"); }
    /// <summary>近い時だけ既存攻撃を要求する。失敗後も再試行でき、Clip終了通知の欠落には安全に復帰する。</summary>
    private void Update()
    {
        if (!_practice || _enemy == null || !_enemy.IsTraining || GameManager.Instance == null || !GameManager.Instance.IsCombatActive) return;
        if (_enemy.TrainingAttackInProgress)
        {
            if (Time.time - _started > 7f) { _enemy.CancelTrainingAttack(); OnFinished(); }
            return;
        }
        var delta = _player.position - _enemy.transform.position; delta.y = 0;
        if (AdjustSpacing(delta)) { SetCue("一歩引いて構える。相手の予備動作を見よう"); return; }
        if (delta.sqrMagnitude > _practiceRange * _practiceRange) { SetCue("相手の正面へ近づこう。間合いに入ると攻撃する"); return; }
        if (Time.time < _next) return;
        if (_enemy.BeginTrainingAttack(_attack)) { _started = Time.time; SetCue("構えを見よう。刃が届く直前に回避"); }
    }
    /// <summary>同じ補足文の不要なTMP再生成を避ける。</summary>
    private void SetCue(string cue) { if (_cue == cue) return; _cue = cue; _ui?.SetCue(cue); }
    /// <summary>攻撃・被弾中を避け、NavMesh上で少しずつ後退する。到達できる位置へ瞬間移動はしない。</summary>
    private bool AdjustSpacing(Vector3 delta)
    {
        if (delta.sqrMagnitude >= _minimumSpacing * _minimumSpacing) return false;
        var animation = _enemy.GetComponent<EnemyAnimationController>();
        var agent = _enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (_enemy.IsAttackAnimationActive || (animation != null && animation.IsReacting) || agent == null || !agent.enabled || !agent.isOnNavMesh) return true;
        Vector3 away = delta.sqrMagnitude > .0001f ? -delta.normalized : -_enemy.transform.forward;
        float distance = Mathf.Min(_spacingSpeed * Time.deltaTime, _minimumSpacing - delta.magnitude);
        agent.Move(away * distance);
        return true;
    }
    /// <summary>Scene破棄時に終了Eventの購読を解放する。</summary>
    private void OnDestroy() { if (_enemy != null) _enemy.TrainingAttackFinished -= OnFinished; }
}
