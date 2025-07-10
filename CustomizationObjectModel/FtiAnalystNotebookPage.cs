using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.XtraPrinting;
using DevExpress.XtraRichEdit;
using DevExpress.Spreadsheet;
using System;
using System.IO;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Exceptions;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.EntityDefinition;

namespace Thermo.SampleManager.ObjectModel
{
    /// <summary>
    /// Defines extended business logic and manages access to the HAZARD entity.
    /// </summary> 
    [SampleManagerEntity(EntityName)]
    public class FtiAnalystNotebookPage : AnalystNotebookPage
    {
        [PromptByteArray]
        [ExplorerIgnore]
        public byte[] FtiPageAsPdfReport
        {
            get
            {
                base.EntityManager.SetEntityCacheAsOutOfDate();
                PageData = null;
                switch (PageType.PhraseId)
                {
                    case "CHAPTER":
                    case "DOC":
                        return FtiGetRichtTextAsPdf(PageData);
                    case "SHEET":
                        return FtiGetSpreadSheetDocumentAsPdf(PageData);
                    case "PDF":
                        return PageData;
                    default:
                        return PageAsPdfReport;
                }
            }
        }
        public byte[] FtiGetRichtTextAsPdf(byte[] openXmlBytes)
        {
            try
            {
                using RichEditDocumentServer richEditDocumentServer = new RichEditDocumentServer();
                richEditDocumentServer.OpenXmlBytes = openXmlBytes;

                foreach (var section in richEditDocumentServer.Document.Sections)
                {
                    section.Margins.Left = 26;
                    section.Margins.Right = 26;
                    section.Margins.Top = 0;
                    section.Margins.Bottom = 0;

                    section.Page.Width = Library.Environment.GetGlobalInt("FTI_CERT_NOTEBOOK_PAGE_WIDTH");
                    section.Page.Height = Library.Environment.GetGlobalInt("FTI_CERT_NOTEBOOK_PAGE_HEIGHT");
                }


                string text = Path.GetTempFileName() + ".pdf";

                richEditDocumentServer.ExportToPdf(text, new PdfExportOptions());
                richEditDocumentServer.Dispose();

                byte[] result = File.ReadAllBytes(text);

                File.Delete(text);

                return result;
            }
            catch (Exception innerException)
            {
                throw new SampleManagerError("Error converting to PDF", innerException);
            }
        }

        public byte[] FtiGetSpreadSheetDocumentAsPdf(byte[] openXmlBytes)
        {
            try
            {
                using DevExpress.Spreadsheet.Workbook workbook = new DevExpress.Spreadsheet.Workbook();
                workbook.LoadDocument(openXmlBytes);

                foreach (var worksheet in workbook.Worksheets)
                {
                    worksheet.ActiveView.Margins.Left = 26;
                    worksheet.ActiveView.Margins.Right = 26;
                    worksheet.ActiveView.Margins.Top = 0;
                    worksheet.ActiveView.Margins.Bottom = 0;

                    worksheet.ActiveView.SetCustomPaperSize(Library.Environment.GetGlobalInt("FTI_CERT_NOTEBOOK_PAGE_WIDTH"), Library.Environment.GetGlobalInt("FTI_CERT_NOTEBOOK_PAGE_HEIGHT"));
                }

                string text = Path.GetTempFileName() + ".pdf";

                workbook.ExportToPdf(text, new PdfExportOptions());
                workbook.Dispose();

                byte[] result = File.ReadAllBytes(text);

                File.Delete(text);

                return result;
            }
            catch (Exception innerException)
            {
                throw new SampleManagerError("Error converting to PDF", innerException);
            }
        }
    }
}
