"""呼吸（青）・布（緑）の最大ずれ量と、血の経路・滴の位置（黄）を元画像に重ねて確認用画像を書き出す。"""
import sys, pickle, math
sys.path.insert(0, sys.argv[1]); import pngio
w, h, ch, px = pickle.load(open(sys.argv[1] + '/title.pkl', 'rb'))
def sstep(t): t = max(0., min(1., t)); return t * t * (3 - 2 * t)
def edge(t, e): return sstep(min(t, 1 - t) / e)
def img_to_uv(x, y): return (x - 1) / 1533, 1 - (y - 3) / 1017
BREATH = ((0.3842, 0.4641), (0.619, 0.7453)); CLOTH = ((0.3986, 0.2242), (0.5877, 0.4641))
def local(uvp, region):
    (u0, v0), (u1, v1) = region; return (uvp[0] - u0) / (u1 - u0), (uvp[1] - v0) / (v1 - v0)
def breath(u, v):
    vert = sstep(v / 0.62) * sstep((1 - v) / 0.08)
    half = 0.22; head = sstep((half - abs(u - 0.49)) / (half * 0.5)); above = sstep((v - 0.62) / 0.12)
    wgt = vert * (edge(u, 0.1) * (1 - above) + head * above)
    return math.hypot((u - .5) * 2 * .4 * wgt, 1.4 * wgt)
def cloth(u, v):
    vert = (max(0, (1 - v) / (1 - .14))) ** 1.6 if v >= .14 else sstep(v / .14)
    return 3.5 * vert * edge(u, .25)
x0, y0, x1, y1 = 540, 230, 1220, 920
W, H = x1 - x0, y1 - y0; out = bytearray(W * H * 4)
for y in range(H):
    for x in range(W):
        sx, sy = x0 + x, y0 + y; i = (sy * w + sx) * ch
        r, g, b = px[i], px[i + 1], px[i + 2]
        uvp = img_to_uv(sx, sy)
        for region, fn, colr, mx in ((BREATH, breath, (60, 140, 255), 1.5), (CLOTH, cloth, (60, 255, 120), 3.5)):
            u, v = local(uvp, region)
            if 0 <= u <= 1 and 0 <= v <= 1:
                k = min(1, fn(u, v) / mx) * 0.7
                r, g, b = [int(c * (1 - k) + t * k) for c, t in zip((r, g, b), colr)]
                if min(u, 1 - u, v, 1 - v) < 0.004: r, g, b = colr
        o = (y * W + x) * 4; out[o:o + 4] = bytes((r, g, b, 255))
def dot(px_, py_, rad=4):
    for dy in range(-rad, rad + 1):
        for dx in range(-rad, rad + 1):
            if dx * dx + dy * dy <= rad * rad:
                X, Y = px_ - x0 + dx, py_ - y0 + dy
                if 0 <= X < W and 0 <= Y < H: out[(Y * W + X) * 4:(Y * W + X) * 4 + 4] = bytes((255, 230, 0, 255))
for t in range(0, 101, 5): dot(int(926 + (1035 - 926) * t / 100), int(668 + (786 - 668) * t / 100), 1)
dot(978, 740); dot(1008, 766)
for X in range(960, 1030, 4): dot(X, 840, 1)
pngio.write(sys.argv[1] + '/heatmap.png', W, H, out)
print('ok')
