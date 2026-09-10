# 明示的Initへの整理（2026-09-11）

## 呼び出し順
- タイトル：TitleManager.Start → TitleManager.Init → SceneInitialization.Init。
- 戦闘：GameManager.Start → GameManager.Init → SceneInitialization.Init → PlayerController.Init → EnemyController.Init → CameraManager.Init。
- SceneInitializationは共通サービス（Audio、GlobalFader、LoadScene、HitStop、FinalBlow、PlayerDead）→入力→アニメーション参照→装備画面・HUD・ダメージ表示の順にpublic Initを呼ぶ。
- キャラクター側はCombatFeedback、AnimationSpeedController、StatusEffectManagerを初期化。武器側はSwordTrailとWeaponHitboxRelayを初期化。
- HitStopManagerが追加のAnimatorを扱う場合も、速度コントローラーのInitを明示的に呼んでから使用する。

## 主な変更
- 各コンポーネントの初期化をAwake/Startからpublic Initへ移動。
- 再呼び出しのガードを追加。元マテリアル・速度基準値の再取得、入力イベントの重複登録、UIプールの重複生成を防止。
- InputBufferは参照取得後に有効状態を反映。OnEnableがInitより先でも入力が使える。
- PlayerDeadManagerはVolumeと死亡テキストの準備もInitから呼ぶ。
- ダメージ表示のShowは生成・初期化を行わず、準備済みプールだけを利用する。
- GameManagerのAwakeは自身のInstance登録とカーソル設定のみ維持。Unityからの入口となるGameManager/TitleManagerのStart、空の未使用Start、第三者アセット内部のライフサイクルは変更していない。

## シーンと動的生成の扱い
SceneInitializationはシーン開始時だけ配置済みの有効なコンポーネントを検索する。無効な旧HUDなどは初期化しない。実行中に新しい対象を配置・有効化する場合は、配置側からそのInitを呼ぶ。

今回の変更は初期化順の統一。SwordTrail以外の既存AddComponentフォールバック、音源・フェード・死亡演出UIの生成は残している。動的追加したコンポーネントについても呼び出し元がInitを呼ぶように修正。生成処理全体を廃止した変更ではない。

## 検証
- 本体C#コンパイル成功。
- FeedbackRunner：10項目成功。スローとヒットストップの終了順、Init再呼び出し後の速度復帰を確認。
- EditableUIRunner：14項目成功。Init再呼び出し後も開いた画面とダメージ表示を保持し、プール数が増えないことを確認。
- CombatInitRunner：4項目成功。Init再呼び出し中の幽体化状態保持と、解除・無効化時の元マテリアル復元を確認。
- 実際のTitleScene→GameSceneの通しプレイは未確認。
