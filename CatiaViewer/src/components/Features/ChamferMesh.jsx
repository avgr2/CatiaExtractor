import { useMemo } from 'react';
import { getFeatureStyle } from '../../utils/featureColors';
import { computeFilletPosition } from '../../utils/placement';
import { useDisplayMode } from '../../contexts/RenderMode';

export function ChamferMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();

  const pos = useMemo(
    () => computeFilletPosition(feature, allFeatures),
    [feature, allFeatures]
  );

  // Diagnostic — confirms each indicator has a distinct position
  console.log(
    `[ChamferMesh] "${feature.Nom}" — indicateur à (${pos.x.toFixed(1)}, ${pos.y.toFixed(1)}, ${pos.z.toFixed(1)})`
  );

  if (displayMode === 'overview') return null;

  const style = getFeatureStyle('Chamfer', highlighted);
  const longueur = feature.Parametres?.longueur ?? 1;
  const angle    = feature.Parametres?.angle ?? 45;

  console.warn(
    `[ChamferMesh] "${feature.Nom}" — géométrie d'arête non disponible. ` +
    `Indicateur visuel : L=${longueur}, angle=${angle}°.`
  );

  return (
    <mesh position={pos} onClick={onClick}>
      <octahedronGeometry args={[Math.max(2, longueur * 0.8), 0]} />
      <meshStandardMaterial
        color={style.color}
        transparent
        opacity={style.opacity}
        emissive={style.emissive}
        emissiveIntensity={highlighted ? 0.55 : 0.2}
        roughness={0.2}
        metalness={0.3}
      />
    </mesh>
  );
}
