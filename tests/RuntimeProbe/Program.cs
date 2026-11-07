using System;
using TheFarmerWasRefactored.Runtime;

try
{
    RuntimeProbe.Run(Console.WriteLine);
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
