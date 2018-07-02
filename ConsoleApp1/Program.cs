#region License
/* 
 * Copyright (C) 2018 Christian Hostelet.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using System;
using System.IO;
using System.Linq;

namespace MCoff
{
    using static System.Console;


    class Program
    {

        private string coffPathname;
        private MicrochipCOFFFile coff;

        static void Main(string[] args)
        {
            var pgm = new Program();
            pgm.Execute(args);
        }

        void Execute(string[] args)
        {
            if (args.Count() < 1)
            {
                Usage();
                return;
            }
            coffPathname = args[0];
            if (!File.Exists(coffPathname))
            {
                WriteLine($"File '{coffPathname}' not found.");
                return;
            }
            try
            {
                using (var fs = new FileStream(coffPathname, FileMode.Open, FileAccess.Read))
                {
                    using (var rd = new BinaryReader(fs))
                    {
                        try
                        {
                            coff = MicrochipCOFFFile.Create(rd);
                        }
                        catch (Exception ex)
                        {
                            WriteLine($"Caught exception: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Caught exception: {ex.Message}");
            }

            WriteLine($"Dump of COFF file '{Path.GetFileName(coffPathname)}'");
            WriteLine();
            coff.Render();
        }

        void Usage()
        {
            WriteLine("Usage:");
            WriteLine(" MCOFF.exe <coff-file>.");
        }

    }
}
