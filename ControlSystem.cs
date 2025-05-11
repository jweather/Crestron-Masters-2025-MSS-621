using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Masters_2025_MSS_621_JW.Devices;
using Masters_2025_MSS_621_JW.UserInterface;


namespace Masters_2025_MSS_621_JW
{
    public class ControlSystem : CrestronControlSystem
    {
        public EventTimers SetupTimers;

        public Xpanel TP;
        public AirMedia3100 MyAirMedia;
        public CrestronConnected MyCrestronConnected;
        public Nvx351 MyNvx;
        public Audio audio;
        public NvxProducer NvxProducer;

        private bool power = false;
        public bool SystemPower {
            get {
                return power;
            }
            set {
                if (value != power) {
                    power = value;
                    if (value) SystemOn();
                    else SystemOff();
                }
            }
        }

        public ControlSystem()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                // I prefer to have all the hardware devices set up in one place
                TP = new Xpanel(0x04, this);
                MyNvx = new Nvx351(0x11, Nvx351.EMode.Rx, this);
                MyAirMedia = new AirMedia3100(0x22, this);
                MyCrestronConnected = new CrestronConnected(0x09, this);

                // software components
                audio = (Audio)new AudioTV(MyCrestronConnected, TP);
                SetupTimers = new EventTimers();
                SetupTimers.scheduleDaily("22:00", SystemOff);
                NvxProducer = new NvxProducer();

                // connect events
                TP.SourceSelect += SourceSelect;
                TP.setupSources(NvxProducer);

                MyAirMedia.AddressChanged += TP.UpdateAirMediaAddress;
                MyAirMedia.PinCodeChanged += TP.UpdateAirMediaPin;

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        private void SystemOn() {
            MyCrestronConnected.On();
            MyCrestronConnected.Input(1);
        }

        private void SystemOff() {
            MyCrestronConnected.Off();
            MyNvx.SetInput(Nvx351.ESource.Disable);
        }

        private void SourceSelect(int index) {
            MyCrestronConnected.Input(1);
            switch (index) {
                case 0:
                    MyNvx.SetInput(Nvx351.ESource.Hdmi1);
                    TP.SourceControls("MediaControl");
                    break;
                case 1:
                    MyNvx.SetInput(Nvx351.ESource.Hdmi2);
                    TP.SourceControls("AirMediaInfo");
                    break;
                default:
                    int nvxIndex = index - 2;
                    NvxSource source = NvxProducer.sources[nvxIndex];
                    MyNvx.SetInput(Nvx351.ESource.Stream);
                    MyNvx.SetStreamLocation(source.ip);
                    TP.SourceControls("NvxInfo");
                    TP.UpdateNvxAddress(source.ip);
                    break;
            }
        }

        public void log(string msg) {
            CrestronConsole.PrintLine(msg);
        }

    }
}