using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using static discord_rpc_vs.DiscordRPCVSPackage;

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

        internal const int TogglePresenceId = 0x1020;
        internal const int DisplayFileNameId = 0x1021;
        internal const int DisplaySolutionNameId = 0x1022;
        internal const int DisplayTimestampId = 0x1023;
        internal const int ResetTimestampId = 0x1024;
        internal const int ToggleImagePositionId = 0x1025;

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
                toggled += (s, e) => Settings.Save();
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
                Settings.IsPresenceEnabled ^= true;
                DisableIfNeeded();
            });

            presenceCmd.BeforeQueryStatus += (s, e) =>
            {
                if (Settings.IsPresenceEnabled)
                {
                    DiscordRPCVSPackage.DiscordController.Initialize();
                    DiscordRPC.UpdatePresence(ref DiscordRPCVSPackage.DiscordController.presence);
                }
            };

            addCommand(DisplayFileNameId, (s, e) => Settings.IsFileNameShown ^= true);
            addCommand(DisplaySolutionNameId, (s, e) => Settings.IsSolutionNameShown ^= true);
            addCommand(DisplayTimestampId, (s, e) => Settings.IsTimestampShown ^= true);

            OleMenuCommand resetTimestampCmd = addCommand(ResetTimestampId, (s, e) => Settings.IsTimestampResetEnabled ^= true);
            resetTimestampCmd.BeforeQueryStatus += (s, e) => ((OleMenuCommand)s).Visible = Settings.IsTimestampShown;

            addCommand(ToggleImagePositionId, (s, e) => Settings.IsLanguageImageLarge ^= true);
        }

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
            if (!Settings.IsPresenceEnabled)
                DiscordRPC.Shutdown();
        }

        private static bool GetSettingValue(int commandId)
        {
            switch (commandId)
            {
                case 0x1020: return Settings.IsPresenceEnabled;
                case 0x1021: return Settings.IsFileNameShown;
                case 0x1022: return Settings.IsSolutionNameShown;
                case 0x1023: return Settings.IsTimestampShown;
                case 0x1024: return Settings.IsTimestampResetEnabled;
                case 0x1025: return Settings.IsLanguageImageLarge;
                default: return false;
            }
        }
    }
}
