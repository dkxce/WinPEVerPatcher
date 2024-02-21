//
// C# 
// WinPEVerPatcher Config
// v 0.1, 21.02.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace WinPEVerPatcher
{
    public class Config : XMLSaved<Config>
    {
        public class Property
        {
            #region XML

            [XmlAttribute("name")]
            public string name;

            [XmlText]
            public string value;

            #endregion XML

            #region Constructor
            public Property() { }

            public Property(string name) { this.name = name; }

            public Property(string name, string value) { this.name = name; this.value = value; }

            #endregion Constructor

            #region Override
            public override string ToString() { return String.Format("{0,-20} = {1}", name, value); }
            #endregion Override
        }

        #region Private
        private string fileName;
        private string regex;
        #endregion Private

        #region Public (XML)

        [XmlElement("FileName")]
        public string FileName
        {
            get { return fileName; }
            set
            {
                string val = value.Replace("%CD%", XMLSaved<int>.CurrentDirectory()).Replace("\\\\", "\\");
                fileName = Environment.ExpandEnvironmentVariables(val).Replace("\\\\", "\\");
            }
        }

        [XmlElement("FileRegex")]
        public string FileRegex { get { return regex; } set { regex = value; } }

        [XmlArray("Properties")]
        public List<Property> Properties = new List<Property>();

        [XmlElement("OnDone")]
        public string OnDone = null;

        [XmlElement("SelectFile")]
        public bool SelectFile = true;

        #endregion Public (XML)

        #region XmlIgnore

        [XmlIgnore]
        public string CfgFilePath
        {
            get
            {
                string fn = Path.GetFileName(fileName);
                int iof = fileName.LastIndexOf(fn);
                return fileName.Substring(0, iof);
            }
        }

        [XmlIgnore]
        public string CfgFileMask { get { return Path.GetFileName(fileName); } }

        #endregion XmlIgnore

        #region Methods

        public List<string> GetFileNames()
        {
            List<string> res = new List<string>();
            foreach (string f in Directory.GetFiles(CfgFilePath, CfgFileMask, SearchOption.TopDirectoryOnly))
            {
                if (string.IsNullOrEmpty(regex))
                    res.Add(f);
                else
                    try
                    {
                        Regex rx = new Regex(regex, RegexOptions.IgnoreCase);
                        Match mx = rx.Match(Path.GetFileName(f));
                        if (mx.Success) res.Add(f);
                    }
                    catch { };
            };
            return res;
        }

        public string GetFileName()
        {
            foreach (string f in Directory.GetFiles(CfgFilePath, CfgFileMask, SearchOption.TopDirectoryOnly))
            {
                if (string.IsNullOrEmpty(regex)) return f;
                try
                {
                    Regex rx = new Regex(regex, RegexOptions.IgnoreCase);
                    Match mx = rx.Match(Path.GetFileName(f));
                    if (mx.Success) return f;
                }
                catch { };
            };
            return null;
        }

        public Dictionary<string, string> GetFileParts(string filename)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            try
            {
                Regex rx = new Regex(regex, RegexOptions.IgnoreCase);
                Match mx = rx.Match(Path.GetFileName(filename));
                foreach (Group g in mx.Groups)
                    res.Add(g.Name, g.Value);
            }
            catch { };
            return res;
        }

        public Dictionary<string, string> GetFileParams(string filename)
        {
            Dictionary<string, string> replaceFVI = GetReplaceFVI(filename);
            Dictionary<string, string> res = new Dictionary<string, string>();
            string path = Path.GetDirectoryName(filename);
            string file = Path.GetFileName(filename);

            foreach (Property p in Properties)
            {
                string v = p.value;
                v = v.Replace("%CD%", XMLSaved<int>.CurrentDirectory());
                foreach (KeyValuePair<string, string> kvp in replaceFVI)
                    v = v.Replace($"${kvp.Key}$", kvp.Value);
                foreach (KeyValuePair<string, string> kvp in GetFileParts(file))
                    v = v.Replace($"%{kvp.Key}%", kvp.Value);
                res.Add(p.name, v.Trim());
            };

            return res;
        }

        private static Dictionary<string, string> GetReplaceFVI(string filename)
        {
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(filename);
            Dictionary<string, string> replaceFVI = new Dictionary<string, string>();
            replaceFVI.Add("FILE_COMPANY", fvi.CompanyName);
            replaceFVI.Add("FILE_DESCRIPTION", fvi.FileDescription);
            replaceFVI.Add("FILE_COMMENT", fvi.Comments);
            replaceFVI.Add("FILE_VERSION", fvi.FileVersion);
            replaceFVI.Add("FILE_LANGUAGE", fvi.Language);
            replaceFVI.Add("FILE_COPYRIGHTS", fvi.LegalCopyright);
            replaceFVI.Add("PRODUCT_NAME", fvi.ProductName);
            replaceFVI.Add("PRODUCT_VERSION", fvi.ProductVersion);
            return replaceFVI;
        }

        #endregion Methods

        public void ReInitProperties()
        {
            for (int i = Properties.Count - 1; i >= 0; i--)
            {
                if (Properties[i].name == null) { Properties.RemoveAt(i); continue; };
                if (Properties[i].name.Trim().Length == 0) { Properties.RemoveAt(i); continue; };
                if (Properties[i].value == null) { Properties.RemoveAt(i); continue; };
                if (Properties[i].value.Trim().Length == 0) { Properties.RemoveAt(i); continue; };
            };
        }

        #region LoadSave

        public void Save()
        {
            string path = Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.xml");
            XMLSaved<Config>.Save(path, this);
        }

        public static Config Init(string file = null)
        {
            string path = string.IsNullOrEmpty(file) ? Path.Combine(XMLSaved<int>.CurrentDirectory(), "WinPEVerPatcher.xml") : file;
            return XMLSaved<Config>.Load(path);
        }

        #endregion LoadSave

        #region Static
        public static string ToReadableSize(double value)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value /
                        Math.Pow(1024, i)) +
                        " " + suffixes[i];
                };
            };

            return ThreeNonZeroDigits(value /
                Math.Pow(1024, suffixes.Length - 1)) +
                " " + suffixes[suffixes.Length - 1];
        }

        private static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
            {
                // No digits after the decimal.
                return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (value >= 10)
            {
                // One digit after the decimal.
                return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                // Two digits after the decimal.
                return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        #endregion Static

        public override string ToString()
        {
            return $"{FileName} P{Properties?.Count}";
        }
    }
}

