# 追撃2連撃：命中ピーク・Blood主筋・被弾後の崩れ

2026-10-17。開始HEADは `6c451db`、作業開始時のGit作業ツリーはクリーンでした。Play Modeの起動・操作・撮影は行っていません。以下は実装と静的検証の結果であり、実Game Viewでの改善を保証するものではありません。

## 調査で確認できたこと

- `EnemyController.ApplyDamage` は、生存している追撃命中で既存の専用Reactionを開始し、その後 `CombatFeedback.Hit` がSlash / Blood / SE / HitStopを同じ命中通知から開始します。ダメージやイベントの経路を重複追加する必要はありませんでした。致死時は専用Reactionを要求せず既存Death処理を優先する構造です。
- Strokeの前縁には短いピークがありますが、中央コアには命中直後だけ上乗せするピークがありませんでした。コア保持中の強さがほぼ一定で、命中直後と停止中の差が弱い構成でした。
- Bloodは既にWorld空間・非ループ・unscaled時間・固定Poolです。主筋は命中から12ms / 18ms遅れ、小滴も25ms / 55msに発生する設定で、主筋と小滴の時間差が狭い状態でした。
- Enemy Prefabは共通Configを参照し、HitReaction LayerにはLight / Heavy各4方向Stateがあります。全Stateが実Clipを参照し、Body / Head Maskは有効、Root Maskは無効です。上位の通常Heavy Layerは追撃開始時に重みを解除する構造です。
- 専用Animationは命中時にnormalized time 0.05を直接評価し、FirstはLayer weight 0.42、Secondはimpact weight 0.85から開始します。Clipの胸・頭のカーブには変化があり、未設定・空Clipが原因ではありません。実際のState再生・Blend結果は未検証です。
- 通常用 `EnemyBoneHitReaction` と追撃用 `CounterHitPose` は排他制御されています。今回、通常側の処理は変更していません。
- 追撃の骨補正は従来、胸・頭・腰が同じ時間進行で追従し、最大崩れに達した直後から復帰していました。最大姿勢を短く読む区間がありませんでした。

斬撃位置については、既存Stroke中心・Blood原点が同じHitPointであるため変更していません。実Game Viewでの不満の全原因を確定したわけではなく、上記の構成を改善する調整です。

## Slash：中央コアだけに命中ピーク

Configに `Stroke Hit Peak` を追加し、既存MaterialPropertyBlockでShaderへ渡します。1で追加ピーク無効です。中央の細いコアにだけ倍率を掛け、青銀の面・色・幅・長さには掛けません。

最初の約33.3msはピーク倍率を保ち、その後は既存 `Stroke Peak Duration` まで時間ベースで通常のコア強度へ戻します。フレームカウンターやフレーム数による延長はありません。コア保持・消失・残光の既存時間は維持しました。30fpsでピークを取得できる時間幅は確保しましたが、実際の視認性はユーザー確認が必要です。

| Config項目 | First 変更前 → 後 | Second 変更前 → 後 |
|---|---:|---:|
| Stroke Hit Peak | 追加：1.65倍 | 追加：2.05倍 |
| Stroke Peak Duration | 0.050 → 0.060秒 | 0.065 → 0.080秒 |
| Stroke Core Hold | 0.035秒（維持） | 0.155秒（維持） |
| Stroke Core Duration | 0.105秒（維持） | 0.205秒（維持） |
| Stroke Duration | 0.185秒（維持） | 0.275秒（維持） |
| HitStop | 0.045秒（維持） | 0.155秒（維持） |

ShaderのBlend One One、ColorMask RGB、Alpha 0、ZTest LEqual、Queue 3011は維持しています。Material値は全て変更していません。旧Cut Quadは既存設定通り非表示で、既存Contact Flashを使います。

## Blood：少数の主筋を速く出し、小滴を遅らせる

既存の `Crimson Threads` と `Late Fine Drops` のみ調整しました。`Dark Blood Drops` の設定、深紅の色、Blood Shader / Materialは維持しています。

