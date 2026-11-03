"""TitleSceneのTitleAmbient配下に、侍の呼吸・外套の揺れ・刀の血の演出を追加する。"""
import os, re, sys, uuid

proj = sys.argv[1]
amb = os.path.join(proj, 'Assets/Katsuro/Scripts/UI/TitleAmbient')
tmpl = open(os.path.join(amb, 'TitleWind.cs.meta'), encoding='utf-8').read()
guids = {}
for name in ['TitleImageWarp', 'TitleCharacterBreath', 'TitleClothSway', 'TitleBladeBlood']:
    path = os.path.join(amb, name + '.cs.meta')
    if os.path.exists(path):
        guids[name] = re.search(r'guid: (\w+)', open(path, encoding='utf-8').read()).group(1)
        continue
    guids[name] = uuid.uuid4().hex
    open(path, 'w', encoding='utf-8', newline='\n').write(re.sub(r'guid: \w+', 'guid: ' + guids[name], tmpl, count=1))

scene_path = os.path.join(proj, 'Assets/Katsuro/Scenes/TitleScene.unity')
scene = open(scene_path, encoding='utf-8', newline='').read()
assert '\r\n' not in scene
assert '&7310000101' not in scene, 'already placed'
SPRITE = '{fileID: -53586890, guid: c9e99956661a2da47958a86c3fefdcf5, type: 3}'

# 画像上のpx（1536x1024、左上原点）→ 背景Sprite（x 1..1534、y 3..1020）に対する割合（左下原点）
def uv(px, py):
    return round((px - 1) / 1533, 4), round(1 - (py - 3) / 1017, 4)

BREATH = (uv(590, 548), uv(950, 262))   # 頭巾〜肩〜帯
CLOTH = (uv(612, 792), uv(902, 548))    # 帯〜外套の裾〜脛


def go(fid, name, comps):
    return ('--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n%s'
            '  m_Layer: 5\n  m_Name: %s\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n'
            '  m_StaticEditorFlags: 0\n  m_IsActive: 1\n') % (fid, ''.join('  - component: {fileID: %d}\n' % c for c in comps), name)


def rt(fid, go_id, father, children, amin, amax, pos=(0, 0), size=(0, 0), pivot=(0.5, 0.5)):
    ch = ''.join('\n  - {fileID: %d}' % c for c in children) if children else ' []'
    return ('--- !u!224 &%d\nRectTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n'
            '  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n'
            '  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n  m_Children:%s\n'
            '  m_Father: {fileID: %d}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
            '  m_AnchorMin: {x: %g, y: %g}\n  m_AnchorMax: {x: %g, y: %g}\n  m_AnchoredPosition: {x: %g, y: %g}\n'
            '  m_SizeDelta: {x: %g, y: %g}\n  m_Pivot: {x: %g, y: %g}\n') % (fid, go_id, ch, father, *amin, *amax, *pos, *size, *pivot)


def cr(fid, go_id):
    return ('--- !u!222 &%d\nCanvasRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n'
            '  m_CullTransparentMesh: 1\n') % (fid, go_id)


def graphic(fid, go_id, script, fields):
    return ('--- !u!114 &%d\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n'
            '  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n'
            '  m_EditorClassIdentifier: Assembly-CSharp::%s\n  m_Material: {fileID: 0}\n'
            '  m_Color: {r: 1, g: 1, b: 1, a: 1}\n  m_RaycastTarget: 0\n  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n'
            '  m_Maskable: 1\n  m_OnCullStateChanged:\n    m_PersistentCalls:\n      m_Calls: []\n%s') % (fid, go_id, guids[script], script, fields)


