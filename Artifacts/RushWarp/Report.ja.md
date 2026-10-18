# Katsuro：突進攻撃・背後ワープ攻撃

2026-10-18。突進を先に実装して単独検証し、その後ワープを追加しました。開始時のGit差分はなく、HEADはbd46c23でした。作業対象の開始時コピーはTemp/RushWarp-StartSnapshotに保護しています。開始時はUnityが既にPlay Modeだったため、ユーザーご自身による停止を待ち、以降はEdit Modeで作業しました。CodexからPlay Modeは起動していません。

## 1. 突進の採用アニメーション・Root Motion

現在のEnemy.prefabのHumanoid Avatarで次の2候補を比較しました。

| 候補 | 長さ | 補正後のネイティブdeltaPosition合計 |
|---|---:|---|
| ARPG_Samurai_Attack_Sprint | 約0.983秒 | (-0.007255, 0, 2.832967)m、向き補正0° |
| Armature\|Sword_Dash（RM版） | 約1.567秒 | (-0.000009, 0, 4.776226)m、向き補正180° |

UAL2 RMは元のGeneric FBXを一時コピーし、既存のHuman設定からhumanDescriptionを適用してHumanoid化しました。両候補でEnemy Avatarが有効であり、前進deltaPositionを抽出できました。一時コピーは参照がないことを確認して削除しています。元FBXは変更していません。

今回は刀の振り下ろし、頭上へ刀を構える予備動作、現在の構えとの接続からARPG Sprintを採用しました。敵専用コピーEnemyRushAttack.animを作成し、Root Rotation / YをBake、XZはBakeせず、ループOFFにしています。Root Motionは既存EnemyMoverへ渡し、倍率1.3、累計水平移動上限4mを設定しました。Clip単体の移動をそのまま倍した値と実戦移動量は、開始Blend・衝突・停止Eventにより異なります。

Clip冒頭0.22秒分を0.45秒かけて再生し、その後0.9倍速へ切り替えます。構えながらの踏み込みにも元のRoot Motionを使うため、棒立ちで待機させたり、初期移動を後からまとめて適用したりしません。ネイティブ曲線は先頭0.22秒で約1.35m、0.45秒で約2.69mまで前進します。実移動は倍率・Blend・衝突制限を通ります。

両新攻撃の斬撃は1回です。専用ClipのEventはON 0.28秒 / OFF 0.45秒、Root Motion停止0.48秒、終了約0.953秒です。すべてClip時刻であり、固定実時間のTimerで命中を発火させていません。刀の軌跡サンプルの主斬撃は約0.35秒でした。実際の見切りやすさ・足滑りは未確認です。

## 2. 背後ワープの実装方法

既存のEnemyActionType.WarpAttackは、通常攻撃の実行入口と未使用アニメーション素材があるだけで、テレポートは行っていませんでした。このAction値8を維持し、Pattern実行経路に準備処理を追加しました。RushAttackは既存enum値を変えず10を追加しています。

流れは、向き直り→WarpDepartureで刀を構える（0.35秒）→位置を再検証→煙・SEとRenderer非表示（0.18秒）→現在のPlayer背後を再計算・再検証→Agent.Warpの成功確認→Renderer復元・Playerへ向き直り→0.50秒の攻撃予兆→斬撃→Recoveryです。

WarpDepartureは攻撃Clip冒頭をゆっくり再生する専用Stateです。判定Event・Root Motion StateBehaviourを持たず、XZをBakeしています。出現後のEnemyWarpAttack.animはARPG Sprintの別コピーで、予兆・判定・終了を共通Runnerに委譲します。Warpそのものは明示的な位置変更です。出現後の斬撃だけ倍率0.7・上限2mのRoot Motionを使います。

背後はPlayer.forwardの水平成分を使い、初期距離2.5m・角度Offset 0°で算出します。選択時、消失直前、移動直前に以下を確認します。

- Agent type / areaMaskに一致したNavMesh.SamplePosition（探索半径0.35m）と高さ差。
- Player側の床からのNavMesh.Raycast。NavMesh未反映の壁も腰の高さの物理Rayで検査。
- NavMesh端から敵Capsule半径＋0.05m以上の余白。
- 出現後の向きにした敵Capsule全体のOverlap検査。自身とTrigger以外のColliderがあれば中止。

