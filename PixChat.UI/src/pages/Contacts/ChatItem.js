import React, { useEffect, useState } from 'react';
import { Avatar, Typography, Badge, ListItem, ListItemAvatar, ListItemText, IconButton } from '@mui/material';
import { styled } from '@mui/material/styles';
import DeleteIcon from '@mui/icons-material/Delete';
import db from '../../stores/db';
import axios from 'axios';

const defaultGroupPic = 'path-to-default-group-image.jpg';

const StyledBadge = styled(Badge)(({ theme }) => ({
  '& .MuiBadge-badge': {
    backgroundColor: '#44b700',
    color: '#44b700',
    boxShadow: `0 0 0 2px ${theme.palette.background.paper}`,
    '&::after': {
      position: 'absolute',
      top: 0,
      left: 0,
      width: '100%',
      height: '100%',
      borderRadius: '50%',
      animation: 'ripple 1.2s infinite ease-in-out',
      border: '1px solid currentColor',
      content: '""',
    },
  },
  '@keyframes ripple': {
    '0%': {
      transform: 'scale(.8)',
      opacity: 1,
    },
    '100%': {
      transform: 'scale(2.4)',
      opacity: 0,
    },
  },
}));

const ChatItem = ({ chat, onChatSelect, token, newMessage, onDeleteChatOrLeave }) => {
  const [lastMessage, setLastMessage] = useState('');
  const [unreadMessageCount, setUnreadMessageCount] = useState(0);

  useEffect(() => {
    const fetchMessages = async () => {
      try {
        const messages = await db.chatmessages
          .where('chatId').equals(chat.id)
          .toArray();

          const sortedMessages = messages.sort((a, b) => {
            return new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime();
          });

          console.log("Sorted Messages:", sortedMessages);

        if (sortedMessages.length > 0) {
          setLastMessage(sortedMessages[0].decryptedMessage);
          const unreadMessages = sortedMessages.filter(message => !message.isRead);
          setUnreadMessageCount(unreadMessages.length);
        } else {
          setLastMessage('No messages');
          setUnreadMessageCount(0);
        }
      } catch (error) {
        console.error('Error getting messages:', error);
      }
    };
    fetchMessages();
  }, [chat.id, newMessage]);

  const handleChatSelect = async () => {
    try {
      const unreadMessages = await db.chatmessages
        .where('chatId').equals(chat.id)
        .and(message => !message.isRead)
        .toArray();

      await Promise.all(unreadMessages.map(message => db.chatmessages.update(message.id, { isRead: true })));
      setUnreadMessageCount(0);
      onChatSelect(chat.id);
    } catch (error) {
      console.error('Error updating messages to "read":', error);
    }
  };


  const handleChatDeleteAction = (event) => {
    event.stopPropagation();
    onDeleteChatOrLeave(chat.id);
  };

  return (
    <ListItem
      alignItems="flex-start"
      sx={{
        padding: '10px 0',
        borderBottom: '1px solid #e0e0e0',
        cursor: 'pointer',
        backgroundColor: unreadMessageCount > 0 ? '#f0f8ff' : 'inherit',
      }}
      onClick={handleChatSelect}
    >
      <ListItemAvatar>
        <StyledBadge
          overlap="circular"
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          variant={chat.isGroup ? undefined : 'dot'}
        >
          <Avatar alt={chat.name} src={defaultGroupPic} />
        </StyledBadge>
      </ListItemAvatar>
      <ListItemText
        primary={
          <Typography variant="subtitle1" component="span">
            {chat.name}
          </Typography>
        }
        secondary={
          <Typography component="div" style={{ marginTop: '4px' }}>
            <Typography component="span" variant="body2" color={unreadMessageCount > 0 ? 'primary' : 'text.secondary'}>
              {typeof lastMessage === 'string' && lastMessage.length > 40
                ? `${lastMessage.substring(0, 40)}...`
                : lastMessage}
            </Typography>
            <Badge badgeContent={unreadMessageCount} color="secondary" sx={{ ml: 2 }} />
          </Typography>
        }
      />
      <IconButton edge="end" aria-label="delete" onClick={handleChatDeleteAction}>
        <DeleteIcon />
      </IconButton>
    </ListItem>
  );
};

export default ChatItem;