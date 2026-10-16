# Play Modeによる追撃CFX検証（2026-10-16）

今回の判定には、Unity 6000.2.7f2 / HDRPで実行したGameSceneのPlay Modeと、実Game Viewの画像だけを使用した。Edit ModeのCFX描画画像は代用していない。

## 実行方法と範囲

- Unityプラグインの `editor_play` / `editor_stop` は実行成功。`simulate_key` は入力送信成功の応答を確認。`capture_game_view` の `source=screen` でHUDを含む実Game Viewの撮影に成功した。
- Editorが背面にある状態で命令がタイムアウトしたため、Computer UseでUnityを前面に戻して復帰した。
- 外部入力だけで短いJust Avoid受付へ安定して合わせることはできなかったため、Editor Pipelineの一時コンパイル診断を使用した。診断ソースは `Artifacts/JustAvoidCounter/PlayMode/CounterPlayValidationFinal2.cs`。Assets外なのでゲームのビルドには含まれない。
- 診断はPlay Mode内で既存Ghost能力・ステートを開始し、その受付中にPlayerController.ApplyDamageへ敵からの診断用入力を渡す。JustAvoidSucceededの発行を記録した。敵の攻撃Colliderを手動操作で回避したという検証ではない。
- 続く追撃は既存PlayerStateMachine、Animator、Animation Event、武器Colliderを動かした。敵のHitReceivedへ届いた呼び出し履歴は `WeaponHitboxRelay.OnTriggerEnter → PlayerAttacker.HandleWeaponHit → EnemyController.ApplyDamage`。敵に直接診断Damageを渡したり、CFX.Playだけを手動再生したり、Clip.SampleAnimationで描いた画像ではない。
- 比較のためGameSceneの実行中インスタンスだけを修練状態にし、配置を揃えた。通常AI・Scene・Prefabのゲームロジック設定は診断用に保存していない。カメラは既存CameraManager / LockOnCameraMoverとMain Cameraを使用した。Near条件だけ実行中のCameraConfig複製で距離2.8m、他は既存4.2m設定。障害物補正も既存処理を通る。
- CutOnlyでは専用Cut＋内蔵Contact Flashを残し、BloodとSwordTrail描画を抑止。BloodOnlyではBloodを残し、Cut＋内蔵FlashとSwordTrail描画を抑止。Allでは全演出。キャラクター、HUD、環境は実Game Viewのまま。

## 最終記録

1280×720のGame Viewで6条件、各条件の1撃目・2撃目、合計12回の実武器命中を記録した。最終画像は156枚。全命中が `counter=True` と `First / Second` になっている。

| 条件 | 実命中数 | 画像数 | 撮影間隔の中央値 |
|---|---:|---:|---:|
| 明るい地面・CutOnly | 2 | 25 | 30.3ms |
| 明るい地面・BloodOnly | 2 | 25 | 30.9ms |
| 明るい地面・All | 2 | 24 | 32.3ms |
| 赤草背景・All | 2 | 26 | 30.2ms |
| 反対方向／影のある地面・All | 2 | 26 | 28.1ms |
| 近距離・All | 2 | 30 | 25.7ms |

2撃目は命中後約100msの画像でもAnimator.speed=0が記録され、その停止中に白銀線と深紅の筋を確認した。明るい地面で全演出を重ねた場合もCutの芯が残り、BloodOnlyで接触点から飛ぶ深紅の筋を分離して確認できた。カメラ方向が異なると画面上の線の傾きは変わる。位置・方向は実命中のHitPoint / SweepDirectionによる。

約30fps前後の実フレーム記録であり、固定60fpsの連続検証ではない。撮影の最大間隔は条件別に31.0～61.6ms。全角度・すべての距離・壁際の組合せは網羅していない。

## Play Modeで発見して修正した点

