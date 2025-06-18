import React, { useState } from 'react';
import { Box, IconButton, Modal } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { AddContactModal } from '../../components/AddContactModal';

const AddContact = ({ connection, onClose, user, token }) => {

    const [newContactId, setNewContactId] = useState('');

    const addContact = async () => {
        if (!connection) {
          console.error('No SignalR connection.');
          return;
        }
      
        if (connection.state !== 'Connected') {
          console.error('Connection is not in the Connected state.');
          try {
            
            await connection.start();
            console.log('Reconnected to SignalR.');
          } catch (err) {
            console.error('Failed to reconnect:', err);
            return;
          }
        }
    
        if (!newContactId || !user?.id || !token) {
          alert('User email must be specified');
          return;
        }
      
        try {
          await connection.invoke("SendFriendRequest", user.email, newContactId);
          alert(`A friend request has been sent to the user with email: ${newContactId}`);
          setNewContactId(newContactId);
        } catch (error) {
          console.error('Error sending friend request:', error);
          alert(`Failed to send friend request: ${error.message}`);
        }
      };
  
    return (
        <Modal open={addContact} onClose={onClose}>
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
          <AddContactModal
            newContactId={newContactId}
            setNewContactId={setNewContactId}
            onAddContact={addContact}
          />
        </Box>
      </Modal>
    );
  };
  
  export default AddContact;