using AP.F5.Base.Discovery.Classes;
using iControl;
using LumenWorks.Framework.IO.Csv;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System;
using System.Collections.Generic;
using System.IO;

namespace AP.F5.Device.Discovery
{
    class Program
    {
        // SCOM Functions Instance
        private static SCOM_Functions sf = new SCOM_Functions();
        // log4net Instance
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Config File Name
        private static string m_configFileName = "config.csv";
        // Device File Name
        private static string m_deviceFileName = "devices.csv";
        // Management Server Name
        private static string m_managementServer;
        // Discovery Type
        private static string m_discoveryType;

        // Interface to F5 Device
        private static Interfaces m_interfaces = new Interfaces();

        // List of Devices
        private static List<f5Device> DeviceList = new List<f5Device>();

        // CSV Position Indicators
        public static int CSV_ADDRESS = 0;
        public static int CSV_COMMUNITY = 1;
        public static int CSV_PORT = 2;
        public static int CSV_F5USER = 3;
        public static int CSV_F5PASSWORD = 4;

        // Create Snapshot Discovery Data Object
        private static SnapshotDiscoveryData snapshotDiscoveryData = new SnapshotDiscoveryData();
        private static IncrementalDiscoveryData incrementalDiscoveryData = new IncrementalDiscoveryData();

        /// <summary>
        /// Main Application Entry Point
        /// </summary>
        /// <param name="args">None</param>
        static void Main(string[] args)
        {
            // Write Header
            Console.WriteLine("AP.F5.Base.Discovery, ©A.Patrick 2017-2018");
            Console.WriteLine("Discovers F5 Devices for SCOM.");
            Console.WriteLine("");

            // First Thing is to get the Managment Server Name from the config file (if it exists).
            LoadConfig();

            // Get Devices (If Device File Exists
            if (File.Exists(m_deviceFileName))
            {
                // Write Discovered Data to SCOM Database ( For Relationships
                log.Info("Creating Inbound Connector to " + m_managementServer + "...");

                // Get Management Group
                SCOM_Functions.m_managementGroup = new ManagementGroup(m_managementServer);
                // Create Our Inbound Connector
                SCOM_Functions.CreateConnector();
                if (SCOM_Functions.m_monitoringConnector.Initialized)
                {

                    // Get Devices (and add to Discovery Data)
                    log.Info("Starting Device Discovery...");
                    GetDevices();

                    try
                    {
                        // Write Discovered Data to SCOM Database 
                        log.Info("Writing Discovery Data to " + m_managementServer);
                        if (m_discoveryType == "snapshot")
                        {
                            snapshotDiscoveryData.Overwrite(SCOM_Functions.m_monitoringConnector);
                        } else
                        {
                            incrementalDiscoveryData.Overwrite(SCOM_Functions.m_monitoringConnector);
                        }

                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }

                    // Free Connector
                    SCOM_Functions.m_monitoringConnector.Uninitialize();
                    SCOM_Functions.m_monitoringConnector = null;
                }
                else
                {
                    log.Fatal("Couldn't Initialize Inbound SCOM Connector!");
                    Environment.Exit(5);
                }
            }
            else
            {
                log.Fatal("Could not find device file, " + m_deviceFileName);
                Environment.Exit(5);
            }

            log.Info("Discovery Finished...");
        }

        /// <summary>
        /// Get Devices from CSV
        /// </summary>
        private static void GetDevices()
        {
            // Load In CSV File
            CsvReader csv = new CsvReader(new StreamReader(m_deviceFileName), true);
            while (csv.ReadNextRecord())
            {
                // Show Info
                log.Info("Discovering " + csv[CSV_ADDRESS] + "...");

                // Connect to F5 Device via iControl
                m_interfaces.initialize(csv[CSV_ADDRESS], csv[CSV_F5USER], csv[CSV_F5PASSWORD]);
                if (m_interfaces.initialized)
                {
                    // Set active partition to "Common"
                    m_interfaces.ManagementPartition.set_active_partition("Common");

                    // Create New Device
                    f5Device dev = new f5Device(SCOM_Functions.m_managementGroup, csv[CSV_ADDRESS], csv[CSV_COMMUNITY], Convert.ToInt32(csv[CSV_PORT]), csv[CSV_F5USER], csv[CSV_F5PASSWORD]);
                    if (m_discoveryType=="snapshot")
                    {
                        AddDeviceToSnapshotDiscoveryData(dev);
                    }
                    {
                        AddDeviceToIncrementalDiscoveryData(dev);
                    }

                } else
                {
                    log.Error("Couldn't connect iControl to " + csv[CSV_ADDRESS].ToString());
                }

            }
            // Clear CSV File
            csv.Dispose();

        }

        /// <summary>
        /// Load Config
        /// </summary>
        /// <returns>Name of Management Server to Conenct to, localhost if config.csv cannot be found or no entry</returns>
        private static void LoadConfig()
        {
            // Set to default of localhost
            m_managementServer = "localhost";
            m_discoveryType = "snapshot";

            // See if File Exists
            if (!File.Exists(m_configFileName))
            {
                log.Info("Could not find Config File, config.csv, will use locahost as Management Server Name and perform a snapshot discovery.");
            }

            // Load In CSV File
            CsvReader csv = new CsvReader(new StreamReader(m_configFileName), true);
            if (csv.FieldCount != 2)
            {
                log.Info("Config File, config.csv, does not have the expected number of fields, will use locahost as Management Server Name and perform a snapshot discovery.");
            }
            else
            {
                // Read First Record
                csv.ReadNextRecord();
                // Get Management Server Name
                m_managementServer = csv[0].ToString();
                string incremental  = csv[1].ToString().ToLower();
                if (incremental=="true")
                {
                    m_discoveryType = "incremental";
                }
            }

            // Dispose of CSV Handler
            csv.Dispose();

        }

