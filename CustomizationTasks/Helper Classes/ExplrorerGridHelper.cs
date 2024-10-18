using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library.ClientControls.Browse;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.DesignerRuntime;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.Core.Definition;
using Thermo.Framework.Core;
using System.Windows.Controls;

namespace Customization.Tasks.Helper_Classes
{
    public class ExplorerGridHelper
    {
        private readonly IEntityManager _EntityManager;
        private readonly StandardLibrary _Library;
        private readonly UnboundGrid m_UnboundGrid;
        private readonly BrowseFactory _BrowseFactory;

        public ExplorerGridHelper(IEntityManager entityManager, StandardLibrary library, UnboundGrid unboundGrid, BrowseFactory browseFactory)
        {
            _EntityManager = entityManager;
            _Library = library;
            m_UnboundGrid = unboundGrid;
            _BrowseFactory = browseFactory;
        }

        public void PopulateColumns(IEntityCollection entities)
        {
            BuildColumns(entities);

            BuildRows(entities);

        }

        private void BuildRows(IEntityCollection entities)
        {
            foreach (IEntity entity in entities)
            {
                EntityTemplateInternal template = GetTemplate(entity);
                if (template == null) continue;

                // Add a row to the grid

                UnboundGridRow newRow = m_UnboundGrid.AddRow();
                newRow.Tag = entity;

                // Set the row icon

                newRow.SetIcon(new IconName(entity.Icon));
                // Set cell values and enable/disable redundant cells based on the entity template

                for (int i = m_UnboundGrid.FixedColumns; i < m_UnboundGrid.Columns.Count; i++)
                {
                    UnboundGridColumn column = m_UnboundGrid.Columns[i];

                    // Try getting the template property

                    EntityTemplatePropertyInternal templateProperty = template.GetProperty(column.Name);

                    if (templateProperty == null)
                    {
                        // Disable this cell

                        column.DisableCell(newRow, DisabledCellDisplayMode.GreyHideContents);
                        continue;
                    }

                    // Set value

                    newRow[column] = entity.Get(templateProperty.PropertyName);

                    // This is an active cell

                    if (templateProperty.IsMandatory)
                    {
                        column.Caption = _Library.Utils.AppendAsteriskMandatoryField(column.Caption);
                        column.SetCellMandatory(newRow);

                        var name = entity.Name;
                    }

                    if (LocationControl.IsLocation(templateProperty.PropertyName))
                    {
                        column.ShowCellButton(newRow, LocationControl.LocationBrowseIcon);
                    }

                    if (!string.IsNullOrEmpty(templateProperty.FilterBy))
                    {
                        // Setup this column for filtering

                        // Mark the column that is used for filtering
                        if (templateProperty.FilterBy.Contains(","))
                        {
                            var filterBys = templateProperty.FilterBy.Split(',');
                            foreach (var filter in filterBys)
                            {
                                var filterItem = filter.Trim();
                                UnboundGridColumn filterBySourceColumn = m_UnboundGrid.GetColumnByName(filterItem);
                                if (filterBySourceColumn != null)
                                {
                                    filterBySourceColumn.Tag = true;
                                }

                                // Setup filter

                                object filterValue = entity.Get(filterItem);

                                if (filterValue != null)
                                {
                                    IEntity filterValueEntity = filterValue as IEntity;
                                    bool isValid = filterValueEntity == null || BaseEntity.IsValid(filterValueEntity);
                                    if (isValid)
                                    {
                                        SetupFilterBy(templateProperty, newRow, column, filterValue);
                                    }
                                }
                            }
                        }
                        else
                        {
                            UnboundGridColumn filterBySourceColumn = m_UnboundGrid.GetColumnByName(templateProperty.FilterBy);
                            if (filterBySourceColumn != null)
                            {
                                filterBySourceColumn.Tag = true;
                            }

                            // Setup filter

                            object filterValue = entity.Get(templateProperty.FilterBy);

                            if (filterValue != null)
                            {
                                IEntity filterValueEntity = filterValue as IEntity;
                                bool isValid = filterValueEntity == null || BaseEntity.IsValid(filterValueEntity);
                                if (isValid)
                                {
                                    SetupFilterBy(templateProperty, newRow, column, filterValue);
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(templateProperty.Criteria))
                    {
                        // A criteria has been specified for this column, setup the browse

                        ICriteriaTaskService criteriaTaskService = (ICriteriaTaskService)_Library.GetService(typeof(ICriteriaTaskService));

                        // Once the query is populated the Query Populated Event is raised. This is because the criteria
                        // could prompt for VGL values or C# values.
                        // Prompted Criteria is ignored

                        string linkedType = EntityType.GetLinkedEntityType(template.TableName, templateProperty.PropertyName);
                        CriteriaSaved criteria = (CriteriaSaved)_EntityManager.Select(TableNames.CriteriaSaved, new Identity(linkedType, templateProperty.Criteria));

                        if (BaseEntity.IsValid(criteria))
                        {
                            //    // Generate a query based on the criteria

                            //    criteriaTaskService.QueryPopulated += CriteriaTaskService_QueryPopulated;
                            //    m_CriteriaQuery = null;
                            //    m_InitialisingCriteria = true;
                            //    criteriaTaskService.GetPopulatedCriteriaQuery(criteria);
                            //    m_InitialisingCriteria = false;

                            //    if (m_CriteriaQuery != null)
                            //    {
                            //        // Assign the browse to the column

                            //        IEntityCollection browseEntities = EntityManager.Select(m_CriteriaQuery.TableName, m_CriteriaQuery);
                            //        EntityBrowse criteriaBrowse = BrowseFactory.CreateEntityBrowse(browseEntities);
                            //        column.SetCellBrowse(newRow, criteriaBrowse);
                            //        m_CriteriaBrowseLookup[criteriaBrowse] = browseEntities;

                            //        // Make sure the cell's value is present within the browse

                            //        IEntity defaultValueEntity = entity.GetEntity(templateProperty.PropertyName);

                            //        if (BaseEntity.IsValid(defaultValueEntity) && !browseEntities.Contains(defaultValueEntity) && !defaultValueEntity.IsNew())
                            //        {
                            //            // The default value is not within the specified criteria, null out this cell

                            //            newRow[templateProperty.PropertyName] = null;
                            //        }
                            //    }
                        }
                    }

                    if (templateProperty.IsReadOnly || !ValidStatusForModify(entity))
                    {
                        // Disable the cell but display it's contents

                        column.DisableCell(newRow, DisabledCellDisplayMode.GreyShowContents);
                    }
                    else if (templateProperty.IsHidden)
                    {
                        column.Visible = false;
                    }

                    // Do specific column stuff 

                    //SetupGridColumn(entity, templateProperty, newRow, column);
                }
            }
        }

        /// <summary>
		/// Validates the status for modify.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <returns></returns>
        protected virtual bool ValidStatusForModify(IEntity entity)
        {

            if (entity is Sample)
            {
                if (!((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdC) &&
                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdW) &&
                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdV) &&
                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdU) &&
                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdB) &&
                    !((Sample)entity).Status.IsPhrase(PhraseSampStat.PhraseIdD) &&
                    !entity.IsNew())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
		/// Setups the filter by.
		/// </summary>
		/// <param name="templateProperty">The template property.</param>
		/// <param name="row">The new row.</param>
		/// <param name="column">The column.</param>
		/// <param name="filterValue">The filter value.</param>
		private void SetupFilterBy(EntityTemplatePropertyInternal templateProperty, UnboundGridRow row, UnboundGridColumn column, object filterValue)
        {
            // Setup entity browse filtering

            IQuery filteredQuery = templateProperty.CreateFilterByQuery(filterValue, row);

            // Setup the property browse for the collection column to browse collection properties for the table.

            IEntityBrowse browse = _BrowseFactory.CreateEntityOrHierarchyBrowse(filteredQuery.TableName, filteredQuery);

            column.SetCellEntityBrowse(row, browse);
        }
        private void BuildColumns(IEntityCollection entities)
        {
            foreach (IEntity entity in entities)
            {
                // Get the entity template from the entity

                EntityTemplateInternal template = GetTemplate(entity);
                if (template == null) continue;

                foreach (EntityTemplateProperty property in template.EntityTemplateProperties)
                {
                    // Add a column for this entity template property

                    // Retrieve or create column

                    UnboundGridColumn gridcolumn = m_UnboundGrid.GetColumnByName(property.PropertyName);

                    if (gridcolumn != null) continue;

                    gridcolumn = m_UnboundGrid.AddColumn(property.PropertyName, property.LocalTitle, "Properties", 100);

                    //if (template.TableName == TestBase.EntityName && property.PropertyName == TestPropertyNames.Instrument)
                    //{
                    //    // Instruments must be available and not retired
                    //    IQuery query = _EntityManager.CreateQuery(InstrumentBase.EntityName);
                    //    query.AddEquals(InstrumentPropertyNames.Available, true);
                    //    query.AddEquals(InstrumentPropertyNames.Retired, false);
                    //    EntityBrowse instrumentBrowse = BrowseFactory.CreateEntityBrowse(query);
                    //    gridcolumn.SetColumnBrowse(instrumentBrowse);
                    //}
                    //else
                    //{
                    //    gridcolumn.SetColumnEditorFromObjectModel(template.TableName, property.PropertyName);
                    //}

                    if (property.PromptType.IsPhrase(PhraseEntTmpPt.PhraseIdHIDDEN))
                    {
                        gridcolumn.Visible = false;
                    }
                }
            }
        }

        private EntityTemplateInternal GetTemplate(IEntity entity)
        {
            return (EntityTemplateInternal)entity.GetEntity("EntityTemplate");
        }
    }
}