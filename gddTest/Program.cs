using System;
using System.Collections.Generic;

using DiskDeviceUtility;  // Reference to your library

namespace DiskDeviceConsoleTest {
	static class Program {
		static void Main(String[] _args) {
			Console.WriteLine("Disk Device Information Utility");
			Console.WriteLine("==============================\n");

			try {
				// Create instance of the utility class
				DiskDeviceInfo diskInfo = new DiskDeviceInfo();

				// Process command line arguments
				Boolean onlyReady = true;  // Default to only show ready drives
				DeviceType? filterType = null;
				OutputFormat format = OutputFormat.Table;

				foreach (String arg in _args) {
					if (arg.Equals("-a", StringComparison.OrdinalIgnoreCase)) {
						onlyReady = false;  // Show all drives including those not ready
					}
					else if (arg.Equals("-csv", StringComparison.OrdinalIgnoreCase)) {
						format = OutputFormat.CSV;
					}
					else if (arg.StartsWith("-t:", StringComparison.OrdinalIgnoreCase)) {
						String typeStr = arg.Substring(3).ToUpper();
						switch (typeStr) {
							case "LOCAL":
								filterType = DeviceType.Local;
								break;
							case "REMOVABLE":
								filterType = DeviceType.Removable;
								break;
							case "NETWORK":
								filterType = DeviceType.Network;
								break;
							case "CDROM":
								filterType = DeviceType.CDRom;
								break;
							case "RAM":
								filterType = DeviceType.Ram;
								break;
							default:
								Console.WriteLine($"Unknown device type: {typeStr}");
								break;
						}
					}
					else if (arg.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
										arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
										arg.Equals("--help", StringComparison.OrdinalIgnoreCase)) {
						ShowHelp();
						return;
					}
				}

				// Get and display disk information
				List<DiskDevice> devices = diskInfo.GetDiskDevices(filterType, onlyReady);
				diskInfo.PrintDiskDevices(devices, format);
			}
			catch (Exception ex) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"Error: {ex.Message}");
				if (ex.InnerException != null) {
					Console.WriteLine($"Inner error: {ex.InnerException.Message}");
				}
				Console.ResetColor();
			}

			// Wait for a key press in debug mode
#if DEBUG
			Console.WriteLine("\nPress any key to exit...");
			Console.ReadKey();
#endif
		}

		static void ShowHelp() {
			Console.WriteLine("Usage: DiskDeviceConsoleTest [options]");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  -a         Show all drives, including those without media");
			Console.WriteLine("  -csv       Output in CSV format instead of table");
			Console.WriteLine("  -t:TYPE    Filter by device type: LOCAL, REMOVABLE, NETWORK, CDROM, or RAM");
			Console.WriteLine("  -?, -h     Show this help message");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  DiskDeviceConsoleTest");
			Console.WriteLine("  DiskDeviceConsoleTest -a -t:REMOVABLE");
			Console.WriteLine("  DiskDeviceConsoleTest -csv -t:LOCAL");
		}
	}
}