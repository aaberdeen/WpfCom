using System;
using System.Collections.Generic;
using System.Text;
//using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Data;
//Add MySql Library
using MySql.Data.MySqlClient;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WpfApplication1;


namespace ComPort
{
    public class DBConnect
    {
        private errorLog _errorLog = new errorLog();
        private MySqlConnection _connection;
        private string _server;
        private string _port;
        private string _database;
        private string _uid;
        private string _password;

        

        //Constructor
        public DBConnect(string server,string port, string database, string uid, string password)
        {
            Initialize(server,port, database, uid, password);
        }

        //Initialize values
        private void Initialize(string server,string port, string database, string uid, string password)
        {
            //server = "localhost";
            _server = server; // "10.1.0.16";
            _database = database; // "wpandb";
            _port = port;
            _uid = uid; // "root";
            _password = password; // null;
            string connectionString;
            connectionString = "SERVER=" + _server + ";" + "Port=" + _port +";" + "DATABASE=" + _database + ";" + "UID=" + _uid + ";" + "PASSWORD=" + _password + ";";

            _connection = new MySqlConnection(connectionString);
        }

        public void createDB()
        {
          
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                _connection.Open();
                return true;
            }
            
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        MessageBox.Show("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
            catch(Exception e)
            {

                _errorLog.write(e, "DBConnect, OpenConnection");
                return false;
            }

        }

 
        //Close connection
        private bool CloseConnection()
        {
            try
            {
                _connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                _errorLog.write(ex, "DBConnect CloseConnection");
                return false;
            }
        }

        //Insert statement
        public void Insert()
        {
            string query = "INSERT INTO tableinfo (name, age) VALUES('John Smith', '33')";

            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                
                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
            }
        }

        //Update statement
        public void Update()
        {
            string query = "UPDATE tableinfo SET name='Joe', age='22' WHERE name='John Smith'";

            //Open connection
            if (this.OpenConnection() == true)
            {
                //create mysql command
                MySqlCommand cmd = new MySqlCommand();
                //Assign the query using CommandText
                cmd.CommandText = query;
                //Assign the connection using Connection
                cmd.Connection = _connection;

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
            }
        }

        //Delete statement
        public void Delete()
        {
            string query = "DELETE FROM tableinfo WHERE name='John Smith'";

            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }

        //Select statement
        public List<string>[] Select()
        {
            
            string query = "SELECT * FROM locationdb";

            //Create a list to store the result
            List<string>[] list = new List<string>[3];
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();

            //Open connection
            if (this.OpenConnection() == true)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();
                
                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    //list[0].Add(dataReader["id"] + "");
                    //list[1].Add(dataReader["name"] + "");
                    //list[2].Add(dataReader["age"] + "");
                    Debug.WriteLine("TAGid " + dataReader["TagID"] + "\tPktLqi: " + dataReader["PktLqi"]);


                }

            

                //close Data Reader
                dataReader.Close();

                //close Connection
                this.CloseConnection();

