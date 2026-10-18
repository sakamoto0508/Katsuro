# AKI-112：BloodOnly検証モード

2026-10-18。今回はBloodの外観調整をしていません。Play Modeの起動・入力・撮影・戦闘再現は未実施です。

## 使い方

1. Projectで `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset` を選択します。
2. Inspectorの **Counter VFX diagnostics (Editor / Development Build only) > VFX Debug Mode** を **BloodOnly** にします。初期値はNormalです。
3. ユーザー自身でGameSceneをPlayし、ジャスト回避追撃のFirst、続いてSecondを命中させます。
4. Consoleの検索欄に **[CounterBlood]** を入れます。CollapseをOFFにすると各段階の記録を比較しやすくなります。
5. 比較後はPlayを停止し、同じ設定を **Normal** へ戻して再Playしてください。

設定はPlayerのSwordTrailとCombatFeedbackのVFXConfigが共通参照するConfigに置きました。First / Secondへ同時に適用され、EnemyやPoolインスタンスを個別設定する必要はありません。Shared Configのため、そのConfigを使う追撃全てに適用されます。

実行中の切り替えでは既に生成を抑えたContact Flashを途中から再生成しません。公平な比較と状態の切り分けには、**停止→切り替え→再Play**を使用してください。PrefabやSceneにRendererの手動OFFを保存する必要はありません。

## 表示対象と抑制対象

| 対象 | Normal | BloodOnly |
|---|---|---|
| Crimson Threads / Dark Blood Drops / Late Fine Drops | 従来通り | 従来通り |
| 追撃専用SwordTrailのRibbon Renderer | 従来通り | forceRenderingOff |
| Counter Impact Straight BladeのRenderer | 従来通り | forceRenderingOff |
| CFX内の旧Cut Renderer | 従来設定通り | forceRenderingOff |
| 同じ追撃命中のContact Flash | 従来通り | 再生要求を抑制 |
| 同じ追撃命中のEnemy Normal Hit Overlay | 従来通り | 追加Flash開始を抑制 |
| 混在HitVFXへのフォールバック | 従来通り | 抑制し、専用Pool欠落なら警告 |
| Player / Enemyの本体Mesh | 従来通り | 従来通り |
| HitReaction / HitStop / HitSE / カメラ / UI | 従来通り | 従来通り |
| ジャスト回避の白黒Post Process・成功時の演出 | 従来通り | 従来通り |
| 通常Light / HeavyのVFX | 従来通り | 従来通り |

BloodのParticle Rendererは変更しません。Bladeサンプリング、Mesh更新、命中方向の計算、Strokeの寿命、CFXの寿命とPause処理、Pool返却は従来の経路で動きます。表示を消すためにGameObjectやSwordTrailコンポーネントを無効化する処理はありません。

Normalへの復帰、通常攻撃へのStyle切り替え、Trail Reset、CFX Stopでは診断用の描画抑制を解除するコードを入れています。実際の連続命中・Retryでの復帰はPlay Modeで未検証です。

## ログの読み方

BloodOnly時だけ、追撃命中あたり通常4つのConsole記録を出します。

1. **Request**：命中通知がCombatFeedbackに到達したこと、First / Second、専用Pool有無、Prefab名、HitPoint、入力方向。
2. **Play**：CFXのPlay処理が到達し、既存ParticleSystem.Playを呼んだ直後の状態。
3. **After50ms**：命中から約50ms以降の最初のTickで採取。
4. **After180ms**：命中から約180ms以降の最初のTickで採取。遅延小滴の開始後を確認するための記録です。

後3件は各々1つの複数行ログで、以下を含みます。

- First / Second、Poolインスタンス名とInstance ID、実際の経過秒、ワールド位置、最終Blood飛散方向。
- Main Camera名、発生原点のViewport座標、カメラとの距離。
- 3層それぞれの `isPlaying` / `isPaused` / `particleCount` / GameObjectのactive状態。
- Particle Rendererのenabled / forceRenderingOff。
- Material名 / Shader名 / Shader.isSupported。
- CameraのCulling MaskにParticleのLayerが含まれるか。

Requestの方向は既存のBlood入力方向、Play以降のdirectionは既存Outward Bias適用後のCFXルートforwardです。数値が少し異なるのは既存仕様です。

