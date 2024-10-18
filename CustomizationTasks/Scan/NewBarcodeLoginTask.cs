using System;
using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.Tasks;

namespace Customization.Tasks
{
    [SampleManagerTask("NewBarcodeLoginTask")]
    public class NewBarcodeLoginTask : DefaultFormTask
    {
        private FormBarcodeLogin m_Form;
        private BarcodeLogin barcode;
        string _taskName = String.Empty;
        string _taskParameters = String.Empty;

        protected override void MainFormCreated()
        {
            base.MainFormCreated();
            m_Form = (FormBarcodeLogin)MainForm;
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            barcode = new BarcodeLogin(EntityManager, Library, m_Form.ScanBox);

            m_Form.ScanBox.EditValueChanged += ScanBox_EditValueChanged;

            ReadParameters();
        }

        private void ReadParameters()
        {
            var menuparams = Context.MenuItem.Get(MasterMenuPropertyNames.Parameters)?.ToString();

            if (menuparams is null)
            {
                throw new ArgumentNullException("Please Ensure that the following parameters are provided : TaskName, Task Parameters");
            }

            var parameters = menuparams.Split(',');



            if (parameters.Length > 1)
            {
                _taskParameters = menuparams.Substring(menuparams.IndexOf(',') + 1);
                _taskName = parameters[0];
            }
            else
            {
                throw new ArgumentException("Expected at least 1 parameter");
            }
        }

        #region Custom Methods


        private void ScanBox_EditValueChanged(object sender, Thermo.SampleManager.Library.ClientControls.TextChangedEventArgs e)
        {
            barcode.Scan(sender, e);

            if (barcode.scannedValues.Count > 0)
            {
                var scannedValues = barcode.scannedValues;

                CreateScannedSamples(scannedValues);
            }
        }

        private void CreateScannedSamples(List<string> scannedValues)
        {
            foreach (var scannedValue in scannedValues)
            {

                var scannedSample = EntityManager.CreateEntity<ScannedEntityBase>();

                //TODO - Check config value and set the default status
                scannedSample.SetStatus(PhraseUPenStat.PhraseIdSC);
                scannedSample.ScannedText = scannedValue;
                scannedSample.ScannedOn = DateTime.Now;
                scannedSample.ScannedBy = (PersonnelBase)Library.Environment.CurrentUser;
                scannedSample.TaskName = _taskName;
                scannedSample.TaskParameters = _taskParameters;

                EntityManager.Transaction.Add(scannedSample);

            }

            EntityManager.Commit();
            barcode.ResetScanData();
        }
        #endregion
    }
}
