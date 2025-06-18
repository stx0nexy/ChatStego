import React from 'react';
import { Box, Avatar, Typography, IconButton, Modal } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

const ViewProfile = ({ profile, onClose }) => (
  <Modal open={!!profile} onClose={onClose}>
    <Box
      sx={{
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        padding: 2,
        backgroundColor: 'white',
        borderRadius: 2,
        boxShadow: 3,
        width: '100%',
        maxWidth: 400,
      }}
    >
      <IconButton onClick={onClose} sx={{ position: 'absolute', top: 10, right: 10 }}>
        <CloseIcon />
      </IconButton>
      <Avatar src={profile?.profilePictureUrl} alt={profile?.username} sx={{ width: 80, height: 80 }} />
      <Typography variant="h6" sx={{ marginTop: 2 }}>{profile?.username}</Typography>
      <Typography variant="body1">{profile?.email}</Typography>
      {profile?.phone && (
        <Typography variant="body1" sx={{ marginTop: 1 }}>{profile.phone}</Typography>
      )}
    </Box>
  </Modal>
);

export default ViewProfile;