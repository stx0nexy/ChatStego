import React, { useState } from 'react';
import axios from 'axios';
import {
  Container,
  TextField,
  Button,
  Typography,
  Paper,
  Box,
  Alert,
  CircularProgress,
  Snackbar,
} from '@mui/material';

const Register = ({ onRegisterSuccess, setIsRegistering }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [username, setUsername] = useState('');
  const [phone, setPhone] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [codeSent, setCodeSent] = useState(false);
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

  const handleRegister = async () => {
    setErrors({});
    setGeneralError('');
    setLoading(true);
    try {
      await axios.post('http://localhost:5038/api/auth/register', {
        email,
        password,
        username,
        phone,
      });

      setCodeSent(true);
      showSnackbar('Verification code has been sent to your email.', 'success');
    } catch (error) {
      console.error('Registration error', error);
      if (error.response && error.response.data) {
        if (error.response.data) {
          setGeneralError(error.response.data);
          showSnackbar(error.response.data, 'error');
        } else {
          setGeneralError('Registration error, please check your data.');
          showSnackbar('Registration error, please check your data.', 'error');
        }
      } else {
        setGeneralError('Network error or server unreachable.');
        showSnackbar('Network error or server unreachable.', 'error');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyCode = async () => {
    setErrors({});
    setGeneralError('');
    setLoading(true);
    try {
      await axios.post('http://localhost:5038/api/auth/verify-registration', {
        email,
        code: verificationCode,
      });

      onRegisterSuccess();
      showSnackbar('Registration complete! Please sign in.', 'success');
    } catch (error) {
      console.error('Verification error', error);
      if (error.response && error.response.data) {
        if (error.response.data.errors) {
          setErrors(error.response.data.errors);
          showSnackbar('Please correct the highlighted errors.', 'error');
        } else if (error.response.data.message) {
          setGeneralError(error.response.data.message);
          showSnackbar(error.response.data.message, 'error');
        } else {
          setGeneralError('Error verifying code.');
          showSnackbar('Error verifying code.', 'error');
        }
      } else {
        setGeneralError('Network error or server unreachable.');
        showSnackbar('Network error or server unreachable.', 'error');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container component="main" maxWidth="xs" sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
      <Paper elevation={3} sx={{ padding: 3 }}>
        <Box textAlign="center" marginBottom={2}>
          <Typography variant="h5" component="h1" gutterBottom>
            Registration
          </Typography>
        </Box>

         {generalError && (
          <Alert severity={generalError.includes('sent') || generalError.includes('complete') ? "success" : "error"} sx={{ marginBottom: 2 }}>
            {generalError}
          </Alert>
        )} 

        {!codeSent ? (
          <>
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              error={!!errors.Username}
              helperText={errors.Username ? errors.Username[0] : ''}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={!!errors.Email}
              helperText={errors.Email ? errors.Email[0] : ''}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Phone"
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              error={!!errors.Phone}
              helperText={errors.Phone ? errors.Phone[0] : ''}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              error={!!errors.Password}
              helperText={errors.Password ? errors.Password[0] : ''}
            />
            <Button
              variant="contained"
              color="primary"
              fullWidth
              onClick={handleRegister}
              sx={{ marginTop: 2 }}
              disabled={loading}
            >
              {loading ? <CircularProgress size={24} /> : 'Send code'}
            </Button>
          </>
        ) : (
          <>
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Enter verification code"
              value={verificationCode}
              onChange={(e) => setVerificationCode(e.target.value)}
              error={!!errors.Code}
              helperText={errors.Code ? errors.Code[0] : ''}
            />
            <Button
              variant="contained"
              color="primary"
              fullWidth
              onClick={handleVerifyCode}
              sx={{ marginTop: 2 }}
              disabled={loading}
            >
              {loading ? <CircularProgress size={24} /> : 'Confirm registration'}
            </Button>
          </>
        )}

        <Button
          variant="outlined"
          color="primary"
          onClick={() => setIsRegistering(false)}
          sx={{ marginTop: 2, width: '100%' }}
        >
          Already have an account? Sign in
        </Button>
      </Paper>

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
    </Container>
  );
};

export default Register;