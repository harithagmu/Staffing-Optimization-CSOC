using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using MySql.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
//using csvHelper

namespace ProjectGK
{
    //Analyst class
    public class Analyst
    {
        private string _name;
        private bool _status;
        private string _team;
        private string _taskMap;
        private int _day;
        public Analyst(string name, bool status, string team, string taskMap, int startday)
        {
            Name = name;
            Status = status;
            Team = team;
            TaskMap = taskMap;
            StartDay = startday;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public bool Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }
        public string Team
        {
            get
            {
                return _team;
            }
            set
            {
                _team = value;
            }
        }
        public string TaskMap
        {
            get
            {
                return _taskMap;
            }
            set
            {
                _taskMap = value;             
            }
        }
        public int StartDay
        {
            get
            {
                return _day;
            }
            set
            {
                _day = value;
            }
        }
    }
    //task class - task table - to create tasks at the beginning and update the same at eod 
    class Work
    {
        private string _status;
        private int _sla;
        private int _eA;
        private int _eB;
        private int _eC;
        private int _startDay;
        private int _remaining;
        private bool _isParallel;
        private int _currenttask;
        public Work(int Id, bool isParallel, string level, int sla, int eA, int eB, int eC, int startDay, string status, int currentTask)
        {
            ID = Id;
            IsParallel = isParallel;
            Level = level;
            SLA = sla;
            effortA = eA;
            effortB = eB;
            effortC = eC;
            StartDay = startDay;
            Taskstatus = status;
            //IsNew = true;
            CurrentTask = currentTask;
        }
        public int ID { get; set; }
        public bool IsParallel
        {
            get
            {
                return _isParallel;
            }
            set
            {
                _isParallel = value;
            }
        }
        public int CurrentTask
        {
            get
            {
                return _currenttask;
            }
            set
            {
                _currenttask = value;
            }

        }
        public string Taskstatus
        {
            get
            {
                return _status;

            }
            set
            {
                _status = value;
            }
        }
        public string Level { get; set; }
        public int SLA
        {
            get
            {
                return _sla;
            }
            set
            {
                _sla = value;
            }
        }
        public int effortA
        {
            get
            {
                return _eA;
            }
            set
            {
                _eA = value;
            }
        }
        public int effortB
        {
            get
            {
                return _eB;
            }
            set
            {
                _eB = value;
            }
        }
        public int effortC
        {
            get
            {
                return _eC;
            }
            set
            {
                _eC = value;
            }
        }
        public int StartDay
        {
            get
            {
                return _startDay;
            }
            set
            {
                _startDay = value;
            }
        }
        public int RemainingWork
        {
            get
            {
                return _remaining;
            }
            set
            {
                _remaining = value;
            }
        }
        
    }
    
    public class Program
    {
        static int team1Counter, team2Counter, team3Counter;
        //mapping for analyst and work done. Yet to decide which is key and what is value.
        // May enhance it oto take key as work and value as the list of analysts
        Dictionary<Work, List<Analyst>> Important = new Dictionary<Work, List<Analyst>>();
        Dictionary<Work, int[]> TrackTheTaskDict = new Dictionary<Work, int[]>();
        Dictionary<string, int[][]> AnalystMap = new Dictionary<string, int[][]>();
        int [] AnalystCounter = new int[] { 0,0,0};
        List<Analyst> Analysts = null;
        static string connectionstring = "server=localhost;uid=root;pwd=Infinity!90;database=VulnerabilityDB";

        List<string> FreeAnalystsT1 = new List<string>();
        List<string> FreeAnalystsT2 = new List<string>();
        List<string> FreeAnalystsT3 = new List<string>();

