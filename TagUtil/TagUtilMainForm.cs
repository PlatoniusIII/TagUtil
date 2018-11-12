using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TagLib;

//ToDo Problems with taglib-sharp:
//ToDo TagLib: doesn't read a lot of tags from other formats than ID3V2 (for eaxmple doesn't read VORBIS_COMMENT metadata block from FLAC) See https://www.the-roberts-family.net/metadata/flac.html
//ToDo TagLib: no list of "tags available in file"
namespace TagUtil
{
    /// <remarks>Whole idea:
    /// - be able to rename directories based on content
    /// - 'repair' cue files - so also view them and highlight errors
    /// </remarks>
    public partial class MainForm : Form
    {
        //        public string directoryRenameScheme = "..\\%isrc% %albumartist% - %album% - %year% (%bitratetype%)";
        /// <summary>Active open file in the application</summary>
        public TagLib.File currentFile = null;

        private TagDetailInfoForm fileInfo;

        /// <summary>id3v1 tag</summary>
        public TagLib.Id3v1.Tag id3v1;
        /// <summary>id3v2 tag</summary>
        public TagLib.Id3v2.Tag id3v2;
        /// <summary>Apple tag</summary>
        public TagLib.Mpeg4.AppleTag apple;
        /// <summary>Ape tag</summary>
        public TagLib.Ape.Tag ape;
        /// <summary>Ogg Vorbis Xiph tag</summary>
        public TagLib.Ogg.XiphComment ogg;
        /// <summary>Flac Metadata tag</summary>
        public TagLib.Flac.Metadata flac;

        private ByteVector Serato_Autotags_Identifier_ID3 = new ByteVector("Serato Autotags");
        private ByteVector Serato_BeatGrid_Identifier_ID3 = new ByteVector("Serato BeatGrid");
        private ByteVector Serato_Autotags_Identifier = new ByteVector("SERATO_ANALYSIS");
        private ByteVector Serato_BeatGrid_Identifier = new ByteVector("SERATO_BEATGRID");
        private ByteVector LAME_Identifier = new ByteVector("LAME");
        private string[] extensions = { ".mp3", ".wma", ".mp4", ".wav", ".flac", ".m4a" };

        /// <summary>
        /// Serato struct containing all Serato info.
        /// </summary>
        public Serato serato;

        private enum BitrateType
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

        private string QualityRating = "";
        //        public List<TagInfo> fileTags = new List<TagInfo>();

        /// <summary>
        /// Link to settings class
        /// </summary>
        public XMLsetting.AppSettings appSettings = new XMLsetting.AppSettings();

        /// <summary>
        /// Main form for the application, contains the listbox
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            appSettings = XMLsetting.AppSettings.LoadSettings("TagUtil.xml");
            fileInfo = new TagDetailInfoForm(this);
            fileInfo.TopLevel = false;
            fileInfo.AutoScroll = true;
            TagInfoPanel.Controls.Add(fileInfo);
            fileInfo.Show();

            serato = new Serato(this);

            editDirectoryRenameScheme.Text = appSettings.TagUtilSettings.directoryRenameScheme;
            editCurrentDirectory.Text = appSettings.TagUtilSettings.currentDirectory;
        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            appSettings.TagUtilSettings.currentDirectory = editCurrentDirectory.Text;
            appSettings.SaveSettings();
        }

        /// <summary>Function to read the various tags from file, if available
        /// These will be checked in various get functions
        /// </summary>
        private void ReadTags()
        {
            //currentFile = TagLib.File.Create(FileInfoView.FocusedItem.SubItems[4].Text);
            id3v1 = currentFile.GetTag(TagLib.TagTypes.Id3v1) as TagLib.Id3v1.Tag;
            id3v2 = currentFile.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            apple = currentFile.GetTag(TagLib.TagTypes.Apple) as TagLib.Mpeg4.AppleTag;
            ape = currentFile.GetTag(TagLib.TagTypes.Ape) as TagLib.Ape.Tag;
            //            asf = currentFile.GetTag(TagLib.TagTypes.Asf) as TagLib.Asf.Tag;
            ogg = currentFile.GetTag(TagLib.TagTypes.Xiph) as TagLib.Ogg.XiphComment;
            flac = currentFile.GetTag(TagLib.TagTypes.FlacMetadata) as TagLib.Flac.Metadata;
        }

