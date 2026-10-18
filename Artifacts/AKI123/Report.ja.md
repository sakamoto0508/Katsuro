# AKI-123 — 敵2連撃コンボ 実装報告

2026-10-18 / Unity 6000.2.7f2 / HDRP。
作業開始時のgit status / git diffは空。ローカルの前回実装を基に拡張した。
Play Modeは起動していない。以下の「検証」は静的確認またはEdit Modeの一時Preview Sceneでの評価であり、GameSceneの実プレイではない。

## 1. 変更ファイル

既存ファイル:

| ファイル | 内容 |
|---|---|
| Assets/Mock/Scripts/Enemy/EnemyConfig/EnemyDecisionConfig.cs | 候補へPattern参照を追加。既存Attack/Action/Weightを維持。 |
| Assets/Mock/Scripts/Enemy/EnemyAI/EnemyDecisionMaker.cs | EnemyDecisionへPatternを追加。優先順位、無効Pattern除外、RepeatPenaltyを拡張。 |
| Assets/Mock/Scripts/Enemy/EnemyAI/EnemyAI.cs | 中断時のBusy解除入口。完了時の待機フラグ整理。 |
| Assets/Mock/Scripts/Enemy/EnemyAction/EnemyAttacker.cs | 既存の開始条件をCanPerformAttackへ切り出し、全段の事前確認に再利用。 |
| Assets/Mock/Scripts/Enemy/EnemyController.cs | Pattern実行入口、更新、中断、Root Motionの所有権・単発Eventとの分離。 |
| Assets/Mock/Scripts/Enemy/Animation/EnemyAnimationController.cs | 既存強制攻撃中断からPatternも解除。 |
| Assets/Mock/AnimationController/Enemy.controller | EnemyCombo1/2 State、同名Trigger、各StateへのAnyState Transitionを追加。既存State/Transitionは維持。 |
| Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset | 既存4攻撃＋TwoHitCombo＋StepBackの近距離候補。 |
| .gitignore | 従来除外されていたAnimationフォルダ内で、TwoHitComboフォルダとそのmetaのみ追跡可能にした。 |

新規ファイル（各Unityアセット・スクリプトのmetaを含む）:

- Assets/Mock/Scripts/Enemy/Config/EnemyAttackPattern.cs
- Assets/Mock/Scripts/Enemy/EnemyAction/EnemyAttackPatternRunner.cs
- Assets/Mock/Scripts/Enemy/EnemyController.Pattern.cs
- Assets/Mock/Animation/Enemy/TwoHitCombo/EnemyCombo1.anim
- Assets/Mock/Animation/Enemy/TwoHitCombo/EnemyCombo2.anim
- Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/ComboStep1.asset
- Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/ComboStep2.asset
- Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/TwoHitCombo.asset
- Artifacts/AKI123/ 内の検証スクリプト・結果・骨格比較資料・本報告書

Enemy.prefabの変更は不要。既存DecisionConfig参照からPatternへ届く。Prefabの単発AttackData配列は4件のままで、修練場の登録対象は増やしていない。
Player、既存4攻撃データ/Clip、元ARPG Clip、EnemyMover、AttackRootMotionState、AttackRootMotionPhysics、HitReactionアセット、VFX、UI、Sceneの内容は変更していない。

## 2. 採用アニメーションと理由

| 段 | 原本 | 敵専用コピーで使う原本の区間 | コピーの長さ |
|---|---|---|---:|
| 1 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo1.anim | 0～0.400秒 | 0.400秒 |
| 2 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo2.anim | 0.083333～0.966667秒 | 0.883333秒 |

敵Prefabの実AvatarでCombo1～3をサンプルし、腕・手・頭の骨格と刀Colliderの位置を比較した。Combo1の斬撃後の姿勢からCombo2の構えへつなぐため、1撃目の戻り動作と2撃目冒頭の重複する構え直しをコピー側だけで省いた。
全曲線を保持区間へ切り出し、2撃目は時刻を0へ移した。再生速度は両方1倍。新規StateへのBlendは0.08秒。既存攻撃の速度・Transitionは変更していない。

