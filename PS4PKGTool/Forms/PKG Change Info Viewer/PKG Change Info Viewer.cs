using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using PS4PKGTool.Utilities.PS4PKGToolHelper;

namespace PS4PKGTool
{
    public partial class PKGChangeInfoViewer : DarkUI.Forms.DarkForm
    {
        static string xmlContent;
        public PKGChangeInfoViewer(string txt)
        {
            InitializeComponent();
            xmlContent = txt;
            parseXml();
        }

        private void parseXml()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("App Version");
            dt.Columns.Add("Change Info");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            XmlElement root = doc.DocumentElement;
            var nodes = doc.SelectSingleNode("//changeinfo"); // You can also use XPath here

            foreach (XmlNode node in nodes)
            {
                var cdata = node.FirstChild.InnerText;

                dt.Rows.Add(node.Attributes["app_ver"].Value, cdata);
            }

            darkDataGridView1.DataSource = dt;
            darkDataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            darkDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            darkDataGridView1.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            darkDataGridView1.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            darkDataGridView1.ScrollBars = ScrollBars.Both;

            foreach (DataGridViewColumn col in darkDataGridView1.Columns)
            {
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
    }
}
