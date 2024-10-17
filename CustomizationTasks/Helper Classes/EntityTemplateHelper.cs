using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Thermo.Framework.Core;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;

namespace Customization.Tasks
{
    public static class EntityTemplateHelper
    {
        #region Serialization

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

        public static IEntity DeserializeJSONUsingEntityTemplate(ScannedSampleBase pendingSample, EntityTemplateInternal entityTemplate, string JSON, IEntity entity)
        {
            IDictionary<string, string> deserializedJSON = JsonConvert.DeserializeObject<Dictionary<string, string>>(JSON);

            //SampleBase deserializedSample = JsonConvert.DeserializeObject<SampleBase>(JSON);
            try
            {
                foreach (EntityTemplateProperty field in entityTemplate.EntityTemplateProperties)
                {
                    entity.Set(field.Name, deserializedJSON[field.Name]);
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            ////map object to sample fields
            //foreach(var entry in deserializedJSON)
            //{

            //}

            return entity;

        }
        #endregion
    }
}
