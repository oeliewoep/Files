﻿using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Files.Uwp.Filesystem.StorageItems
{
    public abstract class BaseStorageFile : IBaseStorageFile
    {
        public abstract string Path { get; }
        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string ContentType { get; }
        public abstract string FileType { get; }
        public abstract string DisplayType { get; }

        public bool IsAvailable => true;
        public StorageProvider Provider => null;

        public abstract DateTimeOffset DateCreated { get; }
        public abstract FileAttributes Attributes { get; }
        public abstract string FolderRelativeId { get; }

        public abstract IStorageItemExtraProperties Properties { get; }
        StorageItemContentProperties IStorageItemProperties.Properties
            => this is SystemStorageFile file ? file.File.Properties : null;

        public static implicit operator BaseStorageFile(StorageFile value)
            => value is not null ? new SystemStorageFile(value) : null;

        public abstract IAsyncOperation<StorageFile> ToStorageFileAsync();

        public abstract bool IsEqual(IStorageItem item);
        public abstract bool IsOfType(StorageItemTypes type);

        public abstract IAsyncOperation<BaseStorageFolder> GetParentAsync();
        IAsyncOperation<StorageFolder> IStorageItem2.GetParentAsync()
            => AsyncInfo.Run(async (cancellationToken) => await (await GetParentAsync()).ToStorageFolderAsync());

        public abstract IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();
        IAsyncOperation<BasicProperties> IStorageItem.GetBasicPropertiesAsync()
            => AsyncInfo.Run(async (cancellationToken) => await (await ToStorageFileAsync()).GetBasicPropertiesAsync());

        public abstract IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode);
        public abstract IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options);
        public abstract IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync();
        public abstract IAsyncOperation<IInputStream> OpenSequentialReadAsync();
        public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync();
        public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options);

        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder);
        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder)
            => AsyncInfo.Run(async (cancellationToken) => await (await CopyAsync(destinationFolder)).ToStorageFileAsync());

        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);
        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
            => AsyncInfo.Run(async (cancellationToken) => await (await CopyAsync(destinationFolder, desiredNewName)).ToStorageFileAsync());

        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
            => AsyncInfo.Run(async (cancellationToken) => await (await CopyAsync(destinationFolder, desiredNewName, option)).ToStorageFileAsync());

        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder);
        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName);
        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);

        public abstract IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace);
        public abstract IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace);

        public abstract IAsyncAction RenameAsync(string desiredName);
        public abstract IAsyncAction RenameAsync(string desiredName, NameCollisionOption option);

        public abstract IAsyncAction DeleteAsync();
        public abstract IAsyncAction DeleteAsync(StorageDeleteOption option);

        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options);

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

        public static IAsyncOperation<BaseStorageFile> GetFileFromPathAsync(string path)
            => AsyncInfo.Run(async (cancellationToken)
                => await ZipStorageFile.FromPathAsync(path) ?? await FtpStorageFile.FromPathAsync(path) ?? await ShellStorageFile.FromPathAsync(path) ?? await NativeStorageFile.FromPathAsync(path) ?? await SystemStorageFile.FromPathAsync(path)
            );

        public async Task<string> ReadTextAsync(int maxLength = -1)
        {
            using var inputStream = await OpenSequentialReadAsync();
            using var dataReader = new DataReader(inputStream);
            StringBuilder builder = new();
            uint bytesRead, bytesToRead;
            do
            {
                bytesToRead = maxLength < 0 ? 4096 : (uint)Math.Min(maxLength, 4096);
                bytesRead = await dataReader.LoadAsync(bytesToRead);
                builder.Append(dataReader.ReadString(bytesRead));
            } while (bytesRead > 0);
            return builder.ToString();
        }

        public async Task WriteTextAsync(string text)
        {
            using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(0);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString(text);
            await dataWriter.StoreAsync();
            await stream.FlushAsync();
        }
        public async Task WriteBytesAsync(byte[] dataBytes)
        {
            using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(0);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteBytes(dataBytes);
            await dataWriter.StoreAsync();
            await stream.FlushAsync();
        }
    }
}
