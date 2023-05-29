#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public class Blend
        {
            
            public static Model.Template FindAllElementTemplates()
            {
                var templates = Connector.GetAFDatabase().ElementTemplates
                    .Where(n => n.InstanceType == typeof(AFElement)).ToList();

                var result = new Model.Template(templates.Count);
                for (var index = 0; index < templates.Count; index++)
                {
                    result.Id[index] = templates[index].UniqueID;
                    result.Name[index] = templates[index].Name;
                }
                return result;
            }

            public static Model.Template FindAllFrameTemplates()
            {
                var templates = Connector.GetAFDatabase().ElementTemplates
                    .Where(n => n.InstanceType == typeof(AFEventFrame)).ToList();

                var result = new Model.Template(templates.Count);
                for (var index = 0; index < templates.Count; index++)
                {
                    result.Id[index] = templates[index].UniqueID;
                    result.Name[index] = templates[index].Name;
                }
                return result;
            }



            public static List<List<string>> GetEventFramesAndAttributes(string startTime,
                string endTime,
                string name)
            {
                var result = new List<List<string>>();
                var afEventFrames = Frame.GetFrames(startTime, endTime, name);
                result.Add(new List<string>
                {
                    "Type",
                    "Parent",
                    "Name",
                    "Id",
                    "StartTime",
                    "EndTime",
                    "Value",
                    "DataType",
                    "Category",
                    "Template",
                    "Element"
                });
                foreach (var afEventFrame in afEventFrames)
                {
                    result.Add(new List<string>
                    {
                        "Frame",
                        afEventFrame.Parent != null ? afEventFrame.Parent.Name : "",
                        afEventFrame.Name,
                        afEventFrame.ID.ToString(),
                        afEventFrame.StartTime.ToString(),
                        afEventFrame.EndTime.ToString(),
                        "",
                        "",
                        string.Join(";", afEventFrame.Categories),
                        afEventFrame.Template.Name,
                        afEventFrame.PrimaryReferencedElement != null ? afEventFrame.PrimaryReferencedElement.Name : ""
                    });
                    var attributes = Attribute.GetAttributes("frame","",afEventFrame.UniqueID,"","");
                    result.AddRange(attributes.Select(afAttribute => new List<string>
                    {
                        "Attribute",
                        afEventFrame.Name,
                        afAttribute.Name,
                        afAttribute.ID.ToString(),
                        "",
                        "",
                        afAttribute.GetValue().ToString(),
                        afAttribute.Type.ToString().Replace("System.", ""),
                        string.Join(";", afAttribute.Categories)
                    }));
                }
                return result;
            }
        }
    }
}