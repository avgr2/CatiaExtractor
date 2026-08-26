import { useMemo } from 'react';
import * as THREE from 'three';
import { calculerMatricePlacement } from '../../utils/placement';
import { buildLatheProfile } from '../../utils/featureGeometry';
import { useDisplayMode } from '../../contexts/RenderMode';
import { SolidMeshContent } from './SolidMeshContent';
import { FallbackMesh } from './FallbackMesh';

export function GrooveMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();
  const sketchNom = feature.SketchReferences?.[0];
  const sketch = allFeatures.find((f) => f.Nom === sketchNom && f.Type === 'Sketch');

  const geometry = useMemo(() => {
    if (!sketch) {
      console.warn(`[GrooveMesh] "${feature.Nom}" — sketch "${sketchNom}" introuvable.`);
      return null;
    }
    const points = buildLatheProfile(sketch);
    if (!points) {
      console.warn(`[GrooveMesh] "${feature.Nom}" — profil de révolution indéterminable.`);
      return null;
    }
    const angleRad = ((feature.Parametres?.angle ?? 360) * Math.PI) / 180;
    try {
      const geo = new THREE.LatheGeometry(points, 64, 0, angleRad);
      geo.computeVertexNormals();
      return geo;
    } catch (e) {
      console.warn(`[GrooveMesh] "${feature.Nom}" — LatheGeometry échoué:`, e.message);
      return null;
    }
  }, [sketch, feature.Parametres, feature.Nom, sketchNom]);

  const matrix = useMemo(() => {
    const p = sketch?.Geometrie?.Placement;
    return p ? calculerMatricePlacement(p) : new THREE.Matrix4();
  }, [sketch]);

  if (!geometry) {
    if (displayMode === 'overview') return null;
    return <FallbackMesh feature={feature} allFeatures={allFeatures} highlighted={highlighted} onClick={onClick} />;
  }

  return (
    <SolidMeshContent
      geometry={geometry}
      matrix={matrix}
      displayMode={displayMode}
      highlighted={highlighted}
      featureType="Groove"
      depthWriteOverride={false}
      onClick={onClick}
    />
  );
}
