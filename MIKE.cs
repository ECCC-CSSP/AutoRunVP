using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.Objects;
using CSSPAppModel;

namespace AutoRunVP
{
    public partial class AutoRunVP
    {
        public enum Direction
        {
            Up,
            Down
        }
        public enum KMZResultType
        {
            InputAndResult = 0,
            StudyArea = 1,
            Bathymetry = 2,
            Mesh = 3
        }
        public class PeakDifference
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public float Value { get; set; }
        }
        public class Peaks
        {
            public DateTime Date { get; set; }
            public float Value { get; set; }
        }
        public enum PurposeType
        {
            Input = 0,
            InputPol = 1,
            MikeResult = 2,
            KMZResult = 3,
            Original = 4,
            MikeScenarioOther = 5,
            MunicipalityOther = 6
        }
        public enum ScenarioStatusType
        {
            Created = 0,    // scenario has just been created
            ReadyToRun = 1, // scenario is ready to be run
            Running = 2,    // scenario being run
            Completed = 3,  // scenario ran without error
            Error = 4,      // scenario ran but has error
            Canceled = 5,     // scenario was cancelled
            Changed = 6       // scenario was saved
        }
        public enum TideType
        {
            Low,
            High
        }

        #region MIKE
        #region Functions

        private bool AddFileInDB(MikeScenario mikeScenario)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            Application.DoEvents();

//            openFileDialogFileToLoadInDB.InitialDirectory = @"C:\CSSP\Modelling\Mike21\New Brunswick\Tracadie\Tracadie\Model\Model Inputs\";

            openFileDialogFileToLoadInDB.Filter = "All Files (*.*)|*.*";
            openFileDialogFileToLoadInDB.DefaultExt = null;
            openFileDialogFileToLoadInDB.Multiselect = true;

            if (!(openFileDialogFileToLoadInDB.ShowDialog() == System.Windows.Forms.DialogResult.OK))
            {
                return true;
            }
            List<string> FileNameList = new List<string>();

            // this will add the file(s) selected in the FileNameList
            FileNameList = openFileDialogFileToLoadInDB.FileNames.ToList<string>();

            Application.DoEvents();

            richTextBoxMikePanelStatus.AppendText("Trying to save file(s) to DB: \r\n\r\n");
            Application.DoEvents();
            foreach (string f in FileNameList)
            {
                UploadOtherScenarioFilesToDB(f, mikeScenario);
            }
            Application.DoEvents();

            FillDataGridViewMikeScenairosInDB(mikeScenario.MikeScenarioID);

