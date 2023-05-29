#region using section

using System;
using AFMaster.Util;
using OSIsoft.AF;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        public class Category
        {
            public static AFCategory GetElementCategory(string name)
            {
                try
                {
                    var cat = Connector.GetAFDatabase().ElementCategories[name];
                    return cat;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static AFCategory CreateElementCategory(string name)
            {
                try
                {
                    var cat = new AFCategory(name);
                    Connector.GetAFDatabase().ElementCategories.Add(cat);
                    return cat;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static AFCategory GetOrCreateElementCategory(string name)
            {
                return GetElementCategory(name) ?? CreateElementCategory(name);
            }

            public static AFCategory GetAttributeCategory(string name)
            {
                try
                {
                    var cat = Connector.GetAFDatabase().ElementCategories[name];
                    return cat;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static AFCategory GetOrCreateAttributeCategory(string name)
            {
                return GetAttributeCategory(name) ?? CreateAttributeCategory(name);
            }

            public static AFCategory CreateAttributeCategory(string name)
            {
                try
                {
                    var cat = new AFCategory(name);
                    Connector.GetAFDatabase().AttributeCategories.Add(cat);
                    return cat;
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