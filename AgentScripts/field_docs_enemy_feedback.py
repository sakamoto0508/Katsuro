from serialized_field_docs import *
add('EnemyAnimationController','''
_animName=Enemy Animatorで使う移動・攻撃・死亡パラメータ名の設定。
_reactionLayerName=Just Avoid追撃の大きなHitReactionを再生するAnimator Layer名。
_lightHitDuration=Light用の大Reaction時間設定（秒）。通常Lightでは大Reactionを要求しない仕様。
_heavyHitDuration=Just Avoid追撃の大Reactionを維持する時間（秒）。
_lightHitWeight=Light用Reaction Layer Weight。通常Lightでは大Reactionを要求しない仕様。
_reactionBlendIn=大Reactionへ入る短いBlend時間（秒）。Poseの急変を抑える。
_reactionBlendOut=大Reactionから通常状態へ戻すBlend時間（秒）。
''')
add('EnemyAttackData','''
_actionType=この攻撃設定を使うEnemy行動の種類。
_animatorTrigger=この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。
_damage=この行動の基準ダメージ。攻撃開始時に武器へ渡す。
_range=行動の想定射程（Unity単位）。現在の攻撃実行処理では直接参照しない設定。
_hitboxIndex=有効にする武器Colliderの番号。現在の攻撃実行処理では直接参照しない設定。
_hitboxEnableDelay=Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。
_hitboxDisableDelay=Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。
''')
add('EnemyDecisionConfig','''
FarDistance=遠距離行動候補へ切り替えるPlayerとの距離（Unity単位）。
NearDistance=近距離行動候補へ切り替えるPlayerとの距離（Unity単位）。
ObserveSeconds=Wait行動で様子を見る時間（秒）。
ReconsiderInterval=行動を再検討する間隔の設定（秒）。利用状況はAI側の判断処理に従う。
WeightWarpAttack=旧形式のWarp攻撃の抽選重み。現行は距離帯別Candidates一覧を使う。
WeightApproach=旧形式の接近行動の抽選重み。現行は距離帯別Candidates一覧を使う。
WeightRush=旧形式の突進行動の抽選重み。現行は距離帯別Candidates一覧を使う。
WeightObserve=旧形式の待機行動の抽選重み。現行は距離帯別Candidates一覧を使う。
WeightSlash=旧形式の斬撃行動の抽選重み。現行は距離帯別Candidates一覧を使う。
WeightBackstep=旧形式の後退行動の抽選重み。現行は距離帯別Candidates一覧を使う。
Action=この抽選候補が実行するEnemy行動の種類。
Weight=この候補の抽選重み。大きいほど選ばれやすく、直前行動にはRepeatPenaltyを掛ける。
''')
add('EnemyStuts','''
_enemyPower=Enemy武器を初期化する基準攻撃力。行動別Damageを使う場合はその値が優先される。
_enemyMaxHealth=Enemyの最大HP。体力初期化とHP比率の基準。
_chaseStartDistance=追跡開始距離の基礎設定（Unity単位）。距離帯別の行動判断はEnemyDecisionConfigを使用する。
_stopDistance=Playerへ近づく際のNavMeshAgent停止距離（Unity単位）。
_destinationUpdateInterval=追跡中にNavMeshAgentの目的地を更新する間隔（秒）。
FarDistance=旧形式の遠距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。
NearDistance=旧形式の近距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。
ObserveSeconds=旧形式の様子見時間（秒）。現行の判断はEnemyDecisionConfigを使用する。
WeightWarpBehind=旧形式の背後Warp行動の重み。現行の判断はEnemyDecisionConfigを使用する。
WeightApproach=旧形式の接近行動の重み。現行の判断はEnemyDecisionConfigを使用する。
WeightRush=旧形式の突進行動の重み。現行の判断はEnemyDecisionConfigを使用する。
WeightObserve=旧形式の様子見行動の重み。現行の判断はEnemyDecisionConfigを使用する。
WeightMelee=旧形式の近接攻撃の重み。現行の判断はEnemyDecisionConfigを使用する。
WeightBackstep=旧形式の後退行動の重み。現行の判断はEnemyDecisionConfigを使用する。
_rotateSmoothTime=旧回転補間の時間設定（秒）。Smooth旋回はTurnSpeedによる角速度制御を使う。
RotationMode=Enemyの向きの制御方式。Agent任せ・即時旋回・角速度で滑らかに旋回から選ぶ。
''')
add('EnemyController','''
_enemyStuts=EnemyのHP・基準攻撃力・NavMesh移動・旋回の設定参照。
_animName=Enemy Animatorのパラメータ名を共有する設定参照。
_enemyWeaponColliders=Enemyの武器攻撃判定Collider。Animation Eventと死亡処理でON/OFFする。
_playerWeaponColliders=Player武器との接触を調整するために参照するCollider一覧。
_animator=Enemy本体のAnimator。攻撃・移動・死亡・Root Motionの状態を取得する。
_attackData=行動種類に対応する攻撃Trigger・Damageの設定一覧。
_decisionConfig=距離帯別の行動候補・抽選重みを定義したAI設定。
_stepBackDistance=後退行動でPlayerから離れる目標距離（Unity単位）。
_characterEffect=Enemyの登録済み見た目Effectを再生するコンポーネント参照。
_enemyDeadDelay=旧死亡待機の設定（ミリ秒）。現行の死亡・Final Blow処理では直接参照しない。
_attackRootMotionScale=Enemy攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。
''')
add('SampleEnemyAttacks','''
attacks=Enemy攻撃設定のサンプル参照一覧。各要素にEnemyAttackDataを割り当てる。
''')
add('CombatFeedback','''
_ghostTint=幽体化中の体色と透明度。
_ghostSway=幽体化中の輪郭の揺れ幅。大きいほど揺れが目立つ。
_ghostFlowSpeed=幽体化の輪郭を流す速度倍率。
_afterImageDuration=Just Avoid成功時の固定残像を表示する時間（秒）。
_flashDuration=Just Avoid成功時の残像Flashの継続時間（秒）。
_afterImageTint=Just Avoid成功時の固定残像の色と初期透明度。
_flashTint=Just Avoid成功時の残像Flashの色と初期透明度。
_hitFlashDuration=命中時に体へ加えるFlashの継続時間（秒）。
_justAvoidFlashDuration=Just Avoid成功時の小さなFlashの継続時間（秒）。
_justAvoidFlashSize=Just Avoid成功時の小さなFlashのサイズ（Unity単位）。
_lightReactionAngle=通常Light命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。
_heavyReactionAngle=通常Heavy命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。
_reactionDuration=通常命中の局所的な体の反応時間（秒）。
_enemyHeavyReactionDuration=Enemyが通常Heavyを受けた際の局所反応時間（秒）。
_enemyAttackReactionScale=Enemy攻撃中の局所反応の強さに掛ける倍率。攻撃Poseの崩れを抑える。
_enemyLightFlashStrength=EnemyへLightが命中した際の体Flashの強さ。
_enemyHeavyFlashStrength=EnemyへHeavyが命中した際の体Flashの強さ。
_justAvoidBodyFlashStrength=Just Avoid成功時に体へ加えるFlashの強さ。
''')
add('DamageNumbers','''
_container=ダメージ数字のUIを生成・配置する親RectTransform。
_normalTemplate=通常命中のダメージ数字に使うTMPテンプレート。
_criticalTemplate=強い命中のダメージ数字に使う必須TMPテンプレート。通常用と両方割り当てる。
_camera=命中のワールド位置を画面座標へ変換するCamera。
_lifetime=ダメージ数字を表示して消すまでの時間（秒）。
_worldOffset=命中位置へ加えるワールド座標の表示ずらし（Unity単位）。
_screenOffset=画面へ投影した後に加えるUI座標の表示ずらし。
_riseSpeed=数字の投影元ワールド位置が上昇する速度（Unity単位/秒）。その位置を毎フレーム画面へ投影する。
_poolSize=初期化時に用意するダメージ数字のプール数。
''')
for owner,purpose in [('JustAvoidContrast','Just Avoid成功'),('VictoryContrast','勝利')]:
    add(owner,f'''
_volume={purpose}専用のHDRP Volume。通常Profileを書き換えずWeightでモノクロを制御する。
_enter={purpose}から完全モノクロへ移行する時間（実時間の秒）。
_hold=完全モノクロを保持する時間（実時間の秒）。移行時間とは別に加算する。
_restore=完全モノクロから元の色へ戻す時間（実時間の秒）。
''')
add('JustAvoidScreenDistortion','''
_material=Just Avoid画面歪みのCustom Pass描画に使うMaterial参照。
''')
add('NormalAttackSpeedState','''
_speedParameter=この攻撃State専用のAnimator速度Floatパラメータ名。他のコンボ段階と共有しない。
_windupSpeed=振りかぶり区間のアニメーション速度倍率。1でClip本来の速度。
_slashSpeed=斬撃区間のアニメーション速度倍率。1でClip本来の速度。
_recoverySpeed=攻撃後の回復区間のアニメーション速度倍率。1でClip本来の速度。
_slashStart=斬撃速度へ切り替えるClipの正規化時刻（0〜1）。
_slashEnd=斬撃区間が終わるClipの正規化時刻（0〜1）。ここから回復速度へ補間する。
_recoveryStart=回復速度への補間が完了するClipの正規化時刻（0〜1）。
''')
add('SwordTrail','''
_duration=通常攻撃の刀軌跡を残す時間（秒）。
_bladeBase=刀身の根元を示すTransform。刃先との間を軌跡の幅として使う。
_tip=刀の刃先を示すTransform。刀身の軌跡の外側を生成する。
_trailMaterial=刀軌跡を描画するMaterial参照。未設定なら対応ShaderからMaterialを生成する。
_brightness=通常攻撃の軌跡の明るさ倍率。
_fade=時間経過で軌跡を消すFade曲線の指数。
_color=刀軌跡の基準色と透明度。
_minimumSpeed=軌跡を出し始める刀の最低速度（Unity単位/秒）。低速の三角形を抑える。
_fullSpeed=軌跡の表示強度が最大になる刀の速度（Unity単位/秒）。
_heavyDuration=Heavy攻撃の刀軌跡を残す時間（秒）。
_counterDuration=Just Avoid追撃の刀軌跡を残す時間（秒）。
_heavyBrightness=Heavy攻撃の軌跡の明るさ倍率。
_counterBrightness=Just Avoid追撃の軌跡の明るさ倍率。
_width=刃先Transform未設定時だけ使う旧互換の軌跡端点距離（Unity単位）。通常Inspectorでは非表示。
''')
save()
