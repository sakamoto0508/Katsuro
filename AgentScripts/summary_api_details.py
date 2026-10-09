from summary_inventory import *
import shutil,html
returns={
 'LockOnCamera.HasValidTarget':'対象が存在し、Hierarchy上で有効ならtrue。',
 'LockOnCamera.ReturnLockOnDirection':'PlayerからLock-On対象への方向。Lock-Onが無効ならゼロ。',
 'LowHpBuffTable.GetTier':'Inspector順で最初にしきい値を満たすTier。該当なしならnull。',
 'LowHpBuffTable.EvaluateDamageMultiplier':'Tierの非負ダメージ倍率。該当なしなら1。',
 'LowHpBuffTable.EvaluateSkillGaugeRegenBonus':'Tierに設定したゲージ回復への加算割合。該当なしなら0。',
 'PlayerPassiveBuffSet.EvaluateDamageMultiplier':'登録装備の攻撃倍率を掛け合わせた値。未登録なら1。',
 'PlayerPassiveBuffSet.EvaluateFlatDamageBonus':'登録装備の固定攻撃力を合計した加算値。',
 'EnemyWeapon.Damage':'攻撃中の指定威力。指定が正でなければ基礎威力。',
 'EnemyWeapon.GetContactPoint':'刀の判定形状と対象の近接点から求めた接触位置。',
 'PlayerWeapon.GetContactPoint':'刀の判定形状と対象の近接点から求めた接触位置。',
 'PlayerWeapon.GetSlashDirection':'直近の刀の振りから求めた斬撃方向。取得できなければ対象との方向。',
 'AbilityManager.ToggleGhost':'開始・解除・失敗を表す結果。',
 'AbilityManager.ToggleSelfSacrifice':'要求に応じて自傷能力を切り替えられた場合はtrue。',
 'AbilityManager.ToggleHeal':'要求に応じて回復能力を切り替えられた場合はtrue。',
 'GlobalFader.FadeToScene':'暗転・Scene読み込み・再表示の完了を待機するタスク。',
 'SkillGauge.TryConsume':'必要量を消費できた場合はtrue。',
 'EnemyMover.LookTargetSmooth':'対象方向への補間旋回が終了するまで待機するタスク。',
 'PlayerMover.LookTargetSmooth':'対象方向への補間旋回が終了するまで待機するタスク。',
 'PlayerHeal.TryBegin':'正の回復率と使用可能なゲージで回復を開始できた場合はtrue。',
 'PlayerSelfSacrifice.CanBegin':'HP割合が開始しきい値より高く、ゲージが残っている場合はtrue。'
}
params={
 'VictoryCameraRig.Begin':{'delay':'構図の移行を始めるまでの実時間秒数。'},
 'VictoryCameraRig.Tick':{'now':'Time.unscaledTimeと同じ基準の実時刻。'},
 'FinalBlowPresentation.RenderAt':{'elapsed':'勝利UI開始からの実経過秒数。'},
 'JustAvoidEnvelope.Begin':{'now':'移行開始の基準となる実時刻。'},
 'JustAvoidEnvelope.Evaluate':{'now':'開始時と同じ時計の実時刻。','enter':'最大効果へ移行する実時間秒数。','hold':'最大効果を保持する実時間秒数。','restore':'通常へ戻る実時間秒数。'},
 'PlayerController.BeginAnimationAttackRootMotion':{'stateHash':'Root Motionを所有する攻撃StateのfullPathHash。'},
 'PlayerController.EndAnimationAttackRootMotion':{'stateHash':'終了したStateのfullPathHash。別Stateの終了では所有権を解除しない。'},
 'EnemyController.BeginAnimationAttackRootMotion':{'stateHash':'Root Motionを所有する攻撃StateのfullPathHash。'},
 'EnemyController.EndAnimationAttackRootMotion':{'stateHash':'終了したStateのfullPathHash。現在の所有者と一致する場合だけ解除する。'},
 'HitStopManager.PlayHitStop':{'durationRealtime':'完全停止する実時間秒数。','targets':'あらかじめRegisterTargetで登録した対象またはその子。'},
 'HitStopManager.PlayHitStopSlow':{'durationRealtime':'一時倍率の実時間の有効秒数。','slowSpeed':'一時的なAnimator速度倍率。ゼロなら完全停止。','targets':'登録済みの停止・減速対象。'},
 'AnimationSpeedController.SetTemporary':{'multiplier':'基準速度と状態倍率へ掛ける一時倍率。','seconds':'実時間で計測する有効秒数。'},
 'PlayerSelfSacrifice.CanBegin':{'currentHpRatio':'現在HPの最大HPに対する割合。'},
 'PlayerHeal.TryBegin':{'percentPerSecond':'最大HPを毎秒何パーセント回復するか。1は1%/秒。'},
 'GameplayRules.Regen':{'hp':'現在HPの最大HPに対する0から1の割合。'},
 'GameplayRules.SelfCost':{'hp':'現在HPの最大HPに対する0から1の割合。'},
 'GameplayRules.AvoidWindow':{'hp':'現在HPの最大HPに対する0から1の割合。'}
}
count=0
for p in sorted(root.rglob('*.cs')):
 text,enc=read(p);lines=text.splitlines(keepends=True)
 for d in reversed(declarations(text)):
  if d['kind']!='method':continue
  key=p.stem+'.'+d['name'];i=d['line'];j=i-1
  while j>=0 and (lines[j].strip().startswith('///') or lines[j].strip().startswith('[') or not lines[j].strip()):j-=1
  block=''.join(lines[j+1:i]);ending='\r\n' if '\r\n' in text else '\n';indent=re.match(r'\s*',lines[i]).group().strip('\r\n')
  added=[]
  for name,desc in params.get(key,{}).items():
   if f'<param name="{name}">' not in block:added.append(f'<param name="{name}">{html.escape(desc)}</param>')
  if key in returns and not re.search(r'<returns>\s*\S',block):
   if '<returns></returns>' in block:
    for lineindex in range(j+1,i):lines[lineindex]=lines[lineindex].replace('<returns></returns>','<returns>'+returns[key]+'</returns>')
    count+=1
   else:added.append('<returns>'+returns[key]+'</returns>')
  if added:lines.insert(i,''.join(indent+'/// '+a+ending for a in added));count+=len(added)
 updated=''.join(lines)
 if updated!=text:
  saved=Path('AgentScripts/SummaryBaseline')/p.relative_to(root)
  if not saved.exists():saved.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,saved)
  bom=p.read_bytes().startswith(b'\xef\xbb\xbf');p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+updated.encode(enc))
print('Added/completed param and return tags',count)
