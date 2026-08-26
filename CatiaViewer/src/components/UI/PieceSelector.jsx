import styles from './PieceSelector.module.css';

export function PieceSelector({ pieces, selectedIndex, onSelect, onReset }) {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.title}>Pièces</span>
        <button className={styles.resetBtn} onClick={onReset} title="Charger un autre fichier">
          ✕
        </button>
      </div>
      <div className={styles.list}>
        {pieces.map((piece, i) => (
          <button
            key={i}
            className={`${styles.card} ${selectedIndex === i ? styles.active : ''}`}
            onClick={() => onSelect(i)}
          >
            <span className={styles.cardName}>{piece.Nom || `Pièce ${i + 1}`}</span>
            {piece.Fichier && (
              <span className={styles.cardFile}>{piece.Fichier}</span>
            )}
            <span className={styles.cardCount}>
              {(piece.Features ?? []).filter((f) => f.Type === 'Sketch').length} sketch(es)
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