元Clipの比較資料はComboPoseComparison.pngとComboPoses.json。これは元Clipの骨格資料で、完成したゲーム描画や最終Blendの動画ではない。
実際の衣装・刀・画面上でのポーズ連続性、斬撃方向の読みやすさ、回避猶予は未プレイ。品質をClip名だけから断定していない。

UAL2のSword_Regular_Combo / A / B / A_Rec / B_Recも既存調査結果を確認した。現在Genericで敵Humanoidへのリターゲットが未検証のため、今回は組み込まず、実Avatarでサンプル可能だったARPGを採用した。UAL2のAvatar/Import設定は変更していない。

## 3. 実行フロー

EnemyAI → DecisionMakerがTwoHitComboを選択 → EnemyControllerが全段のデータ・Trigger・State・武器を事前確認 → Windup → ComboStep1をAttackerで実行 → 1段目専用Event → Interval → ComboStep2 → 最終段専用Event → Recovery → AIへ一度だけ完了通知。

RunnerはInactive / Windup / Playing / Interval / Recoveryの小さな状態管理だけを担当し、Animator、武器、移動、AIを直接操作しない。
Controllerから渡された開始・Hitbox解除・完了コールバックで既存の仕組みにつなぐ。2段専用のハードコードはなく、AttackStepsの件数に従う。

## 4. Inspectorで定義するデータ

TwoHitCombo.assetはCreate > Enemy > Attack Patternで作成できるEnemyAttackPattern。

| 項目 | 初期値 / 用途 |
|---|---|
| Pattern Name | TwoHitCombo。表示・ログ用。 |
| Attack Steps | ComboStep1 / ComboStep2の2件。 |
| 各段のAttack | 既存型EnemyAttackData。両段ともLight、基準Damage10、HitboxIndex0。 |
| 各段のAnimator State | Base Layer.EnemyCombo1 / Base Layer.EnemyCombo2。完全なStateパス。Event送信元照合にも使う。 |
| 各段のTimeout | 3秒。終了Event欠落や再生不能時の中断期限。 |
| Windup | 0.15秒。最初のClip開始前の待機。Clip内の予備動作とは別。 |
| Combo Interval | 0.12秒。前段終了姿勢を保持してから次段へ進む待機。 |
| Recovery Duration | 0.65秒。最終段後の攻撃判定が無い反撃猶予。 |

全タイマーはTime.deltaTime×Animator.speedで進み、HitStopのAnimator停止中に段間やRecoveryを消費しない。
Cooldownは今回追加していない。将来3段目を追加する場合は、専用AttackData/State/Clipと対応する段番号のEventを作ってAttackStepsへ追加する。同じStateへの自己遷移は使わない。

## 5. AI候補・Weight

EnemyDecisionConfig.assetのNear Candidatesを開く。Actionは攻撃カテゴリとしてSlashを使い、単発はAttack、コンボはPatternを指定する。

| 候補 | Weight初期値 |
|---|---:|
| LightAttack | 14.285714 |
| LightAttack2 | 7.142857 |
| HeavyAttack | 7.142857 |
| HeavyAttack2 | 7.142857 |
| TwoHitCombo | 14.285714 |
| StepBack | 10 |

攻撃内の比率は提示例の20:10:10:10:20。攻撃カテゴリ合計50・後退10を保つよう換算した。1回のコンボが2撃になる分、攻撃回数や圧力まで従来と同じになるわけではない。
遠距離・中距離候補は維持。Weightは自由に変更でき、0でその候補を無効化できる。

AttackとPattern両方指定時はPatternを優先する。設定済みPatternに空の段・未設定Attack/State/Trigger・不正な時間があれば候補を抽選から除外し、Attackへフォールバックしない。
両方Noneの攻撃候補は前回と同じ旧形式のActionType抽選。移動・待機では両方Noneにする。
StateやTriggerの実在・武器の有効性はControllerでも確認し、実行できない候補は待機後の再抽選へ戻る。

