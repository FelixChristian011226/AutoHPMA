using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHPMA.GameTask;

public class SystemControl
{
    public static nint FindMumuSimulatorHandle()
    {
        return FindHandleByProcessName("Mumu模拟器", "Mumu模拟器12", "MuMuPlayer");
    }

    public static nint FindHandleByProcessName(params string[] names)
    {
        foreach (var name in names)
        {
            var pros = Process.GetProcessesByName(name);
            if (pros.Any())
            {
                return pros[0].MainWindowHandle;
            }
        }

        return 0;
    }

}
