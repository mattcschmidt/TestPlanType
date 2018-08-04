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
            Patient p = app.OpenPatientById("US-EC-055");
            foreach(Course c in p.Courses)
            {
                foreach(PlanSetup ps in c.PlanSetups)
                {
                    if(ps.Dose != null)
                    {
                        //read through the message lines.
                        
                    }
                }
            }
        }
    }
}
