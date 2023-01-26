using System.Diagnostics;
Main();


void Main()
{
    var ebm = new EfiBootMgr();
    ebm.LoadTo(ebm.BootEntity(ebm.GetBootOrder())[0]);
}

class EfiBootMgr
{
    private static string ExecuteCommand(string command)
    {
        // Create a new process to execute the command
        Process proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = "/bin/bash";
        proc.StartInfo.Arguments = "-c \" " + command + " \"";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();

        // Read the output of the command
        string output = "";
        while (!proc.StandardOutput.EndOfStream)
        {
            output = output + "\n" + proc.StandardOutput.ReadLine();
        }

        return output;
    }

    // Get the boot order from the output of the efibootmgr command
    public IEnumerable<string> GetBootOrder()
    {
        var orderString = ExecuteCommand("efibootmgr").Split('\n').FirstOrDefault(s => s.Contains("BootOrder"));
        var order = orderString.Split(':').Skip(1).Select(s => s.Trim());
        return order;
    }

    // Get the boot order and write it to the console
    public void GetBootOrder(bool consoleWrite)
    {
        var orderString = ExecuteCommand("efibootmgr").Split('\n').FirstOrDefault(s => s.Contains("BootOrder"));
        var order = orderString.Split(':').Skip(1).Select(s => s.Trim());
        Console.WriteLine(string.Join(',', order));
    }

    private Boot getBootFromString(string boot)
    {
        if (boot.Contains("Windows"))
        {
            return Boot.Windows;
        }

        if (boot.Contains("UEFI OS"))
        {
            return Boot.UEFI;
        }

        return Boot.Other;
    }

    public void LoadTo(BootEntityModel boot)
    {
        var run = $@"pkexec efibootmgr -n {boot.BOOTID} && sync && reboot";
        ExecuteCommand(run);
    }

    public List<BootEntityModel> BootEntity(IEnumerable<string> bootOrder)
    {
        var orderString = ExecuteCommand("efibootmgr").Split('\n').ToList().FindAll(s => s.Contains('*'));
        var boot = new List<BootEntityModel>();

        foreach (var b in orderString)
        {
            var r = new BootEntityModel
            {
                BOOTID = b.Split('*')[0].Replace("Boot", ""),
                Boot = getBootFromString(b)
            };
            boot.Add(r);
        }

        return boot;
    }
}

public class BootEntityModel
{
    public string BOOTID { get; set; }
    public Boot Boot { get; set; }
}

public enum Boot
{
    Windows,
    Linux,
    UEFI,
    Other,
}