import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

export const useSignalR = (
  token,
  user,
  onMessageReceived,
  onGroupMessageReceived,
  onPendingMessageReceived,
  onFriendRequestReceived,
  onFriendRequestConfirm,
  onFriendRequestRejected,
  onOneTimeMessageReceived,
  onOneTimePendingMessageReceived,
  onFullOneTimePendingMessageReceived,
  onUserOnline,
  onUserOffline,
  onReceiveOnlineContacts,
  onFileReceived,
  onPendingFileReceived
) => {
  const [connection, setConnection] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    const initializeConnection = async () => {
      if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
        return;
      }

      if (!token || !user?.id) {
        console.warn('Token or user ID is missing. Skipping SignalR connection.');
        return;
      }

      if (!connectionRef.current) {
        const newConnection = new signalR.HubConnectionBuilder()
          .withUrl('http://localhost:5038/chatHub', {
            accessTokenFactory: () => token,
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets,
          })
          .withAutomaticReconnect([0, 1000, 5000, 10000])
          .configureLogging(signalR.LogLevel.Information)
          .build();

        newConnection.on('ReceiveMessage', onMessageReceived);
        newConnection.on('ReceiveGroupMessage', onGroupMessageReceived);
        newConnection.on('ReceivePendingMessage', onPendingMessageReceived);
        newConnection.on('ReceiveFriendRequest', onFriendRequestReceived);
        newConnection.on('ConfirmFriendRequest', onFriendRequestConfirm);
        newConnection.on('RejectFriendRequest', onFriendRequestRejected);
        newConnection.on('ReceiveOneTimeMessage', onOneTimeMessageReceived);
        newConnection.on('ReceiveOneTimePendingMessage', onOneTimePendingMessageReceived);
        newConnection.on('ReceiveFullOneTimePendingMessage', onFullOneTimePendingMessageReceived);
        newConnection.on('ReceiveFile', onFileReceived);
        newConnection.on('ReceivePendingFile', onPendingFileReceived);

        newConnection.on('UserOnline', (userId) => {
          console.log(`${userId} is now online`);
          if (onUserOnline && typeof onUserOnline === 'function') {
            onUserOnline(userId);
          }
        });

        newConnection.on('UserOffline', (userId) => {
          console.log(`${userId} is now offline`);
          if (onUserOffline && typeof onUserOffline === 'function') {
            onUserOffline(userId);
          }
        });

        newConnection.on('ReceiveOnlineContacts', (onlineContactEmails) => {
          console.log('Received online contacts:', onlineContactEmails);
          if (onReceiveOnlineContacts && typeof onReceiveOnlineContacts === 'function') {
            onReceiveOnlineContacts(onlineContactEmails);
          }
        });

        newConnection.onreconnecting((error) => {
          console.log('SignalR reconnecting...', error);
          setIsConnected(false);
        });

        newConnection.onreconnected(() => {
          console.log('SignalR reconnected');
          setIsConnected(true);
        });

        newConnection.onclose((error) => {
          console.error('SignalR connection closed:', error);
          setIsConnected(false);
          if (isMounted) {
            startConnection(newConnection);
          }
        });

        connectionRef.current = newConnection;
        setConnection(newConnection);
      }

      await startConnection(connectionRef.current);
    };

    const startConnection = async (conn) => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try {
          await conn.start();
          console.log('SignalR connection started');
          setIsConnected(true);
        } catch (err) {
          console.error('SignalR connection error:', err);
          setIsConnected(false);
          setTimeout(() => startConnection(conn), 2000);
        }
      }
    };

    initializeConnection();

    return () => {
      isMounted = false;
      if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
        connectionRef.current
          .stop()
          .then(() => console.log('Connection stopped'))
          .catch((err) => console.error('Error stopping connection:', err));
      }
    };
  }, [token, user?.id]);

  return { connection: connectionRef.current, isConnected };
};