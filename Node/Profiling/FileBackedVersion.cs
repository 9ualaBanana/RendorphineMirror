namespace Node.Profiling;

internal class FileBackedVersion
    : IEquatable<FileBackedVersion>, IEquatable<Version>, IComparable, IComparable<FileBackedVersion>, IComparable<Version>
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    internal bool Exists => Value is not null;
    internal Version? Value { get; private set; }
    string ParentDirectoryPath
    {
        get => _parentDirectoryPath;
        init
        {
            if (!Directory.Exists(value))
                Directory.CreateDirectory(value);
            _parentDirectoryPath = value;
        }
    }
    readonly string _parentDirectoryPath = null!;

    const string _versionFileExtension = ".version";


    internal FileBackedVersion(string version, string parentDirectoryPath)
    : this(Version.Parse(version), parentDirectoryPath)
    {
    }

    internal FileBackedVersion(Version version, string parentDirectoryPath) : this(parentDirectoryPath)
    {
        Update(version);
    }

    internal FileBackedVersion(string? parentDirectoryPath = default)
    {
        ParentDirectoryPath = parentDirectoryPath ?? Directory.GetCurrentDirectory();
    }


    internal void Update(Version version)
    {
        var currentVersionFilePath = CurrentVersionFilePath;
        var newVersionFilePath = AsVersionFilePath(version);

        if (currentVersionFilePath is null)
        { using var _ = File.Create(newVersionFilePath); }
        else
            File.Move(currentVersionFilePath, newVersionFilePath);
        Value = version;
    }

    string? CurrentVersionFilePath
    {
        get
        {
            try { return Directory.EnumerateFiles(ParentDirectoryPath).SingleOrDefault(IsVersionFile); }
            catch (InvalidOperationException ex)
            {
                _logger.Fatal("Directory can't contain more than one version file: {Directory}", ParentDirectoryPath);
                throw new InvalidOperationException($"Directory can't contain more than one version file: {ParentDirectoryPath}", ex);
            }
        }
    }

    bool IsVersionFile(string filePath) => Path.GetExtension(filePath) == _versionFileExtension;

    string AsVersionFilePath(Version version) => Path.Combine(ParentDirectoryPath, $"{version}{_versionFileExtension}");

    internal static Version Parse(string input) => Version.Parse(input);


    #region Equality
    public override bool Equals(object? obj) => Equals(obj as FileBackedVersion);

    public bool Equals(Version? other) => Value == other;

    public bool Equals(FileBackedVersion? other) => Value == other?.Value;

    public override int GetHashCode() => base.GetHashCode();


    public static bool operator !=(FileBackedVersion? this_, FileBackedVersion? other) => this_?.Value != other?.Value;
    public static bool operator ==(FileBackedVersion? this_, FileBackedVersion? other) => this_?.Value == other?.Value;
    public static bool operator !=(FileBackedVersion? this_, Version? other) => this_?.Value != other;
    public static bool operator ==(FileBackedVersion? this_, Version? other) => this_?.Value == other;
    #endregion

    #region Comparison
    public int CompareTo(FileBackedVersion? other) => CompareTo(other?.Value);

    public int CompareTo(object? obj) => CompareTo(obj as Version);

    public int CompareTo(Version? other)
    {
        if (Value is not null) return Value.CompareTo(other);
        else return other is null ? 0 : -1;
    }

    public static bool operator >(FileBackedVersion this_, Version other) => this_.Value > other;
    public static bool operator >(FileBackedVersion this_, FileBackedVersion other) => this_.Value > other.Value;
    public static bool operator <(FileBackedVersion this_, Version other) => this_.Value < other;
    public static bool operator <(FileBackedVersion this_, FileBackedVersion other) => this_.Value < other.Value;
    #endregion
}
