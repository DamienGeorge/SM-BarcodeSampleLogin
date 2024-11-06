using System;
using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.DesignerRuntime;
using System.Timers;
using Environment = System.Environment;
using System.Drawing;

namespace Customization.Tasks
{
    /// <summary>
    /// Utility class containing Helper methods for Barcode Processing
    /// </summary>
    public class BarcodeScan
    {
        public IEntityManager EntityManager { get; }
        public StandardLibrary StandardLibrary { get; }
        public TextEdit ScanBox { get; }
        public List<string> scannedValues { get; private set; }
        public bool isScanned { get; private set; } = false;

        //For Barcode Scanner
        private readonly Timer m_TextChangedTimer = new Timer();
        private TextChangedEventArgs m_TextChangedEventData;
        private string m_Title;

        public BarcodeScan(IEntityManager entityManager, StandardLibrary standardLibrary, TextEdit scanBox)
        {
            EntityManager = entityManager;
            StandardLibrary = standardLibrary;
            ScanBox = scanBox;

            // Delay processing the scan box
            m_TextChangedTimer.Elapsed += KeypressedTimerTick;
            m_TextChangedTimer.Interval = 200;

            scannedValues = new List<string>();
        }

        ///// <summary>
        ///// Handles the EditValueChanged event of the ScanBox control.
        ///// </summary>
        public void Scan(object sender, TextChangedEventArgs e) //, TextEdit scanBox
        {

            lock (m_TextChangedTimer)
            {
                m_TextChangedEventData = e;

                if (m_TextChangedTimer.Enabled)
                {
                    // Don't do it now, it will happen when the timer ticks.
                }
                else
                {
                    // Do it now, but start the timer, so the next one doesn't.
                    TextChanged();
                }

                m_TextChangedTimer.Stop();
                m_TextChangedTimer.Start();
            }
        }


        #region Scanning

        /// <summary>
        /// Keypresseds the timer tick.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void KeypressedTimerTick(object sender, EventArgs e)
        {
            lock (m_TextChangedTimer)
            {
                m_TextChangedTimer.Stop();
                TextChanged();
            }
        }

        /// <summary>
        /// Delayed Text Changed Processing
        /// </summary>
        private void TextChanged()
        {
            if (m_TextChangedEventData == null) return;
            var e = m_TextChangedEventData;
            m_TextChangedEventData = null;

            ScanBox.Focus();

            // Process the Text Changed Event as normal

            if (e == null || string.IsNullOrEmpty(e.Text)) return;
            ResetScanData();
            if (e.Text.EndsWith(Environment.NewLine))
            {
                var scanItems = e.Text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                ScanBox.Text = String.Empty;

                foreach (var scanText in scanItems)
                {
                    if (string.IsNullOrEmpty(scanText)) continue;
                    //if (ProcessScanCommand(scanText)) continue;
                    ScanValidate(scanText);
                }

                isScanned = scannedValues.Count > 0 ? true : false;

            }
        }

        public void ResetScanData()
        {
            if (scannedValues.Count > 0)
            {
                scannedValues.Clear();
            }
        }

        /// <summary>
        /// Processes the scan command.
        /// </summary>
        /// <param name="scanText">The scan text.</param>
        /// <returns></returns>
        private bool ProcessScanCommand(string scanText)
        {
            //switch (scanText)
            //{
            //    case CommandApply:
            //        ApplyChanges();
            //        return true;
            //    case CommandClear:
            //        ClearRows();
            //        return true;
            //    case CommandRemove:
            //        RemoveRow();
            //        return true;
            //    default:
            //        return false;
            //}
            return false;
        }

        /// <summary>
        /// Scans the validate.
        /// </summary>
        /// <param name="scanText">The scan text.</param>
        protected virtual bool ScanValidate(string scanText)
        {
            //TODO -What to validate? entry exists already?
            //TODO - Scan to text can possibly be an entity?
            scannedValues.Add(scanText);

            return true;

        }

        private void AddRow(string scanText)
        {
            //Do nothing for now
        }

        /// <summary>
        /// Method to set label caption and color for succesful scan
        /// </summary>
        /// <param name="label"></param>
        /// <param name="scanText"></param>
        public void ScanSuccess(Label label, string scanText)
        {
            label.Caption = "Successfully Scanned : " + scanText;
            label.BackColor = Color.SeaGreen;
        }

        /// <summary>
        /// Method to set label caption and color for unsuccesful scan
        /// </summary>
        /// <param name="label"></param>
        /// <param name="scanText"></param>
        public void ScanFailure(Label label, string scanText, string errorMessage = "")
        {
            label.Caption = "Cannot scan :" + scanText + "." + errorMessage;
            label.BackColor = Color.Red;
        }
        #endregion


    }
}
