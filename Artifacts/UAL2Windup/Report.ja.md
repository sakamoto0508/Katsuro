# UAL2コンボ：本体速度・初撃予兆の調整

2026-10-18。GameSceneのPlay Modeは起動していません。実際の見切りやすさはユーザーのPlay Mode確認後に判断します。

## 1. 既存のPlaybackSpeed / Windup

既存PlaybackSpeedはEnemyController.PatternからFloatパラメータEnemyComboPlaybackSpeedへ渡され、RegularCombo / HeavyComboのStateだけに適用されていました。実Animatorを0.5秒更新したところ、速度1.0でClip時刻0.500秒、0.85で0.425秒まで進行し、Animator全体のspeedは1のままでした。この仕組みを再利用しました。

以前のWindupはRegular 0.05秒 / Heavy 0.15秒のClip開始前待機でした。刀を構えるアニメーションにはなっていませんでした。

作業開始時にgit status / diffを確認し、既存変更・未追跡ファイルをTemp/UAL2Windup-StartSnapshotへコピーしました。開始時diffも同フォルダに保存しています。今回変更した既存ファイルはPattern、Runner、Controller関連のC#計4ファイルとRegular / HeavyのPatternデータ2ファイルです。

## 2. 追加・変更した処理

敵が向き直り、移動をholdする従来の入口から、次の更新で直ちにCombo Clipを開始します。Clip冒頭の予備動作をゆっくり再生し、指定Clip時刻に達したら同じState内で本体速度へ切り替えます。Idleでの待ち時間は追加していません。

WindupをWindupDurationへ改名し、FormerlySerializedAsで既存シリアライズ値を引き継げるようにしました。同じ意味の待機フィールドは残していません。追加したWindupEndTimeは「予兆に使う冒頭区間の終端Clip時刻」です。予兆速度はWindupEndTime ÷ WindupDurationから自動算出するため、独立したWindupPlaybackSpeedは不要です。

Runnerは予兆中もBusyを保持し、Hitbox開始を受理しません。実AnimatorのClip時刻を観測して本体へ移り、低FPSで初撃Eventまで進んだ場合もEvent自身のClip時刻で本体へ移って初撃を受理します。Clip開始失敗・予兆進行停止にはWindupDuration＋1秒、本体の終了Event欠落には既存Timeout / PlaybackSpeedのwatchdogを使用します。HitStop中はこれらの時間も停止します。

予兆から同じ攻撃StateがRoot Motionを所有するため、冒頭のRoot Motionを二度再生したり、State切替で位置カーブをリセットしたりしません。EnemyMoverと衝突制限のコードは変更していません。

## 3. 採用した予兆アニメーションと理由

専用の別Clipは追加せず、EnemyUAL2RegularCombo / EnemyUAL2HeavyComboの冒頭を採用しました。使用するClip、Animator State、Transition、Event、Root Transform設定は従来のままです。

ARPG SamuraiのHeavy1_Start、Guard_Start、Guard_B_Start、Idle1_to_Idle2、HalberdのHeavy1_Start、UAL2 Sword_Blockを現在のEnemy Avatarで比較しました。攻撃構え・Guard候補の終端からUAL2冒頭への平均骨回転差は約43〜52度、Idle切替でも約15度ありました。Sword_Blockの終端は近いものの防御動作であり、コンボ固有の攻撃予備動作をそのまま引き延ばす方が今回の目的に合うと判断しました。これは候補サンプルに基づく実装判断です。

Regularは冒頭0.15秒までの身体の捻り・刀を引く動き、Heavyは冒頭0.25秒までの予備動作を利用します。その後もClipの同じ時刻から連続して進むため、別ポーズへの巻き戻しやつなぎ直しはありません。姿勢の連続性は骨格サンプルで確認しましたが、カメラ越しの見やすさ・印象は未評価です。LeadInPoses.pngにサンプルを保存しています。

## 4. 現在の設定値

| 設定 | Regular | Heavy |
|---|---:|---:|
| PlaybackSpeed（本体） | 0.85 | 0.85 |
| WindupDuration（予兆時間） | 0.35秒 | 0.50秒 |
| WindupEndTime（Clip冒頭区間） | 0.15秒 | 0.25秒 |
| 自動算出される予兆速度 | 約0.429 | 0.50 |
| RootMotionScale | 0.18（維持） | 0.10（維持） |
| RecoveryDuration | 0.40秒（維持） | 0.80秒（維持） |
| Timeout | 5秒（維持） | 7秒（維持） |
| Damage / Strength | 5 / Light（維持） | 6 / Heavy（維持） |

初撃Hitbox開始の計算上の目安はRegular約0.374秒、Heavy約0.535秒です。フレーム更新・遷移の影響が加わります。240Hzの接続テストでは予兆終了がRegular約0.358秒、Heavy約0.508秒でした。固定の実時間でEventを発火させる実装ではありません。

## 5. Inspectorで変更する手順

