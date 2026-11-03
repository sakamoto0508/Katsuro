"""1920x1080（Expand・16:9でCanvasはちょうど1920x1080）での文字幅・行数・高さの概算確認。"""
from fontTools.ttLib import TTFont
import math, sys

body = TTFont(sys.argv[1]); brush = TTFont(sys.argv[2])


def width(text, size, font, spacing=0):
    cmap = font.getBestCmap(); hm = font['hmtx']; upm = font['head'].unitsPerEm
    w = 0; missing = []
    for ch in text:
        g = cmap.get(ord(ch))
        if g is None: missing.append(ch); w += upm; continue
        w += hm[g][0]
    n = len(text)
    return w / upm * size + spacing / 100 * size * max(0, n - 1), missing


def lines(text, size, font, avail, spacing=0):
    total = 0
    for para in text.split('\n'):
        w, _ = width(para, size, font, spacing)
        total += max(1, math.ceil(w / avail - 1e-6))
    return total


def line_h(size, font, line_spacing):
    # TMP：行送り = (ascender - descender + lineGap) * size/upm + lineSpacing/100 * size
    hh = font['hhea']; upm = font['head'].unitsPerEm
    return (hh.ascent - hh.descent + hh.lineGap) / upm * size + line_spacing / 100 * size


def text_h(text, size, font, avail, ls=0, spacing=0):
    n = lines(text, size, font, avail, spacing)
    return n, line_h(size, font, 0) + (n - 1) * line_h(size, font, ls)


issues = []
def check(name, text, size, font, avail, maxlines=None, spacing=0, auto_min=None):
    w, miss = width(text.replace('\n', ''), size, font, spacing)
    n = lines(text, size, font, avail, spacing)
    ok = maxlines is None or n <= maxlines
    note = ''
    if not ok and auto_min:
        for s in range(size, auto_min - 1, -1):
            if lines(text, s, font, avail, spacing) <= maxlines: note = f'auto→{s}px'; ok = True; break
    if miss: issues.append(f'{name}: 欠け字 {miss}')
    if not ok: issues.append(f'{name}: {n}行 > {maxlines}')
    print(f'{name:22s} {size:>3}px 幅{w:7.1f}/{avail:6.1f} {n}行 {note} {"OK" if ok else "NG"}{" 欠字" + str(miss) if miss else ""}')


CW = 1920 - 192
LEFT = CW - 600 - 56
CARD = (LEFT - 44) / 3
CARD_IN = CARD - 44
RIGHT_IN = 600 - 56
ROW_TXT = RIGHT_IN - 48 - 18
print(f'Content幅 {CW}  左列 {LEFT}  カード {CARD:.1f}（内側 {CARD_IN:.1f}）  右パネル内側 {RIGHT_IN}  行テキスト {ROW_TXT}')

check('見出し', '挑戦の準備', 80, brush, CW - 480 - 40, 1, spacing=8)
check('小見出し', '出陣の前に', 22, body, CW - 480 - 40, 1, spacing=16)
for t in ['攻撃の加護', '防御の加護']: check('節見出し ' + t, t, 36, brush, LEFT, 1, spacing=4)
for n in ['剛力', '吸魂', '窮地', '堅守', '霊衣', '息吹']: check('カード名 ' + n, n, 40, brush, CARD_IN - 64 - 16, 1, spacing=4, auto_min=28)
effects = ['与ダメージ ×1.2', '命中時のゲージ回復 ×2', '低HPほど追加威力（最大 +35%）', '被ダメージ ×0.8', '幽霊化の開始・継続消費 ×0.7', 'ゲージ自然回復 ×1.3']
for e in effects: check('カード効果', e, 22, body, CARD_IN, 2, auto_min=17)
for n, e in zip(['剛力', '吸魂', '窮地', '堅守', '霊衣', '息吹'], effects):
    nw, _ = width(n, 32, brush, 4)
    check('右:効果値 ' + n, e, 24, body, ROW_TXT - nw - 16, 1, auto_min=17)
details = ['すべての攻撃の威力が上がる。素直に火力で押し切りたい時に。',
           '攻撃を当てるたびにゲージが多くたまる。手数で技を回したい時に。',
           '自分のHPが減るほど攻撃が強くなる。危険を背負って逆転を狙う。',
           '受けるダメージを常に減らす。被弾を抑えて粘り強く戦える。',
           '幽霊化に入る時と続ける間のゲージ消費が減る。回避を多用する時に。',
           'ゲージが自然に回復する速さが上がる。攻めと守りの両方に余裕が出る。']
for d in details: check('右:詳細', d, 21, body, ROW_TXT, 3)
check('相手', '前回の勝者：とても長い挑戦者の名前 / 窮地・霊衣', 24, body, RIGHT_IN, 2)
check('掟', '命は3つ。復活してもボスのHPは維持。勝利すると痕跡を更新。', 21, body, RIGHT_IN, 2)
check('操作ガイド', '移動：方向キー／スティック　　決定：Enter／Aボタン\n金枠＝装備中　　四隅の印＝操作中', 20, body, CW - 340 - 440 - 56, 2)
check('ボス戦へ', 'ボス戦へ', 46, brush, 440 - 32, 1, spacing=10)
check('修練場へ', '修練場へ', 34, brush, 340 - 32, 1, spacing=10)
check('ボス説明', '修練場を省略して挑戦する', 18, body, 440 - 32, 1)
check('修練説明', '基本操作と攻撃を練習', 18, body, 340 - 32, 1)
check('札 装備中', '装備中', 18, body, 84, 1, spacing=4)
check('名前入力(16字)', 'あ' * 16, 30, body, 480 - 44, 1)

# --- 高さ ---
avail = 1080 - 84 - 124 - 100 - 2 - 2 - 4 * 18
print('\nBody高さ', avail)
left = 50 + 18 + 200 + 18 + 10 + 18 + 50 + 18 + 200
print('左列', left, 'OK' if left <= avail else 'NG')
card_need = 22 + 64 + 12 + text_h(effects[5 - 3 + 1], 22, body, CARD_IN, 16)[1] + 16
print('カード内容', round(card_need), '/ 200')
worst = max(details, key=lambda d: lines(d, 21, body, ROW_TXT))
def row_h(desc):
    return max(48, 32 * 1.0 + 4 + text_h(desc, 21, body, ROW_TXT, 20)[1])
title_h = line_h(28, brush, 0)
load = 20 + title_h + 12 + 1 + 12 + row_h(worst) + 12 + row_h(worst) + 22
info = 20 + title_h + 12 + 1 + 12 + text_h('前回の勝者：とても長い挑戦者の名前 / 窮地・霊衣', 24, body, RIGHT_IN, 16)[1] + 12 + text_h('命は3つ。復活してもボスのHPは維持。勝利すると痕跡を更新。', 21, body, RIGHT_IN, 20)[1] + 22
res = 14 + text_h('保存に失敗しました：ディスクに書き込めません。\n前回の挑戦は敗北しました。', 22, body, 600 - 52, 16)[1] + 14
right = load + 18 + info
print('右列 装備', round(load), '掟', round(info), '結果欄', round(res), '→ 結果なし', round(right), '/ 結果あり', round(right + 18 + res), '/', avail)
if right + 18 + res > avail: issues.append('右列が結果欄ありでBody高さを超える')
print('\n問題:', issues or 'なし')
