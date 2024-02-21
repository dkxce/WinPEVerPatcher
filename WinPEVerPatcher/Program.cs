//
// C# 
// WinPEVerPatcher
// v 0.1, 21.02.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

using AsmResolver.PE.File.Headers;
using AsmResolver.PE.File;
using AsmResolver.PE.Win32Resources.Builder;
using AsmResolver.PE.Win32Resources.Version;
using AsmResolver.PE;
using System.Reflection;
using AsmResolver.PE.DotNet.Metadata;
using System.Text;

namespace WinPEVerPatcher
{
    internal class Program
    {
        private static byte[] NSIS = Encoding.ASCII.GetBytes("Nullsoft.NSIS.exehead");
        private static string cfgFile = "WinPEVerPatcher.xml";
        private static Config cfg = null;
        private static int sleep_error = 2500;
        private static int sleep_ok = 10;
        private static int sleep_wait = 750;

        static void Main(string[] args)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string fileVersion = fvi.FileVersion;
            string description = fvi.Comments;
            string subVersion = fvi.LegalTrademarks;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // BEGIN OUTPUT
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteNoLog($"{description}");
                WriteNoLog($"  v{fileVersion} {subVersion}");
                WriteNoLog($"  Usage: WinPEVerPatcher.exe [config_file]");
            };

            // SAVE DEFAULT CONFIGS
            InitDlls();

