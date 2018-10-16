using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using TagLib;

/// <summary>Whole idea:
/// - be able to rename directories based on content
/// - 'repair' cue files - so also view them and highlight errors
/// </summary>
//ToDo Problems with taglib-sharp:
//ToDo TagLib: doesn't read a lot of tags from other formats than ID3V2 (for eaxmple doesn't read VORBIS_COMMENT metadata block from FLAC) See https://www.the-roberts-family.net/metadata/flac.html
//ToDo TagLib: no list of "tags available in file"
namespace TagUtil
{
    public partial class formMain : Form
    {
        //        public string directoryRenameScheme = "..\\%isrc% %albumartist% - %album% - %year% (%bitratetype%)";
        public TagLib.File currentFile = null;
        TagDetailInfoForm fileInfo;

        public TagLib.Id3v1.Tag id3v1;
        public TagLib.Id3v2.Tag id3v2;
        //        public TagLib.Mpeg4.AppleTag apple;
        //        public TagLib.Ape.Tag ape;
        //        public TagLib.Asf.Tag asf;
        public TagLib.Ogg.XiphComment ogg;
        public TagLib.Flac.Metadata flac;
        public TagLib.ByteVector Serato_Autotags_Identifier_ID3 = new TagLib.ByteVector("Serato Autotags");
        public TagLib.ByteVector Serato_BeatGrid_Identifier_ID3 = new TagLib.ByteVector("Serato BeatGrid");
        public TagLib.ByteVector Serato_Autotags_Identifier = new TagLib.ByteVector("SERATO_ANALYSIS");
        public TagLib.ByteVector Serato_BeatGrid_Identifier = new TagLib.ByteVector("SERATO_BEATGRID");
        public TagLib.ByteVector LAME_Identifier = new TagLib.ByteVector("LAME");
        string[] extensions = { ".mp3", ".wma", ".mp4", ".wav", ".flac", ".m4a" };

        public class serato_struct
        {
            public struct seratoRaw
            {
                public string Type { set; get; }
                public string Name { set; get; }
                public byte[] data;
            }

            public struct Markers
            {
                public void Init()
                {
                    Name = string.Empty;
                }
                public string Name { set; get; }
                public byte[] raw { set; get; }
            }

            public void Init()
            {
                seratoAnalysis = string.Empty;
                seratoAutogain = string.Empty;
                seratoAutotags = string.Empty;
                seratoBeatgrid = string.Empty;
                seratoMarkers = string.Empty;
                seratoOverview = string.Empty;
                seratoRelVol = string.Empty;
                seratoVideoAssoc = string.Empty;

                BPM = 0.0;
                tag2 = 0.0;
                tag3 = 0.0;
                AutoGain = 0.0;

                for (int i = 0; i < markers.Length; i++)
                {
                    markers[i].Init();
                }
            }
            public string seratoAnalysis { set; get; }
            public string seratoAutogain { set; get; }
            public string seratoAutotags { set; get; }
            public string seratoBeatgrid { set; get; }
            public string seratoMarkers { set; get; }
            public string seratoOverview { set; get; }
            public string seratoRelVol { set; get; }
            public string seratoVideoAssoc { set; get; }

            public double BPM { set; get; }
            public double tag2 { set; get; }
            public double tag3 { set; get; }
            public Markers[] markers = new Markers[8];
            public string Color { set; get; }
            public string BPMLock { set; get; }
            public double AutoGain { set; get; }
            public List<seratoRaw> dataRaw;
        }
        public serato_struct serato;

        enum bitrateType
        {
            bitrate_NONE,
            bitrate_FLAC,
            bitrate_MP3,
            bitrate_320,
            bitrate_256,
            bitrate_192,
            bitrate_160,
            bitrate_128,
            bitrate_LOW,
            bitrate_VBR
        }
        private int sortColumn = -1;

        string QualityRating = "";
//        public List<TagInfo> fileTags = new List<TagInfo>();

        XMLsetting.AppSettings appSettings;

        /// <summary>
        ///    Normalize, verify and pad string for Base64-decoding
        ///    if necessary. 
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the santized string with
        ///    padding if necessary.
        /// </value>
        /// <remarks>
        ///    Serato has the habit of ending with a null, so it's possible 
        ///    the last quartet of characters just has a single A.
        ///    I'm not sure if adding "A==" actually adds an unwanted '\0'.
        /// </remarks>
        private static string ValidateBase64EncodedString(string inputText)
        {
            string stringToValidate = inputText;
            stringToValidate = stringToValidate.Replace("\n", ""); // Would mess up regex and counts
            stringToValidate = stringToValidate.Replace('-', '+'); // 62nd char of encoding
            stringToValidate = stringToValidate.Replace('_', '/'); // 63rd char of encoding
            switch (stringToValidate.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: stringToValidate += "=="; break; // Two pad chars
                case 3: stringToValidate += "="; break; // One pad char
                    //I've seen base64 strings with an additional A at the end...
                case 1: if (stringToValidate[stringToValidate.Length - 1] == 'A')
                            stringToValidate += "A==";
                        else throw new System.Exception("Illegal base64url string!");
                    break; // No pad chars in this case
                default:
                    throw new System.Exception(
             "Illegal base64url string!");
            }

            return stringToValidate;
        }

