using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace Aster;

internal static class Inventory
{
	private static string S(object v)
	{
		if (v != null)
		{
			return Convert.ToString(v);
		}
		return "Unavailable";
	}

	private static void Wmi(Table t, string query, params string[] keys)
	{
		using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(query);
		managementObjectSearcher.Options.Timeout = TimeSpan.FromSeconds(12.0);
		foreach (ManagementObject o in managementObjectSearcher.Get())
		{
			using (o)
			{
				t.Rows.Add(keys.Select((string k) => S(o[k])).ToArray());
			}
		}
	}

	public static Table Read(string name)
	{
		if (new string[6] { "CPU specs", "Memory specs", "Storage specs", "Motherboard & BIOS", "Network adapters", "GPU" }.Contains(name))
		{
			return Hardware(name);
		}
		try
		{
			Table table;
			switch (name)
			{
			case "Services":
				table = new Table("Name", "Display name", "Status", "Start mode", "PID");
				Wmi(table, "SELECT Name,DisplayName,State,StartMode,ProcessId FROM Win32_Service", "Name", "DisplayName", "State", "StartMode", "ProcessId");
				table.Note = "Read-only service inventory. Use Windows Services for configuration.";
				break;
			case "Startup apps":
				table = new Table("Name", "Command", "Location", "User");
				Wmi(table, "SELECT Name,Command,Location,User FROM Win32_StartupCommand", "Name", "Command", "Location", "User");
				table.Note = "Registered startup commands; scheduled tasks and all packaged-app startup entries are not included.";
				break;
			case "Drivers":
				table = new Table("Name", "Display name", "State", "Start mode", "Path");
				Wmi(table, "SELECT Name,DisplayName,State,StartMode,PathName FROM Win32_SystemDriver", "Name", "DisplayName", "State", "StartMode", "PathName");
				break;
			case "GPU":
				table = new Table("Adapter", "Driver version", "Video processor", "Display mode");
				Wmi(table, "SELECT Name,DriverVersion,VideoProcessor,VideoModeDescription FROM Win32_VideoController", "Name", "DriverVersion", "VideoProcessor", "VideoModeDescription");
				table.Note = "GPU identification. Temperature, board power and CPU package power need vendor sensor support and are not available here.";
				break;
			case "Users":
				table = new Table("Account", "Domain", "Status", "SID");
				Wmi(table, "SELECT Name,Domain,Status,SID FROM Win32_UserAccount WHERE LocalAccount=True", "Name", "Domain", "Status", "SID");
				table.Note = "Local user accounts, not a list of active login sessions.";
				break;
			case "Connections":
			{
				table = new Table("Protocol", "Local endpoint", "Remote endpoint", "State");
				IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				TcpConnectionInformation[] activeTcpConnections = iPGlobalProperties.GetActiveTcpConnections();
				foreach (TcpConnectionInformation tcpConnectionInformation in activeTcpConnections)
				{
					table.Rows.Add(new string[4]
					{
						"TCP",
						tcpConnectionInformation.LocalEndPoint.ToString(),
						tcpConnectionInformation.RemoteEndPoint.ToString(),
						tcpConnectionInformation.State.ToString()
					});
				}
				IPEndPoint[] activeTcpListeners = iPGlobalProperties.GetActiveTcpListeners();
				foreach (IPEndPoint iPEndPoint in activeTcpListeners)
				{
					table.Rows.Add(new string[4]
					{
						"TCP",
						iPEndPoint.ToString(),
						"—",
						"Listening"
					});
				}
				IPEndPoint[] activeUdpListeners = iPGlobalProperties.GetActiveUdpListeners();
				foreach (IPEndPoint iPEndPoint2 in activeUdpListeners)
				{
					table.Rows.Add(new string[4]
					{
						"UDP",
						iPEndPoint2.ToString(),
						"—",
						"Bound"
					});
				}
				table.Note = "Current endpoints on this computer. Process ownership is not provided.";
				break;
			}
			case "Disk space":
			{
				table = new Table("Drive", "Label", "Format", "Capacity", "Free", "Used");
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo driveInfo in drives)
				{
					try
					{
						if (driveInfo.IsReady)
						{
							table.Rows.Add(new string[6]
							{
								driveInfo.Name,
								driveInfo.VolumeLabel,
								driveInfo.DriveFormat,
								Bytes(driveInfo.TotalSize),
								Bytes(driveInfo.AvailableFreeSpace),
								(100.0 * (double)(driveInfo.TotalSize - driveInfo.TotalFreeSpace) / (double)driveInfo.TotalSize).ToString("F1") + "%"
							});
						}
					}
					catch
					{
					}
				}
				table.Note = "Mounted volumes. Drive health / SMART is not inferred from free space.";
				break;
			}
			case "Installed apps":
			{
				table = new Table("Application", "Version", "Publisher", "Installed");
				HashSet<string> hashSet = new HashSet<string>();
				RegistryHive[] array2 = new RegistryHive[2]
				{
					RegistryHive.LocalMachine,
					RegistryHive.CurrentUser
				};
				foreach (RegistryHive hKey in array2)
				{
					RegistryView[] array3 = new RegistryView[2]
					{
						RegistryView.Registry64,
						RegistryView.Registry32
					};
					foreach (RegistryView view in array3)
					{
						using RegistryKey registryKey = RegistryKey.OpenBaseKey(hKey, view);
						using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
						if (registryKey2 == null)
						{
							continue;
						}
						string[] array = registryKey2.GetSubKeyNames();
						foreach (string text3 in array)
						{
							using RegistryKey registryKey3 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + text3);
							if (registryKey3 != null)
							{
								string text4 = S(registryKey3.GetValue("DisplayName"));
								string text5 = S(registryKey3.GetValue("DisplayVersion"));
								if (!(text4 == "Unavailable") && hashSet.Add(text4 + text5))
								{
									table.Rows.Add(new string[4]
									{
										text4,
										text5,
										S(registryKey3.GetValue("Publisher")),
										S(registryKey3.GetValue("InstallDate"))
									});
								}
							}
						}
					}
				}
				table.Note = "Desktop applications registered for uninstall; some Microsoft Store apps may not appear.";
				break;
			}
			default:
			{
				table = new Table("Property", "Value");
				table.Rows.Add(new string[2]
				{
					"Computer",
					Environment.MachineName
				});
				table.Rows.Add(new string[2]
				{
					"Current user",
					Environment.UserDomainName + "\\" + Environment.UserName
				});
				table.Rows.Add(new string[2]
				{
					"Logical processors",
					Environment.ProcessorCount.ToString()
				});
				table.Rows.Add(new string[2]
				{
					"Process architecture",
					Environment.Is64BitProcess ? "64-bit" : "32-bit"
				});
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Caption,Version,OSArchitecture,LastBootUpTime FROM Win32_OperatingSystem"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						using (item)
						{
							string[] array = new string[4] { "Caption", "Version", "OSArchitecture", "LastBootUpTime" };
							foreach (string text in array)
							{
								table.Rows.Add(new string[2]
								{
									text,
									S(item[text])
								});
							}
						}
					}
				}
				using (ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed FROM Win32_Processor"))
				{
					foreach (ManagementObject item2 in managementObjectSearcher2.Get())
					{
						using (item2)
						{
							string[] array = new string[4] { "Name", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed" };
							foreach (string text2 in array)
							{
								table.Rows.Add(new string[2]
								{
									"CPU " + text2,
									S(item2[text2])
								});
							}
						}
					}
				}
				table.Note = "Clock values from firmware are in MHz and may differ from live boost frequencies.";
				break;
			}
			}
			return table;
		}
		catch (Exception ex)
		{
			Table table = new Table("Status", "Details");
			table.Rows.Add(new string[2] { "Provider unavailable", ex.Message });
			table.Note = "Windows may restrict this data. No estimated or simulated values are substituted.";
			return table;
		}
	}

	public static string Bytes(double n)
	{
		if (double.IsNaN(n))
		{
			return "Unavailable";
		}
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		int num = 0;
		while (n >= 1024.0 && num < array.Length - 1)
		{
			n /= 1024.0;
			num++;
		}
		return n.ToString((num > 1) ? "F1" : "F0") + " " + array[num];
	}

	private static string Property(ManagementBaseObject o, string key)
	{
		try
		{
			return S(o[key]);
		}
		catch
		{
			return "Unavailable";
		}
	}

	private static string FormatSpec(string key, string value)
	{
		if (!double.TryParse(value, out var result))
		{
			return value;
		}
		switch (key)
		{
		case "Capacity":
		case "Size":
			return Bytes(result);
		case "MaxClockSpeed":
		case "CurrentClockSpeed":
			return result + " MHz (firmware reported)";
		case "ConfiguredClockSpeed":
		case "Speed":
			return result + " (Windows memory speed value)";
		case "L2CacheSize":
		case "L3CacheSize":
			return result + " KB";
		case "SMBIOSMemoryType":
		{
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			dictionary.Add(20, "DDR");
			dictionary.Add(21, "DDR2");
			dictionary.Add(24, "DDR3");
			dictionary.Add(26, "DDR4");
			dictionary.Add(30, "LPDDR4");
			dictionary.Add(34, "DDR5");
			dictionary.Add(35, "LPDDR5");
			Dictionary<int, string> dictionary2 = dictionary;
			if (!dictionary2.ContainsKey((int)result))
			{
				return result + " (SMBIOS type code)";
			}
			return dictionary2[(int)result];
		}
		case "FormFactor":
			if (result == 8.0)
			{
				return "DIMM";
			}
			if (result == 12.0)
			{
				return "SO-DIMM";
			}
			return result + " (form factor code)";
		default:
			return value;
		}
	}

	private static void Specs(Table t, string component, string wmiClass, string[] fields, string scope = null)
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = ((scope == null) ? new ManagementObjectSearcher("SELECT * FROM " + wmiClass) : new ManagementObjectSearcher(scope, "SELECT * FROM " + wmiClass));
			managementObjectSearcher.Options.Timeout = TimeSpan.FromSeconds(10.0);
			int num = 0;
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				using (item)
				{
					string text = component + " " + num++;
					foreach (string text2 in fields)
					{
						string[] array = text2.Split('|');
						t.Rows.Add(new string[3]
						{
							text,
							(array.Length > 1) ? array[1] : array[0],
							FormatSpec(array[0], Property(item, array[0]))
						});
					}
				}
			}
			if (num == 0)
			{
				t.Rows.Add(new string[3] { component, "Status", "No entries reported" });
			}
		}
		catch (Exception ex)
		{
			t.Rows.Add(new string[3] { component, "Provider unavailable", ex.Message });
		}
	}

	public static Table Hardware(string name)
	{
		Table table = new Table("Component", "Specification", "Value");
		switch (name)
		{
		case "CPU specs":
			Specs(table, "CPU", "Win32_Processor", new string[14]
			{
				"Name|Model", "Manufacturer", "SocketDesignation|Socket", "NumberOfCores|Physical cores", "NumberOfEnabledCore|Enabled cores", "NumberOfLogicalProcessors|Logical processors", "MaxClockSpeed|Maximum firmware clock", "CurrentClockSpeed|Current firmware clock", "L2CacheSize|L2 cache", "L3CacheSize|L3 cache",
				"AddressWidth|Address width (bits)", "VirtualizationFirmwareEnabled|Virtualization enabled in firmware", "SecondLevelAddressTranslationExtensions|Second-level address translation", "ProcessorId|Processor identifier"
			});
			table.Note = "Firmware clock values are not a live boost-clock sensor. Live CPU load is on Summary / Performance.";
			break;
		case "Memory specs":
			Specs(table, "Memory module", "Win32_PhysicalMemory", new string[11]
			{
				"DeviceLocator|Slot", "BankLabel|Bank", "Manufacturer", "PartNumber|Part number", "Capacity", "SMBIOSMemoryType|Memory type", "FormFactor|Form factor", "Speed|Rated speed", "ConfiguredClockSpeed|Configured speed", "DataWidth|Data width (bits)",
				"TotalWidth|Total width (bits)"
			});
			Specs(table, "Memory array", "Win32_PhysicalMemoryArray", new string[3] { "MemoryDevices|Slots reported", "MaxCapacityEx|Maximum supported capacity (KB)", "MemoryErrorCorrection|Error correction (SMBIOS code)" });
			table.Note = "Capacity and module details from firmware. Live RAM usage appears on Summary / Performance.";
			break;
		case "Storage specs":
			Specs(table, "Disk", "Win32_DiskDrive", new string[10] { "Index|Disk index", "Model", "FirmwareRevision|Firmware", "SerialNumber|Serial number", "Size|Capacity", "InterfaceType|Legacy interface label", "MediaType|Legacy media label", "Partitions|Partition count", "BytesPerSector|Bytes per sector", "PNPDeviceID|Device identifier" });
			Specs(table, "Storage device", "MSFT_PhysicalDisk", new string[7] { "DeviceId|Device ID", "FriendlyName|Model", "MediaType|Media type code (3 HDD / 4 SSD / 5 SCM)", "BusType|Bus code (7 USB / 11 SATA / 17 NVMe)", "Size|Capacity", "HealthStatus|Health code (0 healthy / 1 warning / 2 unhealthy)", "SpindleSpeed|Spindle speed (RPM)" }, "root\\Microsoft\\Windows\\Storage");
			table.Note = "Modern storage provider may require additional Windows access. Disk space lists mounted volumes separately.";
			break;
		case "Motherboard & BIOS":
			Specs(table, "Motherboard", "Win32_BaseBoard", new string[4] { "Manufacturer", "Product|Model", "Version", "SerialNumber|Serial number" });
			Specs(table, "BIOS", "Win32_BIOS", new string[5] { "Manufacturer", "SMBIOSBIOSVersion|BIOS version", "ReleaseDate|Release date (WMI format)", "SMBIOSMajorVersion|SMBIOS major version", "SMBIOSMinorVersion|SMBIOS minor version" });
			Specs(table, "System", "Win32_ComputerSystem", new string[5] { "Manufacturer", "Model", "SystemType|Architecture", "NumberOfProcessors|CPU sockets", "HypervisorPresent|Hypervisor present" });
			break;
		case "GPU":
			Specs(table, "Graphics adapter", "Win32_VideoController", new string[10] { "Name|Model", "AdapterCompatibility|Vendor", "VideoProcessor|Video processor", "DriverVersion|Driver version", "DriverDate|Driver date (WMI format)", "PNPDeviceID|Device identifier", "CurrentHorizontalResolution|Horizontal pixels", "CurrentVerticalResolution|Vertical pixels", "CurrentRefreshRate|Refresh rate (Hz)", "VideoModeDescription|Display mode" });
			table.Note = "NVIDIA telemetry refreshes every five cycles; other adapters show Windows-reported specifications.";
			break;
		case "Network adapters":
			try
			{
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface networkInterface in allNetworkInterfaces)
				{
					string name2 = networkInterface.Name;
					table.Rows.Add(new string[3] { name2, "Description", networkInterface.Description });
					table.Rows.Add(new string[3]
					{
						name2,
						"Type",
						networkInterface.NetworkInterfaceType.ToString()
					});
					table.Rows.Add(new string[3]
					{
						name2,
						"Status",
						networkInterface.OperationalStatus.ToString()
					});
					table.Rows.Add(new string[3]
					{
						name2,
						"Link speed",
						((double)networkInterface.Speed / 1000000.0).ToString("F0") + " Mbps"
					});
					table.Rows.Add(new string[3]
					{
						name2,
						"MAC address",
						networkInterface.GetPhysicalAddress().ToString()
					});
					try
					{
						IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
						table.Rows.Add(new string[3]
						{
							name2,
							"IP addresses",
							string.Join(", ", iPProperties.UnicastAddresses.Select((UnicastIPAddressInformation a) => a.Address.ToString()))
						});
						table.Rows.Add(new string[3]
						{
							name2,
							"Gateways",
							string.Join(", ", iPProperties.GatewayAddresses.Select((GatewayIPAddressInformation a) => a.Address.ToString()))
						});
						table.Rows.Add(new string[3]
						{
							name2,
							"DNS servers",
							string.Join(", ", iPProperties.DnsAddresses.Select((IPAddress a) => a.ToString()))
						});
					}
					catch
					{
						table.Rows.Add(new string[3] { name2, "Address details", "Unavailable" });
					}
				}
			}
			catch (Exception ex)
			{
				table.Rows.Add(new string[3] { "Network", "Provider unavailable", ex.Message });
			}
			table.Note = "Includes physical and virtual adapters. Link speed is negotiated capacity, not internet speed.";
			break;
		}
		return table;
	}
}
