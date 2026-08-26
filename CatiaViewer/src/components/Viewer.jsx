import { useState, useEffect, useRef, useCallback } from 'react';
import { Scene } from './Scene/Scene';
import { PieceSelector } from './UI/PieceSelector';
import { FeaturePanel } from './UI/FeaturePanel';
import { useFeatureSelection } from '../hooks/useFeatureSelection';
import styles from './Viewer.module.css';

export function Viewer({ data, onReset }) {
  const [pieceIndex, setPieceIndex] = useState(0);
  const piece = data.pieces[pieceIndex] ?? null;
  const { features, selectedNom, selectFeature } = useFeatureSelection(piece);

  // ── Display mode (overview / selection) ──────────────────────────────────
  const targetMode = selectedNom ? 'selection' : 'overview';
  const [activeMode, setActiveMode] = useState(targetMode);
  const [fading, setFading] = useState(false);
  const prevMode = useRef(targetMode);

  useEffect(() => {
    if (prevMode.current === targetMode) return;
    prevMode.current = targetMode;
    setFading(true);
    const t = setTimeout(() => {
      setActiveMode(targetMode);
      setFading(false);
    }, 180);
    return () => clearTimeout(t);
  }, [targetMode]);

  // ── Per-feature visibility ────────────────────────────────────────────────
  const [hiddenFeatures, setHiddenFeatures] = useState(() => new Set());

  // Reset visibility when the piece changes
  const prevPieceIndex = useRef(pieceIndex);
  useEffect(() => {
    if (prevPieceIndex.current !== pieceIndex) {
      prevPieceIndex.current = pieceIndex;
      setHiddenFeatures(new Set());
      selectFeature(null);
    }
  }, [pieceIndex, selectFeature]);

  const toggleVisibility = useCallback((nom) => {
    setHiddenFeatures((prev) => {
      const next = new Set(prev);
      if (next.has(nom)) next.delete(nom);
      else next.add(nom);
      return next;
    });
  }, []);

  const showAll = useCallback(() => setHiddenFeatures(new Set()), []);

  // ── Derived counts ────────────────────────────────────────────────────────
  const featureCount  = features.length;
  const sketchCount   = features.filter((f) => f.Type === 'Sketch').length;
  const filletCount   = features.filter((f) => f.Type === 'EdgeFillet').length;
  const chamferCount  = features.filter((f) => f.Type === 'Chamfer').length;
  const hiddenCount   = hiddenFeatures.size;

  const filletNotice = (() => {
    const parts = [];
    if (filletCount  > 0) parts.push(`${filletCount} congé${filletCount   > 1 ? 's' : ''}`);
    if (chamferCount > 0) parts.push(`${chamferCount} chanfrein${chamferCount > 1 ? 's' : ''}`);
    return parts.length ? parts.join(' · ') : null;
  })();

  return (
    <div className={styles.root}>
      <div
        className={styles.canvas}
        style={{ opacity: fading ? 0 : 1, transition: 'opacity 180ms ease' }}
      >
        <Scene
          features={features}
          selectedNom={selectedNom}
          displayMode={activeMode}
          hiddenFeatures={hiddenFeatures}
          onFeatureClick={selectFeature}
        />
      </div>

      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <span className={styles.hex}>⬡</span>
          <span className={styles.brandText}>
            CATIA<span className={styles.brandAccent}>VIEWER</span>
          </span>
        </div>

        {data.pieces.length > 1 && (
          <PieceSelector
            pieces={data.pieces}
            selectedIndex={pieceIndex}
            onSelect={(i) => setPieceIndex(i)}
            onReset={onReset}
          />
        )}
        {data.pieces.length === 1 && (
          <div className={styles.singlePiece}>
            <span className={styles.singleLabel}>{piece?.Nom || 'Pièce'}</span>
            <button className={styles.resetBtn} onClick={onReset}>
              Changer de fichier
            </button>
          </div>
        )}

        <FeaturePanel
          features={features}
          plans={piece?.Plans ?? []}
          selectedNom={selectedNom}
          onSelect={selectFeature}
          hiddenFeatures={hiddenFeatures}
          onToggleVisibility={toggleVisibility}
        />
      </aside>

      <div className={styles.topbar}>
        <span className={styles.topbarPiece}>{piece?.Nom || '—'}</span>
        <span className={styles.topbarSep}>·</span>
        <span className={styles.topbarCount}>
          {featureCount} feature{featureCount !== 1 ? 's' : ''}
        </span>
        <span className={styles.topbarSep}>·</span>
        <span className={styles.topbarCount}>{sketchCount} sketch{sketchCount !== 1 ? 's' : ''}</span>
        <span className={styles.topbarSep}>·</span>
        <span className={styles.modeBadge} data-mode={activeMode}>
          {activeMode === 'overview' ? 'Vue d\'ensemble' : 'Inspection'}
        </span>

        {hiddenCount > 0 && (
          <>
            <span className={styles.topbarSep}>·</span>
            <button className={styles.showAllBtn} onClick={showAll} title="Réafficher toutes les features">
              {hiddenCount} masqué{hiddenCount > 1 ? 's' : ''} — Tout réafficher
            </button>
          </>
        )}
      </div>

      {activeMode === 'overview' && filletNotice && (
        <div className={styles.filletNotice}>
          <span className={styles.filletIcon}>◎</span>
          {filletNotice} — non repr. géométriquement
        </div>
      )}

      <div className={styles.hint}>
        Clic gauche : rotation · Molette : zoom · Clic droit : déplacer · Clic sur feature : inspecter · Clic ailleurs : vue d'ensemble
      </div>
    </div>
  );
}
