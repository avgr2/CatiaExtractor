import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

const EPS = 0.001;

// Sort unordered line segments into a single connected path.
function buildClosedPath(lines) {
  if (!lines || lines.length === 0) return null;

  const segs = lines.map((l) => ({
    p1: { x: l.X1, y: l.Y1 },
    p2: { x: l.X2, y: l.Y2 },
  }));

  const result = [];
  let current = segs.shift();
  result.push(current.p1, current.p2);

  while (segs.length > 0) {
    const last = result[result.length - 1];
    let found = false;

    for (let i = 0; i < segs.length; i++) {
      const s = segs[i];
      const m1 = Math.abs(s.p1.x - last.x) < EPS && Math.abs(s.p1.y - last.y) < EPS;
      const m2 = Math.abs(s.p2.x - last.x) < EPS && Math.abs(s.p2.y - last.y) < EPS;

      if (m1 || m2) {
        segs.splice(i, 1);
        result.push(m1 ? s.p2 : s.p1);
        found = true;
        break;
      }
    }

    if (!found) break;
  }

  // Remove closing duplicate vertex
  const first = result[0];
  const last = result[result.length - 1];
  if (Math.abs(last.x - first.x) < EPS && Math.abs(last.y - first.y) < EPS) {
    result.pop();
  }

  return result.length >= 3 ? result : null;
}

// Group circles by center proximity so multi-center sketches (e.g. connecting rod)
// don't create holes that lie outside their parent shape — which produces degenerate
// geometry in Three.js ExtrudeGeometry.
function groupCirclesByCenter(circles) {
  const groups = [];
  for (const c of circles) {
    let added = false;
    for (const g of groups) {
      const ref = g[0];
      const dist = Math.hypot(c.CentreX - ref.CentreX, c.CentreY - ref.CentreY);
      // Tolerance: 1 unit (mm in CATIA); concentric circles share same center
      if (dist < 1.0) {
        g.push(c);
        added = true;
        break;
      }
    }
    if (!added) groups.push([c]);
  }
  return groups;
}

// Returns true when a sketch has no usable geometry.
export function isSketchEmpty(sketch) {
  const geo = sketch?.Geometrie;
  if (!geo) return true;
  const hasCircles = (geo.Cercles?.length ?? 0) > 0;
  const hasLines = (geo.Lignes?.length ?? 0) > 0;
  const hasArcs = (geo.Arcs?.length ?? 0) > 0;
  return !hasCircles && !hasLines && !hasArcs;
}

// Build an array of THREE.Shape from a sketch's 2D geometry (local XY plane).
// Returns null when no usable geometry exists.
export function buildShapesFromSketch(sketch) {
  const geo = sketch?.Geometrie;
  if (!geo) return null;

  const circles = (geo.Cercles ?? []).filter((c) => !c.EstConstruction);
  const lines   = (geo.Lignes  ?? []).filter((l) => !l.EstConstruction);

  if (circles.length === 0 && lines.length === 0) return null;

  // --- Circle-based profile ---
  if (circles.length > 0) {
    const groups = groupCirclesByCenter(circles);
    const shapes = [];

    for (const group of groups) {
      const sorted = [...group].sort((a, b) => b.Rayon - a.Rayon);
      const main = sorted[0];

      const shape = new THREE.Shape();
      shape.absarc(main.CentreX, main.CentreY, main.Rayon, 0, Math.PI * 2, false);

      // Only circles within the same center group become holes
      for (let i = 1; i < sorted.length; i++) {
        const h = sorted[i];
        const hole = new THREE.Path();
        hole.absarc(h.CentreX, h.CentreY, h.Rayon, 0, Math.PI * 2, true);
        shape.holes.push(hole);
      }

      shapes.push(shape);
    }

    return shapes;
  }

  // --- Line-based profile (rectangles, polygons) ---
  if (lines.length > 0) {
    const path = buildClosedPath(lines);
    if (!path) return null;

    const shape = new THREE.Shape();
    shape.moveTo(path[0].x, path[0].y);
    for (let i = 1; i < path.length; i++) shape.lineTo(path[i].x, path[i].y);
    shape.closePath();

    return [shape];
  }

  return null;
}

// Legacy single-shape helper (kept for callers that don't need multi-group support).
export function buildShapeFromSketch(sketch) {
  const shapes = buildShapesFromSketch(sketch);
  return shapes ? shapes[0] : null;
}

// Build an ExtrudeGeometry from a sketch + Pad/Pocket parameters.
// Handles multi-center sketches by merging individual extrusions.
export function buildExtrudeGeometry(sketch, params) {
  const shapes = buildShapesFromSketch(sketch);
  if (!shapes || shapes.length === 0) return null;

  const longueur       = params?.longueur       ?? params?.profondeur       ?? 10;
  const longueurSeconde= params?.longueur_seconde?? params?.profondeur_seconde?? 0;
  const estSymetrique  = params?.est_symetrique === 1;
  const totalDepth     = estSymetrique ? longueur + longueurSeconde : longueur;

  const geos = [];
  for (const shape of shapes) {
    try {
      const geo = new THREE.ExtrudeGeometry(shape, {
        depth: totalDepth,
        bevelEnabled: false,
        curveSegments: 48,
      });
      if (estSymetrique) geo.translate(0, 0, -longueurSeconde);
      geo.computeVertexNormals();
      geos.push(geo);
    } catch (e) {
      console.warn('[featureGeometry] ExtrudeGeometry failed:', e.message);
    }
  }

  if (geos.length === 0) return null;
  if (geos.length === 1) return geos[0];

  try {
    const merged = mergeGeometries(geos);
    merged.computeVertexNormals();
    return merged;
  } catch (e) {
    console.warn('[featureGeometry] mergeGeometries failed:', e.message);
    return geos[0];
  }
}

// Build Vector2[] profile for LatheGeometry, expressed as (r, z) in axis frame.
// Returns null if the sketch doesn't have a usable line profile.
export function buildLatheProfile(sketch) {
  const geo = sketch?.Geometrie;
  if (!geo) return null;

  const allLines = geo.Lignes ?? [];
  const axisLine = allLines.find((l) => l.EstConstruction);
  const profileLines = allLines.filter((l) => !l.EstConstruction);

  if (profileLines.length === 0) return null;

  let axisDx = 0;
  let axisDy = 1;
  if (axisLine) {
    const dx = axisLine.X2 - axisLine.X1;
    const dy = axisLine.Y2 - axisLine.Y1;
    const len = Math.sqrt(dx * dx + dy * dy);
    if (len > EPS) { axisDx = dx / len; axisDy = dy / len; }
  }

  const perpDx = -axisDy;
  const perpDy =  axisDx;

  const path = buildClosedPath(profileLines);
  if (!path) return null;

  const points = path.map((pt) => {
    const r = Math.abs(pt.x * perpDx + pt.y * perpDy);
    const z = pt.x * axisDx + pt.y * axisDy;
    return new THREE.Vector2(r, z);
  });

  if (points.length < 2) return null;
  return points;
}
