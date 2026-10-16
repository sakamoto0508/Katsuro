# AKI-111 — ジャスト回避追撃の白銀Cut改善

2026-10-16。今回は実装・静的検証のみ。Play Mode起動、入力シミュレーション、Game View撮影、Scene実行、実戦再現診断の追加、見た目の評価は行っていません。以前のPlay Mode検証結果・画像は今回のShaderの検証結果ではありません。

## A. 原因分析

開始時のGit状態・全差分と、指定の8種類の関連コード／Assetを調査しました。作業開始時点の未コミットAssetを45ファイルのSHA-256で記録しています。

生成経路は `CombatFeedback.Hit → First / Secondの固定Pool → JustAvoidCounterCutVFX.Play → 共有Quad＋段別Material`。各段4個の固定Poolです。専用Cutがある場合、既存のContactPulseを別途重ねない分岐も維持しました。

- 旧Shaderの芯はGaussian形状で、Core width=.055、Firstの面の高さ=.34m。芯の代表的な幅は約.019mで、両端の減衰を含めると設定したQuadより短い細線に見えやすい設計でした。SecondもCore widthは同じで、主な違いはScale・色・保持時間でした。
- 横方向の伸長は存在しましたが、中心の細い線と小さな先端光が主で、前方の肩・細い後端・減衰の方向差が弱い構造でした。
- 外光の幅はShader内の固定値.16、強度も.16固定。芯を増強した際に外光の広さと強さを独立して調整できませんでした。
- Cutの中心は `HitPoint + cameraFacing * .025m`、右方向は実SlashDirectionを視線面に射影した方向です。奥行き斬りでは長さの下限をFirst=.7、Second=.75に保ちます。Bloodは実SlashDirectionを使用します。
- 専用Cut MaterialがZTest Alwaysだったため、接触点がEnemy側にあっても手前のPlayer上へ無条件に描かれます。前回の遮蔽対策が、前景に浮く印象を作る直接の要因です。微小オフセットだけでは解消できません。
- 旧Shaderは独自のHDR RGBを直接返し、HDRP標準Unlitのemissive処理で使用されるpre-exposureを適用していませんでした。今回はローカルのHDRPソースと同じ `GetCurrentExposureMultiplier()` を適用しました。BloomやExposure設定そのものは変更していません。実効Volume Stack・白飛びの程度は実行せずには確定できません。

## B. 変更内容

### 同じQuad内で3層を描画

**White Silver Core**：両端が尖るレンズ形状を基本に、後端を細く、前方の肩を太くした非対称形状に変更。緩い曲率を与え、均一な棒を避けます。先端が素早く伸びてから落ち着く時間曲線、発生時のImpact boost、前方の走る光、後方から薄れる減衰を追加しました。最小ピクセル幅の保証は先端で消えるようにし、尖りを潰さない構成です。

**Outer Glow**：芯より広く薄い発光を、幅・強度・終了時刻で独立調整。明るい背景用の暗い肩と微量の方向フィラメントを維持します。コアが消えた後は弱い残光だけが短時間残ります。

**Contact Flash**：既存Quad内の中心フレアを再利用。局所的な横光と短い縦の光を組み合わせ、コアへ連結。新しいFlashオブジェクトやParticleSystemは追加していません。

時間は既存の `FeedbackVisualTime.Now` による `_Age` を維持。AnimatorのHitStop中も表示時間は進み、ゲームPause中は停止します。SecondのCore holdは.16秒で、既存HitStop .155秒を覆います。HitStop自体の値は変更していません。

### 接触・奥行き

Cut専用2MaterialをAlways(8)からLEqual(4)へ変更しました。HitPoint、HitNormal、SlashDirection、カメラ、Blood、微小オフセット.025mは変更していません。Playerの前に強制的に引き寄せる処理や、ダメージ用HitPointの変更はありません。

その結果、Playerが接触点を覆えばCutも隠れます。これは深度に従う挙動ですが、見せたい一閃が消えすぎないかは手動確認が必要です。確認前にHitPointを胸へ動かしたり、カメラ側へ大きく移したりする修正は加えていません。Blood Materialの既存Depth Testも今回は変更していません。

### ファイル

今回変更したゲームAsset／コードは次の6ファイルだけです。

1. `Assets/Mock/Resources/JustAvoidCounterCut.shader`：3層、非対称形状、伸長・ピーク・方向減衰、調整項目、HDRP pre-exposure。
2. `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs`：寸法・プール寿命・微小オフセットのTooltip、AgeプロパティIDのキャッシュ、MPBのAwake初期化。配置・射影・時間進行の計算は維持。
3. `Assets/Mock/Effects/JustAvoidCounterFirstCut.mat`：Firstの形状・時間・発光初期値、LEqual。
4. `Assets/Mock/Effects/JustAvoidCounterCut.mat`：Secondの形状・時間・発光初期値、LEqual。
5. `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab`：Cut Sizeだけ変更。
6. `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`：Cut Sizeだけ変更。

