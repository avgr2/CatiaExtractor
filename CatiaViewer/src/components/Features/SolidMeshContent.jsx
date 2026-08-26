import { useMemo } from 'react';
import * as THREE from 'three';
import { getFeatureStyle, getOverviewStyle } from '../../utils/featureColors';

// Shared renderer used by PadMesh, PocketMesh, GrooveMesh, ShaftMesh, MirrorMesh.
// Handles the overview ↔ selection material switch and edge highlighting.
export function SolidMeshContent({
  geometry,
  matrix,
  displayMode,
  highlighted,
  featureType,
  castShadow = false,
  depthWriteOverride,
  onClick,
}) {
  const edges = useMemo(() => {
    if (!geometry || displayMode !== 'overview') return null;
    try {
      return new THREE.EdgesGeometry(geometry, 12);
    } catch {
      return null;
    }
  }, [geometry, displayMode]);

  if (!geometry) return null;

  const style =
    displayMode === 'overview'
      ? getOverviewStyle(featureType)
      : getFeatureStyle(featureType, highlighted);

  const dw = depthWriteOverride !== undefined ? depthWriteOverride : style.depthWrite !== false;

  return (
    <group matrix={matrix} matrixAutoUpdate={false} onClick={onClick}>
      <mesh geometry={geometry} castShadow={castShadow} receiveShadow={castShadow}>
        <meshStandardMaterial
          color={style.color}
          transparent={style.transparent}
          opacity={style.opacity}
          roughness={style.roughness ?? 0.25}
          metalness={style.metalness ?? 0.55}
          emissive={style.emissive ?? '#000000'}
          emissiveIntensity={style.emissiveIntensity ?? 0}
          side={THREE.DoubleSide}
          depthWrite={dw}
        />
      </mesh>
      {edges && (
        <lineSegments geometry={edges}>
          <lineBasicMaterial
            color="#00D9FF"
            transparent
            opacity={0.2}
            depthWrite={false}
          />
        </lineSegments>
      )}
    </group>
  );
}