        /// <summary>Read all files in the specified directory, parse all files and fill in the listview
        /// </summary>
        internal bool LoadFileInfo()
        {
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
            tableTagUtil.Columns.Add(new DataColumn("Artist", typeof(string)));
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

            if (Directory.Exists(editCurrentDirectory.Text))
            {
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
                        String[] itemStrings = { string.Join(",",currentFile.Tag.Performers), currentFile.Tag.Title, currentFile.Tag.Album, currentFile.Tag.Year.ToString(), file, currentFile.Properties.AudioBitrate.ToString() };
                        ListViewItem item = new ListViewItem(itemStrings);
                        FileInfoView.Items.Add(item);
                        DataRow dr = tableTagUtil.NewRow();
                        dr["Artist"] = string.Join(",", currentFile.Tag.Performers);
                        dr["Title"] = currentFile.Tag.Title;
                        dr["Album"] = currentFile.Tag.Album;
                        dr["Year"] = currentFile.Tag.Year;
                        dr["File"] = file;
                        dr["Bitrate"] = currentFile.Properties.AudioBitrate;
                        tableTagUtil.Rows.Add(dr);
                        //                    set.Tables.Add(tableTagUtil);
                    }
                    catch (CorruptFileException) //File is probably corrupt
                    {
                        //ToDo: Add logfile
                        String[] itemStrings = { "", "", "", "", file, "" };
                        ListViewItem item = new ListViewItem(itemStrings);
                        FileInfoView.Items.Add(item);
                        DataRow dr = tableTagUtil.NewRow();
                        dr["Artist"] = "";
                        dr["Title"] = "";
                        dr["Album"] = "";
                        dr["Year"] = 0;
                        dr["File"] = file;
                        dr["Bitrate"] = 0;
                        tableTagUtil.Rows.Add(dr);
                        //                    set.Tables.Add(tableTagUtil);
                    }
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
            FileInfoView2.DataMember = "TagUtil";
            FileInfoView2.DataSource = new DataViewManager(set);
            FileInfoView2.Sort(4);
            FileInfoView2.ShowGroups = false;
            FileInfoView2.AutoResizeColumns();
            return true;
        }