`JustAvoidCounterConfig.cs/.asset`、`CombatFeedback.cs`、入力・コンボ・Animator・Animation Event・Hitbox・ダメージ・Root Motion・AI・HitReaction・HitStop・カメラ・Blood・Tutorial・UI・Final Blow・Sceneは開始時のままです。新規Material、Mesh、GameObject、ParticleSystemは追加していません。

## C. Inspectorでの調整

Firstは `JustAvoidCounterFirstCut.mat`、Secondは `JustAvoidCounterCut.mat` を選択します。長さは各CFX Prefabルートの `Just Avoid Counter Cut VFX → Cut Size` のXです。同名パラメータをConfigへ重複追加していません。

| Inspector項目 | First | Second | 意味・調整方向 |
|---|---:|---:|---|
| Cut Size X / Y | 1.95 / .6m | 2.6 / .84m | Xは最大長、Yは発光面の高さ。実際の長さは射影・先細りにより短くなる |
| Core width | .13 | .17 | 面の高さに対する芯の半幅。存在感はまずこれを少し増やす |
| Core intensity | 1.45 | 1.75 | 芯の発光量。幅・形を決めてから調整 |
| Initial impact boost | .4 | .65 | 発生時だけ加わるピーク。常時発光を増やさず衝撃を強める |
| Pointed ends / taper strength | .8 | 1.2 | 大きいほど両端を絞る。絞りすぎると短く見える |
| Blade silhouette curvature | .06 | .12 | 緩い反り。0で直線。大きすぎると刀の切断方向から離れる |
| Forward extension duration | .028s | .045s | 短いほど速く抜ける |
| Core hold until | .045s | .16s | 命中から芯が減衰し始める時刻 |
| Core fade ends at | .17s | .25s | 命中から芯が消える時刻。Holdとの差がFade Duration |
| Glow width | .27 | .36 | 芯を包む外光の幅 |
| Glow intensity | .22 | .28 | 外光の強さ。白い帯になる場合はこちらから下げる |
| Residual glow ends at | .235s | .32s | 命中から残光が消える時刻 |
| Contact flare intensity | 1.2 | 1.65 | 局所フレアの強さ |
| Contact flare hold until | .018s | .025s | 初期フレアの保持終了時刻 |
| Contact flare ends at | .07s | .105s | フレアが完全に消える時刻 |
| Cut depth test | LEqual | LEqual | 前景への浮き対策。Alwaysへ戻すとPlayer越しにも描画される |

寿命の項目は「追加で残る秒数」ではなく、すべて命中からの絶対時刻です。SecondではCore holdを.155秒未満に下げると、HitStopの途中から芯が減衰します。Pool DurationはBloodを含む返却時刻なので、Cutの短さの調整に使用しないでください。

より迫力を出す順序：Cut Size X → Core width → Initial impact boost → Core intensity。敵を隠す場合はGlow intensity／Glow widthを先に下げ、芯の尖りを残します。白飛び時に最初からCameraのBloomを変えると通常攻撃や環境にも影響するため、専用Materialを先に調整してください。

Material AssetをPlay Mode中に変更すると終了後も保存される場合があります。比較する数値を記録し、確定した値を段別Assetへ残してください。Prefab AssetのCut Sizeは次回Play開始時のPool生成で確実に反映されます。Play中に生成済みPoolインスタンスのComponentを直接変更した場合は、そのスロットの次の命中配置から反映され、他のPoolスロットには反映されません。

## D. ユーザーによる手動テスト

1. Unity 6000.2.7f2でプロジェクトを開き、C#／Shaderインポート完了を待つ。Consoleに新しいエラーがないこと、2つの専用Materialが `Katsuro/JustAvoidCounterCut` を参照し、ピンクになっていないことを確認する。
2. GameSceneをPlay。普段の操作で抜刀し、敵をロックオンする。敵の攻撃にSpaceのジャスト回避を合わせ、左クリックで追撃1撃目を出す。既存の受付時間内に左クリックして2撃目へつなぐ。今回追加した診断操作はない。
3. 最初は1撃目だけで確認する。命中直後の局所フレア、鋭い先端の伸び、後方から消える白銀コアを確認する。白い棒や長い発光帯に見えないか、普通のHeavyより特別な切断として読めるかを見る。
4. 2連撃を出し、2撃目の芯がHitStop中に残るか、Firstより前方の肩・ピーク・長さ・幅が強いか、その後は残光が短く消えるかを確認する。敵の被弾姿勢が読めることも確認する。
5. 明るい地面、影のある暗い地面、赤草を背景に繰り返す。通常の距離と近距離、斜めの構図で試す。Exposureが落ち着いた状態でも芯が銀色として読め、暗所で白い塊にならないかを見る。
6. Cut中心と実際に刀が接触した部位、敵のCollider近辺、画面上のSlashDirectionを観察する。Playerが手前を横切った際、Player上へ無条件に浮いて見えないことと、逆に全体が隠れすぎないことを確認する。奥行きに斬る構図では方向の射影が弱くなる点にも注意する。
7. 必要なら手動のPause／フレーム送りや録画で命中直後・約.03秒・約.10秒・約.16秒・約.25秒を比較する。録画30fps／60fpsの読みやすさはユーザー側で確認する。
8. 通常Light、通常Heavy、空振りを試す。専用Cutが通常攻撃へ混ざらず、コンボ・ダメージ・Root Motion・Blood・敵Reaction・カメラ・HitStopに意図しない変化がないことを確認する。
9. フィードバックには、段数、背景、距離、命中直後／停止中／停止明け、太さ・長さ・白飛び・遮蔽のどれが気になるかを添える。各段のMaterial値と短い録画があれば次の調整を絞れる。

