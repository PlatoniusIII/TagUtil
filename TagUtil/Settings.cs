using System;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace XMLsetting
{
    /// <summary>
    /// List of Application Settings
    /// </summary>
    [Serializable()]
    public class TagUtilSettingsClass
    {
        //public List<CameraSettings> CameraList;
        public string directoryRenameScheme;
        public string currentDirectory;
        public string discogsKey;
        public string discogsSecret;
        public string discogsToken;

        // Default Values are loaded
        public void InitDefaults()
        {
            directoryRenameScheme = "..\\%isrc% %albumartist% - %album% - %year% (%bitratetype%)";
            currentDirectory = "H:\\Music\\Archived\\Drum & Bass\\Labels\\[0-9]\\3rd Party\\"; ;// "c:\\";
            discogsKey = "CustomerKey";
            discogsSecret = "CustomerSecret";
            discogsToken = "CustomerToken";
        }
    }

    [Serializable()]
    public class AppSettings
    {
        public TagUtil.Crypto cryptoEngine;

        public AppSettings()
        {
            cryptoEngine = new TagUtil.Crypto(TagUtil.Crypto.CryptoTypes.encTypeDES);
        }

        public void InitDefault()
        {
            TagUtilSettings = new TagUtilSettingsClass();
            TagUtilSettings.InitDefaults();
        }

        private static string SettingsFile = "TagUtil.xml";  // default name of settings file

        public TagUtilSettingsClass TagUtilSettings;

//        public static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Read settings from given settings file
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
        // Write settings to same settings file as they were read from
        public void SaveSettings()
        {
            SaveSettings(SettingsFile);
        }
        // Write settings to given settings file
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
