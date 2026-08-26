import { useState, useCallback, useMemo } from 'react';

export function useFeatureSelection(piece) {
  const [selectedNom, setSelectedNom] = useState(null);

  const features = useMemo(() => {
    if (!piece?.Features) return [];
    return [...piece.Features].sort((a, b) => a.Ordre - b.Ordre);
  }, [piece]);

  const selectFeature = useCallback((nom) => {
    setSelectedNom((prev) => (prev === nom ? null : nom));
  }, []);

  const clearSelection = useCallback(() => setSelectedNom(null), []);

  return { features, selectedNom, selectFeature, clearSelection };
}