実移動は攻撃hold中にAgentを一時有効化し、Warp成功とisOnNavMeshを確認してRigidbody / Transformへ同期、再びAgentを無効化します。失敗時は元の位置を保ち、見た目・Hitbox・Busyを解除します。消失中にPlayerが動いた場合も最新の背後を使い、塞がれた場合は元位置で再表示して中止します。別の場所へ無条件に押し出す処理はありません。

消失中はRendererだけを隠し、本体Colliderは残します。元のRenderer.enabled値を保存し、中断時も元から非表示だったRendererを勝手に表示しません。

SEは既存Iai、VFXは既存Smoke 1を流用した敵専用EnemyWarpSmoke.prefabです。元Prefabの位置Binderが未設定だったため、専用コピーで自分のTransformへ設定し、再生前に位置を反映します。SE音量は既存AudioManager経路へ0.6で渡します。煙は最大2秒で消去し、中断・終了でも破棄します。既存コンボのSwing / Hit SEは変更していません。音・煙の印象は暫定であり、実画面での確認が必要です。

## 3. 変更ファイル

既存変更：

- Assets/Mock/Scripts/Enemy/Config/EnemyAttackPattern.cs：距離・Cooldown・移動上限・ワープ設定。
- Assets/Mock/Scripts/Enemy/EnemyAction/EnemyActionType.cs：RushAttackを追加。
- Assets/Mock/Scripts/Enemy/EnemyAction/EnemyAttackPatternRunner.cs：ワープの出現後予兆時間を共通Runnerへ反映。
- Assets/Mock/Scripts/Enemy/EnemyAction/EnemyMover.cs：累計移動上限、攻撃hold中の安全なAgent.Warp、未登録Agentへの停止命令を回避。
- Assets/Mock/Scripts/Enemy/EnemyAI/EnemyAI.cs：RushのBusy管理と実行可能性フィルター。
- Assets/Mock/Scripts/Enemy/EnemyAI/EnemyDecisionMaker.cs：距離・候補フィルター、Rushの攻撃扱い。RepeatPenaltyを維持。
- Assets/Mock/Scripts/Enemy/EnemyController.cs / EnemyController.Pattern.cs：Target参照、Cooldown、予兆・ワープ・Root停止の共通接続。
- Assets/Mock/AnimationController/Enemy.controller：RushAttack / WarpAttack / WarpDeparture、専用Triggerと遷移を追加。
- Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset：中距離にRush / Warp、遠距離にWarpを追加。
- .gitignore：編集可能なSpecialAttacks ClipだけをGit対象に追加。

新規：

- Assets/Mock/Scripts/Enemy/EnemyController.Warp.cs、EnemyAction/EnemyWarpDestination.csと各meta。
- Assets/Mock/Animation/Enemy/SpecialAttacks/：EnemyRushAttack.anim、EnemyWarpAttack.anim、EnemyWarpDeparture.animとmeta。
- Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/：RushAttack / WarpAttackのPattern、各AttackData、各単独テストConfigとmeta。
- Assets/Mock/Prefabs/EnemyWarpSmoke.prefabとmeta。
- Artifacts/RushWarp/：選定・生成スクリプト、姿勢サンプル、検証コード・ログ・本報告。

GameScene、Enemy.prefab、既存UAL2の2Clip・2Pattern、ComboTest Configは開始時ハッシュと一致しています。未保存Sceneを保存・破棄する操作はしていません。通常単発4種、Playerコンボ、JustAvoid追撃の資産は変更していません。

## 4. Inspector設定

Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/で各Patternを選択します。

