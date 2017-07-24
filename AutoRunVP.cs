using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.IO;
using System.Security.Principal;
using CSSPAppModel;
using System.Globalization;
using System.Threading;

namespace AutoRunVP
{
    public partial class AutoRunVP : Form
    {
        CultureInfo cultureInfo;

        #region Variables
        #region MIKE
        string delimStr = ",";
        char[] delimiter;

        //FemEngineHD femEngineHD = new FemEngineHD();
        M21fmLog m21fmLog = new M21fmLog();
        //byte[] ByteArray;
        //byte[] ByteArray2;

        //Dfs dfs = new Dfs(Dfs.DFSType.DFS0, true);
        //DFSReaderClass dfsReader;

        //int BytePos = 0;
        //StringBuilder sb;

        //FileInfo fi;
        //FileStream fs;
        //BinaryWriter writer;

        //int LineNumber = 0;

        //const double R = 6378137.0;
        //const double d2r = Math.PI / 180;
        //const double r2d = 180 / Math.PI;

        //List<Dfs.Node> InterpolatedContourNodeList = new List<Dfs.Node>();

        public M21fm m21fm { get; set; }
        public Dfs dfs { get; set; }
        public KMZ kmz { get; set; }

        List<float> ContourValueList;

        MikeScenario CurrentMikeScenario = new MikeScenario();
        List<CSSPFileNoContent> CurrentScenarioFileList = new List<CSSPFileNoContent>();
        MikeParameter CurrentMikeParameter = new MikeParameter();
        List<MikeSource> CurrentMikeSourceList = new List<MikeSource>();
        int CurrentMikeSourceIndex = 0;

        #endregion MIKE

        private const uint GW_CHILD = 5;
        private const uint GW_HWNDNEXT = 2;
        private const uint WM_LBUTTONDOWN = 0x201;
        private const uint WM_LBUTTONUP = 0x202;
        private const uint WM_CHAR = 0x102;
        private const uint WM_KEYDOWN = 0x100;
        private const uint WM_KEYUP = 0x101;
        private const uint WM_SYSKEYDOWN = 0x104;
        private const uint WM_SYSKEYUP = 0x105;
        private const uint VK_SHIFT = 0x10;
        private const uint VK_CONTROL = 0x11;
        IntPtr hWndPlumes = IntPtr.Zero;
        IntPtr hWndPlumesToolBar = IntPtr.Zero;
        IntPtr hWndPlumesTab = IntPtr.Zero;
        IntPtr hWndDiffuserTab = IntPtr.Zero;
        IntPtr hWndAmbientTab = IntPtr.Zero;
        IntPtr hWndSpecialSettingTab = IntPtr.Zero;
        IntPtr hWndTextOutputTab = IntPtr.Zero;
        IntPtr hWndGraphicOutputTab = IntPtr.Zero;
        IntPtr hWndDiffuserProjectTextBox = IntPtr.Zero;
        IntPtr hWndDiffuserFlowMixZoneDataGrid = IntPtr.Zero;
        IntPtr hWndDiffuserFlowMixZoneDataGridEdit = IntPtr.Zero;
        IntPtr hWndAmbientTabPanelDataGrid = IntPtr.Zero;
        IntPtr hWndAmbientTabPanelDataGridEdit = IntPtr.Zero;
        IntPtr hWndTextOutputClearButton = IntPtr.Zero;
        IntPtr hWndTextOutputResultTextBox = IntPtr.Zero;
        APIFunc af = new APIFunc() as APIFunc;
        int PreviousRow = 0;
        bool BeforeReadInputDataFromXML;
        private enum DiffuserVariable
        {
            PortDiameter,
            PortElevation,
            VerticalAngle,
            HorizontalAngle,
            NumberOfPorts,
            PortSpacing,
            AcuteMixZone,
            ChronicMixZone,
            PortDepth,
            EffluentFlow,
            EffluentSalinity,
            EffluentTemperature,
            EffluentConcentration,
            FroudeNumber,
            EffluentVelocity
        }
        private enum AmbientVariable
        {
            MeasurementDepth,
            CurrentSpeeds,
            CurrentDirections,
            AmbientSalinity,
            AmbientTemperature,
            BackgroundConcentration,
            PollutantDecayRate,
            FarFieldCurrentSpeed,
            FarFieldCurrentDirection,
            FarFieldDiffusionCoefficient
        }
        // Visual Plumes parameters -- Diffuser Tab
        private string[] PortDiameterValues = { "0.3", "", "" }; // in meters
        private string[] PortElevationValues = { "0.15", "", "" }; // in meters
        private string[] VerticalAngleValues = { "0", "", "" }; // in degrees
        private string[] HorizontalAngleValues = { "90", "", "" }; // in degrees
        private string[] NumberOfPortsValues = { "1", "", "" }; // as a number
        private string[] PortSpacingValues = { "1000", "", "" }; // as a number
        private string[] AcuteMixZoneValues = { "50", "", "" }; // in meters
        private string[] ChronicMixZoneValues = { "40000", "", "" }; // in meters
        private string[] PortDepthValues = { "1", "", "" }; // in meters
        private string[] EffluentFlowValues = { "0.01", "", "" }; // in m3/s
        private string[] EffluentSalinityValues = { "0", "", "" }; // in psu
        private string[] EffluentTemperatureValues = { "15", "", "" }; // in degree celcius
        private string[] EffluentConcentrationValues = { "3000000", "", "" }; // in col/dl
        private string[] FroudeNumberValues = { "", "", "" }; // in col/dl
        private string[] EffluentVelocityValues = { "", "", "" }; // in col/dl

        private string PortDiameterCurrent = "";
        private string PortElevationCurrent = "";
        private string VerticalAngleCurrent = "";
        private string HorizontalAngleCurrent = "";
        private string NumberOfPortsCurrent = "";
        private string PortSpacingCurrent = "";
        private string AcuteMixZoneCurrent = "";
        private string ChronicMixZoneCurrent = "";
        private string PortDepthCurrent = "";
        private string EffluentFlowCurrent = "";
        private string EffluentSalinityCurrent = "";
        private string EffluentTemperatureCurrent = "";
        private string EffluentConcentrationCurrent = "";
        private string FroudeNumberCurrent = "";
        private string EffluentVelocityCurrent = "";


        // Visual Plumes parameters -- Ambiant Tab
        private string[][] MeasurementDepthValues = { new string[] { "0", "", "" }, 
                                                          new string[] { "5", "", "" }, 
                                                          new string[] { "", "", "" },
                                                          new string[] { "", "", "" },
                                                          new string[] { "", "", "" }
                                                  }; // in meters
        private string[][] CurrentSpeedValues = { new string[] { "0", "", "" }, 
                                                      new string[] { "", "", "" }, 
                                                      new string[] { "", "", "" }, 
                                                      new string[] { "", "", "" }, 
                                                      new string[] { "", "", "" }
                                               }; // in m/s
        private string[][] CurrentDirectionValues = { new string[] { "90", "", "" }, 
                                                          new string[] { "", "", "" }, 
                                                          new string[] { "", "", "" }, 
                                                          new string[] { "", "", "" }, 
                                                          new string[] { "", "", "" }
                                               }; // in degrees
        private string[][] AmbientSalinityValues = { new string[] { "28", "", "" }, 
                                                         new string[] { "", "", "" }, 
                                                         new string[] { "", "", "" }, 
                                                         new string[] { "", "", "" }, 
                                                         new string[] { "", "", "" }
                                                   }; // in psu
        private string[][] AmbientTemperatureValues = { new string[] { "10", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }
                                                     }; // in degrees
        private string[][] BackgroundConcentrationValues = { new string[] { "0", "", "" }, 
                                                                 new string[] { "", "", "" }, 
                                                                 new string[] { "", "", "" }, 
                                                                 new string[] { "", "", "" }, 
                                                                 new string[] { "", "", "" }
                                                          }; // in col/dl
        private string[][] PollutantDecayRateValues = { new string[] { "4.6821", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }, 
                                                            new string[] { "", "", "" }
                                                     }; // in per day
        private string[][] FarFieldCurrentSpeedValues = { new string[] { "0.1", "", "" }, 
                                                              new string[] { "", "", "" }, 
                                                              new string[] { "", "", "" }, 
                                                              new string[] { "", "", "" }, 
                                                              new string[] { "", "", "" }
                                                       }; // in m/s
        private string[][] FarFieldCurrentDirectionValues = { new string[] { "90", "", "" }, 
                                                                  new string[] { "", "", "" }, 
                                                                  new string[] { "", "", "" }, 
                                                                  new string[] { "", "", "" }, 
                                                                  new string[] { "", "", "" }
                                                           }; // in degrees
        private string[][] FarFieldDiffusionCoefficientValues = { new string[] { "0.0003", "", "" }, 
                                                                      new string[] { "", "", "" }, 
                                                                      new string[] { "", "", "" }, 
                                                                      new string[] { "", "", "" }, 
                                                                      new string[] { "", "", "" }
                                                               }; // in m0.67/s2

        private string[] MeasurementDepthCurrent = { "", "", "", "", "" };
        private string[] CurrentSpeedCurrent = { "", "", "", "", "" };
        private string[] CurrentDirectionCurrent = { "", "", "", "", "" };
        private string[] AmbientSalinityCurrent = { "", "", "", "", "" };
        private string[] AmbientTemperatureCurrent = { "", "", "", "", "" };
        private string[] BackgroundConcentrationCurrent = { "", "", "", "", "" };
        private string[] PollutantDecayRateCurrent = { "", "", "", "", "" };
        private string[] FarFieldCurrentSpeedCurrent = { "", "", "", "", "" };
        private string[] FarFieldCurrentDirectionCurrent = { "", "", "", "", "" };
        private string[] FarFieldDiffusionCoefficientCurrent = { "", "", "", "", "" };

        // Parsing variables
        string ResultSummaryTxt = "Results summary:\r\n\r\n";
        string AmbientNearFieldCurrentSpeedTxt = "\tAmbient near field current speed: ";
        string AmbientFarFieldCurrentSpeedTxt = "\tAmbient far field current speed: ";
        string MetersPerSecondTxt = " m/s";
        string PortDiameterTxt = "\tPort diameter: ";
        string MeterTxt = " m";
        string PortDepthTxt = "\tPort depth: ";
        string DischargeDepthTxt = "\tDepth at discharge: ";
        string ChannelWidthTxt = "\tChannel width: ";
        string ChannelDepthTxt = "\tChannel depth: ";
        string EffluentFlowTxt = "\tEffluent Flow: ";
        string EffluentTemperatureTxt = "\tEffluent temperature: ";
        string CubicMeterPerSecondTxt = " m3/s";
        string CubicMeterPerDayTxt = " m3/d";
        string EffluentConcentrationTxt = "\tPollution: ";
        string CelsiusTxt = " (C)";
        string ColPerDecaLiterTxt = " (col/dl)";
        string DecayRateTxt = "\tDecay rate: ";
        string PerDayTxt = " (d-1)";
        string FlowClassificationTxt = "\tFlow Classification : ";
        string AmbientAverageDepthTxt = "\tAmbient average depth: ";
        string FroudeNumberTxt = "\tFroude number: ";
        string EffluentVelocityTxt = "\tEffluent velocity: ";
        string DiffuserTxt = "\tDiffuser: ";
        string NumOfPortsTxt = "# of Ports: ";
        string PortSpacingTxt = "Port spacing: ";
        //string AmbientTemperatureTxt = "\tAmbient temperature: ";

        string Line1 = "                                       Far Field     Dispersion      Travel      Corrected";
        string Line2 = "        Concentration    Dilution        width        distance        time        distance";
        string Line3 = "                                          (m)           (m)           (h)           (m)";

        List<VPAndCormixResValues> listOfResVal;
        List<VPAndCormixResValues> listOfCormixResVal;

        double Conc10000 = 0;
        double FarWidth10000 = 0;
        double Dist10000 = 0;
        double CorDist10000 = 0;
        double Time10000 = 0;
        double Conc1000 = 0;
        double FarWidth1000 = 0;
        double Dist1000 = 0;
        double CorDist1000 = 0;
        double Time1000 = 0;
        double Conc100 = 0;
        double FarWidth100 = 0;
        double Dist100 = 0;
        double CorDist100 = 0;
        double Time100 = 0;
        double Dilu88 = 0;
        double FarWidth88 = 0;
        double Dist88 = 0;
        double CorDist88 = 0;
        double Time88 = 0;
        double Dilu14 = 0;
        double FarWidth14 = 0;
        double Dist14 = 0;
        double CorDist14 = 0;
        double Time14 = 0;
        double Conc300 = 0;
        double Dilu300 = 0;
        double FarWidth300 = 0;
        double Time300 = 0;
        double Conc6 = 0;
        double Dilu6 = 0;
        double FarWidth6 = 0;
        double Dist6 = 0;
        double CorDist6 = 0;
        double Conc12 = 0;
        double Dilu12 = 0;
        double FarWidth12 = 0;
        double Dist12 = 0;
        double CorDist12 = 0;
        double Conc18 = 0;
        double Dilu18 = 0;
        double FarWidth18 = 0;
        double Dist18 = 0;
        double CorDist18 = 0;
        double Conc24 = 0;
        double Dilu24 = 0;
        double FarWidth24 = 0;
        double Dist24 = 0;
        double CorDist24 = 0;
        double Conc30 = 0;
        double Dilu30 = 0;
        double FarWidth30 = 0;
        double Dist30 = 0;
        double CorDist30 = 0;

        bool PleaseStopRecursiveRun = false;
        string XMLInputFileName;
        int ScenarioRunningNumber = 0;
        bool IsLoaded = false;
        string[] PathElements;
        List<WndHandleAndTitle> DesktopChildrenWindowsList;
        List<CloseCaptionAndCommand> DialogToClose;
        enum ItemType
        {
            Root,
            Province,
            Municipality,
            WWTP,
            LiftStation
        }
        //ItemType itemType;
        TVI RootTVI = new TVI();

        // Box Model variables
        double BMT90 = 0;
        double BMTemperature = 0;
        double BMDecayCoefficient = 0;
        double BMFlow = 0;
        double BMFlowDuration = 0;
        double BMDilution = 0;
        double BMDepth = 0;
        double BMFCUntreated = 0;
        double BMFCPreDisinfection = 0;
        double BMConcentrationObjective = 0;

        //       Dilution
        double BMDilutionVolume = 0;
        double BMDilutionSurface = 0;
        double BMDilutionRadius = 0;
        double BMDilutionLeftSideDiameterLineAngle = 0;
        double BMDilutionCircleCenterLatitude = 0;
        double BMDilutionCircleCenterLongitude = 0;
        bool BMDilutionFixLength = false;
        bool BMDilutionFixWidth = false;
        double BMDilutionRectLength = 0;
        double BMDilutionRectWidth = 0;
        double BMDilutionLeftSideLineAngle = 0;
        double BMDilutionLeftSideLineStartLatitude = 0;
        double BMDilutionLeftSideLineStartLongitude = 0;

        //       NoDecayUntreated
        double BMNoDecayUntreatedVolume = 0;
        double BMNoDecayUntreatedSurface = 0;
        double BMNoDecayUntreatedRadius = 0;
        double BMNoDecayUntreatedLeftSideDiameterLineAngle = 0;
        double BMNoDecayUntreatedCircleCenterLatitude = 0;
        double BMNoDecayUntreatedCircleCenterLongitude = 0;
        bool BMNoDecayUntreatedFixLength = false;
        bool BMNoDecayUntreatedFixWidth = false;
        double BMNoDecayUntreatedRectLength = 0;
        double BMNoDecayUntreatedRectWidth = 0;
        double BMNoDecayUntreatedLeftSideLineAngle = 0;
        double BMNoDecayUntreatedLeftSideLineStartLatitude = 0;
        double BMNoDecayUntreatedLeftSideLineStartLongitude = 0;

        //       NoDecayPreDis
        double BMNoDecayPreDisVolume = 0;
        double BMNoDecayPreDisSurface = 0;
        double BMNoDecayPreDisRadius = 0;
        double BMNoDecayPreDisLeftSideDiameterLineAngle = 0;
        double BMNoDecayPreDisCircleCenterLatitude = 0;
        double BMNoDecayPreDisCircleCenterLongitude = 0;
        bool BMNoDecayPreDisFixLength = false;
        bool BMNoDecayPreDisFixWidth = false;
        double BMNoDecayPreDisRectLength = 0;
        double BMNoDecayPreDisRectWidth = 0;
        double BMNoDecayPreDisLeftSideLineAngle = 0;
        double BMNoDecayPreDisLeftSideLineStartLatitude = 0;
        double BMNoDecayPreDisLeftSideLineStartLongitude = 0;

        //       DecayUntreated
        double BMDecayUntreatedVolume = 0;
        double BMDecayUntreatedSurface = 0;
        double BMDecayUntreatedRadius = 0;
        double BMDecayUntreatedLeftSideDiameterLineAngle = 0;
        double BMDecayUntreatedCircleCenterLatitude = 0;
        double BMDecayUntreatedCircleCenterLongitude = 0;
        bool BMDecayUntreatedFixLength = false;
        bool BMDecayUntreatedFixWidth = false;
        double BMDecayUntreatedRectLength = 0;
        double BMDecayUntreatedRectWidth = 0;
        double BMDecayUntreatedLeftSideLineAngle = 0;
        double BMDecayUntreatedLeftSideLineStartLatitude = 0;
        double BMDecayUntreatedLeftSideLineStartLongitude = 0;

        //       DecayPreDis
        double BMDecayPreDisVolume = 0;
        double BMDecayPreDisSurface = 0;
        double BMDecayPreDisRadius = 0;
        double BMDecayPreDisLeftSideDiameterLineAngle = 0;
        double BMDecayPreDisCircleCenterLatitude = 0;
        double BMDecayPreDisCircleCenterLongitude = 0;
        bool BMDecayPreDisFixLength = false;
        bool BMDecayPreDisFixWidth = false;
        double BMDecayPreDisRectLength = 0;
        double BMDecayPreDisRectWidth = 0;
        double BMDecayPreDisLeftSideLineAngle = 0;
        double BMDecayPreDisLeftSideLineStartLatitude = 0;
        double BMDecayPreDisLeftSideLineStartLongitude = 0;

        enum BoxModelResultType
        {
            Dilution = 1,
            NoDecayUntreated,
            NoDecayPreDis,
            DecayUntreated,
            DecayPreDis
        }

        // Infrastructure info variables
        string StoredType;
        string StoredCategory;
        int StoredTPID;
        int StoredLSID;
        int StoredSiteID;
        int StoredPrismID;
        int StoredOutfallPrismID;
        string StoredInfrastructureType;
        DateTime StoredDateOfConstruction;
        DateTime StoredDateOfRecentUpgrade;
        string StoredLocator;
        string StoredDatum;
        int StoredZone;
        double StoredEasting;
        double StoredNorthing;
        double StoredLatitude;
        double StoredLongitude;
        int StoredDesignPopulation;
        int StoredPopulationServed;
        double StoredDesignFlow;
        double StoredAverageFlow;
        double StoredPeakFlow;
        double StoredEstimatedFlow;
        string StoredOperatorName;
        string StoredOperatorTelephone;
        string StoredOperatorEmail;
        int StoredNumberOfVisitToPlantPerWeek;
        string StoredDisinfection;
        int StoredBODRequired;
        int StoredSSRequired;
        int StoredFCRequired;
        string StoredAlarmSystemType;
        string StoredCollectionSystemType;
        double StoredCombinedPercent;
        int StoredBypassFreqency;
        string StoredBypassTypeOrCause;
        int StoredBypassAverageTime;
        double StoredBypassNotificationTime;
        string StoredLagoonOrMachanical;
        double StoredOutfallEasting;
        double StoredOutfallNorthing;
        double StoredOutfallLatitude;
        double StoredOutfallLongitude;
        int StoredOutfallZone;
        string StoredOutfallDatum;
        double StoredOutfallDepthHigh;
        double StoredOutfallDepthLow;
        int StoredOutfallNumberOfPorts;
        double StoredOutfallPortDiameter;
        double StoredOutfallPortSpacing;
        double StoredOutfallPortElevation;
        int StoredOutfallVerticalAngle;
        int StoredOutfallHorizontalAngle;
        double StoredOutfallDecayRate;
        double StoredOutfallNearFieldVelocity;
        double StoredOutfallFarFieldVelocity;
        double StoredOutfallReceivingWaterSalinity;
        double StoredOutfallReceivingWaterTemperature;
        double StoredOutfallReceivingWaterFC;
        double StoredOutfallDistanceFromShore;
        string StoredOutfallReceivingWaterName;
        string StoredInputDataComments;
        string StoredOtherComments;
        #endregion

        public AutoRunVP()
        {
            cultureInfo = new CultureInfo("en-CA");

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            InitializeComponent();
            delimiter = delimStr.ToCharArray();

            // these functions are in MIKE.cs
            CreateNewM21FMWithEvents();
            CreateNewDfsWithEvents(Dfs.DFSType.DFS0, true);
            CreateNewKMZWithEvents(dfs, m21fm);

        }

        #region Functions
        private bool RunVisualPlumes()
        {
            try
            {
                string VisualPlumesExecutablePath = @"C:\Plumes\Plumes.exe";
                richTextBoxStatus.AppendText("Starting Visual Plumes.\r\n");
                richTextBoxStatus.AppendText("Trying to run [" + VisualPlumesExecutablePath + "].\r\n");

                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = VisualPlumesExecutablePath;
                pInfo.WindowStyle = ProcessWindowStyle.Normal;
                pInfo.UseShellExecute = true;
                processPlumes.StartInfo = pInfo;
                processPlumes.Start();
                processPlumes.WaitForInputIdle(2000);

                richTextBoxStatus.AppendText("Process for Visual Plumes was started.\r\n");
            }
            catch (Exception ex)
            {
                string ErrorMessage = "Error [" + ex.Message + "]";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage);
                return false;
            }

            return true;
        }
        private bool CheckIfVPRunning()
        {
            richTextBoxStatus.AppendText("Checking if Visual Plumes is running.\r\n");

            hWndPlumes = IntPtr.Zero;
            string VPCaption = "  Visual Plumes,   Ver. 1.0;   U.S. Environmental Protection Agency,  ERD-Athens,   ORD,   14 August 2001";

            if (DesktopChildrenWindowsList.Where(h => h.Title == VPCaption).Count() > 1)
            {
                MessageBox.Show("More than one copy of Visual Plumes is running. Only one is allowed.");
                richTextBoxStatus.AppendText("Too many copies of [" + VPCaption + "] are running.\r\n");
                return true;
            }
            WndHandleAndTitle wht = DesktopChildrenWindowsList.Where(h => h.Title == VPCaption).FirstOrDefault();
            if (wht != null)
            {
                hWndPlumes = wht.Handle;
                richTextBoxStatus.AppendText(VPCaption + " is running.\r\n");
                return true;
            }
            else
            {
                richTextBoxStatus.AppendText(VPCaption + " is NOT running.\r\n");
                hWndPlumes = IntPtr.Zero;
                return false;
            }
        }
        private bool CloseDialogBox()
        {
            FillDesktopWindowsChildrenList(false);
            MegaDoEvents();

            IntPtr TemphWnd = IntPtr.Zero;
            foreach (CloseCaptionAndCommand dtc in DialogToClose)
            {
                //if (DesktopChildrenWindowsList.Where(h => h.Title == dtc.Caption).Count() > 1)
                //{
                //    MessageBox.Show("More than one copy of " + dtc.Caption + " was found. Automatically closing the window might cause inappropriate action.");
                //    richTextBoxStatus.AppendText("More than one copy of " + dtc.Caption + " was found. Automatically closing the window might cause inappropriate action.\r\n");
                //    return true;
                //}
                foreach (WndHandleAndTitle wht in DesktopChildrenWindowsList.Where(h => h.Title == dtc.Caption))
                {
                    if (wht != null)
                    {
                        TemphWnd = wht.Handle;
                        richTextBoxStatus.AppendText("Trying to automatically close [" + dtc.Caption + "] with the command [" + dtc.Command + "].\r\n");
                        MegaDoEvents();
                        af.APISetForegroundWindow(TemphWnd);
                        int Counting = 0;
                        bool JumpOver = false;
                        while (af.APIGetForegroundWindow() != TemphWnd)
                        {
                            Counting += 1;
                            if (Counting > 2000)
                            {
                                JumpOver = true;
                                break;
                            }
                            MegaDoEvents();
                        }
                        if (!JumpOver)
                        {
                            SendKeys.SendWait(dtc.Command);

                            MegaDoEvents();
                            MegaDoEvents();
                        }
                    }
                }

                return true;
                //WndHandleAndTitle wht = DesktopChildrenWindowsList.Where(h => h.Title == dtc.Caption).FirstOrDefault();
            }
            return false;
        }
        private void SetTimerToCloseDialogBox(string Caption, string Command, int Interval, int NumberOfTry)
        {
            DialogToClose.Clear();
            CloseCaptionAndCommand ccc = new CloseCaptionAndCommand()
            {
                Caption = Caption,
                Command = Command
            };

            DialogToClose.Add(ccc);
            MegaDoEvents();
            timerCheckForDialogBoxToClose.Enabled = true;
            timerCheckForDialogBoxToClose.Interval = Interval;
            timerCheckForDialogBoxToClose.Start();
            return;
        }
        private void MegaDoEvents()
        {
            for (int i = 0; i < 20000; i++)
            {
                Application.DoEvents();
            }
        }
        private void LoadVP()
        {
            RunVisualPlumes();
            MegaDoEvents();

            FillDesktopWindowsChildrenList(false);

            // Visual Plumes is now running and we need to click a few dialog boxes
            if (checkBoxForJeff.Checked == true)
            {


                int countInformation = DesktopChildrenWindowsList.Where(u => u.Title == "Information").Count();
                while (countInformation > 1)
                {
                    List<WndHandleAndTitle> TempInformationList = new List<WndHandleAndTitle>();
                    TempInformationList = DesktopChildrenWindowsList.Where(u => u.Title == "Information").ToList<WndHandleAndTitle>();
                    foreach (WndHandleAndTitle wht in TempInformationList)
                    {
                        SetTimerToCloseDialogBox("Information", "{ENTER}", 1000, 3);
                        MegaDoEvents();
                    }
                    FillDesktopWindowsChildrenList(false);
                    countInformation = DesktopChildrenWindowsList.Where(u => u.Title == "Information").Count();
                }
            }
            else
            {
                WndHandleAndTitle wht = DesktopChildrenWindowsList.Where(u => u.Title == "Information").FirstOrDefault();
                while (wht != null)
                {
                    SetTimerToCloseDialogBox("Information", "{ENTER}", 1000, 3);
                    MegaDoEvents();
                    FillDesktopWindowsChildrenList(false);
                    wht = DesktopChildrenWindowsList.Where(u => u.Title == "Information").FirstOrDefault();
                }
            }
        }
        private void StartVP()
        {
            butRunVPScenarios.Enabled = true;
            int CountLoop = 0;
            if (!CopyAutoRunVPFiles())
            {
                String TempErr = "Error while copying Visual Plumes project files.";
                lblError.Text = TempErr;
                MessageBox.Show(TempErr);
                return;
            }

            FillDesktopWindowsChildrenList(false);
            if (CheckIfVPRunning())
            {
                String TempErr = "Please close the current Visual Plumes that is running.";
                lblError.Text = TempErr;
                MessageBox.Show(TempErr);
                return;
            }

            LoadVP();
            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            FillDesktopWindowsChildrenList(false);
            CountLoop = 0;
            if (!CheckIfVPRunning())
            {
                MegaDoEvents();
                FillDesktopWindowsChildrenList(false);
                CountLoop += 1;
                if (CountLoop > 100)
                {
                    MessageBox.Show("ERROR - Visual Plumes could not be started by the application");
                    return;
                }
            }

            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            if (!SethWndPlumesToolBar())
                return;

            if (!LoadAutoRunVPFiles())
                return;

            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            FillDesktopWindowsChildrenList(false);
            if (CheckIfVPRunning())
            {
                processPlumes.CloseMainWindow();
                MegaDoEvents();
            }
            else
            {
                MessageBox.Show("Visual Plumes should be running");
                return;
            }

            LoadVP();
            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            FillDesktopWindowsChildrenList(false);
            if (!CheckIfVPRunning())
            {
                richTextBoxStatus.AppendText("Visual Plumes could not be started.\r\n");
                return;
            }

            af.APISetForegroundWindow(hWndPlumes);

            MegaDoEvents();
            MegaDoEvents();
            MegaDoEvents();

            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            if (!SetHandles())
                return;

            for (int i = 0; i < 100; i++)
            {
                MegaDoEvents();
            }

            af.APISetForegroundWindow(this.Handle);

            richTextBoxStatus.AppendText("Success.\r\n");

            return;

        }
        private bool LoadAutoRunVPFiles()
        {
            int CountSearchForSaveDialogBox = 0;
            ClickOnVPLoadProject();
            MegaDoEvents();

            FillDesktopWindowsChildrenList(false);
            WndHandleAndTitle wht = DesktopChildrenWindowsList.Where(u => u.Title == "Project File").FirstOrDefault();
            while (wht == null)
            {
                MegaDoEvents();
                FillDesktopWindowsChildrenList(false);
                wht = DesktopChildrenWindowsList.Where(u => u.Title == "Project File").FirstOrDefault();
                CountSearchForSaveDialogBox += 1;
                if (CountSearchForSaveDialogBox > 100)
                {
                    richTextBoxStatus.AppendText("ERROR - Could not find the [Project File] dialog box\r\n");
                    return false;
                }
            }

            IntPtr hWndFileNameTextBox = af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(wht.Handle, GW_CHILD),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT);

            richTextBoxStatus.AppendText("hWndFileNameTextBox = [" + hWndFileNameTextBox + "]\r\n");
            if (hWndFileNameTextBox != IntPtr.Zero)
            {
                af.APISetForegroundWindow(wht.Handle);
                MegaDoEvents();
                while (af.APIGetForegroundWindow() != wht.Handle)
                {
                    MegaDoEvents();
                }
                af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                for (int i = 0; i < 100; i++)
                {
                    SendKeys.SendWait("{BACKSPACE}{DELETE}");
                }
                SendKeys.SendWait(@"C:\Plumes\AutoRun.vpp.db");
            }
            else
            {
                richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                return false;
            }

            IntPtr hWndOpenButton = af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(wht.Handle, GW_CHILD),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT),
                GW_HWNDNEXT);
            richTextBoxStatus.AppendText("hWndOpenButton = [" + hWndOpenButton + "]\r\n");

            if (hWndOpenButton != IntPtr.Zero)
            {
                af.APISetForegroundWindow(wht.Handle);
                MegaDoEvents();
                while (af.APIGetForegroundWindow() != wht.Handle)
                {
                    MegaDoEvents();
                }
                af.APIPostMouseClick(hWndOpenButton, 10, 10);
            }
            else
            {
                richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                return false;
            }

            return true;
        }
        private void ClickOnVPLoadProject()
        {
            SelectTextOutputTab();
            af.APISetForegroundWindow(hWndPlumes);

            af.APIPostMouseClick(hWndPlumesToolBar, 45, 10);
            MegaDoEvents();
        }
        private bool CopyAutoRunVPFiles()
        {
            DirectoryInfo di = new DirectoryInfo(@"C:\Plumes\AutoRunVP\");

            if (!di.Exists)
            {
                MessageBox.Show(@"You need to create a subdirectory called [C:\Plumes\AutoRunVP\]");
                return false;
            }

            FileInfo fi = new FileInfo(@"C:\Plumes\AutoRunVP\AutoRun.vpp.db");
            if (!fi.Exists)
            {
                af.APISetForegroundWindow(this.Handle);
                MessageBox.Show(@"File Missing: [C:\Plumes\AutoRunVP\AutoRun.vpp.db]. Please make sure it exist and in the right subdirectory.");
                return false;
            }

            File.Copy(@"C:\Plumes\AutoRunVP\AutoRun.vpp.db", @"C:\Plumes\AutoRun.vpp.db", true);

            fi = new FileInfo(@"C:\Plumes\AutoRunVP\AutoRun.001.db");
            if (!fi.Exists)
            {
                af.APISetForegroundWindow(this.Handle);
                MessageBox.Show(@"File Missing: [C:\Plumes\AutoRunVP\AutoRun.001.db]. Please make sure it exist and in the right subdirectory.");
                return false;
            }

            File.Copy(@"C:\Plumes\AutoRunVP\AutoRun.001.db", @"C:\Plumes\AutoRun.001.db", true);

            fi = new FileInfo(@"C:\Plumes\AutoRunVP\AutoRun.lst");
            if (!fi.Exists)
            {
                af.APISetForegroundWindow(this.Handle);
                MessageBox.Show(@"File Missing: [C:\Plumes\AutoRunVP\AutoRun.lst]. Please make sure it exist and in the right subdirectory.");
                return false;
            }

            File.Copy(@"C:\Plumes\AutoRunVP\AutoRun.lst", @"C:\Plumes\AutoRun.lst", true);

            MegaDoEvents();

            return true;
        }
        private void SelectDiffuserTab()
        {
            af.APISendMouseClick(hWndPlumesTab, 33, 3);
            MegaDoEvents();
        }
        private void SelectAmbientTab()
        {
            af.APISendMouseClick(hWndPlumesTab, 230, 3);
            MegaDoEvents();
        }
        private void SelectSpecialSettingsTab()
        {
            af.APISendMouseClick(hWndPlumesTab, 363, 3);
            MegaDoEvents();
        }
        private void SelectTextOutputTab()
        {
            af.APISendMouseClick(hWndPlumesTab, 443, 3);
            MegaDoEvents();
        }
        private void SelectGraphicOutputTab()
        {
            af.APISendMouseClick(hWndPlumesTab, 533, 3);
            MegaDoEvents();
        }
        private bool SethWndPlumes()
        {
            hWndPlumes = IntPtr.Zero;
            hWndPlumes = af.APIFindWindowEx(IntPtr.Zero, IntPtr.Zero, "TMainform", "  Visual Plumes,   Ver. 1.0;   U.S. Environmental Protection Agency,  ERD-Athens,   ORD,   14 August 2001");

            if (hWndPlumes == IntPtr.Zero)
            {
                if (CheckIfVPRunning())
                {
                    // try again.
                    hWndPlumes = af.APIFindWindowEx(IntPtr.Zero, IntPtr.Zero, "TMainform", "  Visual Plumes,   Ver. 1.0;   U.S. Environmental Protection Agency,  ERD-Athens,   ORD,   14 August 2001");

                    if (hWndPlumes == IntPtr.Zero)
                    {
                        string ErrorMessage = "hWndPlumes not found";
                        lblError.Text = ErrorMessage;
                        richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                        return false;
                    }
                }
            }

            return true;
        }
        private bool SethWndPlumesToolBar()
        {
            MegaDoEvents();
            hWndPlumesToolBar = IntPtr.Zero;
            hWndPlumesToolBar = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT);

            if (hWndPlumesToolBar == IntPtr.Zero)
            {
                if (CheckIfVPRunning())
                {
                    MegaDoEvents();
                    hWndPlumesToolBar = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT);

                    if (hWndPlumesToolBar == IntPtr.Zero)
                    {
                        string ErrorMessage = "hWndToolBar not found";
                        lblError.Text = ErrorMessage;
                        richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                        return false;
                    }
                }
            }

            return true;
        }
        private bool SethWndPlumesTab()
        {
            MegaDoEvents();
            hWndPlumesTab = IntPtr.Zero;
            hWndPlumesTab = af.APIFindWindowEx(hWndPlumes, IntPtr.Zero, "TPageControl", "");
            //af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT);

            if (hWndPlumesTab == IntPtr.Zero)
            {
                if (CheckIfVPRunning())
                {
                    MegaDoEvents();
                    hWndPlumesTab = af.APIFindWindowEx(hWndPlumes, IntPtr.Zero, "TPageControl", "");
                    //                    hWndPlumesTab = af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT);

                    if (hWndPlumesTab == IntPtr.Zero)
                    {
                        string ErrorMessage = "hWndPlumesTab not found";
                        lblError.Text = ErrorMessage;
                        richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                        return false;
                    }
                }
            }

            for (int i = 2; i < 500; i += 20)
            {
                af.APISetForegroundWindow(hWndPlumes);
                while (af.APIGetForegroundWindow() != hWndPlumes)
                {
                    MegaDoEvents();
                }
                af.APISendMouseClick(hWndPlumesTab, i, 2);
                af.APISendMouseClick(hWndPlumesTab, i, 2);
                MegaDoEvents();
            }

            hWndTextOutputTab = af.APIFindWindowEx(hWndPlumesTab, IntPtr.Zero, "TTabSheet", "Text Output");
            //af.APIGetWindow(hWndPlumesTab, GW_CHILD);
            if (hWndTextOutputTab == IntPtr.Zero)
            {
                string ErrorMessage = "hWndTextOutputTab not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            af.APISetForegroundWindow(hWndPlumes);
            SelectTextOutputTab();

            hWndDiffuserTab = af.APIFindWindowEx(hWndPlumesTab, IntPtr.Zero, "TTabSheet", @"Diffuser: AutoRun.vpp.db");
            //af.APIGetWindow(af.APIGetWindow(hWndPlumesTab, GW_CHILD), GW_HWNDNEXT);
            if (hWndDiffuserTab == IntPtr.Zero)
            {
                string ErrorMessage = "hWndDiffuserTab not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            af.APISetForegroundWindow(hWndPlumes);
            SelectDiffuserTab();

            hWndAmbientTab = af.APIFindWindowEx(hWndPlumesTab, IntPtr.Zero, "TTabSheet", @"Ambient: C:\Plumes\AutoRun.001.db");
            //af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumesTab, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT);
            if (hWndAmbientTab == IntPtr.Zero)
            {
                string ErrorMessage = "hWndAmbientTab not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            af.APISetForegroundWindow(hWndPlumes);
            SelectAmbientTab();

            hWndGraphicOutputTab = af.APIFindWindowEx(hWndPlumesTab, IntPtr.Zero, "TTabSheet", "Graphical Output");
            //af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumesTab, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT);
            if (hWndGraphicOutputTab == IntPtr.Zero)
            {
                string ErrorMessage = "hWndGraphicOutputTab not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            af.APISetForegroundWindow(hWndPlumes);
            SelectGraphicOutputTab();

            hWndSpecialSettingTab = af.APIFindWindowEx(hWndPlumesTab, IntPtr.Zero, "TTabSheet", "Special Settings");
            //af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumesTab, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT);
            if (hWndSpecialSettingTab == IntPtr.Zero)
            {
                string ErrorMessage = "hWndSpecialSettingTab not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            af.APISetForegroundWindow(hWndPlumes);
            return true;
        }
        private bool SethWndDiffuserFlowMixZoneDataGrid()
        {
            SelectDiffuserTab();
            MegaDoEvents();
            hWndDiffuserFlowMixZoneDataGrid = IntPtr.Zero;

            hWndDiffuserFlowMixZoneDataGrid = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT);

            if (hWndDiffuserFlowMixZoneDataGrid == IntPtr.Zero)
            {
                SelectDiffuserTab();
                MegaDoEvents();
                hWndDiffuserFlowMixZoneDataGrid = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT);

                if (hWndDiffuserFlowMixZoneDataGrid == IntPtr.Zero)
                {
                    string ErrorMessage = "hWndDiffuserFlowMixZoneDataGrid not found";
                    lblError.Text = ErrorMessage;
                    richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                    return false;
                }
            }

            return true;
        }
        private bool SethWndDiffuserFlowMixZoneDataGridEdit()
        {
            SelectDiffuserTab();
            MegaDoEvents();
            hWndDiffuserFlowMixZoneDataGridEdit = IntPtr.Zero;
            hWndDiffuserFlowMixZoneDataGridEdit = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_CHILD);

            if (hWndDiffuserFlowMixZoneDataGridEdit == IntPtr.Zero)
            {
                // we need to try to set focus to the DataGrid so the Edit portion becomes available
                SelectDiffuserTab();
                af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 20, 10);
                af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 20, 10);
                af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);

                MegaDoEvents();

                hWndDiffuserFlowMixZoneDataGridEdit = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_CHILD);

                if (hWndDiffuserFlowMixZoneDataGridEdit == IntPtr.Zero)
                {
                    string ErrorMessage = "hWndDiffuserFlowMixZoneDataGridEdit not found";
                    lblError.Text = ErrorMessage;
                    richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                    return false;
                }
            }

            return true;
        }
        private bool SethWndAmbientTabPanelDataGrid()
        {
            SelectAmbientTab();
            MegaDoEvents();
            IntPtr hWndambpanel = af.APIFindWindowEx(hWndAmbientTab, IntPtr.Zero, "TPanel", "ambpanel");
            if (hWndambpanel == IntPtr.Zero)
            {
                string ErrorMessage = "hWndambpanel not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }
            IntPtr hWndTPanel1 = af.APIGetWindow(hWndambpanel, GW_CHILD);
            if (hWndTPanel1 == IntPtr.Zero)
            {
                string ErrorMessage = "hWndTPanel1 not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }
            IntPtr hWndTPanel2 = af.APIGetWindow(af.APIGetWindow(hWndambpanel, GW_CHILD), GW_HWNDNEXT);
            if (hWndTPanel2 == IntPtr.Zero)
            {
                string ErrorMessage = "hWndTPanel2 not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            hWndAmbientTabPanelDataGrid = IntPtr.Zero;
            MegaDoEvents();

            hWndAmbientTabPanelDataGrid = af.APIFindWindowEx(hWndTPanel1, IntPtr.Zero, "TDBGrid", "");
            //af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_CHILD), GW_CHILD);

            if (hWndAmbientTabPanelDataGrid == IntPtr.Zero)
            {
                MegaDoEvents();
                hWndAmbientTabPanelDataGrid = af.APIFindWindowEx(hWndTPanel2, IntPtr.Zero, "TDBGrid", "");

                if (hWndAmbientTabPanelDataGrid == IntPtr.Zero)
                {
                    string ErrorMessage = "hWndAmbientTabPanelDataGrid not found";
                    lblError.Text = ErrorMessage;
                    richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                    return false;
                }
            }

            return true;
        }
        private bool SethWndAmbientTabPanelDataGridEdit()
        {
            SelectAmbientTab();
            MegaDoEvents();
            hWndAmbientTabPanelDataGridEdit = IntPtr.Zero;

            hWndAmbientTabPanelDataGridEdit = af.APIGetWindow(hWndAmbientTabPanelDataGrid, GW_CHILD);
            //af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_CHILD), GW_CHILD), GW_CHILD);

            if (hWndAmbientTabPanelDataGridEdit == IntPtr.Zero)
            {
                SelectAmbientTab();
                MegaDoEvents();
                af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 20, 10);
                af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 20, 10);
                //af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 20, 10);
                //af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);

                MegaDoEvents();

                hWndAmbientTabPanelDataGridEdit = af.APIGetWindow(hWndAmbientTabPanelDataGrid, GW_CHILD);

                if (hWndAmbientTabPanelDataGridEdit == IntPtr.Zero)
                {
                    string ErrorMessage = "hWndAmbientTabPanelDataGridEdit not found";
                    lblError.Text = ErrorMessage;
                    richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                    return false;
                }

            }

            return true;
        }
        private bool SethWndDiffuserProjectTextBox()
        {
            SelectDiffuserTab();

            hWndDiffuserProjectTextBox = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT);

            if (hWndDiffuserProjectTextBox == IntPtr.Zero)
            {
                string ErrorMessage = "hWndDiffuserProjectTextBox not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            return true;
        }
        private bool SethWndTextOutputClearButton()
        {
            SelectTextOutputTab();
            MegaDoEvents();

            hWndTextOutputClearButton = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD);

            if (hWndTextOutputClearButton == IntPtr.Zero)
            {
                string ErrorMessage = "hWndTextOutputClearButton not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            return true;
        }
        private bool SethWndTextOutputResultTextBox()
        {
            SelectTextOutputTab();

            hWndTextOutputResultTextBox = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(hWndPlumes, GW_CHILD), GW_HWNDNEXT), GW_CHILD), GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_CHILD);

            if (hWndTextOutputResultTextBox == IntPtr.Zero)
            {
                string ErrorMessage = "hWndTextOutputResultTextBox not found";
                lblError.Text = ErrorMessage;
                richTextBoxStatus.AppendText(ErrorMessage + "\r\n");
                return false;
            }

            return true;
        }
        private bool SetHandles()
        {
            if (!SethWndPlumes())
                return false;
            if (!SethWndPlumesToolBar())
                return false;
            if (!SethWndPlumesTab())
                return false;
            if (!SethWndAmbientTabPanelDataGrid())
                return false;
            if (!SethWndAmbientTabPanelDataGridEdit())
                return false;
            if (!SethWndDiffuserFlowMixZoneDataGrid())
                return false;
            if (!SethWndDiffuserFlowMixZoneDataGridEdit())
                return false;
            if (!SethWndDiffuserProjectTextBox())
                return false;
            if (!SethWndTextOutputClearButton())
                return false;
            if (!SethWndTextOutputResultTextBox())
                return false;

            SelectDiffuserTab();
            return true;
        }
        private void ClickOnVPSaveResults()
        {
            SelectTextOutputTab();
            af.APISetForegroundWindow(hWndPlumes);

            af.APIPostMouseClick(hWndPlumesToolBar, 90, 10);
            MegaDoEvents();
        }
        private void SetAmbientValues(int Row)
        {
            MeasurementDepthValues[Row - 1][0] = textBoxMeasurementDepthStart.Text.Trim();
            MeasurementDepthValues[Row - 1][1] = textBoxMeasurementDepthEnd.Text.Trim();
            MeasurementDepthValues[Row - 1][2] = textBoxMeasurementDepthSteps.Text.Trim();

            CurrentSpeedValues[Row - 1][0] = textBoxCurrentSpeedStart.Text.Trim();
            CurrentSpeedValues[Row - 1][1] = textBoxCurrentSpeedEnd.Text.Trim();
            CurrentSpeedValues[Row - 1][2] = textBoxCurrentSpeedSteps.Text.Trim();

            CurrentDirectionValues[Row - 1][0] = textBoxCurrentDirectionStart.Text.Trim();
            CurrentDirectionValues[Row - 1][1] = textBoxCurrentDirectionEnd.Text.Trim();
            CurrentDirectionValues[Row - 1][2] = textBoxCurrentDirectionSteps.Text.Trim();

            AmbientSalinityValues[Row - 1][0] = textBoxAmbientSalinityStart.Text.Trim();
            AmbientSalinityValues[Row - 1][1] = textBoxAmbientSalinityEnd.Text.Trim();
            AmbientSalinityValues[Row - 1][2] = textBoxAmbientSalinitySteps.Text.Trim();

            AmbientTemperatureValues[Row - 1][0] = textBoxAmbientTemperatureStart.Text.Trim();
            AmbientTemperatureValues[Row - 1][1] = textBoxAmbientTemperatureEnd.Text.Trim();
            AmbientTemperatureValues[Row - 1][2] = textBoxAmbientTemperatureSteps.Text.Trim();

            BackgroundConcentrationValues[Row - 1][0] = textBoxBackgroundConcentrationStart.Text.Trim();
            BackgroundConcentrationValues[Row - 1][1] = textBoxBackgroundConcentrationEnd.Text.Trim();
            BackgroundConcentrationValues[Row - 1][2] = textBoxBackgroundConcentrationSteps.Text.Trim();

            PollutantDecayRateValues[Row - 1][0] = textBoxPollutantDecayRateStart.Text.Trim();
            PollutantDecayRateValues[Row - 1][1] = textBoxPollutantDecayRateEnd.Text.Trim();
            PollutantDecayRateValues[Row - 1][2] = textBoxPollutantDecayRateSteps.Text.Trim();

            FarFieldCurrentSpeedValues[Row - 1][0] = textBoxFarFieldCurrentSpeedStart.Text.Trim();
            FarFieldCurrentSpeedValues[Row - 1][1] = textBoxFarFieldCurrentSpeedEnd.Text.Trim();
            FarFieldCurrentSpeedValues[Row - 1][2] = textBoxFarFieldCurrentSpeedSteps.Text.Trim();

            FarFieldCurrentDirectionValues[Row - 1][0] = textBoxFarFieldCurrentDirectionStart.Text.Trim();
            FarFieldCurrentDirectionValues[Row - 1][1] = textBoxFarFieldCurrentDirectionEnd.Text.Trim();
            FarFieldCurrentDirectionValues[Row - 1][2] = textBoxFarFieldCurrentDirectionSteps.Text.Trim();

            FarFieldDiffusionCoefficientValues[Row - 1][0] = textBoxFarFieldDiffusionCoefficientStart.Text.Trim();
            FarFieldDiffusionCoefficientValues[Row - 1][1] = textBoxFarFieldDiffusionCoefficientEnd.Text.Trim();
            FarFieldDiffusionCoefficientValues[Row - 1][2] = textBoxFarFieldDiffusionCoefficientSteps.Text.Trim();
        }
        private void ReadAmbientValues(int Row)
        {
            textBoxMeasurementDepthStart.Text = MeasurementDepthValues[Row - 1][0].ToString().Trim();
            textBoxMeasurementDepthEnd.Text = MeasurementDepthValues[Row - 1][1].ToString().Trim();
            textBoxMeasurementDepthSteps.Text = MeasurementDepthValues[Row - 1][2].ToString().Trim();

            textBoxCurrentSpeedStart.Text = CurrentSpeedValues[Row - 1][0].ToString().Trim();
            textBoxCurrentSpeedEnd.Text = CurrentSpeedValues[Row - 1][1].ToString().Trim();
            textBoxCurrentSpeedSteps.Text = CurrentSpeedValues[Row - 1][2].ToString().Trim();

            textBoxCurrentDirectionStart.Text = CurrentDirectionValues[Row - 1][0].ToString().Trim();
            textBoxCurrentDirectionEnd.Text = CurrentDirectionValues[Row - 1][1].ToString().Trim();
            textBoxCurrentDirectionSteps.Text = CurrentDirectionValues[Row - 1][2].ToString().Trim();

            textBoxAmbientSalinityStart.Text = AmbientSalinityValues[Row - 1][0].ToString().Trim();
            textBoxAmbientSalinityEnd.Text = AmbientSalinityValues[Row - 1][1].ToString().Trim();
            textBoxAmbientSalinitySteps.Text = AmbientSalinityValues[Row - 1][2].ToString().Trim();

            textBoxAmbientTemperatureStart.Text = AmbientTemperatureValues[Row - 1][0].ToString().Trim();
            textBoxAmbientTemperatureEnd.Text = AmbientTemperatureValues[Row - 1][1].ToString().Trim();
            textBoxAmbientTemperatureSteps.Text = AmbientTemperatureValues[Row - 1][2].ToString().Trim();

            textBoxBackgroundConcentrationStart.Text = BackgroundConcentrationValues[Row - 1][0].ToString().Trim();
            textBoxBackgroundConcentrationEnd.Text = BackgroundConcentrationValues[Row - 1][1].ToString().Trim();
            textBoxBackgroundConcentrationSteps.Text = BackgroundConcentrationValues[Row - 1][2].ToString().Trim();

            textBoxPollutantDecayRateStart.Text = PollutantDecayRateValues[Row - 1][0].ToString().Trim();
            textBoxPollutantDecayRateEnd.Text = PollutantDecayRateValues[Row - 1][1].ToString().Trim();
            textBoxPollutantDecayRateSteps.Text = PollutantDecayRateValues[Row - 1][2].ToString().Trim();

            textBoxFarFieldCurrentSpeedStart.Text = FarFieldCurrentSpeedValues[Row - 1][0].ToString().Trim();
            textBoxFarFieldCurrentSpeedEnd.Text = FarFieldCurrentSpeedValues[Row - 1][1].ToString().Trim();
            textBoxFarFieldCurrentSpeedSteps.Text = FarFieldCurrentSpeedValues[Row - 1][2].ToString().Trim();

            textBoxFarFieldCurrentDirectionStart.Text = FarFieldCurrentDirectionValues[Row - 1][0].ToString().Trim();
            textBoxFarFieldCurrentDirectionEnd.Text = FarFieldCurrentDirectionValues[Row - 1][1].ToString().Trim();
            textBoxFarFieldCurrentDirectionSteps.Text = FarFieldCurrentDirectionValues[Row - 1][2].ToString().Trim();

            textBoxFarFieldDiffusionCoefficientStart.Text = FarFieldDiffusionCoefficientValues[Row - 1][0].ToString().Trim();
            textBoxFarFieldDiffusionCoefficientEnd.Text = FarFieldDiffusionCoefficientValues[Row - 1][1].ToString().Trim();
            textBoxFarFieldDiffusionCoefficientSteps.Text = FarFieldDiffusionCoefficientValues[Row - 1][2].ToString().Trim();

        }
        private void SetDiffuserValues()
        {
            PortDiameterValues[0] = textBoxPortDiameterStart.Text.Trim();
            PortDiameterValues[1] = textBoxPortDiameterEnd.Text.Trim();
            PortDiameterValues[2] = textBoxPortDiameterSteps.Text.Trim();

            PortElevationValues[0] = textBoxPortElevationStart.Text.Trim();
            PortElevationValues[1] = textBoxPortElevationEnd.Text.Trim();
            PortElevationValues[2] = textBoxPortElevationSteps.Text.Trim();

            VerticalAngleValues[0] = textBoxVerticalAngleStart.Text.Trim();
            VerticalAngleValues[1] = textBoxVerticalAngleEnd.Text.Trim();
            VerticalAngleValues[2] = textBoxVerticalAngleSteps.Text.Trim();

            HorizontalAngleValues[0] = textBoxHorizontalAngleStart.Text.Trim();
            HorizontalAngleValues[1] = textBoxHorizontalAngleEnd.Text.Trim();
            HorizontalAngleValues[2] = textBoxHorizontalAngleSteps.Text.Trim();

            NumberOfPortsValues[0] = textBoxNumberOfPortsStart.Text.Trim();
            NumberOfPortsValues[1] = textBoxNumberOfPortsEnd.Text.Trim();
            NumberOfPortsValues[2] = textBoxNumberOfPortsSteps.Text.Trim();

            PortSpacingValues[0] = textBoxPortSpacingStart.Text.Trim();
            PortSpacingValues[1] = textBoxPortSpacingEnd.Text.Trim();
            PortSpacingValues[2] = textBoxPortSpacingSteps.Text.Trim();

            AcuteMixZoneValues[0] = textBoxAcuteMixZoneStart.Text.Trim();
            AcuteMixZoneValues[1] = textBoxAcuteMixZoneEnd.Text.Trim();
            AcuteMixZoneValues[2] = textBoxAcuteMixZoneSteps.Text.Trim();

            ChronicMixZoneValues[0] = textBoxChronicMixZoneStart.Text.Trim();
            ChronicMixZoneValues[1] = textBoxChronicMixZoneEnd.Text.Trim();
            ChronicMixZoneValues[2] = textBoxChronicMixZoneSteps.Text.Trim();

            PortDepthValues[0] = textBoxPortDepthStart.Text.Trim();
            PortDepthValues[1] = textBoxPortDepthEnd.Text.Trim();
            PortDepthValues[2] = textBoxPortDepthSteps.Text.Trim();

            EffluentFlowValues[0] = textBoxEffluentFlowStart.Text.Trim();
            EffluentFlowValues[1] = textBoxEffluentFlowEnd.Text.Trim();
            EffluentFlowValues[2] = textBoxEffluentFlowSteps.Text.Trim();

            EffluentSalinityValues[0] = textBoxEffluentSalinityStart.Text.Trim();
            EffluentSalinityValues[1] = textBoxEffluentSalinityEnd.Text.Trim();
            EffluentSalinityValues[2] = textBoxEffluentSalinitySteps.Text.Trim();

            EffluentTemperatureValues[0] = textBoxEffluentTemperatureStart.Text.Trim();
            EffluentTemperatureValues[1] = textBoxEffluentTemperatureEnd.Text.Trim();
            EffluentTemperatureValues[2] = textBoxEffluentTemperatureSteps.Text.Trim();

            EffluentConcentrationValues[0] = textBoxEffluentConcentrationStart.Text.Trim();
            EffluentConcentrationValues[1] = textBoxEffluentConcentrationEnd.Text.Trim();
            EffluentConcentrationValues[2] = textBoxEffluentConcentrationSteps.Text.Trim();

            FroudeNumberValues[0] = "";
            FroudeNumberValues[1] = "";
            FroudeNumberValues[2] = "";

            EffluentVelocityValues[0] = "";
            EffluentVelocityValues[1] = "";
            EffluentVelocityValues[2] = "";
        }
        private void ReadDiffuserValues()
        {
            textBoxPortDiameterStart.Text = PortDiameterValues[0].ToString().Trim();
            textBoxPortDiameterEnd.Text = PortDiameterValues[1].ToString().Trim();
            textBoxPortDiameterSteps.Text = PortDiameterValues[2].ToString().Trim();

            textBoxPortElevationStart.Text = PortElevationValues[0].ToString().Trim();
            textBoxPortElevationEnd.Text = PortElevationValues[1].ToString().Trim();
            textBoxPortElevationSteps.Text = PortElevationValues[2].ToString().Trim();

            textBoxVerticalAngleStart.Text = VerticalAngleValues[0].ToString().Trim();
            textBoxVerticalAngleEnd.Text = VerticalAngleValues[1].ToString().Trim();
            textBoxVerticalAngleSteps.Text = VerticalAngleValues[2].ToString().Trim();

            textBoxHorizontalAngleStart.Text = HorizontalAngleValues[0].ToString().Trim();
            textBoxHorizontalAngleEnd.Text = HorizontalAngleValues[1].ToString().Trim();
            textBoxHorizontalAngleSteps.Text = HorizontalAngleValues[2].ToString().Trim();

            textBoxNumberOfPortsStart.Text = NumberOfPortsValues[0].ToString().Trim();
            textBoxNumberOfPortsEnd.Text = NumberOfPortsValues[1].ToString().Trim();
            textBoxNumberOfPortsSteps.Text = NumberOfPortsValues[2].ToString().Trim();

            textBoxPortSpacingStart.Text = PortSpacingValues[0].ToString().Trim();
            textBoxPortSpacingEnd.Text = PortSpacingValues[1].ToString().Trim();
            textBoxPortSpacingSteps.Text = PortSpacingValues[2].ToString().Trim();

            textBoxAcuteMixZoneStart.Text = AcuteMixZoneValues[0].ToString().Trim();
            textBoxAcuteMixZoneEnd.Text = AcuteMixZoneValues[1].ToString().Trim();
            textBoxAcuteMixZoneSteps.Text = AcuteMixZoneValues[2].ToString().Trim();

            textBoxChronicMixZoneStart.Text = ChronicMixZoneValues[0].ToString().Trim();
            textBoxChronicMixZoneEnd.Text = ChronicMixZoneValues[1].ToString().Trim();
            textBoxChronicMixZoneSteps.Text = ChronicMixZoneValues[2].ToString().Trim();

            textBoxPortDepthStart.Text = PortDepthValues[0].ToString().Trim();
            textBoxPortDepthEnd.Text = PortDepthValues[1].ToString().Trim();
            textBoxPortDepthSteps.Text = PortDepthValues[2].ToString().Trim();

            textBoxEffluentFlowStart.Text = EffluentFlowValues[0].ToString().Trim();
            textBoxEffluentFlowEnd.Text = EffluentFlowValues[1].ToString().Trim();
            textBoxEffluentFlowSteps.Text = EffluentFlowValues[2].ToString().Trim();

            textBoxEffluentSalinityStart.Text = EffluentSalinityValues[0].ToString().Trim();
            textBoxEffluentSalinityEnd.Text = EffluentSalinityValues[1].ToString().Trim();
            textBoxEffluentSalinitySteps.Text = EffluentSalinityValues[2].ToString().Trim();

            textBoxEffluentTemperatureStart.Text = EffluentTemperatureValues[0].ToString().Trim();
            textBoxEffluentTemperatureEnd.Text = EffluentTemperatureValues[1].ToString().Trim();
            textBoxEffluentTemperatureSteps.Text = EffluentTemperatureValues[2].ToString().Trim();

            textBoxEffluentConcentrationStart.Text = EffluentConcentrationValues[0].ToString().Trim();
            textBoxEffluentConcentrationEnd.Text = EffluentConcentrationValues[1].ToString().Trim();
            textBoxEffluentConcentrationSteps.Text = EffluentConcentrationValues[2].ToString().Trim();
        }
        private void DiffuserEnterData(DiffuserVariable dv, string VariableText)
        {
            SelectDiffuserTab();
            if (dv == DiffuserVariable.PortDiameter)
                af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 70, 10);
            else
                af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 20, 10);

            switch (dv)
            {
                case DiffuserVariable.PortDiameter:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 20, 10);
                    break;
                case DiffuserVariable.PortElevation:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 125, 10);
                    break;
                case DiffuserVariable.VerticalAngle:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 175, 10);
                    break;
                case DiffuserVariable.HorizontalAngle:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 225, 10);
                    break;
                case DiffuserVariable.NumberOfPorts:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 275, 10);
                    break;
                case DiffuserVariable.PortSpacing:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 330, 10);
                    break;
                case DiffuserVariable.AcuteMixZone:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 535, 10);
                    break;
                case DiffuserVariable.ChronicMixZone:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 585, 10);
                    break;
                case DiffuserVariable.PortDepth:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 635, 10);
                    break;
                case DiffuserVariable.EffluentFlow:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 690, 10);
                    break;
                case DiffuserVariable.EffluentSalinity:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 735, 10);
                    break;
                case DiffuserVariable.EffluentTemperature:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 790, 10);
                    break;
                case DiffuserVariable.EffluentConcentration:
                    af.APISendMouseClick(hWndDiffuserFlowMixZoneDataGrid, 835, 10);
                    break;
                default:
                    MessageBox.Show("Error in DiffuserEnterData - Case not found [" + dv + "]");
                    return;
            }

            af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);
            for (int i = 0; i < 10; i++)
            {
                af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)Keys.Delete, 0);
                af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)Keys.Back, 0);
            }
            foreach (char k in VariableText)
            {
                af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)k, 0);
            }
            af.APISendMessage(hWndDiffuserFlowMixZoneDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);
        }
        private void DiffuserFillValues()
        {
            DiffuserEnterData(DiffuserVariable.PortDiameter, PortDiameterValues[0]);
            DiffuserEnterData(DiffuserVariable.PortDepth, PortDepthValues[0]);
            DiffuserEnterData(DiffuserVariable.VerticalAngle, VerticalAngleValues[0]);
            DiffuserEnterData(DiffuserVariable.HorizontalAngle, HorizontalAngleValues[0]);
            DiffuserEnterData(DiffuserVariable.NumberOfPorts, NumberOfPortsValues[0]);
            DiffuserEnterData(DiffuserVariable.PortSpacing, PortSpacingValues[0]);
            DiffuserEnterData(DiffuserVariable.AcuteMixZone, AcuteMixZoneValues[0]);
            DiffuserEnterData(DiffuserVariable.ChronicMixZone, ChronicMixZoneValues[0]);
            DiffuserEnterData(DiffuserVariable.PortElevation, PortElevationValues[0]);
            DiffuserEnterData(DiffuserVariable.EffluentFlow, EffluentFlowValues[0]);
            DiffuserEnterData(DiffuserVariable.EffluentSalinity, EffluentSalinityValues[0]);
            DiffuserEnterData(DiffuserVariable.EffluentTemperature, EffluentTemperatureValues[0]);
            DiffuserEnterData(DiffuserVariable.EffluentConcentration, EffluentConcentrationValues[0]);
        }
        private void AmbientEnterData(AmbientVariable av, string VariableText, int Row)
        {
            SelectAmbientTab();
            if (av == AmbientVariable.MeasurementDepth)
                af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 100, 10);
            else
                af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 20, 10);

            switch (av)
            {
                case AmbientVariable.MeasurementDepth:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 20, 10);
                    break;
                case AmbientVariable.CurrentSpeeds:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 100, 10);
                    break;
                case AmbientVariable.CurrentDirections:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 170, 10);
                    break;
                case AmbientVariable.AmbientSalinity:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 250, 10);
                    break;
                case AmbientVariable.AmbientTemperature:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 330, 10);
                    break;
                case AmbientVariable.BackgroundConcentration:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 405, 10);
                    break;
                case AmbientVariable.PollutantDecayRate:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 475, 10);
                    break;
                case AmbientVariable.FarFieldCurrentSpeed:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 550, 10);
                    break;
                case AmbientVariable.FarFieldCurrentDirection:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 630, 10);
                    break;
                case AmbientVariable.FarFieldDiffusionCoefficient:
                    af.APISendMouseClick(hWndAmbientTabPanelDataGrid, 705, 10);
                    break;
                default:
                    MessageBox.Show("Error in AmbientEnterData - Case not found [" + av + "]");
                    return;
            }

            MegaDoEvents();

            for (int i = 1; i < Row; i++)
            {
                af.APISendMessage(hWndAmbientTabPanelDataGrid, WM_KEYDOWN, (int)Keys.Down, 0);
            }

            af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);
            for (int i = 0; i < 10; i++)
            {
                af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)Keys.Delete, 0);
                af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)Keys.Back, 0);
            }
            foreach (char k in VariableText)
            {
                af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)k, 0);
            }
            af.APISendMessage(hWndAmbientTabPanelDataGridEdit, WM_CHAR, (int)Keys.Enter, 0);
        }
        private void AmbientFillValues()
        {
            for (int Row = 1; Row < 6; Row++)
            {
                if (MeasurementDepthValues[Row - 1][0] != "")
                {
                    AmbientEnterData(AmbientVariable.MeasurementDepth, MeasurementDepthValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.CurrentSpeeds, CurrentSpeedValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.CurrentDirections, CurrentDirectionValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.AmbientSalinity, AmbientSalinityValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.AmbientTemperature, AmbientTemperatureValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.BackgroundConcentration, BackgroundConcentrationValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.PollutantDecayRate, PollutantDecayRateValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.FarFieldCurrentSpeed, FarFieldCurrentSpeedValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.FarFieldCurrentDirection, FarFieldCurrentDirectionValues[Row - 1][0], Row);
                    AmbientEnterData(AmbientVariable.FarFieldDiffusionCoefficient, FarFieldDiffusionCoefficientValues[Row - 1][0], Row);
                }
                else
                {
                    return;
                }
            }
        }
        private void StartVPExecuteOfScenario()
        {
            af.APISetForegroundWindow(hWndPlumes);
            while (af.APIGetForegroundWindow() != hWndPlumes)
            {
                MegaDoEvents();
            }
            SelectTextOutputTab();
            ClickClearButton();
            af.APISendMouseClick(hWndDiffuserProjectTextBox, 10, 10);
            MegaDoEvents();
            SendKeys.SendWait("^u"); // running VP scenario quick function Ctrl+U
            MegaDoEvents();

            // this will stop the execution after one second
            timerStopExecutionAfterOneSecond.Enabled = true;
            timerStopExecutionAfterOneSecond.Start();

        }
        private void StopVPExecutionOfScenario()
        {
            af.APISetForegroundWindow(hWndPlumes);
            MegaDoEvents();
            while (af.APIGetForegroundWindow() != hWndPlumes)
            {
                MegaDoEvents();
            }
            SendKeys.SendWait("^q^q^q");

            MegaDoEvents();
            MegaDoEvents();

        }
        private void ClickClearButton()
        {
            af.APISendMouseClick(hWndTextOutputClearButton, 10, 10);
        }
        private void LoadResultsInRTB()
        {
            bool IsOK = false;
            while (!IsOK)
            {
                try
                {
                    richTextBoxRawResults.LoadFile(lblSaveResultFileName.Text, RichTextBoxStreamType.PlainText);
                    IsOK = true;
                }
                catch (Exception)
                {
                    MegaDoEvents();
                    // nothing
                }
            }
        }
        private void DoFillText()
        {
            if (radioButtonEn.Checked == true)
            {
                ResultSummaryTxt = "Results summary:\r\n\r\n";
                AmbientNearFieldCurrentSpeedTxt = "\tAmbient near field current speed: ";
                AmbientFarFieldCurrentSpeedTxt = "\tAmbient far field current speed: ";
                MetersPerSecondTxt = " m/s";
                PortDiameterTxt = "\tPort diameter: ";
                MeterTxt = " m";
                PortDepthTxt = "\tPort depth: ";
                DischargeDepthTxt = "\tDepth at discharge: ";
                ChannelWidthTxt = "\tChannel width: ";
                ChannelDepthTxt = "\tChannel depth: ";
                EffluentFlowTxt = "\tEffluent Flow: ";
                EffluentTemperatureTxt = "\tEffluent temperature: ";
                CubicMeterPerSecondTxt = " m3/s";
                CubicMeterPerDayTxt = " m3/d";
                EffluentConcentrationTxt = "\tPollution: ";
                CelsiusTxt = " (C)";
                ColPerDecaLiterTxt = " (col/dl)";
                DecayRateTxt = "\tDecay rate: ";
                PerDayTxt = " (d-1)";
                FlowClassificationTxt = "\tFlow Classification : ";
                AmbientAverageDepthTxt = "\tAmbient average depth: ";
                FroudeNumberTxt = "\tFroude number: ";
                EffluentVelocityTxt = "\tEffluent velocity: ";
                DiffuserTxt = "\tDiffuser: ";
                NumOfPortsTxt = "# of Ports: ";
                PortSpacingTxt = "Port spacing: ";
                //AmbientTemperatureTxt = "\tAmbient temperature: ";

                Line1 = "                                       Far Field     Dispersion      Travel      Corrected";
                Line2 = "        Concentration    Dilution        width        distance        time        distance";
                Line3 = "                                          (m)           (m)           (h)           (m)";
            }
            else
            {
                ResultSummaryTxt = "Sommaire des Résultats:\r\n\r\n";
                AmbientNearFieldCurrentSpeedTxt = "\tVitesse du courant ambiant à exutoire: ";
                AmbientFarFieldCurrentSpeedTxt = "\tVitesse du courant ambiant au loin: ";
                MetersPerSecondTxt = " m/s";
                PortDiameterTxt = "\tDiamètre de l'émissaire: ";
                MeterTxt = " m";
                PortDepthTxt = "\tProfondeur de l'émissaire: ";
                DischargeDepthTxt = "\tProfondeur à l'endroit du rejet: ";
                ChannelWidthTxt = "\tLargeur du chenal: ";
                ChannelDepthTxt = "\tProfondeur du chenal: ";
                EffluentFlowTxt = "\tDébit de l'effluent: ";
                EffluentTemperatureTxt = "\tTempérature de l'effluent: ";
                CubicMeterPerSecondTxt = " m3/s";
                CubicMeterPerDayTxt = " m3/j";
                EffluentConcentrationTxt = "\tPollution: ";
                CelsiusTxt = " (C)";
                ColPerDecaLiterTxt = " (col/dl)";
                DecayRateTxt = "\tTaux de décroissance: ";
                PerDayTxt = " (j-1)";
                FlowClassificationTxt = "\tClassification du rejet: ";
                AmbientAverageDepthTxt = "\tProfondeur moyenne du milieu ambiant: ";
                FroudeNumberTxt = "\tNombre de Froude: ";
                EffluentVelocityTxt = "\tVitesse de l'effluent: ";
                DiffuserTxt = "\tDiffuseur: ";
                NumOfPortsTxt = "# de sortie d'émissaire: ";
                PortSpacingTxt = "Espacement des ports: ";
                //AmbientTemperatureTxt = "\tTempérature de l'eau ambiante: ";

                Line1 = "                                       Largeur de    Distance de     Temps       Distance";
                Line2 = "        Concentration    Dilution      dispersion    dispersion      requis      corrigée";
                Line3 = "                                          (m)           (m)           (h)           (m)";
            }
        }
        private bool ParseVPResults()
        {
            string[] MeasurementDepthValueTxt = { "", "", "", "", "" };
            string[] CurrentSpeedValueTxt = { "", "", "", "", "" };
            string[] CurrentDirectionValueTxt = { "", "", "", "", "" };
            string[] AmbientSalinityValueTxt = { "", "", "", "", "" };
            string[] AmbientTemperatureValueTxt = { "", "", "", "", "" };
            string[] BackgroundConcentrationValueTxt = { "", "", "", "", "" };
            string[] PollutantDecayRateValueTxt = { "", "", "", "", "" };
            string[] FarFieldCurrentSpeedValueTxt = { "", "", "", "", "" };
            string[] FarFieldCurrentDirectionValueTxt = { "", "", "", "", "" };
            string[] FarFieldDiffusionCoefficientValueTxt = { "", "", "", "", "" };
            string PortDiameterValueTxt = "";
            string PortElevationValueTxt = "";
            string VerticalAngleValueTxt = "";
            string HorizontalAngleValueTxt = "";
            string NumberOfPortsValueTxt = "";
            string PortSpacingValueTxt = "";
            string AcuteMixZoneValueTxt = "";
            string ChronicMixZoneValueTxt = "";
            string PortDepthValueTxt = "";
            string EffluentFlowValueTxt = "";
            string EffluentSalinityValueTxt = "";
            string EffluentTemperatureValueTxt = "";
            string EffluentConcentrationValueTxt = "";

            //before doing anything, we need to check if there is more one [ UM3.] text in the richTextBoxRawResults
            int FirstFound = richTextBoxRawResults.Text.IndexOf(" UM3.");
            if (FirstFound < 0)
            {
                return false;
            }
            int LastFound = richTextBoxRawResults.Text.LastIndexOf(" UM3.");

            if (FirstFound != LastFound)
            {
                return false;
            }

            FileStream fs = new FileStream(lblSaveResultFileName.Text, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);

            bool StartReadingValues = false;
            bool IsFirstPart = false;

            listOfResVal = new List<VPAndCormixResValues>();
            double factor = 0;
            Conc10000 = 0;
            FarWidth10000 = 0;
            Dist10000 = 0;
            CorDist10000 = 0;
            Time10000 = 0;
            Conc1000 = 0;
            FarWidth1000 = 0;
            Dist1000 = 0;
            CorDist1000 = 0;
            Time1000 = 0;
            Conc100 = 0;
            FarWidth100 = 0;
            Dist100 = 0;
            CorDist100 = 0;
            Time100 = 0;
            Dilu88 = 0;
            FarWidth88 = 0;
            Dist88 = 0;
            CorDist88 = 0;
            Time88 = 0;
            Dilu14 = 0;
            FarWidth14 = 0;
            Dist14 = 0;
            CorDist14 = 0;
            Time14 = 0;
            Conc300 = 0;
            Dilu300 = 0;
            FarWidth300 = 0;
            Time300 = 0;
            Conc6 = 0;
            Dilu6 = 0;
            FarWidth6 = 0;
            Dist6 = 0;
            CorDist6 = 0;
            Conc12 = 0;
            Dilu12 = 0;
            FarWidth12 = 0;
            Dist12 = 0;
            CorDist12 = 0;
            Conc18 = 0;
            Dilu18 = 0;
            FarWidth18 = 0;
            Dist18 = 0;
            CorDist18 = 0;
            Conc24 = 0;
            Dilu24 = 0;
            FarWidth24 = 0;
            Dist24 = 0;
            CorDist24 = 0;
            Conc30 = 0;
            Dilu30 = 0;
            FarWidth30 = 0;
            Dist30 = 0;
            CorDist30 = 0;

            bool GotDecayAlready = false;

            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();

            richTextBoxParsedResults.Text = "\r\n---------------------------------------------------------------------------------\r\n";
            richTextBoxParsedResults.AppendText(ResultSummaryTxt);


            string TempTxt = sr.ReadLine();
            while (TempTxt != null)
            {
                TempTxt = sr.ReadLine();
                if (TempTxt == null)
                {
                    continue;
                }

                if (TempTxt == "")
                {
                    TempTxt = " ";
                    continue;
                }

                if (TempTxt.Length < 6)
                    continue;

                if (TempTxt.Substring(0, 6) == "Ambien")
                {
                    TempTxt = sr.ReadLine();
                    TempTxt = sr.ReadLine();
                    TempTxt = sr.ReadLine();
                    MeasurementDepthValueTxt[0] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                    CurrentSpeedValueTxt[0] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                    CurrentDirectionValueTxt[0] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                    AmbientSalinityValueTxt[0] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                    AmbientTemperatureValueTxt[0] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                    BackgroundConcentrationValueTxt[0] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                    PollutantDecayRateValueTxt[0] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                    FarFieldCurrentSpeedValueTxt[0] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                    FarFieldCurrentDirectionValueTxt[0] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                    FarFieldDiffusionCoefficientValueTxt[0] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                    TempTxt = sr.ReadLine();
                    if (string.IsNullOrEmpty(TempTxt))
                    {
                        sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[0]).ToString("F3") + MeterTxt);
                        sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                        sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                    }
                    else
                    {
                        MeasurementDepthValueTxt[1] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                        CurrentSpeedValueTxt[1] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                        CurrentDirectionValueTxt[1] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                        AmbientSalinityValueTxt[1] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                        AmbientTemperatureValueTxt[1] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                        BackgroundConcentrationValueTxt[1] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                        PollutantDecayRateValueTxt[1] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                        FarFieldCurrentSpeedValueTxt[1] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                        FarFieldCurrentDirectionValueTxt[1] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                        FarFieldDiffusionCoefficientValueTxt[1] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                        TempTxt = sr.ReadLine();
                        if (string.IsNullOrEmpty(TempTxt))
                        {
                            sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[1]).ToString("F3") + MeterTxt);
                            sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[1]).ToString("F3") + MetersPerSecondTxt);
                            sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                        }
                        else
                        {
                            MeasurementDepthValueTxt[2] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                            CurrentSpeedValueTxt[2] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                            CurrentDirectionValueTxt[2] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                            AmbientSalinityValueTxt[2] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                            AmbientTemperatureValueTxt[2] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                            BackgroundConcentrationValueTxt[2] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                            PollutantDecayRateValueTxt[2] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                            FarFieldCurrentSpeedValueTxt[2] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                            FarFieldCurrentDirectionValueTxt[2] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                            FarFieldDiffusionCoefficientValueTxt[2] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                            TempTxt = sr.ReadLine();
                            if (string.IsNullOrEmpty(TempTxt))
                            {
                                sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[2]).ToString("F3") + MeterTxt);
                                sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[2]).ToString("F3") + MetersPerSecondTxt);
                                sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                            }
                            else
                            {
                                MeasurementDepthValueTxt[3] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                                CurrentSpeedValueTxt[3] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                                CurrentDirectionValueTxt[3] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                                AmbientSalinityValueTxt[3] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                                AmbientTemperatureValueTxt[3] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                                BackgroundConcentrationValueTxt[3] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                                PollutantDecayRateValueTxt[3] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                                FarFieldCurrentSpeedValueTxt[3] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                                FarFieldCurrentDirectionValueTxt[3] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                                FarFieldDiffusionCoefficientValueTxt[3] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                                TempTxt = sr.ReadLine();
                                if (string.IsNullOrEmpty(TempTxt))
                                {
                                    sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[3]).ToString("F3") + MeterTxt);
                                    sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[3]).ToString("F3") + MetersPerSecondTxt);
                                    sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                                }
                                else
                                {
                                    MeasurementDepthValueTxt[4] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                                    CurrentSpeedValueTxt[4] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                                    CurrentDirectionValueTxt[4] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                                    AmbientSalinityValueTxt[4] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                                    AmbientTemperatureValueTxt[4] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                                    BackgroundConcentrationValueTxt[4] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                                    PollutantDecayRateValueTxt[4] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                                    FarFieldCurrentSpeedValueTxt[4] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                                    FarFieldCurrentDirectionValueTxt[4] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                                    FarFieldDiffusionCoefficientValueTxt[4] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                                    TempTxt = sr.ReadLine();
                                    if (string.IsNullOrEmpty(TempTxt))
                                    {
                                        sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[4]).ToString("F3") + MeterTxt);
                                        sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[4]).ToString("F3") + MetersPerSecondTxt);
                                        sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                                    }
                                    else
                                    {
                                        //MeasurementDepthValueTxt[5] = double.Parse(TempTxt.Substring(0, 10)).ToString("F8");
                                        //CurrentSpeedValueTxt[5] = double.Parse(TempTxt.Substring(10, 10)).ToString("F8");
                                        //CurrentDirectionValueTxt[5] = double.Parse(TempTxt.Substring(20, 10)).ToString("F8");
                                        //AmbientSalinityValueTxt[5] = double.Parse(TempTxt.Substring(30, 10)).ToString("F8");
                                        //AmbientTemperatureValueTxt[5] = double.Parse(TempTxt.Substring(40, 10)).ToString("F8");
                                        //BackgroundConcentrationValueTxt[5] = double.Parse(TempTxt.Substring(50, 10)).ToString("F8");
                                        //PollutantDecayRateValueTxt[5] = double.Parse(TempTxt.Substring(60, 10)).ToString("F8");
                                        //FarFieldCurrentSpeedValueTxt[5] = double.Parse(TempTxt.Substring(70, 10)).ToString("F8");
                                        //FarFieldCurrentDirectionValueTxt[5] = double.Parse(TempTxt.Substring(80, 10)).ToString("F8");
                                        //FarFieldDiffusionCoefficientValueTxt[5] = double.Parse(TempTxt.Substring(90, 10)).ToString("F8");
                                        //sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(MeasurementDepthValueTxt[5]).ToString("F3") + MeterTxt);
                                        //sb1.AppendLine(AmbientNearFieldCurrentSpeedTxt + double.Parse(CurrentSpeedValueTxt[5]).ToString("F3") + MetersPerSecondTxt);
                                        //sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(FarFieldCurrentSpeedValueTxt[0]).ToString("F3") + MetersPerSecondTxt);
                                    }
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(TempTxt))
                    continue;

                if (TempTxt.Substring(0, 8) == "Diffuser")
                {
                    TempTxt = sr.ReadLine();
                    if (TempTxt.Substring(0, 72) == "   P-dia  P-elev V-angle H-angle   Ports AcuteMZ ChrncMZ P-depth Ttl-flo")
                    {
                        TempTxt = sr.ReadLine();
                        TempTxt = sr.ReadLine();
                        PortDiameterValueTxt = double.Parse(TempTxt.Substring(0, 8)).ToString("F8");
                        PortElevationValueTxt = double.Parse(TempTxt.Substring(8, 8)).ToString("F8");
                        VerticalAngleValueTxt = double.Parse(TempTxt.Substring(16, 8)).ToString("F8");
                        HorizontalAngleValueTxt = double.Parse(TempTxt.Substring(24, 8)).ToString("F8");
                        NumberOfPortsValueTxt = double.Parse(TempTxt.Substring(32, 8)).ToString("F8");
                        AcuteMixZoneValueTxt = double.Parse(TempTxt.Substring(40, 8)).ToString("F8");
                        ChronicMixZoneValueTxt = double.Parse(TempTxt.Substring(48, 8)).ToString("F8");
                        PortDepthValueTxt = double.Parse(TempTxt.Substring(56, 8)).ToString("F8");
                        EffluentFlowValueTxt = double.Parse(TempTxt.Substring(64, 8)).ToString("F8");
                        EffluentSalinityValueTxt = double.Parse(TempTxt.Substring(72, 8)).ToString("F8");
                        EffluentTemperatureValueTxt = double.Parse(TempTxt.Substring(80, 8)).ToString("F8");
                        EffluentConcentrationValueTxt = double.Parse(TempTxt.Substring(88, 8)).ToString("F8");
                        sb2.AppendLine(PortDiameterTxt + double.Parse(PortDiameterValueTxt).ToString("F3") + MeterTxt);
                        sb2.AppendLine(PortDepthTxt + double.Parse(PortDepthValueTxt).ToString("F3") + MeterTxt);
                        sb2.AppendLine(EffluentFlowTxt + double.Parse(EffluentFlowValueTxt).ToString("F4") + CubicMeterPerSecondTxt + "  (" + (double.Parse(TempTxt.Substring(64, 8)) * 3600 * 24).ToString("F0") + CubicMeterPerDayTxt + ")");
                        sb2.AppendLine(EffluentTemperatureTxt + double.Parse(EffluentTemperatureValueTxt).ToString("F1") + CelsiusTxt);
                        sb2.AppendLine(EffluentConcentrationTxt + double.Parse(EffluentConcentrationValueTxt).ToString("F0") + ColPerDecaLiterTxt);
                    }
                    else if (TempTxt.Substring(0, 72) == "   P-dia  P-elev V-angle H-angle   Ports Spacing AcuteMZ ChrncMZ P-depth")
                    {
                        TempTxt = sr.ReadLine();
                        TempTxt = sr.ReadLine();
                        PortDiameterValueTxt = double.Parse(TempTxt.Substring(0, 8)).ToString("F8");
                        PortElevationValueTxt = double.Parse(TempTxt.Substring(8, 8)).ToString("F8");
                        VerticalAngleValueTxt = double.Parse(TempTxt.Substring(16, 8)).ToString("F8");
                        HorizontalAngleValueTxt = double.Parse(TempTxt.Substring(24, 8)).ToString("F8");
                        NumberOfPortsValueTxt = double.Parse(TempTxt.Substring(32, 8)).ToString("F8");
                        PortSpacingValueTxt = double.Parse(TempTxt.Substring(40, 8)).ToString("F8");
                        AcuteMixZoneValueTxt = double.Parse(TempTxt.Substring(48, 8)).ToString("F8");
                        ChronicMixZoneValueTxt = double.Parse(TempTxt.Substring(56, 8)).ToString("F8");
                        PortDepthValueTxt = double.Parse(TempTxt.Substring(64, 8)).ToString("F8");
                        EffluentFlowValueTxt = double.Parse(TempTxt.Substring(72, 8)).ToString("F8");
                        EffluentSalinityValueTxt = double.Parse(TempTxt.Substring(80, 8)).ToString("F8");
                        EffluentTemperatureValueTxt = double.Parse(TempTxt.Substring(88, 8)).ToString("F8");
                        EffluentConcentrationValueTxt = double.Parse(TempTxt.Substring(96, 8)).ToString("F8");
                        sb2.AppendLine(PortDiameterTxt + double.Parse(PortDiameterValueTxt).ToString("F3") + MeterTxt);
                        sb2.AppendLine(DiffuserTxt + NumOfPortsTxt + double.Parse(NumberOfPortsValueTxt).ToString("F0") + " --- " + PortSpacingTxt + double.Parse(PortSpacingValueTxt).ToString("F3") + MeterTxt);
                        sb2.AppendLine(PortDepthTxt + double.Parse(PortDepthValueTxt).ToString("F3") + MeterTxt);
                        sb2.AppendLine(EffluentFlowTxt + double.Parse(EffluentFlowValueTxt).ToString("F4") + CubicMeterPerSecondTxt + "  (" + (double.Parse(TempTxt.Substring(72, 8)) * 3600 * 24).ToString("F0") + CubicMeterPerDayTxt + ")");
                        sb2.AppendLine(EffluentTemperatureTxt + double.Parse(EffluentTemperatureValueTxt).ToString("F1") + CelsiusTxt);
                        sb2.AppendLine(EffluentConcentrationTxt + double.Parse(EffluentConcentrationValueTxt).ToString("F0") + ColPerDecaLiterTxt);
                    }
                }

                if (TempTxt.Substring(0, 6) == "Simula")
                {
                    TempTxt = sr.ReadLine();
                    FroudeNumberValues[0] = double.Parse(TempTxt.Substring(19, 6)).ToString("F3");
                    EffluentVelocityValues[0] = double.Parse(TempTxt.Substring(84, 8)).ToString("F4");
                    sb2.AppendLine(FroudeNumberTxt + double.Parse(TempTxt.Substring(19, 6)).ToString("F3"));
                    sb2.AppendLine(EffluentVelocityTxt + double.Parse(TempTxt.Substring(84, 8)).ToString("F4") + MetersPerSecondTxt);
                }


                if (TempTxt.Substring(0, 5) == "count")
                {

                    // look for dilution 1000
                    VPAndCormixResValues OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Distance > 300)
                        {
                            factor = (300 - OldVal.Distance) / (TempVal.Distance - OldVal.Distance);
                            Conc300 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu300 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth300 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Time300 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.TheTime > 6)
                        {
                            factor = (6 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                            Conc6 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu6 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth6 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist6 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            CorDist6 = Dist6;
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.TheTime > 12)
                        {
                            factor = (12 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                            Conc12 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu12 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth12 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist12 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            CorDist12 = GetCorrectedDistance(Dist12, 12, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.TheTime > 18)
                        {
                            factor = (18 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                            Conc18 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu18 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth18 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist18 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            CorDist18 = GetCorrectedDistance(Dist18, 18, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.TheTime > 24)
                        {
                            factor = (24 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                            Conc24 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu24 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth24 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist24 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            CorDist24 = GetCorrectedDistance(Dist24, 24, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.TheTime > 30)
                        {
                            factor = (30 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                            Conc30 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            Dilu30 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                            FarWidth30 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist30 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            CorDist30 = GetCorrectedDistance(Dist30, 30, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Dilu > 10000)
                        {
                            factor = (10000 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                            Conc10000 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            FarWidth10000 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist10000 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            Time10000 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                            CorDist10000 = GetCorrectedDistance(Dist10000, Time10000, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Dilu > 1000)
                        {
                            factor = (1000 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                            Conc1000 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            FarWidth1000 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist1000 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            Time1000 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                            CorDist1000 = GetCorrectedDistance(Dist1000, Time1000, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    // look for dilution 100
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Dilu > 100)
                        {
                            factor = (100 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                            Conc100 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                            FarWidth100 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                            Dist100 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                            Time100 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                            CorDist100 = GetCorrectedDistance(Dist100, Time100, Dist6, Dist12, Dist18, Dist24, Dist30);
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Conc < 88)
                        {
                            try
                            {
                                factor = (88 - OldVal.Conc) / (TempVal.Conc - OldVal.Conc);
                                Dilu88 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth88 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist88 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                Time88 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                CorDist88 = GetCorrectedDistance(Dist88, Time88, Dist6, Dist12, Dist18, Dist24, Dist30);
                            }
                            catch (Exception)
                            {
                                // nothing for now.
                            }
                            break;
                        }
                        OldVal = TempVal;
                    }
                    OldVal = null;
                    foreach (VPAndCormixResValues TempVal in listOfResVal)
                    {
                        if (TempVal.Conc < 14)
                        {
                            try
                            {
                                factor = (14 - OldVal.Conc) / (TempVal.Conc - OldVal.Conc);
                                Dilu14 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth14 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist14 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                Time14 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                CorDist14 = GetCorrectedDistance(Dist14, Time14, Dist6, Dist12, Dist18, Dist24, Dist30);

                            }
                            catch (Exception)
                            {
                                // nothing for now.
                            }
                            break;
                        }
                        OldVal = TempVal;
                    }
                    StartReadingValues = false;
                    TempTxt = "";

                    sb3.AppendLine();
                    sb3.AppendLine(Line1);
                    sb3.AppendLine(Line2);
                    sb3.AppendLine(Line3);

                    string Spaces = "                                                            ";
                    SortedDictionary<double, string> sortedDic = new SortedDictionary<double, string>();
                    sortedDic.Add(Dist10000, "Dist10000");
                    sortedDic.Add(Dist1000, "Dist1000");
                    sortedDic.Add(300, "Dist300");
                    sortedDic.Add(Dist88, "Dist88");
                    sortedDic.Add(Dist14, "Dist14");

                    foreach (KeyValuePair<double, string> sd in sortedDic)
                    {
                        if (sd.Value == "Dist10000")
                        {
                            sb3.AppendLine(
                                Spaces.Substring(0, 15 - Conc10000.ToString("F0").Trim().Length) + Conc10000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - "10000".Length) + "10000"
                                + Spaces.Substring(0, 15 - FarWidth10000.ToString("F0").Trim().Length) + FarWidth10000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Dist10000.ToString("F0").Trim().Length) + Dist10000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Time10000.ToString("F2").Trim().Length) + Time10000.ToString("F2").Trim()
                                + Spaces.Substring(0, 15 - CorDist10000.ToString("F0").Trim().Length) + CorDist10000.ToString("F0").Trim()
                                );
                        }
                        else if (sd.Value == "Dist1000")
                        {
                            sb3.AppendLine(
                                Spaces.Substring(0, 15 - Conc1000.ToString("F0").Trim().Length) + Conc1000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - "1000".Length) + "1000"
                                + Spaces.Substring(0, 15 - FarWidth1000.ToString("F0").Trim().Length) + FarWidth1000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Dist1000.ToString("F0").Trim().Length) + Dist1000.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Time1000.ToString("F2").Trim().Length) + Time1000.ToString("F2").Trim()
                                + Spaces.Substring(0, 15 - CorDist1000.ToString("F0").Trim().Length) + CorDist1000.ToString("F0").Trim()
                                );
                        }
                        else if (sd.Value == "Dist300")
                        {
                            sb3.AppendLine(
                                Spaces.Substring(0, 15 - Conc300.ToString("F0").Trim().Length) + Conc300.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Dilu300.ToString("F0").Length) + Dilu300.ToString("F0")
                                + Spaces.Substring(0, 15 - FarWidth300.ToString("F0").Trim().Length) + FarWidth300.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - "300".Trim().Length) + "300".Trim()
                                + Spaces.Substring(0, 15 - Time300.ToString("F2").Trim().Length) + Time300.ToString("F2").Trim()
                                + Spaces.Substring(0, 15 - "300".Trim().Length) + "300".Trim()
                                );

                        }
                        else if (sd.Value == "Dist88")
                        {
                            sb3.AppendLine(
                                Spaces.Substring(0, 15 - "88".Trim().Length) + "88".Trim()
                                + Spaces.Substring(0, 15 - Dilu88.ToString("F0").Length) + Dilu88.ToString("F0")
                                + Spaces.Substring(0, 15 - FarWidth88.ToString("F0").Trim().Length) + FarWidth88.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Dist88.ToString("F0").Trim().Length) + Dist88.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Time88.ToString("F2").Trim().Length) + Time88.ToString("F2").Trim()
                                + Spaces.Substring(0, 15 - CorDist88.ToString("F0").Trim().Length) + CorDist88.ToString("F0").Trim()
                                );
                        }
                        else if (sd.Value == "Dist14")
                        {
                            sb3.AppendLine(
                                Spaces.Substring(0, 15 - "14".Trim().Length) + "14".Trim()
                                + Spaces.Substring(0, 15 - Dilu14.ToString("F0").Length) + Dilu14.ToString("F0")
                                + Spaces.Substring(0, 15 - FarWidth14.ToString("F0").Trim().Length) + FarWidth14.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Dist14.ToString("F0").Trim().Length) + Dist14.ToString("F0").Trim()
                                + Spaces.Substring(0, 15 - Time14.ToString("F2").Trim().Length) + Time14.ToString("F2").Trim()
                                + Spaces.Substring(0, 15 - CorDist14.ToString("F0").Trim().Length) + CorDist14.ToString("F0").Trim()
                                );
                        }
                        else
                        {

                        }

                    }
                    if (Dist1000 == 0 || Dist88 == 0 || Dist14 == 0)
                    {
                        sr.Close();
                        fs.Close();
                        StopVPExecutionOfScenario();
                        if (double.Parse(ChronicMixZoneValues[0]) > 30000)
                            return true;
                        ChronicMixZoneValues[0] = (double.Parse(ChronicMixZoneValues[0]) + 1000).ToString();
                        DiffuserEnterData(DiffuserVariable.ChronicMixZone, ChronicMixZoneValues[0]);
                        return false;
                    }

                }
                if (StartReadingValues)
                {
                    VPAndCormixResValues TempResValues = new VPAndCormixResValues();
                    if (IsFirstPart)
                    {

                        if (TempTxt.Length > 14)
                        {
                            if (TempTxt.Substring(0, 13) == "4/3 Power Law" || TempTxt.Substring(0, 13) == "Plumes not me")
                            {
                                StartReadingValues = false;
                                IsFirstPart = false;
                                continue;
                            } 
                        }

                        if (TempTxt.Length < 86)
                        {
                            MessageBox.Show("Please add the time under the 'Special Setting' under selected variables.\r\nIt needs to be the last variable.\r\n");
                            VPSettings vps = new VPSettings();
                            vps.ShowDialog();
                            return false;
                        }

                        TempResValues.FarfieldWidth = double.Parse(TempTxt.Substring(24, 8));
                        TempResValues.Conc = double.Parse(TempTxt.Substring(33, 8));
                        TempResValues.Dilu = double.Parse(TempTxt.Substring(51, 8));
                        TempResValues.Distance = double.Parse(TempTxt.Substring(70, 8));
                        TempResValues.TheTime = double.Parse(TempTxt.Substring(78, 8));
                    }
                    else
                    {
                        TempResValues.Conc = double.Parse(TempTxt.Substring(0, 9));
                        TempResValues.Dilu = double.Parse(TempTxt.Substring(8, 9));
                        TempResValues.FarfieldWidth = double.Parse(TempTxt.Substring(17, 8));
                        TempResValues.Distance = double.Parse(TempTxt.Substring(24, 8));
                        TempResValues.TheTime = double.Parse(TempTxt.Substring(33, 8));

                        if (!GotDecayAlready)
                        {
                            sb2.AppendLine(DecayRateTxt + double.Parse(TempTxt.Substring(49, 8)).ToString("F3") + PerDayTxt);
                            GotDecayAlready = true;
                        }
                    }

                    listOfResVal.Add(TempResValues);
                }
                if (string.IsNullOrEmpty(TempTxt))
                    continue;
                if (TempTxt.Length > 4)
                {
                    if (TempTxt.Substring(0, 4) == "(col")
                    {
                        StartReadingValues = true;
                        IsFirstPart = false;
                    }
                }
                if (TempTxt.Length > 40)
                {
                    if (TempTxt.Substring(0, 40) == "Step      (m)    (m/s)      (m) (col/dl)")
                    {
                        StartReadingValues = true;

                        IsFirstPart = true;
                    }
                }

            }
            sr.Close();
            fs.Close();

            richTextBoxParsedResults.AppendText(sb1.ToString());
            richTextBoxParsedResults.AppendText(sb2.ToString());
            richTextBoxParsedResults.AppendText(sb3.ToString());
            richTextBoxParsedResults.AppendText("\r\n\r\n");
            richTextBoxParsedResults.AppendText("---------------------------------------------------------------------------------\r\n");
            richTextBoxParsedResults.AppendText("-----------------------    Visual Plume Result    -------------------------------\r\n\r\n");
            richTextBoxParsedResults.AppendText(richTextBoxRawResults.Text);

            // checking if all the parsed information and the current values are the same.
            // checking Ambient info
            for (int Row = 1; Row < 6; Row++)
            {
                if (!CompareAmbientValues(MeasurementDepthValueTxt, MeasurementDepthCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(CurrentSpeedValueTxt, CurrentSpeedCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(CurrentDirectionValueTxt, CurrentDirectionCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(AmbientSalinityValueTxt, AmbientSalinityCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(AmbientTemperatureValueTxt, AmbientTemperatureCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(BackgroundConcentrationValueTxt, BackgroundConcentrationCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(PollutantDecayRateValueTxt, PollutantDecayRateValueTxt, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(FarFieldCurrentSpeedValueTxt, FarFieldCurrentSpeedCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(FarFieldCurrentDirectionValueTxt, FarFieldCurrentDirectionCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
                if (!CompareAmbientValues(FarFieldDiffusionCoefficientValueTxt, FarFieldDiffusionCoefficientCurrent, Row))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
            }

            // checking Diffuser info
            if (!CompareDiffuserValues(PortDiameterValueTxt, PortDiameterCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(PortElevationValueTxt, PortElevationCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(VerticalAngleValueTxt, VerticalAngleCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(HorizontalAngleValueTxt, HorizontalAngleCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(NumberOfPortsValueTxt, NumberOfPortsCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (PortSpacingValueTxt != "")
            {
                if (!CompareDiffuserValues(PortSpacingValueTxt, PortSpacingCurrent))
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
            }
            if (!CompareDiffuserValues(AcuteMixZoneValueTxt, AcuteMixZoneCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(ChronicMixZoneValueTxt, ChronicMixZoneCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(PortDepthValueTxt, PortDepthCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(EffluentFlowValueTxt, EffluentFlowCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(EffluentSalinityValueTxt, EffluentSalinityCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(EffluentTemperatureValueTxt, EffluentTemperatureCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }
            if (!CompareDiffuserValues(EffluentConcentrationValueTxt, EffluentConcentrationCurrent))
            {
                richTextBoxParsedResults.Text = "";
                return false;
            }

            return true;
        }
        private void ParseAndSaveCormixResults()
        {
            richTextBoxCormixDetailResults.SaveFile(lblSaveResultFileName.Text.Trim(), RichTextBoxStreamType.PlainText);
            FileStream fs = new FileStream(lblSaveResultFileName.Text, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);

            bool StartReadingValues = false;
            bool IsShort = false;
            listOfCormixResVal = new List<VPAndCormixResValues>();
            double factor = 0;
            Conc10000 = 0;
            FarWidth10000 = 0;
            Dist10000 = 0;
            CorDist10000 = 0;
            Time10000 = 0;
            Conc1000 = 0;
            FarWidth1000 = 0;
            Dist1000 = 0;
            CorDist1000 = 0;
            Time1000 = 0;
            Conc100 = 0;
            FarWidth100 = 0;
            Dist100 = 0;
            CorDist100 = 0;
            Time100 = 0;
            Dilu88 = 0;
            FarWidth88 = 0;
            Dist88 = 0;
            CorDist88 = 0;
            Time88 = 0;
            Dilu14 = 0;
            FarWidth14 = 0;
            Dist14 = 0;
            CorDist14 = 0;
            Time14 = 0;
            Conc300 = 0;
            Dilu300 = 0;
            FarWidth300 = 0;
            Time300 = 0;
            Conc6 = 0;
            Dilu6 = 0;
            FarWidth6 = 0;
            Dist6 = 0;
            CorDist6 = 0;
            Conc12 = 0;
            Dilu12 = 0;
            FarWidth12 = 0;
            Dist12 = 0;
            CorDist12 = 0;
            Conc18 = 0;
            Dilu18 = 0;
            FarWidth18 = 0;
            Dist18 = 0;
            CorDist18 = 0;
            Conc24 = 0;
            Dilu24 = 0;
            FarWidth24 = 0;
            Dist24 = 0;
            CorDist24 = 0;
            Conc30 = 0;
            Dilu30 = 0;
            FarWidth30 = 0;
            Dist30 = 0;
            CorDist30 = 0;
            double LastDistance = 0;
            int CormixType = 0;

            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();

            richTextBoxCormixParsedResults.Text = "\r\n---------------------------------------------------------------------------------\r\n";
            richTextBoxCormixParsedResults.AppendText(ResultSummaryTxt);


            string TempTxt = sr.ReadLine();
            if (TempTxt.Substring(0, 7) == "CORMIX1")
            {
                CormixType = 1;
            }
            else if (TempTxt.Substring(0, 7) == "CORMIX2")
            {
                CormixType = 2;
            }
            else if (TempTxt.Substring(0, 7) == "CORMIX3")
            {
                CormixType = 3;
            }
            else
            {
                MessageBox.Show("Could not detect Cormix type");
                return;
            }
            while (TempTxt != null)
            {
                TempTxt = sr.ReadLine();
                if (TempTxt == null)
                {
                    continue;
                }

                if (TempTxt == "")
                {
                    TempTxt = " ";
                    continue;
                }

                if (TempTxt.Length < 6)
                    continue;

                if (TempTxt.Length > 36)
                {
                    if (TempTxt.Substring(0, 37) == "ENVIRONMENT PARAMETERS (metric units)")
                    {
                        TempTxt = sr.ReadLine();
                        if (TempTxt.Substring(0, 10).Trim() == "Unbounded" && radioButtonFR.Checked == true)
                        {
                            sb1.AppendLine("\tMilieu infini");
                        }
                        else
                        {
                            sb1.AppendLine("\t" + TempTxt.Substring(0, 10).Trim());
                        }
                        TempTxt = sr.ReadLine();
                        sb1.AppendLine(AmbientAverageDepthTxt + double.Parse(TempTxt.Substring(12, 7)).ToString("F2") + MeterTxt);
                        sb1.AppendLine(DischargeDepthTxt + double.Parse(TempTxt.Substring(30, 7)).ToString("F2") + MeterTxt);
                        TempTxt = sr.ReadLine();
                        sb1.AppendLine(AmbientFarFieldCurrentSpeedTxt + double.Parse(TempTxt.Substring(12, 7)).ToString("F2") + MetersPerSecondTxt);
                    }
                }

                if (TempTxt.Length > 34)
                {
                    if (TempTxt.Substring(0, 35) == "DISCHARGE PARAMETERS (metric units)")
                    {
                        if (CormixType == 1)
                        {
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(PortDiameterTxt + double.Parse(TempTxt.Substring(12, 7)).ToString("F2") + MeterTxt);
                            sb2.AppendLine(PortDepthTxt + double.Parse(TempTxt.Substring(70, 5)).ToString("F2") + MeterTxt);
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(EffluentFlowTxt + double.Parse(TempTxt.Substring(46, 10)).ToString("F6") + CubicMeterPerSecondTxt + "  (" + (double.Parse(TempTxt.Substring(46, 10)) * 3600 * 24).ToString("F0") + CubicMeterPerDayTxt + ")");
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(EffluentConcentrationTxt + double.Parse(TempTxt.Substring(8, 10)).ToString("F0") + ColPerDecaLiterTxt);
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(DecayRateTxt + (double.Parse(TempTxt.Substring(46, 10)) * 24 * 3600).ToString("F3") + PerDayTxt);
                        }
                        else if (CormixType == 2)
                        {
                            MessageBox.Show("Not implememted yet");
                            return;
                        }
                        else if (CormixType == 3)
                        {
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(ChannelWidthTxt + double.Parse(TempTxt.Substring(12, 7)).ToString("F2") + MeterTxt);
                            sb2.AppendLine(ChannelDepthTxt + double.Parse(TempTxt.Substring(31, 7)).ToString("F2") + MeterTxt);
                            TempTxt = sr.ReadLine();
                            if (TempTxt.Length > 16)
                            {
                                if (TempTxt.Substring(0, 16) == " Reduced channel")
                                {
                                    MessageBox.Show("Please try to remove the line\r\n\r\n[" + TempTxt + "]\r\n\r\nBy reducing the channel area (width*height)\r\n\r\nParsing will stop");
                                    return;
                                }
                            }

                            sb2.AppendLine(EffluentFlowTxt + double.Parse(TempTxt.Substring(46, 10)).ToString("F6") + CubicMeterPerSecondTxt + "  (" + (double.Parse(TempTxt.Substring(46, 10)) * 3600 * 24).ToString("F0") + CubicMeterPerDayTxt + ")");
                            TempTxt = sr.ReadLine();
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(EffluentConcentrationTxt + double.Parse(TempTxt.Substring(8, 10)).ToString("F0") + ColPerDecaLiterTxt);
                            TempTxt = sr.ReadLine();
                            sb2.AppendLine(DecayRateTxt + (double.Parse(TempTxt.Substring(46, 10)) * 24 * 3600).ToString("F3") + PerDayTxt);
                        }
                    }
                }

                if (TempTxt.Length > 18)
                {
                    if (TempTxt.Substring(0, 19) == "FLOW CLASSIFICATION")
                    {
                        TempTxt = sr.ReadLine();
                        TempTxt = sr.ReadLine();
                        sb2.AppendLine(FlowClassificationTxt + TempTxt.Substring(4, 23));
                    }
                }

                if (TempTxt.Length > 43)
                {
                    if (TempTxt.Substring(0, 44) == "   This is the REGION OF INTEREST limitation")
                    {
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            TempVal.TheTime = TempVal.TheTime / 3600;
                        }

                        VPAndCormixResValues OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Distance > 300)
                            {
                                factor = (300 - OldVal.Distance) / (TempVal.Distance - OldVal.Distance);
                                Conc300 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu300 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth300 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Time300 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.TheTime > 6)
                            {
                                factor = (6 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                                Conc6 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu6 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth6 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist6 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                CorDist6 = Dist6;
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.TheTime > 12)
                            {
                                factor = (12 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                                Conc12 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu12 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth12 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist12 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                CorDist12 = GetCorrectedDistance(Dist12, 12, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.TheTime > 18)
                            {
                                factor = (18 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                                Conc18 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu18 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth18 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist18 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                CorDist18 = GetCorrectedDistance(Dist18, 18, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.TheTime > 24)
                            {
                                factor = (24 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                                Conc24 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu24 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth24 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist24 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                CorDist24 = GetCorrectedDistance(Dist24, 24, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.TheTime > 30)
                            {
                                factor = (30 - OldVal.TheTime) / (TempVal.TheTime - OldVal.TheTime);
                                Conc30 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                Dilu30 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                FarWidth30 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist30 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                CorDist30 = GetCorrectedDistance(Dist30, 30, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Dilu > 10000)
                            {
                                factor = (10000 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                                Conc10000 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                FarWidth10000 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist10000 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                Time10000 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                CorDist10000 = GetCorrectedDistance(Dist10000, Time10000, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Dilu > 1000)
                            {
                                factor = (1000 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                                Conc1000 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                FarWidth1000 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist1000 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                Time1000 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                CorDist1000 = GetCorrectedDistance(Dist1000, Time1000, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        // look for dilution 100
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Dilu > 100)
                            {
                                factor = (100 - OldVal.Dilu) / (TempVal.Dilu - OldVal.Dilu);
                                Conc100 = (TempVal.Conc - OldVal.Conc) * factor + OldVal.Conc;
                                FarWidth100 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                Dist100 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                Time100 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                CorDist100 = GetCorrectedDistance(Dist100, Time100, Dist6, Dist12, Dist18, Dist24, Dist30);
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Conc < 88)
                            {
                                try
                                {
                                    factor = (88 - OldVal.Conc) / (TempVal.Conc - OldVal.Conc);
                                    Dilu88 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                    FarWidth88 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                    Dist88 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                    Time88 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                    CorDist88 = GetCorrectedDistance(Dist88, Time88, Dist6, Dist12, Dist18, Dist24, Dist30);
                                }
                                catch (Exception)
                                {
                                    // nothing for now.
                                }
                                break;
                            }
                            OldVal = TempVal;
                        }
                        OldVal = null;
                        foreach (VPAndCormixResValues TempVal in listOfCormixResVal)
                        {
                            if (TempVal.Conc < 14)
                            {
                                try
                                {
                                    factor = (14 - OldVal.Conc) / (TempVal.Conc - OldVal.Conc);
                                    Dilu14 = (TempVal.Dilu - OldVal.Dilu) * factor + OldVal.Dilu;
                                    FarWidth14 = (TempVal.FarfieldWidth - OldVal.FarfieldWidth) * factor + OldVal.FarfieldWidth;
                                    Dist14 = (TempVal.Distance - OldVal.Distance) * factor + OldVal.Distance;
                                    Time14 = (TempVal.TheTime - OldVal.TheTime) * factor + OldVal.TheTime;
                                    CorDist14 = GetCorrectedDistance(Dist14, Time14, Dist6, Dist12, Dist18, Dist24, Dist30);

                                }
                                catch (Exception)
                                {
                                    // nothing for now.
                                }
                                break;
                            }
                            OldVal = TempVal;
                        }
                        StartReadingValues = false;
                        TempTxt = "";

                        sb3.AppendLine();
                        sb3.AppendLine(Line1);
                        sb3.AppendLine(Line2);
                        sb3.AppendLine(Line3);

                        string DiluTxt14 = "";
                        if ((int)Dilu14 == -999)
                        {
                            DiluTxt14 = ">10K";
                        }
                        else
                        {
                            DiluTxt14 = Dilu14.ToString("F0").Trim();
                        }
                        string DiluTxt88 = "";
                        if ((int)Dilu88 == -999)
                        {
                            DiluTxt88 = ">10K";
                        }
                        else
                        {
                            DiluTxt88 = Dilu88.ToString("F0").Trim();
                        }
                        string Spaces = "                                                            ";
                        SortedDictionary<double, string> sortedDic = new SortedDictionary<double, string>();
                        sortedDic.Add(Dist10000, "Dist10000");
                        sortedDic.Add(Dist1000, "Dist1000");
                        sortedDic.Add(300, "Dist300");
                        sortedDic.Add(Dist88, "Dist88");
                        sortedDic.Add(Dist14, "Dist14");

                        foreach (KeyValuePair<double, string> sd in sortedDic)
                        {
                            if (sd.Value == "Dist10000")
                            {
                                sb3.AppendLine(
                                    Spaces.Substring(0, 15 - Conc10000.ToString("F0").Trim().Length) + Conc10000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - "10000".Length) + "10000"
                                    + Spaces.Substring(0, 15 - FarWidth10000.ToString("F0").Trim().Length) + FarWidth10000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Dist10000.ToString("F0").Trim().Length) + Dist10000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Time10000.ToString("F2").Trim().Length) + Time10000.ToString("F2").Trim()
                                    + Spaces.Substring(0, 15 - CorDist10000.ToString("F0").Trim().Length) + CorDist10000.ToString("F0").Trim()
                                    );
                            }
                            else if (sd.Value == "Dist1000")
                            {
                                sb3.AppendLine(
                                    Spaces.Substring(0, 15 - Conc1000.ToString("F0").Trim().Length) + Conc1000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - "1000".Length) + "1000"
                                    + Spaces.Substring(0, 15 - FarWidth1000.ToString("F0").Trim().Length) + FarWidth1000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Dist1000.ToString("F0").Trim().Length) + Dist1000.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Time1000.ToString("F2").Trim().Length) + Time1000.ToString("F2").Trim()
                                    + Spaces.Substring(0, 15 - CorDist1000.ToString("F0").Trim().Length) + CorDist1000.ToString("F0").Trim()
                                    );
                            }
                            else if (sd.Value == "Dist300")
                            {
                                sb3.AppendLine(
                                    Spaces.Substring(0, 15 - Conc300.ToString("F0").Trim().Length) + Conc300.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Dilu300.ToString("F0").Length) + Dilu300.ToString("F0")
                                    + Spaces.Substring(0, 15 - FarWidth300.ToString("F0").Trim().Length) + FarWidth300.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - "300".Trim().Length) + "300".Trim()
                                    + Spaces.Substring(0, 15 - Time300.ToString("F2").Trim().Length) + Time300.ToString("F2").Trim()
                                    + Spaces.Substring(0, 15 - "300".Trim().Length) + "300".Trim()
                                    );

                            }
                            else if (sd.Value == "Dist88")
                            {
                                sb3.AppendLine(
                                    Spaces.Substring(0, 15 - "88".Trim().Length) + "88".Trim()
                                    + Spaces.Substring(0, 15 - DiluTxt88.Length) + DiluTxt88
                                    + Spaces.Substring(0, 15 - FarWidth88.ToString("F0").Trim().Length) + FarWidth88.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Dist88.ToString("F0").Trim().Length) + Dist88.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Time88.ToString("F2").Trim().Length) + Time88.ToString("F2").Trim()
                                    + Spaces.Substring(0, 15 - CorDist88.ToString("F0").Trim().Length) + CorDist88.ToString("F0").Trim()
                                    );
                            }
                            else if (sd.Value == "Dist14")
                            {
                                sb3.AppendLine(
                                    Spaces.Substring(0, 15 - "14".Trim().Length) + "14".Trim()
                                    + Spaces.Substring(0, 15 - DiluTxt14.Length) + DiluTxt14
                                    + Spaces.Substring(0, 15 - FarWidth14.ToString("F0").Trim().Length) + FarWidth14.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Dist14.ToString("F0").Trim().Length) + Dist14.ToString("F0").Trim()
                                    + Spaces.Substring(0, 15 - Time14.ToString("F2").Trim().Length) + Time14.ToString("F2").Trim()
                                    + Spaces.Substring(0, 15 - CorDist14.ToString("F0").Trim().Length) + CorDist14.ToString("F0").Trim()
                                    );
                            }
                            else
                            {

                            }
                        }
                    }
                }

                if (TempTxt.Length > 84)
                {
                    if (TempTxt.Substring(0, 85) == "       X        Y       Z        S       C       BV       BH      ZU      ZL       TT")
                    {
                        StartReadingValues = true;
                        IsShort = false;
                        continue;
                    }
                }

                if (TempTxt.Length > 69)
                {
                    if (CormixType == 1)
                    {
                        if (TempTxt.Substring(0, 70) == "       X        Y       Z        S       C       B        Uc        TT")
                        {
                            StartReadingValues = true;
                            IsShort = true;
                            continue;
                        }
                    }
                    else if (CormixType == 2)
                    {
                        MessageBox.Show("not implemented yet");
                    }
                    else if (CormixType == 3)
                    {
                        if (TempTxt.Substring(0, 70) == "       X        Y       Z        S       C       BV       BH        TT")
                        {
                            StartReadingValues = true;
                            IsShort = true;
                            continue;
                        }
                    }
                }

                if (StartReadingValues)
                {
                    if (!IsShort)
                    {
                        if (CormixType == 1)
                        {
                            if (TempTxt.Length > 85)
                            {
                                if (TempTxt.Substring(85, 1) != "+")
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                //StartReadingValues = false;
                                continue;
                            }
                        }
                        else if (CormixType == 2)
                        {
                            MessageBox.Show("not implemented");
                            return;
                        }
                        else if (CormixType == 3)
                        {
                            if (TempTxt.Length > 85)
                            {
                                if (TempTxt.Substring(85, 1) != "+")
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                //StartReadingValues = false;
                                continue;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Error CormixType not equal to 1 or 2 or 3");
                            return;
                        }
                    }
                    else
                    {
                        if (CormixType == 1)
                        {
                            if (TempTxt.Length > 71)
                            {
                                if (TempTxt.Substring(71, 1) != "+")
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (CormixType == 2)
                        {
                            MessageBox.Show("not implemented yet");
                        }
                        if (CormixType == 3)
                        {
                            if (TempTxt.Length > 69)
                            {
                                if (TempTxt.Substring(69, 1) != "+")
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    VPAndCormixResValues TempResValues = new VPAndCormixResValues();
                    if (IsShort)
                    {
                        if (CormixType == 1)
                        {
                            TempResValues.Distance = double.Parse(TempTxt.Substring(0, 10));
                            try
                            {
                                TempResValues.Dilu = double.Parse(TempTxt.Substring(27, 8));
                            }
                            catch (Exception)
                            {
                                TempResValues.Dilu = -999;
                            }
                            TempResValues.Conc = double.Parse(TempTxt.Substring(35, 10));
                            TempResValues.FarfieldWidth = double.Parse(TempTxt.Substring(46, 7));
                            TempResValues.TheTime = double.Parse(TempTxt.Substring(63, 11));
                            if (TempResValues.Distance > LastDistance)
                            {
                                LastDistance = TempResValues.Distance;
                                listOfCormixResVal.Add(TempResValues);
                            }
                        }
                        else if (CormixType == 2)
                        {
                        }
                        else if (CormixType == 3)
                        {
                            TempResValues.Distance = double.Parse(TempTxt.Substring(0, 10));
                            try
                            {
                                TempResValues.Dilu = double.Parse(TempTxt.Substring(27, 8));
                            }
                            catch (Exception)
                            {
                                TempResValues.Dilu = -999;
                            }
                            TempResValues.Conc = double.Parse(TempTxt.Substring(35, 10));
                            TempResValues.FarfieldWidth = double.Parse(TempTxt.Substring(53, 8));
                            TempResValues.TheTime = double.Parse(TempTxt.Substring(62, 10));
                            if (TempResValues.Distance > LastDistance)
                            {
                                LastDistance = TempResValues.Distance;
                                listOfCormixResVal.Add(TempResValues);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Error could not find proper Else if for CormixType - [" + CormixType.ToString() + "]");
                        }
                    }
                    else
                    {
                        TempResValues.Distance = double.Parse(TempTxt.Substring(0, 10));
                        try
                        {
                            TempResValues.Dilu = double.Parse(TempTxt.Substring(27, 8));
                        }
                        catch (Exception)
                        {
                            TempResValues.Dilu = -999;
                        }
                        TempResValues.Conc = double.Parse(TempTxt.Substring(35, 10));
                        TempResValues.FarfieldWidth = double.Parse(TempTxt.Substring(53, 7));
                        TempResValues.TheTime = double.Parse(TempTxt.Substring(77, 11));
                        if (TempResValues.Distance > LastDistance)
                        {
                            LastDistance = TempResValues.Distance;
                            listOfCormixResVal.Add(TempResValues);
                        }
                    }

                }

            }
            sr.Close();
            fs.Close();

            richTextBoxCormixParsedResults.AppendText(sb1.ToString());
            richTextBoxCormixParsedResults.AppendText(sb2.ToString());
            richTextBoxCormixParsedResults.AppendText(sb3.ToString());
            SaveCormixDetailAndParsedResults();
            SaveCormixResultsInDB();
        }
        private void SaveCormixResultsInDB()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

            Scenario sDB = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c).FirstOrDefault<Scenario>();

            // Should delete existing data in SelectedCormixResults and ValuedCormixResults
            List<SelectedCormixResult> lscr = (from l in vpse.SelectedCormixResults where l.ScenarioID == s.ScenarioID select l).ToList<SelectedCormixResult>();

            if (lscr.Count() > 0)
            {
                foreach (SelectedCormixResult scr in lscr)
                {
                    vpse.DeleteObject(scr);
                }
            }

            List<ValuedCormixResult> lvcr = (from l in vpse.ValuedCormixResults where l.ScenarioID == s.ScenarioID select l).ToList<ValuedCormixResult>();

            if (lvcr.Count() > 0)
            {
                foreach (ValuedCormixResult vcr in lvcr)
                {
                    vpse.DeleteObject(vcr);
                }
            }

            try
            {
                vpse.SaveChanges();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            // filling the ValuedResults Table
            int count = 0;
            foreach (VPAndCormixResValues v in listOfCormixResVal)
            {
                count += 1;
                ValuedCormixResult NewValuedResult = new ValuedCormixResult()
                {
                    ArrayNum = count,
                    Concentration = v.Conc,
                    Dilution = v.Dilu,
                    FarFieldWidth = v.FarfieldWidth,
                    DispersionDistance = v.Distance,
                    TravelTime = double.Parse(v.TheTime.ToString("F3"))
                };
                sDB.ValuedCormixResults.Add(NewValuedResult);
                if (v.Dilu > 1000 && v.Conc < 14)
                {
                    break;
                }
            }
            // Filling the Selected Results for Dilution 10000 ...
            SelectedCormixResult NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 10000,
                Concentration = double.Parse(Conc10000.ToString("F0")),
                Dilution = 10000,
                FarFieldWidth = double.Parse(FarWidth10000.ToString("F0")),
                DispersionDistance = double.Parse(Dist10000.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist10000.ToString("F0")),
                TravelTime = double.Parse(Time10000.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Dilution 1000 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 1000,
                Concentration = double.Parse(Conc1000.ToString("F0")),
                Dilution = 1000,
                FarFieldWidth = double.Parse(FarWidth1000.ToString("F0")),
                DispersionDistance = double.Parse(Dist1000.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist1000.ToString("F0")),
                TravelTime = double.Parse(Time1000.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Dilution 100 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 100,
                Concentration = double.Parse(Conc100.ToString("F0")),
                Dilution = 100,
                FarFieldWidth = double.Parse(FarWidth100.ToString("F0")),
                DispersionDistance = double.Parse(Dist100.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist100.ToString("F0")),
                TravelTime = double.Parse(Time100.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for DispersionDistance 300 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 300,
                Concentration = double.Parse(Conc300.ToString("F0")),
                Dilution = double.Parse(Dilu300.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth300.ToString("F0")),
                DispersionDistance = 300,
                CorrectedDistance = 300,
                TravelTime = double.Parse(Time300.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Concentration 88 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 88,
                Concentration = 88,
                Dilution = double.Parse(Dilu88.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth88.ToString("F0")),
                DispersionDistance = double.Parse(Dist88.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist88.ToString("F0")),
                TravelTime = double.Parse(Time88.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Concentration 14 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 14,
                Concentration = 14,
                Dilution = double.Parse(Dilu14.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth14.ToString("F0")),
                DispersionDistance = double.Parse(Dist14.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist14.ToString("F0")),
                TravelTime = double.Parse(Time14.ToString("F2"))
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 6 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 6,
                Concentration = Conc6,
                Dilution = double.Parse(Dilu6.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth6.ToString("F0")),
                DispersionDistance = double.Parse(Dist6.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist6.ToString("F0")),
                TravelTime = 6
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 12 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 12,
                Concentration = Conc12,
                Dilution = double.Parse(Dilu12.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth12.ToString("F0")),
                DispersionDistance = double.Parse(Dist12.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist12.ToString("F0")),
                TravelTime = 12
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 18 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 18,
                Concentration = Conc18,
                Dilution = double.Parse(Dilu18.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth18.ToString("F0")),
                DispersionDistance = double.Parse(Dist18.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist18.ToString("F0")),
                TravelTime = 18
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 24 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 24,
                Concentration = Conc24,
                Dilution = double.Parse(Dilu24.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth24.ToString("F0")),
                DispersionDistance = double.Parse(Dist24.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist24.ToString("F0")),
                TravelTime = 24
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 30 ...
            NewSelectedResult = new SelectedCormixResult()
            {
                ResType = 30,
                Concentration = Conc30,
                Dilution = double.Parse(Dilu30.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth30.ToString("F0")),
                DispersionDistance = double.Parse(Dist30.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist30.ToString("F0")),
                TravelTime = 30
            };
            sDB.SelectedCormixResults.Add(NewSelectedResult);


            try
            {
                vpse.SaveChanges();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private double GetCorrectedDistance(double DistVal, double TimeVal, double Dist6, double Dist12, double Dist18, double Dist24, double Dist30)
        {
            if (TimeVal >= 6)
            {
                if (TimeVal >= 12)
                {
                    if (TimeVal >= 18)
                    {
                        if (TimeVal >= 24)
                        {
                            if (TimeVal >= 30)
                            {
                                return DistVal - Dist24 + Dist18 - Dist12 + Dist6;
                            }
                            else
                            {
                                return Dist6 + Dist18 - Dist12;
                            }
                        }
                        else
                        {
                            return Dist6 + Dist18 - Dist12;
                        }
                    }
                    else
                    {
                        return DistVal - Dist12 + Dist6;
                    }
                }
                else
                {
                    return Dist6;
                }
            }
            else
            {
                return DistVal;
            }
        }
        private bool CompareAmbientValues(string[] ParsedValueTxt, string[] CurrentValueTxt, int Row)
        {
            double PrecisionFactor = 0.01;
            double ParsedValue;
            double CurrentValue;
            double HighValue;
            double LowValue;

            if (ParsedValueTxt[Row - 1] != CurrentValueTxt[Row - 1])
            {
                ParsedValue = double.Parse(ParsedValueTxt[Row - 1]);
                CurrentValue = double.Parse(GetLastValue(CurrentValueTxt, Row));
                HighValue = CurrentValue + CurrentValue * PrecisionFactor;
                LowValue = CurrentValue - CurrentValue * PrecisionFactor;
                if (ParsedValue > HighValue || ParsedValue < LowValue)
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
            }
            return true;
        }
        private bool CompareDiffuserValues(string ParsedValueTxt, string CurrentValueTxt)
        {
            double PrecisionFactor = 0.01;
            double ParsedValue;
            double CurrentValue;
            double HighValue;
            double LowValue;

            if (ParsedValueTxt != CurrentValueTxt)
            {
                ParsedValue = double.Parse(ParsedValueTxt);
                CurrentValue = double.Parse(CurrentValueTxt);
                HighValue = CurrentValue + CurrentValue * PrecisionFactor;
                LowValue = CurrentValue - CurrentValue * PrecisionFactor;
                if (ParsedValue > HighValue || ParsedValue < LowValue)
                {
                    richTextBoxParsedResults.Text = "";
                    return false;
                }
            }
            return true;
        }
        private string GetLastValue(string[] TheValue, int Row)
        {
            for (int i = Row; i > 0; i--)
            {
                if (TheValue[i - 1] != "")
                {
                    return TheValue[i - 1];
                }
            }
            return "";
        }
        private void SaveInfoInDB()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI InfrastructureItem = null;

            if (comboBoxInfrastructures.SelectedValue == null)
            {
                MessageBox.Show("Please select an infrastructure item");
                return;
            }
            if ((int)comboBoxInfrastructures.SelectedValue < 0)
            {
                MessageBox.Show("Please select an infrastructure item");
                return;
            }
            if (comboBoxSubInfrastructures.SelectedValue != null)
            {
                if ((int)comboBoxSubInfrastructures.SelectedValue > 0)
                {
                    InfrastructureItem = (TVI)comboBoxSubInfrastructures.SelectedItem;
                }
            }
            if (InfrastructureItem == null)
            {
                InfrastructureItem = (TVI)comboBoxInfrastructures.SelectedItem;
            }

            // keep going the scenario does not exist
            Scenario NewScenario = new Scenario();
            NewScenario.ScenarioDate = DateTime.Now;
            NewScenario.PortDiameter = (double)((PortDiameterCurrent == "") ? (double)-999 : double.Parse(PortDiameterCurrent));
            NewScenario.PortElevation = (double)((PortElevationCurrent == "") ? (double)-999 : double.Parse(PortElevationCurrent));
            NewScenario.VerticalAngle = (double)((VerticalAngleCurrent == "") ? (double)-999 : double.Parse(VerticalAngleCurrent));
            NewScenario.HorizontalAngle = (double)((HorizontalAngleCurrent == "") ? (double)-999 : double.Parse(HorizontalAngleCurrent));
            NewScenario.NumberOfPorts = (double)((NumberOfPortsCurrent == "") ? (double)-999 : double.Parse(NumberOfPortsCurrent));
            NewScenario.PortSpacing = (double)((PortSpacingCurrent == "") ? (double)-999 : double.Parse(PortSpacingCurrent));
            NewScenario.AcuteMixZone = (double)((AcuteMixZoneCurrent == "") ? (double)-999 : double.Parse(AcuteMixZoneCurrent));
            NewScenario.ChronicMixZone = (double)((ChronicMixZoneCurrent == "") ? (double)-999 : double.Parse(ChronicMixZoneCurrent));
            NewScenario.PortDepth = (double)((PortDepthCurrent == "") ? (double)-999 : double.Parse(PortDepthCurrent));
            NewScenario.EffluentFlow = (double)((EffluentFlowCurrent == "") ? (double)-999 : double.Parse(EffluentFlowCurrent));
            NewScenario.EffluentSalinity = (double)((EffluentSalinityCurrent == "") ? (double)-999 : double.Parse(EffluentSalinityCurrent));
            NewScenario.EffluentTemperature = (double)((EffluentTemperatureCurrent == "") ? (double)-999 : double.Parse(EffluentTemperatureCurrent));
            NewScenario.EffluentConcentration = (double)((EffluentConcentrationCurrent == "") ? (double)-999 : double.Parse(EffluentConcentrationCurrent));
            NewScenario.FroudeNumber = (double)((FroudeNumberValues[0] == "") ? (double)-999 : double.Parse(FroudeNumberValues[0]));
            NewScenario.EffluentVelocity = (double)((EffluentVelocityValues[0] == "") ? (double)-999 : double.Parse(EffluentVelocityValues[0]));
            NewScenario.ScenarioName = "Scenario_Flow_[" + NewScenario.EffluentFlow.ToString("F4") + "]_FC_[" + NewScenario.EffluentConcentration.ToString("F0") + "]";
            NewScenario.RawResults = richTextBoxRawResults.Text.ToString();
            NewScenario.ParsedResults = richTextBoxParsedResults.Text.ToString();

            // Filling the Ambients Table

            for (int Row = 1; Row < 6; Row++)
            {

                Ambient NewAmbient = new Ambient()
                {
                    Row = Row,
                    MeasurementDepth = (double)((MeasurementDepthCurrent[Row - 1] == "") ? (double)-999 : double.Parse(MeasurementDepthCurrent[Row - 1])),
                    CurrentSpeed = (double)((CurrentSpeedCurrent[Row - 1] == "") ? (double)-999 : double.Parse(CurrentSpeedCurrent[Row - 1])),
                    CurrentDirection = (double)((CurrentDirectionCurrent[Row - 1] == "") ? (double)-999 : double.Parse(CurrentDirectionCurrent[Row - 1])),
                    AmbientSalinity = (double)((AmbientSalinityCurrent[Row - 1] == "") ? (double)-999 : double.Parse(AmbientSalinityCurrent[Row - 1])),
                    AmbientTemperature = (double)((AmbientTemperatureCurrent[Row - 1] == "") ? (double)-999 : double.Parse(AmbientTemperatureCurrent[Row - 1])),
                    BackgroundConcentration = (double)((BackgroundConcentrationCurrent[Row - 1] == "") ? (double)-999 : double.Parse(BackgroundConcentrationCurrent[Row - 1])),
                    PollutantDecayRate = (double)((PollutantDecayRateCurrent[Row - 1] == "") ? (double)-999 : double.Parse(PollutantDecayRateCurrent[Row - 1])),
                    FarFieldCurrentSpeed = (double)((FarFieldCurrentSpeedCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldCurrentSpeedCurrent[Row - 1])),
                    FarFieldCurrentDirection = (double)((FarFieldCurrentDirectionCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldCurrentDirectionCurrent[Row - 1])),
                    FarFieldDiffusionCoefficient = (double)((FarFieldDiffusionCoefficientCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldDiffusionCoefficientCurrent[Row - 1]))
                };
                NewScenario.Ambients.Add(NewAmbient);
            }

            // filling the ValuedResults Table
            int count = 0;
            foreach (VPAndCormixResValues v in listOfResVal)
            {
                count += 1;
                ValuedResult NewValuedResult = new ValuedResult()
                {
                    ArrayNum = count,
                    Concentration = v.Conc,
                    Dilution = v.Dilu,
                    FarFieldWidth = v.FarfieldWidth,
                    DispersionDistance = v.Distance,
                    TravelTime = double.Parse(v.TheTime.ToString("F3"))
                };
                NewScenario.ValuedResults.Add(NewValuedResult);
                if (v.Dilu > 1000 && v.Conc < 14)
                {
                    break;
                }
            }
            // Filling the Selected Results for Dilution 10000 ...
            SelectedResult NewSelectedResult = new SelectedResult()
            {
                ResType = 10000,
                Concentration = double.Parse(Conc10000.ToString("F0")),
                Dilution = 10000,
                FarFieldWidth = double.Parse(FarWidth10000.ToString("F0")),
                DispersionDistance = double.Parse(Dist10000.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist10000.ToString("F0")),
                TravelTime = double.Parse(Time10000.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Dilution 1000 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 1000,
                Concentration = double.Parse(Conc1000.ToString("F0")),
                Dilution = 1000,
                FarFieldWidth = double.Parse(FarWidth1000.ToString("F0")),
                DispersionDistance = double.Parse(Dist1000.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist1000.ToString("F0")),
                TravelTime = double.Parse(Time1000.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Dilution 100 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 100,
                Concentration = double.Parse(Conc100.ToString("F0")),
                Dilution = 100,
                FarFieldWidth = double.Parse(FarWidth100.ToString("F0")),
                DispersionDistance = double.Parse(Dist100.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist100.ToString("F0")),
                TravelTime = double.Parse(Time100.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for DispersionDistance 300 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 300,
                Concentration = double.Parse(Conc300.ToString("F0")),
                Dilution = double.Parse(Dilu300.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth300.ToString("F0")),
                DispersionDistance = 300,
                CorrectedDistance = 300,
                TravelTime = double.Parse(Time300.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Concentration 88 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 88,
                Concentration = 88,
                Dilution = double.Parse(Dilu88.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth88.ToString("F0")),
                DispersionDistance = double.Parse(Dist88.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist88.ToString("F0")),
                TravelTime = double.Parse(Time88.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Concentration 14 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 14,
                Concentration = 14,
                Dilution = double.Parse(Dilu14.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth14.ToString("F0")),
                DispersionDistance = double.Parse(Dist14.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist14.ToString("F0")),
                TravelTime = double.Parse(Time14.ToString("F2"))
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 6 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 6,
                Concentration = Conc6,
                Dilution = double.Parse(Dilu6.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth6.ToString("F0")),
                DispersionDistance = double.Parse(Dist6.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist6.ToString("F0")),
                TravelTime = 6
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 12 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 12,
                Concentration = Conc12,
                Dilution = double.Parse(Dilu12.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth12.ToString("F0")),
                DispersionDistance = double.Parse(Dist12.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist12.ToString("F0")),
                TravelTime = 12
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 18 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 18,
                Concentration = Conc18,
                Dilution = double.Parse(Dilu18.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth18.ToString("F0")),
                DispersionDistance = double.Parse(Dist18.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist18.ToString("F0")),
                TravelTime = 18
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 24 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 24,
                Concentration = Conc24,
                Dilution = double.Parse(Dilu24.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth24.ToString("F0")),
                DispersionDistance = double.Parse(Dist24.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist24.ToString("F0")),
                TravelTime = 24
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            // Filling the Selected Results for Time 30 ...
            NewSelectedResult = new SelectedResult()
            {
                ResType = 30,
                Concentration = Conc30,
                Dilution = double.Parse(Dilu30.ToString("F0")),
                FarFieldWidth = double.Parse(FarWidth30.ToString("F0")),
                DispersionDistance = double.Parse(Dist30.ToString("F0")),
                CorrectedDistance = double.Parse(CorDist30.ToString("F0")),
                TravelTime = 30
            };
            NewScenario.SelectedResults.Add(NewSelectedResult);

            var SelectedItem = (from c in vpse.CSSPItems
                                where c.CSSPItemID == InfrastructureItem.ItemID
                                select c).FirstOrDefault();

            if (SelectedItem != null)
            {
                SelectedItem.Scenarios.Add(NewScenario);
            }

            try
            {
                vpse.SaveChanges();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private void ShowError(Exception ex)
        {
            MessageBox.Show("Error while saving information to CSSP DB.\r\n\r\nYou can check the bottom richtextbox to see the detail.");

            richTextBoxStatus.AppendText("\r\nError Message - [" + ex.Message + "]\r\n");
            if (ex.InnerException != null)
            {
                richTextBoxStatus.AppendText("\r\nInner Error Message - [" + ex.InnerException.Message + "]\r\n");
            }
        }
        private bool ScenarioAlreadyInDB()
        {
            int ScenarioExistID = 0;

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI InfrastructureItem = null;

            if (comboBoxInfrastructures.SelectedValue == null)
            {
                MessageBox.Show("Please select an infrastructure item");
                return false;
            }
            if (((TVI)comboBoxInfrastructures.SelectedItem).ItemID < 0)
            {
                MessageBox.Show("Please select an infrastructure item");
                return false;
            }
            InfrastructureItem = (TVI)comboBoxInfrastructures.SelectedItem;
            if (comboBoxSubInfrastructures.SelectedItem != null)
            {
                if (((TVI)comboBoxSubInfrastructures.SelectedItem).ItemID > 0)
                {
                    InfrastructureItem = (TVI)comboBoxSubInfrastructures.SelectedItem;
                }
            }
            // first check if a previous scenario with all the same parameter is already in the DB

            double TempPortDiameter = (double)((PortDiameterCurrent == "") ? (double)-999 : double.Parse(PortDiameterCurrent));
            double TempPortElevation = (double)((PortElevationCurrent == "") ? (double)-999 : double.Parse(PortElevationCurrent));
            double TempVerticalAngle = (double)((VerticalAngleCurrent == "") ? (double)-999 : double.Parse(VerticalAngleCurrent));
            double TempHorizontalAngle = (double)((HorizontalAngleCurrent == "") ? (double)-999 : double.Parse(HorizontalAngleCurrent));
            double TempNumberOfPorts = (double)((NumberOfPortsCurrent == "") ? (double)-999 : double.Parse(NumberOfPortsCurrent));
            double TempPortSpacing = (double)((PortSpacingCurrent == "") ? (double)-999 : double.Parse(PortSpacingCurrent));
            double TempAcuteMixZone = (double)((AcuteMixZoneCurrent == "") ? (double)-999 : double.Parse(AcuteMixZoneCurrent));
            double TempChronicMixZone = (double)((ChronicMixZoneCurrent == "") ? (double)-999 : double.Parse(ChronicMixZoneCurrent));
            double TempPortDepth = (double)((PortDepthCurrent == "") ? (double)-999 : double.Parse(PortDepthCurrent));
            double TempEffluentFlow = (double)((EffluentFlowCurrent == "") ? (double)-999 : double.Parse(EffluentFlowCurrent));
            double TempEffluentSalinity = (double)((EffluentSalinityCurrent == "") ? (double)-999 : double.Parse(EffluentSalinityCurrent));
            double TempEffluentTemperature = (double)((EffluentTemperatureCurrent == "") ? (double)-999 : double.Parse(EffluentTemperatureCurrent));
            double TempEffluentConcentration = (double)((EffluentConcentrationCurrent == "") ? (double)-999 : double.Parse(EffluentConcentrationCurrent));

            var TempScenarioAndCSSPItem = (from s in vpse.Scenarios
                                           where (s.PortDiameter == TempPortDiameter)
                                           && (s.PortElevation == TempPortElevation)
                                           && (s.VerticalAngle == TempVerticalAngle)
                                           && (s.HorizontalAngle == TempHorizontalAngle)
                                           && (s.NumberOfPorts == TempNumberOfPorts)
                                           && (s.PortSpacing == TempPortSpacing)
                                           && (s.AcuteMixZone == TempAcuteMixZone)
                                           && (s.ChronicMixZone == TempChronicMixZone)
                                           && (s.PortDepth == TempPortDepth)
                                           && (s.EffluentFlow == TempEffluentFlow)
                                           && (s.EffluentSalinity == TempEffluentSalinity)
                                           && (s.EffluentTemperature == TempEffluentTemperature)
                                           && (s.EffluentConcentration == TempEffluentConcentration)
                                           select new { s, s.CSSPItem });

            if (TempScenarioAndCSSPItem != null)
            {

                foreach (var sAndItem in TempScenarioAndCSSPItem)
                {
                    bool Row1Exist = false;
                    bool Row2Exist = false;
                    bool Row3Exist = false;
                    bool Row4Exist = false;
                    bool Row5Exist = false;
                    // already exist with the Diffuser parameters
                    // need to check if ambient parameters are also the same

                    int ScenarioID = sAndItem.s.ScenarioID;

                    // Doing row 1 of ambient values
                    for (int Row = 1; Row < 6; Row++)
                    {

                        double TempMeasurementDepth = (double)((MeasurementDepthCurrent[Row - 1] == "") ? (double)-999 : double.Parse(MeasurementDepthCurrent[Row - 1]));
                        double TempCurrentSpeed = (double)((CurrentSpeedCurrent[Row - 1] == "") ? (double)-999 : double.Parse(CurrentSpeedCurrent[Row - 1]));
                        double TempCurrentDirection = (double)((CurrentDirectionCurrent[Row - 1] == "") ? (double)-999 : double.Parse(CurrentDirectionCurrent[Row - 1]));
                        double TempAmbientSalinity = (double)((AmbientSalinityCurrent[Row - 1] == "") ? (double)-999 : double.Parse(AmbientSalinityCurrent[Row - 1]));
                        double TempAmbientTemperature = (double)((AmbientTemperatureCurrent[Row - 1] == "") ? (double)-999 : double.Parse(AmbientTemperatureCurrent[Row - 1]));
                        double TempBackgroundConcentration = (double)((BackgroundConcentrationCurrent[Row - 1] == "") ? (double)-999 : double.Parse(BackgroundConcentrationCurrent[Row - 1]));
                        double TempPollutantDecayRate = (double)((PollutantDecayRateCurrent[Row - 1] == "") ? (double)-999 : double.Parse(PollutantDecayRateCurrent[Row - 1]));
                        double TempFarFieldCurrentSpeed = (double)((FarFieldCurrentSpeedCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldCurrentSpeedCurrent[Row - 1]));
                        double TempFarFieldCurrentDirection = (double)((FarFieldCurrentDirectionCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldCurrentDirectionCurrent[Row - 1]));
                        double TempFarFieldDiffusionCoefficient = (double)((FarFieldDiffusionCoefficientCurrent[Row - 1] == "") ? (double)-999 : double.Parse(FarFieldDiffusionCoefficientCurrent[Row - 1]));
                        var AmbientRow1 = (from a in vpse.Ambients
                                           where (a.ScenarioID == ScenarioID)
                                           && (a.Row == Row)
                                           && (a.MeasurementDepth == TempMeasurementDepth)
                                           && (a.CurrentSpeed == TempCurrentSpeed)
                                           && (a.CurrentDirection == TempCurrentDirection)
                                           && (a.AmbientSalinity == TempAmbientSalinity)
                                           && (a.AmbientTemperature == TempAmbientTemperature)
                                           && (a.BackgroundConcentration == TempBackgroundConcentration)
                                           && (a.PollutantDecayRate == TempPollutantDecayRate)
                                           && (a.FarFieldCurrentSpeed == TempFarFieldCurrentSpeed)
                                           && (a.FarFieldCurrentDirection == TempFarFieldCurrentDirection)
                                           && (a.FarFieldDiffusionCoefficient == TempFarFieldDiffusionCoefficient)
                                           select a).FirstOrDefault();

                        if (AmbientRow1 != null)
                        {
                            switch (Row)
                            {
                                case 1:
                                    Row1Exist = true;
                                    break;
                                case 2:
                                    Row2Exist = true;
                                    break;
                                case 3:
                                    Row3Exist = true;
                                    break;
                                case 4:
                                    Row4Exist = true;
                                    break;
                                case 5:
                                    Row5Exist = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    if (Row1Exist && Row2Exist && Row3Exist && Row4Exist && Row5Exist)
                    {
                        richTextBoxRawResults.Text = sAndItem.s.RawResults;
                        richTextBoxParsedResults.Text = sAndItem.s.ParsedResults;
                        // check if this run has the same Municipality and Infrastructure name
                        if (sAndItem.CSSPItem.CSSPItemID == InfrastructureItem.ItemID)
                        {
                            // everything is the same no need to save another scenario containing different municipality and infrastructure name
                            ScenarioExistID = 0;
                            return true;
                        }
                        else
                        {
                            ScenarioExistID = sAndItem.s.ScenarioID;
                        }
                    }
                }
                if (ScenarioExistID > 0)
                {
                    if (checkBoxCopyIdenticalScenario.Checked == true)
                    {
                        ResaveScenarioWithNewMunicipalityOrInfrastructureName(ScenarioExistID);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }
        private void ResaveScenarioWithNewMunicipalityOrInfrastructureName(int ScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI InfrastructureItem = null;

            if (comboBoxInfrastructures.SelectedValue == null)
            {
                MessageBox.Show("Please select an infrastructure item");
                return;
            }
            if ((int)comboBoxInfrastructures.SelectedValue < 0)
            {
                MessageBox.Show("Please select an infrastructure item");
                return;
            }
            InfrastructureItem = (TVI)comboBoxInfrastructures.SelectedItem;
            if (comboBoxSubInfrastructures.SelectedValue != null)
            {
                if ((int)comboBoxSubInfrastructures.SelectedValue > 0)
                {
                    InfrastructureItem = (TVI)comboBoxSubInfrastructures.SelectedItem;
                }
            }

            var OldScenario = (from s in vpse.Scenarios
                               where s.ScenarioID == ScenarioID
                               select s).FirstOrDefault();

            if (OldScenario == null)
            {
                MessageBox.Show("Could not find Scenario with ScenarioID = [" + ScenarioID + "] in the DB");
                return;
            }

            var SelectedItem = (from c in vpse.CSSPItems
                                where c.CSSPItemID == InfrastructureItem.ItemID
                                select c).FirstOrDefault();


            // keep going the scenario does not exist
            Scenario NewScenario = new Scenario()
            {
                ScenarioDate = DateTime.Now,
                ScenarioName = "Copy of " + OldScenario.ScenarioName,
                PortDiameter = OldScenario.PortDiameter,
                PortElevation = OldScenario.PortElevation,
                VerticalAngle = OldScenario.VerticalAngle,
                HorizontalAngle = OldScenario.HorizontalAngle,
                NumberOfPorts = OldScenario.NumberOfPorts,
                PortSpacing = OldScenario.PortSpacing,
                AcuteMixZone = OldScenario.AcuteMixZone,
                ChronicMixZone = OldScenario.ChronicMixZone,
                PortDepth = OldScenario.PortDepth,
                EffluentFlow = OldScenario.EffluentFlow,
                EffluentSalinity = OldScenario.EffluentSalinity,
                EffluentTemperature = OldScenario.EffluentTemperature,
                EffluentConcentration = OldScenario.EffluentConcentration,
                FroudeNumber = OldScenario.FroudeNumber,
                EffluentVelocity = OldScenario.EffluentVelocity,
                RawResults = OldScenario.RawResults,
                ParsedResults = OldScenario.ParsedResults
            };

            // Filling the Ambients Table

            for (int Row = 1; Row < 6; Row++)
            {
                var OldAmbient = (from a in vpse.Ambients
                                  where a.ScenarioID == ScenarioID
                                  && a.Row == Row
                                  select a).FirstOrDefault();

                if (OldAmbient == null)
                {
                    MessageBox.Show("Could not find ambient values with ScenarioID = [" + ScenarioID + "] and Row = [" + Row + "] in the DB");
                    return;
                }

                Ambient NewAmbient = new Ambient()
                {
                    Row = Row,
                    MeasurementDepth = OldAmbient.MeasurementDepth,
                    CurrentSpeed = OldAmbient.CurrentSpeed,
                    CurrentDirection = OldAmbient.CurrentDirection,
                    AmbientSalinity = OldAmbient.AmbientSalinity,
                    AmbientTemperature = OldAmbient.AmbientTemperature,
                    BackgroundConcentration = OldAmbient.BackgroundConcentration,
                    PollutantDecayRate = OldAmbient.PollutantDecayRate,
                    FarFieldCurrentSpeed = OldAmbient.FarFieldCurrentSpeed,
                    FarFieldCurrentDirection = OldAmbient.FarFieldCurrentDirection,
                    FarFieldDiffusionCoefficient = OldAmbient.FarFieldDiffusionCoefficient
                };
                NewScenario.Ambients.Add(NewAmbient);
            }

            var OldValuedResult = from r in vpse.ValuedResults
                                  where r.ScenarioID == ScenarioID
                                  orderby r.ArrayNum ascending
                                  select r;

            if (OldValuedResult == null)
            {
                MessageBox.Show("Could not find ValuedResults with ScenarioID = [" + ScenarioID + "] in the DB");
                return;
            }

            // filling the ValuedResults Table
            foreach (ValuedResult vr in OldValuedResult)
            {
                ValuedResult NewValuedResult = new ValuedResult()
                {
                    ArrayNum = vr.ArrayNum,
                    Concentration = vr.Concentration,
                    Dilution = vr.Dilution,
                    FarFieldWidth = vr.FarFieldWidth,
                    DispersionDistance = vr.DispersionDistance,
                    TravelTime = vr.TravelTime
                };
                NewScenario.ValuedResults.Add(NewValuedResult);
            }

            var OldSelectedResults = from sr in vpse.SelectedResults
                                     where sr.ScenarioID == ScenarioID
                                     select sr;

            if (OldSelectedResults == null)
            {
                MessageBox.Show("Could not find SelectedResults with ScenarioID = [" + ScenarioID + "] in the DB");
                return;
            }

            // filling the SelectedResults Table
            foreach (SelectedResult sr in OldSelectedResults)
            {
                SelectedResult NewSelectedResult = new SelectedResult()
                {
                    ResType = sr.ResType,
                    Concentration = sr.Concentration,
                    Dilution = sr.Dilution,
                    FarFieldWidth = sr.FarFieldWidth,
                    DispersionDistance = sr.DispersionDistance,
                    TravelTime = sr.TravelTime
                };
                NewScenario.SelectedResults.Add(NewSelectedResult);

            }

            // Filling the Senarios table with all its related information
            if (SelectedItem != null)
            {
                SelectedItem.Scenarios.Add(NewScenario);
            }

            try
            {
                vpse.SaveChanges();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private void ExecuteScenario()
        {
            int CountSearchForSaveDialogBox = 0;
            int CountParsed = 0;
            richTextBoxRawResults.Text = "";
            richTextBoxParsedResults.Text = "";
            File.Delete(lblSaveResultFileName.Text);
            MegaDoEvents();
            MegaDoEvents();
            MegaDoEvents();
            for (int i = 0; i < 3; i++)
            {
                StopVPExecutionOfScenario();
                lblError.Text = "Doing Click Clear Button ...";
                ClickClearButton();
                MegaDoEvents();
            }
            lblError.Text = "Doing Start VP Execute Scenario ...";
            StartVPExecuteOfScenario();
            MegaDoEvents();
            while (timerStopExecutionAfterOneSecond.Enabled)
            {
                // waiting for the execution to complete
                MegaDoEvents();
            }
            lblError.Text = "Doing Click On VP Save Results ...";
            ClickOnVPSaveResults();
            MegaDoEvents();

            FillDesktopWindowsChildrenList(false);
            WndHandleAndTitle wht = DesktopChildrenWindowsList.Where(u => u.Title == "Save Plumes Output").FirstOrDefault();
            while (wht == null)
            {
                MegaDoEvents();
                FillDesktopWindowsChildrenList(false);
                wht = DesktopChildrenWindowsList.Where(u => u.Title == "Save Plumes Output").FirstOrDefault();
                CountSearchForSaveDialogBox += 1;
                if (CountSearchForSaveDialogBox > 100)
                {
                    richTextBoxStatus.AppendText("ERROR - Could not find the [Save Plumes Output] dialog box\r\n");
                    return;
                }
            }

            IntPtr hWndFileNameTextBox = af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(
                af.APIGetWindow(wht.Handle, GW_CHILD)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT)
                , GW_HWNDNEXT);

            richTextBoxStatus.AppendText("hWndFileNameTextBox = [" + hWndFileNameTextBox + "]\r\n");
            if (hWndFileNameTextBox != IntPtr.Zero)
            {
                af.APISetForegroundWindow(wht.Handle);
                MegaDoEvents();
                while (af.APIGetForegroundWindow() != wht.Handle)
                {
                    MegaDoEvents();
                }
                af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                for (int i = 0; i < 100; i++)
                {
                    SendKeys.SendWait("{BACKSPACE}{DELETE}");
                }
                SendKeys.SendWait(lblSaveResultFileName.Text);
            }
            else
            {
                richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                return;
            }

            IntPtr hWndSaveButton = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(wht.Handle, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT);
            richTextBoxStatus.AppendText("hWndSaveButton = [" + hWndSaveButton + "]\r\n");

            if (hWndSaveButton != IntPtr.Zero)
            {
                af.APISetForegroundWindow(wht.Handle);
                MegaDoEvents();
                while (af.APIGetForegroundWindow() != wht.Handle)
                {
                    MegaDoEvents();
                }
                af.APIPostMouseClick(hWndSaveButton, 10, 10);
            }
            else
            {
                richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                return;
            }

            lblError.Text = "Doing Load Results in RTB ...";
            while (!File.Exists(lblSaveResultFileName.Text))
            {
                MegaDoEvents();
            }
            LoadResultsInRTB();
            MegaDoEvents();
            MegaDoEvents();
            lblError.Text = "Doing Parse VP Results ...";
            while (!ParseVPResults())
            {
                richTextBoxRawResults.Text = "";
                richTextBoxParsedResults.Text = "";
                for (int i = 0; i < 3; i++)
                {
                    StopVPExecutionOfScenario();
                    MegaDoEvents();
                    ClickClearButton();
                    MegaDoEvents();
                }
                File.Delete(lblSaveResultFileName.Text);
                //lblError.Text = "Doing Start VP Execute Scenario ...";
                //StartVPExecuteOfScenario();
                //MegaDoEvents();
                //lblError.Text = "Doing Click On VP Save Results ...";
                //ClickClearButton();
                //MegaDoEvents();
                //MegaDoEvents();
                lblError.Text = "Doing Start VP Execute Scenario ...";
                StartVPExecuteOfScenario();
                MegaDoEvents();
                while (timerStopExecutionAfterOneSecond.Enabled)
                {
                    // waiting for the execution to complete
                    MegaDoEvents();
                }
                MegaDoEvents(); // added August 5, 2010
                lblError.Text = "Doing Click On VP Save Results ...";
                ClickOnVPSaveResults();
                MegaDoEvents();

                FillDesktopWindowsChildrenList(false);
                wht = DesktopChildrenWindowsList.Where(u => u.Title == "Save Plumes Output").FirstOrDefault();
                while (wht == null)
                {
                    MegaDoEvents();
                    MegaDoEvents();
                    FillDesktopWindowsChildrenList(false);
                    wht = DesktopChildrenWindowsList.Where(u => u.Title == "Save Plumes Output").FirstOrDefault();
                    CountSearchForSaveDialogBox += 1;
                    if (CountSearchForSaveDialogBox > 100)
                    {
                        richTextBoxStatus.AppendText("ERROR - Could not find the [Save Plumes Output] dialog box\r\n");
                        return;
                    }
                }

                hWndFileNameTextBox = af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(
                    af.APIGetWindow(wht.Handle, GW_CHILD)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT)
                    , GW_HWNDNEXT);

                richTextBoxStatus.AppendText("hWndFileNameTextBox = [" + hWndFileNameTextBox + "]\r\n");
                if (hWndFileNameTextBox != IntPtr.Zero)
                {
                    af.APISetForegroundWindow(wht.Handle);
                    MegaDoEvents();
                    while (af.APIGetForegroundWindow() != wht.Handle)
                    {
                        MegaDoEvents();
                    }
                    af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                    af.APISendMouseClick(hWndFileNameTextBox, 10, 10);
                    for (int i = 0; i < 100; i++)
                    {
                        SendKeys.SendWait("{BACKSPACE}{DELETE}");
                    }
                    SendKeys.SendWait(lblSaveResultFileName.Text);
                }
                else
                {
                    richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                    return;
                }

                hWndSaveButton = af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(af.APIGetWindow(wht.Handle, GW_CHILD), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT), GW_HWNDNEXT);
                richTextBoxStatus.AppendText("hWndSaveButton = [" + hWndSaveButton + "]\r\n");

                if (hWndSaveButton != IntPtr.Zero)
                {
                    af.APISetForegroundWindow(wht.Handle);
                    MegaDoEvents();
                    while (af.APIGetForegroundWindow() != wht.Handle)
                    {
                        MegaDoEvents();
                    }
                    af.APIPostMouseClick(hWndSaveButton, 10, 10);
                }
                else
                {
                    richTextBoxStatus.AppendText("hWndFileNameTextBox != IntPtr.Zero\r\n");
                    return;
                }

                lblError.Text = "Doing Load Results in RTB ...";
                while (!File.Exists(lblSaveResultFileName.Text))
                {
                    MegaDoEvents();
                }
                LoadResultsInRTB();
                MegaDoEvents();
                MegaDoEvents();
                lblError.Text = "Doing Parse VP Results ...";
                CountParsed += 1;
                if (CountParsed > 5)
                    break;
            }
            MegaDoEvents();
            MegaDoEvents();
            //if (richTextBoxRawResults.Text.IndexOf(" UM3.") >= 0)
            if (CountParsed < 6 && richTextBoxParsedResults.Text.Length > 0)
            {
                lblError.Text = "Completed ...";
                ScenarioRunningNumber += 1;
                lblScenariosCompletedValue.Text = ScenarioRunningNumber.ToString();
                lblScenariosCompletedValue.Refresh();
                SaveInfoInDB();
            }
            else
            {
                MessageBox.Show("An error keeps happenning during the auto execute of Visual Plumes.\r\nYou will need to close Visual Plumes and the AutoRunVP application or click on the Start VP button.");
                PleaseStopRecursiveRun = true;
            }

        }
        private void DoDiffuserSpecificRun(DiffuserVariable dv)
        {
            switch (dv)
            {
                case DiffuserVariable.PortDiameter:
                    RecursiveDiffuserRun(PortElevationValues, DiffuserVariable.PortElevation);
                    break;
                case DiffuserVariable.PortElevation:
                    RecursiveDiffuserRun(VerticalAngleValues, DiffuserVariable.VerticalAngle);
                    break;
                case DiffuserVariable.VerticalAngle:
                    RecursiveDiffuserRun(HorizontalAngleValues, DiffuserVariable.HorizontalAngle);
                    break;
                case DiffuserVariable.HorizontalAngle:
                    RecursiveDiffuserRun(NumberOfPortsValues, DiffuserVariable.NumberOfPorts);
                    break;
                case DiffuserVariable.NumberOfPorts:
                    RecursiveDiffuserRun(PortSpacingValues, DiffuserVariable.PortSpacing);
                    break;
                case DiffuserVariable.PortSpacing:
                    RecursiveDiffuserRun(AcuteMixZoneValues, DiffuserVariable.AcuteMixZone);
                    break;
                case DiffuserVariable.AcuteMixZone:
                    RecursiveDiffuserRun(ChronicMixZoneValues, DiffuserVariable.ChronicMixZone);
                    break;
                case DiffuserVariable.ChronicMixZone:
                    RecursiveDiffuserRun(PortDepthValues, DiffuserVariable.PortDepth);
                    break;
                case DiffuserVariable.PortDepth:
                    RecursiveDiffuserRun(EffluentFlowValues, DiffuserVariable.EffluentFlow);
                    break;
                case DiffuserVariable.EffluentFlow:
                    RecursiveDiffuserRun(EffluentSalinityValues, DiffuserVariable.EffluentSalinity);
                    break;
                case DiffuserVariable.EffluentSalinity:
                    RecursiveDiffuserRun(EffluentTemperatureValues, DiffuserVariable.EffluentTemperature);
                    break;
                case DiffuserVariable.EffluentTemperature:
                    RecursiveDiffuserRun(EffluentConcentrationValues, DiffuserVariable.EffluentConcentration);
                    break;
                case DiffuserVariable.EffluentConcentration:
                    RecursiveAmbientRun(MeasurementDepthValues[0], AmbientVariable.MeasurementDepth, 1);
                    break;
                default:
                    break;
            }
        }
        private void DoAmbientSpecificRun(AmbientVariable av, int Row)
        {
            switch (av)
            {
                case AmbientVariable.MeasurementDepth:
                    RecursiveAmbientRun(CurrentSpeedValues[Row - 1], AmbientVariable.CurrentSpeeds, Row);
                    break;
                case AmbientVariable.CurrentSpeeds:
                    RecursiveAmbientRun(CurrentDirectionValues[Row - 1], AmbientVariable.CurrentDirections, Row);
                    break;
                case AmbientVariable.CurrentDirections:
                    RecursiveAmbientRun(AmbientSalinityValues[Row - 1], AmbientVariable.AmbientSalinity, Row);
                    break;
                case AmbientVariable.AmbientSalinity:
                    RecursiveAmbientRun(AmbientTemperatureValues[Row - 1], AmbientVariable.AmbientTemperature, Row);
                    break;
                case AmbientVariable.AmbientTemperature:
                    RecursiveAmbientRun(BackgroundConcentrationValues[Row - 1], AmbientVariable.BackgroundConcentration, Row);
                    break;
                case AmbientVariable.BackgroundConcentration:
                    RecursiveAmbientRun(PollutantDecayRateValues[Row - 1], AmbientVariable.PollutantDecayRate, Row);
                    break;
                case AmbientVariable.PollutantDecayRate:
                    RecursiveAmbientRun(FarFieldCurrentSpeedValues[Row - 1], AmbientVariable.FarFieldCurrentSpeed, Row);
                    break;
                case AmbientVariable.FarFieldCurrentSpeed:
                    RecursiveAmbientRun(FarFieldCurrentDirectionValues[Row - 1], AmbientVariable.FarFieldCurrentDirection, Row);
                    break;
                case AmbientVariable.FarFieldCurrentDirection:
                    RecursiveAmbientRun(FarFieldDiffusionCoefficientValues[Row - 1], AmbientVariable.FarFieldDiffusionCoefficient, Row);
                    break;
                case AmbientVariable.FarFieldDiffusionCoefficient:
                    if (Row < 5)
                        RecursiveAmbientRun(MeasurementDepthValues[Row], AmbientVariable.MeasurementDepth, Row + 1);
                    break;
                default:
                    break;
            }
        }
        private void RecursiveDiffuserRun(string[] TheValues, DiffuserVariable dv)
        {
            if (PleaseStopRecursiveRun)
                return;

            if (TheValues[1] != "")
            {
                SetTempDiffuserCurrentValues(TheValues[0], dv);
                //string CurrentValue = TheValues[0];
                double IncrementPE = double.Parse(((double.Parse(TheValues[1]) - double.Parse(GetTempDiffuserCurrentValue(dv))) / double.Parse(TheValues[2])).ToString("F4"));

                while (double.Parse(GetTempDiffuserCurrentValue(dv)) <= double.Parse(TheValues[1]))
                {
                    if (PleaseStopRecursiveRun)
                        return;

                    StopVPExecutionOfScenario();
                    DiffuserEnterData(dv, GetTempDiffuserCurrentValue(dv));

                    if (!CheckIfAllTheInformationIsCorrect())
                        return;

                    richTextBoxRawResults.Text = "";
                    richTextBoxParsedResults.Text = "";
                    lblScenariosCompletedValue.Refresh();
                    if (!ScenarioAlreadyInDB())
                    {
                        ExecuteScenario();
                    }

                    DoDiffuserSpecificRun(dv);

                    SetTempDiffuserCurrentValues((double.Parse(GetTempDiffuserCurrentValue(dv)) + IncrementPE).ToString(), dv);
                }
                SetTempDiffuserCurrentValues(TheValues[0], dv);
                DiffuserEnterData(dv, GetTempDiffuserCurrentValue(dv));
            }
            else
            {
                DoDiffuserSpecificRun(dv);
            }
        }
        private void RecursiveAmbientRun(string[] TheValues, AmbientVariable av, int Row)
        {
            if (PleaseStopRecursiveRun)
                return;

            if (TheValues[1] != "")
            {
                SetTempAmbientCurrentValues(TheValues[0], av, Row);
                //string CurrentValue = TheValues[0];
                double IncrementPE = double.Parse(((double.Parse(TheValues[1]) - double.Parse(GetTempAmbientCurrentValue(av, Row))) / double.Parse(TheValues[2])).ToString("F4"));

                while (double.Parse(GetTempAmbientCurrentValue(av, Row)) <= double.Parse(TheValues[1]))
                {
                    if (PleaseStopRecursiveRun)
                        return;

                    StopVPExecutionOfScenario();
                    AmbientEnterData(av, GetTempAmbientCurrentValue(av, Row), Row);

                    if (!CheckIfAllTheInformationIsCorrect())
                        return;

                    richTextBoxRawResults.Text = "";
                    richTextBoxParsedResults.Text = "";
                    if (!ScenarioAlreadyInDB())
                    {
                        ExecuteScenario();
                    }

                    DoAmbientSpecificRun(av, Row);

                    SetTempAmbientCurrentValues((double.Parse(GetTempAmbientCurrentValue(av, Row)) + IncrementPE).ToString(), av, Row);
                }
                SetTempAmbientCurrentValues(TheValues[0], av, Row);
                AmbientEnterData(av, GetTempAmbientCurrentValue(av, Row), Row);
            }
            else
            {
                DoAmbientSpecificRun(av, Row);
            }
        }
        private string GetTempDiffuserCurrentValue(DiffuserVariable dv)
        {
            switch (dv)
            {
                case DiffuserVariable.PortDiameter:
                    return PortDiameterCurrent;
                case DiffuserVariable.PortElevation:
                    return PortElevationCurrent;
                case DiffuserVariable.VerticalAngle:
                    return VerticalAngleCurrent;
                case DiffuserVariable.HorizontalAngle:
                    return HorizontalAngleCurrent;
                case DiffuserVariable.NumberOfPorts:
                    return NumberOfPortsCurrent;
                case DiffuserVariable.PortSpacing:
                    return PortSpacingCurrent;
                case DiffuserVariable.AcuteMixZone:
                    return AcuteMixZoneCurrent;
                case DiffuserVariable.ChronicMixZone:
                    return ChronicMixZoneCurrent;
                case DiffuserVariable.PortDepth:
                    return PortDepthCurrent;
                case DiffuserVariable.EffluentFlow:
                    return EffluentFlowCurrent;
                case DiffuserVariable.EffluentSalinity:
                    return EffluentSalinityCurrent;
                case DiffuserVariable.EffluentTemperature:
                    return EffluentTemperatureCurrent;
                case DiffuserVariable.EffluentConcentration:
                    return EffluentConcentrationCurrent;
                default:
                    return "";
            }
        }
        private string GetTempAmbientCurrentValue(AmbientVariable av, int Row)
        {
            switch (av)
            {
                case AmbientVariable.MeasurementDepth:
                    return MeasurementDepthCurrent[Row - 1];
                case AmbientVariable.CurrentSpeeds:
                    return CurrentSpeedCurrent[Row - 1];
                case AmbientVariable.CurrentDirections:
                    return CurrentDirectionCurrent[Row - 1];
                case AmbientVariable.AmbientSalinity:
                    return AmbientSalinityCurrent[Row - 1];
                case AmbientVariable.AmbientTemperature:
                    return AmbientTemperatureCurrent[Row - 1];
                case AmbientVariable.BackgroundConcentration:
                    return BackgroundConcentrationCurrent[Row - 1];
                case AmbientVariable.PollutantDecayRate:
                    return PollutantDecayRateCurrent[Row - 1];
                case AmbientVariable.FarFieldCurrentSpeed:
                    return FarFieldCurrentSpeedCurrent[Row - 1];
                case AmbientVariable.FarFieldCurrentDirection:
                    return FarFieldCurrentDirectionCurrent[Row - 1];
                case AmbientVariable.FarFieldDiffusionCoefficient:
                    return FarFieldDiffusionCoefficientCurrent[Row - 1];
                default:
                    return "";
            }
        }
        private void SetTempDiffuserCurrentValues(string TheValue, DiffuserVariable dv)
        {
            switch (dv)
            {
                case DiffuserVariable.PortDiameter:
                    PortDiameterCurrent = TheValue;
                    lblDoingPortDiameter.Text = PortDiameterCurrent;
                    lblDoingPortDiameter.Refresh();
                    break;
                case DiffuserVariable.PortElevation:
                    PortElevationCurrent = TheValue;
                    lblDoingPortElevation.Text = PortElevationCurrent;
                    lblDoingPortElevation.Refresh();
                    break;
                case DiffuserVariable.VerticalAngle:
                    VerticalAngleCurrent = TheValue;
                    lblDoingVerticalAngle.Text = VerticalAngleCurrent;
                    lblDoingVerticalAngle.Refresh();
                    break;
                case DiffuserVariable.HorizontalAngle:
                    HorizontalAngleCurrent = TheValue;
                    lblDoingHorizontalAngle.Text = HorizontalAngleCurrent;
                    lblDoingHorizontalAngle.Refresh();
                    break;
                case DiffuserVariable.NumberOfPorts:
                    NumberOfPortsCurrent = TheValue;
                    lblDoingNumberOfPort.Text = NumberOfPortsCurrent;
                    lblDoingNumberOfPort.Refresh();
                    break;
                case DiffuserVariable.PortSpacing:
                    PortSpacingCurrent = TheValue;
                    lblDoingPortSpacing.Text = PortSpacingCurrent;
                    lblDoingPortSpacing.Refresh();
                    break;
                case DiffuserVariable.AcuteMixZone:
                    AcuteMixZoneCurrent = TheValue;
                    lblDoingAcuteMixZone.Text = AcuteMixZoneCurrent;
                    lblDoingAcuteMixZone.Refresh();
                    break;
                case DiffuserVariable.ChronicMixZone:
                    ChronicMixZoneCurrent = TheValue;
                    lblDoingChronicMixZone.Text = ChronicMixZoneCurrent;
                    lblDoingChronicMixZone.Refresh();
                    break;
                case DiffuserVariable.PortDepth:
                    PortDepthCurrent = TheValue;
                    lblDoingPortDepth.Text = PortDepthCurrent;
                    lblDoingPortDepth.Refresh();
                    break;
                case DiffuserVariable.EffluentFlow:
                    EffluentFlowCurrent = TheValue;
                    lblDoingEffluentFlow.Text = EffluentFlowCurrent;
                    lblDoingEffluentFlow.Refresh();
                    break;
                case DiffuserVariable.EffluentSalinity:
                    EffluentSalinityCurrent = TheValue;
                    lblDoingEffluentSalinity.Text = EffluentSalinityCurrent;
                    lblDoingEffluentSalinity.Refresh();
                    break;
                case DiffuserVariable.EffluentTemperature:
                    EffluentTemperatureCurrent = TheValue;
                    lblDoingEffluentTemperature.Text = EffluentTemperatureCurrent;
                    lblDoingEffluentTemperature.Refresh();
                    break;
                case DiffuserVariable.EffluentConcentration:
                    EffluentConcentrationCurrent = TheValue;
                    lblDoingEffluentConcentration.Text = EffluentConcentrationCurrent;
                    lblDoingEffluentConcentration.Refresh();
                    break;
                default:
                    break;
            }

        }
        private void SetTempAmbientCurrentValues(string TheValue, AmbientVariable av, int Row)
        {
            comboBoxInputRow.SelectedIndex = Row - 1;
            comboBoxInputRow.Refresh();

            switch (av)
            {
                case AmbientVariable.MeasurementDepth:
                    MeasurementDepthCurrent[Row - 1] = TheValue;
                    lblDoingMeasurementDepth.Text = MeasurementDepthCurrent[Row - 1];
                    lblDoingMeasurementDepth.Refresh();
                    break;
                case AmbientVariable.CurrentSpeeds:
                    CurrentSpeedCurrent[Row - 1] = TheValue;
                    lblDoingCurrentSpeed.Text = CurrentSpeedCurrent[Row - 1];
                    lblDoingCurrentSpeed.Refresh();
                    break;
                case AmbientVariable.CurrentDirections:
                    CurrentDirectionCurrent[Row - 1] = TheValue;
                    lblDoingCurrentDirection.Text = CurrentDirectionCurrent[Row - 1];
                    lblDoingCurrentDirection.Refresh();
                    break;
                case AmbientVariable.AmbientSalinity:
                    AmbientSalinityCurrent[Row - 1] = TheValue;
                    lblDoingAmbientSalinity.Text = AmbientSalinityCurrent[Row - 1];
                    lblDoingAmbientSalinity.Refresh();
                    break;
                case AmbientVariable.AmbientTemperature:
                    AmbientTemperatureCurrent[Row - 1] = TheValue;
                    lblDoingAmbientTemperature.Text = AmbientTemperatureCurrent[Row - 1];
                    lblDoingAmbientTemperature.Refresh();
                    break;
                case AmbientVariable.BackgroundConcentration:
                    BackgroundConcentrationCurrent[Row - 1] = TheValue;
                    lblDoingBackgroundConcentration.Text = BackgroundConcentrationCurrent[Row - 1];
                    lblDoingBackgroundConcentration.Refresh();
                    break;
                case AmbientVariable.PollutantDecayRate:
                    PollutantDecayRateCurrent[Row - 1] = TheValue;
                    lblDoingPollutantDecayRate.Text = PollutantDecayRateCurrent[Row - 1];
                    lblDoingPollutantDecayRate.Refresh();
                    break;
                case AmbientVariable.FarFieldCurrentSpeed:
                    FarFieldCurrentSpeedCurrent[Row - 1] = TheValue;
                    lblDoingFarFieldCurrentSpeed.Text = FarFieldCurrentSpeedCurrent[Row - 1];
                    lblDoingFarFieldCurrentSpeed.Refresh();
                    break;
                case AmbientVariable.FarFieldCurrentDirection:
                    FarFieldCurrentDirectionCurrent[Row - 1] = TheValue;
                    lblDoingFarFieldCurrentDirection.Text = FarFieldCurrentDirectionCurrent[Row - 1];
                    lblDoingFarFieldCurrentDirection.Refresh();
                    break;
                case AmbientVariable.FarFieldDiffusionCoefficient:
                    FarFieldDiffusionCoefficientCurrent[Row - 1] = TheValue;
                    lblDoingFarFieldDiffusionCoefficient.Text = FarFieldDiffusionCoefficientCurrent[Row - 1];
                    lblDoingFarFieldDiffusionCoefficient.Refresh();
                    break;
                default:
                    break;
            }
        }
        private void RunManyScenarios()
        {
            PleaseStopRecursiveRun = false;
            richTextBoxRawResults.Text = "";
            richTextBoxParsedResults.Text = "";

            lblError.Text = "Doing Diffuser Fill Values ...";
            DiffuserFillValues();
            lblError.Text = "Doing Ambient Fill Values ...";
            AmbientFillValues();
            SetAllCurrentValues();
            RecursiveDiffuserRun(PortDiameterValues, DiffuserVariable.PortDiameter);
        }
        private void SetAllCurrentValues()
        {
            PortDiameterCurrent = PortDiameterValues[0];
            PortElevationCurrent = PortElevationValues[0];
            VerticalAngleCurrent = VerticalAngleValues[0];
            HorizontalAngleCurrent = HorizontalAngleValues[0];
            NumberOfPortsCurrent = NumberOfPortsValues[0];
            PortSpacingCurrent = PortSpacingValues[0];
            AcuteMixZoneCurrent = AcuteMixZoneValues[0];
            ChronicMixZoneCurrent = ChronicMixZoneValues[0];
            PortDepthCurrent = PortDepthValues[0];
            EffluentFlowCurrent = EffluentFlowValues[0];
            EffluentSalinityCurrent = EffluentSalinityValues[0];
            EffluentTemperatureCurrent = EffluentTemperatureValues[0];
            EffluentConcentrationCurrent = EffluentConcentrationValues[0];
            FroudeNumberCurrent = FroudeNumberValues[0];
            EffluentVelocityCurrent = EffluentVelocityValues[0];

            for (int Row = 1; Row < 6; Row++)
            {
                MeasurementDepthCurrent[Row - 1] = MeasurementDepthValues[Row - 1][0];
                CurrentSpeedCurrent[Row - 1] = CurrentSpeedValues[Row - 1][0];
                CurrentDirectionCurrent[Row - 1] = CurrentDirectionValues[Row - 1][0];
                AmbientSalinityCurrent[Row - 1] = AmbientSalinityValues[Row - 1][0];
                AmbientTemperatureCurrent[Row - 1] = AmbientTemperatureValues[Row - 1][0];
                BackgroundConcentrationCurrent[Row - 1] = BackgroundConcentrationValues[Row - 1][0];
                PollutantDecayRateCurrent[Row - 1] = PollutantDecayRateValues[Row - 1][0];
                FarFieldCurrentSpeedCurrent[Row - 1] = FarFieldCurrentSpeedValues[Row - 1][0];
                FarFieldCurrentDirectionCurrent[Row - 1] = FarFieldCurrentDirectionValues[Row - 1][0];
                FarFieldDiffusionCoefficientCurrent[Row - 1] = FarFieldDiffusionCoefficientValues[Row - 1][0];

            }

            lblDoingPortDiameter.Text = PortDiameterCurrent;
            lblDoingPortElevation.Text = PortElevationCurrent;
            lblDoingVerticalAngle.Text = VerticalAngleCurrent;
            lblDoingHorizontalAngle.Text = HorizontalAngleCurrent;
            lblDoingNumberOfPort.Text = NumberOfPortsCurrent;
            lblDoingPortSpacing.Text = PortSpacingCurrent;
            lblDoingAcuteMixZone.Text = AcuteMixZoneCurrent;
            lblDoingChronicMixZone.Text = ChronicMixZoneCurrent;
            lblDoingPortDepth.Text = PortDepthCurrent;
            lblDoingEffluentFlow.Text = EffluentFlowCurrent;
            lblDoingEffluentSalinity.Text = EffluentSalinityCurrent;
            lblDoingEffluentTemperature.Text = EffluentTemperatureCurrent;
            lblDoingEffluentConcentration.Text = EffluentConcentrationCurrent;

            int SelectedRow = int.Parse(comboBoxInputRow.SelectedItem.ToString());

            lblDoingMeasurementDepth.Text = MeasurementDepthCurrent[SelectedRow - 1];
            lblDoingCurrentSpeed.Text = CurrentSpeedCurrent[SelectedRow - 1];
            lblDoingCurrentDirection.Text = CurrentDirectionCurrent[SelectedRow - 1];
            lblDoingAmbientSalinity.Text = AmbientSalinityCurrent[SelectedRow - 1];
            lblDoingAmbientTemperature.Text = AmbientTemperatureCurrent[SelectedRow - 1];
            lblDoingBackgroundConcentration.Text = BackgroundConcentrationCurrent[SelectedRow - 1];
            lblDoingPollutantDecayRate.Text = PollutantDecayRateCurrent[SelectedRow - 1];
            lblDoingFarFieldCurrentSpeed.Text = FarFieldCurrentSpeedCurrent[SelectedRow - 1];
            lblDoingFarFieldCurrentDirection.Text = FarFieldCurrentDirectionCurrent[SelectedRow - 1];
            lblDoingFarFieldDiffusionCoefficient.Text = FarFieldDiffusionCoefficientCurrent[SelectedRow - 1];
            MegaDoEvents();
        }
        private bool CheckDiffuserParameter(string ParameterName, string[] TheValues)
        {
            double StartValue;
            double EndValue;
            double StepValue;
            try
            {
                StartValue = (double)((TheValues[0] == "") ? (double)-999 : double.Parse(TheValues[0]));
                EndValue = (double)((TheValues[1] == "") ? (double)-999 : double.Parse(TheValues[1]));
                StepValue = (double)((TheValues[2] == "") ? (double)-999 : double.Parse(TheValues[2]));
            }
            catch (Exception)
            {
                MessageBox.Show("Error in " + ParameterName + " start, end or step value.\r\nPlease make sure you entered a number.");
                return false;
            }

            if (ParameterName == "Effluent Concentration")
            {
                if (StartValue <= 88)
                {
                    MessageBox.Show(ParameterName + " start value needs to be > 88.");
                    return false;
                }
            }

            if (TheValues[0].Trim() == "")
            {
                MessageBox.Show("Need " + ParameterName + " start value.");
                return false;
            }
            if (TheValues[1].Trim() != "")
            {
                if (StartValue > EndValue)
                {
                    MessageBox.Show(ParameterName + " end value [" + TheValues[1] + "] needs to be > than its start value [" + TheValues[0] + "].");
                    return false;
                }

                if (TheValues[2].Trim() == "")
                {
                    MessageBox.Show(ParameterName + " needs a step value.");
                    return false;
                }

                if (StepValue < 1)
                {
                    MessageBox.Show(ParameterName + " steps needs to be equal or bigger than 1.");
                    return false;
                }
            }
            else
            {
                if (TheValues[2].Trim() != "")
                {
                    MessageBox.Show("No need for " + ParameterName + " step value. Please remove it.");
                    return false;
                }
            }

            return true;
        }
        private bool CheckAmbientParameter(string ParameterName, int Row, string[][] TheValues)
        {
            double StartValue;
            double EndValue;
            double StepValue;
            try
            {
                StartValue = (double)((TheValues[Row - 1][0] == "") ? (double)-999 : double.Parse(TheValues[Row - 1][0]));
                EndValue = (double)((TheValues[Row - 1][1] == "") ? (double)-999 : double.Parse(TheValues[Row - 1][1]));
                StepValue = (double)((TheValues[Row - 1][2] == "") ? (double)-999 : double.Parse(TheValues[Row - 1][2]));
            }
            catch (Exception)
            {
                MessageBox.Show("Error in " + ParameterName + " start, end or step value.\r\nPlease make sure you entered a number.");
                return false;
            }

            if (TheValues[Row - 1][0].Trim() == "")
            {
                if (Row >= 2)
                {
                    if (Row == 2)
                    {
                        if (ParameterName == "Measurement Depth")
                        {
                            MessageBox.Show("Need " + ParameterName + " start value for Row [" + Row + "].");
                            return false;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Need " + ParameterName + " start value for Row [" + Row + "].");
                    return false;
                }
            }
            if (TheValues[Row - 1][1].Trim() != "")
            {
                if (StartValue > EndValue)
                {
                    MessageBox.Show(ParameterName + " end value [" + TheValues[1] + "] for Row [" + Row + "] needs to be > than its Start Value [" + TheValues[0] + "].");
                    return false;
                }

                if (TheValues[Row - 1][2].Trim() == "")
                {
                    MessageBox.Show(ParameterName + " needs a step value for Row [" + Row + "].");
                    return false;
                }

                if (StepValue < 1)
                {
                    MessageBox.Show(ParameterName + " steps needs to be equal or bigger than 1 for Row [" + Row + "].");
                    return false;
                }

            }

            return true;
        }
        private bool CheckIfAllTheInformationIsCorrect()
        {
            if ((int)comboBoxMunicipalities.SelectedValue <= 0)
            {
                MessageBox.Show("Please select a municipality and infrastructure");
                return false;
            }
            string MunicipalityName = ((TVI)comboBoxMunicipalities.SelectedItem).ItemText.Trim();
            if ((int)comboBoxInfrastructures.SelectedValue <= 0)
            {
                MessageBox.Show("Please select an infrastructure");
                return false;
            }
            string InfrastructureName = ((TVI)comboBoxInfrastructures.SelectedItem).ItemText.Trim();
            if ((int)comboBoxSubInfrastructures.SelectedValue <= 0)
            {
                InfrastructureName = InfrastructureName + " - " + ((TVI)comboBoxSubInfrastructures.SelectedItem).ItemText.Trim();
            }


            if (!CheckDiffuserParameter("Port Diameter", PortDiameterValues))
                return false;
            if (!CheckDiffuserParameter("Port Elevation", PortElevationValues))
                return false;
            if (!CheckDiffuserParameter("Vertical Angle", VerticalAngleValues))
                return false;
            if (!CheckDiffuserParameter("Horizontal Angle", HorizontalAngleValues))
                return false;
            if (!CheckDiffuserParameter("Number Of Ports", NumberOfPortsValues))
                return false;
            if (!CheckDiffuserParameter("Port Spacing", PortSpacingValues))
                return false;
            if (!CheckDiffuserParameter("Acute Mix Zone", AcuteMixZoneValues))
                return false;
            if (!CheckDiffuserParameter("Chronic Mix Zone", ChronicMixZoneValues))
                return false;
            if (!CheckDiffuserParameter("Port Depth", PortDepthValues))
                return false;
            if (!CheckDiffuserParameter("Effluent Flow", EffluentFlowValues))
                return false;
            if (!CheckDiffuserParameter("Effluent Salinity", EffluentSalinityValues))
                return false;
            if (!CheckDiffuserParameter("Effluent Temperature", EffluentTemperatureValues))
                return false;
            if (!CheckDiffuserParameter("Effluent Concentration", EffluentConcentrationValues))
                return false;

            for (int Row = 1; Row < 6; Row++)
            {
                if (!CheckAmbientParameter("Measurement Depth", Row, MeasurementDepthValues))
                    return false;
                if (!CheckAmbientParameter("Current Speed", Row, CurrentSpeedValues))
                    return false;
                if (!CheckAmbientParameter("Current Direction", Row, CurrentDirectionValues))
                    return false;
                if (!CheckAmbientParameter("Ambient Salinity", Row, AmbientSalinityValues))
                    return false;
                if (!CheckAmbientParameter("Ambient Temperature", Row, AmbientTemperatureValues))
                    return false;
                if (!CheckAmbientParameter("Background Concentration", Row, BackgroundConcentrationValues))
                    return false;
                if (!CheckAmbientParameter("Pollutant Decay Rate", Row, PollutantDecayRateValues))
                    return false;
                if (!CheckAmbientParameter("Far Field Current Speed", Row, FarFieldCurrentSpeedValues))
                    return false;
                if (!CheckAmbientParameter("Far Field Current Direction", Row, FarFieldCurrentDirectionValues))
                    return false;
                if (!CheckAmbientParameter("Far Field Diffusion Coefficient", Row, FarFieldDiffusionCoefficientValues))
                    return false;
            }

            return true;
        }
        private bool CheckIfShouldRunManyScenarios()
        {
            if (PortDiameterValues[1] != "")
                return true;
            if (PortElevationValues[1] != "")
                return true;
            if (VerticalAngleValues[1] != "")
                return true;
            if (HorizontalAngleValues[1] != "")
                return true;
            if (NumberOfPortsValues[1] != "")
                return true;
            if (PortSpacingValues[1] != "")
                return true;
            if (AcuteMixZoneValues[1] != "")
                return true;
            if (ChronicMixZoneValues[1] != "")
                return true;
            if (PortDepthValues[1] != "")
                return true;
            if (EffluentFlowValues[1] != "")
                return true;
            if (EffluentSalinityValues[1] != "")
                return true;
            if (EffluentTemperatureValues[1] != "")
                return true;
            if (EffluentConcentrationValues[1] != "")
                return true;
            for (int Row = 1; Row < 6; Row++)
            {
                if (MeasurementDepthValues[Row - 1][1] != "")
                    return true;
                if (CurrentSpeedValues[Row - 1][1] != "")
                    return true;
                if (CurrentDirectionValues[Row - 1][1] != "")
                    return true;
                if (AmbientSalinityValues[Row - 1][1] != "")
                    return true;
                if (AmbientTemperatureValues[Row - 1][1] != "")
                    return true;
                if (BackgroundConcentrationValues[Row - 1][1] != "")
                    return true;
                if (PollutantDecayRateValues[Row - 1][1] != "")
                    return true;
                if (FarFieldCurrentSpeedValues[Row - 1][1] != "")
                    return true;
                if (FarFieldCurrentDirectionValues[Row - 1][1] != "")
                    return true;
                if (FarFieldDiffusionCoefficientValues[Row - 1][1] != "")
                    return true;

            }

            return false;
        }
        private void SaveInputDataToXML()
        {
            Dictionary<string, XElement> xeDict = new Dictionary<string, XElement>();
            int ProvinceItemID = 0;
            int MunicipalityItemID = 0;
            int InfrastructureItemID = 0;
            int SubInfrastructureItemID = 0;

            if (comboBoxProvinces.SelectedItem != null)
            {
                ProvinceItemID = (int)comboBoxProvinces.SelectedValue;
            }
            if (comboBoxMunicipalities.SelectedItem != null)
            {
                MunicipalityItemID = (int)comboBoxMunicipalities.SelectedValue;
            }
            if (comboBoxInfrastructures.SelectedItem != null)
            {
                InfrastructureItemID = (int)comboBoxInfrastructures.SelectedValue;
            }
            if (comboBoxSubInfrastructures.SelectedItem != null)
            {
                SubInfrastructureItemID = (int)comboBoxSubInfrastructures.SelectedValue;
            }

            for (int Row = 1; Row < 6; Row++)
            {
                XElement xe = new XElement("Row" + Row.ToString(),
                               new XElement("MeasurementDepth",
                               new XElement("Start", MeasurementDepthValues[Row - 1][0]),
                               new XElement("End", MeasurementDepthValues[Row - 1][1]),
                               new XElement("Steps", MeasurementDepthValues[Row - 1][2])),
                               new XElement("CurrentSpeed",
                               new XElement("Start", CurrentSpeedValues[Row - 1][0]),
                               new XElement("End", CurrentSpeedValues[Row - 1][1]),
                               new XElement("Steps", CurrentSpeedValues[Row - 1][2])),
                               new XElement("CurrentDirection",
                               new XElement("Start", CurrentDirectionValues[Row - 1][0]),
                               new XElement("End", CurrentDirectionValues[Row - 1][1]),
                               new XElement("Steps", CurrentDirectionValues[Row - 1][2])),
                               new XElement("AmbientSalinity",
                               new XElement("Start", AmbientSalinityValues[Row - 1][0]),
                               new XElement("End", AmbientSalinityValues[Row - 1][1]),
                               new XElement("Steps", AmbientSalinityValues[Row - 1][2])),
                               new XElement("AmbientTemperature",
                               new XElement("Start", AmbientTemperatureValues[Row - 1][0]),
                               new XElement("End", AmbientTemperatureValues[Row - 1][1]),
                               new XElement("Steps", AmbientTemperatureValues[Row - 1][2])),
                               new XElement("BackgroundConcentration",
                               new XElement("Start", BackgroundConcentrationValues[Row - 1][0]),
                               new XElement("End", BackgroundConcentrationValues[Row - 1][1]),
                               new XElement("Steps", BackgroundConcentrationValues[Row - 1][2])),
                               new XElement("PollutantDecayRate",
                               new XElement("Start", PollutantDecayRateValues[Row - 1][0]),
                               new XElement("End", PollutantDecayRateValues[Row - 1][1]),
                               new XElement("Steps", PollutantDecayRateValues[Row - 1][2])),
                               new XElement("FarFieldCurrentSpeed",
                               new XElement("Start", FarFieldCurrentSpeedValues[Row - 1][0]),
                               new XElement("End", FarFieldCurrentSpeedValues[Row - 1][1]),
                               new XElement("Steps", FarFieldCurrentSpeedValues[Row - 1][2])),
                               new XElement("FarFieldCurrentDirection",
                               new XElement("Start", FarFieldCurrentDirectionValues[Row - 1][0]),
                               new XElement("End", FarFieldCurrentDirectionValues[Row - 1][1]),
                               new XElement("Steps", FarFieldCurrentDirectionValues[Row - 1][2])),
                               new XElement("FarFieldDiffusionCoefficient",
                               new XElement("Start", FarFieldDiffusionCoefficientValues[Row - 1][0]),
                               new XElement("End", FarFieldDiffusionCoefficientValues[Row - 1][1]),
                               new XElement("Steps", FarFieldDiffusionCoefficientValues[Row - 1][2])));

                xeDict.Add(Row.ToString(), xe);

            }

            XDocument xd = new XDocument(
                           new XElement("Root",
                           new XElement("Province", ProvinceItemID),
                           new XElement("Municipality", MunicipalityItemID),
                           new XElement("Infrastructure", InfrastructureItemID),
                           new XElement("SubInfrastructure", SubInfrastructureItemID),
                           new XElement("DiffuserData",
                           new XElement("PortDiameter",
                           new XElement("Start", PortDiameterValues[0]),
                           new XElement("End", PortDiameterValues[1]),
                           new XElement("Steps", PortDiameterValues[2])),
                           new XElement("PortElevation",
                           new XElement("Start", PortElevationValues[0]),
                           new XElement("End", PortElevationValues[1]),
                           new XElement("Steps", PortElevationValues[2])),
                           new XElement("VerticalAngle",
                           new XElement("Start", VerticalAngleValues[0]),
                           new XElement("End", VerticalAngleValues[1]),
                           new XElement("Steps", VerticalAngleValues[2])),
                           new XElement("HorizontalAngle",
                           new XElement("Start", HorizontalAngleValues[0]),
                           new XElement("End", HorizontalAngleValues[1]),
                           new XElement("Steps", HorizontalAngleValues[2])),
                           new XElement("NumberOfPorts",
                           new XElement("Start", NumberOfPortsValues[0]),
                           new XElement("End", NumberOfPortsValues[1]),
                           new XElement("Steps", NumberOfPortsValues[2])),
                           new XElement("PortSpacing",
                           new XElement("Start", PortSpacingValues[0]),
                           new XElement("End", PortSpacingValues[1]),
                           new XElement("Steps", PortSpacingValues[2])),
                           new XElement("AcuteMixZone",
                           new XElement("Start", AcuteMixZoneValues[0]),
                           new XElement("End", AcuteMixZoneValues[1]),
                           new XElement("Steps", AcuteMixZoneValues[2])),
                           new XElement("ChronicMixZone",
                           new XElement("Start", ChronicMixZoneValues[0]),
                           new XElement("End", ChronicMixZoneValues[1]),
                           new XElement("Steps", ChronicMixZoneValues[2])),
                           new XElement("PortDepth",
                           new XElement("Start", PortDepthValues[0]),
                           new XElement("End", PortDepthValues[1]),
                           new XElement("Steps", PortDepthValues[2])),
                           new XElement("EffluentFlow",
                           new XElement("Start", EffluentFlowValues[0]),
                           new XElement("End", EffluentFlowValues[1]),
                           new XElement("Steps", EffluentFlowValues[2])),
                           new XElement("EffluentSalinity",
                           new XElement("Start", EffluentSalinityValues[0]),
                           new XElement("End", EffluentSalinityValues[1]),
                           new XElement("Steps", EffluentSalinityValues[2])),
                           new XElement("EffluentTemperature",
                           new XElement("Start", EffluentTemperatureValues[0]),
                           new XElement("End", EffluentTemperatureValues[1]),
                           new XElement("Steps", EffluentTemperatureValues[2])),
                           new XElement("EffluentConcentration",
                           new XElement("Start", EffluentConcentrationValues[0]),
                           new XElement("End", EffluentConcentrationValues[1]),
                           new XElement("Steps", EffluentConcentrationValues[2]))),
                           new XElement("AmbientData",
                           new XElement(xeDict["1"]),
                           new XElement(xeDict["2"]),
                           new XElement(xeDict["3"]),
                           new XElement(xeDict["4"]),
                           new XElement(xeDict["5"])
                           )));

            xd.Save(XMLInputFileName);

        }
        private void ReadInputDataFromXML()
        {

            int ProvinceItemID = 0;
            int MunicipalityItemID = 0;
            int InfrastructureItemID = 0;
            int SubInfrastructureItemID = 0;

            try
            {
                if (File.Exists(XMLInputFileName))
                {
                    XDocument xd = XDocument.Load(XMLInputFileName);
                    ProvinceItemID = int.Parse(xd.Root.Element("Province").Value);
                    MunicipalityItemID = int.Parse(xd.Root.Element("Municipality").Value);
                    InfrastructureItemID = int.Parse(xd.Root.Element("Infrastructure").Value);
                    SubInfrastructureItemID = int.Parse(xd.Root.Element("SubInfrastructure").Value);
                    PortDiameterValues[0] = xd.Root.Element("DiffuserData").Element("PortDiameter").Element("Start").Value;
                    PortDiameterValues[1] = xd.Root.Element("DiffuserData").Element("PortDiameter").Element("End").Value;
                    PortDiameterValues[2] = xd.Root.Element("DiffuserData").Element("PortDiameter").Element("Steps").Value;
                    PortElevationValues[0] = xd.Root.Element("DiffuserData").Element("PortElevation").Element("Start").Value;
                    PortElevationValues[1] = xd.Root.Element("DiffuserData").Element("PortElevation").Element("End").Value;
                    PortElevationValues[2] = xd.Root.Element("DiffuserData").Element("PortElevation").Element("Steps").Value;
                    VerticalAngleValues[0] = xd.Root.Element("DiffuserData").Element("VerticalAngle").Element("Start").Value;
                    VerticalAngleValues[1] = xd.Root.Element("DiffuserData").Element("VerticalAngle").Element("End").Value;
                    VerticalAngleValues[2] = xd.Root.Element("DiffuserData").Element("VerticalAngle").Element("Steps").Value;
                    HorizontalAngleValues[0] = xd.Root.Element("DiffuserData").Element("HorizontalAngle").Element("Start").Value;
                    HorizontalAngleValues[1] = xd.Root.Element("DiffuserData").Element("HorizontalAngle").Element("End").Value;
                    HorizontalAngleValues[2] = xd.Root.Element("DiffuserData").Element("HorizontalAngle").Element("Steps").Value;
                    NumberOfPortsValues[0] = xd.Root.Element("DiffuserData").Element("NumberOfPorts").Element("Start").Value;
                    NumberOfPortsValues[1] = xd.Root.Element("DiffuserData").Element("NumberOfPorts").Element("End").Value;
                    NumberOfPortsValues[2] = xd.Root.Element("DiffuserData").Element("NumberOfPorts").Element("Steps").Value;
                    PortSpacingValues[0] = xd.Root.Element("DiffuserData").Element("PortSpacing").Element("Start").Value;
                    PortSpacingValues[1] = xd.Root.Element("DiffuserData").Element("PortSpacing").Element("End").Value;
                    PortSpacingValues[2] = xd.Root.Element("DiffuserData").Element("PortSpacing").Element("Steps").Value;
                    AcuteMixZoneValues[0] = xd.Root.Element("DiffuserData").Element("AcuteMixZone").Element("Start").Value;
                    AcuteMixZoneValues[1] = xd.Root.Element("DiffuserData").Element("AcuteMixZone").Element("End").Value;
                    AcuteMixZoneValues[2] = xd.Root.Element("DiffuserData").Element("AcuteMixZone").Element("Steps").Value;
                    ChronicMixZoneValues[0] = xd.Root.Element("DiffuserData").Element("ChronicMixZone").Element("Start").Value;
                    ChronicMixZoneValues[1] = xd.Root.Element("DiffuserData").Element("ChronicMixZone").Element("End").Value;
                    ChronicMixZoneValues[2] = xd.Root.Element("DiffuserData").Element("ChronicMixZone").Element("Steps").Value;
                    PortDepthValues[0] = xd.Root.Element("DiffuserData").Element("PortDepth").Element("Start").Value;
                    PortDepthValues[1] = xd.Root.Element("DiffuserData").Element("PortDepth").Element("End").Value;
                    PortDepthValues[2] = xd.Root.Element("DiffuserData").Element("PortDepth").Element("Steps").Value;
                    EffluentFlowValues[0] = xd.Root.Element("DiffuserData").Element("EffluentFlow").Element("Start").Value;
                    EffluentFlowValues[1] = xd.Root.Element("DiffuserData").Element("EffluentFlow").Element("End").Value;
                    EffluentFlowValues[2] = xd.Root.Element("DiffuserData").Element("EffluentFlow").Element("Steps").Value;
                    EffluentSalinityValues[0] = xd.Root.Element("DiffuserData").Element("EffluentSalinity").Element("Start").Value;
                    EffluentSalinityValues[1] = xd.Root.Element("DiffuserData").Element("EffluentSalinity").Element("End").Value;
                    EffluentSalinityValues[2] = xd.Root.Element("DiffuserData").Element("EffluentSalinity").Element("Steps").Value;
                    EffluentTemperatureValues[0] = xd.Root.Element("DiffuserData").Element("EffluentTemperature").Element("Start").Value;
                    EffluentTemperatureValues[1] = xd.Root.Element("DiffuserData").Element("EffluentTemperature").Element("End").Value;
                    EffluentTemperatureValues[2] = xd.Root.Element("DiffuserData").Element("EffluentTemperature").Element("Steps").Value;
                    EffluentConcentrationValues[0] = xd.Root.Element("DiffuserData").Element("EffluentConcentration").Element("Start").Value;
                    EffluentConcentrationValues[1] = xd.Root.Element("DiffuserData").Element("EffluentConcentration").Element("End").Value;
                    EffluentConcentrationValues[2] = xd.Root.Element("DiffuserData").Element("EffluentConcentration").Element("Steps").Value;
                    for (int Row = 1; Row < 6; Row++)
                    {
                        MeasurementDepthValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("MeasurementDepth").Element("Start").Value;
                        MeasurementDepthValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("MeasurementDepth").Element("End").Value;
                        MeasurementDepthValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("MeasurementDepth").Element("Steps").Value;
                        CurrentSpeedValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentSpeed").Element("Start").Value;
                        CurrentSpeedValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentSpeed").Element("End").Value;
                        CurrentSpeedValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentSpeed").Element("Steps").Value;
                        CurrentDirectionValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentDirection").Element("Start").Value;
                        CurrentDirectionValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentDirection").Element("End").Value;
                        CurrentDirectionValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("CurrentDirection").Element("Steps").Value;
                        AmbientSalinityValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientSalinity").Element("Start").Value;
                        AmbientSalinityValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientSalinity").Element("End").Value;
                        AmbientSalinityValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientSalinity").Element("Steps").Value;
                        AmbientTemperatureValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientTemperature").Element("Start").Value;
                        AmbientTemperatureValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientTemperature").Element("End").Value;
                        AmbientTemperatureValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("AmbientTemperature").Element("Steps").Value;
                        BackgroundConcentrationValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("BackgroundConcentration").Element("Start").Value;
                        BackgroundConcentrationValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("BackgroundConcentration").Element("End").Value;
                        BackgroundConcentrationValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("BackgroundConcentration").Element("Steps").Value;
                        PollutantDecayRateValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("PollutantDecayRate").Element("Start").Value;
                        PollutantDecayRateValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("PollutantDecayRate").Element("End").Value;
                        PollutantDecayRateValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("PollutantDecayRate").Element("Steps").Value;
                        FarFieldCurrentSpeedValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentSpeed").Element("Start").Value;
                        FarFieldCurrentSpeedValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentSpeed").Element("End").Value;
                        FarFieldCurrentSpeedValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentSpeed").Element("Steps").Value;
                        FarFieldCurrentDirectionValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentDirection").Element("Start").Value;
                        FarFieldCurrentDirectionValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentDirection").Element("End").Value;
                        FarFieldCurrentDirectionValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldCurrentDirection").Element("Steps").Value;
                        FarFieldDiffusionCoefficientValues[Row - 1][0] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldDiffusionCoefficient").Element("Start").Value;
                        FarFieldDiffusionCoefficientValues[Row - 1][1] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldDiffusionCoefficient").Element("End").Value;
                        FarFieldDiffusionCoefficientValues[Row - 1][2] = xd.Root.Element("AmbientData").Element("Row" + Row.ToString()).Element("FarFieldDiffusionCoefficient").Element("Steps").Value;
                    }
                }
            }
            catch (Exception)
            {
            }
            ReadDiffuserValues();
            for (int row = 5; row > 0; row--)
            {
                ReadAmbientValues(row);
            }
            BeforeReadInputDataFromXML = false;
            RecalculateTotalScenario();
            if (ProvinceItemID > 0)
            {
                comboBoxProvinces.SelectedValue = ProvinceItemID;
            }
            if (MunicipalityItemID > 0)
            {
                comboBoxMunicipalities.SelectedValue = MunicipalityItemID;
            }
            if (InfrastructureItemID > 0)
            {
                comboBoxInfrastructures.SelectedValue = InfrastructureItemID;
            }
            if (SubInfrastructureItemID > 0)
            {
                comboBoxSubInfrastructures.SelectedValue = SubInfrastructureItemID;
            }
        }
        private void RecalculateTotalScenario()
        {
            if (BeforeReadInputDataFromXML)
                return;

            if (IsLoaded)
            {
                SetDiffuserValues();
                SetAmbientValues(int.Parse(comboBoxInputRow.SelectedItem.ToString()));
            }

            double totalRun = 1;
            totalRun = totalRun * ((PortDiameterValues[2] == "") ? (double)1 : double.Parse(PortDiameterValues[2]) + 1);
            totalRun = totalRun * ((PortElevationValues[2] == "") ? (double)1 : double.Parse(PortElevationValues[2]) + 1);
            totalRun = totalRun * ((VerticalAngleValues[2] == "") ? (double)1 : double.Parse(VerticalAngleValues[2]) + 1);
            totalRun = totalRun * ((HorizontalAngleValues[2] == "") ? (double)1 : double.Parse(HorizontalAngleValues[2]) + 1);
            totalRun = totalRun * ((NumberOfPortsValues[2] == "") ? (double)1 : double.Parse(NumberOfPortsValues[2]) + 1);
            totalRun = totalRun * ((PortSpacingValues[2] == "") ? (double)1 : double.Parse(PortSpacingValues[2]) + 1);
            totalRun = totalRun * ((AcuteMixZoneValues[2] == "") ? (double)1 : double.Parse(AcuteMixZoneValues[2]) + 1);
            totalRun = totalRun * ((ChronicMixZoneValues[2] == "") ? (double)1 : double.Parse(ChronicMixZoneValues[2]) + 1);
            totalRun = totalRun * ((PortDepthValues[2] == "") ? (double)1 : double.Parse(PortDepthValues[2]) + 1);
            totalRun = totalRun * ((EffluentFlowValues[2] == "") ? (double)1 : double.Parse(EffluentFlowValues[2]) + 1);
            totalRun = totalRun * ((EffluentSalinityValues[2] == "") ? (double)1 : double.Parse(EffluentSalinityValues[2]) + 1);
            totalRun = totalRun * ((EffluentTemperatureValues[2] == "") ? (double)1 : double.Parse(EffluentTemperatureValues[2]) + 1);
            totalRun = totalRun * ((EffluentConcentrationValues[2] == "") ? (double)1 : double.Parse(EffluentConcentrationValues[2]) + 1);
            for (int Row = 1; Row < 6; Row++)
            {
                totalRun = totalRun * ((MeasurementDepthValues[Row - 1][2] == "") ? (double)1 : double.Parse(MeasurementDepthValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((CurrentSpeedValues[Row - 1][2] == "") ? (double)1 : double.Parse(CurrentSpeedValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((CurrentDirectionValues[Row - 1][2] == "") ? (double)1 : double.Parse(CurrentDirectionValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((AmbientSalinityValues[Row - 1][2] == "") ? (double)1 : double.Parse(AmbientSalinityValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((AmbientTemperatureValues[Row - 1][2] == "") ? (double)1 : double.Parse(AmbientTemperatureValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((BackgroundConcentrationValues[Row - 1][2] == "") ? (double)1 : double.Parse(BackgroundConcentrationValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((PollutantDecayRateValues[Row - 1][2] == "") ? (double)1 : double.Parse(PollutantDecayRateValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((FarFieldCurrentSpeedValues[Row - 1][2] == "") ? (double)1 : double.Parse(FarFieldCurrentSpeedValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((FarFieldCurrentDirectionValues[Row - 1][2] == "") ? (double)1 : double.Parse(FarFieldCurrentDirectionValues[Row - 1][2]) + 1);
                totalRun = totalRun * ((FarFieldDiffusionCoefficientValues[Row - 1][2] == "") ? (double)1 : double.Parse(FarFieldDiffusionCoefficientValues[Row - 1][2]) + 1);
            }
            lblTotalScenariosValue.Text = totalRun.ToString();
        }
        private void TreeViewRefresh()
        {
            string Lang = "en";
            TreeNode RootNode = new TreeNode();

            RootNode.Text = "Root";
            RootNode.Tag = RootTVI;

            treeViewItems.Nodes.Clear();
            treeViewItems.Nodes.Add(RootNode);

            LoadChildren(RootNode, ItemType.Province, Lang);
        }
        private void LoadChildren(TreeNode ParentNode, ItemType type, string lang)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            string TypeString = type.ToString();
            int ParentItemID = ((TVI)ParentNode.Tag).ItemID;

            // getting the unique Provinces from the scenario table
            IQueryable<TVI> tvis = (from c in vpse.CSSPItems
                                    from cl in vpse.CSSPItemLanguages
                                    from ct in vpse.CSSPTypeItems
                                    where c.CSSPItemID == cl.CSSPItemID
                                    && cl.Language == lang
                                    && c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                                    && ct.CSSPTypeText == TypeString
                                    && c.CSSPParentItem.CSSPItemID == ParentItemID
                                    orderby cl.CSSPItemText
                                    select new TVI
                                    {
                                        ItemID = c.CSSPItemID,
                                        ItemText = cl.CSSPItemText
                                    }).AsQueryable<TVI>();

            if (tvis != null)
            {
                foreach (TVI tvi in tvis)
                {
                    TreeNode TempNode = new TreeNode();
                    TempNode.Text = tvi.ItemText;
                    TempNode.Tag = tvi;

                    ParentNode.Nodes.Add(TempNode);

                }
            }
        }
        private void FilldataGridViewScenarios(TVI CurrentTVI)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            string ItemTypeSelected = (from c in vpse.CSSPItems
                                       from ct in vpse.CSSPTypeItems
                                       where c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                                       && c.CSSPItemID == CurrentTVI.ItemID
                                       select ct.CSSPTypeText).FirstOrDefault();

            if (ItemTypeSelected == ItemType.WWTP.ToString())
            {
                var AllScenarios = from s in vpse.Scenarios
                                   where s.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                   select s;

                dataGridViewScenarios.DataSource = null;
                dataGridViewScenarios.DataSource = AllScenarios;
            }
            else if (ItemTypeSelected == ItemType.LiftStation.ToString())
            {
                var AllScenarios = from s in vpse.Scenarios
                                   where s.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                   select s;

                dataGridViewScenarios.DataSource = null;
                dataGridViewScenarios.DataSource = AllScenarios;
            }
        }
        private void FilldataGridViewAmbient(int ScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            var SelectedAmbient = from a in vpse.Ambients
                                  where a.ScenarioID == ScenarioID
                                  orderby a.Row ascending
                                  select a;

            dataGridViewAmbient.DataSource = SelectedAmbient;
        }
        private void FilldataGridViewValuedResults(int ScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (checkBoxViewCormix.Checked == true)
            {
                var SelectedValuedResults = from vr in vpse.ValuedCormixResults
                                            where vr.ScenarioID == ScenarioID
                                            orderby vr.ArrayNum ascending
                                            select vr;

                dataGridViewValuedResults.DataSource = SelectedValuedResults;
            }
            else
            {
                var SelectedValuedResults = from vr in vpse.ValuedResults
                                            where vr.ScenarioID == ScenarioID
                                            orderby vr.ArrayNum ascending
                                            select vr;

                dataGridViewValuedResults.DataSource = SelectedValuedResults;
            }
        }
        private void FilldataGridViewSelectedResults(int ScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (checkBoxViewCormix.Checked == true)
            {
                var SelectedSelectedResults = from sr in vpse.SelectedCormixResults
                                              where sr.ScenarioID == ScenarioID
                                              select sr;

                dataGridViewSelectedResults.DataSource = SelectedSelectedResults;
            }
            else
            {
                var SelectedSelectedResults = from sr in vpse.SelectedResults
                                              where sr.ScenarioID == ScenarioID
                                              select sr;

                dataGridViewSelectedResults.DataSource = SelectedSelectedResults;
            }
        }
        private void RefreshGridViews(TreeNode treeNode, TVI CurrentTVI, CSSPTypeItem CurrentType)
        {
            if (CurrentType.CSSPTypeText == ItemType.WWTP.ToString() || CurrentType.CSSPTypeText == ItemType.LiftStation.ToString())
            {
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;
                panelMike.Visible = false;
                panelViewSelection.Visible = true;
                panelVPScenarioResults.Visible = true;

                FilldataGridViewScenarios(CurrentTVI);
                FilldataGridViewAmbient(0);
                FilldataGridViewValuedResults(0);
                FilldataGridViewSelectedResults(0);

            }
            else if (CurrentType.CSSPTypeText == ItemType.Municipality.ToString())
            {
                radioButtonVPScenarioResults.Checked = true;
                panelVPScenarioResults.Visible = false;
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;
                panelViewSelection.Visible = false;
                panelMike.Visible = true;
                string TheName = WindowsIdentity.GetCurrent().Name;
                if (TheName.ToLower() == @"charles-pc\charles" 
                    || TheName.ToLower() == @"ec_atlantic\stoboj" 
                    || TheName.ToLower() == @"ec_atlantic\leblancc" 
                    || TheName.ToLower() == @"ec_atlantic\bastarached"
                    || TheName.ToLower() == @"wmon01dtchlebl\admin-leblancc")
                {
                    butRemoveFileFromDB.Enabled = true;
                    butAddm21fmFileAndAssociatedFilesInDB.Enabled = true;
                }
                else
                {
                    butRemoveFileFromDB.Enabled = false;
                    butAddm21fmFileAndAssociatedFilesInDB.Enabled = false;
                }
                FillDataGridViewMikeScenairosInDB(0);
            }
            else
            {
                panelVPScenarioResults.Visible = false;
                panelMike.Visible = false;
                panelViewSelection.Visible = false;
                panelInfrastructureOrMunicipalityNotSelected.Visible = true;
            }
        }

        //private void FilldataGridViewFileAlreadyInDB(TVI CurrentTVI)
        //{
        //    CSSPAppDBEntities vpse = new CSSPAppDBEntities();

        //    string ItemTypeSelected = (from c in vpse.CSSPItems
        //                               from ct in vpse.CSSPTypeItems
        //                               where c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
        //                               && c.CSSPItemID == CurrentTVI.ItemID
        //                               select ct.CSSPTypeText).FirstOrDefault();

        //    if (ItemTypeSelected == ItemType.Municipality.ToString())
        //    {
        //        List<CSSPFileNoContent> AllCSSPFiles = (from cf in vpse.CSSPFileNoContents
        //                                                from cif in vpse.CSSPItemFiles
        //                                                where cif.CSSPItem.CSSPItemID == CurrentTVI.ItemID
        //                                                && cf.CSSPFileID == cif.CSSPFile.CSSPFileID
        //                                                select cf).ToList<CSSPFileNoContent>();

        //        dataGridViewFileAlreadyInDB.DataSource = null;
        //        dataGridViewFileAlreadyInDB.DataSource = AllCSSPFiles;
        //    }
        //}
        private void TakeSelectedValueAndFillAutoRun(int ScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            var TheScenario = (from s in vpse.Scenarios
                               where s.ScenarioID == ScenarioID
                               select s).FirstOrDefault();

            if (TheScenario != null)
            {
                PortDiameterValues[0] = TheScenario.PortDiameter.ToString();
                PortDiameterValues[1] = "";
                PortDiameterValues[2] = "";
                PortElevationValues[0] = TheScenario.PortElevation.ToString();
                PortElevationValues[1] = "";
                PortElevationValues[2] = "";
                VerticalAngleValues[0] = TheScenario.VerticalAngle.ToString();
                VerticalAngleValues[1] = "";
                VerticalAngleValues[2] = "";
                HorizontalAngleValues[0] = TheScenario.HorizontalAngle.ToString();
                HorizontalAngleValues[1] = "";
                HorizontalAngleValues[2] = "";
                NumberOfPortsValues[0] = TheScenario.NumberOfPorts.ToString();
                NumberOfPortsValues[1] = "";
                NumberOfPortsValues[2] = "";
                PortSpacingValues[0] = TheScenario.PortSpacing.ToString();
                PortSpacingValues[1] = "";
                PortSpacingValues[2] = "";
                AcuteMixZoneValues[0] = TheScenario.AcuteMixZone.ToString();
                AcuteMixZoneValues[1] = "";
                AcuteMixZoneValues[2] = "";
                ChronicMixZoneValues[0] = TheScenario.ChronicMixZone.ToString();
                ChronicMixZoneValues[1] = "";
                ChronicMixZoneValues[2] = "";
                PortDepthValues[0] = TheScenario.PortDepth.ToString();
                PortDepthValues[1] = "";
                PortDepthValues[2] = "";
                EffluentFlowValues[0] = TheScenario.EffluentFlow.ToString();
                EffluentFlowValues[1] = "";
                EffluentFlowValues[2] = "";
                EffluentSalinityValues[0] = TheScenario.EffluentSalinity.ToString();
                EffluentSalinityValues[1] = "";
                EffluentSalinityValues[2] = "";
                EffluentTemperatureValues[0] = TheScenario.EffluentTemperature.ToString();
                EffluentTemperatureValues[1] = "";
                EffluentTemperatureValues[2] = "";
                EffluentConcentrationValues[0] = TheScenario.EffluentConcentration.ToString();
                EffluentConcentrationValues[1] = "";
                EffluentConcentrationValues[2] = "";
                richTextBoxRawResults.Text = TheScenario.RawResults;
                richTextBoxParsedResults.Text = TheScenario.ParsedResults;
            }

            var TheAmbients = from a in vpse.Ambients
                              where a.ScenarioID == ScenarioID
                              select a;

            if (TheAmbients != null)
            {
                foreach (Ambient a in TheAmbients)
                {
                    MeasurementDepthValues[a.Row - 1][0] = a.MeasurementDepth == -999 ? "" : a.MeasurementDepth.ToString();
                    MeasurementDepthValues[a.Row - 1][1] = "";
                    MeasurementDepthValues[a.Row - 1][2] = "";
                    CurrentSpeedValues[a.Row - 1][0] = a.CurrentSpeed == -999 ? "" : a.CurrentSpeed.ToString();
                    CurrentSpeedValues[a.Row - 1][1] = "";
                    CurrentSpeedValues[a.Row - 1][2] = "";
                    CurrentDirectionValues[a.Row - 1][0] = a.CurrentDirection == -999 ? "" : a.CurrentDirection.ToString();
                    CurrentDirectionValues[a.Row - 1][1] = "";
                    CurrentDirectionValues[a.Row - 1][2] = "";
                    AmbientSalinityValues[a.Row - 1][0] = a.AmbientSalinity == -999 ? "" : a.AmbientSalinity.ToString();
                    AmbientSalinityValues[a.Row - 1][1] = "";
                    AmbientSalinityValues[a.Row - 1][2] = "";
                    AmbientTemperatureValues[a.Row - 1][0] = a.AmbientTemperature == -999 ? "" : a.AmbientTemperature.ToString();
                    AmbientTemperatureValues[a.Row - 1][1] = "";
                    AmbientTemperatureValues[a.Row - 1][2] = "";
                    BackgroundConcentrationValues[a.Row - 1][0] = a.BackgroundConcentration == -999 ? "" : a.BackgroundConcentration.ToString();
                    BackgroundConcentrationValues[a.Row - 1][1] = "";
                    BackgroundConcentrationValues[a.Row - 1][2] = "";
                    PollutantDecayRateValues[a.Row - 1][0] = a.PollutantDecayRate == -999 ? "" : a.PollutantDecayRate.ToString();
                    PollutantDecayRateValues[a.Row - 1][1] = "";
                    PollutantDecayRateValues[a.Row - 1][2] = "";
                    FarFieldCurrentSpeedValues[a.Row - 1][0] = a.FarFieldCurrentSpeed == -999 ? "" : a.FarFieldCurrentSpeed.ToString();
                    FarFieldCurrentSpeedValues[a.Row - 1][1] = "";
                    FarFieldCurrentSpeedValues[a.Row - 1][2] = "";
                    FarFieldCurrentDirectionValues[a.Row - 1][0] = a.FarFieldCurrentDirection == -999 ? "" : a.FarFieldCurrentDirection.ToString();
                    FarFieldCurrentDirectionValues[a.Row - 1][1] = "";
                    FarFieldCurrentDirectionValues[a.Row - 1][2] = "";
                    FarFieldDiffusionCoefficientValues[a.Row - 1][0] = a.FarFieldDiffusionCoefficient == -999 ? "" : a.FarFieldDiffusionCoefficient.ToString();
                    FarFieldDiffusionCoefficientValues[a.Row - 1][1] = "";
                    FarFieldDiffusionCoefficientValues[a.Row - 1][2] = "";
                }
            }
            ReadDiffuserValues();
            ReadAmbientValues(int.Parse(comboBoxInputRow.SelectedItem.ToString()));

            // setting the CSSPItems combo boxes
            CSSPItem CurrentCSSP = (from s in vpse.Scenarios
                                    where s.ScenarioID == TheScenario.ScenarioID
                                    select s.CSSPItem).FirstOrDefault();

            if (CurrentCSSP != null)
            {
                var CSSPItemType = (from c in vpse.CSSPItems
                                    where c.CSSPItemID == CurrentCSSP.CSSPItemID
                                    select c.CSSPTypeItem.CSSPTypeText).FirstOrDefault();

                if (CSSPItemType != null)
                {
                    if (CSSPItemType == ItemType.WWTP.ToString())
                    {
                        var CSSPItemsID = (from c in vpse.CSSPItems
                                           where c.CSSPItemID == TheScenario.CSSPItem.CSSPItemID
                                           let InfrastructureID = c.CSSPItemID
                                           let MunID = c.CSSPParentItem.CSSPItemID
                                           let ProvID = c.CSSPParentItem.CSSPParentItem.CSSPItemID
                                           select new { InfrastructureID = InfrastructureID, MunID = MunID, ProvID = ProvID }).FirstOrDefault();

                        if (CSSPItemsID != null)
                        {
                            comboBoxProvinces.SelectedValue = CSSPItemsID.ProvID;
                            comboBoxMunicipalities.SelectedValue = CSSPItemsID.MunID;
                            comboBoxInfrastructures.SelectedValue = CSSPItemsID.InfrastructureID;
                        }
                    }
                    else
                    {
                        var CSSPItemsID = (from c in vpse.CSSPItems
                                           where c.CSSPItemID == TheScenario.CSSPItem.CSSPItemID
                                           let SubInfrastructureID = c.CSSPItemID
                                           let InfrastructureID = c.CSSPParentItem.CSSPItemID
                                           let MunID = c.CSSPParentItem.CSSPParentItem.CSSPItemID
                                           let ProvID = c.CSSPParentItem.CSSPParentItem.CSSPParentItem.CSSPItemID
                                           select new { SubInfrastructureID = SubInfrastructureID, InfrastructureID = InfrastructureID, MunID = MunID, ProvID = ProvID }).FirstOrDefault();

                        if (CSSPItemsID != null)
                        {
                            comboBoxProvinces.SelectedValue = CSSPItemsID.ProvID;
                            comboBoxMunicipalities.SelectedValue = CSSPItemsID.MunID;
                            comboBoxInfrastructures.SelectedValue = CSSPItemsID.InfrastructureID;
                            comboBoxSubInfrastructures.SelectedValue = CSSPItemsID.SubInfrastructureID;
                        }
                    }
                }
            }


            tabControlAutoRunVP.SelectedIndex = 0;
        }
        private void SetUseAsBestEstimateInDB(int TheScenarioID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            // Getting the CSSPItem that is the parent item of TheScenarioID
            CSSPItem CurrentCSSP = (from s in vpse.Scenarios
                                    where s.ScenarioID == TheScenarioID
                                    select s.CSSPItem).FirstOrDefault();

            if (CurrentCSSP != null)
            {
                List<Scenario> Scenarios = (from s in vpse.Scenarios
                                            where s.CSSPItem.CSSPItemID == CurrentCSSP.CSSPItemID
                                            select s).ToList<Scenario>();

                if (Scenarios == null)
                {
                    MessageBox.Show("ERROR - could not find Scenarios where CSSPItemID = [" + CurrentCSSP.CSSPItemID + "]");
                    return;
                }
                else
                {
                    foreach (Scenario s in Scenarios)
                    {
                        if (s.ScenarioID == TheScenarioID)
                        {
                            s.UseAsBestEstimate = true;
                        }
                        else
                        {
                            s.UseAsBestEstimate = false;
                        }
                    }

                    try
                    {
                        vpse.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException == null)
                        {
                            MessageBox.Show("ERROR - " + ex.Message + "\r\nInnerError - " + ex.InnerException.Message);
                            return;
                        }
                        else
                        {
                            MessageBox.Show("ERROR - " + ex.Message);
                            return;
                        }
                    }
                }
            }

            FillAfterSelect(treeViewItems.SelectedNode);
        }
        private void FillDesktopWindowsChildrenList(bool ShowResults)
        {
            IntPtr hWndDesktop = af.APIGetDesktopWindow();
            DesktopChildrenWindowsList.Clear();
            DesktopChildrenWindowsList = af.GetChildrenWindowsHandleAndTitle(hWndDesktop);

            if (ShowResults)
            {
                richTextBoxStatus.Text = "";
                richTextBoxStatus.Text = "DesktopWindow = [" + hWndDesktop + "]\r\n";
                richTextBoxStatus.AppendText("Handle count = [" + DesktopChildrenWindowsList.Count() + "\r\n");

                foreach (WndHandleAndTitle t in DesktopChildrenWindowsList)
                {
                    richTextBoxStatus.AppendText("Handle = [" + t.Handle.ToString("X") + "] Window Title = [" + t.Title + "]\r\n");
                }
            }
        }
        private void butHelp_Click(object sender, EventArgs e)
        {
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = @"\\ECMONTREAL01.quebec.int.ec.gc.ca\ServicesCSL\National_MWQM\Tools\AutoRunVPSetup\HTML\AutoRunVPSetup.html";
            pInfo.WindowStyle = ProcessWindowStyle.Normal;
            pInfo.UseShellExecute = true;
            processPlumes.StartInfo = pInfo;
            processPlumes.Start();
        }
        private void FillComboBox(ComboBox comboBox, string FirstItem, TVI ParentTVI, ItemType it, string Lang)
        {

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            List<TVI> tviList = new List<TVI>();
            TVI TempFirstItem = new TVI()
            {
                ItemID = 0,
                ItemText = FirstItem
            };

            tviList.Add(TempFirstItem);

            string TypeItemText = it.ToString();

            IQueryable<TVI> tvis = (from c in vpse.CSSPItems
                                    from cl in vpse.CSSPItemLanguages
                                    from ct in vpse.CSSPTypeItems
                                    where c.CSSPItemID == cl.CSSPItemID
                                    && ct.CSSPTypeText == TypeItemText
                                    && cl.Language == Lang
                                    && c.CSSPParentItem.CSSPItemID == ParentTVI.ItemID
                                    orderby cl.CSSPItemText
                                    select new TVI
                                    {
                                        ItemID = c.CSSPItemID,
                                        ItemText = cl.CSSPItemText
                                    }).AsQueryable<TVI>();


            if (tvis != null)
            {
                foreach (TVI tvi in tvis)
                {
                    tviList.Add(tvi);
                }
                comboBox.DisplayMember = "ItemText";
                comboBox.ValueMember = "ItemID";
                comboBox.DataSource = tviList;
                comboBox.SelectedIndex = 0;
            }
        }
        private void FillpanelEditMunicipality()
        {
            int MunicipalityItemID = 0;

            if (comboBoxMunicipalities.SelectedValue != null)
            {
                MunicipalityItemID = (int)comboBoxMunicipalities.SelectedValue;
            }

            if (MunicipalityItemID > 0)
            {
                textBoxMunicipalityToEdit.Text = ((TVI)comboBoxMunicipalities.SelectedItem).ItemText;
                butDeleteMunicipalityItem.Enabled = true;
                if (comboBoxInfrastructures.Items.Count > 1)
                {
                    butDeleteMunicipalityItem.Enabled = false;
                }
            }
            else
            {
                textBoxMunicipalityToEdit.Text = "";
                butDeleteMunicipalityItem.Enabled = false;
            }
        }
        private void FillpanelEditInfrastructure()
        {
            int InfrastructureItemID = 0;

            if (comboBoxInfrastructures.SelectedValue != null)
            {
                InfrastructureItemID = (int)comboBoxInfrastructures.SelectedValue;
            }

            if (InfrastructureItemID > 0)
            {
                textBoxInfrastructureToEdit.Text = ((TVI)comboBoxInfrastructures.SelectedItem).ItemText;
                butDeleteInfrastructureItem.Enabled = true;
                if (comboBoxSubInfrastructures.Items.Count > 1)
                {
                    butDeleteInfrastructureItem.Enabled = false;
                }
            }
            else
            {
                textBoxInfrastructureToEdit.Text = "";
                butDeleteInfrastructureItem.Enabled = false;
            }
        }
        private void FillpanelEditSubInfrastructure()
        {
            int SubInfrastructureItemID = 0;

            if (comboBoxSubInfrastructures.SelectedValue != null)
            {
                SubInfrastructureItemID = (int)comboBoxSubInfrastructures.SelectedValue;
            }

            if (SubInfrastructureItemID > 0)
            {
                textBoxSubInfrastructureToEdit.Text = ((TVI)comboBoxSubInfrastructures.SelectedItem).ItemText;
                butDeleteSubInfrastructureItem.Enabled = true;
            }
            else
            {
                textBoxSubInfrastructureToEdit.Text = "";
                butDeleteSubInfrastructureItem.Enabled = false;
            }
        }
        private void AddItem(string ItemText, TVI ParentTVI, ItemType itemType, string lang)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            string TypeText = itemType.ToString();

            TVI tviExist = (from c in vpse.CSSPItems
                            from cl in vpse.CSSPItemLanguages
                            from ct in vpse.CSSPTypeItems
                            where c.CSSPItemID == cl.CSSPItemID
                            && cl.Language == lang
                            && cl.CSSPItemText == ItemText
                            && c.CSSPParentItem.CSSPItemID == ParentTVI.ItemID
                            && c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                            && ct.CSSPTypeText == TypeText
                            select new TVI
                            {
                                ItemID = c.CSSPItemID,
                                ItemText = cl.CSSPItemText
                            }).FirstOrDefault();


            if (tviExist != null)
            {
                MessageBox.Show("Item [" + ItemText + "] already exist under [" + ParentTVI.ItemText + "]");
                return;
            }

            // doesn't exist so we can add it

            try
            {
                CSSPItem TheNewCSSPItem = new CSSPItem();

                CSSPItem ParentItem = (from c in vpse.CSSPItems
                                       where c.CSSPItemID == ParentTVI.ItemID
                                       select c).FirstOrDefault();

                if (ParentItem != null)
                {
                    TheNewCSSPItem.CSSPParentItem = ParentItem;
                }
                else
                {
                    throw new Exception("ParentItem [" + ParentTVI.ItemID + "] not found");
                }

                // get type ID
                CSSPTypeItem TempCSSPTypeItem = (from ct in vpse.CSSPTypeItems
                                                 where ct.CSSPTypeText == TypeText
                                                 select ct).FirstOrDefault();
                if (TempCSSPTypeItem != null)
                {

                    TheNewCSSPItem.CSSPTypeItem = TempCSSPTypeItem;
                }

                TheNewCSSPItem.LastModifiedDate = DateTime.Now;
                TheNewCSSPItem.ModifiedByID = 1;
                TheNewCSSPItem.IsActive = true;

                // creating the language item
                CSSPItemLanguage NewCSSPItemLanguage = new CSSPItemLanguage();
                NewCSSPItemLanguage.Language = "en";
                if (lang == "en")
                {
                    NewCSSPItemLanguage.CSSPItemText = ItemText;
                }
                else
                {
                    NewCSSPItemLanguage.CSSPItemText = ItemText + " (en)";
                }
                NewCSSPItemLanguage.LastModifiedDate = DateTime.Now;
                NewCSSPItemLanguage.ModifiedByID = 1;
                NewCSSPItemLanguage.IsActive = true;

                TheNewCSSPItem.CSSPItemLanguages.Add(NewCSSPItemLanguage);

                CSSPItemLanguage NewCSSPItemLanguage2 = new CSSPItemLanguage();
                NewCSSPItemLanguage2 = new CSSPItemLanguage();
                NewCSSPItemLanguage2.Language = "fr";
                if (lang == "fr")
                {
                    NewCSSPItemLanguage2.CSSPItemText = ItemText;
                }
                else
                {
                    NewCSSPItemLanguage2.CSSPItemText = ItemText + " (fr)";
                }
                NewCSSPItemLanguage2.LastModifiedDate = DateTime.Now;
                NewCSSPItemLanguage2.ModifiedByID = 1;
                NewCSSPItemLanguage2.IsActive = true;

                TheNewCSSPItem.CSSPItemLanguages.Add(NewCSSPItemLanguage2);

                vpse.AddToCSSPItems(TheNewCSSPItem);

                try
                {
                    vpse.SaveChanges();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        MessageBox.Show("ERROR - [" + ex.Message + "]\r\nInnerError - [" + ex.InnerException.Message + "]");
                    }
                    else
                    {
                        MessageBox.Show("ERROR - [" + ex.Message + "]");
                    }
                }
                if (itemType == ItemType.Province)
                {
                    FillComboBox(comboBoxProvinces, "Provinces", RootTVI, ItemType.Province, "en");
                    comboBoxProvinces.SelectedValue = TheNewCSSPItem.CSSPItemID;
                }
                else if (itemType == ItemType.Municipality)
                {
                    FillComboBox(comboBoxMunicipalities, "Municipalities", ParentTVI, ItemType.Municipality, "en");
                    comboBoxMunicipalities.SelectedValue = TheNewCSSPItem.CSSPItemID;
                }
                else if (itemType == ItemType.WWTP)
                {
                    FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
                    comboBoxInfrastructures.SelectedValue = TheNewCSSPItem.CSSPItemID;
                }
                else if (itemType == ItemType.LiftStation)
                {
                    FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
                    comboBoxSubInfrastructures.SelectedValue = TheNewCSSPItem.CSSPItemID;
                }
                else
                {
                    MessageBox.Show("Error in ModifyItem.");
                }

            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        private void DeleteItem(TVI CurrentTVI, TVI ParentTVI, ItemType itemType)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            if (MessageBox.Show("Are you sure you want to delete item [" + CurrentTVI.ItemText +
                "] from [" + ParentTVI.ItemText +
                "].\r\nDeleting this item will also delete all the other related information\r\n" +
                " as well as all Visual Plumes results of [" + CurrentTVI.ItemText + "].", "Deleting [" + CurrentTVI.ItemText + "]",
                MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                CSSPItem ItemToDelete = (from c in vpse.CSSPItems
                                         where c.CSSPItemID == CurrentTVI.ItemID
                                         select c).FirstOrDefault();
                if (ItemToDelete != null)
                {
                    vpse.DeleteObject(ItemToDelete);
                    try
                    {
                        vpse.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex);
                    }
                }
            }
            if (itemType == ItemType.Province)
            {
                comboBoxProvinces.DataSource = null;
                comboBoxProvinces.Items.Clear();
                FillComboBox(comboBoxProvinces, "Provinces", RootTVI, ItemType.Province, "en");
            }
            else if (itemType == ItemType.Municipality)
            {
                comboBoxMunicipalities.DataSource = null;
                comboBoxMunicipalities.Items.Clear();
                textBoxMunicipalityToEdit.Text = "";
                FillComboBox(comboBoxMunicipalities, "Municipalities", ParentTVI, ItemType.Municipality, "en");
            }
            else if (itemType == ItemType.WWTP)
            {
                comboBoxInfrastructures.DataSource = null;
                comboBoxInfrastructures.Items.Clear();
                textBoxInfrastructureToEdit.Text = "";
                FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
            }
            else if (itemType == ItemType.LiftStation)
            {
                comboBoxSubInfrastructures.DataSource = null;
                comboBoxSubInfrastructures.Items.Clear();
                textBoxSubInfrastructureToEdit.Text = "";
                FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
            }
            else
            {
                MessageBox.Show("Error in DeleteItem");
                return;
            }
        }
        private void ModifyItem(string NewText, TVI CurrentTVI, TVI ParentTVI, ItemType itemType)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            CSSPItemLanguage csspItemLanguagExist = (from c in vpse.CSSPItems
                                                     from cl in vpse.CSSPItemLanguages
                                                     where c.CSSPItemID == cl.CSSPItemID
                                                     && c.CSSPParentItem.CSSPItemID == ParentTVI.ItemID
                                                     && c.CSSPItemID != CurrentTVI.ItemID
                                                     && cl.CSSPItemText == NewText
                                                     select cl).FirstOrDefault();

            if (csspItemLanguagExist != null)
            {
                MessageBox.Show("Item [" + NewText + "] already exist for [" + ParentTVI.ItemText + "]");
            }

            CSSPItemLanguage csspItemLanguageToModify = (from c in vpse.CSSPItems
                                                         from cl in vpse.CSSPItemLanguages
                                                         where c.CSSPItemID == cl.CSSPItemID
                                                         && c.CSSPItemID == CurrentTVI.ItemID
                                                         select cl).FirstOrDefault();

            csspItemLanguageToModify.CSSPItemText = NewText;

            try
            {
                vpse.SaveChanges();
                if (itemType == ItemType.Province)
                {
                    FillComboBox(comboBoxProvinces, "Provinces", RootTVI, ItemType.Province, "en");
                    comboBoxProvinces.SelectedValue = CurrentTVI.ItemID;
                }
                else if (itemType == ItemType.Municipality)
                {
                    FillComboBox(comboBoxMunicipalities, "Municipalities", ParentTVI, ItemType.Municipality, "en");
                    comboBoxMunicipalities.SelectedValue = CurrentTVI.ItemID;
                }
                else if (itemType == ItemType.WWTP)
                {
                    FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
                    comboBoxInfrastructures.SelectedValue = CurrentTVI.ItemID;
                }
                else if (itemType == ItemType.LiftStation)
                {
                    FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
                    comboBoxSubInfrastructures.SelectedValue = CurrentTVI.ItemID;
                }
                else
                {
                    MessageBox.Show("Error in ModifyItem.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

        }
        private void DeleteTreeViewItem()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (treeViewItems.SelectedNode != null)
            {
                TreeNode ParentNode = treeViewItems.SelectedNode.Parent;
                TreeNode NodeToDelete = treeViewItems.SelectedNode;

                TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;
                if (CurrentTVI.ItemText == "Root")
                {
                    MessageBox.Show("Can't delete Root");
                    return;
                }

                TVI ParentTVI = (TVI)treeViewItems.SelectedNode.Parent.Tag;

                CSSPTypeItem TypeNotToDelete = (from c in vpse.CSSPItems
                                                from ct in vpse.CSSPTypeItems
                                                where c.CSSPItemID == CurrentTVI.ItemID
                                                && c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                                                select ct).FirstOrDefault();

                if (TypeNotToDelete != null)
                {
                    if (TypeNotToDelete.CSSPTypeText == ItemType.Province.ToString())
                    {
                        MessageBox.Show("Can't delete Province");
                        return;
                    }
                    else
                    {
                        //return;
                    }

                    if (MessageBox.Show("Are you sure you want to delete item [" + CurrentTVI.ItemText +
                        "] from [" + treeViewItems.SelectedNode.Parent.Parent.Text +
                        "][" + treeViewItems.SelectedNode.Parent.Text +
                        "].\r\nDeleting this item will also delete all the other related information\r\n" +
                        " as well as all Visual Plumes results of [" + CurrentTVI.ItemText + "].", "Deleting [" + CurrentTVI.ItemText + "]",
                        MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        CSSPItem ItemToDelete = (from c in vpse.CSSPItems
                                                 where c.CSSPItemID == CurrentTVI.ItemID
                                                 select c).FirstOrDefault();
                        if (ItemToDelete != null)
                        {
                            vpse.DeleteObject(ItemToDelete);
                            try
                            {
                                vpse.SaveChanges();
                                treeViewItems.SelectedNode.Remove();
                                treeViewItems.SelectedNode = ParentNode;
                                butDeleteTreeViewItem.Enabled = false;
                            }
                            catch (Exception ex)
                            {
                                ShowError(ex);
                            }
                        }
                    }
                }

            }
        }
        private void FillAfterSelect(TreeNode treeNode)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI CurrentTVI = new TVI();
            TreeNode tn = treeNode;
            richTextBoxTreeViewPath.Text = tn.FullPath;
            PathElements = tn.FullPath.Split(@"\".ToCharArray()[0]);
            lblScenariosForTxt.Text = "";
            lblBoxModelForTxt.Text = "";
            lblInfrastructureInfoForTxt.Text = "";
            foreach (string Path in PathElements.Skip(1))
            {
                lblScenariosForTxt.Text = lblScenariosForTxt.Text + "[" + Path + "] ";
            }
            lblBoxModelForTxt.Text = lblScenariosForTxt.Text;
            lblInfrastructureInfoForTxt.Text = lblScenariosForTxt.Text;
            lblMikeMunicipalityTxt.Text = lblScenariosForTxt.Text;


            if (treeViewItems.SelectedNode != null)
            {
                CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

                CSSPTypeItem CurrentType = (from ct in vpse.CSSPTypeItems
                                            from c in vpse.CSSPItems
                                            where ct.CSSPTypeItemID == c.CSSPTypeItem.CSSPTypeItemID
                                            && c.CSSPItemID == CurrentTVI.ItemID
                                            select ct).FirstOrDefault();

                //if (treeViewItems.SelectedNode.Nodes.Count == 0)
                //{
                treeViewItems.SelectedNode.Nodes.Clear();

                if (CurrentType.CSSPTypeText == ItemType.Root.ToString())
                {
                    LoadChildren(treeViewItems.SelectedNode, ItemType.Province, "en");
                }
                else if (CurrentType.CSSPTypeText == ItemType.Province.ToString())
                {
                    LoadChildren(treeViewItems.SelectedNode, ItemType.Municipality, "en");
                }
                else if (CurrentType.CSSPTypeText == ItemType.Municipality.ToString())
                {
                    LoadChildren(treeViewItems.SelectedNode, ItemType.WWTP, "en");
                }
                else if (CurrentType.CSSPTypeText == ItemType.WWTP.ToString())
                {
                    LoadChildren(treeViewItems.SelectedNode, ItemType.LiftStation, "en");
                }
                else if (CurrentType.CSSPTypeText == ItemType.LiftStation.ToString())
                {
                    // nothing yet
                }

                //}
                if (radioButtonVPScenarioResults.Checked == true)
                {
                    RefreshGridViews(treeViewItems.SelectedNode, CurrentTVI, CurrentType);
                }
                else if (radioButtonStoredInformation.Checked == true)
                {
                    RefreshStoredInfrastructureStationInfo(treeViewItems.SelectedNode, CurrentTVI, CurrentType);
                }
                else if (radioButtonBoxModel.Checked == true)
                {
                    RefreshBoxModelPanel(treeViewItems.SelectedNode, CurrentTVI, CurrentType);
                }
                else
                {
                    MessageBox.Show("Error in FillAfterSelect");
                    return;
                }
                if (treeViewItems.SelectedNode.Nodes.Count > 0)
                {
                    butDeleteTreeViewItem.Enabled = false;
                }
                else
                {
                    butDeleteTreeViewItem.Enabled = true;
                }

                // MIKE section
                if (CurrentType.CSSPTypeText == ItemType.Province.ToString() || CurrentType.CSSPTypeText == ItemType.Municipality.ToString())
                {
                    RefreshGridViews(treeViewItems.SelectedNode, CurrentTVI, CurrentType);
                }
            }
        }
        private void RefreshStoredInfrastructureStationInfo(TreeNode treeNode, TVI CurrentTVI, CSSPTypeItem CurrentType)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (CurrentType.CSSPTypeText == ItemType.WWTP.ToString() || CurrentType.CSSPTypeText == ItemType.LiftStation.ToString())
            {
                panelInformation.Visible = true;
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;

                //EnableItemForInfrastructure(true);

                var CurrentInfrastructures = from w in vpse.Infrastructures
                                             where w.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                             select w;

                if (CurrentInfrastructures != null)
                {
                    ClearStoredInfrastructureInfo();
                    foreach (Infrastructure Infra in CurrentInfrastructures)
                    {
                        textBoxStoredID.Text = string.Format("{0}", Infra.InfrastructureID);
                        textBoxStoredInfrastructureType.Text = string.Format("{0}", Infra.InfrastructureType);
                        textBoxStoredPrismID.Text = string.Format("{0}", Infra.PrismID);
                        textBoxStoredOutfallPrismID.Text = string.Format("{0}", Infra.OutfallPrismID);
                        textBoxStoredType.Text = string.Format("{0}", Infra.TreatmentType);
                        textBoxStoredCategory.Text = string.Format("{0}", Infra.Category);
                        textBoxStoredTPID.Text = string.Format("{0}", Infra.TPID);
                        textBoxStoredLSID.Text = string.Format("{0}", Infra.LSID);
                        textBoxStoredSiteID.Text = string.Format("{0}", Infra.SiteID);
                        if (Infra.IsActive == null)
                            checkBoxStoredActive.CheckState = CheckState.Indeterminate;
                        else if (Infra.IsActive == false)
                            checkBoxStoredActive.CheckState = CheckState.Unchecked;
                        else
                            checkBoxStoredActive.CheckState = CheckState.Checked;
                        textBoxStoredDateOfConstruction.Text = string.Format("{0:d}", Infra.DateOfConstruction);
                        textBoxStoredDateOfRecentUpgrade.Text = string.Format("{0:d}", Infra.DateOfRecentUpgrade);
                        textBoxStoredLocator.Text = string.Format("{0}", Infra.Locator);
                        textBoxStoredDatum.Text = string.Format("{0}", Infra.Datum);
                        textBoxStoredZone.Text = string.Format("{0}", Infra.Zone);
                        textBoxStoredEasting.Text = string.Format("{0}", Infra.Easting);
                        textBoxStoredNorthing.Text = string.Format("{0}", Infra.Northing);
                        textBoxStoredLatitude.Text = string.Format("{0}", Infra.Latitude);
                        textBoxStoredLongitude.Text = string.Format("{0}", Infra.Longitude);
                        textBoxStoredDesignPopulation.Text = string.Format("{0}", Infra.PopDesign);
                        textBoxStoredPopulationServed.Text = string.Format("{0}", Infra.PopServed);
                        textBoxStoredDesignFlow.Text = string.Format("{0}", Infra.DesignFlow);
                        textBoxStoredAverageFlow.Text = string.Format("{0}", Infra.AverageFlow);
                        textBoxStoredPeakFlow.Text = string.Format("{0}", Infra.PeakFlow);
                        textBoxStoredEstimatedFlow.Text = string.Format("{0}", Infra.EstimatedFlow);
                        textBoxStoredOperatorName.Text = string.Format("{0}", Infra.OperatorName);
                        textBoxStoredOperatorTelephone.Text = string.Format("{0}", Infra.OperatorTelephone);
                        textBoxStoredOperatorEmail.Text = string.Format("{0}", Infra.OperatorEmail);
                        textBoxStoredNumberOfVisitToPlantPerWeek.Text = string.Format("{0}", Infra.NumbOfVisitToPlantPerWeek);
                        textBoxStoredDisinfection.Text = string.Format("{0}", Infra.Disinfection);
                        textBoxStoredBODRequired.Text = string.Format("{0}", Infra.BODRequired);
                        textBoxStoredSSRequired.Text = string.Format("{0}", Infra.SSRequired);
                        textBoxStoredFCRequired.Text = string.Format("{0}", Infra.FCRequired);
                        if (Infra.HasAlarmSystem == null)
                            checkBoxStoredHasAlarmSystem.CheckState = CheckState.Indeterminate;
                        else if (Infra.HasAlarmSystem == false)
                            checkBoxStoredHasAlarmSystem.CheckState = CheckState.Unchecked;
                        else
                            checkBoxStoredHasAlarmSystem.CheckState = CheckState.Checked;
                        richTextBoxStoredAlarmSystemType.Text = string.Format("{0}", Infra.AlarmSystemTypeAndComment);
                        textBoxStoredCollectionSystemType.Text = string.Format("{0}", Infra.CollectionSystemType);
                        textBoxStoredCombinedPercent.Text = string.Format("{0}", Infra.CombinedPercent);
                        textBoxStoredBypassFreqency.Text = string.Format("{0}", Infra.BypassFrequency);
                        textBoxStoredBypassTypeOrCause.Text = string.Format("{0}", Infra.BypassTypeOrCause);
                        textBoxStoredBypassAverageTime.Text = string.Format("{0}", Infra.BypassAverageTime);
                        textBoxStoredBypassNotificationTime.Text = string.Format("{0}", Infra.BypassNotificationTime);
                        textBoxStoredLagoonOrMachanical.Text = string.Format("{0}", Infra.LagoonOrMechanical);
                        textBoxStoredOutfallEasting.Text = string.Format("{0}", Infra.OutfallEasting);
                        textBoxStoredOutfallNorthing.Text = string.Format("{0}", Infra.OutfallNorthing);
                        textBoxStoredOutfallLatitude.Text = string.Format("{0}", Infra.OutfallLatitude);
                        textBoxStoredOutfallLongitude.Text = string.Format("{0}", Infra.OutfallLongitude);
                        textBoxStoredOutfallZone.Text = string.Format("{0}", Infra.OutfallZone);
                        textBoxStoredOutfallDatum.Text = string.Format("{0}", Infra.OutfallDatum);
                        textBoxStoredOutfallDepthHigh.Text = string.Format("{0}", Infra.OutfallDepthHigh);
                        textBoxStoredOutfallDepthLow.Text = string.Format("{0}", Infra.OutfallDepthLow);
                        textBoxStoredOutfallNumberOfPorts.Text = string.Format("{0}", Infra.OutfallNumberOfPorts);
                        textBoxStoredOutfallPortDiameter.Text = string.Format("{0}", Infra.OutfallPortDiameter);
                        textBoxStoredOutfallPortSpacing.Text = string.Format("{0}", Infra.OutfallPortSpacing);
                        textBoxStoredOutfallPortElevation.Text = string.Format("{0}", Infra.OutfallPortElevation);
                        textBoxStoredOutfallVerticalAngle.Text = string.Format("{0}", Infra.OutfallVerticalAngle);
                        textBoxStoredOutfallHorizontalAngle.Text = string.Format("{0}", Infra.OutfallHorizontalAngle);
                        textBoxStoredOutfallDecayRate.Text = string.Format("{0}", Infra.OutfallDecayRate);
                        textBoxStoredOutfallNearFieldVelocity.Text = string.Format("{0}", Infra.OutfallNearFieldVelocity);
                        textBoxStoredOutfallFarFieldVelocity.Text = string.Format("{0}", Infra.OutfallFarFieldVelocity);
                        textBoxStoredOutfallReceivingWaterSalinity.Text = string.Format("{0}", Infra.OutfallReceivingWaterSalinity);
                        textBoxStoredOutfallReceivingWaterTemperature.Text = string.Format("{0}", Infra.OutfallReceivingWaterTemperature);
                        textBoxStoredOutfallReceivingWaterFC.Text = string.Format("{0}", Infra.OutfallReceivingWaterFC);
                        textBoxStoredOutfallDistanceFromShore.Text = string.Format("{0}", Infra.OutfallDistanceFromShore);
                        textBoxStoredOutfallReceivingWaterName.Text = string.Format("{0}", Infra.ReceivingWaterName);
                        richTextBoxStoredInputDataComments.Text = string.Format("{0}", Infra.InputDataComments);
                        richTextBoxStoredOtherComments.Text = string.Format("{0}", Infra.Comments);
                    }
                }
                butSaveInfrastructureInfoChanges.Enabled = false;
            }
            else
            {
                panelInformation.Visible = false;
                panelInfrastructureOrMunicipalityNotSelected.Visible = true;
            }

        }
        //private void EnableItemForInfrastructure(bool IsInfrastructure)
        //{
        //    lblStoredOperatorName.Enabled = IsInfrastructure;
        //    textBoxStoredOperatorName.Enabled = IsInfrastructure;
        //    lblStoredOperatorTelephone.Enabled = IsInfrastructure;
        //    textBoxStoredOperatorTelephone.Enabled = IsInfrastructure;
        //    lblStoredOperatorEmail.Enabled = IsInfrastructure;
        //    textBoxStoredOperatorEmail.Enabled = IsInfrastructure;
        //    lblStoredDisinfection.Enabled = IsInfrastructure;
        //    textBoxStoredDisinfection.Enabled = IsInfrastructure;
        //    lblStoredBODRequired.Enabled = IsInfrastructure;
        //    textBoxStoredBODRequired.Enabled = IsInfrastructure;
        //    lblStoredSSRequired.Enabled = IsInfrastructure;
        //    textBoxStoredSSRequired.Enabled = IsInfrastructure;
        //    lblStoredFCRequired.Enabled = IsInfrastructure;
        //    textBoxStoredFCRequired.Enabled = IsInfrastructure;
        //    lblStoredLagoonOrMachanical.Enabled = IsInfrastructure;
        //    textBoxStoredLagoonOrMachanical.Enabled = IsInfrastructure;
        //}
        private void ClearStoredInfrastructureInfo()
        {
            textBoxStoredID.Text = "";
            textBoxStoredInfrastructureType.Text = "";
            textBoxStoredType.Text = "";
            textBoxStoredCategory.Text = "";
            textBoxStoredTPID.Text = "";
            textBoxStoredLSID.Text = "";
            textBoxStoredSiteID.Text = "";
            textBoxStoredPrismID.Text = "";
            textBoxStoredOutfallPrismID.Text = "";
            checkBoxStoredActive.CheckState = CheckState.Indeterminate;
            textBoxStoredDateOfConstruction.Text = "";
            textBoxStoredDateOfRecentUpgrade.Text = "";
            textBoxStoredLocator.Text = "";
            textBoxStoredDatum.Text = "";
            textBoxStoredZone.Text = "";
            textBoxStoredEasting.Text = "";
            textBoxStoredNorthing.Text = "";
            textBoxStoredLatitude.Text = "";
            textBoxStoredLongitude.Text = "";
            textBoxStoredDesignPopulation.Text = "";
            textBoxStoredPopulationServed.Text = "";
            textBoxStoredDesignFlow.Text = "";
            textBoxStoredAverageFlow.Text = "";
            textBoxStoredPeakFlow.Text = "";
            textBoxStoredEstimatedFlow.Text = "";
            textBoxStoredOperatorName.Text = "";
            textBoxStoredOperatorTelephone.Text = "";
            textBoxStoredOperatorEmail.Text = "";
            textBoxStoredNumberOfVisitToPlantPerWeek.Text = "";
            textBoxStoredDisinfection.Text = "";
            textBoxStoredBODRequired.Text = "";
            textBoxStoredSSRequired.Text = "";
            textBoxStoredFCRequired.Text = "";
            checkBoxStoredHasAlarmSystem.CheckState = CheckState.Indeterminate;
            richTextBoxStoredAlarmSystemType.Text = "";
            textBoxStoredCollectionSystemType.Text = "";
            textBoxStoredCombinedPercent.Text = "";
            textBoxStoredBypassFreqency.Text = "";
            textBoxStoredBypassTypeOrCause.Text = "";
            textBoxStoredBypassAverageTime.Text = "";
            textBoxStoredBypassNotificationTime.Text = "";
            textBoxStoredLagoonOrMachanical.Text = "";
            textBoxStoredOutfallEasting.Text = "";
            textBoxStoredOutfallNorthing.Text = "";
            textBoxStoredOutfallLatitude.Text = "";
            textBoxStoredOutfallLongitude.Text = "";
            textBoxStoredOutfallZone.Text = "";
            textBoxStoredOutfallDatum.Text = "";
            textBoxStoredOutfallDepthHigh.Text = "";
            textBoxStoredOutfallDepthLow.Text = "";
            textBoxStoredOutfallNumberOfPorts.Text = "";
            textBoxStoredOutfallPortDiameter.Text = "";
            textBoxStoredOutfallPortSpacing.Text = "";
            textBoxStoredOutfallPortElevation.Text = "";
            textBoxStoredOutfallVerticalAngle.Text = "";
            textBoxStoredOutfallHorizontalAngle.Text = "";
            textBoxStoredOutfallDecayRate.Text = "";
            textBoxStoredOutfallNearFieldVelocity.Text = "";
            textBoxStoredOutfallFarFieldVelocity.Text = "";
            textBoxStoredOutfallReceivingWaterSalinity.Text = "";
            textBoxStoredOutfallReceivingWaterTemperature.Text = "";
            textBoxStoredOutfallReceivingWaterFC.Text = "";
            textBoxStoredOutfallDistanceFromShore.Text = "";
            textBoxStoredOutfallReceivingWaterName.Text = "";
            richTextBoxStoredInputDataComments.Text = "";
            richTextBoxStoredOtherComments.Text = "";
        }
        private void RefreshBoxModelPanel(TreeNode treeNode, TVI CurrentTVI, CSSPTypeItem CurrentType)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (CurrentType.CSSPTypeText == ItemType.WWTP.ToString() || CurrentType.CSSPTypeText == ItemType.LiftStation.ToString())
            {
                panelBoxModel.Visible = true;
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;
                butSaveBoxModelDataAndResult.Enabled = false;


                // start filling

                List<BoxModel> boxModels = (from b in vpse.BoxModels
                                            where b.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                            select b).ToList<BoxModel>();


                comboBoxStoredBoxModelScenarios.DataSource = null;
                comboBoxStoredBoxModelScenarios.DataSource = boxModels;
                comboBoxStoredBoxModelScenarios.DisplayMember = "ScenarioName";
                comboBoxStoredBoxModelScenarios.ValueMember = "BoxModelID";

                if (boxModels.Count > 0)
                {
                    butDeleteBoxModelScenario.Enabled = true;
                }
                else
                {
                    butDeleteBoxModelScenario.Enabled = false;
                }
                butRecalculate.Enabled = false;

            }
            else
            {
                panelBoxModel.Visible = false;
                panelInfrastructureOrMunicipalityNotSelected.Visible = true;
            }
        }
        private void SaveBoxModelDataAndResults()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (butRecalculate.Enabled == true)
            {
                MessageBox.Show("Please make sure the recalculation is up to date by re-clicking the Recalculate button");
                return;
            }

            if (treeViewItems.SelectedNode == null)
            {
                MessageBox.Show("Please select an infrastructure item");
                return;
            }

            TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

            // check if the scenario name already exist
            BoxModel boxModel = (from b in vpse.BoxModels
                                 where b.ScenarioName == textBoxBoxModelScenario.Text.Trim()
                                 && b.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                 select b).FirstOrDefault();

            if (boxModel != null)
            {
                if (MessageBox.Show("Box model scenario [" + textBoxBoxModelScenario.Text.Trim() +
                    "] for [" + treeViewItems.SelectedNode.Parent.Parent.Text +
                    "] [" + treeViewItems.SelectedNode.Parent.Text +
                    "] [" + CurrentTVI.ItemText +
                    "] already exist in the database. \r\n\r\nDo you want to replace the results?",
                    "Already Exist. Replace Results?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {

                    // getting and changing results for

                    boxModel.Flow = BMFlow;
                    boxModel.Depth = BMDepth;
                    boxModel.Temperature = BMTemperature;
                    boxModel.Dilution = BMDilution;
                    boxModel.DecayRate = BMDecayCoefficient;
                    boxModel.FCUntreated = BMFCUntreated;
                    boxModel.FCPreDisinfection = BMFCPreDisinfection;
                    boxModel.Concentration = BMConcentrationObjective;
                    boxModel.T90 = BMT90;
                    boxModel.FlowDuration = BMFlowDuration;

                    int BoxModelID = boxModel.BoxModelID;

                    // doing Dilution

                    int resultType = (int)BoxModelResultType.Dilution;

                    BoxModelResult boxModelResultDilution = (from br in vpse.BoxModelResults
                                                             where br.BoxModelID == BoxModelID
                                                             && br.ResultType == resultType
                                                             select br).FirstOrDefault();

                    if (boxModelResultDilution == null)
                    {
                        MessageBox.Show("ERROR - Could not find BoxModelResult with ID = [" + BoxModelID + "] and of BoxModelResultType = [" + BoxModelResultType.Dilution.ToString() + "]");
                    }

                    boxModelResultDilution.Volume = BMDilutionVolume;
                    boxModelResultDilution.Surface = BMDilutionSurface;
                    boxModelResultDilution.Radius = BMDilutionRadius;
                    boxModelResultDilution.LeftSideDiameterLineAngle = BMDilutionLeftSideDiameterLineAngle;
                    boxModelResultDilution.CircleCenterLatitude = BMDilutionCircleCenterLatitude;
                    boxModelResultDilution.CircleCenterLongitude = BMDilutionCircleCenterLongitude;
                    boxModelResultDilution.FixLength = BMDilutionFixLength;
                    boxModelResultDilution.FixWidth = BMDilutionFixWidth;
                    boxModelResultDilution.RectLength = BMDilutionRectLength;
                    boxModelResultDilution.RectWidth = BMDilutionRectWidth;
                    boxModelResultDilution.LeftSideLineAngle = BMDilutionLeftSideLineAngle;
                    boxModelResultDilution.LeftSideLineStartLatitude = BMDilutionLeftSideLineStartLatitude;
                    boxModelResultDilution.LeftSideLineStartLongitude = BMDilutionLeftSideLineStartLongitude;

                    // doing NoDecayUntreated

                    resultType = (int)BoxModelResultType.NoDecayUntreated;

                    BoxModelResult boxModelResultNoDecayUntreated = (from br in vpse.BoxModelResults
                                                                     where br.BoxModelID == BoxModelID
                                                                     && br.ResultType == resultType
                                                                     select br).FirstOrDefault();

                    if (boxModelResultNoDecayUntreated == null)
                    {
                        MessageBox.Show("ERROR - Could not find BoxModelResult with ID = [" + BoxModelID + "] and of BoxModelResultType = [" + BoxModelResultType.NoDecayUntreated.ToString() + "]");
                    }

                    boxModelResultNoDecayUntreated.Volume = BMNoDecayUntreatedVolume;
                    boxModelResultNoDecayUntreated.Surface = BMNoDecayUntreatedSurface;
                    boxModelResultNoDecayUntreated.Radius = BMNoDecayUntreatedRadius;
                    boxModelResultNoDecayUntreated.LeftSideDiameterLineAngle = BMNoDecayUntreatedLeftSideDiameterLineAngle;
                    boxModelResultNoDecayUntreated.CircleCenterLatitude = BMNoDecayUntreatedCircleCenterLatitude;
                    boxModelResultNoDecayUntreated.CircleCenterLongitude = BMNoDecayUntreatedCircleCenterLongitude;
                    boxModelResultNoDecayUntreated.FixLength = BMNoDecayUntreatedFixLength;
                    boxModelResultNoDecayUntreated.FixWidth = BMNoDecayUntreatedFixWidth;
                    boxModelResultNoDecayUntreated.RectLength = BMNoDecayUntreatedRectLength;
                    boxModelResultNoDecayUntreated.RectWidth = BMNoDecayUntreatedRectWidth;
                    boxModelResultNoDecayUntreated.LeftSideLineAngle = BMNoDecayUntreatedLeftSideLineAngle;
                    boxModelResultNoDecayUntreated.LeftSideLineStartLatitude = BMNoDecayUntreatedLeftSideLineStartLatitude;
                    boxModelResultNoDecayUntreated.LeftSideLineStartLongitude = BMNoDecayUntreatedLeftSideLineStartLongitude;

                    // doing NoDecayPreDis

                    resultType = (int)BoxModelResultType.NoDecayPreDis;

                    BoxModelResult boxModelResultNoDecayPreDis = (from br in vpse.BoxModelResults
                                                                  where br.BoxModelID == BoxModelID
                                                                  && br.ResultType == resultType
                                                                  select br).FirstOrDefault();

                    if (boxModelResultNoDecayPreDis == null)
                    {
                        MessageBox.Show("ERROR - Could not find BoxModelResult with ID = [" + BoxModelID + "] and of BoxModelResultType = [" + BoxModelResultType.NoDecayPreDis.ToString() + "]");
                    }

                    boxModelResultNoDecayPreDis.Volume = BMNoDecayPreDisVolume;
                    boxModelResultNoDecayPreDis.Surface = BMNoDecayPreDisSurface;
                    boxModelResultNoDecayPreDis.Radius = BMNoDecayPreDisRadius;
                    boxModelResultNoDecayPreDis.LeftSideDiameterLineAngle = BMNoDecayPreDisLeftSideDiameterLineAngle;
                    boxModelResultNoDecayPreDis.CircleCenterLatitude = BMNoDecayPreDisCircleCenterLatitude;
                    boxModelResultNoDecayPreDis.CircleCenterLongitude = BMNoDecayPreDisCircleCenterLongitude;
                    boxModelResultNoDecayPreDis.FixLength = BMNoDecayPreDisFixLength;
                    boxModelResultNoDecayPreDis.FixWidth = BMNoDecayPreDisFixWidth;
                    boxModelResultNoDecayPreDis.RectLength = BMNoDecayPreDisRectLength;
                    boxModelResultNoDecayPreDis.RectWidth = BMNoDecayPreDisRectWidth;
                    boxModelResultNoDecayPreDis.LeftSideLineAngle = BMNoDecayPreDisLeftSideLineAngle;
                    boxModelResultNoDecayPreDis.LeftSideLineStartLatitude = BMNoDecayPreDisLeftSideLineStartLatitude;
                    boxModelResultNoDecayPreDis.LeftSideLineStartLongitude = BMNoDecayPreDisLeftSideLineStartLongitude;

                    // doing DecayUntreated

                    resultType = (int)BoxModelResultType.DecayUntreated;

                    BoxModelResult boxModelResultDecayUntreated = (from br in vpse.BoxModelResults
                                                                   where br.BoxModelID == BoxModelID
                                                                   && br.ResultType == resultType
                                                                   select br).FirstOrDefault();

                    if (boxModelResultDecayUntreated == null)
                    {
                        MessageBox.Show("ERROR - Could not find BoxModelResult with ID = [" + BoxModelID + "] and of BoxModelResultType = [" + BoxModelResultType.DecayUntreated.ToString() + "]");
                    }

                    boxModelResultDecayUntreated.Volume = BMDecayUntreatedVolume;
                    boxModelResultDecayUntreated.Surface = BMDecayUntreatedSurface;
                    boxModelResultDecayUntreated.Radius = BMDecayUntreatedRadius;
                    boxModelResultDecayUntreated.LeftSideDiameterLineAngle = BMDecayUntreatedLeftSideDiameterLineAngle;
                    boxModelResultDecayUntreated.CircleCenterLatitude = BMDecayUntreatedCircleCenterLatitude;
                    boxModelResultDecayUntreated.CircleCenterLongitude = BMDecayUntreatedCircleCenterLongitude;
                    boxModelResultDecayUntreated.FixLength = BMDecayUntreatedFixLength;
                    boxModelResultDecayUntreated.FixWidth = BMDecayUntreatedFixWidth;
                    boxModelResultDecayUntreated.RectLength = BMDecayUntreatedRectLength;
                    boxModelResultDecayUntreated.RectWidth = BMDecayUntreatedRectWidth;
                    boxModelResultDecayUntreated.LeftSideLineAngle = BMDecayUntreatedLeftSideLineAngle;
                    boxModelResultDecayUntreated.LeftSideLineStartLatitude = BMDecayUntreatedLeftSideLineStartLatitude;
                    boxModelResultDecayUntreated.LeftSideLineStartLongitude = BMDecayUntreatedLeftSideLineStartLongitude;


                    // doing DecayPreDis

                    resultType = (int)BoxModelResultType.DecayPreDis;

                    BoxModelResult boxModelResultDecayPreDis = (from br in vpse.BoxModelResults
                                                                where br.BoxModelID == BoxModelID
                                                                && br.ResultType == resultType
                                                                select br).FirstOrDefault();

                    if (boxModelResultDecayPreDis == null)
                    {
                        MessageBox.Show("ERROR - Could not find BoxModelResult with ID = [" + BoxModelID + "] and of BoxModelResultType = [" + BoxModelResultType.DecayPreDis.ToString() + "]");
                    }

                    boxModelResultDecayPreDis.Volume = BMDecayPreDisVolume;
                    boxModelResultDecayPreDis.Surface = BMDecayPreDisSurface;
                    boxModelResultDecayPreDis.Radius = BMDecayPreDisRadius;
                    boxModelResultDecayPreDis.LeftSideDiameterLineAngle = BMDecayPreDisLeftSideDiameterLineAngle;
                    boxModelResultDecayPreDis.CircleCenterLatitude = BMDecayPreDisCircleCenterLatitude;
                    boxModelResultDecayPreDis.CircleCenterLongitude = BMDecayPreDisCircleCenterLongitude;
                    boxModelResultDecayPreDis.FixLength = BMDecayPreDisFixLength;
                    boxModelResultDecayPreDis.FixWidth = BMDecayPreDisFixWidth;
                    boxModelResultDecayPreDis.RectLength = BMDecayPreDisRectLength;
                    boxModelResultDecayPreDis.RectWidth = BMDecayPreDisRectWidth;
                    boxModelResultDecayPreDis.LeftSideLineAngle = BMDecayPreDisLeftSideLineAngle;
                    boxModelResultDecayPreDis.LeftSideLineStartLatitude = BMDecayPreDisLeftSideLineStartLatitude;
                    boxModelResultDecayPreDis.LeftSideLineStartLongitude = BMDecayPreDisLeftSideLineStartLongitude;

                    try
                    {
                        vpse.SaveChanges();
                        butSaveBoxModelDataAndResult.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            MessageBox.Show("ERROR - [" + ex.Message + "]\r\nInnerError - [" + ex.InnerException.Message + "]");
                            return;
                        }
                        else
                        {
                            MessageBox.Show("ERROR - [" + ex.Message + "]");
                            return;
                        }
                    }

                }
            }
            else
            {

                CSSPItem csspItem = (from c in vpse.CSSPItems
                                     where c.CSSPItemID == CurrentTVI.ItemID
                                     select c).FirstOrDefault();


                if (csspItem != null)
                {

                    // doing Dilution

                    BoxModel NewBoxModel = new BoxModel();

                    NewBoxModel.CSSPItem = csspItem;
                    NewBoxModel.ScenarioName = textBoxBoxModelScenario.Text.Trim();
                    NewBoxModel.Flow = BMFlow;
                    NewBoxModel.Depth = BMDepth;
                    NewBoxModel.Temperature = BMTemperature;
                    NewBoxModel.Dilution = BMDilution;
                    NewBoxModel.DecayRate = BMDecayCoefficient;
                    NewBoxModel.FCUntreated = BMFCUntreated;
                    NewBoxModel.FCPreDisinfection = BMFCPreDisinfection;
                    NewBoxModel.Concentration = BMConcentrationObjective;
                    NewBoxModel.T90 = BMT90;
                    NewBoxModel.FlowDuration = BMFlowDuration;
                    NewBoxModel.LastModifiedDate = DateTime.Now;
                    NewBoxModel.ModifiedByID = 1;
                    NewBoxModel.IsActive = true;

                    BoxModelResult NewBoxModelResultDilution = new BoxModelResult();

                    NewBoxModelResultDilution.ResultType = (int)BoxModelResultType.Dilution;
                    NewBoxModelResultDilution.Volume = BMDilutionVolume;
                    NewBoxModelResultDilution.Surface = BMDilutionSurface;
                    NewBoxModelResultDilution.Radius = BMDilutionRadius;
                    NewBoxModelResultDilution.LeftSideDiameterLineAngle = BMDilutionLeftSideDiameterLineAngle;
                    NewBoxModelResultDilution.CircleCenterLatitude = BMDilutionCircleCenterLatitude;
                    NewBoxModelResultDilution.CircleCenterLongitude = BMDilutionCircleCenterLongitude;
                    NewBoxModelResultDilution.FixLength = BMDilutionFixLength;
                    NewBoxModelResultDilution.FixWidth = BMDilutionFixWidth;
                    NewBoxModelResultDilution.RectLength = BMDilutionRectLength;
                    NewBoxModelResultDilution.RectWidth = BMDilutionRectWidth;
                    NewBoxModelResultDilution.LeftSideLineAngle = BMDilutionLeftSideLineAngle;
                    NewBoxModelResultDilution.LeftSideLineStartLatitude = BMDilutionLeftSideLineStartLatitude;
                    NewBoxModelResultDilution.LeftSideLineStartLongitude = BMDilutionLeftSideLineStartLongitude;
                    NewBoxModelResultDilution.LastModifiedDate = DateTime.Now;
                    NewBoxModelResultDilution.ModifiedByID = 1;
                    NewBoxModelResultDilution.IsActive = true;

                    NewBoxModel.BoxModelResults.Add(NewBoxModelResultDilution);

                    BoxModelResult NewBoxModelResultNoDecayUntreated = new BoxModelResult();

                    NewBoxModelResultNoDecayUntreated.ResultType = (int)BoxModelResultType.NoDecayUntreated;
                    NewBoxModelResultNoDecayUntreated.Volume = BMNoDecayUntreatedVolume;
                    NewBoxModelResultNoDecayUntreated.Surface = BMNoDecayUntreatedSurface;
                    NewBoxModelResultNoDecayUntreated.Radius = BMNoDecayUntreatedRadius;
                    NewBoxModelResultNoDecayUntreated.LeftSideDiameterLineAngle = BMNoDecayUntreatedLeftSideDiameterLineAngle;
                    NewBoxModelResultNoDecayUntreated.CircleCenterLatitude = BMNoDecayUntreatedCircleCenterLatitude;
                    NewBoxModelResultNoDecayUntreated.CircleCenterLongitude = BMNoDecayUntreatedCircleCenterLongitude;
                    NewBoxModelResultNoDecayUntreated.FixLength = BMNoDecayUntreatedFixLength;
                    NewBoxModelResultNoDecayUntreated.FixWidth = BMNoDecayUntreatedFixWidth;
                    NewBoxModelResultNoDecayUntreated.RectLength = BMNoDecayUntreatedRectLength;
                    NewBoxModelResultNoDecayUntreated.RectWidth = BMNoDecayUntreatedRectWidth;
                    NewBoxModelResultNoDecayUntreated.LeftSideLineAngle = BMNoDecayUntreatedLeftSideLineAngle;
                    NewBoxModelResultNoDecayUntreated.LeftSideLineStartLatitude = BMNoDecayUntreatedLeftSideLineStartLatitude;
                    NewBoxModelResultNoDecayUntreated.LeftSideLineStartLongitude = BMNoDecayUntreatedLeftSideLineStartLongitude;
                    NewBoxModelResultNoDecayUntreated.LastModifiedDate = DateTime.Now;
                    NewBoxModelResultNoDecayUntreated.ModifiedByID = 1;
                    NewBoxModelResultNoDecayUntreated.IsActive = true;

                    NewBoxModel.BoxModelResults.Add(NewBoxModelResultNoDecayUntreated);


                    BoxModelResult NewBoxModelResultNoDecayPreDis = new BoxModelResult();

                    NewBoxModelResultNoDecayPreDis.ResultType = (int)BoxModelResultType.NoDecayPreDis;
                    NewBoxModelResultNoDecayPreDis.Volume = BMNoDecayPreDisVolume;
                    NewBoxModelResultNoDecayPreDis.Surface = BMNoDecayPreDisSurface;
                    NewBoxModelResultNoDecayPreDis.Radius = BMNoDecayPreDisRadius;
                    NewBoxModelResultNoDecayPreDis.LeftSideDiameterLineAngle = BMNoDecayPreDisLeftSideDiameterLineAngle;
                    NewBoxModelResultNoDecayPreDis.CircleCenterLatitude = BMNoDecayPreDisCircleCenterLatitude;
                    NewBoxModelResultNoDecayPreDis.CircleCenterLongitude = BMNoDecayPreDisCircleCenterLongitude;
                    NewBoxModelResultNoDecayPreDis.FixLength = BMNoDecayPreDisFixLength;
                    NewBoxModelResultNoDecayPreDis.FixWidth = BMNoDecayPreDisFixWidth;
                    NewBoxModelResultNoDecayPreDis.RectLength = BMNoDecayPreDisRectLength;
                    NewBoxModelResultNoDecayPreDis.RectWidth = BMNoDecayPreDisRectWidth;
                    NewBoxModelResultNoDecayPreDis.LeftSideLineAngle = BMNoDecayPreDisLeftSideLineAngle;
                    NewBoxModelResultNoDecayPreDis.LeftSideLineStartLatitude = BMNoDecayPreDisLeftSideLineStartLatitude;
                    NewBoxModelResultNoDecayPreDis.LeftSideLineStartLongitude = BMNoDecayPreDisLeftSideLineStartLongitude;
                    NewBoxModelResultNoDecayPreDis.LastModifiedDate = DateTime.Now;
                    NewBoxModelResultNoDecayPreDis.ModifiedByID = 1;
                    NewBoxModelResultNoDecayPreDis.IsActive = true;

                    NewBoxModel.BoxModelResults.Add(NewBoxModelResultNoDecayPreDis);

                    BoxModelResult NewBoxModelResultDecayUntreated = new BoxModelResult();

                    NewBoxModelResultDecayUntreated.ResultType = (int)BoxModelResultType.DecayUntreated;
                    NewBoxModelResultDecayUntreated.Volume = BMDecayUntreatedVolume;
                    NewBoxModelResultDecayUntreated.Surface = BMDecayUntreatedSurface;
                    NewBoxModelResultDecayUntreated.Radius = BMDecayUntreatedRadius;
                    NewBoxModelResultDecayUntreated.LeftSideDiameterLineAngle = BMDecayUntreatedLeftSideDiameterLineAngle;
                    NewBoxModelResultDecayUntreated.CircleCenterLatitude = BMDecayUntreatedCircleCenterLatitude;
                    NewBoxModelResultDecayUntreated.CircleCenterLongitude = BMDecayUntreatedCircleCenterLongitude;
                    NewBoxModelResultDecayUntreated.FixLength = BMDecayUntreatedFixLength;
                    NewBoxModelResultDecayUntreated.FixWidth = BMDecayUntreatedFixWidth;
                    NewBoxModelResultDecayUntreated.RectLength = BMDecayUntreatedRectLength;
                    NewBoxModelResultDecayUntreated.RectWidth = BMDecayUntreatedRectWidth;
                    NewBoxModelResultDecayUntreated.LeftSideLineAngle = BMDecayUntreatedLeftSideLineAngle;
                    NewBoxModelResultDecayUntreated.LeftSideLineStartLatitude = BMDecayUntreatedLeftSideLineStartLatitude;
                    NewBoxModelResultDecayUntreated.LeftSideLineStartLongitude = BMDecayUntreatedLeftSideLineStartLongitude;
                    NewBoxModelResultDecayUntreated.LastModifiedDate = DateTime.Now;
                    NewBoxModelResultDecayUntreated.ModifiedByID = 1;
                    NewBoxModelResultDecayUntreated.IsActive = true;

                    NewBoxModel.BoxModelResults.Add(NewBoxModelResultDecayUntreated);

                    BoxModelResult NewBoxModelResultDecayPreDis = new BoxModelResult();

                    NewBoxModelResultDecayPreDis.ResultType = (int)BoxModelResultType.DecayPreDis;
                    NewBoxModelResultDecayPreDis.Volume = BMDecayPreDisVolume;
                    NewBoxModelResultDecayPreDis.Surface = BMDecayPreDisSurface;
                    NewBoxModelResultDecayPreDis.Radius = BMDecayPreDisRadius;
                    NewBoxModelResultDecayPreDis.LeftSideDiameterLineAngle = BMDecayPreDisLeftSideDiameterLineAngle;
                    NewBoxModelResultDecayPreDis.CircleCenterLatitude = BMDecayPreDisCircleCenterLatitude;
                    NewBoxModelResultDecayPreDis.CircleCenterLongitude = BMDecayPreDisCircleCenterLongitude;
                    NewBoxModelResultDecayPreDis.FixLength = BMDecayPreDisFixLength;
                    NewBoxModelResultDecayPreDis.FixWidth = BMDecayPreDisFixWidth;
                    NewBoxModelResultDecayPreDis.RectLength = BMDecayPreDisRectLength;
                    NewBoxModelResultDecayPreDis.RectWidth = BMDecayPreDisRectWidth;
                    NewBoxModelResultDecayPreDis.LeftSideLineAngle = BMDecayPreDisLeftSideLineAngle;
                    NewBoxModelResultDecayPreDis.LeftSideLineStartLatitude = BMDecayPreDisLeftSideLineStartLatitude;
                    NewBoxModelResultDecayPreDis.LeftSideLineStartLongitude = BMDecayPreDisLeftSideLineStartLongitude;
                    NewBoxModelResultDecayPreDis.LastModifiedDate = DateTime.Now;
                    NewBoxModelResultDecayPreDis.ModifiedByID = 1;
                    NewBoxModelResultDecayPreDis.IsActive = true;

                    NewBoxModel.BoxModelResults.Add(NewBoxModelResultDecayPreDis);

                    csspItem.BoxModels.Add(NewBoxModel);

                    try
                    {
                        vpse.SaveChanges();
                        FillAfterSelect(treeViewItems.SelectedNode);
                        comboBoxStoredBoxModelScenarios.SelectedValue = NewBoxModel.BoxModelID;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            MessageBox.Show("ERROR - [" + ex.Message + "]\r\nInnerError - [" + ex.InnerException.Message + "]");
                            return;
                        }
                        else
                        {
                            MessageBox.Show("ERROR - [" + ex.Message + "]");
                            return;
                        }
                    }
                }
            }
        }
        private bool CheckIfAllIsCorrect()
        {

            if (textBoxBoxModelScenario.Text.Trim() == "")
            {
                MessageBox.Show("Please enter Scenario name");
                return false;
            }

            // doing T90
            if (!GetTextBoxDoubleOrMinus999(textBoxT90, "T90", ref BMT90, 0, 100, true))
                return false;

            // doing Temperature
            if (!GetTextBoxDoubleOrMinus999(textBoxTemperature, "Temperature", ref BMTemperature, 0, 100, true))
                return false;

            // doing Flow
            if (!GetTextBoxDoubleOrMinus999(textBoxFlow, "Flow", ref BMFlow, 0, 1000000, true))
                return false;

            // doing Flow Duration
            if (!GetTextBoxDoubleOrMinus999(textBoxFlowDuration, "Flow Duration", ref BMFlowDuration, 1, 24, true))
                return false;

            // doing Dilution
            if (!GetTextBoxDoubleOrMinus999(textBoxDilution, "Dilution", ref BMDilution, 0, 100000000, true))
                return false;

            // doing Depth
            if (!GetTextBoxDoubleOrMinus999(textBoxDepth, "Depth", ref BMDepth, 0, 1000000, true))
                return false;

            // doing DecayCoefficient
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayCoefficient, "Decay Coefficient", ref BMDecayCoefficient, 0, 1000, true))
                return false;

            // doing FCUntreated
            if (!GetTextBoxDoubleOrMinus999(textBoxFCUntreated, "FC Untreated", ref BMFCUntreated, 0, 100000000, true))
                return false;

            // doing FCPreDisinfection
            if (!GetTextBoxDoubleOrMinus999(textBoxFCPreDisinfection, "FC Pre-Disinfection", ref BMFCPreDisinfection, 0, 100000000, true))
                return false;

            // doing Concentration
            if (!GetTextBoxDoubleOrMinus999(textBoxConcentrationObjective, "Concentration", ref BMConcentrationObjective, 0, 100000000, true))
                return false;

            // dilution

            // doing dilution left side diameter line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxDiamLineAngle, "dilution left side diameter line angle", ref BMDilutionLeftSideDiameterLineAngle, -360, 360, false))
                return false;

            // doing dilution Circle center latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxCircleCenterLatitude, "dilution circle center latitude", ref BMDilutionCircleCenterLatitude, -180, 180, false))
                return false;

            // doing dilution Circle center longitute:
            if (!GetTextBoxDoubleOrMinus999(textBoxCircleCenterLongitude, "dilution circle center longitude", ref BMDilutionCircleCenterLongitude, -90, 90, false))
                return false;

            // doing dilution Left side line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxHeightLineAngle, "dilution left side line angle", ref BMDilutionLeftSideLineAngle, -360, 360, false))
                return false;

            // doing dilution Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDilutionStartLineLatitude, "dilution start line latitude", ref BMDilutionLeftSideLineStartLatitude, -180, 180, false))
                return false;

            // doing Dilution Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDilutionStartLineLongitude, "dilution start line latitude", ref BMDilutionLeftSideLineStartLongitude, -90, 90, false))
                return false;

            // no decay untreated

            // doing no decay untreated left side diameter line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedDiamLineAngle, "no decay untreated left side diameter line angle", ref BMNoDecayUntreatedLeftSideDiameterLineAngle, -360, 360, false))
                return false;

            // doing no decay untreated Circle center latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedCircleCenterLatitude, "no decay untreated circle center latitude", ref BMNoDecayUntreatedCircleCenterLatitude, -180, 180, false))
                return false;

            // doing no decay untreated Circle center longitute:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedCircleCenterLongitude, "no decay untreated circle center longitude", ref BMNoDecayUntreatedCircleCenterLongitude, -90, 90, false))
                return false;

            // doing no decay untreated Left side line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedHeightLineAngle, "no decay untreated left side line angle", ref BMNoDecayUntreatedLeftSideLineAngle, -360, 360, false))
                return false;

            // doing no decay untreated Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedStartLineLatitude, "no decay untreated start line latitude", ref BMNoDecayUntreatedLeftSideLineStartLatitude, -180, 180, false))
                return false;

            // doing NoDecayUntreated Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayUntreatedStartLineLongitude, "no decay untreated start line latitude", ref BMNoDecayUntreatedLeftSideLineStartLongitude, -90, 90, false))
                return false;

            // no decay pre-disinfection

            // doing no decay pre-disinfection left side diameter line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisDiamLineAngle, "no decay pre-disinfection left side diameter line angle", ref BMNoDecayPreDisLeftSideDiameterLineAngle, -360, 360, false))
                return false;

            // doing no decay pre-disinfection Circle center latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisCircleCenterLatitude, "no decay pre-disinfection circle center latitude", ref BMNoDecayPreDisCircleCenterLatitude, -180, 180, false))
                return false;

            // doing no decay pre-disinfection Circle center longitute:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisCircleCenterLongitude, "no decay pre-disinfection circle center longitude", ref BMNoDecayPreDisCircleCenterLongitude, -90, 90, false))
                return false;

            // doing no decay pre-disinfection Left side line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisHeightLineAngle, "no decay pre-disinfection left side line angle", ref BMNoDecayPreDisLeftSideLineAngle, -360, 360, false))
                return false;

            // doing no decay pre-disinfection Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisStartLineLatitude, "no decay pre-disinfection start line latitude", ref BMNoDecayPreDisLeftSideLineStartLatitude, -180, 180, false))
                return false;

            // doing no decay pre-disinfection Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxNoDecayPreDisStartLineLongitude, "no decay pre-disinfection start line latitude", ref BMNoDecayPreDisLeftSideLineStartLongitude, -90, 90, false))
                return false;

            // decay untreated

            // doing decay untreated left side diameter line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedDiamLineAngle, "decay untreated left side diameter line angle", ref BMDecayUntreatedLeftSideDiameterLineAngle, -360, 360, false))
                return false;

            // doing decay untreated Circle center latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedCircleCenterLatitude, "decay untreated circle center latitude", ref BMDecayUntreatedCircleCenterLatitude, -180, 180, false))
                return false;

            // doing decay untreated Circle center longitute:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedCircleCenterLongitude, "decay untreated circle center longitude", ref BMDecayUntreatedCircleCenterLongitude, -90, 90, false))
                return false;

            // doing decay untreated Left side line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedHeightLineAngle, "decay untreated left side line angle", ref BMDecayUntreatedLeftSideLineAngle, -360, 360, false))
                return false;

            // doing decay untreated Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedStartLineLatitude, "decay untreated start line latitude", ref BMDecayUntreatedLeftSideLineStartLatitude, -180, 180, false))
                return false;

            // doing decay untreated Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayUntreatedStartLineLongitude, "decay untreated start line latitude", ref BMDecayUntreatedLeftSideLineStartLongitude, -90, 90, false))
                return false;

            // decay pre-disinfection

            // doing decay pre-disinfection left side diameter line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisDiamLineAngle, "decay pre-disinfection left side diameter line angle", ref BMDecayPreDisLeftSideDiameterLineAngle, -360, 360, false))
                return false;

            // doing decay pre-disinfection Circle center latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisCircleCenterLatitude, "decay pre-disinfection circle center latitude", ref BMDecayPreDisCircleCenterLatitude, -180, 180, false))
                return false;

            // doing decay pre-disinfection Circle center longitute:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisCircleCenterLongitude, "decay pre-disinfection circle center longitude", ref BMDecayPreDisCircleCenterLongitude, -90, 90, false))
                return false;

            // doing decay pre-disinfection Left side line angle
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisHeightLineAngle, "decay pre-disinfection left side line angle", ref BMDecayPreDisLeftSideLineAngle, -360, 360, false))
                return false;

            // doing decay pre-disinfection Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisStartLineLatitude, "decay pre-disinfection start line latitude", ref BMDecayPreDisLeftSideLineStartLatitude, -180, 180, false))
                return false;

            // doing decay pre-disinfection Start line latitude:
            if (!GetTextBoxDoubleOrMinus999(textBoxDecayPreDisStartLineLongitude, "decay pre-disinfection start line latitude", ref BMDecayPreDisLeftSideLineStartLongitude, -90, 90, false))
                return false;

            if (BMConcentrationObjective > BMFCPreDisinfection || BMConcentrationObjective > BMFCUntreated)
            {
                MessageBox.Show("Concentration objective needs to be lower than the FC Untreated and FC Pre-Disinfection");
                return false;
            }

            return true; // everything is ok
        }
        private bool GetTextBoxDoubleOrMinus999(TextBox textBoxToCheck, string Variable, ref double DoubleToFill, double LowRange, double HighRange, bool Required)
        {
            if (textBoxToCheck.Text.Trim() == "")
            {
                if (Required)
                {
                    MessageBox.Show("Please enter " + Variable + ".");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
                else
                {
                    DoubleToFill = -999;
                    return true;
                }
            }

            if (!double.TryParse(textBoxToCheck.Text.Trim(), out DoubleToFill))
            {
                MessageBox.Show("Please enter a valid " + Variable + ".");
                textBoxToCheck.Focus();
                textBoxToCheck.SelectAll();
                return false;
            }

            if (DoubleToFill < LowRange || DoubleToFill > HighRange)
            {
                if (DoubleToFill != -999)
                {
                    MessageBox.Show("Please enter a valid " + Variable + " in the range between [" + LowRange.ToString() + "] and [" + HighRange.ToString() + "].");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
            }

            return true;
        }
        private bool GetTextBoxIntOrMinus999(TextBox textBoxToCheck, string Variable, ref int IntToFill, int LowRange, int HighRange, bool Required)
        {
            if (textBoxToCheck.Text.Trim() == "")
            {
                if (Required)
                {
                    MessageBox.Show("Please enter " + Variable + ".");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
                else
                {
                    IntToFill = -999;
                    return true;
                }
            }

            if (!int.TryParse(textBoxToCheck.Text.Trim(), out IntToFill))
            {
                MessageBox.Show("Please enter a valid " + Variable + ".");
                textBoxToCheck.Focus();
                textBoxToCheck.SelectAll();
                return false;
            }

            if (IntToFill < LowRange || IntToFill > HighRange)
            {
                if (IntToFill != -999)
                {
                    MessageBox.Show("Please enter a valid " + Variable + " in the range between [" + LowRange.ToString() + "] and [" + HighRange.ToString() + "].");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
            }

            return true;
        }
        private bool GetTextBoxString(TextBox textBoxToCheck, string Variable, ref string StringToFill, int MaxLength, bool Required)
        {
            if (textBoxToCheck.Text.Trim() == "")
            {
                if (Required)
                {
                    MessageBox.Show("Please enter " + Variable + ".");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
            }

            if (textBoxToCheck.Text.Trim().Length > MaxLength)
            {
                MessageBox.Show("Text too long: " + Variable + ".");
                textBoxToCheck.Focus();
                textBoxToCheck.SelectAll();
                return false;
            }

            StringToFill = textBoxToCheck.Text.Trim();

            return true;
        }
        private bool GetRichTextBoxString(RichTextBox richTextBoxToCheck, string Variable, ref string StringToFill, int MaxLength, bool Required)
        {
            string StringToCheck = richTextBoxToCheck.Text.ToString().Trim();
            if (StringToCheck == "")
            {
                if (Required)
                {
                    MessageBox.Show("Please enter " + Variable + ".");
                    richTextBoxToCheck.Focus();
                    richTextBoxToCheck.SelectAll();
                    return false;
                }
            }

            if (StringToCheck.Length > MaxLength)
            {
                MessageBox.Show("Text too long: " + Variable + ".");
                richTextBoxToCheck.Focus();
                richTextBoxToCheck.SelectAll();
                return false;
            }

            StringToFill = StringToCheck;

            return true;
        }
        private bool GetTextBoxDateTime(TextBox textBoxToCheck, string Variable, ref DateTime DateTimeToFill, bool Required)
        {
            if (textBoxToCheck.Text.Trim() == "")
            {
                if (Required)
                {
                    MessageBox.Show("Please enter " + Variable + ".");
                    textBoxToCheck.Focus();
                    textBoxToCheck.SelectAll();
                    return false;
                }
                else
                {
                    DateTimeToFill = new DateTime(1000, 1, 1);
                }
            }
            else if (!DateTime.TryParse(textBoxToCheck.Text.Trim(), out DateTimeToFill))
            {
                MessageBox.Show("Please enter a valid " + Variable + ".");
                textBoxToCheck.Focus();
                textBoxToCheck.SelectAll();
                return false;
            }
            else
            {
                // nothing
            }

            return true;
        }
        private void RecalculateBoxModelResults()
        {
            if (!CheckIfAllIsCorrect())
                return;

            if (!CalculateDilution())
                return;

            if (!CalculateNoDecayUntreated())
                return;

            if (!CalculateNoDecayPreDis())
                return;

            if (!CalculateDecayUntreated())
                return;

            if (!CalculateDecayPreDis())
                return;

            butRecalculate.Enabled = false;
            butSaveBoxModelDataAndResult.Enabled = true;

        }
        private bool CalculateDecayPreDis()
        {
            BMDecayPreDisVolume = (BMFlow * (BMFlowDuration / 24) * (BMFCPreDisinfection - BMConcentrationObjective)) / BMDecayCoefficient / BMConcentrationObjective;
            textBoxDecayPreDisVolume.Text = BMDecayPreDisVolume.ToString("F0");
            BMDecayPreDisSurface = BMDecayPreDisVolume / BMDepth;
            textBoxDecayPreDisSurface.Text = BMDecayPreDisSurface.ToString("F0");

            BMDecayPreDisRadius = Math.Sqrt(2 * (BMDecayPreDisVolume) / BMDepth / Math.PI);
            textBoxDecayPreDisRadius.Text = BMDecayPreDisRadius.ToString("F0");

            if (radioButtonDecayPreDisSquare.Checked == true)
            {
                BMDecayPreDisFixLength = false;
                BMDecayPreDisFixWidth = false;

                BMDecayPreDisRectLength = Math.Sqrt(BMDecayPreDisVolume / BMDepth);
                BMDecayPreDisRectWidth = BMDecayPreDisRectLength;
                textBoxDecayPreDisRectLength.Text = BMDecayPreDisRectLength.ToString("F0");
                textBoxDecayPreDisRectWidth.Text = textBoxDecayPreDisRectLength.Text;
            }
            else if (radioButtonDecayPreDisFixLength.Checked == true)
            {
                BMDecayPreDisFixLength = true;
                BMDecayPreDisFixWidth = false;

                if (!double.TryParse(textBoxDecayPreDisRectLength.Text, out BMDecayPreDisRectLength))
                {
                    MessageBox.Show("Please enter a valid decay pre-disinfection height.");
                    textBoxDecayPreDisRectLength.Focus();
                    return false;
                }
                BMDecayPreDisRectWidth = BMDecayPreDisVolume / (BMDepth * BMDecayPreDisRectLength);
                textBoxDecayPreDisRectWidth.Text = BMDecayPreDisRectWidth.ToString("F0");
            }
            else if (radioButtonDecayPreDisFixWidth.Checked == true)
            {
                BMDecayPreDisFixLength = false;
                BMDecayPreDisFixWidth = true;

                if (!double.TryParse(textBoxDecayPreDisRectWidth.Text, out BMDecayPreDisRectWidth))
                {
                    MessageBox.Show("Please enter a valid decay pre-disinfection width.");
                    textBoxDecayPreDisRectWidth.Focus();
                    return false;
                }
                BMDecayPreDisRectLength = BMDecayPreDisVolume / (BMDepth * BMDecayPreDisRectWidth);
                textBoxDecayPreDisRectLength.Text = BMDecayPreDisRectLength.ToString("F0");
            }
            else
            {
                MessageBox.Show("Error while recalculation.");
                return false;
            }

            return true;
        }
        private bool CalculateDecayUntreated()
        {
            BMDecayUntreatedVolume = (BMFlow * (BMFlowDuration / 24) * (BMFCUntreated - BMConcentrationObjective)) / BMDecayCoefficient / BMConcentrationObjective;
            textBoxDecayUntreatedVolume.Text = BMDecayUntreatedVolume.ToString("F0");
            BMDecayUntreatedSurface = BMDecayUntreatedVolume / BMDepth;
            textBoxDecayUntreatedSurface.Text = BMDecayUntreatedSurface.ToString("F0");

            BMDecayUntreatedRadius = Math.Sqrt(2 * (BMDecayUntreatedVolume) / BMDepth / Math.PI);
            textBoxDecayUntreatedRadius.Text = BMDecayUntreatedRadius.ToString("F0");

            if (radioButtonDecayUntreatedSquare.Checked == true)
            {
                BMDecayUntreatedFixLength = false;
                BMDecayUntreatedFixWidth = false;

                BMDecayUntreatedRectLength = Math.Sqrt(BMDecayUntreatedVolume / BMDepth);
                BMDecayUntreatedRectWidth = BMDecayUntreatedRectLength;
                textBoxDecayUntreatedRectLength.Text = BMDecayUntreatedRectLength.ToString("F0");
                textBoxDecayUntreatedRectWidth.Text = textBoxDecayUntreatedRectLength.Text;
            }
            else if (radioButtonDecayUntreatedFixLength.Checked == true)
            {
                BMDecayUntreatedFixLength = true;
                BMDecayUntreatedFixWidth = false;

                if (!double.TryParse(textBoxDecayUntreatedRectLength.Text, out BMDecayUntreatedRectLength))
                {
                    MessageBox.Show("Please enter a valid decay untreated height.");
                    textBoxDecayUntreatedRectLength.Focus();
                    return false;
                }
                BMDecayUntreatedRectWidth = BMDecayUntreatedVolume / (BMDepth * BMDecayUntreatedRectLength);
                textBoxDecayUntreatedRectWidth.Text = BMDecayUntreatedRectWidth.ToString("F0");
            }
            else if (radioButtonDecayUntreatedFixWidth.Checked == true)
            {
                BMDecayUntreatedFixLength = false;
                BMDecayUntreatedFixWidth = true;

                if (!double.TryParse(textBoxDecayUntreatedRectWidth.Text, out BMDecayUntreatedRectWidth))
                {
                    MessageBox.Show("Please enter a valid decay untreated width.");
                    textBoxDecayUntreatedRectWidth.Focus();
                    return false;
                }
                BMDecayUntreatedRectLength = BMDecayUntreatedVolume / (BMDepth * BMDecayUntreatedRectWidth);
                textBoxDecayUntreatedRectLength.Text = BMDecayUntreatedRectLength.ToString("F0");
            }
            else
            {
                MessageBox.Show("Error while recalculation.");
                return false;
            }

            return true;
        }
        private bool CalculateNoDecayPreDis()
        {
            BMNoDecayPreDisVolume = (BMFlow * (BMFlowDuration / 24) * BMFCPreDisinfection) / BMConcentrationObjective;
            textBoxNoDecayPreDisVolume.Text = BMNoDecayPreDisVolume.ToString("F0");
            BMNoDecayPreDisSurface = BMNoDecayPreDisVolume / BMDepth;
            textBoxNoDecayPreDisSurface.Text = BMNoDecayPreDisSurface.ToString("F0");

            BMNoDecayPreDisRadius = Math.Sqrt(2 * (BMNoDecayPreDisVolume) / BMDepth / Math.PI);
            textBoxNoDecayPreDisRadius.Text = BMNoDecayPreDisRadius.ToString("F0");

            if (radioButtonNoDecayPreDisSquare.Checked == true)
            {
                BMNoDecayPreDisFixLength = false;
                BMNoDecayPreDisFixWidth = false;

                BMNoDecayPreDisRectLength = Math.Sqrt(BMNoDecayPreDisVolume / BMDepth);
                BMNoDecayPreDisRectWidth = BMNoDecayPreDisRectLength;
                textBoxNoDecayPreDisRectLength.Text = BMNoDecayPreDisRectLength.ToString("F0");
                textBoxNoDecayPreDisRectWidth.Text = textBoxNoDecayPreDisRectLength.Text;
            }
            else if (radioButtonNoDecayPreDisFixLength.Checked == true)
            {
                BMNoDecayPreDisFixLength = true;
                BMNoDecayPreDisFixWidth = false;

                if (!double.TryParse(textBoxNoDecayPreDisRectLength.Text, out BMNoDecayPreDisRectLength))
                {
                    MessageBox.Show("Please enter a valid no decay pre-disinfection height.");
                    textBoxNoDecayPreDisRectLength.Focus();
                    return false;
                }
                BMNoDecayPreDisRectWidth = BMNoDecayPreDisVolume / (BMDepth * BMNoDecayPreDisRectLength);
                textBoxNoDecayPreDisRectWidth.Text = BMNoDecayPreDisRectWidth.ToString("F0");
            }
            else if (radioButtonNoDecayPreDisFixWidth.Checked == true)
            {
                BMNoDecayPreDisFixLength = false;
                BMNoDecayPreDisFixWidth = true;

                if (!double.TryParse(textBoxNoDecayPreDisRectWidth.Text, out BMNoDecayPreDisRectWidth))
                {
                    MessageBox.Show("Please enter a valid no decay pre-disinfection width.");
                    textBoxNoDecayPreDisRectWidth.Focus();
                    return false;
                }
                BMNoDecayPreDisRectLength = BMNoDecayPreDisVolume / (BMDepth * BMNoDecayPreDisRectWidth);
                textBoxNoDecayPreDisRectLength.Text = BMNoDecayPreDisRectLength.ToString("F0");
            }
            else
            {
                MessageBox.Show("Error while recalculation.");
                return false;
            }

            return true;
        }
        private bool CalculateNoDecayUntreated()
        {
            BMNoDecayUntreatedVolume = (BMFlow * (BMFlowDuration / 24) * BMFCUntreated) / BMConcentrationObjective;
            textBoxNoDecayUntreatedVolume.Text = BMNoDecayUntreatedVolume.ToString("F0");
            BMNoDecayUntreatedSurface = BMNoDecayUntreatedVolume / BMDepth;
            textBoxNoDecayUntreatedSurface.Text = BMNoDecayUntreatedSurface.ToString("F0");

            BMNoDecayUntreatedRadius = Math.Sqrt(2 * (BMNoDecayUntreatedVolume) / BMDepth / Math.PI);
            textBoxNoDecayUntreatedRadius.Text = BMNoDecayUntreatedRadius.ToString("F0");

            if (radioButtonNoDecayUntreatedSquare.Checked == true)
            {
                BMNoDecayUntreatedFixLength = false;
                BMNoDecayUntreatedFixWidth = false;

                BMNoDecayUntreatedRectLength = Math.Sqrt(BMNoDecayUntreatedVolume / BMDepth);
                BMNoDecayUntreatedRectWidth = BMNoDecayUntreatedRectLength;
                textBoxNoDecayUntreatedRectLength.Text = BMNoDecayUntreatedRectLength.ToString("F0");
                textBoxNoDecayUntreatedRectWidth.Text = textBoxNoDecayUntreatedRectLength.Text;
            }
            else if (radioButtonNoDecayUntreatedFixLength.Checked == true)
            {
                BMNoDecayUntreatedFixLength = true;
                BMNoDecayUntreatedFixWidth = false;

                if (!double.TryParse(textBoxNoDecayUntreatedRectLength.Text, out BMNoDecayUntreatedRectLength))
                {
                    MessageBox.Show("Please enter a valid no decay untreated height.");
                    textBoxNoDecayUntreatedRectLength.Focus();
                    return false;
                }
                BMNoDecayUntreatedRectWidth = BMNoDecayUntreatedVolume / (BMDepth * BMNoDecayUntreatedRectLength);
                textBoxNoDecayUntreatedRectWidth.Text = BMNoDecayUntreatedRectWidth.ToString("F0");
            }
            else if (radioButtonNoDecayUntreatedFixWidth.Checked == true)
            {
                BMNoDecayUntreatedFixLength = false;
                BMNoDecayUntreatedFixWidth = true;

                if (!double.TryParse(textBoxNoDecayUntreatedRectWidth.Text, out BMNoDecayUntreatedRectWidth))
                {
                    MessageBox.Show("Please enter a valid no decay untreated width.");
                    textBoxNoDecayUntreatedRectWidth.Focus();
                    return false;
                }
                BMNoDecayUntreatedRectLength = BMNoDecayUntreatedVolume / (BMDepth * BMNoDecayUntreatedRectWidth);
                textBoxNoDecayUntreatedRectLength.Text = BMNoDecayUntreatedRectLength.ToString("F0");
            }
            else
            {
                MessageBox.Show("Error while recalculation.");
                return false;
            }

            return true;
        }
        private bool CalculateDilution()
        {
            BMDilutionVolume = (BMFlow * (BMFlowDuration / 24) * BMDilution);
            textBoxCalVolume.Text = BMDilutionVolume.ToString("F0");
            BMDilutionSurface = BMDilutionVolume / BMDepth;
            textBoxCalSurface.Text = BMDilutionSurface.ToString("F0");
            BMDilutionRadius = Math.Sqrt(2 * (BMDilutionVolume) / BMDepth / Math.PI);
            textBoxCalRadius.Text = BMDilutionRadius.ToString("F0");

            if (radioButtonSquareDilution.Checked == true)
            {
                BMDilutionFixLength = false;
                BMDilutionFixWidth = false;
                BMDilutionRectLength = Math.Sqrt(BMDilutionVolume / BMDepth);
                BMDilutionRectWidth = BMDilutionRectLength;
                textBoxCalHeight.Text = BMDilutionRectLength.ToString("F0");
                textBoxCalWidth.Text = textBoxCalHeight.Text;
            }
            else if (radioButtonFixLengthDilution.Checked == true)
            {
                BMDilutionFixLength = true;
                BMDilutionFixWidth = false;

                if (!double.TryParse(textBoxCalHeight.Text, out BMDilutionRectLength))
                {
                    MessageBox.Show("Please enter a valid dilution height.");
                    textBoxCalHeight.Focus();
                    return false;
                }
                BMDilutionRectWidth = BMDilutionVolume / (BMDepth * BMDilutionRectLength);
                textBoxCalWidth.Text = BMDilutionRectWidth.ToString("F0");
            }
            else if (radioButtonFixWidthDilution.Checked == true)
            {
                BMDilutionFixLength = false;
                BMDilutionFixWidth = true;

                if (!double.TryParse(textBoxCalWidth.Text, out BMDilutionRectWidth))
                {
                    MessageBox.Show("Please enter a valid dilution width.");
                    textBoxCalWidth.Focus();
                    return false;
                }
                BMDilutionRectLength = BMDilutionVolume / (BMDepth * BMDilutionRectWidth);
                textBoxCalHeight.Text = BMDilutionRectLength.ToString("F0");
            }
            else
            {
                MessageBox.Show("Error while recalculation.");
                return false;
            }

            return true;
        }
        private void FillBoxModelPanel(int boxModelID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            BoxModel boxModelSelected = (from b in vpse.BoxModels
                                         where b.BoxModelID == boxModelID
                                         select b).FirstOrDefault();

            if (boxModelSelected != null)
            {
                ClearAllTextBoxOfBoxModelPanel();

                textBoxBoxModelScenario.Text = boxModelSelected.ScenarioName;
                textBoxFlow.Text = string.Format("{0:F0}", boxModelSelected.Flow);
                textBoxDepth.Text = string.Format("{0:F1}", boxModelSelected.Depth);
                textBoxTemperature.Text = string.Format("{0:F0}", boxModelSelected.Temperature);
                textBoxDilution.Text = string.Format("{0:F0}", boxModelSelected.Dilution);
                textBoxDecayCoefficient.Text = string.Format("{0:F4}", boxModelSelected.DecayRate);
                textBoxFCUntreated.Text = string.Format("{0:F0}", boxModelSelected.FCUntreated);
                textBoxFCPreDisinfection.Text = string.Format("{0:F0}", boxModelSelected.FCPreDisinfection);
                textBoxConcentrationObjective.Text = string.Format("{0:F0}", boxModelSelected.Concentration);
                textBoxT90.Text = string.Format("{0:F1}", boxModelSelected.T90);
                textBoxFlowDuration.Text = string.Format("{0:F1}", boxModelSelected.FlowDuration);

                // Dilution

                List<BoxModelResult> boxModelResults = (from bm in vpse.BoxModelResults
                                                        where bm.BoxModelID == boxModelID
                                                        select bm).ToList<BoxModelResult>();

                if (boxModelResults == null)
                {
                    MessageBox.Show("ERROR - could not find BoxModelResults with ID = [" +
                        boxModelID + "]");
                    return;
                }

                if (boxModelResults.Count != 5)
                {
                    MessageBox.Show("ERROR - Did not find the right number of BoxModelResults with ID = [" +
                        boxModelID + "]. Only found [" + boxModelResults.Count + "]");
                    return;
                }

                foreach (BoxModelResult bmr in boxModelResults)
                {
                    switch ((BoxModelResultType)bmr.ResultType)
                    {
                        case BoxModelResultType.Dilution:
                            textBoxCalVolume.Text = string.Format("{0:F0}", bmr.Volume);
                            textBoxCalSurface.Text = string.Format("{0:F0}", bmr.Surface);
                            textBoxCalRadius.Text = string.Format("{0:F0}", bmr.Radius);
                            textBoxDiamLineAngle.Text = bmr.LeftSideDiameterLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideDiameterLineAngle);
                            textBoxCircleCenterLatitude.Text = bmr.CircleCenterLatitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLatitude);
                            textBoxCircleCenterLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLongitude);
                            if (bmr.FixLength == true)
                            {
                                radioButtonFixLengthDilution.Checked = true;
                            }
                            else if (bmr.FixWidth == true)
                            {
                                radioButtonFixWidthDilution.Checked = true;
                            }
                            else
                            {
                                radioButtonSquareDilution.Checked = true;
                            }
                            textBoxCalHeight.Text = string.Format("{0:F0}", bmr.RectLength);
                            textBoxCalWidth.Text = string.Format("{0:F0}", bmr.RectWidth);
                            textBoxHeightLineAngle.Text = bmr.LeftSideLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideLineAngle);
                            textBoxDilutionStartLineLatitude.Text = bmr.LeftSideLineStartLatitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLatitude);
                            textBoxDilutionStartLineLongitude.Text = bmr.LeftSideLineStartLongitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLongitude);
                            break;
                        case BoxModelResultType.NoDecayUntreated:
                            textBoxNoDecayUntreatedVolume.Text = string.Format("{0:F0}", bmr.Volume);
                            textBoxNoDecayUntreatedSurface.Text = string.Format("{0:F0}", bmr.Surface);
                            textBoxNoDecayUntreatedRadius.Text = string.Format("{0:F0}", bmr.Radius);
                            textBoxNoDecayUntreatedDiamLineAngle.Text = bmr.LeftSideDiameterLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideDiameterLineAngle);
                            textBoxNoDecayUntreatedCircleCenterLatitude.Text = bmr.CircleCenterLatitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLatitude);
                            textBoxNoDecayUntreatedCircleCenterLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLongitude);
                            if (bmr.FixLength == true)
                            {
                                radioButtonNoDecayUntreatedFixLength.Checked = true;
                            }
                            else if (bmr.FixWidth == true)
                            {
                                radioButtonNoDecayUntreatedFixWidth.Checked = true;
                            }
                            else
                            {
                                radioButtonNoDecayUntreatedSquare.Checked = true;
                            }
                            textBoxNoDecayUntreatedRectLength.Text = string.Format("{0:F0}", bmr.RectLength);
                            textBoxNoDecayUntreatedRectWidth.Text = string.Format("{0:F0}", bmr.RectWidth);
                            textBoxNoDecayUntreatedHeightLineAngle.Text = bmr.LeftSideLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideLineAngle);
                            textBoxNoDecayUntreatedStartLineLatitude.Text = bmr.LeftSideLineStartLatitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLatitude);
                            textBoxNoDecayUntreatedStartLineLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLongitude);
                            break;
                        case BoxModelResultType.NoDecayPreDis:
                            textBoxNoDecayPreDisVolume.Text = string.Format("{0:F0}", bmr.Volume);
                            textBoxNoDecayPreDisSurface.Text = string.Format("{0:F0}", bmr.Surface);
                            textBoxNoDecayPreDisRadius.Text = string.Format("{0:F0}", bmr.Radius);
                            textBoxNoDecayPreDisDiamLineAngle.Text = bmr.LeftSideDiameterLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideDiameterLineAngle);
                            textBoxNoDecayPreDisCircleCenterLatitude.Text = bmr.CircleCenterLatitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLatitude);
                            textBoxNoDecayPreDisCircleCenterLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLongitude);
                            if (bmr.FixLength == true)
                            {
                                radioButtonNoDecayPreDisFixLength.Checked = true;
                            }
                            else if (bmr.FixWidth == true)
                            {
                                radioButtonNoDecayPreDisFixWidth.Checked = true;
                            }
                            else
                            {
                                radioButtonNoDecayPreDisSquare.Checked = true;
                            }
                            textBoxNoDecayPreDisRectLength.Text = string.Format("{0:F0}", bmr.RectLength);
                            textBoxNoDecayPreDisRectWidth.Text = string.Format("{0:F0}", bmr.RectWidth);
                            textBoxNoDecayPreDisHeightLineAngle.Text = bmr.LeftSideLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideLineAngle);
                            textBoxNoDecayPreDisStartLineLatitude.Text = bmr.LeftSideLineStartLatitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLatitude);
                            textBoxNoDecayPreDisStartLineLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLongitude);
                            break;
                        case BoxModelResultType.DecayUntreated:
                            textBoxDecayUntreatedVolume.Text = string.Format("{0:F0}", bmr.Volume);
                            textBoxDecayUntreatedSurface.Text = string.Format("{0:F0}", bmr.Surface);
                            textBoxDecayUntreatedRadius.Text = string.Format("{0:F0}", bmr.Radius);
                            textBoxDecayUntreatedDiamLineAngle.Text = bmr.LeftSideDiameterLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideDiameterLineAngle);
                            textBoxDecayUntreatedCircleCenterLatitude.Text = bmr.CircleCenterLatitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLatitude);
                            textBoxDecayUntreatedCircleCenterLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLongitude);
                            if (bmr.FixLength == true)
                            {
                                radioButtonDecayUntreatedFixLength.Checked = true;
                            }
                            else if (bmr.FixWidth == true)
                            {
                                radioButtonDecayUntreatedFixWidth.Checked = true;
                            }
                            else
                            {
                                radioButtonDecayUntreatedSquare.Checked = true;
                            }
                            textBoxDecayUntreatedRectLength.Text = string.Format("{0:F0}", bmr.RectLength);
                            textBoxDecayUntreatedRectWidth.Text = string.Format("{0:F0}", bmr.RectWidth);
                            textBoxDecayUntreatedHeightLineAngle.Text = bmr.LeftSideLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideLineAngle);
                            textBoxDecayUntreatedStartLineLatitude.Text = bmr.LeftSideLineStartLatitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLatitude);
                            textBoxDecayUntreatedStartLineLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLongitude);
                            break;
                        case BoxModelResultType.DecayPreDis:
                            textBoxDecayPreDisVolume.Text = string.Format("{0:F0}", bmr.Volume);
                            textBoxDecayPreDisSurface.Text = string.Format("{0:F0}", bmr.Surface);
                            textBoxDecayPreDisRadius.Text = string.Format("{0:F0}", bmr.Radius);
                            textBoxDecayPreDisDiamLineAngle.Text = bmr.LeftSideDiameterLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideDiameterLineAngle);
                            textBoxDecayPreDisCircleCenterLatitude.Text = bmr.CircleCenterLatitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLatitude);
                            textBoxDecayPreDisCircleCenterLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.CircleCenterLongitude);
                            if (bmr.FixLength == true)
                            {
                                radioButtonDecayPreDisFixLength.Checked = true;
                            }
                            else if (bmr.FixWidth == true)
                            {
                                radioButtonDecayPreDisFixWidth.Checked = true;
                            }
                            else
                            {
                                radioButtonDecayPreDisSquare.Checked = true;
                            }
                            textBoxDecayPreDisRectLength.Text = string.Format("{0:F0}", bmr.RectLength);
                            textBoxDecayPreDisRectWidth.Text = string.Format("{0:F0}", bmr.RectWidth);
                            textBoxDecayPreDisHeightLineAngle.Text = bmr.LeftSideLineAngle < -998 ? "" : string.Format("{0:F0}", bmr.LeftSideLineAngle);
                            textBoxDecayPreDisStartLineLatitude.Text = bmr.LeftSideLineStartLatitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLatitude);
                            textBoxDecayPreDisStartLineLongitude.Text = bmr.CircleCenterLongitude < -998 ? "" : string.Format("{0:F6}", bmr.LeftSideLineStartLongitude);
                            break;
                        default:
                            break;
                    }


                }

            }
            butRecalculate.Enabled = false;
        }
        private void ClearAllTextBoxOfBoxModelPanel()
        {
            textBoxBoxModelScenario.Text = "";
            textBoxFlow.Text = "";
            textBoxDepth.Text = "";
            textBoxTemperature.Text = "";
            textBoxDilution.Text = "";
            textBoxDecayCoefficient.Text = "";
            textBoxFCUntreated.Text = "";
            textBoxFCPreDisinfection.Text = "";
            textBoxConcentrationObjective.Text = "";
            textBoxT90.Text = "";
            textBoxFlowDuration.Text = "";
            // Dilution
            textBoxCalVolume.Text = "";
            textBoxCalSurface.Text = "";
            textBoxCalRadius.Text = "";
            textBoxDiamLineAngle.Text = "";
            textBoxCircleCenterLatitude.Text = "";
            textBoxCircleCenterLongitude.Text = "";
            radioButtonSquareDilution.Checked = true;
            textBoxCalHeight.Text = "";
            textBoxCalWidth.Text = "";
            textBoxHeightLineAngle.Text = "";
            textBoxDilutionStartLineLatitude.Text = "";
            textBoxDilutionStartLineLongitude.Text = "";
            // NoDecayUntreated
            textBoxNoDecayUntreatedVolume.Text = "";
            textBoxNoDecayUntreatedSurface.Text = "";
            textBoxNoDecayUntreatedRadius.Text = "";
            textBoxNoDecayUntreatedDiamLineAngle.Text = "";
            textBoxNoDecayUntreatedCircleCenterLatitude.Text = "";
            textBoxNoDecayUntreatedCircleCenterLongitude.Text = "";
            radioButtonNoDecayUntreatedSquare.Checked = true;
            textBoxNoDecayUntreatedRectLength.Text = "";
            textBoxNoDecayUntreatedRectWidth.Text = "";
            textBoxNoDecayUntreatedHeightLineAngle.Text = "";
            textBoxNoDecayUntreatedStartLineLatitude.Text = "";
            textBoxNoDecayUntreatedStartLineLongitude.Text = "";
            // NoDecayPreDis
            textBoxNoDecayPreDisVolume.Text = "";
            textBoxNoDecayPreDisSurface.Text = "";
            textBoxNoDecayPreDisRadius.Text = "";
            textBoxNoDecayPreDisDiamLineAngle.Text = "";
            textBoxNoDecayPreDisCircleCenterLatitude.Text = "";
            textBoxNoDecayPreDisCircleCenterLongitude.Text = "";
            radioButtonNoDecayPreDisSquare.Checked = true;
            textBoxNoDecayPreDisRectLength.Text = "";
            textBoxNoDecayPreDisRectWidth.Text = "";
            textBoxNoDecayPreDisHeightLineAngle.Text = "";
            textBoxNoDecayPreDisStartLineLatitude.Text = "";
            textBoxNoDecayPreDisStartLineLongitude.Text = "";
            // DecayUntreated
            textBoxDecayUntreatedVolume.Text = "";
            textBoxDecayUntreatedSurface.Text = "";
            textBoxDecayUntreatedRadius.Text = "";
            textBoxDecayUntreatedDiamLineAngle.Text = "";
            textBoxDecayUntreatedCircleCenterLatitude.Text = "";
            textBoxDecayUntreatedCircleCenterLongitude.Text = "";
            radioButtonDecayUntreatedSquare.Checked = true;
            textBoxDecayUntreatedRectLength.Text = "";
            textBoxDecayUntreatedRectWidth.Text = "";
            textBoxDecayUntreatedHeightLineAngle.Text = "";
            textBoxDecayUntreatedStartLineLatitude.Text = "";
            textBoxDecayUntreatedStartLineLongitude.Text = "";
            // DecayPreDis
            textBoxDecayPreDisVolume.Text = "";
            textBoxDecayPreDisSurface.Text = "";
            textBoxDecayPreDisRadius.Text = "";
            textBoxDecayPreDisDiamLineAngle.Text = "";
            textBoxDecayPreDisCircleCenterLatitude.Text = "";
            textBoxDecayPreDisCircleCenterLongitude.Text = "";
            radioButtonDecayPreDisSquare.Checked = true;
            textBoxDecayPreDisRectLength.Text = "";
            textBoxDecayPreDisRectWidth.Text = "";
            textBoxDecayPreDisHeightLineAngle.Text = "";
            textBoxDecayPreDisStartLineLatitude.Text = "";
            textBoxDecayPreDisStartLineLongitude.Text = "";

        }
        private void DeleteBoxModelScenario()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (comboBoxStoredBoxModelScenarios.SelectedItem != null)
            {
                BoxModel boxModel = (BoxModel)comboBoxStoredBoxModelScenarios.SelectedItem;

                if (MessageBox.Show("Are you sure you want to delete the box model scenario [" + boxModel.ScenarioName +
                    "]", "Deleting [" + boxModel.ScenarioName + "]",
                    MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    BoxModel ItemToDelete = (from b in vpse.BoxModels
                                             where b.BoxModelID == boxModel.BoxModelID
                                             select b).FirstOrDefault();
                    if (ItemToDelete != null)
                    {
                        vpse.DeleteObject(ItemToDelete);
                        try
                        {
                            vpse.SaveChanges();
                            FillAfterSelect(treeViewItems.SelectedNode);
                        }
                        catch (Exception ex)
                        {
                            ShowError(ex);
                        }
                    }
                }
            }
        }
        private void textBoxBMInputChanged()
        {
            butRecalculate.Enabled = true;
            butSaveBoxModelDataAndResult.Enabled = false;
        }
        private void SaveInfrastructureInfo()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

            CSSPItem csspItemSelected = (from c in vpse.CSSPItems
                                         where c.CSSPItemID == CurrentTVI.ItemID
                                         select c).FirstOrDefault();

            if (csspItemSelected == null)
            {
                MessageBox.Show("ERROR - could not find CSSPItem with ID = [" + CurrentTVI.ItemID + "]");
                return;
            }

            string ItemType = (from c in vpse.CSSPItems
                               from ct in vpse.CSSPTypeItems
                               where c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                               && c.CSSPItemID == CurrentTVI.ItemID
                               select ct.CSSPTypeText).FirstOrDefault();

            if (!CheckIfAllInfrastructureInfoIsCorrect())
                return;

            Infrastructure Infra = (from w in vpse.Infrastructures
                                    where w.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                    select w).FirstOrDefault();

            if (Infra == null)
            {
                Infrastructure newInfra = new Infrastructure();

                newInfra.TreatmentType = StoredType;
                newInfra.Category = StoredCategory;
                if (StoredPrismID < -998)
                    newInfra.PrismID = null;
                else
                    newInfra.PrismID = StoredPrismID;
                if (StoredPrismID < -998)
                    newInfra.OutfallPrismID = null;
                else
                    newInfra.OutfallPrismID = StoredOutfallPrismID;
                newInfra.InfrastructureType = StoredInfrastructureType;
                if (checkBoxStoredActive.CheckState == CheckState.Indeterminate)
                    newInfra.IsActive = null;
                else if (checkBoxStoredActive.CheckState == CheckState.Checked)
                    newInfra.IsActive = true;
                else
                    newInfra.IsActive = false;
                if (StoredDateOfConstruction < new DateTime(1001, 1, 1))
                    newInfra.DateOfConstruction = null;
                else
                    newInfra.DateOfConstruction = StoredDateOfConstruction;
                if (StoredDateOfRecentUpgrade < new DateTime(1001, 1, 1))
                    newInfra.DateOfRecentUpgrade = null;
                else
                    newInfra.DateOfRecentUpgrade = StoredDateOfRecentUpgrade;
                newInfra.Locator = StoredLocator;
                newInfra.Datum = StoredDatum;
                if (StoredZone < -998)
                    newInfra.Zone = null;
                else
                    newInfra.Zone = StoredZone;
                if (StoredEasting < -998)
                    newInfra.Easting = null;
                else
                    newInfra.Easting = StoredEasting;
                if (StoredNorthing < -998)
                    newInfra.Northing = null;
                else
                    newInfra.Northing = StoredNorthing;
                if (StoredLatitude < -998)
                    newInfra.Latitude = null;
                else
                    newInfra.Latitude = StoredLatitude;
                if (StoredLongitude < -998)
                    newInfra.Longitude = null;
                else
                    newInfra.Longitude = StoredLongitude;
                if (StoredDesignPopulation < -998)
                    newInfra.PopDesign = null;
                else
                    newInfra.PopDesign = StoredDesignPopulation;
                if (StoredPopulationServed < -998)
                    newInfra.PopServed = null;
                else
                    newInfra.PopServed = StoredPopulationServed;
                if (StoredDesignFlow < -998)
                    newInfra.DesignFlow = null;
                else
                    newInfra.DesignFlow = StoredDesignFlow;
                if (StoredAverageFlow < -998)
                    newInfra.AverageFlow = null;
                else
                    newInfra.AverageFlow = StoredAverageFlow;
                if (StoredPeakFlow < -998)
                    newInfra.PeakFlow = null;
                else
                    newInfra.PeakFlow = StoredPeakFlow;
                if (StoredEstimatedFlow < -998)
                    newInfra.EstimatedFlow = null;
                else
                    newInfra.EstimatedFlow = StoredEstimatedFlow;
                newInfra.OperatorName = StoredOperatorName;
                newInfra.OperatorTelephone = StoredOperatorTelephone;
                newInfra.OperatorEmail = StoredOperatorEmail;
                if (StoredNumberOfVisitToPlantPerWeek < -998)
                    newInfra.NumbOfVisitToPlantPerWeek = null;
                else
                    newInfra.NumbOfVisitToPlantPerWeek = StoredNumberOfVisitToPlantPerWeek;
                newInfra.Disinfection = StoredDisinfection;
                if (StoredBODRequired < -998)
                    newInfra.BODRequired = null;
                else
                    newInfra.BODRequired = StoredBODRequired;
                if (StoredSSRequired < -998)
                    newInfra.SSRequired = null;
                else
                    newInfra.SSRequired = StoredSSRequired;
                if (StoredFCRequired < -998)
                    newInfra.FCRequired = null;
                else
                    newInfra.FCRequired = StoredFCRequired;
                if (checkBoxStoredHasAlarmSystem.CheckState == CheckState.Indeterminate)
                    newInfra.IsActive = null;
                else if (checkBoxStoredHasAlarmSystem.CheckState == CheckState.Checked)
                    newInfra.IsActive = true;
                else
                    newInfra.IsActive = false;
                newInfra.AlarmSystemTypeAndComment = StoredAlarmSystemType;
                newInfra.CollectionSystemType = StoredCollectionSystemType;
                if (StoredCombinedPercent < -998)
                    newInfra.CombinedPercent = null;
                else
                    newInfra.CombinedPercent = StoredCombinedPercent;
                if (StoredBypassFreqency < -998)
                    newInfra.BypassFrequency = null;
                else
                    newInfra.BypassFrequency = StoredBypassFreqency;
                newInfra.BypassTypeOrCause = StoredBypassTypeOrCause;
                if (StoredBypassAverageTime < -998)
                    newInfra.BypassAverageTime = null;
                else
                    newInfra.BypassAverageTime = StoredBypassAverageTime;
                if (StoredBypassNotificationTime < -998)
                    newInfra.BypassNotificationTime = null;
                else
                    newInfra.BypassNotificationTime = StoredBypassNotificationTime;
                newInfra.LagoonOrMechanical = StoredLagoonOrMachanical;
                newInfra.OutfallDatum = StoredOutfallDatum;
                if (StoredOutfallZone < -998)
                    newInfra.OutfallZone = null;
                else
                    newInfra.OutfallZone = StoredOutfallZone;
                if (StoredOutfallEasting < -998)
                    newInfra.OutfallEasting = null;
                else
                    newInfra.OutfallEasting = StoredOutfallEasting;
                if (StoredOutfallNorthing < -998)
                    newInfra.OutfallNorthing = null;
                else
                    newInfra.OutfallNorthing = StoredOutfallNorthing;
                if (StoredOutfallLatitude < -998)
                    newInfra.OutfallLatitude = null;
                else
                    newInfra.OutfallLatitude = StoredOutfallLatitude;
                if (StoredOutfallLongitude < -998)
                    newInfra.OutfallLongitude = null;
                else
                    newInfra.OutfallLongitude = StoredOutfallLongitude;
                if (StoredOutfallDepthHigh < -998)
                    newInfra.OutfallDepthHigh = null;
                else
                    newInfra.OutfallDepthHigh = StoredOutfallDepthHigh;
                if (StoredOutfallDepthLow < -998)
                    newInfra.OutfallDepthLow = null;
                else
                    newInfra.OutfallDepthLow = StoredOutfallDepthLow;
                if (StoredOutfallNumberOfPorts < -998)
                    newInfra.OutfallNumberOfPorts = null;
                else
                    newInfra.OutfallNumberOfPorts = StoredOutfallNumberOfPorts;
                if (StoredOutfallPortDiameter < -998)
                    newInfra.OutfallPortDiameter = null;
                else
                    newInfra.OutfallPortDiameter = StoredOutfallPortDiameter;
                if (StoredOutfallPortSpacing < -998)
                    newInfra.OutfallPortSpacing = null;
                else
                    newInfra.OutfallPortSpacing = StoredOutfallPortSpacing;
                if (StoredOutfallPortElevation < -998)
                    newInfra.OutfallPortElevation = null;
                else
                    newInfra.OutfallPortElevation = StoredOutfallPortElevation;
                if (StoredOutfallVerticalAngle < -998)
                    newInfra.OutfallVerticalAngle = null;
                else
                    newInfra.OutfallVerticalAngle = StoredOutfallVerticalAngle;
                if (StoredOutfallHorizontalAngle < -998)
                    newInfra.OutfallHorizontalAngle = null;
                else
                    newInfra.OutfallHorizontalAngle = StoredOutfallHorizontalAngle;
                if (StoredOutfallDecayRate < -998)
                    newInfra.OutfallDecayRate = null;
                else
                    newInfra.OutfallDecayRate = StoredOutfallDecayRate;
                if (StoredOutfallNearFieldVelocity < -998)
                    newInfra.OutfallNearFieldVelocity = null;
                else
                    newInfra.OutfallNearFieldVelocity = StoredOutfallNearFieldVelocity;
                if (StoredOutfallFarFieldVelocity < -998)
                    newInfra.OutfallFarFieldVelocity = null;
                else
                    newInfra.OutfallFarFieldVelocity = StoredOutfallFarFieldVelocity;
                if (StoredOutfallReceivingWaterSalinity < -998)
                    newInfra.OutfallReceivingWaterSalinity = null;
                else
                    newInfra.OutfallReceivingWaterSalinity = StoredOutfallReceivingWaterSalinity;
                if (StoredOutfallReceivingWaterTemperature < -998)
                    newInfra.OutfallReceivingWaterTemperature = null;
                else
                    newInfra.OutfallReceivingWaterTemperature = StoredOutfallReceivingWaterTemperature;
                if (StoredOutfallReceivingWaterFC < -998)
                    newInfra.OutfallReceivingWaterFC = null;
                else
                    newInfra.OutfallReceivingWaterFC = StoredOutfallReceivingWaterFC;
                if (StoredOutfallDistanceFromShore < -998)
                    newInfra.OutfallDistanceFromShore = null;
                else
                    newInfra.OutfallDistanceFromShore = StoredOutfallDistanceFromShore;
                newInfra.ReceivingWaterName = StoredOutfallReceivingWaterName;
                newInfra.InputDataComments = StoredInputDataComments;
                newInfra.Comments = StoredOtherComments;
                newInfra.LastModifiedDate = DateTime.Now;
                newInfra.ModifiedByID = 1;
                newInfra.IsActive = true;

                csspItemSelected.Infrastructures.Add(newInfra);
            }
            else
            {
                Infra.TreatmentType = StoredType;
                Infra.Category = StoredCategory;
                if (StoredPrismID < -998)
                    Infra.PrismID = null;
                else
                    Infra.PrismID = StoredPrismID;
                if (StoredOutfallPrismID < -998)
                    Infra.OutfallPrismID = null;
                else
                    Infra.OutfallPrismID = StoredOutfallPrismID;
                Infra.InfrastructureType = StoredInfrastructureType;
                if (checkBoxStoredActive.CheckState == CheckState.Indeterminate)
                    Infra.IsActive = null;
                else if (checkBoxStoredActive.CheckState == CheckState.Checked)
                    Infra.IsActive = true;
                else
                    Infra.IsActive = false;
                if (StoredDateOfConstruction < new DateTime(1001, 1, 1))
                    Infra.DateOfConstruction = null;
                else
                    Infra.DateOfConstruction = StoredDateOfConstruction;
                if (StoredDateOfRecentUpgrade < new DateTime(1001, 1, 1))
                    Infra.DateOfRecentUpgrade = null;
                else
                    Infra.DateOfRecentUpgrade = StoredDateOfRecentUpgrade;
                Infra.Locator = StoredLocator;
                Infra.Datum = StoredDatum;
                if (StoredZone < -998)
                    Infra.Zone = null;
                else
                    Infra.Zone = StoredZone;
                if (StoredEasting < -998)
                    Infra.Easting = null;
                else
                    Infra.Easting = StoredEasting;
                if (StoredNorthing < -998)
                    Infra.Northing = null;
                else
                    Infra.Northing = StoredNorthing;
                if (StoredLatitude < -998)
                    Infra.Latitude = null;
                else
                    Infra.Latitude = StoredLatitude;
                if (StoredLongitude < -998)
                    Infra.Longitude = null;
                else
                    Infra.Longitude = StoredLongitude;
                if (StoredDesignPopulation < -998)
                    Infra.PopDesign = null;
                else
                    Infra.PopDesign = StoredDesignPopulation;
                if (StoredPopulationServed < -998)
                    Infra.PopServed = null;
                else
                    Infra.PopServed = StoredPopulationServed;
                if (StoredDesignFlow < -998)
                    Infra.DesignFlow = null;
                else
                    Infra.DesignFlow = StoredDesignFlow;
                if (StoredAverageFlow < -998)
                    Infra.AverageFlow = null;
                else
                    Infra.AverageFlow = StoredAverageFlow;
                if (StoredPeakFlow < -998)
                    Infra.PeakFlow = null;
                else
                    Infra.PeakFlow = StoredPeakFlow;
                if (StoredEstimatedFlow < -998)
                    Infra.EstimatedFlow = null;
                else
                    Infra.EstimatedFlow = StoredEstimatedFlow;
                Infra.OperatorName = StoredOperatorName;
                Infra.OperatorTelephone = StoredOperatorTelephone;
                Infra.OperatorEmail = StoredOperatorEmail;
                if (StoredNumberOfVisitToPlantPerWeek < -998)
                    Infra.NumbOfVisitToPlantPerWeek = null;
                else
                    Infra.NumbOfVisitToPlantPerWeek = StoredNumberOfVisitToPlantPerWeek;
                Infra.Disinfection = StoredDisinfection;
                if (StoredBODRequired < -998)
                    Infra.BODRequired = null;
                else
                    Infra.BODRequired = StoredBODRequired;
                if (StoredSSRequired < -998)
                    Infra.SSRequired = null;
                else
                    Infra.SSRequired = StoredSSRequired;
                if (StoredFCRequired < -998)
                    Infra.FCRequired = null;
                else
                    Infra.FCRequired = StoredFCRequired;
                if (checkBoxStoredHasAlarmSystem.CheckState == CheckState.Indeterminate)
                    Infra.IsActive = null;
                else if (checkBoxStoredHasAlarmSystem.CheckState == CheckState.Checked)
                    Infra.IsActive = true;
                else
                    Infra.IsActive = false;
                Infra.AlarmSystemTypeAndComment = StoredAlarmSystemType;
                Infra.CollectionSystemType = StoredCollectionSystemType;
                if (StoredCombinedPercent < -998)
                    Infra.CombinedPercent = null;
                else
                    Infra.CombinedPercent = StoredCombinedPercent;
                if (StoredBypassFreqency < -998)
                    Infra.BypassFrequency = null;
                else
                    Infra.BypassFrequency = StoredBypassFreqency;
                Infra.BypassTypeOrCause = StoredBypassTypeOrCause;
                if (StoredBypassAverageTime < -998)
                    Infra.BypassAverageTime = null;
                else
                    Infra.BypassAverageTime = StoredBypassAverageTime;
                if (StoredBypassNotificationTime < -998)
                    Infra.BypassNotificationTime = null;
                else
                    Infra.BypassNotificationTime = StoredBypassNotificationTime;
                Infra.LagoonOrMechanical = StoredLagoonOrMachanical;
                Infra.OutfallDatum = StoredOutfallDatum;
                if (StoredOutfallZone < -998)
                    Infra.OutfallZone = null;
                else
                    Infra.OutfallZone = StoredOutfallZone;
                if (StoredOutfallEasting < -998)
                    Infra.OutfallEasting = null;
                else
                    Infra.OutfallEasting = StoredOutfallEasting;
                if (StoredOutfallNorthing < -998)
                    Infra.OutfallNorthing = null;
                else
                    Infra.OutfallNorthing = StoredOutfallNorthing;
                if (StoredOutfallLatitude < -998)
                    Infra.OutfallLatitude = null;
                else
                    Infra.OutfallLatitude = StoredOutfallLatitude;
                if (StoredOutfallLongitude < -998)
                    Infra.OutfallLongitude = null;
                else
                    Infra.OutfallLongitude = StoredOutfallLongitude;
                if (StoredOutfallDepthHigh < -998)
                    Infra.OutfallDepthHigh = null;
                else
                    Infra.OutfallDepthHigh = StoredOutfallDepthHigh;
                if (StoredOutfallDepthLow < -998)
                    Infra.OutfallDepthLow = null;
                else
                    Infra.OutfallDepthLow = StoredOutfallDepthLow;
                if (StoredOutfallNumberOfPorts < -998)
                    Infra.OutfallNumberOfPorts = null;
                else
                    Infra.OutfallNumberOfPorts = StoredOutfallNumberOfPorts;
                if (StoredOutfallPortDiameter < -998)
                    Infra.OutfallPortDiameter = null;
                else
                    Infra.OutfallPortDiameter = StoredOutfallPortDiameter;
                if (StoredOutfallPortSpacing < -998)
                    Infra.OutfallPortSpacing = null;
                else
                    Infra.OutfallPortSpacing = StoredOutfallPortSpacing;
                if (StoredOutfallPortElevation < -998)
                    Infra.OutfallPortElevation = null;
                else
                    Infra.OutfallPortElevation = StoredOutfallPortElevation;
                if (StoredOutfallVerticalAngle < -998)
                    Infra.OutfallVerticalAngle = null;
                else
                    Infra.OutfallVerticalAngle = StoredOutfallVerticalAngle;
                if (StoredOutfallHorizontalAngle < -998)
                    Infra.OutfallHorizontalAngle = null;
                else
                    Infra.OutfallHorizontalAngle = StoredOutfallHorizontalAngle;
                if (StoredOutfallDecayRate < -998)
                    Infra.OutfallDecayRate = null;
                else
                    Infra.OutfallDecayRate = StoredOutfallDecayRate;
                if (StoredOutfallNearFieldVelocity < -998)
                    Infra.OutfallNearFieldVelocity = null;
                else
                    Infra.OutfallNearFieldVelocity = StoredOutfallNearFieldVelocity;
                if (StoredOutfallFarFieldVelocity < -998)
                    Infra.OutfallFarFieldVelocity = null;
                else
                    Infra.OutfallFarFieldVelocity = StoredOutfallFarFieldVelocity;
                if (StoredOutfallReceivingWaterSalinity < -998)
                    Infra.OutfallReceivingWaterSalinity = null;
                else
                    Infra.OutfallReceivingWaterSalinity = StoredOutfallReceivingWaterSalinity;
                if (StoredOutfallReceivingWaterTemperature < -998)
                    Infra.OutfallReceivingWaterTemperature = null;
                else
                    Infra.OutfallReceivingWaterTemperature = StoredOutfallReceivingWaterTemperature;
                if (StoredOutfallReceivingWaterFC < -998)
                    Infra.OutfallReceivingWaterFC = null;
                else
                    Infra.OutfallReceivingWaterFC = StoredOutfallReceivingWaterFC;
                if (StoredOutfallDistanceFromShore < -998)
                    Infra.OutfallDistanceFromShore = null;
                else
                    Infra.OutfallDistanceFromShore = StoredOutfallDistanceFromShore;
                Infra.ReceivingWaterName = StoredOutfallReceivingWaterName;
                Infra.InputDataComments = StoredInputDataComments;
                Infra.Comments = StoredOtherComments;
                Infra.LastModifiedDate = DateTime.Now;
                Infra.ModifiedByID = 1;

            }

            try
            {
                vpse.SaveChanges();
                butSaveInfrastructureInfoChanges.Enabled = false;
                FillAfterSelect(treeViewItems.SelectedNode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    MessageBox.Show("ERROR - " + ex.Message);
                    return;
                }
                else
                {
                    MessageBox.Show("ERROR - " + ex.Message + "\r\nInnerException - " + ex.InnerException.Message);
                    return;
                }
            }
        }
        private bool CheckIfAllInfrastructureInfoIsCorrect()
        {

            // doing Treatment or LS type
            if (!GetTextBoxString(textBoxStoredType, "Treatment or LS type", ref StoredType, 255, false))
                return false;

            // doing PrismID
            if (!GetTextBoxIntOrMinus999(textBoxStoredPrismID, "PrismID", ref StoredPrismID, 0, 100000000, false))
                return false;

            // doing OutfallPrismID
            if (!GetTextBoxIntOrMinus999(textBoxStoredOutfallPrismID, "Outfall Prism ID", ref StoredOutfallPrismID, 0, 100000000, false))
                return false;

            // doing Category
            if (!GetTextBoxString(textBoxStoredCategory, "Category", ref StoredCategory, 100, false))
                return false;

            // doing TPID
            if (!GetTextBoxIntOrMinus999(textBoxStoredTPID, "TPID", ref StoredTPID, 0, 100000000, false))
                return false;

            // doing LSID
            if (!GetTextBoxIntOrMinus999(textBoxStoredLSID, "LSID", ref StoredLSID, 0, 100000000, false))
                return false;

            // doing SiteID
            if (!GetTextBoxIntOrMinus999(textBoxStoredSiteID, "SiteID", ref StoredSiteID, 0, 100000000, false))
                return false;

            // doing InfrastructureType
            if (!GetTextBoxString(textBoxStoredInfrastructureType, "Infrastructure type", ref StoredInfrastructureType, 100, false))
                return false;

            // doing Date of construction
            if (!GetTextBoxDateTime(textBoxStoredDateOfConstruction, "Date of construction", ref StoredDateOfConstruction, false))
                return false;

            // doing Date of recent upgrade:
            if (!GetTextBoxDateTime(textBoxStoredDateOfRecentUpgrade, "Date of recent upgrade", ref StoredDateOfRecentUpgrade, false))
                return false;

            // doing Locator
            if (!GetTextBoxString(textBoxStoredLocator, "Locator", ref StoredLocator, 255, false))
                return false;

            // doing Datum
            if (!GetTextBoxString(textBoxStoredDatum, "Datum", ref StoredDatum, 50, false))
                return false;

            // doing Zone
            if (!GetTextBoxIntOrMinus999(textBoxStoredZone, "Zone", ref StoredZone, 0, 1000, false))
                return false;

            // doing Easting
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredEasting, "Easting", ref StoredEasting, 0, 100000000, false))
                return false;

            // doing Northing
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredNorthing, "Northing", ref StoredNorthing, 0, 100000000, false))
                return false;

            // doing Latitude
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredLatitude, "Latitude", ref StoredLatitude, -90, 90, false))
                return false;

            // doing Longitude
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredLongitude, "Longitude", ref StoredLongitude, -180, 180, false))
                return false;

            // doing Design Population
            if (!GetTextBoxIntOrMinus999(textBoxStoredDesignPopulation, "Design Population", ref StoredDesignPopulation, 0, 100000000, false))
                return false;

            // doing Population served
            if (!GetTextBoxIntOrMinus999(textBoxStoredPopulationServed, "Population served", ref StoredPopulationServed, 0, 100000000, false))
                return false;

            // doing Design Flow (m3/d)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredDesignFlow, "Design Flow (m3/d)", ref StoredDesignFlow, 0, 100000000, false))
                return false;

            // doing Average Flow (m3/d)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredAverageFlow, "Average Flow (m3/d)", ref StoredAverageFlow, 0, 100000000, false))
                return false;

            // doing Peak Flow (m3/d)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredPeakFlow, "Peak Flow (m3/d)", ref StoredPeakFlow, 0, 100000000, false))
                return false;

            // doing Estimated Flow (m3/d)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredEstimatedFlow, "Estimated Flow (m3/d)", ref StoredEstimatedFlow, 0, 100000000, false))
                return false;

            // doing Operator name
            if (!GetTextBoxString(textBoxStoredOperatorName, "Operator name:", ref StoredOperatorName, 255, false))
                return false;

            // doing Operator telephone
            if (!GetTextBoxString(textBoxStoredOperatorTelephone, "Operator telephone", ref StoredOperatorTelephone, 255, false))
                return false;

            // doing Operator email
            if (!GetTextBoxString(textBoxStoredOperatorEmail, "Operator email", ref StoredOperatorEmail, 255, false))
                return false;

            // doing Number of visit to plant per week
            if (!GetTextBoxIntOrMinus999(textBoxStoredNumberOfVisitToPlantPerWeek, "Number of visit to plant per week", ref StoredNumberOfVisitToPlantPerWeek, 0, 1000, false))
                return false;

            // doing Disinfection
            if (!GetTextBoxString(textBoxStoredDisinfection, "Disinfection", ref StoredDisinfection, 255, false))
                return false;

            // doing BOD required
            if (!GetTextBoxIntOrMinus999(textBoxStoredBODRequired, "BOD required", ref StoredBODRequired, 0, 100000000, false))
                return false;

            // doing SS required
            if (!GetTextBoxIntOrMinus999(textBoxStoredSSRequired, "SS required", ref StoredSSRequired, 0, 100000000, false))
                return false;

            // doing FC required
            if (!GetTextBoxIntOrMinus999(textBoxStoredFCRequired, "FC required", ref StoredFCRequired, 0, 100000000, false))
                return false;

            // doing Alarm system type and comments
            if (!GetRichTextBoxString(richTextBoxStoredAlarmSystemType, "Alarm system type and comments", ref StoredAlarmSystemType, 50000, false))
                return false;

            // doing Collection system type
            if (!GetTextBoxString(textBoxStoredCollectionSystemType, "Collection system type", ref StoredCollectionSystemType, 50, false))
                return false;

            // doing Combined percentage
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredCombinedPercent, "Combined percentage", ref StoredCombinedPercent, 0, 100, false))
                return false;

            // doing Bypass frequency per year
            if (!GetTextBoxIntOrMinus999(textBoxStoredBypassFreqency, "Bypass frequency per year", ref StoredBypassFreqency, 0, 1000, false))
                return false;

            // doing Bypass type or cause
            if (!GetTextBoxString(textBoxStoredBypassTypeOrCause, "Bypass type or cause", ref StoredBypassTypeOrCause, 250, false))
                return false;

            // doing Bypass average time (h)
            if (!GetTextBoxIntOrMinus999(textBoxStoredBypassAverageTime, "Bypass average time (h)", ref StoredBypassAverageTime, 0, 100000000, false))
                return false;

            // doing Bypass notification time (h)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredBypassNotificationTime, "Bypass notification time (h)", ref StoredBypassNotificationTime, 0, 100000000, false))
                return false;

            // doing Lagoon or mechanical
            if (!GetTextBoxString(textBoxStoredLagoonOrMachanical, "Lagoon or mechanical", ref StoredLagoonOrMachanical, 50, false))
                return false;

            // doing Outfall easting
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallEasting, "Outfall easting", ref StoredOutfallEasting, 0, 100000000, false))
                return false;

            // doing Outfall northing:
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallNorthing, "Outfall northing", ref StoredOutfallNorthing, 0, 100000000, false))
                return false;

            // doing Outfall latitude
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallLatitude, "Outfall latitude", ref StoredOutfallLatitude, -90, 90, false))
                return false;

            // doing Outfall longitude
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallLongitude, "Outfall longitude", ref StoredOutfallLongitude, -180, 180, false))
                return false;

            // doing Outfall zone
            if (!GetTextBoxIntOrMinus999(textBoxStoredOutfallZone, "Outfall zone", ref StoredOutfallZone, 0, 1000, false))
                return false;

            // doing Outfall datum
            if (!GetTextBoxString(textBoxStoredOutfallDatum, "Outfall datum", ref StoredOutfallDatum, 50, false))
                return false;

            // doing Outfall depth high (m)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallDepthHigh, "Outfall depth high (m)", ref StoredOutfallDepthHigh, 0, 100000000, false))
                return false;

            // doing Outfall depth low (m)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallDepthLow, "Outfall depth low (m)", ref StoredOutfallDepthLow, 0, 100000000, false))
                return false;

            // doing Outfall number of ports
            if (!GetTextBoxIntOrMinus999(textBoxStoredOutfallNumberOfPorts, "Outfall number of ports", ref StoredOutfallNumberOfPorts, 0, 100000000, false))
                return false;

            // doing Outfall port diameter (m)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallPortDiameter, "Outfall port diameter (m)", ref StoredOutfallPortDiameter, 0, 100000000, false))
                return false;

            // doing Outfall port spacing (m)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallPortSpacing, "Outfall port spacing (m)", ref StoredOutfallPortSpacing, 0, 100000000, false))
                return false;

            // doing Outfall port elevation (m):
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallPortElevation, "Outfall port elevation (m)", ref StoredOutfallPortElevation, 0, 100000000, false))
                return false;

            // doing Outfall vertical angle
            if (!GetTextBoxIntOrMinus999(textBoxStoredOutfallVerticalAngle, "Outfall vertical angle", ref StoredOutfallVerticalAngle, 0, 100000000, false))
                return false;

            // doing Outfall horizontal angle
            if (!GetTextBoxIntOrMinus999(textBoxStoredOutfallHorizontalAngle, "Outfall horizontal angle", ref StoredOutfallHorizontalAngle, 0, 100000000, false))
                return false;

            // doing Outfall decay rate (/d)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallDecayRate, "Outfall decay rate (/d)", ref StoredOutfallDecayRate, 0, 100000000, false))
                return false;

            // doing Outfall near field velocity (m/s):
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallNearFieldVelocity, "Outfall near field velocity (m/s)", ref StoredOutfallNearFieldVelocity, 0, 100000000, false))
                return false;

            // doing Outfall far field velocity (m/s):
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallFarFieldVelocity, "Outfall far field velocity (m/s)", ref StoredOutfallFarFieldVelocity, 0, 100000000, false))
                return false;

            // doing Outfall receiving water salinity (psu)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallReceivingWaterSalinity, "Outfall receiving water salinity (psu)", ref StoredOutfallReceivingWaterSalinity, 0, 100000000, false))
                return false;

            // doing Outfall receiving water temperature (C):
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallReceivingWaterTemperature, "Outfall receiving water temperature (C)", ref StoredOutfallReceivingWaterTemperature, 0, 100000000, false))
                return false;

            // doing Outfall receiving water FC (MPN/100mL)
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallReceivingWaterFC, "Outfall receiving water FC (MPN/100mL)", ref StoredOutfallReceivingWaterFC, 0, 100000000, false))
                return false;

            // doing Outfall distance from shore
            if (!GetTextBoxDoubleOrMinus999(textBoxStoredOutfallDistanceFromShore, "Outfall distance from shore", ref StoredOutfallDistanceFromShore, 0, 100000000, false))
                return false;

            // doing Outfall receiving water name:
            if (!GetTextBoxString(textBoxStoredOutfallReceivingWaterName, "Outfall receiving water name", ref StoredOutfallReceivingWaterName, 100000, false))
                return false;

            // doing Input Data Comments
            if (!GetRichTextBoxString(richTextBoxStoredInputDataComments, "Input Data Comments", ref StoredInputDataComments, 100000, false))
                return false;

            // doing Other Comments
            if (!GetRichTextBoxString(richTextBoxStoredOtherComments, "Other Comments", ref StoredOtherComments, 100000, false))
                return false;

            return true;
        }
        private void FillBoxModelWithDefault()
        {
            textBoxBoxModelScenario.Text = "Conc14";
            textBoxFlow.Text = "1234";
            textBoxDepth.Text = "3";
            textBoxTemperature.Text = "10";
            textBoxDilution.Text = "1000";
            textBoxDecayCoefficient.Text = "4.6821";
            textBoxFCUntreated.Text = "3000000";
            textBoxFCPreDisinfection.Text = "800";
            textBoxConcentrationObjective.Text = "14";
            textBoxT90.Text = "6";
            textBoxFlowDuration.Text = "24";
            CalculateDecay();
        }
        private void CalculateDecay()
        {
            double T90;
            if (!double.TryParse(textBoxT90.Text, out T90))
            {
                textBoxDecayCoefficient.Text = "Error";
                return;
            }

            double T;
            if (!double.TryParse(textBoxTemperature.Text, out T))
            {
                textBoxDecayCoefficient.Text = "Error";
                return;
            }

            double K20 = Math.Log(10) / (T90 / 24);
            double KTemp = K20 * Math.Pow(1.07, (T - 20));
            textBoxDecayCoefficient.Text = KTemp.ToString("F4");
        }
        private void ChangeScenarioName()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            int ScenarioID = 0;
            Scenario ss = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

            if (ss != null)
            {
                ScenarioID = ss.ScenarioID;
            }

            Scenario SelectedScenario = (from s in vpse.Scenarios
                                         where s.ScenarioID == ScenarioID
                                         select s).FirstOrDefault();

            if (SelectedScenario != null)
            {
                SelectedScenario.ScenarioName = textBoxScenarioName.Text.Trim();
            }

            try
            {
                vpse.SaveChanges();
                ss.ScenarioName = SelectedScenario.ScenarioName;
            }
            catch (Exception ex)
            {
                string ErrMessage;
                if (ex.InnerException != null)
                {
                    ErrMessage = "ERROR - " + ex.Message + "\r\nInnerException - " + ex.InnerException.Message;
                }
                else
                {
                    ErrMessage = "ERROR - " + ex.Message;
                }
                richTextBoxStatus.AppendText("ERROR - " + ex.Message + "\r\nInnerException - " + ex.InnerException.Message);
                MessageBox.Show(ErrMessage);
            }
        }
        private void SaveCormixDetailAndParsedResults()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

            Scenario sDB = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c).FirstOrDefault<Scenario>();

            if (sDB != null)
            {
                sDB.CormixDetailResults = richTextBoxCormixDetailResults.Text;
                sDB.CormixParsedResults = richTextBoxCormixParsedResults.Text;
            }
            else
            {
                MessageBox.Show("Error while saving Cormix Detail Results");
            }

            vpse.SaveChanges();
        }
        private void GetRootTVI()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI TempTVI = (from c in vpse.CSSPItems
                           from cl in vpse.CSSPItemLanguages
                           where c.CSSPParentItem.CSSPItemID == null
                           && cl.CSSPItemID == c.CSSPItemID
                           && cl.Language == "en"
                           select new TVI { ItemID = c.CSSPItemID, ItemText = cl.CSSPItemText }).FirstOrDefault();

            if (TempTVI != null)
            {
                RootTVI = TempTVI;
            }
            else
            {
                MessageBox.Show("Error in GetRootTVI. Could not find Root item");
            }
        }
        private void ShowHidePanelCSSPItem(bool Show)
        {
            panelCSSPItemEditButons.Enabled = Show;
            panelCSSPItemInfo.Enabled = Show;
        }
        private void BlankThelblDoing()
        {
            lblDoingPortDiameter.Text = "";
            lblDoingPortElevation.Text = "";
            lblDoingVerticalAngle.Text = "";
            lblDoingHorizontalAngle.Text = "";
            lblDoingNumberOfPort.Text = "";
            lblDoingPortSpacing.Text = "";
            lblDoingAcuteMixZone.Text = "";
            lblDoingChronicMixZone.Text = "";
            lblDoingPortDepth.Text = "";
            lblDoingEffluentFlow.Text = "";
            lblDoingEffluentSalinity.Text = "";
            lblDoingEffluentTemperature.Text = "";
            lblDoingEffluentConcentration.Text = "";
            lblDoingMeasurementDepth.Text = "";
            lblDoingCurrentSpeed.Text = "";
            lblDoingCurrentDirection.Text = "";
            lblDoingAmbientSalinity.Text = "";
            lblDoingAmbientTemperature.Text = "";
            lblDoingBackgroundConcentration.Text = "";
            lblDoingPollutantDecayRate.Text = "";
            lblDoingFarFieldCurrentSpeed.Text = "";
            lblDoingFarFieldCurrentDirection.Text = "";
            lblDoingFarFieldDiffusionCoefficient.Text = "";
        }
        private void StepsChanged(object sender, EventArgs e)
        {
            RecalculateTotalScenario();
        }
        private void dataGridViewScenariosSelected()
        {
            if (dataGridViewScenarios.SelectedRows.Count > 0)
            {
                butDeleteScenario.Enabled = true;
                if (dataGridViewScenarios.SelectedRows.Count == 1)
                {
                    Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

                    if (s != null)
                    {
                        FilldataGridViewAmbient(s.ScenarioID);
                        FilldataGridViewValuedResults(s.ScenarioID);
                        FilldataGridViewSelectedResults(s.ScenarioID);
                        textBoxScenarioName.Text = s.ScenarioName;
                        if (checkBoxViewCormix.Checked == true)
                        {
                            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

                            try
                            {
                                string CSR = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c.CormixSummaryResults).FirstOrDefault<string>().ToString();
                                richTextBoxCormixSummaryResults.Text = CSR;
                            }
                            catch (Exception)
                            {
                                richTextBoxCormixSummaryResults.Text = "";
                            }
                            try
                            {
                                string CDR = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c.CormixDetailResults).FirstOrDefault<string>().ToString();
                                richTextBoxCormixDetailResults.Text = CDR;
                            }
                            catch (Exception)
                            {
                                richTextBoxCormixDetailResults.Text = "";
                            }
                            try
                            {
                                string CPR = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c.CormixParsedResults).FirstOrDefault<string>().ToString();
                                richTextBoxCormixParsedResults.Text = CPR;
                            }
                            catch (Exception)
                            {
                                richTextBoxCormixParsedResults.Text = "";
                            }

                        }
                    }
                    butGotoAutoRunWithSelectedScenario.Enabled = true;
                    butUseAsBestEstimate.Enabled = true;
                    butChangeScenarioName.Enabled = true;
                    textBoxScenarioName.Enabled = true;
                    buttonSaveCormixSummaryResults.Enabled = true;
                    buttonParseAndSaveCormixResults.Enabled = true;
                }
                else
                {
                    FilldataGridViewAmbient(0);
                    FilldataGridViewValuedResults(0);
                    FilldataGridViewSelectedResults(0);
                    richTextBoxCormixSummaryResults.Text = "";
                    richTextBoxCormixDetailResults.Text = "";
                    butGotoAutoRunWithSelectedScenario.Enabled = false;
                    butUseAsBestEstimate.Enabled = false;
                    butChangeScenarioName.Enabled = false;
                    textBoxScenarioName.Enabled = false;
                    buttonSaveCormixSummaryResults.Enabled = false;
                    buttonParseAndSaveCormixResults.Enabled = false;
                }
            }
            else
            {
                butDeleteScenario.Enabled = false;
                butGotoAutoRunWithSelectedScenario.Enabled = false;
                butUseAsBestEstimate.Enabled = false;
                butChangeScenarioName.Enabled = false;
                textBoxScenarioName.Enabled = false;
                buttonSaveCormixSummaryResults.Enabled = false;
                buttonParseAndSaveCormixResults.Enabled = false;
            }
        }
        #endregion Functions

        #region Events
        private void AutoRunVP_Load(object sender, EventArgs e)
        {
            XMLInputFileName = @"C:\Plumes\AutoRunVP\VPInputData.xml";
            lblError.Text = "";
            panelMiddle.Enabled = false;
            comboBoxInputRow.SelectedIndex = 0;
            PreviousRow = comboBoxInputRow.SelectedIndex;
            IsLoaded = true;
            butDeleteScenario.Enabled = false;
            butGotoAutoRunWithSelectedScenario.Enabled = false;
            butUseAsBestEstimate.Enabled = false;
            DesktopChildrenWindowsList = new List<WndHandleAndTitle>();
            DialogToClose = new List<CloseCaptionAndCommand>();
            FillDesktopWindowsChildrenList(false);
            GetRootTVI();
            FillComboBox(comboBoxProvinces, "Provinces", RootTVI, ItemType.Province, "en");
            panelMiddle.Dock = DockStyle.Fill;
            panelEditMunicipality.Dock = DockStyle.Fill;
            panelEditInfrastructure.Dock = DockStyle.Fill;
            panelEditSubInfrastructure.Dock = DockStyle.Fill;
            splitContainer2.Dock = DockStyle.Fill;
            panelInformation.Dock = DockStyle.Fill;
            panelBoxModel.Dock = DockStyle.Fill;
            panelVPScenarioResults.Dock = DockStyle.Fill;
            panelMike.Dock = DockStyle.Fill;
            panelMikeScenarios.Dock = DockStyle.Fill;
            panelInfrastructureOrMunicipalityNotSelected.Dock = DockStyle.Fill;
            panelCormix.Dock = DockStyle.Fill;
            panelAmbient.Dock = DockStyle.Fill;
            richTextBoxCormixDetailResults.Dock = DockStyle.Fill;
            richTextBoxCormixParsedResults.Dock = DockStyle.Fill;
            panelViewSelection.Visible = false;
            richTextBoxCormixParsedResults.Visible = false;
            richTextBoxCormixDetailResults.Visible = true;
            panelAmbient.Visible = true;
            panelCormix.Visible = false;
            panelVPScenarioResults.Visible = false;
            panelMike.Visible = false;
            panelMikeScenarios.Visible = true;
            panelInformation.Visible = false;
            panelBoxModel.Visible = false;
            panelInfrastructureOrMunicipalityNotSelected.Visible = true;
            butRecalculate.Enabled = false;
            butSaveInfrastructureInfoChanges.Enabled = false;
            BeforeReadInputDataFromXML = true;
            ReadInputDataFromXML();
            BeforeReadInputDataFromXML = false;
            TreeViewRefresh();
        }
        private void butStartVP_Click(object sender, EventArgs e)
        {
            panelMiddle.Enabled = false;
            lblError.Text = "";
            StartVP();
            if (lblError.Text == "")
            {
                panelMiddle.Enabled = true;
            }
        }
        private void comboBoxInputRow_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxInputRow.SelectedIndex != PreviousRow)
            {
                SetAmbientValues(PreviousRow + 1);
            }
            if (comboBoxInputRow.SelectedItem.ToString() != "")
            {
                BeforeReadInputDataFromXML = true;
                ReadAmbientValues(int.Parse(comboBoxInputRow.SelectedItem.ToString()));
                BeforeReadInputDataFromXML = false;
            }
            PreviousRow = comboBoxInputRow.SelectedIndex;
        }
        private void butRunVPScenarios_Click(object sender, EventArgs e)
        {
            ShowHidePanelCSSPItem(false);

            if (!CheckIfVPRunning())
            {
                MessageBox.Show("Please click on Start VP to start Visual Plumes");
                ShowHidePanelCSSPItem(true);
                return;
            }

            // checking to make sure there is an Infrastructure selected
            if (comboBoxInfrastructures.SelectedValue == null)
            {
                MessageBox.Show("Please make sure you have an infrastructure item selected");
                ShowHidePanelCSSPItem(true);
                return;
            }

            if ((int)comboBoxInfrastructures.SelectedValue <= 0)
            {
                MessageBox.Show("Please make sure you have an infrastructure item selected");
                ShowHidePanelCSSPItem(true);
                return;
            }

            if (comboBoxSubInfrastructures.SelectedValue != null)
            {
                if ((int)comboBoxSubInfrastructures.SelectedValue > 0)
                {
                    if (MessageBox.Show("You are about to run the scenario for [" +
                        ((TVI)comboBoxProvinces.SelectedItem).ItemText + "][" +
                        ((TVI)comboBoxMunicipalities.SelectedItem).ItemText + "][" +
                        ((TVI)comboBoxInfrastructures.SelectedItem).ItemText + "][" +
                        ((TVI)comboBoxSubInfrastructures.SelectedItem).ItemText + "]\r\n\r\nDo you want to continue?",
                        "Running AutoRunVP scenario", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    {
                        ShowHidePanelCSSPItem(true);
                        return;
                    }
                }
                else
                {
                    if (MessageBox.Show("You are about to run the scenario for [" +
                        ((TVI)comboBoxProvinces.SelectedItem).ItemText + "][" +
                        ((TVI)comboBoxMunicipalities.SelectedItem).ItemText + "][" +
                        ((TVI)comboBoxInfrastructures.SelectedItem).ItemText + "]\r\n\r\nDo you want to continue?",
                        "Running AutoRunVP scenario", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    {
                        ShowHidePanelCSSPItem(true);
                        return;
                    }
                }
            }

            butRunVPScenarios.Enabled = false;
            richTextBoxRawResults.Text = "";
            richTextBoxParsedResults.Text = "";
            SetDiffuserValues();
            SetAmbientValues(int.Parse(comboBoxInputRow.SelectedItem.ToString()));
            SaveInputDataToXML();
            SetAllCurrentValues();
            ScenarioRunningNumber = 0;
            lblScenariosCompletedValue.Text = ScenarioRunningNumber.ToString();
            lblScenariosCompletedValue.Refresh();
            if (!CheckIfAllTheInformationIsCorrect())
            {
                butRunVPScenarios.Enabled = true;
                return;
            }
            if (CheckIfShouldRunManyScenarios())
            {
                RunManyScenarios();
            }
            else
            {
                PleaseStopRecursiveRun = false;

                if (!CheckIfAllTheInformationIsCorrect())
                    return;

                if (!ScenarioAlreadyInDB())
                {
                    lblError.Text = "Doing Diffuser Fill Values ...";
                    DiffuserFillValues();
                    lblError.Text = "Doing Ambient Fill Values ...";
                    AmbientFillValues();
                    ExecuteScenario();
                }
                butStop.Focus();
            }
            BlankThelblDoing();
            butRunVPScenarios.Enabled = true;
            ShowHidePanelCSSPItem(true);
        }
        private void radioButtonEn_CheckedChanged(object sender, EventArgs e)
        {
            DoFillText();
        }
        private void radioButtonFR_CheckedChanged(object sender, EventArgs e)
        {
            DoFillText();
        }
        private void butCloseAutoRun_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void butStop_Click(object sender, EventArgs e)
        {
            PleaseStopRecursiveRun = true;
        }
        private void butTreeViewResultRefresh_Click(object sender, EventArgs e)
        {
            TreeViewRefresh();
            butDeleteTreeViewItem.Enabled = false;
        }
        private void treeViewResults_AfterSelect(object sender, TreeViewEventArgs e)
        {
            FillAfterSelect(e.Node);
        }
        private void dataGridViewScenarios_SelectionChanged(object sender, EventArgs e)
        {
            dataGridViewScenariosSelected();
        }
        private void butDelete_Click(object sender, EventArgs e)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (dataGridViewScenarios.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow dr in dataGridViewScenarios.SelectedRows)
                {
                    Scenario scenario = (Scenario)dr.DataBoundItem;
                    if (scenario != null)
                    {
                        var ScenarioToDelete = from s in vpse.Scenarios
                                               where s.ScenarioID == scenario.ScenarioID
                                               select s;

                        foreach (Scenario s in ScenarioToDelete)
                        {
                            vpse.DeleteObject(s);
                        }
                    }
                }

                try
                {
                    vpse.SaveChanges();
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
                FillAfterSelect(treeViewItems.SelectedNode);
            }

        }
        private void dataGridViewScenarios_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "CSSPItem":
                case "SelectedResults":
                case "ValuedResults":
                case "RawResults":
                case "ParsedResults":
                case "ScenarioDate":
                case "Ambients":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                    e.Column.Visible = false;
                    break;
                case "Municipality":
                    {
                        switch (PathElements.Count())
                        {
                            case 3:
                            case 2:
                                e.Column.Visible = false;
                                break;
                            case 1:
                                e.Column.Visible = true;
                                e.Column.DisplayIndex = 1;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "Infrastructure":
                    {
                        switch (PathElements.Count())
                        {
                            case 3:
                                e.Column.Visible = false;
                                break;
                            case 2:
                                e.Column.Visible = true;
                                e.Column.DisplayIndex = 1;
                                break;
                            case 1:
                                e.Column.Visible = true;
                                e.Column.DisplayIndex = 2;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private void dataGridViewAmbient_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "ScenarioID":
                case "Scenario":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                    e.Column.Visible = false;
                    break;
                default:
                    break;
            }
        }
        private void dataGridViewValuedResults_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "ArrayNum":
                case "ScenarioID":
                case "Scenario":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                    e.Column.Visible = false;
                    break;
                default:
                    break;
            }
        }
        private void dataGridViewSelectedResults_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "ResType":
                case "ScenarioID":
                case "Scenario":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                    e.Column.Visible = false;
                    break;
                default:
                    break;
            }
        }
        private void butGotoAutoRunWithSelectedScenario_Click(object sender, EventArgs e)
        {
            if (dataGridViewScenarios.SelectedRows.Count == 1)
            {
                Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

                if (s != null)
                {
                    TakeSelectedValueAndFillAutoRun(s.ScenarioID);
                }
            }
            else
            {
                MessageBox.Show("Please select a scenario");
            }
        }
        private void butUseAsBestEstimate_Click(object sender, EventArgs e)
        {
            if (dataGridViewScenarios.SelectedRows.Count == 1)
            {
                Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

                if (s != null)
                {
                    SetUseAsBestEstimateInDB(s.ScenarioID);
                }
            }
            else
            {
                MessageBox.Show("Please select a scenario");
            }
        }
        private void timerStopExecutionAfterOneSecond_Tick(object sender, EventArgs e)
        {
            timerStopExecutionAfterOneSecond.Stop();
            timerStopExecutionAfterOneSecond.Enabled = false;
            StopVPExecutionOfScenario();
        }
        private void timerCheckForDialogBoxToClose_Tick(object sender, EventArgs e)
        {
            timerCheckForDialogBoxToClose.Stop();
            timerCheckForDialogBoxToClose.Enabled = false;
            CloseDialogBox();
        }
        private void processPlumes_Exited(object sender, EventArgs e)
        {
            panelMiddle.Enabled = false;
        }
        private void comboBoxProvinces_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxProvinces.SelectedIndex > 0)
            {
                comboBoxMunicipalities.Enabled = true;
                butEditMunicipality.Enabled = true;
                TVI ParentTVI = (TVI)comboBoxProvinces.SelectedItem;
                FillComboBox(comboBoxMunicipalities, "Municipalities", ParentTVI, ItemType.Municipality, "en");
            }
            else
            {
                TVI ParentTVI = new TVI() { ItemID = 0, ItemText = "" };
                FillComboBox(comboBoxMunicipalities, "Municipalities", ParentTVI, ItemType.Municipality, "en");
                FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
                FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
                butEditMunicipality.Enabled = false;
                butEditInfrastructure.Enabled = false;
                butEditSubInfrastructure.Enabled = false;
                comboBoxMunicipalities.Enabled = false;
                comboBoxInfrastructures.Enabled = false;
                comboBoxSubInfrastructures.Enabled = false;
                if (panelMiddle.Visible == false)
                {
                    panelMiddle.Visible = true;
                    panelEditMunicipality.Visible = false;
                    panelEditInfrastructure.Visible = false;
                    panelEditSubInfrastructure.Visible = false;
                }
            }
        }
        private void comboBoxMunicipalities_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxMunicipalities.SelectedIndex > 0)
            {
                comboBoxInfrastructures.Enabled = true;
                butEditInfrastructure.Enabled = true;
                TVI ParentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
                FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
            }
            else
            {
                TVI ParentTVI = new TVI() { ItemID = 0, ItemText = "" };
                FillComboBox(comboBoxInfrastructures, "Infrastructures", ParentTVI, ItemType.WWTP, "en");
                FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
                comboBoxInfrastructures.Enabled = false;
                butEditInfrastructure.Enabled = false;
                butEditSubInfrastructure.Enabled = false;
                comboBoxSubInfrastructures.Enabled = false;
                if (panelEditInfrastructure.Visible == true || panelEditSubInfrastructure.Visible == true)
                {
                    panelMiddle.Visible = true;
                    panelEditMunicipality.Visible = false;
                    panelEditInfrastructure.Visible = false;
                    panelEditSubInfrastructure.Visible = false;
                }
            }
            FillpanelEditMunicipality();
        }
        private void comboBoxInfrastructures_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxInfrastructures.SelectedIndex > 0)
            {
                comboBoxSubInfrastructures.Enabled = true;
                butEditSubInfrastructure.Enabled = true;
                TVI ParentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
                FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
            }
            else
            {
                TVI ParentTVI = new TVI() { ItemID = 0, ItemText = "" };
                FillComboBox(comboBoxSubInfrastructures, "Sub Infrastructures", ParentTVI, ItemType.LiftStation, "en");
                comboBoxSubInfrastructures.Enabled = false;
                butEditSubInfrastructure.Enabled = false;
                if (panelEditSubInfrastructure.Visible == true)
                {
                    panelMiddle.Visible = true;
                    panelEditMunicipality.Visible = false;
                    panelEditInfrastructure.Visible = false;
                    panelEditSubInfrastructure.Visible = false;
                }
            }
            FillpanelEditInfrastructure();
        }
        private void comboBoxSubInfrastructures_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillpanelEditSubInfrastructure();
        }
        private void butEditMunicipality_Click(object sender, EventArgs e)
        {
            panelMiddle.Visible = false;
            panelEditMunicipality.Visible = true;
            panelEditInfrastructure.Visible = false;
            panelEditSubInfrastructure.Visible = false;
            FillpanelEditMunicipality();
        }
        private void butEditInfrastructure_Click(object sender, EventArgs e)
        {
            panelMiddle.Visible = false;
            panelEditMunicipality.Visible = false;
            panelEditInfrastructure.Visible = true;
            panelEditSubInfrastructure.Visible = false;
            FillpanelEditInfrastructure();
        }
        private void butEditSubInfrastructure_Click(object sender, EventArgs e)
        {
            panelMiddle.Visible = false;
            panelEditMunicipality.Visible = false;
            panelEditInfrastructure.Visible = false;
            panelEditSubInfrastructure.Visible = true;
            FillpanelEditSubInfrastructure();
        }
        private void butAddMunicipalityItem_Click(object sender, EventArgs e)
        {
            string ItemText = textBoxMunicipalityToEdit.Text.Trim();
            TVI ParentTVI = (TVI)comboBoxProvinces.SelectedItem;
            ItemType it = ItemType.Municipality;
            AddItem(ItemText, ParentTVI, it, "en");
        }
        private void butDeleteMunicipalityItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.Municipality;
            TVI ParentTVI = (TVI)comboBoxProvinces.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
            DeleteItem(CurrentTVI, ParentTVI, it);
        }
        private void butModifyMunicipalityItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.Municipality;
            TVI ParentTVI = (TVI)comboBoxProvinces.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
            string NewText = textBoxMunicipalityToEdit.Text.Trim();
            ModifyItem(NewText, CurrentTVI, ParentTVI, it);
        }
        private void textBoxMunicipalityToEdit_TextChanged(object sender, EventArgs e)
        {
            string TheText = textBoxMunicipalityToEdit.Text.Trim();
            if (textBoxMunicipalityToEdit.Text.Trim().Length > 0)
            {
                butAddMunicipalityItem.Enabled = true;

                foreach (TVI LSTVI in comboBoxMunicipalities.Items)
                {
                    if (LSTVI.ItemText.ToLower() == TheText.ToLower() && LSTVI.ItemID != 0)
                    {
                        butAddMunicipalityItem.Enabled = false;
                        break;
                    }
                }
            }
            else
            {
                butAddMunicipalityItem.Enabled = false;
            }
        }
        private void butAddInfrastructureItem_Click(object sender, EventArgs e)
        {
            string ItemText = textBoxInfrastructureToEdit.Text.Trim();
            TVI ParentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
            ItemType it = ItemType.WWTP;
            AddItem(ItemText, ParentTVI, it, "en");
        }
        private void butDeleteInfrastructureItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.WWTP;
            TVI ParentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
            DeleteItem(CurrentTVI, ParentTVI, it);
        }
        private void butModifyInfrastructureItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.WWTP;
            TVI ParentTVI = (TVI)comboBoxMunicipalities.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
            string NewText = textBoxInfrastructureToEdit.Text.Trim();
            ModifyItem(NewText, CurrentTVI, ParentTVI, it);
        }
        private void butFinish_Click(object sender, EventArgs e)
        {
            panelMiddle.Visible = true;
            panelEditInfrastructure.Visible = false;
            panelEditSubInfrastructure.Visible = false;
            panelEditMunicipality.Visible = false;
        }
        private void textBoxInfrastructureToEdit_TextChanged(object sender, EventArgs e)
        {
            string TheText = textBoxInfrastructureToEdit.Text.Trim();
            if (textBoxInfrastructureToEdit.Text.Trim().Length > 0)
            {
                butAddInfrastructureItem.Enabled = true;

                foreach (TVI LSTVI in comboBoxInfrastructures.Items)
                {
                    if (LSTVI.ItemText.ToLower() == TheText.ToLower() && LSTVI.ItemID != 0)
                    {
                        butAddInfrastructureItem.Enabled = false;
                        break;
                    }
                }
            }
            else
            {
                butAddInfrastructureItem.Enabled = false;
            }
        }
        private void butAddSubInfrastructureItem_Click(object sender, EventArgs e)
        {
            string ItemText = textBoxSubInfrastructureToEdit.Text.Trim();
            TVI ParentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
            ItemType it = ItemType.LiftStation;
            AddItem(ItemText, ParentTVI, it, "en");
        }
        private void butDeleteSubInfrastructureItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.LiftStation;
            TVI ParentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxSubInfrastructures.SelectedItem;
            DeleteItem(CurrentTVI, ParentTVI, it);
        }
        private void butModifySubInfrastructureItem_Click(object sender, EventArgs e)
        {
            ItemType it = ItemType.LiftStation;
            TVI ParentTVI = (TVI)comboBoxInfrastructures.SelectedItem;
            TVI CurrentTVI = (TVI)comboBoxSubInfrastructures.SelectedItem;
            string NewText = textBoxSubInfrastructureToEdit.Text.Trim();
            ModifyItem(NewText, CurrentTVI, ParentTVI, it);
        }
        private void textBoxSubInfrastructureToEdit_TextChanged(object sender, EventArgs e)
        {
            string TheText = textBoxSubInfrastructureToEdit.Text.Trim();
            if (TheText.Length > 0)
            {
                butAddSubInfrastructureItem.Enabled = true;
                foreach (TVI LSTVI in comboBoxSubInfrastructures.Items)
                {
                    if (LSTVI.ItemText.ToLower() == TheText.ToLower() && LSTVI.ItemID != 0)
                    {
                        butAddSubInfrastructureItem.Enabled = false;
                        break;
                    }
                }
            }
            else
            {
                butAddSubInfrastructureItem.Enabled = false;
            }
        }
        private void butDeleteTreeViewItem_Click(object sender, EventArgs e)
        {
            DeleteTreeViewItem();
        }
        private void radioButtonVPScenarioResults_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonVPScenarioResults.Checked == true)
            {
                panelVPScenarioResults.Visible = true;
                panelInformation.Visible = false;
                panelBoxModel.Visible = false;
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;

                if (checkBoxViewCormix.Checked == true)
                {
                    panelAmbient.Visible = false;
                    panelCormix.Visible = true;
                }
                else
                {
                    panelAmbient.Visible = true;
                    panelCormix.Visible = false;
                }

                if (treeViewItems.SelectedNode != null)
                    FillAfterSelect(treeViewItems.SelectedNode);
            }
        }
        private void radioButtonBoxModel_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonBoxModel.Checked == true)
            {
                panelVPScenarioResults.Visible = false;
                panelInformation.Visible = false;
                panelBoxModel.Visible = true;
                panelInfrastructureOrMunicipalityNotSelected.Visible = false;

                if (treeViewItems.SelectedNode != null)
                    FillAfterSelect(treeViewItems.SelectedNode);
            }
        }
        private void radioButtonInformation_CheckedChanged(object sender, EventArgs e)
        {
            panelVPScenarioResults.Visible = false;
            panelInformation.Visible = true;
            panelBoxModel.Visible = false;
            panelInfrastructureOrMunicipalityNotSelected.Visible = false;

            if (treeViewItems.SelectedNode != null)
                FillAfterSelect(treeViewItems.SelectedNode);
        }
        private void radioButtonSquareDilution_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonSquareDilution.Checked == true)
            {
                textBoxCalHeight.Enabled = false;
                textBoxCalWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonFixLengthDilution_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonFixLengthDilution.Checked == true)
            {
                textBoxCalHeight.Enabled = true;
                textBoxCalWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonFixWidthDilution_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonFixWidthDilution.Checked == true)
            {
                textBoxCalHeight.Enabled = false;
                textBoxCalWidth.Enabled = true;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayUntreatedSquare_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayUntreatedSquare.Checked == true)
            {
                textBoxNoDecayUntreatedRectLength.Enabled = false;
                textBoxNoDecayUntreatedRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayUntreatedFixLength_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayUntreatedFixLength.Checked == true)
            {
                textBoxNoDecayUntreatedRectLength.Enabled = true;
                textBoxNoDecayUntreatedRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayUntreatedFixWidth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayUntreatedFixWidth.Checked == true)
            {
                textBoxNoDecayUntreatedRectLength.Enabled = false;
                textBoxNoDecayUntreatedRectWidth.Enabled = true;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayPreDisSquare_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayPreDisSquare.Checked == true)
            {
                textBoxNoDecayPreDisRectLength.Enabled = false;
                textBoxNoDecayPreDisRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayPreDisFixLength_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayPreDisFixLength.Checked == true)
            {
                textBoxNoDecayPreDisRectLength.Enabled = true;
                textBoxNoDecayPreDisRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonNoDecayPreDisFixWidth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonNoDecayPreDisFixWidth.Checked == true)
            {
                textBoxNoDecayPreDisRectLength.Enabled = false;
                textBoxNoDecayPreDisRectWidth.Enabled = true;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayUntreatedSquare_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayUntreatedSquare.Checked == true)
            {
                textBoxDecayUntreatedRectLength.Enabled = false;
                textBoxDecayUntreatedRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayUntreatedFixLength_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayUntreatedFixLength.Checked == true)
            {
                textBoxDecayUntreatedRectLength.Enabled = true;
                textBoxDecayUntreatedRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayUntreatedFixWidth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayUntreatedFixWidth.Checked == true)
            {
                textBoxDecayUntreatedRectLength.Enabled = false;
                textBoxDecayUntreatedRectWidth.Enabled = true;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayPreDisSquare_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayPreDisSquare.Checked == true)
            {
                textBoxDecayPreDisRectLength.Enabled = false;
                textBoxDecayPreDisRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayPreDisFixLength_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayPreDisFixLength.Checked == true)
            {
                textBoxDecayPreDisRectLength.Enabled = true;
                textBoxDecayPreDisRectWidth.Enabled = false;
                textBoxBMInputChanged();
            }
        }
        private void radioButtonDecayPreDisFixWidth_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonDecayPreDisFixWidth.Checked == true)
            {
                textBoxDecayPreDisRectLength.Enabled = false;
                textBoxDecayPreDisRectWidth.Enabled = true;
                textBoxBMInputChanged();
            }
        }
        private void butSaveBoxModelDataAndResult_Click(object sender, EventArgs e)
        {
            SaveBoxModelDataAndResults();
        }
        private void butRecalculate_Click(object sender, EventArgs e)
        {
            RecalculateBoxModelResults();
        }
        private void textBoxBMInput_TextChanged(object sender, EventArgs e)
        {
            textBoxBMInputChanged();
            CalculateDecay();
        }
        private void butCopyToDilutionRect_Click(object sender, EventArgs e)
        {
            textBoxHeightLineAngle.Text = textBoxDiamLineAngle.Text.Trim();
            textBoxDilutionStartLineLatitude.Text = textBoxCircleCenterLatitude.Text.Trim();
            textBoxDilutionStartLineLongitude.Text = textBoxCircleCenterLongitude.Text.Trim();
        }
        private void butCopyToNoDecayUntreated_Click(object sender, EventArgs e)
        {
            textBoxNoDecayUntreatedDiamLineAngle.Text = textBoxDiamLineAngle.Text.Trim();
            textBoxNoDecayUntreatedCircleCenterLatitude.Text = textBoxCircleCenterLatitude.Text.Trim();
            textBoxNoDecayUntreatedCircleCenterLongitude.Text = textBoxCircleCenterLongitude.Text.Trim();
        }
        private void butCopyToNoDecayPreDis_Click(object sender, EventArgs e)
        {
            textBoxNoDecayPreDisDiamLineAngle.Text = textBoxNoDecayUntreatedDiamLineAngle.Text;
            textBoxNoDecayPreDisCircleCenterLatitude.Text = textBoxNoDecayUntreatedCircleCenterLatitude.Text.Trim();
            textBoxNoDecayPreDisCircleCenterLongitude.Text = textBoxNoDecayUntreatedCircleCenterLongitude.Text.Trim();
        }
        private void butCopyToDecayUntreated_Click(object sender, EventArgs e)
        {
            textBoxDecayUntreatedDiamLineAngle.Text = textBoxNoDecayPreDisDiamLineAngle.Text;
            textBoxDecayUntreatedCircleCenterLatitude.Text = textBoxNoDecayPreDisCircleCenterLatitude.Text.Trim();
            textBoxDecayUntreatedCircleCenterLongitude.Text = textBoxNoDecayPreDisCircleCenterLongitude.Text.Trim();
        }
        private void butCopyToDecayPreDis_Click(object sender, EventArgs e)
        {
            textBoxDecayPreDisDiamLineAngle.Text = textBoxDecayUntreatedDiamLineAngle.Text;
            textBoxDecayPreDisCircleCenterLatitude.Text = textBoxDecayUntreatedCircleCenterLatitude.Text.Trim();
            textBoxDecayPreDisCircleCenterLongitude.Text = textBoxDecayUntreatedCircleCenterLongitude.Text.Trim();
        }
        private void butCopyToNoDecayUntreatedRect_Click(object sender, EventArgs e)
        {
            textBoxNoDecayUntreatedHeightLineAngle.Text = textBoxHeightLineAngle.Text.Trim();
            textBoxNoDecayUntreatedStartLineLatitude.Text = textBoxDilutionStartLineLatitude.Text.Trim();
            textBoxNoDecayUntreatedStartLineLongitude.Text = textBoxDilutionStartLineLongitude.Text.Trim();
        }
        private void butCopyToNoDecayPreDisRect_Click(object sender, EventArgs e)
        {
            textBoxNoDecayPreDisHeightLineAngle.Text = textBoxNoDecayUntreatedHeightLineAngle.Text.Trim();
            textBoxNoDecayPreDisStartLineLatitude.Text = textBoxNoDecayUntreatedStartLineLatitude.Text.Trim();
            textBoxNoDecayPreDisStartLineLongitude.Text = textBoxNoDecayUntreatedStartLineLongitude.Text.Trim();
        }
        private void butCopyToDecayUntreatedRect_Click(object sender, EventArgs e)
        {
            textBoxDecayUntreatedHeightLineAngle.Text = textBoxNoDecayPreDisHeightLineAngle.Text.Trim();
            textBoxDecayUntreatedStartLineLatitude.Text = textBoxNoDecayPreDisStartLineLatitude.Text.Trim();
            textBoxDecayUntreatedStartLineLongitude.Text = textBoxNoDecayPreDisStartLineLongitude.Text.Trim();
        }
        private void butCopyToDecayPreDisRect_Click(object sender, EventArgs e)
        {
            textBoxDecayPreDisHeightLineAngle.Text = textBoxDecayUntreatedHeightLineAngle.Text.Trim();
            textBoxDecayPreDisStartLineLatitude.Text = textBoxDecayUntreatedStartLineLatitude.Text.Trim();
            textBoxDecayPreDisStartLineLongitude.Text = textBoxDecayUntreatedStartLineLongitude.Text.Trim();
        }
        private void comboBoxStoredBoxModelScenarios_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxStoredBoxModelScenarios.SelectedItem != null)
            {
                FillBoxModelPanel(((BoxModel)comboBoxStoredBoxModelScenarios.SelectedItem).BoxModelID);
            }
            else
            {
                ClearAllTextBoxOfBoxModelPanel();
            }
        }
        private void butDeleteBoxModelScenario_Click(object sender, EventArgs e)
        {
            DeleteBoxModelScenario();
        }
        private void butSaveInfrastructureInfoChanges_Click(object sender, EventArgs e)
        {
            SaveInfrastructureInfo();
        }
        private void butFillBoxModelWithDefault_Click(object sender, EventArgs e)
        {
            FillBoxModelWithDefault();
        }
        private void StoredInfo_Changed(object sender, EventArgs e)
        {
            butSaveInfrastructureInfoChanges.Enabled = true;
        }
        private void butChangeScenarioName_Click(object sender, EventArgs e)
        {
            ChangeScenarioName();
        }
        private void linklblWebResults_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linklblWebResults.Text);
        }
        private void textBoxConvM3PerDay_TextChanged(object sender, EventArgs e)
        {
            double M3PerDayValue = 0;
            if (!double.TryParse(textBoxConvM3PerDay.Text, out M3PerDayValue))
            {
                MessageBox.Show("Please enter a valid flow to be converted.");
                return;
            }
            else
            {
                textBoxM3PerSecondResults.Text = (M3PerDayValue / 24 / 3600).ToString("F5");
            }
        }
        private void checkBoxViewCormix_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxViewCormix.Checked == true)
            {
                panelAmbient.Visible = false;
                panelCormix.Visible = true;
            }
            else
            {
                panelAmbient.Visible = true;
                panelCormix.Visible = false;
            }

            dataGridViewScenariosSelected();

        }
        private void buttonSaveCormixSummaryResults_Click(object sender, EventArgs e)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            Scenario s = (Scenario)dataGridViewScenarios.SelectedRows[0].DataBoundItem;

            Scenario sDB = (from c in vpse.Scenarios where c.ScenarioID == s.ScenarioID select c).FirstOrDefault<Scenario>();

            if (sDB != null)
            {
                sDB.CormixSummaryResults = richTextBoxCormixSummaryResults.Text;
            }
            else
            {
                MessageBox.Show("Error while saving Cormix Summary Results");
            }

            vpse.SaveChanges();
        }
        private void buttonParseAndSaveCormixResults_Click(object sender, EventArgs e)
        {
            ParseAndSaveCormixResults();
            if (checkBoxCormixViewParsedResults.Checked != true)
            {
                checkBoxCormixViewParsedResults.Checked = true;
            }
            dataGridViewScenariosSelected();
        }
        private void checkBoxCormixViewParsedResults_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCormixViewParsedResults.Checked == true)
            {
                richTextBoxCormixParsedResults.Visible = true;
                richTextBoxCormixDetailResults.Visible = false;
            }
            else
            {
                richTextBoxCormixParsedResults.Visible = false;
                richTextBoxCormixDetailResults.Visible = true;
            }
        }

        #endregion Events

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    CSSPAppDBEntities vpse = new CSSPAppDBEntities();

        //    CSSPFile csspFile = (from cf in vpse.CSSPFiles
        //                         where cf.CSSPFileID == 1298
        //                         select cf).FirstOrDefault<CSSPFile>();

        //    if (csspFile != null)
        //    {
        //        ByteArray = File.ReadAllBytes(@"C:\CSSP\Modelling\Mike21\Quebec\Betsiamites\External Data\Cap-Chat Matane WL.dfs0");

        //        csspFile.FileContent = ByteArray;
        //        csspFile.FileSize = ByteArray.Length;
        //        csspFile.DataEndDate = new DateTime(2012, 12, 31, 0, 0, 0);

        //    }

        //    try
        //    {
        //        vpse.SaveChanges(System.Data.Objects.SaveOptions.AcceptAllChangesAfterSave);
        //    }
        //    catch (Exception ex)
        //    {
        //        int i = 34;
        //    }

        //}

     }
}

