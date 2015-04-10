using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Acer_Update_Checker
{
    public class AcerNode
    {
        public const string DownloadURL = "http://global-download.acer.com/GDFiles/";

        public string Name { get; set; }

        public string ObjID { get; set; }

        public string FileID { get; set; }

        public string Class { get; set; }

        public string InstallFile { get; set; }

        public string SKU { get; set; }

        public string DownloadLink
        {
            get
            {
                return DownloadURL + GetDownloadURL();
            }
        }

        public string FileSize { get; set; }

        public string FormatedFileSize
        {
            get
            {
                return GetFormattedFileSize();
            }
        }

        public string Publisher { get; set; }

        public AcerNode()
        {

        }

        public AcerNode(XElement p_node)
        {
            var l_properties = this.GetType().GetProperties();
            foreach (var l_prop in l_properties)
            {
                if (l_prop.PropertyType != typeof(String))
                    continue;

                if (p_node.Attribute(l_prop.Name) != null)
                {

                    if (!string.IsNullOrEmpty(p_node.Attribute(l_prop.Name).Value))
                        l_prop.SetValue(this, p_node.Attribute(l_prop.Name).Value);
                }
            }
        }

        public void SetSKU(string p_sku)
        {
            SKU = p_sku;
        }

        public string GetDownloadURL()
        {
            return InstallFile.Replace(@"\", "/");
        }

        public string GetFormattedFileSize()
        {
            return FormatBytes(long.Parse(FileSize));
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            if (bytes > 1024)
                for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                    dblSByte = bytes / 1024.0;
            return String.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
        }

        public string GetFromAndroidVersion()
        {
            string l_return = "";

            if (!string.IsNullOrEmpty(Name))
            {
                l_return = Name.Split('_')[0];
            }

            return GetAndroidVersion(l_return);
        }

        public string GetToAndroidVersion()
        {
            string l_return = "";

            if (!string.IsNullOrEmpty(Name))
            {
                l_return = Name.Split('_')[2];
            }

            return GetAndroidVersion(l_return);
        }

        public string GetFromBuildVersion()
        {
            string l_return = "";

            if (!string.IsNullOrEmpty(Name))
            {
                l_return = Name.Split('_')[1];
            }

            return GetAndroidVersion(l_return);
        }

        public string GetToBuildVersion()
        {
            string l_return = "";

            if (!string.IsNullOrEmpty(Name))
            {
                l_return = Name.Split('_')[3];
            }

            return GetAndroidVersion(l_return);
        }

        private static string GetAndroidVersion(string p_version)
        {
            if (p_version.Equals("AV052"))
                return "JellyBean";
            else if (p_version.Equals("AV0K0"))
                return "KitKat";

            return string.Empty;
        }
    }
}
