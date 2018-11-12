using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace XMLsetting
{
    /// <summary>
    /// List of Application Settings
    /// </summary>
    [Serializable()]
    public class TagUtilSettingsClass
    {
        //public List<CameraSettings> CameraList;
        /// <summary>Name with placeholders used to rename directory</summary>
        public string directoryRenameScheme;

        /// <summary>Current directory used</summary>
        public string currentDirectory;
        /// <summary>Discogs key - if communicating using key</summary>
        public string discogsKey;
        /// <summary>Discogs secret - if communicating using key</summary>
        public string discogsSecret;
        /// <summary>Discogs token - if communicating using token</summary>
        public string discogsToken;

        /// <summary>Default Values are loaded</summary>
        public void InitDefaults()
        {
            directoryRenameScheme = "..\\%isrc% %albumartist% - %album% - %year% (%bitratetype%)";
            currentDirectory = "H:\\Music\\Archived\\Drum & Bass\\Labels\\[0-9]\\3rd Party\\"; ;// "c:\\";
            discogsKey = "CustomerKey";
            discogsSecret = "CustomerSecret";
            discogsToken = "CustomerToken";
        }
    }

    /// <summary>
    /// Class containing saved settings
    /// </summary>
    [Serializable()]
    public class AppSettings
    {
        /// <summary>
        /// Crypto engine for encoding/decoding Discogs oauth info to be stored
        /// </summary>
        public TagUtil.Crypto cryptoEngine;

        /// <summary>
        /// Defaulkt constructor
        /// </summary>
        public AppSettings()
        {
            cryptoEngine = new TagUtil.Crypto(TagUtil.Crypto.CryptoTypes.encTypeDES);
        }

        /// <summary>
        /// Set default values
        /// </summary>
        public void InitDefault()
        {
            TagUtilSettings = new TagUtilSettingsClass();
            TagUtilSettings.InitDefaults();
        }

        private static string SettingsFile = "TagUtil.xml";  // default name of settings file

        /// <summary>
        /// Link to class containing actual settings
        /// </summary>
        public TagUtilSettingsClass TagUtilSettings;

        //        public static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Read settings from given settings file
        /// </summary>
        /// <param name="settingsFile"> contains the filename to use</param>
        /// <returns>Pointer to settings class</returns>
        public static AppSettings LoadSettings(string settingsFile)
        {
            AppSettings appSettings = null;
            XmlSerializer mySerializer = new XmlSerializer(typeof(AppSettings));
            FileStream fileStream = null;
            SettingsFile = settingsFile;

            try
            {
                fileStream = new FileStream(settingsFile, FileMode.Open);
                if (fileStream.Length == 0) throw new FileNotFoundException();
            }
            catch (FileNotFoundException)
            {
                string message = string.Format("Settings file '{0}' not found, loading default values", settingsFile);
                MessageBox.Show(message, "TagUtil");

                appSettings = new AppSettings();
                appSettings.InitDefault(); // Default values

                return appSettings;
            }
            appSettings = (AppSettings)mySerializer.Deserialize(fileStream);

            // File succesfully opened, store full file path
            SettingsFile = fileStream.Name;
            appSettings.TagUtilSettings.discogsKey = appSettings.cryptoEngine.Decrypt(appSettings.TagUtilSettings.discogsKey);
            //            appSettings.TagUtilSettings.discogsSecret = appSettings.cryptoEngine.Decrypt(appSettings.TagUtilSettings.discogsSecret);
            fileStream.Close();

            return appSettings;
        }

        /// <summary>
        /// Write settings to same settings file as they were read from
        /// </summary>
        public void SaveSettings()
        {
            SaveSettings(SettingsFile);
        }

        /// <summary>
        /// Write settings to given settings file
        /// </summary>
        /// <param name="settingsFile"> contains the filename to use</param>
        public void SaveSettings(string settingsFile)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
            StreamWriter writer = new StreamWriter(settingsFile);
            try
            {
                TagUtilSettings.discogsKey = cryptoEngine.Encrypt(TagUtilSettings.discogsKey);
                TagUtilSettings.discogsSecret = cryptoEngine.Encrypt(TagUtilSettings.discogsSecret);
                serializer.Serialize(writer, this);
            }
            catch (Exception e)
            {
                //                logger.ErrorFormat("Error: opening avi file - catch: {0}", e.Message);
            }
            finally
            {
                writer.Close();
            }
        }
    }
}