﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Configuration;
using Jhu.Spherical;
using Herschel.Lib;

namespace Herschel.Loader
{
    class Program
    {
        static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Herschel"].ConnectionString; }
        }

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            var verb = args[0].ToLowerInvariant();
            var obj = args[1].ToLowerInvariant();

            switch (obj)
            {
                case "pointing":
                    switch (verb)
                    {
                        case "prepare":
                            PreparePointings(args);
                            break;
                        case "load":
                            LoadPointings(args);
                            break;
                        case "merge":
                            MergePointings(args);
                            break;
                        // TODO: add leg generation
                        case "scanmap": // TODO: merge with merge ;-)
                            GenerateScanMapFootprints(args);
                            break;
                        case "cleanup":
                            CleanupPointings(args);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                case "obs":
                    switch (verb)
                    {
                        case "prepare":
                            PrepareObservations(args);
                            break;
                        case "load":
                            LoadObservations(args);
                            break;
                        case "merge":
                            MergeObservations(args);
                            break;
                        /*case "cleanup":
                            CleanupObservations(args);
                            break;*/
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static ObservationsFile GetObservationsFile(string inst)
        {
            ObservationsFile file = null;

            switch (inst.ToLowerInvariant())
            {
                case "pacs":
                    file = new ObservationsFilePacs();
                    break;
                case "spire":
                    file = new ObservationsFileSpire();
                    break;
                case "hifi":
                    file = new ObservationsFileHifi();
                    break;
                default:
                    throw new NotImplementedException();
            }

            return file;
        }

        private static void PrepareObservations(string[] args)
        {
            var inst = args[2].ToLowerInvariant();
            var path = args[3];
            var output = args[4];

            Console.WriteLine("Preparing observation file for bulk load...");

            var file = GetObservationsFile(inst);
            file.ConvertObservationsFile(path, output, false);
        }

        private static void LoadObservations(string[] args)
        {
            ExecuteBulkInsert(args, SqlScripts.LoadObservation);
        }

        private static void MergeObservations(string[] args)
        {
            ExecuteScript(SqlScripts.MergeObservation);
        }

        private static void PreparePointings(string[] args)
        {
            var inst = args[2].ToLowerInvariant();
            var type = (PointingObservationType)byte.Parse(args[3]);
            var path = args[4];
            var output = args[5];
            int fnum = int.Parse(args[6]);

            // Run processing on multiple threads

            var dir = Path.GetDirectoryName(path);
            var pattern = Path.GetFileName(path);

            var files = Directory.GetFiles(dir, pattern);
            var queue = new Queue<string>(files);

            Console.WriteLine("Preparing pointing files for bulk load...", files.Length);
            Console.WriteLine("Found {0} files.", files.Length);

            int q = 0;

            Parallel.For(0, fnum, i =>
            {
                while (true)
                {
                    int qq;
                    string infile = null;

                    lock (queue)
                    {
                        if (queue.Count > 0)
                        {
                            infile = queue.Dequeue();
                            qq = q++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var file = GetPointingsFile(inst, type);

                    try
                    {
                        file.ConvertPointingsFile(infile, String.Format(output, i), true);
                        Console.WriteLine("{0}: {1}", qq, infile);
                    }
                    catch (Exception ex)
                    {

                        Console.Error.WriteLine("Unhandled Exception processing '{0}'", infile);
                        Console.Error.WriteLine("Output file '{0}' may be corrupt.", String.Format(output, i));
                        Console.Error.WriteLine("Unhandled Exception: {0}: {1}", ex.GetType().FullName, ex.Message);
                        Console.Error.WriteLine(ex.StackTrace);
                        Console.WriteLine();
                    }
                }
            });
        }

        private static void LoadPointings(string[] args)
        {
            ExecuteBulkInsert(args, SqlScripts.LoadPointing);
        }

        private static void MergePointings(string[] args)
        {
            ExecuteScript(SqlScripts.MergePointing);
        }

        private static void GenerateScanMapFootprints(string[] args)
        {
            // Generate scan map footprints

            // Find observations
            var sql = @"
SELECT *
FROM Observation o
LEFT OUTER JOIN ScanMap s ON s.inst = o.inst AND s.obsID = o.obsID
LEFT OUTER JOIN RasterMap r ON r.inst = o.inst AND r.obsID = o.obsID
LEFT OUTER JOIN Spectro p ON p.inst = o.inst AND p.obsID = o.obsID
WHERE o.inst IN (1, 2)            -- PACS or SPIRE
  AND o.pointingMode IN (8, 16)   -- Scan map
/*  AND o.calibration = 0           -- not a calibration
  AND o.obsLevel < 250            -- only processed    */
  AND o.region IS NULL
";

            if (args.Length > 2)
            {
                sql += "  AND o.inst = " + args[2];
            }

            if (args.Length > 3)
            {
                sql += "  AND o.obsID = " + args[3];
            }

            var observations = new List<Observation>();

            using (var cmd = new SqlCommand(sql))
            {
                observations.AddRange(
                    DbHelper.ExecuteCommandReader<Observation>(cmd));
            }

            Parallel.ForEach(observations, GenerateScanMapFootprint);
        }

        private static void GenerateScanMapFootprint(Observation obs)
        {
            // Find legs belonging to observation

            var sql = @"
SELECT *
FROM load.LegRegion
WHERE inst = @inst AND obsID = @obsID
ORDER BY legID
";

            var legs = new List<Region>();

            using (var cn = DbHelper.OpenConnection())
            {
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@inst", SqlDbType.TinyInt).Value = (byte)obs.Instrument;
                    cmd.Parameters.Add("@obsID", SqlDbType.BigInt).Value = obs.ObsID;

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var bytes = dr.GetSqlBytes(5);
                            var r = Region.FromSqlBytes(bytes);

                            legs.Add(r);
                        }
                    }
                }
            }

            if (legs.Count == 0)
            {
                SaveScanMapFootprint(obs, null);

                Console.WriteLine("Pointing missing for {0}", obs.ObsID);
            }
            else
            {
                // Calculate union of legs of a single observation
                Region union = null;
                int rep = Math.Max(obs.Repetition, 1);
                int scans = legs.Count / rep;
                int start = 0;

                if (obs.Instrument == Instrument.Spire &&
                    rep > 1)
                {
                    scans++;
                }

                // Retry for another re-scan if building footprint from
                // the first one fails
                while (start < rep)
                {
                    union = legs[start * scans];

                    for (int i = 1; i < scans; i++)
                    {
                        try
                        {
                            union.SmartUnion(legs[start * scans + i], 256);
                        }
                        catch (Exception ex)
                        {
                            union = new Region();
                            union.SetErrorMessage(ex);

                            Console.WriteLine("Error generating footprint for {0}", obs.ObsID);

                            break;
                        }
                    }

                    // If unioning succeeded
                    if (!union.HasError)
                    {
                        break;
                    }

                    start++;

                    Console.WriteLine("Retrying for the {0} time", start);
                }

                SaveScanMapFootprint(obs, union);

                //Console.WriteLine("Generated footprint for {0}", obs.ObsID);
            }
        }

        private static void SaveScanMapFootprint(Observation obs, Region r)
        {
            var sql = @"
UPDATE Observation
SET region = @region
WHERE inst = @inst AND obsID = @obsID";

            using (var cn = DbHelper.OpenConnection())
            {
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@inst", SqlDbType.TinyInt).Value = (byte)obs.Instrument;
                    cmd.Parameters.Add("@obsID", SqlDbType.BigInt).Value = obs.ObsID;
                    cmd.Parameters.Add("@region", SqlDbType.VarBinary).Value = r == null ? (object)DBNull.Value : r.ToSqlBytes();

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void CleanupPointings(string[] args)
        {
            ExecuteScript(SqlScripts.CleanupPointing);
        }

        private static void ExecuteScript(string script)
        {
            // Split query
            var sql = DbHelper.SplitQuery(script);

            for (int i = 0; i < sql.Length; i++)
            {
                Console.WriteLine("Executing query:");
                Console.WriteLine(sql[i]);

                using (var cmd = new SqlCommand(sql[i]))
                {
                    DbHelper.ExecuteCommandNonQuery(cmd);
                }
            }
        }

        private static PointingsFile GetPointingsFile(string inst, PointingObservationType type)
        {
            PointingsFile file = null;

            switch (inst.ToLowerInvariant())
            {
                case "pacs":
                    file = new PointingsFilePacs();
                    break;
                case "spire":
                    file = new PointingsFileSpire();
                    break;
                case "hifi":
                    file = new PointingsFileHifi();
                    break;
                default:
                    throw new NotImplementedException();
            }

            file.ObservationType = type;

            return file;
        }

        static long GetID(string filename)
        {
            var fn = Path.GetFileNameWithoutExtension(filename).Substring(7);
            return long.Parse(fn, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ExecuteBulkInsert(string[] args, string sql)
        {
            var path = args[2];
            int fnum = int.Parse(args[3]);

            var dir = Path.GetDirectoryName(path);
            var pattern = Path.GetFileName(path);

            var files = Directory.GetFiles(dir, pattern);
            var queue = new Queue<string>(files);

            Console.WriteLine("Bulk loading files...");
            Console.WriteLine("Found {0} files.", files.Length);

            var options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = fnum
            };

            Parallel.ForEach(files, options, infile =>
            {
                Console.WriteLine("Loading from {0}...", infile);

                using (var cmd = new SqlCommand(sql.Replace("[$datafile]", Path.GetFullPath(infile))))
                {
                    cmd.CommandTimeout = 3600;  // 1h should be enough for bulk inserts

                    try
                    {
                        DbHelper.ExecuteCommandNonQuery(cmd);
                    }
                    catch (Exception ex)
                    {

                        Console.Error.WriteLine("Unhandled Exception processing '{0}'", infile);
                        Console.Error.WriteLine("Unhandled Exception: {0}: {1}", ex.GetType().FullName, ex.Message);
                        Console.Error.WriteLine(ex.StackTrace);
                        Console.WriteLine();
                    }
                }
            });
        }
    }
}
