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

namespace Customization.Tasks.Testing
{
    [SampleManagerTask(nameof(SpreadSheetToXMLTask))]
    public class SpreadSheetToXMLTask : DefaultFormTask
    {
        FormSpreadSheetForm _Form;
        protected override void SetupTask()
        {
            base.SetupTask();

            _Form = (FormSpreadSheetForm)MainForm;

            var bytes = GetXMLFile();
            OpenFileasSpreadSheet(bytes);

            _Form.Saved += _Form_Saved; ;
        }

        private void _Form_Saved(object sender, SavedEventArgs e)
        {
            //Workbook workbook = new Workbook();
            //var result = workbook.LoadDocument(_Form.SpreadSheetArea.SpreadsheetDocument);

            var text = _Form.SpreadSheetArea.PlainText;
            File.WriteAllText("C:\\Users\\Dan.G\\OneDrive - Zifo RnD Solutions\\Documents\\Rohan\\test.xml", text);
        }

        private void OpenFileasSpreadSheet(byte[] bytes)
        {
            _Form.SpreadSheetArea.ImportCompatibleDocument(bytes);
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
