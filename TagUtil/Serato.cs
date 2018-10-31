using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TagUtil
{
    /// <summary>
    ///    Class containing all Serato info, as well as some MixedInKey info
    /// </summary>
    /// <remarks>
    ///    Base64 encoding fails at times because it's missing an 'A'
    ///    at the end. So instead of 3 padding we add 'A=='.
    /// </remarks>
    public class Serato
    {
        private MainForm mainForm;

        #region Defines

        /// <summary>
        ///   Default length of a Cue tag (without name).
        /// </summary>
        public const int TagCueLength = 20;

        /// <summary>
        ///   Default length of a Loop tag (without name).
        /// </summary>
        public const int TagLoopLength = 29;

        /// <summary>
        ///   Default starting position of flip name.
        /// </summary>
        public const int TagFlipNamePosition = 29;

        /// <summary>
        ///   Some defines to make positions clearer to read.
        /// </summary>
        public const int TagSizeLength = 4;

        public const int TagNumberLength = 2;
        public const int TagEndOfSectionLength = 1;

        /// <summary>
        ///   Offset to make navigating the data easier.
        ///   Base offset is item.length (identifier including \0)
        /// </summary>
        public const int offsetNumber = 4;

        public const int offsetPosition1 = 6;
        public const int offsetPosition2 = 10;
        public const int offsetCueColor = 9; //Probably 9, not 10
        public const int offsetLoopColor = 14;

        #endregion Defines

        #region SeratoString Helper Class

        /// <summary>
        /// Class to allow easier access and conversion between byte data and string versions
        /// </summary>
        public class SeratoString
        {
            private string SeratoStringData;
            private byte[] SeratoRawData;

            public SeratoString()
            {
                Init();
            }

            public void Init()
            {
                SeratoStringData = string.Empty;
                SeratoRawData = new byte[0];
            }

            public string data
            {
                get { return SeratoStringData; }
                set { SeratoStringData = value; if (!string.IsNullOrEmpty(SeratoStringData)) SeratoRawData = Encoding.Default.GetBytes(SeratoStringData); }
            }

            public byte[] raw
            {
                get { return SeratoRawData; }
                set { SeratoRawData = value; SeratoStringData = Encoding.Default.GetString(SeratoRawData); }
            }
        }

        #endregion SeratoString Helper Class

        #region Struct containing all Serato related data

        /// <summary>
        ///   Class containing all Serato related information.
        /// </summary>
        /// <remarks>After analyze the track length is filled in.
        /// So that may be in one of the tags</remarks>
        public class Serato_struct
        {
            /// <summary>
            ///   Possible tags in a Marker tag.
            /// </summary>
            public string[] MarkerTags = { "CUE\0", "LOOP\0", "FLIP\0", "COLOR\0", "BPMLOCK\0" };

            /// <summary>
            /// Struct that contains the data split in description, type and data
            /// </summary>
            public struct SeratoRaw
            {
                public string Type { set; get; }
                public string Name { set; get; }
                public byte[] data;
            }

            /// <summary>
            /// Struct that contains all Cue data
            /// </summary>
            /// <remarks>It seems that for the Position, a second is
            /// divided into 1000 points</remarks>
            public class CueMarkers
            {
                public void Init()
                {
                    Name = string.Empty;
                    DataSize = -1;
                    Position = 0;
                    Number = -1;
                    color = System.Drawing.Color.Black;
                }

                public string Name { set; get; }
                public byte[] raw { set; get; }
                public int DataSize { set; get; }
                public int Position { set; get; }
                public int Number { set; get; }
                public Color color { set; get; }
            }

            /// <summary>
            /// Struct that contains all Loop data
            /// </summary>
            public class LoopMarkers
            {
                public void Init()
                {
                    Name = string.Empty;
                    DataSize = -1;
                    Number = -1;
                    PositionStart = 0;
                    PositionEnd = 0;
                    color = System.Drawing.Color.Black;
                }

                public string Name { set; get; }
                public byte[] raw { set; get; }
                public int DataSize { set; get; }
                public int PositionStart { set; get; }
                public int PositionEnd { set; get; }
                public int Number { set; get; }
                public Color color { set; get; }
            }

            /// <summary>
            /// Struct that contains all Flip data
            /// </summary>
            /// <remarks>Probably just a bunch of start and end markers</remarks>
            public class FlipMarkers
            {
                public struct FlipParts
                {
                    public int Size { set; get; }
                    public byte[] raw { set; get; }
                    public int PositionStart { set; get; }
                    public int PositionEnd { set; get; }
                }

                public void Init()
                {
                    Name = string.Empty;
                    DataSize = -1;
                    Number = -1;
                    NumberOfFlips = -1;
                    flipParts.Clear();
                }

                public string Name { set; get; }
                public byte[] raw { set; get; }
                public int DataSize { set; get; }
                public int Number { set; get; }
                public int NumberOfFlips { set; get; }
                public List<FlipParts> flipParts = new List<FlipParts>();
            }

            public void Init()
            {
                seratoAnalysis.Init();
                seratoMarkersV2.Init();
                seratoBeatgrid.Init();
                seratoAutotags.Init();
                seratoOffsets.Init();
                //                seratoAnalysis = string.Empty;
                //                seratoAnalysisRaw = new byte[0];
                //                seratoAutogain = string.Empty;
                //                seratoAutogainRaw = new byte[0];
                //                seratoAutotags = string.Empty;
                //                seratoAutotagsRaw = new byte[0];
                //                seratoBeatgrid = string.Empty;
                //                seratoBeatgridRaw = new byte[0];
                seratoMarkers = string.Empty;
                seratoMarkersRaw = new byte[0];
                //                seratoMarkersV2 = string.Empty;
                //                seratoMarkersV2Raw = new byte[0];
                seratoOverview = string.Empty;
                seratoOverviewRaw = new byte[0];
                seratoRelVol = string.Empty;
                seratoRelVolRaw = new byte[0];
                seratoVideoAssoc = string.Empty;
                seratoVideoAssocRaw = new byte[0];
                OffsetTag1 = string.Empty;
                OffsetTag2 = string.Empty;
                Bitrate = 0.0;
                Frequency = 0.0;

                BPM = 0.0;
                tag2 = 0.0;
                tag3 = 0.0;
                AutoGain = 0.0;

                HighestMarker = 0;
                HighestLoop = 0;
                HighestFlip = 0;

                TrackColor = Color.Black;

                dataRaw.Clear();

                for (int i = 0; i < Cues.Length; i++)
                {
                    if (Cues[i] is null)
                        Cues[i] = new CueMarkers();
                    else
                        Cues[i].Init();
                }
                for (int i = 0; i < loops.Length; i++)
                {
                    if (loops[i] is null)
                        loops[i] = new LoopMarkers();
                    else
                        loops[i].Init();
                }
                for (int i = 0; i < flips.Length; i++)
                {
                    if (flips[i] is null)
                        flips[i] = new FlipMarkers();
                    else
                        flips[i].Init();
                }
            }

            /// <summary>
            /// For now keep all data in both binary and string format
            /// </summary>
            public SeratoString seratoAnalysis = new SeratoString();

            public SeratoString seratoMarkersV2 = new SeratoString();
            public SeratoString seratoBeatgrid = new SeratoString();
            public SeratoString seratoAutotags = new SeratoString();
            public SeratoString seratoOffsets = new SeratoString();

            //            public string seratoAnalysis { set; get; }
            //            public byte[] seratoAnalysisRaw { set; get; }
            //            public string seratoAutogain { set; get; }
            //            public byte[] seratoAutogainRaw { set; get; }
            //            public string seratoAutotags { set; get; }
            //            public byte[] seratoAutotagsRaw { set; get; }
            //            public string seratoBeatgrid { set; get; }
            //            public byte[] seratoBeatgridRaw { set; get; }
            public string seratoMarkers { set; get; }

            public byte[] seratoMarkersRaw { set; get; }

            //            public string seratoMarkersV2 { set; get; }
            //            public byte[] seratoMarkersV2Raw { set; get; }
            public string seratoOverview { set; get; }

            public byte[] seratoOverviewRaw { set; get; }
            public string seratoRelVol { set; get; }
            public byte[] seratoRelVolRaw { set; get; }
            public string seratoVideoAssoc { set; get; }
            public byte[] seratoVideoAssocRaw { set; get; }

            public double BPM { set; get; }
            public double tag2 { set; get; } //Gain?
            public double tag3 { set; get; } //Only seen it as 0
            public CueMarkers[] Cues = new CueMarkers[8];
            public int HighestMarker { set; get; }
            public LoopMarkers[] loops = new LoopMarkers[8];
            public int HighestLoop { set; get; }
            public FlipMarkers[] flips = new FlipMarkers[6];
            public int HighestFlip { set; get; }
            public byte[] ColorTag { set; get; }
            public Color TrackColor { set; get; }
            public byte[] BPMLockRaw { set; get; }
            public int BPMLock { set; get; }
            public double AutoGain { set; get; }
            public List<SeratoRaw> dataRaw = new List<SeratoRaw>();
            public string OffsetTag1 { set; get; }
            public string OffsetTag2 { set; get; }
            public double Bitrate { set; get; }
            public double Frequency { set; get; }
        }

        public Serato.Serato_struct serato_struct;

        #endregion Struct containing all Serato related data

        #region MixedinKey JSON structures

        public class MixedInKey_cue
        {
            public double time { get; set; }
            public string name { get; set; }
        }

        public class MixedInKey_cuepoints
        {
            public IList<MixedInKey_cue> cues { get; set; }
            public string source { get; set; }
            public string algorithm { get; set; }
        }

        public class MixedInKey_key
        {
            public string key { get; set; }
            public string source { get; set; }
            public string algorithm { get; set; }
        }

        public class MixedInKey_energy
        {
            public string energylevel { get; set; }
            public string source { get; set; }
            public string algorithm { get; set; }
        }

        #endregion MixedinKey JSON structures

        public Serato(MainForm parent)
        {
            mainForm = parent;
            serato_struct = new Serato_struct();
        }

        /// <summary>
        ///    Parses Serato info, returns True if Serato tags are present.
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
        ///          <description>Version of the analysis engine</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Autogain (FLAC)/Autotags (MP3)</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Seems to contain the BPM and 2 more values. Only have seen the 3rd value being 0</description>
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
        ///          as well as a section COLOR and BPMLOCK.
        ///          Tag can have a LOT of padding at the end, to avoid rewrites if
        ///          cues or loops are added?</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Offsets</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Contains the bitrate and sampling frequency and more.</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Overview</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Unknown. Seems to be about 240 blocks of 16 bytes. Mostly 0x01,
        ///          but some code in the middle</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato RelVol</term>
        ///          <term>application/octet-stream</term>
        ///          <description>FLAC version of MP3's Autogain tag?</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato VideoAssoc</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Guess it's the option to link to a video file, but for FLAC only?</description>
        ///       </item>
        ///    </list>
        /// </remarks>
        public bool ContainsSeratoData()
        {
            /// <summary>Scans the file for headers that point to Serato data
            /// Tags may differ between formats
            /// </summary>
            bool bExists = false;

            serato_struct.Init();

            //Try and parse fields
            bExists = SeratoReadID3();
            bExists |= SeratoReadXiph();
            bExists |= SeratoReadApple();

            // See https://stackoverflow.com/questions/41850029/string-parsing-techniques for parsing info

            ParseSeratoAutotagsTag();
            ParseSeratoMarkersV2Tag();
            ParseSeratoBeatgridTag();
            ParseSeratoOffsets();

            return bExists;
        }

        #region Parse helper functions

        /// <summary>
        ///   Parse the Serato Autotags tag
        /// </summary>
        /// <remarks>
        ///   The Autotags/Autogain tag contains the BPM of the song
        ///   as well as 2 other floats. One probably being the gain.
        /// </remarks>
        private void ParseSeratoAutotagsTag()
        {
            if (!string.IsNullOrEmpty(serato_struct.seratoAutotags.data))
            {
                double temp;
                string[] words = serato_struct.seratoAutotags.data.Substring(2).Split('\0');
                double.TryParse(words[0], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.BPM = temp;// Convert.ToDouble(serato.seratoAutotags.Substring(2, serato.seratoAutotags.IndexOf('\0', 2) - 1));
                double.TryParse(words[1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.tag2 = temp;
                double.TryParse(words[2], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.tag3 = temp;
            }
        }

        /// <summary>
        ///   Parse the Serato Markers V2 tag
        /// </summary>
        /// <remarks>
        ///    The Markers V2 tag contains cue points, loops, flips,
        ///    color tags and a BPMLOCK tag.
        ///    This could be easier if parsing like a stream, but for the
        ///    parts I don't know what it is this would be difficult.
        /// </remarks>
        private void ParseSeratoMarkersV2Tag()
        {
            //Markers tag contains CUEs (and their color codes) and Loops
            //ToDo: find out what color does, can occur multiple times in tag
            if (!string.IsNullOrEmpty(serato_struct.seratoMarkersV2.data))
            {
                //Data part of the markers tag is itself again Base64 encoded

                //It was possible for the string to not end with '\0'
                int SkipAtStart = 0;
                for (int i = 0; i < serato_struct.seratoMarkersV2.data.Length; i++)
                {
                    if (serato_struct.seratoMarkersV2.data[i] == '\u0001')
                    {
                        SkipAtStart++;
                    }
                    else break;
                }
                int End = serato_struct.seratoMarkersV2.data.IndexOf('\0');
                if (End == -1) End = serato_struct.seratoMarkersV2.data.Length;
                string ToDecode = serato_struct.seratoMarkersV2.data.Substring(SkipAtStart, End - SkipAtStart);
                serato_struct.seratoMarkersV2.raw = Convert.FromBase64String(ValidateBase64EncodedString(ToDecode));
                //serato_struct.seratoMarkersV2.data = Encoding.ASCII.GetString(serato_struct.seratoMarkersV2.raw);
                //                int nFoundPos = -1; //Skip 2 \u0001 start bytes used as separator
                //                int nStringPos = 2;
                //int nEnd;
                Int32 Size;
                for (int i = 2; i < serato_struct.seratoMarkersV2.data.Length; i++)
                {
                    string temp = serato_struct.seratoMarkersV2.data.Substring(i);
                    foreach (var item in serato_struct.MarkerTags)
                    {
                        if (serato_struct.seratoMarkersV2.data.Substring(i).StartsWith(item))
                        {
                            Size = BitConverter.ToInt32(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length).Take(4).Reverse().ToArray(), 0);
                            switch (item)
                            {
                                case "CUE\0":
                                    //4 bytes: 'CUE\0'
                                    //4 bytes: length of data part
                                    //2 bytes: cue number
                                    //4 bytes: position
                                    //4 bytes: color??? (last always 0)
                                    //1 byte: '\0'
                                    //X bytes: Label of Cue + '\0'
                                    //CUE\0     Size  Nr        Pos         Color
                                    //CUE 0 0 0 0 13 0 0 0 0  0   0 0 204   0   0 0 0 0
                                    //CUE 0 0 0 0 13 0 1 0 0  0   8 0 204 136   0 0 0 0
                                    //CUE 0 0 0 0 13 0 2 0 0  0 100 0   0   0 204 0 0 0
                                    //CUE 0 0 0 0 13 0 3 0 0  3 234 0 204 204   0 0 0 0
                                    //CUE 0 0 0 0 18 0 4 0 0  0   0 0   0 204   0 0 0"0 sec" 0
                                    //CUE 0 0 0 0 18 0 5 0 0 19 136 0 204   0 204 0 0"5 sec" 0
                                    serato_struct.Cues[serato_struct.HighestMarker].DataSize = Size;
                                    //serato_struct.markers[serato_struct.HighestMarker].raw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, 4+4+Size));
                                    serato_struct.Cues[serato_struct.HighestMarker].raw = new byte[4 + item.Length + Size - 1];
                                    Array.Copy(serato_struct.seratoMarkersV2.raw, i, serato_struct.Cues[serato_struct.HighestMarker].raw, 0, 4 + item.Length + Size - 1);
                                    //ToDo: it's possible there's no name, so StartOfName = end of name, so checking with StartOfName+1 is wrong
                                    int StartOfName = serato_struct.seratoMarkersV2.data.LastIndexOf("\0", i + item.Length + TagSizeLength + Size - 1 - 1) + 1; //-1 for last byte of string (\0), so another -1 for the last character of the name, or the \0 before if there is no name
                                    serato_struct.Cues[serato_struct.HighestMarker].Name = serato_struct.seratoMarkersV2.data.Substring(StartOfName, (i + item.Length + TagSizeLength + Size - 1) - StartOfName);
                                    serato_struct.Cues[serato_struct.HighestMarker].Number = BitConverter.ToInt16(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + offsetNumber).Take(2).Reverse().ToArray(), 0);
                                    serato_struct.Cues[serato_struct.HighestMarker].Position = BitConverter.ToInt32(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + offsetPosition1).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.Cues[serato_struct.HighestMarker].color = Color.FromArgb(serato_struct.seratoMarkersV2.raw[i + item.Length + offsetCueColor], serato_struct.seratoMarkersV2.raw[i + item.Length + offsetCueColor + 1], serato_struct.seratoMarkersV2.raw[i + item.Length + offsetCueColor + 2], serato_struct.seratoMarkersV2.raw[i + item.Length + offsetCueColor + 3]);
                                    serato_struct.HighestMarker++;
                                    //                                    i += (Size + item.Length + 4);
                                    break;

                                case "LOOP\0":
                                    //LOOP\0     size  Nr      start        end              ??           ??
                                    //LOOP 0 0 0 0 21 0 0 0 0  1  65 0 0 14 148 255 255 255 255 0 39 170 250 0 0 0
                                    //LOOP 0 0 0 0 26 0 1 0 0 14 142 0 0 27 216 255 255 255 255 0 39 170 225 0 0"Loop2" 0
                                    //
                                    //LOOP 0 0 0 0 25 0 0 0 0  1  8C 0 0 0E   0  FF  FF  FF  FF 0 27  AA  E1 0 0"Intro loop - Aan"
                                    //LOOP 0 0 0 0 21 0 1 0 0 A2  78 0 0 D4  82  FF  FF  FF  FF 0 27  AA  E1 0 0"Loop cue 2-3"
                                    serato_struct.loops[serato_struct.HighestLoop].DataSize = Size;
                                    serato_struct.loops[serato_struct.HighestLoop].raw = new byte[4 + item.Length + Size];
                                    Array.Copy(serato_struct.seratoMarkersV2.raw, i, serato_struct.loops[serato_struct.HighestLoop].raw, 0, 4 + item.Length + Size);
                                    StartOfName = serato_struct.seratoMarkersV2.data.LastIndexOf("\0", i + item.Length + TagSizeLength + Size - 1 - 1) + 1; //-1 for last byte of string (\0), so another -1 for the last character of the name, or the \0 before if there is no name
                                    serato_struct.loops[serato_struct.HighestLoop].Name = serato_struct.seratoMarkersV2.data.Substring(StartOfName, (i + item.Length + TagSizeLength + Size - 1) - StartOfName);
                                    serato_struct.loops[serato_struct.HighestLoop].Number = BitConverter.ToInt16(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + offsetNumber).Take(2).Reverse().ToArray(), 0);
                                    serato_struct.loops[serato_struct.HighestLoop].PositionStart = BitConverter.ToInt32(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + offsetPosition1).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.loops[serato_struct.HighestLoop].PositionEnd = BitConverter.ToInt32(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + offsetPosition2).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.loops[serato_struct.HighestLoop].color = Color.FromArgb(serato_struct.seratoMarkersV2.raw[i + item.Length + offsetLoopColor], serato_struct.seratoMarkersV2.raw[i + item.Length + offsetLoopColor + 1], serato_struct.seratoMarkersV2.raw[i + item.Length + offsetLoopColor + 2]);
                                    serato_struct.HighestLoop++;
                                    //                                    i += (Size + item.Length + 4);
                                    break;

                                case "FLIP\0":
                                    //FLIP    length      Number   Name              ?? # of parts? Would be 23 parts of 21 bytes
                                    //               497                             488 left       483 left (= 23*21)
                                    //FLIP 00 00-00-01-F1 00-00 00 46-6C-69-70-31-00 01 00-00-00-17 00 00-00-00-10 40-46-66-BB-41-47-D5-1C-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-35-14-D6-6C-1F-54-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-2F-23-18-3A-4A-D4-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-29-BC-F7-F8-75-63-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-20-D2-5A-AD-B6-A3-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-1D-D9-7B-94-CC-63-3F-D9-BA-5E-35-3F-7D-00-00-00-00-00-10-3F-F2-B1-2F-D4-16-1F-6C-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-3D-FF-73-B6-DE-14-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-20-D2-5A-AD-B6-A3-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-23-CB-39-C6-A0-E3-40-54-00-20-C4-9B-A5-E3-00-00-00-00-10-40-54-1D-D9-7B-94-CC-63-3F-D9-BA-5E-35-3F-7D-00-00-00-00-00-10-3F-F1-93-DC-2A-BE-48-2C-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-2C-2A-39-21-60-94-40-57-33-53-F7-CE-D9-17-00-00-00-00-10-40-57-99-DE-08-AA-6A-CC-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-46-EA-11-01-9C-D4-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-14-63-40-5A-0E-94-40-57-33-53-F7-CE-D9-17-00-00-00-00-10-40-57-76-33-93-7F-6F-C5-40-5A-66-87-2B-02-0C-4A-00-00-00-00-10-40-5A-8A-31-A0-2D-07-34-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-45-35-14-D6-6C-1F-54-40-57-33-53-F7-CE-D9-17-00-00-00-00-10-40-57-56-FE-6C-F9-D4-1F-40-60-66-7E-F9-DB-22-D1-00-00-00-00-10-40-60-91-97-9C-C4-63-D1-40-63-99-B2-2D-0E-56-04-00-00-00-00-10-40-63-AA-C9-2F-DD-97-84-40-44-CD-0E-56-04-18-94-00-00-00-00-10-40-46-81-97-55-6F-8C-BA-40-46-66-BB-41-47-D5"
                                    //10 could be the size, as there are basically 16 bytes after it till the end, apart from the last one?
                                    //00 00-00-00-10 40-46-66-BB-41-47-D5-1C-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-35-14-D6-6C-1F-54-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-2F-23-18-3A-4A-D4-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-29-BC-F7-F8-75-63-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-20-D2-5A-AD-B6-A3-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-1D-D9-7B-94-CC-63-3F-D9-BA-5E-35-3F-7D-00-
                                    //00 00-00-00-10 3F-F2-B1-2F-D4-16-1F-6C-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-3D-FF-73-B6-DE-14-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-20-D2-5A-AD-B6-A3-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-23-CB-39-C6-A0-E3-40-54-00-20-C4-9B-A5-E3-
                                    //00 00-00-00-10 40-54-1D-D9-7B-94-CC-63-3F-D9-BA-5E-35-3F-7D-00-
                                    //00 00-00-00-10 3F-F1-93-DC-2A-BE-48-2C-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-2C-2A-39-21-60-94-40-57-33-53-F7-CE-D9-17-
                                    //00 00-00-00-10 40-57-99-DE-08-AA-6A-CC-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-46-EA-11-01-9C-D4-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-14-63-40-5A-0E-94-40-57-33-53-F7-CE-D9-17-
                                    //00 00-00-00-10 40-57-76-33-93-7F-6F-C5-40-5A-66-87-2B-02-0C-4A-
                                    //00 00-00-00-10 40-5A-8A-31-A0-2D-07-34-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-45-35-14-D6-6C-1F-54-40-57-33-53-F7-CE-D9-17-
                                    //00 00-00-00-10 40-57-56-FE-6C-F9-D4-1F-40-60-66-7E-F9-DB-22-D1-
                                    //00 00-00-00-10 40-60-91-97-9C-C4-63-D1-40-63-99-B2-2D-0E-56-04-
                                    //00 00-00-00-10 40-63-AA-C9-2F-DD-97-84-40-44-CD-0E-56-04-18-94-
                                    //00 00-00-00-10 40-46-81-97-55-6F-8C-BA-40-46-66-BB-41-47-D5"

                                    //FLIP    length      Number   Name              ?? # of parts? Would be 26 parts of 21 bytes
                                    //               560                             551 left       546 left (= 26*21)
                                    //FLIP 00 00-00-02-30 00-00 01 46-6C-69-70-31-00 01 00-00-00-1A 00-00-00-00-10-40-0D-78-35-AF-3D-B0-79-40-14-00-00-00-00-00-00-00-00-00-00-10-40-17-B7-16-DF-24-D0-02-40-14-00-00-00-00-00-00-00-00-00-00-10-40-16-B1-8A-2E-94-4A-00-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-CA-07-F0-F4-15-C0-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C6-AF-F5-F8-0E-80-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C7-0F-11-DB-2B-C0-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C8-2C-65-84-83-80-40-14-00-00-00-00-00-00-00-00-00-00-10-40-17-9F-4F-E6-5D-7E-02-40-14-00-00-00-00-00-00-00-00-00-00-10-40-16-99-C3-35-CC-F8-00-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-CB-25-44-9D-6D-80-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C7-CD-49-A1-66-40-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C7-CD-49-A1-66-40-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-C7-6E-2D-BE-49-00-40-14-00-00-00-00-00-00-00-00-00-00-10-40-17-B7-16-DF-24-D0-02-40-14-00-00-00-00-00-00-00-00-00-00-10-40-16-B1-8A-2E-94-4A-00-00-00-00-00-00-00-00-00-00-00-00-00-10-3F-D0-58-CB-09-08-55-EA-40-14-00-00-00-00-00-00-00-00-00-00-10-40-16-0B-19-61-21-0C-00-00-00-00-00-00-00-00-00-00-00-00-00-10-3F-D0-58-CB-09-08-55-EA-40-14-00-00-00-00-00-00-00-00-00-00-10-40-15-35-1A-A2-1F-2A-00-00-00-00-00-00-00-00-00-00-00-00-00-10-3F-D9-43-68-53-C7-10-AB-40-14-00-00-00-00-00-00-00-00-00-00-10-40-15-35-1A-A2-1F-2A-00-00-00-00-00-00-00-00-00-00-00-00-00-10-40-0C-96-9B-F9-1B-BA-B2-40-14-00-00-00-00-00-00-00-00-00-00-10-40-17-CE-DD-D7-EC-22-06-40-14-00-00-00-00-00-00-00-00-00-00-10-40-17-57-FA-FC-07-88-01-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-CE-1E-23-B6-57-80-40-72-C0-00-00-00-00-00-00-00-00-00-10-40-72-D9-0F-1E-67-0B-EC-40-0D-78-35-AF-3D-B0"
                                    //00-00-00-00-10 40-0D-78-35-AF-3D-B0-79-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-17-B7-16-DF-24-D0-02-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-16-B1-8A-2E-94-4A-00-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-CA-07-F0-F4-15-C0-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C6-AF-F5-F8-0E-80-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C7-0F-11-DB-2B-C0-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C8-2C-65-84-83-80-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-17-9F-4F-E6-5D-7E-02-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-16-99-C3-35-CC-F8-00-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-CB-25-44-9D-6D-80-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C7-CD-49-A1-66-40-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C7-CD-49-A1-66-40-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-C7-6E-2D-BE-49-00-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-17-B7-16-DF-24-D0-02-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-16-B1-8A-2E-94-4A-00-00-00-00-00-00-00-00-00-
                                    //00-00-00-00-10 3F-D0-58-CB-09-08-55-EA-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-16-0B-19-61-21-0C-00-00-00-00-00-00-00-00-00-
                                    //00-00-00-00-10 3F-D0-58-CB-09-08-55-EA-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-15-35-1A-A2-1F-2A-00-00-00-00-00-00-00-00-00-
                                    //00-00-00-00-10 3F-D9-43-68-53-C7-10-AB-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-15-35-1A-A2-1F-2A-00-00-00-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-0C-96-9B-F9-1B-BA-B2-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-17-CE-DD-D7-EC-22-06-40-14-00-00-00-00-00-00-
                                    //00-00-00-00-10 40-17-57-FA-FC-07-88-01-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-CE-1E-23-B6-57-80-40-72-C0-00-00-00-00-00-
                                    //00-00-00-00-10 40-72-D9-0F-1E-67-0B-EC-40-0D-78-35-AF-3D-B0 - probably lacks a \0?
                                    //As the data starts with '40' or '3F' it doesn't look like a position or offset. 9th value is again '40' or '00'.
                                    //You'd expect to have something like a start position and a play length in there...
                                    serato_struct.flips[serato_struct.HighestFlip].DataSize = Size;
                                    serato_struct.flips[serato_struct.HighestFlip].raw = new byte[4 + item.Length + Size];
                                    Array.Copy(serato_struct.seratoMarkersV2.raw, i, serato_struct.flips[serato_struct.HighestFlip].raw, 0, 4 + item.Length + Size);
                                    int EndOfName = serato_struct.seratoMarkersV2.data.IndexOf("\0", i + item.Length + 1 + TagSizeLength + 3);
                                    serato_struct.flips[serato_struct.HighestFlip].Number = BitConverter.ToInt16(serato_struct.seratoMarkersV2.raw.Skip(i + (item.Length + TagEndOfSectionLength) + TagSizeLength).Take(2).Reverse().ToArray(), 0);
                                    serato_struct.flips[serato_struct.HighestFlip].Name = serato_struct.seratoMarkersV2.data.Substring(i + item.Length + TagSizeLength + 3, EndOfName - (i + item.Length + TagSizeLength + 3));
                                    serato_struct.flips[serato_struct.HighestFlip].NumberOfFlips = BitConverter.ToInt16(serato_struct.seratoMarkersV2.raw.Skip(i + item.Length + TagSizeLength + TagNumberLength + 1 + (serato_struct.flips[serato_struct.HighestFlip].Name.Length + 1) + 1).Take(4).Reverse().ToArray(), 0);
                                    int FlipsStart = i + item.Length + TagSizeLength + TagNumberLength + 1 + (serato_struct.flips[serato_struct.HighestFlip].Name.Length + 1) + 1 + 4 + 1;
                                    for (int j = 0; j < serato_struct.flips[serato_struct.HighestFlip].NumberOfFlips; j++)
                                    {
                                        Serato_struct.FlipMarkers.FlipParts newFlip = new Serato_struct.FlipMarkers.FlipParts();
                                        newFlip.Size = BitConverter.ToInt16(serato_struct.seratoMarkersV2.raw.Skip(FlipsStart).Take(4).Reverse().ToArray(), 0);
                                        newFlip.raw = new byte[newFlip.Size];
                                        Array.Copy(serato_struct.seratoMarkersV2.raw, FlipsStart + 4, newFlip.raw, 0, newFlip.Size);
                                        serato_struct.flips[serato_struct.HighestFlip].flipParts.Add(newFlip);
                                        FlipsStart += 4 + newFlip.Size + 1;
                                    }
                                    serato_struct.HighestFlip++;
                                    //                                    i += (Size + item.Length + 4);
                                    break;

                                case "COLOR\0":
                                    //Serato has the option to give tracks a color
                                    //Size + color, seemingly as alpha R G B
                                    serato_struct.ColorTag = Encoding.ASCII.GetBytes(serato_struct.seratoMarkersV2.data.Substring(i, 4 + item.Length + Size));

                                    serato_struct.TrackColor = Color.FromArgb(
                                        serato_struct.seratoMarkersV2.raw[i + item.Length + TagSizeLength],
                                        serato_struct.seratoMarkersV2.raw[i + item.Length + TagSizeLength + 1],
                                        serato_struct.seratoMarkersV2.raw[i + item.Length + TagSizeLength + 2],
                                        serato_struct.seratoMarkersV2.raw[i + item.Length + TagSizeLength + 3]);
                                    //                                    i += (Size + item.Length + Serato_struct.TagSizeLength - 1);
                                    break;

                                case "BPMLOCK\0":
                                    //Seem to be 3 bytes, all set to 1. Would that have anything to do with key lock/tempo lock?
                                    //serato_struct.loops[serato_struct.HighestLoop].DataSize = Size;
                                    serato_struct.BPMLockRaw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkersV2.data.Substring(i, 4 + item.Length + Size));
                                    //Seems to be a single byte
                                    serato_struct.BPMLock = serato_struct.seratoMarkersV2.raw[i + item.Length + TagSizeLength];
                                    //                                    i += (Size + item.Length + 4 - 1);
                                    break;

                                default:
                                    break;
                            }
                            i += (Size + item.Length + TagSizeLength - 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Parse the Serato Beatgrid tag
        /// </summary>
        /// <remarks>Since it's a fixed grid (no option for multiple adjustments in the grid)
        /// it should contain a start position and range marker</remarks>
        private void ParseSeratoBeatgridTag()
        {
            if (!string.IsNullOrEmpty(serato_struct.seratoBeatgrid.data))
            {
                byte bt = serato_struct.seratoBeatgrid.raw[0]; //Placeholder to quickly see contents
            }
            //The 43 is always on the same position. All my examples start with 01 00 00 00 00 01, which leaves 4 bytes until the 43 and 4 bytes after.
            //The beatgrid will probably need a starting position and spacing
            //The value after the 6th 01 is 3x
            //Maybe make a set of files with slight offsets to the grid and spacing to see how the tag changes
            //01 00 00 00 00 01 3D 88 E7 A4 43 2E 00 00 63
            //01 00 00 00 00 01 3E A4 B6 B2 43 0D 00 00 20 00 ??Flac
            //01 00 00 00 00 01 3D 6F 95 60 43 2C 00 00 2D
            //01 00 00 00 00 01 3C CD 5B 77 43 12 B0 A4 2D
            //01 00 00 00 00 01 3F FB E2 30 43 16 00 00 00
            //01 00 00 00 00 01 3E F3 DC 9D 43 15 21 48 00 00
        }

        /// <summary>
        ///   Parse the Serato Offsets tag
        /// </summary>
        /// <remarks>Contains bitrate and samplerate and more, only for MP3</remarks>
        private void ParseSeratoOffsets()
        {
            if (!string.IsNullOrEmpty(serato_struct.seratoOffsets.data))
            {
                byte bt = serato_struct.seratoOffsets.raw[0]; //Placeholder to quickly see contents
                //Starts with 0x01 0x02
                int StartOfString = 3;
                int EndOfString = serato_struct.seratoOffsets.data.IndexOf("\0", StartOfString);
                double temp;
                double.TryParse(serato_struct.seratoOffsets.data.Substring(StartOfString, EndOfString - StartOfString), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.Bitrate = temp;
                StartOfString = EndOfString + 1;
                EndOfString = serato_struct.seratoOffsets.data.IndexOf("\0", StartOfString);
                serato_struct.OffsetTag1 = serato_struct.seratoOffsets.data.Substring(StartOfString, EndOfString - StartOfString);
                double.TryParse(serato_struct.seratoOffsets.data.Substring(StartOfString, EndOfString - StartOfString), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.Frequency = temp;
                //05-66-37-00-05-24-01-72-00-00-52-3F-00-00
                //The 52 3F could be the size of the data that follows
                //From here we seem to have 08-14 and 08-15 until the end. So is that just rubbish?
                //08-14-08-14-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-14-08-15-08-15-08-15-08-15-08-15
                //Another file:
                //06-57-3F-00-06-15-07-39-00-00-5F-07-00-00
                //08-14-08-14-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-15-08-14-08-15-08-15-08-15-08-15-08-15
            }
        }

        #endregion Parse helper functions

        #region SeratoTag Read Helper Functions

        /// <summary>
        ///   Look for and parse ID3 tags. These are in binary format
        ///   with description and type already removed.
        /// </summary>
        /// <remarks>
        ///    Loops through attachment frames to see if there are
        ///    Serato tags present.
        /// </remarks>
        /// <returns>A <see cref="bool"/> noting if Serato tags are found</returns>
        /// ToDo: Te array.copy below goes wrong, as it's not a 'set', so doesn't set the string function of the byte data on the copy
        private bool SeratoReadID3()
        {
            bool FoundTag = false;
            //Look for and parse ID3 tags. These are in binary format with description and type already removed
            if (mainForm.id3v2 != null)
            {
                foreach (TagLib.Id3v2.Frame f in mainForm.id3v2.GetFrames())
                {
                    if (f is TagLib.Id3v2.AttachmentFrame)
                    {
                        TagLib.Id3v2.AttachmentFrame tagSerato = ((TagLib.Id3v2.AttachmentFrame)f);
                        if (tagSerato.Description == "Serato Analysis")
                        {
                            serato_struct.seratoAnalysis.raw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(tagSerato.Data.Data, serato_struct.seratoAnalysis.raw, tagSerato.Data.Data.Length);
                            serato_struct.seratoAnalysis.data = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Autotags")
                        {
                            serato_struct.seratoAutotags.raw = tagSerato.Data.Data;
                            //serato_struct.seratoAutotags.raw = new byte[tagSerato.Data.Data.Length];
                            //Array.Copy(tagSerato.Data.Data, serato_struct.seratoAutotags.raw, tagSerato.Data.Data.Length);
                            //serato_struct.seratoAutotags = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Autogain")
                        {
                            Debug.WriteLine("Call to Serato Autogain in ID3");
                            Debug.Assert(false);
                            //serato_struct.seratoAutotags.raw = new byte[tagSerato.Data.Data.Length];
                            //Array.Copy(serato_struct.seratoAutotags.raw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            //FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato BeatGrid")
                        {
                            serato_struct.seratoBeatgrid.raw = tagSerato.Data.Data;
                            //serato_struct.seratoBeatgrid.raw = new byte[tagSerato.Data.Data.Length];
                            //Array.Copy(tagSerato.Data.Data, serato_struct.seratoBeatgrid.raw, tagSerato.Data.Data.Length);
                            //serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Markers_")
                        {
                            serato_struct.seratoMarkersRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(tagSerato.Data.Data, serato_struct.seratoMarkersRaw, tagSerato.Data.Data.Length);
                            serato_struct.seratoMarkers = tagSerato.Data.ToString();
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Markers2")
                        {
                            serato_struct.seratoMarkersV2.raw = tagSerato.Data.Data;
                            //serato_struct.seratoMarkersV2.raw = new byte[tagSerato.Data.Data.Length];
                            // new byte[tagSerato.Data.Data.Length];
                            //Array.Copy(tagSerato.Data.Data, serato_struct.seratoMarkersV2.raw, tagSerato.Data.Data.Length);
                            //serato_struct.seratoMarkersV2 = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Overview")
                        {
                            serato_struct.seratoOverviewRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(tagSerato.Data.Data, serato_struct.seratoOverviewRaw, tagSerato.Data.Data.Length);
                            serato_struct.seratoOverview = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Offsets_")
                        {
                            //21177 BYTES?!?!?!?
                            serato_struct.seratoOffsets.raw = tagSerato.Data.Data;
                            //serato_struct.seratoOffsets.raw = new byte[tagSerato.Data.Data.Length];
                            //Array.Copy(tagSerato.Data.Data, serato_struct.seratoOffsets.raw, tagSerato.Data.Data.Length);
                            //serato_struct.seratoOverview = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                    }
                }
            }
            return FoundTag;
        }

        /// <summary>
        ///   Look for and parse Xiph tags. These are Base64 encoded.
        ///   Decode and split in description, type and data.
        /// </summary>
        /// <remarks>
        ///    Loops through attachment frames to see if there are
        ///    Serato tags present.
        /// </remarks>
        /// <returns>A <see cref="bool"/> noting if Serato tags are found</returns>
        private bool SeratoReadXiph()
        {
            bool FoundTag = false;
            if (mainForm.ogg != null)
            {
                serato_struct.seratoAnalysis.raw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_ANALYSIS")].data; //serato_struct.seratoAnalysis.data = Encoding.ASCII.GetString(serato_struct.seratoAnalysis.raw);
                serato_struct.seratoMarkersV2.raw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_MARKERS_V2")].data; //serato_struct.seratoMarkersV2 = Encoding.ASCII.GetString(serato_struct.seratoMarkersV2Raw);
                serato_struct.seratoAutotags.raw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_AUTOGAIN")].data; //serato_struct.seratoAutotags = Encoding.ASCII.GetString(serato_struct.seratoAutotagsRaw);
                serato_struct.seratoBeatgrid.raw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_BEATGRID")].data; //serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(serato_struct.seratoBeatgridRaw);
                serato_struct.seratoOverviewRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_OVERVIEW")].data; serato_struct.seratoOverview = Encoding.ASCII.GetString(serato_struct.seratoOverviewRaw);
                serato_struct.seratoRelVolRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_RELVOL")].data; serato_struct.seratoRelVol = Encoding.ASCII.GetString(serato_struct.seratoRelVolRaw);
                serato_struct.seratoVideoAssocRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_VIDEO_ASSOC")].data; serato_struct.seratoVideoAssoc = Encoding.ASCII.GetString(serato_struct.seratoVideoAssocRaw);
                if (!string.IsNullOrEmpty(serato_struct.seratoOverview) ||
                   !string.IsNullOrEmpty(serato_struct.seratoAnalysis.data) ||
                   !string.IsNullOrEmpty(serato_struct.seratoMarkersV2.data)) FoundTag = true;
            }
            return FoundTag;
        }

        /// <summary>
        ///   Look for and parse Apple tags.
        /// </summary>
        /// <remarks>
        ///    The organization of the tags is different from the others.
        ///    Also because the name can be MixedInKey next to Serato (and maybe others).
        ///    These tags also don't start with '\u0001\u0001', so need to remove them for the others.
        /// </remarks>
        /// <returns>A <see cref="bool"/> noting if Serato tags are found</returns>
        /// ToDo: See if we can enumerate based on only the name and not the mean. Or will all MiK and Serato fields always be different...
        private bool SeratoReadApple()
        {
            TagLib.ByteVector DASH = "----";
            TagLib.ByteVector MEAN = "mean";
            bool FoundTag = false;
            if (mainForm.apple != null)
            {
                //Combinations I've found:
                //com.serato.dj - markers
                //com.serato.dj - markersv2
                //com.serato.dj - analysisVersion
                //com.serato.dj - autgain
                //com.serato.dj - beatgrid
                //com.serato.dj - overview
                //com.serato.dj - relvol
                //com.serato.dj - videoassociation
                //And 3 from MixedInKey - do these also exist in other formats?
                //com.mixedinkey.mixedinkey - cuepoints
                //com.mixedinkey.mixedinkey - key
                //com.mixedinkey.mixedinkey - energy

                //Seems to have \u0001 in front of the mean and name!
                //We do need to have a base64 decode helper
                serato_struct.seratoMarkers = mainForm.apple.GetDashBox("com.serato.dj", "markers");
                if (string.IsNullOrEmpty(serato_struct.seratoMarkers))
                    serato_struct.seratoMarkers = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001markers");
                if (!string.IsNullOrEmpty(serato_struct.seratoMarkers))
                {
                    serato_struct.seratoMarkersRaw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoMarkers)].data; serato_struct.seratoMarkers = Encoding.ASCII.GetString(serato_struct.seratoMarkersRaw);
                    FoundTag = true;
                }

                serato_struct.seratoMarkersV2.data = mainForm.apple.GetDashBox("com.serato.dj", "markersv2");
                if (string.IsNullOrEmpty(serato_struct.seratoMarkersV2.data))
                    serato_struct.seratoMarkersV2.data = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001markersv2");
                if (!string.IsNullOrEmpty(serato_struct.seratoMarkersV2.data))
                {
                    serato_struct.seratoMarkersV2.raw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoMarkersV2.data)].data; //serato_struct.seratoMarkersV2 = Encoding.ASCII.GetString(serato_struct.seratoMarkersV2Raw);
                    FoundTag = true;
                }

                serato_struct.seratoAutotags.data = mainForm.apple.GetDashBox("com.serato.dj", "autogain");
                if (string.IsNullOrEmpty(serato_struct.seratoAutotags.data))
                    serato_struct.seratoAutotags.data = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001autogain");
                if (!string.IsNullOrEmpty(serato_struct.seratoAutotags.data))
                {
                    serato_struct.seratoAutotags.raw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoAutotags.data)].data; //serato_struct.seratoAutogain = Encoding.ASCII.GetString(serato_struct.seratoAutogainRaw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                serato_struct.seratoAnalysis.data = mainForm.apple.GetDashBox("com.serato.dj", "analysisVersion");
                if (string.IsNullOrEmpty(serato_struct.seratoAnalysis.data))
                    serato_struct.seratoAnalysis.data = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001analysisVersion");
                if (!string.IsNullOrEmpty(serato_struct.seratoAnalysis.data))
                {
                    serato_struct.seratoAnalysis.raw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoAnalysis.data)].data;// serato_struct.seratoAnalysis = Encoding.ASCII.GetString(serato_struct.seratoAnalysis.raw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                serato_struct.seratoBeatgrid.data = mainForm.apple.GetDashBox("com.serato.dj", "beatgrid");
                if (string.IsNullOrEmpty(serato_struct.seratoBeatgrid.data))
                    serato_struct.seratoBeatgrid.data = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001beatgrid");
                if (!string.IsNullOrEmpty(serato_struct.seratoBeatgrid.data))
                {
                    serato_struct.seratoBeatgrid.raw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoBeatgrid.data)].data; //serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(serato_struct.seratoBeatgridRaw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                serato_struct.seratoOverview = mainForm.apple.GetDashBox("com.serato.dj", "overview");
                if (string.IsNullOrEmpty(serato_struct.seratoOverview))
                    serato_struct.seratoOverview = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001overview");
                if (!string.IsNullOrEmpty(serato_struct.seratoOverview))
                {
                    serato_struct.seratoOverviewRaw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoOverview)].data; serato_struct.seratoOverview = Encoding.ASCII.GetString(serato_struct.seratoOverviewRaw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                serato_struct.seratoRelVol = mainForm.apple.GetDashBox("com.serato.dj", "relvol");
                if (string.IsNullOrEmpty(serato_struct.seratoRelVol))
                    serato_struct.seratoRelVol = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001relvol");
                if (!string.IsNullOrEmpty(serato_struct.seratoRelVol))
                {
                    serato_struct.seratoRelVolRaw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoRelVol)].data; serato_struct.seratoRelVol = Encoding.ASCII.GetString(serato_struct.seratoRelVolRaw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                serato_struct.seratoVideoAssoc = mainForm.apple.GetDashBox("com.serato.dj", "videoassociation");
                if (string.IsNullOrEmpty(serato_struct.seratoVideoAssoc))
                    serato_struct.seratoVideoAssoc = mainForm.apple.GetDashBox("\u0001com.serato.dj", "\u0001videoassociation");
                if (!string.IsNullOrEmpty(serato_struct.seratoVideoAssoc))
                {
                    serato_struct.seratoVideoAssocRaw = serato_struct.dataRaw[DecodeSeratoApple(serato_struct.seratoVideoAssoc)].data; serato_struct.seratoVideoAssoc = Encoding.ASCII.GetString(serato_struct.seratoVideoAssocRaw);
                    FoundTag = true;
                }
                //                DecodeSeratoApple(serato_struct.seratoMarkers);

                //While Serato writes raw data, MixedInKey writes JSON structures.
                string cuepoints = mainForm.apple.GetDashBox("com.mixedinkey.mixedinkey", "cuepoints");
                if (string.IsNullOrEmpty(cuepoints))
                    cuepoints = mainForm.apple.GetDashBox("\u0001com.mixedinkey.mixedinkey", "\u0001cuepoints");
                string JSON = DecodeMixedInKeyApple(cuepoints);
                MixedInKey_cuepoints newCuepoints = Newtonsoft.Json.JsonConvert.DeserializeObject<MixedInKey_cuepoints>(JSON);

                string key = mainForm.apple.GetDashBox("com.mixedinkey.mixedinkey", "key");
                if (string.IsNullOrEmpty(key))
                    key = mainForm.apple.GetDashBox("\u0001com.mixedinkey.mixedinkey", "\u0001key");
                JSON = DecodeMixedInKeyApple(key);
                MixedInKey_key newKey = Newtonsoft.Json.JsonConvert.DeserializeObject<MixedInKey_key>(JSON);

                string energy = mainForm.apple.GetDashBox("com.mixedinkey.mixedinkey", "energy");
                if (string.IsNullOrEmpty(energy))
                    energy = mainForm.apple.GetDashBox("\u0001com.mixedinkey.mixedinkey", "\u0001energy");
                JSON = DecodeMixedInKeyApple(energy);
                MixedInKey_energy newEnergy = Newtonsoft.Json.JsonConvert.DeserializeObject<MixedInKey_energy>(JSON);

                FoundTag = true;
            }
            return FoundTag;
        }

        #endregion SeratoTag Read Helper Functions

        #region Base64 helper functions

        /// <summary>
        ///   Helper function to retrieve and base64-decode field.
        ///   Also splits description, type and data.
        /// </summary>
        /// <remarks>
        ///    Seems to fault on some base64 strings.
        /// </remarks>
        /// <returns>An <see cref="int"/> containing the position in
        /// <seealso cref="serato_struct.dataRaw"/> where the data is stored</returns>
        private int DecodeSeratoFlac(string FieldName)
        {
            string[] seratoInput = mainForm.ogg.GetField(FieldName);
            string result = string.Empty;
            Serato_struct.SeratoRaw newTag = new Serato_struct.SeratoRaw();
            byte[] RawData = new byte[0];
            if (!string.IsNullOrEmpty(seratoInput[0]))
            {
                try
                {
                    for (int i = 0; i < seratoInput.Length; i++)
                    {
                        if (i == 1) { throw new System.Exception("Only first string should be filled"); }
                        RawData = Convert.FromBase64String(ValidateBase64EncodedString(seratoInput[i]));
                        //                        string full = Encoding.Default.GetString(test);
                        //                        int end_of_string = full.IndexOf("\0");
                        //                        if( end_of_string > -1)
                        //                            newTag.Type = full.Substring(0, end_of_string);

                        result += Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(seratoInput[i])));
                        //                            ByteVector data = new ByteVector(Convert.FromBase64String(seratoInput[i]));
                        //                            seratoAnalysis += Encoding.ASCII.GetString(data.Data);
                        //string[] words = result.Split('\0');
                        //result = words[3]; //Seems to work for all but Video Assoc
                        //int end_of_type = result.IndexOf('\0', 0); //@ null-characters after type
                        //int end_of_header = result.IndexOf('\0', end_of_type+2);
                        //result = result.Substring(end_of_header + 1, result.Length - end_of_header - 2);
                    }
                    string temp = string.Empty;
                    newTag.data = new byte[0];
                    for (int i = 0; i < RawData.Length; i++)
                    {
                        if (string.IsNullOrEmpty(newTag.Name))
                        {
                            if (RawData[i] != '\0')
                            {
                                temp += ((char)RawData[i]).ToString();
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
                                        newTag.data = new byte[RawData.Length - i - 1];
                                        Array.Copy(RawData, i + 1, newTag.data, 0, RawData.Length - i - 2); //-2, remove first and last \0
                                        break;
                                    }
                                }
                                temp = string.Empty;
                            }
                        }
                    }
                    serato_struct.dataRaw.Add(newTag);
                    result = result.Substring(result.IndexOf("\0", result.IndexOf("Serato")) + 1);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Base64 decode " + FieldName + " failed: " + e.Message);
                }
            }

            //            return RawData.Skip();
            return serato_struct.dataRaw.Count - 1;
        }

        /// <summary>
        ///   Helper function to retrieve and base64-decode fields.
        ///   Also splits description, type and data.
        /// </summary>
        /// <returns>An <see cref="int"/> containing the position in
        /// <seealso cref="serato_struct.dataRaw"/> where the data is stored</returns>
        private int DecodeSeratoApple(string EncodedString, bool JSON = false)
        {
            string result = string.Empty;
            Serato_struct.SeratoRaw newTag = new Serato_struct.SeratoRaw();
            byte[] RawData = new byte[0];
            if (!string.IsNullOrEmpty(EncodedString))
            {
                try
                {
                    RawData = Convert.FromBase64String(ValidateBase64EncodedString(EncodedString));
                    result = Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(EncodedString)));
                    string temp = string.Empty;
                    newTag.data = new byte[0];
                    for (int i = 0; i < RawData.Length; i++)
                    {
                        if (string.IsNullOrEmpty(newTag.Name))
                        {
                            if (RawData[i] != '\0')
                            {
                                temp += ((char)RawData[i]).ToString();
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
                                        newTag.data = new byte[RawData.Length - i - 1];
                                        Array.Copy(RawData, i + 1, newTag.data, 0, RawData.Length - i - 2); //-2, remove first and last \0
                                        break;
                                    }
                                }
                                temp = string.Empty;
                            }
                        }
                    }
                    serato_struct.dataRaw.Add(newTag);
                    result = result.Substring(result.IndexOf("\0", result.IndexOf("Serato")) + 1);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Base64 decode " + EncodedString + " failed: " + e.Message);
                }
            }

            //            return RawData.Skip();
            return serato_struct.dataRaw.Count - 1;
        }

        private string DecodeMixedInKeyApple(string EncodedString)
        {
            if (!string.IsNullOrEmpty(EncodedString))
            {
                try
                {
                    string JSON = Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(EncodedString)));
                    return JSON;
                }
                catch
                { }
            }
            return string.Empty;
        }

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
                case 1: stringToValidate += "A=="; break; // For some reason, some encoders don't give an A at the end
                default:
                    throw new System.Exception("Illegal base64url string!");
            }

            return stringToValidate;
        }

        private static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
    }

    #endregion Base64 helper functions
}