from summary_inventory import *
import json
properties={
 'Camera/CameraManager.cs':{'    public Camera OutputCamera':'描画補正とHDRP Passの対象になるSceneの出力カメラ。'},
 'Camera/VictoryCameraRig.cs':{},
 'Managers/FinalBlowManager.cs':{'    public bool IsPlaying':'Final Blowが制御権を持っている間はtrue。回避など低優先演出の開始抑制に使用する。'},
 'Managers/GameManager.cs':{'    public bool IsCombatActive':'戦闘状態と有効なランが両方成立している場合だけtrue。死亡後の入力・AI更新を遮断する。'},
 'Enemy/EnemyController.cs':{'    public bool IsAttackAnimationActive':'Root Motionを所有する攻撃Stateが記録されているか。通常被弾の表示量の調整に使用する。'},
 'Feedback/JustAvoidEnvelope.cs':{'    public bool Playing':'移行・保持・復帰の時計が有効な間はtrue。','    public float Value':'現在の包絡線重み。ゼロが通常、1が最大効果。'}
}
for rel,entries in properties.items():
 p=root/rel;text,enc=read(p);lines=text.splitlines(keepends=True)
 for i in reversed(range(len(lines))):
  for prefix,desc in entries.items():
   if lines[i].startswith(prefix) and (i==0 or not lines[i-1].strip().startswith('///')):lines.insert(i,'    /// <summary>'+desc+'</summary>\r\n')
 data=''.join(lines);bom=p.read_bytes().startswith(b'\xef\xbb\xbf');p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+data.encode(enc))
# Move the existing class documentation ahead of RequireComponent; do not move the attribute itself relative to code.
p=root/'StatusEffect/StatusEffectManager.cs';text,enc=read(p)
m=re.search(r'(\[RequireComponent\(typeof\(Animator\)\)\]\r?\n)(/// <summary>.*?/// </summary>\r?\n)',text,re.S)
if m:
 text=text[:m.start()]+m.group(2)+m.group(1)+text[m.end():];p.write_bytes(text.encode(enc))
# Correct an existing claimed range: the serialized bonus is returned verbatim, not clamped.
p=root/'Config/LowHpBuffTable.cs';text,enc=read(p);text=text.replace('返値は 0..1 の割合（例: 0.05 = +5% 回復量）です。','返値は設定した加算割合をそのまま返します（例: 0.05 = +5% 回復量）。');p.write_bytes(text.encode(enc))
log=json.loads(Path('AgentScripts/SummaryChanges.json').read_text(encoding='utf-8'))
type_names={d['name'] for p in root.rglob('*.cs') for d in declarations(read(p)[0]) if d['kind']!='method'}
ctors=sum(d['kind']=='method' and d['name'] in type_names for d in log)
print('Constructors:',ctors,'Other methods:',445-ctors)
print('Comment corrections and API properties complete')
