import axios from 'axios';
import { generateRSAKeyPair } from './cryptoService';
import db from '../stores/db';

const API_BASE_URL = 'http://localhost:5038/api';

export async function getLocalPrivateKey(userId) {
  try {
    const keyData = await db.keys.get(userId);
    return keyData ? keyData.privateKey : null;
  } catch (error) {
    console.error('Error getting private key from IndexedDB:', error);
    return null;
  }
}

export async function setupUserKeys(userId, token) {
  try {
    console.log(`Checking public key for user ${userId} on server...`);
    try {
      const response = await axios.get(
        `${API_BASE_URL}/keys/public/${userId}`,
        { headers: { Authorization: `Bearer ${token}` } }
      );
      if (response.status === 200 && response.data.publicKey) {
        console.log('Public key found on server.');
        const localKeyData = await db.keys.get(userId);
        if (localKeyData && localKeyData.publicKey === response.data.publicKey) {
          console.log('Local keys match server public key. No action needed.');
          return {
            publicKey: localKeyData.publicKey,
            privateKey: localKeyData.privateKey,
          };
        } else {
          console.warn('Local keys missing or mismatched. Will generate new keys.');
          return await generateAndSaveNewKeys(userId, token);
        }
      }
    } catch (serverError) {
      if (serverError.response && serverError.response.status === 404) {
        console.log('Public key not found on server. Checking local keys...');
        const localKeyData = await db.keys.get(userId);
        if (localKeyData) {
          console.log('Found local keys in IndexedDB. Sending public key to server...');
          await axios.post(
            `${API_BASE_URL}/keys/public/${userId}`,
            { publicKey: localKeyData.publicKey },
            { headers: { Authorization: `Bearer ${token}` } }
          );
          console.log('Public key sent to server successfully.');
          return {
            publicKey: localKeyData.publicKey,
            privateKey: localKeyData.privateKey,
          };
        } else {
          console.log('No local keys found in IndexedDB. Generating new keys...');
          return await generateAndSaveNewKeys(userId, token);
        }
      } else {
        throw new Error(`Failed to check public key: ${serverError.response?.data?.message || serverError.message}`);
      }
    }
  } catch (error) {
    console.error('Error during key setup:', error);
    throw error;
  }
}

async function generateAndSaveNewKeys(userId, token) {
  try {
    console.log('Generating new RSA key pair...');
    const { publicKey, privateKey } = await generateRSAKeyPair();

    const response = await axios.post(
      `${API_BASE_URL}/keys/public/${userId}`,
      { publicKey },
      { headers: { Authorization: `Bearer ${token}` } }
    );

    if (response.status === 200 || response.status === 201) {
      console.log('Public key sent to server successfully.');
      await db.keys.put({ userId, publicKey, privateKey });
      console.log('Keys saved to IndexedDB.');
      return { publicKey, privateKey };
    } else {
      throw new Error(`Failed to save public key on server: ${response.status} - ${response.statusText}`);
    }
  } catch (error) {
    console.error('Error generating and saving keys:', error);
    throw error;
  }
}

export async function getPublicKeyFromServer(userId, token) {
  try {
    const response = await axios.get(
      `${API_BASE_URL}/keys/public/${userId}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    if (response.status === 200) {
      return response.data.publicKey;
    }
    throw new Error(`Failed to get public key for user ${userId}: ${response.status} - ${response.statusText}`);
  } catch (error) {
    console.error(`Error getting public key for user ${userId}:`, error.response?.data || error.message);
    throw error;
  }
}