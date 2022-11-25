namespace Transport.Upload._3DModelsUpload.CGTrader.Captcha;

public record CGTraderCaptchaFoldChallenge(string Seed, int Slots, int Depth)
{
    readonly int[] URLSafeBase64CharCode2IntMap =
    {
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        0x3e,
        -0x1,
        -0x1,
        0x0,
        0x1,
        0x2,
        0x3,
        0x4,
        0x5,
        0x6,
        0x7,
        0x8,
        0x9,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        0xa,
        0xb,
        0xc,
        0xd,
        0xe,
        0xf,
        0x10,
        0x11,
        0x12,
        0x13,
        0x14,
        0x15,
        0x16,
        0x17,
        0x18,
        0x19,
        0x1a,
        0x1b,
        0x1c,
        0x1d,
        0x1e,
        0x1f,
        0x20,
        0x21,
        0x22,
        0x23,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        0x3f,
        -0x1,
        0x24,
        0x25,
        0x26,
        0x27,
        0x28,
        0x29,
        0x2a,
        0x2b,
        0x2c,
        0x2d,
        0x2e,
        0x2f,
        0x30,
        0x31,
        0x32,
        0x33,
        0x34,
        0x35,
        0x36,
        0x37,
        0x38,
        0x39,
        0x3a,
        0x3b,
        0x3c,
        0x3d,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1,
        -0x1
    };

    readonly char[] URLSafeBase64Int2CharMap =
    {
        '0',
        '1',
        '2',
        '3',
        '4',
        '5',
        '6',
        '7',
        '8',
        '9',
        'A',
        'B',
        'C',
        'D',
        'E',
        'F',
        'G',
        'H',
        'I',
        'J',
        'K',
        'L',
        'M',
        'N',
        'O',
        'P',
        'Q',
        'R',
        'S',
        'T',
        'U',
        'V',
        'W',
        'X',
        'Y',
        'Z',
        'a',
        'b',
        'c',
        'd',
        'e',
        'f',
        'g',
        'h',
        'i',
        'j',
        'k',
        'l',
        'm',
        'n',
        'o',
        'p',
        'q',
        'r',
        's',
        't',
        'u',
        'v',
        'w',
        'x',
        'y',
        'z',
        '-',
        '_'
    };

    int URLSafeBase64CharToInt(char urlSafeBase64Char)
    {
        var urlSafeInt = URLSafeBase64CharCode2IntMap[urlSafeBase64Char % 256];
        if (urlSafeInt < 0) throw new ArgumentOutOfRangeException(
            nameof(urlSafeBase64Char), urlSafeBase64Char, $"{nameof(urlSafeBase64Char)} must be within [a-zA-Z0-9:;]"
            );
        else return urlSafeInt;
    }

    char URLSafeBase64IntToChar(int urlSafeBase64Int)
    {
        if (urlSafeBase64Int < 0 || urlSafeBase64Int >= 64) throw new ArgumentOutOfRangeException(
            nameof(urlSafeBase64Int), urlSafeBase64Int, $"{nameof(urlSafeBase64Int)} must be between 0 and 63 inclusive."
            );
        else return URLSafeBase64Int2CharMap[urlSafeBase64Int % 64];
    }

    string URLSafeBase4096IntToString(int urlSafeBase4096Int)
    {
        if (urlSafeBase4096Int < 0 || urlSafeBase4096Int >= 4096) throw new ArgumentOutOfRangeException(
            nameof(urlSafeBase4096Int), urlSafeBase4096Int, $"{nameof(urlSafeBase4096Int)} must be between 0 and 4095 inclusive."
            );
        else return string.Concat(
            URLSafeBase64IntToChar(urlSafeBase4096Int >> 6),
            URLSafeBase64IntToChar(urlSafeBase4096Int & 63)
            );
    }

    int[] URLSafeBase64Str2IntArray(string urlSafeBase64String) => urlSafeBase64String.Select(URLSafeBase64CharToInt).ToArray();

    static int HashIntArray(int[] intArray)
    {
        int hash = 0;
        foreach (int element in intArray)
        {
            hash = (hash << 5) - hash + element;
            hash &= hash;
        }

        return hash > 0 ? hash : hash * -1;
    }

    public string Solve()
    {
        if (string.IsNullOrWhiteSpace(Seed) || Slots < 1)
            return "0";

        List<string> solution = new();
        var urlSafeBase64IntArray = URLSafeBase64Str2IntArray(Seed);
        for (var i = 0; i < Slots; i++)
        {
            urlSafeBase64IntArray = _FoldBase64IntArray(urlSafeBase64IntArray, 31);
            var arrayHash = HashIntArray(_FoldBase64IntArray(urlSafeBase64IntArray, Depth));
            solution.Add(URLSafeBase4096IntToString(arrayHash % 4096));
        }
        return string.Join(null, solution);
    }

    static int[] _FoldBase64IntArray(int[] array, int foldCount)
    {
        int[] a2 = array.Reverse().ToArray();
        int[] result = new int[array.Length]; Array.Copy(array, result, array.Length);

        int y, z; y = z = 0;
        for (int i = 0, offset = 1; i < foldCount; i++, offset++)
        {
            for (var x = 0; x < array.Length; x++)
            {
                result[x] = (int)(Math.Floor((result[x] + a2[(x + offset) % a2.Length]) * 73.0 / 8) + y + z) % 64;
                z = (int)Math.Floor((double)y / 2);
                y = (int)Math.Floor((double)result[x] / 2);
            }
        }

        return result;
    }
}
