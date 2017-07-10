using System;
using System.IO;

namespace SevenZip.Extend
{
    public class FileChangeInfo
	{
		public string inpath;

		public string outpath;

		public ProgressDelegate progressDelegate;
	}
    public class FilesChangeInfo
    {
        public string foldername;

        public string[] inpaths;

        public string outpath;

        public ProgressDelegate progressDelegate;
    }

    public class FileStreamChangeInfo
    {
        public string foldername;

        public string[] inpaths;

        public Stream outStream;

        public ProgressDelegate progressDelegate;
    }
}
