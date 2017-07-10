using SevenZip;
using SevenZip.Compression.LZMA;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace SevenZip.Extend
{
    public class CodeProgress : ICodeProgress
    {
        public ProgressDelegate m_ProgressDelegate = null;
        public long fileSize;
        public CodeProgress(ProgressDelegate del)
        {
            this.m_ProgressDelegate = del;
        }

        public void SetProgress(long inSize, long outSize)
        {
            this.m_ProgressDelegate.Invoke(fileSize, inSize, outSize);
        }
    }
}
