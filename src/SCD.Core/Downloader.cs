﻿using SCD.Core.DataModels;
using SCD.Core.Extensions;
using SCD.Core.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SCD.Core;

public static class AlbumDownloader
{
    private static readonly Progress<double> _progress = new Progress<double>(progressAmount => ProgressChanged?.Invoke(Math.Round(progressAmount, MidpointRounding.ToZero)));

    public static event Action<AlbumFile>? FileChanged;
    public static event Action<double>? ProgressChanged;
    public static event Action<string>? DownloadFinished;
    public static event Action<string>? ErrorOccurred;

    public static async Task DownloadAsync(Album album, string downloadLocation, CancellationToken cancellationToken)
    {
        if(album.Files is null || album.Files.Length == 0)
            return;

        if(string.IsNullOrEmpty(album.Title))
            album.Title = "Album";

        string path = PathUtilities.NormalizePath(Path.Combine(downloadLocation, album.Title));

        if(!Directory.Exists(path))
            Directory.CreateDirectory(path);

        foreach(AlbumFile file in album.Files)
        {
            ProgressChanged?.Invoke(0);
            FileChanged?.Invoke(file);

            cancellationToken.ThrowIfCancellationRequested();

            if(string.IsNullOrEmpty(file.File) || string.IsNullOrEmpty(file.Name))
                continue;

            string filePath = PathUtilities.NormalizePath(Path.Combine(path, file.Name));

            if(File.Exists(filePath))
                continue;

            using(FileStream fileStream = File.OpenWrite(filePath))
            {
                try
                {
                    await HttpClientHelper.HttpClient.DownloadAsync(file.File, fileStream, _progress, cancellationToken);
                }
                catch(Exception ex)
                {
                    switch(ex)
                    {
                        case OperationCanceledException:
                            break;

                        default:
                            ErrorOccurred?.Invoke(ex.Message);
                            break;
                    }
                }
            }

            GC.Collect();
        }

        DownloadFinished?.Invoke(path);
    }
}
