# UAL2 RegularCombo / HeavyCombo 実装報告

2026-10-18。ローカルUnity 6000.2.7f2を調査・変更。GameSceneのPlay Modeは起動していません。実戦での完成判断はユーザーのPlay Modeテスト後です。突進・背後ワープは対象外です。

## 1. 作業開始時の状態・保護

- Enemy.controllerには手動追加のLightCombo / HeavyComboがあり、UAL2通常版からの編集可能なClipが割り当てられていました。専用の命中EventとAttackRootMotionStateは未設定でした。
- 割り当てClipの実名は`Armature_Sword_Regular_Combo` / `Armature_Sword_Heavy_Combo`。FBX内の実名は`Armature|Sword_Regular_Combo` / `Armature|Sword_Heavy_Combo`です。
- 今回参照した通常版UAL2_Standard.fbxは既にHumanoid設定済み、RM版UAL2_Standard_RM.fbxはGeneric / NoAvatar / Root Motion Node未指定でした。ARPGPack内には別のGeneric版も存在します。
- EnemyDecisionConfigとGameSceneのComboTest overrideには、旧TwoHitComboへの選択参照が残っていました。
- 開始時のgit status / diffを確認し、変更・未追跡ファイルを`Temp/UAL2Combo-StartSnapshot/`へ保護しました（ローカル・Git対象外）。Scene、フォント等の既存変更を含みます。作業開始時の差分は同フォルダのStartDiff.patchです。
- GameSceneとKatsuroUIFont.assetの保存済みファイルは開始時とSHA256一致。既存Playerコンボ・JustAvoid追撃のソースや資産は変更していません。Sceneの保存は行っていません。現在EditorのGameSceneには未保存マークがあるため、既存の未保存作業を含めてそのまま保持しています。

## 2. 旧2段コンボの整理

EnemyAttackPatternとRunnerを「1本のClip＋複数の命中区間＋Recovery」へ整理し、AttackSteps / ComboInterval / ComboStepFinishedの連結処理を除去しました。旧TwoHitComboのPattern、Step1/2データ、ARPGコピーClipを、参照移行・依存確認後に削除しました。旧EnemyCombo1/2とLightComboの不要Triggerも削除しています。

Assets/Mockおよび.gitignoreにTwoHitCombo、EnemyCombo1/2、ComboStepFinished、AttackStepsの残存参照はありません。過去作業のArtifacts/AKI123や開始時バックアップには履歴として残ります。一般のARPG元アセットは削除していません。

## 3. 採用Clip・Animator

| 攻撃 | 敵専用Clip | Animator State / Trigger | 長さ |
|---|---|---|---:|
| RegularCombo | Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2RegularCombo.anim | Base Layer.Attacks.RegularCombo / RegularCombo | 3.00秒 |
| HeavyCombo | Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2HeavyCombo.anim | Base Layer.Attacks.HeavyCombo / HeavyCombo | 4.33秒 |

両方ともRM版をHumanoid化して書き出した独立した.animです。現在のEnemy.prefabの有効なHumanoid Avatarでリターゲット・サンプリング可能です。元FBXとユーザーが作った通常版Clipは変更していません。

StateはEnemyComboタグ、AttackRootMotionStateを持ち、AnyStateから専用Triggerで遷移（0.08秒、自己遷移なし）。速度はFloatパラメータEnemyComboPlaybackSpeedを使用します。自動Exitで早期に移動・AIを解放せず、終了Event後にRecoveryを経てIdleへ戻します。LoopはOFFです。既存単発4攻撃は独立した従来の実行経路を維持します。

## 4. 斬撃数・Hitbox区間

Enemy Avatar上で骨格と刀の軌跡を60Hzでサンプルし、Regularは主斬撃3回、Heavyは斬り上げを含む4回として設定しました。区間はClip先頭からの秒数・再生速度1の値です。実画面での最終タイミング調整は未実施です。

| 攻撃 | 斬撃1 | 斬撃2 | 斬撃3 | 斬撃4 | 終了通知 |
|---|---|---|---|---|---|
| Regular | 0.17–0.32 | 0.65–0.80 | 1.45–1.67 | — | 2.95 |
| Heavy | 0.28–0.50 | 1.05–1.30 | 1.72–1.95 | 2.43–2.73 | 約4.283 |

