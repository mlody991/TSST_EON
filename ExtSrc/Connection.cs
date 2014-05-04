﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExtSrc
{
    [Serializable()]
    public class Connection
    {
        IPEndPoint EPIn, EPOut;

        [NonSerialized()] public int[] InLambdaIDs;
        public int[] OutLambdaIDs;
        [NonSerialized()] int InWireID;
        public int OutWireID, Bandwidth;
        public int connectionID; // musi byc jakas wiekksza losowa liczba zeby nie bylo ze dwie maja takie samo id

        public Connection(int[] inLambdas, int[] outLambdas,int wireID, int band, int cID) 
        {
            EPIn = null;
            EPOut = null;
            InWireID = -1;
            OutWireID = wireID;
            InLambdaIDs = null;
            OutLambdaIDs = null;
            Bandwidth = band;
            connectionID = cID;
        
        }
        public Connection(IPEndPoint inEP, IPEndPoint outEP, int inWire, int outWire, int[] inLambdas, int[] outLambdas, int band, int cID)
        {
            EPIn = inEP;
            EPOut = outEP;
            InWireID = inWire;
            OutWireID = outWire;
            InLambdaIDs = inLambdas;
            OutLambdaIDs = outLambdas;
            Bandwidth = band;
            connectionID = cID;

        }
        public override String ToString()
        {
            String result = String.Format("EPIn: {0} EPOut: {1} InWireID: {2} OutWireID: {3} Bandwidth: {4} Connection ID: {5}",
                EPIn, EPOut, InWireID, OutWireID, Bandwidth, connectionID);
            return result;
        }
    }
}
