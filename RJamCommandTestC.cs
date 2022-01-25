using System;
using Rhino;
using Rhino.Commands;

namespace RJam
{
    public class RJamCommandTestC : Command
    {
        public RJamCommandTestC()
        {
            Instance = this;
        }

        public static RJamCommandTestC Instance { get; private set; }

        public override string EnglishName => "RJamStopForDocument";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Stopped RJam for " + doc.Name);
            RJamPlugin.Instance.StopHost(doc);

            if(RJamPlugin.Instance.HasHost(doc))
            {
                RhinoApp.WriteLine("RJam not stopped for " + doc.Name);
                return Result.Failure;
            }
            else
            {
                RhinoApp.WriteLine("RJam stopped for " + doc.Name);
                return Result.Success;
            }
        }
    }
}