# 白銀の斬撃面（Silver Sheet）強化 — 2026-10-16

今回はBloodを変更せず、Cut自体を主役にするための強めの初期値を設定しました。実際の見た目の評価・実戦テスト・撮影は行っていません。接続時にユーザー側でUnityがPlay Mode中だったため、その起動・停止・ゲーム操作も行っていません。

## 弱かった原因

前回のOuter Glowは低い輝度の青いGaussian光でした。芯の幅を広げても、その外側に輪郭と密度を持った銀色の面がなく、細い芯が主に残る構成でした。また、発生時の形状は全長の一部から伸びるため、最初の描画フレームで大きな一閃を作りにくい設計でした。

## 変更したもの

- `Assets/Mock/Resources/JustAvoidCounterCut.shader`
- `Assets/Mock/Effects/JustAvoidCounterFirstCut.mat`
- `Assets/Mock/Effects/JustAvoidCounterCut.mat`
- `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab`
- `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`

C#は変更していません。Prefabの変更は `_cutSize` のみで、BloodのParticleSystem、Material、Shader、寿命、方向は維持しています。

既存Quadの中へSilver Sheetを追加しました。Gaussianの薄い外光とは別に、先細りの輪郭・前方に膨らむ肩・細い後端を持つ銀色の面を生成し、その内部に細い明るいファセットを走らせます。面自体が発光するため、Bloomで膨らむことだけに依存しません。面の透過率はコアより低くし、局所的な切断面として重ねます。

発生時から完成形の約7割の輪郭を出し、残りを短時間で前方へ伸ばします。初期のImpact boostとFlashが先に落ち、その後は低いコアをHitStop中に残し、芯の消失後に薄い銀の面と弱い残光を短く残します。新規GameObject・ParticleSystem・Mesh・Draw Pass・Poolは増やしていません。

Depth Testは前回のLEqualのまま。HitPoint、投影方向、微小オフセット、時間管理も変更していません。Player越しの強制前景描画へは戻していないため、近距離で隠れる量はユーザー確認が必要です。

## 段別の初期値

| 項目 | 1撃目 | 2撃目 |
|---|---:|---:|
| Prefab Cut Size X / Y | 2.1 / .72m | 3.2 / 1.12m |
| Core Width | .165 | .28 |
| Core Length | .90 | .98 |
| Core Intensity | 1.75 | 2.7 |
| Initial Impact Boost | .6 | 1.3 |
| Silver Sheet Width | .25 | .48 |
| Silver Sheet Intensity | .5 | 1.2 |
| Extension Duration | .018秒 | .026秒 |
| Core Hold | .03秒 | .16秒 |
| Fade Duration | .095秒 | .12秒 |
| Residual Glow Duration | .075秒 | .11秒 |
| Flash Intensity | 1.6 | 2.7 |
| Flash Duration | .055秒 | .08秒 |
| Flash Hold | .01秒 | .012秒 |
| Taper Strength | .85 | .85 |

1撃目は短いピークと速い減衰を優先。2撃目は面の幅・密度、芯の幅・強度、ピークの倍率を大きく変えています。2撃目の代表的なコア幅は前回の約2.2倍です（形状の先細りとカメラ投影前の設定値比較）。銀の面の代表幅は約.54m。敵全身を覆う大きさを意図していませんが、実際の遮蔽・白飛びは未評価です。

HitStopは1撃目.045秒、2撃目.155秒のままです。2撃目のCore Hold .16秒はその停止時間を覆います。コアが完全に消える時刻は1撃目.125秒、2撃目.28秒、残光の終了はそれぞれ.20秒と.39秒です。Poolの返却時刻は変更していません。

## Inspectorの見方

1撃目は `JustAvoidCounterFirstCut.mat`、2撃目は `JustAvoidCounterCut.mat` を選択します。

