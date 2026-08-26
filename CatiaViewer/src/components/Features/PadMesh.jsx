import { useMemo } from 'react';
import * as THREE from 'three';
import { calculerMatricePlacement } from '../../utils/placement';
import { buildExtrudeGeometry, isSketchEmpty } from '../../utils/featureGeometry';
import { useDisplayMode } from '../../contexts/RenderMode';
import { SolidMeshContent } from './SolidMeshContent';
import { EmptySketchIndicator } from './EmptySketchIndicator';

export function PadMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();
  const sketchNom = feature.SketchReferences?.[0];
  const sketch = allFeatures.find((f) => f.Nom === sketchNom && f.Type === 'Sketch');

  const empty = !sketch || isSketchEmpty(sketch);

  const geometry = useMemo(() => {
    if (empty) return null;
    try {
      const geo = buildExtrudeGeometry(sketch, feature.Parametres);
      if (!geo) console.warn(`[PadMesh] "${feature.Nom}" — extrusion échouée.`);
      return geo;
    } catch (e) {
      console.warn(`[PadMesh] "${feature.Nom}" — erreur:`, e.message);
      return null;
    }
  }, [empty, sketch, feature.Parametres, feature.Nom]);

  const matrix = useMemo(() => {
    const p = sketch?.Geometrie?.Placement;
    return p ? calculerMatricePlacement(p) : new THREE.Matrix4();
  }, [sketch]);

  if (empty) {
    if (displayMode === 'overview') return null;
    if (!sketch) {
      console.warn(`[PadMesh] "${feature.Nom}" — sketch "${sketchNom}" introuvable.`);
    } else {
      console.warn(`[PadMesh] "${feature.Nom}" — sketch "${sketchNom}" vide. Feature ignorée.`);
    }
    return (
      <EmptySketchIndicator
        feature={feature}
        sketch={sketch}
        highlighted={highlighted}
        onClick={onClick}
      />
    );
  }

  return (
    <SolidMeshContent
      geometry={geometry}
      matrix={matrix}
      displayMode={displayMode}
      highlighted={highlighted}
      featureType="Pad"
      castShadow
      onClick={onClick}
    />
  );
}