| 設定 | RushAttack | WarpAttack |
|---|---:|---:|
| MinAttackDistance | 3m | 3m |
| MaxAttackDistance | 4.25m | 18m |
| Cooldown（開始時から） | 5秒 | 8秒 |
| WindupDuration | 0.45秒 | 消失前0.35秒 |
| WindupEndTime | Clip 0.22秒 | Clip 0.22秒 |
| PlaybackSpeed | 0.90 | 0.90（出現後のAttackPlaybackSpeed） |
| RootMotionScale | 1.30 | 0.70（出現後斬撃のみ） |
| MaximumTravelDistance | 4m | 2m（Teleport距離は含まない） |
| RecoveryDuration | 0.65秒 | 0.80秒 |
| TeleportDistance | 使用しない | 2.5m |
| TeleportAngleOffset | 使用しない | 0° |
| DisappearDuration | 使用しない | 0.18秒 |
| ReappearWindupDuration | 使用しない | 0.50秒 |

Damage / StrengthはPatternのAttack参照先RushAttackData.asset / WarpAttackData.assetで変更します。両方とも初期値は基準Damage 10、Heavy。既存RunSession補正が別途適用されます。HitStop / SwordTrail / HitVFX / HitReactionは既存のHeavy経路です。

SelectionWeightはEnemyDecisionConfigの該当候補行のWeightです。同じ意味のWeightをPatternへ重複追加していません。距離・移動・演出時間・CooldownはPatternにあります。

## 5. EnemyDecisionConfigへの候補追加

対象の距離帯Candidatesへ次を追加します。

| Action | Attack | Pattern | 通常Configで追加したWeight |
|---|---|---|---:|
| RushAttack | None | RushAttack.asset | Mid 5 |
| WarpAttack | None | WarpAttack.asset | Mid 2 / Far 2 |

近距離候補や既存の重みは維持しました。距離帯の条件とPatternのMin / Maxの両方を満たした場合だけ抽選します。例えばRushはMid帯でも4.25mを超えていれば除外され、Approachなどが残ります。

単独確認用として同フォルダにEnemyDecisionConfig_RushAttackTest.asset / EnemyDecisionConfig_WarpAttackTest.assetがあります。全距離帯にその攻撃1行・Weight 1を設定し、使用不能時はWaitへ戻ります。距離条件やCooldownはテストConfigでも有効です。

GameSceneの現在のConfig overrideは変更していません。新攻撃の確認には、確認したいEnemyのDecision Configを上記テストConfigへ手動で割り当ててください。既存ComboTestを使用しているEnemyには、新攻撃はまだ追加されていません。

バック回避後の65% / 25% / 10%分岐は今回追加していません。

## 6. Hitbox・Busy・Cooldown・移動

Hitbox、DamageInfo、Strength、重複命中防止はEnemyAttackerを再利用します。各新Clipに1組のON/OFF、Root停止、最終完了Eventを設定しました。予兆・消失中は判定が有効になりません。前のStateから遅れて届くEventはState所有権で除外します。

Busyは準備・消失・出現後予兆・攻撃・Recoveryまで保持します。通常被弾の継続方針は維持し、JustAvoid追撃・死亡・戦闘終了・OnDisable・強制中断で共通キャンセルを通します。完了Event欠落はTimeoutで、進行しない予兆は予兆時間＋1秒で解除します。

Cooldownは敵ごとのPattern別Dictionaryで、開始時にTime.time基準の期限を予約します。中断や移動直前の位置不成立でも予約を残すため、失敗した同じ攻撃の即再選択を防ぎます。抽選前に距離、Cooldown、Animator / 武器参照、ワープ先を確認し、候補から除外します。RepeatPenaltyはその後の有効候補に対して維持されます。

攻撃移動は従来通りAgentを無効化し、Animatorの自動位置適用を止めて、水平deltaPositionをRigidbodyへ渡します。Root Motion累計距離を上限で切り、SweepとNavMesh境界で制限します。ワープ中もAgentは移動主体にならず、テレポート命令の実行時だけ有効化します。終了時に共通処理で再同期します。

## 7. 壁際・障害物・死亡時の安全性

一時Sceneの実Colliderで、壁とプレイヤー相当CapsuleへのSweep、最大距離上限、その後の追加移動禁止、HitStop時の保留移動破棄を確認しました。ワープ先は壁との重複、未Bakeの壁を挟む位置、NavMesh外、端の余白不足を除外します。移動中Playerの最新背後へ再計算します。

位置が消失前に無効なら表示したまま中止し、消失後に無効なら元位置で再表示して中止します。Agent.Warp失敗時も元の位置を維持します。死亡・戦闘終了・DisableではHitbox、Root Motion、保留移動、Renderer非表示、煙、Busyを解除します。

