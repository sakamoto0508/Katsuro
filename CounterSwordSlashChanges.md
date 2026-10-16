# Katsuro：刀の軌道に追従する白銀Slash（2026-10-17）

追撃専用Slashの実装・Asset設定・コンパイル・静的検証を実施した。**実Sceneでの見た目は未検証。Play Modeは起動・操作・撮影していない。** ユーザーによるGameScene確認と調整が必要。

## Cut Quadで目標に届きにくかった理由

旧Cutは命中座標に置くカメラ向きの1枚のQuadで、刀が通った場所の履歴を持たない。形状や発光を強めても、実際の振りに連続する弧にはならなかった。RGBと独立したAlpha遮蔽もあり、弱い発光領域で背景を暗くする可能性がある。

既存SwordTrailには実軌道の履歴が既にあるが、Playerの `_bladeTrailStart=.58` により刀身の外側42%を使い、旧Shaderでも切先付近の狭い帯へ絞っていた。Configの実際のCore DurationはFirst=.07秒、Second=.085秒。30fpsでは約2〜3フレームであり、頂点色の減衰とShaderの減衰が重なり、SecondのHitStop=.155秒より早く弱まる構成だった。SwordTrailに残る互換値 `.026秒` は、現在のConfig接続時には上書きされている。

## 再利用した仕組みとGeometry

- Playerのcollider1が代表SwordTrail、collider2〜5が `_sharedTrail` で共有する構成を維持。
- `_bladeBase=TrailBladeBase` / `_tip=TrailBladeTip` の位置履歴、40サンプルの配列、既存Mesh、頂点・UV・色・Indexバッファ、3分割補間を再利用。
- 追撃だけ、根元側の使用範囲を広げ、刀身軸に沿って表示上の切先を少し延長。ダメージ判定や刀Transformは変更しない。
- 根元位置は既存Catmull-Rom補間、追撃の刀身方向は既存2サンプル間のSlerpで補間する。低fpsで2サンプルしかなくても回転を弧として補う。反対向き・退化した刀身は直線補間へ戻す。
- 各時刻の内外2頂点を時間方向につないだRibbon。切先側を基準として根元側へ幅を取り、時間方向の両端はGeometryとShader双方で先細りにする。前方に独立した面を飛ばす処理はない。
- 頂点数は最大236、三角形は最大234。新たなサンプリングループ、別の履歴、毎攻撃のMesh生成は追加していない。

通常Light/Heavyは既存 `BladeTrail.shader` / `SwordTrailSteel.mat` を継続使用する。Player Prefabの既存値も変更していない。親Animator参照の解決だけは非アクティブ階層を含めるようにし、非表示の武器から初期化されても追撃Stateを取得できるようにした。

## 時間・命中・終了処理

SwordTrailが既存 `JustAvoidCounterAnimation.Strike` を読み、追撃State中の表示だけを開始・終了する。これによりVFXのサンプリングは振り始めから行える。低速時は既存の速度閾値で抑制する。**Hitboxの有効時間とAnimation Eventは変更していない。**

命中受理後のCombatFeedbackから、既存の共有Trailへ表示通知を1回渡す。`CounterSlashPresentation` が命中点を記憶し、局所的な強調と弧全体の短い輝度ピークを与える。

`FeedbackVisualTime` を維持し、追撃の命中時だけ、既存HitStop値を読み取った期間分、サンプル時刻をずらす。これにより生成済みのGeometryと寿命が保持され、止まった刀を繰り返しサンプリングしない。コアの表示年齢も命中で更新し、停止解除後にコア、その後に銀の面と残光が消える。実際のAnimatorやHitStopManagerには介入しない。

Pause中は既存時計とMesh更新を止める。DisableでMeshと命中状態を消し、Destroyで表示リソースを解放する。Victory/Defeat/Titleへの移行では追撃表示だけを消す。通常攻撃への切り替えでMaterialと履歴を戻す。これらの実ゲームでの遷移・Retry確認は未実施。

## 専用Shader

`Assets/Mock/Resources/CounterSwordSlash.shader` を追加。

