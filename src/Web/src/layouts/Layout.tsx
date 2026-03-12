import { Outlet } from 'react-router'
import { Link } from 'react-router-dom'
import { Users, Package } from 'lucide-react'

export default function Layout() {
  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <nav className="border-b border-border bg-background">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center gap-8">
              <Link to="/" className="text-xl font-bold text-foreground">
                DotNetStarterKitv2
              </Link>
              <div className="flex gap-6">
                <Link
                  to="/users"
                  className="flex items-center gap-2 text-foreground hover:text-primary transition-colors"
                >
                  <Users className="w-4 h-4" />
                  Users
                </Link>
                <Link
                  to="/products"
                  className="flex items-center gap-2 text-foreground hover:text-primary transition-colors"
                >
                  <Package className="w-4 h-4" />
                  Products
                </Link>
              </div>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Outlet />
      </main>
    </div>
  )
}
