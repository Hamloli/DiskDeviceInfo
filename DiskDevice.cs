using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if WINDOWS
using System.Management;
#endif

namespace DiskDeviceUtility {
	/// <summary>
	/// Represents the type of storage device
	/// </summary>
	public enum DeviceType {
		/// <summary>Unknown device type</summary>
		Unknown = 0,

		/// <summary>Fixed local disk (internal HDD, SSD)</summary>
		Local = 1,

		/// <summary>Removable media (USB drives, memory cards)</summary>
		Removable = 2,

		/// <summary>Network mapped drive</summary>
		Network = 3,

		/// <summary>Optical drive (CD, DVD, Blu-ray)</summary>
		CDRom = 4,

		/// <summary>RAM disk</summary>
		Ram = 5
	}

	/// <summary>
	/// Represents how a device is physically connected to the system
	/// </summary>
	public enum MountType {
		/// <summary>Mount type could not be determined</summary>
		Unknown = 0,

		/// <summary>Physical device (directly connected via hardware interface)</summary>
		Physical = 1,

		/// <summary>Network mounted device</summary>
		Network = 2,

		/// <summary>Virtual device (software-based)</summary>
		Virtual = 3
	}

	/// <summary>
	/// Represents detailed information about a disk device in the system
	/// </summary>
	public class DiskDevice {
		/// <summary>
		/// Constructor that initializes all string properties to empty strings
		/// </summary>
		public DiskDevice() {
			DriveLetter = String.Empty;
			VolumeName = String.Empty;
			FileSystem = String.Empty;
			DeviceID = String.Empty;
			Model = String.Empty;
			SerialNumber = String.Empty;
			InterfaceType = String.Empty;
			MediaType = String.Empty;
			FirmwareRevision = String.Empty;
			Manufacturer = String.Empty;
			PartitionStyle = String.Empty;
			HealthStatus = String.Empty;
			VolumeGuid = String.Empty;
			BusType = String.Empty;
		}

		/// <summary>
		/// Drive letter with colon (e.g., "C:")
		/// </summary>
		public String DriveLetter { get; set; }

		/// <summary>
		/// Volume label or name of the drive
		/// </summary>
		public String VolumeName { get; set; }

		/// <summary>
		/// Type of storage device (Local, Removable, Network, etc.)
		/// </summary>
		public DeviceType DeviceType { get; set; }

		/// <summary>
		/// How the device is mounted (Physical, Network, Virtual)
		/// </summary>
		public MountType MountType { get; set; }

		/// <summary>
		/// Indicates if the drive is ready for I/O operations
		/// </summary>
		public Boolean IsReady { get; set; }

		/// <summary>
		/// File system format (NTFS, FAT32, exFAT, etc.)
		/// </summary>
		public String FileSystem { get; set; }

		/// <summary>
		/// Total size of the drive in bytes
		/// </summary>
		public Int64 TotalSize { get; set; }

		/// <summary>
		/// Available free space in bytes
		/// </summary>
		public Int64 FreeSpace { get; set; }

		/// <summary>
		/// Windows device ID (e.g., "\\\\.\\PHYSICALDRIVE0")
		/// </summary>
		public String DeviceID { get; set; }

		/// <summary>
		/// Manufacturer and model information
		/// </summary>
		public String Model { get; set; }

		/// <summary>
		/// Serial number of the device
		/// </summary>
		public String SerialNumber { get; set; }

		/// <summary>
		/// Interface type (IDE, SATA, SCSI, USB, etc.)
		/// </summary>
		public String InterfaceType { get; set; }

		/// <summary>
		/// Media type (Fixed hard disk, Removable media, etc.)
		/// </summary>
		public String MediaType { get; set; }

		/// <summary>
		/// Firmware revision of the device
		/// </summary>
		public String FirmwareRevision { get; set; }

		/// <summary>
		/// Device manufacturer name
		/// </summary>
		public String Manufacturer { get; set; }

		/// <summary>
		/// Partition style (MBR, GPT, etc.)
		/// </summary>
		public String PartitionStyle { get; set; }

