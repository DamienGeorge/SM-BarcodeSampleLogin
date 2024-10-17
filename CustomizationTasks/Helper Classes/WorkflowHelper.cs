using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server.Workflow;

namespace Customization.Tasks
{
    public class WorkflowHelper
    {
        public StandardLibrary Library { get; }

        public WorkflowHelper(StandardLibrary library)
        {
            Library = library;
        }
        #region Workflow

        /// <summary>
        /// Runs the workflow for entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="selectedWorkflow">The selected workflow.</param>
        /// <param name="count">The count.</param>
        /// <returns>List of newly created entities.</returns>
        internal IList<IEntity> RunWorkflow(Workflow selectedWorkflow, int count)
        {
            var newEntities = new List<IEntity>();

            for (var j = 0; j < count; j++)
            {
                // Run the workflow with a property bag for results - used passed in Parameters if available.

                var propertyBag = selectedWorkflow.Properties ?? new WorkflowPropertyBag();

                // Counters

                propertyBag.Set("$WORKFLOW_MAX", count);
                propertyBag.Set("$WORKFLOW_COUNT", j + 1);

                // Perform

                PerformWorkflow(selectedWorkflow, propertyBag);

                // Keep track of the newly created entities.

                var entities = propertyBag.GetEntities(selectedWorkflow.TableName);

                newEntities.AddRange(entities);
            }

            return newEntities;
        }

        private bool PerformWorkflow(Workflow workflow, IWorkflowPropertyBag propertyBag)
        {
            if (propertyBag == null)
            {
                // Perform the workflow & validate its output

                propertyBag = workflow.Perform();
            }
            else
            {
                workflow.Perform(propertyBag);
            }

            // Make sure the workflow generated something

            if (propertyBag.Count == 0)
            {
                // Un-supported entity type

                string message = Library.Message.GetMessage("GeneralMessages", "EmptyWorkflowOutput");
                Library.Utils.FlashMessage(message, "Alert!", MessageButtons.OK, MessageIcon.Error, MessageDefaultButton.Button1);

                return false;
            }

            // Exit if there are errors

            if (propertyBag.Errors.Count > 0)
            {
                Library.Utils.FlashMessage(propertyBag.Errors[0].Message, "Alert!", MessageButtons.OK, MessageIcon.Error, MessageDefaultButton.Button1);
                return false;
            }

            return true;
        }


        #endregion
    }
}
