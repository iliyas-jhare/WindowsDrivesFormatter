using System.Diagnostics;
using System.Reflection;
using System.Text;

try
{
    if (Invalid(args) || HelpRequested(args))
    {
        Console.WriteLine("No command arguments were passed.");
        Console.WriteLine("Formats drives from Windows system.");
        Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name?.ToUpper()} D: E: F: G: H:");
        Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name?.ToUpper()} --list");
        return;
    }

    if (args.Contains("--list") || args.Contains("-l"))
    {
        Array.ForEach(DriveInfo.GetDrives(),
            d => Console.WriteLine($"Drive Name={d.Name}, Label={d.VolumeLabel}, Type={d.DriveType}"));
        return;
    }

    FormatDrives(args);
    Console.WriteLine("All done.");
}
catch (Exception e)
{
    Console.WriteLine(new StringBuilder()
        .AppendLine($"Application stopped unexpectedly.")
        .AppendLine(e.Message)
        .AppendLine(e.Source)
        .AppendLine(e.StackTrace));
}

static bool Invalid(string[] args)
    => args is null ||
       args.Any() == false;

static bool HelpRequested(string[] args)
    => args.Contains("?") ||
       args.Contains("/?") ||
       args.Contains("-h") ||
       args.Contains("--help");

static void FormatDrives(string[] args)
{
    DriveInfo[] drives = DriveInfo.GetDrives();

    foreach (var arg in args)
    {
        if (string.IsNullOrEmpty(arg)) continue;

        if (!drives.Any(d => GetDriveLetter(d) == arg))
        {
            Console.WriteLine($"Drive not found. {arg}");
            continue;
        }

        DriveInfo drive = drives.Single(d => GetDriveLetter(d) == arg);
        Console.WriteLine($"Drive found. {arg}");
        Console.WriteLine(drive);
        Console.WriteLine("Formatting drive...");
        FormatDrive(drive);
    }
}

static void FormatDrive(DriveInfo drive)
{
    var letter = GetDriveLetter(drive);
    if (!CanFormat(letter))
    {
        Console.WriteLine($"Cannot format the drive {letter}. It has either invalid drive letter or its a Windows System Drive.");
        return;
    }

    ProcessStartInfo info = new()
    {
        FileName = "cmd.exe",
        Arguments = $"/c {FormatArguments(letter, drive.VolumeLabel)}",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8
    };
    Process process = new() { StartInfo = info };
    process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
    process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

    if (process is null || !process.Start())
    {
        Console.WriteLine("Process could not be started. Please check the log for more details.");
        return;
    }

    Console.WriteLine("Process started.");
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
    Console.WriteLine($"Process finished. Exit STATUS: {ToProcessExitStatus(process.ExitCode)}");
}

static string FormatArguments(string letter, string label)
    => string.IsNullOrEmpty(label)
    ? $"format {letter} /q /x /y"
    : $"format {letter} /v:{label} /q /x /y";

static string ToProcessExitStatus(int code)
    => code switch
    {
        0 => "Success",
        1 => "Incorrect parameters",
        5 => "User pressed N in response to the prompt.",
        _ => "A fatal error occurred."
    };

static bool CanFormat(string drive)
    => !string.IsNullOrEmpty(drive)
       && drive.Length == 2
       && char.IsLetter(drive[0])
       && drive[1] == ':'
       && drive != Environment.GetEnvironmentVariable("SystemDrive");

static string GetDriveLetter(DriveInfo drive)
    => drive.Name.Replace("\\", string.Empty);
