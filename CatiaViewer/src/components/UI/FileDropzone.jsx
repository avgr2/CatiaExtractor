import { useCallback, useState } from 'react';
import styles from './FileDropzone.module.css';

export function FileDropzone({ onFile }) {
  const [dragging, setDragging] = useState(false);

  const handleDrop = useCallback(
    (e) => {
      e.preventDefault();
      setDragging(false);
      const file = e.dataTransfer.files?.[0];
      if (file) onFile(file);
    },
    [onFile]
  );

  const handleChange = useCallback(
    (e) => {
      const file = e.target.files?.[0];
      if (file) onFile(file);
    },
    [onFile]
  );

  return (
    <div
      className={`${styles.zone} ${dragging ? styles.dragging : ''}`}
      onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onDrop={handleDrop}
    >
      <div className={styles.icon}>⬡</div>
      <p className={styles.label}>
        Déposer un fichier <span className={styles.accent}>.json</span> ici
      </p>
      <p className={styles.sub}>ou</p>
      <label className={styles.btn}>
        Parcourir
        <input
          type="file"
          accept=".json,application/json"
          onChange={handleChange}
          style={{ display: 'none' }}
        />
      </label>
    </div>
  );
}
