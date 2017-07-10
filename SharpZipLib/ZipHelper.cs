using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Core;
using System.Threading;

public class ZipHelper
{
    public delegate void FileChangeEvent(string filename,long total, long current);
    /// <summary>
    /// 多线程中压缩
    /// </summary>
    /// <param name="sourceFilePath"></param>
    /// <param name="destinationZipFilePath"></param>
    /// <param name="events"></param>
    /// <param name="recurse"></param>
    /// <param name="fileFilter"></param>
    public static void CreateZipThread(string sourceFilePath, string destinationZipFilePath, FastZipEvents events, bool recurse = true, string fileFilter = "")
    {
        Thread thread = new Thread(new ThreadStart(()=> {
            CreateZip(destinationZipFilePath, sourceFilePath, events,recurse,fileFilter);
        }));

        thread.Start();
    }
    
    /// <summary>
    /// 多线程指定添加文件
    /// </summary>
    /// <param name="sourceFilesPath"></param>
    /// <param name="destinationZipFilePath"></param>
    /// <param name="events"></param>
    /// <param name="recurse"></param>
    /// <param name="fileFilter"></param>
    public static void CreateZipThread(Dictionary<string, string> sourceFilesPath, string destinationZipFilePath, FileChangeEvent events)
    {
        Thread thread = new Thread(new ThreadStart(() => {
            CreateZip(sourceFilesPath, destinationZipFilePath, events);
        }));

        thread.Start();
    }
    
    /// <summary>
    /// 将指定文件压缩到zip文件
    /// </summary>
    /// <param name="sourceFilesPath"></param>
    /// <param name="destinationZipFilePath"></param>
    /// <param name="events"></param>
    /// <param name="recurse"></param>
    /// <param name="fileFilter"></param>
    public static void CreateZip(Dictionary<string,string> sourceFilesPath, string destinationZipFilePath, FileChangeEvent events)
    {
        ZipOutputStream zipStream = new ZipOutputStream(File.Create(destinationZipFilePath));
        zipStream.SetLevel(9);  // 压缩级别 0-9

        Crc32 crc = new Crc32();
        Dictionary<ZipEntry, byte[]> holders = new Dictionary<ZipEntry, byte[]>();
        long total = 0;
        long current = 0;
        foreach (var file in sourceFilesPath)
        {
            if (File.Exists(file.Key))
            {
                FileStream fileStream = File.OpenRead(file.Key);
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                ZipEntry entry = new ZipEntry(file.Value);
                entry.DateTime = DateTime.Now;
                entry.Size = fileStream.Length;
                entry.IsUnicodeText = true;
                fileStream.Close();
                crc.Reset();
                crc.Update(buffer);
                entry.Crc = crc.Value;
                holders.Add(entry, buffer);
                total += buffer.Length;
            }
        }

        foreach (var hoder in holders)
        {
            zipStream.PutNextEntry(hoder.Key);
            zipStream.Write(hoder.Value, 0, hoder.Value.Length);
            if (events != null) events.Invoke(hoder.Key.Name, total, current += hoder.Value.Length);
        }

        zipStream.Finish();
        zipStream.Close();
    }

