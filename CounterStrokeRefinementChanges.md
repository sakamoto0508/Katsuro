# 直線斬撃の透明感・接触位置・段階差の調整

今回の実装と静的検証を完了。**Play Mode・GameSceneの実行・撮影・見た目評価は未実施**。以下はユーザーの実画面で確認するための初期調整。

## 調査で確認した位置の原因

従来は命中点をストロークの長さ35%位置に配置していた。そのため幾何中心は前方へ長さの15%だけずれ、旧Secondの長さ2.1mでは約31.5cmの差になっていた。さらに先端の白いピークが前方へ出るため、「敵の横にストローク」が出る印象につながり得る。

今回は命中点を長さ50%位置とし、幾何中心をHitPointへ一致させた。白芯も長手方向の中央で最も強く、遠い部分では約22%まで弱まる分布へ変更。前方への抜けは表示マスクと小さな先端光で維持する。HitPoint、Hitbox、ダメージの座標や計算は変更していない。

これは確認できたVFX内のオフセットの修正である。Colliderから得るHitPoint自体が描画上の敵表面とずれる場合や、深度でストロークが部分的に隠れる場合は、実画面で追加確認が必要。

## 色・外周・板感

- 白芯を細くし、画面上の最小半径も.8pxからFirst .6px / Second .65pxへ下げた。
- 先端の白い広がりを長手・幅方向の両方で狭め、前縁の強度を下げた。
- 白い色を冷たい青白へ寄せ、銀青の面の強度も下げて背景が残るよう調整。
- 面と前縁の外周へ追加の滑らかな減衰を入れ、長手の端のフェードも広げた。
- 面の明暗差を少し穏やかにし、平坦な板の輪郭が立ちすぎない設定へ変更。

加算合成・Alpha=0・LEqualは維持。面の透明感は低輝度の銀青発光による表現であり、PBR反射や屈折は追加していない。背景を黒く覆うAlphaは追加していない。

## First / Second

| 項目 | First 旧→新 | Second 旧→新 |
|---|---|---|
| 長さ | 1.35→1.20m | 2.10→2.05m |
| 幅設定 | .085→.072m | .150→.135m |
| 芯の寿命 | .055→.070秒 | .090秒を維持 |
| 総寿命 | .140→.150秒 | .230→.210秒 |
| 段階の強度 | 1→1.1 | 1.25→1.1 |
| Material Core Intensity | 2.4→2.6 | 2.8→2.4 |
| Material Front Intensity | 1.3→.85 | 1.8→1.15 |
| Material Surface Intensity | .8→.55 | 1.1→.65 |

Firstは短く細い形状を保ち、接触中心だけの鋭いピークと少し長い芯で読み取りやすくする方針。Secondは白の総量を抑えつつ、Firstより約1.7倍の長さ・約1.9倍の幅を持つ決定打として区別する。実際の迫力は未評価。

## Inspectorと手動確認

`Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset` のFirst / Secondで **Stroke Length / Width / Core Duration / Duration / Intensity** を調整する。

`JustAvoidCounterFirstStroke.mat` / `JustAvoidCounterFinisherStroke.mat` では以下を確認する。

- **Thin Core Width / Intensity**：接触中心の白芯。Firstが弱い場合は全体の面を上げず、まず芯を調整。
- **Forward Tip and Edge Intensity**：先端・前縁の白。白すぎる場合に下げる。
- **Transparent Silver Blue Face / Blue Face Intensity**：銀青の面の色と存在感。
- **Outer Transparency Falloff**：大きくすると外周がより広く柔らかく消える。初期値First .40 / Second .38。
- **Steel Facet Contrast**：面の明暗差。板の境界が気になる場合は下げて比較。

GameSceneではFirst単発、First→Second、明暗の背景、正面・斜め・奥行き方向、30fpsで比較する。命中時に芯の中心が敵の斬撃位置へ重なり、そこから前縁が抜けるか確認する。BloodとPoseが隠れないか、Firstの芯が読めるかも確認。実HitPointが敵表面から外れて見える場合は、そのフレームの画像と方向を次の調査材料にする。

## 変更と検証

変更した既存ファイルは5個。

1. `Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs`：命中点をMesh中央へ一致。
2. `Assets/Mock/Resources/CounterImpactStroke.shader`：白芯の中心集中、先端光の縮小、面と外周の柔らかい減衰。
3. `Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat`
4. `Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat`：色・芯・前縁・面の強度と外周。
5. `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：段階別の寸法・寿命・強度。

開始時Gitはクリーン（HEAD `693ae0e`）。開始時のAssets/Packages/ProjectSettings全ファイルをHash比較し、上記以外は一致。弧Trail、Prefab、Blood、Enemy HitReaction、HitStop、入力・Animation Event・Hitbox・ダメージ・Root Motion・AI・カメラ・UI・Scene関連に変更なし。新しい描画リソース、サンプリング、毎命中のInstantiate/配列確保も追加していない。

C#全体のコンパイル成功（既存警告9件、エラーなし）。Unity APIで4つのMaterialのShader Passコンパイル成功、Direct3D12でShader診断0件。SceneやGameObjectを作らない実C#関数の計算テストで方向と両段階54頂点の中央位置・直線性・幅上限・先細りを確認。`git diff --check` とConfigの変更範囲確認も成功。

記録：`Artifacts/CounterStrokeRefinement/EditorVerification.json`、`DiffVerification.json`、`Compile.log`、`TaskOnly.patch`。実行時の見た目・接触位置・Pause/Retry等の挙動・Profiler値は未検証。
