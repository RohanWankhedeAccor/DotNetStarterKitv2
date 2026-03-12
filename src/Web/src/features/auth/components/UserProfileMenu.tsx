import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import { selectUser } from '../../../lib/redux/store';
import { useAzureLogin } from '../hooks/useAzureLogin';
import { LogOut, Settings } from 'lucide-react';

/**
 * User profile menu component with logout option.
 * Displays user avatar, name, email, and dropdown menu.
 * Part of Phase 12.4: Logout implementation.
 */
export const UserProfileMenu: React.FC<{ fallbackName?: string; fallbackEmail?: string }> = ({
  fallbackName = 'Admin User',
  fallbackEmail = 'admin@localhost'
}) => {
  const user = useSelector(selectUser);
  const { logoutFromAzureAd, isLoading } = useAzureLogin();
  const [isOpen, setIsOpen] = useState(false);

  // Use Redux user data if available, otherwise use fallback (for local login)
  const email = user.email || fallbackEmail;
  const fullName = user.fullName || fallbackName;

  if (!email) {
    return null; // Don't show if not logged in
  }

  const initials = (fullName || email)
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const handleLogout = async () => {
    await logoutFromAzureAd();
    // Reload page to reset app state
    window.location.href = '/';
    setIsOpen(false);
  };

  return (
    <div className="relative">
      {/* User Profile Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-3 w-full px-4 py-3 rounded-lg hover:bg-white/5 transition-colors"
        title="Click to open menu"
      >
        {/* Avatar */}
        <div className="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center text-sm font-bold text-white flex-shrink-0">
          {initials}
        </div>

        {/* User Info */}
        <div className="flex-1 text-left min-w-0">
          <p className="text-sm font-medium text-white truncate">
            {user.fullName || 'User'}
          </p>
          <p className="text-xs text-gray-400 truncate">{user.email}</p>
        </div>

        {/* Dropdown Arrow */}
        <svg
          className={`w-4 h-4 text-gray-400 transition-transform ${
            isOpen ? 'rotate-180' : ''
          }`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 14l-7 7m0 0l-7-7m7 7V3"
          />
        </svg>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute bottom-full left-0 right-0 mb-2 bg-[#1a1a1d] border border-white/10 rounded-lg shadow-lg overflow-hidden z-50">
          {/* Settings Option (Optional) */}
          <button
            onClick={() => {
              // TODO: Implement settings page
              setIsOpen(false);
            }}
            className="w-full flex items-center gap-3 px-4 py-3 text-sm text-gray-300 hover:bg-white/5 transition-colors border-b border-white/5"
          >
            <Settings className="w-4 h-4" />
            Settings (Coming Soon)
          </button>

          {/* Logout Button */}
          <button
            onClick={handleLogout}
            disabled={isLoading}
            className="w-full flex items-center gap-3 px-4 py-3 text-sm text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-50"
          >
            <LogOut className="w-4 h-4" />
            {isLoading ? 'Logging out...' : 'Logout'}
          </button>
        </div>
      )}

      {/* Close menu when clicking outside */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40"
          onClick={() => setIsOpen(false)}
        />
      )}
    </div>
  );
};
