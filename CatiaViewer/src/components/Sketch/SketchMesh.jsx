import { useMemo, useRef } from 'react';
import { calculerMatricePlacement } from '../../utils/placement';
import { FEATURE_COLORS } from '../../utils/featureColors';
import { useDisplayMode } from '../../contexts/RenderMode';
import { SketchCircle } from './SketchCircle';
import { SketchLine } from './SketchLine';

export function SketchMesh({ sketch, highlighted = false, onClick }) {
  const { displayMode } = useDisplayMode();
  if (displayMode === 'overview') return null;
  const groupRef = useRef();
  const geo = sketch.Geometrie;

  const matrix = useMemo(() => {
    if (!geo?.Placement) return null;
    return calculerMatricePlacement(geo.Placement);
  }, [geo]);

  if (!geo) return null;

  const colors = FEATURE_COLORS.Sketch;
  const lineColor = highlighted ? colors.highlight : colors.normal;

  return (
    <group ref={groupRef} matrix={matrix} matrixAutoUpdate={false} onClick={onClick}>
      {(geo.Cercles ?? []).map((c, i) => (
        <SketchCircle
          key={i}
          cx={c.CentreX}
          cy={c.CentreY}
          radius={c.Rayon}
          isConstruction={c.EstConstruction}
          highlighted={highlighted}
          overrideColor={lineColor}
        />
      ))}
      {(geo.Lignes ?? []).map((l, i) => (
        <SketchLine
          key={i}
          x1={l.X1}
          y1={l.Y1}
          x2={l.X2}
          y2={l.Y2}
          isConstruction={l.EstConstruction}
          highlighted={highlighted}
          overrideColor={lineColor}
        />
      ))}
    </group>
  );
}
