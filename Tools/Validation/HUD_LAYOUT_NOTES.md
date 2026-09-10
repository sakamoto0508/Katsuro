# インゲームHUDの配置調整
更新日: 2026-09-11

## 変更
- RunSetupUI のインゲーム用 OnGUI 表示を削除。GameManager も RunSetupUI を動的追加しない。
- GameScene に RunHUD Canvas と Readout、および7つの TextMeshProUGUI を事前配置。
- RunHUD コンポーネントの Player / Visibility / 各ラベルの参照はシーンに保存済み。
- 再生中は文字列と表示状態のみ更新（0.1秒間隔）。位置・サイズ・フォント・色をスクリプトで上書きしない。
- 既存のHP・スキルゲージバーやタイトルの装備選択画面は変更していない。

## 調整する場所
GameScene の Hierarchy:
RunHUD
  Readout
    Challenger  … 挑戦者名
    Opponent    … 相手名
    HP          … HP数値
    Lives       … 命の残数
    Equipment   … 装備
    Skill       … スキル数値
    GhostStatus … 半霊半生・無敵

- Readout の Rect Transform / Pos X, Pos Y でまとめて移動。
- 各子の Rect Transform で個別に移動。アンカー・幅・高さも変更可能。
- TextMeshPro コンポーネントでフォント、文字サイズ、色、整列を調整。
- 再生前の文字は配置確認用のサンプル。実行時には現在のゲーム情報に更新。
- Canvas Scaler は 1280×720 基準、Scale With Screen Size、Match 0.5。
- 参照を差し替える場合は RunHUD の Inspector から対象テキストを指定。
- 日本語フォントは既存の玉ねぎ楷書SDFを使用。別の字形や収録文字が必要ならInspectorから変更可能。
- 勝敗確定・フェード中は CanvasGroup で非表示。UIオブジェクトの生成・破棄はしない。

## 確認
- 全体C#コンパイル成功（既存Unityアセンブリ参照）。
- シーン内のHUD参照、Player参照、重複IDがないこと、全HUDオブジェクトの有効状態を確認。
- Unityでの見た目の目視・通しプレイ確認は未実施。
