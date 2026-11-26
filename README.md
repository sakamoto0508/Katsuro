# Katsuro（活路）

**敵の攻撃を「幽体化」でかわし、ジャスト回避から追撃へつなげる、1対1のボス戦を中心とした3D剣戟アクションゲームです。**

- [プレイ動画](https://youtu.be/gMJ5AtFKUY0)
- [ポートフォリオ](https://global-diagnostic-240.notion.site/6ebbcae3a9fb476ba20aa0e5ad399064)

## 作品概要

| 項目 | 内容 |
| --- | --- |
| 制作形態 | 個人制作 |
| ジャンル | 3D剣戟アクション |
| 言語 | C# |
| Unity | 6000.6.3f1（現行mainのProjectVersion.txt） |
| レンダリング | HDRP 17.6.0 |
| 主なパッケージ | Input System 1.20.0 / Cinemachine 6.6.0 / UniTask |

通常攻撃・強攻撃・幽体化・ジャスト回避・追撃・敵AI・加護選択・修練モードを実装しています。勝利した挑戦者の情報と加護を次回の挑戦へ引き継ぐ仕組みもあります。

## 主要な設計・実装

### 1. State Patternによるプレイヤー行動管理

入力受付を担う`PlayerController`、現在の状態を管理する`PlayerStateMachine`、個別のStateを分離しています。`Enter` / `Update` / `Exit`で、移動・攻撃・幽体化・回復などの処理を管理しています。

```text
Input System → PlayerController → PlayerStateMachine
                                        ├─ Locomotion
                                        ├─ Light / Strong Attack
                                        ├─ Ghost
                                        └─ Heal / その他
```

- [PlayerController.cs](Assets/Katsuro/Scripts/Player/PlayerController.cs)
- [PlayerStateMachine.cs](Assets/Katsuro/Scripts/Player/StateMachine/PlayerStateMachine.cs)
- [PlayerGhostState.cs](Assets/Katsuro/Scripts/Player/StateMachine/States/PlayerGhostState.cs)
- [PlayerStateContext.cs](Assets/Katsuro/Scripts/Player/StateMachine/PlayerStateContext.cs)

### 2. 幽体化・ジャスト回避・追撃

幽体化開始直後に短いジャスト回避受付時間を設け、HP割合に応じて受付時間を補正します。成功時の画面演出や追撃の緩急によって、成功したことが感覚的に伝わるよう調整しています。

- [PlayerGhostState.cs](Assets/Katsuro/Scripts/Player/StateMachine/States/PlayerGhostState.cs)：受付時間と状態復帰
- [GameplayRules.cs](Assets/Katsuro/Scripts/Progression/GameplayRules.cs)：受付時間のルール
- [JustAvoidContrast.cs](Assets/Katsuro/Scripts/Feedback/JustAvoid/JustAvoidContrast.cs)：画面演出
- [JustAvoidCounterAnimation.cs](Assets/Katsuro/Scripts/Player/Animation/JustAvoidCounterAnimation.cs)：追撃演出

### 3. 攻撃の手応えとフィードバック

Animation Eventによる武器判定の制御と、同一攻撃による重複命中の防止を実装しています。命中時のヒットストップ、骨の被弾リアクション、刀の軌跡などを組み合わせています。

- [PlayerAttacker.cs](Assets/Katsuro/Scripts/Player/Actions/PlayerAttacker.cs)
- [CombatFeedback.cs](Assets/Katsuro/Scripts/Feedback/CombatFeedback.cs)
- [EnemyBoneHitReaction.cs](Assets/Katsuro/Scripts/Feedback/HitReactions/EnemyBoneHitReaction.cs)
- [SwordTrail.cs](Assets/Katsuro/Scripts/Feedback/Weapons/SwordTrail.cs)

### 4. 距離別の重み付き敵AI

プレイヤーとの距離を近・中・遠に分類し、距離帯別の行動候補から重み付き抽選で行動を選択します。直前の行動の繰り返しを抑える補正と、データ側で調整可能な構成を採用しました。

- [EnemyDecisionMaker.cs](Assets/Katsuro/Scripts/Enemy/AI/EnemyDecisionMaker.cs)

### 5. 初見プレイヤー向けの修練モード

移動、ロックオン、攻撃、幽体化、ジャスト回避、追撃を段階的に練習できるモードを用意しました。

- [TutorialDirector.cs](Assets/Katsuro/Scripts/Tutorial/TutorialDirector.cs)

## 開発で重視したこと

プレイテストで得た意見をもとに、回避タイミング、追撃の緩急、命中時のフィードバックを調整しました。「機能が動く」だけでなく「プレイヤーの操作意図と結果が気持ちよく伝わる」ことを目指しています。

## プロジェクトの開き方

1. リポジトリをクローンし、`ProjectSettings/ProjectVersion.txt`に記載されたUnity Editorで開きます。
2. Package Managerの依存解決を確認してください。Git URL指定のパッケージや外部アセットなど、環境によって追加の準備が必要になる場合があります。
3. [TitleScene.unity](Assets/Katsuro/Scenes/TitleScene.unity)を開いてPlayします。戦闘用[GameScene.unity](Assets/Katsuro/Scenes/GameScene.unity)、修練用[TutorialScene.unity](Assets/Katsuro/Scenes/TutorialScene.unity)もあります。
4. 操作方法はゲーム内の操作ガイド・修練モードを参照してください。

## 動作確認の注目点

- 幽体化開始直後のジャスト回避受付と解除後の状態復帰
- 攻撃中の当たり判定期間と重複ダメージ防止
- 回復条件を満たさない場合の状態復帰
- 修練モードの進行とボス撃破後の結果表示

※本READMEは実装構成と確認ポイントの案内であり、第三者環境でのビルド成功や全機能の動作を保証するものではありません。
