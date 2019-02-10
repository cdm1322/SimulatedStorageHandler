/*-----------------------------------------------------------------------------------------
    Author: Cody Martin
    Date: Feb 5 2019
    Purpose: A re-engineered memory driver originally written for an OS course
-----------------------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Globalization;
namespace FileSystemDriver
{
    // THIS IS ONLY A DRIVER FOR WRITING AND DELETING BINARY DATA FROM A BINARY FILE
    // BINARY FILE IS TO REPRESENT PHYSICAL FLASH MEMORY

    // THE FOLLOWING MEMORY HANDLER DOES NOT CHECK IF DATA IS ALREADY PRESENT
    // OVERWRITING CAN EASILY OCCUR!!!
    // THIS IS TO BE EXPANDED LATER TO INCLUDE FILE TABLES

    public class MemoryHandler
    {
        // Each memory sector is 64KB, 64x1024=65536
        // 65536 bytes total
        private static int totalBytes = 65536;
        private static int numberOfSectors = 20;
        private static String line = "-------------------------------------------------------\n";

        private static FileStream memory = new FileStream("memory.bin", System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);

        public static int Main(string[] arguments)
        {

            // checking if file already exists
            // if it does exist, retains information written
            // if not, initializes the file to all zeroes
            long fileLength = new System.IO.FileInfo("memory.bin").Length;
            if (fileLength == 0)
            {
                // i: sectors
                // j: bytes
                for (int i = 0; i < numberOfSectors; i++)
                {
                    for (int j = 0; j < totalBytes; j++)
                    {
                        byte zeroes = 0x00;
                        memory.WriteByte(zeroes);
                    }
                }
                Console.Write("Memory file written.\n");
            }
            else
            {
                Console.Write("Memory already written.\n");
            }

            while (true)
            {
                Console.Write(line);
                Console.Write("What would you like to do?\n");
                Console.Write("Please enter: write, read, erase, eraseall, exit\n");
                String input = Console.ReadLine();
                Console.Write(line);

                if (input.ToString() == "write")
                {
                    // write word to location

                    Console.Write("Where would you like to write to?\n");
                    Console.Write("Please enter a byte offset from 0-" + ((totalBytes * numberOfSectors) - 1) + "\n");

                    int locationToWrite = 0;
                    bool next = false;
                    while (!next)
                    {
                        input = Console.ReadLine();
                        Console.Write(line);
                        if (input.All(char.IsDigit) && Int32.TryParse(input, NumberStyles.Integer, null as IFormatProvider, out locationToWrite))
                        {
                            next = true;
                        }
                        else Console.Write("Bad input, please only enter numbers between 0-" + ((totalBytes * numberOfSectors) - 1) + ":\n");
                    }

                    byte byteValue = 0;
                    if (locationToWrite < totalBytes * numberOfSectors && locationToWrite >= 0)
                    {
                        Console.Write("What would you like to write to location: " + locationToWrite + "?\n");
                        Console.Write("Please enter a one byte value:\n");

                        bool passed = false;
                        while (!passed)
                        {
                            input = Console.ReadLine();
                            NumberStyles styles = NumberStyles.HexNumber;
                            passed = Byte.TryParse(input, styles, null as IFormatProvider, out byteValue);
                            if (!passed)
                            {
                                Console.Write("Invalid input. Please type in one byte as hex.\n");
                                Console.Write("ex:, FF, 00, ac\n");
                                Console.Write(line);
                            }
                        }

                        Console.Write("Byte parsed correctly.\n");
                    }
                    else
                    {
                        Console.Write("Incorrect Address.\n");
                    }

                    byte byteToWrite = byteValue;

                    writeByte(locationToWrite, byteToWrite);

                    continue;
                }
                else if (input.ToString() == "read")
                {
                    Console.Write("What address would you like to read from?\n");
                    Console.Write("Please enter a byte offset from 0-" + ((totalBytes * numberOfSectors) - 1) + ":\n");
                    bool next = false;
                    int locationToRead = 0;
                    while (!next)
                    {
                        input = Console.ReadLine();
                        Console.Write(line);
                        if (input.All(char.IsDigit) && Int32.TryParse(input, NumberStyles.Integer, null as IFormatProvider, out locationToRead))
                        {
                            if (locationToRead > ((totalBytes * numberOfSectors) - 1))
                            {
                                Console.Write("Bad input, please only enter numbers between 0-" + ((totalBytes * numberOfSectors) - 1) + ":\n");
                            }
                            else next = true;
                        }
                        else Console.Write("Bad input, please only enter numbers between 0-" + ((totalBytes * numberOfSectors) - 1) + ":\n");
                    }

                    byte data = readByte(locationToRead);
                    byte[] dataArr = new byte[1];
                    dataArr[0] = data;
                    Console.Write("Byte read: " + BitConverter.ToString(dataArr) + "\n");

                }
                else if (input.ToString() == "erase")
                {
                    Console.Write("Erase which sector?\n");
                    Console.Write("Please enter a value between 1-20\n");
                    int sectorToErase = 0;
                    bool next = false;
                    while (!next)
                    {
                        input = Console.ReadLine();
                        if (input.All(char.IsDigit) && Int32.TryParse(input, NumberStyles.Integer, null as IFormatProvider, out sectorToErase))
                        {
                            if (sectorToErase > 0 && sectorToErase <= 20) next = true;
                            else Console.Write("Bad input, please only enter numbers between 1-20:\n");
                        }
                        else Console.Write("Bad input, please only enter numbers between 1-20:\n");
                    }

                    eraseSector(sectorToErase);
                    continue;
                }

                else if (input.ToString() == "eraseall")
                {
                    Console.Write("Are you sure you want to erase all sectors?\n");
                    Console.Write("Yes/No\n");
                    input = Console.ReadLine();

                    input = input.ToUpper();
                    if (input.ToString() == "YES" || input.ToString() == "Y" || input.ToString() == "YE")
                    {
                        eraseAllSectors();
                    }
                    else if (input.ToString() == "NO" || input.ToString() == "N")
                    {
                        continue;
                    }

                    continue;
                }
                else if (input.ToString() == "exit")
                {
                    Console.Write("Goodbye!\n");
                    break;
                }
                else if (input.ToString() != "exit" && input.ToString() != "write" && input.ToString() != "read"
                        && input.ToString() != "erase" && input.ToString() != "eraseall")
                {
                    Console.Write("Invalid Input. Try Again.\n");
                    continue;
                }
                else
                {
                    Console.Write("Something strange happened. Exitting.\n");
                    break;
                }
            }

            return 1;
        }

        // As is the case with flash memory, entire sectors are erased
        public static int eraseSector(int sectorNumber)
        {
            // ensuring we are actually handling a sector within our bounds
            if (sectorNumber >= numberOfSectors || sectorNumber < 1) return 0;

            int sectorToErase = sectorNumber - 1; // zero indexing

            // if sector 0, then 0*65536 = 0
            // if sector 1, then 1*65536 = 65536 (first byte of sector 1)
            int startingLocation = sectorToErase * totalBytes;
            int endingLocation = startingLocation + totalBytes - 1;
            memory.Seek(startingLocation, SeekOrigin.Begin);

            for (int i = startingLocation; i < endingLocation; i++)
            {
                memory.WriteByte(0x00);
            }

            Console.Write("Sector " + sectorNumber + " erased.\n");

            return 1;
        }

        public static int eraseAllSectors()
        {
            for (int i = 0; i < numberOfSectors; i++)
            {
                for (int j = 0; j < totalBytes; j++)
                {
                    byte zeroes = 0x00;
                    memory.WriteByte(zeroes);
                }
            }
            Console.Write("Memory erased successfully.\n");
            return 1;
        }

        // address to be given as a byte offset from the beginning
        public static int writeByte(int address, byte byteToWrite)
        {
            if (address >= 0 && address <= (totalBytes * numberOfSectors) - 1)
            {
                memory.Seek(address, SeekOrigin.Begin);
                memory.WriteByte(byteToWrite);
            }
            else
            {
                Console.Write("Error\n");
            }

            return 1;
        }

        // address to be given as a byte offset from the beginning
        public static byte readByte(int address)
        {
            byte data = 0;
            if (address >= 0 && address <= (totalBytes * numberOfSectors) - 1)
            {
                memory.Seek(address, SeekOrigin.Begin);
                data = Convert.ToByte(memory.ReadByte());
            }
            return data;
        }

    }
}