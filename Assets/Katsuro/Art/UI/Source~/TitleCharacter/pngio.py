import zlib, struct
def read(path):
    d = open(path, 'rb').read(); pos = 8; idat = b''; w = h = 0; ct = 0
    while pos < len(d):
        ln, = struct.unpack('>I', d[pos:pos+4]); t = d[pos+4:pos+8]; c = d[pos+8:pos+8+ln]; pos += 12 + ln
        if t == b'IHDR': w, h, bd, ct = struct.unpack('>IIBB', c[:10])
        elif t == b'IDAT': idat += c
    ch = {2: 3, 6: 4}[ct]; raw = zlib.decompress(idat); stride = w * ch
    out = bytearray(h * stride); prev = bytearray(stride); i = 0
    for y in range(h):
        f = raw[i]; i += 1; line = bytearray(raw[i:i+stride]); i += stride
        if f == 1:
            for x in range(ch, stride): line[x] = (line[x] + line[x-ch]) & 255
        elif f == 2:
            for x in range(stride): line[x] = (line[x] + prev[x]) & 255
        elif f == 3:
            for x in range(stride): line[x] = (line[x] + ((line[x-ch] if x >= ch else 0) + prev[x]) // 2) & 255
        elif f == 4:
            for x in range(stride):
                a = line[x-ch] if x >= ch else 0; b = prev[x]; c2 = prev[x-ch] if x >= ch else 0
                p = a + b - c2; pa, pb, pc = abs(p-a), abs(p-b), abs(p-c2)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c2)) & 255
        out[y*stride:(y+1)*stride] = line; prev = line
    return w, h, ch, out
def write(path, w, h, rgba):
    raw = b''.join(b'\x00' + bytes(rgba[y*w*4:(y+1)*w*4]) for y in range(h))
    def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t+d) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))