Play直後にparticleCount=0でも異常確定ではありません。特にDelayed Dropletsは遅延中に0となります。後のログと合わせて判断してください。50 / 180msは時間基準の採取目標で、実際の採取時刻はフレーム間隔に依存するため、ログの `age=` を確認します。ゲームPause中は既存VFX時計が止まり、診断の経過時間も止まります。

Normalではこのログを出しません。StringBuilder・文字列整形・診断用Renderer検索はBloodOnlyの採取時だけです。Coroutineや毎フレームのログは追加していません。診断中のログには割り当てコストがあるため、パフォーマンス評価には使わないでください。

### 判定の目安

- **Requestが出ない**：この命中経路へ到達していないか、実際に参照するConfigがBloodOnlyでない可能性があります。追撃が命中したか、ConsoleのLog表示が有効か、参照先Configを確認してください。
- **RequestでdedicatedPool=false**：専用CFXの参照・Pool準備を確認。BloodOnlyで混在フォールバックを表示して結果を混同しないよう、そのフォールバックは抑制します。
- **Playが出るが後のparticleCountも0**：Emission、遅延、再生停止・Pool返却、ParticleSystem設定を優先調査します。
- **particleCount>0だがRendererがfalse / forceOff=true / MaterialやShaderがNULL / supported=false**：描画経路や参照の問題を優先調査します。今回のBloodOnly実装はBlood RendererをforceOffにしません。
- **particleCount>0で描画設定も有効なのに見えない**：Viewportのz（負ならカメラ後方）、x / y、Camera Layer、形状・Alpha、合成、白黒Post Processとの関係などを調べます。Viewport座標は原点のみで、飛散粒子全体の画面内判定ではありません。
- **BloodOnlyでは見えるがNormalでは見えない**：Slash / Flash / 他の描画効果との重なり、順序が有力な候補です。
- **両方で弱い**：今回のログで発生を確認してから、次工程で形状・サイズ・透明度等を検討してください。

Shader.isSupported=trueや粒子数>0は、実画面に見えることの証明ではありません。コンパイル成功と実描画を分けて判断します。白黒Post Processを無効化するコードはありません。

## 現行Blood描画設定（今回変更なし）

| 設定 | 現行値 |
|---|---|
| Material / Shader | JustAvoidCounterBlood / Katsuro/JustAvoidCounterBlood |
| Render Queue | 3005 |
| Blend | SrcAlpha OneMinusSrcAlpha（straight alpha） |
| ZTest | Material値8：Always |
| ZWrite / Cull | Off / Off |
| Blood Gain / Opacity | 0.4 / 1 |
| Tint | (0.8, 0.35, 1, 1)を既存Particle色へ乗算 |
| Simulation Space / Clock | World / useUnscaledTime |
| Particle Render Mode | 3層ともStretch Billboard |
| Main Length Scale / Velocity Scale | First 7 / 0.045、Second 8 / 0.055 |
| Dark Drops Length Scale / Velocity Scale | First 1.8 / 0.035、Second 2.8 / 0.035 |
| Delayed Drops Length Scale / Velocity Scale | 両段階1.4 / 0.035 |
| Camera Velocity Scale | 全層0 |
| Start Alpha | 主筋・暗赤滴1、小滴0.95 |
| Size over Lifetime | 主筋1→1.12→1→0.48→0.06、滴は1→0.35 |
| Color over Lifetime | 後半でAlphaが0へ減衰。寿命末尾0を確認 |
| 発生位置 | CFXルート=HitPoint、各Emitter / Shapeの位置オフセット0 |

主筋のParticle色は既存の深紅2色、暗赤滴は暗い赤2色、小滴は深紅2色です。色・Alpha値・Mask・Exposureの扱いは維持しました。実カメラとの位置関係は静的Assetだけでは確定できないため、実行時ログへ出します。

旧 `JustAvoidCounterCut.shader` にはSolid Magenta / Core Mask / Sheet Mask / Flash Mask診断がありますが、Blood以外のVFXを隔離する機能ではありません。今回の目的には流用できないため、変更していません。既存のBloodOnly / CutOnly表示切り替えは見つかりませんでした。

