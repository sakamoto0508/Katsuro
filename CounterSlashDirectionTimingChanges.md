# JustAvoidCounter 白銀斬撃：方向・接触・時間の調整

2026-10-17。実Scene / Play Modeでの見た目は未検証です。Play Modeの起動・入力・撮影は行っていません。

## 原因と変更方針

- 両段階共通：直線Strokeは、刀先履歴よりも命中データのSweep / SlashDirectionを優先していました。Hitboxの移動方向にはキャラクター移動が含まれ、刀先の振り方向と一致する保証がありません。刀先の実測速度から所有者の平行移動を引いた方向を優先するよう変更しました。物理判定に渡す方向は変更していません。
- 両段階共通：ワールド方向が視線に近い場合、画面上では線が短くなります。表示方向だけを実方向から最大20度まで補正します。測定した画面上の向きを保ち、無条件の水平化はしません。
- 1撃目：コアは従来10msから減衰し65msで消えていました。30fpsでは減衰中の1〜2フレーム程度しか得られない可能性があります。保持と減衰を分け、幅を増やさず長さ・読み取り時間を補強しました。
- 2撃目：従来のコア消失90msはHitStop 155msより短く、停止途中で芯がなくなる設定でした。コアを155ms保持し、205msまでに消す設定へ変更しました。

上記はコード・設定から確認できる原因です。実Game Viewで「敵の横に出る」現象が方向・遮蔽・実接触位置のどれに起因するかは未確定です。

## 方向の算出

1. 最新の刀先移動から所有者の平行移動を差し引き、FeedbackVisualTimeの経過時間で速度を算出します。
2. 既存SwordTrailと同じ速度閾値（既定3.5m/s）以上なら、その瞬間方向を採用します。
3. 瞬間値が得られない場合は、既存サンプルの80ms以内の速度履歴を使用します。直近2方向が約26度以内で一致する場合だけ、新しい方向を2:1で優先して平均します。逆向きや大きく曲がる軌跡は平均しません。
4. どちらも信頼できる速度でない場合のみ、既存SweepDirectionに戻します。方向を取得できなければStrokeを描きません。
5. カメラ視線に対する投影長が55%未満のときだけ、最大20度まで表示軸を視線平面側へ回します。元の投影方向と奥行きの符号は維持します。視線と完全に重なり、画面方向を決める信頼できる履歴もない場合は補正しません。

既存サンプルに速度メタデータを追加しました。別の軌跡サンプラーはありません。新たな固定Vector3配列は40要素（データ部分480bytes）で、初期化時に確保します。毎フレーム・命中ごとのManaged配列確保やInstantiateは追加していません。

攻撃Clip固有の固定軸は採用していません。AnimatorController上のHeavy1 / Heavy2と各Animation EventをUnity APIで読み取りましたが、リターゲット後の実刀先方向はPlay Modeで未計測です。Clip名から方向を推測して上書きするより、刀先の実測方向を優先しています。

## 接触位置と描画

- CombatFeedbackが受け取るDamageInfo.HitPointを、Strokeと既存CFXの双方へ渡しています。
- Stroke Meshの中央（UV x=0.5）は同じHitPointに一致します。今回、中心移動・カメラ方向の押し出し・Enemy Transform補正は加えていません。
- 両CFX PrefabのBloodはWorld空間で、Emitterのローカル位置・Shape位置は0です。発生原点は同じHitPointです。飛沫のその後の移動は従来通りです。
- Strokeは既存の27頂点 / 32三角形Meshを再利用。Blend One One、RGBのみ、Alpha=0、ZTest LEqual、ZWrite Off、Queue 3011を維持しています。RGBが0の領域がAlphaで背景を黒く覆う合成ではありません。
- 弧状SwordTrailはQueue 3010、Bloodは3005。両CFXは既にLocal Contact Flash設定で、旧Cut Quadは再生時に非表示です。今回、追加FlashやRendererは作成していません。

ColliderのHitPointと実際に表示される敵Rendererの表面位置、Playerによる遮蔽、透明描画の見かけの重なりは実Scene確認が必要です。LEqualのため、身体の裏側に入った部分は遮蔽されます。常時最前面表示には変更していません。

## First / Second 設定値

秒数は命中からの経過時間です。Core Duration / Stroke Durationは「保持後に加算する時間」ではなく、命中から消失までの総時間です。

| Inspector項目 | First 変更前 → 後 | Second 変更前 → 後 |
|---|---:|---:|
| Stroke Length (m) | 1.2 → 1.4 | 2.05（維持） |
| Stroke Width (m) | 0.068（維持） | 0.135（維持） |
| Stroke Core Hold | 旧Shader固定0.010 → 0.035 | 旧Shader固定0.010 → 0.155 |
| Stroke Core Duration | 0.065 → 0.105 | 0.090 → 0.205 |
| Stroke Peak Duration | 旧計算0.040 → 0.050 | 旧計算0.044 → 0.065 |
| Stroke Duration | 0.130 → 0.185 | 0.210 → 0.275 |
| Stroke Intensity | 1.1（維持） | 1.1（維持） |
| Stroke Min Projection | 追加：0.55 | 追加：0.55 |
| Stroke Max Projection Angle | 追加：20度 | 追加：20度 |
| Material Core Intensity | 3.05（維持） | 2.4 → 2.8 |
| Material Front Intensity | 1.08（維持） | 1.2 → 1.3 |
| HitStop（変更禁止） | 0.045（維持） | 0.155（維持） |

