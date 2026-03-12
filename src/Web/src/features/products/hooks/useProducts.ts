import { useQuery, useMutation } from '@tanstack/react-query'
import api from '@/lib/api-client'
import { Product, ProductDTO, ProductsResponse } from '../types'
import { queryClient } from '@/lib/query-client'
import { toast } from 'sonner'

export function useProducts(pageNumber = 1, pageSize = 10) {
  return useQuery({
    queryKey: ['products', { pageNumber, pageSize }],
    queryFn: async () => {
      const response = await api.get<ProductsResponse>('/api/v1/products', {
        params: { pageNumber, pageSize },
      })
      return response.data
    },
  })
}

export function useProductById(id: string) {
  return useQuery({
    queryKey: ['products', id],
    queryFn: async () => {
      const response = await api.get<Product>(`/api/v1/products/${id}`)
      return response.data
    },
    enabled: !!id,
  })
}

export function useCreateProduct() {
  return useMutation({
    mutationFn: async (data: ProductDTO) => {
      const response = await api.post<Product>('/api/v1/products', data)
      return response.data
    },
    onSuccess: () => {
      toast.success('Product created successfully')
      queryClient.invalidateQueries({ queryKey: ['products'] })
    },
    onError: (error: any) => {
      const message = error.response?.data?.detail || 'Failed to create product'
      toast.error(message)
    },
  })
}
