import { useState, useMemo } from 'react';
import { isSketchEmpty } from '../../utils/featureGeometry';
import styles from './FeaturePanel.module.css';

// ── SVG feature icons ─────────────────────────────────────────────────────────

function IconSketch() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M2 10L5 7 8 4 11 1" stroke="#7ecfff" strokeWidth="1.5" strokeLinecap="round"/>
      <circle cx="11" cy="1" r="1.2" fill="#7ecfff"/>
      <line x1="1" y1="12" x2="12" y2="12" stroke="#7ecfff" strokeWidth="1.2" strokeLinecap="round"/>
    </svg>
  );
}

function IconPad() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <rect x="2" y="5" width="9" height="6" rx="1" fill="#4db8ff" opacity="0.9"/>
      <path d="M2 5 L6.5 2 L11 5" fill="#2e88cc" opacity="0.9"/>
      <path d="M11 5 L11 11 L6.5 11" stroke="#4db8ff" strokeWidth="0.8" fill="none"/>
    </svg>
  );
}

function IconPocket() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <rect x="2" y="5" width="9" height="6" rx="1" stroke="#ff8c42" strokeWidth="1.2" fill="none"/>
      <path d="M2 5 L6.5 2 L11 5" stroke="#ff8c42" strokeWidth="1.2" fill="none"/>
      <rect x="4.5" y="6.5" width="4" height="3" rx="0.5" fill="#ff8c4244"/>
    </svg>
  );
}

function IconGroove() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M6.5 6.5 A4 4 0 1 1 6.5 6.6" stroke="#c084fc" strokeWidth="1.3" fill="none" strokeLinecap="round"/>
      <path d="M9 4 L11 2 M9 4 L11 5.5" stroke="#c084fc" strokeWidth="1.1" strokeLinecap="round"/>
    </svg>
  );
}

function IconMirror() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <line x1="6.5" y1="1" x2="6.5" y2="12" stroke="#a3e635" strokeWidth="1.2" strokeDasharray="2 1.5"/>
      <path d="M1 4 L5.5 6.5 L1 9Z" fill="#a3e63588" stroke="#a3e635" strokeWidth="0.8"/>
      <path d="M12 4 L7.5 6.5 L12 9Z" fill="#a3e63544" stroke="#a3e63588" strokeWidth="0.8"/>
    </svg>
  );
}

function IconEdgeFillet() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M3 10 Q3 3 10 3" stroke="#fbbf24" strokeWidth="1.5" fill="none" strokeLinecap="round"/>
      <path d="M3 10 L3 12 M10 3 L12 3" stroke="#fbbf24" strokeWidth="1" strokeLinecap="round"/>
    </svg>
  );
}

function IconPlan() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M1 9 L6.5 6 L12 9 L6.5 12Z" stroke="#94a3b8" strokeWidth="1" fill="#94a3b822"/>
      <line x1="6.5" y1="1" x2="6.5" y2="6" stroke="#94a3b8" strokeWidth="1" strokeDasharray="2 1.5"/>
    </svg>
  );
}

function IconShaft() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <ellipse cx="6.5" cy="3.5" rx="4" ry="1.8" stroke="#e879f9" strokeWidth="1.1" fill="none"/>
      <line x1="2.5" y1="3.5" x2="2.5" y2="9.5" stroke="#e879f9" strokeWidth="1.1"/>
      <line x1="10.5" y1="3.5" x2="10.5" y2="9.5" stroke="#e879f9" strokeWidth="1.1"/>
      <ellipse cx="6.5" cy="9.5" rx="4" ry="1.8" stroke="#e879f9" strokeWidth="1.1" fill="none"/>
    </svg>
  );
}

function IconGeneric() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <circle cx="6.5" cy="6.5" r="4.5" stroke="#94a3b8" strokeWidth="1.2" fill="none"/>
    </svg>
  );
}

function IconEmpty() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M2 2L11 11M11 2L2 11" stroke="#ef4444" strokeWidth="1.5" strokeLinecap="round"/>
    </svg>
  );
}

// ── Eye icons ─────────────────────────────────────────────────────────────────

