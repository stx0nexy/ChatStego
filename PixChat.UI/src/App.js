import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Login from './pages/Login/Login';
import Register from './pages/Register/Register';
import Chat from './pages/Chat/Chat';
import Profile from './pages/Profile/Profile';
import { Box } from '@mui/material';
import { createTheme, ThemeProvider } from '@mui/material/styles';
import './App.css';

const App = () => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState('');

  const [isRegistering, setIsRegistering] = useState(false);

  useEffect(() => {
    const storedUser = localStorage.getItem('user');
    const storedToken = localStorage.getItem('token');
    if (storedUser && storedToken) {
      setUser(JSON.parse(storedUser));
      setToken(storedToken);
    }
  }, []);

  const theme = createTheme({
    palette: {
      primary: { main: '#61082b' },
      secondary: { main: '#b4d0e7' },
      background: { default: '#ffffff' },
      text: { primary: '#000000' },
    },
  });

  const handleAuthSuccess = (user, token) => {
    setUser(user);
    setToken(token);
    localStorage.setItem('user', JSON.stringify(user));
    localStorage.setItem('token', token);
  };

  const handleRegisterSuccess = () => {
    setIsRegistering(false);
  };

  const handleLogout = () => {
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    setUser(null);
    setToken('');
  };

  return (
    <ThemeProvider theme={theme}>
      <Router>
        <Box
          component="main"
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            height: '100vh',
            width: '100%',
            backgroundColor: 'var(--white-color)',
          }}
        >
          {user && token ? (
            <Routes>
              <Route
                path="/chat"
                element={
                  <Box
                    sx={{
                      height: '100%',
                      width: '97%',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Chat user={user} token={token} onLogout={handleLogout} />
                  </Box>
                }
              />
              <Route path="/profile" element={<Profile user={user} token={token} />} />
              <Route path="*" element={<Navigate to="/chat" />} />
            </Routes>
          ) : isRegistering ? (
            <Register onRegisterSuccess={handleRegisterSuccess} setIsRegistering={setIsRegistering} />
          ) : (
            <Login onAuthSuccess={handleAuthSuccess} setIsRegistering={setIsRegistering} />
          )}
        </Box>
      </Router>
    </ThemeProvider>
  );
};

export default App;