        private MySqlConnection returnConnectionObj()
        {
            MySqlConnection sqlconnection = null;
            try {
                sqlconnection = new MySqlConnection(connectionstring);
                sqlconnection.Open();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                string connstr = "create database VulnerabilityDB";
                sqlconnection = new MySqlConnection("server=localhost;uid=root;pwd=Infinity!90;");
                sqlconnection.Open();
                MySqlCommand cmd = new MySqlCommand(connstr, sqlconnection);
                cmd.ExecuteNonQuery();
                sqlconnection.Close();
                sqlconnection = new MySqlConnection(connectionstring);
            }
            return sqlconnection;
        }
        public Dictionary<string, int[][]> ReturnNothingForNow()
        {
            MySqlConnection sqlconnection = returnConnectionObj();
            //2 global lists for tracking tasks and analysts
            List<Work> Works = new List<Work>();
            Analysts = new List<Analyst>();
            string sqlquery;
            Console.WriteLine("Enter path for output file");
            string outputfilepath = Console.ReadLine();
            StreamWriter sw = new StreamWriter(outputfilepath + @"\OutputFile.txt");
            sw.WriteLine(" ************************************************************************");
            sw.WriteLine("This file contains the daily report by each vulnerability");
            sw.WriteLine("Each row has 5 fields that give information about task such as task ID, Number of analysts from each team assigned to task and their respective IDs");
            sw.WriteLine("--------Format of the data is as follows---------");
            sw.WriteLine("task Id|team1 analysts|team2 analysts|team3 analysts|AnalystIDs");
            sw.WriteLine(" ************************************************************************");

            var csv = new StringBuilder();
            var csv2 = new StringBuilder();
            string outputnew = outputfilepath + @"\OutputFile.csv";
            string outputNumbers = outputfilepath + @"\SummaryStats.csv";
            csv.AppendLine(",Task ID,Analysts Working");
            csv.AppendLine(",,Team1, Team2, Team3");
            csv2.AppendLine("Total number of Analysts by team working on tasks per day");
            csv2.AppendLine("Day #, Team1, Team2, Team3");
            //int workId = 1;
            Console.WriteLine("Enter the number of days you wanna process");
            int daycount = Convert.ToInt32(Console.ReadLine());
            //sqlconnection.Open();
            PrepareTables(sqlconnection);

            //READ FROM EXCEL
            string con =
            @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + outputfilepath + @"\Bigdump_1.xlsx;" +
            @"Extended Properties='Excel 8.0;HDR=Yes;'";
            
            //loop through eaxh day. I hardcoded 6. May extend to any number 
            for (int d = 1; d <= daycount; d++)
            {
                csv.AppendLine("Day " + d);
                string workstartstatus = "New";
                
                using (OleDbConnection connection = new OleDbConnection(con))
                {
                    #region Reading from Excel by page
                    connection.Open();
                    string sheetname = "Day" + d;
                    string query = string.Format(@"Select * From [Bigdump_1$] where startDay = {0}", d);
                    OleDbCommand command = new OleDbCommand(query, connection); //new OleDbCommand("select * from [Sheet1$]", connection);
                    using (OleDbDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (!DBNull.Value.Equals(dr[0]))
                            {
                                //sqlquery = string.Format("Insert into TaskData(TaskID, CriticalityLevel, SLA, EffortA, EffortB, EffortC, StartDay, TaskStatus, IsParallel) values('{0}',\"{1}\", '{2}', '{3}','{4}','{5}','{6}', \"{7}\", \"{8}\")", Convert.ToInt32(dr[0]), dr[1].ToString(), Convert.ToInt32(dr[2]), Convert.ToInt32(dr[3]), Convert.ToInt32(dr[4]), Convert.ToInt32(dr[5]), d, Convert.ToBoolean(string.Compare(dr[6].ToString(), "concurrent", true)) ? "New" : "NewT1", Convert.ToBoolean(string.Compare(dr[6].ToString(), "concurrent", true)) ? true : false);
                                sqlquery = string.Format("Insert into TaskData(TaskID, CriticalityLevel, SLA, EffortA, EffortB, EffortC, StartDay, TaskStatus, IsParallel, CurrentTask) values('{0}',\"{1}\", '{2}', '{3}','{4}','{5}','{6}', \"{7}\", {8}, 0)", Convert.ToInt32(dr[0]), dr[1].ToString(), Convert.ToInt32(dr[2]), Convert.ToInt32(dr[3]), Convert.ToInt32(dr[4]), Convert.ToInt32(dr[5]), d, workstartstatus, Convert.ToBoolean(string.Compare(dr[6].ToString(), "concurrent", true) == 0) ? 1 : 0);

                                MySqlCommand cmd = new MySqlCommand(sqlquery, sqlconnection);
                                cmd.ExecuteNonQuery();
                                sqlquery = string.Format("Insert into TaskAnalystMap(TaskID, Team1, Team2, Team3) values('{0}', 0, 0, 0)", Convert.ToInt32(dr[0]));
                                cmd = new MySqlCommand(sqlquery, sqlconnection);
                                cmd.ExecuteNonQuery();
                                //Work work = new Work(Convert.ToInt32(dr[0]), dr[1].ToString(), Convert.ToInt32(dr[2]), Convert.ToInt32(dr[3]), Convert.ToInt32(dr[4]), Convert.ToInt32(dr[5]), 0, Convert.ToInt32(dr[6]));
                                //Works.Add(work);
                            }
                        }
                    }
                    connection.Close();
                    #endregion Reading from Excel by page
                }

                #region Collect free analysts start of the day
                FreeAnalystsT1 = ReturnAnalystID(sqlconnection, 0, "T1");
                FreeAnalystsT2 = ReturnAnalystID(sqlconnection, 0, "T2");
                FreeAnalystsT3 = ReturnAnalystID(sqlconnection, 0, "T3");
                #endregion Collect free analysts start of the day

                //GH:Loop through each work and assign analysts based on the effort required from each team and SLA 
                #region New Tasks
                List<Work> newTasks = ReturnTaskList(sqlconnection, "New");
                foreach (Work nw in newTasks)
                {                    
                    double effortoft1 = nw.effortA;
                    double effortoft2 = nw.effortB;
                    double effortoft3 = nw.effortC;
                    //double maxEffort = Math.Max(effortoft1, Math.Max(effortoft2, effortoft3));
                    string sqteam = string.Empty;
                    if (nw.IsParallel == true)
                    {
                        if (effortoft1 > 0)
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + "0_T1", true, "T1", nw.ID.ToString(), d);
                        if (effortoft2 > 0)
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + "0_T2", true, "T2", nw.ID.ToString(), d);
                        if (effortoft3 > 0)
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + "0_T3", true, "T3", nw.ID.ToString(), d);
                        
                        double effortDivided;                        
                        double[] ne = new double[3] { 1, 1, 1 };
                        double[] tempeffort = new double[3] { effortoft1, effortoft2, effortoft3 };
                       
