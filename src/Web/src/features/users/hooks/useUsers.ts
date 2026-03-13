import { useQuery, useMutation } from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import api from '@/lib/api-client'
import { User, UserDTO, UsersResponse } from '../types'
import { queryClient } from '@/lib/query-client'
import { toast } from 'sonner'

export function useUsers(pageNumber = 1, pageSize = 10) {
  return useQuery({
    queryKey: ['users', { pageNumber, pageSize }],
    queryFn: async () => {
      const response = await api.get<UsersResponse>('/api/v1/users', {
        params: { pageNumber, pageSize },
      })
      return response.data
    },
  })
}

export function useUserById(id: string) {
  return useQuery({
    queryKey: ['users', id],
    queryFn: async () => {
      const response = await api.get<User>(`/api/v1/users/${id}`)
      return response.data
    },
    enabled: !!id,
  })
}

export function useCreateUser() {
  return useMutation({
    mutationFn: async (data: UserDTO) => {
      const response = await api.post<User>('/api/v1/users', data)
      return response.data
    },
    onSuccess: () => {
      toast.success('User created successfully')
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError: (error: AxiosError<{ detail?: string }>) => {
      const message = error.response?.data?.detail || 'Failed to create user'
      toast.error(message)
    },
  })
}
