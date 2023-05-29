#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public class Template
        {
            [Method(Description = "Get Template by Id ...")]
            public static Dictionary<string, object> GetTemplate(
                [Parameter("Id")] string id)
            {
                var template =AFElementTemplate.FindElementTemplate(Connector.GetAFServer(), Guid.Parse(id));
                return template == null ? null : Conversion.CreateDictionary(new List<AFElementTemplate> {template}) ;
            }
            [Method(Description = "Get Template Attributes ...")]
            public static Dictionary<string, object> GetTemplateAttributes(
                [Parameter("Id")] string id)

            {
                AFElementTemplate template;
                if (Guid.TryParse(id, out Guid result))
                    template = AFElementTemplate.FindElementTemplate(Connector.GetAFServer(), result);
                else
                    return null;

                if (template == null) return null;
                return template.AttributeTemplates.Count == 0 ? null :
                    Conversion.CreateDictionary(template.AttributeTemplates);

            }
            [Method(Description = "Get Element Templates by Name ...")]
            public static Dictionary<string, object> GetElementTemplates(
                [Parameter("Name")] string name)
            {
                var templates = AFElementTemplate.FindElementTemplates(Connector.GetAFDatabase(),
                    name, AFSearchField.Name,
                    AFSortField.Name, AFSortOrder.Ascending, Connector.GetMaxItemReturn()); // we need only 1st template
                templates = new AFNamedCollectionList<AFElementTemplate>(templates
                    .Where(n => n.InstanceType == typeof(AFElement)).ToList());

                return templates.Count == 0 ? null : Conversion.CreateDictionary(templates);
            }

            [Method(Description = "Get Attributes in Element Template by Name ...")]
            public static Dictionary<string, object> GetElementTemplateAttributes(
                [Parameter("Name")]string name)
            {
                var templates = AFElementTemplate.FindElementTemplates(Connector.GetAFDatabase(),
                    name, AFSearchField.Name,
                    AFSortField.Name, AFSortOrder.Ascending, 1); // we need only 1st template
                templates = new AFNamedCollectionList<AFElementTemplate>(templates
                    .Where(n => n.InstanceType == typeof(AFElement)).ToList());

                var template = templates.FirstOrDefault();
                return template == null ? null : Conversion.CreateDictionary(template.AttributeTemplates);
            }
            [Method(Description = "Get Frame Templates by Name ...")]
            public static Dictionary<string, object> GetFrameTemplates(
             [Parameter("Name")] string name)

            {
                var templates =
                    AFElementTemplate.FindElementTemplates(Connector.GetAFDatabase(), name, AFSearchField.Name,
                        AFSortField.Name, AFSortOrder.Ascending, Connector.GetMaxItemReturn());
                templates = new AFNamedCollectionList<AFElementTemplate>(templates
                    .Where(n => n.InstanceType == typeof(AFEventFrame)).ToList());
                return templates.Count == 0 ? null : Conversion.CreateDictionary(templates);
            }
            [Method(Description = "Get Attributes in Frame Template by Name ...")]
            public static Dictionary<string, object> GetFrameTemplateAttributes(
                [Parameter("Name")] string name)

            {
                var templates =
                    AFElementTemplate.FindElementTemplates(Connector.GetAFDatabase(), name, AFSearchField.Name,
                        AFSortField.Name, AFSortOrder.Ascending, 1);
                templates = new AFNamedCollectionList<AFElementTemplate>(templates
                    .Where(n => n.InstanceType == typeof(AFEventFrame)).ToList());
                var template = templates.FirstOrDefault();
                return template == null ? null : Conversion.CreateDictionary(template.AttributeTemplates);
            }
        }
    }
}