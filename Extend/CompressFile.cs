using SevenZip;
using SevenZip.Compression.LZMA;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace SevenZip.Extend
{
	public class CompressFile
	{
		public static void CompressAsync(string inpath, string outpath, ProgressDelegate progress)
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Compress));
			thread.Start(new FileChangeInfo
			{
				inpath = inpath,
				outpath = outpath,
				progressDelegate = progress
			});
		}

		public static void DeCompressAsync(string inpath, string outpath, ProgressDelegate progress)
		{
			Thread thread = new Thread(new ParameterizedThreadStart(DeCompress));
			thread.Start(new FileChangeInfo
			{
				inpath = inpath,
				outpath = outpath,
				progressDelegate = progress
			});
		}

		private static void Compress(object obj)
		{
			FileChangeInfo fileChangeInfo = (FileChangeInfo)obj;
			string inpath = fileChangeInfo.inpath;
			string outpath = fileChangeInfo.outpath;
			CodeProgress progress = null;
			if (fileChangeInfo.progressDelegate != null)
			{
				progress = new CodeProgress(fileChangeInfo.progressDelegate);
			}
			try
			{
				Encoder encoder = new Encoder();
                FileStream fileStream = new FileStream(inpath, FileMode.Open);
                progress.fileSize = fileStream.Length;
				FileStream fileStream2 = new FileStream(outpath, FileMode.Create);
				encoder.WriteCoderProperties(fileStream2);
				fileStream2.Write(BitConverter.GetBytes(fileStream.Length), 0, 8);
				encoder.Code(fileStream, fileStream2, fileStream.Length, -1L, progress);
				fileStream2.Flush();
				fileStream2.Close();
				fileStream.Close();
				Debug.Log("压缩完毕");
			}
			catch (Exception ex)
			{
				Debug.Log(ex);
			}
		}

		public static void Compress(string inpath, string outpath, ProgressDelegate progress)
		{
			Compress(new FileChangeInfo
			{
				inpath = inpath,
				outpath = outpath,
				progressDelegate = progress
			});
		}

		private static void DeCompress(object obj)
		{
			FileChangeInfo fileChangeInfo = (FileChangeInfo)obj;
			string inpath = fileChangeInfo.inpath;
			string outpath = fileChangeInfo.outpath;
			CodeProgress progress = null;
			if (fileChangeInfo.progressDelegate != null)
			{
				progress = new CodeProgress(fileChangeInfo.progressDelegate);
			}
			try
			{
				Decoder decoder = new Decoder();
				FileStream fileStream = new FileStream(inpath, FileMode.Open);
                progress.fileSize = fileStream.Length;
                FileStream fileStream2 = new FileStream(outpath, FileMode.Create);
				int num = 5;
				byte[] array = new byte[num];
				fileStream.Read(array, 0, array.Length);
				byte[] array2 = new byte[8];
				fileStream.Read(array2, 0, 8);
				long outSize = BitConverter.ToInt64(array2, 0);
				decoder.SetDecoderProperties(array);
				decoder.Code(fileStream, fileStream2, fileStream.Length, outSize, progress);
				fileStream2.Flush();
				fileStream2.Close();
				fileStream.Close();
				Debug.Log("解压完毕");
			}
			catch (Exception ex)
			{
				Debug.Log(ex);
			}
		}

		public static void DeCompress(string inpath, string outpath, ProgressDelegate progress)
		{
			DeCompress(new FileChangeInfo
			{
				inpath = inpath,
				outpath = outpath,
				progressDelegate = progress
			});
		}
	}
}
