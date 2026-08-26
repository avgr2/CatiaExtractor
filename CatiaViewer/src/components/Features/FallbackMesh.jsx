import { useMemo } from 'react';
import * as THREE from 'three';
import { getFeatureStyle } from '../../utils/featureColors';
import { computeFeatureCenter } from '../../utils/placement';
import { useDisplayMode } from '../../contexts/RenderMode';

export function FallbackMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();

  const center = useMemo(
    () => computeFeatureCenter(feature, allFeatures),
    [feature, allFeatures]
  );

  if (displayMode === 'overview') return null;

  const style = getFeatureStyle(feature.Type, highlighted);

  return (
    <mesh position={center} onClick={onClick}>
      <boxGeometry args={[4, 4, 4]} />
      <meshStandardMaterial
        color={style.color}
        transparent
        opacity={style.opacity}
        emissive={style.emissive}
        emissiveIntensity={highlighted ? 0.5 : 0.15}
        wireframe={!highlighted}
      />
    </mesh>
  );
}
