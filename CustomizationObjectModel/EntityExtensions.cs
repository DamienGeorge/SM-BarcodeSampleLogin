using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;

namespace Customization.ObjectModel
{
    public static class EntityExtensions
    {
        //public static IDataType GetEntityType(this EntityTemplateProperty entityTemplateProperty)
        //{
        //    ISchemaField schemaField = entityTemplateProperty.FindSchemaField(entityTemplateProperty.Name);
        //    return schemaField.DataType;
        //}

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

        public static EntityTemplateInternal GetEntityTemplateById(this IEntityManager entityManager, string entityTemplateId)
        {
            return entityManager.SelectLatestVersion<EntityTemplateInternal>(entityTemplateId) ?? throw new NullReferenceException($"Could not find EntityTemplate with id : {entityTemplateId}");
        }

        public static Workflow GetWorkflowById(this IEntityManager entityManager, string workflowGUID, string workflowVersion= "")
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
    }
}
