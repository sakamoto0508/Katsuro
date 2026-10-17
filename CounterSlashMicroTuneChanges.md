# 白銀Slashの最終微調整

ゲームAssetの変更は既存Stroke Material 2個、計6項目のみ。Shader、C#、Geometry、色、サイズ、寿命、位置・方向は維持した。Play Modeと実Game Viewの見た目は未検証。

| Material | パラメータ | 変更前→変更後 | 意図 |
|---|---|---|---|
| First | Core Intensity | 2.90→3.05 | 面積を増やさず芯のピークを約5%強化 |
| First | Forward Tip and Edge Intensity | 1.00→1.08 | 前縁・切先の短いピークを約8%強化 |
| Finisher | Blue Face Intensity | .65→.56 | 面の発光を約14%抑え、背景と芯の分離を改善 |
| Finisher | Thin Core Width | .035→.031 | 太くせず輪郭を細くする |
| Finisher | Forward Tip and Edge Intensity | 1.15→1.20 | 前縁・切先の局所ピークを約4%強化 |
| Finisher | Steel Facet Contrast | .60→.65 | 薄い銀青の面の明暗差をわずかに強める |

対象ファイル：

- `Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat`
- `Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat`

現行Shaderは加算合成でAlpha=0のため、独立した「不透明度」を上げ下げする方式ではない。今回は既存のSurface Intensityで面の発光を減らして透明感を調整した。白銀中心と銀青の周辺色、既存の時間・方向グラデーションは維持。画面上の最小芯半径も維持しているため、遠距離では芯幅変更の差が小さくなる可能性がある。

## 接触位置

CombatFeedbackはストロークとBloodへ同じHitPointとSlashDirectionを渡す。ストロークのu=.5の中心はそのHitPointへ一致し、有効なSweepもBloodと同じ方向を使う。コード上の明確なずれは見つからなかったため、座標補正や方向変更は行っていない。敵の描画表面とCollider由来の位置の関係は実Game Viewで確認が必要。

## Game Viewでの比較

1. 同じ敵・距離・カメラ角度でFirst単発を比較。面の大きさが変わらず、芯と前縁が読み取りやすくなったか確認。
2. Secondを明るい背景と暗い背景で比較。銀青の面が薄くなり、芯が細く見え、既存の迫力が保たれるか確認。
3. 30fpsと通常fpsで、先端の短いピーク、HitStop中の芯、Bloodとの接触位置を確認。
4. 位置が気になる場合は、その方向と命中フレームを記録。HitPointやカメラを根拠なく変更して比較しない。

## 復元と検証

変更前のMaterialファイル全体とEditor上の値を `Artifacts/CounterSlashMicroTune/Baseline/` に保存。6項目の旧値・新値は `Artifacts/CounterSlashMicroTune/Changes.json` に記録した。元に戻す場合は表の旧値をInspectorへ入力できる。

6項目だけを旧値へ戻すUnity APIスクリプト `Artifacts/CounterSlashMicroTune/RestoreMaterialValues.cs` も用意した（未実行）。他のパラメータを戻さず、今回分だけ復元するためのもの。

開始時Gitはクリーン（HEAD `46a4c89`）。開始時のAssets/Packages/ProjectSettings全ファイルをSHA256で照合し、2つのMaterial以外は一致。Blood、HitReaction、HitStop、入力、Animation Event、Hitbox、ダメージ、Root Motion、AI、カメラ、通常攻撃、Prefab、Scene等に変更なし。新規クラス・Renderer・ParticleSystemや描画負荷は追加していない。

Unity APIで両MaterialのConfig参照とShader Passコンパイルを確認し、診断メッセージ0件。`git diff --check`も成功。ゲームC#/Shaderに変更がないため、全体再コンパイルや実行テストは行っていない。検証結果は `Artifacts/CounterSlashMicroTune/EditorVerification.json` と `DiffVerification.json` に記録した。実Game Viewでの最終評価はユーザー担当。
