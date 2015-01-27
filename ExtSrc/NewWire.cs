﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ExtSrc.Observers;

#endregion

namespace ExtSrc
{
    public class NewWire : Observer
    {
        static readonly int GUARD_BAND = 10;
        public static readonly int FREQ_SLOT_UNIT = 10;
        public static readonly int EMPTY_VALUE = -1;

        private int nextSlotID = -1;

        public int ID { get; set; }
        public int distance { get; set; }
        //public List<FrequencySlotUnit> lambdas { get; set; }
        public List<FrequencySlotUnit> FrequencySlotUnitList { get; set; }
        public Dictionary<int, FrequencySlot> FrequencySlotDictionary { get; set; }
        public int PortPrefix { get; set; }
        public int[] RouterIds { get; set; }
        public int[] spectralWidth{get;set;}
        public NewWire(int id, int distance, int lambdasSize, int spectralWidth, int portPref)
        {
            this.ID = id;
            this.distance = distance;
            FrequencySlotUnitList = new List<FrequencySlotUnit>();
            FrequencySlotDictionary = new Dictionary<int, FrequencySlot>();
            //represents 1000Ghz
            this.spectralWidth = Enumerable.Repeat(EMPTY_VALUE, spectralWidth).ToArray(); //new int[spectralWidth];
            PortPrefix = portPref;
            RouterIds = new int[2];
            RouterIds[0] = portPref%10;
            portPref /= 10;
            RouterIds[1] = portPref % 10;
        }

        public int addFreqSlot(int startingFreq, int FSUcount, Modulation mod/*, int FSid*/)
        {
            var id = ++nextSlotID;//FSid;
            var slot = new FrequencySlot(id, Modulation.QPSK, startingFreq);
            FrequencySlotDictionary.Add(id, slot);
            var count = 0;
            foreach (var unit in FrequencySlotUnitList)
            {
                if (!unit.isUsed)
                {
                    unit.isUsed = true;
                    slot.FSUList.Add(unit);
                    count++;
                }
                if (count == FSUcount) break;
            }
            //if (count != FSUcount) Log.d("addFreqSlot error! Needed = " + FSUcount + " got = " + count);
            takeSpectralWidth(startingFreq, FSUcount * FREQ_SLOT_UNIT /*+ GUARD_BAND*/, id);
            return id;
        }

        public void SlideDown()
        {
            //var freeSpectrumBefore = spectralWidth.Where(x => x==-1).ToList().Count;
            Log.d("Slide down.");
            this.spectralWidth = Enumerable.Repeat(EMPTY_VALUE, spectralWidth.Count()).ToArray();
            var idxSpectralWidth = 0;
            var list = FrequencySlotDictionary.Values.ToList();
            list.Sort((x, y) => x.startingFreq.CompareTo(y.startingFreq));
            var idx = 0;
            foreach (var frequencySlot in list)
            {
                if (frequencySlot.startingFreq == idx)
                {
                    idx += frequencySlot.FSUList.Count;
                    idxSpectralWidth += FREQ_SLOT_UNIT;
                }
                else
                {
                    var newStartingFreq = idxSpectralWidth;
                    var tmpList = new List<FrequencySlotUnit>();
                    foreach (var fsuOld in frequencySlot.FSUList)
                    {
                        fsuOld.isUsed = false;
                        var fsuNew = FrequencySlotUnitList[idx];
                        idx++;
                        fsuNew.isUsed = true;
                        tmpList.Add(fsuNew);
                        for (var i = 0; i < FREQ_SLOT_UNIT; i++, idxSpectralWidth++)
                        {
                            spectralWidth[idxSpectralWidth] = frequencySlot.ID;
                        }
                    }
//                    for (var i = 0; i < GUARD_BAND; i++, idxSpectralWidth++)
//                    {
//                        spectralWidth[idxSpectralWidth] = frequencySlot.ID;
//                    }
                    frequencySlot.FSUList.Clear();
                    frequencySlot.FSUList.AddRange(tmpList);
                    frequencySlot.startingFreq = newStartingFreq;
                }
            }
            //var freeSpectrumAfter = spectralWidth.Where(x => x == -1).ToList().Count;
            //Log.d("SLIDING TEST" + (freeSpectrumBefore == freeSpectrumAfter) + " Before:" + freeSpectrumBefore + " After:" + freeSpectrumAfter);
        }

        public Boolean IsPossibleToSlide(int FSUcount)
        {
            var freeUnits = FrequencySlotUnitList.Where(x => !x.isUsed).ToList().Count;
            return freeUnits > FSUcount;
        }

        public bool IsTherePlace(int startfreq1, int fsuCount)
        {
            for (int i = startfreq1; i < startfreq1 + fsuCount * FREQ_SLOT_UNIT + GUARD_BAND; i++)
            {
                if (spectralWidth[i] != -1) return false;
            }
            return true;
        }
        

        public Boolean removeFreqSlot(int id)
        {
            FrequencySlot freqSlot;
            if (FrequencySlotDictionary.TryGetValue(id, out freqSlot)) {
                foreach (FrequencySlotUnit fsu in freqSlot.FSUList)
                {
                    fsu.isUsed = false;
                }
                removeSpectralWidth(freqSlot.startingFreq, freqSlot.FSUList.Count * FREQ_SLOT_UNIT /*+ GUARD_BAND*/);
                FrequencySlotDictionary.Remove(id);
                return true;
            }
            return false;
        }

        public int findSpaceForFS(int fsucount)
        {
            int counter = 0;
            int startval = -1; 
            for (int i = 0; i < spectralWidth.Length; i++)
            {
                if (spectralWidth[i] == EMPTY_VALUE)
                {
                    counter++;
                    if (counter == 1)
                        startval = i;
                }
                else counter = 0;

                if (counter == fsucount*FREQ_SLOT_UNIT)
                    return startval;
            }
            return -1;
        }

        public void takeSpectralWidth(int start, int count, int id)
        {
            //Log.d("takeSpectralWidth : " + start + " - " + count + " - " + id);
            for (int i = start; i < start + count; i++)
            {
                if(i<spectralWidth.Length) spectralWidth[i] = id;
            }
        }

        public void removeSpectralWidth(int start, int count)
        {
            for (int i = start; i < start + count; i++)
            {
                spectralWidth[i] = EMPTY_VALUE;
            }
        }

        public void initWire(String address, IPEndPoint cloudEP)
        {
            foreach (var l in FrequencySlotUnitList)
            {
                l.initFSUSocket(address, cloudEP);
                //Thread.Sleep(1000);
            }
        }

        public void Update()
        {
            //close all sockets
            foreach (var l in FrequencySlotUnitList)
            {
                l.close();
            }
        }

        public void Close()
        {
            FrequencySlotUnitList.ForEach(f => f.close());
        }
    }
}
