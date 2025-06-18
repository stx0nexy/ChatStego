import React, { useState, useEffect } from 'react';
import { Avatar, Typography, ListItem, ListItemAvatar, ListItemText, IconButton, Menu, MenuItem, CircularProgress } from '@mui/material';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import db from '../../stores/db';

const defaultProfilePic = 'path-to-default-image.jpg';

const SelfChatItem = ({ user, onSelfChatSelect, token, newMessage, newOneTimeMessage}) => {
  const [lastMessage, setLastMessage] = useState('');
  const [anchorEl, setAnchorEl] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [unreadOneTimeMessageCount, setUnreadOneTimeMessageCount] = useState(0);

  useEffect(() => {
    const fetchMessages = async () => {
      try {
        if (!user || !user.email) {
          setIsLoading(false);
          return;
        } 

        const messages = await db.messages
          .where('senderId')
          .equals(user.email)
          .and((message) => message.receiverId === user.email)
          .reverse()
          .sortBy('timestamp');

        if (messages.length > 0) {
          setLastMessage(messages[0].decryptedMessage);
        } else {
          setLastMessage('No notes yet');
        }
        const oneTimeMessages = await db.oneTimeMessages
        .where('senderId').equals(user.email)
        .toArray();

      if (oneTimeMessages.length > 0) {
        const unreadOneTimeMessages = oneTimeMessages.filter(
          oneTimeMessage => !oneTimeMessage.isRead 
        );
        setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
      } else {
        setUnreadOneTimeMessageCount(0);
      }
      } catch (error) {
        console.error('Error getting messages:', error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchMessages();
  }, [user, user?.email, newMessage, newOneTimeMessage]);

  useEffect(() => {
    const fetchUnreadOneTimeMessages = async () => {
      try {
        const oneTimeMessages = await db.oneTimeMessages
          .where('senderId').equals(user.email)
          .toArray();

        const unreadOneTimeMessages = oneTimeMessages.filter(
          msg => !msg.isRead && msg.senderId === user.email
        );
        setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
      } catch (error) {
        console.error('Error fetching one-time messages:', error);
      }
    };

    if (user) {
      fetchUnreadOneTimeMessages();
    }
  }, [user, newMessage, newOneTimeMessage]);

  const handleSelfChatSelect = async () => {
    try {
      onSelfChatSelect();
    } catch (error) {
      console.error('Error updating messages to "read":', error);
    }
  };

  const handleMenuOpen = (event) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleClearNotes = async () => {
    try {
      if (!user || !user.email) return;

      await db.messages
        .where('senderId')
        .equals(user.email)
        .and((message) => message.receiverId === user.email)
        .delete();
      setLastMessage('No notes yet');
    } catch (error) {
      console.error('Error clearing notes:', error);
    }
  };

  if (!user || !user.email) {
    return (
      <ListItem alignItems="flex-start" sx={{ padding: '10px 0', borderBottom: '1px solid #e0e0e0' }}>
        <ListItemAvatar>
          <Avatar alt="Self" src={defaultProfilePic} />
        </ListItemAvatar>
        <ListItemText
          primary={
            <Typography variant="subtitle1" component="span">
              Notes
            </Typography>
          }
          secondary={
            <Typography component="div" style={{ marginTop: '4px' }}>
              <Typography component="span" variant="body2" color="text.secondary">
                {isLoading ? <CircularProgress size={20} /> : 'No notes yet'}
              </Typography>
            </Typography>
          }
        />
      </ListItem>
    );
  }

  return (
    <ListItem
      alignItems="flex-start"
      sx={{
        padding: '10px 0',
        borderBottom: '1px solid #e0e0e0',
        cursor: 'pointer',
        backgroundColor: 'inherit',
      }}
      onClick={() => handleSelfChatSelect()}
    >
      <ListItemAvatar>
        <Avatar alt="Self" src={defaultProfilePic} />
      </ListItemAvatar>

      <ListItemText
        primary={
          <Typography variant="subtitle1" component="span">
            Notes
          </Typography>
        }
        secondary={
          <Typography component="div" style={{ marginTop: '4px', display: 'flex', alignItems: 'center' }}>
            <Typography component="span" variant="body2" color="text.secondary">
              {lastMessage.length > 40 ? `${lastMessage.substring(0, 40)}...` : lastMessage}
            </Typography>
            {unreadOneTimeMessageCount > 0 && (
              <VisibilityOffIcon sx={{ ml: 2, color: '#61082b' }} />
            )}
          </Typography>
        }
      />
      <IconButton edge="end" aria-label="more" onClick={handleMenuOpen}>
        <MoreVertIcon />
      </IconButton>
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={handleClearNotes}>Clear Notes</MenuItem>
      </Menu>
    </ListItem>
  );
};

export default SelfChatItem;