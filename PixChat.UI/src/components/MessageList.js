import React, { useState, useEffect } from 'react';
import { Box, Typography, Avatar, IconButton, Modal, Button } from '@mui/material';
import db from '../stores/db';
import NewOneTimeMessages from './NewOneTimeMessages';
import DownloadIcon from '@mui/icons-material/Download';
import backgroundImage from '../components/1661337141_55-oir-mobi-p-fon-dlya-telegram-oboi-58.png';

export const MessageList = ({
  connection,
  fulloneTimeMessage,
  newOneTimeMessage,
  messages,
  files,
  user,
  chat,
  receiverProfile,
  notes,
  messagesEndRef,
  onProfileOpen,
  updateMessages,
}) => {
  const [username, setUsername] = useState('Unknown');
  const [unreadOneTimeMessageCount, setUnreadOneTimeMessageCount] = useState(0);
  const profilePictureUrl = receiverProfile ? receiverProfile.profilePictureUrl : '';
  const [showNewOneTimeMessages, setShowNewOneTimeMessages] = useState(false);
  const [oneTimeMessages, setOneTimeMessages] = useState([]);
  const [usersData, setUsersData] = useState({});
  const [openImageModal, setOpenImageModal] = useState(false);
  const [selectedImageUrl, setSelectedImageUrl] = useState('');
  const [selectedFileName, setSelectedFileName] = useState('');

  const fetchUserData = async (senderId, token) => {
    try {
      const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      return data;
    } catch (error) {
      console.error(`Error fetching user data for ${senderId}:`, error);
      return null;
    }
  };

  useEffect(() => {
    const loadUsersData = async () => {
      if (chat) {
        const uniqueSenderIds = [
          ...new Set([
            ...messages.map((msg) => msg.senderId),
            ...files.map((file) => file.senderId),
          ]),
        ];
        const token = localStorage.getItem('token');

        const newUsersData = { ...usersData };
        for (const senderId of uniqueSenderIds) {
          if (!newUsersData[senderId] && senderId !== user.email) {
            const userData = await fetchUserData(senderId, token);
            if (userData) {
              newUsersData[senderId] = {
                username: userData.username || 'Unknown',
                profilePictureUrl: userData.profilePictureUrl || '',
              };
            }
          }
        }
        setUsersData(newUsersData);
      }
    };

    loadUsersData();
  }, [messages, files, chat, user.email]);

  useEffect(() => {
    const fetchOneTimeMessages = async () => {
      if (chat) {
        setUnreadOneTimeMessageCount(0);
        setOneTimeMessages([]);
      } else if (notes) {
        try {
          const oneTimeMessages = await db.oneTimeMessages
            .where('senderId')
            .equals(user.email)
            .toArray();

          const unreadOneTimeMessages = oneTimeMessages.filter((msg) => !msg.isRead);
          setOneTimeMessages(unreadOneTimeMessages);
          setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
        } catch (error) {
          console.error('Error fetching one-time messages for Notes:', error);
        }
      } else if (user && receiverProfile?.email) {
        try {
          const oneTimeMessages = await db.oneTimeMessages
            .where('senderId')
            .equals(receiverProfile.email)
            .toArray();

          const unreadOneTimeMessages = oneTimeMessages.filter((msg) => !msg.isRead);
          setOneTimeMessages(unreadOneTimeMessages);
          setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
        } catch (error) {
          console.error('Error fetching one-time messages for contact:', error);
        }
      } else {
        setUnreadOneTimeMessageCount(0);
        setOneTimeMessages([]);
      }
    };

    fetchOneTimeMessages();
  }, [user, receiverProfile?.email, notes, chat, newOneTimeMessage]);

  useEffect(() => {
    if (chat && chat.name) {
      setUsername(chat.name);
    } else if (receiverProfile) {
      setUsername(receiverProfile.username);
    } else if (notes) {
      setUsername('Notes');
    } else {
      setUsername('Unknown');
    }
  }, [chat, receiverProfile, notes]);

  const combinedMessages = [...messages, ...files].sort((a, b) => {
    return new Date(a.timestamp) - new Date(b.timestamp);
  });

  const filteredMessages = combinedMessages.filter((item) => {
    if (chat) {
      return item.chatId === chat.id;
    } else if (receiverProfile) {
      return (
        (item.senderId === user.email && item.receiverId === receiverProfile.email) ||
        (item.senderId === receiverProfile.email && item.receiverId === user.email)
      );
    } else if (notes) {
      return item.senderId === user.email && item.receiverId === user.email;
    }
    return false;
  });

  const isImage = (fileType) => {
    const imageTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    return imageTypes.includes(fileType);
  };

  const renderFile = (file) => {
    const fileUrl = file.fileData && typeof file.fileData === 'string'
      ? `data:${file.fileType};base64,${file.fileData}`
      : '#';
    const fileNameWithExtension = file.fileName || 'unnamed_file';
    const iconColor = file.senderId === user.email ? '#b4d0e7' : '#61082b';

    if (isImage(file.fileType)) {
      return (
        <Box
          component="img"
          src={fileUrl}
          alt={fileNameWithExtension}
          sx={{
            maxWidth: '200px',
            maxHeight: '200px',
            borderRadius: '5px',
            cursor: 'pointer',
            objectFit: 'cover',
          }}
          onClick={() => {
            setSelectedImageUrl(fileUrl);
            setSelectedFileName(fileNameWithExtension);
            setOpenImageModal(true);
          }}
        />
      );
    }

    return (
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <IconButton
          component="a"
          href={fileUrl}
          download={fileNameWithExtension}
          sx={{ padding: 0 }}
        >
          <DownloadIcon sx={{ color: iconColor }} />
        </IconButton>
        <Typography variant="body1" sx={{ wordBreak: 'break-word' }}>
          {fileNameWithExtension}
        </Typography>
      </Box>
    );
  };

  const handleCloseModal = () => {
    setOpenImageModal(false);
    setSelectedImageUrl('');
    setSelectedFileName('');
  };

  return (
    <>
      <Box
        sx={{
          position: 'fixed',
          top: 0,
          width: '66%',
          maxWidth: '100vw',
          display: 'flex',
          alignItems: 'center',
          zIndex: 1,
          padding: 1,
          flexDirection: 'column',
          backgroundColor: 'white',
          overflowX: 'hidden',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', width: '100%' }}>
          <Avatar
            src={profilePictureUrl}
            alt={username}
            onClick={(e) => {
              e.stopPropagation();
              onProfileOpen(receiverProfile);
            }}
            sx={{ cursor: 'pointer' }}
          />
          <Typography
            variant="h6"
            sx={{ ml: '2', cursor: 'pointer' }}
            onClick={(e) => {
              e.stopPropagation();
              onProfileOpen(receiverProfile);
            }}
          >
            {username}
          </Typography>
        </Box>
        {unreadOneTimeMessageCount > 0 && (
          <Box
            sx={{
              mt: 1,
              width: '100%',
              textAlign: 'center',
              backgroundColor: 'rgba(97, 8, 43, 0.57)',
              padding: '8px',
              borderRadius: '5px',
              cursor: 'pointer',
              transition: 'background-color 0.3s ease-in-out',
              '&:hover': { backgroundColor: '#61082b' },
            }}
            onClick={() => {
              setShowNewOneTimeMessages(true);
            }}
          >
            <Typography variant="body2" color="white">
              You have {unreadOneTimeMessageCount} secret messages.
            </Typography>
          </Box>
        )}
      </Box>
      {showNewOneTimeMessages && (
        <NewOneTimeMessages
          onClose={() => setShowNewOneTimeMessages(false)}
          messages={oneTimeMessages}
          connection={connection}
          fullOneTimeMessage={fulloneTimeMessage}
          updateMessages={updateMessages}
        />
      )}

      <Box
        sx={{
          height: '81vh',
          overflowY: 'auto',
          mt: '44px',
          display: 'flex',
          flexDirection: 'column',
          backgroundImage: `url(${backgroundImage})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          backgroundRepeat: 'no-repeat',
          width: '100%',
          maxWidth: '100vw',
          overflowX: 'hidden',
        }}
      >
        {filteredMessages.map((item, index) => {
          const senderData = chat && usersData[item.senderId]
            ? { ...usersData[item.senderId], email: item.senderId }
            : { username: receiverProfile?.username || 'Unknown', profilePictureUrl: receiverProfile?.profilePictureUrl || '' };

          return (
            <Box
              key={index}
              sx={{
                mb: 2,
                display: 'flex',
                justifyContent: item.senderId === user.email ? 'flex-end' : 'flex-start',
                alignItems: item.senderId === user.email ? 'flex-end' : 'flex-start',
                width: '100%',
              }}
            >
              {item.senderId !== user.email ? (
                <Box sx={{ display: 'flex', alignItems: 'flex-end' }}>
                  <Avatar
                    src={senderData.profilePictureUrl}
                    alt={senderData.username}
                    onClick={(e) => {
                      e.stopPropagation();
                      onProfileOpen(senderData);
                    }}
                    sx={{ width: 32, height: 32, mr: 1, cursor: 'pointer' }}
                  />
                  <Box>
                    <Box
                      sx={{
                        backgroundColor: '#b4d0e7',
                        color: 'black',
                        borderRadius: '10px',
                        padding: '10px',
                        maxWidth: '45%',
                        minWidth: '150px',
                        overflowWrap: 'break-word',
                        wordBreak: 'break-all',
                      }}
                    >
                      <Typography variant="body1" sx={{ color: '#61082b', mb: 0.5, fontWeight: 'bold' }}>
                        {senderData.username}
                      </Typography>
                      {item.decryptedMessage ? (
                        <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                          {item.decryptedMessage}
                        </Typography>
                      ) : (
                        renderFile(item)
                      )}
                      <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
                        {new Date(item.timestamp).toLocaleString()}
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              ) : (
                <Box
                  sx={{
                    backgroundColor: '#61082b',
                    color: 'white',
                    borderRadius: '10px',
                    padding: '10px',
                    maxWidth: '45%',
                    minWidth: '150px',
                    overflowWrap: 'break-word',
                    wordBreak: 'break-all',
                  }}
                >
                  {item.decryptedMessage ? (
                    <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                      {item.decryptedMessage}
                    </Typography>
                  ) : (
                    renderFile(item)
                  )}
                  <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
                    {new Date(item.timestamp).toLocaleString()}
                  </Typography>
                </Box>
              )}
            </Box>
          );
        })}
        <div ref={messagesEndRef} />
      </Box>

      <Modal
        open={openImageModal}
        onClose={handleCloseModal}
        sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}
      >
        <Box
          sx={{
            position: 'relative',
            bgcolor: 'rgba(0, 0, 0, 0.8)',
            borderRadius: '10px',
            p: 2,
            maxWidth: '90vw',
            maxHeight: '90vh',
            overflow: 'auto',
          }}
        >
          <img
            src={selectedImageUrl}
            alt={selectedFileName}
            style={{ maxWidth: '100%', maxHeight: '80vh', objectFit: 'contain' }}
          />
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Button
              variant="contained"
              component="a"
              href={selectedImageUrl}
              download={selectedFileName}
              startIcon={<DownloadIcon />}
              sx={{ bgcolor: '#61082b', '&:hover': { bgcolor: '#8b0c3f' } }}
            >
              Download
            </Button>
          </Box>
        </Box>
      </Modal>
    </>
  );
};