﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Herschel.Lib;

namespace Herschel.Loader
{
    class ObservationsFilePacs : ObservationsFile
    {
        protected override bool Parse(string[] parts, out Observation observation)
        {
            if (parts.Length == 15)
            {
                // Photometer
                var aor = parts[5];

                observation = new Observation()
                {
                    Instrument = Instrument.Pacs,
                    ID = long.Parse(parts[0]),
                    Type = ObservationType.Photometry,
                    Level = ParseObservationLevel(parts[12]),
                    InstrumentMode = ParseInstrumentMode(parts[1]),
                    Object = parts[13],
                    Calibration = aor.IndexOf("cal", StringComparison.InvariantCultureIgnoreCase) >= 0,
                    PointingMode = ParsePointingMode(parts[10]),
                    FineTimeStart = -1,
                    FineTimeEnd = -1,
                    RA = -999,
                    Dec = -999,
                    PA = -999,
                    Repetition = (int)double.Parse(parts[9]),
                    MapScanSpeed = ParseMapScanSpeed(parts[14]),
                    MapHeight = double.NaN,
                    MapWidth = double.NaN,
                    RasterNumPoint = -1,
                    RasterPointStep = double.NaN,
                    RasterLine = -1,
                    RasterColumn = -1,
                    AORLabel = aor,
                    AOT = parts[6],
                };
            }
            else if (parts.Length == 19)
            {
                // Spectrometer

                var aor = parts[7];

                observation = new Observation()
                {
                    Instrument = Instrument.Pacs,
                    ID = long.Parse(parts[0]),
                    Type = ObservationType.Spectroscopy,
                    Level = ParseObservationLevel(parts[15]),
                    InstrumentMode = ParseInstrumentMode(parts[5]) | ParseChoppingMode(parts[10]),
                    Object = parts[16],
                    Calibration = aor.IndexOf("cal", StringComparison.InvariantCultureIgnoreCase) >= 0,
                    PointingMode = ParsePointingMode(parts[6]),
                    FineTimeStart = -1,
                    FineTimeEnd = -1,
                    RA = double.Parse(parts[1]),
                    Dec = double.Parse(parts[2]),
                    PA = double.Parse(parts[3]),
                    Repetition = -1,   // TODO: parse from spec info
                    MapScanSpeed = double.NaN,
                    MapHeight = double.NaN,
                    MapWidth = double.NaN,
                    RasterNumPoint = int.Parse(parts[12]) * int.Parse(parts[13]),
                    RasterPointStep = double.Parse(parts[11]),
                    RasterLine = int.Parse(parts[12]),
                    RasterColumn = int.Parse(parts[13]),

                    SpecNumLine = int.Parse(parts[17]),
                    SpecRange = parts[18],

                    AORLabel = aor,
                    AOT = parts[8],
                };
            }
            else if (parts.Length == 14)
            {
                // Parallel
                var aor = parts[5];

                observation = new Observation()
                {
                    Instrument = Instrument.PacsSpireParallel,
                    ID = long.Parse(parts[0]),
                    Type = ObservationType.Photometry,
                    Level = ParseObservationLevel(parts[12]),
                    InstrumentMode = ParseInstrumentMode(parts[1]),
                    Object = parts[13],
                    Calibration = aor.IndexOf("cal", StringComparison.InvariantCultureIgnoreCase) >= 0,
                    PointingMode = ParsePointingMode(parts[10]),
                    FineTimeStart = -1,
                    FineTimeEnd = -1,
                    RA = -999,
                    Dec = -999,
                    PA = -999,
                    Repetition = -1,       // TODO: missing
                    MapScanSpeed = ParseMapScanSpeed(parts[7]),
                    MapHeight = double.Parse(parts[8]),
                    MapWidth = double.Parse(parts[9]),
                    RasterNumPoint = -1,
                    RasterPointStep = double.NaN,
                    RasterLine = -1,
                    RasterColumn = -1,
                    AORLabel = aor,
                    AOT = parts[6],
                };
            }
            else
            {
                throw new NotImplementedException();
            }

            return true;
        }

        protected override InstrumentMode ParseInstrumentMode(string value)
        {
            switch (value)
            {
                case "blue1":
                    return InstrumentMode.PacsPhotoBlue;
                case "blue2":
                    return InstrumentMode.PacsPhotoGreen;
                case "none":
                    return InstrumentMode.Pacs;     // TODO: calibration ?
                case "PacsRangeSpec":
                    return InstrumentMode.PacsSpectroRange;
                case "PacsLineSpec":
                    return InstrumentMode.PacsSpectroLine;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override PointingMode ParsePointingMode(string value)
        {
            // TODO: raster mode is instrument (chopper) setting!

            switch (value)
            {
                case "Basic-fine":
                case "Pointed":
                    return PointingMode.Pointed;
                case "Line_scan":
                    return PointingMode.ScanLine;
                case "Nodding":
                    return PointingMode.Pointed | PointingMode.Nodding;
                case "Raster":
                    return PointingMode.Raster;
                case "Nodding-raster":
                    return PointingMode.Raster | PointingMode.Nodding;
                case "Mapping":
                    return PointingMode.Mapping;
                case "Pointed with dither":
                    return PointingMode.Pointed;    // only calibration, no special flag defined
                default:
                    throw new NotImplementedException();
            }
        }

        private InstrumentMode ParseChoppingMode(string value)
        {
            switch (value)
            {
                case "true":
                    return InstrumentMode.PacsChopperOn;
                case "false":
                    return InstrumentMode.PacsChopperOff;
                default:
                    throw new NotImplementedException();
            }
        }

        private double ParseMapScanSpeed(string value)
        {
            switch (value)
            {
                case "none":
                    return double.NaN;
                case "low":
                    return 10.0;
                case "medium":          // photo
                case "slow":            // parallel
                    return 20.0;
                case "high":            // photo
                case "fast":            // parallel
                    return 60;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}