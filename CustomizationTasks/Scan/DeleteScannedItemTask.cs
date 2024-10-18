using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Library.DesignerRuntime;

namespace Customization.Tasks
{
    [SampleManagerTask("DeleteScannedItemTask")]
    public class DeleteScannedItemTask : SampleManagerTask
    {
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
    }
}
