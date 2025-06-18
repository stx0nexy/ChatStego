import React, { useState, useEffect } from 'react';
import { Button, TextField, List, ListItem, Checkbox, FormControlLabel, Box, IconButton, Alert, Typography } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

const CreateChatForm = ({ user, contacts, onClose, token }) => {
  const [chatName, setChatName] = useState('');
  const [chatDescription, setChatDescription] = useState('');
  const [selectedParticipants, setSelectedParticipants] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [contactNames, setContactNames] = useState({});
  const [contactsLoaded, setContactsLoaded] = useState(false);
  const [errors, setErrors] = useState({});
  const [generalError, setGeneralError] = useState('');

  useEffect(() => {
    const fetchContactNames = async () => {
      try {
        const names = await Promise.all(
          contacts.map(async (contact) => {
            const response = await fetch(`http://localhost:5038/api/Users/${contact.contactUserId}`, {
              headers: {
                Authorization: `Bearer ${token}`,
              },
            });

            if (response.ok) {
              const userData = await response.json();
              return { userId: contact.contactUserId, name: userData.username };
            } else {
              console.error(`Failed to fetch user ${contact.contactUserId}`);
              return { userId: contact.contactUserId, name: 'Unknown' };
            }
          })
        );

        const namesMap = names.reduce((acc, { userId, name }) => {
          acc[userId] = name;
          return acc;
        }, {});

        setContactNames(namesMap);
        setContactsLoaded(true);
      } catch (error) {
        console.error('Error fetching contacts:', error);
        setGeneralError('Error loading contacts.');
      }
    };

    fetchContactNames();
  }, [contacts, token]);

  const handleCreate = async () => {
    setErrors({});
    setGeneralError('');
    setIsLoading(true);

    if (selectedParticipants.length < 2) {
      setErrors(prevErrors => ({
        ...prevErrors,
        ParticipantIds: ['Please select at least 2 participants.'],
      }));
      setIsLoading(false);
      return;
    }

    const chatData = {
      Name: chatName,
      Description: chatDescription,
      IsGroup: true,
      CreatorId: user.id,
      ParticipantIds: selectedParticipants,
    };

    try {
      const response = await fetch(`http://localhost:5038/api/Chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(chatData),
      });

      if (!response.ok) {
        const errorData = await response.json();
        if (errorData.errors) {
          setErrors(errorData.errors);
        } else if (errorData.detail) {
          setGeneralError(errorData.detail);
        } else {
          setGeneralError('Failed to create chat.');
        }
        throw new Error('Failed to create chat');
      }

      setChatName('');
      setChatDescription('');
      setSelectedParticipants([]);
      onClose();
    } catch (error) {
      console.error('Failed to create chat:', error);
      if (Object.keys(errors).length === 0 && !generalError) {
        setGeneralError('Error occurred while creating chat. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box
      sx={{
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        p: 3,
        pt: 6,
        bgcolor: 'background.paper',
        borderRadius: 2,
        boxShadow: 3,
        display: 'flex',
        flexDirection: 'column',
        width: '100%',
        maxWidth: 400,
      }}
    >
      <IconButton
        sx={{
          position: 'absolute',
          top: 8,
          right: 8,
          zIndex: 10,
        }}
        onClick={onClose}
      >
        <CloseIcon />
      </IconButton>

      <Typography variant="h6" gutterBottom>Create New Chat</Typography>

      {generalError && (
        <Alert severity="error" sx={{ marginBottom: 2 }}>
          {generalError}
        </Alert>
      )}

      <TextField
        label="Chat Name"
        value={chatName}
        onChange={e => setChatName(e.target.value)}
        fullWidth
        margin="normal"
        error={!!errors.Name}
        helperText={errors.Name ? errors.Name[0] : ''}
      />
      <TextField
        label="Description"
        value={chatDescription}
        onChange={e => setChatDescription(e.target.value)}
        fullWidth
        margin="normal"
        error={!!errors.Description}
        helperText={errors.Description ? errors.Description[0] : ''}
      />
      <Typography variant="subtitle1" sx={{ mt: 2 }}>Select Participants:</Typography>
      {errors.ParticipantIds && (
        <Typography color="error" variant="caption" sx={{ ml: 1 }}>
          {errors.ParticipantIds[0]}
        </Typography>
      )}
      <List sx={{ maxHeight: 200, overflow: 'auto', border: '1px solid #ccc', borderRadius: 1 }}>
        {contactsLoaded ? (
          contacts.map(contact => (
            <ListItem key={contact.contactUserId} dense>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={selectedParticipants.includes(contact.contactUserId)}
                    onChange={() => {
                      if (errors.ParticipantIds) {
                         setErrors(prevErrors => {
                            const newErrors = { ...prevErrors };
                            delete newErrors.ParticipantIds;
                            return newErrors;
                         });
                      }

                      if (selectedParticipants.includes(contact.contactUserId)) {
                        setSelectedParticipants(selectedParticipants.filter(id => id !== contact.contactUserId));
                      } else {
                        setSelectedParticipants([...selectedParticipants, contact.contactUserId]);
                      }
                    }}
                  />
                }
                label={contactNames[contact.contactUserId] || 'Unknown'}
              />
            </ListItem>
          ))
        ) : (
          <ListItem><Typography>Loading contacts...</Typography></ListItem>
        )}
      </List>
      <Button
        variant="contained"
        color="primary"
        onClick={handleCreate}
        disabled={isLoading || !chatName.trim()}
        sx={{ mt: 2 }}
      >
        {isLoading ? 'Creating...' : 'Create Chat'}
      </Button>
      <Button variant="outlined" onClick={onClose} sx={{ mt: 1 }}>
        Cancel
      </Button>
    </Box>
  );
};

export default CreateChatForm;