## E. 確認済みと残課題

**確認済み**

- Unity Editorは閉じており、Pipelineへ接続できないことを確認。プロセス／ログでSafe Modeや背面Editorとの取り違えも除外した。Unity Hub同梱CLIの`unity.exe serve`をEditorと見なしていない。
- Unity 6000.2.7f2同梱のRoslynと、Library/Beeの既存Assembly-CSharp応答ファイル・Unity参照を利用してC#をコンパイル。終了コード0。今回変更部分のエラーなし。既存の非推奨API・未使用フィールド警告9件あり。Editor本体のインポートやIL Post Processingを通した検証ではない。
- ShaderLab／HLSLの括弧、MaterialキーとShader Propertyの対応、ローカルHDRP includeとExposure API、時間設定の順序を静的確認。
- 全Material／Prefabの外部GUID参照が存在することを静的確認。既存GUID、fileID、階層、ParticleSystemを保持。
- First／SecondのCore holdが既存HitStopを覆い、Core → Glowの終了時刻が順序どおりで、Pool返却時刻より短いことを確認。
- 固定PoolとMPB再利用を維持。新たな命中ごとのInstantiate、Material複製、配列、LINQ、文字列生成を追加していない。MPBはPoolインスタンスのAwakeで作成し、AgeのShader IDをキャッシュ。ProfilerによるGC計測は未実施。
- Git diff --checkを通過。開始時点からの保護対象ファイルをハッシュ比較。

**未検証／残課題**

- Unity EditorのShaderインポート、DX12向けShaderコンパイル、Material再インポート、実機GPUの描画。静的チェックを「Shaderコンパイル成功」とはしていない。
- 新しいCore／Glow／Flashの迫力、Bloom／Exposure込みの白飛び、残光の長さ、HitStop中の印象。
- LEqualでPlayerやEnemyに隠れる量。接触点で半分だけ隠れる場合は、実画像を基にCut専用の微小オフセット／面の方向を検討する。ダメージ座標を動かす対応はしない。
- 奥行き斬りの射影方向。ほぼ視線軸と一致する場合は既存フォールバックが働き、画面上の方向が安定しない可能性が残る。射影方式は今回維持。
- 幅広化したQuadの透過描画負荷と実測GC。新規DrawやPool個数は増やしていないが、描画面積は増える。

## F. Git差分の区別

開始時点ですでにPlayer／Enemy Prefab、Animator、各種Config、CombatFeedback、SwordTrail、HitReaction、PlayerAttackState、フォント等に未コミット変更があり、Cut関連の今回対象6ファイルもGit上は未追跡でした。既存変更を戻したり、今回の成果として扱ったりしていません。

開始時の45保護対象のうち今回変更した6ファイル以外の39ファイルはSHA-256が一致しました。両Prefabは `_cutSize` 以外の内容が同一であることも確認しました。今回変更した6ファイルは元々未追跡なので、通常の `git diff HEAD` だけでは今回分を分離できません。

- `Artifacts/AKI-111/Baseline/StartHashes.json`：開始時のハッシュ。
- `Artifacts/AKI-111/Baseline/GitStatus.txt`：開始時Git状態。
- `Artifacts/AKI-111/Baseline/TrackedChanges.patch`：開始時から存在した追跡済みファイル差分。
- `Artifacts/AKI-111/Baseline/Assets/...`：今回対象6ファイルの変更前コピー。
- `Artifacts/AKI-111/ThisTask.patch`：開始時コピーと今回実装との差分だけ。
- `Artifacts/AKI-111/StaticVerification.json`：参照・設定・変更範囲の確認結果。
- `Artifacts/AKI-111/CSharpCompile.log`：今回のオフラインC#コンパイルログ。

今回追加したものはこの報告書とAssets外の調査・静的検証用ファイルだけです。Play Mode診断、Sceneテスト、Game Viewの画像や動画は追加していません。