        /// <summary>
        /// Add Device To Snapshot Discovery Data
        /// </summary>
        /// <param name="dev"></param>
        private static void AddDeviceToSnapshotDiscoveryData(f5Device dev)
        {
            // Add Device to Discovery Data
            snapshotDiscoveryData.Include(dev.SCOM_Object_Device);

            // Add Failover State and Relationship to Discovery Data
            snapshotDiscoveryData.Include(dev.SCOM_Object_FailoverState);

            // Add Memory and Relationship to Discovery Data
            snapshotDiscoveryData.Include(dev.SCOM_Object_Memory);

            // Add Fans and Relationships to Discovery Data
            if (dev.Fans.Count > 0)
            {
                // Add FansGroup and Relationship
                snapshotDiscoveryData.Include(dev.SCOM_Object_FanGroup);
                foreach (Fan f in dev.Fans)
                {
                    // Add Fan and Relationship
                    snapshotDiscoveryData.Include(f.SCOM_Object_Fan);
                }
            }

            // Add PowerSupplies and Relationships to Discovery Data
            if (dev.PowerSupplies.Count > 0)
            {
                // Add PowerSuppliesGroup and Relationship
                snapshotDiscoveryData.Include(dev.SCOM_Object_PowerSupplyGroup);
                foreach (PowerSupply p in dev.PowerSupplies)
                {
                    // Add PowerSupply and Relationship
                    snapshotDiscoveryData.Include(p.SCOM_Object_PowerSupply);
                }
            }

            // Add Processors
            if (dev.Processors.Count > 0)
            {
                // Add ProcessorsGroup and Relationship
                snapshotDiscoveryData.Include(dev.SCOM_Object_ProcessorGroup);
                foreach (Processor p in dev.Processors)
                {
                    // Add Processor and Relationship
                    snapshotDiscoveryData.Include(p.SCOM_Object_Processor);
                }
            }

            // Add Temperature Sensors
            if (dev.TempSensors.Count > 0)
            {
                // Add Temp Sensors Group and Relationship
                snapshotDiscoveryData.Include(dev.SCOM_Object_TempSensorsGroup);
                // Add Temperature Sensors
                foreach (TempSensor t in dev.TempSensors)
                {
                    snapshotDiscoveryData.Include(t.SCOM_Object_TempSensor);
                }
            }

            // Add Disk Partitions and Relationships to Discovery Data
            if (dev.DiskPartitions.Count > 0)
            {
                // Add Disk partitions Group to Discovery Data
                snapshotDiscoveryData.Include(dev.SCOM_Object_DiskPartitionGroup);
                // Add Disk Partitions
                foreach (DiskPartition disk in dev.DiskPartitions)
                {
                    snapshotDiscoveryData.Include(disk.SCOM_Object_DiskPartition);
                }
            }
        }

        /// <summary>
        /// Add Device To Incremental Discovery Data
        /// </summary>
        /// <param name="dev"></param>
        private static void AddDeviceToIncrementalDiscoveryData(f5Device dev)
        {
            // Add Device to Discovery Data
            incrementalDiscoveryData.Add(dev.SCOM_Object_Device);

            // Add Failover State and Relationship to Discovery Data
            incrementalDiscoveryData.Add(dev.SCOM_Object_FailoverState);

            // Add Memory and Relationship to Discovery Data
            incrementalDiscoveryData.Add(dev.SCOM_Object_Memory);

            // Add Fans and Relationships to Discovery Data
            if (dev.Fans.Count > 0)
            {
                // Add FansGroup and Relationship
                incrementalDiscoveryData.Add(dev.SCOM_Object_FanGroup);
                foreach (Fan f in dev.Fans)
                {
                    // Add Fan and Relationship
                    incrementalDiscoveryData.Add(f.SCOM_Object_Fan);
                }
            }

            // Add PowerSupplies and Relationships to Discovery Data
            if (dev.PowerSupplies.Count > 0)
            {
                // Add PowerSuppliesGroup and Relationship
                incrementalDiscoveryData.Add(dev.SCOM_Object_PowerSupplyGroup);
                foreach (PowerSupply p in dev.PowerSupplies)
                {
                    // Add PowerSupply and Relationship
                    incrementalDiscoveryData.Add(p.SCOM_Object_PowerSupply);
                }
            }

            // Add Processors
            if (dev.Processors.Count > 0)
            {
                // Add ProcessorsGroup and Relationship
                incrementalDiscoveryData.Add(dev.SCOM_Object_ProcessorGroup);
                foreach (Processor p in dev.Processors)
                {
                    // Add Processor and Relationship
                    incrementalDiscoveryData.Add(p.SCOM_Object_Processor);
                }
            }

            // Add Temperature Sensors
            if (dev.TempSensors.Count > 0)
            {
                // Add Temp Sensors Group and Relationship
                incrementalDiscoveryData.Add(dev.SCOM_Object_TempSensorsGroup);
                // Add Temperature Sensors
                foreach (TempSensor t in dev.TempSensors)
                {
                    incrementalDiscoveryData.Add(t.SCOM_Object_TempSensor);
                }
            }

            // Add Disk Partitions and Relationships to Discovery Data
            if (dev.DiskPartitions.Count > 0)
            {
                // Add Disk partitions Group to Discovery Data
                incrementalDiscoveryData.Add(dev.SCOM_Object_DiskPartitionGroup);
                // Add Disk Partitions
                foreach (DiskPartition disk in dev.DiskPartitions)
                {
                    incrementalDiscoveryData.Add(disk.SCOM_Object_DiskPartition);
                }
            }
        }
    }
}