## 変更ファイルとGit差分

今回の変更はC#の6ファイルです。

- `Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs`：Editor / Development限定EnumとInspector設定、Releaseでは常にfalseの判定。
- `Assets/Mock/Scripts/Feedback/CombatFeedback.cs`：追撃命中の非Blood VFX抑制、Requestログ、CFXへ診断設定通知。
- `Assets/Mock/Scripts/Feedback/SwordTrail.cs`：追撃時だけ表示抑制を更新。通常Styleでは解除。
- `Assets/Mock/Scripts/Feedback/CounterSlashPresentation.cs`：既存Ribbon / StrokeのforceRenderingOffとReset時復帰。
- `Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs`：既存Stroke Rendererの表示制御。
- `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs`：旧Cutのみ抑制、既存Play / Tickで3回の状態ログ、Stop時復帰。

作業開始前から、前回AKI-112のBlood Shader / Material / 両CFX Prefab / SwordTrail / CombatFeedbackの6ファイルが未コミットでした。既存の `AKI112BloodChanges.md` と `Artifacts/AKI112Blood/` もありました。それらを保持しています。特にBlood Shader / Material / Prefabは今回の開始時ハッシュと同一です。DamageInfo、Hitbox、Animation Event、HitStop、段階識別、HitReaction、カメラ、白黒Post Process、Sceneは変更していません。

開始時の差分とソースコピーは `Artifacts/CounterBloodOnly/Baseline/`、今回だけの差分は `Artifacts/CounterBloodOnly/TaskOnly.patch`、変更範囲確認は `DiffVerification.json` にあります。Git HEADとの差分には前回作業も含まれるため、今回だけの復元やレビューにはTaskOnly.patchを参照してください。

## 実施した確認と未検証項目

- C#：Editor / Development / Release相当の3つの定義セットで全Assembly-CSharpソースをコンパイル。全てエラー0、既存警告9。
- Release相当DLLに診断Enumと `[CounterBlood]` のログ文字列が含まれないことを確認。実際のPlayer Buildは実施していません。ReleaseではConfigにBloodOnlyが保存されていても診断判定はfalseです。
- 一時ScriptableObjectでNormal→BloodOnly→Normalの判定と初期値Normalを確認。実Assetの設定は変更せず、現在Normalです。
- Asset API：PlayerのSwordTrailとVFXConfigが共通Configを参照、両CFXの3層・Renderer・Material / Shader参照が有効であることを確認。
- Blood ShaderのPassコンパイル成功、Shaderメッセージ0。
- 開始時ハッシュ比較でBloodの描画Assetと対象外ファイルが不変、今回の変更が上記C#6ファイルのみであることを確認。

これらの確認では、Bloodが実際に発生しない明確なバグは確定していません。欠落する原因は、ユーザーのPlay Modeで今回のログとBloodOnly表示を組み合わせて切り分ける必要があります。

未検証：実GameSceneでのBloodOnly / Normal表示、実際のparticleCountとログ、Pool再利用時のRenderer復帰、Pause / Retry / 連続命中、通常Light / Heavyの回帰、実機Development / Release Buildです。実際に正常再生・正常表示したとは報告していません。

## ユーザーによる検証A / B / C

**A：First** — BloodOnlyを選びGameSceneをPlay。ジャスト回避後の1撃目を命中させ、位置・太さ・速度・方向を確認。ConsoleのFirstのRequest→Play→After50ms→After180msを保存します。

**B：Second** — 同じモードのまま2撃目を命中させ、主筋・暗赤滴・遅延小滴を確認。HitStop中と解除後を比較し、Secondのログを保存します。

**C：Normal** — Playを停止しNormalへ戻して再Play。同じ敵・距離・カメラ・背景で再現し、BloodOnlyとの違いを比較。Normalでは今回追加したログが出ないことも確認します。

最後に通常Light / HeavyのTrail / Flash / Bloodが維持されるか、停止・再Play後に白銀Slashが戻るか、敵本体・HitReaction・HitStop・白黒演出が維持されるかを確認してください。BloodOnlyでも見えない場合は、まずConsoleログ一式と、その時の通常サイズGame Viewの動画を次の調査材料にしてください。
