import { Resvg } from '@resvg/resvg-js';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';

const root = fileURLToPath(new URL('../../', import.meta.url));
const svg = readFileSync(resolve(root, 'assets/app-icon.svg'));
const sizes = [16, 32, 48, 64, 128, 256, 512, 1024];
const pngs = new Map(sizes.map(size => [size, Buffer.from(new Resvg(svg, {
  fitTo: { mode: 'width', value: size }, font: { loadSystemFonts: false }
}).render().asPng())]));
const outputs = new Map([...pngs].map(([size, bytes]) => [`assets/generated/app-${size}.png`, bytes]));
const iconSizes = sizes.filter(size => size <= 256);
const directory = Buffer.alloc(6 + iconSizes.length * 16);
directory.writeUInt16LE(1, 2); directory.writeUInt16LE(iconSizes.length, 4);
let offset = directory.length;
for (const [index, size] of iconSizes.entries()) {
  const data = pngs.get(size), at = 6 + index * 16;
  directory[at] = directory[at + 1] = size === 256 ? 0 : size;
  directory.writeUInt16LE(1, at + 4); directory.writeUInt16LE(32, at + 6);
  directory.writeUInt32LE(data.length, at + 8); directory.writeUInt32LE(offset, at + 12);
  offset += data.length;
}
const ico = Buffer.concat([directory, ...iconSizes.map(size => pngs.get(size))]);
outputs.set('assets/generated/app.ico', ico);
const chunks = [['icp4',16], ['icp5',32], ['icp6',64], ['ic07',128], ['ic08',256], ['ic09',512], ['ic10',1024], ['ic11',32], ['ic12',64], ['ic13',256], ['ic14',512]].map(([type,size]) => {
  const png = pngs.get(size), header = Buffer.alloc(8);
  header.write(type, 0, 4, 'ascii'); header.writeUInt32BE(png.length + 8, 4);
  return Buffer.concat([header, png]);
});
const header = Buffer.alloc(8); header.write('icns', 0, 4, 'ascii');
header.writeUInt32BE(8 + chunks.reduce((sum, chunk) => sum + chunk.length, 0), 4);
outputs.set('assets/generated/app.icns', Buffer.concat([header, ...chunks]));
outputs.set('ShopifyAdministration.Desktop/Assets/app.ico', ico);
outputs.set('ShopifyAdministration.Desktop/Assets/app.png', pngs.get(256));
const manifest = Object.fromEntries([...outputs].map(([name,data]) => [name, createHash('sha256').update(data).digest('hex')]));
outputs.set('assets/generated/sha256.json', Buffer.from(JSON.stringify(manifest, null, 2) + '\n'));
for (const [name,bytes] of outputs) {
  const file = resolve(root, name);
  if (process.argv.includes('--verify')) {
    if (!readFileSync(file).equals(bytes)) throw new Error(`Generated asset differs: ${name}`);
  } else { mkdirSync(resolve(file, '..'), { recursive: true }); writeFileSync(file, bytes); }
}
console.log(`Verified ${outputs.size} deterministic PNG/ICO/ICNS application resources.`);