1撃目は35msの芯保持、50msまでの前縁ピーク、105msで芯消失、185msで面消失。2撃目は155msの芯保持、65msまでの前縁ピーク、205msで芯消失、275msで面消失です。コア消失後に面だけが残る区間はそれぞれ80ms / 70msで、面自体は従来通り早期から薄く減衰します。全体を同じ強さで長く残す変更ではありません。

青白い色、面の強度、Core Width、先細り形状、前方への展開、後端の速い消失は維持しています。1撃目のMaterialは今回変更していません。2撃目は面を増やさず、芯・前縁だけ補強しました。

## 変更ファイルとGitの区別

今回の製品コード・Asset変更は6ファイルです。

- `Assets/Mock/Scripts/Feedback/SwordTrail.cs`：既存履歴に実測刀先速度を保持し、命中時の表示方向を選択。
- `Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs`：方向選択・投影補正、保持・ピークを既存MPBへ設定。
- `Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs`：First / Second独立の保持・ピーク・投影設定とTooltip。
- `Assets/Mock/Resources/CounterImpactStroke.shader`：コア保持後の減衰と前縁ピークを独立制御。
- `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：上表の両段階設定。
- `Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat`：Core / Front Intensityのみ調整。

作業開始前からFirstStroke.matとFinisherStroke.matが未コミット変更でした。FirstStroke.matは開始時のバイト列と同一です。Secondの既存微調整（SurfaceIntensity 0.56 / CoreWidth 0.031 / FacetContrast 0.65など）も維持しました。既存の `Artifacts/CounterSlashMicroTune/` と `CounterSlashMicroTuneChanges.md` は変更していません。

開始状態と復元用コピーは `Artifacts/CounterSlashDirectionTiming/Baseline/`、今回だけの差分は `Artifacts/CounterSlashDirectionTiming/TaskOnly.patch` です。復元時は開始時コピーと今回差分を参照し、既存の未コミットMaterial調整を含めてGit HEADへ一括リセットしないでください。Configure.csは初期記録用なので再実行するとBefore記録を更新します。

## 検証結果

- Unity付属RoslynでAssembly-CSharpの全ソースをコンパイル：エラー0、既存警告9。
- Unity API：First / SecondのArc / Stroke計4 MaterialのShaderパス、Queue、Passコンパイル成功、Shaderメッセージ0。
- 純粋C#検査：速度の優先・フォールバック、8方向の投影補正、20度上限、画面上の向き維持、54頂点位置、命中点中心、保持・消失の順序を確認。
- Asset API：共有サンプラー1、CFX参照、Blood原点、既存HitStop設定を確認。
- Git差分と開始時ハッシュ比較：対象外のAssets / Packages / ProjectSettingsの開始時ファイルは変更なし。
- 実GameScene、実アニメーションの刀先速度、30fps時の視認性、Bloom / Exposure、遮蔽、見た目の品質は未検証です。

## ユーザーによるGameScene確認

1. Firstを単体で、続いてSecondまでつなげ、まず通常速度で比較してください。Firstが通常Heavyより明確に鋭く見えるか、Secondが停止中に芯を保つかを確認します。
2. 同じ敵・距離・カメラで動画を撮り、通常速度とスロー再生で、刀先移動→接触→直線Stroke→Blood / Poseが一動作に見えるか比較してください。30fpsでもFirstの芯を認識できるか確認します。
3. 敵正面・斜め・側面、明るい背景・暗い背景で両段階を確認してください。大きく上下に走る場合、それが実際の刀先方向なのか、以前のSweep方向なのかを動画で分けて確認します。
4. InspectorのJustAvoidCounterConfigで、First / Second別に `Stroke Max Projection Angle` を20→0へ一時変更して比較してください。0は投影補正無効です。方向の改善・違和感を切り分けたら20へ戻します。Min Projection 0でも補正を無効にできます。
5. ユーザー自身で命中フレームをPauseし、Scene ViewとGame Viewを比較してください。Hierarchyの `Counter Impact Straight Blade` を選択し、Wireframeで敵との交差を確認します。この既存Meshはワールド座標頂点なので、GameObjectのTransform原点を命中位置と見間違えないでください。
6. 同フレームでStrokeのRendererを一時的にOFFにし、Blood / Contact Flashが出る位置と見比べます。弧状Trailも別RendererとしてON/OFF比較できます。切り分け後は元に戻し、停止中の変更をPrefabへ保存しないでください。
7. 位置が浮く場合、First / Second、カメラ角度、敵の種類、接触直前と直後の映像、Wireframeの交差状況を記録してください。中心は既にHitPointなので、根拠のない位置オフセットを足す前にCollider接触点と見た目の表面を確認する必要があります。
8. 通常Light / Heavy、空振り、Pause / 再開、攻撃キャンセル、Retryで既存動作と残留の有無を確認してください。

調整はConfigのCore Hold→Core Duration→Stroke Durationの順で行うと、ピークを長くしすぎず読み取り時間を調整できます。前縁だけ強すぎる場合はMaterialのFront IntensityかConfigのPeak Durationを下げます。面の不透明感を増やす変更は今回行っていません。投影補正の強すぎる回転はMax Projection Angleを下げてください。

表示方向を最大20度しか補正しないため、視線に非常に近い斬撃の短縮を完全には解消しません。特に完全な視線方向で信頼できる投影履歴がない場合と、敵身体による深度遮蔽は、今回の静的検証だけで品質を保証できない残課題です。
