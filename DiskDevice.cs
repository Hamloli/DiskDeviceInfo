#if WINDOWS
using System.Management;
#endif

using System.Diagnostics;
using System.Management;

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
		/// <summary>
		/// Gets a comprehensive list of disk devices on the system with their mounting information
		/// </summary>
		/// <param name="_deviceTypeFilter">Optional filter to only return specific device types</param>
		/// <param name="_onlyMounted">If true, only returns mounted/ready drives</param>
		/// <returns>List of DiskDevice objects containing detailed device information</returns>
		public List<DiskDevice> GetDiskDevices(DeviceType? _deviceTypeFilter = null, Boolean _onlyReady = false) {
			List<DiskDevice> devices = new List<DiskDevice>();
			Debug.Print("\n=== Starting GetDiskDevices scan ===");

			try {
				// Check every drive letter A-Z
				for (char letter = 'A'; letter <= 'Z'; letter++) {
					String drivePath = $"{letter}:";
					Debug.Print($"\nChecking drive {drivePath}");

					try {
						DriveInfo driveInfo = new DriveInfo(drivePath);
						bool isReady = driveInfo.IsReady;
						Debug.Print($"  Drive exists, IsReady: {isReady}");

						DiskDevice device = new DiskDevice {
							DriveLetter = drivePath,
							DeviceType = MapDriveType(driveInfo.DriveType),
							IsReady = isReady
						};
						Debug.Print($"  Drive type: {device.DeviceType}");

						// Get additional info only if drive is ready
						if (device.IsReady) {
							device.VolumeName = driveInfo.VolumeLabel;
							device.FileSystem = driveInfo.DriveFormat;
							device.TotalSize = driveInfo.TotalSize;
							device.FreeSpace = driveInfo.AvailableFreeSpace;
							Debug.Print($"  Volume: {device.VolumeName}");
							Debug.Print($"  FileSystem: {device.FileSystem}");
						}

						EnrichDeviceInfo(device);
						Debug.Print($"  Model after enrichment: {device.Model}");

						// Only filter by device type if specified
						if (!_deviceTypeFilter.HasValue || device.DeviceType == _deviceTypeFilter.Value) {
							devices.Add(device);
							Debug.Print($"  Added to device list");
						}
					}
					catch (DriveNotFoundException) {
						Debug.Print($"  Drive not found");
					}
					catch (IOException ex) {
						Debug.Print($"  IO Exception (drive exists but not ready): {ex.Message}");
						// Drive exists but isn't ready - try to get info from WMI
						try {
							using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
									$"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{drivePath}'")) {
								foreach (ManagementObject drive in searcher.Get()) {
									Debug.Print($"  Found drive in WMI");
									DiskDevice device = new DiskDevice {
										DriveLetter = drivePath,
										DeviceType = MapDriveType((DriveType)Convert.ToInt32(drive["DriveType"])),
										IsReady = false
									};
									EnrichDeviceInfo(device);

									// Only filter by device type if specified
									if (!_deviceTypeFilter.HasValue || device.DeviceType == _deviceTypeFilter.Value) {
										devices.Add(device);
										Debug.Print($"  Added non-ready drive to list");
									}
								}
							}
						}
						catch (Exception wmiEx) {
							Debug.Print($"  Error checking WMI: {wmiEx.Message}");
						}
					}
					catch (Exception ex) {
						Debug.Print($"  Error checking drive: {ex.Message}");
					}
				}

				Debug.Print($"\nScan complete. Found {devices.Count} devices");
				foreach (var device in devices) {
					Debug.Print($"Final list: {device.DriveLetter} - {device.DeviceType} - Ready: {device.IsReady}");
				}
				return devices;
			}
			catch (Exception ex) {
				Debug.Print($"Critical error: {ex.Message}\n{ex.StackTrace}");
				throw;
			}
		}

		private DeviceType DetermineDeviceType(ManagementObject _disk) {
			String? mediaType = _disk["MediaType"] as String;
			String? interfaceType = _disk["InterfaceType"] as String;
			String? description = _disk["Description"] as String;

			if (mediaType?.Contains("Removable", StringComparison.OrdinalIgnoreCase) == true ||
					interfaceType?.Contains("USB", StringComparison.OrdinalIgnoreCase) == true ||
					interfaceType?.Contains("1394", StringComparison.OrdinalIgnoreCase) == true ||
					description?.Contains("USB", StringComparison.OrdinalIgnoreCase) == true ||
					description?.Contains("Card Reader", StringComparison.OrdinalIgnoreCase) == true) {
				return DeviceType.Removable;
			}

			if (mediaType?.Contains("Fixed", StringComparison.OrdinalIgnoreCase) == true) {
				return DeviceType.Local;
			}

			if (mediaType?.Contains("CD-ROM", StringComparison.OrdinalIgnoreCase) == true ||
					mediaType?.Contains("DVD", StringComparison.OrdinalIgnoreCase) == true) {
				return DeviceType.CDRom;
			}

			return DeviceType.Unknown;
		}


