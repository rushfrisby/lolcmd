using System;
using System.Collections.Generic;
using System.Drawing;
using Console = Colorful.Console;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace lolcmd
{
    class Program
    {
        private static Queue<Color> Colors = GetGradients(Color.Yellow, Color.Fuchsia, 15);
        private static Task outputTask;
        private static Task inputTask;
        private static Process process;
        private static CancellationTokenSource CTS = new CancellationTokenSource();

        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            var skipLine = false;
            process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;

            var charsPerColor = (int)Math.Floor(Console.BufferWidth / (decimal)Colors.Count());

            if (args != null && args.Any())
            {
                process.StartInfo.Arguments = string.Join(" ", args);
            }

            outputTask = new Task(() =>
            {
                var colors = new Queue<Color>(Colors);
                var currentColor = GetNextColor();
                var charsOnLine = 0;

                while (!process.StandardOutput.EndOfStream)
                {
                    var outputValue = process.StandardOutput.Read();
                    if (skipLine)
                    {
                        if (outputValue == 13 && process.StandardOutput.Peek() == 10)
                        {
                            process.StandardOutput.Read();
                            skipLine = false;
                        }
                        continue;
                    }

                    if (outputValue == 13 && process.StandardOutput.Peek() == 10)
                    {
                        Colors = new Queue<Color>(colors); //reset to previous line's starting point
                        currentColor = GetNextColor(); //advance one color
                        colors = new Queue<Color>(Colors); //new starting position for next line
                        charsOnLine = 0;
                    }

                    charsOnLine += 1;
                    if (charsOnLine % charsPerColor == 0)
                    {
                        currentColor = GetNextColor();
                    }

                    Console.Write((char)outputValue, currentColor);
                }
            }, CTS.Token);

            inputTask = new Task(() =>
            {
                using (var tr = Console.In)
                {
                    while (true)
                    {
                        var inputValue = tr.Read();
                        if (inputValue == 13 && tr.Peek() == 10)
                        {
                            tr.Read();
                            skipLine = true;
                            process.StandardInput.WriteLine();
                        }
                        else
                        {
                            process.StandardInput.Write((char)inputValue);
                        }
                    }
                }
            }, CTS.Token);

            process.Start();
            outputTask.Start();
            inputTask.Start();
            process.WaitForExit();
        }

        private static Color GetNextColor()
        {
            var color = Colors.Dequeue();
            Colors.Enqueue(color);
            return color;
        }

        private static Queue<Color> GetGradients(Color start, Color end, int steps)
        {
            var queue = new Queue<Color>();
            var colorList = new List<Color>();
            for (int i = 0; i < steps; i++)
            {
                var rAverage = end.R + (start.R - end.R) * i / steps;
                var gAverage = end.G + (start.G - end.G) * i / steps;
                var bAverage = end.B + (start.B - end.B) * i / steps;
                var color = Color.FromArgb(rAverage, gAverage, bAverage);
                colorList.Add(color);
                queue.Enqueue(color);
            }

            colorList.Reverse();
            foreach(var item in colorList)
            {
                queue.Enqueue(item);
            }

            return queue;
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                CTS.Cancel();
                process.Dispose();
            }
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    }
}
