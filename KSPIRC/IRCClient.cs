/*
KSPIRC - Internet Relay Chat plugin for Kerbal Space Program.
Copyright (C) 2013 Maik Schreiber

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using UnityEngine;

namespace KSPIRC
{
    class IRCClient
    {
        private const long SERVER_PING_INTERVAL = 30000;
        private const long AUTO_JOIN_DELAY = 5000;

        public event IRCCommandHandler onCommandReceived;
        public event IRCCommandHandler onCommandSent;
        public event Callback onConnect;
        public event Callback onConnected;
        public event Callback onDisconnected;

        private IRCConfig config;

        private TcpClient client;
        private NetworkStream stream;
        private byte[] buffer = new byte[10240];
        private StringBuilder textBuffer = new StringBuilder();
        private bool tryReconnect = true;
        private bool connected;
        private long connectTime;
        private bool autoJoinsSent = false;
        private long lastServerPing = DateTime.UtcNow.Ticks / 10000;

        public void connect(IRCConfig config)
        {
            this.config = config;
            connect();
        }

        public static bool PermissiveCertificateValidationCallback(object sender,
                                                     X509Certificate certificate,
                                                     X509Chain chain,
                                                     SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Debug.Log("Certificate all good");
                return true;
            }

            Debug.LogError("Certificate error: [" + sslPolicyErrors + "]");

            // Do not allow this client to communicate with unauthenticated servers. 
            return false;
        }

        private void connect()
        {
            doDisconnect();

            if (onConnect != null)
            {
                onConnect();
            }

            try
            {
                client = new TcpClient();
                client.Connect(config.host, config.port);
                stream = client.GetStream();
                //if (secure)
                //{
                //    SslStream sslStream = new SslStream(stream, 
                //                                        false, 
                //                                        new RemoteCertificateValidationCallback (PermissiveCertificateValidationCallback), null);
                //    sslStream.AuthenticateAsClient(hostname);
                //    stream = sslStream;
                //}

                if ((config.serverPassword != null) && (config.serverPassword != ""))
                {
                    send(new IRCCommand(null, "PASS", config.serverPassword));
                }
                send(new IRCCommand(null, "NICK", config.nick));
                send(new IRCCommand(null, "USER", (String.IsNullOrEmpty(config.user) ? config.nick : config.user), "8", "*", config.nick));

                connectTime = DateTime.UtcNow.Ticks / 10000;
                connected = true;

                if (onConnected != null)
                {
                    onConnected();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void disconnect()
        {
            tryReconnect = false;
            doDisconnect();
        }

        private void doDisconnect()
        {
            bool wasConnected = connected;

            if (stream != null)
            {
                try
                {
                    send(new IRCCommand(null, "QUIT", "Build. Fly. Dream."));
                }
                catch
                {
                    // ignore
                }
            }

            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
            if (client != null)
            {
                client.Close();
                client = null;
            }

            connected = false;
            autoJoinsSent = false;
            textBuffer.Clear();

            if (wasConnected && (onDisconnected != null))
            {
                onDisconnected();
            }
        }

        private void reconnect()
        {
            if (tryReconnect && connected)
            {
                try
                {
                    tryReconnect = false;
                    doDisconnect();
                    connect();
                }
                finally
                {
                    tryReconnect = true;
                }
            }
        }

        public void update()
        {
            if (connected)
            {
                try
                {
                    if (stream.CanRead)
                    {
                        //while (stream.DataAvailable)
                        //{
                        //    int numBytes = stream.Read(buffer, 0, buffer.Length);
                        //    string text = Encoding.UTF8.GetString(buffer, 0, numBytes);
                        //    textBuffer.Append(text);
                        //}
                        while (client.Available > 0)
                        {
                            int numBytes = stream.Read(buffer, 0, buffer.Length);
                            string text = Encoding.UTF8.GetString(buffer, 0, numBytes);
                            textBuffer.Append(text);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Debug.LogException(ex);
                    reconnect();
                }

                if (textBuffer.Length > 0)
                {
                    for (; ; )
                    {
                        int pos = textBuffer.ToString().IndexOf("\r\n");
                        if (pos >= 0)
                        {
                            string line = textBuffer.ToString().Substring(0, pos);
                            textBuffer.Remove(0, pos + 2);

                            if (onCommandReceived != null)
                            {
                                try
                                {
                                    IRCCommand cmd = IRCCommand.fromLine(line);
                                    onCommandReceived(new IRCCommandEvent(cmd));
                                }
                                catch (ArgumentException e)
                                {
                                    Debug.LogException(e);
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // send something to socket to potentially trigger SocketException elsewhere when reading
                // off the socket
                long now = DateTime.UtcNow.Ticks / 10000;
                if ((now - lastServerPing) >= SERVER_PING_INTERVAL)
                {
                    lastServerPing = now;
                    send("PING :" + now);
                }

                if (!autoJoinsSent && ((now - connectTime) >= AUTO_JOIN_DELAY))
                {
                    autoJoinChannels();
                }
            }
        }

        public void send(IRCCommand cmd)
        {
            if (onCommandSent != null)
            {
                onCommandSent(new IRCCommandEvent(cmd));
            }
            byte[] data = Encoding.UTF8.GetBytes(cmd.ToString() + "\r\n");
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch (SocketException ex)
            {
                Debug.LogException(ex);
                reconnect();
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
                reconnect();
            }
        }

        public void send(string cmdAndParams)
        {
            if (onCommandSent != null)
            {
                onCommandSent(new IRCCommandEvent(IRCCommand.fromLine(cmdAndParams)));
            }
            byte[] data = Encoding.UTF8.GetBytes(cmdAndParams + "\r\n");
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch (SocketException ex)
            {
                Debug.LogException(ex);
                reconnect();
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
                reconnect();
            }
        }

        private void autoJoinChannels()
        {
            string[] autoJoinChannels = config.channels.Split(' ');
            foreach (string channel in autoJoinChannels)
            {
                if (channel.StartsWith("#"))
                {
                    send("JOIN " + channel);
                }
            }
            autoJoinsSent = true;
        }
    }
}