RepeatPenaltyPerAttackは初期OFFのまま。Slash候補全体へ従来の補正を掛ける。ONなら同じAttackDataまたは同じPatternだけを補正する。最終段の攻撃データではなくPattern単位で履歴を保持する。

## 6. 1撃目から2撃目へ進む方法

コピーClipには専用Eventを配置した。

| コピーClip | Hitbox ON | Hitbox OFF | 段終了 |
|---|---:|---:|---:|
| EnemyCombo1 | 0.183333秒 | 0.316667秒 | 0.383333秒 |
| EnemyCombo2 | 0.216667秒 | 0.366667秒 | 0.866667秒 |

Event名はAnimEvent_ComboEnableWeaponHitbox / AnimEvent_ComboDisableWeaponHitbox / AnimEvent_ComboStepFinished。ON時にはAnimEvent_ComboSoundも置き、既存NormalSlash / Slash2を使用する。
全EventのintParameterは段番号1または2。AnimatorStateInfo.fullPathHashと段番号の両方が現在段に一致する場合だけ受け付ける。

1撃目の段終了EventはHitboxとその段のRoot Motionを止めてIntervalへ進む。AIへ通知しない。
2撃目の段終了Eventも直ちにAIへ返さずRecoveryへ進む。Recoveryの時間経過でコンボ全体完了となる。
各Stateに自動Idle遷移を置かず、段間・Recoveryは終端姿勢を保持する。最終完了後にIdleへ戻し、AIの次行動を許可する。
既存のAnimEvent_OnAttackFinishedは単発専用のまま。Pattern中に旧単発Eventが届いても無視する。

## 7. Hitbox / Root Motion / Busy

同じEnemyAttackerとEnemyWeaponを再利用。各段開始で選択データを渡し直し、ONで命中済み対象一覧をクリアする。1段内の複数Collider接触は1回に抑え、次段では同じ対象へ再び命中できる。
Light/Heavy判定は前回のEnemyAttackData.Strengthを維持。コンボは両段Light10。将来の段ごとの強弱・基準威力は各データで編集できる。

攻撃開始からRecovery終了までHoldMovementForAttackを維持する。段間にNavMeshAgentを再開しない。
各Stateには既存AttackRootMotionStateを付け、既存のOnAnimatorMove → EnemyMover → AttackRootMotionPhysicsへ渡す。共通倍率0.18は変更していない。
State終了時、Patternが継続中ならその段のRoot Motionだけを止め、移動停止を解放しない。入ってくるStateが所有者を更新した後に前段のStateExitが来ても、ハッシュ不一致なので新しい所有権は消さない。
終了Event、キャンセル、異常終了では保留Root MotionとHitboxを消去する。AIはPattern全体を一行動としてBusyにする。

## 8. ジャスト回避追撃と中断

既存ApplyDamageのIsJustAvoidCounter判定でPatternをキャンセルしてから、従来の追撃HitReactionへ進む。
Hitbox、Runner状態、次段待機、Trigger、予約行動、Root Motion、AI Busyを解除する。通常抽選は既存の被弾復帰通知まで開始しない。
キャンセル済みPatternへの遅れた段Event、前段のEvent、重複した段終了は受け付けない。同じ旧Stateがまだ残っている場合は、新しい段の開始として再利用しない。

通常Light/Heavyの被弾ではコンボをキャンセルせず、従来の中断ポリシーを維持する。回避しただけで追撃しなければ2段目を継続する。
死亡・戦闘終了・OnDisableでは再抽選せず全予約を解除する。
既存の強制攻撃中断入口ではコンボを消去し、被弾側が復帰を管理しないケースも待機後に再抽選できるようにする。
設定不備・Event欠落では失敗終了としてHitbox/移動停止を解除し、ObserveSeconds後に再抽選する。

## 9. 検証と未検証

Unity Editorのコンパイルはcompleted / failed=false / errors=[]。最終のEdit Mode検証は65チェックすべて成功。
証跡: CompileResult.json、EditModeResult.json、Validation.txt、EditorState.json。

確認した内容:

