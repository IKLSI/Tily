const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');

const ICON_SIZE = 1024;
const ASSETS_DIR = path.join(__dirname, '..', 'desktop', 'resources');
const SQUIRCLE_EXPONENT = 4.6;
const SQUIRCLE_STEPS = 360;

function squirclePath(center, radius) {
  const points = [];
  for (let step = 0; step < SQUIRCLE_STEPS; step++) {
    const angle = (2 * Math.PI * step) / SQUIRCLE_STEPS;
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    const x = center + radius * Math.sign(cos) * Math.abs(cos) ** (2 / SQUIRCLE_EXPONENT);
    const y = center + radius * Math.sign(sin) * Math.abs(sin) ** (2 / SQUIRCLE_EXPONENT);
    points.push(`${x.toFixed(2)},${y.toFixed(2)}`);
  }
  return `M${points.join(' L')} Z`;
}

function iconSvg() {
  const tile = squirclePath(512, 412);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${ICON_SIZE}" height="${ICON_SIZE}" viewBox="0 0 1024 1024">
  <defs>
    <linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1f2224"/><stop offset="1" stop-color="#151719"/></linearGradient>
    <filter id="shadow" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="10" stdDeviation="12" flood-color="#000" flood-opacity=".3"/></filter>
  </defs>
  <path d="${tile}" fill="url(#body)" filter="url(#shadow)"/>
  <path d="${tile}" fill="none" stroke="#2c3033" stroke-width="4"/>
  <polyline points="330,400 438,500 330,600" fill="none" stroke="#a9c4b4" stroke-width="44" stroke-linecap="round" stroke-linejoin="round"/>
  <line x1="508" y1="600" x2="660" y2="600" stroke="#7a9f8b" stroke-width="44" stroke-linecap="round"/>
</svg>`;
}

const svgPath = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'tily-icon-')), 'icon.svg');
fs.writeFileSync(svgPath, iconSvg());
fs.mkdirSync(ASSETS_DIR, { recursive: true });
execFileSync('sips', ['-s', 'format', 'png', '-z', String(ICON_SIZE), String(ICON_SIZE), svgPath, '--out', path.join(ASSETS_DIR, 'icon.png')], { stdio: 'ignore' });
fs.rmSync(path.dirname(svgPath), { recursive: true, force: true });
console.log(`Icône écrite dans ${ASSETS_DIR} (${ICON_SIZE} px)`);
