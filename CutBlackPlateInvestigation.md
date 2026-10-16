# JustAvoidCounterCut 黒い板の調査（2026-10-17）

結論：ユーザーのGame Viewに出た黒い形状がCut由来かは未確定。通常描画の計算・Blendを変更する条件が満たせていないため、今回は切り分け用Debug表示だけ追加した。黒い板の解消を確認した、という報告ではない。

## 調査と変更範囲

- Shader全文、両Materialの全プロパティ、両Prefabの全Renderer/Material参照を確認。
- Unity 6000.2.7f2 / Direct3D12 のEditor APIから参照を照合し、ShaderのPassをコンパイルした。両Materialとも成功、Shader診断メッセージ0件。
- `Assets/Mock/Resources/JustAvoidCounterCut.shader` に `_DebugMode` と表示分岐のみ追加。初期値Normal=0。Normal時の既存RGB/Alpha式は保持した。
- Material、Prefab、C#、Blood、HitReaction、HitStop、カメラ、アニメーション、Poolには変更なし。光量・寸法・色も変更なし。
- Play Modeの起動・停止、入力、撮影、Scene実行評価は行っていない。API確認時のEditorは非Play状態だった。

## 参照と設定

| 項目 | First | Second |
|---|---|---|
| Prefab（Assets/Mock/Effects/以下） | JustAvoidCounterFirstCutCFX.prefab | JustAvoidCounterFinisherCFX.prefab |
| Cut Material（同上） | JustAvoidCounterFirstCut.mat | JustAvoidCounterCut.mat |
| Core width / gain | .18 / 1.9 | .32 / 3.1 |
| Sheet width / intensity | .32 / .65 | .64 / 1.7 |
| Strike multiplier | 1 | 1.35 |
| Core hold / fade | .025 / .08秒 | .16 / .095秒 |
| Flash hold / end | .006 / .045秒 | .008 / .06秒 |
| Residual glow | .06秒 | .11秒 |
| Edge contrast | .5 | .6 |
| Quad envelope | 2.15 × .76m | 3.4 × 1.2m |

Cutは両方とも `Silver Cut + Contact + Afterimage` のMeshRenderer、共通の `JustAvoidCounterCutQuad.asset` と `Katsuro/JustAvoidCounterCut` Shaderを参照。欠落・取り違えなし。First/Secondの設定独立性を維持。

各Prefabの他のRendererはBlood用ParticleSystemRenderer 3個。こちらは `JustAvoidCounterBlood.mat` / `JustAvoidCounterBlood.shader` を参照する。読み取りのみ実施。

全Material値・全Rendererの参照記録：`Artifacts/CutBlackPlate/AssetInspection.json`（変更前）と `EditorVerification.json`（変更後）。

## Blend / RGB / Alpha

現状は `Blend One OneMinusSrcAlpha`。合成式は `出力RGB = ShaderのRGB + 背景RGB × (1 − Alpha)`。

Core/Sheet/Flash/GlowのRGBは各マスク・寿命で重み付け済みの発光成分として加算されている。単純にRGB全体へ再度Alphaを掛ける修正は採用しなかった。HDRのRGBがAlphaを超えること自体は異常ではない。

一方、AlphaはRGBから導かれていない独立の遮蔽量である。

```
Alpha = saturate(core*.95 + edge*_EdgeContrast + contact*.75 + sheet*.32 + glow + air)
RGB = emission * _StrikeMultiplier * GetCurrentExposureMultiplier()
```

確認できた重要な点：

1. **`edge * _EdgeContrast` はAlphaだけを増やし、対応するRGBがない。** Inspectorにも `Dark shoulder on bright backgrounds` と明記された既存の暗い縁取り。Core/Sheet/Flashがほぼ0でも背景を暗くする領域がある。これは無条件に不具合とは判定しない。
2. **Sheet単体はRGBとAlphaの双方に同じ `sheet` 値を使う。** 現在のSheet Tintは正の値なので、有限の計算結果・Exposure=1を仮定すると、Sheetだけが広い無発光の不透明板を作る式ではない。
3. **ExposureはRGBだけに適用され、Alphaには適用されない。** 低いExposure倍率では、発光が弱まっても遮蔽が残る。HDRP標準のemissive経路もpre-exposureを使用するため、この関数を外すことが正しいとは断定できない。実Game ViewのExposure倍率は未測定。
4. Strike multiplierを0にしてもAlphaは残るため、**この値を0にする操作はCut非表示の代わりにならない。** 切り分けにはRenderer.enabledを使用する。

CPUの式走査（各Material 181,447点、fwidth=.01を仮定）では、Secondの例として正規化座標(-.55, -.0125)、age=0でAlpha≈.487、RGB≈(.021,.033,.047)となった。edge由来のAlpha≈.452。明るい背景を約半分暗くし得ることを示す。実際の画面上の座標や面積を検証した値ではない。

走査のコードと結果：`Artifacts/CutBlackPlate/StaticScan.cjs` / `StaticScan.json`。これは意図された数学上の二乗を使用したCPU計算であり、GPUのpow実装やGame Viewを再現したものではない。

## 数値計算の別の候補

head / glow / air / contactに計8箇所、負値を取り得る座標への `pow(value, 2)` がある。HLSLの負の底はNaNになる仕様であり、GPUコンパイラの最適化次第では描画不定の候補となる。[Microsoftのpow仕様](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-pow)

