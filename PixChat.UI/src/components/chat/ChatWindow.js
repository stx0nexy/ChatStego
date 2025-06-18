import { Paper, Box, Typography } from '@mui/material';
import { MessageList } from '../../components/MessageList';
import { MessageInput } from '../../components/MessageInput';
import ViewProfile from '../../pages/Profile/ViewProfile';

export const ChatWindow = ({
  user,
  token,
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
  receiverChatId,
  notesSelect,
  receiverProfile,
  receiverChat,
  receiverNotes,
  messagesEndRef,
  selectedProfile,
  onProfileOpen,
  handleProfileClose,
  isBlockedByUser,
  isBlockedByContact,
  blockMessage,
  updateMessages,
  fullOneTimeMessage
}) => {
  return (
    <Box
      flex={8}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '98vh',
        width: '100%'
      }}
    >
      <Paper
        sx={{
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          padding: 2,
          borderRadius: 0
        }}
      >
        {!receiverEmail && !receiverChatId && !notesSelect ? (
          <Box
            sx={{
              flexGrow: 1,
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center'
            }}
          >
            <Typography variant="h6">Select a chat</Typography>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <MessageList
              connection={connection}
              fulloneTimeMessage={fullOneTimeMessage}
              newOneTimeMessage={null}
              messages={messages}
              files={files}
              user={user}
              chat={receiverChat}
              receiverProfile={receiverProfile}
              notes={receiverNotes}
              messagesEndRef={messagesEndRef}
              onProfileOpen={onProfileOpen}
              updateMessages={updateMessages}
            />
            {isBlockedByUser || isBlockedByContact ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', padding: 2 }}>
                <Typography variant="body1" color="error">{blockMessage}</Typography>
              </Box>
            ) : (
              <MessageInput
                message={message}
                setMessage={setMessage}
                onSend={sendMessage}
                messageStatus={setIsOneTimeMessage}
                isGroupChat={!!receiverChatId}
                onSendFile={onSendFile}
              />
            )}
            <ViewProfile profile={selectedProfile} onClose={handleProfileClose} />
          </Box>
        )}
      </Paper>
    </Box>
  );
};