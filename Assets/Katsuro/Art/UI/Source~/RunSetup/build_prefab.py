"""RunSetupCanvas.prefab を再構築する。既存Prefabからコンポーネントのひな形を取り、シーン参照されるfileIDは維持する。"""
import re, sys, json, hashlib

SRC = sys.argv[1]
DST = sys.argv[2]
SPR = dict(l.split() for l in open(sys.argv[3], encoding='utf-8') if l.strip())

text = open(SRC, encoding='utf-8', newline='').read().replace('\r\n', '\n')
parts = re.split(r'^(--- !u!\d+ &-?\d+(?: stripped)?)\n', text, flags=re.M)
HEADER = parts[0]
OLD = {}
for i in range(1, len(parts), 2):
    fid = re.search(r'&(-?\d+)', parts[i]).group(1)
    OLD[fid] = parts[i + 1]

def tmpl(fid):
    return OLD[str(fid)]

def old_text(fid):
    s = re.search(r'^  m_text: (.*)$', tmpl(fid), re.M).group(1)
    return json.loads(s) if s.startswith('"') else s

# 既存の札の名前と紋（Attack_0..2 / Defense_0..2 の Label / Emblem）
OLD_LABELS = {
    'Attack': [old_text(5965762262441241973), old_text(6065739741175602682), old_text(348570639768854485)],
    'Defense': [old_text(7840379522090121305), old_text(5358791258661499059), old_text(6816067786031904043)],
}
OLD_EMBLEMS = {
    'Attack': [old_text(8699363446376941634), old_text(2007184407822594733), old_text(1107988658802192656)],
    'Defense': [old_text(8596454536230074739), old_text(3635685348591558619), old_text(6797543867134860554)],
}
OLD_NAME_TEXT = re.search(r'^  m_Text: (.*)$', tmpl(379248718934119660), re.M).group(1)

T_IMAGE = tmpl(6501019911631560869)
T_TMP = tmpl(5965762262441241973)
T_TOGGLE = tmpl(8554489723371277616)
T_BUTTON = tmpl(3915051304809069629)
T_INPUT = tmpl(379248718934119660)
T_GROUP = tmpl(76108055858946114)
T_CANVASGROUP = tmpl(7462631114155061920)
T_RECTMASK = next(v for v in OLD.values() if 'UnityEngine.UI.RectMask2D' in v)

# シーン（TitleScene）から参照・上書きされているfileIDは維持する。
KEEP = {
    'root.go': 5401191227933041408, 'root.rt': 6330066199334865004, 'root.Canvas': 9043415182819911970,
    'root.Scaler': 5215122410851613692, 'root.Raycaster': 6833328634716503345,
    'root.CanvasGroup': 7462631114155061920, 'root.RunSetupUI': 5778134622391078993,
    'NameText.TMP': 4759766509143167490,
}

def fid_for(key):
    if key in KEEP:
        return KEEP[key]
    h = int(hashlib.sha1(('RunSetupCanvas/' + key).encode()).hexdigest()[:16], 16)
    return 1000000000000000000 + h % 8000000000000000000

def sub(body, key, value, indent=2):
    pat = re.compile(r'^(' + ' ' * indent + re.escape(key) + r':)( .*)?$', re.M)
    assert pat.search(body), key
    return pat.sub(lambda m: m.group(1) + (' ' + value if value != '' else ' '), body, count=1)

def ref(x):
    return '{fileID: %d}' % (x if isinstance(x, int) else 0)