| Prefab項目 | First 変更前 → 後 | Second 変更前 → 後 |
|---|---:|---:|
| 主筋 Start Speed | 5–6.2 → 6.4–7.6 | 6.3–7.8 → 8.2–9.6 |
| 主筋 Start Size | 0.065–0.090 → 0.070–0.095 | 0.085–0.120 → 0.100–0.135 |
| 主筋 Shape Angle | 14 → 10度 | 18 → 14度 |
| 主筋 Burst Count | 6 → 5 | 9 → 7 |
| 主筋 Renderer Length Scale | 7 → 7.5 | 8 → 9 |
| 主筋 Start Delay | 固定0.012 → 0秒 | 固定0.018 → 0秒 |
| 主筋 Start Lifetime | 0.28–0.36 → 0.23–0.30秒 | 0.34–0.45 → 0.30–0.38秒 |
| Late Fine Drops Start Delay | 固定0.025 → 0.035–0.050秒 | 固定0.055 → 0.065–0.090秒 |
| Blood Outward Bias | 追加：0.22 | 追加：0.30 |

主筋を細い広角の束から、少数で少し太く長い方向性のある筋へ寄せました。速度を上げた分、寿命は短くして、遠くへ飛び続ける余韻を抑えています。粒子数は増やしていません。

`Blood Outward Bias` は、斬撃方向を基準とし、攻撃者→接触点方向のうち斬撃軸に直交する成分だけを少量加えます。既存Player命中経路のHitNormalがこの方向です。表示原点・DamageInfo・物理方向を変更せず、血の軸だけを攻撃者から離れる側へ最大約12.4度 / 16.7度傾けます。0で従来方向です。斬撃軸と攻撃者方向が平行な場合は補正しません。Player衣装との重なりを全カメラ角度で解消する保証はありません。

## HitReaction：胸が先行し、崩れを短く保持して復帰

専用Clip、AnimatorController、Layer構成、命中Pose位置、SecondのLayer重み、Root / AI / NavMesh / 行動拘束時間は変更していません。

既存 `CounterHitPose` の時間曲線を調整しました。胸が先行し、頭がPeakの12%、腰が20%だけ遅れて追従します。`Follow Through Hold` を追加し、崩れの最大姿勢を短く保ってから復帰します。各骨は遅延があっても同じPose Durationまでに補正が0になります。

| Config項目 | First 変更前 → 後 | Second 変更前 → 後 |
|---|---:|---:|
| First Reaction Weight | 0.42 → 0.58 | 対象外 |
| Pose Duration | 0.18 → 0.22秒 | 0.40秒（維持） |
| Torso Follow Through | 4 → 7度 | 24度（維持） |
| Follow Through Peak | 0.070 → 0.055秒 | 0.140 → 0.095秒 |
| Follow Through Hold | 追加：0.018秒 | 追加：0.035秒 |

Secondの回転角を増やして崩れを作る変更ではありません。既存の24度の胸補正に早く到達し、35ms保持してから復帰します。FirstもLayer重みと短い崩れを補強し、単体での切断感を狙っています。胸の角度は各骨へ分配される設定値で、実際の画面上の角度ではありません。

HitReaction時計は既存のAnimator速度を掛けた時間です。HitStop中は命中Poseと初期骨補正が止まり、解除後に崩れが進みます。SlashとBloodは既存のVFX時計 / unscaled時間でHitStop中も進み、ゲームPauseでは既存処理で停止します。同期のために全演出の時計を一つへ作り直してはいません。

## 命中から復帰までの役割

1. 既存SwordTrailが刀の軌跡を表示。
2. 既存の受理済み命中通知で、専用Reactionを開始し、Strokeピーク・遅延0のBlood主筋・既存SE・既存HitStopを開始。
3. 33.3ms以降に追加ピークが急速に減衰。Bloodは停止中も飛散し、細滴はFirst 35–50ms / Second 65–90ms遅れて発生。
4. HitStop解除後、骨反応が胸→頭・腰の順で既存の崩れへ移る。コアは既存消失時刻までに消え、青銀の面と血滴が残る。
5. 専用Pose Duration内に骨補正を復帰。ゲームプレイ上のReaction終了条件や通常攻撃の行動中断条件は従来のまま。

順序は既存の同じ命中コールスタックを利用しています。別Coroutine、追加命中イベント、毎攻撃のInstantiateは追加していません。

## 変更ファイル

