import React, { ReactNode } from 'react';
import { useSilentSso } from '../features/auth/hooks/useSilentSso';

/**
 * App initializer component that runs setup logic on app load.
 * Currently handles Silent SSO for Azure AD users.
 * Part of Phase 12: Azure AD Integration.
 *
 * Wrap your main app content with this component:
 * ```tsx
 * <AppInitializer>
 *   <App />
 * </AppInitializer>
 * ```
 */
export const AppInitializer: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  // Run silent SSO on app load
  useSilentSso();

  return <>{children}</>;
};