    /// <summary>
    /// 解压到指定文件夹
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="events"></param>
    public static void ExtractZipThread(string zipFileName, string targetDirectory, FileChangeEvent events)
    {
        Thread thread = new Thread(new ThreadStart(() => {
            ExtractZip(zipFileName, targetDirectory, events);
        }));

        thread.Start();
    }
    public static void ExtractZip(string zipFileName, string targetDirectory, FileChangeEvent events)
    {
        if (!File.Exists(zipFileName))
        {
            return;
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        ZipInputStream s = null;
        ZipEntry theEntry = null;

        string fileName;
        FileStream streamWriter = null;

        try
        {
            long total = 0;
            long current = 0;

            s = new ZipInputStream(File.OpenRead(zipFileName));
            Dictionary<string, byte[]> dataDic = new Dictionary<string, byte[]>();

            while ((theEntry = s.GetNextEntry()) != null)
            {
                total += s.Length;
                fileName = Path.Combine(targetDirectory, theEntry.Name);

                if (fileName.EndsWith("/") || fileName.EndsWith("\\"))
                {
                    Directory.CreateDirectory(fileName);
                    continue;//文件夹跳过
                }

                if (theEntry.Name != String.Empty)
                {
                    using (var mem = new MemoryStream())
                    {
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                mem.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                        dataDic.Add(theEntry.Name, mem.ToArray());
                    }
                   
                }
            }
            foreach (var item in dataDic)
            {
                fileName = Path.Combine(targetDirectory, item.Key);
                ///判断文件路径是否是文件夹
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }
                current += item.Value.Length;
                streamWriter = File.Create(fileName);
                streamWriter.Write(item.Value, 0, item.Value.Length);
                if (events != null) events.Invoke(item.Key, total, current);
            }
        }

        catch
        {
            throw;
        }
        finally
        {
            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter = null;
            }
            if (theEntry != null)
            {
                theEntry = null;
            }
            if (s != null)
            {
                s.Close();
                s = null;
            }
            GC.Collect();
            GC.Collect(1);
        }
    }
    /// <summary>
    /// 多线程中解压
    /// </summary>
    /// <param name="sourceFilePath"></param>
    /// <param name="destinationZipFilePath"></param>
    /// <param name="events"></param>
    /// <param name="recurse"></param>
    /// <param name="fileFilter"></param>
    public static void ExtractZipThread(string zipFileName, string targetDirectory, FastZipEvents events = null, string fileFilter = "")
    {
        Thread thread = new Thread(new ThreadStart(() => {
            ExtractZip(zipFileName, targetDirectory, events,fileFilter);
        }));

        thread.Start();
    }

    /// <summary>
    /// 多线程添加指定文件
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="filesNames"></param>
    public static void AddFileToZipThread(string zipFileName,Dictionary<string, string> filesNames)
    {
        Thread thread = new Thread(new ThreadStart(() => {
            AddFileToZip(zipFileName, filesNames);
        }));

        thread.Start();
    }

    /// <summary>
    /// 从指定zip删除文件
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="fileNames"></param>
    public static void DeleteFileFromZipThread(string zipFileName, IList<string> fileNames)
    {
        Thread thread = new Thread(new ThreadStart(() => {
            DeleteFileFromZip(zipFileName, fileNames);
        }));

        thread.Start();
    }

    /// <summary>
    /// 压缩
    /// </summary>
    /// <param name="sourceDirectory"></param>
    /// <param name="targetZipName"></param>
    /// <param name="recurse"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static void CreateZip(string zipFileName, string sourceDirectory, FastZipEvents events = null, bool recurse = true, string fileFilter = "")
    {
        if (string.IsNullOrEmpty(sourceDirectory))
        {
            throw new ArgumentNullException("SourceZipDirectory");
        }
        if (string.IsNullOrEmpty(zipFileName))
        {
            throw new ArgumentNullException("TargetZipName");
        }
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException("SourceDirecotry");
        }
        if (Path.GetExtension(zipFileName).ToUpper() != ".ZIP")
            throw new ArgumentException("TargetZipName  is not zip");
        FastZip fastZip = new FastZip(events);
        fastZip.CreateZip(zipFileName, sourceDirectory, recurse, fileFilter);
    }

    /// <summary>
    /// 解压
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="targetDirectory"></param>
    /// <param name="fileFilter"></param>
    public static void ExtractZip(string zipFileName, string targetDirectory, FastZipEvents events = null, string fileFilter = "")
    {
        if (string.IsNullOrEmpty(zipFileName))
        {
            throw new ArgumentNullException("ZIPFileName");
        }
        if (!File.Exists(zipFileName))
        {
            throw new FileNotFoundException("zipFileName");
        }
        if (Path.GetExtension(zipFileName).ToUpper() != ".ZIP")
        {
            throw new ArgumentException("ZipFileName is not Zip ");
        }
        FastZip fastZip = new FastZip(events);
        fastZip.ExtractZip(zipFileName, targetDirectory, fileFilter);
    }

    /// <summary>
    /// 添加文件到压缩文件中
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="filesNames"></param>
    public static void AddFileToZip(string zipFileName, Dictionary<string, string> filesNames)
    {
        if (string.IsNullOrEmpty(zipFileName))
        {
            throw new ArgumentNullException("ZipName");
        }
        if (Path.GetExtension(zipFileName).ToUpper() != ".ZIP")
        {
            throw new ArgumentException("ZipFileName is not Zip ");
        }
        if (filesNames == null || filesNames.Count < 1)
            return;

        ZipFile zFile = File.Exists(zipFileName) ? new ZipFile(zipFileName):ZipFile.Create(zipFileName);

        using (zFile)
        {
            zFile.BeginUpdate();

            foreach (KeyValuePair<string,string> fileName in filesNames)
            {
                zFile.Add(fileName.Key,fileName.Value);
                UnityEngine.Debug.Log(fileName.Key);
            }

            zFile.CommitUpdate();
        }

    }

    /// <summary>
    /// 移除压缩文件中的文件
    /// </summary>
    /// <param name="zipName"></param>
    /// <param name="fileNames"></param>
    public static void DeleteFileFromZip(string zipFileName, IList<string> fileNames)
    {
        if (string.IsNullOrEmpty(zipFileName))
        {
            throw new ArgumentNullException("ZipName");
        }
        if (Path.GetExtension(zipFileName).ToUpper() != ".ZIP")
        {
            throw new ArgumentException("ZipName");
        }
        if (fileNames == null || fileNames.Count < 1)
        {
            return;
        }
        using (ZipFile zipFile = new ZipFile(zipFileName))
        {
            zipFile.BeginUpdate();
            foreach (string fileName in fileNames)
            {
                zipFile.Delete(fileName);
            }
            zipFile.CommitUpdate();
        }
    }
}
/*
  #region 压缩文件

    /// <summary>
    /// 压缩文件
    /// </summary>
    /// <param name="sourceFilePath"></param>
    /// <param name="destinationZipFilePath"></param>
    public static void CreateZip(string sourceFilePath, string destinationZipFilePath)
    {
        if (sourceFilePath[sourceFilePath.Length - 1] != System.IO.Path.DirectorySeparatorChar)
            sourceFilePath += System.IO.Path.DirectorySeparatorChar;
        ZipOutputStream zipStream = new ZipOutputStream(File.Create(destinationZipFilePath));
        zipStream.SetLevel(9);  // 压缩级别 0-9
        CreateZipFiles(sourceFilePath, zipStream);
        zipStream.Finish();
        zipStream.Close();
    }

    /// <summary>
    /// 递归压缩文件
    /// </summary>
    /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
    /// <param name="zipStream">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名
    /// <param name="staticFile"></param>
    private static void CreateZipFiles(string sourceFilePath, ZipOutputStream zipStream)
    {
        Crc32 crc = new Crc32();
        string[] filesArray = Directory.GetFileSystemEntries(sourceFilePath);
        foreach (string file in filesArray)
        {
            if (Directory.Exists(file))                     //如果当前是文件夹，递归
            {
                CreateZipFiles(file, zipStream);
            }
            else                                            //如果是文件，开始压缩
            {
                FileStream fileStream = File.OpenRead(file);
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                string tempFile = file.Substring(sourceFilePath.LastIndexOf("\\") + 1);
                ZipEntry entry = new ZipEntry(tempFile);
                entry.DateTime = DateTime.Now;
                entry.Size = fileStream.Length;
                fileStream.Close();
                crc.Reset();
                crc.Update(buffer);
                entry.Crc = crc.Value;
                zipStream.PutNextEntry(entry);
                zipStream.Write(buffer, 0, buffer.Length);
            }
        }
    }
    #endregion

    #region 解压缩
    /// <summary>
    /// 解压缩
    /// </summary>
    /// <param name="压缩文件地址"></param>
    /// <param name="解压到的目录"></param>
    public static void UnZip(string FileToUpZip, string ZipedFolder)
    {
        if (!File.Exists(FileToUpZip))
        {
            return;
        }

        if (!Directory.Exists(ZipedFolder))
        {
            Directory.CreateDirectory(ZipedFolder);
        }

        ZipInputStream s = null;
        ZipEntry theEntry = null;

        string fileName;
        FileStream streamWriter = null;
        try
        {
            s = new ZipInputStream(File.OpenRead(FileToUpZip));
            while ((theEntry = s.GetNextEntry()) != null)
            {
                if (theEntry.Name != String.Empty)
                {
                    fileName = Path.Combine(ZipedFolder, theEntry.Name);
                    ///判断文件路径是否是文件夹

                    if (fileName.EndsWith("/") || fileName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(fileName);
                        continue;
                    }
                    else if(!Directory.Exists(Path.GetDirectoryName(fileName)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    }
                    streamWriter = File.Create(fileName);
                    int size = 2048;
                    byte[] data = new byte[2048];
                    while (true)
                    {
                        size = s.Read(data, 0, data.Length);
                        if (size > 0)
                        {
                            streamWriter.Write(data, 0, size);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
        finally
        {
            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter = null;
            }
            if (theEntry != null)
            {
                theEntry = null;
            }
            if (s != null)
            {
                s.Close();
                s = null;
            }
            GC.Collect();
            GC.Collect(1);
        }
    }

    #endregion
     */