- UV0：サンプルの寿命比と刀身方向。
- UV1：速度由来の強度と、古い側から新しい側への進行位置。
- UV2：コア強度、残光強度、コア年齢。
- 頂点色：既存速度判定と段階別の明るさを反映。旧Shader用の二重減衰は追撃経路で使わない。
- 白銀コア、切先側の明るいエッジ、幅を持った青白い銀の面、時間差の残光を別分布で描く。
- 接触点からの距離で局所ピークを強め、弧全体にも控えめなピークを与える。
- Gaussianの二乗は `x*x` / `dot(x,x)` を使用し、符号付き座標へのpowを避ける。

**Blend One One / ColorMask RGB / Alpha=0 / ZWrite Off / ZTest LEqual / Cull Off**。RGBを加えるだけで背景を減算しないため、RGB=0の領域がAlphaだけで黒い板を作る合成にはならない。これは発光専用の加算表現であり、濃い不透明な銀板を重ねる方式ではない。

HDRP ForwardOnly / Transparent+10（Queue3010）の線形HDR出力。Scene Exposure Influenceは0を初期値とし、既存SwordTrailと同様に視認性を保つ露出補償VFXとして出力する。1にするとHDRPの現在Exposure倍率を発光へ適用する。カメラのBloom、Exposure、トーンマッピングは変更していない。

## First / Secondの初期値

| 項目 | First | Second |
|---|---:|---:|
| Blade Start | .28 | .12 |
| Tip Extension（刀身長比） | .06 | .14 |
| Trail Width | .95 | 1.15 |
| Trail Duration（サンプルごと、停止保持を除く上限秒） | .23 | .36 |
| Trail Brightness | 1.3 | 1.6 |
| Core Intensity | 2.2 | 3.6 |
| Core Duration（秒） | .095 | .18 |
| Afterglow | .4 | .6 |
| Material Core Width | .11 | .16 |
| Material Sheet Intensity | .7 | 1.15 |
| Material Bright Edge Intensity | 2.4 | 3.1 |
| HitStop（変更なし） | .045秒 | .155秒 |

Firstは狭く速い弧、Secondは刀身の広い範囲を使った強い弧となる設定。初期値であり、実際の画面上の迫力・白飛び・姿勢との共存は未評価。

## 既存CFX・Bloodとの分担

両追撃CFX Prefabの `Use Local Contact Flash` を有効化した。旧Cut MeshRendererを再生時に非表示にし、CombatFeedbackの既存ContactPulseを再利用する。大きな白いQuadを新しいSlashと重ねない。

Bloodは同じPrefab内の同じ3つのParticleSystemを同じPoolから再生する。両Prefabを開始時SHA256と照合し、このフラグ1行を除く全内容が一致することを確認した。旧CutのShader/Material/参照は保持。HitPoint、血の方向・色・個数・寿命、Enemy HitReactionには変更なし。

## Inspectorで調整する場所

`Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset` のFirst / Second：

- **Blade Start**を下げると根元側から広い面を使う。
- **Trail Width**を上げると切先を基準に面が広がる。まず少量ずつ変更する。
- **Tip Extension**は刀身軸方向の表示延長。飛び道具化を避けるため過度に上げない。
- **Core Duration / Trail Duration**で芯と残光の時間差を調整。
- **Core Intensity / Trail Brightness / Afterglow**で段階ごとの強度を調整。
- **Slash Material**で段階ごとの専用Materialを指定。

`Assets/Mock/Effects/JustAvoidCounterFirstSlash.mat` / `JustAvoidCounterFinisherSlash.mat`：

- **Core Width / Core Position**：刀身方向における芯の太さと位置。
- **Silver Surface Intensity / Surface Edge Softness**：銀の面の強さと根元側の柔らかさ。
- **Bright Blade Edge Intensity / Width**：外縁の鋭さ。
- **Directional Afterglow Intensity**：コアが消えた後の銀色の余韻。
- **Short Impact Brightness Boost / Local Contact Boost Radius**：命中ピークと局所範囲。
- **Overall Slash Intensity**：最後の全体調整。
- **Scene Exposure Influence**：0は露出補償、1はScene Exposureに追従。

