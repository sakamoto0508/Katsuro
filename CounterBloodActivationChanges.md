# AKI-112：Blood子GameObjectの有効化不足を修正

2026-10-18。実Play Mode・実粒子再生・Game Viewの検証はユーザー担当です。CodexはPlay Modeを起動・操作・撮影していません。

## 確認した原因

提供されたログは**SecondのFinisherCFX**について、同一インスタンスのPlay直後と55ms後に3層すべて `active=False / isPlaying=False / particleCount=0` でした。Firstの実行ログではありません。

旧ログの `active` は `ps.gameObject.activeInHierarchy` です。Renderer.enabledとは別で、親階層も含めてGameObjectが有効かを示していました。旧ログだけでは、子自身が無効か、親が無効かまでは分かりませんでした。

今回、Unity Asset APIとPrefab全文から以下を確認しました。

| 項目 | FirstCutCFX | FinisherCFX（Second） |
|---|---|---|
| 保存されたルートactiveSelf | false | false |
| Crimson Threads activeSelf | false | false |
| Dark Blood Drops activeSelf | false | false |
| Late Fine Drops activeSelf | false | false |
| Blood各層の親 | CFXルート直下 | CFXルート直下 |
| playOnAwake | 全てfalse | 全てfalse |
| Particle Stop Action | 全てNone | 全てNone |
| Emission | 全てenabled | 全てenabled |
| CFX内のMonoBehaviour | JustAvoidCounterCutVFXのみ | 同左 |

ルートがfalseなのはPool待機として成立します。しかし現行 `Play()` は、ルートだけを `SetActive(true)` にして子のactiveSelfを変更せず、非アクティブのBloodへ `ParticleSystem.Play(false)` を呼んでいました。ルートの有効化では、activeSelf=falseの子は有効になりません。

**確認した実装不具合は、再生前のBlood子GameObjectの有効化不足です。** 単に遅延BurstのparticleCountが0だったという説明では、子の非アクティブ状態を解消できません。

Firstにも同じ保存状態と共通Play処理があるため、同じ不具合が適用されます。ただしFirstの実GameSceneでの再生失敗ログは今回提供されておらず、実行時のFirstも測定したという意味ではありません。

## Normal / BloodOnlyの比較

Bloodの生成・有効化・Play・Pause・StopはNormalとBloodOnlyで共通です。BloodOnlyの既存制御は旧Cut RendererのforceRenderingOff、Slash等の非Blood描画抑制であり、Bloodの親や子を無効化する処理はありませんでした。

したがって、今回確認した有効化不足はBloodOnlyだけのバグではありません。Normalでも、保存状態が同じ子へ同じPlay処理を実行する場合に生じます。修正もモード分岐の外へ置き、両方へ適用しました。

## 修正内容と処理順序

製品コードの変更は `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs` だけです。

1. CombatFeedbackが従来通り専用Poolのスロットを選択し、診断設定を通知。
2. Play入口の `Acquired` ログ。
3. 既存の `Stop()` で前回の粒子をStopEmittingAndClearし、ルートを非アクティブ化。
4. 既存のTransform・方向・Cut表示用Transformを準備。World Simulation Spaceのため、粒子を再生する前に命中位置へ配置する順序を維持。
5. CFXルートを有効化し、`RootEnabled` ログ。
6. **3つのBlood子GameObjectを明示的に有効化**し、`BloodEnabled` ログ。
7. 従来通り各ParticleSystemへ `Play(false)` を実行し、`Play` ログ。
8. 既存Tickで寿命・Pauseを管理。50 / 100 / 200msの診断ログを採取。
9. 既存の寿命経過後にStopEmittingAndClearし、ルートを無効化してPool待機へ戻る。

確認した両PrefabではBloodは全てルート直下なので、中間親を推測して有効化する処理は不要です。playOnAwake=false、独自OnEnable処理なし、StopAction=Noneも確認したうえで追加しました。旧Cut GameObjectや敵・プレイヤーの階層全体を有効化する変更ではありません。

Pool待機時、Blood子のactiveSelfはtrueのままでも、ルートがfalseなのでactiveInHierarchyはfalseになります。次回のPlayは同じ共通経路で有効化します。子を毎回falseへ戻す必要はありません。

PrefabそのもののActive値は保存変更していません。既存インスタンスでもPlay時に正しい状態を整えるようにしました。

## Play / Stop / Clear / SetActiveの呼び出し元

| 呼び出し元 | 処理 |
|---|---|
| CombatFeedback.CreateCounterPool | 初期化時に固定4スロットを作成し、各CFX.Stopで待機へ。命中時のInstantiateではない |
| CombatFeedback.Hit | First / Secondの既存Poolを選択し、CFX.Play |
| JustAvoidCounterCutVFX.Play | 再利用前のStop、Transform準備、root.SetActive(true)、Blood子.SetActive(true)、各ps.Play(false) |
| JustAvoidCounterCutVFX.Tick | Pause時ps.Pause(false)、再開時ps.Play(false)、寿命経過時CFX.Stop |
| JustAvoidCounterCutVFX.Stop | 各ps.Stop(false, StopEmittingAndClear)、root.SetActive(false) |
| CombatFeedback.OnDisable | 保有する両段階のCFX.Stop |

独立した `ps.Clear()` 呼び出しはなく、ClearはStopEmittingAndClearに含まれます。CFX内には他の停止スクリプトやParticle Stop Actionによる無効化設定はありませんでした。

StopにCallerMemberNameを付け、診断ログへ `caller=Play / Tick / OnDisable / CreateCounterPool` 等を渡すようにしました。呼び出し元名はコンパイル時の文字列で、通常時にStackTraceを生成する処理はありません。初期Pool作成時は診断Configがまだ設定されていないため、通常その準備ログは出ません。コード上の呼び出し元は上表です。