                        while (tempeffort[0] > nw.SLA)
                        {
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + Convert.ToInt32(ne[0]) + "_T1", true, "T1", nw.ID.ToString(), d);
                            ne[0] = ne[0] + 1; //2 analysts
                            effortDivided = Convert.ToDouble(effortoft1 / ne[0]);
                            tempeffort[0] = Math.Ceiling(effortDivided);                            
                        }
                        while (tempeffort[1] > nw.SLA)
                        {
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + Convert.ToInt32(ne[1]) + "_T2", true, "T2", nw.ID.ToString(), d);
                            ne[1] = ne[1] + 1; //2 analysts
                            effortDivided = Convert.ToDouble(effortoft2 / ne[1]);
                            tempeffort[1] = Math.Ceiling(effortDivided);
                        }
                        while (tempeffort[2] > nw.SLA)
                        {
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + Convert.ToInt32(ne[2]) + "_T3", true, "T3", nw.ID.ToString(), d);
                            ne[2] = ne[2] + 1; //2 analysts
                            effortDivided = Convert.ToDouble(effortoft3 / ne[2]);
                            tempeffort[2] = Math.Ceiling(effortDivided);
                        }                        
                    }
                    else
                    {
                        if(nw.CurrentTask == 0)
                        {
                            //This should be called only one. when currenttask is not yet assigned, we call this method that runs the algorithm to determine task analyst map based on SLA and total effort required
                            //in case of sequential tasks only
                            SequentialTaskAnalystMap_v2(sqlconnection, nw);
                        }
                        string up_query = "Update taskdata set CurrentTask = ";
                        //set the currentTask
                        if (nw.effortA > 0)
                            nw.CurrentTask = 1;
                        else if (nw.effortB > 0 && nw.effortA <= 0)
                            nw.CurrentTask = 2;
                        else if(nw.effortC > 0 && nw.effortB <= 0 && nw.effortA <= 0)
                            nw.CurrentTask = 3;
                        up_query = up_query + nw.CurrentTask + " where TaskID = '" + nw.ID + "'";
                        MySqlCommand cmd_up = new MySqlCommand(up_query, sqlconnection);
                        cmd_up.ExecuteNonQuery();
                        //Start allocation of analysts 
                        string teamname = string.Empty;
                        string q = string.Empty;
                        if (nw.CurrentTask == 1)
                        {
                            q = "select Team1 from taskanalystmap  where TaskID = '" + nw.ID + "'";
                            teamname = "T1";
                        }
                        else if(nw.CurrentTask == 2)
                        {
                            teamname = "T2";
                            q = "select team2 from taskanalystmap  where TaskID = '" + nw.ID + "'";
                        }
                        else if(nw.CurrentTask == 3)
                        {
                            teamname = "T3";
                            q = "select Team3 from taskanalystmap  where TaskID = '" + nw.ID + "'";
                        }
                        //get the total number of analysts to be created by reading from taskanalystmap
                        int anlstCount = 0;
                        cmd_up = new MySqlCommand(q, sqlconnection);
                        MySqlDataReader mdr_1 = cmd_up.ExecuteReader();
                        while(mdr_1.Read())
                        {
                            anlstCount = Convert.ToInt32(mdr_1.GetValue(0));
                        }
                        mdr_1.Close();

                        for(int i =0; i < anlstCount; i++)
                        {
                            CreateAnalyst(sqlconnection, "An" + "_d" + d + "w_" + nw.ID + i + "_" + teamname, false, teamname, nw.ID.ToString(), d);
                        }                                               
                    }
                }
                #endregion New Tasks

                //Writing the report to a text file at the beginning of processing each task for any day
                sw.WriteLine(string.Format("------------- Start Report of Day {0} - New tasks taken -----------", d));
                sqlquery = string.Format("select tap.* from taskanalystmap tap, taskdata td where tap.taskId = td.TaskId and td.taskstatus = 'New';");
                MySqlCommand cmdl = new MySqlCommand(sqlquery, sqlconnection);
                MySqlDataReader mdr = cmdl.ExecuteReader();
                string columns = null;

                int columnCount = mdr.FieldCount;
                while (mdr.Read())
                {
                    for (int i = 0; i <= columnCount - 1; i++)
                    {
                        columns = columns + mdr[i].ToString() + "|";
                    }
                    sw.WriteLine(columns);
                    columns = string.Empty;
                }
                sw.Write(Environment.NewLine);
                mdr.Close();

                #region Daily report - start
                
                //Query by team and task ID
                sqlquery = string.Format("select tap.taskId, Analysts from taskanalystmap tap, taskdata td where tap.taskId = td.TaskId");
                cmdl = new MySqlCommand(sqlquery, sqlconnection);
                mdr = cmdl.ExecuteReader();
                columns = null;
                columnCount = mdr.FieldCount;
                //GH: to list Analysts under the repsective team in CSV file
                //List<string> ancsv = new List<string>();
                string[] ancsv = null;
                int countT1 = 0, countT2 = 0, countT3 = 0;
                while (mdr.Read())
                {
                    ancsv = new string[3];
                    string taskidval = mdr[0].ToString(); //Task ID
                    string anval = mdr[1].ToString(); // analysts seperated by commas
                    string[] anvals = anval.Split(',');
                    //Boolean t1Exist, t2Exist, t3Exist;
                    foreach (string val in anvals)
                    {
                        if (val.Contains("T1"))
                        {
                            countT1++;
                            //t1Exist = true;
                            if (string.IsNullOrEmpty(ancsv[0]))
                                ancsv[0] = val;
                            else
                                ancsv[0] = ancsv[0] + " | " + val;
                        }
                        else if (val.Contains("T2"))
                        {
                            countT2++;
                            //t2Exist = true;
                            if (string.IsNullOrEmpty(ancsv[1]))
                                ancsv[1] = val;
                            else
                                ancsv[1] = ancsv[1] + " | " + val;
                        }
                        else if (val.Contains("T3"))
                        {
                            countT3++;
                            //t3Exist = true;
                            if (string.IsNullOrEmpty(ancsv[2]))
                                ancsv[2] = val;
                            else
                                ancsv[2] = ancsv[2] + " | " + val;
                        }
                    }
                    if (string.IsNullOrEmpty(ancsv[2]))
                    {
                        if (string.IsNullOrEmpty(ancsv[1]))
                        {
                            if (string.IsNullOrEmpty(ancsv[0]))
                                ancsv[2] = "--Done--";
                            else
                                ancsv[2] = "--Not started--";
                        }
                        else
                            ancsv[2] = "--Not started--";
                    }
                    if (string.IsNullOrEmpty(ancsv[1]))
                    {
                        if (string.IsNullOrEmpty(ancsv[0]))                        
                            ancsv[1] = "--Done--";
                        
                        else
                            ancsv[1] = "--Not started--";
                    }

                    if (string.IsNullOrEmpty(ancsv[0]))
                        ancsv[0] = "--Done--";

                    //for (int i = 0; i < 3; i++)
                    //{
                    //    if (string.IsNullOrEmpty(ancsv[i]))
                    //        ancsv[i] = "--Done--";
                    //}
                    columns = "," + taskidval + "," + ancsv[0] + "," + ancsv[1] + "," + ancsv[2];
                    csv.AppendLine(columns);
                    columns = string.Empty;
                }
                csv2.AppendLine("Day " + d + "," + countT1 + "," + countT2 + "," + countT3);
                csv.AppendLine();
                mdr.Close();

                //Setting taskstatus to in progress at the end of each day
                sqlquery = string.Format("update taskdata set taskstatus = 'InProgress'");
                cmdl = new MySqlCommand(sqlquery, sqlconnection);
                cmdl.ExecuteNonQuery();

                #endregion Daily report - start

                #region In progress Tasks

                //Process the tasks in progress         
                List<Work> oldTasks = ReturnTaskList(sqlconnection, "InProgress");
                foreach (Work ow in oldTasks)
                {
                    List<string> analystIDs = new List<string>();
                    List<string> removedIDs = new List<string>();
                    if (ow.IsParallel == false)
                    {
                        string team = string.Empty;
                        string query1 = string.Empty;
                        string query2 = string.Empty;
                        string query3 = string.Empty;
                        string query_s = string.Empty;
                        if (ow.CurrentTask == 1)
                        {
                            team = "T1";
                            query1 = "select EffortA from TaskData where TaskID = '" + ow.ID + "'";
                            query2 = "select Team1 from TaskAnalystMap where TaskId = '" + ow.ID + "'";
                            query3 = "update taskanalystmap set Team1 = ";
                            query_s = "update TaskData set EffortA = ";
                        }
                        if (ow.CurrentTask == 2)
                        {
                            team = "T2";
                            query1 = "select EffortB from TaskData where TaskID = '" + ow.ID + "'";
                            query2 = "select team2 from TaskAnalystMap where TaskId = '" + ow.ID + "'";
                            query3 = "update taskanalystmap set Team2 = ";
                            query_s = "update TaskData set EffortB = ";
                        }
                        if (ow.CurrentTask == 3)
                        {
                            team = "T3";
                            query1 = "select EffortC from TaskData where TaskID = '" + ow.ID + "'";
                            query2 = "select Team3 from TaskAnalystMap where TaskId = '" + ow.ID + "'";
                            query3 = "update taskanalystmap set Team3 = ";
                            query_s = "update TaskData set EffortC = ";
                        }
                        MySqlCommand cmd_s = new MySqlCommand(query1, sqlconnection);
                        MySqlDataReader mySqlDataReader_s = cmd_s.ExecuteReader();
                        int currenteffort = 0;
                        while (mySqlDataReader_s.Read())
                        {
                            currenteffort = Convert.ToInt32(mySqlDataReader_s.GetValue(0));
                        }
                        mySqlDataReader_s.Close();


                        int t = 0;
                        cmd_s = new MySqlCommand(query2, sqlconnection);
                        mySqlDataReader_s = cmd_s.ExecuteReader();
                        while (mySqlDataReader_s.Read())
                        {
                            t = Convert.ToInt32(mySqlDataReader_s.GetValue(0));
                        }
                        mySqlDataReader_s.Close();
                        int e = 0;
                        if (currenteffort > 0)
                            e = currenteffort - t;

                        //When No.of analysts from a team working on that task is graeter than Currenteffort required
                        if (e < 0)
                            e = 0;

                        //update database current effort value
                        if (e == 0)
                        {
                            string query4 = query3 + e + " where TaskId = '" + ow.ID + "'";
                            cmd_s = new MySqlCommand(query4, sqlconnection);
                            cmd_s.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, team);
                            if (analystIDs != null && analystIDs.Count != 0)
                            {
                                foreach (string anl in analystIDs)
                                {
                                    ReleaseAnalyst(sqlconnection, anl);
                                    removedIDs.Add(anl);
                                }
                            }
                        }

                        //This is the case where effort required is less than number of analysts assigned
                        if (e != 0 && e < t)
                        {
                            string query4 = query3 + e + " where TaskId = '" + ow.ID + "'";
                            cmd_s = new MySqlCommand(query4, sqlconnection);
                            cmd_s.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, team);
                            for (int a = 0; a < t - e; a++)
                            {
                                ReleaseAnalyst(sqlconnection, analystIDs[a]);
                                removedIDs.Add(analystIDs[a]);
                            }
                        }
                        query_s = query_s + e + " where TaskID = '" + ow.ID + "'";
                        cmd_s = new MySqlCommand(query_s, sqlconnection);
                        cmd_s.ExecuteNonQuery();

                        //Call method for updating current task value
                        int[] eff = new int[3];
                        string query_select = "select EffortA, EffortB, EffortC from TaskData where TaskID = '" + ow.ID + "'";
                        MySqlCommand cmd_p = new MySqlCommand(query_select, sqlconnection);
                        MySqlDataReader mySqlDataReader_p = cmd_p.ExecuteReader();
                        int count = mySqlDataReader_p.FieldCount;
                        while (mySqlDataReader_p.Read())
                        {
                            for (int j = 0; j < count; j++)
                            {
                                eff[j] = Convert.ToInt32(mySqlDataReader_p.GetValue(j));
                            }
                        }
                        bool currenttaskchanged = false;
                        mySqlDataReader_p.Close();
                        if (eff[0] == 0 && ow.CurrentTask == 1)
                        {
                            if (eff[1] > 0)
                            {
                                ow.CurrentTask = 2;
                                currenttaskchanged = true;
                            }
                            else if (eff[2] > 0)
                            {
                                ow.CurrentTask = 3;
                                currenttaskchanged = true;
                            }
                        }
                        else if (eff[1] == 0 && ow.CurrentTask == 2)
                        {
                            if (eff[2] > 0)
                            {
                                ow.CurrentTask = 3;
                                currenttaskchanged = true;
                            }
                        }
                        //else if (eff[2] == 0 && ow.CurrentTask == 3)
                        // Delete the task
                        if (currenttaskchanged)
                        {
                            string q = string.Format("update taskdata set taskstatus = 'New' where TaskId = '" + ow.ID + "'");
                            cmd_s = new MySqlCommand(q, sqlconnection);
                            cmd_s.ExecuteNonQuery();

                        }

                    }

                    if (ow.IsParallel == true)
                    {
                        int[] currenteffort = new int[3];
                        string query_p = "select EffortA, EffortB, EffortC from TaskData where TaskID = '" + ow.ID + "'";
                        MySqlCommand cmd_p = new MySqlCommand(query_p, sqlconnection);
                        MySqlDataReader mySqlDataReader_p = cmd_p.ExecuteReader();
                        int count = mySqlDataReader_p.FieldCount;
                        while (mySqlDataReader_p.Read())
                        {
                            for (int j = 0; j < count; j++)
                            {
                                currenteffort[j] = Convert.ToInt32(mySqlDataReader_p.GetValue(j));
                            }
                        }
                        mySqlDataReader_p.Close();
                        int t1 = 0, t2 = 0, t3 = 0;
                        query_p = "select Team1, team2, Team3 from TaskAnalystMap where TaskId = '" + ow.ID + "'";
                        cmd_p = new MySqlCommand(query_p, sqlconnection);
                        mySqlDataReader_p = cmd_p.ExecuteReader();
                        while (mySqlDataReader_p.Read())
                        {
                            t1 = Convert.ToInt32(mySqlDataReader_p.GetValue(0));
                            t2 = Convert.ToInt32(mySqlDataReader_p.GetValue(1));
                            t3 = Convert.ToInt32(mySqlDataReader_p.GetValue(2));
                        }
                        mySqlDataReader_p.Close();
                        int e1 = 0, e2 = 0, e3 = 0;
                        if (currenteffort[0] > 0)
                            e1 = currenteffort[0] - t1;
                        if (currenteffort[1] > 0)
                            e2 = currenteffort[1] - t2;
                        if (currenteffort[2] > 0)
                            e3 = currenteffort[2] - t3;

                        //When No.of analysts from a team working on that task is graeter than Currenteffort required
                        if (e1 < 0)
                            e1 = 0;
                        if (e2 < 0)
                            e2 = 0;
                        if (e3 < 0)
                            e3 = 0;


                        //In case of odd-even, we may have 1 analyst extra. Handle that here
                        //which means no over-allocation of analysts
                        if (e1 == 0)
                        {
                            query_p = "update taskanalystmap set Team1 = " + e1 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T1");
                            if (analystIDs != null && analystIDs.Count != 0)
                            {
                                foreach (string anl in analystIDs)
                                {
                                    ReleaseAnalyst(sqlconnection, anl);
                                    removedIDs.Add(anl);
                                }
                            }
                        }
                        if (e2 == 0)
                        {
                            query_p = "update taskanalystmap set Team2 = " + e2 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T2");
                            if (analystIDs != null && analystIDs.Count != 0)
                            {
                                foreach (string anl in analystIDs)
                                {
                                    ReleaseAnalyst(sqlconnection, anl);
                                    removedIDs.Add(anl);
                                }
                            }
                        }
                        if (e3 == 0)
                        {
                            query_p = "update taskanalystmap set Team3 = " + e3 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T3");
                            if (analystIDs != null && analystIDs.Count != 0)
                            {
                                foreach (string anl in analystIDs)
                                {
                                    ReleaseAnalyst(sqlconnection, anl);
                                    removedIDs.Add(anl);
                                }
                            }
                        }
                        if (e1 != 0 && e1 < t1)
                        {
                            query_p = "update taskanalystmap set Team1 = " + e1 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T1");
                            for (int a = 0; a < t1 - e1; a++)
                            {
                                ReleaseAnalyst(sqlconnection, analystIDs[a]);
                                removedIDs.Add(analystIDs[a]);
                                //a = a + 1; GH:IMP:Commented this on 15th Aug 2019
                            }
                        }
                        analystIDs = new List<string>();
                        if (e2 != 0 && e2 < t2)
                        {
                            query_p = "update taskanalystmap set Team2 = " + e2 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T2");
                            for (int a = 0; a < t2 - e2; a++) //(t2 < e2)
                            {
                                ReleaseAnalyst(sqlconnection, analystIDs[a]);
                                removedIDs.Add(analystIDs[a]);
                                //a = a + 1;
                            }
                        }
                        analystIDs = new List<string>();
                        if (e3 != 0 && e3 < t3)
                        {
                            query_p = "update taskanalystmap set Team3 = " + e3 + " where TaskId = '" + ow.ID + "'";
                            cmd_p = new MySqlCommand(query_p, sqlconnection);
                            cmd_p.ExecuteNonQuery();
                            analystIDs = ReturnAnalystID(sqlconnection, ow.ID, "T3");
                            for (int a = 0; a < t3 - e3; a++)
                            {
                                ReleaseAnalyst(sqlconnection, analystIDs[a]);
                                removedIDs.Add(analystIDs[a]);
                                //a = a + 1;
                            }
                        }

                        query_p = "update TaskData set EffortA = " + e1 + ", EffortB = " + e2 + ", EffortC = " + e3 + " where TaskID = '" + ow.ID + "'";
                        cmd_p = new MySqlCommand(query_p, sqlconnection);
                        cmd_p.ExecuteNonQuery();
                    }
                    if (removedIDs != null && removedIDs.Count != 0)
                    {
                        string query_0 = "select analysts from taskanalystmap where TaskId = '" + ow.ID + "'";
                        MySqlCommand cmd_0 = new MySqlCommand(query_0, sqlconnection);
                        MySqlDataReader mySqlDataReader_0 = cmd_0.ExecuteReader();
                        List<string> analystList = null;
                        while (mySqlDataReader_0.Read())
                        {
                            string anl = mySqlDataReader_0.GetValue(0).ToString();
                            analystList = new List<string>(anl.Split(','));
                        }
                        mySqlDataReader_0.Close();
                        IEnumerable<string> newanalysts = analystList.Except(removedIDs);
                        string newanalyst = string.Empty;
                        foreach (string anst in newanalysts)
                        {
                            if (!string.IsNullOrEmpty(anst))
                                newanalyst = anst + "," + newanalyst;
                        }
                        query_0 = string.Format("update taskanalystmap set Analysts = \"{0}\" where TaskId = '{1}'", newanalyst, ow.ID);
                        cmd_0 = new MySqlCommand(query_0, sqlconnection);
                        cmd_0.ExecuteNonQuery();
                    }
                    //GH: Moved recently. As the task should be delelted from the 2 tables on the same day after finishing all effort contribution
                    int[] ce = new int[3];
                    string query = "select EffortA, EffortB, EffortC from TaskData where TaskID = '" + ow.ID + "'";
                    MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
                    MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
                    int fc = mySqlDataReader.FieldCount;
                    while (mySqlDataReader.Read())
                    {
                        for (int j = 0; j < fc; j++)
                        {
                            ce[j] = Convert.ToInt32(mySqlDataReader.GetValue(j));
                        }
                    }
                    mySqlDataReader.Close();
                    if (ce[0] == 0 && ce[1] == 0 && ce[2] == 0)
                    {
                        query = "delete from TaskData where TaskID = '" + ow.ID + "'";
                        cmd = new MySqlCommand(query, sqlconnection);
                        cmd.ExecuteNonQuery();
                        query = "delete from TaskAnalystMap where TaskID = '" + ow.ID + "'";
                        cmd = new MySqlCommand(query, sqlconnection);
                        cmd.ExecuteNonQuery();
                        //GH: Similar code should be written below
                        //Move this to a seperate method
                        List<string> analystIDs1 = new List<string>();
                        analystIDs1 = ReturnAnalystID(sqlconnection, ow.ID, null);
                        foreach (string analystID in analystIDs1)
                        {
                            UpdateTasksListForAnAnalyst(sqlconnection, analystID, ow.ID.ToString());
                            ReleaseAnalyst(sqlconnection, analystID);
                        }
                    }                    
                }
                #endregion In progress Tasks

                #region DailyReport
                //Writing the report to a text file at the end of the day

                sw.WriteLine(string.Format("------------- End of the day Report - Day {0} -----------", d));
                sqlquery = string.Format("select * from taskanalystmap");
                cmdl = new MySqlCommand(sqlquery, sqlconnection);
                mdr = cmdl.ExecuteReader();
                columns = null;
                columnCount = mdr.FieldCount;
                while (mdr.Read())
                {
                    for (int i = 0; i <= columnCount - 1; i++)
                    {
                        columns = columns + mdr[i].ToString() + "|";
                    }
                    //csv.AppendLine(columns.Replace("|", ","));
                    sw.WriteLine(columns);
                    columns = string.Empty;
                }
                sw.Write(Environment.NewLine);
                mdr.Close();
                #endregion DailyReport
            }

            #region FinalReport
            sw.WriteLine("***************************Report of No.of Analysts**************************** ");
            int count1, count2, count3;

            count1 = ReturnAnalystCountByTeam(sqlconnection, "T1");
            count2 = ReturnAnalystCountByTeam(sqlconnection, "T2");
            count3 = ReturnAnalystCountByTeam(sqlconnection, "T3");

            sw.WriteLine("Total number of analysts in Team 1 = " + team1Counter);
            sw.WriteLine("Total number of analysts in Team 2 = " + team2Counter);
            sw.WriteLine("Total number of analysts in Team 3 = " + team3Counter);

            csv.AppendLine("*********Summary***********");
            csv.AppendLine(",Total number of analysts hired in Team 1 = " + team1Counter);
            csv.AppendLine(",Total number of analysts hired in Team 2 = " + team2Counter);
            csv.AppendLine(",Total number of analysts hired in Team 3 = " + team3Counter);

            File.WriteAllText(outputnew, csv.ToString());
            File.WriteAllText(outputNumbers, csv2.ToString());

            sqlconnection.Close();
            sw.Close();
            return AnalystMap;
            #endregion FinalReport
        }



        private int ReturnAnalystCountByTeam(MySqlConnection sqlconnection, string team)
        {
            int count = 0;
            string sqlquery = string.Format("select count(*) from analystdata where Team = '" + team +"'");
            MySqlCommand msqlcmd = new MySqlCommand(sqlquery, sqlconnection);
            MySqlDataReader msqldr = msqlcmd.ExecuteReader();
            while (msqldr.Read())
            {
                count = Convert.ToInt32(msqldr.GetValue(0));
            }
            msqldr.Close();
            return count;
        }

        private List<string> ReturnAnalystID(MySqlConnection sqlconnection, int TaskID, string TeamName)
        {
            List<string> analystIDs = new List<string>();
            string query = "select AnalystID from analystdata where TaskMap = '" + TaskID + "'";
            if(TeamName != null)
            {
                query = query + " and Team = '" + TeamName + "'";
            }
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
            while (mySqlDataReader.Read())
            {
                analystIDs.Add(mySqlDataReader["AnalystID"].ToString());
            }
            mySqlDataReader.Close();
            return analystIDs;
        }
        private void ReleaseAnalyst(MySqlConnection sqlconnection, string analystID)
        {
            //string query = "update AnalystData set Team = '' where AnalystID = '" + analystID + "'"; 
            //MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            //cmd.ExecuteNonQuery();
            string query = "update AnalystData set TaskMap = 0 where AnalystID = '" + analystID + "'";
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            cmd.ExecuteNonQuery();
        }
        private List<Work> ReturnTaskList(MySqlConnection sqlconnection, string status)
        {
            CheckForDummyTasks(sqlconnection);
            List<Work> Tasklist = new List<Work>();
            string query = string.Format("select * from TaskData where TaskStatus = \"{0}\"", status);
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
            while (mySqlDataReader.Read())
            {
                //public Work(int Id, bool isParallel, string level, int sla, int eA, int eB, int eC, int startDay, string status, int currentTask)
                Work task = new Work(Convert.ToInt32(mySqlDataReader["TaskId"]),
                    Convert.ToBoolean(mySqlDataReader["IsParallel"]),
                    mySqlDataReader["CriticalityLevel"].ToString(),
                    Convert.ToInt32(mySqlDataReader["SLA"]), 
                    Convert.ToInt32(mySqlDataReader["EffortA"]), Convert.ToInt32(mySqlDataReader["EffortB"]), Convert.ToInt32(mySqlDataReader["EffortC"]),
                    Convert.ToInt32(mySqlDataReader["StartDay"]), mySqlDataReader["TaskStatus"].ToString(),
                    Convert.ToInt32(mySqlDataReader["CurrentTask"]));

                Tasklist.Add(task);
            }
            mySqlDataReader.Close();
            return Tasklist;
        }
        private void CheckForDummyTasks(MySqlConnection sqlconnection)
        {
            List<int> dummytaskIDs = new List<int>();
            string query = string.Format("SELECT TaskID FROM TASKDATA WHERE EFFORTA =0 AND EFFORTB= 0 AND EFFORTC = 0");
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
            while (mySqlDataReader.Read())
            {
                dummytaskIDs.Add(Convert.ToInt32(mySqlDataReader["TaskId"]));
            }
            mySqlDataReader.Close();
            foreach(int ti in dummytaskIDs)
            {
                query = string.Format("delete from taskdata where taskID = " + ti);
                MySqlCommand mySqlCommand1 = new MySqlCommand(query, sqlconnection);
                mySqlCommand1.ExecuteNonQuery();
                query = string.Format("delete from taskanalystmap where taskID = " + ti);
                mySqlCommand1 = new MySqlCommand(query, sqlconnection);
                mySqlCommand1.ExecuteNonQuery();
            }
        }
       
        private bool CheckforFreeAnalysts(string team, out string freenanalyst)
        {
            freenanalyst = string.Empty;
            if (string.Compare("T1", team, true) == 0)
            {
                if (FreeAnalystsT1.Count > 0)
                {
                    freenanalyst = FreeAnalystsT1[0];
                    FreeAnalystsT1.Remove(freenanalyst);
                    return true;
                }
            }
            else if (string.Compare("T2", team, true) == 0)
            {
                if (FreeAnalystsT2.Count > 0)
                {
                    freenanalyst = FreeAnalystsT2[0];
                    FreeAnalystsT2.Remove(freenanalyst);
                    return true;
                }
            }
            else if (string.Compare("T3", team, true) == 0)
            {
                if (FreeAnalystsT3.Count > 0)
                {
                    freenanalyst = FreeAnalystsT3[0];
                    FreeAnalystsT3.Remove(freenanalyst);
                    return true;
                }
            }

            return false;

        }
        private void CreateAnalyst(MySqlConnection sqlconnection, string name, bool isparallel, string team, string workMap, int sd)
        {
            string analystName = name;
            string getvaldr = string.Empty;
            string freeAnalystName = string.Empty;

            if (CheckforFreeAnalysts(team, out freeAnalystName) == true)
            {
                string query = string.Format("update AnalystData set TaskMap = " + workMap + " where AnalystID = '" + freeAnalystName + "'");
                MySqlCommand mySqlCommand1 = new MySqlCommand(query, sqlconnection);
                mySqlCommand1.ExecuteNonQuery();
                //query = string.Format("update AnalystData set Team = '" + team + "' where AnalystID = '" + freeAnalystName + "'");
                //mySqlCommand1 = new MySqlCommand(query, sqlconnection);
                //mySqlCommand1.ExecuteNonQuery();
                analystName = freeAnalystName;
            }
            else
            {
                string query = string.Format("insert into AnalystData(AnalystID, team, taskmap, startday) values(\"{0}\",'{1}', \"{2}\", '{3}')", name, team, workMap, sd);
                MySqlCommand mySqlCommand1 = new MySqlCommand(query, sqlconnection);
                mySqlCommand1.ExecuteNonQuery();
                if (string.Compare(team, "T1") == 0)
                    team1Counter = team1Counter + 1;
                else if (string.Compare(team, "T2") == 0)
                    team2Counter = team2Counter + 1;
                else if (string.Compare(team, "T3") == 0)
                    team3Counter = team3Counter + 1;
            }
            string sqlquery = "select * from TaskAnalystMap where TaskID = " + workMap;
            MySqlCommand mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
            MySqlDataReader dr = mySqlCommand.ExecuteReader();
            while(dr.Read())
            { 
                getvaldr = dr["Analysts"].ToString();
                if (getvaldr != null)
                    getvaldr = analystName + ',' + getvaldr;
                else
                    getvaldr = analystName;
            }
            dr.Close();
            if (isparallel)
            {
                string teamname = string.Empty;
                if (string.Compare(team, "T1") == 0)
                    teamname = "Team1";
                else if (string.Compare(team, "T2") == 0)
                    teamname = "Team2";
                else if (string.Compare(team, "T3") == 0)
                    teamname = "Team3";
                //Updating the teamsize for each new analyst created w.r.t the task ID 
                sqlquery = "select " + teamname + " from taskanalystmap where TaskID = '" + workMap + "'";
                mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
                dr = mySqlCommand.ExecuteReader();
                int teamsize = 0;
                while (dr.Read())
                {
                    teamsize = Convert.ToInt32(dr[teamname]);
                }
                dr.Close();
                teamsize = teamsize + 1;

                sqlquery = string.Format("Update taskanalystmap set " + teamname + " = " + teamsize + " where TaskID = '" + workMap + "'");
                mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
                mySqlCommand.ExecuteNonQuery();
            }
            //Updating the Analyst list for each new analyst created w.r.t the task ID 
            string lastchar = getvaldr.Trim(); ;
            if (lastchar[lastchar.Length - 1] == ',')
            {
                lastchar = lastchar.Substring(0, lastchar.Length - 1);
            }            
            sqlquery = string.Format("Update taskanalystmap set Analysts = '" + lastchar + "' where TaskID = '" + workMap + "'");
            mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
            mySqlCommand.ExecuteNonQuery();
        }
        private void SequentialTaskAnalystMap_v2(MySqlConnection sqlconnection, Work task)
        {
            int totaleffort = task.effortA + task.effortB + task.effortC;

            int[] people = new int[3] { (task.effortA > 0) ? 1 :0, (task.effortB > 0) ? 1 : 0, (task.effortC > 0) ? 1 : 0 };
            int finishline = task.SLA;
            if (totaleffort > finishline)
            {
                switch(finishline)
                {
                    case 3: //priority - critical
                        people = new int[3] { task.effortA, task.effortB, task.effortC };
                        break;
                    default:
                        people = SequentialTaskAnalystMapAllocation(finishline, new int[] { task.effortA, task.effortB, task.effortC }, totaleffort);
                        break;
                }
            }
            string sqlquery = string.Format("Update taskanalystmap set Team1 = " + people[0] + ", Team2 = " + people[1] + ", Team3 = " + people[2] + " where TaskID = '" + task.ID + "'");
            MySqlCommand mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
            mySqlCommand.ExecuteNonQuery();
        }

        private int[] SequentialTaskAnalystMapAllocation(int sla, int[] effort, int total, bool repeat = false)
        {
            int neweffort = 0;
            int[] people = new int[3];
            int[] peepdays = new int[3];
            for(int i =0; i <3; i++)
            {
                if (effort[i] == 0)
                {
                    people[i] = 0;
                }
                else
                {
                    double val = (Convert.ToDouble(effort[i]) / total) * sla;
                    if (repeat == false)
                    {
                        people[i] = Convert.ToInt32(Math.Floor(val));
                    }
                    else
                    {
                        people[i] = Convert.ToInt32(Math.Ceiling(val));
                    }
                    if (people[i] == 0)  // to address thecase where math.floor applied on a decimal < 1  
                        people[i] = 1;
                    if (people[i] > effort[i]) // address over-allocation. Max number of people should not exceed man days required
                        people[i] = effort[i];
                    double peopledaysval = Convert.ToDouble(effort[i]) / (people[i]); //avoid divide by zero
                    peepdays[i] = Convert.ToInt32(Math.Ceiling(peopledaysval));
                    neweffort = neweffort + peepdays[i];
                }
            }
            if(neweffort > sla)
            {
                SequentialTaskAnalystMapAllocation(sla, effort, total, true);
            }
            if (repeat)
            {
                //add one more analyst for the team that requires max effort
                int maxIndex = effort.ToList().IndexOf(effort.Max());
                people[maxIndex] = people[maxIndex] + 1;
                if (people[maxIndex] > effort[maxIndex]) // address over-allocation. Max number of people should not exceed man days required
                    people[maxIndex] = effort[maxIndex];
            }
            return people;
        }

        private void SequentialTaskAnalystMap(MySqlConnection sqlconnection, int taskID)
        {
            string query = "select EffortA, EffortB, EffortC, SLA from TaskData where TaskID = '" + taskID + "'";
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
            int[] ce = new int[4];
            int fc = mySqlDataReader.FieldCount;
            while (mySqlDataReader.Read())
            {
                for (int j = 0; j < fc; j++)
                {
                    ce[j] = Convert.ToInt32(mySqlDataReader.GetValue(j));
                }
            }
            mySqlDataReader.Close();

            int[] people = new int[3] { 1, 1, 1 };
            int totaleffort = 0;
            for(int i=0; i<3; i++)
            {
                totaleffort = totaleffort + ce[i];
            }
            if (totaleffort > ce[3])
            {
                double[] peopledays = new double[3];
                int neweffort = 0;
                for(int i = 0; i<3; i++)
                {
                    if (ce[i] == 0)
                    {
                        people[i] = 0;
                    }
                    else
                    {
                        double val = (ce[i] / totaleffort) * ce[3];
                        people[i] = Convert.ToInt32(Math.Floor(val));
                        if (people[i] == 0)
                            people[i] = 1;
                        double peopledaysval = ce[i] / (people[i]); //avoid divide by zero
                        peopledays[i] = Math.Ceiling(peopledaysval);
                        neweffort = neweffort + Convert.ToInt32(peopledays[i]);
                    }
                }
                //deadline still less than new effort
                if(ce[3] < neweffort)
                {
                    neweffort = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        if (ce[i] == 0)
                        {
                            people[i] = 0;
                        }
                        else
                        {
                            double val = (ce[i] / totaleffort) * ce[3];
                            people[i] = Convert.ToInt32(Math.Ceiling(val)); // This is the difference between 2 for loops
                            if (people[i] == 0) // This is to avoid such case where math.floor on 0.88 gives 0.
                                people[i] = 1;
                            double peopledaysval = ce[i] / (people[i]);  //avoid divide by zero
                            peopledays[i] = Math.Ceiling(peopledaysval);
                            neweffort = neweffort + Convert.ToInt32(peopledays[i]);
                        }
                    }
                    //add one more analyst for the team that requires max effort
                    int maxIndex = ce.ToList().IndexOf(ce.Max());
                    people[maxIndex] = people[maxIndex] + 1;
                }
            }
            string sqlquery = string.Format("Update taskanalystmap set Team1 = " + people[0] + ", Team2 = " + people[1] + ", Team3 = " + people[2] + " where TaskID = '" + taskID + "'");
            MySqlCommand mySqlCommand = new MySqlCommand(sqlquery, sqlconnection);
            mySqlCommand.ExecuteNonQuery();
        }
        private void UpdateTasksListForAnAnalyst(MySqlConnection sqlconnection, string analystID, string taskID)
        {
            string query = "select TasksDone from AnalystData where AnalystID = '" + analystID + "'";
            MySqlCommand cmd = new MySqlCommand(query, sqlconnection);
            MySqlDataReader mySqlDataReader = cmd.ExecuteReader();
            string taskList = taskID;
            while (mySqlDataReader.Read())
            {
                taskList = taskList + "," + mySqlDataReader.GetValue(0).ToString();
            }
            mySqlDataReader.Close();

            //GH:WATCH
            //Trim last comma
            string lastchar = taskList.Trim(); ;
            if(lastchar[lastchar.Length-1] == ',')
            {
                lastchar = lastchar.Substring(0, lastchar.Length - 1);
            }
            query = string.Format("update AnalystData set TasksDone = \"{0}\" where AnalystID = \"{1}\"", lastchar, analystID);
            cmd = new MySqlCommand(query, sqlconnection);
            cmd.ExecuteNonQuery();
        }
        public Analyst CreateAnalyst(string name, bool status, string team, string workMap, int sd, out bool isNew)
        {
            foreach (Analyst analyst in Analysts)
            {
                if (string.IsNullOrEmpty(analyst.TaskMap))
                {
                    analyst.TaskMap = workMap;
                    isNew = false;
                    return analyst;
                }
            }
            Analyst ana = new Analyst(name, true, team, workMap, sd);
            Analysts.Add(ana);
            isNew = true;
            return ana;
        }
        private void printreport(MySqlConnection sqlconnection, int n)
        {
            StreamWriter sw = new StreamWriter(@"C:\Projects\gary\VulnerabilityPrototype\ghoutput.txt");
            for (int i = 0; i < n; i++)
            {
                
                sw.WriteLine(string.Format("------------- Report of Day {0} -----------", i));
                string sqlquery = string.Format("select * from taskanalystmap");
                MySqlCommand cmdl = new MySqlCommand(sqlquery, sqlconnection);
                MySqlDataReader mdr = cmdl.ExecuteReader();
                string columns = null;
                int columnCount = mdr.FieldCount;
                while (mdr.Read())
                {
                    for (int j = 0; j <= columnCount - 1; j++)
                    {
                        columns = columns + mdr[j].ToString() + "|";
                    }
                    sw.WriteLine(columns);
                    columns = string.Empty;
                }
                sw.Write(Environment.NewLine);
                mdr.Close();
            }
            sw.Close();
        }
        private void PrepareTables(MySqlConnection sqlconnection)
        {
            string sqlquery = string.Format("drop table TaskData");
            MySqlCommand cmd = new MySqlCommand(sqlquery, sqlconnection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch(Exception)
            {//do nothing
            }
            sqlquery = string.Format("drop table TaskAnalystMap");
            cmd = new MySqlCommand(sqlquery, sqlconnection);
            try {
            cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {//do nothing
            }
            sqlquery = string.Format("drop table analystdata");
            cmd = new MySqlCommand(sqlquery, sqlconnection);
            try {
            cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {//do nothing
            }
            sqlquery = string.Format("CREATE TABLE IF NOT EXISTS TaskData(TaskId INT(20), IsParallel Boolean, CriticalityLevel VARCHAR(20), SLA INT(20), EffortA INT(20), EffortB INT(20), EffortC INT(20), StartDay INT(20), TaskStatus VARCHAR(250), CurrentTask INT(5))");
            cmd = new MySqlCommand(sqlquery, sqlconnection);
            cmd.ExecuteNonQuery();
            sqlquery = string.Format("CREATE TABLE IF NOT EXISTS TaskAnalystMap(TaskId INT(20), Team1 INT(20), Team2 INT(20), Team3 INT(20), Analysts LONGTEXT)");
            cmd = new MySqlCommand(sqlquery, sqlconnection);
            cmd.ExecuteNonQuery();
            sqlquery = string.Format("CREATE TABLE IF NOT EXISTS AnalystData(AnalystId VARCHAR(20), Team VARCHAR(50), TaskMap INT(20), StartDay INT(20), TasksDone LONGTEXT)");
            cmd = new MySqlCommand(sqlquery, sqlconnection);
            cmd.ExecuteNonQuery();
        }
        static void Main(string[] args)
        {
            //entry 
            Program pg = new Program();
            team1Counter = 0;
            team2Counter = 0;
            team3Counter = 0;
            //pg.printreport(1);
            pg.ReturnNothingForNow();
            
            Console.ReadLine();
        }
    }
}
