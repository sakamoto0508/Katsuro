# 敵HP・スキルゲージ・装備選択の変更（2026-09-10）

## 変更内容
- 敵HP：現在値（赤）が即座に減り、ダメージ残像（黒）が0.25秒後から減少、最後に背景が見える3層構成。
- DamageTrailGaugeはfillAmountだけを更新。画像・色・位置は実行中に上書きしない。時間はunscaledTimeを使用してスロー中も一定速度で減少。
- スキルゲージ：旧CanvasのSkillと同じ元画像・配色・内部比率へ復元。現在のプレイヤーHUDの幅に合わせて配置。
- 装備ToggleにEquipmentOptionSelectionを追加。フォーカス移動時にisOnを更新し、選択枠・説明・決定時の装備を同期。
- TitleSceneの既存InputSystem UIアクションにはWASD・矢印・パッドスティック・十字キーが登録済み。入力を二重に処理する独自キー監視は追加していない。

## Inspectorで調整する場所
Assets/Mock/UI/BattleHUD.prefabを開く（GameScene上のインスタンスでも調整可能）。

EnemyStatus配下：
- EnemyHealthFill：現在HP。ImageのColorとSource Imageを変更。
- EnemyDamageTrail：遅延HP。同じくColorとSource Imageを変更。
- EnemyHealthTrack：空になった部分の背景。ColorとSource Imageを変更。
- EnemyHealthFrame：外枠画像。
- EnemyStatusのDamageTrailGauge：Delayが待ち時間、Catchup Per Secondが1秒あたりに減るゲージ全体の割合（初期0.65）。

新しい画像に差し替える場合、現在値と残像はImage Type=Filled、Fill Method=Horizontal、Origin=Leftを維持する。ゲージ3層のRectTransformを重ね、描画順を背景→残像→現在値にする。Spriteは白またはグレー基調だとColorが意図した色になりやすい。新しいGameObjectで作り直した場合はDamageTrailGaugeのCurrent FillとTrail Fillに割り当て直す。

PlayerStatus/SkillGauge：元のスキルゲージ。HealthGuage/SkillFillのImageがPlayerHUDViewのSkill Fillに接続されている。

Assets/Mock/UI/RunSetupCanvas.prefabの各装備Toggle：EquipmentOptionSelectionを維持する。位置・Navigationは通常のuGUIとして編集可能。

## 検証
- 隔離UnityプロジェクトでEnemyGaugeRunnerの15項目が成功。敵ダメージ即時反映、残像待機・追従・回復、毎フレーム同値更新、色・画像保持、スキルゲージ接続、EventSystemの上下左右移動とクリックを検証。
- プレビュー：Tools/Validation/Previews/enemy-gauge-preview.png（UI単体のレンダー）。
- 実機パッド入力と実際の戦闘通しプレイは未確認。
- 本体C#コンパイル結果は作業報告を参照。

再現用：Tools/Validation/EnemyGaugeAuthor.csは変更前のプレハブに対する一度限りのオーサリング用。既存変更済みプレハブへ再実行しない（残像・スキルゲージを重複生成する）。
