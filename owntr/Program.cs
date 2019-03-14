using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace owntr
{

     class Program
    {
         static void Main(string[] args)
        {
            byte[] mess = new byte[1024];
            int answer, begin, end;
            Socket icmppacket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            string IPorAdress = args[0];
            IPAddress ip;
            IPEndPoint iep;
            if (IPAddress.TryParse(IPorAdress, out ip))
            {
                iep = new IPEndPoint(IPAddress.Parse(IPorAdress), 0);
            }
            else
            {
                IPHostEntry iphe = Dns.GetHostEntry(args[0]);
                iep = new IPEndPoint(iphe.AddressList[0], 0);
            }
            EndPoint ep = (EndPoint)iep;
            ICMP packet = new ICMP();

            packet.Type = 8;
            packet.Code = 0;
            packet.Checksum = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 2, 2);
            mess = Encoding.ASCII.GetBytes("abcdefg");
            Buffer.BlockCopy(mess, 0, packet.Message, 4, mess.Length);
            packet.MessageSize = mess.Length + 4;
            int packetsize = packet.MessageSize + 4;

            packet.Checksum = packet.getChecksum();

            icmppacket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

            int errors = 0;
            int i = 0;
            int number = 1;
            int exitflag = 0;
            while (exitflag == 0)
            {
                i = i + 1;
                icmppacket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);
                Console.Write("{0}:", i);
                int amount;
                    //amount = int.Parse(args[1]);
                    amount = 3;
                for (int j = 0; j < amount; j++)
                {
                    packet.Checksum = 0;
                    Buffer.BlockCopy(BitConverter.GetBytes(number), 0, packet.Message, 2, 2);
                    number++;
                    packet.Checksum = packet.getChecksum();

                    begin = Environment.TickCount;
                    icmppacket.SendTo(packet.getBytes(), iep);
                    try
                    {
                        mess = new byte[1024];
                        answer = icmppacket.ReceiveFrom(mess, ref ep);
                        ICMP response = new ICMP(mess, answer);
                        end = Environment.TickCount;
                        if (response.Type == 11)
                            Console.Write(" {0}ms ", end - begin);
                        if (response.Type == 0)
                        {
                            Console.Write(" {0}ms ", end - begin);
                            exitflag = 1;
                        }
                        errors = 0;
                    }
                    catch 
                    {
                        Console.Write(" * ");
                        errors++;
                        if (errors == amount*9)
                        {
                            Console.WriteLine("Unable to contact remote host");
                            exitflag = 1;
                        }
                    }
                }
                if (errors < amount)
                    Console.WriteLine(":{0}", ep.ToString());
                else
                    Console.WriteLine("");
            }

            icmppacket.Close();
        }
    }

    class ICMP
    {
        public byte Type;
        public byte Code;
        public UInt16 Checksum;
        public int MessageSize;
        public byte[] Message = new byte[1024];

        public ICMP()
        {
        }

        public ICMP(byte[] mess, int size)
        {
            Type = mess[20];
            Code = mess[21];
            Checksum = BitConverter.ToUInt16(mess, 22);
            MessageSize = size - 24;
            Buffer.BlockCopy(mess, 24, Message, 0, MessageSize);
        }

        public byte[] getBytes()
        {
            byte[] mess = new byte[MessageSize + 9];
            Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, mess, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, mess, 1, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, mess, 2, 2);
            Buffer.BlockCopy(Message, 0, mess, 4, MessageSize);
            return mess;
        }

        public UInt16 getChecksum()
        {
            UInt32 CheckSum = 0;
            byte[] mess = getBytes();
            int packetsize = MessageSize + 8;
            int index = 0;

            while (index < packetsize)
            {
                CheckSum += Convert.ToUInt32(BitConverter.ToUInt16(mess, index));
                index += 2;
            }
            CheckSum = (CheckSum >> 16) + (CheckSum & 0xffff);
            CheckSum += (CheckSum >> 16);
            return (UInt16)(~CheckSum);
        }

    }
}