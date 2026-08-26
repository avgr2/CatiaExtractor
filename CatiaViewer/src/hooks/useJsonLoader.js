import { useState, useCallback } from 'react';

function normaliser(json) {
  // Format complet : { pieces: [...] }
  if (Array.isArray(json.pieces)) {
    return { pieces: json.pieces };
  }

  // Format pièce unique : { Nom, Features, ... }
  if (Array.isArray(json.Features)) {
    return { pieces: [json] };
  }

  // Format inconnu : liste les clés de premier niveau pour diagnostic
  const cles = Object.keys(json).join(', ');
  throw new Error(
    `Format JSON non reconnu. Clés trouvées à la racine : [${cles}]. ` +
    `Attendu : "pieces" (format lot) ou "Features" (format pièce unique).`
  );
}

export function useJsonLoader() {
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const loadFile = useCallback((file) => {
    if (!file) return;
    setLoading(true);
    setError(null);
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const json = JSON.parse(e.target.result);
        setData(normaliser(json));
      } catch (err) {
        setError(err.message);
        setData(null);
      } finally {
        setLoading(false);
      }
    };
    reader.onerror = () => {
      setError('Impossible de lire le fichier.');
      setLoading(false);
    };
    reader.readAsText(file);
  }, []);

  const reset = useCallback(() => {
    setData(null);
    setError(null);
  }, []);

  return { data, error, loading, loadFile, reset };
}
