using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;

/// <summary>
/// 最後の一撃（フィニッシュ）演出を制御するマネージャー。
/// フェーズ1: 最後の一撃の瞬間演出
///   - 敵のアニメを一時停止（ヒットストップ）
///   - 画面を白くフラッシュ
///   - SE のみ再生し BGM は停止
/// フェーズ2: 余韻演出
///   - スロー解除（アニメ復帰）
///   - カメラを横から回り込ませる（専用の Cinemachine 仮想カメラを使用可能）
///   - BGM を風の音に切り替え
///   - 敵を倒れる（Collapse 呼び出し）
/// </summary>
public class FinalBlowManager : MonoBehaviour
{
    public static FinalBlowManager Instance { get; private set; }

    [SerializeField] private AudioConfig _audioConfig;
    [SerializeField] private PlayerController _player;
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private float _phase1HitStop = 0.2f;
    [SerializeField] private float _whiteFlashDuration = 0.18f;
    [SerializeField] private float _phase2Duration = 1.6f;
    private int _prevVcamPriority = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartFinalBlow()
    {
        if (_enemyController == null) return;
        DoFinalBlow().Forget();
    }

    private async UniTaskVoid DoFinalBlow()
    {
        // フェーズ1: ヒットストップ（敵の Animator を一時停止）とプレイヤーの短時間スロー
        // 注意: 呼び出し元が player を null で渡しているとスローが適用されないため、

        HitStopManager.Instance.PlayHitStop(_phase1HitStop, _player.gameObject);
        HitStopManager.Instance.PlayHitStop(_phase1HitStop, _enemyController.gameObject);
        Debug.Log("Final Blow: Phase 1 - Hit Stop and White Flash");
        // BGM を停止し、敵の死亡SEを再生する
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.PlaySE(_audioConfig.EnemyDeadSound);
        }

        // プレイヤーは完全停止ではなくスローにする（例: 0.3 の速度）
        HitStopManager.Instance.PlayHitStopSlow(0.2f, 0.3f, _player.gameObject);
        // UniTask のバージョンに合わせてミリ秒で待機（実時間）
        await UniTask.Delay((int)(_whiteFlashDuration * 1000));
        // player のスローは上のコルーチンが終了すると自動で元に戻るため、ここで再設定はしない
        _player.AnimController.PlayTrigger(_player.AnimController.AnimName.SwordSheathing);
        Debug.Log("Final Blow: Phase 2 - Camera and Enemy Collapse");
        await UniTask.Delay((int)(_phase2Duration * 1000));
    }
}
