import { useMemo } from 'react';
import { linePoints, pointsToGeometry } from '../../utils/geometry';

const COLOR_NORMAL = '#00D9FF';
const COLOR_CONSTRUCTION = '#6A5ACD';

export function SketchLine({ x1, y1, x2, y2, isConstruction = false, highlighted = false, overrideColor }) {
  const geometry = useMemo(
    () => pointsToGeometry(linePoints(x1, y1, x2, y2)),
    [x1, y1, x2, y2]
  );

  const color = overrideColor ?? (highlighted ? '#FFFFFF' : isConstruction ? COLOR_CONSTRUCTION : COLOR_NORMAL);
  const opacity = isConstruction ? 0.45 : 1;

  return (
    <line geometry={geometry}>
      <lineBasicMaterial
        color={color}
        transparent={isConstruction}
        opacity={opacity}
        linewidth={1}
      />
    </line>
  );
}
