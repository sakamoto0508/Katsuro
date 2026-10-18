# AKI-112 — Counter Second Bloodの板感を抑える調整

## 範囲と確認状態

対象は `JustAvoidCounterFinisherCFX.prefab` の `Crimson Threads` のみ。
Play Mode・ParticleSystemの再生／シミュレーション・Game View撮影は実施しない。
実際の映像品質はユーザーのGameScene検証で判断する。

**Unity Asset APIで適用・保存済み。** Editorの応答回復後、Edit Modeであることを確認し、適用と保存後の値確認を実施した。
`Verification.json` に結果を記録。適用スクリプトは開始値チェックがあり、二重適用を防ぐ。

## 原因の調査

- 主筋は `Crimson Threads` のStretched Billboard。Start Sizeが幅に加え、Length Scaleに比例する長さにも影響する。
- Length Scaleは10.666667、Velocity Scaleは0.055、Camera Velocity Scaleは0。カメラ移動による追加伸長は設定されていない。
- 7本が同時発生し、初期サイズは0.10875–0.14625。Size over Lifetimeは寿命15%で1.12まで膨張し、48%でも1を保持する。太い面が残る設定である。
- 共有Shaderは両端の先細り、輪郭の揺らぎ、暗い縁を既に持つ。輪郭内はほぼ不透明なため、伸長された主筋が重なると赤い面として目立つ可能性がある。
- MaterialのDepth TestはAlways（8）、Queueは3005、ZWrite Off、通常のStraight Alpha合成。手前のPlayerにも重なる原因候補だが、今回共有Materialは変更しない。
- 構成上、大きな細長い血の面の有力な生成元はこの主筋。ただし、ユーザー映像の各ピクセルがどのRenderer由来かは未確定。BloodOnlyで主筋Rendererだけを一時的に無効化すると切り分けられる。

## 段階別設定

| Step | 対象 | 変更前 | 調整値／方針 |
|---|---|---|---|
| A | Main / Start Size（Random Between Two Constants） | 0.10875–0.14625 | 0.0815625–0.1096875（現在値の75%） |
| A | Renderer / Length Scale | 10.666667 | 維持。幅を減らした分を伸長倍率で再補償しない |
| B | Shaderの先細り・輪郭・幅のばらつき | 両端先細り、輪郭変動0.65、Start Sizeのランダム幅 | 維持。共有Shaderを変えてFirstや血滴へ影響させない |
| C | Size over Lifetime | (0,1), (.15,1.12), (.48,1), (.8,.48), (1,.06) | (0,1), (.12,1), (.35,.72), (.6,.36), (1,.06) |
| D | 発生位置・方向 | 既存HitPoint、測定斬撃方向＋Outward Bias0.30、Spawn Offset0 | 変更なし。位置ずれや方向偏りを実行時に確定できていないため |

カーブはClamped Autoで補間し、0.06–1の範囲からはみ出さないことを検査する。
Start Size変更でサイズ由来の長さも25%短くなる。Velocity Scale由来の伸びと粒子の移動速度は維持する。
Size over Lifetimeも縦横両方へ作用するため、単なる幅だけの減衰ではなく、太い面全体が早めに縮む。

### 変更しない値

- 主筋：Start Speed 8.2–9.6、Lifetime 0.30–0.38秒、Burst 7、Cone角18度、Radius0.035、Length0.02、Gravity0.3。
- 主筋のColor over Lifetime・Start Color・Alpha・Material・Shader・Renderer・Simulation Space・Use Unscaled Time。
- `Dark Blood Drops` と `Late Fine Drops` の全設定。
- Counter First、通常Light／Heavy、SwordTrail／Slash Stroke、HitReaction、HitStop0.155秒、SE、カメラと全戦闘ロジック。
- Blood子GameObjectを有効にしてからPlayする修正、固定Pool、再生停止順序。

## Inspectorでの確認・再調整

Prefab Modeで `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab` を開き、`Crimson Threads` を選択する。
MainのStart SizeとSize over LifetimeのSizeを使用する。新しい調整コンポーネントは追加しない。

まず幅だけのStep Aを比較したい場合、Size over Lifetimeを上表の変更前カーブへ戻して比較する。
幅を戻す場合も上表の開始時の値を使用する。Length Scaleを同時に増やすと長い板感が残りやすい。
勢いが弱まった場合、まず後半のSizeカーブを少し上げるか、Start Sizeを開始時の80%（0.087–0.117）へ寄せる。
速度・寿命・粒子数をまとめて変更しない。

## 手動テスト

1. Play開始前に既存Counter ConfigのVFX Debug ModeをBloodOnlyへ設定する。モード切替後はPlayを再開始し、固定Poolを新しいPrefab値で生成する。
2. GameSceneでSecondを命中させ、通常速度と録画スローで、命中直後の勢い・赤い面積・約0.155秒後の主筋の細さ・遅れた血滴を確認する。
3. 血の大きな形状の出所を確定する場合は、同一条件で `Crimson Threads` のRendererのみ一時的に無効化して比較する。検証後に有効へ戻す。子GameObjectや親CFXは無効化しない。
4. Normalへ戻してPlayを再開始し、白銀Slashと敵の被弾姿勢を主筋が隠さないか、Playerの腰との重なりが減ったか確認する。
5. First・Light・Heavyも比較し、副作用がないか確認する。明るい／暗い背景、正面／斜めのカメラでもSecondを比較する。

## 未検証と副作用候補

太さの軽減により遠景や明るい背景で存在感が下がる可能性がある。
Stretched Billboardの長さも縮むため、長い血筋の印象は少し弱まる可能性がある。
共有MaterialのZTest Alwaysは維持したため、Playerとの前後関係を無視した重なりは残り得る。
生成位置が敵の身体と一致するか、奥行き方向の飛散偏り、実HitStop中の視認性は実Sceneで未検証。
今回の値は最終的な映像品質を保証する値ではなく、比較用の調整値。

## Git・検証記録

開始時に12個の既存変更ファイルがあった。`Artifacts/CounterSecondBloodPolish/Baseline/` に開始時のGit差分、HEAD、変更ファイルのSHA256、Second Prefabのコピーを保存。
今回のゲーム用Asset差分はSecond Prefabのみを意図する。調査・適用・検証用スクリプトはAssets外に置き、ゲームのRuntimeへ追加しない。
`Before.json`・`StepAWidthOnly.json`・`After.json`・`Verification.json` はAsset APIが正常に実行された場合に保存される。
`Compile.log` は既存Assembly-CSharpと適用スクリプトをUnity付属C#コンパイラで静的コンパイルした結果。実Sceneテストの証拠ではない。
今回の静的コンパイルはエラー0件・既存警告9件。Unity側でも適用スクリプトのコンパイル成功。
保存値、他の2個のParticleSystemの全設定、全Renderer、CFXコンポーネントの位置・ライフサイクル設定をAPIで確認した。
Blood・First Stroke・Second StrokeのMaterial／Shader参照とShaderコンパイルも正常（Shaderメッセージ0件）。
`Preservation.json` で開始時の変更12ファイルのうちSecond Prefab以外の11ファイルがバイト単位で維持されていることを確認済み。
Second Prefabの開始時からの差分もStart SizeとSize over Lifetimeだけであることを確認。`git diff --check` は正常。
今回追加したのは本報告とAssets外の調査・適用・静的コンパイル用Artifacts。RuntimeのC#・Shader・Materialには今回の変更なし。
