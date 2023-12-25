﻿using System.IO.Compression;

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

        public string Store(ref string file, string? @as = default, StoreMode mode = StoreMode.Move)
            => file = Store(file, @as, mode);
        // TODO: Add support for storing AssetContainer entries.
        // TODO: Properly implement OnArchive Copy behaviour.
        public enum StoreMode { Move, Copy }
        /// <remarks>Currently only files can be stored using this method.</remarks>
        public string Store(string file, string? @as = default, StoreMode mode = StoreMode.Move)
        {
            var name = System.IO.Path.ChangeExtension(System.IO.Path.GetFileNameWithoutExtension(@as ?? file), System.IO.Path.GetExtension(file));
            var destination = System.IO.Path.Combine(Path, name);

            (mode switch
            {
                StoreMode.Move => new FileSystemOperation
                {
                    OnArchive = () => ZipFile.Open(this, ZipArchiveMode.Create).CreateEntryFromFile(file, name),
                    OnDirectory = () => File.Move(file, destination),
                },
                StoreMode.Copy => new FileSystemOperation
                {
                    OnArchive = () => ZipFile.Open(this, ZipArchiveMode.Create).CreateEntryFromFile(file, name),
                    OnDirectory = () => File.Copy(file, destination),
                },
                _ => throw new NotImplementedException()
            })
            .ExecuteOn(this);

            return destination;
        }

        public void Move(string destinationContainerName)
            => new FileSystemOperation
            {
                OnArchive = () => File.Move(this, destinationContainerName),
                OnDirectory = () => Directory.Move(this, destinationContainerName)
            }
            .ExecuteOn(this);

        //public void Copy(string destinationContainerName)
        //    => new FileSystemOperation
        //    {
        //        OnArchive = () => File.Copy(this, destinationContainerName),
        //        OnDirectory = () => Directory.Copy(this, destinationContainerName)
        //    }
        //    .ExecuteOn(this);

        //public IEnumerable<AssetContainer> EnumerateContainers()
        //    => EnumerateFiles().Where(AssetContainer.Exists).Select(_ => new AssetContainer(_));

        /// <remarks>
        /// Creates a temporary directory if <see langword="this"/> <see cref="AssetContainer.ContainerType"/> is <see cref="Type_.Archive"/>.
        /// </remarks>
        public IEnumerable<string> EnumerateEntries(EntryType entryTypes = EntryType.All)
        {
            var allEntries = _directoryPath is null ?
                Archive_.EnumerateFiles(_archivePath!, out _directoryPath) :
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
            => Archive_.EnumerateAt(directoryPath)
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
                                try { new DirectoryInfo(_directoryPath!).Delete(DeletionMode.Wipe); } catch { };
                                break;
                            case Type_.Directory:
                                try { File.Delete(_archivePath!); } catch { };
                                break;
                        }

                        _isDisposed = true;
                    }
                }
            }
        }
        bool _isDisposed;
        readonly bool _disposeTemps;

        public static implicit operator string(AssetContainer container) => container.Path;


        internal static class Archive_
        {
            static bool IsArchive(string path) => _validExtensions.Contains(System.IO.Path.GetExtension(path));
            readonly static string[] _validExtensions = [".zip", ".rar"];
            internal static IEnumerable<string> EnumerateFiles(string path, out string tempDirectoryPath)
                => Directory.EnumerateFiles(tempDirectoryPath = Archive_.Unpack(path));

            internal static string Unpack(string path)
            {
                if (Archive_.Exists(path))
                {
                    DirectoryInfo unpackedArchive = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
                    ZipFile.ExtractToDirectory(path, unpackedArchive.FullName);
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
                    ZipFile.CreateFromDirectory(archiveBuffer.FullName, archivePath);

                    return archivePath;
                }
                finally { archiveBuffer.Delete(DeletionMode.Wipe); }
            }

            internal static IEnumerable<string> EnumerateAt(string path)
                => Directory.EnumerateFiles(path).Where(IsArchive);

            internal static bool Exists(string path) => File.Exists(path) && IsArchive(path);
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
            {
                switch (target.ContainerType)
                {
                    case AssetContainer.Type_.Archive: return OnArchive();
                    case AssetContainer.Type_.Directory: return OnDirectory();
                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
