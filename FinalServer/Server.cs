﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Collections;
using System.Xml;

namespace FinalServer
{
    public class Server
    {
        IPEndPoint endPoint;
        Socket localSocket;
        ArrayList sockets;
        FIB fib;
        public ManualResetEvent allDone = new ManualResetEvent(false);
        String xmlFileName = "log.xml";
        XmlDocument xmlDoc;
        XmlNode rootNode;

        public Server(string ip, int port)
        {
            fib = new FIB();
            xmlDoc = new XmlDocument();
            rootNode = xmlDoc.CreateElement("cloud-log");
            xmlDoc.AppendChild(rootNode);
            sockets = new ArrayList();
            endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            localSocket.Bind(endPoint);
            Thread t = new Thread(Run);
            t.Start();
        }

        void addLog(String t, String f_ip, String t_ip, String d)
        {
            XmlNode userNode = xmlDoc.CreateElement("event");
            XmlAttribute type = xmlDoc.CreateAttribute("type");
            XmlAttribute from = xmlDoc.CreateAttribute("from");
            XmlAttribute to = xmlDoc.CreateAttribute("to");
            type.Value = t;
            from.Value = f_ip;
            to.Value = t_ip;
            userNode.Attributes.Append(type);
            userNode.Attributes.Append(from);
            userNode.Attributes.Append(to);
            userNode.InnerText = d;
            rootNode.AppendChild(userNode);
            xmlDoc.Save(xmlFileName);
        }

        public XmlDocument Doc
        {
            get { return xmlDoc; }
        }

        void Run()
        {
            try
            {
                localSocket.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    localSocket.BeginAccept(new AsyncCallback(AcceptCallback), localSocket);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            sockets.Add(handler);
            Console.WriteLine("Socket [{0}] {1} - {2} was added to sockets list", sockets.Count, handler.LocalEndPoint.ToString(), handler.RemoteEndPoint.ToString());

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read '{0}'[{1} bytes] from socket {2}.",
                       content, content.Length, IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()));
                    addLog("Receive", handler.LocalEndPoint.ToString(), handler.RemoteEndPoint.ToString(), content);
                    // Echo the data back to the client.
                    Socket s = findTarget((IPEndPoint)handler.RemoteEndPoint);
                    StateObject newState = new StateObject();
                    newState.workSocket = handler;

                    Send(s, content);
                    
                    handler.BeginReceive(newState.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), newState);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private Socket findTarget(IPEndPoint iPEndPoint)
        {
            for (int i = 0; i < fib.Wires.Count; i++)
            {
                Wire w = fib.Wires[i] as Wire;
                if (iPEndPoint.Equals(w.One))
                {
                    for (int j = 0; j < sockets.Count; j++)
                    {
                        Socket so = sockets[j] as Socket;
                        if (so.RemoteEndPoint.Equals(w.Two)) return so;
                    }
                }
                if (iPEndPoint.Equals(w.Two))
                {
                    for (int j = 0; j < sockets.Count; j++)
                    {
                        Socket so = sockets[j] as Socket;
                        if (so.RemoteEndPoint.Equals(w.One)) return so;
                    }
                }
            }
            Console.WriteLine("Target was not found in the FIB.");
            return null;
        }

        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client {1}.", bytesSent, IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()));
                addLog("Send", handler.LocalEndPoint.ToString(), handler.RemoteEndPoint.ToString(), "none");
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class FIB
    {
        ArrayList _wires;

        public ArrayList Wires
        {
            get { return _wires; }
        }

        public FIB()
        {
            _wires = new ArrayList();
            _wires.Add(new Wire(new IPEndPoint(IPAddress.Parse("127.0.0.10"), 8010), new IPEndPoint(IPAddress.Parse("127.0.0.20"), 8020)));
            _wires.Add(new Wire(new IPEndPoint(IPAddress.Parse("127.0.0.30"), 8030), new IPEndPoint(IPAddress.Parse("127.0.0.40"), 8040)));
            _wires.Add(new Wire(new IPEndPoint(IPAddress.Parse("127.0.0.50"), 8050), new IPEndPoint(IPAddress.Parse("127.0.0.60"), 8060)));
        }
    }

    public class Wire
    {
        IPEndPoint _one, _two;

        public IPEndPoint One
        {
            get { return _one; }
        }

        public IPEndPoint Two
        {
            get { return _two; }
        }

        public Wire(IPEndPoint first, IPEndPoint second)
        {
            _one = first;
            _two = second;
        }
    }

    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}