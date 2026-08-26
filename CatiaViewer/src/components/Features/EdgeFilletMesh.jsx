import { useMemo } from 'react';
import { getFeatureStyle } from '../../utils/featureColors';
import { computeFilletPosition } from '../../utils/placement';
import { useDisplayMode } from '../../contexts/RenderMode';

export function EdgeFilletMesh({ feature, allFeatures, highlighted, onClick }) {
  const { displayMode } = useDisplayMode();

  const pos = useMemo(
    () => computeFilletPosition(feature, allFeatures),
    [feature, allFeatures]
  );

  // Diagnostic — confirms each indicator has a distinct position
  console.log(
    `[EdgeFilletMesh] "${feature.Nom}" — indicateur à (${pos.x.toFixed(1)}, ${pos.y.toFixed(1)}, ${pos.z.toFixed(1)})`
  );

  if (displayMode === 'overview') return null;

  const style = getFeatureStyle('EdgeFillet', highlighted);
  const rayon = feature.Parametres?.rayon ?? feature.Parametres?.radius ?? 1;

  return (
    <mesh position={pos} onClick={onClick}>
      <torusGeometry args={[Math.max(2, rayon), 0.6, 8, 24]} />
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
