# Katsuro のフォルダ構成

本編に手を加えるときは、まず `Assets/Katsuro` を開いてください。

| 場所 | 内容 |
| --- | --- |
| `Assets/Katsuro/Scenes` | 本編の TitleScene・GameScene・TutorialScene |
| `Assets/Katsuro/Scenes/Prototypes` | 試作用の Mock・OutdoorsScene |
| `Assets/Katsuro/Scenes/Data` | NavMesh・シーンに付属するVolumeなどのデータ |
| `Assets/Katsuro/Scripts` | ゲームのC#スクリプト |
| `Assets/Katsuro/Data` | Inspectorで調整する設定データ。Player・Enemy・Feedbackなどで分類 |
| `Assets/Katsuro/Animations` | Clips・Controllers・Masks |
| `Assets/Katsuro/Prefabs` | Characters・Cameras・Audio・Input・Managers・UI・VFX |
| `Assets/Katsuro/Art/UI` | UIのTextures・Fonts・Materials |
| `Assets/Katsuro/Art/Effects` | 既存の命中・回避エフェクト。素材の種類別に分類 |
| `Assets/Katsuro/Art/CombatVFX` | 一閃・ワープなどのモデル、テクスチャ、Material、Prefab、Shader |
| `Assets/Katsuro/Art/Environment/Tutorial` | 修練場の環境素材 |
| `Assets/Katsuro/Input` | ゲームのInput Actions |
| `Assets/Katsuro/Resources` | `Resources.Load` から名前で読み込むShader・Material・ルール設定 |
| `Assets/Katsuro/Editor/Verification` | Editor専用の実機検証補助 |
| `Assets/ThirdParty` | 取り込んだ外部アセット。パック内の構造は維持 |
| `Assets/Samples/HDRPTemplate` | UnityテンプレートのReadme・補助ファイル |
| `Assets/Settings` | HDRPなどのプロジェクト共通設定 |
| `Assets/Resources` | DOTweenなど、外部ツールが使用する共通Resources |
| `Source/Art` | Blender制作データ、生成スクリプト、参考画像、検証映像、バックアップ |

## スクリプトの探し方

- `Core`：ゲーム全体の管理と初期化。
- `Player`：プレイヤーの制御、Actions、Animation、Abilities、StateMachine。
- `Enemy`：敵の制御、AI、Actions、Animation、StateMachine。
- `Combat`：ダメージ情報、武器判定、HitStop、Root Motion。
- `Configuration`：設定用ScriptableObjectのC#型定義。実際に調整する `.asset` は `Data` に配置。
- `Feedback`：命中、回避、武器の軌跡、VFX、勝利時の見た目。
- `Audio`・`Camera`・`Scenes`・`Flow`：音声、カメラ、ロード、タイトル・勝敗の進行。
- `UI`・`Tutorial`・`Progression`・`StatusEffect`・`Rendering`：各機能の処理。
- `Legacy`：現在は空の拡張枠。実処理のあるクラスと区別して保管。

## 配置のルール

1. 本編で使う自作ファイルは `Assets/Katsuro` に追加します。
2. C#の型定義は `Scripts/Configuration`、調整値を保存するアセットは `Data` に置きます。
3. `Resources` 内の相対パスはコードから使われるため、移動するときは `Resources.Load` の呼び出しも確認します。
4. Unityアセットの移動・改名はEditorで行い、GUIDを維持します。
5. 外部パックは `ThirdParty` 内で元の構造を保ちます。`Plugins` と `TextMesh Pro` はツール側の既定配置を維持しています。
6. 制作用ファイル・参考資料・バックアップは `Source/Art`、出力された検証結果は `Artifacts` に置きます。

従来のローカル素材・制作元ファイルに対するGit除外設定は、新しい配置にも引き継いでいます。フォルダ整理だけで外部素材の管理対象を増やすことはしていません。
