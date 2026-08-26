import { createContext, useContext } from 'react';

export const RenderModeContext = createContext({ displayMode: 'overview' });

export function useDisplayMode() {
  return useContext(RenderModeContext);
}
