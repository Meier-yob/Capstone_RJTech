// Verify the formal UML SVG: parse rects + paths, test for box-box overlaps
// and connector-vs-box crossings (excluding endpoints on source/target boxes).
const fs = require('fs');
const svg = fs.readFileSync(process.argv[2], 'utf8');

// Parse <rect> boxes (the 3-section class boxes; exclude full-canvas bg rect)
const rects = [];
const rectRe = /<rect x="([\d.]+)" y="([\d.]+)" width="([\d.]+)" height="([\d.]+)" rx="7" fill="#FFFFFF"/g;
let m;
while ((m = rectRe.exec(svg))) {
  rects.push({ x: +m[1], y: +m[2], w: +m[3], h: +m[4], name: `rect@${m[1]},${m[2]}` });
}

// band rects are rx="10" with dashed stroke — they contain boxes; ignore for crossing tests
// parse connector paths (stroke-width="1.6")
const paths = [];
const pathRe = /<path d="M([\d.\s,\-L]+)" fill="none" stroke="([^"]+)" stroke-width="1.6"([^/]*)\/?>/g;
let mp;
while ((mp = pathRe.exec(svg))) {
  const ptsStr = mp[1];
  const pts = ptsStr.split(' L').map(s => s.trim().split(/\s+/).map(Number));
  // rebuild segments
  const segs = [];
  for (let i = 0; i < pts.length - 1; i++) segs.push([pts[i], pts[i + 1]]);
  paths.push({ segs, stroke: mp[2] });
}

// point in/on rect (with padding for stroke width)
function pointInRect(px, py, r, pad = 1) {
  return px >= r.x - pad && px <= r.x + r.w + pad && py >= r.y - pad && py <= r.y + r.h + pad;
}
// segment-rect intersection (axis-aligned): Liang-Barsky style
function segHitsRect(ax, ay, bx, by, r, pad = 0) {
  const x0 = Math.min(ax, bx), x1 = Math.max(ax, bx);
  const y0 = Math.min(ay, by), y1 = Math.max(ay, by);
  const rx0 = r.x - pad, rx1 = r.x + r.w + pad, ry0 = r.y - pad, ry1 = r.y + r.h + pad;
  // quick reject
  if (x1 < rx0 || x0 > rx1 || y1 < ry0 || y0 > ry1) return false;
  // segment endpoints inside?
  if (pointInRect(ax, ay, r) || pointInRect(bx, by, r)) return true;
  // use Liang-Barsky with t in [0,1]
  let t0 = 0, t1 = 1;
  const dx = bx - ax, dy = by - ay;
  const p = [-dx, dx, -dy, dy];
  const q = [ax - rx0, rx1 - ax, ay - ry0, ry1 - ay];
  for (let i = 0; i < 4; i++) {
    if (p[i] === 0) { if (q[i] < 0) return false; continue; }
    const t = q[i] / p[i];
    if (p[i] < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
    else { if (t < t0) return false; if (t < t1) t1 = t; }
  }
  return true;
}

let issues = 0;

// 1) box-box overlap
for (let i = 0; i < rects.length; i++) {
  for (let j = i + 1; j < rects.length; j++) {
    const a = rects[i], b = rects[j];
    const overlap = !(a.x + a.w < b.x || b.x + b.w < a.x || a.y + a.h < b.y || b.y + b.h < a.y);
    if (overlap) {
      issues++;
      console.log(`OVERLAP: ${a.name} vs ${b.name}`);
    }
  }
}

// 2) connector vs box crossings
// For each path segment, test against every box EXCEPT the two boxes whose innermost
// endpoints the segment touches (endpoints on a box edge are lawful).
function owningRect(px, py) {
  for (const r of rects) if (pointInRect(px, py, r, 1.5)) return r.name;
  return null;
}

paths.forEach((p, idx) => {
  for (const [a, b] of p.segs) {
    const ownA = owningRect(a[0], a[1]);
    const ownB = owningRect(b[0], b[1]);
    for (const r of rects) {
      if (r.name === ownA || r.name === ownB) continue;
      // exclude segment that lies exactly along a box's top border path area? none expected
      if (segHitsRect(a[0], a[1], b[0], b[1], r, 1.5)) {
        issues++;
        console.log(`CROSS: path#${idx} segment (${a})->(${b}) crosses ${r.name}`);
      }
    }
  }
});

// 3) connector-connector crossings (any two segments intersecting)
function segX(a1, a2, b1, b2) {
  const [ax1, ay1] = a1, [ax2, ay2] = a2;
  const [bx1, by1] = b1, [bx2, by2] = b2;
  const d1x = ax2 - ax1, d1y = ay2 - ay1;
  const d2x = bx2 - bx1, d2y = by2 - by1;
  const denom = d1x * d2y - d1y * d2x;
  if (denom === 0) return false; // parallel
  const t = ((bx1 - ax1) * d2y - (by1 - ay1) * d2x) / denom;
  const u = ((bx1 - ax1) * d1y - (by1 - ay1) * d1x) / denom;
  const eps = 2.5; // stroke-width padding
  return t > eps / 100 && t < 1 - eps / 100 && u > eps / 100 && u < 1 - eps / 100;
}

let xCount = 0;
for (let i = 0; i < paths.length; i++) {
  for (let j = i + 1; j < paths.length; j++) {
    for (const [a, b] of paths[i].segs) {
      for (const [c, d] of paths[j].segs) {
        if (segX(a, b, c, d)) {
          xCount++;
          const x = (a[0] + b[0] + c[0] + d[0]) / 4, y = (a[1] + b[1] + c[1] + d[1]) / 4;
          console.log(`XCROSS: path#${i} x path#${j} near (${x.toFixed(0)},${y.toFixed(0)})`);
        }
      }
    }
  }
}

console.log(`\nChecked ${rects.length} boxes, ${paths.length} connectors (${paths.reduce((s, p) => s + p.segs.length, 0)} segments).`);
console.log(`Connector-connector crossings: ${xCount}`);
console.log(issues === 0 ? 'OK — no box overlaps, no connector crossings through foreign boxes.' : `FOUND ${issues} box issue(s).`);