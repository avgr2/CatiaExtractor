import * as THREE from 'three';

export function circlePoints(cx, cy, radius, segments = 96) {
  const pts = [];
  for (let i = 0; i <= segments; i++) {
    const a = (i / segments) * Math.PI * 2;
    pts.push(new THREE.Vector3(cx + Math.cos(a) * radius, cy + Math.sin(a) * radius, 0));
  }
  return pts;
}

export function linePoints(x1, y1, x2, y2) {
  return [new THREE.Vector3(x1, y1, 0), new THREE.Vector3(x2, y2, 0)];
}

export function pointsToGeometry(points) {
  const geo = new THREE.BufferGeometry().setFromPoints(points);
  return geo;
}
