using System;
using System.Text;

namespace SampleEXE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to SampleEXE!\n");

            string input = GetInput();
            byte[] byteArray = StringToBytes(input);

            DisplayBytes(byteArray, "Original bytes: ");
            DisplayString(BytesToString(byteArray), "Converted back to string: ");

            byte[] encrypted = XorEncrypt(byteArray, 42);
            DisplayBytes(encrypted, "Encrypted bytes: ");
            DisplayString(BytesToString(XorEncrypt(encrypted, 42)), "Decrypted back to string: ");

            DisplayString(ReverseString(input), "Reversed string: ");
            DisplayString(CountCharacters(input), "Character count: ");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static string GetInput()
        {
            Console.Write("Type a string: ");
            string input = Console.ReadLine().Trim();
            Console.Clear();
            return input;
        }

        static byte[] StringToBytes(string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        static string BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        static void DisplayBytes(byte[] bytes, string message)
        {
            Console.WriteLine(message);
            foreach (byte b in bytes)
            {
                Console.Write(b + " ");
            }
            Console.WriteLine();
        }

        static void DisplayString(string text, string message)
        {
            Console.WriteLine(message + text);
        }

        static byte[] XorEncrypt(byte[] bytes, byte key)
        {
            byte[] result = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = (byte)(bytes[i] ^ key);
            }
            return result;
        }

        static string ReverseString(string text)
        {
            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        static string CountCharacters(string text)
        {
            return text.Length.ToString();
        }
    }
}
