#region using section

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.Analysis;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster
{
    public static class Extensions
    {
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }
        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
        public static AFTime ToAFTime(this object timeStamp)
        {
            if (timeStamp is AFTime) return (AFTime)timeStamp;
            if (timeStamp is string stringValue)return new AFTime(stringValue);
            if (double.TryParse(timeStamp.ToString(), out double doubleValue)) return new AFTime(doubleValue);

            throw new AFAnalysisException();
        }
        public static string ToStringTime(this object timeStamp)
        {
            //SimpleLog.Error("ToStringTime: "+timeStamp.ToAFTime().ToString());
            return timeStamp.ToAFTime().ToString();
        }
        public static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
        public static bool WildCardIsMatch(this string name, string filter)
        {
            return Regex.IsMatch(name, WildCardToRegular(filter));
        }
        public static AFCategory FindCategoryByName(this AFDatabase afDatabase, string name)
        {
            return afDatabase.ElementCategories.FirstOrDefault(n => n.Name.Equals(
                name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static List<AFCategory> FindCategoriesByName(this AFDatabase afDatabase,
            string[] names)
        {
            var result = new List<AFCategory>();
            foreach (var name in names)
                result.Add(afDatabase.FindCategoryByName(name));
            return result;
        }

        public static AFCategory CreateCategoryIfNotExist(this AFDatabase afDatabase, string name)
        {
            try
            {
                var category = afDatabase.ElementCategories.FirstOrDefault(n => n.Name.Equals(
                                   name, StringComparison.InvariantCultureIgnoreCase)) ??
                               afDatabase.ElementCategories.Add(name);
                afDatabase.CheckIn();
                return category;
            }
            catch
            {
                return null;
            }
        }

        public static AFCategory FindTemplateByName(this AFDatabase afDatabase, string name)
        {
            return afDatabase.ElementCategories.FirstOrDefault(n => n.Name.Equals(
                name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static AFElementTemplate CreateTemplateIfNotExist(this AFDatabase afDatabase,
            string name, string description, string[] categoryNames)
        {
            try
            {
                var template = afDatabase.ElementTemplates.FirstOrDefault(n => n.Name.Equals(
                                   name, StringComparison.InvariantCultureIgnoreCase)) ??
                               afDatabase.ElementTemplates.Add(name);
                template.Description = description;
                if (categoryNames != null)
                {
                    var categories = afDatabase.FindCategoriesByName(categoryNames);
                    categories.ForEach(n => template.Categories.Add(n));
                }
                afDatabase.CheckIn();
                return template;
            }
            catch
            {
                return null;
            }
        }

        public static AFAttribute Configure(this AFAttribute attribute, string description,
            string dataType, string dataReference = "", string configString = "")
        {
            attribute.Description = description;
            attribute.Type = Type.GetType(dataType);
            if (!string.IsNullOrEmpty(dataReference))
                attribute.DataReferencePlugIn =
                    attribute.Database.PISystem.DataReferencePlugIns[dataReference];
            if (!string.IsNullOrEmpty(dataReference)) attribute.ConfigString = configString;
            return attribute;
        }

        public static List<AFEventFrame> IncludeChildren(this List<AFEventFrame> parents)
        {
            var result = new List<AFEventFrame>();
            var stack = new Stack<AFEventFrame>();
            parents.ForEach(n => stack.Push(n));
            while (stack.Count > 0)
            {
                var currentFrame = stack.Pop();
                result.Add(currentFrame);
                currentFrame.EventFrames?.ToList().ForEach(n => stack.Push(n));
            }
            return result;
        }
    }
}