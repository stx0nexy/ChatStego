import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  CircularProgress
} from '@mui/material';
import db from '../stores/db';
import CloseIcon from '@mui/icons-material/Close';

const NewOneTimeMessages = ({ onClose, messages, connection, fullOneTimeMessage, updateMessages }) => {
  const [openConfirmDialog, setOpenConfirmDialog] = useState(false);
  const [openMessageDialog, setOpenMessageDialog] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState(null);
  const [revealedMessageContent, setRevealedMessageContent] = useState('');
  const [localMessages, setLocalMessages] = useState(messages);
  const [isLoadingMessage, setIsLoadingMessage] = useState(false);

  useEffect(() => {
    setLocalMessages(messages);
  }, [messages]);

  useEffect(() => {
    if (fullOneTimeMessage && selectedMessage && fullOneTimeMessage.messageId === selectedMessage.messageId) {
      setRevealedMessageContent(fullOneTimeMessage.extractedMessage);
      setIsLoadingMessage(false);
    }
  }, [fullOneTimeMessage, selectedMessage]);


  const uniqueMessages = Array.from(new Map(localMessages.map(msg => [msg.messageId, msg])).values());

  const handleRevealMessage = (msg) => {
    setSelectedMessage(msg);
    setOpenConfirmDialog(true);
  };

  const confirmRevealMessage = async () => {
    if (selectedMessage) {
      setOpenConfirmDialog(false);
      setRevealedMessageContent('');
      setIsLoadingMessage(true);
      setOpenMessageDialog(true);

      try {
        if (connection && connection.state === 'Connected') {
          await connection.invoke("ReadOneTimeMessage", selectedMessage.messageId);
          console.log('Requested ONE TIME Message from the server.');
        } else {
          console.error('SignalR connection is not active. Trying to reconnect...');
          try {
            await connection.start();
            await connection.invoke("ReadOneTimeMessage", selectedMessage.messageId);
            console.log('Reconnection successful. ONE TIME Message requested from server.');
          } catch (err) {
            console.error('Failed to reconnect and request message:', err);
            setIsLoadingMessage(false);
            setRevealedMessageContent('Failed to load message.');
          }
        }
      } catch (err) {
        console.error('Error sending request to read one-time message:', err);
        setIsLoadingMessage(false);
        setRevealedMessageContent('Error requesting message from server.');
      }
    }
  };

  const handleRemoveMessage = async (messageId) => {
    try {
      await db.oneTimeMessages.where('messageId').equals(messageId).delete();
      console.log(`Message ${messageId}removed from IndexedDB`);

      if (typeof updateMessages === 'function') {
        updateMessages();
      }
    } catch (error) {
      console.error('Error deleting message:', error);
    }
  };

  const handleCloseMessageDialog = () => {
    setOpenMessageDialog(false);
    if (selectedMessage) {
      handleRemoveMessage(selectedMessage.messageId);
      setSelectedMessage(null);
      setRevealedMessageContent('');
      setIsLoadingMessage(false);
    }
  };

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100vw',
        height: '100vh',
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1050,
      }}
    >
      <Box
        sx={{
          width: '66vw',
          height: '66vh',
          backgroundColor: 'white',
          borderRadius: 2,
          boxShadow: 3,
          position: 'relative',
          padding: 3,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <IconButton
          onClick={() => {
            onClose();
            updateMessages();
          }}
          sx={{
            position: 'absolute',
            top: 10,
            right: 10,
          }}
        >
          <CloseIcon />
        </IconButton>
        <Box
          sx={{
            flexGrow: 1,
            overflowY: 'auto',
            mt: 2,
            maxHeight: '80%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          {uniqueMessages.length === 0 ? (
            <Typography variant="body1" sx={{ textAlign: 'center', mt: 4, color: 'gray' }}>
              No secret messages yet
            </Typography>
          ) : (
            uniqueMessages.map((msg) => (
              <Box
                key={msg.messageId}
                sx={{
                  mb: 2,
                  backgroundColor: '#b4d0e7',
                  color: 'black',
                  borderRadius: '10px',
                  padding: '10px',
                  width: '90%',
                  wordWrap: 'break-word',
                  textAlign: 'center',
                  transition: 'background-color 0.3s',
                  cursor: 'pointer',
                  '&:hover': {
                    backgroundColor: '#61082b',
                    color: 'white',
                  },
                }}
                onClick={() => handleRevealMessage(msg)}
              >
                <Typography variant="body1">New secret message</Typography>
                <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
                  {new Date(msg.timestamp).toLocaleString()}
                </Typography>
              </Box>
            ))
          )}
        </Box>
      </Box>

      <Dialog open={openConfirmDialog} onClose={() => setOpenConfirmDialog(false)}>
        <DialogTitle>Warning</DialogTitle>
        <DialogContent>
          This message will be deleted after viewing. Do you want to continue?
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenConfirmDialog(false)}>Cancel</Button>
          <Button onClick={confirmRevealMessage} color="primary">OK</Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={openMessageDialog}
        onClose={handleCloseMessageDialog}
      >
        <DialogTitle>Secret Message</DialogTitle>
        <DialogContent>
          {isLoadingMessage ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100px' }}>
              <CircularProgress />
            </Box>
          ) : (
            <Typography>{revealedMessageContent}</Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={handleCloseMessageDialog}
            color="primary"
          >
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default NewOneTimeMessages;