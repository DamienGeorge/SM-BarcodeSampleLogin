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
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Server.Workflow;
using System.Linq;

namespace Thermo.SampleManager.ObjectModel
{
	/// <summary>
	/// Defines extended business logic and manages access to the HAZARD entity.
	/// </summary> 
	[SampleManagerEntity(EntityName)]
	public class FtiCert : Cert
	{
        protected override void UpdateOrigin()
        {
            var bag = TriggerEvent("FTI_POST_CREATE");

            if (!bag.HasErrors)
            {
                base.UpdateOrigin();
                return;
            }

            WorkflowError error = bag.Errors.First();

            throw new SampleManagerError(error.Title, error.Message, error);
        }
    }
}
