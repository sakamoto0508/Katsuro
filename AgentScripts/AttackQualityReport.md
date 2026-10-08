# Katsuro 攻撃Animation品質調整

通常攻撃15 Stateにフェーズ別速度倍率を追加し、攻撃終了後のIdle復帰Blendを調整した。Clip、既存Animation Event、Root Motion曲線、Prefab、PlayerStateConfig、Counter設定は変更していない。Play Modeは実施していない。

## 設定値

倍率は既存State Speedへの追加倍率。Playerの実効速度は表の倍率×0.8、Enemyは×0.7。Animator全体のstatus/temporary倍率は既存AnimationSpeedControllerが管理する。既存Blend中は追加倍率1、振り抜き区間はSlash倍率からRecovery倍率へSmoothStepで移行する。

| State | Windup | Slash | Recovery | Slash開始/終了・Recovery開始（Clip秒） | Idle Blend秒 |
|---|---:|---:|---:|---|---:|
| Player LockOnLightAttack（B1） | .96 | 1.13 | 1.00 | .180 / .467 / .550 | .14 |
| Player LOLightAttack2（B2） | .98 | 1.10 | 1.00 | .150 / .350 / .430 | .14 |
| Player LOLightAttack3（B3） | .97 | 1.12 | 1.00 | .200 / .500 / .580 | .15 |
| Player LOLightAttack4（B4） | 1.00 | 1.08 | 1.00 | .080 / .350 / .430 | .14 |
| Player LOLightAttack5（B5） | .94 | 1.15 | .98 | .200 / .433 / .520 | .16 |
| Player UnLock Combo1 | .96 | 1.12 | 1.00 | .170 / .350 / .430 | .14 |
| Player UnLock Combo2 | .96 | 1.13 | 1.00 | .290 / .450 / .530 | .15 |
| Player UnLock Combo3 | .98 | 1.10 | 1.00 | .180 / .300 / .400 | .14 |
| Player UnLock Combo4 | .94 | 1.12 | .99 | .400 / .530 / .780 | .16 |
| Player StrongAttack | .88 | 1.15 | .97 | .100 / .230 / .650 | .18 |
| Player Heavy2 | .87 | 1.16 | .96 | .240 / .467 / .570 | .20 |
| Enemy LightAttack1 | .96 | 1.10 | 1.00 | .500 / .730 / .820 | .16 |
| Enemy LightAttack2 | .95 | 1.10 | 1.00 | .380 / .600 / .700 | .16 |
| Enemy AttackHeavy | .92 | 1.14 | .98 | .140 / .450 / .560 | .20 |
| Enemy AttackHeavy2 | .93 | 1.13 | .98 | .170 / .467 / .570 | .20 |

Idle遷移は既存.25秒から表の値へ変更。Fixed Duration、Has Exit Time=true、Exit Time=1、Offset=0、Interruption=None、Ordered Interruption=true、全条件を保持。Combo遷移は既存.25秒、条件、Exit Time/Offset/Interruptionを保持。入口・Ready→Slash・Counter・Death・HitReaction遷移は変更していない。

## コンボ・時間管理

- LightはLock-On5段、UnLock-On4段、Heavy2段。順序、ComboStep、Trigger、入力予約bool、消費条件、受付Event、各段の受付ディレイは維持。
- 変更前はClip長を実時間タイマーとして使い、State Speed .8やHitStopを考慮せずLocomotionへ終了する可能性があった。通常攻撃AnimatorがnormalizedTime<1の間はタイマー終了を抑制し、既存Attack Finished Eventを主経路として維持。
- Animator.speed=0の間はPlayerAttackStateの時間加算と予約入力消費を停止。予約自体は保持する。
- 通常攻撃のEvent受信元をAnimatorの現在/次段Stateと照合。旧段から遅れて届くON/OFF、Combo開閉、Finishedが次段を壊すことを防ぐ。Clip側の関数名・Event引数・時刻は一切変更していない。Counter等のEventには追加フィルターを適用しない。
- 各通常Stateに専用Float Parameterを持たせ、旧Stateの退出が新Stateの倍率を消さない。State退出時に自身の倍率だけ1へ戻す。

受付区間の計算（Blend外、global speed=1。実操作時間の測定ではない）：B1 .917→.871秒、B2 1.188→1.163、B3 1.042→1.002、B4 1.125→1.096、UnLock1 .875→.853、UnLock2 1.083→1.067、UnLock3 .875→.858、Heavy1 .813→.778。最大短縮約5%。受付ディレイは変更していない。

