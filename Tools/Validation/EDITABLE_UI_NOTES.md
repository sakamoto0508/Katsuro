# uGUIでの「挑戦の準備」とダメージ数値の調整

## 挑戦の準備
TitleScene の RunSetupCanvas > PreparationPanel を開く。
- NameInput: TMP Input Field。枠は Image、文字は Viewport/Text、未入力の案内は Placeholder。
- Attack_0～2 / DefenseOptions/Defense_0～2: 装備選択の Toggle。背景は Image、選択印は Selected、名称は Label。
- AttackDescription / DefenseDescription: 説明文。
- Opponent / Rules / Result: 前回の勝者、ルール、前回の結果。
- StartButton: 開始ボタン。名称は子の Label。
- 各 Rect Transform で位置・サイズ、Image で背景色、TextMeshPro でフォント・文字色・文字サイズを調整。
- 画面全体は PreparationPanel を移動。Canvas Scaler の基準は1280×720。
- Scene上で編集でき、Prefab Modeで Assets/Mock/UI/RunSetupCanvas.prefab を開いて共通デザインも編集できる。
- RunSetupCanvas の RunSetupUI に各参照を保存済み。TitleManager の Setup も設定済み。
- 編集中は見本を表示。実行開始時はCanvasGroupで隠し、タイトルから開く。
- 名前・説明・結果は実データで更新。見た目と配置はコードから上書きしない。
- 矢印/ゲームパッドで移動して決定、マウスでクリック。名前は入力欄から入力。
- 新しい KatsuroUIFont は元の日本語TTFを参照する動的SDF。Padding 8で生成。既存フォントの共有設定は変更しない。

## ダメージ数値
GameScene の DamageNumbersCanvas > Numbers を開く。
- NormalTemplate: 通常ダメージ。TextMeshProでフォント・文字サイズ・色・整列を編集。
- CriticalTemplate: クリティカル。同じ方法で個別調整。編集時の重なりを避けるため初期状態は非表示。見た目を確認するときは一時的に有効にする。
- テンプレートの文字「123」「456」はプレビュー。戦闘時は実際のダメージ値に置き換える。
- テンプレートのRect Transform位置はダメージ位置からのオフセットになる。中心アンカー推奨。
- DamageNumbersCanvas の DamageNumbers コンポーネント:
  - Lifetime: 表示時間（秒）
  - World Offset: 敵のルートからの表示位置。初期値は高さ1.85
  - Screen Offset: Canvas上の位置調整
  - Rise Speed: 毎秒の上昇量（ワールド単位）
  - Pool Size: 同時表示する最大数。既定32
  - Camera: 投影するカメラ。通常は既存のゲーム初期化から渡される
- Assets/Mock/UI/DamageNumbersCanvas.prefab からも編集可能。
- Canvasやフォントをコードで新規組み立てる処理は廃止。再生開始時にテンプレートのコピーを用意して再利用する。
- 通常32個＋クリティカル32個を事前準備し、同時に出る数は合計32個まで。テンプレート本体は戦闘中非表示。
- デザインの変更は再生前に行う。再生中の変更は既に作成されたコピーに反映されない項目がある。

## 検証
- 全体C#コンパイル成功。
- 隔離Unityで14項目成功: 初期非表示、開く処理、名前の入力、択一選択、説明更新、開始ボタン、同一フレームの二重決定防止、ダメージのスタイル継承、文字メッシュ、プール上限、表示終了。
- 準備画面は1280×720のレンダリングを目視確認（Temp/setup-ui-preview.png）。
- 実戦シーンの通しプレイと実機ゲームパッド操作は未確認。