/* SAMPLE 
 
<?xml version="1.0" encoding="utf-8"?>
<Config>  

  <FileName>%CD%\*.exe</FileName>
  <FileRegex>^(?&lt;FileName&gt;(?!WinPEVerPatcher).*\.exe)$</FileRegex>  
  <SelectFile>1</SelectFile>
  
  <Properties>
    <Property name="Comments"> </Property>       
    <Property name="CompanyName"> </Property>       
    <Property name="FileDescription"> </Property>       
    <Property name="FileVersion"> </Property>       
    <Property name="InternalName"> </Property>       
    <Property name="LegalCopyright"> </Property>       
    <Property name="LegalTrademarks"> </Property>       
    <Property name="OriginalFilename"> </Property>       
    <Property name="ProductName"> </Property>       
    <Property name="ProductVersion"> </Property>       
    <Property name="Assembly Version"> </Property>       
  </Properties>
  
  <OnDone>notepad.exe WinPEVerPatcher.log</OnDone>
  <!--OnDone>WinPEVerPatcher.cmd</OnDone-->
  <!--OnDone>WinPEVerPatcher.cmd</OnDone-->
  
  <!-- 
	PROPERTIES REPLACEMENTS:
	
	%CD% - Current Directory
	
	%ENVIRONMENT_VARIABLE% - Windows Environment Variables
	%ALLUSERSPROFILE%
	%APPDATA%
	%CommonProgramFiles%
	%COMMONPROGRAMFILES(x86)%
	%COMPUTERNAME%
	%DATE%
	%HOMEDRIVE%
	%HOMEPATH%
	%LOCALAPPDATA%
	%PROCESSOR_ARCHITECTURE%
	%ProgramData%
	%ProgramFiles%
	%ProgramFiles(x86)%
	%ProgramW6432%
	%Public%
	%RANDOM%
	%SYSTEMDRIVE%
	%SYSTEMROOT%
	%TEMP%
	%TIME%
	%TMP%
	%USERNAME%
	%USERPROFILE%
	%WINDIR%
	
	%ANY_NAME% - FOR REPLACE WITH REGEX (?<ANY_NAME>...)
	%FileName% 
	%FileVersion% 
	
	$FILE_COMPANY$
	$FILE_DESCRIPTION$
	$FILE_COMMENT$
	$FILE_COMMENT$
	$FILE_VERSION$
	$FILE_LANGUAGE$
	$FILE_COPYRIGHTS$
	$PRODUCT_NAME$
	$PRODUCT_VERSION$
  -->
  
</Config>

*/