		/// <summary>
		/// Sector size in bytes
		/// </summary>
		public UInt32 BytesPerSector { get; set; }

		/// <summary>
		/// Health status of the device if available
		/// </summary>
		public String HealthStatus { get; set; }

		/// <summary>
		/// GUID of the volume
		/// </summary>
		public String VolumeGuid { get; set; }

		/// <summary>
		/// Bus type (USB, SATA, NVMe, etc.)
		/// </summary>
		public String BusType { get; set; }

		/// <summary>
		/// Timestamp of when the drive was last accessed
		/// </summary>
		public DateTime? LastAccessed { get; set; }

		/// <summary>
		/// Returns a formatted string representation of the disk device
		/// </summary>
		/// <returns>String representation of the disk device</returns>
		public override String ToString() {
			return $"{DriveLetter} ({DeviceType}) - {(String.IsNullOrEmpty(VolumeName) ? "No Label" : VolumeName)} - {MountType}";
		}
	}

	/// <summary>
	/// Provides methods to retrieve detailed information about disk devices in the system
	/// </summary>
	public class DiskDeviceInfo {
		/// <summary>
		/// Gets a comprehensive list of disk devices on the system with their mounting information
		/// </summary>
		/// <param name="_deviceTypeFilter">Optional filter to only return specific device types</param>
		/// <param name="_onlyMounted">If true, only returns mounted/ready drives</param>
		/// <returns>List of DiskDevice objects containing detailed device information</returns>
		/// <remarks>
		/// This method combines information from multiple sources including DriveInfo and,
		/// on Windows, WMI to provide the most complete picture of storage devices.
		/// Physical vs Virtual determination is based on device characteristics and 
		/// interface types.
		/// </remarks>
		public List<DiskDevice> GetDiskDevices(DeviceType? _deviceTypeFilter = null, Boolean _onlyMounted = false) {
			List<DiskDevice> devices = new List<DiskDevice>();

			try {
				// Get basic drive info
				DriveInfo[] drives = DriveInfo.GetDrives();

				// Create mapping from drive letter to DiskDevice
				Dictionary<String, DiskDevice> driveMapping = new Dictionary<String, DiskDevice>();

#if WINDOWS
                Dictionary<String, String> volumeGuidMap = GetVolumeGuidMapping();
#endif

				foreach (DriveInfo drive in drives) {
					// Skip drives that aren't ready if requested
					if (_onlyMounted && !drive.IsReady) {
						continue;
					}

					// Create the basic disk device
					DiskDevice device = new DiskDevice {
						DriveLetter = drive.Name.TrimEnd('\\'),
						IsReady = drive.IsReady,
						DeviceType = MapDriveType(drive.DriveType),
						LastAccessed = GetLastAccessTime(drive.Name)
					};

#if WINDOWS
                    // Map volume GUID if available
                    if (volumeGuidMap.ContainsKey(device.DriveLetter)) {
                        device.VolumeGuid = volumeGuidMap[device.DriveLetter];
                    }
#endif

					// Filter by device type if requested
					if (_deviceTypeFilter.HasValue && device.DeviceType != _deviceTypeFilter.Value) {
						continue;
					}

					// Add additional info for ready drives
					if (drive.IsReady) {
						device.VolumeName = drive.VolumeLabel;
						device.FileSystem = drive.DriveFormat;
						device.TotalSize = drive.TotalSize;
						device.FreeSpace = drive.AvailableFreeSpace;
					}

					driveMapping[drive.Name.Replace("\\", "")] = device;
					devices.Add(device);
				}

#if WINDOWS
                // Windows-specific enhancements using WMI
                EnrichWithWindowsSpecificInfo(driveMapping);
#endif

				//// Mark any remaining drives that couldn't be mapped to physical devices
				//foreach (DiskDevice device in devices) {
				//	if (device.MountType == MountType.Unknown) {
				//		if (device.DeviceType == DeviceType.Network) {
				//			device.MountType = MountType.Network;
				//		}
				//		else if (device.DeviceType == DeviceType.Ram) {
				//			device.MountType = MountType.Virtual;
				//		}
				//		else {
				//			device.MountType = DetermineVirtualOrPhysical(device);
				//		}
				//	}
				//}

				// For each device, determine mount type if it hasn't been set
				foreach (DiskDevice device in devices) {
					if (device.MountType == MountType.Unknown) {
						device.MountType = DetermineVirtualOrPhysical(device);
					}
				}


			}
			catch (Exception ex) {
				System.Diagnostics.Debug.Print($"Error in GetDiskDevices: {ex.Message}\nStack trace: {ex.StackTrace}");
				throw;
			}

			return devices;
		}

