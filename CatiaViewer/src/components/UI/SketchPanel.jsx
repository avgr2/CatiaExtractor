import styles from './SketchPanel.module.css';

export function SketchPanel({ sketches, selectedNom, onSelect }) {
  if (!sketches.length) return null;

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <span className={styles.title}>Sketches</span>
        <span className={styles.count}>{sketches.length}</span>
      </div>
      <div className={styles.list}>
        {sketches.map((sk) => {
          const geo = sk.Geometrie;
          const nCircles = (geo?.Cercles ?? []).filter((c) => !c.EstConstruction).length;
          const nLines = (geo?.Lignes ?? []).filter((l) => !l.EstConstruction).length;
          const isSelected = sk.Nom === selectedNom;

          return (
            <button
              key={sk.Nom}
              className={`${styles.item} ${isSelected ? styles.selected : ''}`}
              onClick={() => onSelect(sk.Nom)}
            >
              <div className={styles.itemLeft}>
                <span className={styles.dot} />
                <div>
                  <span className={styles.name}>{sk.Nom}</span>
                  <span className={styles.meta}>
                    ordre {sk.Ordre}
                    {nCircles > 0 && ` · ${nCircles}⊙`}
                    {nLines > 0 && ` · ${nLines}—`}
                  </span>
                </div>
              </div>
              {isSelected && <span className={styles.badge}>actif</span>}
            </button>
          );
        })}
      </div>
    </div>
  );
}
