import json
from pathlib import Path
p=Path('AgentScripts/summary_catalog.json');c=json.loads(p.read_text(encoding='utf-8'))
c['_types'].update({
'EnemyAction':'Enemy行動用に残されている空の拡張枠。現在は実行処理を持たない。',
'EnemyActionProvider':'Enemyの行動提供用に残されている空の拡張枠。現在のAI行動はこのクラスで実行しない。',
'EnemyStateMachine':'Enemy状態管理用に残されている空の拡張枠。現在の戦闘状態管理は別のAI実装が担当する。',
'EnemyActionLock':'Enemy行動ロック用に残されている拡張枠。現在のStartとUpdateには処理がない。',
'EnemyActionTimer':'Enemy行動時間の管理用に残されている拡張枠。現在のStartとUpdateには処理がない。',
'SampleEnemyAttacks':'InspectorでEnemyAttackDataのサンプル参照一覧を保持する補助コンポーネント。攻撃の実行処理は持たない。',
'DistanceManager':'距離管理用に残されている拡張枠。現在は距離計測・通知の処理を持たない。',
'RotationControlMode':'Enemyの旋回をAgent任せ、対象へ即時旋回、手動補間のいずれで行うか識別する。',
'JustAvoidBuffConfig':'ジャスト回避成功で蓄積する攻撃倍率の加算量と最大スタック数を定義する。'
})
rows='''
EnemyAnimationController.GetHitDirection=攻撃者位置または命中情報をEnemyのローカル方向へ変換し、四方向の被弾Clipを選ぶ。|前・後・左・右を表す方向インデックス。
EnemyAnimationController.TryPlayHitReaction=有効な専用Layerと追撃フラグがある場合だけ大きい被弾反応を開始する。|追撃専用の被弾Stateを開始した場合はtrue。
EnemyAnimationController.InterruptAttackForHitReaction=攻撃用トリガーと攻撃再生を解除して被弾への短いBlendを要求する。死亡トリガーは保持する。
EnemyAnimationController.TickHitReaction=Animator速度に合わせて被弾Layerと減速移動の進行を更新し、復帰完了を通知する。|被弾反応がこの更新で完了した場合はtrue。
EnemyAnimationController.CancelHitReaction=被弾状態・専用パラメータ・Layer重みを解除し、死亡や中断に反応を持ち越さない。
EnemyAnimationController.Init=Animatorと被弾Layer・必須パラメータを取得して、専用反応が使用可能か一度だけ確認する。
EnemyAttacker.EnemyAttacker=Enemyの攻撃設定、武器、Animator、基礎威力と攻撃者を保持する。
EnemyAttacker.Init=命中時に使用するHitStopManagerを一度だけ接続する。
EnemyAttacker.FindData=指定行動に対応する攻撃設定を候補から選ぶ。|対応する攻撃設定。候補がなければnull。
EnemyAttacker.Dispose=全武器の命中判定を停止し、自身が登録した命中通知ハンドラを解除する。
EnemyMover.BeginAttackRootMotion=攻撃の移動停止中だけ水平Root Motionを受け付ける。被弾と後退移動が優先中なら開始しない。
EnemyMover.EndAttackRootMotion=攻撃移動の受付・保留量を解除し、動的Rigidbodyの残留速度を消去する。
EnemyMover.FixedUpdateAttackRootMotion=蓄積した水平移動を衝突とNavMeshで制限して物理フレームに反映する。HitStop中は移動を消去する。
EnemyMover.HoldMovementForReaction=被弾用にAgentとRigidbodyの状態を保存して移動を停止し、攻撃Root Motionを解除する。
EnemyMover.ReleaseMovementAfterReaction=被弾前のAgentとRigidbody設定を復元し、現在位置にAgentを再同期する。
EnemyMover.InterruptMovementAction=後退と攻撃による移動停止を終了し、別の行動や死亡へ安全に切り替える。
EnemyMover.UpdatePatrolWalking=待機歩行中の旋回設定と移動先を更新し、実移動に合わせてAnimatorへ反映する。
EnemyMover.UpdateAnimatorValues=Agentの実速度を取得し、Enemyの向きを基準に移動BlendTreeへ反映する。
EnemyMover.ResetLocomotionAnimation=移動速度と二軸方向をゼロにして、攻撃・被弾中の歩行Blendを止める。
EnemyMover.GetLocomotionVelocity=移動停止・被弾・後退と微小速度を除外し、Agentの有効な水平実速度を取得する。|移動表示に使用する水平速度。停止条件ではゼロ。
EnemyMover.GetLocalMovementDirection=水平速度をEnemyの向きに対する正規化二軸方向へ変換する。|ローカルX/Z方向。微小速度ではゼロ。
EnemyMover.UpdateTrackingAndDestination=追跡距離と更新間隔を判定してPlayer側の移動先を更新し、範囲外ならAgentを停止する。
EnemyMover.GetAdjustedDestination=PlayerのCollider半径を考慮して接近先をずらし、中心へのめり込みを抑える。|Playerの占有半径を考慮した接近先。
EnemyDecisionMaker.Decide=距離帯の候補から無効な重みを除外し、連続選択の補正を含めて行動を抽選する。|選ばれた行動。有効候補がない場合はWait。
EnemyDecisionMaker.Weight=候補の重みを検証し、直前と同じ行動には繰り返し補正を適用する。|抽選に使用する有効重み。無効値ならゼロ。
EnemyController.BeginAnimationAttackRootMotion=戦闘中の有効な攻撃StateだけにRoot Motionの所有権を渡す。死亡・被弾中は開始しない。
EnemyController.EndAnimationAttackRootMotion=終了Stateが攻撃移動の所有者に一致する場合だけ移動停止を解除する。
EnemyController.FixedUpdate=戦闘中の攻撃Root Motionを物理更新へ反映する。死亡・戦闘終了時は移動受付を解除する。
EnemyController.OnDisable=攻撃Stateの所有権と被弾・攻撃による移動停止を解除する。
EnemyController.Init=EnemyのHP・AI・移動・武器・演出を作成し、ゲーム進行と命中通知を一度だけ接続する。
EnemyController.PlayEffectNextFrame=生成直後の初期化を一フレーム待ってCharacterEffectを再生する。破棄時は待機をキャンセルする。
EnemyController.ApplyDamage=通常の命中情報をクリティカルなしの共通ダメージ処理へ渡す。
EnemyController.OnDestroy=Root Motionを終了し、武器購読とHP通知の所有リソースを解放する。
EnemyController.Update=戦闘中だけ被弾の復帰・AI判断・予約行動・移動を順に進める。死亡時は歩行表示を停止する。
EnemyController.EnemyDead=死亡を確定して攻撃・AI更新・移動を停止し、既存死亡ClipとFinal Blowまたは遷移を要求する。
EnemyController.OnAnimatorMove=所有中の攻撃Stateの移動をMoverへ渡す。死亡・別Stateへの切り替えではRoot Motionを解除する。
EnemyController.AnimEvent_OnStepBackFinished=有効な後退終了Eventで移動を再開し、AIへ行動完了を通知する。死亡・大被弾中は無視する。
EnemyController.AnimEvent_OnSoundEffect=Clipから指定されたSEを再生し、診断用の戦闘ログへ記録する。
EnemyHealth.EnemyHealth=基礎HPにラン継承倍率を適用し、現在HPのReactive通知を初期化する。
EnemyHealth.Dispose=EnemyのHP通知に使用するReactivePropertyを解放する。
EnemyWeapon.EnemyWeapon=刀判定Colliderと攻撃設定未指定時の基礎威力を保持する。
EnemyWeapon.Init=武器ColliderのTrigger Relayと刀の軌跡を一度だけ準備する。
EnemyWeapon.EnumerateRelays=武器に保存した命中通知Relayを列挙する。|登録済みの武器Relay。
EnemyWeapon.SetHitboxActive=武器Colliderと軌跡の有効状態を切り替え、攻撃判定区間と表示を同期する。
'''
for row in rows.strip().splitlines():
 key,value=row.split('=',1);file,method=key.split('.',1);c.setdefault(file,{})[method]=value
p.write_text(json.dumps(c,ensure_ascii=False,indent=2),encoding='utf-8')
