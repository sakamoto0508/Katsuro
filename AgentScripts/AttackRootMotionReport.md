# Player / Enemy Attack Root Motion

Unity Editorで調査・修正・Compile・保存・再取得を実施。Play ModeとRuntimeの視覚確認は未実施。

## Clip調査

下記は実PrefabのAvatarをPreview Sceneで120Hz評価した単独ClipのXZ差分合計の長さ。ゲーム内の実移動距離ではない。Blend、当たり判定、キャンセルにより実移動量は変わる。

| 対象 / Clip | 倍率適用前の正味XZ移動(m) | 設定倍率 | 計算上の倍率適用後(m) |
|---|---:|---:|---:|
| Player LockOn / Samurai_Attack_Combo_B1 | 0.485 | 0.25 | 0.121 |
| Player LockOn / Samurai_Attack_Combo_B2 | 1.433 | 0.25 | 0.358 |
| Player LockOn / Samurai_Attack_Combo_B3 | 0.775 | 0.25 | 0.194 |
| Player LockOn / Samurai_Attack_Combo_B4 | 1.502 | 0.25 | 0.376 |
| Player LockOn / Samurai_Attack_Combo_B5 | 1.050 | 0.25 | 0.263 |
| Player UnLock / ARPG_Samurai_Idle2_to_Idle1（攻撃前の構え変更） | 0.023 | 0.25 | 0.006 |
| Player UnLock / ARPG_Samurai_Attack_Combo1 | 0.607 | 0.25 | 0.152 |
| Player UnLock / ARPG_Samurai_Attack_Combo2 | 1.028 | 0.25 | 0.257 |
| Player UnLock / ARPG_Samurai_Attack_Combo3 | 1.513 | 0.25 | 0.378 |
| Player UnLock / ARPG_Samurai_Attack_Combo4 | 1.428 | 0.25 | 0.357 |
| Player Heavy / ARPG_Halberd_Attack_Heavy1_Start | 0.421（後方） | 0.25 | 0.105 |
| Player Heavy / ARPG_Halberd_Attack_Heavy1 | 2.645 | 0.25 | 0.661 |
| Player Heavy / ARPG_Halberd_Attack_Heavy2 | 1.604 | 0.25 | 0.401 |
| Player Counter / ARPG_Samurai_Attack_Heavy1 | 2.605 | 0.25 | 0.651 |
| Player Counter / ARPG_Samurai_Attack_Heavy2 | 1.997 | 0.25 | 0.499 |
| Enemy Heavy2 / ARPG_Halberd_Attack_Heavy1_Start | 0.590 | 0.18 | 0.106 |
| Enemy Heavy2 / ARPG_Halberd_Attack_Heavy1 | 3.709 | 0.18 | 0.668 |
| Enemy Heavy1 / ARPG_Samurai_Attack_Heavy1_Start | 0.434 | 0.18 | 0.078 |
| Enemy Heavy1 / ARPG_Samurai_Attack_Heavy1 | 3.652 | 0.18 | 0.657 |
| Enemy Light1 / ARPG_Halberd_Attack_Dodge | 2.700 | 0.18 | 0.486 |
| Enemy Light2 / ARPG_Halberd_Attack_Combo4 | 2.617 | 0.18 | 0.471 |

全参照攻撃ClipにRootT曲線あり。攻撃前の構え変更Clipは正味移動がごく小さい。Just AvoidのHeavy1_Start（回避準備）は今回のAttack Root Motion対象から除外し、既存処理を維持。EnemyAttackDataはSlash分類の4件で、LightAttack / LightAttack2 / HeavyAttack / HeavyAttack2を発火する。Thrust / WarpAttackのClip割り当てはないため、新規割り当ては行っていない。

## Clip / Animator設定

対象は独立した`.anim`であり、共有FBXのImport設定は変更していない。XZ Bake Into Pose OFF、Y ON、Rotation ONが既に設定されていたため全て維持。Root Motion NodeはModelImporter対象外。Clip曲線・再生速度・Transition・Hitbox Event・Combo Eventは変更していない。

Player.controllerの15個、Enemy.controllerの6個の攻撃StateにAttackRootMotionStateを追加。Locomotion、Death、Reaction、StepBack、Just Avoid準備Stateには追加していない。Counterの既存Tagと速度Behaviourを維持。