各区間にComboEnableWeaponHitbox / ComboDisableWeaponHitboxと区間番号を設定し、開始時にComboSound、最後にComboFinishedを1回だけ通知します。Runnerは区間数を決め打ちしません。連番の開始のみ受理し、同じONの重複で命中履歴をリセットしないようにしています。区間内の同一IDamageableには1回だけ、次区間では再度命中可能です。

全区間で各ComboAttackのDamage / Strengthを使用します。Regularは5×3＝基準15、Heavyは6×4＝基準24。実戦ダメージには既存RunSession補正が加わります。各斬撃を別々のDamage値にする方式ではありません。

## 5. Root MotionのImport設定・抽出

元のGeneric RM ClipはhumanMotion=false、hasRootCurves=falseで、root骨のローカル移動があるだけの状態でした。そのままEnemy Avatarへ割り当てて移動を強制する方式は採用していません。

RM FBXを一時コピーし、ユーザーが既に設定していた通常版のhumanDescriptionを適用してHuman / CreateFromThisModelで再Importしました。有効なHumanoid Avatarを確認し、必要な2Clipを編集可能な.animへ書き出しました。独立Clipの依存が自身のみであることを確認して一時FBXコピーは削除しています。設定証跡はHumanoidRMImporter.meta.txt、変換手順はPrepareUALHumanoid.cs / BuildUALCombos.csに残しています。

最終Clip設定は、Root RotationをBake Into Pose、Based Upon Original・Y Offset 180°、Root YをBake Into Pose・Original、Root XZをBakeしない設定です。HumanoidのBody / Root Transformから抽出するため、GenericのRoot Motion Node指定には依存しません。方向補正なしでは後方・斜めへ出るため、このClip固有の向き補正を入れています。

敵の向きをidentity（前方+Z）にしたEdit ModeのAnimator.Updateで取得したdeltaPositionの全Clip合計：

| 攻撃 | X | Y | Z | 最大絶対Y |
|---|---:|---:|---:|---:|
| Regular | -0.004307 | 0 | 3.025489 | 0 |
| Heavy | 0.002397 | 0 | 6.887328 | 0 |

applyRootMotion=false / true双方で同じ抽出結果を確認しました。これはAnimatorのネイティブ移動抽出結果です。単にroot骨のカーブを読み取った推測値ではありません。GameSceneでの最終的な向き・足滑り・段差での見た目は未確認です。

## 6. Root Motion倍率と所有権

Regularは0.18、Heavyは0.10。全Clipの抽出量に倍率をかけると前進約0.54m / 0.69mが初期目安です。実移動量は遷移、終了時刻、衝突、NavMesh境界で変わります。既存単発攻撃の共通倍率0.18は変更していません。

Pattern開始時だけEnemyMoverへ倍率overrideを渡し、終了・キャンセル・異常復帰で解除します。移動所有権は既存の攻撃holdを使い、Agentを無効化してAnimatorの自動適用を止め、deltaPositionを水平成分に限定して蓄積、FixedUpdateでRigidbody.MovePositionへ渡します。既存AttackRootMotionPhysicsのSweepとNavMesh境界制限を通します。終了時は保留移動を破棄し、Agentを再有効化・Warpして同期します。

Edit Modeの一時Sceneで既存Sweepを実行し、要求Z=3mに対し壁で1.01m、プレイヤー相当Capsuleで0.81mへ制限、入力Yも0へ制限されました。一時Sceneは保存せず閉じています。GameSceneの実Collider構成、NavMesh再同期、動く障害物への実戦動作はユーザーテスト対象です。

## 7. Inspectorでの調整先

Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/内のRegularCombo.asset / HeavyCombo.asset：

| 設定 | Regular | Heavy |
|---|---:|---:|
| Windup（Clip前の待機） | 0.05秒 | 0.15秒 |
| Playback Speed | 1.0 | 1.0 |
| Root Motion Scale | 0.18 | 0.10 |
| Recovery Duration（Clip終了後の追加硬直） | 0.40秒 | 0.80秒 |
| Timeout（速度1での終了欠落対策） | 5秒 | 7秒 |

Attack参照先RegularComboAttack.asset / HeavyComboAttack.assetでDamageとStrengthを調整できます。RegularはLight、HeavyはHeavyとして既存のHitStop / Trail / CombatFeedback / HitReactionへ渡します。共通Feedback値は変更していません。Clip内の既存回復動作にも追加Recoveryが乗るため、実際の反撃猶予を見て調整してください。速度1のまま開始し、極端な速度変更はしていません。

