using System;
using System.Collections.Generic;
using System.IO;

namespace K2V
{

    class Program
    {
        // DSP
        static readonly string[] dspBaseModules={
            "PC.NET",
            "PWM.NET",
            "DMA.NET",
            "PRAM.NET",
            "DRAM.NET",
            "ADDRESS.NET",
            "INSTRUCT.NET",
            "ALU.NET",
            "INTRUDE.NET"
        };

        // VIDEO HARDWARE
        static readonly string[] videoBaseModules={
            "CLOCK.NET",
            "IODEC.NET",
            "STAT.NET",
            "MEM.NET",
            "INT.NET",
            "VCNT.NET",
            "HCNT.NET",
            "BM.NET",
            "VTIM.NET",
            "PIX.NET"
        };

        // BLITTER
        static readonly string[] blitterDataModules={
            "SRCDATA.NET",
            "DSTDATA.NET",
            "PATDATA.NET",
            "COMP.NET",
            "LFU.NET"
        };
        static readonly string[] blitterStateModules={
            "STOUTER.NET",
            "STCMD.NET",
            "STPARAM.NET",
            "STINNER.NET",
            "STMEM.NET",
            "CMDREGS.NET",
            "INNERCNT.NET",
            "OUTERCNT.NET"
        };
        static readonly string[] blitterAddrModules={
            "DSTAREG.NET",
            "SRCAREG.NET",
            "PCAREG.NET",
            "ADDAMUX.NET",
            "STEPREG.NET",
            "ADDBSEL.NET",
            "ADDRADD.NET",
            "ADDROUT.NET"
        };

        static readonly string[] blitterModules={
            "DATA.NET",
            "ADDR.NET",
            "STATE.NET",
            "BUSCON.NET"
        };

        static readonly string[][] leafModules={
            videoBaseModules,
            blitterDataModules,
            blitterStateModules,
            blitterAddrModules,
            dspBaseModules
        };

        static readonly string[] videoModules = {
            "VID.NET"
        };

        static readonly string[] dspModules = {
            "DSP.NET"
        };

        static readonly string [][] layer1Modules={
            videoModules,
            blitterModules,
            dspModules
        };
        static readonly string [] blitterMain={
            "BLIT.NET"
        };

        static readonly string [][] layer2Modules={
            blitterMain
        };

        static string[] GetAllModulesFromMultiArray(string baseInputFolder, string[][] array)
        {
            var newlist = new List<string>();
            foreach(var l in array)
            {
                foreach (var m in l)
                {
                    newlist.Add(Path.Combine(baseInputFolder,m));
                }
            }

            return newlist.ToArray();
        }

        static string[] GetAllLeafModules(string inputFolder)
        {
            return GetAllModulesFromMultiArray(inputFolder, leafModules);
        }

        static string[] GetAllLayer1Modules(string inputFolder)
        {
            return GetAllModulesFromMultiArray(inputFolder, layer1Modules);
        }

        static string[] GetAllLayer2Modules(string inputFolder)
        {
            return GetAllModulesFromMultiArray(inputFolder, layer2Modules);
        }

        static void DoConvertModules(string inputFolder, string outputFolder, string[] children, string[] parents, IModification mods)
        {
            var includes = FetchIncludes(inputFolder, mods);

            List<Module> deps = new List<Module>();
            for (int i = 0; i < children.Length; i++)
            {
                string s = children[i];

                foreach (var c in Module.ParseFile(s,mods))
                {
                    c.RegisterModules(includes);
                    deps.Add(c);
                }
            }

            foreach (var m in parents)
            {
                var modules = Module.ParseFile(m,mods);
                foreach (var mod in modules)
                {
                    mod.RegisterModules(deps);
                    mod.RegisterModules(includes);
                    mod.RegisterModules(modules);
                    mod.Dump(Path.Combine(outputFolder, mod.FileName),mods);
                }
            }
        }

        static void DoLeafModules(string inputFolder, string outputFolder, IModification mods)
        {
            DoConvertModules(inputFolder, outputFolder, new string[] { }, GetAllLeafModules(inputFolder), mods);
        }

        static void DoLayer1Modules(string inputFolder, string outputFolder, IModification mods)
        {
            DoConvertModules(inputFolder, outputFolder, GetAllLeafModules(inputFolder), GetAllLayer1Modules(inputFolder), mods);
        }

        static void DoLayer2Modules(string inputFolder, string outputFolder, IModification mods)
        {
            DoConvertModules(inputFolder, outputFolder, GetAllLayer1Modules(inputFolder), GetAllLayer2Modules(inputFolder), mods);
        }

        static List<Module> FetchIncludes(string inputFolder, IModification mods)
        {
            var modules = Module.ParseFile(Path.Combine(inputFolder, "COUNTERS.NET"),mods);
            modules.AddRange(Module.ParseFile(Path.Combine(inputFolder, "LEGO.NET"), mods));
            modules.AddRange(Module.ParseFile(Path.Combine(inputFolder, "MACROS.NET"),mods));
            modules.AddRange(Module.ParseFile(Path.Combine(inputFolder, "QMACROS.NET"),mods));
            foreach (var m in modules)
            {
                m.RegisterModules(modules);
            }
            return modules;
        }

        static void DoIncludes(string inputFolder, string outputFolder, IModification mods)
        {
            var modules = FetchIncludes(inputFolder, mods);
            foreach (var m in modules)
            {
                m.Dump(Path.Combine(outputFolder, m.FileName),mods);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: K2V <input folder> <output folder>");
                return;
            }

            var inputFolder = args[0];
            var outputFolder = args[1];

            Slipstream88 mods = default;

            DoIncludes(inputFolder, outputFolder, mods);

            DoLeafModules(inputFolder, outputFolder, mods);
            DoLayer1Modules(inputFolder, outputFolder, mods);
            DoLayer2Modules(inputFolder, outputFolder, mods);
        }
    }
}