		/// <summary>
		/// Maps System.IO.DriveType to our custom DeviceType enum
		/// </summary>
		/// <param name="_driveType">The DriveType from System.IO</param>
		/// <returns>Corresponding DeviceType value</returns>
		private DeviceType MapDriveType(DriveType _driveType) {
			switch (_driveType) {
				case DriveType.Fixed:
					return DeviceType.Local;
				case DriveType.Removable:
					return DeviceType.Removable;
				case DriveType.Network:
					return DeviceType.Network;
				case DriveType.CDRom:
					return DeviceType.CDRom;
				case DriveType.Ram:
					return DeviceType.Ram;
				default:
					return DeviceType.Unknown;
			}
		}


		/// <summary>
		/// Determines if a device is virtual or physical based on its characteristics
		/// </summary>
		/// <param name="_device">The disk device to evaluate</param>
		/// <returns>The determined MountType (Virtual, Physical, or Unknown)</returns>
		private MountType DetermineVirtualOrPhysical(DiskDevice _device) {
			// Check device type first
			if (_device.DeviceType == DeviceType.Network) {
				return MountType.Network;
			}

			if (_device.DeviceType == DeviceType.Ram) {
				return MountType.Virtual;
			}

			// Check for virtual drive indicators
			Boolean hasVirtualIndicators =
					!String.IsNullOrEmpty(_device.Model) && _device.Model.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
					!String.IsNullOrEmpty(_device.DeviceID) && (_device.DeviceID.IndexOf("VMWARE", StringComparison.OrdinalIgnoreCase) >= 0 ||
																										 _device.DeviceID.IndexOf("VBOX", StringComparison.OrdinalIgnoreCase) >= 0) ||
					!String.IsNullOrEmpty(_device.InterfaceType) && _device.InterfaceType.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
					!String.IsNullOrEmpty(_device.Manufacturer) && _device.Manufacturer.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 &&
					!String.IsNullOrEmpty(_device.Model) && _device.Model.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0;

			if (hasVirtualIndicators) {
				return MountType.Virtual;
			}

			// Check for physical device indicators
			if (_device.DeviceType == DeviceType.Local || _device.DeviceType == DeviceType.Removable) {
				// If we know it's removable, it's physical
				if (_device.DeviceType == DeviceType.Removable) {
					return MountType.Physical;
				}

				// For local drives, look for physical interface indicators
				Boolean hasPhysicalInterface =
						!String.IsNullOrEmpty(_device.InterfaceType) &&
						(_device.InterfaceType.IndexOf("IDE", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("SCSI", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("SATA", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("SAS", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("1394", StringComparison.OrdinalIgnoreCase) >= 0 ||
						 _device.InterfaceType.IndexOf("NVMe", StringComparison.OrdinalIgnoreCase) >= 0);

				if (hasPhysicalInterface) {
					return MountType.Physical;
				}

				// If it's a CD/DVD drive, it's physical
				if (_device.DeviceType == DeviceType.CDRom) {
					return MountType.Physical;
				}

				// Default for local drives: assume physical if we have a drive letter
				if (!String.IsNullOrEmpty(_device.DriveLetter)) {
					return MountType.Physical;
				}
			}

			// As a last resort for local drives, default to physical
			if (_device.DeviceType == DeviceType.Local) {
				return MountType.Physical;
			}

			return MountType.Unknown;
		}

#if WINDOWS
        [SupportedOSPlatform("windows")]
        private void EnrichWithWindowsSpecificInfo(Dictionary<String, DiskDevice> _driveMapping) {
            try {
                // Add physical disk information from WMI
                EnrichWithPhysicalDiskInfo(_driveMapping);
                
                // Add disk partition information including partition style
                EnrichWithPartitionInfo(_driveMapping);
                
                // Add SMART health status where available
                EnrichWithHealthStatus(_driveMapping);
            } catch (Exception ex) {
                System.Diagnostics.Debug.Print($"Error enriching with Windows-specific info: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps Windows volume GUIDs to drive letters
        /// </summary>
        /// <returns>Dictionary mapping drive letters to volume GUIDs</returns>
        [SupportedOSPlatform("windows")]
        private Dictionary<String, String> GetVolumeGuidMapping() {
            Dictionary<String, String> volumeMap = new Dictionary<String, String>();
            
            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT DeviceID, DriveLetter FROM Win32_Volume WHERE DriveLetter IS NOT NULL")) {
                    foreach (ManagementObject volume in searcher.Get()) {
                        String? deviceId = volume["DeviceID"] as String;
                        String? driveLetter = volume["DriveLetter"] as String;
                        
                        if (!String.IsNullOrEmpty(driveLetter) && deviceId != null) {
                            volumeMap[driveLetter] = deviceId;
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.Print($"Error getting volume GUID mapping: {ex.Message}");
            }
            
            return volumeMap;
        }

        /// <summary>
        /// Enriches drive information with physical disk details from WMI
        /// </summary>
        /// <param name="_driveMapping">Mapping of drive letters to DiskDevice objects</param>
        [SupportedOSPlatform("windows")]
        private void EnrichWithPhysicalDiskInfo(Dictionary<String, DiskDevice> _driveMapping) {
            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive")) {
                    foreach (ManagementObject disk in searcher.Get()) {
                        // Get partition associations
                        using (ManagementObjectCollection partitions = disk.GetRelated("Win32_DiskPartition")) {
                            foreach (ManagementObject partition in partitions) {
                                using (ManagementObjectCollection logicalDisks = partition.GetRelated("Win32_LogicalDisk")) {
                                    foreach (ManagementObject logicalDisk in logical Disks) {
                                        String? driveLetter = logicalDisk["DeviceID"] as String;
                                        
                                        if (!String.IsNullOrEmpty(driveLetter) && _driveMapping.ContainsKey(driveLetter)) {
                                            DiskDevice device = _driveMapping[driveLetter];
                                            device.DeviceID = disk["DeviceID"] as String ?? String.Empty;
                                            device.Model = disk["Model"] as String ?? "Unknown";
                                            device.SerialNumber = disk["SerialNumber"] as String ?? String.Empty;
                                            device.InterfaceType = disk["InterfaceType"] as String ?? "Unknown";
                                            device.MediaType = disk["MediaType"] as String ?? "Unknown";
                                            device.FirmwareRevision = disk["FirmwareRevision"] as String ?? String.Empty;
                                            device.Manufacturer = GetManufacturerFromModel(device.Model);
                                            device.BytesPerSector = Convert.ToUInt32(disk["BytesPerSector"] ?? 512);
                                            device.BusType = DetermineBusType(device.InterfaceType, device.DeviceID);
                                            
                                            // Determine mount type based on physical device characteristics
                                            if (device.InterfaceType.Contains("USB") || device.InterfaceType.Contains("1394")) {
                                                device.MountType = MountType.Physical;
                                            } else if (device.DeviceType == DeviceType.Network) {
                                                device.MountType = MountType.Network;
                                            } else if (device.InterfaceType.Contains("IDE") || 
                                                      device.InterfaceType.Contains("SCSI") || 
                                                      device.InterfaceType.Contains("SATA") || 
                                                      device.InterfaceType.Contains("SAS") ||
                                                      device.InterfaceType.Contains("NVMe")) {
                                                device.MountType = MountType.Physical;
                                            } else {
                                                device.MountType = DetermineVirtualOrPhysical(device);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.Print($"Error enriching physical disk info: {ex.Message}");
            }
        }

        /// <summary>
        /// Enriches drive information with partition details
        /// </summary>
        /// <param name="_driveMapping">Mapping of drive letters to DiskDevice objects</param>
        [SupportedOSPlatform("windows")]
        private void EnrichWithPartitionInfo(Dictionary<String, DiskDevice> _driveMapping) {
            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition")) {
                    foreach (ManagementObject partition in searcher.Get()) {
                        using (ManagementObjectCollection logicalDisks = partition.GetRelated("Win32_LogicalDisk")) {
                            foreach (ManagementObject logicalDisk in logicalDisks) {
                                String? driveLetter = logicalDisk["DeviceID"] as String;
                                
                                if (!String.IsNullOrEmpty(driveLetter) && _driveMapping.ContainsKey(driveLetter)) {
                                    String? type = partition["Type"] as String;
                                    if (type?.Contains("GPT") == true) {
                                        _driveMapping[driveLetter].PartitionStyle = "GPT";
                                    } else if (type?.Contains("MBR") == true || type?.Contains("Partition") == true) {
                                        _driveMapping[driveLetter].PartitionStyle = "MBR";
                                    } else {
                                        _driveMapping[driveLetter].PartitionStyle = type ?? "Unknown";
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.Print($"Error enriching partition info: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds health status information where available
        /// </summary>
        /// <param name="_driveMapping">Mapping of drive letters to DiskDevice objects</param>
        [SupportedOSPlatform("windows")]
        private void EnrichWithHealthStatus(Dictionary<String, DiskDevice> _driveMapping) {
            try {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive")) {
                    foreach (ManagementObject disk in searcher.Get()) {
                        String? deviceID = disk["DeviceID"] as String;
                        String? status = disk["Status"] as String;
                        
                        if (!String.IsNullOrEmpty(deviceID)) {
                            // Find all devices mapped to this physical disk
                            foreach (var device in _driveMapping.Values) {
                                if (device.DeviceID == deviceID) {
                                    device.HealthStatus = status ?? "Unknown";
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.Print($"Error enriching health status: {ex.Message}");
            }
        }
#endif

		/// <summary>
		/// Extracts the manufacturer from the model string
		/// </summary>
		/// <param name="_model">The model string</param>
		/// <returns>The extracted manufacturer name</returns>
		private String GetManufacturerFromModel(String _model) {
			if (String.IsNullOrEmpty(_model)) {
				return "Unknown";
			}

			// Common manufacturer prefixes
			String[] commonManufacturers = new String[] {
								"WDC", "WESTERN DIGITAL", "SEAGATE", "TOSHIBA", "HITACHI", "SAMSUNG",
								"KINGSTON", "SANDISK", "CRUCIAL", "INTEL", "MICRON", "HP", "HGST",
								"FUJITSU", "PLEXTOR", "OCZ", "ADATA", "TRANSCEND"
						};

			foreach (String manufacturer in commonManufacturers) {
				if (_model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase) ||
						_model.Contains(manufacturer, StringComparison.OrdinalIgnoreCase)) {
					return manufacturer;
				}
			}

			// If no manufacturer found, return the first word as a guess
			String[] parts = _model.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
			return parts.Length > 0 ? parts[0] : "Unknown";
		}

		/// <summary>
		/// Determines the bus type from the interface type and device ID
		/// </summary>
		/// <param name="_interfaceType">Interface type string</param>
		/// <param name="_deviceID">Device ID string</param>
		/// <returns>The determined bus type</returns>
		private String DetermineBusType(String _interfaceType, String _deviceID) {
			if (String.IsNullOrEmpty(_interfaceType)) {
				return "Unknown";
			}

			if (_interfaceType.Contains("USB")) {
				return "USB";
			}

			if (_interfaceType.Contains("1394")) {
				return "FireWire";
			}

			if (_interfaceType.Contains("IDE")) {
				return "IDE";
			}

			if (_interfaceType.Contains("SATA")) {
				return "SATA";
			}

			if (_interfaceType.Contains("SCSI")) {
				return "SCSI";
			}

			if (_interfaceType.Contains("SAS")) {
				return "SAS";
			}

			if (!String.IsNullOrEmpty(_deviceID) && _deviceID.Contains("NVME")) {
				return "NVMe";
			}

			return _interfaceType;
		}
	}
}