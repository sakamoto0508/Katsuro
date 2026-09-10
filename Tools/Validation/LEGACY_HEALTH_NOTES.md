# 元画像のプレイヤーHP・被ダメージ表示・ライフ球

## 変更
- プレイヤーHPだけ、指定された ChatGPT Image 2026年2月1日 03_49_10.png の画像に戻した。
- 元画像のピクセルは変更せず、専用コピー LegacyHealthAtlas.png をUnity Spriteとして切り分けた。元画像とその既存インポート設定は維持。
- 緑は現在HP。被弾すると即座に減る。
- 赤は被ダメージ表示。0.25秒待ってから、毎秒最大HPの65%の速さで緑へ追いつく。赤が減った部分は空のゲージ背景が見える。
- 連続被弾では赤を保持して待機時間を更新。回復・復活・初期化では緑と赤を同期。無効化時は赤の残りを片付ける。
- プレイヤー名はゲージ上側。敵の名前・HPゲージは前回の画面上部中央を維持。
- ライフの朱印を撤去し、元画像の右下の赤い球を3個に分離して配置。残りライフに応じて左から点灯し、減った分は暗くなる。
- ゲームルールは2ライフのまま。開始時は2個点灯・1個消灯。2→1→0で点灯数も減る。
- SelectionFrameの朱印・かすれ枠は、細い古金色の縁と朱色の下線に変更。新しいSpriteはSelectionOutline.png。
- 旧SelectionFrame/LifeSealのアセットは削除せず保管。現行UIからは参照しない。

## Inspector
GameScene > BattleHUD > PlayerStatus:
- PlayerName: 名前の位置・文字サイズ。
- HealthFrame: 元画像の枠。
- HealthEmpty: 空の背景。
- DamageTrail: 赤。
- HealthFill: 緑。
- LifeOrb1～3: 右下の球。

BattleHUD > PlayerHUDView:
- Damage Delay: 赤が動き始めるまでの待機秒数。
- Damage Catchup Per Second: 赤が減る速さ（正規化HP/秒）。

BattleHUD > RunHUD:
- Life Orbs: 球の参照。
- Life Active Color / Life Empty Color: 点灯・消灯時の色。

TitleScene > RunSetupCanvas > PreparationPanel:
- 各装備ToggleのSelected: SelectionOutline。Imageで変更可能。

## 検証
- 隔離Unityで14項目成功。画像参照、初期値、緑の即時減少、赤の待機と追従、連続被弾、回復、無効化、復活、ライフ2→1→0、新しい選択枠。
- UIのプレビューを目視確認。
- Scene→Prefabの参照ID保持を確認。
- 全体C#コンパイル成功。
- 実ゲーム背景上での通しプレイ確認は未実施。

プレビュー:
- Previews/legacy-health-preview.png
- Previews/selection-outline-preview.png
