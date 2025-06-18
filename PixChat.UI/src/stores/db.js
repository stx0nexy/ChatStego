import Dexie from 'dexie';

const db = new Dexie('ChatDatabase');

db.version(8).stores({
  messages: '++id, userId, senderId, receiverId, decryptedMessage, timestamp, isRead, isSent, messageId',
  chatmessages: '++id, userId, senderId, chatId, decryptedMessage, timestamp, isRead, isSent, messageId',
  oneTimeMessages: '++id, messageId, chatId, senderId, timestamp, isRead',
  files: '++id, userId, senderId, receiverId, chatId, fileName, fileType, fileData, encryptedAESKey, aesIV, timestamp, isRead, isSent, messageId, isGroup',
  keys: 'userId, publicKey, privateKey', 
});

export default db;
