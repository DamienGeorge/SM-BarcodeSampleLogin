using System;
using System.Collections.Generic;
using System.Timers;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.FormDefinition;
using Thermo.SampleManager.Tasks;
using Thermo.SampleManager.Server;
using System.Drawing;
using Thermo.SampleManager.Library.EntityDefinition;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Thermo.SampleManager.Common.Data;
using System.Linq;
using Thermo.Framework.Core;
using Thermo.SampleManager.Server.Workflow;
using System.Text;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Common.Extensions;

namespace Customization.Tasks
{
    /// <summary>
    /// Backing Task for ServiceMonitor Form
    /// Checks if WCF, Timerqueue and Sweepers are running by testing the service itself.
    /// </summary>

    [SampleManagerTask(nameof(ServiceMonitorFormTask))]
    public class ServiceMonitorFormTask : DefaultFormTask
    {
        private const string CheckLogMessage = " Please check the logs and restart if necessary.";
        private const string ErrorMessage = "One or more SampleManager Services require your attention";
        FormServiceMonitor m_Form;
        private IVglGlobalService m_VglGlobalService;

        #region Colors
        //hardcoded colors for the form
        private Color MainLabelColor = Color.FromArgb(250, 237, 205);
        private Color TimerqueueLabelColor = Color.FromArgb(233, 237, 201);
        private Color BackgroundColor = Color.FromArgb(0, 221, 184, 146);

        //Set via config
        private Color failColor = Color.DarkSalmon;
        private Color passColor;
        private int queueLength;
        private string mailReportConfig;
        #endregion

        #region Settings
        private string HeaderCaption = string.Empty;
        private TimeSpan wdtInterval;

        private TimeSpan wcfInterval;
        private List<WCFDetail> currentWcfDetails;
        private bool IsTimerqueueRunning;
        private Workflow mailWorkflow;
        private TimeSpan mailFrequency;

        public string[] WCFurls { get; private set; }
        public bool isWCFRunning { get; private set; }
        #endregion

        #region variables
        //ServiceController[] scServices;
        //scServices = ServiceController.GetServices();
        #endregion

        /// <summary>
        /// Overrides the MainFormCreated method
        /// </summary>
        protected override void MainFormCreated()
        {
            base.MainFormCreated();
            m_Form = (FormServiceMonitor)MainForm;
            m_VglGlobalService = (IVglGlobalService)Library.GetService(typeof(IVglGlobalService));
        }

        /// <summary>
        /// Overrides the MainFormLoaded method
        /// </summary>
        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            Startup();
        }

        /// <summary>
        /// Performs startup activities
        /// </summary>
        private void Startup()
        {
            TimeSpan formRefreshInterval;
            string wcfConfig;

            FetchConfigItems(out formRefreshInterval, out wcfConfig);

            //Split the WCF url separated by semi colon
            WCFurls = wcfConfig.Contains(',') ? wcfConfig.Split(',') : [wcfConfig];

            //Start the timer
            Timer timer = new Timer();
            if (formRefreshInterval.Ticks > 0)
            {
                timer.Interval = formRefreshInterval.TotalMilliseconds;
            }

            //Store the header caption to update on timer elapsed
            HeaderCaption = m_Form.MainLabel.Caption;

            //Setup the form with colors
            SetColors();

            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            UpdateForm();
            CheckRunErrorWorkflow();
        }

        /// <summary>
        /// Fetches the config items for the task
        /// </summary>
        /// <param name="formRefreshInterval"></param>
        /// <param name="wcfConfig"></param>
        private void FetchConfigItems(out TimeSpan formRefreshInterval, out string wcfConfig)
        {
            formRefreshInterval = m_VglGlobalService.GetGlobalInterval("MONITOR_REFRESH_INTERVAL");
            wcfConfig = m_VglGlobalService.GetGlobalString("MONITOR_WCF_URL");
            wdtInterval = m_VglGlobalService.GetGlobalInterval("MONITOR_WDT_FAIL_INTERVAL");
            wcfInterval = m_VglGlobalService.GetGlobalInterval("MONITOR_WCF_FAIL_INTERVAL");
            queueLength = m_VglGlobalService.GetGlobalInt("MONITOR_WDT_FAIL_QUEUE_LENGTH");

            mailReportConfig = Library.Environment.GetGlobalString("MONITOR_WORKFLOW");
            mailWorkflow = EntityManager.SelectLatestVersion<Workflow>(new Identity(mailReportConfig));
            mailFrequency = m_VglGlobalService.GetGlobalInterval("MONITOR_MAIL_FREQUENCY");
        }

