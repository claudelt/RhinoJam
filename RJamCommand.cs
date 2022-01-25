using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

using RJam.Network;

namespace RJam
{
    public class RJamCommand : Command
    {
        public RJamCommand()
        {
            Instance = this;
        }

        public static RJamCommand Instance { get; private set; }

        public override string EnglishName => "RJam";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Test

            return Result.Nothing;
        }
    }
}