function EyeOpen() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <path d="M1 6.5C2.5 3.5 4.5 2 6.5 2C8.5 2 10.5 3.5 12 6.5C10.5 9.5 8.5 11 6.5 11C4.5 11 2.5 9.5 1 6.5Z"
        stroke="currentColor" strokeWidth="1.1" fill="none"/>
      <circle cx="6.5" cy="6.5" r="1.7" fill="currentColor"/>
    </svg>
  );
}

function EyeClosed() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <line x1="2" y1="11" x2="11" y2="2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
      <path d="M1 6.5C2.2 4.2 4 2.8 6 2.3" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" opacity="0.55"/>
      <path d="M12 6.5C10.8 8.8 9 10.2 7 10.7" stroke="currentColor" strokeWidth="1.1" strokeLinecap="round" opacity="0.55"/>
    </svg>
  );
}

// ── Feature icon map ──────────────────────────────────────────────────────────

const FEATURE_ICONS = {
  Sketch:     IconSketch,
  Pad:        IconPad,
  Pocket:     IconPocket,
  Groove:     IconGroove,
  Shaft:      IconShaft,
  Mirror:     IconMirror,
  EdgeFillet: IconEdgeFillet,
  Chamfer:    IconEdgeFillet,
};

function FeatureIcon({ type }) {
  const Icon = FEATURE_ICONS[type] ?? IconGeneric;
  return <Icon />;
}

// ── Tree building ─────────────────────────────────────────────────────────────

function buildTree(features) {
  const sketchConsumers = new Set();
  const sorted = [...features].sort((a, b) => a.Ordre - b.Ordre);

  for (const f of sorted) {
    for (const ref of f.SketchReferences ?? []) sketchConsumers.add(ref);
  }

  const sketchByNom = Object.fromEntries(
    sorted.filter((f) => f.Type === 'Sketch').map((f) => [f.Nom, f])
  );

  const nodes = [];
  for (const f of sorted) {
    if (f.Type === 'Sketch') {
      if (!sketchConsumers.has(f.Nom)) nodes.push({ feature: f, children: [] });
      continue;
    }
    const children = (f.SketchReferences ?? [])
      .map((nom) => sketchByNom[nom])
      .filter(Boolean)
      .map((s) => ({ feature: s, children: [] }));
    nodes.push({ feature: f, children });
  }

  return nodes;
}

// ── Tree node ─────────────────────────────────────────────────────────────────

