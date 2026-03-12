# Frontend Standards & React Patterns

Guidelines for React component design, state management, hooks, forms, and styling.

---

## Component Architecture

### Feature-Sliced Design

All code related to a feature lives in one folder:
```
features/
├── users/
│   ├── index.ts                  # Public exports
│   ├── types/
│   │   └── index.ts              # User, UserDto, interfaces
│   ├── hooks/
│   │   ├── useUsers.ts           # Query hooks
│   │   ├── useUserById.ts
│   │   └── useCreateUser.ts      # Mutation hooks
│   ├── components/
│   │   ├── UsersList.tsx         # Presentational components
│   │   ├── CreateUserForm.tsx
│   │   └── UserCard.tsx
│   ├── pages/
│   │   ├── UsersPage.tsx         # Route-level components
│   │   └── UserDetailPage.tsx
│   └── __tests__/
│       ├── UsersPage.test.tsx
│       └── useUsers.test.ts
│
└── products/
    ├── index.ts
    ├── types/
    ├── hooks/
    ├── components/
    ├── pages/
    └── __tests__/
```

### Benefits
- **Cohesion**: All user-related code in one place
- **Discoverability**: Easy to find what you need
- **Maintainability**: Delete feature = delete one folder
- **Scaling**: Add features without affecting others

---

## State Management Strategy

### State Placement Rules

```
┌─────────────────────────────────────────────────┐
│              Global Redux State                  │
│  (auth, theme, global notifications)            │
└─────────────────────────────────────────────────┘
              ↓
         Component Tree
              ↓
┌─────────────────────────────────────────────────┐
│  Server State (TanStack Query)                  │
│  (users, products, API data)                    │
└─────────────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────────────┐
│  Local State (useState)                         │
│  (form inputs, UI visibility, animations)      │
└─────────────────────────────────────────────────┘
```

### Redux (Global Client State)

**ONLY for:**
- Authentication state (current user, token)
- Theme preference (dark/light mode)
- Global UI state (sidebar collapse, modal visibility)

**NOT for:**
- API data (products, users, projects) ← Use TanStack Query
- Form state ← Use React Hook Form
- Local UI state ← Use useState

**Example:** `src/lib/redux/slices/authSlice.ts`
```typescript
import { createSlice, PayloadAction } from '@reduxjs/toolkit'

export interface User {
  id: number
  email: string
  fullName: string
  roles: string[]
}

interface AuthState {
  user: User | null
  isLoading: boolean
}

const initialState: AuthState = {
  user: null,
  isLoading: false
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setAuth(state, action: PayloadAction<User>) {
      state.user = action.payload
    },
    logout(state) {
      state.user = null
    }
  }
})

export const { setAuth, logout } = authSlice.actions
export default authSlice.reducer
```

**Usage in Component:**
```typescript
import { useAppDispatch, useAppSelector } from '@/lib/redux/store'
import { logout } from '@/lib/redux/slices/authSlice'

export function UserMenu() {
  const dispatch = useAppDispatch()
  const user = useAppSelector(state => state.auth.user)

  return (
    <div>
      <span>{user?.fullName}</span>
      <button onClick={() => dispatch(logout())}>Logout</button>
    </div>
  )
}
```

### TanStack Query (Server State)

**For all API data:**
- Automatic caching
- Background refetch
- Stale-while-revalidate
- Automatic retry on error

**Example:** `src/features/users/hooks/useUsers.ts`
```typescript
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/lib/api-client'
import { PagedResponse } from '@/lib/types'
import type { UserDto } from '../types'

export interface UseUsersParams {
  pageNumber?: number
  pageSize?: number
}

export function useUsers(params: UseUsersParams = {}) {
  const { pageNumber = 1, pageSize = 10 } = params

  return useQuery({
    queryKey: ['users', { pageNumber, pageSize }],
    queryFn: async () => {
      const response = await apiClient.get<PagedResponse<UserDto>>(
        '/api/v1/users',
        { params: { pageNumber, pageSize } }
      )
      return response.data
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000    // 10 minutes (formerly cacheTime)
  })
}

export function useUserById(id: number | null) {
  return useQuery({
    queryKey: ['users', id],
    queryFn: async () => {
      const response = await apiClient.get<UserDto>(`/api/v1/users/${id}`)
      return response.data
    },
    enabled: id !== null,  // Don't fetch if id is null
    staleTime: 5 * 60 * 1000
  })
}
```

