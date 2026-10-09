from serialized_field_docs import *
add('AudioManager','''
bgmSource=単一BGM Sourceの互換参照。複数BGMチャンネルの初期化に使用する。
bgmSources=BGMを再生するAudioSource一覧。各チャンネルの基準音量へ一時Ducking倍率を掛ける。
sfxSourcePrefab=SEプールを生成するAudioSource Prefab。SE再生用のSource設定を引き継ぐ。
bgmList=BGMの登録名とAudioClipの対応一覧。登録名からClipを検索する。
seList=SEの登録名とAudioClipの対応一覧。AudioConfigやAnimation Eventの名前と一致させる。
sfxPoolSize=初期化時に用意するSE用AudioSource数。
maxSfxPoolSize=SE用AudioSourceプールの最大数。同時再生で増やせる上限。
_audioConfig=タイトル・戦闘・命中などの再生登録名を共有する設定。
_audioListener=敗北演出のローパスを適用するAudioListener参照。
_bgmChannelCount=初期化で確保するBGMチャンネル数。複数BGMの切り替えに使用する。
_justAvoidDuckVolume=Just Avoid成功中のBGM音量倍率。0で無音、1で基準音量。SEには適用しない。
_justAvoidDuckEnter=BGMが一時音量へ下がる時間（実時間の秒）。
_justAvoidDuckHold=Just Avoidの一時BGM音量を保持する時間（実時間の秒）。
_justAvoidDuckRestore=BGMが基準音量へ戻る時間（実時間の秒）。停止済みBGMを再開しない。
name=AudioManagerからClipを検索する登録名。AudioConfigやAnimation Eventの指定名と一致させる。
clip=この登録名で再生するAudioClip。
''')
add('FinalBlowManager','''
_audioConfig=Final Blow開始時に再生するEnemy死亡SEの登録名設定。
_player=勝利時に納刀・一時速度解除を要求するPlayer参照。
_enemyController=最後のHitStopと一時速度解除の対象Enemy参照。
_phase1HitStop=最終Hitの停止・スローを維持する時間（実時間の秒）。
_whiteFlashDuration=最終Hitの白Flashの継続時間（実時間の秒）。
_finalBlowText=納刀後に表示する討伐文字のTMP参照。
_finalBlowTextFadeIn=討伐文字のFadeとScale演出の時間（実時間の秒）。
_presentation=白Flash・HUD・黒帯・討伐文字を進行させるUI演出コンポーネント。
_cameraFeedback=勝利専用構図・FOV・モノクロの開始と解除を要求するCameraManager参照。
_cameraPushDelay=最終Hitから勝利カメラとFOVの寄りを始めるまでの時間（実時間の秒）。
_sheathingDelay=最終Hitから納刀Triggerを要求するまでの時間（実時間の秒）。カメラDelay以上で開始する。
_textDelay=最終Hitから討伐文字を表示するまでの時間（実時間の秒）。納刀の見せ場に合わせる。
''')
add('GameManager','''
_inputBuffer=Player入力通知と戦闘中の入力受付を管理する参照。
_playerPosition=初期化時にカメラとEnemyへ渡すPlayerの位置参照。
_playerController=Playerの初期化・入力・戦闘状態を管理する参照。
_playerAnimationController=PlayerのAnimation Event・パラメータ制御の参照。
_animationName=Playerと関連処理で使うAnimatorパラメータ名の設定。
_cameraConfig=通常追従・Lock-On・命中カメラの調整値。
_audioConfig=タイトル・戦闘・効果音の登録名設定。
_enemyPosition=Lock-Onカメラが注視するEnemyの位置参照。
_enemyController=Enemyの初期化と戦闘終了処理を管理する参照。
_cameraManager=通常・Lock-On・勝利カメラの制御を接続する参照。
_camera=画面に描画する出力Camera。入力の向きやUI投影にも使用する。
_cinemachineCamera=通常戦闘のCinemachineCamera参照。
_cinemachineLockOncamera=Lock-On用CinemachineCameraの設定参照。
_loadSceneManager=戦闘終了時などのSceneロードを接続する参照。
_damageNumbers=命中時ダメージ数字を表示するコンポーネント参照。
_soundVolume=GameManagerがBGM再生を要求する際の基準音量。0で無音、1で最大。
_state=現在のゲーム進行状態。タイトル・戦闘・勝利・敗北の入力受付と演出を分ける。
''')
add('GlobalFader','''
fadeImage=Scene切り替え時に画面を覆うFade用Image。Alphaで暗転を制御する。
duration=Scene切り替えの暗転・明転それぞれの時間（実時間の秒）。
''')
add('HitStopManager','''
_hitStopTime=通常Light命中のHitStop時間（実時間の秒）。
_lastHitStopTime=最終Hit用に公開するHitStop時間設定（実時間の秒）。FinalBlowManagerには専用時間設定もある。
_heavyHitStop=通常Heavy命中のHitStop時間（実時間の秒）。
''')
add('LoadSceneManager','''
_sceneNameConfig=タイトルと戦闘Sceneのロード名を共有する設定参照。
''')
add('PlayerDeadManager','''
_playerController=敗北時の死亡Triggerとスローの対象Player参照。
_enemyController=敗北時のスローを適用するEnemy参照。
_vignetteFadeIn=敗北時の画面周辺の赤みと暗さが立ち上がる時間（実時間の秒）。
_vignetteColor=敗北用の画面周辺オーバーレイの色。
_vignetteMaxAlpha=敗北用オーバーレイの最大不透明度。0で透明、1で不透明。
_slowDuration=敗北時のAnimatorスローを維持する時間（実時間の秒）。
_slowSpeed=敗北時のAnimator速度倍率。0で停止、1で通常速度。TimeScaleは変更しない。
_silenceDuration=敗北のスロー後、全Audioを停止して待つ時間（実時間の秒）。
_lowPassCutoff=敗北時のローパスのカットオフ周波数（Hz）。低いほど音がこもる。
_volume=敗北時にVignetteを制御するHDRP Volume参照。
_smoothTime=旧補間時間設定（秒）。現行の敗北演出では直接参照しない。
_intensity=敗北時のHDRP Vignetteの目標強度。
_smoothness=敗北時のHDRP Vignetteの縁の滑らかさ。0〜1。
_deadText=敗北時に表示する死亡文字のTMP参照。
_deadTextFadeIn=死亡文字が不透明になるまでの時間（実時間の秒）。
_ease=死亡文字のFadeに適用するDOTweenの補間曲線。
''')
add('TitleManager','''
_audioConfig=タイトルBGMと開始SEの登録名設定。
_sceneNameConfig=開始後にロードする戦闘Scene名の設定。
_transitionDelay=開始操作からScene切り替えまでの待機時間（秒）。
_setup=名前・装備選択を開くRun設定画面の参照。
''')
add('PlayerController','''
_attackRootMotionScale=Player攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。
_playerWeapon=抜刀・納刀に合わせて表示を切り替える刀のMeshRenderer。
_playerStartWeapon=納刀中の刀の見た目を切り替えるGameObject参照。
_weaponColliders=Player武器の攻撃判定Collider一覧。Animation Eventで有効・無効を切り替える。
_enemyWeaponColliders=接触調整で参照するEnemy武器のCollider一覧。
_playerStatus=PlayerのHP・移動速度・ゲージ・能力コストの基礎設定。
_animationName=Player Animatorのパラメータ名を共有する設定。
_playerStateConfig=Playerの回避受付時間・幽体化・コンボClipの設定。
_passiveBuffSet=装備による攻撃倍率・加算威力・命中Effectの設定。
_vfxConfig=命中・Just Avoid・回復・バフなどの見た目演出設定。
_justAvoidSlowDef=Just Avoid成功時に適用するスロー状態効果の定義。
_canAttack=攻撃入力を受け付けるための許可フラグ。falseならLight/Heavy入力を無視する。
_playerHudView=PlayerのHP・ダメージ追従・ゲージを表示するHUD参照。
''')
add('PlayerAnimationController','''
_animName=Player Animatorの移動・攻撃・回避・抜刀/納刀パラメータ名の設定。
''')
add('JustAvoidCounterAnimation','''
_counterWindupSpeed=Just Avoid追撃の振りかぶり区間のAnimator速度倍率。
_counterSlashSpeed=Just Avoid追撃の斬撃区間のAnimator速度倍率。
''')
save()
