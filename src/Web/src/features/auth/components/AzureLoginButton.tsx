import { useAzureLogin } from '../hooks/useAzureLogin';
import { Button } from '../../../components/ui/button';
import { Cloud, Loader2 } from 'lucide-react';

/**
 * Azure AD login button component.
 * Allows users to authenticate using their Microsoft/Azure AD credentials.
 * Part of Phase 12: Azure AD Integration.
 */
export const AzureLoginButton: React.FC<{
  variant?: 'default' | 'outline' | 'ghost' | 'secondary';
  showIcon?: boolean;
  fullWidth?: boolean;
}> = ({ variant = 'default', showIcon = true, fullWidth = false }) => {
  const { loginWithAzureAd, isLoading, error } = useAzureLogin();

  const handleClick = async () => {
    // Login updates Redux state which triggers app to show dashboard
    await loginWithAzureAd();
  };

  return (
    <div className="flex flex-col gap-2">
      <Button
        onClick={handleClick}
        disabled={isLoading}
        variant={variant}
        className={fullWidth ? 'w-full' : ''}
      >
        {isLoading ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Signing in...
          </>
        ) : (
          <>
            {showIcon && <Cloud className="mr-2 h-4 w-4" />}
            Login with Azure AD
          </>
        )}
      </Button>
      {error && (
        <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
      )}
    </div>
  );
};
