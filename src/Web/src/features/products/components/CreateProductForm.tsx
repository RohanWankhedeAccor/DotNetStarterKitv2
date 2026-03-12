import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateProduct } from '../hooks/useProducts'

const createProductSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters').max(200),
  description: z.string().min(10, 'Description must be at least 10 characters').max(2000),
  price: z.coerce.number().positive('Price must be greater than 0'),
})

type CreateProductForm = z.infer<typeof createProductSchema>

interface CreateProductFormProps {
  onSuccess?: () => void
}

export default function CreateProductForm({ onSuccess }: CreateProductFormProps) {
  const [isOpen, setIsOpen] = useState(false)
  const createProduct = useCreateProduct()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<CreateProductForm>({
    resolver: zodResolver(createProductSchema),
  })

  const onSubmit = async (data: CreateProductForm) => {
    createProduct.mutate(data, {
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
        Create Product
      </button>
    )
  }

  return (
    <div className="border border-border rounded-lg p-6 bg-background mb-6">
      <h3 className="text-lg font-semibold mb-4">Create New Product</h3>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Name</label>
          <input
            {...register('name')}
            placeholder="Product name"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.name && <p className="text-sm text-destructive mt-1">{errors.name.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Description</label>
          <textarea
            {...register('description')}
            placeholder="Product description"
            rows={4}
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.description && <p className="text-sm text-destructive mt-1">{errors.description.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Price</label>
          <input
            {...register('price')}
            type="number"
            placeholder="0.00"
            step="0.01"
            className="w-full px-3 py-2 border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
          />
          {errors.price && <p className="text-sm text-destructive mt-1">{errors.price.message}</p>}
        </div>

        <div className="flex gap-2">
          <button
            type="submit"
            disabled={createProduct.isPending}
            className="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90 transition-colors text-sm font-medium disabled:opacity-50"
          >
            {createProduct.isPending ? 'Creating...' : 'Create'}
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
