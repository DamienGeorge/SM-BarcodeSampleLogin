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

namespace Customization.Tasks
{
    /// <summary>
    /// Backing Task for ServiceMonitor Form
    /// Checks if WCF, Timerqueue and Sweepers are running by testing the service itself.
    /// </summary>

    [SampleManagerTask(nameof(ServiceMonitorFormTask))]
    public class ServiceMonitorFormTask : DefaultFormTask
    {
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
                if (IsTimerqueueRunning == false || currentWcfDetails.Any(x => x.IsResponsive == false))
                {
                    WorkflowPropertyBag propertyBag = new WorkflowPropertyBag();

                    StringBuilder mailContent = new StringBuilder();

                    if (IsTimerqueueRunning)
                    {
                        mailContent.AppendLine("Timerqueue service is running normally");
                    }
                    else
                    {
                        mailContent.AppendLine($"Timerqueue is not running as expected. Please check the logs and restart if necessary.");
                    }

                    foreach (var entry in currentWcfDetails.Where(x => x.IsResponsive == false))
                    {
                        mailContent.Append($"WCF service {entry.url} is not responding. Please check the logs and restart if necessary.");
                    }

                    propertyBag.Add("$mailSubject", "Errors Occured in one or more of the Services");
                    propertyBag.Add("$mailMessage", mailContent.ToString());

                    Library.Workflow.Perform(mailWorkflow, propertyBag);

                    var errorMessage = string.Empty;
                    if (propertyBag.HasErrors)
                    {
                        foreach (WorkflowError error in propertyBag.Errors)
                        {
                            errorMessage += error.Message;
                        }
                    }

                    CreateMonitorLogEntry(mailContent.ToString(), errorMessage);
                }
            }
        }

        /// <summary>
        /// Create a Log entry for each time the workflow is triggered
        /// </summary>
        /// <param name="mailContent"></param>
        /// <param name="errorMessage"></param>
        private void CreateMonitorLogEntry(string mailContent, string errorMessage)
        {
            var logEntry = EntityManager.CreateEntity(TableNames.UServiceMonitorLog) as UServiceMonitorLogBase;
            logEntry.MailContent = mailContent;
            logEntry.DeliveryErrors = errorMessage;
            logEntry.SentOn = DateTime.Now;

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
                var row = m_Form.WCFUnboundGrid.AddRow(wcf.url, wcf.LastCheckIn, wcf.IsResponsive);

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

                            if (string.IsNullOrEmpty(end))
                            {
                                Logger.Error("WCF Rest Request: Server returned no data");
                            }

                            if (Regex.IsMatch("Connected" + System.Environment.NewLine + end, "overallStatus.+Ok.+\n", RegexOptions.IgnoreCase))
                            {
                                wCFDetails.Add(new WCFDetail(wcfUrl, DateTime.Now, true));
                                Logger.Error("WCF Status Good");
                            }
                            else
                            {
                                string message = "WCF response indicated there was an issue. This indicates there is an issue with the WCF service";
                                wCFDetails.Add(new WCFDetail(wcfUrl, DateTime.Now, false));
                            }
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

            var message = $"Timerqueue Service : \n {pendingTasks} active tasks pending. \n Last run time at {runTime}";
            m_Form.TimerqueueLabel.Caption = message;

            if ((DateTime.Now - runTime.Value) > wdtInterval && pendingTasks < queueLength)
            {
                m_Form.TimerqueueLabel.ForeColor = failColor;
                IsTimerqueueRunning = false;
            }
            else
            {
                IsTimerqueueRunning = true;
            }
        }
    }
}
