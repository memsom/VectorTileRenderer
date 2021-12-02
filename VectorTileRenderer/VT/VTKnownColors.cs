using System;
using System.Collections.Generic;
using System.Drawing;

namespace VectorTileRenderer
{
    // this is taken from the WPF source, and smashes some of Parses code in to it too
    internal static class VTKnownColors
    {

        static VTKnownColors()
        {
            Array knownColorValues = Enum.GetValues(typeof(VTKnownColor));
            foreach (VTKnownColor colorValue in knownColorValues)
            {
                string aRGBString = String.Format("#{0,8:X8}", (uint)colorValue);
                s_knownArgbColors[aRGBString] = colorValue;
            }
        }


        static internal string MatchColor(string colorString, out bool isKnownColor, out bool isNumericColor, out bool isContextColor, out bool isScRgbColor)
        {
            string trimmedString = colorString.Trim();

            if (((trimmedString.Length == 4) ||
                (trimmedString.Length == 5) ||
                (trimmedString.Length == 7) ||
                (trimmedString.Length == 9)) &&
                (trimmedString[0] == '#'))
            {
                isNumericColor = true;
                isScRgbColor = false;
                isKnownColor = false;
                isContextColor = false;
                return trimmedString;
            }
            else
                isNumericColor = false;

            if ((trimmedString.StartsWith("sc#", StringComparison.Ordinal) == true))
            {
                isNumericColor = false;
                isScRgbColor = true;
                isKnownColor = false;
                isContextColor = false;
            }
            else
            {
                isScRgbColor = false;
            }

            if ((trimmedString.StartsWith(s_ContextColor, StringComparison.OrdinalIgnoreCase) == true))
            {
                isContextColor = true;
                isScRgbColor = false;
                isKnownColor = false;
                return trimmedString;
            }
            else
            {
                isContextColor = false;
                isKnownColor = true;
            }

            return trimmedString;
        }


        internal static Color ParseColor(string color)
        {
            bool isPossibleKnowColor;
            bool isNumericColor;
            bool isScRgbColor;
            bool isContextColor;
            string trimmedColor = VTKnownColors.MatchColor(color, out isPossibleKnowColor, out isNumericColor, out isContextColor, out isScRgbColor);

            if ((isPossibleKnowColor == false) &&
                (isNumericColor == false) &&
                (isScRgbColor == false) &&
                (isContextColor == false))
            {
                throw new FormatException("Bad colour format");
            }

            //Is it a number?
            if (isNumericColor)
            {
                return ParseHexColor(trimmedColor);
            }
            else
            {
                VTKnownColor kc = VTKnownColors.ColorStringToKnownColor(trimmedColor);

                if (kc == VTKnownColor.UnknownColor)
                {
                    throw new FormatException("Bad format");
                }

                return Color.FromArgb((int)kc);
            }
        }

        const int s_zeroChar = (int)'0';
        const int s_aLower = (int)'a';
        const int s_aUpper = (int)'A';

        static private int ParseHexChar(char c)
        {
            int intChar = (int)c;

            if ((intChar >= s_zeroChar) && (intChar <= (s_zeroChar + 9)))
            {
                return (intChar - s_zeroChar);
            }

            if ((intChar >= s_aLower) && (intChar <= (s_aLower + 5)))
            {
                return (intChar - s_aLower + 10);
            }

            if ((intChar >= s_aUpper) && (intChar <= (s_aUpper + 5)))
            {
                return (intChar - s_aUpper + 10);
            }
            throw new FormatException("Bad format");
        }