1. ProjectでAssets/Mock/ScriptableObjects/Enemy/UAL2Combos/RegularCombo.assetまたはHeavyCombo.assetを選択します。
2. Playback Speedで構え終了後の速度を変更します。両方0.85が初期値、1.0で本体を元速度へ戻せます。Animator全体のSpeedを編集する必要はありません。
3. Windup Durationで見せる予兆の長さを変更します。例えばRegularを0.35→0.45にすると、同じ冒頭の動作がよりゆっくりになります。0にすると予兆区間の減速を省略し、冒頭からPlayback Speedで再生します。
4. Root Motion Scale / Recovery Durationは同じPattern上で調整できます。今回の初期値は前回から維持しています。
5. Windup End Timeは区間の作者向け設定です。通常はRegular 0.15 / Heavy 0.25のままにしてください。変更する場合は初撃ON時刻（Regular 0.17 / Heavy 0.28）より前に保ちます。

Windup Animation参照はありません。同一Clip冒頭を使う方式のため不要です。EnemyDecisionConfigには演出時間を追加していません。今回、ユーザー調整済みの選択Weight・距離設定は変更していません。

## 6. Event・Root Motion・既存仕様への影響

Regularの3斬撃、Heavyの4斬撃と各Hitbox ON/OFF、同一区間内の重複命中防止、Light / HeavyのDamage・Feedback経路は維持しました。ClipのEvent時刻は一切変更していません。速度変更後もAnimatorがClip内時刻に応じて通知し、予兆終了後はPlaybackSpeedに従って間隔が伸びます。

HitStopは既存Animator.speed=0の経路を使い、予兆の姿勢・Runnerタイマー・本体・Recoveryを停止します。予兆終了時に変更するのはState専用Floatだけで、Animator全体のspeedを上書きしません。

Root Motionの元カーブと倍率も維持しています。Clip単体を複数の予兆速度・本体速度で完走したdeltaPositionの合計は、Regular約(-0.00431, 0, 3.02549)m、Heavy約(0.00240, 0, 6.88733)mで一致しました。Yは0です。既存のRigidbody Sweep、NavMesh制限、Agent無効化・再同期の経路は変更していません。

Controller / Mover接続テストの初期設定では、Blend込みの水平移動はRegular約0.508m、Heavy約0.680mでした。開始Blend中に通過するカーブ量が速度により変わるため、実際の総移動距離はClip単体の積分値×倍率と完全には一致しません。実戦の衝突・NavMesh端・足滑りもユーザー確認対象です。

予兆中・本体中のJustAvoid追撃による中断、通常被弾時の継続、死亡・戦闘終了・Disable・強制中断の後始末を維持しています。

## 7. コンパイル・Edit Mode検証

- コンパイル完了：scriptCompilationFailed=false、isCompiling=false、isPlaying=false。
- ValidateWindup.cs：243項目成功。速度1.0 / 0.85、予兆時間0 / 0.2 / 0.35 / 0.5 / 0.8秒、実Animator時刻、Event区間数、Damage重複防止、Strength、Recovery、HitStop、Root Motion、中断、終了通知欠落を検証。
- 低FPS（0.1秒刻み）で予兆終端をまたいでもRegular3区間 / Heavy4区間が処理されることを確認。
- 予兆中のHitbox無効、Busy維持、JustAvoid中断、死亡・戦闘終了・Disableを検証。
- 既存単発4攻撃の選択参照を確認。Enemy.controller、両Combo Clip、GameScene、フォント、両EnemyDecisionConfigは開始時ファイルとSHA256一致。Player / JustAvoid / EnemyMover / 衝突制限は今回編集していません。
- 通常EnemyDecisionConfigには開始時から各コンボの候補行が2つずつ存在しました。選択重みが合算される現在の設定を保護し、今回の操作感調整では整理していません。

検証は一時Preview Scene上です。Animatorの速度・姿勢・deltaPositionはネイティブAnimator.Updateで測定しました。Edit ModeではAnimation EventやStateBehaviour / OnAnimatorMoveの自動通知がPlay Modeと異なるため、接続テストでは実Clip時刻に従って既存Controllerへ明示通知しています。GameSceneでの自動通知・実衝突・実際の見切りやすさを確認済みという意味ではありません。

詳細証跡：Inspection.txt、Validation.txt、ValidationResult.json、FinalAudit.txt、SnapshotComparison.txt。

## 8. ユーザーのPlay Mode確認項目

1. まずRegular単独、次にHeavy単独で試し、向き直り→刀を構える予兆→初撃が読み取れるか確認してください。予兆0.35 / 0.50秒と本体速度0.85から比較できます。
2. 構え中に被弾判定が出ず、各3回 / 4回の斬撃だけで命中すること、各斬撃を見て回避・JustAvoidできることを確認してください。
3. 特にRegular初撃とHeavy斬り上げ、Heavyの空振り後の反撃猶予を確認してください。構えが足りなければWindup Durationを先に伸ばし、本体が速ければPlayback Speedを調整します。
4. 命中時のHitStopで構え・攻撃・Recoveryが飛ばないこと、JustAvoid追撃で予兆中も本体中も停止することを確認してください。
5. 壁際・密着・NavMesh端で前進量、貫通、足滑り、終了後の追跡再開を確認してください。
6. 速度1.0へ戻した比較と、単発4攻撃・Playerコンボ・JustAvoid追撃の回帰確認を行ってください。