1. **1撃目の段階がNoneへ消える問題**：通常攻撃のClip長による終了判定が、Just Avoidの振りかぶりから追撃へのBlend中にも働き、EndAttackが段階を消していた。PlayerAttackStateの再生継続条件に既存JustAvoidCounterタグを追加した。コンボ受付時刻、Animation Event時刻、Hitbox時刻、ダメージは変更していない。
2. **Scene終了時のMissingReferenceException**：Scene側が先に破棄したプールCFXへCombatFeedbackがStopを呼んでいた。Tick / StopループへUnity Objectのnull確認を追加した。修正後の診断Scene再読込・Play Mode終了では同じ例外は再発していない。
3. **手前モデルによる遮蔽**：実戦カメラでは接触点とPlayerが重なり、専用CutとBloodが隠れていた。専用Shaderへ `Contact depth test` を追加し、専用3MaterialをAlwaysに設定した。これは短寿命の接触アクセントを前面描画する設定なので、重なるPlayerモデル上にも描画される。通常攻撃のShader / Materialは対象外。
4. **Bloodの小ささ・朱色化・筋の重なり**：実Game Viewを見て、主筋のサイズ・速度・寿命を調整した後、明るすぎる朱色をCrimson luminance=0.4と色設定で深紅へ戻した。主筋は6本／9本へ減らし、細く長い筋が重なりすぎないようにした。

## 保存したCFX設定

| 項目 | 1撃目 | 2撃目 |
|---|---:|---:|
| 主筋の数 | 6 | 9 |
| 主筋の開始サイズ | .065～.09m | .085～.12m |
| 主筋の速度 | 5～6.2m/s | 6.3～7.8m/s |
| 主筋の寿命 | .28～.36秒 | .34～.45秒 |
| 主筋のCone角度 | 14度 | 18度 |
| Stretch Length Scale | 7 | 8 |
| 暗赤血滴サイズ | .06～.09m | .075～.12m |

World Simulation Space、実SweepDirectionへの向き、HitPoint中心、3層の血は維持。Cut形状・長さ・保持時間は前回改善値を維持した。今回のCut側の主な追加は遮蔽設定で、無制限に線を長くしたものではない。Bloodの `Crimson luminance`、Tint、Vein Color、Contact depth testは専用MaterialのInspectorから調整できる。各粒子のShape、Size、Speed、Lifetime、Emission、Rendererは専用Prefabから調整できる。

変更対象：

- Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab
- Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab
- Assets/Mock/Effects/JustAvoidCounterFirstCut.mat
- Assets/Mock/Effects/JustAvoidCounterCut.mat
- Assets/Mock/Effects/JustAvoidCounterBlood.mat
- Assets/Mock/Effects/JustAvoidCounterBlood.shader
- Assets/Mock/Resources/JustAvoidCounterCut.shader
- Assets/Mock/Scripts/Feedback/CombatFeedback.cs
- Assets/Mock/Scripts/Player/StateMachine/States/PlayerAttackState.cs

今回、ConfigのHitStop / Reaction値、Animatorの遷移値、ClipのAnimation Event、通常攻撃、AIの実装、ダメージ式は変更していない。上記以外にも前の作業からの未コミット変更があるため、Git差分全体が今回だけの差分ではない。

## 証拠と保存確認

`Artifacts/JustAvoidCounter/PlayMode/` 内：

- `PlayModeComparison.png`：1撃目／2撃目 × CutOnly／BloodOnly／All。全Game Viewと同一フレームの拡大切り出し。拡大部分は新しいカメラ映像ではない。
- `Backgrounds.png`：背景・距離別の全Game View。
- `PlayModeBeforeAfter.png`：今回の遮蔽・血調整前後を実Game Viewで比較。別実行のため軌道・命中点・撮影時刻は完全一致しない。
- `GameView-Strike1.png` / `GameView-Strike2.png`：全演出の未切り出し画像。
- `Final2/*.png`：実Game Viewの元連続フレーム。DX12のGPU読み戻しの上下向きだけを補正し、色補正・CFX後描きはしていない。
- `Final2/Validation.log`：成功通知、命中段階、実接触のスタック、HitPoint、SweepDirection、撮影時刻、Animator停止状態。
- `Final2/Frames.json` / `CaptureStatistics.json`：フレームの時刻情報。
- `AssetVerification.json`：保存後に再取得したPrefab接続、Particle Material、Simulation Space、Material設定、Shaderエラーなしの結果。

C#コンパイルを通してPlay Modeを再実行し、専用Shaderのエラーは0件。終了後はPlay Modeを停止し、元のGameSceneはdirty=false。Game ViewはFull HDの元の選択へ戻し、追加した720p診断プリセットも削除した。診断用の配置・修練フラグ・近距離カメラ複製は終了とともに破棄された。
