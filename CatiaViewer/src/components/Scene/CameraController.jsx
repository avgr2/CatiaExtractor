import { useEffect, useRef } from 'react';
import { useThree } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import * as THREE from 'three';

export function CameraController({ targetBox, focusTarget }) {
  const controlsRef = useRef();
  const { camera } = useThree();

  // Fit camera to bounding box on mount / when box changes
  useEffect(() => {
    if (!targetBox || targetBox.isEmpty()) return;
    const center = new THREE.Vector3();
    const size = new THREE.Vector3();
    targetBox.getCenter(center);
    targetBox.getSize(size);
    const maxDim = Math.max(size.x, size.y, size.z, 1);
    const fov = camera.fov * (Math.PI / 180);
    const dist = (maxDim / 2 / Math.tan(fov / 2)) * 2.2;

    camera.position.set(center.x + dist * 0.6, center.y + dist * 0.6, center.z + dist * 0.8);
    camera.lookAt(center);
    camera.near = dist * 0.001;
    camera.far = dist * 10;
    camera.updateProjectionMatrix();

    if (controlsRef.current) {
      controlsRef.current.target.copy(center);
      controlsRef.current.update();
    }
  }, [targetBox, camera]);

  // Focus on a specific sketch
  useEffect(() => {
    if (!focusTarget || !controlsRef.current) return;
    controlsRef.current.target.copy(focusTarget);
    controlsRef.current.update();
  }, [focusTarget]);

  return (
    <OrbitControls
      ref={controlsRef}
      enableDamping
      dampingFactor={0.07}
      minDistance={1}
      maxDistance={5000}
    />
  );
}
