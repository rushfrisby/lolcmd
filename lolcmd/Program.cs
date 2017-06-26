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
    internal class Program
    {
        //private static Queue<Color> Colors = GetGradients(Color.Yellow, Color.Fuchsia, 15);
        private static Queue<Color> _colors = GetDefaultGradients();
        private static Task _outputTask;
        private static Task _inputTask;
        private static Process _process;
        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        private static void Main(string[] args)
        {
            _handler = ConsoleEventCallback;
            SetConsoleCtrlHandler(_handler, true);

            var skipLine = false;
            _process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                }
            };

            var charsPerColor = (int)Math.Floor(Console.BufferWidth / (decimal)_colors.Count()) * 2;

            if (args != null && args.Any())
            {
                _process.StartInfo.Arguments = string.Join(" ", args);
            }

            _outputTask = new Task(() =>
            {
                var colors = new Queue<Color>(_colors);
                var currentColor = GetNextColor();
                var charsOnLine = 0;

                while (!_process.StandardOutput.EndOfStream)
                {
                    var outputValue = _process.StandardOutput.Read();
                    if (skipLine)
                    {
                        if (outputValue == 13 && _process.StandardOutput.Peek() == 10)
                        {
                            _process.StandardOutput.Read();
                            skipLine = false;
                        }
                        continue;
                    }

                    if (outputValue == 13 && _process.StandardOutput.Peek() == 10)
                    {
                        _colors = new Queue<Color>(colors); //reset to previous line's starting point
                        currentColor = GetNextColor(); //advance one color
                        colors = new Queue<Color>(_colors); //new starting position for next line
                        charsOnLine = 0;
                    }

                    charsOnLine += 1;
                    if (charsOnLine % charsPerColor == 0)
                    {
                        currentColor = GetNextColor();
                    }

                    Console.Write((char)outputValue, currentColor);
                }
            }, Cts.Token);

            _inputTask = new Task(() =>
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
                            _process.StandardInput.WriteLine();
                        }
                        else
                        {
                            _process.StandardInput.Write((char)inputValue);
                        }
                    }
                }
            }, Cts.Token);

            _process.Start();
            _outputTask.Start();
            _inputTask.Start();
            _process.WaitForExit();
        }

        private static Color GetNextColor()
        {
            var color = _colors.Dequeue();
            _colors.Enqueue(color);
            return color;
        }

        private static Queue<Color> GetGradients(Color start, Color end, int steps)
        {
            var queue = new Queue<Color>();
            var colorList = new List<Color>();
            for (var i = 0; i < steps; i++)
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

        private static Queue<Color> GetDefaultGradients()
        {
            var q = new Queue<Color>();
            q.Enqueue(Color.FromArgb(232, 42, 38)); // E82A26 red
            q.Enqueue(Color.FromArgb(232, 106, 25));
            q.Enqueue(Color.FromArgb(233, 170, 12));
            q.Enqueue(Color.FromArgb(234, 235, 0)); // EAEB00 yellow
            q.Enqueue(Color.FromArgb(156, 216, 15));
            q.Enqueue(Color.FromArgb(78, 197, 30));
            q.Enqueue(Color.FromArgb(0, 179, 45)); // 00B32D green
            q.Enqueue(Color.FromArgb(2, 140, 107));
            q.Enqueue(Color.FromArgb(5, 102, 169));
            q.Enqueue(Color.FromArgb(8, 64, 231)); // 0840E7 blue
            q.Enqueue(Color.FromArgb(90, 42, 229));
            q.Enqueue(Color.FromArgb(172, 21, 227));
            q.Enqueue(Color.FromArgb(255, 0, 226)); // FF00E2 magenta
            q.Enqueue(Color.FromArgb(247, 14, 163));
            q.Enqueue(Color.FromArgb(239, 28, 100));

            foreach (var color in q.ToArray().Reverse())
            {
                q.Enqueue(color);
            }

            return q;
        }

        private static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Cts.Cancel();
                _process.Dispose();
            }
            return false;
        }
        static ConsoleEventDelegate _handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    }
}
