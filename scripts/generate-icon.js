const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const GRID = 256;
const SUPERSAMPLING = 4;
const ICON_SIZE = 1024;
const ASSETS_DIR = path.join(__dirname, '..', 'desktop', 'resources');

const tile = { radius: 56, fill: [0x17, 0x19, 0x1b], border: [0x2c, 0x30, 0x33], borderWidth: 8 };
const chevron = { points: [[70, 82], [126, 128], [70, 174]], width: 26, color: [0xa9, 0xc4, 0xb4] };
const cursor = { x: 142, y: 158, width: 50, height: 26, radius: 6, color: [0x7a, 0x9f, 0x8b] };

function roundedRectDistance(x, y, left, top, width, height, radius) {
  const halfWidth = width / 2;
  const halfHeight = height / 2;
  const dx = Math.abs(x - (left + halfWidth)) - (halfWidth - radius);
  const dy = Math.abs(y - (top + halfHeight)) - (halfHeight - radius);
  const outside = Math.hypot(Math.max(dx, 0), Math.max(dy, 0));
  return outside + Math.min(Math.max(dx, dy), 0) - radius;
}

function segmentDistance(x, y, [ax, ay], [bx, by]) {
  const abx = bx - ax;
  const aby = by - ay;
  const t = Math.max(0, Math.min(1, ((x - ax) * abx + (y - ay) * aby) / (abx * abx + aby * aby)));
  return Math.hypot(x - (ax + t * abx), y - (ay + t * aby));
}

function chevronDistance(x, y) {
  const [first, middle, last] = chevron.points;
  return Math.min(segmentDistance(x, y, first, middle), segmentDistance(x, y, middle, last)) - chevron.width / 2;
}

function sampleColor(x, y) {
  const tileDistance = roundedRectDistance(x, y, 0, 0, GRID, GRID, tile.radius);
  if (tileDistance > 0) return null;
  if (chevronDistance(x, y) <= 0) return chevron.color;
  if (roundedRectDistance(x, y, cursor.x, cursor.y, cursor.width, cursor.height, cursor.radius) <= 0) return cursor.color;
  if (tileDistance > -tile.borderWidth) return tile.border;
  return tile.fill;
}

function renderPixels(size) {
  const pixels = Buffer.alloc(size * size * 4);
  const scale = GRID / size;
  const samples = SUPERSAMPLING * SUPERSAMPLING;
  for (let row = 0; row < size; row++) {
    for (let column = 0; column < size; column++) {
      let red = 0;
      let green = 0;
      let blue = 0;
      let covered = 0;
      for (let sy = 0; sy < SUPERSAMPLING; sy++) {
        for (let sx = 0; sx < SUPERSAMPLING; sx++) {
          const color = sampleColor((column + (sx + 0.5) / SUPERSAMPLING) * scale, (row + (sy + 0.5) / SUPERSAMPLING) * scale);
          if (!color) continue;
          red += color[0];
          green += color[1];
          blue += color[2];
          covered++;
        }
      }
      const offset = (row * size + column) * 4;
      if (covered === 0) continue;
      pixels[offset] = Math.round(red / covered);
      pixels[offset + 1] = Math.round(green / covered);
      pixels[offset + 2] = Math.round(blue / covered);
      pixels[offset + 3] = Math.round((covered / samples) * 255);
    }
  }
  return pixels;
}

const crcTable = Array.from({ length: 256 }, (_, index) => {
  let value = index;
  for (let bit = 0; bit < 8; bit++) value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
  return value >>> 0;
});

function crc32(buffer) {
  let crc = 0xffffffff;
  for (const byte of buffer) crc = crcTable[(crc ^ byte) & 0xff] ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
}

function pngChunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);
  const typeAndData = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(typeAndData));
  return Buffer.concat([length, typeAndData, crc]);
}

function encodePng(size, pixels) {
  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0);
  header.writeUInt32BE(size, 4);
  header[8] = 8;
  header[9] = 6;
  const stride = size * 4;
  const raw = Buffer.alloc((stride + 1) * size);
  for (let row = 0; row < size; row++) pixels.copy(raw, row * (stride + 1) + 1, row * stride, (row + 1) * stride);
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    pngChunk('IHDR', header),
    pngChunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    pngChunk('IEND', Buffer.alloc(0)),
  ]);
}

fs.mkdirSync(ASSETS_DIR, { recursive: true });
fs.writeFileSync(path.join(ASSETS_DIR, 'icon.png'), encodePng(ICON_SIZE, renderPixels(ICON_SIZE)));
console.log(`Icône écrite dans ${ASSETS_DIR} (${ICON_SIZE} px)`);
