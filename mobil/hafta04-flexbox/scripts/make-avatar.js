// Generates assets/karakter.png: a 16x16 pixel-art knight helmet scaled to
// 256x256. Original artwork drawn in code, so there are no copyright concerns.
// Uses only Node built-ins (zlib) to encode the PNG.
// Run: node scripts/make-avatar.js

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

// Each character is one pixel of the sprite.
const SPRITE = [
  '................',
  '......RRRR......',
  '.....RRRRRR.....',
  '......RRRR......',
  '.....KKKKKK.....',
  '....KLSSSSSK....',
  '...KLSSSSSSSK...',
  '...KLSSGGSSSK...',
  '...KKKKKKKKKK...',
  '...KSDDDDDDSK...',
  '...KLSSSSSSSK...',
  '...KLSDSDSDSK...',
  '...KLSSSSSSSK...',
  '....KSSSSSSK....',
  '.....KKKKKK.....',
  '................',
];

const PALETTE = {
  '.': [43, 58, 103], // background (dark blue)
  K: [27, 27, 47], // outline
  S: [154, 165, 177], // steel
  L: [217, 226, 236], // steel highlight
  G: [226, 164, 0], // gold emblem
  R: [201, 55, 44], // red plume
  D: [11, 11, 20], // visor slit / breathing holes
};

const SCALE = 16;
const SIZE = SPRITE.length * SCALE;

// CRC32 as required by the PNG chunk format.
const CRC_TABLE = Array.from({ length: 256 }, (_, n) => {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});

function crc32(buf) {
  let c = 0xffffffff;
  for (const byte of buf) c = CRC_TABLE[(c ^ byte) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

// Raw RGB scanlines, each prefixed with filter byte 0 (none).
const raw = Buffer.alloc(SIZE * (SIZE * 3 + 1));
let offset = 0;
for (let y = 0; y < SIZE; y++) {
  raw[offset++] = 0;
  const row = SPRITE[Math.floor(y / SCALE)];
  for (let x = 0; x < SIZE; x++) {
    const [r, g, b] = PALETTE[row[Math.floor(x / SCALE)]];
    raw[offset++] = r;
    raw[offset++] = g;
    raw[offset++] = b;
  }
}

const header = Buffer.alloc(13);
header.writeUInt32BE(SIZE, 0); // width
header.writeUInt32BE(SIZE, 4); // height
header[8] = 8; // bit depth
header[9] = 2; // colour type: truecolour RGB
header[10] = 0; // compression
header[11] = 0; // filter
header[12] = 0; // no interlace

const png = Buffer.concat([
  Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
  chunk('IHDR', header),
  chunk('IDAT', zlib.deflateSync(raw)),
  chunk('IEND', Buffer.alloc(0)),
]);

const out = path.join(__dirname, '..', 'assets', 'karakter.png');
fs.writeFileSync(out, png);
console.log(`Wrote ${out} (${SIZE}x${SIZE}, ${png.length} bytes)`);
