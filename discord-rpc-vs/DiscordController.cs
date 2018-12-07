using System;

namespace discord_rpc_vs
{
    internal class DiscordController
    {
        public DiscordRPC.RichPresence Presence;
        public string ApplicationId = "391385173045936131";
        public string OptionalSteamId = string.Empty;

        private DiscordRPC.EventHandlers _handlers;

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