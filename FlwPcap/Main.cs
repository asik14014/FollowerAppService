using System;
using System.Collections.Generic;
using System.Diagnostics;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace FlwPcap
{
    public class Main
    {
        public Main()
        {
            
        }

        public IList<LivePacketDevice> GetDevices()
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                return null;
            }

            return allDevices;
        }

        public void Handler()
        {
            var devices = GetDevices();
            PacketDevice selectedDevice = devices[4];


            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                communicator.SetFilter("tcp port 443");

                Console.WriteLine("Listening on " + selectedDevice.Description + "...");

                Packet packet;
                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" +
                                              packet.Length);
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);
            }
        }
    }
}