            // LOAD CONFIG
            if (args != null && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    try
                    {
                        if (File.Exists(arg))
                        {
                            cfgFile = Path.GetFileName(arg);
                            cfg = Config.Init(arg);
                        };
                    }
                    catch { };
                };
            }
            else cfg = Config.Init();            

            // INIT LOG
            ClearLog();
            WriteToLog($";# {description}");
            WriteToLog($";# v{fileVersion} {subVersion}");
            WriteToLog($";# Launched at {DateTime.Now}", false);
            WriteToLog();

            // CONFIGURATION
            {
                WriteWithLog("CONFIGURATION: {");
                WriteWithLog("  File   : {0}", cfgFile);
                WriteWithLog("  Path   : {0}", cfg.CfgFilePath);
                WriteWithLog("  Mask   : {0}", cfg.CfgFileMask);
                WriteWithLog("  Regex  : {0}", cfg.FileRegex);
                WriteWithLog("  OnDone : {0}", cfg.OnDone);
                WriteWithLog("  Select : {0}", cfg.SelectFile);
                WriteWithLog("  Properties : {0}", cfg.Properties.Count);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                foreach (Config.Property p in cfg.Properties)
                    WriteWithLog("    {0}", p);
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteWithLog("}");
            };

            // Init Props
            cfg.ReInitProperties();
            if (cfg.Properties.Count == 0) // ERROR
            {
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Failed");
                WriteWithLog("  Error  : {0}", "Properties is not Set");
                WriteWithLog("  Exit   : {0}", 4);
                WriteWithLog("}");
                Exit(4); return;
            };

            // LOAD FILE(S)
            List<string> files = cfg.GetFileNames();
            if (files.Count == 0) // ERROR
            {
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Failed");
                WriteWithLog("  Error  : {0}", "No any file found");
                WriteWithLog("  Exit   : {0}", 2);
                WriteWithLog("}");
                Exit(2); return;
            };

            // SELECT FILE
            string f = files[0];
            if (files.Count > 1 && cfg.SelectFile) // SELECT
            {
                WriteWithLog("FILES: {");
                int fid = 0;
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (string fi in files)
                    WriteWithLog("  {0}: {1}", fid++, Path.GetFileName(fi));
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteWithLog("}");

                WriteWithLog("SELECT: {");
                Console.WriteLine("  -- TYPE exit OR abort TO EXIT --");
                int top = Console.CursorTop;
                while (true)
                {
                    Console.Write("  CHOOSE FILE NUMBER: ");
                    string fNo = Console.ReadLine().Trim();

                    Console.CursorTop = top; Console.CursorLeft = 0;
                    for (int i = 0; i < Console.WindowWidth * 2; i++) Console.Write(" ");
                    Console.CursorTop = top;

                    if (fNo == "abort" || fNo == "exit")
                    {
                        WriteWithLog("  NUMBER: {0}", -1);
                        WriteWithLog("  FILE  : {0}", "NONE");
                        WriteWithLog("}");
                        WriteWithLog("RESULT: {");
                        WriteWithLog("  Status : {0}", "Failed");
                        WriteWithLog("  Error  : {0}", "Aborted by User");
                        WriteWithLog("  Exit   : {0}", 1);
                        WriteWithLog("}");
                        Exit(1); return;
                    };
                    if (int.TryParse(fNo, out fid) && fid >= 0 && fid < files.Count) break;
                };
                f = files[fid];
                WriteWithLog("  NUMBER: {0}", fid);
                Console.ForegroundColor = ConsoleColor.Green;
                WriteWithLog("  FILE  : {0}", Path.GetFileName(files[fid]));
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteWithLog("}");
            };

            // PRE INITIALIZATION
            Dictionary<string, string> props = new Dictionary<string, string>();

            try // INITIALIZATION
            {
                WriteWithLog("INITIALIZATION: {");
                WriteWithLog("  Path   : {0}", Path.GetDirectoryName(f));
                WriteWithLog("  File   : {0}", Path.GetFileName(f));
                WriteWithLog("  Size   : {0}", Config.ToReadableSize((new FileInfo(f)).Length));
                Dictionary<string, string> s_props = cfg.GetFileParams(f);
                WriteWithLog("  Properties : " + s_props.Count.ToString());
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (KeyValuePair<string, string> p in s_props)
                {
                    if (p.Key == null || string.IsNullOrEmpty(p.Key.Trim()) || p.Value == null || string.IsNullOrEmpty(p.Value.Trim())) continue;
                    props.Add(p.Key, p.Value);
                    WriteWithLog(String.Format("    {0,-20} = {1}", p.Key, p.Value));
                };
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteWithLog("}");
            }
            catch (Exception ex)
            {
                WriteWithLog("}");
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Failed");
                WriteWithLog("  Error  : {0}", ex.Message);
                WriteWithLog("  Exit   : {0}", 3);
                WriteWithLog("}");
                Exit(3); return;
            };

            if (props.Count == 0) // ERROR
            {
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Failed");
                WriteWithLog("  Error  : {0}", "Properties is not Set");
                WriteWithLog("  Exit   : {0}", 4);
                WriteWithLog("}");
                Exit(5); return;
            };

            System.Threading.Thread.Sleep(sleep_wait);
            long wasLength = new FileInfo(f).Length;
            string resFile = f;
            try { patch(f, props); }
            catch (Exception ex)
            {
                WriteWithLog("}");
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Failed");
                WriteWithLog("  Error  : {0}", ex.Message);
                WriteWithLog("  Exit   : {0}", 6);
                WriteWithLog("}");
                Exit(6); return;
            };

            // COMPILATION
            {
                WriteWithLog("COMPILATION: {");
                WriteWithLog("  Path   : {0}", Path.GetDirectoryName(resFile));
                WriteWithLog("  File   : {0}", Path.GetFileName(resFile));
                WriteWithLog("  PrevSz : {0}", Config.ToReadableSize(wasLength));
                WriteWithLog("  NewSz  : {0}", Config.ToReadableSize((new FileInfo(resFile)).Length));
                WriteWithLog("}");
            };

            // RESULT
            {
                WriteWithLog("RESULT: {");
                WriteWithLog("  Status : {0}", "Build succeeded successfully");
                WriteWithLog("  Exit   : {0}", 0);
                WriteWithLog("}");

                try { if (!string.IsNullOrEmpty(cfg.OnDone)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(cfg.OnDone, $"\"{resFile}\"") { UseShellExecute = true }); }
                catch { try { if (!string.IsNullOrEmpty(cfg.OnDone)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/C {cfg.OnDone}") { UseShellExecute = true }); } catch { } };

                Exit(0); return;
            };
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assmPath = GetTemporaryDirectory();

            string assemblyNameString = (new AssemblyName(args.Name))?.Name;
            if (assemblyNameString == null) return null;

            if (!assemblyNameString.StartsWith("AsmResolver")) return null;
            const string extension = ".dll";
            string fName = assmPath + assemblyNameString + extension;
            
            if(!File.Exists(fName)) return null;

            Assembly assembly = Assembly.LoadFrom(fName);
            return assembly;
        }

        public static string GetTemporaryDirectory(bool create = true, string dirName = "WinPEVerPatcher")
        {
            string tempDirectory;
            if (string.IsNullOrEmpty(dirName))
                tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            else
                tempDirectory = Path.Combine(Path.GetTempPath(), dirName);

            try { Directory.CreateDirectory(tempDirectory); } catch { };
            tempDirectory = tempDirectory.TrimEnd('\\') + @"\";
            return tempDirectory;
        }

        private static void ClearLog()
        {
            string logFile = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.log");
            try { if (File.Exists(logFile)) File.Delete(logFile); } catch { }
        }

        private static void WriteNoLog(string line = "", params object[] pars)
        {
            line = pars == null || pars.Length == 0 ? line : String.Format(line, pars);
            Console.WriteLine(line);
        }

        private static void WriteToLog(string line = "", params object[] pars)
        {
            line = pars == null || pars.Length == 0 ? line : String.Format(line, pars);
            string logFile = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.log");
            try
            {
                using (FileStream fs = new FileStream(logFile, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                    sw.WriteLine(line);
            }
            catch { };
        }

        private static void WriteWithLog(string line = "", params object[] pars)
        {
            line = pars == null || pars.Length == 0 ? line : String.Format(line, pars);
            Console.WriteLine(line);
            string logFile = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.log");
            try
            {
                using (FileStream fs = new FileStream(logFile, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                    sw.WriteLine(line);
            }
            catch { };
        }

        private static void InitDlls()
        {
            string tmpDir = GetTemporaryDirectory();
            if (Directory.GetFiles(tmpDir, "*.dll").Length == 0) 
            {
                File.WriteAllBytes(Path.Combine(tmpDir,"AsmResolver.dll"), global::WinPEVerPatcher.Properties.Resources.AsmResolver_dll);
                File.WriteAllBytes(Path.Combine(tmpDir, "AsmResolver.PE.dll"), global::WinPEVerPatcher.Properties.Resources.AsmResolver_PE_dll);
                File.WriteAllBytes(Path.Combine(tmpDir, "AsmResolver.PE.File.dll"), global::WinPEVerPatcher.Properties.Resources.AsmResolver_PE_File_dll);
                File.WriteAllBytes(Path.Combine(tmpDir, "AsmResolver.PE.Win32Resources.dll"), global::WinPEVerPatcher.Properties.Resources.AsmResolver_PE_Win32Resources_dll);
            };
            
            string CMD = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.cmd");
            if (!File.Exists(CMD)) File.WriteAllText(CMD, global::WinPEVerPatcher.Properties.Resources.CMD);

            string XML = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.xml");
            if (!File.Exists(XML)) File.WriteAllText(XML, global::WinPEVerPatcher.Properties.Resources.XML);
        }
        private static void Exit(int code)
        {
            WriteToLog();
            WriteToLog($";# Ended at {DateTime.Now}", false);
            System.Threading.Thread.Sleep(code == 0 ? sleep_ok : sleep_error);
            Erase();
            Environment.Exit(code);
        }

        private static void Erase()
        {
            try { foreach (string f in Directory.GetFiles(GetTemporaryDirectory(), "*.dll")) File.Delete(f); } catch { };
        }

        private static void patch(string fileName, Dictionary<string, string> props)
        {
            if (props == null || props.Count == 0) return;

            PEFile file = PEFile.FromFile(fileName);
            IPEImage image = PEImage.FromFile(file);

            // Check NSIS //
            if (new FileInfo(fileName).Length < 10_485_760)
            {
                byte[] fbytes = File.ReadAllBytes(fileName);
                bool isNsis = SeekData(fbytes, 0, fbytes.Length, NSIS) > 0;
                fbytes = null;
                if (isNsis)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    WriteWithLog("CAUTION: {");
                    WriteWithLog("  Status : {0}", "NSIS Executable Detected");
                    WriteWithLog("  Message: {0}", "Ensure that you set ``CRCCheck off``");
                    WriteWithLog("}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Threading.Thread.Sleep(500);
                };
            };

            if (image.Certificates != null && image.Certificates.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
                WriteWithLog("WARNING: {");
                WriteWithLog("  Status : {0}", $"{image.Certificates.Count} Certificate(s) has been Found!");
                WriteWithLog("  Message: {0}", "Certificate signature will be corrupted!");
                WriteWithLog("}");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.Black;

                Console.Write("Do you with to continue? [Y/N]: ");                
                if (!Console.ReadLine().Trim().ToLower().StartsWith("y")) throw new Exception("Aborted by user");
            };            

            // Open version info from the PE's resources:
            VersionInfoResource versionInfo = VersionInfoResource.FromDirectory(image.Resources);

            // Update the file description to something.
            StringFileInfo info = versionInfo.GetChild<StringFileInfo>(StringFileInfo.StringFileInfoKey);
            for (int j = 0; j < info.Tables.Count; j++)
                foreach(KeyValuePair<string,string> prop in props)
                    info.Tables[j][prop.Key] = prop.Value;

            //info.Tables[0]["Comments"] = "This is a test application";
            //info.Tables[0]["CompanyName"] = "This is a test application";
            //info.Tables[0]["FileDescription"] = "This is a test application";
            //info.Tables[0]["FileVersion"] = "0.0.0.0";
            //info.Tables[0]["InternalName"] = "0.0.0.0";
            //info.Tables[0]["LegalCopyright"] = "0.0.0.0";
            //info.Tables[0]["LegalTrademarks"] = "0.0.0.0";
            //info.Tables[0]["OriginalFilename"] = "0.0.0.0";
            //info.Tables[0]["ProductName"] = "0.0.0.0";
            //info.Tables[0]["ProductVersion"] = "0.0.0.0";
            //info.Tables[0]["Assembly Version"] = "0.0.0.0";

            // Write version info back into resources directory.
            versionInfo.WriteToDirectory(image.Resources);

            // We need to rebuild the resources section, let's create a new buffer and add the directory.
            ResourceDirectoryBuffer resourceBuffer = new ResourceDirectoryBuffer();
            resourceBuffer.AddDirectory(image.Resources);

            // Obtain the original resource section, and replace its contents.
            PESection section = file.GetSectionContainingRva(file.OptionalHeader.GetDataDirectory(DataDirectoryIndex.ResourceDirectory).VirtualAddress);
            section.Contents = resourceBuffer;

            // Update offsets and sizes.
            file.UpdateHeaders();
            file.OptionalHeader.SetDataDirectory(DataDirectoryIndex.ResourceDirectory,
                new DataDirectory(resourceBuffer.Rva, resourceBuffer.GetPhysicalSize()));

            // Write to disk.
            file.Write(fileName);
        }

        public static int SeekData(byte[] data, int offset, int dataSize, byte[] b2seek)
        {
            int b2seekSize = b2seek.Length;
            int matched = 0;
            for (int pos = offset; pos < dataSize; pos++)
                if (data[pos] != b2seek[matched]) matched = 0;
                else if (++matched == b2seekSize)
                    return pos + 1 - b2seekSize;
            return -1;
        }
    }
}
