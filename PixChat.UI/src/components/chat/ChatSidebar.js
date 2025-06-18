import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Paper, Box, IconButton, Tooltip, List, ListItem, ListItemText, Alert, Snackbar, Grid, Typography } from '@mui/material';
import ChatIcon from '@mui/icons-material/Chat';
import PersonIcon from '@mui/icons-material/Person';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import GroupAddIcon from '@mui/icons-material/GroupAdd';
import SettingsIcon from '@mui/icons-material/Settings';
import ExitToAppIcon from '@mui/icons-material/ExitToApp';
import SyncIcon from '@mui/icons-material/Sync';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
import AddContact from '../../pages/Contacts/AddContact';
import CreateChatForm from '../CreateChatForm';
import ContactsList from '../../pages/Contacts/ContactsList';
import { SyncModal } from './Modals';
import db from '../../stores/db';

export const ChatSidebar = ({
  user,
  token,
  onLogout,
  contacts,
  setContacts,
  chats,
  setChats,
  contactStatus,
  onContactSelect,
  onChatSelect,
  onNotesSelect,
  onProfileOpen,
  unreadMessages,
  newMessage,
  newOneTimeMessage,
  friendRequests,
  setFriendRequests,
  onBlockContact,
  onUnBlockContact,
  onDeleteChat,
  isBlockedByUser,
  connection
}) => {
  const [showAddContact, setShowAddContact] = useState(false);
  const [showCreateChat, setShowCreateChat] = useState(false);
  const [showSyncModal, setShowSyncModal] = useState(false);
  const [syncFile, setSyncFile] = useState(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState('info');
  const navigate = useNavigate();


  const showSnackbar = (message, severity = 'info') => {
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

  const acceptFriendRequest = async (requesterId) => {
    if (!connection) {
      console.error('No SignalR connection.');
      return;
    }
    try {
      await connection.invoke("ConfirmFriendRequest", user.email, requesterId);
      setFriendRequests(prev => prev.filter(id => id !== requesterId));
      fetchContacts();
    } catch (error) {
      console.error('Error accepting friend request:', error);
    }
  };

  const rejectFriendRequest = async (requesterId) => {
    if (!connection) {
      console.error('No SignalR connection.');
      return;
    }
    try {
      await connection.invoke("RejectFriendRequest", user.email, requesterId);
      setFriendRequests(prev => prev.filter(id => id !== requesterId));
    } catch (error) {
      console.error('Error rejecting friend request:', error);
    }
  };

  const fetchContacts = useCallback(async () => {
    if (!user?.id || !token) return [];

    try {
      const response = await fetch(`http://localhost:5038/api/contacts/${user.id}/contacts`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.status === 401) {
        console.error('Unauthorized: Token is invalid or expired');
        onLogout();
        return;
      }
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      console.log('Contacts data:', data);
      setContacts(data);
      return data;
    } catch (error) {
      console.error('Error fetching contacts list:', error);
      return [];
    }
  }, [user?.id, token, onLogout, setContacts]);

  const fetchChats = useCallback(async () => {
    if (!user?.id || !token) return;

    try {
      const response = await fetch(`http://localhost:5038/api/chat/user/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.status === 401) {
        console.error('Unauthorized: Token is invalid or expired');
        onLogout();
        return;
      }
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      const groupChats = data.filter((chat) => chat.isGroup);
      setChats(groupChats);
    } catch (error) {
      console.error('Error fetching chats list:', error);
    }
  }, [user?.id, token, onLogout, setChats]);

  useEffect(() => {
    const loadInitialData = async () => {
      await fetchContacts();
      await fetchChats();
    };
    loadInitialData();
  }, [fetchContacts, fetchChats]);

  const exportData = async () => {
    try {
      const messagesData = await db.messages.toArray();
      const chatMessagesData = await db.chatmessages.toArray();
      const oneTimeMessagesData = await db.oneTimeMessages.toArray();
      const filesData = await db.files.toArray();

      const exportData = {
        messages: messagesData,
        chatmessages: chatMessagesData,
        oneTimeMessages: oneTimeMessagesData,
        files: filesData,
      };

      const jsonString = JSON.stringify(exportData, null, 2);
      const blob = new Blob([jsonString], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `chat_data_${new Date().toISOString()}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      showSnackbar('Data exported successfully!', 'success');
      console.log('Data exported successfully');
    } catch (error) {
      showSnackbar('Error exporting data. Please try again.', 'error');
      console.error('Error exporting data:', error);
    }
  };

  const importData = async (file) => {
    try {
      if (!file) {
        showSnackbar('No file selected for import.', 'warning');
        return;
      }
      const reader = new FileReader();
      reader.onload = async (event) => {
        try {
          const jsonString = event.target.result;
          const importData = JSON.parse(jsonString);

          await db.messages.clear();
          await db.chatmessages.clear();
          await db.oneTimeMessages.clear();
          await db.files.clear();

          if (importData.messages) {
            await db.messages.bulkAdd(importData.messages);
          }
          if (importData.chatmessages) {
            await db.chatmessages.bulkAdd(importData.chatmessages);
          }
          if (importData.oneTimeMessages) {
            await db.oneTimeMessages.bulkAdd(importData.oneTimeMessages);
          }
          if (importData.files) {
            await db.files.bulkAdd(importData.files);
          }

          showSnackbar('Data imported successfully! Reloading chat...', 'success');
          console.log('Data imported successfully');
          window.location.reload();
        } catch (parseError) {
          showSnackbar('Error parsing JSON from file. Invalid file format.', 'error');
          console.error('Error parsing JSON from file:', parseError);
        }
      };
      reader.readAsText(file);
    } catch (error) {
      showSnackbar('Error importing data. Please try again.', 'error');
      console.error('Error importing data:', error);
    }
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSyncFile(file);
    }
  };

  const handleImport = () => {
    importData(syncFile);
    setShowSyncModal(false);
    setSyncFile(null);
  };

  return (
    <Box
    flex={4}
      sx={{
        display: 'flex',
        flexDirection: 'row',
        height: '98vh',
        width: '100%',
      }}
    >
      <Box
        flex={0.5}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          height: '98vh',
          width: '100%',
        }}
      >
        <Paper
          sx={{
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: 2,
            borderRadius: 0
          }}
        >
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Tooltip title="All chats">
              <IconButton color="primary" onClick={() => onContactSelect(null)}>
                <ChatIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Editing profile">
              <IconButton
                  color="primary"
                  onClick={() => {
                    navigate('/profile');
                  }}
                >
                  <PersonIcon fontSize="large" />
                </IconButton>
            </Tooltip>

            <Tooltip title="Add contact">
              <IconButton
                color="primary"
                onClick={() => setShowAddContact(true)}
              >
                <PersonAddIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Create chat">
              <IconButton
                color="primary"
                onClick={() => setShowCreateChat(true)}
              >
                <GroupAddIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Data synchronization">
              <IconButton
                color="primary"
                onClick={() => setShowSyncModal(true)}
              >
                <SyncIcon fontSize="large" />
              </IconButton>
            </Tooltip>
          </Box>

          <Box>
            <Tooltip title="Settings">
              <IconButton color="secondary">
                <SettingsIcon fontSize="large" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Log out">
              <IconButton color="secondary" onClick={onLogout}>
                <ExitToAppIcon fontSize="large" />
              </IconButton>
            </Tooltip>
          </Box>
        </Paper>
      </Box>
      <Box
        flex={3.5}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          height: '98vh',
          width: '100%',
        }}
      >
        <Paper sx={{ height: '100%', display: 'flex', flexDirection: 'column', padding: 2, borderRadius: 0 }}>
          <Grid container spacing={2}>
            <Grid item xs={4}>
            </Grid>
            <Grid item xs={8}>
              <Typography variant="h6" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span>Contacts</span>
              </Typography>
            </Grid>
          </Grid>
        {showAddContact && (
          <AddContact connection={connection} onClose={() => setShowAddContact(false)} user={user} token={token} />
        )}

        {showCreateChat && (
          <CreateChatForm user={user} contacts={contacts} onClose={() => setShowCreateChat(false)} token={token} />
        )}

        <SyncModal
          showSyncModal={showSyncModal}
          setShowSyncModal={setShowSyncModal}
          exportData={exportData}
          importData={handleImport}
          syncFile={syncFile}
          setSyncFile={setSyncFile}
          handleFileChange={handleFileChange}
        />

        <Snackbar
          open={snackbarOpen}
          autoHideDuration={5000}
          onClose={handleSnackbarClose}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        >
          <Alert
            onClose={handleSnackbarClose}
            severity={snackbarSeverity}
            sx={{ width: '100%', display: 'flex', alignItems: 'center' }}
          >
            {snackbarMessage}
          </Alert>
        </Snackbar>

        <Box sx={{ marginBottom: 2 }}>
          <List>
            {friendRequests.map((requesterId) => (
              <ListItem key={requesterId}>
                <ListItemText primary={requesterId} />
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <IconButton
                    edge="end"
                    color="primary"
                    onClick={() => acceptFriendRequest(requesterId)}
                  >
                    <CheckIcon />
                  </IconButton>
                  <IconButton
                    edge="end"
                    color="secondary"
                    onClick={() => rejectFriendRequest(requesterId)}
                  >
                    <CloseIcon />
                  </IconButton>
                </Box>
              </ListItem>
            ))}
          </List>
        </Box>

        <Box
          sx={{
            flexGrow: 1,
            overflowY: 'auto',
            overflowX: 'hidden',
            maxHeight: '60vh',
            width: '100%'
          }}
        >
          <ContactsList
            user={user}
            onContactSelect={onContactSelect}
            onChatSelect={onChatSelect}
            onNotesSelect={onNotesSelect}
            contactStatus={contactStatus}
            onProfileOpen={onProfileOpen}
            token={token}
            unreadMessages={unreadMessages}
            newMessage={newMessage}
            newOneTimeMessage={newOneTimeMessage}
            contacts={contacts}
            setContacts={setContacts}
            chats={chats}
            setChats={setChats}
            onBlockContact={onBlockContact}
            onUnBlockContact={onUnBlockContact}
            onDeleteChat={onDeleteChat}
            isBlocked={isBlockedByUser}
          />
        </Box>
        </Paper>
      </Box>
    </Box>
  );
};