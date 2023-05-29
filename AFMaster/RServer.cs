using OSIsoft.AF.Asset;

namespace AFMaster
{
    public class RServer
    {
        public static void CreateAttributes(string category, string[] childAttributes)
        {
           var attributes= Library.Attribute.GetAttributes("element","","",category,"");
           // create PI points
           
           // create AF structure
           foreach (AFAttribute attribute in attributes)
            {
                foreach (string childAttribute in childAttributes)
                {
                    var pointName = attribute.GetPath().
                        Replace("\\\\","").
                        Replace("\\",".").
                        Replace("|", ".")+"." + childAttribute+".R";
                    Library.Point.CreateCalculationTag(pointName);
                    //Library.Attribute.CreateAttributeInAttribute(attribute.GetPath(),
                    //    childAttribute, attribute.Name + " - " + childAttribute,
                    //    "System.Double", "PI Point", @"\\BeastServer\Sinusoid1H",true);
                }
                
            }
        }
        
    }
}