## 新しいログ

Inspectorの共通ConfigをBloodOnlyにし、Consoleで `[CounterBlood]` を検索してください。Normalでは診断ログは出ません。

通常の命中では次の順に記録されます。

1. `Request`：既存のCombatFeedbackログ。段階、Prefab、専用Pool有無。
2. `Acquired`：取得した同一インスタンスの再生前状態。
3. `StopClearAndDeactivate caller=Play`：**前回分のリセットとして正常なStop**。
4. `RootEnabled`：ルート有効化後。
5. `BloodEnabled`：Blood子有効化後。
6. `Play`：ParticleSystem.Play直後。
7. `After50ms`。
8. `After100ms`。
9. `After200ms`。
10. `StopClearAndDeactivate caller=Tick`：既存寿命による返却。Firstは0.43秒、Secondは0.60秒。

旧After180msは50 / 100 / 200msの採取へ置き換えました。フレーム間隔により採取は閾値より後になるため、実際の `age=` を確認してください。大きくフレームが飛んだ場合は複数の閾値ログが同じTickで出ることがありますが、経過時間ごとの採取は各1回で、毎フレーム出力ではありません。

各状態ログは次を含みます。

```text
stage=First または Second
instance=... instanceId=...
caller=...
root.activeSelf=...
root.activeInHierarchy=...

Crimson Threads / Dark Blood Drops / Late Fine Drops:
blood.activeSelf=...
blood.activeInHierarchy=...
isPlaying=...
isEmitting=...
isPaused=...
particleCount=...
emission.enabled=...
renderer=...   ← ParticleSystemRenderer.enabled
forceOff=...
material=...
shader=...
supported=...
cameraLayerVisible=...
```

位置・最終飛散方向・Camera / Viewportも維持しています。`Acquired` と再利用前のStopのageは前回サイクルの開始時刻を基準とする値です。今回サイクルのageは `RootEnabled` 以降を参照してください。FirstとSecondは異なるPoolなので、それぞれstageとinstanceIdを組にして追跡します。

`isEmitting=false` だけで失敗と断定しないでください。一回のBurstと遅延の構成なので、放出を終えた後でも粒子が生存している場合があります。Play直後のparticleCount=0もあり得ます。後続ログとActive / isPlaying / countを合わせて判断してください。

## 実施した検証

- Unity Asset APIで両PrefabのActive値、親子関係、Emission、playOnAwake、StopAction、スクリプト構成を確認。
- 保存しないPrefab編集用コピーで、旧手順「ルートのみ有効化」では3つのBloodが非アクティブのままとなることを再現。
- 実際に追加した共通有効化メソッドを呼び、First / Second × Normal / BloodOnlyの4条件で各子のactiveSelf / activeInHierarchyがtrueになることを確認。
- 同コピーでルートを無効化→再有効化し、再利用時も有効化されることを確認。
- この検証では **ParticleSystem.Play / Emit / Simulateを一度も呼んでいません**。粒子数0・isPlaying=falseであることを併せて確認しており、実際の再生成功テストとは扱っていません。SceneやPrefab Assetへの保存も行っていません。
- Editor / Development / Release相当の3定義セットで全C#コンパイル成功。全てエラー0、既存警告9。
- Blood ShaderのPassコンパイル成功、エラーなし。
- 開始時ハッシュ比較で、製品側の変更がJustAvoidCounterCutVFX.csだけであることを確認。BloodのPrefab / Shader / Material / 色 / サイズ / 速度 / 数、他の戦闘機能は不変です。

実際のPlay Modeでの放出・描画・寿命返却・Pause / Retryは未検証です。有効化不足は確認・修正できましたが、実GameSceneで全て正常になったと保証する段階ではありません。

記録は `Artifacts/CounterBloodActivation/` に保存しています。PrefabInspection.json、ActivationVerification.json、各Compile.log、DiffVerification.jsonを参照してください。Baselineに開始時の差分とソース、TaskOnly.patchに今回だけの差分があります。前回までの未コミットBlood改修とBloodOnly実装は保持しました。

## ユーザーが取得するログと検証手順

1. 既存Playを停止し、コードのコンパイル完了を確認。共通JustAvoidCounterConfigのVFX Debug ModeをBloodOnlyへ設定して再Playします。
2. まずFirstを命中させ、FirstのRequestからAfter200ms、返却Stopまでを取得してください。
3. 次にSecondを命中させ、Secondの同じログ列を取得してください。今回提供されたSecondの失敗ログと、段階を取り違えず比較します。
4. `RootEnabled` でルートがtrue、`BloodEnabled` で3層のblood.activeSelf / activeInHierarchyがtrueとなることを確認します。
5. Playと50 / 100 / 200msでisPlaying・isEmitting・particleCountを確認。小滴の開始遅延はFirst 55–80ms、Second 95–135msなので、50msで小滴0でも異常確定ではありません。
6. 寿命前にStopが出る場合はcallerを確認。`caller=Play` は次の再利用前リセット、`caller=Tick` は寿命、`caller=OnDisable` は保有者の無効化です。複数命中のログが混ざるため、必ずinstanceIdで追跡します。
7. 数回繰り返してPool再利用も確認。Pause / 再開、Retry後にも確認してください。
8. Playを停止しNormalへ戻して再Play。Normalでは診断ログを出しませんが、Bloodは同じ修正済みの共通経路で再生されます。First / Secondの両方を映像で比較してください。

両段階が有効で後続particleCountも正なら、初めて次段階として生成位置・投影方向・Alpha・Shader合成・深度・白黒演出・Slashとの競合を調査します。今回、それらの数値や見た目は変更していません。
