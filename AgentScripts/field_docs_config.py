from serialized_field_docs import *
add('AnimationName','''
_moveVelocity=移動速度をAnimatorへ渡すFloatパラメータ名。
_moveVectorX=移動方向の左右成分をAnimatorへ渡すFloatパラメータ名。
_moveVectorY=移動方向の前後成分をAnimatorへ渡すFloatパラメータ名。
_isDrawingSword=抜刀中の状態をAnimatorへ伝えるパラメータ名。Controller側と一致させる。
_isLockOn=Lock-On状態をAnimatorへ伝えるBoolパラメータ名。
_lightAttack=通常Light攻撃を開始するAnimator Trigger名。
_strongAttack=通常Heavy攻撃を開始するAnimator Trigger名。
_justAvoidAttack=Just Avoid成功後の追撃を開始するAnimator Trigger名。
_comboStep=攻撃のコンボ段階を指定するAnimator Intパラメータ名。
_backStep=バックステップを開始するAnimator Trigger名。
_isSwordDrawn=刀を抜いている状態をAnimatorへ伝えるBoolパラメータ名。
_justAvoid=Just Avoid成功演出を開始するAnimator Trigger名。
_justAvoidWindow=Just Avoid受付状態をAnimatorへ伝えるパラメータ名。
_enemyDead=Enemy死亡アニメーションを開始するAnimator Trigger名。
_playerDead=Player死亡アニメーションを開始するAnimator Trigger名。
_swordSheathing=勝利時の納刀アニメーションを開始するAnimator Trigger名。
''')
add('AudioConfig','''
_lightHitSound=通常Light命中時にAudioManagerのSE一覧から検索する登録名。
_heavyHitSound=通常Heavy命中時にAudioManagerのSE一覧から検索する登録名。
_titleBGM=タイトル画面で再生するBGMの登録名。AudioManagerの一覧と一致させる。
_startSE=ゲーム開始操作で再生するSEの登録名。
_inGameBGM=戦闘中に再生するBGMの登録名。
_attackSound=攻撃時に再生するSEの登録名。
_hitSound=被弾時に再生する共通SEの登録名。
_plaeyrDeadSound=Player死亡時に再生するSEの登録名。
_enemyDeadSound=Final Blow開始時に再生するEnemy死亡SEの登録名。
''')
add('CameraConfig','''
_lightHitCameraStrength=Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。
_heavyHitCameraStrength=Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。
_cameraDuration=命中時カメラ衝撃の継続時間（秒）。
_justAvoidFOVOffset=Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。
_justAvoidFOVDuration=Just Avoidの画角演出を往復させる時間（秒）。
_cameraDistance=追従カメラとPlayerの基準水平距離（Unity単位）。
_cameraHeight=Player位置からの追従カメラの基準高さ（Unity単位）。
_positionSmooth=カメラ位置の追従補間係数。大きいほど目標位置へ速く追従する。
_rotationSmooth=カメラ回転の追従補間係数。大きいほど目標方向へ速く追従する。
_lookAtHeight=Player位置から注視点までの高さ（Unity単位）。
_cameraCollisionRadius=カメラ障害物判定のSphereCast半径（Unity単位）。
''')
add('LowHpBuffTable','''
_tiers=HP比率に応じて選ぶ低HPボーナスの段階一覧。
''')
add('PlayerPassiveBuffSet','''
_buffs=装備による攻撃倍率・固定加算威力・任意の命中Effectの一覧。
_label=Inspector上で装備ボーナスを識別する表示用ラベル。
_attackPowerMultiplier=この装備の与ダメージ倍率。1で補正なし。他の装備倍率と乗算する。
_flatAttackBonus=この装備が加える固定攻撃力。他の装備の加算値と合算する。
_onHitEffectPrefab=この装備の命中時に使うEffect Prefab参照。未設定も許容する。
''')
add('PlayerStateConfig','''
_clips=コンボ段階順のAnimation Clip一覧。段階番号に対応する要素を再生する。
_comboWindowDelaySeconds=段階ごとの追加コンボ受付開始までの遅延（秒）。未設定・負数は代替値を使う。
_clipFlatDamage=段階ごとの固定ダメージ。未設定なら代替値を使い、設定済みのゼロ・負数はそのまま返す。
_maxSkillGauge=スキルゲージ最大値の設定。実際のPlayerゲージ初期化ではPlayerStatus側の最大値を使用する。
_skillGaugeRecoveryPerSecond=PlayerStatus未設定時に使うスキルゲージの代替自然回復量（ゲージ単位/秒）。
_justAvoidTime=回避開始後のJust Avoid判定を受け付ける時間（秒）。
_ghostColor=幽体化中の見た目に使用する色。
_alpha=幽体化中の見た目の透明度。0で透明、1で不透明。
_defaultLightComboWindowDelay=Lightの段階別受付遅延が未設定の場合に使うコンボ受付開始時間（秒）。
_defaultStrongComboWindowDelay=Heavyの段階別受付遅延が未設定の場合に使うコンボ受付開始時間（秒）。
_lightAttackClips=非Lock-On時のLightコンボClip・受付遅延・固定ダメージ設定。
_lockOnLightAttackClips=Lock-On時のLightコンボ設定。未設定時は通常Light設定を使う。
_strongAttackClips=HeavyコンボのClip・受付遅延・固定ダメージ設定。
_justAvoidAttackClips=Just Avoid追撃のClip・受付遅延・固定ダメージ設定。
''')
add('PlayerStatus','''
_life=Playerの初期残機設定。Runの進行ではRunSessionの残機を使用する。
_maxHealth=Playerの最大HP。体力初期化とHP比率計算の基準。
_attackPower=Playerの基準攻撃力。攻撃Clipや装備・バフのダメージ計算で参照する。
_noWeaponMoveSpeed=納刀中の通常移動速度（Unity単位/秒）。
_noWeaponSprintSpeed=納刀中の疾走速度（Unity単位/秒）。
_unLockWalkSpeed=抜刀・非Lock-On時の歩行速度（Unity単位/秒）。
_unLockSprintSpeed=抜刀・非Lock-On時の疾走速度（Unity単位/秒）。
_lockOnWalkSpeed=Lock-On中の歩行速度（Unity単位/秒）。
_lockOnSprintSpeed=Lock-On中の疾走速度（Unity単位/秒）。
_rotationSmoothness=Playerが移動方向へ向く回転補間係数。大きいほど素早く向きを合わせる。
_acceleration=通常移動で目標速度へ近づける加速係数。
_breakForce=移動入力がなくなったときに速度を落とす減速係数。
_maxSkillGauge=Playerスキルゲージの最大値。初期化時に使用する。
_skillGaugeLockoutThresholdNormalized=ゲージ消費行動の受付制限に使う正規化閾値。0〜1で最大ゲージに対する比率。
_skillGaugePassiveRecoveryPerSecond=スキルゲージの基準自然回復量（ゲージ単位/秒）。
_skillGaugeOnAttackGain=攻撃命中時の基本ゲージ回復量（ゲージ単位）。
_skillGaugeOnAvoidGain=回避時の基本ゲージ回復量（ゲージ単位）。
_skillGaugeOnJustAvoidBonus=Just Avoid成功時に追加するゲージ回復量（ゲージ単位）。
_skillGaugeCost=疾走・幽体化・自傷・回復などのゲージ消費設定。
_lowHpBuffTable=現在HP比率に応じた攻撃倍率・ゲージ回復補正の参照表。
_justAvoidBuffConfig=Just Avoid成功の累積攻撃ボーナスと最大スタックの設定。
_dashPerSecond=疾走中の毎秒ゲージ消費量（ゲージ単位/秒）。
_ghostActivationCost=幽体化を開始した瞬間に消費するゲージ量（ゲージ単位）。
_ghostPerSecondCost=幽体化を継続する毎秒ゲージ消費量（ゲージ単位/秒）。
_selfSacrificeGaugePerSecond=自傷中の毎秒ゲージ消費量（ゲージ単位/秒）。HP消費とは別に支払う。
_selfSacrificeMinHpRatio=自傷を継続できるHP比率の下限。0〜1で最大HPに対する比率。
_selfSacrificeDamagePercentPerSecond=自傷で毎秒失う最大HPの割合（%/秒）。1で最大HPの1%を毎秒失う。
_healGaugePerPercent=最大HPを1%回復するために消費するゲージ量（ゲージ単位/1%）。
_buffGaugePerSecond=バフ用の毎秒ゲージ消費設定。使用する行動側から参照するための値。
''')
add('SceneNameConfig','''
_titleScene=タイトルへ戻る際にロードするScene名。Build設定のScene名と一致させる。
_gameScene=戦闘開始時にロードするScene名。Build設定のScene名と一致させる。
''')
add('VFXConfig','''
_screenDistortionEnable=Just Avoid成功時の画面歪みを有効にする。
_distortionDuration=Just Avoid画面歪みが消えるまでの時間（秒）。
_distortionRingWidth=画面歪みのリング幅。画面UVを基準にした値。
_distortionStrength=リング付近の画面UVをずらす強さ。大きいほど歪みが強くなる。
_distortionNoiseStrength=画面歪みに重ねるノイズの強さ。
_distortionNoiseScale=画面歪みのノイズの細かさを調整する倍率。
_distortionFadePower=画面歪みの減衰曲線の指数。消え方の速さを調整する。
_distortionEdgeTint=画面歪みの縁に加える色。
_distortionEdgeTintStrength=画面歪みの縁色を加える強さ。
_shockwaveDuration=従来Particle Shockwaveの寿命（秒）。旧演出互換の設定。
_shockwaveEnable=従来Particle Shockwaveの有効設定。画面歪みとは別の旧演出設定。
_shockwaveStrength=従来Particle Shockwaveの強度設定。
_shockwaveSize=従来Particle Shockwaveのサイズ倍率。
_hitVFXDuration=命中VFXの再生後に片付けるまでの時間（秒）。
_heavyHitVFXSpread=Heavy命中VFXの粒子の広がり補正。
_heavyHitVFXSize=Heavy命中VFXの粒子サイズ補正。
_heavyHitVFXScale=Heavy命中VFXオブジェクト全体のScale倍率。
_heavyHitVFXSpeed=Heavy命中VFXの粒子速度補正。
_counterHitVFXSpeed=Just Avoid追撃命中VFXの粒子速度補正。
_counterHitVFXSize=Just Avoid追撃命中VFXの粒子サイズ補正。
_playEffectHeal=CharacterEffectに登録した回復Effectの再生名。
_playEffectGhost=CharacterEffectに登録した幽体化Effectの再生名。
_playEffectBuff=CharacterEffectに登録したバフEffectの再生名。
''')
add('CameraManager','''
_combatImpulse=命中時の揺れを発生させるCinemachine Impulse Source。
_finalBlowFOVOffset=Final Blow中に加える画角の差（度）。負数で画角を狭めて寄りを強調する。
_finalBlowPushDuration=Final Blowの画角が寄るまでの時間（秒）。
''')
add('VictoryCameraRig','''
_camera=勝利中だけOverrideで使用する専用CinemachineCamera。通常時は無効にして待機する。
_moveDuration=勝利カメラが開始位置から納刀構図へ移る時間（実時間の秒）。
_sideAngle=Player背面から側面へ回り込む角度（度）。腕と刀が見える構図を調整する。
_lookHeight=Player位置から勝利カメラの注視点までの高さ（Unity単位）。
_horizontalFraming=Playerの右方向への注視点のずらし量（Unity単位）。画面内の配置を調整する。
_obstacles=勝利カメラのSphereCastで障害物として扱うLayer。Player・Enemy自身は除外する。
''')
save()