        /// <summary>For each file retrieve all tag info and store in TagInfo list
        /// </summary>
        private void FillTagInfoStruct(string file)
        {
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

        /// <summary>Read specific frame from ID3V2 tag
        /// </summary>
        private string ReadID3V2Tag(string tag)
        {
            TagLib.Id3v2.TextInformationFrame frame = TagLib.Id3v2.TextInformationFrame.Get(id3v2, tag, false);
            if (frame == null) return "";
            return frame.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            appSettings.TagUtilSettings.directoryRenameScheme = editDirectoryRenameScheme.Text;
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
                    fileInfo.SetInfoToForm();
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
        ///    We are going to rename the folder. Loop through all (music) files in the folder to determine aspects 
        ///    then rename. Contains some specific replacements I need.
        /// </remarks>
        /// <seealso cref="replacePlaceholder"/>
        private string ParseString()
        {
            string newDirectory = string.Empty;
            bool bPlaceholderActive = false;
            string placeHolder = string.Empty;
            QualityRating = DetermineMusicQualityRating();
            if (FileInfoView2.FocusedItem != null)
                ReadTags();
            else
                return string.Empty;

            for (int nChar = 0; nChar < editDirectoryRenameScheme.Text.Length; nChar++)
            {
                if (editDirectoryRenameScheme.Text[nChar] == '%')
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
                        placeHolder += editDirectoryRenameScheme.Text[nChar];
                    else
                        newDirectory += editDirectoryRenameScheme.Text[nChar];
                }
            }
            labelResultingString.Text = newDirectory;
            //Here should be a general 'remove chars that can't be used in a directory' function
            return newDirectory;
        }

        /// <summary>
        /// Cleanup filename to filter out invalid characters
        /// </summary>
        /// <param name="fileName"> is the string to sanitize</param>
        /// <param name="replacementChar"> is a replacement character for invalid characters found</param>
        /// <returns></returns>
        private static string SanitizeFileName(string fileName, char replacementChar = '_')
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
                case "isrc": newData = (currentFile.Tag.ISRC.IndexOf(',') >= 0) ? currentFile.Tag.ISRC.Substring(0, currentFile.Tag.ISRC.IndexOf(',')) : currentFile.Tag.ISRC; break;
                case "isrc_no_spaces": newData = ((currentFile.Tag.ISRC.IndexOf(',') >= 0) ? currentFile.Tag.ISRC.Substring(0, currentFile.Tag.ISRC.IndexOf(',')) : currentFile.Tag.ISRC).Replace(" ", ""); break;
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
        ///    A <see cref="BitrateType" /> containing filetype or bitrate.
        /// </value>
        /// <remarks>
        ///    Contains some specific replacements I need.
        /// </remarks>
        /// <seealso cref="BitrateType"/>
        /// <completionlist cref="BitrateType"/>
        /// <returns>This function returns a <see cref="BitrateType"/> that contains either the
        /// type of file, or (for MP3) either the bitrate or if it's VBR</returns>
        private BitrateType GetBitrateType()
        {
            BitrateType typeBitrate = BitrateType.bitrate_NONE;
            if (currentFile.MimeType == "taglib/flac") typeBitrate = BitrateType.bitrate_FLAC;
            if (currentFile.MimeType == "taglib/mp3")
            {
                if (IsVBR())
                    typeBitrate = BitrateType.bitrate_VBR;
                else
                {
                    switch (currentFile.Properties.AudioBitrate)
                    {
                        case 320: typeBitrate = BitrateType.bitrate_320; break;
                        case 256: typeBitrate = BitrateType.bitrate_256; break;
                        case 192: typeBitrate = BitrateType.bitrate_192; break;
                        case 160: typeBitrate = BitrateType.bitrate_160; break;
                        case 128: typeBitrate = BitrateType.bitrate_128; break;
                        default:
                            if (currentFile.Properties.AudioBitrate < 128) typeBitrate = BitrateType.bitrate_LOW;
                            else typeBitrate = BitrateType.bitrate_NONE;
                            break;
                    }
                }
            }
            return typeBitrate;
        }

        /// <summary>Returns string version of bitrate type enum
        /// </summary>
        private string GetBitrateTypeString(BitrateType type = BitrateType.bitrate_NONE)
        {
            string bitrateTypeString = string.Empty;
            if (type == BitrateType.bitrate_NONE) type = GetBitrateType();
            switch (type)
            {
                case BitrateType.bitrate_NONE: bitrateTypeString = "-"; break;
                case BitrateType.bitrate_FLAC: bitrateTypeString = "FLAC"; break;
                case BitrateType.bitrate_320: bitrateTypeString = "320"; break;
                case BitrateType.bitrate_256: bitrateTypeString = "256"; break;
                case BitrateType.bitrate_192: bitrateTypeString = "192"; break;
                case BitrateType.bitrate_160: bitrateTypeString = "160"; break;
                case BitrateType.bitrate_128: bitrateTypeString = "128"; break;
                case BitrateType.bitrate_LOW: bitrateTypeString = "low"; break;
                case BitrateType.bitrate_VBR: bitrateTypeString = "VBR"; break;
                default: break;
            }

            return bitrateTypeString;
        }

        //Additional info here http://gabriel.mp3-tech.org/mp3infotag.html
        //https://www.codeproject.com/Articles/8295/MPEG-Audio-Frame-Header

        /// <summary>See if the mp3 file is encoded using VBR. Not sure what to do with ABR...
        /// </summary>
        private bool IsVBR()
        {
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
                        currentFile.Seek(XingHeader + 8 + 4 * (FramesPresent ? 1 : 0) + 4 * (BytesPresent ? 1 : 0) + 100 * (TOCPresent ? 1 : 0));
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

        /// <summary>
        /// Function that goes through a directory with music files to determine a rating, checking for types,
        /// bitrates and if files are corrupt.
        /// </summary>
        /// <returns>A string containing the quality rating</returns>
        /// <remarks>This function checks all files in a directory and returns a 'quality rating'
        /// This means the lossless type, bitrate for lossy, corrupt if there are 0-byte music files
        /// Or various if it contains files with various bitrates or for example both VBR and CBR files
        /// </remarks>
        private string DetermineMusicQualityRating()
        {
            string verdict = string.Empty; //The resulting string
            //Possible reasons/results
            bool bCorruptFiles = false; //Are the corrupt files
            bool bVariousBitrates = false;
            bool bMP3 = false;
            bool bFlac = false;
            bool bVBR = false;
            bool bCBR = false;
            BitrateType typeBitrate = BitrateType.bitrate_NONE;
            int lastBitrate = 0;
            foreach (string file in Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(currentFile.Name), "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => extensions.Any(ext => ext == Path.GetExtension(s))))
            {
                try
                {
                    currentFile = TagLib.File.Create(file);
                    typeBitrate = GetBitrateType();
                    if (typeBitrate == BitrateType.bitrate_FLAC) bFlac = true;
                    //Flac is currently the last none-mp3 type listed
                    else if (typeBitrate > BitrateType.bitrate_MP3)
                    {
                        bMP3 = true;
                        if (typeBitrate == BitrateType.bitrate_VBR) bVBR = true;
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
            if (bFlac && !bMP3 && !bCBR && !bVBR) { verdict += " - FLAC"; }
            if (bFlac && (bMP3 || bCBR || bVBR)) { verdict += " - Lossless & Lossy"; }
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
            FileInfoView.ListViewItemSorter = new ListViewItemComparer(e.Column, FileInfoView.Sorting);
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
                    fileInfo.SetInfoToForm();
                    //                fileInfo.UpdateInfo(Int32.Parse(FileInfoView.FocusedItem.SubItems[0].Text));
                }
                catch (CorruptFileException)
                {
                }
                catch (UnsupportedFormatException)
                {
                }
            }
        }
    }

    internal class ListViewItemComparer : IComparer
    {
        private readonly int col;
        private readonly SortOrder order;

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