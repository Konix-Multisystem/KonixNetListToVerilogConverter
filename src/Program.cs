using System;
using System.IO;

namespace K2V
{
    class Program
    {
        static void Validate()
        {

        }

        static void Main(string[] args)
        {
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/CLOCK.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/IODEC.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/STAT.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/MEM.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/INT.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/VCNT.NET";
            //string path = "/home/snax/fpga/OLDDIRS/SS1NET/HCNT.NET";
            string path = "/home/snax/fpga/OLDDIRS/SS1NET/BM.NET";

            //Tokenizer.Test(path);

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
    }
}
