using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Tasks;
using DevExpress.Spreadsheet;
using Thermo.SampleManager.Common.Extensions;
using System.Xml;
using System.Security.AccessControl;
using DevExpress.Spreadsheet;

namespace Customization.Tasks.Testing
{
    [SampleManagerTask(nameof(SpreadSheetToXMLTask))]
    public class SpreadSheetToXMLTask : DefaultFormTask
    {
        FormSpreadSheetForm m_Form;
        protected override void MainFormCreated()
        {
            base.MainFormCreated();

            m_Form = (FormSpreadSheetForm)MainForm;

            var bytes = GetXMLFile();
            OpenFileasSpreadSheet(bytes);

            //m_Form.Saved += _Form_Saved;
            m_Form.SaveButton.Click += SaveButton_OnClick;
        }

        private void SaveButton_OnClick(object sender, EventArgs e)
        {
            var excelContent = m_Form.SpreadSheetArea.SpreadsheetDocument;

            Workbook workbook = new Workbook();
            workbook.LoadDocument(excelContent);

            var excelAsXml = workbook.SaveDocument(DocumentFormat.XmlSpreadsheet2003);
            File.WriteAllBytes("C:\\Users\\Dan.G\\OneDrive - Zifo RnD Solutions\\Documents\\Rohan\\test.xml", excelAsXml);
        }

        protected override void MainFormLoaded()
        {

        }

        private void OpenFileasSpreadSheet(byte[] bytes)
        {
            m_Form.SpreadSheetArea.ImportCompatibleDocument(bytes);
        }

        private byte[] GetXMLFile()
        {
            string fileName = "";
            var clientFile = Library.Utils.PromptForFile(string.Empty, "*|*.*");

            if (clientFile.Is_Not_NullWhitespaceOrEmpty())
            {
                fileName = Path.GetFileName(clientFile);
                var tempFile = Library.File.TransferToServerTemp(clientFile);
                var bytes = File.ReadAllBytes(tempFile.FullName);
                tempFile.Delete();
                return bytes;
            }

            return default(byte[]);
        }
    }
}
