﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpCompress.Common;
using SharpCompress.Reader;

namespace SharpCompress.Test
{
    public class TestBase
    {
        protected static string Combine(string a, string b, string c)
        {
            var slash = new[] { '\\' };
            return string.Format("{0}\\{1}\\{2}", a.TrimEnd(slash), b.Trim(slash), c.TrimStart(slash));
        }

        protected readonly static string TEST_BASE_PATH = Directory.GetParent(System.AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        
        protected readonly string TEST_ARCHIVES_PATH = Combine(TEST_BASE_PATH, "TestArchives", "Archives");
        protected readonly string ORIGINAL_FILES_PATH = Combine(TEST_BASE_PATH, "TestArchives", "Original");
        protected readonly string MISC_TEST_FILES_PATH = Combine(TEST_BASE_PATH, "TestArchives", "MiscTest");
        protected readonly string SCRATCH_FILES_PATH = Combine(TEST_BASE_PATH, "TestArchives", "Scratch");
        protected readonly string SCRATCH2_FILES_PATH = Combine(TEST_BASE_PATH, "TestArchives", "Scratch2");

        protected IEnumerable<string> GetRarArchives()
        {
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Rar.none.rar");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Rar.rar");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Rar.solid.rar");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Rar.multi.part01.rar");
        }

        protected IEnumerable<string> GetZipArchives()
        {
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.bzip2.dd.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.bzip2.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.deflate.dd-.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.deflate.dd.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.deflate.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.lzma.dd.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.lzma.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.none.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.ppmd.dd.zip");
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Zip.ppmd.zip");
        }

        protected  IEnumerable<string> GetTarArchives()
        {
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Tar.tar");
        }

        protected  IEnumerable<string> GetTarBz2Archives()
        {
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Tar.tar.bz2");
        }

        protected  IEnumerable<string> GetTarGzArchives()
        {
            yield return Path.Combine(TEST_ARCHIVES_PATH, "Tar.tar.gz");
        }

        public void ResetScratch()
        {
            if (Directory.Exists(SCRATCH_FILES_PATH))
            {
                Directory.Delete(SCRATCH_FILES_PATH, true);
            }
            Directory.CreateDirectory(SCRATCH_FILES_PATH);
            if (Directory.Exists(SCRATCH2_FILES_PATH))
            {
                Directory.Delete(SCRATCH2_FILES_PATH, true);
            }
            Directory.CreateDirectory(SCRATCH2_FILES_PATH);

        }

        public void VerifyFiles()
        {
            if (UseExtensionInsteadOfNameToVerify)
            {
                VerifyFilesByExtension();
            }
            else
            {
                VerifyFilesByName();
            }
        }

        protected void VerifyFilesByName()
        {
            var extracted = Directory
                .GetFiles(SCRATCH_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(path => path.Substring(SCRATCH_FILES_PATH.Length));

            var original = Directory
                .GetFiles(ORIGINAL_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(path => path.Substring(ORIGINAL_FILES_PATH.Length));

            Assert.AreEqual(extracted.Count, original.Count);

            foreach (var orig in original)
            {
                Assert.IsTrue(extracted.Contains(orig.Key));

                CompareFilesByPath(orig.Single(), extracted[orig.Key].Single());
            }
        }

        protected bool UseExtensionInsteadOfNameToVerify { get; set; }

        protected void VerifyFilesByExtension()
        {
            var extracted = Directory
                .GetFiles(SCRATCH_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(Path.GetExtension);
            var original = Directory
                .GetFiles(ORIGINAL_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(Path.GetExtension);

            Assert.AreEqual(extracted.Count, original.Count);

            foreach (var orig in original)
            {
                Assert.IsTrue(extracted.Contains(orig.Key));

                CompareFilesByPath(orig.Single(), extracted[orig.Key].Single());
            }
        }

        protected void CompareFilesByPath(string file1, string file2)
        {
            using (var file1Stream = File.OpenRead(file1))
            using (var file2Stream = File.OpenRead(file2))
            {
                Assert.AreEqual(file1Stream.Length, file2Stream.Length);
                int byte1 = 0;
                for (int counter = 0; byte1 != -1; counter++)
                {
                    byte1 = file1Stream.ReadByte();
                    int byte2 = file2Stream.ReadByte();
                    if (byte1 != byte2)
                    {
                        Assert.AreEqual(byte1, byte2,
                            string.Format("Byte {0} differ between {1} and {2}", counter, file1, file2));
                    }
                }
            }
        }

        protected void CompareArchivesByPath(string file1, string file2)
        {
            using (var archive1 = ReaderFactory.Open(File.OpenRead(file1), Options.None))
            using (var archive2 = ReaderFactory.Open(File.OpenRead(file2), Options.None))
            {
                while (archive1.MoveToNextEntry())
                {
                    Assert.IsTrue(archive2.MoveToNextEntry());
                    Assert.AreEqual(archive1.Entry.Key, archive2.Entry.Key);
                }
                Assert.IsFalse(archive2.MoveToNextEntry());
            }
        }

        private static readonly object testLock = new object();

        [TestInitialize]
        public void TestSetup()
        {
            Monitor.Enter(testLock);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Monitor.Exit(testLock);
        }
    }
}