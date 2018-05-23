using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace discord_rpc_vs.Config
{
    internal class Configuration
    {

        /// <summary>
        ///     Dictates whether or not to enable presence
        /// </summary>
        public bool PresenceEnabled { get; set; } = true;

        /// <summary>
        ///     Dictates whether or not to display the file name you are working on
        /// </summary>
        public bool DisplayFileName { get; set; } = true;

        /// <summary>
        ///     Dictates whether or not to display the current project you are working on.
        /// </summary>
        public bool DisplayProject { get; set; } = true;

        /// <summary>
        ///     Dictates whether or not to display how long you have been working on a file for.
        /// </summary>
        public bool DisplayTimestamp { get; set; } = true;

        /// <summary>
        ///     Dictates whether or not to reset the timestamp
        /// </summary>
        public bool ResetTimestamp { get; set; } = true;

        /// <summary>
        ///     Deserializes the config into a Config object
        /// </summary>
        /// <returns></returns>
        internal static Configuration Deserialize()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/discord_rpc_vs/config.json";

            // If the file doesn't exist, make it.
            if (!File.Exists(path))
            {
                var config = new Configuration();
                config.PresenceEnabled = true;
                config.DisplayTimestamp = true;
                config.DisplayFileName = true;
                config.DisplayProject = true;
                config.ResetTimestamp = true;
                config.Save();
                return config;
            }

            Configuration returnConfig;

            // Deserialize it if it already exists.
            using (var file = File.OpenText(path))
            {
                var serializer = new JsonSerializer();
                returnConfig = (Configuration) serializer.Deserialize(file, typeof(Configuration));
            }

            returnConfig.Save();

            return returnConfig;
        }

        /// <summary>
        ///     Saves the instance of the config JSON file 
        /// </summary>
        internal void Save()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/discord_rpc_vs/";
            Directory.CreateDirectory(dir);

            var path = dir + "/config.json";

            // Save
            using (var sw = new StreamWriter(path))
            {
                var output = JsonConvert.SerializeObject(this, Formatting.Indented);
                sw.Write(output);
            }
        }
    }
}
