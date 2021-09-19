using System;
using System.Collections.Generic;
using System.IO;

namespace K2V
{
    class Program
    {
        static readonly string baseInputFolder = "/home/snax/fpga/OLDDIRS/SS1NET/";
        static readonly string baseOutputFolder = "/home/snax/fpga/autoGen/";

        // For the morning, lets convert counters.net (multiple module, we should form a set of standard includes, LEGO, COUNTERS, MACROS, QMACROS)


        // DSP
        static string[] dspBaseModules={
            "PC.NET",
            "PWM.NET",
            "DMA.NET",
            "PRAM.NET",
            "DRAM.NET"
        };

        // VIDEO HARDWARE
        static string[] videoBaseModules={
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
        static string[] blitterDataModules={
            "SRCDATA.NET",
            "DSTDATA.NET",
            "PATDATA.NET",
            "COMP.NET",
            "LFU.NET"
        };
        static string[] blitterStateModules={
            "STOUTER.NET",
            "STCMD.NET",
            "STPARAM.NET",
            "STINNER.NET",
            "STMEM.NET",
            "CMDREGS.NET",
            "INNERCNT.NET",
            "OUTERCNT.NET"
        };
        static string[] blitterAddrModules={
            "DSTAREG.NET",
            "SRCAREG.NET",
            "PCAREG.NET",
            "ADDAMUX.NET",
            "STEPREG.NET",
            "ADDBSEL.NET",
            "ADDRADD.NET",
            "ADDROUT.NET"
        };

        static string[] blitterModules={
            "DATA.NET",
            "ADDR.NET",
            "STATE.NET",
            "BUSCON.NET"
        };

        static string[][] leafModules={
            videoBaseModules,
            blitterDataModules,
            blitterStateModules,
            blitterAddrModules,
            dspBaseModules
        };

        static string[] videoModules = {
            "VID.NET"
        };

        static string [][] layer1Modules={
            videoModules,
            blitterModules,
        };
        static string [] blitterMain={
            "BLIT.NET"
        };

        static string [][] layer2Modules={
            blitterMain
        };

        static Tokenizer Tokenise(string path)
        {
            Tokenizer tokenizer = new Tokenizer();
            try 
            {
                using (var input = new StreamReader(path))
                {
                    tokenizer.tokenize(input);
                }
            } 
            catch (Exception ex) 
            {
                Console.WriteLine(ex.StackTrace);
            }
            return tokenizer;
        }

        static string[] GetAllModulesFromMultiArray(string[][] array)
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

        static string[] GetAllLeafModules()
        {
            return GetAllModulesFromMultiArray(leafModules);
        }

        static string[] GetAllLayer1Modules()
        {
            return GetAllModulesFromMultiArray(layer1Modules);
        }

        static string[] GetAllLayer2Modules()
        {
            return GetAllModulesFromMultiArray(layer2Modules);
        }

        static void DoConvertModules(string[] children, string[] parents)
        {
            var includes = FetchIncludes();

            List<Module> deps = new List<Module>();
            for (int i = 0; i < children.Length; i++)
            {
                string s = children[i];

                foreach (var c in Module.ParseFile(s))
                {
                    c.RegisterModules(includes);
                    deps.Add(c);
                }
            }

            foreach (var m in parents)
            {
                var modules = Module.ParseFile(m);
                foreach (var mod in modules)
                {
                    mod.RegisterModules(deps);
                    mod.RegisterModules(includes);
                    mod.RegisterModules(modules);
                    mod.Dump(Path.Combine(baseOutputFolder, mod.FileName));
                }
            }
        }

        static void DoLeafModules()
        {
            DoConvertModules(new string[] { }, GetAllLeafModules());
        }

        static void DoLayer1Modules()
        {
            DoConvertModules(GetAllLeafModules(), GetAllLayer1Modules());
        }

        static void DoLayer2Modules()
        {
            DoConvertModules(GetAllLayer1Modules(), GetAllLayer2Modules());
        }

        static List<Module> FetchIncludes()
        {
            var modules = Module.ParseFile(Path.Combine(baseInputFolder, "COUNTERS.NET"));
            modules.AddRange(Module.ParseFile(Path.Combine(baseInputFolder, "LEGO.NET")));
            modules.AddRange(Module.ParseFile(Path.Combine(baseInputFolder, "MACROS.NET")));
            modules.AddRange(Module.ParseFile(Path.Combine(baseInputFolder, "QMACROS.NET")));
            foreach (var m in modules)
            {
                m.RegisterModules(modules);
            }
            return modules;
        }

        static void DoIncludes()
        {
            var modules = FetchIncludes();
            foreach (var m in modules)
            {
                m.Dump(Path.Combine(baseOutputFolder, m.FileName));
            }
        }

        static void Main(string[] args)
        {
            DoIncludes();

            DoLeafModules();
            DoLayer1Modules();
            DoLayer2Modules();
        }
    }
}