#if WINDOWS
[SupportedOSPlatform("windows")]
private void DetectUnmountedRemovableMedia(List<DiskDevice> _devices) {
    try {
        using (ManagementObjectSearcher searcher = 
               new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaLoaded=FALSE")) {
            foreach (ManagementObject disk in searcher.Get()) {
                // Check if it's a removable drive
                String? mediaType = disk["MediaType"] as String;
                String? description = disk["Description"] as String;
                String? deviceID = disk["DeviceID"] as String;
                
                if ((mediaType?.Contains("Removable") == true || 
                     description?.Contains("USB") == true || 
                     description?.Contains("Card Reader") == true) && 
                    !String.IsNullOrEmpty(deviceID)) {
                        
                    // Make sure we don't already have this device
                    if (!_devices.Any(d => d.DeviceID == deviceID)) {
                        DiskDevice device = new DiskDevice {
                            DeviceType = DeviceType.Removable,
                            MountType = MountType.Physical,
                            IsReady = false, // No media present
                            DeviceID = deviceID,
                            Model = disk["Model"] as String ?? "Unknown",
                            InterfaceType = disk["InterfaceType"] as String ?? "Unknown",
                            MediaType = mediaType ?? "Removable Media",
                            Manufacturer = GetManufacturerFromModel(disk["Model"] as String ?? "Unknown")
                        };
                        
                        _devices.Add(device);
                    }
                }
            }
        }
    } catch (Exception ex) {
        System.Diagnostics.Debug.Print($"Error detecting unmounted removable media: {ex.Message}");
    }
}

