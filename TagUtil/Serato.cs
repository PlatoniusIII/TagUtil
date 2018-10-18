using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TagUtil
{
    /// <summary>
    ///    Class containing all Serato info. 
    /// </summary>
    /// <remarks>
    ///    Base64 encoding fails at times because it's missing an 'A'
    ///    at the end. So instead of 3 padding we add 'A=='.
    /// </remarks>
    public class Serato
    {
        MainForm mainForm;

        public Serato(MainForm parent)
        {
            mainForm = parent;
            serato_struct = new Serato_struct();
        }

        #region Struct containing all Serato related data
        /// <summary>
        ///   Class containing all Serato related information. 
        /// </summary>
        public class Serato_struct
        {
            /// <summary>
            ///   Possible tags in a Marker tag. 
            /// </summary>
            public string[] MarkerTags = { "CUE", "LOOP", "COLOR", "BPMLOCK" };
            /// <summary>
            ///   Default length of a Cue tag. 
            /// </summary>
            public const int TagCueLength = 20;
            /// <summary>
            ///   Default length of a Loop tag. 
            /// </summary>
            public const int TagLoopLength = 29;

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
            public struct CueMarkers
            {
                public void Init()
                {
                    Name = string.Empty;
                    DataSize = -1;
                    Position = 0;
                    Number = -1;
                }
                public string Name { set; get; }
                public byte[] raw { set; get; }
                public int DataSize { set; get; }
                public int Position { set; get; }
                public int Number { set; get; }
            }

            /// <summary>
            /// Struct that contains all Loop data
            /// </summary>
            public struct LoopMarkers
            {
                public void Init()
                {
                    Name = string.Empty;
                    DataSize = -1;
                    Number = -1;
                    PositionStart = 0;
                    PositionEnd = 0;
                }
                public string Name { set; get; }
                public byte[] raw { set; get; }
                public int DataSize { set; get; }
                public int PositionStart { set; get; }
                public int PositionEnd { set; get; }
                public int Number { set; get; }
            }

            public void Init()
            {
                seratoAnalysis = string.Empty;
                seratoAutogain = string.Empty;
                seratoAutotags = string.Empty;
                seratoBeatgrid = string.Empty;
                seratoMarkers = string.Empty;
                seratoMarkersRaw = new byte[0];
                seratoOverview = string.Empty;
                seratoRelVol = string.Empty;
                seratoVideoAssoc = string.Empty;

                BPM = 0.0;
                tag2 = 0.0;
                tag3 = 0.0;
                AutoGain = 0.0;

                HighestMarker = 0;
                HighestLoop = 0;

                dataRaw.Clear();

                for (int i = 0; i < markers.Length; i++)
                {
                    markers[i].Init();
                }
                for (int i = 0; i < loops.Length; i++)
                {
                    loops[i].Init();
                }
            }

            /// <summary>
            /// For now keep all data in both binary and string format
            /// </summary>
            public string seratoAnalysis { set; get; }
            public byte[] seratoAnalysisRaw { set; get; }
            public string seratoAutogain { set; get; }
            public byte[] seratoAutogainRaw { set; get; }
            public string seratoAutotags { set; get; }
            public byte[] seratoAutotagsRaw { set; get; }
            public string seratoBeatgrid { set; get; }
            public byte[] seratoBeatgridRaw { set; get; }
            public string seratoMarkers { set; get; }
            public byte[] seratoMarkersRaw { set; get; }
            public string seratoOverview { set; get; }
            public byte[] seratoOverviewRaw { set; get; }
            public string seratoRelVol { set; get; }
            public byte[] seratoRelVolRaw { set; get; }
            public string seratoVideoAssoc { set; get; }
            public byte[] seratoVideoAssocRaw { set; get; }

            public double BPM { set; get; }
            public double tag2 { set; get; } //Gain?
            public double tag3 { set; get; } //Only seen it as 0
            public CueMarkers[] markers = new CueMarkers[8];
            public int HighestMarker { set; get; }
            public LoopMarkers[] loops = new LoopMarkers[4];
            public int HighestLoop { set; get; }
            public byte[] Color { set; get; }
            public byte[] BPMLock { set; get; }
            public double AutoGain { set; get; }
            public List<SeratoRaw> dataRaw = new List<SeratoRaw>();
        }
        public Serato.Serato_struct serato_struct;
        #endregion

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
        ///          <description>Just 2 bytes or 1 short.
        ///          Seems to be 0x02 0x01.
        ///          Later tests show a 3rd value - 0x00 or 0x0C</description>
        ///       </item>
        ///       <item>
        ///          <term>Serato Autogain (FLAC)/Autotags (MP3)</term>
        ///          <term>application/octet-stream</term>
        ///          <description>Seems to contain the BPM and 2 more values</description>
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
        ///          <description>Guess it's the option to link to a video file, but FLAC only?</description>
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



            // See https://stackoverflow.com/questions/41850029/string-parsing-techniques for parsing info


            //Convert tags more if needed
            //foreach (var item in serato_struct.dataRaw)
            //{
            //    if (item.Name == "Serato Markers2")
            //    {
            //        string ToDecode = Encoding.ASCII.GetString(item.data);
            //        ToDecode = ToDecode.Substring(2, ToDecode.IndexOf('\0') - 2);
            //       serato_struct.seratoMarkersRaw = Convert.FromBase64String(ValidateBase64EncodedString(ToDecode));
            //       serato_struct.seratoMarkers = Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(ToDecode)));
            //        //byte[] test = Convert.FromBase64String(ValidateBase64EncodedString(ToDecode));
            //    }
            //}
            //Parse tags
            //ToDo maybe give each tag it's own function
            ParseSeratoAutotagsTag();
            ParseSeratoMarkersTag();
            ParseSeratoAutogainTag();

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
            if (serato_struct.seratoAutotags.Length > 0)
            {
                double temp;
                string[] words = serato_struct.seratoAutotags.Substring(2).Split('\0');
                double.TryParse(words[0], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.BPM = temp;// Convert.ToDouble(serato.seratoAutotags.Substring(2, serato.seratoAutotags.IndexOf('\0', 2) - 1));
                double.TryParse(words[1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.tag2 = temp;
                double.TryParse(words[2], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.tag3 = temp;
            }
        }

        /// <summary>
        ///   Parse the Serato Autogain tag
        /// </summary>
        /// <remarks>
        ///    Probably contains the autogain value
        /// </remarks>
        private void ParseSeratoAutogainTag()
        {
            if (serato_struct.seratoAutogain.Length > 0)
            {
                double temp = 0.0;
                double.TryParse(serato_struct.seratoAutogain.Substring(2), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
                serato_struct.AutoGain = temp;
            }
        }

        /// <summary>
        ///   Parse the Serato Markers V2 tag
        /// </summary>
        /// <remarks>
        ///    The Markers V2 tag contains cue points, loops,
        ///    color tags and a BPMLOCK tag.
        /// </remarks>
        private void ParseSeratoMarkersTag()
        {
            //Markers tag contains CUEs (and their color codes) and Loops
            //ToDo: find out what color does, can occur multiple times in tag
            if (serato_struct.seratoMarkers.Length > 0)
            {
                //Data part of the markers tag is itself again Base64 encoded
                string ToDecode = serato_struct.seratoMarkers.Substring(2, serato_struct.seratoMarkers.IndexOf('\0') - 2);
                serato_struct.seratoMarkersRaw = Convert.FromBase64String(ValidateBase64EncodedString(ToDecode));
                serato_struct.seratoMarkers = Encoding.ASCII.GetString(serato_struct.seratoMarkersRaw);
                //                int nFoundPos = -1; //Skip 2 \u0001 start bytes used as separator
                //                int nStringPos = 2;
                //int nEnd;
                Int32 Size;
                for (int i = 2; i < serato_struct.seratoMarkers.Length; i++)
                {
                    string temp = serato_struct.seratoMarkers.Substring(i);
                    foreach (var item in serato_struct.MarkerTags)
                    {
                        if (serato_struct.seratoMarkers.Substring(i).StartsWith(item))
                        {
                            Size = BitConverter.ToInt32(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1).Take(4).Reverse().ToArray(), 0);
                            switch (item)
                            {
                                case "CUE":
                                    //4 bytes: 'CUE\0'
                                    //4 bytes: length of data part
                                    //2 bytes: cue number
                                    //4 bytes: position
                                    //4 bytes: color??? (last always 0)
                                    //1 byte: '\0'
                                    //X bytes: Label of Cue + '\0'
                                    //CUE 0 0 0 0 13 0 0 0 0  0   0 0 204   0   0 0 0 0
                                    //CUE 0 0 0 0 13 0 1 0 0  0   8 0 204 136   0 0 0 0
                                    //CUE 0 0 0 0 13 0 2 0 0  0 100 0   0   0 204 0 0 0
                                    //CUE 0 0 0 0 13 0 3 0 0  3 234 0 204 204   0 0 0 0
                                    //CUE 0 0 0 0 18 0 4 0 0  0   0 0   0 204   0 0 0"0 sec" 0
                                    //CUE 0 0 0 0 18 0 5 0 0 19 136 0 204   0 204 0 0"5 sec" 0
                                    serato_struct.markers[serato_struct.HighestMarker].DataSize = Size;
                                    //serato_struct.markers[serato_struct.HighestMarker].raw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, 4+4+Size));
                                    serato_struct.markers[serato_struct.HighestMarker].raw = new byte[4 + item.Length + Size];
                                    Array.Copy(serato_struct.seratoMarkersRaw, i, serato_struct.markers[serato_struct.HighestMarker].raw, 0, 4 + item.Length + Size);
                                    serato_struct.markers[serato_struct.HighestMarker].Name = serato_struct.seratoMarkers.Substring(i + Serato_struct.TagCueLength, Size + 4 + item.Length - Serato_struct.TagCueLength);
                                    serato_struct.markers[serato_struct.HighestMarker].Number = BitConverter.ToInt16(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1 + 4).Take(2).Reverse().ToArray(), 0);
                                    serato_struct.markers[serato_struct.HighestMarker].Position = BitConverter.ToInt32(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1 + 4 + 2).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.HighestMarker++;
                                    i += (Size + item.Length + 4);
                                    break;
                                case "LOOP":
                                    //LOOP 0 0 0 0 21 0 0 0 0  1  65 0 0 14 148 255 255 255 255 0 39 170 250 0 0 0
                                    //LOOP 0 0 0 0 26 0 1 0 0 14 142 0 0 27 216 255 255 255 255 0 39 170 225 0 0"Loop2" 0
                                    serato_struct.loops[serato_struct.HighestLoop].DataSize = Size;
                                    serato_struct.loops[serato_struct.HighestLoop].raw = new byte[4 + item.Length + Size];
                                    Array.Copy(serato_struct.seratoMarkersRaw, i, serato_struct.loops[serato_struct.HighestLoop].raw, 0, 4 + item.Length + Size);
                                    serato_struct.loops[serato_struct.HighestLoop].Name = serato_struct.seratoMarkers.Substring(i + Serato_struct.TagLoopLength, Size + 4 + item.Length - Serato_struct.TagLoopLength);
                                    serato_struct.loops[serato_struct.HighestLoop].Number = BitConverter.ToInt16(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1 + 4).Take(2).Reverse().ToArray(), 0);
                                    serato_struct.loops[serato_struct.HighestLoop].PositionStart = BitConverter.ToInt32(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1 + 4 + 2).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.loops[serato_struct.HighestLoop].PositionEnd = BitConverter.ToInt32(serato_struct.seratoMarkersRaw.Skip(i + item.Length + 1 + 4 + 2 + 4).Take(4).Reverse().ToArray(), 0);
                                    serato_struct.HighestLoop++;
                                    i += (Size + item.Length + 4);
                                    break;
                                case "COLOR":
                                    serato_struct.Color = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, 4 + item.Length + Size));
                                    //serato_struct.loops[serato_struct.HighestLoop].DataSize = Size;
                                    //serato_struct.loops[serato_struct.HighestLoop].raw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, 4 + 4 + Size));
                                    //serato_struct.loops[serato_struct.HighestLoop].Name = serato_struct.seratoMarkers.Substring(i + 20, Size + 4 + 4 - 21);
                                    //serato_struct.HighestLoop++;
                                    i += (Size + item.Length + 4);
                                    break;
                                case "BPMLOCK":
                                    //serato_struct.loops[serato_struct.HighestLoop].DataSize = Size;
                                    serato_struct.BPMLock = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, 4 + item.Length + Size));
                                    //serato_struct.loops[serato_struct.HighestLoop].Name = serato_struct.seratoMarkers.Substring(i + 20, Size + 4 + 4 - 21);
                                    //serato_struct.HighestLoop++;
                                    i += (Size + item.Length + 4);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
        #endregion

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
            if (seratoInput[0].Length > 0)
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
            return serato_struct.dataRaw.Count-1;
        }

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
                            serato_struct.seratoAnalysisRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoAnalysisRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoAnalysis = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Autotags")
                        {
                            serato_struct.seratoAutotagsRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoAutotagsRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoAutotags = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Autogain")
                        {
                            serato_struct.seratoAutogainRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoAutogainRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoAutogain = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato BeatGrid")
                        {
                            serato_struct.seratoBeatgridRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoBeatgridRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Markers_")
                        {
                            serato_struct.seratoMarkersRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoMarkersRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoMarkers = tagSerato.Data.ToString();
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Markers2")
                        {
                            serato_struct.seratoMarkersRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoMarkersRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoMarkers = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            FoundTag = true;
                        }
                        if (tagSerato.Description == "Serato Overview")
                        {
                            serato_struct.seratoOverviewRaw = new byte[tagSerato.Data.Data.Length];
                            Array.Copy(serato_struct.seratoOverviewRaw, tagSerato.Data.Data, tagSerato.Data.Data.Length);
                            serato_struct.seratoOverview = Encoding.ASCII.GetString(tagSerato.Data.Data);
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
                serato_struct.seratoAnalysisRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_ANALYSIS")].data; serato_struct.seratoAnalysis = Encoding.ASCII.GetString(serato_struct.seratoAnalysisRaw);
                serato_struct.seratoMarkersRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_MARKERS_V2")].data; serato_struct.seratoMarkers = Encoding.ASCII.GetString(serato_struct.seratoMarkersRaw);
                serato_struct.seratoAutotagsRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_AUTOGAIN")].data; serato_struct.seratoAutotags = Encoding.ASCII.GetString(serato_struct.seratoAutotagsRaw);
                serato_struct.seratoBeatgridRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_BEATGRID")].data; serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(serato_struct.seratoBeatgridRaw);
                serato_struct.seratoOverviewRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_OVERVIEW")].data; serato_struct.seratoOverview = Encoding.ASCII.GetString(serato_struct.seratoOverviewRaw);
                serato_struct.seratoRelVolRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_RELVOL")].data; serato_struct.seratoRelVol = Encoding.ASCII.GetString(serato_struct.seratoRelVolRaw);
                serato_struct.seratoVideoAssocRaw = serato_struct.dataRaw[DecodeSeratoFlac("SERATO_VIDEO_ASSOC")].data; serato_struct.seratoVideoAssoc = Encoding.ASCII.GetString(serato_struct.seratoVideoAssocRaw);
                if (serato_struct.seratoOverview.Length > 0 ||
                   serato_struct.seratoAnalysis.Length > 0 ||
                   serato_struct.seratoMarkers.Length > 0) FoundTag = true;
            }
            return FoundTag;
        }

        #endregion
        #region Base64 helper functions
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
                case 1:
                    if (stringToValidate[stringToValidate.Length - 1] == 'A')
                        stringToValidate += "A==";
                    else throw new System.Exception("Illegal base64url string!");
                    break; // No pad chars in this case
                default:
                    throw new System.Exception(
             "Illegal base64url string!");
            }

            return stringToValidate;
        }

        private static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
    }
    #endregion
}