        public static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }

        public formMain()
        {
            InitializeComponent();
            appSettings = XMLsetting.AppSettings.LoadSettings("TagUtil.xml");
            fileInfo = new TagDetailInfoForm(this);
            fileInfo.TopLevel = false;
            fileInfo.AutoScroll = true;
            TagInfoPanel.Controls.Add(fileInfo);
            fileInfo.Show();

            serato = new serato_struct();

            editDirectoryRenameScheme.Text = appSettings.TagUtilSettings.directoryRenameScheme;
            editCurrentDirectory.Text = appSettings.TagUtilSettings.currentDirectory;
        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            appSettings.TagUtilSettings.currentDirectory = editCurrentDirectory.Text;
            appSettings.SaveSettings();
        }

        //Read all tags
        public void ReadTags()
        {
            /// <summary>Function to read the various tags from file, if available
            /// These will be checked in various get functions
            /// </summary>
            //currentFile = TagLib.File.Create(FileInfoView.FocusedItem.SubItems[4].Text);
            id3v1 = currentFile.GetTag(TagLib.TagTypes.Id3v1) as TagLib.Id3v1.Tag;
            id3v2 = currentFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
//            apple = currentFile.GetTag(TagLib.TagTypes.Apple) as TagLib.Mpeg4.AppleTag;
//            ape = currentFile.GetTag(TagLib.TagTypes.Ape) as TagLib.Ape.Tag;
//            asf = currentFile.GetTag(TagLib.TagTypes.Asf) as TagLib.Asf.Tag;
            ogg = currentFile.GetTag(TagLib.TagTypes.Xiph) as TagLib.Ogg.XiphComment;
            flac = currentFile.GetTag(TagLib.TagTypes.FlacMetadata) as TagLib.Flac.Metadata;
        }

        internal bool LoadFileInfo()
        {
            /// <summary>Read all files in the specified directory, parse all files and fill in the listview
            /// </summary>
            FileInfoView.View = View.Details;
            int width = FileInfoView.Width / 6 - 1;
            FileInfoView.Columns.Add("Artist", width, HorizontalAlignment.Left);
            FileInfoView.Columns.Add("Title", width, HorizontalAlignment.Left);
            FileInfoView.Columns.Add("Album", width, HorizontalAlignment.Left);
            FileInfoView.Columns.Add("Year", width, HorizontalAlignment.Left);
            FileInfoView.Columns.Add("File", width, HorizontalAlignment.Left);
            FileInfoView.Columns.Add("Bitrate", width, HorizontalAlignment.Left);

            DataSet set = new DataSet("setTagUtil");
            DataTable tableTagUtil = new DataTable("TagUtil");
            set.Tables.Add(tableTagUtil);
            tableTagUtil.Columns.Add(new DataColumn("Artist",typeof(string)));
            tableTagUtil.Columns.Add(new DataColumn("Title", typeof(string)));
            tableTagUtil.Columns.Add(new DataColumn("Album", typeof(string)));
            tableTagUtil.Columns.Add(new DataColumn("Year", typeof(int)));
            tableTagUtil.Columns.Add(new DataColumn("File", typeof(string)));
            tableTagUtil.Columns.Add(new DataColumn("Bitrate", typeof(int)));
            //DataTable tableTitle = new DataTable("Title");
            //DataTable tableAlbum = new DataTable("Album");
            //DataTable tableYear = new DataTable("Year");
            //DataTable tableFile = new DataTable("File");
            //DataTable tableBitrate = new DataTable("Bitrate");
            //set.Tables.Add(tableArtists);
            //set.Tables.Add(tableTitle);
            //set.Tables.Add(tableAlbum);
            //set.Tables.Add(tableYear);
            //set.Tables.Add(tableFile);
            //set.Tables.Add(tableBitrate);

//            fileTags.Clear(); //Delete current list of tags

            FileInfoView.Items.Clear();

            foreach (string file in Directory.EnumerateFiles(editCurrentDirectory.Text, "*.*", SearchOption.AllDirectories)
                .Where(s => extensions.Any(ext => ext == Path.GetExtension(s))))
            {
                try
                {
                    currentFile = TagLib.File.Create(file);
                    ReadTags();
//                    FillTagInfoStruct(file);
                    //                    uint year = currentFile.Tag.Year;
                    //bool vbr = IsVBR();
                                        String[] itemStrings = { currentFile.Tag.Performers[0], currentFile.Tag.Title, currentFile.Tag.Album, currentFile.Tag.Year.ToString(), file, currentFile.Properties.AudioBitrate.ToString() };
                                        ListViewItem item = new ListViewItem(itemStrings);
                                        FileInfoView.Items.Add(item);
                                        DataRow dr = tableTagUtil.NewRow();
                    dr["Artist"] = currentFile.Tag.Performers[0];
                    dr["Title"] = currentFile.Tag.Title;
                    dr["Album"] = currentFile.Tag.Album;
                    dr["Year"] = currentFile.Tag.Year;
                    dr["File"] = file;
                    dr["Bitrate"] = currentFile.Properties.AudioBitrate;
                    tableTagUtil.Rows.Add(dr);
//                    set.Tables.Add(tableTagUtil);
                }
                catch (TagLib.CorruptFileException)
                {
                                        String[] itemStrings = { "", "", "", "", file, "" };
                                        ListViewItem item = new ListViewItem(itemStrings);
                                        FileInfoView.Items.Add(item);
                                        DataRow dr = tableTagUtil.NewRow();
                                        dr["Artist"] = "";
                                        dr["Title"] = "";
                                        dr["Album"] = "";
                                        dr["Year"] = 0;
                                        dr["File"] = "";
                                        dr["Bitrate"] = 0;
                    tableTagUtil.Rows.Add(dr);
//                    set.Tables.Add(tableTagUtil);
                }
            }

/*            foreach (TagInfo tag in fileTags)
            {
                String[] itemStrings = { tag.ID.ToString(), tag.Artist, tag.Title, tag.Album, tag.Year.ToString(), tag.file, tag.Bitrate.ToString() };
                ListViewItem item = new ListViewItem(itemStrings);
                FileInfoView.Items.Add(item);
            }
*/
//            this.FileInfoView2.DataSource = new BindingSource(set, "tagUtil");
//            this.FileInfoView2.DataSource = set.Tables["tagUtil"];
//            this.FileInfoView2.DataSource = new DataView(set.Tables["tagUtil"]);
//            this.FileInfoView2.DataMember = "tagUtil"; this.FileInfoView2.DataSource = set;
            this.FileInfoView2.DataMember = "TagUtil";
            this.FileInfoView2.DataSource = new DataViewManager(set);
            this.FileInfoView2.Sort(4);
            this.FileInfoView2.ShowGroups = false;
            this.FileInfoView2.AutoResizeColumns();
            return true;
        }