## 8. AIの選択方法

EnemyDecisionConfigのNear CandidatesにAction=Slash、Attack=None、Pattern=RegularComboまたはHeavyComboの独立した行を設定します。各行のWeightで個別調整します。RepeatPenaltyもPatternの同一性を区別します。

通常Configは単発4種のWeightを維持し、旧コンボのWeight 14.285714をRegular 9.523809 / Heavy 4.761904へ2:1で分配しました。全体の攻撃カテゴリの重み合計は維持しています。

現在GameSceneの既存overrideはEnemyDecisionConfig_ComboTestを参照しています。この参照は維持し、同Configの旧コンボ行をRegular / Heavyへ1:1で分配しました（各約7.142857）。単発4種とStepBackのWeight=0もユーザーのテスト設定のままです。

Windup、再生中、Recovery中はBusyを維持。通常被弾は従来通り継続、JustAvoid追撃での怯み・死亡・戦闘終了・Disable・明示中断ではHitbox、保留Root Motion、Patternを解除します。終了Event欠落はTimeoutで復帰します。HitStop中は進行タイマーを止めます。

## 9. 検証結果と限界

- C#コンパイル完了、scriptCompilationFailed=false、isCompiling=false、isPlaying=falseを確認。
- ValidateUAL.csの87項目成功。参照、4単発＋2コンボの個別抽選、State設定、Event整合、区間内重複防止、Recovery中Busy、完了1回、通常Heavy継続、HitStop停止、JustAvoid中断、死亡・戦闘終了・Disable・強制中断・全Event欠落を検証しました。
- 実AnimatorのState時間に合わせてClip内EventをControllerへ明示通知し、実EnemyAttackerへ各区間2回ずつ命中を入力して区間ごと1回・Strength・基準総Damageを確認しました。Edit ModeではAnimation Event / StateBehaviour / OnAnimatorMoveがPlay Mode同様に自動通知されないため、この接続テストは明示呼出しです。自動通知による実戦動作を検証済みとはしていません。
- 実ClipからのdeltaPosition抽出はネイティブAnimator.Updateで別途検証しています。Moverへの明示入力で倍率・水平移動・解除も検証しました。Rigidbody / NavMeshAgentを含むGameScene全体の物理更新は実行していません。
- 既存単発4種の選択参照を確認。単発Clip・Playerコンボ・JustAvoid追撃資産は維持しています。全ゲームプレイの回帰完了を意味するものではありません。
- 保存済みGameScene・フォントの保護照合結果はProtectedChanges.txt。詳細はValidation.txt、ValidationResult.json、FinalAudit.txt、DeltaExtraction.txt、CollisionAudit.txt、骨格画像を参照。

## 10. Unityでの確認手順

1. GameSceneのEnemyでDecision Config overrideがComboTestになっていることを確認。テスト用ConfigのRegular Weightだけを正、Heavyを0にしてPlayし、Regularを単独確認します。続いて逆にしてHeavyを確認します。調整前の値は両方約7.142857です。
2. 敵正面で各3回 / 4回の斬撃・刀Collider・Trailが一致するか確認します。同じ区間で複数Colliderに触れても1回、次の斬撃では再度ダメージになるかを確認します。基準Damageは5 / 6、総量は15 / 24に既存補正が加わります。
3. 各斬撃を回避・JustAvoidし、特にHeavyの斬り上げ（1.72–1.95秒）が視認・回避可能か確認します。Light / HeavyのHitStopと被弾表現の差も確認します。
4. 正面、横向き、壁際、プレイヤー密着、障害物際、NavMesh端で攻撃させ、前進方向、Yの浮き沈み、足滑り、貫通、終了時の位置跳びを確認します。必要ならPatternのRoot Motion Scaleを小刻みに調整します。
5. 全段空振り後に、RegularよりHeavyの反撃猶予が長いか、Recovery後に追跡・次行動が再開するか確認します。
6. コンボ中の通常被弾継続、JustAvoid追撃での中断、死亡・戦闘終了・Disable後にHitboxやBusyが残らないか確認します。
7. 回帰確認時はEnemyのDecision Configを通常のEnemyDecisionConfigへ一時変更し、単発4種と2コンボが選ばれること、PlayerコンボとJustAvoid追撃が従来通りであることを確認します。終了後は希望のConfig / Weightへ戻してください。

以上の実戦確認後に、見た目・実命中・回避タイミング・足滑りを含む完成判断を行います。
