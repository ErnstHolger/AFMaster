#region using section

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.EventFrame;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;

#endregion

namespace AFMaster
{
    public partial class Library
    {
        //[ErrorAspect]
        public class Frame
        {
            [Method(Description = "Delete ALL Frames ...")]
            public static bool DeleteAllFrames(
                [Parameter("Start Time")] object startTime,
                [Parameter("End Time")] object endTime)
            {
                var queryString = "AllDescendants:True";
                var deleteTally = 0;
                using (AFEventFrameSearch eventFrameSearch =
                        new AFEventFrameSearch(Connector.GetAFDatabase(), "search1",
                AFSearchMode.StartInclusive, startTime.ToAFTime(),
                endTime.ToAFTime(), queryString))

                {
                    eventFrameSearch.CacheTimeout = TimeSpan.FromMinutes(10);
                    IEnumerable<AFEventFrame> eventFrameResults =
                        eventFrameSearch.FindEventFrames(0, false, 0);
                    foreach (AFEventFrame f in eventFrameResults)
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch { }

                        if (++deleteTally < 1000) continue;
                        deleteTally = 0;
                        Connector.GetAFDatabase().CheckIn();
                    }
                }

                if (deleteTally > 0)
                    Connector.GetAFDatabase().CheckIn();

                return true;
            }

            [Method(Description = "Delete Frames ...")]
            public static bool DeleteFrames(
                    [Parameter("Array of Ids")] string[] ids)
            {
                foreach (var id in ids)
                {
                    try
                    {
                        var frames = GetFrames("", id, "", "", "", "","");
                        // double check name
                        if (frames == null) continue;

                        frames[0].Delete();
                        Connector.GetAFDatabase().CheckIn();
                    }
                    catch (Exception ex)
                    {
                        SimpleLog.Error(ex.Message);
                    }
                }
                return true;
            }
            [Method(Description = "Listen to Frame Events ...")]
            public static Model.FrameEvent GetChangedFrames(
                [Parameter("Duration in Seconds")] double durationInSeconds)
            {
                object dbCookie = null;
                var startTime = DateTime.Now;
                var allChanges = new List<AFChangeInfo>();
                var allFrames = new List<AFEventFrame>();
                var localTimes = new List<DateTime>();
                while (DateTime.Now < startTime + TimeSpan.FromSeconds(durationInSeconds))
                {
                    var tempNow = DateTime.Now;
                    var temp = Connector.GetAFDatabase()
                        .FindChangedItems(AFIdentity.EventFrame, false, int.MaxValue, dbCookie, out dbCookie)
                        .ToList();
                    Thread.Sleep(250);
                    foreach (var afChangeInfo in temp)
                    {
                        allChanges.Add(afChangeInfo);
                        var frames = Frame.GetFrames("", afChangeInfo.ID.ToString(), "", "", "", "","");
                        if (frames.Count > 0)
                            allFrames.Add(frames[0]);
                        localTimes.Add(tempNow);
                    }
                }
                var frameEvent = new Model.FrameEvent(allChanges.Count);
                for (var index = 0; index < allChanges.Count; index++)
                {
                    frameEvent.LocalTime[index] = new DateTimeOffset(localTimes[index]).ToUnixTimeMilliseconds() / 1000.0;
                    // ____________________________
                    frameEvent.QueueTime[index] = allChanges[index].ChangeTime.UtcSeconds;
                    frameEvent.Action[index] = allChanges[index].Action.ToString();
                    // ____________________________
                    if (allFrames[index] != null)
                    {
                        frameEvent.StartTime[index] = allFrames[index].StartTime.UtcSeconds;
                        frameEvent.EndTime[index] = allFrames[index].EndTime.UtcSeconds;
                        frameEvent.Id[index] = allFrames[index].ID.ToString();
                        frameEvent.Name[index] = allFrames[index].Name;
                        if (allFrames[index].ReferencedElements.Count > 0)
                            frameEvent.Element[index] =
                                allFrames[index].ReferencedElements[0].Name;
                        if (allFrames[index].Template != null)
                            frameEvent.Template[index] =
                                allFrames[index].Template.Name;
                    }
                    else
                    {
                        frameEvent.StartTime[index] = frameEvent.EndTime[index] = Double.NaN;
                        frameEvent.Id[index] = frameEvent.Name[index] = "n/a/";
                    }
                }
                return frameEvent;
            }

