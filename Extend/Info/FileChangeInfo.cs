using System;

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
}