AMB_RT = 7310000002
# 背景Imageと同じ矩形（上端中央基準、1980x1080）
FRAME = dict(amin=(0.5, 1), amax=(0.5, 1), pos=(0, 0), size=(1980, 1080), pivot=(0.5, 1))
docs = [
    go(7310000101, 'CharacterMotion', [7310000102]),
    rt(7310000102, 7310000101, AMB_RT, [7310000112, 7310000122], **FRAME),
    go(7310000111, 'Breath', [7310000112, 7310000113, 7310000114]),
    rt(7310000112, 7310000111, 7310000102, [], BREATH[0], BREATH[1]),
    cr(7310000113, 7310000111),
    graphic(7310000114, 7310000111, 'TitleCharacterBreath',
            '  _source: %s\n  _grid: {x: 12, y: 18}\n  _lift: 1.4\n  _widen: 0.4\n  _period: 4.6\n  _periodJitter: 0.18\n'
            '  _depthJitter: 0.3\n  _shoulderHeight: 0.62\n  _topFade: 0.08\n  _sideFade: 0.1\n  _seed: 11\n' % SPRITE),
    go(7310000121, 'Cloth', [7310000122, 7310000123, 7310000124]),
    rt(7310000122, 7310000121, 7310000102, [], CLOTH[0], CLOTH[1]),
    cr(7310000123, 7310000121),
    graphic(7310000124, 7310000121, 'TitleClothSway',
            '  _source: %s\n  _grid: {x: 14, y: 16}\n  _swayAmplitude: 1.2\n  _windInfluence: 0.03\n  _gustBoost: 0.8\n'
            '  _maxOffset: 3.5\n  _swaySpeed: 0.35\n  _waveDelay: 0.6\n  _weight: 1.4\n  _hemHeight: 0.14\n  _sideFade: 0.25\n'
            '  _lift: 0.15\n  _seed: 23\n' % SPRITE),
    go(7310000131, 'BladeBlood', [7310000132, 7310000133, 7310000134]),
    rt(7310000132, 7310000131, AMB_RT, [], **FRAME),
    cr(7310000133, 7310000131),
    graphic(7310000134, 7310000131, 'TitleBladeBlood',
            '  _bladeStart: {x: %g, y: %g}\n  _bladeEnd: {x: %g, y: %g}\n  _dripPoints:\n  - {x: %g, y: %g}\n  - {x: %g, y: %g}\n'
            '  _groundHeight: %g\n  _beadCount: 2\n  _beadSpeed: {x: 5, y: 11}\n  _beadSize: {x: 3.5, y: 5.5}\n  _beadStretch: 2.6\n'
            '  _dropInterval: {x: 3, y: 6.5}\n  _growTime: {x: 0.9, y: 1.6}\n  _dropSize: {x: 4.5, y: 6.5}\n  _gravity: 1100\n'
            '  _fallStretch: 0.006\n  _windInfluence: 0.03\n  _bloodColor: {r: 0.5, g: 0.03, b: 0.04, a: 0.95}\n'
            '  _glossColor: {r: 1, g: 0.42, b: 0.36, a: 0.5}\n'
            % (*uv(930, 664), *uv(1040, 782), *uv(978, 740), *uv(1008, 766), uv(0, 840)[1])),
]

# 描画順：背景 → 人物の動き（絵の一部なので霧より奥）→ 霧・火の粉 → 刀の血 → ロゴ/UI
old_children = '  m_Children:\n  - {fileID: 7310000012}\n  - {fileID: 7310000022}\n  - {fileID: 7310000032}\n  - {fileID: 7310000042}\n  m_Father: {fileID: 866026673}'
new_children = '  m_Children:\n  - {fileID: 7310000102}\n  - {fileID: 7310000012}\n  - {fileID: 7310000022}\n  - {fileID: 7310000032}\n  - {fileID: 7310000042}\n  - {fileID: 7310000132}\n  m_Father: {fileID: 866026673}'
assert scene.count(old_children) == 1
scene = scene.replace(old_children, new_children)
# TitleAmbient配下の最後の要素（Fog_Near）の後ろに追加する
anchor = '--- !u!1 &7310000041\n'
idx = scene.index(anchor)
nxt = scene.find('\n--- !u!', scene.index('&7310000044', idx))
nxt = scene.find('\n--- !u!', nxt + 1) if False else nxt
scene = scene[:nxt + 1] + ''.join(docs) + scene[nxt + 1:]
open(scene_path, 'w', encoding='utf-8', newline='\n').write(scene)
print(guids, BREATH, CLOTH)
