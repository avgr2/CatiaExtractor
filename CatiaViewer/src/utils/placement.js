import * as THREE from 'three';

export function calculerMatricePlacement(placement) {
  const origin = new THREE.Vector3(
    placement.OrigineX ?? 0,
    placement.OrigineY ?? 0,
    placement.OrigineZ ?? 0
  );

  const axeX = new THREE.Vector3(
    placement.AxeXx ?? 1,
    placement.AxeXy ?? 0,
    placement.AxeXz ?? 0
  ).normalize();

  const axeY = new THREE.Vector3(
    placement.AxeYx ?? 0,
    placement.AxeYy ?? 1,
    placement.AxeYz ?? 0
  ).normalize();

  const axeZ = new THREE.Vector3().crossVectors(axeX, axeY).normalize();
  const axeYOrtho = new THREE.Vector3().crossVectors(axeZ, axeX).normalize();

  const matrix = new THREE.Matrix4();
  matrix.makeBasis(axeX, axeYOrtho, axeZ);
  matrix.setPosition(origin);

  return matrix;
}

// Center point in world space for any feature (used for camera focus).
export function computeFeatureCenter(feature, allFeatures) {
  // Direct sketch: use placement origin
  if (feature.Type === 'Sketch' && feature.Geometrie?.Placement) {
    const p = feature.Geometrie.Placement;
    return new THREE.Vector3(p.OrigineX ?? 0, p.OrigineY ?? 0, p.OrigineZ ?? 0);
  }

  // Other features: fall back to the referenced sketch origin
  const sketchNom = feature.SketchReferences?.[0];
  if (sketchNom) {
    const sketch = allFeatures?.find((f) => f.Nom === sketchNom && f.Type === 'Sketch');
    if (sketch?.Geometrie?.Placement) {
      const p = sketch.Geometrie.Placement;
      return new THREE.Vector3(p.OrigineX ?? 0, p.OrigineY ?? 0, p.OrigineZ ?? 0);
    }
  }

  return new THREE.Vector3(0, 0, 0);
}

// Bounding box that encompasses all features (used for initial camera fit).
export function computeFeaturesBounds(features) {
  const box = new THREE.Box3();

  features.forEach((f) => {
    if (f.Type === 'Sketch' && f.Geometrie) {
      const geo = f.Geometrie;
      const mat = calculerMatricePlacement(geo.Placement ?? {});
      const applyMat = (v) => v.applyMatrix4(mat);

      (geo.Cercles ?? []).forEach(({ CentreX, CentreY, Rayon }) => {
        for (let a = 0; a < Math.PI * 2; a += Math.PI / 8) {
          box.expandByPoint(
            applyMat(new THREE.Vector3(CentreX + Math.cos(a) * Rayon, CentreY + Math.sin(a) * Rayon, 0))
          );
        }
      });

      (geo.Lignes ?? []).forEach(({ X1, Y1, X2, Y2 }) => {
        box.expandByPoint(applyMat(new THREE.Vector3(X1, Y1, 0)));
        box.expandByPoint(applyMat(new THREE.Vector3(X2, Y2, 0)));
      });
    } else {
      // Non-sketch: approximate from referenced sketch + pad depth
      const sketchNom = f.SketchReferences?.[0];
      const sketch = features.find((s) => s.Nom === sketchNom && s.Type === 'Sketch');
      if (sketch?.Geometrie?.Placement) {
        const p = sketch.Geometrie.Placement;
        const origin = new THREE.Vector3(p.OrigineX ?? 0, p.OrigineY ?? 0, p.OrigineZ ?? 0);
        box.expandByPoint(origin);

        const longueur = f.Parametres?.longueur ?? 0;
        const longueurSeconde = f.Parametres?.longueur_seconde ?? 0;
        const depth = longueur + longueurSeconde;

        if (depth > 0) {
          const normal = new THREE.Vector3(
            p.AxeXy * 1 - p.AxeXz * 0,  // rough normal via placement
            p.AxeXz * 0 - p.AxeXx * 1,
            p.AxeXx * 0 - p.AxeXy * 0
          );
          // Fallback: just expand by depth in all directions
          box.expandByPoint(origin.clone().addScalar(depth));
          box.expandByPoint(origin.clone().addScalar(-longueurSeconde));
        }
      }
    }
  });

  if (box.isEmpty()) {
    box.set(new THREE.Vector3(-50, -50, -50), new THREE.Vector3(50, 50, 50));
  }

  return box;
}

const FILLET_TYPES = new Set(['EdgeFillet', 'Chamfer']);

// Spread EdgeFillet/Chamfer indicators in a row above the bounding box so
// each one gets a distinct, non-overlapping position.
// Position is approximate — we don't have edge geometry — but visually distinct.
export function computeFilletPosition(feature, allFeatures) {
  const fillets = allFeatures
    .filter((f) => FILLET_TYPES.has(f.Type))
    .sort((a, b) => a.Ordre - b.Ordre);

  const idx = fillets.findIndex((f) => f.Nom === feature.Nom);
  const n   = fillets.length;

  if (idx < 0 || n === 0) return new THREE.Vector3(0, 0, 0);

  // Bounding box from solid (non-fillet) features for a tighter estimate
  const solidOnly = allFeatures.filter((f) => !FILLET_TYPES.has(f.Type));
  const box = computeFeaturesBounds(solidOnly.length ? solidOnly : allFeatures);

  const center = new THREE.Vector3();
  const size   = new THREE.Vector3();
  box.getCenter(center);
  box.getSize(size);

  // Row of indicators above the part along X, at max-Z + margin
  const margin = Math.max(15, size.z * 0.5);
  const zAbove = box.max.z + margin;

  // Ensure minimum spacing of 12 units even on small parts
  const spanX  = Math.max(size.x * 0.8, 12 * (n - 1));
  const startX = center.x - spanX / 2;
  const stepX  = n > 1 ? spanX / (n - 1) : 0;

  return new THREE.Vector3(startX + idx * stepX, center.y, zAbove);
}

// Legacy alias kept for backward compatibility
export { computeFeaturesBounds as computeSketchBounds };