## Event・刀・Root Motion検証

全19通常関連Clip（攻撃15＋既存準備4）を変更前後で比較した。全Clipバイト列SHA256、Clip設定、全Event、60Hzサンプル＋Event時刻での刀先/Blade Base/腰/胸の姿勢が完全一致。Hitbox ON/OFF時点と区間内の刀軌道はClip時間に対して維持。速度による実時間上の到達時刻は変化する。

実Avatarで全15攻撃のClip単体Root Motionを1000Hz積分し、一定速度とフェーズ速度のXZ総移動を比較。一致（許容.015m内、記録値はほぼ同一）。Player .25 / Enemy .18の倍率、Root Motion設定、Rigidbody/NavMesh同期スクリプトは変更なし。Combo中のBlend、衝突、接地を含む実ゲーム移動距離の保証ではない。

Just Avoid Counter速度.78 / 1.10 / 命中後1は既存のまま。通常速度BehaviourはCounterに付与していない。Animator.speedへの書き込みを追加していない。HitStop時間と管理方法を変更していない。

## Editor検証結果

- 代表4攻撃でParameter、Root Motion距離、受付時間を検証してから残りへ展開。
- 実Player Controllerの手動Animator.Update：Lock-On5段、UnLock-On4段、Heavy2段の次段接続・最終段終了・各段単発/途中終了からIdle復帰・旧倍率リセット・HitStop停止を確認。Event配送は無効にしてController経路を独立検証。
- 既存PlayerAttackStateを使う検証Harness：5/4/2段、閉じた受付への入力予約、受付ディレイ、1入力の複数消費防止、段飛ばし/同段反復防止、停止中の予約保持、再開後消費、受付終了、最終段終了、タイマー早期終了防止、キャンセル時Hitbox解除を確認。
- このEditorではEdit Mode手動評価からAnimation Eventを配送できなかった。実Eventの呼出順・受信ガードの実配送・入力デバイス・物理攻撃判定はRuntime未検証。Event欠落なしとはClipデータの検証を指し、Runtime配送の保証ではない。
- C# Compile Errorなし。Controllerを保存後、UnityプラグインからAnimator / Prefab / Clip / PlayerStateConfigを再取得。PrefabとClipは変更不要。

## Clip品質の残課題

1. Player StrongAttackReady（ARPG_Halberd_Attack_Heavy1_Start）は既存Exit Time=0 / Blend=.06629秒でHeavy1へ進む。Clipの長い準備姿勢をほぼ使わず、Heavy1の先頭.0〜.15秒も短い。入口Blend中は追加倍率1なので、Heavy1の短いWindupへの減速効果には限界がある。腰・胸・肩・右腕の溜めをさらに明確にするには、同じ既存時刻とRoot Motionを維持した小規模な姿勢調整を別途検討する。準備Stateの遷移を延長する変更は今回採用していない。
2. Heavy1はHalberd系Clipを刀キャラクターへ適用し、.15〜.23秒に突き寄りの動作がある。手首・右上腕・胸・腰の武器種に合わせたPose調整は速度では解決できない。刀軌道やHitbox対応を変える編集は行っていない。
3. Player Light B4はHitbox ON=.10秒、斬撃ピークは約.30秒。UnLock Combo3はON=.20〜OFF=.60秒に対し刀先ピークは約.25秒で、広い有効区間がある。既存の対応関係を保持し、Eventや軌道を修正していない。右腕・手首・胸の振り抜きを変更する場合は有効区間全体の再評価が必要。
4. Enemy Heavy準備2 Clipは既存State Speed=.8で維持。新しいWindupを足したり、Slash開始を後方へずらす変更はしていない。構えの判読性と全身Poseの自然さはPlay Modeで確認が必要。

これらはサンプルと構造からの制約・改善案であり、実ゲームでの見た目を検証済みとするものではない。

## 変更ファイル

- Assets/Mock/Scripts/Feedback/NormalAttackSpeedState.cs（新規、meta含む）
- Assets/Mock/Scripts/Player/StateMachine/States/PlayerAttackState.cs
- Assets/Mock/Scripts/Player/PlayerController.cs（既存文字コードを保持）
- Assets/Mock/AnimationController/Player.controller
- Assets/Mock/AnimationController/Enemy.controller
- AgentScriptsの調査・設定・検証スクリプト、変更前後記録、検証結果。

Animation Clip / Prefab / ScriptableObject / Enemy AI / Damage / Movement / VFX / Camera / UI / Counter設定には変更なし。Play Modeは未実施。
