export const FEATURE_COLORS = {
  Sketch:     { normal: '#00D9FF', highlight: '#FFFFFF' },
  Pad:        { normal: '#00C853', highlight: '#69FF47' },
  Pocket:     { normal: '#FF6D00', highlight: '#FFAB40' },
  Groove:     { normal: '#AA00FF', highlight: '#E040FB' },
  Shaft:      { normal: '#7C4DFF', highlight: '#B388FF' },
  Chamfer:    { normal: '#FFD600', highlight: '#FFFF8D' },
  EdgeFillet: { normal: '#FFD600', highlight: '#FFFF8D' },
  Mirror:     { normal: '#F50057', highlight: '#FF80AB' },
  Rib:        { normal: '#FF6D00', highlight: '#FFAB40' },
  Slot:       { normal: '#FF6D00', highlight: '#FFAB40' },
  default:    { normal: '#9E9E9E', highlight: '#E0E0E0' },
};

const FEATURE_BASE_OPACITY = {
  Sketch: 1,
  Pad: 0.48,
  Pocket: 0.38,
  Groove: 0.48,
  Shaft: 0.48,
  Chamfer: 0.85,
  EdgeFillet: 0.85,
  Mirror: 0.45,
  Rib: 0.38,
  Slot: 0.38,
  default: 0.4,
};

// Overview mode — satiné mat, pas chromé
const OVERVIEW_SOLID = {
  color: '#B8C4D0',
  transparent: false,
  opacity: 1,
  roughness: 0.58,
  metalness: 0.26,
  emissive: '#000000',
  emissiveIntensity: 0,
  depthWrite: true,
};

// Subtractive features (Pocket/Groove) get a transparent "void" look
const OVERVIEW_VOID = {
  color: '#5a7280',
  transparent: true,
  opacity: 0.38,
  roughness: 0.5,
  metalness: 0.35,
  emissive: '#000000',
  emissiveIntensity: 0,
  depthWrite: false,
};

const SUBTRACTIVE_TYPES = new Set(['Pocket', 'Groove', 'Slot', 'Rib']);

export function getOverviewStyle(type) {
  return SUBTRACTIVE_TYPES.has(type) ? OVERVIEW_VOID : OVERVIEW_SOLID;
}

export function getFeatureStyle(type, highlighted) {
  const c = FEATURE_COLORS[type] ?? FEATURE_COLORS.default;
  const baseOp = FEATURE_BASE_OPACITY[type] ?? FEATURE_BASE_OPACITY.default;
  return {
    color: highlighted ? c.highlight : c.normal,
    opacity: highlighted ? Math.min(baseOp + 0.42, 1) : baseOp,
    emissive: highlighted ? c.highlight : c.normal,
    emissiveIntensity: highlighted ? 0.28 : 0.04,
  };
}
