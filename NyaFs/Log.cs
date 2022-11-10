using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs
{
    public static class Log
    {
        static int LogLevel = 2;

        public static void SetLevel(int Level)
        {
            LogLevel = Level;
        }

        public static void Write(int Level, string Text)
        {
            if(Level <= LogLevel)
                Console.WriteLine(Text);
        }

        public static void Warning(int Level, string Text)
        {
            if (Level <= LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
        }

        public static void Error(int Level, string Text)
        {
            if (Level <= LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
        }

        public static void Ok(int Level, string Text)
        {
            if (Level <= LogLevel)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Text);
                Console.ResetColor();
            }
        }
    }
}