## 8. 検証結果・未確認事項

- C#コンパイル完了：scriptCompilationFailed=false、isCompiling=false、isPlaying=false。
- 突進単独：36項目成功。
- 特殊攻撃の参照・AI・安全性・ワープ制御：69項目成功。重み3:1の抽選はRush 3001 / 4000回でした。
- 既存単発選択・UAL2コンボ：243項目成功。ユーザー設定のRegular速度0.8 / 予兆0.50秒、Heavy速度0.8 / 予兆0.75秒を維持しています。
- 合計348項目に加え、最終参照・ハッシュ照合と煙VFX位置Binderを確認しました。

重要な検証限界：Edit ModeではNavMeshクエリは動作しましたが、NavMeshAgent自体は登録されずisOnNavMesh=falseでした。そのため、ネイティブAgent.Warpの成功と実ゲームでのAgent再同期は未検証です。実APIが失敗した場合に元位置を保ち安全に解除されることを確認しています。到着後の制御テストでは、安全位置の検証後に到着処理を明示通知しました。これはAgent.Warp成功の検証ではありません。

Animation Event、StateBehaviour、OnAnimatorMoveもEdit ModeではPlay Mode同様に自動通知されないため、接続テストは実AnimatorのClip時刻に従って明示通知しています。ネイティブdeltaPosition・姿勢のサンプル、実Collider Sweep、NavMeshクエリとは分けて検証しました。ワープの再出現後から初撃までの制御テスト実測は約0.583秒です。

実戦のJustAvoid可能性、速度感、実命中、開始・停止時の足滑り、ワープ煙・SEの分かりやすさ、GameSceneでのAgent.Warp成功はユーザー確認対象です。専用のワープSEが必要かどうかも実際の印象で判断してください。

証跡：Candidates.txt / json、SprintPoses.png / DashPoses.png、RushValidation.txt、SafetyValidation.txt、RegressionValidation.txt、各Result.json、FinalAudit.txt。

## 9. Unityで試す手順

1. GameSceneの確認対象Enemyを選び、InspectorのAI → Decision ConfigをEnemyDecisionConfig_RushAttackTestへ変更します。敵から約4m離れ、Playしてください。Cooldown後も再度3〜4.25mへ離れて試します。範囲外やCooldown中にWaitになるのは設定どおりです。
2. 刀を頭上へ構える動作から斬撃を見切れるか、初撃を回避・JustAvoidできるか、空振り後0.65秒の追加Recoveryで反撃できるか確認します。
3. 正面・横向き・壁際・プレイヤー密着・NavMesh端で突進させ、前進方向、貫通防止、移動上限、終了後の追跡復帰、足滑りを確認します。距離が合わなければPatternのMaxAttackDistance / RootMotionScale / MaximumTravelDistanceを一緒に調整します。
4. 停止後、同じEnemyのConfigをEnemyDecisionConfig_WarpAttackTestへ変更します。約4〜8mからPlayし、消失前予兆→煙・消失→現在のPlayer背後2.5mへ出現→構え→斬撃の順を確認します。特に実際にAgent.Warpが成功するかを確認してください。
5. 出現直後にはダメージがなく、再出現を認識してから回避を判断できるかを確認します。短ければReappear Windup Durationを伸ばします。斬撃が届かない場合はRoot Motion Scale / Teleport Distanceを確認します。
6. Playerを移動させながら、背後が壁・障害物・崖・NavMesh外になる場所で試します。移動せずに中止すること、消失中でも中断後に必ず見えること、元位置や出現位置で固まらないことを確認します。
7. 新攻撃中のJustAvoid追撃、死亡、戦闘終了、Disableで判定・移動・非表示が残らないか確認します。
8. 最後に通常EnemyDecisionConfigへ戻して既存4単発＋UAL2コンボ＋新2種を確認し、Playerコンボ・JustAvoid追撃の回帰を確認します。必要なら作業前のComboTest overrideへ戻してください。

ゲーム上の自然さ・見切りやすさを含む完成判断は、このPlay Mode確認後に行います。