さらにバッサリ感を出す場合は、まずSecondのBlade Startを下げるかTrail Widthを少し上げ、面の形状を確認する。次にCore Width / Silver Surface Intensityを調整する。白飛びした場合はOverall Slash Intensity / Impact Boost / Bright Edge Intensityを下げる。姿勢を覆う場合はTrail Widthを下げ、Blade Startを上げる。長く残る場合はTrail Duration / Afterglowを下げる。HitStopやカメラの値を代用しない。

## ユーザーによるGameSceneテスト

1. 通常Light/Heavyを出し、従来の細いSwordTrailが使われることを確認。
2. 追撃1撃目のみと、2撃目までの両方を確認。振り始めから刀に沿って伸び、ワールドに残した軌道が刀と一緒に移動しないことを確認。
3. 命中しない追撃でも弧が出ること、命中時だけ接触光と輝度ピークが強まることを確認。
4. SecondのHitStop中に生成済みの弧が残り、解除後に芯→銀の面→残光の順で消えることを確認。
5. 明るい背景／暗い背景、正面／側面／奥行き方向の振りで確認。白飛び、背後への深度隠れ、細い角度で面が見えづらい場合を記録。
6. Player/Enemyの全身を白帯で覆わないか、旧Cut Quadの大きな板が出ないか、Bloodと被弾Poseが見えるかを確認。
7. 30fpsと通常fpsで比較し、弧の欠け・カクつき・HitStop明けの飛びを確認。40サンプルの上限があり、高fpsでは記録できる時間幅が短くなる場合がある。
8. 1→2撃目、直後の通常攻撃、空振り、中断、Pause/Resume、敗北・Retry・Scene切り替えで残留を確認。
9. ProfilerのGC AllocとMesh更新負荷を確認。静的には毎フレーム／毎攻撃のnew配列・new Material・Instantiateを追加していないが、実機Profilerは未実施。

Ribbonは実刀身が作る面なので、カメラと面が平行になると細く見える場合がある。カメラ向きに面を捏造する補正は入れていない。加算発光は明るい背景ではコントラストが下がり、重なりでは白飛びし得る。これらは実画面での調整項目。

## 変更ファイルと検証

既存ファイルの変更は7個：

- `Assets/Mock/Scripts/Feedback/SwordTrail.cs`：既存履歴の追撃用Geometry、State表示窓、専用表示との接続。
- `Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs`：Material参照、刀身範囲、延長、調整範囲とTooltip。
- `Assets/Mock/Scripts/Feedback/CombatFeedback.cs`：追撃命中の表示通知と既存ContactPulse再利用。
- `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs`：旧Quad抑制フラグ。
- `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：First/Secondの専用設定。
- `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab` と `JustAvoidCounterFinisherCFX.prefab`：上記フラグのみ。

新規Assetは `CounterSlashPresentation.cs`、`CounterSwordSlash.shader`、First/FinisherのSlash Material 2個と、それぞれのmeta。

ゲームのC#全体をUnity同梱Roslynと既存のUnity参照でコンパイル成功。既存の非推奨API／未使用フィールド警告9件、エラーなし。両MaterialのShader Passを現在のDirect3D12環境でコンパイル成功、Shader診断メッセージ0件。

SceneやGameObjectを作らない純粋なC#計算テストで、履歴時刻保持、保持期限、コアの復帰、両端テーパー、回転弧、反対向きの補間フォールバックを確認。2〜40サンプルのGeometry分割で2,379点のIndex/テーパー範囲を確認。

開始時から存在する未コミット変更を保持し、Assets/Packages/ProjectSettings内の全開始時ファイルをSHA256で照合した。今回の変更7個以外は一致。Player Prefab、Animator、通常Trail Shader/Material、旧Cut Shader/Material、Blood関連Assetを変更していない。ConfigのHitStop・HitReaction/Pose関連フィールドも開始時と一致。

記録：`Artifacts/CounterSwordSlash/EditorVerification.json`、`DiffVerification.json`、`Compile.log`、`TaskOnly.patch`、開始時HashとGit Status。これらはAssets外で、実戦再現用の診断コードではない。**実行時の見た目・GC Alloc・描画負荷・各遷移の最終評価は未検証。**
