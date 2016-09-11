﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Common;
using Microsoft.Win32;
using htd = HtmlAgilityPack.HtmlDocument;

namespace STDLite
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            InitializeComponent();

            ResetFormTitle();

            lstMain.ListViewItemSorter = new ListViewColumnSorter();
            lstMain.ColumnClick += ListViewHelper.ListView_ColumnClick;

            var value = RegistryHelper.ReadValue(Registry.LocalMachine, "Software\\STDLite", "OnTop");
            TopMost = "True" == value;
            mnuOnTop.Checked = "True" == value;

        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dllName = args.Name.Contains(",")
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            var rsc = new ResourceManager(GetType().Namespace + ".Properties.Resources",
                Assembly.GetExecutingAssembly());
            var buffer = (byte[]) rsc.GetObject(dllName);

            return Assembly.Load(buffer);
        }

        private void ResetFormTitle()
        {
            const string title = "{0}  V{1}   工艺系统室专版";
            Text = string.Format(title,
                Application.ProductName,
                Application.ProductVersion);
        }

        private void txtKeyword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 27) txtKeyword.Clear();
        }

        private static void Copy2Clipboard(string content)
        {
            Clipboard.Clear(); // 清空剪切板内容
            Clipboard.SetData(DataFormats.Text, content); // 复制内容到剪切板
        }

        private static bool DownloadFile(string fileName, string uri)
        {
            if (File.Exists(fileName))
            {
                // 如果文件存在直接返回
                return true;
            }

            //var md = new MultiDownload(5, uri, fileName);
            //md.Start();

            var wc = new WebClient();
            var datas = wc.DownloadData(uri);
            if (datas.Length < 10000) 
            {
                // URI不存在给出提示
                MessageBox.Show("/(ㄒoㄒ)/~~SEG标准库暂未收录该标准!", "友情提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var fs = new FileStream(fileName, FileMode.Create);
            fs.Write(datas, 0, datas.Length);
            fs.Close();

            return true;
        }

        private static string AddBlank(string number)
        {
            if (number.Length > 0 && char.IsLetter(number.First()))
            {
                foreach (var c in number.Where(char.IsDigit))
                {
                    return number.Insert(number.IndexOf(c), " ");
                }
            }

            return number;
        }

        /// <summary>
        ///     下载网页源码
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="postData">post数据</param>
        /// <returns></returns>
        private string RetrieveHtml(string url, string postData="")
        {
            var htmlContent = string.Empty;
            var wc = new WebClient();
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            try
            {
                var html = postData == string.Empty
                    ? wc.DownloadData(url)
                    : wc.UploadData(url, "POST", Encoding.UTF8.GetBytes(postData));
                htmlContent = Encoding.UTF8.GetString(html);
            }
            catch (Exception)
            {
                MessageBox.Show("连接SEG标准网站失败", "报错提示", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }
            finally
            {
                ResetFormTitle();
            }

            return htmlContent;
        }

        private void GetStandard(string url, ref List<object> items)
        {
            const string postData =
                "__EVENTTARGET=stdlist%24lbtn_refresh_2&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2NjMyMjA0OTYPZBYCAgEPZBYEZg8PZBYCHgVzdHlsZQUkbWFyZ2luOjBweDtwYWRkaW5nOjBweDtkaXNwbGF5Om5vbmU7ZAIBDw9kFgIfAAUkZGlzcGxheTpub25lO21hcmdpbjowcHg7cGFkZGluZzowcHg7ZBgBBR5fX0NvbnRyb2xzUmVxdWlyZVBvc3RCYWNrS2V5X18WCwUdc3RkbGlzdCRTb3J0RmllbGRfUGVyZm9ybWRhdGUFHXN0ZGxpc3QkU29ydEZpZWxkX1B1Ymxpc2hkYXRlBR1zdGRsaXN0JFNvcnRGaWVsZF9QdWJsaXNoZGF0ZQUdc3RkbGlzdCRTb3J0RmllbGRfRXhwaXJlZGRhdGUFHXN0ZGxpc3QkU29ydEZpZWxkX0V4cGlyZWRkYXRlBRxzdGRsaXN0JFNvcnRGaWVsZF9QZXJtaXRkZXB0BRxzdGRsaXN0JFNvcnRGaWVsZF9QZXJtaXRkZXB0BRlzdGRsaXN0JFNvcnRGaWVsZF9TdGRjb2RlBRlzdGRsaXN0JFNvcnRGaWVsZF9TdGRjb2RlBRtzdGRsaXN0JFNvZnRGaWVsZF9WaWV3Q291bnQFG3N0ZGxpc3QkU29mdEZpZWxkX1ZpZXdDb3VudO8M73tN9IyZEmIzbo3Rt9RG6tux&__EVENTVALIDATION=%2FwEWKwLO8JzAAwKHpeXgAQL7pL24BAKLtoHNCAKH9772BgKR9%2FPABALA9NmOBwLgoYrZCQLA9N2OBwK9s4GIDgLI%2BY3QAgKX%2BOKqAgLh%2B4PaDQKdsbKgDgKi%2F46dBALzwuvZBgLJ2OjZBgKKy%2BGqDQLqycYZAv7lkOIDAurJyjkC%2FuWUggQC6sme8AEC%2FuW4uQUC6smikAIC%2FuW82QUC6smWsAEC%2FuWg%2BQQC6sma0AEC%2FuWkmQUC6smu6QIC%2FuWIsgYC6smyiQMC%2FuWM0gYC6smmsAIC%2FuWw%2BQUC6smq0AIC%2FuW0mQYCmPePxAoCwPThjgcC96GGwgYCwPTFjgcC5trLyAEB9COtPt6LDYSCzyGfSC34U4IzoA%3D%3D&stdlist%24hd_dir=&stdlist%24hd_key=635983975871817969&stdlist%24txt_page_count_top=100000&stdlist%24txt_page_index_top=1&stdlist%24SortFieldGrp1=SortField_Performdate&stdlist%24cboSortDirection=%E9%99%8D%E5%BA%8F&stdlist%24txt_page_count_btm=10&stdlist%24txt_page_index_btm=1";
            var html = RetrieveHtml(url, postData);

            var doc = new htd();
            doc.LoadHtml(html);

            const string xPath = "//a[@href]";
            var nodes = doc.DocumentNode.SelectNodes(xPath);

            foreach (var node in nodes)
            {
                if (!node.InnerText.Contains("---")) continue;
                var innerText = node.InnerText.Replace("---", "^").Split('^');
                items.Add(new
                {
                    // 标准号
                    number = AddBlank(innerText[1].ToUpper()),
                    // 标准名
                    name = innerText[0],
                    // 下载地址
                    link = "http://10.113.1.69/std/showEBook.aspx?fileid=" +
                           node.Attributes["href"].Value.Replace("../std/stdmgr.aspx?fileid=", ""),
                    // 是否过期
                    validate = node.ParentNode.ParentNode.ChildNodes[13].ChildNodes[1].InnerText.Replace("标准状态:", "")
                });
            }

            items = items.Distinct().ToList();
        }

        private void AddListItems(IEnumerable<object> items)
        {
            lstMain.BeginUpdate();
            lstMain.Items.Clear();

            foreach (var item in items)
            {
                // 通过反射获取匿名对象属性
                var lvi = new ListViewItem(item.GetType().GetProperty("number").GetValue(item, null).ToString());
                lvi.SubItems.Add(item.GetType().GetProperty("name").GetValue(item, null).ToString());
                lvi.SubItems.Add(item.GetType().GetProperty("validate").GetValue(item, null).ToString());
                lvi.SubItems.Add(item.GetType().GetProperty("link").GetValue(item, null).ToString());
                lstMain.Items.Add(lvi);
                switch (item.GetType().GetProperty("validate").GetValue(item, null).ToString())
                {
                    case "未实施":
                        lvi.ForeColor = Color.Green;
                        break;
                    case "已过期":
                        lvi.ForeColor = Color.Red;
                        break;
                }
            }

            lstMain.EndUpdate();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var keyword = txtKeyword.Text.ToLower().Replace("t", "/t");
            txtKeyword.Text = keyword;

            Text = "努力检索中...马上就来";
            var items = new List<object>();
            // 搜索标准号
            var url = string.Format("http://10.113.1.69/std/stdsearch.aspx?key={0}&idx=1&pcnt=10", keyword);
            GetStandard(url, ref items);
            // 搜索标准名
            url = string.Format("http://10.113.1.69/std/stdsearch.aspx?key={0}&idx=0&pcnt=10", keyword);
            GetStandard(url, ref items);
            AddListItems(items);
            
        }

        private void dgvMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (MouseButtons.Left == e.Button)
            {
                Cursor = Cursors.WaitCursor;
                Text = "玩命下载中...稍安勿躁(*@ο@*)";
                // 获取注册表中的存放路径
                var value = RegistryHelper.ReadValue(Registry.LocalMachine, "Software\\STDLite", "SavedDirectory");
                var directoryPath = Directory.Exists(value) ? value : Environment.CurrentDirectory;
                var fileName = directoryPath + "\\" +
                               lstMain.SelectedItems[0].SubItems[0].Text + " " +
                               (lstMain.SelectedItems[0].SubItems[1].Text).Replace("/", "").Replace(":", "") + ".pdf";
                var uri = lstMain.SelectedItems[0].SubItems[3].Text;
                var ret = DownloadFile(fileName, uri);
                Cursor = Cursors.Default;
                if (ret)
                {
                    Process.Start(fileName);
                }
                ResetFormTitle();
            }
        }

        private void mnuOnTop_Click(object sender, EventArgs e)
        {
            var value = RegistryHelper.ReadValue(Registry.LocalMachine, "Software\\STDLite", "OnTop");
            if ("True" == value)
            {
                RegistryHelper.SetValue(Registry.LocalMachine, "Software\\STDLite", "OnTop", "False");
                TopMost = false;
            }
            else
            {
                RegistryHelper.SetValue(Registry.LocalMachine, "Software\\STDLite", "OnTop", "True");
                TopMost = true;
            }
        }

        private void munCopySTDId_Click(object sender, EventArgs e)
        {
            Copy2Clipboard(lstMain.SelectedItems[0].SubItems[0].Text);
        }

        private void munCopySTDName_Click(object sender, EventArgs e)
        {
            Copy2Clipboard(lstMain.SelectedItems[0].SubItems[1].Text);
        }

        private void munCopySTD_Click(object sender, EventArgs e)
        {
            var stdId = lstMain.SelectedItems[0].SubItems[0].Text;
            var stdName = lstMain.SelectedItems[0].SubItems[1].Text;
            Copy2Clipboard(stdId + stdName);
        }

        private void munSaveAs_Click(object sender, EventArgs e)
        {
            // 注册表中路径不存在则使用程序所在目录
            var value = RegistryHelper.ReadValue(Registry.LocalMachine, "Software\\STDLite", "SavedDirectory");
            var path = Directory.Exists(value) ? value : Environment.CurrentDirectory;
            // 构造文件名
            var pdfName = string.Format("{0} {1}.pdf",
                lstMain.SelectedItems[0].SubItems[0].Text,
                lstMain.SelectedItems[0].SubItems[1].Text).Replace("/", "").Replace(":", "");
            var sfd = new SaveFileDialog
            {
                InitialDirectory = path, // 设置初始目录
                FileName = pdfName // 设置默认保存文件名
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Cursor = Cursors.WaitCursor;
                Text = "玩命下载中...稍安勿躁(*@ο@*)";
                var fileName = sfd.FileName;
                var uri = lstMain.SelectedItems[0].SubItems[3].Text;
                var ret = DownloadFile(fileName, uri);
                if (ret)
                {
                    Process.Start(fileName);
                }
                ResetFormTitle();
                Cursor = Cursors.Default;
                // 存储此次保存的路径
                var savedDirectory = fileName.Substring(0, fileName.LastIndexOf("\\", StringComparison.Ordinal));
                RegistryHelper.SetValue(Registry.LocalMachine, "Software\\STDLite", "SavedDirectory", savedDirectory);
            }
        }

        private void munOpenSavedDirectory_Click(object sender, EventArgs e)
        {
            var value = RegistryHelper.ReadValue(Registry.LocalMachine, "Software\\STDLite", "SavedDirectory");
            Process.Start(Directory.Exists(value) ? value : Environment.CurrentDirectory);
        }

        private void mnuCheckUpdate_Click(object sender, EventArgs e)
        {
            MessageBox.Show("功能开发中....");
        }
    }
}

