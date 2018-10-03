//***********************Read ME **********************
//This code is developed in order to assist in the understanding of how the calculation logs can be used
//to determine the type of beam from the calculation logs.
//The underlying assumptions are as follows:
//1. If the beam calculation notes contain the line "This is a VMAT field", it is considered a VMAT field.
//2. If the calculation notes state that hte field uses IMRT field normalization it could be either IMRT, Irreg or FiF
//  2.a. If the Calculation notes contain a "Compensator" category, it is considered Irregular Surface Compensator.
//  2.b. If the Field has less than 15 control points it is considered Field in Field.
//  2.c. If the field has more than 15 control points it is considered IMRT.
//3. If both results from 1 and 2 return false, it is considered a 3DCRT. At this point the gantry angle of the first and last control point is checked. 
//  3.a. If the gantry angles are the same this is a static 3DCRT.
//  3.b. If the gantry angles are different it is a non-VMAT Arc.
//While this code has been run on a couple of example patients, the clinician should always use best judgement in determinng
//what type of plan and field the patient plan is for both safety and billing concerns.
//
//If you have any questions -- matt.schmidt@varian.com.
//*******************************************
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
                //remove "null,null" inside of its CreateApplication method if in version 15+
                using (Application app = Application.CreateApplication(null,null))
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
            //Patient p = app.OpenPatientById("US-EC-005");
            //loop through all patients from the last week.
            foreach (PatientSummary psu in app.PatientSummaries.Where(x => x.CreationDateTime >= DateTime.Today.AddDays(-7)))
            {

                Patient p = app.OpenPatient(psu);
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
                                        if (line.Contains("This is a VMAT field")) { _VMAT = true; break; }
                                        if (line.Contains("IMRT field normalization")) { _IMRT = true; _FiF = true; _Irreg = true; break; }

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
                                        //if (b.ControlPoints.Count() < 15) { _IMRT = false; }
                                        //else { _FiF = false; }
                                        //decided not to use the logic above becausethe number of control points is not a robust way to tell the
                                        //difference between IMRT and FiF. Instead, if the plan is IMRT, it will have gone through the optimizer.
                                        //The existence of Optimization calculation notes will determine whether it is IMRT>
                                        if(b.CalculationLogs.Where(x=>x.Category == "Optimization").Count() == 0)
                                        {
                                            _IMRT = false;//optimization logs do not exist the plan was not optimized
                                        }
                                        else
                                        {
                                            _FiF = false;//FiF do not have optimization logs, only dose.
                                        }
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
}