**Mutation Example:** `src/features/users/hooks/useCreateUser.ts`
```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/lib/api-client'
import { toast } from 'sonner'
import type { CreateUserDto, UserDto } from '../types'

export function useCreateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateUserDto) => {
      const response = await apiClient.post<UserDto>('/api/v1/users', data)
      return response.data
    },
    onSuccess: () => {
      // Invalidate the users list so it refetches
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('User created successfully')
    },
    onError: (error) => {
      toast.error('Failed to create user')
    }
  })
}
```

### Local State (useState)

**For UI-only data:**
- Form inputs
- Modal open/close
- Dropdown visibility
- Animation states

```typescript
export function UserForm() {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const { createUser } = useCreateUser()

  const handleSubmit = async (data: CreateUserDto) => {
    setIsSubmitting(true)
    try {
      await createUser(data)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <input
        type={showPassword ? 'text' : 'password'}
        placeholder="Password"
      />
      <button
        type="button"
        onClick={() => setShowPassword(!showPassword)}
      >
        {showPassword ? 'Hide' : 'Show'}
      </button>
      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Creating...' : 'Create'}
      </button>
    </form>
  )
}
```

---

## Component Patterns

### Functional Components Only
Always use functional components with hooks. No class components.

```typescript
// ✅ GOOD
export function UsersList({ users }: UsersListProps) {
  return <ul>{users.map(u => <UserCard key={u.id} user={u} />)}</ul>
}

// ❌ BAD
export class UsersList extends React.Component {
  render() {
    return <ul>...</ul>
  }
}
```

### Component Naming
- **PascalCase**: `UsersList`, `CreateUserForm`, `ErrorBoundary`
- **Files match component name**: `UsersList.tsx` contains `UsersList`
- **Folder name**: lowercase of feature: `users/`, `products/`

### Props Typing
Always use TypeScript interfaces or types for props.

```typescript
interface UsersListProps {
  users: UserDto[]
  isLoading?: boolean
  onUserClick?: (user: UserDto) => void
}

export function UsersList({
  users,
  isLoading = false,
  onUserClick
}: UsersListProps) {
  if (isLoading) return <Skeleton />
  return (
    <ul>
      {users.map(u => (
        <li key={u.id} onClick={() => onUserClick?.(u)}>
          {u.email}
        </li>
      ))}
    </ul>
  )
}
```

### Error Boundaries

Wrap each page with ErrorBoundary to catch rendering errors.

**Component:** `src/components/ErrorBoundary.tsx`
```typescript
import { ReactNode } from 'react'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
  error?: Error
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error }
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="p-6 text-center">
          <h1 className="text-2xl font-bold">Something went wrong</h1>
          <p className="text-gray-600">{this.state.error?.message}</p>
          <button onClick={() => window.location.reload()}>
            Reload Page
          </button>
        </div>
      )
    }

    return this.props.children
  }
}
```

**Usage in App.tsx:**
```typescript
<ErrorBoundary>
  <UsersPage />
</ErrorBoundary>
```

### Loading States

Always show something while data is loading.

```typescript
import { Skeleton } from '@/components/Skeleton'

export function UsersList() {
  const { data, isLoading, error } = useUsers()

  if (isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-4 bg-red-100 text-red-700 rounded">
        Failed to load users: {error.message}
      </div>
    )
  }

  if (!data?.items?.length) {
    return (
      <div className="p-4 text-center text-gray-500">
        No users found
      </div>
    )
  }

  return (
    <ul>
      {data.items.map(user => (
        <UserCard key={user.id} user={user} />
      ))}
    </ul>
  )
}
```

---

## Forms & Validation

### React Hook Form + Zod

**Define schema:**
```typescript
// features/users/types/index.ts
import { z } from 'zod'

export const CreateUserSchema = z.object({
  email: z
    .string()
    .email('Invalid email format')
    .min(1, 'Email is required'),
  fullName: z
    .string()
    .min(2, 'Name must be at least 2 characters')
    .max(100, 'Name must be under 100 characters'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Must contain uppercase letter')
    .regex(/[0-9]/, 'Must contain number')
    .regex(/[!@#$%^&*]/, 'Must contain special character')
})

export type CreateUserDto = z.infer<typeof CreateUserSchema>
```

