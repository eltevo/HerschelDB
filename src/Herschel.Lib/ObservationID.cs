﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Herschel.Lib
{
    public struct ObservationID
    {
        public Instrument Instrument { get; set; }
        public long ID { get; set; }

        public static ObservationID Parse(string obsID)
        {
            var i = obsID.IndexOf('|');
            return Parse(obsID.Substring(0, i), obsID.Substring(i + 1));
        }

        public static ObservationID Parse(string instrument, string obsID)
        {
            return new ObservationID()
            {
                Instrument = (Instrument)Enum.Parse(typeof(Instrument), instrument),
                ID = long.Parse(obsID)
            };
        }
    }
}