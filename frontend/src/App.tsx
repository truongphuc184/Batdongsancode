import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import HomePage from './pages/HomePage';
import ListingDetailPage from './pages/ListingDetailPage';
import CreateListingPage from './pages/CreateListingPage';
import ProfilePage from './pages/ProfilePage';

const App: React.FC = () => {
  const isAuthenticated = !!localStorage.getItem('accessToken');

  const RequireAuth: React.FC<{ children: React.ReactElement; allowedRoles?: string[] }> = ({ children, allowedRoles }) => {
    if (!isAuthenticated) {
      return <Navigate to="/login" replace />;
    }
    if (allowedRoles) {
      const token = localStorage.getItem('accessToken') || '';
      const payload = JSON.parse(atob(token.split('.')[1] || '{}'));
      const userRole = payload.role;
      if (!allowedRoles.includes(userRole)) {
        return <Navigate to="/" replace />;
      }
    }
    return children;
  };

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/" element={<HomePage />} />
      <Route path="/listing/:id" element={<ListingDetailPage />} />
      <Route
        path="/create-listing"
        element={
          <RequireAuth allowedRoles={["Seller", "Broker", "Agency", "Admin"]}>
            <CreateListingPage />
          </RequireAuth>
        }
      />
      <Route
        path="/profile"
        element={
          <RequireAuth>
            <ProfilePage />
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

export default App;

