using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

using RJam.Data;

namespace RJam
{
    public class RJamCommandTestB : Command
    {
        public RJamCommandTestB()
        {
            Instance = this;
        }

        public static RJamCommandTestB Instance { get; private set; }

        public override string EnglishName => "RJamStartForDocument";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Starting RJam for " + doc.Name);

            int port = -1;

            while (!(port >= 1 && port <= 65535))
            {
                GetInteger getter = new GetInteger();
                getter.AcceptNothing(false);
                getter.SetCommandPrompt("Type a port number from 1 to 65535");
                getter.Get();
                port = getter.Number();
            }

            RJamPlugin.Instance.StartHost(doc, port);

            RhinoApp.WriteLine("RJam hould be started for " + doc.Name);

            return Result.Success;
        }
    }
}