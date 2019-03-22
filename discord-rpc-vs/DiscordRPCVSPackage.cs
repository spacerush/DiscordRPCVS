using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using discord_rpc_vs.Properties;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace discord_rpc_vs
{
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The minimum requirement for a class to be considered a valid package for Visual Studio
    ///         is to implement the IVsPackage interface and register itself with the shell.
    ///         This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///         to do it: it derives from the Package class that provides the implementation of the
    ///         IVsPackage interface and uses the registration attributes defined in the framework to
    ///         register itself and its components with the shell. These attributes tell the pkgdef creation
    ///         utility what data to put into .pkgdef file.
    ///     </para>
    ///     <para>
    ///         To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...
    ///         &gt; in .vsixmanifest file.
    ///     </para>
    /// </remarks>
    [PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class DiscordRPCVSPackage : AsyncPackage
    {
        /// <summary>
        ///     DiscordRPCVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ea99cd90-97ea-40a5-be3c-2f3377242800";

        /// <summary>
        ///     Dictionary in which the key is the file extension, and the value is the
        ///     image key for RPC
        /// </summary>
        private readonly Dictionary<string, string> _languages = new Dictionary<string, string>
        {
            { ".cs", "c-sharp" },
            { ".cpp", "cpp" },
            { ".py", "python" },
            { ".js", "javascript" },
            { ".html", "html" },
            { ".css", "css" },
            { ".java", "java" },
            { ".go", "go" },
            { ".php", "php" },
            { ".c", "c-clang" },
            { ".h", "h-clang" },
            { ".class", "C-java" }
        };

        /// <summary>
        ///     DTE
        /// </summary>
        private DTE _dte;

        private Events _dteEvents;

        /// <summary>
        ///     Discord Controller instance
        /// </summary>
        internal static DiscordController DiscordController { get; } = new DiscordController();

        /// <summary>
        ///     Keeps track of if we have already initialized the timestamp
        /// </summary>
        private bool InitializedTimestamp { get; set; }

        /// <summary>
        ///     The initial timestamp
        /// </summary>
        private DateTime? InitialTimestamp { get; set; }

        /// <summary>
        ///     Global configuration
        /// </summary>
        internal static Settings Settings => Settings.Default;

        #region Package Members

        /// <inheritdoc />
        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                // Switches to the UI thread in order to consume some services used in command initialization
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Query service asynchronously from the UI thread
                _dte = await GetServiceAsync(typeof(SDTE)) as DTE;
                Assumes.Present(_dte);
                _dteEvents = _dte.Events;
                _dteEvents.WindowEvents.WindowActivated += OnWindowSwitch;

                if (Settings.IsPresenceEnabled)
                {
                    DiscordController.Initialize();
                }

                PresenceCommand.Initialize(this);
                await base.InitializeAsync(cancellationToken, progress);
            }
            catch (Exception)
            {
                //ignored
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///     When switching between windows
        /// </summary>
        /// <param name="windowActivated"></param>
        /// <param name="lastWindow"></param>
        private async void OnWindowSwitch(Window windowActivated, Window lastWindow)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

                // Get Extension
                string ext = "";

                if (windowActivated.Document != null)
                {
                    ext = Path.GetExtension(windowActivated.Document.FullName);
                }

                DiscordRPC.Assets assets = null;
                string details = string.Empty;
                string state = string.Empty;
                DateTime? startTimestamp = null;
                if (Settings.IsLanguageImageLarge)
                {
                    assets = new DiscordRPC.Assets()
                    {
                        LargeImageKey = _languages.ContainsKey(ext) ? _languages[ext] : "visualstudio",
                        LargeImageText = _languages.ContainsKey(ext) ? _languages[ext] : "",
                        SmallImageKey = "visualstudio",
                        SmallImageText = "Visual Studio 2019"
                    };
                }
                else
                {
                    assets = new DiscordRPC.Assets()
                    {
                        LargeImageKey = "visualstudio",
                        LargeImageText = "Visual Studio 2019",
                        SmallImageKey = _languages.ContainsKey(ext) ? _languages[ext] : "visualstudio",
                        SmallImageText = _languages.ContainsKey(ext) ? _languages[ext] : ""
                    };
                }

                // Add things to the presence based on config.
                if (Settings.IsFileNameShown && windowActivated.Document != null)
                    details = Path.GetFileName(GetExactPathName(windowActivated.Document.FullName));

                if (Settings.IsSolutionNameShown && _dte.Solution != null)
                    state = "Developing " + Path.GetFileNameWithoutExtension(_dte.Solution.FileName);

                // Initialize timestamp
                if (Settings.IsTimestampShown && !InitializedTimestamp)
                {
                    startTimestamp = DateTime.UtcNow;
                    InitialTimestamp = startTimestamp;
                    InitializedTimestamp = true;
                }

                // Reset it
                if (Settings.IsTimestampResetEnabled && InitializedTimestamp && Settings.IsTimestampShown)
                    startTimestamp = DateTime.UtcNow;
                // Set it equal to the initial timestamp (To not reset)
                else if (Settings.IsTimestampShown && !Settings.IsTimestampResetEnabled)
                    startTimestamp = InitialTimestamp;

                if (Settings.IsPresenceEnabled)
                    DiscordController.client.SetPresence(new DiscordRPC.RichPresence()
                    {
                        Details = details,
                        State = state,
                        Timestamps = startTimestamp != null ? new DiscordRPC.Timestamps() { Start = startTimestamp } : null,
                        Assets = assets
                    });
                else
                    DiscordController.client.ClearPresence();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        ///     Gets path name with correct casing
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
            {
                return pathName;
            }

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }

            return di.Name.ToUpper();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Called to ask the package if the shell can be closed. By default this method returns canClose as true and S_OK.
        /// </summary>
        /// <param name="canClose">Returns true if the shell can be closed, otherwise false.</param>
        /// <returns>S_OK(0) if the method succeeded, otherwise an error code.</returns>
        protected override int QueryClose(out bool canClose)
        {
            DiscordController.client.ClearPresence();
            DiscordController.client.Dispose();
            return base.QueryClose(out canClose);
        }

        #endregion
    }
}