using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Modeling;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;

namespace AFMaster
{
    public partial class Library
    {
        public class Transfer
        {
            [Method(Description = "Set Transfer ...")]
            public static bool SetTransfer(
                [Parameter("Name")] string name,
                [Parameter("Description")] string description,
                [Parameter("Source Id")] string sourceId,
                [Parameter("Destination Id")] string destinationId,
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime,
                [Parameter("Batch Id")] string batchId,
                [Parameter("Total")] double total
                )
            {
                var transfer = new AFTransfer(Connector.GetAFDatabase(), name);
                AFElement element0, element1;
                if (Guid.TryParse(sourceId, out var _))
                    element0 = Element.GetElements("", sourceId, "", "").FirstOrDefault();
                else
                    element0 = Element.GetElements( sourceId, "", "", "").FirstOrDefault();

                if (Guid.TryParse(destinationId, out var _))
                    element1 = Element.GetElements("", destinationId, "", "").FirstOrDefault();
                else
                    element1 = Element.GetElements( destinationId, "", "", "").FirstOrDefault();

                transfer.SetSource(element0.Ports["Out"]);
                transfer.SetDestination(element1.Ports["In"]);
                transfer.SetStartTime(startTime.ToAFTime());
                transfer.SetEndTime(endTime.ToAFTime());
                transfer.Name = name;
                transfer.Description = description;
                transfer.Attributes.Add("BatchId").Configure("Source Batch Id", "System.String", null, null);
                transfer.Attributes.Add("Total").Configure("Transfer Total Weight or Volume", "System.Double", null, null);
                transfer.Attributes["BatchId"].Data.UpdateValue(new AFValue(null, batchId, startTime.ToAFTime()),AFUpdateOption.Insert);
                transfer.Attributes["Total"].Data.UpdateValue(new AFValue(null, total, startTime.ToAFTime()), AFUpdateOption.Insert);

                transfer.CheckIn();
                return true;
            }

            public static List<AFTransfer> GetTransfers(string name, string id, string category, string template,
                string source, string destination, object start, object end)
            {
                List<AFTransfer> transfers = new List<AFTransfer>();
                string query = "";
                if (!string.IsNullOrEmpty(name)) { query += $" Name:'{name}'"; }
                if (!string.IsNullOrEmpty(id)) { query += $" ID:'{id}'"; }
                if (!string.IsNullOrEmpty(category)) { query += $" Category:'{category}'"; }
                if (!string.IsNullOrEmpty(template)) { query += $" Template:'{template}'"; }
                if (!string.IsNullOrEmpty(source)) { query += $" Source:'{source}'"; }
                if (!string.IsNullOrEmpty(destination)) { query += $" Destination:'{destination}'"; }
                if (!string.IsNullOrEmpty(start.ToString())) { query += $" Start:>='{start.ToStringTime()}'"; }
                if (!string.IsNullOrEmpty(end.ToString())) { query += $" End:<='{end.ToStringTime()}'"; }

                using (var search =
                    new AFTransferSearch(Connector.GetAFDatabase(),
                        "Get Transfers", query))
                {
                    search.CacheTimeout = TimeSpan.FromMinutes(10);
                    transfers.AddRange(search.FindObjects(fullLoad: true));
                }
                return transfers;
            }
            [Method(Description = "Get Transfers ...")]
            public static Dictionary<string, object> GetTransfers(
                [Parameter("Name")] string name,
                [Parameter("Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("Template")] string source,
                [Parameter("Template")] string destination,
                [Parameter("Start Time")] object start,
                [Parameter("End Time")] object end,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    return
                        Conversion.CreateDictionary(
                            GetTransfers(name, id, category, template,source,destination, start, end));
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
