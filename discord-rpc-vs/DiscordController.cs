using System;

namespace discord_rpc_vs
{
    internal class DiscordController
    {
        private DiscordRPC.EventHandlers _handlers;
        public string ApplicationId = "551675228691103796";
        public string OptionalSteamId = string.Empty;
        public DiscordRPC.RichPresence Presence;

        /// <summary>
        ///     Initializes Discord RPC
        /// </summary>
        public void Initialize()
        {
            _handlers = new DiscordRPC.EventHandlers
            {
                readyCallback = ReadyCallback
            };
            _handlers.disconnectedCallback += DisconnectedCallback;
            _handlers.errorCallback += ErrorCallback;
            DiscordRPC.Initialize(ApplicationId, ref _handlers, true, OptionalSteamId);
        }

        public void ReadyCallback()
        {
            Console.WriteLine("Discord RPC is ready!");
        }

        public void DisconnectedCallback(int errorCode, string message)
        {
            Console.WriteLine($"Error: {errorCode} - {message}");
        }

        public void ErrorCallback(int errorCode, string message)
        {
            Console.WriteLine($"Error: {errorCode} - {message}");
        }
    }
}