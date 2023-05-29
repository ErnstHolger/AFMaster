#region using section

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AFMaster.Util;
using OSIsoft.AF;
using OSIsoft.AF.PI;

#endregion

namespace AFMaster
{
    //[ErrorAspect]

    public partial class Library
    {
        public static Connector AFConnector;

        public Library()
        {
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                var path = Environment.CurrentDirectory;
                SimpleLog.SetLogFile(path, "AFMasterLog_", writeText: false);
                SimpleLog.Info("log started ...");
            }
            catch (Exception ex)
            {
                SimpleLog.Error(ex.Message);
            }
            try
            {
                AFConnector = new Connector();
            }
            catch (Exception ex)
            {
                SimpleLog.Error(ex.Message);
            }
        }

        public class Connector
        {
            public Connector()
            {
                SetObjects();
            }

            // add SQL connections ------
            // public static SqlConnection SelectedSqlConnection { get; set; }

            // --------------------------
            public static PISystems PISystems { get; set; }
            public static PIServers PIServers { get; set; }
            public static PIServer DefaultPIServer { get; set; }
            public static PISystem DefaultAFServer { get; set; }
            public static AFDatabase DefaultAFDatabase { get; set; }
            public static PIServer SelectedPIServer { get; private set; }
            public static PISystem SelectedAFServer { get; private set; }
            public static AFDatabase SelectedAFDatabase { get; private set; }
            public static int MaxItemReturn { get; private set; }

