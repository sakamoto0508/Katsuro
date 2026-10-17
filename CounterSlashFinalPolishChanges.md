# 白銀Slashの最終ブラッシュアップ

既存の弧Trail・直線Mesh・固定バッファを維持し、5ファイルだけ調整した。**GameScene実行・Play Mode・撮影・見た目の最終評価は未実施。**

## 芯・前縁・後端

直線Shaderの白芯を、裾が広がるGaussianから、細い明部とアンチエイリアス付きの明確な境界を持つ形へ変更した。既存のCore WidthとMinimum Core Radius in Pixelsを使い、過度なぼけと遠距離での消失を抑える。銀青の面の色・強度は維持。

前縁と切先だけに約.04秒の短い強い光を与え、その後は接触中心の芯と青い面を残す。後端は約.025〜.04秒で先に薄くなり、進行方向を読み取りやすくする。全体のBloomや光量を増やす変更ではない。

## HitPointとBloodの一体感

コード上では、ストローク中心と専用Blood Prefabの親の双方へ同じDamageInfo.HitPointが渡される。Unity APIで、First/Secondの各3個のBloodエミッタ位置とShape位置のオフセットがすべて0、Simulation SpaceがWorldであることを確認した。ストローク中心がHitPointに一致するGeometryも計算で確認。

従来はSweepへ過去のtip軌道を35%混ぜていたため、現在のBlood方向と直線方向がずれ得た。有効なSweepがある場合はBloodと同じSweep方向をそのまま使い、tip履歴はSweepがない場合だけの代替に変更した。

命中座標・Blood仕様・実際の接触判定は変更していない。カメラ方向への位置オフセットや前景への強制描画も追加せず、既存LEqualを維持。描画上の敵表面とCollider由来のHitPointが一致するかは実画面で確認が必要。

## 段階差と初期値

| 項目 | First 旧→新 | Second |
|---|---|---|
| 芯幅（Material） | .040→.028 | .045→.035 |
| 最小芯半径 | .60→.55px | .65pxを維持 |
| 芯強度（Material） | 2.6→2.9 | 2.4を維持 |
| 前縁強度（Material） | .85→1.0 | 1.15を維持 |
| 前方への表示伸長 | .015→.010秒 | .022秒を維持 |
| ストローク幅設定 | .072→.068m | .135mを維持 |
| 芯の寿命 | .070→.065秒 | .090秒を維持 |
| 総寿命 | .150→.130秒 | .210秒を維持 |
| 長さ | 1.20mを維持 | 2.05mを維持 |

Firstは小さな鋭いピークと短い残光、Secondは大きな面と長めの余韻で区別する。SecondのConfigは全項目が開始時と一致し、寸法・寿命・全体強度を維持した。ただし、共通Shaderによる芯・前縁・後端の改善はSecondにも適用される。

## ユーザーが確認する項目

GameSceneでFirst単発、連続2撃、通常Heavyとの比較を行う。明るい背景と暗い背景、正面・斜め・奥行き方向、30fpsで以下を確認する。

- 細い芯が読め、前縁・切先の光が小さな鋭いピークになるか。
- First単体の命中が十分に強く、短い青い余韻へ切り替わるか。
- ストローク中心とBloodが同じ斬撃位置に見えるか。Playerの前面などへ浮いていないか。
- Secondの既存の迫力と敵Poseの読み取りが維持されるか。
- Pause/Resume、追撃中断、Retry後に残留がないか。

調整は既存Inspector項目だけを使用する。First/Finisher Stroke Materialの **Core Width / Minimum Core Radius in Pixels / Core Intensity / Forward Tip and Edge Intensity / Forward Reveal Duration**、Configの **Stroke Width / Core Duration / Duration** が主要項目。

純白に近い明るい背景では、維持した加算合成のコントラストが下がる可能性がある。今回の輪郭・ピーク分離が十分かは実画面で評価する。黒い縁取りやAlpha遮蔽を追加して見え方を強制する変更はしていない。

## 変更ファイルと検証

- `Assets/Mock/Resources/CounterImpactStroke.shader`：鋭い芯、短い前縁ピーク、後端の先行減衰。
- `Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs`：Bloodと同じ有効Sweepを優先。
- `Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat`：Firstの芯とピーク、表示伸長。
- `Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat`：芯幅だけ調整。
- `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：Firstの幅・芯寿命・総寿命だけ調整。

開始時Gitはクリーン（HEAD `7a04634`）。開始時のAssets/Packages/ProjectSettings全ファイルをSHA256比較し、上記以外は一致。Blood、弧Trail、HitReaction、HitStop、入力、Animation Event、Hitbox、ダメージ、コンボ、Root Motion、AI、カメラ、UI、Scene関連に変更なし。新規システム・描画Pass・Mesh・リソース確保は追加していない。

C#全体のコンパイル成功（既存警告9件、エラーなし）。4つのMaterialのShader PassはDirect3D12でコンパイル成功、診断メッセージ0件。Scene/GameObjectを生成しない実C#関数の計算確認で、Sweep一致、方向の代替、両段階54頂点の命中点中心・直線性・幅・先細りを確認。Asset参照、Config変更範囲、`git diff --check`も確認済み。

記録は `Artifacts/CounterSlashFinalPolish/EditorVerification.json`、`DiffVerification.json`、`Compile.log`、`TaskOnly.patch`。実行時の見た目・負荷・各ゲーム遷移は未検証。
