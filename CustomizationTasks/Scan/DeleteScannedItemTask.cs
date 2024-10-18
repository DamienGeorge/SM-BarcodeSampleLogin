using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;

namespace Customization.Tasks
{
    /// <summary>
    /// Task to Delete incorrect records from Table ScannedEntity
    /// </summary>
    [SampleManagerTask("DeleteScannedItemTask")]
    public class DeleteScannedItemTask : SampleManagerTask
    {
        #region Overrides
        protected override void SetupTask()
        {
            IEntityCollection entitiesToDelete = Context.SelectedItems;

            foreach (IEntity entity in entitiesToDelete)
            {
                EntityManager.Delete(entity);
                EntityManager.Transaction.Add(entitiesToDelete);
            }

            EntityManager.Commit();
        }
        #endregion
    }
}
