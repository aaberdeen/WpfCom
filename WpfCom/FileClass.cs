using System;
using System.Xml;

namespace WpfApplication1
{
    public class FileClass
    {
        public static void LoadMapXml(string filename, ref ComSetup comSetup1, ref MinersNamesForm minersNamesForm)
        {
            comSetup1.coordIpList.Clear();

            XmlDocument map = new XmlDocument();
            map.Load(filename);

            minersNamesForm.minerNames.Clear();
            // listToReturn.Clear();

            foreach (XmlNode node in map.DocumentElement.SelectSingleNode("/root"))
            {

                foreach (XmlNode inner in node)
                {

                    if (inner.Name == "map")
                    {
                    }



                    if (inner.Name == "coordIp")
                    {
                        string IP = "";
                        string localIP = "";
                        string port = "";
                        string udpport = "";
                        int index = 0;

                        foreach (XmlNode innerInner in inner)
                        {
                            if (innerInner.Name == "Index")
                            {
                                index = Convert.ToInt16(innerInner.InnerText);
                            }
                            if (innerInner.Name == "IP")
                            {
                                IP = innerInner.InnerText;
                            }
                            if (innerInner.Name == "localIP")
                            {
                                localIP = innerInner.InnerText;
                            }
                            if (innerInner.Name == "port")
                            {
                                port = innerInner.InnerText;
                            }
                            if (innerInner.Name == "udpport")
                            {
                                udpport = innerInner.InnerText;
                            }
                        }

                        if (IP != "")
                        {
                            comSetup1.coordIpList.Add(new coodData(IP, port, false, index, localIP, udpport));
                        }

                    }



                    if (inner.Name == "database")
                    {
                        foreach (XmlNode innerInner in inner)
                        {
                            if (innerInner.Name == "ip")
                            {
                                Properties.Settings.Default.Server = innerInner.InnerText;
                            }
                            if (innerInner.Name == "port")
                            {
                                Properties.Settings.Default.Port = innerInner.InnerText;
                            }

                            if (innerInner.Name == "name")
                            {
                                Properties.Settings.Default.Database = innerInner.InnerText;
                            }
                            if (innerInner.Name == "user")
                            {
                                Properties.Settings.Default.UID = innerInner.InnerText;
                            }
                            if (innerInner.Name == "password")
                            {

                                if (innerInner.InnerText == "")
                                {
                                    Properties.Settings.Default.Password = null;
                                }
                                else
                                {
                                    Properties.Settings.Default.Password = innerInner.InnerText;
                                }

                            }

                            Properties.Settings.Default.Save();
                        }
                    }

                    //load routers
                    if (inner.Name == "router")
                    {
                    }

                    //miner names
                    if (inner.Name == "minerName")
                    {
                        string minerMAC = null;
                        string minerName = null;
                        string endPointType = null;


                        foreach (XmlNode innerInner in inner)
                        {


                            if (innerInner.Name == "MAC")
                            {
                                minerMAC = innerInner.InnerText;
                            }
                            if (innerInner.Name == "miner")
                            {
                                minerName = innerInner.InnerText;
                            }
                            if (innerInner.Name == "endPointType")
                            {
                                // endPointType = innerInner.InnerText; 

                                endPointType = innerInner.InnerText;
                            }

                        }

                        if (minerMAC != null)
                        {
                            minersNamesForm.minerNames.Add(new NamesBind(minerMAC, minerName, endPointType));
                        }
                    }

                }




            }

        }

        public static void saveMapXml(string fileName, ref ComSetup comSetup1, ref MinersNamesForm minersNamesForm)
        {
            // Create the XmlDocument.
            XmlDocument doc = new XmlDocument();
            //  doc.LoadXml("<item><name>router</name></item>");
            doc.LoadXml("<root></root>");


            //data base save
            XmlElement dataBaseChild = doc.CreateElement("child");
            doc.DocumentElement.AppendChild(dataBaseChild);

            XmlElement newDatabase = doc.CreateElement("database");
            dataBaseChild.AppendChild(newDatabase);
            XmlElement data;

            data = doc.CreateElement("ip");
            data.InnerText = Properties.Settings.Default.Server;
            newDatabase.AppendChild(data);

            data = doc.CreateElement("port");
            data.InnerText = Properties.Settings.Default.Port;
            newDatabase.AppendChild(data);

            data = doc.CreateElement("name");
            data.InnerText = Properties.Settings.Default.Database;
            newDatabase.AppendChild(data);

            data = doc.CreateElement("user");
            data.InnerText = Properties.Settings.Default.UID;
            newDatabase.AppendChild(data);

            data = doc.CreateElement("password");
            data.InnerText = Properties.Settings.Default.Password;
            newDatabase.AppendChild(data);

            foreach (coodData connection in comSetup1.coordIpList)
            {
                XmlElement newChild = doc.CreateElement("child");
                doc.DocumentElement.AppendChild(newChild);

                XmlElement newCoordIP = doc.CreateElement("coordIp");
                newChild.AppendChild(newCoordIP);


                // Add a Index.
                data = doc.CreateElement("Index");
                data.InnerText = connection.Index.ToString();
                newCoordIP.AppendChild(data);

                // Add a IP.
                data = doc.CreateElement("IP");
                data.InnerText = connection.IP;
                newCoordIP.AppendChild(data);

                // Add a localIP.
                data = doc.CreateElement("localIP");
                data.InnerText = connection.localIP;
                newCoordIP.AppendChild(data);

                // Add a port.
                data = doc.CreateElement("port");
                data.InnerText = connection.port;
                newCoordIP.AppendChild(data);
                
                // Add a udpport.
                data = doc.CreateElement("udpport");
                data.InnerText = connection.udpPort;
                newCoordIP.AppendChild(data);

            }

            foreach (NamesBind name in minersNamesForm.minerNames)
            {
                XmlElement newChild = doc.CreateElement("child");
                doc.DocumentElement.AppendChild(newChild);

                XmlElement newMiner = doc.CreateElement("minerName");
                newChild.AppendChild(newMiner);

                // Add a MAC address.
                data = doc.CreateElement("MAC");
                data.InnerText = name.MAC;
                newMiner.AppendChild(data);

                // Add Name
                data = doc.CreateElement("miner");
                data.InnerText = name.minerName;
                newMiner.AppendChild(data);

                // Add endPointType
                data = doc.CreateElement("endPointType");
                //data.InnerText = name.endPointType;
                data.InnerText = name.endPointType.ToString();
                newMiner.AppendChild(data);
            }




            if (fileName != null)
            {
                // Save the document to a file and auto-indent the output.
                XmlTextWriter writer = new XmlTextWriter(fileName, null);
                writer.Formatting = Formatting.Indented;
                doc.Save(writer);
                writer.Close();
            }
            

        }
    }
}
