////using PostSharp.Aspects;

//using System;
//using System.Reflection;
//using AFMaster.Util;


//namespace AFMaster
//{
//    [Serializable]
//    public class ErrorAspect : OnMethodBoundaryAspect
//    {
//        public override void RuntimeInitialize(MethodBase method)
//        {
//            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
//            SimpleLog.SetLogFile(logDir: path, prefix: "AFMasterLog_", writeText: false);
//            base.RuntimeInitialize(method);
//        }

//        public override void OnSuccess(MethodExecutionArgs args)
//        {
//            SimpleLog.Info("sucess: " + args.Method.Name);
//            base.OnSuccess(args);
//        }

//        public override void OnEntry(MethodExecutionArgs args)
//        {
//            // do nothing
//        }

//        public override void OnException(MethodExecutionArgs args)
//        {
//            SimpleLog.Error(args.Exception.Message);
//            args.FlowBehavior = FlowBehavior.Return;

//        }
//    }
//}

