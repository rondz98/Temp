using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Temp.Entities;

namespace Temp.Helpers
{
    public class CustomCurveParse
    {
        public CustomCurveParse() {
            parseFiles();
        }

        public void parseFiles()
        {
            curves.Clear();
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            string[] files = Directory.GetFiles(appFolderPath);

            List<string> customCurves = new List<string>();

            if (files.Count() > 0)
            {
                foreach (string file in files)
                {
                    if (file.EndsWith(".ccw"))
                    {
                        customCurves.Add(file);
                    }
                }
            }

            foreach (string file in customCurves)
            {
                Curve? curve = parseCurveFile(Path.GetFileName(file), File.ReadAllText(file));
                if (curve != null)
                {
                    curves.Add(curve);
                }
            }
        }

        public Curve parseCurveFromName(string name)
        {
            Curve Resultcurve = null;
            foreach(Curve curve in curves)
            {
                if(curve.Name == name)
                {
                    Resultcurve = curve;
                    break;
                }
            }
            return Resultcurve;
        }

        public bool CreateFile(string Name)
        {
            bool result = false;

            try
            {
                File.WriteAllText(appFolderPath+"\\" + Name + ".ccw", "");
            }
            catch
            {
                return result;
            }

            return result;
        }

        public bool RenameFile(string oldName, string newName)
        {
            bool result = false;

            try
            {
                File.Move(appFolderPath + "\\" + oldName + ".ccw", appFolderPath + "\\" + newName + ".ccw");
            }
            catch
            {
                return result;
            }

            return result;
        }

        public bool SaveCurve(Curve Savecurve)
        {
            bool result = false;

            string ChangedFilePath = findCurveFile(Savecurve.Name);

            if (string.IsNullOrEmpty(ChangedFilePath))
            {
                CreateFile(Savecurve.Name);
                ChangedFilePath = findCurveFile(Savecurve.Name);
            }

            try
            {
                File.WriteAllText(ChangedFilePath, string.Empty);
                using (StreamWriter writer = new StreamWriter(ChangedFilePath))
                {
                    for(int i=0; i < Savecurve.points.Count; i++)
                    {
                        if (i < Savecurve.points.Count - 1)
                        {
                            writer.WriteLine(Savecurve.points[i].ID + ":" + Savecurve.points[i].TempValue + ":" + Savecurve.points[i].TimeValue);
                        }
                        else
                        {
                            writer.Write(Savecurve.points[i].ID + ":" + Savecurve.points[i].TempValue + ":" + Savecurve.points[i].TimeValue);
                        }
                    }
                    writer.Flush();
                }

                result = true;
            }
            catch
            {

            }

            return result;
        }

        public string findCurveFile(string curveName)
        {

            string[] files = Directory.GetFiles(appFolderPath);
            string result = string.Empty;

            List<string> customCurves = new List<string>();

            if (files.Count() > 0)
            {
                foreach (string file in files)
                {
                    if (file.EndsWith(".ccw"))
                    {
                        customCurves.Add(file);
                    }
                }
            }

            foreach (string file in customCurves)
            {
                if (file.Contains(curveName))
                {
                    result = file;
                    break;
                }
            }

            return result;
        }

        private Curve? parseCurveFile(string name, string fileContent)
        {
            Curve newCurve = new Curve();
            newCurve.Name = name.Split('.')[0];
            try
            {
                string[] points = fileContent.Split("\r\n");

                foreach (string point in points)
                {
                    if (!string.IsNullOrEmpty(point))
                    {
                        Point singlePoint = new Point();
                        string[] splittedPoint = point.Split(':');
                        singlePoint.ID = Convert.ToInt32(splittedPoint[0]);
                        singlePoint.TempValue = Convert.ToInt32(splittedPoint[1]);
                        singlePoint.TimeValue = Convert.ToInt32(splittedPoint[2]);
                        newCurve.points.Add(singlePoint);
                    }
                }
            }
            catch
            {
                return null;
            }

            return newCurve;
        }

        /// <summary>
        /// curves parsed from custom files
        /// </summary>
        public List<Curve> curves = new List<Curve>();

        /// <summary>
        /// Folder where the custom files are placed
        /// </summary>
        private string appFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeDo_Temp_Controller");
    }
}
