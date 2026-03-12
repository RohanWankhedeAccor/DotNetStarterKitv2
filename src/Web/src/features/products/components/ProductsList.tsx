import { Product } from '../types'
import { DollarSign, Clock } from 'lucide-react'

interface ProductsListProps {
  products: Product[]
  isLoading: boolean
}

export default function ProductsList({ products, isLoading }: ProductsListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="text-muted-foreground">Loading products...</div>
      </div>
    )
  }

  if (!products.length) {
    return (
      <div className="flex items-center justify-center h-32 border border-dashed border-border rounded">
        <div className="text-muted-foreground">No products found</div>
      </div>
    )
  }

  return (
    <div className="border border-border rounded-lg overflow-hidden">
      <table className="w-full">
        <thead className="bg-muted">
          <tr>
            <th className="px-6 py-3 text-left text-sm font-semibold">Name</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Description</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Price</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Status</th>
            <th className="px-6 py-3 text-left text-sm font-semibold">Created</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {products.map((product) => (
            <tr key={product.id} className="hover:bg-muted/50 transition-colors">
              <td className="px-6 py-4 text-sm font-medium">{product.name}</td>
              <td className="px-6 py-4 text-sm text-muted-foreground line-clamp-2">
                {product.description}
              </td>
              <td className="px-6 py-4 text-sm">
                <div className="flex items-center gap-2">
                  <DollarSign className="w-4 h-4 text-muted-foreground" />
                  {product.price.toFixed(2)}
                </div>
              </td>
              <td className="px-6 py-4 text-sm">
                <span className="px-2 py-1 rounded text-xs font-medium bg-accent/10 text-accent">
                  {product.status}
                </span>
              </td>
              <td className="px-6 py-4 text-sm text-muted-foreground">
                <div className="flex items-center gap-2">
                  <Clock className="w-4 h-4" />
                  {new Date(product.createdAt).toLocaleDateString()}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
