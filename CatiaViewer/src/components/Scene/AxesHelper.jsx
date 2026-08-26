import { useMemo } from 'react';
import * as THREE from 'three';
import { pointsToGeometry } from '../../utils/geometry';

function Axis({ dir, color, length = 80 }) {
  const geo = useMemo(() => {
    const pts = [new THREE.Vector3(0, 0, 0), dir.clone().multiplyScalar(length)];
    return pointsToGeometry(pts);
  }, [dir, length]);

  return (
    <line geometry={geo}>
      <lineBasicMaterial color={color} linewidth={1} />
    </line>
  );
}

export function StyledAxes({ length = 80 }) {
  return (
    <group>
      <Axis dir={new THREE.Vector3(1, 0, 0)} color="#FF3B5C" length={length} />
      <Axis dir={new THREE.Vector3(0, 1, 0)} color="#39FF14" length={length} />
      <Axis dir={new THREE.Vector3(0, 0, 1)} color="#00BFFF" length={length} />
    </group>
  );
}