**Form component:**
```typescript
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useCreateUser } from '../hooks/useCreateUser'
import { CreateUserSchema, CreateUserDto } from '../types'

export function CreateUserForm() {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateUserDto>({
    resolver: zodResolver(CreateUserSchema)
  })

  const { mutate, isPending } = useCreateUser()

  const onSubmit = (data: CreateUserDto) => {
    mutate(data)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label>Email</label>
        <input
          {...register('email')}
          type="email"
          className="w-full px-3 py-2 border rounded"
        />
        {errors.email && (
          <p className="text-red-500 text-sm">{errors.email.message}</p>
        )}
      </div>

      <div>
        <label>Full Name</label>
        <input
          {...register('fullName')}
          className="w-full px-3 py-2 border rounded"
        />
        {errors.fullName && (
          <p className="text-red-500 text-sm">{errors.fullName.message}</p>
        )}
      </div>

      <div>
        <label>Password</label>
        <input
          {...register('password')}
          type="password"
          className="w-full px-3 py-2 border rounded"
        />
        {errors.password && (
          <p className="text-red-500 text-sm">{errors.password.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isPending}
        className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {isPending ? 'Creating...' : 'Create User'}
      </button>
    </form>
  )
}
```

---

## Styling with Tailwind CSS

### File Organization
```
src/styles/
├── globals.css          # Tailwind imports + global styles
└── [feature].css        # Feature-specific styles (rare)
```

### globals.css
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

/* CSS Variables for theming */
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 0 0% 3.6%;
    --primary: 0 0% 9%;
    --primary-foreground: 0 0% 98%;
    --secondary: 0 0% 96.1%;
    --secondary-foreground: 0 0% 9%;
    --muted: 0 0% 89.5%;
    --muted-foreground: 0 0% 45.1%;
  }

  .dark {
    --background: 0 0% 3.6%;
    --foreground: 0 0% 98%;
    --primary: 0 0% 98%;
    --primary-foreground: 0 0% 9%;
    --secondary: 0 0% 14.9%;
    --secondary-foreground: 0 0% 98%;
    --muted: 0 0% 14.9%;
    --muted-foreground: 0 0% 63.9%;
  }

  * {
    @apply border-border;
  }

  body {
    @apply bg-background text-foreground;
  }
}

/* Utility classes */
@layer components {
  .btn-primary {
    @apply px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 font-medium;
  }

  .btn-secondary {
    @apply px-4 py-2 bg-secondary text-secondary-foreground rounded-md hover:bg-secondary/80 font-medium;
  }

  .card {
    @apply rounded-lg border border-border bg-card p-6 shadow-sm;
  }

  .input {
    @apply flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-50;
  }
}
```

### Tailwind Usage
- **Utility-first**: Use classes directly in JSX
- **No custom CSS files** unless unavoidable
- **Responsive**: `sm:`, `md:`, `lg:`, `xl:` prefixes
- **Dark mode**: `dark:` prefix (configured in `tailwind.config.ts`)

```typescript
// ✅ GOOD: Tailwind utilities
<div className="p-4 bg-white dark:bg-slate-950 rounded-lg shadow-md">
  <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
    {title}
  </h1>
  <p className="mt-2 text-gray-600 dark:text-gray-400">
    {description}
  </p>
</div>

// ❌ BAD: Custom CSS
<div className="custom-card">
  <h1 className="custom-title">{title}</h1>
