using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Acer_Update_Checker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ObservableCollection<AcerNode> OTAUpdates { get; set; }

        public const string ServerURL = "http://ws.gtm.acer.com/ServerInfo/en/{0}_{1}/{0}_{1}_{2}_en.xml";

        public const string DownloadURL = "http://global-download.acer.com/GDFiles/";

        private static object _syncLock = new object();

        public static App CurApp;

        public ComboBox ComboDevice
        {
            get
            {
                return (ComboBox)MainWindow.FindName("m_device");
            }
        }

        public ComboBox ComboVersion
        {
            get
            {
                return (ComboBox)MainWindow.FindName("m_androidVersion");
            }
        }

        public ComboBox ComboSKU
        {
            get
            {
                return (ComboBox)MainWindow.FindName("m_sku");
            }
        }

        public string DeviceName
        {
            get
            {

                return (string)((ComboBoxItem)ComboDevice.SelectedValue).Content;
            }
        }

        public string AndroidVersion
        {
            get
            {
                return (string)((ComboBoxItem)ComboVersion.SelectedValue).Content;
            }
        }

        public DataGrid DataResult
        {
            get
            {
                return MainWindow.FindName("m_dataResult") as DataGrid;
            }
        }

        public string AndroidVersionForRequest;

        public App()
        {
            OTAUpdates = new ObservableCollection<AcerNode>();
            //Enable the cross acces to this collection elsewhere;
            BindingOperations.EnableCollectionSynchronization(OTAUpdates, _syncLock);
            CurApp = this;
        }

        public void Button_MouseDown(object sender, EventArgs e)
        {
            DataResult.Items.Clear();

            SetAndroidVersion();

            foreach (ComboBoxItem l_sku in ComboSKU.Items)
            {
                string l_url = string.Format(ServerURL, DeviceName, l_sku.Content, AndroidVersionForRequest);

                HttpWebRequest request = GenerateRequest(l_url, l_sku.Content.ToString());

                try
                {
                    RequestState myRequestState = new RequestState();

                    myRequestState.request = request;

                    IAsyncResult asyncResult = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(RespCallback), myRequestState);
                   // m_result.Text = "Checking for update..." + l_sku.Content;
                }
                catch (WebException ex)
                {
                   // m_result.Text = "Error Occured : " + ex.Message;
                }
            }
        }

        HttpWebRequest GenerateRequest(string url, string sku)
        {
            HttpWebRequest toReturn = (HttpWebRequest)WebRequest.Create(url);
            toReturn.UserAgent = "ALU_99.99.9999_10F3AF2EDE898C81F81";//"OTA_1.000.00_1740B475CAA83B9B22F";
            toReturn.Headers.Add("Acer", "1DA39A3EE5E6B4B0D3255BFEF95601890AFD80709F3AF2ED9DE074A32D");//14208621918BED8424B292653942C08603BD1BA4403CB2D097F978775F");
            toReturn.Headers.Add("SN", "ACER123456789012345630");
            toReturn.Headers.Add("RT", "1");
            //request.Headers.Add("ALURC: 1");
            toReturn.Headers.Add("Pragma", "no-cache");
            toReturn.Headers.Add("SKU: " + sku);
            toReturn.Method = "GET";
            toReturn.Timeout = 5000;

            return toReturn;
        }

        void SetAndroidVersion()
        {
            if (AndroidVersion.Equals("JellyBean"))
            {
                AndroidVersionForRequest = "AV052";
            }
            else if (AndroidVersion.Equals("Lollipop"))
            {
                AndroidVersionForRequest = "AV0L0";
            }
            else
            {
                //Some devices (Intel ?) need specific version
                if (DeviceName.Equals("A1-840FHD") || DeviceName.Equals("A1-724")
                    || DeviceName.Equals("A1-840"))
                    AndroidVersionForRequest = "AV0K1";
                else
                    AndroidVersionForRequest = "AV0K0";
            }
        }

        private void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                OTAUpdates = new ObservableCollection<AcerNode>();

                // Set the State of request to asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                WebRequest myWebRequest = myRequestState.request;
                // End the Asynchronous response.

                myRequestState.response = myWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a 'Stream' object.
                Stream responseStream = myRequestState.response.GetResponseStream();

                //GUnzip Stream
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);

                StreamReader responseReader = new StreamReader(responseStream, Encoding.Default);

                string htmlResult = responseReader.ReadToEnd();

                responseReader.Close();
                responseStream.Close();

                string l_sku = myRequestState.request.Headers.GetValues("SKU")[0];

                ParseHTML(htmlResult, l_sku);

                if (OTAUpdates != null && OTAUpdates.Count > 0)
                {
                    OTAUpdates.OrderBy(o => o.FileID);

                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new ParameterizedThreadStart(UpdateItems), OTAUpdates);
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("WebException raised!");
                Console.WriteLine("\n{0}", e.Message);
                Console.WriteLine("\n{0}", e.Status);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception raised!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
        }

        void ParseHTML(string html, string sku)
        {
            XDocument doc = XDocument.Parse(html);

            if (doc.Element("AcerLiveUpdate") != null &&
                doc.Element("AcerLiveUpdate").Element("Application") != null)
            {
                List<XElement> l_node = doc.Element("AcerLiveUpdate").Element("Application").Elements("Node").ToList();

                if (l_node != null && l_node.Count > 0)
                {

                    foreach (XElement l_element in l_node)
                    {
                        AcerNode l_acerNode = new AcerNode(l_element);
                        l_acerNode.SKU = sku;

                        OTAUpdates.Add(l_acerNode);
                    }
                }
            }
        }

        void UpdateItems(object p_nodes)
        {
            foreach (AcerNode l_node in (ObservableCollection<AcerNode>)p_nodes)
            {
                DataResult.Items.Add(l_node);
            }
        }


        private void DG_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)e.OriginalSource;
            Process.Start(link.NavigateUri.AbsoluteUri);
        }
    }
    public class RequestState
    {
        // This class stores the state of the request. 
        const int BUFFER_SIZE = 1024;
        public StringBuilder requestData;
        public byte[] bufferRead;
        public WebRequest request;
        public WebResponse response;
        public Stream responseStream;
        public RequestState()
        {
            bufferRead = new byte[BUFFER_SIZE];
            requestData = new StringBuilder("");
            request = null;
            responseStream = null;
        }
    }
}
