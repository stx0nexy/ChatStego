import React, { useState, useEffect } from 'react';
import { Box, Avatar, Typography, TextField, Button, IconButton, Alert, CircularProgress } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const Profile = ({ user, token }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [profileData, setProfileData] = useState({
    username: '',
    email: '',
    phone: '',
    profilePicture: '',
    passwordHash: '',
  });

  const [updatedProfileData, setUpdatedProfileData] = useState(profileData);
  const [selectedImage, setSelectedImage] = useState(null);
  const [errors, setErrors] = useState({});
  const [generalError, setGeneralError] = useState('');
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();

  const fetchUserData = async () => {
    setLoading(true);
    setGeneralError('');
    try {
      const response = await axios.get(`http://localhost:5038/api/users/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const userData = response.data;
      setProfileData({
        username: userData.username,
        email: userData.email,
        phone: userData.phone,
        profilePicture: userData.profilePictureUrl,
        passwordHash: userData.passwordHash,
      });
      setUpdatedProfileData({
        username: userData.username,
        email: userData.email,
        phone: userData.phone,
        profilePicture: userData.profilePictureUrl,
      });
    } catch (error) {
      console.error('Error loading profile data:', error.response?.data || error.message);
      setGeneralError('Failed to load profile data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUserData();
  }, []);

  const handleEditClick = () => {
    setIsEditing(true);
    setErrors({});
    setGeneralError('');
    setSelectedImage(null);
  };

  const handleSaveClick = async () => {
    setErrors({});
    setGeneralError('');
    setLoading(true);
    try {
      const dataToUpdate = {
        id: user.id,
        username: updatedProfileData.username,
        email: updatedProfileData.email,
        phone: updatedProfileData.phone,
        profilePictureUrl: updatedProfileData.profilePicture,
        passwordHash: profileData.passwordHash
      };

      await axios.put(`http://localhost:5038/api/users/${user.id}`, dataToUpdate, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (selectedImage) {
        const formData = new FormData();
        formData.append('image', selectedImage);
        
        await axios.post(`http://localhost:5038/api/users/${user.id}/uploadPhoto`, formData, {
          headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'multipart/form-data' },
        });
      }

      await fetchUserData();
      setIsEditing(false);
      setGeneralError('Profile updated successfully!');
    } catch (error) {
      console.error('Error updating profile:', error.response?.data || error.message);
      if (error.response && error.response.data) {
        if (error.response.data.errors) {
          setErrors(error.response.data.errors);
        } else if (error.response.data.message) {
          setGeneralError(error.response.data.message);
        } else {
          setGeneralError('Error updating profile.');
        }
      } else {
        setGeneralError('Network error or server unreachable.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancelClick = () => {
    setUpdatedProfileData(profileData);
    setIsEditing(false);
    setErrors({});
    setGeneralError('');
    setSelectedImage(null);
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      setSelectedImage(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setUpdatedProfileData({ ...updatedProfileData, profilePicture: reader.result });
      };
      reader.readAsDataURL(file);
    } else {
      setSelectedImage(null);
      setUpdatedProfileData({ ...updatedProfileData, profilePicture: profileData.profilePicture });
    }
  };

  const handleGoBack = () => {
    navigate('/chat');
  };

  return (
    <Box sx={{ position: 'relative', display: 'flex', flexDirection: 'column', alignItems: 'center', padding: 2 }}>
      <IconButton
        onClick={handleGoBack}
        sx={{ position: 'absolute', top: 10, right: 10 }}
      >
        <CloseIcon />
      </IconButton>

      <Avatar src={updatedProfileData.profilePicture} alt={updatedProfileData.username} sx={{ width: 80, height: 80 }} />

      {generalError && (
        <Alert severity={generalError.includes('successfully') ? "success" : "error"} sx={{ marginTop: 2, width: '100%' }}>
          {generalError}
        </Alert>
      )}

      {isEditing ? (
        <>
          <TextField
            label="Username"
            value={updatedProfileData.username}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, username: e.target.value })}
            margin="normal"
            error={!!errors.Username}
            helperText={errors.Username ? errors.Username[0] : ''}
          />
          <TextField
            label="Email"
            value={updatedProfileData.email}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, email: e.target.value })}
            margin="normal"
            error={!!errors.Email}
            helperText={errors.Email ? errors.Email[0] : ''}
          />
          <TextField
            label="Phone"
            value={updatedProfileData.phone}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, phone: e.target.value })}
            margin="normal"
            error={!!errors.Phone}
            helperText={errors.Phone ? errors.Phone[0] : ''}
          />

          <input
            type="file"
            onChange={handleImageChange}
            accept="image/*"
            style={{ display: 'none' }}
            id="upload-profile-picture"
          />
          <label htmlFor="upload-profile-picture">
            <Button variant="outlined" component="span" sx={{ marginTop: 2 }}>
              Choose Profile Picture
            </Button>
          </label>
          <Typography variant="body2" color="textSecondary" sx={{ mt: 1 }}>
            {selectedImage ? selectedImage.name : 'No file chosen'}
          </Typography>

          {errors.image && (
            <Typography color="error" variant="caption" sx={{ ml: 1 }}>
              {errors.image[0]}
            </Typography>
          )}

          <Box sx={{ display: 'flex', gap: 2, marginTop: 2 }}>
            <Button variant="contained" color="primary" onClick={handleSaveClick} disabled={loading}>
              {loading ? <CircularProgress size={24} /> : 'Save'}
            </Button>
            <Button variant="outlined" onClick={handleCancelClick} disabled={loading}>
              Cancel
            </Button>
          </Box>
        </>
      ) : (
        <>
          <Typography variant="h6">{profileData.username}</Typography>
          <Typography variant="body1">{profileData.email}</Typography>
          <Typography variant="body1">{profileData.phone}</Typography>
          <Button variant="contained" color="primary" sx={{ marginTop: 2 }} onClick={handleEditClick}>
            Edit
          </Button>
        </>
      )}
    </Box>
  );
};

export default Profile;