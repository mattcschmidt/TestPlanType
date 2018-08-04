using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]
// hello, to run this in version 13 or less, please change the .NET version to .NET 4.0 client profile.
// you can do this by going to Project-->Properties and changing target framework.
// also the CreateApplication method needs some updates as mentioned below.

namespace PlanTypeCheck
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                //this line will need to have (null,null) inside of its CreateApplication method.
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static void Execute(Application app)
        {
            // TODO: Add your code here.
            //foreach(PatientSummary psumm in app.PatientSummaries.Where(x=>x.CreationDateTime >= )))
            Patient p = app.OpenPatientById("US-EC-005");
            foreach (Course c in p.Courses)
            {
                Console.WriteLine($"Running plans from {c.Id}");
                foreach (PlanSetup ps in c.PlanSetups)
                {
                    if (ps.Dose != null)
                    {

                        Console.WriteLine($"{ps.Id} contains {ps.Beams.Count()} fields.\n");
                        foreach (Beam b in ps.Beams)
                        {
                            //initial checks for plan types.
                            bool _VMAT = false;
                            bool _IMRT = false;
                            bool _3DCRT = false;
                            bool _FiF = false;
                            bool _Irreg = false;
                            //read through the message lines.
                            if (b.CalculationLogs.Where(x => x.Category == "Dose").Count() == 0)
                            {
                                Console.WriteLine($"{b.Id} does not have any calculation logs. It is possible this plan is not calculated or was not calculated in Eclipse");

                            }
                            else
                            {


                                foreach (String line in b.CalculationLogs.First(x => x.Category == "Dose").MessageLines)
                                {
                                    if (line.Contains("This is a VMAT field")) { _VMAT = true; }
                                    if (line.Contains("IMRT field normalization")) { _IMRT = true; _FiF = true; _Irreg = true; }

                                }
                                if (_Irreg)
                                {
                                    if (b.CalculationLogs.Where(x => x.Category == "Compensator").Count() != 0)
                                    {
                                        _IMRT = false; _FiF = false;
                                    }
                                    else { _Irreg = false; }
                                }
                                if (_IMRT)
                                {
                                    //this logic below doesn't work because if a plan is copied in or imported it will not have LMC notes even if it is an IMRT.
                                    //if (b.CalculationLogs.Where(x => x.Category == "LMC").Count() != 0)
                                    //{
                                    //    _FiF = false;
                                    //}
                                    //else
                                    //{
                                    //    _IMRT = false;
                                    //}
                                    //Try using the number of control points to differentiate IMRT and FiF
                                    if(b.ControlPoints.Count() < 25) { _IMRT = false; }
                                    else { _FiF = false; }
                                }
                                string field_type = "";
                                if (_VMAT) { field_type += "VMAT"; }
                                if (_IMRT) { field_type += "IMRT"; }
                                if (_Irreg) { field_type += "Irregular Surface Compensator"; }
                                if (_FiF) { field_type += "Field In Field"; }
                                if (!_VMAT && !_IMRT && !_Irreg && !_FiF)
                                {
                                    _3DCRT = true;
                                    //check whether this is an arc or a static field.
                                    if (b.ControlPoints.First().GantryAngle == b.ControlPoints.Last().GantryAngle)
                                    {
                                        if (b.MLCPlanType == MLCPlanType.Static)
                                        {
                                            field_type += "3D Conformal Field";
                                        }
                                        else { field_type += "NO MLC in the beam"; }
                                    }
                                    else
                                    {
                                        field_type += "Non-VMAT Arc Field";
                                    }

                                }
                                Console.WriteLine($"{b.Id} is a {field_type} field");
                            }
                        }
                        Console.ReadLine();
                    }
                    else { Console.WriteLine($"Plan: {ps.Id} contains no dose"); }
                }
            }
        }
    }
}