            return true;
        }
        private FileInfo AddFileToFileNameList(string m21fmFileName, string file_name, List<string> FileNameList)
        {
            string TheFile = "";
            if (file_name == null)
            {
                return null;
            }
            FileInfo fi = new FileInfo(m21fmFileName.Substring(0, m21fmFileName.LastIndexOf("\\") + 1));
            if (file_name.Length > 2)
            {
                TheFile = fi.FullName + file_name.Substring(1, file_name.Length - 2);
                fi = new FileInfo(TheFile);
                if (fi.Exists)
                {
                    if (!FileNameList.Contains(fi.FullName))
                    {
                        richTextBoxMikePanelStatus.AppendText(string.Format("Collecting file [" + fi.FullName + "]\r\n"));
                        FileNameList.Add(fi.FullName);
                    }
                    return fi;
                }
                else
                {
                    richTextBoxMikePanelStatus.AppendText(string.Format("------------- Could not find file [" + fi.FullName + "]\r\n"));
                    return null;
                }
            }
            return null;
        }
        private bool Addm21fmFileInDB()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            Application.DoEvents();

            openFileDialogFileToLoadInDB.InitialDirectory = @"C:\CSSP\Modelling\Mike21\Quebec\Gaspe\Model Input\";

            openFileDialogFileToLoadInDB.Filter = "Mike 21 input files (*.m21fm)|*.m21fm";
            openFileDialogFileToLoadInDB.DefaultExt = ".m21fm";

            if (!(openFileDialogFileToLoadInDB.ShowDialog() == System.Windows.Forms.DialogResult.OK))
            {
                return true;
            }
            List<string> FileNameList = new List<string>();

            // this will add the file(s) selected in the FileNameList
            FileNameList = openFileDialogFileToLoadInDB.FileNames.ToList<string>();

            Application.DoEvents();

            bool FileExist = false;

            richTextBoxMikePanelStatus.AppendText("\r\n\r\nFile [" + FileNameList[0] + "]\r\n");

            FileInfo fi = new FileInfo(FileNameList[0]);
            FileStream fs = fi.OpenRead();

            string FilePath = FileNameList[0].Substring(0, FileNameList[0].LastIndexOf("\\") + 1);
            string ShortFileName = FileNameList[0].Substring(FileNameList[0].LastIndexOf("\\") + 1);

            Application.DoEvents();

            //Read all file bytes into an array from the specified file.
            int nBytes = (int)fi.Length;
            Byte[] ByteArray = new byte[nBytes];
            int nBytesRead = fs.Read(ByteArray, 0, nBytes);

            fs.Close();

            richTextBoxMikePanelStatus.AppendText("Checking if Scenario is already in DB ...\r\n");
            Application.DoEvents();

            TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

            CSSPItem csspItem = (from ci in vpse.CSSPItems
                                 where ci.CSSPItemID == CurrentTVI.ItemID
                                 select ci).FirstOrDefault<CSSPItem>();

            if (csspItem == null)
            {
                MessageBox.Show("Error: Could not find CSSPItems with CSSPItemID = [" + CurrentTVI.ItemID + "] in the DB");
                return false;
            }

            // checking if file is already in DB
            CSSPFile csspFileExist = (from f in vpse.CSSPFiles
                                      where f.FileName == ShortFileName
                                      && f.FileOriginalPath == FilePath
                                      && f.FileType == fi.Extension
                                      && f.FileSize == fi.Length
                                      select f).FirstOrDefault<CSSPFile>();

            if (csspFileExist != null)
            {
                byte[] ByteArray2 = csspFileExist.FileContent;

                richTextBoxMikePanelStatus.AppendText("Comparing saved scenario and scenario to upload in DB ...\r\n");
                FileExist = BinIdentical(ByteArray, ByteArray2);
                if (FileExist)
                {
                    MessageBox.Show("The scenario \r\n[" + FileNameList[0] + "]\r\n already exist in DB");
                    richTextBoxMikePanelStatus.AppendText("Scenario [" + FileNameList[0] + "] already in DB ...\r\n");
                    return false;
                }
            }

            //_________________________________________________
            // Saving scenario files
            //_________________________________________________
            richTextBoxMikePanelStatus.AppendText("For scenario: [" + FileNameList[0] + "]\r\n\r\n");
            richTextBoxMikePanelStatus.AppendText("Reading and parsing ...\r\n");

            MemoryStream myMemoryStream = new MemoryStream();
            myMemoryStream = FileToMemoryStream(FileNameList[0]);

            if (!m21fm.StreamToM21fm(myMemoryStream))
            {
                MessageBox.Show("Scenario not read properly");
                return false;
            }

            richTextBoxMikePanelStatus.AppendText("Trying to create and save a new MikeScenarios in to DB: \r\n\r\n");
            Application.DoEvents();

            // here we force the user to only add a new m21fm file and associate that has only continuous pollution
            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE> kvp2 in m21fm.femEngineHD.transport_module.sources.source)
            {
                foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT> kvp3 in kvp2.Value.component)
                {
                    if (kvp3.Value.format != 0) // 0 == continuous pollution
                    {
                        MessageBox.Show("You are only alowed to upload m21fm project with continuous pollution flow.");
                        return false;
                    }
                }
            }

            // here we force the user to only add a m21fm file and associate that has only constant decay rate
            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DECAY.COMPONENT> kvp in m21fm.femEngineHD.transport_module.decay.component)
            {
                if (kvp.Value.format != 0) // 0 == constant value
                {
                    MessageBox.Show("You are only alowed to upload m21fm project with constant decay.");
                    return false;
                }
            }

            // _______________________________________
            // Creating a new MikeScenario
            // _______________________________________
            MikeScenario NewMikeScenario = new MikeScenario();
            NewMikeScenario.CSSPItem = csspItem;
            NewMikeScenario.ScenarioName = ShortFileName.Substring(0, ShortFileName.LastIndexOf("."));
            NewMikeScenario.ScenarioStatus = ScenarioStatusType.Completed.ToString();
            NewMikeScenario.ScenarioStartDateAndTime = m21fm.femEngineHD.time.start_time;
            NewMikeScenario.ScenarioEndDateAndTime = m21fm.femEngineHD.time.start_time.AddSeconds(m21fm.femEngineHD.time.time_step_interval * m21fm.femEngineHD.time.number_of_time_steps);

            string LogFileName = FileNameList[0].Substring(0, FileNameList[0].LastIndexOf(".")) + ".log";

            fi = new FileInfo(LogFileName);
            if (fi.Exists)
            {
                MemoryStream myLogMemoryStream = new MemoryStream();
                myLogMemoryStream = FileToMemoryStream(LogFileName);

                if (!m21fmLog.StreamToM21fmLog(myLogMemoryStream))
                {
                    MessageBox.Show("Scenario not read properly");
                    return false;
                }
                else
                {
                    NewMikeScenario.ScenarioStartExecutionDateAndTime = m21fmLog.StartExecutionDate;
                    NewMikeScenario.ScenarioExecutionTimeInMinutes = m21fmLog.TotalElapseTimeInSeconds / 60;
                }
            }
            else
            {
                //nothing
            }


            try
            {
                vpse.AddToMikeScenarios(NewMikeScenario);
                vpse.SaveChanges();
                richTextBoxMikePanelStatus.AppendText("New Mike Scenarios created in to DB: \r\n\r\n");
            }
            catch (Exception ex)
            {
                richTextBoxMikePanelStatus.AppendText("Error while trying to create and save a new MikeScenarios in to DB: \r\n\r\n");
                richTextBoxMikePanelStatus.AppendText("Error:" + ex.Message + "\r\n\r\n");
                return false;
            }


            richTextBoxMikePanelStatus.AppendText("Collecting all the files ...\r\n");
            richTextBoxMikePanelStatus.AppendText(string.Format("Collecting required files for scenario ... \r\n"));

            string TempFileName = FileNameList[0];

            //__________________________________________________
            // uploading Input Files
            //__________________________________________________
            if (!GetAllInputFilesToUpload(TempFileName, FileNameList, NewMikeScenario))
            {
                MessageBox.Show("Error in GetAllInputFilesToUpload");
                richTextBoxMikePanelStatus.AppendText("Error while collecting all Input Files ...\r\n");
                return false;
            }

            richTextBoxMikePanelStatus.AppendText("Trying to save Input File(s) to DB: \r\n\r\n");
            Application.DoEvents();
            foreach (string f in FileNameList)
            {
                AddNewScenarioFileAndAssociatedFiles(f, NewMikeScenario, csspItem, CurrentTVI, vpse);
            }
            Application.DoEvents();

            //__________________________________________________
            // uploading Result Files
            //__________________________________________________
            FileNameList.Clear();
            if (!GetAllResultFilesToUpload(TempFileName, FileNameList, NewMikeScenario))
            {
                MessageBox.Show("Error in GetAllResultFilesToUpload");
                richTextBoxMikePanelStatus.AppendText("Error while collecting all Result Files ...\r\n");
                return false;
            }

            // will try to load the scenario associated log file
            FileNameList.Add(TempFileName.Substring(0, TempFileName.LastIndexOf(".")) + ".log");

            richTextBoxMikePanelStatus.AppendText("Trying to save Result File(s) to DB: \r\n\r\n");
            Application.DoEvents();

            foreach (string f in FileNameList)
            {
                //   if (!f.ToLower().EndsWith(".m21fm"))
                AddNewScenarioFileAndAssociatedFiles(f, NewMikeScenario, csspItem, CurrentTVI, vpse);
            }
            Application.DoEvents();

            // _______________________________________
            // Creating MikeParameters associated with the scenario
            // _______________________________________
            richTextBoxMikePanelStatus.AppendText("Trying to create and save a new MikeParameters in to DB: \r\n\r\n");
            Application.DoEvents();

            MikeParameter NewMikeParameter = new MikeParameter();
            NewMikeParameter.WindSpeed = m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed * 3.6;
            NewMikeParameter.WindDirection = m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_direction;
            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DECAY.COMPONENT> kvp in m21fm.femEngineHD.transport_module.decay.component)
            {
                NewMikeParameter.DecayFactorPerDay = kvp.Value.constant_value * 24 * 3600;
                if (kvp.Value.format == 0)
                {
                    NewMikeParameter.DecayIsConstant = true;
                }
                else
                {
                    NewMikeParameter.DecayIsConstant = false;
                }
                NewMikeParameter.DecayFactorAmplitude = 4;

                break;
            }
            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.OUTPUTS.OUTPUT> kvp in m21fm.femEngineHD.transport_module.outputs.output)
            {
                NewMikeParameter.ResultFrequencyInMinutes = kvp.Value.time_step_frequency;
            }
            NewMikeParameter.AmbientTemperature = m21fm.femEngineHD.hydrodynamic_module.density.temperature_reference;
            NewMikeParameter.AmbientSalinity = m21fm.femEngineHD.hydrodynamic_module.density.salinity_reference;
            NewMikeParameter.ManningNumber = m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.constant_value;

            NewMikeScenario.MikeParameters.Add(NewMikeParameter);

            try
            {
                vpse.SaveChanges();
                richTextBoxMikePanelStatus.AppendText("New MikeParameters created in to DB: \r\n\r\n");
            }
            catch (Exception ex)
            {
                richTextBoxMikePanelStatus.AppendText("Error while trying to create and save a new MikeParameters in to DB: \r\n\r\n");
                richTextBoxMikePanelStatus.AppendText("Error:" + ex.Message + "\r\n\r\n");
                return false;
            }
            Application.DoEvents();

            // _______________________________________
            // creating all the MikeSources associated with the scenario
            // _______________________________________
            richTextBoxMikePanelStatus.AppendText("Trying to create and save a new MikeSources in to DB: \r\n\r\n");
            Application.DoEvents();

            foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.hydrodynamic_module.sources.source)
            {
                MikeSource NewMikeSource = new MikeSource();

                NewMikeSource.SourceNumberString = kvp.Key;
                NewMikeSource.SourceName = kvp.Value.Name.Substring(1, kvp.Value.Name.Length - 2);
                NewMikeSource.Include = kvp.Value.include == 1 ? true : false;
                NewMikeSource.SourceFlow = kvp.Value.constant_value * 24 * 3600;
                NewMikeSource.SourceLat = kvp.Value.coordinates.y;
                NewMikeSource.SourceLong = kvp.Value.coordinates.x;

                foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE> kvp2 in m21fm.femEngineHD.transport_module.sources.source)
                {
                    if (kvp.Key == kvp2.Key)
                    {
                        foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT> kvp3 in kvp2.Value.component)
                        {
                            NewMikeSource.SourcePollution = kvp3.Value.constant_value;
                            NewMikeSource.IsContinuous = kvp3.Value.format == 0 ? true : false;
                            NewMikeSource.StartDateAndTime = NewMikeScenario.ScenarioStartDateAndTime;
                            NewMikeSource.EndDateAndTime = NewMikeScenario.ScenarioEndDateAndTime;
                            break;
                        }
                    }
                }

                foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE> kvp2 in m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source)
                {
                    if (kvp.Key == kvp2.Key)
                    {
                        NewMikeSource.SourceTemperature = kvp2.Value.temperature.constant_value;
                        NewMikeSource.SourceSalinity = kvp2.Value.salinity.constant_value;
                        break;
                    }
                }

                NewMikeScenario.MikeSources.Add(NewMikeSource);
            }

            try
            {
                vpse.SaveChanges();
                richTextBoxMikePanelStatus.AppendText("New MikeSources created in to DB: \r\n\r\n");
            }
            catch (Exception ex)
            {
                richTextBoxMikePanelStatus.AppendText("Error while trying to create and save a new MikeSources in to DB: \r\n\r\n");
                richTextBoxMikePanelStatus.AppendText("Error:" + ex.Message + "\r\n\r\n");
                return false;
            }
            Application.DoEvents();

            // running the GenerateInputSummary and saving it in the ScenarioDescription field of MikeScenario
            NewMikeScenario.ScenarioSummary = GenerateInputSummary(NewMikeScenario);

            try
            {
                vpse.SaveChanges();
            }
            catch (Exception)
            {
                richTextBoxMikePanelStatus.AppendText("Error while trying to update ScenarioDescriptioin of MikeScenario \r\n\r\n");
            }
            Application.DoEvents();


            // updating the m21fm file
            MemoryStream msM21fm = new MemoryStream();

            msM21fm = m21fm.M21fmToStream();

            string LookForFileName = TempFileName.Substring(TempFileName.LastIndexOf("\\") + 1);
            string LookForFileOriginalPath = TempFileName.Substring(0, TempFileName.LastIndexOf("\\") + 1);

            CSSPFile csspFileM21fm = (from c in vpse.CSSPFiles
                                      where c.FileName == LookForFileName
                                      && c.FileOriginalPath == LookForFileOriginalPath
                                      select c).FirstOrDefault<CSSPFile>();

            if (csspFileM21fm == null)
            {
                MessageBox.Show("Could not found the m21fm file just loaded to the DB file = [" + LookForFileOriginalPath + LookForFileName + "]\r\n");
                richTextBoxMikePanelStatus.AppendText("Could not found the m21fm file just loaded to the DB file = [" + TempFileName + "]\r\n");
                return false;
            }

            csspFileM21fm.FileContent = msM21fm.ToArray();

            try
            {
                vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
            }
            catch (Exception)
            {
                MessageBox.Show("Could not update the m21fm file with proper new file names to the DB file = [" + TempFileName + "]\r\n");
                richTextBoxMikePanelStatus.AppendText("Could not update the m21fm file with proper new file names to the DB file = [" + TempFileName + "]\r\n");
                return false;
            }

            richTextBoxMikePanelStatus.AppendText("Creating BathymetryFromMesh.kmz file. \r\n\r\n");
            if (!CreateKMZResultFiles(KMZResultType.Bathymetry, NewMikeScenario))
                return false;
            richTextBoxMikePanelStatus.AppendText("Creating Mesh.kmz file. \r\n\r\n");
            if (!CreateKMZResultFiles(KMZResultType.Mesh, NewMikeScenario))
                return false;
            richTextBoxMikePanelStatus.AppendText("Creating StudyArea.kmz file. \r\n\r\n");
            if (!CreateKMZResultFiles(KMZResultType.StudyArea, NewMikeScenario))
                return false;

            FillDataGridViewMikeScenairosInDB(NewMikeScenario.MikeScenarioID);

            SaveScenarioChanges(false);

            FillDataGridViewMikeScenairosInDB(NewMikeScenario.MikeScenarioID);

            return true;
        }
        private bool AddNewScenarioFileAndAssociatedFiles(string FileName, MikeScenario NewMikeScenario, CSSPItem csspItem, TVI CurrentTVI, CSSPAppDBEntities vpse)
        {
            //CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            bool FileExist = false;
            CSSPFile csspFile = new CSSPFile();
            CSSPFile csspFileExist = new CSSPFile();

            richTextBoxMikePanelStatus.AppendText("\r\n\r\nFile [" + FileName + "]\r\n");

            FileInfo fi = new FileInfo(FileName);
            FileStream fs = fi.OpenRead();

            string FilePath = FileName.Substring(0, FileName.LastIndexOf("\\") + 1);
            string ShortFileName = FileName.Substring(FileName.LastIndexOf("\\") + 1);

            Application.DoEvents();

            //Read all file bytes into an array from the specified file.
            int nBytes = (int)fi.Length;
            Byte[] ByteArray = new byte[nBytes];
            int nBytesRead = fs.Read(ByteArray, 0, nBytes);

            fs.Close();

            richTextBoxMikePanelStatus.AppendText("Checking if file already in DB ...\r\n");
            Application.DoEvents();

            //string TheFileName = fi.FullName.Substring(fi.FullName.LastIndexOf("\\") + 1);
            csspFileExist = (from f in vpse.CSSPFiles
                             where f.FileName == ShortFileName
                             && f.FileOriginalPath == FilePath
                             && f.FileType == fi.Extension
                             && f.FileSize == fi.Length
                             select f).FirstOrDefault<CSSPFile>();

            if (csspFileExist != null)
            {
                byte[] ByteArray2 = csspFileExist.FileContent;

                richTextBoxMikePanelStatus.AppendText("Comparing saved file and file to upload in DB ...\r\n");
                FileExist = BinIdentical(ByteArray, ByteArray2);
                if (FileExist)
                {
                    //MessageBox.Show("The file \r\n[" + FileName + "]\r\n already exist in DB");
                    richTextBoxMikePanelStatus.AppendText("File already in DB ...\r\n");
                    //return false;
                }
            }

            if (!FileExist)
            {
                richTextBoxMikePanelStatus.AppendText("File does not exist in DB ...\r\n");
                richTextBoxMikePanelStatus.AppendText("Saving file in DB ...\r\n");
                Application.DoEvents();

                csspFile.CSSPGuid = Guid.NewGuid();
                csspFile.FileName = ShortFileName;
                csspFile.FileOriginalPath = FilePath;
                csspFile.FileSize = fi.Length;
                csspFile.FileDescription = "";
                csspFile.FileCreatedDate = fi.CreationTime;
                csspFile.FileType = fi.Extension;
                csspFile.FileContent = ByteArray;

                if (fi.Extension.ToLower() == ".dfs0" || fi.Extension.ToLower() == ".dfs1")
                {
                    csspFile.Purpose = PurposeType.Original.ToString();
                }
                else if (fi.Extension.ToLower() == ".mdf")
                {
                    csspFile.Purpose = PurposeType.Original.ToString();
                }
                else if (fi.Extension.ToLower() == ".mesh")
                {
                    csspFile.Purpose = PurposeType.Original.ToString();
                }
                else if (fi.Extension.ToLower() == ".dfsu")
                {
                    csspFile.Purpose = PurposeType.MikeResult.ToString();
                }
                else if (fi.Extension.ToLower() == ".log")
                {
                    csspFile.Purpose = PurposeType.MikeResult.ToString();
                }
                else
                {
                    csspFile.Purpose = PurposeType.Input.ToString();
                }

                try
                {
                    vpse.AddToCSSPFiles(csspFile);
                    vpse.SaveChanges();
                    richTextBoxMikePanelStatus.AppendText("CSSPFile saved in DB ...\r\n");
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not store file [" + FileName + "] in DB\r\n" + ex.Message + "\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not store file [" + FileName + "] in DB\r\n" + ex.Message + "\r\n");
                    Application.DoEvents();
                    return false;
                }

                MikeScenarioFile mikeScenarioFileExist = (from msf in vpse.MikeScenarioFiles
                                                          where msf.MikeScenario.MikeScenarioID == NewMikeScenario.MikeScenarioID
                                                          && msf.CSSPFile.CSSPFileID == csspFile.CSSPFileID
                                                          select msf).FirstOrDefault<MikeScenarioFile>();

                if (mikeScenarioFileExist != null)
                {
                    // already exist this should never happen
                }
                else
                {
                    MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                    NewMikeScenarioFile.MikeScenario = NewMikeScenario;
                    NewMikeScenarioFile.CSSPFile = csspFile;
                    NewMikeScenarioFile.CSSPParentFile = csspFile;

                    try
                    {
                        richTextBoxMikePanelStatus.AppendText("Linking MikeScenario to CSSPFile using MikeScenarioFile ...\r\n");
                        Application.DoEvents();
                        vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                        vpse.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                        Application.DoEvents();
                        return false;
                    }
                }
            }
            else
            {
                MikeScenarioFile mikeScenarioFile = (from msif in vpse.MikeScenarioFiles
                                                     where msif.CSSPFile.CSSPFileID == csspFileExist.CSSPFileID
                                                     && msif.MikeScenario.MikeScenarioID == NewMikeScenario.MikeScenarioID
                                                     select msif).FirstOrDefault<MikeScenarioFile>();

                if (mikeScenarioFile != null)
                {
                    //MessageBox.Show("Link to file already stored for Municipality [" + CurrentTVI.ItemText + "] in DB\r\n");
                    richTextBoxMikePanelStatus.AppendText("Link to file already stored for Municipality [" + CurrentTVI.ItemText + "] in DB\r\n");
                    Application.DoEvents();
                }
                else
                {
                    richTextBoxMikePanelStatus.AppendText("Storing link to file for Municipality [" + CurrentTVI.ItemText + "] in DB\r\n");

                    MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                    NewMikeScenarioFile.MikeScenario = NewMikeScenario;
                    NewMikeScenarioFile.CSSPFile = csspFileExist;
                    NewMikeScenarioFile.CSSPParentFile = csspFileExist;

                    try
                    {
                        richTextBoxMikePanelStatus.AppendText("Stored link to file for Municipality [" + CurrentTVI.ItemText + "] in DB\r\n");
                        Application.DoEvents();
                        vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                        vpse.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not store link to file in DB\r\n" + ex.Message + "\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not store link to file in DB\r\n" + ex.Message + "\r\n");
                        Application.DoEvents();
                        return false;
                    }
                }

            }


            // if FileType is .dfs0 then we should create a smaller file 
            // the .m21fm file fileContent appropriatly would already be 
            if (fi.Extension.ToLower() == ".dfs0" || fi.Extension.ToLower() == ".dfs1")
            {

                if (fi.Extension.ToLower() == ".dfs0")
                {
                    dfs = new Dfs(Dfs.DFSType.DFS0, true);
                }
                else
                {
                    dfs = new Dfs(Dfs.DFSType.DFS1, true);
                }

                MemoryStream ms = FileToMemoryStream(fi.FullName);

                dfs.StreamToDfs(ms);

                csspFile.DataStartDate = dfs.DataStartDate;
                csspFile.DataEndDate = dfs.DataStartDate.AddSeconds(dfs.TimeSteps * dfs.XValueList.Count);
                csspFile.TimeStepsInSecond = dfs.TimeSteps;
                string ParamNameTxt = "";
                string ParamUnitTxt = "";
                foreach (Dfs.Parameter p in dfs.ParameterList)
                {
                    ParamUnitTxt += string.Format("[{0}]-", p.UnitCode.ToString());
                    ParamNameTxt += string.Format("[{0}]-", p.Code);
                }
                csspFile.ParameterNames = ParamNameTxt.Substring(0, ParamNameTxt.Length - 1);
                csspFile.ParameterUnits = ParamUnitTxt.Substring(0, ParamUnitTxt.Length - 1);

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Updating csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Updated csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while updating csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error Message [" + ex.Message + "] ... \r\n");
                }

                dfs.FileCreatedDate = DateTime.Now;
                dfs.FileLastModifiedDate = DateTime.Now;
                if (dfs.Equidistant != Dfs.EquidistantOrNonEquidistant.Equidistant)
                {
                    MessageBox.Show("Please use dfs equidistant in dfs0 files.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Please use dfs equidistant in dfs0 files.\r\n");
                    return false;
                }
                DateTime StartDateTime = m21fm.femEngineHD.time.start_time;
                DateTime EndDateTime = m21fm.femEngineHD.time.start_time.AddSeconds(m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval);

                dfs.Title = string.Format("[{0}] ", NewMikeScenario.MikeScenarioID) + dfs.Title;

                if (dfs.UnitCode != Dfs.Unit.Second)
                {
                    MessageBox.Show("Please use dfs unit in second in dfs0 files.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Please use dfs unit in second in dfs0 files.\r\n");
                    return false;
                }

                int NumberOfSeconds = m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval;

                DateTime DfsStartDate = dfs.DataStartDate;

                TimeSpan ts = new TimeSpan(StartDateTime.Ticks - DfsStartDate.Ticks);

                dfs.DataStartDate = m21fm.femEngineHD.time.start_time;
                dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                if (dfs.NumberOfValues > dfs.ParameterList[0].ValueList.Count)
                {
                    dfs.NumberOfValues = dfs.ParameterList[0].ValueList.Count;
                }

                int NumberOfValuesToRemoveFromStart = (int)(ts.TotalSeconds / dfs.TimeSteps);

                List<float> TempValueList = new List<float>();
                dfs.XValueList = new List<double>();
                for (int i = 0; i < dfs.NumberOfValues; i++)
                {
                    dfs.XValueList.Add(i * dfs.TimeSteps);
                }

                foreach (Dfs.Parameter p in dfs.ParameterList)
                {
                    dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                    if (dfs.NumberOfValues > p.ValueList.Count)
                    {
                        dfs.NumberOfValues = p.ValueList.Count;
                    }

                    TempValueList = new List<float>();
                    for (int i = NumberOfValuesToRemoveFromStart; i < (NumberOfValuesToRemoveFromStart + dfs.NumberOfValues); i++)
                    {
                        TempValueList.Add(p.ValueList[i]);
                    }

                    p.ValueList = new List<float>();
                    foreach (float f in TempValueList)
                    {
                        p.ValueList.Add(f);
                    }

                    p.Maximum = p.ValueList.Max<float>();
                    p.Minimum = p.ValueList.Min<float>();

                    foreach (Dfs.Parameter.Stat s in p.StatList)
                    {
                        s.LastValue = p.ValueList[p.ValueList.Count - 1];
                        s.Maximum = p.ValueList.Max<float>();
                        s.Minimum = p.ValueList.Min<float>();
                        s.NumberOfTimeSeries = dfs.NumberOfValues;
                        s.NumberOfValuesMinus1 = dfs.NumberOfValues - 1;
                        s.Sum = p.ValueList.Sum();
                        s.SumOfSquareOfValue = 0;
                        s.ValueMultiplicated = 0;
                        foreach (float f in p.ValueList)
                        {
                            s.SumOfSquareOfValue += f * f;
                            s.ValueMultiplicated *= f;
                        }
                    }
                }

                MemoryStream msNewFile = new MemoryStream();
                ms = dfs.DfsToStream();

                CSSPFile csspSubFile = new CSSPFile();
                csspSubFile.CSSPGuid = Guid.NewGuid();
                csspSubFile.FileName = string.Format("[{0}] ", NewMikeScenario.MikeScenarioID) + ShortFileName;
                csspSubFile.FileOriginalPath = FilePath;
                csspSubFile.FileSize = ms.Length;
                csspSubFile.FileDescription = "";
                csspSubFile.FileCreatedDate = DateTime.Now;
                csspSubFile.FileType = fi.Extension;
                csspSubFile.FileContent = ms.ToArray();
                csspSubFile.Purpose = PurposeType.Input.ToString();
                csspSubFile.DataStartDate = dfs.DataStartDate;
                csspSubFile.DataEndDate = dfs.DataStartDate.AddSeconds(dfs.TimeSteps * dfs.XValueList.Count);
                csspSubFile.TimeStepsInSecond = dfs.TimeSteps;

                ParamNameTxt = "";
                ParamUnitTxt = "";
                foreach (Dfs.Parameter p in dfs.ParameterList)
                {
                    ParamUnitTxt += string.Format("[{0}]-", p.UnitCode.ToString());
                    ParamNameTxt += string.Format("[{0}]-", p.Code);
                }
                csspSubFile.ParameterNames = ParamNameTxt.Substring(0, ParamNameTxt.Length - 1);
                csspSubFile.ParameterUnits = ParamUnitTxt.Substring(0, ParamUnitTxt.Length - 1);

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Adding file [" + string.Format("[{0}] ", NewMikeScenario.MikeScenarioID) + ShortFileName + "] in DB\r\n");
                    vpse.AddToCSSPFiles(csspSubFile);
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Added file [" + string.Format("[{0}] ", NewMikeScenario.MikeScenarioID) + ShortFileName + "] in DB\r\n");
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not save file " + csspSubFile.FileName + " in DB .\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not save file " + csspSubFile.FileName + " in DB .\r\n");
                    return false;
                }

                MikeScenarioFile NewMikeScenarioSubFile = new MikeScenarioFile();
                NewMikeScenarioSubFile.MikeScenario = NewMikeScenario;
                NewMikeScenarioSubFile.CSSPFile = csspSubFile;
                NewMikeScenarioSubFile.CSSPParentFile = csspFile;

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Linking MikeScenario to CSSPFile using MikeScenarioFile ...\r\n");
                    Application.DoEvents();

                    vpse.AddToMikeScenarioFiles(NewMikeScenarioSubFile);
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                    Application.DoEvents();
                    return false;
                }
            }
            else if (fi.Extension.ToLower() == ".dfsu")
            {
                dfs = new Dfs(Dfs.DFSType.DFSU, true);

                MemoryStream ms = FileToMemoryStream(fi.FullName);

                dfs.StreamToDfs(ms);

                csspFile.DataStartDate = dfs.DataStartDate;
                csspFile.DataEndDate = dfs.DataStartDate.AddSeconds(dfs.TimeSteps * dfs.XValueList.Count);
                csspFile.TimeStepsInSecond = dfs.TimeSteps;
                string ParamNameTxt = "";
                string ParamUnitTxt = "";
                foreach (Dfs.Parameter p in dfs.ParameterList)
                {
                    ParamUnitTxt += string.Format("[{0}]-", p.UnitCode.ToString());
                    ParamNameTxt += string.Format("[{0}]-", p.Code);
                }
                csspFile.ParameterNames = ParamNameTxt.Substring(0, ParamNameTxt.Length - 1);
                csspFile.ParameterUnits = ParamUnitTxt.Substring(0, ParamUnitTxt.Length - 1);

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Updating csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Updated csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while updating csspFile with DataStartDate, DataEndDate and TimeStepsInSecond in DB\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error Message [" + ex.Message + "] ... \r\n");
                }
            }
            else if (fi.Extension.ToLower() == ".mesh")
            {
                CSSPFile csspSubFile = new CSSPFile();
                csspSubFile.CSSPGuid = Guid.NewGuid();
                csspSubFile.FileName = string.Format("[{0}] ", NewMikeScenario.MikeScenarioID) + ShortFileName;
                csspSubFile.FileOriginalPath = FilePath;
                csspSubFile.FileSize = fi.Length;
                csspSubFile.FileDescription = "";
                csspSubFile.FileCreatedDate = fi.CreationTime;
                csspSubFile.FileType = fi.Extension;
                csspSubFile.FileContent = ByteArray;
                csspSubFile.Purpose = PurposeType.Input.ToString();

                try
                {
                    vpse.AddToCSSPFiles(csspSubFile);
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("CSSPFile saved in DB ...\r\n");
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not store file [" + csspSubFile.FileName + "] in DB\r\n" + ex.Message + "\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not store file [" + csspSubFile.FileName + "] in DB\r\n" + ex.Message + "\r\n");
                    Application.DoEvents();
                    return false;
                }

                MikeScenarioFile mikeScenarioFileExist = (from msf in vpse.MikeScenarioFiles
                                                          where msf.MikeScenario.MikeScenarioID == NewMikeScenario.MikeScenarioID
                                                          && msf.CSSPFile.CSSPFileID == csspSubFile.CSSPFileID
                                                          select msf).FirstOrDefault<MikeScenarioFile>();

                if (mikeScenarioFileExist != null)
                {
                    // already exist this should never happen
                }
                else
                {
                    MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                    NewMikeScenarioFile.MikeScenario = NewMikeScenario;
                    NewMikeScenarioFile.CSSPFile = csspSubFile;
                    NewMikeScenarioFile.CSSPParentFile = csspFile;

                    try
                    {
                        richTextBoxMikePanelStatus.AppendText("Linking MikeScenario to CSSPFile using MikeScenarioFile ...\r\n");
                        Application.DoEvents();
                        vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not link MikeScenario to CSSPFile\r\n" + ex.Message + "\r\n");
                        Application.DoEvents();
                        return false;
                    }
                }
            }
            else
            {
                // nothing
            }
            return true;
        }
        private bool BinIdentical(byte[] ByteArray, byte[] ByteArray2)
        {
            if (ByteArray.Length != ByteArray2.Length)
            {
                return false;
            }

            for (int i = 0; i < ByteArray.Length; i++)
            {
                if (ByteArray[i] != ByteArray2[i])
                {
                    return false;
                }
            }

            return true;
        }
        private void ComboBoxMikeScenarioSourceNameSelectionIndexChanged()
        {
            butMikeScenarioAddNewSource.Enabled = false;

            if (comboBoxMikeScenarioSourceName.SelectedItem != null)
            {
                if (CurrentMikeSourceIndex > -1)
                {
                    if (textBoxNewSourceName.Text != "||||||")
                    {
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceName = textBoxNewSourceName.Text.Trim();
                        CurrentMikeSourceList[CurrentMikeSourceIndex].Include = (bool)checkBoxMikeSouceIncluded.Checked;
                        float TempFloat;
                        if (!float.TryParse(textBoxMikeSouceFlowInCubicMeterPerDay.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid flow.");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceFlow = TempFloat;
                        CurrentMikeSourceList[CurrentMikeSourceIndex].IsContinuous = (bool)checkBoxFlowContinuous.Checked;
                        CurrentMikeSourceList[CurrentMikeSourceIndex].StartDateAndTime = dateTimePickerSourcePollutionStartDateAndTime.Value;
                        CurrentMikeSourceList[CurrentMikeSourceIndex].EndDateAndTime = dateTimePickerSourcePollutionEndDateAndTime.Value;
                        if (!float.TryParse(textBoxMikeSourceFC.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid number for FC.");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourcePollution = TempFloat;
                        if (!float.TryParse(textBoxMikeSourceTemperature.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid number temperature.");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceTemperature = TempFloat;
                        if (!float.TryParse(textBoxMikeSourceSalinity.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid number salinity.");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceSalinity = TempFloat;
                        if (!float.TryParse(textBoxLatitude.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid latitute.");
                            return;
                        }
                        if (TempFloat < -90 || TempFloat > 90)
                        {
                            MessageBox.Show("Please enter a valid latitute (between -90 and 90).");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceLat = TempFloat;
                        if (!float.TryParse(textBoxLongitude.Text, out TempFloat))
                        {
                            MessageBox.Show("Please enter a valid longitude.");
                            return;
                        }
                        if (TempFloat < -180 || TempFloat > 180)
                        {
                            MessageBox.Show("Please enter a valid longitude (between -180 and 180).");
                            return;
                        }
                        CurrentMikeSourceList[CurrentMikeSourceIndex].SourceLong = TempFloat;
                        CurrentMikeSourceList[CurrentMikeSourceIndex].StartDateAndTime = dateTimePickerSourcePollutionStartDateAndTime.Value;
                        CurrentMikeSourceList[CurrentMikeSourceIndex].EndDateAndTime = dateTimePickerSourcePollutionEndDateAndTime.Value;
                    }
                }

                CurrentMikeSourceIndex = (int)comboBoxMikeScenarioSourceName.SelectedIndex;

                if (CurrentMikeSourceIndex > CurrentMikeSourceList.Count - 1)
                {
                    CurrentMikeSourceIndex = 0;
                }
                textBoxNewSourceName.Text = CurrentMikeSourceList[CurrentMikeSourceIndex].SourceName.Trim();
                checkBoxMikeSouceIncluded.Checked = (bool)CurrentMikeSourceList[CurrentMikeSourceIndex].Include;
                textBoxMikeSouceFlowInCubicMeterPerDay.Text = string.Format("{0:F2}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceFlow);
                textBoxMikeSouceFlowInCubicMeterPerSecond.Text = string.Format("{0:F8}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceFlow / 24 / 3600);
                checkBoxFlowContinuous.Checked = (bool)CurrentMikeSourceList[CurrentMikeSourceIndex].IsContinuous;
                if (CurrentMikeSourceList[CurrentMikeSourceIndex].StartDateAndTime != null)
                {
                    dateTimePickerSourcePollutionStartDateAndTime.Value = (DateTime)CurrentMikeSourceList[CurrentMikeSourceIndex].StartDateAndTime;
                }
                if (CurrentMikeSourceList[CurrentMikeSourceIndex].EndDateAndTime != null)
                {
                    dateTimePickerSourcePollutionEndDateAndTime.Value = (DateTime)CurrentMikeSourceList[CurrentMikeSourceIndex].EndDateAndTime;
                }
                textBoxMikeSourceFC.Text = string.Format("{0:F0}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourcePollution);
                textBoxMikeSourceTemperature.Text = string.Format("{0:F1}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceTemperature);
                textBoxMikeSourceSalinity.Text = string.Format("{0:F1}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceSalinity);
                textBoxLatitude.Text = string.Format("{0:F8}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceLat);
                textBoxLongitude.Text = string.Format("{0:F8}", CurrentMikeSourceList[CurrentMikeSourceIndex].SourceLong);

                butMikeScenarioRemoveSource.Enabled = true;

            }
        }
        private bool CreateKMZResultFiles(KMZResultType kMZResultType, MikeScenario mikeScenario)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            string FileName = "";
            string ShortFileName = "";

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1 || mikeScenario != null)
            {
                if (mikeScenario == null)
                {
                    mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                }

                // finding the MikeScenarioFiles with FileType == ".m21fm"
                CSSPFile csspFilem21fm = (from cf in vpse.CSSPFiles
                                          from msf in vpse.MikeScenarioFiles
                                          where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                          && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                          && cf.FileType == ".m21fm"
                                          select cf).FirstOrDefault<CSSPFile>();

                if (csspFilem21fm == null)
                {
                    MessageBox.Show("Could not find CSSPFile with extension [.m21fm] in DB for the selected scenario.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not find CSSPFile with extension [.m21fm] in DB for the selected scenario.\r\n");
                    return false;
                }

                FileName = csspFilem21fm.FileOriginalPath + csspFilem21fm.FileName;
                ShortFileName = FileName.Substring(FileName.LastIndexOf("\\") + 1);
                ShortFileName = ShortFileName.Substring(0, ShortFileName.LastIndexOf("."));

                MemoryStream ms = new MemoryStream(csspFilem21fm.FileContent);
                //StreamReader sr = new StreamReader(ms);

                richTextBoxMikePanelStatus.AppendText("Trying to load and parse file = [" + csspFilem21fm.FileOriginalPath + csspFilem21fm.FileName + "]\r\n");


                // M21fm m21fm = new M21fm();
                if (!m21fm.StreamToM21fm(ms))
                {
                    MessageBox.Show("File Not read properly");
                    return false;
                }

                string FirstPart = FileName.Substring(0, FileName.LastIndexOf("\\"));
                string SecondPart = m21fm.system.ResultRootFolder.Replace("|", "\\");
                string ThirdPart = FileName.Substring(FileName.LastIndexOf("\\") + 1) + " - Result Files\\";
                string ForthPart = m21fm.femEngineHD.transport_module.outputs.output.First().Value.file_name.Replace("'", "");

                FileInfo fi = new FileInfo(FirstPart + SecondPart + ThirdPart + ForthPart);

                // finding the MikeScenarioFiles with FileType == ".m21fm"
                CSSPFile csspFiledfsu = (from cf in vpse.CSSPFiles
                                         from msf in vpse.MikeScenarioFiles
                                         where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                         && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                         && cf.FileType == ".dfsu"
                                         && (cf.FileOriginalPath + cf.FileName) == fi.FullName
                                         select cf).FirstOrDefault<CSSPFile>();

                if (csspFiledfsu == null)
                {
                    MessageBox.Show("Could not find CSSPFile with full file name = [" + fi.FullName + "].\r\n");
                    richTextBoxMikePanelStatus.AppendText("Could not find CSSPFile with full file name = [" + fi.FullName + "].\r\n");
                    return false;
                }

                richTextBoxMikePanelStatus.AppendText(string.Format("Reading ... \r\n\r\n[{0}]", fi.FullName));

                MemoryStream mems = new MemoryStream(csspFiledfsu.FileContent);
                CreateNewDfsWithEvents(Dfs.DFSType.DFSU, true);
                dfs.StreamToDfs(mems);

                richTextBoxMikePanelStatus.AppendText("\r\n\r\nDone reading ...\r\n\r\n");

                if (dfs.ElementList == null)
                {
                    MessageBox.Show("Please load the data");
                    return false;
                }

                // Reseting appliation variables sbKML, sbStyle, sbPlacemark
                StringBuilder sbKML = new StringBuilder();
                StringBuilder sbStyle = new StringBuilder();
                StringBuilder sbPlacemark = new StringBuilder();

                // private KML and KMZ file names
                string KMLFileNameRoot = fi.FullName.Substring(0, fi.FullName.LastIndexOf("\\"));
                string KMZFileNameRoot = fi.FullName.Substring(0, fi.FullName.LastIndexOf("\\"));
                KMLFileNameRoot = KMLFileNameRoot.Substring(0, KMLFileNameRoot.LastIndexOf("\\") + 1);
                KMZFileNameRoot = KMZFileNameRoot.Substring(0, KMZFileNameRoot.LastIndexOf("\\") + 1);

                // Making sure the ByteArray contains the information of the DFSU file and is of type DFSType.DFSU
                if (m21fm == null || dfs.Type != Dfs.DFSType.DFSU)
                {
                    MessageBox.Show("Please load a DFSU file");
                    return false;
                }

                richTextBoxMikePanelStatus.AppendText(string.Format("Creating ... \r\n\r\n[{0}]\r\n", KMZFileNameRoot));

                ContourValueList = new List<float>();

                try
                {
                    foreach (string s in textBoxContourValues.Text.Split(delimiter))
                    {
                        ContourValueList.Add(float.Parse(s));
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Please enter valid numbers separated by commas for pollution contour");
                    return false;
                }

                CreateNewKMZWithEvents(dfs, m21fm);

                switch (kMZResultType)
                {
                    case KMZResultType.InputAndResult:
                        {
                            #region Pollution Video and Limits and Model Input
                            string DocName = fi.FullName.Substring(0, fi.FullName.LastIndexOf("\\"));
                            DocName = DocName.Substring(0, DocName.LastIndexOf("\\"));
                            DocName = DocName.Substring(0, DocName.LastIndexOf("\\"));
                            DocName = DocName.Substring(DocName.LastIndexOf("\\") + 1);

                            foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.hydrodynamic_module.sources.source)
                            {
                                if (kvp.Value.include == 1)
                                {
                                    if (m21fm.femEngineHD.transport_module.sources.source[kvp.Key].component["COMPONENT_1"].constant_value > 0)
                                    {
                                        string Accronym = "";
                                        char LowChar = "A".ToCharArray()[0];
                                        char HighChar = "Z".ToCharArray()[0];
                                        char LowNumChar = "0".ToCharArray()[0]; ;
                                        char HighNumChar = "9".ToCharArray()[0]; ; ;
                                        foreach (char c in kvp.Value.Name)
                                        {
                                            if ((c >= LowChar && c <= HighChar) || (c >= LowNumChar && c <= HighNumChar))
                                            {
                                                Accronym += c;
                                            }
                                        }

                                        DocName = DocName + string.Format("_[{0}]", Accronym);
                                        DocName = DocName + string.Format("_p[{0}]", m21fm.femEngineHD.transport_module.sources.source[kvp.Key].component["COMPONENT_1"].constant_value);
                                        DocName = DocName + string.Format("_d[{0}]", kvp.Value.constant_value * 24 * 3600);
                                    }
                                }
                            }

                            if (m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed > 0)
                            {
                                DocName = DocName + string.Format("_ws[{0}]", m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed);
                                DocName = DocName + string.Format("_wd[{0}]", m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_direction);
                            }
                            else
                            {
                                DocName = DocName + "no_wind";
                            }

                            DocName = DocName.Replace(" ", "_");

                            // __________________________________________________________
                            // Writting top part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLTop = new StringBuilder();
                            //kmz.WriteKMLTop(DocName, sbKMLTop);
                            
                            kmz.WriteKMLTop(ShortFileName + " Pol_Cont[" + textBoxContourValues.Text.Replace(",", "_") + "]", sbKMLTop);
                            kmz.mikeScenarioID = mikeScenario.MikeScenarioID;
                            sbKMLTop.Append(kmz.GetModelAndSourceDesc(ContourValueList));

                            sbKML = new StringBuilder();
                            sbStyle = new StringBuilder();
                            sbPlacemark = new StringBuilder();

                            // __________________________________________________________
                            // Creating Pollution Video
                            // __________________________________________________________
                            StringBuilder sbKMLFeacalColiformContour = new StringBuilder();
                            StringBuilder sbStyleFeacalColiformContour = new StringBuilder();

                            kmz.WriteKMLFeacalColiformContourLine(sbStyleFeacalColiformContour, sbKMLFeacalColiformContour, ContourValueList);
                            sbStyle.Append(sbStyleFeacalColiformContour.ToString());
                            sbPlacemark.Append(sbKMLFeacalColiformContour.ToString());

                            // __________________________________________________________
                            // Creating Pollution Limits
                            // __________________________________________________________
                            StringBuilder sbKMLPollutionLimitsContour = new StringBuilder();
                            StringBuilder sbStylePollutionLimitsContour = new StringBuilder();
                            kmz.WriteKMLPollutionLimitsContourLine(sbStylePollutionLimitsContour, sbKMLPollutionLimitsContour, ContourValueList);
                            sbStyle.Append(sbStylePollutionLimitsContour.ToString());
                            sbPlacemark.Append(sbKMLPollutionLimitsContour.ToString());

                            // __________________________________________________________
                            // Creating Input To Model
                            // __________________________________________________________
                            StringBuilder sbKMLModelInput = new StringBuilder();
                            StringBuilder sbStyleModelInput = new StringBuilder();
                            if (!kmz.WriteKMLModelInput(sbStyleModelInput, sbKMLModelInput, ContourValueList))
                                return false;
                            sbStyle.Append(sbStyleModelInput.ToString());
                            sbPlacemark.Append(sbKMLModelInput.ToString());

                            // __________________________________________________________
                            // Writting bottom part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLBottom = new StringBuilder();
                            kmz.WriteKMLBottom(sbKMLBottom);

                            // __________________________________________________________
                            // Concatenating KML file
                            // __________________________________________________________
                            sbKML.Append(sbKMLTop.ToString());
                            sbKML.Append(sbStyle.ToString());
                            sbKML.Append(sbPlacemark.ToString());
                            sbKML.Append(sbKMLBottom.ToString());

                            string PolCont = "_Pol_Cont";
                            foreach (float f in ContourValueList)
                            {
                                PolCont += string.Format("[{0}]", f);
                            }
                            string KMLFileName = KMLFileNameRoot + ShortFileName + PolCont + ".kml";
                            string KMZFileName = KMZFileNameRoot + ShortFileName + PolCont + ".kmz";

                            // make sure directory exist if not create it
                            DirectoryInfo di = new DirectoryInfo(KMZFileName.Substring(0, KMZFileName.LastIndexOf("\\")));
                            if (!di.Exists)
                                di.Create();

                            // Saving sbKML StringBuilder to the KML file [KMZFileName]
                            SaveInKMZFileStream(KMZFileName, KMLFileName, sbKML);

                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ file created and saved.\r\n");
                            UploadOtherScenarioFilesToDB(KMZFileName, mikeScenario);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ uploaded to DB.\r\n");

                            #endregion Pollution Video and Limits and Model Input
                        }
                        break;
                    case KMZResultType.StudyArea:
                        {
                            #region Study Area
                            // __________________________________________________________
                            // Creating Study Area line
                            // __________________________________________________________
                            // __________________________________________________________
                            // Writting top part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLTop = new StringBuilder();
                            kmz.WriteKMLTop("Study Area", sbKMLTop);

                            sbKML = new StringBuilder();
                            sbStyle = new StringBuilder();
                            sbPlacemark = new StringBuilder();

                            StringBuilder sbKMLStudyAreaLine = new StringBuilder();
                            StringBuilder sbStyleStudyAreaLine = new StringBuilder();
                            kmz.WriteKMLStudyAreaLine(sbStyleStudyAreaLine, sbKMLStudyAreaLine);
                            sbStyle.Append(sbStyleStudyAreaLine.ToString());
                            sbPlacemark.Append(sbKMLStudyAreaLine.ToString());

                            // __________________________________________________________
                            // Writting bottom part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLBottom = new StringBuilder();
                            kmz.WriteKMLBottom(sbKMLBottom);

                            // __________________________________________________________
                            // Concatenating KML file
                            // __________________________________________________________
                            sbKML.Append(sbKMLTop.ToString());
                            sbKML.Append(sbStyle.ToString());
                            sbKML.Append(sbPlacemark.ToString());
                            sbKML.Append(sbKMLBottom.ToString());

                            //sbKMLForShow.Clear();
                            //sbKMLForShow.Append(sbKML.ToString());

                            //richTextBoxMikePanelStatus.Text = sbKMLForShow.ToString();

                            string KMLFileName = KMLFileNameRoot + "StudyArea.kml";
                            string KMZFileName = KMZFileNameRoot + "StudyArea.kmz";

                            // make sure directory exist if not create it
                            DirectoryInfo di = new DirectoryInfo(KMZFileName.Substring(0, KMZFileName.LastIndexOf("\\")));
                            if (!di.Exists)
                                di.Create();
                            // Saving sbKML StringBuilder to the KML file [KMZFileName]
                            SaveInKMZFileStream(KMZFileName, KMLFileName, sbKML);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ file created and saved.\r\n");
                            UploadOtherScenarioFilesToDB(KMZFileName, mikeScenario);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ uploaded to DB.\r\n");
                            #endregion Study Area
                        }
                        break;
                    case KMZResultType.Bathymetry:
                        {
                            #region Bathymetry
                            // __________________________________________________________
                            // Writting top part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLTop = new StringBuilder();
                            kmz.WriteKMLTop("Bathymetry", sbKMLTop);

                            sbKML = new StringBuilder();
                            sbStyle = new StringBuilder();
                            sbPlacemark = new StringBuilder();

                            // __________________________________________________________
                            // Creating Bathymetry
                            // __________________________________________________________
                            StringBuilder sbKMLBathymetry = new StringBuilder();
                            StringBuilder sbStyleBathymetry = new StringBuilder();
                            kmz.WriteKMLBathymetryFromMesh(sbStyleBathymetry, sbKMLBathymetry);
                            sbStyle.Append(sbStyleBathymetry.ToString());
                            sbPlacemark.Append(sbKMLBathymetry.ToString());

                            // Temporarily removed 

                            //// __________________________________________________________
                            //// Creating Bathymetry Contours
                            //// __________________________________________________________
                            //StringBuilder sbKMLBathymetryContours = new StringBuilder();
                            //StringBuilder sbStyleBathymetryContours = new StringBuilder();
                            //kmz.WriteKMLBathymetryContours(sbStyleBathymetryContours, sbKMLBathymetryContours, ContourValueList);
                            //sbStyle.Append(sbStyleBathymetryContours.ToString());
                            //sbPlacemark.Append(sbKMLBathymetryContours.ToString());

                            // __________________________________________________________
                            // Writting bottom part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLBottom = new StringBuilder();
                            kmz.WriteKMLBottom(sbKMLBottom);

                            // __________________________________________________________
                            // Concatenating KML file
                            // __________________________________________________________
                            sbKML.Append(sbKMLTop.ToString());
                            sbKML.Append(sbStyle.ToString());
                            sbKML.Append(sbPlacemark.ToString());
                            sbKML.Append(sbKMLBottom.ToString());

                            //sbKMLForShow.Clear();
                            //sbKMLForShow.Append(sbKML.ToString());

                            //richTextBoxFile2.Text = sbKMLForShow.ToString();

                            string KMLFileName = KMLFileNameRoot + "BathymetryFromMesh.kml";
                            string KMZFileName = KMZFileNameRoot + "BathymetryFromMesh.kmz";

                            // make sure directory exist if not create it
                            DirectoryInfo di = new DirectoryInfo(KMZFileName.Substring(0, KMZFileName.LastIndexOf("\\")));
                            if (!di.Exists)
                                di.Create();
                            // Saving sbKML StringBuilder to the KML file [KMZFileName]
                            SaveInKMZFileStream(KMZFileName, KMLFileName, sbKML);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ file created and saved.\r\n");
                            UploadOtherScenarioFilesToDB(KMZFileName, mikeScenario);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ uploaded to DB.\r\n");

                            #endregion Bathymetry
                        }
                        break;
                    case KMZResultType.Mesh:
                        {
                            #region Mesh
                            // __________________________________________________________
                            // Creating Mesh
                            // __________________________________________________________
                            // __________________________________________________________
                            // Writting top part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLTop = new StringBuilder();
                            kmz.WriteKMLTop("Mesh", sbKMLTop);

                            sbKML = new StringBuilder();
                            sbStyle = new StringBuilder();
                            sbPlacemark = new StringBuilder();

                            StringBuilder sbKMLMesh = new StringBuilder();
                            StringBuilder sbStyleMesh = new StringBuilder();
                            kmz.WriteKMLMesh(sbStyleMesh, sbKMLMesh);
                            sbStyle.Append(sbStyleMesh.ToString());
                            sbPlacemark.Append(sbKMLMesh.ToString());

                            // __________________________________________________________
                            // Writting bottom part of KML file
                            // __________________________________________________________
                            StringBuilder sbKMLBottom = new StringBuilder();
                            kmz.WriteKMLBottom(sbKMLBottom);

                            // __________________________________________________________
                            // Concatenating KML file
                            // __________________________________________________________
                            sbKML.Append(sbKMLTop.ToString());
                            sbKML.Append(sbStyle.ToString());
                            sbKML.Append(sbPlacemark.ToString());
                            sbKML.Append(sbKMLBottom.ToString());

                            //sbKMLForShow.Clear();
                            //sbKMLForShow.Append(sbKML.ToString());

                            //richTextBoxFile2.Text = sbKMLForShow.ToString();

                            string KMLFileName = KMLFileNameRoot + "Mesh.kml";
                            string KMZFileName = KMZFileNameRoot + "Mesh.kmz";

                            // make sure directory exist if not create it
                            DirectoryInfo di = new DirectoryInfo(KMZFileName.Substring(0, KMZFileName.LastIndexOf("\\")));
                            if (!di.Exists)
                                di.Create();
                            // Saving sbKML StringBuilder to the KML file [KMZFileName]
                            SaveInKMZFileStream(KMZFileName, KMLFileName, sbKML);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ file created and saved.\r\n");
                            UploadOtherScenarioFilesToDB(KMZFileName, mikeScenario);
                            richTextBoxMikePanelStatus.AppendText("Mesh KMZ uploaded to DB.\r\n");

                            #endregion Mesh
                        }
                        break;
                    default:
                        break;
                }

                FillAfterSelectMikeScenario();
            }
            return true;
        }
        private void CreateNewDfsWithEvents(Dfs.DFSType dfsType, bool readOnly)
        {
            if (dfs != null)
            {
                dfs = null;
            }

            dfs = new Dfs(dfsType, readOnly);

            dfs.DfsMessageEvent += new Dfs.DfsMessageEventHandler(dfs_MessageEvent);

            return;
        }
        private void CreateNewKMZWithEvents(Dfs dfs, M21fm m21fm)
        {
            if (kmz != null)
            {
                kmz = null;
            }

            kmz = new KMZ(dfs, m21fm);

            kmz.KMLMessageEvent += new KMZ.KMLMessageEventHandler(kmz_KMLMessageEvent);

            return;
        }
        private void CreateNewM21FMWithEvents()
        {
            if (m21fm != null)
            {
                m21fm = null;
            }
            m21fm = new M21fm();

            m21fm.M21fmMessageEvent += new M21fm.M21fmMessageEventHandler(m21fm_MessageEvent);

            return;
        }
        private void DownloadFiles()
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Trying to download files from DB ...\r\n");
            Application.DoEvents();

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI tvi = (TVI)treeViewItems.SelectedNode.Tag;

            if (dataGridViewScenarioFiles.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow dr in dataGridViewScenarioFiles.SelectedRows)
                {
                    CSSPFileNoContent csspFileNoContent = (CSSPFileNoContent)dr.DataBoundItem;
                    if (csspFileNoContent != null)
                    {
                        richTextBoxMikePanelStatus.AppendText(string.Format("{0}\r\n", csspFileNoContent.FileOriginalPath + csspFileNoContent.FileName));
                        richTextBoxMikePanelStatus.AppendText("Dowloading ...\r\n");
                        Application.DoEvents();

                        var csspFile = from cf in vpse.CSSPFiles
                                       where cf.CSSPFileID == csspFileNoContent.CSSPFileID
                                       select cf;

                        foreach (CSSPFile cf in csspFile)
                        {
                            FileInfo fi = new FileInfo(cf.FileOriginalPath + cf.FileName);

                            DirectoryInfo di = new DirectoryInfo(cf.FileOriginalPath);
                            if (!di.Exists)
                            {
                                di.Create();
                            }

                            if (fi.Exists)
                            {
                                if (MessageBox.Show("Are you sure you want to replace file? \r\n[" + cf.FileOriginalPath + cf.FileName + "]", "Replace file?", MessageBoxButtons.YesNoCancel) != System.Windows.Forms.DialogResult.Yes)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Canceled dowload ...\r\n");
                                    Application.DoEvents();
                                    return;
                                }
                            }
                            FileStream fs = fi.Create();
                            BinaryWriter writer = new BinaryWriter(fs);
                            writer.Write(cf.FileContent);
                            writer.Close();
                            fs.Close();
                            richTextBoxMikePanelStatus.AppendText("Dowloaded ...\r\n");
                            Application.DoEvents();
                        }
                    }
                }
                //FilldataGridViewMikeScenairosInDB(0);
            }
            else
            {
                butRemoveFileFromDB.Enabled = false;
                butViewTextFile.Enabled = false;
            }
        }
        private MemoryStream FileToMemoryStream(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            MemoryStream myMemoryStream = new MemoryStream();
            if (!fi.Exists)
            {
                richTextBoxMikePanelStatus.AppendText("File: \r\n\r\n[" + fileName + "] \r\n\r\ndoes not exist ... \r\n");
                return null;
            }
            richTextBoxMikePanelStatus.AppendText("Reading file: \r\n\r\n [" + fileName + "] ... \r\n\r\n");
            FileStream myFileStream = fi.OpenRead();
            myFileStream.CopyTo(myMemoryStream);
            myFileStream.Flush();
            myMemoryStream.Position = 0;
            return myMemoryStream;
        }
        private void FillAfterSelectMikeScenario()
        {
            CurrentMikeScenario = null;
            TVI tvi;

            butMikeNewScenarioSave.Enabled = false;

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                panelFileAlreadyInDB.Enabled = true;
                CurrentMikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                if (radioButtonViewMikeScenarioOthers.Checked || radioButtonViewMunicipalityOthers.Checked || radioButtonViewOriginals.Checked)
                {
                    butAddFiles.Enabled = true;
                }
                else
                {
                    butAddFiles.Enabled = false;
                }
            }
            else
            {
                panelFileAlreadyInDB.Enabled = false;
                panelGeneralParameters.Enabled = false;
                panelSources.Enabled = false;
                return;
            }

            if (treeViewItems.SelectedNode != null)
            {
                tvi = (TVI)treeViewItems.SelectedNode.Tag;
            }
            else
            {
                return;
            }

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (CurrentMikeScenario != null)
            {
                switch (CurrentMikeScenario.ScenarioStatus.ToString())
                {
                    case "Created":
                        {
                            panelGeneralParameters.Enabled = true;
                            panelSources.Enabled = true;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = true;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    case "ReadyToRun":
                        {
                            panelGeneralParameters.Enabled = true;
                            panelSources.Enabled = true;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = false;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    case "Running":
                        {
                            panelGeneralParameters.Enabled = false;
                            panelSources.Enabled = false;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = false;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    case "Completed":
                        {
                            panelGeneralParameters.Enabled = false;
                            panelSources.Enabled = false;
                            butMikeNewScenarioCreateFromSelected.Enabled = true;
                            butMikeNewScenarioRun.Enabled = false;
                            if (radioButtonViewKMZResults.Checked)
                            {
                                butGenerateScenarioInputAndResults.Enabled = true;
                                textBoxContourValues.Enabled = true;
                            }
                            else
                            {
                                butGenerateScenarioInputAndResults.Enabled = false;
                                textBoxContourValues.Enabled = false;
                            }
                        }
                        break;
                    case "Error":
                        {
                            panelGeneralParameters.Enabled = true;
                            panelSources.Enabled = true;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = true;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    case "Canceled":
                        {
                            panelGeneralParameters.Enabled = true;
                            panelSources.Enabled = true;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = true;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    case "Changed":
                        {
                            panelGeneralParameters.Enabled = true;
                            panelSources.Enabled = true;
                            butMikeNewScenarioCreateFromSelected.Enabled = false;
                            butMikeNewScenarioRun.Enabled = true;
                            butGenerateScenarioInputAndResults.Enabled = false;
                            textBoxContourValues.Enabled = false;
                        }
                        break;
                    default:
                        break;
                }

                if (radioButtonViewInputs.Checked)
                {
                    // Input files
                    CurrentScenarioFileList = (from msif in vpse.MikeScenarioFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where msif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && msif.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                               && (cf.Purpose == "Input" || cf.Purpose == "InputPol")
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else if (radioButtonViewMikeResults.Checked)
                {
                    // Result files
                    CurrentScenarioFileList = (from msif in vpse.MikeScenarioFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where msif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && msif.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                               && cf.Purpose == "MikeResult"
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else if (radioButtonViewKMZResults.Checked)
                {
                    // Result files
                    CurrentScenarioFileList = (from msif in vpse.MikeScenarioFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where msif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && msif.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                               && cf.Purpose == "KMZResult"
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else if (radioButtonViewOriginals.Checked)
                {
                    // Result files
                    CurrentScenarioFileList = (from msif in vpse.MikeScenarioFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where msif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && msif.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                               && cf.Purpose == "Original"
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else if (radioButtonViewMikeScenarioOthers.Checked)
                {
                    // Other files
                    CurrentScenarioFileList = (from msif in vpse.MikeScenarioFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where msif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && msif.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                               && cf.Purpose == "MikeScenarioOther"
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else if (radioButtonViewMunicipalityOthers.Checked)
                {
                    // Other files
                    CurrentScenarioFileList = (from cif in vpse.CSSPItemFiles
                                               from cf in vpse.CSSPFileNoContents
                                               where cif.CSSPFile.CSSPFileID == cf.CSSPFileID
                                               && cif.CSSPItem.CSSPItemID == tvi.ItemID
                                               && cf.Purpose == "MunicipalityOther"
                                               orderby cf.FileName
                                               select cf).ToList<CSSPFileNoContent>();
                }
                else
                {
                    CurrentScenarioFileList = null;
                }
                dataGridViewScenarioFiles.DataSource = CurrentScenarioFileList;
                //richTextBoxMikePanelStatus.Clear();


                // General Parameters
                textBoxMikeNewScenarioName.Text = CurrentMikeScenario.ScenarioName != null ? CurrentMikeScenario.ScenarioName : "";
                //richTextBoxMikeScenarioDescription.Text = CurrentMikeScenario.ScenarioDescription != null ? CurrentMikeScenario.ScenarioDescription : "";
                if (CurrentMikeScenario.ScenarioStartDateAndTime != null)
                {
                    dateTimePickerScenarioStartDateAndTime.Value = (DateTime)CurrentMikeScenario.ScenarioStartDateAndTime;
                }
                if (CurrentMikeScenario.ScenarioEndDateAndTime != null)
                {
                    dateTimePickerScenarioEndDateAndTime.Value = (DateTime)CurrentMikeScenario.ScenarioEndDateAndTime;
                }
                if (CurrentMikeScenario.ScenarioEndDateAndTime != null && CurrentMikeScenario.ScenarioStartDateAndTime != null)
                {
                    TimeSpan ts = new TimeSpan(CurrentMikeScenario.ScenarioEndDateAndTime.Value.Ticks - CurrentMikeScenario.ScenarioStartDateAndTime.Value.Ticks);

                    lblScenarioLengthDays.Text = string.Format("{0:F0}", ts.Days);
                    lblScenarioLengthHours.Text = string.Format("{0:F0}", ts.Hours);
                    lblScenarioLengthMinutes.Text = string.Format("{0:F0}", ts.Minutes);
                }
                else
                {
                    lblScenarioLengthDays.Text = "";
                    lblScenarioLengthHours.Text = "";
                    lblScenarioLengthMinutes.Text = "";
                }

                CurrentMikeParameter = (from p in vpse.MikeParameters
                                        where p.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                        select p).FirstOrDefault<MikeParameter>();

                if (CurrentMikeParameter == null)
                {
                    MessageBox.Show("Error reading MikeParameter for MikeScenarioID [" + CurrentMikeScenario.MikeScenarioID + "]");
                    return;
                }

                textBoxMikeScenarioDecayFactorPerDay.Text = string.Format("{0:F5}", CurrentMikeParameter.DecayFactorPerDay);
                checkBoxDecayIsConstant.Checked = CurrentMikeParameter.DecayIsConstant.Value;
                textBoxMikeScenarioDecayFactorAmplitude.Text = string.Format("{0:F0}", CurrentMikeParameter.DecayFactorAmplitude);
                textBoxMikeScenarioResultFrequencyInMinutes.Text = string.Format("{0:F0}", CurrentMikeParameter.ResultFrequencyInMinutes);
                textBoxMikeScenarioAmbientTemperature.Text = string.Format("{0:F2}", CurrentMikeParameter.AmbientTemperature);
                textBoxMikeScenarioAmbientSalinity.Text = string.Format("{0:F2}", CurrentMikeParameter.AmbientSalinity);
                textBoxMikeScenarioManningNumber.Text = string.Format("{0:F2}", CurrentMikeParameter.ManningNumber);
                textBoxMikeScenarioWindSpeedKilometerPerHour.Text = string.Format("{0:F2}", CurrentMikeParameter.WindSpeed);
                textBoxMikeScenarioWindSpeedMeterPerSecond.Text = string.Format("{0:F2}", CurrentMikeParameter.WindSpeed / 3.6);
                textBoxMikeScenarioWindDirection.Text = string.Format("{0:F2}", CurrentMikeParameter.WindDirection);
                listBoxMinMaxTideDateAndTime.Items.Clear();

                if (CurrentMikeScenario != null)
                {
                    butScenarioSummary.Enabled = true;
                }
                else
                {
                    butScenarioSummary.Enabled = false;
                }

                CurrentMikeSourceIndex = -1;

                // Sources
                CurrentMikeSourceList = (from s in vpse.MikeSources
                                         where s.MikeScenario.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                         orderby s.SourceName
                                         select s).ToList<MikeSource>();

                comboBoxMikeScenarioSourceName.DataSource = CurrentMikeSourceList;
                comboBoxMikeScenarioSourceName.DisplayMember = "SourceName";
                comboBoxMikeScenarioSourceName.ValueMember = "SourceNumberString";
                comboBoxMikeScenarioSourceName.SelectedIndex = -1;
                if (comboBoxMikeScenarioSourceName.Items.Count > 0)
                {
                    comboBoxMikeScenarioSourceName.SelectedIndex = 0;
                }

                tabControlMikeScenario.Enabled = true;
            }
            else
            {
                tabControlMikeScenario.Enabled = false;
            }
        }
        private void FillDataGridViewMikeScenairosInDB(int SelectMikeScenairoID)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

            string ItemTypeSelected = (from c in vpse.CSSPItems
                                       from ct in vpse.CSSPTypeItems
                                       where c.CSSPTypeItem.CSSPTypeItemID == ct.CSSPTypeItemID
                                       && c.CSSPItemID == CurrentTVI.ItemID
                                       select ct.CSSPTypeText).FirstOrDefault();

            if (ItemTypeSelected == ItemType.Municipality.ToString())
            {
                List<MikeScenario> mikeScenarioList = (from ms in vpse.MikeScenarios
                                                       where ms.CSSPItem.CSSPItemID == CurrentTVI.ItemID
                                                       orderby ms.ScenarioName
                                                       select ms).ToList<MikeScenario>();

                dataGridViewMikeScenairosInDB.DataSource = null;
                dataGridViewMikeScenairosInDB.DataSource = mikeScenarioList;
                if (SelectMikeScenairoID == 0)
                {
                    FillAfterSelectMikeScenario();
                }
                else
                {
                    for (int i = 0; i < dataGridViewMikeScenairosInDB.Rows.Count; i++)
                    {
                        MikeScenario mikeScenario = (MikeScenario)(dataGridViewMikeScenairosInDB.Rows[i].DataBoundItem);
                        if (mikeScenario.MikeScenarioID == SelectMikeScenairoID)
                        {
                            dataGridViewMikeScenairosInDB.Rows[i].Selected = true;
                            break;
                        }

                    }
                }
            }
        }
        private void FindMonthlyHighAndLowTide(TideType tideType)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;

                if (mikeScenario == null)
                {
                    MessageBox.Show("You need to select a scenario before you can find Montly tides");
                    return;
                }

                string purpose = PurposeType.Original.ToString();

                CSSPFile csspFileWaterLevels = (from cf in vpse.CSSPFiles
                                                from msf in vpse.MikeScenarioFiles
                                                where cf.CSSPFileID == msf.CSSPParentFile.CSSPFileID
                                                && msf.MikeScenarioID == mikeScenario.MikeScenarioID
                                                && cf.Purpose == purpose
                                                && cf.ParameterNames == "[WaterLevel]"
                                                select cf).FirstOrDefault<CSSPFile>();

                if (csspFileWaterLevels == null)
                {
                    MessageBox.Show("Could not find any water level files for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]");
                    return;
                }

                Dfs dfsWL = new Dfs(Dfs.DFSType.DFS0, true);

                MemoryStream ms = new MemoryStream(csspFileWaterLevels.FileContent);

                dfsWL.StreamToDfs(ms);

                DateTime StartUpDate = new DateTime();

                StartUpDate = dfsWL.DataStartDate;

                List<Peaks> PeakList = new List<Peaks>();

                Direction direction = new Direction();

                if (dfsWL.ParameterList[0].ValueList[0] > dfsWL.ParameterList[0].ValueList[1])
                {
                    direction = Direction.Down;
                }
                else
                {
                    direction = Direction.Up;
                }

                for (int i = 1; i < dfsWL.ParameterList[0].ValueList.Count - 2; i++)
                {
                    if (dfsWL.ParameterList[0].ValueList[i] > dfsWL.ParameterList[0].ValueList[i + 1])
                    {
                        if (direction == Direction.Up)
                        {
                            PeakList.Add(new Peaks() { Date = dfsWL.DataStartDate.AddSeconds(dfsWL.TimeSteps * i), Value = dfsWL.ParameterList[0].ValueList[i] });
                            direction = Direction.Down;
                        }
                    }
                    else
                    {
                        if (direction == Direction.Down)
                        {
                            PeakList.Add(new Peaks() { Date = dfsWL.DataStartDate.AddSeconds(dfsWL.TimeSteps * i), Value = dfsWL.ParameterList[0].ValueList[i] });
                            direction = Direction.Up;
                        }
                    }
                }

                List<PeakDifference> PeakDiffList = new List<PeakDifference>();

                for (int i = 0; i < PeakList.Count - 1; i++)
                {
                    PeakDiffList.Add(new PeakDifference() { StartDate = PeakList[i].Date, EndDate = PeakList[i + 1].Date, Value = Math.Abs(PeakList[i].Value - PeakList[i + 1].Value) });
                }

                float TempFloat;
                if (!float.TryParse(textBoxHighLowTideSpanInDays.Text, out TempFloat))
                {
                    MessageBox.Show("Please enter a valid value for days.");
                    return;
                }

                float Hours = TempFloat * 24;
                int PeakDiffNumber = (int)(Hours / 6.2) == 0 ? 1 : (int)(Hours / 6.2);

                List<PeakDifference> MovingAverageOfPeakDiffList = new List<PeakDifference>();

                for (int i = 0; i < PeakDiffList.Count - PeakDiffNumber; i++)
                {
                    float Average = 0;
                    for (int j = 0; j < PeakDiffNumber; j++)
                    {
                        Average += PeakDiffList[i + j].Value;
                    }
                    Average = Average / PeakDiffNumber;
                    MovingAverageOfPeakDiffList.Add(new PeakDifference() { StartDate = PeakDiffList[i].StartDate, EndDate = PeakDiffList[i + PeakDiffNumber - 1].EndDate, Value = Average });
                }

                List<PeakDifference> MonthlyPeaks = new List<PeakDifference>();
                for (int i = 1; i < 13; i++)
                {
                    switch (tideType)
                    {
                        case TideType.Low:
                            {
                                PeakDifference peakDifference = (from pd in MovingAverageOfPeakDiffList
                                                                 where pd.StartDate.Month == i
                                                                 orderby pd.Value
                                                                 select pd).FirstOrDefault<PeakDifference>();

                                MonthlyPeaks.Add(peakDifference);
                            }
                            break;
                        case TideType.High:
                            {
                                PeakDifference peakDifference = (from pd in MovingAverageOfPeakDiffList
                                                                 where pd.StartDate.Month == i
                                                                 orderby pd.Value descending
                                                                 select pd).FirstOrDefault<PeakDifference>();

                                MonthlyPeaks.Add(peakDifference);
                            }
                            break;
                        default:
                            break;
                    }
                }

                listBoxMinMaxTideDateAndTime.Items.Clear();
                for (int i = 0; i < MonthlyPeaks.Count; i++)
                {
                    try
                    {
                        listBoxMinMaxTideDateAndTime.Items.Add(string.Format("{1:MMM} average {0:F2} - {1:yyyy/MM/dd HH:mm} - {2:yyyy/MM/dd HH:mm}", MonthlyPeaks[i].Value, MonthlyPeaks[i].StartDate, MonthlyPeaks[i].EndDate));
                    }
                    catch (Exception)
                    {
                        // error is cause if user has selected more than a month
                    }
                }

            }
            else
            {
                MessageBox.Show("You need to select a scenario before you can find Montly tides");
                return;
            }
            // reading
        }
        private string GenerateInputSummary(MikeScenario mikeScenario)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            int NumbOfCharToPush = 35;
            StringBuilder sbReturn = new StringBuilder();

            // Scenario General
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Name:", NumbOfCharToPush) + "\t{0}\r\n", mikeScenario.ScenarioName != null ? mikeScenario.ScenarioName : "empty"));
            //TheRichTextBoxMikePanelStatus.AppendText(string.Format(ReturnStrLimit("Scenario Description:", NumbOfCharToPush) + "\t{0}\r\n", mikeScenario.ScenarioDescription != null ? mikeScenario.ScenarioDescription : "empty"));
            if (mikeScenario.ScenarioStartDateAndTime != null)
            {
                sbReturn.Append(string.Format(ReturnStrLimit("Scenario Start Date and Time:", NumbOfCharToPush) + "\t{0:yyyy/MM/dd HH:mm:ss tt}\r\n", mikeScenario.ScenarioStartDateAndTime));
            }
            else
            {
                sbReturn.Append(string.Format(ReturnStrLimit("Scenario Start Date and Time:", NumbOfCharToPush) + "\tNot Set\r\n"));
            }
            if (mikeScenario.ScenarioEndDateAndTime != null && mikeScenario.ScenarioStartDateAndTime != null)
            {
                TimeSpan ts = new TimeSpan(mikeScenario.ScenarioEndDateAndTime.Value.Ticks - mikeScenario.ScenarioStartDateAndTime.Value.Ticks);

                sbReturn.Append(string.Format(ReturnStrLimit("Scenario Length:", NumbOfCharToPush) + "\t{0:F0} days {1:F0} hours {2:F0} minutes\r\n\r\n", ts.Days, ts.Hours, ts.Minutes));
            }
            else
            {
                sbReturn.Append(string.Format(ReturnStrLimit("Scenario Length:", NumbOfCharToPush) + "\t{0:F0} days {1:F0} hours {2:F0} minutes\r\n\r\n", 0, 0, 0));
            }

            MikeParameter mikeParameter = (from p in vpse.MikeParameters where p.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID select p).FirstOrDefault<MikeParameter>();

            if (mikeParameter == null)
            {
                MessageBox.Show("Error reading MikeParameter for MikeScenarioID [" + mikeScenario.MikeScenarioID + "]");
                return null;
            }

            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Decay Factor:", NumbOfCharToPush) + "\t{0:F5} (/day)\t{1:F8} (/second)\r\n", mikeParameter.DecayFactorPerDay, mikeParameter.DecayFactorPerDay / 24 / 3600));
            sbReturn.Append(string.Format(ReturnStrLimit("Decay Is Constant:", NumbOfCharToPush) + "\t{0}\r\n", mikeParameter.DecayIsConstant.ToString()));
            if (!mikeParameter.DecayIsConstant.Value)
            {
                sbReturn.Append(string.Format(ReturnStrLimit("Scenario Decay Factor Amplitude:", NumbOfCharToPush) + "\t{0:F5}\r\n", mikeParameter.DecayFactorAmplitude));
            }
            sbReturn.Append(string.Format(ReturnStrLimit("Result Frequency:", NumbOfCharToPush) + "\t{0:F0} minutes\r\n", mikeParameter.ResultFrequencyInMinutes));
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Ambient Temperature:", NumbOfCharToPush) + "\t{0:F2} (Celcius)\r\n", mikeParameter.AmbientTemperature));
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Ambient Salinity:", NumbOfCharToPush) + "\t{0:F2} (PSU)\r\n", mikeParameter.AmbientSalinity));
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Manning Number:", NumbOfCharToPush) + "\t{0:F2} (m^(1/3)/s)\r\n", mikeParameter.ManningNumber));
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Wind Speed:", NumbOfCharToPush) + "\t{0:F2} (km/h)\t{1:F2} (m/s)\r\n", mikeParameter.WindSpeed, mikeParameter.WindSpeed / 3.6));
            sbReturn.Append(string.Format(ReturnStrLimit("Scenario Wind Direction:", NumbOfCharToPush) + "\t{0:F2} (Degrees)\r\n\r\n", mikeParameter.WindDirection));

            // Sources

            List<MikeSource> mikeSourceList = (from s in vpse.MikeSources where s.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID select s).ToList<MikeSource>();

            if (mikeSourceList == null)
            {
                MessageBox.Show("Error reading MikeSources for MikeScenarioID [" + mikeScenario.MikeScenarioID + "]");
                return null;
            }

            int CountSource = 0;
            foreach (MikeSource ms in mikeSourceList)
            {
                CountSource += 1;
                sbReturn.Append(string.Format(ReturnStrLimit("\r\nSources", 15) + " ({0})\r\n", CountSource));
                sbReturn.Append(string.Format(ReturnStrLimit("Name:", NumbOfCharToPush) + "\t{0}\r\n", ms.SourceName));
                sbReturn.Append(string.Format(ReturnStrLimit("Is included:", NumbOfCharToPush) + "\t{0}\r\n", ms.Include.ToString()));
                sbReturn.Append(string.Format(ReturnStrLimit("Flow:", NumbOfCharToPush) + "\t{0:F2} (m3/d)      {1:F8} (m3/s)\r\n", ms.SourceFlow, ms.SourceFlow / 24 / 3600));
                sbReturn.Append(string.Format(ReturnStrLimit("Pollution:", NumbOfCharToPush) + "\t{0:F0} (FC MPN / 100 ml)\r\n", ms.SourcePollution));
                sbReturn.Append(string.Format(ReturnStrLimit("Is Continuous:", NumbOfCharToPush) + "\t{0}\r\n", ms.IsContinuous.ToString()));
                if (!(bool)ms.IsContinuous)
                {
                    sbReturn.Append(string.Format(ReturnStrLimit("Spill Start Date:", NumbOfCharToPush + 4) + "\t{0:yyyy/MM/dd HH:mm:ss tt}\r\n", ms.StartDateAndTime));
                    sbReturn.Append(string.Format(ReturnStrLimit("Spill End Date:", NumbOfCharToPush + 4) + "\t{0:yyyy/MM/dd HH:mm:ss tt}\r\n", ms.EndDateAndTime));
                }
                sbReturn.Append(string.Format(ReturnStrLimit("Temperature:", NumbOfCharToPush) + "\t{0:F2} (Celcius)\r\n", ms.SourceTemperature));
                sbReturn.Append(string.Format(ReturnStrLimit("Salinity:", NumbOfCharToPush) + "\t{0:F2} (PSU)\r\n", ms.SourceSalinity));
                sbReturn.Append(string.Format(ReturnStrLimit("Latitude:", NumbOfCharToPush) + "\t{0:F8}\r\n", ms.SourceLat));
                sbReturn.Append(string.Format(ReturnStrLimit("Longitude:", NumbOfCharToPush) + "\t{0:F8}\r\n\r\n", ms.SourceLong));
            }
            return sbReturn.ToString();
        }
        private bool GetAllInputFilesToUpload(string m21fmFileName, List<string> FileNameList, MikeScenario NewMikeScenario)
        {
            FileInfo fi;
            string[] dfsToChange = { ".dfs0", ".dfs1", ".mesh" };
            try
            {
                // trying to find the file that created the .mesh file i.e. .mdf
                // keep this before doing domain .mesh
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.domain.file_name.Substring(0, m21fm.femEngineHD.domain.file_name.LastIndexOf(".")) + ".mdf|", FileNameList);

                // doing domain .mesh
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.domain.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.domain.file_name = GetRightFileName(m21fm.femEngineHD.domain.file_name, NewMikeScenario.MikeScenarioID);


                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.domain.gis_background.file_Name, FileNameList);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.constant_eddy_formulation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.constant_eddy_formulation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.constant_eddy_formulation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.smagorinsky_formulation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.smagorinsky_formulation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.horizontal_eddy_viscosity.smagorinsky_formulation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.vertical_eddy_viscosity.constant_eddy_formulation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.vertical_eddy_viscosity.constant_eddy_formulation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.eddy_viscosity.vertical_eddy_viscosity.constant_eddy_formulation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.bed_resistance.drag_coefficient.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.bed_resistance.drag_coefficient.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.bed_resistance.drag_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.bed_resistance.chezy_number.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.bed_resistance.chezy_number.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.bed_resistance.chezy_number.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.bed_resistance.roughness.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.bed_resistance.roughness.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.bed_resistance.roughness.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.wind_forcing.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.wind_forcing.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.wind_forcing.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.ice.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.ice.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.ice.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.ice.roughness.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.ice.roughness.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.ice.roughness.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.tidal_potential.constituent_file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.tidal_potential.constituent_file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.tidal_potential.constituent_file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.precipitation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.precipitation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.precipitation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.evaporation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.evaporation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.precipitation_evaporation.evaporation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.radiation_stress.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.radiation_stress.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.radiation_stress.file_name, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.hydrodynamic_module.sources != null && m21fm.femEngineHD.hydrodynamic_module.sources.source != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.hydrodynamic_module.sources.source)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.file_name = GetRightFileName(kvp.Value.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.structure_module.crosssections.CrossSectionFile, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.structure_module.crosssections.CrossSectionFile = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.structure_module.crosssections.CrossSectionFile, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.hydrodynamic_module.structures.gates != null && m21fm.femEngineHD.hydrodynamic_module.structures.gates.gate != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.STRUCTURES.GATES.GATE> kvp in m21fm.femEngineHD.hydrodynamic_module.structures.gates.gate)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.input_file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.input_file_name = GetRightFileName(kvp.Value.input_file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.file_name = GetRightFileName(kvp.Value.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.hydrodynamic_module.structures.turbines != null && m21fm.femEngineHD.hydrodynamic_module.structures.turbines.turbine != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.STRUCTURES.TURBINES.TURBINE> kvp in m21fm.femEngineHD.hydrodynamic_module.structures.turbines.turbine)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.correction_factor.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.correction_factor.file_name = GetRightFileName(kvp.Value.correction_factor.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.hydrodynamic_module.boundary_conditions != null && m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.BOUNDARY_CONDITIONS.CODE> kvp in m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.file_name = GetRightFileName(kvp.Value.file_name, NewMikeScenario.MikeScenarioID);
                        if (kvp.Value.data != null)
                        {
                            foreach (M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.BOUNDARY_CONDITIONS.CODE.DATA bc in kvp.Value.data)
                            {
                                fi = AddFileToFileNameList(m21fmFileName, bc.file_name, FileNameList);
                                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                                    bc.file_name = GetRightFileName(bc.file_name, NewMikeScenario.MikeScenarioID);
                            }
                        }
                    }
                }
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.scaled_eddy_viscosity.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.scaled_eddy_viscosity.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.scaled_eddy_viscosity.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.diffusion_coefficient.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.diffusion_coefficient.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.horizontal_diffusion.diffusion_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.scaled_eddy_viscosity.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.scaled_eddy_viscosity.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.scaled_eddy_viscosity.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.diffusion_coefficient.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.diffusion_coefficient.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.diffusion.vertical_diffusion.diffusion_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.air_temperature.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.air_temperature.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.air_temperature.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.relative_humidity.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.relative_humidity.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.relative_humidity.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.clearness_coefficient.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.clearness_coefficient.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.heat_exchange.clearness_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.precipitation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.precipitation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.precipitation.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.evaporation.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.evaporation.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.precipitation_evaporation.evaporation.file_name, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources != null && m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.temperature.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.temperature.file_name = GetRightFileName(kvp.Value.temperature.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.salinity.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.salinity.file_name = GetRightFileName(kvp.Value.salinity.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.temperature.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.temperature.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.temperature.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.salinity.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.salinity.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.initial_conditions.salinity.file_name, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.boundary_conditions != null && m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.boundary_conditions.code != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.BOUNDARY_CONDITIONS.CODE> kvp in m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.boundary_conditions.code)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.temperature.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.temperature.file_name = GetRightFileName(kvp.Value.temperature.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.salinity.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.salinity.file_name = GetRightFileName(kvp.Value.salinity.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources != null && m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.source != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.source)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.kinetic_energy.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.kinetic_energy.file_name = GetRightFileName(kvp.Value.kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.dissipation_of_kinetic_energy.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.dissipation_of_kinetic_energy.file_name = GetRightFileName(kvp.Value.dissipation_of_kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.kinetic_energy.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.kinetic_energy.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.dissipation_of_kinetic_energy.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.dissipation_of_kinetic_energy.file_name = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.turbulence_module.initial_conditions.dissipation_of_kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.hydrodynamic_module.turbulence_module.boundary_conditions != null && m21fm.femEngineHD.hydrodynamic_module.turbulence_module.boundary_conditions.code != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.BOUNDARY_CONDITIONS.CODE> kvp in m21fm.femEngineHD.hydrodynamic_module.turbulence_module.boundary_conditions.code)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.kinetic_energy.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.kinetic_energy.file_name = GetRightFileName(kvp.Value.kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.dissipation_of_kinetic_energy.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.dissipation_of_kinetic_energy.file_name = GetRightFileName(kvp.Value.dissipation_of_kinetic_energy.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_flux, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_flux = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_flux, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_area, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_area = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_area, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_volume, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_volume = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.decoupling.file_name_volume, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.decoupling.specification_file, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.hydrodynamic_module.decoupling.specification_file = GetRightFileName(m21fm.femEngineHD.hydrodynamic_module.decoupling.specification_file, NewMikeScenario.MikeScenarioID);
                fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name, FileNameList);
                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                    m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name = GetRightFileName(m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name, NewMikeScenario.MikeScenarioID);
                if (m21fm.femEngineHD.transport_module.dispersion.horizontal_dispersion != null && m21fm.femEngineHD.transport_module.dispersion.horizontal_dispersion.component != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DISPERSION.HORIZONTAL_DISPERSION.COMPONENT> kvp in m21fm.femEngineHD.transport_module.dispersion.horizontal_dispersion.component)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.scaled_eddy_viscosity.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.scaled_eddy_viscosity.file_name = GetRightFileName(kvp.Value.scaled_eddy_viscosity.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.dispersion_coefficient.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.dispersion_coefficient.file_name = GetRightFileName(kvp.Value.dispersion_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.transport_module.dispersion.vertical_dispersion != null && m21fm.femEngineHD.transport_module.dispersion.vertical_dispersion.component != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DISPERSION.VERTICAL_DISPERSION.COMPONENT> kvp in m21fm.femEngineHD.transport_module.dispersion.vertical_dispersion.component)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.scaled_eddy_viscosity.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.scaled_eddy_viscosity.file_name = GetRightFileName(kvp.Value.scaled_eddy_viscosity.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.dispersion_coefficient.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.dispersion_coefficient.file_name = GetRightFileName(kvp.Value.dispersion_coefficient.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.transport_module.decay != null && m21fm.femEngineHD.transport_module.decay.component != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DECAY.COMPONENT> kvp in m21fm.femEngineHD.transport_module.decay.component)
                    {
                        if (kvp.Value.format != 0) // This should never happen. It was stopped in the Addm21fmFileInDB
                        {
                            fi = AddFileToFileNameList(m21fmFileName, kvp.Value.file_name, FileNameList);
                            if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                                kvp.Value.file_name = GetRightFileName(kvp.Value.file_name, NewMikeScenario.MikeScenarioID);
                        }
                    }
                }
                if (m21fm.femEngineHD.transport_module.precipitation_evaporation != null && m21fm.femEngineHD.transport_module.precipitation_evaporation.component != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.PRECIPITATION_EVAPORATION.COMPONENT> kvp in m21fm.femEngineHD.transport_module.precipitation_evaporation.component)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.precipitation.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.precipitation.file_name = GetRightFileName(kvp.Value.precipitation.file_name, NewMikeScenario.MikeScenarioID);
                        fi = AddFileToFileNameList(m21fmFileName, kvp.Value.evaporation.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            kvp.Value.evaporation.file_name = GetRightFileName(kvp.Value.evaporation.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.transport_module.initial_conditions != null && m21fm.femEngineHD.transport_module.initial_conditions.component != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.INITIAL_CONDITIONS.COMPONENT> kvp in m21fm.femEngineHD.transport_module.initial_conditions.component)
                    {
                        fi = AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name, FileNameList);
                        if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                            m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name = GetRightFileName(m21fm.femEngineHD.transport_module.hydrodynamic_conditions.file_name, NewMikeScenario.MikeScenarioID);
                    }
                }
                if (m21fm.femEngineHD.transport_module.boundary_conditions != null && m21fm.femEngineHD.transport_module.boundary_conditions.code != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.BOUNDARY_CONDITIONS.CODE> kvp in m21fm.femEngineHD.transport_module.boundary_conditions.code)
                    {
                        if (kvp.Value.component != null)
                        {
                            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.BOUNDARY_CONDITIONS.CODE.COMPONENT> kvp2 in kvp.Value.component)
                            {
                                fi = AddFileToFileNameList(m21fmFileName, kvp2.Value.file_name, FileNameList);
                                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                                    kvp2.Value.file_name = GetRightFileName(kvp2.Value.file_name, NewMikeScenario.MikeScenarioID);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;

        }
        private bool GetAllInputPollutionFilesToUpload(string m21fmFileName, List<string> FileNameList, MikeScenario NewMikeScenario)
        {
            FileInfo fi;
            string[] dfsToChange = { ".dfs0", ".dfs1" };
            try
            {
                if (m21fm.femEngineHD.transport_module.sources != null && m21fm.femEngineHD.transport_module.sources.source != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE> kvp in m21fm.femEngineHD.transport_module.sources.source)
                    {
                        if (kvp.Value.component != null)
                        {
                            foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT> kvp2 in kvp.Value.component)
                            {
                                fi = AddFileToFileNameList(m21fmFileName, kvp2.Value.file_name, FileNameList);
                                if (fi != null && dfsToChange.Contains(fi.Extension.ToLower()))
                                    kvp2.Value.file_name = GetRightSourceFileName(kvp.Value.name, kvp2.Value.file_name, NewMikeScenario.MikeScenarioID);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;

        }
        private bool GetAllResultFilesToUpload(string m21fmFileName, List<string> FileNameList, MikeScenario NewMikeScenario)
        {
            try
            {
                AddFileToFileNameList(m21fmFileName, m21fm.femEngineHD.hydrodynamic_module.structures.turbines.output_file_name, FileNameList);
                if (m21fm.femEngineHD.hydrodynamic_module.outputs != null && m21fm.femEngineHD.hydrodynamic_module.outputs.output != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.OUTPUTS.OUTPUT> kvp in m21fm.femEngineHD.hydrodynamic_module.outputs.output)
                    {
                        FileInfo fi = new FileInfo(m21fmFileName.Substring(0, m21fmFileName.LastIndexOf("\\") + 1)
                            + m21fm.system.ResultRootFolder.Substring(1, m21fm.system.ResultRootFolder.Length - 2)
                            + m21fmFileName.Substring(m21fmFileName.LastIndexOf("\\"))
                            + " - Result Files\\");
                        AddFileToFileNameList(fi.FullName, kvp.Value.file_name, FileNameList);
                        AddFileToFileNameList(fi.FullName, kvp.Value.input_file_name, FileNameList);
                    }
                }
                if (m21fm.femEngineHD.transport_module.outputs != null && m21fm.femEngineHD.transport_module.outputs.output != null)
                {
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.OUTPUTS.OUTPUT> kvp in m21fm.femEngineHD.transport_module.outputs.output)
                    {
                        FileInfo fi = new FileInfo(m21fmFileName.Substring(0, m21fmFileName.LastIndexOf("\\") + 1)
                            + m21fm.system.ResultRootFolder.Substring(1, m21fm.system.ResultRootFolder.Length - 2)
                            + m21fmFileName.Substring(m21fmFileName.LastIndexOf("\\"))
                            + " - Result Files\\");
                        AddFileToFileNameList(fi.FullName, kvp.Value.file_name, FileNameList);
                        AddFileToFileNameList(fi.FullName, kvp.Value.input_file_name, FileNameList);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;

        }
        private string GetRightFileName(string FileName, int MikeScenarioID)
        {
            string RetValue = "";
            if (FileName.LastIndexOf("\\") != -1)
            {
                RetValue = FileName.Insert(FileName.LastIndexOf("\\") + 1, string.Format("[{0}] ", MikeScenarioID));
            }
            else
            {
                RetValue = FileName;
            }
            return RetValue;
        }
        private string GetRightSourceFileName(string SourceName, string FileName, int MikeScenarioID)
        {
            string RetValue = "";
            if (FileName.LastIndexOf("\\") != -1)
            {
                RetValue = FileName.Substring(0, FileName.LastIndexOf("\\") + 1) + string.Format("[{0}] Pol [{1}]", MikeScenarioID, SourceName.Substring(1, SourceName.Length - 2)) + FileName.Substring(FileName.LastIndexOf("."));
            }
            else
            {
                RetValue = FileName;
            }
            return RetValue;
        }
        private MemoryStream KMZToKML(MemoryStream ms)
        {
            MemoryStream msUnzipped = new MemoryStream();
            try
            {
                using (ZipInputStream ZipStream = new ZipInputStream(ms))
                {
                    ZipEntry theEntry;
                    while ((theEntry = ZipStream.GetNextEntry()) != null) // should only have one for our KMZ files
                    {
                        if (theEntry.IsFile)
                        {
                            if (theEntry.Name != "")
                            {
                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = ZipStream.Read(data, 0, data.Length);
                                    if (size > 0)
                                        msUnzipped.Write(data, 0, size);
                                    else
                                        break;
                                }
                            }
                        }
                    }
                    ZipStream.Close();
                }
            }
            catch (Exception)
            {
                return null;
            }
            return msUnzipped;
        }
        private void MikeNewScenarioCreateFromSelected()
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Trying to create a new Scenario ...\r\n");
            Application.DoEvents();

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI tvi = (TVI)treeViewItems.SelectedNode.Tag;

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                if (mikeScenario != null)
                {
                    richTextBoxMikePanelStatus.AppendText(string.Format("Duplicating Scenario: {0}\r\n", mikeScenario.ScenarioName));
                    Application.DoEvents();

                    CSSPItem csspItem = (from ci in vpse.CSSPItems
                                         where ci.CSSPItemID == tvi.ItemID
                                         select ci).FirstOrDefault<CSSPItem>();

                    if (csspItem == null)
                    {
                        MessageBox.Show("Could not find CSSPItem with CSSPItemID = [" + tvi.ItemID + "]\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not find CSSPItem with CSSPItemID = [" + tvi.ItemID + "]\r\n");
                        return;
                    }

                    // checking to see if a scenario with that name already exist in the DB
                    bool ScenarioExist = true;
                    int ExistCount = 0;
                    string CopyOf = "";

                    while (ScenarioExist)
                    {
                        ExistCount += 1;
                        CopyOf = string.Format("{0} copy of ", ExistCount.ToString());

                        MikeScenario MikeScenarioExist = (from ms in vpse.MikeScenarios
                                                          where ms.ScenarioName == CopyOf + mikeScenario.ScenarioName
                                                          select ms).FirstOrDefault<MikeScenario>();

                        if (MikeScenarioExist != null)
                        {
                            richTextBoxMikePanelStatus.AppendText("A Scenario with the name = [" + CopyOf + mikeScenario.ScenarioName + "] already exist.\r\n");
                        }
                        else
                        {
                            ScenarioExist = false;
                        }
                    }

                    MikeScenario NewMikeScenario = new MikeScenario();
                    NewMikeScenario.CSSPItem = csspItem;
                    NewMikeScenario.ScenarioName = CopyOf + mikeScenario.ScenarioName;
                    NewMikeScenario.ScenarioSummary = mikeScenario.ScenarioSummary;
                    NewMikeScenario.ScenarioStatus = ScenarioStatusType.Created.ToString();
                    NewMikeScenario.ScenarioStartDateAndTime = mikeScenario.ScenarioStartDateAndTime;
                    NewMikeScenario.ScenarioEndDateAndTime = mikeScenario.ScenarioEndDateAndTime;

                    List<MikeParameter> mikeParameterList = (from mp in vpse.MikeParameters
                                                             where mp.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                             select mp).ToList<MikeParameter>();

                    foreach (MikeParameter mp in mikeParameterList)
                    {
                        MikeParameter NewMikeParameter = new MikeParameter();
                        NewMikeParameter.WindSpeed = mp.WindSpeed;
                        NewMikeParameter.WindDirection = mp.WindDirection;
                        NewMikeParameter.DecayFactorPerDay = mp.DecayFactorPerDay;
                        NewMikeParameter.DecayIsConstant = mp.DecayIsConstant;
                        NewMikeParameter.DecayFactorAmplitude = mp.DecayFactorAmplitude;
                        NewMikeParameter.ResultFrequencyInMinutes = mp.ResultFrequencyInMinutes;
                        NewMikeParameter.AmbientTemperature = mp.AmbientTemperature;
                        NewMikeParameter.AmbientSalinity = mp.AmbientSalinity;
                        NewMikeParameter.ManningNumber = mp.ManningNumber;


                        NewMikeScenario.MikeParameters.Add(NewMikeParameter);
                    }

                    List<MikeSource> mikeSourceList = (from ms in vpse.MikeSources
                                                       where ms.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                       select ms).ToList<MikeSource>();

                    foreach (MikeSource ms in mikeSourceList)
                    {
                        MikeSource NewMikeSource = new MikeSource();
                        NewMikeSource.SourceNumberString = ms.SourceNumberString;
                        NewMikeSource.SourceName = ms.SourceName;
                        NewMikeSource.Include = ms.Include;
                        NewMikeSource.SourceFlow = ms.SourceFlow;
                        NewMikeSource.IsContinuous = ms.IsContinuous;
                        NewMikeSource.StartDateAndTime = ms.StartDateAndTime;
                        NewMikeSource.EndDateAndTime = ms.EndDateAndTime;
                        NewMikeSource.SourcePollution = ms.SourcePollution;
                        NewMikeSource.SourceTemperature = ms.SourceTemperature;
                        NewMikeSource.SourceSalinity = ms.SourceSalinity;
                        NewMikeSource.SourceLat = ms.SourceLat;
                        NewMikeSource.SourceLong = ms.SourceLong;

                        NewMikeScenario.MikeSources.Add(NewMikeSource);
                    }

                    try
                    {
                        vpse.AddToMikeScenarios(NewMikeScenario);
                        vpse.SaveChanges();
                        richTextBoxMikePanelStatus.AppendText("New MikeScenario created with name = [" + NewMikeScenario.ScenarioName + "]\r\n");
                    }
                    catch (Exception)
                    {
                        richTextBoxMikePanelStatus.AppendText("Error while creating new MikeScenario with name = [" + NewMikeScenario.ScenarioName + "]\r\n");
                    }

                    // linking All files associated with the mikeScenario that is being copied to the newMikeScenario created                    
                    var mikeScenarioFileListAndCsspFileID = (from cf in vpse.CSSPFiles
                                                             from msf in vpse.MikeScenarioFiles
                                                             where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                                             && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                             && msf.CSSPFile.Purpose == "Original"
                                                             select new { msf, csspFileID = cf.CSSPFileID, csspParentFileID = msf.CSSPParentFile.CSSPFileID }).ToList();

                    foreach (var msfandcfid in mikeScenarioFileListAndCsspFileID)
                    {
                        MikeScenarioFile NewMikeScenarioFileOther = new MikeScenarioFile();
                        NewMikeScenarioFileOther.MikeScenario = NewMikeScenario;

                        CSSPFile csspFileTemp = (from cf in vpse.CSSPFiles
                                                 where cf.CSSPFileID == msfandcfid.csspFileID
                                                 select cf).FirstOrDefault<CSSPFile>();

                        if (csspFileTemp == null)
                        {
                            MessageBox.Show("Could not find CSSPFile with CSSPFileID = [" + msfandcfid.csspFileID + "].\r\n");
                            return;
                        }

                        NewMikeScenarioFileOther.CSSPFile = csspFileTemp;

                        CSSPFile csspParentFileTemp = (from cf in vpse.CSSPFiles
                                                       where cf.CSSPFileID == msfandcfid.csspFileID
                                                       select cf).FirstOrDefault<CSSPFile>();

                        if (csspParentFileTemp == null)
                        {
                            MessageBox.Show("Could not find CSSPFile with CSSPParentFileID = [" + msfandcfid.csspParentFileID + "].\r\n");
                            return;
                        }

                        NewMikeScenarioFileOther.CSSPParentFile = csspParentFileTemp;

                        try
                        {
                            vpse.AddToMikeScenarioFiles(NewMikeScenarioFileOther);
                            vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                            richTextBoxMikePanelStatus.AppendText("MikeScenarioFiles link created.\r\n");
                        }
                        catch (Exception ex)
                        {
                            richTextBoxMikePanelStatus.AppendText("Error while created new MikeScenarioFiles link.\r\n" + ex.Message + "\r\n");
                        }

                    }


                    richTextBoxMikePanelStatus.AppendText("Linking Mesh.kmz, StudyArea.kmz and BathymetryFromMesh.kmz to new scenario if these files exist.\r\n");

                    // need to link Mesh.kmz, StudyArea.kmz and BathymetryFromMesh.kmz
                    List<CSSPFile> csspFileResultList = (from cf in vpse.CSSPFiles
                                                         from msf in vpse.MikeScenarioFiles
                                                         where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                                         && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                         && msf.CSSPFile.Purpose == "KMZResult"
                                                         select cf).ToList<CSSPFile>();

                    foreach (CSSPFile cf in csspFileResultList)
                    {
                        List<string> FileNameList = new List<string>() { "Mesh.kmz", "StudyArea.kmz", "BathymetryFromMesh.kmz" };

                        if (FileNameList.Contains(cf.FileName))
                        {
                            MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                            NewMikeScenarioFile.MikeScenario = NewMikeScenario;
                            NewMikeScenarioFile.CSSPFile = cf;
                            NewMikeScenarioFile.CSSPParentFile = cf;

                            try
                            {
                                richTextBoxMikePanelStatus.AppendText("Linking file [" + cf.FileName + "].\r\n");
                                vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                                vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                                richTextBoxMikePanelStatus.AppendText("File linked.\r\n");
                            }
                            catch (Exception)
                            {
                                richTextBoxMikePanelStatus.AppendText("Error: file [" + cf.FileName + "] could not be linked to new scenario\r\n");
                            }
                        }
                    }


                    // need to create copies of all the input files including .m21fm, .dfs0, .dfs1
                    List<CSSPFile> csspFileInputList = (from cf in vpse.CSSPFiles
                                                        from msf in vpse.MikeScenarioFiles
                                                        where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                                        && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                        && msf.CSSPFile.Purpose == "Input"
                                                        select cf).ToList<CSSPFile>();

                    foreach (CSSPFile cf in csspFileInputList)
                    {
                        CSSPFile NewInputCSSPFile = new CSSPFile();
                        NewInputCSSPFile.CSSPGuid = Guid.NewGuid();
                        if (cf.FileName.ToLower().EndsWith(".m21fm"))
                        {
                            NewInputCSSPFile.FileName = CopyOf + cf.FileName;
                            StringBuilder sb = new StringBuilder();
                            sb.Append(Encoding.ASCII.GetChars(cf.FileContent));
                            sb = sb.Replace("[" + mikeScenario.MikeScenarioID.ToString().Trim() + "]", "[" + NewMikeScenario.MikeScenarioID.ToString().Trim() + "]");
                            NewInputCSSPFile.FileContent = Encoding.ASCII.GetBytes(sb.ToString().ToCharArray());
                        }
                        else
                        {
                            NewInputCSSPFile.FileName = cf.FileName.Replace("[" + mikeScenario.MikeScenarioID.ToString().Trim() + "]", "[" + NewMikeScenario.MikeScenarioID.ToString().Trim() + "]");
                            NewInputCSSPFile.FileContent = cf.FileContent;
                        }
                        NewInputCSSPFile.FileOriginalPath = cf.FileOriginalPath;
                        NewInputCSSPFile.Purpose = cf.Purpose;
                        NewInputCSSPFile.FileDescription = cf.FileDescription;
                        NewInputCSSPFile.FileType = cf.FileType;
                        NewInputCSSPFile.FileSize = cf.FileSize;
                        NewInputCSSPFile.FileCreatedDate = cf.FileCreatedDate;
                        NewInputCSSPFile.IsCompressed = cf.IsCompressed;
                        NewInputCSSPFile.DataStartDate = cf.DataStartDate;
                        NewInputCSSPFile.DataEndDate = cf.DataEndDate;
                        NewInputCSSPFile.TimeStepsInSecond = cf.TimeStepsInSecond;
                        NewInputCSSPFile.ParameterNames = cf.ParameterNames;
                        NewInputCSSPFile.ParameterUnits = cf.ParameterUnits;

                        try
                        {
                            vpse.AddToCSSPFiles(NewInputCSSPFile);
                            vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                            richTextBoxMikePanelStatus.AppendText("Copy of file created = [" + NewInputCSSPFile.FileName + "]\r\n");
                        }
                        catch (Exception)
                        {
                            richTextBoxMikePanelStatus.AppendText("Error while copying new scenario file.\r\n");
                        }


                        MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                        NewMikeScenarioFile.MikeScenario = NewMikeScenario;
                        NewMikeScenarioFile.CSSPFile = NewInputCSSPFile;

                        if (cf.FileType == ".m21fm")
                        {
                            NewMikeScenarioFile.CSSPParentFile = NewInputCSSPFile;
                        }
                        else
                        {
                            CSSPFile csspParentFileInput = (from cff in vpse.CSSPFiles
                                                            from msf in vpse.MikeScenarioFiles
                                                            where cff.CSSPFileID == msf.CSSPFile.CSSPFileID
                                                            && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                            && msf.CSSPFile.Purpose == "Input"
                                                            && cff.CSSPFileID == cf.CSSPFileID
                                                            select msf.CSSPParentFile).FirstOrDefault<CSSPFile>();

                            if (csspParentFileInput == null)
                            {
                                MessageBox.Show("Could not find CSSPFile where CSSPParentFileID = [" + cf.CSSPFileID + "]\r\n");
                                return;
                            }

                            NewMikeScenarioFile.CSSPParentFile = csspParentFileInput;
                        }


                        NewInputCSSPFile.MikeScenarioFiles.Add(NewMikeScenarioFile);

                        try
                        {
                            vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                            vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                            richTextBoxMikePanelStatus.AppendText("File [" + NewInputCSSPFile.FileName + "] linked to new scenario\r\n");
                        }
                        catch (Exception)
                        {
                            richTextBoxMikePanelStatus.AppendText("Error file [" + NewInputCSSPFile.FileName + "] could not be linked to new scenario\r\n");
                        }

                    }

                    FillDataGridViewMikeScenairosInDB(NewMikeScenario.MikeScenarioID);
                }
            }
        }
        private void MikeNewScenarioRun()
        {
            richTextBoxMikePanelStatus.AppendText("Setting the Scenario to run.\r\n");

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            MikeScenario mikeScenarioToRun = (from ms in vpse.MikeScenarios
                                              where ms.MikeScenarioID == CurrentMikeScenario.MikeScenarioID
                                              select ms).FirstOrDefault<MikeScenario>();

            if (mikeScenarioToRun == null)
            {
                MessageBox.Show("Could not find MikeScenario with MikeScenarioID = [" + CurrentMikeScenario.MikeScenarioID + "] in the DB.\r\n");
                richTextBoxMikePanelStatus.AppendText("Could not find MikeScenario with MikeScenarioID = [" + CurrentMikeScenario.MikeScenarioID + "] in the DB.\r\n");
                return;
            }

            mikeScenarioToRun.ScenarioStatus = "ReadyToRun";

            try
            {
                vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                richTextBoxMikePanelStatus.AppendText("Scenario set to run. Waiting ...\r\n");
            }
            catch (Exception)
            {
                MessageBox.Show("Could not save changes to ScenarioStatus for MikeScenarioID = [" + CurrentMikeScenario.MikeScenarioID + "] in the DB.\r\n");
                richTextBoxMikePanelStatus.AppendText("Could not save changes to ScenarioStatus for MikeScenarioID = [" + CurrentMikeScenario.MikeScenarioID + "] in the DB.\r\n");
                return;
            }

            FillDataGridViewMikeScenairosInDB(CurrentMikeScenario.MikeScenarioID);
        }
        private void MikeNewScenarioSave()
        {
            SaveScenarioChanges(true);
            FillDataGridViewMikeScenairosInDB(CurrentMikeScenario.MikeScenarioID);
        }
        private void MikeScenarioAddNewSource()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (textBoxNewSourceName.Text.Trim() == "")
            {
                MessageBox.Show("Please enter a valid name for the new source name.");
                return;
            }

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;

                if (mikeScenario == null)
                {
                    MessageBox.Show("A scenario needs to be selected. Please select a scenario.");
                    return;
                }

                CSSPFile csspFile = (from cf in vpse.CSSPFiles
                                     from msf in vpse.MikeScenarioFiles
                                     where cf.CSSPFileID == msf.CSSPFileID
                                     && cf.FileType == ".m21fm"
                                     && msf.MikeScenarioID == mikeScenario.MikeScenarioID
                                     select cf).FirstOrDefault<CSSPFile>();

                if (csspFile == null)
                {
                    MessageBox.Show("Could not find m21fm CSSPFile for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "].");
                    return;
                }

                MemoryStream ms = new MemoryStream(csspFile.FileContent);

                m21fm.StreamToM21fm(ms);

                //int CountSource = m21fm.femEngineHD.hydrodynamic_module.sources.number_of_sources;

                string NextAvailableSourceNumberString = "";
                int SourceNumber = 0;
                while (NextAvailableSourceNumberString == "")
                {
                    SourceNumber += 1;
                    string SourceStr = "SOURCE_" + SourceNumber.ToString();
                    if (!m21fm.femEngineHD.hydrodynamic_module.sources.source.Keys.Contains(SourceStr))
                    {
                        NextAvailableSourceNumberString = SourceStr;
                        break;
                    }
                }

                MikeSource SelectedMikeSource = ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem);
                MikeSource NewMikeSource = new MikeSource();
                NewMikeSource.SourceName = textBoxNewSourceName.Text.Trim();
                NewMikeSource.Include = checkBoxMikeSouceIncluded.Checked;
                NewMikeSource.SourceFlow = float.Parse(textBoxMikeSouceFlowInCubicMeterPerDay.Text);
                NewMikeSource.SourcePollution = float.Parse(textBoxMikeSourceFC.Text);
                NewMikeSource.SourceTemperature = float.Parse(textBoxMikeSourceTemperature.Text);
                NewMikeSource.SourceSalinity = float.Parse(textBoxMikeSourceSalinity.Text);
                NewMikeSource.SourceLat = float.Parse(textBoxLatitude.Text);
                NewMikeSource.SourceLong = float.Parse(textBoxLongitude.Text);
                NewMikeSource.IsContinuous = checkBoxFlowContinuous.Checked;
                NewMikeSource.StartDateAndTime = dateTimePickerSourcePollutionStartDateAndTime.Value;
                NewMikeSource.EndDateAndTime = dateTimePickerSourcePollutionEndDateAndTime.Value;
                NewMikeSource.SourceNumberString = NextAvailableSourceNumberString;

                CurrentMikeSourceList.Add(NewMikeSource);

                // Hydrodynamic_Module
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE SelectedSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE();
                SelectedSource = m21fm.femEngineHD.hydrodynamic_module.sources.source[((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString];
                if (SelectedSource == null)
                {
                    MessageBox.Show("Could not access M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE of selected source [" + ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString + "]");
                    return;
                }
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE NewSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.SOURCES.SOURCE();
                NewSource.connected_source = SelectedSource.connected_source;
                NewSource.constant_value = SelectedSource.constant_value;
                NewSource.constant_values = SelectedSource.constant_values;
                NewSource.coordinate_type = SelectedSource.coordinate_type;
                NewSource.coordinates = SelectedSource.coordinates;
                NewSource.distribution_type = SelectedSource.distribution_type;
                NewSource.file_name = SelectedSource.file_name;
                NewSource.format = SelectedSource.format;
                NewSource.include = SelectedSource.include;
                NewSource.interpolation_type = SelectedSource.interpolation_type;
                NewSource.item_name = SelectedSource.item_name;
                NewSource.item_names = SelectedSource.item_names;
                NewSource.item_number = SelectedSource.item_number;
                NewSource.item_numbers = SelectedSource.item_numbers;
                NewSource.layer = SelectedSource.layer;
                NewSource.Name = "'" + textBoxNewSourceName.Text.Trim() + "'";
                NewSource.reference_value = SelectedSource.reference_value;
                NewSource.soft_time_interval = SelectedSource.soft_time_interval;
                NewSource.type = SelectedSource.type;
                NewSource.type_of_soft_start = SelectedSource.type_of_soft_start;
                NewSource.type_of_time_interpolation = SelectedSource.type_of_time_interpolation;
                NewSource.zone = SelectedSource.zone;

                m21fm.femEngineHD.hydrodynamic_module.sources.source.Add(NewMikeSource.SourceNumberString, NewSource);
                m21fm.femEngineHD.hydrodynamic_module.sources.number_of_sources += 1;
                m21fm.femEngineHD.hydrodynamic_module.sources.MzSEPfsListItemCount += 1;

                // Temperature_Salinity_Module
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE SelectedTSSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE();
                SelectedTSSource = m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source[((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString];
                if (SelectedTSSource == null)
                {
                    MessageBox.Show("Could not access M21fm.FemEngineHD.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE of selected source [" + ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString + "]");
                    return;
                }
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE NewTSSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE();
                NewTSSource.name = "'" + textBoxNewSourceName.Text.Trim() + "'";
                NewTSSource.type_of_temperature = SelectedTSSource.type_of_temperature;
                NewTSSource.type_of_salinity = SelectedTSSource.type_of_salinity;

                NewTSSource.temperature = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE.TEMPERATURE();
                NewTSSource.temperature.constant_value = SelectedTSSource.temperature.constant_value;
                NewTSSource.temperature.file_name = SelectedTSSource.temperature.file_name;
                NewTSSource.temperature.format = SelectedTSSource.temperature.format;
                NewTSSource.temperature.item_name = SelectedTSSource.temperature.item_name;
                NewTSSource.temperature.item_number = SelectedTSSource.temperature.item_number;
                NewTSSource.temperature.reference_value = SelectedTSSource.temperature.reference_value;
                NewTSSource.temperature.soft_time_interval = SelectedTSSource.temperature.soft_time_interval;
                NewTSSource.temperature.Touched = SelectedTSSource.temperature.Touched;
                NewTSSource.temperature.type = SelectedTSSource.temperature.type;
                NewTSSource.temperature.type_of_soft_start = SelectedTSSource.temperature.type_of_soft_start;
                NewTSSource.temperature.type_of_time_interpolation = SelectedTSSource.temperature.type_of_time_interpolation;

                NewTSSource.salinity = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TEMPERATURE_SALINITY_MODULE.SOURCES.SOURCE.SALINITY();
                NewTSSource.salinity.constant_value = SelectedTSSource.salinity.constant_value;
                NewTSSource.salinity.file_name = SelectedTSSource.salinity.file_name;
                NewTSSource.salinity.format = SelectedTSSource.salinity.format;
                NewTSSource.salinity.item_name = SelectedTSSource.salinity.item_name;
                NewTSSource.salinity.item_number = SelectedTSSource.salinity.item_number;
                NewTSSource.salinity.reference_value = SelectedTSSource.salinity.reference_value;
                NewTSSource.salinity.soft_time_interval = SelectedTSSource.salinity.soft_time_interval;
                NewTSSource.salinity.Touched = SelectedTSSource.salinity.Touched;
                NewTSSource.salinity.type = SelectedTSSource.salinity.type;
                NewTSSource.salinity.type_of_soft_start = SelectedTSSource.salinity.type_of_soft_start;
                NewTSSource.salinity.type_of_time_interpolation = SelectedTSSource.salinity.type_of_time_interpolation;

                m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source.Add(NewMikeSource.SourceNumberString, NewTSSource);
                m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.MzSEPfsListItemCount += 1;


                // HYDRODYNAMIC_MODULE.TURBULENCE_MODULE
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE SelectedTMSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE();
                SelectedTMSource = m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.source[((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString];
                if (SelectedTMSource == null)
                {
                    MessageBox.Show("Could not access M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE of selected source [" + ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString + "]");
                    return;
                }
                M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE NewTMSource = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE();
                NewTMSource.kinetic_energy = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE.KINETIC_ENERGY();
                NewTMSource.kinetic_energy.constant_value = SelectedTMSource.kinetic_energy.constant_value;
                NewTMSource.kinetic_energy.file_name = SelectedTMSource.kinetic_energy.file_name;
                NewTMSource.kinetic_energy.format = SelectedTMSource.kinetic_energy.format;
                NewTMSource.kinetic_energy.item_name = SelectedTMSource.kinetic_energy.item_name;
                NewTMSource.kinetic_energy.item_number = SelectedTMSource.kinetic_energy.item_number;
                NewTMSource.kinetic_energy.reference_value = SelectedTMSource.kinetic_energy.reference_value;
                NewTMSource.kinetic_energy.soft_time_interval = SelectedTMSource.kinetic_energy.soft_time_interval;
                NewTMSource.kinetic_energy.Touched = SelectedTMSource.kinetic_energy.Touched;
                NewTMSource.kinetic_energy.type = SelectedTMSource.kinetic_energy.type;
                NewTMSource.kinetic_energy.type_of_soft_start = SelectedTMSource.kinetic_energy.type_of_soft_start;
                NewTMSource.kinetic_energy.type_of_time_interpolation = SelectedTMSource.kinetic_energy.type_of_time_interpolation;

                NewTMSource.dissipation_of_kinetic_energy = new M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.TURBULENCE_MODULE.SOURCES.SOURCE.DISSIPATION_OF_KINETIC_ENERGY();
                NewTMSource.dissipation_of_kinetic_energy.constant_value = SelectedTMSource.dissipation_of_kinetic_energy.constant_value;
                NewTMSource.dissipation_of_kinetic_energy.file_name = SelectedTMSource.dissipation_of_kinetic_energy.file_name;
                NewTMSource.dissipation_of_kinetic_energy.format = SelectedTMSource.dissipation_of_kinetic_energy.format;
                NewTMSource.dissipation_of_kinetic_energy.item_name = SelectedTMSource.dissipation_of_kinetic_energy.item_name;
                NewTMSource.dissipation_of_kinetic_energy.item_number = SelectedTMSource.dissipation_of_kinetic_energy.item_number;
                NewTMSource.dissipation_of_kinetic_energy.reference_value = SelectedTMSource.dissipation_of_kinetic_energy.reference_value;
                NewTMSource.dissipation_of_kinetic_energy.soft_time_interval = SelectedTMSource.dissipation_of_kinetic_energy.soft_time_interval;
                NewTMSource.dissipation_of_kinetic_energy.Touched = SelectedTMSource.dissipation_of_kinetic_energy.Touched;
                NewTMSource.dissipation_of_kinetic_energy.type = SelectedTMSource.dissipation_of_kinetic_energy.type;
                NewTMSource.dissipation_of_kinetic_energy.type_of_soft_start = SelectedTMSource.dissipation_of_kinetic_energy.type_of_soft_start;
                NewTMSource.dissipation_of_kinetic_energy.type_of_time_interpolation = SelectedTMSource.dissipation_of_kinetic_energy.type_of_time_interpolation;

                m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.source.Add(NewMikeSource.SourceNumberString, NewTMSource);
                m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.MzSEPfsListItemCount += 1;


                // TRANSPORT_MODULE
                M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE SelectedTRMSource = new M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE();
                SelectedTRMSource = m21fm.femEngineHD.transport_module.sources.source[((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString];
                if (SelectedTRMSource == null)
                {
                    MessageBox.Show("Could not access M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE of selected source [" + ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString + "]");
                    return;
                }
                M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE NewTRMSource = new M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE();
                NewTRMSource.component = new Dictionary<string, M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT>();

                M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT NewComponent = new M21fm.FemEngineHD.TRANSPORT_MODULE.SOURCES.SOURCE.COMPONENT();
                NewComponent.constant_value = SelectedTRMSource.component["COMPONENT_1"].constant_value;
                NewComponent.file_name = SelectedTRMSource.component["COMPONENT_1"].file_name;
                NewComponent.format = SelectedTRMSource.component["COMPONENT_1"].format;
                NewComponent.item_name = SelectedTRMSource.component["COMPONENT_1"].item_name;
                NewComponent.item_number = SelectedTRMSource.component["COMPONENT_1"].item_number;
                NewComponent.reference_value = SelectedTRMSource.component["COMPONENT_1"].reference_value;
                NewComponent.soft_time_interval = SelectedTRMSource.component["COMPONENT_1"].soft_time_interval;
                NewComponent.type = SelectedTRMSource.component["COMPONENT_1"].type;
                NewComponent.type_of_component = SelectedTRMSource.component["COMPONENT_1"].type_of_component;
                NewComponent.type_of_soft_start = SelectedTRMSource.component["COMPONENT_1"].type_of_soft_start;
                NewComponent.type_of_time_interpolation = SelectedTRMSource.component["COMPONENT_1"].type_of_time_interpolation;

                NewTRMSource.component.Add("COMPONENT_1", NewComponent);

                m21fm.femEngineHD.transport_module.sources.source.Add(NewMikeSource.SourceNumberString, NewTRMSource);
                m21fm.femEngineHD.transport_module.sources.MzSEPfsListItemCount += 1;

                m21fm.femEngineHD.transport_module.sources.source[NewMikeSource.SourceNumberString].name = "'" + textBoxNewSourceName.Text.Trim() + "'";
                m21fm.femEngineHD.transport_module.sources.source[NewMikeSource.SourceNumberString].MzSEPfsListItemCount = 1;
                m21fm.femEngineHD.transport_module.sources.source[NewMikeSource.SourceNumberString].Touched = 1;

                csspFile.FileContent = m21fm.M21fmToStream().ToArray();

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Saving m21fm fileContent with new added source.\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Saved m21fm fileContent with new added source.\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while saving m21fm fileContent with new added source.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                    return;
                }

                MikeScenario mikeScenarioToChange = (from m in vpse.MikeScenarios
                                                     where m.MikeScenarioID == mikeScenario.MikeScenarioID
                                                     select m).FirstOrDefault<MikeScenario>();

                if (mikeScenarioToChange == null)
                {
                    MessageBox.Show("Could not find MikeScenario with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]");
                    return;
                }

                mikeScenarioToChange.MikeSources.Add(NewMikeSource);

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Saving new source added with name [" + NewMikeSource.SourceName + "].\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Saved new source added with name [" + NewMikeSource.SourceName + "].\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while saving new source.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                }


                textBoxNewSourceName.Text = ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceName;
                MikeNewScenarioSave();

            }
            else
            {
                MessageBox.Show("A scenario needs to be selected. Please select a scenario.");
                return;
            }
        }
        private void MikeScenarioParamSourceInputResultsClear()
        {
            // Input Files
            dataGridViewScenarioFiles.DataSource = null;

            // General Parameters
            textBoxMikeNewScenarioName.Text = "";
            //richTextBoxMikeScenarioDescription.Text = "";
            dateTimePickerScenarioStartDateAndTime.Value = DateTime.Now;
            dateTimePickerScenarioEndDateAndTime.Value = DateTime.Now;
            lblScenarioLengthDays.Text = "";
            lblScenarioLengthHours.Text = "";
            lblScenarioLengthMinutes.Text = "";
            textBoxMikeScenarioDecayFactorPerDay.Text = "";
            textBoxMikeScenarioAmbientTemperature.Text = "";
            textBoxMikeScenarioAmbientSalinity.Text = "";
            textBoxMikeScenarioManningNumber.Text = "";
            textBoxMikeScenarioWindSpeedKilometerPerHour.Text = "";
            textBoxMikeScenarioWindSpeedMeterPerSecond.Text = "";
            textBoxMikeScenarioWindDirection.Text = "";
            listBoxMinMaxTideDateAndTime.Items.Clear();

            // Sources
            comboBoxMikeScenarioSourceName.DataSource = null;
            textBoxNewSourceName.Text = "";
            checkBoxMikeSouceIncluded.Checked = false;
            textBoxMikeSouceFlowInCubicMeterPerDay.Text = "";
            textBoxMikeSouceFlowInCubicMeterPerSecond.Text = "";
            checkBoxFlowContinuous.Checked = false;
            dateTimePickerSourcePollutionStartDateAndTime.Value = DateTime.Now;
            dateTimePickerSourcePollutionStartDateAndTime.Value = DateTime.Now;
            textBoxMikeSourceFC.Text = "";
            textBoxMikeSourceTemperature.Text = "";
            textBoxMikeSourceSalinity.Text = "";
            textBoxLatitude.Text = "";
            textBoxLongitude.Text = "";

        }
        private void MikeScenarioRemoveSource()
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;

                if (mikeScenario == null)
                {
                    MessageBox.Show("A scenario needs to be selected. Please select a scenario.");
                    return;
                }

                CSSPFile csspFile = (from cf in vpse.CSSPFiles
                                     from msf in vpse.MikeScenarioFiles
                                     where cf.CSSPFileID == msf.CSSPFileID
                                     && cf.FileType == ".m21fm"
                                     && msf.MikeScenarioID == mikeScenario.MikeScenarioID
                                     select cf).FirstOrDefault<CSSPFile>();

                if (csspFile == null)
                {
                    MessageBox.Show("Could not find m21fm CSSPFile for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "].");
                    return;
                }

                MemoryStream ms = new MemoryStream(csspFile.FileContent);

                m21fm.StreamToM21fm(ms);

                if (m21fm.femEngineHD.hydrodynamic_module.sources.source.Count == 1)
                {
                    MessageBox.Show("You are not allowed to delete the last resource. Create a new one than delete this one.");
                    return;
                }

                string SourceNumberString = ((MikeSource)comboBoxMikeScenarioSourceName.SelectedItem).SourceNumberString;
                int SourceNumber = int.Parse(SourceNumberString.Substring(SourceNumberString.LastIndexOf("_") + 1));

                for (int i = 0; i < CurrentMikeSourceList.Count; i++)
                {
                    if (CurrentMikeSourceList[i].SourceNumberString == SourceNumberString)
                    {
                        CurrentMikeSourceList.Remove(CurrentMikeSourceList[i]);
                        break;
                    }
                }

                MikeSource mikeSourceToDelete = (from mss in vpse.MikeSources
                                                 where mss.MikeScenarioID == mikeScenario.MikeScenarioID
                                                 && mss.SourceNumberString == SourceNumberString
                                                 select mss).FirstOrDefault<MikeSource>();

                if (mikeSourceToDelete == null)
                {
                    MessageBox.Show("Could not find MikeSource where MikeSource.SourceNumberString = [" + SourceNumberString + "]  and MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]");
                    return;
                }

                vpse.DeleteObject(mikeSourceToDelete);

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Removing source from DB SourceName [" + mikeSourceToDelete.SourceName + "].\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Removed source from DB SourceName [" + mikeSourceToDelete.SourceName + "].\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while trying to remove source from DB.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                }

                List<MikeSource> mikeSourceList = (from msss in vpse.MikeSources
                                                   where msss.MikeScenarioID == mikeScenario.MikeScenarioID
                                                   select msss).ToList<MikeSource>();

                if (mikeSourceList.Count == 0)
                {
                    MessageBox.Show("Could not find any MikeSource for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]");
                    return;
                }


                // Hydrodynamic_Module
                m21fm.femEngineHD.hydrodynamic_module.sources.source.Remove(SourceNumberString);
                m21fm.femEngineHD.hydrodynamic_module.sources.number_of_sources -= 1;
                m21fm.femEngineHD.hydrodynamic_module.sources.MzSEPfsListItemCount -= 1;

                // Temperature_Salinity_Module
                m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source.Remove(SourceNumberString);
                m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.MzSEPfsListItemCount -= 1;


                // HYDRODYNAMIC_MODULE.TURBULENCE_MODULE
                m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.source.Remove(SourceNumberString);
                m21fm.femEngineHD.hydrodynamic_module.turbulence_module.sources.MzSEPfsListItemCount -= 1;


                // TRANSPORT_MODULE
                m21fm.femEngineHD.transport_module.sources.source.Remove(SourceNumberString);
                m21fm.femEngineHD.transport_module.sources.MzSEPfsListItemCount -= 1;

                csspFile.FileContent = m21fm.M21fmToStream().ToArray();

                StringBuilder sb = new StringBuilder(Encoding.UTF8.GetString(csspFile.FileContent));

                int NextSourceNumber = SourceNumber + 1;
                bool KeepGoing = true;
                while (KeepGoing)
                {
                    if (m21fm.femEngineHD.hydrodynamic_module.sources.source.Keys.Contains("SOURCE_" + NextSourceNumber.ToString()))
                    {
                        sb.Replace("[SOURCE_" + NextSourceNumber.ToString() + "]", "[SOURCE_" + SourceNumber.ToString() + "]");
                        sb.Replace("EndSect  // SOURCE_" + NextSourceNumber.ToString(), "EndSect  // SOURCE_" + SourceNumber.ToString());
                        for (int i = 0; i < CurrentMikeSourceList.Count; i++)
                        {
                            if (CurrentMikeSourceList[i].SourceNumberString == "SOURCE_" + NextSourceNumber.ToString())
                            {
                                CurrentMikeSourceList[i].SourceNumberString = "SOURCE_" + SourceNumber.ToString();
                                break;
                            }
                        }
                        for (int i = 0; i < mikeSourceList.Count; i++)
                        {
                            if (mikeSourceList[i].SourceNumberString == "SOURCE_" + NextSourceNumber.ToString())
                            {
                                mikeSourceList[i].SourceNumberString = "SOURCE_" + SourceNumber.ToString();
                            }
                        }
                        SourceNumber += 1;
                        NextSourceNumber = SourceNumber + 1;
                    }
                    else
                    {
                        KeepGoing = false;
                    }
                }

                csspFile.FileContent = Encoding.UTF8.GetBytes(sb.ToString());

                try
                {
                    richTextBoxMikePanelStatus.AppendText("Saving m21fm fileContent with new added source.\r\n");
                    vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    richTextBoxMikePanelStatus.AppendText("Saved m21fm fileContent with new added source.\r\n");
                }
                catch (Exception ex)
                {
                    richTextBoxMikePanelStatus.AppendText("Error while saving m21fm fileContent with new added source.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                    return;
                }

                textBoxNewSourceName.Text = "||||||";

                MikeNewScenarioSave();
            }
            else
            {
                MessageBox.Show("A scenario needs to be selected. Please select a scenario.");
                return;
            }

        }
        private void RemoveFileFromDB()
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Trying to remove files from DB ...\r\n");
            Application.DoEvents();

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI tvi = (TVI)treeViewItems.SelectedNode.Tag;


            if (dataGridViewScenarioFiles.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow dr in dataGridViewScenarioFiles.SelectedRows)
                {
                    CSSPFileNoContent csspFileNoContent = (CSSPFileNoContent)dr.DataBoundItem;
                    if (csspFileNoContent != null)
                    {
                        richTextBoxMikePanelStatus.AppendText(string.Format("{0}\r\n", csspFileNoContent.FileOriginalPath + csspFileNoContent.FileName));
                        richTextBoxMikePanelStatus.AppendText("Removing link to municipality ...\r\n");
                        Application.DoEvents();

                        // first delete the CSSPItemFiles object then delete the CSSPFiles
                        var CsspItemFileToDelete = from cif in vpse.CSSPItemFiles
                                                   where cif.CSSPFile.CSSPFileID == csspFileNoContent.CSSPFileID
                                                   && cif.CSSPItem.CSSPItemID == tvi.ItemID
                                                   select cif;

                        foreach (CSSPItemFile cif in CsspItemFileToDelete)
                        {

                            vpse.DeleteObject(cif);
                        }

                        try
                        {
                            vpse.SaveChanges();

                            richTextBoxMikePanelStatus.AppendText("Removed link to the municipality ...\r\n\r\n");
                            Application.DoEvents();
                        }
                        catch (Exception)
                        {
                            // nothing for now
                            //ShowMikeError(ex);
                            richTextBoxMikePanelStatus.AppendText("Could not remove file linked to the municipality ...\r\n\r\n");
                        }

                        richTextBoxMikePanelStatus.AppendText("Removing file ...\r\n");
                        Application.DoEvents();

                        var CsspFileToDelete = from cf in vpse.CSSPFiles
                                               where cf.CSSPFileID == csspFileNoContent.CSSPFileID
                                               select cf;

                        foreach (CSSPFile cf in CsspFileToDelete)
                        {
                            vpse.DeleteObject(cf);
                        }

                        try
                        {
                            vpse.SaveChanges();

                            richTextBoxMikePanelStatus.AppendText("Removed file ...\r\n\r\n");
                            Application.DoEvents();
                        }
                        catch (Exception)
                        {
                            // nothing for now
                            richTextBoxMikePanelStatus.AppendText("Only the link was removed. The file is probably use by another municipality ...\r\n\r\n");
                        }

                    }
                }

                FillAfterSelect(treeViewItems.SelectedNode);

            }
        }
        private void RemoveScenarioFromDB()
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Trying to remove Scenario from DB ...\r\n");
            Application.DoEvents();

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI tvi = (TVI)treeViewItems.SelectedNode.Tag;


            if (dataGridViewMikeScenairosInDB.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow dr in dataGridViewMikeScenairosInDB.SelectedRows)
                {
                    MikeScenario mikeScenario = (MikeScenario)dr.DataBoundItem;
                    if (mikeScenario != null)
                    {
                        richTextBoxMikePanelStatus.AppendText(string.Format("{0}\r\n", mikeScenario.ScenarioName));
                        richTextBoxMikePanelStatus.AppendText("Removing Scenario ...\r\n");
                        Application.DoEvents();

                        List<CSSPFile> AssociateFileToTryToDelete = new List<CSSPFile>();

                        List<MikeScenario> mikeScenarioToDeleteList = (from ms in vpse.MikeScenarios
                                                                       where ms.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                       && ms.CSSPItem.CSSPItemID == tvi.ItemID
                                                                       select ms).ToList<MikeScenario>();

                        foreach (MikeScenario ms in mikeScenarioToDeleteList)
                        {
                            List<CSSPFile> TempInputFileToDelete = (from cf in vpse.CSSPFiles
                                                                    from msif in vpse.MikeScenarioFiles
                                                                    where cf.CSSPFileID == msif.CSSPFile.CSSPFileID
                                                                    && msif.MikeScenario.MikeScenarioID == ms.MikeScenarioID
                                                                    select cf).ToList<CSSPFile>();
                            foreach (CSSPFile cf in TempInputFileToDelete)
                            {
                                AssociateFileToTryToDelete.Add(cf);
                            }

                            vpse.DeleteObject(ms);
                        }

                        try
                        {
                            vpse.SaveChanges();

                            richTextBoxMikePanelStatus.AppendText("Removed Scenario ...\r\n\r\n");
                            Application.DoEvents();
                        }
                        catch (Exception)
                        {
                            // nothing for now
                            //ShowMikeError(ex);
                            richTextBoxMikePanelStatus.AppendText("Could not remove scenario ...\r\n\r\n");
                        }

                        richTextBoxMikePanelStatus.AppendText("Trying to remove associated file ...\r\n");
                        Application.DoEvents();

                        foreach (CSSPFile cftd in AssociateFileToTryToDelete)
                        {
                            richTextBoxMikePanelStatus.AppendText("Trying to remove file [" + cftd.FileName + "] ...\r\n");

                            CSSPFile CsspFileToDelete = (from cf in vpse.CSSPFiles
                                                         where cf.CSSPFileID == cftd.CSSPFileID
                                                         select cf).FirstOrDefault<CSSPFile>();

                            vpse.DeleteObject(CsspFileToDelete);

                            try
                            {
                                vpse.SaveChanges();

                                richTextBoxMikePanelStatus.AppendText("Removed file ...\r\n\r\n");
                                Application.DoEvents();
                            }
                            catch (Exception)
                            {
                                vpse.AcceptAllChanges();
                                // nothing for now
                                richTextBoxMikePanelStatus.AppendText("Could not removed file. The file is probably use by anyther scenario ...\r\n\r\n");
                            }
                        }
                    }
                }

                FillDataGridViewMikeScenairosInDB(0);

            }
        }
        private string ReturnStrLimit(string TempStr, int NumbOfChar)
        {
            StringBuilder RetString = new StringBuilder();
            if (TempStr != null)
            {
                if (TempStr.Length < NumbOfChar)
                {
                    for (int i = 0; i < (NumbOfChar - TempStr.Length); i++)
                    {
                        RetString.Append(" ");
                    }
                    RetString.Append(TempStr);
                }
                else
                {
                    RetString.Append(TempStr);
                }
            }
            return RetString.ToString();
        }
        private void SaveInKMZFileStream(string KMZFileName, string KMLFileName, StringBuilder sbKML)
        {
            FileInfo fi = new FileInfo(KMZFileName);
            FileStream fs = fi.Create();
            ZipOutputStream zos = new ZipOutputStream(fs, sbKML.Length);
            byte[] zipByte = System.Text.Encoding.UTF8.GetBytes(sbKML.ToString());

            ZipEntry ze = new ZipEntry(KMLFileName);
            ze.DateTime = DateTime.Now;
            ze.Size = zipByte.Length;
            zos.PutNextEntry(ze);
            zos.SetLevel(3);
            zos.IsStreamOwner = true;
            zos.Write(zipByte, 0, sbKML.Length);
            zos.CloseEntry();
            zos.Flush();
            zos.Close();
            fs.Close();
        }
        private bool SaveScenarioChanges(bool CheckIfScenarioStatusIsRunningOrComplete)
        {
            //richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Trying to save changes to the new Scenario ...\r\n");
            Application.DoEvents();
            string FileName = "";
            string ShortFileName = "";

            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            TVI tvi = (TVI)treeViewItems.SelectedNode.Tag;

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
            {
                ComboBoxMikeScenarioSourceNameSelectionIndexChanged();

                double DecayFactorAmplitude = double.Parse(textBoxMikeScenarioDecayFactorAmplitude.Text);
                double AverageDecayFactor = double.Parse(textBoxMikeScenarioDecayFactorPerDay.Text);

                if (!checkBoxDecayIsConstant.Checked)
                {
                    if (AverageDecayFactor < DecayFactorAmplitude)
                    {
                        MessageBox.Show("Average Decay Factor has to be >= than the Decay Factor Amplitude");
                        return false;
                    }
                }


                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                MikeScenario MikeScenarioToChange = (from ms in vpse.MikeScenarios
                                                     where ms.MikeScenarioID == mikeScenario.MikeScenarioID
                                                     select ms).FirstOrDefault<MikeScenario>();

                if (MikeScenarioToChange != null)
                {
                    if (CheckIfScenarioStatusIsRunningOrComplete)
                    {
                        if (MikeScenarioToChange.ScenarioStatus == ScenarioStatusType.Running.ToString() || MikeScenarioToChange.ScenarioStatus == ScenarioStatusType.Completed.ToString())
                        {
                            MessageBox.Show("Can't save Scenario because the Scenario is currently running or has the status of completed.\r\n");
                            return false;
                        }
                        MikeScenarioToChange.ScenarioStatus = ScenarioStatusType.Changed.ToString();
                    }
                    try
                    {
                        richTextBoxMikePanelStatus.AppendText("Trying to update Scenario Status with Changed ...\r\n");
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                        richTextBoxMikePanelStatus.AppendText("Updated Scenario Status with Changed ...\r\n");
                    }
                    catch (Exception ex)
                    {
                        richTextBoxMikePanelStatus.AppendText("Error while updating Scenario Status with Changed ...\r\n");
                        richTextBoxMikePanelStatus.AppendText("Error message [" + ex.Message + "] ...\r\n");
                        MessageBox.Show("Can't save Scenario because the unknown error. Please check status box for more details.\r\n");
                        return false;
                    }
                }

                foreach (MikeSource currentMikeSource in CurrentMikeSourceList)
                {
                    if ((bool)currentMikeSource.Include && !(bool)currentMikeSource.IsContinuous)
                    {
                        if (currentMikeSource.StartDateAndTime.Value > currentMikeSource.EndDateAndTime.Value)
                        {
                            MessageBox.Show(string.Format("Source [{0}] start date should be >= than it's end date.", currentMikeSource.SourceName));
                            return false;
                        }
                    }
                }

                if (mikeScenario != null)
                {
                    bool NeedToChangePollutionInfo = false;

                    List<CSSPFileNoContent> csspFileNoContentInputDfs0List = (from cf in vpse.CSSPFileNoContents
                                                                              from msif in vpse.MikeScenarioFiles
                                                                              where cf.CSSPFileID == msif.CSSPFile.CSSPFileID
                                                                              && msif.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                              && msif.CSSPFile.Purpose == "Original"
                                                                              && (msif.CSSPFile.FileType == ".dfs0" || msif.CSSPFile.FileType == ".dfs1")
                                                                              select cf).ToList<CSSPFileNoContent>();


                    CSSPFile csspFilem21fm = (from cf in vpse.CSSPFiles
                                              from msif in vpse.MikeScenarioFiles
                                              where cf.CSSPFileID == msif.CSSPFile.CSSPFileID
                                              && msif.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                              && msif.CSSPFile.Purpose == "Input"
                                              && msif.CSSPFile.FileType == ".m21fm"
                                              select cf).FirstOrDefault<CSSPFile>();


                    if (csspFilem21fm == null)
                    {
                        MessageBox.Show("Could not .m21fm file for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not .m21fm file for MikeScenarioID = [" + mikeScenario.MikeScenarioID + "]\r\n");
                        return false;
                    }

                    FileName = csspFilem21fm.FileOriginalPath + csspFilem21fm.FileName;
                    ShortFileName = FileName.Substring(FileName.LastIndexOf("\\") + 1);
                    ShortFileName = ShortFileName.Substring(0, ShortFileName.LastIndexOf("."));
                    //richTextBoxMikePanelStatus.Clear();

                    MemoryStream mems = new MemoryStream(csspFilem21fm.FileContent);
                    //StreamReader sr = new StreamReader(mems);

                    richTextBoxMikePanelStatus.AppendText("Trying to load and parse file = [" + csspFilem21fm.FileOriginalPath + csspFilem21fm.FileName + "]\r\n");


                    if (!m21fm.StreamToM21fm(mems))
                    {
                        MessageBox.Show("File Not read properly.\r\n");
                        richTextBoxMikePanelStatus.AppendText("File Not read properly.\r\n");
                        return false;
                    }

                    // Updating the femEngineHD object 
                    if (dateTimePickerScenarioStartDateAndTime.Value == null)
                    {
                        MessageBox.Show("Please enter the Scenario Start date and time under the parameter tab.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Please enter the Scenario Start date and time under the parameter tab.\r\n");
                        return false;
                    }
                    if (dateTimePickerScenarioEndDateAndTime.Value == null)
                    {
                        MessageBox.Show("Please enter the Scenario End date and time under the parameter tab.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Please enter the Scenario End date and time under the parameter tab.\r\n");
                        return false;
                    }
                    if (dateTimePickerScenarioEndDateAndTime.Value <= dateTimePickerScenarioStartDateAndTime.Value)
                    {
                        MessageBox.Show("Scenario End date and time needs to be > than Scenario Start date and time under the parameter tab.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Scenario End date and time needs to be > than Scenario Start date and time under the parameter tab.\r\n");
                        return false;
                    }

                    if (m21fm.femEngineHD.time.start_time != (DateTime)dateTimePickerScenarioStartDateAndTime.Value)
                    {
                        foreach (CSSPFileNoContent cfnc in csspFileNoContentInputDfs0List)
                        {
                            if (cfnc.DataStartDate.HasValue)
                            {
                                if (cfnc.DataStartDate.Value > dateTimePickerScenarioStartDateAndTime.Value)
                                {
                                    MessageBox.Show(string.Format("Scenario start date is too early for the existing water level or current file [{0}]] min date is [{1:yyyy/MM/dd HH:mm:ss tt}].\r\n", cfnc.FileName, cfnc.DataStartDate.Value));
                                    return false;
                                }
                            }
                        }
                        NeedToChangePollutionInfo = true;
                        m21fm.femEngineHD.time.start_time = (DateTime)dateTimePickerScenarioStartDateAndTime.Value;
                    }


                    TimeSpan ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dateTimePickerScenarioStartDateAndTime.Value.Ticks);

                    lblScenarioLengthDays.Text = ts.Days.ToString();
                    lblScenarioLengthHours.Text = ts.Hours.ToString();
                    lblScenarioLengthMinutes.Text = ts.Minutes.ToString();
                    int Days = 0;
                    int Hours = 0;
                    int Minutes = 0;
                    int.TryParse(lblScenarioLengthDays.Text.Trim(), out Days);
                    int.TryParse(lblScenarioLengthHours.Text.Trim(), out Hours);
                    int.TryParse(lblScenarioLengthMinutes.Text.Trim(), out Minutes);

                    if (Days == 0 && Hours == 0)
                    {
                        MessageBox.Show("Scenario length should be at least 1 hour.");
                        return false;
                    }

                    if (m21fm.femEngineHD.time.number_of_time_steps != (Days * 24 * 60) + Hours * 60 + Minutes)
                    {
                        DateTime EndDateRequired = new DateTime();
                        EndDateRequired = dateTimePickerScenarioStartDateAndTime.Value.AddSeconds(m21fm.femEngineHD.time.time_step_interval * (Days * 24 * 60) + Hours * 60 + Minutes);

                        foreach (CSSPFileNoContent cfnc in csspFileNoContentInputDfs0List)
                        {
                            if (cfnc.DataEndDate.HasValue)
                            {
                                if (cfnc.DataEndDate.Value < EndDateRequired)
                                {
                                    MessageBox.Show(string.Format("Scenario end date is too late for the existing water level or current file [{0}] max date is [{1:yyyy/MM/dd HH:mm:ss tt}].\r\n", cfnc.FileName, cfnc.DataEndDate.Value));
                                    return false;
                                }
                            }
                        }
                        NeedToChangePollutionInfo = true;
                    }
                    m21fm.femEngineHD.time.number_of_time_steps = (Days * 24 * 60) + Hours * 60 + Minutes;


                    if (m21fm.femEngineHD.time.number_of_time_steps == 0)
                    {
                        MessageBox.Show("Please enter a valid scenario length under the parameter tab.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Please enter a valid scenario length under the parameter tab.\r\n");
                        return false;
                    }
                    // setting proper decoupling last_time_step
                    m21fm.femEngineHD.hydrodynamic_module.decoupling.last_time_step = m21fm.femEngineHD.time.number_of_time_steps;

                    // setting proper hydrodynamic_module outputs.output.last_time_step
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.OUTPUTS.OUTPUT> kvp in m21fm.femEngineHD.hydrodynamic_module.outputs.output)
                    {
                        kvp.Value.last_time_step = m21fm.femEngineHD.time.number_of_time_steps;
                    }

                    // setting proper transport_module outputs.output.last_time_step
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.OUTPUTS.OUTPUT> kvp in m21fm.femEngineHD.transport_module.outputs.output)
                    {
                        kvp.Value.last_time_step = m21fm.femEngineHD.time.number_of_time_steps;
                        int TempInt = 0;
                        if (int.TryParse(textBoxMikeScenarioResultFrequencyInMinutes.Text, out TempInt))
                        {
                            kvp.Value.time_step_frequency = TempInt;
                        }
                        else
                        {
                            MessageBox.Show("Please enter a valid result frequency.");
                            return false;
                        }
                    }

                    if (textBoxMikeScenarioWindSpeedKilometerPerHour.Text.Trim() == "")
                    {
                        m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed = 0;
                    }
                    else
                    {
                        float TempFloat;
                        if (float.TryParse(textBoxMikeScenarioWindSpeedKilometerPerHour.Text.Trim(), out TempFloat))
                        {
                            m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed = TempFloat / (float)3.6;
                        }
                        else
                        {
                            MessageBox.Show("Please enter a valid wind speed.");
                            return false;
                        }

                    }
                    if (textBoxMikeScenarioWindDirection.Text.Trim() == "")
                    {
                        m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_direction = 0;
                    }
                    else
                    {
                        float TempFloat;
                        if (float.TryParse(textBoxMikeScenarioWindDirection.Text.Trim(), out TempFloat))
                        {
                            if (TempFloat >= 0.0 && TempFloat <= 360.0)
                            {
                                m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_direction = TempFloat;
                            }
                            else
                            {
                                MessageBox.Show("Please enter a valid wind direction (between 0 and 360).");
                                return false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please enter a valid wind direction.");
                            return false;
                        }
                    }
                    foreach (KeyValuePair<string, M21fm.FemEngineHD.TRANSPORT_MODULE.DECAY.COMPONENT> kvp in m21fm.femEngineHD.transport_module.decay.component)
                    {
                        if (checkBoxDecayIsConstant.Checked)
                        {
                            kvp.Value.format = 0;
                            if (textBoxMikeScenarioDecayFactorPerDay.Text.Trim() == "")
                            {
                                kvp.Value.constant_value = 0;
                            }
                            else
                            {
                                kvp.Value.constant_value = float.Parse(textBoxMikeScenarioDecayFactorPerDay.Text.Trim()) / 24 / 3600;
                            }
                        }
                        else
                        {
                            if (textBoxMikeScenarioDecayFactorPerDay.Text.Trim() == "")
                            {
                                kvp.Value.constant_value = 0;
                            }
                            else
                            {
                                kvp.Value.constant_value = float.Parse(textBoxMikeScenarioDecayFactorPerDay.Text.Trim()) / 24 / 3600;
                            }

                            kvp.Value.format = 1;

                            // creating the varying decay file

                            dfs = new Dfs(Dfs.DFSType.DFS0, true);

                            dfs.FileCreatedDate = DateTime.Now;
                            dfs.FileLastModifiedDate = DateTime.Now;
                            dfs.Equidistant = Dfs.EquidistantOrNonEquidistant.Equidistant;
                            dfs.Title = string.Format("Varying Decay Factor for Scenario [{0}]", mikeScenario.ScenarioName);
                            dfs.FileType = "MIKE Zero";
                            dfs.TypeOfAxis = Dfs.AxisType.EquidistantCalendarAxis;
                            dfs.DataStartDate = m21fm.femEngineHD.time.start_time;
                            dfs.UnitCode = Dfs.Unit.Second;

                            CSSPFileNoContent csspFileNoContent = (from cf in vpse.CSSPFileNoContents
                                                                   from msif in vpse.MikeScenarioFiles
                                                                   where cf.CSSPFileID == msif.CSSPFile.CSSPFileID
                                                                   && msif.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                   && msif.CSSPFile.Purpose == "Original"
                                                                   && msif.CSSPFile.FileType == ".dfs0"
                                                                   select cf).FirstOrDefault<CSSPFileNoContent>();

                            if (csspFileNoContent == null)
                            {
                                dfs.TimeSteps = 3600;
                            }
                            else
                            {
                                dfs.TimeSteps = (double)csspFileNoContent.TimeStepsInSecond;
                            }

                            dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                            ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dfs.DataStartDate.Ticks);

                            dfs.NumberOfParameters = 1;
                            dfs.ParameterList = new List<Dfs.Parameter>();
                            Dfs.Parameter p = new Dfs.Parameter();
                            p.Description = "Decay Factor";
                            p.Code = Dfs.Parameter.ParameterType.DecayFactor;
                            p.UnitCode = Dfs.Unit.PerSecond;
                            p.GridStep = (float)0.0;
                            p.GridStepMultipliedBy9 = 0;
                            p.NumberOfElements = 0;
                            p.NumberOfEmptyValues = 0;
                            p.NumberOfGridPoints = 0;
                            p.NumberOfTimeSteps = 0;
                            p.TimeSeriesTypeCode = Dfs.Parameter.TimeSeriesType.Instantaneous;

                            dfs.ParameterList.Add(p);

                            dfs.XValueList = new List<double>();
                            p.ValueList = new List<float>();
                            float DecayAverage = float.Parse(textBoxMikeScenarioDecayFactorPerDay.Text);
                            DateTime NewDateTime = dfs.DataStartDate;
                            for (int i = 0; i < dfs.NumberOfValues; i++)
                            {
                                dfs.XValueList.Add(i * dfs.TimeSteps);
                                NewDateTime = dfs.DataStartDate.AddSeconds(i * dfs.TimeSteps);
                                double DecayValue = AverageDecayFactor + DecayFactorAmplitude * Math.Cos((double)(((double)(NewDateTime.Hour * 3600 + NewDateTime.Minute * 60 + NewDateTime.Second) / (double)86400) + (double)0.5) * 2 * Math.PI);
                                p.ValueList.Add((float)(DecayValue / 24 / 3600));
                            }

                            p.Maximum = p.ValueList.Max<float>();
                            p.Minimum = p.ValueList.Min<float>();

                            Dfs.Parameter.Stat stat = new Dfs.Parameter.Stat();
                            p.StatList = new List<Dfs.Parameter.Stat>();
                            p.StatList.Add(stat);

                            p.StatList[0].LastValue = p.ValueList[p.ValueList.Count - 1];
                            p.StatList[0].Maximum = p.ValueList.Max<float>();
                            p.StatList[0].Minimum = p.ValueList.Min<float>();
                            p.StatList[0].NumberOfEmptyValues = 0;
                            p.StatList[0].NumberOfTimeSeries = dfs.NumberOfValues;
                            p.StatList[0].NumberOfValuesMinus1 = dfs.NumberOfValues - 1;
                            p.StatList[0].Sum = p.ValueList.Sum();
                            p.StatList[0].SumOfSquareOfValue = 0;
                            p.StatList[0].ValueMultiplicated = 0;
                            foreach (float f in p.ValueList)
                            {
                                p.StatList[0].SumOfSquareOfValue += f * f;
                                p.StatList[0].ValueMultiplicated *= f;
                            }

                            MemoryStream DecayUpdate = new MemoryStream();

                            DecayUpdate = dfs.DfsToStream();

                            string TempFileName = string.Format("[{0}] Decay.dfs0", mikeScenario.MikeScenarioID);
                            string TempPath = "";
                            if (m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code.Count > 1) // boundary condition exist
                            {
                                foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.BOUNDARY_CONDITIONS.CODE> kvp2 in m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code)
                                {
                                    if (kvp2.Value.file_name != null)
                                    {
                                        TempPath = kvp2.Value.file_name;
                                        break;
                                    }
                                }
                                TempPath = TempPath.Substring(0, TempPath.LastIndexOf("\\") + 1);
                            }

                            m21fm.femEngineHD.transport_module.decay.component["COMPONENT_1"].file_name = TempPath + TempFileName + "|";

                            CSSPFile csspFileExist = (from cp in vpse.CSSPFiles
                                                      where cp.FileName == TempFileName
                                                      select cp).FirstOrDefault<CSSPFile>();

                            if (csspFileExist != null)
                            {
                                csspFileExist.FileContent = DecayUpdate.ToArray();
                                csspFileExist.FileSize = csspFileExist.FileContent.Length;
                                csspFileExist.FileCreatedDate = DateTime.Now;
                                csspFileExist.DataStartDate = dateTimePickerScenarioStartDateAndTime.Value;
                                csspFileExist.DataEndDate = dateTimePickerScenarioEndDateAndTime.Value;

                                try
                                {
                                    richTextBoxMikePanelStatus.AppendText("Updating file = [" + csspFileExist.FileName + "]\r\n");
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("Updated file = [" + csspFileExist.FileName + "]\r\n");
                                }
                                catch (Exception ex)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error while updating file = [" + csspFileExist.FileName + "]\r\n");
                                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                                }
                            }
                            else
                            {
                                CSSPFile NewInputCSSPFile = new CSSPFile();
                                NewInputCSSPFile.CSSPGuid = Guid.NewGuid();
                                NewInputCSSPFile.Purpose = "InputPol";
                                NewInputCSSPFile.FileName = TempFileName;
                                NewInputCSSPFile.FileOriginalPath = csspFileNoContentInputDfs0List[0].FileOriginalPath;
                                NewInputCSSPFile.FileDescription = "";
                                NewInputCSSPFile.FileContent = DecayUpdate.ToArray();
                                NewInputCSSPFile.FileType = ".dfs0";
                                NewInputCSSPFile.FileSize = NewInputCSSPFile.FileContent.Length;
                                NewInputCSSPFile.FileCreatedDate = DateTime.Now;
                                NewInputCSSPFile.DataStartDate = dateTimePickerScenarioStartDateAndTime.Value;
                                NewInputCSSPFile.DataEndDate = dateTimePickerScenarioEndDateAndTime.Value;
                                NewInputCSSPFile.TimeStepsInSecond = dfs.TimeSteps;
                                NewInputCSSPFile.ParameterNames = dfs.ParameterList[0].Code.ToString();
                                NewInputCSSPFile.ParameterUnits = dfs.ParameterList[0].UnitCode.ToString();
                                NewInputCSSPFile.IsCompressed = false;


                                try
                                {
                                    vpse.AddToCSSPFiles(NewInputCSSPFile);
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("Copy of file created = [" + NewInputCSSPFile.FileName + "]\r\n");
                                }
                                catch (Exception)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error while copying new scenario file.\r\n");
                                    return false;
                                }

                                MikeScenario MikeScenarioToLink = (from mstl in vpse.MikeScenarios
                                                                   where mstl.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                   select mstl).FirstOrDefault<MikeScenario>();

                                if (MikeScenarioToLink == null)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Could not find MikeScenario where MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in the DB.\r\n");
                                    return false;
                                }

                                MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                                NewMikeScenarioFile.MikeScenario = MikeScenarioToLink;
                                NewMikeScenarioFile.CSSPFile = NewInputCSSPFile;
                                NewMikeScenarioFile.CSSPParentFile = NewInputCSSPFile;

                                try
                                {
                                    vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("File [" + NewInputCSSPFile.FileName + "] linked to new scenario\r\n");
                                }
                                catch (Exception)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error file [" + NewInputCSSPFile.FileName + "] could not be linked to new scenario\r\n");
                                }
                            }

                        }
                        break;
                    }
                    if (textBoxMikeScenarioAmbientTemperature.Text.Trim() == "")
                    {
                        m21fm.femEngineHD.hydrodynamic_module.density.temperature_reference = 10;
                    }
                    else
                    {
                        float TempFloat = 0;
                        if (float.TryParse(textBoxMikeScenarioAmbientTemperature.Text.Trim(), out TempFloat))
                        {
                            m21fm.femEngineHD.hydrodynamic_module.density.temperature_reference = TempFloat;
                        }
                        else
                        {
                            MessageBox.Show("Please enter a valid number for Ambient Temperature");
                            return false;
                        }
                    }
                    if (textBoxMikeScenarioAmbientSalinity.Text.Trim() == "")
                    {
                        m21fm.femEngineHD.hydrodynamic_module.density.salinity_reference = 28;
                    }
                    else
                    {
                        float TempFloat = 0;
                        if (float.TryParse(textBoxMikeScenarioAmbientSalinity.Text.Trim(), out TempFloat))
                        {
                            m21fm.femEngineHD.hydrodynamic_module.density.salinity_reference = TempFloat;
                        }
                        else
                        {
                            MessageBox.Show("Please enter a valid number for Ambient Salinity");
                            return false;
                        }
                    }
                    if (textBoxMikeScenarioManningNumber.Text == "")
                    {
                        m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.constant_value = 25;
                    }
                    else
                    {
                        m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.constant_value = float.Parse(textBoxMikeScenarioManningNumber.Text.Trim());
                    }

                    foreach (MikeSource ms in CurrentMikeSourceList)
                    {
                        m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].Name = "'" + ms.SourceName + "'";
                        m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].include = ms.Include == true ? (int)1 : (int)0;
                        m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].constant_value = (float)(ms.SourceFlow / 24 / 3600);
                        m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].coordinates.y = (float)ms.SourceLat;
                        m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].coordinates.x = (float)ms.SourceLong;

                        m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].constant_value = (float)ms.SourcePollution;
                        m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].format = ms.IsContinuous == true ? (int)0 : (int)1;

                        if (m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].format == 0)
                        {
                            m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].file_name = "||";
                        }
                        else
                        {
                            dfs = new Dfs(Dfs.DFSType.DFS0, true);

                            dfs.FileCreatedDate = DateTime.Now;
                            dfs.FileLastModifiedDate = DateTime.Now;
                            dfs.Equidistant = Dfs.EquidistantOrNonEquidistant.Equidistant;
                            dfs.Title = @"FC MPN / 100 ml";
                            dfs.FileType = "MIKE Zero";
                            dfs.TypeOfAxis = Dfs.AxisType.EquidistantCalendarAxis;
                            dfs.DataStartDate = m21fm.femEngineHD.time.start_time;
                            dfs.UnitCode = Dfs.Unit.Second;

                            CSSPFileNoContent csspFileNoContent = (from cf in vpse.CSSPFileNoContents
                                                                   from msif in vpse.MikeScenarioFiles
                                                                   where cf.CSSPFileID == msif.CSSPFile.CSSPFileID
                                                                   && msif.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                   && msif.CSSPFile.Purpose == "Original"
                                                                   && msif.CSSPFile.FileType == ".dfs0"
                                                                   select cf).FirstOrDefault<CSSPFileNoContent>();

                            if (csspFileNoContent == null)
                            {
                                dfs.TimeSteps = 3600;
                            }
                            else
                            {
                                dfs.TimeSteps = (double)csspFileNoContent.TimeStepsInSecond;
                            }

                            dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                            ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dfs.DataStartDate.Ticks);

                            dfs.NumberOfParameters = 1;
                            dfs.ParameterList = new List<Dfs.Parameter>();
                            Dfs.Parameter p = new Dfs.Parameter();
                            p.Description = "FC MPN";
                            p.Code = Dfs.Parameter.ParameterType.FColiConcentration;
                            p.UnitCode = Dfs.Unit.Per100ml;
                            p.GridStep = (float)0.0;
                            p.GridStepMultipliedBy9 = 0;
                            p.NumberOfElements = 0;
                            p.NumberOfEmptyValues = 0;
                            p.NumberOfGridPoints = 0;
                            p.NumberOfTimeSteps = 0;
                            p.TimeSeriesTypeCode = Dfs.Parameter.TimeSeriesType.Instantaneous;

                            dfs.ParameterList.Add(p);

                            dfs.XValueList = new List<double>();
                            p.ValueList = new List<float>();
                            for (int i = 0; i < dfs.NumberOfValues; i++)
                            {
                                dfs.XValueList.Add(i * dfs.TimeSteps);
                                DateTime TempDate = new DateTime();
                                TempDate = dfs.DataStartDate.AddSeconds(i * dfs.TimeSteps);
                                if (TempDate >= ms.StartDateAndTime && TempDate <= ms.EndDateAndTime)
                                {
                                    p.ValueList.Add((float)ms.SourcePollution);
                                }
                                else
                                {
                                    p.ValueList.Add((float)0);
                                }
                            }

                            p.Maximum = p.ValueList.Max<float>();
                            p.Minimum = p.ValueList.Min<float>();

                            Dfs.Parameter.Stat stat = new Dfs.Parameter.Stat();
                            p.StatList = new List<Dfs.Parameter.Stat>();
                            p.StatList.Add(stat);

                            p.StatList[0].LastValue = p.ValueList[p.ValueList.Count - 1];
                            p.StatList[0].Maximum = p.ValueList.Max<float>();
                            p.StatList[0].Minimum = p.ValueList.Min<float>();
                            p.StatList[0].NumberOfEmptyValues = 0;
                            p.StatList[0].NumberOfTimeSeries = dfs.NumberOfValues;
                            p.StatList[0].NumberOfValuesMinus1 = dfs.NumberOfValues - 1;
                            p.StatList[0].Sum = p.ValueList.Sum();
                            p.StatList[0].SumOfSquareOfValue = 0;
                            p.StatList[0].ValueMultiplicated = 0;
                            foreach (float f in p.ValueList)
                            {
                                p.StatList[0].SumOfSquareOfValue += f * f;
                                p.StatList[0].ValueMultiplicated *= f;
                            }

                            MemoryStream msUpdate = new MemoryStream();

                            msUpdate = dfs.DfsToStream();

                            string TempFileName = string.Format("[{0}] Pol [{1}].dfs0", mikeScenario.MikeScenarioID, ms.SourceName);
                            string TempPath = "";
                            if (m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code.Count > 1) // boundary condition exist
                            {
                                foreach (KeyValuePair<string, M21fm.FemEngineHD.HYDRODYNAMIC_MODULE.BOUNDARY_CONDITIONS.CODE> kvp in m21fm.femEngineHD.hydrodynamic_module.boundary_conditions.code)
                                {
                                    if (kvp.Value.file_name != null)
                                    {
                                        TempPath = kvp.Value.file_name;
                                        break;
                                    }
                                }
                                TempPath = TempPath.Substring(0, TempPath.LastIndexOf("\\") + 1);
                            }

                            m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].file_name = TempPath + TempFileName + "|";

                            CSSPFile csspFileExist = (from cp in vpse.CSSPFiles
                                                      where cp.FileName == TempFileName
                                                      select cp).FirstOrDefault<CSSPFile>();

                            if (csspFileExist != null)
                            {
                                csspFileExist.FileContent = msUpdate.ToArray();
                                csspFileExist.FileSize = csspFileExist.FileContent.Length;
                                csspFileExist.FileCreatedDate = DateTime.Now;
                                csspFileExist.DataStartDate = dateTimePickerScenarioStartDateAndTime.Value;
                                csspFileExist.DataEndDate = dateTimePickerScenarioEndDateAndTime.Value;

                                try
                                {
                                    richTextBoxMikePanelStatus.AppendText("Updating file = [" + csspFileExist.FileName + "]\r\n");
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("Updated file = [" + csspFileExist.FileName + "]\r\n");
                                }
                                catch (Exception ex)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error while updating file = [" + csspFileExist.FileName + "]\r\n");
                                    richTextBoxMikePanelStatus.AppendText("Error message = [" + ex.Message + "].\r\n");
                                }
                            }
                            else
                            {
                                CSSPFile NewInputCSSPFile = new CSSPFile();
                                NewInputCSSPFile.CSSPGuid = Guid.NewGuid();
                                NewInputCSSPFile.Purpose = "InputPol";
                                NewInputCSSPFile.FileName = TempFileName;
                                NewInputCSSPFile.FileOriginalPath = csspFileNoContentInputDfs0List[0].FileOriginalPath;
                                NewInputCSSPFile.FileDescription = "";
                                NewInputCSSPFile.FileContent = msUpdate.ToArray();
                                NewInputCSSPFile.FileType = ".dfs0";
                                NewInputCSSPFile.FileSize = NewInputCSSPFile.FileContent.Length;
                                NewInputCSSPFile.FileCreatedDate = DateTime.Now;
                                NewInputCSSPFile.DataStartDate = dateTimePickerScenarioStartDateAndTime.Value;
                                NewInputCSSPFile.DataEndDate = dateTimePickerScenarioEndDateAndTime.Value;
                                NewInputCSSPFile.TimeStepsInSecond = dfs.TimeSteps;
                                NewInputCSSPFile.ParameterNames = dfs.ParameterList[0].Code.ToString();
                                NewInputCSSPFile.ParameterUnits = dfs.ParameterList[0].UnitCode.ToString();
                                NewInputCSSPFile.IsCompressed = false;


                                try
                                {
                                    vpse.AddToCSSPFiles(NewInputCSSPFile);
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("Copy of file created = [" + NewInputCSSPFile.FileName + "]\r\n");
                                }
                                catch (Exception)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error while copying new scenario file.\r\n");
                                    return false;
                                }

                                MikeScenario MikeScenarioToLink = (from mstl in vpse.MikeScenarios
                                                                   where mstl.MikeScenarioID == mikeScenario.MikeScenarioID
                                                                   select mstl).FirstOrDefault<MikeScenario>();

                                if (MikeScenarioToLink == null)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Could not find MikeScenario where MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in the DB.\r\n");
                                    return false;
                                }

                                MikeScenarioFile NewMikeScenarioFile = new MikeScenarioFile();
                                NewMikeScenarioFile.MikeScenario = MikeScenarioToLink;
                                NewMikeScenarioFile.CSSPFile = NewInputCSSPFile;
                                NewMikeScenarioFile.CSSPParentFile = NewInputCSSPFile;

                                try
                                {
                                    vpse.AddToMikeScenarioFiles(NewMikeScenarioFile);
                                    vpse.SaveChanges();
                                    richTextBoxMikePanelStatus.AppendText("File [" + NewInputCSSPFile.FileName + "] linked to new scenario\r\n");
                                }
                                catch (Exception)
                                {
                                    richTextBoxMikePanelStatus.AppendText("Error file [" + NewInputCSSPFile.FileName + "] could not be linked to new scenario\r\n");
                                }
                            }
                        }

                        m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source[ms.SourceNumberString].temperature.constant_value = (float)ms.SourceTemperature;
                        m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source[ms.SourceNumberString].salinity.constant_value = (float)ms.SourceSalinity;
                    }

                    // Saving the information in the DB Mike___ tables
                    vpse.AcceptAllChanges();

                    MikeScenario mikeScenarioToUpdate = (from ms in vpse.MikeScenarios
                                                         where ms.MikeScenarioID == mikeScenario.MikeScenarioID
                                                         select ms).FirstOrDefault<MikeScenario>();

                    if (mikeScenarioToUpdate == null)
                    {
                        MessageBox.Show("Could not find MikeScenario with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not find MikeScenario with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        return false;
                    }

                    if (textBoxMikeNewScenarioName.Text == null || textBoxMikeNewScenarioName.Text == "")
                    {
                        MessageBox.Show("Please Enter a Unique Scenario Name.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Please Enter a Unique Scenario Name.\r\n");
                        return false;
                    }

                    // checking if scenario already exist in DB
                    MikeScenario mikeScenarioExist = (from ms in vpse.MikeScenarios
                                                      where ms.ScenarioName == textBoxMikeNewScenarioName.Text.Trim()
                                                      && ms.MikeScenarioID != mikeScenario.MikeScenarioID
                                                      select ms).FirstOrDefault<MikeScenario>();

                    if (mikeScenarioExist != null)
                    {
                        MessageBox.Show("Please Enter a Unique Scenario Name. [" + textBoxMikeNewScenarioName.Text + "] already exist.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Please Enter a Unique Scenario Name. [" + textBoxMikeNewScenarioName.Text + "] already exist.\r\n");
                        return false;
                    }

                    mikeScenarioToUpdate.ScenarioName = textBoxMikeNewScenarioName.Text.Trim();
                    mikeScenarioToUpdate.ScenarioStartDateAndTime = m21fm.femEngineHD.time.start_time;
                    mikeScenarioToUpdate.ScenarioEndDateAndTime = m21fm.femEngineHD.time.start_time.AddSeconds(m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval);

                    try
                    {
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Could not save changes in the DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not save changes in the DB.\r\n");
                        return false;
                    }

                    // Doing MikeParameters
                    MikeParameter mikeParameterToUpdate = (from mp in vpse.MikeParameters
                                                           where mp.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                           select mp).FirstOrDefault<MikeParameter>();

                    if (mikeParameterToUpdate == null)
                    {
                        MessageBox.Show("Could not find MikeParameter with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not find MikeParameter with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        return false;
                    }

                    mikeParameterToUpdate.WindSpeed = m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_speed * 3.6;
                    mikeParameterToUpdate.WindDirection = m21fm.femEngineHD.hydrodynamic_module.wind_forcing.constant_direction;
                    mikeParameterToUpdate.DecayFactorPerDay = m21fm.femEngineHD.transport_module.decay.component["COMPONENT_1"].constant_value * 24 * 3600;
                    mikeParameterToUpdate.DecayIsConstant = checkBoxDecayIsConstant.Checked;
                    mikeParameterToUpdate.DecayFactorAmplitude = float.Parse(textBoxMikeScenarioDecayFactorAmplitude.Text);
                    mikeParameterToUpdate.ResultFrequencyInMinutes = int.Parse(textBoxMikeScenarioResultFrequencyInMinutes.Text);
                    mikeParameterToUpdate.AmbientTemperature = m21fm.femEngineHD.hydrodynamic_module.density.temperature_reference;
                    mikeParameterToUpdate.AmbientSalinity = m21fm.femEngineHD.hydrodynamic_module.density.salinity_reference;
                    mikeParameterToUpdate.ManningNumber = m21fm.femEngineHD.hydrodynamic_module.bed_resistance.manning_number.constant_value;

                    try
                    {
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Could not save changes in the DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not save changes in the DB.\r\n");
                        return false;
                    }

                    // Doing MikeSources
                    List<MikeSource> mikeSourceToUpdateList = (from ms in vpse.MikeSources
                                                               where ms.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                               select ms).ToList<MikeSource>();

                    if (mikeSourceToUpdateList.Count == 0)
                    {
                        MessageBox.Show("Could not find MikeSource with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not find MikeSource with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        return false;
                    }

                    foreach (MikeSource ms in mikeSourceToUpdateList)
                    {
                        string TempSourceName = m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].Name.Trim();
                        ms.SourceName = TempSourceName.Substring(1, TempSourceName.Length - 2);
                        ms.Include = m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].include == 1 ? true : false;
                        ms.SourceFlow = m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].constant_value * 24 * 3600;
                        ms.SourceLat = m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].coordinates.y;
                        ms.SourceLong = m21fm.femEngineHD.hydrodynamic_module.sources.source[ms.SourceNumberString].coordinates.x;

                        if (ms.SourcePollution != m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].constant_value)
                        {
                            NeedToChangePollutionInfo = true;
                        }

                        ms.SourcePollution = m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].constant_value;
                        foreach (MikeSource mikeSource in CurrentMikeSourceList)
                        {
                            if (mikeSource.SourceNumberString == ms.SourceNumberString)
                            {
                                ms.StartDateAndTime = mikeSource.StartDateAndTime; ;
                                ms.EndDateAndTime = mikeSource.EndDateAndTime;
                            }
                        }
                        ms.IsContinuous = m21fm.femEngineHD.transport_module.sources.source[ms.SourceNumberString].component["COMPONENT_1"].format == 0 ? true : false;
                        ms.SourceTemperature = m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source[ms.SourceNumberString].temperature.constant_value;
                        ms.SourceSalinity = m21fm.femEngineHD.hydrodynamic_module.temperature_salinity_module.sources.source[ms.SourceNumberString].salinity.constant_value;
                    }

                    try
                    {
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Could not save changes in the DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not save changes in the DB.\r\n");
                        return false;
                    }

                    mikeScenarioToUpdate.ScenarioSummary = GenerateInputSummary(mikeScenarioToUpdate);

                    try
                    {
                        vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Could not save ScenarioSummary in the DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not save ScenarioSummary in the DB.\r\n");
                        return false;
                    }


                    //
                    //
                    // -------------- changing the data in .dfs0 an .dfs1 files to reflect StartTimeChange and EndTimeChange ---------------
                    //
                    //

                    if (NeedToChangePollutionInfo)
                    {
                        List<CSSPFile> csspFileInput = (from cf in vpse.CSSPFiles
                                                        from msf in vpse.MikeScenarioFiles
                                                        where cf.CSSPFileID == msf.CSSPFile.CSSPFileID
                                                        && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                        && (cf.FileType == ".dfs0" || cf.FileType == ".dfs1")
                                                        && cf.Purpose == "Input"
                                                        select cf).ToList<CSSPFile>();

                        foreach (CSSPFile cfInput in csspFileInput)
                        {
                            int? originalCSSPFileID = (from msf in vpse.MikeScenarioFiles
                                                       where msf.CSSPFile.CSSPFileID == cfInput.CSSPFileID
                                                       select msf.CSSPParentFile.CSSPFileID).FirstOrDefault<int>();

                            if (originalCSSPFileID == null)
                            {
                                MessageBox.Show("Could not save ScenarioSummary in the DB.\r\n");
                                richTextBoxMikePanelStatus.AppendText("Could not find MikeScenarioFile with CsspFileID = [" + cfInput.CSSPFileID + "] in the DB.\r\n");
                                return false;
                            }


                            CSSPFile csspFileOriginal = (from cf in vpse.CSSPFiles
                                                         where cf.CSSPFileID == originalCSSPFileID
                                                         && cf.Purpose != null
                                                         select cf).FirstOrDefault<CSSPFile>();

                            if (csspFileOriginal == null)
                            {
                                MessageBox.Show("Could not save ScenarioSummary in the DB.\r\n");
                                richTextBoxMikePanelStatus.AppendText("Could not find CSSPFile with CsspFileID = [" + originalCSSPFileID + "] in the DB.\r\n");
                                return false;
                            }

                            if (csspFileOriginal.FileType.ToLower() == ".dfs0")
                            {
                                dfs = new Dfs(Dfs.DFSType.DFS0, true);
                            }
                            else
                            {
                                dfs = new Dfs(Dfs.DFSType.DFS1, true);
                            }

                            MemoryStream ms = new MemoryStream(csspFileOriginal.FileContent);

                            dfs.StreamToDfs(ms);

                            dfs.FileCreatedDate = DateTime.Now;
                            dfs.FileLastModifiedDate = DateTime.Now;
                            if (dfs.Equidistant != Dfs.EquidistantOrNonEquidistant.Equidistant)
                            {
                                MessageBox.Show("Please use dfs equidistant in dfs0 files.\r\n");
                                richTextBoxMikePanelStatus.AppendText("Please use dfs equidistant in dfs0 files.\r\n");
                                return false;
                            }
                            DateTime StartDateTime = m21fm.femEngineHD.time.start_time;
                            DateTime EndDateTime = m21fm.femEngineHD.time.start_time.AddSeconds(m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval);

                            dfs.Title = dfs.Title + " for scenario [" + mikeScenario.ScenarioName + "]";

                            if (dfs.UnitCode != Dfs.Unit.Second)
                            {
                                MessageBox.Show("Please use dfs unit in second in dfs0 files.\r\n");
                                richTextBoxMikePanelStatus.AppendText("Please use dfs unit in second in dfs0 files.\r\n");
                                return false;
                            }

                            int NumberOfSeconds = m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval;

                            DateTime DfsStartDate = dfs.DataStartDate;

                            ts = new TimeSpan(StartDateTime.Ticks - DfsStartDate.Ticks);

                            dfs.DataStartDate = m21fm.femEngineHD.time.start_time;
                            dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                            if (dfs.NumberOfValues > dfs.ParameterList[0].ValueList.Count)
                            {
                                dfs.NumberOfValues = dfs.ParameterList[0].ValueList.Count;
                            }

                            int NumberOfValuesToRemoveFromStart = (int)(ts.TotalSeconds / dfs.TimeSteps);

                            List<float> TempValueList = new List<float>();
                            dfs.XValueList = new List<double>();
                            for (int i = 0; i < dfs.NumberOfValues; i++)
                            {
                                dfs.XValueList.Add(i * dfs.TimeSteps);
                            }

                            foreach (Dfs.Parameter p in dfs.ParameterList)
                            {
                                dfs.NumberOfValues = (int)((m21fm.femEngineHD.time.number_of_time_steps * m21fm.femEngineHD.time.time_step_interval) / dfs.TimeSteps) + 1;

                                if (dfs.NumberOfValues > p.ValueList.Count)
                                {
                                    dfs.NumberOfValues = p.ValueList.Count;
                                }

                                TempValueList = new List<float>();
                                for (int i = NumberOfValuesToRemoveFromStart; i < (NumberOfValuesToRemoveFromStart + dfs.NumberOfValues); i++)
                                {
                                    TempValueList.Add(p.ValueList[i]);
                                }

                                p.ValueList = new List<float>();
                                foreach (float f in TempValueList)
                                {
                                    p.ValueList.Add(f);
                                }

                                p.Maximum = p.ValueList.Max<float>();
                                p.Minimum = p.ValueList.Min<float>();

                                foreach (Dfs.Parameter.Stat s in p.StatList)
                                {
                                    s.LastValue = p.ValueList[p.ValueList.Count - 1];
                                    s.Maximum = p.ValueList.Max<float>();
                                    s.Minimum = p.ValueList.Min<float>();
                                    s.NumberOfTimeSeries = dfs.NumberOfValues;
                                    s.NumberOfValuesMinus1 = dfs.NumberOfValues - 1;
                                    s.Sum = p.ValueList.Sum();
                                    s.SumOfSquareOfValue = 0;
                                    s.ValueMultiplicated = 0;
                                    foreach (float f in p.ValueList)
                                    {
                                        s.SumOfSquareOfValue += f * f;
                                        s.ValueMultiplicated *= f;
                                    }
                                }

                                if (p.Code == Dfs.Parameter.ParameterType.WaterLevel)
                                {
                                    m21fm.femEngineHD.hydrodynamic_module.initial_conditions.surface_elevation_constant = p.ValueList[0];
                                }
                            }

                            MemoryStream msUpdate = new MemoryStream();

                            msUpdate = dfs.DfsToStream();

                            cfInput.FileSize = msUpdate.Length;
                            cfInput.FileCreatedDate = DateTime.Now;
                            cfInput.FileContent = msUpdate.ToArray();
                            cfInput.DataStartDate = dfs.DataStartDate;
                            cfInput.DataEndDate = dfs.DataStartDate.AddSeconds(dfs.TimeSteps * dfs.XValueList.Count);

                            try
                            {
                                richTextBoxMikePanelStatus.AppendText("Updating file " + cfInput.FileName + " in DB .\r\n");
                                vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                                richTextBoxMikePanelStatus.AppendText("Updated file " + cfInput.FileName + " in DB .\r\n");
                            }
                            catch (Exception)
                            {
                                MessageBox.Show("Could not update file " + cfInput.FileName + " in DB .\r\n");
                                richTextBoxMikePanelStatus.AppendText("Could not update file " + cfInput.FileName + " in DB .\r\n");
                                return false;
                            }

                        }
                    }


                    // need to recreate the file m21fm and filename
                    CSSPFile csspm2fmFile = (from cp in vpse.CSSPFiles
                                             from msf in vpse.MikeScenarioFiles
                                             where cp.CSSPFileID == msf.CSSPFile.CSSPFileID
                                             && msf.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                             && cp.FileType == ".m21fm"
                                             select cp).FirstOrDefault<CSSPFile>();

                    if (csspm2fmFile == null)
                    {
                        MessageBox.Show("Could not find CSSPFile of type .m21fm with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not find CSSPFile of type .m21fm with MikeScenarioID = [" + mikeScenario.MikeScenarioID + "] in DB.\r\n");
                        return false;
                    }

                    csspm2fmFile.FileName = textBoxMikeNewScenarioName.Text.Trim() + csspm2fmFile.FileType;
                    csspm2fmFile.FileContent = m21fm.M21fmToStream().ToArray();

                    try
                    {
                        vpse.SaveChanges();
                        richTextBoxMikePanelStatus.AppendText("FileContent changed in DB.\r\n");
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Could not changed FileContent in DB.\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not changed FileContent in DB.\r\n");
                        return false;
                    }

                }
            }

            return true;
        }
        private void ScenarioHasChanged()
        {
            butMikeNewScenarioSave.Enabled = true;
            butMikeNewScenarioRun.Enabled = false;
        }
        private void ShowMikeError(Exception ex)
        {
            MessageBox.Show("Error while saving information to CSSP DB.\r\n\r\nYou can check the bottom richtextbox to see the detail.");

            richTextBoxMikePanelStatus.AppendText("\r\nError Message - [" + ex.Message + "]\r\n");
            if (ex.InnerException != null)
            {
                richTextBoxMikePanelStatus.AppendText("\r\nInner Error Message - [" + ex.InnerException.Message + "]\r\n");
            }
        }
        private bool UploadOtherScenarioFilesToDB(string FileName, MikeScenario NewMikeScenario)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();
            bool FileExist = false;

            richTextBoxMikePanelStatus.AppendText("\r\n\r\nFile [" + FileName + "]\r\n");

            if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1 || NewMikeScenario != null)
            {
                if (NewMikeScenario == null)
                {
                    NewMikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                }

                MikeScenario mikeScenario = (from ms in vpse.MikeScenarios
                                             where ms.MikeScenarioID == NewMikeScenario.MikeScenarioID
                                             select ms).FirstOrDefault<MikeScenario>();

                if (mikeScenario == null)
                {
                    MessageBox.Show("Error trying to read MikeScenario with MikeScenairoID = [" + NewMikeScenario.MikeScenarioID + "].");
                    richTextBoxMikePanelStatus.AppendText("Error trying to read MikeScenario with MikeScenairoID = [" + NewMikeScenario.MikeScenarioID + "].\r\n");
                    return false;
                }

                FileInfo fi = new FileInfo(FileName);
                if (fi.Extension == ".m21fm")
                {
                    MessageBox.Show("Only one .m21fm file can be loaded per scenario. Please use Add m21fm file and associate file");
                    richTextBoxMikePanelStatus.AppendText("Only one .m21fm file can be loaded per scenario. Please use Add m21fm file and associate file.\r\n");
                    richTextBoxMikePanelStatus.AppendText("Canceled file upload.\r\n");
                    return false;
                }
                FileStream fs = fi.OpenRead();

                string FilePath = FileName.Substring(0, FileName.LastIndexOf("\\") + 1);
                string ShortFileName = FileName.Substring(FileName.LastIndexOf("\\") + 1);

                Application.DoEvents();

                //Read all file bytes into an array from the specified file.
                int nBytes = (int)fi.Length;
                Byte[] ByteArray = new byte[nBytes];
                int nBytesRead = fs.Read(ByteArray, 0, nBytes);

                fs.Close();

                richTextBoxMikePanelStatus.AppendText("Checking if file already in DB ...\r\n");
                Application.DoEvents();

                TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;

                CSSPItem csspItem = (from ci in vpse.CSSPItems
                                     where ci.CSSPItemID == CurrentTVI.ItemID
                                     select ci).FirstOrDefault<CSSPItem>();

                if (csspItem == null)
                {
                    MessageBox.Show("Error: Could not find CSSPItems with CSSPItemID = [" + CurrentTVI.ItemID + "] in the DB");
                    return false;
                }

                // should check if file is already in DB

                //string TheFileName = fi.FullName.Substring(fi.FullName.LastIndexOf("\\") + 1);
                CSSPFile csspFileExist = (from f in vpse.CSSPFiles
                                          where f.FileName == ShortFileName
                                          && f.FileOriginalPath == FilePath
                                          && f.FileType == fi.Extension
                                          && f.FileSize == fi.Length
                                          select f).FirstOrDefault<CSSPFile>();

                if (csspFileExist != null)
                {
                    byte[] ByteArray2 = csspFileExist.FileContent;

                    richTextBoxMikePanelStatus.AppendText("Comparing saved file and file to upload in DB ...\r\n");

                    // kmz file should just replace the content of the KMZ file in the DB
                    if (fi.Extension.ToLower() == ".kmz")
                    {
                        FileExist = true;
                    }
                    else
                    {
                        FileExist = BinIdentical(ByteArray, ByteArray2);
                        if (FileExist)
                        {
                            MessageBox.Show("The file \r\n[" + FileName + "]\r\n already exist in DB");
                            richTextBoxMikePanelStatus.AppendText("File already in DB ...\r\n");
                            //return false;
                        }
                    }
                }

                if (!FileExist)
                {
                    richTextBoxMikePanelStatus.AppendText("File does not exist in DB ...\r\n");
                    richTextBoxMikePanelStatus.AppendText("Saving file in DB ...\r\n");
                    Application.DoEvents();

                    CSSPFile csspFile = new CSSPFile();
                    csspFile.CSSPGuid = Guid.NewGuid();
                    csspFile.FileName = ShortFileName;
                    csspFile.FileOriginalPath = FilePath;
                    csspFile.FileSize = fi.Length;
                    csspFile.FileDescription = "";
                    csspFile.FileCreatedDate = fi.CreationTime;
                    csspFile.FileType = fi.Extension;
                    csspFile.FileContent = ByteArray;
                    if (fi.Extension.ToLower() == ".kmz")
                    {
                        csspFile.Purpose = PurposeType.KMZResult.ToString();
                    }
                    else if (fi.Extension.ToLower() == ".dfsu")
                    {
                        csspFile.Purpose = PurposeType.MikeResult.ToString();
                    }
                    else
                    {
                        if (radioButtonViewMunicipalityOthers.Checked)
                        {
                            csspFile.Purpose = PurposeType.MunicipalityOther.ToString();
                        }
                        else
                        {
                            csspFile.Purpose = PurposeType.MikeScenarioOther.ToString();
                        }
                    }

                    try
                    {
                        vpse.AddToCSSPFiles(csspFile);
                        vpse.SaveChanges();

                        richTextBoxMikePanelStatus.AppendText("File saved in DB ...\r\n");
                        Application.DoEvents();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not store file [" + FileName + "] in DB\r\n" + ex.Message + "\r\n");
                        richTextBoxMikePanelStatus.AppendText("Could not store file [" + FileName + "] in DB\r\n" + ex.Message + "\r\n");
                        Application.DoEvents();
                        return false;
                    }

                    if (radioButtonViewMunicipalityOthers.Checked)
                    {
                        CSSPItemFile csspItemFile = new CSSPItemFile();
                        csspItemFile.CSSPFile = csspFile;
                        csspItemFile.CSSPItem = csspItem;

                        try
                        {
                            vpse.AddToCSSPItemFiles(csspItemFile);
                            vpse.SaveChanges();

                            richTextBoxMikePanelStatus.AppendText("Linked file to Municipality in DB ...\r\n");
                            Application.DoEvents();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not Link file [" + FileName + "] to Municipality in DB\r\n" + ex.Message + "\r\n");
                            richTextBoxMikePanelStatus.AppendText("Could not Link file [" + FileName + "] to Municipality in DB\r\n" + ex.Message + "\r\n");
                            Application.DoEvents();
                            return false;
                        }
                    }
                    else
                    {
                        MikeScenarioFile mikeScenarioFile = new MikeScenarioFile();
                        mikeScenarioFile.MikeScenario = mikeScenario;
                        mikeScenarioFile.CSSPFile = csspFile;
                        mikeScenarioFile.CSSPParentFile = csspFile;

                        try
                        {
                            vpse.AddToMikeScenarioFiles(mikeScenarioFile);
                            vpse.SaveChanges();

                            richTextBoxMikePanelStatus.AppendText("Linked file to scenario in DB ...\r\n");
                            Application.DoEvents();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not Link file [" + FileName + "] to scenario in DB\r\n" + ex.Message + "\r\n");
                            richTextBoxMikePanelStatus.AppendText("Could not Link file [" + FileName + "] to scenario in DB\r\n" + ex.Message + "\r\n");
                            Application.DoEvents();
                            return false;
                        }
                    }
                }
                else
                {
                    if (fi.Extension.ToLower() == ".kmz")
                    {
                        csspFileExist.FileSize = fi.Length;
                        csspFileExist.FileDescription = "";
                        csspFileExist.FileCreatedDate = fi.CreationTime;
                        csspFileExist.FileContent = ByteArray;

                        try
                        {
                            vpse.SaveChanges(SaveOptions.AcceptAllChangesAfterSave);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("The KMZ created file content could not be changed for the scenario [" + mikeScenario.ScenarioName + "] in DB\r\n");
                            richTextBoxMikePanelStatus.AppendText("The KMZ created file content could not be changed for the scenario [" + mikeScenario.ScenarioName + "] in DB\r\n");
                            Application.DoEvents();
                        }
                    }

                    MikeScenarioFile mikeScenarioFileExist = (from ms in vpse.MikeScenarioFiles
                                                              where ms.CSSPFile.CSSPFileID == csspFileExist.CSSPFileID
                                                              && ms.MikeScenario.MikeScenarioID == mikeScenario.MikeScenarioID
                                                              select ms).FirstOrDefault<MikeScenarioFile>();

                    if (mikeScenarioFileExist != null)
                    {
                        if (fi.Extension.ToLower() == ".kmz")
                        {
                            // this is not an error because we just replace the content of the CSSPFile and keep the MikeScenarioFile link
                        }
                        else
                        {
                            MessageBox.Show("File already exist for scenario [" + mikeScenario.ScenarioName + "] in DB\r\n");
                            richTextBoxMikePanelStatus.AppendText("File already exist for scenario [" + mikeScenario.ScenarioName + "] in DB\r\n");
                            Application.DoEvents();
                        }
                    }
                    else
                    {
                        richTextBoxMikePanelStatus.AppendText("Storing file [" + FileName + "] in DB\r\n");

                        if (radioButtonViewMunicipalityOthers.Checked)
                        {
                            CSSPItemFile csspItemFile = new CSSPItemFile();
                            csspItemFile.CSSPFile = csspFileExist;
                            csspItemFile.CSSPItem = csspItem;

                            try
                            {
                                vpse.AddToCSSPItemFiles(csspItemFile);
                                vpse.SaveChanges();

                                richTextBoxMikePanelStatus.AppendText("Linked file to Municipality in DB ...\r\n");
                                Application.DoEvents();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Could not Link file [" + FileName + "] to Municipality in DB\r\n" + ex.Message + "\r\n");
                                richTextBoxMikePanelStatus.AppendText("Could not Link file [" + FileName + "] to Municipality in DB\r\n" + ex.Message + "\r\n");
                                Application.DoEvents();
                                return false;
                            }
                        }
                        else
                        {
                            MikeScenarioFile mikeScenarioFile = new MikeScenarioFile();
                            mikeScenarioFile.MikeScenario = mikeScenario;
                            mikeScenarioFile.CSSPFile = csspFileExist;
                            mikeScenarioFile.CSSPParentFile = csspFileExist;

                            try
                            {
                                vpse.AddToMikeScenarioFiles(mikeScenarioFile);
                                vpse.SaveChanges();

                                richTextBoxMikePanelStatus.AppendText("Linked file to scenario in DB ...\r\n");
                                Application.DoEvents();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Could not Link file [" + FileName + "] to scenario in DB\r\n" + ex.Message + "\r\n");
                                richTextBoxMikePanelStatus.AppendText("Could not Link file [" + FileName + "] to scenario in DB\r\n" + ex.Message + "\r\n");
                                Application.DoEvents();
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {
            }


            return true;
        }
        private void ViewTextFile(TVI CurrentTVI, CSSPFileNoContent csspFileNoContent)
        {
            CSSPAppDBEntities vpse = new CSSPAppDBEntities();

            CSSPFile csspFileExist = (from f in vpse.CSSPFiles
                                      where f.CSSPFileID == csspFileNoContent.CSSPFileID
                                      select f).FirstOrDefault<CSSPFile>();

            if (csspFileExist == null)
            {
                MessageBox.Show("Could not read the DB");
                return;
            }

            string[] FileTypeArray = new string[] { ".m21fm", ".txt", ".log", ".mdf", ".mesh", ".kml" };
            if (FileTypeArray.Contains(csspFileNoContent.FileType.ToLower()))
            {
                richTextBoxMikePanelStatus.Text = Encoding.UTF8.GetString(csspFileExist.FileContent);
            }
            else if (csspFileNoContent.FileType.ToLower().StartsWith(".dfs"))
            {
                if (csspFileNoContent.FileType.ToLower() == ".dfs0")
                {
                    CreateNewDfsWithEvents(Dfs.DFSType.DFS0, false);
                }
                else if (csspFileNoContent.FileType.ToLower() == ".dfs1")
                {
                    CreateNewDfsWithEvents(Dfs.DFSType.DFS1, false);
                }
                else if (csspFileNoContent.FileType.ToLower() == ".dfsu")
                {
                    CreateNewDfsWithEvents(Dfs.DFSType.DFSU, false);
                }
                else
                {
                    MessageBox.Show("Can't read file type [" + csspFileNoContent.FileType + "]");
                    return;
                }

                MemoryStream ms = new MemoryStream(csspFileExist.FileContent);
                dfs.StreamToDfs(ms);
                richTextBoxMikePanelStatus.Text = "";
                richTextBoxMikePanelStatus.AppendText(string.Format("File Name: {0}\r\n", csspFileNoContent.FileOriginalPath + csspFileNoContent.FileName));
                richTextBoxMikePanelStatus.Text = dfs.sbFileTxt.ToString();
            }
            else if (csspFileNoContent.FileType.ToLower() == ".kmz")
            {
                richTextBoxMikePanelStatus.Clear();
                MemoryStream ms = new MemoryStream(csspFileExist.FileContent);
                MemoryStream msUnzipped = KMZToKML(ms);
                msUnzipped.Position = 0;
                StreamReader sr = new StreamReader(msUnzipped);
                string TheFileTxt = sr.ReadToEnd();
                richTextBoxMikePanelStatus.AppendText(TheFileTxt);
                sr.Close();
                msUnzipped.Close();
                ms.Close();
            }
            else
            {
                MessageBox.Show("Can't read file type [" + csspFileNoContent.FileType + "]");
                return;
            }
        }
        #endregion Functions
        #region Events
        private void butAddFiles_Click(object sender, EventArgs e)
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Add file(s) in DB started.\r\n");
            MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
            if (!AddFileInDB(mikeScenario))
            {
                richTextBoxMikePanelStatus.AppendText("Error ...\r\n");
            }
            else
            {
                richTextBoxMikePanelStatus.AppendText("Add file(s) in DB completed.\r\n");
            }
            if (mikeScenario != null)
            {
                FillDataGridViewMikeScenairosInDB(mikeScenario.MikeScenarioID);
            }
            else
            {
                FillDataGridViewMikeScenairosInDB(0);
            }
        }
        private void butAddm21fmFileAndAssociatedFilesInDB_Click(object sender, EventArgs e)
        {
            richTextBoxMikePanelStatus.Clear();
            richTextBoxMikePanelStatus.AppendText("Add m21fm file and associated files in DB started.\r\n");
            if (!Addm21fmFileInDB())
            {
                richTextBoxMikePanelStatus.AppendText("Cancelled Loading of m21fm file ...\r\n");
                CSSPAppDBEntities vpse = new CSSPAppDBEntities();
                MikeScenario mikeScenario = (from ms in vpse.MikeScenarios
                                             orderby ms.MikeScenarioID descending
                                             select ms).FirstOrDefault<MikeScenario>();

                if (mikeScenario != null)
                {
                    FillDataGridViewMikeScenairosInDB(mikeScenario.MikeScenarioID);
                }
                else
                {
                    FillDataGridViewMikeScenairosInDB(0);
                }
            }
            else
            {
                richTextBoxMikePanelStatus.AppendText("Add m21fm file and associated files in DB completed.\r\n");
            }
        }
        private void butDownloadFiles_Click(object sender, EventArgs e)
        {
            DownloadFiles();
        }
        private void butGenerateScenarioInputAndResults_Click(object sender, EventArgs e)
        {
            CreateKMZResultFiles(KMZResultType.InputAndResult, null);
        }
        private void butMikeFindLargestTide_Click(object sender, EventArgs e)
        {
            FindMonthlyHighAndLowTide(TideType.High);
        }
        private void butMikeFindSmallestTide_Click(object sender, EventArgs e)
        {
            FindMonthlyHighAndLowTide(TideType.Low);
        }
        private void butMikeNewScenarioCreateFromSelected_Click(object sender, EventArgs e)
        {
            MikeNewScenarioCreateFromSelected();
        }
        private void butMikeNewScenarioRun_Click(object sender, EventArgs e)
        {
            MikeNewScenarioRun();
        }
        private void butMikeNewScenarioSave_Click(object sender, EventArgs e)
        {
            MikeNewScenarioSave();
        }
        private void butMikeScenarioAddNewSource_Click(object sender, EventArgs e)
        {
            MikeScenarioAddNewSource();
        }
        private void butMikeScenarioRemoveSource_Click(object sender, EventArgs e)
        {
            MikeScenarioRemoveSource();
        }
        private void butRefreshScenarioList_Click(object sender, EventArgs e)
        {
            MikeScenario mikeScenario;
            if (dataGridViewMikeScenairosInDB.SelectedRows.Count > 0)
            {
                mikeScenario = (MikeScenario)(dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem);
                FillDataGridViewMikeScenairosInDB(mikeScenario.MikeScenarioID);
            }
            else
            {
                FillDataGridViewMikeScenairosInDB(-1);
            }
            FillAfterSelectMikeScenario();
        }
        private void butRemoveFileFromDB_Click(object sender, EventArgs e)
        {
            RemoveFileFromDB();
        }
        private void butRemoveScenarioFromDB_Click(object sender, EventArgs e)
        {
            RemoveScenarioFromDB();
        }
        private void butScenarioSummary_Click(object sender, EventArgs e)
        {
            richTextBoxMikePanelStatus.Clear();
            if (dataGridViewMikeScenairosInDB.SelectedRows.Count > 0)
            {
                if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
                {
                    MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                    richTextBoxMikePanelStatus.Text = GenerateInputSummary(mikeScenario);
                }
            }
        }
        private void butViewTextFile_Click(object sender, EventArgs e)
        {
            TVI CurrentTVI = (TVI)treeViewItems.SelectedNode.Tag;
            CSSPFileNoContent csspFileNoContent = (CSSPFileNoContent)dataGridViewScenarioFiles.SelectedRows[0].DataBoundItem;

            if (dataGridViewScenarioFiles.SelectedRows.Count == 1)
            {
                ViewTextFile(CurrentTVI, csspFileNoContent);
            }
        }
        private void checkBoxDecayIsConstant_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDecayIsConstant.Checked)
            {
                lblDecayFactorAmplitude.Enabled = false;
                textBoxMikeScenarioDecayFactorAmplitude.Enabled = false;
            }
            else
            {
                lblDecayFactorAmplitude.Enabled = true;
                textBoxMikeScenarioDecayFactorAmplitude.Enabled = true;
            }
            ScenarioHasChanged();
        }
        private void checkBoxFlowContinuous_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxFlowContinuous.Checked)
            {
                groupBoxNotContinuous.Enabled = false;
            }
            else
            {
                groupBoxNotContinuous.Enabled = true;
            }

            CheckBox cb = (CheckBox)sender;
            if (cb.Focused)
            {
                ScenarioHasChanged();
            }
        }
        private void checkBoxMikeSouceIncluded_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Focused)
            {
                ScenarioHasChanged();
            }
        }
        private void comboBoxMikeScenarioSourceName_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBoxMikeScenarioSourceNameSelectionIndexChanged();
        }
        private void dataGridViewScenarioFiles_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "CSSPFileID":
                    e.Column.HeaderText = "ID";
                    break;
                case "FileName":
                    e.Column.HeaderText = "File name";
                    e.Column.Width = 300;
                    break;
                case "FileOriginalPath":
                    e.Column.HeaderText = "Directory";
                    break;
                case "Purpose":
                    e.Column.HeaderText = "Purpose";
                    break;
                case "FileType":
                    e.Column.HeaderText = "File type";
                    break;
                case "FileSize":
                    e.Column.HeaderText = "File size";
                    break;
                case "CSSPGuid":
                case "FileDescription":
                case "FileContent":
                case "FileCreatedDate":
                case "IsCompressed":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                case "CSSPItemFiles":
                case "MikeScenarioFiles":
                    e.Column.Visible = false;
                    break;
                default:
                    break;
            }
        }
        private void dataGridViewScenarioFiles_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewScenarioFiles.SelectedRows.Count > 0)
            {
                butRemoveFileFromDB.Enabled = true;
                butDownloadFiles.Enabled = true;
                CSSPFileNoContent csspFileNoContent = (CSSPFileNoContent)dataGridViewScenarioFiles.SelectedRows[0].DataBoundItem;
                //string[] FileTypeArray = new string[] { ".m21fm", ".txt", ".log", ".mdf", ".mesh", ".kml" };
                if (dataGridViewScenarioFiles.SelectedRows.Count == 1)
                {
                    butViewTextFile.Enabled = true;
                }
                else
                {
                    butViewTextFile.Enabled = false;
                }
            }
            else
            {
                butRemoveFileFromDB.Enabled = false;
                butViewTextFile.Enabled = false;
                butDownloadFiles.Enabled = false;
            }
        }
        private void dataGridViewMikeScenairosInDB_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            switch (e.Column.DataPropertyName)
            {
                case "MikeScenarioID":
                    e.Column.HeaderText = "ID";
                    break;
                case "ScenarioName":
                    e.Column.HeaderText = "Name";
                    e.Column.Width = 300;
                    break;
                case "ScenarioStatus":
                    e.Column.HeaderText = "Status";
                    break;
                case "ScenarioStartDateAndTime":
                    e.Column.HeaderText = "Start Time";
                    break;
                case "ScenarioEndDateAndTime":
                    e.Column.HeaderText = "End Time";
                    break;
                case "ScenarioStartExecutionDateAndTime":
                    e.Column.HeaderText = "Start execution at";
                    break;
                case "ScenarioExecutionTimeInMinutes":
                    e.Column.HeaderText = "Execution time (min)";
                    break;
                case "CSSPItemID":
                case "ScenarioSummary":
                case "LastModifiedDate":
                case "ModifiedByID":
                case "IsActive":
                case "CSSPItem":
                case "MikeParameters":
                case "MikeScenarioFiles":
                case "MikeSources":
                    e.Column.Visible = false;
                    break;
                default:
                    break;
            }
        }
        private void dataGridViewMikeScenairosInDB_SelectionChanged(object sender, EventArgs e)
        {
            butMikeNewScenarioSave.Enabled = false;
            if (dataGridViewMikeScenairosInDB.SelectedRows.Count > 0)
            {
                butRemoveScenarioFromDB.Enabled = true;
                butMikeNewScenarioCreateFromSelected.Enabled = true;
                butScenarioSummary.Enabled = true;
                MikeScenario mikeScenario = (MikeScenario)dataGridViewMikeScenairosInDB.SelectedRows[0].DataBoundItem;
                if (dataGridViewMikeScenairosInDB.SelectedRows.Count == 1)
                {
                    FillAfterSelectMikeScenario();
                }
                else
                {
                    MikeScenarioParamSourceInputResultsClear();
                    butAddFiles.Enabled = false;
                    butMikeNewScenarioCreateFromSelected.Enabled = false;
                    butScenarioSummary.Enabled = false;
                }
            }
            else
            {
                butRemoveScenarioFromDB.Enabled = false;
                butAddFiles.Enabled = false;
                butMikeNewScenarioCreateFromSelected.Enabled = false;
                butScenarioSummary.Enabled = false;
                MikeScenarioParamSourceInputResultsClear();
            }
            FillAfterSelectMikeScenario();
        }
        private void dateTimePickerScenarioEndDateAndTime_Leave(object sender, EventArgs e)
        {
            if (dateTimePickerScenarioStartDateAndTime.Value != null)
            {
                if (dateTimePickerScenarioStartDateAndTime.Value >= dateTimePickerScenarioEndDateAndTime.Value)
                {
                    MessageBox.Show("Scenario start date and time needs to be < than end date and time");
                    return;
                }
                TimeSpan ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dateTimePickerScenarioStartDateAndTime.Value.Ticks);

                lblScenarioLengthDays.Text = ts.Days.ToString();
                lblScenarioLengthHours.Text = ts.Hours.ToString();
                lblScenarioLengthMinutes.Text = ts.Minutes.ToString();
                int Days = 0;
                int Hours = 0;
                int Minutes = 0;
                int.TryParse(lblScenarioLengthDays.Text.Trim(), out Days);
                int.TryParse(lblScenarioLengthHours.Text.Trim(), out Hours);
                int.TryParse(lblScenarioLengthMinutes.Text.Trim(), out Minutes);
            }
        }
        private void dateTimePickerScenarioStartDateAndTime_Leave(object sender, EventArgs e)
        {
            if (dateTimePickerScenarioEndDateAndTime.Value != null)
            {
                if (dateTimePickerScenarioStartDateAndTime.Value >= dateTimePickerScenarioEndDateAndTime.Value)
                {
                    MessageBox.Show("Scenario start date and time needs to be < than end date and time");
                    return;
                }
                TimeSpan ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dateTimePickerScenarioStartDateAndTime.Value.Ticks);

                lblScenarioLengthDays.Text = ts.Days.ToString();
                lblScenarioLengthHours.Text = ts.Hours.ToString();
                lblScenarioLengthMinutes.Text = ts.Minutes.ToString();
                int Days = 0;
                int Hours = 0;
                int Minutes = 0;
                int.TryParse(lblScenarioLengthDays.Text.Trim(), out Days);
                int.TryParse(lblScenarioLengthHours.Text.Trim(), out Hours);
                int.TryParse(lblScenarioLengthMinutes.Text.Trim(), out Minutes);
            }
        }
        private void dateTimePickerSourcePollutionEndDateAndTime_ValueChanged(object sender, EventArgs e)
        {
            DateTimePicker dtp = new DateTimePicker();
            dtp = (DateTimePicker)sender;
            if (dtp.Focused)
            {
                if (dateTimePickerSourcePollutionStartDateAndTime.Value > dateTimePickerScenarioEndDateAndTime.Value)
                {
                    MessageBox.Show("Source start date should be < end date.");
                    return;
                }

                ScenarioHasChanged();
            }
        }
        private void dateTimePickerSourcePollutionStartDateAndTime_ValueChanged(object sender, EventArgs e)
        {
            DateTimePicker dtp = new DateTimePicker();
            dtp = (DateTimePicker)sender;
            if (dtp.Focused)
            {
                if (dateTimePickerSourcePollutionStartDateAndTime.Value > dateTimePickerScenarioEndDateAndTime.Value)
                {
                    MessageBox.Show("Source start date should be < end date.");
                    return;
                }

                ScenarioHasChanged();
            }
        }
        private void dfs_MessageEvent(object sender, Dfs.DfsMessageEventArgs e)
        {
            richTextBoxMikePanelStatus.AppendText(e.Message);
            Application.DoEvents();
        }
        private void kmz_KMLMessageEvent(object sender, KMZ.MessageEventArgs e)
        {
            richTextBoxMikePanelStatus.AppendText(e.Message);
            Application.DoEvents();
        }
        private void m21fm_MessageEvent(object sender, M21fm.M21fmMessageEventArgs e)
        {
            richTextBoxMikePanelStatus.AppendText(e.Message);
            Application.DoEvents();
        }
        private void radioButtonViewInputs_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewInputs.Checked)
                FillAfterSelectMikeScenario();
        }
        private void radioButtonViewKMZResults_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewKMZResults.Checked)
                FillAfterSelectMikeScenario();
        }
        private void radioButtonViewOriginals_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewOriginals.Checked)
                FillAfterSelectMikeScenario();
        }
        private void radioButtonViewMikeScenarioOthers_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewMikeScenarioOthers.Checked)
                FillAfterSelectMikeScenario();
        }
        private void radioButtonViewMunicipalityOthers_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewMunicipalityOthers.Checked)
                FillAfterSelectMikeScenario();
        }
        private void radioButtonViewResults_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonViewMikeResults.Checked)
                FillAfterSelectMikeScenario();
        }
        private void textBoxMikeSouceFlowInCubicMeterPerDay_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMikeSouceFlowInCubicMeterPerDay.Focused)
            {
                try
                {
                    textBoxMikeSouceFlowInCubicMeterPerSecond.Text = string.Format("{0:F8}", float.Parse(textBoxMikeSouceFlowInCubicMeterPerDay.Text) / 24 / 3600);
                }
                catch (Exception)
                {
                    textBoxMikeSouceFlowInCubicMeterPerSecond.Text = "";
                }
                ScenarioHasChanged();
            }
        }
        private void textBoxMikeSouceFlowInCubicMeterPerSecond_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMikeSouceFlowInCubicMeterPerSecond.Focused)
            {
                try
                {
                    textBoxMikeSouceFlowInCubicMeterPerDay.Text = string.Format("{0:F2}", float.Parse(textBoxMikeSouceFlowInCubicMeterPerSecond.Text) * 24 * 3600);
                }
                catch (Exception)
                {
                    textBoxMikeSouceFlowInCubicMeterPerDay.Text = "";
                }
                ScenarioHasChanged();
            }
        }
        private void textBoxMikeScenarioWindSpeedKilometerPerHour_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMikeScenarioWindSpeedKilometerPerHour.Focused)
            {
                try
                {
                    textBoxMikeScenarioWindSpeedMeterPerSecond.Text = string.Format("{0:F8}", float.Parse(textBoxMikeScenarioWindSpeedKilometerPerHour.Text) / 3.6);
                }
                catch (Exception)
                {
                    textBoxMikeScenarioWindSpeedMeterPerSecond.Text = "";
                }
                ScenarioHasChanged();
            }
        }
        private void textBoxMikeScenarioWindSpeedMeterPerSecond_TextChanged(object sender, EventArgs e)
        {
            if (textBoxMikeScenarioWindSpeedMeterPerSecond.Focused)
            {
                try
                {
                    textBoxMikeScenarioWindSpeedKilometerPerHour.Text = string.Format("{0:F8}", float.Parse(textBoxMikeScenarioWindSpeedMeterPerSecond.Text) * 3.6);
                }
                catch (Exception)
                {
                    textBoxMikeScenarioWindSpeedKilometerPerHour.Text = "";
                }
                ScenarioHasChanged();
            }
        }
        private void textBoxNewSourceName_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = new TextBox();
            tb = (TextBox)sender;
            if (tb.Focused)
            {
                butMikeScenarioAddNewSource.Enabled = true;
                butMikeScenarioRemoveSource.Enabled = false;
                for (int i = 0; i < comboBoxMikeScenarioSourceName.Items.Count; i++)
                {
                    if (textBoxNewSourceName.Text.Trim() == ((MikeSource)comboBoxMikeScenarioSourceName.Items[i]).SourceName.Trim())
                    {
                        butMikeScenarioAddNewSource.Enabled = false;
                        butMikeScenarioRemoveSource.Enabled = true;
                        break;
                    }
                }
            }
            ScenarioHasChanged();
        }
        private void ScenarioHasChanged(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(TextBox))
            {
                TextBox tb = (TextBox)sender;
                if (tb.Focused)
                {
                    ScenarioHasChanged();
                }
            }

            if (sender.GetType() == typeof(DateTimePicker))
            {
                if (dateTimePickerScenarioStartDateAndTime.Value != null && dateTimePickerScenarioEndDateAndTime.Value != null)
                {
                    TimeSpan ts = new TimeSpan(dateTimePickerScenarioEndDateAndTime.Value.Ticks - dateTimePickerScenarioStartDateAndTime.Value.Ticks);

                    lblScenarioLengthDays.Text = string.Format("{0:F0}", ts.Days);
                    lblScenarioLengthHours.Text = string.Format("{0:F0}", ts.Hours);
                    lblScenarioLengthMinutes.Text = string.Format("{0:F0}", ts.Minutes);
                }
                else
                {
                    lblScenarioLengthDays.Text = "";
                    lblScenarioLengthHours.Text = "";
                    lblScenarioLengthMinutes.Text = "";
                }

                ScenarioHasChanged();
            }
        }
        #endregion Events
        #endregion MIKE

    }
}
