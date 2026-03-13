import { configureStore } from '@reduxjs/toolkit';
import { persistReducer, persistStore, FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER } from 'redux-persist';
import storage from 'redux-persist/lib/storage';
import authReducer from './authSlice';

/**
 * redux-persist config for the auth slice.
 * Persists user identity (email, roles, expiry) to localStorage so the app
 * knows who is logged in across page refreshes.
 * The JWT itself is in an HttpOnly cookie — never stored here.
 */
const authPersistConfig = {
  key: 'auth',
  storage,
  // Exclude isLoading — it should always reset to false after hydration.
  blacklist: ['isLoading'],
};

const persistedAuthReducer = persistReducer(authPersistConfig, authReducer);

export const store = configureStore({
  reducer: {
    auth: persistedAuthReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // redux-persist dispatches these action types internally; they are safe to ignore.
        ignoredActions: [FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER],
      },
    }),
});

/** Used by PersistGate in main.tsx to gate rendering until rehydration completes. */
export const persistor = persistStore(store);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

// Helper selectors
export const selectAuth = (state: RootState) => state.auth;
export const selectUser = (state: RootState) => state.auth.user;
/** User is authenticated when we have a userId in Redux AND (presumably) a valid cookie. */
export const selectIsAuthenticated = (state: RootState) => !!state.auth.user.userId;
export const selectUserRoles = (state: RootState) => state.auth.user.roles || [];