        /// <summary>
        /// Update the Form components
        /// </summary>
        private void UpdateForm()
        {
            FormatSweeperGrid();
            UpdateWCFGrid();
            UpdateTimerQueueCaption();
            SetHeader();
        }

        /// <summary>
        /// Set Caption on the Form
        /// </summary>
        private void SetHeader()
        {
            m_Form.MainLabel.Caption = $"{HeaderCaption} {DateTime.Now}";
        }

        /// <summary>
        /// Better looking colors for the form
        /// </summary>
        private void SetColors()
        {
            m_Form.MainLabel.BackColor = MainLabelColor;
            m_Form.TimerqueueLabel.BackColor = TimerqueueLabelColor;
        }

        /// <summary>
        /// Method to handle form refresh on timer interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateForm();

            CheckRunErrorWorkflow();
        }

        /// <summary>
        /// Run Error Workflow if one of the services is down
        /// </summary>
        private void CheckRunErrorWorkflow()
        {
            if (mailWorkflow != null)
            {

                StringBuilder mailContent = GetMailContent(out string errorPhrase);

                var entry = GetLatestServiceLogEntry();

                //TODO - move to workflow
                if (entry is not null)
                {
                    if (entry.MailContent == mailContent.ToString() && (DateTime.Now - entry.SentOn.Value) > mailFrequency)
                    {
                        WorkflowPropertyBag propertyBag = new WorkflowPropertyBag
                    {
                        { "$mailSubject", ErrorMessage},
                        { "$mailMessage", mailContent.ToString() }
                    };

                        Library.Workflow.Perform(mailWorkflow, propertyBag);

                        var errorMessage = string.Empty;
                        if (propertyBag.HasErrors)
                        {
                            foreach (WorkflowError error in propertyBag.Errors)
                            {
                                errorMessage += error.Message;
                            }
                        }
                        CreateMonitorLogEntry(mailContent.ToString(), errorMessage, errorPhrase);
                    }
                }
            }
        }

        private UServiceMonitorLogBase GetLatestServiceLogEntry()
        {
            IQuery query = EntityManager.CreateQuery<UServiceMonitorLogBase>();

            var serviceLogRecord = EntityManager.Select(query).ActiveItems.Cast<UServiceMonitorLogBase>().OrderByDescending(x => x.SentOn).FirstOrDefault();

            return serviceLogRecord;

        }

        private StringBuilder GetMailContent(out string errorPhrase)
        {
            errorPhrase = PhraseErrorType.PhraseIdNA;

            StringBuilder mailContent = new StringBuilder();

            if (currentWcfDetails.Any(x => x.IsResponsive == false || (DateTime.Now - x.LastResponseTime.Value) > wcfInterval))
            {
                foreach (var entry in currentWcfDetails.Where(x => x.IsResponsive == false || (DateTime.Now - x.LastResponseTime.Value) > wcfInterval))
                {
                    mailContent.Append($"WCF service {entry.url} is not responding since {entry.LastResponseTime}.");
                }

                errorPhrase = PhraseErrorType.PhraseIdWCF;
            }

            if (IsTimerqueueRunning == false)
            {
                mailContent.AppendLine(EntityManager.SelectPhrase(PhraseErrorType.Identity, PhraseErrorType.PhraseIdTQ)?.Description);
                errorPhrase = errorPhrase == PhraseErrorType.PhraseIdWCF ? PhraseErrorType.PhraseIdBOTH : PhraseErrorType.PhraseIdTQ;
            }

            if (mailContent.Length > 0)
            {
                mailContent.Append(CheckLogMessage);
            }
            return mailContent;
        }

        /// <summary>
        /// Create a Log entry for each time the workflow is triggered
        /// </summary>
        /// <param name="mailContent"></param>
        /// <param name="errorMessage"></param>
        private void CreateMonitorLogEntry(string mailContent, string errorMessage, string errorPhrase)
        {
            var logEntry = EntityManager.CreateEntity(TableNames.UServiceMonitorLog) as UServiceMonitorLogBase;
            logEntry.MailContent = mailContent;
            logEntry.DeliveryErrors = errorMessage;
            logEntry.SentOn = DateTime.Now;
            logEntry.ErrorType = EntityManager.SelectPhrase(PhraseErrorType.Identity, errorPhrase).ToString();
            logEntry.Status = mailContent.Is_Not_NullWhitespaceOrEmpty();


            EntityManager.Transaction.Add(logEntry);
            EntityManager.Commit();
        }

        /// <summary>
        /// Update the WCF Unbound Grid
        /// </summary>
        private void UpdateWCFGrid()
        {
            currentWcfDetails = CheckWCF();

            m_Form.WCFUnboundGrid.BeginUpdate();
            m_Form.WCFUnboundGrid.ClearRows();

            foreach (var wcf in currentWcfDetails)
            {
                var row = m_Form.WCFUnboundGrid.AddRow(wcf.url, wcf.LastCheckIn, wcf.IsResponsive, wcf.LastResponseTime);

                if (wcf.IsResponsive == false)
                {
                    row.SetBackgroundColor(failColor);
                }
            }
            m_Form.WCFUnboundGrid.EndUpdate();
        }

        /// <summary>
        /// Check all the WCFs defined in the configuration
        /// </summary>
        /// <returns></returns>
        private List<WCFDetail> CheckWCF()
        {
            List<WCFDetail> wCFDetails = new List<WCFDetail>();

            foreach (var wcfUrl in WCFurls)
            {
                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, wcfUrl + "/healthcheck");

                        using (StreamReader streamReader = new StreamReader(httpClient.Send(request).Content.ReadAsStream()))
                        {
                            string end = streamReader.ReadToEnd();
                            var wcfDetail = new WCFDetail(wcfUrl, DateTime.Now, false);

                            if (string.IsNullOrEmpty(end))
                            {
                                Logger.Error("WCF Rest Request: Server returned no data");
                            }

                            if (Regex.IsMatch("Connected" + System.Environment.NewLine + end, "overallStatus.+Ok.+\n", RegexOptions.IgnoreCase))
                            {
                                wcfDetail.LastResponseTime = DateTime.Now;
                                wcfDetail.IsResponsive = true;
                                Logger.Error("WCF Status Good");
                            }
                            else
                            {
                                string message = "WCF response indicated there was an issue. This indicates there is an issue with the WCF service";
                            }

                            wCFDetails.Add(wcfDetail);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    wCFDetails.Add(new WCFDetail(wcfUrl, DateTime.Now, false)); ;
                }
            }
            return wCFDetails;
        }

        /// <summary>
        /// Apply formatting to the Sweeper Grid
        /// </summary>
        private void FormatSweeperGrid()
        {
            var entries = m_Form.SweeperDataGrid.GridData;
            foreach (SweeperMonitorBase entry in entries)
            {
                if (entry.TimeSinceLastResponse > entry.UnresponsiveFrequency)
                {
                    var row = m_Form.SweeperDataGrid.GetRowByEntity(entry);

                    foreach (var column in m_Form.SweeperDataGrid.Columns)
                    {
                        m_Form.SweeperDataGrid.SetCellBackgroundColor(row, column.Name, failColor);
                    }
                }
            }
        }

        /// <summary>
        /// Set the timerqueue Caption
        /// </summary>
        private void UpdateTimerQueueCaption()
        {
            IQuery tqMonitorQuery = EntityManager.CreateQuery(TableNames.TimerqueueMonitor);
            IEntityCollection entityCollection = EntityManager.Select(tqMonitorQuery);

            var query = entityCollection.ActiveItems
                .Cast<TimerqueueMonitorBase>()
                .Select(x => x)
                .FirstOrDefault();

            var runTime = query.RunTime;
            var pendingTasks = query.PendingTasks;

            var message = $"Timerqueue Service : \r\n {pendingTasks} tasks pending before next Canary Task run. " +
                $"\n {query.SuspendedTasks} suspended timerqueue tasks out of {query.TotalTasks} total tasks" +
                $"\r\n Next canary run time at {runTime.ToSampleManagerString(EntityManager)}";

            m_Form.TimerqueueLabel.Caption = message;

            if ((DateTime.Now - runTime.Value) > wdtInterval && pendingTasks < queueLength)
            {
                m_Form.TimerqueueLabel.BackColor = failColor;
                IsTimerqueueRunning = false;
            }
            else
            {
                IsTimerqueueRunning = true;
                m_Form.TimerqueueLabel.BackColor = TimerqueueLabelColor;
            }
        }
    }
}
