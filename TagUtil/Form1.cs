﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using TagLib;

//Whole idea:
//- be able to rename directories based on content
//- 'repair' cue files - so also view them and highlight errors
//
//Problems with taglib-sharp:
//- doesn't read a lot of tags from other formats than ID3V2 (for eaxmple doesn't read VORBIS_COMMENT metadata block from FLAC) See https://www.the-roberts-family.net/metadata/flac.html
//- no list of "tags available in file"
namespace TagUtil
{
    public partial class formMain : Form
    {
//        public string directoryRenameScheme = "..\\%isrc% %albumartist% - %album% - %year% (%bitratetype%)";
        public TagLib.File currentFile = null;
        TagDetailInfoForm fileInfo;

        public TagLib.Id3v1.Tag id3v1;
        public TagLib.Id3v2.Tag id3v2;
        public TagLib.Mpeg4.AppleTag apple;
        public TagLib.Ape.Tag ape;
        public TagLib.Asf.Tag asf;
        public TagLib.Ogg.XiphComment ogg;
        public TagLib.Flac.Metadata flac;
        public TagLib.ByteVector Serato_Autotags_Identifier_ID3 = new TagLib.ByteVector("Serato Autotags");
        public TagLib.ByteVector Serato_BeatGrid_Identifier_ID3 = new TagLib.ByteVector("Serato BeatGrid");
        public TagLib.ByteVector Serato_Autotags_Identifier = new TagLib.ByteVector("SERATO_ANALYSIS");
        public TagLib.ByteVector Serato_BeatGrid_Identifier = new TagLib.ByteVector("SERATO_BEATGRID");
        public TagLib.ByteVector LAME_Identifier = new TagLib.ByteVector("LAME");
        string[] extensions = { ".mp3", ".wma", ".mp4", ".wav", ".flac", ".m4a" };

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

        public formMain()
        {
            InitializeComponent();
            appSettings = XMLsetting.AppSettings.LoadSettings("TagUtil.xml");
            fileInfo = new TagDetailInfoForm(this);
            fileInfo.TopLevel = false;
            fileInfo.AutoScroll = true;
            TagInfoPanel.Controls.Add(fileInfo);
            fileInfo.Show();

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
            apple = currentFile.GetTag(TagLib.TagTypes.Apple) as TagLib.Mpeg4.AppleTag;
            ape = currentFile.GetTag(TagLib.TagTypes.Ape) as TagLib.Ape.Tag;
            asf = currentFile.GetTag(TagLib.TagTypes.Asf) as TagLib.Asf.Tag;
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

                    currentTag.Key = ReadID3V2Tag("TKEY");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TKEY", false).ToString();
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

        private string ParseString()
        {
            /// <summary>We are going to rename the folder. Loop through all (music) files in the folder to determine aspects
            /// then rename
            /// </summary>
            string newDirectory = string.Empty;
            bool bPlaceholderActive = false;
            string placeHolder = string.Empty;
            QualityRating = DetermineMusicQualityRating();
            if (FileInfoView.FocusedItem != null)
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
                        newDirectory += replacePlaceholder(placeHolder);
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
            return newDirectory;
        }

        private string replacePlaceholder(string placeHolder)
        {
            string newData = string.Empty;

            switch (placeHolder)
            {
                case "isrc": newData = GetISRC((TagLib.Id3v2.Tag)currentFile.GetTag(TagLib.TagTypes.Id3v2)); break;
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

        //RW@20180925: Moet dit meerdere tags af kunnen gaan?
        //RW@20180925: Discogs zet meerder vormen van de ISRC achter elkaar, alleen de eerste pakken (tot comma)?
        public static string GetISRC(TagLib.Id3v2.Tag tag)
        {
            String ISRC = "";
            if (tag == null) return ISRC;

            var frames = tag.GetFrames<TagLib.Id3v2.TextInformationFrame>("TSRC");
            foreach (TagLib.Id3v2.TextInformationFrame frame in frames)
            {
                foreach (string text in frame.Text)
                    if (text.Length > ISRC.Length)
                        ISRC = text;
            }

            return ISRC;
        }

        //RW@20180925: Return type - flac, vbr or bitrate
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

        public bool ContainsSeratoData()
        {
            /// <summary>Scans the file for headers that point to Serato data
            /// Tags may differ between formats
            /// </summary>
            currentFile.Mode = TagLib.File.AccessMode.Read;
            long seratoData = currentFile.Find(Serato_Autotags_Identifier_ID3); //Find in MP3 files
            if (seratoData > 0) return true;
            seratoData = currentFile.Find(Serato_Autotags_Identifier); //Find in Xiph tag
            if (seratoData > 0) return true;
            //            seratoData = currentFile.Find(Serato_BeatGrid_Identifier);
//            if (seratoData > 0) tag.SeratoBeatgrid = true;

            return false;
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
//            if (!bVariousBitrates && !bVBRandCBR) { verdict += " - " + lastBitrate.ToString(); }
            if (bFlac && !bCBR && !bCBR) { verdict += " - FLAC"; }
            if (!bFlac && bMP3 && !bVBR && !bVariousBitrates) { verdict += " - " + GetBitrateTypeString(typeBitrate); } //Just use the last one
            if (!bFlac && !bMP3 && bVBR && !bVariousBitrates) { verdict += " - VBR"; }
            if (!bFlac && bMP3 && bVBR) { verdict += " - VBR+CBR"; }
            return verdict;
        }

        private string CheckOwnFiles()
        {
            /// <summary>Add a marker if it's something I've done - may make this more generic like a certain replace or add based on what it finds
            /// </summary>
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