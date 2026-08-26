import { FileDropzone } from './FileDropzone';
import styles from './LandingScreen.module.css';

export function LandingScreen({ onFile, error }) {
  return (
    <div className={styles.root}>
      <div className={styles.card}>
        <div className={styles.logo}>
          <span className={styles.logoHex}>⬡</span>
          <span className={styles.logoText}>CATIA<span className={styles.logoAccent}>VIEWER</span></span>
        </div>
        <p className={styles.tagline}>
          Visualiseur de features CATIA — sketches & géométrie 3D
        </p>
        <FileDropzone onFile={onFile} />
        {error && <p className={styles.error}>{error}</p>}
      </div>
    </div>
  );
}
