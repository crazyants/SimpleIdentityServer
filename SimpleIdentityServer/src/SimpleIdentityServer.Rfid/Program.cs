﻿#region copyright
// Copyright 2016 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using SimpleIdentityServer.Rfid.Menu;
using System;
using System.Linq;
using System.Text;
using Microsoft.Owin.Hosting;

namespace SimpleIdentityServer.Rfid
{
    class Program
    {
        static void Main(string[] args)
        {
            // Port : Port_#0004.Hub_#0003
            // VID : FFFF
            // PID : 0035
            // var b = GetIdToken();
            // WriteIdentityToken(b);
            Console.Title = "RFID reader & writer";
            using (WebApp.Start<Startup>("http://localhost:8080"))
            {
                Console.WriteLine("Server running at http://localhost:8080/");
                Console.ReadLine();
            }
        }

        private static void LaunchListener()
        {
            var listener = new CardListener();
            listener.Start();
            listener.CardReceived += CardReceived;
            Console.WriteLine("Press a key to exit the application ...");
        }

        private static void CardReceived(object sender, CardReceivedArgs e)
        {
            Console.WriteLine($"Card number received {e.CardNumber}");
        }

        private static void LaunchMenu()
        {
            var home = new ChoiceMenuItem();
            var reader = new ChoiceMenuItem("Execute system commands");
            reader.Add(new ReaderSerialNumberMenuItem());
            home.Add(reader);
            home.Execute();
        }

        private static void WriteIdentityToken(byte[] bytes)
        {
            // ISO Standard 14443A Memory type EEPROM
            // Number of blocks : 47
            // Block size : 16 bytes.
            // More information see : http://blog.pepperl-fuchs.us/high-capacity-rfid-tags
            byte mode = 0x00;
            int startIndex = 0x0D;
            int numberOfBlocks = 30;
            byte[] buffer = new byte[16 * numberOfBlocks],
                serialNumber = new byte[]
                {
                    0xFF,
                    0xFF,
                    0xFF,
                    0xFF,
                    0xFF,
                    0xFF,
                    0,0,0,0,0,0,0,0,0,0
                };
            if (bytes.Length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("Length is too high");
            }

            Buffer.BlockCopy(bytes, 0, buffer, 0, bytes.Length);
            for (var i = 1; i < numberOfBlocks; i++)
            {
                try
                {
                    var temp = buffer.Skip(i * 16).Take(16).ToArray();
                    var tempIndex = Convert.ToByte(startIndex);
                    int nRet = Reader.MF_Write(mode, tempIndex, 1, serialNumber, temp);
                    startIndex = startIndex + 1;
                }
                catch (Exception)
                {
                    string s = "";
                }
            }
        }

        static byte[] GetIdToken()
        {
            const string idToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";
            return  Encoding.UTF8.GetBytes(idToken);
        }
    }
}
