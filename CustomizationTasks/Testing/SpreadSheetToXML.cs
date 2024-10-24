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

            var fileName = GetXMLFile();
            OpenFileasSpreadSheet(fileName);

            _Form.Saved += _Form_Saved; ;
        }

        private void _Form_Saved(object sender, SavedEventArgs e)
        {
            Workbook workbook = new Workbook();
            var result = workbook.LoadDocument(_Form.SpreadSheetArea.SpreadsheetDocument);
        }

        private void OpenFileasSpreadSheet(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);

            _Form.SpreadSheetArea.SpreadsheetDocument = bytes;
        }

        private string GetXMLFile()
        {
            return Library.Utils.PromptForFile("Choose XMl file", ".xml");
        }
    }
}
