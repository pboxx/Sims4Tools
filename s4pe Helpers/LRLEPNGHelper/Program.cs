/***************************************************************************
 *  Copyright (C) 2016 by Sims 4 Tools Development Team                    *
 *  Credits: Peter Jones, Keyi Zhang, Cmar                                 *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using s4pi.Helpers;
using System.IO;
using s4pi.ImageResource;

namespace LRLEPNGHelper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
           if (args.Contains("/import"))
           {
               MessageBox.Show("Hi");
               List<string> largs = new List<string>(args);
               largs.Remove("/import");
               s4pi.Helpers.RunHelper.Run(typeof(LRLEPNGHelper.Import), largs.ToArray());
           }
           else if (args.Contains("/export"))
           {
               using (FileStream fs = new FileStream(args[1], FileMode.Open))
               {
                   using (SaveFileDialog save = new SaveFileDialog() { Filter = "PNG|*.png", FileName = Path.GetFileName(args[1]), Title = "Export to PNG" })
                   {
                       if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                       {
                           using (FileStream fs2 = new FileStream(save.FileName, FileMode.Create))
                           {
                               LRLEResource r = new LRLEResource(1, fs);
                               r.ToImageStream().CopyTo(fs2);
                           }
                       }
                   }
               }
           }
        }
    }
}