        static private Color ParseHexColor(string trimmedColor)
        {
            int a, r, g, b;
            a = 255;

            if (trimmedColor.Length > 7)
            {
                a = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                r = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
                b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
            }
            else if (trimmedColor.Length > 5)
            {
                r = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                g = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
            }
            else if (trimmedColor.Length > 4)
            {
                a = ParseHexChar(trimmedColor[1]);
                a = a + a * 16;
                r = ParseHexChar(trimmedColor[2]);
                r = r + r * 16;
                g = ParseHexChar(trimmedColor[3]);
                g = g + g * 16;
                b = ParseHexChar(trimmedColor[4]);
                b = b + b * 16;
            }
            else
            {
                r = ParseHexChar(trimmedColor[1]);
                r = r + r * 16;
                g = ParseHexChar(trimmedColor[2]);
                g = g + g * 16;
                b = ParseHexChar(trimmedColor[3]);
                b = b + b * 16;
            }

            return (Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
        }

        internal const string s_ContextColor = "ContextColor ";
        internal const string s_ContextColorNoSpace = "ContextColor";


        /// Return the VTKnownColor from a color string.  If there's no match, VTKnownColor.UnknownColor
        internal static VTKnownColor ColorStringToKnownColor(string colorString)
        {
            if (null != colorString)
            {
                // We use invariant culture because we don't globalize our color names
                string colorUpper = colorString.ToUpper(System.Globalization.CultureInfo.InvariantCulture);

                // Use String.Equals because it does explicit equality
                // StartsWith/EndsWith are culture sensitive and are 4-7 times slower than Equals

                switch (colorUpper.Length)
                {
                    case 3:
                        if (colorUpper.Equals("RED")) return VTKnownColor.Red;
                        if (colorUpper.Equals("TAN")) return VTKnownColor.Tan;
                        break;
                    case 4:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AQUA")) return VTKnownColor.Aqua;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BLUE")) return VTKnownColor.Blue;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CYAN")) return VTKnownColor.Cyan;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GOLD")) return VTKnownColor.Gold;
                                if (colorUpper.Equals("GRAY")) return VTKnownColor.Gray;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIME")) return VTKnownColor.Lime;
                                break;
                            case 'N':
                                if (colorUpper.Equals("NAVY")) return VTKnownColor.Navy;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PERU")) return VTKnownColor.Peru;
                                if (colorUpper.Equals("PINK")) return VTKnownColor.Pink;
                                if (colorUpper.Equals("PLUM")) return VTKnownColor.Plum;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SNOW")) return VTKnownColor.Snow;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TEAL")) return VTKnownColor.Teal;
                                break;
                        }
                        break;
                    case 5:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AZURE")) return VTKnownColor.Azure;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BEIGE")) return VTKnownColor.Beige;
                                if (colorUpper.Equals("BLACK")) return VTKnownColor.Black;
                                if (colorUpper.Equals("BROWN")) return VTKnownColor.Brown;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CORAL")) return VTKnownColor.Coral;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GREEN")) return VTKnownColor.Green;
                                break;
                            case 'I':
                                if (colorUpper.Equals("IVORY")) return VTKnownColor.Ivory;
                                break;
                            case 'K':
                                if (colorUpper.Equals("KHAKI")) return VTKnownColor.Khaki;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LINEN")) return VTKnownColor.Linen;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLIVE")) return VTKnownColor.Olive;
                                break;
                            case 'W':
                                if (colorUpper.Equals("WHEAT")) return VTKnownColor.Wheat;
                                if (colorUpper.Equals("WHITE")) return VTKnownColor.White;
                                break;
                        }
                        break;
                    case 6:
                        switch (colorUpper[0])
                        {
                            case 'B':
                                if (colorUpper.Equals("BISQUE")) return VTKnownColor.Bisque;
                                break;
                            case 'I':
                                if (colorUpper.Equals("INDIGO")) return VTKnownColor.Indigo;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MAROON")) return VTKnownColor.Maroon;
                                break;
                            case 'O':
                                if (colorUpper.Equals("ORANGE")) return VTKnownColor.Orange;
                                if (colorUpper.Equals("ORCHID")) return VTKnownColor.Orchid;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PURPLE")) return VTKnownColor.Purple;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SALMON")) return VTKnownColor.Salmon;
                                if (colorUpper.Equals("SIENNA")) return VTKnownColor.Sienna;
                                if (colorUpper.Equals("SILVER")) return VTKnownColor.Silver;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TOMATO")) return VTKnownColor.Tomato;
                                break;
                            case 'V':
                                if (colorUpper.Equals("VIOLET")) return VTKnownColor.Violet;
                                break;
                            case 'Y':
                                if (colorUpper.Equals("YELLOW")) return VTKnownColor.Yellow;
                                break;
                        }
                        break;
                    case 7:
                        switch (colorUpper[0])
                        {
                            case 'C':
                                if (colorUpper.Equals("CRIMSON")) return VTKnownColor.Crimson;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKRED")) return VTKnownColor.DarkRed;
                                if (colorUpper.Equals("DIMGRAY")) return VTKnownColor.DimGray;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FUCHSIA")) return VTKnownColor.Fuchsia;
                                break;
                            case 'H':
                                if (colorUpper.Equals("HOTPINK")) return VTKnownColor.HotPink;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MAGENTA")) return VTKnownColor.Magenta;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLDLACE")) return VTKnownColor.OldLace;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SKYBLUE")) return VTKnownColor.SkyBlue;
                                break;
                            case 'T':
                                if (colorUpper.Equals("THISTLE")) return VTKnownColor.Thistle;
                                break;
                        }
                        break;
                    case 8:
                        switch (colorUpper[0])
                        {
                            case 'C':
                                if (colorUpper.Equals("CORNSILK")) return VTKnownColor.Cornsilk;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKBLUE")) return VTKnownColor.DarkBlue;
                                if (colorUpper.Equals("DARKCYAN")) return VTKnownColor.DarkCyan;
                                if (colorUpper.Equals("DARKGRAY")) return VTKnownColor.DarkGray;
                                if (colorUpper.Equals("DEEPPINK")) return VTKnownColor.DeepPink;
                                break;
                            case 'H':
                                if (colorUpper.Equals("HONEYDEW")) return VTKnownColor.Honeydew;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LAVENDER")) return VTKnownColor.Lavender;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MOCCASIN")) return VTKnownColor.Moccasin;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SEAGREEN")) return VTKnownColor.SeaGreen;
                                if (colorUpper.Equals("SEASHELL")) return VTKnownColor.SeaShell;
                                break;
                        }
                        break;
                    case 9:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("ALICEBLUE")) return VTKnownColor.AliceBlue;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BURLYWOOD")) return VTKnownColor.BurlyWood;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CADETBLUE")) return VTKnownColor.CadetBlue;
                                if (colorUpper.Equals("CHOCOLATE")) return VTKnownColor.Chocolate;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKGREEN")) return VTKnownColor.DarkGreen;
                                if (colorUpper.Equals("DARKKHAKI")) return VTKnownColor.DarkKhaki;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FIREBRICK")) return VTKnownColor.Firebrick;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GAINSBORO")) return VTKnownColor.Gainsboro;
                                if (colorUpper.Equals("GOLDENROD")) return VTKnownColor.Goldenrod;
                                break;
                            case 'I':
                                if (colorUpper.Equals("INDIANRED")) return VTKnownColor.IndianRed;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LAWNGREEN")) return VTKnownColor.LawnGreen;
                                if (colorUpper.Equals("LIGHTBLUE")) return VTKnownColor.LightBlue;
                                if (colorUpper.Equals("LIGHTCYAN")) return VTKnownColor.LightCyan;
                                if (colorUpper.Equals("LIGHTGRAY")) return VTKnownColor.LightGray;
                                if (colorUpper.Equals("LIGHTPINK")) return VTKnownColor.LightPink;
                                if (colorUpper.Equals("LIMEGREEN")) return VTKnownColor.LimeGreen;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MINTCREAM")) return VTKnownColor.MintCream;
                                if (colorUpper.Equals("MISTYROSE")) return VTKnownColor.MistyRose;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLIVEDRAB")) return VTKnownColor.OliveDrab;
                                if (colorUpper.Equals("ORANGERED")) return VTKnownColor.OrangeRed;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PALEGREEN")) return VTKnownColor.PaleGreen;
                                if (colorUpper.Equals("PEACHPUFF")) return VTKnownColor.PeachPuff;
                                break;
                            case 'R':
                                if (colorUpper.Equals("ROSYBROWN")) return VTKnownColor.RosyBrown;
                                if (colorUpper.Equals("ROYALBLUE")) return VTKnownColor.RoyalBlue;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SLATEBLUE")) return VTKnownColor.SlateBlue;
                                if (colorUpper.Equals("SLATEGRAY")) return VTKnownColor.SlateGray;
                                if (colorUpper.Equals("STEELBLUE")) return VTKnownColor.SteelBlue;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TURQUOISE")) return VTKnownColor.Turquoise;
                                break;
                        }
                        break;
                    case 10:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AQUAMARINE")) return VTKnownColor.Aquamarine;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BLUEVIOLET")) return VTKnownColor.BlueViolet;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CHARTREUSE")) return VTKnownColor.Chartreuse;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKORANGE")) return VTKnownColor.DarkOrange;
                                if (colorUpper.Equals("DARKORCHID")) return VTKnownColor.DarkOrchid;
                                if (colorUpper.Equals("DARKSALMON")) return VTKnownColor.DarkSalmon;
                                if (colorUpper.Equals("DARKVIOLET")) return VTKnownColor.DarkViolet;
                                if (colorUpper.Equals("DODGERBLUE")) return VTKnownColor.DodgerBlue;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GHOSTWHITE")) return VTKnownColor.GhostWhite;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTCORAL")) return VTKnownColor.LightCoral;
                                if (colorUpper.Equals("LIGHTGREEN")) return VTKnownColor.LightGreen;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMBLUE")) return VTKnownColor.MediumBlue;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PAPAYAWHIP")) return VTKnownColor.PapayaWhip;
                                if (colorUpper.Equals("POWDERBLUE")) return VTKnownColor.PowderBlue;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SANDYBROWN")) return VTKnownColor.SandyBrown;
                                break;
                            case 'W':
                                if (colorUpper.Equals("WHITESMOKE")) return VTKnownColor.WhiteSmoke;
                                break;
                        }
                        break;
                    case 11:
                        switch (colorUpper[0])
                        {
                            case 'D':
                                if (colorUpper.Equals("DARKMAGENTA")) return VTKnownColor.DarkMagenta;
                                if (colorUpper.Equals("DEEPSKYBLUE")) return VTKnownColor.DeepSkyBlue;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FLORALWHITE")) return VTKnownColor.FloralWhite;
                                if (colorUpper.Equals("FORESTGREEN")) return VTKnownColor.ForestGreen;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GREENYELLOW")) return VTKnownColor.GreenYellow;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSALMON")) return VTKnownColor.LightSalmon;
                                if (colorUpper.Equals("LIGHTYELLOW")) return VTKnownColor.LightYellow;
                                break;
                            case 'N':
                                if (colorUpper.Equals("NAVAJOWHITE")) return VTKnownColor.NavajoWhite;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SADDLEBROWN")) return VTKnownColor.SaddleBrown;
                                if (colorUpper.Equals("SPRINGGREEN")) return VTKnownColor.SpringGreen;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TRANSPARENT")) return VTKnownColor.Transparent;
                                break;
                            case 'Y':
                                if (colorUpper.Equals("YELLOWGREEN")) return VTKnownColor.YellowGreen;
                                break;
                        }
                        break;
                    case 12:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("ANTIQUEWHITE")) return VTKnownColor.AntiqueWhite;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKSEAGREEN")) return VTKnownColor.DarkSeaGreen;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSKYBLUE")) return VTKnownColor.LightSkyBlue;
                                if (colorUpper.Equals("LEMONCHIFFON")) return VTKnownColor.LemonChiffon;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMORCHID")) return VTKnownColor.MediumOrchid;
                                if (colorUpper.Equals("MEDIUMPURPLE")) return VTKnownColor.MediumPurple;
                                if (colorUpper.Equals("MIDNIGHTBLUE")) return VTKnownColor.MidnightBlue;
                                break;
                        }
                        break;
                    case 13:
                        switch (colorUpper[0])
                        {
                            case 'D':
                                if (colorUpper.Equals("DARKSLATEBLUE")) return VTKnownColor.DarkSlateBlue;
                                if (colorUpper.Equals("DARKSLATEGRAY")) return VTKnownColor.DarkSlateGray;
                                if (colorUpper.Equals("DARKGOLDENROD")) return VTKnownColor.DarkGoldenrod;
                                if (colorUpper.Equals("DARKTURQUOISE")) return VTKnownColor.DarkTurquoise;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSEAGREEN")) return VTKnownColor.LightSeaGreen;
                                if (colorUpper.Equals("LAVENDERBLUSH")) return VTKnownColor.LavenderBlush;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PALEGOLDENROD")) return VTKnownColor.PaleGoldenrod;
                                if (colorUpper.Equals("PALETURQUOISE")) return VTKnownColor.PaleTurquoise;
                                if (colorUpper.Equals("PALEVIOLETRED")) return VTKnownColor.PaleVioletRed;
                                break;
                        }
                        break;
                    case 14:
                        switch (colorUpper[0])
                        {
                            case 'B':
                                if (colorUpper.Equals("BLANCHEDALMOND")) return VTKnownColor.BlanchedAlmond;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CORNFLOWERBLUE")) return VTKnownColor.CornflowerBlue;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKOLIVEGREEN")) return VTKnownColor.DarkOliveGreen;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSLATEGRAY")) return VTKnownColor.LightSlateGray;
                                if (colorUpper.Equals("LIGHTSTEELBLUE")) return VTKnownColor.LightSteelBlue;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMSEAGREEN")) return VTKnownColor.MediumSeaGreen;
                                break;
                        }
                        break;
                    case 15:
                        if (colorUpper.Equals("MEDIUMSLATEBLUE")) return VTKnownColor.MediumSlateBlue;
                        if (colorUpper.Equals("MEDIUMTURQUOISE")) return VTKnownColor.MediumTurquoise;
                        if (colorUpper.Equals("MEDIUMVIOLETRED")) return VTKnownColor.MediumVioletRed;
                        break;
                    case 16:
                        if (colorUpper.Equals("MEDIUMAQUAMARINE")) return VTKnownColor.MediumAquamarine;
                        break;
                    case 17:
                        if (colorUpper.Equals("MEDIUMSPRINGGREEN")) return VTKnownColor.MediumSpringGreen;
                        break;
                    case 20:
                        if (colorUpper.Equals("LIGHTGOLDENRODYELLOW")) return VTKnownColor.LightGoldenrodYellow;
                        break;
                }
            }
            // colorString was null or not found
            return VTKnownColor.UnknownColor;
        }

        internal static VTKnownColor ArgbStringToKnownColor(string argbString)
        {
            string argbUpper = argbString.Trim().ToUpper(System.Globalization.CultureInfo.InvariantCulture);

            VTKnownColor color;
            if (s_knownArgbColors.TryGetValue(argbUpper, out color))
                return color;

            return VTKnownColor.UnknownColor;
        }
#if DEBUG
        private static int s_count = 0;
#endif

        //private static Dictionary<uint, SolidColorBrush> s_solidColorBrushCache = new Dictionary<uint, SolidColorBrush>();
        private static Dictionary<string, VTKnownColor> s_knownArgbColors = new Dictionary<string, VTKnownColor>();
    }
}
