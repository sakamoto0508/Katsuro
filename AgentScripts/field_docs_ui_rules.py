from serialized_field_docs import *
add('GameplayRules','''
StartingLives=新しいRunを開始したときの残機数。
ReviveInvulnerability=復活後にダメージを無効化する時間（秒）。
ShortGhostDuration=短い幽体化回避を維持する時間（秒）。
GhostCooldown=幽体化回避を再使用できるまでの時間（秒）。
JustBuffDuration=Just Avoid成功で得る攻撃ボーナスの有効時間（秒）。
FullHpRegenMultiplier=満HP時の自然ゲージ回復倍率。低HP時の倍率からHP比率で補間する。
LowHpRegenMultiplier=HP比率が0のときの自然ゲージ回復倍率。満HP時の倍率まで補間する。
FullHpSelfCostMultiplier=満HP時の自傷ゲージ消費倍率。低HP時の倍率から補間する。
LowHpSelfCostMultiplier=HP比率が0のときの自傷ゲージ消費倍率。満HP時の倍率まで補間する。
FullHpAvoidMultiplier=満HP時のJust Avoid受付時間倍率。低HP時の倍率から補間する。
LowHpAvoidMultiplier=HP比率が0のときのJust Avoid受付時間倍率。満HP時の倍率まで補間する。
PowerAttack=攻撃装備「剛力」の与ダメージ倍率。1で補正なし。
SoulHitGain=攻撃装備「吸魂」の命中時ゲージ回復倍率。1で補正なし。
DesperationBonus=攻撃装備「窮地」の最大追加ダメージ倍率。HPが低いほど増え、0.35で最大35%増。
GuardDamage=防御装備「堅守」の被ダメージ倍率。0.8で通常の80%。
SpiritCost=防御装備「霊衣」の幽体化開始・継続ゲージ消費倍率。
BreathRegen=防御装備「息吹」の自然ゲージ回復倍率。
SoulBossDamage=前回勝者が「吸魂」の場合に継承Enemyへ適用する攻撃倍率。
SpiritBossSpeed=前回勝者が「霊衣」の場合に継承Enemyへ適用する移動速度倍率。
BreathBossHealth=前回勝者が「息吹」の場合に継承Enemyへ適用する最大HP倍率。
''')
add('RunSession','''
Version=保存した勝者データの形式バージョン。ロード時に互換性を判定する。
Name=次のRunの相手として引き継ぐ前回勝者の名前。
Attack=前回勝者の攻撃装備番号。AttackNamesの要素番号に対応する。
Defense=前回勝者の防御装備番号。DefenseNamesの要素番号に対応する。
ClearSeconds=前回勝者がクリアまでに要した時間（秒）。
''')
add('StatusEffectDef','''
_id=状態効果を識別するID。同じIDの追加時にStackPolicyで扱いを決める。
_duration=状態効果の有効時間（秒）。経過後に解除する。
_speedMultiplier=状態効果中の移動速度倍率。1で補正なし。
_animationSpeedMultiplier=状態効果中のAnimator速度倍率。HitStopの一時倍率とは別に合成する。
_maxStacks=同じ状態効果を重ねられる最大スタック数。
_stacking=同じIDの状態効果を追加した際の更新・加算・置換の方針。
_vfxPrefab=状態効果中に表示する任意のVFX Prefab。未設定ならVFXを生成しない。
''')
add('BossBarSilhouette','''
_outline=ボスHPバーの輪郭を作る正規化座標の頂点一覧。RectTransformの大きさへ変換して描画する。
''')
add('DamageTrailGauge','''
_currentFill=現在値を即時反映する手前のゲージImage。Filled方式で使用する。
_trailFill=ダメージ前の値から遅れて追従する奥のゲージImage。
_delay=ダメージ追従ゲージが減り始めるまでの待機時間（実時間の秒）。
_catchupPerSecond=追従ゲージが現在値へ近づく速度（正規化ゲージ量/秒）。大きいほど速く追従する。
''')
add('FinalBlowPresentation','''
_whiteFlash=最終Hitで白Flashを表示するCanvasGroup。Alphaで強さを制御する。
_letterboxTop=勝利中に画面上へ出す黒帯のRectTransform。
_letterboxBottom=勝利中に画面下へ出す黒帯のRectTransform。
_hudGroups=勝利中にFade OutするHUDのCanvasGroup一覧。
_hudVisibilityOwners=通常HUD更新と勝利演出の表示制御を調停するRunHUD一覧。
_damageNumbers=勝利中に新しいダメージ数字を抑制する表示コンポーネント。
_flashStrength=最終Hit白Flashの最大Alpha。0で透明、1で不透明。
_flashDelay=勝利開始から白Flashを出すまでの時間（実時間の秒）。
_hudFadeDelay=勝利開始からHUDを消し始めるまでの時間（実時間の秒）。
_hudFadeDuration=HUDのFade Outにかける時間（実時間の秒）。
_letterboxDelay=勝利開始から上下黒帯を出し始めるまでの時間（実時間の秒）。
_letterboxDuration=上下黒帯が目標高さになるまでの時間（実時間の秒）。
_letterboxHeight=各黒帯の目標高さ。親画面の高さに対する比率。
_textStartScale=討伐文字が現れる瞬間のScale倍率。元のScaleを基準にする。
_textOvershootScale=討伐文字が素早く拡大したときの最大Scale倍率。その後元のScaleへ戻す。
_textRiseFraction=討伐表示時間のうち最初の拡大・Fadeに使う割合。残りの時間でScaleを落ち着かせる。
''')
add('PlayerHUDView','''
_hpFill=Playerの現在HPを即時反映するゲージImage。
_damageFill=ダメージ前のHPから遅れて減る追従ゲージImage。
_skillFill=Playerのスキルゲージ比率を反映するImage。
_damageDelay=HPダメージ後に追従ゲージが減り始めるまでの時間（実時間の秒）。
_damageCatchupPerSecond=HP追従ゲージの減少速度（正規化ゲージ量/秒）。
''')
add('RunHUD','''
_player=HP・スキルゲージ・無敵・Just Avoidボーナスを表示するPlayer参照。
_enemy=ボスHP比率を取得するEnemy参照。
_enemyHpFill=ボスHPを直接反映するImage。EnemyGauge未設定時に使用する。
_enemyGauge=ボスHPと遅延ダメージを表示するゲージ。設定時は直接Fillより優先する。
_lifeOrbs=残機を表すImage一覧。先頭から残機数だけ点灯色にする。
_lifeActiveColor=残っている命の表示色。
_lifeEmptyColor=消費済みの命の表示色。
_visibility=Run HUD全体の表示Alphaを制御するCanvasGroup。
_challenger=現在の挑戦者名を表示するTMPテキスト。
_opponent=前回勝者または初期相手の名前を表示するTMPテキスト。
_health=Playerの現在HPを数値で表示するTMPテキスト。
_lives=現在の残機数を文字で表示するTMPテキスト。
_equipment=選択した攻撃装備・防御装備の名前を表示するTMPテキスト。
_skill=Playerの現在スキルゲージを数値で表示するTMPテキスト。
_ghostStatus=Just Avoidボーナスの倍率・残り時間と無敵状態を表示するTMPテキスト。
''')
add('RunSetupUI','''
_panel=名前・装備選択画面全体の表示と入力受付を切り替えるCanvasGroup。
_nameInput=新しいRunの挑戦者名を入力するTMP Input Field。
_attackOptions=攻撃装備を選ぶToggle一覧。配列順をRunSession.AttackNamesに合わせる。
_defenseOptions=防御装備を選ぶToggle一覧。配列順をRunSession.DefenseNamesに合わせる。
_attackDescription=選択中の攻撃装備の効果説明を表示するTMPテキスト。
_defenseDescription=選択中の防御装備の効果説明を表示するTMPテキスト。
_opponent=前回勝者と継承装備の説明を表示するTMPテキスト。
_rules=残機・復活・勝者継承ルールを表示するTMPテキスト。
_result=前回の勝敗結果と保存エラーを表示するTMPテキスト。
_startButton=入力した名前と装備でRunを確定し、戦闘を開始するButton。
''')
add('TitleText','''
_text=タイトル画面で点滅FadeするTMP文字参照。
_fadeInDuration=タイトル文字が現れるまでの時間（秒）。
_fadeOutDuration=タイトル文字が消えるまでの時間（秒）。
_ease=タイトル文字のFadeに使用するDOTween補間曲線。
_gameSceneName=旧Scene指定の互換設定。現在の開始入力はTitleManagerへ委譲する。
''')
save()
