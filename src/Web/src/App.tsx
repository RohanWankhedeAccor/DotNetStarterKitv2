import { useState } from 'react'
import { useSelector } from 'react-redux'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import { apiClient } from './lib/api-client'
import { UserProfileMenu } from './features/auth/components/UserProfileMenu'
import { AzureLoginButton } from './features/auth/components/AzureLoginButton'
import { selectIsAuthenticated, selectAuth } from './lib/redux/store'

// ─── Types ────────────────────────────────────────────────────────────────────

interface User {
  id: number
  email: string
  fullName: string
  status: number
  createdAt: string
}

interface PagedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// ─── API Hooks ─────────────────────────────────────────────────────────────────

function useUsers(page = 1) {
  return useQuery<PagedResponse<User>>({
    queryKey: ['users', page],
    queryFn: () =>
      apiClient.get(`/api/v1/users?pageNumber=${page}&pageSize=8`).then((r) => r.data),
  })
}

function useCreateUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: { email: string; fullName: string; password: string }) =>
      apiClient.post('/api/v1/users', data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }),
  })
}

// ─── Navigation config ────────────────────────────────────────────────────────

type View = 'dashboard' | 'users'

const NAV_ITEMS: { id: View; label: string; icon: string }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: '⊞' },
  { id: 'users', label: 'Users', icon: '👤' },
]

// ─── Root App ─────────────────────────────────────────────────────────────────

