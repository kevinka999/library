import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from './components/app-shell'
import { BooksPage } from './pages/books'
import { BookDetailsPage } from './pages/book-details'
import { NotFoundPage } from './pages/not-found'

export const routes = [{
  element: <AppShell />,
  children: [
    { path: '/', element: <Navigate to="/books" replace /> },
    { path: '/books', element: <BooksPage /> },
    { path: '/books/:bookId', element: <BookDetailsPage /> },
    { path: '*', element: <NotFoundPage /> },
  ],
}]

export const router = createBrowserRouter(routes)
