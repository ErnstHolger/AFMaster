#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster
{
    //[ErrorAspect]


    public partial class Library
    {
        #region Nested type: Attribute

        public class Attribute
        {
            [Method(Description = "Delete Attributes ...")]
            public static bool DeleteAttributes(
                [Parameter("Type: Frame or Element")] string type,
                [Parameter("Parent Id")] string parentId,
                [Parameter("Array of ids")] string[] ids)
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var attribute = GetAttributes(type, parentId, id, "", "").FirstOrDefault();
                        // double check name
                        if (attribute == null) continue;
                        var temp = attribute.Element;
                        temp.Attributes.Remove(attribute);
                        Connector.GetAFDatabase().CheckIn();
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                }
                return true;
            }

            [Method(Description = "Set Attribute in Element ...")]
            public static Dictionary<string, object> SetAttributeInElement(
                [Parameter("Element Id")] string id,
                [Parameter("Attribute Name")] string name,
                [Parameter("Attribute Description")] string description,
                [Parameter("Data Type")] string dataType,
                [Parameter("Data Reference")] string dataReference,
                [Parameter("Configuration String")] string configString)
            {
                try
                {
                    var elements = Element.GetElements("", id, "", "");
                    if (elements.Count == 0) return null;
                    var attribute = elements[0].Attributes.Add(name).Configure(description,
                        dataType, dataReference, configString);
                    elements[0].Database.CheckIn();
                    return Conversion.CreateDictionary(new List<AFAttribute> { attribute });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Set Attribute in Attribute ...")]
            public static Dictionary<string, object> SetAttributeInAttribute(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Parent Attribute Id")] string id,
                [Parameter("Attribute Name")] string name,
                [Parameter("Attribute Description")] string description,
                [Parameter("Data Type")] string dataType,
                [Parameter("Data Reference")] string dataReference,
                [Parameter("Config String")] string configString,
                [Parameter("True or False is Configuration Item")] bool isConfigurationItem)
            {
                try
                {
                    var attributes = Attribute.GetAttributes("",parentId, id, "", "");
                    if (attributes.Count == 0) return null;
                    var attribute = attributes[0].Attributes.Add(name).
                        Configure(description,
                        dataType, dataReference, configString);
                    attributes[0].Database.CheckIn();
                    return Conversion.CreateDictionary(new List<AFAttribute> { attribute });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Set static Attribute in Frame ...")]
            public static Dictionary<string, object> SetAttributeInFrame(
                [Parameter("Frame Id")] string id,
                [Parameter("Name")] string name,
                [Parameter("Description")] string description,
                [Parameter("Data type")] string dataType,
                [Parameter("Value")] object value)
            {
                try
                {
                    var frame = Frame.GetFrames("", id, "", "", "", "", "").FirstOrDefault();
                    if (frame == null) return null;
                    var attribute = frame.Attributes.Add(name);
                    attribute.Description = description;
                    attribute.Type = Type.GetType("System." + dataType);
                    attribute.SetValue(new AFValue(null, value, frame.StartTime));

                    frame.Database.CheckIn();
                    return Conversion.CreateDictionary(new List<AFAttribute> { attribute });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            //[Model(Description = "Create AF file ...")]
            [Method(Description = "Create AF File ...")]
            public static bool CreateAFFile(
                [Parameter("Element Path")] string elementPath,
                [Parameter("Attribute Name")] string attributeName,
                [Parameter("File Path")] string filePath)
            {
                IDictionary<string, string> errors;
                var sc = StringComparison.InvariantCultureIgnoreCase;
                IEnumerable<string> elementPathList = new List<string> { elementPath };
                IList<AFElement> elements = AFElement.FindElementsByPath(elementPathList, null, out errors);
                if (elements.Count != 1) return false;
                IList<AFAttribute> attributes = elements[0].Attributes;
                if (attributes == null || !attributes.Any(n => n.Name.Equals(attributeName, sc))) return false;
                var attribute = attributes.First(n => n.Name.Equals(attributeName, sc));
                try
                {
                    var file = new AFFile(attribute);
                    file.Upload(filePath);
                    attribute.SetValue(file, null);
                    attribute.Database.CheckIn();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public static T SetAttributesToProperties<T>(AFElement element) where T : new()
            {
                var result = new T();
                foreach (var afAttribute in element.Attributes)
                    try
                    {
                        var propertyInfo = result.GetType().GetProperty(afAttribute.Name);
                        propertyInfo.SetValue(result, Convert.ChangeType(afAttribute.GetValue().Value,
                            propertyInfo.PropertyType), null);
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                return result;
            }

            public static IList<AFAttribute> GetAttributes(string type,
                string parentId, string attributeId, string category, string template)
            {
                var attributes = new List<AFAttribute>();
                string query = "";
                string parentQuery = "";
                if (string.IsNullOrEmpty(parentId))
                { parentQuery="Name: '*' "; }
                else if (Guid.TryParse(parentId, out var _))
                { parentQuery = $"ID: '{parentId}' ";  }
                else
                { parentQuery = $"Name: '{parentId}' "; }

                if (type == "element") { query += $"Element:{{ {parentQuery} }}"; }
                if (type == "frame") { query += $"EventFrame:{{ {parentQuery} }}"; }
                //https://customers.osisoft.com/s/knowledgearticle?knowledgeArticleUrl=000029686&_ga=2.12207293.842393574.1609786349-934359580.1608301935
                if (!string.IsNullOrEmpty(attributeId))
                {
                    if (Guid.TryParse(attributeId, out var _))
                    {
                        query += $" ID:{attributeId}";
                    }
                    else
                    {
                        query += $" Name:'{attributeId}'";
                    }
                }
                if (!string.IsNullOrEmpty(category)) { query += $" Category:'{category}'"; }
                if (!string.IsNullOrEmpty(template)) { query += $" Template:'{template}'"; }
                try
                {
                    using (var search =
                        new AFAttributeSearch(Connector.GetAFDatabase(),
                            "Get Elements", query))
                    {
                        search.CacheTimeout = TimeSpan.FromMinutes(10);
                        attributes.AddRange(search.FindObjects(fullLoad: true));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLog.Error("error in GetAttributes " + ex.Message);
                }
                return attributes;
            }

            [Method(Description = "Get Element Attributes ...")]
            public static Dictionary<string,object> GetElementAttributes(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    return
                        Conversion.CreateDictionary(
                            GetAttributes("element",parentId, id, category, template));
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Frame Attributes ...")]
            public static Dictionary<string, object> GetFrameAttributes(
                [Parameter("Parent Id")] string parentId,
                [Parameter("Attribute Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    return
                        Conversion.CreateDictionary(
                            GetAttributes("frame", parentId,id, category, template));
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }
        }

        #endregion
    }
}