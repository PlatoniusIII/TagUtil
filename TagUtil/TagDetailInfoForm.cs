using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace TagUtil
{
    public partial class TagDetailInfoForm : Form
    {
        formMain mainFrm;

        public TagDetailInfoForm( formMain parent )
        {
            mainFrm = parent;
            InitializeComponent();
        }

        private string ReadID3V2Tag(string tag)
        {
            TagLib.Id3v2.TextInformationFrame frame = TagLib.Id3v2.TextInformationFrame.Get(mainFrm.id3v2, tag, false);
            if (frame == null) return "";
            return frame.ToString();
        }

        // ID3v2 Tags Reference: http://id3.org/id3v2.4.0-frames
        public void UpdateInfo( string file )
        {

            TagLib.File tagFile = mainFrm.currentFile; // track is the name of the mp3

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
            if (mainFrm.id3v2 != null)
            {
                TagLib.Id3v2.PlayCountFrame pcf = TagLib.Id3v2.PlayCountFrame.Get(mainFrm.id3v2, false);
                editPlaycount.Text = pcf == null ? "" : pcf.ToString();

                TagLib.Id3v2.TextInformationFrame trackFrame = TagLib.Id3v2.TextInformationFrame.Get(mainFrm.id3v2, "TRCK", false);
                if (trackFrame != null)
                {
                    if (trackFrame.Text[0].Contains("/"))
                    {
                        int nSlash = trackFrame.Text[0].IndexOf("/");
                        editTrack.Text = trackFrame.Text[0].Substring(0, nSlash);
                        editTrackTotal.Text = (trackFrame.Text[0].Length > nSlash+1)?trackFrame.Text[0].Substring(nSlash+1):"";
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

            if( mainFrm.ContainsSeratoData() )
            {
                editSeratoAnalysis.Text = BitConverter.ToString(Encoding.Default.GetBytes(mainFrm.serato.seratoAnalysis));
                if (mainFrm.serato.seratoAutotags.Length > 0)
                    editSeratoAutotags.Text = "BPM: " + mainFrm.serato.BPM + " - tag2: " + mainFrm.serato.tag2 + " - tag3: " + mainFrm.serato.tag3;
                else
                    editSeratoAutotags.Text = "Field not available";
                string Markers = string.Empty;
                for (int i = 0; i < 8; i++)
                {
                    if (mainFrm.serato.markers[i].Name != string.Empty )
                    {
                        if (Markers.Length > 0) Markers += "; ";
                        Markers += mainFrm.serato.markers[i].Name;
                    }
                }
                editSeratoMarkers.Text = Markers;
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
    }
}
