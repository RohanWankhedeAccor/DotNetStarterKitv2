export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  status: 'Active' | 'Inactive' | 'Suspended' | 'PendingActivation'
  createdAt: string
  createdBy: string
  modifiedAt: string
  modifiedBy: string
}

export interface UserDTO {
  email: string
  firstName: string
  lastName: string
  password: string
}

export interface UsersResponse {
  items: User[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}
