using ICSharpCode.SharpZipLib.Zip;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    public record AssetContainer : IDisposable
    {
        public enum Type_
        { Archive, Directory }

        public Type_ ContainerType { get; }
        public string Path { get; private set; }
        public string Name => System.IO.Path.GetFileName(Path);

        string? _directoryPath;
        string? _archivePath;

        public static AssetContainer Create(string path, bool disposeTemps = true)
            => AssetContainer.Exists(path) ?
            new(path, disposeTemps) : new(Directory.CreateDirectory(path).FullName, disposeTemps);

        public AssetContainer(string path, bool disposeTemps = true)
        {
            if (Archive_.Exists(path))
            { Path = _archivePath = path; ContainerType = Type_.Archive; }
            else if (Directory.Exists(path))
            { Path = _directoryPath = path; ContainerType = Type_.Directory; }
            else throw new ArgumentException($"{nameof(AssetContainer)} must reference either directory or archive.", nameof(path));

            _disposeTemps = disposeTemps;
        }

        /// <summary>
        /// Creates a temporary archive to which the files are extracted if the <see cref="_3DModel"/>
        /// is initialized from a directory (i.e. <see cref="Path"/> referes to a directory).
        /// </summary>
        /// <returns>Path to the archive where this <see cref="_3DModel"/> is stored.</returns>
        internal ValueTask<string> Archive()
            => ValueTask.FromResult(_archivePath ??= Archive_.Pack(_directoryPath!));

        public string Store(string entry, string? @as = default, StoreMode mode = StoreMode.Move)
        {
            string name = @as ?? System.IO.Path.GetFileNameWithoutExtension(entry);

            if (AssetContainer.Exists(entry))
            {
                // Dispose of that container.
                var entryContainer = new AssetContainer(entry);
                Store(entryContainer, @as, mode);
            }
            else if (File.Exists(entry))
            {
                name = System.IO.Path.ChangeExtension(name, System.IO.Path.GetExtension(entry));
                (mode switch
                {
                    StoreMode.Move => new FileSystemOperation
                    {
                        OnArchive = () => { Archive(entry, name); File.Delete(entry); },
                        OnDirectory = () => File.Move(entry, System.IO.Path.Combine(Path, name))
                    },
                    StoreMode.Copy => new FileSystemOperation
                    {
                        OnArchive = () => Archive(entry, name),
                        OnDirectory = () => File.Copy(entry, System.IO.Path.Combine(Path, name))
                    },
                    _ => throw new NotImplementedException()
                })
                .ExecuteOn(this);


                void Archive(string entry, string? @as = default)
                {
                    using var archive = new ZipFile(this);
                    archive.BeginUpdate();
                    { archive.Add(entry, name); }
                    archive.CommitUpdate();
                }
            }
            else throw new FileNotFoundException($"{nameof(entry)} to store inside of the {nameof(AssetContainer)} was not found.", entry);

            return System.IO.Path.Combine(this, name);
        }
        public string Store(AssetContainer container, string? @as = default, StoreMode mode = StoreMode.Move)
        {
            string name = @as ?? System.IO.Path.GetFileNameWithoutExtension(container);

            new FileSystemOperation
            {
                OnArchive = () =>
                {
                    using var archive = new ZipFile(this);
                    archive.BeginUpdate();
                    {
                        if (container.ContainerType is AssetContainer.Type_.Directory)
                            foreach (var nestedEntry in Directory.EnumerateFiles(container, "*", SearchOption.AllDirectories))
                                archive.Add(nestedEntry, System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(container) ?? container, nestedEntry));
                        else archive.Add(container, @as);
                    }
                    archive.CommitUpdate();

                    if (mode is StoreMode.Move)
                        container.Delete();
                },
                OnDirectory = () =>
                {
                    switch (mode)
                    {
                        case StoreMode.Copy: container.Copy(System.IO.Path.Combine(this.Path, name)); break;
                        case StoreMode.Move: container.Move(System.IO.Path.Combine(this.Path, name)); break;
                    }
                }
            }
            .ExecuteOn(this);

            return container.Path;
        }
        public enum StoreMode { Move, Copy }

        // TODO: Ensure correct Path-changing behavior for RFProducts.
        public void Move(string destination)
        {
            new FileSystemOperation
            {
                OnArchive = () => File.Move(this, System.IO.Path.ChangeExtension(destination, System.IO.Path.GetExtension(this))),
                OnDirectory = () => new DirectoryInfo(this).MoveTo_(destination)
            }
            .ExecuteOn(this);
            Path = new FileSystemOperation<string>
            {
                OnArchive = () => _archivePath = destination,
                OnDirectory = () => _directoryPath = destination
            }
            .ExecuteOn(this);
        }

        public void Copy(string destination)
        {
            new FileSystemOperation
            {
                OnArchive = () => File.Copy(this, System.IO.Path.ChangeExtension(destination, System.IO.Path.GetExtension(this))),
                OnDirectory = () => new DirectoryInfo(this).CopyTo(destination)
            }
            .ExecuteOn(this);
            Path = new FileSystemOperation<string>
            {
                OnArchive = () => _archivePath = destination,
                OnDirectory = () => _directoryPath = destination
            }
            .ExecuteOn(this);
        }

        public IEnumerable<string> EnumerateEntries(EntryType entryTypes = EntryType.All)
        {
            var allEntries = _directoryPath is null ?
                Archive_.EnumerateEntries(this, out _directoryPath) :
                Directory.EnumerateFileSystemEntries(_directoryPath);

            if (entryTypes is EntryType.All)
                return allEntries;
            else
            {
                var entries = new HashSet<string>();
                if (entryTypes.HasFlag(EntryType.Containers))
                    entries.UnionWith(allEntries.Where(AssetContainer.Exists));
                if (entryTypes.HasFlag(EntryType.NonContainers))
                    entries.UnionWith(allEntries.Where(_ => !AssetContainer.Exists(_)));
                return entries;
            }
        }

        [Flags]
        public enum EntryType
        {
            Containers = 1,
            NonContainers = 2,
            All = Containers | NonContainers
        }

        internal static IEnumerable<string> EnumerateAt(string directoryPath)
            => Directory.EnumerateFiles(directoryPath).Where(Archive_.Exists)
            .Concat
            (Directory.EnumerateDirectories(directoryPath));

        public static bool Exists(string path) => Archive_.Exists(path) || Directory.Exists(path);

        public void Delete()
        {
            switch (ContainerType)
            {
                case Type_.Archive: File.Delete(this); break;
                case Type_.Directory: new DirectoryInfo(this).Delete(DeletionMode.Wipe); break;
            }
        }

        public void Dispose()
        { Dispose(true); GC.SuppressFinalize(this); }

        protected void Dispose(bool managed)
        {
            if (managed)
            {
                if (_disposeTemps)
                {
                    if (!_isDisposed)
                    {
                        switch (ContainerType)
                        {
                            case Type_.Archive:
                                try { new DirectoryInfo(_directoryPath!).Delete(DeletionMode.Wipe); } catch { }; break;
                            case Type_.Directory:
                                try { File.Delete(_archivePath!); } catch { }; break;
                        }

                        _isDisposed = true;
                    }
                }
            }
        }
        bool _isDisposed;
        readonly bool _disposeTemps;

        public static implicit operator string(AssetContainer container) => container.Path;


        public static class Archive_
        {
            internal const string Extension = ".zip";
            internal static bool Exists(string path) => File.Exists(path) && IsArchive(path);
            public static bool IsArchive(string path) => System.IO.Path.GetExtension(path) == Archive_.Extension;

            internal static IEnumerable<string> EnumerateEntries(string path, out string tempDirectoryPath)
                => Directory.EnumerateFileSystemEntries(tempDirectoryPath = Archive_.Unpack(path));

            public static string Unpack(string path)
            {
                if (Archive_.Exists(path))
                {
                    DirectoryInfo unpackedArchive = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
                    System.IO.Compression.ZipFile.ExtractToDirectory(path, unpackedArchive.FullName);
                    return unpackedArchive.FullName;
                }
                else throw new FileNotFoundException("Archive doesn't exist.", path);
            }

            internal static string Pack(string path)
            {
                DirectoryInfo archiveBuffer = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
                try
                {
                    Directory.EnumerateFileSystemEntries(path).CopyTo(archiveBuffer);
                    var archivePath = System.IO.Path.ChangeExtension(archiveBuffer.FullName, ".zip");
                    System.IO.Compression.ZipFile.CreateFromDirectory(archiveBuffer.FullName, archivePath);

                    return archivePath;
                }
                finally { archiveBuffer.Delete(DeletionMode.Wipe); }
            }
        }

        class FileSystemOperation
        {
            internal required Action OnArchive { get; init; }
            internal required Action OnDirectory { get; init; }

            internal void ExecuteOn(AssetContainer target)
            {
                switch (target.ContainerType)
                {
                    case AssetContainer.Type_.Archive: OnArchive(); break;
                    case AssetContainer.Type_.Directory: OnDirectory(); break;
                    default: throw new NotImplementedException();
                }
            }
        }
        class FileSystemOperation<TResult>
        {
            internal required Func<TResult> OnArchive { get; init; }
            internal required Func<TResult> OnDirectory { get; init; }

            internal TResult ExecuteOn(AssetContainer target)
                => target.ContainerType switch
                {
                    AssetContainer.Type_.Archive => OnArchive(),
                    AssetContainer.Type_.Directory => OnDirectory(),
                    _ => throw new NotImplementedException(),
                };
        }
    }
}
