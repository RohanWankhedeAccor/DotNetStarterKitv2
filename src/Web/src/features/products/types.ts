export interface Product {
  id: string
  name: string
  description: string
  price: number
  status: 'Draft' | 'Active' | 'Discontinued'
  createdAt: string
  createdBy: string
  modifiedAt: string
  modifiedBy: string
}

export interface ProductDTO {
  name: string
  description: string
  price: number
}

export interface ProductsResponse {
  items: Product[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}
