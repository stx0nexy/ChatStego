import React from 'react';
import { Box } from '@mui/material';
import { ChatSidebar } from './ChatSidebar';
import { ChatWindow } from './ChatWindow';
import { useNavigate } from 'react-router-dom';

export const ChatContainer = ({
  user,
  token,
  onLogout,
  connection,
  messages,
  files,
  message,
  setMessage,
  sendMessage,
  isOneTimeMessage,
  setIsOneTimeMessage,
  onSendFile,
  receiverEmail,
  setReceiverEmail,
  receiverChatId,
  setReceiverChatId,
  notesSelect,
  setNotesSelect,
  receiverProfile,
  receiverChat,
  receiverNotes,
  messagesEndRef,
  selectedProfile,
  setSelectedProfile,
  contacts,
  setContacts,
  chats,
  setChats,
  unreadMessages,
  newMessage,
  newOneTimeMessage,
  fullOneTimeMessage,
  friendRequests,
  setFriendRequests,
  onBlockContact,
  onUnBlockContact,
  onDeleteChat,
  isBlockedByUser,
  isBlockedByContact,
  blockMessage,
  updateMessages,
  contactStatus,
  handleUserOnline,
  handleUserOffline,
  handleReceiveOnlineContacts
}) => {
  const navigate = useNavigate();

  const handleProfileOpen = (profile) => {
    setSelectedProfile(profile);
  };

  const handleProfileClose = () => {
    setSelectedProfile(null);
  };

  const handleContactSelect = (email) => {
    setNotesSelect(false);
    setReceiverChatId(null);
    setReceiverEmail(email);
  };

  const handleChatSelect = (chatId) => {
    setNotesSelect(false);
    setReceiverEmail(null);
    setReceiverChatId(chatId);
  };

  const handleNotesSelect = () => {
    setReceiverEmail(null);
    setReceiverChatId(null);
    setNotesSelect(true);
  };

  const handleSidebarProfileClick = () => {
    navigate('/profile');
  }


  return (
    <Box
      display="flex"
      flexDirection="row"
      sx={{
        height: '100%',
        width: '100%',
        padding: 1,
        gap: 0,
      }}
    >
      <ChatSidebar
        user={user}
        token={token}
        onLogout={onLogout}
        contacts={contacts}
        setContacts={setContacts}
        chats={chats}
        setChats={setChats}
        contactStatus={contactStatus}
        onContactSelect={handleContactSelect}
        onChatSelect={handleChatSelect}
        onNotesSelect={handleNotesSelect}
        onProfileOpen={handleProfileOpen}
        unreadMessages={unreadMessages}
        newMessage={newMessage}
        newOneTimeMessage={newOneTimeMessage}
        friendRequests={friendRequests}
        setFriendRequests={setFriendRequests}
        onBlockContact={onBlockContact}
        onUnBlockContact={onUnBlockContact}
        onDeleteChat={onDeleteChat}
        isBlockedByUser={isBlockedByUser}
        connection={connection}
        handleUserOnline={handleUserOnline}
        handleUserOffline={handleUserOffline}
        handleReceiveOnlineContacts={handleReceiveOnlineContacts}
      />
      <ChatWindow
        user={user}
        token={token}
        connection={connection}
        messages={messages}
        files={files}
        message={message}
        setMessage={setMessage}
        sendMessage={sendMessage}
        isOneTimeMessage={isOneTimeMessage}
        setIsOneTimeMessage={setIsOneTimeMessage}
        onSendFile={onSendFile}
        receiverEmail={receiverEmail}
        receiverChatId={receiverChatId}
        notesSelect={notesSelect}
        receiverProfile={receiverProfile}
        receiverChat={receiverChat}
        receiverNotes={receiverNotes}
        messagesEndRef={messagesEndRef}
        selectedProfile={selectedProfile}
        onProfileOpen={handleProfileOpen}
        handleProfileClose={handleProfileClose}
        isBlockedByUser={isBlockedByUser}
        isBlockedByContact={isBlockedByContact}
        blockMessage={blockMessage}
        updateMessages={updateMessages}
        fullOneTimeMessage={fullOneTimeMessage}
      />
    </Box>
  );
};