﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtSrc
{
    [Serializable()]
    public class AgentData
    {
        public String routerAddress;
        public FIB fib;
        public UnexpectedFIB unFib;
        public List<ExtSrc.WireBand> AvalaibleBandIN;
        public List<ExtSrc.WireBand> AvalaibleBandOUT;
        public List<ExtSrc.Connection> Connections;


        public String address
        {
            get { return routerAddress; }
            set { routerAddress = value;  }
        }

        public FIB fibTable
        {
            get { return fib; }
            set { fib = value; }
        }

        public UnexpectedFIB unFibTable
        {
            get { return unFib; }
            set { unFib = value; }
        }

        public AgentData(String address, FIB fib, UnexpectedFIB ufib, List<ExtSrc.WireBand> AvalaibleBandIN, List<ExtSrc.WireBand> AvalaibleBandOUT, List<ExtSrc.Connection> Connections)
        {
            this.routerAddress = address;
            this.fib = fib;
            this.unFib = ufib;
            this.AvalaibleBandIN = AvalaibleBandIN;
            this.AvalaibleBandOUT = AvalaibleBandOUT;
            this.Connections = Connections;
        }

    }
}