- **Core Width**：発光面の高さに対するコア半幅。太さの主調整。
- **Core Length**：PrefabのCut長さに対する使用率（.3～1）。1で最大。銀の面とコアの方向・長さを一緒に調整します。さらに物理寸法を伸ばす場合はPrefabのCut Size Xを変更します。
- **Core Intensity / Initial Impact Boost**：保持中の芯と、発生直後のピークを分けて調整。
- **Silver Sheet Width / Intensity**：今回の主調整。芯の外側の銀の面の広さ・密度。Outer Glowとは別です。
- **Flash Intensity / Duration**：接触中心の強さと消えるまでの時間。
- **Taper Strength**：大きいほど端を絞る。上げすぎると面積が減り、再び細線に近づきます。
- **Fade Duration**：Core Hold後に芯が消えるまでの秒数。
- **Residual Glow Duration**：芯の消失後、残光が消えるまでの追加秒数。

前回の「Core fade ends at」「Residual glow ends at」という絶対時刻の項目は、実際のDurationに置き換えました。古い `_LineDuration` / `_GlowDuration` は段別Materialから除去し、重複した項目は残していません。

Materialの変更は共有Materialを使うPoolへ反映できます。PrefabのCut Size変更を確実に反映するには次回Play開始時にPoolを生成し直してください。生成済みPoolのComponent値はPrefab保存だけで必ず更新されるとは限りません。

## さらにバッサリ感を上げる場合

まず2撃目の **Silver Sheet Width .48→.55**、**Silver Sheet Intensity 1.2→1.5** を試します。次にCore Width .28→.30、Core Intensity 2.7→3.2。長さが不足する場合はCore Lengthを1へ、さらに必要ならPrefab Cut Size Xを3.2→3.5mへ上げます。

白い塊でPoseが隠れる場合は、Flash IntensityとSheet Intensityから下げてください。長く白く残る場合はHoldを伸ばすのではなくFade Durationを下げます。残光だけが長い場合はResidual Glow Durationを下げます。BloomやBloodを増やして迫力を補う必要はありません。

## ユーザー側での確認

GameSceneで普段の操作からジャスト回避追撃を出し、1撃目の速さと2撃目の銀の面を比較してください。2撃目は「強すぎるかもしれない」初期値として設定しています。

命中直後、HitStop中、停止明けで、①芯・銀の面・フレアが区別できるか、②血より先に斬撃が目に入るか、③敵の上半身・被弾Poseが読めるか、④明暗の背景で面が見えるか、⑤近距離でPlayerに隠れすぎないか、を確認してください。通常Light／Heavyに専用Cutが混ざらないことも確認対象です。

## 実施した確認・差分の保護

Unity APIで専用Material／Prefabを保存・再取得し、両Materialのパスを `ShaderUtil.CompilePass` で同期コンパイルしました。`IsPassCompiled=true`、Shaderエラー0件。段別の新パラメータが保存されていること、専用Prefab参照とHitStop値も確認しました。ゲームの見た目や実戦動作を確認したという意味ではありません。

開始時との比較で、意図的な変更は上記5ファイル。C#、Blood、HitReaction、HitStop設定、Animation Event、Hitbox、ダメージ、Root Motion、AI、カメラ、固定Poolは維持しました。Prefabは寸法以外の内容・参照が一致しています。

共有EditorのPlay中に、開始時から変更済みだった `Assets/Mock/UI/KatsuroUIFont.asset` にも追加の保存差分が検出されました。動的フォント更新がAsset保存時に保存された可能性があり、今回のCut変更としては扱っていません。既存の編集・実行状態を保護するため巻き戻していません。専用Assetの更新以外のゲーム操作は行っていません。

`Artifacts/CutSilverSheet/ThisTask.patch` にCutの5ファイルだけの今回差分、`Baseline/` に作業開始時のコピーとハッシュ、`EditorVerification.json` にShaderコンパイル／保存値、`StaticVerification.json` に保護対象の比較結果を記録しています。実戦再現コード、Game View画像、動画は追加していません。