def col(h, a=1.0):
    h = h.lstrip('#')
    r, g, b = (int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
    return (round(r, 3), round(g, 3), round(b, 3), a)

def colstr(c):
    return '{r: %g, g: %g, b: %g, a: %g}' % c

def sprite(name):
    return '{fileID: 21300000, guid: %s, type: 3}' % SPR[name]

# 配色：墨黒・深紅・くすんだ金・生成り
INK = '#0B0B0D'
PANEL = '#131315'
CARD = '#19191C'
CRIMSON = '#9B1C24'
CRIMSON_DEEP = '#4E0E13'
GOLD = '#BC9A5C'
KINARI = '#EDE5D3'
KINARI_SUB = '#BDB4A2'
MUTED = '#9C9586'
ERROR = '#F0A08F'
FONT = '{fileID: 11400000, guid: 02e77e14e0a074a46acde53a9cc98647, type: 2}'
FONT_MAT = '{fileID: 4364770920905358197, guid: 02e77e14e0a074a46acde53a9cc98647, type: 2}'
BODY = '{fileID: 11400000, guid: b6cae3d71b5e4efb81f8f7067f787cb3, type: 2}'
BODY_MAT = '{fileID: 2503971997271526200, guid: b6cae3d71b5e4efb81f8f7067f787cb3, type: 2}'


class Node:
    all = []

    def __init__(self, name, parent=None, key=None, active=True):
        self.name = name
        self.parent = parent
        self.key = key or ((parent.key + '/' if parent else '') + name)
        self.go = fid_for(self.key + '.go') if self.key != 'root' else KEEP['root.go']
        self.rt = fid_for(self.key + '.rt') if self.key != 'root' else KEEP['root.rt']
        self.children = []
        self.comps = []  # (classId, fid, lambda->body)
        self.active = active
        self.rect = dict(amin=(0, 1), amax=(0, 1), pos=(0, 0), size=(100, 100), pivot=(0.5, 0.5), scale=1)
        self.ids = {}
        if parent:
            parent.children.append(self)
        Node.all.append(self)

    def cid(self, kind):
        return self.ids.setdefault(kind, fid_for(self.key + '.' + kind))

    # --- RectTransform ---
    def stretch(self, l=0, b=0, r=0, t=0):
        self.rect.update(amin=(0, 0), amax=(1, 1), pos=((l - r) / 2, (b - t) / 2), size=(-(l + r), -(b + t)), pivot=(0.5, 0.5))
        return self

    def at(self, amin, amax, pos, size, pivot=(0.5, 0.5)):
        self.rect.update(amin=amin, amax=amax, pos=pos, size=size, pivot=pivot)
        return self

    # --- コンポーネント ---
    def add(self, cls, kind, body_fn):
        fid = self.cid(kind)
        self.comps.append((cls, fid, body_fn))
        return fid

    def mono(self, kind, guid, ident, fields):
        def body():
            return ('MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
                    '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
                    '  m_GameObject: {fileID: %d}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
                    '  m_Script: {fileID: 11500000, guid: %s, type: 3}\n  m_Name: \n'
                    '  m_EditorClassIdentifier: %s\n%s') % (self.go, guid, ident, fields() if callable(fields) else fields)
        return self.add(114, kind, body)

    def canvas_renderer(self):
        if 'CanvasRenderer' not in self.ids:
            self.add(222, 'CanvasRenderer', lambda: (
                'CanvasRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
                '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
                '  m_GameObject: {fileID: %d}\n  m_CullTransparentMesh: 1\n') % self.go)

    def image(self, color, spr=None, sliced=False, raycast=False, ppu=1):
        self.canvas_renderer()
        def body():
            b = rego(T_IMAGE, self.go)
            b = sub(b, 'm_Color', colstr(color))
            b = sub(b, 'm_RaycastTarget', '1' if raycast else '0')
            b = sub(b, 'm_Sprite', sprite(spr) if spr else '{fileID: 0}')
            b = sub(b, 'm_Type', '1' if sliced else '0')
            b = sub(b, 'm_PixelsPerUnitMultiplier', str(ppu))
            return b
        return self.add(114, 'Image', body)

    def tmp(self, text, size, color, h=1, v=512, wrap=True, auto=None, spacing=0, line=0, overflow=0, kind='TMP', brush=False):
        self.canvas_renderer()
        def body():
            b = rego(T_TMP, self.go)
            b = sub(b, 'm_text', json.dumps(text))
            b = sub(b, 'm_fontAsset', FONT if brush else BODY)
            b = sub(b, 'm_sharedMaterial', FONT_MAT if brush else BODY_MAT)
            b = sub(b, 'm_fontColor', colstr(color))
            r, g, bl, a = (max(0, min(255, round(x * 255))) for x in color)
            b = re.sub(r'(m_fontColor32:\n    serializedVersion: 2\n    rgba: )\d+', lambda m: m.group(1) + str((a << 24) | (bl << 16) | (g << 8) | r), b)
            b = sub(b, 'm_fontSize', str(size))
            b = sub(b, 'm_fontSizeBase', str(size))
            b = sub(b, 'm_enableAutoSizing', '1' if auto else '0')
            b = sub(b, 'm_fontSizeMin', str(auto[0] if auto else 18))
            b = sub(b, 'm_fontSizeMax', str(auto[1] if auto else 72))
            b = sub(b, 'm_HorizontalAlignment', str(h))
            b = sub(b, 'm_VerticalAlignment', str(v))
            b = sub(b, 'm_TextWrappingMode', '1' if wrap else '0')
            b = sub(b, 'm_characterSpacing', str(spacing))
            b = sub(b, 'm_lineSpacing', str(line))
            b = sub(b, 'm_overflowMode', str(overflow))
            b = sub(b, 'm_RaycastTarget', '0')
            return b
        return self.add(114, kind, body)

    def canvas_group(self, alpha=1, interactable=1, blocks=1):
        def body():
            b = rego(T_CANVASGROUP, self.go)
            b = sub(b, 'm_Alpha', str(alpha))
            b = sub(b, 'm_Interactable', str(interactable))
            b = sub(b, 'm_BlocksRaycasts', str(blocks))
            return b
        return self.add(225, 'CanvasGroup', body)

    def layout(self, minw=-1, minh=-1, pw=-1, ph=-1, fw=-1, fh=-1, ignore=0):
        return self.mono('LayoutElement', '306cc8c2b49d7114eaa3623786fc2126', 'UnityEngine.UI::UnityEngine.UI.LayoutElement',
                         '  m_IgnoreLayout: %d\n  m_MinWidth: %g\n  m_MinHeight: %g\n  m_PreferredWidth: %g\n'
                         '  m_PreferredHeight: %g\n  m_FlexibleWidth: %g\n  m_FlexibleHeight: %g\n  m_LayoutPriority: 1\n'
                         % (ignore, minw, minh, pw, ph, fw, fh))

    def group(self, vertical, pad=(0, 0, 0, 0), spacing=0, align=0, cw=1, ch=1, ew=0, eh=0):
        guid, ident = (('59f8146938fff824cb5fd77236b75775', 'UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup') if vertical
                       else ('30649d3a9faa99c48a7b1166b86bf2a0', 'UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup'))
        return self.mono('LayoutGroup', guid, ident,
                         '  m_Padding:\n    m_Left: %d\n    m_Right: %d\n    m_Top: %d\n    m_Bottom: %d\n'
                         '  m_ChildAlignment: %d\n  m_Spacing: %g\n  m_ChildForceExpandWidth: %d\n  m_ChildForceExpandHeight: %d\n'
                         '  m_ChildControlWidth: %d\n  m_ChildControlHeight: %d\n  m_ChildScaleWidth: 0\n  m_ChildScaleHeight: 0\n'
                         '  m_ReverseArrangement: 0\n' % (pad[0], pad[1], pad[2], pad[3], align, spacing, ew, eh, cw, ch))

    def toggle_group(self):
        return self.add(114, 'ToggleGroup', lambda: rego(T_GROUP, self.go))

    def rect_mask(self):
        return self.add(114, 'RectMask2D', lambda: rego(T_RECTMASK, self.go))


def rego(body, go):
    return re.sub(r'^  m_GameObject: \{fileID: -?\d+\}$', '  m_GameObject: {fileID: %d}' % go, body, count=1, flags=re.M)


def nav_fields(b, up=0, down=0, left=0, right=0):
    for k, v in (('m_SelectOnUp', up), ('m_SelectOnDown', down), ('m_SelectOnLeft', left), ('m_SelectOnRight', right)):
        b = sub(b, k, ref(v), indent=4)
    b = sub(b, 'm_Mode', '4', indent=4)
    return b


def child(parent, name, **kw):
    return Node(name, parent, **kw)


# ======================== 階層 ========================
root = Node('RunSetupCanvas', key='root')
root.rect.update(amin=(0, 0), amax=(0, 0), pos=(0, 0), size=(0, 0), pivot=(0, 0), scale=0)

backdrop = child(root, 'Backdrop').stretch()
backdrop.image(col('#000000', 1), 'SetupVignette', raycast=True)
tint = child(backdrop, 'Tint').stretch()
tint.image(col(INK, 0.45))

content = child(root, 'Content').stretch(96, 40, 96, 44)
content.group(True, spacing=18, align=0, cw=1, ch=1, ew=1, eh=0)

# --- 見出し ---
header = child(content, 'Header')
header.canvas_group()
header.group(False, spacing=40, align=6, cw=1, ch=1, ew=0, eh=0)
header.layout(ph=124)

title_block = child(header, 'TitleBlock')
title_block.group(True, spacing=2, align=6, cw=1, ch=1, ew=1, eh=0)
title_block.layout(fw=1)
eyebrow_row = child(title_block, 'Eyebrow')
eyebrow_row.group(False, spacing=14, align=3, cw=1, ch=1)
seal = child(eyebrow_row, 'Seal')
seal.image(col(CRIMSON), 'SetupDiamond')
seal.layout(pw=18, ph=18)
eyebrow = child(eyebrow_row, 'Text')
eyebrow.tmp('出陣の前に', 22, col(GOLD), h=1, wrap=False, spacing=16)
heading = child(title_block, 'Heading')
heading.tmp('挑戦の準備', 80, col(KINARI), h=1, v=1024, wrap=False, spacing=8, brush=True)

name_block = child(header, 'NameBlock')
name_block.group(True, spacing=10, align=6, cw=1, ch=1, ew=1, eh=0)
name_block.layout(pw=480)
name_label = child(name_block, 'NameLabel')
name_label.tmp('挑戦者名', 22, col(GOLD), h=1, wrap=False, spacing=8)

name_input = child(name_block, 'NameInput')
name_input.image(col('#0E0E10', 0.96), raycast=True)
name_input.layout(ph=64)
ni_frame = child(name_input, 'Frame').stretch()
ni_frame.image(col(KINARI, 0.2), 'SetupFrame', sliced=True)
ni_line = child(name_input, 'Underline').at((0, 0), (1, 0), (0, 1), (0, 2), (0.5, 0.5))
ni_line.image(col(GOLD, 0.85))
ni_hover = child(name_input, 'HoverLayer').stretch()
ni_hover.canvas_group(0, 1, 0)
ni_hover.image(col(KINARI, 0.05))
viewport = child(name_input, 'Viewport').stretch(22, 6, 22, 6)
viewport.rect_mask()
name_text = child(viewport, 'Text').stretch()
name_text.ids['TMP'] = KEEP['NameText.TMP']
name_text.tmp('​', 26, col(KINARI), h=1, v=512, wrap=False)
placeholder = child(viewport, 'Placeholder').stretch()
placeholder.tmp('名を入力', 26, col(MUTED, 0.8), h=1, v=512, wrap=False)
ni_focus = child(name_input, 'FocusFrame').stretch(-10, -10, -10, -10)
ni_focus.canvas_group(0, 1, 0)
ni_focus.image(col(KINARI), 'SetupFocusCorners', sliced=True)

header_rule = child(content, 'HeaderRule')
header_rule.image(col(GOLD, 0.5), 'SetupFadeLine')
header_rule.layout(ph=2)

# --- 本体（左：加護カード / 右：情報） ---
body = child(content, 'Body')
body.group(False, spacing=56, align=0, cw=1, ch=1, ew=0, eh=1)
body.layout(fh=1)

left = child(body, 'Blessings')
left.canvas_group()
left.group(True, spacing=18, align=0, cw=1, ch=1, ew=1, eh=0)
left.layout(fw=1, minw=640)

cards = {}
# Editor上の見た目確認用の仮文字。実行時はRunSetupUIがRunSessionの値で上書きする。
PREVIEW = {
    'Attack': ['与ダメージ ×1.2', '命中時のゲージ回復 ×2', '低HPほど追加威力（最大 +35%）'],
    'Defense': ['被ダメージ ×0.8', '幽霊化の開始・継続消費 ×0.7', 'ゲージ自然回復 ×1.3'],
}
groups = {}


def section_heading(parent, name, label):
    row = child(parent, name)
    row.group(False, spacing=14, align=3, cw=1, ch=1, ew=0, eh=0)
    row.layout(ph=50)
    mark = child(row, 'Mark')
    mark.image(col(CRIMSON), 'SetupDiamond')
    mark.layout(pw=16, ph=16)
    t = child(row, 'Label')
    t.tmp(label, 36, col(KINARI), h=1, wrap=False, spacing=4, brush=True)
    line = child(row, 'Line')
    line.image(col(GOLD, 0.28), 'SetupFadeRight')
    line.layout(fw=1, ph=1)
    hint = child(row, 'Hint')
    hint.tmp('決定ボタンで装備', 20, col(MUTED), h=4, wrap=False)
    return row


def build_card(parent, kind, i):
    card = child(parent, '%s_%d' % (kind, i))
    card.image(col(CARD, 0.96), raycast=True)
    base = child(card, 'BaseFrame').stretch()
    base.image(col(KINARI, 0.14), 'SetupFrame', sliced=True)
    hover = child(card, 'HoverLayer').stretch()
    hover.canvas_group(0, 1, 0)
    hover.image(col(KINARI, 0.06))
    sel = child(card, 'Selected').stretch()
    sel.canvas_group(0, 1, 0)
    fill = child(sel, 'Fill').stretch()
    fill.image(col(CRIMSON_DEEP, 0.55))
    glow = child(sel, 'Glow').stretch()
    glow.image(col(CRIMSON, 0.7), 'SetupGradient')
    sframe = child(sel, 'Frame').stretch()
    sframe.image(col(GOLD), 'SetupFrame', sliced=True)
    topbar = child(sel, 'TopBar').at((0, 1), (1, 1), (0, -2), (0, 4), (0.5, 0.5))
    topbar.image(col(GOLD))
    badge = child(sel, 'Badge').at((1, 1), (1, 1), (-12, -12), (84, 30), (1, 1))
    badge.image(col(GOLD))
    btxt = child(badge, 'Text').stretch()
    btxt.tmp('装備中', 18, col(INK), h=2, v=512, wrap=False, spacing=4)
    flash = child(card, 'Flash').stretch()
    flash.canvas_group(0, 1, 0)
    flash.image(col(KINARI, 0.22))
    body_ = child(card, 'Content').stretch()
    body_.group(True, pad=(22, 22, 22, 16), spacing=12, align=0, cw=1, ch=1, ew=1, eh=0)
    top = child(body_, 'TopRow')
    top.group(False, spacing=16, align=3, cw=1, ch=1, ew=0, eh=0)
    top.layout(ph=64)
    emblem = child(top, 'Emblem')
    emblem.image(col(INK, 0.9))
    emblem.layout(pw=64, ph=64)
    eframe = child(emblem, 'Frame').stretch()
    eframe.image(col(GOLD, 0.75), 'SetupFrame', sliced=True)
    etxt = child(emblem, 'Text').stretch()
    etxt.tmp(OLD_EMBLEMS[kind][i], 38, col(KINARI), h=2, v=512, wrap=False, brush=True)
    label = child(top, 'Label')
    label.tmp(OLD_LABELS[kind][i], 40, col(KINARI), h=1, v=512, wrap=False, auto=(28, 40), overflow=1, spacing=4, brush=True)
    label.layout(fw=1)
    effect = child(body_, 'Effect')
    effect.tmp(PREVIEW[kind][i], 22, col(KINARI_SUB), h=1, v=256, wrap=True, auto=(17, 22), line=16, overflow=1)
    effect.layout(fh=1)
    focus = child(card, 'FocusFrame').stretch(-10, -10, -10, -10)
    focus.canvas_group(0, 1, 0)
    focus.image(col(KINARI), 'SetupFocusCorners', sliced=True)
    card.parts = dict(hover=hover, sel=sel, flash=flash, focus=focus, effect=effect)
    cards[(kind, i)] = card
    return card


for kind, label in (('Attack', '攻撃の加護'), ('Defense', '防御の加護')):
    if kind == 'Defense':
        gap = child(left, 'Gap')
        gap.layout(ph=10)
    section_heading(left, kind + 'Heading', label)
    opts = child(left, kind + 'Options')
    groups[kind] = opts.toggle_group()
    opts.group(False, spacing=22, align=0, cw=1, ch=1, ew=1, eh=1)
    opts.layout(minh=164, ph=200)
    for i in range(3):
        build_card(opts, kind, i)

right = child(body, 'Details')
right.canvas_group()
right.group(True, spacing=18, align=0, cw=1, ch=1, ew=1, eh=0)
right.layout(pw=600)


def panel(parent, name, title):
    p = child(parent, name)
    p.image(col(PANEL, 0.92))
    p.group(True, pad=(28, 28, 20, 22), spacing=12, align=0, cw=1, ch=1, ew=1, eh=0)
    frame = child(p, 'Frame').stretch()
    frame.image(col(KINARI, 0.1), 'SetupFrame', sliced=True)
    frame.layout(ignore=1)
    t = child(p, 'Title')
    t.tmp(title, 28, col(GOLD), h=1, wrap=False, spacing=6, brush=True)
    rule = child(p, 'Rule')
    rule.image(col(GOLD, 0.35), 'SetupFadeRight')
    rule.layout(ph=1)
    return p


# 右パネルの詳しい説明（数値はRunSessionの効果文で別に表示するため、ここには書かない）。
DETAILS = {
    'Attack': ['すべての攻撃の威力が上がる。素直に火力で押し切りたい時に。',
               '攻撃を当てるたびにゲージが多くたまる。手数で技を回したい時に。',
               '自分のHPが減るほど攻撃が強くなる。危険を背負って逆転を狙う。'],
    'Defense': ['受けるダメージを常に減らす。被弾を抑えて粘り強く戦える。',
                '幽霊化に入る時と続ける間のゲージ消費が減る。回避を多用する時に。',
                'ゲージが自然に回復する速さが上がる。攻めと守りの両方に余裕が出る。'],
}
loadout = panel(right, 'LoadoutPanel', '装備中の加護')
texts = {}
for kind, tag in (('Attack', '攻'), ('Defense', '防')):
    row = child(loadout, kind + 'Row')
    row.group(False, spacing=18, align=0, cw=1, ch=1, ew=0, eh=0)
    row.layout(minh=48)
    seal_ = child(row, 'Tag')
    seal_.image(col(CRIMSON))
    seal_.layout(pw=48, ph=48)
    st = child(seal_, 'Text').stretch()
    st.tmp(tag, 28, col(KINARI), h=2, v=512, wrap=False, brush=True)
    col_ = child(row, 'Text')
    col_.group(True, spacing=4, align=0, cw=1, ch=1, ew=1, eh=0)
    col_.layout(fw=1)
    head_ = child(col_, 'Head')
    head_.group(False, spacing=16, align=6, cw=1, ch=1, ew=0, eh=0)
    nm = child(head_, 'Name')
    texts[kind + 'Name'] = nm.tmp(OLD_LABELS[kind][0], 32, col(KINARI), h=1, v=1024, wrap=False, spacing=4, brush=True)
    ds = child(head_, 'Description')
    texts[kind + 'Description'] = ds.tmp(PREVIEW[kind][0], 24, col(GOLD), h=1, v=1024, wrap=False, auto=(17, 24), overflow=1)
    ds.layout(fw=1)
    dt = child(col_, 'Detail')
    texts[kind + 'Detail'] = dt.tmp(DETAILS[kind][0], 21, col(KINARI_SUB), h=1, v=256, wrap=True, line=20)

info = panel(right, 'InfoPanel', '挑戦の掟')
opp = child(info, 'Opponent')
texts['Opponent'] = opp.tmp('最初の相手：名もなき守人', 24, col(KINARI), h=1, v=256, wrap=True, line=16)
rules = child(info, 'Rules')
texts['Rules'] = rules.tmp('命は1つ。倒されれば挑戦終了。勝利すると痕跡を更新。', 21, col(KINARI_SUB), h=1, v=256, wrap=True, line=20)

result = child(right, 'ResultPanel', active=False)
result.image(col(CRIMSON_DEEP, 0.6))
result.group(True, pad=(28, 24, 14, 14), spacing=0, align=0, cw=1, ch=1, ew=1, eh=0)
rbar = child(result, 'Bar').at((0, 0), (0, 1), (0, 0), (5, 0), (0, 0.5))
rbar.image(col(CRIMSON))
rbar.layout(ignore=1)
rtxt = child(result, 'Text')
texts['Result'] = rtxt.tmp('', 22, col(ERROR), h=1, v=256, wrap=True, line=16)

footer_rule = child(content, 'FooterRule')
footer_rule.image(col(GOLD, 0.35), 'SetupFadeLine')
footer_rule.layout(ph=2)

footer = child(content, 'Footer')
footer.canvas_group()
footer.group(False, spacing=28, align=5, cw=1, ch=1, ew=0, eh=0)
footer.layout(ph=100)
hint = child(footer, 'InputHint')
hint.tmp('移動：方向キー／スティック　　決定：Enter／Aボタン\n金枠＝装備中　　四隅の印＝操作中',
         20, col(MUTED), h=1, v=512, wrap=True, auto=(16, 20), line=20)
hint.layout(fw=1)


def build_button(parent, name, primary, label, caption):
    btn = child(parent, name)
    btn.image(col(CRIMSON) if primary else col(PANEL, 0.94), raycast=True)
    btn.layout(pw=440 if primary else 340, ph=100 if primary else 84)
    if primary:
        shade = child(btn, 'Shade').stretch()
        shade.image(col('#000000', 0.45), 'SetupGradient')
        frame = child(btn, 'Frame').stretch(6, 6, 6, 6)
        frame.image(col(GOLD, 0.95), 'SetupFrame', sliced=True)
    else:
        frame = child(btn, 'Frame').stretch()
        frame.image(col(KINARI, 0.4), 'SetupFrame', sliced=True)
    hover = child(btn, 'HoverLayer').stretch()
    hover.canvas_group(0, 1, 0)
    hover.image(col(KINARI, 0.12 if primary else 0.07))
    flash = child(btn, 'Flash').stretch()
    flash.canvas_group(0, 1, 0)
    flash.image(col(KINARI, 0.45 if primary else 0.25))
    inner = child(btn, 'Content').stretch(16, 8, 16, 8)
    inner.group(True, spacing=0, align=4, cw=1, ch=1, ew=1, eh=0)
    lab = child(inner, 'Label')
    lab.tmp(label, 46 if primary else 34, col(KINARI), h=2, v=512, wrap=False, auto=(28, 46 if primary else 34), overflow=1, spacing=10, brush=True)
    cap = child(inner, 'Caption')
    cap.tmp(caption, 18, col(KINARI, 0.85) if primary else col(KINARI_SUB), h=2, v=256, wrap=False, auto=(14, 18), overflow=1)
    focus = child(btn, 'FocusFrame').stretch(-10, -10, -10, -10)
    focus.canvas_group(0, 1, 0)
    focus.image(col(KINARI), 'SetupFocusCorners', sliced=True)
    btn.parts = dict(hover=hover, flash=flash, focus=focus)
    return btn


tutorial = build_button(footer, 'TutorialButton', False, '修練場へ', '基本操作と攻撃を練習')
start = build_button(footer, 'StartButton', True, 'ボス戦へ', '修練場を省略して挑戦する')

# ======================== 操作部品 ========================
EFFECT_GUID = '1d7604166c87495e81b0d2403ab21ff1'


def effect(node, hover, focus, flash=None, selected=None, focus_scale=1.03, hover_scale=1.015):
    node.mono('Effect', EFFECT_GUID, 'Assembly-CSharp::RunSetupSelectableEffect',
              lambda: ('  _focusFrame: %s\n  _hoverLayer: %s\n  _selectedLayer: %s\n  _pressFlash: %s\n  _scaleTarget: {fileID: 0}\n'
                       '  _focusScale: %g\n  _hoverScale: %g\n  _focusFrameStartScale: 1.08\n  _selectedStartScale: 1.06\n'
                       '  _pressPunch: -0.05\n  _stateDuration: 0.12\n  _selectDuration: 0.22\n  _pressDuration: 0.25\n')
              % (ref(focus.ids['CanvasGroup']), ref(hover.ids['CanvasGroup']),
                 ref(selected.ids['CanvasGroup']) if selected else ref(0),
                 ref(flash.ids['CanvasGroup']) if flash else ref(0), focus_scale, hover_scale))


def sel_id(node):
    return node.cid('Selectable')


# 明示ナビゲーション：名前 → 攻撃 → 防御 → ボタン列
def card_nav(kind, i):
    other = 'Defense' if kind == 'Attack' else 'Attack'
    up = sel_id(name_input) if kind == 'Attack' else sel_id(cards[('Attack', i)])
    down = sel_id(cards[('Defense', i)]) if kind == 'Attack' else sel_id(start)
    left_ = sel_id(cards[(kind, i - 1)]) if i > 0 else 0
    right_ = sel_id(cards[(kind, i + 1)]) if i < 2 else 0
    return up, down, left_, right_


for (kind, i), card in cards.items():
    def toggle_body(kind=kind, i=i, card=card):
        b = rego(T_TOGGLE, card.go)
        b = nav_fields(b, *card_nav(kind, i))
        b = sub(b, 'm_Transition', '0')
        b = sub(b, 'm_TargetGraphic', ref(card.ids['Image']))
        b = sub(b, 'graphic', '{fileID: 0}')
        b = sub(b, 'm_Group', ref(groups[kind]))
        b = sub(b, 'm_IsOn', '1' if i == 0 else '0')
        return b
    card.add(114, 'Selectable', toggle_body)
    p = card.parts
    effect(card, p['hover'], p['focus'], p['flash'], p['sel'])

for btn, nav in ((tutorial, lambda: (sel_id(cards[('Defense', 1)]), 0, 0, sel_id(start))),
                 (start, lambda: (sel_id(cards[('Defense', 2)]), 0, sel_id(tutorial), 0))):
    def button_body(btn=btn, nav=nav):
        b = rego(T_BUTTON, btn.go)
        b = nav_fields(b, *nav())
        b = sub(b, 'm_Transition', '0')
        b = sub(b, 'm_TargetGraphic', ref(btn.ids['Image']))
        return b
    btn.add(114, 'Selectable', button_body)
    p = btn.parts
    effect(btn, p['hover'], p['focus'], p['flash'], focus_scale=1.04, hover_scale=1.02)


def input_body():
    b = rego(T_INPUT, name_input.go)
    b = nav_fields(b, 0, sel_id(cards[('Attack', 0)]), 0, 0)
    b = sub(b, 'm_Transition', '0')
    b = sub(b, 'm_TargetGraphic', ref(name_input.ids['Image']))
    b = sub(b, 'm_TextViewport', ref(viewport.rt))
    b = sub(b, 'm_TextComponent', ref(KEEP['NameText.TMP']))
    b = sub(b, 'm_Placeholder', ref(placeholder.ids['TMP']))
    b = sub(b, 'm_GlobalPointSize', '26')
    b = sub(b, 'm_CaretColor', colstr(col(KINARI)))
    b = sub(b, 'm_CustomCaretColor', '1')
    b = sub(b, 'm_CaretWidth', '2')
    b = sub(b, 'm_SelectionColor', colstr(col(CRIMSON, 0.6)))
    b = sub(b, 'm_GlobalFontAsset', BODY)
    return b


name_input.add(114, 'Selectable', input_body)
effect(name_input, ni_hover, ni_focus, focus_scale=1.0, hover_scale=1.0)

# ======================== ルート ========================
root.ids['Canvas'] = KEEP['root.Canvas']
root.add(223, 'Canvas', lambda: OLD[str(KEEP['root.Canvas'])])


def scaler_body():
    b = OLD[str(KEEP['root.Scaler'])]
    b = sub(b, 'm_ReferenceResolution', '{x: 1920, y: 1080}')
    b = sub(b, 'm_MatchWidthOrHeight', '0.5')
    # Expand：どの画面比率でも基準解像度より狭くならず、縦長・横長で詰まらない。
    b = sub(b, 'm_ScreenMatchMode', '1')
    return b


root.ids['Scaler'] = KEEP['root.Scaler']
root.add(114, 'Scaler', scaler_body)
root.ids['Raycaster'] = KEEP['root.Raycaster']
root.add(114, 'Raycaster', lambda: OLD[str(KEEP['root.Raycaster'])])
root.ids['CanvasGroup'] = KEEP['root.CanvasGroup']
root.add(225, 'CanvasGroup', lambda: OLD[str(KEEP['root.CanvasGroup'])])


def runsetup_body():
    b = OLD[str(KEEP['root.RunSetupUI'])]
    head = b.split('  _panel:')[0]
    arr = lambda xs: '\n' + ''.join('  - %s\n' % ref(x) for x in xs)
    strs = lambda xs: '\n' + ''.join('  - %s\n' % json.dumps(x) for x in xs)
    sel = lambda kind: [sel_id(cards[(kind, i)]) for i in range(3)]
    eff = lambda kind: [cards[(kind, i)].parts['effect'].ids['TMP'] for i in range(3)]
    reveal = [header.ids['CanvasGroup'], left.ids['CanvasGroup'], right.ids['CanvasGroup'], footer.ids['CanvasGroup']]
    return head + (
        '  _panel: %s\n  _titleStartPrompt: {fileID: 0}\n  _nameInput: %s\n'
        '  _attackOptions:%s  _defenseOptions:%s  _attackEffects:%s  _defenseEffects:%s'
        '  _attackName: %s\n  _defenseName: %s\n  _attackDescription: %s\n  _defenseDescription: %s\n'
        '  _attackDetail: %s\n  _defenseDetail: %s\n  _attackDetails:%s  _defenseDetails:%s'
        '  _opponent: %s\n  _rules: %s\n  _result: %s\n  _resultGroup: %s\n  _startButton: %s\n  _tutorialButton: %s\n'
        '  _content: %s\n  _revealGroups:%s  _openOffset: {x: 0, y: -36}\n  _openDuration: 0.35\n  _revealStagger: 0.05\n'
    ) % (ref(KEEP['root.CanvasGroup']), ref(sel_id(name_input)), arr(sel('Attack')), arr(sel('Defense')),
         arr(eff('Attack')), arr(eff('Defense')), ref(texts['AttackName']), ref(texts['DefenseName']),
         ref(texts['AttackDescription']), ref(texts['DefenseDescription']),
         ref(texts['AttackDetail']), ref(texts['DefenseDetail']), strs(DETAILS['Attack']), strs(DETAILS['Defense']), ref(texts['Opponent']),
         ref(texts['Rules']), ref(texts['Result']), ref(result.go), ref(sel_id(start)), ref(sel_id(tutorial)),
         ref(content.rt), arr(reveal))


root.ids['RunSetupUI'] = KEEP['root.RunSetupUI']
root.add(114, 'RunSetupUI', runsetup_body)

# ======================== 出力 ========================
out = [HEADER.rstrip('\n') + '\n']
for n in Node.all:
    comps = [n.rt] + [c[1] for c in n.comps]
    out.append('--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
               '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n%s'
               '  m_Layer: 5\n  m_Name: %s\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n'
               '  m_StaticEditorFlags: 0\n  m_IsActive: %d\n'
               % (n.go, ''.join('  - component: {fileID: %d}\n' % c for c in comps), n.name, 1 if n.active else 0))
    r = n.rect
    s = r['scale']
    out.append('--- !u!224 &%d\nRectTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
               '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n'
               '  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n'
               '  m_LocalScale: {x: %g, y: %g, z: %g}\n  m_ConstrainProportionsScale: 0\n  m_Children:%s\n'
               '  m_Father: {fileID: %d}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n'
               '  m_AnchorMin: {x: %g, y: %g}\n  m_AnchorMax: {x: %g, y: %g}\n  m_AnchoredPosition: {x: %g, y: %g}\n'
               '  m_SizeDelta: {x: %g, y: %g}\n  m_Pivot: {x: %g, y: %g}\n'
               % (n.rt, n.go, s, s, s, (''.join('\n  - {fileID: %d}' % c.rt for c in n.children) if n.children else ' []'),
                  n.parent.rt if n.parent else 0, *r['amin'], *r['amax'], *r['pos'], *r['size'], *r['pivot']))
    for cls, fid, fn in n.comps:
        out.append('--- !u!%d &%d\n%s' % (cls, fid, fn()))

data = ''.join(o if o.endswith('\n') else o + '\n' for o in out)
open(DST, 'w', encoding='utf-8', newline='\r\n').write(data)
print('objects', len(Node.all), 'bytes', len(data))
