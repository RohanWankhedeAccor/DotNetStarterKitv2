import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateUser } from '../hooks/useUsers'

const createUserSchema = z.object({
  email: z.string().email('Invalid email address'),
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

type CreateUserForm = z.infer<typeof createUserSchema>

interface CreateUserFormProps {
  onSuccess?: () => void
}

export default function CreateUserForm({ onSuccess }: CreateUserFormProps) {
  const [isOpen, setIsOpen] = useState(false)
  const createUser = useCreateUser()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<CreateUserForm>({
    resolver: zodResolver(createUserSchema),
  })

  const onSubmit = async (data: CreateUserForm) => {
    createUser.mutate(data, {
      onSuccess: () => {
        reset()
        setIsOpen(false)
        onSuccess?.()
      },
    })
  }

  if (!isOpen) {
    return (
      <button
        onClick={() => setIsOpen(true)}
        className="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90 transition-colors text-sm font-medium"
      >
        Create User
      </button>
    )
  }

  return (
    <div className="border border-border rounded-lg p-6 bg-background mb-6">
      <h3 className="text-lg font-semibold mb-4">Create New User</h3>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Email</label>
          <input
            {...register('email')}
            type="email"
            placeholder="user@example.com"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.email && <p className="text-sm text-destructive mt-1">{errors.email.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">First Name</label>
          <input
            {...register('firstName')}
            placeholder="John"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.firstName && <p className="text-sm text-destructive mt-1">{errors.firstName.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Last Name</label>
          <input
            {...register('lastName')}
            placeholder="Doe"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.lastName && <p className="text-sm text-destructive mt-1">{errors.lastName.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Password</label>
          <input
            {...register('password')}
            type="password"
            placeholder="••••••••"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.password && <p className="text-sm text-destructive mt-1">{errors.password.message}</p>}
        </div>

        <div className="flex gap-2">
          <button
            type="submit"
            disabled={createUser.isPending}
            className="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90 transition-colors text-sm font-medium disabled:opacity-50"
          >
            {createUser.isPending ? 'Creating...' : 'Create'}
          </button>
          <button
            type="button"
            onClick={() => {
              setIsOpen(false)
              reset()
            }}
            className="px-4 py-2 border border-input rounded hover:bg-muted transition-colors text-sm font-medium"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
