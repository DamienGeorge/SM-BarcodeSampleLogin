using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.ObjectModel;

namespace Customization.ObjectModel
{
    /// <summary>
    /// Class contains extenstion methods for Processing Entities
    /// </summary>
    public static class EntityExtensions
    {
        #region Entity Extensions
        public static void SetFieldByType(this IEntity entity, string PropertyName, ISchemaField schemaField, string value)
        {
            switch (schemaField.DataType.SMType)
            {
                case SMDataType.PackedDecimal:
                    entity.Set(PropertyName, value);
                    break;
                case SMDataType.Real:
                    entity.Set(PropertyName, float.Parse(value));
                    break;
                case SMDataType.Integer:
                    entity.Set(PropertyName, int.Parse(value));
                    break;
                case SMDataType.Boolean:
                    entity.Set(PropertyName, bool.Parse(value));
                    break;
                case SMDataType.DateTime:
                    var date = entity.GetNullableDateTime(PropertyName);

                    if (date.ToString().Trim() == string.Empty)
                    {
                        entity.Set(PropertyName, string.Empty);
                    }
                    else
                    {
                        entity.Set(PropertyName, ((DateTime)date));
                    }
                    break;
                case SMDataType.Text:
                    entity.Set(PropertyName, value);
                    break;
                default:
                    if (value != null)
                    {
                        //TODO - If entity how to resolve back to entity?
                        //schemaField.
                    }
                    else
                    {
                        if (entity.GetType() == typeof(string))
                        {
                            entity.Set(PropertyName, string.Empty);
                        }
                        else
                        {
                            entity.Set(PropertyName, null);
                        }
                    }
                    break;
            }
        }
        #endregion

        #region EntityManager Extensions
        /// <summary>
        /// Method to get Entity Template by EntityTemplate Id
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="entityTemplateId"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static EntityTemplateInternal GetEntityTemplateById(this IEntityManager entityManager, string entityTemplateId)
        {
            return entityManager.SelectLatestVersion<EntityTemplateInternal>(entityTemplateId) ?? throw new NullReferenceException($"Could not find EntityTemplate with id : {entityTemplateId}");
        }

        /// <summary>
        /// Method to get Workflow by WorkflowID and optionally Workflow Version
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="workflowGUID"></param>
        /// <param name="workflowVersion"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static Workflow GetWorkflowById(this IEntityManager entityManager, string workflowGUID, string workflowVersion = "")
        {
            if (String.IsNullOrEmpty(workflowVersion))
            {
                return entityManager.SelectLatestVersion<Workflow>(new Identity(workflowGUID)) ?? throw new NullReferenceException($"Could not find an active workflow with id : {workflowGUID}");
            }
            else
            {
                return entityManager.Select<Workflow>(new Identity(workflowGUID, workflowVersion)) ?? throw new NullReferenceException($"Could not find workflow with id : {workflowGUID} and version : {workflowVersion}");
            }
        }
        #endregion
    }
}
