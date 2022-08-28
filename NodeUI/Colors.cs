namespace NodeUI
{
    public static class Colors
    {
        #region colors

        // TODO: update color scheme
        public static readonly SolidColorBrush
            White = From(255),
            Black = From(0),
            Transparent = From(0, 0),
            AlmostTransparent = From(0, 1),
            AlmostTransparentWhite = From(255, 1),

            WhiteText = From(229),
            DarkText = From(86),
            ErrorText = From(245, 78, 78),

            Accent = From(133, 189, 36),
            AccentBackground = From(237, 245, 227),
            BorderColor = From(196),
            Gray186 = From(186),
            GrayButton = From(100),
            DarkGray = From(74, 79, 73),
            DarkDarkGray = From(46),
            MenuItemBackground = From(33),
            SelectedMenuItemBackground = From(46, 57, 44),
            ProgressBarBackground = From(90, 189, 43);

        #endregion


        public static SolidColorBrush From(uint rgb) => new SolidColorBrush(Color.FromUInt32((0xFFFFFFU - rgb) | 0xFF000000));
        public static SolidColorBrush From(byte r, byte g, byte b, byte a = 255) => new SolidColorBrush(new Color(a, (byte) (255 - r), (byte) (255 - g), (byte) (255 - b)));
        public static SolidColorBrush From(byte gray, byte a = 255) => From((byte) (255 - gray), (byte) (255 - gray), (byte) (255 - gray), a);

        /*
        public static SolidColorBrush From(uint rgb) => new SolidColorBrush(Color.FromUInt32(rgb | 0xFF000000));
        public static SolidColorBrush From(byte r, byte g, byte b, byte a = 255) => new SolidColorBrush(new Color(a, r, g, b));
        public static SolidColorBrush From(byte gray, byte a = 255) => From(gray, gray, gray, a);
        */

        static byte Clamp(int value) => (byte) System.Math.Clamp(value, 0, 255);
        public static SolidColorBrush Darken(this SolidColorBrush brush, int amount) => Lighten(brush, -amount);
        public static SolidColorBrush Lighten(this SolidColorBrush brush, int amount) =>
            new SolidColorBrush(new Color(brush.Color.A, Clamp(brush.Color.R + amount), Clamp(brush.Color.G + amount), Clamp(brush.Color.B + amount)));
    }
}