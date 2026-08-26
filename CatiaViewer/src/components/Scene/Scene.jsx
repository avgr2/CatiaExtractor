import { Canvas } from '@react-three/fiber';
import { Grid, Environment } from '@react-three/drei';
import { Suspense, useMemo } from 'react';
import { EffectComposer, Bloom } from '@react-three/postprocessing';
import { PieceRenderer } from '../PieceRenderer';
import { StyledAxes } from './AxesHelper';
import { CameraController } from './CameraController';
import { computeFeaturesBounds, computeFeatureCenter } from '../../utils/placement';

function SceneContent({ features, selectedNom, displayMode, hiddenFeatures, onFeatureClick }) {
  const bounds = useMemo(() => computeFeaturesBounds(features), [features]);

  const focusTarget = useMemo(() => {
    if (!selectedNom) return null;
    const f = features.find((x) => x.Nom === selectedNom);
    return f ? computeFeatureCenter(f, features) : null;
  }, [selectedNom, features]);

  const isOverview = displayMode === 'overview';

  return (
    <>
      <CameraController targetBox={bounds} focusTarget={focusTarget} />

      {/* Lighting — stronger in overview for metallic shading, minimal in selection */}
      <ambientLight intensity={isOverview ? 0.55 : 0.08} />
      <directionalLight
        position={[150, 220, 120]}
        intensity={isOverview ? 1.6 : 0.5}
        color={isOverview ? '#ffffff' : '#4488ff'}
        castShadow={isOverview}
      />
      {isOverview && (
        <>
          <directionalLight position={[-80, 60, -100]} intensity={0.32} color="#d0e8ff" />
          <directionalLight position={[0, -120, 80]} intensity={0.14} color="#c8d8e8" />
        </>
      )}
      {!isOverview && (
        <pointLight position={[100, 200, 100]} intensity={0.6} color="#4488ff" />
      )}

      {/* Environment HDRI — warehouse : lumière douce industrielle, moins réfléchissante que studio */}
      {isOverview && <Environment preset="warehouse" backgroundIntensity={0} environmentIntensity={0.6} />}

      <Grid
        args={[1000, 1000]}
        cellSize={10}
        cellThickness={0.4}
        cellColor="#0A3A4A"
        sectionSize={50}
        sectionThickness={0.8}
        sectionColor="#005F73"
        fadeDistance={600}
        fadeStrength={1.5}
        infiniteGrid
      />

      <StyledAxes length={100} />

      <PieceRenderer
        features={features}
        selectedNom={selectedNom}
        displayMode={displayMode}
        hiddenFeatures={hiddenFeatures}
        onFeatureClick={onFeatureClick}
      />

      <EffectComposer>
        <Bloom
          intensity={isOverview ? 0.25 : 1.2}
          luminanceThreshold={isOverview ? 0.7 : 0.05}
          luminanceSmoothing={0.9}
          mipmapBlur
        />
      </EffectComposer>
    </>
  );
}

export function Scene({ features, selectedNom, displayMode, hiddenFeatures, onFeatureClick }) {
  return (
    <Canvas
      gl={{ antialias: true, toneMapping: 0 }}
      camera={{ fov: 45, near: 0.1, far: 50000 }}
      style={{ background: 'transparent' }}
    >
      <Suspense fallback={null}>
        <SceneContent
          features={features}
          selectedNom={selectedNom}
          displayMode={displayMode}
          hiddenFeatures={hiddenFeatures}
          onFeatureClick={onFeatureClick}
        />
      </Suspense>
    </Canvas>
  );
}