- Pattern/AttackDataのシリアライズ、Prefab参照、State/Trigger/Transition、Root Motion Behaviour、Clip長・Event配置。
- 既存4単発の個別選択、Pattern優先順位、無効Pattern除外、RepeatPenalty、攻撃カテゴリ重み合計。
- 1段目で全体完了しない、段間で2段目を開始、Recovery終了で一度だけAI復帰。
- 順序違い・重複・前段・中断後のEventを無視。
- 同じ対象への各段1回ずつの命中と、段内重複接触の抑止。2段ともLight情報。
- タイマー停止、通常Light/Heavy被弾の継続、1段目有効区間中の追撃キャンセル、死亡・戦闘終了・強制中断・次段前Animator無効化・Event欠落の復帰。
- 段間で移動停止が続くこと、遅いStateExitが次段のRoot Motion所有権を消さないこと、中断時の移動解除。

重要な検証上の制限:

Edit ModeのAnimator.UpdateではAnimation Eventが自動通知されなかった。そのため実Animatorを手動評価し、実ClipのEvent時刻とState情報でEventを明示的に通知してController経路を検証した。物理接触も明示通知であり、ゲーム中のTrigger衝突やEvent自動発火の実証ではない。
この条件でHitboxの開始は約0.325秒 / 0.883秒となり、2つの区間を確認できた。Play Modeでの実タイミング・操作感とは区別する。
Preview SceneのMover検証はNavMeshAgentを接続せず所有権・停止フラグを評価した。GameSceneの実NavMesh/Rigidbodyとの移動・衝突、壁際の押し出しは未プレイ。
実描画での接続の滑らかさ、斬撃方向の違い、ジャスト回避の成立範囲、追撃演出、SE/VFX/HitStopとカメラ、最終バランスはユーザー側の確認事項。

アセット保存時に生じた動的フォントキャッシュの付随差分は退避し、UIファイルを作業開始時の内容へ戻した。以後の保存は今回の対象アセットだけに限定した。

## 10. Unityでの手動テスト

1. GameSceneでEnemyのDecisionConfig参照を確認する。Near CandidatesにTwoHitComboのPattern参照があり、既存4Attack参照とStepBackが残ることを確認する。
2. 検証用にDecisionConfigを複製してEnemyへ一時割当し、TwoHitComboだけWeight=1、他0、RepeatPenalty=1にする。Play中のSO編集は残るため、元データを直接実験用にしない。
3. コンボを空振りさせ、予備動作→1撃目→短い間→2撃目→明確な後隙があることを確認する。段間に追跡歩行が入らず、終了後だけ次行動になることを見る。
4. 1撃目を通常回避して追撃せず、2撃目が続くことを確認する。続けて2回の回避が入力可能で、両方を見切れるか確認する。
5. 1撃目をジャスト回避し、既存の追撃2連撃を成立させる。敵の2撃目が出ず、追撃HitReaction・HitStop・VFX・カメラと被弾復帰が従来どおりか確認する。
6. 2撃とも受ける、1撃目だけ避ける、1撃目だけ受ける場合を比較する。各段のCollider区間と1段内の多重ダメージがないことを見る。RunSession補正により表示ダメージは基準10と異なる場合がある。
7. 正面・側面・至近距離・射程外・壁際で実行し、State切替時の位置飛び、段間のNavMesh再開、Root Motionの残留がないか確認する。
8. Windup / ComboInterval / RecoveryDurationを検証用Patternの複製で調整し、2撃目を判断する余裕と空振り後の反撃猶予を確認する。特に2撃目の方向と構えの見え方を確認する。
9. 1段目・段間・2段目・Recovery中に撃破/戦闘終了を試し、Hitboxや次段が残らないことを確認する。検証用データで次段Trigger/State不正も試し、永続Busyにならないことを見る。
10. Pattern Weight=0で既存4単発を1種類ずつ確認し、バック回避、通常被弾、ジャスト回避追撃、死亡/Final Blow、勝敗/Retry、修練場の4攻撃も確認する。最後に検証用Scene overrideを戻し、通常のConfig参照・Weightへ戻す。

今回、突進攻撃・バック回避後専用AI・HitReaction差し替え・Player変更は追加していない。
