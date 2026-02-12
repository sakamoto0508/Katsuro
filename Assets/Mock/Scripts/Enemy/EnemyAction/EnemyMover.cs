using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵の移動制御を担うユーティリティクラス。
/// </summary>
public class EnemyMover
{
    /// <summary>
    /// コンストラクタ。必要な参照を受け取り内部状態を初期化します。
    /// </summary>
    /// <param name="enemyStuts">敵の設定データ（ScriptableObject）</param>
    /// <param name="enemyPosition">この敵の Transform（所有者）</param>
    /// <param name="playerPosition">追跡対象プレイヤーの Transform</param>
    /// <param name="animationController">Enemy 用のアニメーションラッパー</param>
    /// <param name="rb">Rigidbody（存在する場合）</param>
    /// <param name="agent">NavMeshAgent（経路探索用）</param>
    /// <param name="animator">Animator（root motion 用など）</param>
    /// <param name="animName">アニメーションパラメータ名集</param>
    public EnemyMover(EnemyStuts enemyStuts, Transform enemyPosition, Transform playerPosition
        , EnemyAnimationController animationController, Rigidbody rb, NavMeshAgent agent
        , Animator animator, AnimationName animName)
    {
        _enemyStuts = enemyStuts;
        _enemyTransform = enemyPosition;
        _playerPosition = playerPosition;
        _animationController = animationController;
        _rb = rb;
        _agent = agent;
        _animator = animator;
        _animName = animName;
        // 初期化時に RotationMode に応じた設定を反映しておく
        ApplyRotationMode();
    }

    private EnemyStuts _enemyStuts;
    private Transform _playerPosition;
    private Transform _enemyTransform;
    private EnemyAnimationController _animationController;
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimationName _animName;
    private float _destinationUpdateTimer = 0f;
    private bool _usingRootMotionStepBack;
    private bool _isStepBack;
    private bool _isPatrolWalking;
    private bool _manualRotationDisabledByAgent = false;
    float _speed = 0f;

    /// <summary>
    /// 毎フレーム呼び出す更新処理。
    /// - パトロール歩行中は目的地更新を抑止しつつ移動アニメを更新する
    /// - 通常はプレイヤー方向へ NavMeshAgent に目的地をセットして追跡を行う
    /// - Smooth モードでは手動回転を適用する
    /// </summary>
    public void Update()
    {
        if (_playerPosition == null) return;

        if (_isStepBack)
        {
            // root motion ステップバック中は常に停止扱い
            StopMove();
            return;
        }

        if (_isPatrolWalking)
        {
            UpdatePatrolWalking();
            return;
        }

        // 通常追跡／追跡待機時の更新
        UpdateAnimatorValues();
        UpdateTrackingAndDestination();
    }

