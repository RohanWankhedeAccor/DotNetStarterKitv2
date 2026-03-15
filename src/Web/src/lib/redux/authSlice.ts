import { createSlice, PayloadAction } from '@reduxjs/toolkit';

/**
 * Redux slice for authentication state management.
 * Stores user identity and session metadata — the JWT itself lives in an
 * HttpOnly cookie managed by the server, not in JavaScript memory.
 * Part of Phase 12: Azure AD Integration.
 */

export interface AuthUser {
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
  expiresIn: number;
  /** Unix ms timestamp of when the internal JWT expires. Used by useTokenRefresh for proactive cookie rotation. */
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
    firstName: null,
    lastName: null,
    roles: [],
    expiresIn: 0,
    tokenExpiresAt: 0,
    authSource: null,
  },
  isAuthenticated: false,
  // true on load: prevents a flash of the Login button before the silent SSO check completes.
  isLoading: true,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    /** Set user info after successful login (token is in HttpOnly cookie, not here). */
    setUser: (state, action: PayloadAction<AuthUser>) => {
      state.user = action.payload;
      state.isAuthenticated = !!action.payload.userId;
    },

    /** Clear user info on logout. */
    clearUser: (state) => {
      state.user = initialState.user;
      state.isAuthenticated = false;
    },

    /** Set loading state during auth operations. */
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },

    /** Update user roles after permission changes. */
    updateRoles: (state, action: PayloadAction<string[]>) => {
      state.user.roles = action.payload;
    },

    /** Update expiry metadata after the server rotates the cookie. */
    refreshToken: (state, action: PayloadAction<{ expiresIn: number; tokenExpiresAt: number }>) => {
      state.user.expiresIn = action.payload.expiresIn;
      state.user.tokenExpiresAt = action.payload.tokenExpiresAt;
    },
  },
});

export const { setUser, clearUser, setLoading, updateRoles, refreshToken } =
  authSlice.actions;

export default authSlice.reducer;
