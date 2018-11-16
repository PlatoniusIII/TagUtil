using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TagUtil
{
    /// <summary>
    /// Class for parsing and writing CUE sheets
    /// </summary>
    /// <see href="https://www.gnu.org/software/ccd2cue/manual/html_node/CUE-sheet-format.html"/>
    class CueSheets
    {
        private MainForm mainForm;

        public class Timecode
        {
            private static readonly Regex TimecodeRegex = new Regex(@"^(?<hours>\d{1,2}):(?<minutes>\d{1,2}):(?<seconds>\d{1,2}):(?<frames>\d{1,3})$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            private static readonly Regex TimecodeRegexNoHours = new Regex(@"^(?<minutes>\d{1,2}):(?<seconds>\d{1,2}):(?<frames>\d{1,3})$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

            /// <summary>
            /// The hours segment of the timecode.
            /// </summary>
            public int Hours { get; private set; }

            /// <summary>
            /// The minutes segment of the timecode.
            /// </summary>
            public int Minutes { get; private set; }

            /// <summary>
            /// The seconds segment of the timecode.
            /// </summary>
            public int Seconds { get; private set; }

            /// <summary>
            /// The frames segment of the timecode.
            /// </summary>
            public int Frames { get; private set; }

            private static string PadTimecodeUnit(int unit, int places = 2)
            {
                return unit.ToString().PadLeft(places, '0');
            }

            public override string ToString()
            {
                return string.Format("{0}:{1}:{2}:{3}",
                    PadTimecodeUnit(Hours),
                    PadTimecodeUnit(Minutes),
                    PadTimecodeUnit(Seconds),
                    PadTimecodeUnit(Frames));
            }

            /// <summary>
            /// Parses a timecode string of the format "hh:mm:ss:ff".
            /// </summary>
            /// <param name="timecodeStr"></param>
            /// <returns></returns>
            public void Parse(string timecodeStr)
            {
                GroupCollection captureGroups;
                switch (timecodeStr.Count(f => f == ':'))
                {
                    case 2:
                        captureGroups = TimecodeRegexNoHours.Match(timecodeStr.Trim(' ')).Groups;
                        break;
                    case 3:
                        captureGroups = TimecodeRegex.Match(timecodeStr.Trim(' ')).Groups;
                        Hours = int.Parse(captureGroups["hours"].Value);
                        break;
                    default:
                        throw new FormatException();
                }
                Minutes = int.Parse(captureGroups["minutes"].Value);
                Seconds = int.Parse(captureGroups["seconds"].Value);
                Frames = int.Parse(captureGroups["frames"].Value);

//                return new Timecode(hours, minutes, seconds, frames);
            }

        }

        public class CueSheet //Context: None
        {
            public CueSheet()
            {
                Files = new List<CueSheetFile>();
                Comment = new List<string>();
            }

            /// <summary>Define MCN (Media Catalog Number) of the disc as mcn. The argument mcn is a number composed of 13 decimal digits in UPC/EAN.</summary>
            public string Catalog { get; set; }
            /// <summary>CDTextFile contains the file for CDText.</summary>
            public string CdTextFile { get; set; }
            /// <summary>Title contains the title. It comes after Catalog and CDTextFile and before File.</summary>
            public string Title { get; set; }
            /// <summary>Performer contains the artist. It comes after Catalog and CDTextFile and before File.</summary>
            public string Performer { get; set; }
            /// <summary>Songwriter contains the songwriter. It comes after Catalog and CDTextFile and before File.</summary>
            public string Songwriter { get; set; }

            public string DiscID { get; set; }
            public string Date { get; set; }
            public int Discnumber { get; set; }
            public int TotalDiscs { get; set; }
            public string Flags { get; set; }
            public string Genre { get; set; }
            public List<CueSheetFile> Files;
            public List<string> Comment;
        }

        public class CueSheetFile //Context: File
        {
            public enum FileType
            {
                NONE, BINARY, MOTOROLA, AIFF, WAV, MP3
            };

            public CueSheetFile()
            {
                Tracks = new List<CueSheetTrack>();
                fileType = FileType.NONE;
                Reset();
            }

            public void Reset()
            {
                Filename = string.Empty;
                Tracks.Clear();
            }

            public string Filename { get; set; }
            public FileType fileType { get; set; }
            public List<CueSheetTrack> Tracks { get; set; }
        }

        public class CueSheetTrack //Context: Track
        {
            public CueSheetTrack()
            {
                Indexes = new List<CueSheetIndex>();
                Reset();
            }

            public void Reset()
            {
                TrackNumber = 0;
                Type = string.Empty;
                Performer = string.Empty;
                Title = string.Empty;
                Songwriter = string.Empty;
                PreGap = string.Empty;
                PostGap = string.Empty;
                ISRC = string.Empty;
                Indexes.Clear();
            }

            /// <summary>Title contains the title. It should come before the Index keyword</summary>
            public string Title { get; set; }
            /// <summary>Performer contains the artist. It should come before the Index keyword</summary>
            public string Performer { get; set; }
            /// <summary>Songwriter contains the songwriter. It should come before the Index keyword</summary>
            public string Songwriter { get; set; }

            public int TrackNumber { get; set; }
            public string Type; //ToDo: Enumeration
            public string PreGap { get; set; }
            public string PostGap { get; set; }
            public string ISRC { get; set; }
            public List<CueSheetIndex> Indexes;
        }

        public class CueSheetIndex
        {
            public CueSheetIndex()
            {
                Reset();
            }

            public void Reset()
            {
                IndexNumber = -1;
                Time = new Timecode();
            }

            public int IndexNumber { get; set; }
            public Timecode Time { get; set; }
        }

        public CueSheets(MainForm parent)
        {
            mainForm = parent;
        }

        private enum CueSheetContext { NONE, FILE, TRACK };

        /// <summary>
        /// Parse and store cue sheet. But how to handle comments... they should be stored in the corresponding section so that
        /// writing the cue sheet will still have all remarks at the right place.
        /// But should we remove remarks to note other creation programs?
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public CueSheet ParseCueSheet(string Filename)
        {
            CueSheet cueSheet = new CueSheet();
            CueSheetFile cueFile = new CueSheetFile();
            CueSheetTrack cueTrack = new CueSheetTrack();
            CueSheetIndex cueIndex = new CueSheetIndex();
            CueSheetContext cueContext = CueSheetContext.NONE;

            int Temp;

            string[] lines = System.IO.File.ReadAllLines(Filename);

            foreach (var item in lines)
            {
                if(cueContext == CueSheetContext.FILE)
                {
                    if (item.TrimStart(' ').StartsWith("TRACK", true, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        cueContext = CueSheetContext.TRACK;
                        if (cueTrack.TrackNumber > 0) cueFile.Tracks.Add(cueTrack);
                        cueTrack.Reset();
                        int Start = item.IndexOf(' ', item.IndexOf("TRACK"));
                        int End = item.LastIndexOf(' ');
                        int.TryParse(item.Substring(Start, End - Start), out Temp);
                        cueTrack.TrackNumber = Temp;
                    }
                    if (item.TrimStart(' ').StartsWith("INDEX", true, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        if (cueIndex.IndexNumber > 0) cueTrack.Indexes.Add(cueIndex);
                        cueIndex.Reset();
                        int Start = item.IndexOf(' ', item.IndexOf("INDEX"));
                        int End = item.LastIndexOf(' ');
                        int.TryParse(item.Substring(Start, End - Start), out Temp);
                        cueIndex.IndexNumber = Temp;
                        cueIndex.Time.Parse(item.Substring(End));
                    }
                    if (item.TrimStart(' ').StartsWith("PERFORMER", true, System.Globalization.CultureInfo.CurrentCulture))
                    {
                        int Start = item.IndexOf(' ', item.IndexOf("PERFORMER"));
                    }
                }
                if (item.StartsWith("rem ", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    string[] words = item.Split(' ');
                    switch (words[1].ToUpper())
                    {
                        case "DISCID":
                            cueSheet.DiscID = words[2];
                            break;
                        case "DISCNUMBER":
                            int.TryParse(words[2], out Temp);
                            cueSheet.Discnumber = Temp;
                            break;
                        case "TOTALDISCS":
                            int.TryParse(words[2], out Temp);
                            cueSheet.TotalDiscs = Temp;
                            break;
                        default:
                            break;
                    }
                }
                if (item.StartsWith("PERFORMER ", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    cueSheet.Performer = item.Substring(item.IndexOf(' '));
                }
                if (item.StartsWith("TITLE ", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    cueSheet.Title = item.Substring(item.IndexOf(' '));
                }
                if (item.StartsWith("FILE ", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    if (!string.IsNullOrEmpty(cueFile.Filename)) cueSheet.Files.Add(cueFile);
                    cueFile.Reset();
                    cueFile.Filename = item.Substring(item.IndexOf(' '));
                    cueContext = CueSheetContext.FILE;
                }
            }
            //Add the final file
            if (cueIndex.IndexNumber > 0) cueTrack.Indexes.Add(cueIndex);
            if (cueTrack.TrackNumber > 0) cueFile.Tracks.Add(cueTrack);
            if (!string.IsNullOrEmpty(cueFile.Filename)) cueSheet.Files.Add(cueFile);
            return cueSheet;
        }

        /// <summary>
        /// Generates cue sheet. Need to figure out how to get the data there.
        /// Probably 2 options:
        /// - create cue sheet from selected a range of files,
        /// - create cue sheet for single file, probably using tracklist from Discogs.
        /// </summary>
        /// <param name="Filename"></param>
        public void GenerateCueSheet(string Filename)
        {
            try
            {
                TagLib.File tagFile = mainForm.currentFile; // track is the name of the mp3
                List<string> lines = new List<string>();
                lines.Add("REM Created by TagUtil");
                //Order:
                //CATALOG
                //CDTEXTFILE
                //Below 3 may be in any order
                //TITLE
                //PERFORMER
                //SONGWRITER
                //
                //FILE
                //
                //TRACK
                //with children
                //FLAGS
                //ISRC
                //TITLE
                //PERFORMER
                //SONGWRITER
                //PREGAP
                //INDEX
                //POSTGAP
                System.IO.File.WriteAllLines(@"C:\Users\Public\TestFolder\WriteLines.txt", lines);
            }
            catch
            {
            }
        }
    }
}