            private static void SetObjects()
            {
                try
                {
                    PISystems = new PISystems();
                    //todo: piSystems.SetApplicationIdentity();
                    PIServers = new PIServers();
                    DefaultPIServer = PIServers.DefaultPIServer;
                    DefaultAFServer = PISystems.DefaultPISystem;
                    MaxItemReturn = 500;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
            }

 

            [Method(Description = "Connect to AF with Prompt ...")]
            public static Dictionary<string, object> ConnectToAFWithPrompt(
                [Parameter("AF Server Name")] string afServer,
                [Parameter("AF Database")]string afDatabase)
            {
                // will always call Disconnect!
                // see https://pisquare.osisoft.com/message/30604#30604
                try
                {
                    SetAFServer(afServer);
                    //GetAFServer().Disconnect();
                    if (!GetAFServer().ConnectionInfo.IsConnected)
                        GetAFServer().ConnectWithPrompt(null, AFConnectionPreference.PreferPrimary);
                    var connection = SetAFDatabase(afDatabase);
                    DefaultAFDatabase = DefaultAFServer.Databases.DefaultDatabase;
                    //SetObjects();
                    return connection;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return null;
            }

            private static NetworkCredential CreateNetworkCredential(string piLogin, string piPassword)
            {
                // check if login is domain login
                var result = piLogin.Split('\\');
                return result.Length == 1
                    ? new NetworkCredential(result[0], piPassword)
                    : new NetworkCredential(result[1], piPassword, result[0]);
            }

            [Method(Description = "Connect to AF with User Name and Password ...")]
            public static Dictionary<string, object> ConnectToAF(
                [Parameter("AF Server Name")]string afServer,
                [Parameter("AF Database")]string afDatabase,
                [Parameter("Active Directory User Name")]string userName,
                [Parameter("Password")]string passWord)
            {
                try
                {
                    SetAFServer(afServer);
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(passWord))
                    {
                        var credential = CreateNetworkCredential(userName, passWord);
                        if (!GetAFServer().ConnectionInfo.IsConnected)
                            GetAFServer().Connect(credential, AFConnectionPreference.PreferPrimary
                            );
                    }
                    else
                    {
                        GetAFServer().Connect();
                    }

                    var connection = SetAFDatabase(afDatabase);
                    DefaultAFDatabase = DefaultAFServer.Databases.DefaultDatabase;
                    return connection;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return null;
            }

            [Method(Description = "Connect to Default AF Server and Database ...")]
            public static Dictionary<string, object> ConnectToDefaultAF()
            {
                try
                {
                    SelectedAFServer = PISystems.DefaultPISystem;
                    GetAFServer().Connect();
                    DefaultAFDatabase = GetAFServer().Databases.DefaultDatabase;

                    var connection = GetConnection();
                    return connection;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return null;
            }

            [Method(Description = "Connect to Default PI Server ...")]
            public static Dictionary<string, object> ConnectToDefaultPI()
            {
                try
                {
                    //System.Diagnostics.Debugger.Launch();
                    SelectedPIServer = PIServers.DefaultPIServer;
                    GetPIServer().Connect();
                    return GetConnection();
                    ;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return null;
            }

            [Method(Description = "Connect to Default PI Server ...")]
            public static Dictionary<string, object> ConnectToDefaultAFandPI()
            {
                ConnectToDefaultAF();
                ConnectToDefaultPI();
                return GetConnection();
            }

            [Method(Description = "Connect to PI Server with User Name and Password ...")]
            public static Dictionary<string, object> ConnectToAFandPI(
                [Parameter("PI Server Name")]string piServer,
                [Parameter("PI User Name")]string userName,
                [Parameter("Password")]string passWord)
            {
                try
                {
                    SetPIServer(piServer);
                    //SecureString securePassword = Conversion.ConvertToSecureString(passWord);
                    var credential = CreateNetworkCredential(userName, passWord);
                    // check for collective
                    if (!string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(passWord))
                        GetPIServer().Connect(credential, AFConnectionPreference.PreferPrimary,
                            PIAuthenticationMode.WindowsAuthentication);
                    else
                        GetPIServer().Connect();
                    return GetConnection();
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return null;
            }

            [Method(Description = "Connect to PI Server with Prompt (TODO) ...")]
            public static Dictionary<string, object> ConnectToPIWithPrompt(
                [Parameter("PI Server Name")]string piServer)
            {
                SetPIServer(piServer);
                GetPIServer().ConnectWithPrompt(null);

                return GetConnection();
            }

            [Method(Description = "Connect to PI Server as Windows User ...")]
            public static Dictionary<string, object> ConnectToPI(
                [Parameter("PI Server Name")]string piServer,
                [Parameter("User Name")]string userName,
                [Parameter("Password")]string passWord,
                [Parameter("Authenticate as PI or Windows User")]bool asPIUser)
            {
                var credential = CreateNetworkCredential(userName, passWord);
                SetPIServer(piServer);
                //GetPIServer().Connect(credential, AFConnectionPreference.PreferPrimary,
                //    asPIUser ? PIAuthenticationMode.PIUserAuthentication : 
                //    PIAuthenticationMode.WindowsAuthentication);
                //GetPIServer().ConnectWithPrompt(null);
                _ = Library.Connector.ConnectToDefaultPI();
                _ = Library.Connector.ConnectToDefaultAF();
                return GetConnection();
            }
     
            [Method(Description = "Disconnect from AF and PI Server ...")]
            public static Dictionary<string, object> DisconnectAll()
            {
                DisconnectFromAF();
                DisconnectFromPI();
                return GetConnection();
            }

            [Method(Description = "Disconnect from AF Server ...")]
            public static Dictionary<string, object> DisconnectFromAF()
            {
                GetAFServer().Disconnect();
                return GetConnection();
            }

            [Method(Description = "Disconnect from PI Server ...")]
            public static Dictionary<string, object> DisconnectFromPI()
            {
                GetPIServer().Disconnect();
                return GetConnection();
            }

            [Method(Description = "Get the Connection Object ...")]
            public static Dictionary<string, object> GetConnection()
            {
                return Conversion.CreateConnection();
            }

            public static AFDatabase GetAFDatabase()
            {
                return SelectedAFDatabase ?? DefaultAFDatabase;
            }

            public IEnumerable<string> GetAFDatabaseNames()
            {
                return GetAFServer().Databases.Select(n => n.Name);
            }

            public IEnumerable<string> GetAFDatabaseGuids()
            {
                return GetAFServer().Databases.Select(n => n.UniqueID);
            }

            public static PISystem GetAFServer()
            {
                return SelectedAFServer ?? DefaultAFServer;
            }

            public static PIServer GetPIServer()
            {
                return SelectedPIServer ?? DefaultPIServer;
            }

            public static IEnumerable<string> GetAFServerGuids()
            {
                return PISystems.Select(n => n.UniqueID);
            }

            public static IEnumerable<string> GetAFServerNames()
            {
                return PISystems.Select(n => n.Name);
            }

            [Method(Description = "Get the Maximum Number of Items to Return ...")]
            public static int GetMaxItemReturn()
            {
                return MaxItemReturn;
            }

            public static IEnumerable<string> GetPIServerGuids()
            {
                return PIServers.Select(n => n.UniqueID);
            }

            public static IEnumerable<string> GetPIServerNames()
            {
                return PIServers.Select(n => n.Name);
            }

            public PIServers GetPIServers()
            {
                return PIServers;
            }

            [Method(Description = "Get the Connection Object ...")]
            public static Dictionary<string, object> GetPIServers(
                [Parameter("Dummy Parameter")]bool asModel)
            {
                return Conversion.CreateConnection();
            }

            [Method(Description = "Set the AF Database ...")]
            public static Dictionary<string, object> SetAFDatabase(
                [Parameter("AF Database Name")]string name)
            {
                try
                {
                    SelectedAFDatabase = GetAFServer().Databases[name];
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return Conversion.CreateConnection();
            }

            [Method(Description = "Set the AF Server ...")]
            public static Dictionary<string, object> SetAFServer(
                [Parameter("AF Server Name")]string name)
            {
                try
                {
                    SelectedAFServer = PISystems[name];
                    //DefaultAFDatabase = SelectedAFServer.Databases.DefaultDatabase;
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return Conversion.CreateConnection();
            }

            [Method(Description = "Set the Maximum Number of Items to Return ...")]
            public static Dictionary<string, object> SetMaxItemReturn(
                [Parameter("Maximum Number of Parameter")]double count)
            {
                try
                {
                    if (count <= 0) throw new ArgumentException("max item must be greater than zero ...");
                    MaxItemReturn = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return Conversion.CreateConnection();
            }

            [Method(Description = "Set the PI Server ...")]
            public static Dictionary<string, object> SetPIServer(
                [Parameter("PI Server Name")]string name)
            {
                try
                {
                    SelectedPIServer = PIServers[name];
                    SelectedPIServer.Connect(); // try to connect
                }
                catch (Exception ex)
                {
                    SimpleLog.Error(ex.Message);
                }
                return Conversion.CreateConnection();
            }
        }
    }
}