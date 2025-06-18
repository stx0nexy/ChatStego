import React, { useState } from 'react';
import { Box, TextField, Button, IconButton, Popover, Typography } from '@mui/material';
import TimesOneMobiledataIcon from '@mui/icons-material/TimesOneMobiledata';
import EmojiEmotionsIcon from '@mui/icons-material/EmojiEmotions';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import EmojiPicker from 'emoji-picker-react';

export const MessageInput = ({ message, setMessage, onSend, messageStatus, isGroupChat, onSendFile }) => {
  const [isOneTime, setIsOneTime] = useState(false);
  const [anchorEl, setAnchorEl] = useState(null);
  const [selectedFile, setSelectedFile] = useState(null);

  const maxChars = 1000;
  const getUTF8ByteLength = (str) => {
    return new TextEncoder().encode(str).length;
  };

  const currentCharCount = getUTF8ByteLength(message);

  const canSend = message.trim() && currentCharCount <= maxChars;

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey && canSend) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleSend = () => {
    if (canSend) {
      messageStatus(isOneTime);
      onSend();
      setIsOneTime(false);
      setMessage('');
    }
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSelectedFile(file);
      handleSendFile(file);
    }
  };

  const handleSendFile = (file) => {
    if (file) {
      onSendFile(file);
      setSelectedFile(null);
    }
  };

  const handleEmojiClick = (emojiObject) => {
    const newMessage = message + emojiObject.emoji;
    if (getUTF8ByteLength(newMessage) <= maxChars) {
      setMessage(newMessage);
    }
    setAnchorEl(null);
  };

  const handleOpenEmojiPicker = (event) => {
    setAnchorEl(event.currentTarget);
  };

  const handleCloseEmojiPicker = () => {
    setAnchorEl(null);
  };

  const handleMessageChange = (e) => {
    const newMessage = e.target.value;
    if (getUTF8ByteLength(newMessage) <= maxChars) {
      setMessage(newMessage);
    }
  };

  return (
    <Box
      sx={{
        width: '100%',
        display: 'flex',
        backgroundColor: 'white',
        padding: 1,
        boxShadow: 3,
        zIndex: 1000,
        alignItems: 'center',
        height: '60px',
        flexShrink: 0,
      }}
    >
      <Box sx={{ flexGrow: 1, position: 'relative' }}>
        <TextField
          variant="outlined"
          placeholder="Enter your message"
          value={message}
          onChange={handleMessageChange}
          onKeyDown={handleKeyDown}
          fullWidth
          multiline
          maxRows={4}
          sx={{
            maxHeight: '60px',
          }}
        />
        <Typography
          variant="caption"
          sx={{
            position: 'absolute',
            bottom: 4,
            right: 8,
            color: currentCharCount > maxChars ? 'red' : 'gray',
          }}
        >
          {currentCharCount}/{maxChars}
        </Typography>
      </Box>

      <IconButton onClick={handleOpenEmojiPicker} sx={{ ml: 1 }}>
        <EmojiEmotionsIcon />
      </IconButton>

      <IconButton component="label" sx={{ ml: 1 }}>
        <AttachFileIcon />
        <input type="file" hidden onChange={handleFileChange} />
      </IconButton>

      {!isGroupChat && (
        <IconButton
          onClick={() => setIsOneTime(!isOneTime)}
          sx={{ color: isOneTime ? 'red' : 'gray', ml: 1 }}
        >
          <TimesOneMobiledataIcon />
        </IconButton>
      )}

      <Button
        variant="contained"
        color="primary"
        onClick={handleSend}
        disabled={!canSend}
        sx={{
          ml: 1,
          height: '100%',
          backgroundColor: !canSend ? 'gray' : undefined,
          '&:hover': { backgroundColor: !canSend ? 'gray' : undefined },
        }}
      >
        Send
      </Button>

      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={handleCloseEmojiPicker}
        anchorOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
      >
        <EmojiPicker onEmojiClick={handleEmojiClick} />
      </Popover>
    </Box>
  );
};