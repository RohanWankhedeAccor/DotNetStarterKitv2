import { createSlice, PayloadAction } from '@reduxjs/toolkit';

/**
 * Redux slice for authentication state management.
 * Stores user information, JWT token, and authentication metadata.
 * Part of Phase 12: Azure AD Integration.
 */

export interface AuthUser {
  userId: string | null;
  email: string | null;
  fullName: string | null;
  roles: string[];
  token: string | null;
  expiresIn: number;
  /** Unix ms timestamp of when the token expires. Used by useTokenRefresh for proactive refresh. */
  tokenExpiresAt: number;
  authSource: 'Local' | 'AzureAd' | null;
}

interface AuthState {
  user: AuthUser;
  isAuthenticated: boolean;
  isLoading: boolean;
}

const initialState: AuthState = {
  user: {
    userId: null,
    email: null,
    fullName: null,
    roles: [],
    token: null,
    expiresIn: 0,
    tokenExpiresAt: 0,
    authSource: null,
  },
  isAuthenticated: false,
  // true on load: the app assumes an SSO check is in-flight until useSilentSso resolves.
  // This prevents a flash of the "Login" button before the silent check completes.
  isLoading: true,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    /**
     * Set user info and token after successful login.
     */
    setUser: (state, action: PayloadAction<AuthUser>) => {
      state.user = action.payload;
      state.isAuthenticated = !!action.payload.token;
    },

    /**
     * Clear user info on logout.
     */
    clearUser: (state) => {
      state.user = initialState.user;
      state.isAuthenticated = false;
    },

    /**
     * Set loading state during auth operations.
     */
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },

    /**
     * Update user roles after permission changes.
     */
    updateRoles: (state, action: PayloadAction<string[]>) => {
      state.user.roles = action.payload;
    },

    /**
     * Refresh token (called before expiration).
     */
    refreshToken: (state, action: PayloadAction<{ token: string; expiresIn: number; tokenExpiresAt: number }>) => {
      state.user.token = action.payload.token;
      state.user.expiresIn = action.payload.expiresIn;
      state.user.tokenExpiresAt = action.payload.tokenExpiresAt;
    },
  },
});

export const { setUser, clearUser, setLoading, updateRoles, refreshToken } =
  authSlice.actions;

export default authSlice.reducer;