ただし、現在のShaderコンパイル診断にエラー・警告はなく、実GPUがこれらを乗算へ最適化しているか、問題のフレームでNaNを出しているかは未確認。**黒い板の原因と確定していないため、この計算も今回の最終変更には含めていない。** Cut由来と判明し、この候補を追う場合は、符号付き座標の二乗を `value * value` に置き換えた版との比較が最小の次の検証となる。

## Depth / HDRP / 描画順序

- CutはQueue=3010（Transparent+10）、RenderType=Transparent、LightMode=ForwardOnly。HDRP通常Transparent範囲2900〜3100内。
- `ZTest LEqual`、`ZWrite Off`、`Cull Off`。MaterialのZTest=4をAPIでも確認。両CutのsortingOrder=0、sortingLayerID=0。
- LEqualによる不合格画素は描画されない。これだけで黒いRGBを生成する処理ではない。敵に隠れて銀の面が見えない可能性は残る。
- BloodはQueue=3005、ZTest=Always（8）。Cutより前のQueueで描かれる設定。実フレームの全Transparentオブジェクトの合成順序はFrame Debugger等での追加確認が必要。
- 独自ForwardOnly Passが直接RGB/Alphaを返す構成。標準HDRP MaterialのApplyBlendModeがその後もう一度RGBへAlphaを掛ける構造ではない。Fogや他のHDRP標準Material機能を自動的に全て受けるShaderでもない。
- Blend / Queue / Depth / Exposure処理は変更していない。

## ユーザーによる確認順序

GameSceneのPlay Mode操作と撮影はユーザー担当。まずSecond用Material `Assets/Mock/Effects/JustAvoidCounterCut.mat` をInspectorで開く。`Diagnostics > Cut debug view` を切り替える。shared MaterialなのでSecondの固定Pool全インスタンスに反映される。First用Materialは別設定。

1. **Normal**：2撃目の黒い形状を再現。命中直後の同じタイミングで比較する。
2. **Cut Rendererの無効化/有効化**：問題フレームでUnityをPauseし、現在アクティブな追撃Prefab内の子 `Silver Cut + Contact + Afterimage` のMeshRendererだけを一時的に無効化。Bloodの親やParticleSystemRendererは操作しない。黒い形状が消えるか確認し、すぐ元に戻す。次の命中は別Poolスロットを使うため、必ず同一フレーム・同一のアクティブな個体で比較する。消えなければCut以外を調べる必要がある。
3. **Solid Magenta**：同じ2撃目を発生させ、Rendererの可視範囲と黒い形状の範囲を比較。全Quadを不透明マゼンタで表示する診断であり、最終の斬撃形状ではない。LEqualは有効なので敵の背後の部分は隠れる。マスクと寿命を無視し、PrefabがPoolへ戻るまで表示される点にも注意。
4. **Core Mask**：色・光量・Exposureを除き、Coreの形状と寿命を白いマスクで確認。
5. **Sheet Mask**：同様に独立した銀の面の形状と寿命を確認。Normalで黒くなる領域と重なるか確認。
6. **Flash Mask**：接触中心の短いマスクを確認。Secondは.06秒で消えるため、命中直後を見る。FlashのGaussian計算自体はNormalと共通なので、このモードだけ異常なら負値powの候補も追う。
7. **Normalへ戻す**。Cut由来と確認でき、黒い領域が残る場合のみ、既存の `Dark shoulder on bright backgrounds` を一時的に0へ変更して比較する。Secondの元値は.6（Firstは.5）。比較後は必ず復元。ここで消えればAlpha専用の縁取りを原因として狭く修正できる。

Maskモードは `RGB=(mask,mask,mask), Alpha=mask` のpremultiplied表示。マスク外は透明なので、診断用に黒い矩形を追加しない。色・光量・Exposureを省くが、深度・描画順序は維持する。マゼンタとマスクはBloom/トーンマッピング等による画面変換を完全に無効化するものではない。

切り分け結果として必要な情報：Renderer無効化で黒が消えるか、Normal/各Maskの同じタイミングの画像、Dark shoulder=0比較結果。Maskが正常なのにNormalだけ黒い場合は、さらに実フレームのExposure倍率とFrame DebuggerでCutのDraw前後・Blend状態を確認する。

終了時は両MaterialをNormalへ戻し、Renderer.enabledとDark shoulderも元の値に戻す。実際の白銀の見え方、接触位置、HitStop中の表示は未検証。

## Gitと保護の確認

作業開始時に既存の未コミット変更を記録し、Assets/Packages/ProjectSettings内の既存変更・未追跡ファイルをSHA256で比較した。今回変更された既存AssetファイルはShader 1個だけ。Material/Prefab/C#を含む他の開始時ファイルは変更なし。

このShader自体も開始時から未追跡だったため、通常の `git diff` だけでは今回分を分離できない。開始時コピーとの差分を `Artifacts/CutBlackPlate/TaskOnly.patch` に保存した。既存の大きなGit差分は今回の成果に含めない。

新規追加は本報告書と `Artifacts/CutBlackPlate/` 内の調査記録のみ。ArtifactsのC#/JSはAssets外であり、ゲームへ診断システムを追加していない。命中処理のInstantiate/GC Alloc追加なし。
