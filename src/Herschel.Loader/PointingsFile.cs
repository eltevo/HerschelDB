﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Herschel.Lib;

namespace Herschel.Loader
{
    abstract class PointingsFile
    {
        private PointingObservationType observationType;

        public PointingObservationType ObservationType
        {
            get { return observationType; }
            set { observationType = value; }
        }

        protected abstract bool Parse(string[] parts, out Pointing pointing);

        protected void Write(Pointing p, TextWriter writer)
        {
            writer.Write("{0} ", (byte)p.Instrument);
            writer.Write("{0} ", p.ObsID);
            writer.Write("{0} ", p.BBID);
            writer.Write("{0} ", (sbyte)p.ObsType);
            writer.Write("{0} ", p.FineTime);
            writer.Write("{0} ", p.Ra);
            writer.Write("{0} ", p.Dec);
            writer.Write("{0} ", p.Pa);
            writer.WriteLine("{0} ", p.AV);
        }
        
        protected IEnumerable<Pointing> ReadPointingsFile(string filename)
        {
            // Open file
            using (var infile = new StreamReader(filename))
            {
                // Skip four lines
                for (int i = 0; i < 4; i++)
                {
                    infile.ReadLine();
                }

                string line;
                while ((line = infile.ReadLine()) != null)
                {
                    Pointing p;
                    if (Parse(line.Split(' '), out p))
                    {
                        yield return p;
                    }
                }
            }
        }

        /// <summary>
        /// Reads Herschel pointing file and writes bulk-insert ready file
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        public virtual void ConvertPointingsFile(string inputFile, string outputFile, bool append)
        {
            append &= File.Exists(outputFile);

            using (var outfile = new StreamWriter(outputFile, append))
            {
                foreach (var p in ReadPointingsFile(inputFile))
                {
                    Write(p, outfile);
                }
            }
        }

    }
}
