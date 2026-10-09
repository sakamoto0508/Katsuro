from serialized_field_docs import *
fixes={
'PlayerStateConfig':{
'_maxSkillGauge':'PlayerStatus未設定時に使うスキルゲージの代替最大値。',
'_ghostColor':'幽体化の旧表示色設定。現在の体色演出はCombatFeedbackのGhost Tintを使用する。',
'_alpha':'幽体化の旧透明度設定（0〜1）。現在の体色演出はCombatFeedbackのGhost TintのAlphaを使用する。'},
'TitleText':{
'_fadeInDuration':'タイトル文字が現れるまでの時間（実時間の秒）。',
'_fadeOutDuration':'タイトル文字が消えるまでの時間（実時間の秒）。',
'_gameSceneName':'TitleManager未設定時の代替開始処理でロードするScene名。通常はTitleManagerへ委譲する。'},
'TitleManager':{'_transitionDelay':'開始操作からScene切り替えまでの待機時間（実時間の秒）。'},
'EnemyDecisionConfig':{'ReconsiderInterval':'行動中に距離条件などを再検討する間隔（秒）。短いほど頻繁に判断する。'},
'SwordTrail':{
'_heavyBrightness':'通常の明るさへ掛けるHeavy攻撃の追加倍率。',
'_counterBrightness':'通常の明るさへ掛けるJust Avoid追撃の追加倍率。'},
'DamageNumbers':{'_randomOffsetRange':'数字の表示位置に加えるランダムなずらし幅（UIローカル単位）。両成分を0にすると無効。'}
}
for owner,entries in fixes.items():
    p=next(root.rglob(owner+'.cs'));raw=p.read_bytes();text,enc=read(p)
    for key,desc in entries.items():
        old=catalog.get(owner,{}).get(key)
        if old:text=text.replace(old,desc)
        else:
            field=next(f for f in json.loads(Path('AgentScripts/SerializedFields.json').read_text(encoding='utf-8'))['fields'] if f['path']==str(p).replace('\\','/') and f['name']==key)
            old=field['tooltip'];text=text.replace(old,desc)
        catalog.setdefault(owner,{})[key]=desc
    bom=b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'';p.write_bytes(bom+text.encode(enc))
save()