                //return list to be displayed
                return list;
            }
            else
            {
                return list;
            }
        }

        public DataTable searchDB(string sql)
        {
            DataTable table = new DataTable();

            if (this.OpenConnection() == true)
            {
                // Creates a SQL command
                using (var command = new MySqlCommand(sql, _connection))
                {
                    // Loads the query results into the table
                    table.Load(command.ExecuteReader());
                    this.CloseConnection();
                    return table;
                }

            }
            else
            {
                this.CloseConnection();
                return null;
            }




        }


        public void trackingDBaseUpDate(Tag WorkingTag) //(string TagAdd, string ReaderAdd)
        {
                
                DataTable table = new DataTable(); // to store results
                // System.Data.SqlClient.SqlCommandBuilder trackingCB;                     //needed to add new records on clossed DB 
                // trackingCB = new System.Data.SqlClient.SqlCommandBuilder(trackingDA);           //
                

                //Open connection
                if (this.OpenConnection() == true)
                {
                    //Remove all TOFdistance entrys for this tag
                    //------------------------------------------------------------------------------------------------------------
                    string sql = string.Format("UPDATE LocationDB SET TOFdistance = 0 WHERE TagAdd like '{0}%'", WorkingTag.TagAdd); // TagAdd);

                    //Create Command
                    MySqlCommand command = new MySqlCommand(sql, _connection);
                    //Create a data reader and Execute the command

                    //command.ExecuteNonQuery();  //Will change to this after testing

                    //----------------------------------------------------------------------------------------------------------------
                    //check if tag reader pair are in DB
                    //------------------------------------------------------------------------------------------------------------------

                    sql = string.Format("SELECT * FROM LocationDB WHERE TagAdd like '{0}%' and ReaderAdd like '{1}%' LIMIT 1", WorkingTag.TagAdd, WorkingTag.ReaderAdd); //TagAdd,ReaderAdd);

                    //sql = string.Format("SELECT * FROM LocationDb");
                    command = new MySqlCommand(sql, _connection);
                    try
                    {
                        MySqlDataReader dataReader = command.ExecuteReader();
                        // table.Load(command.ExecuteReader(), LoadOption.OverwriteChanges);
                        table.Load(dataReader, LoadOption.OverwriteChanges);


                        if (table.Rows.Count > 0)
                        {// We have one in the table so needs updating
                            // trackingCon.Open();
                            string sqlChange = string.Format("UPDATE LocationDB SET PktLqi = '{0}', TOFdistance ='{1}', TOFmac= '{2}', TimeStamp = (@value), TOF_MAC_LQI_LIFETIME = '{3}', RxLQI = '{4}', sequence = '{7}', CH4 = '{8}', CO = '{9}', O2 = '{10}', CO2 = '{11}', Name = '{12}', endPointType ='{13}' WHERE TagAdd like '{5}%' and ReaderAdd like '{6}%'  ", WorkingTag.PktLqi, WorkingTag.TOFdistance, WorkingTag.TOFmac, 6, WorkingTag.RxLQI, WorkingTag.TagAdd, WorkingTag.ReaderAdd, WorkingTag.PktSequence, WorkingTag.CH4gas, WorkingTag.COgas, WorkingTag.O2gas, WorkingTag.CO2gas, WorkingTag.Name, WorkingTag.endPointType);

                            // Creates a SQL command
                            command = new MySqlCommand(sqlChange, _connection);
                            command.Parameters.AddWithValue("@value", DateTime.Now);
                            command.ExecuteNonQuery();


                        }
                        else
                        {// Not in the table so needs adding 
                            trackingDataBaseAddNew(WorkingTag);           //add new data to tracking DB
                        }

                        this.CloseConnection();
                    }
                    catch
                    { }


                }
                else // datbase connection not open
                {
                    MessageBox.Show("Error , no database connection!"); 
                }

             }

        private void trackingDataBaseAddNew(Tag WorkingTag)
        {
            
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "INSERT INTO LocationDB (TagAdd, ReaderAdd, PktLqi, TOFdistance, TOFmac, TimeStamp, TOF_MAC_LQI_LIFETIME, RxLQI,sequence , CH4, CO, O2, CO2, Name, endPointType) VALUES(@TagAdd,@ReaderAdd,@PktLqi,@TOFdistance,@TOFmac,@TimeStamp,@TOF_MAC_LQI_LIFETIME,@RxLQI,@sequence,@CH4,@CO,@O2,@CO2,@Name,@endPointType)";
            cmd.Parameters.AddWithValue("TagAdd", WorkingTag.TagAdd);
            cmd.Parameters.AddWithValue("ReaderAdd", WorkingTag.ReaderAdd);
            cmd.Parameters.AddWithValue("PktLqi", WorkingTag.PktLqi);
            cmd.Parameters.AddWithValue("TOFdistance", WorkingTag.TOFdistance);
            cmd.Parameters.AddWithValue("TOFmac", WorkingTag.TOFmac);
            cmd.Parameters.AddWithValue("TimeStamp", DateTime.Now);
            cmd.Parameters.AddWithValue("TOF_MAC_LQI_LIFETIME", 6);
            cmd.Parameters.AddWithValue("RxLQI", WorkingTag.RxLQI);
            cmd.Parameters.AddWithValue("sequence", WorkingTag.PktSequence);
            cmd.Parameters.AddWithValue("CH4", WorkingTag.CH4gas);
            cmd.Parameters.AddWithValue("CO", WorkingTag.COgas);
            cmd.Parameters.AddWithValue("O2", WorkingTag.O2gas);
            cmd.Parameters.AddWithValue("CO2", WorkingTag.CO2gas);
            cmd.Parameters.AddWithValue("Name", WorkingTag.Name);
            cmd.Parameters.AddWithValue("endPointType", WorkingTag.endPointType);
            cmd.Connection = _connection;
            cmd.ExecuteNonQuery();

            
        }

        public void historyDataBaseAddNew(Tag WorkingTag)
        {
            //Open connection
            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = @"INSERT INTO Historydb ( PktLength, ReaderAdd, TagAdd, Volt, PktSequence, PktEvent, PktLqi, TOFping, TOFtimeout, TOFrefuse, TOFsuccess, TOFdistance, RSSIdistance, TOFerror, TOFmac, PktTemp, BrSequ, BrCmd) 
                                                    VALUES(@PktLength,@ReaderAdd,@TagAdd,@Volt,@PktSequence,@PktEvent,@PktLqi,@TOFping,@TOFtimeout,@TOFrefuse,@TOFsuccess,@TOFdistance,@RSSIdistance,@TOFerror,@TOFmac,@PktTemp,@BrSequ,@BrCmd)";

                cmd.Parameters.AddWithValue("PktLength", WorkingTag.PktLength);
                cmd.Parameters.AddWithValue("ReaderAdd", WorkingTag.ReaderAdd);
                cmd.Parameters.AddWithValue("TagAdd", WorkingTag.TagAdd);
                cmd.Parameters.AddWithValue("Volt", WorkingTag.Volt);
                cmd.Parameters.AddWithValue("PktSequence", WorkingTag.PktSequence);
                cmd.Parameters.AddWithValue("PktEvent", WorkingTag.PktEvent);
                cmd.Parameters.AddWithValue("PktLqi", WorkingTag.PktLqi);
                cmd.Parameters.AddWithValue("TOFping", WorkingTag.TOFping);
                cmd.Parameters.AddWithValue("TOFtimeout", WorkingTag.TOFtimeout);
                cmd.Parameters.AddWithValue("TOFrefuse", WorkingTag.TOFrefuse);
                cmd.Parameters.AddWithValue("TOFsuccess", WorkingTag.TOFsuccess);
                cmd.Parameters.AddWithValue("TOFdistance", WorkingTag.TOFdistance);
                cmd.Parameters.AddWithValue("RSSIdistance", WorkingTag.RSSIdistance);
                cmd.Parameters.AddWithValue("TOFerror", WorkingTag.TOFerror);
                cmd.Parameters.AddWithValue("TOFmac", WorkingTag.TOFmac);
                cmd.Parameters.AddWithValue("PktTemp", WorkingTag.PktTemp);
                cmd.Parameters.AddWithValue("BrSequ", WorkingTag.BrSequ);
                cmd.Parameters.AddWithValue("BrCmd", WorkingTag.BrCmd);
                cmd.Connection = _connection;
                cmd.ExecuteNonQuery();

                this.CloseConnection();

            }
   

        }

        public void LQIdecay()
        {
            if (this.OpenConnection() == true)
            {
                string sql = string.Format("UPDATE LocationDB SET PktLqi = PktLqi/(POWER(0.75,(TOF_MAC_LQI_LIFETIME - 6))), TOF_MAC_LQI_LIFETIME = (TOF_MAC_LQI_LIFETIME -1)  WHERE PktLqi > 0 and TOF_MAC_LQI_LIFETIME > 0 ");   //TagAdd, ReaderAdd, PktLqi, TimeStamp, TOF_MAC_LQI_LIFETIME

                //Create Command
                MySqlCommand command = new MySqlCommand(sql, _connection);
                command.ExecuteNonQuery();

                this.CloseConnection();

            }
            this.CloseConnection();                
            
        }




        //Count statement
        public int Count()
        {
            string query = "SELECT Count(*) FROM tableinfo";
            int Count = -1;

            //Open Connection
            if (this.OpenConnection() == true)
            {
                //Create Mysql Command
                MySqlCommand cmd = new MySqlCommand(query, _connection);

                //ExecuteScalar will return one value
                Count = int.Parse(cmd.ExecuteScalar()+"");
                
                //close Connection
                this.CloseConnection();

                return Count;
            }
            else
            {
                return Count;
            }
        }



        public void exicuteScript(string fileName)
        {
            MySqlScript script = new MySqlScript(this._connection, File.ReadAllText(fileName));
            script.Delimiter = "$$";
            try
            {
                script.Execute();
                MessageBox.Show("DB Created");
            }
            catch
            {
                MessageBox.Show("Error, Server connection");
            }
        }


 

        public class TestBind
        {public string Name;
            public TestBind(string stringIN)
            {
            Name = stringIN;
               }
        }

        public class TagBind : INotifyPropertyChanged
        {
            //public TagBind():this("")
            //{
            //}
            public event PropertyChangedEventHandler PropertyChanged;
           
            
            
            
            public TagBind(string TagAddIn, int TtlIn, string tagData1) 
            {
               
                
                _name = TagAddIn;
                _TTL = TtlIn;
                TestBind aa = new TestBind(tagData1);
                tagData.Add(aa);
                
                
                
               
            }

            string _name = "";
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
            private int _TTL;
            public int TTL
            {
                                 get { return _TTL; }
                                set
                                {
                                    _TTL = value;
                                    this.NotifyPropertyChanged("TTL");}
                                }          

            private ObservableCollection<TestBind> _tagData = null;
            public ObservableCollection<TestBind> tagData    //test for tree view
            {

                get
                {

                    if (_tagData == null) _tagData = new ObservableCollection<TestBind>();
                    return _tagData;
                }
                set { _tagData = value; }
            }

                    private void NotifyPropertyChanged(string name)
        {


            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));


        }

        }

    }
}
