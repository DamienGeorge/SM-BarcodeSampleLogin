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
    /// <summary>
    /// Task for creating entities scanned using Scanner into Table Scanned Entities
    /// TODO - Needs a better task name
    /// </summary>
    [SampleManagerTask("BarcodeScanTask")]
    public class BarcodeScanTask : DefaultFormTask
    {
        #region Global Variables
        private FormBarcodeLogin m_Form;
        private BarcodeScan barcode;
        string _taskName = String.Empty;
        string _taskParameters = String.Empty;
        #endregion

        #region Overrides
        protected override void MainFormCreated()
        {
            base.MainFormCreated();
            m_Form = (FormBarcodeLogin)MainForm;
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            barcode = new BarcodeScan(EntityManager, Library, m_Form.ScanBox);

            m_Form.ScanBox.EditValueChanged += ScanBox_EditValueChanged;

            ReadParameters();
        }
        #endregion

        #region Custom Methods
        /// <summary>
        /// Read all the parameters from the task
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
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

            if (Context.TaskParameters != null && Context.TaskParameters.Length > 1)
            {
                var contextTaskParameters = String.Join(',', Context.TaskParameters);
                _taskParameters += contextTaskParameters.Substring(contextTaskParameters.IndexOf(',')); //skip the Form Parameter 
            }
        }

        /// <summary>
        /// Logic to process Scan box value change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanBox_EditValueChanged(object sender, Thermo.SampleManager.Library.ClientControls.TextChangedEventArgs e)
        {
            barcode.Scan(sender, e);

            if (barcode.scannedValues.Count > 0)
            {
                var scannedValues = barcode.scannedValues;

                CreateScannedEntities(scannedValues);
            }
        }

        /// <summary>
        /// Create the Scanned Entities
        /// </summary>
        /// <param name="scannedValues"></param>
        private void CreateScannedEntities(List<string> scannedValues)
        {
            foreach (var scannedValue in scannedValues)
            {
                //Set scanned text for feedback
                barcode.ScanSuccess(m_Form.StatusText, scannedValue);

                var scannedSample = EntityManager.CreateEntity<ScannedEntityBase>();

                //TODO - Check config value and set the default status
                scannedSample.SetStatus(PhraseUPenStat.PhraseIdSC);
                scannedSample.ScannedText = scannedValue;
                scannedSample.ScannedOn = DateTime.Now;
                scannedSample.ScannedBy = (PersonnelBase)Library.Environment.CurrentUser;
                scannedSample.TaskName = _taskName;
                scannedSample.TaskParameters = _taskParameters;
                //Add Default Group Id to manipulate the scanned data visible to user.
                scannedSample.GroupId = scannedSample.ScannedBy.DefaultGroup;

                EntityManager.Transaction.Add(scannedSample);

            }

            EntityManager.Commit();
            barcode.ResetScanData();
        }
        #endregion
    }
}
