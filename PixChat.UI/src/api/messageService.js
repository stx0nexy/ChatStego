import axios from 'axios';
import db from '../stores/db';
import { decryptDataAES, decryptAESKeyWithRSA } from './cryptoService';
import { getLocalPrivateKey } from './keyService';

export const extractMessageFromImage = async (user, token, senderId, base64Image, chatId, messageId, isOneTime) => {
  try {
    const isGroupChat = chatId !== null && chatId !== undefined;
    const keySecondPart = isGroupChat ? chatId : user.email;
    const encryptedKey = await generateDynamicKey(senderId, keySecondPart);
    console.log('Generated encryptedKey:', encryptedKey, 'with senderId:', senderId, 'and second part:', keySecondPart);

    const responseEM = await axios.post(
      `http://localhost:5038/api/users/${user.id}/receiveMessage`,
      { base64Image, encryptedKey },
      { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );

    const { message, timestamp: serverTimestamp, encryptedAesKey, aesIv } = responseEM.data;

    console.log('Extracted from server:', { message, serverTimestamp, encryptedAesKey, aesIv });

    let aesKeyBase64;

    if (isGroupChat) {
      if (!encryptedAesKey || typeof encryptedAesKey !== 'string') {
        throw new Error(`Invalid encryptedAESKey for group chat: expected a JSON string, got ${typeof encryptedAesKey}`);
      }

      let parsedAesKey;
      try {
        parsedAesKey = JSON.parse(encryptedAesKey);
      } catch (err) {
        throw new Error(`Failed to parse encryptedAESKey JSON: ${err.message}`);
      }

      if (typeof parsedAesKey !== 'object' || parsedAesKey === null || !parsedAesKey[user.id.toString()]) {
        throw new Error(`Invalid or missing encryptedAESKey for user ${user.id} in group chat`);
      }

      aesKeyBase64 = parsedAesKey[user.id.toString()];
    } else {
      if (!encryptedAesKey || typeof encryptedAesKey !== 'string' || encryptedAesKey.trim() === '') {
        throw new Error('Invalid or missing encryptedAESKey');
      }
      aesKeyBase64 = encryptedAesKey;
    }

    if (!message || typeof message !== 'string') {
      throw new Error('Invalid encrypted message');
    }
    if (!aesIv || typeof aesIv !== 'string') {
      throw new Error('Invalid aesIV');
    }

    const privateKey = await getLocalPrivateKey(user.id);
    if (!privateKey) {
      throw new Error('Private key not found for user');
    }

    const aesKey = await decryptAESKeyWithRSA(aesKeyBase64, privateKey);

    const decryptedMessage = await decryptDataAES(message, aesKey, aesIv);

    const newMsg = {
      senderId,
      decryptedMessage,
      timestamp: serverTimestamp ? new Date(serverTimestamp).toISOString() : new Date().toISOString(),
      messageId: messageId || null,
      chatId,
    };

    if (newMsg.messageId) {
      const messageExists = await db.messages.where('messageId').equals(newMsg.messageId).count();
      const chatMessageExists = chatId ? await db.chatmessages.where('messageId').equals(newMsg.messageId).count() : 0;
      if (messageExists > 0 || chatMessageExists > 0) {
        console.log('Message with this ID already exists:', newMsg.messageId);
        return decryptedMessage;
      }
    }

    if (chatId) {
      await db.chatmessages.add({
        userId: user.id,
        senderId,
        chatId,
        decryptedMessage,
        timestamp: newMsg.timestamp,
        isRead: false,
        isSent: false,
        messageId: newMsg.messageId,
      });
      console.log('Saved to chatmessages:', newMsg);
    } else if (isOneTime) {
      console.log('Not saved to chatmessages:', newMsg);
    } else {
      await db.messages.add({
        userId: user.id,
        senderId,
        receiverId: user.email,
        decryptedMessage,
        timestamp: newMsg.timestamp,
        isRead: false,
        isSent: false,
        messageId: newMsg.messageId,
      });
      console.log('Saved to messages:', newMsg);
    }

    return decryptedMessage;
  } catch (err) {
    console.error('Error extracting message:', err);
    if (err.response) {
      console.error('Server response:', err.response.data);
    }
    return 'Failed to extract message';
  }
};

async function generateDynamicKey(senderId, receiverIdOrChatId) {
  const input = `${senderId}${receiverIdOrChatId}`;
  console.log('Generating key with input:', input);
  const encoder = new TextEncoder();
  const data = encoder.encode(input);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = new Uint8Array(hashBuffer);
  return btoa(String.fromCharCode(...hashArray));
}