import { useState, useCallback, useMemo } from 'react';

export function useSketchSelection(piece) {
  const [selectedNom, setSelectedNom] = useState(null);

  const sketches = useMemo(() => {
    if (!piece?.Features) return [];
    return piece.Features
      .filter((f) => f.Type === 'Sketch')
      .sort((a, b) => a.Ordre - b.Ordre);
  }, [piece]);

  const selectSketch = useCallback((nom) => {
    setSelectedNom((prev) => (prev === nom ? null : nom));
  }, []);

  const clearSelection = useCallback(() => setSelectedNom(null), []);

  return { sketches, selectedNom, selectSketch, clearSelection };
}