- `Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs`：命中コア倍率・崩れ保持の2設定を追加。
- `Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs`：既存MPBへ命中ピークを設定。
- `Assets/Mock/Resources/CounterImpactStroke.shader`：中央コアの時間ピーク。
- `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs`：Blood軸の小さな外向き補正。
- `Assets/Mock/Scripts/Feedback/CounterHitPose.cs`：胸・頭・腰の時間差、短い保持と復帰。
- `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：両段階の設定。
- `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab`：Firstの主筋・細滴・Blood軸設定。
- `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`：Secondの主筋・細滴・Blood軸設定。

SwordTrail、CombatFeedback、EnemyController、EnemyAnimationController、EnemyBoneHitReaction、PlayerAttacker、PlayerWeapon、Counter Animation / SpeedState、DamageInfo、Material、Animation Clip / Event、AnimatorControllerは読み取り調査のみです。

今回の記録は `Artifacts/CounterImpactCohesion/` にあります。Baselineに開始時のGit状態・SHA・ソースとPrefab / Configコピー、Inventory.jsonに変更前Blood / Animator参照、ConfigAfter.jsonに変更後Config、TaskOnly.patchに今回差分を保存しました。既存の記録ファイルは変更していません。

## Inspector調整

- Config First / Second：`Stroke Hit Peak`（追加コア倍率）、`Stroke Peak Duration`（追加ピークと前縁の終了）、既存Core Hold / Core Duration / Stroke Duration。白飛び時は倍率から下げ、面の強度を増やして補わないでください。
- CFX Prefabの `Crimson Threads`：Main Start Speed / Start Size / Start Lifetime、Shape Angle、Renderer Length Scale。過剰な飛散はSpeedかLifetime、太い塊になる場合はSizeかLength Scaleを下げます。
- `Late Fine Drops`：Main Start Delay。Secondの崩れとのつながりは65–90msを基準に比較してください。
- CFXルート：`Blood Outward Bias`。血が刀の方向からずれる場合は0へ下げて比較します。
- Config：`First Reaction Weight`、First / Secondの `Torso Follow Through`、`Follow Through Peak`、`Follow Through Hold`、`Pose Duration`。Peakが立ち上がり、Holdが最大姿勢、Pose Durationが復帰までの時間です。行動拘束時間を変更する設定ではありません。

## 検証と手動確認

実施：Unity付属RoslynでAssembly-CSharp全ソースをコンパイル（エラー0、既存警告9）、5 MaterialのShader Passコンパイル（メッセージ0）、606点の骨反応時間曲線と12方向のBlood軸検査、Prefab参照・World空間・unscaled時間・非ループ・発生原点・Pool寿命内の粒子寿命を確認。Git差分の対象外ファイル・全Materialは開始時と同一です。

未実施：実Scene、Animatorの実行時State / Layer weight、リターゲット後の姿勢、足の接地、実際の発光 / Bloodの見た目、30fpsでの視認性。Clipのカーブ調査はPose品質の実測ではありません。

ユーザーによる確認手順：

1. 同じ敵・距離・カメラでFirst単体と2連撃を録画し、通常速度とスロー再生で比較してください。刀→芯のピーク→主筋→Poseが同じ接触に見えるかを確認します。
2. 30fpsでFirstのピークが読めるか、Secondは155msの停止中にコアと被弾Poseが読めるかを確認してください。明るい背景と暗い背景の両方で、青銀の面より白銀コアが主役か確認します。
3. InspectorでStroke Hit Peakを1へ戻して追加ピークだけを比較してください。First / Second別に行います。元値は1.65 / 2.05です。
4. 血が敵シルエットから出るか、赤いPlayer衣装へ重ならないか、主筋が太い塊や遠距離の噴射に見えないかを、正面・側面・斜めで確認してください。位置は動かしていません。
5. 非致死のFirstとSecondを確認し、HitStop中のPose保持→解除後の胸・頭・腰の崩れ→復帰を比較してください。Firstは次の斬撃につながるか、Secondは足が滑る・腰が不自然にねじれる問題がないかを確認します。
6. Reactionが弱い / 再生されない場合は、ユーザー自身でPauseし、AnimatorのHitReaction LayerのState・weight、NormalHeavyHit Layerが0か、Counter Config参照を確認してください。今回の静的調査だけでは実行時のState再生を証明できません。
7. 通常Light / Heavyが敵攻撃を新たに中断しないか、連打による永久拘束がないか、致死追撃でDeathを優先するか、Pause / 再開 / Retry / 連続命中で血や骨補正が残らないかを確認してください。

Clip自体の重心移動が足りない場合、今回の限定的な骨補正だけでは解決しません。実動画で不足が確認できた段階で、既存Clipの胸・腰・足の連動をAnimation側で調整する必要があります。根拠なく回転角を追加して補う方針にはしていません。
