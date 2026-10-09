from summary_inventory import *
import shutil
def replace(file,old,new):
 p=root/file;saved=Path('AgentScripts/SummaryBaseline')/file
 if not saved.exists():saved.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,saved)
 text,enc=read(p)
 if old not in text:raise ValueError(str(p)+' missing correction')
 data=text.replace(old,new);bom=p.read_bytes().startswith(b'\xef\xbb\xbf');p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+data.encode(enc))
replace(Path('Config/JustAvoidBuffConfig.cs'),'ジャスト回避で付与する状態効果と追撃時の倍率を定義する設定。','ジャスト回避成功で蓄積する攻撃倍率の加算量と最大スタック数を定義する。')
replace(Path('Feedback/DamageNumbers.cs'),'命中位置とダメージ量を通常表示の共通処理へ渡す。','有効な正のダメージだけを表示し、演出による抑制中は表示要求を無視する。')
replace(Path('Player/PlayerAction/PlayerMover.cs'),'現在の入力を移動Animatorへ渡す二軸値として取得する。','ワールド移動方向をPlayerローカルの二軸値へ変換してAnimatorへ渡す。')
replace(Path('Managers/AnimationSpeedController.cs'),'// <summary>アニメーションの再生速度を設定します。0 で停止、1 で通常速度、2 で倍速など。</summary>','// 状態効果と一時演出の速度は別々に保持する。')
replace(Path('Managers/AnimationSpeedController.cs'),'// <summary>一時的にアニメーションの再生速度を設定します。0 で停止、1 で通常速度、2 で倍速など。</summary>','// HitStopの期限は実時間で管理する。')
replace(Path('StatusEffect/IStatusEffectReceiver.cs'),'既存効果がある場合はスタックポリシーに従って処理。','指定IDの効果を解除する。重複方針の適用は付与側が担当する。')
replace(Path('StatusEffect/IStatusEffectReceiver.cs'),'<param name="instance"></param>','<param name="instance">効果定義と付与元を保持した効果インスタンス。</param>')
for file in ['StatusEffect/IStatusEffectReceiver.cs','StatusEffect/StatusEffectManager.cs']:
 replace(Path(file),'<param name="id"></param>','<param name="id">状態効果定義の識別子。</param>')
 replace(Path(file),'<returns></returns>','<returns>指定IDの効果が適用中ならtrue。</returns>')
replace(Path('Config/PlayerStatus.cs'),'（秒あたり、1 = 1%/秒）。 /// </summary>','（秒あたり、1 = 1%/秒）。</summary>')
# Remove initializer-method docs accidentally attached to a flag; Init now has accurate docs.
for file in ['Player/PlayerAnimationController.cs','Enemy/Animation/EnemyAnimationController.cs']:
 p=root/file;text,enc=read(p)
 pattern=r'    /// <summary>\r?\n(?:(?:    ///[^\n]*\n)+?)    private bool _initialized;'
 m=re.search(pattern,text)
 if m:
  before=m.group();new='    private bool _initialized;';replace(Path(file),before,new)
# The existing block describes Tick but was attached to CostMultiplier.
p=root/'Player/PlayerAction/PlayerSelfSacrifice.cs';text,enc=read(p)
pattern=r'    /// <summary>\r?\n    /// 毎フレームの進行処理：.*?    public float CostMultiplier'
m=re.search(pattern,text,flags=re.S)
if m:replace(Path('Player/PlayerAction/PlayerSelfSacrifice.cs'),m.group(),'    /// <summary>自傷の継続ゲージ消費へ掛ける追加倍率。HPの消費は通知先が担当する。</summary>\r\n    public float CostMultiplier')
print('Targeted comment corrections complete')
