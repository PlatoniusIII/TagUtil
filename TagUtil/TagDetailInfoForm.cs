using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

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

            if (mainFrm.id3v2 != null)
            {

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

                    editKey.Text = ReadID3V2Tag("TKEY");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TKEY", false).ToString();
                    editISRC.Text = ReadID3V2Tag("TSRC");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TSRC", false).ToString();
                    editPublisher.Text = ReadID3V2Tag("TPUB");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPUB", false).ToString();
                    editRemixer.Text = ReadID3V2Tag("TPE4");// TagLib.Id3v2.TextInformationFrame.Get(id3v2, "TPE4", false).ToString();

                }
            } 
            
//            editGenre.Text = tagFile.Tag.Genres[0];
//            foreach (CommentFrame comment in id3Tag.Comments)
//                textBox_Comments.Text += comment.Comment + "\n";
 
            if( mainFrm.ContainsSeratoData() )
                checkSerato.CheckState = CheckState.Checked;
            else
                checkSerato.CheckState = CheckState.Unchecked;

            if (tagFile.Tag.Pictures.Length >= 1)
            {
                var bin = (byte[])(tagFile.Tag.Pictures[0].Data.Data);
//                pictureBoxPreview.Image = Image.FromStream(new MemoryStream(bin)).GetThumbnailImage(100, 100, null, IntPtr.Zero);
                pictureBoxPreview.Image = Image.FromStream(new MemoryStream(bin));
            }

            //TagLib.Id3v2.TextInformationFrame test = tagFile.TagTypes
        }
    }
}