</div>
```

---

## Async Data Patterns

### Pagination Example
```typescript
export function UsersPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { data, isLoading, error } = useUsers({ pageNumber, pageSize: 10 })

  return (
    <div>
      <UsersList users={data?.items || []} isLoading={isLoading} />

      {data && (
        <div className="mt-4 flex gap-2 justify-center">
          <button
            onClick={() => setPageNumber(p => Math.max(1, p - 1))}
            disabled={!data.hasPreviousPage}
          >
            Previous
          </button>
          <span>{pageNumber} of {data.totalPages}</span>
          <button
            onClick={() => setPageNumber(p => p + 1)}
            disabled={!data.hasNextPage}
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
```

### Optimistic Updates
```typescript
export function useDeleteUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (userId: number) => {
      await apiClient.delete(`/api/v1/users/${userId}`)
    },
    onMutate: async (userId: number) => {
      // Cancel ongoing queries
      await queryClient.cancelQueries({ queryKey: ['users'] })

      // Snapshot old data
      const previousUsers = queryClient.getQueryData(['users'])

      // Optimistically update UI
      queryClient.setQueryData(['users'], (old: PagedResponse<UserDto>) => ({
        ...old,
        items: old.items.filter(u => u.id !== userId)
      }))

      return { previousUsers }
    },
    onError: (error, userId, context) => {
      // Revert on error
      if (context?.previousUsers) {
        queryClient.setQueryData(['users'], context.previousUsers)
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('User deleted')
    }
  })
}
```

---

## Custom Hooks Patterns

### Reusable Data Hook
```typescript
// Encapsulates query logic
export function useUsers(params: UseUsersParams = {}) {
  return useQuery({
    queryKey: ['users', params],
    queryFn: () => apiClient.get('/api/v1/users', { params })
  })
}
```

### Reusable Mutation Hook
```typescript
// Encapsulates mutation logic + cache invalidation
export function useUpdateUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateUserDto) =>
      apiClient.put(`/api/v1/users/${data.id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('User updated')
    }
  })
}
```

### Local State Hook
```typescript
// Encapsulates component state logic
export function useFormModal(initialOpen = false) {
  const [isOpen, setIsOpen] = useState(initialOpen)
  return {
    isOpen,
    open: () => setIsOpen(true),
    close: () => setIsOpen(false),
    toggle: () => setIsOpen(prev => !prev)
  }
}

// Usage
const modal = useFormModal()
return (
  <>
    <button onClick={modal.open}>Create</button>
    {modal.isOpen && <CreateForm onClose={modal.close} />}
  </>
)
```

---

## Testing Components

### Test File Organization
Place tests next to components:
```
features/users/
├── components/
│   ├── UsersList.tsx
│   └── UsersList.test.tsx
├── pages/
│   ├── UsersPage.tsx
│   └── UsersPage.test.tsx
└── hooks/
    ├── useUsers.ts
    └── useUsers.test.ts
```

### Component Test Example
```typescript
// UsersList.test.tsx
import { render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/query-client'
import { UsersList } from './UsersList'

describe('UsersList', () => {
  it('renders user list', () => {
    const users = [
      { id: 1, email: 'user1@example.com', fullName: 'User 1' },
      { id: 2, email: 'user2@example.com', fullName: 'User 2' }
    ]

    render(
      <QueryClientProvider client={queryClient}>
        <UsersList users={users} />
      </QueryClientProvider>
    )

    expect(screen.getByText('user1@example.com')).toBeInTheDocument()
    expect(screen.getByText('user2@example.com')).toBeInTheDocument()
  })

  it('shows skeleton when loading', () => {
    render(
      <QueryClientProvider client={queryClient}>
        <UsersList users={[]} isLoading={true} />
      </QueryClientProvider>
    )

    expect(screen.getByTestId('skeleton')).toBeInTheDocument()
  })
})
```

### Hook Test Example
```typescript
// useUsers.test.ts
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/query-client'
import { useUsers } from './useUsers'
import { server } from '@/test/msw'
import { HttpResponse, http } from 'msw'

const wrapper = ({ children }) => (
  <QueryClientProvider client={queryClient}>
    {children}
  </QueryClientProvider>
)

describe('useUsers', () => {
  it('fetches users successfully', async () => {
    const { result } = renderHook(() => useUsers(), { wrapper })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.items).toHaveLength(2)
  })

  it('handles error', async () => {
    server.use(
      http.get('/api/v1/users', () => HttpResponse.error())
    )

    const { result } = renderHook(() => useUsers(), { wrapper })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
```

---

## Best Practices Checklist

- ✅ Use feature-sliced architecture
- ✅ Separate Redux (auth), TanStack Query (API), useState (local)
- ✅ Always type props with interfaces
- ✅ Show loading states with skeletons or spinners
- ✅ Show error states with toast or inline messages
- ✅ Wrap features with ErrorBoundary
- ✅ Use React Hook Form for forms
- ✅ Validate with Zod before API call
- ✅ Use Tailwind utilities, no custom CSS files
- ✅ Place tests next to components
- ✅ Mock API with MSW in tests
- ✅ Use semantic HTML (`<button>`, not `<div onClick>`)
- ✅ Lazy-load routes with `React.lazy()` + `Suspense`

---

**Build fast, build with intent. Your frontend is the user's first experience. 🚀**
