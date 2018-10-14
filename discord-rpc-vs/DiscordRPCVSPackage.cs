//------------------------------------------------------------------------------
// <copyright file="DiscordRPCVSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Configuration = discord_rpc_vs.Config.Configuration;

namespace discord_rpc_vs
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(DiscordRPCVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class DiscordRPCVSPackage : Package
    {
        /// <summary>
        /// DiscordRPCVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ea99cd90-97ea-40a5-be3c-2f3377242800";

        /// <summary>
        ///     Discord Controller instance
        /// </summary>
        internal static DiscordController DiscordController { get; private set; } = new DiscordController();

        /// <summary>
        ///     Keeps track of if we have already initialized the timestamp
        /// </summary>
        private bool InitializedTimestamp { get; set; }

        /// <summary>
        ///     The initial timestamp
        /// </summary>
        private long InitialTimestamp { get; set; }

        /// <summary>
        ///     DTE
        /// </summary>
        private DTE _dte;
        private Events _dteEvents;

        /// <summary>
        ///     Dictionary in which the key is the file extension, and the value is the 
        ///     image key for RPC
        /// </summary>
        private Dictionary<string, string> Languages = new Dictionary<string, string>()
        {
            { ".cs", "c-sharp"},
            { ".cpp", "cpp" },
            { ".py" , "python"},
            { ".js", "javascript" },
            { ".html", "html" },
            { ".css", "css"},
            { ".java", "java" },
            { ".go", "go" },
            { ".php", "php" },
            { ".c", "clang" },
            { ".h", "clang" },
            { ".class", "java" },
        };

        /// <summary>
        ///     Global configuration 
        /// </summary>
        internal static Configuration Config { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscordRPCVS"/> class.
        /// </summary>
        public DiscordRPCVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            // Try to deserialize the config file, it should throw an error if it doesn't exist. 
            // in that case, we'll want to create a new instance and save it.
            Config = Configuration.Deserialize();

            _dte = (DTE)GetService(typeof(SDTE));
            _dteEvents = _dte.Events;
            _dteEvents.WindowEvents.WindowActivated += OnWindowSwitch;

            if (Config.PresenceEnabled)
            {
                DiscordController.Initialize();

                DiscordRPC.UpdatePresence(ref DiscordController.presence);
            }

            base.Initialize();
            PresenceCommand.Initialize(this);
        }

        /// <summary>
        ///     When switching between windows
        /// </summary>
        /// <param name="windowActivated"></param>
        /// <param name="lastWindow"></param>
        private void OnWindowSwitch(Window windowActivated, Window lastWindow)
        {
            // Get Extension
            var ext = "";

            if (windowActivated.Document != null)
            {
                ext = Path.GetExtension(windowActivated.Document.FullName);
            }

            // Update the RichPresence Images based on config.
            if (Config.DisplayFileTypeAsLargeImage)
            {
                DiscordController.presence = new DiscordRPC.RichPresence()
                {
                    largeImageKey = (Languages.ContainsKey(ext)) ? Languages[ext] : "smallvs",
                    largeImageText = (Languages.ContainsKey(ext)) ? Languages[ext] : "",
                    smallImageKey = "visualstudio",
                    smallImageText = "Visual Studio",
                };
            }
            else if (!Config.DisplayFileTypeAsLargeImage)
            {
                DiscordController.presence = new DiscordRPC.RichPresence()
                {
                    largeImageKey = "visualstudio",
                    largeImageText = "Visual Studio",
                    smallImageKey = (Languages.ContainsKey(ext)) ? Languages[ext] : "smallvs",
                    smallImageText = (Languages.ContainsKey(ext)) ? Languages[ext] : "",
                };
            }

            // Add things to the presence based on config.
            if (Config.DisplayFileName && windowActivated.Document != null)
                DiscordController.presence.details = Path.GetFileName(GetExactPathName(windowActivated.Document.FullName));

            if (Config.DisplayProject && _dte.Solution != null)
                DiscordController.presence.state = "Developing " + Path.GetFileNameWithoutExtension(_dte.Solution.FileName);

            // Initialize timestamp
            if (Config.DisplayTimestamp && !InitializedTimestamp)
            {
                DiscordController.presence.startTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                InitialTimestamp = DiscordController.presence.startTimestamp;
                InitializedTimestamp = true;
            }

            // Reset it
            if (Config.ResetTimestamp && InitializedTimestamp && Config.DisplayTimestamp)
                DiscordController.presence.startTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            // Set it equal to the initial timestamp (To not reset)
            else if (Config.DisplayTimestamp && !Config.ResetTimestamp)
                DiscordController.presence.startTimestamp = InitialTimestamp;

            if (Config.PresenceEnabled)
                DiscordRPC.UpdatePresence(ref DiscordController.presence);
        }

        /// <summary>
        ///     Gets path name with correct casing
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }

        /// <summary>
        /// Called to ask the package if the shell can be closed. By default this method returns canClose as true and S_OK.
        /// </summary>
        /// <param name="canClose">Returns true if the shell can be closed, otherwise false.</param>
        /// <returns>S_OK(0) if the method succeeded, otherwise an error code.</returns>
        protected override int QueryClose(out bool canClose)
        {
            DiscordRPC.Shutdown();
            return base.QueryClose(out canClose);
        }
        #endregion
    }
}