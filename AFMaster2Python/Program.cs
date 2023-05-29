using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using AFMaster;
using AFMaster.Util;

namespace AFMaster2R
{
    class Program
    {
        static void Main()
        {
            // CreateRFunctions();
            CreatePythonFunctions();
        }
        static void CreateHelp(StreamWriter sw, MethodInfo method)
        {
            sw.WriteLine("#");
            sw.WriteLine("# @author " + "Ernst Holger Amort");
            sw.WriteLine(@"# @references \url{mailto:holgeramort@gmail.com}");
            sw.WriteLine("# @keywords OSIsoft, PI, AF, EF, realtime, PI, heterogenous, time series");
            sw.WriteLine("#");
            sw.WriteLine("#");

            if (true)
            {
                return;
            }

            var attribute = method.GetCustomAttributes().ToList().Select(n => (n as Method)).
                    First(m => m != null);

            var input = method.GetParameters();
            var result = method.ReturnParameter;
            var resultProperties = method.ReturnType.GetProperties();

            sw.WriteLine("          " + "'''");
            if (!String.IsNullOrEmpty(attribute.Description)) sw.WriteLine("    " + attribute.Description);
            else sw.WriteLine(" " + "add description ...");
            sw.WriteLine("#");
            sw.WriteLine("#' @author " + "Ernst Holger Amort");
            sw.WriteLine(@"#' @references \url{mailto:hamprt@tqsintegration.com}");
            sw.WriteLine("#' @keywords OSIsoft, PI, AF, EF, realtime, PI, heterogenous, time series");
            sw.WriteLine("#");
            sw.WriteLine("#");
            sw.WriteLine("      Parameters:");
            foreach (var n in input.ToList())
            {
                if (n.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (n.GetCustomAttributes(typeof(Parameter), false).Length > 0)
                {
                    var parameterAttribute = n.GetCustomAttributes().ToList().Select(m =>
                    (m as Parameter)).First(p => p != null);
                    sw.WriteLine("          " + UppercaseFirst(n.Name) + " " + parameterAttribute.Description);
                }
                else
                {
                    sw.WriteLine("          " + UppercaseFirst(n.Name) + " input variable");
                }
            }
            sw.WriteLine("");
            sw.WriteLine("      Returns:");
            if (method.ReturnType.FullName.Contains("System"))
                sw.WriteLine("          " + method.ReturnType.Name);
            else
                foreach (PropertyInfo t in resultProperties) sw.WriteLine("         " + t.Name);

            //sw.WriteLine("#' @export");
            sw.WriteLine("          " + "'''");

        }
        static void CreatePythonFunctions()
        {
            var path = @"C:\Repos\AFMaster\AFMaster\bin\Debug\";
 
            Library library = new Library();
            Assembly assembly = Assembly.Load("AFMaster");
            //Console.WriteLine(assembly.FullName);

            MethodInfo[] methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(m => m.GetCustomAttributes(typeof(Method), false).Length > 0)
                .ToArray();

            var fileInfo = new FileInfo(path + "PIFrames.py");
            StreamWriter sw = fileInfo.CreateText();
            sw.WriteLine("import pandas as pd");
            // sw.WriteLine("from pipanda import PIPanda");
            sw.WriteLine("import clr");
            sw.WriteLine("import collections");
            sw.WriteLine("import six");
            //sw.WriteLine("import pandasnet");
            sw.WriteLine("import sys");
            sw.WriteLine("from datetime import datetime");
            sw.WriteLine(" ");
            sw.WriteLine(@"sys.path.append(r'AFMaster.dll')");
            sw.WriteLine("library = clr.AddReference('AFMaster')");
            sw.WriteLine("from AFMaster import Library # internal dll name");
            sw.WriteLine("from AFMaster import Model # internal dll name");

            sw.WriteLine("class PIforPandas:");
            sw.WriteLine("    def __init__(self):");
            sw.WriteLine("        self.library=Library()");
            sw.WriteLine("        self.connector=self.library.Connector");
            sw.WriteLine(" ");
            sw.WriteLine("    def ConnectToAFandPI(self) :");
            sw.WriteLine("        connection =self.connector.ConnectToDefaultPI()");
            sw.WriteLine("        return self.connector.ConnectToDefaultAF()");
            sw.WriteLine(" ");
            // decoder to pandas
            sw.WriteLine("def ConvertCSharpToDataFrame(raw):");
            sw.WriteLine("    dict = { }");
            sw.WriteLine("    if raw is None:");
            sw.WriteLine("        return None");
            sw.WriteLine("    for key in raw.Keys:");
            sw.WriteLine("        if (isinstance(raw[key], collections.Iterable)");
            sw.WriteLine("            and not isinstance(raw[key], six.string_types)):");
            sw.WriteLine("            dict[key] = list(raw[key])");
            sw.WriteLine("        else:");
            sw.WriteLine("            dict[key] = raw[key]");
            sw.WriteLine("    try:");
            sw.WriteLine("        df = pd.DataFrame.from_dict(dict)");
            sw.WriteLine("    except:");
            sw.WriteLine("        return dict");
            sw.WriteLine("    if 'UTCSeconds' in df:");
            sw.WriteLine("        df['DateTime'] =[datetime.fromtimestamp(float(value)) for value in df.UTCSeconds.values]");
            sw.WriteLine("        df.set_index('DateTime', inplace = True)");
            sw.WriteLine("    return df");
            sw.WriteLine(" ");
            // __________
            foreach (MethodInfo method in methods)
            {
                Method attribute = method.GetCustomAttributes().ToList().Select(n => (n as Method)).
                    First(m => m != null);
                var methodName = method.Name;
                //.Replace("AsModel","");
                // make unique
                // var fileInfo = MakeUnique(path + methodName + ".py");


                var callingClass = method.DeclaringType.FullName;

                var input = method.GetParameters();
                var result = method.ReturnParameter;
                var resultProperties = method.ReturnType.GetProperties();

                var tab = "      ";


                var inputString = String.Join(",", input.Select(n => UppercaseFirst(n.Name)));
                var isFirstVaraible = true;
                //sw.WriteLine(methodName + " <- function(");

                sw.Write("def " + methodName + "(");
                foreach (ParameterInfo info in input)
                {
                    if (info.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (isFirstVaraible)
                    {
                        isFirstVaraible = false;
                        sw.Write(UppercaseFirst(info.Name) + "");
                    }
                    else
                        sw.Write(", " + UppercaseFirst(info.Name) + "");
                }
                sw.WriteLine("):");
                // help\documentation
                CreateHelp(sw, method);

                sw.WriteLine("# _______________________________________________");
                sw.WriteLine(tab + "__piforpandas=PIforPandas()");

                Console.WriteLine(methodName);
                if (methodName == "GetTimesMultiplePointValues")
                {
                }
                sw.WriteLine(tab + "model = " + callingClass.Replace("AFMaster.Library+", "__piforpandas.library.") + "."
                    + methodName + "(");
                //sw.WriteLine(tab + ", '" + methodName + "'");

                bool isFirst = true;
                for (int index = 0; index < input.Length; index++)
                {
                    ParameterInfo info = input[index];
                    if (index == 0)
                    {
                        if (info.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                            sw.WriteLine(tab + "True" + "");
                        else
                            sw.WriteLine(tab + UppercaseFirst(info.Name));
                    }
                    else
                    {
                        if (info.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                            sw.WriteLine(tab + ", True" + "");
                        else
                            sw.WriteLine(tab + ", " + UppercaseFirst(info.Name));
                    }
                }
                sw.WriteLine(tab + ")");


                if (!method.ReturnType.FullName.Contains("System"))
                {
                    sw.WriteLine(tab + "if model == None: return None");
                    sw.WriteLine(tab + "df = pd.DataFrame()");
                    foreach (PropertyInfo t in resultProperties)
                    {
                        Console.WriteLine(method.ReturnType.FullName);
                        if (method.ReturnType.FullName == "AFMaster.Model+ModelTable")
                        {
                            sw.WriteLine(tab + "cols = list(model.Columns)");
                            sw.WriteLine(tab + "for index in range(len(cols)):");
                            sw.WriteLine(tab + tab + "df[cols[index]] = list(model.Data[index])");
                            break;
                        }
                        else if (t.PropertyType.IsArray)
                        {
                            sw.WriteLine(tab + "df['" + t.Name + "'] = pd.Series(list(model." + t.Name + "))");
                        }
                        else
                        {
                            sw.Write(tab + "df['" + t.Name + "'] = [");
                            sw.WriteLine("model." + t.Name + "]");
                        }

                    }
                    // some code to make data frame
                    //sw.WriteLine(tab + "if 'UTCSeconds' in df:");
                    //sw.WriteLine(tab + tab + "df['Time']=pd.to_datetime(df.UTCSeconds, unit='s')");
                    //sw.WriteLine(tab + tab + "df.drop('TimeStamp', axis = 1, inplace = True)");
                    //sw.WriteLine(tab + tab + "df.set_index(['Time'])");
                    sw.WriteLine(tab + "return df");
                }
                else if (method.ReturnType.FullName == "System.String[]")
                {
                    sw.WriteLine(tab + "return list(model)");

                }
                else if (method.ReturnType.FullName.StartsWith("System.Collection"))
                {
                    sw.WriteLine(tab + "return ConvertCSharpToDataFrame(model)");
                }
                else
                {
                    Console.WriteLine(method.ReturnType.FullName);
                    sw.WriteLine(tab + "return model");
                    
                }

                // 
                if (method.ReturnType.Name == "DataVector")
                {
                    var i = 1;
                }



                sw.WriteLine("# _______________________________________________");
                sw.WriteLine(" ");
                // remove
                //sw.WriteLine("________________________________________________");

            }
            // remove
            sw.Close();
            Console.WriteLine("bye ...");
            Console.ReadLine();
            Thread.Sleep(1500);

        }
        static void CreateRFunctions()
        {
            var path = @"C:\Users\Holger\Documents\ROSIsoft\R\";
            //path = @"C:\Users\Holger\Documents\Temp\";

            Library function = new Library();
            Assembly assembly = Assembly.Load("AFMaster");
            //Console.WriteLine(assembly.FullName);

            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(m => m.GetCustomAttributes(typeof(Method), false).Length > 0)
                .ToArray();
            // remove
            //var fileInfo = MakeUnique(path + "AllMethods" + ".R");
            //StreamWriter sw = fileInfo.CreateText();
            // __________
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttributes().ToList().Select(n => (n as Method)).
                    First(m => m != null);
                var methodName = method.Name;
                //.Replace("AsModel","");
                // make unique
                var fileInfo = MakeUnique(path + methodName + ".R");
                StreamWriter sw = fileInfo.CreateText();

                var callingClass = method.DeclaringType.FullName;

                var input = method.GetParameters();
                var result = method.ReturnParameter;
                var resultProperties = method.ReturnType.GetProperties();
                // help\documentation
                if (!String.IsNullOrEmpty(attribute.Description)) sw.WriteLine("#' " + attribute.Description);
                else sw.WriteLine("#' " + "add description ...");
                sw.WriteLine("#");
                sw.WriteLine("#' @author " + "Ernst Holger Amort");
                sw.WriteLine(@"#' @references \url{mailto:holgeramort@gmail.com}");
                sw.WriteLine("#' @keywords OSIsoft, PI, AF, EF, realtime, PI, heterogenous, time series");
                sw.WriteLine("#");
                foreach (var n in input.ToList())
                {
                    if (n.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (n.GetCustomAttributes(typeof(Parameter), false).Length > 0)
                    {
                        var parameterAttribute = n.GetCustomAttributes().ToList().Select(m =>
                        (m as Parameter)).First(p => p != null);
                        sw.WriteLine("#' @param " + UppercaseFirst(n.Name) + " " + parameterAttribute.Description);
                    }
                    else
                    {
                        sw.WriteLine("#' @param " + UppercaseFirst(n.Name) + " input variable");
                    }
                }
                sw.WriteLine("#");
                sw.WriteLine("# @return data.frame with the following columns:");
                if (method.ReturnType.FullName.Contains("System"))
                    sw.WriteLine("#  " + method.ReturnType.Name);
                else
                    foreach (PropertyInfo t in resultProperties) sw.WriteLine("#  " + t.Name);

                sw.WriteLine("#' @export");
                var tab = "      ";
                var inputString = String.Join(",", input.Select(n => UppercaseFirst(n.Name)));
                var isFirstVaraible = true;
                sw.WriteLine(methodName + " <- function(");
                foreach (ParameterInfo info in input)
                {
                    if (info.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (isFirstVaraible)
                    {
                        isFirstVaraible = false;
                        sw.WriteLine(tab + UppercaseFirst(info.Name) + "");
                    }
                    else
                        sw.WriteLine(tab + ", " + UppercaseFirst(info.Name) + "");
                }
                sw.WriteLine(tab + ")");
                sw.WriteLine("{");


                sw.WriteLine(tab + "Data.Model <- clrCallStatic('" + callingClass + "'");
                sw.WriteLine(tab + ", '" + methodName + "'");

                foreach (ParameterInfo info in input)
                {
                    if (info.Name.Equals("asModel", StringComparison.InvariantCultureIgnoreCase))
                        sw.WriteLine(tab + ", TRUE" + "");
                    else
                        sw.WriteLine(tab + ", " + UppercaseFirst(info.Name) + "");
                }
                sw.WriteLine(tab + ")");

                if (!method.ReturnType.FullName.Contains("System"))
                {
                    foreach (PropertyInfo t in resultProperties)
                    {
                        sw.Write(tab + t.Name + " <- ");
                        sw.WriteLine("clrGet(Data.Model, '" + t.Name + "')");
                    }
                }
                // 
                if (method.ReturnType.Name == "DataVector")
                {
                    var i = 1;
                }
                sw.WriteLine(tab + "data.frame(check.rows = FALSE");
                if (!method.ReturnType.FullName.Contains("System"))
                {
                    foreach (PropertyInfo t in resultProperties)
                        sw.WriteLine(tab + ", " + t.Name);
                }
                else
                {
                    sw.WriteLine(tab + ", Values = Data.Model");
                }

                sw.WriteLine(tab + ")");
                sw.WriteLine("}");
                // remove
                //sw.WriteLine("________________________________________________");
                sw.Close();
            }
            // remove

            Console.WriteLine("bye ...");
            Thread.Sleep(1500);

        }
        static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return s.ToLower();
            //return char.ToUpper(s[0]) + s.Substring(1);
        }
        static void CreateUniteTests()
        {
            List<string> header = new List<string>
            {
                "using System;",
                "using AFMaster;",
                "using Microsoft.VisualStudio.TestTools.UnitTesting;",
                "namespace LibraryTesting",
                "{",
                "",
                "[TestClass]",
                "public class UnitTest",
                "{",
                @"   private TestContext testContextInstance;",
                @"   private string AFServerName = ""BeastServer"";",
                @"   private string AFDatabase = ""UnitTest""; ",
                @"   private string PIServer = ""BeastServer"";",
                "private AFMaster.Function function;",
                "public UnitTest()",
                "{",
                "    function = new Function();",
                "    Function.Connector.ConnectToAF(AFServerName,",
                @"        AFDatabase,""BeastServer\\Holger"", ""herbst22"");",
                "    Function.Connector.ConnectToPI(PIServer,",
                @"    ""BeastServer\\Holger"", ""herbst22"");",
            "}"
            };
            var path = @"C:\Source\AFMaster\AFMaster\LibraryTesting\UnitTest.cs";
            var fileInfo = new FileInfo(path);
            StreamWriter sw = fileInfo.CreateText();
            header.ForEach(n => sw.WriteLine(n));
            Library library = new Library();

            Assembly assembly = Assembly.Load("AFMaster, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null");

            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)).
                Where(t => t.Name.Contains("AsModel")).ToArray();
            methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(Method), false).Length > 0)
                .ToArray();
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttributes().ToList().Select(n => (n as Method)).
                    First(m => m != null);
                var methodName = method.Name; //.Replace("AsModel","");
                                              // make unique

                var callingClass = method.DeclaringType.FullName;

                var input = method.GetParameters().ToList();
                var result = method.ReturnParameter;
                var resultProperties = method.ReturnType.GetProperties();
                var inputString = String.Join(",", input
                    .Select(n => n.Name));

                sw.WriteLine("[TestMethod]");
                sw.WriteLine("public void UnitTest" +
                             methodName + " (" + "){");
                input.ForEach(n =>
                    sw.WriteLine(n.ParameterType.Name + " " +
                                 n.Name +
                                 " = default(" + n.ParameterType + ")" +
                                 ";"));
                sw.WriteLine("var test = " +
                             method.DeclaringType.FullName
                             .Replace('+', '.') + "." +
                             method.Name +
                             " (" + inputString + ");");

                sw.WriteLine("Assert.Fail();");


                sw.WriteLine("");
                sw.WriteLine("}");

            }
            sw.WriteLine("}");
            sw.WriteLine("}");
            sw.Close();
            Console.ReadLine();

        }

        public static FileInfo MakeUnique(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fileExt = Path.GetExtension(path);

            for (int i = 1; ; ++i)
            {
                if (!File.Exists(path))
                    return new FileInfo(path);

                path = Path.Combine(dir, fileName + " " + i + fileExt);
            }
        }
    }
}
