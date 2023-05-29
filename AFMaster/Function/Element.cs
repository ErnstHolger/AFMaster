#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Search;

#endregion

namespace AFMaster
{
    //[ErrorAspect]

    public partial class Library
    {
        public class Element
        {
            [Method(Description = "Delete Elements ...")]
            public static bool DeleteElements(
                [Parameter("Array of Ids")] string[] ids)
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var elements = GetElements("",id,"","");
                        // double check name
                        if (elements==null) continue;

                        elements[0].Delete();
                        Connector.GetAFDatabase().CheckIn();
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                }
                return true;
            }

            [Method(Description = "Create Element Category ...")]
            public static bool CreateElementCategory(
                [Parameter("Category Name")]string name)
            {
                try
                {
                    Connector.GetAFDatabase().CreateCategoryIfNotExist(name);
                    return true;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return false;
                }
            }

            [Method(Description = "Set Element Template ...")]
            public static Dictionary<string,object> SetElementTemplate(
                [Parameter("Name")]string name,
                [Parameter("Description")]string description,
                [Parameter("Array Categories")]string[] categories,
                [Parameter("Base Name")]string baseTemplate = "")
            {
                try
                {
                    var template = Connector.GetAFDatabase()
                        .CreateTemplateIfNotExist(name, description, categories);
                    return Conversion.CreateDictionary(
                        new List<AFElementTemplate> {template});
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }
            [Method(Description = "Get Element ...")]
            public static Dictionary<string, object> GetElementTemplate(
                [Parameter("Template Name with Wildcards")]string nameFilter)
            {
                var templates=Connector.GetAFDatabase().ElementTemplates.
                    Where(n => n.Name.WildCardIsMatch(nameFilter)).ToList();
                return Conversion.CreateDictionary(templates);
            }
            [Method(Description = "Set Element ...")]
            public static Dictionary<string, object> SetElement(
                [Parameter("Name")]string name,
                [Parameter("Description")]string description,
                [Parameter("Template")]string templateName,
                [Parameter("Category")]string[] categories,
                [Parameter("Parent id")] string parentId)
            {
                try
                {
                    var element = new AFElement(name) {Description = description};
                    Connector.GetAFDatabase().Elements.Add(element);
                    var template =
                        Connector.GetAFDatabase()
                            .ElementTemplates.FirstOrDefault(
                                n => n.Name.Equals(templateName, StringComparison.CurrentCultureIgnoreCase));
                    if (template != null) element.Template = template;
                    if (categories != null)
                    {
                        foreach (var categoryName in categories)
                        {
                            var category =
                                Connector.GetAFDatabase()
                                    .ElementCategories.FirstOrDefault(
                                        n => n.Name.Equals(categoryName, StringComparison.CurrentCultureIgnoreCase));
                            if (category != null) element.Categories.Add(category.Name);
                        }
                    }
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        var elements = Element.GetElements("",parentId,"","");
                        elements[0].Elements.Add(element);
                    }
                    Connector.GetAFDatabase().CheckIn();
                    return Conversion.CreateDictionary(new List<AFElement> {element}, true);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static IList<AFElement> GetElements(string name, string id, string category, string template)
            {
                List<AFElement> elements = new List<AFElement>();
                string query = "";
                if (!string.IsNullOrEmpty(name)) {query += $" Name:'{name}'";}
                if (!string.IsNullOrEmpty(id)) { query += $" ID:'{id}'"; }
                if (!string.IsNullOrEmpty(category)) { query += $" Category:'{category}'"; }
                if (!string.IsNullOrEmpty(template)) { query += $" Template:'{template}'"; }
                using (var search =
                    new AFElementSearch(Connector.GetAFDatabase(), 
                        "Get Elements", query))
                {
                    search.CacheTimeout = TimeSpan.FromMinutes(10);
                    elements.AddRange(search.FindObjects(fullLoad: true));
                }
                return elements;
            }

            [Method(Description = "Get Elements ...")]
            public static Dictionary<string, object> GetElements(
                [Parameter("Name")] string name,
                [Parameter("Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    return
                        Conversion.CreateDictionary(
                            GetElements(name, id, category,template));
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            [Method(Description = "Get Attributes in Element ...")]
            public static Dictionary<string, object> GetAttributesInElement(
                [Parameter("Name")] string name,
                [Parameter("Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    var element = GetElements(name, id, category, template).FirstOrDefault();
                    if(element is null) return null;
                    return Conversion.CreateDictionary(element.Attributes);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

        }
    }
}