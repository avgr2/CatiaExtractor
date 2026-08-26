import { useJsonLoader } from './hooks/useJsonLoader';
import { LandingScreen } from './components/UI/LandingScreen';
import { Viewer } from './components/Viewer';

export default function App() {
  const { data, error, loadFile, reset } = useJsonLoader();

  if (data) {
    return <Viewer data={data} onReset={reset} />;
  }

  return <LandingScreen onFile={loadFile} error={error} />;
}
