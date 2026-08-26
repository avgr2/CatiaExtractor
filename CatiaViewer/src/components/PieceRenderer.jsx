import { Component } from 'react';
import { RenderModeContext } from '../contexts/RenderMode';
import { SketchMesh } from './Sketch/SketchMesh';
import { PadMesh } from './Features/PadMesh';
import { PocketMesh } from './Features/PocketMesh';
import { GrooveMesh } from './Features/GrooveMesh';
import { ShaftMesh } from './Features/ShaftMesh';
import { ChamferMesh } from './Features/ChamferMesh';
import { EdgeFilletMesh } from './Features/EdgeFilletMesh';
import { MirrorMesh } from './Features/MirrorMesh';
import { FallbackMesh } from './Features/FallbackMesh';

const FEATURE_COMPONENTS = {
  Sketch:     SketchMesh,
  Pad:        PadMesh,
  Pocket:     PocketMesh,
  Groove:     GrooveMesh,
  Shaft:      ShaftMesh,
  Chamfer:    ChamferMesh,
  EdgeFillet: EdgeFilletMesh,
  Mirror:     MirrorMesh,
};

class FeatureErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error) {
    return { error };
  }

  componentDidCatch(error, info) {
    console.warn(
      `[PieceRenderer] Feature "${this.props.nom}" (${this.props.type}) a planté — isolé:`,
      error.message,
      info.componentStack?.split('\n')[1]?.trim() ?? ''
    );
  }

  render() {
    if (this.state.error) return null;
    return this.props.children;
  }
}

export function PieceRenderer({ features, selectedNom, displayMode, hiddenFeatures, onFeatureClick }) {
  return (
    <RenderModeContext.Provider value={{ displayMode }}>
      <group>
        {features.map((feature) => {
          // Feature hidden by user — skip entirely (not just transparent)
          if (hiddenFeatures?.has(feature.Nom)) return null;

          const Component = FEATURE_COMPONENTS[feature.Type] ?? FallbackMesh;
          const highlighted = feature.Nom === selectedNom;

          const props =
            feature.Type === 'Sketch'
              ? {
                  key: feature.Nom,
                  sketch: feature,
                  highlighted,
                  onClick: (e) => { e.stopPropagation(); onFeatureClick(feature.Nom); },
                }
              : {
                  key: feature.Nom,
                  feature,
                  allFeatures: features,
                  highlighted,
                  onClick: (e) => { e.stopPropagation(); onFeatureClick(feature.Nom); },
                };

          return (
            <FeatureErrorBoundary key={feature.Nom} nom={feature.Nom} type={feature.Type}>
              <Component {...props} />
            </FeatureErrorBoundary>
          );
        })}
      </group>
    </RenderModeContext.Provider>
  );
}