    // パトロール中のアニメ／回転／到達判定の更新
    private void UpdatePatrolWalking()
    {
        _animationController?.MoveVelocity(_agent != null ? _agent.velocity.magnitude : 0f);
        _animationController?.MoveVector(_agent != null ? TargetVector() : Vector2.zero);

        if (_enemyStuts != null)
        {
            switch (_enemyStuts.RotationMode)
            {
                case EnemyStuts.RotationControlMode.Agent:
                    if (_agent != null) _agent.updateRotation = true;
                    break;
                case EnemyStuts.RotationControlMode.Snap:
                    SnapRotateToPlayer();
                    break;
                case EnemyStuts.RotationControlMode.Smooth:
                    if (!_manualRotationDisabledByAgent)
                        ManualSmoothRotateTowardsPlayer(Time.deltaTime);
                    break;
            }
        }

        // 到達判定（パトロール時は通常の到達判定と同じ）
        if (_agent != null && _agent.hasPath && !_agent.pathPending)
        {
            float remaining = _agent.remainingDistance;
            float stopDist = _agent.stoppingDistance;
            if (remaining <= stopDist + 0.1f)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _destinationUpdateTimer = 0f;
                _isPatrolWalking = false;
            }
        }
    }

    // Animator に渡す速度・方向値の更新
    private void UpdateAnimatorValues()
    {
        if (_agent != null)
        {
            _speed = _agent.velocity.magnitude;
        }
        else if (_rb != null)
        {
            _speed = _rb.linearVelocity.magnitude;
        }

        _animationController?.MoveVelocity(_speed);
        _animationController?.MoveVector(_agent != null ? TargetVector() : Vector2.zero);
    }

    // 追跡の有効判定、目的地更新、回転の更新をまとめた処理
    private void UpdateTrackingAndDestination()
    {
        float distanceToPlayer = Vector3.Distance(_enemyTransform.position, _playerPosition.position);

        // 追跡開始距離外なら停止
        if (distanceToPlayer > _enemyStuts.ChaseStartDistance)
        {
            StopMove();
            return;
        }

        // 攻撃距離に入ったら停止
        if (distanceToPlayer <= _enemyStuts.StopDistance)
        {
            StopMove();
            return;
        }

        // 目的地更新（一定間隔）
        _destinationUpdateTimer -= Time.deltaTime;
        if (_destinationUpdateTimer <= 0f)
        {
            _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
            // プレイヤーのコリジョン半径分だけ目標位置を補正して重なりを避ける
            var adjusted = GetAdjustedDestination(_playerPosition.position);
            _agent.SetDestination(adjusted);
        }

        // Smooth 回転モードなら手動回転を適用
        if (_enemyStuts != null && _enemyStuts.RotationMode == EnemyStuts.RotationControlMode.Smooth && !_manualRotationDisabledByAgent)
        {
            ManualSmoothRotateTowardsPlayer(Time.deltaTime);
        }

        // 到達判定: 安定した判定（pathPending が false かつ remainingDistance <= stoppingDistance）
        if (_agent != null && _agent.hasPath && !_agent.pathPending)
        {
            float remaining = _agent.remainingDistance;
            float stopDist = _agent.stoppingDistance;
            if (remaining <= stopDist + 0.1f)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _destinationUpdateTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 移動を停止し NavMeshAgent のパスをクリアします。
    /// </summary>
    public void StopMove()
    {
        if (_agent == null)
        {
            return;
        }

        // 停止フラグを立ててパスをクリア
        if (!_agent.isStopped)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // 移動を完全に止めるために速度等もゼロクリア
        try
        {
            _agent.velocity = Vector3.zero;
            _agent.nextPosition = _enemyTransform.position;
        }
        catch
        {
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // パトロールフラグを折り、目的地更新までのタイマーをリセットして即再発行を防ぐ
        _isPatrolWalking = false;
        _destinationUpdateTimer = _enemyStuts != null ? _enemyStuts.DestinationUpdateInterval : 0.2f;
    }

    /// <summary>
    /// NavMeshAgent が停止中かどうかを返します。
    /// </summary>
    public bool IsStopped => _agent.isStopped;

    /// <summary>
    /// 停止状態を解除し移動処理を再開します。
    /// </summary>
    public void ResumeMove()
    {
        _agent.isStopped = false;
        _destinationUpdateTimer = 0f;
    }

    public Vector2 TargetVector()
    {
        if (_agent == null) return Vector2.zero;
        Vector3 toTarget = _playerPosition.position - _enemyTransform.position;
        Vector3 localDir = _enemyTransform.InverseTransformDirection(toTarget.normalized);
        return new Vector2(localDir.x, localDir.z).normalized;

    }

    // プレイヤーのコライダー半径を考慮して目的地を少し手前にずらす
    private Vector3 GetAdjustedDestination(Vector3 playerPos)
    {
        if (_playerPosition == null) return playerPos;

        // Try find a collider on the player to determine radius; prefer CapsuleCollider then CharacterController
        var playerRoot = _playerPosition.gameObject;
        CapsuleCollider cap = playerRoot.GetComponentInChildren<CapsuleCollider>();
        float radius = 0f;
        if (cap != null)
        {
            radius = Mathf.Max(cap.radius, 0f);
        }
        else
        {
            var cc = playerRoot.GetComponentInChildren<UnityEngine.CharacterController>();
            if (cc != null)
            {
                radius = Mathf.Max(cc.radius, 0f);
            }
        }

        // fallback: small default if no collider
        if (radius <= 0f) radius = 0.5f;

        // 目標位置をプレイヤー方向から radius 分だけ手前に引く
        Vector3 dir = (playerPos - _enemyTransform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return playerPos;

        Vector3 adjusted = playerPos - dir.normalized * radius;
        return adjusted;
    }

    /// <summary>
    /// 即時にプレイヤー方向へ目的地を更新して追跡を開始します。
    /// </summary>
    /// <summary>
    /// 即座にプレイヤー方向へ目的地を更新して追跡を開始します。
    /// </summary>
    public void Approach()
    {
        if (_playerPosition == null || _agent == null) return;
        _agent.isStopped = false;
        _agent.SetDestination(_playerPosition.position);
        _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
        ApplyRotationMode();
    }

    /// <summary>
    /// プレイヤー基準で左右どちらかへ離れる目標点を決めて移動を開始します。
    /// 主に WaitWalk（様子見の歩行）で使用します。
    /// </summary>
    public void StartPatrolWalk()
    {
        if (_agent == null || _playerPosition == null || _enemyTransform == null) return;
        ApplyRotationMode();
        // ランダムで左右どちらかに移動する（プレイヤーを基準）
        int choice = UnityEngine.Random.Range(0, 2); // 0 or 1

        // プレイヤー基準の正面方向
        Vector3 toPlayer = (_playerPosition.position - _enemyTransform.position);
        if (toPlayer.sqrMagnitude < 0.001f)
        {
            // プレイヤーとほぼ同位置なら敵の正面を基準にする
            toPlayer = _enemyTransform.forward;
        }
        Vector3 forward = toPlayer.normalized;

        // 横方向ベクトル（右方向）を作る
        Vector3 right = Quaternion.LookRotation(forward) * Vector3.right;

        // オフセット距離は追跡開始距離と停止距離から計算（最低1m、最大4m）
        float offset = Mathf.Clamp(_enemyStuts.ChaseStartDistance - _enemyStuts.StopDistance, 1f, 4f);
        float sign = choice == 0 ? 1f : -1f;

        Vector3 target = _playerPosition.position + right * sign * offset;

        // NavMesh 上の到達可能な位置へスナップ
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
            _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
            _isPatrolWalking = true;
        }
    }

    /// <summary>
    /// 後退アニメ（root motion）によるステップバックを開始します。
    /// NavMeshAgent の位置更新・回転更新を無効化し、アニメ側の root motion を適用します。
    /// </summary>
    public void StartStepBack()
    {
        if (_animator == null || _agent == null) return;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.isStopped = true;
        _usingRootMotionStepBack = true;
        _animator.applyRootMotion = true;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _animator.SetTrigger(_animName.BackStep);
        _isStepBack = true;
    }

    /// <summary>
    /// ステップバック（root motion）を終了し NavMeshAgent を復帰させます。
    /// </summary>
    public void EndStepBack()
    {
        _usingRootMotionStepBack = false;
        if (_animator != null) _animator.applyRootMotion = false;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        if (_agent != null)
        {
            _agent.Warp(_enemyTransform.position);
            _agent.velocity = Vector3.zero;
            _agent.isStopped = false;
            _agent.updatePosition = true;
            // ステータスの RotationMode に応じて updateRotation を復帰させる
            ApplyRotationMode();
            _agent.ResetPath();
        }
        _isStepBack = false;
    }

    /// <summary>
    /// Animator の OnAnimatorMove イベントから呼ばれる想定のハンドラ。
    /// root motion で移動中は Animator.deltaPosition/Rotation を transform に適用し NavMeshAgent と同期します。
    /// </summary>
    public void OnAnimatorMove()
    {
        if (!_usingRootMotionStepBack) return;
        // Animator.deltaPosition/Rotation を transform に適用
        _enemyTransform.position += _animator.deltaPosition;
        _enemyTransform.rotation *= _animator.deltaRotation;
        // NavMeshAgent と位置同期
        if (_agent != null)
        {
            _agent.nextPosition = _enemyTransform.position;
        }
    }

    /// <summary>
    /// EnemyStuts の RotationMode に従って NavMeshAgent.updateRotation の切り替えや
    /// スナップ回転の実行などを行います。
    /// </summary>
    private void ApplyRotationMode()
    {
        if (_agent == null) return;

        switch (_enemyStuts.RotationMode)
        {
            case EnemyStuts.RotationControlMode.Agent:
                // Agent に回転を任せる
                _agent.updateRotation = true;
                _manualRotationDisabledByAgent = true;
                break;
            case EnemyStuts.RotationControlMode.Snap:
                // 即時で向く
                _agent.updateRotation = false;
                _manualRotationDisabledByAgent = false;
                SnapRotateToPlayer();
                break;
            case EnemyStuts.RotationControlMode.Smooth:
            default:
                // 滑らかに回転する（Agent の回転制御は無効にする）
                _agent.updateRotation = false;
                _manualRotationDisabledByAgent = false;
                break;
        }
    }

    /// <summary>
    /// プレイヤー方向へ瞬時に敵を向けます（Y 軸は無視）。
    /// </summary>
    private void SnapRotateToPlayer()
    {
        Vector3 dir = (_playerPosition.position - _enemyTransform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        _enemyTransform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// プレイヤー方向へ滑らかに回転します（Smooth モード用）。
    /// <paramref name="deltaTime"/> を使ってフレームレートに依存しない回転量を計算します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    public void ManualSmoothRotateTowardsPlayer(float deltaTime)
    {
        if (_enemyTransform == null || _playerPosition == null) return;
        Vector3 dir = (_playerPosition.position - _enemyTransform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        float maxDeg = _enemyStuts.TurnSpeed * Mathf.Clamp01(deltaTime);
        _enemyTransform.rotation = Quaternion.RotateTowards(_enemyTransform.rotation, target, maxDeg);
    }

    /// <summary>指定ターゲットの方向を見る。</summary>

    public async UniTask LookTargetSmooth(float duration, CancellationToken ct = default)
    {
        if (_enemyTransform == null || _playerPosition == null) return;

        Vector3 dir = (_playerPosition.position - _enemyTransform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion start = _enemyTransform.rotation;
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        float elapsed = 0f;

        // 一時的に NavMeshAgent の自動回転を無効化して、回転処理が上書きされないようにする。
        bool previousAgentUpdateRotation = true;
        if (_agent != null)
        {
            previousAgentUpdateRotation = _agent.updateRotation;
            _agent.updateRotation = false;
        }

        try
        {
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _enemyTransform.rotation = Quaternion.Slerp(start, target, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _enemyTransform.rotation = target;
        }
        finally
        {
            // 終了・キャンセル時に必ず以前の updateRotation 設定を復帰する
            if (_agent != null)
            {
                _agent.updateRotation = previousAgentUpdateRotation;
            }
        }
    }
}
