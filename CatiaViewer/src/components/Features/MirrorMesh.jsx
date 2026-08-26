import { useMemo } from 'react';
import * as THREE from 'three';
import { calculerMatricePlacement } from '../../utils/placement';
import { buildExtrudeGeometry } from '../../utils/featureGeometry';
import { useDisplayMode } from '../../contexts/RenderMode';
import { SolidMeshContent } from './SolidMeshContent';
import { FallbackMesh } from './FallbackMesh';

function makeMirrorMatrix(planSym) {
  switch (planSym) {
    case '#XY': return new THREE.Matrix4().makeScale(1, 1, -1);
    case '#YZ': return new THREE.Matrix4().makeScale(-1, 1, 1);
    case '#ZX': return new THREE.Matrix4().makeScale(1, -1, 1);
    default:    return new THREE.Matrix4();
  }
}

export function MirrorMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();
  const sourceNom = feature.Refs?.element_source_probable;
  const planSym = feature.Refs?.plan_symetrie;
  const source = allFeatures.find((f) => f.Nom === sourceNom);

  const sketchNom = source?.SketchReferences?.[0];
  const sketch = sketchNom ? allFeatures.find((f) => f.Nom === sketchNom && f.Type === 'Sketch') : null;

  const geometry = useMemo(() => {
    if (!source || !sketch) return null;
    if (source.Type === 'Pad' || source.Type === 'Pocket') {
      try {
        return buildExtrudeGeometry(sketch, source.Parametres);
      } catch (e) {
        console.warn(`[MirrorMesh] "${feature.Nom}" — erreur:`, e.message);
        return null;
      }
    }
    return null;
  }, [source, sketch, feature.Nom]);

  const matrix = useMemo(() => {
    const mirrorMat = makeMirrorMatrix(planSym);
    if (!sketch?.Geometrie?.Placement) return mirrorMat;
    const sketchMat = calculerMatricePlacement(sketch.Geometrie.Placement);
    return mirrorMat.multiply(sketchMat);
  }, [sketch, planSym]);

  if (!geometry) {
    if (displayMode === 'overview') return null;
    console.warn(
      `[MirrorMesh] "${feature.Nom}" — source "${sourceNom}" non résolue. Indicateur affiché.`
    );
    return <FallbackMesh feature={feature} allFeatures={allFeatures} highlighted={highlighted} onClick={onClick} />;
  }

  return (
    <SolidMeshContent
      geometry={geometry}
      matrix={matrix}
      displayMode={displayMode}
      highlighted={highlighted}
      featureType="Mirror"
      castShadow
      onClick={onClick}
    />
  );
}