namespace Common
{
    /// <summary>
    ///     对ListView点击列标题自动排序功能
    /// </summary>
    public class ListViewHelper
    {
        public static void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var lv = sender as ListView;
            // 检查点击的列是不是现在的排序列.
            if (null != lv && e.Column == ((ListViewColumnSorter) lv.ListViewItemSorter).SortColumn)
            {
                // 重新设置此列的排序方法.
                ((ListViewColumnSorter) lv.ListViewItemSorter).Order =
                    ((ListViewColumnSorter) lv.ListViewItemSorter).Order == SortOrder.Ascending
                        ? SortOrder.Descending
                        : SortOrder.Ascending;
            }
            else
            {
                // 设置排序列，默认为正向排序
                if (null != lv)
                {
                    ((ListViewColumnSorter) lv.ListViewItemSorter).SortColumn = e.Column;
                    ((ListViewColumnSorter) lv.ListViewItemSorter).Order = SortOrder.Ascending;
                }
            }
            // 用新的排序方法对ListView排序
            ((ListView) sender).Sort();
        }
    }

    /// <summary>
    ///     继承自IComparer
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        /// <summary>
        ///     声明CaseInsensitiveComparer类对象
        /// </summary>
        private readonly CaseInsensitiveComparer _objectCompare;

        /// <summary>
        ///     构造函数
        /// </summary>
        public ListViewColumnSorter()
        {
            // 默认按第一列排序
            SortColumn = 0;
            // 排序方式为不排序
            Order = SortOrder.None;
            // 初始化CaseInsensitiveComparer类对象
            _objectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        ///     获取或设置按照哪一列排序
        /// </summary>
        public int SortColumn { set; get; }

        /// <summary>
        ///     获取或设置排序方式
        /// </summary>
        public SortOrder Order { set; get; }

        /// <summary>
        ///     重写IComparer接口
        /// </summary>
        /// <param name="ipx">要比较的第一个对象</param>
        /// <param name="ipy">要比较的第二个对象</param>
        /// <returns>比较的结果.如果相等返回0，如果x大于y返回1，如果x小于y返回-1</returns>
        public int Compare(object ipx, object ipy)
        {
            int compareResult;
            // 将比较对象转换为ListViewItem对象
            var listviewX = (ListViewItem) ipx;
            var listviewY = (ListViewItem) ipy;
            var xText = listviewX.SubItems[SortColumn].Text;
            var yText = listviewY.SubItems[SortColumn].Text;
            int xInt, yInt;
            // 比较,如果值为IP地址，则根据IP地址的规则排序。
            if (IsIp(xText) && IsIp(yText))
            {
                compareResult = CompareIp(xText, yText);
            }
            else if (int.TryParse(xText, out xInt) && int.TryParse(yText, out yInt)) //是否全为数字
            {
                // 比较数字
                compareResult = CompareInt(xInt, yInt);
            }
            else
            {
                // 比较对象
                compareResult = _objectCompare.Compare(xText, yText);
            }
            // 根据上面的比较结果返回正确的比较结果
            switch (Order)
            {
                case SortOrder.Ascending:
                    // 因为是正序排序，所以直接返回结果
                    return compareResult;
                case SortOrder.Descending:
                    // 如果是反序排序，所以要取负值再返回
                    return (-compareResult);
            }

            // 如果相等返回0
            return 0;
        }

        /// <summary>
        ///     判断是否为正确的IP地址，IP范围（0.0.0.0～255.255.255）
        /// </summary>
        /// <param name="ip">需验证的IP地址</param>
        /// <returns></returns>
        public bool IsIp(string ip)
        {
            return Regex.Match(ip,
                @"^(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$")
                .Success;
        }

        /// <summary>
        ///     比较两个数字的大小
        /// </summary>
        /// <param name="ipx">要比较的第一个对象</param>
        /// <param name="ipy">要比较的第二个对象</param>
        /// <returns>比较的结果.如果相等返回0，如果x大于y返回1，如果x小于y返回-1</returns>
        public static int CompareInt(int ipx, int ipy)
        {
            if (ipx > ipy)
            {
                return 1;
            }
            if (ipx < ipy)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        ///     比较两个IP地址的大小
        /// </summary>
        /// <param name="ipx">要比较的第一个对象</param>
        /// <param name="ipy">要比较的第二个对象</param>
        /// <returns>比较的结果.如果相等返回0，如果x大于y返回1，如果x小于y返回-1</returns>
        public static int CompareIp(string ipx, string ipy)
        {
            var ipxs = ipx.Split('.');
            var ipys = ipy.Split('.');

            for (var i = 0; i < 4; i++)
            {
                if (Convert.ToInt32(ipxs[i]) > Convert.ToInt32(ipys[i]))
                {
                    return 1;
                }
                if (Convert.ToInt32(ipxs[i]) < Convert.ToInt32(ipys[i]))
                {
                    return -1;
                }
            }

            return 0;
        }
    }

    internal class RegistryHelper
    {
        public static string ReadValue(RegistryKey rootKey, string subkey, string name)
        {
            var value = string.Empty;
            using (var subKey = rootKey.OpenSubKey(subkey, true))
            {
                if (null != subKey)
                {
                    value = subKey.GetValue(name) + "";
                }
            }

            return value;
        }

        public static void SetValue(RegistryKey rootKey, string subkey, string name, string value)
        {
            using (var subKey = rootKey.CreateSubKey(subkey))
            {
                if (null != subKey)
                {
                    subKey.SetValue(name, value);
                }
            }
        }

        public void DeleteKey(RegistryKey rootKey, string subkey, string name)
        {
            using (var subKey = rootKey.OpenSubKey(subkey, true))
            {
                if (null != subKey)
                {
                    var subKeys = subKey.GetSubKeyNames();
                    foreach (var x in subKeys.Where(x => x == name))
                    {
                        subKey.DeleteSubKeyTree(name);
                    }
                }
            }
        }

        public bool IsKeyExist(RegistryKey rootKey, string subkey, string name)
        {
            using (var subKey = rootKey.OpenSubKey(subkey, true))
            {
                if (subKey != null)
                {
                    var subkeyNames = subKey.GetSubKeyNames();
                    if (subkeyNames.Any(keyName => keyName == name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class MultiDownload
    {
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="threahNum">线程数量</param>
        /// <param name="fileUrl">文件Url路径</param>
        /// <param name="savePath">本地保存路径</param>
        public MultiDownload(int threahNum, string fileUrl, string savePath)
        {
            ThreadNum = threahNum;
            _thread = new Thread[threahNum];
            _fileUrl = fileUrl;
            SavePath = savePath;
        }

        public void Start()
        {
            var request = (HttpWebRequest) WebRequest.Create(_fileUrl);
            var response = (HttpWebResponse) request.GetResponse();
            FileSize = response.ContentLength;
            var singelNum = (int) (FileSize/ThreadNum); //平均分配
            var remainder = (int) (FileSize%ThreadNum); //获取剩余的
            request.Abort();
            response.Close();
            for (var i = 0; i < ThreadNum; i++)
            {
                var range = new List<int> {i*singelNum};
                if (remainder != 0 && (ThreadNum - 1) == i) //剩余的交给最后一个线程
                    range.Add(i*singelNum + singelNum + remainder - 1);
                else
                    range.Add(i*singelNum + singelNum - 1);
                //下载指定位置的数据
                int[] ran = {range[0], range[1]};
                _thread[i] = new Thread(Download)
                {
                    Name = Path.GetFileNameWithoutExtension(_fileUrl) +
                           "_{0}".Replace("{0}", Convert.ToString(i + 1))
                };
                _thread[i].Start(ran);
            }
        }

        private void Download(object obj)
        {
            Stream httpFileStream = null, localFileStram = null;
            try
            {
                var ran = obj as int[];
                var tmpFileBlock = Path.GetTempPath() + Thread.CurrentThread.Name + ".tmp";
                _tempFiles.Add(tmpFileBlock);
                var httprequest = (HttpWebRequest) WebRequest.Create(_fileUrl);
                if (ran != null) httprequest.AddRange(ran[0], ran[1]);
                var httpresponse = (HttpWebResponse) httprequest.GetResponse();
                httpFileStream = httpresponse.GetResponseStream();
                localFileStram = new FileStream(tmpFileBlock, FileMode.Create);
                var by = new byte[5000];
                if (httpFileStream != null)
                {
                    var getByteSize = httpFileStream.Read(@by, 0, @by.Length); //Read方法将返回读入by变量中的总字节数
                    while (getByteSize > 0)
                    {
                        Thread.Sleep(20);
                        lock (_locker) _downloadSize += getByteSize;
                        localFileStram.Write(@by, 0, getByteSize);
                        getByteSize = httpFileStream.Read(@by, 0, @by.Length);
                    }
                }
                lock (_locker) _threadCompleteNum++;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (httpFileStream != null) httpFileStream.Dispose();
                if (localFileStram != null) localFileStram.Dispose();
            }
            if (_threadCompleteNum == ThreadNum)
            {
                Complete();
                IsComplete = true;
            }
        }

        /// <summary>
        ///     下载完成后合并文件块
        /// </summary>
        private void Complete()
        {
            Stream mergeFile = new FileStream(SavePath, FileMode.Create);
            var addWriter = new BinaryWriter(mergeFile);
            foreach (var file in _tempFiles)
            {
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    var tempReader = new BinaryReader(fs);
                    addWriter.Write(tempReader.ReadBytes((int) fs.Length));
                    tempReader.Close();
                }
                File.Delete(file);
            }
            addWriter.Close();
        }

        #region 变量

        private readonly string _fileUrl; //文件地址
        private short _threadCompleteNum; //线程完成数量
        private volatile int _downloadSize; //当前下载大小(实时的)
        private readonly Thread[] _thread; //线程数组
        private readonly List<string> _tempFiles = new List<string>();
        private readonly object _locker = new object();

        #endregion

        #region 属性

        /// <summary>
        ///     文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///     文件大小
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        ///     当前下载大小(实时的)
        /// </summary>
        public int DownloadSize
        {
            get { return _downloadSize; }
        }

        /// <summary>
        ///     是否完成
        /// </summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        ///     线程数量
        /// </summary>
        public int ThreadNum { get; private set; }

        /// <summary>
        ///     保存路径
        /// </summary>
        public string SavePath { get; set; }

        #endregion
    }
}