Animatorの自動applyRootMotionはfalseのまま。OnAnimatorMoveがある場合のHandled by Scriptを利用し、攻撃State所有者の一致とMoverの攻撃フラグでXZ差分の適用を許可する。実Avatar評価でapplyRootMotion=falseでもdeltaPositionが取得できることを確認済み。攻撃ごとのapplyRootMotion切替によるAnimator再初期化を避ける。

## 移動の責務

- PlayerAttackState.Enter: MoveStop維持、BeginAttackRootMotion。Exit: StopAttackRootMotion。
- PlayerController.OnAnimatorMove: 現在／遷移先の攻撃State所有者を確認し、deltaPositionをPlayerMoverへ渡す。FixedUpdateで蓄積差分を消費する。
- PlayerMover: XZだけを使用。Dynamic Rigidbodyの水平velocityへ変換し、Y速度／重力とColliderを維持する。通常Movementとの同時実行を抑止する。
- Enemy: HoldMovementForAttackでNavMeshAgentの位置／回転更新を止めて無効化。StepBackと別フラグで攻撃差分を蓄積し、FixedUpdateでRigidbody.MovePositionを使用する。
- 既存Solid ColliderへSweepし、EnemyはNavMesh境界もRaycastして制限する。
- Enemy終了: Rigidbodyの現在位置をAgent.Warp／nextPositionへ同期してから追跡を復帰。開始位置は使用しない。従来のRigidbody状態も復帰する。
- State所有Hashにより、コンボの古いState.Exitが新しいStateのRoot Motionを解除しない。
- Reaction、Death、非戦闘状態、Disable、Destroyで攻撃差分を破棄。Enemy Counter Reaction開始前にRoot Motionを終了する。
- StepBackのdeltaPosition／deltaRotation適用処理は既存のまま。

## Counter / HitStop

既存JustAvoidCounterAnimationとAnimationSpeedControllerは変更していない。Animatorが評価したdeltaPositionを使うため、Windup / Slashの速度制御を重ねて計算しない。

Animator.speedが0の間は差分を蓄積・適用せず、保留差分を破棄。Playerの水平velocityも停止する。再開時に停止前の差分を一括適用しない。RootのY・Rotationは攻撃には適用しない。

## 変更ファイル

- Assets/Mock/Scripts/Player/PlayerController.cs
- Assets/Mock/Scripts/Player/PlayerAction/PlayerMover.cs
- Assets/Mock/Scripts/Player/StateMachine/States/PlayerAttackState.cs
- Assets/Mock/Scripts/Enemy/EnemyController.cs
- Assets/Mock/Scripts/Enemy/EnemyAction/EnemyMover.cs
- Assets/Mock/Scripts/Feedback/AttackRootMotionState.cs（新規）
- Assets/Mock/Scripts/Feedback/AttackRootMotionPhysics.cs（新規）
- Assets/Mock/Prefabs/Player.prefab
- Assets/Mock/Prefabs/Enemy.prefab
- Assets/Mock/AnimationController/Player.controller
- Assets/Mock/AnimationController/Enemy.controller

Animation Clip / FBX Import設定の変更なし。今回の作業ではSwordTrail、VFX、Damage、AI距離設定、HitStop時間、Final Blow、UIを変更していない。作業開始時からあったSwordTrail・UIFont・TimeManagerの差分は維持。

## 検証

C# Compile: completed / failed=false / errors=[]。Prefab / AnimatorをUnity APIで保存し、攻撃StateへのBehaviour割り当て、倍率、Root設定、Rigidbody Collision、Clip設定とEventの維持を再取得確認。

Edit Mode: 床に接した水平移動、壁Sweep、PlayerのXZ／倍率／Y速度保持、HitStop中の差分破棄と再開時の非再生、コンボ所有者の維持と最終Exit解除、Enemy Reactionによる即時解除を確認。

Preview SceneではNavMeshAgentがバインドできず、Agent.Warpによる実際の復帰は未検証。保存設定とコード構造の確認のみ。Play Mode、Runtimeの前進・足滑り・貫通・視覚確認は未実施。

調査の詳細: AttackRootMotionBefore.txt / AttackRootMotionMeasurements.md / AttackRootMotionVerification.txt。再実行用Editor Script: InspectAttackRootMotion.cs / MeasureAttackRootMotion.cs / ConfigureAttackRootMotion.cs / ValidateAttackRootMotion.cs。
