using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TranscriptionService.SignalR
{
    public class SignalRHub: Hub
    {
        public async Task ReceiveTranscription(string user, List<int> message)
        {
            await Task.Factory.StartNew(new Action(() =>
            {
                Console.WriteLine($"Message received from client: {message}");
            }));
        }
    }
}
