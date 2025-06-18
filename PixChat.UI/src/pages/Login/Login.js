import React, { useState } from 'react';
import { TextField, Button, Box, Alert, CircularProgress, Snackbar } from '@mui/material';
import axios from 'axios';
import { setupUserKeys } from '../../api/keyService';

const Login = ({ onAuthSuccess, setIsRegistering }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState({});
  const [generalError, setGeneralError] = useState('');
  const [loading, setLoading] = useState(false);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState('success');

  const showSnackbar = (message, severity) => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  const handleSnackbarClose = (event, reason) => {
    if (reason === 'clickaway') {
      return;
    }
    setSnackbarOpen(false);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setErrors({});
    setGeneralError('');
    setLoading(true);

    try {
      const response = await axios.post('http://localhost:5038/api/auth/login', {
        email,
        password,
      });

      const { user, token } = response.data;

      if (!user.id) {
        throw new Error('User ID not provided in login response.');
      }

      showSnackbar('Login successful. Setting up user keys...', 'info');

      await setupUserKeys(user.id, token);

      showSnackbar('Keys initialized successfully!', 'success');
      onAuthSuccess(user, token);

    } catch (error) {
      console.error('Authentication or key setup error:', error);
      if (error.response && error.response.data) {
        if (error.response.data.errors) {
          setErrors(error.response.data.errors);
          showSnackbar('Please correct the highlighted errors.', 'error');
        } else if (error.response.data.message) {
          setGeneralError(error.response.data.message);
          showSnackbar(error.response.data.message, 'error');
        } else {
          setGeneralError('Oops, something went wrong. Check your data and try again.');
          showSnackbar('Oops, something went wrong. Check your data and try again.', 'error');
        }
      } else {
        setGeneralError(error.message || 'Network error or server unreachable.');
        showSnackbar(error.message || 'Network error or server unreachable.', 'error');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      {generalError && (
        <Alert severity="error" sx={{ marginBottom: 2, width: '100%' }}>
          {generalError}
        </Alert>
      )}
      <TextField
        label="Email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
        error={!!errors.Email}
        helperText={errors.Email ? errors.Email[0] : ''}
      />
      <TextField
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
        error={!!errors.Password}
        helperText={errors.Password ? errors.Password[0] : ''}
      />
      <Button type="submit" variant="contained" color="primary" disabled={loading}>
        {loading ? <CircularProgress size={24} /> : 'Login'}
      </Button>

      <Button
        variant="outlined"
        color="primary"
        onClick={() => setIsRegistering(true)}
        sx={{ marginTop: 2 }}
      >
        No account? Register
      </Button>

      <Snackbar
        open={snackbarOpen}
        autoHideDuration={5000}
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          onClose={handleSnackbarClose}
          severity={snackbarSeverity}
          sx={{ width: '100%' }}
        >
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default Login;