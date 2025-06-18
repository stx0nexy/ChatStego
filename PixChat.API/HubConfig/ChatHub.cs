using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.HubConfig
{
   [Authorize]
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();
        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;
        private readonly ISteganographyService _steganographyService;
        private readonly IKeyService _keyService;
        private readonly IOfflineMessageService _offlineMessageService;
        private readonly IContactService _contactService;
        private readonly IChatService _chatService;
        private readonly IOneTimeMessageService _oneTimeMessageService;

        public ChatHub(
            IUserService userService, 
            ILogger<ChatHub> logger, 
            ISteganographyService steganographyService, 
            IKeyService keyService,
            IOfflineMessageService offlineMessageService,
            IContactService contactService,
            IChatService chatService,
            IOneTimeMessageService oneTimeMessageService)
        {
            _userService = userService;
            _logger = logger;
            _steganographyService = steganographyService;
            _keyService = keyService;
            _offlineMessageService = offlineMessageService;
            _contactService = contactService;
            _chatService = chatService;
            _oneTimeMessageService = oneTimeMessageService;
        }

       public async Task SendMessage(string senderId, int chatId, string? receiverId, string encryptedMessageData,
           object  encryptedAESKey,  string iv, DateTime timestamp, bool isOneTime)
        {
            try
            {
                _logger.LogInformation($"SendMessage invoked by senderId: {senderId}, chatId: {chatId}, receiverId: {receiverId}");

                string dynamicKey = GenerateDynamicKey(senderId, receiverId ?? chatId.ToString());
                _logger.LogInformation($"SendMessage invoked by senderId: {dynamicKey}");
                byte[] image = _steganographyService.GetRandomImage();
                
                if (isOneTime)
                {
                    var receiverUser = await _userService.GetByEmailAsync(receiverId);
                    if (receiverUser == null)
                    {
                        _logger.LogWarning($"Receiver with email {receiverId} not found.");
                        throw new ArgumentException($"Receiver with email {receiverId} not found.");
                    }

                    var receiverPublicKey = await _keyService.GetPublicKeyAsync(receiverUser.Id);
                    if (string.IsNullOrEmpty(receiverPublicKey))
                    {
                        _logger.LogError($"Public key for receiver {receiverId} is null or empty.");
                        throw new InvalidOperationException($"Public key for receiver {receiverId} is null or empty.");
                    }
                    _logger.LogInformation($"Receiver public key: {receiverPublicKey}");
                    
                    string fullMessage = $"{encryptedMessageData}|" +
                                         $"{timestamp.ToString("O")}|" +
                                         $"{encryptedAESKey}|" +
                                         $"{iv}|" +
                                         $"X7K9P2M|";
                    byte[] stegoImage = _steganographyService.EmbedMessage(image, fullMessage, dynamicKey);
                    
                    var messageId = await _oneTimeMessageService.SendMessageAsync(senderId, receiverId, chatId, stegoImage,  timestamp, false);
                    if (ConnectedUsers.TryGetValue(receiverId, out var receiver))
                    {
                        await Clients.Client(receiver.ConnectionId).SendAsync("ReceiveOneTimeMessage", messageId, chatId, senderId, timestamp);
                        _logger.LogInformation($"One-time message sent to receiverId: {receiverId}");
                    }
                    else
                    {
                        _logger.LogInformation($"One-time message saved for receiverId: {receiverId}");
                    }
                }
                else if (receiverId == null)
                {
                    var participants = await _chatService.GetParticipantsByChatIdAsync(chatId);
                    foreach (var participant in participants)
                    {
                        if (participant.Email == senderId) continue;

                        var participantUser = await _userService.GetByEmailAsync(participant.Email);
                        if (participantUser == null)
                        {
                            _logger.LogWarning($"Participant with email {participant.Email} not found in chat {chatId}. Skipping.");
                            continue;
                        }

                        string encryptedAESKeyJson = System.Text.Json.JsonSerializer.Serialize(encryptedAESKey);

                        string fullMessage = $"{encryptedMessageData}|" +
                                             $"{timestamp.ToString("O")}|" +
                                             $"{encryptedAESKeyJson}|" +
                                             $"{iv}|" +
                                             $"X7K9P2M|";
                        byte[] stegoImage = _steganographyService.EmbedMessage(image, fullMessage, dynamicKey);

                        if (ConnectedUsers.TryGetValue(participant.Email, out var receiver))
                        {
                            await Clients.Client(receiver.ConnectionId).SendAsync("ReceiveGroupMessage", chatId, senderId, Convert.ToBase64String(stegoImage), timestamp);
                        }
                        else
                        {
                            var offlineMessage = new OfflineMessageDto
                            {
                                Id = Guid.NewGuid().ToString(),
                                SenderId = senderId,
                                ReceiverId = participant.Email,
                                ChatId = chatId,
                                StegoImage = stegoImage,
                                CreatedAt = timestamp,
                                Received = false,
                                IsGroup = true
                            };
                            await _offlineMessageService.SaveMessageAsync(offlineMessage);
                            _logger.LogInformation($"Offline message saved for receiverId: {participant.Email}");
                        }
                    }
                    _logger.LogInformation($"Message sent to group chatId: {chatId}");
                }
                else if (!string.IsNullOrEmpty(receiverId))
                {
                    var receiverUser = await _userService.GetByEmailAsync(receiverId);
                    if (receiverUser == null)
                    {
                        _logger.LogWarning($"Receiver with email {receiverId} not found.");
                        throw new ArgumentException($"Receiver with email {receiverId} not found.");
                    }
                    
                    string fullMessage = $"{encryptedMessageData}|" +
                                         $"{timestamp.ToString("O")}|" +
                                         $"{encryptedAESKey}|" +
                                         $"{iv}|" +
                                         $"X7K9P2M|";
                    byte[] stegoImage = _steganographyService.EmbedMessage(image, fullMessage, dynamicKey);

                    
                    if (ConnectedUsers.TryGetValue(receiverId, out var receiver))
                    {
                        await Clients.Client(receiver.ConnectionId).SendAsync("ReceiveMessage", chatId, senderId, Convert.ToBase64String(stegoImage));
                        _logger.LogInformation($"Message sent to receiverId: {receiverId}");
                    }
                    else
                    {
                        var offlineMessage = new OfflineMessageDto
                        {
                            Id = Guid.NewGuid().ToString(),
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            ChatId = chatId,
                            StegoImage = stegoImage,
                            CreatedAt = timestamp,
                            Received = false,
                            IsGroup = false
                        };
                        await _offlineMessageService.SaveMessageAsync(offlineMessage);
                        _logger.LogInformation($"Offline message saved for receiverId: {receiverId}");
                    }
                }
                else
                {
                    throw new ArgumentException("Either chatId or receiverId must be provided.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendMessage: senderId={senderId}, chatId={chatId}, receiverId={receiverId}");
                throw;
            }
        }
       
       public async Task SendFile(string senderId, int chatId, string? receiverId, string fileName, string encryptedMessageData,
           string encryptedAESKey,  string iv, string fileType, DateTime timestamp)
        {
            try
            {
                _logger.LogInformation($"SendFile invoked by senderId: {senderId}, chatId: {chatId}, receiverId: {receiverId}, fileName: {fileName}");

                var senderUser = await _userService.GetByEmailAsync(senderId);
                if (senderUser == null)
                {
                    _logger.LogWarning($"Sender with email {senderId} not found.");
                    throw new ArgumentException($"User with email {senderId} not found.");
                }

                if (!string.IsNullOrEmpty(receiverId))
                {
                    var receiverUser = await _userService.GetByEmailAsync(receiverId);
                    if (receiverUser == null)
                    {
                        _logger.LogWarning($"Receiver with email {receiverId} not found.");
                        throw new ArgumentException($"User with email {receiverId} not found.");
                    }

                    if (ConnectedUsers.TryGetValue(receiverId, out var receiver))
                    {
                        await Clients.Client(receiver.ConnectionId).SendAsync(
                            "ReceiveFile",
                            chatId,
                            senderId,
                            fileName,
                            fileType,
                            encryptedMessageData,
                            encryptedAESKey,
                            iv,
                            timestamp
                        );
                        _logger.LogInformation($"File sent to receiverId: {receiverId}");
                    }
                    else
                    {
                        var offlineFileMessage = new OfflineMessageFileDto
                        {
                            Id = Guid.NewGuid().ToString(),
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            ChatId = chatId,
                            FileData = encryptedMessageData,
                            FileName = fileName,
                            FileType = fileType,
                            EncryptedAESKey = encryptedAESKey,
                            AESIV = iv,
                            CreatedAt = timestamp,
                            Received = false,
                            IsGroup = false,
                            IsFile = true
                        };
                        await _offlineMessageService.SaveMessageAsync(offlineFileMessage);
                        _logger.LogInformation($"Offline file saved for receiverId: {receiverId}");
                    }
                }
                else
                {
                    var participants = await _chatService.GetParticipantsByChatIdAsync(chatId);
                    foreach (var participant in participants)
                    {
                        if (participant.Email == senderId) continue;

                        var participantUser = await _userService.GetByEmailAsync(participant.Email);
                        if (participantUser == null)
                        {
                            _logger.LogWarning($"Participant with email {participant.Email} not found in chat {chatId}. Skipping.");
                            continue;
                        }

                        if (ConnectedUsers.TryGetValue(participant.Email, out var receiver))
                        {
                            await Clients.Client(receiver.ConnectionId).SendAsync(
                                "ReceiveFile",
                                chatId,
                                senderId,
                                fileName,
                                fileType,
                                encryptedMessageData,
                                encryptedAESKey,
                                iv,
                                timestamp
                            );
                            _logger.LogInformation($"File sent to participant: {participant.Email}");
                        }
                        else
                        {
                            var offlineFileMessage = new OfflineMessageFileDto
                            {
                                Id = Guid.NewGuid().ToString(),
                                SenderId = senderId,
                                ReceiverId = participant.Email,
                                ChatId = chatId,
                                FileData = encryptedMessageData,
                                FileName = fileName,
                                FileType = fileType,
                                EncryptedAESKey = encryptedAESKey,
                                AESIV = iv,
                                CreatedAt = timestamp,
                                Received = false,
                                IsGroup = true,
                                IsFile = true
                            };
                            await _offlineMessageService.SaveMessageAsync(offlineFileMessage);
                            _logger.LogInformation($"Offline file saved for participant: {participant.Email}");
                        }
                    }
                    _logger.LogInformation($"File sent to group chatId: {chatId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendFile: senderId={senderId}, chatId={chatId}, receiverId={receiverId}");
                throw;
            }
        }

       private string GenerateDynamicKey(string senderId, string receiverIdOrChatId)
       {
           using (SHA256 sha256 = SHA256.Create())
           {
               string input = $"{senderId}{receiverIdOrChatId}";
               byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
               return Convert.ToBase64String(hash);
           }
       }

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var userId = userIdClaim?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User is not authenticated.");
                throw new InvalidOperationException("User is not authenticated.");
            }

            var userEntity = await _userService.GetByEmailAsync(userId);

            ConnectedUsers.AddOrUpdate(userId,
                new ConnectedUser
                {
                    UserId = userId,
                    ConnectionId = Context.ConnectionId
                },
                (key, existingUser) =>
                {
                    existingUser.ConnectionId = Context.ConnectionId;
                    return existingUser;
                });

            await _userService.UpdateUserStatusAsync(userEntity.Id, "true");
            
            var contacts = await _contactService.GetAllContacts(userEntity.Id);
            
            var onlineContactEmails = new List<string>();
            foreach (var contact in contacts)
            {
                var contactUser = await _userService.GetByIdAsync(contact.ContactUserId);
                if (contactUser != null)
                {
                    if (ConnectedUsers.ContainsKey(contactUser.Email))
                    {
                        onlineContactEmails.Add(contactUser.Email);
                    }
                }
            }

            foreach (var contactEmail in onlineContactEmails)
            {
                if (ConnectedUsers.TryGetValue(contactEmail, out var contact))
                {
                    await Clients.Client(contact.ConnectionId).SendAsync("UserOnline", userId);
                    _logger.LogInformation($"Notified contact {contactEmail} that {userId} is online.");
                }
            }

            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveOnlineContacts", onlineContactEmails);
            _logger.LogInformation($"Sent online contacts list to {userId}: {string.Join(", ", onlineContactEmails)}");
            
            await SendPendingMessages(userId);
            await SendOneTimeMessages(userId);
            await SendPendingFriendRequest(userId);

            _logger.LogInformation($"User connected: {userId}, ConnectionId: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        private async Task SendPendingFriendRequest(string userId)
        {
            try
            {
                var user = await _userService.GetByEmailAsync(userId);
                var requests = await _contactService.GetFriendRequests(user.Id);
                foreach (var request in requests)
                {
                    var friendUser = _userService.GetByIdAsync(request.UserId);
                    await Clients.Client(Context.ConnectionId).SendAsync("ReceiveFriendRequest", friendUser.Result.Email);
                }

                if (requests.Any())
                {
                    _logger.LogInformation($"Sent {requests.Count()} pending friend requests to user: {userId}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        private async Task SendPendingMessages(string userId)
        {
            try
            {
                var pendingMessages = await _offlineMessageService.GetPendingMessagesAsync(userId);
                foreach (var message in pendingMessages)
                {
                    if (!message.Received)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync(
                            "ReceivePendingMessage",
                            message.SenderId,
                            message.ChatId,
                            Convert.ToBase64String(message.StegoImage),
                            message.Id,
                            message.CreatedAt,
                            message.IsGroup
                        );
                    }
                }

                if (pendingMessages.Any())
                {
                    _logger.LogInformation($"Sent {pendingMessages.Count()} pending messages to user: {userId}");
                }

                var pendingFileMessages = await _offlineMessageService.GetPendingFileMessagesAsync(userId);
                foreach (var fileMessage in pendingFileMessages)
                {
                    if (!fileMessage.Received)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync(
                            "ReceivePendingFile",
                            fileMessage.SenderId,
                            fileMessage.ChatId,
                            fileMessage.FileName,
                            fileMessage.FileType,
                            fileMessage.FileData,
                            fileMessage.EncryptedAESKey,
                            fileMessage.AESIV,
                            fileMessage.Id,
                            fileMessage.CreatedAt,
                            fileMessage.IsGroup
                        );
                    }
                }

                if (pendingFileMessages.Any())
                {
                    _logger.LogInformation($"Sent {pendingFileMessages.Count()} pending file messages to user: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending pending messages to user: {userId}");
            }
        }

        private async Task SendOneTimeMessages(string userId)
        {
            try
            {
                var oneTimeMessages = await _oneTimeMessageService.GetMessagesByReceiverIdAsync(userId);

                foreach (var message in oneTimeMessages)
                {
                    if (!message.Received)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync(
                            "ReceiveOneTimePendingMessage",
                            message.SenderId,
                            message.ChatId,
                            message.Id,
                            message.CreatedAt
                        );
                    }
                }

                if (oneTimeMessages.Any())
                {
                    _logger.LogInformation($"Sent {oneTimeMessages.Count()} one time pending messages to user: {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending one time pending messages to user: {userId}");
            }
        }

        public async Task ReadOneTimeMessage(string messageId)
        {
            var message = await _oneTimeMessageService.GetMessageByIdAsync(messageId);
    
            if (message == null)
            {
                _logger.LogError( $"Error sending full one time pending messages to user");
                return;
            }

            if (!message.Read)
            {
                await Clients.Client(Context.ConnectionId).SendAsync(
                    "ReceiveFullOneTimePendingMessage",
                    message.SenderId,
                    message.ChatId,
                    Convert.ToBase64String(message.StegoImage),
                    message.MessageLength,
                    message.Id,
                    message.CreatedAt
                );

                await _oneTimeMessageService.DeleteMessageAsync(messageId);
            }
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var userId = userIdClaim?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUsers.TryRemove(userId, out _);
                var userEntity = await _userService.GetByEmailAsync(userId);
                await _userService.UpdateUserStatusAsync(userEntity.Id, "false");
                var contacts = await _contactService.GetAllContacts(userEntity.Id);

                var onlineContactEmails = new List<string>();
                foreach (var contact in contacts)
                {
                    var contactUser = await _userService.GetByIdAsync(contact.ContactUserId);
                    if (contactUser != null && ConnectedUsers.ContainsKey(contactUser.Email))
                    {
                        onlineContactEmails.Add(contactUser.Email);
                    }
                }

                foreach (var contactEmail in onlineContactEmails)
                {
                    if (ConnectedUsers.TryGetValue(contactEmail, out var contact))
                    {
                        await Clients.Client(contact.ConnectionId).SendAsync("UserOffline", userId);
                        _logger.LogInformation($"Notified contact {contactEmail} that {userId} is offline.");
                    }
                }

                _logger.LogInformation($"User disconnected: {userId}, ConnectionId: {Context.ConnectionId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task SendFriendRequest(string userId, string contactEmail)
        {
            try 
            {
                var contact = await _userService.GetByEmailAsync(contactEmail);
                if (contact == null)
                {
                    throw new Exception($"User with email {contactEmail} not found.");
                }

                var user = await _userService.GetByEmailAsync(userId);
                if (user == null)
                {
                    throw new Exception($"User with email {userId} not found.");
                }

                await _contactService.SendFriendRequest(user.Id, contact.Id);
                _logger.LogInformation($"Friend request sent from userId: {userId} to contactEmail: {contactEmail}");

                if (ConnectedUsers.TryGetValue(contactEmail, out var contactUser))
                {
                    await Clients.Client(contactUser.ConnectionId).SendAsync("ReceiveFriendRequest", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending friend request from {userId} to {contactEmail}");
                throw;
            }
        }

        public async Task ConfirmFriendRequest(string userId, string contactUserId)
        {
            var user = await  _userService.GetByEmailAsync(userId);
            var contact = await  _userService.GetByEmailAsync(contactUserId);
            await _contactService.ConfirmFriendRequest(contact.Id, user.Id);
            _logger.LogInformation($"Friend request confirmed between userId: {userId} and contactUserId: {contact.Email}");

            if (ConnectedUsers.TryGetValue(contactUserId, out var contactUser))
            {
                await Clients.Client(contactUser.ConnectionId).SendAsync("FriendRequestConfirmed", userId);
            }
        }

        public async Task RejectFriendRequest(string userId, string contactUserId)
        {
            var user = await  _userService.GetByEmailAsync(userId);
            var contact = await  _userService.GetByEmailAsync(contactUserId);
            await _contactService.RejectFriendRequest(contact.Id, user.Id);
            _logger.LogInformation($"Friend request rejected between userId: {userId} and contactUserId: {contactUserId}");

            if (ConnectedUsers.TryGetValue(contactUserId, out var contactUser))
            {
                await Clients.Client(contactUser.ConnectionId).SendAsync("FriendRequestRejected", userId);
            }
        }

        public class ConnectedUser
        {
            public string UserId { get; set; }
            public string ConnectionId { get; set; }
        }
    }
}

