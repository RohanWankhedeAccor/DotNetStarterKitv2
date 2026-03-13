import { useState } from 'react'
import { useProducts } from '../hooks/useProducts'
import CreateProductForm from '../components/CreateProductForm'
import ProductsList from '../components/ProductsList'

export default function ProductsPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { data, isLoading, isError, error } = useProducts(pageNumber, 10)

  if (isError) {
    return (
      <div className="text-center py-8">
        <p className="text-destructive font-medium">Error loading products</p>
        <p className="text-sm text-muted-foreground">{(error as Error)?.message}</p>
      </div>
    )
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Products</h1>
        <CreateProductForm />
      </div>

      <ProductsList products={data?.items || []} isLoading={isLoading} />

      {data && (
        <div className="mt-6 flex justify-between items-center">
          <p className="text-sm text-muted-foreground">
            Showing page {data.pageNumber} of {data.totalPages} ({data.totalCount} total)
          </p>
          <div className="flex gap-2">
            <button
              disabled={!data.hasPreviousPage}
              onClick={() => setPageNumber(p => p - 1)}
              className="px-3 py-2 border border-input rounded hover:bg-muted transition-colors text-sm disabled:opacity-50"
            >
              Previous
            </button>
            <button
              disabled={!data.hasNextPage}
              onClick={() => setPageNumber(p => p + 1)}
              className="px-3 py-2 border border-input rounded hover:bg-muted transition-colors text-sm disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
