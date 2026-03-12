import { configureStore } from '@reduxjs/toolkit';
import authReducer from './authSlice';

/**
 * Redux store configuration.
 * Includes all application slices and middleware.
 * Part of Phase 12: Azure AD Integration.
 */
export const store = configureStore({
  reducer: {
    auth: authReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // Ignore MSAL-related actions that contain non-serializable objects
        ignoredActions: ['auth/setUser'],
        ignoredActionPaths: [],
        ignoredPaths: [],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

// Helper selectors
export const selectAuth = (state: RootState) => state.auth;
export const selectUser = (state: RootState) => state.auth.user;
export const selectIsAuthenticated = (state: RootState) =>
  !!state.auth.user.token;
export const selectUserToken = (state: RootState) => state.auth.user.token;
export const selectUserRoles = (state: RootState) => state.auth.user.roles || [];
