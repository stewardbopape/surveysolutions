﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using WB.Services.Infrastructure.FileSystem;

namespace WB.Services.Export.Services.Processing
{
    public interface IDataExportFileAccessor
    {
        string GetExternalStoragePath(string name);
        IZipArchive CreateExportArchive(Stream outputStream, string archivePassword, CompressionLevel compressionLevel = CompressionLevel.Fastest);
        void RecreateExportArchive(string exportTempDirectoryPath, string archiveName, string archivePassword, IProgress<int> exportProgress);
        void PubishArchiveToExternalStorage(string archiveName, IProgress<int> exportProgress);
        void RecreateExportArchive(string exportTempDirectoryPath, IEnumerable<string> filesToArchive, string archiveFilePath, string password);
    }
}
