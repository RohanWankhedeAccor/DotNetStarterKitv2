import { User } from '../types'
import { Mail, Clock } from 'lucide-react'

interface UsersListProps {
  users: User[]
  isLoading: boolean
}

export default function UsersList({ users, isLoading }: UsersListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="text-muted-foreground">Loading users...</div>
      </div>
    )
  }

  if (!users.length) {
    return (
      <div className="flex items-center justify-center h-32 border border-dashed border-border rounded">
        <div className="text-muted-foreground">No users found</div>
      </div>
    )
  }

  return (
    <div className="border border-border rounded-lg overflow-hidden">
      <table className="w-full">
        <thead className="bg-muted">
          <tr>
            <th className="px-6 py-3 text-left text-sm font-semibold">Email</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Full Name</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Status</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Created</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-muted/50 transition-colors">
              <td className="px-6 py-4 text-sm">
                <div className="flex items-center gap-2">
                  <Mail className="w-4 h-4 text-muted-foreground" />
                  {user.email}
                </div>
              </td>
              <td className="px-6 py-4 text-sm">{user.fullName}</td>
              <td className="px-6 py-4 text-sm">
                <span className="px-2 py-1 rounded text-xs font-medium bg-primary/10 text-primary">
                  {user.status}
                </span>
              </td>
              <td className="px-6 py-4 text-sm text-muted-foreground">
                <div className="flex items-center gap-2">
                  <Clock className="w-4 h-4" />
                  {new Date(user.createdAt).toLocaleDateString()}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
