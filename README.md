# Katsuro（活路）

**Unity / C#で制作した、1対1のボス戦を中心とする3D剣戟アクションゲーム**です。敵の攻撃を見極めて「幽体化」でかわし、ジャスト回避から追撃につなげる戦闘と、攻撃が命中したときの手応えを重視しています。

- [プレイ動画（YouTube）](https://youtu.be/gMJ5AtFKUY0)
- [ポートフォリオ（設計の意図・制作背景）](https://global-diagnostic-240.notion.site/6ebbcae3a9fb476ba20aa0e5ad399064)

## 作品概要

- **制作形態**：個人制作
- **開発環境**：Unity `6000.2.7f2`、C#、HDRP `17.2.0`
- **主要パッケージ**：Input System `1.14.2`、Cinemachine `3.1.5`、UniTask、UniRx
- **特徴**：通常攻撃・強攻撃、幽体化、ジャスト回避と追撃、敵AI、加護選択、修練モード

戦闘の開始前には攻撃・防御の加護を選択します。勝利した挑戦者の情報と加護は保存され、次の挑戦で戦う相手に引き継がれる仕組みも実装しています。

## 主要な設計・実装

### 1. State Patternによるプレイヤー行動の管理

[PlayerController](Assets/Mock/Scripts/Player/PlayerController.cs)が入力を受け取り、[PlayerStateMachine](Assets/Mock/Scripts/Player/StateMachine/PlayerStateMachine.cs)が現在のStateへ処理を委譲します。移動・攻撃・幽体化などを個別Stateに分け、`Enter` / `Update` / `Exit`を通じて遷移前後の処理を管理しています。

```text
Input System → PlayerController → PlayerStateMachine
                                         ├─ Locomotion
                                         ├─ Light / Strong Attack
                                         ├─ Ghost
                                         └─ Heal / その他
```

代表コード：[PlayerGhostState](Assets/Mock/Scripts/Player/StateMachine/States/PlayerGhostState.cs)、[PlayerStateContext](Assets/Mock/Scripts/Player/StateMachine/PlayerStateContext.cs)。

### 2. 幽体化・ジャスト回避・追撃

幽体化を開始した直後にジャスト回避受付時間を設け、HPの割合に応じて受付時間を変更します。成功時は画面の色味・音量などで成功を伝え、追撃ではアニメーションの振りかぶりと斬撃の速度に差をつけています。

代表コード：[PlayerGhostState](Assets/Mock/Scripts/Player/StateMachine/States/PlayerGhostState.cs)、[GameplayRules](Assets/Mock/Scripts/Progression/GameplayRules.cs)、[JustAvoidContrast](Assets/Mock/Scripts/Feedback/JustAvoidContrast.cs)、[JustAvoidCounterAnimation](Assets/Mock/Scripts/Player/JustAvoidCounterAnimation.cs)。

### 3. 攻撃の手応えとフィードバック

Animation Eventで攻撃中だけ武器の当たり判定を有効にし、同じ攻撃による重複命中を防止しています。命中時にはヒットストップ、被弾方向を踏まえた骨のリアクション、刀の速度に応じた軌跡などを組み合わせています。

代表コード：[PlayerAttacker](Assets/Mock/Scripts/Player/PlayerAction/PlayerAttacker.cs)、[CombatFeedback](Assets/Mock/Scripts/Feedback/CombatFeedback.cs)、[EnemyBoneHitReaction](Assets/Mock/Scripts/Feedback/EnemyBoneHitReaction.cs)、[SwordTrail](Assets/Mock/Scripts/Feedback/SwordTrail.cs)。

### 4. 距離別の重み付き敵AI

プレイヤーとの距離を近・中・遠に分類し、距離帯ごとに設定された行動候補から重み付き抽選で次の行動を選びます。直前の行動を繰り返しにくくする補正を加え、調整値はデータ側で変更できる構成です。

代表コード：[EnemyDecisionMaker](Assets/Mock/Scripts/Enemy/EnemyAI/EnemyDecisionMaker.cs)。

### 5. 初見プレイヤー向けの修練モード

移動、ロックオン、攻撃、幽体化、ジャスト回避、追撃などを段階的に練習できるモードを用意しています。

代表コード：[TutorialDirector](Assets/Mock/Scripts/Tutorial/TutorialDirector.cs)。

## プロジェクトの開き方

1. リポジトリをクローンし、Unity Hubからフォルダを開きます。Unity Editorは`6000.2.7f2`を使用してください。
2. Package Managerによる依存解決を確認します。外部アセットやローカル環境に依存する項目がある場合、追加の設定が必要になることがあります。
3. `Assets/Mock/Scenes/TitleScene.unity`を開いてPlayします。戦闘シーンは`GameScene.unity`、修練モードは`TutorialScene.unity`です。いずれもBuild Settingsに登録されています。
4. 操作方法はゲーム内の操作ガイド・修練モードを参照してください。入力表示はキーボード／ゲームパッドに応じて変わります。

> このREADMEはソースとプロジェクト設定の案内です。全環境でのビルド・プレイ成功を保証するものではありません。

## 動作確認で注目する点

- 幽体化開始直後のジャスト回避判定と解除後の状態復帰
- 通常／強攻撃時のヒットボックス有効期間と一振りでの重複ダメージ防止
- 回復に必要なゲージが不足したときの状態復帰とエフェクト停止
- チュートリアルの課題進行、ボス撃破後の結果表示

## 開発で重視したこと

「仕様どおり動く」だけでなく、「プレイヤーが意図どおりに操作でき、結果が気持ちよく伝わる」ことを目標に、プレイテストで得た意見を実装・演出の改善につなげています。
