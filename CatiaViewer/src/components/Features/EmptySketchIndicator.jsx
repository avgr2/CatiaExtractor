import * as THREE from 'three';
import { calculerMatricePlacement } from '../../utils/placement';
import { useMemo } from 'react';
import { useDisplayMode } from '../../contexts/RenderMode';

// Shown when a Pad/Pocket references a sketch with no geometry.
// Hidden in overview mode — only visible when a feature is explicitly selected/inspected.
export function EmptySketchIndicator({ feature, sketch, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();

  const matrix = useMemo(() => {
    const p = sketch?.Geometrie?.Placement;
    return p ? calculerMatricePlacement(p) : new THREE.Matrix4();
  }, [sketch]);

  if (displayMode === 'overview') return null;

  return (
    <group matrix={matrix} matrixAutoUpdate={false} onClick={onClick}>
      <mesh>
        <boxGeometry args={[8, 8, 8]} />
        <meshStandardMaterial
          color={highlighted ? '#ff9900' : '#ff4444'}
          transparent
          opacity={0.18}
          wireframe
          depthWrite={false}
        />
      </mesh>
    </group>
  );
}