            public static AFEventFrame SetFrame(string name, string description,
                object startTime, object endTime, string templateName = "",
                string elementtId = "", string parentId = "")
            {
                var frame = new AFEventFrame(Connector.GetAFDatabase(), name);
                frame.SetStartTime(startTime.ToAFTime());
                frame.SetEndTime(endTime.ToAFTime());
                frame.Description = description;

                if (!string.IsNullOrEmpty(templateName))
                {
                    var template = GetByPath.GetElementTemplateByName(templateName);
                    frame.Template = template;
                }
                if (!string.IsNullOrEmpty(elementtId))
                {
                    AFElement element;
                    if (Guid.TryParse(elementtId, out var _))
                        element = Element.GetElements("", elementtId, "", "").FirstOrDefault();
                    else
                        element = Element.GetElements(elementtId, "", "", "").FirstOrDefault();

                    if (element!=null)
                        frame.PrimaryReferencedElement = element;
                }
                if (!string.IsNullOrEmpty(parentId))
                {
                    var frames = Frame.GetFrames("", parentId, "", "", "", "","");
                    if (frames.Count > 0)
                        frames[0].EventFrames.Add(frame);
                }
                frame.CheckIn();
                Connector.GetAFDatabase().CheckIn();
                return frame;
            }

            [Method(Description = "Set Frame ...")]
            public static Dictionary<string,object> SetFrame(
                [Parameter("Frame Name")] string name,
                [Parameter("Fram Description")] string description,
                [Parameter("Frame Start Time")] object startTime,
                [Parameter("Frame End Time")] object endTime,
                [Parameter("Template Guid")] string templateGuid = "",
                [Parameter("Element Guid")] string elementGuid = "",
                [Parameter("Parent Guid")] string parentGuid = "",
                [Parameter("As Model")] bool asModel = false)
            {
                try
                {
                    //Debugger.Launch();
                    return Conversion.CreateDictionary(new List<AFEventFrame>
                    {
                        SetFrame(name, description, startTime,
                            endTime, templateGuid,
                            elementGuid, parentGuid)
                    });
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                    return null;
                }
            }

            public static IList<AFEventFrame> GetFrames(string name, object start, object end)
            {
                return GetFrames(name, "", "", "","", start, end);
            }

            public static IList<AFEventFrame> GetFrames(string name, string id, string category, string template,
                string elementId, object start, object end)
            {
                var frames = new List<AFEventFrame>();
                var query = "";
                if (!string.IsNullOrEmpty(name)) { query += $" Name:'{name}'"; }
                if (!string.IsNullOrEmpty(id)) { query += $" ID:'{id}'"; }
                if (!string.IsNullOrEmpty(category)) { query += $" Category:'{category}'"; }
                if (!string.IsNullOrEmpty(template)) { query += $" Template:'{template}'"; }
                if (!string.IsNullOrEmpty(elementId)) { query += $" ElementName:'{elementId}'"; }
                if (!string.IsNullOrEmpty(start.ToString())) { query += $" Start:>='{start.ToStringTime()}'"; }
                if (!string.IsNullOrEmpty(end.ToString())) { query += $" End:<='{end.ToStringTime()}'"; }

                using (var search =
                    new AFEventFrameSearch(Connector.GetAFDatabase(),
                        "Get Frames", query))
                {
                    search.CacheTimeout = TimeSpan.FromMinutes(10);
                    frames.AddRange(search.FindObjects(fullLoad: true));
                }
                return frames;
            }

            [Method(Description = "Get Frames ...")]
            public static Dictionary<string,object> GetFrames(
                [Parameter("Name")] string name,
                [Parameter("Id")] string id,
                [Parameter("Category")] string category,
                [Parameter("Template")] string template,
                [Parameter("ElementId")] string elementId,
                [Parameter("Start Time")] object start,
                [Parameter("End Time")] object end,
                [Parameter("Dummy Parameter")] bool asModel)
            {
                try
                {
                    return
                        Conversion.CreateDictionary(
                            GetFrames(name, id, category, template, elementId, start, end));
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

        }
    }
}