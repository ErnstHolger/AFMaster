#region using section

using System;
using System.Collections.Generic;
using System.Linq;
using AFMaster.Util;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        // todo: limit size?
        private static readonly BufferDictionary<string, AFAttribute> _attributeLookup =
            new BufferDictionary<string, AFAttribute>(2500);

        private static readonly BufferDictionary<string, AFElement> _elementLookup =
            new BufferDictionary<string, AFElement>(2500);

        private static readonly BufferDictionary<string, AFEventFrame> _frameLookup =
            new BufferDictionary<string, AFEventFrame>(2500);

        public class GetByPath
        {
            //public static AFAttribute GetAttributeByPath(string attributePath)
            //{
            //    var afAttribute = AFAttribute.FindAttribute(attributePath, null);
            //    return afAttribute;
            //}

            //public static Model.Attribute GetAttributeByPath(string attributePath, bool asModel)
            //{
            //    return Conversion.CreateAttribute(new List<AFAttribute> {GetAttributeByPath(attributePath)});
            //}

            //public static AFElement GetElementByPath(string elementPath)
            //{
            //    IEnumerable<AFElement> afAttributes =
            //        AFElement.FindElementsByPath(new List<string> {elementPath}, null);
            //    return afAttributes.FirstOrDefault();
            //}

            //public static Model.Element GetElementByPath(string elementPath, bool asModel)
            //{
            //    return Conversion.CreateElement(new List<AFElement> {GetElementByPath(elementPath)});
            //}
            //// this is not working
            //[Obsolete("should use GetElementByPath")]
            //public static AFAttribute GetAttributeByGuid(string attributeGuid)
            //{
            //    AFAttribute afAttribute;
            //    if (String.IsNullOrEmpty(attributeGuid)) return null;
            //    if (_attributeLookup.TryGet(attributeGuid, out afAttribute))
            //        return afAttribute;

            //    var temp = AFAttribute.FindAttribute(null, new Guid(attributeGuid));
            //    _attributeLookup.TryAdd(attributeGuid, temp);
            //    return temp;
            //}
            //[Obsolete("should use GetElementByPath")]
            //public static Model.Attribute GetAttributeByGuid(string attributeGuid, bool asModel)
            //{
            //    return Conversion.CreateAttribute(new List<AFAttribute>()
            //    { GetAttributeByGuid(attributeGuid)});
            //}
            //[Obsolete("should use GetElementByPath")]
            //public static AFElement GetElementByGuid(string elementGuid)
            //{
            //    AFElement element;
            //    if (String.IsNullOrEmpty(elementGuid)) return null;
            //    if (_elementLookup.TryGet(elementGuid, out element))
            //        return element;
            //    AFElement.LoadElements(Connector.GetAFServer(), new[] { new Guid(elementGuid) }, null);
            //    element =AFElement.FindElement(Connector.GetAFServer(), new Guid(elementGuid));
            //    _elementLookup.TryAdd(elementGuid,element);
            //    return element;
            //}
            //[Obsolete("should use GetElementByPath")]
            //public static Model.Element GetElementByGuid(string elementGuid, bool asModel)
            //{
            //    return Conversion.CreateElement(new List<AFElement>()
            //    { GetElementByGuid( elementGuid)});
            //}

            public static AFElementTemplate GetElementTemplateByName(string name)
            {
                var template = Connector.GetAFDatabase().ElementTemplates.
                    FirstOrDefault(n => n.Name.WildCardIsMatch(name));
                return template;
            }

            public static Dictionary<string, object> GetElementTemplateByName(string name, bool asModel)
            {
                return Conversion.CreateDictionary(new List<AFElementTemplate>
                { GetElementTemplateByName(name)});
            }
            //[Method(Description = "Get Frame by Id ...")]
            //public static AFEventFrame GetFrameByGuid(string frameGuid)
            //{
            //    AFEventFrame frame;
            //    if (string.IsNullOrEmpty(frameGuid)) return null;
            //    AFEventFrame.LoadEventFrames(Connector.GetAFServer(), new[] {new Guid(frameGuid)}, null);
            //    frame = AFEventFrame.FindEventFrame(Connector.GetAFServer(), new Guid(frameGuid));

            //    return frame;
            //}
            //public static Model.Frame GetFrameByGuid(string frameGuid, bool asModel)
            //{
            //    return Conversion.CreateFrame(new List<AFEventFrame> {GetFrameByGuid(frameGuid)});
            //}
            //[Method(Description = "Get Element by Id ...")]
            //public static AFElement GetElementByGuid(string elementGuid)
            //{
            //    AFElement element;
            //    if (string.IsNullOrEmpty(elementGuid)) return null;
            //    element = AFElement.FindElement(Connector.GetAFServer(), new Guid(elementGuid));
            //    return element;
            //}

            //public static Model.Element GetElementByGuid(string elementGuid, bool asModel)
            //{
            //    return Conversion.CreateElement(new List<AFElement> { GetElementByGuid(elementGuid) });
            //}

            //[Method(Description = "Get Element by Id ...")]
            //public static AFAttribute GetAttributeByGuid(string attributeGuid)
            //{
            //    AFAttribute attribute;
               
            //    if (string.IsNullOrEmpty(attributeGuid)) return null;
            //    return AFAttribute.FindAttribute(null, new Guid(attributeGuid));
                
            //}

            //public static Model.Element GetAttributeByGuid(string attributeGuid, bool asModel)
            //{
            //    return Conversion.CreateElement(new List<AFElement> { GetElementByGuid(attributeGuid) });
            //}
        }
    }
}