using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;
using Customization.ObjectModel;

namespace Customization.Tasks
{
    /// <summary>
    /// Utility class containing Helper methods for EntityTemplate Processing
    /// </summary>
    public static class EntityTemplateHelper
    {
        #region Serialization

        /// <summary>
        /// Serialize Entity to JSON using Entity Template
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityTemplate"></param>
        /// <returns></returns>
        public static string SerializeJSONUsingEntityTemplate(IEntity entity, EntityTemplateInternal entityTemplate)
        {
            IDictionary<string, string> fields = new Dictionary<string, string>();

            foreach (EntityTemplateProperty field in entityTemplate.EntityTemplateProperties)
            {
                fields.Add(field.Name, EntityType.ValueToString(entity, field.Name));
            }

            string serializedJSON = JsonConvert.SerializeObject(fields, Formatting.Indented);

            return serializedJSON;
        }

        /// <summary>
        /// Deserialize Entity from JSON using Entity Template
        /// </summary>
        /// <param name="pendingSample"></param>
        /// <param name="entityTemplate"></param>
        /// <param name="JSON"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IEntity DeserializeJSONUsingEntityTemplate(ScannedSampleBase pendingSample, EntityTemplateInternal entityTemplate, string JSON, IEntity entity)
        {
            IDictionary<string, string> deserializedJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(JSON);

            try
            {
                var currentEntityTemplate = entity.GetEntity("EntityTemplate");
                if (currentEntityTemplate is null || String.IsNullOrEmpty(currentEntityTemplate.Identity))
                {
                    entity.Set("EntityTemplate", entityTemplate);
                }

                foreach (EntityTemplateProperty field in entityTemplate.EntityTemplateProperties)
                {
                    if (String.IsNullOrEmpty(deserializedJSON[field.Name]) == false)
                    {
                        ISchemaField schemaField = entity.FindSchemaField(field.Name);

                        entity.SetFieldByType(field.Name, schemaField, deserializedJSON[field.Name]);
                    }
                }
            }
            catch (Exception ex)
            {
                //If entity Template is modified, should anything be done?
            }

            return entity;

        }
        #endregion
    }
}