        private void FillTagInfoStruct(string file)
        {
            /// <summary>For each file retrieve all tag info and store in TagInfo list
            /// </summary>
            TagInfo currentTag = new TagInfo();
            currentTag.Tags = currentFile.TagTypesOnDisk.ToString();
            currentTag.file = file;
            currentTag.Artist = string.Join("; ", currentFile.Tag.Performers);
            currentTag.Title = currentFile.Tag.Title;
            currentTag.AlbumArtist = string.Join("; ", currentFile.Tag.AlbumArtists); //tagFile.Tag.AlbumArtists.Length == 0 ? "" : tagFile.Tag.AlbumArtists[0];
            currentTag.Album = currentFile.Tag.Album;
            currentTag.Year = currentFile.Tag.Year.ToString();

            currentTag.Disc = currentFile.Tag.Disc == 0 ? "" : currentFile.Tag.Disc.ToString();
            currentTag.DiscTotal = currentFile.Tag.DiscCount == 0 ? "" : currentFile.Tag.DiscCount.ToString();

            currentTag.Genre = string.Join("; ", currentFile.Tag.Genres);
            currentTag.Bitrate = currentFile.Properties.AudioBitrate;

            currentTag.Duration = currentFile.Properties.Duration.Hours.ToString() + ":" + currentFile.Properties.Duration.Minutes.ToString("0#") + "." + currentFile.Properties.Duration.Seconds.ToString("0#");

            currentTag.BPM = currentFile.Tag.BeatsPerMinute;

            if (id3v2 != null)
            {

                TagLib.Id3v2.TextInformationFrame trackFrame = TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TRCK", false);
                if (trackFrame != null)
                {
                    if (trackFrame.Text[0].Contains("/"))
                    {
                        int nSlash = trackFrame.Text[0].IndexOf("/");
                        currentTag.Track = trackFrame.Text[0].Substring(0, nSlash);
                        currentTag.TrackTotal = (trackFrame.Text[0].Length > nSlash + 1) ? trackFrame.Text[0].Substring(nSlash + 1) : "";
                    }
                    else
                    {
                        currentTag.Track = currentFile.Tag.Track == 0 ? "" : currentFile.Tag.Track.ToString();
                        currentTag.TrackTotal = currentFile.Tag.TrackCount == 0 ? "" : currentFile.Tag.TrackCount.ToString();
                    };

                    currentTag.Key = currentFile.Tag.InitialKey;// ReadID3V2Tag("TKEY");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TKEY", false).ToString();
                    currentTag.ISRC = ReadID3V2Tag("TSRC");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TSRC", false).ToString();
                    currentTag.Publisher = ReadID3V2Tag("TPUB");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPUB", false).ToString();
                    currentTag.RemixedBy = ReadID3V2Tag("TPE4");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPE4", false).ToString();

                }
            }

//            ContainsSeratoData(currentTag);
            IsVBR();

//            currentTag.ID = fileTags.Count();
//            fileTags.Add(currentTag);
        }

        private string ReadID3V2Tag(string tag)
        {
            /// <summary>Read specific frame from ID3V2 tag
            /// </summary>
            TagLib.Id3v2.TextInformationFrame frame = TagLib.Id3v2.TextInformationFrame.Get(id3v2, tag, false);
            if (frame == null) return "";
            return frame.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string newDirectory = ParseString();
        }

