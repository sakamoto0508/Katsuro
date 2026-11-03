import sys, pickle; sys.path.insert(0, sys.argv[1]); import pngio
w, h, ch, px = pngio.read(sys.argv[2]); pickle.dump((w, h, ch, bytes(px)), open(sys.argv[1] + '/title.pkl', 'wb'))
def crop(name, x0, y0, x1, y1, scale=1, grid=0):
    W, H = (x1-x0)*scale, (y1-y0)*scale; out = bytearray(W*H*4)
    for y in range(H):
        for x in range(W):
            sx, sy = x0 + x//scale, y0 + y//scale; i = (sy*w+sx)*ch
            r, g, b = px[i], px[i+1], px[i+2]
            if grid and ((sx % grid == 0 and y % scale == 0) or (sy % grid == 0 and x % scale == 0)): r, g, b = 0, 255, 255
            o = (y*W+x)*4; out[o:o+4] = bytes((r, g, b, 255))
    pngio.write(sys.argv[1] + '/' + name + '.png', W, H, out)
crop('body', 560, 250, 1000, 950, 1, 50)
crop('sword', 860, 600, 1200, 920, 2, 25)
