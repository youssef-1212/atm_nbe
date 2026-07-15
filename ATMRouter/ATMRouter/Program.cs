using System;
using System.Linq;
using ATMRouter.Models;

Console.WriteLine("Connecting to the database...");

try
{
    using var context = new AtmrouterContext();
    
    // Test the connection by counting the ATMs
    var atmCount = context.Atms.Count();
    
    Console.WriteLine("✅ Database connection successful!");
    Console.WriteLine($"There are {atmCount} ATMs in the database.");
}
catch (Exception ex)
{
    Console.WriteLine("❌ Database connection failed:");
    Console.WriteLine(ex.Message);
}
