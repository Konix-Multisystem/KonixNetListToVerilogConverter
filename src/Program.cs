using System;
using System.IO;

namespace K2V
{
    class Program
    {
        static void Validate()
        {

        }

        // DSP

        // VIDEO HARDWARE
        static string[] videoModules={
            "/home/snax/fpga/OLDDIRS/SS1NET/CLOCK.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/IODEC.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STAT.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/MEM.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/INT.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/VCNT.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/HCNT.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/BM.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/VTIM.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/VID.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/PIX.NET"
        };

        // BLITTER
        static string[] blitterDataModules={
            "/home/snax/fpga/OLDDIRS/SS1NET/SRCDATA.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/DSTDATA.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/PATDATA.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/COMP.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/LFU.NET"
        };
        static string[] blitterStateModules={
            "/home/snax/fpga/OLDDIRS/SS1NET/STOUTER.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STCMD.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STPARAM.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STINNER.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STMEM.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/CMDREGS.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/INNERCNT.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/OUTERCNT.NET"
        };
        static string[] blitterAddrModules={
            "/home/snax/fpga/OLDDIRS/SS1NET/DSTAREG.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/SRCAREG.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/PCAREG.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/ADDAMUX.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STEPREG.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/ADDBSEL.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/ADDRADD.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/ADDROUT.NET"
        };

        static string[] blitterModules={
            "/home/snax/fpga/OLDDIRS/SS1NET/DATA.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/ADDR.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/STATE.NET",
            "/home/snax/fpga/OLDDIRS/SS1NET/BUSCON.NET"
        };

        static void DoModule(string path)
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

            var m = new Module();
            m.Parse(tokenizer);
            m.Dump(path);
        }

        static void DoInputModule()
        {
            string path = videoModules[0];

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

            var m = new Module();
            m.Parse(tokenizer);
            m.Dump(path);
        }

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

        static void DoVideo()
        {
            Module [] deps = new Module[videoModules.Length];
            var m = new Module();
            for (int i = 0; i < videoModules.Length; i++)
            {
                string s = videoModules[i];

                deps[i] = new Module();
                deps[i].Parse(Tokenise(s));
                m.RegisterModule(deps[i], Path.GetFileNameWithoutExtension(s));
            }

            string path = "/home/snax/fpga/OLDDIRS/SS1NET/VID.NET";

            var tokenizer = Tokenise(path);
            m.Parse(tokenizer);
            m.Dump(path);

        }

        static void DoBlitterDataPath()
        {
            Module [] deps = new Module[blitterDataModules.Length];
            var m = new Module();
            for (int i = 0; i < blitterDataModules.Length; i++)
            {
                string s = blitterDataModules[i];

                deps[i] = new Module();
                deps[i].Parse(Tokenise(s));
                m.RegisterModule(deps[i], Path.GetFileNameWithoutExtension(s));
            }

            string path = "/home/snax/fpga/OLDDIRS/SS1NET/DATA.NET";

            var tokenizer = Tokenise(path);
            m.Parse(tokenizer);
            m.Dump(path);

        }
        
        static void DoBlitterStatePath()
        {
            Module [] deps = new Module[blitterStateModules.Length];
            var m = new Module();
            for (int i = 0; i < blitterStateModules.Length; i++)
            {
                string s = blitterStateModules[i];

                deps[i] = new Module();
                deps[i].Parse(Tokenise(s));
                m.RegisterModule(deps[i], Path.GetFileNameWithoutExtension(s));
            }

            string path = "/home/snax/fpga/OLDDIRS/SS1NET/STATE.NET";

            var tokenizer = Tokenise(path);
            m.Parse(tokenizer);
            m.Dump(path);
        }

        static void DoBlitterAddrPath()
        {
            Module [] deps = new Module[blitterAddrModules.Length];
            var m = new Module();
            for (int i = 0; i < blitterAddrModules.Length; i++)
            {
                string s = blitterAddrModules[i];

                deps[i] = new Module();
                deps[i].Parse(Tokenise(s));
                m.RegisterModule(deps[i], Path.GetFileNameWithoutExtension(s));
            }

            string path = "/home/snax/fpga/OLDDIRS/SS1NET/ADDR.NET";

            var tokenizer = Tokenise(path);
            m.Parse(tokenizer);
            m.Dump(path);
        }

        static void DoBlitterPath()
        {
            Module [] deps = new Module[blitterModules.Length];
            var m = new Module();
            for (int i = 0; i < blitterModules.Length; i++)
            {
                string s = blitterModules[i];

                deps[i] = new Module();
                deps[i].Parse(Tokenise(s));
                m.RegisterModule(deps[i], Path.GetFileNameWithoutExtension(s));
            }

            string path = "/home/snax/fpga/OLDDIRS/SS1NET/BLIT.NET";

            var tokenizer = Tokenise(path);
            m.Parse(tokenizer);
            m.Dump(path);
        }
        static void Main(string[] args)
        {
            //DoInputModule();
            //DoVideo();

            DoModule("/home/snax/fpga/OLDDIRS/SS1NET/ADDAMUX.NET");

            //DoBlitterDataPath();
            //DoBlitterStatePath();
            //DoBlitterAddrPath();
            //DoBlitterPath();
        }
    }
}
