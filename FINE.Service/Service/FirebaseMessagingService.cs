using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FINE.Service.Service
{
    public interface IFirebaseMessagingService
    {
        void SendToTopic(string topic, Notification notification, Dictionary<string, string> data);
        void SendToTopic(string topic, Dictionary<string, string> data);
        void SendToTopicAsync(string topic, Notification notification);
        void SendToDevices(List<string> tokens, Notification notification, Dictionary<string, string> data);
        void SendToDevices(List<string> tokens, Dictionary<string, string> data);
        void SendToDevices(List<string> tokens, Notification notification);
        void Subcribe(IReadOnlyList<string> tokens, string topic);
        Task<bool> ValidToken(string fcmToken);
        void Unsubcribe(IReadOnlyList<string> tokens, string topic);
    }
    public class FirebaseMessagingService : IFirebaseMessagingService
    {
        private readonly static FirebaseMessaging _fm = FirebaseMessaging.DefaultInstance;

        public async void SendToTopic(string topic, Notification notification, Dictionary<string, string> data)
        {
            // See documentation on defining a message payload.
            var message = new Message()
            {
                Data = data,
                Notification = notification,
                Topic = topic,
            };

            // Send a message to the devices subscribed to the provided topic.
            var response = await _fm.SendAsync(message);
            Console.WriteLine($"Successfully send message to topic '{topic}': {response}");
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

        public void SendToDevices(List<string> tokens, Dictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public void SendToDevices(List<string> tokens, Notification notification)
        {
            throw new NotImplementedException();
        }


        public void SendToTopic(string topic, Dictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public async void SendToTopicAsync(string topic, Notification notification)
        {
            // See documentation on defining a message payload.
            var message = new Message()
            {
                Notification = notification,
                Topic = topic,
            };

            // Send a message to the devices subscribed to the provided topic.
            var response = await _fm.SendAsync(message);
            Console.WriteLine($"Successfully send message to topic '{topic}': {response}");
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

    }
}