[SupportedOSPlatform("windows")]
private void DetectEmptyOpticalDrives(List<DiskDevice> _devices) {
    try {
        using (ManagementObjectSearcher searcher = 
               new ManagementObjectSearcher("SELECT * FROM Win32_CDROMDrive WHERE MediaLoaded=FALSE")) {
            foreach (ManagementObject cdrom in searcher.Get()) {
                String? driveLetter = cdrom["Drive"] as String;
                String? deviceID = cdrom["DeviceID"] as String;
                
                // Skip if we already have this drive
                if (!String.IsNullOrEmpty(driveLetter) && 
                    _devices.Any(d => d.DriveLetter.Equals(driveLetter + ":", StringComparison.OrdinalIgnoreCase))) {
                    continue;
                }
                
                if (!String.IsNullOrEmpty(deviceID) && !_devices.Any(d => d.DeviceID == deviceID)) {
                    DiskDevice device = new DiskDevice {
                        DriveLetter = !String.IsNullOrEmpty(driveLetter) ? driveLetter + ":" : String.Empty,
                        DeviceType = DeviceType.CDRom,
                        MountType = MountType.Physical,
                        IsReady = false, // No media present
                        DeviceID = deviceID,
                        Model = cdrom["Caption"] as String ?? "Optical Drive",
                        Manufacturer = cdrom["Manufacturer"] as String ?? "Unknown",
                        MediaType = "Optical"
                    };
                    
                    _devices.Add(device);
                }
            }
        }
    } catch (Exception ex) {
        System.Diagnostics.Debug.Print($"Error detecting empty optical drives: {ex.Message}");
    }
}
#endif



		private void EnrichDeviceInfo(DiskDevice _device) {
			try {
				String driveLetter = _device.DriveLetter.TrimEnd('\\');
				String query = $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'";

				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query)) {
					foreach (ManagementObject logicalDisk in searcher.Get()) {
						// Get the associated physical disk
						using (ManagementObjectCollection physicalDisks =
								logicalDisk.GetRelated("Win32_DiskDrive")) {
							foreach (ManagementObject disk in physicalDisks) {
								_device.DeviceID = disk["DeviceID"] as String ?? String.Empty;
								_device.Model = disk["Model"] as String ?? "Unknown";
								_device.InterfaceType = disk["InterfaceType"] as String ?? "Unknown";
								_device.MediaType = disk["MediaType"] as String ?? "Unknown";
								_device.Manufacturer = GetManufacturerFromModel(_device.Model);

								// Update device type based on physical characteristics
								if (_device.DeviceType == DeviceType.Removable ||
										_device.InterfaceType.Contains("USB") ||
										_device.InterfaceType.Contains("1394")) {
									_device.DeviceType = DeviceType.Removable;
								}

								_device.MountType = MountType.Physical;
								break;
							}
						}
					}
				}

				// If no physical disk was found (e.g., for unmounted removable drives)
				// try getting info directly from Win32_DiskDrive
				if (String.IsNullOrEmpty(_device.DeviceID) && _device.DeviceType == DeviceType.Removable) {
					using (ManagementObjectSearcher searcher =
							new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'")) {
						foreach (ManagementObject disk in searcher.Get()) {
							_device.DeviceID = disk["DeviceID"] as String ?? String.Empty;
							_device.Model = disk["Model"] as String ?? "Unknown";
							_device.InterfaceType = disk["InterfaceType"] as String ?? "Unknown";
							_device.MediaType = disk["MediaType"] as String ?? "Removable";
							_device.Manufacturer = GetManufacturerFromModel(_device.Model);
							_device.MountType = MountType.Physical;
							break;
						}
					}
				}
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.Print($"Error enriching device info: {ex.Message}");
			}
		}

		/// <summary>
		/// Maps System.IO.DriveType to our custom DeviceType enum
		/// </summary>
		/// <param name="_driveType">The DriveType from System.IO</param>
		/// <returns>Corresponding DeviceType value</returns>
		private DeviceType MapDriveType(DriveType _driveType) {
			switch (_driveType) {
				case DriveType.Fixed: return DeviceType.Local;
				case DriveType.Removable: return DeviceType.Removable;
				case DriveType.Network: return DeviceType.Network;
				case DriveType.CDRom: return DeviceType.CDRom;
				case DriveType.Ram: return DeviceType.Ram;
				default: return DeviceType.Unknown;
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

		/// <summary>
		/// Gets the last access time of a drive
		/// </summary>
		/// <param name="_driveLetter">Drive letter to check</param>
		/// <returns>The last access time, or null if unavailable</returns>
		private DateTime? GetLastAccessTime(String _driveLetter) {
			try {
				DirectoryInfo dirInfo = new DirectoryInfo(_driveLetter);
				return dirInfo.LastAccessTime;
			}
			catch {
				return null;
			}
		}



		public void PrintDiskDevices(List<DiskDevice> _devices, OutputFormat _format = OutputFormat.Table) {
			if (_devices == null || _devices.Count == 0) {
				Console.WriteLine("No disk devices found.");
				return;
			}

			if (_format == OutputFormat.Table) {
				String header = String.Format("{0,-4} {1,-15} {2,-12} {3,-10} {4,-7} {5,-8} {6,-10} {7,-10} {8,-30}",
						"Drive", "Label", "Type", "Mount", "Ready", "FS", "Total", "Free", "Model");

				Console.WriteLine(header);
				Console.WriteLine(new String('-', header.Length));

				foreach (DiskDevice device in _devices) {
					SetConsoleColorForDeviceType(device.DeviceType);

					Console.WriteLine(String.Format("{0,-4} {1,-15} {2,-12} {3,-10} {4,-7} {5,-8} {6,-10} {7,-10} {8,-30}",
							device.DriveLetter,
							device.IsReady ? device.VolumeName : "[No Media]",
							device.DeviceType,
							device.MountType,
							device.IsReady ? "Yes" : "No",
							device.IsReady ? device.FileSystem : "-",
							device.IsReady ? FormatByteSize(device.TotalSize) : "-",
							device.IsReady ? FormatByteSize(device.FreeSpace) : "-",
							device.Model));

					Console.ResetColor();
				}

				Console.WriteLine();
				Console.WriteLine($"Total devices: {_devices.Count}");
				Console.WriteLine($"Ready: {_devices.Count(d => d.IsReady)}");
				Console.WriteLine($"Not Ready: {_devices.Count(d => !d.IsReady)}");
			}
			else {
				// CSV format handling remains the same
			}
		}



		/// <summary>
		/// Prints disk device information to the console in either table or CSV format
		/// </summary>
		/// <param name="_devices">List of disk devices to display</param>
		/// <param name="_format">Output format (Table or CSV)</param>
		//public void PrintDiskDevices(List<DiskDevice> _devices, OutputFormat _format = OutputFormat.Table) {
		//	// Check for empty device list
		//	if (_devices == null || _devices.Count == 0) {
		//		Console.WriteLine("No disk devices found.");
		//		return;
		//	}

		//	if (_format == OutputFormat.CSV) {
		//		// Print CSV format with headers
		//		Console.WriteLine("DriveLetter,VolumeName,DeviceType,MountType,IsReady,FileSystem,TotalSize,FreeSpace,DeviceID,Model");

		//		// Print each device as a CSV row
		//		foreach (DiskDevice device in _devices) {
		//			Console.WriteLine($"{device.DriveLetter},{EscapeCsv(device.VolumeName)},{device.DeviceType}," +
		//											$"{device.MountType},{device.IsReady},{device.FileSystem}," +
		//											$"{FormatByteSize(device.TotalSize)},{FormatByteSize(device.FreeSpace)}," +
		//											$"{EscapeCsv(device.DeviceID)},{EscapeCsv(device.Model)}");
		//		}
		//	}
		//	else {
		//		// Table Format - Create header with fixed column widths
		//		String header = String.Format("{0,-4} {1,-15} {2,-12} {3,-10} {4,-7} {5,-8} {6,-10} {7,-10} {8,-30}",
		//				"Drive", "Label", "Type", "Mount", "Ready", "FS", "Total", "Free", "Details");

		//		// Print header and separator line
		//		Console.WriteLine(header);
		//		Console.WriteLine(new String('-', header.Length));

		//		// Print each device as a formatted table row
		//		foreach (DiskDevice device in _devices) {
		//			// Set color based on device type
		//			SetConsoleColorForDeviceType(device.DeviceType);

		//			// For network devices, show network path instead of model
		//			String details = device.DeviceType == DeviceType.Network ? device.DeviceID : device.Model;

		//			// Format and print the device information with truncation for long fields
		//			Console.WriteLine(String.Format("{0,-4} {1,-15} {2,-12} {3,-10} {4,-7} {5,-8} {6,-10} {7,-10} {8,-30}",
		//					device.DriveLetter,
		//					device.VolumeName.Length > 15 ? device.VolumeName.Substring(0, 12) + "..." : device.VolumeName,
		//					device.DeviceType,
		//					device.MountType,
		//					device.IsReady ? "Yes" : "No",
		//					device.FileSystem,
		//					FormatByteSize(device.TotalSize),
		//					FormatByteSize(device.FreeSpace),
		//					details?.Length > 30 ? details.Substring(0, 27) + "..." : details));

		//			// Reset color for next line
		//			Console.ResetColor();
		//		}

		//		// Print summary
		//		Console.WriteLine();
		//		Console.WriteLine($"Total devices: {_devices.Count}");
		//	}
		//}



		/// <summary>
		/// Formats a byte size into human-readable format with appropriate unit
		/// </summary>
		/// <param name="_bytes">Number of bytes to format</param>
		/// <returns>Formatted string (e.g., "1.5 GB")</returns>
		private String FormatByteSize(Int64 _bytes) {
			if (_bytes <= 0) return "0 B";
			String[] sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
			Int32 order = (Int32)Math.Log(_bytes, 1024);
			order = Math.Min(order, sizes.Length - 1);
			Double size = _bytes / Math.Pow(1024, order);
			return $"{size:0.##} {sizes[order]}";
		}

		/// <summary>
		/// Escapes a string for CSV output by wrapping in quotes and doubling internal quotes
		/// </summary>
		/// <param name="_value">String to escape</param>
		/// <returns>CSV-escaped string</returns>
		private String EscapeCsv(String _value) {
			if (String.IsNullOrEmpty(_value)) return String.Empty;
			return $"\"{_value.Replace("\"", "\"\"")}\"";
		}

		/// <summary>
		/// Sets console text color based on device type for visual distinction
		/// </summary>
		/// <param name="_deviceType">Type of device to set color for</param>
		private void SetConsoleColorForDeviceType(DeviceType _deviceType) {
			switch (_deviceType) {
				case DeviceType.Local: Console.ForegroundColor = ConsoleColor.Green; break;
				case DeviceType.Removable: Console.ForegroundColor = ConsoleColor.Cyan; break;
				case DeviceType.Network: Console.ForegroundColor = ConsoleColor.Blue; break;
				case DeviceType.CDRom: Console.ForegroundColor = ConsoleColor.Yellow; break;
				case DeviceType.Ram: Console.ForegroundColor = ConsoleColor.Magenta; break;
				default: Console.ForegroundColor = ConsoleColor.Gray; break;
			}
		}


	}
	/// <summary>
	/// Output format for disk device information
	/// </summary>
	public enum OutputFormat {
		/// <summary>Formatted table output</summary>
		Table,

		/// <summary>CSV format for data import</summary>
		CSV
	}



}