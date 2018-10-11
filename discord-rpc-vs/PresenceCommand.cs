using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace discord_rpc_vs
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PresenceCommand
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = Guid.Parse("8b0ef413-de58-42e0-aa72-1dffd0b4c664");

        internal const int DisplayFileNameId = 0x1021;
        internal const int DisplaySolutionNameId = 0x1022;
        internal const int DisplayTimestampId = 0x1023;
        internal const int ResetTimestampId = 0x1024;
        internal const int ToggleImagePositionId = 0x1025;
        internal const int TogglePresenceId = 0x1026;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TogglePresence"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PresenceCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (!(ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService))
                return;

            OleMenuCommand addCommand(int id, EventHandler toggled)
            {
                toggled += (s, e) => Config.Save();
                var menuCommand = new OleMenuCommand(toggled, new CommandID(CommandSet, id));

                menuCommand.BeforeQueryStatus += (s, e) =>
                {
                    if (s is OleMenuCommand command)
                        command.Checked = GetSettingValue(command.CommandID.ID);
                };

                commandService.AddCommand(menuCommand);
                return menuCommand;
            }

            OleMenuCommand presenceCmd = addCommand(TogglePresenceId, (s, e) =>
            {
                Config.PresenceEnabled ^= true;
                DisableIfNeeded();
            });

            presenceCmd.BeforeQueryStatus += (s, e) =>
            {
                if (Config.PresenceEnabled)
                {
                    DiscordRPCVSPackage.DiscordController.Initialize();
                    DiscordRPC.UpdatePresence(ref DiscordRPCVSPackage.DiscordController.presence);
                }
            };

            addCommand(DisplayFileNameId, (s, e) => Config.DisplayFileName ^= true);
            addCommand(DisplaySolutionNameId, (s, e) => Config.DisplayProject ^= true);
            addCommand(DisplayTimestampId, (s, e) => Config.DisplayTimestamp ^= true);

            OleMenuCommand resetTimestampCmd = addCommand(ResetTimestampId, (s, e) => Config.ResetTimestamp ^= true);
            resetTimestampCmd.BeforeQueryStatus += (s, e) => ((OleMenuCommand)s).Visible = Config.DisplayTimestamp;

            addCommand(ToggleImagePositionId, (s, e) => Config.DisplayFileTypeAsLargeImage ^= true);
        }

        private static Config.Configuration Config => DiscordRPCVSPackage.Config;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PresenceCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package) => Instance = new PresenceCommand(package);

        /// <summary>
        /// Disables the presence if needed.
        /// </summary>
        private static void DisableIfNeeded()
        {
            if (!Config.PresenceEnabled)
                DiscordRPC.Shutdown();
        }

        private static bool GetSettingValue(int commandId)
        {
            switch (commandId)
            {
                case 0x1021: return Config.DisplayFileName;
                case 0x1022: return Config.DisplayProject;
                case 0x1023: return Config.DisplayTimestamp;
                case 0x1024: return Config.ResetTimestamp;
                case 0x1025: return Config.DisplayFileTypeAsLargeImage;
                case 0x1026: return Config.PresenceEnabled;
                default: return false;
            }
        }
    }
}