        private void FileInfoView_ShowInfo(object sender, EventArgs e)
        {

        }

        private void FileInfoView_ShowSelection(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (FileInfoView.FocusedItem != null)
            {
                try
                {
                    currentFile = TagLib.File.Create(FileInfoView.FocusedItem.SubItems[4].Text);
                    ReadTags();
                    fileInfo.UpdateInfo(FileInfoView.FocusedItem.SubItems[4].Text);
                    //                fileInfo.UpdateInfo(Int32.Parse(FileInfoView.FocusedItem.SubItems[0].Text));
                }
                catch (TagLib.CorruptFileException)
                {
                }
            }
        }

        /// <summary>
        ///    Parses the rename directory string, replacing placeholders used.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the santized string with all
        ///    placeholders replaced.
        /// </value>
        /// <remarks>
        ///    Contains some specific replacements I need.
        /// </remarks>
        /// <seealso cref="replacePlaceholder"/>
        private string ParseString()
        {
            /// <summary>We are going to rename the folder. Loop through all (music) files in the folder to determine aspects
            /// then rename
            /// </summary>
            string newDirectory = string.Empty;
            bool bPlaceholderActive = false;
            string placeHolder = string.Empty;
            QualityRating = DetermineMusicQualityRating();
            if (FileInfoView2.FocusedItem != null)
                ReadTags();
            else
                return string.Empty;

            for (int nChar = 0; nChar < appSettings.TagUtilSettings.directoryRenameScheme.Length; nChar++)
            {
                if (appSettings.TagUtilSettings.directoryRenameScheme[nChar] == '%')
                {
                    if (bPlaceholderActive)
                    {
                        bPlaceholderActive = false;
                        newDirectory += SanitizeFileName(replacePlaceholder(placeHolder));
                    }
                    else
                    {
                        bPlaceholderActive = true;
                        placeHolder = string.Empty;
                    }
                }
                else
                {

                    if (bPlaceholderActive)
                        placeHolder += appSettings.TagUtilSettings.directoryRenameScheme[nChar];
                    else
                        newDirectory += appSettings.TagUtilSettings.directoryRenameScheme[nChar];
                }
            }
            labelResultingString.Text = newDirectory;
            //Here should be a general 'remove chars that can't be used in a directory' function
            return newDirectory;
        }

