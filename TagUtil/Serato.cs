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
            public struct SeratoRaw
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

            public struct Loops
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
            public int HighestMarker { set; get; }
            public Loops[] loops = new Loops[4];
            public int HighestLoop { set; get; }
            public string Color { set; get; }
            public string BPMLock { set; get; }
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

            if (mainForm.id3v2 != null)
            {
                foreach (TagLib.Id3v2.Frame f in mainForm.id3v2.GetFrames())
                {
                    if (f is TagLib.Id3v2.AttachmentFrame)
                    {
                        TagLib.Id3v2.AttachmentFrame tagSerato = ((TagLib.Id3v2.AttachmentFrame)f);
                        if (tagSerato.Description == "Serato Analysis")
                        {
                           serato_struct.seratoAnalysis = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Autotags")
                        {
                           serato_struct.seratoAutotags = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Autogain")
                        {
                           serato_struct.seratoAutogain = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato BeatGrid")
                        {
                           serato_struct.seratoBeatgrid = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Markers_")
                        {
                           serato_struct.seratoMarkers = tagSerato.Data.ToString();
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Markers2")
                        {
                           serato_struct.seratoMarkers = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                        if (tagSerato.Description == "Serato Overview")
                        {
                           serato_struct.seratoOverview = Encoding.ASCII.GetString(tagSerato.Data.Data);
                            bExists = true;
                        }
                    }
                }
            }
            if (mainForm.ogg != null)
            {
               serato_struct.seratoAnalysis = DecodeSeratoFlac("SERATO_ANALYSIS");
               serato_struct.seratoMarkers = DecodeSeratoFlac("SERATO_MARKERS_V2");
               serato_struct.seratoAutotags = DecodeSeratoFlac("SERATO_AUTOGAIN");
               serato_struct.seratoBeatgrid = DecodeSeratoFlac("SERATO_BEATGRID");
               serato_struct.seratoOverview = DecodeSeratoFlac("SERATO_OVERVIEW");
               serato_struct.seratoRelVol = DecodeSeratoFlac("SERATO_RELVOL");
               serato_struct.seratoVideoAssoc = DecodeSeratoFlac("SERATO_VIDEO_ASSOC");
                if (serato_struct.seratoOverview.Length > 0 ||
                   serato_struct.seratoAnalysis.Length > 0 ||
                   serato_struct.seratoMarkers.Length > 0) bExists = true;
            }


            // See https://stackoverflow.com/questions/41850029/string-parsing-techniques for parsing info

            //Convert tags more if needed
            foreach (var item in serato_struct.dataRaw)
            {
                if (item.Name == "Serato Markers2")
                {
                    string ToDecode = Encoding.ASCII.GetString(item.data);
                    ToDecode = ToDecode.Substring(2, ToDecode.IndexOf('\0') - 2);
                   serato_struct.seratoMarkers = Encoding.ASCII.GetString(Convert.FromBase64String(ValidateBase64EncodedString(ToDecode)));
                    //byte[] test = Convert.FromBase64String(ValidateBase64EncodedString(ToDecode));
                }
            }
            //Parse tags
            //ToDo maybe give each tag it's own function
            if (serato_struct.seratoAutotags.Length > 0)
            {
                double temp;
                string[] words =serato_struct.seratoAutotags.Substring(2).Split('\0');
                double.TryParse(words[0], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
               serato_struct.BPM = temp;// Convert.ToDouble(serato.seratoAutotags.Substring(2, serato.seratoAutotags.IndexOf('\0', 2) - 1));
                double.TryParse(words[1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
               serato_struct.tag2 = temp;
                double.TryParse(words[2], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
               serato_struct.tag3 = temp;
            }
            //Markers tag contains CUEs (and their color codes) and Loops
            //ToDo: find out what color does, can occur multiple times in tag
            if (serato_struct.seratoMarkers.Length > 0)
            {
                //                int nFoundPos = -1; //Skip 2 \u0001 start bytes used as separator
                //                int nStringPos = 2;
                int nEnd;
                for (int i = 2; i <serato_struct.seratoMarkers.Length; i++)
                {
                    string temp =serato_struct.seratoMarkers.Substring(i);
                    if (serato_struct.seratoMarkers.Substring(i).StartsWith("CUE")) //We have a CUE - 21 bytes without name
                    {
                        //4 bytes: CUE\0
                        //4 bytes: length
                        //1 byte: Cue number
                        //4 bytes: position (what format?)
                        //Position???
                        //CUE 0 0 0 0 13 0 0 0 0  0   0 0 63  0  0 0 0 0
                        //CUE 0 0 0 0 13 0 1 0 0  0   8 0 63 63  0 0 0 0
                        //CUE 0 0 0 0 13 0 2 0 0  0 100 0  0  0 63 0 0 0
                        //CUE 0 0 0 0 13 0 3 0 0  3  63 0 63 63  0 0 0 0
                        //CUE 0 0 0 0 18 0 4 0 0  0   0 0  0 63  0 0 0"0 sec" 0
                        //CUE 0 0 0 0 18 0 5 0 0 19  63 0 63  0 63 0 0"5 sec" 0
                        nEnd = serato_struct.seratoMarkers.IndexOf("\0", i + 20); //Look if there is a name at the end
                        if (nEnd == -1) break; //ToDo What to do here...
                       serato_struct.markers[serato_struct.HighestMarker].raw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, nEnd - i));
                        int nStringStart =serato_struct.seratoMarkers.LastIndexOf("\0", nEnd - 1);
                       serato_struct.markers[serato_struct.HighestMarker].Name =serato_struct.seratoMarkers.Substring(nStringStart + 1, nEnd - nStringStart - 1);
                       serato_struct.HighestMarker++;
                        i = nEnd;
                    }
                    if (serato_struct.seratoMarkers.Substring(i).StartsWith("LOOP")) //We have a LOOP - 30 bytes without name
                    {
                        nEnd =serato_struct.seratoMarkers.IndexOf("\0", i + 29); //Look if there is a name at the end
                        if (nEnd == -1) break; //ToDo What to do here...
                       serato_struct.loops[serato_struct.HighestLoop].raw = Encoding.ASCII.GetBytes(serato_struct.seratoMarkers.Substring(i, nEnd - i));
                        int nStringStart =serato_struct.seratoMarkers.LastIndexOf("\0", nEnd - 1);
                       serato_struct.loops[serato_struct.HighestLoop].Name =serato_struct.seratoMarkers.Substring(nStringStart + 1, nEnd - nStringStart - 1);
                       serato_struct.HighestLoop++;
                        i = nEnd;
                    }
                    if (serato_struct.seratoMarkers.Substring(i).StartsWith("COLOR")) //We have a COLOR code - 14 bytes
                    {
                        //Byte 10: Corresponding cue number?
                       serato_struct.Color =serato_struct.seratoMarkers.Substring(i, 14);
                        i += 13;
                    }
                    if (serato_struct.seratoMarkers.Substring(i).StartsWith("BPMLOCK")) //We have a BPMLOCK - 13 - 15 bytes???
                    {
                       serato_struct.BPMLock =serato_struct.seratoMarkers.Substring(i, Math.Min(15,serato_struct.seratoMarkers.Length - i));
                        i += 14;
                    }
                }
            }
            if (serato_struct.seratoAutogain.Length > 0)
            {
                double temp = 0.0;
                double.TryParse(serato_struct.seratoAutogain.Substring(2), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo, out temp);
               serato_struct.AutoGain = temp;
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
            string[] seratoInput = mainForm.ogg.GetField(FieldName);
            string result = string.Empty;
            Serato_struct.SeratoRaw newTag = new Serato_struct.SeratoRaw();
            byte[] test = new byte[0];
            if (seratoInput[0].Length > 0)
            {
                try
                {
                    for (int i = 0; i < seratoInput.Length; i++)
                    {
                        if (i == 1) { throw new System.Exception("Only first string should be filled"); }
                        test = Convert.FromBase64String(ValidateBase64EncodedString(seratoInput[i]));
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
                   serato_struct.dataRaw.Add(newTag);
                    result = result.Substring(result.IndexOf("\0", result.IndexOf("Serato")) + 1);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Base64 decode " + FieldName + " failed: " + e.Message);
                }
            }

            return result;
        }

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

        public static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        }
    }
    #endregion
}
