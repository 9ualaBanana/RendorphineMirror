﻿using System.IO.Compression;

namespace _3DProductsPublish._3DProductDS;

public partial record _3DProduct
{
    public record AssetContainer : IDisposable
    {
        public enum Type_
        { Archive, Directory }

        internal Type_ ContainerType { get; }
        internal string Path { get; }

        string? _directoryPath;
        string? _archivePath;

        internal AssetContainer(string path, bool disposeTemps = true)
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

        /// <remarks>
        /// Creates a temporary directory if <see langword="this"/> <see cref="AssetContainer"/>'s OriginalContainer type is <see cref="Type_.Archive"/>.
        /// </remarks>
        internal IEnumerable<string> EnumerateFiles()
            => _directoryPath is not null ?
            Directory.EnumerateFiles(_directoryPath) :
            Archive_.EnumerateFiles(_archivePath!, out _directoryPath);

        internal static IEnumerable<string> EnumerateAt(string directoryPath)
            => Archive_.EnumerateAt(directoryPath)
            .Concat
            (Directory.EnumerateDirectories(directoryPath));

        internal static bool Exists(string path) => Archive_.Exists(path) || Directory.Exists(path);

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


        internal static class Archive_
        {
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

            static bool IsArchive(string path) => _validExtensions.Contains(System.IO.Path.GetExtension(path));
            readonly static string[] _validExtensions = { ".zip", ".rar" };
        }
    }
}