        public static string SanitizeFileName(string fileName, char replacementChar = '_')
        {
            var blackList = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars());
            var output = fileName.ToCharArray();
            for (int i = 0, ln = output.Length; i < ln; i++)
            {
                if (blackList.Contains(output[i]))
                {
                    output[i] = replacementChar;
                }
            }
            return new String(output);
        }

        /// <summary>
        ///    Replaces a placeholder string with the value from the file or a
        ///    specific result.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the placeholders value.
        /// </value>
        /// <remarks>
        ///    Contains some specific replacements I need.
        /// </remarks>
        /// <seealso cref="ParseString"/>
        /// <param name="placeHolder">Placeholder string that needs to be replaced</param>
        /// <returns>This function returns a <see cref="string"/> that contains either the 
        /// value from the current file or a result based on info, like the bitrate type</returns>
        private string replacePlaceholder(string placeHolder)
        {
            string newData = string.Empty;

            switch (placeHolder)
            {
                case "isrc": newData = (currentFile.Tag.ISRC.IndexOf(',') >= 0) ? currentFile.Tag.ISRC.Substring(0, currentFile.Tag.ISRC.IndexOf(',')): currentFile.Tag.ISRC; break;
                case "albumartist": newData = currentFile.Tag.AlbumArtists[0]; break;
                case "album": newData = currentFile.Tag.Album; break;
                case "year": newData = currentFile.Tag.Year.ToString(); break;
                case "bitrate": newData = currentFile.Properties.AudioBitrate.ToString(); break;
                case "bitratetype": newData = GetBitrateTypeString(); break;
                case "musicqualityrating": newData = QualityRating; break;
                case "RENEW": newData = CheckOwnFiles(); break;
                default: break;
            }


            return newData;
        }

        /// <summary>
        ///    Gets a bitrate type enumeration. 
        /// </summary>
        /// <value>
        ///    A <see cref="bitrateType" /> containing filetype or bitrate.
        /// </value>
        /// <remarks>
        ///    Contains some specific replacements I need.
        /// </remarks>
        /// <seealso cref="bitrateType"/>
        /// <completionlist cref="bitrateType"/>
        /// <returns>This function returns a <see cref="bitrateType"/> that contains either the 
        /// type of file, or (for MP3) either the bitrate or if it's VBR</returns>
        private bitrateType GetBitrateType()
        {
            /// <summary>Returns enum of bitrate type
            /// </summary>
            bitrateType typeBitrate = bitrateType.bitrate_NONE;
            if (currentFile.MimeType == "taglib/flac") typeBitrate = bitrateType.bitrate_FLAC;
            if (currentFile.MimeType == "taglib/mp3")
            {
                if (IsVBR())
                    typeBitrate = bitrateType.bitrate_VBR; 
                else
                {
                    switch (currentFile.Properties.AudioBitrate)
                    {
                        case 320: typeBitrate = bitrateType.bitrate_320; break;
                        case 256: typeBitrate = bitrateType.bitrate_256; break;
                        case 192: typeBitrate = bitrateType.bitrate_192; break;
                        case 160: typeBitrate = bitrateType.bitrate_160; break;
                        case 128: typeBitrate = bitrateType.bitrate_128; break;
                        default: if( currentFile.Properties.AudioBitrate < 128 ) typeBitrate = bitrateType.bitrate_LOW; 
                            else typeBitrate = bitrateType.bitrate_NONE;
                            break;
                    }
                }
            }
            return typeBitrate;
        }

        private string GetBitrateTypeString( bitrateType type = bitrateType.bitrate_NONE )
        {
            /// <summary>Returns string version of bitrate type enum
            /// </summary>
            string bitrateTypeString = string.Empty;
            if( type == bitrateType.bitrate_NONE ) type = GetBitrateType();
            switch (type)
            {
                case bitrateType.bitrate_NONE: bitrateTypeString = "-"; break;
                case bitrateType.bitrate_FLAC: bitrateTypeString = "FLAC"; break;
                case bitrateType.bitrate_320: bitrateTypeString = "320"; break;
                case bitrateType.bitrate_256: bitrateTypeString = "256"; break;
                case bitrateType.bitrate_192: bitrateTypeString = "192"; break;
                case bitrateType.bitrate_160: bitrateTypeString = "160"; break;
                case bitrateType.bitrate_128: bitrateTypeString = "128"; break;
                case bitrateType.bitrate_LOW: bitrateTypeString = "low"; break;
                case bitrateType.bitrate_VBR: bitrateTypeString = "VBR"; break;
                default: break;
            }

            return bitrateTypeString;
        }

        //Additional info here http://gabriel.mp3-tech.org/mp3infotag.html
        //https://www.codeproject.com/Articles/8295/MPEG-Audio-Frame-Header

        /// <summary>
        ///    Returns if Serato tags are present. 
        /// </summary>
        /// <remarks>
        ///    Info is BASE64 encoded.
        /// </remarks>
        /// <returns>A <see cref="bool"/> to show if Serato tags are present</returns>
        /// 
        /// <remarks>
        ///    <para>I want to try and decode the Serato tags that are
        ///    written to the file.</para>
        ///    <para>For now I have the following info</para>
        ///    <list type="table">
        ///       <listheader>
        ///          <term>Format</term>
        ///          <description>Info</description>
        ///       </listheader>
        ///       <item>
        ///          <term>MP3</term>
        ///          <description>Tags are stored plain</description>
        ///       </item>
        ///       <item>
        ///          <term>Flac</term>
        ///          <description>Tags are Base64 encoded</description>
        ///       </item>
        ///    </list>
        ///    
        ///    <list type="table">
        ///       <listheader>
        ///          <term>Tag</term>
        ///          <term>Type</term>
        ///          <description>Description</description>
        ///       </listheader>
        ///       <item>
        ///          <term>Serato Analysis</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Just 2 bytes or 1 short.
        ///          Seems to be 0x02 0x01.</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Autogain</term>
        ///          <term>Type</term>
        ///          <description>Calculated autogain level of the track</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Autotags</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Seems to contain at least the BPM, only on MP3</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Beatgrid</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Structure for storing the beatgrid</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Markers</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Probably old(er) format of how cues are stored. 
        ///          Though both can occur in the same file</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Markers 2</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Current format in which cue points are stored.
        ///          Data itself is base64 encoded. Contains info for 8 cue points
        ///          as well as a section COLOR and BPMLOCK.</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Offsets</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Seems to contain the audio rate (44.1KHz)</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Overview</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Unknown</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato RelVol</term>
        ///          <term>Type</term>
        ///          <description>FLAC tag for MP3's Autogain tag?</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato VideoAssoc</term>
        ///          <term>Type</term>
        ///          <description>Guess it's the option to link to a video file</description>
        ///       </item>
        ///    </list>
        /// </remarks>
        public bool ContainsSeratoData()
        {
            /// <summary>Scans the file for headers that point to Serato data
            /// Tags may differ between formats
            /// </summary>
            bool bExists = false;

            serato.Init();

            if (id3v2 != null)
            {
                foreach (TagLib.Id3v2.Frame f in id3v2.GetFrames())
                {
                    if (f is TagLib.Id3v2.AttachmentFrame)
                    {
                        TagLib.Id3v2.AttachmentFrame tagSerato = ((TagLib.Id3v2.AttachmentFrame)f);
                        if (tagSerato.Description == "Serato Analysis")
                        {
                            serato.seratoAnalysis = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Autotags")
                        {
                            serato.seratoAutotags = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Autogain")
                        {
                            serato.seratoAutogain = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato BeatGrid")
                        {
                            serato.seratoBeatgrid = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Markers_")
                        {
                            serato.seratoMarkers = tagSerato.Data.ToString();
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Markers2")
                        {
                            serato.seratoMarkers = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Overview")
                        {
                            serato.seratoOverview = Encoding.UTF8.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                    }
                }
            }
            if (ogg != null)
            {
                serato.seratoAnalysis = DecodeSeratoFlac("SERATO_ANALYSIS");
                serato.seratoMarkers = DecodeSeratoFlac("SERATO_MARKERS_V2");
                serato.seratoAutogain = DecodeSeratoFlac("SERATO_AUTOGAIN");
                serato.seratoBeatgrid = BitConverter.ToString(Encoding.Default.GetBytes(DecodeSeratoFlac("SERATO_BEATGRID")));
                serato.seratoOverview = BitConverter.ToString(Encoding.Default.GetBytes(DecodeSeratoFlac("SERATO_OVERVIEW")));
                serato.seratoRelVol = BitConverter.ToString(Encoding.Default.GetBytes(DecodeSeratoFlac("SERATO_RELVOL")));
                serato.seratoVideoAssoc = BitConverter.ToString(Encoding.Default.GetBytes(DecodeSeratoFlac("SERATO_VIDEO_ASSOC")));
                if (serato.seratoOverview.Length > 0 ||
                    serato.seratoAnalysis.Length > 0 ||
                    serato.seratoMarkers.Length > 0 ) bExists = true;
            }


            // See https://stackoverflow.com/questions/41850029/string-parsing-techniques for parsing info

            if (serato.seratoAutotags.Length > 0)
            {
                double temp;
                string[] words = serato.seratoAutotags.Substring(2).Split('\0');
                double.TryParse(words[0], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato.BPM = temp;// Convert.ToDouble(serato.seratoAutotags.Substring(2, serato.seratoAutotags.IndexOf('\0', 2) - 1));
                double.TryParse(words[1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato.tag2 = temp;
                double.TryParse(words[2], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato.tag3 = temp;
            }
            foreach (var item in serato.dataRaw)
            {
                if( item.Name == "Serato Markers")
                {

                }
            }
            if (serato.seratoMarkers.Length > 0)
            {
                string result = string.Empty;
                string[] words = serato.seratoMarkers.Substring(2).Split('\0');
                result += Encoding.UTF8.GetString(Convert.FromBase64String(ValidateBase64EncodedString(words[0])));
                if (result.Length > 0)
                {
                    int nFoundPos = -1; //Skip 2 \u0001 start bytes used as separator
                    int nStringPos = 2;
                    if (result.Contains("CUE")) //We have 1 or more CUEs
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int nEnd = -1;
                            int nStringStart = -1;
                            nFoundPos = result.IndexOf("CUE", nStringPos);
                            if (nFoundPos == -1) break;
                            //if (nFoundPos + 20 > result.Length) break;
                            //Check with less than 8 cues
                            //For some reason all CUEs are 29 bytes - except cue 2 is 28 bytes...
                            //Is able to find "BPMLOCK" if last marker doesn't have a name?
                            nEnd = result.IndexOf("\0", nFoundPos + 20);
                            if (nEnd == -1) break;
                            serato.markers[i].raw = Encoding.ASCII.GetBytes(result.Substring(nFoundPos, nEnd - nFoundPos));
                            nStringStart = result.LastIndexOf("\0", nEnd - 1);
                            serato.markers[i].Name = result.Substring(nStringStart + 1, nEnd - nStringStart - 1);
                            nStringPos = nEnd + 1;
                            Debug.WriteLine("Marker {0} length {1}", i, serato.markers[i].raw.Length);
                        }
                    }
                    //Check for COLOR and BPMLOCK
                    nFoundPos = result.IndexOf("COLOR");
                    if (nFoundPos > -1) //We have COLOR info
                    {
                        serato.Color = result.Substring(nFoundPos);
                    }
                    nFoundPos = result.IndexOf("BPMLOCK"); //We have BPMLOCK info
                    if (nFoundPos > -1)
                    {
                        serato.BPMLock = result.Substring(nFoundPos);
                    }
                }
            }
            if (serato.seratoAutogain.Length > 0)
            {
                double temp = 0.0;
                double.TryParse(serato.seratoAutogain.Substring(2), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato.AutoGain = temp;
            }

                return bExists;
        }

        /// <summary>
        ///   Help function to retrieve and base64-decode field. 
        /// </summary>
        /// <remarks>
        ///    Seems to fault on some base64 strings.
        /// </remarks>
        /// <returns>A <see cref="string"/> containing the info,
        /// stripped from type identifier and name</returns>
        private string DecodeSeratoFlac(string FieldName)
        {
            string[] seratoInput = ogg.GetField(FieldName);
            string result = string.Empty;
            formMain.serato_struct.seratoRaw newTag = new serato_struct.seratoRaw();
            byte[] test = new byte[0];
            if (seratoInput[0].Length > 0)
            {
                try
                {
                    for (int i = 0; i < seratoInput.Length; i++)
                    {
                        if( i == 1) { throw new System.Exception("Only first string should be filled"); }
                        test = Convert.FromBase64String(ValidateBase64EncodedString(seratoInput[i]));
//                        string full = Encoding.Default.GetString(test);
//                        int end_of_string = full.IndexOf("\0");
//                        if( end_of_string > -1)
//                            newTag.Type = full.Substring(0, end_of_string);

                        result += Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(seratoInput[i])));
                        //                            ByteVector data = new ByteVector(Convert.FromBase64String(seratoInput[i]));
                        //                            seratoAnalysis += Encoding.UTF8.GetString(data.Data);
                        //string[] words = result.Split('\0');
                        //result = words[3]; //Seems to work for all but Video Assoc
                        //int end_of_type = result.IndexOf('\0', 0); //@ null-characters after type
                        //int end_of_header = result.IndexOf('\0', end_of_type+2);
                        //result = result.Substring(end_of_header + 1, result.Length - end_of_header - 2);
                    }
                    string temp = string.Empty;
                    newTag.data = new byte[0];
                    for (int i = 0; i < test.Length; i++)
                    {
                        if (string.IsNullOrEmpty(newTag.Name))
                        {
                            if (test[i] != '\0')
                            {
                                temp += ((char)test[i]).ToString();
                            }
                            else
                            {
                                if (temp.Length > 0)
                                {
                                    if (string.IsNullOrEmpty(newTag.Type))
                                    {
                                        newTag.Type = temp;
                                    }
                                    else if (string.IsNullOrEmpty(newTag.Name))
                                    {
                                        newTag.Name = temp;
                                        newTag.data = new byte[test.Length - i - 1];
                                        Array.Copy(test, i + 1, newTag.data, 0, test.Length - i - 2); //-2, remove first and last \0
                                        break;
                                    }
                                }
                                temp = string.Empty;
                            }
                        }
                    }
                    serato.dataRaw.Add(newTag);
                    result = result.Substring(result.IndexOf("\0",result.IndexOf("Serato"))+1);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Base64 decode " + FieldName + " failed: " + e.Message);
                }
            }

            return result;
        }
        private bool IsVBR()
        {
            /// <summary>See if the mp3 file is encoded using VBR. Not sure what to do with ABR...
            /// </summary>
            bool bVBR = false;

            if (currentFile.MimeType != "taglib/mp3") return false;

            foreach (ICodec codec in currentFile.Properties.Codecs)
            {
                TagLib.Mpeg.AudioHeader header = (TagLib.Mpeg.AudioHeader)codec;
                //                if (header == null)
                //                    return;

                if (header.XingHeader.Present)
                {
                    currentFile.Mode = TagLib.File.AccessMode.Read;
                    long XingHeader = currentFile.Find(TagLib.Mpeg.XingHeader.FileIdentifier);
                    long Offset = TagLib.Mpeg.XingHeader.XingHeaderOffset(header.Version, header.ChannelMode);
                    TagLib.Mpeg.XingHeader xing_header = TagLib.Mpeg.XingHeader.Unknown;
                    currentFile.Seek(XingHeader);// + Offset);
                    ByteVector xing_data = currentFile.ReadBlock(16);
                    if (xing_data.Count == 16 && xing_data.StartsWith(
                        TagLib.Mpeg.XingHeader.FileIdentifier))
                        xing_header = new TagLib.Mpeg.XingHeader(xing_data);

                    int Flags = BitConverter.ToInt32(xing_data.Take(8).ToArray().Reverse().ToArray(), 0);
                    bool FramesPresent = (Flags & 0x0001) > 0;
                    bool BytesPresent = (Flags & 0x0002) > 0;
                    bool TOCPresent = (Flags & 0x0004) > 0;
                    bool QualityPresent = (Flags & 0x0008) > 0;
                    long LameHeader = currentFile.Find(LAME_Identifier);

                    if (QualityPresent)
                    {
                        //Header offset + 8 (XING + flags) + Frames, Bytes and TOC if available
                        currentFile.Seek(XingHeader + 8 + 4 * (FramesPresent ? 1 : 0 )+ 4 * (BytesPresent ? 1 : 0) + 100 * (TOCPresent ? 1 : 0));
                        ByteVector Quality_data = currentFile.ReadBlock(4);
                        int VBRQuality = BitConverter.ToInt32(Quality_data.ToArray().Reverse().ToArray(), 0);
                        bVBR = true;
                    }

//                    if (xing_header.Present)
//                        return false;
                    //                    var vector = new TagLib.ByteVector();
                    //                    TagLib.ByteVector xing = header.;
                    /* CODE HERE */
                    /*                    TagLib.File ref(fileName);
                                        TagLib.Mpeg.File *file = dynamic_cast<TagLib.Mpeg.File *>(ref file());

                                        if(!file)
                                            return;

                                        TagLib.Mpeg.Properties *properties = file->audioProperties();
                                        const TagLib::MPEG::XingHeader *xingHeader = properties->xingHeader();

                                        if(!xingHeader)
                                            return;
                    */
                }

                if (header.VBRIHeader.Present)
                {
                    /* CODE HERE */
                }
            }
            return bVBR;
        }

        //MP3GAIN_MINMAX
        //MP3GAIN_UNDO
        //REPLAYGAIN_TRACK_GAIN
        //REPLAYGAIN_TRACK_PEAK

        private string DetermineMusicQualityRating()
        {
            /// <summary>This function checks all files in a directory and returns a 'quality rating'
            /// This means the lossless type, bitrate for lossy, corrupt if there are 0-byte music files
            /// Or various if it contains files with various bitrates or for example both VBR and CBR files
            /// </summary>
            string verdict = string.Empty; //The resulting string
            //Possible reasons/results
            bool bCorruptFiles = false; //Are the corrupt files
            bool bVariousBitrates = false;
            bool bMP3 = false;
            bool bFlac = false;
            bool bVBR = false;
            bool bCBR = false;
            bitrateType typeBitrate = bitrateType.bitrate_NONE;
            int lastBitrate = 0;
            foreach (string file in Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(currentFile.Name), "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => extensions.Any(ext => ext == Path.GetExtension(s))))
            {
                try
                {
                    currentFile = TagLib.File.Create(file);
                    typeBitrate = GetBitrateType();
                    if (typeBitrate == bitrateType.bitrate_FLAC) bFlac = true;
                        //Flac is currently the last none-mp3 type listed
                    else if (typeBitrate > bitrateType.bitrate_MP3)
                    {
                        bMP3 = true;
                        if (typeBitrate == bitrateType.bitrate_VBR) bVBR = true;
                        else bCBR = true;
                        if (lastBitrate > 0)
                        {
                            if (lastBitrate != currentFile.Properties.AudioBitrate) bVariousBitrates = true;
                        }
                        if (lastBitrate == 0)
                            lastBitrate = currentFile.Properties.AudioBitrate;
                    }
                }
                catch (TagLib.CorruptFileException)
                {
                    bCorruptFiles = true;
                }
            }
            if (bCorruptFiles) { if (verdict.Length > 0) verdict += "+"; verdict += "CORRUPT"; }
            if (bVariousBitrates) { if (verdict.Length > 0) verdict += "+"; verdict += "VARIOUS BITRATES"; }
            if (bFlac && !bCBR && !bCBR) { verdict += " - FLAC"; }
            if (!bFlac && bMP3 && !bVBR && !bVariousBitrates) { verdict += " - " + GetBitrateTypeString(typeBitrate); } //Just use the last one
            if (!bFlac && !bMP3 && bVBR && !bVariousBitrates) { verdict += " - VBR"; }
            if (!bFlac && bMP3 && bVBR) { verdict += " - VBR+CBR"; }
            return verdict;
        }

        /// <summary>Add a marker if it's something I've done - may make this more generic like a certain replace or add based on what it finds
        /// </summary>
        //ToDo CheckOwnFiles - general string search?
        private string CheckOwnFiles()
        {
            return "";
        }

        private void buttonCurrentDirectory_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();

            fbd.SelectedPath = editCurrentDirectory.Text;
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                editCurrentDirectory.Text = fbd.SelectedPath;
                LoadFileInfo();
            }
        }

        private void FileInfoView_SortColumn(object sender, ColumnClickEventArgs e)
        {
            // Determine whether the column is the same as the last column clicked.
            if (e.Column != sortColumn)
            {
                // Set the sort column to the new column.
                sortColumn = e.Column;
                // Set the sort order to ascending by default.
                FileInfoView.Sorting = SortOrder.Ascending;
            }
            else
            {
                // Determine what the last sort order was and change it.
                if (FileInfoView.Sorting == SortOrder.Ascending)
                    FileInfoView.Sorting = SortOrder.Descending;
                else
                    FileInfoView.Sorting = SortOrder.Ascending;
            }

            // Call the sort method to manually sort.
            FileInfoView.Sort();
            // Set the ListViewItemSorter property to a new ListViewItemComparer
            // object.
            this.FileInfoView.ListViewItemSorter = new ListViewItemComparer(e.Column, FileInfoView.Sorting);
        }

        private void formMain_Shown(object sender, EventArgs e)
        {
            LoadFileInfo();
        }

        private void FileInfoView2_Click(object sender, EventArgs e)
        {
            if (FileInfoView2.FocusedItem != null)
            {
                try
                {
                    currentFile = TagLib.File.Create(FileInfoView2.FocusedItem.SubItems[4].Text);
                    ReadTags();
                    fileInfo.UpdateInfo(FileInfoView2.FocusedItem.SubItems[4].Text);
                    //                fileInfo.UpdateInfo(Int32.Parse(FileInfoView.FocusedItem.SubItems[0].Text));
                }
                catch (TagLib.CorruptFileException)
                {
                }
            }

        }
    }

    class ListViewItemComparer : IComparer
    {
        private int col;
        private SortOrder order;
        public ListViewItemComparer()
        {
            col = 0;
            order = SortOrder.Ascending;
        }
        public ListViewItemComparer(int column, SortOrder order)
        {
            col = column;
            this.order = order;
        }
        public int Compare(object x, object y)
        {
            int returnVal = -1;
            returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                                    ((ListViewItem)y).SubItems[col].Text);
            // Determine whether the sort order is descending.
            if (order == SortOrder.Descending)
                // Invert the value returned by String.Compare.
                returnVal *= -1;
            return returnVal;
        }
    }

}
