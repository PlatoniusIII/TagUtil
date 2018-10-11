using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TagUtil
{
    public class TagInfo
    {
        public string Tags { get; set; }

        public int ID;
        public string file;

        public string Artist;
        public string Title;
        public string Album;
        public string AlbumArtist;
        public string Duration;

        public string Year;
        public string Track;
        public string TrackTotal;
        public string Disc;
        public string DiscTotal;

        public string Genre;
        public int Bitrate;
        public uint BPM;
        public string Key;
        public string ISRC;
        public string Publisher;
        public string RemixedBy;

        public bool VBR;
        public float VBRQuality;

        public bool SeratoMarkers;
        public bool SeratoBeatgrid;
    }
}
