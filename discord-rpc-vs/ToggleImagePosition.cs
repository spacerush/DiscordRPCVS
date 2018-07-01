using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace discord_rpc_vs
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ToggleImagePosition
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4133;

        /// <summary
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8b0ef413-de58-42e0-aa72-1dffd0b4c664");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleImagePosition"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToggleImagePosition(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += ToggleImagePosition_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ToggleImagePosition Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ToggleImagePosition(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Turn the config variable off/on
            DiscordRPCVSPackage.Config.DisplayFileTypeAsLargeImage = !DiscordRPCVSPackage.Config.DisplayFileTypeAsLargeImage;
            DiscordRPCVSPackage.Config.Save();
        }

        private void ToggleImagePosition_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Checked = DiscordRPCVSPackage.Config.DisplayFileTypeAsLargeImage;
                menuCommand.Visible = true;
            }
        }
    }
}
