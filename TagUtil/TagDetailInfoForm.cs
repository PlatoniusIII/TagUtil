using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
namespace TagUtil
{
    /// <summary>Form with detail info for the selected file</summary>
    public partial class TagDetailInfoForm : Form
    {
        private MainForm mainForm;
        private DiscogsInterface discogs = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">Link to main form</param>
        public TagDetailInfoForm(MainForm parent)
        {
            mainForm = parent;
            InitializeComponent();
        }

        private string ReadID3V2Tag(string tag)
        {
            TagLib.Id3v2.TextInformationFrame frame = TagLib.Id3v2.TextInformationFrame.Get(mainForm.id3v2, tag, false);
            if (frame == null) return "";
            return frame.ToString();
        }

        // ID3v2 Tags Reference: http://id3.org/id3v2.4.0-frames
        /// <summary>
        /// Full TagDetailInfoForm with information from the currently selected file
        /// </summary>
        public void SetInfoToForm()
        {
            try
            {
                TagLib.File tagFile = mainForm.currentFile; // track is the name of the mp3

                editArtist.Text = string.Join("; ", tagFile.Tag.Performers); //tagFile.Tag.Performers[0];
                editTitle.Text = tagFile.Tag.Title;
                editAlbumArtist.Text = string.Join("; ", tagFile.Tag.AlbumArtists); //tagFile.Tag.AlbumArtists.Length == 0 ? "" : tagFile.Tag.AlbumArtists[0];
                editAlbumTitle.Text = tagFile.Tag.Album;
                editYear.Text = tagFile.Tag.Year.ToString();

                editDisc.Text = tagFile.Tag.Disc == 0 ? "" : tagFile.Tag.Disc.ToString();
                editDiscTotal.Text = tagFile.Tag.DiscCount == 0 ? "" : tagFile.Tag.DiscCount.ToString();

                editGenre.Text = string.Join("; ", tagFile.Tag.Genres);
                editBitrate.Text = tagFile.Properties.AudioBitrate.ToString();

                editDuration.Text = tagFile.Properties.Duration.Hours.ToString() + ":" + tagFile.Properties.Duration.Minutes.ToString("0#") + "." + tagFile.Properties.Duration.Seconds.ToString("0#");

                editBPM.Text = tagFile.Tag.BeatsPerMinute.ToString();

                editTags.Text = tagFile.TagTypesOnDisk.ToString();

                editKey.Text = tagFile.Tag.InitialKey;
                editISRC.Text = tagFile.Tag.ISRC;
                editPublisher.Text = tagFile.Tag.Publisher;
                editRemixer.Text = tagFile.Tag.RemixedBy;

                editComment.Text = tagFile.Tag.Comment;
                if (mainForm.id3v2 != null)
                {
                    TagLib.Id3v2.PlayCountFrame pcf = TagLib.Id3v2.PlayCountFrame.Get(mainForm.id3v2, false);
                    editPlaycount.Text = pcf == null ? "" : pcf.ToString();

                    TagLib.Id3v2.TextInformationFrame trackFrame = TagLib.Id3v2.TextInformationFrame.Get(mainForm.id3v2, "TRCK", false);
                    if (trackFrame != null)
                    {
                        if (trackFrame.Text[0].Contains("/"))
                        {
                            int nSlash = trackFrame.Text[0].IndexOf("/");
                            editTrack.Text = trackFrame.Text[0].Substring(0, nSlash);
                            editTrackTotal.Text = (trackFrame.Text[0].Length > nSlash + 1) ? trackFrame.Text[0].Substring(nSlash + 1) : "";
                        }
                        else
                        {
                            editTrack.Text = tagFile.Tag.Track == 0 ? "" : tagFile.Tag.Track.ToString();
                            editTrackTotal.Text = tagFile.Tag.TrackCount == 0 ? "" : tagFile.Tag.TrackCount.ToString();
                        };

                        //                    editKey.Text = ReadID3V2Tag("TKEY");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TKEY", false).ToString();
                        //                    editISRC.Text = ReadID3V2Tag("TSRC");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TSRC", false).ToString();
                        //                    editPublisher.Text = ReadID3V2Tag("TPUB");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPUB", false).ToString();
                        //                    editRemixer.Text = ReadID3V2Tag("TPE4");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPE4", false).ToString();
                    }
                }
                else
                {
                    editTrack.Text = tagFile.Tag.Track.ToString();
                    editTrackTotal.Text = tagFile.Tag.TrackCount.ToString();
                }

                if (mainForm.serato.ContainsSeratoData())
                {
                    if (mainForm.serato.serato_struct.seratoAnalysis.raw.Length > 0)
                    {
                        //ToDo: Is able to get here when raw data is just zeroes
                        editSeratoAnalysis.Text = string.Empty;
                        for (int i = 0; i < mainForm.serato.serato_struct.seratoAnalysis.raw.Length; i++)
                        {
                            if (mainForm.serato.serato_struct.seratoAnalysis.raw[i] > 0)
                            {
                                if (i > 0) editSeratoAnalysis.Text += ".";
                                editSeratoAnalysis.Text += mainForm.serato.serato_struct.seratoAnalysis.raw[i].ToString();
                            }
                        }
                    }
                    else editSeratoAnalysis.Text = "Field not available";

                    if (!string.IsNullOrEmpty(mainForm.serato.serato_struct.seratoAutotags.data))
                        editSeratoAutotags.Text = "BPM: " + mainForm.serato.serato_struct.BPM + " - tag2: " + mainForm.serato.serato_struct.tag2 + " - tag3: " + mainForm.serato.serato_struct.tag3;
                    else
                        editSeratoAutotags.Text = "Field not available";
                    string Markers = string.Empty;
                    if (mainForm.serato.serato_struct.HighestMarker > 0)
                    {
                        Markers += mainForm.serato.serato_struct.HighestMarker + " Cues ";
                        string CueNames = string.Empty;
                        for (int i = 0; i < 8; i++)
                        {
                            if (mainForm.serato.serato_struct.Cues[i].Name != string.Empty)
                            {
                                if (CueNames.Length > 0) CueNames += "; ";
                                else CueNames += "(";
                                CueNames += mainForm.serato.serato_struct.Cues[i].Name;
                            }
                        }
                        if (CueNames.Length > 1) Markers += CueNames + ")";
                        editSeratoCue1.Text = mainForm.serato.serato_struct.Cues[0].Name;
                        editSeratoCue1.BackColor = mainForm.serato.serato_struct.Cues[0].color;
                    }
                    if (mainForm.serato.serato_struct.HighestLoop > 0)
                    {
                        if (Markers.Length > 0) Markers += "; ";
                        Markers += mainForm.serato.serato_struct.HighestLoop + " Loops ";
                        string LoopNames = string.Empty;
                        for (int i = 0; i < 4; i++)
                        {
                            if (mainForm.serato.serato_struct.loops[i].Name != string.Empty)
                            {
                                if (LoopNames.Length > 0) LoopNames += "; ";
                                else LoopNames += "(";
                                LoopNames += mainForm.serato.serato_struct.loops[i].Name;
                            }
                        }
                        if (LoopNames.Length > 1)
                        {
                            Markers += LoopNames + ")";
                        }
                    }
                    editSeratoMarkers.Text = Markers;
                    editSeratoBPMLock.Text = mainForm.serato.serato_struct.BPMLock == 0 ? "Off" : "On";
                }
                else
                {
                    editSeratoMarkers.Text = "Field not available";
                    editSeratoAutotags.Text = "Field not available";
                }

                //            editGenre.Text = tagFile.Tag.Genres[0];
                //            foreach (CommentFrame comment in id3Tag.Comments)
                //                textBox_Comments.Text += comment.Comment + "\n";

                //if( mainFrm.ContainsSeratoData() )
                //    checkSerato.CheckState = CheckState.Checked;
                //else
                //    checkSerato.CheckState = CheckState.Unchecked;

                pictureBoxPreview.Image = null;
                if (tagFile.Tag.Pictures.Length >= 1)
                {
                    for (int i = 0; i < tagFile.Tag.Pictures.Length; i++)
                    {
                        var bin = (byte[])(tagFile.Tag.Pictures[i].Data.Data);
                        //                pictureBoxPreview.Image = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(100, 100, null, IntPtr.Zero);
                        try
                        {
                            pictureBoxPreview.Image = Image.FromStream(new MemoryStream(bin));
                            break;
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                //TagLib.Id3v2.TextInformationFrame test = tagFile.TagTypes
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// Read changed info
        /// </summary>
        /// ToDO: GetInfoFromForm needs to be changed - handled for each field if it's changed
        public void GetInfoFromForm()
        {
            TagLib.File tagFile = mainForm.currentFile; // track is the name of the mp3

            editArtist.Text = string.Join("; ", tagFile.Tag.Performers); //tagFile.Tag.Performers[0];
            tagFile.Tag.Title = editTitle.Text;
            editAlbumArtist.Text = string.Join("; ", tagFile.Tag.AlbumArtists); //tagFile.Tag.AlbumArtists.Length == 0 ? "" : tagFile.Tag.AlbumArtists[0];
            tagFile.Tag.Album = editAlbumTitle.Text;
            editYear.Text = tagFile.Tag.Year.ToString();

            editDisc.Text = tagFile.Tag.Disc == 0 ? "" : tagFile.Tag.Disc.ToString();
            editDiscTotal.Text = tagFile.Tag.DiscCount == 0 ? "" : tagFile.Tag.DiscCount.ToString();

            editGenre.Text = string.Join("; ", tagFile.Tag.Genres);
            editBitrate.Text = tagFile.Properties.AudioBitrate.ToString();

            editDuration.Text = tagFile.Properties.Duration.Hours.ToString() + ":" + tagFile.Properties.Duration.Minutes.ToString("0#") + "." + tagFile.Properties.Duration.Seconds.ToString("0#");

            uint.TryParse(editBPM.Text, out uint BPM);
            tagFile.Tag.BeatsPerMinute = BPM;

            //            editTags.Text = tagFile.TagTypesOnDisk.ToString();

            tagFile.Tag.InitialKey = editKey.Text;
            tagFile.Tag.ISRC = editISRC.Text;
            tagFile.Tag.Publisher = editPublisher.Text;
            tagFile.Tag.RemixedBy = editRemixer.Text;

            editComment.Text = tagFile.Tag.Comment;
            if (mainForm.id3v2 != null)
            {
                TagLib.Id3v2.PlayCountFrame pcf = TagLib.Id3v2.PlayCountFrame.Get(mainForm.id3v2, false);
                editPlaycount.Text = pcf == null ? "" : pcf.ToString();

                TagLib.Id3v2.TextInformationFrame trackFrame = TagLib.Id3v2.TextInformationFrame.Get(mainForm.id3v2, "TRCK", false);
                if (trackFrame != null)
                {
                    if (trackFrame.Text[0].Contains("/"))
                    {
                        int nSlash = trackFrame.Text[0].IndexOf("/");
                        editTrack.Text = trackFrame.Text[0].Substring(0, nSlash);
                        editTrackTotal.Text = (trackFrame.Text[0].Length > nSlash + 1) ? trackFrame.Text[0].Substring(nSlash + 1) : "";
                    }
                    else
                    {
                        editTrack.Text = tagFile.Tag.Track == 0 ? "" : tagFile.Tag.Track.ToString();
                        editTrackTotal.Text = tagFile.Tag.TrackCount == 0 ? "" : tagFile.Tag.TrackCount.ToString();
                    };
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (mainForm.currentFile != null)
            {
                GetInfoFromForm();
                mainForm.currentFile.Save();
            }
        }

        private void buttonSearchDiscogs_Click(object sender, EventArgs e)
        {
            try
            {
                if (discogs == null)
                    discogs = new DiscogsInterface(mainForm.appSettings.TagUtilSettings.discogsToken);
            }
            catch( Exception ex)
            {
                Debug.WriteLine("Error with Discogs: " + ex.Message);
            }

//            DiscogsInterface discogs = new DiscogsInterface();
            //Create authentication object using private and public keys: you should fournish real keys here
//            var oAuthCompleteInformation = new OAuthCompleteInformation("consumerKey",
//                                            "consumerSecret", "token", "tokenSecret");
            //Create discogs client using the authentication
//            var discogsClient = new DiscogsClient.DiscogsClient(oAuthCompleteInformation);
        }
    }
}