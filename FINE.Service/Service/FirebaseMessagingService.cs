using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public interface IFirebaseMessagingService
    {
        void SendToToken(string token, Notification notification, Dictionary<string, string> data);
        void SendToDevices(List<string> tokens, Notification notification, Dictionary<string, string> data);
        void Subcribe(IReadOnlyList<string> tokens, string topic);
        Task<bool> ValidToken(string fcmToken);
        void Unsubcribe(IReadOnlyList<string> tokens, string topic);
    }
    public class FirebaseMessagingService : IFirebaseMessagingService
    {
        private readonly static FirebaseMessaging _fm = FirebaseMessaging.DefaultInstance;

        public void SendToToken(string token, Notification notification, Dictionary<string, string> data)
        {
            // See documentation on defining a message payload.
            var message = _fm.SendAsync(new Message()
            {
                Token = token,
                Data = data,
                Notification = notification
            });            
        }
        public async void SendToDevices(List<string> tokens, Notification notification, Dictionary<string, string> data)
        {
            var message = new MulticastMessage()
            {
                Tokens = tokens,
                Data = data,
                Notification = notification
            };

            var response = await _fm.SendMulticastAsync(message);
            Console.WriteLine($"{response.SuccessCount} messages were sent successfully");
        }

        public async Task<bool> ValidToken(string fcmToken)
        {
            if (fcmToken == null || fcmToken.Trim().Length == 0)
                return false;
            var result = await _fm.SendMulticastAsync(new MulticastMessage()
            {
                Tokens = new List<string>()
                {
                    fcmToken
                },

            }, true);

            return result.FailureCount == 0;
        }

        public async void Subcribe(IReadOnlyList<string> tokens, string topic)
        {
            var response = await _fm.SubscribeToTopicAsync(tokens, topic);
            Console.WriteLine($"Successfully subcribe users to topic '{topic}': {response.SuccessCount} sent");
        }
        public async void Unsubcribe(IReadOnlyList<string> tokens, string topic)
        {
            var response = await _fm.UnsubscribeFromTopicAsync(tokens, topic);
            Console.WriteLine($"Successfully unsubcribe users from topic '{topic}': {response.SuccessCount} sent");
        }
    }
}