export default function App() {
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const { isLoading: isSsoLoading } = useSelector(selectAuth)
  const [view, setView] = useState<View>('dashboard')
  const [search, setSearch] = useState('')
  const [sidebarOpen, setSidebarOpen] = useState(true)

  return (
    <div className="flex min-h-screen bg-[#0a0a0a] text-white font-sans">
      {/* ── Sidebar ── */}
      <aside
        className={`flex-shrink-0 flex flex-col bg-[#111113] border-r border-white/[0.07] transition-all duration-200 ${
          sidebarOpen ? 'w-64' : 'w-16'
        }`}
        style={{ minHeight: '100vh' }}
      >
        {/* Logo */}
        <div className="flex items-center gap-3 px-4 py-5 border-b border-white/[0.07]">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center text-sm font-black flex-shrink-0 shadow-lg shadow-indigo-500/20">
            D
          </div>
          {sidebarOpen && (
            <div className="min-w-0">
              <p className="text-sm font-semibold text-white truncate">DotNet Starter</p>
              <p className="text-[10px] text-white/30 truncate">v2 · Dev</p>
            </div>
          )}
        </div>

        {/* Profile / Auth area */}
        {sidebarOpen && !isSsoLoading && (
          <div className="mx-3 mt-4 mb-2">
            {isAuthenticated ? (
              <UserProfileMenu />
            ) : (
              <AzureLoginButton variant="default" fullWidth showIcon />
            )}
          </div>
        )}

        {/* Nav Section */}
        {sidebarOpen && (
          <p className="px-4 pt-4 pb-1.5 text-[10px] font-semibold text-white/25 uppercase tracking-widest">
            Navigation
          </p>
        )}

        <nav className="flex-1 px-2 space-y-0.5 mt-1">
          {NAV_ITEMS.map((item) => {
            const active = view === item.id
            return (
              <button
                key={item.id}
                onClick={() => setView(item.id)}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150 ${
                  active
                    ? 'bg-indigo-600/20 text-indigo-400 border border-indigo-500/20'
                    : 'text-white/50 hover:text-white/80 hover:bg-white/[0.04]'
                }`}
              >
                <span className="text-base flex-shrink-0">{item.icon}</span>
                {sidebarOpen && <span>{item.label}</span>}
              </button>
            )
          })}
        </nav>

        {/* Bottom actions */}
        {sidebarOpen && (
          <div className="px-2 pb-4 space-y-0.5 border-t border-white/[0.07] pt-3 mt-3">
            <button className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/40 hover:text-white/70 hover:bg-white/[0.04] transition-all">
              <span>⚙</span>
              <span>Settings</span>
            </button>
            <button className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/40 hover:text-white/70 hover:bg-white/[0.04] transition-all">
              <span>?</span>
              <span>Help & Docs</span>
            </button>
          </div>
        )}

        {/* Collapse toggle */}
        <button
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className="mx-2 mb-3 flex items-center justify-center py-2 rounded-lg text-white/20 hover:text-white/50 hover:bg-white/[0.04] transition-all text-xs"
        >
          {sidebarOpen ? '◀ Collapse' : '▶'}
        </button>
      </aside>

      {/* ── Main Content ── */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top Header */}
        <header className="flex items-center gap-4 px-6 py-4 border-b border-white/[0.07] bg-[#0a0a0a]/95 sticky top-0 z-10 backdrop-blur-sm">
          {/* Search */}
          <div className="flex-1 max-w-md relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-white/30 text-sm">🔍</span>
            <input
              type="text"
              placeholder="Search users..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full bg-white/[0.05] border border-white/[0.08] rounded-xl pl-9 pr-4 py-2.5 text-sm text-white placeholder:text-white/30 focus:outline-none focus:border-indigo-500/50 focus:bg-white/[0.07] transition-all"
            />
          </div>

          <div className="ml-auto flex items-center gap-3">
            {/* API Status */}
            <div className="flex items-center gap-2 px-3 py-1.5 bg-emerald-500/10 border border-emerald-500/20 rounded-lg">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              <span className="text-[11px] font-medium text-emerald-400">API</span>
            </div>

            {/* Notifications */}
            <button className="relative w-9 h-9 flex items-center justify-center rounded-lg bg-white/[0.05] border border-white/[0.08] text-white/50 hover:text-white hover:bg-white/[0.08] transition-all">
              🔔
              <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-indigo-500 border border-[#0a0a0a]" />
            </button>

            {/* Avatar */}
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-pink-500 to-rose-600 flex items-center justify-center text-xs font-bold cursor-pointer">
              AD
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-auto p-6">
          {view === 'dashboard' && <DashboardView setView={setView} />}
          {view === 'users' && <UsersView search={search} />}
        </main>
      </div>
    </div>
  )
}

// ─── Dashboard View ────────────────────────────────────────────────────────────

function DashboardView({ setView }: { setView: (v: View) => void }) {
  const { data: users } = useUsers(1)

  const stats = [
    {
      label: 'Total Users',
      value: users?.totalCount ?? '–',
      icon: '👤',
      color: 'from-indigo-500/20 to-violet-500/10',
      border: 'border-indigo-500/20',
      text: 'text-indigo-400',
      change: '+12%',
    },
    {
      label: 'Active Sessions',
      value: 3,
      icon: '⚡',
      color: 'from-amber-500/20 to-orange-500/10',
      border: 'border-amber-500/20',
      text: 'text-amber-400',
      change: 'Now',
    },
    {
      label: 'API Health',
      value: '99.9%',
      icon: '🟢',
      color: 'from-pink-500/20 to-rose-500/10',
      border: 'border-pink-500/20',
      text: 'text-pink-400',
      change: 'Uptime',
    },
  ]

  return (
    <div>
      {/* Welcome header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight text-white">
          Welcome back, <span className="text-indigo-400">Admin</span> 👋
        </h1>
        <p className="text-sm text-white/40 mt-1">
          Here's what's happening in DotNetStarterKitv2 today
        </p>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-3 gap-4 mb-8">
        {stats.map((s) => (
          <div
            key={s.label}
            className={`bg-gradient-to-br ${s.color} border ${s.border} rounded-2xl p-5 transition-all hover:scale-[1.01]`}
          >
            <div className="flex items-start justify-between mb-3">
              <span className="text-2xl">{s.icon}</span>
              <span className={`text-[11px] font-semibold ${s.text} bg-white/5 px-2 py-0.5 rounded-full`}>
                {s.change}
              </span>
            </div>
            <p className={`text-3xl font-bold ${s.text} mb-1`}>{s.value}</p>
            <p className="text-xs text-white/40 font-medium">{s.label}</p>
          </div>
        ))}
      </div>

      {/* Recent Users */}
      <div className="bg-[#111113] border border-white/[0.07] rounded-2xl overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4 border-b border-white/[0.07]">
          <div>
            <h2 className="text-sm font-semibold text-white">Recent Users</h2>
            <p className="text-[11px] text-white/30 mt-0.5">{users?.totalCount ?? 0} total</p>
          </div>
          <button
            onClick={() => setView('users')}
            className="text-[11px] text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
          >
            View all →
          </button>
        </div>
        <div className="divide-y divide-white/[0.05]">
          {users?.items.slice(0, 4).map((u) => (
            <div key={u.id} className="flex items-center gap-3 px-5 py-3.5 hover:bg-white/[0.02] transition-colors">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center text-xs font-bold flex-shrink-0">
                {u.fullName.charAt(0).toUpperCase()}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-white truncate">{u.fullName}</p>
                <p className="text-[11px] text-white/40 truncate">{u.email}</p>
              </div>
              <StatusDot active={u.status === 0} />
            </div>
          )) ?? <SkeletonRows count={4} />}
        </div>
      </div>
    </div>
  )
}

// ─── Users View ───────────────────────────────────────────────────────────────

function UsersView({ search }: { search: string }) {
  const [page, setPage] = useState(1)
  const [showForm, setShowForm] = useState(false)
  const { data, isLoading, error } = useUsers(page)
  const createUser = useCreateUser()
  const [form, setForm] = useState({ email: '', fullName: '', password: '' })
  const [formError, setFormError] = useState('')

  const filtered = data?.items.filter(
    (u) =>
      !search ||
      u.fullName.toLowerCase().includes(search.toLowerCase()) ||
      u.email.toLowerCase().includes(search.toLowerCase())
  )

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError('')
    try {
      await createUser.mutateAsync(form)
      setForm({ email: '', fullName: '', password: '' })
      setShowForm(false)
    } catch (err) {
      const e = err as AxiosError<{ detail?: string }>
      setFormError(e.response?.data?.detail || e.message || 'Failed to create user')
    }
  }

  return (
    <div>
      {/* Page header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl font-bold tracking-tight">Users</h1>
          <p className="text-sm text-white/40 mt-0.5">
            {data ? `${data.totalCount} registered accounts` : 'Loading...'}
          </p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white text-sm font-semibold rounded-xl transition-colors shadow-lg shadow-indigo-500/20"
        >
          <span className="text-base leading-none">+</span>
          New User
        </button>
      </div>

      {/* Create form */}
      {showForm && (
        <div className="mb-5 bg-[#111113] border border-white/[0.08] rounded-2xl p-5">
          <h2 className="text-sm font-semibold text-white/70 mb-4">Create New User</h2>
          {formError && (
            <div className="mb-3 flex items-start gap-2 text-xs text-red-400 bg-red-500/10 border border-red-500/20 px-3 py-2.5 rounded-xl">
              <span className="mt-px">⚠</span>
              {formError}
            </div>
          )}
          <form onSubmit={handleSubmit} className="grid grid-cols-3 gap-3">
            <input
              required
              type="email"
              placeholder="Email address"
              value={form.email}
              onChange={(e) => setForm((p) => ({ ...p, email: e.target.value }))}
              className="bg-white/[0.05] border border-white/[0.08] rounded-xl px-4 py-2.5 text-sm placeholder:text-white/25 focus:outline-none focus:border-indigo-500/60 focus:bg-white/[0.07] text-white transition-all"
            />
            <input
              required
              placeholder="Full name"
              value={form.fullName}
              onChange={(e) => setForm((p) => ({ ...p, fullName: e.target.value }))}
              className="bg-white/[0.05] border border-white/[0.08] rounded-xl px-4 py-2.5 text-sm placeholder:text-white/25 focus:outline-none focus:border-indigo-500/60 focus:bg-white/[0.07] text-white transition-all"
            />
            <input
              required
              type="password"
              placeholder="Password (8+ chars)"
              value={form.password}
              onChange={(e) => setForm((p) => ({ ...p, password: e.target.value }))}
              className="bg-white/[0.05] border border-white/[0.08] rounded-xl px-4 py-2.5 text-sm placeholder:text-white/25 focus:outline-none focus:border-indigo-500/60 focus:bg-white/[0.07] text-white transition-all"
            />
            <div className="col-span-3 flex justify-end gap-2 pt-1">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="px-4 py-2.5 text-sm text-white/40 hover:text-white rounded-xl transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={createUser.isPending}
                className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-sm font-semibold rounded-xl transition-colors"
              >
                {createUser.isPending ? 'Creating...' : 'Create User'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Users grid cards */}
      {isLoading ? (
        <div className="grid grid-cols-2 gap-4">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="bg-[#111113] border border-white/[0.07] rounded-2xl p-5 animate-pulse">
              <div className="flex items-center gap-3 mb-4">
                <div className="w-12 h-12 rounded-full bg-white/[0.06]" />
                <div className="space-y-2">
                  <div className="h-4 bg-white/[0.06] rounded w-32" />
                  <div className="h-3 bg-white/[0.04] rounded w-48" />
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : error ? (
        <ErrorCard error={error} />
      ) : (
        <div className="grid grid-cols-2 gap-4">
          {(filtered ?? []).map((u) => (
            <UserCard key={u.id} user={u} />
          ))}
          {(filtered ?? []).length === 0 && (
            <div className="col-span-2 py-16 text-center">
              <p className="text-2xl mb-2">🔍</p>
              <p className="text-white/40 text-sm">No users found</p>
            </div>
          )}
        </div>
      )}

      {data && <PaginationBar data={data} page={page} setPage={setPage} />}
    </div>
  )
}

// ─── Cards ────────────────────────────────────────────────────────────────────

function UserCard({ user }: { user: User }) {
  const initials = user.fullName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)

  const gradients = [
    'from-indigo-500 to-violet-600',
    'from-pink-500 to-rose-600',
    'from-amber-500 to-orange-600',
    'from-emerald-500 to-teal-600',
    'from-cyan-500 to-blue-600',
    'from-purple-500 to-pink-600',
  ]
  const gradient = gradients[user.id % gradients.length]

  return (
    <div className="group bg-[#111113] border border-white/[0.07] hover:border-white/[0.12] rounded-2xl p-5 transition-all duration-200">
      <div className="flex items-start gap-4">
        <div
          className={`w-12 h-12 rounded-full bg-gradient-to-br ${gradient} flex items-center justify-center text-sm font-bold flex-shrink-0 shadow-lg`}
        >
          {initials}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <p className="text-sm font-semibold text-white truncate">{user.fullName}</p>
            <StatusBadge active={user.status === 0} />
          </div>
          <p className="text-[12px] text-white/40 mt-0.5 truncate">{user.email}</p>
          <p className="text-[11px] text-white/25 mt-2">Joined {fmtDate(user.createdAt)}</p>
        </div>
      </div>

      {/* Action row */}
      <div className="flex items-center gap-2 mt-4 pt-4 border-t border-white/[0.06]">
        <button className="flex-1 py-2 rounded-lg text-xs font-medium bg-white/[0.04] hover:bg-indigo-600/20 hover:text-indigo-400 text-white/50 transition-all border border-white/[0.06] hover:border-indigo-500/30">
          View Profile
        </button>
        <button className="w-8 h-8 flex items-center justify-center rounded-lg bg-white/[0.04] hover:bg-white/[0.08] text-white/30 hover:text-white/60 transition-all border border-white/[0.06] text-sm">
          ✉
        </button>
        <button className="w-8 h-8 flex items-center justify-center rounded-lg bg-white/[0.04] hover:bg-white/[0.08] text-white/30 hover:text-white/60 transition-all border border-white/[0.06] text-sm">
          ⋯
        </button>
      </div>
    </div>
  )
}

// ─── Shared UI ────────────────────────────────────────────────────────────────

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span
      className={`inline-flex items-center gap-1 text-[10px] font-semibold px-2 py-0.5 rounded-full flex-shrink-0 ${
        active
          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
          : 'bg-white/[0.05] text-white/30 border border-white/[0.08]'
      }`}
    >
      <span className={`w-1 h-1 rounded-full ${active ? 'bg-emerald-400' : 'bg-white/20'}`} />
      {active ? 'Active' : 'Inactive'}
    </span>
  )
}

function StatusDot({ active }: { active: boolean }) {
  return (
    <span className={`w-2 h-2 rounded-full flex-shrink-0 ${active ? 'bg-emerald-400' : 'bg-white/20'}`} />
  )
}

function SkeletonRows({ count }: { count: number }) {
  return (
    <>
      {[...Array(count)].map((_, i) => (
        <div key={i} className="flex items-center gap-3 px-5 py-3.5 animate-pulse">
          <div className="w-8 h-8 rounded-full bg-white/[0.05] flex-shrink-0" />
          <div className="flex-1 space-y-1.5">
            <div className="h-3 bg-white/[0.05] rounded w-24" />
            <div className="h-2.5 bg-white/[0.03] rounded w-36" />
          </div>
        </div>
      ))}
    </>
  )
}

function ErrorCard({ error }: { error: unknown }) {
  return (
    <div className="flex flex-col items-center justify-center py-20 bg-[#111113] border border-red-500/20 rounded-2xl">
      <p className="text-3xl mb-3">⚠️</p>
      <p className="text-sm font-semibold text-red-400 mb-1">Could not load data</p>
      <p className="text-xs text-white/30 text-center max-w-xs">
        {(error as Error)?.message ?? 'Make sure the API is running on https://localhost:5001'}
      </p>
    </div>
  )
}

function PaginationBar({
  data,
  page,
  setPage,
}: {
  data: PagedResponse<unknown>
  page: number
  setPage: (p: number) => void
}) {
  return (
    <div className="mt-6 flex items-center justify-between">
      <span className="text-xs text-white/30">
        Page {data.pageNumber} of {data.totalPages} — {data.totalCount} records
      </span>
      <div className="flex gap-2">
        <button
          onClick={() => setPage(page - 1)}
          disabled={!data.hasPreviousPage}
          className="px-4 py-2 text-xs font-medium border border-white/[0.08] rounded-xl hover:bg-white/[0.05] disabled:opacity-30 disabled:cursor-not-allowed transition-colors text-white/60"
        >
          ← Previous
        </button>
        <button
          onClick={() => setPage(page + 1)}
          disabled={!data.hasNextPage}
          className="px-4 py-2 text-xs font-medium border border-white/[0.08] rounded-xl hover:bg-white/[0.05] disabled:opacity-30 disabled:cursor-not-allowed transition-colors text-white/60"
        >
          Next →
        </button>
      </div>
    </div>
  )
}

function fmtDate(iso: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}