function TreeNode({ node, depth, selectedNom, onSelect, allFeatures, hiddenFeatures, onToggleVisibility }) {
  const { feature, children } = node;
  const hasChildren = children.length > 0;
  const [open, setOpen] = useState(true);

  const isSelected = feature.Nom === selectedNom;
  const isHidden   = hiddenFeatures.has(feature.Nom);

  const sketchEmpty = useMemo(() => {
    if (feature.Type !== 'Sketch') return false;
    return isSketchEmpty(feature);
  }, [feature]);

  const childSketchEmpty = useMemo(() => {
    if (feature.Type === 'Sketch') return false;
    const ref = feature.SketchReferences?.[0];
    if (!ref) return false;
    const sketch = allFeatures.find((f) => f.Nom === ref && f.Type === 'Sketch');
    return sketch ? isSketchEmpty(sketch) : false;
  }, [feature, allFeatures]);

  const nodeClass = [
    styles.node,
    isSelected       ? styles.nodeSelected  : '',
    childSketchEmpty ? styles.nodeWarning   : '',
    isHidden         ? styles.nodeHidden    : '',
  ].filter(Boolean).join(' ');

  return (
    <div>
      <button
        className={nodeClass}
        style={{ paddingLeft: `${10 + depth * 16}px` }}
        onClick={() => onSelect(feature.Nom)}
      >
        {/* Expand/collapse toggle */}
        <span
          className={styles.toggle}
          onClick={(e) => { e.stopPropagation(); setOpen((v) => !v); }}
        >
          {hasChildren ? (open ? '▾' : '▸') : <span className={styles.leaf}>·</span>}
        </span>

        {/* Type icon */}
        <span className={styles.icon}>
          {childSketchEmpty || sketchEmpty ? <IconEmpty /> : <FeatureIcon type={feature.Type} />}
        </span>

        {/* Name */}
        <span className={styles.nodeName}>{feature.Nom}</span>

        {/* Order badge */}
        <span className={styles.nodeOrder}>#{feature.Ordre}</span>

        {/* Empty-sketch warning badge */}
        {(childSketchEmpty || sketchEmpty) && (
          <span className={styles.warnBadge} title="Sketch vide — feature ignorée">vide</span>
        )}

        {/* Eye visibility toggle — hover-only when visible, always shown when hidden */}
        <button
          className={`${styles.eyeBtn} ${isHidden ? styles.eyeAlways : ''}`}
          title={isHidden ? 'Réafficher dans la scène' : 'Masquer dans la scène'}
          onClick={(e) => { e.stopPropagation(); onToggleVisibility(feature.Nom); }}
        >
          {isHidden ? <EyeClosed /> : <EyeOpen />}
        </button>
      </button>

      {hasChildren && open && (
        <div>
          {children.map((child) => (
            <TreeNode
              key={child.feature.Nom}
              node={child}
              depth={depth + 1}
              selectedNom={selectedNom}
              onSelect={onSelect}
              allFeatures={allFeatures}
              hiddenFeatures={hiddenFeatures}
              onToggleVisibility={onToggleVisibility}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ── Main panel ────────────────────────────────────────────────────────────────

export function FeaturePanel({
  features,
  plans,
  selectedNom,
  onSelect,
  hiddenFeatures,
  onToggleVisibility,
}) {
  const [plansOpen, setPlansOpen] = useState(true);
  const [bodyOpen, setBodyOpen]   = useState(true);

  const treeNodes = useMemo(() => buildTree(features), [features]);

  if (!features.length) return null;

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <span className={styles.title}>Arbre de spécification</span>
      </div>

      <div className={styles.tree}>
        {/* PartBody root */}
        <button className={styles.rootNode} onClick={() => setBodyOpen((v) => !v)}>
          <span className={styles.toggle}>{bodyOpen ? '▾' : '▸'}</span>
          <svg width="13" height="13" viewBox="0 0 13 13" fill="none" style={{ flexShrink: 0 }}>
            <rect x="1" y="1" width="11" height="11" rx="2" fill="#1e40af" stroke="#60a5fa" strokeWidth="1"/>
            <text x="6.5" y="9" textAnchor="middle" fontSize="7" fill="#93c5fd" fontWeight="bold">P</text>
          </svg>
          <span className={styles.rootLabel}>PartBody</span>
          <span className={styles.nodeOrder}>{features.filter((f) => f.Type !== 'Sketch').length} feat.</span>
        </button>

        {bodyOpen && (
          <div className={styles.subtree}>
            {treeNodes.map((node) => (
              <TreeNode
                key={node.feature.Nom}
                node={node}
                depth={0}
                selectedNom={selectedNom}
                onSelect={onSelect}
                allFeatures={features}
                hiddenFeatures={hiddenFeatures}
                onToggleVisibility={onToggleVisibility}
              />
            ))}
          </div>
        )}

        {/* Plans section */}
        {plans && plans.length > 0 && (
          <>
            <div className={styles.divider} />
            <button className={styles.rootNode} onClick={() => setPlansOpen((v) => !v)}>
              <span className={styles.toggle}>{plansOpen ? '▾' : '▸'}</span>
              <IconPlan />
              <span className={styles.rootLabel}>Plans</span>
              <span className={styles.nodeOrder}>{plans.length}</span>
            </button>

            {plansOpen && (
              <div className={styles.subtree}>
                {plans.map((plan) => (
                  <div key={plan.Nom} className={styles.planNode}>
                    <span className={styles.leaf}>·</span>
                    <span className={styles.icon}><IconPlan /></span>
                    <span className={styles.nodeName}>{plan.Nom}</span>
                    {plan.Offset != null && (
                      <span className={styles.nodeOrder}>Δ{plan.Offset}</span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
