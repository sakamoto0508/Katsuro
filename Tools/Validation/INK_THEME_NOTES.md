# A案「墨と朱印」の実装メモ

## 戦闘準備
- RunSetupCanvas.prefabをA案の横並びの装備札に変更。
- 墨黒の面、かすれた縁、生成りの文字、朱色の選択枠・印、朱色の開始ボタン。
- 上段に見出しと名前入力。攻撃3種、防御3種、選択説明、相手名とルール、開始ボタンの順。
- 仮案の装備アイコンは、読みやすい漢字の紋に置き換えている。
- 名前入力、装備選択、キーボード・ゲームパッドのナビゲーションは維持。
- 背景の神社や人物を描いた案画像は実装アセットには使用していない。画面構成とUI意匠を反映。

## 戦闘HUD
- GameScene に BattleHUD.prefab を配置。
- PlayerStatus: 左上。PlayerNameを自分のHPバー上部に配置。HP数値、スキルバー、命の朱印2個、装備、半霊半生の表示をまとめた。
- EnemyStatus: 画面上部中央。EnemyNameの下に横長のHPバー。EnemyController.HpRatioを参照し、毎フレーム追従。
- プレイヤーのHP・スキルバーは既存のPlayerHUDPresenter→PlayerHUDView経由で更新。参照を新HUDのPlayerHUDViewに付け替えた。
- 古いHP・スキルCanvasはGameSceneのインスタンスだけ無効化。元プレハブは維持。
- 以前の固定ラベル群はRunHUD_Legacyとして無効化して残し、二重表示を防止。
- 名前はRunSessionの挑戦者名・前回勝者名から表示。初回の敵名は「名もなき守人」。
- プレイヤー名・装備などの文字列更新間隔は0.1秒。新しいUIは実行時に組み立てない。

## Inspectorでの調整
- TitleScene > RunSetupCanvas > PreparationPanel: 準備画面。
- GameScene > BattleHUD > PlayerStatus: プレイヤー名、HP、スキル、命など。
- GameScene > BattleHUD > EnemyStatus: 敵名、HP。
- Rect Transformで位置・サイズ、TextMeshProで文字、Imageで色や画像を調整。
- プレハブ: Assets/Mock/UI/RunSetupCanvas.prefab / BattleHUD.prefab。
- InkフォルダーのPNGはかすれた面・選択枠・命の印。UI素材として作成し、Spriteとして保存。
- 元の装備効果・ボスHP・戦闘ルールは変更しない。

## 検証
- 全体C#コンパイル成功。
- 隔離Unityで13項目成功。準備画面・装備説明・朱色の選択枠・カード幅・名前・敵HPの減少とゼロ・PlayerHUDViewのHP更新・上下関係・中央配置。
- GameScene内のプレイヤー、敵、各バーへの参照とID重複を確認。
- 1280×720のUIプレビューを目視確認。
- Previews/ink-preparation-preview.png と ink-battle-hud-preview.png は隔離環境のUIプレビュー。実ゲーム背景上での通しプレイ確認は未実施。
